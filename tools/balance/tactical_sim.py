"""Tactical check of 10.2 Combat System Draft, on a Combat Grid.

combat_sim.py has no space: anyone can hit anyone, so a fight is a race of total
damage against total HP, and one more body decides it. This model puts the same
fighters on cells. Melee fighters must walk, they are held by whoever stands next
to them, and every fighter picks targets by impact: how much harm the target does
to its side, against how fast this fighter can bring it down.

V0 is the configuration 10.2 reports. The other switches of Rules are the levers
tried against headcount, kept so that the table of levers can be repeated.

Run: python tools/balance/tactical_sim.py [fights per line]
"""
import io
import math
import random
import sys
import zlib
from dataclasses import dataclass, replace
from multiprocessing import Pool

import combat_sim as c

RUNS = int(sys.argv[1]) if len(sys.argv) > 1 else 3000
BOW_RANGE = 12
SPELL_RANGE = 6
ORDER_RANGE = 4
SUPPORT_RANGE = 2
ESCAPE_AP = 2
REACH_DISCOUNT = 0.7
KILL_BONUS = 2.5
ASLEEP_FACTOR = 0.2
CROWD_FACTOR = 0.5
BREAK_OFF_FACTOR = 1.5
ADJACENT_SHOT = 0.20
OBJECTIVE_BONUS = 3.0
FLASK_AP = 3
FLASK_RANGE = 6
STUN_AP = 2
STUN_SKILL = 5
SWEEP_AP = 2
FLASK_VALUE = 4.0
CONTROL_VALUE = {"sleep": 6.0, "stand": 3.0, "crush": 3.0, "support": 2.0}


@dataclass(frozen=True)
class Rules:
    width: int = 6
    gap: int = 12
    spacing: int = 2
    targeting: str = "impact"
    a_targeting: str = ""
    escape_ap: int = ESCAPE_AP
    armour: str = "percent"
    flat_divisor: float = 4.0
    flat_floor: float = 0.25
    skill_weight: float = 1.0
    cleave: bool = False
    morale: float = 0.0
    leader_weight: float = 1.0
    enemy_bonus: int = 0
    damage_scale: float = 1.0
    crit_bonus: float = 0.0
    flasks: int = 0
    flask_damage: float = 8.0
    flask_limit: int = 8
    carriers: int = 0
    flask_min: int = 2
    a_flask_min: int = 2
    stun: float = 0.0
    forces: tuple = (5, 5, 6, 6, 7)
    ranged: bool = False
    shift: int = 0
    sweep_skill: int = 0
    sweep_mult: float = 1.0


def cheb(a, b):
    return max(abs(a.x - b.x), abs(a.y - b.y))


def alive(side):
    return [u for u in side if u.hp > 0]


class Fight:
    def __init__(self, a, b, rules, objective=None, wounded_a=False):
        self.a, self.b, self.r, self.objective = a, b, rules, objective
        self.per_column = (rules.width + rules.spacing - 1) // rules.spacing
        depth = (len(a + b) // self.per_column + 1) * rules.spacing
        self.front = depth
        self.length = 2 * depth + rules.gap + 1
        self.cells = {}
        for u in (b if wounded_a else a + b):
            u.reset()
        for side, left in ((a, True), (b, False)):
            self.place(side, left)
        for u in a + b:
            u.intent = None
            u.skip = u.asleep = 0
            if not wounded_a or u in b:
                carrier = u.limit is not None and u.limit <= rules.flask_limit and not u.conducting
                u.flasks = u.extra.get("flasks", rules.flasks if carrier else 0)
            u.threat = self.threat(u)
        if rules.carriers:
            for side in (a, b):
                ranked = sorted((u for u in side if u.flasks and "flasks" not in u.extra), key=lambda u: u.limit)
                for u in ranked[rules.carriers:]:
                    u.flasks = 0
        self.leaders = [self.leader(a), self.leader(b)]
        self.first_throw = None

    def leader(self, side):
        ranks = sorted(side, key=lambda u: 9 if u.limit is None else u.limit)
        return ranks[0] if len(ranks) < 2 or ranks[0].limit != ranks[1].limit else None

    def place(self, side, left):
        front = [u for u in side if not u.weapon.ranged and "sleep" not in u.abilities]
        back = [u for u in side if u not in front]
        s = self.r.spacing
        for i, u in enumerate(front + back):
            rank, row = divmod(i, self.per_column)
            u.x = self.front - rank * s if left else self.front + self.r.gap + rank * s
            u.y = row * s
            self.cells[(u.x, u.y)] = u

    def foes(self, u):
        return self.b if u in self.a else self.a

    def own(self, u):
        return self.a if u in self.a else self.b

    def blows(self, u):
        return max(1, int((10 + u.dex / 20) / u.weapon.ap))

    def expected_blow(self, a, t):
        w = a.weapon
        raw = w.base * c.stage_factor(w.stage) * (1 if w.ranged else 1 + a.str / 50) * self.r.damage_scale
        return self.chance(a, t, 0.0) * self.reduce(raw, t)

    def threat(self, u):
        foes = self.foes(u)
        harm = self.blows(u) * sum(self.expected_blow(u, f) for f in foes) / max(1, len(foes))
        return harm + sum(CONTROL_VALUE.get(x, 0.0) for x in u.abilities) + FLASK_VALUE * u.flasks

    def chance(self, a, t, bonus):
        p = 0.70 + self.r.skill_weight * (a.accuracy() - t.evasion()) / 100 + bonus
        return min(0.95, max(0.05, p * (1 + a.luck / 100)))

    def defence(self, t):
        d = c.ARMOUR[t.armour][0] * c.stage_factor(t.armour_stage)
        if any("support" in u.abilities and u is not t and cheb(u, t) <= SUPPORT_RANGE for u in alive(self.own(t))):
            d += c.SUPPORT_DEFENCE
        return d

    def reduce(self, raw, t):
        d = self.defence(t)
        if self.r.armour == "flat":
            return max(raw * self.r.flat_floor, raw - d / self.r.flat_divisor)
        return raw * (1 - c.mitigation(d))

    def flanked(self, a, t):
        if a.weapon.ranged:
            return False
        ax, ay = a.x - t.x, a.y - t.y
        return any(b is not a and cheb(b, t) == 1 and (b.x - t.x) * ax + (b.y - t.y) * ay < 0 for b in alive(self.own(a)))

    def strike(self, a, t, bonus_crit=0.0, crit_extra=0.0, mult=1.0, unguarded=False):
        flank = self.flanked(a, t)
        bonus_hit = c.FLANK_HIT if flank else 0.0
        bonus_crit += c.FLANK_CRIT if flank else 0.0
        if t.exposed:
            bonus_hit += 0.10
            t.exposed = False
        if a.weapon.ranged and any(cheb(a, f) == 1 for f in alive(self.foes(a))):
            bonus_hit -= ADJACENT_SHOT
        if random.random() >= self.chance(a, t, bonus_hit):
            return False
        raw = a.crit_raw() + bonus_crit + self.r.crit_bonus
        crit = 1.5 + max(0, raw - 0.95) + crit_extra if random.random() < min(0.95, raw) else 1.0
        damage = self.reduce(a.damage_roll() * crit * mult * self.r.damage_scale, t)
        if t.brace:
            damage *= 1 - c.BRACE_REDUCTION
        if not unguarded and not a.weapon.ranged and t.weapon.guard and t.reserve >= c.REACTION_AP and not t.asleep and not t.skip:
            t.reserve -= c.REACTION_AP
            if random.random() < t.weapon.guard_chance:
                damage *= 1 - t.weapon.guard_absorb
        t.hp -= damage
        t.asleep = 0
        if t.hp <= 0:
            self.cells.pop((t.x, t.y), None)
        return True

    def score(self, u, e, ap):
        targeting = self.r.a_targeting if u in self.a and self.r.a_targeting else self.r.targeting
        if targeting == "random":
            return random.random()
        reach = cheb(u, e) - 1 if not u.weapon.ranged else max(0, cheb(u, e) - BOW_RANGE)
        if targeting == "nearest":
            return -reach - e.hp / 1000
        turns = max(0, math.ceil((reach - ap) / 10))
        expected = self.blows(u) * self.expected_blow(u, e)
        value = (e.threat + 0.1) * min(1.0, expected / max(1.0, e.hp)) * REACH_DISCOUNT ** turns
        if turns == 0 and e.hp <= expected:
            value *= KILL_BONUS
        if e.asleep or e.skip:
            value *= ASLEEP_FACTOR
        if sum(1 for b in alive(self.own(u)) if b is not u and cheb(b, e) == 1) >= 3:
            value *= CROWD_FACTOR
        if e is self.objective:
            value *= OBJECTIVE_BONUS
        return value

    def best(self, u, targets, ap):
        return max(targets, key=lambda e: self.score(u, e, ap))

    def step(self, u, goal, ap):
        """One cell towards goal, around whoever stands in the way. Returns the AP left, or None if stuck."""
        here = (cheb_xy(u.x, u.y, goal), dist2(u.x, u.y, goal))
        options = []
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                x, y = u.x + dx, u.y + dy
                if (dx or dy) and 0 <= x < self.length and 0 <= y < self.r.width and (x, y) not in self.cells:
                    options.append(((cheb_xy(x, y, goal), dist2(x, y, goal)), x, y))
        if not options:
            return None
        cost, x, y = min(options)
        if cost[0] > here[0] or (cost[0] == here[0] and cost[1] >= here[1]):
            return None
        held = [f for f in alive(self.foes(u)) if cheb(u, f) == 1 and not f.asleep and not f.skip]
        price = 1 + (self.r.escape_ap if held and not any(max(abs(x - f.x), abs(y - f.y)) == 1 for f in held) else 0)
        if ap < price:
            return None
        del self.cells[(u.x, u.y)]
        u.x, u.y = x, y
        self.cells[(x, y)] = u
        return ap - price

    def splash(self, x, y):
        return [v for v in alive(self.a + self.b) if max(abs(v.x - x), abs(v.y - y)) <= 1]

    def throw_flask(self, u):
        foes = alive(self.foes(u))
        own = alive(self.own(u))
        best, value = None, self.r.a_flask_min - 1 if u in self.a else self.r.flask_min - 1
        for f in foes:
            if cheb(u, f) > FLASK_RANGE:
                continue
            for dx in (-1, 0, 1):
                for dy in (-1, 0, 1):
                    hit = self.splash(f.x + dx, f.y + dy)
                    score = sum(v in foes for v in hit) - 2 * sum(v in own for v in hit)
                    if score > value:
                        best, value = (f.x + dx, f.y + dy), score
        if best is None:
            return False
        u.flasks -= 1
        self.first_throw = self.first_throw or self.own(u)
        for v in self.splash(*best):
            dodged = random.random() >= self.chance(u, v, 0.0)
            defence = c.ARMOUR[v.armour][1] * c.stage_factor(v.armour_stage)
            v.hp -= self.r.flask_damage * random.uniform(0.85, 1.15) * (0.5 if dodged else 1.0) * (1 - c.mitigation(defence))
            v.asleep = 0
            if v.hp <= 0:
                self.cells.pop((v.x, v.y), None)
        return True

    def control(self, u, ap):
        foes = alive(self.foes(u))
        if u.flasks and ap >= FLASK_AP and self.throw_flask(u):
            ap -= FLASK_AP
            foes = alive(self.foes(u))
        awake = [f for f in foes if not f.asleep and not f.skip]
        if "sleep" in u.abilities and len(awake) >= 2 and ap >= 5 and u.can_spend(c.POWERFUL):
            near = [f for f in awake if cheb(u, f) <= SPELL_RANGE]
            if near:
                target = max(near, key=lambda f: f.threat)
                u.wp -= u.max_wp() * c.POWERFUL
                ap -= 5
                if c.resist_check(u, target):
                    target.asleep = c.SLEEP_ROUNDS
                else:
                    u.exposed = True
        if "stand" in u.abilities and ap >= 4 + u.weapon.ap and u.can_spend(c.ORDINARY):
            near = [f for f in awake if not f.asleep and not f.skip and cheb(u, f) <= ORDER_RANGE]
            if near:
                target = max(near, key=lambda f: f.threat)
                u.wp -= u.max_wp() * c.ORDINARY
                ap -= 4
                if c.resist_check(u, target):
                    target.skip = 1
                else:
                    u.exposed = True
        if "brace" in u.abilities and ap >= 3 + u.weapon.ap and u.can_spend(c.ORDINARY) and sum(1 for f in foes if cheb(u, f) <= 3) >= 2:
            u.wp -= u.max_wp() * c.ORDINARY
            ap -= 3
            u.brace = True
        return ap

    def attack(self, u, t, ap):
        foes = alive(self.foes(u))
        around = [f for f in foes if cheb(u, f) == 1]
        if self.r.sweep_skill and u.skill >= self.r.sweep_skill and len(around) >= 2 and ap >= u.weapon.ap + SWEEP_AP:
            for f in around:
                self.strike(u, f, mult=self.r.sweep_mult)
            return u.weapon.ap + SWEEP_AP
        if self.r.stun and u.skill >= STUN_SKILL and ap >= u.weapon.ap + STUN_AP and not t.skip and not t.asleep:
            if self.strike(u, t) and t.hp > 0 and random.random() < self.r.stun:
                t.skip = 1
            return u.weapon.ap + STUN_AP
        if "crush" in u.abilities and not u.weapon.ranged and u.can_spend(c.POWERFUL):
            u.wp -= u.max_wp() * c.POWERFUL
            if not self.strike(u, t, mult=1.5, unguarded=True):
                u.exposed = True
            return u.weapon.ap + 1
        self.strike(u, t)
        if self.r.cleave and u.weapon.name == "two-hand":
            rest = [f for f in foes if f is not t and f.hp > 0 and cheb(u, f) == 1]
            if rest:
                self.strike(u, random.choice(rest), mult=0.5)
        return u.weapon.ap

    def turn(self, u, rnd):
        u.brace = False
        if u.skip or u.asleep:
            u.skip = max(0, u.skip - 1)
            u.asleep = max(0, u.asleep - 1)
            u.reserve = 0
            return
        ap = self.control(u, u.ap())
        if u.hp <= 0:
            return
        cost = u.weapon.ap
        first = True
        while True:
            foes = alive(self.foes(u))
            if not foes or ap < 1:
                break
            if u.intent not in foes or first:
                u.intent = self.best(u, foes, ap)
            t = u.intent
            adjacent = [f for f in foes if cheb(u, f) == 1]
            if u.weapon.ranged:
                if cheb(u, t) <= BOW_RANGE:
                    if ap < cost:
                        break
                    self.strike(u, t)
                    ap -= cost
                else:
                    left = self.step(u, (t.x, t.y), ap)
                    if left is None:
                        break
                    ap = left
                first = False
                continue
            if u.conducting and t not in adjacent:
                adjacent_t = self.best(u, adjacent, ap) if adjacent else None
                if adjacent_t is None or ap < cost:
                    break
                ap -= self.attack(u, adjacent_t, ap)
                continue
            if t in adjacent:
                if ap < cost:
                    break
                ap -= self.attack(u, t, ap)
                first = False
                continue
            need = cheb(u, t) - 1
            if adjacent:
                near = self.best(u, adjacent, ap)
                reach_it = ap - need - self.r.escape_ap >= cost
                worth_it = first and self.score(u, t, ap) > BREAK_OFF_FACTOR * self.score(u, near, ap)
                if not (reach_it or worth_it):
                    if ap < cost:
                        break
                    ap -= self.attack(u, near, ap)
                    first = False
                    continue
            left = self.step(u, (t.x, t.y), ap)
            if left is None:
                if adjacent and ap >= cost:
                    ap -= self.attack(u, self.best(u, adjacent, ap), ap)
                    first = False
                    continue
                break
            ap = left
            first = False
        u.reserve = max(0, ap)

    def strength(self, side, leader):
        total = sum(max(0, u.hp) / u.max_hp() * c.LIMIT_S[u.limit] ** 2 for u in side)
        return total * (self.r.leader_weight if leader is not None and leader.hp <= 0 else 1.0)

    def run(self, max_rounds):
        order = sorted(self.a + self.b, key=lambda u: -u.initiative())
        for rnd in range(1, max_rounds + 1):
            for u in order:
                if u.hp <= 0:
                    continue
                if self.objective is not None and self.objective.hp <= 0:
                    return rnd, self.a
                self.turn(u, rnd)
            if self.objective is not None and self.objective.hp <= 0:
                return rnd, self.a
            if not alive(self.a):
                return rnd, self.b
            if not alive(self.b):
                return rnd, self.a
            if self.r.morale:
                sa, sb = self.strength(self.a, self.leaders[0]), self.strength(self.b, self.leaders[1])
                if self.objective is None and sb < self.r.morale * sa:
                    return rnd, self.a
                if sa < self.r.morale * sb:
                    return rnd, self.b
        return max_rounds, None


def cheb_xy(x, y, goal):
    return max(abs(x - goal[0]), abs(y - goal[1]))


def dist2(x, y, goal):
    return (x - goal[0]) ** 2 + (y - goal[1]) ** 2


def stronger(units, bonus):
    for u in units:
        u.str += bonus
        u.end += bonus
        u.dex += bonus
        u.per += bonus
        u.skill += bonus
    return units


def main_force(rules, size=None):
    n = (random.choice(rules.forces) if size is None else size) - 3
    units = [c.follower9() for _ in range(n)] + [c.follower8(), c.follower8(), c.senior7()]
    if rules.shift > 0:
        units[n - 1] = c.follower8()
    elif rules.shift < 0:
        units[n] = c.follower9()
    if rules.ranged:
        units[0].weapon = c.CROSSBOW
    return stronger(units, rules.enemy_bonus)


def allies():
    return [c.ally9(), c.ally9(), c.ally8(), c.ally8(), c.ally8()]


def archers():
    units = allies()
    units[0].weapon = units[1].weapon = c.BOW
    return units


def chain(rules, side, size=None, runs=None):
    wins = 0
    for _ in range(runs or RUNS):
        a = side()
        rounds, winner = Fight(a, main_force(rules, size), rules).run(55)
        if winner is not a:
            continue
        start = max(rounds + 5, random.randint(15, 20))
        survivors = alive(a)
        senior, one, two = stronger([c.follower8(), c.follower9(), c.follower9()], rules.enemy_bonus)
        for u in (senior, one, two):
            u.conducting = True
        winner = Fight(survivors, [senior, one, two], rules, objective=senior, wounded_a=True).run(60 - start)[1]
        wins += winner is survivors
    return wins / (runs or RUNS) * 100


def duel(rules, a_fn, b_fn, runs=None):
    wins = 0
    for _ in range(runs or RUNS):
        a = a_fn()
        wins += Fight(a, b_fn(), rules).run(40)[1] is a
    return wins / (runs or RUNS) * 100


V0 = Rules(width=4, flasks=1, carriers=2, flask_damage=20.0, enemy_bonus=1, ranged=True)
NO_FLASK = replace(V0, flasks=0)
PLAIN = Rules()
LEVERS = {
    "no space (combat_sim.py): base 38, +club +31, +L9 +32, +L8 +50": None,
    "cells, target choice, no flask": NO_FLASK,
    "wide street, 8 cells": replace(NO_FLASK, width=8),
    "lane, 3 cells": replace(NO_FLASK, width=3),
    "nearest target, both sides": replace(NO_FLASK, targeting="nearest"),
    "random target, both sides": replace(NO_FLASK, targeting="random"),
    "free to leave a fight": replace(NO_FLASK, escape_ap=0),
    "flat armour": replace(NO_FLASK, armour="flat"),
    "skill difference ×3": replace(NO_FLASK, skill_weight=3.0),
    "damage ×1.5": replace(NO_FLASK, damage_scale=1.5),
    "damage ×2": replace(NO_FLASK, damage_scale=2.0),
    "critical chance +30": replace(NO_FLASK, crit_bonus=0.30),
    "morale and leader": replace(NO_FLASK, morale=0.5, leader_weight=0.7),
    "stun on 30 %": replace(NO_FLASK, stun=0.3),
    "sweep at skill 10": replace(NO_FLASK, sweep_skill=10),
    "cleave, two-handed": replace(NO_FLASK, cleave=True),
    "one carrier a side, flask 20": replace(V0, carriers=1),
    "two carriers, flask 15": replace(V0, flask_damage=15.0),
    "two carriers, flask 20 (V0)": V0,
    "two carriers, flask 20, cult +2": replace(V0, enemy_bonus=2),
    "all of Limit 8 and deeper, flask 20": replace(V0, carriers=0),
}


def side():
    return archers()


def carrying(u, flasks):
    u.extra["flasks"] = flasks
    return u


def without_abilities(u):
    u.abilities = ()
    return u


def bowman(u):
    u.weapon = c.BOW
    return u


def changed(attribute, n=3):
    units = side()
    for u in units:
        setattr(u, attribute, getattr(u, attribute) + n)
    return units


def equipped(weapon=None, armour=None, only=""):
    units = side()
    for u in units:
        if u.name.startswith(only):
            u.weapon = weapon or u.weapon
            u.armour = armour or u.armour
    return units


def plain():
    return c.Unit("p", 9, 6, 6, 4, 4, 5, will=4, weapon=c.ONE_HAND, armour="cloth")


def plains(n):
    return [plain() for _ in range(n)]


SIDES = {
    "allies alone": side,
    "+ Limit 8, whole": lambda: side() + [c.ally8()],
    "+ Limit 8 with a third flask": lambda: side() + [carrying(c.ally8(), 1)],
    "+ Limit 8, no Дрёма": lambda: side() + [without_abilities(c.ally8())],
    "+ Limit 9": lambda: side() + [c.ally9()],
    "+ Limit 9 with a bow": lambda: side() + [bowman(c.ally9())],
    "+ club": lambda: side() + [c.new_character()],
    "+ club and a flask": lambda: side() + [carrying(c.new_character(), 1)],
    "+ hero of Limit 9": lambda: side() + [c.hero9()],
    "+ hero of Limit 9, 2 flasks": lambda: side() + [carrying(c.hero9(), 2)],
    "+ hero of Limit 8": lambda: side() + [c.hero()],
    "+ hero of Limit 8, 2 flasks": lambda: side() + [carrying(c.hero(), 2)],
    "allies in chain": lambda: equipped(armour="chain"),
    "allies in leather": lambda: equipped(armour="leather"),
    "Limit 8 with two-handed": lambda: equipped(c.TWO_HAND, only="A8"),
    "Limit 8 with shield": lambda: equipped(c.SHIELDED, only="A8"),
    "Limit 9 with crossbows": lambda: equipped(c.CROSSBOW, only="A9"),
    "Limit 9 in melee, no bows": allies,
    "+3 Strength": lambda: changed("str"),
    "+3 Endurance": lambda: changed("end"),
    "+3 Dexterity": lambda: changed("dex"),
    "+3 Perception": lambda: changed("per"),
    "+3 weapon skill": lambda: changed("skill"),
    "+3 Will": lambda: changed("will"),
    "+3 Intelligence": lambda: changed("intl"),
}
DUELS = {
    "5 v 5, Limit 9, no items": (lambda: plains(5), lambda: plains(5), PLAIN),
    "5 v 6, Limit 9, no items": (lambda: plains(5), lambda: plains(6), PLAIN),
    "10 v 11, Limit 9, no items": (lambda: plains(10), lambda: plains(11), PLAIN),
    "Limit 8 v Limit 9": (lambda: [c.follower8()], lambda: [c.follower9()], V0),
    "Limit 8 v 2 Limit 9": (lambda: [c.follower8()], lambda: [c.follower9(), c.follower9()], V0),
    "Limit 8 v 3 Limit 9": (lambda: [c.follower8()], lambda: [c.follower9() for _ in range(3)], V0),
    "Limit 7 v 2 Limit 8": (lambda: [c.senior7()], lambda: [c.follower8(), c.follower8()], V0),
    "Limit 7 v 3 Limit 9": (lambda: [c.senior7()], lambda: [c.follower9() for _ in range(3)], V0),
}


def seeded(name):
    random.seed(zlib.crc32(name.encode()))


def lever(name):
    seeded(name)
    rules = LEVERS[name]
    if rules is None:
        return name
    base = chain(rules, side)
    club = chain(rules, lambda: side() + [c.new_character()])
    l9 = chain(rules, lambda: side() + [c.ally9()])
    l8 = chain(rules, lambda: side() + [c.ally8()])
    return f"{name:<36} base {base:3.0f}  +club {club - base:+3.0f}  +L9 {l9 - base:+3.0f}  +L8 {l8 - base:+3.0f}"


def fielded(name):
    seeded(name)
    return f"{name:<30} {chain(V0, SIDES[name]):5.1f}"


def forced(size):
    seeded(str(size))
    return f"main force of {size:<16} {chain(V0, side, size):5.1f}"


def shifted(shift):
    seeded(f"shift{shift}")
    return f"one enemy {'a Limit stronger' if shift > 0 else 'a Limit weaker':<20} {chain(replace(V0, shift=shift), side):5.1f}"


def careless(name):
    seeded(name)
    return f"{name:<30} {chain(replace(V0, a_targeting=name.split()[-1], a_flask_min=1), side):5.1f}"


def duelled(name):
    seeded(name)
    a, b, rules = DUELS[name]
    return f"{name:<30} {duel(rules, a, b):5.1f}"


def first_throw(_):
    seeded("first")
    wins = rounds = thrown = won = 0
    for _ in range(RUNS):
        a = side()
        f = Fight(a, main_force(V0), V0)
        r, w = f.run(55)
        rounds += r
        wins += w is a
        thrown += f.first_throw is not None
        won += f.first_throw is not None and w is f.first_throw
    return f"main battle {wins / RUNS * 100:.1f} %, {rounds / RUNS:.1f} rounds; the side that throws first wins {won / max(1, thrown) * 100:.1f} %"


def main():
    with Pool() as pool:
        print("Levers against one more fighter: points of the whole chain")
        print("\n".join(pool.map(lever, LEVERS)))
        print("\nV0: what stops the ritual")
        print("\n".join(pool.map(fielded, SIDES)))
        print("\n".join(pool.map(forced, (5, 6, 7))))
        print("\n".join(pool.map(shifted, (-1, 1))))
        print("\nV0: allies who play badly, enemies who play well")
        print("\n".join(pool.map(careless, ("careless nearest", "careless random"))))
        print("\nFirst throw")
        print("\n".join(pool.map(first_throw, (0,))))
        print("\nSmall fights, first side wins")
        print("\n".join(pool.map(duelled, DUELS)))


if __name__ == "__main__":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", line_buffering=True)
    main()

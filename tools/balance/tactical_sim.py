"""Tactical check of 10.2 Combat System Draft, on a Combat Grid.

combat_sim.py has no space: anyone can hit anyone, so a fight is a race of total
damage against total HP, and one more body decides it. This model puts the same
fighters on cells. Melee fighters must walk and strike only across a side of a cell,
never across a corner. Whoever leaves a cell beside an enemy with a melee weapon takes
its Reaction Attack, a fighter with two or more enemies beside it is Surrounded, and
every fighter picks targets by impact: how much harm the target does to its side,
against how fast this fighter can bring it down. Fighters can wait to the end of the
round, hold back from an enemy's reach, and stand in a Defensive Stance that answers
an attacker with a Counterattack. A shot has no separate Reload and costs more than a
blow, a shooter steps back with the AP its shot leaves, armour counts more against a
shot from beyond the next cell, a shooter with an enemy beside it aims worse, and a blow
can be filled with Will Power. A fighter may carry a second Weapon Set and change to it.

The ritual of 21 §4 is a circle: its Limit 9 conductors never answer, a Limit 8 senior leads the
guards and fights, a guard with no enemy beside it takes an empty place, and a circle short of
hands for 10 rounds has failed. circle_first makes the attackers go for the conductors.

V0 is the configuration 10.2 reports, before items. The other switches of Rules
are the levers tried against headcount, kept so that the table of levers can be
repeated. item_sim.py dresses these fighters through Rules.outfit.

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
SPELL_RANGE = 6
ORDER_RANGE = 8  # 05.1 Стой!, 4 cells until the balance after COMBAT-7: an order heard at firearms' distance
SUPPORT_RANGE = 2
ESCAPE_AP = 0
REACH_DISCOUNT = 0.7
KILL_BONUS = 2.5
ASLEEP_FACTOR = 0.2
CROWD_FACTOR = 0.5
BREAK_OFF_FACTOR = 1.5
SWAP_AP = 2
SURROUND_HIT = c.SURROUND_HIT
OBJECTIVE_BONUS = 3.0
STING_AP = 4
STING_BASE = 6  # 10.2 §5f: 9 with the old Intelligence factor taken in, 6 since the balance after COMBAT-7
STING_RANGE = 12
DISORIENT_HIT = 0.10
PILLAR_AP = 4
WAKE_AP = 6
SLEEP_AP = c.SLEEP_AP
ORDER_AP = 4
BRACE_AP = 2
ORDER_BY_WILL = False
SLEEP_COST = c.POWERFUL
SLEEP_CONTEST = 0.40
ORDER_COST = c.ORDINARY
STUN_AP = 2
STUN_SKILL = 5
SWEEP_AP = 2
DEFENCE_EVASION = 0.015
DEFENCE_GUARD = 0.03
COUNTER_AP = 2
# The ritual of 21 §4: conductors stand round the circle and never answer; a guard with no enemy
# beside it takes an empty place, and a circle short of hands for SHORT_TICKS rounds has failed.
RITUAL_TICKS = 60
SHORT_TICKS = 10
CONTROL_VALUE = {"sleep": 6.0, "stand": 3.0, "crush": 3.0, "support": 2.0, "sting": 3.0, "pillar": 3.0,
                 "screen": 2.0, "double": 2.0, "press": 3.0}
# 05.1's four new abilities (CR5), their numbers from 05.1's table. A cloud of Пелена is a Partial Obstacle: a targeted
# attack through it or at someone in it is Obscured, once, but a shot from inside goes out unhindered (COMBAT-12). The
# balance after COMBAT-7 (10.2 §5f) took Пелена from 3 rounds to 2, Двойник from 3 AP to 4 and Гнёт's base from 4 to 2.
SCREEN_AP, SCREEN_RANGE, SCREEN_ROUNDS = 3, 8, 2
OBSCURED = 0.20
DOUBLE_AP = 4
DASH_AP, DASH_RANGE = 3, 6
PRESS_AP, PRESS_RANGE, PRESS_BASE = 5, 10, 2
# How NPCs use them (authoring defaults, the game's NPCs the same): Двойник from the AP a turn leaves, the AP kept for
# it below this share of HP; Гнёт where the enemies it strikes, less this weight for each of its own, come to PRESS_NET.
DOUBLE_HP = 0.5
PRESS_FRIEND = 1.5
PRESS_NET = 2.0


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
    morale: float = 0.0
    leader_weight: float = 1.0
    enemy_bonus: int = 0
    damage_scale: float = 1.0
    crit_bonus: float = 0.0
    stun: float = 0.0
    # The main force: 4, 5, or 6 in 20, 40, and 40 % of runs; the ritual group stands apart (21 §4).
    forces: tuple = (4, 5, 5, 6, 6)
    places: int = 3
    guards: tuple = (2,)
    guard_limit: int = 9
    circle_first: bool = False
    ranged: bool = False
    shift: int = 0
    sweep_skill: int = 0
    sweep_mult: float = 1.0
    outfit: object = None
    crit: float = c.CRIT
    wait: bool = True
    hold: bool = True
    turtle: bool = False
    stance_evasion: float = DEFENCE_EVASION
    stance_guard: float = DEFENCE_GUARD
    counter_ap: int = COUNTER_AP
    ranged_armour: float = c.ARMOUR_RANGED
    point_blank: float = c.ADJACENT
    infusion_cost: float = c.INFUSION_COST
    infusion_share: float = c.INFUSION_SHARE
    # 10.2 §5f: each person on a shot's line takes it in this share of shots, nearest first; 0 turns it off.
    intercept: float = 0.0
    shield_cover: float = 0.0  # 10.2 §5f: a held shield hinders shots and targeted spells at its carrier, as cover
    cover: float = 0.0
    crit_pierce: float = c.CRIT_PIERCE
    obscured: float = OBSCURED
    press_base: float = PRESS_BASE
    order_range: int = ORDER_RANGE
    sting_base: float = STING_BASE
    screen_rounds: int = SCREEN_ROUNDS
    double_ap: int = DOUBLE_AP
    support: float = c.SUPPORT_DEFENCE
    screen_cost: float = c.ORDINARY
    screen_ap: int = SCREEN_AP
    double_hp: float = DOUBLE_HP
    reaction: bool = True
    room: int = 10


def cheb(a, b):
    return max(abs(a.x - b.x), abs(a.y - b.y))


def touch(a, b):
    """Side by side: a melee blow crosses a side of a cell, never a corner."""
    return abs(a.x - b.x) + abs(a.y - b.y) == 1


def steps_to(a, b):
    """Steps, diagonal ones included, until a stands side by side with b."""
    dx, dy = abs(a.x - b.x), abs(a.y - b.y)
    return max(max(dx, dy) - 1, min(dx, dy))


def alive(side):
    return [u for u in side if u.hp > 0]


class Fight:
    def __init__(self, a, b, rules, objective=None, wounded_a=False, ritual=0):
        """ritual: the ticks the circle still needs, held by side b's conductors. 0 is an ordinary fight."""
        self.a, self.b, self.r, self.objective = a, b, rules, objective
        self.progress, self.short = ritual, 0
        self.per_column = (rules.width + rules.spacing - 1) // rules.spacing
        depth = (len(a + b) // self.per_column + 1) * rules.spacing + rules.room
        self.front = depth
        self.length = 2 * depth + rules.gap + 1
        self.cells = {}
        self.clouds = []
        for u in (b if wounded_a else a + b):
            u.reset()
        for side, left in ((a, True), (b, False)):
            self.place(side, left)
        for u in a + b:
            u.intent = None
            u.skip = u.asleep = 0
            u.stance = False
            u.held = -1
            u.fired = False
            u.reacted = False
            u.disoriented, u.dazed, u.taunter = 0, False, None
            u.veiled, u.copy = 0, False
            u.threat = self.threat(u)
        self.leaders = [self.leader(a), self.leader(b)]
        self.circle = [(u.x, u.y) for u in b if u.conducting] if ritual else []

    def leader(self, side):
        ranks = sorted(side, key=lambda u: 9 if u.limit is None else u.limit)
        return ranks[0] if len(ranks) < 2 or ranks[0].limit != ranks[1].limit else None

    def place(self, side, left):
        front = [u for u in side if (u.opener or not u.weapon.ranged) and "sleep" not in u.abilities and not u.conducting]
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
        return max(1, int((10 + u.dex / 20 - u.burden[1]) / u.weapon.ap))

    def expected_blow(self, a, t):
        w = a.weapon
        base = w.base * c.stage_factor(w.stage) * self.r.damage_scale
        raw = base * (1 + a.gear.damage)
        share = a.gear.magic + (self.infusion(a) if a.infuse else 0)
        return self.chance(a, t, -self.obscured(a, t)) * a.aim() * (self.reduce(raw, t, w.ranged and cheb(a, t) > 1) + self.warded(share * base, t))

    def seen(self, foes):
        """05.1 Двойник: an enemy unseen with no copy left is not targeted; one whose copy stands is taken for it."""
        return [f for f in foes if not f.veiled or f.copy]

    def clouded(self, x, y):
        return [k for k in self.clouds if abs(x - k[0]) <= 1 and abs(y - k[1]) <= 1]

    def obscured(self, a, t):
        """10.1 Obscured, once: a cell of the line past a, t's own included, in a cloud a does not stand in (COMBAT-12)."""
        if not self.clouds:
            return 0.0
        own = self.clouded(a.x, a.y)
        n = max(abs(t.x - a.x), abs(t.y - a.y))
        for i in range(1, n + 1):
            x, y = round(a.x + (t.x - a.x) * i / n), round(a.y + (t.y - a.y) * i / n)
            if any(all(k is not o for o in own) for k in self.clouded(x, y)):
                return self.r.obscured
        return 0.0

    def unveil(self, u):
        u.veiled, u.copy = 0, False

    def threatened(self, u):
        """Someone awake on the other side could attack u in its next turn."""
        return any(cheb(u, f) <= (f.weapon.range if f.weapon.ranged else 1) + 6 for f in alive(self.foes(u)) if not f.asleep and not f.skip)

    def threat(self, u):
        foes = self.foes(u)
        harm = self.blows(u) * sum(self.expected_blow(u, f) for f in foes) / max(1, len(foes))
        return harm + sum(CONTROL_VALUE.get(x, 0.0) for x in u.abilities)

    def chance(self, a, t, bonus):
        p = 0.70 + self.r.skill_weight * (a.accuracy() - t.evasion()) / 100 + bonus - (DISORIENT_HIT if a.dazed else 0.0)
        return min(0.95, max(0.05, p * (1 + a.luck / 100)))

    def defence(self, t):
        d = c.ARMOUR[t.armour][0] * c.stage_factor(t.armour_stage) * t.armour_share() + t.gear.defence
        if any("support" in u.abilities and u is not t and cheb(u, t) <= SUPPORT_RANGE for u in alive(self.own(t))):
            d += self.r.support
        return d

    def warded(self, magic, t):
        return magic * (1 - c.mitigation(c.ARMOUR[t.armour][1] * c.stage_factor(t.armour_stage) * t.armour_share() + t.gear.ward))

    def infusion(self, a):
        return self.r.infusion_share * c.INFUSION_ARMOUR.get(a.armour, 1.0)

    def infused(self, a):
        """The share of a weapon's base a blow adds as magical damage. Pouring Will Power in stops short of exhaustion."""
        share = a.gear.magic
        cost = self.r.infusion_cost * c.LIMIT_S[a.limit]
        if a.infuse and a.wp >= 2 * cost:
            a.wp -= cost
            share += self.infusion(a)
        return share

    def reduce(self, raw, t, ranged=False, pierce=0.0):
        d = self.defence(t) * (self.r.ranged_armour if ranged else 1) * (1 - pierce)
        if self.r.armour == "flat":
            return max(raw * self.r.flat_floor, raw - d / self.r.flat_divisor)
        return raw * (1 - c.mitigation(d))

    def surrounded(self, t):
        """How many awake enemies stand side by side with t."""
        return sum(1 for b in alive(self.foes(t)) if touch(b, t) and not b.asleep and not b.skip and not b.conducting)

    def strike(self, a, t, bonus_crit=0.0, crit_extra=0.0, mult=1.0, unguarded=False, counter=False):
        hit = self.landed(a, t, bonus_crit, crit_extra, mult, unguarded)
        price = t.weapon.ap + self.r.counter_ap
        if t.stance and t.hp > 0 and a.hp > 0 and not counter and not a.weapon.ranged and not t.weapon.ranged and t.reserve >= price:
            t.reserve -= price
            self.strike(t, a, counter=True)
        return hit

    def landed(self, a, t, bonus_crit, crit_extra, mult, unguarded, reaction=False):
        if a.weapon.ranged and self.r.intercept and cheb(a, t) > 1:
            t = self.interceptor(a, t)
        share = a.gear.magic if reaction else self.infused(a)
        if t.veiled and t.copy:
            # 05.1 Двойник: the blow meets the copy, which breaks at its first hit and harms nobody.
            if random.random() < self.chance(a, t, -self.obscured(a, t)) * a.aim():
                t.copy = False
                return True
            return False
        bonus_hit = SURROUND_HIT.get(self.surrounded(t), 0.0) - self.obscured(a, t)
        if t.exposed:
            bonus_hit += 0.10
            t.exposed = False
        if a.weapon.ranged and any(touch(a, f) for f in alive(self.foes(a))):
            bonus_hit -= self.r.point_blank
        if a.weapon.ranged and cheb(a, t) > 1:
            bonus_hit -= self.r.cover + self.shielded(t)
        if t.stance:
            bonus_hit -= self.r.stance_evasion * t.reserve
        if random.random() >= self.chance(a, t, bonus_hit) * a.aim():
            return False
        raw = a.crit_raw() + bonus_crit + self.r.crit_bonus
        crit = self.r.crit + max(0, raw - 0.95) + crit_extra + a.gear.crit_damage if random.random() < min(0.95, raw) else 1.0
        w = a.weapon
        magic = share * w.base * c.stage_factor(w.stage) * crit * mult * self.r.damage_scale
        pierce = self.r.crit_pierce if crit > 1 and not w.ranged else 0.0
        physical = self.reduce(a.damage_roll() * crit * mult * self.r.damage_scale, t, w.ranged and cheb(a, t) > 1, pierce)
        if t.brace:
            physical *= 1 - c.BRACE_REDUCTION  # 05.1 Не сдвинуть lowers the physical damage only, as the game does (CR6)
        damage = physical + self.warded(magic, t)
        if not unguarded and not a.weapon.ranged and t.weapon.guard and t.reserve >= c.REACTION_AP and not t.asleep and not t.skip and not t.conducting:
            guard = t.weapon.guard_chance * (1 + self.r.stance_guard * t.reserve if t.stance else 1)
            t.reserve -= c.REACTION_AP
            if random.random() < guard:
                damage *= 1 - t.weapon.guard_absorb
        t.hp -= damage
        if damage > 0:
            self.unveil(a)
        t.asleep = 0
        if t.hp <= 0:
            self.cells.pop((t.x, t.y), None)
        elif w.stun and random.random() < w.stun:
            t.skip = max(t.skip, 1)
        return True

    def line(self, a, t, at=None):
        """The people standing on the cells between a (or the cell at) and t, nearest first (10.1 Shot Line)."""
        x0, y0 = at if at is not None else (a.x, a.y)
        n = max(abs(t.x - x0), abs(t.y - y0))
        people = []
        for i in range(1, n):
            cell = (round(x0 + (t.x - x0) * i / n), round(y0 + (t.y - y0) * i / n))
            u = self.cells.get(cell)
            if u is not None and u is not a and u is not t and u.hp > 0 and u not in people:
                people.append(u)
        return people

    def shielded(self, t):
        return self.r.shield_cover if t.weapon.name.endswith("and shield") else 0.0

    def friends_on_line(self, u, t, at=None):
        own = self.own(u)
        return [b for b in self.line(u, t, at) if b in own]

    def clear_step(self, u, t, ap):
        """A shooter whose line to t has its own people on it steps to a free cell beside it with no friend on the
        line, in range and beside no enemy, when the AP still buy the shot (10.2 §5f). Returns the AP left, or None."""
        if ap < 1 + u.weapon.ap or not self.friends_on_line(u, t):
            return None
        foes = alive(self.foes(u))
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                x, y = u.x + dx, u.y + dy
                if not (dx or dy) or not (0 <= x < self.length and 0 <= y < self.r.width) or (x, y) in self.cells:
                    continue
                if max(abs(x - t.x), abs(y - t.y)) > u.weapon.range or any(max(abs(x - f.x), abs(y - f.y)) <= 1 for f in foes):
                    continue
                if self.friends_on_line(u, t, (x, y)):
                    continue
                left = [f for f in foes if touch(u, f)]
                del self.cells[(u.x, u.y)]
                u.x, u.y = x, y
                self.cells[(x, y)] = u
                return self.reactions(u, left, ap - 1)
        return None

    def interceptor(self, a, t):
        """10.1 Shot Line: each person on the line, nearest first, takes the shot in this share of shots before t is
        rolled; the shot is then rolled against whoever took it."""
        for u in self.line(a, t):
            if random.random() < self.r.intercept:
                return u
        return t

    def score(self, u, e, ap):
        targeting = self.r.a_targeting if u in self.a and self.r.a_targeting else self.r.targeting
        if targeting == "random":
            return random.random()
        reach = steps_to(u, e) if not u.weapon.ranged else max(0, cheb(u, e) - u.weapon.range)
        if targeting == "nearest":
            return -reach - e.hp / 1000
        turns = max(0, math.ceil((reach - ap) / 10))
        expected = self.blows(u) * self.expected_blow(u, e)
        value = (e.threat + 0.1) * min(1.0, expected / max(1.0, e.hp)) * REACH_DISCOUNT ** turns
        if turns == 0 and e.hp <= expected:
            value *= KILL_BONUS
        if e.asleep or e.skip:
            value *= ASLEEP_FACTOR
        if sum(1 for b in alive(self.own(u)) if b is not u and touch(b, e)) >= 3:
            value *= CROWD_FACTOR
        if u.weapon.ranged and self.r.intercept:
            value *= (1 - self.r.intercept) ** len(self.friends_on_line(u, e))
        if e is self.objective or (self.r.circle_first and u in self.a and e.conducting):
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
        left = [f for f in alive(self.foes(u)) if touch(u, f)]
        del self.cells[(u.x, u.y)]
        u.x, u.y = x, y
        self.cells[(x, y)] = u
        return self.reactions(u, left, ap - price)

    def reactions(self, u, left, ap):
        """Each enemy with a melee weapon whose side u has just left strikes once, free, once a round.
        The blow takes no Will Power, ability or second target; u may parry it with the AP it holds."""
        if not self.r.reaction:
            return ap
        for f in left:
            if f.hp > 0 and u.hp > 0 and not f.weapon.ranged and not f.reacted and not f.asleep and not f.skip and not f.conducting:
                f.reacted = True
                u.reserve = ap
                self.landed(f, u, 0.0, 0.0, 1.0, False, reaction=True)
                ap = u.reserve
        return ap

    def control(self, u, ap):
        foes = self.seen(alive(self.foes(u)))
        awake = [f for f in foes if not f.asleep and not f.skip]
        if "sleep" in u.abilities and len(awake) >= 2 and ap >= SLEEP_AP and u.can_spend(SLEEP_COST):
            near = [f for f in awake if cheb(u, f) <= SPELL_RANGE]
            if near:
                target = max(near, key=lambda f: f.threat)
                u.wp -= u.max_wp() * SLEEP_COST
                ap -= SLEEP_AP
                if c.resist_check(u, target, base=SLEEP_CONTEST):
                    target.asleep = c.SLEEP_ROUNDS
                else:
                    u.exposed = True
        if "stand" in u.abilities and ap >= ORDER_AP + u.weapon.ap and u.can_spend(ORDER_COST):
            near = [f for f in awake if not f.asleep and not f.skip and cheb(u, f) <= self.r.order_range]
            if near:
                target = max(near, key=lambda f: f.threat)
                u.wp -= u.max_wp() * ORDER_COST
                ap -= ORDER_AP
                if c.resist_check(u, target, ORDER_BY_WILL):
                    target.skip = 1
                else:
                    u.exposed = True
        if "press" in u.abilities and ap >= PRESS_AP and u.can_spend(c.POWERFUL):
            spot = self.press_spot(u, foes)
            if spot is not None:
                u.wp -= u.max_wp() * c.POWERFUL
                ap -= PRESS_AP
                self.press(u, spot)
        if "sting" in u.abilities and ap >= STING_AP and u.can_spend(c.ORDINARY):
            near = [f for f in foes if cheb(u, f) <= STING_RANGE]
            if near:
                u.wp -= u.max_wp() * c.ORDINARY
                ap -= STING_AP
                self.sting(u, self.best(u, near, ap))
        if "pillar" in u.abilities and ap >= PILLAR_AP + u.weapon.ap and u.can_spend(c.POWERFUL):
            around = [f for f in awake if touch(u, f)]
            if len(around) >= 2:
                u.wp -= u.max_wp() * c.POWERFUL
                ap -= PILLAR_AP
                for f in around:
                    if c.resist_check(u, f, ORDER_BY_WILL):
                        f.taunter = u
        return ap

    def sting(self, u, t):
        """Укол разума: a ranged magical attack. A hit disorients the target for its next turn, a miss the caster."""
        if self.r.intercept and cheb(u, t) > 1:
            t = self.interceptor(u, t)
        p = 0.70 + (u.magic_accuracy() - t.evasion()) / 100 - (self.r.cover + self.shielded(t) if cheb(u, t) > 1 else 0.0) - self.obscured(u, t)
        hit = random.random() < min(0.95, max(0.05, p * (1 + u.luck / 100)))
        if not hit:
            u.disoriented = 1
            return
        if t.veiled and t.copy:
            t.copy = False
            return
        self.unveil(u)
        raw = self.r.sting_base * self.r.damage_scale * random.uniform(0.85, 1.15)
        t.hp -= self.warded(raw, t)
        t.asleep = 0
        t.disoriented = 1
        if t.hp <= 0:
            self.cells.pop((t.x, t.y), None)

    def screen_spot(self, u):
        """05.1 Пелена: of the centres that cover the caster, the one that covers most of its own people not yet in a
        cloud, an enemy covered counting twice against it; None if every one covers an enemy as much."""
        own = [b for b in alive(self.own(u)) if not self.clouded(b.x, b.y)]
        foes = alive(self.foes(u))
        best, spot = 0, None
        for x, y in {(u.x + dx, u.y + dy) for dx in (-1, 0, 1) for dy in (-1, 0, 1)}:
            if not (0 <= x < self.length and 0 <= y < self.r.width):
                continue
            mine = sum(1 for b in own if abs(b.x - x) <= 1 and abs(b.y - y) <= 1)
            value = mine - 2 * sum(1 for f in foes if abs(f.x - x) <= 1 and abs(f.y - y) <= 1)
            if mine and value > best:
                best, spot = value, (x, y)
        return spot

    def press_spot(self, u, seen):
        """05.1 Гнёт: the centre within reach where the enemies struck, less PRESS_FRIEND for each of u's own, are most."""
        everyone = [f for f in alive(self.a + self.b) if f is not u]
        own = self.own(u)
        best, spot = PRESS_NET - 1e-9, None
        for x, y in {(f.x + dx, f.y + dy) for f in seen for dx in (-1, 0, 1) for dy in (-1, 0, 1)}:
            if not (0 <= x < self.length and 0 <= y < self.r.width) or cheb_xy(u.x, u.y, (x, y)) > PRESS_RANGE:
                continue
            value = sum((-PRESS_FRIEND if f in own else 1.0) for f in everyone if abs(f.x - x) <= 1 and abs(f.y - y) <= 1)
            if value > best:
                best, spot = value, (x, y)
        return spot

    def press(self, u, spot):
        """05.1 Гнёт: magical harm to everyone in the area but its caster, its own people too, with no roll and no
        critical hit; a copy in it breaks."""
        harmed = False
        for f in alive(self.a + self.b):
            if f is u or abs(f.x - spot[0]) > 1 or abs(f.y - spot[1]) > 1:
                continue
            f.copy = False
            damage = self.warded(self.r.press_base * self.r.damage_scale * random.uniform(0.85, 1.15), f)
            f.hp -= damage
            f.asleep = 0
            harmed = harmed or damage > 0
            if f.hp <= 0:
                self.cells.pop((f.x, f.y), None)
        if harmed:
            self.unveil(u)

    def dash(self, u, t, ap, cost):
        """05.1 Рывок: at once into a free cell beside t, past it as u sees it, with no walk and so no Reaction Attack;
        worth it when walking there costs 3 AP or more and a blow is still paid for."""
        if ("dash" not in u.abilities or u.weapon.ranged or touch(u, t) or cheb(u, t) > DASH_RANGE or steps_to(u, t) < 3
                or ap < DASH_AP + cost or not u.can_spend(c.ORDINARY)):
            return False
        dx, dy = (t.x > u.x) - (t.x < u.x), (t.y > u.y) - (t.y < u.y)
        options = ((dx, 0), (0, dy)) if dx and dy else ((dx, dy), (dy, dx), (-dy, -dx))
        for ox, oy in options:
            x, y = t.x + ox, t.y + oy
            if (ox or oy) and 0 <= x < self.length and 0 <= y < self.r.width and (x, y) not in self.cells:
                u.wp -= u.max_wp() * c.ORDINARY
                del self.cells[(u.x, u.y)]
                u.x, u.y = x, y
                self.cells[(x, y)] = u
                return True
        return False

    def screen(self, u, ap):
        """05.1 Пелена, from the AP a turn leaves: a cloud over the caster, where it ends its turn, and over as many of its
        own as it can cover, when a shooter on the other side could fire at it and no cloud covers it yet."""
        if "screen" not in u.abilities or ap < self.r.screen_ap or not u.can_spend(self.r.screen_cost) or self.clouded(u.x, u.y):
            return ap
        if not any(f.weapon.ranged and cheb(u, f) <= f.weapon.range + 4 for f in alive(self.foes(u)) if not f.asleep and not f.skip):
            return ap
        spot = self.screen_spot(u)
        if spot is None:
            return ap
        u.wp -= u.max_wp() * self.r.screen_cost
        self.clouds.append([spot[0], spot[1], self.r.screen_rounds])
        return ap - self.r.screen_ap

    def double(self, u, ap):
        """05.1 Двойник, from the AP a turn leaves: unseen until its next turn, its copy taking the first hit."""
        if "double" in u.abilities and ap >= self.r.double_ap and not u.veiled and u.can_spend(c.POWERFUL) and self.threatened(u):
            u.wp -= u.max_wp() * c.POWERFUL
            u.veiled, u.copy = 1, True
            return ap - self.r.double_ap
        return ap

    def keeps_for_double(self, u):
        return (self.r.double_ap if "double" in u.abilities and u.hp <= self.r.double_hp * u.max_hp() and u.can_spend(c.POWERFUL)
                and self.threatened(u) else 0)

    def attack(self, u, t, ap):
        u.struck = True
        foes = alive(self.foes(u))
        around = [f for f in foes if touch(u, f)]
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
        if u.weapon.off:
            return self.pair(u, t, ap)
        self.strike(u, t)
        if u.weapon.cleave:
            # The sweep reaches a cell beside the target, across the attacker's corner.
            rest = [f for f in foes if f is not t and f.hp > 0 and touch(f, t) and cheb(u, f) == 1 and not touch(u, f)]
            if rest:
                self.strike(u, random.choice(rest), mult=u.weapon.cleave)
        return u.weapon.ap

    def cost(self, u):
        """The AP of the cheapest blow open to u: its weapon's, or the lighter of two."""
        w = u.weapon
        return min(w.ap, w.off.ap) if w.off else w.ap

    def pair(self, u, t, ap):
        """A blow with two weapons, with the hand that opens the mix of blows the AP left can buy that does most harm."""
        main = u.weapon
        worth = {}
        for h in (main, main.off):
            u.weapon = h
            worth[id(h)] = self.expected_blow(u, t)
        u.weapon = main

        def best(left):
            options = [(worth[id(h)] + best(left - h.ap)[0], h) for h in (main, main.off) if h.ap <= left]
            return max(options, key=lambda o: o[0]) if options else (0.0, None)

        hand = best(ap)[1] or min((main, main.off), key=lambda h: h.ap)
        u.weapon = hand
        try:
            self.strike(u, t)
        finally:
            u.weapon = main
        return hand.ap

    def full_ap(self, u):
        return math.floor(10 + u.dex / 20 + u.gear.ap - u.burden[1])

    def waits(self, u, later):
        """Wait if nothing is in reach now, and an enemy still to act will walk into reach."""
        if not self.r.wait or u.weapon.ranged or u.conducting or u.skip or u.asleep:
            return False
        reach = self.full_ap(u) - u.weapon.ap
        if any(steps_to(u, f) <= reach for f in alive(self.foes(u))):
            return False
        return any(not f.weapon.ranged and steps_to(u, f) - self.full_ap(f) <= reach for f in later if f.hp > 0 and f in self.foes(u))

    def holds(self, u, ap):
        """Nothing to strike this turn: do not walk into the reach of whoever still has to act."""
        foes = alive(self.foes(u))
        if not self.r.hold or u.conducting or u.struck or min(steps_to(u, f) for f in foes) + u.weapon.ap <= ap:
            return False
        return any(f in self.pending and not f.weapon.ranged and steps_to(u, f) - 1 <= self.full_ap(f) - f.weapon.ap for f in foes)

    def changes_set(self, u, foes, ap=10):
        """A firearm gives way to the melee set: after its first shot for one carried to open a fight, and, by the
        combat rework (10.2 §5f), for a shooter once an enemy stands beside it only if the melee set does more harm
        over the AP left, the change paid, than shooting point blank. A shooter takes the firearm back when free."""
        if u.second is None:
            return False
        beside = [f for f in foes if touch(u, f) and not f.asleep and not f.skip]
        if u.weapon.ranged and not u.second.ranged:
            if u.opener:
                return u.fired
            if not beside:
                return False
            t = max(beside, key=lambda f: f.threat)
            shots = ap // u.weapon.ap * self.expected_blow(u, t) * self.point_blank_share(u, t)
            main, u.weapon = u.weapon, u.second
            blows = max(0, ap - SWAP_AP) // u.weapon.ap * self.expected_blow(u, t)
            u.weapon = main
            return blows > shots
        return not u.weapon.ranged and u.second.ranged and not u.opener and not beside

    def point_blank_share(self, u, t):
        """How much of a shot's expected harm stays at point blank: the aim lost, armour plain and not doubled."""
        far = self.chance(u, t, 0.0)
        near = self.chance(u, t, -self.r.point_blank)
        w = u.weapon
        raw = w.base * c.stage_factor(w.stage) * self.r.damage_scale * (1 + u.gear.damage)
        doubled = self.reduce(raw, t, True)
        plain = self.reduce(raw, t, False)
        return near / max(1e-9, far) * plain / max(1e-9, doubled)

    def stand(self, u, ap):
        """Defence: no attack this turn, and the AP left pay for it until the next one."""
        u.stance = True
        u.reserve = ap

    def turn(self, u, rnd):
        self.pending.discard(u)
        self.unveil(u)  # 05.1 Двойник: seen again in its next turn, which is all the same here as at its end
        u.dazed, u.disoriented = u.disoriented > 0, max(0, u.disoriented - 1)
        taunter, u.taunter = u.taunter, None
        u.brace = u.reacted = False
        u.stance = u.struck = False
        if u.conducting:
            u.reserve = 0
            return
        if self.joins_circle(u):
            return
        if u.skip or u.asleep:
            u.skip = max(0, u.skip - 1)
            u.asleep = max(0, u.asleep - 1)
            u.reserve = 0
            u.held = -1
            return
        pool = u.ap() if u.held < 0 else min(u.held, u.reserve)
        u.held = -1
        ap = self.control(u, pool)
        if u.hp <= 0:
            return
        if self.r.turtle and u in self.a and any(touch(u, f) for f in alive(self.foes(u))):
            self.stand(u, ap)
            return
        keep = self.keeps_for_double(u)
        ap -= keep
        first = True
        while True:
            foes = alive(self.foes(u))
            seen = self.seen(foes)
            if not seen or ap < 1 or u.hp <= 0:
                break
            if self.changes_set(u, foes, ap):
                if ap < SWAP_AP:
                    break
                ap -= SWAP_AP
                u.weapon, u.second = u.second, u.weapon
                continue
            cost = self.cost(u)
            if u.intent not in seen or first:
                u.intent = self.best(u, seen, ap)
            if taunter in seen:
                u.intent = taunter
            sleeper = next((b for b in alive(self.own(u)) if b.asleep and cheb(u, b) == 1), None) if WAKE_AP else None
            if sleeper and ap >= WAKE_AP:
                sleeper.asleep = 0
                ap -= WAKE_AP
                continue
            t = u.intent
            adjacent = [f for f in seen if touch(u, f)]
            if u.weapon.ranged:
                if cheb(u, t) <= u.weapon.range:
                    if self.r.intercept and first:
                        stepped = self.clear_step(u, t, ap)
                        if stepped is not None:
                            ap = stepped
                    if ap < cost or u.hp <= 0:
                        break
                    self.strike(u, t)
                    ap -= cost
                    u.fired = True
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
            if not adjacent and self.holds(u, ap):
                self.stand(u, ap + keep)
                return
            if self.dash(u, t, ap, cost):
                ap -= DASH_AP
                first = False
                continue
            need = steps_to(u, t)
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
        ap = self.retreat(u, ap + keep)
        ap = self.double(u, ap) if keep else self.double(u, self.screen(u, ap))
        u.reserve = self.brace(u, max(0, ap))

    def joins_circle(self, u):
        """A guard with no enemy beside it walks to an empty place of the circle and, once there, conducts."""
        if not self.circle or u not in self.b or u is self.objective or u.skip or u.asleep:
            return False
        empty = [p for p in self.circle if p not in self.cells]
        if not empty or any(touch(u, f) and not f.asleep and not f.skip for f in alive(self.a)):
            return False
        goal = min(empty, key=lambda p: cheb_xy(u.x, u.y, p))
        ap = u.ap()
        # Walking past an enemy draws its Reaction Attack, which can kill the guard on the way.
        while ap >= 1 and u.hp > 0 and (u.x, u.y) != goal:
            left = self.step(u, goal, ap)
            if left is None:
                break
            ap = left
        u.conducting = u.hp > 0 and (u.x, u.y) == goal
        u.reserve = 0
        return True

    def ritual_round(self):
        """At the end of a round: a full circle moves the ritual on, a short one waits and may fail."""
        if sum(1 for u in alive(self.b) if u.conducting and not u.asleep) < len(self.circle):
            self.short += 1
            return self.a if self.short >= SHORT_TICKS else None
        self.short = 0
        self.progress -= 1
        return self.b if self.progress <= 0 else None

    def retreat(self, u, ap):
        """A shooter spends the AP left after its shot stepping away from the nearest melee enemy,
        never out of a side it touches, which would draw a Reaction Attack."""
        if not u.weapon.ranged or u.opener or u.hp <= 0:
            return ap
        melee = [f for f in alive(self.foes(u)) if not f.weapon.ranged and not f.asleep and not f.skip]
        if not melee or any(touch(u, f) for f in alive(self.foes(u))):
            return ap
        f = min(melee, key=lambda m: cheb(u, m))
        goal = (u.x + 40 * ((u.x > f.x) - (u.x < f.x)), u.y + 40 * ((u.y > f.y) - (u.y < f.y)))
        while ap >= 1 and u.hp > 0:
            left = self.step(u, goal, ap)
            if left is None:
                break
            ap = left
        return ap

    def brace(self, u, ap):
        """Не сдвинуть is paid from the AP left at the end of a turn, as a Defensive Stance is, never from a blow; since the
        balance after COMBAT-7 an NPC braces whenever an enemy could attack it in its next turn, not only two near."""
        if "brace" in u.abilities and ap >= BRACE_AP and u.can_spend(c.ORDINARY) and self.threatened(u):
            u.wp -= u.max_wp() * c.ORDINARY
            u.brace = True
            return ap - BRACE_AP
        return ap

    def strength(self, side, leader):
        total = sum(max(0, u.hp) / u.max_hp() * c.LIMIT_S[u.limit] ** 2 for u in side)
        return total * (self.r.leader_weight if leader is not None and leader.hp <= 0 else 1.0)

    def queue(self, order):
        """Turns of a round. A fighter who waits acts after everyone, the lowest Initiative first."""
        self.pending = set(order)
        waiting = []
        for i, u in enumerate(order):
            if u.hp > 0 and self.waits(u, order[i + 1:]):
                u.held = u.reserve = u.ap()
                u.stance = False
                waiting.append(u)
            else:
                yield u
        yield from reversed(waiting)

    def run(self, max_rounds):
        order = sorted(self.a + self.b, key=lambda u: -u.initiative())
        for rnd in range(1, max_rounds + 1):
            for u in self.queue(order):
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
            if self.circle and (winner := self.ritual_round()) is not None:
                return rnd, winner
            for k in self.clouds:
                k[2] -= 1
            self.clouds = [k for k in self.clouds if k[2] > 0]
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
    """Adds bonus to the combat attributes and skills, the Arcana skill too, or takes a negative one away, never below 1."""
    for u in units:
        for name in ("str", "end", "dex", "per", "skill", "ranged_skill", "arcana"):
            setattr(u, name, max(1, getattr(u, name) + bonus))
    return units


def main_force(rules, size=None):
    n = (random.choice(rules.forces) if size is None else size) - 3
    units = [c.follower9() for _ in range(n)] + [c.follower8(), c.follower8(), c.senior7()]
    if rules.shift > 0:
        units[n - 1] = c.follower8()
    elif rules.shift < 0:
        units[n] = c.follower9()
    if rules.ranged:
        units[0].weapon = c.RIFLE
    return outfitted(rules, stronger(units, rules.enemy_bonus))


def outfitted(rules, units):
    return [rules.outfit(u) for u in units] if rules.outfit else units


def allies():
    return [c.ally9(), c.ally9(), c.ally8(), c.ally8(), c.ally8()]


def archers():
    units = allies()
    units[0].weapon = units[1].weapon = c.RIFLE
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
        senior, group = ritual_group(rules)
        # The circle can wait at most SHORT_TICKS at a time, so a run past this bound cannot happen.
        bound = 4 * RITUAL_TICKS
        winner = Fight(survivors, group, rules, objective=senior, wounded_a=True, ritual=RITUAL_TICKS - start).run(bound)[1]
        wins += winner is survivors
    return wins / (runs or RUNS) * 100


def ritual_group(rules):
    """21 §4: a Limit 8 senior who leads the guards and fights, Limit 9 conductors round the circle, Limit 9 guards."""
    places = rules.places
    guard = c.follower9 if rules.guard_limit == 9 else c.follower8
    units = [c.follower8()] + [c.follower9() for _ in range(places)] + [guard() for _ in range(random.choice(rules.guards))]
    units = outfitted(rules, stronger(units, rules.enemy_bonus))
    for u in units[1:1 + places]:
        u.conducting = True
    return units[0], units


def duel(rules, a_fn, b_fn, runs=None):
    wins = 0
    for _ in range(runs or RUNS):
        a = a_fn()
        wins += Fight(a, b_fn(), rules).run(40)[1] is a
    return wins / (runs or RUNS) * 100


V0 = Rules(width=4, ranged=True)
PLAIN = Rules()
LEVERS = {
    "no space (combat_sim.py): base 38, +club +31, +L9 +32, +L8 +50": None,
    "cells and target choice (V0)": V0,
    "wide street, 8 cells": replace(V0, width=8),
    "lane, 3 cells": replace(V0, width=3),
    "nearest target, both sides": replace(V0, targeting="nearest"),
    "random target, both sides": replace(V0, targeting="random"),
    "no Reaction Attack": replace(V0, reaction=False),
    "2 AP to leave instead of a Reaction Attack": replace(V0, reaction=False, escape_ap=2),
    "flat armour": replace(V0, armour="flat"),
    "skill difference ×3": replace(V0, skill_weight=3.0),
    "damage ×1.5": replace(V0, damage_scale=1.5),
    "damage ×2": replace(V0, damage_scale=2.0),
    "critical chance +30": replace(V0, crit_bonus=0.30),
    "morale and leader": replace(V0, morale=0.5, leader_weight=0.7),
    "stun on 30 %": replace(V0, stun=0.3),
    "sweep at skill 10": replace(V0, sweep_skill=10),
}


def side():
    return archers()


def without_abilities(u):
    u.abilities = ()
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
    "+ Limit 8, no Дрёма": lambda: side() + [without_abilities(c.ally8())],
    "+ Limit 9": lambda: side() + [c.ally9()],
    "+ club": lambda: side() + [c.new_character()],
    "+ hero of Limit 9": lambda: side() + [c.hero9()],
    "+ hero of Limit 8": lambda: side() + [c.hero()],
    "allies in chain": lambda: equipped(armour="chain"),
    "allies in leather": lambda: equipped(armour="leather"),
    "Limit 8 with two-handed": lambda: equipped(c.TWO_HAND, only="A8"),
    "Limit 8 with shield": lambda: equipped(c.SHIELDED, only="A8"),
    "Limit 9 with rifles": lambda: equipped(c.RIFLE, only="A9"),
    "Limit 9 in melee, no shooters": allies,
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
    return f"{name:<30} {chain(replace(V0, a_targeting=name.split()[-1]), side):5.1f}"


def duelled(name):
    seeded(name)
    a, b, rules = DUELS[name]
    return f"{name:<30} {duel(rules, a, b):5.1f}"


def main():
    with Pool() as pool:
        print("Levers against one more fighter: points of the whole chain")
        print("\n".join(pool.map(lever, LEVERS)))
        print("\nV0: what stops the ritual")
        print("\n".join(pool.map(fielded, SIDES)))
        print("\n".join(pool.map(forced, (4, 5, 6))))
        print("\n".join(pool.map(shifted, (-1, 1))))
        print("\nV0: allies who play badly, enemies who play well")
        print("\n".join(pool.map(careless, ("careless nearest", "careless random"))))
        print("\nSmall fights, first side wins")
        print("\n".join(pool.map(duelled, DUELS)))


if __name__ == "__main__":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", line_buffering=True)
    main()

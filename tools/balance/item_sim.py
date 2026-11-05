"""What items do to a fight, on the Combat Grid of tactical_sim.py.

19 gives items a stage, slots, 0-6 modifiers, and a quality named by their count,
but no modifier types or values. This file tries a working catalogue of them, made
as 19 §3 makes them. Every value here is an authoring default, not a rule. Soldiers
differ a little by a random spread of attributes, and the rest of a fighter's
strength comes from its items, which it buys in the shops both sides share.

Run: python tools/balance/item_sim.py [fights per line] [section]
"""
import io
import math
import random
import sys
import zlib
from dataclasses import dataclass, replace
from multiprocessing import Pool

import combat_sim as c
import tactical_sim as t

RUNS = int(sys.argv[1]) if len(sys.argv) > 1 else 3000
SECTION = sys.argv[2] if len(sys.argv) > 2 else "all"
SPREAD = 2
FAVOURED = 0.75
# The types each base favours, and through them what a whole set leans to.
SLOTS = {
    "weapon": ("damage", "accuracy", "crit", "crit_damage", "magic"),
    "head": ("accuracy", "crit", "will", "hp"),
    "body": ("defence", "hp"),
    "shoulders": ("defence", "ward", "will", "hp"),
    "gloves": ("damage", "accuracy", "crit"),
    "trousers": ("hp", "defence", "evasion"),
    "boots": ("evasion", "initiative", "ap"),
    "belt": ("hp", "ap", "defence"),
    "ring 1": ("damage", "crit", "crit_damage", "magic", "wp"),
    "ring 2": ("damage", "crit", "crit_damage", "magic", "wp"),
    "necklace": ("will", "hp", "initiative", "accuracy", "wp", "ward"),
}
# What one major modifier of tier 9 gives. Percentages are fractions, the rest are points.
VALUE = {
    "damage": 0.11, "accuracy": 5.0, "crit": 0.11, "crit_damage": 0.30,
    "hp": 0.09, "defence": 5.0, "evasion": 4.0,
    "ap": 0.8, "initiative": 3.0, "will": 4.0,
    "magic": 0.11, "wp": 0.09, "ward": 4.0,
}
ALL_TYPES = tuple(VALUE)
TIER_STEP = 0.5
TIER_WEIGHTS = {9: ((9, 8, 7), (70, 25, 5)), 8: ((9, 8, 7, 6), (35, 40, 20, 5))}
QUALITY = {"Normal": (0, 0), "Magic": (1, 2), "Rare": (3, 3), "Epic": (4, 4), "Legendary": (5, 6)}
CITY, CAPITAL, FACTION = 26, 50, 75
GIFT = (("Magic", "Rare", "Epic"), (70, 28, 2))


@dataclass(frozen=True)
class Items:
    scale: float = 1.0
    spread: int = SPREAD
    minor: float = 0.5


def roll_modifier(slot, stage, items):
    pool = SLOTS[slot] if random.random() < FAVOURED else ALL_TYPES
    kind = random.choice(pool)
    tiers, weights = TIER_WEIGHTS[stage]
    tier = random.choices(tiers, weights)[0]
    return kind, VALUE[kind] * items.scale * (1 + TIER_STEP * (9 - tier)) * random.uniform(0.8, 1.2)


def add(u, kind, value):
    setattr(u.gear, kind, getattr(u.gear, kind) + value)


def filled(u, slot, count, items, stage=9):
    """One item with this many modifiers, 3 major and 3 minor at most."""
    if slot == "weapon":
        u.weapon = c.staged(u.weapon, stage)
    elif slot == "body":
        u.armour_stage = stage
    major = minor = 0
    for _ in range(count):
        is_major = minor == 3 or (major < 3 and random.random() < 0.5)
        major += is_major
        minor += not is_major
        kind, value = roll_modifier(slot, stage, items)
        add(u, kind, value * (1 if is_major else items.minor))
    return u


def item(u, slot, quality, items):
    return filled(u, slot, random.randint(*QUALITY[quality]), items)


def equip(u, quality, items):
    """A whole set of one quality. The luck of the rolls stays in."""
    for slot in SLOTS:
        item(u, slot, quality, items)
    return u


def modifier_count(skill):
    """19 §3, with the maker's Perception and Luck left out."""
    p = 0.10 + 0.60 * (skill - 1) / 99
    return sum(random.random() < p for _ in range(6))


def supplied(u, skill, items, slots=tuple(SLOTS), tries=1, stage=9):
    """Items as a maker of this skill makes them, each the best of `tries` attempts."""
    for slot in slots:
        filled(u, slot, max(modifier_count(skill) for _ in range(tries)), items, stage)
    return u


def spread(u, n=SPREAD):
    """A soldier is a little better or worse than the reference, at random."""
    for name in ("str", "end", "dex", "per", "skill", "will"):
        setattr(u, name, max(1, getattr(u, name) + random.randint(-n, n)))
    return u


def soldier(items=Items()):
    return spread(c.Unit("s", 9, 6, 6, 4, 4, 5, will=4, weapon=c.ONE_HAND, armour="cloth"), items.spread)


def band(n, quality, items):
    return [equip(soldier(items), quality, items) for _ in range(n)]


def pure(n, kinds, count, items):
    """A band whose every member carries `count` modifiers of the given types."""
    units = []
    for _ in range(n):
        u = soldier(items)
        for i in range(count):
            kind = kinds[i % len(kinds)]
            add(u, kind, VALUE[kind] * items.scale)
        units.append(u)
    return units


def d(a, b):
    return t.duel(t.PLAIN, a, b, RUNS)


def seeded(name):
    random.seed(zlib.crc32(name.encode()))


def line(name, row):
    return f"{name:<24} " + " ".join(f"{k} {v:3.0f}" for k, v in row.items())


def row(name, percent):
    return f"{name:<44} {percent:5.1f}"


# One stat alone: which types are worth the same? 5 with it against 5 without.
def stat_job(kind):
    seeded(kind)
    items = Items()
    return line(kind, {f"+{n}": d(lambda: pure(5, (kind,), n, items), lambda: pure(5, (), 0, items)) for n in (3, 6, 12)})


THEMES = {
    "attack": ("damage", "accuracy", "crit", "crit_damage"),
    "defence": ("hp", "defence", "evasion"),
    "tempo": ("ap", "evasion"),
    "crit": ("crit", "crit_damage"),
    "mixed": ("damage", "hp", "accuracy", "defence", "crit", "evasion"),
}


# Whole sets of equal size against each other: is there one best set?
def theme_job(name):
    seeded(name)
    items = Items(scale=0.75)
    out = {other: d(lambda: pure(5, THEMES[name], 12, items), lambda: pure(5, THEMES[other], 12, items)) for other in THEMES}
    out["plain"] = d(lambda: pure(5, THEMES[name], 12, items), lambda: pure(5, (), 0, items))
    return line(name, out)


# Does going first cost anything once the sides already stand in reach?
def first_job(gap):
    seeded(f"first {gap}")
    items = Items()
    rules = replace(t.PLAIN, gap=gap)
    a = lambda: pure(5, ("initiative",), 6, items)
    b = lambda: pure(5, (), 0, items)
    return line(f"gap {gap}", {"plain": t.duel(rules, b, b, RUNS), "initiative": t.duel(rules, a, b, RUNS)})


def one_of(n, fn, items):
    units = [soldier(items) for _ in range(n)]
    fn(units)
    return units


# How far one item reaches, on a base of 5 against 5 Normal.
def reach_job(_):
    seeded("reach")
    items = Items()
    base = lambda: one_of(5, lambda u: None, items)
    return line("reach", {
        "plain": d(base, base),
        "a R weapon": d(lambda: one_of(5, lambda u: item(u[0], "weapon", "Rare", items), items), base),
        "a L weapon": d(lambda: one_of(5, lambda u: item(u[0], "weapon", "Legendary", items), items), base),
        "a R set": d(lambda: one_of(5, lambda u: equip(u[0], "Rare", items), items), base),
        "a L set": d(lambda: one_of(5, lambda u: equip(u[0], "Legendary", items), items), base),
        "5 R body": d(lambda: one_of(5, lambda us: [item(u, "body", "Rare", items) for u in us], items), base),
        "5 R weapon": d(lambda: one_of(5, lambda us: [item(u, "weapon", "Rare", items) for u in us], items), base),
    })


# One fighter alone against several with no items.
def solo_job(quality):
    seeded(f"solo {quality}")
    items = Items()
    alone = lambda: [equip(soldier(items), quality, items)]
    return line(f"one in {quality}", {f"v {n}": d(alone, lambda: band(n, "Normal", items)) for n in (1, 2, 3, 5)})


# The random spread of a soldier.
def spread_job(n):
    seeded(f"spread {n}")
    items = Items(spread=n)
    b = lambda q, k: band(k, q, items)
    return line(f"spread ±{n}", {
        "5v5": d(lambda: b("Normal", 5), lambda: b("Normal", 5)),
        "5v6": d(lambda: b("Normal", 5), lambda: b("Normal", 6)),
        "M v N": d(lambda: b("Magic", 5), lambda: b("Normal", 5)),
        "R+N v R": d(lambda: b("Rare", 5) + b("Normal", 1), lambda: b("Rare", 5)),
    })


# The ladder of quality.
def grid_job(_):
    seeded("ladder")
    items = Items()
    b = lambda q, k: band(k, q, items)
    return line("ladder", {
        "M v N": d(lambda: b("Magic", 5), lambda: b("Normal", 5)),
        "R v M": d(lambda: b("Rare", 5), lambda: b("Magic", 5)),
        "E v R": d(lambda: b("Epic", 5), lambda: b("Rare", 5)),
        "L v E": d(lambda: b("Legendary", 5), lambda: b("Epic", 5)),
        "M v 6N": d(lambda: b("Magic", 5), lambda: b("Normal", 6)),
        "R v 7N": d(lambda: b("Rare", 5), lambda: b("Normal", 7)),
        "R v 8N": d(lambda: b("Rare", 5), lambda: b("Normal", 8)),
        "M+N v M": d(lambda: b("Magic", 5) + b("Normal", 1), lambda: b("Magic", 5)),
        "R+N v R": d(lambda: b("Rare", 5) + b("Normal", 1), lambda: b("Rare", 5)),
    })


@dataclass(frozen=True)
class Supply:
    """The Shop Skill a rank buys at: Limit 9, 8, and 7. None means no items."""
    allies: tuple = (CITY, CAPITAL)
    cult: tuple = (CITY, CITY, CAPITAL)
    bonus: int = 1
    scale: float = 1.0
    spread: int = SPREAD
    minor: float = 0.5

    def items(self):
        return Items(scale=self.scale, spread=self.spread, minor=self.minor)

    def wears(self, skills):
        """A rank's skill may be a pair: then each member gets one of the two at random."""
        items = self.items()

        def dress(u):
            spread(u, items.spread)
            skill = skills[9 - u.limit]
            skill = random.choice(skill) if isinstance(skill, tuple) else skill
            return u if skill is None else supplied(u, skill, items)
        return dress

    def rules(self):
        return replace(t.V0, outfit=self.wears(self.cult), enemy_bonus=self.bonus)

    def side(self):
        return [self.wears(self.allies)(u) for u in t.archers()]


V0 = Supply()
MINORS = {
    "minor ×1, same total": Supply(minor=1.0, scale=0.75),
    "minor ×0.75, same total": Supply(minor=0.75, scale=0.857),
    "minor ×0.5": V0,
    "minor ×0.25, same total": Supply(minor=0.25, scale=1.2),
}
SUPPLIES = {
    "as they are": V0,
    "no items at all": Supply(allies=(None, None), cult=(None, None, None)),
    "city shops -10": Supply(allies=(CITY - 10, CAPITAL), cult=(CITY - 10, CITY - 10, CAPITAL)),
    "city shops -5": Supply(allies=(CITY - 5, CAPITAL), cult=(CITY - 5, CITY - 5, CAPITAL)),
    "city shops +5": Supply(allies=(CITY + 5, CAPITAL), cult=(CITY + 5, CITY + 5, CAPITAL)),
    "city shops +10": Supply(allies=(CITY + 10, CAPITAL), cult=(CITY + 10, CITY + 10, CAPITAL)),
    "city shops +20": Supply(allies=(CITY + 20, CAPITAL), cult=(CITY + 20, CITY + 20, CAPITAL)),
    "every shop +10": Supply(allies=(CITY + 10, CAPITAL + 10), cult=(CITY + 10, CITY + 10, CAPITAL + 10)),
    "every shop +20": Supply(allies=(CITY + 20, CAPITAL + 20), cult=(CITY + 20, CITY + 20, CAPITAL + 20)),
    "allies alone -10": Supply(allies=(CITY - 10, CAPITAL - 10)),
    "allies alone +10": Supply(allies=(CITY + 10, CAPITAL + 10)),
    "cult alone -10": Supply(cult=(CITY - 10, CITY - 10, CAPITAL - 10)),
    "cult alone +10": Supply(cult=(CITY + 10, CITY + 10, CAPITAL + 10)),
    "allied Limit 8 at the city shop": Supply(allies=(CITY, CITY)),
    "cult Limit 8 at the capital": Supply(cult=(CITY, CAPITAL, CAPITAL)),
    "half the cult Limit 9 at the capital": Supply(cult=((CITY, CAPITAL), CITY, CAPITAL)),
    "half the cult Limit 9 with no items": Supply(cult=((CITY, None), CITY, CAPITAL)),
    "half the cult Limit 8 with no items": Supply(cult=(CITY, (CITY, None), CAPITAL)),
    "cult Limit 7 at the city shop": Supply(cult=(CITY, CITY, CITY)),
    "cult Limit 7 with no items": Supply(cult=(CITY, CITY, None)),
    "no spread": Supply(spread=0),
    "spread ±4": Supply(spread=4),
    "modifiers ×0.75": Supply(scale=0.75),
    "modifiers ×1.5": Supply(scale=1.5),
}


def changed_one(limit, skill):
    """The allies with one fighter of this rank in another set."""
    units = V0.side()
    fresh = c.ally9() if limit == 9 else c.ally8()
    if skill is not None:
        supplied(fresh, skill, V0.items())
    units[[u.limit for u in units].index(limit)].gear = fresh.gear
    return units


ONE_FIGHTER = {
    "one allied Limit 9 at the capital's shop": lambda: changed_one(9, CAPITAL),
    "one allied Limit 8 at the faction's shop": lambda: changed_one(8, FACTION),
    "one allied Limit 8 at the city shop": lambda: changed_one(8, CITY),
    "one allied Limit 8 with no items": lambda: changed_one(8, None),
}


def looted(n, items):
    return supplied(c.hero9(), CITY, items, random.sample(list(SLOTS), n))


def stage_8_two(items):
    """Skill 15: stage 8 on weapon and body, stage 9 on the rest."""
    u = supplied(c.hero9(), 15, items, ("weapon", "body"), stage=8)
    return supplied(u, 15, items, tuple(s for s in SLOTS if s not in ("weapon", "body")))


def gift(u, items):
    """Faction Gift: one weapon or body armour of a rolled quality."""
    return item(u, random.choice(("weapon", "body")), random.choices(*GIFT)[0], items)


PLAYER = {
    "club, no items": lambda i: c.new_character(),
    "club, own work, skill 5": lambda i: supplied(c.new_character(), 5, i),
    "club, own work, skill 15": lambda i: supplied(c.new_character(), 15, i),
    "club, a Rare weapon": lambda i: item(c.new_character(), "weapon", "Rare", i),
    "club, city set": lambda i: supplied(c.new_character(), CITY, i),
    "club, capital set": lambda i: supplied(c.new_character(), CAPITAL, i),
    "Limit 9, no items": lambda i: c.ally9(),
    "Limit 9, same as the allies": lambda i: V0.wears(V0.allies)(c.ally9()),
    "Limit 8, same as the allies": lambda i: V0.wears(V0.allies)(c.ally8()),
    "hero L9, no items": lambda i: c.hero9(),
    "hero L9, own work, skill 5": lambda i: supplied(c.hero9(), 5, i),
    "hero L9, own work, skill 15": lambda i: supplied(c.hero9(), 15, i),
    "hero L9, best of 3, skill 15": lambda i: supplied(c.hero9(), 15, i, tries=3),
    "hero L9, stage 8 weapon and body, skill 15": stage_8_two,
    "hero L9, whole set stage 8, skill 15": lambda i: supplied(c.hero9(), 15, i, stage=8),
    "hero L9, whole set stage 8, best of 3": lambda i: supplied(c.hero9(), 15, i, tries=3, stage=8),
    "hero L9, 4 looted items": lambda i: looted(4, i),
    "hero L9, 8 looted items": lambda i: looted(8, i),
    "hero L9, best of 5, skill 15": lambda i: supplied(c.hero9(), 15, i, tries=5),
    "hero L9, best of 10, skill 15": lambda i: supplied(c.hero9(), 15, i, tries=10),
    "hero L9, best of 5, skill 25": lambda i: supplied(c.hero9(), 25, i, tries=5),
    "hero L9, Faction Gift": lambda i: gift(c.hero9(), i),
    "hero L9, own work skill 15 and Gift": lambda i: gift(supplied(c.hero9(), 15, i), i),
    "hero L9, city weapon and body": lambda i: supplied(c.hero9(), CITY, i, ("weapon", "body")),
    "hero L9, city set": lambda i: supplied(c.hero9(), CITY, i),
    "hero L9, city set and Gift": lambda i: gift(supplied(c.hero9(), CITY, i), i),
    "hero L9, capital set": lambda i: supplied(c.hero9(), CAPITAL, i),
    "hero L8, city set": lambda i: supplied(c.hero(), CITY, i),
    "hero L8, capital set": lambda i: supplied(c.hero(), CAPITAL, i),
}


def each(fn):
    units = V0.side()
    for u in units:
        fn(u)
    return units


def raised(u, *names, n=3):
    for name in names:
        setattr(u, name, getattr(u, name) + n)


def armed(u, weapon, limit):
    if u.limit == limit:
        u.weapon = weapon


LEVERS = {
    "allies in chain": lambda: each(lambda u: setattr(u, "armour", "chain")),
    "allies in leather": lambda: each(lambda u: setattr(u, "armour", "leather")),
    "Limit 8 with shields": lambda: each(lambda u: armed(u, c.SHIELDED, 8)),
    "Limit 8 with two-handed": lambda: each(lambda u: armed(u, c.TWO_HAND, 8)),
    "Limit 9 in melee, no shooters": lambda: [V0.wears(V0.allies)(u) for u in t.allies()],
    "allies do not infuse": lambda: each(lambda u: setattr(u, "infuse", False)),
    "+3 Strength on all": lambda: each(lambda u: raised(u, "str")),
    "+3 Endurance on all": lambda: each(lambda u: raised(u, "end")),
    "+3 Dexterity on all": lambda: each(lambda u: raised(u, "dex")),
    "+3 Perception on all": lambda: each(lambda u: raised(u, "per")),
    "+3 weapon skill on all": lambda: each(lambda u: raised(u, "skill")),
    "+3 on all five": lambda: each(lambda u: raised(u, "str", "end", "dex", "per", "skill")),
}
RULES = {
    "allies take the nearest": dict(a_targeting="nearest"),
    "allies take at random": dict(a_targeting="random"),
    "street of 3 cells": dict(width=3),
    "street of 8 cells": dict(width=8),
    "one enemy a Limit weaker": dict(shift=-1),
    "one enemy a Limit stronger": dict(shift=1),
    "critical hits ×1.5": dict(crit=1.5),
    "no Wait": dict(wait=False),
    "NPCs walk into reach": dict(hold=False),
    "no Wait, no holding back, ×1.5": dict(wait=False, hold=False, crit=1.5),
}


def minor_job(name):
    seeded(name)
    o = MINORS[name]
    rules = o.rules()
    chain = lambda fn: t.chain(rules, fn, runs=RUNS)
    base = chain(o.side)
    more = {
        "club": chain(lambda: o.side() + [c.new_character()]) - base,
        "L9": chain(lambda: o.side() + [o.wears(o.allies)(c.ally9())]) - base,
        "L8": chain(lambda: o.side() + [o.wears(o.allies)(c.ally8())]) - base,
    }
    return f"{name:<34} base {base:3.0f} " + " ".join(f"{k} {v:+3.0f}" for k, v in more.items())


def supply_job(name):
    seeded(name)
    o = SUPPLIES[name]
    return row(name, t.chain(o.rules(), o.side, runs=RUNS))


def fighter_job(name):
    seeded(name)
    return row(name, t.chain(V0.rules(), ONE_FIGHTER[name], runs=RUNS))


def player_job(name):
    seeded(name)
    fn, items = PLAYER[name], V0.items()
    return row("+ " + name, t.chain(V0.rules(), lambda: V0.side() + [fn(items)], runs=RUNS))


def lever_job(name):
    seeded(name)
    return row(name, t.chain(V0.rules(), LEVERS[name], runs=RUNS))


def rule_job(name):
    seeded(name)
    return row(name, t.chain(replace(V0.rules(), **RULES[name]), V0.side, runs=RUNS))


def forced_job(size):
    seeded(f"force {size}")
    return row(f"main force of {size}", t.chain(V0.rules(), V0.side, size, RUNS))


# How the ritual of 21 §4 ends: the allies, with or without a hero, and a hero alone at the circle.
RITUAL = {
    "allies, senior first": (lambda: V0.side(), False, True),
    "allies, circle first": (lambda: V0.side(), True, True),
    "+ hero L9 city set, senior first": (lambda: V0.side() + [PLAYER["hero L9, city set"](V0.items())], False, True),
    "+ hero L9 city set, circle first": (lambda: V0.side() + [PLAYER["hero L9, city set"](V0.items())], True, True),
    "hero L9 city set alone": (lambda: [PLAYER["hero L9, city set"](V0.items())], False, False),
    "hero L8 capital set alone": (lambda: [PLAYER["hero L8, capital set"](V0.items())], False, False),
    "hero L8 capital set alone, circle first": (lambda: [PLAYER["hero L8, capital set"](V0.items())], True, False),
}


def ritual_job(name):
    """With no main battle first, the side reaches the circle at tick 15, unwounded."""
    seeded(f"ritual {name}")
    side, circle_first, battle = RITUAL[name]
    rules = replace(V0.rules(), circle_first=circle_first)
    ends = {"senior": 0, "circle": 0, "done": 0}
    for _ in range(RUNS):
        a, start = side(), 15
        if battle:
            rounds, winner = t.Fight(a, t.main_force(rules), rules).run(55)
            if winner is not a:
                continue
            start = max(rounds + 5, random.randint(15, 20))
        senior, group = t.ritual_group(rules)
        fight = t.Fight(t.alive(a) if battle else a, group, rules, objective=senior, wounded_a=battle, ritual=t.RITUAL_TICKS - start)
        winner = fight.run(4 * t.RITUAL_TICKS)[1]
        key = "done" if winner is not fight.a else "senior" if senior.hp <= 0 else "circle"
        ends[key] += 1
    share = lambda k: ends[k] / RUNS * 100
    return f"{name:<40} stopped {share('senior') + share('circle'):5.1f}  by the senior {share('senior'):5.1f}  by the circle {share('circle'):5.1f}"


# How much a fighter who only stands in Defence wins against one who attacks.
def defence_job(name):
    seeded(f"defence {name}")
    rules = replace(t.PLAIN, gap=2, turtle=True)

    def fighter(weapon):
        u = c.ally8()
        u.weapon = weapon
        return u
    weapon = {"one-handed": c.ONE_HAND, "shield": c.SHIELDED, "two-handed": c.TWO_HAND, "dagger": c.DAGGER}[name]
    return row(f"only Defence, {name}", t.duel(rules, lambda: [fighter(weapon)], lambda: [c.ally8()], RUNS))


def binomial(skill):
    p = 0.10 + 0.60 * (skill - 1) / 99
    return [math.comb(6, k) * p ** k * (1 - p) ** (6 - k) for k in range(7)]


# What a shelf holds at each Shop Skill, by quality. 19 §3 with no Luck or Perception.
def stock_job(skill):
    b = binomial(skill)
    share = {"N": b[0], "M": b[1] + b[2], "R": b[3], "E": b[4], "L": b[5] + b[6]}
    return f"skill {skill:3} " + " ".join(f"{k} {v * 100:5.2f}" for k, v in share.items())


# Does the outfit keep fights to a length the ritual allows?
def length_job(_):
    seeded("length")
    rules = V0.rules()
    rounds = wins = longest = timed = 0
    for _ in range(RUNS):
        a = V0.side()
        n, winner = t.Fight(a, t.main_force(rules), rules).run(55)
        rounds += n
        longest = max(longest, n)
        wins += winner is a
        timed += winner is None
    return f"main battle: allies win {wins / RUNS * 100:.0f} %, {rounds / RUNS:.1f} rounds, longest {longest}, undecided {timed / RUNS * 100:.1f} %"


SECTIONS = {
    "stats": (stat_job, list(VALUE)),
    "themes": (theme_job, list(THEMES)),
    "first": (first_job, [12, 6, 3]),
    "reach": (reach_job, [0]),
    "solo": (solo_job, ["Magic", "Rare", "Epic", "Legendary"]),
    "spread": (spread_job, [0, 2, 4]),
    "grid": (grid_job, [0]),
    "minors": (minor_job, list(MINORS)),
    "supply": (supply_job, list(SUPPLIES)),
    "fighter": (fighter_job, list(ONE_FIGHTER)),
    "player": (player_job, list(PLAYER)),
    "levers": (lever_job, list(LEVERS)),
    "rules": (rule_job, list(RULES)),
    "force": (forced_job, [4, 5, 6]),
    "ritual": (ritual_job, list(RITUAL)),
    "defence": (defence_job, ["one-handed", "shield", "two-handed", "dagger"]),
    "stock": (stock_job, [5, 15, 26, 36, 46, 50, 60, 70, 75, 95]),
    "length": (length_job, [0]),
}


def main():
    with Pool() as pool:
        for name, (job, args) in SECTIONS.items():
            if SECTION in ("all", name):
                print(f"\n== {name}", flush=True)
                for text in pool.imap(job, args):
                    print(text, flush=True)


if __name__ == "__main__":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", line_buffering=True)
    main()

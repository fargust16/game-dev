"""What item sets do to a fight, on the Combat Grid of tactical_sim.py.

19 gives items a stage, slots, 0-6 modifiers, and a quality named by their count,
but no modifier types or values. This file tries a working catalogue of them.
Every value here is an experiment, not a rule. Soldiers differ a little by a random
spread of attributes, and the rest of a fighter's strength comes from its items.

Run: python tools/balance/item_sim.py [fights per line] [section]
"""
import io
import random
import sys
import zlib
from dataclasses import dataclass, replace
from multiprocessing import Pool

import combat_sim as c
import tactical_sim as t

RUNS = int(sys.argv[1]) if len(sys.argv) > 1 else 1500
SECTION = sys.argv[2] if len(sys.argv) > 2 else "all"
SPREAD = 2
FAVOURED = 0.75
# The types each base favours, and through them what a whole set leans to.
SLOTS = {
    "weapon": ("damage", "accuracy", "crit", "crit_damage"),
    "head": ("accuracy", "crit", "will", "hp"),
    "body": ("defence", "hp"),
    "shoulders": ("defence", "will", "hp"),
    "gloves": ("damage", "accuracy", "crit"),
    "trousers": ("hp", "defence", "evasion"),
    "boots": ("evasion", "initiative", "ap"),
    "belt": ("hp", "ap", "defence"),
    "ring 1": ("damage", "crit", "crit_damage", "will"),
    "ring 2": ("damage", "crit", "crit_damage", "will"),
    "necklace": ("will", "hp", "initiative", "accuracy"),
}
# What one modifier of tier 9 gives. Percentages are fractions, the rest are points.
VALUE = {
    "damage": 0.04, "accuracy": 2.0, "crit": 0.035, "crit_damage": 0.10,
    "hp": 0.035, "defence": 1.5, "evasion": 1.4,
    "ap": 0.3, "initiative": 1.0, "will": 1.5,
}
ALL_TYPES = tuple(VALUE)
TIER_STEP = 0.5
TIER_WEIGHTS = {9: ((9, 8, 7), (70, 25, 5)), 8: ((9, 8, 7, 6), (35, 40, 20, 5))}
QUALITY = {"Normal": (0, 0), "Magic": (1, 2), "Rare": (3, 3), "Epic": (4, 4), "Legendary": (5, 6)}
SHORT = dict(zip("NMREL", QUALITY))


@dataclass(frozen=True)
class Items:
    scale: float = 1.0
    stage_step: float = 0.35
    spread: int = SPREAD


def roll_modifier(slot, stage, items):
    pool = SLOTS[slot] if random.random() < FAVOURED else ALL_TYPES
    kind = random.choice(pool)
    tiers, weights = TIER_WEIGHTS[stage]
    tier = random.choices(tiers, weights)[0]
    return kind, VALUE[kind] * items.scale * (1 + TIER_STEP * (9 - tier)) * random.uniform(0.8, 1.2)


def add(u, kind, value):
    setattr(u.gear, kind, getattr(u.gear, kind) + value)


def item(u, slot, quality, items, stage=9):
    if slot == "weapon":
        u.weapon = c.staged(u.weapon, stage)
    elif slot == "body":
        u.armour_stage = stage
    low, high = QUALITY[quality]
    for _ in range(random.randint(low, high)):
        add(u, *roll_modifier(slot, stage, items))
    return u


def equip(u, quality, items, stage=9):
    """A whole set of one quality. The luck of the rolls stays in."""
    for slot in SLOTS:
        item(u, slot, quality, items, stage)
    return u


def spread(u, n=SPREAD):
    """A soldier is a little better or worse than the reference, at random."""
    for name in ("str", "end", "dex", "per", "skill", "will"):
        setattr(u, name, max(1, getattr(u, name) + random.randint(-n, n)))
    return u


def soldier(items=Items()):
    return spread(c.Unit("s", 9, 6, 6, 4, 4, 5, will=4, weapon=c.ONE_HAND, armour="cloth"), items.spread)


def band(n, quality, items, stage=9):
    return [equip(soldier(items), quality, items, stage) for _ in range(n)]


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


# One stat alone: which types are worth the same? 5 with it against 5 without.
def stat_job(kind):
    seeded(kind)
    items = Items()
    return line(kind, {f"+{n}": d(lambda: pure(5, (kind,), n, items), lambda: pure(5, (), 0, items)) for n in (6, 12, 24)})


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
    items = Items(scale=2.0)
    row = {other: d(lambda: pure(5, THEMES[name], 12, items), lambda: pure(5, THEMES[other], 12, items)) for other in THEMES}
    row["plain"] = d(lambda: pure(5, THEMES[name], 12, items), lambda: pure(5, (), 0, items))
    return line(name, row)


def one_of(n, fn, items):
    units = [soldier(items) for _ in range(n)]
    fn(units)
    return units


# How far one item reaches, on a base of 5 against 5 Normal.
def reach_job(scale):
    seeded(f"reach {scale}")
    items = Items(scale=scale)
    base = lambda: one_of(5, lambda u: None, items)
    row = {
        "a R weapon": d(lambda: one_of(5, lambda u: item(u[0], "weapon", "Rare", items), items), base),
        "a L weapon": d(lambda: one_of(5, lambda u: item(u[0], "weapon", "Legendary", items), items), base),
        "a R set": d(lambda: one_of(5, lambda u: equip(u[0], "Rare", items), items), base),
        "a L set": d(lambda: one_of(5, lambda u: equip(u[0], "Legendary", items), items), base),
        "5 R body": d(lambda: one_of(5, lambda us: [item(u, "body", "Rare", items) for u in us], items), base),
        "5 R weapon": d(lambda: one_of(5, lambda us: [item(u, "weapon", "Rare", items) for u in us], items), base),
    }
    return line(f"scale {scale}", row)


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


# Ladders of quality, by the size of a modifier.
GRID = {f"scale {k}": Items(scale=k) for k in (1.0, 1.5, 2.0, 3.0)}


def grid_job(name):
    seeded(name)
    items = GRID[name]
    b = lambda q, k: band(k, q, items)
    return line(name, {
        "M v N": d(lambda: b("Magic", 5), lambda: b("Normal", 5)),
        "R v M": d(lambda: b("Rare", 5), lambda: b("Magic", 5)),
        "E v R": d(lambda: b("Epic", 5), lambda: b("Rare", 5)),
        "L v E": d(lambda: b("Legendary", 5), lambda: b("Epic", 5)),
        "M v 6N": d(lambda: b("Magic", 5), lambda: b("Normal", 6)),
        "R v 7N": d(lambda: b("Rare", 5), lambda: b("Normal", 7)),
        "R v 8N": d(lambda: b("Rare", 5), lambda: b("Normal", 8)),
        "M+N v M": d(lambda: b("Magic", 5) + b("Normal", 1), lambda: b("Magic", 5)),
        "R+N v R": d(lambda: b("Rare", 5) + b("Normal", 1), lambda: b("Rare", 5)),
        "R+M v R": d(lambda: b("Rare", 5) + b("Magic", 1), lambda: b("Rare", 5)),
    })


# Does going first cost anything once the sides already stand in reach?
def first_job(gap):
    seeded(f"first {gap}")
    items = Items()
    rules = replace(t.PLAIN, gap=gap)
    a = lambda: pure(5, ("initiative",), 12, items)
    b = lambda: pure(5, (), 0, items)
    return line(f"gap {gap}", {"plain": t.duel(rules, b, b, RUNS), "+12 initiative": t.duel(rules, a, b, RUNS)})


def ranked(profile):
    return {9: SHORT[profile[0]], 8: SHORT[profile[1]], 7: SHORT[profile[-1]]}


@dataclass(frozen=True)
class Outfit:
    """Who wears what by rank. Letters are qualities at Limit 9, 8 and 7."""
    allies: str = "MR"
    cult: str = "MMR"
    bonus: int = 1
    scale: float = 2.0
    spread: int = SPREAD

    def items(self):
        return Items(scale=self.scale, spread=self.spread)

    def wears(self, profile):
        items = self.items()
        return lambda u: equip(spread(u, items.spread), ranked(profile)[u.limit], items)

    def rules(self):
        return replace(t.V0, outfit=self.wears(self.cult), enemy_bonus=self.bonus)

    def side(self):
        return [self.wears(self.allies)(u) for u in t.archers()]


# The slice: what each outfit gives, and what one more does.
SLICES = {f"{a} v {b}+{n} ×{k}": Outfit(a, b, n, k) for k in (2.0,) for a, b, n in (
    ("NN", "NNN", 1), ("MR", "NME", 2), ("MR", "MMR", 1), ("MR", "MMR", 2), ("MR", "MRR", 1),
    ("MM", "MMR", 1), ("RE", "RRR", 1), ("RR", "RRR", 1),
)}


def slice_job(name):
    seeded(name)
    o = SLICES[name]
    items, rules = o.items(), o.rules()
    chain = lambda fn: t.chain(rules, fn, runs=RUNS)
    base = chain(o.side)
    extra = lambda fn: chain(lambda: o.side() + [fn()]) - base
    row = {
        "base": base,
        "club": extra(c.new_character),
        "club M": extra(lambda: equip(c.new_character(), "Magic", items)),
        "club R": extra(lambda: equip(c.new_character(), "Rare", items)),
        "L9": extra(lambda: o.wears(o.allies)(c.ally9())),
        "L8": extra(lambda: o.wears(o.allies)(c.ally8())),
    }
    return f"{name:<24} " + " ".join(f"{k} {v:+3.0f}" if k != "base" else f"{k} {v:3.0f}" for k, v in row.items())


# Outfit V0: what moves it, in points of the whole chain.
V0 = Outfit()
ALLIED_ROWS = {
    "allies alone": lambda o, i: o.side(),
    "+ club": lambda o, i: o.side() + [c.new_character()],
    "+ club, a Rare weapon": lambda o, i: o.side() + [item(c.new_character(), "weapon", "Rare", i)],
    "+ club, Magic set": lambda o, i: o.side() + [equip(c.new_character(), "Magic", i)],
    "+ club, Rare set": lambda o, i: o.side() + [equip(c.new_character(), "Rare", i)],
    "+ Limit 9, no items": lambda o, i: o.side() + [c.ally9()],
    "+ Limit 9, same": lambda o, i: o.side() + [o.wears(o.allies)(c.ally9())],
    "+ Limit 8, same": lambda o, i: o.side() + [o.wears(o.allies)(c.ally8())],
    "+ hero of Limit 9, no items": lambda o, i: o.side() + [c.hero9()],
    "+ hero of Limit 9, Magic set": lambda o, i: o.side() + [equip(c.hero9(), "Magic", i)],
    "+ hero of Limit 9, Rare set": lambda o, i: o.side() + [equip(c.hero9(), "Rare", i)],
    "+ hero of Limit 8, Rare set": lambda o, i: o.side() + [equip(c.hero(), "Rare", i)],
}
OUTFITS = {
    "no items at all": Outfit("NN", "NNN"),
    "allies a quality up": Outfit(allies="RE"),
    "allies a quality down": Outfit(allies="NM"),
    "allied Limit 9 in Rare": Outfit(allies="RR"),
    "allied Limit 8 in Epic": Outfit(allies="ME"),
    "cult a quality up": Outfit(cult="RRE"),
    "cult a quality down": Outfit(cult="NNM"),
    "no spread": Outfit(spread=0),
    "spread ±4": Outfit(spread=4),
    "modifiers ×1.5": Outfit(scale=1.5),
    "modifiers ×3": Outfit(scale=3.0),
}


def each(o, fn):
    units = o.side()
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
    "allies in chain": lambda o: each(o, lambda u: setattr(u, "armour", "chain")),
    "allies in leather": lambda o: each(o, lambda u: setattr(u, "armour", "leather")),
    "Limit 8 with shields": lambda o: each(o, lambda u: armed(u, c.SHIELDED, 8)),
    "Limit 8 with two-handed": lambda o: each(o, lambda u: armed(u, c.TWO_HAND, 8)),
    "Limit 9 with crossbows": lambda o: each(o, lambda u: armed(u, c.CROSSBOW, 9)),
    "Limit 9 in melee, no bows": lambda o: [o.wears(o.allies)(u) for u in t.allies()],
    "+3 Strength on all": lambda o: each(o, lambda u: raised(u, "str")),
    "+3 Endurance on all": lambda o: each(o, lambda u: raised(u, "end")),
    "+3 Dexterity on all": lambda o: each(o, lambda u: raised(u, "dex")),
    "+3 Perception on all": lambda o: each(o, lambda u: raised(u, "per")),
    "+3 weapon skill on all": lambda o: each(o, lambda u: raised(u, "skill")),
    "+3 on all five": lambda o: each(o, lambda u: raised(u, "str", "end", "dex", "per", "skill")),
}
RULES = {
    "allies take the nearest": dict(a_targeting="nearest"),
    "allies take at random": dict(a_targeting="random"),
    "street of 3 cells": dict(width=3),
    "street of 8 cells": dict(width=8),
    "one enemy a Limit weaker": dict(shift=-1),
    "one enemy a Limit stronger": dict(shift=1),
}


def lever_job(name):
    seeded(name)
    return f"{name:<30} {t.chain(V0.rules(), lambda: LEVERS[name](V0), runs=RUNS):5.1f}"


def rule_job(name):
    seeded(name)
    return f"{name:<30} {t.chain(replace(V0.rules(), **RULES[name]), V0.side, runs=RUNS):5.1f}"


def allied_job(name):
    seeded(name)
    fn = ALLIED_ROWS[name]
    items = V0.items()
    return f"{name:<30} {t.chain(V0.rules(), lambda: fn(V0, items), runs=RUNS):5.1f}"


def outfit_job(name):
    seeded(name)
    o = OUTFITS[name]
    return f"{name:<30} {t.chain(o.rules(), o.side, runs=RUNS):5.1f}"


def forced_job(size):
    seeded(f"force {size}")
    return f"main force of {size:<16} {t.chain(V0.rules(), V0.side, size, RUNS):5.1f}"


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
    "reach": (reach_job, [1.0, 2.0, 3.0]),
    "first": (first_job, [12, 6, 3]),
    "spread": (spread_job, [0, 1, 2, 3, 4]),
    "grid": (grid_job, list(GRID)),
    "slice": (slice_job, list(SLICES)),
    "allied": (allied_job, list(ALLIED_ROWS)),
    "outfits": (outfit_job, list(OUTFITS)),
    "levers": (lever_job, list(LEVERS)),
    "rules": (rule_job, list(RULES)),
    "force": (forced_job, [5, 6, 7]),
    "length": (length_job, [0]),
}


def main():
    with Pool() as pool:
        for name, (job, args) in SECTIONS.items():
            if SECTION in ("all", name):
                print(f"\n== {name}", flush=True)
                for row in pool.imap(job, args):
                    print(row, flush=True)


if __name__ == "__main__":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", line_buffering=True)
    main()

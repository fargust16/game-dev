"""Do the kinds of fighter of 10.2 balance against each other?

Builds of one Limit, with one budget of attributes and of item modifiers, on the
Combat Grid of tactical_sim.py, averaged over how far apart a fight starts. The aim
of 10.2: a mage beats plate, plate beats a shooter, a shooter beats a mage, attack
and critical hits sit between them, and no build is best overall. The lines below
the five main builds are the choices a player could make instead: they check that
no such choice beats the main builds. Every value is an authoring default.

Run: python tools/balance/build_sim.py [fights per cell]
"""
import io
import random
import sys
import zlib
from dataclasses import dataclass
from multiprocessing import Pool

import combat_sim as c
import item_sim as s
import tactical_sim as t

RUNS = int(sys.argv[1]) if len(sys.argv) > 1 else 300
SKILL = 12
MODIFIERS = 12
SCALE = 0.75
GAPS = (6, 9, 12)
AVERAGE_COVER = 0.10
TEMPO = ("crit", "crit_damage", "initiative")


@dataclass(frozen=True)
class Build:
    str: int
    end: int
    dex: int
    per: int
    intl: int
    will: int
    weapon: c.Weapon
    armour: str
    mods: tuple
    infuse: bool = False


PLATE = Build(15, 14, 6, 6, 3, 7, c.SHIELDED, "plate", ("hp", "defence", "damage"))
ATTACK = Build(18, 10, 9, 6, 3, 5, c.TWO_HAND, "chain", ("damage", "accuracy", "hp"))
CRIT = Build(9, 9, 11, 16, 3, 3, c.ONE_HAND, "leather", ("crit", "crit_damage", "accuracy"))
SHOOTER = Build(5, 9, 15, 15, 3, 4, c.RIFLE, "leather", ("damage", "accuracy", "evasion"))
MAGE = Build(4, 9, 6, 8, 14, 10, c.RIFLE, "leather", ("magic", "wp", "hp", "accuracy"), True)


def variant(base, **changes):
    return Build(**{**base.__dict__, **changes})


BUILDS = {"plate": PLATE, "attack": ATTACK, "crit": CRIT, "rifle": SHOOTER, "mage": MAGE}
MAIN = tuple(BUILDS)
BUILDS.update({
    "attack in plate": variant(ATTACK, armour="plate"),
    "crit, dagger": variant(CRIT, weapon=c.DAGGER),
    "crit, Initiative": variant(CRIT, mods=TEMPO),
    "bow": variant(SHOOTER, weapon=c.BOW),
    "pistol": variant(SHOOTER, weapon=c.PISTOL),
    "rifle, Initiative": variant(SHOOTER, mods=("damage", "accuracy", "initiative")),
    "rifle in plate": variant(SHOOTER, armour="plate"),
    "mage in cloth": variant(MAGE, armour="cloth"),
    "mage in chain": variant(MAGE, armour="chain"),
    "mage in plate": variant(MAGE, armour="plate"),
    "mage, pistol": variant(MAGE, weapon=c.PISTOL),
    "mage, sword": variant(MAGE, weapon=c.ONE_HAND),
    "mage, two-hand": variant(MAGE, weapon=c.TWO_HAND),
})


def fighter(build):
    b = BUILDS[build]
    u = c.Unit(build, 8, b.str, b.end, b.dex, b.per, SKILL, b.will, b.intl, b.weapon, b.armour, infuse=b.infuse)
    for i in range(MODIFIERS):
        kind = b.mods[i % len(b.mods)]
        s.add(u, kind, s.VALUE[kind] * SCALE)
    return u


def cell(job):
    a, b, n, gap = job
    random.seed(zlib.crc32(f"{a} {b} {n} {gap}".encode()))
    rules = t.Rules(gap=gap, cover=AVERAGE_COVER)
    return t.duel(rules, lambda: [fighter(a) for _ in range(n)], lambda: [fighter(b) for _ in range(n)], RUNS)


def table(pool, n, rows):
    jobs = [(a, b, n, gap) for a in rows for b in MAIN for gap in GAPS]
    wins = dict(zip(jobs, pool.map(cell, jobs)))
    print(f"{n} v {n}".ljust(18) + " ".join(f"{b:>6}" for b in MAIN) + "   mean")
    for a in rows:
        row = [sum(wins[(a, b, n, gap)] for gap in GAPS) / len(GAPS) for b in MAIN]
        mean = sum(v for b, v in zip(MAIN, row) if b != a) / (len(MAIN) - (a in MAIN))
        print(a.ljust(18) + " ".join(f"{v:6.0f}" for v in row) + f" {mean:6.0f}", flush=True)


def main():
    with Pool() as pool:
        table(pool, 5, tuple(BUILDS))
        print()
        table(pool, 1, MAIN)


if __name__ == "__main__":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", line_buffering=True)
    main()

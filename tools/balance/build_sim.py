"""Do the kinds of fighter of 10.2 balance against each other?

Builds of one Limit, with one budget of attributes and of item modifiers, on the
Combat Grid of tactical_sim.py, averaged over how far apart a fight starts. The aim
of 10.2: a mage beats plate, plate beats a shooter, a shooter beats a mage, attack
and critical hits sit between them, and no build is best overall. The lines below
the five main builds are the choices a player could make instead: they check that
no such choice beats the main builds. Every value is an authoring default.

Run: python tools/balance/build_sim.py [fights per cell] [none | 8 | 7]

With 8 or 7 every fighter is of that Limit and has the combat abilities of its Arcana
(05.1) for that Limit and those below it: plate and attack are on Император, the rest
on Жрица, and the lines that end in an Arcana swap it. Завеса is not modelled.
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
MODE = sys.argv[2] if len(sys.argv) > 2 else "none"
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
    second: c.Weapon = None
    opener: bool = False
    arcana: str = "Жрица"


ARCANA = {
    "Император": {9: ("brace",), 8: ("support", "stand"), 7: ("pillar", "crush")},
    "Жрица": {9: (), 8: ("sleep",), 7: ("sting",)},
}

# Strength, Endurance, Dexterity, Perception, Intelligence, Will: 51 points each.
STATS = {
    "plate": (15, 11, 6, 6, 3, 10),
    "attack": (17, 10, 9, 6, 3, 6),
    "crit": (9, 9, 11, 16, 3, 3),
    "rifle": (5, 9, 15, 15, 3, 4),
    "mage": (4, 9, 6, 8, 14, 10),
}
MAIN = tuple(STATS)


def variant(base, **changes):
    return Build(**{**base.__dict__, **changes})


def make_builds():
    """Read from the weapons of combat_sim.py as they stand, so a tuning run can change them."""
    shield = c.shielded(c.SWORD)
    plate = Build(*STATS["plate"], shield, "plate", ("hp", "defence", "damage"), arcana="Император")
    attack = Build(*STATS["attack"], c.GREATSWORD, "chain", ("damage", "accuracy", "hp"), arcana="Император")
    crit = Build(*STATS["crit"], c.SWORD, "leather", ("crit", "crit_damage", "accuracy"))
    shooter = Build(*STATS["rifle"], c.RIFLE, "leather", ("damage", "accuracy", "evasion"))
    mage = Build(*STATS["mage"], c.RIFLE, "leather", ("magic", "wp", "hp", "accuracy"), True)
    builds = {"plate": plate, "attack": attack, "crit": crit, "rifle": shooter, "mage": mage}
    builds.update({
        "attack, greataxe": variant(attack, weapon=c.GREATAXE),
        "attack, maul": variant(attack, weapon=c.MAUL),
        "attack, pistol set": variant(attack, weapon=c.PISTOL, second=c.GREATSWORD, opener=True),
        "attack in plate": variant(attack, armour="plate"),
        "crit, dagger": variant(crit, weapon=c.DAGGER),
        "crit, axe": variant(crit, weapon=c.AXE),
        "crit, mace": variant(crit, weapon=c.MACE),
        "crit, pistol set": variant(crit, weapon=c.PISTOL, second=c.SWORD, opener=True),
        "crit, Initiative": variant(crit, mods=TEMPO),
        "plate, mace": variant(plate, weapon=c.shielded(c.MACE)),
        "plate, axe": variant(plate, weapon=c.shielded(c.AXE)),
        "plate, no shield": variant(plate, weapon=c.SWORD),
        "plate, pistol set": variant(plate, weapon=c.PISTOL, second=shield, opener=True),
        "pistol": variant(shooter, weapon=c.PISTOL),
        "rifle, sword set": variant(shooter, second=c.SWORD),
        "rifle, Initiative": variant(shooter, mods=("damage", "accuracy", "initiative")),
        "rifle in plate": variant(shooter, armour="plate"),
        "mage in cloth": variant(mage, armour="cloth"),
        "mage in chain": variant(mage, armour="chain"),
        "mage in plate": variant(mage, armour="plate"),
        "mage, pistol": variant(mage, weapon=c.PISTOL),
        "mage, sword": variant(mage, weapon=c.SWORD),
        "mage, sword set": variant(mage, second=c.SWORD),
        "mage, greatsword": variant(mage, weapon=c.GREATSWORD),
        "mage, maul": variant(mage, weapon=c.MAUL),
    })
    if MODE != "none":
        builds.update({
            "plate, Жрица": variant(plate, arcana="Жрица"),
            "attack, Жрица": variant(attack, arcana="Жрица"),
            "crit, Император": variant(crit, arcana="Император"),
            "rifle, Император": variant(shooter, arcana="Император"),
            "mage, Император": variant(mage, arcana="Император"),
        })
    return builds


BUILDS = make_builds()


def abilities(b):
    if MODE == "none":
        return ()
    return sum((kit for limit, kit in ARCANA[b.arcana].items() if limit >= int(MODE)), ())


def fighter(build):
    b = BUILDS[build]
    u = c.Unit(build, 8 if MODE == "none" else int(MODE), b.str, b.end, b.dex, b.per, SKILL, b.will, b.intl, b.weapon, b.armour, infuse=b.infuse, second=b.second, opener=b.opener, abilities=abilities(b))
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

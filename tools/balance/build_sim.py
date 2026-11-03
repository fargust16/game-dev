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
OFF_SKILL = 3
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
    "attack": (17, 13, 9, 6, 3, 3),
    "crit": (9, 9, 11, 16, 3, 3),
    "rifle": (5, 9, 15, 15, 3, 4),
    "mage": (4, 9, 6, 8, 14, 10),
}
MAIN = tuple(STATS)


def variant(base, **changes):
    return Build(**{**base.__dict__, **changes})


def make_builds():
    """Read from the weapons of combat_sim.py as they stand, so a tuning run can change them.

    Every main build carries two Weapon Sets, because at 2 AP a second set is worth more than
    anything it costs: a melee build opens with a pistol, and a shooter keeps a sword. The critical
    build fights with a dagger, the weapon of critical hits."""
    shield = c.shielded(c.SWORD)
    plate = Build(*STATS["plate"], c.PISTOL, "plate", ("hp", "defence", "damage"), second=shield, opener=True, arcana="Император")
    attack = Build(*STATS["attack"], c.PISTOL, "chain", ("damage", "accuracy", "hp"), second=c.GREATSWORD, opener=True, arcana="Император")
    crit = Build(*STATS["crit"], c.PISTOL, "leather", ("crit", "crit_damage", "accuracy"), second=c.DAGGER, opener=True)
    shooter = Build(*STATS["rifle"], c.RIFLE, "leather", ("damage", "accuracy", "evasion"), second=c.SWORD)
    mage = Build(*STATS["mage"], c.RIFLE, "leather", ("magic", "wp", "hp", "accuracy"), True, second=c.SWORD)
    builds = {"plate": plate, "attack": attack, "crit": crit, "rifle": shooter, "mage": mage}
    builds.update({
        "attack, greataxe": variant(attack, second=c.GREATAXE),
        "attack, maul": variant(attack, second=c.MAUL),
        "attack, one set": variant(attack, weapon=c.GREATSWORD, second=None, opener=False),
        "attack in plate": variant(attack, armour="plate"),
        "crit, sword": variant(crit, second=c.SWORD),
        "crit, axe": variant(crit, second=c.AXE),
        "crit, mace": variant(crit, second=c.MACE),
        "crit, one set": variant(crit, weapon=c.DAGGER, second=None, opener=False),
        "crit, Initiative": variant(crit, mods=TEMPO),
        "plate, mace": variant(plate, second=c.shielded(c.MACE)),
        "plate, axe": variant(plate, second=c.shielded(c.AXE)),
        "plate, no shield": variant(plate, second=c.SWORD),
        "plate, one set": variant(plate, weapon=shield, second=None, opener=False),
        "pistol": variant(shooter, weapon=c.PISTOL),
        "rifle, one set": variant(shooter, second=None),
        "rifle, Initiative": variant(shooter, mods=("damage", "accuracy", "initiative")),
        "rifle in plate": variant(shooter, armour="plate"),
        "mage in cloth": variant(mage, armour="cloth"),
        "mage in chain": variant(mage, armour="chain"),
        "mage in plate": variant(mage, armour="plate"),
        "mage, one set": variant(mage, second=None),
        "mage, pistol": variant(mage, weapon=c.PISTOL),
        "mage, sword": variant(mage, weapon=c.SWORD, second=None),
        "mage, greatsword": variant(mage, weapon=c.GREATSWORD, second=None),
        "mage, maul": variant(mage, weapon=c.MAUL, second=None),
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
    """A build trains the skill of the weapon it relies on, the firearm of a shooter or the melee
    weapon after a pistol's opening shot, and has OFF_SKILL in the other. Its 51 points are those of
    Limit 8, so it meets or misses the Attribute Requirements of stage 8 gear."""
    b = BUILDS[build]
    shooter = b.weapon.ranged and not b.opener
    melee, ranged = (OFF_SKILL, SKILL) if shooter else (SKILL, OFF_SKILL)
    u = c.Unit(build, 8 if MODE == "none" else int(MODE), b.str, b.end, b.dex, b.per, melee, b.will, b.intl, b.weapon, b.armour, infuse=b.infuse, second=b.second, opener=b.opener, abilities=abilities(b), ranged_skill=ranged, gear_stage=8)
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

"""Combat rework check (owner, 2026-11-13; 10.2 §5f).

The owner's goal: in a fight a firearm is the first choice, and melee only once an enemy has closed in. No attribute
adds damage; accuracy is the profile attribute of the weapon or attack and its skill; Perception sets the critical
chance; Infusion adds a fixed share of the weapon's base; a Will Contest is won by Intelligence and the Arcana skill.
Everyone in the slice carries a firearm (10.2 §5d), the Wardens a baton beside it, everyone else fists.

It measures:
- the slice's chain with no player, the allies against the main force and then the ritual (21 §8), with and without
  people on a shot's line taking it (10.1 Shot Line), and with the player;
- what pays once an enemy stands beside a shooter, and duels of a melee fighter against shooters from several
  distances, so that a firearm is first at range and melee pays only once an enemy has closed in;
- a Limit's step and the cult's small edge over an allied fighter of the same Limit (10.2 §5b: +1, 56–58 % of duels
  between equals), and the duel across the Arcana, Император's kit against Жрица's (COMBAT-7);
- the Prepared Abilities over the whole scale of Intelligence + Will, and how far a pool of Will Power goes.

Run: python tools/balance/rework_sim.py [fights per line]
"""
import io
import random
import sys
import zlib
from dataclasses import replace
from multiprocessing import Pool

import combat_sim as c
import tactical_sim as t

RUNS = int(sys.argv[1]) if len(sys.argv) > 1 else 2000
INTERCEPT = 0.60
V1 = t.Rules(width=4, enemy_bonus=1)
LINE = replace(V1, intercept=INTERCEPT)


def prepared(intelligence, will, bonus=0):
    """07.2 §4.5: 3 slots, one more for every 14 points of Intelligence + Will above 2, 10 at a sum of 100; items add."""
    return min(10, 3 + max(0, intelligence + will - 2) // 14) + bonus


def player8():
    """The player at Limit 8 as a shooter: Жрица's attributes after 9 free points, a rifle and a pistol set."""
    return c.Unit("hero", 8, 3, 5, 12, 9, 10, will=4, intl=10, weapon=c.RIFLE, second=c.PISTOL, armour="leather", focus=True,
                  abilities=("sleep",), infuse=True)


def player9():
    return c.Unit("hero9", 9, 2, 3, 8, 6, 6, will=2, intl=6, weapon=c.PISTOL, second=c.FISTS, armour="leather", focus=True)


def seeded(name):
    random.seed(zlib.crc32(name.encode()))


SIDES = {
    "allies alone": t.archers,
    "allies alone, people on the line take shots": t.archers,
    "+ allied Limit 8": lambda: t.archers() + [c.ally8()],
    "+ player Limit 9": lambda: t.archers() + [player9()],
    "+ player Limit 8": lambda: t.archers() + [player8()],
    "+ two Wardens": lambda: t.archers() + [c.warden(), c.warden()],
}


def chained(name):
    seeded(name)
    rules = LINE if "line" in name or name.startswith("+") else V1
    return f"  {name:<46} {t.chain(rules, SIDES[name], runs=RUNS):5.1f} %"


def harm(a, weapon, target, adjacent):
    """Expected damage of one attack with weapon, at point blank or from beyond the next cell."""
    main, a.weapon = a.weapon, weapon
    try:
        bonus = -c.ADJACENT if weapon.ranged and adjacent else 0.0
        p = c.hit_chance(a, target, bonus) * a.aim()
        crit = min(0.95, a.crit_raw())
        defence = target.defence() * (c.ARMOUR_RANGED if weapon.ranged and not adjacent else 1)
        base = weapon.base * c.stage_factor(weapon.stage)
        return p * base * (1 + crit * (c.CRIT - 1)) * (1 - c.mitigation(defence))
    finally:
        a.weapon = main


def turn_harm(a, weapon, target, adjacent, swap=0, ap=10):
    return (ap - swap) // weapon.ap * harm(a, weapon, target, adjacent)


PAIRS = {
    "Warden against a cult Limit 9": (c.warden, c.follower9),
    "allied Limit 8 against a cult Limit 8": (c.ally8, c.follower8),
    "player Limit 8 against a cult Limit 8": (player8, c.follower8),
}


def adjacent_table():
    lines = ["What a turn of 10 AP does once an enemy stands beside a shooter (expected damage)"]
    for name, (fa, ft) in PAIRS.items():
        a, tgt = fa(), ft()
        a.reset()
        tgt.reset()
        far = turn_harm(a, a.weapon, tgt, False)
        near = turn_harm(a, a.weapon, tgt, True)
        melee = turn_harm(a, a.second, tgt, True, swap=t.SWAP_AP) if a.second and not a.second.ranged else 0.0
        sword = turn_harm(a, c.SWORD, tgt, True, swap=t.SWAP_AP)
        lines.append(f"  {name:<40} shots from range {far:4.1f}  point blank {near:4.1f}  change to {a.second.name if a.second else '-'} {melee:4.1f}  to a sword {sword:4.1f}")
    return "\n".join(lines)


def melee_fighter():
    """A Limit 8 melee build: a sword and leather, its melee skill trained and its firearm skill not."""
    return c.Unit("M", 8, 6, 10, 12, 6, 12, will=6, intl=3, weapon=c.SWORD, armour="leather", ranged_skill=3)


def shooter(weapon, second):
    return c.Unit("S", 8, 6, 10, 12, 6, 3, will=6, intl=3, weapon=weapon, second=second, armour="leather", ranged_skill=12)


DUELS = {
    "melee v pistol shooter, 2 cells": (melee_fighter, lambda: shooter(c.PISTOL, c.FISTS), 2),
    "melee v pistol shooter, 6 cells": (melee_fighter, lambda: shooter(c.PISTOL, c.FISTS), 6),
    "melee v pistol shooter, 12 cells": (melee_fighter, lambda: shooter(c.PISTOL, c.FISTS), 12),
    "melee v rifleman, 6 cells": (melee_fighter, lambda: shooter(c.RIFLE, c.FISTS), 6),
    "melee v rifleman, 12 cells": (melee_fighter, lambda: shooter(c.RIFLE, c.FISTS), 12),
    "melee v rifleman with a sword set, 12 cells": (melee_fighter, lambda: shooter(c.RIFLE, c.SWORD), 12),
    "cult L8 +1 v cult L8 (equals)": (lambda: t.stronger([c.follower8()], 1)[0], c.follower8, 8),
    "cult L9 +1 v cult L9 (equals)": (lambda: t.stronger([c.follower9()], 1)[0], c.follower9, 8),
    "cult L8 +1 v allied L8 (across the Arcana)": (lambda: t.stronger([c.follower8()], 1)[0], c.ally8, 8),
    "cult L9 +1 v allied L9 (across the Arcana)": (lambda: t.stronger([c.follower9()], 1)[0], c.ally9, 8),
    "cult L8 v two cult L9": (c.follower8, lambda: c.follower9(), 8),
    "cult L7 v two cult L8": (c.senior7, lambda: c.follower8(), 8),
}


def duelled(name):
    seeded(name)
    fa, fb, gap = DUELS[name]
    rules = replace(LINE, gap=gap)
    two = name.startswith("cult L8 v two") or name.startswith("cult L7 v two")
    b = (lambda: [fb(), fb()]) if two else (lambda: [fb()])
    return f"  {name:<46} first side wins {t.duel(rules, lambda: [fa()], b, runs=RUNS):5.1f} %"


def budget():
    lines = ["Will Power of the slice's fighters (07.2 §3.3): uses of each cost from a full pool"]
    for name, u in (("allied Limit 9", c.ally9()), ("allied Limit 8", c.ally8()), ("cult Limit 8", c.follower8()),
                    ("cult Limit 7", c.senior7()), ("player Limit 8", player8())):
        u.reset()
        wp = u.max_wp()
        infusion = c.INFUSION_COST * c.LIMIT_S[u.limit]
        lines.append(f"  {name:<16} WP {wp:5.1f}  ordinary {int(wp // (wp * c.ORDINARY)):2d}  powerful {int(wp // (wp * c.POWERFUL)):2d}  Infusions {int(wp // infusion):2d}  in-fight recovery a round {wp * 0.001:4.2f}")
    return "\n".join(lines)


def slots():
    sums = (2, 10, 16, 20, 30, 44, 58, 72, 86, 100, 150, 200)
    return "Prepared Abilities by Intelligence + Will (07.2 §4.5)\n  " + "  ".join(f"{s}:{prepared(s, 0)}" for s in sums)


MELEE = ("DAGGER", "SWORD", "AXE", "MACE", "GREATSWORD", "GREATAXE", "MAUL")
ORIGINAL = {name: getattr(c, name) for name in MELEE + ("PISTOL", "RIFLE", "BATON", "FISTS")}
# 10.2 §5f's weapon levers, measured 2026-11-13: melee × against combat_sim's table (light × 1.25 and heavy × 1.35 of
# V0, so 0.8 is about V0), the pistol's and the rifle's bases. The chosen line is combat_sim's table.
WEAPON_LEVERS = {
    "melee as the table, pistol 5, rifle 6.75": (1.0, 5.0, 6.75),
    "melee about V0, pistol 5, rifle 6.75": (0.8, 5.0, 6.75),
    "melee about V0, pistol 6.5, rifle 9": (0.8, 6.5, 9.0),
    "melee as the table, pistol 6.5, rifle 9 (chosen)": (1.0, 6.5, 9.0),
    "melee as the table, pistol 7, rifle 9.5": (1.0, 7.0, 9.5),
}


def armed(melee, pistol, rifle):
    """Sets the weapon table for one lever: melee bases × melee against the table, the firearms' bases."""
    for name in MELEE + ("BATON", "FISTS"):
        w = ORIGINAL[name]
        scale = melee if name in MELEE or name == "FISTS" else 1.0
        setattr(c, name, replace(w, base=w.base * scale))
    c.PISTOL = replace(ORIGINAL["PISTOL"], base=pistol)
    c.RIFLE = replace(ORIGINAL["RIFLE"], base=rifle)
    c.ONE_HAND, c.TWO_HAND = c.SWORD, c.GREATSWORD


def levered(name):
    armed(*WEAPON_LEVERS[name])
    out = []
    for duel in ("melee v pistol shooter, 2 cells", "melee v pistol shooter, 12 cells", "melee v rifleman, 12 cells",
                 "melee v rifleman with a sword set, 12 cells"):
        seeded(name + duel)
        fa, fb, gap = DUELS[duel]
        out.append(t.duel(replace(LINE, gap=gap), lambda: [fa()], lambda: [fb()], runs=RUNS))
    seeded(name)
    chain = t.chain(LINE, t.archers, runs=RUNS)
    w, f = c.warden(), c.follower9()
    w.reset()
    f.reset()
    near, baton = turn_harm(w, c.PISTOL, f, True), turn_harm(w, c.BATON, f, True, swap=t.SWAP_AP)
    return (f"  {name:<36} melee wins v pistol at 2 / 12 cells {out[0]:3.0f} / {out[1]:3.0f}, v rifle at 12 {out[2]:3.0f},"
            f" v rifle and sword {out[3]:3.0f}; slice {chain:4.1f} %; Warden beside a cult L9: point blank {near:3.1f}, baton {baton:3.1f}")


def main():
    if len(sys.argv) > 2 and sys.argv[2] == "levers":
        with Pool() as pool:
            print("Weapon levers (melee wins, %; the slice with people on the line)")
            print("\n".join(pool.map(levered, WEAPON_LEVERS)))
        return
    print(slots())
    print()
    print(budget())
    print()
    print(adjacent_table())
    with Pool() as pool:
        print("\nThe slice's chain, the ritual stopped (21 §8), firearms first")
        print("\n".join(pool.map(chained, SIDES)))
        print("\nDuels on the grid, people on the line taking shots")
        print("\n".join(pool.map(duelled, DUELS)))


if __name__ == "__main__":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", line_buffering=True)
    main()

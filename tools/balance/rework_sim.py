"""Combat rework check (owner, 2026-11-13; 10.2 §5f).

The owner's goal: in a fight a firearm is the first choice, and melee only once an enemy has closed in. No attribute
adds damage; accuracy is the profile attribute of the weapon or attack and its skill; Perception sets the critical
chance; Infusion adds a fixed share of the weapon's base; a Will Contest is won by Intelligence and the Arcana skill.
Everyone in the slice carries a firearm (10.2 §5d), the Wardens a baton beside it, everyone else fists.

The owner's answers to COMBAT-1 … COMBAT-12 (2026-11-13): only the initiated have Will Power and all of them infuse;
NPCs prepare by the seed from every active fight ability of their Limits, the four new ones included; the cult is weaker
one on one, an allied fighter winning about 75 % of duels at the same Limit, and wins by numbers. By the owner's
follow-up the cult is a point lower in its base and the lion's share of the gap is gear: the allies are supplied by
their faction's makers and the cult poorly (Outfit V1).

It measures:
- Жрица against Император at equal attributes and no items, alone and five against five, NPCs preparing by the seed
  and with every ability prepared, as a player would choose, so that the Arcana are even;
- an allied fighter against a cult fighter of the same Limit in Outfit V1, so that the ally wins about 75 %;
- the slice's chain with no player, the allies against the main force and then the ritual (21 §8), people on a shot's
  line taking it (10.1 Shot Line), so that it stays at about 22 %, and with the player;
- what pays once an enemy stands beside a shooter, and duels of a melee fighter against shooters from several
  distances, so that a firearm is first at range and melee pays only once an enemy has closed in;
- the Prepared Abilities over the whole scale of Intelligence + Will, and how far a pool of Will Power goes.

Run: python tools/balance/rework_sim.py [fights per line] [levers]
"""
import io
import random
import sys
import zlib
from dataclasses import replace
from multiprocessing import Pool

import combat_sim as c
import item_sim as ims
import tactical_sim as t

RUNS = int(sys.argv[1]) if len(sys.argv) > 1 else 2000
INTERCEPT = 0.60
# Outfit V1 (owner's follow-up to COMBAT-7, 2026-11-13; 10.2 §5f): the Shop Skill each rank's set is made at. The allies'
# faction makes their gear, the faction's Master supplying Limit 7; the cult makes do, its Limit 9 with makeshift gear.
# Authoring defaults: about 15 points under the allies at Limits 9 and 8 and 25 at Limit 7 give the duels about 75 %.
# The player buys at the city's shops.
SUPPLY = {"allies": {9: 26, 8: 50, 7: 75}, "cult": {9: 10, 8: 36, 7: 50}, "player": {9: ims.CITY, 8: ims.CITY}}


def dress(side):
    def wear(u):
        skill = SUPPLY[side].get(u.limit)
        return ims.supplied(u, skill, ims.Items()) if skill else u
    return wear


# The slice: a main force of 6 or 7, half and half, its 2 Limit 8 and Limit 7 senior and the rest Limit 9, and a ritual
# of a Limit 8 senior, 3 conductors and 3 guards (21 §4), 13 or 14 enemies in all.
V1 = t.Rules(width=4, forces=(6, 7), guards=(3,), outfit=dress("cult"))
LINE = replace(V1, intercept=INTERCEPT)
EVEN = t.Rules(width=4, intercept=INTERCEPT)


def prepared(intelligence, will, bonus=0):
    return c.prepared_slots(intelligence, will, bonus)


def allies():
    return [dress("allies")(u) for u in t.archers()]


def player8():
    """The player at Limit 8 as a shooter: Жрица's attributes after 9 free points, a rifle and a pistol set, a city set."""
    return dress("player")(c.Unit("hero", 8, 3, 5, 12, 9, 10, will=4, intl=10, weapon=c.RIFLE, second=c.PISTOL, armour="leather",
                                  focus=True, abilities=("screen", "sleep", "double"), infuse=True))


def player9():
    return dress("player")(c.Unit("hero9", 9, 2, 3, 8, 6, 6, will=2, intl=6, weapon=c.PISTOL, second=c.FISTS, armour="leather",
                                  focus=True, abilities=("screen",), infuse=True))


def seeded(name):
    random.seed(zlib.crc32(name.encode()))


SIDES = {
    "allies alone, no line": allies,
    "allies alone": allies,
    "+ allied Limit 8": lambda: allies() + [dress("allies")(c.ally8())],
    "+ player Limit 9 in a city set": lambda: allies() + [player9()],
    "+ player Limit 8 in a city set": lambda: allies() + [player8()],
    "+ two Wardens": lambda: allies() + [c.warden(), c.warden()],
}


def chained(name):
    seeded(name)
    rules = V1 if "no line" in name else LINE
    return f"  {name:<46} {t.chain(rules, SIDES[name], runs=RUNS):5.1f} %"


# The Император reference at a Limit before the cult's shortfall: the allied row, Will in place of Intelligence.
PEER = {9: ((6, 6, 4, 4, 5), 6, c.PISTOL, "cloth"), 8: ((12, 10, 8, 6, 12), 12, c.PISTOL, "leather"),
        7: ((20, 16, 12, 10, 20), 20, c.RIFLE, "leather")}
ALLY = {9: c.ally9, 8: c.ally8, 7: c.ally7}
CULT = {9: c.follower9, 8: c.follower8, 7: c.senior7}


def emperor(limit, chosen=False):
    row, will, weapon, armour = PEER[limit]
    u = c.Unit(f"E{limit}", limit, *row, will=will, weapon=weapon, second=c.FISTS, armour=armour, infuse=True,
               abilities=c.kit("emperor", limit, 3, will))
    if chosen:
        u.abilities = tuple(a for lim, (act, pas) in c.POOLS["emperor"].items() if lim >= limit for a in act + pas)
    return u


def parity(args):
    limit, n, chosen = args
    seeded(f"parity {limit} {n} {chosen}")
    rules = replace(EVEN, gap=8 if n == 1 else 12)
    win = t.duel(rules, lambda: [ALLY[limit]() for _ in range(n)], lambda: [emperor(limit, chosen) for _ in range(n)], runs=RUNS)
    kind = "every ability prepared" if chosen else "by the seed"
    return f"  Limit {limit}, {n} v {n}, Император {kind:<24} Жрица wins {win:5.1f} %"


def gap(args):
    limit, n = args
    seeded(f"gap {limit} {n}")
    rules = replace(EVEN, gap=8 if n == 1 else 12)
    win = t.duel(rules, lambda: [dress("allies")(ALLY[limit]()) for _ in range(n)],
                 lambda: [dress("cult")(CULT[limit]()) for _ in range(n)], runs=RUNS)
    return f"  Limit {limit}, {n} v {n}{'':<28} allies win {win:5.1f} %"


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
    lines = ["What a turn of 10 AP does once an enemy stands beside a shooter (expected damage, no items)"]
    for name, (fa, ft) in PAIRS.items():
        a, tgt = fa(), ft()
        a.gear, tgt.gear = c.Gear(), c.Gear()
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
    "cult L8 v two cult L9, Outfit V1": (lambda: dress("cult")(c.follower8()), lambda: dress("cult")(c.follower9()), 8),
    "cult L7 v two cult L8, Outfit V1": (lambda: dress("cult")(c.senior7()), lambda: dress("cult")(c.follower8()), 8),
}


def duelled(name):
    seeded(name)
    fa, fb, distance = DUELS[name]
    rules = replace(EVEN, gap=distance)
    b = (lambda: [fb(), fb()]) if " two " in name else (lambda: [fb()])
    return f"  {name:<46} first side wins {t.duel(rules, lambda: [fa()], b, runs=RUNS):5.1f} %"


def budget():
    lines = ["Will Power of the slice's fighters (07.2 §3.3), no items: uses of each cost from a full pool"]
    for name, u in (("allied Limit 9", c.ally9()), ("allied Limit 8", c.ally8()), ("cult Limit 9", c.follower9()),
                    ("cult Limit 8", c.follower8()), ("cult Limit 7", c.senior7()), ("player Limit 8", player8()),
                    ("Warden, no Arcana", c.warden())):
        u.gear = c.Gear()
        u.reset()
        wp = u.max_wp()
        if not wp:
            lines.append(f"  {name:<18} WP   0.0  no Will Power, so no ability and no Infusion (COMBAT-7)")
            continue
        infusion = c.INFUSION_COST * c.LIMIT_S[u.limit]
        lines.append(f"  {name:<18} WP {wp:5.1f}  ordinary {int(wp // (wp * c.ORDINARY)):2d}  powerful {int(wp // (wp * c.POWERFUL)):2d}  Infusions {int(wp // infusion):2d}")
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
        fa, fb, distance = DUELS[duel]
        out.append(t.duel(replace(EVEN, gap=distance), lambda: [fa()], lambda: [fb()], runs=RUNS))
    seeded(name)
    chain = t.chain(LINE, allies, runs=RUNS)
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
        print("\nThe Arcana at equal attributes and no items (Жрица's row against Император's)")
        print("\n".join(pool.map(parity, [(lim, n, ch) for lim in (9, 8, 7) for n in (1, 5) for ch in (False, True)])))
        print("\nAn allied fighter against a cult fighter of the same Limit, Outfit V1")
        print("\n".join(pool.map(gap, [(lim, n) for lim in (9, 8, 7) for n in (1, 5)])))
        print("\nThe slice's chain, the ritual stopped (21 §8), firearms first, Outfit V1")
        print("\n".join(pool.map(chained, SIDES)))
        print("\nDuels on the grid, people on the line taking shots")
        print("\n".join(pool.map(duelled, DUELS)))


if __name__ == "__main__":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", line_buffering=True)
    main()

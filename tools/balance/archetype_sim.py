"""Character Archetypes measured again after the combat rework (07 Character Life; S5b.1, 2026-11-13).

A new character starts with every attribute at 1 and either spends 9 free points or takes an archetype of 11 points.
Since the combat rework (10.2 §5f) no attribute adds damage, a firearm aims by Dexterity and its skill, Perception sets
the critical chance, and an item gives about half below its Attribute Requirement: a pistol, a rifle and leather ask
Dexterity 3, chain Strength 5, plate Strength 7. So the old Боец, Strength +5, gives little in a fight but chain.

It measures each start, uninitiated and with every skill at 1, in a pistol and leather (or chain where Strength allows):
- a pistol duel with a Warden's like, an uninitiated fighter of a few points (10.2 §5a), from 6 cells;
- a wolf from 8 cells (beast_sim.py);
- its HP, AP, the load it carries comfortably (20 kg + Strength, 19 §5), its critical chance, Initiative's mean.

Run: python tools/balance/archetype_sim.py [fights per line]
"""
import random
import sys
import zlib
from dataclasses import replace

import beast_sim as b
import combat_sim as c
import tactical_sim as t

RUNS = int(sys.argv[1]) if len(sys.argv) > 1 else 4000
NAMES = ("str", "end", "dex", "per", "intl", "will", "luck")

# Added to the starting 1. The old spreads are 07's of 2026-11-02; the new Боец is this measurement's (authoring default).
STARTS = {
    "Боец, old (Сила 5, Выносливость 4, Ловкость 2)": {"str": 5, "end": 4, "dex": 2},
    "Боец, new (Сила 4, Выносливость 3, Ловкость 4)": {"str": 4, "end": 3, "dex": 4},
    "Сыщик (Восприятие 5, Интеллект 4, Ловкость 2)": {"per": 5, "intl": 4, "dex": 2},
    "Оборванец (Воля 5, Удача 4, Ловкость 2)": {"will": 5, "luck": 4, "dex": 2},
    "9 points, a shooter (Ловкость 4, Выносливость 3, Восприятие 2)": {"dex": 4, "end": 3, "per": 2},
    "9 points, Ловкость left at 1 (Сила 5, Выносливость 4)": {"str": 5, "end": 4},
}


def values(spread):
    return {n: 1 + spread.get(n, 0) for n in NAMES}


def start(spread, armour):
    v = values(spread)
    return c.Unit("start", None, v["str"], v["end"], v["dex"], v["per"], 1, will=v["will"], intl=v["intl"], luck=v["luck"],
                  weapon=c.PISTOL, second=c.FISTS, armour=armour)


def armour_for(spread):
    """Chain where Strength meets it, else leather: the best the start can wear in full."""
    return "chain" if values(spread)["str"] >= 5 else "leather"


def seeded(name):
    random.seed(zlib.crc32(name.encode()))


def line(name, worn=None):
    spread = STARTS[name]
    armour = worn or armour_for(spread)
    u = start(spread, armour)
    u.reset()
    seeded(name + " warden")
    warden = t.duel(t.Rules(width=4, gap=6), lambda: [start(spread, armour)], lambda: [c.warden()], runs=RUNS)
    seeded(name + " wolf")
    wolf = t.duel(t.Rules(width=4, gap=8), lambda: [start(spread, armour)], lambda: [b.wolf()], runs=RUNS)
    v = values(spread)
    crit = min(0.95, u.crit_raw()) * 100
    initiative = (v["dex"] + v["per"]) / 2 + 10.5
    return (f"  {name:<62} {armour:<7} HP {u.max_hp():4.1f}  AP {10 + v['dex'] / 20:5.2f}  load {20 + v['str']:2d} kg"
            f"  crit {crit:4.1f} %  Initiative {initiative:4.1f}  v Warden {warden:5.1f} %  v wolf {wolf:5.1f} %")


def main():
    print(f"Starts against the slice's first fights ({RUNS} fights a line)")
    for name in STARTS:
        print(line(name))
    print("Everyone in leather, so the armour's part is apart")
    for name in STARTS:
        print(line(name, "leather"))
    print("The very start: plain clothes with no defence and a pistol (11; a chain or leather body costs 500 bronze, the whole purse)")
    for name in STARTS:
        print(line(name, "none"))


if __name__ == "__main__":
    main()

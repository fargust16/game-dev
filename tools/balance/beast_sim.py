"""Beasts of the slice for 10.2 Combat System Draft §5b.

Wolves, boars, and glowing beasts against the slice's characters, on the Combat Numbers V0 of
combat_sim.py. A wolf's or a boar's critical hit causes Bleeding, 1 % of maximum HP a round, which
ends by itself at Endurance / 2 % a round (10.2 § First Aid and conditions in the slice). A glowing
beast's bite poisons, which only blocks recovery, so it changes no fight here.

Run: python tools/balance/beast_sim.py
"""
import random

import combat_sim as cs
from combat_sim import Unit, Weapon, Gear, alive, take_turn

RUNS = 4000
cs.ARMOUR["hide"] = (4, 0)
cs.ARMOUR["thick hide"] = (8, 0)
cs.ARMOUR_WEIGHT["hide"] = 0.0
cs.ARMOUR_WEIGHT["thick hide"] = 0.0

BITE = Weapon("bite", 5, 6, weight=0.0, crit=0.05)
TUSK = Weapon("tusk", 5.5, 5, weight=0.0)


def wolf(glowing=False):
    u = Unit("wolf", None, 4, 3, 6, 5, 4, will=3, weapon=BITE, armour="hide")
    return glow(u, 5, 1.0) if glowing else u


def boar(glowing=False):
    u = Unit("boar", None, 8, 8, 2, 2, 4, will=3, weapon=TUSK, armour="thick hide")
    return glow(u, 2, 0.25) if glowing else u


def glow(u, add, hp):
    """A glowing beast walks alone, with more to its attributes, skill and HP, and a poisonous bite."""
    for a in ("str", "end", "dex", "per", "skill"):
        setattr(u, a, getattr(u, a) + add)
    u.ranged_skill = u.skill
    u.gear = Gear(hp=hp)
    u.name = "glowing " + u.name
    u.extra["glowing"] = True
    return u


def beast(u):
    return u.name.endswith("wolf") or u.name.endswith("boar")


def strike_with_bleed(a, t, *args, **kwargs):
    """A critical hit deals more than the largest ordinary blow, so a beast's critical hit is read from the damage."""
    before = t.hp
    hit = ORIGINAL_STRIKE(a, t, *args, **kwargs)
    if hit and beast(a):
        w = a.weapon
        largest = w.base * (1 + a.str / 50) * 1.15 * (1 - cs.mitigation(t.defence() * cs.CRIT_PIERCE))
        if before - t.hp > largest:
            t.extra["bleeding"] = True
    return hit


ORIGINAL_STRIKE = cs.strike
cs.strike = strike_with_bleed


def fight(side_a, side_b, max_rounds=60):
    units = side_a + side_b
    for u in units:
        u.reset()
        u.extra.pop("bleeding", None)
    order = sorted(units, key=lambda u: -u.initiative())
    for rnd in range(1, max_rounds + 1):
        for u in order:
            if u.hp <= 0:
                continue
            own, enemy = (side_a, side_b) if u in side_a else (side_b, side_a)
            take_turn(u, own, enemy, rnd)
        for u in units:
            if u.hp > 0 and u.extra.get("bleeding"):
                u.hp -= 0.01 * u.max_hp()
                if random.random() < u.end / 2 / 100:
                    u.extra["bleeding"] = False
        if not alive(side_a):
            return side_b
        if not alive(side_b) or len(alive(side_b)) < len(side_b) >= 2:
            return side_a
    return None


def report(title, make_a, make_b):
    wins = 0
    bled = 0
    left = 0.0
    for _ in range(RUNS):
        a, b = make_a(), make_b()
        w = fight(a, b)
        if w is a:
            wins += 1
            left += sum(max(0, u.hp) for u in a) / sum(u.max_hp() for u in a)
        bled += any(u.extra.get("bleeding") is not None for u in a)
    print(f"{title:<46} wins {wins / RUNS * 100:5.1f} %  HP left on win {left / max(1, wins) * 100:3.0f} %  bled {bled / RUNS * 100:3.0f} %")


def new_character():
    """A new character with the 9 free points spread for a fight and a club, no armour."""
    return [Unit("new", None, 5, 4, 3, 2, 1, will=1, weapon=Weapon("club", 4, 5), armour="none", focus=True)]


def new_armed():
    """The same character with a bought sword and leather."""
    return [Unit("new", None, 5, 4, 3, 2, 1, will=1, weapon=cs.SWORD, armour="leather", focus=True)]


def hero9():
    return [cs.hero9()]


def hero9_set():
    """A Limit 9 hero in a city-shop set: about +10 % damage and +5 defence, a rough reading of item_sim.py."""
    h = cs.hero9(armour="leather")
    h.gear = Gear(damage=0.10, defence=5, accuracy=3)
    return [h]


def hero8():
    return [cs.hero(limit=8, abilities=())]


def main():
    random.seed(7)
    for name, side in (("new character, club", new_character), ("new character, sword, leather", new_armed),
                       ("Limit 9 hero, sword, cloth", hero9), ("Limit 9 hero, city set", hero9_set),
                       ("Limit 8 hero, leather", hero8)):
        print(name)
        report("  one wolf", side, lambda: [wolf()])
        report("  two wolves", side, lambda: [wolf(), wolf()])
        report("  three wolves", side, lambda: [wolf(), wolf(), wolf()])
        report("  one boar", side, lambda: [boar()])
        report("  glowing wolf", side, lambda: [wolf(True)])
        report("  glowing boar", side, lambda: [boar(True)])


if __name__ == "__main__":
    main()

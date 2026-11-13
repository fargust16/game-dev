"""Beasts of the slice for 10.2 Combat System Draft §5b.

Wolves, boars, and glowing beasts against the slice's characters, on the Combat Numbers V0 of
combat_sim.py. A wolf's or a boar's critical hit causes Bleeding, 1 % of maximum HP a round, which
ends by itself at Endurance / 2 % a round (10.2 § First Aid and conditions in the slice). A glowing
beast's bite poisons, which only blocks recovery, so it changes no fight here.

A blow is 10.2 §5b's, as the slice's C# fight rules strike it (S3a.2): a critical melee blow halves
physical defence, and a weapon above its user aims worse. combat_sim.strike predates the first rule.
A tie in Initiative goes to the higher Dexterity, then at random. The table of 10.2 *Beasts of the
slice* is this script's output at seed 7, and the C# parity test lands within 3 points of it.

Since the combat rework (10.2 §5f) no attribute adds damage and a light weapon aims by Dexterity and its skill, so a
natural weapon takes in its beast's old Strength factor, base × (1 + Strength / 50), and its To hit makes up the
change in aim, ((Strength + Dexterity + Perception) / 3 − Dexterity) / 2: the beasts fight as before. The glowing
wolf's bite has its own base; the glowing boar keeps the boar's tusks and loses 3 % of its harm.

Run: python tools/balance/beast_sim.py
"""
import random

import combat_sim as cs
from combat_sim import Unit, Weapon, Gear, alive, take_turn

RUNS = 40000
cs.ARMOUR["hide"] = (4, 0)
cs.ARMOUR["thick hide"] = (8, 0)
cs.ARMOUR_WEIGHT["hide"] = 0.0
cs.ARMOUR_WEIGHT["thick hide"] = 0.0

BITE = Weapon("bite", 5.4, 6, weight=0.0, crit=0.05, accuracy=-0.5)
GLOWING_BITE = Weapon("glowing bite", 5.9, 6, weight=0.0, crit=0.05, accuracy=-0.5)
TUSK = Weapon("tusk", 6.4, 5, weight=0.0, accuracy=1.0)


def wolf(glowing=False):
    u = Unit("wolf", None, 4, 3, 6, 5, 4, will=3, weapon=BITE, armour="hide")
    if glowing:
        u.weapon = GLOWING_BITE
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


def strike(a, t, foes_of_t=(), side_of_t=(), bonus_crit=0.0, crit_extra=0.0, mult=1.0, unguarded=False, surround=0):
    """combat_sim.strike with 10.2 §5b's critical pierce and aim; a beast's critical hit that deals damage bleeds."""
    bonus_hit = cs.SURROUND_HIT.get(surround, 0.0)
    if t.exposed:
        bonus_hit += 0.10
        t.exposed = False
    if random.random() >= cs.hit_chance(a, t, bonus_hit) * a.aim():
        return False
    raw = a.crit_raw() + bonus_crit
    crit = 1.0
    if random.random() < min(0.95, raw):
        crit = cs.CRIT + max(0, raw - 0.95) + crit_extra + a.gear.crit_damage
    pierce = cs.CRIT_PIERCE if crit > 1 and not a.weapon.ranged else 0.0
    defence = t.defence(side_of_t) * (cs.ARMOUR_RANGED if a.weapon.ranged else 1) * (1 - pierce)
    damage = a.damage_roll() * crit * mult * (1 - cs.mitigation(defence))
    if t.brace:
        damage *= 1 - cs.BRACE_REDUCTION
    if not unguarded and not a.weapon.ranged and t.weapon.guard and t.reserve >= cs.REACTION_AP and not t.asleep and not t.skip:
        t.reserve -= cs.REACTION_AP
        if random.random() < t.weapon.guard_chance:
            damage *= 1 - t.weapon.guard_absorb
    t.hp -= damage
    if t.asleep and damage > 0:
        t.asleep = 0
    if crit > 1 and damage > 0 and t.hp > 0 and beast(a):
        t.extra["bleeding"] = True
    return True


cs.strike = strike


def fight(side_a, side_b, max_rounds=60):
    units = side_a + side_b
    for u in units:
        u.reset()
        u.extra.pop("bleeding", None)
    order = sorted(units, key=lambda u: (-u.initiative(), -u.dex, random.random()))
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
    """A new character with the 9 free points spread for a fight and a club, no armour; since the combat rework the
    club's base is 5, light melee × 1.25 as 10.2 §5f."""
    return [Unit("new", None, 5, 4, 3, 2, 1, will=1, weapon=Weapon("club", 5, 5), armour="none", focus=True)]


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

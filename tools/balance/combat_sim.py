"""Combat balance check for 10.2 Combat System Draft.

Monte Carlo over the combat numbers. Since the combat rework (owner, 2026-11-13, 10.2 §5f) no attribute adds
damage: accuracy is the profile attribute of the weapon or attack and its skill, Perception sets the critical
chance, Infusion adds a fixed share of the weapon's base, and a Will Contest is won by Intelligence and the
Arcana skill against the target's Will. It has no grid: melee fighters spend the
AP of their first turn on closing in, and later turns on attacks. It checks hits
to kill, fight length, and the balance of the slice's fights.

Run: python tools/balance/combat_sim.py
"""
import io
import math
import os
import random
import sys
from dataclasses import dataclass, field, replace

LIMIT_S = {None: 1.00, 9: 1.00, 8: 1.15, 7: 1.32, 6: 1.52, 5: 1.75, 4: 2.01, 3: 2.31, 2: 2.66, 1: 3.06, 0: 3.52}
RUNS = 4000
CLOSE_IN_CELLS = 6
REACTION_AP = 2
SURROUND_HIT = {2: 0.10, 3: 0.20, 4: 0.20}
ORDINARY = 0.15
POWERFUL = 0.33
BRACE_REDUCTION = 0.25
SUPPORT_DEFENCE = 3  # 05.1 Опора, 5 until the balance after COMBAT-7 (10.2 §5f)
SLEEP_ROUNDS = 1
SLEEP_AP = 2
CONDUCTING_AP = 3
STAGE_STEP = 0.35
CRIT = 2.0
ARMOUR_RANGED = 2.0
ADJACENT = 0.10
CRIT_PIERCE = 0.5
INFUSION_COST = 5.0
# 10.2 §5f: Infusion adds this share of the weapon's base as magical damage, with no attribute in it.
INFUSION_SHARE = 0.40
INFUSION_ARMOUR = {"chain": 0.5, "plate": 0.0}
ARMOUR_WEIGHT = {"none": 0.0, "cloth": 5.7, "leather": 11.3, "chain": 21.2, "plate": 31.8}
CARRIED = 3.0
LOAD = ((1.00, 0, 0), (1.25, 8, 0), (1.50, 16, 1), (2.00, 24, 2))


def stage_factor(stage):
    return 1 + STAGE_STEP * (9 - stage)


@dataclass(frozen=True)
class Weapon:
    name: str
    base: float
    ap: int
    ranged: bool = False
    stage: int = 9
    guard: str = ""
    guard_chance: float = 0.0
    guard_absorb: float = 0.0
    stealth_mult: float = 0.0
    range: int = 12
    weight: float = 2.0
    accuracy: float = 0.0
    crit: float = 0.0
    stun: float = 0.0
    cleave: float = 0.0
    req: tuple = ()
    off: object = None
    # 10.2 §5f: the attribute that aims it, Dexterity for a light weapon and Strength for a heavy one; a heavy
    # weapon (heavy firearms) halves movement; a subduing one (the Wardens' baton) leaves its victim unconscious.
    profile: str = "dex"
    heavy: bool = False
    subdue: bool = False


SHIELD_BLOCK = 0.50


def shielded(w):
    return replace(w, name=w.name + " and shield", guard="block", guard_chance=SHIELD_BLOCK, guard_absorb=0.8, weight=w.weight + 4.0)


# Kinds of weapon (10.2 §5b): a dagger is the truest and the likeliest to find a critical hit, a sword the
# quickest and truer than an axe or a mace, an axe hits as hard as a sword and finds more critical hits,
# and a mace hits hardest, finds the fewest, and may stun. A greatsword also catches a second enemy.
# Attribute Requirements (10.2 §5b), at stage 9; an item asks (10 − stage) times as much, so stage 8 asks double.
# Below them a weapon hits and armour defends in proportion: at half the requirement, half as well.
# 10.2 §5f: Strength no longer adds damage, so the melee bases take in the old Strength factor of their users, light
# weapons × 1.25 and heavy ones × 1.35 of V0, while the firearms rise more (pistol 6.5, rifle 9), so that a firearm is
# first at range (rework_sim.py) and the triangle of build_sim.py holds with a shield as Half Cover against shots. A
# one-handed weapon is light and aims by Dexterity, a two-handed one heavy and by Strength, and each asks only that.
DAGGER = Weapon("dagger", 3.75, 3, guard="parry", guard_chance=0.30, guard_absorb=1.0, stealth_mult=1.0, weight=1.0, accuracy=10, crit=0.25, req=(("dex", 4),))
SWORD = Weapon("sword", 6.25, 4, guard="parry", guard_chance=0.40, guard_absorb=1.0, accuracy=5, req=(("dex", 3),))
AXE = Weapon("axe", 6.25, 5, guard="parry", guard_chance=0.30, guard_absorb=1.0, weight=2.2, crit=0.20, req=(("dex", 4),))
MACE = Weapon("mace", 8.1, 5, guard="parry", guard_chance=0.30, guard_absorb=1.0, weight=2.5, crit=-0.03, stun=0.05, req=(("dex", 5),))
GREATSWORD = Weapon("greatsword", 11.5, 6, guard="block", guard_chance=0.40, guard_absorb=0.6, weight=4.5, accuracy=5, cleave=0.5, req=(("str", 7),), profile="str")
GREATAXE = Weapon("greataxe", 11.5, 7, guard="block", guard_chance=0.40, guard_absorb=0.6, weight=5.0, crit=0.22, req=(("str", 7),), profile="str")
MAUL = Weapon("maul", 14.2, 7, guard="block", guard_chance=0.40, guard_absorb=0.6, weight=6.0, crit=-0.03, stun=0.08, req=(("str", 8),), profile="str")
# 10.2 §5f: the Wardens' baton subdues, and fists are the dagger's numbers at half damage (§5c), with no guard.
BATON = Weapon("baton", 5, 4, guard="parry", guard_chance=0.30, guard_absorb=1.0, weight=1.0, accuracy=5, req=(("dex", 3),), subdue=True)
FISTS = Weapon("fists", 1.875, 3, weight=0.0, accuracy=10, crit=0.25)
ONE_HAND = SWORD
TWO_HAND = GREATSWORD
SHIELDED = shielded(SWORD)


def paired(main, off):
    """Two weapons (10.2 §5b, owner 2026-11-11): a one-handed melee weapon in each hand, never with a shield or a
    firearm. Each blow is struck with either hand at that weapon's own AP, aim, damage, critical chance and trait,
    so the AP a turn leaves after the heavier weapon's blows can pay for the lighter one's. The pair parries with the
    better weapon; both weights count in Load, and each weapon keeps its requirements. Checked by build_sim.py."""
    return replace(main, name=f"{main.name} and {off.name}", off=off, weight=main.weight + off.weight,
                   guard_chance=max(main.guard_chance, off.guard_chance))
RIFLE = Weapon("rifle", 9, 6, True, range=16, weight=4.0, req=(("dex", 3),))
PISTOL = Weapon("pistol", 6.5, 4, True, range=6, weight=1.2, accuracy=-10, req=(("dex", 3),))

ARMOUR = {"none": (0, 0), "cloth": (2, 20), "leather": (8, 8), "chain": (16, 3), "plate": (24, 0)}
ARMOUR_REQ = {"leather": (("dex", 3),), "chain": (("str", 5),), "plate": (("str", 7),)}


def staged(weapon, stage):
    return replace(weapon, stage=stage)


@dataclass
class Gear:
    """What a fighter's modifiers add up to."""
    damage: float = 0.0
    accuracy: float = 0.0
    crit: float = 0.0
    crit_damage: float = 0.0
    hp: float = 0.0
    defence: float = 0.0
    evasion: float = 0.0
    ap: float = 0.0
    initiative: float = 0.0
    will: float = 0.0
    magic: float = 0.0
    wp: float = 0.0
    ward: float = 0.0


@dataclass(eq=False)
class Unit:
    name: str
    limit: int | None
    str: int
    end: int
    dex: int
    per: int
    skill: int
    will: int = 5
    intl: int = 3
    weapon: Weapon = ONE_HAND
    armour: str = "leather"
    armour_stage: int = 9
    luck: int = 1
    focus: bool = False
    abilities: tuple = ()
    guarded: bool = False
    conducting: bool = False
    hp: float = 0
    wp: float = 0
    reserve: int = 0
    skip: int = 0
    asleep: int = 0
    brace: bool = False
    exposed: bool = False
    stance: bool = False
    struck: bool = False
    infuse: bool = False
    fired: bool = False
    second: object = None
    opener: bool = False
    reacted: bool = False
    disoriented: int = 0
    dazed: bool = False
    taunter: object = None
    held: int = -1
    frac: float = 0.0
    burden: tuple = (0, 0)
    extra: dict = field(default_factory=dict)
    gear: Gear = field(default_factory=Gear)
    ranged_skill: int | None = None
    gear_stage: int | None = None
    arcana: int | None = None

    def __post_init__(self):
        """skill is the skill of the melee weapon it carries; a fighter given no firearm skill shoots with the same
        value, and one given no Arcana skill (10.2 §5f) casts with it too."""
        if self.ranged_skill is None:
            self.ranged_skill = self.skill
        if self.arcana is None:
            self.arcana = self.skill

    def reset(self):
        self.burden = self.load()
        self.hp = self.max_hp()
        self.wp = self.max_wp()
        self.reserve = 0
        self.skip = 0
        self.asleep = 0
        self.brace = False
        self.exposed = False
        self.frac = random.random()

    def load(self):
        """What the worn armour, the held weapons and the rest of the load cost in Evasion and AP, as a share of
        Carrying Capacity. It stands beside the Attribute Requirement of heavy armour: without it plate beats shooters 85 %."""
        share = (ARMOUR_WEIGHT[self.armour] + self.weapon.weight + (self.second.weight if self.second else 0.0) + CARRIED) / (20 + self.str)
        return next((band[1:] for band in LOAD if share <= band[0]), LOAD[-1][1:])

    def max_hp(self):
        return (20 + 0.8 * self.end) * LIMIT_S[self.limit] * (1 + self.gear.hp)

    def max_wp(self):
        """07.2 (owner, 2026-11-13, COMBAT-7): only a character with an Arcana has Will Power; without one, Will is
        only its resistance."""
        if self.limit is None:
            return 0.0
        return (20 + 0.8 * self.will) * LIMIT_S[self.limit] * (1 + self.gear.wp)

    def ap(self):
        whole = 10 + self.dex / 20 + self.gear.ap - self.burden[1] + self.frac
        self.frac = whole - math.floor(whole)
        return math.floor(whole) - (CONDUCTING_AP if self.conducting else 0)

    def initiative(self):
        return (self.dex + self.per) / 2 + self.gear.initiative + random.randint(1, 20)

    def accuracy(self):
        """10.2 §5f: the weapon's profile attribute and the skill of its kind, halved; Perception aims no more."""
        w = self.weapon
        return (getattr(self, w.profile) + (self.ranged_skill if w.ranged else self.skill)) / 2 + self.gear.accuracy + w.accuracy

    def magic_accuracy(self):
        """10.2 §5f: a targeted ability aims by Intelligence and the Arcana skill; Will never aims."""
        return (self.intl + self.arcana) / 2 + self.gear.accuracy

    def evasion(self):
        return 0 if self.asleep else (self.dex + self.per) / 2 + self.gear.evasion - self.burden[0]

    def damage_roll(self):
        w = self.weapon
        return w.base * stage_factor(w.stage) * (1 + self.gear.damage) * random.uniform(0.85, 1.15)

    def crit_raw(self):
        return (0.05 + 0.90 * (self.per - 1) / 99 + self.gear.crit + self.weapon.crit) * (1 + self.luck / 100)

    def defence(self, side=()):
        support = SUPPORT_DEFENCE if any("support" in u.abilities and u.hp > 0 and u is not self for u in side) else 0
        return ARMOUR[self.armour][0] * stage_factor(self.armour_stage) * self.armour_share() + support + self.gear.defence

    def fit(self, req, stage):
        """The share of an item that works: the attribute furthest below its requirement over that requirement,
        so half the requirement gives half the item. gear_stage, when set, stands for the stage of the fighter's
        gear: build_sim.py keeps its items at Stage Factor 1 for comparison but asks the requirements of the Limit
        its builds stand for."""
        scale = 10 - (self.gear_stage or stage)
        return min((min(1.0, getattr(self, a) / (v * scale)) for a, v in req), default=1.0)

    def meets(self, req, stage):
        return self.fit(req, stage) == 1.0

    def aim(self):
        """Below its weapon's Attribute Requirements a fighter hits less often, in proportion to the shortfall."""
        return self.fit(self.weapon.req, self.weapon.stage)

    def armour_share(self):
        """Below its armour's Attribute Requirements a fighter gets that share of the armour's defence."""
        return self.fit(ARMOUR_REQ.get(self.armour, ()), self.armour_stage)

    def can_spend(self, share):
        return self.wp > self.max_wp() * share


def mitigation(defence):
    return min(0.75, defence / (defence + 50))


# 07.1 §10, S5b.3 (owner's direction, 2026-11-13, to be checked): Perception as every attack's secondary attribute by
# 07.1 §5, the target's Evasion standing for the task's difficulty, at most +10 % of the hit chance, relatively.
# Adopted in S5b.3 (perception_sim.py); PERCEPTION_SECONDARY=0 turns it off, read so that a Pool's workers see it too.
PERCEPTION_SECONDARY = os.environ.get("PERCEPTION_SECONDARY", "1") == "1"
SECONDARY_SHARE = 0.10


def secondary(a, t):
    """07.1 §5: 1 + 0.10 × min(1, Perception / difficulty); the factor a hit chance is multiplied by before Luck."""
    if not PERCEPTION_SECONDARY:
        return 1.0
    difficulty = t.evasion()
    return 1 + SECONDARY_SHARE * (1.0 if difficulty <= 0 else min(1.0, a.per / difficulty))


def hit_chance(a, t, bonus=0.0):
    p = (0.70 + (a.accuracy() - t.evasion()) / 100 + bonus) * secondary(a, t)
    return min(0.95, max(0.05, p * (1 + a.luck / 100)))


CONTEST_SCALE = 100


def resist_check(a, t, will_only=False, base=0.50):
    """10.2 §5f Will Contest: the caster's magical accuracy against the target's Will. will_only is kept for the
    levers of before the rework and now means the same."""
    power = a.magic_accuracy()
    p = base + (power - t.will - t.gear.will) / CONTEST_SCALE
    return random.random() < min(0.95, max(0.05, p * (1 + a.luck / 100)))


def strike(a, t, foes_of_t=(), side_of_t=(), bonus_crit=0.0, crit_extra=0.0, mult=1.0, unguarded=False, surround=0):
    bonus_hit = SURROUND_HIT.get(surround, 0.0)
    if t.exposed:
        bonus_hit += 0.10
        t.exposed = False
    if random.random() >= hit_chance(a, t, bonus_hit):
        return False
    raw = a.crit_raw() + bonus_crit
    crit = 1.0
    if random.random() < min(0.95, raw):
        crit = CRIT + max(0, raw - 0.95) + crit_extra + a.gear.crit_damage
    defence = t.defence(side_of_t) * (ARMOUR_RANGED if a.weapon.ranged else 1)
    damage = a.damage_roll() * crit * mult * (1 - mitigation(defence))
    if t.brace:
        damage *= 1 - BRACE_REDUCTION
    if not unguarded and not a.weapon.ranged and t.weapon.guard and t.reserve >= REACTION_AP and not t.asleep and not t.skip:
        t.reserve -= REACTION_AP
        if random.random() < t.weapon.guard_chance:
            damage *= 1 - t.weapon.guard_absorb
    t.hp -= damage
    if t.asleep and damage > 0:
        t.asleep = 0
    return True


def alive(side):
    return [u for u in side if u.hp > 0]


def deepest(foes):
    return min(foes, key=lambda f: (9 if f.limit is None else f.limit, f.hp))


def take_turn(u, own, enemy, rnd):
    u.brace = False
    if u.skip:
        u.skip -= 1
        u.reserve = 0
        return
    if u.asleep:
        u.asleep -= 1
        u.reserve = 0
        return
    ap = u.ap()
    if rnd == 1 and not u.weapon.ranged and not u.conducting:
        ap -= CLOSE_IN_CELLS
    foes = alive(enemy)
    if not foes:
        return
    awake = [f for f in foes if not f.asleep and not f.skip]
    if "sleep" in u.abilities and len(awake) >= 2 and ap >= SLEEP_AP and u.can_spend(POWERFUL):
        target = deepest(awake)
        u.wp -= u.max_wp() * POWERFUL
        ap -= SLEEP_AP
        if resist_check(u, target):
            target.asleep = SLEEP_ROUNDS
        else:
            u.exposed = True
    if "stand" in u.abilities and awake and ap >= 4 + u.weapon.ap and u.can_spend(ORDINARY) and (len(foes) >= 2 or rnd > 1):
        target = deepest(awake)
        u.wp -= u.max_wp() * ORDINARY
        ap -= 4
        if resist_check(u, target):
            target.skip = 1
        else:
            u.exposed = True
    if "brace" in u.abilities and len(foes) >= 2 and ap >= 3 + u.weapon.ap and u.can_spend(ORDINARY):
        u.wp -= u.max_wp() * ORDINARY
        ap -= 3
        u.brace = True
    while ap >= u.weapon.ap:
        foes = alive(enemy)
        if not foes:
            break
        if u.guarded and ap < 2 * u.weapon.ap:
            break
        awake = [f for f in foes if not f.asleep] or foes
        target = min(awake, key=lambda f: f.hp) if u.focus else random.choice(awake)
        # No grid: with three or more on the side the target counts as surrounded by two, with two at even odds.
        attackers = len(alive(own))
        surround = 2 if not u.weapon.ranged and (attackers >= 3 or (attackers == 2 and random.random() < 0.5)) else 0
        if "crush" in u.abilities and not u.weapon.ranged and ap >= u.weapon.ap + 1 and u.can_spend(POWERFUL):
            u.wp -= u.max_wp() * POWERFUL
            ap -= u.weapon.ap + 1
            if not strike(u, target, own, enemy, mult=1.5, unguarded=True, surround=surround):
                u.exposed = True
            continue
        strike(u, target, own, enemy, surround=surround)
        ap -= u.weapon.ap
    u.reserve = ap


def fight(side_a, side_b, max_rounds=60, opener=None, objective=None, wounded_a=False):
    units = side_a + side_b
    for u in (side_b if wounded_a else units):
        u.reset()
    if opener is not None:
        hero_unit = side_a[0]
        strike(hero_unit, opener, side_a, side_b, bonus_crit=0.5, crit_extra=hero_unit.weapon.stealth_mult, unguarded=True)
    order = sorted(units, key=lambda u: -u.initiative())
    for rnd in range(1, max_rounds + 1):
        for u in order:
            if u.hp <= 0:
                continue
            if objective is not None and objective.hp <= 0:
                return rnd, side_a
            own, enemy = (side_a, side_b) if u in side_a else (side_b, side_a)
            take_turn(u, own, enemy, rnd)
        if objective is not None and objective.hp <= 0:
            return rnd, side_a
        if not alive(side_a):
            return rnd, side_b
        if not alive(side_b):
            return rnd, side_a
    return max_rounds, None


def report(title, a, b, opener=None, objective=None):
    wins = 0
    rounds = 0
    left = 0.0
    for _ in range(RUNS):
        r, w = fight(a, b, opener=opener, objective=objective)
        rounds += r
        if w is a:
            wins += 1
            left += sum(max(0, u.hp) for u in a) / sum(u.max_hp() for u in a)
    print(f"{title:<52} side A wins {wins / RUNS * 100:5.1f} %  rounds {rounds / RUNS:4.1f}  HP left on win {left / max(1, wins) * 100:3.0f} %")
    return wins / RUNS


# 05.1: the fight abilities each Limit of the slice's two Arcana gives, as (actives, passives). Император's are the
# cult's, Жрица's the allies'. Завеса (Жрица, Limit 7) is not modelled.
POOLS = {
    "emperor": {9: (("brace", "dash"), ()), 8: (("stand", "press"), ("support",)), 7: (("pillar", "crush"), ())},
    "priestess": {9: (("screen",), ()), 8: (("sleep", "double"), ()), 7: (("sting",), ())},
}


def prepared_slots(intelligence, will, bonus=0):
    """07.2 §4.5: 3 slots, one more for every 14 points of Intelligence + Will above 2, 10 at a sum of 100; items add."""
    return min(10, 3 + max(0, intelligence + will - 2) // 14) + bonus


def kit(arcana, limit, intelligence, will):
    """05.1 (owner, 2026-11-13, COMBAT-2 and COMBAT-8): a Limit gives its whole pool, and an NPC prepares by the seed
    from every active fight ability of its Limits, as many as its slots hold; passives need no slot."""
    actives = [a for lim, (act, _) in POOLS[arcana].items() if lim >= limit for a in act]
    passives = tuple(a for lim, (_, pas) in POOLS[arcana].items() if lim >= limit for a in pas)
    return passives + tuple(random.sample(actives, min(prepared_slots(intelligence, will), len(actives))))


# The slice's reference fighters, firearms first (10.2 §5d): a pistol, or the cult senior's rifle, with fists for
# melee. The allies' values are 10.2 §5b's reference table; the skill is that of every weapon each carries, and of its
# Arcana. Everyone with Will Power infuses (COMBAT-7), and each prepares by the seed unless given its abilities.
# The cult's followers (owner, 2026-11-13, COMBAT-7 and its follow-up): a point lower than the allied row in every
# combat attribute, skill and Will, the Император row of 10.2 §5f; the lion's share of the gap is in their poor gear,
# Outfit V1 in rework_sim.py, so that an allied fighter wins about 75 % of duels at the same Limit.
def follower9(abilities=None):
    return Unit("L9", 9, 5, 5, 3, 3, 4, will=5, weapon=PISTOL, second=FISTS, armour="cloth", infuse=True,
                abilities=kit("emperor", 9, 3, 5) if abilities is None else abilities)


def follower8(abilities=None):
    return Unit("L8", 8, 11, 9, 7, 5, 11, will=11, weapon=PISTOL, second=FISTS, armour="leather", infuse=True,
                abilities=kit("emperor", 8, 3, 11) if abilities is None else abilities)


def senior7(abilities=None):
    return Unit("L7", 7, 19, 15, 11, 9, 19, will=19, weapon=RIFLE, second=FISTS, armour="leather", infuse=True,
                abilities=kit("emperor", 7, 3, 19) if abilities is None else abilities)


def ally9(abilities=None):
    return Unit("A9", 9, 6, 6, 4, 4, 5, will=4, intl=6, weapon=PISTOL, second=FISTS, armour="cloth", infuse=True,
                abilities=kit("priestess", 9, 6, 4) if abilities is None else abilities)


def ally8(abilities=None):
    return Unit("A8", 8, 12, 10, 8, 6, 12, will=6, intl=12, weapon=PISTOL, second=FISTS, armour="leather", infuse=True,
                abilities=kit("priestess", 8, 12, 6) if abilities is None else abilities)


def ally7(abilities=None):
    return Unit("A7", 7, 20, 16, 12, 10, 20, will=12, intl=20, weapon=RIFLE, second=FISTS, armour="leather", infuse=True,
                abilities=kit("priestess", 7, 20, 12) if abilities is None else abilities)


def warden():
    """A Warden (10.2 §5a): no Limit, 07.1's starting attributes and a few points, a pistol and the baton."""
    return Unit("W", None, 4, 4, 4, 3, 5, will=3, weapon=PISTOL, second=BATON, armour="leather")


KIT = ("stand", "brace", "support")


def hero(weapon=ONE_HAND, armour="leather", stage=9, limit=8, abilities=KIT, guarded=False):
    return Unit("hero", limit, 14, 10, 6, 5, 10, will=9, weapon=staged(weapon, stage), armour=armour, armour_stage=stage, focus=True, abilities=abilities, guarded=guarded)


def hero9(weapon=ONE_HAND, armour="cloth"):
    return Unit("hero9", 9, 9, 6, 4, 3, 6, will=6, weapon=weapon, armour=armour, focus=True)


def new_character():
    return Unit("new", None, 6, 5, 3, 1, 1, will=1, weapon=Weapon("club", 4, 5), armour="none", focus=True)


def priestess(abilities=("sleep",), weapon=RIFLE):
    return Unit("priestess", 8, 4, 5, 6, 14, 10, will=4, intl=12, weapon=weapon, armour="cloth", focus=True, abilities=abilities)


def peer(limit):
    a = 5 + 10 * (9 - limit)
    return Unit(f"peer{limit}", limit, a, a, a, a, a, will=a, weapon=staged(ONE_HAND, limit), armour="chain", armour_stage=limit)


def main():
    print("One blow between peers, by Limit")
    for limit in (9, 8, 7, 5, 3, 2, 0):
        u = peer(limit)
        u.reset()
        blow = u.weapon.base * stage_factor(u.weapon.stage) * (1 - mitigation(u.defence()))
        print(f"  Limit {limit}: HP {u.max_hp():5.0f}  blow {blow:5.1f}  reduction {mitigation(u.defence()) * 100:3.0f} %  hit {hit_chance(u, u) * 100:3.0f} %  hits to kill {u.max_hp() / blow:4.1f}")

    print("\nDuels of peers")
    for limit in (9, 8, 7, 5, 2, 0):
        report(f"Peer L{limit} v peer L{limit}", [peer(limit)], [peer(limit)])

    print("\nOne Limit step")
    report("L8 v L9", [follower8()], [follower9()])
    report("L8 v L9, L9", [follower8()], [follower9(), follower9()])
    report("L8 v L9, L9, L9", [follower8()], [follower9(), follower9(), follower9()])
    report("L7 v L8", [senior7()], [follower8()])
    report("L7 v L8, L8", [senior7()], [follower8(), follower8()])

    print("\nWeapons, no abilities, against a plain Limit 8 follower")
    plain = lambda: follower8(())
    report("Hero, one-hand", [hero(abilities=())], [plain()])
    report("Hero, one-hand, one blow and two parries", [hero(abilities=(), guarded=True)], [plain()])
    report("Hero, one-hand and shield", [hero(SHIELDED, abilities=())], [plain()])
    report("Hero, shield, one blow and two blocks", [hero(SHIELDED, abilities=(), guarded=True)], [plain()])
    report("Hero, two-hand", [hero(TWO_HAND, abilities=())], [plain()])
    report("Hero, dagger", [hero(DAGGER, abilities=())], [plain()])
    report("Hero, dagger, two blows and two parries", [hero(DAGGER, abilities=(), guarded=True)], [plain()])
    report("Hero, chain", [hero(ONE_HAND, "chain", abilities=())], [plain()])
    report("Priestess, rifle", [priestess(())], [plain()])

    print("\nThe player, with abilities")
    report("New character v L9", [new_character()], [follower9()])
    report("Hero L9 v L9", [hero9()], [follower9()])
    report("Hero L8 v L8", [hero()], [follower8()])
    report("Hero L8 v L9, L9", [hero()], [follower9(), follower9()])
    report("Priestess L8 v L8", [priestess()], [follower8()])
    report("Hero L8 v L7", [hero()], [senior7()])
    report("Hero L8, stage 8 two-hand and chain v L7", [hero(TWO_HAND, "chain", 8)], [senior7()])
    s = senior7()
    report("The same, with a stealth attack", [hero(TWO_HAND, "chain", 8)], [s], opener=s)


    def ritual(senior, title):
        print(f"\nThe ritual group: {title}")
        for name, h, kind in [
            ("Hero, stage 9 gear, open fight", hero(), None),
            ("Hero, stage 8 gear, open fight", hero(TWO_HAND, "chain", 8), None),
            ("Hero, stage 8 gear, stealth on a conductor", hero(TWO_HAND, "chain", 8), "conductor"),
            ("Hero, stage 8 gear, stealth on the senior", hero(TWO_HAND, "chain", 8), "senior"),
            ("Hero, dagger, stage 8, stealth on the senior", hero(DAGGER, "chain", 8), "senior"),
        ]:
            a = follower9()
            b = follower9()
            s = senior()
            for u in (a, b, s):
                u.conducting = True
            target = {"conductor": a, "senior": s, None: None}[kind]
            report(name + ", all", [h], [a, b, s], opener=target)
            report(name + ", senior only", [h], [a, b, s], opener=target, objective=s)


    ritual(senior7, "senior L7, conductors L9, L9, conducting")
    ritual(follower8, "senior L8, conductors L9, L9, conducting")

    print("\nThe main battle, without and with the player")
    allies = lambda: [ally9(), ally9(), ally8(), ally8(), ally8()]
    for n, force in [
        ("5 with L7", [follower9, follower9, follower8, follower8, senior7]),
        ("6 with L7", [follower9, follower9, follower9, follower8, follower8, senior7]),
        ("7 with L7", [follower9, follower9, follower9, follower9, follower8, follower8, senior7]),
    ]:
        report(f"5 allies v {n}", allies(), [f() for f in force])
        report(f"5 allies + hero L9 v {n}", allies() + [hero9()], [f() for f in force])
        report(f"5 allies + hero L8 v {n}", allies() + [hero()], [f() for f in force])


    def general():
        u = peer(2)
        return Unit("General", 2, u.str, u.end, u.dex, u.per, u.skill, will=u.will, weapon=staged(TWO_HAND, 2), armour="chain", armour_stage=2, abilities=("stand", "brace", "crush"))


    def slice_war(title, side, main_force):
        """The whole chain: main force first, then the ritual, inside its 60 ticks."""
        wins = 0
        for _ in range(RUNS):
            a = side()
            rounds, winner = fight(a, main_force(), max_rounds=55)
            if winner is not a:
                continue
            start = max(rounds + 5, random.randint(15, 20))
            senior, one, two = follower8(), follower9(), follower9()
            for u in (senior, one, two):
                u.conducting = True
            winner = fight(a, [senior, one, two], max_rounds=60 - start, objective=senior, wounded_a=True)[1]
            wins += winner is a
        print(f"{title:<52} ritual stopped {wins / RUNS * 100:5.1f} %")


    def enemy_main():
        return [follower9() for _ in range(random.choice((2, 2, 3, 3, 4)))] + [follower8(), follower8(), senior7()]


    print("\nThe slice with no player: do the allies stop the ritual?")
    for name, group in [
        ("3 L9, 2 L8", lambda: [ally9(), ally9(), ally9(), ally8(), ally8()]),
        ("2 L9, 3 L8", lambda: [ally9(), ally9(), ally8(), ally8(), ally8()]),
        ("1 L9, 4 L8", lambda: [ally9(), ally8(), ally8(), ally8(), ally8()]),
        ("3 L9, 3 L8", lambda: [ally9(), ally9(), ally9(), ally8(), ally8(), ally8()]),
        ("2 L9, 3 L8, Limit 9 hero", lambda: [ally9(), ally9(), ally8(), ally8(), ally8(), hero9()]),
        ("2 L9, 3 L8, Limit 8 hero", lambda: [ally9(), ally9(), ally8(), ally8(), ally8(), hero()]),
    ]:
        slice_war(f"Allies {name}", group, enemy_main)

    print("\nThe Spirit General, Limit 2")
    g = general()
    g.reset()
    print(f"  HP {g.max_hp():.0f}, reduction {mitigation(g.defence()) * 100:.0f} %, blow on the hero {g.weapon.base * stage_factor(2) * (1 - mitigation(ARMOUR['chain'][0] * stage_factor(8))):.0f}")
    report("Hero L8, best gear v General", [hero(TWO_HAND, "chain", 8)], [general()])
    report("5 allies + hero v General", allies() + [hero()], [general()])
    report("5 peers L5 v General", [peer(5) for _ in range(5)], [general()])


if __name__ == "__main__":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")
    main()

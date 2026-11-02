"""Campaign balance check for 21 Cult Campaign and City Control.

Monte Carlo over a war with no player, up to the day the Capital Gate opens. The
final attack is left out, because its design is open (CC-17). Each city keeps a
ledger of followers per Limit, and a fight runs tick by tick from a kill table
built with the Combat Numbers V0 of 10.2 §5b.

Numbers here are authoring defaults. Run: python tools/balance/campaign_sim.py
"""
import io
import random
import statistics
import sys
from collections import Counter
from dataclasses import dataclass, field

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")

LIMIT_S = {9: 1.00, 8: 1.15, 7: 1.32, 6: 1.52, 5: 1.75, 4: 2.01, 3: 2.31, 2: 2.66}
GENERAL = "G"
RUNS = 300
HOURS_PER_DAY = 24
HORIZON_DAYS = 40

ENGAGE = 0.62
BATTLE_LUCK = 0.2
MOST_TICKS = 240
RITUAL_TICKS = 60
APPROACH_TICKS = 5
RITUAL_GROUP = 6
SPIRIT_STAY = (15, 20)
RITUAL_WAIT_DAYS = 2
RAID_ESCORT = 3
HOLD_LEFT_BEHIND = 5
FORCE_CAP = 30

FLEE_ESTIMATE = 0.35
CULT_RETREAT_ESTIMATE = 0.15
ATTACK_ESTIMATE = 3.0
PURSUIT_TICKS = 8
FAILED_SORTIE_REST_DAYS = 1

CITY_DEFENDERS = 10
CAPITAL_DEFENDERS = 24
SEND_SHARE = 0.06
SEER_CHANCE = 0.3
REPLACEMENT_PER_DAY = 0.35
HOME_KEEP = 8
CAPITAL_KEEP = 16
UNITE_SHARE = 0.4
FIND_RAIDING_PER_DAY = 0.15
ALLIED_MIX = {9: 0.55, 8: 0.30, 7: 0.12, 6: 0.03}
ALLIED_DEEP = {5: 2, 4: 1, 3: 1}


def stage_factor(stage):
    return 1 + 0.35 * (9 - stage)


REFERENCE = {
    9: dict(acc=4.8, eva=4, hp=25, blow=5.6, attacks=1.6, defence=3),
    8: dict(acc=10.3, eva=7, hp=32, blow=6.2, attacks=1.6, defence=8),
    7: dict(acc=17, eva=11, hp=43, blow=12.6, attacks=0.65, defence=8),
}


def peer(limit):
    a = 5 + 10 * (9 - limit)
    return dict(acc=a, eva=a, hp=(20 + 0.8 * a) * LIMIT_S[limit], blow=5 * stage_factor(limit) * (1 + a / 50), attacks=(10 + a / 20) / 5 * 0.8, defence=16 * stage_factor(limit))


def general(a):
    return dict(acc=a, eva=a, hp=(20 + 0.8 * a) * LIMIT_S[2], blow=9 * stage_factor(2) * (1 + a / 50), attacks=(10 + a / 20) / 7 * 0.55, defence=16 * stage_factor(2))


def build_kill_table(general_attribute):
    """Expected kills per tick of one i on one j, capped at one kill per blow."""
    fighters = {limit: REFERENCE.get(limit) or peer(limit) for limit in (9, 8, 7, 6, 5, 4, 3, 2)}
    fighters[GENERAL] = general(general_attribute)
    table = {}
    for i, a in fighters.items():
        for j, b in fighters.items():
            hit = min(0.95, max(0.05, 0.70 + (a["acc"] - b["eva"]) / 100))
            reduction = min(0.75, b["defence"] / (b["defence"] + 50))
            hits_to_kill = max(1.0, b["hp"] / (a["blow"] * (1 - reduction)))
            table[i, j] = ENGAGE * a["attacks"] * hit / hits_to_kill
    return table


KILL = build_kill_table(75)


def size(side):
    return sum(side.values())


def firepower(a, b):
    nb = size(b)
    if not nb:
        return 0.0
    return sum(n_i * KILL[i, j] * n_j / nb for i, n_i in a.items() for j, n_j in b.items())


def force_estimate(a, b):
    """Above 1 side a should win, by the square law over the kill table."""
    fb = firepower(b, a) * size(b)
    return firepower(a, b) * size(a) / fb if fb else 99.0


def wound(wounds, attackers, targets, luck):
    total = size(targets)
    for i, n_i in attackers.items():
        for j, n_j in targets.items():
            wounds[j] += n_i * KILL[i, j] * n_j / total * luck


def settle(side, wounds):
    for key in list(wounds):
        k = min(side[key], int(wounds[key]))
        side[key] -= k
        wounds[key] -= k


def battle(a, b, ticks, a_flee=0.0, b_flee=0.0):
    """Returns the side left standing, 'a', 'b', or None, and the ticks it took."""
    wounds_a, wounds_b = Counter(), Counter()
    luck_a, luck_b = random.lognormvariate(0, BATTLE_LUCK), random.lognormvariate(0, BATTLE_LUCK)
    for t in range(ticks):
        if not size(b):
            return "a", t
        if not size(a):
            return "b", t
        estimate = force_estimate(a, b)
        if estimate < a_flee:
            return "b", t
        if b_flee and estimate > 1 / b_flee:
            for _ in range(PURSUIT_TICKS):
                if size(b):
                    wound(wounds_b, a, b, luck_a)
                    settle(b, wounds_b)
            return "a", t
        wound(wounds_b, a, b, luck_a)
        wound(wounds_a, b, a, luck_b)
        settle(a, wounds_a)
        settle(b, wounds_b)
    return None, ticks


def take(side, n):
    keys = list(side.elements())
    random.shuffle(keys)
    taken = Counter(keys[:n])
    side.subtract(taken)
    return taken


def validate():
    print("Ledger against the tactical simulation of 10.2 §5b")
    allies = {9: 3, 8: 2}
    for name, cult, tactical in (("5 with L7", {9: 2, 8: 2, 7: 1}, 47), ("6 with L7", {9: 3, 8: 2, 7: 1}, 15), ("7 with L7", {9: 4, 8: 2, 7: 1}, 1)):
        results = [battle(Counter(allies), Counter(cult), 200) for _ in range(3000)]
        wins = sum(r[0] == "a" for r in results) / 30
        ticks = sum(r[1] for r in results) / 3000
        print(f"  5 allies v {name}: ledger {wins:3.0f} %, {ticks:4.1f} ticks, tactical {tactical} %, estimate {force_estimate(Counter(allies), Counter(cult)):.2f}")
    for name, side, tactical in (("5 of Limit 5", {5: 5}, 87), ("L8 v L9", {8: 1}, None)):
        foe = {GENERAL: 1} if tactical else {9: 1}
        wins = sum(battle(Counter(side), Counter(foe), 200)[0] == "a" for _ in range(3000)) / 30
        print(f"  {name} v {'a General' if tactical else 'one'}: ledger {wins:3.0f} %, tactical {tactical or 93} %")
    print("  Kills per tick of a General on Limit 9, 7, 5:", ", ".join(f"{KILL[GENERAL, j]:.2f}" for j in (9, 7, 5)))


@dataclass
class CultForce:
    name: str
    rate: float
    mix: dict
    threshold: float = 20
    pool: Counter = field(default_factory=Counter)
    alive: bool = True
    holds: object = None
    fraction: float = 0.0
    first_sortie: float = None
    rest_until: float = 0

    def strength(self):
        return size(self.pool) + (size(self.holds.cult) if self.holds else 0)

    def needed(self):
        return self.threshold + (HOLD_LEFT_BEHIND if self.holds else 0)


@dataclass
class City:
    capital: bool = False
    allied: Counter = field(default_factory=Counter)
    cult: Counter = field(default_factory=Counter)
    control: str = "allied"
    last_ritual: float = -99


@dataclass
class Setting:
    name: str
    pace: float
    shift: int = 0
    general_attribute: int = 75
    generals_at_ritual: bool = False
    united: bool = False


def draw(mix, shift=0):
    roll = random.random()
    total = 0.0
    for limit in sorted(mix):
        total += mix[limit]
        if roll < total:
            return max(5, min(9, limit - shift))
    return 9


@dataclass
class World:
    setting: Setting
    cities: list
    forces: list
    start_allied: int = 0
    day: float = 0
    log: Counter = field(default_factory=Counter)
    results: list = field(default_factory=list)
    largest: int = 0

    def free(self, exclude=None):
        return [c for c in self.cities if c.control == "allied" and c is not exclude]

    def held(self):
        return [c for c in self.cities if c.control == "cult"]

    def allied_total(self):
        return sum(size(c.allied) for c in self.cities)

    def united(self):
        return self.setting.united or self.allied_total() < self.start_allied * UNITE_SHARE

    def refuge(self, survivors, force):
        held = self.held()
        if held:
            min(held, key=lambda c: size(c.cult)).cult.update(survivors)
        elif force.alive:
            force.pool.update(survivors)


def make_world(setting):
    cities = [City(capital=i == 0) for i in range(7)]
    for city in cities:
        for _ in range(CAPITAL_DEFENDERS if city.capital else CITY_DEFENDERS):
            city.allied[draw(ALLIED_MIX)] += 1
    for limit, n in ALLIED_DEEP.items():
        for _ in range(n):
            (cities[0] if limit <= 3 else random.choice(cities)).allied[limit] += 1
    forces = [
        CultForce("Spirit", 6.0, {9: 0.75, 8: 0.25}),
        CultForce("Raiding", 4.0, {9: 0.50, 8: 0.35, 7: 0.15}),
        CultForce("Holding", 2.8, {9: 0.35, 8: 0.40, 7: 0.20, 6: 0.05}),
    ]
    for f in forces:
        f.rate *= setting.pace * random.uniform(0.9, 1.1)
        f.threshold = random.uniform(18, 22)
    world = World(setting, cities, forces)
    world.start_allied = world.allied_total()
    return world


def gather_defenders(world, city, force):
    defenders = city.allied
    city.allied = Counter()
    share = SEND_SHARE * (2 if force.name == "Spirit" and random.random() < SEER_CHANCE else 1)
    for other in world.free(city):
        if not other.capital:
            defenders.update(take(other.allied, round(size(other.allied) * share)))
        if world.setting.united:
            for limit in [k for k in other.allied if k <= 5]:
                defenders[limit] += other.allied.pop(limit)
    return defenders


def scatter(world, city, defenders):
    """Those who came to help go home afterwards."""
    city.allied.update(take(defenders, CITY_DEFENDERS))
    homes = world.free(city) or [city]
    for key in list(defenders.elements()):
        random.choice(homes).allied[key] += 1


def sortie(world, force, city):
    world.log["sorties"] += 1
    if force.first_sortie is None:
        force.first_sortie = world.day
    main = force.pool
    force.pool = Counter()
    if force.holds:
        main.update(take(force.holds.cult, size(force.holds.cult) - HOLD_LEFT_BEHIND))
        force.holds = None
    guards = take(main, RITUAL_GROUP)
    at_ritual = force.name == "Spirit" or world.setting.generals_at_ritual
    (guards if at_ritual else main)[GENERAL] = 1
    defenders = gather_defenders(world, city, force)
    city.last_ritual = world.day

    retreat = 0.0 if force.name == "Holding" else CULT_RETREAT_ESTIMATE
    fled = force_estimate(defenders, main) < FLEE_ESTIMATE
    if fled:
        winner, t = "b", 0
        homes = world.free(city)
        if homes:
            random.choice(homes).allied.update(defenders)
        defenders = Counter()
    else:
        winner, t = battle(defenders, main, MOST_TICKS, b_flee=retreat)
    reach = t + APPROACH_TICKS
    spirit = force.name == "Spirit"
    stay = random.randint(*SPIRIT_STAY) if spirit else RITUAL_TICKS
    fought = winner == "a" and reach < RITUAL_TICKS and force_estimate(defenders, guards) >= FLEE_ESTIMATE
    result = None
    if fought and reach < stay:
        result = battle(defenders, guards, stay - reach, b_flee=retreat)[0]
    if main[GENERAL] + guards[GENERAL] == 0:
        force.alive = False
        world.log["generals"] += 1
    main[GENERAL] = guards[GENERAL] = 0
    if fought and result is None and stay < RITUAL_TICKS:
        result = battle(defenders, guards, RITUAL_TICKS - max(reach, stay), b_flee=retreat)[0]
    ritual_done = result != "a"
    survivors = +(main + guards)

    taken = winner == "b"
    world.results.append((world.day, taken, ritual_done))
    if not taken:
        force.rest_until = world.day + FAILED_SORTIE_REST_DAYS
    if taken:
        city.control = "cult"
        city.cult = survivors
        scatter(world, city, defenders)
        city.allied = Counter()
        if force.name == "Raiding":
            force.pool.update(take(city.cult, RAID_ESCORT))
        elif force.name == "Holding" and force.alive:
            force.holds = city
        world.largest = max(world.largest, size(city.cult))
    else:
        scatter(world, city, defenders)
        world.refuge(survivors, force)


def allied_group(world):
    """Factions act alone until they are weak, and then together."""
    share = 1.0 if world.united() else 1 / 3
    group = Counter()
    for city in world.free():
        spare = size(city.allied) - (CAPITAL_KEEP if city.capital else HOME_KEEP)
        if spare > 0:
            group.update(take(city.allied, round(spare * share)))
    return group


def send_home(world, group):
    homes = world.free()
    for key in list(group.elements()):
        random.choice(homes).allied[key] += 1


def allied_offensive(world):
    if not world.free():
        return
    group = allied_group(world)
    if not size(group):
        return
    raiding = world.forces[1]
    if raiding.alive and random.random() < FIND_RAIDING_PER_DAY:
        foe = raiding.pool
        foe[GENERAL] = 1
        if force_estimate(group, foe) >= ATTACK_ESTIMATE:
            battle(group, foe, MOST_TICKS, b_flee=CULT_RETREAT_ESTIMATE)
            if not foe[GENERAL]:
                raiding.alive = False
                world.log["generals"] += 1
        foe[GENERAL] = 0
    options = []
    for city in world.held():
        holder = next((f for f in world.forces if f.holds is city), None)
        foe = city.cult + (holder.pool if holder else Counter())
        if holder:
            foe[GENERAL] = 1
        options.append((force_estimate(group, foe), id(city), city, holder, foe))
    if options:
        estimate, _, city, holder, foe = max(options, key=lambda o: o[:2])
        if estimate >= ATTACK_ESTIMATE:
            world.log["liberation_tries"] += 1
            winner, _ = battle(group, foe, MOST_TICKS, b_flee=CULT_RETREAT_ESTIMATE)
            if holder and not foe[GENERAL]:
                holder.alive = False
                holder.holds = None
                world.log["generals"] += 1
            foe[GENERAL] = 0
            foe = +foe
            if winner == "a":
                world.log["liberations"] += 1
                city.control = "allied"
                city.cult = Counter()
                city.last_ritual = world.day
                if holder:
                    holder.holds = None
                    holder.pool = Counter()
                world.refuge(foe, holder or world.forces[0])
            else:
                city.cult = foe
                if holder:
                    holder.pool = Counter()
    if size(group):
        send_home(world, group)


def gate_open(world):
    return all(c.control == "cult" for c in world.cities if not c.capital)


def run(setting):
    world = make_world(setting)
    outcome = {"gate": None, "generals_day": None, "allied": {}}
    for hour in range(HOURS_PER_DAY * HORIZON_DAYS):
        world.day = hour / HOURS_PER_DAY
        for f in world.forces:
            if not f.alive:
                continue
            f.fraction += f.rate / HOURS_PER_DAY
            while f.fraction >= 1:
                f.fraction -= 1
                if size(f.pool) < FORCE_CAP:
                    f.pool[draw(f.mix, setting.shift)] += 1
            if f.strength() >= f.needed() and world.day >= f.rest_until:
                targets = [c for c in world.free() if not c.capital and world.day - c.last_ritual >= RITUAL_WAIT_DAYS]
                if targets:
                    sortie(world, f, min(targets, key=lambda c: size(c.allied)))
        if gate_open(world):
            outcome["gate"] = world.day
            break
        if not any(f.alive for f in world.forces):
            outcome["generals_day"] = world.day
            break
        if hour % HOURS_PER_DAY == 0:
            outcome["allied"][hour // HOURS_PER_DAY] = world.allied_total()
            for city in world.free():
                n = REPLACEMENT_PER_DAY * (2 if city.capital else 1)
                city.allied[9 if random.random() < 0.7 else 8] += int(n) + (random.random() < n % 1)
            allied_offensive(world)
    outcome["world"] = world
    return outcome


def mean(values):
    values = [v for v in values if v is not None]
    return statistics.mean(values) if values else float("nan")


def report(setting):
    global KILL
    KILL = build_kill_table(setting.general_attribute)
    runs = [run(setting) for _ in range(RUNS)]
    print(f"\n{setting.name}: Cult Pace ×{setting.pace}, Limit shift {setting.shift:+d}, General {setting.general_attribute}{', all Generals at the ritual' if setting.generals_at_ritual else ''}")
    for i, name in enumerate(("Spirit", "Raiding", "Holding")):
        days = sorted(d for d in (r["world"].forces[i].first_sortie for r in runs) if d is not None)
        if days:
            print(f"  First sortie, {name}: {days[len(days) // 20]:.1f}–{days[-max(1, len(days) // 20)]:.1f}, mean {mean(days):.1f}")
    all_results = [x for r in runs for x in r["world"].results]
    first = [r["world"].results[0][1] for r in runs if r["world"].results]
    upto = [sum(1 for x in r["world"].results if x[0] <= 18) for r in runs]
    print(f"  Sorties by day 18: {mean(upto):.1f}; a city falls in {mean([x[1] for x in all_results]) * 100:.0f} % of sorties, in the first sortie {mean(first) * 100:.0f} %; ritual completes in {mean([x[2] for x in all_results]) * 100:.0f} %")
    deaths = ", ".join(f"{f}: {mean([not r['world'].forces[i].alive for r in runs]):.2f}" for i, f in enumerate(("Spirit", "Raiding", "Holding")))
    print(f"  Liberations {mean([r['world'].log['liberations'] for r in runs]):.1f}, Generals killed per world {deaths}, largest garrison {max(r['world'].largest for r in runs)}")
    gates = sorted(r["gate"] for r in runs if r["gate"] is not None)
    if gates:
        share_20 = sum(d <= 20 for d in gates) / len(gates) * 100
        print(f"  Capital Gate opens in {len(gates) / RUNS * 100:.0f} % of worlds, day {gates[len(gates) // 10]:.0f}–{gates[-max(1, len(gates) // 10)]:.0f}, mean {mean(gates):.1f}, by day 20 {share_20:.0f} %")
    won = [r["generals_day"] for r in runs if r["generals_day"] is not None]
    if won:
        print(f"  All three Generals killed in {len(won) / RUNS * 100:.0f} % of worlds, mean day {mean(won):.1f}")
    print("  Allied followers on days 0, 5, 10, 15:", ", ".join(f"{mean([r['allied'].get(d) for r in runs]):.0f}" for d in (0, 5, 10, 15)))


SETTINGS = (
    Setting("Lowest", 0.5, -1, 45, united=True),
    Setting("Pace 0.5", 0.5),
    Setting("Pace 0.75", 0.75),
    Setting("Middle", 1.0),
    Setting("Middle, every General stays at the ritual", 1.0, generals_at_ritual=True),
    Setting("Pace 1.25", 1.25),
    Setting("Pace 1.5", 1.5),
    Setting("Middle pace, harder combat", 1.0, 1),
    Setting("High", 1.25, 1),
    Setting("Highest", 1.5, 2),
)

if __name__ == "__main__":
    random.seed(2026)
    validate()
    for s in SETTINGS:
        report(s)

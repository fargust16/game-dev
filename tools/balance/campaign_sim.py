"""Campaign balance check for 21 Cult Campaign and City Control.

Monte Carlo over a whole war with no player. Both sides act on their goals. The
cult gathers followers, captures cities, and attacks the capital once the Capital
Gate opens. The allied command defends attacked cities, frees captured ones, hunts
Generals, keeps a reserve in the capital, and hires outsiders through contracts.

A war ends in manifestation or with all three Cult Generals dead. Each city keeps
a ledger of followers per Limit, and a fight runs tick by tick from a kill table
built with the Combat Numbers V0 of 10.2 §5b.

Numbers here are authoring defaults.
Run: python tools/balance/campaign_sim.py [wars per setting, default 500]
"""
import io
import random
import statistics
import sys
from collections import Counter
from dataclasses import dataclass, field

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")

LIMIT_S = {9: 1.00, 8: 1.15, 7: 1.32, 6: 1.52, 5: 1.75, 4: 2.01, 3: 2.31, 2: 2.66}
CULT_GENERAL = "G"
ALLIED_GENERAL = "A"
RIVAL = {CULT_GENERAL: ALLIED_GENERAL, ALLIED_GENERAL: CULT_GENERAL}
RUNS = int(sys.argv[1]) if len(sys.argv) > 1 else 500
HOURS_PER_DAY = 24
HORIZON_DAYS = 60

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
CITY_CAP = 30
RATE_PER_CITY = 1.0
GARRISON_TARGET = 10
GARRISON_SHARE = 0.5
FAILED_SORTIE_REST_DAYS = 1
PURSUIT_TICKS = 8
CULT_RETREAT_ESTIMATE = 0.15

FINAL_DELAY_DAYS = 1
FINAL_RITUAL_GROUP = 12
FINAL_GARRISON_SHARE = 0.6
FINAL_RETRY_DAYS = 2
FINAL_RITUAL_SHARE = 0.25

CITY_DEFENDERS = 10
CAPITAL_DEFENDERS = 24
ALLIED_MIX = {9: 0.55, 8: 0.30, 7: 0.12, 6: 0.03}
ALLIED_DEEP = {5: 2, 4: 1, 3: 1}
REPLACEMENT_PER_DAY = 0.35
ALLIED_GENERALS = 3
GENERALS_AT_HOME = 2
GENERAL_ANSWERS = 0.3
GENERAL_REST_DAYS = 0.5

CITY_KEEP = 8
CAPITAL_RESERVE_SHARE = 0.4
CAPITAL_RESERVE_MIN = 12
DEFEND_ESTIMATE = 0.9
DEFEND_FLOOR = 0.9
LIBERATE_ESTIMATE = 1.75
LIBERATE_URGENT_ESTIMATE = 1.25
HUNT_ESTIMATE = 1.2
FLEE_ESTIMATE = 0.35
ALLIED_BREAK_ESTIMATE = 0.25
SEER_CHANCE = 0.15
FIND_RAIDING_PER_DAY = 0.08
CLOSING_BOOST = 4
OUTSIDERS = 20
OUTSIDERS_PER_DAY = 0.25
CONTRACT_SIZE = 4
OUTSIDER_MIX = {9: 0.6, 8: 0.3, 7: 0.1}
BLESSING = 1.3
BLESSING_REST_DAYS = 2


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


def build_kill_table(cult_attribute, allied_attribute):
    """Expected kills per tick of one i on one j, capped at one kill per blow."""
    fighters = {limit: REFERENCE.get(limit) or peer(limit) for limit in (9, 8, 7, 6, 5, 4, 3, 2)}
    fighters[CULT_GENERAL] = general(cult_attribute)
    fighters[ALLIED_GENERAL] = general(allied_attribute)
    table = {}
    for i, a in fighters.items():
        for j, b in fighters.items():
            hit = min(0.95, max(0.05, 0.70 + (a["acc"] - b["eva"]) / 100))
            reduction = min(0.75, b["defence"] / (b["defence"] + 50))
            hits_to_kill = max(1.0, b["hp"] / (a["blow"] * (1 - reduction)))
            table[i, j] = ENGAGE * a["attacks"] * hit / hits_to_kill
    return table


KILL = build_kill_table(75, 75)


def size(side):
    return sum(side.values())


def damage(attackers, targets):
    """Expected wounds per tick on each target. A General seeks out the rival General."""
    total = size(targets)
    wounds = Counter()
    for i, n_i in attackers.items():
        if not n_i:
            continue
        rival = RIVAL.get(i)
        if rival and targets[rival] > 0:
            wounds[rival] += n_i * KILL[i, rival]
            continue
        for j, n_j in targets.items():
            if n_j:
                wounds[j] += n_i * KILL[i, j] * n_j / total
    return wounds


def firepower(a, b):
    return sum(damage(a, b).values()) if size(b) else 0.0


def force_estimate(a, b):
    """Above 1 side a should win, by the square law over the kill table."""
    if not size(a):
        return 0.0
    fb = firepower(b, a) * size(b)
    return firepower(a, b) * size(a) / fb if fb else 99.0


def settle(side, wounds):
    for key in list(wounds):
        k = min(side[key], int(wounds[key]))
        side[key] -= k
        wounds[key] -= k


def battle(a, b, ticks, a_flee=0.0, b_flee=0.0, blessing=1.0):
    """Returns the side left standing, 'a', 'b', or None, and the ticks it took."""
    wounds_a, wounds_b = Counter(), Counter()
    luck_a = random.lognormvariate(0, BATTLE_LUCK) * blessing
    luck_b = random.lognormvariate(0, BATTLE_LUCK)
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
                    for key, w in damage(a, b).items():
                        wounds_b[key] += w * luck_a
                    settle(b, wounds_b)
            return "a", t
        for key, w in damage(a, b).items():
            wounds_b[key] += w * luck_a
        for key, w in damage(b, a).items():
            wounds_a[key] += w * luck_b
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
    allies = {9: 2, 8: 3}
    for name, cult, tactical in (("5 with L7", {9: 2, 8: 2, 7: 1}, 78), ("6 with L7", {9: 3, 8: 2, 7: 1}, 46), ("7 with L7", {9: 4, 8: 2, 7: 1}, 12)):
        results = [battle(Counter(allies), Counter(cult), 200) for _ in range(3000)]
        wins = sum(r[0] == "a" for r in results) / 30
        print(f"  5 allies v {name}: ledger {wins:3.0f} %, tactical {tactical} %, estimate {force_estimate(Counter(allies), Counter(cult)):.2f}")
    wins = sum(battle(Counter({5: 5}), Counter({CULT_GENERAL: 1}), 200)[0] == "a" for _ in range(3000)) / 30
    print(f"  5 of Limit 5 v a General: ledger {wins:3.0f} %, tactical 87 %")
    wins = sum(battle(Counter({ALLIED_GENERAL: 1}), Counter({CULT_GENERAL: 1}), 200)[0] == "a" for _ in range(3000)) / 30
    print(f"  Limit 2 v Limit 2: ledger {wins:3.0f} %")


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
    shift: float = 0
    cult_general: int = 75
    allied_general: int = 75


@dataclass
class Party:
    """An allied group on one task, with what must go back when it ends."""
    fighters: Counter = field(default_factory=Counter)
    generals: int = 0
    hired: int = 0
    start: int = 0


def draw(mix, shift=0.0):
    """A fractional shift moves that share of followers one Limit further."""
    roll = random.random()
    total = 0.0
    step = int(shift // 1) + (random.random() < shift % 1)
    for limit in sorted(mix):
        total += mix[limit]
        if roll < total:
            return max(5, min(9, limit - step))
    return 9


@dataclass
class World:
    setting: Setting
    cities: list
    forces: list
    free_generals: list
    day: float = 0
    outsiders: float = OUTSIDERS
    next_blessing: float = 0
    final_ready: float = None
    log: Counter = field(default_factory=Counter)
    results: list = field(default_factory=list)
    largest: int = 0
    ended: str = None

    def capital(self):
        return self.cities[0]

    def free(self, exclude=None):
        return [c for c in self.cities if c.control == "allied" and c is not exclude]

    def held(self):
        return [c for c in self.cities if c.control == "cult"]

    def allied_total(self):
        return sum(size(c.allied) for c in self.cities)

    def reserve(self):
        return max(CAPITAL_RESERVE_MIN, self.allied_total() * CAPITAL_RESERVE_SHARE)

    def spare(self, city):
        return size(city.allied) - (self.reserve() if city.capital else CITY_KEEP)

    def blessing(self, fighters):
        """The goddess helps her direct followers now and then, never as a rule."""
        if fighters[ALLIED_GENERAL] and self.day >= self.next_blessing:
            self.next_blessing = self.day + BLESSING_REST_DAYS
            self.log["blessings"] += 1
            return BLESSING
        return 1.0

    def refuge(self, survivors, force):
        """Survivors spread over the held cities. Those with no room scatter."""
        held = self.held()
        for key in survivors.elements():
            room = [c for c in held if size(c.cult) < CITY_CAP]
            if room:
                min(room, key=lambda c: size(c.cult)).cult[key] += 1
            elif force.alive and size(force.pool) < FORCE_CAP:
                force.pool[key] += 1

    def closing(self):
        """One Cult General left: the allied command stakes everything on the kill."""
        return sum(f.alive for f in self.forces) <= 1

    def check_generals(self):
        if not any(f.alive for f in self.forces):
            self.ended = "allies"


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
    return World(setting, cities, forces, [0.0] * ALLIED_GENERALS)


def pull_general(world, party):
    ready = [t for t in world.free_generals if t <= world.day]
    closing = world.closing()
    if len(ready) <= (0 if closing else GENERALS_AT_HOME) or (not closing and random.random() >= GENERAL_ANSWERS):
        return False
    world.free_generals.remove(ready[0])
    party.fighters[ALLIED_GENERAL] += 1
    party.generals += 1
    return True


def release(world, party, garrison=None, keep=0):
    """Ends a task: Generals rest, hired outsiders leave, the rest go home."""
    fighters = party.fighters
    alive = fighters.pop(ALLIED_GENERAL, 0)
    world.log["allied_generals_lost"] += party.generals - alive
    world.free_generals.extend([world.day + GENERAL_REST_DAYS] * alive)
    if party.hired and party.start:
        back = round(party.hired * min(1, size(fighters) / party.start))
        world.outsiders += back
        take(fighters, back)
    if garrison is not None:
        garrison.allied.update(take(fighters, keep))
    home = world.capital() if world.capital().control == "allied" else (world.free() or [world.capital()])[0]
    home.allied.update(+fighters)


def reinforce(world, party, foe, target, floor, exclude=None, generals_first=False, hire=False, generals=True):
    """Draws on spare forces until the estimate reaches the target. The capital's reserve stays put."""
    pulled = Counter()
    joined = 0

    def estimate():
        return force_estimate(party.fighters + pulled, foe)

    if generals_first and pull_general(world, party):
        joined += 1
    while estimate() < target:
        sources = [(world.spare(c), id(c), c) for c in world.free(exclude) if world.spare(c) > 0]
        if not sources:
            break
        spare, _, source = max(sources, key=lambda s: s[:2])
        pulled.update(take(source.allied, min(3, int(spare) or 1)))
    if hire and estimate() < target and world.outsiders >= 1:
        hired = int(min(CONTRACT_SIZE, world.outsiders))
        world.outsiders -= hired
        party.hired += hired
        for _ in range(hired):
            pulled[draw(OUTSIDER_MIX)] += 1
    while generals and estimate() < target and pull_general(world, party):
        joined += 1
    if estimate() < floor:
        world.outsiders += party.hired
        for _ in range(party.hired):
            keys = list(pulled.elements())
            pulled[random.choice(keys)] -= 1
        party.hired = 0
        world.capital().allied.update(+pulled)
        party.fighters[ALLIED_GENERAL] -= joined
        party.generals -= joined
        world.free_generals.extend([world.day] * joined)
        return False
    if party.hired:
        world.log["contracts"] += 1
    party.fighters.update(+pulled)
    return True


def seer_ambush(world, guards, stay):
    """A seer's warning lets an Allied General wait where the Spirit General comes out."""
    party = Party()
    if not reinforce(world, party, guards, HUNT_ESTIMATE, HUNT_ESTIMATE, generals_first=True):
        return
    if not party.generals:
        release(world, party)
        return
    party.start = size(party.fighters)
    world.log["ambushes"] += 1
    battle(party.fighters, guards, stay, ALLIED_BREAK_ESTIMATE, CULT_RETREAT_ESTIMATE, world.blessing(party.fighters))
    release(world, party)


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
    spirit = force.name == "Spirit"
    (guards if spirit else main)[CULT_GENERAL] = 1
    stay = random.randint(*SPIRIT_STAY) if spirit else RITUAL_TICKS
    if spirit and random.random() < SEER_CHANCE * (CLOSING_BOOST if world.closing() else 1):
        seer_ambush(world, guards, stay)

    city.last_ritual = world.day
    party = Party(city.allied)
    city.allied = Counter()
    reinforce(world, party, main, DEFEND_ESTIMATE * random.uniform(0.8, 1.25), DEFEND_FLOOR, exclude=city, generals=main[CULT_GENERAL] > 0)
    party.start = size(party.fighters)
    retreat = 0.0 if force.name == "Holding" else CULT_RETREAT_ESTIMATE
    if force_estimate(party.fighters, main) < FLEE_ESTIMATE:
        winner, t = "b", 0
    else:
        winner, t = battle(party.fighters, main, MOST_TICKS, ALLIED_BREAK_ESTIMATE, retreat, world.blessing(party.fighters))
    reach = t + APPROACH_TICKS
    fought = winner == "a" and reach < RITUAL_TICKS and force_estimate(party.fighters, guards) >= FLEE_ESTIMATE
    result = None
    if fought and reach < stay:
        result = battle(party.fighters, guards, stay - reach, ALLIED_BREAK_ESTIMATE, retreat)[0]
    if main[CULT_GENERAL] + guards[CULT_GENERAL] == 0:
        force.alive = False
        force.holds = None
        world.log["generals"] += 1
    main[CULT_GENERAL] = guards[CULT_GENERAL] = 0
    if fought and result is None and stay < RITUAL_TICKS:
        result = battle(party.fighters, guards, RITUAL_TICKS - max(reach, stay), ALLIED_BREAK_ESTIMATE, retreat)[0]
    survivors = +(main + guards)

    taken = winner == "b" and force.alive
    world.results.append((world.day, taken, result != "a"))
    if taken:
        city.control = "cult"
        city.cult = take(survivors, CITY_CAP)
        force.pool.update(take(survivors, max(0, FORCE_CAP - size(force.pool))))
        release(world, party)
        if force.name == "Raiding":
            force.pool.update(take(city.cult, RAID_ESCORT))
        elif force.name == "Holding":
            force.holds = city
        world.largest = max(world.largest, size(city.cult))
    else:
        force.rest_until = world.day + FAILED_SORTIE_REST_DAYS
        release(world, party, city, CITY_DEFENDERS)
        world.refuge(survivors, force)
    world.check_generals()


def hunt_raiding(world):
    raiding = world.forces[1]
    if not raiding.alive or random.random() >= FIND_RAIDING_PER_DAY * (CLOSING_BOOST if world.closing() else 1):
        return
    foe = Counter(raiding.pool)
    foe[CULT_GENERAL] = 1
    party = Party()
    if not reinforce(world, party, foe, HUNT_ESTIMATE, HUNT_ESTIMATE, generals_first=True, hire=True):
        return
    party.start = size(party.fighters)
    world.log["hunts"] += 1
    battle(party.fighters, foe, MOST_TICKS, ALLIED_BREAK_ESTIMATE, CULT_RETREAT_ESTIMATE, world.blessing(party.fighters))
    if not foe[CULT_GENERAL]:
        raiding.alive = False
        world.log["generals"] += 1
    foe[CULT_GENERAL] = 0
    raiding.pool = +foe
    release(world, party)
    world.check_generals()


def liberate(world, city):
    holder = next((f for f in world.forces if f.holds is city), None)
    foe = city.cult + (holder.pool if holder else Counter())
    if holder:
        foe[CULT_GENERAL] = 1
    urgent = len([c for c in world.free() if not c.capital]) <= 1 or world.final_ready is not None
    target = LIBERATE_URGENT_ESTIMATE if urgent else LIBERATE_ESTIMATE
    party = Party()
    if not reinforce(world, party, foe, target, target, generals_first=bool(holder), hire=True):
        return
    party.start = size(party.fighters)
    world.log["liberation_tries"] += 1
    winner, _ = battle(party.fighters, foe, MOST_TICKS, ALLIED_BREAK_ESTIMATE, 0.0 if holder else CULT_RETREAT_ESTIMATE, world.blessing(party.fighters))
    if holder:
        holder.pool = Counter()
        if not foe[CULT_GENERAL]:
            holder.alive = False
            holder.holds = None
            world.log["generals"] += 1
    foe[CULT_GENERAL] = 0
    foe = +foe
    if winner == "a":
        world.log["liberations"] += 1
        city.control = "allied"
        city.cult = Counter()
        city.last_ritual = world.day
        if holder:
            holder.holds = None
        release(world, party, city, CITY_KEEP + 2)
        world.refuge(foe, holder or world.forces[0])
    else:
        city.cult = take(foe, CITY_CAP)
        if holder:
            holder.pool = take(foe, FORCE_CAP)
        release(world, party)
    world.check_generals()


def allied_orders(world):
    hunt_raiding(world)
    def first(city):
        return (not any(f.holds is city for f in world.forces), size(city.cult))
    for city in sorted(world.held(), key=first):
        if not world.ended and world.free():
            liberate(world, city)


def final_attack(world):
    world.log["final_attacks"] += 1
    capital = world.capital()
    attackers = Counter()
    alive = [f for f in world.forces if f.alive]
    for f in alive:
        attackers.update(f.pool)
        f.pool = Counter()
        f.holds = None
    for city in world.held():
        attackers.update(take(city.cult, round(size(city.cult) * FINAL_GARRISON_SHARE)))
    ritual = take(attackers, FINAL_RITUAL_GROUP)
    attackers[CULT_GENERAL] = len(alive)

    party = Party(capital.allied)
    capital.allied = Counter()
    for t in world.free_generals:
        party.fighters[ALLIED_GENERAL] += 1
        party.generals += 1
    world.free_generals = []
    hired = int(world.outsiders)
    world.outsiders -= hired
    party.hired = hired
    for _ in range(hired):
        party.fighters[draw(OUTSIDER_MIX)] += 1
    party.start = size(party.fighters)
    strike = take(party.fighters, round(size(party.fighters) * FINAL_RITUAL_SHARE))
    if party.fighters[ALLIED_GENERAL] > 1:
        party.fighters[ALLIED_GENERAL] -= 1
        strike[ALLIED_GENERAL] += 1
    bless = world.blessing(party.fighters + strike)
    stopped = battle(strike, ritual, RITUAL_TICKS, blessing=bless)[0] == "a"
    winner, t = battle(party.fighters, attackers, RITUAL_TICKS, blessing=bless)
    if not stopped and winner == "a" and t + APPROACH_TICKS < RITUAL_TICKS:
        stopped = battle(party.fighters, ritual, RITUAL_TICKS - t - APPROACH_TICKS)[0] == "a"
    party.fighters.update(+strike)
    dead = len(alive) - attackers[CULT_GENERAL]
    attackers[CULT_GENERAL] = 0
    for f in random.sample(alive, dead):
        f.alive = False
        world.log["generals"] += 1
    if not stopped:
        world.ended = "cult"
        return
    world.log["final_stopped"] += 1
    release(world, party, capital, size(party.fighters))
    world.refuge(+(attackers + ritual), alive[0])
    world.final_ready = world.day + FINAL_RETRY_DAYS
    world.check_generals()


def gate_open(world):
    return all(c.control == "cult" for c in world.cities if not c.capital)


def run(setting):
    world = make_world(setting)
    outcome = {"gate": None, "allied": {}}
    for hour in range(HOURS_PER_DAY * HORIZON_DAYS):
        world.day = hour / HOURS_PER_DAY
        for f in world.forces:
            if not f.alive:
                continue
            f.fraction += f.rate * (1 + RATE_PER_CITY * len(world.held())) / HOURS_PER_DAY
            while f.fraction >= 1:
                f.fraction -= 1
                weak = [c for c in world.held() if size(c.cult) < GARRISON_TARGET]
                if weak and random.random() < GARRISON_SHARE:
                    min(weak, key=lambda c: size(c.cult)).cult[draw(f.mix, setting.shift)] += 1
                elif size(f.pool) < FORCE_CAP:
                    f.pool[draw(f.mix, setting.shift)] += 1
            if f.strength() >= f.needed() and world.day >= f.rest_until and not world.ended:
                targets = [c for c in world.free() if not c.capital and world.day - c.last_ritual >= RITUAL_WAIT_DAYS]
                if targets:
                    sortie(world, f, min(targets, key=lambda c: size(c.allied)))
        if world.ended:
            break
        if gate_open(world):
            if outcome["gate"] is None:
                outcome["gate"] = world.day
            if world.final_ready is None:
                world.final_ready = world.day + FINAL_DELAY_DAYS
            if world.day >= world.final_ready:
                final_attack(world)
                if world.ended:
                    break
        else:
            world.final_ready = None
        if hour % HOURS_PER_DAY == 0:
            outcome["allied"][hour // HOURS_PER_DAY] = world.allied_total()
            for city in world.free():
                n = REPLACEMENT_PER_DAY * (2 if city.capital else 1)
                city.allied[9 if random.random() < 0.7 else 8] += int(n) + (random.random() < n % 1)
            world.outsiders = min(OUTSIDERS, world.outsiders + OUTSIDERS_PER_DAY)
            allied_orders(world)
            if world.ended:
                break
    outcome["world"] = world
    outcome["end"] = world.day
    return outcome


def mean(values):
    values = [v for v in values if v is not None]
    return statistics.mean(values) if values else float("nan")


def percent(runs, test):
    return sum(1 for r in runs if test(r)) / len(runs) * 100


def report(setting):
    global KILL
    KILL = build_kill_table(setting.cult_general, setting.allied_general)
    runs = [run(setting) for _ in range(RUNS)]
    allies = [r for r in runs if r["world"].ended == "allies"]
    cult = [r for r in runs if r["world"].ended == "cult"]
    print(f"\n{setting.name}: Cult Pace ×{setting.pace}, Limit shift {setting.shift:+.1f}, Generals {setting.cult_general}/{setting.allied_general}")
    print(f"  ALLIES WIN {len(allies) / RUNS * 100:.0f} %, day {mean([r['end'] for r in allies]):.1f}; CULT WINS {len(cult) / RUNS * 100:.0f} %, day {mean([r['end'] for r in cult]):.1f}; undecided at day {HORIZON_DAYS}: {(RUNS - len(allies) - len(cult)) / RUNS * 100:.0f} %")
    first = ", ".join(f"{name} {mean([r['world'].forces[i].first_sortie for r in runs]):.1f}" for i, name in enumerate(("Spirit", "Raiding", "Holding")))
    all_results = [x for r in runs for x in r["world"].results]
    upto = [sum(1 for x in r["world"].results if x[0] <= 18) for r in runs]
    print(f"  First sorties: {first}. Sorties by day 18: {mean(upto):.1f}; a city falls in {mean([x[1] for x in all_results]) * 100:.0f} %; ritual completes in {mean([x[2] for x in all_results]) * 100:.0f} %")
    log = lambda key: mean([r["world"].log[key] for r in runs])
    print(f"  Per war: liberation tries {log('liberation_tries'):.1f}, liberated {log('liberations'):.1f}, contracts {log('contracts'):.1f}, ambushes {log('ambushes'):.1f}, hunts {log('hunts'):.1f}, blessings {log('blessings'):.1f}")
    largest = sorted(r["world"].largest for r in runs)
    print(f"  Generals killed: cult {log('generals'):.2f}, allied {log('allied_generals_lost'):.2f}. Final attacks {log('final_attacks'):.2f}, stopped {log('final_stopped'):.2f}. Gate opened in {percent(runs, lambda r: r['gate'] is not None):.0f} % of wars, day {mean([r['gate'] for r in runs]):.1f}. Largest garrison: median {largest[len(largest) // 2]}, most {largest[-1]}")
    print("  Allied followers on days 0, 5, 10, 15:", ", ".join(f"{mean([r['allied'].get(d) for r in runs]):.0f}" for d in (0, 5, 10, 15)))


SETTINGS = (
    Setting("Lowest", 0.5, -1, 55),
    Setting("Low", 0.75, -0.5, 70),
    Setting("Middle", 1.0),
    Setting("High", 1.25, 0.5),
    Setting("Highest", 1.5, 1.25),
)

if __name__ == "__main__":
    random.seed(2026)
    validate()
    for s in SETTINGS:
        report(s)

"""Break-ins of the slice for 10.3 Stealth and Detection §6 and 19 §6.3.

A burglar spends one night (22:00-06:00, 480 ticks of one in-game minute) breaking into dwellings of one
class, one after another, until the night ends, the picks run out, or the Wardens arrest the burglar. A
greedy burglar goes on all night; a careful one stops after three dwellings. A rich house's servants add to
the chance of being noticed inside (furnishing rules §9).
It reports what a night brings against the run's budget of 19 §6.3: about 10 000 bronze for two days'
honest work, about 5 000 a day.

The lock is a Reaction Check like theft: a good zone of 20 % x 1.25^D, held between 5 % and 60 %, with
D = (Dexterity + Theft skill) / 2 - Lock. The player's press is modelled as a normal spread around the
zone's centre with a deviation of 10 % of the bar (an average player); a better player does better.

The witness and resident chances below stand in for the Vision Cones and Notice Meters of 10.3, which
the game evaluates in place; they are this simulation's assumptions, not rules.

Run: python tools/balance/burglary_sim.py
"""
import math
import random

RUNS = 20000
NIGHT = 480                # ticks, 22:00-06:00
WALK = 8                   # ticks between two doors
SEARCH = 5                 # ticks to search a dwelling
PICKS = 5                  # one set of picks
PICK_SET_PRICE = 200       # bronze, from Veinmarket
FINE = 1000                # bronze, to take the weapons back (10.2 §9)
PRESS_SIGMA = 0.10         # an average player's spread, as a share of the bar

# Money ranges follow the purses of 19 §6.3: a poor home like a townsman's, a middling one like a guard's,
# a rich one like a merchant's. A stash may hold an item of the same shop value instead; sold at a quarter,
# it is worth less, so money is the upper bound.
# class: lock, witness chance a tick at the door, chance a resident notices inside, stash chance, money
CLASSES = {
    "poor":     dict(lock=2, witness=0.03, inside=0.10, stash=0.50, money=(100, 400)),
    "middling": dict(lock=4, witness=0.03, inside=0.08, stash=0.60, money=(300, 1000)),
    "rich":     dict(lock=6, witness=0.05, inside=0.06, stash=0.70, money=(1000, 3000)),
}
ARREST = 0.6               # a noticed burglar is caught by a Warden before getting away

# Furnishing rules §9 (owner, 2026-11-07): every rich house keeps one servant and half of them a second. A
# servant asleep in the servant's room may wake while the burglar searches, and runs for a Warden like any
# witness; each adds this chance of notice to the residents' (an authoring default, not a rule).
SECOND_SERVANT = 0.5
SERVANT_NOTICE = 0.03

CHARACTERS = {
    "new (Dex 3, Theft 1)": (3, 1),
    "practised (Dex 5, Theft 5)": (5, 5),
}


def zone(dex, theft, lock):
    d = (dex + theft) / 2 - lock
    return min(0.60, max(0.05, 0.20 * 1.25 ** d))


def hit(width):
    return math.erf((width / 2) / (PRESS_SIGMA * math.sqrt(2)))


def night(rng, dex, theft, cls, limit=None):
    c = CLASSES[cls]
    p = hit(zone(dex, theft, c["lock"]))
    t, picks, carried, houses = 0, PICKS, 0, 0
    while t + WALK + SEARCH < NIGHT and picks > 0 and (limit is None or houses < limit):
        t += WALK
        opened = False
        while picks > 0:
            t += 1
            if rng.random() < c["witness"]:
                return arrested(rng, carried, houses, t)
            if rng.random() < p:
                opened = True
                break
            picks -= 1
        if not opened:
            break
        t += SEARCH
        servants = 0 if cls != "rich" else 1 + (rng.random() < SECOND_SERVANT)
        if rng.random() < 1 - (1 - c["inside"]) * (1 - SERVANT_NOTICE) ** servants:
            return arrested(rng, carried, houses, t)
        houses += 1
        if rng.random() < c["stash"]:
            carried += rng.randint(*c["money"])
    return carried - PICK_SET_PRICE, houses, False


def arrested(rng, carried, houses, t):
    if rng.random() < ARREST:
        return -FINE - PICK_SET_PRICE, houses, True
    return carried - PICK_SET_PRICE, houses, False


def main():
    rng = random.Random(20261106)
    print(f"{'character':28} {'policy':7} {'class':9} {'zone':>5} {'hit':>5} {'houses':>6} {'jailed':>6} {'mean':>7} {'median':>7}")
    for name, (dex, theft) in CHARACTERS.items():
        for policy, limit in (("greedy", None), ("careful", 3)):
            for cls in CLASSES:
                results = [night(rng, dex, theft, cls, limit) for _ in range(RUNS)]
                gains = sorted(r[0] for r in results)
                houses = sum(r[1] for r in results) / RUNS
                jailed = sum(r[2] for r in results) / RUNS
                z = zone(dex, theft, CLASSES[cls]["lock"])
                print(f"{name:28} {policy:7} {cls:9} {z:5.0%} {hit(z):5.0%} {houses:6.1f} {jailed:6.0%} "
                      f"{sum(gains) / RUNS:7.0f} {gains[RUNS // 2]:7.0f}")


if __name__ == "__main__":
    main()

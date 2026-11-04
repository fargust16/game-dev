"""Attribute and skill progression for 07.1 §3, Progression Numbers V0.

Checks how many actions a track needs to reach a level, and what a focused player gains in the slice's
two days: a crafter at the Master's workshop, a gatherer who strips the territory, and an ordinary player.

Run: python tools/balance/progress_sim.py
"""
import random

K = {-3: 0.0, -2: 0.1, -1: 0.4, 0: 1.0}
ABOVE = 0.8
OUTCOME = {"failure": 0.5, "normal": 1.0, "critical": 1.5}
ATTRIBUTE_SHARE = 0.01
SUCCESS = {-2: 0.50, -1: 0.65, 0: 0.80, 1: 0.90, 2: 0.95, 3: 0.98}


def stage(level):
    """Levels 1–9 are stage 9, 10–19 stage 8, and so on to 90–99 at stage 0."""
    return 9 - min(9, level // 10)


def base_xp(task_stage):
    return 10 * 2 ** (9 - task_stage)


def xp_award(level, task_stage, outcome="normal", share=1.0):
    """Δ = track stage − task stage, positive when the task is harder than the track. A task below the track
    gives its own BaseXP times K; a task above gives the matched award times 0.8 a stage, so never more."""
    delta = stage(level) - task_stage
    if delta > 0:
        xp = base_xp(stage(level)) * ABOVE ** delta
    else:
        xp = base_xp(task_stage) * K[max(-3, delta)]
    return xp * OUTCOME[outcome] * share


def required(level):
    """XP from level to level + 1."""
    return 10 * 1.1 ** (level - 1)


class Track:
    def __init__(self, level):
        self.level = level
        self.xp = 0.0

    def gain(self, amount):
        self.xp += amount
        while self.level < 99 and self.xp >= required(self.level):
            self.xp -= required(self.level)
            self.level += 1


def success_chance(prep, d):
    delta = max(-2, min(3, round(prep - d)))
    return SUCCESS[delta]


def matched_actions_per_level():
    print("Matched actions a level needs (task at the track's own stage, normal outcome)")
    for level in (1, 5, 9, 15, 25, 35, 45, 55, 65, 75, 85, 95):
        print(f"  level {level:>2}: {required(level) / xp_award(level, stage(level)):5.1f} actions")
    total = 0.0
    for level in range(1, 50):
        total += required(level) / xp_award(level, stage(level))
    print(f"  level 1 to 50 for a skill: {total:5.0f} matched actions, an attribute {total / ATTRIBUTE_SHARE:6.0f}")


def crafter():
    """Item Creation from 1 with stage 9 pieces; a stage 8 piece needs a glowing beast's ingredient, two or three a run."""
    skill, n, marks = Track(1), 0, {}
    while skill.level < 15:
        skill.gain(xp_award(skill.level, 9))
        n += 1
        marks.setdefault(skill.level, n)
    print(f"Crafter, stage 9 pieces only: skill 10 after {marks[10]}, 11 after {marks[11]}, 15 after {n} pieces")
    for stage9, stage8 in ((20, 0), (20, 3), (30, 3)):
        t = Track(1)
        for _ in range(stage9):
            t.gain(xp_award(t.level, 9))
        for _ in range(stage8):
            t.gain(xp_award(t.level, 8))
        ticks = 10 * stage9 + 20 * stage8
        print(f"  {stage9} stage 9 and {stage8} stage 8 pieces: skill {t.level}, {ticks} ticks ({ticks / 60:.1f} game hours)")
    unlimited, n = Track(1), 0
    while unlimited.level < 15:
        unlimited.gain(xp_award(unlimited.level, 8 if unlimited.level >= 11 else 9))
        n += 1
    print(f"  with any number of stage 8 pieces from skill 11: 15 after {n} pieces")


def gatherer(cycles, strength, mining, d=3, label=""):
    """Ore cycles at D 3: Mining and Strength from the same actions, failure and critical outcomes included."""
    random.seed(11)
    s, m = Track(strength), Track(mining)
    for _ in range(cycles):
        p = success_chance((s.level + m.level) / 2, d)
        roll = random.random()
        outcome = "critical" if roll < 0.05 else ("normal" if roll < p else "failure")
        m.gain(xp_award(m.level, 9, outcome))
        s.gain(xp_award(s.level, 9, outcome, ATTRIBUTE_SHARE))
    print(f"{label:<44} Mining {mining} to {m.level}, Strength {strength} to {s.level}")


def main():
    matched_actions_per_level()
    print()
    crafter()
    print()
    gatherer(100, 3, 1, label="Ordinary player: 100 cycles in two days")
    gatherer(300, 3, 1, label="Focused gatherer: 300 cycles")
    gatherer(430, 3, 1, label="Strips the territory: 430 cycles")
    gatherer(430, 6, 1, label="Fighter archetype strips the territory")


if __name__ == "__main__":
    main()

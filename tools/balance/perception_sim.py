"""Perception as accuracy's secondary attribute (07.1 §10; owner's direction, 2026-11-13; slice iteration S5b.3).

The owner's direction: Perception comes back into every attack's accuracy as its secondary attribute by 07.1 §5's
model, the target's Evasion standing for the task's difficulty, at most +10 % of the hit chance relatively, while it
stays the critical chance's own attribute, so that it grows with every attack. It goes in if the slice stays at about
20 % with no player and an allied fighter still wins about 75 % of duels at the same Limit; otherwise it is reported.

It runs rework_sim's measurements, the duels of the same Limit in Outfit V1 and the slice's chain, once with the rule
off and once on (the switch is combat_sim.PERCEPTION_SECONDARY, read from the environment so that a Pool's workers see
it), each with the same seeds.

Run: python tools/balance/perception_sim.py [fights per line]
"""
import io
import os
import subprocess
import sys

RUNS = sys.argv[1] if len(sys.argv) > 1 else "2000"

PROBE = """
import io, sys
from multiprocessing import Pool
import rework_sim as r
if __name__ == "__main__":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", line_buffering=True)
    with Pool() as pool:
        print("\\n".join(pool.map(r.gap, [(lim, n) for lim in (9, 8, 7) for n in (1, 5)])))
        print("\\n".join(pool.map(r.chained, ["allies alone", "+ player Limit 9 in a city set", "+ player Limit 8 in a city set"])))
"""


def run(on):
    env = dict(os.environ, PERCEPTION_SECONDARY="1" if on else "0", PYTHONIOENCODING="utf-8")
    here = os.path.dirname(os.path.abspath(__file__))
    out = subprocess.run([sys.executable, "-c", PROBE, RUNS], cwd=here, env=env, capture_output=True, text=True,
                         encoding="utf-8", check=True)
    return out.stdout.rstrip().splitlines()


def main():
    off, on = run(False), run(True)
    print(f"Perception as accuracy's secondary attribute, {RUNS} fights a line: off | on")
    for a, b in zip(off, on):
        print(f"{a.rstrip():<72} | {b.strip().split()[-2]} %")


if __name__ == "__main__":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", line_buffering=True)
    main()

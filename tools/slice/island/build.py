"""Builds one island concept: python tools/slice/island/build.py <a|b|c> [out folder].

Writes the plan (map.png, map.svg, city.png, elevation.png), the preliminary height map (heightmap_1025.png, 16 bits),
the statistics (stats.txt) and the data of the 3D preview, into unity/Documentation/Terrain/Concepts/<key>/.
"""
import importlib
import os
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import islandgen  # noqa: E402


def main():
    key = sys.argv[1].lower()
    concept = importlib.import_module("concept_" + key).CONCEPT
    out = sys.argv[2] if len(sys.argv) > 2 else os.path.join(islandgen.ROOT, "unity", "Documentation", "Terrain", "Concepts", key.upper())
    started = time.time()
    islandgen.build(concept, out, lambda s: print(s, flush=True))
    print("built in %.0f s -> %s" % (time.time() - started, out))


if __name__ == "__main__":
    main()

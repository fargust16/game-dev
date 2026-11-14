"""Builds one island concept: python tools/slice/island/build.py <a|b|c> [--export]

Writes the plan (map.png, map.svg, city.png, elevation.png), the height map (heightmap_1025.png, 16 bits), the
statistics (stats.txt) and the data of the 3D preview into unity/Documentation/Terrain/Concepts/<key>/.
With --export, and ISLAND_N=1025 in the environment, it also writes the scene's source data into
unity/Assets/Data/Slice/Island/ (islandexport.py): the heights and where each surface lies.
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
    out = os.path.join(islandgen.ROOT, "unity", "Documentation", "Terrain", "Concepts", key.upper())
    started = time.time()
    F = islandgen.build(concept, out, lambda s: print(s, flush=True))
    if "--export" in sys.argv:
        import islandexport
        share = islandexport.export(concept, F, os.path.join(islandgen.ROOT, "unity", "Assets", "Data", "Slice", "Island"))
        print("surfaces on land: " + ", ".join("%s %.0f%%" % (k, v * 100) for k, v in share.items()))
    print("built in %.0f s -> %s" % (time.time() - started, out))


if __name__ == "__main__":
    main()

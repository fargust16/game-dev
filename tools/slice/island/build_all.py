"""Builds the three concepts one after another: python tools/slice/island/build_all.py"""
import importlib
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import islandgen  # noqa: E402

for key in sys.argv[1:] or ["a", "b", "c"]:
    concept = importlib.import_module("concept_" + key).CONCEPT
    islandgen.build(concept, os.path.join(islandgen.ROOT, "unity", "Documentation", "Terrain", "Concepts", key.upper()),
                    lambda s: print(s, flush=True))

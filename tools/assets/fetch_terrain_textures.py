"""Fetches the slice terrain's CC0 texture sets from Poly Haven and writes their manifest (owner, 2026-11-14, VS-ISLAND).

python tools/assets/fetch_terrain_textures.py [--check]

Each set is five maps at 2K: colour, OpenGL normal, roughness, ambient occlusion (JPG) and displacement (PNG). They go to
unity/Assets/Art/CC0/Terrain/<id>/, and MANIFEST.json beside them records, for every file, its source URL, its size and
its MD5 as Poly Haven's API gives it, with the licence. A file already there with the right MD5 is left alone, so the
script is also the way to check a clone (--check downloads nothing and fails on a missing or changed file).
Every asset is CC0-1.0 (https://polyhaven.com/license).
"""
import hashlib
import json
import os
import sys
import time
import urllib.request

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
OUT = os.path.join(ROOT, "unity", "Assets", "Art", "CC0", "Terrain")
AGENT = {"User-Agent": "veilmouth-terrain-fetch/1.0"}

# The surface each set is for (ISLAND_CONCEPTS.md; the list the owner approved on 2026-11-14).
SETS = [
    ("grass_ground", "meadow grass"), ("forest_ground_04", "forest floor"), ("forest_leaves_03", "leaf litter"),
    ("brown_mud_03", "mud"), ("dirt_floor", "dry earth and paths"), ("gravel_ground_01", "gravel"),
    ("river_small_rocks", "river stones"), ("rock_face_03", "rock faces"), ("mossy_rock", "mossy rock"),
    ("rocks_ground_06", "rocky shore"),
    # The owner's second leave, 2026-11-14 (TERRAIN_POLISH_REPORT.md §8): an even beach sand in place of coast_sand_03,
    # whose patches of tone read as a grid from the air and as blotches under the stochastic read, and a second turf.
    ("sand_03", "beach sand"), ("leafy_grass", "lush turf"),
]
# map name in our files -> (the API's keys to try, format)
MAPS = [("diff", ("Diffuse", "diff", "Color"), "jpg"), ("nor_gl", ("nor_gl",), "jpg"), ("rough", ("Rough", "rough"), "jpg"),
        ("ao", ("AO", "ao"), "jpg"), ("disp", ("Displacement", "disp"), "png")]


def get(url):
    with urllib.request.urlopen(urllib.request.Request(url, headers=AGENT), timeout=120) as r:
        return r.read()


def md5(path):
    h = hashlib.md5()
    with open(path, "rb") as fh:
        for block in iter(lambda: fh.read(1 << 20), b""):
            h.update(block)
    return h.hexdigest()


def main():
    check = "--check" in sys.argv
    manifest = {"source": "Poly Haven (https://polyhaven.com)", "licence": "CC0-1.0", "licence_url": "https://polyhaven.com/license",
                "resolution": "2k", "fetched": time.strftime("%Y-%m-%d"), "sets": []}
    failed = 0
    for asset, use in SETS:
        files = json.loads(get("https://api.polyhaven.com/files/" + asset))
        folder = os.path.join(OUT, asset)
        os.makedirs(folder, exist_ok=True)
        entry = {"id": asset, "use": use, "page": "https://polyhaven.com/a/" + asset, "files": []}
        for name, keys, fmt in MAPS:
            node = next((files[k] for k in keys if k in files), None)
            if node is None or "2k" not in node or fmt not in node["2k"]:
                print("MISSING on the server: %s %s" % (asset, name), flush=True)
                failed += 1
                continue
            info = node["2k"][fmt]
            path = os.path.join(folder, "%s_%s_2k.%s" % (asset, name, fmt))
            have = os.path.exists(path) and md5(path) == info["md5"]
            if not have and check:
                print("MISSING or changed: " + path, flush=True)
                failed += 1
            elif not have:
                data = get(info["url"])
                with open(path, "wb") as fh:
                    fh.write(data)
                if md5(path) != info["md5"]:
                    print("MD5 differs after download: " + path, flush=True)
                    failed += 1
                print("fetched %s (%.1f MB)" % (os.path.basename(path), len(data) / 1e6), flush=True)
            entry["files"].append({"map": name, "file": os.path.basename(path), "url": info["url"], "size": info["size"], "md5": info["md5"]})
        manifest["sets"].append(entry)
    if not check:
        with open(os.path.join(OUT, "MANIFEST.json"), "w", encoding="utf-8") as fh:
            json.dump(manifest, fh, indent=1)
    total = sum(f["size"] for s in manifest["sets"] for f in s["files"])
    print("%d sets, %d files, %.0f MB; %d problems" % (len(manifest["sets"]), sum(len(s["files"]) for s in manifest["sets"]), total / 1e6, failed))
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()

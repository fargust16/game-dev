"""Write the read-only site that GitHub Pages serves.

The local server keeps reading data/ on each request.
This export is a copy for publishing: the app, one atlas JSON file, and the reference images.
"""

import json
import shutil
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
REPO = ROOT.parent
sys.path.insert(0, str(ROOT))
import validate


def export_site():
    atlas = validate.load_atlas(ROOT)
    errors = validate.validate(atlas, REPO)
    if errors:
        print("Atlas data failed validation. The site was not exported.")
        for error in errors:
            print(" - " + error)
        raise SystemExit(1)

    site = ROOT / "site"
    if site.exists():
        shutil.rmtree(site)
    shutil.copytree(ROOT / "app", site)
    (site / ".nojekyll").write_text("", encoding="utf-8")

    api_dir = site / "api"
    api_dir.mkdir()
    (api_dir / "atlas.json").write_text(
        json.dumps(atlas, ensure_ascii=False),
        encoding="utf-8",
    )

    copied = 0
    for ref in atlas["references"]:
        relative = ref.get("path")
        if not relative:
            continue
        source = (REPO / relative).resolve()
        try:
            source.relative_to(REPO.resolve())
        except ValueError:
            raise SystemExit("Reference image is outside the repository: " + relative)
        if not source.is_file():
            raise SystemExit("Reference image is missing: " + relative)
        dest = site / "media" / relative
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, dest)
        copied += 1

    print("Exported %s" % site)
    print("Reference images copied: %s" % copied)


if __name__ == "__main__":
    export_site()

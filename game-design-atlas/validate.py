"""Checks Atlas JSON against the schema and the repository on disk.

Run from the repository root or from game-design-atlas:

    python game-design-atlas/validate.py
"""

import json
import sys
from pathlib import Path

COLLECTIONS = (
    "systems",
    "relationships",
    "sources",
    "questions",
    "decisions",
    "references",
    "audits",
    "profiles",
)

MATURITY = {
    "not-audited",
    "idea-only",
    "partially-specified",
    "specification-documented",
    "decision-pending",
    "conflicting-sources",
}
DECISION_STATE = {"proposed", "accepted", "rejected", "superseded", "unresolved"}
IMPLEMENTATION = {
    "unknown",
    "not-implemented",
    "partially-implemented",
    "implemented",
    "verified",
}
CATEGORIES = {
    "foundation",
    "world",
    "exploration",
    "choice",
    "combat",
    "character",
    "society",
    "scope",
    "proposed",
}
REL_TYPES = {"dependency", "data-flow", "event", "influence", "shared-rule"}
EVIDENCE = {"confirmed", "inferred", "unresolved", "conflicting"}
QUESTION_STATUS = {"open", "under-discussion", "resolved", "deferred"}
DECISION_STATUS = {"proposed", "accepted", "rejected", "superseded"}
AUDIT_STATE = {
    "not-assessed",
    "missing",
    "partially-covered",
    "covered",
    "not-applicable",
}
SOURCE_KIND = {"gameplay-design", "technical-research", "scope"}


def atlas_root():
    here = Path(__file__).resolve().parent
    if (here / "data").is_dir():
        return here
    nested = here / "game-design-atlas"
    if (nested / "data").is_dir():
        return nested
    raise SystemExit("game-design-atlas/data was not found")


def load_articles(data_dir):
    folder = data_dir / "articles"
    if not folder.is_dir():
        raise SystemExit("data/articles was not found")
    if (data_dir / "systems.json").exists():
        raise SystemExit("data/systems.json is retired. Edit data/articles/<id>.json")
    records = []
    for path in folder.glob("*.json"):
        record = json.loads(path.read_text(encoding="utf-8"))
        if record.get("id") != path.stem:
            raise SystemExit(path.name + " must use its file name as the id")
        records.append(record)
    records.sort(key=lambda item: (item.get("order") if type(item.get("order")) is int else 100000, item.get("id") or ""))
    return records


def load_atlas(root):
    data_dir = root / "data"
    atlas = {}
    for name in COLLECTIONS:
        if name == "systems":
            atlas[name] = load_articles(data_dir)
            continue
        path = data_dir / (name + ".json")
        atlas[name] = json.loads(path.read_text(encoding="utf-8"))
    return atlas


def _ids(records):
    return [record.get("id") for record in records]


def _duplicate(ids):
    seen = set()
    duplicates = []
    for item in ids:
        if item in seen:
            duplicates.append(item)
        seen.add(item)
    return duplicates


def validate(atlas, repo_root):
    errors = []
    systems = atlas["systems"]
    if not isinstance(systems, list):
        return ["systems.json must be a list"]
    system_ids = set()
    for system in systems:
        sid = system.get("id")
        if not sid or sid in system_ids:
            errors.append("system id missing or duplicated: " + str(sid))
        system_ids.add(sid)
        if type(system.get("order")) is not int or system.get("order") < 1:
            errors.append(str(sid) + " needs a positive integer order")
        if system.get("category") not in CATEGORIES:
            errors.append(sid + " has an unknown category")
        if system.get("designMaturity") not in MATURITY:
            errors.append(sid + " has an unknown design maturity")
        if system.get("decisionState") not in DECISION_STATE:
            errors.append(sid + " has an unknown decision state")
        impl = system.get("implementationMaturity")
        if impl not in IMPLEMENTATION:
            errors.append(sid + " has an unknown implementation maturity")
        if impl != "unknown" and not system.get("implementationEvidence"):
            errors.append(sid + " changes implementation without implementationEvidence")
        if not isinstance(system.get("proposed"), bool):
            errors.append(sid + " proposed must be true or false")
        if not system.get("summary"):
            errors.append(sid + " needs a summary")
        if not isinstance(system.get("points"), list) or not system["points"]:
            errors.append(sid + " needs at least one sourced point")
        for point in system.get("points") or []:
            if not point.get("text") or not point.get("sourceId"):
                errors.append(sid + " has a point without text or sourceId")
        if not isinstance(system.get("x"), (int, float)) or not isinstance(system.get("y"), (int, float)):
            errors.append(sid + " needs numeric x and y")

    profiles = atlas["profiles"]
    if not isinstance(profiles, dict):
        errors.append("profiles.json must be an object")
        return errors

    source_ids = set()
    for source in atlas["sources"]:
        sid = source.get("id")
        if not sid or sid in source_ids:
            errors.append("source id missing or duplicated: " + str(sid))
        source_ids.add(sid)
        path = source.get("path")
        if path and not (repo_root / path).exists():
            errors.append("source file missing: " + path)

    for system in systems:
        for source_id in system.get("sourceIds") or []:
            if source_id not in source_ids:
                errors.append(system["id"] + " cites missing source " + source_id)
        for point in system.get("points") or []:
            if point.get("sourceId") not in source_ids:
                errors.append(system["id"] + " point cites missing source " + str(point.get("sourceId")))

    for rel in atlas["relationships"]:
        rid = rel.get("id")
        if not rel.get("explanation"):
            errors.append(str(rid) + " has no explanation")
        if rel.get("fromId") not in system_ids or rel.get("toId") not in system_ids:
            errors.append(str(rid) + " points at an unknown system")
        if rel.get("fromId") == rel.get("toId"):
            errors.append(str(rid) + " connects a system to itself")
        if rel.get("type") not in REL_TYPES:
            errors.append(str(rid) + " has an unknown relationship type")
        if rel.get("evidence") not in EVIDENCE:
            errors.append(str(rid) + " has an unknown evidence status")
        for source_id in rel.get("sourceIds") or []:
            if source_id not in source_ids:
                errors.append(str(rid) + " cites missing source " + source_id)

    if _duplicate(_ids(atlas["relationships"])):
        errors.append("duplicate relationship ids")
    if _duplicate(_ids(atlas["questions"])):
        errors.append("duplicate question ids")
    if _duplicate(_ids(atlas["decisions"])):
        errors.append("duplicate decision ids")
    if _duplicate(_ids(atlas["references"])):
        errors.append("duplicate reference ids")

    for question in atlas["questions"]:
        if question.get("status") not in QUESTION_STATUS:
            errors.append(question.get("id") + " has an unknown question status")
        if question.get("sourceKind") not in SOURCE_KIND:
            errors.append(question.get("id") + " has an unknown source kind")
        if not question.get("text"):
            errors.append(question.get("id") + " has no text")
        for sid in question.get("systemIds") or []:
            if sid not in system_ids:
                errors.append(question.get("id") + " cites missing system " + sid)
        if question.get("priority") not in (None, "high", "medium", "low"):
            errors.append(question.get("id") + " has an unknown priority")

    for decision in atlas["decisions"]:
        if decision.get("status") not in DECISION_STATUS:
            errors.append(decision.get("id") + " has an unknown decision status")
        if decision.get("origin") not in ("imported", "new"):
            errors.append(decision.get("id") + " origin must be imported or new")
        if not decision.get("statement"):
            errors.append(decision.get("id") + " has no statement")
        for sid in decision.get("systemIds") or []:
            if sid not in system_ids:
                errors.append(decision.get("id") + " cites missing system " + sid)

    for ref in atlas["references"]:
        if not ref.get("note"):
            errors.append(ref.get("id") + " needs a note of what it demonstrates")
        for sid in ref.get("systemIds") or []:
            if sid not in system_ids:
                errors.append(ref.get("id") + " cites missing system " + sid)
        path = ref.get("path")
        if path and not (repo_root / path).exists():
            errors.append("reference file missing: " + path)

    audited = set()
    for audit in atlas["audits"]:
        sid = audit.get("systemId")
        audited.add(sid)
        profile_name = audit.get("profile")
        profile = profiles.get(profile_name)
        if sid not in system_ids:
            errors.append("audit cites missing system " + str(sid))
            continue
        if not profile:
            errors.append(sid + " uses unknown audit profile " + str(profile_name))
            continue
        expected = [item["id"] for item in profile["criteria"]]
        got = audit.get("criteria") or {}
        if set(got) != set(expected):
            errors.append(sid + " audit criteria do not match profile " + profile_name)
        for cid, item in got.items():
            if item.get("state") not in AUDIT_STATE:
                errors.append(sid + " criterion " + cid + " has an unknown state")
            if item.get("state") == "not-applicable" and not item.get("evidence"):
                errors.append(sid + " marks " + cid + " not applicable without a rationale")
            if item.get("state") in ("covered", "partially-covered", "missing") and not item.get("evidence"):
                errors.append(sid + " criterion " + cid + " has no evidence note")
    missing_audits = system_ids - audited
    if missing_audits:
        errors.append("systems without an audit: " + ", ".join(sorted(missing_audits)))

    orders = [system.get("order") for system in systems if type(system.get("order")) is int]
    if len(orders) != len(set(orders)):
        errors.append("article order values must be unique")

    alchemy = next((s for s in systems if s["id"] == "alchemy"), None)
    alchemy_source = next(
        (source for source in atlas["sources"] if source.get("path") == "20 Alchemy and Potioncraft.md"),
        None,
    )
    if alchemy and alchemy_source and alchemy.get("proposed"):
        errors.append("documented alchemy must not remain marked as a proposed gap")
    if alchemy and not alchemy_source and not alchemy.get("proposed"):
        errors.append("alchemy cannot be accepted without its design document")
    if alchemy and alchemy_source and alchemy_source.get("id") not in alchemy.get("sourceIds", []):
        errors.append("alchemy article must cite its design document")
    return errors


def main():
    root = atlas_root()
    repo = root.parent
    errors = validate(load_atlas(root), repo)
    if errors:
        for error in errors:
            print(error)
        print(str(len(errors)) + " error(s)")
        return 1
    print("Atlas data is consistent with the schema and the files on disk.")
    return 0


if __name__ == "__main__":
    sys.exit(main())

# Game Design Atlas — data schema

Version: 0.1

Status: Draft

Last Updated: 2026-10-29

Depends On:
[README.md](README.md)

Referenced By:
`validate.py`, `server.py`, and the workspace UI

---

# Purpose

Describe the JSON files that hold the Atlas. These files are the authoritative Atlas data. Browser storage is not.

# Files

All live in `data/`.

| File | Shape | Meaning |
|------|--------|---------|
| `articles/<id>.json` | one object | One design system. The file name is the id |
| `relationships.json` | list | A directed link between two systems |
| `sources.json` | list | A Markdown file or other artifact on disk |
| `questions.json` | list | An open or settled question |
| `decisions.json` | list | A design decision, imported or new |
| `references.json` | list | An image, note, or external example |
| `audits.json` | list | One audit per system |
| `profiles.json` | object | Criteria for each kind of system |

One article is one file. `order` is only the sequence of the system list. The site reads these files and does not write them. `python validate.py` rejects a file that breaks this schema.

# Identity

Every record in a list has a stable `id`. Relationships use `fromId` and `toId`. The arrow is only as meaningful as `explanation`. A link is not added to make the map look connected.

`evidence` on a relationship is `confirmed`, `inferred`, `unresolved`, or `conflicting`.

`origin` on a decision is `imported` or `new`. Imported text comes from the documents. New text was added to the Atlas data and is not canon.

# Three different statuses

A system carries all three:

- `designMaturity`: `not-audited`, `idea-only`, `partially-specified`, `specification-documented`, `decision-pending`, `conflicting-sources`
- `decisionState`: `proposed`, `accepted`, `rejected`, `superseded`, `unresolved`
- `implementationMaturity`: `unknown`, `not-implemented`, `partially-implemented`, `implemented`, `verified`

`unknown` is the starting implementation value. Any other implementation value requires `implementationEvidence`. A design document is not that evidence by itself.

`documentStatus` repeats the status of the main source file. Canonical, Draft, and Deprecated are document labels, not design completeness and not implementation.

`proposed: true` means the node is a gap or a deferred card, not an approved system.

# Audit

Each system uses one profile. A criterion state is `not-assessed`, `missing`, `partially-covered`, `covered`, or `not-applicable`.

`not-applicable` needs a rationale. Covered, partial, and missing need an evidence note. A heading in a document is not enough to mark a criterion covered.

The count shown in the UI is:

```text
covered / (criteria in the profile − not-applicable)
```

Partial and not-assessed stay out of the numerator. There is no single percentage for the whole game.

# Sources

`path` is relative to the repository root and must exist. The UI links to the file on GitHub. Section titles are named in the text when a precise heading was read. Anchors are not invented.

# Questions

`sourceKind` is `gameplay-design`, `technical-research`, or `scope`.

Priority is `high`, `medium`, `low`, or empty. Empty means the source did not assign one. The Stage 3F entry is one classified cluster, not a copy of all twenty-five research questions.

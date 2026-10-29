# Game Design Atlas

Version: 0.1

Status: Draft

Last Updated: 2026-10-29

Depends On:
[00.1 Core Loop](../00.1%20Core%20Loop.md), [00.2 Meta Loop](../00.2%20Meta%20Loop.md), [02 World Laws](../02%20World%20Laws.md), [03 Decision Engine](../03%20Decision%20Engine.md), [04 Points of Interest](../04%20Points%20of%20Interest.md), [05 Progression](../05%20Progression.md), [06 Approaches](../06%20Approaches.md), [07 Character Life](../07%20Character%20Life.md), [08 Confront Constraints](../08%20Confront%20Constraints.md), [09 Information Sources](../09%20Information%20Sources.md), [11 Vertical Slice](../11%20Vertical%20Slice.md), [16 Architecture Pivot](../16%20Architecture%20Pivot.md), [17 Game Systems Architecture](../17%20Game%20Systems%20Architecture.md), [99 Open Questions](../99%20Open%20Questions.md)

Referenced By:
Local design workspace only

---

# Purpose

This is the local working environment for finishing the game's design.

It is not a design authority. Canonical and Draft Markdown documents stay the source of truth. The Atlas stores a map, annotations, questions, decisions, and an audit so the next design session can start from what is already known and what is still open.

The name The Unseen Order is the owner's working title for this workspace. It is not yet written into the canonical documents.

# Where to edit

The site only displays these files. Change them here, then refresh the browser.

| Change | File |
|--------|------|
| The words of one system | `data/articles/<id>.json` |
| How an article page looks | `app/styles/reading.css`, `app/js/views/system.js` |
| Type size and colors for the whole atlas | `app/styles/tokens.css` |
| Header, columns, and scrolling | `app/styles/layout.css` |
| The map | `app/styles/map.css`, `app/js/graph.js` |
| Reference gallery and the full-screen picture | `app/styles/gallery.css`, `app/js/views/gallery.js` |
| Questions, decisions, links, sources, audits | the matching file in `data/` |
| Reference notes and image paths | `data/references.json` |

`order` inside an article only controls where that system sits in the list. It is not a design fact. The file name is the system id.

# Run

From this directory:

```text
python server.py
```

Then open http://127.0.0.1:8765/

A push to `main` publishes a read-only copy through `.github/workflows/atlas-pages.yml`. GitHub Pages serves that copy. Edit `data/` locally, then push to update the published copy.


Check the data without starting the server:

```text
python validate.py
```

The server uses only the Python standard library. There is no install step and no Zensical.

# How the atlas changes

The site only displays `data/`. It does not save edits. Change a system, a link, a question, or an audit by editing the JSON, usually by asking the agent, then refresh the page. `python validate.py` checks the files before the server starts.

The JSON is not canon. A decision added here stays `origin: new` until someone deliberately copies it into a design document.

# How to read a system

1. Open the system from the map or the list.
2. Read the annotation, the sourced paragraphs, and the gaps.
3. Follow the links to related systems. The text under each link says what the connection means in play.
4. Look at the audit: covered, partial, missing, and not assessed are different.
5. Use the generated list only as the agenda for the next conversation with the agent. It is not an approved backlog.

# Boundaries

Implementation status starts as unknown. The workspace does not infer code progress from a design document.

Alchemy is a proposed gap because no alchemy design document was found. Crafting is only a deferred card, and deep crafting is outside the vertical slice.

Stage 3F questions are classified as technical research. They are not gameplay-design blockers.

Deprecated `13 City Generation` and `11.2 Milestone S0 Greybox` are listed so they are not reused. OrganicTown is not the current path.

# Schema

See [SCHEMA.md](SCHEMA.md).

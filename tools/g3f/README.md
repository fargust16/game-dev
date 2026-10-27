# G3f research package

Research code for G3f — regional geography architecture
([phase plan](../../unity/Assets/Plans/GEOGRAPHY_G3F_PHASE_PLAN.md),
[technical specification](../../unity/Assets/Plans/GEOGRAPHY_G3F_TECHNICAL_SPEC.md)). It is **not production code**
and nothing in the Unity project references it; outputs go to `D:\Hobby\game-dev\research-data\g3f\`, never into the
repository.

| Path | What |
|---|---|
| `fixture/sector_nw.json` | the stress fixture on contour A's north-west sector (spec §4) |
| `g3f/lattice.py` | lattice conventions, I/O, PNG writer, exact distance transform, Dijkstra, components |
| `g3f/plan.py` | Geography Plan: variant draws, rasterisation, plateau and ramp, targets, canonical hash |
| `g3f/validate.py` | validator GF-V1 … GF-V7 |
| `g3f/synth.py` | the synthesis contract and the laws GF-C0, GF-UF, GF-RF (spec §7) |
| `g3f/checks.py`, `g3f/measure.py` | invariant checks and the readings (spec §9) |
| `g3f/assembly.py` | GF-A on the sector |
| `g3f/run.py`, `g3f/views.py` | context and candidate runs; cameras, sheets, sealed key |
| `g3f/vendor/lmcspec.py` | byte copy of the approved G3e reference replicas (SHA-256 `de666680…ed79`) |
| `g3f_run.py` | the runner (`variant`, `hydro-list`, `merge-hydro`, `views-list`, `sheets`, `summary`, `manifest`) |
| `tests/` | analytic checks on synthetic lattices, continuity on run 11's preserved lattices |

Requires Python 3.13 and numpy; nothing else. The Unity steps (contour export, the production hydrology planner,
rendering) are in `unity/Assets/Scripts/Editor/TerrainGenG3fDump.cs`.

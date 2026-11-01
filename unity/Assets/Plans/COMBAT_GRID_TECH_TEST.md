# Combat Grid Technical Test

Status: Draft

Last Updated: 2026-11-01

Authority: [16 Architecture Pivot](../../../16%20Architecture%20Pivot.md), [10.1 Combat Grid and Environment Actions](../../../10.1%20Grid%20World%20and%20Environment%20Actions.md), [99 Open Questions](../../../99%20Open%20Questions.md) CC-33

## Purpose

The owner wants fights in place, on a grid that forms around the player. Before further design, this test answers one question:

> Can a grid reaching 20 cells from the player be built from generated terrain and buildings, fast enough and correctly, with no hand correction?

The test decides nothing about combat rules. It changes no production code, scene, or `TerrainData`.

## State

Phase A is written and compiles against Unity 6000.5.7f1 with the Editor's Roslyn. It has not run yet, so nothing below is measured. Phase B is not built.

## Running Phase A

Close the Unity Editor first, because a project can only be open once.

```text
"C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe" -batchmode -projectPath "D:/Hobby/game-dev/game-dev/unity" -executeMethod GameDev.EditorTools.CombatGridProbe.RunBatch -gridOut "D:/Hobby/game-dev/research-data/combat-grid/run_1" -logFile "D:/Hobby/game-dev/research-data/combat-grid/run_1/unity.log"
```

It writes `summary.md`, `sites.csv`, and `maps/` into the output folder and closes itself. In the maps, green is reachable, yellow and orange are low and high cover, blue is standable but out of reach, grey is blocked, and white is the player's cell.

## Phases

| Phase | What | How |
|---|---|---|
| **A** | Build time, correctness, stability | `CombatGridProbe`, automatic, batch mode |
| **B** | Grid overlay, camera rotation, transparent walls | A Play Mode lab for the owner to look at. Built after A, because A decides how the grid reads the world |

## Phase A setup

- **Tool:** `Assets/Scripts/Editor/CombatGridProbe.cs`, Editor-only. It is research code, not the production grid.
- **Scenes:** `Overworld_Generator` (production terrain, 2×2 tiles, one town), and an unsaved flat scene with `ShellSandboxLab` Compare All Districts as the dense case. Neither is saved.
- **Output:** `D:\Hobby\game-dev\research-data\combat-grid\`, outside the repository.
- **Grid:** 41 × 41 cells, 20 each way from the player's cell. Square is the upper bound for a round grid.
- **Cell size:** 1 m and 1.5 m, because CC-33 leaves it open.
- **Cells are world-aligned:** a cell's coordinates depend on the world only, never on where the grid started. A cell's reading is therefore a function of world position, as the terrain rules require.
- **Walker:** the controller `OverworldPlayerMenu` builds: height 1.8 m, radius 0.3 m, `stepOffset` 0.4 m, `skinWidth` 0.03 m, slope limit 45°.

## Reading a cell

1. A ray from above the world collects every surface in the column, so a street and the roof over it are separate levels.
2. A surface is standable if it is above the sea, its slope is under 45°, and a character-sized capsule fits on it.
3. Two neighbouring surfaces are joined if a capsule can sweep between them. Diagonals need both side routes free, which is the no-corner-cutting rule of 10.1.
4. A search from the player's cell marks what is reachable.
5. Short rays at 0.7 m and 1.5 m find low and high cover on each side of a reachable cell.

## Sites

All sites come from one fixed seed, 20261101.

| Class | Count | Selection |
|---|---|---|
| Flat, gentle, hill, steep, cliff | 5 each | Slope 0–5°, 5–15°, 15–30°, 30–45°, over 45°, above the sea |
| Forest | 5 | The densest tree areas |
| Town | 10 | Random points in the town with at least 100 reachable cells |
| Dense | 10 | The same, in the district strip |

## Two passes

The town's fences, doors, corners, chimneys, trim, and props have no colliders today, so a reader based on physics cannot see them.

- **As built:** the scene as it is.
- **All pieces:** a box collider on every piece that lacks one, in memory only.

The difference between them is what the grid would wrongly allow. Gates are read on the all-pieces pass, because production needs that geometry either way.

## Gates

All thresholds are authoring defaults. They are fixed before the first run and are not changed to make a run pass. The machine is the owner's, in batch-mode Editor with Mono. A player build will differ.

| ID | Gate | Pass |
|---|---|---|
| **G1** | Full build, 1 m cells | Median at most 50 ms, worst at most 100 ms |
| **G2** | One-cell step of the grid, 1 m cells | Worst at most 8 ms |
| **G3** | Repeatability | Repeated builds are identical, and a grid moved step by step equals one built fresh |
| **G4** | Truth against the real controller | At most 1 % of sampled passable edges fail for the controller, and at least 95 % of all sampled edges agree |
| **G5** | Air walls | No cell is blocked by a collider that has nothing visible |
| **G6** | Hand correction | None. No site has an override |

Report-only: time by stage, reachable share by slope, levels per cell, phantom terrain cover, trees inside reachable cells, and the cells the two passes disagree on.

## What the result decides

| Result | Next step |
|---|---|
| G1–G6 pass | Grid technology holds. Take the edge rule to the owner, then build Phase B |
| G1 or G2 fails | Try batched queries, then a smaller reach or a grid fixed at the start of a fight |
| G4 fails | Find the cause in the reading before any change to the rules |
| Large pass difference | Production pieces need colliders or a semantic reading, which is its own decision |

## Not covered

- Player builds and IL2CPP.
- The real sizes of a slice city. The dense strip stands in for it.
- Vegetation concealment and Environment Action tags.
- Fighters crossing the grid's edge, which needs a design rule first.
- The camera and transparent walls, which belong to Phase B.

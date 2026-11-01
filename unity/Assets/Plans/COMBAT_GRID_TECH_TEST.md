# Combat Grid Technical Test

Status: Draft

Last Updated: 2026-11-02

Authority: [16 Architecture Pivot](../../../16%20Architecture%20Pivot.md), [10.1 Combat Grid and Environment Actions](../../../10.1%20Grid%20World%20and%20Environment%20Actions.md), [99 Open Questions](../../../99%20Open%20Questions.md) CC-33

## Purpose

The owner wants fights in place, on a grid that forms around the player. Before further design, this test answers one question:

> Can a grid reaching 20 cells from the player be built from generated terrain and buildings, fast enough and correctly, with no hand correction?

The test decides nothing about combat rules. It changes no production code, scene, or `TerrainData`.

## State

Phase A has run: see Results. Phase B is built and checked by script, and waits for the owner's look.

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
| **B** | Grid overlay, camera rotation, transparent walls | `CombatGridLab`, a Play Mode lab for the owner to look at |

## Phase A setup

- **Tool:** `Assets/Scripts/Editor/CombatGridProbe.cs`, Editor-only, over the cell reading in `Assets/Scripts/Grid/CombatGridReader.cs`, which Phase B shares. It is research code, not the production grid.
- **Scenes:** `Overworld_Generator` (production terrain, 2×2 tiles, a town generated in memory, see Deviation), and an unsaved flat scene with `ShellSandboxLab` Compare All Districts as the dense case. Neither is saved.
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

## Results

Run 4 is the reference run. Run 5 repeats it after the reading moved to its own file, with identical readings. Records are in `D:\Hobby\game-dev\research-data\combat-grid\run_1` … `run_5`. The gates never changed.

| Gate | Run 4 | Result |
|---|---|---|
| G1 full build | Median 8.0 ms, worst 15.4 ms | PASS |
| G2 one-cell step | Median about 1.2 ms, worst 4.4 ms | PASS in runs 1, 2, 4. **FAIL in run 3 (9.8 ms) and run 5 (9.0 ms)** |
| G3 repeatability | Identical, and a moved grid equals a fresh one | PASS |
| G4 truth | False pass 0.2 % of 1410, agreement 98.9 % of 1710 | PASS |
| G5 air walls | 0 cells | PASS |
| G6 hand correction | None | PASS |

**G2 is not a clean pass.** Run 5 points to the cause: of 3714 steps exactly one had a garbage collection, that step took 9.0 ms, and the slowest step without one took 4.4 ms. Run 4 had no collection and no step above 4.4 ms. Run 3 did not count collections, so its 9.8 ms is only presumed to be the same. The reading is not what is slow: production code should allocate less per step, and this lab's reader allocates on every step.

### Deviation

Runs 1 and 2 passed every gate, but their town class is **void as evidence about buildings**. The saved town stands 2–45 m above the ground, so its walls block nothing and the grid correctly read an open slope. The grid was right and the scene was wrong.

The terrain assets are gitignored and date from 2026-10-18, a month after the scene was saved on 2026-09-18, and the height sampler prefers `TerrainCollider`. That points to a stale scene rather than a generator fault, though the old terrain no longer exists to prove it. From run 3 the probe generates the town in memory on the present terrain, with the production calls and without the dust paint, which would write to `TerrainData`. Those buildings stand on the ground. The saved scene has not been changed.

### Findings

- **1 m cells.** At 1.5 m, adding the missing pieces cut the dense strip's reachable cells from 785 to 616, with 1839 cells changing. At 1 m the same pass changed 34. Narrow gaps do not survive wide cells.
- **Missing colliders.** Across ten town sites, 758 cells at 1 m are open as built and closed once every piece has a collider, about 5 % of a window. That is fences, boundaries, lamps, and props. Adding them cost under 1 ms per build.
- **Trees are not obstacles.** 256 of the 272 trees near the sites stand in reachable cells. Trees have no colliders, so neither the grid nor the character meets them. G4 cannot see this, because the controller walks through them too.
- **False blocks** are 16 at 1 m, and every one is a thin prop such as a lamp or crate that the controller slides around. The grid is the stricter of the two, which suits a grid game.
- **False passes** are 3 at 1 m, on slopes near the 45° limit and one in the town.
- **Moving the grid is cheap.** A step costs about one seventh of a full build, so the grid can follow the player every cell.
- **Characters must stay out of the reading.** The first lab run saw one cell, because the player's own capsule blocked the cells around it. The grid reads the place; who stands where is occupancy, a separate layer of data.

## Phase B

Menu: `GameDev/Combat Grid Test/Play Lab`. It opens `Overworld_Generator`, generates a town on the present terrain, gives every piece a collider, puts the player beside the buildings, and enters Play Mode. All of it is in memory, and the scene reloads from disk on leaving Play Mode.

| Key | Action |
|---|---|
| G | Enter or leave the fight view |
| WASD | Walk; the grid follows |
| Q / E, right mouse | Turn the camera; right mouse also tilts it |
| Mouse wheel | Zoom |

Colours: pale blue is open, yellow is low cover, orange is high cover, white is the player's cell. The grid moves only when the player enters a new cell.

**Transparent walls.** A building between the camera and the player turns into its foundation and the outlines of its ground-floor walls. The first version cut a hole in the pieces in the way, which showed the empty shell inside, so the whole building now goes.

**Checked by script** (`D:\Hobby\game-dev\research-data\combat-grid\lab_2`, four camera angles and a walk): 1353 cells around the player, the grid moved 7 times over 8 m with the last move at 1.8 ms, and the building in the way was replaced by its outline.

**Not checked:** the keys and mouse, which only a person can press; how the view feels; fighters crossing the grid's edge, because the lab has no NPCs. The character's model does not appear in the script's snapshots, for a reason not yet established.

## What the result decides

| Result | Next step |
|---|---|
| G1–G6 pass | Grid technology holds. The owner set the edge rule on 2026-11-02: the grid follows the player. Next is Phase B |
| G1 or G2 fails | Try batched queries, then a smaller reach or a grid fixed at the start of a fight |
| G4 fails | Find the cause in the reading before any change to the rules |
| Large pass difference | Production pieces need colliders or a semantic reading, which is its own decision |

## Not covered

- Player builds and IL2CPP.
- The real sizes of a slice city. The dense strip stands in for it.
- Vegetation concealment and Environment Action tags.
- Fighters crossing the grid's edge, which is now a rule but has no NPCs to test it with.

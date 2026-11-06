# Slice Modular Kit Standard

Status: Draft — authoring defaults set by the agent; the owner's decision that sets the frame is marked
Created: 2026-11-04
Last Updated: 2026-11-06 (§2, §4, §6: after the owner's play test of C1, a wall piece's back on the wall's face, solid door leaves, a proud strip and stoops as nested blocks, a ceiling collider for the camera, and a fall guard. Earlier: §6: an arriving player stands on the ground under the arrival point, after C1's Wardens' station dropped the player 0.44 m. Earlier: §2: a wall seen thicker than 0.3 m keeps a body of 0.3 m on its line, and a single-sided wall between two rooms gets a seen face for the second, after the BK interiors. Earlier: §1: streets at 45° under a `KitFrame`, after the grid check of C1 §12. Earlier: §7: the agent shapes the ground once from the city plan, by the owner after the play test. Earlier: §8 points to the slice's asset map. Earlier: S0.6: the ground starts as one flat level, and the water lies at Sea Level under it. Earlier: S0.4: the portal door and a 1 m wall. Earlier: S0.2: pivots on grid nodes, posts where walls meet, the stand-ins, and the layout check)
Authority: [11 Vertical Slice](../../../11%20Vertical%20Slice.md), [16 Architecture Pivot](../../../16%20Architecture%20Pivot.md), [10.1 Grid World and Environment Actions](../../../10.1%20Grid%20World%20and%20Environment%20Actions.md), [COMBAT_GRID_TECH_TEST.md](COMBAT_GRID_TECH_TEST.md)
Used by: [VERTICAL_SLICE_IMPLEMENTATION_PLAN.md](VERTICAL_SLICE_IMPLEMENTATION_PLAN.md)

---

## Purpose

Owner's decision, 2026-11-04: the slice's city is assembled by hand from modular blocks that the owner's side prepares: houses, streets, and props. The game uses real assets from the start, not a greybox.

This standard says what a block must be so that it snaps together, so that the Combat Grid reads it correctly, and so that code can be written before every block exists. Every number here is an authoring default unless it is marked as measured. A number marked **measured** comes from [COMBAT_GRID_TECH_TEST.md](COMBAT_GRID_TECH_TEST.md) or from the player controller, and changing it means changing that code.

---

## 1. Units and orientation

- 1 Unity unit = 1 m. Y is up. A block is modelled at its real size and imported at scale 1.
- The front of a block faces +Z.
- **Pivot on a grid node**, so a block placed on whole metres lands on the grid: at the bottom-left-back corner of a block that fills cells (crate, street tile, tree), and at the bottom-left end of the centre line of a linear block (wall, fence), because that line lies on the grid line (§2). Corrected 2026-11-04 in S0.2: a corner pivot would put a wall's centre line half a thickness off the grid.
- Rotations are multiples of 90° about Y only. **Exception, streets at 45°** (owner, 2026-11-05; C1 §12): blocks under a `KitFrame` keep the grid and 90° turns in the frame's axes; the frame stands on a node of the world's 1 m grid at a multiple of 45°. Such a street is at least 3 m wide, and the layout check measures a turned block's overlap on its colliders.

## 2. The grid that everything follows

The Combat Grid has **1 m cells aligned to the world** (measured: at 1.5 m narrow gaps did not survive). Blocks therefore follow a 1 m placement grid:

- Footprints are whole metres. Preferred wall widths are 1, 2, and 4 m.
- **A wall stands on a grid line, centred on it,** at most 0.3 m thick. A capsule of radius 0.3 m (measured) then still stands in the middle of the cells on both sides, so a wall closes the edge between two cells and costs neither cell. A wall seen thicker keeps a body of 0.3 m centred on its line (BK's plaster wall, 0.377 m measured, 2026-11-06). A wall model drawn on one side only, as BK's are, gets a second block between two rooms: a face turned to the other room, NoGrid and without a collider.
- **Where linear blocks meet** (authoring default, 2026-11-04): a wall or fence stops half its thickness short of each grid node, and a **post** of the wall's thickness fills a node where walls meet at a corner or a run ends. A wall of nominal width 4 m is therefore 3.7 m long. A T-junction needs no post: the through wall's face meets the end of the other. Without this, walls meeting at a corner share a square of their thickness and break the non-overlap law (§6).
- A floor or street surface top sits at a whole 0.1 m height.
- **A piece against a wall** (2026-11-06) puts its back on the wall's face, 0.19 m from the line (`SliceKitCatalogue.WallFace`): `Anchor.Back` for a stove, a cupboard, or a desk, and `Anchor.Face` for a painting or a shelf. Its footprint counts from the wall's line, so it stays on whole metres.

## 3. Sizes that gameplay depends on

| Item | Value | Why |
|---|---|---|
| Character controller | height 1.8 m, radius 0.3 m, step 0.4 m, slope limit 45° | measured: the walker of the grid test |
| Storey, floor to floor | 3 m | Climb Up covers one level of up to 2 m only as a ledge; storeys are not climbed in a fight |
| Door opening | at least 1.2 m clear wide, 2.2 m high | one cell must pass through it |
| Ground-floor window | sill 0.9 m, at least 0.8 m clear wide | the Easy band of 10.1: *Climb Through Window* |
| Stair riser | at most 0.25 m, run at least 0.28 m | under the 0.4 m step, under 45° |
| Ramp | at most 30° | under the 45° limit with margin |
| Street, alley | at least 3 m and 2 m clear | three cells and two cells: a fight needs room |
| Low cover (fence, crate line, low wall) | 0.8–1.2 m high | the cover rays sit at 0.7 m and 1.5 m (measured): low cover stops the first and not the second, so it reads as Half Cover |
| High cover (wall, pillar, coach, tree trunk) | at least 1.6 m high | stops both rays: Full Cover |
| Gap a fighter may jump | 1, 2, or 3 cells | Easy, Hard, Very hard of 10.1 |
| Ledge a fighter may climb | at most 2 m | Climb Up of 10.1 |

## 4. Colliders

The grid test found that pieces without colliders are invisible to the grid: about 5 % of a town window was wrongly open (measured). Therefore:

- **Every block that a person cannot walk through has a collider.** That includes fences, lamps, crates, barrels, trims that stick out, and tree trunks.
- Colliders follow what is visible: simple boxes or capsules, or a simplified convex mesh. **No collider without visible geometry** (the grid test's gate G5, *air walls*).
- Small decoration a person steps over or through, such as grass, litter, and flat signs, has no collider.
- A door leaf has its own collider and its own object, so it can open. A closed door is a solid leaf filling its opening, 0.05 m either side of the line, nested in its doorway's block (2026-11-06: an open doorway in an interior's outer wall dropped the player out of the scene).
- Characters are never part of the reading: the grid reads the place, and who stands where is a separate layer (measured: the first lab run read the player's own capsule as a wall).

## 5. Layers and tags

| Layer | Contents | Read by the grid |
|---|---|---|
| `Ground` | terrain, streets, floors, stairs | yes, as surfaces |
| `Structure` | walls, roofs, fixed building parts | yes |
| `Prop` | fences, crates, lamps, carts, furniture | yes |
| `Vegetation` | tree trunks and large shrubs with colliders | yes |
| `Character` | player, NPCs, beasts | no |
| `Interactable` | doors, shop counters, beds, resource nodes, containers | yes if it has a solid collider |
| `NoGrid` | decoration without collision | no |

A block that can be vaulted, climbed, or breached in a fight carries a tag component with its 10.1 action and band. The slice needs at least one vault or mantle tag ([10.1](../../../10.1%20Grid%20World%20and%20Environment%20Actions.md), *Vertical Slice Implications*).

## 6. Buildings

- A house is a group of blocks under one root object, named by its building id. The camera's transparent walls (grid test, Phase B) hide a whole building at once and show its foundation and ground-floor outline, so ground-floor walls are named `F0_…` and higher storeys `F1_…`, `F2_…`.
- **No two blocks occupy the same space.** This is the law of the [Shell Non-Overlap Rule](SHELL_NON_OVERLAP_RULE.md), applied to hand-assembled buildings: bounds may touch, never overlap. An editor check reports overlaps; it never moves blocks itself.
- **Interiors are separate scenes** ([16](../../../16%20Architecture%20Pivot.md), locked). A building that can be entered has a door block marked as a portal, with the id of its interior scene. Interiors are built from the same kit. In code (S0.4) the door block carries `Portal`: its own id, the target scene, the id of the paired door there, and the side of the wall where an arriving player stands; the leaf is an `Interactable` child inside the opening. The interior's door leads back to the street door, so the player comes out where they went in. The player arrives 1 m out from the opening, on the ground under that point within 0.75 m of the sill, so a door at the top of steps does not drop them (2026-11-06).
- A building that cannot be entered still has a closed door, so the street reads as lived in.
- **What stands proud of a footprint** (2026-11-06) is a block nested in its owner's block, so the layout check measures it for itself. A baked house's pilasters and door frames are covered by a strip 0.16 m proud of the front, stopping 0.16 m short of each end, and each house door's stoop is three collider steps.
- **Interior ceilings** are 3.48 m high. Each room has a box collider under its ceiling on Unity's `Ignore Raycast` layer, which the camera collides with and the Combat Grid does not read, because its mask leaves that layer out.
- **Fall guard:** the player carries `FallGuard`. Once they fall 5 m below the lowest collider in the scene, it returns them to the last ground they stood on. It is a safety net under any gap in a scene, not a way to design one.

## 7. Ground and forest

- The slice's ground is a Unity terrain using the five canonical `TerrainLayer` assets. It is authored content, not a generator, a bake path, or an input to the Overworld pipeline. Code (S0.6) created it once as one flat level at 25.9 m, 1.9 m above the 24 m Sea Level below which the Combat Grid reads water. After the owner's play test of 2026-11-05 the agent shapes it once from the city plan (`VEILMOUTH_CITY_PLAN.md` §9) with an authoring tool that refuses to run twice, and from then on the owner changes it by hand with Unity's tools (owner).
- The sea and the river are one water sheet at Sea Level under the ground; they appear where the ground is dug below 24 m.
- Forest trees are placed as prefabs or terrain trees **with trunk colliders**, because trees are obstacles and Full Cover in a fight ([10.1](../../../10.1%20Grid%20World%20and%20Environment%20Actions.md)). The grid test found trees without colliders, and they must not stay that way here.
- A tree that can be felled, an ore vein, and a herb or flax patch are resource nodes ([19](../../../19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) §6.4): separate prefabs on the `Interactable` layer, not terrain trees.

## 8. Materials, naming, performance

- URP Lit or URP Simple Lit materials. A pack made for the Built-in pipeline is converted before use (see [ASSET_LIBRARY_REFERENCE.md](ASSET_LIBRARY_REFERENCE.md), Low Poly Environment).
- Names: `Kit_<Family>_<Part>_<Width>x<Height>`, for example `Kit_Wall_Brick_4x3`, `Kit_Door_Wood_1x2`, `Kit_Prop_Crate_1x1`.
- Which vendor pack serves which block, prop, and place is in [SLICE_ASSET_MAP.md](SLICE_ASSET_MAP.md).
- Blocks are prefabs under `Assets/Prefabs/SliceKit/<Family>/`. A third-party pack stays in its vendor folder and is wrapped by a project prefab that adds the colliders, layer, and tags of this standard.
- Static blocks are marked static for batching. Large blocks carry LODs when their triangle count is high; the exact budget is set once the first blocks arrive.

## 9. Stand-ins

Code may need a block before the owner's side has made it. A stand-in is a plain box at the block's exact size, collider, layer, and name, under `Assets/Prefabs/SliceKit/_StandIn/`. When the real block arrives it replaces the stand-in in place, and the grid must read the same cells.

The menu *GameDev → Slice → Create Kit Stand-ins* (S0.2) writes the first set: a wall 4×3 and 2×3, a post, a 3×3 wall with a 1.2 × 2.2 m doorway over its middle cell edge, a fence 2×1 at 0.1 m thick, a crate 1×1, a street tile 4×4, and a tree trunk of radius 0.3 m and 4 m high. S0.4 added a wall 1×3, because a side that holds the 3 m doorway has an odd length, and a portal door: the 3×3 doorway closed by a 0.05 m leaf, with `Portal` on it.

## 9a. The layout check

The menu *GameDev → Slice → Check Kit Layout* (S0.2) reads every `KitBlock` in the open scenes and reports, without moving anything: a block off the 1 m grid, off the 0.1 m height step, turned other than by 90° about Y, or scaled; a block on a layer that is not a kit block layer; a solid block (`Ground`, `Structure`, `Prop`, `Vegetation`) without a solid collider, and a `NoGrid` block with one; and two blocks whose solid colliders overlap by more than 0.01 m. Touching is allowed. A block whose Environment Action matters carries `KitEnvironmentAction` with its action and band (§5).

## 10. Acceptance of a block

A new block is accepted when:

1. it is at scale 1, with its pivot and front as §1 says;
2. its footprint and placement follow the 1 m grid (§2);
3. it has the colliders, layer, and tags of §4–§5;
4. in the grid lab it gives the reading expected of it: a wall closes one edge, a fence reads as low cover, a doorway lets one cell through.

---

## Open

- The triangle and texture budget per block, set when the first blocks arrive.
- Whether the slice needs blocks for a second floor that can be walked on inside, or only ground floors.

# Vertical Slice — Implementation Plan

Status: Draft — the owner's decisions are marked; everything else is the agent's plan and authoring defaults
Created: 2026-11-04
Last Updated: 2026-11-06 (step 4a's first half: the Dwelling scene, its generator, and 265 dwelling doors, city plan §16; breaking in waits on S2, S3, S5a, and S5c. Earlier: S0.6 rebuild: the seven interiors built from BK_AlchemistHouse, city plan §14. Earlier: the Dwelling scene and breaking in added after the seven interiors, by the owner. Earlier: S0.6 rebuild: the city built from C1 with its green cover, replacing C0's dressing. Earlier: S0.6 rebuild: BK's materials in URP, the modules measured, the ground shaped from C1. Earlier: C1 accepted by the owner; the S0.6 rebuild follows its §11. Earlier: S0.6: the owner's play test of the dressing pass, the fixes of the same day, and the city plan C1 proposed; the agent shapes the ground once from it. Earlier: S0.6: the agent's dressing pass of Veilmouth and its seven interiors at the owner's request, and what the scene still lacks. Earlier: the ritual is a magic circle with three conductors and two guards, and the main force is 4–6, by 21 §4 and 11. Earlier: no factory locations in the slice: Ritual Site Candidate 1 is a square in Lower Veil, by the owner. Earlier: the inventory's money is in bronze, by 19 §6.3; the asset map takes the owner's second list. Earlier: the slice's asset map, SLICE_ASSET_MAP.md, by the owner's list of packs. Earlier: S0.6a: the Veilmouth scene's starting point. Earlier: C0, the Veilmouth map, accepted by the owner: §2)
Authority: [11 Vertical Slice](../../../11%20Vertical%20Slice.md) (what the slice is), [17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md) (systems and folders), [16 Architecture Pivot](../../../16%20Architecture%20Pivot.md) (scene locks)
Companion: [SLICE_MODULAR_KIT_STANDARD.md](SLICE_MODULAR_KIT_STANDARD.md), [SLICE_ASSET_MAP.md](SLICE_ASSET_MAP.md) (which pack serves each mechanic, character, and place)

---

## Goal

Turn the documented slice of [11](../../../11%20Vertical%20Slice.md) into a playable Unity build: one small city, Veilmouth, with a forest around it; two game days of preparation; the Spirit General's invasion; and a ritual the player stops or loses to.

This plan orders the work, names the systems of [17](../../../17%20Game%20Systems%20Architecture.md) each milestone builds, says what the existing code gives, and states how each milestone is accepted. It does not restate the rules: every rule and number lives in its design document, and the code reads it from there.

---

## Owner's decisions, 2026-11-04

1. **One hand-built scene.** The slice is one small city and a little land around it, a forest for gathering and for fighting beasts. It is not generated and does not stand on the Overworld V0 island. The seeded Overworld of [16](../../../16%20Architecture%20Pivot.md) stays the full game's path; the slice is an exception to it.
2. **Modular blocks.** The owner's side prepares modular blocks for houses, streets, and props, and the city is assembled from them by hand. The blocks follow [SLICE_MODULAR_KIT_STANDARD.md](SLICE_MODULAR_KIT_STANDARD.md).
3. **Existing code is checked against 17.** What fits stays; the rest is rewritten (§4).
4. **Real assets from the start.** No greybox phase. Where a block does not exist yet, a stand-in of its exact size holds its place (kit standard §9).

---

## 1. Standing rules for this plan

- **Authorisation.** This plan authorises no code and no Unity run. Each milestone starts only when the owner says so, as AGENTS.md requires. Within an authorised milestone the agent works through its iterations.
- **Iterations** follow 17's rule: one micro-plan per iteration, about 500 lines of code at most, then design → implement one component → review → refactor.
- **Architecture laws** of 17 and AGENTS: no monolithic managers; a system talks through its interface; ScriptableObjects hold immutable data only; runtime state lives in plain objects that the save system can write.
- **Randomness** comes only from `IWorldSeed.Fork(name)` with `System.Random`, never `UnityEngine.Random`. The slice has one fixed seed, and its random choices are forks: the Ritual Site Candidate, the two Ritual Weak Points, which beasts glow, and every roll in a fight.
- **Numbers are data.** A balance number from a design document goes into a data asset with the document and section in its description, never into code as a constant. When a document changes a number, only the asset changes.
- **Parity with the simulations.** The fight formulas in C# must give the same results as `tools/balance/combat_sim.py`, `tactical_sim.py`, and `beast_sim.py`. Pure rules are tested in EditMode against values from the simulations (§6).
- **Terrain guardrails stay untouched.** The slice does not edit TerrainGen, the Overworld bake, `TerrainData` assets of the Overworld, or the `Overworld_Generator` scene. The slice's own ground is authored content (kit standard §7): code created it once as a flat level, the agent shapes it once from the city plan at the owner's request of 2026-11-05, and the owner changes it by hand afterwards.

---

## 2. The slice scene: the Veilmouth map (C0)

C0, accepted by the owner as the agent drafted it, 2026-11-04. After the play test of 2026-11-05 the agent proposes C1 in [VEILMOUTH_CITY_PLAN.md](VEILMOUTH_CITY_PLAN.md), which keeps everything below and moves it into a city of about 480 × 340 m. The owner accepted C1 the same day: its layout replaces the districts and sizes below, which stay as the record of C0. It is a layout: S0.6 places it on the ground, and the sizes and counts stay authoring defaults until the scene is built. North is +Z.

| Item | Value |
|---|---|
| Scene | `Assets/Scenes/Slice/Veilmouth.unity`, with interiors as separate scenes beside it |
| Playable area | about 700 × 700 m: the sea along the south edge and steep ground (over 45°) on the other three, with no invisible walls |
| Sea and river | the harbour in the mouth of the river Veil; the Veil runs from the north edge along the city's east side into the sea, and one bridge crosses it from the city |
| City | about 200 × 200 m on the shore, west of the river, in the districts below |
| Fields | west of the city: the flax and the boars' ground |
| Forest | north and west, around the fields and the city: trees and herbs; wolves deeper in, boars at its edge |
| Hill | north-east, across the river: the ore, so a trip for ore crosses the bridge |
| Road | from the Ridge north through the forest to the edge of the area |
| Beasts | 6 wolf packs deeper in the forest and 4 boars at the forest edge and in the fields, in territories about 30 m across, at least 60 m apart ([11](../../../11%20Vertical%20Slice.md)) |
| Resource nodes | about 40 trees in the forest, 12 ore veins on the hill, 30 herb patches in forest glades, 10 flax patches in the fields ([11](../../../11%20Vertical%20Slice.md)) |

**Districts.**

| Place | Where | Holds |
|---|---|---|
| Ridge | the upper town, the city's north side; the rich | the faction's house; the Wardens' station with its lock-up |
| Market | the centre and west | the trader; the counters of the Masters of Item Creation, Enchanting, and First Aid; the Item Creation yard; the seer's counter (authoring default) |
| Lower Veil | the south-east, by the river and the docks; the poor and the factories | the Soot & Bell (the tavern) with the Notice Board; Harrow & Tincture; the square of Ritual Site Candidate 1; Loom Lane; the pawn shop; the missing person's tenement room |
| Soot Stair | from the Ridge down into Lower Veil, near Loom Lane | the narrow way between the upper and the lower town, with its cellar |
| Old chapel | the city's north-west edge, toward the forest | — |

**Ritual Site Candidates** ([11](../../../11%20Vertical%20Slice.md); [21](../../../21%20Cult%20Campaign%20and%20City%20Control.md) §4): (1) a square in Lower Veil, open ground in the city at the same place (the slice has no factory locations, owner 2026-11-05; the scene's guide `Ritual Site Candidate 1 - Factory Yard` keeps its old name until the owner's S0.6b pass, and `VeilmouthScene` follows then); (2) the cellar under the Soot Stair, closed and inside the city; (3) the old chapel at the city's edge. The seed picks one by its own key.

**Buildings that open into an interior scene** (C0): seven — the Soot & Bell, Harrow's apothecary, the faction's house, the Wardens' lock-up, the missing person's room, the cellar under the Soot Stair, and the old chapel. The lock-up opens because Harrow can be heard there when the Wardens hold him ([11](../../../11%20Vertical%20Slice.md)). The trader, the three Masters, the pawn shop, and the seer serve over a counter that faces the street, and the Item Creation workshop is a yard, so they need no interior. Every other building has a closed door.

At the pace of [18](../../../18%20World%20Time%20and%20Living%20Simulation.md), one tick of 10 real seconds per game minute, a run walks across 700 m in about two real minutes, or 12 ticks. Distance therefore costs little time, and the two game days hold enough real time to explore everything.

---

## 3. Milestones

The milestones keep the names of [11](../../../11%20Vertical%20Slice.md). S5 is split in three because it holds most of the slice's systems.

### S0 — Scene, movement, doors, clock

| Part | Systems ([17](../../../17%20Game%20Systems%20Architecture.md)) | Notes |
|---|---|---|
| Kit in the project | — | Folders, layers, and tags of the kit standard; stand-ins; an editor check that reports overlapping blocks and blocks off the 1 m grid |
| The scene | — | Ground shaped by hand, the city's streets and first buildings, the forest |
| Movement and camera | L.1 Input, L.2 Movement, L.3 Camera, L.4 Animation | From the existing controller and camera (§4) |
| Doors | I.1 Portal, I.2 Scene Transition, I.3 Interior Runtime | One interior, the tavern; the return puts the player back where they entered |
| Clock | X.1 Time | `IWorldClock` with whole ticks, 10 real seconds a tick, tick events, pause; it survives a scene change |
| Seed | W.1 WorldSeed | The fixed slice seed and its named forks |
| Save contract | X.3 Save | Schema version and the list of what persists; no save slot yet |
| UI base | X.6 UI | Unity UI Toolkit (authoring default); a HUD with the time of day |
| Tests | — | Assembly definitions and the first EditMode tests (§6) |

Accepted when: the player walks the city and the forest in third person; enters the tavern and comes back to the same place; the clock gives one tick every 10 seconds and keeps counting through the door; the grid lab, run in this scene, reads a street correctly (walls close edges, a fence is low cover, a doorway passes one cell); the overlap check is clean; and the tests pass.

**S0 iterations** (owner authorised S0 on 2026-11-04). Each is one micro-plan of at most about 500 lines.

| Iteration | Contents | Done when |
|---|---|---|
| S0.1 Assemblies, seed, clock | Assembly definitions for the seed, the clock, and the slice, plus an EditMode test assembly; `IWorldClock` as a plain model with whole ticks, partial-tick progress, pause holders, and tick-boundary and time-advanced events; a driver that survives scene changes; clock and slice settings as data assets; the slice's named seed streams | It compiles, and the clock and seed tests pass |
| S0.2 Kit in the project | The kit standard's layers, tags, and folders; the stand-ins folder; an editor check for overlapping blocks and blocks off the 1 m grid | The check runs on a test scene and reports both faults |
| S0.3 Movement and camera | The controller and camera moved to `Locomotion/` and `Camera/` with the input service in its own assembly, freed from the Overworld and decision code | The player walks a test scene in third person |
| S0.4 Doors and interiors | Portal, scene transition, interior runtime; the tavern as the first interior; the return to the entry point | In and out of the tavern, and the clock keeps counting |
| S0.5 Save contract, HUD | Schema version and the list of what persists; a UI Toolkit HUD with the day and time | The HUD shows the time; the contract is in code and in this plan |
| S0.6 The scene | `Veilmouth.unity`: the playable ground, the sea, the player rig, the clock, and the HUD; then the streets, buildings, and forest by hand from the owner's kit, by C0 | The acceptance above |

S0.1–S0.5 need nothing from the owner. S0.6 needs the C0 map (§2, done) and the first kit blocks; stand-ins fill the gaps.

Progress: S0.1 done, 12 tests (commit `0bb96fd`). S0.2 done: the layers `Ground`, `Structure`, `Prop`, `Vegetation`, `Character`, `Interactable`, `NoGrid` at 8–14; `KitBlock` and `KitEnvironmentAction`; the layout check and eight stand-ins (kit standard §9–§9a); 6 tests, 18 in all. S0.2 also fixed the kit standard: pivots sit on grid nodes, and a post fills a node where walls meet. S0.3 done: `GameDev.Input` (the input service, unchanged), `GameDev.Locomotion` (`PlayerLocomotion`, the old Overworld controller; the Animator is optional and sits on a child), and `GameDev.Cameras` (`ThirdPersonCamera`, the old Overworld camera without the old clock and decision node: whoever opens a menu sets `LookEnabled`); the Overworld player menu and the grid lab use the new types. Editor menus build the slice player prefab and `Scenes/Slice/S0_Movement_Test.unity` from stand-ins (0 kit faults); 2 PlayMode tests walk the player there in a batch run, and an EditMode test keeps the scene's kit clean, 19 EditMode tests in all. S0.4 done: `GameDev.Core.Scenes` (`TravelRequest` with the scene and the arrival door; `SceneTransition`, a single-scene swap behind a gate), `GameDev.World.Portals` (`Portal`: Interact within 1.5 m of the opening; the paired door places the player 1 m in front of itself, facing away), and `GameDev.World.Interiors` (`InteriorRuntime`, which marks an interior scene with its building id). The movement test scene's door leads to `Scenes/Slice/S0_Tavern_Test.unity`, an 8 × 8 m stand-in room, and back; both are in Build Settings. Authoring default: a door costs no game time of its own; the clock runs through the load, so a door takes only the real seconds the load takes. 2 EditMode tests check that the doors pair up and where the street door puts the player, and 2 PlayMode tests go in and out and check the clock; 22 EditMode and 4 PlayMode tests in all. The real tavern comes from the owner's kit in S0.6. S0.5 done: the save contract below, in `GameDev.Core.Save` (`SaveSchema`, `SaveFile`, `ISaveContributor`, `SaveAssembler`), with the clock as its first contributor; and a UI Toolkit HUD in `GameDev.UI` (`HudView` on `UI/Hud/Hud.uxml`, one panel from Unity's default runtime theme scaled from 1920 × 1080) that shows the day and time and an "E Enter" prompt within a door's reach. Both test scenes carry it. 8 EditMode and 2 PlayMode tests; 30 EditMode and 6 PlayMode in all.

S0.6 runs in two parts. **S0.6a done** (agent): the menu *GameDev → Slice → Create Veilmouth Scenes* (`VeilmouthScene`) writes `Scenes/Slice/Veilmouth.unity` and `Scenes/Slice/Interiors/SootAndBell.unity` once and never overwrites them, because from then on they are hand-built. Veilmouth holds a flat terrain over the 700 × 700 m area (`Data/Slice/Veilmouth_Terrain.asset`, the five canonical layers, read only); one water sheet at Sea Level under it; the C0 map as `LayoutZone` guides drawn in the Scene view (the districts, fields, forest, hill, sea, river, bridge, and the three Ritual Site Candidates); a 32 × 16 m stand-in street in Lower Veil with a fence, crates, and the Soot & Bell, whose door leads to its 8 × 8 m stand-in interior and back; eight stand-in trunks at the forest's edge; and the player rig, clock, and HUD. Authoring defaults: the origin is the area's centre and north is +Z; Sea Level is 24 m, the level below which the Combat Grid reads water; the untouched ground lies at 25.9 m and a street's top at 26 m; the terrain's height budget is 100 m. The water shows wherever the ground is dug below 24 m, so the sea and the river need only digging. 4 EditMode and 1 PlayMode test; 36 EditMode and 7 PlayMode in all. **S0.6b** (owner's side, by hand): shape the ground (the sea, the river bed, the Ridge above Lower Veil, the hill, the steep border), paint it, and place the kit by the guides; then the grid lab is run on a street of the scene and the acceptance above is checked. The other six interiors follow when their milestones need them.

**S0.6 dressing pass** (agent, at the owner's request, 2026-11-05: "use every asset and model we agreed in the slice, work through the whole scene, and record what is missing"). It is one pass, not a generator, and it goes under one root, `Dressing`, which the owner moves, removes, and adds to by hand; the tool refuses to run where its root or an interior exists (*GameDev → Slice → Dress Veilmouth (one pass)*; `DressReplacing` is for the agent's own batch iterations only). Three editor tools do it:

- `SliceAssetSurvey` measured every model of the asset map and drew it from the front and from above (`Logs/SliceSurvey`, outside Assets). It found the Tripo models normalised to 1 m, the Colonial church 5 m wide, the Multistory Dungeons 2 pivots up to 180 m from their models, the bushes' pivots in their middles, and the Victorian houses' fronts on −Z.
- `SliceKitCatalogue` wraps 80 kit blocks under `Prefabs/SliceKit/<Family>/` (kit standard §8): the model scaled where the survey asked, set inside a whole-metre footprint beyond its pivot with its front on +Z, its own colliders removed, and one box, trunk, or trigger of the block's own. Its own blocks: two 4 × 4 m pavings (Yughues), the door leaf `Kit_Portal_Leaf_3x2` that marks a portal on a model's face, the magic circle `Kit_Ritual_Circle_8x0` (a flat mesh of 4 m radius with three conductors' places), and the cell front, whose bars keep their own doorway open for the cell door. Sizes are authoring defaults from the survey.
- `VeilmouthDressing` lays out C0. **Streets:** Bell Street, Loom Lane, the lanes between them, the road up to the Ridge and Ridge Street, the chapel road, the Market square, the square of Ritual Site Candidate 1, the sea front, and the bridge road, 4 × 4 m paving at 26 m. **Houses:** about 150, from Modular Victorian City and Modular European House, fronts to the street, at 25.9 m. **The seven that open,** each with its door and an interior: the Soot & Bell (where the S0.6a shell stood), Harrow & Tincture, the missing person's tenement on Loom Lane, the cellar door at the foot of the Soot Stair, the faction's house and the Wardens' station on the Ridge, and the old chapel (the Colonial church at 2.5 times its size, with its graveyard). **Market:** the trader, the Masters of Enchanting and First Aid, the seer, three food stalls, and the Master of Item Creation's stall with a fenced yard (anvil, leather frame, armour stand). **Lower Veil:** the Notice Board between the tavern and Harrow's, the pawnbroker's counter and chest, cargo on the sea front. **Outside:** the bridge over the Veil; three farms and a fenced flax field with 10 patches; 40 fellable trees and 30 herb patches in eight glades, about 190 trees and bushes of cover; 12 iron ore veins on the hill; six wolf and four boar territories of 30 m (`LayoutZone`s under `Dressing`) with the dragons scaled to a wolf's 1.6 m and a boar's 1.5 m. **People:** 33 Dummies at their posts in the asset map's colours, teal and brown as project materials. **Ritual:** the circle at each Ritual Site Candidate (the Lower Veil square, the cellar, the chapel's forecourt), hidden until a ritual starts there. **Interiors:** the Soot & Bell keeps its stand-in walls and gets the owner's bar counter and cabinet, a shelf, tables, chairs, and the bed; the six new rooms have Low Poly Dungeons Lite floors and walls (8 × 8 m; the faction's house 12 × 8, the cellar 12 × 12, the chapel 8 × 12): Harrow's cupboards, shelf, and alchemy table; the faction's rostrum and pews; the lock-up's cell with the owner's bars and door; the missing person's bed, drawer, and cache; the cellar's barrels; the chapel's pews. Layout randomness is a fixed `System.Random`, never the world seed.

Checked: the kit layout check finds no fault in any of the eight scenes (1,087 blocks in Veilmouth), every door leads to a door that leads back, and the views from above and from a walker's height are in `Logs/SliceSurvey`. Three pack problems were found and handled without touching the vendor files: the Tripo Bridge left the base map empty on nine of the owner's ten models, so the wrappers wear project copies with the model's own image (`Materials/Slice/Tripo/`); the Stylized Trees Pack draws nothing without its `WindController`, so the scene has one `Wind`; the bridge's material is a Substance and shows magenta, so it wears a Yughues stone.

**What the scene still lacks** after the pass. Systems and mechanics, each in the milestone that owns it:

| Missing | Today in the scene | Milestone |
|---|---|---|
| The ground: the sea and the river dug below 24 m, the Ridge raised above Lower Veil, the Soot Stair's slope, the hill, the border; paint | flat at 25.9 m, so the water is hidden; the Ridge's houses and the stair's foot will be lifted with the ground | S0.6b (owner) |
| A closed door's interaction on every other house | doors are painted on the models; nothing answers | S0.6b, S2 |
| Doors that open: the portal leaf is a dark stand-in over the model's own door | the leaf | S7 (and asset map §9) |
| Townsfolk who move, keep schedules, and talk | 33 Dummies standing in their bind pose with the idle controller | S2, S5c |
| Gathering, felling, mining, and their regrowth | resource nodes on the Interactable layer: herbs, flax, trees, ore | S5a |
| Shops and the pawnbroker, the seer's service | counters and stalls with their keepers | S5a |
| Tasks on the Notice Board | the board | S1 |
| Sleep | the bed in the Soot & Bell | S5c |
| Beasts' behaviour in their territories | the dragons standing, territories drawn | S3 |
| The cell door's lock and Yield's custody | the cell front and its door | S3 |
| Showing the seed's Ritual Site Candidate and the circle's places | three hidden circles | S6 |
| Light at night: the lamps and candles give none | lamp and candle models | S5c, S7 |
| Footsteps by surface | none | S7 |

Models are in the asset map §9.

**The owner's play test of the dressing pass** (2026-11-05). Fifteen remarks, each answered in the city plan's §1 ([VEILMOUTH_CITY_PLAN.md](VEILMOUTH_CITY_PLAN.md)). Fixed the same day by the agent, checked by the tests:

- **The player was invisible**, and so was every Dummy but the teal and brown ones: the Human Character Dummy wears built-in `Unlit/Texture`, which URP does not draw. `SliceCharacterLook` gives each vendor material a URP Lit copy with the same palette (`Materials/Slice/Dummy/`), in the player prefab and every slice scene.
- **People hung in the air** in the player animator's fall pose (`Grounded` starts false); they now wear an idle-only controller (`Settings/Slice/SlicePersonIdle`).
- **The dragons floated and died** through the pack's demo controller, which plays every clip in turn; they now stand in the pack's idle alone (`Settings/Slice/SliceBeastIdle_*`).
- **Wooden boards by some doors** were the portal leaf; it is hidden, and only its trigger stays (owner).
- **The camera jerked near walls:** the spring arm now shortens in 0.05 s, waits 0.15 s, and lengthens over 0.45 s, the pivot is smoothed apart from the arm, and only the Ground and Structure layers move it (authoring defaults).
- **Scene changes jerked:** `SceneTransition` travels behind `ScreenFade`, a fade to black of 0.3 s, the load in the dark, two frames for the player and camera to settle, and a fade in of 0.5 s (authoring defaults).

**Still to do, in order, now that the owner has accepted C1** (the city plan's §11): ~~measure the Victorian modules and BK_AlchemistHouse and give BK's built-in Standard materials URP copies~~ (done 2026-11-05: ×1.25 and ×1.2, city plan §2 and §8); ~~shape the ground~~ (done 2026-11-05, city plan §9); ~~build the city from plots and modules by the street sections, the street furniture rules and the courtyards~~ and ~~plant the green cover~~ (done 2026-11-05 by `VeilmouthCity`, city plan §13, which lists what this pass still lacks); ~~rebuild the seven interiors from BK_AlchemistHouse with ceilings, larger and of several rooms~~ (done 2026-11-06 by `VeilmouthInteriors`, city plan §14, ground floor only). The rows of the table above that C1 changes: the ground is now the agent's (once, then the owner's); the portal leaf is hidden; the people stand in an idle, though walking and schedules are still S2 and S5c; the beasts stand in their idle, though their behaviour and fights are still S3. New rows: the city's edge, wall and gates; lamps by the street furniture rules; passable arches into courtyards. Added by the owner on 2026-11-06, after the seven interiors: the Dwelling scene that assembles an NPC home's rooms from the house's type and id, a door on every dwelling, and breaking in with witnesses, the Wardens and the Arrest ([10.3](../../../10.3%20Stealth%20and%20Detection.md) §6.1–6.2). ~~The Dwelling scene and a door on every dwelling~~ done 2026-11-06 (city plan §16); the doors open without a lock until breaking in is built, which needs the Reaction Check and Lockpicks (S5a), witnesses (S3), the Wardens' walking (S5c), and the Arrest's dialogue (S2).

**Save contract** (S0.5; authoring defaults, under the owner's Suspend Save rule in [17](../../../17%20Game%20Systems%20Architecture.md) X.3). A save is a schema version and one section per system; each system writes and reads its own section and versions it on its own. Nothing regenerable is saved: the scene, the kit, and the seed's draws come back from the scene and the seed. A save is written outside a fight and a dialogue only, so the Combat Grid and a conversation are never in it. A save from a newer build, or a section newer than its system, is refused whole and never half-read; a section that is missing leaves its system as a new game has it.

| Section | Holds | System |
|---|---|---|
| `world.seed` | The slice seed | W.1 |
| `world.clock` | The tick and the progress toward the next one (written in S0.5) | X.1 |
| `player.place` | The scene the player is in and their pose | L.2, I.2 |
| `player.character` | Attributes, skills, Arcana and Limit, Conduct Progress, HP, Will Power, Sanity, Fatigue, conditions | P |
| `player.inventory` | The bag, equipped items and Weapon Sets, money in bronze | 19 |
| `player.knowledge` | Arguments gained in this life, lessons, recipes | 10.4, 19, 20 |
| `world.state` | POI states, decision outcomes, residue, interiors' changes by building id, spent resource nodes, shop shelves | C.6, Q.3, W.7 |
| `world.people` | Named NPCs and beasts: alive or dead, where, their task, their opinion of the player | A.2 |
| `world.factions` | Membership, the trial, forces, City Control | E.1 |
| `world.invasion` | The General, the ritual, the Weak Points found, the cult's forces | E.1b |

The slot itself, writing to disk, the gate that refuses a save in a fight or a dialogue, and deleting the save on load come with the Suspend Save in S5c.

### S1 — Information and POIs

| Part | Systems | Notes |
|---|---|---|
| Information Sources | Q.2 | The Notice Board and the bartender; tips move a POI to Hinted or Marked ([09](../../../09%20Information%20Sources.md), [04](../../../04%20Points%20of%20Interest.md)) |
| POI Director | Q.3 | POI A, B, C from data; each Active Window runs from the POI's appearance; unattended resolution leaves Aftermath |
| Character Notes | Q.2 | Found facts, one line each, with no hint of their use ([09](../../../09%20Information%20Sources.md)) |
| World state | C.6 Residue | One store of flags that residue and aftermath write and the world reads |
| Named NPCs | A.1 | The roster placed in the city, still without schedules |

Accepted when: a tip marks a POI; a POI left alone resolves at the end of its window and leaves its Aftermath; a found fact appears in the Notes.

### S2 — Decisions and dialogue

| Part | Systems | Notes |
|---|---|---|
| Action checks | P.3 | The shared curve of [07.1](../../../07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md) §4 as one service, used by every later check |
| Dialogue | A.3 | Social Checks of [10.4](../../../10.4%20Dialogue%20and%20Social%20Checks.md): `2d6 + value + modifiers` against `request + listener`, Arguments from this life only, Final Checks; any dialogue pauses the world and costs one tick when it ends |
| Decision Nodes | Q.3, C.6 | POI A and B as data cards of [03.1](../../../03.1%20Decision%20Node%20Template.md); Convince, Threaten, Exchange, Veil, Withdraw; Confront opens S3's fight |
| Residue | C.6 | Shop, patrol, and rumour change after a decision |

Accepted when: POI A and B can each be settled by at least two Approaches without a fight; a failed Social Check cannot be repeated on the spot; residue shows the next day.

### S3 — Combat Grid, stealth, beasts

| Part | Systems | Notes |
|---|---|---|
| Entering a fight | C.1 Encounter Lock | As [10.2](../../../10.2%20Combat%20System%20Draft.md) sets it |
| Combat Grid | C.2 | The production grid from `CombatGridReader` (§4): 1 m cells, 20 cells from the player, following every step, characters as a separate occupancy layer, the 10.1 tags for vault and mantle |
| Turns | C.5 | Initiative, AP, attacks, damage, critical hits, cover against ranged attacks, Wait, Defensive Stance, Reaction Attack, Surrounded, two Weapon Sets, Break Away, Yield ([10.2](../../../10.2%20Combat%20System%20Draft.md) §5a–§5b, §9); one shared world tick after each round |
| Conditions | P.4 | Bleeding, Poison, fracture ([10.2](../../../10.2%20Combat%20System%20Draft.md), [18](../../../18%20World%20Time%20and%20Living%20Simulation.md)) |
| Fight AI | A.2 | Target Rules V0 of [10.2](../../../10.2%20Combat%20System%20Draft.md) |
| Stealth | 17 Wave C0.3a | Stealth Mode, Vision Cones, Notice Meter, Alert ([10.3](../../../10.3%20Stealth%20and%20Detection.md)) |
| Beasts | W.9 Spawn Tables | Territories, two warnings, packs that break, glowing beasts alone (10.2, *Beasts of the slice*) |
| Fight gear | P.1, P.2 | Just enough equipment to fight: weapons, armour, their values from 10.2 §5b |
| Residue | C.6 | What a fight leaves in the world ([08](../../../08%20Confront%20Constraints.md)) |

Accepted when: a wolf pack warns, attacks, and breaks when one wolf falls; a fight in a street uses cover; Break Away and a Yield to the Wardens both end a fight as written; an NPC notices or misses a character in Stealth Mode; and the C# fight rules, run headless over the lines of `beast_sim.py`, land within 3 points of its win rates.

### S4 — Trace and Bargain tease

| Part | Systems | Notes |
|---|---|---|
| POI C | Q.3 | Opened from an Aftermath of A or B; the cellar under the Soot Stair |
| Bargain tease | Q.3 | The black drop, a priced offer the player may refuse ([05](../../../05%20Progression.md)) |

Accepted when: POI C appears only through A's or B's Aftermath, and the Bargain shows its price before the choice.

### S5a — Items, gathering, crafting, shops, theft

| Part | Systems | Notes |
|---|---|---|
| Inventory and equipment | P.1, P.2 | Weight-only bag, carrying limits, equipment slots, Weapon Sets ([19](../../../19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) §4) |
| Gathering and Skinning | W.9, P.3 | Nodes, cycles until cancelled or empty, Stamina, the Skinning lesson ([19](../../../19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) §6.4) |
| Item Creation | E.3 | The lesson, the workshop, stage 9 and 8 recipes, modifier count and catalogue ([19](../../../19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) §2–§3) |
| Shops | E.2 | Prices, stock refilled each morning, Shop Skill, selling at a quarter ([19](../../../19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) §6) |
| First Aid | P.4 | Bandage, splint, antidote ([11](../../../11%20Vertical%20Slice.md)) |
| Theft | 10.3 | One Reaction Check ([10.3](../../../10.3%20Stealth%20and%20Detection.md) §6) |

### S5b — Arcana, Alchemy, progression, Sanity

| Part | Systems | Notes |
|---|---|---|
| Progression | P.3 | XP by Progression Numbers V0 ([07.1](../../../07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md) §3), character creation with 9 points or an archetype ([07](../../../07%20Character%20Life.md)) |
| Arcana | P.3 | Жрица and Император, Limits 9 and 8, their abilities, Conduct Progress ([05.1](../../../05.1%20Arcana%20and%20Limits.md)) |
| Alchemy | P.3 | The lesson, the brewing Reaction Checks, the Brewing Factor, interruption ([20](../../../20%20Alchemy%20and%20Potioncraft.md) §3) |
| Sanity and Will Power | P.4 | Costs of the Limits, symptoms and penalties, exhaustion ([07.2](../../../07.2%20Derived%20Character%20Statistics.md), [05.1](../../../05.1%20Arcana%20and%20Limits.md)) |

### S5c — Days, townsfolk, faction, save

| Part | Systems | Notes |
|---|---|---|
| Sleep and Fatigue | X.1, P.4 | Tavern room or street, recovery, Fatigue ([18](../../../18%20World%20Time%20and%20Living%20Simulation.md), [07.2](../../../07.2%20Derived%20Character%20Statistics.md)) |
| Townsfolk | A.2 | Schedules of named NPCs, shop hours, crowds ([18](../../../18%20World%20Time%20and%20Living%20Simulation.md)) |
| Faction and trial | E.1 | The first-day trial, the cache fork, the Faction Gift ([11](../../../11%20Vertical%20Slice.md), [19](../../../19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) §6.2) |
| Suspend Save | X.3 | One slot, written on save-and-quit outside a fight or dialogue, after each full sleep and at the invasion, deleted on load; free saves in a development build ([17](../../../17%20Game%20Systems%20Architecture.md) X.3) |

Accepted when (S5 as a whole): a new character can gather, learn and craft, buy, steal, brew, drink the Limit 9 potion, earn Conduct Progress, and reach Limit 8 within two game days; it can join the faction through the trial or keep the cache; and a Suspend Save restores the run exactly.

### S6 — Invasion

| Part | Systems | Notes |
|---|---|---|
| The General and the ritual | E.1b | The Spirit General comes out on day 2 between 12:00 and 24:00, the ritual starts at once and lasts 60 ticks, the General leaves after 15–20 ticks ([11](../../../11%20Vertical%20Slice.md), [21](../../../21%20Cult%20Campaign%20and%20City%20Control.md) §4) |
| Forces | E.1b, A.2 | The ritual group at its magic circle: conductors who never answer, guards who take an empty place, a circle that waits while short and fails after 10 ticks short; the main force, the allies; the allies' directive; Force Estimate and Cult Retreat ([21](../../../21%20Cult%20Campaign%20and%20City%20Control.md)) |
| Fights away from the player | C.5 | The same tactical rules without drawing, on the shared tick ([18](../../../18%20World%20Time%20and%20Living%20Simulation.md)) |
| Weak Points and traces | Q.2, Q.3 | Two Weak Points and one Ritual Site Candidate by the seed; their traces in the city ([11](../../../11%20Vertical%20Slice.md), [21](../../../21%20Cult%20Campaign%20and%20City%20Control.md) §4) |
| Endings | — | Win, loss, death, Sanity 0 |

Accepted when: the ritual can be stopped by a fight, by stealth, and by a Weak Point; with no player, headless runs give the allies about 21–23 %, as the owner accepted ([11](../../../11%20Vertical%20Slice.md)), within the simulations' spread.

### S7 — Polish

As [11](../../../11%20Vertical%20Slice.md) lists: readable urgency, failure that teaches, nothing that pushes toward grind. Then a playtest against the slice's Success Criteria.

---

## 4. Existing code, checked against 17

Owner's decision 3. The check read each file's structure against the System Cards; it changed nothing.

| Code | Verdict | Why |
|---|---|---|
| `World/WorldSeed` | **Keep** | Already `Fork(name)` with `System.Random`, as W.1 asks; move to `World/Seed/` |
| `Input/GameInputService`, `Input/GameInput.inputactions` | **Keep**, extend | Input System actions; add the slice's actions |
| `Player/OverworldPlayerController`, `Player/OverworldThirdPersonCamera` | **Keep**, move | Free third-person movement and an orbit camera with collision; move to `Locomotion/` and `Camera/`, and free them from the Overworld scene's setup. Done in S0.3 as `PlayerLocomotion` and `ThirdPersonCamera` |
| `Grid/CombatGridReader` | **Keep** as the base of C.2 | It passed the grid test; production work: stop allocating on every step (gate G2), keep characters out of the reading, read the kit's tags |
| `Grid/CombatGridLab` | **Lab only** | Its following, transparent walls, and outlines move into production code; the lab stays as a tool |
| `World/WorldClock` | **Rewrite** | Float hours at 45 real seconds an hour, no tick events, no round tick; 17 X.1 asks for `IWorldClock` with one-minute ticks at 10 seconds each |
| `World/PoiDirector`, `PoiEnums`, `PoiShellId`, `PoiShellMarker`, `TipGiver` | **Rewrite**, keep the rules | POIs are hard-coded in C# and drawn with debug GUI; the window rule (from appearance) and unattended resolution move into the data-driven Q.3 |
| `Decisions/DecisionNodeDef` | **Keep** the shape | Its fields follow 03.1; the definitions become data assets |
| `Decisions/DecisionNodeCatalog`, `DecisionNodeSession` | **Rewrite** | Content in code and debug GUI with keys 1–5 |
| `World/DistrictResidue` | **Rewrite** | Hard-coded NPC lines; becomes the world state store of C.6 |
| `Interaction/SimpleInteractable`, `InteractionFeedback` | **Rewrite** | Becomes the interaction layer: examine, talk, open, gather |
| `Grid/GridMap`, `GridPathfinder`, `GridCell`, `GridSettings`, `GridDebugDrawer`, `Player/GridPlayerController`, `Environment/*` | **Not used** | Pre-pivot grid world, whose free grid movement 16 cancelled; the in-fight pathfinder is written over the Combat Grid window |
| `World/CityGenerator`, `CityDistrict`, `LowerVeilGreyboxBuilder`, `Landmark*`, `S0_LightingController`, `BillboardLabel`, Editor `S0GreyboxMenu`, `TownGenerateMenu`, `CityGenerateMenu` | **Not used** | Pre-pivot archive ([16](../../../16%20Architecture%20Pivot.md) §5) |
| Terrain, Overworld, and research Editor tools | **Untouched** | Not part of the slice |

The project has no assembly definitions of its own, and its existing tests (terrain, roads, seed) live in the Editor assembly. S0 adds assemblies for the slice's code and an EditMode test assembly beside them; the seed's folder becomes its own assembly, which the rest of the project still sees.

---

## 5. Content pack

Content is written with the owner, by the usual method, just before the milestone that needs it, and kept in documents rather than in code.

| Piece | Needed by | Contents |
|---|---|---|
| C0 Veilmouth map | S0 | **Done, 2026-11-04** (§2): districts, the buildings and which open, shops, forest zones, territories, resource nodes, the three Ritual Site Candidates; the street plan itself is drawn in S0.6 |
| C1 People | S1 | 6–10 named NPCs and the Masters: role, place, schedule, what they know, what tips they give |
| C2 POI A and B | S2 | Full Decision Node cards, dialogue, the Arguments each needs |
| C3 Beasts and enemies | S3 | Where each territory lies, which beasts glow, what they leave |
| C4 POI C and the Bargain | S4 | The trace, the cellar, the black drop |
| C5 Goods and the trial | S5 | Shop shelves, the trial's investigation, the cache, the formulas |
| C6 The invasion | S6 | The groups and where they go, the Weak Points' content, their traces |

---

## 6. Tests

- **EditMode** for pure rules: the clock, the action-check curve, Social Checks, hit chance, damage, HP, AP, the Brewing Factor, XP, Sanity costs, prices. Expected values come from the design documents and the simulations.
- **Headless parity** for fights: the C# rules run the lines of `beast_sim.py` and `tactical_sim.py` and must land within 3 points of their results.
- **PlayMode smoke** for each milestone's acceptance, where a script can check it.

---

## 7. Open

- The triangle and texture budget of the kit (kit standard, *Open*).
- Whether the full game later reuses the slice's city as a hand-built landmark on the seeded Overworld, or rebuilds it.

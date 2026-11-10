# Vertical Slice — Implementation Plan

Status: Draft — the owner's decisions are marked; everything else is the agent's plan and authoring defaults
Created: 2026-11-04
Last Updated: 2026-11-11 (S3a.6 done: the Reaction Attack, Surrounded, a shot's line of sight and cover, higher ground, the Point-Blank Shot, Wait, the Defensive Stance with its Counterattack, and two Weapon Sets. Earlier: S3a.5 done: the player's turn by the cursor, under the fight camera, with the order, HP and AP on the HUD and the moves and blows shown beat by beat. Earlier: 2026-11-10: since C5 Bawling Square's market is Bawling Row on the Penny (city plan §29.6). Earlier: names since C4: Bell Street is the Black Puddle, Loom Lane Draught Lane, the Market square Bawling Square; the records keep their day's names (city plan §28.10). Earlier: S3a.4 done: the Encounter Lock and the turns on the Combat Grid, one world tick after each round, and F7's development fight. Earlier: S3b.0, the land outside the walls, planned from the owner's fifth play test (city plan §26.2): grass, a nearer and denser forest, an entrance, a hamlet with cows, paths, hunting grounds, rocks and a mountain backdrop; S5a's gathering and C3 point to it. The city's part of the play test fixed before S3a.4. Earlier: S3a.3 done: the Combat Grid in `GameDev.Confront.Grid` behind `ICombatGrid`, with occupants, cover on each side, the 10.1 tags, paths and a window that follows the player with no allocation; S0's doorway and house-front readings brought up to date. Earlier: S3a.2 done: the headless fight lands on 10.2's beast table, re-run with `beast_sim.py` striking as 10.2 §5b reads. Earlier: S3a.1 done: the fight's numbers of 10.2 §5b as one service, `FightRules`, with weapons, armour and the beasts' natural weapons as data. Earlier: the city's greenery pass, asked for by the owner before S3a.1 (city plan §25). Earlier: S2 accepted and S3 authorised by the owner; S3's iterations proposed with their authoring defaults; by the owner's answers S3 is split into S3a, the fight, and S3b, stealth, beasts and the POIs, people carry a few pistols, the beasts are the new Wolf and Wild Boar packs, and a development kit gives the gear until S5a. Earlier: S2.7 done: the next day's residue in the world, Nell's day-2 lines and a lie found out, and S2's acceptance test. Earlier: S2.6 done: POI B as a Decision Node, its night scene on Loom Lane, and its conversation. Earlier: S2.5 done: POI A as a Decision Node, its scene before Harrow & Tincture, and its conversation. Earlier: S2.4 done: the dialogue column, `E  Talk`, and Nell's and Kit's conversations. Earlier: S2.3 done: a conversation as data and the session behind `IDialogueRunner`. Earlier: S2.2 done: the Deeds Record, Reputation and Danger, Attitude, and lies found out at midnight, all in the world state. Earlier: S2.1 done: the character's values, the action check of 07.1 and the Social Check of 10.4, their numbers as data. Earlier: S2.0 done: everything S1 shows is a string table key, in English and Russian, switched on F10. Earlier: S2.0 Localization added first, by the owner's direction: every line, name and piece of UI by key, in Russian and English. Earlier: S2's iterations accepted by the owner: a Disco Elysium column at the right, later milestones' options hidden. Earlier: S1 accepted and S2 authorised by the owner; S2's seven iterations proposed with their authoring defaults and three questions for the owner. Earlier: S1.6: the player's map copied from the city plan C3, by hand. Earlier: interiors: Victorian Interiors chosen by the owner, the rebuild planned in `VICTORIAN_INTERIORS_PLAN.md`, before S1's acceptance walk. Earlier: S1.6 done: the map on M and S1's acceptance test; S1 awaits the owner's acceptance. Earlier: S1.5 done: the fourteen named people stand in Veilmouth and four interiors, named nearby, Nell and Kit as tip sources. Earlier: S1.4 done: the Character Notes on J, one line for each fact found, in order, in the character's words. Earlier: S1.3 done: the Notice Board, the publican and the newsboy say their tips as data, by 09's rule from Hinted to Marked, in a plain panel. Earlier: S1.2 done: the POI Director runs POI A, B and C on the clock and writes their unattended Aftermath. Earlier: S1.1 done: the world state store and its save section. Earlier: S1 iterations and the People pack accepted: fourteen people, placeholder names, a map on M, a plain panel until S2. Earlier: S0 accepted and S1 authorised by the owner, 2026-11-06. Earlier: street dogs, an experiment by the owner: the Tripo shepherd in the Dogs Kit clips, five dogs in Veilmouth. Earlier: the smaller city C2, by the owner after the fourth play test (VS-SIZE): the ground shaped again and the city rebuilt, city plan §21. Earlier: detached houses: the Terraces and the Ridge stand on railed green plots, by the owner after the third play test (VS-PLOT). Earlier: loose props: tables, chairs, crates, barrels, benches, and the things on sets have mass and gravity, by the owner's choice after the third play test. Earlier: the townsmen are the owner's new man, in the gentleman's retargeted clips; the gentleman's models are removed. Earlier: the owner's second play test of the city and interiors, fixed and rebuilt at the owner's request: city plan §17; interior doorways now pass two grid cells over a node. Earlier: the walking townsfolk are 7 women and 8 men: the owner's Tripo woman joins the gentleman. Earlier: the ordinary townsfolk: the owner's Tripo gentleman replaces the yellow Dummies and walks closed routes, a stand-in for S5c's schedules. Earlier: S0's acceptance in the slice's scenes: the Combat Grid reads Veilmouth's streets, fronts, railings and doorways as expected, after three fixes; the owner's walk remains. Earlier: step 4a's first half: the Dwelling scene, its generator, and 265 dwelling doors, city plan §16; breaking in waits on S2, S3, S5a, and S5c. Earlier: S0.6 rebuild: the seven interiors built from BK_AlchemistHouse, city plan §14. Earlier: the Dwelling scene and breaking in added after the seven interiors, by the owner. Earlier: S0.6 rebuild: the city built from C1 with its green cover, replacing C0's dressing. Earlier: S0.6 rebuild: BK's materials in URP, the modules measured, the ground shaped from C1. Earlier: C1 accepted by the owner; the S0.6 rebuild follows its §11. Earlier: S0.6: the owner's play test of the dressing pass, the fixes of the same day, and the city plan C1 proposed; the agent shapes the ground once from it. Earlier: S0.6: the agent's dressing pass of Veilmouth and its seven interiors at the owner's request, and what the scene still lacks. Earlier: the ritual is a magic circle with three conductors and two guards, and the main force is 4–6, by 21 §4 and 11. Earlier: no factory locations in the slice: Ritual Site Candidate 1 is a square in Lower Veil, by the owner. Earlier: the inventory's money is in bronze, by 19 §6.3; the asset map takes the owner's second list. Earlier: the slice's asset map, SLICE_ASSET_MAP.md, by the owner's list of packs. Earlier: S0.6a: the Veilmouth scene's starting point. Earlier: C0, the Veilmouth map, accepted by the owner: §2)
Authority: [11 Vertical Slice](../../../11%20Vertical%20Slice.md) (what the slice is), [17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md) (systems and folders), [16 Architecture Pivot](../../../16%20Architecture%20Pivot.md) (scene locks)
Companion: [SLICE_MODULAR_KIT_STANDARD.md](SLICE_MODULAR_KIT_STANDARD.md), [SLICE_ASSET_MAP.md](SLICE_ASSET_MAP.md) (which pack serves each mechanic, character, and place)

**Names since C4** ([VEILMOUTH_CITY_PLAN.md](VEILMOUTH_CITY_PLAN.md) §28.10, 2026-11-10): Bell Street is the Black Puddle, Loom Lane is Draught Lane, and the Market square is Bawling Square. Since C5 (§29.6) Bawling Square is gone: its market is Bawling Row, the stalls round Stone Grandad on the Penny. The records below keep the names and places of the city they were built in.

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
| Townsfolk who move, keep schedules, and talk | the 15 ordinary townsfolk walk closed routes (2026-11-06); the rest stand in their idle | S2, S5c |
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

**Still to do, in order, now that the owner has accepted C1** (the city plan's §11): ~~measure the Victorian modules and BK_AlchemistHouse and give BK's built-in Standard materials URP copies~~ (done 2026-11-05: ×1.25 and ×1.2, city plan §2 and §8); ~~shape the ground~~ (done 2026-11-05, city plan §9); ~~build the city from plots and modules by the street sections, the street furniture rules and the courtyards~~ and ~~plant the green cover~~ (done 2026-11-05 by `VeilmouthCity`, city plan §13, which lists what this pass still lacks); ~~rebuild the seven interiors from BK_AlchemistHouse with ceilings, larger and of several rooms~~ (done 2026-11-06 by `VeilmouthInteriors`, city plan §14, ground floor only). The rows of the table above that C1 changes: the ground is now the agent's (once, then the owner's); the portal leaf is hidden; the people stand in an idle, though walking and schedules are still S2 and S5c; the beasts stand in their idle, though their behaviour and fights are still S3. New rows: the city's edge, wall and gates; lamps by the street furniture rules; passable arches into courtyards. Added by the owner on 2026-11-06, after the seven interiors: the Dwelling scene that assembles an NPC home's rooms from the house's type and id, a door on every dwelling, and breaking in with witnesses, the Wardens and the Arrest ([10.3](../../../10.3%20Stealth%20and%20Detection.md) §6.1–6.2). ~~The Dwelling scene and a door on every dwelling~~ done 2026-11-06 (city plan §16); the doors open without a lock until breaking in is built, which needs the Reaction Check and Lockpicks (S5a), witnesses (S3), the Wardens' walking (S5c), and the Arrest's dialogue (S2). The owner chose to wait (2026-11-06): no stand-in is built ahead, and each part comes with its own milestone.

**S0's acceptance in the slice's own scenes** (agent, 2026-11-06). `VeilmouthGridCheck` (*GameDev → Slice → Check the Combat Grid in Veilmouth*; batch `GameDev.EditorTools.VeilmouthGridCheck.RunBatch`) reads Veilmouth, its seven interiors, and a sample dwelling of each kind with the Combat Grid's reader at 1 m cells, and writes `Logs/SliceGrid/report.txt` with a map of each street window. It only reads scenes. **Measured**, with thresholds that are authoring defaults:

| Reading | Result |
|---|---|
| A street reaches open ground (at least 200 cells of the 41 × 41 window) | 8 of 8 sites: the player's start and the seven street doors, 609–1505 cells |
| A house front closes the edge from the street into the house | 248 of 248 dwelling doors closed; 17 doors have no street cell to stand on beside them, on steps or a slope |
| A railing or fence reads low cover, not high, and the grid does not cross it | 1326 of 1326 sides low, 663 of 663 crossings closed |
| A doorway between rooms lets its cells through, one over a cell's middle and, since the 4 m module of 2026-11-06, two over a node; a doorway with a door leaf none | 0 faults in the seven interiors and five dwellings: 15 + 10 open doorways pass one cell, 9 + 5 with a leaf pass none |

Found and fixed by the check:

- **The railings read as high cover.** Their collider stood 1.36 m, over the kit standard's 0.8–1.2 m for low cover, and the railing stands 0.1 m above the cells beside it. The railing is now 1.04 m high, with a collider of 1.20 m (`SliceKitCatalogue`), and every side reads low cover. Before: 57 %.
- **The grid read the characters**: its mask took in the slice's Character layer, so a standing person or the player closed cells. It now leaves out Character and NoGrid, as the slice target says: the grid reads the place, never the characters in it.
- **Every interior floor would have read as water**: the reader took any surface below Sea Level (24 m) as water, and the interiors stand near 0 m. Water is now the terrain below Sea Level only.

The other parts of S0's acceptance were met earlier: walking the city in third person, the doors in and out with the clock running, the clean overlap check, and the tests. EditMode 64 and PlayMode 18 pass. **Left for S0:** the owner's walk through the city and the forest, the city plan's §11 step 6. S1 starts only on the owner's authorisation. Done: through the play tests of 2026-11-05 and 2026-11-06 the owner accepted S0 and authorised S1 on 2026-11-06.

**Walking townsfolk** (owner, 2026-11-06: "replace the ordinary yellow NPCs with the new model and let them simply walk round the city on a looped route"). A stand-in for S5c's schedules, which will replace the routes. `SliceTownsfolk` (*GameDev → Slice → Townsfolk → Build the Townsperson and Replace the Yellow Dummies in Veilmouth (once)*) wraps the owner's gentleman as `Prefabs/Slice/Townsperson.prefab` and replaced the 15 yellow Dummies of Veilmouth once, at their places and with their names; it refuses to run again, since the scene is the owner's. The same day the owner added a woman ("let there be as many of each"): *Build the Townswoman and Make Half the Townsfolk Women in Veilmouth (once)* wraps her as `Prefabs/Slice/Townswoman.prefab` and turned the 7 walkers flagged female in the yellow list into townswomen, at their places and on their routes; it too refuses to run again. With 15 walkers, 7 and 8 is as even as it gets. `TownsfolkWalker` walks a closed route at a steady pace, keeps its feet on the ground, and turns smoothly; people pass through one another. Authoring defaults unless marked:

- A route is planned on 1 m cells within 22 m of the person's place, over ground a person's capsule (1.75 m, radius 0.3 m, step 0.4 m) stands and moves on. It turns at up to four points 8–18 m away, one in each quarter, joined by the shortest cell paths and straightened where a person fits along the straight line. Routes are 61–93 m round.
- **The new man** (owner, 2026-11-06: "replace the old model, it was crooked; keep only the woman and the last man"). `SliceTownsfolk.RebuildMan` (*GameDev → Slice → Townsfolk → Rebuild the Townsperson from Its Model*) rebuilt `Townsperson.prefab` from the owner's stylized male character, and the 8 townsmen of Veilmouth keep their places and routes at his pace. He has no clips, so `FitGentlemanClipsToMan` retargeted the gentleman's walk and idle to his rig once, before the gentleman's models were removed; it cannot run again.
- **Measured:** the man walks 1.00 m/s (the gentleman walked 0.98) and the woman 1.13 m/s, the backward speed of the foot on the ground in each walk clip, so the feet do not slide.
- Many loops are thin triangles along a street, not rounds of a block: they are stand-ins.

Tests: `TownsfolkTests` in EditMode (both prefabs' looping clips; 15 walkers, 7 women and 8 men, no yellow Dummy left, a person fits all along every route) and PlayMode (in Veilmouth every walker moves, stands on the ground, keeps to its route, and plays its walk). EditMode 67 and PlayMode 19 pass.

**The owner's second play test** (2026-11-06). Ten remarks on the city and interiors, each answered in the city plan's §17 ([VEILMOUTH_CITY_PLAN.md](VEILMOUTH_CITY_PLAN.md)), and both passes rebuilt at the owner's request: the dressing is still (vendor rigid bodies removed), furniture stands at game scale ×1.2, BK's rooms are on a 4 m module, walls are set by their infill so hung pieces touch the plaster, steps are one style with no gap, props tilt to slopes, railings have gates only before doors, and a shop's ground floor is blue all round. Checked: 0 kit faults in the city (3284 blocks), the seven interiors (749), and 56 sample dwellings; `VeilmouthGridCheck` passes, an open interior doorway now passing two cells over a node; the `slice`, `slice-play` and `world` test sets pass.

**Loose props** (owner, 2026-11-06, after the third play test; kit standard §4). `SliceKitCatalogue` gives 32 kinds a sleeping rigid body with the mass of its kind (`LooseMass`) and convex colliders on its meshes, and every thing on a set a body of its own on the NoGrid layer; `LooseProp`, `ThingSurface`, and `PushLooseProps` live in `Scripts/Slice/Kit`. `LoosePropsPass` rebuilt the kit and the Dwelling scene once, which carries the change to every instance in Veilmouth and its interiors, gave the saved player its push and the townsfolk their kinematic bodies. Checked (`LoosePropsPass.CheckScenesBatch`): 0 kit faults in the city (3284 blocks, 79 of them loose), the seven interiors (77 loose props and 126 things on sets), and the 56 Dwelling samples; `VeilmouthGridCheck` passes. Tests: `LoosePropsTests` (PlayMode): in the tavern, the apothecary, and Veilmouth every loose prop, woken, rests within 0.15 m of where it stood, and every thing has left its set; the player walking into a chair moves it. The `slice` (67), `slice-play` (23) and `world` (302) sets pass. Not kept across a load until S5c; breaking is V2 (VS-PHYS).

**Detached houses** (owner, 2026-11-06, after the third play test; city plan C1 §19, VS-PLOT). `VeilmouthCity` places each house of the Terraces and the Ridge on its own plot: railings all round with one 3 m gate opposite the door and no other hole, a grass garden with trees in the back corners, shrubs and poppies along the sides, flowers along the front, and a bench. `VictorianHouses` bakes the Terraces' house as `_detached` and the villa with both side facades. Two plots of 22 m stand on each of the Terraces' four rows, the arch house to Wardley Garden unrailed with a green beside it; seven villa plots and the faction's house of 36 m stand on Ridge Street. A house floor never rises above what its steps can reach inside the front garden. Rebuild of 2026-11-06: 3376 blocks, 269 dwelling doors, 0 kit faults, the least room between a door's steps and its railings 1.78 m (§19 asks 1.5 m); grid check PASS, railings 1437 of 1438 sides low cover and 719 of 719 crossings closed; slice 67/67, slice-play 23/23, world 302/302.

**The smaller city C2** (owner, 2026-11-06, after the fourth play test; city plan §21, VS-SIZE). Veilmouth is 300 × 204 m, C1 at ×0.6, with crossroads every 40–90 m, each lit at its corners, and the Ridge 4 m high so the shorter avenue keeps its 10 % climb. The ground was shaped again from C2 and the city rebuilt, with every place and the seven buildings that open kept and fewer repeats: four villas and the faction's house, one terrace plot each side of Terrace Street, the palace without its wings, one arch house. The player still starts on Bell Street before the Soot & Bell. Rebuild: 2349 blocks, 0 kit faults, the least room between a door's steps and its railings 3.18 m, every loose prop at rest; grid check PASS, railings 830 of 830 sides low cover and 415 of 415 crossings closed. There are 77 dwellings, C1 had 269, so the tests now ask for more than 60 dwelling doors and 120 doors in all. Slice 67/67, slice-play 30/30, world 302/302.

**Street dogs** (owner, 2026-11-06: "take the shepherd's clips and put them on our dog; let a few dogs run round the city, like police dogs; for now I only want to see how the clips play"). An experiment towards VS-BEASTS, not its answer. `SliceDogs` (*GameDev → Slice → Dogs → Build the Dog and Place Dogs in Veilmouth (once)*) carries the 12 clips of 3D Stylized Animated Dogs Kit's German shepherd over to the owner's Tripo shepherd, bone by bone, since both have the same rig: each bone turns in the world as the kit's turns from its rest, a moved bone moves by the kit's amount scaled to the dog (×0.931), and each frame is lifted or lowered so the skin's lowest point stays on the ground (at most 7 cm, in the run). It builds `Prefabs/Slice/Dog.prefab` and places five dogs under `Veilmouth C2/Dogs`: on Bell Street by the player's start, the Market square, High Street, Ridge Street, and the Sea Quay, each on a route planned as a townsperson's. `StreetDog` walks the route (Walking01) or sits (the kit's Sitting, which sits up and begs), even odds, 8–20 s and 6–15 s; within 3 m of the player it turns to the player and wags its tail (WigglingTail) until the player is 4.5 m away. All numbers are authoring defaults; the walk, 0.47 m/s, is measured from the clip's stride. The other clips (Walking02, Running01, Breathing, Eating, Angry) are in the dog's Animator for the owner to look at. Checked: the poses beside the kit's in `Logs/SliceDogs`; `DogTests` (PlayMode): the five dogs stand on the ground, walk or sit, and greet the player.

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

The owner accepted S0 and authorised S1 on 2026-11-06 ("дальше S1"). **S1 iterations**, proposed by the agent the same day and accepted with the owner's answers of 2026-11-07. Each is one micro-plan of at most about 500 lines, like S0's. The content is [SLICE_PEOPLE.md](SLICE_PEOPLE.md) (content piece C1, accepted).

| Iteration | Contents | Done when |
|---|---|---|
| S1.1 World state | `IWorldState`: named flags and small values that residue and Aftermath write and the world reads (C.6), with its save section `world.state` | EditMode tests: write, read, save, and load |
| S1.2 POI Director | POI A, B, C as data assets: appearance, Active Window, discovery (Unknown, Hinted, Marked, 04), and an unattended resolution that writes its Aftermath into the world state on the clock's ticks; the old `PoiDirector`'s rules kept, its code rewritten (§4) | EditMode tests on a fake clock: each window opens and closes at its times, and a POI left alone writes its Aftermath |
| S1.3 Tips | The Notice Board's notices, the bartender's evening tip, and the newsboy's rumours as data from SLICE_PEOPLE §2; 09's Hinted-to-Marked rule; reading or asking shows the line in a plain panel until S2's dialogue | EditMode tests on the rule; reading the board in a PlayMode test marks POI A |
| S1.4 Character Notes | One line for each found fact, in order, in the character's words, on a UI Toolkit page, with nothing that says what to do (11) | A tip's fact appears in the Notes |
| S1.5 Named people | The roster of SLICE_PEOPLE §1 at their places in Veilmouth and the interiors, replacing the Dummies of those roles, each with a name the player sees nearby | An EditMode check: every named person stands at its place, once |
| S1.6 Map and acceptance | A simple map on M: the C2 city plan, a Marked POI as a pin and a Hinted one as an area, with no countdown (04 rule 5); a PlayMode test of S1's acceptance in Veilmouth | The acceptance above |

The owner's answers (2026-11-07): all fourteen people with the agent's placeholder names; a simple map on M; a plain panel for reading the board and asking the publican until S2's dialogue replaces it. Authoring default: in S1 a POI runs only its window and its unattended resolution; its scene and choices come with S2.

Progress: **S1.1 done** (2026-11-07). `GameDev.World.State` holds `IWorldState` and its plain model `WorldState`: whole-number values by dotted key, a flag being a value that is not zero, a key never written reading as zero, a value set to zero leaving the store, and every change announced once with its old and new value. `WorldStateSaveContributor` writes the section `world.state` (version 1); a load replaces the whole store and announces nothing, since a load is not an event in the world. Its runtime owner comes with S1.2, the POI Director being its first writer. 5 EditMode tests (`WorldStateTests`).

Progress: **S1.2 done** (2026-11-07). `GameDev.World.Poi` (folder `Scripts/World/PoiWindows`, apart from the Overworld's placement code in `World/Poi`) holds `PoiDefinition`, a POI as data, and `PoiDirector`, a plain model on the clock's `TickBoundary`. A POI opens at its minute or once any POI it follows has resolved, and its window closes after its length or at a fixed minute; left alone it resolves unattended, drawing one outcome by weight from the seed stream `slice.poi.unattended` (forked by the id's FNV hash) and raising that outcome's keys. Discovery (Unknown, Hinted, Marked) may rise before the window opens and never falls; `Resolved` says whether the player had heard of the POI, so S1.3 can announce it or stay silent. All of it lives in the world state under `poi.<id>.phase`, `.discovery`, `.opened_at`, `.resolution`, `.outcome`, so the `world.state` section saves it, and a load catches up with one `Evaluate`. `ResolveParticipated` is the hook S2's scenes call; it writes no Aftermath of its own. Assets in `Assets/Data/Slice/Poi` (authoring defaults): **A** opens at 14:00 and closes at 20:00, the second-shift bell, *before the whistle*, Wardens (Harrow taken, shop sealed) or Veinmarket (Harrow vanished, drawers emptied); **B** opens at 23:00 and closes at 02:00, *tonight*, Veinmarket (girls taken) or Wardens (snatch spoiled, trail colder); **C** opens when A or B resolves and fades at the end of day 2, a stand-in until S6's invasion closes it, *before the rain*. 11.1 lets the locally stronger side decide A and B; the slice has no reading of local strength, so each side has an even weight until one exists. Places and hint areas are zero until S1.6's map sets them. The runtime owner of the world state and the director moves to S1.3, whose tips are its first runtime reader. 8 EditMode tests (`PoiDirectorTests`).
Progress: **S1.3 done** (2026-11-07). `TipCatalogue` (`Assets/Data/Slice/Poi/Tips.asset`) holds SLICE_PEOPLE §2's ten lines: the board's two morning notices and the seizure notice pinned at 14:00 on day 1, the Wren notice every day; Nell Garrow's tip from 16:00 (POI A on day 1, POI C on day 2 once it is open); Kit Mallow's two rumours on day 1 and, on day 2, the seal rumour only if the Wardens sealed the shop and the glove rumour once B has resolved. Each line names its source, day, minute, POI, and whether it Hints or Marks. `TipBook` reads a source, hears every line it says at that moment, and applies 09's rule: Marks marks, Hints hints, and Hinted tips about one POI from two different sources mark it. Heard tips are kept in the world state in the order heard (`tip.<id>.heard`, counter `tips.heard`), so a save keeps them and S1.4's Notes read them. Nell's Loom Lane tip needs an Approach and waits for S2. The board's money tasks wait for the task system, and S1 has no false rumour (09 allows at most one a day). `SliceWorldDriver`, the run-time owner of the world state, the director, and the tips, lives through scene changes like the clock. `TipSourcePoint` is a source in the scene: Interact within reach shows its lines in the HUD's plain panel, and Interact again or walking away closes it. Asking a person pauses the world and costs one tick when it ends (10.4); reading the board does not. The HUD shows `E  Read` or `E  Ask` through `IPromptSource`. Veilmouth, the owner's scene, gained only the `SliceWorld` object and the board's source, by the one-shot `SliceWorldSetup.AddToVeilmouth`; `AddPlayerRig` adds the world to scenes built later. The publican and the newsboy get their sources with S1.5, which places them. Announcing the end of a POI the player had heard of comes with S1.4. 8 EditMode tests (`TipBookTests`) and a PlayMode test (`TipTests`): reading the board at 14:00 in Veilmouth marks POI A.
Progress: **S1.4 done** (2026-11-07). Each tip carries a `note`, its fact in the character's own words: who said it and what, never what to do with it. `CharacterNotes` lists the notes of heard tips in the order heard. It never sorts, marks, or links them, names no place still to visit, sets no task, and has no limit (11). Physical Traces and scenes add their facts with S2. J opens the Notes page in the HUD, a UI Toolkit page with one line for each fact (`Nothing noted yet.` before the first), and J again closes it. A fact found while the page is open appears at once. The world runs on while the Notes are open. The old code's rule that the end of a POI the player had heard of is announced is not carried over: by 04 and 09 the end reaches the player through sources and Symptoms (Nell's and Kit's day-2 lines, S2's Aftermath dressing), and `Resolved`'s `known` flag is kept for the private daily summary (21). 3 EditMode tests (`CharacterNotesTests`); `TipTests` now also opens the Notes after reading the board and finds the seizure notice's fact.
Progress: **S1.5 done** (2026-11-07). `SliceRoster` lists SLICE_PEOPLE §1's fourteen, and `NamedPerson` gives each its id, name, and role. The one-shot `SlicePeople.PlaceAll` put them in the owner's scenes. Eleven took the place and pose of the Dummy that stood for their role, as a Dummy of the role's colour and the person's sex: Nell Garrow and Dace in the Soot & Bell, Josiah Harrow in the apothecary, Edda Brann, Silas Quill, Ada Merrow, Tobias Penn, and Madame Ilse on the Market square, Ruth Kettle on Loom Lane, Sergeant Coyle in the Wardens' station, and Mr Ashgrave in the faction's house. Three are new, each at the first free place of a short list: Pip Vance in the apothecary's back room, Kit Mallow on the Market square before the Masters' stalls, and Lottie Wren in the Soot & Bell. Lottie stands in the tavern all day until S5c gives her the tenement by day; the tavern is where the trial's door is easiest to find (authoring default). Nell is the bartender source (reach 3 m) and Kit the Street Rumour source (2 m), so asking them shows their S1.3 lines and pauses the world. The HUD names the nearest named person within 6 m, over their head (`INameTag`; authoring default). The other Dummies (Wardens, faction members, Veinmarket's men, townsfolk) stay. An EditMode check (`SlicePeopleTests`): every named person stands once, in its scene, on a floor, as the right Dummy, and only Nell and Kit are tip sources. A PlayMode test (`TipTests`): Kit's name shows nearby, asking him pauses the world, and the talk costs a tick.
Progress: **S1.6 done** (2026-11-07). M opens the map, and M again closes it. The map draws the city plan (C2, and C3 since 2026-11-08) with UI Toolkit's vector API (`CityMapElement`, which reads the plan's SVG subset). It shows each POI the player knows of whose window is still open: a Marked one as a pin with its title and its source's words (*before the whistle*), a Hinted one as an area of its hint radius. It shows no number, so no countdown (04 rule 5). In Veilmouth it also shows the player as a dot. The drawing is the player's copy of the plan, `Assets/Data/Slice/Map/VeilmouthC3Plan.xml` (copied from C3 by hand on 2026-11-08, city plan §24.7; C2's copy before): the planner's legend, crossroads, street widths, and Ritual Site Candidate 1 are taken out, because the seed's ritual site must not be on the map. A later plan change is copied by hand. POI places, from Veilmouth's doors (authoring defaults): A at Harrow & Tincture's door, area 25 m; B on Loom Lane, 35 m; C at the Soot Stair cellar door, 25 m. Opening the map closes the Notes, and opening the Notes closes the map. The world runs on while the map is open. Tests: `S1AcceptanceTests` walks S1's acceptance in Veilmouth (reading the board at 14:00 marks A, the map shows A as a pin and B as an area with no number, A resolves alone at 20:00 with its Aftermath in the world state and leaves the map, and the board's fact is in the Notes), and `CityMapPlanTests` checks that the map names no ritual site. `RenderTheMap` (explicit, run by name) writes `Logs/SliceMap/map.png`. **S1 is built.** The owner accepted it on 2026-11-09, after the Veilmouth reference plan, and authorised S2.
Interiors (owner, 2026-11-07, before S1's acceptance walk): every interior is rebuilt from Victorian Interiors by the five iterations of [VICTORIAN_INTERIORS_PLAN.md](VICTORIAN_INTERIORS_PLAN.md), each on the owner's authorisation; it changes no system of S1.


### S2 — Decisions and dialogue

| Part | Systems | Notes |
|---|---|---|
| Action checks | P.3 | The shared curve of [07.1](../../../07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md) §4 as one service, used by every later check |
| Dialogue | A.3 | Social Checks of [10.4](../../../10.4%20Dialogue%20and%20Social%20Checks.md): `2d6 + value + modifiers` against `request + listener`, Arguments from this life only, Final Checks; any dialogue pauses the world and costs one tick when it ends |
| Decision Nodes | Q.3, C.6 | POI A and B as data cards of [03.1](../../../03.1%20Decision%20Node%20Template.md); Convince, Threaten, Exchange, Veil, Withdraw; Confront opens S3's fight |
| Residue | C.6 | Shop, patrol, and rumour change after a decision |

Accepted when: POI A and B can each be settled by at least two Approaches without a fight; a failed Social Check cannot be repeated on the spot; residue shows the next day.

The owner accepted S1 and authorised S2 on 2026-11-09 ("S2 — решения и диалоги"). **S2 iterations**, proposed by the agent the same day and accepted with the owner's answers below. Each is one micro-plan of at most about 500 lines, like S1's. The checks and the character's values live in `Player/Progression/` (P.3), the conversations in `Dialogue/` (A.3), the nodes in `Decisions/`.

| Iteration | Contents | Done when |
|---|---|---|
| S2.0 Localization | Added by the owner's direction of 2026-11-09 (17 X.7): `Loc` and the string tables, Russian and English side by side; the language from the system's, switched in the game and kept as a setting; every text S1 shows moved to keys (the HUD's words and prompts, the clock, the sources' titles, the ten tips and their notes, the POIs' titles and words, the fourteen names), translated into Russian; a font that has Cyrillic | EditMode tests: every key has both languages, every tip, POI and person has its keys, a missing key falls back; S1's PlayMode tests pass in both languages |
| S2.1 Checks | `CharacterSheet`, a plain model of the seven attributes (1–100) and the three social skills, with its save section, the schema's `player.character`; `ActionCheck`, 07.1's curve (§4) with linear interpolation, Luck (§6) and the three outcomes (§8); `SocialCheck`, 10.4's two dice (§2): Character and Listener Value, Difficulty, modifiers held within ±4, 1 + 1 always failing and 6 + 6 always succeeding, Luck up to 97 %. The curve, the request sizes and the caps are data assets naming their sections, and rolls come from seed forks (`slice.check.*`) | EditMode tests: the curve and its interpolation, the chances of 10.4 §7's table, Luck's caps |
| S2.2 Standing | The Deeds Record (Reputation points, kills, Limits reached) and its two readings, Reputation and Danger, by 10.4 §4's tables; each named person's Attitude, −2 to +2; the reading a check takes by skill and listener; the step of Attitude Intimidation costs; a lie that comes out at the next day boundary (20 % or 40 %) and costs two steps. All of it in the world state, so `world.state` saves it | EditMode tests on a fake clock: the readings, the choice of reading, a lie found out at midnight |
| S2.3 Dialogue model | A conversation as data (`DialogueDefinition`): lines and options. An option may need Arguments, world-state keys the character gained in this life (a heard tip, a seen event); be a Social Check with its skill and request, each further Argument adding +1 up to +2; show an item; or name its Approach. Outcomes write world-state keys or settle a POI through `ResolveParticipated`. `DialogueSession`, behind `IDialogueRunner` (17 A.3), pauses the world while open and costs one tick when it ends (10.4); a failed check locks its option for good; a failed Final Check against an enemy ends in a fight, which S3 builds | EditMode tests: an option without its Argument does not appear, a failed check cannot be tried again, the Final Check's fight, the tick at the end |
| S2.4 Dialogue panel | A UI Toolkit panel in the HUD with the lines and the numbered options; the chance shown by Intelligence (10: the percentage; 20: the modifiers, 10.4 §3); `E  Talk` on a named person. S1's plain panel goes: Nell's and Kit's lines become conversations, and Nell's Loom Lane tip opens through Convince, Threaten or Exchange (SLICE_PEOPLE §2) | A PlayMode test: asking Nell with an Approach marks POI B |
| S2.5 POI A at Harrow & Tincture | 11.1's card as a Decision Node asset (03.1; the old `DecisionNodeDef`'s shape, §4). The scene appears when the player enters its area within the window (lazy activation, 04): Harrow, two Wardens holding him, a third at the door, the neighbours. Its options are conversations: defend Harrow by Convince or Threaten, help the Wardens by Convince (testify) or Exchange (hold the crowd), walk away. Each writes A's outcome (S1.2) and its residue | EditMode tests on each option's outcome; a PlayMode test: A settled by Convince, and by Exchange |
| S2.6 POI B on Loom Lane | 11.1's card the same way, at night. The collectors are Veinmarket's men, so a check on them is Final: Convince the lane's people to swarm, Threaten the collectors, fetch a Warden by Convince or Exchange (a tip promised), buy the door-watcher's tip, leave | EditMode tests on each option's outcome; a PlayMode test: B settled by two Approaches without a fight |
| S2.7 Residue and acceptance | What the next day shows: Harrow & Tincture open or sealed with wax and brass, Warden pairs on Tincture Row, fear on Loom Lane, Nell's and Kit's day-2 lines by outcome; a lie found out at the day boundary. S2's acceptance as a PlayMode test | The acceptance above |

Authoring defaults proposed with them:

- Until S5b's character creation the character's attributes and skills stay at 1, so its Character Value is 0, as a new character's is (10.4 §7); a development command sets them for tests.
- The listeners' values are data for each named person, from 10.4 §7's paragraph on the slice: residents 0 or 1, the Wardens 1.
- Exchange in S2 is a service or a promise, as 11.1 names them; paying in coin comes with S5a's money.
- A failed Final Check before S3 ends the scene as if the character had fled: the POI resolves as its unattended Veinmarket outcome.
- The scenes stand still: people placed in their poses, with no struggle animated, which S7 may add.
- The lines are the agent's placeholders from 11.1 and SLICE_PEOPLE, mutable as the names are.

The owner's answers (2026-11-09): the seven iterations and their authoring defaults as proposed; the panel is a column at the right edge, as in Disco Elysium, the earlier lines above and the numbered options below, the scene visible to its left; options that need a later milestone (Veil and Confront in S3, a paid Exchange in S5a) are hidden until it comes. On language the owner first chose English, as S1's lines are, then directed the same day: every line, name and piece of UI is translatable from the start, in Russian and English for now (AGENTS, 17 X.7), so S2.0 comes first and every later line is written in both.

**S2.0 done (2026-11-09).** `Core/Localization` holds `Loc` over four String Tables in `Resources/Localization/` (`ui`, `people`, `poi`, `map`; 17 X.7 has their format and keys). Everything S1 shows is a key with its English and Russian: the HUD's words and prompts, the clock, the sources' titles and their lines for nothing new, the ten tips and their notes, the three POIs' titles and words of urgency, the fourteen names and roles, and the 40 names on the city plan. The text fields are gone from `Tips.asset`, `PoiA`–`C` and the five scenes with named people; Tips, POIs and people read their words by id, and the tip sources carry keys. The language starts from the system's, is kept as a machine setting, and goes round on F10 (authoring default until a settings page). The HUD, an open source panel, the Notes and the map follow a change at once, and the HUD binds again when UI Toolkit rebuilds its tree. The Russian is the agent's, as placeholder as the English: the Wardens are «Стража», the brass-numbers «медные номера», meaningful street names are translated (Loom Lane «Ткацкий переулок», Soot Stair «Сажная лестница») and people's names transliterated. Tests: EditMode, the S2.0 classes and those it touched, 37 of 37; PlayMode `LocalizationPlayTests`, `TipTests` in both languages, `S1AcceptanceTests`, `HudTests` and `DoorTests`, 23 of 23. That the HUD draws Russian, Ё, Щ, Ъ, «», — and № included, was checked by eye in a render of it: the labels use Unity's default runtime font, which code cannot read; a project font, when the style guide chooses one, brings its own glyph test. Pre-pivot archive code the slice does not show keeps its English until it is replaced or removed.

**S2.1 done (2026-11-09).** `Player/Progression` holds `CharacterSheet`, the seven attributes and the three social skills, all at 1 until S5b, saved as the schema's `player.character` section (the plan's `character.sheet` was a slip: the schema of S0.5 already named it); `ActionCheck`, the one service of 07.1 §4–8, secondary attributes of §5 included because every later check declares them; and `SocialCheck`, 10.4 §2's two dice with the exact chance over the 36 throws. Their numbers are `ActionCheckRules` and `SocialCheckRules` in `Assets/Data/Progression/`, each field naming its section, and the slice's rolls fork `slice.check.action` and `slice.check.social`. Two readings, not decisions: Δ is taken in raw points of the 1–100 scale, as 10.1 and 10.2 already use it (07.1's task-equivalent conversion stays open); and Luck lifts a Social Check up to the dice's own ceiling, 35/36, which 10.4 rounds to "the same 97 %", so a check with no Luck keeps its exact dice chance. No player-facing words yet: the names of skills and the chance in an option come with S2.4's panel, through the string tables. Tests: EditMode `CharacterSheetTests`, `ActionCheckTests`, `SocialCheckTests` (07.1's curve, its steps and tails, 10.2's First Aid chances, 10.4 §7's table and the slice's examples, Luck's caps, rolls from the seed), with `SliceSeedStreamsTests` and `SaveContractTests`, 76 of 76.

**S2.2 done (2026-11-09).** `Player/Standing` holds `SocialStanding`, what a Social Check reads of the character's deeds and of its listener: the Deeds Record (Reputation points by 10.4 §4's deeds, kills, Limits reached) and its readings, Reputation and Danger, by their tables; each named person's Attitude, −2 to +2; the reading by skill and listener; Intimidation's step of Attitude; 5 Reputation for a failed lie; and the lies that worked, each with one chance at the next day boundary (20 % Ordinary, 40 % Large, never Small), costing 5 Reputation and two steps of the listener's Attitude when it comes out, drawn from `slice.lies` by the listener and the day. Every value is a world-state key (`deeds.*`, `attitude.<person>`, `lie.<request>.<person>`), so `world.state` saves it. Its numbers are `StandingRules` in `Assets/Data/Progression/`. The FNV hash the POI Director used for its forks is now `StableHash` in `World/Seed`, shared, with the same draws. Recorded in 10.4 §4–5 with their labels: two working readings, one Reputation in the slice (Veilmouth's, since everyone in the one city has heard) and Deception against an enemy taking only a positive Reputation, turned against the character; and one authoring default, a Huge lie coming out at 60 %. "The listener warns others" has no rule yet and is not built. Nothing calls it in the game yet: S2.3's conversations will, and the driver gets it with them. Tests: EditMode `SocialStandingTests` on a clock driven by hand (both tables, the deeds, the six readings, Attitude's bounds, lies at midnight and only then, their rates, the same seed, a listener gone, a lie kept through the world state's save), with `SliceSeedStreamsTests` and `PoiDirectorTests`, 37 of 37.

**S2.3 done (2026-11-09).** `Dialogue/` holds `DialogueDefinition`, a conversation as data: its listeners (a named person or a group, enemy or not, and Strength, Intelligence and Will for its Listener Value, residents 0 or 1 and the Wardens 1 as the authoring default says) and its nodes, each a speaker's line and numbered options. An option names its Approach and is talk, a Social Check with its skill and request, or showing an item; it appears only with one of its Arguments (world-state keys; the slice has one life), its needs met and no hiding key set, and each further Argument adds +1 to a check, at most +2. Outcomes raise keys, settle an open POI through `ResolveParticipated`, and move to a node or end. `DialogueSession`, behind `IDialogueRunner`, pauses the clock and costs one tick when it ends, however it ends; previews a check's chance and its modifiers (Attitude, the reading of 10.4 §4, the further Arguments) for S2.4's panel; rolls from `slice.check.social` by the option and the tick; locks a failed check's option for good in the world state; ends a failed check on an enemy in a fight, naming the listener; and pays Deception's and Intimidation's costs through S2.2's standing. Approaches a later milestone builds are withheld by the context, and items show only through a holder it gives, which S5a's inventory will be. `DialogueDefinition.Problems()` finds broken data, a check without an Argument included, and a test holds every dialogue asset to it and to its string-table keys, which follow the ids. Two readings: every enemy listener of the slice can fight, as 10.4 §6 requires for a fight; and a new character's Luck of 1 multiplies every chance by 1.01, as 07.1 §6 says, so a shown 42 % is 42.1 %. Nothing opens a conversation in the game yet: S2.4 brings the panel, wires the session into the driver, and turns Nell's and Kit's lines into conversations. Tests: EditMode `DialogueSessionTests` (an option without its Argument hidden, the pause and the one tick, a failed check locked for good, a failed Final Check's fight, the preview's chances by 10.4, threats and lies paying, withheld Approaches, items and needs, an outcome settling a POI, broken data found), with `SocialStandingTests` and `SocialCheckTests`, 58 of 58.

**S2.4 done (2026-11-09).** The HUD has the dialogue column the owner asked for: at the right edge, what was said above with each speaker's name and the latest at the bottom, the numbered options below, and the scene visible to its left; the keys 1–9 answer, and Escape or walking away leaves. `PersonTalk` is `E  Talk` on a named person: it opens the person's conversation through the world driver, which now holds the character's sheet, its standing, and one `DialogueContext` for every conversation, with Veil and Confront withheld until S3. A check's option names its skill; from Intelligence 10 it shows its percentage, and from 20 the modifiers behind it: Attitude, Reputation or Danger, and the further Arguments (10.4 §3; the two thresholds are fields of `SocialCheckRules`). S1's plain panel now serves only the Notice Board. A conversation says tips: a node may say its source's tips on arriving, in place of its own line, which is said only when the source has nothing; an outcome may tell a tip by its id; and an option may ask for one, appearing only while that tip could be told and is not yet heard (`TipBook.CanTell` and `Tell`), which is how a tip that needs an Approach is told. An outcome may also take game minutes. Nell's conversation follows SLICE_PEOPLE §2: from 16:00 she says her Harrow tip, and her Loom Lane tip, which marks POI B, opens through Convince, Threaten or Exchange, as SLICE_PEOPLE §2 now records; a failed check leaves the other two, and the failed one stays locked. Kit says the day's rumours. A development command, F8 in the editor and in development builds, sets every attribute and skill to 1, 10 or 20 in turn. One working reading, not an owner decision: below Intelligence 10 a check's option still names its skill in brackets, so the player knows a failure locks it, but shows no chance (10.4 §3). Tests: EditMode `SliceConversationTests` (a tip asked for is told only when its source would say it, Nell's options by her Arguments, Exchange marking B for an hour, nothing before the evening, Kit's rumours, the tag by Intelligence, every tip a conversation names in the catalogue), with `DialogueSessionTests`, `TipBookTests`, `SlicePeopleTests`, `LocalizationTests` and `SocialCheckTests`, 57 of 57; PlayMode `ConversationTests` (asking Nell with Exchange marks POI B, in English and Russian; F8 shows the chance and then the modifiers), with `TipTests` (the newsboy now talked to), `S1AcceptanceTests`, `LocalizationPlayTests` and `HudTests`, 12 of 12.

**S2.5 done (2026-11-09).** 11.1's POI A card is a Decision Node asset, `DecisionNodeDefinition` in the new assembly `GameDev.Decisions` (`Assets/Data/Slice/Decisions/PoiANode.asset`), in the shape of the old `DecisionNodeDef`: its two axes, its five options with their Approaches, costs and consequences, and the residue each leaves. An option built later names its milestone, Veil and listen S3 and Buy a minute S5a, and the card checks itself against its conversation (`Problems`). The conversation is the rewritten session of §4; the legacy `DecisionNodeCatalog` and `DecisionNodeSession` stay untouched in the old assembly. The scene stands before Harrow & Tincture's street door in Veilmouth, placed once by an editor command (`SlicePoiStages`): Harrow held by two Wardens on the cobbles, a third in his door, and three neighbours facing them, all posed. `PoiStage` keeps it hidden until the player enters its area, 60 m (authoring default), while A's window is open, and once A has resolved its people leave when the player is away, so nobody sees them go. The conversation (`PoiADialogue`, words in the `dialogue` table in both languages) opens with `E  Talk` within 3 m. After the scene's line the player steps up to the Wardens and may ask the neighbours, which gives an Argument. Defend Harrow by Convince or Threaten, to the Wardens (Persuasion or Intimidation, Ordinary requests; Arguments: the neighbours' words or Kit's line). Help the Wardens by Convince, testifying before the street (Arguments: Nell's tip or the seizure notice), or by Exchange, holding the crowd, a service with no check. Or walk away. Defending settles A and puts Veinmarket and Harrow in the player's debt (`debt.veinmarket`, `debt.harrow`, `aftermath.harrow.defended`). Helping settles it as the Wardens' unattended outcome reads, the shop taken and sealed, and puts the Wardens in debt (`debt.wardens`). Walking away settles nothing, and A resolves unattended when its window closes at 20:00. A failed defence or threat teaches that the Wardens came for the cabinet (`poi.a.heard.cabinet`), a failed testimony is jeered, and the other ways stay open. Working readings, not owner decisions: the area brings the scene and `E  Talk` opens the node, where 11.1 has entering start it, so walking past never catches the player in a conversation; walking away leaves A open while its window lasts, so the player may come back; the requests and the Listener Values, the Wardens 1 and the neighbours 0, are authoring defaults, which at values 1 put the defence at 42 %, the threat at 28 % and the testimony at 59 %. Harrow still stands in his shop's interior while his scene is in the street; S2.7 settles the shop. Tests: EditMode `PoiAConversationTests` (the card against its conversation, and each option's outcome and residue on fixed dice), with `DialogueSessionTests`, `SliceConversationTests`, `SlicePeopleTests`, `LocalizationTests`, `TipBookTests` and `TownsfolkTests`, 49 of 49; PlayMode `PoiATests` (the scene stands only once the player comes within the window; A settled by Convince, and in a fresh world by Exchange), with `ConversationTests`, `TipTests`, `S1AcceptanceTests` and `TownsfolkTests`, 12 of 12; the Explicit `RenderTheArrest` draws the scene into `Logs/SliceDialogue/`.

**S2.6 done (2026-11-09).** 11.1's POI B card is the Decision Node asset `PoiBNode`, built the same way as A's. The scene stands at night in Loom Lane's middle near the POI's place, facing the coal stairs (POI C), placed once by `SlicePoiStages`. Two Veinmarket collectors drag a mill girl toward the stairs, a second girl has torn free, and an old woman watches from a doorway on the nearer side. `PoiStage` shows it only while B's window is open, 23:00 to 02:00. The conversation (`PoiBDialogue`, in both languages) opens with `E  Talk`. The collectors are Veinmarket's men, an enemy listener, so a check on them is a Final Check (10.4 §6). The ways through:

- Convince the lane's people to swarm the collectors: Persuasion, an Ordinary request. Arguments: the board's notice of missing girls, Nell's tip, or Kit's line about the coach-coats.
- Threaten the collectors: Intimidation, Final. Arguments: Kit's line, or what the old woman tells.
- Promise the old woman something for her tip: Exchange, a promise, since paid Exchange waits for S5a. It gives the stair as an Argument (`poi.b.heard.stair`, `promise.crone`) and settles nothing; the card marks such an option `prepares`, and `Problems` checks that it does not settle.
- Run for a Warden and fetch one, five minutes: by Convince (Arguments: the board or Nell), or by Exchange, a tip promised (`promise.wardens.tip`).
- Leave: B resolves unattended when its window closes at 02:00.

Protecting the girls settles B with `aftermath.loom_lane.girls_freed`, `against.veinmarket` and `poi.b.heard.harrow_buyers`. Fetching a Warden settles it with `aftermath.loom_lane.snatch_spoiled`, `aftermath.loom_lane.wardens_fetched` and `debt.wardens`. A failed threat is a lost Final Check: the collectors take the girl (`aftermath.loom_lane.girls_taken`, `poi.b.fled`), B settles, and the conversation ends as a fight. Before S3 no fight starts, by the S2 rule that a lost Final Check ends the scene as if the player fled. A failed swarm shuts the doors and a failed urge leaves the Warden shrugging, and the other ways stay open.

Working readings, not owner decisions:

- The Warden is reached inside the conversation, so the cast has no Warden.
- No coach stands in the lane, as carriages are on the wanted list (#22).
- Fetching puts the Wardens in debt and raises no Veinmarket enmity, as 11, which outranks the card, reads.
- Protecting raises `against.veinmarket`, and S6 reads Veinmarket's debt only without it.
- Follow the coach waits for S3.
- The requests and Listener Values (collectors, the lane and the old woman 0, the Warden 1) are authoring defaults. At values 1 they put the swarm at 59 % (73 % with two Arguments), the threat at 17 % (28 %), and the urge at 42 % (59 %).

Tests: EditMode `PoiBConversationTests` (the card against its conversation, and each option's outcome and residue on fixed dice), with `PoiAConversationTests`, `DialogueSessionTests`, `LocalizationTests` and `SliceConversationTests`, 48 of 48. PlayMode `PoiBTests` (the scene stands only once the player comes within the night window; B settled without a fight by Convince, and in a fresh world by Exchange), with `PoiATests`, `S1AcceptanceTests` and `TownsfolkTests`, 9 of 9; the Explicit `RenderTheSnatch` draws the scene into `Logs/SliceDialogue/`.

**S2.7 done (2026-11-09).** What the next day shows is placed once in Veilmouth and in Harrow & Tincture's interior by an editor command (`SliceResidue`). Each piece is a `WorldResidue`: objects it shows, objects it hides and doors it seals while any of its world-state conditions holds, from a given tick. The residue:

- When the Wardens seal the shop, by the player's help or unattended, a cord with a wax and a brass seal crosses Harrow & Tincture's street door at once. The door is `Sealed`: its prompt reads "Sealed with wax and brass" and Interact leads nowhere, though a player already inside still comes out.
- When the Wardens took Harrow, two Warden pairs stand on Bell Street (11.1's Tincture Row) from day 2, 08:00.
- When the collectors took the girls, Loom Lane's fear shows from day 2, 08:00: the two townswomen whose routes pass within 30 m of B's place stay home.
- Harrow is away from his shop's interior while A is open, and once he has been taken or has vanished.

Nell's day-2 lines are six bartender tips from 08:00 on day 2, one for each outcome of A and B: the shop open, sealed or emptied; the girls freed or taken, or the snatch spoiled. They stay Tips read from the world state, by 09's working reading for a line that must outlast the News Pool's 8 hours. Kit's day-2 lines, the melted seal or the glove, already followed the outcome.

The lie found out at the day boundary is Nell's. Her evening conversation gains a Deception option: Lottie Wren sent the character, because one of the Loom Lane girls is Lottie's cousin. Its Arguments are the Wren notice or the board's missing girls, and it is an Ordinary request, 42 % at values 1 and 59 % with both. It opens her Loom Lane tip as the other three ways do. An Ordinary lie comes out at midnight 20 % of the time (10.4 §5). `SocialStanding` then also writes `exposed.<person>`, and the next morning the player can ask Nell why she looks cold: she says she knows, once.

S2's acceptance is `S2AcceptanceTests` in PlayMode, three runs through the real scene and the conversations:

- A helped by Exchange; B settled by the swarm after a failed urge, which is not offered again.
- A defended by Convince; B settled by fetching a Warden by Exchange.
- B lost by a failed Final Check on the collectors.

The next morning shows the seal or none, the pairs or none, the fear, and Kit's seal line by what happened. The sealed door takes no one in, and the open one leads to Harrow. Nell's lines by outcome and the lie found out are covered in EditMode. With S2.5's and S2.6's tests, A and B are each settled by at least two Approaches without a fight. **S2 is built.** The owner accepted it on 2026-11-09 and authorised S3; its working readings stay the agent's readings until the owner changes them.

Two fixes on the way. Interact at Harrow's door both opened the door and opened A's conversation, since the Warden in the doorway stands within reach; a door in reach now takes Interact, as it already took the HUD's prompt. `DoorTests` now begins from a fresh world, since a world left at 16:00 stands A's scene before the door.

Working readings, not owner decisions:

- The seal shows as soon as the shop is sealed; the pairs and the fear show from day 2, 08:00.
- The pairs stand only when the Wardens took Harrow; a defended Harrow leaves Bell Street as it was.
- The fear is townswomen staying home. Shutters and chalk are not built, chalk being POI C's trace.
- Harrow is away while A is open and after it, so the shop stands empty while he is held. Pip opening the shop waits for S5a.
- Nell's day-2 lines start at 08:00 rather than in the evening, as they are talk, not leads.
- The lie is Nell's fourth way to the Loom Lane tip, and she finds out through `exposed.nell`.
- The seal at 1.3 m, the pairs 3.5 m out from the fronts at −7 m and +9 m along the street, and the fear within 30 m are authoring defaults.

Tests: EditMode `ResidueTalkTests` (Nell's six lines by outcome and none the day it happened, Kit's seal line, a lie found out and one that stays hidden), with `DialogueSessionTests`, `LocalizationTests`, `PoiAConversationTests`, `PoiBConversationTests`, `SliceConversationTests`, `SocialStandingTests` and `TipBookTests`, 92 of 92. PlayMode `S2AcceptanceTests`, with `PoiATests`, `PoiBTests`, `ConversationTests`, `S1AcceptanceTests`, `TownsfolkTests` and `DoorTests`, 29 of 29 after the `DoorTests` fix.

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

Accepted when: a wolf pack warns, attacks, and breaks when one wolf falls; a fight in a street uses cover; Break Away and a Yield to the Wardens both end a fight as written; an NPC notices or misses a character in Stealth Mode; and the C# fight rules, run headless over the lines of `beast_sim.py`, land within 3 points of its win rates. The owner split this acceptance between S3a and S3b (below).

**S3 iterations**, proposed by the agent on 2026-11-09, after the owner accepted S2 and authorised S3 ("Принять, начать S3"). Each is one micro-plan of at most about 500 lines, like S2's. The owner split S3 in two the same day: **S3a, the fight**, and **S3b, stealth, beasts and the POIs**, each with its own acceptance, so the fight can be played and accepted before stealth and beasts are built. The code lives in `Scripts/Confront/` (17 C.1, C.2, C.5): the rules in `Confront/Rules/`, the grid in `Confront/Grid/`, the turns in `Confront/Turns/`; stealth in `Confront/Stealth/` (17 Wave C0.3a); beasts in `Slice/Beasts/`.

Before S3a.1 the owner asked for the city's greenery pass (2026-11-09): city trees and bushes, wider pavements with street trees, hedges and grass, and Roadside Tales outside the walls, by [VEILMOUTH_CITY_PLAN.md](VEILMOUTH_CITY_PLAN.md) §25. It is a dressing pass of the scene, not a milestone of the slice.

#### S3a — The fight

| Iteration | Contents | Done when |
|---|---|---|
| S3a.1 Fight rules | The numbers of 10.2 §5b as one service, `FightRules`: a fighter's values from its character sheet and gear (HP by 07.2, AP `10 + Dexterity / 20` with its fraction carried, Initiative, melee and ranged accuracy, Evasion); hit chance with Luck, held between 5 % and 95 %; damage with the Stage Factor and ±15 %; reduction `defence / (defence + 50)` up to 75 %; physical defence doubled against a ranged weapon beyond the next cell; critical hits ×2 with the excess over 95 %, a critical melee blow halving physical defence; blocks and parries by item; stun and cleave; Load in a Fight and Attribute Requirements. Weapons, armour, and the beasts' bites, tusks and hides are data assets naming 10.2's sections, and rolls fork `slice.combat` with the fight's number | EditMode tests against 10.2's numbers: peers hit about 71 %, plate stops 32 % of a blow and 49 % of a bullet, the weapon table's traits |
| S3a.2 Parity | A headless duel in the model of `combat_sim.py` and `beast_sim.py`, with no space: turns, reactions from the AP left, Bleeding from a beast's critical hit, a pack breaking when its first wolf falls | An EditMode test runs the table of 10.2 *Beasts of the slice*, 4,000 fights a line, and lands within 3 points of every cell |
| S3a.3 Combat Grid | `CombatGridReader` moves into `GameDev.Confront.Grid` behind `ICombatGrid`: no allocation on a step (gate G2), characters as their own occupancy layer, cover on each side of a cell, the 10.1 tags read from the kit's `KitEnvironmentAction`, a path search with diagonals and no corner-cutting, the window following the player. The lab stays a tool | EditMode and PlayMode tests on Veilmouth's streets: walls close edges, a fence is Half Cover, a doorway passes one cell, a step allocates nothing |
| S3a.4 Encounter and rounds | `IEncounterLock` and `ITurnEconomy` (17 C.1, C.5): hostility stops the world, the grid appears within half a second with a short sound, Initiative is rolled; each fighter's turn spends AP on moving and attacking, what is left stays for reactions; one world tick after each complete round; a fighter who leaves the grid drops out, one who enters joins next round; the fight ends when no hostile side is left in it, and free movement returns. A development command starts a fight | A PlayMode test: a fight starts in a street, every round costs one tick, and the fight ends |
| S3a.5 The player's turn | The fight camera of the grid lab (it turns, and walls in the way become outlines, 16), the cursor on cells (Basic RPG Cursors), the path and its AP before the click, the Initiative strip, HP and AP; an enemy clicked is attacked. The chance and the damage show from Intelligence 10, the modifiers from 20, as 10.4 §3; an enemy's condition shows as a word. Moves, blows, hits and deaths use the clips of the asset map §2. Every word is a string table key | A PlayMode test plays a turn by the cursor: a path, an attack, the end of the turn |
| S3a.6 Position and reactions | The Reaction Attack, Surrounded, cover against ranged attacks with a line of sight, higher ground, the Point-Blank Shot, blocks and parries from the AP left, Wait, Defensive Stance and Counterattack, two Weapon Sets (10.2 §5b *Position*, *Wait and Defensive Stance*) | EditMode tests on a small grid for each rule |
| S3a.7 Environment and conditions | The Environment Actions of 10.1 (vault, jump, climb, window, mantle, breach, open) with their bands and failures, falls of 10 % of HP for each 2 m and a broken leg from 4 m; Bleeding, Poison and a broken leg on world ticks, in a fight and out of it (P.4); a bandage, an antidote and a splint as actions with their First Aid checks | EditMode tests on the bands, a fall and each condition; a PlayMode vault in a street |
| S3a.8 Fight AI | Target Rules V0 (10.2 §5a): Target Impact, the multipliers for walking, a kill and three allies beside, a melee fighter striking the enemy beside it when it cannot reach its target, a shooter's best target in range and its step back, Waiting, Hold Back, changing Weapon Sets; an NPC faces the way it last moved or struck, for the Vision Cones | EditMode tests on small grids: each rule chooses as 10.2 says |
| S3a.9 Leaving a fight | Break Away: 10 cells from every enemy ends the fight for that fighter, the Reaction Attacks on the way included. A Yield to people, never to beasts or the cult: 2 AP, a short dialogue, and the terms of 10.2 §9, the Wardens' lock-up until 08:00 or the invasion, Veinmarket's take. An NPC asks for mercy when its side is losing, and the player spares it for a term or does not (08 §7). Death at 0 HP ends the run. What a fight leaves (08 §5): the Deeds Record's kills (S2.2), bodies that stay until the player is away, world-state keys of who fought whom, and a fight against the Wardens cancelling their debt | PlayMode tests: Break Away ends a fight; a Yield to the Wardens ends one in the lock-up |
| S3a.10 Acceptance | S3a's acceptance below as PlayMode tests in Veilmouth, started by the development command, with S3a.2's parity | S3a's acceptance |

S3a is accepted when: the C# fight rules, run headless over the lines of `beast_sim.py`, land within 3 points of its win rates; a fight in a street uses cover; and Break Away and a Yield to the Wardens both end a fight as written.

**S3a.1 done (2026-11-09).** The fight's numbers are one service, `FightRules` (assembly `GameDev.Confront`, `Scripts/Confront/Rules/`), with every number of 10.2 §5b in `Assets/Data/Confront/FightRules.asset` and the critical curve read from 07.1's `ActionCheckRules`. A `Fighter` is a character sheet, a Limit, the Weapon Set in hand and a second one, armour, and item modifiers. Building one refuses a shield beside anything but a sword, an axe or a mace, a firearm without its Cartridge Pouch, and a pouch without a firearm. One strike rolls, in this order, the hit, the critical hit, the damage, the target's parry or block from the AP it holds, a stun if the target still stands, and a glowing bite's poison. It reports Bleeding and poison for S3a.7 to apply. The data assets in `Assets/Data/Confront/` each name their sections of 10.2:

- twelve weapons: the seven melee weapons, the pistol and the rifle, a wolf's bite, a glowing wolf's bite, and a boar's tusks;
- the shield and the Cartridge Pouch;
- six armours: cloth, leather, chain, plate, hide, and thick hide.

`CharacterSheet` gains Melee Skill and Ranged Skill at the end of its list, so dialogue assets keep their skill numbers. The rolls fork the existing `slice.combat` stream with the fight's number, in place of the `slice.fight.*` this row first named.

Working readings, not owner decisions:

- A fighter that cannot evade cannot parry or block either, as a stunned one cannot.
- 10.2 gives the poison to a glowing beast's bite, so a glowing boar's tusks do not poison.
- Bleeding is reported only when the target still stands.
- The Cartridge Pouch weighs nothing, as in `build_sim.py`.
- Infusion, Will Power and magical damage are not in S3a.1's row and join with the first fighter that infuses; armour keeps its magical defence for them.

For S3a.2: `beast_sim.py` strikes through `combat_sim.strike`, which has neither the critical pierce nor the weapon's aim, while `FightRules` follows 10.2 §5b, so a beast's critical bite does a little more in C#. S3a.2 measures that against its 3-point gate.

Tests: EditMode `FightRulesTests`, 29 of 29, with the sheet's and the dialogue's tests, 77 of 77:

- Peers hit 70.7 %, and the reference Limit 9 pair 71.5 %.
- Plate stops 32 % of a blow and 49 % of a bullet, leather 14 % and 24 %, and a point-blank shot meets plain defence.
- The weapon table's values and traits hold.
- Blocks, parries, a stun, a cleave, Bleeding, and poison work on fixed rolls.
- Attribute Requirements, Load in a Fight's bands on 10.2's own examples, AP with its fraction, and Initiative are checked.
- HP of the reference fighters and the four beasts is checked.

**S3a.2 done (2026-11-09).** `HeadlessFight` (`Confront/Rules/`) is a fight with no space in the model of `combat_sim.py` and `beast_sim.py`, on S3a.1's rules:

- Initiative sets the order once, a tie going to the higher Dexterity, then at random.
- In its turn a fighter strikes while its AP last. A melee fighter spends 6 AP of its first turn closing in, and what is left stays for parries and blocks.
- With three or more standing on its side, its target counts as Surrounded by two; with two, at even odds.
- The character's side strikes the weakest enemy standing, the other side at random, until Target Rules V0 (S3a.8).
- After each round, a world tick, Bleeding takes 1 % of maximum HP and ends at Endurance / 2 %.
- A pack breaks at the end of the round in which its first wolf fell. A fight undecided after 60 rounds is a draw.

`FightRules` gains the Surrounded bonuses of *Position*, the Bleeding numbers of 10.2 *First Aid and conditions in the slice*, and the headless fight's 6 AP and 60 rounds. A beast's critical hit bleeds only when it deals damage, so a parried bite draws none.

The first comparison found `beast_sim.py` off its own rules: it struck through `combat_sim.strike`, which has no critical pierce, and the pierce alone moved two cells by 3.5–4.5 points, past the gate. `beast_sim.py` now strikes as 10.2 §5b reads and breaks Initiative ties as 10.2 says. It was re-run at 40,000 fights a line, so the table carries little noise of its own. 10.2's table is replaced with no rule changed, and the old table is kept beside it. The largest change is the Limit 9 hero with the city set, 4 points lower against three wolves and a glowing wolf.

Working readings, not owner decisions:

- The club of `beast_sim.py`'s new character lives only in the test, since 10.2 has no club among its iron weapons.
- 6 AP to close in and 60 rounds are `combat_sim.py`'s, for a fight with no space only.
- Bleeding lives in the headless fight until S3a.7's conditions.

Tests: EditMode `HeadlessFightTests` and `FightRulesTests`. The beast table, 4,000 fights a cell in C#:

```
Two wolves  Three wolves  Boar         Glowing wolf  Glowing boar
New character, club, no armour    10.7 (-0.4)   0.4 (-0.6)   6.0 (+0.0)   0.9 (-0.1)   0.4 (+0.4)
New character, sword, leather     65.8 (-0.2)  24.3 (+1.3)  65.4 (+0.4)  53.4 (-1.7)  26.3 (-0.8)
Limit 9 hero, sword, cloth        75.3 (+0.3)  31.0 (+1.0)  70.3 (+0.3)  66.0 (+0.0)  35.0 (+0.0)
Limit 9 hero, city set            93.7 (-0.3)  62.3 (-0.7)  88.8 (-1.2)  84.6 (+0.6)  59.1 (+0.1)
Limit 8 hero, leather             98.3 (+0.3)  80.8 (-1.2)  96.9 (-0.1)  94.4 (+0.4)  79.9 (-0.2)
worst 1.7 points against the gate of 3; 35 of 35 tests with FightRulesTests
```

**S3a.3 done (2026-11-09).** `CombatGridReader` lives in `GameDev.Confront.Grid` (`Confront/Grid/`) behind `ICombatGrid`. S0's readings of the place are unchanged:

- **The window follows the player.** The window is a ring of 43 × 43 columns. `Follow` moves it when the player crosses into another cell, reads only the columns that come into reach, and joins the reachable cells again. A step allocates nothing.
- **Characters.** Characters are a layer of their own, one to a cell (`Occupy`, `Vacate`, `OccupantAt`). The place's readings never see them. A path goes round an occupied cell and never into it, as 10.1 *Movement on Grid* says.
- **Cover** is read on each side of a cell: Half from the low strip at 0.7 m, Full from the high one at 1.5 m.
- **Environment Action tags.** The 10.1 tags come from the `KitEnvironmentAction` of the block in the way. Their two enums and `IEnvironmentAction` moved to `GameDev.Confront.Grid`, so the grid does not depend on the slice. No block in Veilmouth carries a tag yet; S3a.7 places them.
- **Paths.** `FindPath` is an A* search with diagonals at 1 AP and no corner-cutting. Of equally short paths it takes the one with the fewest diagonals.
- **Tools.** The grid lab, `CombatGridProbe` and S0's `VeilmouthGridCheck` stay tools on the same reader. The readings they share with the tests are in `SliceGridReadings` (`Slice/Editor/`).

Tests, EditMode and PlayMode, all passing:

- **`CombatGridRulesTests`, 6 of 6**, on a built floor of 20 × 20 cells:
  - a pillar closes its edge and both diagonals past its corner;
  - a diagonal costs 1;
  - a fence 1 m high is Half Cover and a wall 3 m high Full;
  - the tag of the block in the way, on the wrapper or on the block itself;
  - occupants as a layer of their own;
  - a window moved cell by cell reads the same as one built where it stopped.
- **`CombatGridVeilmouthTests`, in Veilmouth's own scenes:**
  - From the street before 90 dwelling doors, the grid reaches no cell behind the front.
  - Railings and fences: 1,160 sides, 96.6 % of them Half Cover and 40 Full. All 580 crossings are closed.
  - The 17 doorways between rooms in the seven interiors each let their one cell through. The 8 doorways in an outer wall, with no floor beyond, are counted apart, since a portal takes the character through those.
  - Over 8 steps of the window, no step allocates. The median step takes 0.66 ms and the worst 1.14 ms, against G2's 8 ms.
- **`CombatGridScaleTests`:** the reader's capsule and sea level equal `WorldScale`'s and the scene's.
- **PlayMode `CombatGridPlayTests`:** the player walks five cells down a Veilmouth street and back, twice. The grid's cell is the one under the player at every step, and once the walk has been made, no step allocates.

What the tests found in S0's readings, both fixed in `VeilmouthGridCheck` too:

- **Doorways.** These readings went stale with the Victorian Interiors rebuild of 2026-11-07. They looked for BK's names, found no doorway, and passed on nothing. They now read VI's paper, dark and stone doorways, and count a doorway in an outer wall apart.
- **House fronts.** In C3 a door's threshold stands 0.6–1.65 m above the street. The old reading, taken at the street's height, measured only 8 of 90 doors. The reading is now whether a grid built on the street reaches a cell behind the front.

For S3a.6: a cover strip runs across the whole width of its cell. A wall standing on a grid line therefore also reads as cover on the two sides beside it, which is a likely source of the 40 Full readings at railings. S3a.6 should shorten the strips, or read cover from the cell's middle, before a fight relies on cover.

Working readings, not owner decisions:

- `Follow` picks the standable surface within 2 m of the player's feet (authoring default).
- `GameDev.Confront` cannot see `WorldScale`, so it carries its own copy of the capsule and the sea level, and a test keeps the two equal.
- A block's tag is read on demand, by a capsule cast along the closed edge, and is not stored for each edge.
- "A doorway passes one cell" holds as S0's rule: one cell over a cell's middle, which is where every VI doorway stands; two over a node; none with a door leaf.

**S3a.4 done (2026-11-09).** The Encounter Lock and the turns live in `GameDev.Confront`: `IEncounterLock` and `EncounterLock` in `Confront/`, `ITurnEconomy` and `TurnEconomy` in `Confront/Turns/` (17 C.1, C.5). The slice's host is `SliceConfront` in `Slice/Fights/`, which the world driver makes when the slice's settings carry `FightRules`. `SliceConfrontSetup` wires those settings.

- **Start.** Hostility holds the clock, so the world stops. The grid forms round the player and is drawn as a thin outline in every reachable cell, with a short struck chord. Initiative is rolled by 10.2 §5b.
- **Turns.** Each turn spends AP on steps, 1 AP a cell, and on blows at the weapon's AP. What is left stays for reactions, and the old remainder expires when the next turn begins. A melee blow goes across a cell's side and never through a wall; a shot reaches within the weapon's range.
- **Rounds.** One world tick passes after each round, the clock moving by whole ticks while it is held.
- **Joining and leaving.** A fighter who comes within the grid joins at the next round. One whose cell falls outside the grid as it follows the player drops out at once.
- **End.** The fight ends when no two sides stand in it. The clock runs again, free movement returns, and the grid goes.
- **The development command.** F7 gives the player's character the development kit for the fight: a sword, leather, and the rifle with its Cartridge Pouch as the second Weapon Set. It places two opponents, red Human Character Dummies with daggers and every value at 1, four cells away, and starts a fight. Until S3a.5 brings the cursor, the player's character fights by the same rule as everyone else: walk toward the nearest enemy and strike while AP last. A fallen body lies down until S3a.5's death clips, and the development opponents are removed when the fight ends.

Tests: PlayMode **`EncounterPlayTests`** passes. F7 in a Veilmouth street starts a fight of three combatants on a grid of 1,284 cells within half a second, with the clock held and free movement stopped. The fight lasted 3 rounds and moved the clock by exactly 3 ticks, then the world ran again with the grid gone.

Working readings, not owner decisions:

- Every two sides are hostile, until factions' standing decides it.
- The round a fight ends in is its last, and it costs its tick like any other.
- A combatant whose own cell is taken goes to the nearest free cell within two cells, and one with none stays out.
- The grid's sound is a low chord made in code, since the asset map has no such sound. It stays until the owner picks one.
- In the test the two daggers beat the character, every value at 1, in three rounds. The development fight tests the turns, not the balance, and the character's death waits for S3a.9.
- The townsfolk stand-ins keep walking during a fight, as they do during a conversation, until S5c's schedules run on the clock.

**S3a.5 done (2026-11-11, on the owner's grant of autonomous work).** The player's turn is played by the cursor. The rules it needs are in `GameDev.Confront`: `ITurnEconomy.PathToStrike` gives the walk to a cell from which the weapon reaches a target, `ContextFor` what the place adds to a blow from a cell, and `Walked` tells a walk cell by cell. `FightRules.Forecast` gives a blow's chance and an ordinary hit's damage through the armour. The scene's side is in `Slice/Fights/`:

- **The fight camera** (`FightCamera`) is the grid lab's: high over the player at 50°, 13 m off, turned by Q and E or by dragging with the right button, nearer or farther by the wheel. A house between it and the player stops drawing but keeps its shadow, and the edges of its colliders show as pale lines (16). The free camera takes over again when the fight ends.
- **The cursor** (`FightCursor`) is Basic RPG Cursors'. Over a cell it draws the path, a gold mark in each cell and the end cell framed, red past the AP, and the tip says the AP. Over an enemy it draws the walk to it and says the walk's and the blow's AP together; a click walks there and strikes. Space or Enter ends the turn, and a turn ends by itself when its AP are spent. The pack's shot cursor is a crossbow, which the setting has not, so a shot will show the sword too.
- **The HUD** (`FightHud`, in the HUD's own document) shows the order of Initiative across the top, the acting fighter lit, the player's HP as a number and each enemy's condition as a word. The player's HP and AP are at the bottom, the AP as pips, those a pointed action would spend marked. The tip by the cursor says the enemy's condition and the blow's AP. From Intelligence 10 it also says the chance and the damage, and from 20 the parts of the chance: the base, accuracy, Evasion, position, Luck, and the weapon's fit (10.4 §3, `SocialCheckRules`' thresholds). The last blow is said under the order. Every word is a key in `ui.csv`.
- **The beats** (`FightBeats`): the turns resolve at once and the bodies catch up. A walk goes cell by cell at 2 m/s in the walk clip, the player's locomotion at a walk or a person's own walk, and back into the guard. A blow turns the attacker and plays its clip; at 0.35 s the target plays its hit or its death, and the blow holds 0.65 s more. The next turn waits for the last beat, and when a fight is decided its last blow plays out before free movement returns. The order and the bar wait for the beats too, so they never tell a blow before it is seen.

Tests: EditMode **`PlayerTurnRulesTests`** passes: over 4,000 blows every ordinary hit's damage lies within the forecast and hits come as often as it says; the walk to a blow ends beside the target across a side and is told cell by cell. PlayMode **`FightTurnPlayTests`** passes: F7 in a Veilmouth street, the fight camera and the HUD take over, the order shows three; a cell beside the player pointed at shows 1 AP before the click, and the click walks; the nearest enemy pointed at shows the blow's AP, the chance only from Intelligence 10 and its parts only from 20, and the click strikes it; Space ends the turn, and when the fight ends the free camera, the plain cursor and the HUD come back. **`EncounterPlayTests`** passes with the player's character fighting by the rule (`AutoPlayer`), every blow seen to land.

Working readings, not owner decisions:

- Condition words, by the share of maximum HP: unhurt at full, scratched from three quarters, wounded from a half, badly wounded from a quarter, near death below it, out of the fight at 0. In Russian they are nouns (Без ран, Царапины, Раны, Тяжёлые раны, При смерти, Вне боя), so they need no gender.
- An enemy is pointed at when the cursor is within 26 pixels of its body's line, feet to head, or over its cell.
- A turn ends by itself at 0 AP; with AP left it waits for Space, since what is left stays for reactions.
- Moves and blows are shown, not cut: a fight of three takes some seconds more than S3a.4's.
- The camera's numbers, the beats' times and the colours are authoring defaults.

**The fight's look (2026-11-11).** The explicit PlayMode `RenderTheFightView` draws the fight as the player sees it, the camera's frame with the HUD over it, at noon and at 19:00, into `Logs/SliceFight/`. Its first pictures showed two faults, both fixed:

- **Trees hid the fight.** Seen from above, the street trees' crowns covered the fighters and the cells. The fight camera now also hides every tree on the line to the player, to each fighter standing and to the cell under the cursor, its shadow kept, as it hides a house; a tree gets no outline.
- **The grid drowned the street.** Opaque pale lines over 41 by 41 cells glared, worst at dusk. The lines are now half see-through, fade from 5 m round the player to nothing at 14 m, and dim with the main light, by the slice's own unlit shader `GameDev/Combat Grid Line` (`Assets/Shaders/Slice/`). The grid draws with its own copy of the material, since the GPU Resident Drawer draws a renderer without its property block.

The order's "Your turn" and the last blow moved below the chips, with a soft shadow in place of the outline. The fade's radii, the line's alpha 0.5 and its strength 0.45 under no light are authoring defaults.

**S3a.6 done (2026-11-11, on the owner's grant of autonomous work).** Position and reactions follow 10.2 §5b as `tactical_sim.py` plays them. The rules are in `TurnEconomy` and `FightRules`, and the grid's part is in `CombatGridReader`. "Beside" means across a cell's side with no wall between.

- **Reaction Attack.** Leaving a cell draws one free blow from each melee enemy beside it that can act, even when the next cell is beside that enemy too. Each enemy strikes once a round, and its turn resets this. The walker parries or blocks from the AP it has left. A walker who falls stops, and its turn ends. A walk is told up to the blow, so the blow shows where it lands.
- **Surrounded.** Two enemies able to act at a target's sides give +10, three or more +20. The attacker counts from the cell it strikes from.
- **Line of sight and cover.** A shot needs a clear line between the two cells at chest height. The walk to a shot ends at the first cell with the target in range and in sight. Cover counts against ranged attacks beyond the next cell only: Half −15, Full −30, on the sides of the target's cell that face the shooter.
- **Higher ground.** +10 to hit and +10 critical chance from 1.5 m above the target.
- **Point-Blank Shot.** A firearm aims 10 worse while an enemy stands at a side of the shooter's cell.
- **Wait (R).** Once a round. Those who waited act after everyone, lowest Initiative first. A waiter's AP wait in the pool and serve its reactions, and it acts with what is left.
- **Defensive Stance (G).** It ends the turn and is refused after an attack. Each AP held adds +1.5 Evasion and raises the guard chance by 3 %, multiplied. It answers a melee attack by a melee attacker, hit or miss, with a Counterattack for the weapon's AP plus 2, which is not itself answered. It clears at the fighter's next turn.
- **Weapon Sets (X).** A change costs 2 AP.

The HUD's tip warns when a walk will draw a Reaction Attack. The order marks a fighter in a stance or waiting, and the log says when a blow was guarded. The keys are on the fight's hint line, and every word is a key in `ui.csv`.

The cover strip that S3a.3 left for this step is now 0.6 m wide across the middle of each side. A wall on a grid line no longer reads on the two sides it meets at the corners.

Tests:

- EditMode **`PositionRulesTests`** passes, ten tests on a 20 × 20 floor, one for each rule: Surrounded with a stunned enemy and a corner one left out; one Reaction Attack, from the dagger and never the rifle, and none again that round; a wall blocking a shot, and the walk to the first cell in sight; a fence at −15 and a wall at −30, and nothing from the other side or against a blow; higher ground from 2 m and none from 1 m; the Point-Blank Shot; parries spending 2 AP each from the pool; the order of those who waited; the stance's Evasion, guard and Counterattack, no answer to a shot, and no stance after an attack; and the Weapon Set change.
- **`CombatGridRulesTests`** adds a wall on a grid line that covers only its own side.
- With **`PlayerTurnRulesTests`**, **`FightRulesTests`** and **`HeadlessFightTests`**, 54 of 54 pass.
- **`CombatGridVeilmouthTests`** passes 10 of 10 with the narrower strip.
- PlayMode **`FightTurnPlayTests`** and **`EncounterPlayTests`** pass. A step pointed at beside an enemy warns of the Reaction Attack exactly when the walk draws one, and the AP left count any parries.

Working readings, not owner decisions:

- **Higher ground** is a rise of 1.5 m or more. This authoring default stands until the elevation bands of 10.2 §7 are set.
- **Line of sight** is a clear line between the two cells' middles at 1.4 m, through the place only. Characters never block a shot (10.2 §7 leaves line of sight open).
- **Cover facing.** A side of the target's cell faces the shooter when the shooter stands at least half as far along that side's axis as across it. The best facing side counts.
- **Reactions to a walker.** The walker parries a Reaction Attack from the AP it has left in its turn. A stun from one takes its next turn.
- **Stun.** A stunned fighter draws no Reaction Attack, counts toward no Surrounded, and makes no Counterattack.
- **The Russian word** for the Reaction Attack in the UI is «удар вдогонку».
- **Keys.** R, G and X are authoring defaults until a settings page exists.

#### S3b — Stealth, beasts, the POIs

| Iteration | Contents | Done when |
|---|---|---|
| S3b.0 The land | The land outside the walls, one pass by the owner's fifth play test ([VEILMOUTH_CITY_PLAN.md](VEILMOUTH_CITY_PLAN.md) §26.2): outer roads of 5 m with fenced entrances and signposts; grass by the terrain's detail layer on every green surface, fading by the roads; the forest's edge about 15 m from the walls, denser with undergrowth that is walked through, trunks at least 3 m apart; paths of 2 m to the glades, the ore spots and the hunting grounds; the West road's farms as a hamlet with a fenced pasture and cows; rocks at the water, on the hill and in the forest; a snowy mountain backdrop out of reach. The pieces the project lacks (Rock and Boulders 2, a signpost, a snow texture) and the cows wait for the owner (99 VS-LAND) | The owner walks the land; the Combat Grid reads the forest as 10.1 sets it, trunks Full Cover and undergrowth nothing; the kit check is clean |
| S3b.1 Stealth | Stealth Mode (a key, three quarters of walking speed; 3 AP to enter in a fight and 3 AP for two cells), Vision Cones of 10 cells, the Notice Meter of 10.3 §3 with Darkness, Alert, losing sight and going to look, the NPC's line when it notices; the meter shown from Perception 4 and the cones from Intelligence 3 and 6; the stealth attack before Initiative with +50 points of critical chance, a dagger +1.0 | EditMode tests on the meter's times in 10.3's table; a PlayMode test: a guard notices a character on a lit street and misses one behind its back at night |
| S3b.2 Beasts | Content piece C3 first (§5): the six wolf and four boar territories of the city plan's stand-ins, and what each beast leaves. The beasts placed once by an editor command; 2–3 of them glow, chosen by the seed (`slice.beasts.glow`), each alone. A beast notices by the meter with its own Perception, warns twice, a tick each, attacks within 3 cells or when attacked, and chases no further than its territory; a pack breaks when its first wolf falls and does not come back; nothing returns during the run | A PlayMode test: a pack warns, attacks, and breaks |
| S3b.3 Confront and Veil in POI A and B | The options S2 hid open: at A, freeing Harrow by force (a fight with the Wardens) and listening from Stealth Mode to the third Warden; at B, a lost Final Check now starts the real fight with the collectors, Confront is chosen outright, and the collectors can be followed from Stealth Mode. Outcomes and residue as 11.1's cards read; S2's rule that a lost Final Check ends the scene as if the character fled is retired | EditMode tests on the cards; a PlayMode test: B's collectors fought in Loom Lane |
| S3b.4 Acceptance | S3b's acceptance below as PlayMode tests in Veilmouth and the forest | S3b's acceptance |

S3b is accepted when: a wolf pack warns, attacks, and breaks when one wolf falls; and an NPC notices or misses a character in Stealth Mode.

Authoring defaults proposed with them:

- The escape distance of 18, about 10 cells (its open question 15), is measured as moves are, a diagonal step counting one cell, from the nearest enemy at the end of a step.
- A line of sight runs between the chest heights of two cells' centres and is closed by structure. Higher ground is a cell at least 1 m above the target's.
- An NPC faces the way it last moved or struck (10.2 §11 question 7, for the cones only).
- Until the World Inhabitants slider exists, a cone's angle is 95°, the middle of 60–130°.
- Darkness (10.3 §3): +4 outdoors at night farther than 6 m from a lit lamp, +2 in shadow or by a dim lamp, +6 in an unlit room, otherwise 0.
- An NPC of people asks for mercy below a quarter of its HP when its side has fewer fighters standing than the other; the cult and beasts never do.
- Until S5a's money, a fine or a take is kept as a debt in the world state, and S5a's purse settles it.
- Death before S5c's save restarts the scene at the slice's start, with a plain screen that says the character died.
- The character's attributes stay at 1 until S5b, as in S2; a development command sets them and gives gear for tests.

The owner's answers (2026-11-09):

- S3 is split into S3a and S3b, as above.
- People carry firearms before S6: one Warden in three has a pistol, so does one of the collectors, and so does one of each Warden patrol pair, so cover counts in a street fight from S3a. The pistol goes with its Cartridge Pouch (10.2 §5b), so these fighters carry a melee weapon as their second Weapon Set. Their values come with content piece C3.
- The beasts are the owner's new packs, Wolf and Wild Boar, in place of the scaled-down dragons ([SLICE_ASSET_MAP.md](SLICE_ASSET_MAP.md) §6).
- Until S5a's shops the gear for S3's fights comes from a development kit on a key: a sword, leather, and a rifle for tests and the owner's play tests. The character's ordinary start stays as 11 sets it, with no weapon.

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
| Gathering and Skinning | W.9, P.3 | Nodes gathered round the glades and the ore spots that S3b.0's paths reach (city plan §26.2, 12c); cycles until cancelled or empty, Stamina, the Skinning lesson ([19](../../../19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) §6.4) |
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
| C1 People | S1 | 6–10 named NPCs and the Masters: role, place, schedule, what they know, what tips they give. **Accepted 2026-11-07** in [SLICE_PEOPLE.md](SLICE_PEOPLE.md) (not the city plan C1) |
| C2 POI A and B | S2 | Full Decision Node cards, dialogue, the Arguments each needs |
| C3 Beasts and enemies | S3 | Where each territory lies, as the hunting grounds of S3b.0 (city plan §26.2, 12d), which beasts glow, what they leave |
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
- The News Pool (owner, 2026-11-09; [09](../../../09%20Information%20Sources.md) § News Pool) has no milestone yet: NEWS-5 recommends S5c, with POI A's and B's events as its first items.
- Whether the full game later reuses the slice's city as a hand-built landmark on the seeded Overworld, or rebuilds it.

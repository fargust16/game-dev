# Vertical Slice — Asset Map

Status: Draft — the owner's list of packs and answers, 2026-11-05; checked against the project on disk and the design documents by the agent
Created: 2026-11-05
Last Updated: 2026-11-05 (the owner's play test: BK_AlchemistHouse for every interior, Polytope Studio's green trees and grass as common cover, the Dummies' and dragons' render and animator problems found and handled; §9 rows 16–17 resolved. Earlier: the agent's dressing pass: the owner's bar counter, cabinet, cell bars, and cell door in; the magic circle made; three pack problems handled; §9 updated. Earlier: the magic circle's rules are in 21 §4. Earlier: the owner's models through the Tripo Bridge: pistol, rifle, iron ore, Notice Board, bed; Stylized Trees Pack for trees and bushes, its editor script fixed; Modular Fantasy Bridges; the factory yard dropped; render state recounted after the owner's conversion. Earlier: render state recounted after the owner's URP imports; the pouch came through the Tripo Bridge and the pistols failed as ZIP; §9 lists the models the slice still needs. Earlier: Basic Treasure Coins found in REAL_DEDICATED; the dragons are FourEvilDragonsHP; the firearms come through the Tripo Bridge. Earlier: the owner's second list: Multistory Dungeons 2 for alchemy, Victorian Objects weapons, Stylized 3D Tools, RPG_Animations_Pack_FREE, Roadside Tales Free, Modern RPG icons, Yughues pavements, WaterWorks; the owner's answers on shields, hidden followers, colours, coins, and buildings)
Authority: [11 Vertical Slice](../../../11%20Vertical%20Slice.md), [VERTICAL_SLICE_IMPLEMENTATION_PLAN.md](VERTICAL_SLICE_IMPLEMENTATION_PLAN.md), [SLICE_MODULAR_KIT_STANDARD.md](SLICE_MODULAR_KIT_STANDARD.md)
Related: [ASSET_LIBRARY_REFERENCE.md](ASSET_LIBRARY_REFERENCE.md) (the Overworld's registry)

---

## Purpose

For each mechanic, character, and place of the slice, this document names the asset pack it comes from and the files inside it. An agent wiring a model, clip, sound, or effect into the slice takes it from here. A pack that is not listed here needs the owner's word first.

Owner's decision, 2026-11-05: the slice uses these packs. The medieval packs are **temporary**: they stand in for the 18th–19th-century industrial city of [11](../../../11%20Vertical%20Slice.md) (*Era and Mood*), whose rule still binds final art.

## Rules

- **Wrapped, not used raw.** A vendor prefab stays in its vendor folder; the slice uses a project prefab that wraps it with the colliders, layer, and 1 m placement of the kit standard (§4, §5, §8).
- **URP first.** A material on the Built-in pipeline renders magenta here. Before use, import the pack's own URP package where it ships one, or else convert with *Window → Rendering → Render Pipeline Converter*. §7 lists each pack's state.
- **Characters are colour-coded stand-ins.** The colours of §1 help reading and testing; they are not final art.
- Every row was checked on disk on 2026-11-05. *Not on disk* means: do not write code against it until it is imported.

---

## 1. Characters

All people use **Human Character Dummy** (Kevin Iglesias), `Assets/Kevin Iglesias/Human Character Dummy/Prefabs/HumanDummy_M <Colour>.prefab` and `HumanDummy_F <Colour>.prefab`. The pack ships White, Blue, Green, Orange, Purple, Red, and Yellow. A colour it does not ship is a project material variant of `Materials/HumanDummy.mat`.

| Who | Colour | Source | Note |
|---|---|---|---|
| The player | grey | project variant | The slice player is White today (S0.3) |
| Wardens, the city's police | blue | `HumanDummy_* Blue` | |
| Cult Forces that act openly | red | `HumanDummy_* Red` | A hidden follower keeps the colour of their everyday role until revealed (owner, 2026-11-05), so the colour never gives away the traces of a Ritual Weak Point |
| Masters of the professions | green | `HumanDummy_* Green` | Item Creation, Enchanting, First Aid; Harrow, who teaches Alchemy |
| Ordinary townsfolk | yellow | `HumanDummy_* Yellow` | |
| Cult Generals | pink | project variant | |
| Sellers | orange | `HumanDummy_* Orange` | The trader, the pawnbroker, the seer |
| The joinable faction's members and allied fighters | teal | project variant | Owner, 2026-11-05: custom colours. The hue is an authoring default |
| Veinmarket | brown | project variant | As above |

Purple stays unused.

## 2. Animation

The main clips are in `Assets/Kevin Iglesias/Human Animations/Animations/Male/` and `.../Female/`. The slice uses the in-place clips. The `[RM]` root-motion clips are not used, because `PlayerLocomotion` moves the controller itself. **RPG_Animations_Pack_FREE** (DoubleL), `Assets/DoubleL/FBX Unity/`, adds clips on the same Humanoid rig.

| Mechanic | Clips | Pack |
|---|---|---|
| Walking, running, sprinting, jumping, turning, standing | `Movement/Walk`, `Run`, `Sprint`, `Jump`, `Turn`; `Idles/Idle01–03` | Human Basic Motions FREE |
| A Warden standing guard | `Idles/MilitaryIdle01` | Human Basic Motions FREE |
| Dialogue | `NPC/Dialogue/Dialogue_1–5`; `Social/Conversation/Talk01` | RPG_Animations_Pack_FREE; Kevin Iglesias Human Animations |
| Gathering herbs and flax; Skinning | `Work/Gathering/Gathering01–03` | Human Crafting Animations FREE |
| Mining ore | `Work/Mining/MiningOneHand01_* - Ground`, `- Wall` | Human Crafting Animations FREE |
| Felling a tree | no chopping clip; `MiningOneHand01_* - Wall` stands in | Human Crafting Animations FREE (gap) |
| Item Creation | `Work/Hammering/HammeringGround01_*` stands in for bench work | Human Crafting Animations FREE (gap) |
| One-handed melee: dagger, sword, axe, mace | `Combat/1H/Attack1H01_L`, `_R`, `CombatIdle1H01`; `One Hand Up/Attack_A`, `Attack_B` | Human **Melee** Animations FREE; RPG_Animations_Pack_FREE |
| Shield with a sword, an axe, or a mace | `Combat/Shield/AttackShield01`; `One Hand Up/Sheild/Shield_Block_Idle_1`, `1Hand_Up_Shield_Block_Hit_1` (block) | Human Melee Animations FREE; RPG_Animations_Pack_FREE |
| Two-handed melee: greatsword, greataxe, maul | `Combat/2H/Attack2H01`, `CombatIdle2H01` | Human Melee Animations FREE |
| Taking a hit, dying | `Combat/CombatDamage01`, `Damage01–02`, `Death01–03`, `CombatIdle01`; `Hit/Hit_B_4`, `Hit_B_5` (and `Hit/InPlace`) | Human Melee / Soldier Animations FREE; RPG_Animations_Pack_FREE |
| Rifle shot | `Combat/Rifle/Rifle_Aim01`, `Rifle_Aim01_Shoot01`, `Rifle_Reload01` | Human Soldier Animations FREE |
| Pistol shot | `Combat/Gun/Gun_Aim01`, `Gun_Aim01_Shoot01`, `Gun_Reload01` | Human Soldier Animations FREE |
| Abilities and Infusion | `Combat/Spellcasting/CastingEnter01`, `CastingIdle01`, `CastingExit01`, `CastingDamage01`; `MagicAttacks/Call`, `Directional`, `Omnidirectional` (each with *Load* and *Cast*) | Human Spellcasting Animations FREE |
| Climbing a ladder | `Actions/Ladder/*` | RPG_Animations_Pack_FREE |

- The owner's list named Human Crafting Animations for melee. The melee clips are in **Human Melee Animations FREE**, which is in the project, so melee comes from there.
- A shot includes its loading ([10.2](../../../10.2%20Combat%20System%20Draft.md) §5b, no Reload action), so one shot plays Aim → Shoot → Reload as a single action.
- `Actions/Action/Action_A_4_1`, `Action_A_5_1`, `Action_A_5_2` are unnamed actions; look at them in the editor as stand-ins for brewing or First Aid.
- Not used: `Polearm/*` (spears do not exist, owner 2026-11-05), DoubleL's `Bow/*` and `Enemy Attack/*` until a beast needs them, and the assault rifle, bazooka, grenade, and dual-gun clips.
- **Gaps, no clip yet:** sneaking in Stealth Mode, sleeping, brewing, First Aid, the Defensive Stance, and Yield. The idle or gathering clips stand in until a clip is found.

## 3. Weapons, tools, and items

| Item ([10.2](../../../10.2%20Combat%20System%20Draft.md) §5b, [19](../../../19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md)) | Source | Files |
|---|---|---|
| Axe, greataxe | FREE - RPG Weapons | `Assets/Blink/Art/Weapons/LowPoly/FreeRPGWeapons`: `Axe1H_*`, `Axe2H_*` |
| Mace, maul | FREE - RPG Weapons | `Mace1H_*`, `Mace2H_*` |
| Sword, greatsword | FREE - Low Poly Swords | `Assets/Blink/Art/Weapons/LowPoly/FreeSwords`: `Sword1`–`Sword15` variants |
| Dagger | Low-Poly Weapons (proposed; not in the owner's list) | `Assets/Low-Poly Weapons/Prefabs/Dagger.prefab` |
| Wooden and metal shield | FREE - RPG Weapons | `FreeRPGWeapons/_PREFABS/Shield_Basic` (wooden), `Shield_Medium` (metal). `Shield_Epic` reads as magic and is not used |
| Cartridge Pouch | Tripo (the owner's model) | `Assets/TripoModels/leather_satchel_3d_model/leather_satchel_3d_model.fbx`, URP Lit. The pouch of every firearm (owner, 2026-11-05) |
| Pistol | Tripo (the owner's model) | `Assets/TripoModels/flintlock_pistol_3d_model/`, URP Lit |
| Rifle | Tripo (the owner's model) | `Assets/TripoModels/ornate_flintlock_rifle_3d_model/`, URP Lit |
| Woodcutting axe, pickaxe, crafting hammer | Stylized 3D Tools | `Assets/Stylized 3D Tools/Prefabs/axe01`, `pickaxe01`, `hammer01` |
| Potions and brewing glass | Multistory Dungeons 2; Low-Poly Weapons | `Assets/Multistory Dungeons 2/Prefabs/Props/Books And Alchemy/`: `Potion_01–04`, `Potion_Blue_01`, `Potion_Red_01`, `Flask_01–05`, `Retort_Preset_01`; also `Low-Poly Weapons/Prefabs/Potion_*`. Models only; [20](../../../20%20Alchemy%20and%20Potioncraft.md) decides which potions exist |
| Coins: bronze, silver, gold | Basic Treasure Coins | `Assets/REAL_DEDICATED/BasicTreasureCoins/Prefabs/CoinPile_Copper` (bronze), `CoinPile_Silver`, `CoinPile_Gold` |

- Every weapon is iron (10.2), so swords and blades use the plain metal variants (for example `Sword15_Iron`, `Sword1_Silver`). The lava, frost, blood, and coloured variants read as magic weapons the slice does not have.
- A shield goes only with a sword, an axe, or a mace (owner, 2026-11-05; 10.2 §5b).
- Not used, because the design has no such weapons (10.2 §5b): staffs, wands, bows, and spears from RPG Weapons; bows, crossbows, arrows, bombs, and grenades from Low-Poly Weapons.
- Money is bronze, silver, and gold, 100 to 1 ([19](../../../19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) §6.3). Basic Treasure Coins has one pile for each metal; its copper pile is the bronze.

## 4. Places and props

| Place or use | Source | Location | State |
|---|---|---|---|
| The city's houses (trial) | Modular Victorian City; Modular European House | `Assets/Modular Victorian City/`; `Assets/Modular European House/` | Used as they are (owner, 2026-11-05); their textures may later be tuned toward brick or stone |
| Villages and buildings outside the city | FantasyMedievalTown_LITE | `Assets/FantasyMedievalTown_LITE/` (walls, floor, corner, roof, stairs) | URP |
| The market; the Master of Item Creation | Low-Poly Medieval Market | `Assets/Low-Poly Medieval Market/` | imported |
| The places of Alchemy and of the Master of Enchanting; candles, lamps, barrels, furniture, chests | Multistory Dungeons 2 (the URP import of Alchemy Lab Props) | `Assets/Multistory Dungeons 2/Prefabs/Props/` (`Books And Alchemy`, `Furniture`, `Lantern_*`, `Torch_*`, `Treasure_Chest_*`), `Prefabs/VFX/` (candle and torch fire) | URP |
| The old chapel, the faction's church, the graveyard | Colonial City LittlePack – Church Graveyard Environment | `Assets/3DShapes/Colonial City LittlePack – Church Graveyard Environment/` | imported |
| Street and house props | Low Poly Simple Medieval Props; Victorian Objects | `Assets/LowPolyMedievalPropsLite/`; `Assets/Victorian Objects/Props/City_Bench.glb`, `City_Lantern.glb` | the `.glb` files are not models: send them through the Tripo Bridge as FBX if they are wanted |
| Roadside fences and props | Roadside Tales Free (EmaceArt) | `Assets/EmaceArt/Fences 2026/` (`Meshes/EA03_Fence_*`, `Prefabs/`) | URP |
| Street and square paving | Yughues Free Pavement Materials | `Assets/YughuesFreePavementsMaterials/Materials/M_YFPM_*` | URP |
| The sea and the River Veil | WaterWorks | `Assets/WaterWorks/`; the scene's `Water` sheet already uses `Assets/Settings/Water/OverworldWater.mat` on WaterWorks' `SSR_Water` Shader Graph | in use. A water of our own is made only if this one fails in the slice |
| The cellar under the Soot Stair, other cellars | Low Poly Dungeons Lite | `Assets/LowPolyDungeonsLite/` | URP |
| Fellable trees; herb and flax patches | Stylized Trees Pack | `Assets/Stylized Trees Pack/Assets/Prefabs/`: `Trees/Birch 001–003`, `Trees/Warped 001–003` (`Sakura` reads as out of place in Veilmouth); `Bushes/Bush 001–025` for herb and flax patches | the pack's own URP shaders (`Shared/Shaders/Foliage`, `Surface`). An emptied node is removed, with no stump (owner, 2026-11-05; [19](../../../19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) §6.4) |
| Iron ore veins, the slice's only ore | Tripo (the owner's model) | `Assets/TripoModels/rugged_rock_formation_3d_model/` | URP Lit. An emptied vein disappears (owner, 2026-11-05) |
| The Notice Board | Tripo (the owner's model) | `Assets/TripoModels/ornate_notice_board_3d_model/` | URP Lit |
| Beds: the tavern room, homes | Tripo (the owner's model) | `Assets/TripoModels/ornate_wooden_bed_3d_model/` | URP Lit |
| The bridge over the River Veil | Modular Fantasy Bridges | `Assets/Julien Tonsuso/Modular Fantasy Bridges/Models/bridge_01`, `bridge_02` and their `_piece_*` parts | materials inside the FBX files; check in the editor |
| The ritual: a magic circle on the ground, with the conductors standing round it | a decal or a flat mesh of our own | none yet | the owner's form, 2026-11-05; its rules are in 21 §4: three conductors stand round it, and two guards and the senior beside them |

## 5. Effects, sound, cursors, icons

| Use | Source | Location | Note |
|---|---|---|---|
| Ability effects, smoke | Magic Effects FREE (Hovl Studio) | `Assets/Hovl Studio/Magic effects pack/` | URP particles |
| Fire | Free Fire VFX – URP (Vefects) | `Assets/Vefects/Free Fire VFX URP/` | URP; includes looping fire sounds |
| Footsteps | Footsteps - Essentials | `Assets/Footsteps - Essentials/` | Walk, run, and land for each surface: streets on `Gravel` or `Rock`, forest on `Leaves` and `Grass`, interiors on `Wood`, shore on `Sand`, river on `Water` |
| Cursors | Basic RPG Cursors | `Assets/Cursors/` (`Cursors 64`, `Cursors 256`) | For the Combat Grid and menus. Free exploration keeps the mouse locked |
| Every icon in the game: items, abilities, conditions, HUD | Modern RPG - Free icons pack | `Assets/Modern GDR - Free icons pack/00_Atlas/` (`BrightIcons`, `DarkIcons`, and their backgrounds) | Atlas images for the UI Toolkit HUD (S0.5) |
| Gunshots, impacts (proposed; not in the owner's list) | *Free Pack* sounds | `Assets/Free Pack/` | The only shot sounds in the project |

## 6. Beasts

| Beast | Source | Location | Note |
|---|---|---|---|
| Wolves and boars, until real models exist | Dragon for Boss Monster : HP | `Assets/FourEvilDragonsHP/Prefab/` — four dragons in six colours | URP. Scaled down to a wolf's or a boar's size, so the Combat Grid holds them like any fighter. A beast that glows ([11](../../../11%20Vertical%20Slice.md)) uses the material's emission |

The owner's "dragons" are this pack (2026-11-05); the PBR version is not used.

## 7. Render state on disk, 2026-11-05, after the owner's conversion

Counted from every `.mat` file's shader after the owner ran the Render Pipeline Converter. A skybox material on a built-in skybox shader renders in URP and is not counted as a problem. The converter's warnings about having no way to convert a material come from materials already on a URP or custom shader (particles, Vefects, the tree pack, demo scenes) and from the Human Character Dummy materials below; none of them blocks the slice.

| Pack | Materials | Before use |
|---|---|---|
| Modular Victorian City, Modular European House, FantasyMedievalTown_LITE, Low-Poly Medieval Market, Multistory Dungeons 2, Colonial City LittlePack, LowPolyMedievalPropsLite, LowPolyDungeonsLite, Roadside Tales Free, Yughues Free Pavement Materials, FREE - RPG Weapons and Low Poly Swords (Blink), Stylized 3D Tools, FourEvilDragonsHP, Basic Treasure Coins, every Tripo model | URP Lit | ready |
| Magic Effects FREE (Hovl), Free Fire VFX URP | URP particles and the pack's own URP shaders | ready |
| Stylized Trees Pack | the pack's own URP shaders | ready, once its editor script compiles (fixed 2026-11-05, below) |
| Low-Poly Weapons (2) | URP Simple Lit | ready |
| Human Character Dummy (8) and the Human Animations demo materials | built-in Unlit: `HumanDummy.mat` and its colour variants, which pick their colour by moving over one palette texture | renders in URP, but flat, with no light or shadow. The converter has no way for material variants of an unlit shader. The slice's colours (§1) are made as project materials on URP Lit when the characters are wired |
| Modular European House `Skybox BuiltIn`, `M_Cypress` | a Shader Graph skybox; URP SpeedTree | ready |
| Modern RPG - Free icons pack, RPG_Animations_Pack_FREE | images; animation clips | ready |

**Stylized Trees Pack fix** (agent, 2026-11-05): `Shared/Shaders/Editor/GradientGUIDrawer.cs` called `Object.GetInstanceID()`, which Unity 6.3 made an error (CS0619). It now calls `GetEntityId()` on Unity 6.3 and later; the value is only a dictionary key. The pack is a vendor folder outside git, so a reimport of the pack undoes the fix.

**Found by the dressing pass** (agent, 2026-11-05), each handled without changing the vendor files:

- **Tripo base maps.** The Tripo Bridge left `_BaseMap` empty on nine of the owner's ten models (all but the iron ore), though each image lies in the model's `.fbm` folder: the models showed white. `SliceKitCatalogue` gives each wrapper a project copy of the material with that image (`Assets/Materials/Slice/Tripo/`). A model used outside the kit needs the same, or the bridge fixed.
- **Stylized Trees Pack wind.** The pack's Amplify shaders bend every vertex by the global wind its `Growth.WindController` (with a `WindZone`) sets each frame; with none in a scene the trees and bushes vanish. Veilmouth has one `Wind` under `Dressing`; any other scene with the pack needs one.
- **The bridge's Substance.** Modular Fantasy Bridges' material is a Substance (`Stylized_Bridge.sbsar`) and shows magenta without the Substance plugin; the bridge wears Yughues `M_YFPM_Blocs`.

**Found by the owner's play test** (2026-11-05):

- **Human Character Dummy.** Its materials are built-in `Unlit/Texture`, which this URP project does not draw: the player and every Dummy but the project-coloured ones were invisible. `SliceCharacterLook` gives each a URP Lit copy with the same palette texture and offset (`Assets/Materials/Slice/Dummy/`).
- **FourEvilDragonsHP controllers.** `SouleaterCTRL` and `NightmareCTRL` are the pack's demo, chaining every clip with exit times (flight, death, and the rest). The stand-ins wear project controllers with the pack's `Idle` alone (`Assets/Settings/Slice/`).
- **BK_AlchemistHouse** (owner, 2026-11-05: the base for every interior). 157 prefabs: interior walls with doors and windows, wooden and stone floors, ceilings and beams, stairs, doors, the stove, a table with a sink, cupboards, shelves, drawers, tables, chairs, carpets, paintings, books, bottles, flasks, jars, candles and oil lamps, cauldrons and lab sets. Its 143 materials are built-in Standard and need URP copies; its volumetric-light scripts and shaders are built-in only and stay out. Its jars of babies and brains are not used.
- **Polytope Studio Lowpoly Environments** (owner, 2026-11-05: plain green trees and grass as common cover). `PT_Fruit_Tree_01_green` and `PT_Pine_Tree_03_green` with their `cut`, `stump`, and `logs` variants, `PT_Grass_02`, shrubs, flowers, rocks. The Stylized Trees Pack's coloured trees become rare, for resources.

`Assets/AlchemyLabProps/` is no longer needed: Multistory Dungeons 2 is its URP import.

## 8. Open

1. **Missing clips:** sneaking, sleeping, brewing, First Aid, felling, the Defensive Stance, and Yield.
2. **The Tripo Bridge and ZIP.** The bridge (`com.tripo3d.unitybridge`, *GameDev → Tools → Tripo Bridge - Check Port and Open*) imports a model sent as FBX, but two pistols sent as ZIP failed with *End of Central Directory record could not be found*: the archive arrived broken or cut short. Send each model as **FBX**, one at a time.

## 9. Models the slice still needs

What no pack in this map gives, checked against [11](../../../11%20Vertical%20Slice.md) and the C0 map (plan §2) on 2026-11-05 and updated the same day. **Blocks** means a mechanic cannot be read or played without it; **place** means a C0 place has no fitting model; **polish** can wait. Until a model exists, a kit stand-in holds its place (kit standard §9).

| # | Model | For | Priority | State |
|---|---|---|---|---|
| 1 | Pistol and rifle | Ranged fighters, 10.2 §5b | blocks | **done**: Tripo (§3) |
| 2 | Iron ore vein | Gathering ore, 12 veins on the hill | blocks | **done**: Tripo (§4); an emptied vein disappears, so no spent model |
| 3 | Herb patch and flax plant | Gathering, 30 herbs and 10 flax | blocks | **done**: Stylized Trees Pack bushes (§4) |
| 4 | Fellable tree | Felling, about 40 trees | blocks | **done**: Stylized Trees Pack (§4); no stump |
| 5 | Notice Board | Information Source, tasks | blocks | **done**: Tripo (§4) |
| 6 | Magic circle on the ground | The ritual, 21 §4 | blocks | **done**: our own flat mesh, `Kit_Ritual_Circle_8x0`, two rings of 4 and 3 m and three conductors' places (agent, 2026-11-05). No Weak Point objects: the owner's physical vulnerability is the conductors themselves. A drawn decal can replace it in S7 |
| 7 | Bed | Sleep in a tavern room | blocks | **done**: Tripo (§4) |
| 8 | Wolf and boar, rigged, with idle, walk, run, attack, hit, and death | Beasts of the slice | place | the dragons stand in (§6) |
| 9 | Bridge over the River Veil, about 14 × 6 m | C0: the one bridge | place | **done**: Modular Fantasy Bridges (§4) |
| 10 | ~~Factory yard~~ | — | — | **dropped**: the slice has no factory locations, and Ritual Site Candidate 1 is a square in Lower Veil (owner, 2026-11-05) |
| 11 | Cell bars and a cell door | The Wardens' lock-up interior, Yield | place | **done**: Tripo, the owner's models (`rusty_iron_gate`, `rusty_iron_jail_door`); the door stands in the bars' own doorway |
| 12 | Bar counter and tavern shelves | The Soot & Bell interior | place | **done**: Tripo, the owner's models (`ornate_wooden_bar_counter`, `ornate_cabinet`), with a Multistory Dungeons 2 shelf |
| 13 | Quay or pier on the sea front | C0: the sea to the south | polish | none |
| 14 | Worn armour: cloth, leather, chain, and plate on a Dummy | Reading a fighter's gear in a fight | polish | Market `armor_*` for shop display only; Dummies' colours read the role |
| 15 | A Spirit General apart from the pink Dummy | The General's appearance | polish | the pink Dummy (§1) |
| 16 | Interior walls and floors for houses: wood, plaster, paper | The seven interiors | place | **chosen**: BK_AlchemistHouse (owner, 2026-11-05); the rooms are rebuilt from it after C1 is accepted |
| 17 | Doors that open, matching the houses' own | Entering the seven buildings | polish | the leaf is hidden and only its trigger stays (owner, 2026-11-05); the house's own door is the one seen |
| 20 | A town wall with gates, and toll houses | The city's edge, C1 §3.8 | place | none yet; to find in the agreed packs or ask the owner |
| 21 | A palace, a town hall, an assize court, a customs house | Crown Avenue and the Government Quarter, C1 §3.1 | place | to assemble from the Victorian modules, larger; a dome or tower for the palace is missing |
| 22 | Carriages and horses, a crane, a fountain or statue, a water pump, washing lines | Streets, docks, squares, and courts, C1 §6–§7 | polish | none yet |
| 18 | A cellar entrance: a hatch or steps down in a wall | The cellar under the Soot Stair | place | a Low Poly Dungeons wall with a door leaf at the stair's foot |
| 19 | Grey, pink, teal, and brown Dummies as proper materials | §1 colours | polish | teal and brown are plain URP Lit (`Materials/Slice/Dummy_*`); the Dummies' own colours are still built-in Unlit (§7) |

Already covered, so not needed: anvil and workshop (Market `anvil_with_hammer`, `leather_dressing_frame`, tables), alchemy table and glass, the cache and the formula (`Treasure_Chest_*`, `Book_*`, `Scroll_01`), shop counters (Market stalls and sections), loot (`bag`, `bag_of_gold`), coins, street lamps (Victorian City `Lamp post`, Colonial `StreetLamp1`), the church and graveyard, stairs for the Soot Stair (Victorian City `Steps`), fences and the pouch. Bandages, splints, and antidotes need no world model: they live in the inventory as icons.

Resolved by the owner on 2026-11-05: spears do not exist and a shield goes with a sword, an axe, or a mace; red marks only Cult Forces that act openly; the faction's members, allied fighters, and Veinmarket get colours of their own; money is bronze, silver, and gold, 100 to 1; FantasyMedievalTown_LITE serves villages and outer buildings, and Modular Victorian City with Modular European House serves the city.

# Vertical Slice — Asset Map

Status: Draft — the owner's list of packs and answers, 2026-11-05; checked against the project on disk and the design documents by the agent
Created: 2026-11-05
Last Updated: 2026-11-05 (the owner's second list: Multistory Dungeons 2 for alchemy, Victorian Objects weapons, Stylized 3D Tools, RPG_Animations_Pack_FREE, Roadside Tales Free, Modern RPG icons, Yughues pavements, WaterWorks; the owner's answers on shields, hidden followers, colours, coins, and buildings)
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
| Pistol, rifle, Cartridge Pouch | Victorian Objects | `Assets/Victorian Objects/Weapons/pistol.glb`, `rifle.glb`, `satchel.glb`. The satchel is the Cartridge Pouch of every firearm (owner, 2026-11-05). **Not imported as models yet** (§7) |
| Woodcutting axe, pickaxe, crafting hammer | Stylized 3D Tools | `Assets/Stylized 3D Tools/Prefabs/axe01`, `pickaxe01`, `hammer01` |
| Potions and brewing glass | Multistory Dungeons 2; Low-Poly Weapons | `Assets/Multistory Dungeons 2/Prefabs/Props/Books And Alchemy/`: `Potion_01–04`, `Potion_Blue_01`, `Potion_Red_01`, `Flask_01–05`, `Retort_Preset_01`; also `Low-Poly Weapons/Prefabs/Potion_*`. Models only; [20](../../../20%20Alchemy%20and%20Potioncraft.md) decides which potions exist |
| Coins | Basic Treasure Coins: **not on disk** | in the project instead: `LowPolyMedievalPropsLite/Prefabs/Coin_01–03`, `Low-Poly Medieval Market/Prefabs/coin`, `coins` |

- Every weapon is iron (10.2), so swords and blades use the plain metal variants (for example `Sword15_Iron`, `Sword1_Silver`). The lava, frost, blood, and coloured variants read as magic weapons the slice does not have.
- A shield goes only with a sword, an axe, or a mace (owner, 2026-11-05; 10.2 §5b).
- Not used, because the design has no such weapons (10.2 §5b): staffs, wands, bows, and spears from RPG Weapons; bows, crossbows, arrows, bombs, and grenades from Low-Poly Weapons.
- Money is bronze, silver, and gold, 100 to 1 ([19](../../../19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) §6.3). One coin model in three tints serves all three until Basic Treasure Coins is imported.

## 4. Places and props

| Place or use | Source | Location | State |
|---|---|---|---|
| The city's houses (trial) | Modular Victorian City; Modular European House | `Assets/Modular Victorian City/`; `Assets/Modular European House/` | Used as they are (owner, 2026-11-05); their textures may later be tuned toward brick or stone |
| Villages and buildings outside the city | FantasyMedievalTown_LITE | `Assets/FantasyMedievalTown_LITE/` (walls, floor, corner, roof, stairs) | import its URP package first |
| The market; the Master of Item Creation | Low-Poly Medieval Market | `Assets/Low-Poly Medieval Market/` | imported |
| The places of Alchemy and of the Master of Enchanting; candles, lamps, barrels, furniture, chests | Multistory Dungeons 2 (the URP import of Alchemy Lab Props) | `Assets/Multistory Dungeons 2/Prefabs/Props/` (`Books And Alchemy`, `Furniture`, `Lantern_*`, `Torch_*`, `Treasure_Chest_*`), `Prefabs/VFX/` (candle and torch fire) | URP |
| The old chapel, the faction's church, the graveyard | Colonial City LittlePack – Church Graveyard Environment | `Assets/3DShapes/Colonial City LittlePack – Church Graveyard Environment/` | imported |
| Street and house props | Low Poly Simple Medieval Props; Victorian Objects | `Assets/LowPolyMedievalPropsLite/`; `Assets/Victorian Objects/Props/City_Bench.glb`, `City_Lantern.glb` | the `.glb` files are not imported as models yet (§7) |
| Roadside fences and props | Roadside Tales Free (EmaceArt) | `Assets/EmaceArt/Fences 2026/` (`Meshes/EA03_Fence_*`, `Prefabs/`) | URP |
| Street and square paving | Yughues Free Pavement Materials | `Assets/YughuesFreePavementsMaterials/Materials/M_YFPM_*` | import `YughuesFreePavementMaterials_URP.unitypackage` first |
| The sea and the River Veil | WaterWorks | `Assets/WaterWorks/`; the scene's `Water` sheet already uses `Assets/Settings/Water/OverworldWater.mat` on WaterWorks' `SSR_Water` Shader Graph | in use. A water of our own is made only if this one fails in the slice |
| The cellar under the Soot Stair, other cellars | Low Poly Dungeons Lite | `Assets/LowPolyDungeonsLite/` | imported |

## 5. Effects, sound, cursors, icons

| Use | Source | Location | Note |
|---|---|---|---|
| Ability effects, smoke | Magic Effects FREE (Hovl Studio) | `Assets/Hovl Studio/Magic effects pack/` | Built-in particle shaders: convert |
| Fire | Free Fire VFX – URP (Vefects) | `Assets/Vefects/Free Fire VFX URP/` | URP; includes looping fire sounds |
| Footsteps | Footsteps - Essentials | `Assets/Footsteps - Essentials/` | Walk, run, and land for each surface: streets on `Gravel` or `Rock`, forest on `Leaves` and `Grass`, interiors on `Wood`, shore on `Sand`, river on `Water` |
| Cursors | Basic RPG Cursors | `Assets/Cursors/` (`Cursors 64`, `Cursors 256`) | For the Combat Grid and menus. Free exploration keeps the mouse locked |
| Every icon in the game: items, abilities, conditions, HUD | Modern RPG - Free icons pack | `Assets/Modern GDR - Free icons pack/00_Atlas/` (`BrightIcons`, `DarkIcons`, and their backgrounds) | Atlas images for the UI Toolkit HUD (S0.5) |
| Gunshots, impacts (proposed; not in the owner's list) | *Free Pack* sounds | `Assets/Free Pack/` | The only shot sounds in the project |

## 6. Beasts

| Beast | Source | Location | Note |
|---|---|---|---|
| Wolves and boars, until real models exist | Dragon for Boss Monster : HP | `Assets/FourEvilDragonsHP/Prefab/` — four dragons in six colours | Imported. Built-in materials: convert. Scaled down to a wolf's or a boar's size, so the Combat Grid holds them like any fighter. A beast that glows ([11](../../../11%20Vertical%20Slice.md)) uses the material's emission |
| — | Dragon for Boss Monster : PBR | **not on disk**, though the owner imported it on 2026-11-05 | — |

## 7. Render state on disk, 2026-11-05

| Pack | Materials | Before use |
|---|---|---|
| Modular Victorian City, Low-Poly Medieval Market (8 of 9), Free Fire VFX URP, Multistory Dungeons 2, Roadside Tales Free, WaterWorks | URP | ready |
| FREE - RPG Weapons and Low Poly Swords (Blink), Colonial City LittlePack, FourEvilDragonsHP, Modular European House (19 of 21) | Built-in Standard | Render Pipeline Converter |
| Stylized 3D Tools | Built-in Standard (Specular setup) | Render Pipeline Converter |
| Magic Effects FREE (Hovl) | Built-in particles | Render Pipeline Converter |
| LowPolyDungeonsLite, LowPolyMedievalPropsLite, FantasyMedievalTown_LITE, Yughues Free Pavement Materials | Built-in Standard | import the pack's own URP `.unitypackage` |
| Victorian Objects | `.glb` files with no importer: Unity sees them as plain files | needs a glTF importer package, such as glTFast (`com.unity.cloud.gltfast`); packages are the owner's call |
| Human Character Dummy, Low-Poly Weapons | built-in shader ids, not Standard | check in the editor |
| Modern RPG - Free icons pack, RPG_Animations_Pack_FREE | images; animation clips | ready |

`Assets/AlchemyLabProps/` is no longer needed: Multistory Dungeons 2 is its URP import.

## 8. Open

1. **Not on disk:** Basic Treasure Coins and Dragon for Boss Monster : PBR. The owner reports both imported on 2026-11-05, but no folder holds them; the import may not have finished.
2. **The `.glb` weapons and props** need a glTF importer package before they can be placed (§7).
3. **Missing clips:** sneaking, sleeping, brewing, First Aid, felling, the Defensive Stance, and Yield.

Resolved by the owner on 2026-11-05: spears do not exist and a shield goes with a sword, an axe, or a mace; red marks only Cult Forces that act openly; the faction's members, allied fighters, and Veinmarket get colours of their own; money is bronze, silver, and gold, 100 to 1; FantasyMedievalTown_LITE serves villages and outer buildings, and Modular Victorian City with Modular European House serves the city.

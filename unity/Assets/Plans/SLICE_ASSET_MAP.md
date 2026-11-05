# Vertical Slice — Asset Map

Status: Draft — the owner's list of packs, 2026-11-05; checked against the project on disk and the design documents by the agent
Created: 2026-11-05
Last Updated: 2026-11-05
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
- Every row was checked on disk on 2026-11-05. *Not imported* means: do not write code against it until it is imported.

---

## 1. Characters

All people use **Human Character Dummy** (Kevin Iglesias), `Assets/Kevin Iglesias/Human Character Dummy/Prefabs/HumanDummy_M <Colour>.prefab` and `HumanDummy_F <Colour>.prefab`. The pack ships White, Blue, Green, Orange, Purple, Red, and Yellow.

| Who | Colour | Source | Note |
|---|---|---|---|
| The player | grey | a project material variant of `Materials/HumanDummy.mat` | No grey prefab ships. The slice player is White today (S0.3) |
| Wardens, the city's police | blue | `HumanDummy_* Blue` | |
| Cult Forces | red | `HumanDummy_* Red` | Open: hidden followers (§8) |
| Masters of the professions | green | `HumanDummy_* Green` | Item Creation, Enchanting, First Aid; Harrow, who teaches Alchemy |
| Ordinary townsfolk | yellow | `HumanDummy_* Yellow` | |
| Cult Generals | pink | a project material variant | No pink prefab ships |
| Sellers | orange | `HumanDummy_* Orange` | The trader, the pawnbroker, the seer |
| The joinable faction's members and allied fighters; Veinmarket | — | — | Open (§8); purple is the stock colour left |

## 2. Animation

All clips are in `Assets/Kevin Iglesias/Human Animations/Animations/Male/` and `.../Female/`. The slice uses the in-place clips. The `[RM]` root-motion clips are not used, because `PlayerLocomotion` moves the controller itself.

| Mechanic | Clips | Pack |
|---|---|---|
| Walking, running, sprinting, jumping, turning, standing | `Movement/Walk`, `Run`, `Sprint`, `Jump`, `Turn`; `Idles/Idle01–03` | Human Basic Motions FREE |
| A Warden standing guard | `Idles/MilitaryIdle01` | Human Basic Motions FREE |
| Dialogue | `Social/Conversation/Talk01` | Kevin Iglesias Human Animations |
| Gathering herbs and flax; Skinning | `Work/Gathering/Gathering01–03` | Human Crafting Animations FREE |
| Mining ore | `Work/Mining/MiningOneHand01_* - Ground`, `- Wall` | Human Crafting Animations FREE |
| Felling a tree | no chopping clip; `MiningOneHand01_* - Wall` stands in | Human Crafting Animations FREE (gap) |
| Item Creation | `Work/Hammering/HammeringGround01_*` stands in for bench work | Human Crafting Animations FREE (gap) |
| One-handed melee: dagger, sword, axe, mace | `Combat/1H/Attack1H01_L`, `_R`, `CombatIdle1H01` | Human **Melee** Animations FREE |
| Two-handed melee: greatsword, greataxe, maul | `Combat/2H/Attack2H01`, `CombatIdle2H01` | Human Melee Animations FREE |
| Taking a hit, dying | `Combat/CombatDamage01`, `Damage01–02`, `Death01–03`, `CombatIdle01` | Human Melee / Soldier Animations FREE |
| Rifle shot | `Combat/Rifle/Rifle_Aim01`, `Rifle_Aim01_Shoot01`, `Rifle_Reload01` | Human Soldier Animations FREE |
| Pistol shot | `Combat/Gun/Gun_Aim01`, `Gun_Aim01_Shoot01`, `Gun_Reload01` | Human Soldier Animations FREE |
| Abilities and Infusion | `Combat/Spellcasting/CastingEnter01`, `CastingIdle01`, `CastingExit01`, `CastingDamage01`; `MagicAttacks/Call`, `Directional`, `Omnidirectional` (each with *Load* and *Cast*) | Human Spellcasting Animations FREE |

- The owner's list named Human Crafting Animations for melee. The melee clips are in **Human Melee Animations FREE**, which is in the project, so melee comes from there.
- A shot includes its loading ([10.2](../../../10.2%20Combat%20System%20Draft.md) §5b, no Reload action), so one shot plays Aim → Shoot → Reload as a single action.
- Not used: `Shield/AttackShield01` and `Polearm/*` (§8), and the assault rifle, bazooka, grenade, and dual-gun clips.
- **Gaps, no clip yet:** sneaking in Stealth Mode, sleeping, brewing, First Aid, the Defensive Stance, and Yield. The idle or gathering clips stand in until a clip is found.

## 3. Weapons and items

| Item ([10.2](../../../10.2%20Combat%20System%20Draft.md) §5b, [19](../../../19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md)) | Source | Files |
|---|---|---|
| Axe, greataxe | FREE - RPG Weapons | `Assets/Blink/Art/Weapons/LowPoly/FreeRPGWeapons`: `Axe1H_*`, `Axe2H_*` |
| Mace, maul | FREE - RPG Weapons | `Mace1H_*`, `Mace2H_*` |
| Sword, greatsword | FREE - Low Poly Swords | `Assets/Blink/Art/Weapons/LowPoly/FreeSwords`: `Sword1`–`Sword15` variants |
| Dagger | Low-Poly Weapons (proposed; not in the owner's list) | `Assets/Low-Poly Weapons/Prefabs/Dagger.prefab` |
| Rifle, pistol, Cartridge Pouch | **gap**: no pack has them | stand-in boxes until a pack is found |
| Potions | Low-Poly Weapons | `Potion_Health`, `Potion_Stamina`, `Potion_Generic`, `Potion_Berserk`, `Potion_Mana`. These are models only; [20](../../../20%20Alchemy%20and%20Potioncraft.md) decides which potions exist |
| Coins | Basic Treasure Coins: **not imported** | in the project instead: `LowPolyMedievalPropsLite/Prefabs/Coin_01–03`, `Low-Poly Medieval Market/Prefabs/coin`, `coins` |

- Every weapon is iron (10.2), so swords and blades use the plain metal variants (for example `Sword15_Iron`, `Sword1_Silver`). The lava, frost, blood, and coloured variants read as magic weapons the slice does not have.
- Not used, because the design has no such weapons (10.2 §5b): staffs, wands, and bows from RPG Weapons; bows, crossbows, arrows, bombs, and grenades from Low-Poly Weapons. Spears and shields are open (§8).

## 4. Places and props

| Place or use | Source | Location | State |
|---|---|---|---|
| Building blocks (temporary trial) | Low Poly Fantasy Medieval Village Vol.2 Lite | not found under this name; open (§8) | — |
| The market; the Master of Item Creation | Low-Poly Medieval Market | `Assets/Low-Poly Medieval Market/` | imported |
| The places of Alchemy and of the Master of Enchanting | Alchemy Lab Props | `Assets/AlchemyLabProps/` holds only the `.unitypackage` files | import `AlchemyLabProps_URP_2019.4.unitypackage` first |
| The old chapel, the faction's church, the graveyard | Colonial City LittlePack – Church Graveyard Environment | `Assets/3DShapes/Colonial City LittlePack – Church Graveyard Environment/` | imported |
| Street and house props | Low Poly Simple Medieval Props | `Assets/LowPolyMedievalPropsLite/` | imported |
| The cellar under the Soot Stair, other cellars | Low Poly Dungeons Lite | `Assets/LowPolyDungeonsLite/` | imported |

## 5. Effects, sound, cursors

| Use | Source | Location | Note |
|---|---|---|---|
| Ability effects, smoke | Magic Effects FREE (Hovl Studio) | `Assets/Hovl Studio/Magic effects pack/` | Built-in particle shaders: convert |
| Fire | Free Fire VFX – URP (Vefects) | `Assets/Vefects/Free Fire VFX URP/` | URP; includes looping fire sounds |
| Footsteps | Footsteps - Essentials | `Assets/Footsteps - Essentials/` | Walk, run, and land for each surface: streets on `Gravel` or `Rock`, forest on `Leaves` and `Grass`, interiors on `Wood`, shore on `Sand`, river on `Water` |
| Cursors | Basic RPG Cursors | `Assets/Cursors/` (`Cursors 64`, `Cursors 256`) | For the Combat Grid and menus. Free exploration keeps the mouse locked |
| Gunshots, impacts (proposed; not in the owner's list) | *Free Pack* sounds | `Assets/Free Pack/` | The only shot sounds in the project |

## 6. Beasts

| Beast | Source | Location | Note |
|---|---|---|---|
| Wolves and boars, until real models exist | Dragon for Boss Monster : HP | `Assets/FourEvilDragonsHP/Prefab/` — four dragons in six colours | Imported. Built-in materials: convert. Scaled down to a wolf's or a boar's size, so the Combat Grid holds them like any fighter. A beast that glows ([11](../../../11%20Vertical%20Slice.md)) uses the material's emission |
| — | Dragon for Boss Monster : PBR | **not imported** | — |

## 7. Render state on disk, 2026-11-05

| Pack | Materials | Before use |
|---|---|---|
| Modular Victorian City, Low-Poly Medieval Market (8 of 9), Free Fire VFX URP | URP | ready |
| FREE - RPG Weapons and Low Poly Swords (Blink), Colonial City LittlePack, FourEvilDragonsHP | Built-in Standard | Render Pipeline Converter |
| Magic Effects FREE (Hovl) | Built-in particles | Render Pipeline Converter |
| LowPolyDungeonsLite, LowPolyMedievalPropsLite, FantasyMedievalTown_LITE, AlchemyLabProps | Built-in Standard | import the pack's own URP `.unitypackage` |
| Human Character Dummy, Low-Poly Weapons | built-in shader ids, not Standard | check in the editor |

## 8. Open

Recorded in [99 Open Questions](../../../99%20Open%20Questions.md) § Vertical Slice.

1. **Spears and shields.** The owner's list names them (RPG Weapons, the melee shield clip), but [10.2](../../../10.2%20Combat%20System%20Draft.md) §5b's weapons are the dagger, sword, axe, mace, greatsword, greataxe, maul, rifle, and pistol, and a firearm's other hand holds the Cartridge Pouch. Adding them means a new kind of fighter, new numbers, and a new simulation pass. Until the owner decides, they are not used.
2. **Hidden cult followers.** Traces of the Ritual Weak Points are left by townsfolk who joined the General earlier ([11](../../../11%20Vertical%20Slice.md)). If every follower is red, the colour gives that investigation away. The agent's proposal: red marks only Cult Forces that act openly, and a hidden follower keeps the colour of their everyday role until revealed.
3. **Colours for the joinable faction's members, allied fighters, and Veinmarket.** None is set; purple is the stock colour left.
4. **Gold, silver, and bronze coins.** [19](../../../19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) has one currency, gold. Are silver and bronze coins only a look, such as a coin pile's size, or are they denominations? Denominations would change 19.
5. **The building pack.** *Low Poly Fantasy Medieval Village Vol.2 Lite* is not in the project. The project has `FantasyMedievalTown_LITE` (walls, floor, corner, roof, stairs); is it the pack meant? The project also has packs closer to the era: `Modular Victorian City` (already URP), `Modular European House`, and the owner's `VictorianObjects`.
6. **Missing:** rifle, pistol, and Cartridge Pouch models; clips for sneaking, sleeping, brewing, First Aid, felling, the Defensive Stance, and Yield; Basic Treasure Coins and the dragons' PBR version are not imported.

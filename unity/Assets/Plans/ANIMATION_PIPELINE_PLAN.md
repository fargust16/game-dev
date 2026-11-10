# Animation Pipeline — Blender and Mixamo to Unity

Version: 0.1

Status: Draft — the owner's decision of 2026-11-10, planned by the agent; the steps wait for the owner's word, as every slice milestone does

Created: 2026-11-10

Last Updated: 2026-11-10 (§5: A0 built, the four stand-in clips and their controllers, the player's locomotion on them. Earlier: first version: the downloaded clip packs leave the project, the slice's own clips come from Mixamo and Blender, the rigs checked, the phases and the clip list)

Authority: [11 Vertical Slice](../../../11%20Vertical%20Slice.md), [SLICE_ASSET_MAP.md](SLICE_ASSET_MAP.md) §1–§2, [VERTICAL_SLICE_IMPLEMENTATION_PLAN.md](VERTICAL_SLICE_IMPLEMENTATION_PLAN.md)

Related: [10.2 Combat System Draft](../../../10.2%20Combat%20System%20Draft.md) §5a–§5b, [10.3 Stealth and Detection](../../../10.3%20Stealth%20and%20Detection.md), [99 Open Questions](../../../99%20Open%20Questions.md) VS-ANIM

---

## 1. The owner's decision

The owner, 2026-11-10:

- The downloaded animation packs cannot be used commercially, so they leave the project: Kevin Iglesias' Human Animations (Human Basic Motions, Crafting, Melee, Soldier and Spellcasting FREE), already removed; DoubleL's RPG_Animations_Pack_FREE; and the clips of Bublisher's 3D Stylized Animated Dogs Kit. The owner removes them.
- Every clip of the player and the NPCs is our own. The beasts but the dog are animated already. The dog's clips are crooked and are to be replaced.
- New clips are made with Blender and Mixamo and used in Unity.

## 2. What the rigs allow (measured, 2026-11-10)

`SliceCharacterRigs` (*GameDev → Slice → People*) checked the eight Tripo people the slice uses: the gentleman, the lady in the bustle dress, the poorer woman, the officer, the detective, the blacksmith, and the earlier townsman and woman.

- The six new models carry Mixamo's skeleton (`mixamorig:*`, 65 bones); the townsman and woman carry Tripo's own bone names (67). Each imports as a valid Unity **Humanoid**, the two older ones mapped by name (`Logs/SliceSurvey/rigs.txt`).
- A Humanoid clip plays on every one of them. The check posed all eight in the woman's Tripo idle and walk and in a one-handed attack; each pose came across clean, arms and legs where the clip puts them (`Logs/SliceSurvey/rig_check.png`).

So **the characters need not go to Blender before the work starts.** A clip is made once on any Humanoid skeleton and Unity's retargeting carries it to every model. Mixamo's own skeleton matches six of the eight bone for bone.

Blender is needed for three things only:

1. clips Mixamo has not got, or has only roughly: loading a flintlock, the Defensive Stance, Yield, sleeping, brewing, First Aid, picking a lock, and a lifted gesture or two;
2. the dog, since Mixamo animates only people;
3. skirts: the three women's skirts are skinned to the legs, so a wide stance tears them. Their weights are smoothed in Blender, or skirt bones added (§5, A5).

**Tripo's own clips** (the woman's file holds sixteen: idle, walk, run, greet, sit, clap and more) are quick and need no work, but they cannot be edited, are made for one model, and give no control over a blow, a shot or a hand on a weapon. They stand in for everyday clips until Mixamo's arrive (A0) and are not used for combat or shooting.

## 3. Rules

- **Humanoid everywhere.** Every person imports as Humanoid with its own avatar (`CreateFromThisModel`); every clip imports as Humanoid. No Generic person clip remains.
- **In place.** Clips are in place; the code moves the character (`PlayerLocomotion`, `TownsfolkWalker`, the Combat Grid's steps). Mixamo's *In Place* box is ticked where it has one.
- **One take, every model.** A clip is downloaded *Without Skin* once, not once a model. The agent checks it on all eight models with the rig check before it is wired in.
- **Mixamo's licence.** Mixamo's characters and clips may be used royalty-free in commercial games; they may not be sold or shared as raw files. The downloads live in `Assets/Animation/Mixamo/`, tracked in git (no skin, small). The owner signs in to Mixamo and downloads; the agent never signs in.
- **Blender's clips** are authored on one armature, the Mixamo skeleton exported from one of the six models, in `tools/blender/animation/` (source `.blend` outside the repo or in git LFS, owner's choice) and exported as FBX to `Assets/Animation/Blender/`.
- **Names.** A clip's asset name is its use, `Walk`, `Idle`, `PistolShot`, never Mixamo's (`Standing Idle (2)`).

## 4. Download settings (Mixamo)

| Setting | Value |
|---|---|
| Format | FBX for Unity (.fbx) |
| Skin | Without Skin |
| Frames per second | 30 |
| Keyframe reduction | none |
| In Place | ticked where offered |
| Character | any; the agent retargets |

Unity import: Rig → Humanoid, Avatar *Create From This Model*; Animation → Loop Time on cycles, Root Transform Rotation and Position Y *Bake Into Pose*, based on Body Orientation and Feet; Position XZ *Bake Into Pose* on in-place clips.

## 5. Phases

Each phase waits for the owner's word.

**A0 — stand-ins, now (agent).** The eight models are Humanoid. The townsfolk, the named people and the player walk and stand in Tripo clips until A1: the women in the woman's own idle and walk; the men in the earlier walk and idle baked once from the borrowed Generic clips into Humanoid clips. The player's locomotion, whose clips were Kevin Iglesias', uses the same stand-ins. No combat clip exists until A1.

*A0 built, 2026-11-10* (`SlicePeopleModels`; report `Logs/SliceSurvey/people.txt`):

- **The four stand-ins** are in `Assets/Animation/StandIn`, all Humanoid with human motion:
  - `ManIdle` (461 frames) and `ManWalk` (71 frames), baked from the borrowed Generic clips;
  - `WomanIdle` and `WomanWalk`, the historical woman's own `idle.001` and `walk.001`.
- **The controllers.** Every standing person has `Man.controller` or `Woman.controller` in the same folder. The townsfolk walk in `SliceTownsfolk` and `SliceTownswoman`, repointed to the stand-ins.
- **The player.** Its controller's blend tree plays idle, then the man's walk at 1, 1.5 and 1.9 times. Its other states play the man's idle until A1.
- **No person plays a clip from a downloaded pack any more.** The dog keeps the Dogs Kit's clips until A4.

**A1 — the Mixamo base set (owner downloads, agent wires).** The clips of §6 marked A1. The agent imports them, checks them on all eight models, and builds one Animator Controller a role: the player, the townsfolk, the Wardens, the Cult Forces.

**A2 — combat from Mixamo (owner downloads, agent wires).** The clips of §6 marked A2, wired into the Combat Grid's actions; what is rough is listed for A3.

**A3 — Blender (agent, owner's review).** The clips Mixamo lacks (§6, marked A3), made on the Mixamo armature. The firearms come first, since a shot includes its loading (10.2 §5b): a pistol shot of 4 AP and a rifle shot of 6 AP, each aim, fire and reload in one action, and the step back. Blender runs in the background (`-b --factory-startup`), never on the owner's open file.

**A4 — the dog (agent, owner's choice of source).** Mixamo has no quadrupeds. Open for the owner ([99](../../../99%20Open%20Questions.md) VS-ANIM): (a) the agent animates the Tripo shepherd's own rig in Blender (walk, trot, run, idle, sit, lie, bark, sniff, bite, death); (b) a commercially licensed dog pack the owner buys; (c) Tripo's animal clips, if the owner's account offers them. The agent leans to (a): ten clips, one rig, full control.

**A5 — skirts (agent).** The three women's skirts smoothed between the thighs in Blender, so a run or a wide stance no longer tears them. Skirt bones with a jiggle are V2.

## 6. The clip list

The search words are Mixamo's usual names; the agent takes the closest clip, and the owner may swap any.

| Use | Mixamo search | Phase |
|---|---|---|
| Standing, men and women | Idle; Breathing Idle; Happy Idle | A1 |
| Walking | Walking (In Place) | A1 |
| Running, sprinting | Running; Fast Run | A1 |
| Turning on the spot | Left Turn 90; Right Turn 90 | A1 |
| Jumping | Jump | A1 |
| A Warden standing guard | Standing At Attention or Rifle Idle | A1 |
| Talking, listening | Talking; Arguing; Head Nod Yes | A1 |
| Greeting, pointing | Waving; Pointing | A1 |
| Sitting at a table, on a bench | Sitting Idle; Sit To Stand; Stand To Sit | A1 |
| Drinking in the tavern | Drinking | A1 |
| Sneaking in Stealth Mode | Sneak Walk; Crouched Walking; Crouch Idle | A1 |
| Gathering herbs and flax, Skinning | Picking Up; Kneeling; Gathering | A1 |
| Mining ore, felling a tree | Mining; Chopping | A1 |
| The blacksmith at the anvil; Item Creation | Hammering | A1 |
| Sword, axe, mace: idle, blow | Sword And Shield Idle; Sword And Shield Slash | A2 |
| Shield block | Sword And Shield Block | A2 |
| Greatsword, greataxe, maul | Great Sword Idle; Great Sword Slash | A2 |
| Dagger | Stabbing | A2 |
| Pistol aim and shot | Pistol Idle; Pistol Shoot | A2, loading A3 |
| Rifle aim and shot | Rifle Aiming Idle; Shooting | A2, loading A3 |
| Abilities and Infusion | Standing 1H Magic Attack; 2H Magic Area Attack; Spell Casting | A2 |
| Taking a hit, falling, dying | Hit Reaction; Dying; Death From The Front | A2 |
| Break Away, stepping back | Walking Backwards | A2 |
| Defensive Stance and Counterattack | Sword And Shield Block Idle, edited | A3 |
| Yield | Surrender; Kneeling, edited | A3 |
| Loading a flintlock pistol, a rifle | — | A3 |
| Дрёма, sleeping, waking | Sleeping Idle; Lying Down, edited | A3 |
| Brewing at the alchemy table | — | A3 |
| First Aid | Kneeling, edited | A3 |
| Picking a lock, stealing | — | A3 |

## 7. Checks

- The rig check (`SliceCharacterRigs.Check`) poses each new clip on all eight models, front and side, before it is wired.
- A role's controller is built by code, as the townsfolk's is (`SliceTownsfolk`), so it can be built again.
- The slice's PlayMode tests that walk the townsfolk and the player pass after each phase.

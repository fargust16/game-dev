# Animation Pipeline — Kevin Iglesias and Quaternius to Unity

Version: 0.13

Status: Draft — the owner's second direction of 2026-11-11 (§0) and its clip map (§8); §1–§7 are the withdrawn Mixamo and Blender pipeline

Created: 2026-11-10

Last Updated: 2026-11-12 (§12: AN5, swimming: the water marked, the swim and the fast swim, the float measured on the clip, the climb out onto a quay, no fight and no beast in water. Earlier: §11: AN4, the fight: one table for the code and the controllers, two weapons' right and left blows, a shield, shots through their loads, hits, knockback, deaths, the Defensive Stance, Yield, and each ability's cast chosen for S5b. Earlier: §10: AN3, the work played to its key frame for every Reaction Check: the swing, the patch, the anvil, the kneel and the pocket; §8: theft on the crouched reach, the keys of the lock's and brewing's clips measured. Earlier: §9: AN2, the player's eight-way movement facing the camera, each clip at the pace its feet measure, the turn on the spot and the woman player's own takes. Earlier: §8: the clip map of AN1: every use's clip from Kevin Iglesias' FREE packs or Quaternius' Universal Animation Library 1 and 2, each work clip's key frame measured, the Mixamo and Blender clips deleted. Earlier: §0: the owner's second direction: Kevin Iglesias back, Quaternius for the gaps, Mixamo and Blender out, the character facing the camera, checks at the key frame. Earlier: §5 *The play test of 2026-11-11*: one shared avatar for the Mixamo files and a T-pose rest for the Blender clips, the movement state machine, the jump's landing, the blow's timing per weapon, the blacksmith's hammer, anvil and stump, and the sources. Earlier: §5 A fight: the walk in a fight and the hit or the death at the blow's impact, shown beat by beat by S3a.5's `FightBeats`. Earlier: 2026-11-10: §3, §5, §6: A3's first four clips, Mining, Chopping, Hammering and Surrender, made in Blender by a script that is their source; the blacksmith hammers at the anvil; the rest of A3 waits for its actions. Earlier: §5: A1 and A2 built and wired to the slice's actions: the clip table, the player's paces and the jump's cut measured, the talk and the fight, A0's stand-ins retired; Mining, Chopping, Hammering and Surrender not found, to A3. Earlier: §6.1: the download list, 48 clips; the owner downloads from Mixamo. Earlier: §1, §2, §5: the owner's player bodies and the women Wardens' uniform, Humanoid by Unity's own mapping; the animations are the agent's work. Earlier: §5: A0 built, the four stand-in clips and their controllers, the player's locomotion on them. Earlier: first version: the downloaded clip packs leave the project, the slice's own clips come from Mixamo and Blender, the rigs checked, the phases and the clip list)

Authority: [11 Vertical Slice](../../../11%20Vertical%20Slice.md), [SLICE_ASSET_MAP.md](SLICE_ASSET_MAP.md) §1–§2, [VERTICAL_SLICE_IMPLEMENTATION_PLAN.md](VERTICAL_SLICE_IMPLEMENTATION_PLAN.md)

Related: [10.2 Combat System Draft](../../../10.2%20Combat%20System%20Draft.md) §5a–§5b, [10.3 Stealth and Detection](../../../10.3%20Stealth%20and%20Detection.md), [99 Open Questions](../../../99%20Open%20Questions.md) VS-ANIM

---

## 0. The owner's second direction (2026-11-11)

After the play test of S5a the owner found the Mixamo clips crooked and angular and unfit for the game, and decided:

- Kevin Iglesias' **Human Animations** come back: Human Basic Motions, Human Crafting Animations, Human Melee Animations, Human Soldier Animations and Human Spellcasting Animations, the FREE versions, in `Assets/Kevin Iglesias/Human Animations/`. Each pack's PDF gives the Standard Asset Store EULA / Fab Standard License: royalty-free, allowed in commercial games, no resale, no attribution needed. The reading of 2026-11-10 that they could not be used commercially (§1) was wrong.
- The FREE packs have eight-way walking, running and sprinting, turns, the jump and fall, one- and two-handed blows, a shield blow, pistols and rifles, five kinds of cast, hits and deaths, talking, gathering, mining, hammering, farming and fishing. They lack swimming, crouching, sitting, drinking, praying, greeting, bargaining, chopping, First Aid, skinning, sleeping, Yield and blocking. By the owner's choice, **Quaternius' Universal Animation Library** (CC0, Humanoid) fills these, downloaded by the agent from Quaternius' own page on the owner's word.
- The **Mixamo clips and the agent's Blender clips leave the project**, with their controllers; §1–§7 below stand as the history of that pipeline. Tripo's own clips stay stand-ins only.
- The character **faces the camera** and moves in eight directions ([16](../../../16%20Architecture%20Pivot.md) §2).
- Every Reaction Check comes at its animation's **key frame**, the world pausing for it, but theft ([07.1](../../../07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md) §8a).
- New with this pass: swimming ([10.1](../../../10.1%20Grid%20World%20and%20Environment%20Actions.md)), two weapons ([10.2](../../../10.2%20Combat%20System%20Draft.md) §5b), varied casts for the magic, and the walking townsfolk's Activity Spots ([11](../../../11%20Vertical%20Slice.md)).

The work is the plan's AN1–AN7 (`VERTICAL_SLICE_IMPLEMENTATION_PLAN.md`); AN1 writes the clip map for every use here as §8.

## 1. The owner's decision

The owner, 2026-11-10:

- The downloaded animation packs cannot be used commercially, so they leave the project: Kevin Iglesias' Human Animations (Human Basic Motions, Crafting, Melee, Soldier and Spellcasting FREE), already removed; DoubleL's RPG_Animations_Pack_FREE; and the clips of Bublisher's 3D Stylized Animated Dogs Kit. The owner removes them.
- Every clip of the player and the NPCs is our own. The beasts but the dog are animated already. The dog's clips are crooked and are to be replaced.
- New clips are made with Blender and Mixamo and used in Unity.
- With the player's two bodies and the women Wardens' uniform, the same day: the animations are the agent's work. The agent never signs in to Mixamo, so the owner downloads its clips (VS-ANIM (a), owner, 2026-11-10), by the list of §6.1.

## 2. What the rigs allow (measured, 2026-11-10)

`SliceCharacterRigs` (*GameDev → Slice → People*) checked the eight Tripo people the slice uses: the gentleman, the lady in the bustle dress, the poorer woman, the officer, the detective, the blacksmith, and the earlier townsman and woman.

- The six new models carry Mixamo's skeleton (`mixamorig:*`, 65 bones); the townsman and woman carry Tripo's own bone names (67). Each imports as a valid Unity **Humanoid**, the two older ones mapped by name (`Logs/SliceSurvey/rigs.txt`).
- A Humanoid clip plays on every one of them. The check posed all eight in the woman's Tripo idle and walk and in a one-handed attack; each pose came across clean, arms and legs where the clip puts them (`Logs/SliceSurvey/rig_check.png`).

- **The owner's second list** (2026-11-10): the player's man and woman and the women Wardens' uniform. The owner made their skeletons for Unity. They carry Tripo's own bone names, as the historical woman does (67 bones), and Unity maps each as a Humanoid itself. All three pose clean in the same clips, so the slice has eleven Humanoid people.

So **the characters need not go to Blender before the work starts.** A clip is made once on any Humanoid skeleton and Unity's retargeting carries it to every model. Mixamo's own skeleton matches six of the eight bone for bone.

Blender is needed for three things only:

1. clips Mixamo has not got, or has only roughly: loading a flintlock, the Defensive Stance, Yield, sleeping, brewing, First Aid, picking a lock, and a lifted gesture or two;
2. the dog, since Mixamo animates only people;
3. skirts: the three women's skirts are skinned to the legs, so a wide stance tears them. Their weights are smoothed in Blender, or skirt bones added (§5, A5).

**Tripo's own clips** (the woman's file holds sixteen: idle, walk, run, greet, sit, clap and more) are quick and need no work, but they cannot be edited, are made for one model, and give no control over a blow, a shot or a hand on a weapon. They stand in for everyday clips until Mixamo's arrive (A0) and are not used for combat or shooting.

## 3. Rules

- **Humanoid everywhere.** Every person imports as Humanoid with its own avatar (`CreateFromThisModel`); every clip imports as Humanoid. No Generic person clip remains.
- **In place.** Clips are in place; the code moves the character (`PlayerLocomotion`, `TownsfolkWalker`, the Combat Grid's steps). Mixamo's *In Place* box is ticked where it has one.
- **One take, every model.** A clip is downloaded *Without Skin* once, not once a model. The agent checks it on all eleven models with the rig check before it is wired in.
- **Mixamo's licence.** Mixamo's characters and clips may be used royalty-free in commercial games; they may not be sold or shared as raw files. The downloads live in `Assets/Animation/Mixamo/`, tracked in git (no skin, small). The owner signs in to Mixamo and downloads; the agent never signs in.
- **Blender's clips** are authored on one armature, the Mixamo files' own (the player woman's body, Tripo's bone names), in `tools/blender/animation/`, and exported as FBX to `Assets/Animation/Blender/`, one file a clip named by its use. A clip built by a script has the script as its source, tracked in git: it builds every frame from the Mixamo files, so it keeps no `.blend`. Where a hand-keyed clip's `.blend` lives (outside the repo or in git LFS) stays the owner's choice (99 VS-ANIM).
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
- **The player.** Its controller's blend tree plays idle, then the man's walk at 1, 1.5 and 1.9 times. Its other states play the man's idle until A1. The man's body plays it as it is; the woman's plays `OverworldPlayerAnimator_Woman`, an override with the woman's idle and walk.
- **No person plays a clip from a downloaded pack any more.** The dog keeps the Dogs Kit's clips until A4.

*A0 retired with A1, 2026-11-10:* the four stand-in clips, their folder and the woman player's override are deleted; `Man.controller` and `Woman.controller` moved to `Assets/Animation/Controllers`.

**A1 — the Mixamo base set (owner downloads, agent wires).** The clips of §6 marked A1. The agent imports them, checks them on all eleven models, and builds one Animator Controller a role: the player, the townsfolk, the Wardens, the Cult Forces.

**A2 — combat from Mixamo (owner downloads, agent wires).** The clips of §6 marked A2, wired into the Combat Grid's actions; what is rough is listed for A3.

*A1 and A2 wired, owner's word of 2026-11-10* ("apply every clip to its action"; `SliceMixamo`, run by `SlicePeopleModels.Run`; report `Logs/SliceSurvey/people.txt`). The owner downloaded 61 files on the player woman's body; four of §6.1 were not found and go to A3: Mining, Chopping and Hammering (A1) and Surrender (Yield).

- **Import** (`SliceMixamo.Import`): each file as Humanoid with its own avatar and one clip named by its use (table below). Root rotation, height and ground position are baked into the pose as the file has them, and the cycles have loop time and loop pose. Every clip imported as Humanoid, with 130 muscle and root curves. The jumps keep their feet on the root instead (height based upon Feet), since the player's own jump lifts the body.
- **The rig check** posed the idle, the walk, the talk, the sword's blow, the rifle's aim and the end of a death on all eleven models, front and side (`Logs/SliceSurvey/rig_check.png`). It found no wrong mapping. The skirts still tear in a wide stride (A5).
- **Wired now**, to the actions the slice has:
  - **The player's locomotion:** idle, walk, run and sprint in the blend at Speed 0, 1, 2 and 3. The old blend's thresholds ran from 0 to 1, so the run had played the sprint's child.
    - Each clip's pace, measured from the feet on the man and the woman: walk 1.65 and 1.25 m/s, run 2.15 and 1.67, sprint 4.05 and 3.99. Mixamo's in-place strides are shorter than the player's speeds, which would play the clips ×1.38, ×2.36 and ×1.87, six or seven steps a second at a run.
    - So each clip plays at most ×1.3 (authoring default), the step keeps a human cadence, and the feet slide the rest.
    - Both bodies play one controller. The woman's override is gone, since every clip is the same for men and women.
  - **The jump:** the still jump cut where the feet leave the ground and meet it again, measured on the player man (feet over 0.05 m, authoring default): frames 24 and 39 of 65.
    - JumpUp, the push before the takeoff (0.2 s), plays as the jump begins.
    - JumpAir, the flight (0.5 s), plays ×0.68 to fill PlayerLocomotion's 0.73 s in the air, and holds its last pose until the ground.
    - JumpLand plays out the landing.
  - **The townsfolk:** the walk and the idle, each walker's pace measured again from the new walk: 1.18 to 1.57 m/s.
  - **Standing people:** the idle. A Warden stands at attention (`Warden.controller`), and a woman Warden in the women's pose, a hand on the hip (`WardenWoman.controller`). The people's controllers are in `Assets/Animation/Controllers`.
  - **Talking:** a person the player talks to plays the talk (`Talking 1`) while the conversation is open (`PersonTalk`, the Animator's `Talking`).
  - **A fight** (`SliceConfront`, on the turns' `Struck` and `Walked`, shown beat by beat by `FightBeats` since S3a.5):
    - A fighter walks its path cell by cell in the walk clip, the player's locomotion at a walk or a person's own walk, and goes back into its guard.
    - Every fighter takes its guard as the fight begins and leaves it as it ends.
    - The guard and the blow follow the weapon, on the controllers' `Weapon` 1–5: sword, axe and mace with the sword and shield's clips; greatsword, greataxe and maul; the dagger, a stab from the sword's guard, as Mixamo has no dagger guard; pistol; rifle.
    - The attacker turns to its target. The target plays its hit when the blow takes HP, and its death (`Dying`) when it goes down, at the blow's impact, 0.35 s into it.
    - A body with a death clip no longer turns on its back; one without, as a beast's, still does.
- **Kept for their systems**, imported and named but not wired, since the slice has no such action yet: turns on the spot, the gestures (wave, point, nod, argue), sitting, drinking, crouching and sneaking, picking up, kneeling and gathering, the magic attacks and casting, the shield block, the step back, and lying down, sleeping and getting up. Each is wired with the system that brings its action.

| Use | Mixamo file | Loop |
|---|---|---|
| Idle | Breathing Idle | yes |
| IdleHappy | Happy Idle | yes |
| Walk | Walking In Place | yes |
| Run | Running In Place | yes |
| Sprint | Fast Run In Place | yes |
| TurnLeft, TurnRight | Left Turn, Right Turn | no |
| Jump, JumpForward, JumpBack | Jump Still In Place, Jump Forward In Place, Jump Backward In Place | no |
| JumpUp, JumpAir, JumpLand | Jump Still In Place, cut at the measured takeoff and landing | no |
| Attention, AttentionWoman | Male / Female Standing At Attention Pose | yes |
| Talk, Talk2 | Talking 1, Talking 2 | yes |
| Argue, Argue2 | Standing Arguing 1, 2 | yes |
| Nod, Wave, Wave2, Point | Head Nod Yes, Waving 1, 2, Pointing | no |
| Sit, SitAsk, SitGround | Sitting Idle, Sitting Asking Attention, Sitting On The Ground Idle | yes |
| StandToSit, SitToStand | Stand To Sit, Sit To Stand | no |
| Drink | Drinking | no |
| Sneak, CrouchWalk, Crouch | Sneak Walk In Place, Crouched Walking In Place, Male Crouch Idle | yes |
| StandToCrouch, CrouchToStand | Standing To Crouched, Crouch To Stand | no |
| PickUp | Picking Up From The Ground | no |
| Kneel, KneelOne, Gather | Kneeling Idle, On One Kneel Idle, Gathering Objects | yes |
| StartWalking | Start Walking | no |
| SwordIdle, SwordSlash | Sword And Shield Idle, Slash | idle yes |
| BlockStart, BlockIdle, BlockEnd | Sword And Shield Block Start, Idle, End | idle yes |
| GreatswordIdle, GreatswordSlash | Great Sword Idle, Slash | idle yes |
| Stab | Stabbing | no |
| PistolIdle, PistolShot | Pistol Idle, Shooting Pistol | idle yes |
| RifleIdle, RifleShot | Rifle Aiming Idle, Shooting Rifle | idle yes |
| Cast1H, Cast2H, Casting | Standing 1H Magic Attack 01, Standing 2H Magic Area Attack 02, Spell Casting | no |
| Hit | Hit Reaction | no |
| Die, DieBack, DieFront | Dying, Dying Backwards, Death From The Front | no |
| StepBack | Walking Backwards In Place | yes |
| LieDown, Sleep, StandUp, GetUp | Lying Down, Laying Sleeping Idle, Stand Up, Standing Up From The Ground | sleep yes |

Fast Running In Place, a second sprint, is not used.

**A3 — Blender (agent, owner's review).** The clips Mixamo lacks (§6, marked A3), made on the Mixamo armature. The firearms come first, since a shot includes its loading (10.2 §5b): a pistol shot of 4 AP and a rifle shot of 6 AP, each aim, fire and reload in one action, and the step back. Blender runs in the background (`-b --factory-startup`), never on the owner's open file.

*A3's first four, owner's word of 2026-11-10* ("make A3", for the four clips not found on Mixamo; `tools/blender/animation/a3_clips.py`, its pose sheets and report written beside its exports when asked):

- **Mine** (1.47 s, loops): the pick raised over the head, struck at the ground ahead with the body bent at the hips, pulled free and lifted past the right shoulder.
- **Chop** (1.33 s, loops): the axe drawn back over the right shoulder, driven into a trunk ahead at the waist in a diagonal blow to the left.
- **Hammer** (1.0 s, loops): the hammer raised beside the head, elbow out, brought down on the anvil with a rebound; the left hand holds the tongs still.
- **Yield** (2.2 s, holds its last frame): the hands come up in front and open beside the head, palms forward; the feet step back one by one and the body goes down on both knees; the head bows.
- **How they are made.** The body is Breathing Idle's; the two-handed grip is Great Sword Idle's hands, the hammer's Sword And Shield Idle's, the kneel Kneeling Idle's. Each clip is a few key poses joined by smooth curves. Arms and legs reach their marks by two-bone IK, each elbow and knee bending about its own hinge, measured from Great Sword Idle's elbows and Kneeling Idle's knees. The feet stay where they stood.
- **Measured:** the Mixamo files' body stands about 1 m, its shoulders 0.78 m, so the poses are written for a body whose shoulders stand 1.40 m and scaled by 0.56. On that body the arm reaches 0.50 m from shoulder to wrist, and every hand's mark stays within it: at the key poses each hand ends within 0.04 m of its mark. The marks, the bends and the timings are authoring defaults.
- **Checked:** the script's drawn sheets, front, side and above, by the agent; then the rig check on all eleven models, the pick striking the ground, the axe drawn back, the hammer raised and the end of the surrender, with no wrong mapping. `SliceMixamo` imports them as it imports Mixamo's files, Humanoid and in place, 130 curves each.
- **Wired:** the blacksmith at Bawling Row's anvil hammers as its idle (`Blacksmith.controller`, the standing person's controller with Hammer for its idle). Mine, Chop and Yield are imported and checked but not yet played: the slice has no ore or wood gathering yet, and Confront's intents are Move, Attack and End Turn, with no Yield. Each is wired when its action is built.
- **No tool is held**, except the blacksmith's hammer since the play test of 2026-11-11 (below). The hands are posed as if gripping a handle. A pick or an axe in the hand is a prop the code attaches to the hand, built with gathering and Item Creation.
- **Left in A3:** the firearms' loading, the Defensive Stance, Дрёма and sleep, brewing, First Aid, picking a lock. Each is made when its action is built, the firearms first.

*The play test of 2026-11-11.* The owner found that people's feet did not touch the ground, the walk and run looked crooked, the jump played every other time, a blow's hit came late, stealth stood upright, and the blacksmith's arm bent wrongly as he struck air. The agent's fixes, under the owner's direction to fix the animations and make movement crisp, are authoring defaults:

- **The cause: no rest pose.** The Mixamo files were downloaded Without Skin, so each had no bind pose of its own. An avatar made from such a file takes its first frame for its rest pose, and every frame then came across offset by that first frame. Measured by `SliceAnimationCheck`: the walk's knees stood bent at 115° in its "rest", the run's at 79°. So knees and ankles bent wrongly on every body, and the feet floated.
- **The fix: one shared avatar.** Every Mixamo file now copies the player woman's avatar, made from her skin in a true T-pose (`SliceMixamo.SourceAvatar`). The files carry her skeleton bone for bone, so the copy holds. Every clip's height is based on its feet, so the lower foot meets the root on any body. Foot IK is on in every standing state.
- **The Blender clips** keep an avatar of their own, because their bones' lengths differ from hers by up to 0.4 m. Their rest was Breathing Idle's first pose, arms down, so the blacksmith's arm turned by the difference. Each bone of that rest is now pointed at its child as her T-pose points it (Unity's Enforce T-Pose, by direction), and the result is written as the avatar's skeleton.
- **Movement, an explicit state machine** (`PlayerLocomotion`), as responsive third-person control commonly does it:
  - the code owns the state (Locomotion, Crouch, Fall, JumpLand) and cross-fades the moment it changes, never waiting for a clip to end;
  - velocity rises at 28 m/s² and falls at 38 m/s², so a run starts in about 0.16 s and stops in 0.12 s;
  - the body turns at 900° a second;
  - a ground probe of 0.35 m keeps the body on slopes and steps down;
  - coyote time is 0.12 s and a jump buffer 0.15 s, so a jump pressed just before landing or just after an edge still counts.
- **The jump.** The landing played the file's whole 26 frames and held the walk back. JumpLand now keeps 10 frames (0.33 s). It plays only after 0.45 s in the air, holds 0.2 s with no key pressed, and any key leaves it at once.
- **Stealth** plays the Crouch blend: crouched idle and the sneak, at the sneak's measured pace.
- **The blow's timing** (`FightBeats`): the attack cross-fades in 0.08 s. Its impact lands when each weapon's clip reaches its target, measured in seconds into the clip: the sword's slash 0.57, the greatsword's 0.90, the stab 0.53, the pistol shot 0.37, the rifle shot 0.23. Before, every weapon used one 0.35 s, and blow and hit went through the guard's triggers and their transitions, so the hit showed late. The struck body cross-fades into its hit in 0.04 s, entered 0.10 s into the clip, just before its first motion at 0.13 s.
- **The townsfolk** stand still while the clock is held and walk each tick's time at ×6 after a round (10.2 §5a). So does the street dog.
- **The blacksmith** (`SliceBlacksmith`, report `Logs/SliceSurvey/blacksmith.txt`, close-up `blacksmith.png`):
  - **The Hammer clip** was remade in `a3_clips.py`. The fist is turned so its knuckle line points where the blow goes, and the fingers follow the forearm, so the wrist bends no more than 30°. Before the rework it bent up to 79°.
  - **The hammer.** He holds the Medieval Market's hand hammer through its kit wrapper (`SmithHammer`), scaled from 0.63 m to 0.38 m and held a quarter of the way up the handle. It is set on his body at the blow, the frame his right wrist is lowest. There the handle runs forward from the fist, the head 15° lower, its face down on the iron. The wrapper's block is turned off, since a held hammer is carried, not placed.
  - **The anvil.** The anvil is the pack's plain one, so no second hammer leans on it. It stands on an oak stump that brings its face to where the blow comes down, on the kit's 0.1 m step. A hot bar of iron lies on its face.
  - **Not exact:** the handle runs 72° off the line of his knuckles, along the fingers more than across the palm. It reads right at play distance; a closer grip needs a hand pose of its own (A3).
- **Sources.** Mixamo stays the base: its clips are free for commercial use in a game, and the owner downloads them, as the agent never signs in. What was wrong was the import, not the source. For clips Mixamo lacks, Quaternius' *Universal Animation Library* (CC0, Humanoid) is the agent's proposal beside Blender. Taking it in needs the owner's word, as any new pack does ([SLICE_ASSET_MAP.md](SLICE_ASSET_MAP.md)).

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
| Mining ore, felling a tree | Mining; Chopping | A3, made (not on Mixamo) |
| The blacksmith at the anvil; Item Creation | Hammering | A3, made (not on Mixamo) |
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
| Yield | Surrender; Kneeling, edited | A3, made |
| Loading a flintlock pistol, a rifle | — | A3 |
| Дрёма, sleeping, waking | Sleeping Idle; Lying Down, edited | A3 |
| Brewing at the alchemy table | — | A3 |
| First Aid | Kneeling, edited | A3 |
| Picking a lock, stealing | — | A3 |

### 6.1 The download list (owner, 2026-11-10)

The owner downloads; the agent imports, renames each clip by its use, checks it on all eleven models and wires it. Settings as §4: FBX for Unity, Without Skin, 30 fps, no keyframe reduction, *In Place* ticked where Mixamo offers it. The files go to `unity/Assets/Animation/Mixamo/` under Mixamo's own names. Where a name is not found, the closest clip of the same kind does; the agent notes the swap.

**A1, everyday (27):**

1. Breathing Idle
2. Happy Idle
3. Walking, in place
4. Running, in place
5. Fast Run, in place
6. Left Turn 90
7. Right Turn 90
8. Jump, in place where offered
9. Standing At Attention
10. Talking
11. Arguing
12. Head Nod Yes
13. Waving
14. Pointing
15. Sitting Idle
16. Stand To Sit
17. Sit To Stand
18. Drinking
19. Sneak Walk, in place
20. Crouched Walking, in place
21. Crouch Idle
22. Picking Up
23. Kneeling Idle
24. Gathering Objects
25. Mining
26. Chopping
27. Hammering

**A2, combat (17):**

28. Sword And Shield Idle
29. Sword And Shield Slash
30. Sword And Shield Block
31. Great Sword Idle
32. Great Sword Slash
33. Stabbing
34. Pistol Idle
35. Pistol Shoot
36. Rifle Aiming Idle
37. Shooting (rifle)
38. Standing 1H Magic Attack 01
39. Standing 2H Magic Area Attack 01
40. Spell Casting
41. Hit Reaction
42. Dying
43. Death From The Front
44. Walking Backwards, in place

**Bases for A3's edits (4):**

45. Sword And Shield Block Idle: the Defensive Stance
46. Surrender: Yield
47. Sleeping Idle: Дрёма and sleep
48. Lying Down: lying down and waking

## 7. Checks

- The rig check (`SliceCharacterRigs.Check`) poses each new clip on all eleven models, front and side, before it is wired.
- A role's controller is built by code, as the townsfolk's is (`SliceTownsfolk`), so it can be built again.
- The slice's PlayMode tests that walk the townsfolk and the player pass after each phase.

## 8. The clip map (AN1, 2026-11-12)

`SliceClips` holds this table in code, a use to its clip, and builds the controllers from it; `SliceClipSurvey` measures every clip on the player man (`Logs/SliceSurvey/clip_survey.txt`). The Mixamo and Blender clips, their importer and `tools/blender/animation/a3_clips.py` are deleted.

**Sources.**

- **Kevin Iglesias' Human Animations**, the FREE packs (Basic Motions, Crafting, Melee, Soldier, Spellcasting), in `Assets/Kevin Iglesias/Human Animations/Animations/`: a man's take under `Male/` as `HumanM@…`, a woman's under `Female/` as `HumanF@…`, the same 189 clips each. They keep the pack's own import: Humanoid on the pack's avatar, in place. A woman plays the women's takes.
- **Quaternius' Universal Animation Library 1 and 2**, the Standard tier, CC0, from Quaternius' own itch.io pages (the free download, 15.9 MB and 18.7 MB): the root-motion-free Unity files `Assets/Quaternius/UAL1_Standard.fbx` and `UAL2_Standard.fbx`, 43 takes each. `SliceClips` imports each as Humanoid with an avatar of its own, made from the mannequin it carries, and cuts out each take used under its use's name, in place. Both packs are vendor folders outside git; a fresh copy is downloaded again from the same pages.

**The map.** *Key* is a work clip's key frame, where its Reaction Check comes (07.1 §8a), measured by `SliceClipSurvey` (s into the clip, authoring measurement).

| Use | Clip | Where it plays | Key |
|---|---|---|---|
| Idle, Idle2, Idle3 | K Idle01–03 | standing people, the player; the townsfolk vary among them (AN6) | |
| Attention | K MilitaryIdle01 | the Wardens, men and women | |
| Walk and its seven directions | K Walk01_* | the player's eight-way walk (AN2), every walker | |
| Run and its seven directions | K Run01_* | the player's eight-way run (AN2) | |
| Sprint, SprintForwardLeft/Right, SprintLeft/Right | K Sprint01_* | the player's sprint; the pack has no sprint backward, so a sprint backward is a run | |
| TurnLeft, TurnRight | K Turn01_* | turning on the spot (AN2) | |
| JumpUp, Fall, JumpLand | K Jump01 Begin, Fall01, Jump01 Land | the jump and any fall | |
| Crouch, Sneak | Q1 Crouch_Idle, Crouch_Fwd | Stealth Mode | |
| Swim, SwimIdle | Q1 Swim_Fwd, Swim_Idle | swimming (AN5); the fast swim plays Swim faster | the chest 0.12 m under the root |
| ClimbOut | Q2 ClimbUp_1m | climbing out of the water onto a bank (AN5) | 0.67 s |
| WalkFormal, Carry | Q1 Walk_Formal, Q2 Walk_Carry | gentlemen and porters among the walkers (AN6) | |
| Guard | K CombatIdle01 | a fighter with no weapon | |
| Guard1H, Attack1H, Attack1HLeft | K CombatIdle1H01, Attack1H01_R, _L | sword, axe, mace and dagger (Weapon 1 and 3); two weapons strike right and left in turn (AN4) | |
| Guard2H, Attack2H | K CombatIdle2H01, Attack2H01 | greatsword, greataxe and maul (Weapon 2) | |
| ShieldAttack, ShieldGuard, ShieldBash | K AttackShield01; Q2 Idle_Shield, Shield_OneShot | a weapon and a shield (AN4) | |
| Block | Q2 Sword_Block | the Defensive Stance and its Counterattack (AN4) | |
| PistolAim, PistolShot, PistolLoad | K Gun_Aim01, _Shoot01, Gun_Reload01 | the pistol (Weapon 4): its shot includes loading (10.2 §5b), so the shot plays the shot and then the load | |
| RifleAim, RifleShot, RifleLoad | K Rifle_Aim01, _Shoot01, Rifle_Reload01 | the rifle (Weapon 5), as the pistol | |
| Hit, Hit2, GuardHit, HitChest, HitHead, Knockback | K Damage01–02, CombatDamage01; Q1 Hit_Chest, Hit_Head; Q2 Hit_Knockback | hits | |
| Die, Die2, Die3 | K Death01–03 | deaths | |
| GetUp | Q2 LayToIdle | getting up from the ground, waking from Дрёма (AN4) | |
| CastIdle, CastEnter, CastExit, CastHit | K CastingIdle01, Enter01, Exit01, CastingDamage01 | a mage's guard and its hit | |
| CastCall, CastDirectLeft, CastDirectRight, CastDirect2H, CastOmni | K MagicAttack* | each ability its own cast (AN4) | |
| SpellEnter, SpellIdle, SpellShot, SpellExit | Q1 Spell_Simple_* | a quick cast (AN4) | |
| Talk, TalkIdle, SitTalk | K Talk01; Q1 Idle_Talking, Sitting_Talking | a conversation; townsfolk talking together (AN6) | |
| Nod, ShakeHead | Q2 Yes, Idle_No | a greeting in passing; bargaining at a stall (AN6) | |
| FoldArms, Lean, LeanCall, Lantern | Q2 Idle_FoldArms, Idle_Rail, Idle_Rail_Call, Idle_Lantern | townsfolk standing about, a night watchman (AN6) | |
| Sit, StandToSit, SitToStand | Q1 Sitting_* | benches, the tavern's chairs, the chapel's pews (AN6) | |
| Drink | Q2 Consume | the tavern (AN6) | |
| Gather | K Gathering01 | reaching for goods at a stall (AN6) | |
| Chop | Q2 TreeChopping_Loop | felling a tree | the right hand furthest out, 0.53 of 0.97 s |
| Mine | K MiningOneHand01_R Ground | an iron vein | the fist lowest, 0.40 of 0.97 s |
| HammerDown, HammerUp | K MiningOneHand01_R Ground, cut 0–0.38 s and 0.81–0.97 s | the blacksmith at the anvil, the blow and its rebound in turn; Item Creation, the check on the last blow | the end of HammerDown, where the hammer meets the anvil's face, 0.37 s as the cut reads |
| Harvest | Q2 Farm_Harvest | a herb or flax patch, one check | the hips begin to rise, 1.27 of 2.50 s |
| Kneel | Q1 Fixing_Kneeling | Skinning and First Aid | the hips begin to rise, 4.30 of 5.20 s |
| Gather2 | K Gathering02 | theft: the character steals in Stealth Mode, so the crouched hand reaches into the pocket (AN3) | the left hand furthest out, 0.60 of 1.00 s |
| Interact, PickUp, OpenChest | Q1 Interact, PickUp_Table; Q2 Chest_Open | a lock and brewing, not built yet; a Stash | the left hand furthest out: Interact 0.77 of 2.00 s, PickUp 0.33 of 0.83 s (measured in AN3) |
| Gather3, MineWall | K | spare: crouched gathering, a pick at a wall | |

**Choices the packs forced** (the agent's, under the owner's grant of 2026-11-11):

- *The anvil.* Kevin Iglesias' hammering kneels and strikes the ground, the fist down to 0.28 m, under any anvil's face. At the anvil the smith and the player strike with the pick's blow from above instead, cut where the anvil stops it: the blow down to the hammer meeting the face, 0.38 s, and the rebound from 0.81 s, the lower part left out. `SliceBlacksmith` measures the armed smith and sets the anvil on an oak stump 0.10 m high, its face 0.85 m over the ground, the hammer coming down to 0.90 m (`Logs/SliceSurvey/blacksmith.txt`). *Weak point:* the blow is the pick's blow at the ground, so the smith drops into a deep crouch before it lands; neither pack has a standing blow from above. A standing smith's blow wants a clip the packs lack, and is listed for the owner.
- *Gathering.* Kevin Iglesias' gathering clips are loops, one at chest height and two crouched, with no stooping or rising. A herb patch, one check, wants the whole motion, so Quaternius' harvest plays: it stoops, picks, and stands.
- *What neither pack has:* a kneeling prayer, a wave, a surrender and a sleeping loop. The faithful sit in the pews; a nod greets; Yield holds the crouch; a sleeper lies in the last pose of a death clip, and GetUp brings it up.
- *Not used:* Kevin Iglesias' fishing, ploughing, polearm, bazooka, grenade, assault rifle and dual pistols, and Quaternius' dance, driving, punches, roll, slides, ninja jumps, zombies, phone and climbing: no slice action, or out of the Victorian style.
- *The survey's scale.* `SliceClipSurvey` fits the model to each clip's first pose, so a clip that starts crouched, sitting or lying reads up to a third larger; its times are exact.
- *The survey's way.* Since AN3 it reads the body's way across the thighs, as a bone's own axes are the rig's, and reports the left hand and either hand's height too: Quaternius' Interact, PickUp_Table, Consume and Chest_Open work with the left hand.

## 9. Movement (AN2, 2026-11-12)

- **Facing the camera** (owner, 2026-11-11; 16 §2). `PlayerLocomotion` turns the body to the camera's forward while a move key is held, at 900°/s, and moves it where the keys point; standing still, it turns on the spot once the camera is 60° away (authoring default), at 240°/s, playing Kevin Iglesias' Turn01 left or right. In the air or landing with no key held the body keeps its facing. In Stealth Mode, and in the water (AN5), the body turns to where it goes, as the sneak and the swim have forward clips only. Shift sprints within 100° of the camera's forward; further back it runs, as the pack has no sprint backward.
- **The blend.** The player's `Locomotion` is a 2D freeform directional blend on MoveX and MoveZ, the body's own velocity on the scale 0–3: the idle at the centre, the eight walks at 1, the eight runs at 2, the five sprints at 3, each at the direction its name gives. `Speed` stays for the crouch's blend and the controllers that read it. The fight's walk sets MoveZ with Speed.
- **The pace of the feet.** `SlicePeopleModels` measures every moving clip on both player bodies: how fast the planted feet slide (each foot within 3 cm of its lowest point, the median of each component), and plays each clip at `speed / pace`, held between ×0.6 and ×1.3. Kevin Iglesias' clips match the game's speeds within 10 % in every direction: walks 2.0–2.3 m/s against 2.0, runs 4.0–4.7 against 4.5, sprints 7.2–8.3 against 7.5, so every clip plays between ×0.93 and ×1.09 (`Logs/people_models.txt`). The feet's own direction reads up to 55° off the name as the hips swing, so the names place the clips, not the measure.
- **The turn.** Kevin Iglesias' Turn01 steps in place with no root rotation of its own (measured 0°/s), so the code turns the body and the clip plays at ×1. The import leaves any turn's root rotation out of the pose, and a take that loops only in one sex is set as the table says (Idle03, Turn01, Talk01, Gathering01–03).
- **The woman player.** `OverworldPlayerAnimator_Woman.overrideController` is the player's controller with every Kevin Iglesias clip in its women's take; the woman player's prefab plays it. Quaternius' clips have one take.
- **Checked by** `SlicePeopleTests.ThePlayerMovesInEightDirectionsFacingTheCamera` and `MovementSmokeTests.ThePlayerStepsSidewaysAndBacksAwayFacingTheCamera` (right and back), with the door and stair walks.

## 10. Work by key frame (AN3, 2026-11-12)

- **One component for all of it.** `PlayerWork` (beside the Reaction Check on the world's driver) plays a `WorkStroke` on the player's own controller. It turns the character to the work and steps it to its place in 0.3 s, plays any lead-in clips whole, plays the stroke to its key frame, stops the clip there and shows the bar, the world paused but for theft; after the check the clip, or the clip that follows it, plays to its end. The character is held while it works and walks again when the work is over. The places are the thing's own edge, read from its colliders, plus the clip's reach: a swing 0.75 m, a patch or a carcass 0.35 m, the anvil 0.45 m, and a pocket 0.6 m from the person's pivot (authoring defaults).
- **The key frames** (§8, `SliceClipSurvey`, `Logs/SliceSurvey/clip_survey.txt`): Chop 0.53 s, Mine 0.40 s, HammerDown 0.37 s, its end, Harvest 1.27 s, Kneel 4.30 s, Gather2 0.60 s. `SliceClips.Player` gives the player's controller every work clip, `PlayerWork.Clips`, and the woman's controller takes their women's takes.
- **Each action.**
  - *A tree or a vein:* one check a swing, the next swing once the last has played out, while the gathering lasts.
  - *A herb or flax patch:* the whole harvest once, its check as the hips begin to rise; the patch is empty after it whatever came of it, and the rise plays out though the gathering has ended.
  - *The anvil:* the piece's ticks pass as the character steps up, then two blows with their rebounds, the last blow to the anvil's face, its check, its rebound, and the page again with what came of it.
  - *A carcass and a wound:* the character kneels, the ticks pass as it kneels, the check comes at 4.30 s, and it rises.
  - *A pocket:* the crouched reach; the world runs on, and the try is called off, not spent, once the person stands 1 m from where it stood.
- **Breaking off.** A move key breaks the work off before its check at a tree, a vein, a patch or a pocket. Work whose ticks have passed, at the anvil, over a carcass or a wound, is steady and plays to its end.
- **In a fight** First Aid's bar shows as soon as it is chosen and its AP are in hand, the treatment then given with its result (`TurnIntent.Treat` carries it); Space for the bar does not also end the turn. No kneel is played (open for the owner, 99 VS-ANIM).
- **Not built yet:** a lock and brewing; their clips' key frames are in §8.
- **Checked by** EditMode `GatheringTests`, `RemainsTests`, `FirstAidTests`, `EnvironmentConditionTests` and `SlicePeopleTests.ThePlayerWorksEachClipToItsKeyFrame`; PlayMode `GatheringPlayTests` (a tree and a herb patch), `TheftPlayTests` (a purse lifted, a theft noticed, a try called off), `BeastPlayTests` (skinning), `ShopPlayTests` (the anvil) and `S5aAcceptancePlayTests`.

## 11. Combat (AN4, 2026-11-12)

- **One table for the code and the controllers.** `FightBeats` holds each Weapon value's guard, blow and load, the hits and the deaths, and `SliceClips` builds every fighter's blend trees from those tables, so a clip changed in one place is changed in both. The Weapon values: 1 a sword, an axe or a mace; 2 a two-handed weapon; 3 a dagger; 4 a pistol; 5 a rifle; 6 a weapon and a shield; 7 and 8 two weapons, the right hand's blow and the left's.
- **The guards and the blows** (§8): Guard1H and Attack1H for one hand, Guard2H and Attack2H for two, ShieldGuard and ShieldAttack for a shield, PistolAim and RifleAim with their shots. Two weapons guard as one hand and strike with Attack1H for the main hand and Attack1HLeft for the other, the hand the turns chose ([10.2](../../../10.2%20Combat%20System%20Draft.md) §5b *Two weapons*). A shot plays its shot and then its load, since the shot includes loading, and the beat holds until the load is over.
- **Measured** by `SliceAnimationCheck` on the player man (`Logs/SliceSurvey/animation_check.txt`): a blow's contact is the frame the striking hand is furthest out ahead of the chest, the left hand for the left's blow; a shot's is the frame the hand jumps back fastest; a hit's onset is the first frame the head moves faster than 0.6 m/s, entered 0.03 s before.

  | Weapon value | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
  |---|---:|---:|---:|---:|---:|---:|---:|---:|
  | Contact, s | 0.33 | 0.57 | 0.33 | 0.03 | 0.10 | 0.37 | 0.33 | 0.40 |
  | The beat holds after it, s | 0.65 | 0.65 | 0.65 | 2.47 | 2.40 | 0.65 | 0.65 | 0.65 |

  Hits: Hit 0.14 s, HitChest 0.04 s, HitHead 0, Hit2 0, GuardHit 0.04 s. The Block clip's right hand is highest over the chest at 0.63 s; the knockback moves from its first frame; GetUp lasts 1.53 s.
- **Hits.** A plain hit plays Hit, HitChest and HitHead in turn, a critical hit Hit2, a hit through a guard that failed GuardHit, each entered at its onset at the blow's contact. A parry or a block that holds plays Block so that its height, 0.63 s in, meets the blow. A stun knocks the fighter down with Knockback, and it gets up with GetUp.
- **Deaths.** Die, Die2 and Die3 in turn. A body brought back from Down gets up with GetUp.
- **The Defensive Stance.** A melee fighter with no shield in the stance holds the Block clip still at its height until its next turn, and after its Counterattack takes the stance again; a shield's guard and a firearm's aim are their stances already.
- **Yield.** One who asks for mercy crouches, Quaternius' Crouch_Idle, in its turn among the beats, until it is spared or the fight ends; neither pack has a surrender.
- **Beats.** A pose, a set changed, a stance taken or mercy asked, waits its turn among the walks and blows, so a body never changes pose in the middle of another's blow.
- **Casts, for S5b.** The abilities are not in the game yet, so no cast is wired. Each has its clip chosen now, from §8:

  | Arcana | Ability | Cast |
  |---|---|---|
  | Жрица | Дрёма | CastDirectRight at the target; the sleeper lies in the last pose of Die and gets up with GetUp when damage or an ally wakes it |
  | Жрица | Укол разума | SpellShot, the quick cast, at the target |
  | Жрица | Завеса | CastOmni |
  | Жрица | Чужой сон | Kneel's touch, its check at 4.30 s as First Aid's |
  | Император | Не сдвинуть | CastEnter into CastIdle, held until its next turn |
  | Император | Стой! | CastCall at the target |
  | Император | Столп | CastOmni |
  | Император | Сокрушение | CastDirect2H as the blow, its contact to be measured |

  Опора, Родство, Чуткое ухо and Внушение are passive or spoken and play no cast.
- **Not built yet:** the casts above and Дрёма's sleep; a key for the player to choose the hand of a blow (99 VS-ANIM).
- **Checked by** EditMode `TwoWeaponsTests` (a pair's rules, its parry and weight, a sword and a dagger striking 4 + 3 + 3 in 10 AP, two swords taking turns, a hand asked for), `OutfitTests.ASecondWeaponGoesInTheOtherHandBesideAOneHandedOne` and `SlicePeopleTests.AStandingPersonTalksAndFightsInKevinIglesiasClips` (every fight state and blend from `FightBeats`' tables); PlayMode `FightTurnPlayTests`, `LeavingFightPlayTests`, `BeastPlayTests`, `S3aAcceptanceTests` and `S5aAcceptancePlayTests`.

## 12. Swimming (AN5, 2026-11-12)

- **Where.** A `SwimWater` marks a water surface: its level is its height, over its renderer's ground plan, and only an enabled one counts, so a city kept switched off behind an interior has none. The slice's driver marks the city's sheet at Sea Level, named Water by its builder, which has no collider because the grid reads water by height.
- **The state.** `PlayerLocomotion` swims in water 1.3 m or more over its bed, walking in or falling in, and wades out where the bed rises to 1.15 m, the root set down on the bed so the standing pose holds the chest where the stroke held it ([10.1](../../../10.1%20Grid%20World%20and%20Environment%20Actions.md) *As built*).
- **The clips** (§8): the controller's Swim blend holds Quaternius' Swim_Idle at Speed 0 and Swim_Fwd at 1, played faster on Pace for the fast swim, up to 1.8 times; ClimbOut plays ClimbUp_1m.
- **Measured** by `SliceAnimationCheck` on the player man fitted standing (`Logs/SliceSurvey/animation_check.txt`): the stroke holds the chest 0.12 m under the root and the head from 0.04 m under it to 0.08 m over it; the float holds the chest 0.11 m under it and the head 0.25–0.35 m over it. So the root floats 0.05 m over the water. ClimbUp_1m lasts 0.67 s.
- **Climbing out.** Space at a bank whose top stands 0.2–2.1 m over the water, with room for the capsule on it, lifts the body up the face and then over the edge in 0.8 s, the controller off while it moves. The city's quays stand 1.9 m over the sea. *Weak point:* the clip climbs a 1 m ledge, so the hands do not meet a quay's edge (99 VS-ANIM).
- **Fights and beasts.** `PlayerLocomotion.InWater` holds while the character swims or climbs out: `SliceConfront` starts no fight, and `SliceBeasts` neither notices a swimmer nor chases one, a pack that noticed it giving up.
- **Checked by** PlayMode `SwimPlayTests` (a fall into a basin, the float and the stroke, the fast swim, the climb onto a quay, the wade out onto a shelf) and EditMode `SlicePeopleTests.ThePlayerSwimsAndClimbsOut`.

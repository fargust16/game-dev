# Animation Pipeline — Kevin Iglesias and Quaternius to Unity

Version: 0.25

Status: Draft — the owner's second direction of 2026-11-11 (§0) and its clip map (§8); §1–§7 are the withdrawn Mixamo and Blender pipeline

Created: 2026-11-10

Last Updated: 2026-11-12 (§9, §14: the Aimed Walk built with the Aimed Shot (PB8b): the aim's upper-body layer, the side and back walks played by the body's own velocity, and the aim's numbers on the tuning page. Earlier: §9: the Aimed Walk faces the aim, the owner's answer to VS-ALARM (h), built with PB8b. Earlier: §9, §10, §13, §14, the owner's next play test (PB7): the landing 0.15 s; Chop's check at 0.17 s; a pushed bench sat on where it stands; the owner's tuning page for the jump, the work's key frames, the tools' grip and the HUD's size. Earlier: §9, §12, the owner's next play test (PB3): the landing 0.3 s and the arms in a jump no wider than at take-off; the swimmer neck-deep treading, the face just out in the stroke. Earlier: §11: the Assault, WA3–WA4: the walking townsfolk fight in the fight's clips, and a Warden takes up the fallen in Gather2. Earlier: §11, by the owner's play test of 2026-11-12, 10.2 §5c: each weapon's model in the hand that holds it, fitted by the hand's bones; fists guard and strike with Punch_Cross and Punch_Jab; R draws in free exploration and a click strikes. Earlier: the owner's play test of 2026-11-12: §9 the jump's take-off from Basic Motions, the bent-knee landing braking the run, a turn under 135° with no slide; §10 the tools in the right hand, each check a little before the blow; §12 a dip into the water and a lower float; §13 seated people only sit, a sitter's chair held, a knocked chair set up first, a stroll's pace, the two old looks retired. Earlier: §9: the owner revokes the camera-facing body; the character faces where it goes. §13: the people's models turned to the avatar's own forward; they walked 13–24° sideways. Earlier: §13: the owner kept the scaled kit seats; the pew and the furniture kit's chair get new models. Earlier: §13: the owner's trial: the kit's seats scaled so their top stands 0.42 m; a seated walker no longer shoves its seat. Earlier: §13: the walker fitted to the owner's seat sizes, its feet on the floor and its hips 0.225 m behind a seat's front edge, a place to every metre; the sizes in game metres, their scale VS-SEAT. Earlier: §13: the seats measured against the seated body: the kit's chairs and benches 0.50–0.85 m high against the bodies' 0.40–0.45 m; seat sizes proposed for the owner's models. Earlier: §13: AN6, the living townsfolk: Activity Spots on benches, stalls, the tavern's and the chapel's doors, chosen at random from the routes' turns; greetings; four idles at a stop; the Soot & Bell's drinkers and the chapel's people in its pews; Quaternius' takes, found facing backward, turned round at import. Earlier: §12: AN5, swimming: the water marked, the swim and the fast swim, the float measured on the clip, the climb out onto a quay, no fight and no beast in water. Earlier: §11: AN4, the fight: one table for the code and the controllers, two weapons' right and left blows, a shield, shots through their loads, hits, knockback, deaths, the Defensive Stance, Yield, and each ability's cast chosen for S5b. Earlier: §10: AN3, the work played to its key frame for every Reaction Check: the swing, the patch, the anvil, the kneel and the pocket; §8: theft on the crouched reach, the keys of the lock's and brewing's clips measured. Earlier: §9: AN2, the player's eight-way movement facing the camera, each clip at the pace its feet measure, the turn on the spot and the woman player's own takes. Earlier: §8: the clip map of AN1: every use's clip from Kevin Iglesias' FREE packs or Quaternius' Universal Animation Library 1 and 2, each work clip's key frame measured, the Mixamo and Blender clips deleted. Earlier: §0: the owner's second direction: Kevin Iglesias back, Quaternius for the gaps, Mixamo and Blender out, the character facing the camera, checks at the key frame. Earlier: §5 *The play test of 2026-11-11*: one shared avatar for the Mixamo files and a T-pose rest for the Blender clips, the movement state machine, the jump's landing, the blow's timing per weapon, the blacksmith's hammer, anvil and stump, and the sources. Earlier: §5 A fight: the walk in a fight and the hit or the death at the blow's impact, shown beat by beat by S3a.5's `FightBeats`. Earlier: 2026-11-10: §3, §5, §6: A3's first four clips, Mining, Chopping, Hammering and Surrender, made in Blender by a script that is their source; the blacksmith hammers at the anvil; the rest of A3 waits for its actions. Earlier: §5: A1 and A2 built and wired to the slice's actions: the clip table, the player's paces and the jump's cut measured, the talk and the fight, A0's stand-ins retired; Mining, Chopping, Hammering and Surrender not found, to A3. Earlier: §6.1: the download list, 48 clips; the owner downloads from Mixamo. Earlier: §1, §2, §5: the owner's player bodies and the women Wardens' uniform, Humanoid by Unity's own mapping; the animations are the agent's work. Earlier: §5: A0 built, the four stand-in clips and their controllers, the player's locomotion on them. Earlier: first version: the downloaded clip packs leave the project, the slice's own clips come from Mixamo and Blender, the rigs checked, the phases and the clip list)

Authority: [11 Vertical Slice](../../../11%20Vertical%20Slice.md), [SLICE_ASSET_MAP.md](SLICE_ASSET_MAP.md) §1–§2, [VERTICAL_SLICE_IMPLEMENTATION_PLAN.md](VERTICAL_SLICE_IMPLEMENTATION_PLAN.md)

Related: [10.2 Combat System Draft](../../../10.2%20Combat%20System%20Draft.md) §5a–§5b, [10.3 Stealth and Detection](../../../10.3%20Stealth%20and%20Detection.md), [99 Open Questions](../../../99%20Open%20Questions.md) VS-ANIM

---

## 0. The owner's second direction (2026-11-11)

After the play test of S5a the owner found the Mixamo clips crooked and angular and unfit for the game, and decided:

- Kevin Iglesias' **Human Animations** come back: Human Basic Motions, Human Crafting Animations, Human Melee Animations, Human Soldier Animations and Human Spellcasting Animations, the FREE versions, in `Assets/Kevin Iglesias/Human Animations/`. Each pack's PDF gives the Standard Asset Store EULA / Fab Standard License: royalty-free, allowed in commercial games, no resale, no attribution needed. The reading of 2026-11-10 that they could not be used commercially (§1) was wrong.
- The FREE packs have eight-way walking, running and sprinting, turns, the jump and fall, one- and two-handed blows, a shield blow, pistols and rifles, five kinds of cast, hits and deaths, talking, gathering, mining, hammering, farming and fishing. They lack swimming, crouching, sitting, drinking, praying, greeting, bargaining, chopping, First Aid, skinning, sleeping, Yield and blocking. By the owner's choice, **Quaternius' Universal Animation Library** (CC0, Humanoid) fills these, downloaded by the agent from Quaternius' own page on the owner's word.
- The **Mixamo clips and the agent's Blender clips leave the project**, with their controllers; §1–§7 below stand as the history of that pipeline. Tripo's own clips stay stand-ins only.
- The character **faced the camera** and moved in eight directions ([16](../../../16%20Architecture%20Pivot.md) §2); on 2026-11-12 the owner revoked it: the character faces where it goes (§9).
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

- **Facing where it goes** (owner, 2026-11-12; 16 §2). The owner revoked the camera-facing body below: the character always faces where it goes and walks, runs and sprints face first, never backward or sideways; the camera only looks. `PlayerLocomotion` turns the body to the way the keys choose at 900°/s and drives the blend straight ahead only, MoveX staying 0, so the side and back clips and Turn01 stay in the controller, unplayed; standing still, the body keeps the way it last faced, and Shift sprints any way. The faceCamera switch, the turn on the spot and the sprint's 100° arc are gone. The one exception is the **Aimed Walk** (owner, 2026-11-12, VS-ALARM (h); 10.2 §5e): while a firearm is aimed, the body faces the aim and the blend plays its side and back clips, walking only. Built with the Aimed Shot (PB8b): `PlayerAim` sets `PlayerLocomotion.AimFacing` to the camera's way while the aim is held; the body turns to it, moves at the walk's speed wherever the keys point, never sprints, and the blend reads the velocity in the body's own frame, so MoveX and MoveZ choose among the eight walks. The upper body plays on the player controller's `Aim` layer, an override with the `UpperBody` mask (body, head, arms, fingers): the firearm's guard while aiming and its load clip, timed to the loading's 1.5 s or 2.5 s, after a shot. The legs keep the walk, and `PlayerArms` leaves its guard while the aim is held or the firearm loads. The pack's side and slanting walks turn the hips toward the step (measured on the clips' body rotation: Walk01_Right about +58° for the man and +68° for the woman, Walk01_ForwardRight about +31°, Walk01_BackwardRight about −27°, Walk01_Backward about 0°), and the `Aim` layer poses the upper body on the hips, so the spine's three bones share the turn back to the body's facing while the layer weighs in; the legs keep the clip's step. Checked by `AimPlayTests` (the body within 8° of the aim, the walk's speed, MoveX above 0.3 walking right, the shoulders within 15° of their standing aim while the side walk plays).
- **Facing the camera** (owner, 2026-11-11, revoked 2026-11-12; 16 §2), as AN2 built it: `PlayerLocomotion` turned the body to the camera's forward while a move key is held, at 900°/s, and moves it where the keys point; standing still, it turns on the spot once the camera is 60° away (authoring default), at 240°/s, playing Kevin Iglesias' Turn01 left or right. In the air or landing with no key held the body keeps its facing. In Stealth Mode, and in the water (AN5), the body turns to where it goes, as the sneak and the swim have forward clips only. Shift sprints within 100° of the camera's forward; further back it runs, as the pack has no sprint backward.
- **Turns** (owner's play test, 2026-11-12: a run turned through 90° slid along the way it ran). A turn gentler than 135° (authoring default, `PlayerLocomotion.ReverseAngle`) carries the run round with the body: the body turns first and the velocity follows its forward at the speed it had, so every step goes where the body faces (measured, `ARunTurnedAtARightAngleNeverSlides`: under 8° between a frame's step and the body's facing). A sharper turn brakes and turns round as before, the owner having kept its pause.
- **The jump** (owner's play test, 2026-11-12). AN2 played Kevin Iglesias' Fall01 loop through the whole air time, its arms raised, and the take-off, JumpUp, was never played, so the jump looked like a landing from its first frame. It is Basic Motions' own again: `JumpRise` is Jump01 - Begin cut from 0.10 to 0.43 s with its height free, the toes leaving the ground at 0.25 s and the arms still down, since they rise only after 0.45 s (measured, `Logs/SliceSurvey/tools.txt`); it holds its last frame for the rest of the air time, so the arms stay where they were. A fall off an edge still plays Fall. The landing, JumpLand, starts 0.12 s in, as its first frames hold the fall's raised arms, and bends the knees from 0.10 to 0.45 s; it holds at least 0.5 s (`landingHold`), the body braking to a stop in it, and with no key held it plays whole, 0.48 s of its 0.60 s, so in place the two are alike. A jump pressed while landing is kept and goes once the half second is over. A fall shorter than 0.45 s lands without the knees.
- **The jump, the owner's second word** (2026-11-12, PB3): the landing 1.5–2 times shorter, and the arms no farther from the body than at take-off. The landing holds 0.3 s, and with no key held it plays 0.6 of its clip after LandStart, 0.29 s (`LandingShare`), so either way 0.3 s; the run's fade takes the rest. JumpRise's last pose still spread the arms, so from take-off through the air and the landing `PlayerLocomotion` turns each upper arm and forearm back, after the Animator's pose, until it hangs no farther from the body's down than at take-off, nor closer than 10° (authoring default, `LeastArmReach`). Checked by `MovementSmokeTests.AJumpRisesInItsTakeOffAndLandsInBentKnees` (no arm more than 2° wider in the air than at take-off; 0.3 s either way).
- **The jump, the owner's third word** (2026-11-12, PB7): the landing twice shorter again. It holds 0.15 s, and with no key held it plays 0.3 of its clip after LandStart, 0.14 s, so either way 0.15 s. The three values are on the owner's tuning page, `SliceTuning` *Прыжок и приземление* (§14), read each frame, so a change made while the game runs shows at the next landing. Checked by the same test against the values it reads.
- **The blend.** The player's `Locomotion` is a 2D freeform directional blend on MoveX and MoveZ, the body's own velocity on the scale 0–3: the idle at the centre, the eight walks at 1, the eight runs at 2, the five sprints at 3, each at the direction its name gives. `Speed` stays for the crouch's blend and the controllers that read it. The fight's walk sets MoveZ with Speed.
- **The pace of the feet.** `SlicePeopleModels` measures every moving clip on both player bodies: how fast the planted feet slide (each foot within 3 cm of its lowest point, the median of each component), and plays each clip at `speed / pace`, held between ×0.6 and ×1.3. Kevin Iglesias' clips match the game's speeds within 10 % in every direction: walks 2.0–2.3 m/s against 2.0, runs 4.0–4.7 against 4.5, sprints 7.2–8.3 against 7.5, so every clip plays between ×0.93 and ×1.09 (`Logs/people_models.txt`). The feet's own direction reads up to 55° off the name as the hips swing, so the names place the clips, not the measure.
- **The turn.** Kevin Iglesias' Turn01 steps in place with no root rotation of its own (measured 0°/s), so the code turns the body and the clip plays at ×1. The import leaves any turn's root rotation out of the pose, and a take that loops only in one sex is set as the table says (Idle03, Turn01, Talk01, Gathering01–03).
- **The woman player.** `OverworldPlayerAnimator_Woman.overrideController` is the player's controller with every Kevin Iglesias clip in its women's take; the woman player's prefab plays it. Quaternius' clips have one take.
- **Checked by** `SlicePeopleTests.ThePlayerControllerBlendsItsGaits` and `MovementSmokeTests.ThePlayerTurnsAndWalksFaceFirst` (right and back), `ARunTurnedAtARightAngleNeverSlides` and `AJumpRisesInItsTakeOffAndLandsInBentKnees`, with the door and stair walks.

## 10. Work by key frame (AN3, 2026-11-12)

- **One component for all of it.** `PlayerWork` (beside the Reaction Check on the world's driver) plays a `WorkStroke` on the player's own controller. It turns the character to the work and steps it to its place in 0.3 s, plays any lead-in clips whole, plays the stroke to its key frame, stops the clip there and shows the bar, the world paused but for theft; after the check the clip, or the clip that follows it, plays to its end. The character is held while it works and walks again when the work is over. The places are the thing's own edge, read from its colliders, plus the clip's reach: a swing 0.75 m, a patch or a carcass 0.35 m, the anvil 0.45 m, and a pocket 0.6 m from the person's pivot (authoring defaults).
- **The key frames** (§8, `SliceClipSurvey`, `Logs/SliceSurvey/clip_survey.txt`): Chop 0.13 s (0.17 s since the owner's next play test, PB7: 0.13 s still came too early), Mine 0.35 s, HammerDown 0.35 s, Harvest 1.27 s, Kneel 4.30 s, Gather2 0.60 s. By the owner's play test of 2026-11-12 a swing's check comes a little before the blow, never after it: the tool's head is fastest at 0.18 s in Chop, 0.40 s in Mine and 0.38 s in HammerDown (measured by `SliceTools`, `Logs/SliceSurvey/tools.txt`), and the old 0.53 s in Chop was the axe already swinging back. Chop's swing starts at the clip's first frame, inside the 0.2 s fade, so the first stroke starts 0.6 s in, at the axe raised, and stops at the key of its second cycle. `SliceClips.Player` gives the player's controller every work clip, `PlayerWork.Clips`, and the woman's controller takes their women's takes.
- **The tools in hand** (owner's play test, 2026-11-12: the player held nothing). The woodcutting axe, the pickaxe and the crafting hammer of Stylized 3D Tools ([SLICE_ASSET_MAP.md](SLICE_ASSET_MAP.md) §3) hang off both player bodies' right hand, hidden, and `PlayerWork` shows the one its clip uses while it works: Chop the axe, Mine the pickaxe, HammerDown the hammer. `SliceTools` sets each in the fist as the body stands at the blow, the handle across the fist, its head beyond the thumb and its blade or face along the swing, and scales it to a one-handed tool's length, since all three clips swing one-handed (the hands 1.07–1.15 m apart at the blow): the axe 0.75 m and the pickaxe 0.8 m (the pack's 1.12 and 1.11 m), the hammer 0.38 m (0.77 m).
- **Each action.**
  - *A tree or a vein:* one check a swing, the next swing once the last has played out, while the gathering lasts.
  - *A herb or flax patch:* the whole harvest once, its check as the hips begin to rise; the patch is empty after it whatever came of it, and the rise plays out though the gathering has ended.
  - *The anvil:* the piece's ticks pass as the character steps up, then two blows with their rebounds, the last blow to the anvil's face, its check, its rebound, and the page again with what came of it.
  - *A carcass and a wound:* the character kneels, the ticks pass as it kneels, the check comes at 4.30 s, and it rises.
  - *A pocket:* the crouched reach; the world runs on, and the try is called off, not spent, once the person stands 1 m from where it stood, or fails once it goes in at a door (owner, 2026-11-12; 10.3 §6).
- **Breaking off.** A move key breaks the work off before its check at a tree, a vein, a patch or a pocket. Work whose ticks have passed, at the anvil, over a carcass or a wound, is steady and plays to its end.
- **In a fight** First Aid's bar shows as soon as it is chosen and its AP are in hand, the treatment then given with its result (`TurnIntent.Treat` carries it); Space for the bar does not also end the turn. No kneel is played (open for the owner, 99 VS-ANIM).
- **Set by the owner** (2026-11-12, PB7: the check came too early and the axe was held wrong, and the owner sets both by hand). Each work clip's key frame and first stroke, and each tool's grip, are on the tuning page (§14): `PlayerWork` reads the key frame and the start there, the code's measured values only where the page has none, and puts the tool at `SliceTools`' grip shifted in the hand's axes and turned about its own, every frame it is out, so a change shows on the next stroke or at once.
- **Not built yet:** a lock and brewing; their clips' key frames are in §8.
- **Checked by** EditMode `GatheringTests`, `RemainsTests`, `FirstAidTests`, `EnvironmentConditionTests` and `SlicePeopleTests.ThePlayerWorksEachClipToItsKeyFrame`; `SlicePeopleTests.ThePlayerHoldsItsToolsInTheRightHand`; PlayMode `GatheringPlayTests` (a tree, the axe in hand and the check within 0.05 s of its key, and a herb patch), `TheftPlayTests` (a purse lifted, a theft noticed, a try called off, a person gone in at a door), `BeastPlayTests` (skinning), `ShopPlayTests` (the anvil) and `S5aAcceptancePlayTests`.

## 11. Combat (AN4, 2026-11-12)

- **One table for the code and the controllers.** `FightBeats` holds each Weapon value's guard, blow and load, the hits and the deaths, and `SliceClips` builds every fighter's blend trees from those tables, so a clip changed in one place is changed in both. The Weapon values: 1 a sword, an axe or a mace; 2 a two-handed weapon; 3 a dagger; 4 a pistol; 5 a rifle; 6 a weapon and a shield; 7 and 8 two weapons, the right hand's blow and the left's; 9 and 10 fists, the right's blow and the left's.
- **The guards and the blows** (§8): Guard1H and Attack1H for one hand, Guard2H and Attack2H for two, ShieldGuard and ShieldAttack for a shield, PistolAim and RifleAim with their shots. Two weapons guard as one hand and strike with Attack1H for the main hand and Attack1HLeft for the other, the hand the turns chose ([10.2](../../../10.2%20Combat%20System%20Draft.md) §5b *Two weapons*). A shot plays its shot and then its load, since the shot includes loading, and the beat holds until the load is over.
- **Measured** by `SliceAnimationCheck` on the player man (`Logs/SliceSurvey/animation_check.txt`): a blow's contact is the frame the striking hand is furthest out ahead of the chest, the left hand for the left's blow; a shot's is the frame the hand jumps back fastest; a hit's onset is the first frame the head moves faster than 0.6 m/s, entered 0.03 s before.

  | Weapon value | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
  |---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
  | Contact, s | 0.33 | 0.57 | 0.33 | 0.03 | 0.10 | 0.37 | 0.33 | 0.40 | 0.28 | 0.20 |
  | The beat holds after it, s | 0.65 | 0.65 | 0.65 | 2.47 | 2.40 | 0.65 | 0.65 | 0.65 | 0.65 | 0.65 |

  Hits: Hit 0.14 s, HitChest 0.04 s, HitHead 0, Hit2 0, GuardHit 0.04 s. The Block clip's right hand is highest over the chest at 0.63 s; the knockback moves from its first frame; GetUp lasts 1.53 s.
- **Fists** ([10.2](../../../10.2%20Combat%20System%20Draft.md) §5c, the owner's play test of 2026-11-12). A fighter with no weapon guards in Kevin Iglesias' CombatIdle01 and strikes with Quaternius' Punch_Cross from the right and Punch_Jab from the left, by turns, each fighter's own count. Their contact is measured by `SliceWeapons` (`Logs/SliceSurvey/held.txt`): the striking hand furthest out from the hips, at 0.28 s of 1.00 s and 0.20 s of 0.87 s.
- **Weapons in the hand** (WA1). `HeldWeapons` puts the Weapon Set's models into the hands while it is drawn: the weapon in the right, a shield or the second weapon in the left, none for fists or a Cartridge Pouch. Each is set by the hand's own bones, so it holds in every pose: the grip 0.6 of the way from the wrist to the middle knuckle and 0.03 m in front of the palm, the weapon leaving the fist on the index finger's side and turning its edge the knuckles' way, a shield's face the back of the left hand's. A fight draws every fighter's set and sheathes it at its end; a sheathed weapon is out of sight. `SliceWeapons` (batch `GameDev.Slice.EditorTools.SliceWeapons.Run`) makes each held prefab from the asset map's §3 model, measured by its triangles' cross-sections, and renders the check sheet `Logs/SliceSurvey/held.png`: each set's guard and blow, front and side, its hand close up.
- **Drawn in free exploration** (WA2; authoring defaults). `PlayerArms`: R draws the active set and sheathes it. Drawn, the body stands in its guard when still, a pistol or a rifle aimed, and walks and runs with the weapon in hand; a sprint, the water, a crouch, work, a page, a conversation or E sheathes it first. A click 0.4 s after the draw strikes: a melee blow the person within 1.5 m and 45° of the facing, a shot the person within 20 m and 15° of the camera's aim, the body turned to it; with nobody there the blow plays at the air, the body held still until it ends. A blow at a person is the Assault of WA3.
- **The Assault** (WA3–WA4). A walker struck stops its walk and fights: `SliceClips.Walker` gives the walking townsfolk's controllers the fight's parameters and states from Idle, the same table as a standing person's, so a resident guards, punches, takes a hit, yields and falls in the fight's clips. A Warden sent for the fallen (`WardenErrand`) walks in its walk clip, stoops over each body in `Gather`, the person controllers' Gather2, and stands at its post again in Idle.
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
- **Not built yet:** the casts above and Дрёма's sleep; a key for the player to choose the hand of a blow (99 VS-ANIM); the Cartridge Pouch in the hand and a sheathed weapon at the belt or across the back (99 VS-ASSAULT (b)).
- **Checked by** EditMode `TwoWeaponsTests` (a pair's rules, its parry and weight, a sword and a dagger striking 4 + 3 + 3 in 10 AP, two swords taking turns, a hand asked for), `OutfitTests.ASecondWeaponGoesInTheOtherHandBesideAOneHandedOne`, `HeldWeaponsTests` (every weapon's and shield's held model, fists' clips and contact) and `SlicePeopleTests.AStandingPersonTalksAndFightsInKevinIglesiasClips` (every fight state and blend from `FightBeats`' tables); PlayMode `ArmsPlayTests` (R draws fists and a sword into the right hand, a click's blow at the air, a sprint sheathes), `FightTurnPlayTests`, `LeavingFightPlayTests`, `BeastPlayTests`, `S3aAcceptanceTests` and `S5aAcceptancePlayTests`.

## 12. Swimming (AN5, 2026-11-12)

- **Where.** A `SwimWater` marks a water surface: its level is its height, over its renderer's ground plan, and only an enabled one counts, so a city kept switched off behind an interior has none. The slice's driver marks the city's sheet at Sea Level, named Water by its builder, which has no collider because the grid reads water by height.
- **The state.** `PlayerLocomotion` swims in water 1.3 m or more over its bed, walking in or falling in, and wades out where the bed rises to 1.15 m, the root set down on the bed so the standing pose holds the chest where the stroke held it ([10.1](../../../10.1%20Grid%20World%20and%20Environment%20Actions.md) *As built*).
- **The clips** (§8): the controller's Swim blend holds Quaternius' Swim_Idle at Speed 0 and Swim_Fwd at 1, played faster on Pace for the fast swim, up to 1.8 times; ClimbOut plays ClimbUp_1m.
- **Measured** by `SliceAnimationCheck` on the player man fitted standing (`Logs/SliceSurvey/animation_check.txt`): the stroke holds the chest 0.12 m under the root and the head from 0.04 m under it to 0.08 m over it; the float holds the chest 0.11 m under it and the head 0.25–0.35 m over it. The root floated 0.05 m over the water until the owner's play test of 2026-11-12 found the body too high, then 0.04 m under it, still too high by the owner's next word: treading still the character should be neck-deep, and in the stroke only the head a little out, the body under. Now the float goes with the stroke's speed from `treadHeight`, 0.28 m under the water, where the head's bone at the top of the neck is from 0.03 m under to 0.07 m over, to `floatHeight`, 0.08 m under, where the chest is 0.20 m under and the head's bone from 0.12 m under to level, the face just out. Over a bed shallower than the tread's soles, about 1.3 m under its root (authoring default, `TreadSoles`), the treading body rises to stand on it, never above the stroke's float. ClimbUp_1m lasts 0.67 s.
- **The dip** (owner's play test, 2026-11-12: going in was abrupt, with no dip). The water pulls the body toward its float like a spring, 12/s², with a drag of 5/s (authoring defaults, `BuoyancySpring`, `BuoyancyDrag`), and a body that falls in keeps its fall, at most 4 m/s, so it goes under first and comes up to its float in about a second with a small bob: a fall from a quay goes 1.4–1.5 m under (simulated; `SwimPlayTests` asserts deeper than 1.35 m).
- **Climbing out.** Space at a bank whose top stands 0.2–2.1 m over the water, with room for the capsule on it, lifts the body up the face and then over the edge in 0.8 s, the controller off while it moves. The city's quays stand 1.9 m over the sea. *Weak point:* the clip climbs a 1 m ledge, so the hands do not meet a quay's edge (99 VS-ANIM).
- **Fights and beasts.** `PlayerLocomotion.InWater` holds while the character swims or climbs out: `SliceConfront` starts no fight, and `SliceBeasts` neither notices a swimmer nor chases one, a pack that noticed it giving up.
- **Checked by** PlayMode `SwimPlayTests` (a fall into a basin, the float and the stroke, the fast swim, the climb onto a quay, the wade out onto a shelf) and EditMode `SlicePeopleTests.ThePlayerSwimsAndClimbsOut`.

## 13. Living townsfolk (AN6, 2026-11-12)

The owner's direction of 2026-11-11: the townsfolk go to the tavern, drink and talk; walk the city, stop and greet one another; stand at stalls and bargain; go to the chapel and pray; sit on benches; their activities random. The agent's working readings under the owner's grant; numbers are authoring defaults unless marked measured.

- **Activity Spots.** An `ActivitySpot` holds its places, one person to a place, of one of five kinds: *Sit* (a bench, a chair, a pew; the sitter only sits, by the owner's play test of 2026-11-12, since Sitting_Talking moved the hands oddly, and SitTalk stays in the controllers unplayed), *Bargain* (a stall: TalkIdle 3 s, Nod, TalkIdle 3 s, ShakeHead, in turn), *GoIn* (a door: in, out of sight, for a while), *Drink* (a bar: TalkIdle 4 s, Drink, TalkIdle 3 s, Nod) and *Attend* (stands quietly facing the place's way). A visit lasts 20–60 s at a bench, 15–40 s at a stall, 60–180 s in the tavern and 40–120 s in the chapel.
- **Placement.** `SliceActivities` (menu GameDev → Slice → Townsfolk, or batch) builds them again on every run. In Veilmouth: every street bench, its seat read from its own mesh (the top of its up-facing faces 0.25–0.8 m over its foot, its front away from its back), a place to every metre of seat, at most four, the hip joint's point 0.225 m behind its front edge, and none on a seat outside 0.36–0.62 m over the ground under it; each of Bawling Row's six stalls, a customer facing its seller 1.2 m out from where the seller stands, or two before a stall with none; the Soot & Bell's and the old chapel's street doors, three places each where an arriving player stands. A walker's visits are the places within 40 m of a turn of its route to which a straight walk is clear: ground under every half metre, no step over 0.6 m, and the route test's capsule clear of every structure, prop and tree but the spot's own piece, which the walk may pass beside, and but in the last 0.6 m. Most walks still stopped are stopped by the city's unnamed structure colliders, its public buildings, its lamps and its trees, so 24 of the 46 walkers have no visit yet; a walk round a corner would need the route's own graph. The run of 2026-11-12, after the walker was fitted to the seat sizes (`Logs/SliceSurvey/activities.txt`): 32 benches with 64 places, 6 stalls with 12, the two doors with 6; 326 visits for 22 of the 46 walkers.
- **The walker.** At a turn with visits, one turn in two, by the hash of its route and the turn, the walker takes a free one: it walks there straight, turns to the place's facing in 0.5 s, passes its while, and walks back to the turn. At a stop it plays one of four idles by the same hash: Kevin Iglesias' Idle01–03 and Quaternius' folded arms. Two walkers on their routes within 2.2 m, face to face, stop and nod to one another, each at most once in 45 s. Everything takes its time from the world's: a paused world pauses it, and a moved one is caught up at six times. An observer that notices the character stops a walker: it stands up from its seat or comes out of its door at once, frees its place, and walks back once it walks again; a scene switched off behind an interior keeps it as it was.
- **The pace** (owner's play test, 2026-11-12: the townsfolk seemed to run). A walker strolls at 1.3 m/s (authoring default, `TownsfolkWalker.StrollSpeed`), its walk clip played slower so the feet keep the ground, and walks at its own measured pace, 1.69–1.91 m/s, only in a hurry (`Hurry`), which nothing sets yet: S5c's schedules will.
- **Chairs** (owner's play test, 2026-11-12). A *Sit* place's chair is its spot's own piece where that piece is loose, as a street bench is, or else the loose prop whose home lies within 0.5 m of its seat over the ground (`ActivitySpot.Chair`); a fixed pew has none. Since the owner's next play test (PB7), a chair or bench pushed and still upright, within 10°, is sat on where it stands: the place's stand point, seat and facing go with it (`ActivitySpot.Now`), and a walk to it is not begun through a wall at knee height; before, a bench's home lay too far from its end places, the bench was read as fixed, and the townsfolk sat on the air where it had stood ([INTERIOR_FURNISHING_RULES.md](INTERIOR_FURNISHING_RULES.md) §13). From the moment a person takes the place to the moment it leaves, it holds the chair (`LooseProp.Hold`), kinematic, so no push or blow knocks it out from under the sitter. A place whose chair is not at home is never sat on: if the chair lies within 2.5 m of its home and nobody holds it, whoever comes to sit walks to it, stoops in Gather2, whose hands come lowest, 0.73 m, at 0.53 s, where Gather's stay at 1.17 m (measured, clip_survey.txt), and halfway through the 1.0 s clip the chair stands at home again; farther off, the place is not taken. A person with no route whose own chair is gone sits at another free place of its spot, setting that one's chair up first if need be, or stands and looks again every 5 s. Every loose prop goes home by itself one game hour, 60 ticks, after it came to rest away from it ([INTERIOR_FURNISHING_RULES.md](INTERIOR_FURNISHING_RULES.md) §13).
- **Seating, measured** on the player man (1.80 m, his hips 0.97 m over the root standing; `SliceAnimationCheck`, the clips played in `ActivityPlayTests`): Sit holds the hips 0.61 m over the root and 0.26 m behind it. Since the seat sizes (2026-11-12) the walker keeps its feet on the floor: its root stays on the floor before the seat through StandToSit and SitToStand, and its hips come 0.225 m behind the seat's front edge, so the seat must stand at the bodies' height. A body's proportions move its seated hips: the working woman's sat 0.15 m off her seat (measured), so a body seated for the first time reads its own hips, behind and beside its root, once the sitting clip has settled and slides so that they come over its place at 0.5 m/s, a pose out of reason, as a culled animator's, never read. Before the seat sizes the walker set its root 0.26 m in front of the seat and the hips 0.10 m over its top, the feet following. StandToSit lasts 1.30 s and SitToStand 1.03 s; Nod 2.50 s; Drink 1.33 s.
- **The people's models turned** (2026-11-12). `SlicePeopleModels.Fit` turned each person's model by its clip's first pose, whose shoulders turn with the stride or the stance, so the walkers walked 13–24° to the right of their way and the standing people stood 5–7° to the left, which the owner saw as walking sideways (owner, 2026-11-12); it now turns the model by the avatar's neutral pose, and every walk's body faces its way within 1° on average (measured, `ActivityPlayTests.EveryWalkerWalksFacingItsWay`, `MeasureTheWalkFacing`). `SlicePeopleModels.FaceForward` turned every walker, standing person and player body; the walkers' measured paces rose 0.05–0.18 m/s (1.69–1.91 m/s now), their stride no longer read at a slant, and the scenes' walkers follow their prefabs.
- **Quaternius' takes faced backward.** AN6's measurement found every Quaternius take's mannequin facing back along the root, its shoulders 148–180° off in the curves, so since AN1 the sneak, the swim, the block, the shield's guard, the knockback, getting up, the climb out and the rest played facing backward. The import now turns each take round (its root rotation offset 180°). The editor's sampling leaves the offset out, as every table here does, so `ActivityPlayTests` measures the facing in play: each clip within 50° of the body's own idle, and the seated hips where the walker puts them.
- **The interiors.** The Soot & Bell: two drinkers at the two nearest chairs clear of its named people, talking, and one at the bar, drinking, on the customers' side and 1.6 m or more along the bar from the place before Nell (the first free of 2.4 m and 1.6 m to either side), so that the player still walks up to her and talks. The old chapel: three people standing quietly in its pews' rows, 0.35 m before a pew and facing the rostrum (*Attend*): the kit's pew, the Dungeon pack's wooden bench fitted to 4 m by its box, had its seat 0.85 m over the floor (measured) and since the owner's trial of the seats has it at 0.42 m, but they still stand. None of them walks a route: each keeps its place for ever, and comes back to it if stopped.
- **Seats, measured** (2026-11-12, on the owner's question which seats a person can sit on; `SliceActivities.SurveySeats` writes `Logs/SliceSurvey/seats.txt`, and `ActivityPlayTests.MeasureTheSeatedBody` and `RenderTheSeatedBodies`, diagnostics run by name, write `seated_body.txt` and the side views). In Sit, its soles on the floor, a walker's hip joint stands 0.54–0.61 m up and the lowest skin under it 0.35–0.45 m up; on trial seats every body touches a 0.40 m seat, and on a 0.45 m one skirts and coat tails pass into it. Its back is 0.14–0.21 m behind the hip joint, upright, its knees 0.40–0.46 m before it, its toes 0.29–0.36 m before the root; with its hands on its thighs it is 0.58–0.80 m wide for the men and 0.68–0.96 m for the women, whose skirts are the widest. The kit's seats stand higher: the street bench 0.54 m, the Victorian chair 0.50, the dark chair and the Tripo armchair 0.56, the stool 0.70, the furniture kit's chair 0.78 and the pew 0.85 (both fitted by their box); only the Victorian armchair, settee and sofas are low enough, 0.40–0.42 m, but they are 0.75–1.00 m deep, and the walker set the hips over a seat's middle. It set the hip joint 0.10 m over the seat's top, the feet following: on a 0.50 m seat the feet reached the floor and the body sank 7–14 cm into the seat, and on a 0.42 m seat the feet would have sunk 7 cm into the floor.
- **Seat sizes** (the owner, 2026-11-12, accepted by asking that the walker be fitted to them; [INTERIOR_FURNISHING_RULES.md](INTERIOR_FURNISHING_RULES.md) §13): a seat's top 0.42 m over the floor (0.40–0.45), 0.45 m from its front edge to its back's face, a back leaning at most 10°, 1.0 m of seat to a person, arms if any at most 0.18 m over the seat and 0.75 m apart inside, 0.6 m of floor clear before the front edge, pews 0.95–1.0 m apart, no high stool, all in game metres. Furniture stands at ×1.2 its true size, so how a new seat is scaled is open ([99](../../../99%20Open%20Questions.md) VS-SEAT). The walker keeps its feet on the floor and sets its hips 0.225 m behind the seat's front edge, a place to every metre (`TownsfolkWalker.SeatTop`, `HipsBehindFront`). On the owner's trial of the same day, which the owner kept, the kit's seats are scaled so that their top stands 0.42 m ([INTERIOR_FURNISHING_RULES.md](INTERIOR_FURNISHING_RULES.md) §13), the Victorian armchair and sofas staying at 0.40 m, so no body sinks into them; the dark stool and the footstool are not sat on. A seated walker's body ignores whatever collider is within 0.3 m of its place's seat (`TownsfolkWalker.IgnoreSeat`): before, its capsule reached about 0.2 m into the seat and shoved a loose chair or armchair aside.
- **The two old looks retired** (owner's play test, 2026-11-12: the first two people models did not fit the style). `Townsperson`, `Townswoman`, `Townsman` and `Matron` are deleted; their 40 people in Veilmouth, the Soot & Bell, the old chapel and Harrow's took the gentleman (men) and the working woman or the lady (women) in place, keeping their places and routes (`SlicePeopleModels.RetireOldLooks`, `Logs/SliceSurvey/retired_looks.txt`). The owner's two FBX folders stay in `TripoModels` until the owner removes them.
- **Not used yet** of §8's AN6 rows: Lean and LeanCall (no rail placed for them), Lantern (no lantern to carry), WalkFormal and Carry (each walker's pace is measured on its walk), and Gather at a stall.
- **Weak points** ([99](../../../99%20Open%20Questions.md) VS-ANIM): neither pack has a prayer, so the chapel's people stand quietly, though since the owner's trial of the seats its pews are low enough to sit on; the pew and the furniture kit's chair look squat at the seat sizes until their new models come; Quaternius' drink is standing and neither pack has a seated drink, so seated people only sit; a nod stands for a wave; the townsfolk who go in at a door are out of sight inside, and the people within are others until the schedules of S5c join them.
- **Checked by** EditMode `ActivityTests` (the import's turn, the visit rate, Veilmouth's spots and every visit's walk, the tavern's and the chapel's people) and `TownsfolkTests` (the walkers' controllers); PlayMode `ActivityPlayTests` (the facing in play, a bench where two only sit, the feet on the floor and the hip joint over a 0.42 m seat, a stall, a door, a greeting, a person who keeps its seat, a sitter's chair held and a knocked chair set up first, nobody sitting where a chair is gone, a chair home an hour after it was knocked, a pushed bench sat on where it stands, a stroll and a hurry) and `TownsfolkTests`.

## 14. The owner's tuning page (PB7, 2026-11-12)

The owner's direction of 2026-11-12: the values of feel are taken out where the owner turns them in Unity, grouped by what they do and named for it, and the owner comes back with the numbers. One asset, `Assets/Resources/SliceTuning.asset` (`SliceTuning`, opened by the menu GameDev → Slice → Tuning), its groups and fields named in Russian with a tooltip each; a change made while the game runs stays after it, as an asset's does, and the next jump, stroke or frame reads it. Every value on it is an authoring default until the owner sets it, and the code keeps the same defaults where the asset is missing.

- **Прыжок и приземление:** the knees' hold, the share of the landing clip played with no key, the air time before a fall lands in the knees (§9).
- **Работа:** for each work clip, the second where it stops and the Reaction Check's bar shows, and the second its first stroke starts from (§10). The clip stops at that very frame, so the pose on the screen is the pose of the check: a later value is later in the swing.
- **Инструменты в руке:** for the axe, the pickaxe and the hammer, a shift in the hand's axes, m, and a turn about the tool's own axes, degrees, on top of `SliceTools`' grip, shown at once while the tool is out (§10).
- **Прицел и выстрел вне боя:** how fast the firearm comes up, the view's width for a pistol and for a rifle, how far ahead of the eye the rear sight stands for each, where the eye stands from the head and how much its place is smoothed, the mouse's speed while aiming, and the projectile's speed, share of gravity and reach (10.2 §5e *In the slice*).
- **Интерфейс:** the size of the whole HUD, 1 as built for 1920×1080, and how it fits the window, by width (0, as built) or by height (1); the driver sets them on the HUD's panel each frame and gives the panel back as it was when the world ends ([17](../../../17%20Game%20Systems%20Architecture.md) X.6).

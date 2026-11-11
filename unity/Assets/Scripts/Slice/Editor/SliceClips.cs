using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Every clip of the people and the player by its use (owner's second direction, 2026-11-11; ANIMATION_PIPELINE_PLAN.md
    /// §0 and §8): Kevin Iglesias' Human Animations, the FREE packs, men's and women's takes alike, and Quaternius'
    /// Universal Animation Library 1 and 2 (CC0) for what they lack. The Mixamo and Blender clips are gone.
    /// <list type="bullet">
    /// <item>Kevin Iglesias' files keep the pack's own import: Humanoid on the pack's avatar, in place, one clip a file,
    /// a man's file under <c>Male</c> as <c>HumanM@</c> and a woman's under <c>Female</c> as <c>HumanF@</c>.</item>
    /// <item>Quaternius' two files (<see cref="Q1"/>, <see cref="Q2"/>) are imported here as Humanoid with an avatar of
    /// their own, each take used cut out under its use's name, in place, root rotation and height baked into the pose.</item>
    /// <item>A standing person's controller: the idle, the walk, the talk while Talking, and the fight: a guard and a blow
    /// by Weapon (1 sword, axe or mace; 2 greatsword, greataxe or maul; 3 dagger; 4 pistol; 5 rifle), the hit on Hit and
    /// the death while Down. A woman's controller plays the women's takes.</item>
    /// <item>The player's: the locomotion at Speed 0–3 (idle, walk, run, sprint), each clip sped to the player's pace, the
    /// crouch, the fall and landing, and the same fight.</item>
    /// </list>
    /// </summary>
    public static class SliceClips
    {
        public const string KevinFolder = "Assets/Kevin Iglesias/Human Animations/Animations";
        public const string QuaterniusFolder = "Assets/Quaternius";
        public const string Q1 = QuaterniusFolder + "/UAL1_Standard.fbx";
        public const string Q2 = QuaterniusFolder + "/UAL2_Standard.fbx";
        public const string ControllerFolder = "Assets/Animation/Controllers";
        public const string WardenController = ControllerFolder + "/Warden.controller";
        public const string WardenWomanController = ControllerFolder + "/WardenWoman.controller";
        public const string BlacksmithController = ControllerFolder + "/Blacksmith.controller";

        /// <summary>One use's clip: a Kevin Iglesias file (its folder under Male or Female and its name after the @) or a Quaternius take.</summary>
        public readonly struct Entry
        {
            public readonly string Use;
            public readonly string Folder;
            public readonly string Name;
            public readonly string File;
            public readonly bool Loop;

            /// <summary>A part of the file's clip, s into it; From below 0 for the whole clip.</summary>
            public readonly float From;
            public readonly float To;

            private Entry(string use, string folder, string name, string file, bool loop, float from = -1f, float to = -1f)
            {
                Use = use;
                Folder = folder;
                Name = name;
                File = file;
                Loop = loop;
                From = from;
                To = to;
            }

            public bool Kevin => File == null;

            public bool Cut => From >= 0f;

            /// <summary>A part of a Kevin Iglesias file, from <paramref name="from"/> to <paramref name="to"/> s, cut out as a clip of its own.</summary>
            public static Entry KCut(string use, string folder, string name, float from, float to) => new Entry(use, folder, name, null, false, from, to);

            /// <summary>A Kevin Iglesias file, e.g. <c>K("Walk", "Movement/Walk", "Walk01_Forward", true)</c>.</summary>
            public static Entry K(string use, string folder, string name, bool loop) => new Entry(use, folder, name, null, loop);

            /// <summary>A Quaternius take of <paramref name="file"/>, e.g. <c>Q("Swim", Q1, "Swim_Fwd_Loop", true)</c>.</summary>
            public static Entry Q(string use, string file, string take, bool loop) => new Entry(use, null, take, file, loop);
        }

        private static Entry K(string use, string folder, string name, bool loop = false) => Entry.K(use, folder, name, loop);

        private static Entry Q(string use, string file, string take, bool loop = false) => Entry.Q(use, file, take, loop);

        /// <summary>Every clip by its use; ANIMATION_PIPELINE_PLAN.md §8 gives each one's place in the game.</summary>
        public static readonly Entry[] Clips =
        {
            // Standing and moving (Human Basic Motions)
            K("Idle", "Idles", "Idle01", true),
            K("Idle2", "Idles", "Idle02", true),
            K("Idle3", "Idles", "Idle03", true),
            K("Attention", "Idles", "MilitaryIdle01", true),
            K("Walk", "Movement/Walk", "Walk01_Forward", true),
            K("WalkForwardLeft", "Movement/Walk", "Walk01_ForwardLeft", true),
            K("WalkForwardRight", "Movement/Walk", "Walk01_ForwardRight", true),
            K("WalkLeft", "Movement/Walk", "Walk01_Left", true),
            K("WalkRight", "Movement/Walk", "Walk01_Right", true),
            K("WalkBack", "Movement/Walk", "Walk01_Backward", true),
            K("WalkBackLeft", "Movement/Walk", "Walk01_BackwardLeft", true),
            K("WalkBackRight", "Movement/Walk", "Walk01_BackwardRight", true),
            K("Run", "Movement/Run", "Run01_Forward", true),
            K("RunForwardLeft", "Movement/Run", "Run01_ForwardLeft", true),
            K("RunForwardRight", "Movement/Run", "Run01_ForwardRight", true),
            K("RunLeft", "Movement/Run", "Run01_Left", true),
            K("RunRight", "Movement/Run", "Run01_Right", true),
            K("RunBack", "Movement/Run", "Run01_Backward", true),
            K("RunBackLeft", "Movement/Run", "Run01_BackwardLeft", true),
            K("RunBackRight", "Movement/Run", "Run01_BackwardRight", true),
            K("Sprint", "Movement/Sprint", "Sprint01_Forward", true),
            K("SprintForwardLeft", "Movement/Sprint", "Sprint01_ForwardLeft", true),
            K("SprintForwardRight", "Movement/Sprint", "Sprint01_ForwardRight", true),
            K("SprintLeft", "Movement/Sprint", "Sprint01_Left", true),
            K("SprintRight", "Movement/Sprint", "Sprint01_Right", true),
            K("TurnLeft", "Movement/Turn", "Turn01_Left", true), // stepping round for as long as the turn lasts
            K("TurnRight", "Movement/Turn", "Turn01_Right", true),
            K("JumpUp", "Movement/Jump", "Jump01 - Begin"),
            K("Fall", "Movement/Jump", "Fall01", true),
            K("JumpLand", "Movement/Jump", "Jump01 - Land"),

            // Combat (Human Melee, Soldier and Spellcasting)
            K("Guard", "Combat", "CombatIdle01", true),
            K("Guard1H", "Combat/1H", "CombatIdle1H01", true),
            K("Attack1H", "Combat/1H", "Attack1H01_R"),
            K("Attack1HLeft", "Combat/1H", "Attack1H01_L"),
            K("Guard2H", "Combat/2H", "CombatIdle2H01", true),
            K("Attack2H", "Combat/2H", "Attack2H01"),
            K("ShieldAttack", "Combat/Shield", "AttackShield01"),
            K("PistolAim", "Combat/Gun", "Gun_Aim01", true),
            K("PistolShot", "Combat/Gun", "Gun_Aim01_Shoot01"),
            K("PistolLoad", "Combat/Gun", "Gun_Reload01"),
            K("RifleAim", "Combat/Rifle", "Rifle_Aim01", true),
            K("RifleShot", "Combat/Rifle", "Rifle_Aim01_Shoot01"),
            K("RifleLoad", "Combat/Rifle", "Rifle_Reload01"),
            K("Hit", "Combat", "Damage01"),
            K("Hit2", "Combat", "Damage02"),
            K("GuardHit", "Combat", "CombatDamage01"),
            K("Die", "Combat", "Death01"),
            K("Die2", "Combat", "Death02"),
            K("Die3", "Combat", "Death03"),
            K("CastIdle", "Combat/Spellcasting", "CastingIdle01", true),
            K("CastEnter", "Combat/Spellcasting", "CastingEnter01"),
            K("CastExit", "Combat/Spellcasting", "CastingExit01"),
            K("CastHit", "Combat/Spellcasting", "CastingDamage01"),
            K("CastCall", "Combat/Spellcasting/MagicAttacks/Call", "MagicAttackCall1H01_L"),
            K("CastDirectLeft", "Combat/Spellcasting/MagicAttacks/Directional", "MagicAttackDirect1H01_L"),
            K("CastDirectRight", "Combat/Spellcasting/MagicAttacks/Directional", "MagicAttackDirect1H01_R"),
            K("CastDirect2H", "Combat/Spellcasting/MagicAttacks/Directional", "MagicAttackDirect2H01"),
            K("CastOmni", "Combat/Spellcasting/MagicAttacks/Omnidirectional", "MagicAttackOmni01"),

            // Talk and work (Human Basic Motions and Crafting)
            K("Talk", "Social/Conversation", "Talk01", true),
            K("Gather", "Work/Gathering", "Gathering01"),
            K("Gather2", "Work/Gathering", "Gathering02"),
            K("Gather3", "Work/Gathering", "Gathering03"),
            K("Mine", "Work/Mining", "MiningOneHand01_R - Ground", true),
            K("MineWall", "Work/Mining", "MiningOneHand01_R - Wall", true),
            // At the anvil the pick's blow from above, stopped by the anvil: the pack's hammering kneels and strikes the
            // ground, its fist down to 0.28 m, under any anvil's face (clip_survey.txt). The blow down to where the hammer
            // meets a face 0.75 m high, the anvil's own on the ground, and its rebound from the same height on: what goes
            // lower is cut out (measured on the armed blacksmith, blacksmith.txt: down across 0.75 m at 0.38 s, up at 0.81 s).
            Entry.KCut("HammerDown", "Work/Mining", "MiningOneHand01_R - Ground", 0f, 0.38f),
            Entry.KCut("HammerUp", "Work/Mining", "MiningOneHand01_R - Ground", 0.81f, 0.97f),

            // What Kevin Iglesias' FREE packs lack: Quaternius' Universal Animation Library 1
            Q("Swim", Q1, "Swim_Fwd_Loop", true),
            Q("SwimIdle", Q1, "Swim_Idle_Loop", true),
            Q("Crouch", Q1, "Crouch_Idle_Loop", true),
            Q("Sneak", Q1, "Crouch_Fwd_Loop", true),
            Q("Sit", Q1, "Sitting_Idle_Loop", true),
            Q("SitTalk", Q1, "Sitting_Talking_Loop", true),
            Q("StandToSit", Q1, "Sitting_Enter"),
            Q("SitToStand", Q1, "Sitting_Exit"),
            Q("TalkIdle", Q1, "Idle_Talking_Loop", true),
            Q("Interact", Q1, "Interact"),
            Q("PickUp", Q1, "PickUp_Table"),
            Q("Kneel", Q1, "Fixing_Kneeling"),
            Q("WalkFormal", Q1, "Walk_Formal_Loop", true),
            Q("HitChest", Q1, "Hit_Chest"),
            Q("HitHead", Q1, "Hit_Head"),
            Q("SpellEnter", Q1, "Spell_Simple_Enter"),
            Q("SpellIdle", Q1, "Spell_Simple_Idle_Loop", true),
            Q("SpellShot", Q1, "Spell_Simple_Shoot"),
            Q("SpellExit", Q1, "Spell_Simple_Exit"),

            // ... and 2
            Q("Drink", Q2, "Consume"),
            Q("Nod", Q2, "Yes"),
            Q("ShakeHead", Q2, "Idle_No_Loop", true),
            Q("FoldArms", Q2, "Idle_FoldArms_Loop", true),
            Q("Lean", Q2, "Idle_Rail_Loop", true),
            Q("LeanCall", Q2, "Idle_Rail_Call"),
            Q("Lantern", Q2, "Idle_Lantern_Loop", true),
            Q("Chop", Q2, "TreeChopping_Loop", true),
            Q("Harvest", Q2, "Farm_Harvest"),
            Q("Block", Q2, "Sword_Block"),
            Q("ShieldGuard", Q2, "Idle_Shield_Loop", true),
            Q("ShieldBash", Q2, "Shield_OneShot"),
            Q("Knockback", Q2, "Hit_Knockback"),
            Q("GetUp", Q2, "LayToIdle"),
            Q("Carry", Q2, "Walk_Carry_Loop", true),
            Q("OpenChest", Q2, "Chest_Open"),
        };

        /// <summary>The guard and the blow of each Weapon value, 1 to 5; the dagger guards and strikes as a one-handed weapon.</summary>
        private static readonly string[] Guards = { "Guard1H", "Guard2H", "Guard1H", "PistolAim", "RifleAim" };

        private static readonly string[] Blows = { "Attack1H", "Attack2H", "Attack1H", "PistolShot", "RifleShot" };

        public static Entry EntryOf(string use)
        {
            var i = Array.FindIndex(Clips, c => c.Use == use);
            if (i < 0)
            {
                throw new InvalidOperationException($"[SliceClips] no use '{use}' in the table");
            }

            return Clips[i];
        }

        /// <summary>The file of <paramref name="use"/>: a man's or a woman's Kevin Iglesias file, or the Quaternius file.</summary>
        public static string PathOf(string use, bool female = false)
        {
            var e = EntryOf(use);
            if (!e.Kevin)
            {
                return e.File;
            }

            var sex = female ? "Female" : "Male";
            var prefix = female ? "HumanF@" : "HumanM@";
            return $"{KevinFolder}/{sex}/{e.Folder}/{prefix}{e.Name}.fbx";
        }

        /// <summary>The imported clip of <paramref name="use"/>, a woman's take when <paramref name="female"/>; throws if it is not there.</summary>
        public static AnimationClip Get(string use, bool female = false)
        {
            var e = EntryOf(use);
            var path = PathOf(use, female);
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
            var clip = e.Kevin && !e.Cut ? clips.FirstOrDefault(c => !Clips.Any(o => o.Cut && o.Use == c.name)) : clips.FirstOrDefault(c => c.name == use);
            if (clip == null)
            {
                throw new InvalidOperationException($"[SliceClips] no clip '{use}' in {path}: import Kevin Iglesias' packs and run SlicePeopleModels.Run");
            }

            return clip;
        }

        // ------------------------------------------------------------------------------------------- import

        /// <summary>
        /// Quaternius' takes cut out by their uses, and every clip of the table checked Humanoid, a man's and a woman's.
        /// </summary>
        public static void Import(StringBuilder report)
        {
            foreach (var file in new[] { Q1, Q2 })
            {
                ImportQuaternius(file, report);
            }

            foreach (var female in new[] { false, true })
            {
                foreach (var cuts in Clips.Where(c => c.Cut).GroupBy(c => PathOf(c.Use, female)))
                {
                    CutKevin(cuts.Key, cuts.ToList(), report);
                }
            }

            // A moving clip that travels in its own curves would carry the body off and spring it back at each loop, as
            // the code moves the body: its travel is left out of the pose (more than 0.3 m/s, authoring default).
            foreach (var (use, _, _) in Moves())
            {
                foreach (var female in new[] { false, true })
                {
                    var drift = Drift(Get(use, female));
                    var path = PathOf(use, female);
                    var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                    var clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
                    if (drift.magnitude > 0.3f && clips[0].lockRootPositionXZ)
                    {
                        clips[0].lockRootPositionXZ = false;
                        importer.clipAnimations = clips;
                        importer.SaveAndReimport();
                        report.AppendLine($"{Path.GetFileNameWithoutExtension(path)}: travels {drift.magnitude:0.00} m/s ({drift.x:+0.00;-0.00}, {drift.y:+0.00;-0.00}) in its curves, left out of the pose");
                    }
                }
            }

            var broken = 0;
            foreach (var e in Clips)
            {
                foreach (var female in e.Kevin ? new[] { false, true } : new[] { false })
                {
                    var path = PathOf(e.Use, female);
                    if (AssetDatabase.LoadAssetAtPath<Object>(path) == null)
                    {
                        throw new InvalidOperationException($"[SliceClips] {e.Use}: no file at {path}");
                    }

                    var clip = Get(e.Use, female);
                    var turn = e.Use == "TurnLeft" || e.Use == "TurnRight";
                    if (e.Kevin && !e.Cut && (clip.isLooping != e.Loop || (turn && Baked(path))))
                    {
                        // The pack's own takes differ here and there between a man and a woman (Idle03 loops only in the
                        // man's): the table decides. A turn on the spot leaves its rotation to the code, which turns the
                        // body; baked into the pose, the body would turn twice and spring back at each loop.
                        Loop(path, e.Loop, report, turn);
                        clip = Get(e.Use, female);
                    }
                    var curves = AnimationUtility.GetCurveBindings(clip).Count(b => b.type == typeof(Animator));
                    var sound = clip.humanMotion && curves > 0;
                    broken += sound ? 0 : 1;
                    report.AppendLine($"{e.Use}{(female ? " (woman)" : string.Empty)}: {clip.length:0.00} s{(clip.isLooping ? ", loops" : string.Empty)}, " +
                                      $"{curves} muscle and root curves, {(e.Kevin ? "Kevin Iglesias" : Path.GetFileNameWithoutExtension(e.File))}" +
                                      (sound ? string.Empty : ", NOT HUMANOID"));
                }
            }

            if (broken > 0)
            {
                throw new InvalidOperationException($"[SliceClips] {broken} clips are not Humanoid:\n{report}");
            }
        }

        /// <summary>Whether a file's own clip bakes its root rotation into the pose.</summary>
        private static bool Baked(string path)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            var clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
            return clips[0].lockRootRotation;
        }

        /// <summary>A Kevin Iglesias file's own clip set to loop or not, its other settings as the pack made them.</summary>
        private static void Loop(string path, bool loop, StringBuilder report, bool freeRotation = false)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            var clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
            clips[0].loopTime = loop;
            if (freeRotation)
            {
                clips[0].lockRootRotation = false;
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            report.AppendLine($"{Path.GetFileNameWithoutExtension(path)}: set to {(loop ? "loop" : "play once")}, as the table says");
        }

        /// <summary>
        /// A Quaternius file as Humanoid with its own avatar, made from the mannequin it carries in its rest pose, and each
        /// take the table uses cut out under its use's name, in place.
        /// </summary>
        private static void ImportQuaternius(string path, StringBuilder report)
        {
            if (!(AssetImporter.GetAtPath(path) is ModelImporter importer))
            {
                throw new InvalidOperationException($"[SliceClips] no Quaternius file at {path}: download the Universal Animation Library into {QuaterniusFolder}");
            }

            if (importer.animationType != ModelImporterAnimationType.Human || importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel ||
                importer.defaultClipAnimations.Length == 0)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.sourceAvatar = null;
                importer.importAnimation = true;
                importer.clipAnimations = Array.Empty<ModelImporterClipAnimation>();
                importer.SaveAndReimport();
            }

            var takes = importer.defaultClipAnimations;
            var wanted = Clips.Where(c => c.File == path).ToList();
            var clips = wanted.Select(e =>
            {
                // A fresh copy of the take for each use, so two uses of one take are two clips.
                var take = importer.defaultClipAnimations.FirstOrDefault(t => t.takeName == "Armature|" + e.Name || t.takeName == e.Name);
                if (take == null)
                {
                    throw new InvalidOperationException($"[SliceClips] {path} has no take {e.Name} for {e.Use}; it has {string.Join(", ", takes.Select(t => t.takeName))}");
                }

                take.name = e.Use;
                take.loopTime = e.Loop;
                take.loopPose = e.Loop;
                take.lockRootRotation = true;
                take.keepOriginalOrientation = true;
                take.lockRootHeightY = true;
                take.keepOriginalPositionY = true;
                take.lockRootPositionXZ = true;
                take.keepOriginalPositionXZ = true;
                return take;
            }).ToArray();

            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();

            var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                throw new InvalidOperationException($"[SliceClips] {path}: no valid human avatar (see the log's avatar errors)");
            }

            // The mannequin's rest: about 90° from down in a T-pose, as Unity's retargeting wants.
            var copy = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            var animator = copy.GetComponent<Animator>() ?? copy.AddComponent<Animator>();
            animator.avatar = avatar;
            var arm = SlicePeopleModels.ArmDrop(animator);
            Object.DestroyImmediate(copy);
            report.AppendLine($"{Path.GetFileName(path)}: {clips.Length} of {takes.Length} takes cut out; its rest arm {arm:0}° from down");
        }

        /// <summary>
        /// The parts of a Kevin Iglesias file the table cuts out, added after the file's own clip, which stays first and
        /// as the pack made it.
        /// </summary>
        private static void CutKevin(string path, System.Collections.Generic.List<Entry> cuts, StringBuilder report)
        {
            if (!(AssetImporter.GetAtPath(path) is ModelImporter importer))
            {
                throw new InvalidOperationException($"[SliceClips] no file at {path} to cut {cuts[0].Use} from");
            }

            ModelImporterClipAnimation Own() => importer.clipAnimations.Length > 0 ? importer.clipAnimations[0] : importer.defaultClipAnimations[0];
            var own = Own();
            var whole = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => c.name == own.name);
            var rate = whole.frameRate;
            // Each part a new clip with the file's root settings: a copy would keep the clip's internal id, and Unity
            // keeps only one clip of an id.
            var clips = new[] { own }.Concat(cuts.Select(e => new ModelImporterClipAnimation
            {
                name = e.Use,
                takeName = own.takeName,
                firstFrame = own.firstFrame + Mathf.Round(e.From * rate),
                lastFrame = Mathf.Min(own.lastFrame, own.firstFrame + Mathf.Round(e.To * rate)),
                loopTime = false,
                loopPose = false,
                lockRootRotation = own.lockRootRotation,
                lockRootHeightY = own.lockRootHeightY,
                lockRootPositionXZ = own.lockRootPositionXZ,
                keepOriginalOrientation = own.keepOriginalOrientation,
                keepOriginalPositionY = own.keepOriginalPositionY,
                keepOriginalPositionXZ = own.keepOriginalPositionXZ,
                heightFromFeet = own.heightFromFeet,
                rotationOffset = own.rotationOffset,
                heightOffset = own.heightOffset,
                cycleOffset = own.cycleOffset,
                mirror = own.mirror,
            })).ToArray();
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            report.AppendLine($"{Path.GetFileNameWithoutExtension(path)}: {string.Join(", ", cuts.Select(e => $"{e.Use} {e.From:0.00}-{e.To:0.00} s"))} cut out");
        }

        // ------------------------------------------------------------------------------------------- controllers

        /// <summary>
        /// A standing person's controller at <paramref name="path"/>, built afresh in place so what refers to it keeps it.
        /// With <paramref name="idleBack"/> the idle is two clips in turn, as the blacksmith's blow and its rebound.
        /// </summary>
        public static AnimatorController Person(string path, string idleUse, bool female, string idleBack = null)
        {
            var controller = Fresh(path);
            controller.AddParameter("Talking", AnimatorControllerParameterType.Bool);
            FightParameters(controller);
            var machine = controller.layers[0].stateMachine;
            var idle = State(machine, "Idle", Get(idleUse, female), new Vector3(300f, 0f));
            var walk = State(machine, "Walk", Get("Walk", female), new Vector3(300f, 120f));
            machine.defaultState = idle;
            var talk = State(machine, "Talk", Get("Talk", female), new Vector3(300f, -120f));
            idle.iKOnFeet = walk.iKOnFeet = talk.iKOnFeet = true; // the feet on the ground on every body
            if (idleBack != null)
            {
                var back = State(machine, "IdleBack", Get(idleBack, female), new Vector3(560f, -60f));
                back.iKOnFeet = true;
                Exit(idle, back, 1f, 0.06f);
                Exit(back, idle, 1f, 0.04f);
                On(back, talk, 0.25f, "Talking", true);
            }
            On(idle, talk, 0.25f, "Talking", true);
            On(talk, idle, 0.25f, "Talking", false);
            Fight(controller, idle, female);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        /// <summary>
        /// The player's controller at <paramref name="path"/>. Its movement states have no transitions of their own:
        /// <c>PlayerLocomotion</c> owns the state and cross-fades to it the moment it changes, so no clip is waited out.
        /// <list type="bullet">
        /// <item><c>Locomotion</c>, a 2D freeform directional blend on MoveX and MoveZ, the body's own velocity on the scale
        /// 0–3: the idle at the centre, the eight walks at 1, the eight runs at 2 and the five sprints at 3 (forward, the
        /// diagonals forward and the sides), each pace's clips at its time scale;</item>
        /// <item><c>TurnLeft</c> and <c>TurnRight</c>, a turn on the spot;</item>
        /// <item><c>Crouch</c>, the stealth blend at Speed 0 and 1 (crouched idle, sneak), its pace on the <c>Pace</c> float,
        /// the sneak's own pace kept in <c>SneakPace</c>'s default (m/s, measured) for the code to match;</item>
        /// <item><c>Fall</c>, the fall's loop; <c>JumpLand</c>, the landing;</item>
        /// <item>the fight, entered from <c>Locomotion</c> while Fighting.</item>
        /// </list>
        /// Foot IK on every standing state, so the feet meet the ground on every body.
        /// </summary>
        public static AnimatorController Player(string path, IReadOnlyDictionary<string, float> timeScales, float sneakPace, float turnRate)
        {
            var controller = Fresh(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveZ", AnimatorControllerParameterType.Float);
            controller.AddParameter(new AnimatorControllerParameter { name = "Grounded", type = AnimatorControllerParameterType.Bool, defaultBool = true });
            controller.AddParameter(new AnimatorControllerParameter { name = "Pace", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
            controller.AddParameter(new AnimatorControllerParameter { name = "SneakPace", type = AnimatorControllerParameterType.Float, defaultFloat = sneakPace });
            controller.AddParameter(new AnimatorControllerParameter { name = "TurnRate", type = AnimatorControllerParameterType.Float, defaultFloat = turnRate });
            FightParameters(controller);
            var machine = controller.layers[0].stateMachine;

            var locomotion = controller.CreateBlendTreeInController("Locomotion", out var tree, 0);
            tree.blendType = BlendTreeType.FreeformDirectional2D;
            tree.blendParameter = "MoveX";
            tree.blendParameterY = "MoveZ";
            var paces = new List<(string Use, Vector2 At)> { ("Idle", Vector2.zero) };
            paces.AddRange(Moves().Select(m => (m.Use, m.Way * m.Gait)));
            foreach (var (use, at) in paces)
            {
                tree.AddChild(Get(use), at);
            }

            var children = tree.children;
            for (var i = 0; i < children.Length; i++)
            {
                children[i].timeScale = timeScales.TryGetValue(paces[i].Use, out var scale) ? scale : 1f;
            }

            tree.children = children;
            locomotion.iKOnFeet = true;
            machine.defaultState = locomotion;
            foreach (var (turn, y) in new[] { ("TurnLeft", 240f), ("TurnRight", 300f) })
            {
                var state = State(machine, turn, Get(turn), new Vector3(300f, y));
                state.iKOnFeet = true;
                state.speedParameterActive = true;
                state.speedParameter = "TurnRate";
            }

            var crouch = controller.CreateBlendTreeInController("Crouch", out var low, 0);
            low.blendParameter = "Speed";
            low.useAutomaticThresholds = false;
            low.AddChild(Get("Crouch"), 0f);
            low.AddChild(Get("Sneak"), 1f);
            crouch.iKOnFeet = true;
            crouch.speedParameterActive = true;
            crouch.speedParameter = "Pace";

            State(machine, "Fall", Get("Fall"), new Vector3(560f, 0f));
            var land = State(machine, "JumpLand", Get("JumpLand"), new Vector3(560f, 120f));
            land.iKOnFeet = true;
            Fight(controller, locomotion, false);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        /// <summary>
        /// The player's moving clips: each gait (walk 1, run 2, sprint 3, the blend's scale) in each direction the table
        /// has, with the direction in the body's own frame, x to the right and y forward.
        /// </summary>
        public static IEnumerable<(string Use, int Gait, Vector2 Way)> Moves()
        {
            var d = Mathf.Sqrt(0.5f);
            var ways = new (string Suffix, Vector2 Way)[]
            {
                (string.Empty, new Vector2(0f, 1f)), ("ForwardRight", new Vector2(d, d)), ("Right", new Vector2(1f, 0f)), ("BackRight", new Vector2(d, -d)),
                ("Back", new Vector2(0f, -1f)), ("BackLeft", new Vector2(-d, -d)), ("Left", new Vector2(-1f, 0f)), ("ForwardLeft", new Vector2(-d, d)),
            };
            var gaits = new[] { "Walk", "Run", "Sprint" };
            for (var g = 0; g < gaits.Length; g++)
            {
                foreach (var (suffix, way) in ways)
                {
                    var use = gaits[g] + suffix;
                    if (Clips.Any(c => c.Use == use))
                    {
                        yield return (use, g + 1, way);
                    }
                }
            }
        }

        /// <summary>How fast a clip carries its body over the ground in its own curves, x to the side and y forward (m/s).</summary>
        public static Vector2 Drift(AnimationClip clip)
        {
            var travel = SliceCharacterRigs.PoseAt(clip, clip.length).bodyPosition - SliceCharacterRigs.PoseAt(clip, 0f).bodyPosition;
            return new Vector2(travel.x, travel.z) / clip.length;
        }

        /// <summary>
        /// Degrees a second a clip turns its body, read from its root rotation, which the import may discard (left turns
        /// negative).
        /// </summary>
        public static float TurnSpeed(AnimationClip clip)
        {
            const int samples = 60;
            var total = 0f;
            var before = SliceCharacterRigs.PoseAt(clip, 0f).bodyRotation * Vector3.forward;
            for (var i = 1; i <= samples; i++)
            {
                var now = SliceCharacterRigs.PoseAt(clip, clip.length * i / samples).bodyRotation * Vector3.forward;
                total += Vector3.SignedAngle(new Vector3(before.x, 0f, before.z), new Vector3(now.x, 0f, now.z), Vector3.up);
                before = now;
            }

            return total / clip.length;
        }

        /// <summary>
        /// An override of <paramref name="controller"/> at <paramref name="path"/> playing each Kevin Iglesias clip in its
        /// women's take; Quaternius' clips have one take and stay.
        /// </summary>
        public static AnimatorOverrideController WomanOverride(AnimatorController controller, string path)
        {
            var woman = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            if (woman == null)
            {
                woman = new AnimatorOverrideController(controller);
                AssetDatabase.CreateAsset(woman, path);
            }

            woman.runtimeAnimatorController = controller;
            var takes = Clips.Where(c => c.Kevin).ToDictionary(c => Get(c.Use), c => Get(c.Use, true));
            var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            woman.GetOverrides(pairs);
            woman.ApplyOverrides(pairs.Select(p => new KeyValuePair<AnimationClip, AnimationClip>(p.Key,
                takes.TryGetValue(p.Key, out var take) ? take : null)).ToList());
            EditorUtility.SetDirty(woman);
            return woman;
        }

        private static void FightParameters(AnimatorController controller)
        {
            controller.AddParameter("Fighting", AnimatorControllerParameterType.Bool);
            controller.AddParameter(new AnimatorControllerParameter { name = "Weapon", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Down", AnimatorControllerParameterType.Bool);
        }

        /// <summary>
        /// The fight on <paramref name="home"/>: into the guard while Fighting, the blow on Attack, the hit on Hit from
        /// anywhere unless Down, and the death while Down, out of which only a body no longer down comes back.
        /// </summary>
        private static void Fight(AnimatorController controller, AnimatorState home, bool female)
        {
            var machine = controller.layers[0].stateMachine;
            var guard = WeaponBlend(controller, "Fight", Guards, female);
            var blow = WeaponBlend(controller, "Attack", Blows, female);
            var hit = State(machine, "Hit", Get("Hit", female), new Vector3(820f, 120f));
            var die = State(machine, "Die", Get("Die", female), new Vector3(820f, 240f));
            On(home, guard, 0.25f, "Fighting", true);
            On(guard, home, 0.3f, "Fighting", false);
            Transition(guard, blow, 0.1f).AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            Exit(blow, guard, 0.9f, 0.15f);
            Exit(hit, guard, 0.85f, 0.15f);

            var toHit = machine.AddAnyStateTransition(hit);
            toHit.duration = 0.05f;
            toHit.AddCondition(AnimatorConditionMode.If, 0f, "Hit");
            toHit.AddCondition(AnimatorConditionMode.IfNot, 0f, "Down");
            var toDie = machine.AddAnyStateTransition(die);
            toDie.duration = 0.15f;
            toDie.canTransitionToSelf = false;
            toDie.AddCondition(AnimatorConditionMode.If, 0f, "Down");
            On(die, home, 0.3f, "Down", false);
        }

        private static AnimatorState WeaponBlend(AnimatorController controller, string name, string[] uses, bool female)
        {
            var state = controller.CreateBlendTreeInController(name, out var tree, 0);
            tree.blendParameter = "Weapon";
            tree.useAutomaticThresholds = false;
            for (var i = 0; i < uses.Length; i++)
            {
                tree.AddChild(Get(uses[i], female), i + 1);
            }

            return state;
        }

        /// <summary>The controller at <paramref name="path"/> emptied of its states, parameters and blend trees, or a new one.</summary>
        private static AnimatorController Fresh(string path)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
                AssetDatabase.Refresh();
                return AnimatorController.CreateAnimatorControllerAtPath(path);
            }

            while (controller.layers.Length > 1)
            {
                controller.RemoveLayer(1);
            }

            var machine = controller.layers[0].stateMachine;
            foreach (var t in machine.anyStateTransitions)
            {
                machine.RemoveAnyStateTransition(t);
            }

            foreach (var t in machine.entryTransitions)
            {
                machine.RemoveEntryTransition(t);
            }

            foreach (var s in machine.states)
            {
                machine.RemoveState(s.state);
            }

            foreach (var m in machine.stateMachines)
            {
                machine.RemoveStateMachine(m.stateMachine);
            }

            foreach (var p in controller.parameters)
            {
                controller.RemoveParameter(p);
            }

            foreach (var tree in AssetDatabase.LoadAllAssetsAtPath(path).OfType<BlendTree>().ToList())
            {
                Object.DestroyImmediate(tree, true);
            }

            return controller;
        }

        private static AnimatorState State(AnimatorStateMachine machine, string name, Motion motion, Vector3 at)
        {
            var state = machine.AddState(name, at);
            state.motion = motion;
            return state;
        }

        private static AnimatorStateTransition Transition(AnimatorState from, AnimatorState to, float duration)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = duration;
            return t;
        }

        private static void On(AnimatorState from, AnimatorState to, float duration, string parameter, bool value) =>
            Transition(from, to, duration).AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);

        private static void Exit(AnimatorState from, AnimatorState to, float exitTime, float duration)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = true;
            t.exitTime = exitTime;
            t.duration = duration;
        }
    }
}

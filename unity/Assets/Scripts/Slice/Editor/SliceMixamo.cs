using System;
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
    /// The Mixamo clips of ANIMATION_PIPELINE_PLAN.md A1 and A2 (owner, 2026-11-10): the files the owner downloaded
    /// Without Skin on the player woman's body, each imported as Humanoid with its own avatar and one clip named by its
    /// use, and the controllers that play them.
    /// <list type="bullet">
    /// <item>Every clip is in place: root rotation, height and ground position baked into the pose as the file has them
    /// (based upon Original), loop time and loop pose on the cycles. The jumps alone keep their feet on the root (height
    /// based upon Feet), since the player's own jump lifts the body; the still jump is cut at its takeoff and landing,
    /// measured from the feet, into JumpUp, JumpAir and JumpLand.</item>
    /// <item>A standing person's controller: the idle (a Warden's at attention), the walk, the talk while Talking, and
    /// the fight: a guard and a blow by Weapon (1 sword, axe or mace; 2 greatsword, greataxe or maul; 3 dagger;
    /// 4 pistol; 5 rifle), the hit on Hit and the death while Down.</item>
    /// <item>The player's: the locomotion blend at Speed 0–3 (idle, walk, run, sprint), each clip sped to the player's
    /// pace, the jump, and the same fight.</item>
    /// </list>
    /// </summary>
    public static class SliceMixamo
    {
        public const string Folder = "Assets/Animation/Mixamo";
        public const string ControllerFolder = "Assets/Animation/Controllers";
        public const string WardenController = ControllerFolder + "/Warden.controller";
        public const string WardenWomanController = ControllerFolder + "/WardenWoman.controller";
        private const string Body = "human_body_3d_model@";
        private const string StillJump = "Jump Still In Place";

        /// <summary>Authoring default, m: a foot this far over its standing height is off the ground.</summary>
        private const float Lift = 0.05f;

        /// <summary>Authoring default, frames at 30 fps: the push before the takeoff that JumpUp keeps.</summary>
        private const int Push = 6;

        /// <summary>Every clip by its use, its Mixamo file and whether it loops (the plan's table under A2).</summary>
        public static readonly (string Use, string File, bool Loop)[] Clips =
        {
            ("Idle", "Breathing Idle", true),
            ("IdleHappy", "Happy Idle", true),
            ("Walk", "Walking In Place", true),
            ("Run", "Running In Place", true),
            ("Sprint", "Fast Run In Place", true),
            ("TurnLeft", "Left Turn", false),
            ("TurnRight", "Right Turn", false),
            ("Jump", StillJump, false),
            ("JumpForward", "Jump Forward In Place", false),
            ("JumpBack", "Jump Backward In Place", false),
            ("Attention", "Male Standing At Attention Pose", true),
            ("AttentionWoman", "Female Standing At Attention Pose", true),
            ("Talk", "Talking 1", true),
            ("Talk2", "Talking 2", true),
            ("Argue", "Standing Arguing 1", true),
            ("Argue2", "Standing Arguing 2", true),
            ("Nod", "Head Nod Yes", false),
            ("Wave", "Waving 1", false),
            ("Wave2", "Waving 2", false),
            ("Point", "Pointing", false),
            ("Sit", "Sitting Idle", true),
            ("SitAsk", "Sitting Asking Attention", true),
            ("SitGround", "Sitting On The Ground Idle", true),
            ("StandToSit", "Stand To Sit", false),
            ("SitToStand", "Sit To Stand", false),
            ("Drink", "Drinking", false),
            ("Sneak", "Sneak Walk In Place", true),
            ("CrouchWalk", "Crouched Walking In Place", true),
            ("Crouch", "Male Crouch Idle", true),
            ("StandToCrouch", "Standing To Crouched", false),
            ("CrouchToStand", "Crouch To Stand", false),
            ("PickUp", "Picking Up From The Ground", false),
            ("Kneel", "Kneeling Idle", true),
            ("KneelOne", "On One Kneel Idle", true),
            ("Gather", "Gathering Objects", true),
            ("StartWalking", "Start Walking", false),
            ("SwordIdle", "Sword And Shield Idle", true),
            ("SwordSlash", "Sword And Shield Slash", false),
            ("BlockStart", "Sword And Shield Block Start", false),
            ("BlockIdle", "Sword And Shield Block Idle", true),
            ("BlockEnd", "Sword And Shield Block End", false),
            ("GreatswordIdle", "Great Sword Idle", true),
            ("GreatswordSlash", "Great Sword Slash", false),
            ("Stab", "Stabbing", false),
            ("PistolIdle", "Pistol Idle", true),
            ("PistolShot", "Shooting Pistol", false),
            ("RifleIdle", "Rifle Aiming Idle", true),
            ("RifleShot", "Shooting Rifle", false),
            ("Cast1H", "Standing 1H Magic Attack 01", false),
            ("Cast2H", "Standing 2H Magic Area Attack 02", false),
            ("Casting", "Spell Casting", false),
            ("Hit", "Hit Reaction", false),
            ("Die", "Dying", false),
            ("DieBack", "Dying Backwards", false),
            ("DieFront", "Death From The Front", false),
            ("StepBack", "Walking Backwards In Place", true),
            ("LieDown", "Lying Down", false),
            ("Sleep", "Laying Sleeping Idle", true),
            ("StandUp", "Stand Up", false),
            ("GetUp", "Standing Up From The Ground", false),
        };

        /// <summary>The still jump's three parts, cut from its file at the measured takeoff and landing.</summary>
        public static readonly string[] JumpParts = { "JumpUp", "JumpAir", "JumpLand" };

        /// <summary>The guard and the blow of each Weapon value, 1 to 5; the dagger guards as a sword without its shield.</summary>
        private static readonly string[] Guards = { "SwordIdle", "GreatswordIdle", "SwordIdle", "PistolIdle", "RifleIdle" };

        private static readonly string[] Blows = { "SwordSlash", "GreatswordSlash", "Stab", "PistolShot", "RifleShot" };

        public static string PathOf(string use)
        {
            var file = JumpParts.Contains(use) ? StillJump : Clips.FirstOrDefault(c => c.Use == use).File;
            return file == null ? null : $"{Folder}/{Body}{file}.fbx";
        }

        /// <summary>The imported clip of <paramref name="use"/>; throws if it is not there, so a batch stops at the first gap.</summary>
        public static AnimationClip Get(string use)
        {
            var path = PathOf(use);
            var clip = path == null ? null : AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => c.name == use);
            if (clip == null)
            {
                throw new InvalidOperationException($"[SliceMixamo] no clip '{use}' in {path ?? "the table"}: import the Mixamo files first");
            }

            return clip;
        }

        // ------------------------------------------------------------------------------------------- import

        /// <summary>Every file of the table imported as Humanoid with one clip named by its use; the still jump cut in three.</summary>
        public static void Import(StringBuilder report)
        {
            foreach (var (use, file, loop) in Clips)
            {
                var path = $"{Folder}/{Body}{file}.fbx";
                if (!(AssetImporter.GetAtPath(path) is ModelImporter importer))
                {
                    throw new InvalidOperationException($"[SliceMixamo] {use}: no file at {path}");
                }

                Set(importer, Take(importer, use, loop, use.StartsWith("Jump")));
            }

            CutJump(report);
            var broken = 0;
            foreach (var use in Clips.Select(c => c.Use).Concat(JumpParts))
            {
                var clip = Get(use);
                var curves = AnimationUtility.GetCurveBindings(clip).Count(b => b.type == typeof(Animator));
                var avatar = AssetDatabase.LoadAllAssetsAtPath(PathOf(use)).OfType<Avatar>().FirstOrDefault();
                var sound = clip.humanMotion && avatar != null && avatar.isValid && avatar.isHuman && curves > 0;
                broken += sound ? 0 : 1;
                report.AppendLine($"{use}: {clip.length:0.00} s{(clip.isLooping ? ", loops" : string.Empty)}, {curves} muscle and root curves" +
                                  (sound ? string.Empty : $", NOT HUMANOID (avatar {(avatar == null ? "missing" : avatar.isValid ? "valid" : "invalid")})"));
            }

            if (broken > 0)
            {
                throw new InvalidOperationException($"[SliceMixamo] {broken} clips did not import as Humanoid:\n{report}");
            }
        }

        /// <summary>
        /// The still jump cut where its feet leave the ground and meet it again, measured on the player man posed in its
        /// frames: JumpUp, the push before the takeoff; JumpAir, the flight; JumpLand, from the landing to the end.
        /// </summary>
        private static void CutJump(StringBuilder report)
        {
            var whole = Get("Jump");
            var animator = SlicePeopleModels.Fitted(SlicePeopleModels.PlayerMan, whole);
            var root = animator.transform.parent;
            var left = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var right = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            var rate = whole.frameRate;
            var frames = Mathf.RoundToInt(whole.length * rate);

            float Feet(int frame)
            {
                SlicePeopleModels.Pose(animator, whole, frame / rate);
                return Mathf.Min(root.InverseTransformPoint(left.position).y, root.InverseTransformPoint(right.position).y);
            }

            var ground = Feet(0);
            int takeoff = -1, landing = -1;
            var highest = 0f;
            for (var f = 1; f <= frames && landing < 0; f++)
            {
                var feet = Feet(f) - ground;
                highest = Mathf.Max(highest, feet);
                if (feet > Lift && takeoff < 0)
                {
                    takeoff = f;
                }
                else if (feet <= Lift && takeoff >= 0)
                {
                    landing = f;
                }
            }

            Object.DestroyImmediate(root.gameObject);
            if (takeoff < 0 || landing < 0)
            {
                throw new InvalidOperationException($"[SliceMixamo] the still jump's feet never left the ground by {Lift} m (highest {highest:0.00} m)");
            }

            var importer = (ModelImporter)AssetImporter.GetAtPath(PathOf("Jump"));
            var first = importer.defaultClipAnimations[0].firstFrame;
            var last = importer.defaultClipAnimations[0].lastFrame;
            Set(importer,
                Take(importer, "Jump", false, true),
                Take(importer, "JumpUp", false, true, first + Mathf.Max(0, takeoff - Push), first + takeoff),
                Take(importer, "JumpAir", false, true, first + takeoff, first + landing),
                Take(importer, "JumpLand", false, true, first + landing, last));
            report.AppendLine($"The still jump: takeoff at frame {takeoff}, landing at {landing} of {frames} (measured, feet over {Lift} m; " +
                              $"highest {highest:0.00} m on the player man)");
        }

        private static ModelImporterClipAnimation Take(ModelImporter importer, string name, bool loop, bool feet, float first = -1f, float last = -1f)
        {
            var clip = importer.defaultClipAnimations[0];
            clip.name = name;
            if (first >= 0f)
            {
                clip.firstFrame = first;
                clip.lastFrame = last;
            }

            clip.loopTime = loop;
            clip.loopPose = loop;
            clip.lockRootRotation = true;
            clip.keepOriginalOrientation = true;
            clip.lockRootHeightY = true;
            clip.keepOriginalPositionY = !feet;
            clip.heightFromFeet = feet;
            clip.lockRootPositionXZ = true;
            clip.keepOriginalPositionXZ = true;
            return clip;
        }

        private static void Set(ModelImporter importer, params ModelImporterClipAnimation[] clips)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        // ------------------------------------------------------------------------------------------- controllers

        /// <summary>A standing person's controller at <paramref name="path"/>, built afresh in place so what refers to it keeps it.</summary>
        public static AnimatorController Person(string path, string idleUse)
        {
            var controller = Fresh(path);
            controller.AddParameter("Talking", AnimatorControllerParameterType.Bool);
            FightParameters(controller);
            var machine = controller.layers[0].stateMachine;
            var idle = State(machine, "Idle", Get(idleUse), new Vector3(300f, 0f));
            State(machine, "Walk", Get("Walk"), new Vector3(300f, 120f));
            machine.defaultState = idle;
            var talk = State(machine, "Talk", Get("Talk"), new Vector3(300f, -120f));
            On(idle, talk, 0.25f, "Talking", true);
            On(talk, idle, 0.25f, "Talking", false);
            Fight(controller, idle);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        /// <summary>
        /// The player's controller at <paramref name="path"/>: the locomotion blend at Speed 0, 1, 2 and 3, each clip at
        /// its time scale; the jump's three parts, the flight at <paramref name="airSpeed"/>; and the fight.
        /// </summary>
        public static AnimatorController Player(string path, float[] timeScales, float airSpeed)
        {
            var controller = Fresh(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter(new AnimatorControllerParameter { name = "Grounded", type = AnimatorControllerParameterType.Bool, defaultBool = true });
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            FightParameters(controller);
            var machine = controller.layers[0].stateMachine;

            var locomotion = controller.CreateBlendTreeInController("Locomotion", out var tree, 0);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            var uses = new[] { "Idle", "Walk", "Run", "Sprint" };
            for (var i = 0; i < uses.Length; i++)
            {
                tree.AddChild(Get(uses[i]), i);
            }

            var children = tree.children;
            for (var i = 0; i < children.Length; i++)
            {
                children[i].timeScale = timeScales[i];
            }

            tree.children = children;
            machine.defaultState = locomotion;

            var up = State(machine, "JumpBegin", Get("JumpUp"), new Vector3(560f, -120f));
            var air = State(machine, "Fall", Get("JumpAir"), new Vector3(560f, 0f));
            air.speed = airSpeed;
            var land = State(machine, "JumpLand", Get("JumpLand"), new Vector3(560f, 120f));
            Transition(locomotion, up, 0.05f).AddCondition(AnimatorConditionMode.If, 0f, "Jump");
            Transition(up, air, 0.1f).AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
            Exit(up, air, 0.95f, 0.1f);
            Transition(air, land, 0.05f).AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
            Exit(land, locomotion, 0.7f, 0.15f);
            Fight(controller, locomotion);
            EditorUtility.SetDirty(controller);
            return controller;
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
        private static void Fight(AnimatorController controller, AnimatorState home)
        {
            var machine = controller.layers[0].stateMachine;
            var guard = WeaponBlend(controller, "Fight", Guards);
            var blow = WeaponBlend(controller, "Attack", Blows);
            var hit = State(machine, "Hit", Get("Hit"), new Vector3(820f, 120f));
            var die = State(machine, "Die", Get("Die"), new Vector3(820f, 240f));
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

        private static AnimatorState WeaponBlend(AnimatorController controller, string name, string[] uses)
        {
            var state = controller.CreateBlendTreeInController(name, out var tree, 0);
            tree.blendParameter = "Weapon";
            tree.useAutomaticThresholds = false;
            for (var i = 0; i < uses.Length; i++)
            {
                tree.AddChild(Get(uses[i]), i + 1);
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

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
    /// <item>Every file shares the player woman's avatar (<see cref="SourceAvatar"/>), since a file Without Skin has no
    /// rest pose of its own. Every clip is in place: root rotation and ground position baked into the pose, its height
    /// based upon its feet, loop time and loop pose on the cycles; the still jump is cut at its takeoff and landing,
    /// measured from the feet, into JumpUp, JumpAir and a short JumpLand.</item>
    /// <item>A standing person's controller: the idle (a Warden's at attention), the walk, the talk while Talking, and
    /// the fight: a guard and a blow by Weapon (1 sword, axe or mace; 2 greatsword, greataxe or maul; 3 dagger;
    /// 4 pistol; 5 rifle), the hit on Hit and the death while Down.</item>
    /// <item>The player's: the locomotion blend at Speed 0–3 (idle, walk, run, sprint), each clip sped to the player's
    /// pace, the jump, and the same fight.</item>
    /// <item>The clips Mixamo lacks (A3: Mining, Chopping, Hammering, Surrender), made by
    /// <c>tools/blender/animation/a3_clips.py</c> on the same armature and imported the same way from
    /// <see cref="BlenderFolder"/>. The blacksmith hammers as its idle.</item>
    /// </list>
    /// </summary>
    public static class SliceMixamo
    {
        public const string Folder = "Assets/Animation/Mixamo";
        public const string ControllerFolder = "Assets/Animation/Controllers";
        public const string WardenController = ControllerFolder + "/Warden.controller";
        public const string WardenWomanController = ControllerFolder + "/WardenWoman.controller";
        public const string BlacksmithController = ControllerFolder + "/Blacksmith.controller";

        /// <summary>The clips made in Blender (A3), one file a clip named by its use.</summary>
        public const string BlenderFolder = "Assets/Animation/Blender";
        private const string Body = "human_body_3d_model@";
        private const string StillJump = "Jump Still In Place";

        /// <summary>Authoring default, m: a foot this far over its standing height is off the ground.</summary>
        private const float Lift = 0.05f;

        /// <summary>Authoring default, frames at 30 fps: the push before the takeoff that JumpUp keeps.</summary>
        private const int Push = 6;

        /// <summary>
        /// Authoring default, frames at 30 fps: the landing JumpLand keeps, the knees' give and the first rise (0.33 s).
        /// The file's whole landing, 26 frames, held the walk back (owner's play test of 2026-11-11).
        /// </summary>
        private const int LandFrames = 10;

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

        /// <summary>The clips Mixamo lacks, made in Blender by <c>tools/blender/animation/a3_clips.py</c> (A3), and whether each loops.</summary>
        public static readonly (string Use, bool Loop)[] Made =
        {
            ("Mine", true),
            ("Chop", true),
            ("Hammer", true),
            ("Yield", false),
        };

        /// <summary>The still jump's three parts, cut from its file at the measured takeoff and landing.</summary>
        public static readonly string[] JumpParts = { "JumpUp", "JumpAir", "JumpLand" };

        /// <summary>The guard and the blow of each Weapon value, 1 to 5; the dagger guards as a sword without its shield.</summary>
        private static readonly string[] Guards = { "SwordIdle", "GreatswordIdle", "SwordIdle", "PistolIdle", "RifleIdle" };

        private static readonly string[] Blows = { "SwordSlash", "GreatswordSlash", "Stab", "PistolShot", "RifleShot" };

        public static string PathOf(string use)
        {
            if (Made.Any(m => m.Use == use))
            {
                return $"{BlenderFolder}/{use}.fbx";
            }

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
                throw new InvalidOperationException($"[SliceMixamo] no clip '{use}' in {path ?? "the table"}: import the Mixamo and Blender files first");
            }

            return clip;
        }

        // ------------------------------------------------------------------------------------------- import

        /// <summary>
        /// Every file of the table and every Blender clip imported as Humanoid with one clip named by its use; the still
        /// jump cut in three.
        /// </summary>
        public static void Import(StringBuilder report)
        {
            foreach (var (use, loop) in Clips.Select(c => (c.Use, c.Loop)).Concat(Made))
            {
                var path = PathOf(use);
                if (!(AssetImporter.GetAtPath(path) is ModelImporter importer))
                {
                    throw new InvalidOperationException($"[SliceMixamo] {use}: no file at {path}");
                }

                Rig(importer);
                if (OwnRest(importer))
                {
                    TPose(importer, report);
                }

                Set(importer, Take(importer, use, loop, use.StartsWith("Jump")));
            }

            CutJump(report);
            var broken = 0;
            foreach (var use in Clips.Select(c => c.Use).Concat(JumpParts).Concat(Made.Select(m => m.Use)))
            {
                var clip = Get(use);
                var curves = AnimationUtility.GetCurveBindings(clip).Count(b => b.type == typeof(Animator));
                var shared = (AssetImporter.GetAtPath(PathOf(use)) as ModelImporter)?.sourceAvatar;
                var avatar = shared != null ? shared : AssetDatabase.LoadAllAssetsAtPath(PathOf(use)).OfType<Avatar>().FirstOrDefault();
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
                Take(importer, "JumpLand", false, true, first + landing, Mathf.Min(last, first + landing + LandFrames)));
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
            // Every clip's height from its feet (owner's play test of 2026-11-11: the people's feet hung over the ground):
            // the body stands so the lower foot meets the root, on every body whatever its legs' length.
            clip.keepOriginalPositionY = false;
            clip.heightFromFeet = true;
            clip.lockRootPositionXZ = true;
            clip.keepOriginalPositionXZ = true;
            return clip;
        }

        /// <summary>
        /// The avatar every clip file shares: the player woman's model's own, made from its skin in a true T-pose. The files
        /// were downloaded Without Skin on that body, so they carry its skeleton bone for bone but no bind pose, and an
        /// avatar made from such a file takes its first frame for its rest pose: the walk's knees bent at 115°, the run's
        /// at 79°, so every frame came across offset by its first and the knees and ankles bent wrongly on every body
        /// (measured by <see cref="SliceAnimationCheck"/>, 2026-11-11).
        /// </summary>
        public static Avatar SourceAvatar =>
            AssetDatabase.LoadAllAssetsAtPath(SlicePeopleModels.PlayerWoman.Model).OfType<Avatar>().FirstOrDefault(a => a.isHuman) ??
            throw new InvalidOperationException("[SliceMixamo] the player woman's model has no human avatar");

        /// <summary>
        /// The file's rig set to share <see cref="SourceAvatar"/> and imported so, before its clips are read: a file whose
        /// last import failed has no default clip to cut from.
        /// </summary>
        private static void Rig(ModelImporter importer)
        {
            var own = OwnRest(importer);
            var source = own ? null : SourceAvatar;
            if (importer.animationType == ModelImporterAnimationType.Human && importer.preserveHierarchy == !own &&
                importer.avatarSetup == (own ? ModelImporterAvatarSetup.CreateFromThisModel : ModelImporterAvatarSetup.CopyFromOther) &&
                importer.sourceAvatar == source && importer.defaultClipAnimations.Length > 0)
            {
                return;
            }

            Avatar(importer);
            importer.importAnimation = true;
            importer.clipAnimations = Array.Empty<ModelImporterClipAnimation>();
            importer.SaveAndReimport();
            if (importer.defaultClipAnimations.Length == 0)
            {
                throw new InvalidOperationException($"[SliceMixamo] {importer.assetPath} imports no clip with the shared avatar: see the log's Rig Error");
            }
        }

        /// <summary>
        /// The Blender clips (A3) keep an avatar of their own: Blender writes the armature's rest pose into the file, and
        /// its bones' lengths differ from the player woman's by up to 0.4 m, so Unity refuses to copy hers onto them.
        /// </summary>
        private static bool OwnRest(ModelImporter importer) => importer.assetPath.StartsWith(BlenderFolder + "/", StringComparison.Ordinal);

        /// <summary>
        /// A Blender clip's avatar given the player woman's T-pose for its rest (owner's play test of 2026-11-11: the
        /// blacksmith's arm bent unnaturally). The script builds its clips on Breathing Idle's armature, so the file's rest
        /// is that idle's first pose, arms down, and an avatar made from it took the hanging arm for a T-pose: every frame
        /// came across with the arm turned by the difference, as the Mixamo files did before they shared her avatar. Each
        /// bone of the rest, from the hips out, is pointed at its child as her T-pose points it, after the two bodies'
        /// frames are matched (Unity's Enforce T-Pose, by direction), and the result is written as the avatar's skeleton.
        /// </summary>
        private static void TPose(ModelImporter importer, StringBuilder report)
        {
            var description = importer.humanDescription;
            if (description.human == null || description.human.Length == 0)
            {
                throw new InvalidOperationException($"[SliceMixamo] {importer.assetPath} has no human mapping to give a T-pose");
            }

            var file = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(importer.assetPath));
            var woman = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SlicePeopleModels.PlayerWoman.Model));
            try
            {
                Transform Find(GameObject root, string name) => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
                int Depth(Transform t)
                {
                    var d = 0;
                    for (var p = t.parent; p != null; p = p.parent)
                    {
                        d++;
                    }

                    return d;
                }

                var bones = description.human.Select(h => (Human: h.humanName, File: Find(file, h.boneName), Woman: Find(woman, h.boneName)))
                    .Where(b => b.File != null && b.Woman != null).ToDictionary(b => b.Human);

                // The two bodies' frames: up from the hips to the head, right from the left thigh to the right. Blender's
                // export may turn the whole armature, so the woman's directions are carried into the file's frame first.
                Quaternion Frame(Func<string, Vector3> at)
                {
                    var up = (at("Head") - at("Hips")).normalized;
                    var right = (at("RightUpperLeg") - at("LeftUpperLeg")).normalized;
                    return Quaternion.LookRotation(Vector3.Cross(right, up).normalized, up);
                }

                var toFile = Frame(n => bones[n].File.position) * Quaternion.Inverse(Frame(n => bones[n].Woman.position));

                // Each bone in turn from the hips out, pointed at its child as the woman's T-pose points it: Unity's own
                // Enforce T-Pose, by direction.
                string Child(string human) => human switch
                {
                    "Spine" => "Chest",
                    "Chest" => bones.ContainsKey("UpperChest") ? "UpperChest" : "Neck",
                    "UpperChest" => "Neck",
                    "Neck" => "Head",
                    "LeftShoulder" => "LeftUpperArm",
                    "RightShoulder" => "RightUpperArm",
                    "LeftUpperArm" => "LeftLowerArm",
                    "RightUpperArm" => "RightLowerArm",
                    "LeftLowerArm" => "LeftHand",
                    "RightLowerArm" => "RightHand",
                    "LeftHand" => "Left Middle Proximal",
                    "RightHand" => "Right Middle Proximal",
                    "LeftUpperLeg" => "LeftLowerLeg",
                    "RightUpperLeg" => "RightLowerLeg",
                    "LeftLowerLeg" => "LeftFoot",
                    "RightLowerLeg" => "RightFoot",
                    "LeftFoot" => "LeftToes",
                    "RightFoot" => "RightToes",
                    _ => null,
                };

                var worst = 0f;
                foreach (var b in bones.Values.OrderBy(b => Depth(b.File)))
                {
                    var child = Child(b.Human);
                    if (child == null || !bones.TryGetValue(child, out var c))
                    {
                        continue;
                    }

                    var want = toFile * (c.Woman.position - b.Woman.position);
                    var have = c.File.position - b.File.position;
                    worst = Mathf.Max(worst, Vector3.Angle(have, want));
                    b.File.rotation = Quaternion.FromToRotation(have, want) * b.File.rotation;
                }

                description.skeleton = description.skeleton.Select(s =>
                {
                    var t = Find(file, s.name);
                    if (t != null && t != file.transform)
                    {
                        s.rotation = t.localRotation;
                    }

                    return s;
                }).ToArray();
                importer.humanDescription = description;
                importer.SaveAndReimport();
                var down = toFile * Vector3.down;
                var drop = Vector3.Angle(bones["LeftLowerArm"].File.position - bones["LeftUpperArm"].File.position, down);
                report.AppendLine($"{Path.GetFileNameWithoutExtension(importer.assetPath)}: rest turned to the player woman's T-pose " +
                                  $"(a bone turned by up to {worst:0}°; the arm now {drop:0}° from down)");
            }
            finally
            {
                Object.DestroyImmediate(file);
                Object.DestroyImmediate(woman);
            }
        }

        private static void Avatar(ModelImporter importer)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            if (OwnRest(importer))
            {
                importer.preserveHierarchy = false;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.sourceAvatar = null;
                return;
            }

            // A file with one root node has it folded into the file's root, which left Hips one level higher than in the
            // model whose avatar it copies, and Unity refuses the copy: keep the Armature as the model has it.
            importer.preserveHierarchy = true;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = SourceAvatar;
        }

        private static void Set(ModelImporter importer, params ModelImporterClipAnimation[] clips)
        {
            Avatar(importer);
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
            var walk = State(machine, "Walk", Get("Walk"), new Vector3(300f, 120f));
            machine.defaultState = idle;
            var talk = State(machine, "Talk", Get("Talk"), new Vector3(300f, -120f));
            idle.iKOnFeet = walk.iKOnFeet = talk.iKOnFeet = true; // the feet on the ground on every body
            On(idle, talk, 0.25f, "Talking", true);
            On(talk, idle, 0.25f, "Talking", false);
            Fight(controller, idle);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        /// <summary>
        /// The player's controller at <paramref name="path"/> (owner's play test of 2026-11-11). Its movement states have
        /// no transitions of their own: <c>PlayerLocomotion</c> owns the state and cross-fades to it the moment it
        /// changes, so no clip is waited out. The states:
        /// <list type="bullet">
        /// <item><c>Locomotion</c>, the blend at Speed 0, 1, 2 and 3 (idle, walk, run, sprint), each clip at its time scale;</item>
        /// <item><c>Crouch</c>, the stealth blend at Speed 0 and 1 (crouched idle, sneak), its pace on the <c>Pace</c> float,
        /// the sneak's own pace kept in <c>SneakPace</c>'s default (m/s, measured) for the code to match;</item>
        /// <item><c>Fall</c>, the jump's flight at <paramref name="airSpeed"/>, holding its last pose; <c>JumpLand</c>, a short landing;</item>
        /// <item>the fight, entered from <c>Locomotion</c> while Fighting.</item>
        /// </list>
        /// Foot IK on every standing state, so the feet meet the ground on every body.
        /// </summary>
        public static AnimatorController Player(string path, float[] timeScales, float airSpeed, float sneakPace)
        {
            var controller = Fresh(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter(new AnimatorControllerParameter { name = "Grounded", type = AnimatorControllerParameterType.Bool, defaultBool = true });
            controller.AddParameter(new AnimatorControllerParameter { name = "Pace", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
            controller.AddParameter(new AnimatorControllerParameter { name = "SneakPace", type = AnimatorControllerParameterType.Float, defaultFloat = sneakPace });
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
            locomotion.iKOnFeet = true;
            machine.defaultState = locomotion;

            var crouch = controller.CreateBlendTreeInController("Crouch", out var low, 0);
            low.blendParameter = "Speed";
            low.useAutomaticThresholds = false;
            low.AddChild(Get("Crouch"), 0f);
            low.AddChild(Get("Sneak"), 1f);
            crouch.iKOnFeet = true;
            crouch.speedParameterActive = true;
            crouch.speedParameter = "Pace";

            var air = State(machine, "Fall", Get("JumpAir"), new Vector3(560f, 0f));
            air.speed = airSpeed;
            var land = State(machine, "JumpLand", Get("JumpLand"), new Vector3(560f, 120f));
            land.iKOnFeet = true;
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GameDev.Locomotion;
using GameDev.Slice.Kit;
using GameDev.Slice.People;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Every person, the player too, is one of the owner's Tripo people, by role (owner, 2026-11-10; SLICE_ASSET_MAP.md
    /// §1), playing the Mixamo clips of ANIMATION_PIPELINE_PLAN.md A1 and A2 (<see cref="SliceMixamo"/>).
    /// <list type="bullet">
    /// <item>Every clip is Humanoid and in place, the same for men and women; a Warden stands at attention, a woman
    /// Warden in the women's pose. The player's locomotion plays each clip at the player's pace, measured from the
    /// feet.</item>
    /// <item>A model's prefab is fitted in its first pose: turned to face +Z by its shoulders, scaled to its height by
    /// its eyes (eye height 93.6 % of stature, an anthropometric mean) where the avatar maps them, else by its mesh, so
    /// a hat does not shorten a man; its feet on the root.</item>
    /// <item>The Dummies, and the POI casts standing in the townsfolk's walking prefabs, take their role's model in place
    /// (<see cref="PrefabUtility.ReplacePrefabAssetOfPrefabInstance(GameObject, GameObject, PrefabReplacingSettings, InteractionMode)"/>),
    /// so their names, places, NamedPerson and PersonTalk stay. Townsfolk walking by a rich block wear the gentleman's
    /// and the lady's models, by a middling one half of them, by a poor one or the works the working people's.</item>
    /// <item>The blacksmith stands at Bawling Row's anvil beside Edda Brann; walkers whose route now meets the bronze
    /// statue or a new street lamp walk a route planned again.</item>
    /// </list>
    /// Batch, the clips imported, every controller and person built again, the scenes and the player:
    /// -executeMethod GameDev.Slice.EditorTools.SlicePeopleModels.Run
    /// </summary>
    public static class SlicePeopleModels
    {
        public const string Folder = "Assets/Prefabs/Slice/People";
        public const string ManController = SliceMixamo.ControllerFolder + "/Man.controller";
        public const string WomanController = SliceMixamo.ControllerFolder + "/Woman.controller";
        private const string StandIns = "Assets/Animation/StandIn";
        private const string OldWomanPlayer = "Assets/Settings/OverworldPlayerAnimator_Woman.overrideController";
        private const string ManWalkerController = "Assets/Settings/Slice/Townsfolk/SliceTownsfolk.controller";
        private const string WomanWalkerController = "Assets/Settings/Slice/Townsfolk/Woman/SliceTownswoman.controller";
        private const string PlayerController = "Assets/Settings/OverworldPlayerAnimator.controller";
        private const string OutputFolder = "Logs/SliceSurvey";

        /// <summary>
        /// Authoring default: the player's locomotion plays a clip at most this much faster, so a step keeps a human
        /// cadence (the sprint at 4.9 steps a second); Mixamo's in-place strides are shorter than the player's speeds,
        /// and the feet slide the rest.
        /// </summary>
        private const float MostTimeScale = 1.3f;

        /// <summary>Authoring default: eye height as a share of stature (adult mean).</summary>
        private const float EyeShare = 0.936f;

        /// <summary>One of the owner's people: its model, its sex, its height (authoring default, asset map §1).</summary>
        public sealed class Look
        {
            public readonly string Name;
            public readonly string File;
            public readonly bool Female;
            public readonly float Height;

            public Look(string name, string file, bool female, float height)
            {
                Name = name;
                File = file;
                Female = female;
                Height = height;
            }

            public string Model => SliceCharacterRigs.ModelPath(File);

            public string Standing => $"{Folder}/{Name}.prefab";

            /// <summary>The walking prefab: the townsfolk's own for the stylized man and the historical woman.</summary>
            public string Walking => Name == "Townsman" ? SliceTownsfolk.PrefabPath : Name == "Matron" ? SliceTownsfolk.WomanPrefabPath : $"{Folder}/{Name}Walker.prefab";
        }

        public static readonly Look Gentleman = new Look("Gentleman", "vintage_gentleman_3d_model", false, 1.78f);
        public static readonly Look Lady = new Look("Lady", "victorian_dress_3d_model", true, 1.66f);
        public static readonly Look WorkingWoman = new Look("WorkingWoman", "victorian_woman_3d_model", true, 1.63f);
        public static readonly Look Officer = new Look("Officer", "military_officer_3d_model", false, 1.80f);
        public static readonly Look Detective = new Look("Detective", "steampunk_detective_3d_model", false, 1.79f);
        public static readonly Look Blacksmith = new Look("Blacksmith", "medieval_blacksmith_3d_model", false, 1.82f);
        public static readonly Look Townsman = new Look("Townsman", "stylized_male_character_3d_model", false, SliceTownsfolk.Height);
        public static readonly Look Matron = new Look("Matron", "historical_woman_3d_model", true, 1.65f);

        public static readonly Look WardenWoman = new Look("WardenWoman", "military_uniform_3d_model", true, 1.70f);

        public static readonly Look[] All = { Gentleman, Lady, WorkingWoman, Officer, Detective, Blacksmith, Townsman, Matron, WardenWoman };

        /// <summary>The player's two bodies (owner, 2026-11-10): worn by the player's prefabs, with no standing prefab of their own.</summary>
        public static readonly Look PlayerMan = new Look("PlayerMan", "human_figure_3d_model", false, 1.80f);
        public static readonly Look PlayerWoman = new Look("PlayerWoman", "human_body_3d_model", true, 1.68f);
        public const string PlayerWomanPrefab = "Assets/Prefabs/Slice/SlicePlayerWoman.prefab";

        /// <summary>The looks with a walking prefab of their own here; the townsman and the matron walk as the townsfolk.</summary>
        private static readonly Look[] NewWalkers = { Gentleman, Lady, WorkingWoman };

        /// <summary>
        /// Who wears which model (asset map §1): by the person's name in the scene, the Dummies' role names before
        /// SlicePeople named them among them, else by the stand-in colour of their role.
        /// </summary>
        public static Look LookFor(string name, bool female, string colour = null)
        {
            if (name.Contains("Warden") || name == "Sergeant Coyle")
            {
                return female ? WardenWoman : Officer;
            }

            if (name.StartsWith("Development opponent") || name.StartsWith("Cult"))
            {
                return Detective;
            }

            switch (name)
            {
                case "Blacksmith":
                    return Blacksmith;
                case "Josiah Harrow": case "Harrow": case "POI A - Harrow": case "Silas Quill": case "Tobias Penn": case "Mr Ashgrave":
                case "The trader": case "Master of Enchanting":
                    return Gentleman;
                case "Ada Merrow": case "Madame Ilse": case "Master of First Aid": case "The seer":
                    return Lady;
                case "Nell Garrow": case "Lottie Wren": case "Edda Brann": case "Tavern keeper": case "Master of Item Creation":
                    return WorkingWoman;
                case "Pip Vance": case "Kit Mallow": case "Dace": case "Veinmarket": case "Veinmarket at a table": case "Drunkard": case "Farmer":
                    return Townsman;
                case "Ruth Kettle": case "The pawnbroker": case "POI B - Old woman in a doorway": case "POI A - Neighbour (middle)":
                    return Matron;
                case "Faction member":
                    return female ? Lady : Gentleman;
            }

            if (name.StartsWith("POI B - Collector") || name.StartsWith("POI A - Neighbour"))
            {
                return Townsman;
            }

            if (name.StartsWith("POI B - Mill girl"))
            {
                return WorkingWoman;
            }

            return colour switch
            {
                "Blue" => Officer,
                "Red" => Detective,
                "Green" or "Orange" or "Teal" => female ? Lady : Gentleman,
                _ => female ? WorkingWoman : Townsman,
            };
        }

        /// <summary>
        /// A walking townsperson's model by the wealth of the C5 block nearest its place (§29.2): rich, the gentleman and
        /// the lady; poor or the works, the working people; else either, by the place's cell, so a rebuild picks the same.
        /// </summary>
        public static Look WalkerLook(bool female, Vector3 at, VeilmouthLayout layout)
        {
            var (px, py) = (at.x + 500f, 500f - at.z);
            var block = layout.blocks?.OrderBy(b => Outside(b, px, py)).FirstOrDefault();
            var odd = (((Mathf.FloorToInt(px) * 73856093) ^ (Mathf.FloorToInt(py) * 19349663)) & 1) == 1;
            return (block?.wealth ?? "middling") switch
            {
                "rich" => female ? Lady : Gentleman,
                "poor" or "works" => female ? (odd ? Matron : WorkingWoman) : Townsman,
                _ => female ? (odd ? Matron : Lady) : (odd ? Townsman : Gentleman),
            };
        }

        private static float Outside(VeilmouthLayout.BlockSpec b, float px, float py)
        {
            var dx = Mathf.Max(b.x0 - px, 0f, px - b.x1);
            var dy = Mathf.Max(b.y0 - py, 0f, py - b.y1);
            return (dx * dx) + (dy * dy);
        }

        /// <summary>A standing person of <paramref name="name"/>'s role under <paramref name="parent"/>, its feet at <paramref name="at"/>.</summary>
        public static GameObject Instantiate(string name, bool female, string colour, Transform parent, Vector3 at, float yaw)
        {
            var look = LookFor(name, female, colour);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(look.Standing), parent);
            go.name = name;
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            return go;
        }

        // ------------------------------------------------------------------------------------------- the pass

        public static void Run()
        {
            var report = new StringBuilder();
            SliceMixamo.Import(report);
            Controllers(report);
            var speeds = new Dictionary<string, float>
            {
                [SliceTownsfolk.PrefabPath] = Refit(SliceTownsfolk.PrefabPath, Townsman, false, report),
                [SliceTownsfolk.WomanPrefabPath] = Refit(SliceTownsfolk.WomanPrefabPath, Matron, true, report),
            };
            var townsmanScale = AssetDatabase.LoadAssetAtPath<GameObject>(SliceTownsfolk.PrefabPath).transform.Find("Model").localScale;
            foreach (var look in All)
            {
                Build(look, false, look == Townsman ? townsmanScale : (Vector3?)null, report);
            }

            foreach (var look in NewWalkers)
            {
                speeds[look.Walking] = Build(look, true, null, report).GetComponent<TownsfolkWalker>().Speed;
            }

            Scenes(speeds, report);
            Opponent();
            WearPlayer(SliceTestScenes.PlayerPrefabPath, PlayerMan, report);
            WearPlayer(PlayerWomanPrefab, PlayerWoman, report);
            Directory.CreateDirectory(OutputFolder);
            File.WriteAllText($"{OutputFolder}/people.txt", report.ToString());
            Debug.Log("[SlicePeopleModels]\n" + report);
            CheckSheet();
            PlayerSheet();
            RenderPeople();
            SliceCharacterRigs.Check();
        }

        /// <summary>
        /// Batch, after the city is built again: the people's looks, their paces, the blacksmith and the walkers' routes in
        /// the slice's scenes, with the prefabs as they stand. -executeMethod GameDev.Slice.EditorTools.SlicePeopleModels.ScenesOnly
        /// </summary>
        public static void ScenesOnly()
        {
            var speeds = new[] { SliceTownsfolk.PrefabPath, SliceTownsfolk.WomanPrefabPath }.Concat(NewWalkers.Select(l => l.Walking))
                .ToDictionary(p => p, p => AssetDatabase.LoadAssetAtPath<GameObject>(p).GetComponent<TownsfolkWalker>().Speed);
            var report = new StringBuilder();
            Scenes(speeds, report);
            Debug.Log("[SlicePeopleModels] scenes:\n" + report);
        }

        // ------------------------------------------------------------------------------------------- controllers

        /// <summary>
        /// The controllers on the Mixamo clips (<see cref="SliceMixamo"/>): the standing people's, the Wardens' at
        /// attention, the townsfolk's walk and idle, and the player's locomotion at the player's own pace. The A0
        /// stand-ins go.
        /// </summary>
        private static void Controllers(StringBuilder report)
        {
            Tidy();
            SliceMixamo.Person(ManController, "Idle");
            SliceMixamo.Person(WomanController, "Idle");
            SliceMixamo.Person(SliceMixamo.WardenController, "Attention");
            SliceMixamo.Person(SliceMixamo.WardenWomanController, "AttentionWoman");
            SliceMixamo.Person(SliceMixamo.BlacksmithController, "Hammer");
            SliceMixamo.Person(SliceCharacterLook.PersonIdlePath, "Idle");
            Repoint(ManWalkerController, SliceMixamo.Get("Idle"), SliceMixamo.Get("Walk"));
            Repoint(WomanWalkerController, SliceMixamo.Get("Idle"), SliceMixamo.Get("Walk"));

            // The player's pace in each clip, measured from the feet on both bodies at their statures (the mean, as one
            // controller plays both), against PlayerLocomotion's speeds; the flight against its jump's airtime.
            var locomotion = AssetDatabase.LoadAssetAtPath<GameObject>(SliceTestScenes.PlayerPrefabPath).GetComponentInChildren<PlayerLocomotion>(true);
            var so = new SerializedObject(locomotion);
            float Field(string name) => so.FindProperty(name).floatValue;
            var speeds = new[] { 0f, Field("walkSpeed"), Field("runSpeed"), Field("sprintSpeed") };
            var uses = new[] { "Idle", "Walk", "Run", "Sprint" };
            var scales = new float[uses.Length];
            scales[0] = 1f;
            for (var i = 1; i < uses.Length; i++)
            {
                var clip = SliceMixamo.Get(uses[i]);
                var paces = new[] { PlayerMan, PlayerWoman }.Select(look =>
                {
                    var animator = Fitted(look, clip);
                    var pace = Pace(animator, clip, animator.transform.parent);
                    Object.DestroyImmediate(animator.transform.parent.gameObject);
                    return pace;
                }).ToList();
                scales[i] = Mathf.Min(speeds[i] / paces.Average(), MostTimeScale);
                report.AppendLine($"Player {uses[i]}: paced {paces[0]:0.00} m/s on the man, {paces[1]:0.00} on the woman (measured); " +
                                  $"{speeds[i]:0.0} m/s would play it x{speeds[i] / paces.Average():0.00}, plays it x{scales[i]:0.00}");
            }

            var lift = Mathf.Sqrt(2f * Field("jumpHeight") * -Field("gravity"));
            var airtime = 2f * lift / -Field("gravity");
            var air = SliceMixamo.Get("JumpAir").length;
            SliceMixamo.Player(PlayerController, scales, air / airtime);
            report.AppendLine($"Player jump: {airtime:0.00} s in the air (PlayerLocomotion), the clip's flight {air:0.00} s plays x{air / airtime:0.00}");
            AssetDatabase.SaveAssets();
        }

        /// <summary>The A0 stand-ins leave: their four clips and the woman player's override go, and the people's two controllers move beside the Wardens'.</summary>
        private static void Tidy()
        {
            if (!AssetDatabase.IsValidFolder(SliceMixamo.ControllerFolder))
            {
                AssetDatabase.CreateFolder("Assets/Animation", "Controllers");
            }

            foreach (var name in new[] { "Man.controller", "Woman.controller" })
            {
                var old = $"{StandIns}/{name}";
                if (AssetDatabase.LoadAssetAtPath<Object>(old) != null && AssetDatabase.LoadAssetAtPath<Object>($"{SliceMixamo.ControllerFolder}/{name}") == null)
                {
                    AssetDatabase.MoveAsset(old, $"{SliceMixamo.ControllerFolder}/{name}");
                }
            }

            foreach (var old in new[] { "ManIdle", "ManWalk", "WomanIdle", "WomanWalk" }.Select(n => $"{StandIns}/{n}.anim").Append(OldWomanPlayer))
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(old) != null)
                {
                    AssetDatabase.DeleteAsset(old);
                }
            }

            if (AssetDatabase.IsValidFolder(StandIns) && AssetDatabase.FindAssets(string.Empty, new[] { StandIns }).Length == 0)
            {
                AssetDatabase.DeleteAsset(StandIns);
            }
        }

        private static void Repoint(string path, AnimationClip idle, AnimationClip walk)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                return;
            }

            foreach (var s in controller.layers[0].stateMachine.states)
            {
                s.state.motion = s.state.name == "Walk" ? walk : idle;
            }

            EditorUtility.SetDirty(controller);
        }

        // ------------------------------------------------------------------------------------------- prefabs

        /// <summary>The townsfolk's walking prefab fitted again to its Humanoid walk, its pace measured anew.</summary>
        private static float Refit(string prefabPath, Look look, bool rescale, StringBuilder report)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            var model = root.transform.Find("Model").gameObject;
            var animator = Animated(model, look);
            var walk = SliceMixamo.Get("Walk");
            var stature = Fit(model, walk, look.Height, !rescale);
            var speed = Speed(animator, walk, root.transform);
            var head = Keep(model, animator);
            var capsule = root.GetComponent<CapsuleCollider>();
            capsule.height = look.Height;
            capsule.center = new Vector3(0f, look.Height * 0.5f, 0f);
            var walker = root.GetComponent<TownsfolkWalker>();
            walker.Configure(walker.Route, speed, SliceTownsfolk.GroundMask);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            report.AppendLine($"{prefabPath}: {stature:0.00} m (head bone at {head:0.00} m), walking {speed:0.00} m/s in {walk.name}");
            return speed;
        }

        private static GameObject Build(Look look, bool walking, Vector3? scale, StringBuilder report)
        {
            var idle = SliceMixamo.Get(IdleUse(look));
            var walk = SliceMixamo.Get("Walk");
            var root = new GameObject(look.Name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(look.Model), root.transform);
            model.name = "Model";
            SliceKitCatalogue.LinkTripoTextures(model, look.Model);
            var animator = Animated(model, look);
            if (scale.HasValue)
            {
                model.transform.localScale = scale.Value;
            }

            // The controller first: assigning it binds the Animator again, so the pose comes after.
            var controllerPath = walking ? (look.Female ? WomanWalkerController : ManWalkerController) : StandingController(look);
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            var stature = Fit(model, walking ? walk : idle, look.Height, scale.HasValue);
            var speed = walking ? Speed(animator, walk, root.transform) : 0f;
            var head = Keep(model, animator);
            var armLive = ArmDrop(animator);
            var armNames = (animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).name, animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).name);

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = look.Height;
            capsule.radius = 0.25f;
            capsule.center = new Vector3(0f, look.Height * 0.5f, 0f);
            if (walking)
            {
                SliceTownsfolk.ShoulderLooseProps(root);
                root.AddComponent<TownsfolkWalker>().Configure(new Vector3[0], speed, SliceTownsfolk.GroundMask);
            }

            SliceKitCatalogue.SetLayer(root, KitLayers.Character);
            Directory.CreateDirectory(Folder);
            var path = walking ? look.Walking : look.Standing;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            var saved = prefab.GetComponentsInChildren<Transform>(true);
            var upper = saved.FirstOrDefault(t => t.name == armNames.Item1);
            var lower = saved.FirstOrDefault(t => t.name == armNames.Item2);
            var armSaved = upper != null && lower != null ? Vector3.Angle(lower.position - upper.position, Vector3.down) : -1f;
            report.AppendLine($"{path}: {look.File}, {stature:0.00} m (head bone at {head:0.00} m){(walking ? $", walking {speed:0.00} m/s" : string.Empty)}, " +
                              $"upper arm {armLive:0}° from down as posed, {armSaved:0}° as saved");
            return prefab;
        }

        /// <summary>A standing look's idle: a Warden's at attention, a woman Warden's in the women's pose.</summary>
        private static string IdleUse(Look look) => look == Officer ? "Attention" : look == WardenWoman ? "AttentionWoman" : "Idle";

        /// <summary>A standing look's controller: the Wardens' at attention, the blacksmith's hammering at the anvil (A3).</summary>
        private static string StandingController(Look look) =>
            look == Officer ? SliceMixamo.WardenController : look == WardenWoman ? SliceMixamo.WardenWomanController :
            look == Blacksmith ? SliceMixamo.BlacksmithController : look.Female ? WomanController : ManController;

        /// <summary>A loose copy of <paramref name="look"/>'s model under a root of its own, at its stature in the clip's first pose; the caller destroys the root.</summary>
        internal static Animator Fitted(Look look, AnimationClip clip)
        {
            var root = new GameObject(look.Name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(look.Model), root.transform);
            var animator = Animated(model, look);
            Fit(model, clip, look.Height, false);
            return animator;
        }

        /// <summary>The left upper arm's angle from straight down: about 90° in a T-pose, under 30° at rest.</summary>
        internal static float ArmDrop(Animator animator)
        {
            var upper = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var lower = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            return upper != null && lower != null ? Vector3.Angle(lower.position - upper.position, Vector3.down) : -1f;
        }

        /// <summary>
        /// The pose the last <see cref="Pose"/> set, written into the prefab: the pose handler moves the bones from
        /// native code, which a nested model's instance does not record, so the people stood in their T-pose in the scene
        /// (2026-11-10). The Animator drives the human bones in play; this is the pose the editor and the renders show.
        /// Returns the head bone's height over the root, a check on the stature.
        /// </summary>
        private static float Keep(GameObject model, Animator animator)
        {
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(t))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                }
            }

            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            return head != null ? model.transform.parent.InverseTransformPoint(head.position).y : 0f;
        }

        /// <summary>The model's Animator with its own Humanoid avatar.</summary>
        private static Animator Animated(GameObject model, Look look)
        {
            var animator = model.GetComponent<Animator>();
            if (animator == null)
            {
                animator = model.AddComponent<Animator>();
            }

            if (animator.avatar == null || !animator.avatar.isHuman)
            {
                animator.avatar = AssetDatabase.LoadAllAssetsAtPath(look.Model).OfType<Avatar>().First();
            }

            return animator;
        }

        /// <summary>The model in the clip's first pose: its height (unless kept), facing +Z, its feet on the root. Returns its stature.</summary>
        private static float Fit(GameObject model, AnimationClip clip, float height, bool keepScale)
        {
            var animator = model.GetComponent<Animator>();
            var root = model.transform.parent;
            model.transform.localPosition = Vector3.zero;
            if (!keepScale && PrefabUtility.GetCorrespondingObjectFromSource(model) is GameObject source)
            {
                model.transform.localScale = source.transform.localScale; // from the file's own size, never compounding a refit
            }

            Pose(animator, clip, 0f);
            if (!keepScale)
            {
                model.transform.localScale *= height / Stature(animator, root);
                Pose(animator, clip, 0f);
            }

            var l = root.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position);
            var r = root.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position);
            var facing = Vector3.Cross(Vector3.up, l - r);
            facing.y = 0f;
            model.transform.localRotation = Quaternion.AngleAxis(Vector3.SignedAngle(facing, Vector3.forward, Vector3.up), Vector3.up) * model.transform.localRotation;
            Pose(animator, clip, 0f);
            var seen = Seen(model, root);
            model.transform.localPosition -= new Vector3(seen.center.x, seen.min.y, seen.center.z);
            return Stature(animator, root);
        }

        /// <summary>The person's stature: from the eyes where the avatar maps them, else the mesh's height.</summary>
        private static float Stature(Animator animator, Transform root)
        {
            var seen = Seen(animator.gameObject, root);
            var left = animator.GetBoneTransform(HumanBodyBones.LeftEye);
            var right = animator.GetBoneTransform(HumanBodyBones.RightEye);
            if (left == null || right == null)
            {
                return seen.size.y;
            }

            var eyes = root.InverseTransformPoint((left.position + right.position) * 0.5f).y - seen.min.y;
            return eyes / EyeShare;
        }

        internal static void Pose(Animator animator, AnimationClip clip, float time)
        {
            var handler = new HumanPoseHandler(animator.avatar, animator.transform);
            var pose = SliceCharacterRigs.PoseAt(clip, time);
            handler.SetHumanPose(ref pose);
            handler.Dispose();
        }

        /// <summary>A townsperson's walking pace, the clip's own within the walkers' bounds (authoring default, 0.6–2 m/s).</summary>
        private static float Speed(Animator animator, AnimationClip walk, Transform root) => Mathf.Clamp(Pace(animator, walk, root), 0.6f, 2f);

        /// <summary>The clip is in place: the lower foot moves back as fast as the body would go forward (median over the clip).</summary>
        internal static float Pace(Animator animator, AnimationClip walk, Transform root)
        {
            var left = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var right = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            const int samples = 120;
            var dt = walk.length / samples;
            var speeds = new List<float>();
            Pose(animator, walk, 0f);
            var before = (L: root.InverseTransformPoint(left.position), R: root.InverseTransformPoint(right.position));
            for (var i = 1; i <= samples; i++)
            {
                Pose(animator, walk, i * dt);
                var now = (L: root.InverseTransformPoint(left.position), R: root.InverseTransformPoint(right.position));
                var lower = now.L.y < now.R.y;
                speeds.Add(-((lower ? now.L.z - before.L.z : now.R.z - before.R.z) / dt));
                before = now;
            }

            Pose(animator, walk, 0f);
            speeds.Sort();
            return speeds[speeds.Count / 2];
        }

        /// <summary>The skinned mesh as posed, in <paramref name="space"/>.</summary>
        private static Bounds Seen(GameObject model, Transform space)
        {
            var min = Vector3.positiveInfinity;
            var max = Vector3.negativeInfinity;
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh();
                // The baked vertices leave out the model's own scale (measured 2026-11-10: a model scaled ×1.78 still
                // measured 1.00 m with a scale-free matrix, and the Townswoman's refit compounded to ×2.93), so the
                // renderer's whole matrix carries them into the world.
                smr.BakeMesh(mesh, true);
                var m = smr.transform.localToWorldMatrix;
                foreach (var v in mesh.vertices)
                {
                    var w = space.InverseTransformPoint(m.MultiplyPoint3x4(v));
                    min = Vector3.Min(min, w);
                    max = Vector3.Max(max, w);
                }

                Object.DestroyImmediate(mesh);
            }

            var b = new Bounds();
            b.SetMinMax(min, max);
            return b;
        }

        // ------------------------------------------------------------------------------------------- scenes

        private static void Scenes(Dictionary<string, float> speeds, StringBuilder report)
        {
            var layout = VeilmouthLayout.Load();
            var paths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes/Slice" }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p != VeilmouthDwellings.ScenePath).OrderBy(p => p).ToList();
            foreach (var path in paths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var lines = new List<string>();
                foreach (var go in Roots().ToList())
                {
                    var source = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                    var dummy = source.Contains("Human Character Dummy");
                    var townsWoman = source == SliceTownsfolk.WomanPrefabPath;
                    var walker = go.GetComponent<TownsfolkWalker>();
                    Look look;
                    string prefab;
                    if (dummy || ((townsWoman || source == SliceTownsfolk.PrefabPath) && walker == null))
                    {
                        look = LookFor(go.name, dummy ? source.Contains("HumanDummy_F") : townsWoman);
                        prefab = look.Standing;
                    }
                    else if (walker != null && go.name != "Farmer")
                    {
                        look = WalkerLook(townsWoman, go.transform.position, layout);
                        prefab = look.Walking;
                        if (prefab == source)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }

                    Replace(go, AssetDatabase.LoadAssetAtPath<GameObject>(prefab));
                    lines.Add($"{go.name}: {Path.GetFileNameWithoutExtension(source)} to {look.Name}{(walker != null ? " (walking)" : string.Empty)}");
                }

                // Every walker at its prefab's measured pace, the refitted townsfolk's among them.
                foreach (var w in Object.FindObjectsByType<TownsfolkWalker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var source = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(w.gameObject);
                    if (speeds.TryGetValue(source, out var speed) && Mathf.Abs(w.Speed - speed) > 0.001f)
                    {
                        w.Configure(w.Route, speed, SliceTownsfolk.GroundMask);
                        EditorUtility.SetDirty(w);
                        lines.Add($"{w.name}: walking {speed:0.00} m/s");
                    }
                }

                if (path == VeilmouthScene.ScenePath)
                {
                    PlaceBlacksmith(lines);
                    Reroute(lines);
                }

                if (lines.Count > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    report.AppendLine($"{path}:\n  " + string.Join("\n  ", lines));
                }
            }
        }

        /// <summary>Every person's prefab instance in the open scene: the Dummies and the townsfolk's prefabs.</summary>
        private static IEnumerable<GameObject> Roots() =>
            Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(t => t.gameObject)
                .Where(g => PrefabUtility.IsOutermostPrefabInstanceRoot(g))
                .Where(g =>
                {
                    var p = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g);
                    return p.Contains("Human Character Dummy") || p == SliceTownsfolk.PrefabPath || p == SliceTownsfolk.WomanPrefabPath;
                });

        /// <summary>
        /// The instance takes the new prefab in place: its object stays, with its name, place, turn and added components
        /// (NamedPerson, PersonTalk); the Dummy's own collider, added at its dressing, goes, as the new prefab has its own,
        /// and so does its scale, which sized the Dummy.
        /// </summary>
        private static void Replace(GameObject go, GameObject prefab)
        {
            var settings = new PrefabReplacingSettings
            {
                changeRootNameToAssetName = false,
                objectMatchMode = ObjectMatchMode.ByHierarchy,
                prefabOverridesOptions = PrefabOverridesOptions.KeepAllPossibleOverrides,
            };
            PrefabUtility.ReplacePrefabAssetOfPrefabInstance(go, prefab, settings, InteractionMode.AutomatedAction);
            foreach (var c in go.GetComponents<Collider>().Where(c => PrefabUtility.IsAddedComponentOverride(c)).ToList())
            {
                Object.DestroyImmediate(c);
            }

            go.transform.localScale = Vector3.one;
            var scale = new SerializedObject(go.transform).FindProperty("m_LocalScale");
            if (scale.prefabOverride)
            {
                PrefabUtility.RevertPropertyOverride(scale, InteractionMode.AutomatedAction);
            }
        }

        /// <summary>
        /// The blacksmith at Bawling Row's anvil (owner, 2026-11-10), beside Edda Brann's Item Creation stall: on the
        /// first side of the anvil with ground and room for a person, behind it toward the plinth first, facing it.
        /// </summary>
        private static void PlaceBlacksmith(List<string> lines)
        {
            if (Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(t => t.name == "Blacksmith"
                    && PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject) && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == Blacksmith.Standing))
            {
                return; // placed by an earlier run
            }

            var edda = Object.FindObjectsByType<NamedPerson>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(p => p.Id == "edda");
            var anvil = Object.FindObjectsByType<KitBlock>(FindObjectsSortMode.None).Where(b => b.name.StartsWith("Kit_Market_Anvil"))
                .OrderBy(b => edda == null ? 0f : (b.transform.position - edda.transform.position).sqrMagnitude).FirstOrDefault();
            if (anvil == null)
            {
                lines.Add("Blacksmith: NO ANVIL found; not placed");
                return;
            }

            Physics.SyncTransforms();
            var b = anvil.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((p, q) => { p.Encapsulate(q); return p; });
            var ground = LayerMask.GetMask(KitLayers.Ground, KitLayers.Structure);
            var things = ~LayerMask.GetMask(KitLayers.Ground, KitLayers.Character);
            foreach (var side in new[] { Vector3.right, Vector3.forward, Vector3.back, Vector3.left })
            {
                var at = b.center + Vector3.Scale(side, b.extents) + (side * 0.55f);
                if (!Physics.Raycast(new Vector3(at.x, b.max.y + 3f, at.z), Vector3.down, out var hit, 10f, ground, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                var feet = hit.point;
                if (Physics.CheckCapsule(feet + (Vector3.up * 0.45f), feet + (Vector3.up * 1.6f), 0.28f, things, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Blacksmith.Standing),
                    edda != null ? edda.transform.parent : anvil.transform.parent);
                go.name = "Blacksmith";
                go.transform.SetPositionAndRotation(feet, Quaternion.LookRotation(-side));
                lines.Add($"Blacksmith: at {feet} on the anvil's {side} side, facing it");
                return;
            }

            lines.Add("Blacksmith: NO ROOM round the anvil; not placed");
        }

        /// <summary>A walker whose route now meets the bronze statue or a street lamp, this pass's new pieces, walks a route planned again.</summary>
        private static void Reroute(List<string> lines)
        {
            Physics.SyncTransforms();
            var mask = LayerMask.GetMask("Default", KitLayers.Structure, KitLayers.Prop, KitLayers.Vegetation, KitLayers.Interactable);
            foreach (var w in Object.FindObjectsByType<TownsfolkWalker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var hit = Meets(w.Route, mask);
                if (hit == null)
                {
                    continue;
                }

                // From where it stands, else from a point of its old route, else from a cell or two aside: the first route
                // planned that meets no new lamp or statue.
                var starts = new[] { w.transform.position }.Concat(w.Route)
                    .Concat(new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back }.SelectMany(d => new[] { w.transform.position + d, w.transform.position + (d * 2f) }));
                var route = starts.Select(SliceTownsfolk.PlanRoute).FirstOrDefault(r => r.Length >= 3 && Meets(r, mask) == null) ?? new Vector3[0];
                if (route.Length < 3)
                {
                    lines.Add($"{w.name} at {w.transform.position}: its route meets {hit}; NO NEW ROUTE");
                    continue;
                }

                w.Configure(route, w.Speed, SliceTownsfolk.GroundMask);
                w.transform.position = route[0];
                EditorUtility.SetDirty(w);
                lines.Add($"{w.name} at {route[0]}: its route met {hit}; planned again, {w.Length:0} m{(Meets(route, mask) is { } still ? $", STILL MEETS {still}" : string.Empty)}");
            }
        }

        private static string Meets(Vector3[] route, int mask)
        {
            if (route == null || route.Length < 2)
            {
                return null;
            }

            for (var i = 0; i < route.Length; i++)
            {
                var a = route[i];
                var d = route[(i + 1) % route.Length] - a;
                if (d.magnitude < 0.01f)
                {
                    continue;
                }

                foreach (var hit in Physics.CapsuleCastAll(a + (Vector3.up * 0.6f), a + (Vector3.up * 1.5f), 0.25f, d.normalized, d.magnitude, mask, QueryTriggerInteraction.Ignore))
                {
                    var block = hit.collider.GetComponentInParent<KitBlock>();
                    if (block != null && (block.name.StartsWith("Kit_Street_Lamp") || block.name.StartsWith("Kit_Street_BronzeStatue")))
                    {
                        return block.name;
                    }
                }
            }

            return null;
        }

        /// <summary>The development opponent wears the Cult Forces' model (asset map §1).</summary>
        private static void Opponent()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SliceSettings>(SliceConfrontSetup.SettingsPath);
            var so = new SerializedObject(settings);
            so.FindProperty("opponentModel").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Detective.Standing);
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------------------------------- the female Warden and the player

        /// <summary>
        /// The owner's second models of 2026-11-10: the female Warden's prefab, C5's two female Wardens in it, and the
        /// player's two bodies. Batch: -executeMethod GameDev.Slice.EditorTools.SlicePeopleModels.WardensAndPlayer
        /// </summary>
        public static void WardensAndPlayer()
        {
            var report = new StringBuilder();
            Build(WardenWoman, false, null, report);
            FemaleWardens(report);
            WearPlayer(SliceTestScenes.PlayerPrefabPath, PlayerMan, report);
            WearPlayer(PlayerWomanPrefab, PlayerWoman, report);
            Directory.CreateDirectory(OutputFolder);
            File.WriteAllText($"{OutputFolder}/people_wardens_player.txt", report.ToString());
            Debug.Log("[SlicePeopleModels]\n" + report);
            CheckSheet();
            PlayerSheet();
            RenderPeople();
        }

        /// <summary>
        /// The Wardens placed as women, who wore the Officer until the owner's female Warden came: C5's at the Black
        /// Puddle (VeilmouthCity.Districts) and the lock-up's in its office (VeilmouthInteriors.Lockup). A rebuild places
        /// them through <see cref="LookFor"/>, which now gives a woman the female Warden.
        /// </summary>
        private static void FemaleWardens(StringBuilder report)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WardenWoman.Standing);
            foreach (var (scenePath, name, at) in new[]
                     {
                         (VeilmouthScene.ScenePath, "Warden at the Black Puddle", (Vector2?)null),
                         (VeilmouthDressing.LockupScenePath, "Warden", new Vector2(18.3f, 15.5f)),
                     })
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var wardens = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(t => t.name == name && PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)
                                && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == Officer.Standing)
                    .Where(t => at == null || Vector2.Distance(new Vector2(t.localPosition.x, t.localPosition.z), at.Value) < 0.6f)
                    .ToList();
                foreach (var t in wardens)
                {
                    Replace(t.gameObject, prefab);
                }

                report.AppendLine($"{scenePath}: {name}: {wardens.Count} to {WardenWoman.Name}");
                if (wardens.Count > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
        }

        /// <summary>The player prefab at <paramref name="path"/> in <paramref name="look"/>'s body; the woman's is made from the man's.</summary>
        private static void WearPlayer(string path, Look look, StringBuilder report)
        {
            var root = PrefabUtility.LoadPrefabContents(SliceTestScenes.PlayerPrefabPath);
            report.AppendLine($"{path}: {Wear(root, look)}");
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        /// <summary>
        /// <paramref name="look"/>'s body under the player's <paramref name="root"/>, named Model in place of the old one:
        /// at its stature, in the idle's first pose, playing the player's controller, the same for both bodies. The root
        /// and its components stay. Returns a line for the report.
        /// </summary>
        internal static string Wear(GameObject root, Look look)
        {
            var old = root.transform.Find("Model");
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(look.Model), root.transform);
            model.name = "Model";
            model.transform.SetSiblingIndex(0);
            SliceKitCatalogue.LinkTripoTextures(model, look.Model);
            var animator = Animated(model, look);
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerController);
            animator.applyRootMotion = false;
            var stature = Fit(model, SliceMixamo.Get("Idle"), look.Height, false);
            var head = Keep(model, animator);
            SliceKitCatalogue.SetLayer(model, KitLayers.Character);
            return $"{look.File}, {stature:0.00} m (head bone at {head:0.00} m), {animator.runtimeAnimatorController.name}, upper arm {ArmDrop(animator):0}° from down";
        }

        // ------------------------------------------------------------------------------------------- checks

        /// <summary>The player's two prefabs standing in their idle and stepping in their walk, front and side (`people_player.png`).</summary>
        private static void PlayerSheet()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var light = new GameObject("Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
            const int tile = 256;
            var players = new[] { (SliceTestScenes.PlayerPrefabPath, PlayerMan), (PlayerWomanPrefab, PlayerWoman) };
            var sheet = new Texture2D(4 * tile, players.Length * tile, TextureFormat.RGB24, false);
            for (var row = 0; row < players.Length; row++)
            {
                var (path, look) = players[row];
                var column = 0;
                foreach (var (clip, at) in new[] { (SliceMixamo.Get("Idle"), 0.5f), (SliceMixamo.Get("Walk"), 0.25f) })
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                    Pose(go.GetComponentInChildren<Animator>(), clip, clip.length * at);
                    SliceCharacterRigs.Draw(go, sheet, row, ref column, tile, players.Length);
                    Object.DestroyImmediate(go);
                }
            }

            sheet.Apply();
            File.WriteAllBytes($"{OutputFolder}/people_player.png", sheet.EncodeToPNG());
        }

        /// <summary>Every look standing in its idle and stepping in its walk, front and side (`people_check.png`).</summary>
        private static void CheckSheet()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var light = new GameObject("Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
            const int tile = 256;
            var sheet = new Texture2D(4 * tile, All.Length * tile, TextureFormat.RGB24, false);
            for (var row = 0; row < All.Length; row++)
            {
                var look = All[row];
                var column = 0;
                foreach (var (clip, at) in new[] { (SliceMixamo.Get(IdleUse(look)), 0.5f), (SliceMixamo.Get("Walk"), 0.25f) })
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(look.Standing));
                    Pose(go.GetComponentInChildren<Animator>(), clip, clip.length * at);
                    SliceCharacterRigs.Draw(go, sheet, row, ref column, tile, All.Length);
                    Object.DestroyImmediate(go);
                }
            }

            sheet.Apply();
            File.WriteAllBytes($"{OutputFolder}/people_check.png", sheet.EncodeToPNG());
        }

        /// <summary>Veilmouth's people where the pass changed them: the blacksmith and Edda, the Wardens, the market, a lady and a gentleman walking.</summary>
        private static void RenderPeople()
        {
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var people = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var shots = new[] { "Blacksmith", "Warden at the station", "Warden at the Black Puddle", "Tobias Penn", "Madame Ilse", "Drunkard" }
                .Select(n => people.FirstOrDefault(t => t.name == n)).Where(t => t != null).ToList();
            foreach (var path in new[] { Gentleman.Walking, Lady.Walking, WorkingWoman.Walking })
            {
                var one = people.FirstOrDefault(t => PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject) && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == path);
                if (one != null)
                {
                    shots.Add(one);
                }
            }

            var i = 0;
            foreach (var t in shots)
            {
                Debug.Log($"[SlicePeopleModels] {t.name} in the scene: upper arm {ArmDrop(t.GetComponentInChildren<Animator>()):0}° from down");
                var at = t.position + (Vector3.up * 1.1f);
                VeilmouthRenders.Eye($"people_{i++:00}_{t.name.Replace(' ', '_')}", at + (t.forward * 3.4f) + (t.right * 1.2f) + (Vector3.up * 0.3f), at, 50f);
            }
        }
    }
}

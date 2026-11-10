using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GameDev.Slice.Kit;
using GameDev.Slice.People;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Every person but the player is one of the owner's Tripo people, by role (owner, 2026-11-10; SLICE_ASSET_MAP.md
    /// §1), standing and walking in the A0 stand-ins of ANIMATION_PIPELINE_PLAN.md until Mixamo's clips arrive.
    /// <list type="bullet">
    /// <item>The stand-ins are Humanoid clips: the men's walk and idle baked once from the borrowed Generic clips of the
    /// stylized man, through his avatar, into muscle curves; the women's the historical woman's own. Every clip loops
    /// in place, its root baked into the pose as the clip has it.</item>
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
    /// Batch: -executeMethod GameDev.Slice.EditorTools.SlicePeopleModels.Run
    /// </summary>
    public static class SlicePeopleModels
    {
        public const string Folder = "Assets/Prefabs/Slice/People";
        public const string StandIns = "Assets/Animation/StandIn";
        public const string ManIdle = StandIns + "/ManIdle.anim";
        public const string ManWalk = StandIns + "/ManWalk.anim";
        public const string WomanIdle = StandIns + "/WomanIdle.anim";
        public const string WomanWalk = StandIns + "/WomanWalk.anim";
        public const string ManController = StandIns + "/Man.controller";
        public const string WomanController = StandIns + "/Woman.controller";
        private const string ManWalkerController = "Assets/Settings/Slice/Townsfolk/SliceTownsfolk.controller";
        private const string WomanWalkerController = "Assets/Settings/Slice/Townsfolk/Woman/SliceTownswoman.controller";
        private const string PlayerController = "Assets/Settings/OverworldPlayerAnimator.controller";
        private const string OutputFolder = "Logs/SliceSurvey";

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
        private const string PlayerWomanController = "Assets/Settings/OverworldPlayerAnimator_Woman.overrideController";

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
            StandInClips(report);
            Controllers();
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
            Directory.CreateDirectory(OutputFolder);
            File.WriteAllText($"{OutputFolder}/people.txt", report.ToString());
            Debug.Log("[SlicePeopleModels]\n" + report);
            CheckSheet();
            RenderPeople();
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

        // ------------------------------------------------------------------------------------------- clips

        private static void StandInClips(StringBuilder report)
        {
            Directory.CreateDirectory(StandIns);
            AssetDatabase.Refresh();
            Bake(SliceTownsfolk.ManIdle, ManIdle, report);
            Bake(SliceTownsfolk.ManWalk, ManWalk, report);
            var own = AssetDatabase.LoadAllAssetsAtPath(SliceTownsfolk.WomanModelPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToList();
            Copy(own.First(c => c.name.StartsWith("idle")), WomanIdle, report);
            Copy(own.First(c => c.name.StartsWith("walk")), WomanWalk, report);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// A borrowed Generic clip of the stylized man, played on a plain copy of his model bone by bone, read back through
        /// his Humanoid avatar each frame into a clip of muscle curves that any Humanoid plays.
        /// </summary>
        private static void Bake(string genericPath, string path, StringBuilder report)
        {
            var source = AssetDatabase.LoadAssetAtPath<AnimationClip>(genericPath);
            var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SliceTownsfolk.ModelPath));
            var avatar = model.GetComponent<Animator>().avatar;
            Object.DestroyImmediate(model.GetComponent<Animator>());
            var groups = AnimationUtility.GetCurveBindings(source).Where(b => b.type == typeof(Transform))
                .Select(b => (Bone: b.path.Length == 0 ? model.transform : model.transform.Find(b.path), Property: b.propertyName.Replace("m_", "").ToLowerInvariant(),
                    Curve: AnimationUtility.GetEditorCurve(source, b)))
                .Where(c => c.Bone != null).GroupBy(c => c.Bone).ToList();

            var handler = new HumanPoseHandler(avatar, model.transform);
            const float rate = 30f;
            var frames = Mathf.Max(2, Mathf.RoundToInt(source.length * rate));
            var muscles = Enumerable.Range(0, HumanTrait.MuscleCount).Select(_ => new AnimationCurve()).ToArray();
            var root = Enumerable.Range(0, 7).Select(_ => new AnimationCurve()).ToArray();
            var pose = new HumanPose();
            var previous = Quaternion.identity;
            for (var i = 0; i <= frames; i++)
            {
                var time = source.length * i / frames;
                foreach (var group in groups)
                {
                    var t = group.Key;
                    var q = t.localRotation;
                    var p = t.localPosition;
                    foreach (var (_, property, curve) in group)
                    {
                        var v = curve.Evaluate(time);
                        switch (property)
                        {
                            case "localrotation.x": q.x = v; break;
                            case "localrotation.y": q.y = v; break;
                            case "localrotation.z": q.z = v; break;
                            case "localrotation.w": q.w = v; break;
                            case "localposition.x": p.x = v; break;
                            case "localposition.y": p.y = v; break;
                            case "localposition.z": p.z = v; break;
                        }
                    }

                    t.localRotation = Quaternion.Normalize(q);
                    t.localPosition = p;
                }

                handler.GetHumanPose(ref pose);
                var body = pose.bodyRotation;
                if (i > 0 && Quaternion.Dot(previous, body) < 0f)
                {
                    body = new Quaternion(-body.x, -body.y, -body.z, -body.w);
                }

                previous = body;
                var values = new[] { pose.bodyPosition.x, pose.bodyPosition.y, pose.bodyPosition.z, body.x, body.y, body.z, body.w };
                for (var k = 0; k < 7; k++)
                {
                    root[k].AddKey(time, values[k]);
                }

                for (var m = 0; m < HumanTrait.MuscleCount; m++)
                {
                    muscles[m].AddKey(time, pose.muscles[m]);
                }
            }

            handler.Dispose();
            Object.DestroyImmediate(model);
            var clip = new AnimationClip { name = Path.GetFileNameWithoutExtension(path), frameRate = rate };
            string[] rootNames = { "RootT.x", "RootT.y", "RootT.z", "RootQ.x", "RootQ.y", "RootQ.z", "RootQ.w" };
            // Every frame keyed made the idle a 22 MB file; the keys a straight line between their neighbours already
            // carries are dropped (authoring default: 0.002 of a muscle's range, about a fifth of a degree; 0.5 mm).
            var keys = 0;
            for (var k = 0; k < 7; k++)
            {
                var reduced = Reduce(root[k], 0.0005f);
                keys += reduced.length;
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), rootNames[k]), reduced);
            }

            for (var m = 0; m < HumanTrait.MuscleCount; m++)
            {
                var reduced = Reduce(muscles[m], 0.002f);
                keys += reduced.length;
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), SliceCharacterRigs.ClipMuscle(HumanTrait.MuscleName[m])), reduced);
            }

            InPlaceLoop(clip);
            Save(clip, path);
            report.AppendLine($"{path}: baked from {genericPath}, {frames + 1} frames, {groups.Count} bones, {keys} keys of {(frames + 1) * (7 + HumanTrait.MuscleCount)}, human motion {clip.humanMotion}");
        }

        /// <summary>The curve's keys a straight line from the last kept key cannot carry within <paramref name="tolerance"/> (Douglas–Peucker).</summary>
        private static AnimationCurve Reduce(AnimationCurve curve, float tolerance)
        {
            var all = curve.keys;
            if (all.Length < 3)
            {
                return curve;
            }

            var keep = new bool[all.Length];
            keep[0] = keep[all.Length - 1] = true;
            var spans = new Stack<(int A, int B)>();
            spans.Push((0, all.Length - 1));
            while (spans.Count > 0)
            {
                var (a, b) = spans.Pop();
                var worst = -1;
                var most = tolerance;
                for (var i = a + 1; i < b; i++)
                {
                    var line = Mathf.Lerp(all[a].value, all[b].value, (all[i].time - all[a].time) / (all[b].time - all[a].time));
                    var off = Mathf.Abs(all[i].value - line);
                    if (off > most)
                    {
                        most = off;
                        worst = i;
                    }
                }

                if (worst >= 0)
                {
                    keep[worst] = true;
                    spans.Push((a, worst));
                    spans.Push((worst, b));
                }
            }

            var reduced = new AnimationCurve();
            for (var i = 0; i < all.Length; i++)
            {
                if (keep[i])
                {
                    reduced.AddKey(all[i].time, all[i].value);
                }
            }

            for (var i = 0; i < reduced.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(reduced, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(reduced, i, AnimationUtility.TangentMode.Linear);
            }

            return reduced;
        }

        private static void Copy(AnimationClip source, string path, StringBuilder report)
        {
            var copy = Object.Instantiate(source);
            copy.name = Path.GetFileNameWithoutExtension(path);
            InPlaceLoop(copy);
            Save(copy, path);
            report.AppendLine($"{path}: the historical woman's own '{source.name}', {source.length:0.00} s, human motion {copy.humanMotion}");
        }

        /// <summary>A loop in place whose root stays as the clip has it: rotation, height and ground position baked into the pose.</summary>
        private static void InPlaceLoop(AnimationClip clip)
        {
            var s = AnimationUtility.GetAnimationClipSettings(clip);
            s.loopTime = true;
            s.loopBlendOrientation = true;
            s.loopBlendPositionY = true;
            s.loopBlendPositionXZ = true;
            s.keepOriginalOrientation = true;
            s.keepOriginalPositionY = true;
            s.keepOriginalPositionXZ = true;
            s.heightFromFeet = false;
            AnimationUtility.SetAnimationClipSettings(clip, s);
        }

        /// <summary>Written over the asset at <paramref name="path"/> where one is, so what refers to it keeps it.</summary>
        private static void Save(AnimationClip clip, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(clip, path);
                return;
            }

            EditorUtility.CopySerialized(clip, existing);
            existing.name = Path.GetFileNameWithoutExtension(path);
            EditorUtility.SetDirty(existing);
        }

        private static AnimationClip Clip(string path) => AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

        // ------------------------------------------------------------------------------------------- controllers

        /// <summary>
        /// A standing controller a sex (idle first, then walk), and the stand-ins put into the controllers already in use:
        /// the townsfolk's walk and idle, the people's idle, and the player's locomotion (the walk sped up for running).
        /// </summary>
        private static void Controllers()
        {
            Standing(ManController, Clip(ManIdle), Clip(ManWalk));
            Standing(WomanController, Clip(WomanIdle), Clip(WomanWalk));
            Repoint(ManWalkerController, Clip(ManIdle), Clip(ManWalk));
            Repoint(WomanWalkerController, Clip(WomanIdle), Clip(WomanWalk));
            Repoint(SliceCharacterLook.PersonIdlePath, Clip(ManIdle), Clip(ManWalk));

            var player = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerController);
            if (player != null)
            {
                foreach (var s in player.layers[0].stateMachine.states)
                {
                    if (s.state.motion is BlendTree tree)
                    {
                        var children = tree.children;
                        float[] pace = { 1f, 1f, 1.5f, 1.9f }; // idle, walk, run, sprint: authoring defaults until A1's run and sprint
                        for (var i = 0; i < children.Length; i++)
                        {
                            children[i].motion = i == 0 ? Clip(ManIdle) : Clip(ManWalk);
                            children[i].timeScale = pace[Mathf.Min(i, pace.Length - 1)];
                        }

                        tree.children = children;
                        EditorUtility.SetDirty(tree);
                    }
                    else
                    {
                        s.state.motion = Clip(ManIdle); // no jump or fall clip until A1
                    }
                }

                EditorUtility.SetDirty(player);
            }

            AssetDatabase.SaveAssets();
        }

        private static void Standing(string path, AnimationClip idle, AnimationClip walk)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path) ?? AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine = controller.layers[0].stateMachine;
            var idleState = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Idle") ?? machine.AddState("Idle");
            var walkState = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Walk") ?? machine.AddState("Walk");
            idleState.motion = idle;
            walkState.motion = walk;
            machine.defaultState = idleState;
            EditorUtility.SetDirty(controller);
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
            var walk = Clip(look.Female ? WomanWalk : ManWalk);
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
            var idle = Clip(look.Female ? WomanIdle : ManIdle);
            var walk = Clip(look.Female ? WomanWalk : ManWalk);
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
            var controllerPath = walking ? (look.Female ? WomanWalkerController : ManWalkerController) : (look.Female ? WomanController : ManController);
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

        /// <summary>The walk is in place: the lower foot moves back as fast as the body would go forward (median over the clip).</summary>
        private static float Speed(Animator animator, AnimationClip walk, Transform root)
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
            return Mathf.Clamp(speeds[speeds.Count / 2], 0.6f, 2f);
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
        /// at its stature, in the idle's first pose, playing the player's locomotion; the woman's through an override of
        /// its clips by her own stand-ins. The root and its components stay. Returns a line for the report.
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
            animator.runtimeAnimatorController = look.Female ? WomanPlayer() : AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerController);
            animator.applyRootMotion = false;
            var stature = Fit(model, Clip(look.Female ? WomanIdle : ManIdle), look.Height, false);
            var head = Keep(model, animator);
            SliceKitCatalogue.SetLayer(model, KitLayers.Character);
            return $"{look.File}, {stature:0.00} m (head bone at {head:0.00} m), {animator.runtimeAnimatorController.name}, upper arm {ArmDrop(animator):0}° from down";
        }

        /// <summary>The player's controller with the woman's idle and walk in place of the man's (stand-ins until A1).</summary>
        private static RuntimeAnimatorController WomanPlayer()
        {
            var basis = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerController);
            var woman = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(PlayerWomanController);
            if (woman == null)
            {
                woman = new AnimatorOverrideController(basis);
                AssetDatabase.CreateAsset(woman, PlayerWomanController);
            }

            woman.runtimeAnimatorController = basis;
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            woman.GetOverrides(overrides);
            for (var i = 0; i < overrides.Count; i++)
            {
                var key = AssetDatabase.GetAssetPath(overrides[i].Key);
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key,
                    key == ManIdle ? Clip(WomanIdle) : key == ManWalk ? Clip(WomanWalk) : null);
            }

            woman.ApplyOverrides(overrides);
            EditorUtility.SetDirty(woman);
            AssetDatabase.SaveAssets();
            return woman;
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
                foreach (var (clipPath, at) in new[] { (look.Female ? WomanIdle : ManIdle, 0.5f), (look.Female ? WomanWalk : ManWalk, 0.25f) })
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                    var clip = Clip(clipPath);
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
                foreach (var (path, at) in new[] { (look.Female ? WomanIdle : ManIdle, 0.5f), (look.Female ? WomanWalk : ManWalk, 0.25f) })
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(look.Standing));
                    var clip = Clip(path);
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

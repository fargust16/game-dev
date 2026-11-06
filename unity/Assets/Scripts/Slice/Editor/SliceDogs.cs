using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameDev.Slice.Kit;
using GameDev.Slice.People;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The owner's dog experiment (2026-11-06): the German shepherd clips of 3D Stylized Animated Dogs Kit (Bublisher)
    /// put on the owner's shepherd from Tripo, and a few dogs walking or sitting in Veilmouth, greeting the player.
    /// Both models carry the same Rigify rig, <c>german_gamerig</c>, 208 bones of the same names, so each clip is
    /// carried over bone by bone: the target bone turns in the world as the kit's bone turns from its rest, and a bone
    /// the clip moves moves by the same amount scaled to the dog's size. The owner's files are not changed; the clips,
    /// the controller, and the prefab are the project's own.
    /// Batch: -executeMethod GameDev.Slice.EditorTools.SliceDogs.BuildPlaceReplacingAndRender
    /// </summary>
    public static class SliceDogs
    {
        public const string ModelPath = "Assets/TripoModels/germanshepherd/germanshepherd.fbx";
        public const string DonorPath = "Assets/Bublisher/3D Stylized Animated Dogs Kit/Models/germanshepherd.fbx";
        public const string PrefabPath = "Assets/Prefabs/Slice/Dog.prefab";
        private const string Folder = "Assets/Settings/Slice/Dogs";
        private const string ControllerPath = Folder + "/SliceDog.controller";
        private const string ClipPrefix = "germanshepherd_";
        public const string GroupName = "Dogs";
        private const string OutputFolder = "Logs/SliceDogs";

        // Authoring defaults, 2026-11-06.
        public const float Height = 0.85f;       // m, the top of the ears standing at rest: a shepherd is about 0.6 m at the withers
        private const float Width = 0.32f;       // m, the body box
        private const float Length = 0.95f;      // m

        /// <summary>Where the dogs stand, world X and Z (plan px − 500, 500 − py): Bell Street by the player's start, the Market square, High Street, Ridge Street, and the Sea Quay.</summary>
        public static readonly Vector2[] Places = { new Vector2(176f, -275f), new Vector2(63f, -257f), new Vector2(165f, -235f), new Vector2(60f, -168f), new Vector2(100f, -321f) };

        private static string[] Feet => new[] { "DEF-foot.L", "DEF-foot.R", "DEF-front_foot.L", "DEF-front_foot.R" };

        private static string[] Toes => new[] { "DEF-toe.L", "DEF-toe.R", "DEF-front_toe.L", "DEF-front_toe.R" };

        [MenuItem("GameDev/Slice/Dogs/Build the Dog and Place Dogs in Veilmouth (once)")]
        public static void BuildAndPlace()
        {
            BuildPrefab();
            Place(false);
        }

        /// <summary>The agent's own batch iterations: replaces the group it placed.</summary>
        public static void BuildPlaceReplacingAndRender()
        {
            BuildPrefab();
            RenderPoses();
            Place(true);
        }

        /// <summary>The agent's check of the clips alone: the prefab and the poses, the scene untouched.</summary>
        public static void BuildAndRender()
        {
            BuildPrefab();
            RenderPoses();
        }

        // ------------------------------------------------------------------------------------------- clips

        /// <summary>Every clip of the kit's shepherd, carried over to the owner's dog.</summary>
        public static Dictionary<string, AnimationClip> RetargetAll()
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            var clips = AssetDatabase.LoadAllAssetsAtPath(DonorPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToList();
            var result = new Dictionary<string, AnimationClip>();
            foreach (var clip in clips)
            {
                var name = clip.name.StartsWith(ClipPrefix) ? clip.name.Substring(ClipPrefix.Length) : clip.name;
                result[name] = Retarget(clip, $"{Folder}/{name}.anim");
            }

            AssetDatabase.SaveAssets();
            return result;
        }

        private static AnimationClip Retarget(AnimationClip clip, string path)
        {
            var donor = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(DonorPath));
            var target = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
            var d = ByName(donor);
            var t = ByName(target);

            // Turn the kit's dog so its body faces as the owner's dog does, so a turn in the world means the same.
            donor.transform.rotation = Frame(t) * Quaternion.Inverse(Frame(d)) * donor.transform.rotation;
            var donorPose = (donor.transform.position, donor.transform.rotation);
            var ratio = BodyLength(t) / Mathf.Max(1e-4f, BodyLength(d));

            var common = t.Keys.Where(d.ContainsKey).OrderBy(k => Depth(t[k], target.transform)).ToList();
            var moved = new HashSet<string>(AnimationUtility.GetCurveBindings(clip)
                .Where(b => b.type == typeof(Transform) && b.propertyName.StartsWith("m_LocalPosition"))
                .Select(b => b.path.Split('/').Last()));
            var donorRest = common.ToDictionary(k => k, k => d[k].rotation);
            var targetRest = common.ToDictionary(k => k, k => t[k].rotation);
            var donorRestLocal = common.ToDictionary(k => k, k => d[k].localPosition);
            var targetRestLocal = common.ToDictionary(k => k, k => t[k].localPosition);

            // The kit's clips may carry the body below the ground (its sit sinks the hind paws about 0.26 m below the
            // toe bones): each frame is lifted or lowered so the lowest point of the skin stands where it stands at
            // rest. The top bones carry the shift.
            var restLow = Seen(target).Min.y;
            var tops = common.Where(k => !t[k].parent.name.StartsWith("DEF-")).ToList();
            foreach (var k in tops)
            {
                moved.Add(k);
            }

            const float rate = 30f;
            var frames = Mathf.Max(2, Mathf.RoundToInt(clip.length * rate));
            var rotation = common.ToDictionary(k => k, _ => new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve(), new AnimationCurve() });
            var position = common.Where(moved.Contains).ToDictionary(k => k, _ => new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve() });
            var previous = new Dictionary<string, Quaternion>();
            var sinkMin = float.MaxValue;
            var sinkMax = float.MinValue;
            for (var i = 0; i <= frames; i++)
            {
                var time = clip.length * i / frames;
                clip.SampleAnimation(donor, time);
                donor.transform.SetPositionAndRotation(donorPose.position, donorPose.rotation);
                foreach (var k in common)
                {
                    t[k].rotation = d[k].rotation * Quaternion.Inverse(donorRest[k]) * targetRest[k];
                    if (position.ContainsKey(k) && d[k].parent != null && t[k].parent != null)
                    {
                        var world = d[k].parent.TransformVector(d[k].localPosition - donorRestLocal[k]) * ratio;
                        t[k].localPosition = targetRestLocal[k] + t[k].parent.InverseTransformVector(world);
                    }
                }

                var sink = Seen(target).Min.y - restLow;
                sinkMin = Mathf.Min(sinkMin, sink);
                sinkMax = Mathf.Max(sinkMax, sink);
                foreach (var k in tops)
                {
                    t[k].position -= new Vector3(0f, sink, 0f);
                }

                foreach (var k in common)
                {
                    var q = t[k].localRotation;
                    if (previous.TryGetValue(k, out var p) && Quaternion.Dot(p, q) < 0f)
                    {
                        q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                    }

                    previous[k] = q;
                    rotation[k][0].AddKey(time, q.x);
                    rotation[k][1].AddKey(time, q.y);
                    rotation[k][2].AddKey(time, q.z);
                    rotation[k][3].AddKey(time, q.w);
                    if (position.TryGetValue(k, out var curves))
                    {
                        var at = t[k].localPosition;
                        curves[0].AddKey(time, at.x);
                        curves[1].AddKey(time, at.y);
                        curves[2].AddKey(time, at.z);
                    }
                }
            }

            var result = new AnimationClip { name = Path.GetFileNameWithoutExtension(path), frameRate = rate };
            foreach (var k in common)
            {
                var bonePath = AnimationUtility.CalculateTransformPath(t[k], target.transform);
                result.SetCurve(bonePath, typeof(Transform), "localRotation.x", rotation[k][0]);
                result.SetCurve(bonePath, typeof(Transform), "localRotation.y", rotation[k][1]);
                result.SetCurve(bonePath, typeof(Transform), "localRotation.z", rotation[k][2]);
                result.SetCurve(bonePath, typeof(Transform), "localRotation.w", rotation[k][3]);
                if (position.TryGetValue(k, out var curves))
                {
                    result.SetCurve(bonePath, typeof(Transform), "localPosition.x", curves[0]);
                    result.SetCurve(bonePath, typeof(Transform), "localPosition.y", curves[1]);
                    result.SetCurve(bonePath, typeof(Transform), "localPosition.z", curves[2]);
                }
            }

            result.EnsureQuaternionContinuity();
            var settings = AnimationUtility.GetAnimationClipSettings(result);
            settings.loopTime = clip.isLooping;
            AnimationUtility.SetAnimationClipSettings(result, settings);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(result, path);
            var rigScale = target.transform.lossyScale.y;
            Object.DestroyImmediate(donor);
            Object.DestroyImmediate(target);
            Debug.Log($"[SliceDogs] {clip.name}: {common.Count} bones, {position.Count} moved, {tops.Count} top ({string.Join(",", tops.Take(4))}), sink {sinkMin:0.000}…{sinkMax:0.000} m, rig scale {rigScale:0.###}, {frames + 1} frames, size ×{ratio:0.000}, looping {clip.isLooping}, {path}.");
            return result;
        }

        private static Dictionary<string, Transform> ByName(GameObject model)
        {
            var bones = new Dictionary<string, Transform>();
            foreach (var b in model.GetComponentsInChildren<Transform>())
            {
                if (b != model.transform && b.name.StartsWith("DEF-") && !bones.ContainsKey(b.name))
                {
                    bones[b.name] = b;
                }
            }

            return bones;
        }

        private static int Depth(Transform bone, Transform root)
        {
            var depth = 0;
            for (var b = bone; b != null && b != root; b = b.parent)
            {
                depth++;
            }

            return depth;
        }

        private static Vector3 Mid(Dictionary<string, Transform> b, string left, string right) => (b[left].position + b[right].position) * 0.5f;

        /// <summary>The body's frame at rest: forward from the hind paws to the fore paws, right from the left paws to the right.</summary>
        private static Quaternion Frame(Dictionary<string, Transform> b)
        {
            var forward = Mid(b, "DEF-front_foot.L", "DEF-front_foot.R") - Mid(b, "DEF-foot.L", "DEF-foot.R");
            var right = (b["DEF-foot.R"].position + b["DEF-front_foot.R"].position) - (b["DEF-foot.L"].position + b["DEF-front_foot.L"].position);
            var up = Vector3.Cross(forward, right);
            return Quaternion.LookRotation(forward, up);
        }

        private static float BodyLength(Dictionary<string, Transform> b) =>
            (Mid(b, "DEF-front_foot.L", "DEF-front_foot.R") - Mid(b, "DEF-foot.L", "DEF-foot.R")).magnitude;

        // ------------------------------------------------------------------------------------------- the prefab

        public static GameObject BuildPrefab()
        {
            var clips = RetargetAll();
            var root = new GameObject("Dog");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            SliceKitCatalogue.LinkTripoTextures(model, ModelPath);

            // Size, paws, and facing in the standing pose.
            var rest = clips["Breathing"];
            rest.SampleAnimation(model, 0f);
            var (min, max) = Seen(model);
            model.transform.localScale = Vector3.one * (Height / Mathf.Max(0.05f, max.y - min.y));
            var b = ByName(model);
            var forward = Mid(b, "DEF-front_foot.L", "DEF-front_foot.R") - Mid(b, "DEF-foot.L", "DEF-foot.R");
            forward.y = 0f;
            model.transform.localRotation = Quaternion.FromToRotation(forward.normalized, Vector3.forward) * model.transform.localRotation;
            rest.SampleAnimation(model, 0f);
            (min, max) = Seen(model);
            model.transform.localPosition -= new Vector3((min.x + max.x) * 0.5f, min.y, (min.z + max.z) * 0.5f);

            var speed = MeasureSpeed(model, clips["Walking01"]);
            rest.SampleAnimation(model, 0f);

            var animator = model.GetComponent<Animator>();
            if (animator == null)
            {
                animator = model.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = Controller(clips);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(Width, Height * 0.8f, Length);
            box.center = new Vector3(0f, Height * 0.4f, 0f);
            SliceTownsfolk.ShoulderLooseProps(root);
            root.AddComponent<StreetDog>().Configure(new Vector3[0], speed, SliceTownsfolk.GroundMask, animator, 1);
            SliceKitCatalogue.SetLayer(root, KitLayers.Character);

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath) ?? "Assets");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[SliceDogs] {PrefabPath}: {Height} m to the ears, walking {speed:0.00} m/s (measured from the walk's stride), {clips.Count} clips.");
            return prefab;
        }

        /// <summary>The walk is in place: a paw on the ground moves back as fast as the body would move forward. The speed is the median backward speed of the lowest toe.</summary>
        private static float MeasureSpeed(GameObject model, AnimationClip walk)
        {
            var b = ByName(model);
            var toes = Toes.Select(n => b[n]).ToArray();
            var parent = model.transform.parent;
            const int samples = 120;
            var dt = walk.length / samples;
            var speeds = new List<float>();
            walk.SampleAnimation(model, 0f);
            var before = toes.Select(x => parent.InverseTransformPoint(x.position)).ToArray();
            for (var i = 1; i <= samples; i++)
            {
                walk.SampleAnimation(model, i * dt);
                var now = toes.Select(x => parent.InverseTransformPoint(x.position)).ToArray();
                var lowest = 0;
                for (var j = 1; j < now.Length; j++)
                {
                    if (now[j].y < now[lowest].y)
                    {
                        lowest = j;
                    }
                }

                speeds.Add(-(now[lowest].z - before[lowest].z) / dt);
                before = now;
            }

            speeds.Sort();
            return Mathf.Clamp(speeds[speeds.Count / 2], 0.4f, 3f);
        }

        private static (Vector3 Min, Vector3 Max) Seen(GameObject model)
        {
            var min = Vector3.positiveInfinity;
            var max = Vector3.negativeInfinity;
            var parent = model.transform.parent;
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh();
                smr.BakeMesh(mesh, true);
                var m = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
                foreach (var v in mesh.vertices)
                {
                    var w = parent != null ? parent.InverseTransformPoint(m.MultiplyPoint3x4(v)) : m.MultiplyPoint3x4(v);
                    min = Vector3.Min(min, w);
                    max = Vector3.Max(max, w);
                }

                Object.DestroyImmediate(mesh);
            }

            return (min, max);
        }

        /// <summary>One state for each clip, named after it; each Start goes on to its Cycle, and AngryEnd back to Breathing.</summary>
        private static RuntimeAnimatorController Controller(Dictionary<string, AnimationClip> clips)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var machine = controller.layers[0].stateMachine;
            var states = new Dictionary<string, AnimatorState>();
            foreach (var pair in clips.OrderBy(p => p.Key))
            {
                var state = machine.AddState(pair.Key);
                state.motion = pair.Value;
                states[pair.Key] = state;
            }

            void Then(string from, string to)
            {
                if (states.TryGetValue(from, out var a) && states.TryGetValue(to, out var b))
                {
                    var transition = a.AddTransition(b);
                    transition.hasExitTime = true;
                    transition.exitTime = 0.95f;
                    transition.duration = 0.1f;
                }
            }

            Then("SittingStart", "SittingCycle");
            Then("EatingStart", "EatingCycle");
            Then("AngryStart", "AngryCycle");
            Then("AngryEnd", "Breathing");
            if (states.TryGetValue("Breathing", out var idle))
            {
                machine.defaultState = idle;
            }

            AssetDatabase.SaveAssets();
            return controller;
        }

        // ------------------------------------------------------------------------------------------- Veilmouth

        /// <summary>
        /// The dogs go under the city's root, in a group of their own, each on a route round its place. Once in the
        /// owner's scene: it refuses to run where the group exists, unless the agent replaces its own group.
        /// </summary>
        private static void Place(bool replacing)
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var city = scene.GetRootGameObjects().FirstOrDefault(g => g.name == VeilmouthCity.RootName);
            if (city == null)
            {
                Debug.LogError($"[SliceDogs] Veilmouth has no {VeilmouthCity.RootName}.");
                return;
            }

            var old = city.transform.Find(GroupName);
            if (old != null)
            {
                if (!replacing)
                {
                    Debug.LogWarning("[SliceDogs] Veilmouth already has its dogs; they are the owner's now and are not placed again.");
                    return;
                }

                Object.DestroyImmediate(old.gameObject);
            }

            Physics.SyncTransforms();
            var group = new GameObject(GroupName).transform;
            group.SetParent(city.transform, false);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var placed = new List<GameObject>();
            for (var i = 0; i < Places.Length; i++)
            {
                var at = new Vector3(Places[i].x, 60f, Places[i].y);
                at.y = Physics.Raycast(at, Vector3.down, out var hit, 80f, SliceTownsfolk.GroundMask, QueryTriggerInteraction.Ignore) ? hit.point.y : 26f;
                var route = SliceTownsfolk.PlanRoute(at);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                go.name = $"Dog {i + 1}";
                go.SetActive(false); // planned routes ignore the dogs placed before them
                go.transform.SetPositionAndRotation(route.Length > 0 ? route[0] : at, Quaternion.Euler(0f, i * 72f, 0f));
                var dog = go.GetComponent<StreetDog>();
                dog.Configure(route, dog.Speed, SliceTownsfolk.GroundMask, go.GetComponentInChildren<Animator>(), 101 + i);
                EditorUtility.SetDirty(dog);
                placed.Add(go);
                Debug.Log($"[SliceDogs] {go.name} at {at}: a route of {route.Length} points.");
            }

            foreach (var go in placed)
            {
                go.SetActive(true);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SliceDogs] {placed.Count} dogs placed in {VeilmouthScene.ScenePath}.");
        }

        // ------------------------------------------------------------------------------------------- the check

        /// <summary>The kit's shepherd and the owner's dog side by side, from the side, in a few moments of each clip the dog uses.</summary>
        public static void RenderPoses()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Directory.CreateDirectory(OutputFolder);
            var kit = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(DonorPath));
            var dog = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            var kitModel = kit;
            var kitClips = AssetDatabase.LoadAllAssetsAtPath(DonorPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToDictionary(c => c.name);
            kitClips[ClipPrefix + "Breathing"].SampleAnimation(kit, 0f);
            var kb = ByName(kit);
            var kitForward = Mid(kb, "DEF-front_foot.L", "DEF-front_foot.R") - Mid(kb, "DEF-foot.L", "DEF-foot.R");
            kitForward.y = 0f;
            kit.transform.rotation = Quaternion.FromToRotation(kitForward.normalized, Vector3.forward) * kit.transform.rotation;
            var (kmin, kmax) = SeenWorld(kit);
            kit.transform.localScale *= Height / Mathf.Max(0.05f, kmax.y - kmin.y);
            (kmin, kmax) = SeenWorld(kit);
            kit.transform.position += new Vector3(-(kmin.x + kmax.x) * 0.5f, -kmin.y, 0.8f - ((kmin.z + kmax.z) * 0.5f));
            dog.transform.position = new Vector3(0f, 0f, -0.8f);
            var dogModel = dog.GetComponentInChildren<Animator>().gameObject;
            var side = Quaternion.LookRotation(new Vector3(4f, -1.2f, 0f));
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.localScale = new Vector3(0.4f, 1f, 0.4f);

            foreach (var name in new[] { "Walking01", "Breathing", "SittingStart", "SittingCycle", "WigglingTail", "Running01" })
            {
                var mine = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{Folder}/{name}.anim");
                var theirs = kitClips[ClipPrefix + name];
                var tiles = new List<Texture2D>();
                foreach (var f in new[] { 0f, 0.25f, 0.5f, 0.75f })
                {
                    theirs.SampleAnimation(kitModel, theirs.length * f);
                    mine.SampleAnimation(dogModel, mine.length * f);                    tiles.Add(Shoot(new Vector3(-4f, 1.6f, 0f), side, 1.6f));
                }

                var sheet = new Texture2D(tiles[0].width * tiles.Count, tiles[0].height, TextureFormat.RGB24, false);
                for (var i = 0; i < tiles.Count; i++)
                {
                    sheet.SetPixels(i * tiles[i].width, 0, tiles[i].width, tiles[i].height, tiles[i].GetPixels());
                }

                File.WriteAllBytes($"{OutputFolder}/{name}.png", sheet.EncodeToPNG());
            }

            Debug.Log($"[SliceDogs] Poses written to {OutputFolder}: the kit's shepherd on the left, the owner's dog on the right, both facing left.");
        }

        private static (Vector3 Min, Vector3 Max) SeenWorld(GameObject model)
        {
            var bounds = new Bounds(model.transform.position, Vector3.zero);
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                bounds.Encapsulate(r.bounds);
            }

            return (bounds.min, bounds.max);
        }

        private static Texture2D Shoot(Vector3 position, Quaternion rotation, float size)
        {
            const int tile = 512;
            var camera = new GameObject("DogCamera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = size * 0.5f;
            camera.aspect = 2f;
            camera.transform.SetPositionAndRotation(position, rotation);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.75f, 0.85f, 0.95f);
            var target = new RenderTexture(tile * 2, tile, 24);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(tile * 2, tile, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, tile * 2, tile), 0, 0);
            image.Apply();
            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(camera.gameObject);
            return image;
        }
    }
}

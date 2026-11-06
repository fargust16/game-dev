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
    /// The ordinary townsfolk (owner, 2026-11-06): the owner's Victorian gentleman from Tripo, with its own walk and idle,
    /// replaces the yellow Dummies, and each walks a closed route round its place until S5c gives it a schedule.
    /// <list type="bullet">
    /// <item>The model is scaled to 1.75 m and turned to face +Z; its clips are copied to looping project clips; the
    /// owner's files are not changed.</item>
    /// <item>The walking speed is measured from the walk clip's stride, so the feet do not slide.</item>
    /// <item>A route is planned on 1 m cells round the person's place, over ground a person's capsule fits on, then
    /// straightened where a person fits along the straight line.</item>
    /// </list>
    /// Batch: -executeMethod GameDev.Slice.EditorTools.SliceTownsfolk.BuildAndReplace
    /// </summary>
    public static class SliceTownsfolk
    {
        public const string ModelPath = "Assets/TripoModels/victorian_gentleman_3d_model_1/victorian_gentleman_3d_model_1.fbx";
        public const string PrefabPath = "Assets/Prefabs/Slice/Townsperson.prefab";
        private const string AnimationFolder = "Assets/Settings/Slice/Townsfolk";
        private const string ControllerPath = AnimationFolder + "/SliceTownsfolk.controller";

        // Authoring defaults, 2026-11-06.
        public const float Height = 1.75f;       // m
        private const float Radius = 0.3f;       // the controller's radius (kit standard §3)
        private const float Step = 0.4f;         // the controller's step (kit standard §3)
        private const int Reach = 22;            // cells round the person's place a route may use
        private const float LoopMin = 8f;        // m from the place to a turning point
        private const float LoopMax = 18f;       // m

        private static int BlockMask =>
            LayerMask.GetMask("Default", KitLayers.Ground, KitLayers.Structure, KitLayers.Prop, KitLayers.Vegetation, KitLayers.Interactable, KitLayers.Character);

        public static LayerMask GroundMask => LayerMask.GetMask(KitLayers.Ground, KitLayers.Structure);

        [MenuItem("GameDev/Slice/Townsfolk/Build the Townsperson and Replace the Yellow Dummies in Veilmouth (once)")]
        public static void BuildAndReplace()
        {
            BuildPrefab();
            ReplaceInVeilmouth();
        }

        // ------------------------------------------------------------------------------------------- the prefab

        public static GameObject BuildPrefab()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToList();
            var walkSource = clips.First(c => c.name.StartsWith("walk"));
            var idleSource = clips.First(c => c.name.StartsWith("idle"));

            var root = new GameObject("Townsperson");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            SliceKitCatalogue.LinkTripoTextures(model, ModelPath);

            // Size, feet and facing in the walk's first pose.
            walkSource.SampleAnimation(model, 0f);
            var (min, max) = Seen(model);
            model.transform.localScale = Vector3.one * (Height / Mathf.Max(0.1f, max.y - min.y));
            var forward = Facing(model);
            model.transform.localRotation = Quaternion.FromToRotation(forward, Vector3.forward);
            walkSource.SampleAnimation(model, 0f);
            (min, max) = Seen(model);
            model.transform.localPosition -= new Vector3((min.x + max.x) * 0.5f, min.y, (min.z + max.z) * 0.5f);

            var speed = MeasureSpeed(model, walkSource);

            var animator = model.GetComponent<Animator>();
            if (animator == null)
            {
                animator = model.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = Controller(walkSource, idleSource);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = Height;
            capsule.radius = 0.25f;
            capsule.center = new Vector3(0f, Height * 0.5f, 0f);
            root.AddComponent<TownsfolkWalker>().Configure(new Vector3[0], speed, GroundMask);
            SliceKitCatalogue.SetLayer(root, KitLayers.Character);

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath) ?? "Assets");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[SliceTownsfolk] {PrefabPath}: {Height} m, facing turned from {forward}, walking {speed:0.00} m/s (measured from the walk's stride).");
            return prefab;
        }

        /// <summary>The model's forward: from the head to between the eyes, flat.</summary>
        private static Vector3 Facing(GameObject model)
        {
            var bones = model.GetComponentsInChildren<Transform>();
            var head = bones.First(t => t.name == "Head").position;
            var eyes = (bones.First(t => t.name == "Left_Eye").position + bones.First(t => t.name == "Right_Eye").position) * 0.5f;
            var f = eyes - head;
            f.y = 0f;
            return f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
        }

        /// <summary>
        /// The walk is in place: a foot on the ground moves back as fast as the body would move forward. The speed is
        /// the median backward speed of the lower foot over the clip, in the prefab's own metres.
        /// </summary>
        private static float MeasureSpeed(GameObject model, AnimationClip walk)
        {
            var bones = model.GetComponentsInChildren<Transform>();
            var left = bones.First(t => t.name == "Left_Foot");
            var right = bones.First(t => t.name == "Right_Foot");
            const int samples = 120;
            var dt = walk.length / samples;
            var speeds = new List<float>();
            walk.SampleAnimation(model, 0f);
            var previous = (left.position, right.position);
            for (var i = 1; i <= samples; i++)
            {
                walk.SampleAnimation(model, i * dt);
                var lower = left.position.y < right.position.y;
                var now = lower ? left.position : right.position;
                var before = lower ? previous.Item1 : previous.Item2;
                speeds.Add(-(now.z - before.z) / dt);
                previous = (left.position, right.position);
            }

            speeds.Sort();
            var speed = speeds[speeds.Count / 2];
            walk.SampleAnimation(model, 0f);
            return Mathf.Clamp(speed, 0.6f, 2f);
        }

        private static (Vector3 Min, Vector3 Max) Seen(GameObject model)
        {
            var min = Vector3.positiveInfinity;
            var max = Vector3.negativeInfinity;
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh();
                smr.BakeMesh(mesh, true);
                var m = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
                foreach (var v in mesh.vertices)
                {
                    var w = model.transform.parent.InverseTransformPoint(m.MultiplyPoint3x4(v));
                    min = Vector3.Min(min, w);
                    max = Vector3.Max(max, w);
                }

                Object.DestroyImmediate(mesh);
            }

            return (min, max);
        }

        private static RuntimeAnimatorController Controller(AnimationClip walkSource, AnimationClip idleSource)
        {
            Directory.CreateDirectory(AnimationFolder);
            var walk = LoopingCopy(walkSource, AnimationFolder + "/Walk.anim");
            var idle = LoopingCopy(idleSource, AnimationFolder + "/Idle.anim");
            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var machine = controller.layers[0].stateMachine;
            var walkState = machine.AddState("Walk");
            walkState.motion = walk;
            machine.AddState("Idle").motion = idle;
            machine.defaultState = walkState;
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip LoopingCopy(AnimationClip source, string path)
        {
            AssetDatabase.DeleteAsset(path);
            var copy = Object.Instantiate(source);
            copy.name = Path.GetFileNameWithoutExtension(path);
            var settings = AnimationUtility.GetAnimationClipSettings(copy);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(copy, settings);
            AssetDatabase.CreateAsset(copy, path);
            return copy;
        }

        // ------------------------------------------------------------------------------------------- Veilmouth

        /// <summary>
        /// Once, in the owner's Veilmouth: every yellow Dummy becomes a walking townsperson at its place. It refuses to
        /// run where townsfolk already walk, since the scene is the owner's.
        /// </summary>
        public static void ReplaceInVeilmouth()
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == VeilmouthCity.RootName);
            var people = root != null ? root.transform.Find("People") : null;
            if (people == null)
            {
                Debug.LogError("[SliceTownsfolk] Veilmouth has no People group.");
                return;
            }

            if (people.GetComponentsInChildren<TownsfolkWalker>(true).Length > 0)
            {
                Debug.LogWarning("[SliceTownsfolk] Veilmouth's townsfolk already walk; they are the owner's now and are not replaced again.");
                return;
            }

            var yellow = people.Cast<Transform>()
                .Where(t => PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject) is GameObject s && s.name.Contains("Yellow"))
                .Select(t => (t.name, t.position, t.rotation, t.gameObject)).ToList();
            foreach (var y in yellow)
            {
                Object.DestroyImmediate(y.gameObject);
            }

            Physics.SyncTransforms();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var placed = new List<GameObject>();
            foreach (var (name, position, rotation, _) in yellow)
            {
                var route = PlanRoute(position);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, people);
                go.name = name;
                go.SetActive(false); // planned routes ignore the people placed before them
                go.transform.SetPositionAndRotation(route.Length > 0 ? route[0] : position, rotation);
                var walker = go.GetComponent<TownsfolkWalker>();
                walker.Configure(route, walker.Speed, GroundMask);
                EditorUtility.SetDirty(walker);
                placed.Add(go);
                Debug.Log($"[SliceTownsfolk] {name} at {position}: a route of {route.Length} points, {walker.Length:0} m.");
            }

            foreach (var go in placed)
            {
                go.SetActive(true);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SliceTownsfolk] {placed.Count} yellow Dummies replaced by walking townsfolk in {VeilmouthScene.ScenePath}.");
        }

        // ------------------------------------------------------------------------------------------- routes

        /// <summary>
        /// A closed route round <paramref name="place"/>: up to four turning points 8–18 m away, one in each quarter,
        /// joined by the shortest walkable cell paths, then straightened. With fewer than two turning points the person
        /// walks out to the farthest one and back.
        /// </summary>
        public static Vector3[] PlanRoute(Vector3 place)
        {
            var cx = Mathf.FloorToInt(place.x);
            var cz = Mathf.FloorToInt(place.z);
            var cells = new Dictionary<(int, int), float>();
            if (!TrySurface(cx, cz, place.y, out var y0))
            {
                return new Vector3[0];
            }

            // Every cell reachable from the place, with its floor height.
            cells[(cx, cz)] = y0;
            var queue = new Queue<(int, int)>();
            queue.Enqueue((cx, cz));
            while (queue.Count > 0)
            {
                var (x, z) = queue.Dequeue();
                foreach (var (nx, nz) in Neighbours(x, z))
                {
                    if (cells.ContainsKey((nx, nz)) || Mathf.Abs(nx - cx) > Reach || Mathf.Abs(nz - cz) > Reach)
                    {
                        continue;
                    }

                    if (TrySurface(nx, nz, cells[(x, z)], out var ny) && Passable(Centre(x, z, cells[(x, z)]), Centre(nx, nz, ny)))
                    {
                        cells[(nx, nz)] = ny;
                        queue.Enqueue((nx, nz));
                    }
                }
            }

            var home = Paths(cells, (cx, cz));
            var turns = new List<(int, int)>();
            for (var quarter = 0; quarter < 4; quarter++)
            {
                var best = default((int, int));
                var bestScore = -1f;
                foreach (var cell in cells.Keys)
                {
                    var d = new Vector2(cell.Item1 - cx, cell.Item2 - cz);
                    var distance = d.magnitude;
                    if (distance < LoopMin || distance > LoopMax || Quarter(d) != quarter
                        || !home.Distance.TryGetValue(cell, out var walked) || walked > distance * 1.6f)
                    {
                        continue;
                    }

                    if (distance > bestScore)
                    {
                        bestScore = distance;
                        best = cell;
                    }
                }

                if (bestScore > 0f)
                {
                    turns.Add(best);
                }
            }

            var path = new List<(int, int)>();
            if (turns.Count >= 2)
            {
                for (var i = 0; i < turns.Count && path != null; i++)
                {
                    var leg = Paths(cells, turns[i]).To(turns[(i + 1) % turns.Count]);
                    if (leg == null)
                    {
                        path = null;
                        break;
                    }

                    path.AddRange(leg.Take(leg.Count - 1));
                }
            }

            if (path == null || path.Count == 0)
            {
                path = new List<(int, int)>();
                var far = home.Distance.OrderByDescending(c => c.Value).First().Key;
                var leg = home.To(far);
                path.AddRange(leg);
                path.AddRange(Enumerable.Reverse(leg).Skip(1).Take(leg.Count - 2));
            }

            var points = path.Select(c => Centre(c.Item1, c.Item2, cells[c])).ToList();
            return Straighten(points, cells).ToArray();
        }

        private static IEnumerable<(int, int)> Neighbours(int x, int z)
        {
            yield return (x + 1, z);
            yield return (x - 1, z);
            yield return (x, z + 1);
            yield return (x, z - 1);
        }

        private static int Quarter(Vector2 d) => d.x >= 0 ? (d.y >= 0 ? 0 : 3) : (d.y >= 0 ? 1 : 2);

        private static Vector3 Centre(int x, int z, float y) => new Vector3(x + 0.5f, y, z + 0.5f);

        /// <summary>A floor a person stands on in this cell, near <paramref name="nearY"/>: on the ground or a building's
        /// floor, not too steep, and with room for the person's capsule.</summary>
        private static bool TrySurface(int x, int z, float nearY, out float y)
        {
            y = 0f;
            var origin = new Vector3(x + 0.5f, nearY + 1.2f, z + 0.5f);
            if (!Physics.Raycast(origin, Vector3.down, out var hit, 2.4f, BlockMask, QueryTriggerInteraction.Ignore)
                || (GroundMask.value & (1 << hit.collider.gameObject.layer)) == 0
                || Vector3.Angle(hit.normal, Vector3.up) > 40f)
            {
                return false;
            }

            y = hit.point.y;
            return !Physics.CheckCapsule(hit.point + (Vector3.up * (Step + Radius)), hit.point + (Vector3.up * (Height - Radius)), Radius, BlockMask, QueryTriggerInteraction.Ignore);
        }

        private static bool Passable(Vector3 a, Vector3 b)
        {
            if (Mathf.Abs(b.y - a.y) > Step)
            {
                return false;
            }

            var delta = b - a;
            return !Physics.CapsuleCast(
                a + (Vector3.up * (Step + Radius)), a + (Vector3.up * (Height - Radius)), Radius,
                delta.normalized, delta.magnitude, BlockMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Drops the points a straight walk can skip: every 0.25 m of the straight line stands on a reachable
        /// cell, near its floor, with room for the person.</summary>
        private static List<Vector3> Straighten(List<Vector3> points, Dictionary<(int, int), float> cells)
        {
            if (points.Count < 3)
            {
                return points;
            }

            var result = new List<Vector3> { points[0] };
            var i = 0;
            while (i < points.Count - 1)
            {
                var j = i + 1;
                for (var k = Mathf.Min(points.Count - 1, i + 30); k > i + 1; k--)
                {
                    if (Straight(points[i], points[k], cells))
                    {
                        j = k;
                        break;
                    }
                }

                result.Add(points[j]);
                i = j;
            }

            // The loop closes from the last point to the first.
            if ((result[result.Count - 1] - result[0]).sqrMagnitude < 0.01f)
            {
                result.RemoveAt(result.Count - 1);
            }

            return result;
        }

        private static bool Straight(Vector3 a, Vector3 b, Dictionary<(int, int), float> cells)
        {
            var length = new Vector2(b.x - a.x, b.z - a.z).magnitude;
            var steps = Mathf.CeilToInt(length / 0.25f);
            var previousY = a.y;
            for (var s = 1; s < steps; s++)
            {
                var p = Vector3.Lerp(a, b, s / (float)steps);
                if (!cells.TryGetValue((Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.z)), out var y) || Mathf.Abs(y - previousY) > Step)
                {
                    return false;
                }

                p.y = y;
                if (Physics.CheckCapsule(p + (Vector3.up * (Step + Radius)), p + (Vector3.up * (Height - Radius)), Radius, BlockMask, QueryTriggerInteraction.Ignore))
                {
                    return false;
                }

                previousY = y;
            }

            return true;
        }

        private sealed class PathTree
        {
            public readonly Dictionary<(int, int), int> Distance = new Dictionary<(int, int), int>();
            public readonly Dictionary<(int, int), (int, int)> Parent = new Dictionary<(int, int), (int, int)>();
            public (int, int) Source;

            /// <summary>The cells from the source to <paramref name="target"/>, both included, or null if it is not reached.</summary>
            public List<(int, int)> To((int, int) target)
            {
                var path = new List<(int, int)> { target };
                while (!path[path.Count - 1].Equals(Source))
                {
                    if (!Parent.TryGetValue(path[path.Count - 1], out var parent))
                    {
                        return null;
                    }

                    path.Add(parent);
                }

                path.Reverse();
                return path;
            }
        }

        private static PathTree Paths(Dictionary<(int, int), float> cells, (int, int) source)
        {
            var tree = new PathTree { Source = source };
            tree.Distance[source] = 0;
            var queue = new Queue<(int, int)>();
            queue.Enqueue(source);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                foreach (var n in Neighbours(c.Item1, c.Item2))
                {
                    if (!cells.ContainsKey(n) || tree.Distance.ContainsKey(n)
                        || !Passable(Centre(c.Item1, c.Item2, cells[c]), Centre(n.Item1, n.Item2, cells[n])))
                    {
                        continue;
                    }

                    tree.Distance[n] = tree.Distance[c] + 1;
                    tree.Parent[n] = c;
                    queue.Enqueue(n);
                }
            }

            return tree;
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The blacksmith at work (owner's play test, 2026-11-11: his arm bent wrongly and he struck the air with no hammer).
    /// <list type="bullet">
    /// <item>He holds the Medieval Market's hand hammer, the pack of his anvil, through its kit wrapper, a quarter of the
    /// way up its handle. It is set on his body as it stands at the blow (the frame his right wrist is lowest): the handle
    /// runs forward from the fist, the head a little lower, its face down on the iron. The anvil is the pack's plain one,
    /// so no second hammer leans on it.</item>
    /// <item>The blow is measured on the Hammer clip as the game plays it: the frame the hammer's lowest point is lowest
    /// over the soles is the strike.</item>
    /// <item>In Veilmouth he stands where that point lands on the anvil's face, facing the anvil as he did, and a hot bar of
    /// iron lies there under the blow.</item>
    /// </list>
    /// A close-up sheet of the swing round the strike is written to <c>Logs/SliceSurvey/blacksmith.png</c>.
    /// Batch: -executeMethod GameDev.Slice.EditorTools.SliceBlacksmith.Run
    /// </summary>
    public static class SliceBlacksmith
    {
        public const string HammerKey = "SmithHammer";
        public const string HammerName = "Hammer";
        public const string WorkpieceName = "Blacksmith_Workpiece";
        public const string StumpName = "Anvil_Stump";
        private const string WorkpieceMaterial = SliceKitCatalogue.MaterialFolder + "/HotIron.mat";
        private const string Output = "Logs/SliceSurvey";

        // Authoring defaults: a smith's hand hammer of about 0.38 m held a quarter of the way up its handle, whose wood
        // lies this far in front of the palm's bones; a bar of iron 0.22 m long.
        private const float HammerLength = 0.38f;
        private const float GripShare = 0.25f;
        private const float PalmDepth = 0.03f;

        // Authoring default: at the blow the handle runs forward from the fist, its head this many degrees lower.
        private const float HandleTilt = 15f;

        /// <summary>The most an anvil's block is set into the ground to bring its face to the blow, m (authoring default).</summary>
        private const float MaxSink = 0.15f;
        private static readonly Vector3 BarSize = new Vector3(0.22f, 0.022f, 0.032f);

        /// <summary>The strike, as the clip plays it: when, and where the hammer's lowest point is, in the root's space over the soles.</summary>
        public struct Strike
        {
            public float Time;
            public Vector3 Point;
            public float Length;
        }

        public static void Run()
        {
            var report = new StringBuilder();
            SliceKitCatalogue.LoadRebuilding("Anvil");
            Arm(report);
            var strike = Measure(report);
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var lines = new List<string>();
            SlicePeopleModels.PlaceBlacksmith(lines);
            var set = Place(strike, lines);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            report.AppendLine(string.Join("\n", lines));
            if (set.HasValue)
            {
                Render(strike, set.Value, report);
            }

            Directory.CreateDirectory(Output);
            File.WriteAllText($"{Output}/blacksmith.txt", report.ToString());
            Debug.Log("[SliceBlacksmith]\n" + report);
        }

        // ------------------------------------------------------------------------------------------- the hammer in hand

        /// <summary>The blacksmith's standing prefab takes his hammer in his right hand (again, if it had one).</summary>
        public static void Arm(StringBuilder report)
        {
            var hammerPrefab = SliceKitCatalogue.LoadRebuilding(HammerKey)[HammerKey].Prefab;
            var path = SlicePeopleModels.Blacksmith.Standing;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var animator = root.GetComponentInChildren<Animator>();
                var hand = animator != null ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
                if (hand == null)
                {
                    report.AppendLine("Blacksmith: NO RIGHT HAND in his avatar; no hammer");
                    return;
                }

                foreach (var old in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == HammerName).ToList())
                {
                    Object.DestroyImmediate(old.gameObject);
                }

                // The hammer is set on the body as it stands at the blow, then the body goes back to its own pose and
                // the hammer stays in the hand: the clip's fist after the Humanoid retarget, not the bind pose's, holds it.
                var bones = animator.GetComponentsInChildren<Transform>(true).Select(t => (t, t.localPosition, t.localRotation, t.localScale)).ToList();
                var blow = LowestHand(animator, root.transform, SliceMixamo.Get("Hammer"));
                var hammer = (GameObject)PrefabUtility.InstantiatePrefab(hammerPrefab, hand);
                hammer.name = HammerName;
                report.AppendLine(Hold(hammer.transform, animator, root.transform, blow));
                foreach (var (t, position, rotation, scale) in bones)
                {
                    t.localPosition = position;
                    t.localRotation = rotation;
                    t.localScale = scale;
                }

                SliceKitCatalogue.SetLayer(hammer, KitLayers.Character);
                // Carried, not placed: its block is turned off, as dressing's is, so the kit check passes over it.
                foreach (var block in hammer.GetComponentsInChildren<KitBlock>(true))
                {
                    block.enabled = false;
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Poses the body through the Hammer clip and leaves it at the frame where the right wrist is lowest over the toes:
        /// the blow, read from the hand alone, before any hammer is held. Returns that frame's time.
        /// </summary>
        private static float LowestHand(Animator animator, Transform root, AnimationClip clip)
        {
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var frames = Mathf.RoundToInt(clip.length * clip.frameRate);
            float best = float.MaxValue, time = 0f;
            for (var f = 0; f <= frames; f++)
            {
                var t = f / clip.frameRate;
                Pose(animator, root, clip, t, 0f);
                var height = root.InverseTransformPoint(hand.position).y - Toes(animator, root);
                if (height < best)
                {
                    best = height;
                    time = t;
                }
            }

            Pose(animator, root, clip, time, 0f);
            return time;
        }

        /// <summary>
        /// Sets the hammer in the right fist as the body stands at the blow: the handle runs forward from the fist along the
        /// smith's facing, its head <see cref="HandleTilt"/> degrees lower, and the head's long side stands upright, so the
        /// face comes down on the iron. Its grip, a quarter of the way up the handle, sits in the fist.
        /// </summary>
        private static string Hold(Transform hammer, Animator animator, Transform root, float blow)
        {
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var index = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
            var little = animator.GetBoneTransform(HumanBodyBones.RightLittleProximal) ?? animator.GetBoneTransform(HumanBodyBones.RightRingProximal);
            var middle = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal) ?? index;
            var forearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            var forward = Vector3.ProjectOnPlane(root.forward, Vector3.up).normalized;
            var handle = Quaternion.AngleAxis(HandleTilt, Vector3.Cross(Vector3.up, forward)) * forward;
            Vector3 fingers, across;
            string how;
            if (index != null && little != null && middle != null)
            {
                fingers = (middle.position - hand.position).normalized;
                across = Vector3.ProjectOnPlane(index.position - little.position, fingers).normalized;
                how = "in the fist";
            }
            else
            {
                fingers = (hand.position - forearm.position).normalized;
                across = Vector3.ProjectOnPlane(handle, fingers).normalized;
                how = "by the forearm (no finger bones)";
            }

            var knuckles = Vector3.Angle(Vector3.Dot(across, handle) >= 0f ? across : -across, handle);
            var palm = Vector3.Cross(fingers, across).normalized;
            var grip = Vector3.Lerp(hand.position, middle != null ? middle.position : hand.position + (fingers * 0.08f), 0.6f) + (palm * PalmDepth);

            // The hammer's own lines, from its vertices in its root's space: the longest is the handle, the end that is
            // wider across is the head, and the head's longer side is the line of its face.
            var points = hammer.GetComponentsInChildren<MeshFilter>()
                .Where(f => f.sharedMesh != null)
                .SelectMany(f => f.sharedMesh.vertices.Select(v => hammer.InverseTransformPoint(f.transform.TransformPoint(v))))
                .ToList();
            var min = new Vector3(points.Min(p => p.x), points.Min(p => p.y), points.Min(p => p.z));
            var max = new Vector3(points.Max(p => p.x), points.Max(p => p.y), points.Max(p => p.z));
            var size = max - min;
            var a = size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;
            var length = size[a];
            var slab = length * 0.2f;
            var low = points.Where(p => p[a] <= min[a] + slab).ToList();
            var high = points.Where(p => p[a] >= max[a] - slab).ToList();
            var headHigh = Spread(high, a) >= Spread(low, a);
            var head = headHigh ? high : low;
            var butt = headHigh ? low : high;
            var axis = Vector3.zero;
            axis[a] = headHigh ? 1f : -1f;
            int b = (a + 1) % 3, c = (a + 2) % 3;
            var faceAxis = Vector3.zero;
            faceAxis[Range(head, b) >= Range(head, c) ? b : c] = 1f;
            var buttPoint = Mean(butt);
            buttPoint[a] = headHigh ? min[a] : max[a];

            var scale = length >= 0.3f && length <= 0.5f ? 1f : HammerLength / length;
            var face = Vector3.ProjectOnPlane(Vector3.down, handle).normalized;
            var turn = Quaternion.LookRotation(handle, face) * Quaternion.Inverse(Quaternion.LookRotation(axis, faceAxis));
            var held = buttPoint + (axis * (GripShare * length));
            hammer.rotation = turn;
            hammer.localScale = Vector3.one * (scale / hand.lossyScale.x);
            hammer.position = grip - (turn * (held * scale));
            var seen = hammer.GetComponentsInChildren<MeshFilter>().Where(f => f.sharedMesh != null)
                .SelectMany(f => f.sharedMesh.vertices.Select(v => f.transform.TransformPoint(v))).ToList();
            var span = seen.Max(p => Vector3.Distance(p, grip));
            return $"Blacksmith: his hammer is {length * scale:0.00} m (the pack's {length:0.00} m), held {how}, set on the blow at {blow:0.00} s: the handle runs forward " +
                   $"{HandleTilt:0}° down, {knuckles:0}° off the line of his knuckles; its farthest point is {span:0.00} m from the grip";
        }

        private static float Range(List<Vector3> points, int axis) => points.Count == 0 ? 0f : points.Max(p => p[axis]) - points.Min(p => p[axis]);

        private static float Spread(List<Vector3> points, int along) => Mathf.Max(Range(points, (along + 1) % 3), Range(points, (along + 2) % 3));

        private static Vector3 Mean(List<Vector3> points) => points.Aggregate(Vector3.zero, (s, p) => s + p) / Mathf.Max(1, points.Count);

        // ------------------------------------------------------------------------------------------- the strike

        /// <summary>The Hammer clip played on the armed blacksmith, frame by frame: the strike is the hammer's lowest point.</summary>
        public static Strike Measure(StringBuilder report)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var smith = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SlicePeopleModels.Blacksmith.Standing));
            var clip = SliceMixamo.Get("Hammer");
            var animator = smith.GetComponentInChildren<Animator>();
            var sole = Sole(animator, smith.transform);
            var strike = new Strike { Point = new Vector3(0f, float.MaxValue, 0f), Length = clip.length };
            var frames = Mathf.RoundToInt(clip.length * clip.frameRate);
            float highest = float.MinValue;
            for (var f = 0; f <= frames; f++)
            {
                var t = f / clip.frameRate;
                var lowest = Lowest(Pose(animator, smith.transform, clip, t, sole), smith.transform);
                highest = Mathf.Max(highest, lowest.y);
                if (lowest.y < strike.Point.y)
                {
                    strike.Point = lowest;
                    strike.Time = t;
                }
            }

            report.AppendLine($"Blacksmith: the Hammer clip is {clip.length:0.00} s; the hammer's lowest point comes down to {strike.Point.y:0.00} m over his soles " +
                              $"at {strike.Time:0.00} s, {strike.Point.z:0.00} m in front and {strike.Point.x:+0.00;-0.00} m to his left/right; at its highest it is {highest:0.00} m up");
            Object.DestroyImmediate(smith);
            return strike;
        }

        /// <summary>The toes' height over the lowest vertex of the skin, in the prefab's own pose: the sole under the toe bones.</summary>
        private static float Sole(Animator animator, Transform root)
        {
            var lowest = float.MaxValue;
            foreach (var r in animator.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh();
                r.BakeMesh(mesh, true);
                foreach (var v in mesh.vertices)
                {
                    lowest = Mathf.Min(lowest, root.InverseTransformPoint(r.transform.TransformPoint(v)).y);
                }

                Object.DestroyImmediate(mesh);
            }

            return Toes(animator, root) - lowest;
        }

        private static float Toes(Animator animator, Transform root)
        {
            float Up(HumanBodyBones bone)
            {
                var t = animator.GetBoneTransform(bone);
                return t != null ? root.InverseTransformPoint(t.position).y : float.MaxValue;
            }

            var toes = Mathf.Min(Up(HumanBodyBones.LeftToes), Up(HumanBodyBones.RightToes));
            return toes < float.MaxValue ? toes : Mathf.Min(Up(HumanBodyBones.LeftFoot), Up(HumanBodyBones.RightFoot));
        }

        /// <summary>Poses the body at <paramref name="time"/> of the clip and returns the ground's height under it, in the root's space.</summary>
        private static float Pose(Animator animator, Transform root, AnimationClip clip, float time, float sole)
        {
            var handler = new HumanPoseHandler(animator.avatar, animator.transform);
            var pose = SliceCharacterRigs.PoseAt(clip, time);
            handler.SetHumanPose(ref pose);
            handler.Dispose();
            return Toes(animator, root) - sole;
        }

        /// <summary>The hammer's lowest point, in the root's space, its height counted from <paramref name="ground"/>.</summary>
        private static Vector3 Lowest(float ground, Transform root)
        {
            var hammer = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == HammerName);
            var best = new Vector3(0f, float.MaxValue, 0f);
            if (hammer == null)
            {
                return best;
            }

            foreach (var f in hammer.GetComponentsInChildren<MeshFilter>().Where(f => f.sharedMesh != null))
            {
                foreach (var v in f.sharedMesh.vertices)
                {
                    var p = root.InverseTransformPoint(f.transform.TransformPoint(v));
                    if (p.y < best.y)
                    {
                        best = p;
                    }
                }
            }

            best.y -= ground;
            return best;
        }

        // ------------------------------------------------------------------------------------------- at the anvil

        /// <summary>The anvil and the blacksmith as they stand, for the close-up.</summary>
        public struct Set
        {
            public string AnvilPrefab;
            public Matrix4x4 AnvilFromSmith;
            public Vector3 AnvilModel; // the anvil's Model in its wrapper, its block set into the ground (Stump)
            public Vector3 Bar;
            public Quaternion BarTurn;
            public Matrix4x4? StumpFromSmith;
        }

        /// <summary>Moves the blacksmith so the strike lands on the anvil's face, and lays the hot bar there.</summary>
        private static Set? Place(Strike strike, List<string> lines)
        {
            var smith = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Blacksmith"
                && PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject) && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == SlicePeopleModels.Blacksmith.Standing);
            if (smith == null)
            {
                lines.Add("Blacksmith: NOT IN THE SCENE; not set at the anvil");
                return null;
            }

            var anvil = Object.FindObjectsByType<KitBlock>(FindObjectsSortMode.None).Where(b => b.name.StartsWith("Kit_Market_Anvil"))
                .OrderBy(b => (b.transform.position - smith.position).sqrMagnitude).FirstOrDefault();
            if (anvil == null)
            {
                lines.Add("Blacksmith: NO ANVIL; not set");
                return null;
            }

            // The anvil's face (Face), on the anvil's own mesh. The anvil first stands on a stump if its face is too low (Stump).
            var mesh = anvil.GetComponentsInChildren<MeshFilter>().Where(f => f.sharedMesh != null)
                .OrderByDescending(f => f.name == "anvil" ? 1 : 0).ThenByDescending(f => f.sharedMesh.vertexCount).First();
            Stump(anvil.transform, mesh, strike.Point.y - BarSize.y, lines);
            Physics.SyncTransforms();
            var centre = Face(mesh);
            var top = centre.y;

            var turn = smith.rotation;
            var reach = turn * new Vector3(strike.Point.x, 0f, strike.Point.z);
            var at = centre - reach;
            var ground = LayerMask.GetMask(KitLayers.Ground, KitLayers.Structure);
            if (Physics.Raycast(new Vector3(at.x, top + 2f, at.z), Vector3.down, out var hit, 6f, ground, QueryTriggerInteraction.Ignore))
            {
                at.y = hit.point.y;
            }
            else
            {
                at.y = smith.position.y;
            }

            var moved = Vector3.Distance(smith.position, at);
            Undo.RecordObject(smith, "Blacksmith at the anvil");
            smith.position = at;
            var faceHeight = top - at.y;
            lines.Add($"Blacksmith: set {moved:0.00} m from where he stood, so the blow lands on the anvil's face {faceHeight:0.00} m over the ground " +
                      $"(the hammer comes down to {strike.Point.y:0.00} m: {(strike.Point.y - faceHeight) * 100f:+0;-0} cm against the face)");

            // The hot bar on the face, along the blow's line, under the hammer.
            var barTurn = Quaternion.LookRotation(Vector3.ProjectOnPlane(turn * Vector3.right, Vector3.up).normalized, Vector3.up);
            var bar = Bar(smith.parent, centre + (Vector3.up * (BarSize.y * 0.5f)), barTurn);
            lines.Add($"Blacksmith: a hot bar of iron lies on the anvil at {bar.position}");

            var stump = smith.parent != null ? smith.parent.Find(StumpName) : null;
            stump = stump != null ? stump : anvil.transform.parent != null ? anvil.transform.parent.Find(StumpName) : null;
            return new Set
            {
                StumpFromSmith = stump != null ? smith.worldToLocalMatrix * stump.localToWorldMatrix : (Matrix4x4?)null,
                AnvilPrefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(anvil.gameObject),
                AnvilFromSmith = smith.worldToLocalMatrix * anvil.transform.localToWorldMatrix,
                AnvilModel = anvil.transform.Find("Model") is { } anvilModel ? anvilModel.localPosition : Vector3.zero,
                Bar = smith.InverseTransformPoint(bar.position),
                BarTurn = Quaternion.Inverse(smith.rotation) * bar.rotation,
            };
        }

        /// <summary>
        /// The anvil on a stump of oak, as a smith's anvil stands, so its face is at <paramref name="face"/> over the ground,
        /// where the blow comes down: the pack's anvil is 0.33 m high, and a real anvil's face stands about at the smith's
        /// knuckles. The stump is the project's own block, a plain cylinder of the market's dark wood with a box body; a
        /// later run lifts the anvil from the ground again, so it never climbs.
        /// </summary>
        private static void Stump(Transform anvil, MeshFilter mesh, float face, List<string> lines)
        {
            // A block set into the ground by an earlier run stands up again first, so a run never sinks it further.
            var model = anvil.Find("Model");
            if (model != null && PrefabUtility.IsPartOfPrefabInstance(model))
            {
                PrefabUtility.RevertObjectOverride(model, InteractionMode.AutomatedAction);
            }

            var old = anvil.parent != null ? anvil.parent.Find(StumpName) : null;
            var lifted = old != null ? old.localScale.y * 2f : 0f;
            var points = mesh.sharedMesh.vertices.Select(v => mesh.transform.TransformPoint(v)).ToList();
            var bottom = points.Min(p => p.y);
            var feet = points.Where(p => p.y <= bottom + 0.04f).ToList();
            var centre = Mean(feet);
            var ground = LayerMask.GetMask(KitLayers.Ground, KitLayers.Structure);
            var floor = Physics.Raycast(new Vector3(centre.x, points.Max(p => p.y) + 2f, centre.z), Vector3.down, out var hit, 8f, ground, QueryTriggerInteraction.Ignore)
                ? hit.point.y
                : bottom - lifted;
            var rest = Face(mesh).y - lifted - floor; // the face's height with the anvil on the ground
            // The kit's 0.1 m height step (KitLayoutCheck): the anvil stays on it, the face within 5 cm of the blow.
            var lift = Mathf.Max(0f, Mathf.Round((face - rest) / KitLayoutCheck.HeightStep) * KitLayoutCheck.HeightStep);
            Undo.RecordObject(anvil, "Anvil on its stump");
            anvil.position += Vector3.up * (lift - lifted);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            if (lift < 0.05f)
            {
                // An anvil on its own block whose face stands over the blow (the CC0 anvil, 2026-11-11): the block is set into
                // the ground, as a smith sets his, by at most MaxSink; only its Model moves, so the wrapper keeps the kit's step.
                var sink = Mathf.Clamp(rest - face, 0f, MaxSink);
                if (sink > 0.02f && model != null)
                {
                    Undo.RecordObject(model, "Anvil's block set into the ground");
                    model.position += Vector3.down * sink;
                    lines.Add($"Anvil: its block set {sink:0.00} m into the ground, so its face stands {rest - sink:0.00} m over it, where the blow comes down (standing on it, {rest:0.00} m); no stump");
                    return;
                }

                lines.Add($"Anvil: its face stands {rest:0.00} m high on the ground, where the blow comes down; no stump");
                return;
            }

            var radius = Mathf.Clamp((Mathf.Max(feet.Max(p => p.x) - feet.Min(p => p.x), feet.Max(p => p.z) - feet.Min(p => p.z)) * 0.5f) + 0.04f, 0.18f, 0.32f);
            var stump = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stump.name = StumpName;
            Object.DestroyImmediate(stump.GetComponent<Collider>());
            stump.AddComponent<BoxCollider>().size = new Vector3(1f, 2f, 1f);
            stump.transform.SetParent(anvil.parent, false);
            stump.transform.SetPositionAndRotation(new Vector3(centre.x, floor + (lift * 0.5f), centre.z), anvil.rotation);
            stump.transform.localScale = new Vector3(radius * 2f, lift * 0.5f, radius * 2f);
            stump.GetComponent<MeshRenderer>().sharedMaterial = SliceKitCatalogue.Colour("AnvilStump", new Color(0.30f, 0.21f, 0.13f), false);
            SliceKitCatalogue.SetLayer(stump, KitLayers.Prop);
            lines.Add($"Anvil: on an oak stump {lift:0.00} m high and {radius * 2f:0.00} m across, its face now {rest + lift:0.00} m over the ground (the pack's {rest:0.00} m)");
        }

        /// <summary>
        /// The anvil's face, in the world: the level band (1.5 cm deep) holding the most area of upward-facing triangles in
        /// the mesh's top fifth, at its area-weighted middle. Not the highest vertex: a horn or a hardy may stand over the
        /// face, as on the CC0 anvil on its block (SliceKitCc0), and the block's own top, lower down, is left out.
        /// </summary>
        private static Vector3 Face(MeshFilter mesh)
        {
            var m = mesh.sharedMesh;
            var points = m.vertices.Select(v => mesh.transform.TransformPoint(v)).ToArray();
            var normals = m.normals.Select(n => mesh.transform.TransformDirection(n)).ToArray();
            var tris = m.triangles;
            var low = points.Min(p => p.y);
            var high = points.Max(p => p.y);
            var flats = new List<(float Y, float Area, Vector3 Mid)>();
            for (var i = 0; i + 2 < tris.Length; i += 3)
            {
                var a = points[tris[i]];
                var b = points[tris[i + 1]];
                var c = points[tris[i + 2]];
                var up = normals.Length == points.Length ? (normals[tris[i]] + normals[tris[i + 1]] + normals[tris[i + 2]]).normalized.y : 1f;
                var area = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                var mid = (a + b + c) / 3f;
                if (up >= 0.9f && area > 1e-8f && mid.y >= high - ((high - low) * 0.2f))
                {
                    flats.Add((mid.y, area, mid));
                }
            }

            if (flats.Count == 0)
            {
                var top = points.Where(p => p.y >= high - 0.015f).ToList();
                var mean = Mean(top);
                mean.y = high;
                return mean;
            }

            var best = flats.OrderByDescending(f => flats.Where(g => Mathf.Abs(g.Y - f.Y) <= 0.015f).Sum(g => g.Area)).First();
            var band = flats.Where(g => Mathf.Abs(g.Y - best.Y) <= 0.015f).ToList();
            var total = band.Sum(g => g.Area);
            return band.Aggregate(Vector3.zero, (s, g) => s + (g.Mid * g.Area)) / total;
        }

        /// <summary>The hot iron: a bar of the project's own glowing material, seen and never solid (replaces an earlier one).</summary>
        private static Transform Bar(Transform parent, Vector3 at, Quaternion turn)
        {
            foreach (var old in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.name == WorkpieceName).ToList())
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = WorkpieceName;
            Object.DestroyImmediate(bar.GetComponent<Collider>());
            bar.transform.SetParent(parent, false);
            bar.transform.SetPositionAndRotation(at, turn);
            bar.transform.localScale = new Vector3(BarSize.z, BarSize.y, BarSize.x);
            bar.GetComponent<MeshRenderer>().sharedMaterial = HotIron();
            bar.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            SliceKitCatalogue.SetLayer(bar, KitLayers.NoGrid);
            return bar.transform;
        }

        /// <summary>Iron at a forging heat, orange-yellow: dark scale over a glow (authoring default).</summary>
        private static Material HotIron()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(WorkpieceMaterial);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, WorkpieceMaterial);
            }

            material.SetColor("_BaseColor", new Color(0.32f, 0.12f, 0.05f));
            material.SetFloat("_Smoothness", 0.35f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(1f, 0.38f, 0.08f) * 2.2f);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        // ------------------------------------------------------------------------------------------- the close-up

        /// <summary>The swing round the strike, from the side and the front, the anvil and the bar where they stand.</summary>
        private static void Render(Strike strike, Set set, StringBuilder report)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var light = new GameObject("Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);

            // The smith faces +X, so the camera on +Z sees the swing from his side.
            var smith = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SlicePeopleModels.Blacksmith.Standing));
            smith.transform.SetPositionAndRotation(Vector3.zero, Quaternion.LookRotation(Vector3.right));
            var anvil = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(set.AnvilPrefab));
            var world = smith.transform.localToWorldMatrix * set.AnvilFromSmith;
            anvil.transform.SetPositionAndRotation(world.GetColumn(3), world.rotation);
            if (anvil.transform.Find("Model") is { } anvilModel)
            {
                anvilModel.localPosition = set.AnvilModel;
            }

            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.transform.SetPositionAndRotation(smith.transform.TransformPoint(set.Bar), smith.transform.rotation * set.BarTurn);
            bar.transform.localScale = new Vector3(BarSize.z, BarSize.y, BarSize.x);
            bar.GetComponent<MeshRenderer>().sharedMaterial = HotIron();
            if (set.StumpFromSmith.HasValue)
            {
                var stump = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                var m = smith.transform.localToWorldMatrix * set.StumpFromSmith.Value;
                stump.transform.SetPositionAndRotation(m.GetColumn(3), m.rotation);
                stump.transform.localScale = m.lossyScale;
                stump.GetComponent<MeshRenderer>().sharedMaterial = SliceKitCatalogue.Colour("AnvilStump", new Color(0.30f, 0.21f, 0.13f), false);
            }

            var animator = smith.GetComponentInChildren<Animator>();
            var clip = SliceMixamo.Get("Hammer");
            var sole = Sole(animator, smith.transform);
            var times = new[] { -0.36f, -0.18f, -0.06f, 0f, 0.12f }.Select(d => Mathf.Repeat(strike.Time + d, strike.Length)).ToArray();
            const int tile = 384;
            var sheet = new Texture2D(tile * 2 * times.Length, tile, TextureFormat.RGB24, false);
            var column = 0;
            var model = animator.transform;
            var rest = model.localPosition;
            var warm = 0;
            SliceCharacterRigs.Draw(smith, new Texture2D(tile * 2, tile, TextureFormat.RGB24, false), 0, ref warm, tile, 1); // shaders compile on the first draw
            foreach (var t in times)
            {
                var ground = Pose(animator, smith.transform, clip, t, sole);
                model.localPosition = rest - new Vector3(0f, ground, 0f);
                if (Mathf.Approximately(t, strike.Time))
                {
                    var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                    report.AppendLine($"  at the strike: right hand at {hand.position}, its scale {hand.lossyScale}");
                    foreach (var r in smith.GetComponentsInChildren<MeshRenderer>().Concat(anvil.GetComponentsInChildren<MeshRenderer>()))
                    {
                        report.AppendLine($"  {r.transform.parent?.name}/{r.name}: centre {r.bounds.center}, size {r.bounds.size}");
                    }
                }

                SliceCharacterRigs.Draw(smith, sheet, 0, ref column, tile, 1);
                model.localPosition = rest;
            }

            sheet.Apply();
            File.WriteAllBytes($"{Output}/blacksmith.png", sheet.EncodeToPNG());
            report.AppendLine($"Blacksmith: the close-up at {string.Join(", ", times.Select(t => t.ToString("0.00")))} s written to {Output}/blacksmith.png");
        }
    }
}

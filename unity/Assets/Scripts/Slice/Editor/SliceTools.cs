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
    /// The player's tools in hand and the work's and the jump's clips measured (owner's play test, 2026-11-12: the player
    /// felled trees and mined ore with empty hands; the Reaction Check came after the axe had begun to swing back; the
    /// jump had no take-off).
    /// <list type="bullet">
    /// <item><see cref="Measure"/> plays each work clip on the player man frame by frame and writes where the right hand
    /// is and how fast it moves, so the key frame can stand just before the blow; and the jump's clips, the feet, the hips
    /// and the hands, so the take-off and the arms' rise can be read.</item>
    /// <item><see cref="Arm"/> puts the Stylized 3D Tools axe, pickaxe and hammer in both player bodies' right hands,
    /// switched off: <c>PlayerWork</c> shows the one its clip is played with while the work lasts. Each is set on the
    /// body as it stands at the blow: the handle through the fist, the head beyond the thumb, the blade or the face
    /// toward where the hand moves.</item>
    /// </list>
    /// Batch: -executeMethod GameDev.Slice.EditorTools.SliceTools.MeasureOnly, or .Run (measure, arm, render).
    /// </summary>
    public static class SliceTools
    {
        private const string Output = "Logs/SliceSurvey";
        private const string ToolFolder = "Assets/Stylized 3D Tools/Prefabs";

        /// <summary>
        /// Each tool: its child's name in the hand, its pack prefab, its length (m) and where along its handle the right
        /// hand holds it, from the butt (authoring defaults: a one-handed felling axe of 0.75 m held near its end, a
        /// pickaxe of 0.8 m, a smith's hand hammer of 0.38 m a quarter of the way up).
        /// </summary>
        public static readonly (string Name, string Prefab, float Length, float Grip, string Clip)[] Tools =
        {
            (PlayerWork.AxeTool, ToolFolder + "/axe01.prefab", 0.75f, 0.14f, "Chop"),
            (PlayerWork.PickaxeTool, ToolFolder + "/pickaxe01.prefab", 0.8f, 0.14f, "Mine"),
            (PlayerWork.HammerTool, ToolFolder + "/hammer01.prefab", 0.38f, 0.25f, "HammerDown"),
        };

        /// <summary>Authoring default, m: the handle's middle lies this far in front of the palm's bones.</summary>
        private const float PalmDepth = 0.03f;

        /// <summary>Each work clip's fastest hand, s into it, as the last <see cref="Measure"/> read it.</summary>
        private static readonly Dictionary<string, float> Peaks = new Dictionary<string, float>();

        public static void MeasureOnly()
        {
            var report = new StringBuilder();
            Measure(report);
            Write("tools_measure.txt", report);
        }

        public static void Run()
        {
            var report = new StringBuilder();
            Measure(report);
            foreach (var path in new[] { SliceTestScenes.PlayerPrefabPath, SlicePeopleModels.PlayerWomanPrefab })
            {
                Arm(path, report);
            }

            Render(report);
            Write("tools.txt", report);
        }

        /// <summary>
        /// Batch, the owner's play test of 2026-11-12 in one Unity start: the clips cut again (the jump's take-off) and the
        /// controllers built again (the walkers' gathering, the player's take-off), then the stylized man and the
        /// historical woman retired from the scenes. -executeMethod GameDev.Slice.EditorTools.SliceTools.PlayTestPass
        /// </summary>
        public static void PlayTestPass()
        {
            SlicePeopleModels.ClipsAndControllers();
            SlicePeopleModels.RetireOldLooks();
        }

        private static void Write(string file, StringBuilder report)
        {
            Directory.CreateDirectory(Output);
            File.WriteAllText($"{Output}/{file}", report.ToString());
            Debug.Log($"[SliceTools]\n{report}");
        }

        // ------------------------------------------------------------------------------------------- measuring

        /// <summary>The work clips' blows and the jump's clips, frame by frame, on the player man.</summary>
        public static void Measure(StringBuilder report)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SliceTestScenes.PlayerPrefabPath));
            var animator = player.GetComponentInChildren<Animator>();
            var root = player.transform;
            foreach (var use in new[] { "Chop", "Mine", "HammerDown" })
            {
                var blow = Blow(animator, root, SliceClips.Get(use));
                Peaks[use] = blow.PeakTime;
                report.AppendLine($"{use}: {blow.Table}");
                report.AppendLine($"{use}: the hand fastest {blow.Peak:0.00} m/s at {blow.PeakTime:0.00} s; lowest at {blow.LowTime:0.00} s; farthest ahead at {blow.FarTime:0.00} s; " +
                                  $"the hands {blow.Apart:0.00} m apart at the fastest; the key frame now {(PlayerWork.Keys.TryGetValue(use, out var k) ? k : -1f):0.00} s");
            }

            foreach (var use in new[] { "JumpUp", "Fall", "JumpLand" })
            {
                report.AppendLine(Jump(animator, root, SliceClips.Get(use), use));
            }

            Object.DestroyImmediate(player);
        }

        /// <summary>A work clip's blow read from the right hand: its speed, its lowest and its farthest ahead.</summary>
        private struct BlowReading
        {
            public float Peak;
            public float PeakTime;
            public float LowTime;
            public float FarTime;
            public float Apart;
            public string Table;
        }

        private static BlowReading Blow(Animator animator, Transform root, AnimationClip clip)
        {
            const float step = 1f / 60f;
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var left = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var reading = new BlowReading { LowTime = 0f };
            var table = new StringBuilder("the right hand (s: speed m/s, height m, ahead m):");
            float low = float.MaxValue, far = float.MinValue;
            Vector3? before = null;
            for (var t = 0f; t <= clip.length + 1e-4f; t += step)
            {
                Pose(animator, clip, t);
                var at = root.InverseTransformPoint(hand.position);
                var speed = before.HasValue ? (at - before.Value).magnitude / step : 0f;
                before = at;
                if (speed > reading.Peak)
                {
                    reading.Peak = speed;
                    reading.PeakTime = t;
                    reading.Apart = Vector3.Distance(hand.position, left.position);
                }

                if (at.y < low)
                {
                    low = at.y;
                    reading.LowTime = t;
                }

                // Quaternius' takes face back along the root in the editor's sampling (pipeline plan §13), so ahead is
                // read both ways: the hand's farthest from the hips over the ground.
                var hips = root.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.Hips).position);
                var reach = new Vector2(at.x - hips.x, at.z - hips.z).magnitude;
                if (reach > far)
                {
                    far = reach;
                    reading.FarTime = t;
                }

                if (Mathf.RoundToInt(t / step) % 2 == 0)
                {
                    table.Append($" {t:0.00}: {speed:0.0}, {at.y:0.00}, {reach:0.00};");
                }
            }

            reading.Table = table.ToString();
            return reading;
        }

        /// <summary>A jump clip frame by frame: the hips' and the lower toes' height in the clip's own space, and the hands against the shoulders.</summary>
        private static string Jump(Animator animator, Transform root, AnimationClip clip, string use)
        {
            const float step = 1f / 30f;
            float Up(HumanBodyBones bone) => root.InverseTransformPoint(animator.GetBoneTransform(bone).position).y;
            var line = new StringBuilder($"{use}: {clip.length:0.00} s; (s: hips, lower toes, hands over the shoulders, m):");
            for (var t = 0f; t <= clip.length + 1e-4f; t += step)
            {
                Pose(animator, clip, t);
                var toes = Mathf.Min(Up(HumanBodyBones.LeftToes), Up(HumanBodyBones.RightToes));
                var hands = ((Up(HumanBodyBones.LeftHand) + Up(HumanBodyBones.RightHand)) / 2f) - ((Up(HumanBodyBones.LeftUpperArm) + Up(HumanBodyBones.RightUpperArm)) / 2f);
                line.Append($" {t:0.00}: {Up(HumanBodyBones.Hips):0.00}, {toes:0.00}, {hands:+0.00;-0.00};");
            }

            return line.ToString();
        }

        private static void Pose(Animator animator, AnimationClip clip, float time)
        {
            var handler = new HumanPoseHandler(animator.avatar, animator.transform);
            var pose = SliceCharacterRigs.PoseAt(clip, time);
            handler.SetHumanPose(ref pose);
            handler.Dispose();
        }

        // ------------------------------------------------------------------------------------------- the tools in hand

        /// <summary>The player's prefab takes each tool in its right hand (again, if it had them), switched off.</summary>
        public static void Arm(string prefabPath, StringBuilder report)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var animator = root.GetComponentInChildren<Animator>();
                var hand = animator != null ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
                if (hand == null)
                {
                    report.AppendLine($"{prefabPath}: NO RIGHT HAND; no tools");
                    return;
                }

                foreach (var old in root.GetComponentsInChildren<Transform>(true).Where(t => Tools.Any(x => x.Name == t.name)).ToList())
                {
                    Object.DestroyImmediate(old.gameObject);
                }

                // Each tool is set on the body as it stands at its blow, then the body goes back to its own pose and the
                // tool stays in the hand.
                var bones = animator.GetComponentsInChildren<Transform>(true).Select(t => (t, t.localPosition, t.localRotation, t.localScale)).ToList();
                foreach (var (name, prefab, length, grip, use) in Tools)
                {
                    var clip = SliceClips.Get(use);
                    var blow = Blow(animator, root.transform, clip);
                    var source = AssetDatabase.LoadAssetAtPath<GameObject>(prefab);
                    if (source == null)
                    {
                        report.AppendLine($"{prefabPath}: NO {prefab}");
                        continue;
                    }

                    // The swing's way at the blow, from the hand a frame before to a frame after.
                    Pose(animator, clip, Mathf.Max(0f, blow.PeakTime - (1f / 60f)));
                    var from = hand.position;
                    Pose(animator, clip, Mathf.Min(clip.length, blow.PeakTime + (1f / 60f)));
                    var swing = hand.position - from;
                    Pose(animator, clip, blow.PeakTime);

                    var tool = (GameObject)PrefabUtility.InstantiatePrefab(source, hand);
                    PrefabUtility.UnpackPrefabInstance(tool, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    tool.name = name;
                    foreach (var part in tool.GetComponentsInChildren<Component>(true).Where(c => c is Collider || c is Rigidbody || c is MonoBehaviour).ToList())
                    {
                        Object.DestroyImmediate(part);
                    }

                    report.AppendLine($"{Path.GetFileNameWithoutExtension(prefabPath)} {name}: " + Hold(tool.transform, animator, swing, length, grip, blow));
                    SliceKitCatalogue.SetLayer(tool, KitLayers.Character);
                    tool.SetActive(false);
                    foreach (var (t, position, rotation, scale) in bones)
                    {
                        t.localPosition = position;
                        t.localRotation = rotation;
                        t.localScale = scale;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Sets a tool in the right fist as the body stands at the blow. The handle runs across the fist, from the little
        /// finger's side to the index finger's, the head beyond the thumb; where both hands hold it, as in a two-handed
        /// chop, it runs from the left hand through the right. Its blade or face turns toward where the hand moves, and its
        /// grip, <paramref name="grip"/> of the way up from the butt, sits in the fist.
        /// </summary>
        private static string Hold(Transform tool, Animator animator, Vector3 swing, float length, float grip, BlowReading blow)
        {
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var index = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
            var little = animator.GetBoneTransform(HumanBodyBones.RightLittleProximal) ?? animator.GetBoneTransform(HumanBodyBones.RightRingProximal);
            var middle = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal) ?? index;
            var forearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            var left = animator.GetBoneTransform(HumanBodyBones.LeftHand);
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
                across = Vector3.Cross(fingers, Vector3.up).normalized;
                how = "by the forearm (no finger bones)";
            }

            var palm = Vector3.Cross(fingers, across).normalized;
            var gripAt = Vector3.Lerp(hand.position, middle != null ? middle.position : hand.position + (fingers * 0.08f), 0.6f) + (palm * PalmDepth);
            var twoHands = blow.Apart < 0.35f;
            // With both hands on it the left holds the butt and the right the handle above it, the hands apart along it.
            grip = twoHands ? Mathf.Clamp((blow.Apart + 0.06f) / length, grip, 0.7f) : grip;
            var handle = twoHands ? (hand.position - left.position).normalized : across;
            if (Vector3.Dot(handle, across) < 0f && !twoHands)
            {
                handle = -handle;
            }

            var blade = Vector3.ProjectOnPlane(swing.sqrMagnitude > 1e-6f ? swing : fingers, handle).normalized;

            // The tool's own lines, from its vertices in its root's space: the longest is the handle, the end that is wider
            // across is the head, and the head's farthest side from the handle's line is the blade, or for a symmetric
            // head (a hammer, a pick) its longer side is the line of its face.
            var points = tool.GetComponentsInChildren<MeshFilter>().Where(f => f.sharedMesh != null)
                .SelectMany(f => f.sharedMesh.vertices.Select(v => tool.InverseTransformPoint(f.transform.TransformPoint(v)))).ToList();
            var min = new Vector3(points.Min(p => p.x), points.Min(p => p.y), points.Min(p => p.z));
            var max = new Vector3(points.Max(p => p.x), points.Max(p => p.y), points.Max(p => p.z));
            var size = max - min;
            var a = size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;
            var own = size[a];
            var slab = own * 0.25f;
            var low = points.Where(p => p[a] <= min[a] + slab).ToList();
            var high = points.Where(p => p[a] >= max[a] - slab).ToList();
            var headHigh = Spread(high, a) >= Spread(low, a);
            var head = headHigh ? high : low;
            var butt = headHigh ? low : high;
            var axis = Vector3.zero;
            axis[a] = headHigh ? 1f : -1f;
            var line = Mean(butt);
            int b = (a + 1) % 3, c = (a + 2) % 3;
            var wide = Range(head, b) >= Range(head, c) ? b : c;
            var faceAxis = Vector3.zero;
            var outward = head.Max(p => p[wide] - line[wide]);
            var inward = line[wide] - head.Min(p => p[wide]);
            faceAxis[wide] = outward >= inward ? 1f : -1f;
            var buttPoint = line;
            buttPoint[a] = headHigh ? min[a] : max[a];

            var scale = length / own;
            var turn = Quaternion.LookRotation(handle, blade) * Quaternion.Inverse(Quaternion.LookRotation(axis, faceAxis));
            var held = buttPoint + (axis * (grip * own));
            tool.rotation = turn;
            tool.localScale = Vector3.one * (scale / hand.lossyScale.x);
            tool.position = gripAt - (turn * (held * scale));
            return $"{length:0.00} m (the pack's {own:0.00} m), held {how}{(twoHands ? " with both hands" : string.Empty)} at {grip:0.00} of its handle, set on the blow at {blow.PeakTime:0.00} s; " +
                   $"its head {Vector3.Angle(handle, Vector3.up):0}° from up, its blade {Vector3.Angle(blade, Vector3.down):0}° from down; " +
                   $"{(outward >= inward ? outward : inward) * scale:0.00} m of head beyond the handle's line";
        }

        private static float Range(List<Vector3> points, int axis) => points.Count == 0 ? 0f : points.Max(p => p[axis]) - points.Min(p => p[axis]);

        private static float Spread(List<Vector3> points, int along) => Mathf.Max(Range(points, (along + 1) % 3), Range(points, (along + 2) % 3));

        private static Vector3 Mean(List<Vector3> points) => points.Aggregate(Vector3.zero, (s, p) => s + p) / Mathf.Max(1, points.Count);

        // ------------------------------------------------------------------------------------------- the check sheet

        /// <summary>The player man at each tool's blow and a little before it, from the front and the side, tool in hand.</summary>
        private static void Render(StringBuilder report)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var light = new GameObject("Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SliceTestScenes.PlayerPrefabPath));
            var animator = player.GetComponentInChildren<Animator>();
            const int tile = 320;
            var sheet = new Texture2D(tile * 2 * 3, tile * Tools.Length, TextureFormat.RGB24, false);
            var warm = 0;
            SliceCharacterRigs.Draw(player, new Texture2D(tile * 2, tile, TextureFormat.RGB24, false), 0, ref warm, tile, 1);
            for (var row = 0; row < Tools.Length; row++)
            {
                var (name, _, _, _, use) = Tools[row];
                var tool = player.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
                if (tool != null)
                {
                    tool.gameObject.SetActive(true);
                }

                var clip = SliceClips.Get(use);
                var key = Peaks.TryGetValue(use, out var peak) ? peak : PlayerWork.Keys[use];
                var column = 0;
                foreach (var t in new[] { Mathf.Max(0f, key - 0.25f), key, Mathf.Min(clip.length, key + 0.1f) })
                {
                    Pose(animator, clip, t);
                    SliceCharacterRigs.Draw(player, sheet, row, ref column, tile, Tools.Length);
                }

                if (tool != null)
                {
                    tool.gameObject.SetActive(false);
                }
            }

            sheet.Apply();
            File.WriteAllBytes($"{Output}/tools.png", sheet.EncodeToPNG());
            report.AppendLine($"The sheet: each tool's row at its fastest hand less 0.25 s, at it, and 0.1 s after, written to {Output}/tools.png");
        }
    }
}

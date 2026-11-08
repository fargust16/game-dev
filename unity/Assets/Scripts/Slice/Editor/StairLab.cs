using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The stair lab (owner, after VI-8's follow-up): how the pack's stair, balustrade and newel post join. First, the
    /// pack's own demo scene (`Victorian Interiors/Test Scene.unity`), where the author assembled them: every piece's
    /// transform and bounds and pictures of each joint, read only, never saved. Pictures go to Logs/SliceSurvey.
    /// Batch: -executeMethod GameDev.Slice.EditorTools.StairLab.ProbeVendor
    /// </summary>
    internal static class StairLab
    {
        private const string VendorScene = "Assets/Victorian Interiors/Test Scene.unity";
        private static readonly string[] Pieces = { "Banister", "Banister_End", "Stair" };

        [MenuItem("GameDev/Slice/Stair Lab/Probe the Pack's Demo Stair")]
        public static void ProbeVendor()
        {
            EditorSceneManager.OpenScene(VendorScene, OpenSceneMode.Single);
            var log = new StringBuilder("[StairLab] the pack's demo stair:\n");
            var found = new List<(string Kind, Transform T, Bounds B)>();
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude))
            {
                var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go);
                if (source == null || !PrefabUtility.IsAnyPrefabInstanceRoot(go) || !Pieces.Contains(source.name))
                {
                    continue;
                }

                var renderers = go.GetComponentsInChildren<MeshRenderer>();
                var b = renderers[0].bounds;
                foreach (var r in renderers)
                {
                    b.Encapsulate(r.bounds);
                }

                found.Add((source.name, go.transform, b));
            }

            foreach (var (kind, t, b) in found.OrderBy(f => f.Kind).ThenBy(f => f.T.position.x).ThenBy(f => f.T.position.z))
            {
                log.AppendLine($"  {kind,-13} '{t.name}' pos {t.position:F3} rot {t.eulerAngles:F1} scale {t.lossyScale:F3} parent '{(t.parent ? t.parent.name : "-")}' bounds {b.min:F3}..{b.max:F3}");
            }

            // What the balustrades stand on: every other renderer touching a balustrade's bounds, grown 5 cm.
            var near = new HashSet<string>();
            foreach (var (kind, t, b) in found.Where(f => f.Kind == "Banister"))
            {
                var grown = b;
                grown.Expand(0.1f);
                foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
                {
                    if (!r.transform.IsChildOf(t) && r.bounds.Intersects(grown) && !found.Any(f => r.transform.IsChildOf(f.T)))
                    {
                        near.Add($"  beside '{t.name}': '{r.name}' ({r.GetComponent<MeshFilter>()?.sharedMesh?.name}) pos {r.transform.position:F3} rot {r.transform.eulerAngles:F1} bounds {r.bounds.min:F3}..{r.bounds.max:F3}");
                    }
                }
            }

            log.AppendLine(string.Join("\n", near));
            Debug.Log(log.ToString());

            // The demo's light is baked and dark in a batch render: an even light for the pictures, never saved.
            Lit();

            // Pictures of the joints: the corner newel (5) where the long rail meets the end rail, the free end at the
            // stair's head (Banister_End), the end at the wall (6), and the long rail's seat on the well's edge.
            Shot("lab_vendor_corner_floor", new Vector3(-0.4f, 4.6f, 10.2f), new Vector3(-1.38f, 4.0f, 9.18f));
            Shot("lab_vendor_corner_well", new Vector3(-2.6f, 4.4f, 7.6f), new Vector3(-1.38f, 3.9f, 9.18f));
            Shot("lab_vendor_corner_below", new Vector3(-2.4f, 2.2f, 7.8f), new Vector3(-1.45f, 3.4f, 9.1f));
            Shot("lab_vendor_head_end", new Vector3(-0.4f, 4.4f, 2.2f), new Vector3(-1.41f, 4.0f, 3.0f));
            Shot("lab_vendor_wall_end", new Vector3(-3.4f, 4.5f, 10.4f), new Vector3(-4.35f, 4.0f, 9.1f));
            Shot("lab_vendor_seat", new Vector3(-0.6f, 3.9f, 6.0f), new Vector3(-1.5f, 3.4f, 6.0f));
            Shot("lab_vendor_seat_below", new Vector3(-3.0f, 2.4f, 6.0f), new Vector3(-1.5f, 3.4f, 6.0f));
            Shot("lab_vendor_overview", new Vector3(1.5f, 6.2f, 12.5f), new Vector3(-2.9f, 3.5f, 6.0f));
        }

        /// <summary>
        /// The lab scene: a rich villa of three floors, two stair wells, built by the game's own assembler and saved as
        /// `Assets/Scenes/Slice/Lab/StairLab.unity` in an even light, to be opened and walked round in the editor; then
        /// every joint of its balustrades rendered close (Joints).
        /// Batch: -executeMethod GameDev.Slice.EditorTools.StairLab.Build
        /// </summary>
        [MenuItem("GameDev/Slice/Stair Lab/Build and Render the Lab")]
        public static void Build()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Lit();
            var key = VeilmouthDwellings.PreviewKey(Dwellings.DwellingArchetype.Villa, Dwellings.DwellingWealth.Rich, 1000, -2000);
            var faults = VeilmouthDwellings.BuildAndCheck(key, VeilmouthDwellings.Kit, VeilmouthDwellings.Settings, out var root);
            foreach (var f in faults)
            {
                Debug.LogWarning($"[StairLab] {f}");
            }

            System.IO.Directory.CreateDirectory("Assets/Scenes/Slice/Lab");
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), LabScene);
            Joints(root, "lab_villa");
            Debug.Log($"[StairLab] the lab villa built and saved, {faults.Count} kit faults.");
        }

        /// <summary>
        /// The Soot & Bell's balustrades, every joint close, as Build renders the lab's.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.StairLab.RenderSootAndBell
        /// </summary>
        [MenuItem("GameDev/Slice/Stair Lab/Render the Soot and Bell's Rails")]
        public static void RenderSootAndBell()
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.SootAndBellScenePath, OpenSceneMode.Single);
            Lit();
            Joints(null, "lab_sb", scene.GetRootGameObjects());
        }

        public const string LabScene = "Assets/Scenes/Slice/Lab/StairLab.unity";

        /// <summary>
        /// Each newel post from the floor, from over the well and from the storey below; each balustrade's seat on the
        /// well's edge from the floor and from below. Positions come from the posts and the rails themselves.
        /// </summary>
        private static void Joints(Transform root, string prefix, GameObject[] roots = null)
        {
            var all = roots ?? new[] { root.gameObject };
            var posts = all.SelectMany(r => r.GetComponentsInChildren<Transform>()).Where(t => t.name == "Newel").ToList();
            var rails = all.SelectMany(r => r.GetComponentsInChildren<Transform>()).Where(t => t.name == "Balustrade").ToList();
            var log = new StringBuilder($"[StairLab] {prefix}: {posts.Count} posts, {rails.Count} balustrades\n");
            var i = 0;
            foreach (var p in posts)
            {
                // The post stands off its pivot toward its local -X and -Z: that way is into the floor.
                var floor = -(p.rotation * new Vector3(1f, 0f, 1f)).normalized;
                var c = p.position + (p.rotation * new Vector3(-DwellingRailLine, 0.9f, -DwellingRailLine));
                log.AppendLine($"  post {i} pivot {p.position:F3} turn {p.eulerAngles.y:F0} centre {c - (Vector3.up * 0.9f):F3}");
                Shot($"{prefix}_post{i}_floor", c + (floor * 1.3f) + (Vector3.up * 0.5f), c);
                Shot($"{prefix}_post{i}_well", c - (floor * 1.5f) + (Vector3.up * 0.4f), c - (Vector3.up * 0.3f));
                Shot($"{prefix}_post{i}_below", c - (floor * 1.4f) - (Vector3.up * 2.4f), c - (Vector3.up * 1.0f));
                i++;
            }

            i = 0;
            foreach (var r in rails)
            {
                var m = r.TransformPoint(1.5f, 0f, 1f);
                var well = r.forward;
                log.AppendLine($"  rail {i} edge {r.TransformPoint(0f, 0f, 1f):F3}..{r.TransformPoint(3f, 0f, 1f):F3} toward the well {well:F0}");
                Shot($"{prefix}_rail{i}_seat", m - (well * 1.0f) + (Vector3.up * 0.45f) + (r.right * 0.6f), m + (Vector3.up * 0.1f));
                Shot($"{prefix}_rail{i}_below", m + (well * 1.1f) - (Vector3.up * 1.4f) + (r.right * 0.6f), m - (Vector3.up * 0.15f));
                i++;
            }

            // Each stair: its rails' meshes measured, its head and foot from both sides.
            i = 0;
            foreach (var s in all.SelectMany(r => r.GetComponentsInChildren<Transform>()).Where(t => t.name == "Stair"))
            {
                foreach (var mr in s.GetComponentsInChildren<MeshRenderer>())
                {
                    var mesh = mr.GetComponent<MeshFilter>()?.sharedMesh;
                    log.AppendLine($"  stair {i} part '{mr.name}' ({mesh?.name}) bounds {mr.bounds.min:F3}..{mr.bounds.max:F3}");
                }

                // Each rail post from outside the stair, a little ahead of it, and from the stair's middle.
                var middle = s.GetComponentsInChildren<MeshRenderer>().First(r => r.name == "Base").bounds.center;
                var k = 0;
                foreach (var post in s.GetComponentsInChildren<Transform>().Where(t => t.name == "Rail post"))
                {
                    var c = post.position + (Vector3.up * 0.8f);
                    var away = Vector3.ProjectOnPlane(post.position - middle, Vector3.up).normalized;
                    log.AppendLine($"  stair {i} rail post {k} at {post.position:F3}");
                    Shot($"{prefix}_stair{i}_post{k}_out", c + (away * 1.4f) + (Vector3.up * 0.5f), c);
                    Shot($"{prefix}_stair{i}_post{k}_in", c - (away * 1.2f) + (Vector3.up * 0.6f), c);
                    k++;
                }

                i++;
            }

            Debug.Log(log.ToString());
        }

        private const float DwellingRailLine = Dwellings.DwellingAssembler.RailLine;

        private static void Shot(string name, Vector3 from, Vector3 to) => VeilmouthRenders.Eye(name, from, to, 55f);

        private static void Lit()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.8f, 0.78f, 0.75f);
            var sun = new GameObject("Lab light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 0.6f;
            sun.shadows = LightShadows.None;
            sun.transform.rotation = Quaternion.Euler(55f, 140f, 0f);
        }
    }
}

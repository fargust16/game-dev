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
            Views(root);
            Debug.Log($"[StairLab] the lab villa built and saved, {faults.Count} kit faults.");
        }

        /// <summary>
        /// The wells as a player sees them, at eye height (1.6 m) on each upper floor: over the gallery's rails into the
        /// well, along the side where the next flight climbs, and from above (owner, VI-8 third remarks). The lab villa's
        /// wells: the first over x 9–15, z 3–6 on the first floor (3.5 m), the second over x 9–15, z 6–9 on the second (7 m).
        /// </summary>
        private static void Views(Transform root)
        {
            Vector3 P(float x, float y, float z) => root.TransformPoint(x, y, z);
            Shot("lab_view_well1_over", P(12f, 5.1f, 1.4f), P(12f, 3.2f, 4.5f));
            Shot("lab_view_well1_flight", P(7.6f, 5.1f, 7.2f), P(12.5f, 3.3f, 5.6f));
            Shot("lab_view_well1_flight_east", P(16.6f, 5.1f, 7.4f), P(12f, 3.3f, 5.6f));
            Shot("lab_view_well1_top", P(12f, 6.6f, 1.6f), P(12f, 3.4f, 4.6f));
            Shot("lab_view_well1_edge", P(12.4f, 4.3f, 2.3f), P(12.4f, 3.3f, 3.4f));
            Shot("lab_view_well2_over", P(12f, 8.6f, 10.6f), P(12f, 6.7f, 7.5f));
            Shot("lab_view_well2_edge", P(12.4f, 7.8f, 9.7f), P(12.4f, 6.8f, 8.6f));
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

        /// <summary>
        /// The Soot & Bell's yard in the city, read only: the house, the yard's walls and props, and pictures of the
        /// house's side on the yard, to place the back door (owner, VI-8 second follow-up).
        /// Batch: -executeMethod GameDev.Slice.EditorTools.StairLab.ProbeBellYard
        /// </summary>
        public static void ProbeBellYard()
        {
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
            var log = new StringBuilder("[StairLab] the Soot & Bell's yard:\n");
            var yard = new Bounds(new Vector3(197.5f, 0f, -262.5f), new Vector3(12f, 400f, 22f));
            foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude)
                         .Where(t => (t.parent == null || t.parent.parent == null || t.GetComponent<GameDev.Slice.Kit.KitBlock>() != null || t.GetComponent<GameDev.World.Portals.Portal>() != null)
                                     && yard.Contains(t.position)))
            {
                log.AppendLine($"  '{go.name}' pos {go.position:F2} rot {go.eulerAngles.y:F0} parent '{(go.parent ? go.parent.name : "-")}'");
            }

            var bell = GameObject.Find("The Soot & Bell");
            if (bell != null)
            {
                var b = bell.GetComponentInChildren<Renderer>().bounds;
                log.AppendLine($"  house pivot {bell.transform.position:F2} rot {bell.transform.eulerAngles.y:F0} bounds {b.min:F2}..{b.max:F2}");
            }

            float Ground(float x, float z) =>
                Physics.Raycast(new Vector3(x, 300f, z), Vector3.down, out var hit, 400f, LayerMask.GetMask(GameDev.Slice.Kit.KitLayers.Ground)) ? hit.point.y : 0f;
            var g = Ground(197.5f, -262.5f);
            log.AppendLine($"  ground at the yard's middle {g:F2}");
            Debug.Log(log.ToString());
            Lit();
            Shot("lab_yard_side", new Vector3(199.5f, g + 1.7f, -268.5f), new Vector3(195f, g + 1.5f, -258f));
            Shot("lab_yard_north", new Vector3(198f, g + 1.7f, -269.5f), new Vector3(197f, g + 1.2f, -255f));
            Shot("lab_yard_above", new Vector3(206f, g + 14f, -272f), new Vector3(196f, g, -261f));
            Shot("lab_yard_door", new Vector3(199.5f, g + 1.7f, -261.5f), new Vector3(195f, g + 1.3f, -261.5f));

            // The yard door from the stair hall inside.
            EditorSceneManager.OpenScene(VeilmouthScene.SootAndBellScenePath, OpenSceneMode.Single);
            Lit();
            Shot("lab_yard_door_inside", new Vector3(16.5f, 1.7f, 22.5f), new Vector3(21f, 1.3f, 22.5f));
            Shot("lab_yard_door_close", new Vector3(19.4f, 1.6f, 21.6f), new Vector3(21f, 1.4f, 23.2f));
            Shot("lab_street_door_inside", new Vector3(10.5f, 1.7f, 4.5f), new Vector3(10.5f, 1.3f, 0f));
            Shot("lab_street_door_foot", new Vector3(10.1f, 0.35f, 1.1f), new Vector3(10.5f, 0.05f, 0f));
            Shot("lab_street_door_head", new Vector3(9.6f, 1.9f, 0.9f), new Vector3(10.2f, 2.15f, 0f));
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude).Where(t => t.name is "Yard door" or "Street door"))
            {
                var b = t.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, c) => { a.Encapsulate(c); return a; });
                Debug.Log($"[StairLab] '{t.name}' pos {t.position:F3} rot {t.eulerAngles.y:F0} bounds {b.min:F3}..{b.max:F3}; doorway '{t.parent.name}' pos {t.parent.position:F3} rot {t.parent.eulerAngles.y:F0}");
            }
        }

        /// <summary>
        /// How the pack's demo scene seats a door leaf in its doorway (owner, after the yard door: a strip of sky shows
        /// round our shut leaves): every Door_1 / Door_2 and every doorway wall (Wall_2, Wall_8), with their transforms
        /// and bounds, and pictures of each door from both sides. Read only, never saved.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.StairLab.ProbeVendorDoor
        /// </summary>
        public static void ProbeVendorDoor()
        {
            EditorSceneManager.OpenScene(VendorScene, OpenSceneMode.Single);
            var log = new StringBuilder("[StairLab] the pack's demo doors:\n");
            var kinds = new[] { "Door_1", "Door_2", "Wall_2", "Wall_8" };
            var doors = new List<Transform>();
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude))
            {
                var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go);
                if (source == null || !PrefabUtility.IsAnyPrefabInstanceRoot(go) || !kinds.Contains(source.name))
                {
                    continue;
                }

                var b = go.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, c) => { a.Encapsulate(c); return a; });
                log.AppendLine($"  {source.name,-7} '{go.name}' pos {go.transform.position:F3} rot {go.transform.eulerAngles:F1} scale {go.transform.lossyScale:F3} bounds {b.min:F3}..{b.max:F3}");
                if (source.name.StartsWith("Door"))
                {
                    doors.Add(go.transform);
                }
            }

            // Each demo door against the doorway wall it stands in, in the wall's frame (its pivot is the opening's middle).
            var walls = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude)
                .Where(g => PrefabUtility.IsAnyPrefabInstanceRoot(g) && PrefabUtility.GetCorrespondingObjectFromOriginalSource(g)?.name is "Wall_2" or "Wall_8")
                .Select(g => g.transform).ToList();
            foreach (var d in doors)
            {
                var wall = walls.OrderBy(w => (w.position - d.position).sqrMagnitude).First();
                log.AppendLine($"  '{d.name}' in '{wall.name}': leaf pivot in the wall's frame {wall.InverseTransformPoint(d.position):F3}, turn {Mathf.DeltaAngle(wall.eulerAngles.y, d.eulerAngles.y):F0}");
                Measure(wall, d, 0f, log);
            }

            Debug.Log(log.ToString());
            Lit();
            var i = 0;
            foreach (var d in doors)
            {
                var b = d.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, c) => { a.Encapsulate(c); return a; });
                var thin = b.size.x < b.size.z ? Vector3.right : Vector3.forward;
                var c0 = b.center;
                Shot($"lab_vendor_door{i}_a", c0 + (thin * 2.2f) + (Vector3.up * 0.3f), c0);
                Shot($"lab_vendor_door{i}_b", c0 - (thin * 2.2f) + (Vector3.up * 0.3f), c0);
                Shot($"lab_vendor_door{i}_top", c0 + (thin * 0.9f) + (Vector3.up * 0.4f), c0 + (Vector3.up * 1.0f));
                i++;
            }
        }

        /// <summary>
        /// Our doorways measured by rays against their own meshes: for each shut leaf in the Soot & Bell, its doorway's
        /// jambs and head across the wall's depth, and the leaf's own extent, in the doorway's frame. Read only.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.StairLab.MeasureDoorways
        /// </summary>
        public static void MeasureDoorways()
        {
            EditorSceneManager.OpenScene(VeilmouthScene.SootAndBellScenePath, OpenSceneMode.Single);
            var log = new StringBuilder("[StairLab] doorways measured:\n");
            foreach (var leaf in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude).Where(t => t.name is "Street door" or "Yard door"))
            {
                Measure(leaf.parent, leaf, 1.5f, log);
            }

            Debug.Log(log.ToString());
        }

        /// <summary>
        /// A doorway's jambs and head by rays from its opening's middle (local x <paramref name="middle"/>, 1 m up) across
        /// the wall's depth, and the leaf's and the wall's extents, all in the doorway's frame.
        /// </summary>
        private static void Measure(Transform doorway, Transform leaf, float middle, StringBuilder log)
        {
            {
                var added = new List<MeshCollider>();
                foreach (var f in doorway.GetComponentsInChildren<MeshFilter>().Where(f => !f.transform.IsChildOf(leaf)))
                {
                    var c = f.gameObject.AddComponent<MeshCollider>();
                    c.sharedMesh = f.sharedMesh;
                    added.Add(c);
                }

                Physics.SyncTransforms();
                float? Ray(Vector3 localFrom, Vector3 localDir)
                {
                    var from = doorway.TransformPoint(localFrom);
                    var dir = doorway.TransformDirection(localDir);
                    var hits = Physics.RaycastAll(from, dir, 3f, ~0, QueryTriggerInteraction.Ignore).Where(h => added.Contains(h.collider as MeshCollider)).ToList();
                    return hits.Count == 0 ? null : hits.Min(h => h.distance);
                }

                log.AppendLine($"  '{leaf.name}' in '{doorway.name}' at {doorway.position:F2} turn {doorway.eulerAngles.y:F0}");
                foreach (var z in new[] { -0.12f, -0.08f, -0.04f, 0f, 0.04f, 0.08f, 0.12f })
                {
                    var left = Ray(new Vector3(middle, 1.0f, z), Vector3.left);
                    var right = Ray(new Vector3(middle, 1.0f, z), Vector3.right);
                    var head = Ray(new Vector3(middle, 1.0f, z), Vector3.up);
                    log.AppendLine($"    z {z,5:F2}: jambs x {(left.HasValue ? middle - left.Value : float.NaN):F3}..{(right.HasValue ? middle + right.Value : float.NaN):F3}, head y {(head.HasValue ? 1.0f + head.Value : float.NaN):F3}");
                }

                var lb = leaf.GetComponentsInChildren<MeshFilter>().SelectMany(f => f.sharedMesh.vertices.Select(v => doorway.InverseTransformPoint(f.transform.TransformPoint(v)))).ToList();
                log.AppendLine($"    leaf x {lb.Min(v => v.x):F3}..{lb.Max(v => v.x):F3} y {lb.Min(v => v.y):F3}..{lb.Max(v => v.y):F3} z {lb.Min(v => v.z):F3}..{lb.Max(v => v.z):F3}");
                var wb = doorway.GetComponentsInChildren<MeshFilter>().Where(f => !f.transform.IsChildOf(leaf))
                    .SelectMany(f => f.sharedMesh.vertices.Select(v => doorway.InverseTransformPoint(f.transform.TransformPoint(v)))).ToList();
                log.AppendLine($"    wall x {wb.Min(v => v.x):F3}..{wb.Max(v => v.x):F3} z {wb.Min(v => v.z):F3}..{wb.Max(v => v.z):F3}");
                foreach (var c in added)
                {
                    Object.DestroyImmediate(c);
                }
            }
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

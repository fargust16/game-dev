using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Pictures of the dressed Veilmouth and its interiors for checking a dressing pass: views from above and from a
    /// walker's height, written to Logs/SliceSurvey outside Assets. The ritual circles are shown for the picture only,
    /// and nothing is saved. Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderAll
    /// </summary>
    public static class VeilmouthRenders
    {
        private const string OutputFolder = "Logs/SliceSurvey";

        [MenuItem("GameDev/Slice/Render Veilmouth Views")]
        public static void RenderAll()
        {
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            ShowCircles();

            // The wind controller runs only in an editor frame; a batch render sets its globals itself.
            Shader.SetGlobalVector("_Growth_WindSettings", new Vector4(0.17f, 0.1f, 5f, 1f));
            Shader.SetGlobalTexture("_Growth_WindNoise", AssetDatabase.LoadAssetAtPath<Texture2D>(VeilmouthDressing.WindNoisePath));
            // C2 views; an eye stands 1.7 m above the ground under it.
            Top("view_world", new Vector3(0f, 0f, 0f), 500f);
            Top("view_city", new Vector3(130f, 0f, -228f), 160f);
            Top("view_lowerveil", new Vector3(200f, 0f, -287f), 60f);
            Top("view_government", new Vector3(110f, 0f, -170f), 60f);
            Top("view_market", new Vector3(50f, 0f, -287f), 60f);
            Top("view_hill", new Vector3(420f, 0f, 140f), 180f);
            Eye("eye_bellstreet", 195f, -275f, 100f, -275f, 2.5f);
            Eye("eye_highstreet", -10f, -235f, 100f, -235f, 3f);
            Eye("eye_avenue", 110f, -218f, 110f, -160f, 4f);
            Eye("eye_ridgestreet", 0f, -168f, 120f, -168f, 3f);
            Eye("eye_sootstair", 204f, -178f, 204f, -215f, 0f);
            Eye("eye_bridge", 300f, -235f, 200f, -235f, 4f);
            Eye("eye_quay", 100f, -318f, 230f, -330f, 0f);
            Eye("eye_westgate", -80f, -235f, 20f, -220f, 4f);
            Eye("eye_forest", 123f, 120f, 123f, 300f, 2f);
            Eye("eye_hill", 340f, 0f, 420f, 140f, 20f);
            // The owner's remarks of 2026-11-06: the terraces' gardens, a corner house, a villa's garden, the North Gate.
            Eye("eye_terraces", 22f, -184f, 22f, -215f, 2f);
            Eye("eye_corner", 30f, -237f, 2f, -213f, 3f);
            Eye("eye_villa", 40f, -168f, 18f, -147f, 3f);
            Eye("eye_northgate", 247f, -140f, 247f, -118f, 4f);
            Eye("eye_shopfront", 60f, -235f, 60f, -223f, 1.5f);
            // Detached houses on their plots (city plan §19): the Terraces and the Ridge from above, a gate, a villa's side.
            Top("view_terraces", new Vector3(26f, 0f, -203f), 50f);
            Top("view_ridge_west", new Vector3(55f, 0f, -150f), 50f);
            Top("view_ridge_east", new Vector3(220f, 0f, -150f), 50f);
            Eye("eye_plot_gate", 18f, -199f, 7f, -199f, 1.7f);
            Eye("eye_villa_side", 40f, -172f, 21f, -146f, 1.7f);

            RenderInteriors();
            Debug.Log($"Veilmouth views written to {OutputFolder}.");
        }

        /// <summary>
        /// Each C1 interior from above with its ceilings hidden, and each room from a walker's eye in its south-west
        /// corner toward the far corner. Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderInteriors
        /// </summary>
        [MenuItem("GameDev/Slice/Render Veilmouth Interiors")]
        public static void RenderInteriors()
        {
            Directory.CreateDirectory(OutputFolder);
            foreach (var (_, path) in VeilmouthInteriors.All)
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                ShowCircles();
                var name = Path.GetFileNameWithoutExtension(path);
                var ceilings = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                    .Where(t => t.name.StartsWith("Kit_BkRoom_Ceiling")).Select(t => t.gameObject).ToArray();
                foreach (var c in ceilings)
                {
                    c.SetActive(false);
                }

                var room = Bounds();
                Top("room_" + name, room.center, Mathf.Max(room.extents.x, room.extents.z) + 0.5f);
                foreach (var c in ceilings)
                {
                    c.SetActive(true);
                }

                foreach (var marker in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                             .Where(t => t.name.StartsWith(VeilmouthInteriors.RoomMarker)))
                {
                    var p = marker.position;
                    var half = marker.localScale * 0.5f;
                    var label = marker.name.Substring(VeilmouthInteriors.RoomMarker.Length).Replace(' ', '_').ToLowerInvariant();
                    Eye($"roomeye_{name}_{label}", new Vector3(p.x - half.x + 0.5f, 1.7f, p.z - half.z + 0.5f),
                        new Vector3(p.x + half.x - 0.5f, 1.0f, p.z + half.z - 0.5f));
                }
            }
        }

        /// <summary>
        /// Sample dwellings, one of each kind at each wealth it comes in, from above with their ceilings hidden, and
        /// their kit faults in the log. Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderDwellings
        /// </summary>
        [MenuItem("GameDev/Slice/Dwellings/Render Sample Dwellings")]
        public static void RenderDwellings()
        {
            Directory.CreateDirectory(OutputFolder);
            var kit = VeilmouthDwellings.Kit;
            var settings = VeilmouthDwellings.Settings;
            var samples = new[]
            {
                (Dwellings.DwellingArchetype.Tenement, Dwellings.DwellingWealth.Poor), (Dwellings.DwellingArchetype.Terrace, Dwellings.DwellingWealth.Middling),
                (Dwellings.DwellingArchetype.Terrace, Dwellings.DwellingWealth.Rich), (Dwellings.DwellingArchetype.Shop, Dwellings.DwellingWealth.Poor),
                (Dwellings.DwellingArchetype.Shop, Dwellings.DwellingWealth.Middling), (Dwellings.DwellingArchetype.Villa, Dwellings.DwellingWealth.Rich),
                (Dwellings.DwellingArchetype.Warehouse, Dwellings.DwellingWealth.Poor),
            };
            var faults = 0;
            foreach (var (archetype, wealth) in samples)
            {
                for (var n = 0; n < 2; n++)
                {
                    EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                    var key = VeilmouthDwellings.PreviewKey(archetype, wealth, 1000 + n * 37, -2000 - n * 53);
                    var found = VeilmouthDwellings.BuildAndCheck(key, kit, settings, out var root);
                    faults += found.Count;
                    var plan = Dwellings.DwellingRuntime.PlanFor(key, settings, kit);
                    if (plan.Unplaced.Count > 0)
                    {
                        Debug.Log($"[Dwelling] {archetype} {wealth} #{n} found no room for: {string.Join(", ", plan.Unplaced)}");
                    }
                    foreach (var f in found)
                    {
                        Debug.LogWarning($"[Dwelling] {archetype} {wealth} #{n}: {f}");
                    }

                    foreach (var c in root.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Kit_BkRoom_Ceiling")))
                    {
                        c.gameObject.SetActive(false);
                    }

                    var b = Bounds();
                    Top($"dwelling_{archetype}_{wealth}_{n}".ToLowerInvariant(), b.center, Mathf.Max(b.extents.x, b.extents.z) + 0.5f);
                }
            }

            Debug.Log($"[Dwelling] {samples.Length * 2} sample dwellings rendered, {faults} kit faults.");
        }

        private static void ShowCircles()
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(t => t.name.StartsWith("Ritual Circle")))
            {
                t.gameObject.SetActive(true);
            }
        }

        private static Bounds Bounds()
        {
            var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            var b = renderers[0].bounds;
            foreach (var r in renderers.Skip(1))
            {
                b.Encapsulate(r.bounds);
            }

            return b;
        }

        private static void Top(string name, Vector3 centre, float half)
        {
            var camera = Make();
            camera.orthographic = true;
            camera.orthographicSize = half;
            camera.transform.SetPositionAndRotation(new Vector3(centre.x, 300f, centre.z), Quaternion.LookRotation(Vector3.down, Vector3.forward));
            camera.farClipPlane = 400f;
            Shoot(camera, name, 1400, 1400);
        }

        /// <summary>An eye 1.7 m above the ground at (x, z), looking at (toX, toZ) at <paramref name="up"/> above its ground.</summary>
        private static void Eye(string name, float x, float z, float toX, float toZ, float up)
        {
            var terrain = Terrain.activeTerrain;
            float Ground(float gx, float gz) => terrain.SampleHeight(new Vector3(gx, 0f, gz)) + terrain.transform.position.y;
            Eye(name, new Vector3(x, Ground(x, z) + 1.7f, z), new Vector3(toX, Ground(toX, toZ) + up, toZ));
        }

        private static void Eye(string name, Vector3 from, Vector3 to)
        {
            var camera = Make();
            camera.fieldOfView = 65f;
            camera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(to - from));
            camera.farClipPlane = 1500f;
            Shoot(camera, name, 1600, 900);
        }

        /// <summary>
        /// A walking townsperson close by, from the front and the side, and the townsfolk's routes from above.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthRenders.RenderTownsfolk
        /// </summary>
        [MenuItem("GameDev/Slice/Render Veilmouth Townsfolk")]
        public static void RenderTownsfolk()
        {
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var walkers = Object.FindObjectsByType<GameDev.Slice.People.TownsfolkWalker>(FindObjectsSortMode.None).OrderBy(w => w.transform.position.x).ToList();
            var line = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = new Color(1f, 0.15f, 0.6f) };
            foreach (var w in walkers)
            {
                var animator = w.GetComponentInChildren<Animator>();
                animator.runtimeAnimatorController.animationClips.First(c => c.name == "Walk").SampleAnimation(animator.gameObject, 0.6f);
                var lr = new GameObject("Route").AddComponent<LineRenderer>();
                lr.sharedMaterial = line;
                lr.loop = true;
                lr.widthMultiplier = 0.5f;
                lr.positionCount = w.Route.Length;
                lr.SetPositions(w.Route.Select(p => p + (Vector3.up * 0.3f)).ToArray());
            }

            foreach (var (prefix, prefab) in new[] { ("townsperson", SliceTownsfolk.PrefabPath), ("townswoman", SliceTownsfolk.WomanPrefabPath) })
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(prefab);
                var kind = walkers.Where(w => PrefabUtility.GetCorrespondingObjectFromSource(w.gameObject) == source).ToList();
                var one = kind[kind.Count / 2].transform;
                var at = one.position + (Vector3.up * 1.1f);
                Shot($"{prefix}_front", at + (one.forward * 3.2f) + (Vector3.up * 0.2f), at);
                Shot($"{prefix}_side", at + (one.right * 3.2f) + (Vector3.up * 0.2f), at);
            }

            Top("townsfolk_routes_city", new Vector3(130f, 0f, -250f), 150f);
            Top("townsfolk_routes_market", new Vector3(50f, 0f, -270f), 40f);
            Debug.Log($"Townsfolk views written to {OutputFolder}.");
        }

        private static void Shot(string name, Vector3 from, Vector3 to)
        {
            var camera = Make();
            camera.fieldOfView = 45f;
            camera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(to - from));
            Shoot(camera, name, 900, 900);
        }

        private static Camera Make()
        {
            var camera = new GameObject("RenderCamera").AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.62f, 0.74f, 0.86f);
            return camera;
        }

        private static void Shoot(Camera camera, string name, int width, int height)
        {
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes($"{OutputFolder}/{name}.png", image.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(camera.gameObject);
        }
    }
}

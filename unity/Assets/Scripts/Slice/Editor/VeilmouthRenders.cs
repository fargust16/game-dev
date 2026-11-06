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
            // C1 views; an eye stands 1.7 m above the ground under it.
            Top("view_world", new Vector3(0f, 0f, 0f), 500f);
            Top("view_city", new Vector3(30f, 0f, -160f), 260f);
            Top("view_lowerveil", new Vector3(170f, 0f, -255f), 80f);
            Top("view_government", new Vector3(0f, 0f, -70f), 80f);
            Top("view_market", new Vector3(-80f, 0f, -255f), 80f);
            Top("view_hill", new Vector3(420f, 0f, 140f), 180f);
            Eye("eye_bellstreet", 215f, -235f, 100f, -235f, 2.5f);
            Eye("eye_highstreet", -150f, -171f, 100f, -171f, 3f);
            Eye("eye_avenue", 0f, -150f, 0f, -20f, 6f);
            Eye("eye_ridgestreet", -80f, -86f, 100f, -86f, 3f);
            Eye("eye_sootstair", 204f, -95f, 204f, -150f, 0f);
            Eye("eye_bridge", 300f, -172f, 150f, -172f, 4f);
            Eye("eye_quay", 100f, -318f, 300f, -330f, 0f);
            Eye("eye_westgate", -300f, -172f, -200f, -150f, 4f);
            Eye("eye_forest", 123f, 120f, 123f, 300f, 2f);
            Eye("eye_hill", 340f, 0f, 420f, 140f, 20f);

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

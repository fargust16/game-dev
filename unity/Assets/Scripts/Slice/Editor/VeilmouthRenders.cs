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
            const float s = VeilmouthScene.StreetLevel;
            Top("view_world", new Vector3(0f, 0f, 0f), 350f);
            Top("view_city", new Vector3(40f, 0f, -170f), 110f);
            Top("view_lowerveil", new Vector3(90f, 0f, -200f), 55f);
            Top("view_market", new Vector3(-16f, 0f, -202f), 26f);
            Top("view_chapel", new Vector3(-70f, 0f, -58f), 18f);
            Top("view_hill", new Vector3(265f, 0f, 175f), 140f);
            Eye("eye_bellstreet", new Vector3(104f, s + 1.7f, -191f), new Vector3(102f, s + 4f, -176f));
            Eye("eye_harrow", new Vector3(116f, s + 1.7f, -191f), new Vector3(115f, s + 3f, -176f));
            Eye("eye_faction_wardens", new Vector3(36f, s + 1.7f, -98f), new Vector3(36f, s + 4f, -84f));
            Eye("eye_hill", new Vector3(200f, s + 3f, 60f), new Vector3(260f, s, 150f));
            Eye("eye_bellstreet_east", new Vector3(60f, s + 1.7f, -188f), new Vector3(110f, s + 3f, -188f));
            Eye("eye_market", new Vector3(-15f, s + 1.7f, -214f), new Vector3(-18f, s + 1f, -186f));
            Eye("eye_ridge", new Vector3(30f, s + 1.7f, -96f), new Vector3(50f, s + 3f, -90f));
            Eye("eye_chapel", new Vector3(-76f, s + 1.7f, -72f), new Vector3(-75f, s + 4f, -55f));
            Eye("eye_bridge", new Vector3(146f, s + 1.7f, -200f), new Vector3(175f, s + 1f, -200f));
            Eye("eye_ritual_square", new Vector3(120f, s + 6f, -250f), new Vector3(120f, s, -236f));
            Eye("eye_forest", new Vector3(-200f, s + 1.7f, 62f), new Vector3(-200f, s + 2f, 80f));

            foreach (var path in new[] { VeilmouthScene.SootAndBellScenePath }.Concat(VeilmouthDressing.InteriorScenePaths))
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                ShowCircles();
                var name = Path.GetFileNameWithoutExtension(path);
                var room = Bounds();
                Top("room_" + name, room.center, Mathf.Max(room.extents.x, room.extents.z) + 0.5f);
                Eye("roomeye_" + name, new Vector3(room.center.x, 2.2f, room.min.z + 0.5f), new Vector3(room.center.x, 0.8f, room.max.z));
            }

            Debug.Log($"Veilmouth views written to {OutputFolder}.");
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
            camera.transform.SetPositionAndRotation(new Vector3(centre.x, 200f, centre.z), Quaternion.LookRotation(Vector3.down, Vector3.forward));
            camera.farClipPlane = 400f;
            Shoot(camera, name, 1400, 1400);
        }

        private static void Eye(string name, Vector3 from, Vector3 to)
        {
            var camera = Make();
            camera.fieldOfView = 65f;
            camera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(to - from));
            camera.farClipPlane = 800f;
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

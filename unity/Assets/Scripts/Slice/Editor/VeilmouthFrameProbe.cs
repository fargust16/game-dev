using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Debug = UnityEngine.Debug;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// What Gloomwood's cover trees cost (VEILMOUTH_CITY_PLAN.md §28.10): a census of the city's game objects and renderers
    /// and the forest's share, and the time five walker's views take to draw at 1920 × 1080 with the forest's trees shown
    /// and hidden: four rounds of each, taken in turn so that drift falls on both, of 9 frames after 3 warm-ups, the median
    /// of the 36 frames, every frame waited for by reading one pixel back.
    /// An editor frame carries the editor's own overhead and no player loop, so the figures compare the views with and
    /// without the trees; they promise no frame rate. Nothing is saved. The report goes to Logs/SliceSurvey.
    /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthFrameProbe.Run
    /// </summary>
    public static class VeilmouthFrameProbe
    {
        private const int Width = 1920, Height = 1080, Rounds = 4, WarmUps = 3, Frames = 9;
        private const string Report = "Logs/SliceSurvey/c4_frame_probe.txt";

        /// <summary>World X and Z of the eye and of what it looks at: in the wood, from the hamlet to the wood, from outside the gate to the city, down Salt Gullet, across the Penny.</summary>
        private static readonly (string Name, Vector2 From, Vector2 To)[] Views =
        {
            ("in Gloomwood", new Vector2(-240f, -50f), new Vector2(-200f, -110f)),
            ("Crooked Hamlet to the wood", new Vector2(-135f, 60f), new Vector2(-250f, 60f)),
            ("the West Road to the gate", new Vector2(-140f, 60f), new Vector2(-60f, 60f)),
            ("down Salt Gullet", new Vector2(150f, 60f), new Vector2(40f, 60f)),
            ("across the Penny", new Vector2(2f, 58f), new Vector2(40f, 60f)),
        };

        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var layout = VeilmouthLayout.Load();
            var roots = scene.GetRootGameObjects();
            var objects = roots.Sum(g => g.GetComponentsInChildren<Transform>(true).Length);
            var renderers = roots.SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true)).ToList();
            var outskirts = roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.name == "Outskirts");
            var forest = outskirts == null
                ? new List<GameObject>()
                : outskirts.Cast<Transform>()
                    .Where(t => (t.name.Contains("Tree_Pine") || t.name.Contains("Tree_Fruit") || t.name.Contains("Shrub")) &&
                                layout.forest.rects.Any(r => r.Contains(t.position.x + 500f, 500f - t.position.z)))
                    .Select(t => t.gameObject).ToList();
            var forestObjects = forest.Sum(g => g.GetComponentsInChildren<Transform>(true).Length);
            var forestRenderers = forest.Sum(g => g.GetComponentsInChildren<MeshRenderer>(true).Length);
            var forestLods = forest.Count(g => g.GetComponentInChildren<LODGroup>(true) != null);
            var forestTriangles = forest.SelectMany(g => g.GetComponentsInChildren<MeshFilter>(true))
                .Where(f => f.sharedMesh != null)
                .Sum(f => Enumerable.Range(0, f.sharedMesh.subMeshCount).Sum(s => (long)f.sharedMesh.GetIndexCount(s)) / 3);

            var text = new StringBuilder();
            text.AppendLine($"Veilmouth C4 frame probe, {System.DateTime.Now:yyyy-MM-dd HH:mm}, Unity {Application.unityVersion}, graphics {SystemInfo.graphicsDeviceType}, {SystemInfo.graphicsDeviceName}");
            text.AppendLine($"Scene: {objects:N0} game objects, {renderers.Count:N0} mesh renderers.");
            text.AppendLine($"Gloomwood's cover: {forest.Count:N0} trees, {forestObjects:N0} game objects, {forestRenderers:N0} mesh renderers, " +
                            $"{forestLods:N0} with an LOD group, {forestTriangles:N0} triangles in all their meshes.");
            text.AppendLine($"Views at {Width} x {Height}, {Rounds} rounds in turn of {Frames} frames after {WarmUps} warm-ups, median ms: with the trees / without / difference");

            var terrain = Terrain.activeTerrain;
            float Ground(Vector2 p) => terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + terrain.transform.position.y;
            var samples = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp ? urp.msaaSampleCount : 1;
            var target = new RenderTexture(Width, Height, 24) { antiAliasing = Mathf.Max(1, samples) };
            var pixel = new Texture2D(1, 1, TextureFormat.RGB24, false);
            var camera = new GameObject("ProbeCamera").AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 1500f;
            camera.fieldOfView = 65f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.62f, 0.74f, 0.86f);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            camera.targetTexture = target;
            foreach (var (name, from, to) in Views)
            {
                var eye = new Vector3(from.x, Ground(from) + 1.7f, from.y);
                camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(new Vector3(to.x, Ground(to) + 2f, to.y) - eye));
                var shown = new List<double>();
                var hidden = new List<double>();
                for (var round = 0; round < Rounds; round++)
                {
                    Time(camera, target, pixel, shown);
                    forest.ForEach(g => g.SetActive(false));
                    Time(camera, target, pixel, hidden);
                    forest.ForEach(g => g.SetActive(true));
                }

                var with = Median(shown);
                var without = Median(hidden);
                text.AppendLine($"  {name}: {with:0.00} / {without:0.00} / {with - without:+0.00;-0.00}");
            }

            camera.targetTexture = null;
            Object.DestroyImmediate(camera.gameObject);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(pixel);
            Directory.CreateDirectory("Logs/SliceSurvey");
            File.WriteAllText(Report, text.ToString());
            Debug.Log($"[VeilmouthFrameProbe] report written to {Report}\n{text}");
        }

        private static void Time(Camera camera, RenderTexture target, Texture2D pixel, List<double> into)
        {
            for (var i = 0; i < WarmUps + Frames; i++)
            {
                var watch = Stopwatch.StartNew();
                camera.Render();
                if (target.antiAliasing > 1)
                {
                    target.ResolveAntiAliasedSurface();
                }

                RenderTexture.active = target;
                pixel.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
                RenderTexture.active = null;
                watch.Stop();
                if (i >= WarmUps)
                {
                    into.Add(watch.Elapsed.TotalMilliseconds);
                }
            }
        }

        private static double Median(List<double> times)
        {
            times.Sort();
            return times[times.Count / 2];
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Unity.Profiling;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// A diagnostic, not an acceptance test (VEILMOUTH_CITY_PLAN.md §29.7): what the GPU Resident Drawer with its GPU
    /// occlusion culling does to Veilmouth C5's frames. Seven walker's views at 1920 × 1080 are drawn with the drawer off
    /// (the SRP Batcher alone) and on, in turn for <see cref="Rounds"/> rounds so that drift falls on both:
    /// <see cref="WarmUps"/> frames after each switch, then <see cref="Frames"/> frames, each waited for by reading one
    /// pixel back, and the median of each. Then the drawer's own Profiler counters say which renderers it took, why it
    /// left the rest, and what each view saw and culled; it emits them only while the Profiler records, so the timed
    /// rounds run with the Profiler off. The drawer needs the Graphics settings' "BatchRendererGroup Variants" at Keep
    /// All; the test sets it for its run and puts it, the Profiler and the pipeline asset back. An editor frame carries
    /// the editor's overhead, so the figures compare the two ways of drawing and promise no frame rate. The report goes
    /// to Logs/SliceSurvey/c5_gpu_culling.txt. It runs only when named, with graphics:
    /// <c>-testFilter GameDev.Slice.Tests.GpuResidentDrawerTimingTests</c>.
    /// </summary>
    [Explicit("diagnostic: run by name"), Category("Diagnostic")]
    public sealed class GpuResidentDrawerTimingTests : SliceInputTestBase
    {
        private const string CityPath = "Assets/Scenes/Slice/Veilmouth.unity";
        private const string Report = "Logs/SliceSurvey/c5_gpu_culling.txt";
        private const int Width = 1920, Height = 1080, Rounds = 3, WarmUps = 30, Frames = 60;

        /// <summary>World X and Z of the eye and of what it looks at.</summary>
        private static readonly (string Name, Vector2 From, Vector2 To)[] Views =
        {
            ("down Long Tongue from the gate", new Vector2(-70f, 60f), new Vector2(30f, 60f)),
            ("down Salt Gullet", new Vector2(150f, 60f), new Vector2(40f, 60f)),
            ("across the Penny", new Vector2(2f, 58f), new Vector2(40f, 60f)),
            ("down Thimble Street", new Vector2(-22f, 115f), new Vector2(-22f, -40f)),
            ("Draught Lane in Lower Veil", new Vector2(150f, 4f), new Vector2(50f, 4f)),
            ("the West Road to the gate", new Vector2(-140f, 60f), new Vector2(-60f, 60f)),
            ("in Gloomwood", new Vector2(-240f, -50f), new Vector2(-200f, -110f)),
        };

        /// <summary>The drawer's counters (GRDProfilerCounters in the Core package) the report reads.</summary>
        private const string Taken = "GRD Renderers", Left = "Excluded Renderers", Visible = "Visible Instances",
            Frustum = "Frustum Culled", Occluded = "GPU Occlusion Culled", Lod = "LOD Group Culled";

        private static readonly string[] Reasons =
        {
            "Excl: LOD Animate CrossFading", "Excl: Custom MaterialPropertyBlock", "Excl: Render Callback",
            "Excl: Non-Standard Sort Key", "Excl: Proxy Volume Probe", "Excl: Blend Probes With Anchor",
            "Excl: Enlighten Vertex Stream", "Excl: Missing DOTS Instancing", "Excl: Too Many Submeshes",
            "Excl: GPU Driven Disabled", "Excl: Animation Visibility", "Excl: TextMesh Component",
        };

        private UniversalRenderPipelineAsset _urp;
        private BatchRendererGroupStrippingMode _stripping;
        private GPUResidentDrawerMode _mode;
        private bool _occlusion;
        private bool _profiler;

        [UnityTest, Timeout(1800000)]
        public IEnumerator TheDrawerWithOcclusionCullingAgainstTheSrpBatcher()
        {
            _urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            _stripping = EditorGraphicsSettings.batchRendererGroupShaderStrippingMode;
            _mode = _urp.gpuResidentDrawerMode;
            _occlusion = _urp.gpuResidentDrawerEnableOcclusionCullingInCameras;
            _profiler = Profiler.enabled;
            Strip(BatchRendererGroupStrippingMode.KeepAll);
            _urp.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled;
            _urp.gpuResidentDrawerEnableOcclusionCullingInCameras = true;
            Profiler.enabled = false;

            yield return LoadScene(CityPath);
            foreach (var other in Camera.allCameras)
            {
                other.enabled = false;
            }

            var renderers = Object.FindObjectsByType<MeshRenderer>();
            var text = new StringBuilder();
            text.AppendLine($"Veilmouth C5 GPU Resident Drawer probe, {System.DateTime.Now:yyyy-MM-dd HH:mm}, Unity {Application.unityVersion}, " +
                            $"graphics {SystemInfo.graphicsDeviceType}, {SystemInfo.graphicsDeviceName}");
            text.AppendLine($"Scene: {renderers.Length:N0} mesh renderers; by the shader of their first material:");
            foreach (var shader in renderers.GroupBy(r => r.sharedMaterial != null && r.sharedMaterial.shader != null ? r.sharedMaterial.shader.name : "(none)")
                         .OrderByDescending(g => g.Count()).Take(10))
            {
                text.AppendLine($"  {shader.Key}: {shader.Count():N0}");
            }

            text.AppendLine($"Views at {Width} x {Height}, {Rounds} rounds in turn of {Frames} frames after {WarmUps} warm-ups, median ms: " +
                            "SRP Batcher alone / GPU Resident Drawer with occlusion culling / change");

            var terrain = Terrain.activeTerrain;
            float Ground(Vector2 p) => terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + terrain.transform.position.y;
            var target = new RenderTexture(Width, Height, 24) { antiAliasing = Mathf.Max(1, _urp.msaaSampleCount) };
            var pixel = new Texture2D(1, 1, TextureFormat.RGB24, false);
            var camera = new GameObject("ProbeCamera").AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 1500f;
            camera.fieldOfView = 65f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.62f, 0.74f, 0.86f);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            camera.targetTexture = target;

            void Look(Vector2 from, Vector2 to)
            {
                var eye = new Vector3(from.x, Ground(from) + 1.7f, from.y);
                camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(new Vector3(to.x, Ground(to) + 2f, to.y) - eye));
            }

            void WaitForThePixel()
            {
                if (target.antiAliasing > 1)
                {
                    target.ResolveAntiAliasedSurface();
                }

                RenderTexture.active = target;
                pixel.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
                RenderTexture.active = null;
            }

            // Two views drawn each way to the survey folder, to see the drawer draws the same city.
            IEnumerator Picture(bool drawer)
            {
                _urp.gpuResidentDrawerMode = drawer ? GPUResidentDrawerMode.InstancedDrawing : GPUResidentDrawerMode.Disabled;
                foreach (var k in new[] { 2, 4 })
                {
                    Look(Views[k].From, Views[k].To);
                    for (var i = 0; i < WarmUps; i++)
                    {
                        yield return null;
                        WaitForThePixel();
                    }

                    if (target.antiAliasing > 1)
                    {
                        target.ResolveAntiAliasedSurface();
                    }

                    var frame = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                    RenderTexture.active = target;
                    frame.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
                    RenderTexture.active = null;
                    File.WriteAllBytes($"Logs/SliceSurvey/c5_grd_{k}_{(drawer ? "on" : "off")}.png", frame.EncodeToPNG());
                    Object.Destroy(frame);
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Report));
            yield return Picture(false);
            yield return Picture(true);

            var ran = 0;
            var offAll = new List<double>();
            var onAll = new List<double>();
            foreach (var (name, from, to) in Views)
            {
                Look(from, to);
                var off = new List<double>();
                var on = new List<double>();
                for (var round = 0; round < Rounds; round++)
                {
                    foreach (var drawer in new[] { false, true })
                    {
                        _urp.gpuResidentDrawerMode = drawer ? GPUResidentDrawerMode.InstancedDrawing : GPUResidentDrawerMode.Disabled;
                        var laps = new List<double>();
                        var watch = Stopwatch.StartNew();
                        for (var i = 0; i < WarmUps + Frames; i++)
                        {
                            yield return null;
                            WaitForThePixel();
                            if (i >= WarmUps)
                            {
                                laps.Add(watch.Elapsed.TotalMilliseconds);
                            }

                            watch.Restart();
                        }

                        if (drawer && GPUResidentDrawer.IsInstanceOcclusionCullingEnabled())
                        {
                            ran++;
                        }

                        (drawer ? on : off).AddRange(laps);
                    }
                }

                var a = Median(off);
                var b = Median(on);
                offAll.AddRange(off);
                onAll.AddRange(on);
                text.AppendLine($"  {name}: {a:0.00} / {b:0.00} / {(b - a) / a:+0%;-0%}");
            }

            var all = Median(offAll);
            var allOn = Median(onAll);
            text.AppendLine($"All views: {all:0.00} / {allOn:0.00} / {(allOn - all) / all:+0%;-0%}; the drawer with occlusion culling ran in {ran} of {Views.Length * Rounds} switches on.");

            // The drawer's counters, with the Profiler on and the drawer on.
            var category = new ProfilerCategory("GPU Resident Drawer");
            Profiler.enabled = true;
            Profiler.SetCategoryEnabled(category, true);
            _urp.gpuResidentDrawerMode = GPUResidentDrawerMode.InstancedDrawing;
            var counters = new[] { Taken, Left, Visible, Frustum, Occluded, Lod }.Concat(Reasons)
                .ToDictionary(n => n, n => ProfilerRecorder.StartNew(category, n));
            text.AppendLine("The drawer's counters (Profiler on, drawer on), each view after the warm-ups: visible instances / frustum culled / GPU occlusion culled / LOD culled");
            foreach (var (name, from, to) in Views)
            {
                Look(from, to);
                for (var i = 0; i < WarmUps; i++)
                {
                    yield return null;
                    WaitForThePixel();
                }

                text.AppendLine($"  {name}: {counters[Visible].LastValue:N0} / {counters[Frustum].LastValue:N0} / {counters[Occluded].LastValue:N0} / {counters[Lod].LastValue:N0}");
            }

            var taken = counters[Taken].LastValue;
            var left = counters[Left].LastValue;
            text.AppendLine($"Renderers the drawer took: {taken:N0}; left to the SRP Batcher: {left:N0} " +
                            $"({(taken + left > 0 ? 100.0 * taken / (taken + left) : 0):0.0} % taken). Why it left them:");
            foreach (var reason in Reasons.Where(r => counters[r].LastValue > 0))
            {
                text.AppendLine($"  {reason.Substring(6)}: {counters[reason].LastValue:N0}");
            }

            foreach (var recorder in counters.Values)
            {
                recorder.Dispose();
            }

            Profiler.enabled = _profiler;
            Object.Destroy(camera.gameObject);
            Object.Destroy(target);
            Object.Destroy(pixel);
            Directory.CreateDirectory(Path.GetDirectoryName(Report));
            File.WriteAllText(Report, text.ToString());
            Debug.Log($"[GpuResidentDrawerTiming] report written to {Report}\n{text}");
            Assert.AreEqual(Views.Length * Rounds, ran, "the GPU Resident Drawer with occlusion culling ran at every switch on");
        }

        [TearDown]
        public void PutTheSettingsBack()
        {
            if (_urp == null)
            {
                return;
            }

            Profiler.enabled = _profiler;
            _urp.gpuResidentDrawerMode = _mode;
            _urp.gpuResidentDrawerEnableOcclusionCullingInCameras = _occlusion;
            Strip(_stripping);
        }

        /// <summary>The Graphics settings' "BatchRendererGroup Variants", which the editor's API only reads.</summary>
        private static void Strip(BatchRendererGroupStrippingMode mode)
        {
            var settings = new UnityEditor.SerializedObject(GraphicsSettings.GetGraphicsSettings());
            settings.FindProperty("m_BrgStripping").intValue = (int)mode;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Assert.AreEqual(mode, EditorGraphicsSettings.batchRendererGroupShaderStrippingMode, "BatchRendererGroup Variants");
        }

        private static double Median(List<double> times)
        {
            var sorted = times.OrderBy(t => t).ToList();
            return sorted[sorted.Count / 2];
        }
    }
}

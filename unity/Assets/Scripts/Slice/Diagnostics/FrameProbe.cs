using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using GameDev.Core.Scenes;
using GameDev.Locomotion;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace GameDev.Slice.Diagnostics
{
    /// <summary>
    /// The frame diagnostic in a development build (VEILMOUTH_CITY_PLAN.md §29.7): the editor's
    /// <c>GpuResidentDrawerTimingTests</c> in a player, where the GPU Resident Drawer's jobs run Burst-compiled and
    /// without the editor's safety checks. It wakes only in a development build started with
    /// <c>-frameTiming &lt;report file&gt;</c> (<c>SliceLoadProbeBuild.BuildForFrames</c> makes one, keeping the
    /// BatchRendererGroup variants the drawer draws with). Veilmouth loads, the player's own figure is hidden, and from the
    /// editor test's seven views the main camera draws, in turn for <see cref="Rounds"/> rounds, with the drawer off (the
    /// SRP Batcher alone) and on with GPU occlusion culling: <see cref="WarmUps"/> frames after each switch, then
    /// <see cref="Frames"/> measured, with no frame cap and no vertical sync. The report gives the median frame, and the
    /// main thread's and the GPU's time from the FrameTimingManager where the player has it; then the player quits. An
    /// exception or 20 minutes also write what was measured and quit.
    /// </summary>
    public sealed class FrameProbe : MonoBehaviour
    {
        private const string Flag = "-frameTiming";
        private const string City = "Veilmouth";
        private const int Rounds = 3, WarmUps = 60, Frames = 120;
        private const float Watchdog = 1200f; // s

        /// <summary>World X and Z of the eye and of what it looks at, as the editor test's.</summary>
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

        private readonly StringBuilder _report = new StringBuilder();
        private readonly FrameTiming[] _timing = new FrameTiming[1];
        private string _file;
        private bool _done;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Application.isEditor || !Debug.isDebugBuild)
            {
                return;
            }

            var args = Environment.GetCommandLineArgs();
            var i = Array.IndexOf(args, Flag);
            if (i < 0)
            {
                return;
            }

            Application.runInBackground = true;
            var go = new GameObject("Frame Probe");
            DontDestroyOnLoad(go);
            var probe = go.AddComponent<FrameProbe>();
            probe._file = i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal)
                ? args[i + 1]
                : Path.Combine(Application.persistentDataPath, "frame_timing.txt");
            Application.logMessageReceived += probe.OnLog;
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Exception && !_done)
            {
                _report.AppendLine().AppendLine($"!! exception: {message}").AppendLine(stack);
                Finish("stopped by an exception");
            }
        }

        private void Update()
        {
            if (!_done && Time.realtimeSinceStartup > Watchdog)
            {
                Finish($"stopped after {Watchdog:0} s");
            }
        }

        private IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset
                      ?? throw new InvalidOperationException("the pipeline is not URP");
            _report.AppendLine($"Frame diagnostic in a development build, {DateTime.Now:yyyy-MM-dd HH:mm}, Unity {Application.unityVersion}, " +
                               $"graphics {SystemInfo.graphicsDeviceType}, {SystemInfo.graphicsDeviceName}, " +
                               $"quality '{QualitySettings.names[QualitySettings.GetQualityLevel()]}', {Screen.width}x{Screen.height}, " +
                               $"frame timing stats {(FrameTimingManager.IsFeatureEnabled() ? "on" : "off")}");
            yield return null;

            SceneTransition.LoadInTheDark(true);
            var op = SceneManager.LoadSceneAsync(City, LoadSceneMode.Single);
            while (!op.isDone)
            {
                yield return null;
            }

            yield return null;
            SceneTransition.LoadInTheDark(false);
            var warm = Stopwatch.StartNew();
            while (ShaderWarmUp.Collection != null && !ShaderWarmUp.IsDone && warm.Elapsed.TotalSeconds < 120d)
            {
                yield return null;
            }

            var cam = Camera.main ?? throw new InvalidOperationException("the city has no main camera");
            foreach (var b in cam.GetComponents<Behaviour>().Where(b => (b.GetType().Namespace ?? string.Empty).StartsWith("GameDev", StringComparison.Ordinal)))
            {
                b.enabled = false;
            }

            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            if (player != null)
            {
                foreach (var r in player.GetComponentsInChildren<Renderer>())
                {
                    r.enabled = false;
                }
            }

            var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude).Length;
            _report.AppendLine($"{City}: {renderers:N0} mesh renderers; camera field of view {cam.fieldOfView:0}°, far plane {cam.farClipPlane:0} m");
            _report.AppendLine($"{Rounds} rounds in turn of {Frames} frames after {WarmUps} warm-ups, median ms, SRP Batcher alone -> GPU Resident Drawer with occlusion culling: " +
                               "frame / main thread / GPU");

            var terrain = Terrain.activeTerrain;
            float Ground(Vector2 p) => terrain != null ? terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + terrain.transform.position.y : 0f;
            urp.gpuResidentDrawerEnableOcclusionCullingInCameras = true;
            var ran = 0;
            var all = new[] { new Laps(), new Laps() };
            foreach (var (name, from, to) in Views)
            {
                var eye = new Vector3(from.x, Ground(from) + 1.7f, from.y);
                cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(new Vector3(to.x, Ground(to) + 2f, to.y) - eye));
                var view = new[] { new Laps(), new Laps() };
                for (var round = 0; round < Rounds; round++)
                {
                    for (var k = 0; k < 2; k++)
                    {
                        urp.gpuResidentDrawerMode = k == 1 ? GPUResidentDrawerMode.InstancedDrawing : GPUResidentDrawerMode.Disabled;
                        for (var i = 0; i < WarmUps + Frames; i++)
                        {
                            yield return null;
                            FrameTimingManager.CaptureFrameTimings();
                            if (i < WarmUps)
                            {
                                continue;
                            }

                            var got = FrameTimingManager.GetLatestTimings(1, _timing) > 0;
                            view[k].Add(Time.unscaledDeltaTime * 1000d, got ? _timing[0].cpuMainThreadFrameTime : double.NaN, got ? _timing[0].gpuFrameTime : double.NaN);
                        }

                        if (k == 1 && GPUResidentDrawer.IsInstanceOcclusionCullingEnabled())
                        {
                            ran++;
                        }
                    }
                }

                _report.AppendLine($"  {name}: {view[0]} -> {view[1]}");
                all[0].AddAll(view[0]);
                all[1].AddAll(view[1]);
            }

            _report.AppendLine($"All views: {all[0]} -> {all[1]}; the drawer with occlusion culling ran in {ran} of {Views.Length * Rounds} switches on.");
            urp.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled;
            Finish("done");
        }

        private void Finish(string how)
        {
            if (_done)
            {
                return;
            }

            _done = true;
            _report.AppendLine().AppendLine($"Run {how} at {Time.realtimeSinceStartup:0.0} s since start.");
            try
            {
                var folder = Path.GetDirectoryName(Path.GetFullPath(_file));
                if (!string.IsNullOrEmpty(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                File.WriteAllText(_file, _report.ToString());
            }
            catch (Exception e)
            {
                Debug.LogError($"[FrameProbe] could not write {_file}: {e.Message}\n{_report}");
            }

            Debug.Log($"[FrameProbe] {how}\n{_report}");
            Application.Quit();
        }

        /// <summary>Frame, main-thread and GPU times of the measured frames, in ms.</summary>
        private sealed class Laps
        {
            private readonly List<double> _frame = new List<double>(), _cpu = new List<double>(), _gpu = new List<double>();

            public void Add(double frame, double cpu, double gpu)
            {
                _frame.Add(frame);
                if (cpu > 0d)
                {
                    _cpu.Add(cpu);
                }

                if (gpu > 0d)
                {
                    _gpu.Add(gpu);
                }
            }

            public void AddAll(Laps other)
            {
                _frame.AddRange(other._frame);
                _cpu.AddRange(other._cpu);
                _gpu.AddRange(other._gpu);
            }

            public override string ToString() => $"{Median(_frame)} / {Median(_cpu)} / {Median(_gpu)}";

            private static string Median(List<double> times)
            {
                if (times.Count == 0)
                {
                    return "n/a";
                }

                var sorted = times.OrderBy(t => t).ToList();
                return sorted[sorted.Count / 2].ToString("0.00");
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using GameDev.Core.Scenes;
using GameDev.Locomotion;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace GameDev.Slice.Diagnostics
{
    /// <summary>
    /// The frame diagnostic in a development build (VEILMOUTH_CITY_PLAN.md §29.7–§29.9): the editor's
    /// <c>GpuResidentDrawerTimingTests</c> in a player, where the GPU Resident Drawer's jobs run Burst-compiled and
    /// without the editor's safety checks. It wakes only in a development build started with
    /// <c>-frameTiming &lt;report file&gt;</c> (<c>SliceLoadProbeBuild.BuildForFrames</c> makes one, keeping the
    /// BatchRendererGroup variants the drawer draws with). Veilmouth loads, the player's own figure is hidden, and from the
    /// editor test's seven views the main camera draws, in turn for <see cref="Rounds"/> rounds, in each of
    /// <see cref="Modes"/>: the SRP Batcher alone, as the game ships; the drawer with GPU occlusion culling; and the SRP
    /// Batcher alone with the camera's shadows off. Each switch is followed by <see cref="WarmUps"/> frames, then
    /// <see cref="Frames"/> are measured, with no frame cap and no vertical sync.
    /// <para>
    /// The report gives medians: the frame; from the FrameTimingManager the main thread (from the frame's start to the end
    /// of its work, so it holds its waits), the main thread's wait for Present, the render thread (from the start of its
    /// work to Present) and the GPU; the renderer's counters (batches, set-pass calls, draw calls, triangles, shadow
    /// casters); and every <c>Gfx.Wait…</c> marker the player has, summed over a frame on all threads. Then the player
    /// quits. An exception or 20 minutes also write what was measured and quit.
    /// </para>
    /// </summary>
    public sealed class FrameProbe : MonoBehaviour
    {
        private const string Flag = "-frameTiming";
        private const string City = "Veilmouth";
        private const int Rounds = 3, WarmUps = 60, Frames = 120;
        private const float Watchdog = 1200f; // s

        private const string Frame = "frame", Main = "main thread", Wait = "its wait for Present", Render = "render thread", Gpu = "GPU";
        private static readonly string[] Times = { Frame, Main, Wait, Render, Gpu };
        private static readonly string[] Modes = { "SRP Batcher alone", "drawer with occlusion culling", "SRP Batcher alone, no shadows" };
        private static readonly string[] Counters = { "Batches Count", "SetPass Calls Count", "Draw Calls Count", "Triangles Count", "Shadow Casters Count" };

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
        private readonly List<(string Name, bool Nanoseconds, ProfilerRecorder Recorder)> _recorders = new List<(string, bool, ProfilerRecorder)>();
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

        private void OnDestroy()
        {
            foreach (var (_, _, recorder) in _recorders)
            {
                recorder.Dispose();
            }

            _recorders.Clear();
        }

        private IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset
                      ?? throw new InvalidOperationException("the pipeline is not URP");
            _report.AppendLine($"Frame diagnostic in a development build, {DateTime.Now:yyyy-MM-dd HH:mm}, Unity {Application.unityVersion}, " +
                               $"graphics {SystemInfo.graphicsDeviceType}, {SystemInfo.graphicsDeviceName}, {SystemInfo.processorCount} logical processors, " +
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

            // The city draws a few frames first, so the render thread's markers are registered before they are looked up.
            for (var i = 0; i < 30; i++)
            {
                yield return null;
            }

            StartRecorders();
            var camData = cam.GetUniversalAdditionalCameraData();
            var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude).Length;
            _report.AppendLine($"{City}: {renderers:N0} mesh renderers; camera field of view {cam.fieldOfView:0}°, far plane {cam.farClipPlane:0} m, " +
                               $"shadows {(camData.renderShadows ? "on" : "off")} to {urp.shadowDistance:0} m");
            _report.AppendLine($"Recorded: {string.Join(", ", _recorders.Select(r => r.Name))}");
            _report.AppendLine($"{Rounds} rounds in turn of {Frames} frames after {WarmUps} warm-ups in each mode; medians, ms: {string.Join(" / ", Times)}");

            var terrain = Terrain.activeTerrain;
            float Ground(Vector2 p) => terrain != null ? terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + terrain.transform.position.y : 0f;
            urp.gpuResidentDrawerEnableOcclusionCullingInCameras = true;
            var ran = 0;
            var all = Modes.Select(_ => new Laps()).ToArray();
            foreach (var (name, from, to) in Views)
            {
                var eye = new Vector3(from.x, Ground(from) + 1.7f, from.y);
                cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(new Vector3(to.x, Ground(to) + 2f, to.y) - eye));
                var view = Modes.Select(_ => new Laps()).ToArray();
                for (var round = 0; round < Rounds; round++)
                {
                    for (var k = 0; k < Modes.Length; k++)
                    {
                        urp.gpuResidentDrawerMode = k == 1 ? GPUResidentDrawerMode.InstancedDrawing : GPUResidentDrawerMode.Disabled;
                        camData.renderShadows = k != 2;
                        for (var i = 0; i < WarmUps + Frames; i++)
                        {
                            yield return null;
                            FrameTimingManager.CaptureFrameTimings();
                            if (i >= WarmUps)
                            {
                                Sample(view[k]);
                            }
                        }

                        if (k == 1 && GPUResidentDrawer.IsInstanceOcclusionCullingEnabled())
                        {
                            ran++;
                        }
                    }
                }

                _report.AppendLine($"  {name}:");
                for (var k = 0; k < Modes.Length; k++)
                {
                    _report.AppendLine($"    {Modes[k]}: {view[k].Times()}; {view[k].Counts(_recorders)}");
                    all[k].AddAll(view[k]);
                }
            }

            _report.AppendLine($"All views (the drawer with occlusion culling ran in {ran} of {Views.Length * Rounds} switches on):");
            for (var k = 0; k < Modes.Length; k++)
            {
                _report.AppendLine($"  {Modes[k]}: {all[k].Times()}; {all[k].Counts(_recorders)}");
            }

            urp.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled;
            camData.renderShadows = true;
            Finish("done");
        }

        /// <summary>
        /// The renderer's counters by name, and every <c>Gfx.Wait…</c> or <c>Gfx.PresentFrame</c> marker the player has
        /// registered, found by name whatever its category, each summed over a frame on all threads.
        /// </summary>
        private void StartRecorders()
        {
            foreach (var counter in Counters)
            {
                var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, counter);
                if (recorder.Valid)
                {
                    _recorders.Add((counter, false, recorder));
                }
                else
                {
                    recorder.Dispose();
                }
            }

            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            foreach (var handle in handles)
            {
                var description = ProfilerRecorderHandle.GetDescription(handle);
                var markerName = description.Name;
                if (!(markerName.StartsWith("Gfx.Wait", StringComparison.Ordinal) || markerName == "Gfx.PresentFrame")
                    || _recorders.Any(r => r.Name == markerName))
                {
                    continue;
                }

                var recorder = new ProfilerRecorder(handle, 1, ProfilerRecorderOptions.Default);
                recorder.Start();
                _recorders.Add((markerName, description.UnitType == ProfilerMarkerDataUnit.TimeNanoseconds, recorder));
            }
        }

        private void Sample(Laps laps)
        {
            laps.Add(Frame, Time.unscaledDeltaTime * 1000d);
            if (FrameTimingManager.GetLatestTimings(1, _timing) > 0)
            {
                var t = _timing[0];
                laps.Add(Main, t.cpuMainThreadFrameTime);
                laps.Add(Wait, t.cpuMainThreadPresentWaitTime);
                laps.Add(Render, t.cpuRenderThreadFrameTime);
                if (t.gpuFrameTime > 0d)
                {
                    laps.Add(Gpu, t.gpuFrameTime);
                }
            }

            foreach (var (name, nanoseconds, recorder) in _recorders)
            {
                laps.Add(name, nanoseconds ? recorder.LastValue / 1e6 : recorder.LastValue);
            }
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

        /// <summary>The measured frames' samples by name: times in ms, counters as counted.</summary>
        private sealed class Laps
        {
            private readonly Dictionary<string, List<double>> _samples = new Dictionary<string, List<double>>();

            public void Add(string what, double value)
            {
                if (double.IsNaN(value) || value < 0d)
                {
                    return;
                }

                if (!_samples.TryGetValue(what, out var list))
                {
                    _samples[what] = list = new List<double>();
                }

                list.Add(value);
            }

            public void AddAll(Laps other)
            {
                foreach (var pair in other._samples)
                {
                    foreach (var value in pair.Value)
                    {
                        Add(pair.Key, value);
                    }
                }
            }

            public string Times() => string.Join(" / ", FrameProbe.Times.Select(t => Median(t, "0.00")));

            public string Counts(IEnumerable<(string Name, bool Nanoseconds, ProfilerRecorder Recorder)> recorders) =>
                string.Join(", ", recorders.Select(r => $"{r.Name} {Median(r.Name, r.Nanoseconds ? "0.00 'ms'" : "N0")}"));

            private string Median(string what, string format)
            {
                if (!_samples.TryGetValue(what, out var times) || times.Count == 0)
                {
                    return "n/a";
                }

                var sorted = times.OrderBy(t => t).ToList();
                return sorted[sorted.Count / 2].ToString(format);
            }
        }
    }
}

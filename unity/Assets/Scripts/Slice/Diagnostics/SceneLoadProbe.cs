using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using GameDev.Core.Scenes;
using GameDev.Locomotion;
using GameDev.World.Portals;
using GameDev.World.Timekeeping;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace GameDev.Slice.Diagnostics
{
    /// <summary>
    /// The scene load diagnostic in a development build (Plans/VEILMOUTH_REFERENCE_PLAN.md R1.4): the editor's
    /// <c>SceneLoadTimingTests</c> run in a player, which loads scenes as the game will, without the editor merging
    /// prefab instances on load. It wakes only in a development build started with
    /// <c>-sceneLoadTiming &lt;report file&gt;</c> (<c>SliceLoadProbeBuild</c> makes one): Veilmouth loads cold from the
    /// build's first, empty scene, the player goes into the Soot &amp; Bell and back twice and into a dwelling and back,
    /// and the report is written and the player quits. An exception or 20 minutes also write what was measured and quit.
    /// <para>
    /// The report says how the shader warm-up went (R1.5, <see cref="ShaderWarmUp"/>): its time, the states warmed,
    /// and the states the collection lacked on the walk. With <c>-traceGraphicsStates &lt;file&gt;</c> the player also
    /// records every shader variant and graphics state it draws: after the walk it stands at every street door of the
    /// city looking four ways and visits up to three doors of each interior the city leads to, by day and at 22:00, and
    /// saves the collection to the file, which the editor imports into <c>Resources/GraphicsStates</c>.
    /// </para>
    /// </summary>
    public sealed class SceneLoadProbe : MonoBehaviour
    {
        private const string Flag = "-sceneLoadTiming";
        private const string TraceFlag = "-traceGraphicsStates";
        private const int DoorsPerInterior = 3;
        private const int NightMinute = 22 * 60;
        private const string City = "Veilmouth";
        private const string Tavern = "SootAndBell";
        private const float Deadline = 600f; // s, for one step
        private const float Watchdog = 1200f; // s, for the whole run

        private readonly StringBuilder _report = new StringBuilder();
        private string _file;
        private string _traceFile;
        private GraphicsStateCollection _trace;
        private bool _done;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Application.isEditor || !Debug.isDebugBuild)
            {
                return;
            }

            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, Flag) < 0)
            {
                return;
            }

            // The project's player pauses when its window loses focus (runInBackground off), which would stop the
            // walk halfway; the diagnostic runs on without changing the project's setting.
            Application.runInBackground = true;
            var go = new GameObject("Scene Load Probe");
            DontDestroyOnLoad(go);
            var probe = go.AddComponent<SceneLoadProbe>();
            probe._file = Arg(args, Flag) ?? Path.Combine(Application.persistentDataPath, "scene_load.txt");
            if (Array.IndexOf(args, TraceFlag) >= 0)
            {
                probe._traceFile = Arg(args, TraceFlag) ?? Path.Combine(Application.persistentDataPath, "Veilmouth.graphicsstate");
                probe._trace = new GraphicsStateCollection();
                if (!probe._trace.BeginTrace())
                {
                    Debug.LogError("[SceneLoadProbe] the graphics state trace did not start");
                }
            }

            Application.logMessageReceived += probe.OnLog;
        }

        /// <summary>The value after <paramref name="flag"/>, or null when there is none.</summary>
        private static string Arg(string[] args, string flag)
        {
            var i = Array.IndexOf(args, flag);
            return i >= 0 && i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal) ? args[i + 1] : null;
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
            Application.targetFrameRate = 30; // as the editor's diagnostic
            _report.AppendLine($"Scene load diagnostic in a development build, {DateTime.Now:yyyy-MM-dd HH:mm}, Unity {Application.unityVersion}, " +
                               $"graphics {SystemInfo.graphicsDeviceType}, quality '{QualitySettings.names[QualitySettings.GetQualityLevel()]}', " +
                               $"{Screen.width}x{Screen.height}, the player started {Time.realtimeSinceStartup:0.00} s before");
            yield return null;

            // 1. The city, cold, behind the veil as a journey loads.
            var watch = Stopwatch.StartNew();
            SceneTransition.LoadInTheDark(true);
            var op = SceneManager.LoadSceneAsync(City, LoadSceneMode.Single);
            var read = -1d;
            while (!op.isDone && watch.Elapsed.TotalSeconds < Deadline)
            {
                if (read < 0 && op.progress >= 0.9f)
                {
                    read = watch.Elapsed.TotalSeconds;
                }

                yield return null;
            }

            var awake = watch.Elapsed.TotalSeconds;
            yield return null;
            var first = watch.Elapsed.TotalSeconds;
            SceneTransition.LoadInTheDark(false);
            _report.AppendLine().AppendLine($"## Cold load of {City}: read {Seconds(read)}, awake {Seconds(awake)}, first frame {Seconds(first)} (from the request)");
            Census(SceneManager.GetActiveScene());
            WarmUpLine("at the city's first frame");
            yield return new WaitForSeconds(0.3f);

            // 2. Into the tavern and back twice, the city kept behind it; 3. into a dwelling and back.
            for (var round = 1; round <= 2; round++)
            {
                yield return GoInAndBack("soot-and-bell.street", "soot-and-bell.inside", Tavern);
            }

            var dwelling = Object.FindObjectsByType<Portal>(FindObjectsInactive.Exclude)
                .Where(p => p.Target.SceneName == "Dwelling").OrderBy(p => p.Id, StringComparer.Ordinal).First();
            yield return GoInAndBack(dwelling.Id, dwelling.Id + ".inside", "Dwelling");

            if (_trace != null)
            {
                yield return Tour();
                SaveTrace();
            }

            // The warm-up's summary, once it has finished, and the states it lacked on the walk.
            var wait = Stopwatch.StartNew();
            while (ShaderWarmUp.Collection != null && !ShaderWarmUp.IsDone && wait.Elapsed.TotalSeconds < 120d)
            {
                yield return null;
            }

            _report.AppendLine();
            WarmUpLine("at the end");
            Misses();
            Finish("done");
        }

        private void WarmUpLine(string when)
        {
            var c = ShaderWarmUp.Collection;
            _report.AppendLine(c == null
                ? $"Shader warm-up {when}: none (no collection for {SystemInfo.graphicsDeviceType}, or {ShaderWarmUp.Off})"
                : $"Shader warm-up {when}: {(ShaderWarmUp.IsDone ? $"done in {ShaderWarmUp.Seconds:0.00} s" : "still running")}, " +
                  $"{c.completedWarmupCount:N0} of {c.totalGraphicsStateCount:N0} graphics states, {c.variantCount:N0} shader variants ({c.name}, traced at '{c.qualityLevelName}')");
        }

        /// <summary>The states drawn on the walk that the warmed collection lacked, by shader and pass.</summary>
        private void Misses()
        {
            var miss = ShaderWarmUp.Collection != null ? ShaderWarmUp.Collection.cacheMissCollection : null;
            if (ShaderWarmUp.Collection == null)
            {
                return;
            }

            if (miss == null || miss.variantCount == 0)
            {
                _report.AppendLine("Graphics states missing from the collection on the walk: none");
                return;
            }

            var variants = new List<GraphicsStateCollection.ShaderVariant>();
            miss.GetVariants(variants);
            _report.AppendLine($"Graphics states missing from the collection on the walk: {miss.totalGraphicsStateCount:N0} for {miss.variantCount:N0} shader variants");
            foreach (var g in variants.GroupBy(v => $"{(v.shader != null ? v.shader.name : "?")}, pass {v.passId.SubshaderIndex}.{v.passId.PassIndex}")
                         .OrderByDescending(g => g.Count()).Take(15))
            {
                _report.AppendLine($"  {g.Key,-60} {g.Count(),5} variants");
            }
        }

        /// <summary>
        /// The trace's tour: every street door of the city, looking four ways from head height, and up to
        /// <see cref="DoorsPerInterior"/> doors of each interior the city leads to; then the same at 22:00.
        /// </summary>
        private IEnumerator Tour()
        {
            var tour = Stopwatch.StartNew();
            yield return LookAround("day");
            yield return Interiors("day");
            var clock = GameClockDriver.Running;
            if (clock != null)
            {
                clock.AdvanceTicks((NightMinute - clock.MinuteOfDay + GameClock.MinutesPerDay) % GameClock.MinutesPerDay);
                yield return new WaitForSeconds(0.3f);
                yield return LookAround("night");
                yield return Interiors("night");
            }
            else
            {
                _report.AppendLine("No running clock: the night is not traced");
            }

            _report.AppendLine($"Tour of the trace in {tour.Elapsed.TotalSeconds:0} s");
        }

        private IEnumerator LookAround(string when)
        {
            var doors = CityDoors();
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var cam = Camera.main ?? throw new InvalidOperationException("the city has no main camera");
            // The project's camera scripts hold the camera behind the player; the tour turns it itself, and switched on
            // again the camera snaps behind the player.
            var held = cam.GetComponents<Behaviour>()
                .Where(b => b.enabled && (b.GetType().Namespace ?? string.Empty).StartsWith("GameDev", StringComparison.Ordinal)).ToList();
            held.ForEach(b => b.enabled = false);
            foreach (var door in doors)
            {
                var pose = door.ArrivalPose;
                player.Teleport(pose.position, pose.rotation);
                var yaw = pose.rotation.eulerAngles.y;
                for (var k = 0; k < 4; k++)
                {
                    cam.transform.SetPositionAndRotation(pose.position + Vector3.up * 1.7f, Quaternion.Euler(4f, yaw + 90f * k, 0f));
                    yield return null;
                    yield return null;
                }
            }

            held.ForEach(b => b.enabled = true);
            _report.AppendLine($"Traced by {when}: {doors.Count} street doors, four ways each");
            yield return new WaitForSeconds(0.3f);
        }

        private IEnumerator Interiors(string when)
        {
            var visits = CityDoors().GroupBy(p => p.Target.SceneName).SelectMany(g => g.Take(DoorsPerInterior)).Select(p => p.Id).ToList();
            foreach (var id in visits)
            {
                var street = FindPortal(id);
                var scene = street.Target.SceneName;
                Object.FindAnyObjectByType<PlayerLocomotion>().Teleport(street.ArrivalPose.position, street.ArrivalPose.rotation);
                yield return new WaitForSeconds(0.3f);
                yield return Journey(street.Target, scene, false);
                var way = Object.FindObjectsByType<Portal>(FindObjectsInactive.Exclude)
                    .Where(p => p.gameObject.scene == SceneManager.GetActiveScene() && p.Target.SceneName == City)
                    .OrderBy(p => p.Target.ArrivalPortalId == id ? 0 : 1).ThenBy(p => p.Id, StringComparer.Ordinal).FirstOrDefault();
                if (way == null)
                {
                    throw new InvalidOperationException($"no way back to {City} from {scene}");
                }

                yield return new WaitForSeconds(0.5f);
                yield return Journey(way.Target, City, false);
            }

            _report.AppendLine($"Traced by {when}: {visits.Count} visits to {visits.Select(id => FindPortal(id).Target.SceneName).Distinct().Count()} interiors");
        }

        private static List<Portal> CityDoors() =>
            Object.FindObjectsByType<Portal>(FindObjectsInactive.Exclude)
                .Where(p => p.gameObject.scene == SceneManager.GetActiveScene() && p.Target.IsValid && p.Target.SceneName != City)
                .OrderBy(p => p.Id, StringComparer.Ordinal).ToList();

        private void SaveTrace()
        {
            if (_trace == null || !_trace.isTracing)
            {
                return;
            }

            _trace.EndTrace();
            var folder = Path.GetDirectoryName(Path.GetFullPath(_traceFile));
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var saved = _trace.SaveToFile(_traceFile);
            _report.AppendLine().AppendLine($"Graphics state trace {(saved ? "saved" : "NOT saved")} to {_traceFile}: " +
                                            $"{_trace.totalGraphicsStateCount:N0} graphics states for {_trace.variantCount:N0} shader variants, " +
                                            $"{_trace.graphicsDeviceType}, {_trace.runtimePlatform}, '{_trace.qualityLevelName}'");
        }

        private IEnumerator GoInAndBack(string streetId, string insideId, string interior)
        {
            var pose = FindPortal(streetId).ArrivalPose;
            Object.FindAnyObjectByType<PlayerLocomotion>().Teleport(pose.position, pose.rotation);
            yield return new WaitForSeconds(0.3f);
            yield return Journey(FindPortal(streetId).Target, interior);
            yield return Journey(FindPortal(insideId).Target, City);
        }

        private IEnumerator Journey(TravelRequest request, string scene, bool log = true)
        {
            var watch = Stopwatch.StartNew();
            if (!SceneTransition.Travel(request))
            {
                throw new InvalidOperationException($"the journey to {scene} did not start");
            }

            while ((SceneTransition.IsTraveling || SceneManager.GetActiveScene().name != scene ||
                    SceneTransition.LastJourney.To != scene || SceneTransition.LastJourney.FirstFrame <= 0d) &&
                   watch.Elapsed.TotalSeconds < Deadline)
            {
                yield return null;
            }

            if (log)
            {
                _report.AppendLine().AppendLine($"## Journey {SceneTransition.LastJourney}");
            }

            yield return new WaitForSeconds(0.3f);
        }

        private static Portal FindPortal(string id) =>
            Object.FindObjectsByType<Portal>(FindObjectsInactive.Exclude).Single(p => p.Id == id);

        private void Census(Scene scene)
        {
            int objects = 0, rooms = 0;
            var components = new Dictionary<string, int>();
            var stack = new Stack<(Transform T, bool InRooms)>();
            foreach (var root in scene.GetRootGameObjects())
            {
                stack.Push((root.transform, false));
                while (stack.Count > 0)
                {
                    var (t, inRooms) = stack.Pop();
                    objects++;
                    inRooms |= t.name == "Rooms";
                    rooms += inRooms ? 1 : 0;
                    foreach (var c in t.GetComponents<Component>())
                    {
                        if (c != null && !(c is Transform))
                        {
                            var name = c.GetType().Name;
                            components.TryGetValue(name, out var n);
                            components[name] = n + 1;
                        }
                    }

                    for (var i = t.childCount - 1; i >= 0; i--)
                    {
                        stack.Push((t.GetChild(i), inRooms));
                    }
                }
            }

            _report.AppendLine($"### {scene.name}: {objects:N0} game objects, {rooms:N0} of them in rooms behind windows");
            foreach (var p in components.OrderByDescending(p => p.Value).Take(12))
            {
                _report.AppendLine($"  {p.Key,-28} {p.Value,9:N0}");
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
                SaveTrace();
            }
            catch (Exception e)
            {
                _report.AppendLine($"!! the trace could not be saved: {e.Message}");
            }

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
                Debug.LogError($"[SceneLoadProbe] could not write {_file}: {e.Message}\n{_report}");
            }

            Debug.Log($"[SceneLoadProbe] {how}\n{_report}");
            Application.Quit();
        }

        private static string Seconds(double s) => s < 0 ? "n/a" : $"{s:0.00} s";
    }
}

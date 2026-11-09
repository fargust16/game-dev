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
using UnityEngine;
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
    /// </summary>
    public sealed class SceneLoadProbe : MonoBehaviour
    {
        private const string Flag = "-sceneLoadTiming";
        private const string City = "Veilmouth";
        private const string Tavern = "SootAndBell";
        private const float Deadline = 600f; // s, for one step
        private const float Watchdog = 1200f; // s, for the whole run

        private readonly StringBuilder _report = new StringBuilder();
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

            var go = new GameObject("Scene Load Probe");
            DontDestroyOnLoad(go);
            var probe = go.AddComponent<SceneLoadProbe>();
            probe._file = i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal)
                ? args[i + 1]
                : Path.Combine(Application.persistentDataPath, "scene_load.txt");
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
            yield return new WaitForSeconds(0.3f);

            // 2. Into the tavern and back twice, the city kept behind it; 3. into a dwelling and back.
            for (var round = 1; round <= 2; round++)
            {
                yield return GoInAndBack("soot-and-bell.street", "soot-and-bell.inside", Tavern);
            }

            var dwelling = Object.FindObjectsByType<Portal>(FindObjectsInactive.Exclude)
                .Where(p => p.Target.SceneName == "Dwelling").OrderBy(p => p.Id, StringComparer.Ordinal).First();
            yield return GoInAndBack(dwelling.Id, dwelling.Id + ".inside", "Dwelling");

            Finish("done");
        }

        private IEnumerator GoInAndBack(string streetId, string insideId, string interior)
        {
            var pose = FindPortal(streetId).ArrivalPose;
            Object.FindAnyObjectByType<PlayerLocomotion>().Teleport(pose.position, pose.rotation);
            yield return new WaitForSeconds(0.3f);
            yield return Journey(FindPortal(streetId).Target, interior);
            yield return Journey(FindPortal(insideId).Target, City);
        }

        private IEnumerator Journey(TravelRequest request, string scene)
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

            _report.AppendLine().AppendLine($"## Journey {SceneTransition.LastJourney}");
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

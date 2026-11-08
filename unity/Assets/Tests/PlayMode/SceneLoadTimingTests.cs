#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using GameDev.Core.Scenes;
using GameDev.Locomotion;
using GameDev.World.Portals;
using NUnit.Framework;
using UnityEditor.Profiling;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// A diagnostic, not an acceptance test (owner, 2026-11-08: every door took about a minute): Veilmouth loads cold,
    /// the player goes into the Soot &amp; Bell and back, and each step is timed, the scenes counted, and the longest
    /// frames of each journey broken down by the profiler's markers. The report goes to Logs/SceneLoad. It runs only
    /// when named: <c>-testFilter GameDev.Slice.Tests.SceneLoadTimingTests</c>, best with graphics.
    /// </summary>
    [Explicit("diagnostic: run by name"), Category("Diagnostic")]
    public sealed class SceneLoadTimingTests : SliceInputTestBase
    {
        private const string CityPath = "Assets/Scenes/Slice/Veilmouth.unity";
        private const string City = "Veilmouth";
        private const string Tavern = "SootAndBell";
        private const float Deadline = 600f; // s, for one step

        private readonly StringBuilder _report = new();

        [UnityTest, Timeout(3600000)]
        public IEnumerator TheCityLoadsAndThePlayerGoesIntoTheTavernAndBack()
        {
            // A batch run draws thousands of frames a second and the profiler keeps a few hundred: slow the frames.
            var savedRate = Application.targetFrameRate;
            Application.targetFrameRate = 30;
            _report.AppendLine($"Scene load diagnostic, {System.DateTime.Now:yyyy-MM-dd HH:mm}, Unity {Application.unityVersion}, " +
                               $"graphics {SystemInfo.graphicsDeviceType}, quality '{QualitySettings.names[QualitySettings.GetQualityLevel()]}', " +
                               $"profiler {(Profile ? "on (times are inflated)" : "off")}");

            // 1. The city, cold.
            Debug.Log($"[SceneLoadTiming] start at {Time.realtimeSinceStartup:0.0} s");
            StartProfiling();
            var watch = Stopwatch.StartNew();
            SceneTransition.LoadInTheDark(true); // as a journey loads, behind the veil
            var op = EditorSceneManager.LoadSceneAsyncInPlayMode(CityPath, new LoadSceneParameters(LoadSceneMode.Single));
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
            _report.AppendLine();
            _report.AppendLine($"## Cold load of {City}: read {Seconds(read)}, awake {Seconds(awake)}, first frame {Seconds(first)} (from the request)");
            Debug.Log($"[SceneLoadTiming] cold load: read {Seconds(read)}, awake {Seconds(awake)}, first frame {Seconds(first)}");
            StopProfiling("cold load");
            Debug.Log("[SceneLoadTiming] profiled the cold load");
            Census(SceneManager.GetActiveScene());

            // 2. Into the tavern through its street door.
            var street = Object.FindObjectsByType<Portal>(FindObjectsInactive.Exclude).Single(p => p.Id == "soot-and-bell.street");
            var pose = street.ArrivalPose;
            Object.FindAnyObjectByType<PlayerLocomotion>().Teleport(pose.position, pose.rotation);
            yield return new WaitForSeconds(0.3f);
            yield return Journey(street.Target, Tavern);
            Census(SceneManager.GetActiveScene());

            // 3. Back to the street.
            var inside = Object.FindObjectsByType<Portal>(FindObjectsInactive.Exclude).Single(p => p.Id == "soot-and-bell.inside");
            yield return Journey(inside.Target, City);

            Application.targetFrameRate = savedRate;
            var folder = Path.Combine(Application.dataPath, "..", "Logs", "SceneLoad");
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, $"scene_load_{System.DateTime.Now:yyyyMMdd-HHmmss}.txt");
            File.WriteAllText(file, _report.ToString());
            Debug.Log($"[SceneLoadTiming] report written to {file}\n{_report}");
        }

        private IEnumerator Journey(TravelRequest request, string scene)
        {
            StartProfiling();
            var watch = Stopwatch.StartNew();
            Assert.IsTrue(SceneTransition.Travel(request), $"the journey to {scene} starts");
            while ((SceneTransition.IsTraveling || SceneManager.GetActiveScene().name != scene ||
                    SceneTransition.LastJourney.To != scene || SceneTransition.LastJourney.FirstFrame <= 0d) &&
                   watch.Elapsed.TotalSeconds < Deadline)
            {
                yield return null;
            }

            Assert.AreEqual(scene, SceneManager.GetActiveScene().name, "the journey arrived");
            _report.AppendLine();
            _report.AppendLine($"## Journey {SceneTransition.LastJourney}");
            Debug.Log($"[SceneLoadTiming] journey {SceneTransition.LastJourney}");
            StopProfiling($"journey to {scene}");
            yield return new WaitForSeconds(0.3f);
        }

        // ------------------------------------------------------------------ what the scene holds

        private void Census(Scene scene)
        {
            var watch = Stopwatch.StartNew();
            var byRoot = new List<(string Name, int Count)>();
            var rooms = 0;
            var inactive = 0;
            var transforms = 0;
            var components = new Dictionary<string, (int All, int Off)>();
            var stack = new Stack<(Transform T, bool InRooms)>();
            foreach (var root in scene.GetRootGameObjects())
            {
                var before = transforms;
                stack.Push((root.transform, false));
                while (stack.Count > 0)
                {
                    var (t, inRooms) = stack.Pop();
                    transforms++;
                    inRooms |= t.name == "Rooms";
                    if (inRooms)
                    {
                        rooms++;
                    }

                    if (!t.gameObject.activeSelf)
                    {
                        inactive++;
                    }

                    foreach (var c in t.GetComponents<Component>())
                    {
                        if (c == null || c is Transform)
                        {
                            continue;
                        }

                        var name = c.GetType().Name;
                        var off = c is Behaviour b ? !b.enabled : c is Renderer r ? !r.enabled : c is Collider k && !k.enabled;
                        components.TryGetValue(name, out var n);
                        components[name] = (n.All + 1, n.Off + (off ? 1 : 0));
                    }

                    for (var i = t.childCount - 1; i >= 0; i--)
                    {
                        stack.Push((t.GetChild(i), inRooms));
                    }
                }

                byRoot.Add((root.name, transforms - before));
            }

            _report.AppendLine();
            _report.AppendLine($"### {scene.name}: {transforms:N0} game objects ({inactive:N0} turned off themselves), " +
                               $"{rooms:N0} of them in rooms behind windows; {scene.GetRootGameObjects().Length} roots (counted in {watch.Elapsed.TotalSeconds:0.0} s)");
            _report.AppendLine("Components, most first (count, of which turned off):");
            foreach (var (name, n) in components.OrderByDescending(p => p.Value.All).Take(24))
            {
                _report.AppendLine($"  {name,-28} {n.All,9:N0}  {n.Off,9:N0}");
            }

            _report.AppendLine("Largest roots (game objects):");
            foreach (var (name, count) in byRoot.OrderByDescending(r => r.Count).Take(12))
            {
                _report.AppendLine($"  {count,9:N0}  {name}");
            }

            var houses = byRoot.Where(r => r.Name.StartsWith("Kit_House")).ToList();
            if (houses.Count > 0)
            {
                _report.AppendLine($"Kit houses: {houses.Count} roots, {houses.Sum(h => h.Count):N0} game objects");
            }

            Debug.Log($"[SceneLoadTiming] census of {scene.name}: {transforms:N0} game objects, {rooms:N0} in rooms, in {watch.Elapsed.TotalSeconds:0.0} s");
        }

        // ------------------------------------------------------------------ the profiler

        /// <summary>The profiler slows a load: it runs only with <c>-sceneLoadProfile</c> on the command line, for the breakdown.</summary>
        private static readonly bool Profile = System.Environment.GetCommandLineArgs().Contains("-sceneLoadProfile");

        private static void StartProfiling()
        {
            if (!Profile)
            {
                return;
            }

            ProfilerDriver.ClearAllFrames();
            ProfilerDriver.profileEditor = false;
            ProfilerDriver.enabled = true;
        }

        /// <summary>The three longest main-thread frames since <see cref="StartProfiling"/>, each as a tree of its heavy markers.</summary>
        private void StopProfiling(string label)
        {
            if (!Profile)
            {
                return;
            }

            ProfilerDriver.enabled = false;
            var first = ProfilerDriver.firstFrameIndex;
            var last = ProfilerDriver.lastFrameIndex;
            if (first < 0 || last < first)
            {
                _report.AppendLine($"(the profiler recorded no frames for the {label})");
                return;
            }

            var frames = new List<(int Index, float Ms)>();
            for (var f = first; f <= last; f++)
            {
                using var view = ProfilerDriver.GetHierarchyFrameDataView(f, 0, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                    HierarchyFrameDataView.columnTotalTime, false);
                if (view.valid)
                {
                    frames.Add((f, view.frameTimeMs));
                }
            }

            _report.AppendLine($"Profiler, {label}: {frames.Count} frames, {frames.Sum(f => f.Ms) / 1000f:0.00} s in all; the longest:");
            foreach (var (index, ms) in frames.OrderByDescending(f => f.Ms).Take(3))
            {
                _report.AppendLine($"  frame {index}: {ms / 1000f:0.000} s");
                using var view = ProfilerDriver.GetHierarchyFrameDataView(index, 0, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                    HierarchyFrameDataView.columnTotalTime, false);
                Tree(view, view.GetRootItemID(), 2, Mathf.Max(15f, ms * 0.02f), 0);
            }

            ProfilerDriver.ClearAllFrames();
        }

        private void Tree(HierarchyFrameDataView view, int item, int indent, float floorMs, int depth)
        {
            if (depth > 9)
            {
                return;
            }

            var children = new List<int>();
            view.GetItemChildren(item, children);
            foreach (var child in children)
            {
                var total = view.GetItemColumnDataAsFloat(child, HierarchyFrameDataView.columnTotalTime);
                if (total < floorMs)
                {
                    continue;
                }

                var self = view.GetItemColumnDataAsFloat(child, HierarchyFrameDataView.columnSelfTime);
                var calls = view.GetItemColumnDataAsFloat(child, HierarchyFrameDataView.columnCalls);
                _report.AppendLine($"{new string(' ', indent * 2)}{total / 1000f,7:0.000} s  (self {self / 1000f:0.000}, calls {calls:0})  {view.GetItemName(child)}");
                Tree(view, child, indent + 1, floorMs, depth + 1);
            }
        }

        private static string Seconds(double s) => s < 0 ? "n/a" : $"{s:0.00} s";
    }
}
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace GameDev.Core.Scenes
{
    /// <summary>
    /// Scene transition (17 I.2) behind a gate that refuses a second journey while one is under way. The journey
    /// happens behind <see cref="ScreenFade"/>: the screen darkens, the scenes change in the dark, and the veil lifts
    /// once the player and the camera stand in the new scene. Objects that stay alive through it (the clock) keep
    /// running, so travelling costs only the real time the change takes. <see cref="Arriving"/> is raised once the new
    /// scene's objects are awake and enabled, before any of them starts, so a portal can place the player before the
    /// camera first looks at it.
    /// <para>
    /// <b>The scene left for an interior stays loaded</b> (owner, 2026-11-08, reference plan R1, D3 (a)): its roots are
    /// switched off, the interior loads beside it, and coming back unloads the interior and switches the kept scene on
    /// again, so a city is never destroyed and loaded again at a door. A scene is an interior when it says so through
    /// <see cref="MarkInterior"/>. Leaving an interior for a scene that is not kept is the old single-scene swap, and
    /// at most one scene is kept at a time.
    /// </para>
    /// <para>
    /// Every journey is timed and logged (owner, 2026-11-08: a door took about a minute): the darkening, the change up
    /// to the new scene's Awake and OnEnable, and its first drawn frame, which includes every Start.
    /// </para>
    /// </summary>
    public static class SceneTransition
    {
        private static readonly Stopwatch Watch = new();
        private static readonly HashSet<Scene> Interiors = new();
        private static readonly List<GameObject> KeptRoots = new();
        private static Scene _kept;
        private static TravelRequest _current;
        private static string _from;
        private static string _way;
        private static double _dark;
        private static double _loaded;

        /// <summary>True from <see cref="Travel"/> until the destination scene has arrived.</summary>
        public static bool IsTraveling { get; private set; }

        /// <summary>The last finished journey's times, set on its first drawn frame.</summary>
        public static JourneyTimes LastJourney { get; private set; }

        /// <summary>The scene kept loaded, switched off, while the traveller is in an interior; invalid when none.</summary>
        public static Scene KeptScene => _kept.IsValid() && _kept.isLoaded ? _kept : default;

        /// <summary>Raised in the new scene, before Start, with the request that brought the traveller.</summary>
        public static event Action<TravelRequest> Arriving;

        /// <summary>Raised after <see cref="Arriving"/>, once the journey is over.</summary>
        public static event Action<TravelRequest> TravelCompleted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            LoadInTheDark(false);
            IsTraveling = false;
            _current = default;
            _kept = default;
            KeptRoots.Clear();
            Interiors.Clear();
            LastJourney = default;
            ScreenFade.Reset();
            Arriving = null;
            TravelCompleted = null;
        }

        /// <summary>
        /// An interior calls this as it wakes: its scene is unloaded when the traveller leaves it, and the scene the
        /// traveller came from is kept behind it.
        /// </summary>
        public static void MarkInterior(Scene scene)
        {
            if (scene.IsValid())
            {
                Interiors.Add(scene);
            }
        }

        /// <summary>Starts the journey; false when one is already under way or the request is incomplete.</summary>
        public static bool Travel(TravelRequest request)
        {
            if (IsTraveling)
            {
                return false;
            }

            if (!request.IsValid)
            {
                Debug.LogError($"Travel request '{request}' needs a scene and an arrival portal.");
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(request.SceneName))
            {
                Debug.LogError($"Scene '{request.SceneName}' is not in Build Settings.");
                return false;
            }

            IsTraveling = true;
            _current = request;
            _from = SceneManager.GetActiveScene().name;
            Watch.Restart();
            ScreenFade.Out(() =>
            {
                _dark = Watch.Elapsed.TotalSeconds;
                LoadInTheDark(true);
                Go(request.SceneName);
            });
            return true;
        }

        private static ThreadPriority _savedPriority = ThreadPriority.BelowNormal;
        private static bool _loadingInTheDark;

        /// <summary>
        /// Behind the veil nobody sees the frame rate, so the load takes the main thread: Unity's default priority,
        /// BelowNormal, gives a loading scene about 4 ms a frame, and Veilmouth's 150 000 objects then took about 1 400
        /// frames, some 50 s (measured 2026-11-08, SceneLoadTimingTests). High gives it about 50 ms a frame.
        /// </summary>
        public static void LoadInTheDark(bool on)
        {
            if (on == _loadingInTheDark)
            {
                return;
            }

            _loadingInTheDark = on;
            if (on)
            {
                _savedPriority = Application.backgroundLoadingPriority;
                Application.backgroundLoadingPriority = ThreadPriority.High;
            }
            else
            {
                Application.backgroundLoadingPriority = _savedPriority;
            }
        }

        private static void Go(string sceneName)
        {
            var from = SceneManager.GetActiveScene();
            if (KeptScene.IsValid() && KeptScene.name == sceneName)
            {
                _way = "kept scene switched on";
                ComeBack();
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            if (!from.IsValid() || Interiors.Contains(from))
            {
                // Out of an interior to a scene not kept: the single swap, which also unloads a kept scene.
                _way = "single swap";
                Forget();
                SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                return;
            }

            var watch = Stopwatch.StartNew();
            Keep(from);
            _way = $"loaded beside {from.name}, switched off in {watch.Elapsed.TotalSeconds:0.00} s";
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }

        /// <summary>Switches <paramref name="scene"/>'s roots off and keeps it, unloading a scene kept before.</summary>
        private static void Keep(Scene scene)
        {
            if (KeptScene.IsValid() && KeptScene != scene)
            {
                SceneManager.UnloadSceneAsync(KeptScene);
            }

            KeptRoots.Clear();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.activeSelf)
                {
                    root.SetActive(false);
                    KeptRoots.Add(root);
                }
            }

            _kept = scene;
        }

        private static void Forget()
        {
            _kept = default;
            KeptRoots.Clear();
        }

        /// <summary>Unloads every scene but the kept one, then switches it on and arrives there.</summary>
        private static void ComeBack()
        {
            var leaving = new List<Scene>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene != _kept && scene.isLoaded)
                {
                    leaving.Add(scene);
                }
            }

            // One more than the scenes, released last, so a scene unloaded at once cannot switch on before the rest.
            var pending = leaving.Count + 1;
            void OneDone()
            {
                if (--pending == 0)
                {
                    SwitchOn();
                }
            }

            foreach (var scene in leaving)
            {
                Interiors.Remove(scene);
                var op = SceneManager.UnloadSceneAsync(scene);
                if (op == null)
                {
                    OneDone();
                }
                else
                {
                    op.completed += _ => OneDone();
                }
            }

            OneDone();
        }

        private static void SwitchOn()
        {
            var scene = _kept;
            var watch = Stopwatch.StartNew();
            foreach (var root in KeptRoots)
            {
                if (root != null)
                {
                    root.SetActive(true);
                }
            }

            Forget();
            SceneManager.SetActiveScene(scene);

            // The arriving portal looks for the ground under its door, which the physics scene knows only once synced.
            Physics.SyncTransforms();
            _way += $" in {watch.Elapsed.TotalSeconds:0.00} s";
            Arrive(scene.name);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != _current.SceneName)
            {
                return;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (mode == LoadSceneMode.Additive)
            {
                // The interior's light and sky are the active scene's.
                SceneManager.SetActiveScene(scene);
            }

            Arrive(scene.name);
        }

        private static void Arrive(string sceneName)
        {
            _loaded = Watch.Elapsed.TotalSeconds;
            var request = _current;
            _current = default;
            IsTraveling = false;
            Arriving?.Invoke(request);
            TravelCompleted?.Invoke(request);
            ScreenFade.In();
            LastJourney = new JourneyTimes(_from, sceneName, _dark, _loaded - _dark, 0d, _way);
            ScreenFade.NextFrame(OnFirstFrame);
        }

        private static void OnFirstFrame()
        {
            LoadInTheDark(false);
            var total = Watch.Elapsed.TotalSeconds;
            var last = LastJourney;
            LastJourney = new JourneyTimes(last.From, last.To, last.Dark, last.Load, total - _loaded, last.Way);
            Debug.Log($"[SceneTransition] {LastJourney}");
        }
    }

    /// <summary>
    /// One journey's real times, s: the screen darkening, the change to Awake and OnEnable, the first frame; and how
    /// the scenes changed.
    /// </summary>
    public readonly struct JourneyTimes
    {
        public readonly string From;
        public readonly string To;
        public readonly double Dark;
        public readonly double Load;
        public readonly double FirstFrame;
        public readonly string Way;

        public JourneyTimes(string from, string to, double dark, double load, double firstFrame, string way = null)
        {
            From = from;
            To = to;
            Dark = dark;
            Load = load;
            FirstFrame = firstFrame;
            Way = way;
        }

        public double Total => Dark + Load + FirstFrame;

        public override string ToString() =>
            $"{From} -> {To}: darkening {Dark:0.00} s, load {Load:0.00} s, first frame {FirstFrame:0.00} s, total {Total:0.00} s" +
            (string.IsNullOrEmpty(Way) ? string.Empty : $" ({Way})");
    }
}

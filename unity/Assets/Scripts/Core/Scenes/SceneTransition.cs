using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace GameDev.Core.Scenes
{
    /// <summary>
    /// V0 scene transition (17 I.2): a single-scene swap behind a gate that refuses a second journey while one loads.
    /// Since 2026-11-05 the swap happens behind <see cref="ScreenFade"/>: the screen darkens, the scene loads in the
    /// dark, and the veil lifts once the player and the camera stand in the new scene.
    /// Objects that stay alive through the swap (the clock) keep running, so travelling costs only the real time the
    /// load takes. <see cref="Arriving"/> is raised once the new scene's objects are awake and enabled, before any of
    /// them starts, so a portal can place the player before the camera first looks at it.
    /// <para>
    /// Every journey is timed and logged (owner, 2026-11-08: a door took about a minute): the darkening, the load up
    /// to the new scene's Awake and OnEnable, and its first drawn frame, which includes every Start.
    /// </para>
    /// </summary>
    public static class SceneTransition
    {
        private static readonly Stopwatch Watch = new();
        private static TravelRequest _current;
        private static string _from;
        private static double _dark;
        private static double _loaded;

        /// <summary>True from <see cref="Travel"/> until the destination scene has loaded.</summary>
        public static bool IsTraveling { get; private set; }

        /// <summary>The last finished journey's times, set on its first drawn frame.</summary>
        public static JourneyTimes LastJourney { get; private set; }

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
            LastJourney = default;
            ScreenFade.Reset();
            Arriving = null;
            TravelCompleted = null;
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
                SceneManager.sceneLoaded += OnSceneLoaded;
                LoadInTheDark(true);
                SceneManager.LoadSceneAsync(request.SceneName, LoadSceneMode.Single);
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

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != _current.SceneName)
            {
                return;
            }

            _loaded = Watch.Elapsed.TotalSeconds;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            var request = _current;
            _current = default;
            IsTraveling = false;
            Arriving?.Invoke(request);
            TravelCompleted?.Invoke(request);
            ScreenFade.In();
            LastJourney = new JourneyTimes(_from, request.SceneName, _dark, _loaded - _dark, 0d);
            ScreenFade.NextFrame(OnFirstFrame);
        }

        private static void OnFirstFrame()
        {
            LoadInTheDark(false);
            var total = Watch.Elapsed.TotalSeconds;
            LastJourney = new JourneyTimes(LastJourney.From, LastJourney.To, _dark, _loaded - _dark, total - _loaded);
            Debug.Log($"[SceneTransition] {LastJourney}");
        }
    }

    /// <summary>One journey's real times, s: the screen darkening, the load to Awake and OnEnable, the first frame.</summary>
    public readonly struct JourneyTimes
    {
        public readonly string From;
        public readonly string To;
        public readonly double Dark;
        public readonly double Load;
        public readonly double FirstFrame;

        public JourneyTimes(string from, string to, double dark, double load, double firstFrame)
        {
            From = from;
            To = to;
            Dark = dark;
            Load = load;
            FirstFrame = firstFrame;
        }

        public double Total => Dark + Load + FirstFrame;

        public override string ToString() =>
            $"{From} -> {To}: darkening {Dark:0.00} s, load {Load:0.00} s, first frame {FirstFrame:0.00} s, total {Total:0.00} s";
    }
}

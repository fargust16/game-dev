using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDev.Core.Scenes
{
    /// <summary>
    /// V0 scene transition (17 I.2): a single-scene swap behind a gate that refuses a second journey while one loads.
    /// Since 2026-11-05 the swap happens behind <see cref="ScreenFade"/>: the screen darkens, the scene loads in the
    /// dark, and the veil lifts once the player and the camera stand in the new scene.
    /// Objects that stay alive through the swap (the clock) keep running, so travelling costs only the real time the
    /// load takes. <see cref="Arriving"/> is raised once the new scene's objects are awake and enabled, before any of
    /// them starts, so a portal can place the player before the camera first looks at it.
    /// </summary>
    public static class SceneTransition
    {
        private static TravelRequest _current;

        /// <summary>True from <see cref="Travel"/> until the destination scene has loaded.</summary>
        public static bool IsTraveling { get; private set; }

        /// <summary>Raised in the new scene, before Start, with the request that brought the traveller.</summary>
        public static event Action<TravelRequest> Arriving;

        /// <summary>Raised after <see cref="Arriving"/>, once the journey is over.</summary>
        public static event Action<TravelRequest> TravelCompleted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            IsTraveling = false;
            _current = default;
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
            ScreenFade.Out(() =>
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                SceneManager.LoadSceneAsync(request.SceneName, LoadSceneMode.Single);
            });
            return true;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != _current.SceneName)
            {
                return;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            var request = _current;
            _current = default;
            IsTraveling = false;
            Arriving?.Invoke(request);
            TravelCompleted?.Invoke(request);
            ScreenFade.In();
        }
    }
}

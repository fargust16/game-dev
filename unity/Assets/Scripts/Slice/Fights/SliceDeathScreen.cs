using System;
using GameDev.Core.Localization;
using GameDev.Input;
using GameDev.World.Timekeeping;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// S3a.9: the character has died, and the run is over (10.2 §9, owner-approved). Until S5c's save, a plain screen
    /// says so, the world waiting behind it, and Enter (or Space) starts the slice again from its beginning: the running world
    /// and clock are let go, and Veilmouth loads anew (plan, S3 authoring default). The screen goes with its world if
    /// that is let go some other way.
    /// </summary>
    public sealed class SliceDeathScreen : MonoBehaviour
    {
        /// <summary>The scene the slice begins in.</summary>
        public const string StartScene = "Veilmouth";

        private static SliceDeathScreen _shown;
        private IDisposable _pause;
        private SliceWorldDriver _world;
        private bool _restarting;
        private GUIStyle _title;
        private GUIStyle _hint;

        public static bool Shown => _shown != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _shown = null;

        public static void Show(IWorldClock clock)
        {
            if (_shown != null)
            {
                return;
            }

            var go = new GameObject("Death screen");
            DontDestroyOnLoad(go);
            _shown = go.AddComponent<SliceDeathScreen>();
            _shown._pause = clock?.Pause();
            _shown._world = FindAnyObjectByType<SliceWorldDriver>();
        }

        /// <summary>Starts the slice again from its beginning.</summary>
        public void Restart()
        {
            if (_restarting)
            {
                return;
            }

            _restarting = true;
            _pause?.Dispose();
            _pause = null;
            foreach (var world in FindObjectsByType<SliceWorldDriver>(FindObjectsSortMode.None))
            {
                Destroy(world.gameObject);
            }

            foreach (var clock in FindObjectsByType<GameClockDriver>(FindObjectsSortMode.None))
            {
                Destroy(clock.gameObject);
            }

            SceneManager.LoadScene(StartScene, LoadSceneMode.Single);
            Destroy(gameObject);
        }

        private void Update()
        {
            if (!_restarting && _world == null)
            {
                Destroy(gameObject); // its world was let go: a test, or a new game
                return;
            }

            var input = GameInputService.Instance;
            if (input != null && input.EndTurnPressed)
            {
                Restart();
            }
        }

        private void OnDestroy()
        {
            _pause?.Dispose();
            if (_shown == this)
            {
                _shown = null;
            }
        }

        private void OnGUI()
        {
            GUI.depth = -1000;
            var screen = new Rect(0f, 0f, Screen.width, Screen.height);
            GUI.color = Color.black;
            GUI.DrawTexture(screen, Texture2D.whiteTexture);
            GUI.color = Color.white;
            var scale = Screen.height / 1080f;
            _title ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            _hint ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            _title.fontSize = Mathf.RoundToInt(48f * scale);
            _hint.fontSize = Mathf.RoundToInt(22f * scale);
            _title.normal.textColor = new Color(0.86f, 0.82f, 0.74f);
            _hint.normal.textColor = new Color(0.6f, 0.58f, 0.54f);
            GUI.Label(new Rect(0f, Screen.height * 0.4f, Screen.width, 80f * scale), Loc.Get("ui.death.title"), _title);
            GUI.Label(new Rect(0f, Screen.height * 0.55f, Screen.width, 40f * scale), Loc.Get("ui.death.restart"), _hint);
        }
    }
}

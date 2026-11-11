using System;
using GameDev.Core.Localization;
using GameDev.Input;
using GameDev.World.Timekeeping;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// S3a.9: the character has died, and the run is over (10.2 §9, owner-approved). Until S5c's save, a translucent
    /// panel over the world says so in large letters, the world waiting behind it, with two choices (owner's play test
    /// of 2026-11-11, which replaced the black screen): Start again, which lets the running world and clock go and loads
    /// Veilmouth anew, and Quit the game. A click picks one, and Enter starts again; no key quits. The panel stays until the
    /// new scene has loaded, saying that it loads, so the old world never shows frozen without it. A loading screen of
    /// its own comes later. The panel goes with its world if that is let go some other way.
    /// </summary>
    public sealed class SliceDeathScreen : MonoBehaviour
    {
        /// <summary>The scene the slice begins in.</summary>
        public const string StartScene = "Veilmouth";

        // Authoring defaults: the dimming over the world, the panel's colour and size as shares of the screen's height.
        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.45f);
        private static readonly Color PanelColour = new Color(0.07f, 0.06f, 0.05f, 0.82f);
        private static readonly Color Edge = new Color(0.47f, 0.41f, 0.31f, 1f);
        private static readonly Color TitleColour = new Color(0.86f, 0.80f, 0.70f);
        private static readonly Color ButtonColour = new Color(0.16f, 0.13f, 0.10f, 0.95f);
        private static readonly Color ButtonHover = new Color(0.30f, 0.23f, 0.14f, 0.95f);

        private static SliceDeathScreen _shown;
        private IDisposable _pause;
        private SliceWorldDriver _world;
        private bool _restarting;
        private GUIStyle _title;
        private GUIStyle _button;
        private GUIStyle _note;

        public static bool Shown => _shown != null;

        /// <summary>True from Start again until the new scene has loaded.</summary>
        public bool Restarting => _restarting;

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
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>Starts the slice again from its beginning; the panel stays until the scene has loaded.</summary>
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

            var loading = SceneManager.LoadSceneAsync(StartScene, LoadSceneMode.Single);
            if (loading == null)
            {
                Destroy(gameObject);
                return;
            }

            loading.completed += _ =>
            {
                if (this != null)
                {
                    Destroy(gameObject);
                }
            };
        }

        /// <summary>Leaves the game; in the editor, leaves Play Mode.</summary>
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Update()
        {
            if (_restarting)
            {
                return;
            }

            if (_world == null)
            {
                Destroy(gameObject); // its world was let go: a test, or a new game
                return;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
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
            var w = Screen.width;
            var h = Screen.height;
            var scale = h / 1080f;
            Fill(new Rect(0f, 0f, w, h), Dim);

            var panel = new Rect((w - (760f * scale)) / 2f, h * 0.30f, 760f * scale, 360f * scale);
            Fill(new Rect(panel.x - 1f, panel.y - 1f, panel.width + 2f, panel.height + 2f), Edge);
            Fill(panel, PanelColour);

            _title ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _note ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            _button ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            _title.fontSize = Mathf.RoundToInt(84f * scale);
            _title.normal.textColor = TitleColour;
            _note.fontSize = Mathf.RoundToInt(24f * scale);
            _note.normal.textColor = new Color(0.66f, 0.62f, 0.55f);
            _button.fontSize = Mathf.RoundToInt(28f * scale);
            _button.normal.textColor = TitleColour;
            GUI.Label(new Rect(panel.x, panel.y + (40f * scale), panel.width, 120f * scale), Loc.Get("ui.death.title"), _title);

            if (_restarting)
            {
                GUI.Label(new Rect(panel.x, panel.y + (220f * scale), panel.width, 60f * scale), Loc.Get("ui.death.loading"), _note);
                return;
            }

            var size = new Vector2(300f * scale, 64f * scale);
            var top = panel.y + (210f * scale);
            var gap = 40f * scale;
            var left = panel.x + ((panel.width - (size.x * 2f) - gap) / 2f);
            if (Button(new Rect(left, top, size.x, size.y), Loc.Get("ui.death.restart")))
            {
                Restart();
            }

            if (Button(new Rect(left + size.x + gap, top, size.x, size.y), Loc.Get("ui.death.quit")))
            {
                Quit();
            }
        }

        private bool Button(Rect rect, string text)
        {
            var hover = rect.Contains(Event.current.mousePosition);
            Fill(new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, rect.height + 2f), Edge);
            Fill(rect, hover ? ButtonHover : ButtonColour);
            GUI.Label(rect, text, _button);
            return Event.current.type == EventType.MouseUp && Event.current.button == 0 && hover;
        }

        private static void Fill(Rect rect, Color colour)
        {
            var was = GUI.color;
            GUI.color = colour;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = was;
        }
    }
}

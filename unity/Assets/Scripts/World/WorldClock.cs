using GameDev.Input;
using GameDev.Interaction;
using UnityEngine;

namespace GameDev.World
{
    /// <summary>
    /// Player world-time clock. Active Windows tick only in this time.
    /// Design: 04 Points of Interest.
    /// </summary>
    public sealed class WorldClock : MonoBehaviour
    {
        public static WorldClock Instance { get; private set; }

        [SerializeField] private float worldHours = 9f;
        [Tooltip("Realtime seconds that equal one in-world hour.")]
        [SerializeField] private float realtimeSecondsPerWorldHour = 45f;
        [SerializeField] private bool isPaused;

        private GUIStyle _style;
        private float _savedTimeScale = 1f;

        public float WorldHours => worldHours;
        public int DayIndex => Mathf.FloorToInt(worldHours / 24f) + 1;
        public float HourOfDay => worldHours % 24f;
        public bool IsPaused => isPaused;

        private void OnEnable()
        {
            GameInputService.EnsureExists();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ApplyPauseState(announce: false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            Time.timeScale = 1f;
        }

        private void Update()
        {
            var input = GameInputService.Instance;

            // Pause toggle always listens (works even at timeScale 0).
            if (input != null && input.PausePressed)
            {
                SetPaused(!isPaused);
            }

            if (isPaused)
            {
                return;
            }

            if (realtimeSecondsPerWorldHour > 0.01f)
            {
                worldHours += Time.unscaledDeltaTime / realtimeSecondsPerWorldHour;
            }

            if (input == null)
            {
                return;
            }

            // Debug: jump world hours while unpaused.
            if (input.AdvanceHourPressed)
            {
                AdvanceHours(1f);
            }

            if (input.RewindHourPressed)
            {
                AdvanceHours(-1f);
            }
        }

        public void SetPaused(bool paused)
        {
            if (isPaused == paused)
            {
                ApplyPauseState(announce: false);
                return;
            }

            isPaused = paused;
            ApplyPauseState(announce: true);
        }

        public void AdvanceHours(float hours)
        {
            if (isPaused)
            {
                return;
            }

            worldHours = Mathf.Max(0f, worldHours + hours);
            Debug.Log($"[WorldClock] Day {DayIndex}, {HourOfDay:0.0}h (total {worldHours:0.0}h)");
        }

        /// <summary>
        /// Jump forward to the next occurrence of hourOfDay (0–24).
        /// </summary>
        public void SkipToHourOfDay(float hourOfDay)
        {
            if (isPaused)
            {
                return;
            }

            hourOfDay = Mathf.Repeat(hourOfDay, 24f);
            var current = HourOfDay;
            var delta = hourOfDay - current;
            if (delta <= 0.05f)
            {
                delta += 24f;
            }

            AdvanceHours(delta);
        }

        public string FormatStatus()
        {
            var h = Mathf.FloorToInt(HourOfDay);
            var m = Mathf.FloorToInt((HourOfDay - h) * 60f);
            if (isPaused)
            {
                return $"Day {DayIndex}  {h:00}:{m:00}  PAUSED";
            }

            return $"Day {DayIndex}  {h:00}:{m:00}";
        }

        private void ApplyPauseState(bool announce)
        {
            if (isPaused)
            {
                if (Time.timeScale > 0f)
                {
                    _savedTimeScale = Time.timeScale;
                }

                Time.timeScale = 0f;
                if (announce)
                {
                    InteractionFeedback.Show("Paused", "World frozen. Press P to resume. Movement and interacts are locked.", 3f);
                }
            }
            else
            {
                Time.timeScale = _savedTimeScale > 0f ? _savedTimeScale : 1f;
                if (announce)
                {
                    InteractionFeedback.Show("Resumed", "World time and controls are active again.", 2.5f);
                }
            }
        }

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(10, 10, 6, 6)
                };
                _style.normal.textColor = Color.white;
            }

            // Clean clock only — hotkeys live in legend (H), not in the time string.
            GUI.Box(new Rect(12f, 12f, 200f, 28f), FormatStatus(), _style);
        }
    }
}

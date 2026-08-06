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

        [SerializeField] private float worldHours;
        [Tooltip("Realtime seconds that equal one in-world hour.")]
        [SerializeField] private float realtimeSecondsPerWorldHour = 8f;
        [SerializeField] private bool pauseClock;

        private GUIStyle _style;

        public float WorldHours => worldHours;
        public int DayIndex => Mathf.FloorToInt(worldHours / 24f) + 1;
        public float HourOfDay => worldHours % 24f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            if (!pauseClock && realtimeSecondsPerWorldHour > 0.01f)
            {
                worldHours += Time.deltaTime / realtimeSecondsPerWorldHour;
            }

            // Debug: advance / rewind world time for S1 playtests.
            if (Input.GetKeyDown(KeyCode.RightBracket) || Input.GetKeyDown(KeyCode.Equals))
            {
                AdvanceHours(1f);
            }

            if (Input.GetKeyDown(KeyCode.LeftBracket) || Input.GetKeyDown(KeyCode.Minus))
            {
                AdvanceHours(-1f);
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                pauseClock = !pauseClock;
            }
        }

        public void AdvanceHours(float hours)
        {
            worldHours = Mathf.Max(0f, worldHours + hours);
            Debug.Log($"[WorldClock] Day {DayIndex}, {HourOfDay:0.0}h (total {worldHours:0.0}h)");
        }

        public string FormatStatus()
        {
            var h = Mathf.FloorToInt(HourOfDay);
            var m = Mathf.FloorToInt((HourOfDay - h) * 60f);
            var pause = pauseClock ? " PAUSED" : "";
            return $"Day {DayIndex}  {h:00}:{m:00}{pause}";
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

            GUI.Box(new Rect(12f, 12f, 220f, 28f), FormatStatus() + "   ]/+ hour", _style);
        }
    }
}

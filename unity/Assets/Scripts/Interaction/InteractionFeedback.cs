using UnityEngine;

namespace GameDev.Interaction
{
    /// <summary>
    /// S0 on-screen stub feedback: toast boxes + persistent proximity prompt.
    /// </summary>
    public sealed class InteractionFeedback : MonoBehaviour
    {
        private static InteractionFeedback _instance;

        [SerializeField] private float defaultSeconds = 6f;

        private string _title = "";
        private string _body = "";
        private float _until;
        private string _proximityPrompt = "";
        private GUIStyle _boxStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _promptStyle;

        public static void Show(string title, string body, float seconds = -1f)
        {
            EnsureExists();
            _instance._title = title;
            _instance._body = body;
            _instance._until = Time.time + (seconds > 0f ? seconds : _instance.defaultSeconds);
            Debug.Log($"[{title}] {body}");
        }

        public static void Show(string body)
        {
            Show("Notice", body);
        }

        public static void SetProximityPrompt(string prompt)
        {
            EnsureExists();
            _instance._proximityPrompt = prompt ?? "";
        }

        public static void ClearProximityPrompt()
        {
            if (_instance != null)
            {
                _instance._proximityPrompt = "";
            }
        }

        public static void ShowLegend()
        {
            Show(
                "Lower Veil — legend (H to repeat)",
                "Brown NPC/place · Orange POI · Teal env. Mouse LMB path · Tab WASD walk · E talk/tips · N day/night · T TB · ]/+1h world time · P pause clock · H legend. Tips Hint/Mark POIs; windows can expire.",
                12f);
        }

        private static void EnsureExists()
        {
            if (_instance != null)
            {
                return;
            }

            var go = new GameObject("InteractionFeedback");
            _instance = go.AddComponent<InteractionFeedback>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (!string.IsNullOrEmpty(_proximityPrompt))
            {
                const float pWidth = 420f;
                const float pHeight = 36f;
                var pRect = new Rect((Screen.width - pWidth) * 0.5f, Screen.height - 175f, pWidth, pHeight);
                GUI.Box(pRect, GUIContent.none, _boxStyle);
                GUI.Label(new Rect(pRect.x + 12f, pRect.y + 8f, pWidth - 24f, 24f), _proximityPrompt, _promptStyle);
            }

            if (Time.time > _until || string.IsNullOrEmpty(_body))
            {
                return;
            }

            const float width = 560f;
            const float height = 130f;
            var rect = new Rect((Screen.width - width) * 0.5f, Screen.height - height - 28f, width, height);
            GUI.Box(rect, GUIContent.none, _boxStyle);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 10f, width - 32f, 24f), _title, _titleStyle);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 38f, width - 32f, height - 50f), _body, _bodyStyle);
        }

        private void EnsureStyles()
        {
            if (_boxStyle != null)
            {
                return;
            }

            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = MakeTex(2, 2, new Color(0.08f, 0.08f, 0.1f, 0.88f));

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.85f, 0.55f) }
            };

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = new Color(0.92f, 0.92f, 0.9f) }
            };

            _promptStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.75f, 0.95f, 0.8f) }
            };
        }

        private static Texture2D MakeTex(int w, int h, Color color)
        {
            var pixels = new Color[w * h];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            var tex = new Texture2D(w, h);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}

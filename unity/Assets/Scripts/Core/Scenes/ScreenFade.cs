using System;
using System.Collections;
using UnityEngine;

namespace GameDev.Core.Scenes
{
    /// <summary>
    /// The black veil a journey through a door hides behind (owner, 2026-11-05: the swap must not be seen). It darkens
    /// the screen, lets the caller swap the scene in the dark, waits until the new scene has placed its player and its
    /// camera, and lifts. It lives through the swap and draws over everything; times are in real seconds, so a paused
    /// clock does not stop it. Durations are authoring defaults.
    /// </summary>
    public sealed class ScreenFade : MonoBehaviour
    {
        public const float OutSeconds = 0.3f;
        public const float InSeconds = 0.5f;

        // Frames to wait after the scene is loaded: Start runs on the first, the camera settles on the second.
        private const int SettleFrames = 2;

        private static ScreenFade _instance;
        private static Texture2D _black;
        private float _alpha;

        /// <summary>How dark the screen is, 0 to 1.</summary>
        public static float Darkness => _instance == null ? 0f : _instance._alpha;

        internal static void Reset() => _instance = null;

        /// <summary>Darkens the screen, then calls <paramref name="inTheDark"/>.</summary>
        internal static void Out(Action inTheDark) => Instance.StartCoroutine(Instance.Fade(1f, OutSeconds, 0, inTheDark));

        /// <summary>Lifts the veil once the new scene has settled.</summary>
        internal static void In() => Instance.StartCoroutine(Instance.Fade(0f, InSeconds, SettleFrames, null));

        /// <summary>Calls <paramref name="then"/> on the next frame, after every new object's Start (a batch run draws nothing to hook).</summary>
        internal static void NextFrame(Action then) => Instance.StartCoroutine(AfterAFrame(then));

        private static IEnumerator AfterAFrame(Action then)
        {
            yield return null;
            then();
        }

        private static ScreenFade Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("Screen Fade");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<ScreenFade>();
                }

                return _instance;
            }
        }

        private IEnumerator Fade(float to, float seconds, int waitFrames, Action then)
        {
            for (var i = 0; i < waitFrames; i++)
            {
                yield return null;
            }

            var from = _alpha;
            for (var t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                _alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds));
                yield return null;
            }

            _alpha = to;
            then?.Invoke();
        }

        private void OnGUI()
        {
            if (_alpha <= 0.001f)
            {
                return;
            }

            if (_black == null)
            {
                _black = Texture2D.whiteTexture;
            }

            GUI.depth = -1000;
            GUI.color = new Color(0f, 0f, 0f, _alpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _black);
            GUI.color = Color.white;
        }
    }
}

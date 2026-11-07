using System;
using GameDev.World.Timekeeping;
using UnityEngine;

namespace GameDev.Slice.Interiors
{
    /// <summary>
    /// The light of one interior from the game clock (VICTORIAN_INTERIORS_PLAN §5). The pack's window panes are opaque,
    /// so daylight comes in through each window's lit pane and its own spot light, whose colour and strength follow the
    /// hour; lamps and candles burn from dusk to dawn; a fireplace burns in the morning and the evening, a kitchen hearth
    /// all day; the ambient light follows the hour too. Without a running clock it shows <see cref="previewMinute"/>.
    /// Light is information, not decoration: darkness adds to Stealth (10.3). Every hour and colour is an authoring default.
    /// </summary>
    public sealed class InteriorLighting : MonoBehaviour
    {
        [Serializable]
        public struct Hearth
        {
            public Light Light;
            public ParticleSystem Flames;

            [Tooltip("A kitchen hearth: it burns all day, not only in the morning and the evening.")]
            public bool AllDay;
        }

        // Authoring defaults: the sun is up from 05:00 to 20:00, full from 07:00 to 18:00.
        private const int DawnStart = 5 * 60, DawnEnd = 7 * 60, DuskStart = 18 * 60, DuskEnd = 20 * 60;

        // Authoring defaults: a fireplace burns 06:00–10:00 and 17:00–23:00; a kitchen hearth 06:00–22:00.
        private const int MorningFire = 6 * 60, MorningOut = 10 * 60, EveningFire = 17 * 60, EveningOut = 23 * 60, KitchenOut = 22 * 60;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private static readonly Color NoonLight = new(1f, 0.96f, 0.88f), LowLight = new(1f, 0.72f, 0.5f);
        private static readonly Color NoonSky = new(0.78f, 0.86f, 0.95f), LowSky = new(0.95f, 0.62f, 0.42f), NightSky = new(0.04f, 0.06f, 0.11f);
        private static readonly Color DayAmbient = new(0.42f, 0.4f, 0.36f), NightAmbient = new(0.08f, 0.08f, 0.11f);

        [SerializeField] private Light[] windows = Array.Empty<Light>();
        [SerializeField] private Renderer[] panes = Array.Empty<Renderer>();
        [SerializeField] private Light[] lamps = Array.Empty<Light>();
        [SerializeField] private Hearth[] hearths = Array.Empty<Hearth>();

        [Tooltip("Strength of a window's spot at noon.")]
        [SerializeField] private float windowIntensity = 6f;

        [Tooltip("A room with no window (the cellar): no daylight, and its lamps and candles always burn.")]
        [SerializeField] private bool windowless;

        [Tooltip("The minute of the day shown while no game clock runs: in the editor and in renders.")]
        [SerializeField] private int previewMinute = 12 * 60;

        private float[] _lampIntensity = Array.Empty<float>();
        private float[] _hearthIntensity = Array.Empty<float>();
        private MaterialPropertyBlock _block;
        private int _shown = -1;

        public int PreviewMinute => previewMinute;

        /// <summary>Editor setup for scenes built by code.</summary>
        public void Configure(Light[] windowLights, Renderer[] windowPanes, Light[] lampLights, Hearth[] fires, bool noWindow)
        {
            windowless = noWindow;
            windows = windowLights;
            panes = windowPanes;
            lamps = lampLights;
            hearths = fires;
        }

        /// <summary>How much of the day's light stands at <paramref name="minute"/>: 0 at night, 1 by full day.</summary>
        public static float Daylight(int minute)
        {
            if (minute <= DawnStart || minute >= DuskEnd)
            {
                return 0f;
            }

            if (minute < DawnEnd)
            {
                return Mathf.SmoothStep(0f, 1f, (minute - DawnStart) / (float)(DawnEnd - DawnStart));
            }

            return minute <= DuskStart ? 1f : Mathf.SmoothStep(1f, 0f, (minute - DuskStart) / (float)(DuskEnd - DuskStart));
        }

        /// <summary>Lamps and candles burn from dusk to dawn: while less than half the day's light stands.</summary>
        public static bool LampsLit(int minute) => Daylight(minute) < 0.5f;

        public static bool FireLit(int minute, bool allDay) =>
            allDay ? minute >= MorningFire && minute < KitchenOut : (minute >= MorningFire && minute < MorningOut) || (minute >= EveningFire && minute < EveningOut);

        private void Awake()
        {
            _lampIntensity = Array.ConvertAll(lamps, l => l != null ? l.intensity : 0f);
            _hearthIntensity = Array.ConvertAll(hearths, h => h.Light != null ? h.Light.intensity : 0f);
        }

        private void Update()
        {
            HideTheSky();
            var clock = GameClockDriver.Running;
            var minute = clock != null ? clock.MinuteOfDay : previewMinute;
            if (minute != _shown)
            {
                Apply(minute);
            }

            Flicker();
        }

        /// <summary>Sets every light of the interior for <paramref name="minute"/>; renders call it with the hour they show.</summary>
        public void Apply(int minute)
        {
            if (_lampIntensity.Length != lamps.Length || _hearthIntensity.Length != hearths.Length)
            {
                Awake();
            }

            _shown = minute;
            var day = windowless ? 0f : Daylight(minute);
            var low = 1f - Mathf.Clamp01(Mathf.Abs(minute - (12 * 60)) / (6f * 60f)); // 1 at noon, 0 at six
            var light = Color.Lerp(LowLight, NoonLight, low);
            foreach (var w in windows)
            {
                if (w != null)
                {
                    w.color = light;
                    w.intensity = windowIntensity * day;
                    w.enabled = day > 0.01f;
                }
            }

            _block ??= new MaterialPropertyBlock();
            var sky = Color.Lerp(NightSky, Color.Lerp(LowSky, NoonSky, low), day);
            foreach (var p in panes)
            {
                if (p != null)
                {
                    p.GetPropertyBlock(_block);
                    _block.SetColor(BaseColor, sky);
                    p.SetPropertyBlock(_block);
                }
            }

            var lit = windowless || LampsLit(minute);
            for (var i = 0; i < lamps.Length; i++)
            {
                if (lamps[i] != null)
                {
                    lamps[i].enabled = lit;
                    lamps[i].intensity = _lampIntensity[i];
                }
            }

            for (var i = 0; i < hearths.Length; i++)
            {
                var burning = FireLit(minute, hearths[i].AllDay);
                if (hearths[i].Light != null)
                {
                    hearths[i].Light.enabled = burning;
                }

                if (hearths[i].Flames != null)
                {
                    hearths[i].Flames.gameObject.SetActive(burning);
                }
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(NightAmbient, DayAmbient, day);
        }

        /// <summary>
        /// Nothing outside an interior is ever meant to be seen: its windows show the sky as lit panes (§5). The sky
        /// showed bright through the gap round a shut door and the seams between wall pieces (owner's play test VI-5,
        /// remark 6), so behind the walls the camera draws black.
        /// </summary>
        private void HideTheSky()
        {
            var camera = Camera.main;
            if (camera == null || camera == _darkened)
            {
                return;
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            RenderSettings.skybox = null;
            _darkened = camera;
        }

        private Camera _darkened;

        private void Flicker()
        {
            for (var i = 0; i < hearths.Length; i++)
            {
                var l = hearths[i].Light;
                if (l != null && l.enabled)
                {
                    l.intensity = _hearthIntensity[i] * (0.8f + (0.4f * Mathf.PerlinNoise(Time.time * 3f, i * 7.3f)));
                }
            }
        }
    }
}

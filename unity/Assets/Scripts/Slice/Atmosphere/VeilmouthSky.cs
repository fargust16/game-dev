using System;
using GameDev.Slice.Interiors;
using GameDev.VictorianKit;
using GameDev.World.Timekeeping;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameDev.Slice.Atmosphere
{
    /// <summary>
    /// Veilmouth's sky from the game clock (WORLD_STYLE_GUIDE §5, §6.1; city plan §24.5). One directional light is the
    /// sun by day and the crimson moon by night, low over the sea in the south-east; the sky blends day, dusk and night
    /// materials; the ambient light, a thin distance haze and the twilight grade follow the hour; the gas lamps burn
    /// from 19:00 (<see cref="InteriorLighting.LampsLit"/>); the low fog banks thicken by night; and the kit's window
    /// shader reads the night from <see cref="VicStreetLighting.NightId"/>.
    /// <para>
    /// Light is information (10.3): the moon gives shape, not reading light, and the streets between lamps stay dark.
    /// Fog is only seen: the Combat Grid, sight and stealth never read it. Without a running clock the sky shows
    /// <see cref="previewMinute"/>. Every hour, angle, colour and density here is an authoring default.
    /// </para>
    /// </summary>
    public sealed class VeilmouthSky : MonoBehaviour
    {
        /// <summary>A street lamp: its gas light, and the slot of its renderer that holds the glass.</summary>
        [Serializable]
        public struct Lamp
        {
            public Light Light;
            public Renderer Renderer;
            public int Slot;
        }

        // The sun rises at 05:00 and sets at 20:00, as InteriorLighting's daylight; the moon rules 20:00–05:00.
        private const int Sunrise = 5 * 60, Sunset = 20 * 60, Night = 9 * 60;
        private const float SunHighest = 48f, MoonLow = 6f, MoonClimb = 22f;

        private static readonly Color DaySun = new(1f, 0.96f, 0.9f), LowSun = new(1f, 0.62f, 0.38f);
        private static readonly Color Moon = new(0.85f, 0.18f, 0.16f);
        private const float SunIntensity = 1.05f, MoonIntensity = 0.3f;

        private static readonly Color[] Sky = { new(0.42f, 0.47f, 0.55f), new(0.42f, 0.34f, 0.48f), new(0.05f, 0.07f, 0.12f) };
        private static readonly Color[] Equator = { new(0.38f, 0.38f, 0.37f), new(0.45f, 0.32f, 0.28f), new(0.04f, 0.045f, 0.06f) };
        private static readonly Color[] Ground = { new(0.22f, 0.2f, 0.18f), new(0.16f, 0.13f, 0.14f), new(0.02f, 0.02f, 0.025f) };
        private static readonly Color[] Haze = { new(0.62f, 0.66f, 0.7f), new(0.46f, 0.38f, 0.5f), new(0.03f, 0.035f, 0.05f) };
        private static readonly float[] HazeDensity = { 0.0016f, 0.0024f, 0.003f };

        /// <summary>
        /// The low fog's colour and opacity [day, dusk, night]: pale and thin by day, violet at dusk, a dark blue-grey
        /// and thickest by night (WORLD_STYLE_GUIDE §6.1). The particles are unlit, so the colour is set here.
        /// </summary>
        private static readonly Color[] FogColour = { new(0.8f, 0.82f, 0.86f, 0.3f), new(0.6f, 0.5f, 0.66f, 0.45f), new(0.17f, 0.19f, 0.26f, 0.6f) };

        [SerializeField] private Light sun;
        [SerializeField] private Material daySky, duskSky, nightSky;
        [SerializeField] private Volume twilight;
        [SerializeField] private Lamp[] lamps = Array.Empty<Lamp>();
        [SerializeField] private Material glassUnlit, glassLit;
        [SerializeField] private ParticleSystem[] fogBanks = Array.Empty<ParticleSystem>();

        [Tooltip("The moon's disc: a quad kept at MoonDistance from the eye, drawn over the sky, unfogged and unlit.")]
        [SerializeField] private Transform moon;

        [Tooltip("The minute of the day shown while no game clock runs: in the editor and in renders.")]
        [SerializeField] private int previewMinute = 12 * 60;

        /// <summary>The moon's distance from the eye, m: inside the player camera's 1000 m far plane, beyond every street.</summary>
        public const float MoonDistance = 800f;

        private static readonly int SunDisk = Shader.PropertyToID("_SunDisk");

        private Material _sky;
        private int _shown = -1;
        private bool? _lampsLit;
        private bool _moonUp;
        private Vector3 _moonDirection = Vector3.up;

        public int PreviewMinute => previewMinute;

        public Material DaySky => daySky;

        /// <summary>Editor setup for the scene built by code.</summary>
        public void Configure(Light light, Material day, Material dusk, Material night, Volume grade, Lamp[] streetLamps,
            Material unlit, Material lit, ParticleSystem[] banks, Transform moonDisc)
        {
            moon = moonDisc;
            sun = light;
            daySky = day;
            duskSky = dusk;
            nightSky = night;
            twilight = grade;
            lamps = streetLamps;
            glassUnlit = unlit;
            glassLit = lit;
            fogBanks = banks;
        }

        private void Start()
        {
            var camera = Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            }
        }

        private void Update()
        {
            var clock = GameClockDriver.Running;
            var minute = clock != null ? clock.MinuteOfDay : previewMinute;
            if (minute != _shown)
            {
                Apply(minute);
            }
        }

        private void LateUpdate()
        {
            var camera = Camera.main;
            if (camera != null)
            {
                FaceMoon(camera.transform.position);
            }
        }

        /// <summary>Keeps the moon's disc in its direction from <paramref name="eye"/>, shown only while the moon is up.</summary>
        public void FaceMoon(Vector3 eye)
        {
            if (moon == null)
            {
                return;
            }

            moon.gameObject.SetActive(_moonUp);
            moon.SetPositionAndRotation(eye + (_moonDirection * MoonDistance), Quaternion.LookRotation(_moonDirection));
        }

        /// <summary>Sets the sky, the light, the lamps and the fog for <paramref name="minute"/>; renders call it with the hour they show.</summary>
        public void Apply(int minute)
        {
            _shown = minute;
            var day = InteriorLighting.Daylight(minute);
            var dusk = 4f * day * (1f - day); // 1 at 19:00 and 06:00, when half the day's light stands

            // The three looks: from day to dusk while the light is over half, from dusk to night below it.
            var (from, to, t) = day >= 0.5f ? (0, 1, dusk) : (2, 1, dusk);
            Color Blend(Color[] c) => Color.Lerp(c[from], c[to], t);

            SunAndMoon(minute, day);
            Skybox(from, to, t);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Blend(Sky);
            RenderSettings.ambientEquatorColor = Blend(Equator);
            RenderSettings.ambientGroundColor = Blend(Ground);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Blend(Haze);
            RenderSettings.fogDensity = Mathf.Lerp(HazeDensity[from], HazeDensity[to], t);
            Shader.SetGlobalFloat(VicStreetLighting.NightId, 1f - day);
            if (twilight != null)
            {
                twilight.weight = dusk;
            }

            Lamps(InteriorLighting.LampsLit(minute));
            Fog(Blend(FogColour));
        }

        /// <summary>The sun on its arc from east-north-east through the south, or the moon low in the south-east.</summary>
        private void SunAndMoon(int minute, float day)
        {
            if (sun == null)
            {
                return;
            }

            float elevation, azimuth;
            _moonUp = minute < Sunrise || minute > Sunset;
            if (!_moonUp)
            {
                var f = (minute - Sunrise) / (float)(Sunset - Sunrise);
                elevation = SunHighest * Mathf.Sin(Mathf.PI * f);
                azimuth = Mathf.Lerp(80f, 280f, f);
                var low = Mathf.Clamp01(1f - (elevation / 25f));
                sun.color = Color.Lerp(DaySun, LowSun, low * low);
                sun.intensity = SunIntensity * day;
            }
            else
            {
                var f = ((minute - Sunset + 1440) % 1440) / (float)Night;
                elevation = MoonLow + (MoonClimb * Mathf.Sin(Mathf.PI * f));
                azimuth = Mathf.Lerp(115f, 170f, f);
                sun.color = Moon;
                sun.intensity = MoonIntensity * Mathf.SmoothStep(0f, 1f, Mathf.Min(f, 1f - f) * 9f); // up over the first and last hour
            }

            sun.enabled = sun.intensity > 0.005f;
            sun.transform.rotation = Quaternion.Euler(elevation, azimuth + 180f, 0f);
            _moonDirection = -sun.transform.forward;
            RenderSettings.sun = sun;
        }

        private void Skybox(int from, int to, float t)
        {
            var looks = new[] { daySky, duskSky, nightSky };
            if (looks[from] == null || looks[to] == null)
            {
                return;
            }

            if (_sky == null)
            {
                _sky = new Material(daySky) { name = "Veilmouth Sky (live)", hideFlags = HideFlags.DontSave };
            }

            _sky.Lerp(looks[from], looks[to], t);

            // The sky's own disc follows the one directional light, and would burn white where the moon stands: by
            // night the disc is off and the moon is its own quad, red and a little too large (style guide §6.1).
            _sky.SetFloat(SunDisk, _moonUp ? 0f : 2f);
            if (_moonUp)
            {
                _sky.DisableKeyword("_SUNDISK_HIGH_QUALITY");
                _sky.EnableKeyword("_SUNDISK_NONE");
            }
            else
            {
                _sky.DisableKeyword("_SUNDISK_NONE");
                _sky.EnableKeyword("_SUNDISK_HIGH_QUALITY");
            }

            RenderSettings.skybox = _sky;
        }

        private void Lamps(bool lit)
        {
            if (_lampsLit == lit)
            {
                return;
            }

            _lampsLit = lit;
            foreach (var lamp in lamps)
            {
                if (lamp.Light != null)
                {
                    lamp.Light.enabled = lit;
                }

                if (lamp.Renderer != null && glassLit != null && glassUnlit != null)
                {
                    var materials = lamp.Renderer.sharedMaterials;
                    if (lamp.Slot >= 0 && lamp.Slot < materials.Length)
                    {
                        materials[lamp.Slot] = lit ? glassLit : glassUnlit;
                        lamp.Renderer.sharedMaterials = materials;
                    }
                }
            }
        }

        private void Fog(Color colour)
        {
            foreach (var bank in fogBanks)
            {
                if (bank != null)
                {
                    var main = bank.main;
                    main.startColor = colour;
                }
            }
        }

        private void OnDestroy()
        {
            if (_sky != null)
            {
                if (RenderSettings.skybox == _sky)
                {
                    RenderSettings.skybox = daySky;
                }

                Destroy(_sky);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
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
    /// materials of the painted skies (reference plan R3: Free HDR Sky's day and sunset on <c>GameDev/Slice/Sky</c>);
    /// the ambient light, the distance haze and the twilight grade follow the hour; the gas lamps burn
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
        // A lower sun than before (reference plan R3 step 6): the reference's long light, golden from 15:00.
        private const float SunHighest = 42f, MoonLow = 6f, MoonClimb = 22f;
        private const int GoldenFrom = 15 * 60, GoldenFull = 19 * 60, MorningGolden = 8 * 60;

        private static readonly Color DaySun = new(1f, 0.93f, 0.82f), LowSun = new(1f, 0.62f, 0.36f);
        // The moon's light is a soft red, less saturated than its disc: in Linear a pure red light floods the streets.
        private static readonly Color Moon = new(0.85f, 0.3f, 0.26f);

        // The project renders in Linear (owner, 2026-11-08, VS-COLOR): light adds in linear space, so the sun is about
        // twice the Gamma-era 1.05 to light a facade near its material's look, and the ambient colours below, given in
        // sRGB, act as before on surfaces in shade. The moon gives shape, not reading light (WORLD_STYLE_GUIDE §5), so
        // it is a third of its Gamma-era 0.45. Authoring defaults.
        private const float SunIntensity = 2f, MoonIntensity = 0.15f, NightReflection = 0.12f;

        // [day, dusk, night], sRGB: warm greys by day, rose and umber at dusk, a dark blue-grey night (reference plan R3).
        private static readonly Color[] Sky = { new(0.5f, 0.51f, 0.53f), new(0.46f, 0.39f, 0.44f), new(0.06f, 0.065f, 0.09f) };
        private static readonly Color[] Equator = { new(0.47f, 0.45f, 0.41f), new(0.5f, 0.38f, 0.31f), new(0.045f, 0.045f, 0.055f) };
        private static readonly Color[] Ground = { new(0.27f, 0.24f, 0.21f), new(0.2f, 0.16f, 0.14f), new(0.02f, 0.02f, 0.025f) };

        /// <summary>
        /// The distance haze [day, dusk, night]: exponential, about 7 % at 60 m and 30 % at 300 m by day, so the far
        /// roofs fade as in the reference; the painted sky takes the same colour over its horizon.
        /// </summary>
        private static readonly Color[] Haze = { new(0.71f, 0.69f, 0.64f), new(0.6f, 0.49f, 0.45f), new(0.045f, 0.05f, 0.065f) };
        private static readonly float[] HazeDensity = { 0.0012f, 0.0016f, 0.0022f };

        /// <summary>Where the sunset cubemap's painted sun stands with no turn, degrees from north toward east (RenderSkies).</summary>
        private const float SunsetSunYaw = 315f;

        /// <summary>
        /// The low fog's colour and opacity [day, dusk, night]: pale and thin by day, violet at dusk, a dark blue-grey
        /// and thickest by night (WORLD_STYLE_GUIDE §6.1). The particles are unlit, so the colour is set here.
        /// </summary>
        private static readonly Color[] FogColour = { new(0.8f, 0.82f, 0.86f, 0.3f), new(0.6f, 0.5f, 0.66f, 0.45f), new(0.17f, 0.19f, 0.26f, 0.6f) };

        /// <summary>
        /// The low fog (reference plan R3 step 5; <see cref="HeightFog"/>) [day, dusk, night]: its density at the sea's
        /// surface, 1/m, thinning by e every <see cref="LowFogScale"/> metres up; thin by day and thickest by night, as
        /// the fog banks. Its colour is the haze's. Authoring defaults.
        /// </summary>
        private static readonly float[] LowFogDensity = { 0.009f, 0.012f, 0.016f };

        /// <summary>How strongly the fog glows toward the sun or the moon [day, dusk, night], and how tight the glow is.</summary>
        private static readonly float[] Glow = { 0.35f, 1.1f, 0.6f }, GlowSharpness = { 8f, 4f, 6f };

        // The fog's height scale and a sky ray's length, m; the patches' unevenness, size and drift (m/s, toward the
        // north-east, off the sea). The sea's level is WorldScale.SeaLevelMeters. Authoring defaults.
        private const float LowFogScale = 5f, SkyRay = 600f, SeaLevel = 24f, Patchiness = 0.35f, PatchSize = 45f;
        private static readonly Vector2 Drift = new(0.5f, 0.3f);

        // The gas lamps' halos in the fog: strength; the radius of a halo's bright core and of the halo, m; the faint wide
        // part's share. Authoring defaults.
        private const float HaloStrength = 0.6f, HaloCore = 0.18f, HaloRadius = 1.5f, HaloWide = 0.05f;

        /// <summary>
        /// The light's shafts through the gaps between roofs and trees (reference plan R3 step 6) [day, dusk, night]: their
        /// strength, faint by day and strongest with the low sun; the night's is the crimson moon's, whose light is dim, so
        /// its number is larger. How tightly the sky round the light counts, the power of the cosine: the moon is a small
        /// source. Each of 24 steps toward the light counts <see cref="ShaftDecay"/> of the one before, over
        /// <see cref="ShaftReach"/> of the way. Authoring defaults.
        /// </summary>
        private static readonly float[] Shafts = { 0.25f, 1.6f, 3f }, ShaftSharpness = { 24f, 16f, 60f };

        private const float ShaftDecay = 0.95f, ShaftReach = 0.8f;

        [SerializeField] private Light sun;
        [SerializeField] private Material daySky, duskSky, nightSky;
        [SerializeField] private Volume twilight;
        [SerializeField] private Lamp[] lamps = Array.Empty<Lamp>();
        [SerializeField] private Material glassUnlit, glassLit;
        [SerializeField] private ParticleSystem[] fogBanks = Array.Empty<ParticleSystem>();

        [Tooltip("The moon's disc: a quad kept at MoonDistance from the eye, drawn over the sky, unfogged and unlit.")]
        [SerializeField] private Transform moon;

        [Tooltip("The streets' reflection probes, baked at noon (VeilmouthReflections): dimmed with the day.")]
        [SerializeField] private ReflectionProbe[] streetProbes = Array.Empty<ReflectionProbe>();

        [Tooltip("The minute of the day shown while no game clock runs: in the editor and in renders.")]
        [SerializeField] private int previewMinute = 12 * 60;

        /// <summary>The moon's distance from the eye, m: inside the player camera's 1000 m far plane, beyond every street.</summary>
        public const float MoonDistance = 800f;

        private static readonly int RotationB = Shader.PropertyToID("_RotationB");

        private Material _sky;
        private int _shown = -1;
        private bool? _lampsLit;
        private bool _moonUp;
        private Vector3 _moonDirection = Vector3.up;

        public int PreviewMinute => previewMinute;

        /// <summary>The sun's light, for the shadow a hider stands in (10.3 §3, S3b.1).</summary>
        public Light Sun => sun;

        /// <summary>The street lamps, for the Darkness away from them at night (10.3 §3, S3b.1).</summary>
        public IReadOnlyList<Lamp> StreetLamps => lamps;

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

        /// <summary>Editor setup: the street probes the reflections pass baked.</summary>
        public void SetProbes(ReflectionProbe[] probes) => streetProbes = probes;

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
            Skybox(from, to, t, minute);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Blend(Sky);
            RenderSettings.ambientEquatorColor = Blend(Equator);
            RenderSettings.ambientGroundColor = Blend(Ground);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = Blend(Haze);
            RenderSettings.fogDensity = Mathf.Lerp(HazeDensity[from], HazeDensity[to], t);

            // The environment reflection and the street probes hold the day, so glossy surfaces, glass and the water
            // dim with the light, as the kit's street probes do (VicStreetLighting).
            var reflection = Mathf.Lerp(NightReflection, 1f, day);
            RenderSettings.reflectionIntensity = reflection;
            foreach (var probe in streetProbes)
            {
                if (probe != null)
                {
                    probe.intensity = reflection;
                }
            }

            Shader.SetGlobalFloat(VicStreetLighting.NightId, 1f - day);
            Shader.SetGlobalFloat(VicStreetLighting.LateId, VicStreetLighting.Late(minute));
            if (twilight != null)
            {
                twilight.weight = dusk;
            }

            Lamps(InteriorLighting.LampsLit(minute));
            Fog(Blend(FogColour));
            float Mix(float[] v) => Mathf.Lerp(v[from], v[to], t);
            LowFog(Blend(Haze), Mix(LowFogDensity), Mix(Glow), Mix(GlowSharpness));
            Shader.SetGlobalVector(HeightFog.ShaftParamsId, new Vector4(Mix(Shafts), ShaftDecay, Mix(ShaftSharpness), ShaftReach));
        }

        /// <summary>The low fog's globals for <see cref="HeightFogFeature"/>: lit toward the sun by day, the moon by night.</summary>
        private void LowFog(Color haze, float density, float glow, float sharpness)
        {
            HeightFog.Own(this);
            Shader.SetGlobalVector(HeightFog.ColourId, haze.linear);
            Shader.SetGlobalVector(HeightFog.ParamsId, new Vector4(density, LowFogScale, SeaLevel, SkyRay));
            var light = sun != null ? sun.color.linear * Mathf.Max(sun.intensity, _moonUp ? MoonIntensity : 0f) : Color.black;
            Shader.SetGlobalVector(HeightFog.SunId, new Vector4(_moonDirection.x, _moonDirection.y, _moonDirection.z, glow));
            Shader.SetGlobalVector(HeightFog.SunColourId, new Vector4(light.r, light.g, light.b, sharpness));
            Shader.SetGlobalVector(HeightFog.DriftId, new Vector4(Drift.x, Drift.y, Patchiness, 1f / PatchSize));
            Shader.SetGlobalVector(HeightFog.LampParamsId, new Vector4(HaloStrength, SeaLevel, HaloCore, HaloWide));
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
                sun.color = Color.Lerp(DaySun, LowSun, Golden(minute));
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

        /// <summary>
        /// How golden the sun is, 0–1: from 15:00 it warms toward the low sun by 19:00, and in the morning it is golden
        /// at sunrise and white again by 08:00 (reference plan R3 step 6).
        /// </summary>
        private static float Golden(int minute)
        {
            var evening = Mathf.InverseLerp(GoldenFrom, GoldenFull, minute);
            var morning = 1f - Mathf.InverseLerp(Sunrise, MorningGolden, minute);
            return Mathf.SmoothStep(0f, 1f, Mathf.Max(evening, morning));
        }

        /// <summary>
        /// The way the light comes from, degrees from north toward east, carried on through the night so it never jumps:
        /// the sun's 80°–280° by day, then on round through the north to 440° (80°) by sunrise. The sunset cubemap turns
        /// with it, so its painted sun stands behind the low sun at dusk.
        /// </summary>
        private static float LightTrack(int minute)
        {
            if (minute >= Sunrise && minute <= Sunset)
            {
                return Mathf.Lerp(80f, 280f, (minute - Sunrise) / (float)(Sunset - Sunrise));
            }

            return Mathf.Lerp(280f, 440f, ((minute - Sunset + 1440) % 1440) / (float)Night);
        }

        private void Skybox(int from, int to, float t, int minute)
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

            // The looks share the two cubemaps, so blending their numbers blends the sky; the moon is its own quad.
            _sky.Lerp(looks[from], looks[to], t);
            _sky.SetFloat(RotationB, Mathf.Repeat(LightTrack(minute) - SunsetSunYaw, 360f));
            RenderSettings.skybox = _sky;
        }

        private void Lamps(bool lit)
        {
            if (_lampsLit == lit)
            {
                return;
            }

            _lampsLit = lit;
            HeightFog.SetLamps(lit
                ? lamps.Where(l => l.Light != null).Select(l => (l.Light.transform.position, Halo(l.Light)))
                : null);
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

        /// <summary>A lamp's halo for <see cref="HeightFog"/>: its light's linear colour times its intensity, and the halo's radius.</summary>
        private static Color Halo(Light light)
        {
            var c = light.color.linear * light.intensity;
            return new Color(c.r, c.g, c.b, HaloRadius);
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

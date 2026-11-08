using UnityEngine;

namespace GameDev.VictorianKit
{
    /// <summary>
    /// Day and night for the Victorian streets: the sun's colour and strength, the sky's ambient light, and the global
    /// night factor the window shader reads for its lit rooms. One per scene; it changes no material, so the SRP
    /// Batcher keeps every facade in one batch.
    /// </summary>
    [ExecuteAlways]
    public sealed class VicStreetLighting : MonoBehaviour
    {
        /// <summary>The global the window shader reads: 0 by day, 1 at night.</summary>
        public static readonly int NightId = Shader.PropertyToID("_VicNight");

        public Light Sun;

        /// <summary>The streets' baked reflection probes: dimmed at night, since they hold the day sky.</summary>
        public ReflectionProbe[] Probes;
        [Range(0f, 1f)] public float Night;

        // The project renders in Linear colour space (owner, 2026-11-08; AGENTS.md *Colour space*): light multiplies
        // linear albedo, so a sun of about 2 shows a facade in full sun near its material's look, twice the Gamma-era
        // 1.05. A scene saved before the switch keeps its own value until it is rebuilt. Authoring defaults.
        public Color DaySun = new Color(1f, 0.96f, 0.9f);
        public float DaySunIntensity = 2f;
        public Color DaySky = new Color(0.42f, 0.47f, 0.55f);
        public Color DayEquator = new Color(0.38f, 0.38f, 0.37f);
        public Color DayGround = new Color(0.22f, 0.2f, 0.18f);

        /// <summary>The day sky, and the dark one swapped in after dusk; both are material assets, never edited here.</summary>
        public Material DaySkybox;
        public Material NightSkybox;
        public Color DayFog = new Color(0.62f, 0.66f, 0.70f);
        public Color NightFog = new Color(0.03f, 0.035f, 0.05f);

        public Color NightSun = new Color(0.55f, 0.65f, 0.9f);
        public float NightSunIntensity = 0.12f;
        public Color NightSky = new Color(0.05f, 0.07f, 0.12f);
        public Color NightEquator = new Color(0.04f, 0.045f, 0.06f);
        public Color NightGround = new Color(0.02f, 0.02f, 0.025f);

        private void OnEnable() => Apply();

        private void OnValidate() => Apply();

        /// <summary>Sets the sun, the ambient gradient and the night global from <see cref="Night"/>.</summary>
        public void Apply()
        {
            var n = Mathf.Clamp01(Night);
            if (Sun != null)
            {
                Sun.color = Color.Lerp(DaySun, NightSun, n);
                Sun.intensity = Mathf.Lerp(DaySunIntensity, NightSunIntensity, n);
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(DaySky, NightSky, n);
            RenderSettings.ambientEquatorColor = Color.Lerp(DayEquator, NightEquator, n);
            RenderSettings.ambientGroundColor = Color.Lerp(DayGround, NightGround, n);
            RenderSettings.fogColor = Color.Lerp(DayFog, NightFog, n);
            if (DaySkybox != null && NightSkybox != null)
            {
                RenderSettings.skybox = n < 0.5f ? DaySkybox : NightSkybox;
            }

            Shader.SetGlobalFloat(NightId, n);
            if (Probes != null)
            {
                foreach (var probe in Probes)
                {
                    if (probe != null)
                    {
                        probe.intensity = Mathf.Lerp(1f, 0.1f, n);
                    }
                }
            }
        }
    }
}

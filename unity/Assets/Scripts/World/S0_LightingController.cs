using UnityEngine;
using UnityEngine.Rendering;

namespace GameDev.World
{
    /// <summary>
    /// Manages Day/Night lighting transitions and local light pools for S0 Lower Veil greybox.
    /// Toggle in Play Mode with 'N' key, or in Editor via Inspector context menu / toggle field.
    /// </summary>
    [ExecuteAlways]
    public sealed class S0_LightingController : MonoBehaviour
    {
        [Header("Light Objects")]
        [SerializeField] private GameObject sunDay;
        [SerializeField] private GameObject sunNight;
        [SerializeField] private GameObject fillSkyDay;
        [SerializeField] private GameObject rimFactory;
        [SerializeField] private GameObject warmTavernPool;
        [SerializeField] private GameObject coolLoomPool;
        [SerializeField] private GameObject nightGasLamps;

        [Header("State")]
        [SerializeField] private bool isNight = false;

        public bool IsNight => isNight;

        private void Update()
        {
            if (Application.isPlaying && Input.GetKeyDown(KeyCode.N))
            {
                ToggleLighting();
            }
        }

        private void OnValidate()
        {
            ApplyLightingMode(isNight);
        }

        [ContextMenu("Toggle Day/Night")]
        public void ToggleLighting()
        {
            SetNightMode(!isNight);
        }

        [ContextMenu("Set Day Mode")]
        public void SetDayMode()
        {
            SetNightMode(false);
        }

        [ContextMenu("Set Night Mode")]
        public void SetNightModeMenu()
        {
            SetNightMode(true);
        }

        public void SetNightMode(bool night)
        {
            isNight = night;
            ApplyLightingMode(isNight);
            Debug.Log($"[S0_Lighting] Mode switched to: {(isNight ? "NIGHT" : "DAY")}");
        }

        public void Bind(
            GameObject daySun,
            GameObject nightSun,
            GameObject dayFill,
            GameObject factoryRim,
            GameObject tavernPool,
            GameObject loomPool,
            GameObject gasLamps)
        {
            sunDay = daySun;
            sunNight = nightSun;
            fillSkyDay = dayFill;
            rimFactory = factoryRim;
            warmTavernPool = tavernPool;
            coolLoomPool = loomPool;
            nightGasLamps = gasLamps;

            ApplyLightingMode(isNight);
        }

        private void ApplyLightingMode(bool night)
        {
            if (sunDay != null) sunDay.SetActive(!night);
            if (sunNight != null) sunNight.SetActive(night);
            if (fillSkyDay != null) fillSkyDay.SetActive(!night);
            if (rimFactory != null) rimFactory.SetActive(true);
            if (warmTavernPool != null)
            {
                warmTavernPool.SetActive(true);
                var l = warmTavernPool.GetComponent<Light>();
                if (l != null) l.intensity = night ? 0.85f : 0.45f;
            }
            if (coolLoomPool != null)
            {
                coolLoomPool.SetActive(true);
                var l = coolLoomPool.GetComponent<Light>();
                if (l != null) l.intensity = night ? 0.6f : 0.35f;
            }
            if (nightGasLamps != null) nightGasLamps.SetActive(night);

            // Ambient + Fog
            RenderSettings.ambientMode = AmbientMode.Trilight;
            if (!night)
            {
                // DAY Ambient
                RenderSettings.ambientSkyColor = new Color(0.48f, 0.44f, 0.40f);
                RenderSettings.ambientEquatorColor = new Color(0.26f, 0.25f, 0.24f);
                RenderSettings.ambientGroundColor = new Color(0.08f, 0.08f, 0.08f);

                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogDensity = 0.012f;
                RenderSettings.fogColor = new Color(0.32f, 0.30f, 0.35f);
            }
            else
            {
                // NIGHT Ambient
                RenderSettings.ambientSkyColor = new Color(0.08f, 0.12f, 0.20f);
                RenderSettings.ambientEquatorColor = new Color(0.04f, 0.06f, 0.10f);
                RenderSettings.ambientGroundColor = new Color(0.01f, 0.01f, 0.02f);

                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogDensity = 0.018f;
                RenderSettings.fogColor = new Color(0.05f, 0.07f, 0.12f);
            }

            var mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = night ? new Color(0.05f, 0.07f, 0.12f) : new Color(0.22f, 0.20f, 0.24f);
            }

            DynamicGI.UpdateEnvironment();
        }
    }
}

using GameDev.Interaction;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDev.World
{
    /// <summary>
    /// Living day/night driven by WorldClock with smooth dawn/dusk blends.
    /// N jumps clock to dusk or morning for preview (does not hard-swap lights).
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

        [Header("Blend")]
        [SerializeField] [Range(0f, 1f)] private float nightBlend;
        [SerializeField] private float blendSmoothTime = 2.8f;
        [SerializeField] private bool followWorldClock = true;

        private Light _sunDayLight;
        private Light _sunNightLight;
        private Light _fillLight;
        private Light _rimLight;
        private Light _tavernLight;
        private Light _loomLight;
        private Light[] _gasLights;

        private float _sunDayBase = 1.25f;
        private float _sunNightBase = 0.25f;
        private float _fillBase = 0.25f;
        private float _rimBase = 1f;
        private float _tavernDay = 0.45f;
        private float _tavernNight = 0.85f;
        private float _loomDay = 0.35f;
        private float _loomNight = 0.6f;
        private float[] _gasBases;

        private float _blendVelocity;
        private Quaternion _sunDayRot = Quaternion.Euler(50f, -35f, 0f);
        private Quaternion _sunNightRot = Quaternion.Euler(28f, -55f, 0f);

        public float NightBlend => nightBlend;
        public bool IsNight => nightBlend >= 0.55f;

        private void Awake()
        {
            CacheLights();
            ApplyBlend(nightBlend, force: true);
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.N) && WorldClock.Instance != null && !WorldClock.Instance.IsPaused)
            {
                // Preview helper: jump toward dusk or next morning; lighting eases after.
                var hour = WorldClock.Instance.HourOfDay;
                if (hour < 19.5f)
                {
                    WorldClock.Instance.SkipToHourOfDay(20f);
                    InteractionFeedback.Show("Light", "Skipped toward dusk — lighting will ease into night.", 3f);
                }
                else
                {
                    WorldClock.Instance.SkipToHourOfDay(8f);
                    InteractionFeedback.Show("Light", "Skipped toward morning — lighting will ease into day.", 3f);
                }
            }

            var target = followWorldClock && WorldClock.Instance != null
                ? NightFactorFromHour(WorldClock.Instance.HourOfDay)
                : nightBlend;

            // Smooth even while gameplay timeScale is 0? No — freeze blend when paused.
            var dt = WorldClock.Instance != null && WorldClock.Instance.IsPaused
                ? 0f
                : Time.unscaledDeltaTime;

            if (dt > 0f)
            {
                nightBlend = Mathf.SmoothDamp(nightBlend, target, ref _blendVelocity, blendSmoothTime, 10f, dt);
            }
            else if (!followWorldClock)
            {
                nightBlend = target;
            }

            // Snap catch-up when target jumps far (e.g. ] spam / N skip).
            if (Mathf.Abs(target - nightBlend) > 0.85f)
            {
                blendSmoothTime = Mathf.Max(blendSmoothTime, 0.01f);
            }

            ApplyBlend(nightBlend, force: false);
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            CacheLights();
            ApplyBlend(nightBlend, force: true);
        }

        [ContextMenu("Preview Full Day")]
        public void SetDayMode()
        {
            followWorldClock = false;
            nightBlend = 0f;
            _blendVelocity = 0f;
            ApplyBlend(0f, force: true);
        }

        [ContextMenu("Preview Full Night")]
        public void SetNightModeMenu()
        {
            followWorldClock = false;
            nightBlend = 1f;
            _blendVelocity = 0f;
            ApplyBlend(1f, force: true);
        }

        [ContextMenu("Follow World Clock")]
        public void EnableWorldClockFollow()
        {
            followWorldClock = true;
        }

        public void SetNightMode(bool night)
        {
            // Kept for older calls / builder. Uses blend, not hard cut.
            followWorldClock = false;
            nightBlend = night ? 1f : 0f;
            _blendVelocity = 0f;
            ApplyBlend(nightBlend, force: true);
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

            CacheLights();
            followWorldClock = true;
            nightBlend = 0f;
            ApplyBlend(0f, force: true);
        }

        private void CacheLights()
        {
            _sunDayLight = sunDay != null ? sunDay.GetComponent<Light>() : null;
            _sunNightLight = sunNight != null ? sunNight.GetComponent<Light>() : null;
            _fillLight = fillSkyDay != null ? fillSkyDay.GetComponent<Light>() : null;
            _rimLight = rimFactory != null ? rimFactory.GetComponent<Light>() : null;
            _tavernLight = warmTavernPool != null ? warmTavernPool.GetComponent<Light>() : null;
            _loomLight = coolLoomPool != null ? coolLoomPool.GetComponent<Light>() : null;

            if (_sunDayLight != null)
            {
                _sunDayBase = Mathf.Max(_sunDayLight.intensity, 0.01f);
                _sunDayRot = sunDay.transform.rotation;
            }

            if (_sunNightLight != null)
            {
                _sunNightBase = Mathf.Max(_sunNightLight.intensity, 0.01f);
                _sunNightRot = sunNight.transform.rotation;
            }

            if (_fillLight != null)
            {
                _fillBase = Mathf.Max(_fillLight.intensity, 0.01f);
            }

            if (_rimLight != null)
            {
                _rimBase = Mathf.Max(_rimLight.intensity, 0.01f);
            }

            if (nightGasLamps != null)
            {
                _gasLights = nightGasLamps.GetComponentsInChildren<Light>(true);
                _gasBases = new float[_gasLights.Length];
                for (var i = 0; i < _gasLights.Length; i++)
                {
                    _gasBases[i] = Mathf.Max(_gasLights[i].intensity, 0.01f);
                }
            }
        }

        /// <summary>
        /// 0 = full day, 1 = full night. Soft dawn ~5–8, dusk ~17–20.
        /// </summary>
        public static float NightFactorFromHour(float hourOfDay)
        {
            var h = hourOfDay % 24f;
            if (h < 0f)
            {
                h += 24f;
            }

            if (h >= 8f && h < 17f)
            {
                return 0f;
            }

            if (h >= 20f || h < 5f)
            {
                return 1f;
            }

            if (h >= 5f && h < 8f)
            {
                // Dawn: 1 → 0
                return 1f - Smooth01((h - 5f) / 3f);
            }

            // Dusk: 17–20, 0 → 1
            return Smooth01((h - 17f) / 3f);
        }

        private static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private void ApplyBlend(float n, bool force)
        {
            n = Mathf.Clamp01(n);
            var day = 1f - n;

            if (sunDay != null)
            {
                sunDay.SetActive(day > 0.02f);
            }

            if (sunNight != null)
            {
                sunNight.SetActive(n > 0.02f);
            }

            if (fillSkyDay != null)
            {
                fillSkyDay.SetActive(day > 0.02f);
            }

            if (_sunDayLight != null)
            {
                _sunDayLight.intensity = _sunDayBase * day;
                if (sunDay != null)
                {
                    sunDay.transform.rotation = Quaternion.Slerp(_sunDayRot, _sunNightRot, n * 0.35f);
                }
            }

            if (_sunNightLight != null)
            {
                _sunNightLight.intensity = _sunNightBase * n;
            }

            if (_fillLight != null)
            {
                _fillLight.intensity = _fillBase * day;
            }

            if (_rimLight != null)
            {
                _rimLight.intensity = Mathf.Lerp(_rimBase * 0.65f, _rimBase, n);
            }

            if (warmTavernPool != null)
            {
                warmTavernPool.SetActive(true);
            }

            if (_tavernLight != null)
            {
                _tavernLight.intensity = Mathf.Lerp(_tavernDay, _tavernNight, n);
            }

            if (coolLoomPool != null)
            {
                coolLoomPool.SetActive(true);
            }

            if (_loomLight != null)
            {
                _loomLight.intensity = Mathf.Lerp(_loomDay, _loomNight, n);
            }

            if (nightGasLamps != null)
            {
                var lampsOn = n > 0.08f;
                nightGasLamps.SetActive(lampsOn);
                if (lampsOn && _gasLights != null)
                {
                    for (var i = 0; i < _gasLights.Length; i++)
                    {
                        if (_gasLights[i] == null)
                        {
                            continue;
                        }

                        var baseIntensity = _gasBases != null && i < _gasBases.Length ? _gasBases[i] : 1.8f;
                        // Fade in through dusk, full after nightFactor ~0.7
                        var gas = Mathf.Clamp01((n - 0.08f) / 0.55f);
                        _gasLights[i].intensity = baseIntensity * gas;
                    }
                }
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(
                new Color(0.48f, 0.44f, 0.40f),
                new Color(0.08f, 0.12f, 0.20f),
                n);
            RenderSettings.ambientEquatorColor = Color.Lerp(
                new Color(0.26f, 0.25f, 0.24f),
                new Color(0.04f, 0.06f, 0.10f),
                n);
            RenderSettings.ambientGroundColor = Color.Lerp(
                new Color(0.08f, 0.08f, 0.08f),
                new Color(0.01f, 0.01f, 0.02f),
                n);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = Mathf.Lerp(0.012f, 0.018f, n);
            RenderSettings.fogColor = Color.Lerp(
                new Color(0.32f, 0.30f, 0.35f),
                new Color(0.05f, 0.07f, 0.12f),
                n);

            var mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = Color.Lerp(
                    new Color(0.22f, 0.20f, 0.24f),
                    new Color(0.05f, 0.07f, 0.12f),
                    n);
            }

            if (force)
            {
                DynamicGI.UpdateEnvironment();
            }
        }
    }
}

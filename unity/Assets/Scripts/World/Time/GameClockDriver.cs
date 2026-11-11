using UnityEngine;

namespace GameDev.World.Timekeeping
{
    /// <summary>
    /// Feeds the clock real time and keeps it alive through scene changes. A driver that arrives with a
    /// later scene while one already runs removes itself, so a scene may carry one for testing.
    /// </summary>
    public sealed class GameClockDriver : MonoBehaviour
    {
        [SerializeField] private GameClockSettings settings;

        private static GameClockDriver _running;
        private GameClock _clock;

        /// <summary>The running clock, or null before a driver has woken.</summary>
        public static IWorldClock Running => _running != null ? _running._clock : null;

        /// <summary>Real seconds a tick lasts while the clock runs; 0 before a driver has woken.</summary>
        public static float RealSecondsPerTick => _running != null && _running.settings != null ? _running.settings.RealSecondsPerTick : 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _running = null;
        }

        private void Awake()
        {
            if (_running != null)
            {
                Destroy(gameObject);
                return;
            }

            if (settings == null)
            {
                Debug.LogError("GameClockDriver needs a GameClockSettings asset.", this);
                enabled = false;
                return;
            }

            _clock = new GameClock(settings.RealSecondsPerTick, settings.StartTick);
            _running = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_running == this)
            {
                _running = null;
            }
        }

        private void Update()
        {
            _clock.AdvanceRealTime(Time.deltaTime);
        }
    }
}

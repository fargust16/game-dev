using GameDev.World.Poi;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using UnityEngine;

namespace GameDev.Slice
{
    /// <summary>
    /// Owns the slice's run-time world: the world state store, the POI Director on the running clock, and the tips
    /// the sources say. It lives through scene changes like the clock; a driver that arrives with a later scene while
    /// one already runs removes itself, so a scene may carry one for testing.
    /// </summary>
    public sealed class SliceWorldDriver : MonoBehaviour
    {
        [SerializeField] private SliceSettings settings;
        [SerializeField] private PoiDefinition[] pois = new PoiDefinition[0];
        [SerializeField] private TipCatalogue tips;

        private static SliceWorldDriver _running;

        /// <summary>The running world, or null before a driver has started.</summary>
        public static SliceWorldDriver Running => _running != null && _running.Tips != null ? _running : null;

        public WorldState State { get; private set; }

        public PoiDirector Director { get; private set; }

        public TipBook Tips { get; private set; }

        public IWorldClock Clock { get; private set; }

        /// <summary>Setup for scenes built by code; a placed driver is set in the Inspector.</summary>
        public void Configure(SliceSettings sliceSettings, PoiDefinition[] definitions, TipCatalogue catalogue)
        {
            settings = sliceSettings;
            pois = definitions;
            tips = catalogue;
        }

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

            _running = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        // Start, not Awake: the clock's driver wakes in the same scene, in an order Unity does not promise.
        private void Start()
        {
            if (_running != this)
            {
                return;
            }

            Clock = GameClockDriver.Running;
            if (settings == null || tips == null || Clock == null)
            {
                Debug.LogError("SliceWorldDriver needs SliceSettings, a TipCatalogue, and a running clock.", this);
                enabled = false;
                return;
            }

            State = new WorldState();
            Director = new PoiDirector(pois, State, settings.CreateSeed(), SliceSeedStreams.PoiUnattended, Clock);
            Director.Evaluate(Clock.Tick);
            Tips = new TipBook(tips, State, Director);
        }

        private void OnDestroy()
        {
            if (_running == this)
            {
                Director?.Dispose();
                _running = null;
            }
        }
    }
}

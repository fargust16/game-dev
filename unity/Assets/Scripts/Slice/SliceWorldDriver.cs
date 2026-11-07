using GameDev.Input;
using GameDev.UI;
using GameDev.World.Poi;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using UnityEngine;

namespace GameDev.Slice
{
    /// <summary>
    /// Owns the slice's run-time world: the world state store, the POI Director on the running clock, the tips the
    /// sources say, and the Character Notes, which J opens on the HUD. It lives through scene changes like the clock; a driver that arrives with a later scene while
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

        public CharacterNotes Notes { get; private set; }

        private HudView _hud;

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
            Notes = new CharacterNotes(Tips);
            Notes.Added += _ => RefreshNotes();
        }

        // The Notes page belongs to whichever scene's HUD is up; the key works in every scene the world lives through.
        private void Update()
        {
            var input = GameInputService.Instance;
            if (Notes == null || input == null || !input.NotesPressed)
            {
                return;
            }

            var hud = Hud();
            if (hud == null)
            {
                return;
            }

            if (hud.IsNotesOpen)
            {
                hud.CloseNotes();
            }
            else
            {
                hud.ShowNotes(Notes.Lines);
            }
        }

        private void RefreshNotes()
        {
            var hud = Hud();
            if (hud != null && hud.IsNotesOpen)
            {
                hud.ShowNotes(Notes.Lines);
            }
        }

        private HudView Hud()
        {
            if (_hud == null)
            {
                _hud = FindAnyObjectByType<HudView>();
            }

            return _hud;
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

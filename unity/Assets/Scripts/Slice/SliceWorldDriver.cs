using System.Collections.Generic;
using GameDev.Input;
using GameDev.Locomotion;
using GameDev.UI;
using GameDev.World.Poi;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDev.Slice
{
    /// <summary>
    /// Owns the slice's run-time world: the world state store, the POI Director on the running clock, the tips the
    /// sources say, the Character Notes on J, and the map on M. It lives through scene changes like the clock; a
    /// driver that arrives with a later scene while one already runs removes itself, so a scene may carry one for testing.
    /// </summary>
    public sealed class SliceWorldDriver : MonoBehaviour
    {
        [SerializeField] private SliceSettings settings;
        [SerializeField] private PoiDefinition[] pois = new PoiDefinition[0];
        [SerializeField] private TipCatalogue tips;

        [Tooltip("The city plan C2 as SVG text: the map's drawing.")]
        [SerializeField] private TextAsset cityPlan;

        [Tooltip("The scene the plan draws; the map shows the player only there.")]
        [SerializeField] private string cityScene = "Veilmouth";

        private static SliceWorldDriver _running;
        private HudView _hud;

        /// <summary>The running world, or null before a driver has started.</summary>
        public static SliceWorldDriver Running => _running != null && _running.Tips != null ? _running : null;

        public WorldState State { get; private set; }

        public PoiDirector Director { get; private set; }

        public TipBook Tips { get; private set; }

        public IWorldClock Clock { get; private set; }

        public CharacterNotes Notes { get; private set; }

        /// <summary>Setup for scenes built by code; a placed driver is set in the Inspector.</summary>
        public void Configure(SliceSettings sliceSettings, PoiDefinition[] definitions, TipCatalogue catalogue, TextAsset plan)
        {
            settings = sliceSettings;
            pois = definitions;
            tips = catalogue;
            cityPlan = plan;
        }

        /// <summary>
        /// What the map shows (09; 04 rule 5): each POI the player knows of whose window has not closed, a Marked one
        /// as a pin with its title and its source's words of urgency, a Hinted one as an area; never a countdown.
        /// </summary>
        public IReadOnlyList<MapMark> MapMarks()
        {
            var marks = new List<MapMark>();
            foreach (var poi in Director.Pois)
            {
                var known = Director.Discovery(poi);
                if (known == PoiDiscovery.Unknown || Director.Phase(poi) == PoiPhase.Resolved)
                {
                    continue;
                }

                var exact = known == PoiDiscovery.Marked;
                var words = exact && !string.IsNullOrEmpty(poi.UrgencyWords) ? $"{poi.Title}: {poi.UrgencyWords}" : poi.Title;
                marks.Add(new MapMark(poi.Place, poi.HintRadius, exact, words));
            }

            return marks;
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
            Notes.Added += _ => Refresh();
            State.Changed += (_, _, _) => Refresh();
        }

        // The pages belong to whichever scene's HUD is up; the keys work in every scene the world lives through.
        private void Update()
        {
            var input = GameInputService.Instance;
            if (Notes == null || input == null)
            {
                return;
            }

            var hud = Hud();
            if (hud == null)
            {
                return;
            }

            if (input.NotesPressed)
            {
                if (hud.IsNotesOpen)
                {
                    hud.CloseNotes();
                }
                else
                {
                    hud.ShowNotes(Notes.Lines);
                }
            }

            if (input.MapPressed)
            {
                if (hud.IsMapOpen)
                {
                    hud.CloseMap();
                }
                else
                {
                    ShowMap(hud);
                }
            }
            else if (hud.IsMapOpen)
            {
                ShowMap(hud);
            }
        }

        private void ShowMap(HudView hud)
        {
            Vector3? player = null;
            if (SceneManager.GetActiveScene().name == cityScene)
            {
                var walker = FindAnyObjectByType<PlayerLocomotion>();
                player = walker != null ? (Vector3?)walker.transform.position : null;
            }

            hud.ShowMap(cityPlan != null ? cityPlan.text : null, MapMarks(), player);
        }

        private void Refresh()
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

using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Dialogue;
using GameDev.Cameras;
using GameDev.Input;
using GameDev.Items;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Player.Standing;
using GameDev.Slice.Fights;
using GameDev.Slice.Gathering;
using GameDev.Slice.Gear;
using GameDev.Slice.Kit;
using GameDev.Slice.People;
using GameDev.Slice.Shops;
using GameDev.Slice.Stealth;
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
    /// sources say, the character's values and standing, the conversations (S2.4), the Character Notes on J, the map
    /// on M, the bag on I (S5a.1), and the next language on F10. It lives through scene changes like the clock; a
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

        /// <summary>The character's attributes and skills, all 1 until S5b's character creation (S2 authoring default).</summary>
        public CharacterSheet Character { get; private set; }

        /// <summary>The character's bag and purse (S5a.1); null when the settings carry no item catalogue.</summary>
        public Bag Bag { get; private set; }

        /// <summary>What the character wears and holds (S5a.2); null without a bag.</summary>
        public Outfit Outfit { get; private set; }

        /// <summary>The shops on Bawling Row (S5a.3); null without a bag or the shops' data.</summary>
        public SliceShops Shops { get; private set; }

        /// <summary>Edda Brann's workshop (S5a.4); null without the shops.</summary>
        public SliceWorkshop Workshop { get; private set; }

        /// <summary>The character's Stamina (S5a.5; 07.2 §3.2); null without the gathering rules.</summary>
        public SliceStamina Stamina { get; private set; }

        /// <summary>Gathering at the nodes outside the walls (S5a.5); null without a bag or the gathering rules.</summary>
        public SliceGathering Gathering { get; private set; }

        /// <summary>What fallen beasts leave (S5a.6): their ingredients, and their hides by Skinning; null without a bag or the beasts.</summary>
        public GameDev.Slice.Beasts.SliceRemains Remains { get; private set; }

        /// <summary>11's start: plain clothes with no defence and 500 bronze, with no weapon or tool.</summary>
        public const int StartingBronze = 500;

        public const string PlainClothes = "plain_clothes";

        public static Bag StartingBag(ItemCatalogue catalogue, ICharacterValues character)
        {
            var bag = new Bag(catalogue, character);
            bag.AddPiece(new ItemPiece(PlainClothes));
            bag.Earn(StartingBronze);
            return bag;
        }

        /// <summary>The fights in the scene (S3a.4): the Encounter Lock, the turns and the Combat Grid; null without FightRules.</summary>
        public SliceConfront Fights { get; private set; }

        /// <summary>Stealth in free movement (S3b.1); a fight takes it over while it lasts.</summary>
        public SliceStealth Hiding { get; private set; }

        /// <summary>The beasts' territories outside the walls (S3b.2).</summary>
        public GameDev.Slice.Beasts.SliceBeasts Beasts { get; private set; }

        /// <summary>The Deeds Record, Attitude and lies (10.4 §4–5).</summary>
        public SocialStanding Standing { get; private set; }

        /// <summary>The slice's one world seed; every random choice forks it by name (SliceSeedStreams).</summary>
        public GameDev.World.Seed.IWorldSeed Seed { get; private set; }

        /// <summary>07.1's curve for action checks outside a fight (S4.1).</summary>
        public ActionCheckRules ActionChecks => settings != null ? settings.ActionChecks : null;

        private DialogueContext _talk;

        /// <summary>10.4 §2–3's numbers, the Intelligence that shows a check's chance and its modifiers among them.</summary>
        public SocialCheckRules CheckRules => settings != null ? settings.SocialChecks : null;

        /// <summary>
        /// Opens a conversation (10.4; S2.4): it pauses the world until it ends. Approaches a later milestone builds
        /// stay hidden: Veil and Confront until S3 (owner, 2026-11-09).
        /// </summary>
        /// <summary>A conversation; its Veil options are offered only while the character is in Stealth Mode (S3b.3).</summary>
        public IDialogueRunner Talk(DialogueDefinition dialogue, string startNode = null)
        {
            _talk.Withheld = Hiding != null && Hiding.Active ? new HashSet<Approach>() : new HashSet<Approach> { Approach.Veil };
            return new DialogueSession(dialogue, _talk, startNode);
        }

        /// <summary>
        /// Development command (S2 authoring default): every attribute and skill of the character to the next of these
        /// values, so the chance (Intelligence 10) and its modifiers (20) can be seen before S5b's character creation.
        /// </summary>
        public static readonly int[] DevelopmentValues = { CharacterSheet.Lowest, 10, 20 };

        public void SetCharacterValues(int value)
        {
            foreach (CharacterAttribute attribute in Enum.GetValues(typeof(CharacterAttribute)))
            {
                Character.Set(attribute, value);
            }

            foreach (CharacterSkill skill in Enum.GetValues(typeof(CharacterSkill)))
            {
                Character.Set(skill, value);
            }
        }

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
            SceneManager.sceneLoaded += OnSceneLoaded;
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

            if (settings.SocialChecks == null || settings.Standing == null)
            {
                Debug.LogError("SliceSettings needs the SocialCheckRules and the StandingRules.", settings);
                enabled = false;
                return;
            }

            var seed = settings.CreateSeed();
            Seed = seed;
            State = new WorldState();
            Director = new PoiDirector(pois, State, seed, SliceSeedStreams.PoiUnattended, Clock);
            Director.Evaluate(Clock.Tick);
            Tips = new TipBook(tips, State, Director);
            Character = new CharacterSheet();
            if (settings.Items != null)
            {
                Bag = StartingBag(settings.Items, Character);
                Bag.Changed += Refresh;
                Outfit = new Outfit(Bag);
                Outfit.Wear(Bag.Pieces[0]);
                Outfit.Changed += Refresh;
            }

            if (settings.FightRules != null)
            {
                Fights = gameObject.AddComponent<SliceConfront>();
                Fights.Configure(settings, seed, Clock, Character, State);
            }
            if (Fights != null && Outfit != null && settings.Fists != null)
            {
                Outfit.Locked = () => Fights.InFight;
                Fights.WornFighter = () => Outfit.Fighter(settings.FightRules, Character, settings.Fists);
            }

            if (Fights != null && Bag != null)
            {
                Fights.FirstAid = new SliceFirstAid(settings.FightRules, Bag, State, Clock, Character, seed);
            }

            if (Bag != null && settings.Shops != null)
            {
                Shops = new SliceShops(settings.Shops, settings.Items, seed, Clock, Bag, State);
                Workshop = new SliceWorkshop(Shops, settings.Items, seed, Clock, Bag, State, Character);
                OpenCounters();
            }

            Standing = new SocialStanding(State, settings.Standing, Clock, seed, SliceSeedStreams.Lies);
            if (Fights != null)
            {
                Fights.Standing = Standing;
            }

            DebugSight.On = Debug.isDebugBuild;
            if (settings.StealthRules != null)
            {
                Hiding = gameObject.AddComponent<SliceStealth>();
                Hiding.Configure(settings.StealthRules, Character, Clock, () => Fights != null && Fights.InFight);
                Hiding.FogSpeaks = () => State.IsSet(SliceStealth.FogKey);
                if (Fights != null)
                {
                    Fights.Hiding = Hiding;
                }

                if (settings.BeastRules != null)
                {
                    Beasts = gameObject.AddComponent<GameDev.Slice.Beasts.SliceBeasts>();
                    Beasts.Configure(settings.BeastRules, Hiding, Fights, Clock, State, seed);
                }
            }
            if (Bag != null && settings.Gathering != null && settings.ActionChecks != null)
            {
                Stamina = new SliceStamina(settings.Gathering, State, Clock, Character);
                Gathering = new SliceGathering(settings.Gathering, Bag, State, Clock, Character, Stamina, settings.ActionChecks, seed);
                if (Beasts != null)
                {
                    Beasts.Warned += (_, _) => Gathering.Stop("ui.gather.stop.beast");
                }

                FindNodes();
            }

            if (Bag != null && Beasts != null && settings.ActionChecks != null)
            {
                Remains = new GameDev.Slice.Beasts.SliceRemains(settings.BeastRules, Bag, State, Clock, Character, settings.ActionChecks, seed);
                FindCarcasses();
            }

            _talk = new DialogueContext
            {
                State = State,
                Clock = Clock,
                Seed = seed,
                CheckStream = SliceSeedStreams.SocialChecks,
                Rules = settings.SocialChecks,
                Standing = Standing,
                Character = Character,
                Director = Director,
                Tips = Tips,
                Withheld = new HashSet<Approach> { Approach.Veil },
            };
            Notes = new CharacterNotes(Tips);
            Notes.Added += _ => Refresh();
            State.Changed += (_, _, _) => Refresh();
            Loc.Changed += Refresh;
        }

        // The pages belong to whichever scene's HUD is up; the keys work in every scene the world lives through.
        private void Update()
        {
            var input = GameInputService.Instance;
            if (Notes == null || input == null)
            {
                return;
            }

            if (input.LanguagePressed)
            {
                Loc.Next();
            }

            if (input.DevCharacterValuesPressed && Debug.isDebugBuild)
            {
                var now = Character.Get(CharacterAttribute.Intelligence);
                var next = DevelopmentValues.FirstOrDefault(v => v > now);
                SetCharacterValues(next == 0 ? DevelopmentValues[0] : next);
                Debug.Log($"[Slice] Development command: every attribute and skill of the character is {Character.Get(CharacterAttribute.Intelligence)}.");
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

            if (input.BagPressed && Bag != null)
            {
                if (hud.IsBagOpen)
                {
                    hud.CloseBag();
                }
                else
                {
                    _bagNote = null;
                    ShowBag(hud);
                }
            }

            FreeCursorForPage(hud.IsBagOpen || hud.IsShopOpen);

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

        private string _bagNote;
        private ThirdPersonCamera _freedLook;

        /// <summary>
        /// The bag page (S5a.1–S5a.2): each piece's line says where it is worn and how much of it works, and a click puts it
        /// on or takes it off; the note says why a click was refused.
        /// </summary>
        private void ShowBag(HudView hud)
        {
            var actions = new List<Action>();
            actions.AddRange(Enumerable.Repeat<Action>(null, Bag.Stacks.Count));
            foreach (var piece in Bag.Pieces)
            {
                var p = piece;
                actions.Add(Outfit != null ? () => TogglePiece(p) : (Action)null);
            }

            hud.ShowBag(BagPage.Money(Bag), BagPage.Load(Bag), BagPage.Lines(Bag, PieceNote), actions,
                _bagNote != null ? Loc.Get(_bagNote) : Outfit != null ? Loc.Get("ui.hud.bag.hint") : null);
        }

        private void TogglePiece(ItemPiece piece)
        {
            _bagNote = Outfit.Toggle(piece);
            Refresh();
        }

        private string PieceNote(ItemPiece piece)
        {
            if (Outfit == null || !(Outfit.SlotOf(piece) is WearSlot slot))
            {
                return null;
            }

            var note = Loc.Format(slot >= WearSlot.WeaponOne ? "ui.hud.bag.held" : "ui.hud.bag.worn", Outfit.IsSecondSet(slot) ? 2 : 1);
            var fit = Outfit.FitOf(piece, Character);
            return fit < 0.995d ? note + Loc.Format("ui.hud.bag.fit", Mathf.RoundToInt((float)(fit * 100d))) : note;
        }

        /// <summary>The cursor is free while the bag or a shop is open outside a fight, so their lines can be clicked, and the look waits.</summary>
        private void FreeCursorForPage(bool open)
        {
            var free = open && (Fights == null || !Fights.InFight);
            if (free && _freedLook == null)
            {
                _freedLook = FindAnyObjectByType<ThirdPersonCamera>();
                if (_freedLook != null)
                {
                    _freedLook.LookEnabled = false;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
            else if (!free && _freedLook != null)
            {
                _freedLook.LookEnabled = true;
                _freedLook = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Shops != null)
            {
                OpenCounters();
            }

            if (Gathering != null)
            {
                FindNodes();
            }

            if (Remains != null)
            {
                FindCarcasses();
            }
        }

        /// <summary>Remains on each beast in the loaded scenes, to take from once it has fallen (S5a.6).</summary>
        private void FindCarcasses()
        {
            var added = false;
            foreach (var beast in FindObjectsByType<GameDev.Slice.Beasts.SliceBeast>(FindObjectsInactive.Exclude))
            {
                if (beast.GetComponent<GameDev.Slice.Beasts.BeastRemains>() == null)
                {
                    beast.gameObject.AddComponent<GameDev.Slice.Beasts.BeastRemains>().Configure(Remains, () => Fights != null && Fights.InFight);
                    added = true;
                }
            }

            var hud = Hud();
            if (added && hud != null)
            {
                hud.FindPromptSources();
            }
        }

        /// <summary>A gathering node on each herb, flax, fellable tree and iron vein block in the loaded scenes (S5a.5).</summary>
        private void FindNodes()
        {
            var added = false;
            foreach (var block in FindObjectsByType<KitBlock>(FindObjectsInactive.Exclude))
            {
                var rule = Gathering.Rules.RuleFor(block.name);
                if (rule == null || block.GetComponent<GatherNode>() != null)
                {
                    continue;
                }

                var at = block.transform.position;
                var id = $"{rule.kind.ToString().ToLowerInvariant()}.{Mathf.RoundToInt(at.x * 10f)}.{Mathf.RoundToInt(at.z * 10f)}";
                block.gameObject.AddComponent<GatherNode>().Configure(Gathering, Gathering.Spot(id, rule), () => Fights != null && Fights.InFight);
                added = true;
            }

            var hud = Hud();
            if (added && hud != null)
            {
                hud.FindPromptSources();
            }
        }

        /// <summary>A counter on each keeper in the loaded scenes, and the HUD told of them (S5a.3).</summary>
        private void OpenCounters()
        {
            var added = false;
            foreach (var person in FindObjectsByType<NamedPerson>(FindObjectsInactive.Exclude))
            {
                if (Shops.Find(person.Id) != null && person.GetComponent<ShopCounter>() == null)
                {
                    person.gameObject.AddComponent<ShopCounter>().Configure(person.Id);
                    added = true;
                }
            }

            var hud = Hud();
            if (added && hud != null)
            {
                hud.FindPromptSources();
            }
        }

        private void Refresh()
        {
            var hud = Hud();
            if (hud != null && hud.IsNotesOpen)
            {
                hud.ShowNotes(Notes.Lines);
            }

            if (hud != null && hud.IsBagOpen && Bag != null)
            {
                ShowBag(hud);
            }
        }

        private HudView Hud()
        {
            // A city kept behind an interior keeps its HUD, switched off (17 I.2): look again for the one shown.
            if (_hud == null || !_hud.isActiveAndEnabled)
            {
                _hud = FindAnyObjectByType<HudView>();
            }

            return _hud;
        }

        private void OnDestroy()
        {
            if (_running == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Loc.Changed -= Refresh;
                Standing?.Dispose();
                Director?.Dispose();
                _running = null;
            }
        }
    }
}

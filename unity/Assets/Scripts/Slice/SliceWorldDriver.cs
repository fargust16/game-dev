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
using GameDev.Slice.Save;
using GameDev.Slice.Shops;
using GameDev.Slice.Stealth;
using GameDev.UI;
using GameDev.World.Poi;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using UnityEngine;
using UnityEngine.SceneManagement;
using PanelSettings = UnityEngine.UIElements.PanelSettings;
using UIDocument = UnityEngine.UIElements.UIDocument;

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
        private PlayerLocomotion _walker;

        /// <summary>The running world, or null before a driver has started.</summary>
        public static SliceWorldDriver Running => _running != null && _running.Tips != null ? _running : null;

        public WorldState State { get; private set; }

        public PoiDirector Director { get; private set; }

        public TipBook Tips { get; private set; }

        /// <summary>Veilmouth's News Pool (09; PB8d): what the city is talking about, told by the Notice Board.</summary>
        public GameDev.Slice.News.SliceNews News { get; private set; }

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

        /// <summary>A Reaction Check on the HUD (S5a.8): theft's now, breaking in's and brewing's later.</summary>
        public SliceReaction Reaction { get; private set; }

        /// <summary>The player's work by its animation, each Reaction Check at its key frame (AN3, 07.1 §8a).</summary>
        public PlayerWork Work { get; private set; }

        /// <summary>The weapon drawn on R in free exploration and a click's blow (10.2 §5c); null without fights or fists.</summary>
        public PlayerArms Arms { get; private set; }

        /// <summary>10.2 §5e: the firearm's aim through the sights and its shot out of a fight (PB8b).</summary>
        public PlayerAim Aim { get; private set; }

        /// <summary>10.4 §6 *Accusation* (WA5): a Warden stops a character of low Reputation.</summary>
        public WardenAccusation Accusation { get; private set; }

        /// <summary>Theft from the townsfolk (S5a.8); null without a bag or the Deeds Record.</summary>
        public GameDev.Slice.Theft.SliceTheft Theft { get; private set; }

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

        /// <summary>CR4: the page of Prepared Abilities, on K out of a fight.</summary>
        public PreparedPage Prepared { get; private set; }

        /// <summary>Stealth in free movement (S3b.1); a fight takes it over while it lasts.</summary>
        public SliceStealth Hiding { get; private set; }

        /// <summary>The beasts' territories outside the walls (S3b.2).</summary>
        public GameDev.Slice.Beasts.SliceBeasts Beasts { get; private set; }

        /// <summary>The Deeds Record, Attitude and lies (10.4 §4–5).</summary>
        public SocialStanding Standing { get; private set; }

        /// <summary>The Suspend Save (17 X.3, S5c.1): "Save and quit" on the Esc menu, and the start's offer to continue.</summary>
        public SliceSave Save { get; private set; }

        /// <summary>Whether a conversation is open on the HUD.</summary>
        public bool TalkOpen => Hud() is HudView hud && hud.IsDialogueOpen;

        private bool _offer;

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
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                MarkWater(SceneManager.GetSceneAt(i));
            }

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
            News = new GameDev.Slice.News.SliceNews(State, seed);
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
                Fights.News = News;
                Prepared = gameObject.AddComponent<PreparedPage>();
                Prepared.Configure(Fights);
            }
            if (Fights != null && Outfit != null && settings.Fists != null)
            {
                Outfit.Locked = () => Fights.InFight;
                Fights.WornFighter = () => Outfit.Fighter(settings.FightRules, Character, settings.Fists, Fights.PlayerLimit);
            }

            if (Fights != null && Bag != null)
            {
                Fights.FirstAid = new SliceFirstAid(settings.FightRules, Bag, State, Clock, Character, seed);
            }

            Reaction = gameObject.AddComponent<SliceReaction>();
            Work = gameObject.AddComponent<PlayerWork>();
            Work.Configure(Reaction, Clock);
            if (Fights != null && Fights.WornFighter != null)
            {
                Arms = gameObject.AddComponent<PlayerArms>();
                Arms.Configure(settings, Fights.WornFighter, () => Fights.InFight, () => Work.Busy || PageOpen());
                Arms.People = PeopleAround.InSight;
                Arms.Assault = target => Fights.StartAssault(target);
                Aim = gameObject.AddComponent<PlayerAim>();
                Aim.Configure(Arms);
                Aim.Touched = Shot;
                Arms.Aim = Aim;
                Accusation = gameObject.AddComponent<WardenAccusation>();
                Accusation.Configure(this);
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
                Stamina = new SliceStamina(settings.Gathering, State, Clock, Character, GameClockDriver.RealSecondsPerTick);
                Gathering = new SliceGathering(settings.Gathering, Bag, State, Clock, Character, Stamina, seed);
                if (Workshop != null)
                {
                    Workshop.Stamina = Stamina;
                }

                if (Beasts != null)
                {
                    Beasts.Warned += (_, _) => Gathering.Stop("ui.gather.stop.beast");
                }

                FindNodes();
            }

            if (Bag != null && Beasts != null)
            {
                Remains = new GameDev.Slice.Beasts.SliceRemains(settings.BeastRules, Bag, State, Clock, Character, seed);
                FindCarcasses();
            }

            if (Bag != null && Standing != null)
            {
                Theft = new GameDev.Slice.Theft.SliceTheft(Bag, State, Clock, Character, Standing, seed);
                FindPurses();
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
            Save = gameObject.AddComponent<SliceSave>();
            Save.Configure(this, cityScene);
            _offer = Save.ShouldOffer();
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

            KeepStamina(hud);
            Tune(hud);
            if (_offer)
            {
                _offer = false;
                OpenStart(hud);
            }

            if (_menuPaused && !hud.IsMenuOpen)
            {
                Unpause(); // the menu closed by its own line or with its scene
            }

            if (input.CancelPressed)
            {
                Escape(hud);
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

            FreeCursorForPage(hud.IsBagOpen || hud.IsShopOpen || hud.IsMenuOpen);

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

        // ------------------------------------------------------------------------------------------- Esc and the menu

        private bool _windowWasOpen;
        private bool _menuPaused;
        private IDisposable _menuHold;
        private float _menuTimeScale = 1f;

        /// <summary>
        /// Esc (owner, 2026-11-12): the menu closes if it is open; otherwise any window open closes, the pages here and the
        /// shop, the anvil and the conversation by their own hands, which hear the same key; with no window, the menu
        /// opens. A window open at the end of the last frame counts, so one closed earlier this frame by its own hand
        /// never opens the menu behind it.
        /// </summary>
        private void Escape(HudView hud)
        {
            if (hud.IsStartOpen)
            {
                return; // the start's offer is answered by its lines
            }

            if (hud.IsMenuOpen)
            {
                CloseMenu(hud);
                return;
            }

            if (_windowWasOpen || hud.AnyWindowOpen)
            {
                hud.CloseNotes();
                hud.CloseBag();
                hud.CloseMap();
                hud.CloseReading();
                return;
            }

            OpenMenu(hud);
        }

        /// <summary>The menu, the world stopped under it: its clock paused and the frame's time at a stand.</summary>
        private void OpenMenu(HudView hud)
        {
            hud.ShowMenu(() => CloseMenu(hud), Loc.Next, () => SaveAndQuit(hud), Quit);
            PauseUnderMenu(hud);
        }

        /// <summary>17 X.3: the Suspend Save written and the game left; a Screen Message says why it must wait.</summary>
        private void SaveAndQuit(HudView hud)
        {
            var refused = Save != null ? Save.Write() : "ui.save.refused.busy";
            if (refused != null)
            {
                hud.ShowMessage(Loc.Get(refused));
                return;
            }

            Quit();
        }

        /// <summary>The start's offer of a saved run (17 X.3), the world stopped under it as under the menu.</summary>
        private void OpenStart(HudView hud)
        {
            hud.ShowStart(
                () =>
                {
                    CloseMenu(hud);
                    Save.Continue();
                },
                () =>
                {
                    Save.Decline();
                    CloseMenu(hud);
                });
            PauseUnderMenu(hud);
        }

        private void PauseUnderMenu(HudView hud)
        {
            if (!hud.IsMenuOpen || _menuPaused)
            {
                return;
            }

            _menuPaused = true;
            _menuHold = Clock?.Pause();
            _menuTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        private void CloseMenu(HudView hud)
        {
            hud.CloseMenu();
            Unpause();
        }

        private void Unpause()
        {
            if (!_menuPaused)
            {
                return;
            }

            _menuPaused = false;
            _menuHold?.Dispose();
            _menuHold = null;
            Time.timeScale = _menuTimeScale > 0f ? _menuTimeScale : 1f;
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void LateUpdate()
        {
            var hud = Hud();
            _windowWasOpen = hud != null && hud.AnyWindowOpen;
        }

        // ------------------------------------------------------------------------------------------- the tuning page

        private PlayerLocomotion _tuned;
        private PanelSettings _panel;
        private Vector2Int _panelResolution;
        private float _panelMatch;

        /// <summary>
        /// The owner's tuning page (<see cref="SliceTuning"/>) on what it governs here, each frame, so a change made while
        /// the game runs shows at once: the landing, and the HUD's size and fit. The HUD's panel is given back as it was
        /// when the world ends.
        /// </summary>
        private void Tune(HudView hud)
        {
            var tuning = SliceTuning.Current;
            if (_tuned == null)
            {
                _tuned = FindAnyObjectByType<PlayerLocomotion>();
            }

            if (_tuned != null)
            {
                _tuned.LandingHold = tuning.Jump.KneesHeldSeconds;
                _tuned.LandingShare = tuning.Jump.PlayedShareWithoutKeys;
                _tuned.LandingAfter = tuning.Jump.FallBeforeKneesSeconds;
            }

            var document = hud.GetComponent<UIDocument>();
            var panel = document != null ? document.panelSettings : null;
            if (panel == null)
            {
                return;
            }

            if (panel != _panel)
            {
                RestorePanel();
                _panel = panel;
                _panelResolution = panel.referenceResolution;
                _panelMatch = panel.match;
            }

            var scale = Mathf.Clamp(tuning.Ui.Scale, 0.5f, 1.5f);
            var wanted = new Vector2Int(Mathf.RoundToInt(_panelResolution.x / scale), Mathf.RoundToInt(_panelResolution.y / scale));
            if (panel.referenceResolution != wanted)
            {
                panel.referenceResolution = wanted;
            }

            var fit = Mathf.Clamp01(tuning.Ui.FitToHeight);
            if (!Mathf.Approximately(panel.match, fit))
            {
                panel.match = fit;
            }
        }

        private void RestorePanel()
        {
            if (_panel != null)
            {
                _panel.referenceResolution = _panelResolution;
                _panel.match = _panelMatch;
            }

            _panel = null;
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

        /// <summary>A click puts a piece on or takes it off; Shift and a click holds a one-handed melee weapon in the other hand (10.2 §5b *Two weapons*).</summary>
        private void TogglePiece(ItemPiece piece)
        {
            var shift = GameInputService.Instance != null && GameInputService.Instance.SprintHeld;
            _bagNote = shift && !Outfit.IsWorn(piece) ? Outfit.WearOffHand(piece) : Outfit.Toggle(piece);
            Refresh();
        }

        private string PieceNote(ItemPiece piece)
        {
            if (Outfit == null || !(Outfit.SlotOf(piece) is WearSlot slot))
            {
                return null;
            }

            var other = (slot == WearSlot.OffHandOne || slot == WearSlot.OffHandTwo) && Bag.Catalogue.Find(piece.item)?.fight is GameDev.Confront.Rules.FightWeapon;
            var note = Loc.Format(other ? "ui.hud.bag.held_off" : slot >= WearSlot.WeaponOne ? "ui.hud.bag.held" : "ui.hud.bag.worn", Outfit.IsSecondSet(slot) ? 2 : 1);
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
            MarkWater(scene);
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

            if (Theft != null)
            {
                FindPurses();
            }
        }

        /// <summary>
        /// AN5: the city's sheet of water at Sea Level, named Water by its builder, is water the character swims in (10.1);
        /// the sheet has no collider, because the grid reads water by height.
        /// </summary>
        private static void MarkWater(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Water" && root.GetComponent<SwimWater>() == null)
                {
                    root.AddComponent<SwimWater>();
                }
            }
        }

        /// <summary>A purse to lift on each walking townsperson in the loaded scenes (S5a.8).</summary>
        private void FindPurses()
        {
            var added = false;
            foreach (var walker in FindObjectsByType<TownsfolkWalker>(FindObjectsInactive.Exclude))
            {
                if (walker.GetComponent<GameDev.Slice.Theft.PurseTarget>() == null)
                {
                    walker.gameObject.AddComponent<GameDev.Slice.Theft.PurseTarget>().Configure(Theft, Work, () => Fights != null && Fights.InFight, () => Hiding != null && Hiding.Active);
                    added = true;
                }
            }

            var hud = Hud();
            if (added && hud != null)
            {
                hud.FindPromptSources();
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
                    beast.gameObject.AddComponent<GameDev.Slice.Beasts.BeastRemains>().Configure(Remains, () => Fights != null && Fights.InFight, Work);
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
                block.gameObject.AddComponent<GatherNode>().Configure(Gathering, Gathering.Spot(id, rule), () => Fights != null && Fights.InFight, Work);
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

                added |= Workshop != null && Shops.Find(person.Id)?.workshop == true && OpenAnvil(person);
            }

            var hud = Hud();
            if (added && hud != null)
            {
                hud.FindPromptSources();
            }
        }

        /// <summary>Authoring default, m: how far from its Master a workshop's anvil may stand.</summary>
        private const float AnvilReach = 25f;

        /// <summary>
        /// The anvil nearest a workshop's Master becomes its crafting station (19 §3, the owner's play test of S5a); true
        /// when one was opened now.
        /// </summary>
        private static bool OpenAnvil(NamedPerson master)
        {
            var at = master.transform.position;
            var anvil = FindObjectsByType<KitBlock>(FindObjectsInactive.Exclude)
                .Where(b => b.name.StartsWith("Kit_Market_Anvil") && (b.transform.position - at).sqrMagnitude < AnvilReach * AnvilReach)
                .OrderBy(b => (b.transform.position - at).sqrMagnitude)
                .FirstOrDefault();
            if (anvil == null || anvil.GetComponent<CraftStation>() != null)
            {
                return false;
            }

            anvil.gameObject.AddComponent<CraftStation>().Configure(master.Id);
            return true;
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

        /// <summary>
        /// 07.2 §3.2: the sprint spends Stamina each real second and holds its return; run out, the character cannot
        /// sprint until a tenth has come back. The HUD shows its bar while any is spent.
        /// </summary>
        private void KeepStamina(HudView hud)
        {
            if (Stamina == null)
            {
                return;
            }

            if (_walker == null || !_walker.gameObject.activeInHierarchy)
            {
                _walker = FindAnyObjectByType<PlayerLocomotion>();
            }

            var sprinting = _walker != null && _walker.isActiveAndEnabled && _walker.IsSprinting;
            if (sprinting != Stamina.Sprinting)
            {
                Stamina.Sprinting = sprinting;
            }

            if (sprinting)
            {
                Stamina.Spend(SliceStamina.SprintShare * Time.deltaTime);
            }
            else
            {
                Stamina.CatchUp();
            }

            var tired = Stamina.Tired;
            if (_walker != null)
            {
                _walker.SprintAllowed = !tired;

                // 10.1: an overloaded character swims at half speed (Overloaded and Critical Overload, 07.2 §5.2).
                _walker.SwimShare = Bag != null && Bag.Band >= LoadBand.Overloaded ? 0.5f : 1f;
            }

            hud.ShowStamina((float)Stamina.Share, tired);
        }

        /// <summary>
        /// 10.2 §5e: what a shot out of a fight touched, fired from <paramref name="from"/>: a beast or a person is rolled
        /// and turns to the shot; anything else, or nothing, is a miss with no roll.
        /// </summary>
        private void Shot(Collider touched, Vector3 at, Vector3 from)
        {
            if (touched == null || Fights == null || Fights.InFight)
            {
                return;
            }

            var beast = touched.GetComponentInParent<GameDev.Slice.Beasts.SliceBeast>();
            if (beast != null)
            {
                Beasts?.Shot(beast, from);
                return;
            }

            foreach (var body in PeopleAround.InSight())
            {
                if (body != null && touched.transform.IsChildOf(body))
                {
                    Fights.PersonShot(body, from);
                    return;
                }
            }
        }

        // A conversation, a page or a shop puts the weapon away (10.2 §5c).
        private bool PageOpen()
        {
            var hud = Hud();
            return hud != null && hud.AnyWindowOpen;
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
            Unpause();
            RestorePanel();
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

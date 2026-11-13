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

        /// <summary>Alchemy at the three benches (S5b.4; 20 §3); null without the shops.</summary>
        public GameDev.Slice.Alchemy.SliceAlchemy Alchemy { get; private set; }

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

        /// <summary>Break-ins (S5c.5; 10.3 §6.1–6.2): locks, Lockpicks, the Wanted and the night's loot.</summary>
        public GameDev.Slice.BreakIns.SliceBreakIns BreakIns { get; private set; }

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

        /// <summary>The city's scene, where the character sleeps in the street (S5c.2).</summary>
        public string CityScene => cityScene;

        /// <summary>Sleep (S5c.2; 18 §3): the page, the passing night, its visitors and its recovery.</summary>
        public GameDev.Slice.Mind.SleepRun Sleep { get; private set; }

        /// <summary>The people's hours (S5c.3; 18 *Townsfolk in the slice*): who stands where at each hour, and the walkers at night.</summary>
        public PersonHours Hours { get; private set; }

        /// <summary>The first-day trial and Faction Entry (S5c.4; SLICE_TRIAL §1); null without the bag.</summary>
        public GameDev.Slice.Trial.SliceTrial Trial { get; private set; }

        /// <summary>The page of the Faction Gift after a conversation with Ashgrave (S5c.4; 19 §6.2).</summary>
        public GameDev.Slice.Trial.TrialRun TrialRun { get; private set; }

        /// <summary>
        /// The hours switched people on or off (S5c.3): a keeper come to work gets its counter, a walker back from home its
        /// purse, and the HUD looks again for names and prompts.
        /// </summary>
        public void PeopleChanged()
        {
            if (Shops != null)
            {
                OpenCounters();
            }

            if (Theft != null)
            {
                FindPurses();
            }

            Hud()?.FindPromptSources();
        }

        /// <summary>The character's Sanity (07.2 §3.4; S5b.5 spends it on the Limit potions).</summary>
        public GameDev.Slice.Mind.SliceSanity Sanity { get; private set; }

        /// <summary>Drinking a Limit potion and its Assimilation (S5b.5; 20 §3); null without the bag.</summary>
        public GameDev.Slice.Alchemy.SliceAdvancement Advancement { get; private set; }

        /// <summary>The Assimilation's ticks passing in the scene, the character crouched (S5b.5).</summary>
        public GameDev.Slice.Alchemy.AssimilationRun Assimilation { get; private set; }

        /// <summary>07 *Creation choices in V1* (S5b.1): the page at a new run's start; null before the world starts.</summary>
        public GameDev.Slice.Creation.CharacterCreation Creation { get; private set; }

        /// <summary>Who the character is: the name, the sex and the body, chosen at creation and kept in the save.</summary>
        public GameDev.Slice.Creation.CharacterIdentity Identity { get; set; } = new GameDev.Slice.Creation.CharacterIdentity();

        private bool CreationOpen => Creation != null && Creation.IsOpen;

        private bool _offer;
        private bool _create;

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
                Fights.WornFighter = () => Outfit.Fighter(settings.FightRules, Character, settings.Fists, Fights.PlayerLimit, Mind != null ? Mind.FightShift : default);
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
            // S5c.3: the people's hours before the counters, so a keeper away gets none.
            Hours = gameObject.AddComponent<PersonHours>();
            Hours.Configure(this);
            if (Bag != null && settings.Shops != null)
            {
                Shops = new SliceShops(settings.Shops, settings.Items, seed, Clock, Bag, State);
                Shops.KeeperAtWork = Hours.AtWork;
                Workshop = new SliceWorkshop(Shops, settings.Items, seed, Clock, Bag, State, Character);
                Alchemy = new GameDev.Slice.Alchemy.SliceAlchemy(Shops, settings.Items, seed, Clock, Bag, State, Character);
                OpenCounters();
            }

            Standing = new SocialStanding(State, settings.Standing, Clock, seed, SliceSeedStreams.Lies);
            if (Fights != null)
            {
                Fights.Standing = Standing;
            }

            Sanity = new GameDev.Slice.Mind.SliceSanity(State);
            Mind = new GameDev.Slice.Mind.SliceMind(State, Clock, Sanity);
            Mind.Lost += LostMind;
            Reaction.Narrowing = () => Mind.ReactionShare;
            if (Fights != null)
            {
                Fights.Mind = Mind;
            }

            Symptoms = gameObject.AddComponent<GameDev.Slice.Mind.MindSymptoms>();
            Symptoms.Configure(this, seed);
            if (Bag != null)
            {
                Advancement = new GameDev.Slice.Alchemy.SliceAdvancement(State, Clock, Bag, Character, Sanity, Standing, () => Fights != null && Fights.InFight);
                Advancement.Advanced += Advanced;
                Trial = new GameDev.Slice.Trial.SliceTrial(State, Bag, Tips, seed, settings.Items, () => Clock.Tick);
                OpenTrialDoor(); // the first scene's counters were opened before the trial existed
                Conduct = new GameDev.Slice.Alchemy.SliceConduct(State, Advancement);
                Conduct.Felt += key => Hud()?.ShowMessage(Loc.Get(key)); // 05.1: one short line, no counter
                if (Fights != null)
                {
                    Fights.Conduct = Conduct;
                }
                Clock.TickBoundary += SettlePotion;
                Assimilation = gameObject.AddComponent<GameDev.Slice.Alchemy.AssimilationRun>();
                Assimilation.Configure(this);
                if (Fights != null)
                {
                    Fights.Defenceless = () => Advancement.AssimilationLeft;
                }
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
                    Hiding.Screened = Fights.Screened; // 05.1 Пелена out of a fight (S5b.8)
                    Hiding.Kin = Fights.SensesKin;     // 05.1 Родство
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
                Stamina.RecoveryShare = () => Mind.StaminaShare;
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
                Theft = new GameDev.Slice.Theft.SliceTheft(Bag, State, Clock, Character, Standing, seed) { Conduct = Conduct };
                BreakIns = new GameDev.Slice.BreakIns.SliceBreakIns(Bag, State, Clock, Character, Standing, seed) { Conduct = Conduct }; // S5c.5
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
                MindPenalty = () => Mind.SocialPenalty,
                SkillBonus = skill => Fights != null ? Fights.SuggestionFor(skill) : 0,
                Checked = TalkChecked,
            };
            if (Fights != null)
            {
                // 05.1 *Вне боя* (S5b.8): the keys use the abilities only in free exploration.
                Fights.OutsideBusy = () => (Work != null && Work.Busy) || PageOpen() || (Advancement != null && Advancement.Assimilating) || (Sleep != null && Sleep.Sleeping);
            }
            Notes = new CharacterNotes(Tips);
            Notes.Added += _ => Refresh();
            State.Changed += (_, _, _) => Refresh();
            Loc.Changed += Refresh;
            Save = gameObject.AddComponent<SliceSave>();
            Save.Configure(this, cityScene);
            Save.Refusals.Add(() => Advancement != null && Advancement.Assimilating ? "ui.save.refused.settling" : null); // 05.1 *Assimilation*
            Sleep = gameObject.AddComponent<GameDev.Slice.Mind.SleepRun>();
            Sleep.Configure(this);
            TrialRun = gameObject.AddComponent<GameDev.Slice.Trial.TrialRun>();
            TrialRun.Configure(this);
            Save.Refusals.Add(() => Sleep.Sleeping ? "ui.save.refused.sleep" : null); // 17 X.3: not while asleep
            _offer = Save.ShouldOffer();
            Creation = gameObject.AddComponent<GameDev.Slice.Creation.CharacterCreation>();
            Creation.Configure(Character, settings.PlayerLooks);
            Identity = new GameDev.Slice.Creation.CharacterIdentity { look = Creation.FirstLook(false)?.id ?? string.Empty };
            _create = !_offer && GameDev.Slice.Creation.CharacterCreation.ShouldOffer();
            Page = gameObject.AddComponent<GameDev.Slice.Creation.CharacterPage>();
            Page.Configure(Character, Fights, () => Identity?.name, Mind);
            Character.AttributeGrew += (a, level) => Grew(GameDev.Slice.Creation.CharacterPage.AttributeName(a), level);
            Character.SkillGrew += (s, level) => Grew(PersonTalk.SkillName(s), level);
        }

        /// <summary>S5b.2: the character's page, on L out of a fight.</summary>
        public GameDev.Slice.Creation.CharacterPage Page { get; private set; }

        private void SettlePotion(long tick) => Advancement?.Settle();

        /// <summary>The character's Conduct Progress toward the next Limit (S5b.7; 05.1).</summary>
        public GameDev.Slice.Alchemy.SliceConduct Conduct { get; private set; }

        /// <summary>05.1: a Convince or Threaten check won is a deed, worth 2 when its success settles a POI.</summary>
        private void TalkChecked(DialogueOption option, string listener, bool success)
        {
            if (success && Conduct != null)
            {
                Conduct.Checked(option.approach, listener, option.success != null && option.success.settles != null);
            }

            Fights?.Persuaded(option.skill, listener, success); // 05.1 Внушение is spent on the check
        }

        /// <summary>The character's Sanity, its band and Fatigue, and what they take away (S5b.6; 07.2 §3.3–3.4).</summary>
        public GameDev.Slice.Mind.SliceMind Mind { get; private set; }

        /// <summary>The symptoms a troubled mind perceives (S5b.6).</summary>
        public GameDev.Slice.Mind.MindSymptoms Symptoms { get; private set; }

        /// <summary>07.2 §3.4: at Sanity 0 the character is no longer the player's, and the run ends.</summary>
        private void LostMind()
        {
            var player = FindAnyObjectByType<PlayerLocomotion>();
            if (player != null)
            {
                player.enabled = false;
            }

            Debug.Log("[Slice] Sanity reached 0: the run ends.");
            GameDev.Slice.Fights.SliceDeathScreen.Show(Clock, GameDev.Slice.Fights.SliceDeathScreen.SanityTitle);
        }

        /// <summary>A Limit potion settled (S5b.5): a Screen Message names the Arcana and the Limit reached (07.1 §11).</summary>
        private void Advanced(Arcana arcana, int limit, int cost)
        {
            Hud()?.ShowMessage(Loc.Format("ui.limit.reached", Loc.Get(SliceConfront.ArcanaName(arcana)), limit));
            Trial?.Advanced(arcana, limit);
            Debug.Log($"[Slice] Limit {limit} of {arcana} reached; Sanity -{cost}, now {Sanity.Value}.");
            Refresh();
        }

        /// <summary>
        /// A value grew by use (07.1 §3): a Screen Message names it and its new level (07.1 §11), every level of an
        /// attribute and of a skill alike (authoring default, S5b.2).
        /// </summary>
        private void Grew(string name, int level)
        {
            Hud()?.ShowMessage(Loc.Format("ui.character.grew", name, level));
            Debug.Log($"[Slice] {name} grew to {level} by use.");
        }

        /// <summary>The body <see cref="Identity"/> names worn on <paramref name="player"/> (a loaded run's city is new).</summary>
        public void WearLook(PlayerLocomotion player)
        {
            var look = Creation != null ? Creation.Find(Identity?.look) : null;
            if (look != null)
            {
                GameDev.Slice.Creation.PlayerBody.Wear(player, look);
            }
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
            else if (_create)
            {
                _create = false;
                OpenCreation(hud);
            }

            if (_menuPaused && !hud.IsMenuOpen && !CreationOpen)
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
                    _drinkAsked = null;
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
            if (hud.IsStartOpen || CreationOpen)
            {
                return; // the start's offer and the creation page are answered by their lines
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
                    if (GameDev.Slice.Creation.CharacterCreation.ShouldOffer())
                    {
                        OpenCreation(hud);
                    }
                });
            PauseUnderMenu(hud);
        }

        /// <summary>07 (S5b.1): the creation page, the world stopped under it as under the menu until the life begins.</summary>
        private void OpenCreation(HudView hud)
        {
            if (Creation != null && Creation.Open(hud, Begun))
            {
                Hold();
            }
        }

        private void Begun(GameDev.Slice.Creation.CharacterIdentity identity)
        {
            Identity = identity;
            Unpause();
            Debug.Log($"[Slice] A new character: {identity.name}, {(identity.female ? "a woman" : "a man")}, body {identity.look}, attributes "
                      + string.Join(" ", Enum.GetValues(typeof(CharacterAttribute)).Cast<CharacterAttribute>().Select(a => Character.Get(a))) + ".");
        }

        private void PauseUnderMenu(HudView hud)
        {
            if (hud.IsMenuOpen)
            {
                Hold();
            }
        }

        /// <summary>The world stopped: its clock paused and the frame's time at a stand.</summary>
        private void Hold()
        {
            if (_menuPaused)
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
                actions.Add(Bag.Catalogue.Find(p.item)?.kind == ItemKind.Potion
                    ? Advancement != null ? () => AskDrink(p) : (Action)null
                    : Outfit != null ? () => TogglePiece(p) : (Action)null);
            }

            hud.ShowBag(BagPage.Money(Bag), BagPage.Load(Bag), BagPage.Lines(Bag, PieceNote), actions,
                _bagNote != null ? Loc.Get(_bagNote) : Outfit != null ? Loc.Get("ui.hud.bag.hint") : null);
        }

        private ItemPiece _drinkAsked;

        /// <summary>
        /// 20 §3 *Drinking a Limit potion*: the first click on a potion asks, warning of the hour defenceless; a second click
        /// on it drinks it, the bag closes, and its Assimilation begins (S5b.5).
        /// </summary>
        private void AskDrink(ItemPiece piece)
        {
            var potion = Bag.Catalogue.Find(piece.item);
            var fault = Advancement.Fault(piece, potion);
            if (fault != null)
            {
                _drinkAsked = null;
                _bagNote = fault;
                Refresh();
                return;
            }

            if (_drinkAsked != piece)
            {
                _drinkAsked = piece;
                _bagNote = "ui.drink.ask";
                Refresh();
                return;
            }

            _drinkAsked = null;
            _bagNote = null;
            if (Advancement.Drink(piece, potion))
            {
                Hud()?.CloseBag();
                Hud()?.ShowMessage(Loc.Get("ui.drink.drunk"));
            }
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

        /// <summary>19 §6.3's merchants among the named people: the keepers of a shop, a stall or the tavern (authoring default).</summary>
        private static readonly System.Collections.Generic.HashSet<string> Merchants =
            new System.Collections.Generic.HashSet<string> { "nell", "harrow", "edda", "silas", "ada", "tobias", "kettle", SliceHours.MarketTrader };

        /// <summary>A purse to lift on each walking townsperson in the loaded scenes (S5a.8), and on the named people and the Wardens (S5c.5).</summary>
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

            // S5c.5: the named people, the Wardens and the merchants too, each purse by its kind (19 §6.3).
            foreach (var body in PeopleAround.InSight())
            {
                var named = body.GetComponent<NamedPerson>();
                var post = body.GetComponent<PersonPost>();
                var id = named != null ? named.Id : post != null ? post.Person : null;
                var warden = PeopleAround.IsWarden(body);
                if ((id == null && !warden) || body.GetComponent<TownsfolkWalker>() != null || body.GetComponent<GameDev.Slice.Theft.PurseTarget>() != null)
                {
                    continue;
                }

                var kind = warden ? GameDev.Slice.Theft.PurseKind.Guard
                    : Merchants.Contains(id) ? GameDev.Slice.Theft.PurseKind.Merchant : GameDev.Slice.Theft.PurseKind.Townsman;
                body.gameObject.AddComponent<GameDev.Slice.Theft.PurseTarget>().Configure(Theft, Work, () => Fights != null && Fights.InFight, () => Hiding != null && Hiding.Active, kind);
                added = true;
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
                // A keeper trades only at its work post: Silas Quill at a table of the Soot & Bell has no counter (S5c.3).
                if (person.TryGetComponent<PersonPost>(out var post) && post.Post != SliceHours.Work)
                {
                    continue;
                }

                if (Shops.Find(person.Id) != null && person.GetComponent<ShopCounter>() == null)
                {
                    person.gameObject.AddComponent<ShopCounter>().Configure(person.Id);
                    added = true;
                }

                added |= Workshop != null && Shops.Find(person.Id)?.workshop == true && OpenAnvil(person);
            }

            added |= Alchemy != null && OpenBenches();
            added |= OpenBed();
            OpenTrialDoor();

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

        /// <summary>The apothecary's scene, whose lab bench is Harrow's (S5b.4).</summary>
        public const string HarrowScene = "HarrowApothecary";

        /// <summary>The faction house's scene, whose archive table is the members' bench (S5b.4; SLICE_TRIAL §4).</summary>
        public const string FactionHouseScene = "FactionHouse";

        /// <summary>
        /// 20 §3 *Where brewing is done* (S5b.4): Harrow's lab bench in the apothecary's laboratory, and the reading table of
        /// the faction house's archive, the house having one storey, become alchemy benches; the den's comes with its
        /// dwelling (<see cref="GameDev.Slice.Alchemy.AlchemyBench.AtDwelling"/>). True when one was opened now.
        /// </summary>
        private static bool OpenBenches()
        {
            var added = false;
            foreach (var block in FindObjectsByType<KitBlock>(FindObjectsInactive.Exclude))
            {
                var scene = block.gameObject.scene.name;
                GameDev.Slice.Alchemy.BenchKind? kind = scene == HarrowScene && block.name.StartsWith("Kit_BkFurniture_LabBench")
                    ? GameDev.Slice.Alchemy.BenchKind.Harrow
                    : scene == FactionHouseScene && block.name.StartsWith("Kit_ViFurniture_Table_")
                        ? GameDev.Slice.Alchemy.BenchKind.Faction
                        : null;
                if (kind == null || block.GetComponent<GameDev.Slice.Alchemy.AlchemyBench>() != null)
                {
                    continue;
                }

                block.gameObject.AddComponent<GameDev.Slice.Alchemy.AlchemyBench>().Configure(kind.Value);
                added = true;
            }

            return added;
        }

        /// <summary>The Soot &amp; Bell's scene, whose let room's bed is rented for a night (S5c.2).</summary>
        public const string TavernScene = "SootAndBell";

        /// <summary>Thomas Wren's door from Draught Lane locks until it is opened (S5c.4; SLICE_TRIAL §1.2).</summary>
        private void OpenTrialDoor()
        {
            if (Trial == null)
            {
                return;
            }

            foreach (var portal in FindObjectsByType<GameDev.World.Portals.Portal>(FindObjectsInactive.Include))
            {
                if (portal.Id == GameDev.Slice.Trial.SliceTrial.DoorId && portal.GetComponent<GameDev.Slice.Trial.TrialDoor>() == null)
                {
                    portal.gameObject.AddComponent<GameDev.Slice.Trial.TrialDoor>();
                }
            }
        }

        /// <summary>18 §3, 11 *Sleep* (S5c.2): the bed of the Soot &amp; Bell's let room is rented for a night; true when it was opened now.</summary>
        private static bool OpenBed()
        {
            var added = false;
            foreach (var block in FindObjectsByType<KitBlock>(FindObjectsInactive.Exclude))
            {
                if (block.gameObject.scene.name == TavernScene && block.name.StartsWith("Kit_ViFurniture_Bed") && block.GetComponent<GameDev.Slice.Mind.SleepBed>() == null)
                {
                    block.gameObject.AddComponent<GameDev.Slice.Mind.SleepBed>();
                    added = true;
                }
            }

            return added;
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
            hud.ShowWillPower(Fights != null && !Fights.InFight ? Fights.WillPowerShare : 1f); // S5b.8: out of a fight
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
                if (Clock != null)
                {
                    Clock.TickBoundary -= SettlePotion;
                }

                Standing?.Dispose();
                Director?.Dispose();
                Trial?.Detach();
                _running = null;
            }
        }
    }
}

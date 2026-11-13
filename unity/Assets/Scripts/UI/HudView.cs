using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Core.Scenes;
using GameDev.Locomotion;
using GameDev.World.Portals;
using GameDev.World.Timekeeping;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameDev.UI
{
    /// <summary>
    /// The slice HUD (17 X.6), on UI Toolkit: the day and time from the running clock, a prompt while the player is
    /// within reach of a door or another <see cref="IPromptSource"/>, the plain reading panel of S1.3, and the dialogue
    /// column of S2.4. Presentation only: it reads the clock, the doors, and the sources and changes nothing. Its own words are string table keys
    /// (17 X.7): a UXML label of the class <c>hud__loc</c> carries its key as its text, and every word follows a change of
    /// language.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudView : MonoBehaviour
    {
        private const string PromptVisible = "hud__prompt--visible";
        private const string MessageVisible = "hud__message--visible";

        /// <summary>
        /// The owner's, s (2026-11-12, "2–3 seconds"; this value an authoring default): how long a Screen Message stays on the screen.
        /// </summary>
        public const float MessageSeconds = 2.5f;
        private const string ReadingVisible = "hud__reading--visible";
        private const string NotesVisible = "hud__notes--visible";
        private const string NoteClass = "hud__note";
        private const string LocalizedClass = "hud__loc";
        private readonly List<(Label label, string key)> _localized = new List<(Label, string)>();

        private Label _time;
        private Label _prompt;
        private Label _message;
        private float _messageUntil;
        private VisualElement _reading;
        private Label _readingTitle;
        private Label _readingText;
        private VisualElement _notes;
        private ScrollView _notesList;
        private VisualElement _map;
        private VisualElement _bag;
        private Label _bagMoney;
        private Label _bagLoad;
        private ScrollView _bagList;
        private Label _bagNote;
        private VisualElement _shop;
        private Label _shopTitle;
        private Label _shopMoney;
        private ScrollView _shopList;
        private Label _shopNote;
        private TextField _shopField;
        private System.Action<string> _shopFieldChanged;
        private readonly List<System.Action> _shopActions = new List<System.Action>();
        private readonly List<System.Action> _bagActions = new List<System.Action>();
        private CityMapElement _mapView;
        private const string MapVisible = "hud__map--visible";
        private long _shownTick = -1;
        private PlayerLocomotion _player;
        private Portal[] _portals;
        private List<IPromptSource> _sources;
        private List<INameTag> _tags;
        private Label _nameTag;

        private const string NameTagVisible = "hud__nametag--visible";

        private const string DialogueVisible = "hud__dialogue--visible";
        private const string DialogueSpeakerClass = "hud__dialogue-speaker";
        private const string DialogueLineClass = "hud__dialogue-line";
        private const string DialogueOptionClass = "hud__dialogue-option";
        private VisualElement _dialogue;
        private ScrollView _dialogueLines;
        private VisualElement _dialogueOptions;

        /// <summary>Authoring default, m: the HUD names a person within this reach.</summary>
        private const float NameReach = 6f;

        public string TimeText => _time?.text;

        /// <summary>The name shown over a person, or null.</summary>
        public string NameTagText => _nameTag != null && _nameTag.ClassListContains(NameTagVisible) ? _nameTag.text : null;

        public bool IsPromptVisible => _prompt != null && _prompt.ClassListContains(PromptVisible);

        public string PromptText => _prompt?.text;

        /// <summary>The Screen Message on the screen now, or null.</summary>
        public string MessageText => _message != null && Time.unscaledTime < _messageUntil ? _message.text : null;

        /// <summary>
        /// A Screen Message: an outcome or a hint said on the screen for <see cref="MessageSeconds"/>, short and plain, such as "Failed: the target moved
        /// off" (owner, 2026-11-12): it never stands in a prompt, so it never keeps E from the next try. A newer one
        /// replaces it. Whoever says something an attribute gates, Intelligence or Perception, checks it first (07.1 §11).
        /// </summary>
        public void ShowMessage(string text)
        {
            if (string.IsNullOrEmpty(text) || !Bind() || _message == null)
            {
                return;
            }

            _message.text = text;
            _messageUntil = Time.unscaledTime + MessageSeconds;
            _message.AddToClassList(MessageVisible);
        }

        public bool IsReadingOpen => _reading != null && _reading.ClassListContains(ReadingVisible);

        public string ReadingTitle => _readingTitle?.text;

        public string ReadingText => _readingText?.text;

        /// <summary>Shows what a source says in the plain panel.</summary>
        public void ShowReading(string title, string text)
        {
            if (!Bind())
            {
                return;
            }

            _readingTitle.text = title;
            _readingText.text = text;
            _reading.AddToClassList(ReadingVisible);
        }

        public void CloseReading()
        {
            _reading?.RemoveFromClassList(ReadingVisible);
        }

        public bool IsNotesOpen => _notes != null && _notes.ClassListContains(NotesVisible);

        /// <summary>The lines the Notes page shows, top first.</summary>
        public IReadOnlyList<string> NoteLines => _notesList == null
            ? new string[0]
            : _notesList.Query<Label>(className: NoteClass).ToList().Select(l => l.text).ToList();

        /// <summary>Opens the Character Notes page with these lines, or refreshes it while open.</summary>
        public void ShowNotes(IReadOnlyList<string> lines)
        {
            if (!Bind())
            {
                return;
            }

            _notesList.Clear();
            foreach (var line in lines.Count > 0 ? lines : new[] { Loc.Get("ui.hud.notes.empty") })
            {
                var label = new Label(line);
                label.AddToClassList(NoteClass);
                _notesList.Add(label);
            }

            CloseMap();
            CloseBag();
            _notes.AddToClassList(NotesVisible);
        }

        public void CloseNotes()
        {
            _notes?.RemoveFromClassList(NotesVisible);
        }

        public bool IsBagOpen => _bag != null && _bag.ClassListContains(NotesVisible);

        // A Reaction Check's bar (S5a.8), built in code on the first check of each tree.
        private VisualElement _reaction;
        private VisualElement _reactionGood;
        private VisualElement _reactionPerfect;
        private VisualElement _reactionMarker;
        private Label _reactionCaption;

        /// <summary>Whether a Reaction Check's bar is on the screen.</summary>
        public bool IsReactionShown => _reaction != null && _reaction.panel != null && _reaction.style.display.value == DisplayStyle.Flex;

        /// <summary>
        /// A Reaction Check's bar (S5a.8; 20 §3, 10.3 §6): its good zone, a perfect zone inside it when it has one, and
        /// the marker, each as a share of the bar from 0 to 1, under a caption.
        /// </summary>
        public void ShowReaction(string caption, float goodStart, float goodWidth, float perfectStart, float perfectWidth, float marker)
        {
            if (!Bind())
            {
                return;
            }

            if (_reaction == null || _reaction.panel == null)
            {
                BuildReaction(GetComponent<UIDocument>().rootVisualElement);
            }

            _reactionCaption.text = caption;
            _reactionGood.style.left = Length.Percent(goodStart * 100f);
            _reactionGood.style.width = Length.Percent(goodWidth * 100f);
            _reactionPerfect.style.display = perfectWidth > 0f ? DisplayStyle.Flex : DisplayStyle.None;
            _reactionPerfect.style.left = Length.Percent(perfectStart * 100f);
            _reactionPerfect.style.width = Length.Percent(perfectWidth * 100f);
            _reactionMarker.style.left = Length.Percent(marker * 100f);
            _reaction.style.display = DisplayStyle.Flex;
        }

        public void HideReaction()
        {
            if (_reaction != null)
            {
                _reaction.style.display = DisplayStyle.None;
            }
        }

        // Authoring defaults: a dark bar edged in brass at the bottom centre, a muted green good zone, a gold perfect
        // zone and an ivory marker, in the slice's Victorian palette.
        private void BuildReaction(VisualElement root)
        {
            _reaction = new VisualElement { name = "reaction", pickingMode = PickingMode.Ignore };
            _reaction.style.position = Position.Absolute;
            _reaction.style.left = Length.Percent(30f);
            _reaction.style.width = Length.Percent(40f);
            // Under the prompt at 22 %, which shows the work's own line beside the bar (the owner's play test of
            // 2026-11-12: the two stood one over the other).
            _reaction.style.bottom = Length.Percent(11f);
            _reactionCaption = new Label { pickingMode = PickingMode.Ignore };
            _reactionCaption.style.unityTextAlign = TextAnchor.MiddleCenter;
            _reactionCaption.style.color = new Color(0.93f, 0.89f, 0.80f);
            _reactionCaption.style.fontSize = 18f;
            _reactionCaption.style.marginBottom = 6f;
            var bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.style.height = 20f;
            bar.style.backgroundColor = new Color(0.07f, 0.06f, 0.05f, 0.88f);
            var brass = new Color(0.62f, 0.50f, 0.30f);
            bar.style.borderTopColor = brass;
            bar.style.borderBottomColor = brass;
            bar.style.borderLeftColor = brass;
            bar.style.borderRightColor = brass;
            bar.style.borderTopWidth = 1f;
            bar.style.borderBottomWidth = 1f;
            bar.style.borderLeftWidth = 1f;
            bar.style.borderRightWidth = 1f;
            _reactionGood = Zone(new Color(0.42f, 0.52f, 0.30f, 0.95f));
            _reactionPerfect = Zone(new Color(0.86f, 0.72f, 0.36f, 1f));
            _reactionMarker = Zone(new Color(0.96f, 0.94f, 0.88f, 1f));
            _reactionMarker.style.width = 3f;
            _reactionMarker.style.marginLeft = -1.5f;
            _reactionMarker.style.top = -4f;
            _reactionMarker.style.bottom = -4f;
            bar.Add(_reactionGood);
            bar.Add(_reactionPerfect);
            bar.Add(_reactionMarker);
            _reaction.Add(_reactionCaption);
            _reaction.Add(bar);
            _reaction.style.display = DisplayStyle.None;
            root.Add(_reaction);
        }

        private static VisualElement Zone(Color colour)
        {
            var zone = new VisualElement { pickingMode = PickingMode.Ignore };
            zone.style.position = Position.Absolute;
            zone.style.top = 0f;
            zone.style.bottom = 0f;
            zone.style.backgroundColor = colour;
            return zone;
        }

        // Stamina's bar (07.2 §3.2; the owner's play test of S5a), built in code the first time it shows.
        private VisualElement _stamina;
        private VisualElement _staminaFill;

        /// <summary>Whether Stamina's bar is on the screen.</summary>
        public bool IsStaminaShown => _stamina != null && _stamina.panel != null && _stamina.style.display.value == DisplayStyle.Flex;

        /// <summary>The share of Stamina's bar filled, from 0 to 1.</summary>
        public float StaminaShown => _staminaFill == null ? 1f : _staminaFill.style.width.value.value / 100f;

        // Authoring defaults: an amber fill, dull red while tired.
        private static readonly Color StaminaColour = new Color(0.80f, 0.64f, 0.30f, 0.95f);
        private static readonly Color TiredColour = new Color(0.62f, 0.28f, 0.22f, 0.95f);

        /// <summary>
        /// Stamina's bar at <paramref name="share"/> of its width, shown while any Stamina is spent and hidden when it is
        /// full; dull red while <paramref name="tired"/>.
        /// </summary>
        public void ShowStamina(float share, bool tired)
        {
            if (share >= 0.999f)
            {
                if (_stamina != null)
                {
                    _stamina.style.display = DisplayStyle.None;
                }

                return;
            }

            if (!Bind())
            {
                return;
            }

            if (_stamina == null || _stamina.panel == null)
            {
                BuildStamina(GetComponent<UIDocument>().rootVisualElement);
            }

            _staminaFill.style.width = Length.Percent(Mathf.Clamp01(share) * 100f);
            _staminaFill.style.backgroundColor = tired ? TiredColour : StaminaColour;
            _stamina.style.display = DisplayStyle.Flex;
        }

        // Authoring defaults: a thin dark bar edged in brass at the bottom left, under the life line.
        private void BuildStamina(VisualElement root) => _stamina = ThinBar(root, "stamina", 10f, StaminaColour, out _staminaFill);

        private static VisualElement ThinBar(VisualElement root, string name, float bottom, Color colour, out VisualElement fill)
        {
            var bar = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            bar.style.position = Position.Absolute;
            bar.style.left = 24f;
            bar.style.bottom = bottom;
            bar.style.width = 220f;
            bar.style.height = 8f;
            bar.style.backgroundColor = new Color(0.07f, 0.06f, 0.05f, 0.82f);
            var brass = new Color(0.62f, 0.50f, 0.30f);
            bar.style.borderTopColor = brass;
            bar.style.borderBottomColor = brass;
            bar.style.borderLeftColor = brass;
            bar.style.borderRightColor = brass;
            bar.style.borderTopWidth = 1f;
            bar.style.borderBottomWidth = 1f;
            bar.style.borderLeftWidth = 1f;
            bar.style.borderRightWidth = 1f;
            fill = Zone(colour);
            fill.style.left = 0f;
            bar.Add(fill);
            bar.style.display = DisplayStyle.None;
            root.Add(bar);
            return bar;
        }

        // Will Power's bar out of a fight (07.2 §3.3; 05.1 *Вне боя*, S5b.8): a lilac fill above Stamina's (authoring default).
        private static readonly Color WillPowerColour = new Color(0.55f, 0.46f, 0.72f, 0.95f);
        private VisualElement _willPower;
        private VisualElement _willPowerFill;

        /// <summary>Whether Will Power's bar is on the screen.</summary>
        public bool IsWillPowerShown => _willPower != null && _willPower.panel != null && _willPower.style.display.value == DisplayStyle.Flex;

        /// <summary>Will Power's bar at <paramref name="share"/> of its width, shown while any is spent and hidden when it is full.</summary>
        public void ShowWillPower(float share)
        {
            if (share >= 0.999f)
            {
                if (_willPower != null)
                {
                    _willPower.style.display = DisplayStyle.None;
                }

                return;
            }

            if (!Bind())
            {
                return;
            }

            if (_willPower == null || _willPower.panel == null)
            {
                _willPower = ThinBar(GetComponent<UIDocument>().rootVisualElement, "willpower", 22f, WillPowerColour, out _willPowerFill);
            }

            _willPowerFill.style.width = Length.Percent(Mathf.Clamp01(share) * 100f);
            _willPower.style.display = DisplayStyle.Flex;
        }

        /// <summary>The bag page's lines, money and load first, then each stack and piece (S5a.1).</summary>
        public IReadOnlyList<string> BagLines => _bag == null
            ? new string[0]
            : new[] { _bagMoney.text, _bagLoad.text }.Concat(Texts(_bagList, NoteClass)).ToList();

        /// <summary>The note under the bag's list: the last refusal, or the hint (S5a.2).</summary>
        public string BagNote => _bagNote != null ? _bagNote.text : null;

        /// <summary>Opens the bag page on I (S5a.1), or refreshes it while open: the money, the load, and what is carried.</summary>
        public void ShowBag(string money, string load, IReadOnlyList<string> lines) => ShowBag(money, load, lines, null, null);

        /// <summary>
        /// The bag page with a click on each line that has an action, the n-th action for the n-th line, and a note under
        /// the list (S5a.2).
        /// </summary>
        public void ShowBag(string money, string load, IReadOnlyList<string> lines, IReadOnlyList<System.Action> actions, string note)
        {
            if (!Bind() || _bag == null)
            {
                return;
            }

            _bagMoney.text = money;
            _bagLoad.text = load;
            _bagList.Clear();
            _bagActions.Clear();
            var shown = lines.Count > 0 ? lines : new[] { Loc.Get("ui.hud.bag.empty") };
            for (var i = 0; i < shown.Count; i++)
            {
                var label = new Label(shown[i]);
                label.AddToClassList(NoteClass);
                var action = actions != null && i < actions.Count && lines.Count > 0 ? actions[i] : null;
                _bagActions.Add(action);
                if (action != null)
                {
                    label.AddToClassList("hud__bag-action");
                    label.RegisterCallback<ClickEvent>(_ => action());
                }

                _bagList.Add(label);
            }

            if (_bagNote != null)
            {
                _bagNote.text = note ?? string.Empty;
            }

            CloseMap();
            CloseNotes();
            CloseShop();
            _bag.AddToClassList(NotesVisible);
        }

        public void CloseBag()
        {
            _bag?.RemoveFromClassList(NotesVisible);
        }

        // The menu on Esc (owner, 2026-11-12): its lines in order, each a click; the same panel offers a saved run at the
        // start (17 X.3), with its own title and lines.
        private static readonly string[] MenuNames = { "menu-resume", "menu-language", "menu-save", "menu-quit" };
        private static readonly string[] StartNames = { "start-continue", "start-new" };
        private static readonly string[] MenuParts = { "menu-title", "menu-resume", "menu-language", "menu-save", "menu-quit", "menu-close" };
        private static readonly string[] StartParts = { "start-title", "start-continue", "start-new" };
        private readonly System.Action[] _menuActions = new System.Action[4];
        private string[] _menuLines = MenuNames;
        private VisualElement _menu;

        public bool IsMenuOpen => _menu != null && _menu.ClassListContains(NotesVisible);

        /// <summary>Whether the panel open is the start's offer of a saved run.</summary>
        public bool IsStartOpen => IsMenuOpen && _menuLines == StartNames;

        /// <summary>The menu's lines as shown, in order.</summary>
        public IReadOnlyList<string> MenuLines => _menu == null ? new string[0] : _menuLines.Select(n => _menu.Q<Label>(n)?.text).ToArray();

        /// <summary>
        /// The start's offer (17 X.3): continue the saved run, or begin a new one, each line a click; Esc does not close it.
        /// </summary>
        public void ShowStart(System.Action continueRun, System.Action newRun)
        {
            OpenPanel(StartNames, StartParts, MenuParts, continueRun, newRun);
        }

        /// <summary>Whether any window is open: a page, a shop, a conversation, the reading panel or the menu.</summary>
        public bool AnyWindowOpen => IsReadingOpen || IsNotesOpen || IsBagOpen || IsShopOpen || IsMapOpen || IsDialogueOpen || IsMenuOpen;

        /// <summary>
        /// The menu on Esc (owner, 2026-11-12: Esc closes any window open, and with none it opens the menu and closes it
        /// again): resume, change the language, quit the game, each line a click. The pages close under it.
        /// </summary>
        public void ShowMenu(System.Action resume, System.Action language, System.Action save, System.Action quit)
        {
            OpenPanel(MenuNames, MenuParts, StartParts, resume, language, save, quit);
        }

        private void OpenPanel(string[] lines, string[] shown, string[] hidden, params System.Action[] actions)
        {
            if (!Bind() || _menu == null)
            {
                return;
            }

            _menuLines = lines;
            for (var i = 0; i < _menuActions.Length; i++)
            {
                _menuActions[i] = i < actions.Length ? actions[i] : null;
            }

            foreach (var name in hidden)
            {
                var part = _menu.Q(name);
                if (part != null)
                {
                    part.style.display = DisplayStyle.None;
                }
            }

            foreach (var name in shown)
            {
                var part = _menu.Q(name);
                if (part != null)
                {
                    part.style.display = DisplayStyle.Flex;
                }
            }

            foreach (var name in lines)
            {
                var line = _menu.Q<Label>(name);
                if (line != null)
                {
                    line.UnregisterCallback<ClickEvent>(OnMenuClick);
                    line.RegisterCallback<ClickEvent>(OnMenuClick);
                }
            }

            CloseMap();
            CloseNotes();
            CloseBag();
            CloseReading();
            _menu.AddToClassList(NotesVisible);
        }

        public void CloseMenu()
        {
            _menu?.RemoveFromClassList(NotesVisible);
        }

        /// <summary>Clicks the n-th line of the menu, as the mouse does; false if it has no action.</summary>
        public bool ClickMenuLine(int index)
        {
            if (!IsMenuOpen || index < 0 || index >= _menuLines.Length || _menuActions[index] == null)
            {
                return false;
            }

            _menuActions[index]();
            return true;
        }

        private void OnMenuClick(ClickEvent click)
        {
            if (click.currentTarget is VisualElement line)
            {
                ClickMenuLine(System.Array.IndexOf(_menuLines, line.name));
            }
        }

        public bool IsShopOpen => _shop != null && _shop.ClassListContains(NotesVisible);

        public string ShopTitle => _shopTitle != null ? _shopTitle.text : null;

        /// <summary>The shop page's list, headings included, in order (S5a.3).</summary>
        public IReadOnlyList<string> ShopLines => _shopList == null
            ? new string[0]
            : _shopList.Query<Label>().ToList().Select(l => l.text).ToList();

        public string ShopNote => _shopNote != null ? _shopNote.text : null;

        /// <summary>Opens a shop's page, or refreshes it: the keeper, the purse, the lines to click, and a note (S5a.3).</summary>
        public void ShowShop(string title, string money, IReadOnlyList<PageLine> lines, string note)
        {
            if (!Bind() || _shop == null)
            {
                return;
            }

            _shopTitle.text = title;
            _shopMoney.text = money;
            _shopList.Clear();
            _shopActions.Clear();
            foreach (var line in lines)
            {
                var label = new Label(line.Text);
                label.AddToClassList(line.Heading ? "hud__page-heading" : NoteClass);
                _shopActions.Add(line.Action);
                if (line.Action != null)
                {
                    var action = line.Action;
                    label.AddToClassList("hud__bag-action");
                    label.RegisterCallback<ClickEvent>(_ => action());
                }

                _shopList.Add(label);
            }

            _shopNote.text = note ?? string.Empty;
            CloseMap();
            CloseNotes();
            CloseBag();
            _shop.AddToClassList(NotesVisible);
        }

        public void CloseShop()
        {
            HideShopField();
            _shop?.RemoveFromClassList(NotesVisible);
        }

        /// <summary>The text field over the shop page's list, or null when hidden.</summary>
        public string ShopFieldText => _shopField != null && _shopField.style.display != DisplayStyle.None ? _shopField.value : null;

        /// <summary>
        /// A text field over the shop page's list (S5b.1: the name at creation), with its value; each change is told to
        /// <paramref name="changed"/>. Closing the page hides it.
        /// </summary>
        public void ShowShopField(string value, System.Action<string> changed)
        {
            if (!Bind() || _shopField == null)
            {
                return;
            }

            _shopFieldChanged = null;
            _shopField.SetValueWithoutNotify(value ?? string.Empty);
            _shopField.style.display = DisplayStyle.Flex;
            _shopFieldChanged = changed;
        }

        /// <summary>Types into the shop page's field as the keyboard does; false when it is hidden.</summary>
        public bool TypeShopField(string value)
        {
            if (ShopFieldText == null)
            {
                return false;
            }

            _shopField.value = value ?? string.Empty;
            return true;
        }

        public void HideShopField()
        {
            _shopFieldChanged = null;
            if (_shopField != null)
            {
                _shopField.style.display = DisplayStyle.None;
            }

            ShowShopClose(true);
        }

        /// <summary>Shows or hides the page's "Esc closes" line: a page Esc does not close (creation) hides it while it is open.</summary>
        public void ShowShopClose(bool shown)
        {
            var close = _shop?.Q<Label>("shop-close");
            if (close != null)
            {
                close.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void OnShopField(ChangeEvent<string> change) => _shopFieldChanged?.Invoke(change.newValue);

        /// <summary>Clicks the n-th line of the shop's list, as the mouse does; false if it has no action.</summary>
        public bool ClickShopLine(int index)
        {
            if (!IsShopOpen || index < 0 || index >= _shopActions.Count || _shopActions[index] == null)
            {
                return false;
            }

            _shopActions[index]();
            return true;
        }

        /// <summary>Clicks the n-th line of the bag's list, as the mouse does; false if it has no action.</summary>
        public bool ClickBagLine(int index)
        {
            if (!IsBagOpen || index < 0 || index >= _bagActions.Count || _bagActions[index] == null)
            {
                return false;
            }

            _bagActions[index]();
            return true;
        }

        public bool IsMapOpen => _map != null && _map.ClassListContains(MapVisible);

        /// <summary>The marks the map shows now.</summary>
        public IReadOnlyList<MapMark> MapMarks => _mapView != null ? _mapView.Marks : new MapMark[0];

        /// <summary>Opens the map (S1.6) on the city plan with these marks and the player, or refreshes it while open.</summary>
        public void ShowMap(string planSvg, IReadOnlyList<MapMark> marks, Vector3? player)
        {
            if (!Bind() || _map == null)
            {
                return;
            }

            var area = _map.Q<VisualElement>("map-area");
            if (_mapView == null || _mapView.parent != area)
            {
                _mapView = new CityMapElement { pickingMode = PickingMode.Ignore };
                _mapView.style.flexGrow = 1;
                area.Add(_mapView);
            }

            if (!_mapView.HasPlan && !string.IsNullOrEmpty(planSvg))
            {
                _mapView.SetPlan(planSvg);
            }

            _mapView.SetMarks(marks, player);
            CloseNotes();
            CloseBag();
            _map.AddToClassList(MapVisible);
        }

        public void CloseMap()
        {
            _map?.RemoveFromClassList(MapVisible);
        }

        public bool IsDialogueOpen => _dialogue != null && _dialogue.ClassListContains(DialogueVisible);

        /// <summary>What the dialogue column shows as said, top first, without the speakers' names.</summary>
        public IReadOnlyList<string> DialogueLines => Texts(_dialogueLines, DialogueLineClass);

        /// <summary>The numbered options the dialogue column offers, as shown.</summary>
        public IReadOnlyList<string> DialogueOptions => Texts(_dialogueOptions, DialogueOptionClass);

        /// <summary>
        /// Opens the dialogue column of S2.4 at the right edge, or refreshes it: what was said above, each with its
        /// speaker or none, the latest at the bottom, and the numbered options below.
        /// </summary>
        public void ShowDialogue(IReadOnlyList<(string speaker, string text)> lines, IReadOnlyList<string> options)
        {
            if (!Bind() || _dialogue == null || _dialogueLines == null || _dialogueOptions == null)
            {
                return;
            }

            _dialogueLines.Clear();
            Label last = null;
            foreach (var (speaker, text) in lines)
            {
                if (!string.IsNullOrEmpty(speaker))
                {
                    _dialogueLines.Add(Text(speaker, DialogueSpeakerClass));
                }

                last = Text(text, DialogueLineClass);
                _dialogueLines.Add(last);
            }

            _dialogueOptions.Clear();
            foreach (var option in options)
            {
                _dialogueOptions.Add(Text(option, DialogueOptionClass));
            }

            CloseReading();
            CloseNotes();
            CloseMap();
            CloseBag();
            _dialogue.AddToClassList(DialogueVisible);
            if (last != null)
            {
                // Once laid out, the latest line is in view; a line already replaced by the next answer is not scrolled to.
                _dialogueLines.schedule.Execute(() =>
                {
                    if (_dialogueLines.contentContainer.Contains(last))
                    {
                        _dialogueLines.ScrollTo(last);
                    }
                });
            }
        }

        public void CloseDialogue()
        {
            _dialogue?.RemoveFromClassList(DialogueVisible);
        }

        private static Label Text(string text, string className)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(className);
            return label;
        }

        private static IReadOnlyList<string> Texts(VisualElement within, string className) => within == null
            ? new string[0]
            : within.Query<Label>(className: className).ToList().Select(l => l.text).ToList();

        private void Start()
        {
            if (!Bind())
            {
                Debug.LogError("The HUD's UXML needs the labels 'time', 'prompt', 'reading-title' and 'reading-text', the elements 'reading' and 'notes', and the scroll view 'notes-list'.", this);
                enabled = false;
                return;
            }

            FindPromptSources();
        }

        /// <summary>Looks again for the doors, the prompt sources and the name tags, as after a source was added at run time (S5a.3).</summary>
        public void FindPromptSources()
        {
            _player = FindAnyObjectByType<PlayerLocomotion>();
            _portals = FindObjectsByType<Portal>(FindObjectsInactive.Exclude);
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);
            _sources = behaviours.OfType<IPromptSource>().ToList();
            _tags = behaviours.OfType<INameTag>().ToList();
        }

        // A UIDocument builds its tree again when it is enabled again, as a city's HUD is after an interior (17 I.2):
        // labels no longer on a panel are looked up afresh, and the new tree gets its words.
        private bool Bind()
        {
            if (_time != null && _time.panel != null)
            {
                return true;
            }

            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null)
            {
                return false;
            }

            _nameTag = root.Q<Label>("nametag");
            _time = root.Q<Label>("time");
            _prompt = root.Q<Label>("prompt");
            _message = root.Q<Label>("message");
            _reading = root.Q<VisualElement>("reading");
            _readingTitle = root.Q<Label>("reading-title");
            _readingText = root.Q<Label>("reading-text");
            _notes = root.Q<VisualElement>("notes");
            _notesList = root.Q<ScrollView>("notes-list");
            _bag = root.Q<VisualElement>("bag");
            _bagMoney = root.Q<Label>("bag-money");
            _bagLoad = root.Q<Label>("bag-load");
            _bagList = root.Q<ScrollView>("bag-list");
            _bagNote = root.Q<Label>("bag-note");
            _shop = root.Q<VisualElement>("shop");
            _shopTitle = root.Q<Label>("shop-title");
            _shopMoney = root.Q<Label>("shop-money");
            _shopList = root.Q<ScrollView>("shop-list");
            _shopNote = root.Q<Label>("shop-note");
            _shopField = root.Q<TextField>("shop-field");
            if (_shopField != null)
            {
                _shopField.maxLength = 24;
                _shopField.UnregisterValueChangedCallback(OnShopField);
                _shopField.RegisterValueChangedCallback(OnShopField);
            }
            _map = root.Q<VisualElement>("map");
            _dialogue = root.Q<VisualElement>("dialogue");
            _dialogueLines = root.Q<ScrollView>("dialogue-lines");
            _dialogueOptions = root.Q<VisualElement>("dialogue-options");
            _menu = root.Q<VisualElement>("menu");
            if (_time == null || _prompt == null || _reading == null || _readingTitle == null || _readingText == null || _notes == null || _notesList == null)
            {
                _time = null;
                return false;
            }

            // A key is read once per tree: a label already localized keeps the key it was found with.
            var known = _localized.ToDictionary(l => l.label, l => l.key);
            _localized.Clear();
            root.Query<Label>(className: LocalizedClass).ForEach(label => _localized.Add((label, known.TryGetValue(label, out var key) ? key : label.text)));
            Localize();
            return true;
        }

        /// <summary>The HUD's own words in the language shown; the clock's are written again at once.</summary>
        private void Localize()
        {
            foreach (var (label, key) in _localized)
            {
                label.text = Loc.Get(key);
            }

            _shownTick = -1;
        }

        private void OnEnable()
        {
            Loc.Changed += Localize;
        }

        private void OnDisable()
        {
            Loc.Changed -= Localize;
        }

        private void Update()
        {
            if (!Bind())
            {
                return;
            }

            var clock = GameClockDriver.Running;
            if (clock != null && clock.Tick != _shownTick)
            {
                _shownTick = clock.Tick;
                _time.text = HudText.DayAndTime(clock.Day, clock.MinuteOfDay);
            }

            // A conversation has its own keys; nothing else is offered while it is open.
            var prompt = IsDialogueOpen ? null : Prompt();
            if (prompt != null)
            {
                _prompt.text = prompt;
            }

            _prompt.EnableInClassList(PromptVisible, prompt != null);
            _message?.EnableInClassList(MessageVisible, Time.unscaledTime < _messageUntil);
            ShowNameTag();
        }

        // The nearest named person within reach, in front of the camera and in its sight, named over their head: a wall,
        // a floor, a door or a piece of furniture between hides the name (owner, after VI-8).
        private void ShowNameTag()
        {
            if (_nameTag == null)
            {
                return;
            }

            var camera = Camera.main;
            INameTag nearest = null;
            var best = NameReach * NameReach;
            if (_player != null && camera != null && !SceneTransition.IsTraveling)
            {
                var position = _player.transform.position;
                foreach (var person in _tags)
                {
                    // A person switched off since the HUD looked, as one gone home for the night (S5c.3), is no one.
                    if (person as Object == null || (person is Behaviour b && !b.isActiveAndEnabled))
                    {
                        continue;
                    }

                    var distance = (person.TagPosition - position).sqrMagnitude;
                    if (distance <= best && camera.WorldToViewportPoint(person.TagPosition).z > 0f && InSight(camera, person))
                    {
                        best = distance;
                        nearest = person;
                    }
                }
            }

            if (nearest == null || _nameTag.panel == null)
            {
                _nameTag.RemoveFromClassList(NameTagVisible);
                return;
            }

            var at = RuntimePanelUtils.CameraTransformWorldToPanel(_nameTag.panel, nearest.TagPosition, camera);
            _nameTag.text = nearest.DisplayName;
            _nameTag.style.left = at.x;
            _nameTag.style.top = at.y;
            _nameTag.AddToClassList(NameTagVisible);
        }

        /// <summary>
        /// True when nothing solid stands between the camera and the name: the ground, walls and floors, furniture and
        /// doors block it; people, plants and loose props on their own layers do not.
        /// </summary>
        private static bool InSight(Camera camera, INameTag person)
        {
            if (_sightMask == 0)
            {
                _sightMask = LayerMask.GetMask("Ground", "Structure", "Prop", "Interactable");
            }

            // The person's own body, last on the line, does not hide their name.
            return _sightMask == 0
                   || !Physics.Linecast(camera.transform.position, person.TagPosition, out var hit, _sightMask, QueryTriggerInteraction.Ignore)
                   || (person is Component c && hit.collider.transform.IsChildOf(c.transform));
        }

        private static int _sightMask;

        private string Prompt()
        {
            if (_player == null || SceneTransition.IsTraveling)
            {
                return null;
            }

            var position = _player.transform.position;
            foreach (var portal in _portals)
            {
                if (portal != null && portal.InReach(position))
                {
                    return Loc.Get(portal.Sealed ? portal.SealedPrompt ?? "ui.prompt.sealed" : "ui.prompt.enter");
                }
            }

            foreach (var source in _sources)
            {
                var prompt = source as Object != null && !(source is Behaviour b && !b.isActiveAndEnabled) ? source.PromptFor(position) : null;
                if (prompt != null)
                {
                    return prompt;
                }
            }

            return null;
        }
    }
}

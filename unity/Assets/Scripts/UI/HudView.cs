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
    /// within reach of a door or another <see cref="IPromptSource"/>, and the plain reading panel of S1.3. Presentation
    /// only: it reads the clock, the doors, and the sources and changes nothing. Its own words are string table keys
    /// (17 X.7): a UXML label of the class <c>hud__loc</c> carries its key as its text, and every word follows a change of
    /// language.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudView : MonoBehaviour
    {
        private const string PromptVisible = "hud__prompt--visible";
        private const string ReadingVisible = "hud__reading--visible";
        private const string NotesVisible = "hud__notes--visible";
        private const string NoteClass = "hud__note";
        private const string LocalizedClass = "hud__loc";
        private readonly List<(Label label, string key)> _localized = new List<(Label, string)>();

        private Label _time;
        private Label _prompt;
        private VisualElement _reading;
        private Label _readingTitle;
        private Label _readingText;
        private VisualElement _notes;
        private ScrollView _notesList;
        private VisualElement _map;
        private CityMapElement _mapView;
        private const string MapVisible = "hud__map--visible";
        private long _shownTick = -1;
        private PlayerLocomotion _player;
        private Portal[] _portals;
        private List<IPromptSource> _sources;
        private List<INameTag> _tags;
        private Label _nameTag;

        private const string NameTagVisible = "hud__nametag--visible";

        /// <summary>Authoring default, m: the HUD names a person within this reach.</summary>
        private const float NameReach = 6f;

        public string TimeText => _time?.text;

        /// <summary>The name shown over a person, or null.</summary>
        public string NameTagText => _nameTag != null && _nameTag.ClassListContains(NameTagVisible) ? _nameTag.text : null;

        public bool IsPromptVisible => _prompt != null && _prompt.ClassListContains(PromptVisible);

        public string PromptText => _prompt?.text;

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
            _notes.AddToClassList(NotesVisible);
        }

        public void CloseNotes()
        {
            _notes?.RemoveFromClassList(NotesVisible);
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
            _map.AddToClassList(MapVisible);
        }

        public void CloseMap()
        {
            _map?.RemoveFromClassList(MapVisible);
        }

        private void Start()
        {
            if (!Bind())
            {
                Debug.LogError("The HUD's UXML needs the labels 'time', 'prompt', 'reading-title' and 'reading-text', the elements 'reading' and 'notes', and the scroll view 'notes-list'.", this);
                enabled = false;
                return;
            }

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
            _reading = root.Q<VisualElement>("reading");
            _readingTitle = root.Q<Label>("reading-title");
            _readingText = root.Q<Label>("reading-text");
            _notes = root.Q<VisualElement>("notes");
            _notesList = root.Q<ScrollView>("notes-list");
            _map = root.Q<VisualElement>("map");
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

            var prompt = Prompt();
            if (prompt != null)
            {
                _prompt.text = prompt;
            }

            _prompt.EnableInClassList(PromptVisible, prompt != null);
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
                    if (person as Object == null)
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
                    return Loc.Get("ui.prompt.enter");
                }
            }

            foreach (var source in _sources)
            {
                var prompt = source as Object != null ? source.PromptFor(position) : null;
                if (prompt != null)
                {
                    return prompt;
                }
            }

            return null;
        }
    }
}

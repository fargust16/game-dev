using System.Collections.Generic;
using System.Linq;
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
    /// only: it reads the clock, the doors, and the sources and changes nothing.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudView : MonoBehaviour
    {
        private const string PromptVisible = "hud__prompt--visible";
        private const string ReadingVisible = "hud__reading--visible";
        private const string DoorPrompt = "E  Enter";
        private const string NotesVisible = "hud__notes--visible";
        private const string NoteClass = "hud__note";
        private const string NothingNoted = "Nothing noted yet.";

        private Label _time;
        private Label _prompt;
        private VisualElement _reading;
        private Label _readingTitle;
        private Label _readingText;
        private VisualElement _notes;
        private ScrollView _notesList;
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
            foreach (var line in lines.Count > 0 ? lines : new[] { NothingNoted })
            {
                var label = new Label(line);
                label.AddToClassList(NoteClass);
                _notesList.Add(label);
            }

            _notes.AddToClassList(NotesVisible);
        }

        public void CloseNotes()
        {
            _notes?.RemoveFromClassList(NotesVisible);
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
            _nameTag = GetComponent<UIDocument>().rootVisualElement.Q<Label>("nametag");
        }

        private bool Bind()
        {
            if (_time != null)
            {
                return true;
            }

            var root = GetComponent<UIDocument>().rootVisualElement;
            _time = root.Q<Label>("time");
            _prompt = root.Q<Label>("prompt");
            _reading = root.Q<VisualElement>("reading");
            _readingTitle = root.Q<Label>("reading-title");
            _readingText = root.Q<Label>("reading-text");
            _notes = root.Q<VisualElement>("notes");
            _notesList = root.Q<ScrollView>("notes-list");
            if (_time == null || _prompt == null || _reading == null || _readingTitle == null || _readingText == null || _notes == null || _notesList == null)
            {
                _time = null;
                return false;
            }

            return true;
        }

        private void Update()
        {
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

        // The nearest named person within reach and in front of the camera, named over their head.
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
                    if (distance <= best && camera.WorldToViewportPoint(person.TagPosition).z > 0f)
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
                    return DoorPrompt;
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

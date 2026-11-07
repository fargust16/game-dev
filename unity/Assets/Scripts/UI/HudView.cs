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

        private Label _time;
        private Label _prompt;
        private VisualElement _reading;
        private Label _readingTitle;
        private Label _readingText;
        private long _shownTick = -1;
        private PlayerLocomotion _player;
        private Portal[] _portals;
        private List<IPromptSource> _sources;

        public string TimeText => _time?.text;

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

        private void Start()
        {
            if (!Bind())
            {
                Debug.LogError("The HUD's UXML needs the labels 'time', 'prompt', 'reading-title' and 'reading-text' and the element 'reading'.", this);
                enabled = false;
                return;
            }

            _player = FindAnyObjectByType<PlayerLocomotion>();
            _portals = FindObjectsByType<Portal>(FindObjectsInactive.Exclude);
            _sources = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude).OfType<IPromptSource>().ToList();
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
            if (_time == null || _prompt == null || _reading == null || _readingTitle == null || _readingText == null)
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

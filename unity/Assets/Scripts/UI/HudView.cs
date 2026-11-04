using GameDev.Core.Scenes;
using GameDev.Locomotion;
using GameDev.World.Portals;
using GameDev.World.Timekeeping;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameDev.UI
{
    /// <summary>
    /// The slice HUD (17 X.6), on UI Toolkit: the day and time from the running clock, and a prompt while the player is
    /// within reach of a door. Presentation only: it reads the clock and the doors and changes nothing.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudView : MonoBehaviour
    {
        private const string PromptVisible = "hud__prompt--visible";

        private Label _time;
        private Label _prompt;
        private long _shownTick = -1;
        private PlayerLocomotion _player;
        private Portal[] _portals;

        public string TimeText => _time?.text;

        public bool IsPromptVisible => _prompt != null && _prompt.ClassListContains(PromptVisible);

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _time = root.Q<Label>("time");
            _prompt = root.Q<Label>("prompt");
            _player = FindAnyObjectByType<PlayerLocomotion>();
            _portals = FindObjectsByType<Portal>(FindObjectsInactive.Exclude);
            if (_time == null || _prompt == null)
            {
                Debug.LogError("The HUD's UXML needs the labels 'time' and 'prompt'.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            var clock = GameClockDriver.Running;
            if (clock != null && clock.Tick != _shownTick)
            {
                _shownTick = clock.Tick;
                _time.text = HudText.DayAndTime(clock.Day, clock.MinuteOfDay);
            }

            _prompt.EnableInClassList(PromptVisible, NearDoor());
        }

        private bool NearDoor()
        {
            if (_player == null || SceneTransition.IsTraveling)
            {
                return false;
            }

            var position = _player.transform.position;
            foreach (var portal in _portals)
            {
                if (portal != null && portal.InReach(position))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

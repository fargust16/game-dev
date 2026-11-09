using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Input;
using GameDev.Locomotion;
using GameDev.UI;
using GameDev.World.Poi;
using UnityEngine;

namespace GameDev.Slice
{
    /// <summary>
    /// A source the player reads (09; S1.3): the Notice Board. Interact in reach shows what it says now in the HUD's
    /// plain panel; Interact again, or walking away, closes it. Reading does not pause the world. People are asked in
    /// conversation instead (<see cref="PersonTalk"/>, S2.4). Its words are string table keys (17 X.7), and an open panel
    /// follows a change of language.
    /// </summary>
    public sealed class TipSourcePoint : MonoBehaviour, IPromptSource
    {
        [SerializeField] private TipSource source;

        [Tooltip("The string table key of the panel's title, e.g. the board's name.")]
        [SerializeField] private string titleKey = "source.board.title";

        [Tooltip("The string table key of what the panel says when the source has nothing now.")]
        [SerializeField] private string nothingKey = "source.board.nothing";

        [Tooltip("Authoring default, m: horizontal reach from this point.")]
        [SerializeField] private float interactRadius = 2f;

        private PlayerLocomotion _player;
        private HudView _hud;
        private IReadOnlyList<Tip> _shown = new Tip[0];

        public TipSource Source => source;

        public bool IsOpen { get; private set; }

        /// <summary>Setup for scenes built by code; a placed source is set in the Inspector.</summary>
        public void Configure(TipSource tipSource, string panelTitleKey, string nothingLineKey, float radius)
        {
            source = tipSource;
            titleKey = panelTitleKey;
            nothingKey = nothingLineKey;
            interactRadius = radius;
        }

        /// <summary>True when the player stands within reach on the same level and sees the source.</summary>
        public bool InReach(Vector3 position) => InteractReach.Within(transform, position, interactRadius);

        public string PromptFor(Vector3 player) => !IsOpen && InReach(player) ? Loc.Get("ui.prompt.read") : null;

        /// <summary>What Interact does: opens the panel with what the source says now, or closes it.</summary>
        public void Use()
        {
            if (IsOpen)
            {
                Close();
                return;
            }

            var world = SliceWorldDriver.Running;
            var hud = Hud();
            if (world == null || hud == null)
            {
                return;
            }

            _shown = world.Tips.Read(source, world.Clock.Tick);
            IsOpen = true;
            Show();
        }

        // What was said stays what was said; only its words follow the language.
        private void Show()
        {
            if (IsOpen)
            {
                Hud()?.ShowReading(Loc.Get(titleKey), _shown.Count > 0 ? string.Join("\n\n", _shown.Select(t => t.Line)) : Loc.Get(nothingKey));
            }
        }

        private void OnEnable()
        {
            Loc.Changed += Show;
        }

        private void Close()
        {
            IsOpen = false;
            Hud()?.CloseReading();
        }

        private void Update()
        {
            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerLocomotion>();
                if (_player == null)
                {
                    return;
                }
            }

            var inReach = InReach(_player.transform.position);
            if (IsOpen && !inReach)
            {
                Close();
                return;
            }

            var input = GameInputService.Instance;
            if (inReach && input != null && input.InteractPressed)
            {
                Use();
            }
        }

        private void OnDisable()
        {
            Loc.Changed -= Show;
            if (IsOpen)
            {
                Close();
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
    }
}

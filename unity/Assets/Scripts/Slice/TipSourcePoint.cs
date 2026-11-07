using System;
using System.Linq;
using GameDev.Input;
using GameDev.Locomotion;
using GameDev.UI;
using GameDev.World.Poi;
using UnityEngine;

namespace GameDev.Slice
{
    /// <summary>
    /// A source the player reads or asks (09; S1.3): the Notice Board, and the publican and the newsboy from S1.5.
    /// Interact in reach shows what it says now in the HUD's plain panel until S2's dialogue replaces it; Interact
    /// again, or walking away, closes it. Asking a person is dialogue, so the world pauses while the panel is open
    /// and the talk costs one tick when it ends (10.4); reading a board does not pause it.
    /// </summary>
    public sealed class TipSourcePoint : MonoBehaviour, IPromptSource
    {
        [SerializeField] private TipSource source;

        [Tooltip("The panel's title, e.g. the board's or the person's name.")]
        [SerializeField] private string title = "Notice Board";

        [Tooltip("What the panel says when the source has nothing now.")]
        [SerializeField] private string nothingLine = "Nothing new.";

        [Tooltip("Authoring default, m: horizontal reach from this point.")]
        [SerializeField] private float interactRadius = 2f;

        private PlayerLocomotion _player;
        private HudView _hud;
        private IDisposable _pause;

        public TipSource Source => source;

        public bool IsOpen { get; private set; }

        /// <summary>Setup for scenes built by code; a placed source is set in the Inspector.</summary>
        public void Configure(TipSource tipSource, string panelTitle, string nothing, float radius)
        {
            source = tipSource;
            title = panelTitle;
            nothingLine = nothing;
            interactRadius = radius;
        }

        /// <summary>
        /// True when the player stands within reach on the same level and sees the source: the publican could be asked
        /// from the landing above the taproom (owner's play test VI-5, remark 2). Walls, floors and ceilings block the
        /// sight line, taken at chest height; a counter, which is lower, does not.
        /// </summary>
        public bool InReach(Vector3 position)
        {
            var offset = position - transform.position;
            if (offset.y < -SameLevel || offset.y > SameLevel)
            {
                return false;
            }

            offset.y = 0f;
            if (offset.sqrMagnitude > interactRadius * interactRadius)
            {
                return false;
            }

            // The source's own body (a notice board) is the last thing on the line, so hitting it first means nothing
            // stands between.
            var mask = LayerMask.GetMask("Ground", "Structure");
            return mask == 0
                   || !Physics.Linecast(position + (Vector3.up * Chest), transform.position + (Vector3.up * Chest), out var hit, mask, QueryTriggerInteraction.Ignore)
                   || hit.collider.transform.IsChildOf(transform);
        }

        /// <summary>Authoring default, m: a source more than this above or below the player stands on another level.</summary>
        private const float SameLevel = 1.5f;

        /// <summary>Authoring default, m: the sight line's height over the feet, above a counter's 1.05 m.</summary>
        private const float Chest = 1.4f;

        public string PromptFor(Vector3 player) =>
            !IsOpen && InReach(player) ? (source == TipSource.NoticeBoard ? "E  Read" : "E  Ask") : null;

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

            var lines = world.Tips.Read(source, world.Clock.Tick);
            hud.ShowReading(title, lines.Count > 0 ? string.Join("\n\n", lines.Select(t => t.line)) : nothingLine);
            IsOpen = true;
            if (source != TipSource.NoticeBoard)
            {
                _pause = world.Clock.Pause();
            }
        }

        private void Close()
        {
            IsOpen = false;
            Hud()?.CloseReading();
            if (_pause != null)
            {
                _pause.Dispose();
                _pause = null;
                SliceWorldDriver.Running?.Clock.AdvanceTicks(1);
            }
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

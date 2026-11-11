using System;
using GameDev.Core.Localization;
using GameDev.Input;
using GameDev.Locomotion;
using GameDev.UI;
using UnityEngine;

namespace GameDev.Slice.Gathering
{
    /// <summary>
    /// A herb or flax patch, a fellable tree or an iron vein in the scene (S5a.5): E within reach begins gathering it, and
    /// E again, a step, the bag or a fight ends it. While it is gathered the prompt shows what was taken and the Stamina
    /// left; a spent node hides its models and colliders until it comes back. The driver puts one on each node's kit block
    /// when a scene loads, so no scene is changed for it.
    /// </summary>
    public sealed class GatherNode : MonoBehaviour, IPromptSource
    {
        /// <summary>Authoring default, s: how long the reason gathering ended stays in the prompt.</summary>
        public const float NoteSeconds = 4f;

        // E that ends one gathering must not begin another at a node beside it in the same frame.
        private static int _endedFrame = -1;

        private SliceGathering _gathering;
        private Func<bool> _busy;
        private PlayerLocomotion _player;
        private HudView _hud;
        private Vector3 _from;
        private int _seenStops;
        private float _noteUntil;
        private string _kind;

        public GatherSpot Spot { get; private set; }

        public bool Shown { get; private set; } = true;

        public bool IsActive => _gathering != null && _gathering.Active == Spot;

        /// <param name="busy">True while the character cannot gather: in a fight.</param>
        public void Configure(SliceGathering gathering, GatherSpot spot, Func<bool> busy)
        {
            _gathering = gathering;
            _busy = busy;
            Spot = spot;
            _kind = spot.Rule.kind.ToString().ToLowerInvariant();
            spot.Shown = Show;
            _seenStops = gathering.Stops;
            Show(gathering.Left(spot) > 0);
        }

        /// <summary>Shows or hides the node's models and colliders; a spent node leaves no trace (19 §6.4).</summary>
        public void Show(bool shown)
        {
            Shown = shown;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = shown;
            }

            foreach (var c in GetComponentsInChildren<Collider>(true))
            {
                c.enabled = shown;
            }
        }

        public bool InReach(Vector3 position) => _gathering != null && InteractReach.Within(transform, position, _gathering.Rules.Reach);

        public string PromptFor(Vector3 player)
        {
            if (_gathering == null)
            {
                return null;
            }

            if (IsActive)
            {
                var left = Mathf.RoundToInt((float)(_gathering.Stamina.Share * 100d));
                return Loc.Format("ui.gather.working", Loc.Get("ui.gather.doing." + _kind), _gathering.TakenNow, left);
            }

            if (!Shown || _gathering.Active != null || !InReach(player))
            {
                return null;
            }

            if (_gathering.StoppedAt == Spot && _gathering.Stopped != null && Time.unscaledTime < _noteUntil)
            {
                return Loc.Get(_gathering.Stopped);
            }

            var fault = _gathering.Fault(Spot);
            if (fault == "ui.gather.fault.tool")
            {
                return Loc.Format(fault, Loc.Get("item." + Spot.Rule.tool));
            }

            return Loc.Get(fault ?? "ui.prompt.gather." + _kind);
        }

        private void Update()
        {
            if (_gathering == null)
            {
                return;
            }

            if (_gathering.Stops != _seenStops)
            {
                _seenStops = _gathering.Stops;
                if (_gathering.StoppedAt == Spot)
                {
                    _noteUntil = Time.unscaledTime + NoteSeconds;
                }
            }

            var input = GameInputService.Instance;
            _player ??= FindAnyObjectByType<PlayerLocomotion>();
            if (_player == null)
            {
                return;
            }

            var at = _player.transform.position;
            if (IsActive)
            {
                _hud ??= FindAnyObjectByType<HudView>();
                // A step or E is the player's own choice and ends the work with no note (owner's play test of S5a,
                // 2026-11-11: "Interrupted, you moved" stayed on after walking away and back).
                var step = _gathering.Rules.Step;
                var own = (at - _from).sqrMagnitude > step * step || (input != null && input.InteractPressed);
                var reason = _hud != null && _hud.IsBagOpen ? "ui.gather.stop.bag"
                    : _busy != null && _busy() ? "ui.gather.stop.fight"
                    : null;
                if (own || reason != null)
                {
                    _gathering.Stop(reason);
                    _endedFrame = Time.frameCount;
                }

                return;
            }

            if (input != null && input.InteractPressed && Shown && _gathering.Active == null && _endedFrame != Time.frameCount
                && InReach(at) && (_busy == null || !_busy()) && _gathering.Begin(Spot) == null)
            {
                _from = at;
            }
        }

        // A scene left behind leaves its nodes: the spot no longer tells this view, and a gathering here ends.
        private void OnDestroy()
        {
            if (Spot != null && Spot.Shown != null && ReferenceEquals(Spot.Shown.Target, this))
            {
                Spot.Shown = null;
            }

            if (IsActive)
            {
                _gathering.Stop(null);
            }
        }
    }
}

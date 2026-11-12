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
    /// E again, a move key, the bag or a fight ends it. The work goes by its animation (07.1 §8a, the owner's second
    /// direction of 2026-11-11): at a tree or a vein each swing stops where the blade or the pick meets it for its Reaction
    /// Check, the world paused, and the next swing follows once the last has played out; a patch is picked in one go, its
    /// check as the character begins to rise. While it is gathered the prompt shows what was taken and the Stamina left, and
    /// why it ended is a Screen Message, never a prompt that keeps E from the next try (owner, 2026-11-12); a spent node hides
    /// its models and colliders until it comes back. The driver puts one on each node's kit block when a scene loads, so
    /// no scene is changed for it.
    /// </summary>
    public sealed class GatherNode : MonoBehaviour, IPromptSource
    {
        /// <summary>
        /// Authoring defaults, m: how far beyond the node's collider the character stands to work it, so the axe meets the
        /// trunk and the pick the rock (their reach ahead of the chest, measured in `clip_survey.txt`), and the hands the patch.
        /// </summary>
        public const float SwingReach = 0.75f;

        public const float PickReach = 0.35f;

        // E that ends one gathering must not begin another at a node beside it in the same frame.
        private static int _endedFrame = -1;

        private SliceGathering _gathering;
        private PlayerWork _work;
        private WorkStroke _stroke;
        private Func<bool> _busy;
        private PlayerLocomotion _player;
        private HudView _hud;
        private int _seenStops;
        private int _begunFrame;
        private bool _finishing;
        private string _kind;

        public GatherSpot Spot { get; private set; }

        public bool Shown { get; private set; } = true;

        public bool IsActive => _gathering != null && _gathering.Active == Spot;

        /// <summary>Whether the spot is a herb or flax patch, picked in one go.</summary>
        public bool Patch => Spot != null && (Spot.Rule.kind == GatherKind.Herb || Spot.Rule.kind == GatherKind.Flax);

        /// <param name="busy">True while the character cannot gather: in a fight.</param>
        /// <param name="work">The character's work by its animation, with its Reaction Check at each key frame.</param>
        public void Configure(SliceGathering gathering, GatherSpot spot, Func<bool> busy, PlayerWork work)
        {
            _gathering = gathering;
            _busy = busy;
            _work = work;
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
                var tool = Spot.Rule.tool;
                if (string.IsNullOrEmpty(tool))
                {
                    return Loc.Format("ui.gather.working", Loc.Get("ui.gather.doing." + _kind), _gathering.TakenNow, left);
                }

                var held = Mathf.RoundToInt(100f * (SliceGathering.ToolDurability - _gathering.Wear(tool)) / SliceGathering.ToolDurability);
                return Loc.Format("ui.gather.working.tool", Loc.Get("ui.gather.doing." + _kind), _gathering.TakenNow, left, held);
            }

            if (!Shown || _gathering.Active != null || !InReach(player))
            {
                return null;
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
                if (_gathering.StoppedAt == Spot && _gathering.Stopped != null)
                {
                    _hud ??= FindAnyObjectByType<HudView>();
                    _hud?.ShowMessage(Loc.Get(_gathering.Stopped));
                }
            }

            if (_stroke != null && !IsActive && !_finishing)
            {
                // The gathering ended some other way, a beast's notice say: the swing and its bar go with it. A check that
                // ended it itself, the patch picked or the node spent, lets its swing play out.
                var stroke = _stroke;
                _stroke = null;
                if (_work != null && _work.Stroke == stroke)
                {
                    _work.Stop();
                }
            }

            var input = GameInputService.Instance;
            _player ??= FindAnyObjectByType<PlayerLocomotion>();
            if (_player == null)
            {
                return;
            }

            if (IsActive)
            {
                // E is the player's own choice and ends the work with no note (owner's play test of S5a, 2026-11-11); a
                // move key ends it too, through the work itself.
                _hud ??= FindAnyObjectByType<HudView>();
                var own = input != null && input.InteractPressed && Time.frameCount != _begunFrame;
                var reason = _hud != null && _hud.IsBagOpen ? "ui.gather.stop.bag"
                    : _busy != null && _busy() ? "ui.gather.stop.fight"
                    : null;
                if (own || reason != null || _stroke == null)
                {
                    _gathering.Stop(reason);
                    _endedFrame = Time.frameCount;
                }

                return;
            }

            if (input != null && input.InteractPressed && Shown && _gathering.Active == null && _endedFrame != Time.frameCount
                && _work != null && !_work.Busy && InReach(_player.transform.position) && (_busy == null || !_busy())
                && _gathering.Begin(Spot) == null)
            {
                _begunFrame = Time.frameCount;
                _finishing = false;
                _stroke = Stroke();
                if (!_work.Begin(_stroke))
                {
                    _stroke = null;
                    _gathering.Stop(null);
                }
            }
        }

        /// <summary>The work at this node: a swing at a tree or a vein, again and again, or a patch picked in one go.</summary>
        private WorkStroke Stroke()
        {
            var patch = Patch;
            return new WorkStroke
            {
                Clip = patch ? "Harvest" : Spot.Rule.kind == GatherKind.Tree ? "Chop" : "Mine",
                Bar = () => _gathering.NextBar(),
                CaptionKey = patch ? "ui.gather.caption.patch" : "ui.gather.caption",
                Settle = result =>
                {
                    if (!IsActive)
                    {
                        return;
                    }

                    if (patch)
                    {
                        _gathering.Harvest(result);
                    }
                    else
                    {
                        _gathering.Cycle(result);
                    }

                    _finishing = !IsActive;
                },
                Again = () => IsActive && !patch,
                Ended = broken =>
                {
                    _stroke = null;
                    _finishing = false;
                    if (IsActive)
                    {
                        _gathering.Stop(null);
                        _endedFrame = Time.frameCount;
                    }
                },
                Face = transform,
                StandOff = PlayerWork.Radius(transform) + (patch ? PickReach : SwingReach),
            };
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

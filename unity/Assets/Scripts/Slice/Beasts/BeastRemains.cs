using System;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Input;
using GameDev.Locomotion;
using GameDev.UI;
using UnityEngine;

namespace GameDev.Slice.Beasts
{
    /// <summary>
    /// A fallen beast's carcass as something to take from (S5a.6): within reach, E takes its ingredients, and E again
    /// skins it once Skinning is learned: the character kneels at the carcass, its 3 ticks pass with the world running,
    /// and the Reaction Check comes where it begins to rise, the world paused (the owner's second direction of 2026-11-11).
    /// The prompt says what is left or why not, and for a few seconds after, what was taken. The driver puts one on every
    /// beast; it speaks only while the beast is dead.
    /// </summary>
    public sealed class BeastRemains : MonoBehaviour, IPromptSource
    {
        /// <summary>Authoring default, m: how near the carcass the character stands to take from it.</summary>
        public const float Reach = 2.5f;

        /// <summary>Authoring default, s: how long what was taken stays in the prompt.</summary>
        public const float NoteSeconds = 4f;

        /// <summary>Authoring default, m: how far beyond the carcass's edge the character kneels, its hands at the hide.</summary>
        public const float KneelReach = 0.35f;

        private SliceBeast _beast;
        private SliceRemains _remains;
        private Func<bool> _busy;
        private PlayerWork _work;
        private PlayerLocomotion _player;
        private string _note;
        private float _noteUntil;

        /// <param name="work">The character's work by its animation, the check where the kneeling character rises.</param>
        public void Configure(SliceRemains remains, Func<bool> busy, PlayerWork work)
        {
            _remains = remains;
            _busy = busy;
            _work = work;
            _beast = GetComponent<SliceBeast>();
        }

        private bool Ready => _remains != null && _beast != null && _beast.State == BeastState.Dead && (_busy == null || !_busy())
            && (_work == null || !_work.Busy);

        public string PromptFor(Vector3 player)
        {
            if (!Ready || !InteractReach.Within(transform, player, Reach))
            {
                return null;
            }

            if (_note != null && Time.unscaledTime < _noteUntil)
            {
                return _note;
            }

            if (!_remains.PartsTaken(_beast.Id))
            {
                return Loc.Get("ui.prompt.remains.parts");
            }

            if (_remains.Skinned(_beast.Id))
            {
                return null;
            }

            var fault = _remains.SkinFault(_beast.Id, _beast.Kind, _beast.Glowing);
            return fault == null ? Loc.Format("ui.prompt.remains.skin", _remains.Rules.SkinningTicks) : Loc.Get(fault);
        }

        private void Update()
        {
            if (!Ready)
            {
                return;
            }

            var input = GameInputService.Instance;
            if (input == null || !input.InteractPressed)
            {
                return;
            }

            _player ??= FindAnyObjectByType<PlayerLocomotion>();
            if (_player == null || !InteractReach.Within(transform, _player.transform.position, Reach))
            {
                return;
            }

            if (!_remains.PartsTaken(_beast.Id))
            {
                var parts = _remains.PartsOf(_beast.Kind, _beast.Glowing);
                var fault = _remains.TakeParts(_beast.Id, _beast.Kind, _beast.Glowing);
                Say(fault != null ? Loc.Get(fault)
                    : Loc.Format("ui.remains.taken", string.Join(", ", parts.Select(p => Loc.Format("ui.craft.part", p.count, Loc.Get("item." + p.item))))));
                return;
            }

            if (_remains.Skinned(_beast.Id))
            {
                return;
            }

            var refused = _remains.SkinFault(_beast.Id, _beast.Kind, _beast.Glowing);
            if (refused != null || _work == null)
            {
                Say(Loc.Get(refused ?? "ui.remains.interrupted"));
                return;
            }

            var beast = _beast;
            var stroke = new WorkStroke
            {
                Clip = "Kneel",
                Steady = true,
                Bar = () => _remains.Bar(beast.Id),
                CaptionKey = "ui.remains.caption",
                Settle = result =>
                {
                    var leather = _remains.Finish(beast.Id, beast.Kind, beast.Glowing, result);
                    var whole = _remains.Rules.Of(beast.Kind, beast.Glowing).leather;
                    Say(leather < whole ? Loc.Format("ui.remains.spoiled", leather) : Loc.Format("ui.remains.skinned", leather));
                },
                Face = transform,
                StandOff = PlayerWork.Radius(transform) + KneelReach,
            };
            if (!_work.Begin(stroke))
            {
                return;
            }

            // The ticks pass as the character kneels; a fight begun in them ends the work with the hide still on.
            if (_remains.Work(beast.Id, beast.Kind, beast.Glowing, _busy, out _) != SkinOutcome.Ready)
            {
                _work.Stop();
                Say(Loc.Get("ui.remains.interrupted"));
            }
        }

        private void Say(string note)
        {
            _note = note;
            _noteUntil = Time.unscaledTime + NoteSeconds;
        }
    }
}

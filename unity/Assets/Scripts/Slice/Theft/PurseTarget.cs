using System;
using GameDev.Core.Localization;
using GameDev.Input;
using GameDev.Items;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.Stealth;
using GameDev.UI;
using UnityEngine;

namespace GameDev.Slice.Theft
{
    /// <summary>
    /// A townsperson's purse as something to lift (S5a.8): within reach and in Stealth Mode only, E sends the crouched
    /// hand to the pocket, and where it reaches in the Reaction Check of theft shows, against the person's Perception,
    /// Alert or not. The world does not pause for it, and the try is called off, not spent, once the person has moved
    /// 1 m from where it stood (the owner's second direction of 2026-11-11, 10.3 §6). A good result takes the purse
    /// unseen; a bad one makes the person notice the character, cry out and stop to face it. The driver puts one on every
    /// walking townsperson when a scene loads.
    /// </summary>
    public sealed class PurseTarget : MonoBehaviour, IPromptSource
    {
        /// <summary>Authoring default, m: how near the person the character stands to reach the purse.</summary>
        public const float Reach = 1.5f;

        /// <summary>Authoring default, s: how long the outcome stays in the prompt, and the cry over the person's head.</summary>
        public const float NoteSeconds = 4f;

        /// <summary>The owner's, m (2026-11-11): how far the person may move from where it stood before the try is called off.</summary>
        public const float CallOffDistance = 1f;

        /// <summary>Authoring default, m: how far from the person's pivot the character crouches to reach the pocket.</summary>
        public const float StandOff = 0.6f;

        /// <summary>Authoring default: the Perception of a person with no Vision Cone, a townsman's (10.3 §6).</summary>
        public const int PlainPerception = 2;

        private SliceTheft _theft;
        private PlayerWork _work;
        private Func<bool> _busy;
        private Func<bool> _sneaking;
        private StealthObserver _eyes;
        private PlayerLocomotion _player;
        private HudView _hud;
        private string _note;
        private float _noteUntil;
        private bool _settled;

        /// <summary>The person's name and place among its siblings in the scene, the same in every run, for the world state's keys.</summary>
        public string Person => $"{name}.{transform.GetSiblingIndex()}";

        /// <param name="work">The character's work by its animation, the check where the hand reaches the pocket.</param>
        /// <param name="sneaking">Whether the character is in Stealth Mode, the only state it steals in.</param>
        public void Configure(SliceTheft theft, PlayerWork work, Func<bool> busy, Func<bool> sneaking)
        {
            _theft = theft;
            _work = work;
            _busy = busy;
            _sneaking = sneaking;
            _eyes = GetComponent<StealthObserver>();
        }

        private bool Ready => _theft != null && _work != null && !_work.Busy && (_busy == null || !_busy())
            && _sneaking != null && _sneaking();

        public string PromptFor(Vector3 player)
        {
            if (_theft == null || !InteractReach.Within(transform, player, Reach))
            {
                return null;
            }

            if (_note != null && Time.unscaledTime < _noteUntil)
            {
                return _note;
            }

            return Ready && !_theft.TriedToday(Person) ? Loc.Get("ui.prompt.theft") : null;
        }

        private void Update()
        {
            if (!Ready)
            {
                return;
            }

            var input = GameInputService.Instance;
            if (input == null || !input.InteractPressed || _theft.TriedToday(Person))
            {
                return;
            }

            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerLocomotion>();
            }

            // E belongs to the prompt the HUD shows: a counter, a talk or a door beside a passer-by keeps it.
            if (_hud == null)
            {
                _hud = FindAnyObjectByType<HudView>();
            }

            if (_hud != null && _hud.PromptText != Loc.Get("ui.prompt.theft"))
            {
                return;
            }

            if (_player != null && InteractReach.Within(transform, _player.transform.position, Reach))
            {
                // The stealth host gives a townsperson its eyes after the scene loads, so they are looked up now.
                _eyes = GetComponent<StealthObserver>();
                var perception = _eyes != null ? _eyes.Perception : PlainPerception;
                var alert = _eyes != null && _eyes.Meter != null && _eyes.Meter.Alert;
                var stood = Flat(transform.position);
                _settled = false;
                _work.Begin(new WorkStroke
                {
                    Clip = "Gather2",
                    Bar = () => _theft.Bar(Person, perception, alert),
                    CaptionKey = "ui.theft.caption",
                    PauseWorld = false,
                    Settle = Settle,
                    CalledOff = () => Vector3.Distance(Flat(transform.position), stood) > CallOffDistance,
                    Ended = broken =>
                    {
                        if (broken && !_settled)
                        {
                            Say(Loc.Get("ui.theft.called_off"));
                        }
                    },
                    Face = transform,
                    StandOff = StandOff,
                });
            }
        }

        private static Vector3 Flat(Vector3 at) => new Vector3(at.x, 0f, at.z);

        private void Settle(ReactionResult result)
        {
            _settled = true;
            if (_theft.Resolve(Person, result, out var bronze) == TheftOutcome.Lifted)
            {
                Say(Loc.Format("ui.theft.lifted", Coins.Show(bronze)));
                return;
            }

            Say(Loc.Get("ui.theft.noticed"));
            if (_eyes != null)
            {
                var at = _player != null ? _player.transform.position : transform.position;
                _eyes.Meter?.See(1e-6, 1f, at);

                // Noticed as Stealth Mode notices: the cry over its head, and a character in Stealth Mode stands up.
                var hiding = SliceWorldDriver.Running != null ? SliceWorldDriver.Running.Hiding : null;
                if (hiding != null)
                {
                    hiding.NoticedBy(_eyes, Loc.Get("ui.theft.shout"));
                }
                else
                {
                    _eyes.Say(Loc.Get("ui.theft.shout"), NoteSeconds);
                }
            }
        }

        private void Say(string note)
        {
            _note = note;
            _noteUntil = Time.unscaledTime + NoteSeconds;
        }
    }
}

using System;
using GameDev.Core.Localization;
using GameDev.Input;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.UI;
using UnityEngine;

namespace GameDev.Slice
{
    /// <summary>
    /// A Reaction Check in play (S5a.8; 20 §3, 10.3 §6): the bar on the HUD, its marker running once across it, and Space
    /// stopping it. The world does not pause; the character stands still while the bar shows, so Space does not jump.
    /// Theft uses it now, and breaking in and S5b's brewing will.
    /// </summary>
    public sealed class SliceReaction : MonoBehaviour
    {
        private ReactionBar _bar;
        private Action<ReactionResult> _done;
        private double _elapsed;
        private string _caption;
        private HudView _hud;
        private PlayerLocomotion _held;

        public bool Running => _done != null;

        public ReactionBar Bar => _bar;

        /// <summary>Where the marker is now, from 0 to 1.</summary>
        public double Marker => _bar.MarkerAt(_elapsed);

        /// <summary>Shows the bar and runs its marker; <paramref name="done"/> gets the result once it stops or leaves the bar.</summary>
        public bool Begin(ReactionBar bar, string captionKey, Action<ReactionResult> done)
        {
            if (Running || done == null)
            {
                return false;
            }

            _bar = bar;
            _done = done;
            _elapsed = 0d;
            _caption = Loc.Get(captionKey);
            _held = FindAnyObjectByType<PlayerLocomotion>();
            if (_held != null && _held.enabled)
            {
                _held.enabled = false;
            }
            else
            {
                _held = null;
            }

            Show();
            return true;
        }

        /// <summary>Stops the marker where it is now, as Space does.</summary>
        public void Press()
        {
            if (Running)
            {
                Finish(_bar.At(Marker));
            }
        }

        private void Update()
        {
            if (!Running)
            {
                return;
            }

            var input = GameInputService.Instance;
            if (input != null && input.JumpPressed)
            {
                Press();
                return;
            }

            _elapsed += Time.deltaTime;
            if (Marker > 1d)
            {
                Finish(ReactionResult.Bad);
                return;
            }

            Show();
        }

        private void Show()
        {
            if (_hud == null)
            {
                _hud = FindAnyObjectByType<HudView>();
            }

            if (_hud != null)
            {
                _hud.ShowReaction(_caption, (float)_bar.GoodStart, (float)_bar.GoodWidth, (float)_bar.PerfectStart, (float)_bar.PerfectWidth, (float)Math.Min(1d, Marker));
            }
        }

        private void Finish(ReactionResult result)
        {
            var done = _done;
            _done = null;
            if (_hud == null)
            {
                _hud = FindAnyObjectByType<HudView>();
            }

            if (_hud != null)
            {
                _hud.HideReaction();
            }
            if (_held != null)
            {
                _held.enabled = true;
                _held = null;
            }

            done(result);
        }

        private void OnDisable()
        {
            if (Running)
            {
                Finish(ReactionResult.Bad);
            }
        }
    }
}

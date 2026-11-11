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
    /// stopping it. The world does not pause, but for theft, which holds the clock while its bar shows (07.1 §8a); the
    /// character stands still while the bar shows, so Space does not jump. Theft, gathering and Item Creation use it now,
    /// and breaking in and S5b's brewing will.
    /// </summary>
    public sealed class SliceReaction : MonoBehaviour
    {
        private ReactionBar _bar;
        private Action<ReactionResult> _done;
        private double _elapsed;
        private string _caption;
        private HudView _hud;
        private PlayerLocomotion _held;
        private IDisposable _hold;
        private int _releasedFrame;

        public bool Running => _done != null;

        public ReactionBar Bar => _bar;

        /// <summary>Where the marker is now, from 0 to 1.</summary>
        public double Marker => _bar.MarkerAt(_elapsed);

        /// <summary>
        /// Shows the bar and runs its marker; <paramref name="done"/> gets the result once it stops or leaves the bar.
        /// <paramref name="hold"/>, such as the clock's pause during theft, is let go before <paramref name="done"/> runs.
        /// </summary>
        public bool Begin(ReactionBar bar, string captionKey, Action<ReactionResult> done, IDisposable hold = null)
        {
            if (Running || done == null)
            {
                hold?.Dispose();
                return false;
            }

            _hold = hold;

            _bar = bar;
            _done = done;
            _elapsed = 0d;
            _caption = Loc.Get(captionKey);
            if (_held == null)
            {
                // Still held from the bar before, the character stays held through this one.
                var player = FindAnyObjectByType<PlayerLocomotion>();
                if (player != null && player.enabled)
                {
                    player.enabled = false;
                    _held = player;
                }
            }

            Show();
            return true;
        }

        /// <summary>Takes the bar away with no result, as when the work it serves has ended some other way.</summary>
        public void Cancel()
        {
            if (Running)
            {
                _done = null;
                Release();
            }
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
                // The character moves again a frame after the bar, so the Space that stopped it does not also jump.
                // A fight begun meanwhile holds the character itself and frees it when it ends.
                if (_held != null && Time.frameCount > _releasedFrame)
                {
                    var fights = SliceWorldDriver.Running != null ? SliceWorldDriver.Running.Fights : null;
                    _held.enabled = fights == null || !fights.InFight;
                    _held = null;
                }

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
            Release();
            done(result);
        }

        /// <summary>Hides the bar, frees the character and lets go of the hold.</summary>
        private void Release()
        {
            if (_hud == null)
            {
                _hud = FindAnyObjectByType<HudView>();
            }

            if (_hud != null)
            {
                _hud.HideReaction();
            }

            _releasedFrame = Time.frameCount;
            _hold?.Dispose();
            _hold = null;
        }

        private void OnDisable()
        {
            if (Running)
            {
                Finish(ReactionResult.Bad);
            }

            if (_held != null)
            {
                _held.enabled = true;
                _held = null;
            }
        }
    }
}

using GameDev.Locomotion;
using UnityEngine;

namespace GameDev.Slice.Alchemy
{
    /// <summary>
    /// A Limit potion's Assimilation in the scene (S5b.5; 20 §3): while the potion settles and no fight or conversation is
    /// on, the character crouches where it drank and cannot move, and the world's ticks pass quickly, the world going on,
    /// as a sleep's do (18 §3), <see cref="SliceTuning.AssimilationTicksPerSecond"/> of them a second. A fight that takes it
    /// in stops the passing, the character Stunned in it for the ticks left, each round one; a conversation stops it too.
    /// Once neither holds and ticks are left, the passing resumes. Each tick is the world's own, so the potion settles on
    /// the clock's tick boundary (<see cref="SliceAdvancement.Settle"/>), and the character stands again.
    /// </summary>
    public sealed class AssimilationRun : MonoBehaviour
    {
        /// <summary>The player's state while it settles: the crouching loop the controller has for a Yield.</summary>
        private static readonly int Crouch = Animator.StringToHash("Yield");

        private static readonly int Locomotion = Animator.StringToHash("Locomotion");

        private SliceWorldDriver _world;
        private PlayerLocomotion _player;
        private double _owed;

        /// <summary>Whether the ticks are passing now, the character crouched.</summary>
        public bool Running { get; private set; }

        public void Configure(SliceWorldDriver world) => _world = world;

        private static bool InFight(SliceWorldDriver w) => w.Fights != null && w.Fights.InFight;

        // A conversation, an Accusation or a paused world (the menu, a check) holds the ticks; the character stays crouched.
        private static bool Held(SliceWorldDriver w) =>
            InFight(w) || w.TalkOpen || (w.Accusation != null && w.Accusation.IsOpen) || w.Clock.IsPaused;

        private void Update()
        {
            var w = _world;
            var advancement = w != null ? w.Advancement : null;
            if (advancement == null)
            {
                return;
            }

            if (Running && _player == null)
            {
                Running = false; // a new scene's character: crouch it again
            }

            if (!advancement.Assimilating || InFight(w))
            {
                if (Running)
                {
                    Stop();
                }

                if (!InFight(w))
                {
                    advancement.Settle(); // ticks spent in a fight: the potion settles as soon as it is over
                }

                return;
            }

            if (!Running)
            {
                Begin();
                if (!Running)
                {
                    return;
                }
            }

            _player.enabled = false; // a closed conversation or page must not let it walk off
            if (Held(w))
            {
                return;
            }

            _owed += Time.deltaTime * Mathf.Max(0.1f, SliceTuning.Current.AssimilationTicksPerSecond);
            while (_owed >= 1d && advancement.Assimilating && !Held(w))
            {
                _owed -= 1d;
                w.Clock.AdvanceTicks(1);
            }

            if (!advancement.Assimilating)
            {
                Stop();
            }
        }

        private void Begin()
        {
            _player = _player != null ? _player : FindAnyObjectByType<PlayerLocomotion>();
            if (_player == null)
            {
                return;
            }

            Running = true;
            _owed = 0d;
            _player.enabled = false;
            var animator = _player.GetComponentInChildren<Animator>();
            if (animator != null && animator.HasState(0, Crouch))
            {
                animator.CrossFadeInFixedTime(Crouch, 0.4f, 0);
            }
        }

        private void Stop()
        {
            Running = false;
            if (_player == null)
            {
                return;
            }

            var inFight = _world != null && _world.Fights != null && _world.Fights.InFight;
            if (!inFight)
            {
                var animator = _player.GetComponentInChildren<Animator>();
                if (animator != null && animator.HasState(0, Locomotion))
                {
                    animator.CrossFadeInFixedTime(Locomotion, 0.4f, 0);
                }
            }

            _player.enabled = !inFight;
        }

        private void OnDisable()
        {
            if (Running)
            {
                Stop();
            }
        }
    }
}

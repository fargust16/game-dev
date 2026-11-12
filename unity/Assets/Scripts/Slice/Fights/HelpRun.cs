using GameDev.Slice.People;
using GameDev.World.Timekeeping;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// 10.2 §5a (owner, 2026-11-12; PB6): a resident who broke away goes to find help. It runs to the Warden it fled
    /// toward, stands by it a world tick, and walks its round again. Like a searcher it runs straight, its feet on the
    /// ground under it, stops at a wall in its way, and waits while the world is paused.
    /// </summary>
    public sealed class HelpRun : MonoBehaviour
    {
        private const float Reach = 1.6f; // m from the Warden, where it stops

        private static readonly int RunState = Animator.StringToHash("Run");
        private static readonly int IdleState = Animator.StringToHash("Idle");

        private Animator _animator;
        private TownsfolkWalker _walker;
        private IWorldClock _clock;
        private int _playing;

        /// <summary>Where it runs, the Warden's place.</summary>
        public Vector3 To { get; private set; }

        /// <summary>It stands by the Warden.</summary>
        public bool There { get; private set; }

        public static HelpRun Begin(Transform body, Vector3 to, IWorldClock clock)
        {
            var run = body.gameObject.AddComponent<HelpRun>();
            run.To = to;
            run._clock = clock;
            run._animator = body.GetComponentInChildren<Animator>();
            run._walker = body.GetComponent<TownsfolkWalker>();
            if (run._walker != null)
            {
                run._walker.enabled = false;
            }

            if (clock != null)
            {
                clock.TickBoundary += run.OnTick;
            }

            return run;
        }

        private void Update()
        {
            if (There || (_clock != null && _clock.IsPaused))
            {
                return;
            }

            if (FightSearch.Step(transform, To, FightSearch.RunSpeed, Reach, stopAtWall: true))
            {
                There = true;
                Play(IdleState);
                return;
            }

            Play(_animator != null && _animator.HasState(0, RunState) ? RunState : FightSearch.WalkStateHash);
        }

        private void OnTick(long tick)
        {
            if (!There)
            {
                return;
            }

            if (_walker != null && gameObject.activeInHierarchy)
            {
                _walker.enabled = true;
            }

            Destroy(this);
        }

        private void Play(int state)
        {
            if (_playing == state || _animator == null || !_animator.isActiveAndEnabled || !_animator.HasState(0, state))
            {
                return;
            }

            _playing = state;
            _animator.CrossFadeInFixedTime(state, 0.2f, 0);
        }

        private void OnDestroy()
        {
            if (_clock != null)
            {
                _clock.TickBoundary -= OnTick;
            }
        }
    }
}

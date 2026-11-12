using System.Collections.Generic;
using GameDev.World.Timekeeping;
using UnityEngine;

namespace GameDev.Slice.People
{
    /// <summary>
    /// 10.2 §5c *The fallen*: a free Warden walks to each body an Assault left, in turn, stoops over it in the gathering
    /// clip, and the body is gone at its end; then the Warden walks back to its post and stands at it again. It walks
    /// straight, its feet on the ground under it, and waits while the world is paused.
    /// </summary>
    public sealed class WardenErrand : MonoBehaviour
    {
        /// <summary>Authoring defaults: the walk, m/s, near the walk clip's measured pace (1.69–1.91 m/s, pipeline plan §13); how near a body it stoops, m.</summary>
        public const float WalkSpeed = 1.7f;

        public const float StandOff = 0.8f;

        /// <summary>The gathering clip's length, s (TownsfolkWalker.FetchSeconds, measured).</summary>
        public const float StoopSeconds = TownsfolkWalker.FetchSeconds;

        private const float TurnDegreesPerSecond = 360f;

        private static readonly int WalkState = Animator.StringToHash("Walk");
        private static readonly int IdleState = Animator.StringToHash("Idle");
        private static readonly int GatherState = Animator.StringToHash("Gather");

        private readonly Queue<GameObject> _bodies = new Queue<GameObject>();
        private Vector3 _post;
        private Quaternion _facing;
        private Animator _animator;
        private GameObject _stooping;
        private float _stoop;
        private int _playing;

        /// <summary>The bodies still to take up, the one it stoops over among them.</summary>
        public int Left => _bodies.Count + (_stooping != null ? 1 : 0);

        /// <summary>Sends <paramref name="warden"/> for <paramref name="bodies"/>, then back to <paramref name="post"/>.</summary>
        public static WardenErrand Send(Transform warden, IEnumerable<GameObject> bodies, Vector3 post, Quaternion facing)
        {
            var errand = warden.gameObject.AddComponent<WardenErrand>();
            foreach (var body in bodies)
            {
                errand._bodies.Enqueue(body);
            }

            errand._post = post;
            errand._facing = facing;
            errand._animator = warden.GetComponentInChildren<Animator>();
            return errand;
        }

        private void Update()
        {
            var clock = GameClockDriver.Running;
            if (clock != null && clock.IsPaused)
            {
                return;
            }

            if (_stooping != null)
            {
                _stoop += Time.deltaTime;
                if (_stoop >= StoopSeconds)
                {
                    _stooping.SetActive(false);
                    _stooping = null;
                }

                return;
            }

            while (_bodies.Count > 0 && (_bodies.Peek() == null || !_bodies.Peek().activeSelf))
            {
                _bodies.Dequeue();
            }

            var home = _bodies.Count == 0;
            var target = home ? _post : _bodies.Peek().transform.position;
            var way = Vector3.ProjectOnPlane(target - transform.position, Vector3.up);
            var reach = home ? 0.05f : StandOff;
            if (way.magnitude <= reach)
            {
                if (home)
                {
                    transform.SetPositionAndRotation(_post, _facing);
                    Play(IdleState);
                    Destroy(this);
                    return;
                }

                _stooping = _bodies.Dequeue();
                _stoop = 0f;
                Play(GatherState);
                return;
            }

            var step = Mathf.Min(way.magnitude - reach, WalkSpeed * Time.deltaTime);
            var next = transform.position + (way.normalized * step);
            next = PeopleAround.Ground(new Vector3(next.x, transform.position.y, next.z));
            var turn = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(way.normalized, Vector3.up), TurnDegreesPerSecond * Time.deltaTime);
            transform.SetPositionAndRotation(next, turn);
            Play(WalkState);
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

    }
}

using GameDev.World.Timekeeping;
using UnityEngine;

namespace GameDev.Slice.People
{
    /// <summary>
    /// An ordinary townsperson walking a closed route, round and round (owner, 2026-11-06): a stand-in for the schedules
    /// of S5c, which will replace the route. The route is planned in the editor over walkable ground, so the walker only
    /// follows it, keeps its feet on the ground under it, and turns smoothly. People do not collide with one another.
    /// <para>
    /// The walker keeps the world's time (owner's play test of 2026-11-11: townsfolk walked on through a fight). While the
    /// clock is paused, by a fight or a conversation, it stands still mid-stride; each whole tick the clock is moved while
    /// paused, a fight's round or a talk's end, it walks that tick's time at once, faster than life, so the world moves
    /// only after the round.
    /// </para>
    /// </summary>
    public sealed class TownsfolkWalker : MonoBehaviour
    {
        /// <summary>How many times faster than life a walker walks the ticks a paused world was moved by (authoring default).</summary>
        public const float CatchUp = 6f;

        [Tooltip("The closed route in world space; the last point leads back to the first.")]
        [SerializeField] private Vector3[] route = new Vector3[0];

        [Tooltip("Walking speed, m/s: measured from the walk clip's stride so the feet do not slide.")]
        [SerializeField] private float speed = 1.2f;

        [Tooltip("What the feet stand on.")]
        [SerializeField] private LayerMask ground = ~0;

        [SerializeField] private float turnDegreesPerSecond = 270f;

        private int _next;
        private IWorldClock _clock;
        private Animator _animator;
        private float _owed;

        public Vector3[] Route => route;

        public float Speed => speed;

        /// <summary>Seconds of walking the walker still owes the ticks a paused world was moved by.</summary>
        public float Owed => _owed;

        /// <summary>Editor setup.</summary>
        public void Configure(Vector3[] points, float metresPerSecond, LayerMask groundMask)
        {
            route = points;
            speed = metresPerSecond;
            ground = groundMask;
        }

        /// <summary>The route's length, m, round the whole loop.</summary>
        public float Length
        {
            get
            {
                var length = 0f;
                for (var i = 0; i < route.Length; i++)
                {
                    length += Flat(route[(i + 1) % route.Length] - route[i]).magnitude;
                }

                return length;
            }
        }

        private void Start()
        {
            if (route.Length < 2)
            {
                enabled = false;
                return;
            }

            _animator = GetComponentInChildren<Animator>();

            // Join the route at its nearest point and walk on from there.
            var best = float.MaxValue;
            for (var i = 0; i < route.Length; i++)
            {
                var d = Flat(route[i] - transform.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    _next = i;
                }
            }
        }

        private void OnDisable() => Watch(null);

        private void OnDestroy() => Watch(null);

        private void Update()
        {
            Watch(GameClockDriver.Running);
            var paused = _clock != null && _clock.IsPaused;
            float seconds;
            var pace = 1f;
            if (_owed > 0f)
            {
                seconds = Mathf.Min(_owed, Time.deltaTime * CatchUp);
                _owed -= seconds;
                pace = CatchUp;
            }
            else if (paused)
            {
                Pace(0f);
                return;
            }
            else
            {
                seconds = Time.deltaTime;
            }

            Pace(pace);
            Walk(seconds);
        }

        /// <summary>A whole tick moved while the world was paused: the walker owes its time.</summary>
        private void OnTick(long tick)
        {
            if (_clock != null && _clock.IsPaused)
            {
                _owed += GameClockDriver.RealSecondsPerTick;
            }
        }

        private void Watch(IWorldClock clock)
        {
            if (clock == _clock)
            {
                return;
            }

            if (_clock != null)
            {
                _clock.TickBoundary -= OnTick;
            }

            _clock = clock;
            if (_clock != null)
            {
                _clock.TickBoundary += OnTick;
            }
        }

        private void Pace(float pace)
        {
            if (_animator != null)
            {
                _animator.speed = pace;
            }
        }

        private void Walk(float seconds)
        {
            var position = transform.position;
            var step = speed * seconds;
            var target = route[_next];
            var to = Flat(target - position);
            for (var guard = 0; to.magnitude <= step && guard < route.Length; guard++)
            {
                _next = (_next + 1) % route.Length;
                target = route[_next];
                to = Flat(target - position);
            }

            if (to.sqrMagnitude < 1e-6f)
            {
                return;
            }

            var direction = to.normalized;
            position += direction * step;
            position.y = GroundAt(position, Mathf.Lerp(position.y, target.y, 0.5f));
            transform.position = position;

            var facing = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, turnDegreesPerSecond * seconds);
        }

        private float GroundAt(Vector3 at, float fallback)
        {
            return Physics.Raycast(at + (Vector3.up * 1.5f), Vector3.down, out var hit, 4f, ground, QueryTriggerInteraction.Ignore)
                ? hit.point.y
                : fallback;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f);
            for (var i = 0; i < route.Length; i++)
            {
                Gizmos.DrawLine(route[i] + (Vector3.up * 0.1f), route[(i + 1) % route.Length] + (Vector3.up * 0.1f));
            }
        }
    }
}

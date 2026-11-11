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
    /// <para>
    /// At one turn of its route in three the walker stops and stands 4 to 12 s in its idle before it walks on (the owner's
    /// play test of S5a: townsfolk who sometimes stop), the same turns on every run.
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

        /// <summary>Authoring default, the owner's play test of S5a: one turn in this many brings a stop.</summary>
        public const int StopOneIn = 3;

        /// <summary>Authoring default: a stop's shortest and longest standing, s.</summary>
        public const float StopShortest = 4f;

        public const float StopLongest = 12f;

        private static readonly int WalkState = Animator.StringToHash("Walk");
        private static readonly int IdleState = Animator.StringToHash("Idle");

        private int _next;
        private IWorldClock _clock;
        private Animator _animator;
        private float _owed;
        private float _standing;
        private uint _seed;
        private int _turns;

        public Vector3[] Route => route;

        public float Speed => speed;

        /// <summary>Seconds of walking the walker still owes the ticks a paused world was moved by.</summary>
        public float Owed => _owed;

        /// <summary>Seconds the walker still stands at a turn of its route.</summary>
        public float Standing => _standing;

        /// <summary>How many times the walker has stopped at a turn.</summary>
        public int Stops { get; private set; }

        /// <summary>How many turns of its route the walker has reached.</summary>
        public int Turns => _turns;

        /// <summary>
        /// Whether the walker stops at its <paramref name="turn"/>th turn, and for how long: one turn in
        /// <see cref="StopOneIn"/>, for 4 to 12 s, by an FNV-1a hash of its route's start and the turn, so a walker
        /// stops at the same turns on every run.
        /// </summary>
        public static float StopAt(uint seed, int turn)
        {
            var h = Fnv(seed, (uint)turn);
            return h % StopOneIn != 0 ? 0f : StopShortest + ((StopLongest - StopShortest) * ((h / StopOneIn) % 1000u) / 999f);
        }

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
            _seed = Fnv((uint)Mathf.RoundToInt(route[0].x * 10f), (uint)Mathf.RoundToInt(route[0].z * 10f));

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

            // Standing on a turning point already, as a placed walker does, it sets out for the next: no stop before its
            // first step.
            if (best < 0.01f)
            {
                _next = (_next + 1) % route.Length;
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
            // A stop at a turn takes its time from the walk, the time a paused world was moved by too.
            if (_standing > 0f)
            {
                var stood = Mathf.Min(_standing, seconds);
                _standing -= stood;
                seconds -= stood;
                if (_standing > 0f)
                {
                    return;
                }

                Play(WalkState);
            }

            var position = transform.position;
            var step = speed * seconds;
            var target = route[_next];
            var to = Flat(target - position);
            for (var guard = 0; to.magnitude <= step && guard < route.Length; guard++)
            {
                _next = (_next + 1) % route.Length;
                var stop = StopAt(_seed, _turns++);
                if (stop > 0f)
                {
                    // The turn reached, the walker stands there a while before it walks on.
                    target.y = GroundAt(target, target.y);
                    transform.position = target;
                    _standing = stop;
                    Stops++;
                    Play(IdleState);
                    return;
                }

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

        private void Play(int state)
        {
            if (_animator != null && _animator.isActiveAndEnabled && _animator.HasState(0, state))
            {
                _animator.CrossFadeInFixedTime(state, 0.3f);
            }
        }

        private static uint Fnv(uint a, uint b)
        {
            var h = 2166136261u;
            foreach (var v in new[] { a, b })
            {
                for (var i = 0; i < 4; i++)
                {
                    h = (h ^ ((v >> (8 * i)) & 0xFFu)) * 16777619u;
                }
            }

            return h;
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

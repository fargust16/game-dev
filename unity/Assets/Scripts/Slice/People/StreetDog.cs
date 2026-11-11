using GameDev.Locomotion;
using GameDev.World.Timekeeping;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Slice.People
{
    /// <summary>
    /// The owner's dog experiment (2026-11-06): a German shepherd that either walks a closed route or sits, picked at
    /// random each time the last one ends, and greets the player with a wagging tail when the player comes close. A
    /// stand-in for the street animals of VS-BEASTS; it shows how the shepherd's clips play on the owner's dog.
    /// The Animator's states are named after the clips (<c>SliceDogs</c> builds it): Walking01, Breathing, SittingStart
    /// (which goes on to SittingCycle by itself), and WigglingTail. It stands still while the world clock is held.
    /// </summary>
    public sealed class StreetDog : MonoBehaviour
    {
        private const string Walk = "Walking01";
        private const string Sit = "SittingStart";
        private const string Greet = "WigglingTail";

        [Tooltip("The closed route in world space; the last point leads back to the first.")]
        [SerializeField] private Vector3[] route = new Vector3[0];

        [Tooltip("Walking speed, m/s: measured from the walk clip's stride so the paws do not slide.")]
        [SerializeField] private float speed = 1f;

        [Tooltip("What the paws stand on.")]
        [SerializeField] private LayerMask ground = ~0;

        [SerializeField] private Animator animator;

        [Tooltip("The dog greets the player within this distance, m (authoring default).")]
        [SerializeField] private float greetDistance = 3f;

        [Tooltip("And stops greeting beyond this one, m (authoring default).")]
        [SerializeField] private float leaveDistance = 4.5f;

        [Tooltip("How long one walk lasts, s (authoring default).")]
        [SerializeField] private Vector2 walkSeconds = new Vector2(8f, 20f);

        [Tooltip("How long one sit lasts, s (authoring default).")]
        [SerializeField] private Vector2 sitSeconds = new Vector2(6f, 15f);

        [Tooltip("The dog's own random stream: a layout constant, never the world seed.")]
        [SerializeField] private int seed = 1;

        [SerializeField] private float turnDegreesPerSecond = 220f;

        private enum Mode { Walking, Sitting, Greeting }

        private Mode _mode;
        private Mode _resume;
        private float _until;
        private int _next;
        private Random _random;
        private Transform _player;
        private float _lookForPlayer;

        public Vector3[] Route => route;

        public float Speed => speed;

        /// <summary>Editor setup.</summary>
        public void Configure(Vector3[] points, float metresPerSecond, LayerMask groundMask, Animator dogAnimator, int randomSeed)
        {
            route = points;
            speed = metresPerSecond;
            ground = groundMask;
            animator = dogAnimator;
            seed = randomSeed;
        }

        private void Start()
        {
            _random = new Random(seed);
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

            _mode = Mode.Greeting; // so the first choice always starts its clip
            Choose();
        }

        private void Update()
        {
            // While the clock is held (a fight, a conversation) the dog stands as still as the townsfolk, its clip too.
            var clock = GameClockDriver.Running;
            var paused = clock != null && clock.IsPaused;
            if (animator != null)
            {
                animator.speed = paused ? 0f : 1f;
            }

            if (paused)
            {
                _until += Time.deltaTime;
                return;
            }

            FindPlayer();
            var near = _player != null ? Flat(_player.position - transform.position).magnitude : float.MaxValue;
            if (_mode != Mode.Greeting && near <= greetDistance)
            {
                _resume = _mode;
                Enter(Mode.Greeting);
            }

            switch (_mode)
            {
                case Mode.Greeting:
                    if (_player != null)
                    {
                        TurnTo(_player.position - transform.position);
                    }

                    if (near > leaveDistance)
                    {
                        Enter(_resume);
                    }

                    break;
                case Mode.Walking:
                    Follow();
                    if (Time.time >= _until)
                    {
                        Choose();
                    }

                    break;
                default:
                    if (Time.time >= _until)
                    {
                        Choose();
                    }

                    break;
            }
        }

        /// <summary>Walk or sit, even odds; a dog with no route only sits.</summary>
        private void Choose()
        {
            Enter(route.Length >= 2 && _random.NextDouble() < 0.5 ? Mode.Walking : Mode.Sitting);
        }

        private void Enter(Mode mode)
        {
            var keep = mode == _mode && mode != Mode.Greeting && animator != null;
            _mode = mode;
            var span = mode == Mode.Walking ? walkSeconds : sitSeconds;
            _until = Time.time + Mathf.Lerp(span.x, span.y, (float)_random.NextDouble());
            if (animator == null || keep)
            {
                return;
            }

            animator.CrossFadeInFixedTime(mode == Mode.Walking ? Walk : mode == Mode.Sitting ? Sit : Greet, 0.3f);
        }

        private void Follow()
        {
            var position = transform.position;
            var step = speed * Time.deltaTime;
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
            position.y = Physics.Raycast(position + (Vector3.up * 1.5f), Vector3.down, out var hit, 4f, ground, QueryTriggerInteraction.Ignore)
                ? hit.point.y
                : Mathf.Lerp(position.y, target.y, 0.5f);
            transform.position = position;
            TurnTo(direction);
        }

        private void TurnTo(Vector3 direction)
        {
            direction = Flat(direction);
            if (direction.sqrMagnitude < 1e-6f)
            {
                return;
            }

            var facing = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, turnDegreesPerSecond * Time.deltaTime);
        }

        private void FindPlayer()
        {
            if (_player != null || Time.time < _lookForPlayer)
            {
                return;
            }

            _lookForPlayer = Time.time + 1f;
            var player = FindAnyObjectByType<PlayerLocomotion>();
            _player = player != null ? player.transform : null;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.6f, 0.2f);
            for (var i = 0; i < route.Length; i++)
            {
                Gizmos.DrawLine(route[i] + (Vector3.up * 0.1f), route[(i + 1) % route.Length] + (Vector3.up * 0.1f));
            }

            Gizmos.DrawWireSphere(transform.position, greetDistance);
        }
    }
}

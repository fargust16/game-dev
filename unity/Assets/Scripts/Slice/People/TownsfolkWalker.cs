using UnityEngine;

namespace GameDev.Slice.People
{
    /// <summary>
    /// An ordinary townsperson walking a closed route, round and round (owner, 2026-11-06): a stand-in for the schedules
    /// of S5c, which will replace the route. The route is planned in the editor over walkable ground, so the walker only
    /// follows it, keeps its feet on the ground under it, and turns smoothly. People do not collide with one another.
    /// </summary>
    public sealed class TownsfolkWalker : MonoBehaviour
    {
        [Tooltip("The closed route in world space; the last point leads back to the first.")]
        [SerializeField] private Vector3[] route = new Vector3[0];

        [Tooltip("Walking speed, m/s: measured from the walk clip's stride so the feet do not slide.")]
        [SerializeField] private float speed = 1.2f;

        [Tooltip("What the feet stand on.")]
        [SerializeField] private LayerMask ground = ~0;

        [SerializeField] private float turnDegreesPerSecond = 270f;

        private int _next;

        public Vector3[] Route => route;

        public float Speed => speed;

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

        private void Update()
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
            position.y = GroundAt(position, Mathf.Lerp(position.y, target.y, 0.5f));
            transform.position = position;

            var facing = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, turnDegreesPerSecond * Time.deltaTime);
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

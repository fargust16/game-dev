using System;
using UnityEngine;

namespace GameDev.Slice.Gear
{
    /// <summary>
    /// 10.2 §5e (PB8b): a shot out of a fight flies as a projectile with physics and is seen: a small glowing ball with a
    /// short trail, at the tuning page's speed, falling by its share of gravity, stopped by the first thing it touches.
    /// Each frame it sweeps the way it flew, so nothing thin is passed through; triggers and the shooter's own body are
    /// passed. What it touched is told once, and it is gone; nothing touched in its reach is a miss.
    /// </summary>
    public sealed class SliceProjectile : MonoBehaviour
    {
        // Authoring defaults, m: the ball's radius as seen and as it sweeps, and how long its trail is in seconds.
        private const float SeenRadius = 0.025f;
        private const float SweepRadius = 0.02f;
        private const float TrailSeconds = 0.06f;

        private static Material _look;

        private Vector3 _velocity;
        private float _gravity;
        private float _left;
        private Transform _shooter;
        private Action<Collider, Vector3> _touched;

        /// <summary>Where it flew from.</summary>
        public Vector3 From { get; private set; }

        /// <summary>
        /// Fires a projectile from <paramref name="from"/> along <paramref name="way"/> at <paramref name="speed"/> m/s,
        /// pulled down by <paramref name="gravityShare"/> of gravity, gone after <paramref name="reach"/> m.
        /// <paramref name="touched"/> hears the collider it touched and where, or null and its last place when it touched nothing.
        /// </summary>
        public static SliceProjectile Fire(Vector3 from, Vector3 way, float speed, float gravityShare, float reach, Transform shooter,
            Action<Collider, Vector3> touched)
        {
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Shot";
            DestroyImmediate(ball.GetComponent<Collider>());
            ball.transform.position = from;
            ball.transform.localScale = Vector3.one * (SeenRadius * 2f);
            var renderer = ball.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = Look();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var trail = ball.AddComponent<TrailRenderer>();
            trail.time = TrailSeconds;
            trail.startWidth = SeenRadius * 1.6f;
            trail.endWidth = 0f;
            trail.sharedMaterial = Look();
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.minVertexDistance = 0.05f;

            var shot = ball.AddComponent<SliceProjectile>();
            shot.From = from;
            shot._velocity = way.normalized * speed;
            shot._gravity = Physics.gravity.y * gravityShare;
            shot._left = reach;
            shot._shooter = shooter;
            shot._touched = touched;
            return shot;
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }

            var at = transform.position;
            _velocity.y += _gravity * dt;
            var step = _velocity * dt;
            var length = step.magnitude;
            if (Sweep(at, step, length, out var hit))
            {
                transform.position = hit.point;
                Tell(hit.collider, hit.point);
                return;
            }

            transform.position = at + step;
            _left -= length;
            if (_left <= 0f)
            {
                Tell(null, transform.position);
            }
        }

        /// <summary>The nearest thing on the way that is neither a trigger nor the shooter.</summary>
        private bool Sweep(Vector3 at, Vector3 step, float length, out RaycastHit nearest)
        {
            nearest = default;
            if (length <= 1e-6f)
            {
                return false;
            }

            var found = false;
            foreach (var hit in Physics.SphereCastAll(at, SweepRadius, step / length, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (_shooter != null && hit.collider.transform.IsChildOf(_shooter))
                {
                    continue;
                }

                // A cast that starts inside a collider reports it at distance 0 with no point: it counts where it began.
                var h = hit;
                if (h.distance <= 0f && h.point == Vector3.zero)
                {
                    h.point = at;
                }

                if (!found || h.distance < nearest.distance)
                {
                    nearest = h;
                    found = true;
                }
            }

            return found;
        }

        private void Tell(Collider touched, Vector3 at)
        {
            var told = _touched;
            _touched = null;
            enabled = false;
            told?.Invoke(touched, at);
            Destroy(gameObject, TrailSeconds);
        }

        /// <summary>The ball's and the trail's look: a warm unlit glow, so it is seen in the dark too.</summary>
        private static Material Look()
        {
            if (_look != null)
            {
                return _look;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            _look = new Material(shader) { name = "Shot" };
            var colour = new Color(1f, 0.82f, 0.45f);
            foreach (var property in new[] { "_BaseColor", "_Color" })
            {
                if (_look.HasProperty(property))
                {
                    _look.SetColor(property, colour);
                }
            }

            return _look;
        }
    }
}

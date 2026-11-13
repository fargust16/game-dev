using UnityEngine;

namespace GameDev.Confront.Grid
{
    /// <summary>One attack's line (10.1 *Shot Line*): whether it can be made, whether it is Obscured, and where it flies.</summary>
    public readonly struct ShotLine
    {
        public ShotLine(bool clear, bool obscured, Vector3 from, Vector3 to)
        {
            Clear = clear;
            Obscured = obscured;
            From = from;
            To = to;
        }

        /// <summary>At least one of the target's three points has a line free of solid obstacles.</summary>
        public bool Clear { get; }

        /// <summary>The line flown crosses a Partial Obstacle, or the target stands in one.</summary>
        public bool Obscured { get; }

        /// <summary>The muzzle or the caster's hand.</summary>
        public Vector3 From { get; }

        /// <summary>The first of the chest, the head and the hips with a free line, or the chest when none has one.</summary>
        public Vector3 To { get; }
    }

    /// <summary>
    /// 10.1 *Shot Line*, one rule for every projectile in a fight and out of one: the line runs from the shooter's chest to
    /// the target's chest, head and hips in that order, the first free of solid obstacles being the one flown; Partial
    /// Obstacles make it Obscured. People are no obstacle here: they intercept by the fight's rule, and out of a fight by
    /// the projectile touching them. Its queries never allocate.
    /// </summary>
    public static class ShotTrace
    {
        /// <summary>Authoring defaults, m over the feet: where a shot leaves a standing shooter, and the target's three points.</summary>
        public const float Muzzle = 1.4f, Chest = 1.4f, Head = 1.65f, Hips = 0.95f;

        /// <summary>The kit's NoGrid layer, which holds what is seen and never solid, Partial Obstacles among it.</summary>
        public const int NoGridLayer = 14;

        /// <summary>The place, as the grid reads it, and the NoGrid layer for Partial Obstacles; never the characters.</summary>
        public static readonly int Mask = CombatGridReader.Mask | (1 << NoGridLayer);

        private const float BodyRadius = 0.3f;
        private static readonly float[] Points = { Chest, Head, Hips };
        private static readonly RaycastHit[] Hits = new RaycastHit[32];
        private static readonly Collider[] Near = new Collider[16];

        /// <summary>The line from a shooter standing at <paramref name="shooterFeet"/> to a target standing at <paramref name="targetFeet"/>.</summary>
        public static ShotLine Trace(Vector3 shooterFeet, Vector3 targetFeet)
        {
            var from = shooterFeet + (Vector3.up * Muzzle);
            foreach (var height in Points)
            {
                var to = targetFeet + (Vector3.up * height);
                if (Free(from, to, out var partial))
                {
                    return new ShotLine(true, partial || StandsIn(targetFeet), from, to);
                }
            }

            return new ShotLine(false, false, from, targetFeet + (Vector3.up * Chest));
        }

        /// <summary>Whether a segment is free of solid obstacles; <paramref name="partial"/> tells whether it crosses a Partial Obstacle.</summary>
        public static bool Free(Vector3 a, Vector3 b, out bool partial)
        {
            partial = false;
            var line = b - a;
            var length = line.magnitude;
            if (length < 1e-4f)
            {
                return true;
            }

            var count = Physics.RaycastNonAlloc(a, line / length, Hits, length, Mask, QueryTriggerInteraction.Collide);
            var free = true;
            for (var i = 0; i < count; i++)
            {
                var hit = Hits[i].collider;
                if (IsPartial(hit))
                {
                    partial = true;
                }
                else if (!hit.isTrigger && hit.gameObject.layer != NoGridLayer)
                {
                    free = false;
                }
            }

            return free;
        }

        /// <summary>Whether a person standing at <paramref name="feet"/> stands in a Partial Obstacle.</summary>
        public static bool StandsIn(Vector3 feet)
        {
            var count = Physics.OverlapCapsuleNonAlloc(feet + (Vector3.up * BodyRadius), feet + (Vector3.up * (Chest + 0.1f)), BodyRadius, Near,
                1 << NoGridLayer, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
            {
                if (IsPartial(Near[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsPartial(Collider collider) => collider != null && collider.TryGetComponent<PartialObstacle>(out _);
    }
}

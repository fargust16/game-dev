using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>Why a divide arm stopped where it did.</summary>
    public enum DivideTermination
    {
        /// <summary>It left the land. The coastline is where Stage 3 stops owning anything.</summary>
        Coastline = 0,

        /// <summary>It left the world rectangle.</summary>
        WorldEdge = 1,

        /// <summary>A third massif became the nearer one. A triple junction of the partition.</summary>
        TripleJunction = 2,

        /// <summary>It stopped without reaching a boundary. A defect, not an outcome.</summary>
        Unresolved = 3,
    }

    /// <summary>
    /// The medial line where ownership passes from one massif to another.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every point on it is equidistant from the two axes, so it is the exact boundary of the
    /// <see cref="MassifOwnership"/> partition rather than a curve drawn near it. That equidistance
    /// is also what keeps it off both axes: the two are disjoint, so the divide runs at half the gap
    /// or more from each and cannot cross either. The guarantee is structural, not a check bolted on
    /// afterwards - <see cref="MinAxisDistanceMeters"/> only reports it.
    /// </para>
    /// <para>
    /// A polyline, not a raster and not an implicit function. Stage 3B has to stop its spurs against
    /// this line and later slices have to run a valley corridor along it, and both want a curve they
    /// can walk.
    /// </para>
    /// </remarks>
    public sealed class InterMassifDivide
    {
        /// <summary>Lower massif index of the pair.</summary>
        public int MassifA { get; }

        /// <summary>Higher massif index of the pair.</summary>
        public int MassifB { get; }

        /// <summary>The medial polyline, ordered from the start termination to the end one.</summary>
        public Vector2[] Points { get; }

        public float LengthMeters { get; }

        public DivideTermination StartTermination { get; }

        public DivideTermination EndTermination { get; }

        /// <summary>Worst <c>|dA - dB|</c> anywhere on the polyline, metres.</summary>
        public float MaxImbalanceMeters { get; }

        /// <summary>Closest the divide comes to either axis, metres. Positive by construction.</summary>
        public float MinAxisDistanceMeters { get; }

        /// <summary>Whether both ends reached a real geographic or structural boundary.</summary>
        public bool TerminatesCleanly =>
            StartTermination != DivideTermination.Unresolved
            && EndTermination != DivideTermination.Unresolved;

        public InterMassifDivide(
            int massifA,
            int massifB,
            Vector2[] points,
            DivideTermination startTermination,
            DivideTermination endTermination,
            float maxImbalanceMeters,
            float minAxisDistanceMeters)
        {
            MassifA = massifA;
            MassifB = massifB;
            Points = points;
            StartTermination = startTermination;
            EndTermination = endTermination;
            MaxImbalanceMeters = maxImbalanceMeters;
            MinAxisDistanceMeters = minAxisDistanceMeters;

            var length = 0f;
            for (var i = 0; i + 1 < points.Length; i++)
            {
                length += (points[i + 1] - points[i]).magnitude;
            }

            LengthMeters = length;
        }

        /// <summary>Whether any two non-adjacent segments of the divide cross.</summary>
        public bool SelfIntersects()
        {
            for (var i = 0; i + 1 < Points.Length; i++)
            {
                for (var j = i + 2; j + 1 < Points.Length; j++)
                {
                    if (Town.Geometry.Geo2.SegmentIntersect(
                            Points[i], Points[i + 1], Points[j], Points[j + 1], out _))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Builds the medial divides between massifs by marching the bisector, without a raster.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The medial set of two curves is the zero level of <c>f = dA - dB</c>. Both distance fields have
    /// a gradient that is known exactly - the unit vector from the nearest point on the curve to the
    /// query - so <c>grad f = uA - uB</c> is available in closed form and the level set can be walked
    /// directly: step along <c>perp(grad f)</c>, then take a Newton step back onto <c>f = 0</c>. The
    /// step is exact rather than a search, so two iterations converge and four are insurance.
    /// </para>
    /// <para>
    /// This is why no Voronoi raster is needed. A raster would fix the divide to a lattice, which the
    /// design forbids for the same reason it forbids tile-dependent geometry, and would resolve the
    /// line no better than its own cell size.
    /// </para>
    /// </remarks>
    public static class InterMassifDivideBuilder
    {
        /// <summary>Smallest gap between two axes that still admits a divide between them, metres.</summary>
        /// <remarks>
        /// Stage 2A already keeps accepted systems <see cref="MacroStructureSettings.MinSystemSeparationMeters"/>
        /// apart, so this only guards a hand-built pair. Below it the two axes are effectively one
        /// curve and there is no medial line to speak of.
        /// </remarks>
        public const float MinAxisGapMeters = 1f;

        public static List<InterMassifDivide> Build(
            IReadOnlyList<MountainMassif> massifs, CoastQuery coast, RidgeSettings settings)
        {
            settings.Clamp();
            var divides = new List<InterMassifDivide>();
            if (massifs == null || massifs.Count < 2)
            {
                return divides;
            }

            for (var a = 0; a < massifs.Count; a++)
            {
                for (var b = a + 1; b < massifs.Count; b++)
                {
                    var divide = BuildPair(massifs, a, b, coast, settings);
                    if (divide != null)
                    {
                        divides.Add(divide);
                    }
                }
            }

            return divides;
        }

        private static InterMassifDivide BuildPair(
            IReadOnlyList<MountainMassif> massifs,
            int a,
            int b,
            CoastQuery coast,
            RidgeSettings settings)
        {
            var axisA = massifs[a].Axis;
            var axisB = massifs[b].Axis;

            if (!TrySeed(axisA, axisB, out var seed, out var gap) || gap < MinAxisGapMeters)
            {
                return null;
            }

            // A divide that starts offshore describes nothing Stage 3 owns. Both axes are certified
            // inland, so their shortest connection is inland too, and this only fires on hand-built
            // geometry.
            if (coast != null && !coast.IsInside(seed.x, seed.y))
            {
                return null;
            }

            Distance(axisA, seed, out var uA);
            var forward = new Vector2(-uA.y, uA.x);
            if (forward.sqrMagnitude < 1e-8f)
            {
                return null;
            }

            forward.Normalize();

            var ahead = March(massifs, a, b, coast, settings, seed, forward, out var endTermination);
            var behind = March(massifs, a, b, coast, settings, seed, -forward, out var startTermination);

            var points = new List<Vector2>(behind.Count + 1 + ahead.Count);
            for (var i = behind.Count - 1; i >= 0; i--)
            {
                points.Add(behind[i]);
            }

            points.Add(seed);
            points.AddRange(ahead);

            if (points.Count < 2)
            {
                return null;
            }

            var worstImbalance = 0f;
            var closestAxis = float.PositiveInfinity;
            for (var i = 0; i < points.Count; i++)
            {
                var dA = Distance(axisA, points[i], out _);
                var dB = Distance(axisB, points[i], out _);
                worstImbalance = Mathf.Max(worstImbalance, Mathf.Abs(dA - dB));
                closestAxis = Mathf.Min(closestAxis, Mathf.Min(dA, dB));
            }

            return new InterMassifDivide(
                a, b, points.ToArray(), startTermination, endTermination, worstImbalance, closestAxis);
        }

        /// <summary>
        /// Walks the level set from the seed in one direction until it reaches a boundary.
        /// </summary>
        private static List<Vector2> March(
            IReadOnlyList<MountainMassif> massifs,
            int a,
            int b,
            CoastQuery coast,
            RidgeSettings settings,
            Vector2 seed,
            Vector2 direction,
            out DivideTermination termination)
        {
            var axisA = massifs[a].Axis;
            var axisB = massifs[b].Axis;

            var arm = new List<Vector2>();
            var q = seed;
            var heading = direction;
            var walked = 0f;
            termination = DivideTermination.Unresolved;

            while (walked < settings.DivideMaxArmMeters)
            {
                var previous = q;
                var next = q + heading * settings.DivideStepMeters;

                for (var k = 0; k < settings.DivideRefineIterations; k++)
                {
                    var dA = Distance(axisA, next, out var uA);
                    var dB = Distance(axisB, next, out var uB);
                    var f = dA - dB;
                    if (Mathf.Abs(f) <= settings.DivideToleranceMeters)
                    {
                        break;
                    }

                    var g = uA - uB;
                    var gg = g.sqrMagnitude;
                    if (gg < 1e-6f)
                    {
                        // The two gradients are parallel, so the level set has no local direction to
                        // be corrected along. Nothing to do but let the tolerance test below reject it.
                        break;
                    }

                    next -= g * (f / gg);
                }

                var finalA = Distance(axisA, next, out var finalUA);
                var finalB = Distance(axisB, next, out var finalUB);
                if (Mathf.Abs(finalA - finalB) > settings.DivideToleranceMeters)
                {
                    termination = DivideTermination.Unresolved;
                    break;
                }

                if (!InsideWorld(next))
                {
                    var edge = ClipTo(previous, next, InsideWorld);
                    arm.Add(edge);
                    termination = DivideTermination.WorldEdge;
                    break;
                }

                if (coast != null && !coast.IsInside(next.x, next.y))
                {
                    var shore = ClipTo(previous, next, p => coast.IsInside(p.x, p.y));
                    arm.Add(shore);
                    termination = DivideTermination.Coastline;
                    break;
                }

                // A third massif closer than the pair means this is no longer their shared boundary.
                // The partition has a triple junction here, which is a real structural boundary and
                // where the arm honestly ends.
                if (NearerMassifExists(massifs, a, b, next, finalA))
                {
                    arm.Add(next);
                    termination = DivideTermination.TripleJunction;
                    break;
                }

                arm.Add(next);
                walked += (next - previous).magnitude;

                var g2 = finalUA - finalUB;
                if (g2.sqrMagnitude < 1e-8f)
                {
                    termination = DivideTermination.Unresolved;
                    break;
                }

                var tangent = new Vector2(-g2.y, g2.x).normalized;

                // perp() has two answers and only one of them continues the walk. Keeping the sign
                // that agrees with the last heading is what makes the arm a curve rather than a
                // stutter back and forth across the seed.
                heading = Vector2.Dot(tangent, heading) < 0f ? -tangent : tangent;
                q = next;
            }

            return arm;
        }

        private static bool NearerMassifExists(
            IReadOnlyList<MountainMassif> massifs, int a, int b, Vector2 p, float pairDistance)
        {
            for (var i = 0; i < massifs.Count; i++)
            {
                if (i == a || i == b)
                {
                    continue;
                }

                if (massifs[i].Axis.DistanceTo(p.x, p.y) < pairDistance)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Midpoint of the shortest connection between two axes: a point on their medial set.</summary>
        private static bool TrySeed(MacroAxis a, MacroAxis b, out Vector2 seed, out float gapMeters)
        {
            seed = Vector2.zero;
            gapMeters = float.PositiveInfinity;

            var bestA = Vector2.zero;
            var bestB = Vector2.zero;
            var found = false;

            // Sampling A's stored polyline is enough: the shortest connection between two polylines
            // has at least one endpoint at a vertex or is normal to a segment, and the axes are
            // sampled every 20 m, which is finer than the metre this seed needs to be right to.
            for (var i = 0; i < a.Points.Length; i++)
            {
                var p = a.Points[i];
                var d = Nearest(b, p, out var onB);
                if (d >= gapMeters)
                {
                    continue;
                }

                gapMeters = d;
                bestA = p;
                bestB = onB;
                found = true;
            }

            for (var i = 0; i < b.Points.Length; i++)
            {
                var p = b.Points[i];
                var d = Nearest(a, p, out var onA);
                if (d >= gapMeters)
                {
                    continue;
                }

                gapMeters = d;
                bestA = onA;
                bestB = p;
                found = true;
            }

            if (!found)
            {
                return false;
            }

            seed = (bestA + bestB) * 0.5f;
            return true;
        }

        /// <summary>Distance to an axis and the unit gradient of that distance at the query point.</summary>
        private static float Distance(MacroAxis axis, Vector2 p, out Vector2 unit)
        {
            var distance = Nearest(axis, p, out var closest);
            var away = p - closest;
            unit = away.sqrMagnitude < 1e-10f ? Vector2.zero : away / away.magnitude;
            return distance;
        }

        private static float Nearest(MacroAxis axis, Vector2 p, out Vector2 closest)
        {
            closest = axis.Points[0];
            var bestSq = float.PositiveInfinity;
            for (var i = 0; i + 1 < axis.Points.Length; i++)
            {
                var q = Town.Geometry.Geo2.ClosestPointOnSegment(p, axis.Points[i], axis.Points[i + 1]);
                var sq = (q - p).sqrMagnitude;
                if (sq >= bestSq)
                {
                    continue;
                }

                bestSq = sq;
                closest = q;
            }

            return Mathf.Sqrt(bestSq);
        }

        private static bool InsideWorld(Vector2 p) =>
            p.x >= 0f && p.x <= WorldScale.WorldExtentXMeters
            && p.y >= 0f && p.y <= WorldScale.WorldExtentZMeters;

        /// <summary>Bisects a step that left a region to find where it crossed the boundary.</summary>
        private static Vector2 ClipTo(Vector2 inside, Vector2 outside, System.Func<Vector2, bool> isInside)
        {
            for (var i = 0; i < 16; i++)
            {
                var mid = (inside + outside) * 0.5f;
                if (isInside(mid))
                {
                    inside = mid;
                }
                else
                {
                    outside = mid;
                }
            }

            return inside;
        }
    }
}

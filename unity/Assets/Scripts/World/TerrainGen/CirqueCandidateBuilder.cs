using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Stage 3E slice E1: enumerates the cirque enclosures the frozen primary structure contains.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Walks structure, never heights.</b> A candidate is found because two adjacent spurs and the
    /// crest between them close a ring, not because the ground inside is low. No height field is read
    /// here - not <c>H3</c>, not the carve, not Stage 2's surface - and no depression is scanned.
    /// </para>
    /// <para>
    /// <b>A candidate is one corridor's own frame.</b> A primary corridor is by construction the ground
    /// between two adjacent spurs over their crest interval, so the enclosure needs no geometry that
    /// does not already exist: the crest run, the two spurs, and the gap between their toes. Every one
    /// of the four runs carries the structure it was read off, because the design's first validity
    /// condition is about what fraction of the perimeter is ridge, and that cannot be measured from a
    /// polygon that has forgotten where its edges came from.
    /// </para>
    /// <para>
    /// <b>Candidates, not basins.</b> Nothing here assigns a floor elevation, an outlet elevation, an
    /// outlet corridor or a verdict. Validation is blocked on a threshold the design never states and
    /// on what the outlet's elevation is measured over; producing a half-validated basin would settle
    /// both by accident.
    /// </para>
    /// <para>
    /// No RNG. Enumeration order is corridor order, so identity is a function of the frozen skeleton.
    /// </para>
    /// </remarks>
    public static class CirqueCandidateBuilder
    {
        public static CirqueCandidateSet Build(RidgeSkeleton primary) =>
            Build(primary, primary.Settings);

        public static CirqueCandidateSet Build(RidgeSkeleton primary, RidgeSettings settings)
        {
            settings.Clamp();
            var sw = Stopwatch.StartNew();

            var candidates = new List<CirqueCandidate>();
            var stats = new CirqueCandidateStats
            {
                Seed = primary.Seed,
                CorridorCount = primary.Valleys.Count,
            };

            for (var i = 0; i < primary.Valleys.Count; i++)
            {
                var corridor = primary.Valleys[i];

                // An outer corridor has an attachment-interval edge where its second spur would be.
                // There is nothing there to enclose against, so it is not a candidate and not a defect.
                if (corridor.IsOuter)
                {
                    stats.SkippedOuter++;
                    continue;
                }

                var candidate = TryBuild(primary, settings, corridor, candidates.Count, stats);
                if (candidate == null)
                {
                    continue;
                }

                candidates.Add(candidate);

                stats.MinAreaSqm = Mathf.Min(stats.MinAreaSqm, candidate.AreaSqm);
                stats.MaxAreaSqm = Mathf.Max(stats.MaxAreaSqm, candidate.AreaSqm);
                stats.MinConvergenceRatio = Mathf.Min(
                    stats.MinConvergenceRatio, candidate.ConvergenceRatio);
                stats.MaxConvergenceRatio = Mathf.Max(
                    stats.MaxConvergenceRatio, candidate.ConvergenceRatio);
                stats.MinRidgePerimeterFraction = Mathf.Min(
                    stats.MinRidgePerimeterFraction, candidate.RidgePerimeterFraction);
                stats.MaxRidgePerimeterFraction = Mathf.Max(
                    stats.MaxRidgePerimeterFraction, candidate.RidgePerimeterFraction);

                if (candidate.ConvergenceRatio < 1f)
                {
                    stats.ConvergentCandidates++;
                }
            }

            stats.CandidateCount = candidates.Count;
            stats.BuildMs = sw.ElapsedMilliseconds;

            return new CirqueCandidateSet(primary.Seed, settings, primary, candidates, stats);
        }

        private static CirqueCandidate TryBuild(
            RidgeSkeleton primary,
            RidgeSettings settings,
            ValleyCorridor corridor,
            int id,
            CirqueCandidateStats stats)
        {
            if (corridor.LowerSpurId < 0 || corridor.LowerSpurId >= primary.Spurs.Count
                || corridor.UpperSpurId < 0 || corridor.UpperSpurId >= primary.Spurs.Count)
            {
                stats.RejectedForMissingSpur++;
                return null;
            }

            var lower = primary.Spurs[corridor.LowerSpurId];
            var upper = primary.Spurs[corridor.UpperSpurId];

            // Both divides have to belong to the same flank of the same massif as the corridor, or the
            // ring would not close on one body.
            if (lower.MassifIndex != corridor.MassifIndex || upper.MassifIndex != corridor.MassifIndex
                || lower.FlankSign != corridor.FlankSign || upper.FlankSign != corridor.FlankSign
                || lower.Points.Length < 2 || upper.Points.Length < 2)
            {
                stats.RejectedForMissingSpur++;
                return null;
            }

            if (corridor.SInterval.y - corridor.SInterval.x <= 1e-6f)
            {
                stats.RejectedForCrestInterval++;
                return null;
            }

            var massif = primary.Ownership.Massifs[corridor.MassifIndex];
            var ring = new List<Vector2>();
            var segments = new BoundarySegment[4];

            // 1 - the crest, from the lower attachment to the upper one. The depth-0 divide.
            var first = ring.Count;
            AppendCrest(ring, massif, settings, corridor.SInterval);
            segments[0] = new BoundarySegment(
                BoundarySource.Crest, corridor.MassifIndex, first, ring.Count - first,
                RunLength(ring, first, ring.Count - first));

            // 2 - the upper spur, crest to toe. Its first point is the crest run's last, so it is not
            // repeated.
            first = ring.Count;
            for (var k = 1; k < upper.Points.Length; k++)
            {
                ring.Add(upper.Points[k]);
            }

            segments[1] = new BoundarySegment(
                BoundarySource.Spur, upper.Id, first, ring.Count - first,
                RunLength(ring, first - 1, ring.Count - first + 1));

            // 3 - the gap between the toes. The one run with no ridge beneath it, and the only place
            // the enclosure is open.
            first = ring.Count;
            AppendGap(ring, upper.Toe, lower.Toe, settings.SpurSampleSpacingMeters);
            segments[2] = new BoundarySegment(
                BoundarySource.Outlet, -1, first, ring.Count - first,
                (lower.Toe - upper.Toe).magnitude);

            // 4 - the lower spur, toe back to the crest. Its toe is the gap run's last point and its
            // attachment is the ring's first, so neither end is repeated.
            first = ring.Count;
            for (var k = lower.Points.Length - 2; k >= 1; k--)
            {
                ring.Add(lower.Points[k]);
            }

            segments[3] = new BoundarySegment(
                BoundarySource.Spur, lower.Id, first, ring.Count - first,
                RunLength(ring, first - 1, ring.Count - first + 1) + (ring.Count > first
                    ? (ring[0] - ring[ring.Count - 1]).magnitude
                    : 0f));

            var boundary = ring.ToArray();
            if (boundary.Length < 4)
            {
                stats.RejectedForArea++;
                return null;
            }

            var area = Mathf.Abs(SignedArea(boundary));
            if (area <= 0f)
            {
                stats.RejectedForArea++;
                return null;
            }

            if (SelfIntersects(boundary))
            {
                stats.RejectedForSelfIntersection++;
                return null;
            }

            if (!InsideDomain(primary, boundary, corridor.MassifIndex))
            {
                stats.RejectedForDomain++;
                return null;
            }

            var perimeter = Perimeter(boundary);
            var ridge = 0f;
            for (var k = 0; k < segments.Length; k++)
            {
                if (segments[k].IsRidge)
                {
                    ridge += segments[k].LengthMeters;
                }
            }

            return new CirqueCandidate(
                id, corridor.MassifIndex, corridor.FlankSign, corridor.Id, lower.Id, upper.Id,
                corridor.SInterval, boundary, segments, lower.Toe, upper.Toe,
                (lower.Toe - upper.Toe).magnitude,
                CrestSpan(massif, settings, corridor.SInterval),
                area, perimeter, perimeter > 1e-3f ? Mathf.Clamp01(ridge / perimeter) : 0f);
        }

        // ---------- boundary construction ----------

        /// <summary>The crest between two attachments, sampled at the skeleton's own spacing.</summary>
        private static void AppendCrest(
            List<Vector2> ring, MountainMassif massif, RidgeSettings settings, Vector2 interval)
        {
            var span = (interval.y - interval.x) * massif.LengthMeters;
            var steps = Mathf.Max(2, Mathf.CeilToInt(span / settings.SpurSampleSpacingMeters));
            for (var k = 0; k <= steps; k++)
            {
                ring.Add(massif.PointAt(Mathf.Lerp(interval.x, interval.y, k / (float)steps)));
            }
        }

        /// <summary>
        /// The open run between the two toes, sampled so the domain test has something to read.
        /// </summary>
        /// <remarks>
        /// Sampled at the same spacing as everything else, and it carries no ridge: this is the gap the
        /// design calls the outlet, not an edge invented to close the shape. The endpoints are the two
        /// spur toes, so both ends have provenance even though the run between them does not.
        /// </remarks>
        private static void AppendGap(List<Vector2> ring, Vector2 from, Vector2 to, float spacing)
        {
            var span = (to - from).magnitude;
            var steps = Mathf.Max(1, Mathf.CeilToInt(span / Mathf.Max(1e-3f, spacing)));
            for (var k = 1; k <= steps; k++)
            {
                ring.Add(Vector2.Lerp(from, to, k / (float)steps));
            }
        }

        private static float CrestSpan(
            MountainMassif massif, RidgeSettings settings, Vector2 interval)
        {
            var span = (interval.y - interval.x) * massif.LengthMeters;
            var steps = Mathf.Max(2, Mathf.CeilToInt(span / settings.SpurSampleSpacingMeters));
            var total = 0f;
            var previous = massif.PointAt(interval.x);
            for (var k = 1; k <= steps; k++)
            {
                var here = massif.PointAt(Mathf.Lerp(interval.x, interval.y, k / (float)steps));
                total += (here - previous).magnitude;
                previous = here;
            }

            return total;
        }

        // ---------- checks ----------

        private static bool InsideDomain(RidgeSkeleton primary, Vector2[] ring, int massifIndex)
        {
            for (var i = 0; i < ring.Length; i++)
            {
                var owner = primary.Ownership.Ownership.At(ring[i].x, ring[i].y);
                if (!owner.IsStructural || owner.MassifIndex != massifIndex)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool SelfIntersects(Vector2[] ring)
        {
            var n = ring.Length;
            for (var i = 0; i < n; i++)
            {
                var a0 = ring[i];
                var a1 = ring[(i + 1) % n];
                for (var j = i + 2; j < n; j++)
                {
                    // The last edge closes onto the first, so they are neighbours and cannot be tested.
                    if (i == 0 && j == n - 1)
                    {
                        continue;
                    }

                    if (Town.Geometry.Geo2.SegmentIntersect(a0, a1, ring[j], ring[(j + 1) % n], out _))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static float SignedArea(Vector2[] ring)
        {
            var total = 0f;
            for (var i = 0; i < ring.Length; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % ring.Length];
                total += a.x * b.y - b.x * a.y;
            }

            return total * 0.5f;
        }

        public static float Perimeter(Vector2[] ring)
        {
            var total = 0f;
            for (var i = 0; i < ring.Length; i++)
            {
                total += (ring[(i + 1) % ring.Length] - ring[i]).magnitude;
            }

            return total;
        }

        private static float RunLength(List<Vector2> ring, int first, int count)
        {
            var total = 0f;
            for (var i = Mathf.Max(1, first + 1); i < first + count && i < ring.Count; i++)
            {
                total += (ring[i] - ring[i - 1]).magnitude;
            }

            return total;
        }
    }
}

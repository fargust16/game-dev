using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Stage 3E slice E2: validates cirque enclosures into declared structural basins.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Structural and declarative.</b> Every condition it enforces is decidable from frozen
    /// structure and the Stage 2 structural elevation. It reads no <c>H3</c>, builds no
    /// <see cref="Stage3Field"/>, carves nothing, creates no <see cref="ValleyCorridor"/>, draws no
    /// random number, and writes nothing back into anything it was handed.
    /// </para>
    /// <para>
    /// <b>What it does not assert.</b> Neither that a hollow exists nor how deep it is. Both were
    /// deferred by owner decision to the stage that owns a carved field, because measured over all 28
    /// enclosures the Stage 2 surface contains no hollow in any of them. A validator that asserted one
    /// anyway would be asserting a number it cannot see.
    /// </para>
    /// <para>
    /// <b>Order matters and is fixed.</b> Candidates are walked in their own order, checks run in one
    /// sequence, and the first failure is the recorded reason. That makes a rejection reason a fact
    /// about the geometry rather than about which check happened to run first on the day.
    /// </para>
    /// </remarks>
    public static class StructuralBasinBuilder
    {
        /// <summary>
        /// The perimeter must be ridge structure over a strict majority of its length.
        /// </summary>
        /// <remarks>
        /// The design says "a stated majority" and never states it; the owner resolved it as a strict
        /// mathematical majority. Strict, so a perimeter exactly half ridge is not a majority ridge.
        /// Not a setting: there is one stated value, and a tuning surface for it would invite it to be
        /// tuned.
        /// </remarks>
        public const float RidgeMajorityFraction = 0.5f;

        public static StructuralBasinSet Build(
            CirqueCandidateSet candidates,
            ValleyTrajectoryNetwork trajectories,
            Stage1Result stage1,
            MassifSettings massifSettings,
            RidgeSettings settings)
        {
            settings.Clamp();
            var sw = Stopwatch.StartNew();

            var skeleton = candidates.Primary;
            var uplift = new MassifUpliftModel(skeleton.Ownership.Massifs, massifSettings);

            var basins = new List<StructuralBasin>();
            var verdicts = new List<BasinVerdict>();
            var stats = new StructuralBasinStats
            {
                Seed = candidates.Seed,
                CandidateCount = candidates.Candidates.Count,
            };

            for (var i = 0; i < candidates.Candidates.Count; i++)
            {
                var candidate = candidates.Candidates[i];
                var rejection = Validate(
                    skeleton, trajectories, stage1, uplift, settings, candidate,
                    out var outletPoint, out var outletElevation, out var outletIndex);

                if (rejection != BasinRejection.None)
                {
                    stats.Rejections[(int)rejection]++;
                    verdicts.Add(new BasinVerdict(candidate.Id, candidate.CorridorId, rejection, -1));
                    continue;
                }

                var spurs = candidate.LowerSpurId <= candidate.UpperSpurId
                    ? new[] { candidate.LowerSpurId, candidate.UpperSpurId }
                    : new[] { candidate.UpperSpurId, candidate.LowerSpurId };

                var basin = new StructuralBasin(
                    basins.Count, candidate.Id, candidate.CorridorId, candidate.MassifIndex,
                    candidate.FlankSign, spurs, candidate.Boundary, candidate.Segments,
                    outletPoint, outletElevation, outletIndex, candidate.RidgePerimeterFraction,
                    candidate.AreaSqm, candidate.PerimeterMeters);

                verdicts.Add(new BasinVerdict(
                    candidate.Id, candidate.CorridorId, BasinRejection.None, basin.Id));
                basins.Add(basin);

                stats.MinOutletElevationMeters = Mathf.Min(
                    stats.MinOutletElevationMeters, outletElevation);
                stats.MaxOutletElevationMeters = Mathf.Max(
                    stats.MaxOutletElevationMeters, outletElevation);
                stats.MinRidgePerimeterFraction = Mathf.Min(
                    stats.MinRidgePerimeterFraction, candidate.RidgePerimeterFraction);
                stats.MaxRidgePerimeterFraction = Mathf.Max(
                    stats.MaxRidgePerimeterFraction, candidate.RidgePerimeterFraction);
            }

            stats.BasinCount = basins.Count;
            stats.BuildMs = sw.ElapsedMilliseconds;

            return new StructuralBasinSet(
                candidates.Seed, settings, candidates, basins, verdicts, stats);
        }

        /// <summary>Every condition, in one fixed order. The first failure is the reason.</summary>
        private static BasinRejection Validate(
            RidgeSkeleton skeleton,
            ValleyTrajectoryNetwork trajectories,
            Stage1Result stage1,
            MassifUpliftModel uplift,
            RidgeSettings settings,
            CirqueCandidate candidate,
            out Vector2 outletPoint,
            out float outletElevation,
            out int outletIndex)
        {
            outletPoint = Vector2.zero;
            outletElevation = 0f;
            outletIndex = -1;

            // 1 - the ring. E1 built it closed and simple; asserted again here so the two can disagree
            // audibly rather than one trusting the other.
            if (candidate.Boundary.Length < 4
                || candidate.AreaSqm <= 0f
                || CirqueCandidateBuilder.SelfIntersects(candidate.Boundary))
            {
                return BasinRejection.Geometry;
            }

            // 2 - structural domain, and its own massif.
            for (var i = 0; i < candidate.Boundary.Length; i++)
            {
                var owner = skeleton.Ownership.Ownership.At(
                    candidate.Boundary[i].x, candidate.Boundary[i].y);
                if (!owner.IsStructural || owner.MassifIndex != candidate.MassifIndex)
                {
                    return BasinRejection.Domain;
                }
            }

            // 3 - exactly one opening. Secondary ridges never appear here: E1 only ever writes Crest,
            // Spur and Outlet provenance, and the enum has no other member.
            var gapRun = -1;
            var gaps = 0;
            for (var k = 0; k < candidate.Segments.Length; k++)
            {
                if (candidate.Segments[k].Source != BoundarySource.Outlet)
                {
                    continue;
                }

                gaps++;
                gapRun = k;
            }

            if (gaps != 1 || candidate.Segments[gapRun].PointCount <= 0)
            {
                return BasinRejection.OutletRun;
            }

            // 4 - the perimeter is ridge over a strict majority.
            if (candidate.RidgePerimeterFraction <= RidgeMajorityFraction)
            {
                return BasinRejection.RidgeMajority;
            }

            // 5 - the outlet: the lowest structural ground on the opening, and only on the opening.
            // Walked in boundary order with the first minimum winning, so an exact tie resolves by
            // position on the ring rather than by a draw.
            var run = candidate.Segments[gapRun];
            var lowest = float.MaxValue;
            for (var k = 0; k < run.PointCount; k++)
            {
                var index = (run.FirstIndex + k) % candidate.Boundary.Length;
                var here = StructuralElevation.At(stage1, uplift, candidate.Boundary[index]);
                if (here >= lowest)
                {
                    continue;
                }

                lowest = here;
                outletIndex = index;
                outletPoint = candidate.Boundary[index];
            }

            if (outletIndex < 0)
            {
                return BasinRejection.OutletRun;
            }

            outletElevation = lowest;

            // 6 - the corridor the enclosure frames. For a cirque it is also the corridor the outlet
            // feeds: the opening runs between the toes of that corridor's own bounding spurs, which is
            // that corridor's mouth. One relationship, so one id.
            if (candidate.CorridorId < 0 || candidate.CorridorId >= trajectories.Trajectories.Count)
            {
                return BasinRejection.CorridorMissing;
            }

            var trajectory = trajectories.Trajectories[candidate.CorridorId];
            if (!trajectory.HasGeometry)
            {
                return BasinRejection.CorridorMissing;
            }

            // 7 - it actually leaves through the opening.
            if (!CrossesRun(trajectory.Points, candidate, run))
            {
                return BasinRejection.OutletNotOnCorridor;
            }

            // 8 - and it continues below the outlet. Downstream is the trajectory's own published
            // direction - "head first, endpoint last" - so the comparison reads the endpoint, not an
            // extreme of an unordered array.
            var end = StructuralElevation.At(stage1, uplift, trajectory.EndPoint);
            if (end >= outletElevation)
            {
                return BasinRejection.CorridorNotDownhill;
            }

            // 9 - no corridor but its own may run through the enclosed ground.
            //
            // Entering, not touching. The crest belongs to both flanks, so every corridor on the far
            // flank has its head on the same crest this enclosure is bounded by and crosses that run on
            // its way down the other side; measured on the canonical set, twelve of twelve such
            // meetings were exactly that. Rejecting on a boundary crossing would therefore reject for a
            // fact about the shared crest rather than for anything about the basin. What matters is
            // whether a foreign corridor is inside it.
            for (var i = 0; i < trajectories.Trajectories.Count; i++)
            {
                if (i == candidate.CorridorId || !trajectories.Trajectories[i].HasGeometry)
                {
                    continue;
                }

                if (AnyPointDeepInside(
                        trajectories.Trajectories[i].Points, candidate.Boundary,
                        settings.SpurSampleSpacingMeters))
                {
                    return BasinRejection.ForeignValleyInside;
                }
            }

            return BasinRejection.None;
        }

        /// <summary>
        /// Whether any sample of a polyline lies inside a ring by more than a sample's margin.
        /// </summary>
        /// <remarks>
        /// The margin is not caution, it is the resolution the structure is defined at. The crest is
        /// shared by both flanks, so a corridor on the far flank has its head exactly on it; the crest
        /// run is a chord-sampled polyline, and a point on the true arc between two chords lands a few
        /// centimetres to one side of it. Measured on the canonical set, every foreign corridor that
        /// tested "inside" did so with a single sample at the crest, which is that and nothing else.
        /// One sample spacing is the same tolerance Stage 3C uses to decide two floors are one floor
        /// and Stage 3D uses to keep a rib clear of a ridge, for the same reason.
        /// </remarks>
        public static bool AnyPointDeepInside(Vector2[] polyline, Vector2[] ring, float marginMeters)
        {
            for (var i = 0; i < polyline.Length; i++)
            {
                if (Inside(ring, polyline[i])
                    && DistanceToRing(ring, polyline[i]) > marginMeters)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Distance from a point to a closed ring, metres.</summary>
        public static float DistanceToRing(Vector2[] ring, Vector2 p)
        {
            var best = float.MaxValue;
            for (var i = 0; i < ring.Length; i++)
            {
                best = Mathf.Min(best, DistanceToSegment(ring[i], ring[(i + 1) % ring.Length], p));
            }

            return best;
        }

        private static float DistanceToSegment(Vector2 a, Vector2 b, Vector2 p)
        {
            var ab = b - a;
            var length = ab.sqrMagnitude;
            if (length < 1e-12f)
            {
                return (p - a).magnitude;
            }

            var u = Mathf.Clamp01(Vector2.Dot(p - a, ab) / length);
            return (p - (a + ab * u)).magnitude;
        }

        /// <summary>Even-odd point in polygon.</summary>
        public static bool Inside(Vector2[] ring, Vector2 p)
        {
            var inside = false;
            for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
            {
                if (ring[i].y > p.y != ring[j].y > p.y
                    && p.x < (ring[j].x - ring[i].x) * (p.y - ring[i].y)
                    / (ring[j].y - ring[i].y) + ring[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        /// <summary>Whether a polyline crosses the opening, over the opening's whole extent.</summary>
        /// <remarks>
        /// The gap's two endpoints are the two spur toes, and E1 bookkeeps the upper one into the spur
        /// run that precedes it, so the run's own points start one step inside the opening. For a
        /// question about whether something passes <em>through</em> the gap, the gap is the full span
        /// between the toes, so the segment before the run's first point is included here.
        /// <para>
        /// This does not touch how the outlet's <em>elevation</em> is read. That is the run and only
        /// the run, as B3 resolved, and the same endpoint asymmetry is recorded against it in the
        /// preflight. Keeping the resolved rule and noting the wrinkle is the honest order; quietly
        /// widening the elevation domain here would be deciding B3 again by implementation.
        /// </para>
        /// </remarks>
        private static bool CrossesRun(
            Vector2[] polyline, CirqueCandidate candidate, BoundarySegment run)
        {
            var n = candidate.Boundary.Length;
            var first = (run.FirstIndex - 1 + n) % n;

            for (var k = 0; k <= run.PointCount - 1; k++)
            {
                var a = candidate.Boundary[(first + k) % n];
                var b = candidate.Boundary[(first + k + 1) % n];

                for (var i = 0; i + 1 < polyline.Length; i++)
                {
                    if (Town.Geometry.Geo2.SegmentIntersect(polyline[i], polyline[i + 1], a, b, out _))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

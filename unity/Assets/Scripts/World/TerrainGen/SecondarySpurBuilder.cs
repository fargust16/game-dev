using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Stage 3D: subdivides each frozen primary spur's strip one level down.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Skeleton only.</b> It reads the frozen depth-1 skeleton and the finished primary
    /// trajectories, and writes a <see cref="SecondaryRidgeSkeleton"/>. It carves nothing, touches no
    /// height field, and creates no <see cref="ValleyCorridor"/>: the corridor set is what the primary
    /// valley topology is built on and it is frozen at depth 1.
    /// </para>
    /// <para>
    /// <b>The parent's coordinate, not the world's.</b> A rib attaches at a parent depth
    /// <c>t in (0,1)</c> and the parent's curve is evaluated there. A parent never leaves the flank -
    /// its reach is bounded by <c>FlankHalfWidth · ReliefScale · jitter</c> in Stage 3B - so the
    /// terminal apron is outside a rib's domain by construction and needs no world-space rule to keep
    /// it out.
    /// </para>
    /// <para>
    /// <b>Streams of its own.</b> Both forks are taken per parent spur and side, so each parent draws
    /// from a stream nothing else reads. That is stronger than Stage 3B, which forks once per flank
    /// and therefore has every spur on a flank sharing one cursor in order; here, changing one parent
    /// cannot shift another's ribs.
    /// </para>
    /// </remarks>
    public static class SecondarySpurBuilder
    {
        /// <summary>Stations and shape draw from separate streams, per parent spur and per side.</summary>
        public const string StationStream = "terrain.ridge.secondary.station";

        public const string ShapeStream = "terrain.ridge.secondary.shape";

        /// <summary>
        /// The largest fraction of its own length a rib may bow sideways.
        /// </summary>
        /// <remarks>
        /// Stage 3B's own drift coefficient one tier down: <c>SpurDriftPerGap / TierRatio</c>. Not a
        /// new constant, and substantially weaker than the parent's by exactly the ratio the tier
        /// table runs on. The bow is at most a quarter of this in departure from the chord, because
        /// the offset grows as the square of the descent.
        /// </remarks>
        public static float BendPerLength(RidgeSettings settings) =>
            settings.SpurDriftPerGap / TierRatio;

        /// <summary>
        /// The relief principles' tier separation: adjacent tiers differ by at least this ratio.
        /// </summary>
        /// <remarks>
        /// Not a tuning constant and not new. It is the ratio the tier table already runs on and the
        /// one Stage 3C derived <c>CorridorIncisionMeters = 18 / 2.5</c> from; naming it here only
        /// makes the depth-2 budget read off the same rule rather than a second opinion.
        /// </remarks>
        public const float TierRatio = 2.5f;

        public static SecondaryRidgeSkeleton Build(
            RidgeSkeleton primary,
            ValleyTrajectoryNetwork trajectories,
            Seed.IWorldSeed worldSeed) =>
            Build(primary, trajectories, primary.Settings, worldSeed);

        public static SecondaryRidgeSkeleton Build(
            RidgeSkeleton primary,
            ValleyTrajectoryNetwork trajectories,
            RidgeSettings settings,
            Seed.IWorldSeed worldSeed)
        {
            settings.Clamp();
            var sw = Stopwatch.StartNew();

            var spurs = new List<SecondarySpur>();
            var strips = new List<SecondaryStrip>();
            var children = new int[primary.Spurs.Count][];
            var stats = new SecondaryRidgeStats
            {
                Seed = primary.Seed,
                PrimarySpurCount = primary.Spurs.Count,
            };

            for (var p = 0; p < primary.Spurs.Count; p++)
            {
                var parent = primary.Spurs[p];
                var massif = primary.Ownership.Massifs[parent.MassifIndex];
                var mine = new List<int>();

                // The depth the parent actually achieved. A truncated spur offers less of itself.
                var realised = parent.Termination == SpurTermination.Toe
                    ? 1f
                    : Mathf.Clamp01(parent.ReachMeters / Mathf.Max(1e-3f, parent.TargetReachMeters));

                // Sides in a fixed order, so the identity space is a function of the world and not of
                // the order anything happens to be iterated in.
                for (var k = 0; k < 2; k++)
                {
                    var side = k == 0 ? -1 : 1;
                    BuildSide(
                        primary, trajectories, settings, worldSeed, massif, parent, side, realised,
                        spurs, strips, mine, stats);
                }

                children[p] = mine.ToArray();
                if (mine.Count == 0)
                {
                    stats.ParentsWithoutChildren++;
                }
            }

            stats.SecondarySpurCount = spurs.Count;
            stats.SecondaryStripCount = strips.Count;
            stats.BuildMs = sw.ElapsedMilliseconds;

            return new SecondaryRidgeSkeleton(
                primary.Seed, settings, primary, spurs, strips, children, stats);
        }

        private static void BuildSide(
            RidgeSkeleton primary,
            ValleyTrajectoryNetwork trajectories,
            RidgeSettings settings,
            Seed.IWorldSeed worldSeed,
            MountainMassif massif,
            SpurCurve parent,
            int side,
            float realised,
            List<SecondarySpur> spurs,
            List<SecondaryStrip> strips,
            List<int> mine,
            SecondaryRidgeStats stats)
        {
            // The valley on this side, and the edge of the parent's own strip. Both are frozen
            // geometry; a rib is bounded by whichever it meets first.
            var valley = ValleyOn(primary, trajectories, parent, side);
            var stripEdge = side > 0 ? parent.StripInterval.y : parent.StripInterval.x;

            var stations = PlaceStations(
                massif, parent, settings, worldSeed, side, realised, stripEdge, valley);
            if (stations.Count == 0)
            {
                return;
            }

            var parentBudget = massif.CrestAt(parent.AttachS) / TierRatio;
            var budget = parentBudget / TierRatio;
            var shape = worldSeed.Fork(ShapeStream, parent.Id, SideKey(side));
            var accepted = new List<SecondarySpur>();

            for (var i = 0; i < stations.Count; i++)
            {
                var t = stations[i];

                // Two draws per rib, in a fixed order: how much of the room it takes, and which way it
                // bows. Both come off this parent and side's own stream, so neither depends on how many
                // ribs any other parent happened to keep.
                var slack = 1f - (float)shape.NextDouble() * settings.SpurReachJitter;
                var bend = NextSigned(shape) * (settings.SpurDriftPerGap / TierRatio);

                var candidate = TryBuildRib(
                    primary, trajectories, settings, massif, parent, side, t, stripEdge, valley,
                    slack, bend, budget, parentBudget, spurs.Count + accepted.Count, stats);

                if (candidate == null)
                {
                    continue;
                }

                if (!Accepts(primary, trajectories, settings, massif, parent, candidate, spurs,
                        accepted, stats, out var clearance))
                {
                    continue;
                }

                accepted.Add(new SecondarySpur(
                    candidate.Id, candidate.ParentSpurId, candidate.MassifIndex, candidate.FlankSign,
                    candidate.Side, candidate.AttachT, candidate.AttachPoint, candidate.ParentInterval,
                    candidate.Points, candidate.TargetReachMeters, candidate.TierTargetReachMeters,
                    candidate.LateralDriftMeters, clearance, candidate.ReliefBudgetMeters,
                    candidate.ParentReliefBudgetMeters, candidate.Termination));
            }

            // The cells come from the ribs that survived, so the partition is exact whatever was
            // dropped. N ribs leave N+1 cells, the same arithmetic the flank partition runs on.
            var lower = 0f;
            for (var i = 0; i <= accepted.Count; i++)
            {
                var upper = i == accepted.Count ? realised : accepted[i].AttachT;
                strips.Add(new SecondaryStrip(
                    parent.Id,
                    side,
                    new Vector2(lower, upper),
                    i == 0 ? -1 : accepted[i - 1].Id,
                    i == accepted.Count ? -1 : accepted[i].Id));
                lower = upper;
            }

            for (var i = 0; i < accepted.Count; i++)
            {
                spurs.Add(accepted[i]);
                mine.Add(accepted[i].Id);

                stats.Terminations[(int)accepted[i].Termination]++;
                stats.MinReachMeters = Mathf.Min(stats.MinReachMeters, accepted[i].ReachMeters);
                stats.MaxReachMeters = Mathf.Max(stats.MaxReachMeters, accepted[i].ReachMeters);
                stats.MinReliefBudgetMeters = Mathf.Min(
                    stats.MinReliefBudgetMeters, accepted[i].ReliefBudgetMeters);
                stats.MaxReliefBudgetMeters = Mathf.Max(
                    stats.MaxReliefBudgetMeters, accepted[i].ReliefBudgetMeters);
                stats.MinChordDeviationMeters = Mathf.Min(
                    stats.MinChordDeviationMeters, accepted[i].ChordDeviationMeters);
                stats.MaxChordDeviationMeters = Mathf.Max(
                    stats.MaxChordDeviationMeters, accepted[i].ChordDeviationMeters);
                stats.MinClearanceMeters = Mathf.Min(
                    stats.MinClearanceMeters, accepted[i].ClearanceMeters);
            }
        }

        /// <summary>
        /// Places rib attachments along one side of a parent, in the parent's own depth.
        /// </summary>
        /// <remarks>
        /// The same shape as the flank's own station rule: the domain is walked and the local room
        /// consulted at each step rather than once in the middle, the count comes from the spacing rule
        /// alone, and the draw only nudges a station inside its own gap. Stations stay strictly inside
        /// <c>(0, realised)</c>, so no rib can attach at the crest or at the parent's toe.
        /// </remarks>
        private static List<float> PlaceStations(
            MountainMassif massif,
            SpurCurve parent,
            RidgeSettings settings,
            Seed.IWorldSeed worldSeed,
            int side,
            float realised,
            float stripEdge,
            FinalValleyTrajectory valley)
        {
            var stations = new List<float>();
            if (realised <= 0f || parent.TargetReachMeters <= settings.SpurSampleSpacingMeters)
            {
                return stations;
            }

            var count = 0;
            var walked = 0f;
            while (count < settings.MaxPrimarySpursPerFlank)
            {
                var room = RoomAt(massif, parent, settings, walked, stripEdge, valley);
                var step = settings.SpurSpacingPerHalfWidth * room
                           / Mathf.Max(1f, parent.TargetReachMeters);
                if (step <= 1e-6f)
                {
                    break;
                }

                if (walked + step > realised)
                {
                    if (realised - walked > step * 0.5f)
                    {
                        count++;
                    }

                    break;
                }

                walked += step;
                count++;
            }

            count = Mathf.Clamp(count, 0, settings.MaxPrimarySpursPerFlank);
            if (count == 0)
            {
                return stations;
            }

            var gap = realised / count;
            var rnd = worldSeed.Fork(StationStream, parent.Id, SideKey(side));
            for (var i = 0; i < count; i++)
            {
                var evenly = (i + 0.5f) * gap;
                stations.Add(evenly + NextSigned(rnd) * settings.SpurStationJitter * gap);
            }

            return stations;
        }

        /// <summary>How much room a rib has at a parent depth, metres.</summary>
        /// <remarks>
        /// The lesser of two frozen bounds: the arc to the edge of the parent's own strip, and the
        /// distance to the centre line of the valley on this side. Both are read, never invented.
        /// </remarks>
        private static float RoomAt(
            MountainMassif massif,
            SpurCurve parent,
            RidgeSettings settings,
            float t,
            float stripEdge,
            FinalValleyTrajectory valley)
        {
            var arcRoom = Mathf.Abs(stripEdge - parent.ArcAt(t)) * massif.LengthMeters;
            if (valley == null || !valley.HasGeometry)
            {
                return arcRoom;
            }

            var here = ParentPointAt(massif, parent, settings, t);
            return Mathf.Min(arcRoom, DistanceToPolyline(valley.Points, here));
        }

        private static SecondarySpur TryBuildRib(
            RidgeSkeleton primary,
            ValleyTrajectoryNetwork trajectories,
            RidgeSettings settings,
            MountainMassif massif,
            SpurCurve parent,
            int side,
            float t,
            float stripEdge,
            FinalValleyTrajectory valley,
            float slack,
            float bend,
            float budget,
            float parentBudget,
            int id,
            SecondaryRidgeStats stats)
        {
            var attach = ParentPointAt(massif, parent, settings, t);
            var room = RoomAt(massif, parent, settings, t, stripEdge, valley);

            // A rib stops one sample short of the valley's centre line. That spacing is the resolution
            // the whole skeleton is defined at - the same quantity Stage 3C uses as its merge distance,
            // for the same reason: two structural lines closer than one sample are one line.
            var allowed = room - settings.SpurSampleSpacingMeters;

            // Shorter by the tier ratio, strictly: the jitter no longer multiplies this, so a rib can
            // never be longer than one tier below its parent.
            var tier = parent.TargetReachMeters / TierRatio;

            // Then held to the room, and only then shortened by the draw. Taking the slack after the
            // clamp rather than before it is what stops every room-bound rib landing on the same offset
            // from the valley: the bound stays authoritative, the toes stop sharing one contour.
            var target = Mathf.Min(tier, allowed) * slack;

            if (target <= settings.SpurSampleSpacingMeters)
            {
                stats.RejectedForRoom++;
                return null;
            }

            var termination = tier <= allowed
                ? SecondaryTermination.Toe
                : BindingBound(massif, parent, settings, t, stripEdge, valley);

            var direction = RibDirection(massif, parent, settings, t, side);
            if (direction == Vector2.zero)
            {
                stats.RejectedForRoom++;
                return null;
            }

            // The bow: a lateral offset growing as the square of the descent, so the rib leaves its
            // parent square-on and curves away as it goes. One arc, no inflection - a subordinate
            // ridge, not a winding line. Stage 3B bows its spurs with a smoothstep, which is
            // antisymmetric and therefore barely departs from its own chord; the square is one-sided
            // and shows.
            var drift = bend * target;
            var across = new Vector2(-direction.y, direction.x);

            var steps = Mathf.Max(2, Mathf.CeilToInt(target / settings.SpurSampleSpacingMeters));
            var points = new Vector2[steps + 1];
            for (var k = 0; k <= steps; k++)
            {
                var f = k / (float)steps;
                points[k] = attach + direction * (f * target) + across * (drift * f * f);
            }

            return new SecondarySpur(
                id, parent.Id, parent.MassifIndex, parent.FlankSign, side, t, attach,
                new Vector2(t, t), points, target, tier, drift, float.PositiveInfinity, budget,
                parentBudget, termination);
        }

        /// <summary>Which frozen bound stopped the rib, when the tier reach did not.</summary>
        private static SecondaryTermination BindingBound(
            MountainMassif massif,
            SpurCurve parent,
            RidgeSettings settings,
            float t,
            float stripEdge,
            FinalValleyTrajectory valley)
        {
            var arcRoom = Mathf.Abs(stripEdge - parent.ArcAt(t)) * massif.LengthMeters;
            if (valley == null || !valley.HasGeometry)
            {
                return SecondaryTermination.Strip;
            }

            var here = ParentPointAt(massif, parent, settings, t);
            return DistanceToPolyline(valley.Points, here) < arcRoom
                ? SecondaryTermination.Valley
                : SecondaryTermination.Strip;
        }

        /// <summary>
        /// Every check a rib must pass, in a fixed order, with the reason recorded when it fails.
        /// </summary>
        /// <remarks>
        /// A failing candidate is dropped, once, and counted. There is no retry: a rib that cannot be
        /// built inside its own domain is a finding, and a loop that shrinks it until it fits would
        /// bury that finding in geometry nobody asked for.
        /// </remarks>
        private static bool Accepts(
            RidgeSkeleton primary,
            ValleyTrajectoryNetwork trajectories,
            RidgeSettings settings,
            MountainMassif massif,
            SpurCurve parent,
            SecondarySpur candidate,
            List<SecondarySpur> built,
            List<SecondarySpur> pending,
            SecondaryRidgeStats stats,
            out float measuredClearance)
        {
            measuredClearance = float.PositiveInfinity;
            if (candidate.SelfIntersects())
            {
                stats.RejectedForSelfIntersection++;
                return false;
            }

            // Inside the parent's strip, read through the massif's own frame rather than through the
            // formula the rib was built from, so the two have to agree.
            var tolerance = settings.SpurSampleSpacingMeters / Mathf.Max(1f, massif.LengthMeters);
            for (var i = 0; i < candidate.Points.Length; i++)
            {
                massif.Project(candidate.Points[i].x, candidate.Points[i].y, out var s, out _);
                if (s < parent.StripInterval.x - tolerance || s > parent.StripInterval.y + tolerance)
                {
                    stats.RejectedForLeavingStrip++;
                    return false;
                }
            }

            for (var i = 0; i < candidate.Points.Length; i++)
            {
                var owner = primary.Ownership.Ownership.At(
                    candidate.Points[i].x, candidate.Points[i].y);
                if (!owner.IsStructural || owner.MassifIndex != parent.MassifIndex)
                {
                    stats.RejectedForLeavingDomain++;
                    return false;
                }
            }

            for (var i = 0; i < primary.Spurs.Count; i++)
            {
                // Its own parent is a declared attachment: the rib leaves it, so the segment that
                // carries the attachment is allowed to meet it and no other segment is.
                var skip = primary.Spurs[i].Id == parent.Id ? 1 : 0;
                if (Crosses(candidate.Points, primary.Spurs[i].Points, skip))
                {
                    stats.RejectedForCrossingPrimary++;
                    return false;
                }
            }

            // Clearance from every ridge that is not its own parent. Not crossing is not enough: at the
            // resolution the skeleton is defined at, two structural lines closer than one sample are
            // one line, and a rib running a few metres off a neighbouring spur is that spur.
            var clearance = float.PositiveInfinity;
            for (var i = 0; i < primary.Spurs.Count; i++)
            {
                if (primary.Spurs[i].Id == parent.Id)
                {
                    continue;
                }

                for (var k = 0; k < candidate.Points.Length; k++)
                {
                    clearance = Mathf.Min(
                        clearance,
                        DistanceToPolyline(primary.Spurs[i].Points, candidate.Points[k]));
                }
            }

            if (clearance < settings.SpurSampleSpacingMeters)
            {
                stats.RejectedForClearance++;
                return false;
            }

            measuredClearance = clearance;

            for (var i = 0; i < built.Count; i++)
            {
                if (Crosses(candidate.Points, built[i].Points, 0))
                {
                    stats.RejectedForCrossingSecondary++;
                    return false;
                }
            }

            for (var i = 0; i < pending.Count; i++)
            {
                if (Crosses(candidate.Points, pending[i].Points, 0))
                {
                    stats.RejectedForCrossingSecondary++;
                    return false;
                }
            }

            for (var i = 0; i < trajectories.Trajectories.Count; i++)
            {
                var trajectory = trajectories.Trajectories[i];
                if (!trajectory.HasGeometry)
                {
                    continue;
                }

                if (Crosses(candidate.Points, trajectory.Points, 0))
                {
                    stats.RejectedForCrossingValley++;
                    return false;
                }

                for (var k = 0; k < candidate.Points.Length; k++)
                {
                    if (DistanceToPolyline(trajectory.Points, candidate.Points[k])
                        < settings.SpurSampleSpacingMeters)
                    {
                        stats.RejectedForCrossingValley++;
                        return false;
                    }
                }
            }

            return true;
        }

        // ---------- geometry ----------

        /// <summary>
        /// The parent's own curve at a transverse depth, rebuilt from the parent's published terms.
        /// </summary>
        /// <remarks>
        /// The same expression Stage 3B laid the spur down with - the massif point at
        /// <see cref="SpurCurve.ArcAt"/>, offset along the flank normal by <c>t · TargetReachMeters</c>.
        /// Read rather than re-decided: every term comes off the frozen <see cref="SpurCurve"/>.
        /// </remarks>
        public static Vector2 ParentPointAt(
            MountainMassif massif, SpurCurve parent, RidgeSettings settings, float t)
        {
            var arc = parent.ArcAt(t);
            var here = massif.PointAt(arc);
            var normal = ValleyTrajectoryBuilder.NormalAt(
                massif, arc, settings.SpurSampleSpacingMeters);
            return here + normal * (parent.FlankSign * t * parent.TargetReachMeters);
        }

        /// <summary>The direction a rib leaves its parent in: across the parent, along the crest.</summary>
        private static Vector2 RibDirection(
            MountainMassif massif, SpurCurve parent, RidgeSettings settings, float t, int side)
        {
            const float h = 1e-3f;
            var ahead = ParentPointAt(massif, parent, settings, Mathf.Min(1f, t + h));
            var behind = ParentPointAt(massif, parent, settings, Mathf.Max(0f, t - h));
            var tangent = ahead - behind;
            if (tangent.sqrMagnitude < 1e-12f)
            {
                return Vector2.zero;
            }

            tangent.Normalize();

            var arc = parent.ArcAt(t);
            var ds = settings.SpurSampleSpacingMeters * 0.5f / Mathf.Max(1f, massif.LengthMeters);
            var along = massif.PointAt(Mathf.Min(1f, arc + ds)) - massif.PointAt(Mathf.Max(0f, arc - ds));
            if (along.sqrMagnitude < 1e-12f)
            {
                return Vector2.zero;
            }

            along.Normalize();

            // The part of "up the crest" that is across the parent. A rib runs into the strip, not
            // along the ridge it leaves.
            var across = along - tangent * Vector2.Dot(along, tangent);
            if (across.sqrMagnitude < 1e-8f)
            {
                return Vector2.zero;
            }

            return across.normalized * (side >= 0 ? 1f : -1f);
        }

        /// <summary>The primary valley bounding one side of a parent's strip, or null.</summary>
        private static FinalValleyTrajectory ValleyOn(
            RidgeSkeleton primary,
            ValleyTrajectoryNetwork trajectories,
            SpurCurve parent,
            int side)
        {
            for (var i = 0; i < primary.Valleys.Count; i++)
            {
                var corridor = primary.Valleys[i];
                var match = side > 0 ? corridor.LowerSpurId : corridor.UpperSpurId;
                if (match != parent.Id)
                {
                    continue;
                }

                return corridor.Id < trajectories.Trajectories.Count
                    ? trajectories.Trajectories[corridor.Id]
                    : null;
            }

            return null;
        }

        private static bool Crosses(Vector2[] a, Vector2[] b, int skipFirstOfA)
        {
            for (var i = skipFirstOfA; i + 1 < a.Length; i++)
            {
                for (var j = 0; j + 1 < b.Length; j++)
                {
                    if (Town.Geometry.Geo2.SegmentIntersect(a[i], a[i + 1], b[j], b[j + 1], out _))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static float DistanceToPolyline(Vector2[] polyline, Vector2 p)
        {
            var best = float.MaxValue;
            for (var i = 0; i + 1 < polyline.Length; i++)
            {
                best = Mathf.Min(best, DistanceToSegment(polyline[i], polyline[i + 1], p));
            }

            return polyline.Length == 1 ? (polyline[0] - p).magnitude : best;
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

        private static int SideKey(int side) => side > 0 ? 1 : 0;

        private static float NextSigned(System.Random rnd) => (float)(rnd.NextDouble() * 2.0 - 1.0);
    }
}

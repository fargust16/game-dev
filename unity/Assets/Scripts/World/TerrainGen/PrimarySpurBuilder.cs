using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Builds the primary spurs of one world: the depth-1 ridges descending each massif's flanks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The whole construction happens in the massif's own <c>(s, d)</c> frame. A spur is
    /// <c>s(t) = station + drift(t)</c>, <c>d(t) = sign * t * reach</c>, which is the design's formula
    /// and not a reinterpretation of it. Working in that frame is what makes the topology safe:
    /// stations are spaced along the arc, each spur stays inside its own arc strip, and two curves
    /// that own disjoint arc intervals have nowhere to cross.
    /// </para>
    /// <para>
    /// Two guarantees come from geometry rather than from checking afterwards:
    /// </para>
    /// <list type="bullet">
    /// <item><b>No crossing on a flank.</b> Stations are ordered along the arc, and each spur's drift
    /// is bounded by the room to its neighbours and held clear of where the previous toe landed, so
    /// the toes stay in the same order as the stations. The normals the spurs are offset along cannot
    /// converge within their length either, because the length is bounded by the flank half-width and
    /// the normals converge at the axis radius of curvature, which Stage 2A certified to exceed it.</item>
    /// <item><b>No crossing between flanks or massifs.</b> Opposite flanks are separated by the crest;
    /// different massifs by the medial divide. Both fall out of the ownership query rather than being
    /// tested for.</item>
    /// </list>
    /// <para>
    /// Everything else is enforced by asking Stage 3A. Each sample is resolved through the hardened
    /// contract, and the spur stops at the first point that is not a structural coordinate belonging
    /// to this massif and this flank. Ocean, unclaimed land, ground beyond the reach and ground past
    /// an axis end are therefore unreachable by construction: not because the builder avoids them, but
    /// because it cannot get a frame there and stops.
    /// </para>
    /// <para>
    /// Randomness decides variation and never topology. How many spurs there are, where they attach
    /// and which strips exist come from the spacing rule and the attachment interval; the draws move
    /// a station within its own middle-half, bend the spur inside its own strip, and vary its length
    /// within the headroom Stage 2A guarantees.
    /// </para>
    /// </remarks>
    public static class PrimarySpurBuilder
    {
        /// <summary>Stations and shape draw from separate streams, per massif and per flank.</summary>
        public const string StationStream = "terrain.ridge.spurs";

        public const string ShapeStream = "terrain.ridge.shape";

        /// <summary>Offset below which a sample is on the axis and has no flank to be on.</summary>
        /// <remarks>
        /// The attachment point itself sits on the crest, where the signed offset is zero and the
        /// flank sign is the tie-break rather than a side. Samples inside this are checked for
        /// everything except which flank they are on.
        /// </remarks>
        private const float OnAxisMeters = 1f;

        public static void Build(
            Stage3ASkeleton skeleton,
            RidgeSettings settings,
            Seed.IWorldSeed worldSeed,
            List<SpurCurve> spurs,
            List<ValleyCorridor> valleys,
            RidgeSkeletonStats stats)
        {
            settings.Clamp();

            for (var m = 0; m < skeleton.Massifs.Count; m++)
            {
                for (var f = 0; f < skeleton.Flanks.Count; f++)
                {
                    var flank = skeleton.Flanks[f];
                    if (flank.MassifIndex != m)
                    {
                        continue;
                    }

                    BuildFlank(skeleton, flank, settings, worldSeed, spurs, valleys, stats);
                }
            }
        }

        private static void BuildFlank(
            Stage3ASkeleton skeleton,
            MassifFlank flank,
            RidgeSettings settings,
            Seed.IWorldSeed worldSeed,
            List<SpurCurve> spurs,
            List<ValleyCorridor> valleys,
            RidgeSkeletonStats stats)
        {
            var massif = skeleton.Massifs[flank.MassifIndex];

            // The authoritative interval, forwarded from Stage 2B. Never CertifiedCore: on seed 7 that
            // reaches to 20 m of an axis tip and on 20261231 it starts on one.
            var interval = flank.AttachmentInterval;
            var spanArc = interval.y - interval.x;
            if (spanArc <= 0f)
            {
                return;
            }

            var stations = PlaceStations(massif, interval, settings, worldSeed, flank);
            if (stations.Count == 0)
            {
                return;
            }

            stats.StationsPlaced += stations.Count;

            var shape = worldSeed.Fork(ShapeStream, flank.MassifIndex, FlankKey(flank.Sign));
            var built = new List<SpurCurve>(stations.Count);

            // Where the previous spur's toe landed on the arc, so this one can be kept clear of it.
            var previousToeArc = float.NegativeInfinity;

            for (var i = 0; i < stations.Count; i++)
            {
                // Drawn for every station whether or not the spur survives, so one dropped spur cannot
                // shift the draws of the ones after it.
                var drift = DrawDrift(shape, stations, i, interval, settings, ref previousToeArc);
                var reachScale = 1f + NextSigned(shape) * settings.SpurReachJitter;

                var strip = StripOf(stations, i, interval);
                var spur = TryBuildSpur(
                    skeleton, massif, flank, stations[i], strip, drift, reachScale, settings,
                    spurs.Count + built.Count, stats);

                if (spur != null)
                {
                    built.Add(spur);
                }
                else
                {
                    stats.StationsWithoutASpur++;
                }
            }

            if (built.Count == 0)
            {
                return;
            }

            // Strips are recomputed from the spurs that survived, so the partition and the valley count
            // stay consistent with what the flank actually carries.
            var kept = new List<float>(built.Count);
            for (var i = 0; i < built.Count; i++)
            {
                kept.Add(built[i].AttachS);
            }

            for (var i = 0; i < built.Count; i++)
            {
                var strip = StripOf(kept, i, interval);
                spurs.Add(new SpurCurve(
                    spurs.Count,
                    built[i].MassifIndex,
                    built[i].FlankSign,
                    built[i].AttachS,
                    built[i].DriftArc,
                    strip,
                    built[i].Points,
                    built[i].TargetReachMeters,
                    built[i].Termination));
            }

            AddValleys(spurs, valleys, flank, kept, interval, built.Count);
        }

        /// <summary>
        /// Places attachment stations evenly along the attachment interval, then nudges each.
        /// </summary>
        /// <remarks>
        /// The count comes from the spacing rule and the interval alone; the draw only moves a station
        /// inside the middle half of its own gap, which is what keeps the ordering an arithmetic fact
        /// rather than something to sort afterwards.
        /// </remarks>
        private static List<float> PlaceStations(
            MountainMassif massif,
            Vector2 interval,
            RidgeSettings settings,
            Seed.IWorldSeed worldSeed,
            MassifFlank flank)
        {
            // The spacing rule is a function of arc, not a single number for the flank: the design
            // makes it k * HalfWidthAt(s), so the interval is walked and the local half-width consulted
            // at each step rather than once in the middle.
            var count = 0;
            var walked = interval.x;
            while (count < settings.MaxPrimarySpursPerFlank)
            {
                var step = settings.SpurSpacingPerHalfWidth * massif.HalfWidthAt(walked)
                           / Mathf.Max(1f, massif.LengthMeters);
                if (step <= 1e-6f)
                {
                    break;
                }

                if (walked + step > interval.y)
                {
                    // The remainder counts as another spur when it is more than half a step, which is
                    // the same rounding an even division would do.
                    if (interval.y - walked > step * 0.5f)
                    {
                        count++;
                    }

                    break;
                }

                walked += step;
                count++;
            }

            // The two flanks do not carry the same number. A steeper slope of the same length
            // dissects more finely, so the flank Stage 2B built steep takes one spur more than the
            // gentle one. Two consequences, both wanted: the counts differ, so the two layouts have
            // different gaps and their stations interleave instead of lining up into bars across the
            // crest; and the asymmetry is the one Stage 2B already decided through
            // MountainMassif.SteepSide rather than a second opinion about the same massif.
            if (flank.Sign == massif.SteepSide)
            {
                count++;
            }

            count = Mathf.Clamp(count, 1, settings.MaxPrimarySpursPerFlank);

            var spanArc = interval.y - interval.x;
            var gap = spanArc / count;

            var rnd = worldSeed.Fork(StationStream, flank.MassifIndex, FlankKey(flank.Sign));
            var stations = new List<float>(count);
            for (var i = 0; i < count; i++)
            {
                var evenly = interval.x + (i + 0.5f) * gap;
                stations.Add(evenly + NextSigned(rnd) * settings.SpurStationJitter * gap);
            }

            return stations;
        }

        /// <summary>
        /// Draws a spur's lateral drift: the design's bound, kept from closing a valley at the toe.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The design bounds the drift by "half of the spacing to the neighbouring attachment, which
        /// is what keeps the spur inside its strip", and that is the bound used here - measured to the
        /// actual neighbours rather than to a nominal gap, so a station that jittered towards one
        /// neighbour has correspondingly less room on that side.
        /// </para>
        /// <para>
        /// The bound alone is not enough. Two neighbours drifting towards each other at half a gap
        /// each would put their toes at the same arc position, so the lower end of the draw is also
        /// held clear of where the previous spur's toe landed. That converts "they probably will not
        /// meet" into "they cannot", and it costs the spur nothing whenever its neighbour drifted away.
        /// </para>
        /// </remarks>
        private static float DrawDrift(
            System.Random shape,
            IReadOnlyList<float> stations,
            int i,
            Vector2 interval,
            RidgeSettings settings,
            ref float previousToeArc)
        {
            var station = stations[i];
            var lowerNeighbour = i == 0 ? interval.x : stations[i - 1];
            var upperNeighbour = i == stations.Count - 1 ? interval.y : stations[i + 1];

            var room = settings.SpurDriftPerGap;
            var low = -(station - lowerNeighbour) * room;
            var high = (upperNeighbour - station) * room;

            // Keep this toe clear of the previous one by half the separation of their stations, the
            // same anti-crowding floor the station jitter is bounded by.
            if (!float.IsNegativeInfinity(previousToeArc))
            {
                var clearance = (station - lowerNeighbour) * 0.5f;
                low = Mathf.Max(low, previousToeArc + clearance - station);
            }

            if (low > high)
            {
                low = high;
            }

            var drift = Mathf.Lerp(low, high, (float)shape.NextDouble());
            previousToeArc = station + drift;
            return drift;
        }

        /// <summary>The arc interval a station commands: midpoints to its neighbours.</summary>
        private static Vector2 StripOf(IReadOnlyList<float> stations, int i, Vector2 interval)
        {
            var lower = i == 0
                ? interval.x
                : (stations[i - 1] + stations[i]) * 0.5f;
            var upper = i == stations.Count - 1
                ? interval.y
                : (stations[i] + stations[i + 1]) * 0.5f;

            return new Vector2(lower, upper);
        }

        /// <summary>
        /// Walks one spur down the flank, stopping at the first sample the contract will not resolve.
        /// </summary>
        private static SpurCurve TryBuildSpur(
            Stage3ASkeleton skeleton,
            MountainMassif massif,
            MassifFlank flank,
            float station,
            Vector2 strip,
            float drift,
            float reachScale,
            RidgeSettings settings,
            int id,
            RidgeSkeletonStats stats)
        {
            var target = FlankHalfWidth(massif, station, flank.Sign)
                         * ReliefScale(massif, station, settings)
                         * reachScale;
            if (target <= settings.SpurSampleSpacingMeters)
            {
                return null;
            }

            var steps = Mathf.Max(2, Mathf.CeilToInt(target / settings.SpurSampleSpacingMeters));
            var points = new List<Vector2>(steps + 1);
            var termination = SpurTermination.Toe;

            for (var k = 0; k <= steps; k++)
            {
                var t = k / (float)steps;
                var arc = Mathf.Clamp01(station + drift * Ease(t));
                var here = massif.PointAt(arc);
                var normal = NormalAt(massif, arc, settings.SpurSampleSpacingMeters);
                var p = here + normal * (flank.Sign * t * target);

                if (k == 0)
                {
                    points.Add(p);
                    continue;
                }

                if (!Resolves(skeleton, flank, p, out var reason))
                {
                    termination = reason;
                    break;
                }

                points.Add(p);
            }

            if (points.Count < 2)
            {
                return null;
            }

            stats.Terminations[(int)termination]++;

            var resampled = MacroAxis.Resample(points, settings.SpurSampleSpacingMeters);
            if (resampled.Length < 2)
            {
                return null;
            }

            return new SpurCurve(
                id, flank.MassifIndex, flank.Sign, station, drift, strip, resampled, target,
                termination);
        }

        /// <summary>
        /// Whether a sample is a structural coordinate belonging to this massif and this flank.
        /// </summary>
        /// <remarks>
        /// The single gate. Every geographic rule Stage 3B has to obey - stay out of the ocean, out of
        /// unclaimed land, inside the reach, alongside the axis rather than past its ends, and on this
        /// side of the medial divide - is one of the answers this can return, so none of them needs a
        /// test of its own in the builder.
        /// </remarks>
        private static bool Resolves(
            Stage3ASkeleton skeleton, MassifFlank flank, Vector2 p, out SpurTermination reason)
        {
            var owner = skeleton.Ownership.At(p.x, p.y);

            switch (owner.Kind)
            {
                case MassifOwnerKind.Ocean:
                    reason = SpurTermination.Coast;
                    return false;
                case MassifOwnerKind.Unclaimed:
                    reason = SpurTermination.Unclaimed;
                    return false;
                case MassifOwnerKind.BeyondReach:
                    reason = SpurTermination.Domain;
                    return false;
                case MassifOwnerKind.BeyondAxisEnd:
                    reason = SpurTermination.AxisEnd;
                    return false;
            }

            if (owner.MassifIndex != flank.MassifIndex)
            {
                reason = SpurTermination.Divide;
                return false;
            }

            if (!owner.TryGetFrame(out var frame))
            {
                reason = SpurTermination.Domain;
                return false;
            }

            // Off the axis, the flank has to be the one this spur descends. On it - the first step or
            // two of a spur on a nearly straight stretch - the sign is a tie-break and says nothing.
            if (Mathf.Abs(frame.OffsetMeters) > OnAxisMeters && frame.FlankSign != flank.Sign)
            {
                reason = SpurTermination.Divide;
                return false;
            }

            reason = SpurTermination.Toe;
            return true;
        }

        /// <summary>Valleys are what the spurs left: one more than there are spurs, per flank.</summary>
        private static void AddValleys(
            List<SpurCurve> spurs,
            List<ValleyCorridor> valleys,
            MassifFlank flank,
            IReadOnlyList<float> stations,
            Vector2 interval,
            int builtCount)
        {
            var firstSpur = spurs.Count - builtCount;

            for (var i = 0; i <= stations.Count; i++)
            {
                var lower = i == 0 ? interval.x : stations[i - 1];
                var upper = i == stations.Count ? interval.y : stations[i];

                valleys.Add(new ValleyCorridor(
                    valleys.Count,
                    flank.MassifIndex,
                    flank.Sign,
                    i == 0 ? -1 : firstSpur + i - 1,
                    i == stations.Count ? -1 : firstSpur + i,
                    new Vector2(lower, upper)));
            }
        }

        /// <summary>
        /// How far a spur reaches relative to the flank, from the crest it hangs off.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The revision that gives a straight massif a longitudinal hierarchy. Every variation the
        /// original rule offered was keyed to <see cref="MountainMassif.HalfWidthAt"/>, which tapers
        /// only within <see cref="MassifSettings.EndTaperSpan"/> of an axis end - and the attachment
        /// interval is <see cref="MountainMassif.CoreInterval"/>, held clear of both ends by
        /// <see cref="MassifSettings.MinRampFraction"/>. The two numbers together make the width
        /// profile exactly constant wherever a spur may attach, so spacing and length came out
        /// uniform and a straight axis read as a comb.
        /// </para>
        /// <para>
        /// The signal used instead is Stage 2B's own longitudinal crest profile, which varies from
        /// about a fifth of the peak at the shoulders to the full height at a culmination - a five-fold
        /// range across the same interval where the width profile is flat. It is the right signal
        /// rather than merely an available one: a spur is the remnant between two valleys, the valleys
        /// either side of it are incised in proportion to the local relief, and where that relief is
        /// small the two valleys are shallow and the ridge between them stops being a distinguishable
        /// ridge. Spurs under a culmination therefore run to the toe of the flank; spurs at a shoulder
        /// die well short of it.
        /// </para>
        /// <para>
        /// Deterministic, structural and bounded: it reads a curve Stage 2B already built, adds no
        /// draw, and only shortens - so the domain guarantee the reach bound rests on is strengthened
        /// rather than weakened.
        /// </para>
        /// </remarks>
        private static float ReliefScale(
            MountainMassif massif, float s, RidgeSettings settings)
        {
            var relief = massif.CrestHeightMeters > 1e-3f
                ? Mathf.Clamp01(massif.CrestAt(s) / massif.CrestHeightMeters)
                : 1f;

            return Mathf.Lerp(settings.SpurReliefFloor, 1f, relief);
        }

        /// <summary>
        /// The massif's half-width on one flank at an arc position, metres.
        /// </summary>
        /// <remarks>
        /// Mirrors what <see cref="MountainMassif.Evaluate"/> computes for a point straight off the
        /// flank, where its continuous steep-side weight resolves to one side or the other. Read from
        /// the massif rather than restated: the taper and the asymmetry are both Stage 2B's, and this
        /// only evaluates them at the pure-flank limit.
        /// </remarks>
        private static float FlankHalfWidth(MountainMassif massif, float s, int flankSign)
        {
            var symmetric = massif.HalfWidthAt(s);
            var asymmetry = flankSign == massif.SteepSide
                ? massif.Asymmetry
                : 2f - massif.Asymmetry;
            return symmetric * asymmetry;
        }

        /// <summary>Unit normal of the massif's projection curve, on the +1 flank side.</summary>
        /// <remarks>
        /// The same convention <see cref="MountainMassif.Project"/> signs its offset by: the positive
        /// side is counter-clockwise from the tangent. Defining it any other way here would be the
        /// second reading of the massif frame the Stage 3A contract exists to prevent.
        /// </remarks>
        private static Vector2 NormalAt(MountainMassif massif, float s, float sampleSpacingMeters)
        {
            var ds = Mathf.Max(1e-4f, sampleSpacingMeters * 0.5f / Mathf.Max(1f, massif.LengthMeters));
            var ahead = massif.PointAt(Mathf.Min(1f, s + ds));
            var behind = massif.PointAt(Mathf.Max(0f, s - ds));
            var tangent = ahead - behind;
            if (tangent.sqrMagnitude < 1e-10f)
            {
                return Vector2.up;
            }

            tangent.Normalize();
            return new Vector2(-tangent.y, tangent.x);
        }

        /// <summary>Smoothstep, so the drift starts and ends with no kink.</summary>
        private static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static float NextSigned(System.Random rnd) => (float)(rnd.NextDouble() * 2.0 - 1.0);

        private static int FlankKey(int sign) => sign >= 0 ? 1 : 0;
    }
}

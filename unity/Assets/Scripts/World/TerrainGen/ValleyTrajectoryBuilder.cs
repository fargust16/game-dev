using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>Where a valley trajectory ends, and why.</summary>
    public enum ValleyTermination
    {
        /// <summary>It reached the coastline. The ordinary outcome.</summary>
        Coast = 0,

        /// <summary>It ran into another corridor and joined it.</summary>
        Merge = 1,

        /// <summary>A neighbouring massif's territory began.</summary>
        Divide = 2,

        /// <summary>It stopped without reaching an endpoint. A defect, not an outcome.</summary>
        Unresolved = 3,
    }

    /// <summary>
    /// Builds the floor trajectory of every valley corridor, head to termination.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The object the Stage 3 design specifies and the first Stage 3C implementation did not build.
    /// A corridor is not a strip between two spurs; it is a longitudinal system with a head on the
    /// crest, a floor curve, a continuation past the massif and a real endpoint. Its longitudinal
    /// profile is parameterised by arc length along that curve, which is what stops its support being
    /// a compact bump with closed level sets.
    /// </para>
    /// <para>
    /// Two halves, joined without a seam. Inside the massif the floor is the mid-line between the
    /// corridor's two bounding ridges, evaluated at each transverse depth - so it is derived entirely
    /// from Stage 3B's published spurs and needs no new draw and no Stage 3B change. Past the massif
    /// toe the corridor keeps the direction it left with and descends inside the cone its two bounding
    /// spurs define, choosing freely within that cone by the local slope of <em>Stage 1's own island
    /// surface</em> - Stage 1 rather than the Stage 3 field, so the construction has no self-reference.
    /// </para>
    /// <para>
    /// The cone is what stops the continuation being radial. Stage 1 is a dome, so its steepest
    /// descent points outward from the island centre everywhere; following it directly turned every
    /// corridor onto the same radial direction within a step and produced a starfish. Containing the
    /// walk inside the corridor's own structural cone is a hard constraint rather than a blend, so
    /// there is no weighting between structure and terrain to invent, and two corridors cannot
    /// converge because their bounding spurs are ordered along the crest and so are their cones.
    /// </para>
    /// <para>
    /// Deterministic and seedless. Every input is geometry that already exists.
    /// </para>
    /// </remarks>
    public static class ValleyTrajectoryBuilder
    {
        /// <summary>Two floors closer than this are the same floor and the later one merges.</summary>
        /// <remarks>
        /// The spur sample spacing: the resolution the whole structure is defined at. Resolving two
        /// corridors finer than the curves they hang off would be resolving interpolation.
        /// </remarks>
        public static float MergeDistanceMeters(RidgeSettings settings) =>
            settings.SpurSampleSpacingMeters;

        public static void Build(
            Stage3ASkeleton skeleton,
            IReadOnlyList<SpurCurve> spurs,
            IReadOnlyList<ValleyCorridor> corridors,
            Stage1Result stage1,
            RidgeSettings settings)
        {
            settings.Clamp();
            var built = new List<ValleyCorridor>(corridors.Count);

            for (var i = 0; i < corridors.Count; i++)
            {
                var corridor = corridors[i];
                var massif = skeleton.Massifs[corridor.MassifIndex];

                var floor = new List<Vector2>();
                var arcs = new List<float>();
                BuildMassifPart(massif, corridor, spurs, settings, floor, arcs);

                if (floor.Count < 2)
                {
                    corridor.SetTrajectory(
                        floor.ToArray(), 0, 1f, ValleyTermination.Unresolved, -1);
                    continue;
                }

                var toeIndex = floor.Count - 1;
                var halfWidth = CorridorHalfWidthMeters(massif, corridor, spurs, arcs[toeIndex], 1f);

                var termination = Continue(
                    skeleton, stage1, settings, built, spurs, corridor, floor, halfWidth,
                    out var downstreamId);

                corridor.SetTrajectory(floor.ToArray(), toeIndex, halfWidth, termination, downstreamId);
                built.Add(corridor);
            }
        }

        /// <summary>
        /// The floor inside the massif: the mid-line between the corridor's two bounding ridges.
        /// </summary>
        /// <remarks>
        /// Evaluated at each transverse depth rather than once at the crest, so the floor follows the
        /// ridges as they drift. At <c>t = 0</c> it is exactly <see cref="ValleyCorridor.HeadS"/>,
        /// which is the head Stage 3B already published.
        /// </remarks>
        private static void BuildMassifPart(
            MountainMassif massif,
            ValleyCorridor corridor,
            IReadOnlyList<SpurCurve> spurs,
            RidgeSettings settings,
            List<Vector2> floor,
            List<float> arcs) =>
            BuildCandidateBed(massif, corridor, spurs, settings, floor, arcs);

        /// <summary>
        /// The corridor's bed inside the massif: the mid-line between its two bounding ridges.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Topology-free by construction.</b> Every input is Stage 3B output or Stage 2B geometry -
        /// the two bounding spurs through <see cref="SpurCurve.ArcAt"/>, and the massif's own
        /// <see cref="MountainMassif.PointAt"/>, <see cref="MountainMassif.HalfWidthAt"/>,
        /// <see cref="MountainMassif.Asymmetry"/> and <see cref="MountainMassif.SteepSide"/>. No merge,
        /// no <c>DownstreamId</c> and no termination appears anywhere in it.
        /// </para>
        /// <para>
        /// That property is what makes the network construction acyclic: the convergence test that
        /// decides topology consumes these beds, and these beds consume no topology. Exposed rather
        /// than inlined so the candidate geometry and the carved trajectory are the same curve read
        /// once, not two readings that could drift apart.
        /// </para>
        /// </remarks>
        public static void BuildCandidateBed(
            MountainMassif massif,
            ValleyCorridor corridor,
            IReadOnlyList<SpurCurve> spurs,
            RidgeSettings settings,
            List<Vector2> floor,
            List<float> arcs)
        {
            var steps = Mathf.Max(4, Mathf.CeilToInt(
                massif.HalfWidthMeters * 1.5f / settings.SpurSampleSpacingMeters));

            for (var k = 0; k <= steps; k++)
            {
                var t = k / (float)steps;
                var arc = FloorArcAt(massif, corridor, spurs, t);
                var width = FlankHalfWidth(massif, arc, corridor.FlankSign);
                var here = massif.PointAt(arc);
                var normal = NormalAt(massif, arc, settings.SpurSampleSpacingMeters);
                floor.Add(here + normal * (corridor.FlankSign * t * width));
                arcs.Add(arc);
            }
        }

        /// <summary>Where the corridor's floor sits on the arc, at a transverse depth.</summary>
        /// <remarks>
        /// The mid-line of the two divides that bound the corridor, plus the ground each of them
        /// vacates once it ends. See <see cref="BoundArc"/>.
        /// </remarks>
        public static float FloorArcAt(
            MountainMassif massif,
            ValleyCorridor corridor,
            IReadOnlyList<SpurCurve> spurs,
            float t)
        {
            var lower = BoundArc(
                massif, spurs, corridor.LowerSpurId, corridor.SInterval.x, t, out var lowerVacated);
            var upper = BoundArc(
                massif, spurs, corridor.UpperSpurId, corridor.SInterval.y, t, out var upperVacated);

            var mid = (lower + upper) * 0.5f;

            // Each ended divide releases its ground, and the floor descends into it. Both terms are
            // strictly under one half-gap and point opposite ways, so the floor stays strictly between
            // its two bounds however many of them have ended.
            return mid + lowerVacated * (lower - mid) + upperVacated * (upper - mid);
        }

        /// <summary>
        /// One bounding divide's arc at a transverse depth, and how much of its ground it has released.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A divide stops shaping the valley floor where the divide stops.
        /// <see cref="SpurCurve.ArcAt"/> is a closed form defined over the whole flank, but the spur it
        /// describes ends at <see cref="SpurCurve.ReachMeters"/>, and on the measured worlds six of
        /// seven divides end before the flank foot. Past that depth the ridge is no longer raised
        /// ground, only the line it tapered out along, and the floor descends into what it held.
        /// </para>
        /// <para>
        /// Nothing here is a new number. <c>toe</c> is Slice B2's own
        /// <see cref="SpurToeEvent.ToeFraction"/>, read from the same expression one level earlier, and
        /// the ground released is the square of the descent since the divide ended. That is the plain
        /// kinematic statement: the floor starts responding from rest where the ridge stops, the rate
        /// of response grows with how long the ground has been open, so the displacement goes as the
        /// square. It also carries both properties the endpoint needs on its own - it joins the live
        /// divide without a kink (<c>e(toe) = e'(toe) = 0</c>) and it still has slope
        /// <c>2 * (1 - toe)</c> at the foot, where the smoothstep used elsewhere would have slope zero.
        /// That zero is exactly what flattened every candidate ray onto the flank normal, and a slope
        /// that remembers <c>toe</c> is also what keeps two divides dying at different depths from
        /// cancelling each other out.
        /// </para>
        /// <para>
        /// What the floor moves towards is the dead ridge's own line and never a neighbouring channel,
        /// and it is released at most <c>(1 - toe)^2</c> of the way, so it approaches that line and
        /// cannot reach it. Two neighbours therefore converge without coinciding and without crossing,
        /// and whether they merge at all remains Slice B2's decision.
        /// </para>
        /// </remarks>
        private static float BoundArc(
            MountainMassif massif,
            IReadOnlyList<SpurCurve> spurs,
            int spurId,
            float edge,
            float t,
            out float vacated)
        {
            vacated = 0f;

            // An attachment-interval edge is not a divide, so it never ends and never vacates.
            if (spurId < 0 || spurId >= spurs.Count)
            {
                return edge;
            }

            var spur = spurs[spurId];

            // The same reading Slice B2 orders its events by: the reach the spur achieved, against the
            // flank at its own attachment.
            var flankWidth = FlankHalfWidth(massif, spur.AttachS, spur.FlankSign);
            var toe = flankWidth > 1e-3f ? Mathf.Clamp01(spur.ReachMeters / flankWidth) : 1f;

            if (toe < 1f && t > toe)
            {
                // The ground released grows as the square of the descent since the divide ended, so
                // the floor leaves the live mid-line from rest and the two agree in value and slope.
                var descent = t - toe;
                vacated = descent * descent;
            }

            return spur.ArcAt(t);
        }

        /// <summary>Half the corridor's arc width, in metres, at a transverse depth.</summary>
        public static float CorridorHalfWidthMeters(
            MountainMassif massif,
            ValleyCorridor corridor,
            IReadOnlyList<SpurCurve> spurs,
            float arc,
            float t)
        {
            var lower = corridor.LowerSpurId >= 0
                ? spurs[corridor.LowerSpurId].ArcAt(t)
                : corridor.SInterval.x;
            var upper = corridor.UpperSpurId >= 0
                ? spurs[corridor.UpperSpurId].ArcAt(t)
                : corridor.SInterval.y;

            // Arc is normalised; the massif's own length converts it to metres.
            return Mathf.Max(1f, (upper - lower) * 0.5f * massif.LengthMeters);
        }

        /// <summary>
        /// Walks the trough on past the massif, down Stage 1's own slope, to a real endpoint.
        /// </summary>
        private static ValleyTermination Continue(
            Stage3ASkeleton skeleton,
            Stage1Result stage1,
            RidgeSettings settings,
            IReadOnlyList<ValleyCorridor> built,
            IReadOnlyList<SpurCurve> spurs,
            ValleyCorridor corridor,
            List<Vector2> floor,
            float halfWidthMeters,
            out int downstreamId)
        {
            downstreamId = -1;

            var step = settings.SpurSampleSpacingMeters;
            var mergeDistance = MergeDistanceMeters(settings);
            var maxLength = WorldScale.WorldExtentXMeters;
            var walked = 0f;

            var p = floor[floor.Count - 1];

            // The direction the valley was already going when it left the massif. Fixed for the whole
            // continuation, so the structural heading persists instead of decaying: clamping every
            // step against this rather than against the previous step is what stops the walk drifting
            // onto the island's radial gradient one bisection at a time.
            var structural = floor.Count >= 2
                ? (floor[floor.Count - 1] - floor[floor.Count - 2]).normalized
                : Vector2.right;

            // The cone the corridor may descend in: between the terminal tangents of the two spurs
            // that bound it. Terrain chooses freely inside it and cannot leave it, so the containment
            // is structural and needs no weighting between "structural" and "downhill" - there is no
            // blend to weight. It also keeps two corridors apart, because their bounding spurs are
            // ordered along the crest and so are their cones.
            Cone(corridor, spurs, structural, out var coneLow, out var coneHigh);

            while (walked < maxLength)
            {
                var gradient = Gradient(stage1, p);
                var downhill = gradient.sqrMagnitude > 1e-8f ? (-gradient).normalized : structural;

                var offered = Vector2.SignedAngle(structural, downhill);
                var chosen = Mathf.Clamp(offered, coneLow, coneHigh);
                var direction = Rotate(structural, chosen);

                var next = p + direction * step;

                if (!InsideWorld(next))
                {
                    return ValleyTermination.Coast;
                }

                if (stage1.Coast.SignedDistance(next.x, next.y) <= 0f)
                {
                    floor.Add(next);
                    return ValleyTermination.Coast;
                }

                var owner = skeleton.Ownership.At(next.x, next.y);
                if (owner.IsOwned && owner.MassifIndex != corridor.MassifIndex)
                {
                    floor.Add(next);
                    return ValleyTermination.Divide;
                }

                // A corridor that runs into one already built joins it. Deterministic because the
                // corridors are walked in id order and only earlier ones can be joined.
                for (var i = 0; i < built.Count; i++)
                {
                    if (built[i].DistanceToFloor(next) >= mergeDistance)
                    {
                        continue;
                    }

                    floor.Add(next);
                    downstreamId = built[i].Id;
                    return ValleyTermination.Merge;
                }

                floor.Add(next);
                walked += step;
                p = next;
            }

            return ValleyTermination.Unresolved;
        }

        /// <summary>
        /// The continuation past the massif on its own, with no merge test in it.
        /// </summary>
        /// <remarks>
        /// The same walk <see cref="Build"/> uses, entered with nothing already built, so the
        /// "a corridor that runs into one already built joins it" branch cannot fire. Slice C needs
        /// the descent and not the decision: topology is Slice B2's, settled before any trajectory
        /// exists, and a second geometric merge test here could only contradict it.
        /// </remarks>
        public static ValleyTermination ContinueCandidate(
            Stage3ASkeleton skeleton,
            Stage1Result stage1,
            RidgeSettings settings,
            IReadOnlyList<SpurCurve> spurs,
            ValleyCorridor corridor,
            List<Vector2> floor)
        {
            var termination = Continue(
                skeleton, stage1, settings, System.Array.Empty<ValleyCorridor>(), spurs, corridor,
                floor, 0f, out var downstreamId);

            if (downstreamId >= 0)
            {
                throw new System.InvalidOperationException(
                    "the continuation decided a merge with nothing built to merge into");
            }

            return termination;
        }

        /// <summary>
        /// The angular cone a corridor's continuation may descend in, degrees about its own tangent.
        /// </summary>
        /// <remarks>
        /// Bounded by the terminal tangents of the two spurs that bound the corridor, which is the
        /// same containment that keeps a spur inside its strip applied one level out: past the massif
        /// the corridor keeps running between the two ridges it always ran between. Where one side is
        /// an attachment-interval edge rather than a spur, that side is bounded by the corridor's own
        /// tangent, so an outer corridor runs straight rather than fanning.
        /// </remarks>
        private static void Cone(
            ValleyCorridor corridor,
            IReadOnlyList<SpurCurve> spurs,
            Vector2 structural,
            out float low,
            out float high)
        {
            var lower = TerminalTangent(corridor.LowerSpurId, spurs, structural);
            var upper = TerminalTangent(corridor.UpperSpurId, spurs, structural);

            var a = Vector2.SignedAngle(structural, lower);
            var b = Vector2.SignedAngle(structural, upper);

            low = Mathf.Min(a, b);
            high = Mathf.Max(a, b);

            // The corridor's own tangent has to be inside its own cone; two spurs that both lean the
            // same way would otherwise exclude it.
            low = Mathf.Min(low, 0f);
            high = Mathf.Max(high, 0f);
        }

        private static Vector2 TerminalTangent(
            int spurId, IReadOnlyList<SpurCurve> spurs, Vector2 fallback)
        {
            if (spurId < 0 || spurId >= spurs.Count)
            {
                return fallback;
            }

            var points = spurs[spurId].Points;
            if (points.Length < 2)
            {
                return fallback;
            }

            var tangent = points[points.Length - 1] - points[points.Length - 2];
            return tangent.sqrMagnitude < 1e-10f ? fallback : tangent.normalized;
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            var r = degrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(r);
            var sin = Mathf.Sin(r);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        /// <summary>Gradient of Stage 1's island surface, bilinear on its own lattice.</summary>
        private static Vector2 Gradient(Stage1Result stage1, Vector2 p)
        {
            var field = stage1.Field;
            var cell = field.CellSize;
            var h = cell;

            var dx = Sample(field, p.x + h, p.y) - Sample(field, p.x - h, p.y);
            var dz = Sample(field, p.x, p.y + h) - Sample(field, p.x, p.y - h);
            return new Vector2(dx / (2f * h), dz / (2f * h));
        }

        private static float Sample(WorldHeightField field, float worldX, float worldZ)
        {
            var fx = Mathf.Clamp((worldX - field.OriginX) / field.CellSize, 0f, field.Width - 1.001f);
            var fz = Mathf.Clamp((worldZ - field.OriginZ) / field.CellSize, 0f, field.Height - 1.001f);

            var x0 = Mathf.FloorToInt(fx);
            var z0 = Mathf.FloorToInt(fz);
            var tx = fx - x0;
            var tz = fz - z0;

            var i00 = z0 * field.Width + x0;
            var i10 = i00 + 1;
            var i01 = i00 + field.Width;
            var i11 = i01 + 1;

            return Mathf.Lerp(
                Mathf.Lerp(field.Heights[i00], field.Heights[i10], tx),
                Mathf.Lerp(field.Heights[i01], field.Heights[i11], tx),
                tz);
        }

        private static bool InsideWorld(Vector2 p) =>
            p.x >= 0f && p.x <= WorldScale.WorldExtentXMeters
            && p.y >= 0f && p.y <= WorldScale.WorldExtentZMeters;

        /// <summary>Mirrors what <see cref="MountainMassif.Evaluate"/> uses for a point off one flank.</summary>
        public static float FlankHalfWidth(MountainMassif massif, float s, int flankSign)
        {
            var symmetric = massif.HalfWidthAt(s);
            var asymmetry = flankSign == massif.SteepSide
                ? massif.Asymmetry
                : 2f - massif.Asymmetry;
            return symmetric * asymmetry;
        }

        /// <summary>Unit normal of the massif's projection curve, on the +1 flank side.</summary>
        public static Vector2 NormalAt(MountainMassif massif, float s, float sampleSpacingMeters)
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
    }
}

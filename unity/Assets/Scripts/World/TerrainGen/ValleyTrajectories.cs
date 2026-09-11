using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// One valley's final centre line: its bed, the confluences Slice B2 accepted along it, and
    /// where it ends.
    /// </summary>
    /// <remarks>
    /// A tributary's trajectory <em>stops</em> at the confluence. Everything below that point belongs
    /// to the channel that survived, and appears exactly once, on that channel. This is what makes
    /// the set of trajectories a network rather than a pile of overlapping curves.
    /// </remarks>
    public sealed class FinalValleyTrajectory
    {
        /// <summary>Same identity as the corridor it came from. Slice C invents no new valleys.</summary>
        public int Id { get; }

        public int MassifIndex { get; }

        public int FlankSign { get; }

        /// <summary>Head first, endpoint last, in world XZ.</summary>
        public Vector2[] Points { get; }

        /// <summary>Arc length at each point, metres.</summary>
        public float[] Cumulative { get; }

        /// <summary>The sample at the massif foot, where the bed ends.</summary>
        public int ToeIndex { get; }

        /// <summary>Indices into <see cref="Points"/> where a tributary joins, in downstream order.</summary>
        public int[] ConfluenceIndices { get; }

        /// <summary>Which trajectory joins at each of <see cref="ConfluenceIndices"/>.</summary>
        public int[] TributaryIds { get; }

        /// <summary>The channel this one joins, or -1. Slice B2's answer, copied, never recomputed.</summary>
        public int DownstreamId { get; }

        public ValleyTermination Termination { get; }

        public float HalfWidthMeters { get; }

        public float LengthMeters => Cumulative.Length > 0 ? Cumulative[Cumulative.Length - 1] : 0f;

        /// <summary>Arc length at the massif foot, metres.</summary>
        public float ToeArcMeters =>
            ToeIndex >= 0 && ToeIndex < Cumulative.Length ? Cumulative[ToeIndex] : 0f;

        public bool IsTributary => DownstreamId >= 0;

        public bool HasGeometry => Points.Length >= 2;

        public Vector2 HeadPoint => Points[0];

        public Vector2 EndPoint => Points[Points.Length - 1];

        public FinalValleyTrajectory(
            int id,
            int massifIndex,
            int flankSign,
            Vector2[] points,
            int toeIndex,
            int[] confluenceIndices,
            int[] tributaryIds,
            int downstreamId,
            ValleyTermination termination,
            float halfWidthMeters)
        {
            Id = id;
            MassifIndex = massifIndex;
            FlankSign = flankSign;
            Points = points;
            ToeIndex = toeIndex;
            ConfluenceIndices = confluenceIndices;
            TributaryIds = tributaryIds;
            DownstreamId = downstreamId;
            Termination = termination;
            HalfWidthMeters = halfWidthMeters;

            Cumulative = new float[points.Length];
            for (var i = 1; i < points.Length; i++)
            {
                Cumulative[i] = Cumulative[i - 1] + (points[i] - points[i - 1]).magnitude;
            }
        }

        /// <summary>The direction the trajectory arrives at a sample, or zero if there is none.</summary>
        public Vector2 TangentAt(int index)
        {
            if (index <= 0 || index >= Points.Length)
            {
                return Vector2.zero;
            }

            var d = Points[index] - Points[index - 1];
            return d.sqrMagnitude < 1e-10f ? Vector2.zero : d.normalized;
        }

        /// <summary>The direction the trajectory leaves a sample in, or zero if there is none.</summary>
        public Vector2 OutgoingAt(int index)
        {
            if (index < 0 || index + 1 >= Points.Length)
            {
                return Vector2.zero;
            }

            var d = Points[index + 1] - Points[index];
            return d.sqrMagnitude < 1e-10f ? Vector2.zero : d.normalized;
        }
    }

    /// <summary>
    /// Stage 3C slice C: the final valley trajectories, built from candidate geometry and Slice B2's
    /// topology and from nothing else.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Topology is consumed, never re-decided.</b> Slice B2 settled which channels converge, where,
    /// and which one survives. This slice reads <see cref="ValleyTopology.DownstreamOf"/> and
    /// <see cref="ValleyTopology.ConfluenceOf"/> and lays geometry along them. There is no second
    /// convergence test here and no distance threshold: the continuation is entered through
    /// <see cref="ValleyTrajectoryBuilder.ContinueCandidate"/>, which has the old builder's merge
    /// branch closed off, so the only merges in the result are Slice B2's.
    /// </para>
    /// <para>
    /// <b>Why a straight run to a confluence is the right curve.</b> Slice B2 established a confluence
    /// as the intersection of two candidate rays, and a candidate ray is by construction the terminal
    /// tangent of a candidate bed. So the stretch between a bed's foot and a confluence is the very
    /// geometry the decision was made on: following it reproduces the confluence exactly, joins the
    /// bed with no kink, and puts both tributaries on the point Slice B2 named rather than near it. A
    /// corridor with no confluence has no such stretch and goes straight from its bed into the
    /// continuation, exactly as before.
    /// </para>
    /// <para>
    /// Below the last confluence the surviving channel descends by the approved continuation - inside
    /// the cone its two bounding spurs define, choosing within it by Stage 1's own slope - and ends
    /// at the coast, at a neighbouring massif's territory, or at the world edge.
    /// </para>
    /// </remarks>
    public sealed class ValleyTrajectoryNetwork
    {
        public int Seed { get; }

        public ValleyTopology Topology { get; }

        /// <summary>Indexed by corridor id: one trajectory per corridor, always.</summary>
        public IReadOnlyList<FinalValleyTrajectory> Trajectories { get; }

        /// <summary>The channels nothing flows out of, in ascending id order.</summary>
        public IReadOnlyList<int> Trunks { get; }

        public ValleyTrajectoryStats Stats { get; }

        public ValleyTrajectoryNetwork(
            int seed,
            ValleyTopology topology,
            IReadOnlyList<FinalValleyTrajectory> trajectories,
            IReadOnlyList<int> trunks,
            ValleyTrajectoryStats stats)
        {
            Seed = seed;
            Topology = topology;
            Trajectories = trajectories;
            Trunks = trunks;
            Stats = stats;
        }

        public int RootOf(int id) => Topology.RootOf(id);

        public int DepthOf(int id) => Topology.DepthOf(id);

        // ---------- construction ----------

        /// <summary>A junction on one channel's own ray, and how far down that ray it sits.</summary>
        private readonly struct Junction
        {
            public readonly int TributaryId;
            public readonly float RayParam;

            public Junction(int tributaryId, float rayParam)
            {
                TributaryId = tributaryId;
                RayParam = rayParam;
            }
        }

        public static ValleyTrajectoryNetwork Build(ValleyTopology topology, Stage1Result stage1) =>
            Build(topology, topology.Network.Ridges.Settings, stage1);

        public static ValleyTrajectoryNetwork Build(
            ValleyTopology topology, RidgeSettings settings, Stage1Result stage1)
        {
            settings.Clamp();
            var sw = Stopwatch.StartNew();

            var network = topology.Network;
            var ridges = network.Ridges;
            var skeleton = ridges.Ownership;
            var corridors = ridges.Valleys;
            var count = corridors.Count;

            var stats = new ValleyTrajectoryStats
            {
                Seed = topology.Seed,
                TrajectoryCount = count,
                MergeCount = topology.Stats.MergeCount,
            };

            // Who joins whom, read straight off the topology.
            var tributaries = new List<int>[count];
            for (var i = 0; i < count; i++)
            {
                var downstream = topology.DownstreamOf[i];
                if (downstream < 0)
                {
                    continue;
                }

                if (tributaries[downstream] == null)
                {
                    tributaries[downstream] = new List<int>();
                }

                tributaries[downstream].Add(i);
            }

            var trajectories = new FinalValleyTrajectory[count];
            var step = settings.SpurSampleSpacingMeters;

            for (var i = 0; i < count; i++)
            {
                var corridor = corridors[i];
                var massif = skeleton.Massifs[corridor.MassifIndex];
                var bed = network.CandidateBeds[i];
                var ray = network.CandidateRays[i];

                var points = new List<Vector2>(bed.Length + 32);
                points.AddRange(bed);
                var toeIndex = points.Count - 1;

                var halfWidth = bed.Length >= 2
                    ? ValleyTrajectoryBuilder.CorridorHalfWidthMeters(
                        massif, corridor, ridges.Spurs,
                        ValleyTrajectoryBuilder.FloorArcAt(massif, corridor, ridges.Spurs, 1f), 1f)
                    : 1f;

                if (points.Count < 2 || !ray.IsValid)
                {
                    // Nothing to lay geometry along. Slice B2 will not have merged it either - a
                    // corridor with no candidate ray resolves as NoGeometry there.
                    stats.WithoutGeometry++;
                    trajectories[i] = new FinalValleyTrajectory(
                        corridor.Id, corridor.MassifIndex, corridor.FlankSign, points.ToArray(),
                        toeIndex, System.Array.Empty<int>(), System.Array.Empty<int>(), -1,
                        ValleyTermination.Unresolved, halfWidth);
                    continue;
                }

                // Where this channel's own run ends, if it is itself a tributary.
                var downstreamId = topology.DownstreamOf[i];
                var ownParam = downstreamId >= 0
                    ? Mathf.Max(0f, RayParam(ray, topology.ConfluenceOf[i]))
                    : float.PositiveInfinity;

                var junctions = Order(tributaries[i], topology, ray, ownParam, stats);

                // The shared run: straight down this channel's own ray, through each confluence in
                // turn. That ray is what Slice B2 intersected, so every confluence lands on it exactly.
                var confluenceIndices = new int[junctions.Count];
                var tributaryIds = new int[junctions.Count];
                var walked = 0f;

                for (var k = 0; k < junctions.Count; k++)
                {
                    var confluence = topology.ConfluenceOf[junctions[k].TributaryId];
                    walked = Advance(points, ray, walked, junctions[k].RayParam, confluence, step, stats);
                    confluenceIndices[k] = points.Count - 1;
                    tributaryIds[k] = junctions[k].TributaryId;
                }

                ValleyTermination termination;
                if (downstreamId >= 0)
                {
                    // A tributary ends where it joins. Everything below belongs to the survivor.
                    Advance(points, ray, walked, ownParam, topology.ConfluenceOf[i], step, stats);
                    termination = ValleyTermination.Merge;
                }
                else
                {
                    termination = ValleyTrajectoryBuilder.ContinueCandidate(
                        skeleton, stage1, settings, ridges.Spurs, corridor, points);
                }

                trajectories[i] = new FinalValleyTrajectory(
                    corridor.Id, corridor.MassifIndex, corridor.FlankSign, points.ToArray(), toeIndex,
                    confluenceIndices, tributaryIds, downstreamId, termination, halfWidth);
            }

            var trunks = new List<int>();
            for (var i = 0; i < count; i++)
            {
                if (topology.DownstreamOf[i] < 0)
                {
                    trunks.Add(i);
                }
            }

            Measure(trajectories, topology, stats);
            stats.TrunkCount = trunks.Count;
            stats.BuildMs = sw.ElapsedMilliseconds;

            return new ValleyTrajectoryNetwork(topology.Seed, topology, trajectories, trunks, stats);
        }

        /// <summary>
        /// The tributaries of one channel, in the order they meet it going downstream.
        /// </summary>
        /// <remarks>
        /// Every confluence a channel takes part in lies forward on its own ray - Slice B2 rejects an
        /// intersection behind either origin - so the ray parameter <em>is</em> the downstream order,
        /// and sorting by it settles the ordering without deciding anything. A tributary arriving past
        /// the point where this channel itself ends would be joining water that has already gone: that
        /// cannot come out of Slice B2's own ordering, so it is clamped to the end and counted rather
        /// than quietly accepted.
        /// </remarks>
        private static List<Junction> Order(
            List<int> tributaries,
            ValleyTopology topology,
            CandidateRay ray,
            float ownParam,
            ValleyTrajectoryStats stats)
        {
            var junctions = new List<Junction>();
            if (tributaries == null)
            {
                return junctions;
            }

            for (var k = 0; k < tributaries.Count; k++)
            {
                var param = Mathf.Max(0f, RayParam(ray, topology.ConfluenceOf[tributaries[k]]));
                if (param > ownParam)
                {
                    stats.ConfluencesOutOfOrder++;
                    param = ownParam;
                }

                junctions.Add(new Junction(tributaries[k], param));
            }

            junctions.Sort((a, b) =>
            {
                var c = a.RayParam.CompareTo(b.RayParam);
                return c != 0 ? c : a.TributaryId.CompareTo(b.TributaryId);
            });

            return junctions;
        }

        /// <summary>How far along a ray a point sits, metres.</summary>
        private static float RayParam(CandidateRay ray, Vector2 point) =>
            Vector2.Dot(point - ray.Origin, ray.Direction);

        /// <summary>
        /// Walks the ray from one parameter to the next, landing exactly on the confluence itself.
        /// </summary>
        /// <remarks>
        /// Stepped at the spacing the whole skeleton is sampled at, divided evenly so the endpoint is
        /// a sample rather than an interpolation of two. A span shorter than a step becomes one step,
        /// which keeps a near-immediate confluence from being dropped.
        /// <para>
        /// The last sample is the confluence point itself rather than the ray parameter that projects
        /// onto it. For Slice B2's own confluences the two are the same point - the confluence is that
        /// ray's intersection with another - so this only removes the rounding; where they differ, the
        /// difference is a defect in the geometry feeding the topology and is recorded as one.
        /// </para>
        /// </remarks>
        private static float Advance(
            List<Vector2> points,
            CandidateRay ray,
            float from,
            float to,
            Vector2 confluence,
            float step,
            ValleyTrajectoryStats stats)
        {
            var span = to - from;
            if (span <= 1e-4f)
            {
                stats.MaxConfluenceOffRayMeters = Mathf.Max(
                    stats.MaxConfluenceOffRayMeters,
                    (points[points.Count - 1] - confluence).magnitude);
                points[points.Count - 1] = confluence;
                return from;
            }

            var steps = Mathf.Max(1, Mathf.RoundToInt(span / Mathf.Max(1e-3f, step)));
            for (var k = 1; k < steps; k++)
            {
                points.Add(ray.Origin + ray.Direction * (from + span * (k / (float)steps)));
            }

            stats.MaxConfluenceOffRayMeters = Mathf.Max(
                stats.MaxConfluenceOffRayMeters,
                (ray.Origin + ray.Direction * to - confluence).magnitude);
            points.Add(confluence);
            return to;
        }

        /// <summary>Reads the finished network for the things a join can get wrong.</summary>
        private static void Measure(
            FinalValleyTrajectory[] trajectories, ValleyTopology topology, ValleyTrajectoryStats stats)
        {
            for (var i = 0; i < trajectories.Length; i++)
            {
                var trajectory = trajectories[i];
                stats.Terminations[(int)trajectory.Termination]++;
                stats.ConfluenceCount += trajectory.ConfluenceIndices.Length;
                stats.PointCount += trajectory.Points.Length;
                stats.MaxLengthMeters = Mathf.Max(stats.MaxLengthMeters, trajectory.LengthMeters);
                stats.MaxDepth = Mathf.Max(stats.MaxDepth, topology.DepthOf(i));

                for (var k = 2; k < trajectory.Points.Length; k++)
                {
                    var a = trajectory.Points[k - 1] - trajectory.Points[k - 2];
                    var b = trajectory.Points[k] - trajectory.Points[k - 1];
                    if (a.sqrMagnitude < 1e-10f || b.sqrMagnitude < 1e-10f)
                    {
                        continue;
                    }

                    var turn = Mathf.Abs(Vector2.SignedAngle(a, b));
                    stats.MaxTurnDegrees = Mathf.Max(stats.MaxTurnDegrees, turn);
                    if (turn > 1e-3f)
                    {
                        stats.MinBendRadiusMeters = Mathf.Min(
                            stats.MinBendRadiusMeters, b.magnitude / (turn * Mathf.Deg2Rad));
                    }
                }

                if (!trajectory.IsTributary)
                {
                    continue;
                }

                // C0 at the confluence: the tributary's endpoint against the point the survivor
                // carries for it. Both are the same ray intersection, so this should be zero.
                var survivor = trajectories[trajectory.DownstreamId];
                var index = IndexOfTributary(survivor, i);
                if (index < 0)
                {
                    stats.MissingConfluences++;
                    continue;
                }

                stats.MaxConfluenceGapMeters = Mathf.Max(
                    stats.MaxConfluenceGapMeters,
                    (trajectory.EndPoint - survivor.Points[index]).magnitude);

                var into = trajectory.TangentAt(trajectory.Points.Length - 1);
                var trunk = survivor.TangentAt(index);
                var onward = survivor.OutgoingAt(index);

                if (into != Vector2.zero && trunk != Vector2.zero)
                {
                    stats.MaxConfluenceAngleDegrees = Mathf.Max(
                        stats.MaxConfluenceAngleDegrees, Vector2.Angle(into, trunk));
                }

                if (into != Vector2.zero && onward != Vector2.zero)
                {
                    stats.MaxTributaryTurnDegrees = Mathf.Max(
                        stats.MaxTributaryTurnDegrees, Vector2.Angle(into, onward));
                }
            }
        }

        private static int IndexOfTributary(FinalValleyTrajectory survivor, int tributaryId)
        {
            for (var k = 0; k < survivor.TributaryIds.Length; k++)
            {
                if (survivor.TributaryIds[k] == tributaryId)
                {
                    return survivor.ConfluenceIndices[k];
                }
            }

            return -1;
        }
    }

    public sealed class ValleyTrajectoryStats
    {
        public int Seed;
        public int TrajectoryCount;
        public int TrunkCount;

        /// <summary>Merges Slice B2 accepted. <see cref="ConfluenceCount"/> must equal it.</summary>
        public int MergeCount;

        public int ConfluenceCount;
        public int MaxDepth;
        public int PointCount;

        /// <summary>Corridors with no candidate ray to lay a trajectory along.</summary>
        public int WithoutGeometry;

        /// <summary>A tributary whose confluence the survivor does not carry. Always a defect.</summary>
        public int MissingConfluences;

        /// <summary>A tributary arriving below where its survivor itself ends. Always a defect.</summary>
        public int ConfluencesOutOfOrder;

        public float MaxLengthMeters;
        public float MaxTurnDegrees;
        public float MinBendRadiusMeters = float.PositiveInfinity;

        /// <summary>C0 error at a confluence, metres. Zero by construction.</summary>
        public float MaxConfluenceGapMeters;

        /// <summary>How far a confluence sits off the ray it was supposed to lie on, metres.</summary>
        public float MaxConfluenceOffRayMeters;

        /// <summary>The angle two channels meet at. A confluence, not an error.</summary>
        public float MaxConfluenceAngleDegrees;

        /// <summary>How far a tributary turns to take up the survivor's onward heading.</summary>
        public float MaxTributaryTurnDegrees;

        /// <summary>Indexed by <see cref="ValleyTermination"/>.</summary>
        public readonly int[] Terminations = new int[4];

        public long BuildMs;

        public string ToLine() =>
            $"seed {Seed}: trajectories={TrajectoryCount} trunks={TrunkCount}"
            + $" confluences={ConfluenceCount}/{MergeCount} maxDepth={MaxDepth}"
            + $" points={PointCount} maxLength={MaxLengthMeters:F0}m build={BuildMs}ms";

        public string ContinuityLine() =>
            $"continuity: c0Gap={MaxConfluenceGapMeters:F3}m"
            + $" offRay={MaxConfluenceOffRayMeters:F3}m"
            + $" confluenceAngle={MaxConfluenceAngleDegrees:F1}deg"
            + $" tributaryTurn={MaxTributaryTurnDegrees:F1}deg"
            + $" maxTurn={MaxTurnDegrees:F1}deg"
            + $" minRadius={(MinBendRadiusMeters == float.PositiveInfinity ? -1f : MinBendRadiusMeters):F0}m"
            + $" outOfOrder={ConfluencesOutOfOrder} missing={MissingConfluences}"
            + $" noGeometry={WithoutGeometry}";

        public string TerminationLine() =>
            $"terminations: coast={Terminations[0]} merge={Terminations[1]}"
            + $" divide={Terminations[2]} unresolved={Terminations[3]}";
    }
}

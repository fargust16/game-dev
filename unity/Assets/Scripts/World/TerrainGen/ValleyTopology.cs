using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>What happened at one divide-end event.</summary>
    /// <remarks>
    /// Every outcome other than <see cref="Merged"/> leaves both channels running. None of them is a
    /// failure: a world whose valleys genuinely diverge has no merges, and forcing one would be
    /// inventing convergence the geometry does not have.
    /// </remarks>
    public enum ConvergenceOutcome
    {
        /// <summary>The two candidate rays meet ahead of both, and the meeting point is usable.</summary>
        Merged = 0,

        /// <summary>The rays meet behind one of them: the channels are already separating.</summary>
        Diverged = 1,

        /// <summary>The rays are parallel. They never meet.</summary>
        Parallel = 2,

        /// <summary>The meeting point is upstream of the spur toe, where the divide still stood.</summary>
        UpstreamOfToe = 3,

        /// <summary>The meeting point is offshore.</summary>
        Ocean = 4,

        /// <summary>The meeting point is on another massif's ground, or on nobody's.</summary>
        ForeignOwnership = 5,

        /// <summary>The meeting point is outside the structural domain Stage 3A certifies.</summary>
        OutsideDomain = 6,

        /// <summary>It would land upstream of a confluence already recorded on the same survivor.</summary>
        NonMonotone = 7,

        /// <summary>One of the channels has no usable candidate ray.</summary>
        NoGeometry = 8,

        /// <summary>The two positions already hold the same channel.</summary>
        AlreadyOneChannel = 9,
    }

    /// <summary>One divide-end event, and what the geometry said about it.</summary>
    public readonly struct ToeEventResult
    {
        public int EventIndex { get; }
        public int SpurId { get; }
        public int ChainIndex { get; }
        public int Position { get; }

        /// <summary>The corridors the spur physically separates. Fixed by Stage 3B.</summary>
        public int LowerCorridorId { get; }

        public int UpperCorridorId { get; }

        /// <summary>The channels occupying those two positions when this event was processed.</summary>
        public int LowerActiveId { get; }

        public int UpperActiveId { get; }

        public ConvergenceOutcome Outcome { get; }

        /// <summary>Where the two candidate rays meet. Meaningful only when <see cref="Merged"/>.</summary>
        public Vector2 ConfluencePoint { get; }

        /// <summary>How far that point is from the massif axis, metres. The downstream measure.</summary>
        public float ConfluenceAxisDistanceMeters { get; }

        public int WinnerId { get; }

        public int LoserId { get; }

        public bool Merged => Outcome == ConvergenceOutcome.Merged;

        public ToeEventResult(
            int eventIndex,
            int spurId,
            int chainIndex,
            int position,
            int lowerCorridorId,
            int upperCorridorId,
            int lowerActiveId,
            int upperActiveId,
            ConvergenceOutcome outcome,
            Vector2 confluencePoint,
            float confluenceAxisDistanceMeters,
            int winnerId,
            int loserId)
        {
            EventIndex = eventIndex;
            SpurId = spurId;
            ChainIndex = chainIndex;
            Position = position;
            LowerCorridorId = lowerCorridorId;
            UpperCorridorId = upperCorridorId;
            LowerActiveId = lowerActiveId;
            UpperActiveId = upperActiveId;
            Outcome = outcome;
            ConfluencePoint = confluencePoint;
            ConfluenceAxisDistanceMeters = confluenceAxisDistanceMeters;
            WinnerId = winnerId;
            LoserId = loserId;
        }
    }

    /// <summary>
    /// Stage 3C slice B2: which physically adjacent valley channels actually converge.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The topology is an <b>outcome of geometry</b>. A spur's toe says when a merge becomes possible -
    /// below it the divide that spur carried has ended - and the two channels' candidate rays say
    /// whether they actually meet. Rank never proposes a merge; it only decides which of two genuinely
    /// converging channels keeps its name.
    /// </para>
    /// <para>
    /// Nothing here forces convergence and nothing rewards it. A world whose valleys splay apart on a
    /// convex flank produces no merges at all, and that is a correct answer, not a defect: many short
    /// radial streams is the real drainage of a small mountainous island. The count is reported, never
    /// targeted.
    /// </para>
    /// <para>
    /// <b>This object owns the topology; nothing upstream is mutated.</b> The corridors' own
    /// <c>DownstreamId</c> and <c>Termination</c> belong to the trajectory stage and are left alone, so
    /// slices A and B remain exactly as they were built and their tests keep proving that the geometry
    /// B2 consumes was produced without knowing any of this.
    /// </para>
    /// </remarks>
    public sealed class ValleyTopology
    {
        /// <summary>Two rays whose directions differ by less than this are parallel.</summary>
        /// <remarks>
        /// The cross product of two unit vectors is the sine of the angle between them. This is float
        /// noise, not a geographic threshold: below it the intersection is numerically meaningless
        /// rather than geometrically distant, and a merge computed from it would land anywhere.
        /// </remarks>
        private const float ParallelSine = 1e-4f;

        public int Seed { get; }

        public ValleyNetwork Network { get; }

        /// <summary>One per divide-end event, in processing order.</summary>
        public IReadOnlyList<ToeEventResult> Events { get; }

        /// <summary>The channel each corridor drains into, or -1 where it is a trunk.</summary>
        public IReadOnlyList<int> DownstreamOf { get; }

        /// <summary>Where each corridor joins its winner. Meaningless for a trunk.</summary>
        public IReadOnlyList<Vector2> ConfluenceOf { get; }

        /// <summary>Final occupancy per chain: which channel holds each physical position.</summary>
        public IReadOnlyList<int[]> Occupancy { get; }

        /// <summary>Corridors that never merged.</summary>
        public IReadOnlyList<int> Trunks { get; }

        public ValleyTopologyStats Stats { get; }

        private readonly int[] downstream;

        public ValleyTopology(
            int seed,
            ValleyNetwork network,
            IReadOnlyList<ToeEventResult> events,
            int[] downstream,
            IReadOnlyList<Vector2> confluences,
            IReadOnlyList<int[]> occupancy,
            IReadOnlyList<int> trunks,
            ValleyTopologyStats stats)
        {
            Seed = seed;
            Network = network;
            Events = events;
            DownstreamOf = downstream;
            ConfluenceOf = confluences;
            Occupancy = occupancy;
            Trunks = trunks;
            Stats = stats;
            this.downstream = downstream;
        }

        /// <summary>The trunk a corridor's water ends up in.</summary>
        /// <remarks>
        /// Terminates because every parent pointer goes to a strictly higher fixed rank, so the walk
        /// cannot revisit a corridor. The guard is belt and braces against a future change breaking
        /// that, and it throws rather than looping.
        /// </remarks>
        public int RootOf(int corridorId)
        {
            var guard = downstream.Length + 1;
            var here = corridorId;
            while (downstream[here] >= 0)
            {
                here = downstream[here];
                if (--guard < 0)
                {
                    throw new System.InvalidOperationException(
                        $"corridor {corridorId} is in a downstream cycle");
                }
            }

            return here;
        }

        /// <summary>How many merges separate a corridor from its trunk.</summary>
        public int DepthOf(int corridorId)
        {
            var depth = 0;
            var here = corridorId;
            while (downstream[here] >= 0)
            {
                here = downstream[here];
                depth++;
            }

            return depth;
        }

        // ---------- construction ----------

        public static ValleyTopology Build(ValleyNetwork network) =>
            Build(network, network.Ridges.Settings);

        public static ValleyTopology Build(ValleyNetwork network, RidgeSettings settings)
        {
            settings.Clamp();
            var sw = Stopwatch.StartNew();

            var ridges = network.Ridges;
            var count = network.Ranks.Count;

            var downstream = new int[count];
            var confluences = new Vector2[count];
            var lastConfluenceOn = new float[count];
            for (var i = 0; i < count; i++)
            {
                downstream[i] = -1;
                lastConfluenceOn[i] = float.NegativeInfinity;
            }

            // Occupancy is B2's own state. Slice B's chains keep their initial identity so their tests
            // continue to prove the geometry was built without topology.
            var occupancy = new int[network.Chains.Count][];
            for (var c = 0; c < network.Chains.Count; c++)
            {
                var chain = network.Chains[c];
                occupancy[c] = new int[chain.Length];
                System.Array.Copy(chain.Corridors, occupancy[c], chain.Length);
            }

            var events = new List<ToeEventResult>(network.ToeEvents.Count);
            var stats = new ValleyTopologyStats { Seed = network.Seed, CorridorCount = count };

            for (var e = 0; e < network.ToeEvents.Count; e++)
            {
                var toe = network.ToeEvents[e];
                var chain = network.Chains[toe.ChainIndex];
                var slots = occupancy[toe.ChainIndex];

                var lowerActive = slots[toe.Position];
                var upperActive = slots[toe.Position + 1];

                var result = Resolve(
                    network, ridges, toe, e, lowerActive, upperActive, lastConfluenceOn);

                events.Add(result);
                stats.Outcomes[(int)result.Outcome]++;

                if (!result.Merged)
                {
                    continue;
                }

                downstream[result.LoserId] = result.WinnerId;
                confluences[result.LoserId] = result.ConfluencePoint;
                lastConfluenceOn[result.WinnerId] = result.ConfluenceAxisDistanceMeters;
                stats.MergeCount++;

                // Every position the loser held now holds the winner. That is what makes a later event
                // able to test against a channel that has already absorbed its neighbour.
                for (var i = 0; i < slots.Length; i++)
                {
                    if (slots[i] == result.LoserId)
                    {
                        slots[i] = result.WinnerId;
                    }
                }
            }

            var trunks = new List<int>();
            for (var i = 0; i < count; i++)
            {
                if (downstream[i] < 0)
                {
                    trunks.Add(i);
                }
            }

            stats.TrunkCount = trunks.Count;
            for (var i = 0; i < count; i++)
            {
                var depth = 0;
                var here = i;
                while (downstream[here] >= 0)
                {
                    here = downstream[here];
                    depth++;
                }

                stats.MaxDepth = Mathf.Max(stats.MaxDepth, depth);
            }

            var inflow = new int[count];
            for (var i = 0; i < count; i++)
            {
                if (downstream[i] >= 0)
                {
                    inflow[downstream[i]]++;
                }
            }

            for (var i = 0; i < count; i++)
            {
                stats.MaxMergesIntoOne = Mathf.Max(stats.MaxMergesIntoOne, inflow[i]);
            }

            stats.BuildMs = sw.ElapsedMilliseconds;

            return new ValleyTopology(
                network.Seed, network, events, downstream, confluences, occupancy, trunks, stats);
        }

        /// <summary>Decides one event, entirely from geometry.</summary>
        private static ToeEventResult Resolve(
            ValleyNetwork network,
            RidgeSkeleton ridges,
            SpurToeEvent toe,
            int eventIndex,
            int lowerActive,
            int upperActive,
            float[] lastConfluenceOn)
        {
            // The meeting point is carried into a rejection too, when there is one. A rejected event
            // that had no intersection and one that had a perfectly good intersection a kilometre out
            // to sea are different findings, and only the second says the convergence rule is meeting
            // the land rather than the geometry.
            var found = Vector2.zero;
            var foundDistance = 0f;

            ToeEventResult Reject(ConvergenceOutcome why) => new ToeEventResult(
                eventIndex, toe.SpurId, toe.ChainIndex, toe.Position,
                toe.LowerCorridorId, toe.UpperCorridorId, lowerActive, upperActive,
                why, found, foundDistance, -1, -1);

            if (lowerActive == upperActive)
            {
                return Reject(ConvergenceOutcome.AlreadyOneChannel);
            }

            var a = network.CandidateRays[lowerActive];
            var b = network.CandidateRays[upperActive];
            if (!a.IsValid || !b.IsValid)
            {
                return Reject(ConvergenceOutcome.NoGeometry);
            }

            if (!Intersect(a, b, out var point))
            {
                // Parallel and behind are told apart so the diagnostics can say which, but both mean
                // the same thing on the ground: these two channels are not going to meet.
                var cross = a.Direction.x * b.Direction.y - a.Direction.y * b.Direction.x;
                return Reject(
                    Mathf.Abs(cross) < ParallelSine
                        ? ConvergenceOutcome.Parallel
                        : ConvergenceOutcome.Diverged);
            }

            var massif = ridges.Ownership.Massifs[
                ridges.Valleys[toe.LowerCorridorId].MassifIndex];

            // Downstream is measured as distance from the massif's own axis, which grows monotonically
            // outward across the flank and on past it.
            var axisDistance = massif.Axis.DistanceTo(point.x, point.y);
            found = point;
            foundDistance = axisDistance;
            var toeDistance = massif.Axis.DistanceTo(toe.ToePoint.x, toe.ToePoint.y);
            if (axisDistance < toeDistance)
            {
                return Reject(ConvergenceOutcome.UpstreamOfToe);
            }

            var owner = ridges.Ownership.Ownership.At(point.x, point.y);
            if (owner.Kind == MassifOwnerKind.Ocean)
            {
                return Reject(ConvergenceOutcome.Ocean);
            }

            if (!owner.IsOwned
                || owner.MassifIndex != ridges.Valleys[toe.LowerCorridorId].MassifIndex)
            {
                return Reject(ConvergenceOutcome.ForeignOwnership);
            }

            if (!owner.IsStructural)
            {
                return Reject(ConvergenceOutcome.OutsideDomain);
            }

            var lowerRank = network.Ranks[lowerActive];
            var upperRank = network.Ranks[upperActive];
            var lowerWins = ValleyNetwork.CompareRank(lowerRank, upperRank) < 0;
            var winner = lowerWins ? lowerActive : upperActive;
            var loser = lowerWins ? upperActive : lowerActive;

            // Confluences on one survivor have to march downstream. Events arrive in toe order, which
            // makes this hold in practice, but a survivor that has already absorbed a neighbour further
            // out must not then take one further in.
            if (axisDistance < lastConfluenceOn[winner])
            {
                return Reject(ConvergenceOutcome.NonMonotone);
            }

            return new ToeEventResult(
                eventIndex, toe.SpurId, toe.ChainIndex, toe.Position,
                toe.LowerCorridorId, toe.UpperCorridorId, lowerActive, upperActive,
                ConvergenceOutcome.Merged, point, axisDistance, winner, loser);
        }

        /// <summary>Where two rays meet, if they meet ahead of both.</summary>
        public static bool Intersect(CandidateRay a, CandidateRay b, out Vector2 point)
        {
            point = Vector2.zero;

            var denominator = a.Direction.x * b.Direction.y - a.Direction.y * b.Direction.x;
            if (Mathf.Abs(denominator) < ParallelSine)
            {
                return false;
            }

            var offset = b.Origin - a.Origin;
            var alongA = (offset.x * b.Direction.y - offset.y * b.Direction.x) / denominator;
            var alongB = (offset.x * a.Direction.y - offset.y * a.Direction.x) / denominator;

            if (alongA < 0f || alongB < 0f)
            {
                return false;
            }

            point = a.Origin + a.Direction * alongA;
            return true;
        }
    }

    /// <summary>What slice B2 measured about the topology it resolved.</summary>
    public sealed class ValleyTopologyStats
    {
        public int Seed;
        public int CorridorCount;
        public int MergeCount;
        public int TrunkCount;
        public int MaxDepth;

        /// <summary>Most tributaries joining a single survivor.</summary>
        public int MaxMergesIntoOne;

        /// <summary>Event outcomes, indexed by <see cref="ConvergenceOutcome"/>.</summary>
        public readonly int[] Outcomes = new int[10];

        public long BuildMs;

        public int EventCount
        {
            get
            {
                var total = 0;
                for (var i = 0; i < Outcomes.Length; i++)
                {
                    total += Outcomes[i];
                }

                return total;
            }
        }

        public int RejectedCount => EventCount - MergeCount;

        public string ToLine()
        {
            var culture = CultureInfo.InvariantCulture;
            return $"seed {Seed}: corridors={CorridorCount} events={EventCount}"
                   + $" merges={MergeCount} trunks={TrunkCount} maxDepth={MaxDepth}"
                   + $" maxInflow={MaxMergesIntoOne} rejected={RejectedCount}"
                   + $" build={BuildMs.ToString(culture)}ms";
        }

        public string OutcomeLine() =>
            $"outcomes: merged={Outcomes[(int)ConvergenceOutcome.Merged]}"
            + $" diverged={Outcomes[(int)ConvergenceOutcome.Diverged]}"
            + $" parallel={Outcomes[(int)ConvergenceOutcome.Parallel]}"
            + $" upstreamOfToe={Outcomes[(int)ConvergenceOutcome.UpstreamOfToe]}"
            + $" ocean={Outcomes[(int)ConvergenceOutcome.Ocean]}"
            + $" foreign={Outcomes[(int)ConvergenceOutcome.ForeignOwnership]}"
            + $" outsideDomain={Outcomes[(int)ConvergenceOutcome.OutsideDomain]}"
            + $" nonMonotone={Outcomes[(int)ConvergenceOutcome.NonMonotone]}"
            + $" noGeometry={Outcomes[(int)ConvergenceOutcome.NoGeometry]}"
            + $" alreadyOne={Outcomes[(int)ConvergenceOutcome.AlreadyOneChannel]}";
    }
}

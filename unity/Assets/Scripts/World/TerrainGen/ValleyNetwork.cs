using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>A corridor's fixed, pre-topology rank.</summary>
    /// <remarks>
    /// <para>
    /// <b>A relief-volume proxy, not a catchment.</b> <see cref="FlankFootprintSqm"/> is the rectangle
    /// the corridor's strip occupies on the massif flank, not the drainage area, which is a trapezoid
    /// widening downslope and takes in everything past the massif toe. The name says footprint for that
    /// reason.
    /// </para>
    /// <para>
    /// Computed once, before any merge, and never recomputed. Ranking by an accumulated quantity would
    /// let the order change as merges happen, and an order that changes mid-construction can produce a
    /// cycle - which is why the depth scaling of a later slice uses a separate accumulated value and
    /// this one stays untouched.
    /// </para>
    /// </remarks>
    public readonly struct ValleyRank
    {
        public int CorridorId { get; }

        /// <summary>Arc width times flank length, square metres.</summary>
        public float FlankFootprintSqm { get; }

        /// <summary>Footprint times the crest above the head, cubic metres. The ranking quantity.</summary>
        public float ReliefVolumeCum { get; }

        /// <summary>Crest height above the corridor's head, metres. The first tie-break.</summary>
        public float HeadCrestMeters { get; }

        public ValleyRank(
            int corridorId, float flankFootprintSqm, float reliefVolumeCum, float headCrestMeters)
        {
            CorridorId = corridorId;
            FlankFootprintSqm = flankFootprintSqm;
            ReliefVolumeCum = reliefVolumeCum;
            HeadCrestMeters = headCrestMeters;
        }
    }

    /// <summary>The straight ray a later slice tests convergence with. Never carved.</summary>
    /// <remarks>
    /// Topology-testing geometry only. It begins at the last point of the corridor's candidate bed and
    /// continues along that bed's terminal tangent. It is not a trajectory: it has no end, it is never
    /// clipped to the land, and it must never be promoted into the final valley floor.
    /// </remarks>
    public readonly struct CandidateRay
    {
        public Vector2 Origin { get; }

        public Vector2 Direction { get; }

        public bool IsValid => Direction.sqrMagnitude > 0.5f;

        public CandidateRay(Vector2 origin, Vector2 direction)
        {
            Origin = origin;
            Direction = direction;
        }
    }

    /// <summary>One internal spur, and where its divide stops.</summary>
    /// <remarks>
    /// A spur is the divide between the two corridors either side of it. Below its toe that divide has
    /// ended, which is when a merge <em>may</em> become possible - not when one happens. Ordering these
    /// by toe position is what lets a later slice process divide-ends in the order the ground presents
    /// them, rather than in an order rank invented.
    /// </remarks>
    public readonly struct SpurToeEvent
    {
        public int SpurId { get; }

        /// <summary>Corridor at the lower-arc side of the spur.</summary>
        public int LowerCorridorId { get; }

        /// <summary>Corridor at the higher-arc side.</summary>
        public int UpperCorridorId { get; }

        /// <summary>Chain this spur belongs to.</summary>
        public int ChainIndex { get; }

        /// <summary>Position of the lower corridor in its chain; the spur sits between it and the next.</summary>
        public int Position { get; }

        /// <summary>How far the toe is from the crest, as a fraction of the local flank width.</summary>
        /// <remarks>
        /// Dimensionless, so spurs on different flanks and different massifs compare directly. It can
        /// exceed one: a spur's reach is the flank width times a relief scale and a jitter, and the
        /// jitter can carry it past the massif toe.
        /// </remarks>
        public float ToeFraction { get; }

        public float ToeMeters { get; }

        public Vector2 ToePoint { get; }

        public SpurToeEvent(
            int spurId,
            int lowerCorridorId,
            int upperCorridorId,
            int chainIndex,
            int position,
            float toeFraction,
            float toeMeters,
            Vector2 toePoint)
        {
            SpurId = spurId;
            LowerCorridorId = lowerCorridorId;
            UpperCorridorId = upperCorridorId;
            ChainIndex = chainIndex;
            Position = position;
            ToeFraction = toeFraction;
            ToeMeters = toeMeters;
            ToePoint = toePoint;
        }
    }

    /// <summary>
    /// The corridors of one flank, in physical order along the crest, and who occupies each position.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Adjacency here is physical and permanent.</b> It comes only from corridors sharing a bounding
    /// spur - <c>Corridors[i].UpperSpurId == Corridors[i+1].LowerSpurId</c> - and nothing in the network
    /// construction may change it. It is never inferred from distance, never closed over a corridor
    /// that became a tributary, and never read from a downstream link.
    /// </para>
    /// <para>
    /// <see cref="Occupancy"/> is the separate, mutable thing: which channel currently holds each
    /// physical position. It starts as the identity and a later slice moves entries as merges happen.
    /// Position stays put; occupancy changes.
    /// </para>
    /// </remarks>
    public sealed class ValleyChain
    {
        public int MassifIndex { get; }

        public int FlankSign { get; }

        /// <summary>Corridor ids in ascending arc along the crest.</summary>
        public int[] Corridors { get; }

        /// <summary>Which corridor's channel occupies each position. Identity until a merge happens.</summary>
        public int[] Occupancy { get; }

        /// <summary>Spur between position i and i+1. Length is one less than <see cref="Corridors"/>.</summary>
        public int[] SeparatingSpurs { get; }

        public int Length => Corridors.Length;

        public ValleyChain(int massifIndex, int flankSign, int[] corridors, int[] separatingSpurs)
        {
            MassifIndex = massifIndex;
            FlankSign = flankSign;
            Corridors = corridors;
            SeparatingSpurs = separatingSpurs;

            Occupancy = new int[corridors.Length];
            System.Array.Copy(corridors, Occupancy, corridors.Length);
        }

        /// <summary>Where a corridor sits in this chain, or -1.</summary>
        public int PositionOf(int corridorId)
        {
            for (var i = 0; i < Corridors.Length; i++)
            {
                if (Corridors[i] == corridorId)
                {
                    return i;
                }
            }

            return -1;
        }
    }

    /// <summary>
    /// Stage 3C slices A and B: corridor rank, the physical flank chains, and the candidate geometry
    /// a later slice will test convergence with.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Everything here is <b>topology-free</b>, and that is the point of building it as its own object.
    /// Rank comes from Stage 3B's corridor intervals and Stage 2B's profiles; adjacency from the spur
    /// ids Stage 3B published; the candidate beds from
    /// <see cref="ValleyTrajectoryBuilder.BuildCandidateBed"/>, whose inputs contain no merge; the toe
    /// events from the spurs' own reach. Nothing reads <c>DownstreamId</c> or
    /// <see cref="ValleyTermination"/>, and nothing writes them.
    /// </para>
    /// <para>
    /// That is what makes the apparent dependency cycle - topology needs convergence, convergence needs
    /// candidate geometry, geometry needs topology - not close. The third arrow does not exist.
    /// </para>
    /// <para>
    /// No merge is performed here. No corridor is reclassified, no downstream link is written and no
    /// trajectory is rebuilt. This object is the input a later slice consumes, not a result.
    /// </para>
    /// </remarks>
    public sealed class ValleyNetwork
    {
        public int Seed { get; }

        public RidgeSkeleton Ridges { get; }

        /// <summary>One per massif flank that carries corridors.</summary>
        public IReadOnlyList<ValleyChain> Chains { get; }

        /// <summary>Rank per corridor, indexed by corridor id.</summary>
        public IReadOnlyList<ValleyRank> Ranks { get; }

        /// <summary>Corridor ids, strongest first. See <see cref="CompareRank"/>.</summary>
        public IReadOnlyList<int> RankOrder { get; }

        /// <summary>Candidate bed per corridor, indexed by corridor id.</summary>
        public IReadOnlyList<Vector2[]> CandidateBeds { get; }

        /// <summary>Candidate ray per corridor, indexed by corridor id.</summary>
        public IReadOnlyList<CandidateRay> CandidateRays { get; }

        /// <summary>Internal spurs ordered by how far downstream their divide ends.</summary>
        public IReadOnlyList<SpurToeEvent> ToeEvents { get; }

        public ValleyNetworkStats Stats { get; }

        private readonly int[] lowerNeighbour;
        private readonly int[] upperNeighbour;
        private readonly int[] chainOf;

        public ValleyNetwork(
            int seed,
            RidgeSkeleton ridges,
            IReadOnlyList<ValleyChain> chains,
            IReadOnlyList<ValleyRank> ranks,
            IReadOnlyList<int> rankOrder,
            IReadOnlyList<Vector2[]> candidateBeds,
            IReadOnlyList<CandidateRay> candidateRays,
            IReadOnlyList<SpurToeEvent> toeEvents,
            int[] lowerNeighbour,
            int[] upperNeighbour,
            int[] chainOf,
            ValleyNetworkStats stats)
        {
            Seed = seed;
            Ridges = ridges;
            Chains = chains;
            Ranks = ranks;
            RankOrder = rankOrder;
            CandidateBeds = candidateBeds;
            CandidateRays = candidateRays;
            ToeEvents = toeEvents;
            Stats = stats;

            this.lowerNeighbour = lowerNeighbour;
            this.upperNeighbour = upperNeighbour;
            this.chainOf = chainOf;
        }

        /// <summary>The corridor at lower arc, or -1 where the attachment interval bounds it.</summary>
        public int LowerNeighbourOf(int corridorId) => lowerNeighbour[corridorId];

        /// <summary>The corridor at higher arc, or -1.</summary>
        public int UpperNeighbourOf(int corridorId) => upperNeighbour[corridorId];

        /// <summary>Which chain a corridor belongs to, or -1.</summary>
        public int ChainOf(int corridorId) => chainOf[corridorId];

        /// <summary>Whether two corridors are physical neighbours.</summary>
        /// <remarks>
        /// Only a shared bounding spur makes two corridors adjacent. Proximity does not, a downstream
        /// link does not, and a corridor becoming a tributary does not make its neighbours adjacent to
        /// each other.
        /// </remarks>
        public bool AreAdjacent(int a, int b) =>
            a >= 0 && b >= 0 && (lowerNeighbour[a] == b || upperNeighbour[a] == b);

        /// <summary>Which channel currently occupies a physical position.</summary>
        public int OccupantAt(int chainIndex, int position) => Chains[chainIndex].Occupancy[position];

        /// <summary>
        /// Orders two corridors by rank: strongest first.
        /// </summary>
        /// <remarks>
        /// Relief volume, then the crest above the head, then the id. The last is what makes the order
        /// total: two corridors with identical geometry still sort deterministically, and nothing in
        /// the comparison depends on the order the corridors happen to be stored in.
        /// </remarks>
        public static int CompareRank(ValleyRank a, ValleyRank b)
        {
            var c = b.ReliefVolumeCum.CompareTo(a.ReliefVolumeCum);
            if (c != 0)
            {
                return c;
            }

            c = b.HeadCrestMeters.CompareTo(a.HeadCrestMeters);
            return c != 0 ? c : a.CorridorId.CompareTo(b.CorridorId);
        }

        // ---------- construction ----------

        public static ValleyNetwork Build(RidgeSkeleton ridges) =>
            Build(ridges, ridges.Settings);

        public static ValleyNetwork Build(RidgeSkeleton ridges, RidgeSettings settings)
        {
            settings.Clamp();
            var sw = Stopwatch.StartNew();

            var corridors = ridges.Valleys;
            var count = corridors.Count;

            var ranks = new ValleyRank[count];
            var beds = new Vector2[count][];
            var rays = new CandidateRay[count];
            var lower = new int[count];
            var upper = new int[count];
            var chainOf = new int[count];

            for (var i = 0; i < count; i++)
            {
                lower[i] = -1;
                upper[i] = -1;
                chainOf[i] = -1;
                beds[i] = System.Array.Empty<Vector2>();
            }

            // ---- Slice A: rank ----
            var floorBuffer = new List<Vector2>();
            var arcBuffer = new List<float>();

            for (var i = 0; i < count; i++)
            {
                var corridor = corridors[i];
                var massif = ridges.Ownership.Massifs[corridor.MassifIndex];

                var headCrest = massif.CrestAt(corridor.HeadS);
                var flankWidth = ValleyTrajectoryBuilder.FlankHalfWidth(
                    massif, corridor.HeadS, corridor.FlankSign);

                // SWidth is a normalised arc interval, so multiplying by the massif's length gives
                // metres; times the flank width gives square metres; times the crest gives cubic.
                var footprint = corridor.SWidth * massif.LengthMeters * flankWidth;
                ranks[i] = new ValleyRank(corridor.Id, footprint, footprint * headCrest, headCrest);

                // ---- Slice B: candidate bed and ray ----
                floorBuffer.Clear();
                arcBuffer.Clear();
                ValleyTrajectoryBuilder.BuildCandidateBed(
                    massif, corridor, ridges.Spurs, settings, floorBuffer, arcBuffer);

                beds[i] = floorBuffer.ToArray();
                rays[i] = RayOf(beds[i]);
            }

            var rankOrder = new int[count];
            for (var i = 0; i < count; i++)
            {
                rankOrder[i] = i;
            }

            System.Array.Sort(rankOrder, (a, b) => CompareRank(ranks[a], ranks[b]));

            // ---- Slice A: physical chains ----
            var chains = BuildChains(ridges, lower, upper, chainOf);

            // ---- Slice B: divide-end events ----
            var events = BuildToeEvents(ridges, chains, settings);

            var stats = new ValleyNetworkStats
            {
                Seed = ridges.Seed,
                CorridorCount = count,
                ChainCount = chains.Count,
                AdjacencyCount = events.Count,
                ToeEventCount = events.Count,
                BuildMs = sw.ElapsedMilliseconds,
            };

            for (var i = 0; i < count; i++)
            {
                stats.BedSampleCount += beds[i].Length;
                if (!rays[i].IsValid)
                {
                    stats.InvalidRays++;
                }

                stats.MaxReliefVolumeCum = Mathf.Max(stats.MaxReliefVolumeCum, ranks[i].ReliefVolumeCum);
                stats.MinReliefVolumeCum = Mathf.Min(stats.MinReliefVolumeCum, ranks[i].ReliefVolumeCum);
            }

            return new ValleyNetwork(
                ridges.Seed, ridges, chains, ranks, rankOrder, beds, rays, events,
                lower, upper, chainOf, stats);
        }

        /// <summary>The terminal tangent ray of a candidate bed.</summary>
        private static CandidateRay RayOf(Vector2[] bed)
        {
            if (bed.Length < 2)
            {
                return new CandidateRay(
                    bed.Length == 1 ? bed[0] : Vector2.zero, Vector2.zero);
            }

            var origin = bed[bed.Length - 1];
            var tangent = origin - bed[bed.Length - 2];
            return new CandidateRay(
                origin, tangent.sqrMagnitude < 1e-10f ? Vector2.zero : tangent.normalized);
        }

        /// <summary>
        /// The physical chains, from shared bounding spurs and from nothing else.
        /// </summary>
        private static List<ValleyChain> BuildChains(
            RidgeSkeleton ridges, int[] lower, int[] upper, int[] chainOf)
        {
            var chains = new List<ValleyChain>();
            var corridors = ridges.Valleys;

            for (var m = 0; m < ridges.Ownership.Massifs.Count; m++)
            {
                foreach (var sign in new[] { -1, 1 })
                {
                    var members = new List<ValleyCorridor>();
                    for (var i = 0; i < corridors.Count; i++)
                    {
                        if (corridors[i].MassifIndex == m && corridors[i].FlankSign == sign)
                        {
                            members.Add(corridors[i]);
                        }
                    }

                    if (members.Count == 0)
                    {
                        continue;
                    }

                    // Ascending arc along the crest. Stage 3B already emits them this way; sorting
                    // explicitly means the chain does not depend on that staying true.
                    members.Sort((a, b) =>
                    {
                        var c = a.SInterval.x.CompareTo(b.SInterval.x);
                        return c != 0 ? c : a.Id.CompareTo(b.Id);
                    });

                    var ids = new int[members.Count];
                    var spurs = new int[Mathf.Max(0, members.Count - 1)];

                    for (var i = 0; i < members.Count; i++)
                    {
                        ids[i] = members[i].Id;
                        chainOf[members[i].Id] = chains.Count;

                        if (i + 1 >= members.Count)
                        {
                            continue;
                        }

                        // The only adjacency rule: the spur bounding one corridor above is the spur
                        // bounding the next one below. A pair that does not share one is not adjacent,
                        // whatever their positions in the list.
                        var shared = members[i].UpperSpurId;
                        if (shared < 0 || shared != members[i + 1].LowerSpurId)
                        {
                            spurs[i] = -1;
                            continue;
                        }

                        spurs[i] = shared;
                        upper[members[i].Id] = members[i + 1].Id;
                        lower[members[i + 1].Id] = members[i].Id;
                    }

                    chains.Add(new ValleyChain(m, sign, ids, spurs));
                }
            }

            return chains;
        }

        /// <summary>Divide-end events, ordered by how far downstream each spur's toe sits.</summary>
        private static List<SpurToeEvent> BuildToeEvents(
            RidgeSkeleton ridges, List<ValleyChain> chains, RidgeSettings settings)
        {
            var events = new List<SpurToeEvent>();

            for (var c = 0; c < chains.Count; c++)
            {
                var chain = chains[c];
                var massif = ridges.Ownership.Massifs[chain.MassifIndex];

                for (var i = 0; i < chain.SeparatingSpurs.Length; i++)
                {
                    var spurId = chain.SeparatingSpurs[i];
                    if (spurId < 0)
                    {
                        continue;
                    }

                    var spur = ridges.Spurs[spurId];

                    // The reach the spur actually achieved, not the one it was aimed at: a spur cut
                    // short by the coast or by a neighbouring massif ends its divide earlier, and it is
                    // where the divide ends that matters.
                    var flankWidth = ValleyTrajectoryBuilder.FlankHalfWidth(
                        massif, spur.AttachS, spur.FlankSign);
                    var fraction = flankWidth > 1e-3f ? spur.ReachMeters / flankWidth : 1f;

                    events.Add(new SpurToeEvent(
                        spurId,
                        chain.Corridors[i],
                        chain.Corridors[i + 1],
                        c,
                        i,
                        fraction,
                        spur.ReachMeters,
                        spur.Toe));
                }
            }

            events.Sort((a, b) =>
            {
                var c = a.ToeFraction.CompareTo(b.ToeFraction);
                return c != 0 ? c : a.SpurId.CompareTo(b.SpurId);
            });

            return events;
        }
    }

    /// <summary>What slices A and B measured about themselves.</summary>
    public sealed class ValleyNetworkStats
    {
        public int Seed;
        public int CorridorCount;
        public int ChainCount;

        /// <summary>Physical neighbour pairs, which equals the number of internal spurs.</summary>
        public int AdjacencyCount;

        public int ToeEventCount;
        public int BedSampleCount;

        /// <summary>Candidate beds too short to give a tangent. Expected zero.</summary>
        public int InvalidRays;

        public float MaxReliefVolumeCum;
        public float MinReliefVolumeCum = float.PositiveInfinity;
        public long BuildMs;

        public float ReliefVolumeSpread =>
            MinReliefVolumeCum > 1e-6f && !float.IsPositiveInfinity(MinReliefVolumeCum)
                ? MaxReliefVolumeCum / MinReliefVolumeCum
                : 0f;

        public string ToLine()
        {
            var culture = CultureInfo.InvariantCulture;
            return $"seed {Seed}: corridors={CorridorCount} chains={ChainCount}"
                   + $" adjacency={AdjacencyCount} toeEvents={ToeEventCount}"
                   + $" bedSamples={BedSampleCount} invalidRays={InvalidRays}"
                   + $" reliefVolume=[{MinReliefVolumeCum.ToString("F0", culture)},"
                   + $"{MaxReliefVolumeCum.ToString("F0", culture)}]m3"
                   + $" spread={ReliefVolumeSpread.ToString("F2", culture)}x"
                   + $" build={BuildMs}ms";
        }
    }
}

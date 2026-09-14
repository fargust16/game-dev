using System;
using System.Collections.Generic;

namespace GameDev.World.Hydrology
{
    /// <summary>
    /// Counts and timings for one hydrology plan. Reported once per generation, never used to make
    /// decisions.
    /// </summary>
    public readonly struct HydrologyStats
    {
        public int GridWidth { get; }
        public int GridHeight { get; }
        public float CellSizeMeters { get; }
        public int SourceCount { get; }
        public int ReachCount { get; }
        public int TributaryCount { get; }
        public int OceanOutletCount { get; }
        public int LakeCount { get; }
        public int WaterfallCandidateCount { get; }
        public float TotalRiverLengthMeters { get; }
        public float SampleMs { get; }
        public float FillMs { get; }
        public float FlowMs { get; }
        public float NetworkMs { get; }
        public float TotalMs { get; }

        public HydrologyStats(
            int gridWidth,
            int gridHeight,
            float cellSizeMeters,
            int sourceCount,
            int reachCount,
            int tributaryCount,
            int oceanOutletCount,
            int lakeCount,
            int waterfallCandidateCount,
            float totalRiverLengthMeters,
            float sampleMs,
            float fillMs,
            float flowMs,
            float networkMs,
            float totalMs)
        {
            GridWidth = gridWidth;
            GridHeight = gridHeight;
            CellSizeMeters = cellSizeMeters;
            SourceCount = sourceCount;
            ReachCount = reachCount;
            TributaryCount = tributaryCount;
            OceanOutletCount = oceanOutletCount;
            LakeCount = lakeCount;
            WaterfallCandidateCount = waterfallCandidateCount;
            TotalRiverLengthMeters = totalRiverLengthMeters;
            SampleMs = sampleMs;
            FillMs = fillMs;
            FlowMs = flowMs;
            NetworkMs = networkMs;
            TotalMs = totalMs;
        }

        public int CellCount => GridWidth * GridHeight;

        public override string ToString() =>
            $"cells={GridWidth}x{GridHeight}@{CellSizeMeters:F2}m sources={SourceCount} " +
            $"reaches={ReachCount} tributaries={TributaryCount} outlets={OceanOutletCount} " +
            $"lakes={LakeCount} waterfalls={WaterfallCandidateCount} " +
            $"lengthKm={TotalRiverLengthMeters * 0.001f:F2} | " +
            $"sample={SampleMs:F0}ms fill={FillMs:F0}ms flow={FlowMs:F0}ms " +
            $"network={NetworkMs:F0}ms total={TotalMs:F0}ms";
    }

    /// <summary>
    /// The geographic truth of one generated overworld's inland water: what exists and where.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the source of truth, and the renderer is not. Meshes, gizmos and water samplers all
    /// read from here; nothing writes back. Keeping that direction one-way is what the previous,
    /// removed hydrology attempts got wrong — per-lake scene objects became the record of where
    /// water was, and every other system had to go through the scene to find out.
    /// </para>
    /// <para>
    /// Collections arrive already sorted by id, so iteration order is reproducible without the
    /// caller having to know that.
    /// </para>
    /// </remarks>
    public sealed class HydrologyDefinition
    {
        public static readonly HydrologyDefinition Empty = new HydrologyDefinition(
            Array.Empty<RiverReach>(),
            Array.Empty<LakeBasin>(),
            Array.Empty<WaterfallCandidate>(),
            default,
            HydrologySettings.Default);

        public IReadOnlyList<RiverReach> Reaches { get; }
        public IReadOnlyList<LakeBasin> Lakes { get; }
        public IReadOnlyList<WaterfallCandidate> Waterfalls { get; }
        public HydrologyStats Stats { get; }
        public HydrologySettings Settings { get; }

        public HydrologyDefinition(
            IReadOnlyList<RiverReach> reaches,
            IReadOnlyList<LakeBasin> lakes,
            IReadOnlyList<WaterfallCandidate> waterfalls,
            HydrologyStats stats,
            HydrologySettings settings)
        {
            Reaches = reaches ?? Array.Empty<RiverReach>();
            Lakes = lakes ?? Array.Empty<LakeBasin>();
            Waterfalls = waterfalls ?? Array.Empty<WaterfallCandidate>();
            Stats = stats;
            Settings = settings;
        }

        public bool IsEmpty => Reaches.Count == 0 && Lakes.Count == 0;

        public float TotalRiverLengthKilometers => Stats.TotalRiverLengthMeters * 0.001f;

        /// <summary>Looks a reach up by id, or null when the id is <see cref="ReachId.None"/>.</summary>
        /// <remarks>
        /// A linear scan on purpose: reach counts are in the tens, and a dictionary would be one
        /// more thing whose iteration order has to be kept deterministic.
        /// </remarks>
        public RiverReach FindReach(ReachId id)
        {
            if (!id.IsValid)
            {
                return null;
            }

            for (var i = 0; i < Reaches.Count; i++)
            {
                if (Reaches[i].Id == id)
                {
                    return Reaches[i];
                }
            }

            return null;
        }

        public LakeBasin FindLake(LakeId id)
        {
            if (!id.IsValid)
            {
                return null;
            }

            for (var i = 0; i < Lakes.Count; i++)
            {
                if (Lakes[i].Id == id)
                {
                    return Lakes[i];
                }
            }

            return null;
        }
    }
}

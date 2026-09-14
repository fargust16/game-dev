using UnityEngine;

namespace GameDev.World.Hydrology
{
    /// <summary>
    /// Every tunable threshold the hydrology analysis and carve use. No magic numbers live in the
    /// algorithms themselves.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Thresholds that describe the world are in metres or square metres, never in grid cells, so
    /// changing the analysis resolution cannot change the shape of the river network. That is the
    /// single most important property of this struct.
    /// </para>
    /// <para>
    /// The floor on every width is set by the terrain, not by taste: the tile heightmap is 513 px
    /// across 1400 m, i.e. 2.73 m per pixel, so a channel narrower than roughly three pixels
    /// (~8.2 m) cannot exist as terrain at all. See <c>HYDROLOGY_GENERATION_PLAN.md</c> §19.
    /// </para>
    /// </remarks>
    [System.Serializable]
    public struct HydrologySettings
    {
        /// <summary>Upstream area a cell needs before it is treated as a channel, in square metres.</summary>
        /// <remarks>
        /// The single knob that controls network density. Raising it drops the smallest headwaters
        /// first; it cannot create parallel duplicate networks, because D8 gives every cell exactly
        /// one downstream neighbour. It controls how branched each system is — how many systems
        /// there are is <see cref="MinRiverSystemAreaSqm"/>'s job.
        /// </remarks>
        public float ChannelInitiationAreaSqm;

        /// <summary>Contributing area at which channel and valley reach their maximum size.</summary>
        public float FullSizeAreaSqm;

        /// <summary>Smallest catchment that may become a river system in its own right.</summary>
        /// <remarks>
        /// An absolute floor, in square metres. Without it a coastal island produces a rill at every
        /// gully on the shoreline — the «dozens of parallel rivers» failure. A whole drainage tree
        /// below this is dropped, not truncated, so nothing is left flowing into nowhere.
        /// </remarks>
        public float MinRiverSystemAreaSqm;

        /// <summary>
        /// A system must also drain at least this fraction of what the largest system drains.
        /// </summary>
        /// <remarks>
        /// The absolute floor alone cannot be right for every island: raise it and a small world
        /// loses all its rivers, lower it and a large one grows a fringe of gullies. Judging each
        /// system against the biggest one on the same landmass is the standard cartographic way to
        /// decide what is worth drawing, and it keeps the count stable across seeds without anyone
        /// tuning a constant per world.
        /// </remarks>
        public float MinRiverSystemFractionOfLargest;

        /// <summary>Rise above the original surface below which a filled cell is noise, not a lake.</summary>
        public float LakeFillToleranceMeters;

        public float MinLakeAreaSqm;
        public float MinLakeDepthMeters;

        /// <summary>Spacing the reach centrelines are resampled to, in metres.</summary>
        public float NodeSpacingMeters;

        /// <summary>Deepest carve, in metres, the rasterised carve field may request.</summary>
        public float MaxCarveDepthMeters;

        public float ChannelHalfWidthMinMeters;
        public float ChannelHalfWidthMaxMeters;
        public float ChannelDepthMinMeters;
        public float ChannelDepthMaxMeters;

        public float ValleyHalfWidthMinMeters;
        public float ValleyHalfWidthMaxMeters;
        public float ValleyDepthMinMeters;
        public float ValleyDepthMaxMeters;

        /// <summary>Water depth in the channel, from headwater to main stem, in metres.</summary>
        public float FlowDepthMinMeters;
        public float FlowDepthMaxMeters;

        /// <summary>Minimum fall for a waterfall candidate, in metres.</summary>
        public float WaterfallMinDropMeters;

        /// <summary>Longest horizontal run over which that fall still reads as a waterfall.</summary>
        public float WaterfallMaxRunMeters;

        /// <summary>
        /// How much of the terrain grid the analysis uses: 1 = every heightmap sample, 2 = every
        /// second, and so on.
        /// </summary>
        /// <remarks>
        /// The documented fallback if planning turns out to be too slow to iterate with. Because
        /// every other threshold is in metres, changing this changes cost and precision but not the
        /// network's identity.
        /// </remarks>
        public int AnalysisStride;

        public static HydrologySettings Default => new HydrologySettings
        {
            // Both tuned against the first real bake, not derived from theory. The diagnostic
            // report prints the largest catchment the terrain actually produced, which is the
            // number to re-tune these against if the network comes out too sparse or too busy.
            ChannelInitiationAreaSqm = 30_000f,
            FullSizeAreaSqm = 800_000f,
            MinRiverSystemAreaSqm = 60_000f,
            MinRiverSystemFractionOfLargest = 0.18f,

            LakeFillToleranceMeters = 0.25f,
            MinLakeAreaSqm = 4_000f,
            MinLakeDepthMeters = 1.5f,

            NodeSpacingMeters = 12f,

            MaxCarveDepthMeters = 45f,

            // Three heightmap pixels is the narrowest channel the terrain can hold, so the minimum
            // half width is deliberately just over half of that.
            ChannelHalfWidthMinMeters = 4.5f,
            ChannelHalfWidthMaxMeters = 11f,
            ChannelDepthMinMeters = 1.2f,
            ChannelDepthMaxMeters = 4.5f,

            ValleyHalfWidthMinMeters = 20f,
            ValleyHalfWidthMaxMeters = 85f,
            ValleyDepthMinMeters = 2f,
            ValleyDepthMaxMeters = 15f,

            FlowDepthMinMeters = 0.4f,
            FlowDepthMaxMeters = 2.2f,

            WaterfallMinDropMeters = 6f,
            WaterfallMaxRunMeters = 30f,

            AnalysisStride = 1,
        };

        public void Clamp()
        {
            ChannelInitiationAreaSqm = Mathf.Max(1_000f, ChannelInitiationAreaSqm);
            FullSizeAreaSqm = Mathf.Max(ChannelInitiationAreaSqm * 2f, FullSizeAreaSqm);
            MinRiverSystemAreaSqm = Mathf.Max(ChannelInitiationAreaSqm, MinRiverSystemAreaSqm);
            MinRiverSystemFractionOfLargest = Mathf.Clamp01(MinRiverSystemFractionOfLargest);

            LakeFillToleranceMeters = Mathf.Max(0.01f, LakeFillToleranceMeters);
            MinLakeAreaSqm = Mathf.Max(0f, MinLakeAreaSqm);
            MinLakeDepthMeters = Mathf.Max(LakeFillToleranceMeters, MinLakeDepthMeters);

            NodeSpacingMeters = Mathf.Clamp(NodeSpacingMeters, 2f, 100f);
            MaxCarveDepthMeters = Mathf.Clamp(MaxCarveDepthMeters, 1f, WorldScale.TerrainHeightBudgetMeters);

            ChannelHalfWidthMinMeters = Mathf.Max(0.5f, ChannelHalfWidthMinMeters);
            ChannelHalfWidthMaxMeters = Mathf.Max(ChannelHalfWidthMinMeters, ChannelHalfWidthMaxMeters);
            ChannelDepthMinMeters = Mathf.Max(0.1f, ChannelDepthMinMeters);
            ChannelDepthMaxMeters = Mathf.Max(ChannelDepthMinMeters, ChannelDepthMaxMeters);

            ValleyHalfWidthMinMeters = Mathf.Max(ChannelHalfWidthMinMeters, ValleyHalfWidthMinMeters);
            ValleyHalfWidthMaxMeters = Mathf.Max(ValleyHalfWidthMinMeters, ValleyHalfWidthMaxMeters);
            ValleyDepthMinMeters = Mathf.Max(0f, ValleyDepthMinMeters);
            ValleyDepthMaxMeters = Mathf.Max(ValleyDepthMinMeters, ValleyDepthMaxMeters);

            FlowDepthMinMeters = Mathf.Max(0.05f, FlowDepthMinMeters);
            FlowDepthMaxMeters = Mathf.Max(FlowDepthMinMeters, FlowDepthMaxMeters);

            WaterfallMinDropMeters = Mathf.Max(0.5f, WaterfallMinDropMeters);
            WaterfallMaxRunMeters = Mathf.Max(NodeSpacingMeters, WaterfallMaxRunMeters);

            AnalysisStride = Mathf.Clamp(AnalysisStride, 1, 8);

            // The carve must be able to express the deepest thing it is asked to cut, or the
            // rasteriser silently clips the valley floor.
            var deepest = ValleyDepthMaxMeters + ChannelDepthMaxMeters;
            MaxCarveDepthMeters = Mathf.Max(MaxCarveDepthMeters, deepest);
        }

        /// <summary>
        /// Maps a contributing area onto 0..1, where 1 is a full-size main stem.
        /// </summary>
        /// <remarks>
        /// Square-rooted rather than linear because channel width in real hydraulic geometry scales
        /// roughly with the square root of discharge. Linear interpolation on raw area makes every
        /// tributary look like a ditch next to one enormous trunk.
        /// </remarks>
        public float SizeFactor01(float contributingAreaSqm)
        {
            var lo = ChannelInitiationAreaSqm;
            var hi = Mathf.Max(lo + 1f, FullSizeAreaSqm);
            var t = Mathf.InverseLerp(lo, hi, Mathf.Max(lo, contributingAreaSqm));
            return Mathf.Sqrt(Mathf.Clamp01(t));
        }

        public float ChannelHalfWidth(float areaSqm) =>
            Mathf.Lerp(ChannelHalfWidthMinMeters, ChannelHalfWidthMaxMeters, SizeFactor01(areaSqm));

        public float ChannelDepth(float areaSqm) =>
            Mathf.Lerp(ChannelDepthMinMeters, ChannelDepthMaxMeters, SizeFactor01(areaSqm));

        public float ValleyHalfWidth(float areaSqm) =>
            Mathf.Lerp(ValleyHalfWidthMinMeters, ValleyHalfWidthMaxMeters, SizeFactor01(areaSqm));

        public float ValleyDepth(float areaSqm) =>
            Mathf.Lerp(ValleyDepthMinMeters, ValleyDepthMaxMeters, SizeFactor01(areaSqm));

        public float FlowDepth(float areaSqm) =>
            Mathf.Lerp(FlowDepthMinMeters, FlowDepthMaxMeters, SizeFactor01(areaSqm));
    }
}

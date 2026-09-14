using System;
using System.Diagnostics;
using GameDev.World.Seed;
using Random = System.Random;

namespace GameDev.World.Hydrology
{
    /// <summary>
    /// Runs the drainage analysis end to end and publishes one <see cref="HydrologyDefinition"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pure C#: it takes a height field and returns geography. It knows nothing about terrain
    /// assets, meshes or the scene, which is what lets the whole algorithm be tested on a synthetic
    /// hillside without opening Unity.
    /// </para>
    /// <para>
    /// The order of the stages is not interchangeable. Depression filling has to come first because
    /// everything after it relies on the surface descending to the sea; lakes have to come before the
    /// network because a lake decides where a reach ends; waterfalls have to come last because they
    /// are read off finished reach geometry.
    /// </para>
    /// </remarks>
    public static class HydrologyPlanner
    {
        /// <summary>
        /// Why a plan produced nothing. Never a silent empty result — see
        /// <c>HYDROLOGY_GENERATION_PLAN.md</c> §16 rule 9.
        /// </summary>
        public enum FailureReason
        {
            None = 0,

            /// <summary>The grid has no sea connected to the world border, so nothing can drain.</summary>
            NoOcean,

            /// <summary>The flow field did not form a tree. Impossible on a filled surface.</summary>
            FlowCycle,

            /// <summary>No cell reached the channel-initiation threshold.</summary>
            NoChannels,

            /// <summary>The network was built but failed validation.</summary>
            ValidationFailed,
        }

        public readonly struct Result
        {
            public HydrologyDefinition Definition { get; }
            public FailureReason Failure { get; }
            public string Message { get; }

            public Result(HydrologyDefinition definition, FailureReason failure, string message)
            {
                Definition = definition;
                Failure = failure;
                Message = message;
            }

            public bool Succeeded => Failure == FailureReason.None;
        }

        public static Result Plan(HydrologyGrid grid, HydrologySettings settings, IWorldSeed worldSeed)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            settings.Clamp();
            var total = Stopwatch.StartNew();

            var oceanMask = grid.BuildOceanMask();
            var oceanCells = 0;
            for (var i = 0; i < oceanMask.Length; i++)
            {
                if (oceanMask[i])
                {
                    oceanCells++;
                }
            }

            if (oceanCells == 0)
            {
                return Fail(
                    FailureReason.NoOcean,
                    "no sea is connected to the world border, so no river can have an outlet");
            }

            var fillWatch = Stopwatch.StartNew();
            var filled = DepressionFill.Fill(grid, oceanMask);
            fillWatch.Stop();

            var flowWatch = Stopwatch.StartNew();
            var directions = FlowRouting.ComputeD8(grid, filled, oceanMask);
            var accumulation = FlowRouting.Accumulate(grid, directions, oceanMask, out var processed);
            flowWatch.Stop();

            if (processed != grid.CellCount)
            {
                return Fail(
                    FailureReason.FlowCycle,
                    $"flow routing visited {processed} of {grid.CellCount} cells, so a cycle survived "
                    + "the depression fill");
            }

            var networkWatch = Stopwatch.StartNew();
            var lakeField = BasinExtraction.Extract(grid, filled, oceanMask, settings);
            var rng = worldSeed?.Fork(RiverNetworkBuilder.SeedStream) ?? new Random(0);
            var network = RiverNetworkBuilder.Build(
                grid, filled, oceanMask, directions, accumulation, lakeField, settings, rng);
            var waterfalls = WaterfallDetector.Detect(network.Reaches, settings);
            networkWatch.Stop();

            if (network.Reaches.Count == 0)
            {
                return Fail(
                    FailureReason.NoChannels,
                    $"no cell reached the {settings.ChannelInitiationAreaSqm:F0} m2 channel threshold "
                    + "— the relief is too flat or the threshold is too high");
            }

            var validation = HydrologyValidator.Validate(network.Reaches, network.Lakes, grid);
            if (!validation.IsValid)
            {
                return Fail(FailureReason.ValidationFailed, validation.Message);
            }

            var totalLength = 0f;
            var tributaries = 0;
            var outlets = 0;
            for (var i = 0; i < network.Reaches.Count; i++)
            {
                var reach = network.Reaches[i];
                totalLength += reach.LengthMeters;
                if (reach.DownstreamId.IsValid)
                {
                    tributaries++;
                }

                if (reach.Termination == ReachTermination.Ocean)
                {
                    outlets++;
                }
            }

            total.Stop();

            var stats = new HydrologyStats(
                grid.Width,
                grid.Height,
                grid.CellSize,
                network.SourceCount,
                network.Reaches.Count,
                tributaries,
                outlets,
                network.Lakes.Count,
                waterfalls.Count,
                totalLength,
                0f,
                fillWatch.ElapsedMilliseconds,
                flowWatch.ElapsedMilliseconds,
                networkWatch.ElapsedMilliseconds,
                total.ElapsedMilliseconds);

            return new Result(
                new HydrologyDefinition(network.Reaches, network.Lakes, waterfalls, stats, settings),
                FailureReason.None,
                null);
        }

        /// <summary>
        /// Reports a failure with an empty definition attached.
        /// </summary>
        /// <remarks>
        /// Deliberately not a partial result and never a fabricated one. A seed that cannot produce
        /// valid geography produces none, and says why; inventing a river to satisfy a quota is what
        /// the requirement «prefer fewer valid rivers over invalid geography» rules out.
        /// </remarks>
        private static Result Fail(FailureReason reason, string message) =>
            new Result(HydrologyDefinition.Empty, reason, message);
    }
}

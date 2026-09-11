using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// What one belt's geometry actually came out as, for judging it without opening an image.
    /// </summary>
    /// <remarks>
    /// <see cref="ReliefElongation"/> is the dome detector. It is the ratio of the two principal
    /// spreads of the belt's own relief - every sample where the belt raises the ground by at least a
    /// third of its crest - so a circular hill scores about one and a genuine range scores close to
    /// its length-to-width ratio. It measures the relief that came out, not the spine that went in,
    /// which is the distinction the previous implementation failed: its spines were long while its
    /// relief was a row of circles.
    /// </remarks>
    public readonly struct RangeMetrics
    {
        public float LengthMeters { get; }
        public float CharacteristicHalfWidthMeters { get; }
        public float LengthToWidthRatio { get; }
        public float HighestCrestMeters { get; }

        /// <summary>Local maxima of the crest profile above 55 % of its peak.</summary>
        public int MajorSummits { get; }

        /// <summary>Normalised arc positions of those summits.</summary>
        public float[] SummitPositions { get; }

        /// <summary>Lowest crest between the first and last major summit, as a fraction of the peak.</summary>
        public float MinSaddleFraction { get; }

        /// <summary>Principal spread ratio of the belt's own relief. About 1 is a dome.</summary>
        public float ReliefElongation { get; }

        /// <summary>Fraction of the range where the curvature cap, not the profile, set the width.</summary>
        public float CurvatureLimitedFraction { get; }

        public RangeMetrics(
            float lengthMeters,
            float characteristicHalfWidthMeters,
            float highestCrestMeters,
            int majorSummits,
            float[] summitPositions,
            float minSaddleFraction,
            float reliefElongation,
            float curvatureLimitedFraction)
        {
            LengthMeters = lengthMeters;
            CharacteristicHalfWidthMeters = characteristicHalfWidthMeters;
            LengthToWidthRatio = characteristicHalfWidthMeters > 1e-3f
                ? lengthMeters / characteristicHalfWidthMeters
                : 0f;
            HighestCrestMeters = highestCrestMeters;
            MajorSummits = majorSummits;
            SummitPositions = summitPositions ?? System.Array.Empty<float>();
            MinSaddleFraction = minSaddleFraction;
            ReliefElongation = reliefElongation;
            CurvatureLimitedFraction = curvatureLimitedFraction;
        }
    }

    /// <summary>One Stage 2 world: the Stage 1 landmass with its uplift belts raised on it.</summary>
    public sealed class Stage2Result
    {
        public TerrainGenSettings Settings { get; }

        /// <summary>The Stage 1 world this was built on, unmodified.</summary>
        public Stage1Result Stage1 { get; }

        public WorldStructure Structure { get; }
        public CoastQuery Coast { get; }
        public CoastHeightModel CoastModel { get; }
        public MountainUpliftModel Uplift { get; }

        /// <summary>Stage 1 height plus uplift.</summary>
        public WorldHeightField Field { get; }

        /// <summary>The uplift contribution alone, for diagnostics.</summary>
        public WorldHeightField UpliftField { get; }

        public TerrainGenStats Stats { get; }

        /// <summary>Per-belt geometry, in the same order as <c>Structure.Ranges</c>.</summary>
        public IReadOnlyList<RangeMetrics> Metrics { get; }

        public Stage2Result(
            TerrainGenSettings settings,
            Stage1Result stage1,
            WorldStructure structure,
            MountainUpliftModel uplift,
            WorldHeightField field,
            WorldHeightField upliftField,
            TerrainGenStats stats,
            IReadOnlyList<RangeMetrics> metrics)
        {
            Metrics = metrics ?? System.Array.Empty<RangeMetrics>();
            Settings = settings;
            Stage1 = stage1;
            Structure = structure;
            Coast = stage1.Coast;
            CoastModel = stage1.Model;
            Uplift = uplift;
            Field = field;
            UpliftField = upliftField;
            Stats = stats;
        }

        /// <summary>Uplift at a world point, evaluated as a pure function of position.</summary>
        public float UpliftAt(float worldX, float worldZ) =>
            Uplift.UpliftAt(worldX, worldZ, Coast.SignedDistance(worldX, worldZ));

        /// <summary>
        /// Final Stage 2 height at a world point, evaluated as a pure function of position.
        /// </summary>
        /// <remarks>
        /// The signed distance is computed once and fed to both halves, exactly as the bulk pass
        /// does, so the point query and the field cannot drift apart.
        /// </remarks>
        public float HeightAt(float worldX, float worldZ)
        {
            var signed = Coast.SignedDistance(worldX, worldZ);
            return CoastModel.HeightAt(signed) + Uplift.UpliftAt(worldX, worldZ, signed);
        }
    }

    /// <summary>
    /// Stage 2 of the deterministic terrain generator: structural mountain uplift.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Stage 1 produced a landmass whose height is a function of distance to the shore and nothing
    /// else. Stage 2 adds the first real relief: one or two uplift belts chained from the boundaries
    /// between crustal blocks, each with a longitudinal crest profile, a cross-range flank profile
    /// and an asymmetry that makes its two sides different.
    /// </para>
    /// <para>
    /// <b>Nothing here is a noise field.</b> Every metre of uplift is the product of two authored
    /// curves evaluated against a distance to an explicit world-space polyline, and every polyline is
    /// a chain of crustal boundaries. There are no octaves, no domain warp and no erosion; ridges,
    /// valleys, drainage and refinement all belong to later stages and none of them is here.
    /// </para>
    /// <para>
    /// Stage 1 remains a valid output on its own: this runs it, reads its structure, and adds to its
    /// field without changing a single Stage 1 decision.
    /// </para>
    /// </remarks>
    public static class Stage2MountainGenerator
    {
        public static Stage2Result Generate(int seed, TerrainGenSettings settings) =>
            Generate(new Seed.WorldSeed(seed), settings);

        public static Stage2Result Generate(Seed.IWorldSeed worldSeed, TerrainGenSettings settings)
        {
            settings.Clamp();
            var total = Stopwatch.StartNew();

            var stage1 = Stage1LandmassGenerator.Generate(worldSeed, settings);

            var sw = Stopwatch.StartNew();
            var edges = CrustalEdges.Build(
                stage1.Structure.Partition, stage1.Structure.LandSites, stage1.Coast);
            var edgesMs = sw.ElapsedMilliseconds;

            sw.Restart();
            var ranges = UpliftBeltBuilder.Build(worldSeed, settings, edges, stage1.Coast);
            var beltsMs = sw.ElapsedMilliseconds;

            var structure = stage1.Structure.WithRanges(ranges);
            var uplift = new MountainUpliftModel(ranges, settings);

            sw.Restart();
            EvaluateFields(stage1, uplift, out var field, out var upliftField);
            var fieldMs = sw.ElapsedMilliseconds;

            var stats = BuildStats(stage1, structure, edges, settings, field, upliftField);
            stats.EdgesMs = edgesMs;
            stats.BeltsMs = beltsMs;
            stats.UpliftFieldMs = fieldMs;
            stats.TotalMs = total.ElapsedMilliseconds;

            var metrics = MeasureRanges(structure, field);
            for (var i = 0; i < metrics.Count; i++)
            {
                stats.MinLengthToWidthRatio = i == 0
                    ? metrics[i].LengthToWidthRatio
                    : Mathf.Min(stats.MinLengthToWidthRatio, metrics[i].LengthToWidthRatio);
                stats.MinReliefElongation = i == 0
                    ? metrics[i].ReliefElongation
                    : Mathf.Min(stats.MinReliefElongation, metrics[i].ReliefElongation);
                stats.MaxCurvatureLimitedFraction =
                    Mathf.Max(stats.MaxCurvatureLimitedFraction, metrics[i].CurvatureLimitedFraction);
                stats.MinSaddleFraction = i == 0
                    ? metrics[i].MinSaddleFraction
                    : Mathf.Min(stats.MinSaddleFraction, metrics[i].MinSaddleFraction);
            }

            return new Stage2Result(
                settings, stage1, structure, uplift, field, upliftField, stats, metrics);
        }

        /// <summary>
        /// Adds uplift to the Stage 1 field, producing the final field and the uplift alone.
        /// </summary>
        /// <remarks>
        /// Stage 1's field and its signed distances are reused rather than recomputed. The signed
        /// distance is the expensive quantity in this generator and it cannot have changed, so
        /// evaluating it again would double the cost of Stage 2 for an identical answer.
        /// <para>
        /// The uplift is a pure function of position and that distance, so nothing is carried between
        /// samples and the traversal order cannot matter.
        /// </para>
        /// </remarks>
        public static void EvaluateFields(
            Stage1Result stage1,
            MountainUpliftModel uplift,
            out WorldHeightField field,
            out WorldHeightField upliftField)
        {
            var baseField = stage1.Field;
            field = WorldHeightField.ForWorld(baseField.Width);
            upliftField = WorldHeightField.ForWorld(baseField.Width);

            for (var z = 0; z < baseField.Height; z++)
            {
                var worldZ = baseField.WorldZ(z);
                var row = z * baseField.Width;
                for (var x = 0; x < baseField.Width; x++)
                {
                    var index = row + x;
                    var contribution = uplift.UpliftAt(
                        baseField.WorldX(x), worldZ, stage1.SignedDistance[index]);
                    upliftField.Heights[index] = contribution;
                    field.Heights[index] = baseField.Heights[index] + contribution;
                }
            }
        }

        /// <summary>
        /// Measures each belt's geometry from the relief it produces, not from its parameters.
        /// </summary>
        /// <remarks>
        /// Sampled on the world lattice at a stride, over the belt's own support rectangle. A stride
        /// is enough: this is a covariance, and halving the sample count in each axis changes it by
        /// nothing that matters while making the measurement cost a fraction of the field pass.
        /// </remarks>
        private static List<RangeMetrics> MeasureRanges(WorldStructure structure, WorldHeightField field)
        {
            const int stride = 2;
            var metrics = new List<RangeMetrics>(structure.Ranges.Count);

            for (var r = 0; r < structure.Ranges.Count; r++)
            {
                var range = structure.Ranges[r];
                var threshold = range.CrestHeightMeters * 0.33f;

                var minX = Mathf.Max(0, Mathf.FloorToInt(range.Support.xMin / field.CellSize));
                var maxX = Mathf.Min(field.Width - 1, Mathf.CeilToInt(range.Support.xMax / field.CellSize));
                var minZ = Mathf.Max(0, Mathf.FloorToInt(range.Support.yMin / field.CellSize));
                var maxZ = Mathf.Min(field.Height - 1, Mathf.CeilToInt(range.Support.yMax / field.CellSize));

                // Measured in the range's own frame. A covariance in world axes reports a curved
                // belt as round however narrow it is - seed 424242's branch scored 1.57 while being
                // a perfectly good ridge - so the extent is taken along the axis and across it.
                var tMin = 1f;
                var tMax = 0f;
                var offsetMin = float.MaxValue;
                var offsetMax = float.MinValue;
                var samples = 0;

                for (var z = minZ; z <= maxZ; z += stride)
                {
                    var worldZ = field.WorldZ(z);
                    for (var x = minX; x <= maxX; x += stride)
                    {
                        var worldX = field.WorldX(x);
                        if (range.Evaluate(worldX, worldZ) < threshold)
                        {
                            continue;
                        }

                        range.Project(worldX, worldZ, out var t, out var offset);
                        samples++;
                        tMin = Mathf.Min(tMin, t);
                        tMax = Mathf.Max(tMax, t);
                        offsetMin = Mathf.Min(offsetMin, offset);
                        offsetMax = Mathf.Max(offsetMax, offset);
                    }
                }

                var elongation = 1f;
                if (samples > 3 && offsetMax > offsetMin)
                {
                    var longitudinal = (tMax - tMin) * range.LengthMeters;
                    var transverse = offsetMax - offsetMin;
                    elongation = transverse > 1e-3f ? longitudinal / transverse : 1f;
                }

                MeasureCrest(range, out var summits, out var positions, out var minSaddle);

                metrics.Add(new RangeMetrics(
                    range.LengthMeters,
                    range.CharacteristicHalfWidthMeters,
                    range.CrestHeightMeters,
                    summits,
                    positions,
                    minSaddle,
                    elongation,
                    range.CurvatureLimitedFraction));
            }

            return metrics;
        }

        /// <summary>Major summits of the crest profile and the lowest saddle between them.</summary>
        private static void MeasureCrest(
            MountainRange range, out int summits, out float[] positions, out float minSaddle)
        {
            const int samples = 401;
            var values = new float[samples];
            var peak = 0f;
            for (var i = 0; i < samples; i++)
            {
                values[i] = range.Crest.Evaluate(i / (float)(samples - 1));
                peak = Mathf.Max(peak, values[i]);
            }

            var found = new List<float>(4);
            var cutoff = peak * 0.55f;
            for (var i = 1; i < samples - 1; i++)
            {
                if (values[i] >= cutoff && values[i] >= values[i - 1] && values[i] > values[i + 1])
                {
                    found.Add(i / (float)(samples - 1));
                }
            }

            summits = found.Count;
            positions = found.ToArray();

            minSaddle = 1f;
            if (found.Count >= 2)
            {
                var first = Mathf.RoundToInt(found[0] * (samples - 1));
                var last = Mathf.RoundToInt(found[found.Count - 1] * (samples - 1));
                var lowest = float.MaxValue;
                for (var i = first; i <= last; i++)
                {
                    lowest = Mathf.Min(lowest, values[i]);
                }

                minSaddle = peak > 1e-4f ? lowest / peak : 0f;
            }
        }

        private static TerrainGenStats BuildStats(
            Stage1Result stage1,
            WorldStructure structure,
            CrustalEdges edges,
            TerrainGenSettings settings,
            WorldHeightField field,
            WorldHeightField upliftField)
        {
            var s = stage1.Stats;

            // Stage 1's own numbers are kept alongside so the two stages can be compared in one row
            // rather than by diffing two reports.
            var stats = new TerrainGenStats
            {
                Seed = s.Seed,
                GeneratorVersion = s.GeneratorVersion,
                SiteCount = s.SiteCount,
                SitesInWorld = s.SitesInWorld,
                EligibleSites = s.EligibleSites,
                LandSiteCount = s.LandSiteCount,
                IsletCount = s.IsletCount,
                ComponentCount = s.ComponentCount,
                TargetLandFraction = s.TargetLandFraction,
                RasterLandFraction = s.RasterLandFraction,
                PolygonLandFraction = s.PolygonLandFraction,
                EnclosedSeaFilled = s.EnclosedSeaFilled,
                LandBudgetShort = s.LandBudgetShort,
                CoastRingCount = s.CoastRingCount,
                CoastVertexCount = s.CoastVertexCount,
                CoastLengthMeters = s.CoastLengthMeters,
                CoastCrenulation = s.CoastCrenulation,
                MaxRingDisplaceAttempts = s.MaxRingDisplaceAttempts,
                RingsFallenBack = s.RingsFallenBack,
                PartitionMs = s.PartitionMs,
                LandmassMs = s.LandmassMs,
                FieldMs = s.FieldMs,

                Stage1MinHeightMeters = s.MinHeightMeters,
                Stage1MaxHeightMeters = s.MaxHeightMeters,
                Stage1MeanHeightMeters = s.MeanHeightMeters,
                Stage1LandFraction = s.FieldLandFraction,

                CrustalEdgeCount = edges.Edges.Length,
            };

            for (var i = 0; i < structure.Ranges.Count; i++)
            {
                var range = structure.Ranges[i];
                if (range.ParentIndex < 0)
                {
                    stats.MountainSystemCount++;
                }
                else
                {
                    stats.BranchCount++;
                }

                stats.TotalSpineMeters += range.LengthMeters;
                stats.LongestSpineMeters = Mathf.Max(stats.LongestSpineMeters, range.LengthMeters);
                stats.TallestCrestMeters = Mathf.Max(stats.TallestCrestMeters, range.CrestHeightMeters);
            }

            var min = float.PositiveInfinity;
            var max = float.NegativeInfinity;
            var sum = 0.0;
            var landSamples = 0;

            var upliftMin = float.PositiveInfinity;
            var upliftMax = float.NegativeInfinity;
            var upliftSum = 0.0;
            var upliftLandSum = 0.0;
            var ceiling = settings.MaxUpliftMeters - 0.01f;

            for (var i = 0; i < field.Heights.Length; i++)
            {
                var h = field.Heights[i];
                var u = upliftField.Heights[i];

                if (float.IsNaN(h) || float.IsInfinity(h) || float.IsNaN(u) || float.IsInfinity(u))
                {
                    stats.HasNonFinite = true;
                    continue;
                }

                min = Mathf.Min(min, h);
                max = Mathf.Max(max, h);
                sum += h;

                upliftMin = Mathf.Min(upliftMin, u);
                upliftMax = Mathf.Max(upliftMax, u);
                upliftSum += u;

                if (u >= ceiling)
                {
                    stats.UpliftClampedSamples++;
                }

                var stage1Height = stage1.Field.Heights[i];
                if (stage1Height > WorldScale.SeaLevelMeters)
                {
                    landSamples++;
                    upliftLandSum += u;
                }
                else if (h != stage1Height)
                {
                    // The coast gate makes this impossible; counting it is how we know that.
                    stats.OceanSamplesChanged++;
                }
            }

            stats.MinHeightMeters = min;
            stats.MaxHeightMeters = max;
            stats.MeanHeightMeters = (float)(sum / field.Heights.Length);
            stats.FieldLandFraction = landSamples / (float)field.Heights.Length;
            stats.FieldLandComponents = s.FieldLandComponents;
            stats.UpliftMinMeters = upliftMin;
            stats.UpliftMaxMeters = upliftMax;
            stats.UpliftMeanMeters = (float)(upliftSum / field.Heights.Length);
            stats.UpliftMeanOverLandMeters =
                landSamples > 0 ? (float)(upliftLandSum / landSamples) : 0f;
            stats.FieldHash = field.Hash();

            return stats;
        }
    }
}

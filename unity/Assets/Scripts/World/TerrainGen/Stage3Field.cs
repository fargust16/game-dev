using System.Diagnostics;
using System.Globalization;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Stage 3C: the Stage 3 surface, cut out of the Stage 2 surface by the valley structure.
    /// </summary>
    /// <remarks>
    /// Subtractive and nothing else. <c>H3 = H2 - C</c>, so the Stage 2 envelope, the certified core,
    /// the width budget, the height budget and the coastline all survive without being checked for -
    /// they cannot be violated by an operation that only removes. What is measured here is whether the
    /// removal left a surface that is still geographic: the slope bands, the isotropy, and whether any
    /// pit appeared that nothing declared.
    /// </remarks>
    public sealed class Stage3Field
    {
        public int Seed { get; }

        public RidgeSkeleton Ridges { get; }

        /// <summary>Stage 1 + Stage 2 massif uplift, untouched.</summary>
        public WorldHeightField Stage2 { get; }

        /// <summary>The Stage 3 surface.</summary>
        public WorldHeightField Field { get; }

        /// <summary>What was removed, metres, never negative.</summary>
        public WorldHeightField Carve { get; }

        public Stage3FieldStats Stats { get; }

        public Stage3Field(
            int seed,
            RidgeSkeleton ridges,
            WorldHeightField stage2,
            WorldHeightField field,
            WorldHeightField carve,
            Stage3FieldStats stats)
        {
            Seed = seed;
            Ridges = ridges;
            Stage2 = stage2;
            Field = field;
            Carve = carve;
            Stats = stats;
        }

        public static Stage3Field Build(
            Stage2BResult stage2b, RidgeSkeleton ridges, RidgeSettings settings)
        {
            settings.Clamp();
            var sw = Stopwatch.StartNew();

            var carveModel = new CarveField(
                ridges, stage2b.Stage1.Coast, settings, stage2b.MassifSettings);

            var source = stage2b.Field;
            var field = WorldHeightField.ForWorld(source.Width);
            var carve = WorldHeightField.ForWorld(source.Width);

            var stats = new Stage3FieldStats
            {
                Seed = stage2b.Seed,
                IncisionBoundMeters = settings.CorridorIncisionMeters,
            };
            var stage1 = stage2b.Stage1;

            for (var z = 0; z < source.Height; z++)
            {
                var worldZ = source.WorldZ(z);
                for (var x = 0; x < source.Width; x++)
                {
                    var index = z * source.Width + x;
                    var worldX = source.WorldX(x);
                    var above = source.Heights[index];

                    // Stage 1 already measured the signed distance on this very lattice, so the
                    // gate and the land test are reads rather than two coastline walks per sample.
                    var inland = stage1.SignedDistance[index];
                    var depth = carveModel.Depth(
                        worldX, worldZ, carveModel.GateFor(inland), inland > 0f);
                    if (depth < 0f)
                    {
                        depth = 0f;
                    }

                    // A valley corridor may cut below the Stage 1 surface, but only by the derived
                    // bound - that is the whole of the exception, and it is what lets the trough
                    // continue across the island floor instead of the valley ending at the toe.
                    // The coastline still cannot move: the gate is zero at the shore, so the trough
                    // has already faded to nothing before it gets there.
                    var floor = stage1.Field.Heights[index];
                    var allowance = Mathf.Max(0f, above - floor) + settings.CorridorIncisionMeters;
                    var cut = Mathf.Min(depth, allowance);
                    cut = Mathf.Min(cut, Mathf.Max(0f, above - WorldScale.SeaLevelMeters - 0.5f));

                    carve.Heights[index] = cut;
                    field.Heights[index] = above - cut;

                    stats.MaxCarveMeters = Mathf.Max(stats.MaxCarveMeters, cut);
                    if (cut > 0.01f)
                    {
                        stats.CarvedSamples++;
                        stats.TotalCarveMeters += cut;
                    }

                    if (depth > cut + 0.01f)
                    {
                        stats.DepthClampedSamples++;
                    }

                    if (field.Heights[index] < floor - 1e-4f)
                    {
                        stats.CorridorIncisionSamples++;
                        stats.MaxIncisionMeters = Mathf.Max(
                            stats.MaxIncisionMeters, floor - field.Heights[index]);
                    }
                }
            }

            stats.FieldMs = sw.ElapsedMilliseconds;
            stats.FieldHash = field.Hash();
            stats.CarveHash = carve.Hash();
            Measure(stage2b, field, stats);

            return new Stage3Field(stage2b.Seed, ridges, source, field, carve, stats);
        }

        /// <summary>
        /// Measures the surface against the targets in WORLD_RELIEF_PRINCIPLES, and against Stage 2.
        /// </summary>
        private static void Measure(
            Stage2BResult stage2b, WorldHeightField field, Stage3FieldStats stats)
        {
            var stage1 = stage2b.Stage1;
            var source = stage2b.Field;
            var cell = field.CellSize;

            var land = 0;
            var gentle = 0;
            var steep = 0;
            var passable = new bool[field.Width * field.Height];

            double curvatureX = 0;
            double curvatureZ = 0;
            var curvatureSamples = 0;

            for (var z = 1; z + 1 < field.Height; z++)
            {
                for (var x = 1; x + 1 < field.Width; x++)
                {
                    var index = z * field.Width + x;
                    var here = field.Heights[index];

                    if (here > source.Heights[index] + 1e-4f)
                    {
                        stats.SamplesAboveStage2++;
                    }

                    // Only a breach of the declared bound counts. Incision inside the bound is the
                    // corridor exception and is reported separately.
                    if (here < stage1.Field.Heights[index] - stats.IncisionBoundMeters - 1e-3f)
                    {
                        stats.SamplesBelowStage1++;
                    }

                    if (stage1.SignedDistance[index] <= 0f)
                    {
                        if (here != source.Heights[index])
                        {
                            stats.OceanSamplesChanged++;
                        }

                        continue;
                    }

                    land++;

                    var dx = (field.Heights[index + 1] - field.Heights[index - 1]) / (2f * cell);
                    var dz = (field.Heights[index + field.Width]
                              - field.Heights[index - field.Width]) / (2f * cell);
                    var slope = Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg;

                    if (slope < 5f)
                    {
                        gentle++;
                    }

                    if (slope > 45f)
                    {
                        steep++;
                    }

                    passable[index] = slope <= 45f;

                    var ddx = field.Heights[index + 1] - 2f * here + field.Heights[index - 1];
                    var ddz = field.Heights[index + field.Width] - 2f * here
                              + field.Heights[index - field.Width];
                    curvatureX += Mathf.Abs(ddx);
                    curvatureZ += Mathf.Abs(ddz);
                    curvatureSamples++;
                }
            }

            stats.LandSamples = land;
            if (land > 0)
            {
                stats.GentleFraction = gentle / (float)land;
                stats.SteepFraction = steep / (float)land;
                stats.ReachableFraction = Reachable(field, stage1, passable) / (float)land;
            }

            if (curvatureSamples > 0 && curvatureZ > 1e-9)
            {
                // The banding investigation's measure: one means the surface bends the same amount
                // east-west as north-south, which is what an isotropic landform does.
                var ratio = curvatureX / curvatureZ;
                stats.IsotropyRatio = (float)(ratio > 1.0 ? 1.0 / ratio : ratio);
            }

            // The same measure on the surface Stage 3 cut into, because the target is "no worse than
            // Stage 2" and a number without its baseline decides nothing.
            stats.Stage2IsotropyRatio = Isotropy(source, stage1);
        }

        private static float Isotropy(WorldHeightField field, Stage1Result stage1)
        {
            double dx = 0;
            double dz = 0;
            for (var z = 1; z + 1 < field.Height; z++)
            {
                for (var x = 1; x + 1 < field.Width; x++)
                {
                    var i = z * field.Width + x;
                    if (stage1.SignedDistance[i] <= 0f)
                    {
                        continue;
                    }

                    var h = field.Heights[i];
                    dx += Mathf.Abs(field.Heights[i + 1] - 2f * h + field.Heights[i - 1]);
                    dz += Mathf.Abs(
                        field.Heights[i + field.Width] - 2f * h + field.Heights[i - field.Width]);
                }
            }

            if (dz <= 1e-9)
            {
                return 1f;
            }

            var ratio = dx / dz;
            return (float)(ratio > 1.0 ? 1.0 / ratio : ratio);
        }

        /// <summary>
        /// Land reachable from the shore without ever crossing ground steeper than 45 degrees.
        /// </summary>
        /// <remarks>
        /// The relief principles ask for reachability, not for a slope histogram: 45 degrees is
        /// <c>CharacterController.slopeLimit</c>, so what matters is whether a walker can get there,
        /// and a steep band only disqualifies the ground behind it. A flood fill from every coastal
        /// land sample is the measurement that question actually asks for.
        /// </remarks>
        private static int Reachable(WorldHeightField field, Stage1Result stage1, bool[] passable)
        {
            var width = field.Width;
            var seen = new bool[passable.Length];
            var queue = new System.Collections.Generic.Queue<int>();

            for (var z = 1; z + 1 < field.Height; z++)
            {
                for (var x = 1; x + 1 < width; x++)
                {
                    var i = z * width + x;
                    if (!passable[i] || seen[i])
                    {
                        continue;
                    }

                    // A shore cell: land, passable, and within a cell of the water.
                    if (stage1.SignedDistance[i] > field.CellSize * 1.5f)
                    {
                        continue;
                    }

                    seen[i] = true;
                    queue.Enqueue(i);
                }
            }

            var reached = 0;
            while (queue.Count > 0)
            {
                var i = queue.Dequeue();
                reached++;

                var x = i % width;
                var z = i / width;

                for (var d = 0; d < 4; d++)
                {
                    var nx = x + (d == 0 ? 1 : d == 1 ? -1 : 0);
                    var nz = z + (d == 2 ? 1 : d == 3 ? -1 : 0);
                    if (nx <= 0 || nz <= 0 || nx + 1 >= width || nz + 1 >= field.Height)
                    {
                        continue;
                    }

                    var n = nz * width + nx;
                    if (seen[n] || !passable[n] || stage1.SignedDistance[n] <= 0f)
                    {
                        continue;
                    }

                    seen[n] = true;
                    queue.Enqueue(n);
                }
            }

            return reached;
        }
    }

    /// <summary>What Stage 3C measured about the surface it produced.</summary>
    public sealed class Stage3FieldStats
    {
        public int Seed;

        public int CarvedSamples;
        public int LandSamples;
        public float MaxCarveMeters;
        public float TotalCarveMeters;

        /// <summary>Samples where the carve was limited by the Stage 1 floor.</summary>
        public int DepthClampedSamples;

        /// <summary>Samples where Stage 3 sits above Stage 2. Expected zero.</summary>
        public int SamplesAboveStage2;

        /// <summary>Land samples cut below Stage 1 by more than the declared bound. Expected zero.</summary>
        public int SamplesBelowStage1;

        /// <summary>The declared corridor incision bound this run was measured against, metres.</summary>
        public float IncisionBoundMeters;

        /// <summary>Land samples inside the declared corridor exception.</summary>
        public int CorridorIncisionSamples;

        /// <summary>Deepest incision below Stage 1 anywhere, metres. Must not exceed the bound.</summary>
        public float MaxIncisionMeters;

        /// <summary>Ocean samples that moved. Expected zero.</summary>
        public int OceanSamplesChanged;

        /// <summary>Fraction of land under five degrees. Target at least 0.35.</summary>
        public float GentleFraction;

        /// <summary>
        /// Fraction of land a walker can reach from the shore at 45 degrees or less. Target 0.95.
        /// </summary>
        public float ReachableFraction;

        /// <summary>Fraction of land steeper than 45 degrees. Target 0.06 to 0.15.</summary>
        public float SteepFraction;

        /// <summary>Curvature ratio, one being isotropic.</summary>
        public float IsotropyRatio = 1f;

        /// <summary>The same ratio on the Stage 2 surface, which Stage 3 must not do worse than.</summary>
        public float Stage2IsotropyRatio = 1f;

        public string FieldHash = string.Empty;
        public string CarveHash = string.Empty;
        public long FieldMs;

        public float MeanCarveMeters =>
            CarvedSamples > 0 ? TotalCarveMeters / CarvedSamples : 0f;

        public string ToLine()
        {
            var culture = CultureInfo.InvariantCulture;
            return $"seed {Seed}: carved={CarvedSamples} ({(100f * CarvedSamples / Mathf.Max(1, LandSamples)).ToString("F1", culture)}% of land)"
                   + $" maxCut={MaxCarveMeters.ToString("F1", culture)}m"
                   + $" meanCut={MeanCarveMeters.ToString("F1", culture)}m"
                   + $" aboveStage2={SamplesAboveStage2} belowStage1={SamplesBelowStage1}"
                   + $" oceanMoved={OceanSamplesChanged}"
                   + $" incised={CorridorIncisionSamples}"
                   + $" maxIncision={MaxIncisionMeters.ToString("F1", culture)}m"
                   + $" slope[<5={(100f * GentleFraction).ToString("F1", culture)}%"
                   + $" reachable={(100f * ReachableFraction).ToString("F1", culture)}%"
                   + $" >45={(100f * SteepFraction).ToString("F1", culture)}%]"
                   + $" isotropy={IsotropyRatio.ToString("F2", culture)}"
                   + $" (stage2 {Stage2IsotropyRatio.ToString("F2", culture)})"
                   + $" hash={FieldHash} field={FieldMs}ms";
        }
    }
}

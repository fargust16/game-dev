using System.Collections.Generic;
using System.Diagnostics;
using GameDev.World.Town.Geometry;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// One Stage 1 world: crustal partition, landmass, coastline and the coastal height field.
    /// </summary>
    public sealed class Stage1Result
    {
        public TerrainGenSettings Settings { get; }
        public WorldStructure Structure { get; }
        public CoastQuery Coast { get; }
        public CoastHeightModel Model { get; }
        public WorldHeightField Field { get; }

        /// <summary>
        /// Signed distance to the coast at every lattice sample, positive inland, row-major.
        /// </summary>
        /// <remarks>
        /// A by-product of evaluating the field, kept rather than recomputed. It is the single most
        /// expensive quantity in the generator - about 5 of Stage 1's 6 seconds - and every later
        /// stage needs it to know how far inland a point is. Recomputing it per stage doubles
        /// generation time for a number that cannot have changed.
        /// </remarks>
        public float[] SignedDistance { get; }

        public LandmassBuilder.Result Landmass { get; }
        public TerrainGenStats Stats { get; }

        public Stage1Result(
            TerrainGenSettings settings,
            WorldStructure structure,
            CoastQuery coast,
            CoastHeightModel model,
            WorldHeightField field,
            float[] signedDistance,
            LandmassBuilder.Result landmass,
            TerrainGenStats stats)
        {
            Settings = settings;
            Structure = structure;
            Coast = coast;
            Model = model;
            Field = field;
            SignedDistance = signedDistance;
            Landmass = landmass;
            Stats = stats;
        }

        /// <summary>
        /// World height at a world coordinate, evaluated as a pure function of position.
        /// </summary>
        /// <remarks>
        /// This is the obligation the design places on structure evaluation: no dependence on
        /// evaluation order and none on neighbouring samples. The bulk field is what actually runs;
        /// this exists so the D2 test can prove the two agree, and because streaming or a
        /// higher-resolution tile would need it later.
        /// </remarks>
        public float HeightAt(float worldX, float worldZ) =>
            Model.HeightAt(Coast.SignedDistance(worldX, worldZ));
    }

    /// <summary>
    /// Stage 1 of the deterministic terrain generator: land, sea and the coast between them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// There are no mountains, no ridges, no basins and no rivers here, and adding any of them
    /// belongs to a later stage. What this stage has to earn is narrow and checkable: a landmass
    /// whose outline is structural rather than radial, a height field that is deterministic and
    /// seamless because it is one world lattice, and a set of numbers a human can judge it by.
    /// </para>
    /// <para>
    /// The field is evaluated at <see cref="TerrainGenSettings.FieldResolution"/> in world
    /// coordinates. At the V0 layout that is 1025 x 1025, which is exactly the lattice the four
    /// Unity tiles share - so tile seams are the same array element read twice rather than two
    /// values that have to be matched. Nothing here knows tiles exist.
    /// </para>
    /// </remarks>
    public static class Stage1LandmassGenerator
    {
        public static Stage1Result Generate(int seed, TerrainGenSettings settings) =>
            Generate(new Seed.WorldSeed(seed), settings);

        public static Stage1Result Generate(Seed.IWorldSeed worldSeed, TerrainGenSettings settings)
        {
            settings.Clamp();

            var total = Stopwatch.StartNew();
            var stats = new TerrainGenStats
            {
                Seed = worldSeed.Value,
                GeneratorVersion = settings.GeneratorVersion,
            };

            var sw = Stopwatch.StartNew();
            var partition = CrustalPartition.Build(worldSeed, settings);
            stats.PartitionMs = sw.ElapsedMilliseconds;

            stats.SiteCount = partition.SiteCount;
            for (var i = 0; i < partition.SiteCount; i++)
            {
                if (partition.SiteCellCount[i] > 0)
                {
                    stats.SitesInWorld++;
                }

                if (partition.IsInsideMargin(i, WorldScale.IslandOceanMarginMeters))
                {
                    stats.EligibleSites++;
                }
            }

            sw.Restart();
            var landmass = LandmassBuilder.Build(worldSeed, settings, partition);
            stats.LandmassMs = sw.ElapsedMilliseconds;

            var structure = new WorldStructure(
                worldSeed.Value,
                settings.GeneratorVersion,
                landmass.CoastRings,
                landmass.LandSites,
                landmass.IsletCount,
                partition);

            var coast = new CoastQuery(structure.CoastRings, settings.MaxCoastDistanceMeters);
            var model = new CoastHeightModel(settings);

            sw.Restart();
            var field = EvaluateField(coast, model, settings.FieldResolution, out var signedDistance);
            stats.FieldMs = sw.ElapsedMilliseconds;

            FillStats(stats, settings, structure, landmass, field);
            stats.TotalMs = total.ElapsedMilliseconds;

            return new Stage1Result(
                settings, structure, coast, model, field, signedDistance, landmass, stats);
        }

        /// <summary>
        /// Evaluates the whole world lattice.
        /// </summary>
        /// <remarks>
        /// Row by row, because the inside test is a scanline: the crossings of one horizontal line
        /// are computed once and every sample on that row reads them. The predicate that turns those
        /// crossings into land/sea is shared with the point query, so the two paths cannot disagree
        /// about where the coast is.
        /// </remarks>
        public static WorldHeightField EvaluateField(
            CoastQuery coast, CoastHeightModel model, int resolution) =>
            EvaluateField(coast, model, resolution, out _);

        /// <inheritdoc cref="EvaluateField(CoastQuery, CoastHeightModel, int)"/>
        /// <param name="signedDistance">
        /// Receives the signed distance to the coast at every sample, so later stages do not have to
        /// pay for it a second time.
        /// </param>
        public static WorldHeightField EvaluateField(
            CoastQuery coast, CoastHeightModel model, int resolution, out float[] signedDistance)
        {
            var field = WorldHeightField.ForWorld(resolution);
            signedDistance = new float[field.Width * field.Height];
            var crossings = new List<float>(16);

            for (var z = 0; z < field.Height; z++)
            {
                var worldZ = field.WorldZ(z);
                coast.RowCrossings(worldZ, crossings);

                var row = z * field.Width;
                for (var x = 0; x < field.Width; x++)
                {
                    var worldX = field.WorldX(x);
                    var inside = CoastQuery.InsideFromSortedCrossings(crossings, worldX);
                    var distance = coast.NearestDistance(worldX, worldZ);
                    var signed = inside ? distance : -distance;
                    signedDistance[row + x] = signed;
                    field.Heights[row + x] = model.HeightAt(signed);
                }
            }

            return field;
        }

        private static void FillStats(
            TerrainGenStats stats,
            TerrainGenSettings settings,
            WorldStructure structure,
            LandmassBuilder.Result landmass,
            WorldHeightField field)
        {
            var worldArea = WorldScale.WorldExtentXMeters * WorldScale.WorldExtentZMeters;

            stats.LandSiteCount = landmass.LandSites.Count;
            stats.IsletCount = landmass.IsletCount;
            stats.ComponentCount = landmass.ComponentCount;
            stats.TargetLandFraction = landmass.TargetLandFraction;
            stats.RasterLandFraction = landmass.RasterLandFraction;
            stats.EnclosedSeaFilled = landmass.EnclosedSeaFilled;
            stats.LandBudgetShort = landmass.LandBudgetShort;
            stats.RingsFallenBack = landmass.RingsFallenBack;

            for (var i = 0; i < landmass.RingDisplaceAttempts.Count; i++)
            {
                if (landmass.RingDisplaceAttempts[i] > stats.MaxRingDisplaceAttempts)
                {
                    stats.MaxRingDisplaceAttempts = landmass.RingDisplaceAttempts[i];
                }
            }

            stats.PolygonLandFraction = structure.PolygonLandAreaSqm() / worldArea;
            stats.CoastRingCount = structure.CoastRings.Count;
            stats.CoastVertexCount = structure.CoastVertexCount();
            stats.CoastLengthMeters = structure.CoastLengthMeters();
            stats.CoastCrenulation = Crenulation(structure);

            var min = float.PositiveInfinity;
            var max = float.NegativeInfinity;
            var sum = 0.0;
            var landSamples = 0;

            for (var i = 0; i < field.Heights.Length; i++)
            {
                var h = field.Heights[i];
                if (float.IsNaN(h) || float.IsInfinity(h))
                {
                    stats.HasNonFinite = true;
                    continue;
                }

                if (h < min)
                {
                    min = h;
                }

                if (h > max)
                {
                    max = h;
                }

                sum += h;
                if (h > WorldScale.SeaLevelMeters)
                {
                    landSamples++;
                }
            }

            stats.MinHeightMeters = min;
            stats.MaxHeightMeters = max;
            stats.MeanHeightMeters = (float)(sum / field.Heights.Length);
            stats.FieldLandFraction = landSamples / (float)field.Heights.Length;
            stats.FieldLandComponents = CountFieldLandComponents(field);
            stats.FieldHash = field.Hash();

            _ = settings;
        }

        /// <summary>Mainland coastline length over its convex-hull perimeter.</summary>
        private static float Crenulation(WorldStructure structure)
        {
            if (structure.CoastRings.Count == 0)
            {
                return 0f;
            }

            var mainland = structure.CoastRings[0];
            if (mainland == null || mainland.Length < 3)
            {
                return 0f;
            }

            var hull = Geo2.ConvexHull(mainland);
            var hullPerimeter = CoastOutline.Perimeter(hull);
            if (hullPerimeter <= 0.01f)
            {
                return 0f;
            }

            return CoastOutline.Perimeter(mainland) / hullPerimeter;
        }

        /// <summary>
        /// 4-connected components of the field's dry-land mask.
        /// </summary>
        /// <remarks>
        /// Measured on the field rather than on the polygons, because the field is what downstream
        /// systems would see. It should equal one mainland plus the islets; anything else means the
        /// height model and the outline disagree about where land is.
        /// </remarks>
        private static int CountFieldLandComponents(WorldHeightField field)
        {
            var land = new bool[field.Heights.Length];
            for (var i = 0; i < land.Length; i++)
            {
                land[i] = field.Heights[i] > WorldScale.SeaLevelMeters;
            }

            return CoastOutline.LabelComponents(land, field.Width, field.Height, out _);
        }
    }
}

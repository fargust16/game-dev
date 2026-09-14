using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// Fills the overworld Unity Terrain tiles from the final terrain field: heights, surface weights,
    /// trees and ground cover, through <c>TerrainData</c>'s own API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The whole terrain bake after TerrainGen. It creates nothing: the tiles come from
    /// <see cref="OverworldTerrainGrid"/>, the layer and prototype assets from the editor, the relief
    /// from <see cref="OverworldTerrainField"/>. What it owns is the order and the sinks —
    /// <c>SetHeights</c>, <c>SetAlphamaps</c>, <c>SetTreeInstances</c>, <c>SetDetailLayer</c>.
    /// </para>
    /// <para>
    /// <b>Seams are exact by construction.</b> Heights are read off one world lattice whose tile-edge
    /// vertices are shared; alphamap texels are placed at <c>i / (resolution − 1)</c>, so a tile's last
    /// texel and its neighbour's first are the same world point and classify identically; every tree
    /// and detail rule is a function of world position alone.
    /// </para>
    /// </remarks>
    public static class OverworldTerrainBuilder
    {
        /// <summary>Stream id for grove noise.</summary>
        public const string GroveStream = "terrain.vegetation.groves";

        /// <summary>Stream id for species patches.</summary>
        public const string SpeciesStream = "terrain.vegetation.species";

        /// <summary>Stream id for per-cell tree candidates.</summary>
        public const string TreeStream = "terrain.vegetation.trees";

        /// <summary>Stream id for meadow patchiness.</summary>
        public const string GrassStream = "terrain.vegetation.grass";

        public sealed class Report
        {
            public long HeightMs;
            public long DerivedMs;
            public long AlphamapMs;
            public long TreeMs;
            public long DetailMs;
            public int Trees;
            public long DetailInstances;
            public int ClampedHeightSamples;
        }

        /// <summary>
        /// Runs every stage over every tile. <paramref name="tiles"/> is row-major <c>[x + z · tilesX]</c>,
        /// as <see cref="OverworldTerrainGrid.Create"/> returns it.
        /// </summary>
        /// <param name="speciesPrototype">
        /// Tree prototype index per <see cref="TerrainTreeSpecies"/>, or −1 where the species has none.
        /// </param>
        /// <param name="grassPrototype">Detail prototype index for ground cover, or −1 for none.</param>
        public static Report Build(
            IReadOnlyList<UnityEngine.Terrain> tiles,
            OverworldTerrainField field,
            int seed,
            TerrainSurfaceRules surfaceRules,
            TerrainVegetationRules vegetationRules,
            int[] speciesPrototype,
            int grassPrototype)
        {
            var report = new Report { ClampedHeightSamples = field.ClampedSamples };
            var spec = field.Spec;
            var watch = Stopwatch.StartNew();

            ForEachTile(tiles, spec, (tile, tx, tz) => WriteHeights(tile, field, tx, tz));
            report.HeightMs = Lap(watch);

            // Forces slope, convexity and drainage once, before any tile reads them.
            field.SlopeDegreesAt(0f, 0f);
            report.DerivedMs = Lap(watch);

            ForEachTile(tiles, spec, (tile, tx, tz) => PaintSurface(tile, field, tx, tz, seed, surfaceRules));
            report.AlphamapMs = Lap(watch);

            ForEachTile(tiles, spec, (tile, tx, tz) =>
                report.Trees += PlaceTrees(tile, field, tx, tz, seed, surfaceRules, vegetationRules, speciesPrototype));
            report.TreeMs = Lap(watch);

            ForEachTile(tiles, spec, (tile, tx, tz) =>
                report.DetailInstances += PlaceGrass(tile, field, tx, tz, seed, surfaceRules, vegetationRules, grassPrototype));
            report.DetailMs = Lap(watch);

            return report;
        }

        // ------------------------------------------------------------------------------------------
        // Heights
        // ------------------------------------------------------------------------------------------

        public static void WriteHeights(UnityEngine.Terrain tile, OverworldTerrainField field, int tileX, int tileZ)
        {
            var data = tile.terrainData;
            data.SetHeightsDelayLOD(0, 0, field.TileHeights01(tileX, tileZ));
            data.SyncHeightmap();
        }

        // ------------------------------------------------------------------------------------------
        // Surface
        // ------------------------------------------------------------------------------------------

        public static void PaintSurface(
            UnityEngine.Terrain tile, OverworldTerrainField field, int tileX, int tileZ, int seed, TerrainSurfaceRules rules)
        {
            var data = tile.terrainData;
            var res = data.alphamapResolution;
            var layers = data.terrainLayers != null ? data.terrainLayers.Length : 0;
            if (layers < TerrainSurfaceClassifier.LayerCount)
            {
                UnityEngine.Debug.LogError(
                    $"[Terrain] Tile {tileX},{tileZ} has {layers} terrain layer(s); the surface needs " +
                    $"{TerrainSurfaceClassifier.LayerCount}. Alphamaps not written.");
                return;
            }

            var keys = SurfaceKeys.For(seed);
            var tileMeters = field.Spec.TileMeters;
            var originX = tileX * tileMeters;
            var originZ = tileZ * tileMeters;
            var weights = new float[TerrainSurfaceClassifier.LayerCount];
            var map = new float[res, res, layers];

            for (var j = 0; j < res; j++)
            {
                var worldZ = originZ + j * tileMeters / (res - 1);
                for (var i = 0; i < res; i++)
                {
                    var worldX = originX + i * tileMeters / (res - 1);
                    Classify(field, keys, worldX, worldZ, rules, weights);
                    for (var k = 0; k < TerrainSurfaceClassifier.LayerCount; k++)
                    {
                        map[j, i, k] = weights[k];
                    }
                }
            }

            data.SetAlphamaps(0, 0, map);
        }

        /// <summary>Surface weights at a world point, the same evaluation every stage uses.</summary>
        public static void Classify(
            OverworldTerrainField field, SurfaceKeys keys, float worldX, float worldZ,
            TerrainSurfaceRules rules, float[] weights)
        {
            var sample = new TerrainSurfaceSample
            {
                HeightMeters = field.HeightAt(worldX, worldZ),
                SlopeDegrees = field.SlopeDegreesAt(worldX, worldZ),
                ConvexityMeters = field.ConvexityAt(worldX, worldZ),
                DrainageAreaSqm = field.DrainageAreaAt(worldX, worldZ),
                MassifUpliftMeters = field.UpliftAt(worldX, worldZ),
                ShoreNoise = TerrainHash.ValueNoise(keys.Shore, worldX, worldZ, rules.ShoreNoiseWavelengthMeters),
                RockNoise = TerrainHash.ValueNoise(keys.Rock, worldX, worldZ, rules.RockHeightNoiseWavelengthMeters),
            };
            TerrainSurfaceClassifier.Classify(sample, rules, weights);
        }

        public readonly struct SurfaceKeys
        {
            public readonly ulong Shore;
            public readonly ulong Rock;

            private SurfaceKeys(ulong shore, ulong rock)
            {
                Shore = shore;
                Rock = rock;
            }

            public static SurfaceKeys For(int seed) => new SurfaceKeys(
                TerrainHash.StreamKey(seed, TerrainSurfaceClassifier.ShoreNoiseStream),
                TerrainHash.StreamKey(seed, TerrainSurfaceClassifier.RockNoiseStream));
        }

        // ------------------------------------------------------------------------------------------
        // Trees
        // ------------------------------------------------------------------------------------------

        /// <returns>Trees placed on this tile.</returns>
        public static int PlaceTrees(
            UnityEngine.Terrain tile, OverworldTerrainField field, int tileX, int tileZ, int seed,
            TerrainSurfaceRules surfaceRules, TerrainVegetationRules rules, int[] speciesPrototype)
        {
            var data = tile.terrainData;
            var prototypes = data.treePrototypes != null ? data.treePrototypes.Length : 0;
            if (prototypes == 0 || speciesPrototype == null)
            {
                data.SetTreeInstances(System.Array.Empty<TreeInstance>(), false);
                return 0;
            }

            var surfaceKeys = SurfaceKeys.For(seed);
            var groveKey = TerrainHash.StreamKey(seed, GroveStream);
            var speciesKey = TerrainHash.StreamKey(seed, SpeciesStream);
            var treeKey = TerrainHash.StreamKey(seed, TreeStream);

            var spec = field.Spec;
            var tileMeters = spec.TileMeters;
            var originX = tileX * tileMeters;
            var originZ = tileZ * tileMeters;
            var cell = rules.TreeCellMeters;
            var weights = new float[TerrainSurfaceClassifier.LayerCount];
            var trees = new List<TreeInstance>();

            var cx0 = Mathf.FloorToInt(originX / cell);
            var cx1 = Mathf.FloorToInt((originX + tileMeters) / cell);
            var cz0 = Mathf.FloorToInt(originZ / cell);
            var cz1 = Mathf.FloorToInt((originZ + tileMeters) / cell);

            for (var cz = cz0; cz <= cz1; cz++)
            {
                for (var cx = cx0; cx <= cx1; cx++)
                {
                    var worldX = (cx + TerrainHash.Unit(treeKey, cx, cz, 0)) * cell;
                    var worldZ = (cz + TerrainHash.Unit(treeKey, cx, cz, 1)) * cell;

                    // Each world cell belongs to exactly one tile: half-open on the far edges, closed
                    // on the world's outer edge.
                    if (!OwnsPoint(spec, tileX, tileZ, worldX, worldZ))
                    {
                        continue;
                    }

                    var chance = TreeChance(field, surfaceKeys, groveKey, worldX, worldZ, surfaceRules, rules, weights);
                    if (chance <= 0f || TerrainHash.Unit(treeKey, cx, cz, 2) >= chance)
                    {
                        continue;
                    }

                    var index = speciesPrototype[(int)PickSpecies(field, speciesKey, treeKey, cx, cz, worldX, worldZ, rules)];
                    if (index < 0 || index >= prototypes)
                    {
                        continue;
                    }

                    var scale = Mathf.Lerp(rules.TreeScaleMin, rules.TreeScaleMax, TerrainHash.Unit(treeKey, cx, cz, 3));
                    trees.Add(new TreeInstance
                    {
                        position = new Vector3(
                            (worldX - originX) / tileMeters,
                            Mathf.Clamp01(field.HeightAt(worldX, worldZ) / spec.HeightBudgetMeters),
                            (worldZ - originZ) / tileMeters),
                        prototypeIndex = index,
                        widthScale = scale,
                        heightScale = scale,
                        rotation = TerrainHash.Unit(treeKey, cx, cz, 4) * Mathf.PI * 2f,
                        color = Color.white,
                        lightmapColor = Color.white,
                    });
                }
            }

            data.SetTreeInstances(trees.ToArray(), true);
            return trees.Count;
        }

        /// <summary>Probability that a tree candidate at a world point becomes a tree.</summary>
        public static float TreeChance(
            OverworldTerrainField field, SurfaceKeys surfaceKeys, ulong groveKey, float worldX, float worldZ,
            TerrainSurfaceRules surfaceRules, TerrainVegetationRules rules, float[] weights)
        {
            var h = field.HeightAt(worldX, worldZ);
            var gate = TerrainSurfaceClassifier.SmoothStep(rules.VegetationFloorStartMeters, rules.VegetationFloorFullMeters, h);
            if (gate <= 0f)
            {
                return 0f;
            }

            var slope = field.SlopeDegreesAt(worldX, worldZ);
            if (slope >= rules.TreeMaxSlopeDegrees)
            {
                return 0f;
            }

            Classify(field, surfaceKeys, worldX, worldZ, surfaceRules, weights);
            var ground = weights[(int)TerrainSurfaceLayer.Grass] + 0.5f * weights[(int)TerrainSurfaceLayer.Soil];
            if (ground <= 0f)
            {
                return 0f;
            }

            var drainage = TerrainSurfaceClassifier.SmoothStep(
                Mathf.Log10(rules.DrainageGroveStartSqm), Mathf.Log10(rules.DrainageGroveFullSqm),
                Mathf.Log10(Mathf.Max(1f, field.DrainageAreaAt(worldX, worldZ))));
            var grove = TerrainHash.ValueNoise(groveKey, worldX, worldZ, rules.GroveWavelengthMeters)
                        + rules.DrainageGroveBoost * drainage;
            var groveMask = TerrainSurfaceClassifier.SmoothStep(rules.GroveThresholdStart, rules.GroveThresholdFull, grove);
            var slopeFade = 1f - TerrainSurfaceClassifier.SmoothStep(rules.TreeSlopeFadeStartDegrees, rules.TreeMaxSlopeDegrees, slope);

            return rules.TreeMaxProbability * gate * ground * groveMask * slopeFade;
        }

        public static TerrainTreeSpecies PickSpecies(
            OverworldTerrainField field, ulong speciesKey, ulong treeKey, int cx, int cz,
            float worldX, float worldZ, TerrainVegetationRules rules)
        {
            var massif = TerrainSurfaceClassifier.SmoothStep(
                rules.PineUpliftStartMeters, rules.PineUpliftFullMeters, field.UpliftAt(worldX, worldZ));
            var patch = TerrainHash.ValueNoise(speciesKey, worldX, worldZ, rules.SpeciesWavelengthMeters);
            var pine = TerrainHash.Unit(treeKey, cx, cz, 5) < massif || patch > rules.LowlandPineThreshold;
            var variant = TerrainHash.Unit(treeKey, cx, cz, 6);
            if (pine)
            {
                return variant < rules.PinePaleShare ? TerrainTreeSpecies.PinePale : TerrainTreeSpecies.Pine;
            }

            if (variant < rules.OrchardShare)
            {
                return TerrainTreeSpecies.Orchard;
            }

            return variant < rules.OrchardShare + rules.FruitDeepShare ? TerrainTreeSpecies.FruitDeep : TerrainTreeSpecies.Fruit;
        }

        // ------------------------------------------------------------------------------------------
        // Ground cover
        // ------------------------------------------------------------------------------------------

        /// <returns>Detail instances written on this tile.</returns>
        public static long PlaceGrass(
            UnityEngine.Terrain tile, OverworldTerrainField field, int tileX, int tileZ, int seed,
            TerrainSurfaceRules surfaceRules, TerrainVegetationRules rules, int grassPrototype)
        {
            var data = tile.terrainData;
            var prototypes = data.detailPrototypes != null ? data.detailPrototypes.Length : 0;
            if (grassPrototype < 0 || grassPrototype >= prototypes)
            {
                return 0;
            }

            var surfaceKeys = SurfaceKeys.For(seed);
            var grassKey = TerrainHash.StreamKey(seed, GrassStream);
            var res = data.detailResolution;
            var tileMeters = field.Spec.TileMeters;
            var originX = tileX * tileMeters;
            var originZ = tileZ * tileMeters;
            var cellMeters = tileMeters / res;
            var weights = new float[TerrainSurfaceClassifier.LayerCount];
            var layer = new int[res, res];
            long total = 0;

            for (var j = 0; j < res; j++)
            {
                var worldZ = originZ + (j + 0.5f) * cellMeters;
                for (var i = 0; i < res; i++)
                {
                    var worldX = originX + (i + 0.5f) * cellMeters;
                    var count = GrassCount(field, surfaceKeys, grassKey, worldX, worldZ, surfaceRules, rules, weights);
                    layer[j, i] = count;
                    total += count;
                }
            }

            data.SetDetailLayer(0, 0, grassPrototype, layer);
            return total;
        }

        public static int GrassCount(
            OverworldTerrainField field, SurfaceKeys surfaceKeys, ulong grassKey, float worldX, float worldZ,
            TerrainSurfaceRules surfaceRules, TerrainVegetationRules rules, float[] weights)
        {
            var h = field.HeightAt(worldX, worldZ);
            var gate = TerrainSurfaceClassifier.SmoothStep(rules.VegetationFloorStartMeters, rules.VegetationFloorFullMeters, h);
            if (gate <= 0f || field.SlopeDegreesAt(worldX, worldZ) > rules.GrassMaxSlopeDegrees)
            {
                return 0;
            }

            Classify(field, surfaceKeys, worldX, worldZ, surfaceRules, weights);
            var grass = weights[(int)TerrainSurfaceLayer.Grass];
            var patch = Mathf.Lerp(rules.GrassMinShare, 1f,
                TerrainHash.ValueNoise(grassKey, worldX, worldZ, rules.GrassNoiseWavelengthMeters));
            return Mathf.Clamp(Mathf.RoundToInt(rules.GrassMaxInstancesPerCell * gate * grass * patch), 0, 255);
        }

        // ------------------------------------------------------------------------------------------

        public static bool OwnsPoint(OverworldTerrainGrid.Spec spec, int tileX, int tileZ, float worldX, float worldZ)
        {
            var minX = tileX * spec.TileMeters;
            var minZ = tileZ * spec.TileMeters;
            var lastX = tileX == spec.TileCountX - 1;
            var lastZ = tileZ == spec.TileCountZ - 1;
            var inX = worldX >= minX && (worldX < minX + spec.TileMeters || (lastX && worldX <= minX + spec.TileMeters));
            var inZ = worldZ >= minZ && (worldZ < minZ + spec.TileMeters || (lastZ && worldZ <= minZ + spec.TileMeters));
            return inX && inZ;
        }

        private static void ForEachTile(
            IReadOnlyList<UnityEngine.Terrain> tiles, OverworldTerrainGrid.Spec spec,
            System.Action<UnityEngine.Terrain, int, int> action)
        {
            for (var z = 0; z < spec.TileCountZ; z++)
            {
                for (var x = 0; x < spec.TileCountX; x++)
                {
                    var tile = tiles[x + z * spec.TileCountX];
                    if (tile != null && tile.terrainData != null)
                    {
                        action(tile, x, z);
                    }
                }
            }
        }

        private static long Lap(Stopwatch watch)
        {
            var ms = watch.ElapsedMilliseconds;
            watch.Restart();
            return ms;
        }
    }
}

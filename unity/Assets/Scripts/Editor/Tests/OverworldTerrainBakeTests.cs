using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GameDev.EditorTools;
using GameDev.World.TerrainGen;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameDev.World.Terrain.Tests
{
    /// <summary>
    /// The terrain pipeline contract: TerrainGen's relief reaches <c>TerrainData</c> unchanged, and the
    /// surface, trees and ground cover derived from it are deterministic, valid and world-space.
    /// </summary>
    /// <remarks>
    /// Most tests run on a small synthetic world through the same builder the production bake uses, so
    /// they are fast and independent of the seed. The TerrainGen tests use the canonical seed and pin the
    /// field the terrain pipeline was built on. One test runs the full production bake.
    /// </remarks>
    public sealed class OverworldTerrainBakeTests
    {
        private const int CanonicalSeed = 110201;

        /// <summary>
        /// MD5 (first six bytes) of the authoritative field on a 256² grid over the world. Pins that
        /// TerrainGen's relief did not move.
        /// </summary>
        /// <remarks>
        /// Measured from the production authority, not authored. It was <c>7614bd2ed819</c> while the
        /// authority was Stage 2 alone (<c>H2 = H1 + U2</c>); Stage 4A becoming the authority moved it to
        /// the value below (<c>H4A = H1 + U2 + R4A</c>). A change here is only ever a recorded
        /// measurement after a deliberate generator change, never an edit to make a test pass.
        /// </remarks>
        private const string CanonicalAuthorityHash = "a09953a3bdb3";

        /// <summary>
        /// FNV-1a of the H4A lattice for the canonical seed, as <see cref="TerrainGenIslandReliefDump"/>
        /// recorded it while Stage 4A was still dump-first.
        /// </summary>
        /// <remarks>
        /// The production authority builds Stage 4A from its own Stage 2A plates and massifs; the dump
        /// builds it from Stage 2B's. Both are the same <c>MacroPlates.Build</c> and the same
        /// <c>MountainMassif.Build</c> over the same accepted axes, so the two fields must be one field.
        /// This pins that, so the production path can never quietly drift from the layer that was
        /// accepted on the dump.
        /// </remarks>
        private const string CanonicalDumpH4AHash = "e2bad571f4af76c7";

        /// <summary>
        /// Unity stores heights in 16 bits over half the normalised range (a 2.14 cm step on a 700 m tile)
        /// and rounds, so a written height is within half a step of its source.
        /// </summary>
        private static readonly float HeightQuantisation = WorldScale.TerrainHeightBudgetMeters / 32766f * 0.5f + 1e-4f;

        /// <summary>Unity stores splat weights at 8 bits per channel.</summary>
        private const float WeightQuantisation = 1.5f / 255f;

        private static TerrainAuthorityField s_authority;

        private readonly List<Object> m_spawned = new List<Object>();
        private readonly List<GameObject> m_hidden = new List<GameObject>();

        private static TerrainAuthorityField Authority =>
            s_authority ??= new TerrainAuthorityField(CanonicalSeed);

        [SetUp]
        public void HideSceneTerrain()
        {
            foreach (var terrain in Object.FindObjectsByType<UnityEngine.Terrain>())
            {
                var root = terrain.transform.root.gameObject;
                if (root.activeSelf && !m_hidden.Contains(root))
                {
                    root.SetActive(false);
                    m_hidden.Add(root);
                }
            }
        }

        [TearDown]
        public void Restore()
        {
            foreach (var o in m_spawned)
            {
                if (o != null)
                {
                    Object.DestroyImmediate(o);
                }
            }

            m_spawned.Clear();
            foreach (var go in m_hidden)
            {
                if (go != null)
                {
                    go.SetActive(true);
                }
            }

            m_hidden.Clear();
        }

        // ------------------------------------------------------------------------------------------
        // TerrainGen authority
        // ------------------------------------------------------------------------------------------

        [Test]
        public void AuthorityField_ReproducesTheCanonicalField()
        {
            const int grid = 256;
            var world = WorldScale.OverworldTileMeters * Mathf.Max(WorldScale.OverworldTileCountX, WorldScale.OverworldTileCountZ);
            var values = new float[grid * grid];
            for (var z = 0; z < grid; z++)
            {
                for (var x = 0; x < grid; x++)
                {
                    values[z * grid + x] = Authority.HeightAt(world * x / (grid - 1f), world * z / (grid - 1f));
                }
            }

            Assert.AreEqual(CanonicalAuthorityHash, Hash(values), "TerrainGen's authoritative relief moved");
        }

        [Test]
        public void AuthorityField_IsTheSameH4AFieldStage4AWasAcceptedOn()
        {
            var lattice = WorldHeightField.ForWorld(TerrainGenSettings.Default.FieldResolution);
            for (var z = 0; z < lattice.Height; z++)
            {
                var worldZ = lattice.WorldZ(z);
                var row = z * lattice.Width;
                for (var x = 0; x < lattice.Width; x++)
                {
                    lattice.Heights[row + x] = Authority.HeightAt(lattice.WorldX(x), worldZ);
                }
            }

            Assert.AreEqual(
                CanonicalDumpH4AHash, lattice.Hash(),
                "the production authority is no longer the H4A field Stage 4A was accepted on");
        }

        [Test]
        public void AuthorityField_SampleEqualsHeightAtExactly()
        {
            var random = new System.Random(7);
            for (var i = 0; i < 500; i++)
            {
                var x = (float)random.NextDouble() * 2800f;
                var z = (float)random.NextDouble() * 2800f;
                Assert.AreEqual(Authority.HeightAt(x, z), Authority.Sample(x, z, out _, out _), 0f, $"at {x},{z}");
            }
        }

        [Test]
        public void AuthorityField_IsDeterministic_AndIslandOnlyHasNoUplift()
        {
            var again = new TerrainAuthorityField(CanonicalSeed);
            var island = new TerrainAuthorityField(CanonicalSeed, includeMassifs: false);
            Assert.Greater(Authority.MassifCount, 0, "the canonical seed has at least one massif");

            var random = new System.Random(11);
            for (var i = 0; i < 300; i++)
            {
                var x = (float)random.NextDouble() * 2800f;
                var z = (float)random.NextDouble() * 2800f;
                Assert.AreEqual(Authority.HeightAt(x, z), again.HeightAt(x, z), 0f);
                island.Sample(x, z, out var uplift, out _);
                Assert.AreEqual(0f, uplift, 0f, "an island-only field carries no massif uplift");
            }
        }

        // ------------------------------------------------------------------------------------------
        // Height, seams, bounds
        // ------------------------------------------------------------------------------------------

        [Test]
        public void TileHeights_MatchTheFieldWithinQuantisation_AndSeamsAreExact()
        {
            var spec = SmallSpec();
            var field = OverworldTerrainField.FromSamples(spec, Island);
            var tiles = Grid(spec);
            for (var z = 0; z < spec.TileCountZ; z++)
            {
                for (var x = 0; x < spec.TileCountX; x++)
                {
                    OverworldTerrainBuilder.WriteHeights(tiles[x + z * spec.TileCountX], field, x, z);
                }
            }

            var res = spec.HeightmapResolution;
            var step = spec.TileMeters / (res - 1);
            var read = new float[tiles.Length][,];
            for (var t = 0; t < tiles.Length; t++)
            {
                read[t] = tiles[t].terrainData.GetHeights(0, 0, res, res);
            }

            for (var tz = 0; tz < spec.TileCountZ; tz++)
            {
                for (var tx = 0; tx < spec.TileCountX; tx++)
                {
                    var h = read[tx + tz * spec.TileCountX];
                    for (var j = 0; j < res; j += 4)
                    {
                        for (var i = 0; i < res; i += 4)
                        {
                            var expected = Island(tx * spec.TileMeters + i * step, tz * spec.TileMeters + j * step, out _, out _);
                            Assert.AreEqual(expected, h[j, i] * spec.HeightBudgetMeters, HeightQuantisation);
                        }
                    }
                }
            }

            for (var k = 0; k < res; k++)
            {
                Assert.AreEqual(read[0][k, res - 1], read[1][k, 0], 0f, "seam 0,0 | 1,0");
                Assert.AreEqual(read[2][k, res - 1], read[3][k, 0], 0f, "seam 0,1 | 1,1");
                Assert.AreEqual(read[0][res - 1, k], read[2][0, k], 0f, "seam 0,0 | 0,1");
                Assert.AreEqual(read[1][res - 1, k], read[3][0, k], 0f, "seam 1,0 | 1,1");
            }
        }

        [Test]
        public void Grid_HasCorrectWorldBounds()
        {
            var spec = SmallSpec();
            var tiles = Grid(spec);
            for (var z = 0; z < spec.TileCountZ; z++)
            {
                for (var x = 0; x < spec.TileCountX; x++)
                {
                    var tile = tiles[x + z * spec.TileCountX];
                    Assert.AreEqual(new Vector3(x * spec.TileMeters, 0f, z * spec.TileMeters), tile.transform.position);
                    Assert.AreEqual(spec.TileSize, tile.terrainData.size);
                    Assert.AreEqual(spec.HeightmapResolution, tile.terrainData.heightmapResolution);
                }
            }
        }

        // ------------------------------------------------------------------------------------------
        // Surface
        // ------------------------------------------------------------------------------------------

        [Test]
        public void Classifier_WeightsSumToOne_AndStayInRange()
        {
            var rules = TerrainSurfaceRules.Default;
            var weights = new float[TerrainSurfaceClassifier.LayerCount];
            var random = new System.Random(3);
            for (var i = 0; i < 20000; i++)
            {
                var sample = new TerrainSurfaceSample
                {
                    HeightMeters = (float)random.NextDouble() * 400f,
                    SlopeDegrees = (float)random.NextDouble() * 80f,
                    ConvexityMeters = (float)(random.NextDouble() * 20 - 10),
                    DrainageAreaSqm = (float)random.NextDouble() * 1e6f,
                    MassifUpliftMeters = (float)random.NextDouble() * 300f,
                    ShoreNoise = (float)random.NextDouble(),
                    RockNoise = (float)random.NextDouble(),
                };
                TerrainSurfaceClassifier.Classify(sample, rules, weights);
                var sum = 0f;
                foreach (var w in weights)
                {
                    Assert.That(w, Is.InRange(0f, 1f));
                    sum += w;
                }

                Assert.AreEqual(1f, sum, 1e-4f);
            }
        }

        [Test]
        public void SeabedRock_IsAllowedUnderwater_AndSuppressedOnCoastalLand()
        {
            var rules = TerrainSurfaceRules.Default;
            var weights = new float[TerrainSurfaceClassifier.LayerCount];

            // A coastal slope far too gentle to be bare rock on land.
            float RockAt(float height)
            {
                TerrainSurfaceClassifier.Classify(new TerrainSurfaceSample
                {
                    HeightMeters = height,
                    SlopeDegrees = 10f,
                    ShoreNoise = 0.5f,
                    RockNoise = 0.5f,
                }, rules, weights);
                return weights[(int)TerrainSurfaceLayer.Rock];
            }

            Assert.Greater(RockAt(10f), 0.99f, "sloped seabed is rock");
            Assert.Greater(RockAt(22f), 0.99f, "rock is allowed at 22 m");
            Assert.AreEqual(0f, RockAt(24f), 1e-5f, "rock is suppressed at the 24 m waterline");
            for (var h = 24.1f; h < 40f; h += 0.5f)
            {
                Assert.AreEqual(0f, RockAt(h), 1e-5f, $"coastal land at {h} m receives no rock");
            }
        }

        [Test]
        public void Slope_IsTrueWorldSlope_WithoutAmplification()
        {
            var spec = SmallSpec();
            var grade = Mathf.Tan(10f * Mathf.Deg2Rad);
            var field = OverworldTerrainField.FromSamples(spec, (float x, float z, out float up, out float sd) =>
            {
                up = 0f;
                sd = 0f;
                return 30f + grade * x;
            });

            for (var i = 1; i < 10; i++)
            {
                Assert.AreEqual(10f, field.SlopeDegreesAt(i * 70f, 300f), 0.05f, "a 10 degree plane reads 10 degrees");
            }
        }

        [Test]
        public void Alphamaps_AreDeterministic_ValidAndSeamless()
        {
            var spec = SmallSpec();
            var first = Surface(spec);
            var second = Surface(spec);

            var res = spec.AlphamapResolution;
            for (var t = 0; t < first.Length; t++)
            {
                Assert.AreEqual(TerrainSurfaceClassifier.LayerCount, first[t].GetLength(2), "five layers");
                for (var j = 0; j < res; j++)
                {
                    for (var i = 0; i < res; i++)
                    {
                        var sum = 0f;
                        for (var k = 0; k < TerrainSurfaceClassifier.LayerCount; k++)
                        {
                            Assert.AreEqual(first[t][j, i, k], second[t][j, i, k], 0f, "alphamaps are deterministic");
                            sum += first[t][j, i, k];
                        }

                        Assert.AreEqual(1f, sum, WeightQuantisation);
                    }
                }
            }

            for (var k = 0; k < res; k++)
            {
                for (var l = 0; l < TerrainSurfaceClassifier.LayerCount; l++)
                {
                    Assert.AreEqual(first[0][k, res - 1, l], first[1][k, 0, l], WeightQuantisation, "alphamap seam 0,0 | 1,0");
                    Assert.AreEqual(first[0][res - 1, k, l], first[2][0, k, l], WeightQuantisation, "alphamap seam 0,0 | 0,1");
                }
            }
        }

        // ------------------------------------------------------------------------------------------
        // Trees and ground cover
        // ------------------------------------------------------------------------------------------

        [Test]
        public void Trees_AreDeterministic_ValidAndStandOnTheTerrain()
        {
            var spec = SmallSpec();
            var field = OverworldTerrainField.FromSamples(spec, Island);
            var firstTiles = Vegetated(spec, field, out var firstTrees, out _);
            var secondTiles = Vegetated(spec, field, out var secondTrees, out _);
            Assert.Greater(firstTrees, 0, "the synthetic island grows trees");
            Assert.AreEqual(firstTrees, secondTrees);

            for (var t = 0; t < firstTiles.Length; t++)
            {
                var a = firstTiles[t].terrainData.treeInstances;
                var b = secondTiles[t].terrainData.treeInstances;
                Assert.AreEqual(a.Length, b.Length);
                var prototypes = firstTiles[t].terrainData.treePrototypes.Length;
                for (var i = 0; i < a.Length; i++)
                {
                    Assert.AreEqual(a[i].position, b[i].position, "tree placement is deterministic");
                    Assert.AreEqual(a[i].prototypeIndex, b[i].prototypeIndex);
                    Assert.That(a[i].prototypeIndex, Is.InRange(0, prototypes - 1));
                    Assert.That(a[i].position.x, Is.InRange(0f, 1f));
                    Assert.That(a[i].position.z, Is.InRange(0f, 1f));

                    var tile = firstTiles[t];
                    var world = tile.transform.position + Vector3.Scale(a[i].position, tile.terrainData.size);
                    var ground = tile.SampleHeight(world);
                    Assert.AreEqual(ground, a[i].position.y * tile.terrainData.size.y, 0.05f, "a tree stands on the final terrain");
                    Assert.Greater(ground, TerrainVegetationRules.Default.VegetationFloorStartMeters, "no tree below the vegetation floor");
                }
            }
        }

        [Test]
        public void Grass_IsDeterministic_AndOnlyAboveTheVegetationFloor()
        {
            var spec = SmallSpec();
            var field = OverworldTerrainField.FromSamples(spec, Island);
            var first = Vegetated(spec, field, out _, out var firstCount);
            var second = Vegetated(spec, field, out _, out var secondCount);
            Assert.Greater(firstCount, 0L);
            Assert.AreEqual(firstCount, secondCount);

            for (var t = 0; t < first.Length; t++)
            {
                var data = first[t].terrainData;
                Assert.AreEqual(1, data.detailPrototypes.Length);
                var res = data.detailResolution;
                var a = data.GetDetailLayer(0, 0, res, res, 0);
                var b = second[t].terrainData.GetDetailLayer(0, 0, res, res, 0);
                var cell = spec.TileMeters / res;
                for (var j = 0; j < res; j += 3)
                {
                    for (var i = 0; i < res; i += 3)
                    {
                        Assert.AreEqual(a[j, i], b[j, i], "ground cover is deterministic");
                        if (a[j, i] > 0)
                        {
                            var tx = t % spec.TileCountX;
                            var tz = t / spec.TileCountX;
                            var h = field.HeightAt(tx * spec.TileMeters + (i + 0.5f) * cell, tz * spec.TileMeters + (j + 0.5f) * cell);
                            Assert.Greater(h, TerrainVegetationRules.Default.VegetationFloorStartMeters);
                        }
                    }
                }
            }
        }

        // ------------------------------------------------------------------------------------------
        // Hydrology boundary
        // ------------------------------------------------------------------------------------------

        [Test]
        public void Carve_LowersOnlyTheRequestedVertices_AndRefreshesSlope()
        {
            var spec = SmallSpec();
            var field = OverworldTerrainField.FromSamples(spec, Island);
            var before = (float[])field.Heights.Clone();

            // Beside the carved column: its central difference reads the carved vertex.
            var probeX = (field.Width / 2 + 1) * field.StepMeters;
            var slopeBefore = field.SlopeDegreesAt(probeX, 300f);

            var moved = field.ApplyCarve((x, z) => x == field.Width / 2 ? 5f : 0f);

            Assert.AreEqual(field.Depth, moved);
            for (var z = 0; z < field.Depth; z++)
            {
                for (var x = 0; x < field.Width; x++)
                {
                    var expected = before[z * field.Width + x] - (x == field.Width / 2 ? 5f : 0f);
                    Assert.AreEqual(expected, field.Heights[z * field.Width + x], 0f);
                }
            }

            Assert.AreNotEqual(slopeBefore, field.SlopeDegreesAt(probeX, 300f), "derived quantities follow the carve");
        }

        // ------------------------------------------------------------------------------------------
        // Production bake
        // ------------------------------------------------------------------------------------------

        [Test]
        public void ProductionBake_CanonicalSeed_ProducesAValidNativeTerrain()
        {
            // The bake persists the production tile assets; with a baked world open, that would pull the
            // data out from under its tiles.
            if (m_hidden.Count > 0)
            {
                Assert.Ignore("A scene with terrain is open; run this test with no overworld scene loaded.");
            }

            try
            {
                var tiles = OverworldTerrainBake.Bake(CanonicalSeed, TerrainBakeFlags.Full);
                Assert.AreEqual(WorldScale.OverworldTileCountX * WorldScale.OverworldTileCountZ, tiles.Length);

                var random = new System.Random(5);
                foreach (var tile in tiles)
                {
                    var data = tile.terrainData;
                    var marker = tile.GetComponent<OverworldTerrainTile>();
                    Assert.IsNotNull(marker);

                    // Only Unity and project components on a terrain tile: the pipeline needs nothing else.
                    foreach (var component in tile.GetComponents<Component>())
                    {
                        var assembly = component.GetType().Assembly.GetName().Name;
                        Assert.That(assembly.StartsWith("UnityEngine", StringComparison.Ordinal) || assembly.StartsWith("Assembly-CSharp", StringComparison.Ordinal),
                            $"unexpected component {component.GetType().FullName} from {assembly}");
                    }

                    Assert.AreEqual(TerrainSurfaceClassifier.LayerCount, data.terrainLayers.Length);
                    Assert.Greater(data.treeInstances.Length + data.GetDetailLayer(0, 0, data.detailResolution, data.detailResolution, 0).Length, 0);
                    Assert.IsNotNull(tile.materialTemplate, "terrain material assigned");

                    var res = data.heightmapResolution;
                    var heights = data.GetHeights(0, 0, res, res);
                    var step = data.size.x / (res - 1);
                    for (var n = 0; n < 400; n++)
                    {
                        var i = random.Next(res);
                        var j = random.Next(res);
                        var expected = Authority.HeightAt(marker.TileX * data.size.x + i * step, marker.TileZ * data.size.z + j * step);
                        Assert.AreEqual(Mathf.Clamp(expected, 0f, data.size.y), heights[j, i] * data.size.y, HeightQuantisation);
                    }
                }

                var report = OverworldTerrainBake.LastReport;
                Assert.Greater(report.Trees, 0, "trees populated");
                Assert.Greater(report.DetailInstances, 0L, "ground cover populated");
                Assert.AreEqual(0, report.ClampedHeightSamples);
            }
            finally
            {
                var root = GameObject.Find(OverworldTerrainAccess.TerrainsRootName);
                if (root != null)
                {
                    m_spawned.Add(root);
                }
            }
        }

        // ------------------------------------------------------------------------------------------
        // Fixtures
        // ------------------------------------------------------------------------------------------

        /// <summary>2×2 tiles of 400 m, a low-resolution copy of the production layout.</summary>
        private static OverworldTerrainGrid.Spec SmallSpec() =>
            new OverworldTerrainGrid.Spec(2, 2, 400f, WorldScale.TerrainHeightBudgetMeters, 65, 64);

        /// <summary>A gentle cone island (about 7°) with a steep massif on its crown.</summary>
        private static float Island(float x, float z, out float uplift, out float coast)
        {
            var d = Mathf.Sqrt((x - 400f) * (x - 400f) + (z - 400f) * (z - 400f));
            var island = Mathf.Max(2f, 60f - 0.12f * d);
            var core = Mathf.Max(0f, 1f - d / 120f);
            uplift = 220f * core * core;
            coast = 300f - d;
            return island + uplift;
        }

        private UnityEngine.Terrain[] Grid(OverworldTerrainGrid.Spec spec)
        {
            var root = new GameObject("TerrainBakeTests");
            m_spawned.Add(root);
            var tiles = OverworldTerrainGrid.Create(root.transform, spec, OverworldTerrainLayers.EnsureCanonicalLayers());
            foreach (var tile in tiles)
            {
                m_spawned.Add(tile.terrainData);
            }

            return tiles;
        }

        private float[][,,] Surface(OverworldTerrainGrid.Spec spec)
        {
            var field = OverworldTerrainField.FromSamples(spec, Island);
            var tiles = Grid(spec);
            var maps = new float[tiles.Length][,,];
            for (var t = 0; t < tiles.Length; t++)
            {
                var tx = t % spec.TileCountX;
                var tz = t / spec.TileCountX;
                OverworldTerrainBuilder.PaintSurface(tiles[t], field, tx, tz, CanonicalSeed, TerrainSurfaceRules.Default);
                maps[t] = tiles[t].terrainData.GetAlphamaps(0, 0, spec.AlphamapResolution, spec.AlphamapResolution);
            }

            return maps;
        }

        private UnityEngine.Terrain[] Vegetated(OverworldTerrainGrid.Spec spec, OverworldTerrainField field, out int trees, out long grass)
        {
            var tiles = Grid(spec);
            var tree = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tree.SetActive(false);
            m_spawned.Add(tree);
            var species = new[] { 0, 0, 0, 0, 0 };
            foreach (var tile in tiles)
            {
                tile.terrainData.treePrototypes = new[] { new TreePrototype { prefab = tree } };
                tile.terrainData.detailPrototypes = new[]
                {
                    new DetailPrototype { prototype = tree, usePrototypeMesh = true, renderMode = DetailRenderMode.VertexLit, useInstancing = false },
                };
            }

            var report = OverworldTerrainBuilder.Build(
                tiles, field, CanonicalSeed, TerrainSurfaceRules.Default, TerrainVegetationRules.Default, species, 0);
            trees = report.Trees;
            grass = report.DetailInstances;
            return tiles;
        }

        private static string Hash(float[] values)
        {
            var bytes = new byte[values.Length * 4];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var d = md5.ComputeHash(bytes);
                var sb = new StringBuilder(12);
                for (var i = 0; i < 6; i++)
                {
                    sb.Append(d[i].ToString("x2", CultureInfo.InvariantCulture));
                }

                return sb.ToString();
            }
        }
    }
}

using System.Collections.Generic;
using GameDev.EditorTools;
using GameDev.World.Roads;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameDev.World.Terrain.Tests
{
    /// <summary>
    /// Guards the Debt 11 resolution: URP TerrainLit draws eight terrain layers, and the settlement
    /// tile used to want nine. It now shares <c>Dirt_Road_Edge</c> instead of adding its own dust
    /// layer, which is the only reason the stack fits. These tests drive the real paint path so the
    /// merge cannot be undone by a well-meaning rename.
    /// </summary>
    public sealed class TerrainLayerBudgetTests
    {
        private const string RoadCenterPath = "Assets/Settings/TerrainLayers/Dirt_Road_Center.terrainlayer";
        private const string RoadEdgePath = "Assets/Settings/TerrainLayers/Dirt_Road_Edge.terrainlayer";
        private const string RoadGravelPath = "Assets/Settings/TerrainLayers/Road_Gravel.terrainlayer";

        private const int MaxRenderedTerrainLayers = 8;

        private readonly List<Object> _spawned = new();

        [TearDown]
        public void DestroySpawned()
        {
            foreach (var o in _spawned)
            {
                if (o != null)
                {
                    Object.DestroyImmediate(o);
                }
            }

            _spawned.Clear();
        }

        // ---------- helpers ----------

        private static TerrainLayer Load(string path)
        {
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            Assert.IsNotNull(layer, $"missing terrain layer asset at {path}");
            return layer;
        }

        /// <summary>A throwaway terrain seeded with the five canonical layers, weights summing to 1.</summary>
        private UnityEngine.Terrain NewTerrain(TerrainLayer[] layers, int resolution = 32)
        {
            var data = new TerrainData
            {
                heightmapResolution = 33,
                size = new Vector3(100f, 50f, 100f),
                alphamapResolution = resolution,
            };
            data.terrainLayers = layers;

            var maps = data.GetAlphamaps(0, 0, resolution, resolution);
            for (var z = 0; z < resolution; z++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    for (var l = 0; l < maps.GetLength(2); l++)
                    {
                        maps[z, x, l] = l == 0 ? 1f : 0f;
                    }
                }
            }

            data.SetAlphamaps(0, 0, maps);

            var go = UnityEngine.Terrain.CreateTerrainGameObject(data);
            _spawned.Add(go);
            _spawned.Add(data);
            return go.GetComponent<UnityEngine.Terrain>();
        }

        private static TerrainLayer[] Canonical()
        {
            var canonical = OverworldTerrainLayers.EnsureCanonicalLayers();
            Assert.AreEqual(5, canonical.Length, "the canonical stack is five layers");
            return canonical;
        }

        private static TerrainLayer[] Filler(int count)
        {
            // Distinct throwaway layers, only used to fill the budget up to the ceiling.
            var arr = new TerrainLayer[count];
            for (var i = 0; i < count; i++)
            {
                arr[i] = new TerrainLayer { name = $"Filler_{i}" };
            }

            return arr;
        }

        private static IReadOnlyList<RoadDefinition> OneRoad()
        {
            var centre = new List<Vector2> { new(10f, 50f), new(90f, 50f) };
            var profile = new List<float> { 10f, 10f };
            return new[] { new RoadDefinition(centre, profile, 6f, 2.5f, 8f, RoadTier.Primary) };
        }

        private static string[] Names(UnityEngine.Terrain t)
        {
            var layers = t.terrainData.terrainLayers;
            var names = new string[layers.Length];
            for (var i = 0; i < layers.Length; i++)
            {
                names[i] = layers[i] != null ? layers[i].name : "<null>";
            }

            return names;
        }

        // ---------- the merge itself ----------

        [Test]
        public void SettlementDust_ResolvesToTheRoadEdgeLayer()
        {
            // The whole Debt 11 resolution in one assertion: the settlement does not own a layer,
            // it shares the road edge. Nine layers become eight because of exactly this.
            var dust = SettlementOverworldMenu.EnsureDustTerrainLayer();
            var edge = Load(RoadEdgePath);

            Assert.IsNotNull(dust);
            Assert.AreSame(edge, dust, "settlement dust must be the road edge layer, not a ninth layer");
        }

        // ---------- budget ----------

        [Test]
        public void SettlementThenRoads_StaysWithinTheBudget()
        {
            var canonical = Canonical();
            var dust = SettlementOverworldMenu.EnsureDustTerrainLayer();

            // Settlement paints first in the pipeline, so its layer is already on the tile.
            var seeded = new TerrainLayer[canonical.Length + 1];
            canonical.CopyTo(seeded, 0);
            seeded[canonical.Length] = dust;
            var terrain = NewTerrain(seeded);

            RoadSurfacePaint.Paint(
                terrain, OneRoad(), Load(RoadCenterPath), Load(RoadEdgePath), Load(RoadGravelPath));

            Assert.AreEqual(
                MaxRenderedTerrainLayers,
                terrain.terrainData.terrainLayers.Length,
                "five canonical + centre + edge (shared with dust) + gravel is exactly the budget");
        }

        [Test]
        public void EitherOrder_ProducesTheSameLayerArray()
        {
            var dust = SettlementOverworldMenu.EnsureDustTerrainLayer();

            // Settlement first, then roads.
            var a = new TerrainLayer[6];
            Canonical().CopyTo(a, 0);
            a[5] = dust;
            var settlementFirst = NewTerrain(a);
            RoadSurfacePaint.Paint(
                settlementFirst, OneRoad(), Load(RoadCenterPath), Load(RoadEdgePath), Load(RoadGravelPath));

            // Roads first, then the settlement asks for its layer.
            var roadsFirst = NewTerrain(Canonical());
            RoadSurfacePaint.Paint(
                roadsFirst, OneRoad(), Load(RoadCenterPath), Load(RoadEdgePath), Load(RoadGravelPath));
            var beforeDust = roadsFirst.terrainData.terrainLayers.Length;
            Assert.Contains(dust, roadsFirst.terrainData.terrainLayers,
                "the road edge the settlement would ask for is already on the tile");
            Assert.AreEqual(beforeDust, roadsFirst.terrainData.terrainLayers.Length,
                "asking for it again must not grow the stack");

            CollectionAssert.AreEquivalent(
                Names(settlementFirst),
                Names(roadsFirst),
                "the two execution orders must agree on the layer set");
            Assert.AreEqual(
                settlementFirst.terrainData.terrainLayers.Length,
                roadsFirst.terrainData.terrainLayers.Length);
        }

        [Test]
        public void CanonicalLayers_KeepIndicesZeroToFour()
        {
            var canonical = Canonical();
            var terrain = NewTerrain(canonical);

            RoadSurfacePaint.Paint(
                terrain, OneRoad(), Load(RoadCenterPath), Load(RoadEdgePath), Load(RoadGravelPath));

            var after = terrain.terrainData.terrainLayers;
            for (var i = 0; i < canonical.Length; i++)
            {
                Assert.AreSame(canonical[i], after[i], $"canonical index {i} moved");
            }
        }

        [Test]
        public void RepeatedPaint_KeepsTheSameOrdering()
        {
            var terrain = NewTerrain(Canonical());
            var road = OneRoad();
            var c = Load(RoadCenterPath);
            var e = Load(RoadEdgePath);
            var g = Load(RoadGravelPath);

            RoadSurfacePaint.Paint(terrain, road, c, e, g);
            var first = Names(terrain);
            RoadSurfacePaint.Paint(terrain, road, c, e, g);
            RoadSurfacePaint.Paint(terrain, road, c, e, g);

            CollectionAssert.AreEqual(first, Names(terrain), "repainting must not reorder or duplicate");
        }

        // ---------- optional layers ----------

        [Test]
        public void MissingEdgeAndGravel_StillPaintsTheRoad()
        {
            var canonical = Canonical();
            var terrain = NewTerrain(canonical);

            RoadSurfacePaint.Paint(terrain, OneRoad(), Load(RoadCenterPath), null, null);

            var layers = terrain.terrainData.terrainLayers;
            Assert.AreEqual(canonical.Length + 1, layers.Length, "only the mandatory centre layer is added");
            Assert.AreSame(Load(RoadCenterPath), layers[canonical.Length]);

            var res = terrain.terrainData.alphamapResolution;
            var maps = terrain.terrainData.GetAlphamaps(0, 0, res, res);
            var painted = 0f;
            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    painted = Mathf.Max(painted, maps[z, x, canonical.Length]);
                }
            }

            Assert.Greater(painted, 0f, "the centre layer must still receive weight");
        }

        // ---------- the ceiling ----------

        [Test]
        public void NinthLayer_IsRejectedAndLeavesTheAlphamapAlone()
        {
            // A tile already at the ceiling: five canonical plus three fillers.
            var full = new TerrainLayer[MaxRenderedTerrainLayers];
            Canonical().CopyTo(full, 0);
            var fillers = Filler(3);
            for (var i = 0; i < fillers.Length; i++)
            {
                full[5 + i] = fillers[i];
                _spawned.Add(fillers[i]);
            }

            var terrain = NewTerrain(full);
            var res = terrain.terrainData.alphamapResolution;
            var before = terrain.terrainData.GetAlphamaps(0, 0, res, res);

            LogAssert.ignoreFailingMessages = true;
            RoadSurfacePaint.Paint(
                terrain, OneRoad(), Load(RoadCenterPath), Load(RoadEdgePath), Load(RoadGravelPath));
            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(
                MaxRenderedTerrainLayers,
                terrain.terrainData.terrainLayers.Length,
                "the ninth layer must be refused, not appended");

            var after = terrain.terrainData.GetAlphamaps(0, 0, res, res);
            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    for (var l = 0; l < MaxRenderedTerrainLayers; l++)
                    {
                        Assert.AreEqual(before[z, x, l], after[z, x, l], 1e-6f,
                            $"alphamap changed at ({x},{z}) layer {l} despite the refusal");
                    }
                }
            }
        }
    }
}

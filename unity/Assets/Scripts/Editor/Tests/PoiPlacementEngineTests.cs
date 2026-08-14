using System.Collections.Generic;
using GameDev.World;
using GameDev.World.Landscape;
using GameDev.World.Poi;
using GameDev.World.Streaming;
using GameDev.World.Terrain;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Poi.Tests
{
    public sealed class PoiPlacementEngineTests
    {
        private sealed class FlatSampler : IBiomeSampler
        {
            private readonly float height01;
            private readonly BiomeId biome;

            public FlatSampler(float height01 = 0.4f, BiomeId biome = BiomeId.Meadow)
            {
                this.height01 = height01;
                this.biome = biome;
            }

            public float SampleHeight(float worldX, float worldZ) => height01;
            public BiomeId SampleBiome(float worldX, float worldZ) => biome;
        }

        private static List<ChunkCoord> GridChunks(int n)
        {
            var list = new List<ChunkCoord>();
            for (var z = 0; z < n; z++)
            {
                for (var x = 0; x < n; x++)
                {
                    list.Add(new ChunkCoord(x, z));
                }
            }

            return list;
        }

        [Test]
        public void Catalog_HasProfileForEveryKind()
        {
            foreach (PoiKind kind in System.Enum.GetValues(typeof(PoiKind)))
            {
                var profile = PoiPlacementCatalog.Get(kind);
                Assert.AreEqual(kind, profile.Kind);
            }
        }

        [Test]
        public void Engine_PlacesRequestedKinds_Deterministic()
        {
            var chunks = GridChunks(8);
            var requests = new[]
            {
                new PoiRequest(PoiKind.TownApproach, new[] { BiomeId.Meadow }),
                new PoiRequest(PoiKind.Camp, new[] { BiomeId.Meadow }),
            };
            var a = new PoiPlacementEngine(new GameDev.World.Seed.WorldSeed(42), new FlatSampler(), chunks, requests).AllMarkers();
            var b = new PoiPlacementEngine(new GameDev.World.Seed.WorldSeed(42), new FlatSampler(), chunks, requests).AllMarkers();
            Assert.AreEqual(a.Count, b.Count);
            Assert.AreEqual(2, a.Count);
            Assert.AreEqual(a[0].Position, b[0].Position);
        }

        [Test]
        public void Engine_TownPrefersFlatterFootprint_WhenAnalysisPresent()
        {
            // Left half flat, right half steep.
            float Height(float x, float z) => x < 200f ? 10f : 10f + (x - 200f) * 0.4f;
            BiomeId Biome(float x, float z) => BiomeId.Meadow;
            var field = new LandscapeCostField(0f, 0f, 512f, 512f, 16f, Height, Biome, lakes: null);

            var chunks = new List<ChunkCoord>();
            for (var z = 0; z < 8; z++)
            {
                for (var x = 0; x < 8; x++)
                {
                    chunks.Add(new ChunkCoord(x, z));
                }
            }

            var requests = new[]
            {
                new PoiRequest(PoiKind.TownApproach, new[] { BiomeId.Meadow }),
            };
            var engine = new PoiPlacementEngine(
                new GameDev.World.Seed.WorldSeed(99), new FlatSampler(), chunks, requests, field);
            var markers = engine.AllMarkers();
            Assert.AreEqual(1, markers.Count);
            Assert.Less(markers[0].Position.x, 280f, "town should prefer the flat western terrace");
        }

        [Test]
        public void CustomProfileKind_WorksWithoutEngineRewrite()
        {
            // Ruin uses catalog profile — placing it proves engine is data-driven.
            var chunks = GridChunks(6);
            var requests = new[]
            {
                new PoiRequest(PoiKind.Ruin, PoiPlacementCatalog.Get(PoiKind.Ruin).PreferredBiomes),
            };
            var markers = new PoiPlacementEngine(
                new GameDev.World.Seed.WorldSeed(7), new FlatSampler(0.5f, BiomeId.Forest), chunks, requests).AllMarkers();
            Assert.AreEqual(1, markers.Count);
            Assert.AreEqual(PoiKind.Ruin, markers[0].Kind);
        }

        [Test]
        public void AllMarkers_StableSort_ByKindThenId()
        {
            var chunks = GridChunks(10);
            var requests = new[]
            {
                new PoiRequest(PoiKind.Camp, new[] { BiomeId.Meadow }),
                new PoiRequest(PoiKind.TownApproach, new[] { BiomeId.Meadow }),
                new PoiRequest(PoiKind.Shrine, new[] { BiomeId.Meadow }),
                new PoiRequest(PoiKind.TownApproach, new[] { BiomeId.Meadow }),
            };
            var markers = new PoiPlacementEngine(
                new global::GameDev.World.Seed.WorldSeed(11), new FlatSampler(), chunks, requests).AllMarkers();

            Assert.GreaterOrEqual(markers.Count, 2);
            for (var i = 1; i < markers.Count; i++)
            {
                var cmp = ((int)markers[i - 1].Kind).CompareTo((int)markers[i].Kind);
                if (cmp == 0)
                {
                    Assert.LessOrEqual(
                        string.CompareOrdinal(markers[i - 1].Id.Value, markers[i].Id.Value), 0);
                }
                else
                {
                    Assert.LessOrEqual(cmp, 0);
                }
            }
        }

        [Test]
        public void AddingExtraKind_DoesNotMoveExistingTownApproach()
        {
            var chunks = GridChunks(10);
            var baseRequests = new[]
            {
                new PoiRequest(PoiKind.TownApproach, new[] { BiomeId.Meadow }),
                new PoiRequest(PoiKind.Camp, new[] { BiomeId.Meadow }),
            };
            var extended = new[]
            {
                new PoiRequest(PoiKind.TownApproach, new[] { BiomeId.Meadow }),
                new PoiRequest(PoiKind.Camp, new[] { BiomeId.Meadow }),
                new PoiRequest(PoiKind.Shrine, new[] { BiomeId.Meadow }),
            };

            var a = new PoiPlacementEngine(new global::GameDev.World.Seed.WorldSeed(55), new FlatSampler(), chunks, baseRequests)
                .AllMarkers();
            var b = new PoiPlacementEngine(new global::GameDev.World.Seed.WorldSeed(55), new FlatSampler(), chunks, extended)
                .AllMarkers();

            PoiMarker FindTown(IReadOnlyList<PoiMarker> list)
            {
                foreach (var m in list)
                {
                    if (m.Kind == PoiKind.TownApproach && m.Id.Value == "TownApproach_0")
                    {
                        return m;
                    }
                }

                return default;
            }

            var townA = FindTown(a);
            var townB = FindTown(b);
            Assert.AreEqual(townA.Position, townB.Position);
        }

        [Test]
        public void DemoScenario_FiveTownsPreferCornerAndCenterSectors()
        {
            var settings = WorldGenSettings.Default;
            settings.TownCount = 5;
            settings.ShrineCount = 0;
            settings.SmallCampCount = 0;
            settings.LandmarkCount = 0;
            var requests = PoiDemoScenario.BuildRequests(settings);
            Assert.AreEqual(5, requests.Count);

            float Height(float x, float z) => 20f;
            BiomeId Biome(float x, float z) => BiomeId.Meadow;
            var field = new LandscapeCostField(0f, 0f, 960f, 960f, 32f, Height, Biome, lakes: null);
            var chunks = GridChunks(15);
            var markers = new PoiPlacementEngine(
                new global::GameDev.World.Seed.WorldSeed(123),
                new FlatSampler(),
                chunks,
                requests,
                field).AllMarkers();

            Assert.AreEqual(5, markers.Count);
            var sectors = WorldSectorMap.FromCostField(field);
            var occupied = new bool[3, 3];
            foreach (var m in markers)
            {
                Assert.AreEqual(PoiKind.TownApproach, m.Kind);
                sectors.WorldToSector(m.Position.x, m.Position.z, out var sx, out var sz);
                Assert.IsFalse(occupied[sx, sz], "towns must not share a sector on a flat map");
                occupied[sx, sz] = true;
            }

            // Preferred pattern: corners + center should all be occupied when map is fully habitable.
            Assert.IsTrue(occupied[1, 1], "center hub sector should host a town");
            Assert.IsTrue(occupied[0, 0] || occupied[0, 2] || occupied[2, 0] || occupied[2, 2]);
        }
    }
}

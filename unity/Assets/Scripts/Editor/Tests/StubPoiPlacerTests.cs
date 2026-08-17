using System.Collections.Generic;
using System.Linq;
using GameDev.World.Poi;
using GameDev.World.Seed;
using GameDev.World.Streaming;
using GameDev.World.Terrain;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Poi.Tests
{
    /// <summary>Height-only fake so POI placement tests don't need a live MapMagic terrain.</summary>
    internal sealed class FakeBiomeSampler : IBiomeSampler
    {
        public float SampleHeight(float worldX, float worldZ)
        {
            // Deterministic pseudo-height from coordinates alone, spanning the full biome range.
            var h = (System.Math.Sin(worldX * 0.01f) + System.Math.Cos(worldZ * 0.013f) + 2.0) / 4.0;
            return (float)h;
        }

        // Flat/dry: this fake only varies height, so slope/moisture stay at neutral defaults.
        public BiomeId SampleBiome(float worldX, float worldZ) => BiomeClassifier.Classify(SampleHeight(worldX, worldZ), 0f, 0f);
    }

    public sealed class StubPoiPlacerTests
    {
        private static List<ChunkCoord> StubChunks()
        {
            var chunks = new List<ChunkCoord>();
            for (var x = -4; x < 4; x++)
            {
                for (var z = -4; z < 4; z++)
                {
                    chunks.Add(new ChunkCoord(x, z));
                }
            }

            return chunks;
        }

        // Wide enough that PoiDemoScenario's 23-request quota (well under this grid's ~256 candidate
        // chunks) has room to place every marker without starving the pool.
        private static List<ChunkCoord> WideStubChunks()
        {
            var chunks = new List<ChunkCoord>();
            for (var x = -8; x < 8; x++)
            {
                for (var z = -8; z < 8; z++)
                {
                    chunks.Add(new ChunkCoord(x, z));
                }
            }

            return chunks;
        }

        private static List<PoiMarker> AllMarkers(IPoiPlacer placer, IEnumerable<ChunkCoord> chunks)
        {
            var result = new List<PoiMarker>();
            foreach (var chunk in chunks)
            {
                result.AddRange(placer.PlaceForChunk(chunk));
            }

            return result;
        }

        [Test]
        public void MarkerCount_IsBetweenThreeAndFive()
        {
            var placer = new StubPoiPlacer(new GameDev.World.Seed.WorldSeed(110201), new FakeBiomeSampler(), StubChunks());
            var markers = AllMarkers(placer, StubChunks());

            Assert.GreaterOrEqual(markers.Count, 3);
            Assert.LessOrEqual(markers.Count, 5);
        }

        [Test]
        public void SameSeed_ProducesIdenticalMarkers()
        {
            var chunks = StubChunks();
            var a = AllMarkers(new StubPoiPlacer(new GameDev.World.Seed.WorldSeed(110201), new FakeBiomeSampler(), chunks), chunks);
            var b = AllMarkers(new StubPoiPlacer(new GameDev.World.Seed.WorldSeed(110201), new FakeBiomeSampler(), chunks), chunks);

            Assert.AreEqual(a.Count, b.Count);
            for (var i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Id, b[i].Id);
                Assert.AreEqual(a[i].Kind, b[i].Kind);
                Assert.AreEqual(a[i].Position, b[i].Position);
            }
        }

        [Test]
        public void DifferentSeed_CanProduceDifferentMarkers()
        {
            var chunks = StubChunks();
            var a = AllMarkers(new StubPoiPlacer(new GameDev.World.Seed.WorldSeed(110201), new FakeBiomeSampler(), chunks), chunks);
            var b = AllMarkers(new StubPoiPlacer(new GameDev.World.Seed.WorldSeed(220301), new FakeBiomeSampler(), chunks), chunks);

            var same = a.Count == b.Count && a.Select(m => m.Id).SequenceEqual(b.Select(m => m.Id));
            Assert.IsFalse(same, "Different seeds produced identical marker sets - suspiciously non-random.");
        }

        [Test]
        public void Markers_NeverShareAChunk()
        {
            var chunks = StubChunks();
            var placer = new StubPoiPlacer(new GameDev.World.Seed.WorldSeed(110201), new FakeBiomeSampler(), chunks);

            foreach (var chunk in chunks)
            {
                Assert.LessOrEqual(placer.PlaceForChunk(chunk).Count, 1);
            }
        }

        [Test]
        public void Always_PlacesTownApproach_WhenCandidatesExist()
        {
            var chunks = StubChunks();
            var placer = new StubPoiPlacer(new GameDev.World.Seed.WorldSeed(110201), new FakeBiomeSampler(), chunks);
            var markers = AllMarkers(placer, chunks);
            Assert.IsTrue(
                markers.Exists(m => m.Kind == PoiKind.TownApproach),
                "Expected a TownApproach marker for settlement S6 hook");
        }

        [Test]
        public void QuotaConstructor_PlacesExactlyTheRequestedCountPerKind()
        {
            var chunks = WideStubChunks();
            var requests = new List<PoiRequest>
            {
                new PoiRequest(PoiKind.Camp, new[] { BiomeId.Meadow, BiomeId.Forest }),
                new PoiRequest(PoiKind.Camp, new[] { BiomeId.Meadow, BiomeId.Forest }),
                new PoiRequest(PoiKind.Camp, new[] { BiomeId.Meadow, BiomeId.Forest }),
                new PoiRequest(PoiKind.Shrine, new[] { BiomeId.Forest, BiomeId.Mountain }),
            };

            var placer = new StubPoiPlacer(new GameDev.World.Seed.WorldSeed(110201), new FakeBiomeSampler(), chunks, requests);
            var markers = AllMarkers(placer, chunks);

            Assert.AreEqual(3, markers.Count(m => m.Kind == PoiKind.Camp),
                "repeating a request three times should place three markers of that kind, not collapse to one");
            Assert.AreEqual(1, markers.Count(m => m.Kind == PoiKind.Shrine));
        }

        [Test]
        public void QuotaConstructor_SameKindMarkers_SpreadAcrossMap()
        {
            // All biomes preferred so the pool is the full grid — isolates maximin spacing from
            // meadow/forest band geometry on the fake sampler.
            var chunks = WideStubChunks();
            var allBiomes = new[]
            {
                BiomeId.Swamp, BiomeId.Meadow, BiomeId.Forest, BiomeId.Highland, BiomeId.Mountain
            };
            var requests = new List<PoiRequest>(9);
            for (var i = 0; i < 9; i++)
            {
                requests.Add(new PoiRequest(PoiKind.Camp, allBiomes));
            }

            var placer = new StubPoiPlacer(
                new GameDev.World.Seed.WorldSeed(110201), new FakeBiomeSampler(), chunks, requests);
            var camps = AllMarkers(placer, chunks).Where(m => m.Kind == PoiKind.Camp).ToList();
            Assert.AreEqual(9, camps.Count);

            var minDist = MinPairwiseDistanceXZ(camps);
            // 9 points on a ~1024 m stub tile: random placement often stacks neighbors; maximin
            // should keep every same-kind pair well clear of adjacent chunks.
            Assert.Greater(
                minDist,
                ChunkCoord.ChunkSize * 3.5f,
                $"same-kind camps clustered (minDist={minDist:F1}m); expected spread across the map");
        }

        [Test]
        public void QuotaConstructor_SameSeed_ProducesIdenticalMarkers()
        {
            var chunks = WideStubChunks();
            var requests = PoiDemoScenario.BuildRequests();

            var a = AllMarkers(new StubPoiPlacer(new GameDev.World.Seed.WorldSeed(110201), new FakeBiomeSampler(), chunks, requests), chunks);
            var b = AllMarkers(new StubPoiPlacer(new GameDev.World.Seed.WorldSeed(110201), new FakeBiomeSampler(), chunks, requests), chunks);

            Assert.AreEqual(a.Count, b.Count);
            for (var i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Id, b[i].Id);
                Assert.AreEqual(a[i].Kind, b[i].Kind);
                Assert.AreEqual(a[i].Position, b[i].Position);
            }
        }

        [Test]
        public void QuotaConstructor_MarkerIdsAreUniquePerKind()
        {
            var chunks = WideStubChunks();
            var requests = PoiDemoScenario.BuildRequests();
            var placer = new StubPoiPlacer(new GameDev.World.Seed.WorldSeed(110201), new FakeBiomeSampler(), chunks, requests);
            var markers = AllMarkers(placer, chunks);

            var ids = markers.Select(m => m.Id).ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count(), "every marker, including repeats of the same kind, must get a unique id");
        }

        [Test]
        public void PoiDemoScenario_BuildRequests_MatchesTheDocumentedQuota()
        {
            var requests = PoiDemoScenario.BuildRequests();

            Assert.AreEqual(PoiDemoScenario.TownApproachCount, requests.Count(r => r.Kind == PoiKind.TownApproach));
            Assert.AreEqual(PoiDemoScenario.ShrineCount, requests.Count(r => r.Kind == PoiKind.Shrine));
            Assert.AreEqual(PoiDemoScenario.CampCount, requests.Count(r => r.Kind == PoiKind.Camp));
            Assert.AreEqual(
                PoiDemoScenario.LandmarkCount,
                requests.Count(r => r.Kind == PoiKind.Landmark));

            // Landmarks cycle through the biome list, so however many are requested, every biome
            // must still be represented — that is what stops them clustering in one band.
            var landmarkBiomes = requests
                .Where(r => r.Kind == PoiKind.Landmark)
                .SelectMany(r => r.PreferredBiomes)
                .Distinct()
                .ToList();
            Assert.AreEqual(5, landmarkBiomes.Count, "each biome should get at least one landmark request");
        }

        private static float MinPairwiseDistanceXZ(IReadOnlyList<PoiMarker> markers)
        {
            var min = float.PositiveInfinity;
            for (var i = 0; i < markers.Count; i++)
            {
                for (var j = i + 1; j < markers.Count; j++)
                {
                    var d = DistanceXZ(markers[i].Position, markers[j].Position);
                    if (d < min)
                    {
                        min = d;
                    }
                }
            }

            return min;
        }

        private static float DistanceXZ(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}

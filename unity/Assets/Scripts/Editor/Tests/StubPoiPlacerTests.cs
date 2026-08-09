using System.Collections.Generic;
using System.Linq;
using GameDev.World.Poi;
using GameDev.World.Seed;
using GameDev.World.Streaming;
using GameDev.World.Terrain;
using NUnit.Framework;

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

        public BiomeId SampleBiome(float worldX, float worldZ) => BiomeClassifier.Classify(SampleHeight(worldX, worldZ));
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
    }
}

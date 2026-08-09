using GameDev.World.Streaming;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Streaming.Tests
{
    public sealed class ChunkCoordTests
    {
        [Test]
        public void FromWorld_OriginChunk_CoversZeroToChunkSize()
        {
            Assert.AreEqual(new ChunkCoord(0, 0), ChunkCoord.FromWorld(new Vector3(0f, 0f, 0f)));
            Assert.AreEqual(new ChunkCoord(0, 0), ChunkCoord.FromWorld(new Vector3(63.999f, 0f, 63.999f)));
        }

        [Test]
        public void FromWorld_AtChunkSizeBoundary_RollsOverToNextChunk()
        {
            Assert.AreEqual(new ChunkCoord(1, 0), ChunkCoord.FromWorld(new Vector3(ChunkCoord.ChunkSize, 0f, 0f)));
            Assert.AreEqual(new ChunkCoord(0, 1), ChunkCoord.FromWorld(new Vector3(0f, 0f, ChunkCoord.ChunkSize)));
        }

        [Test]
        public void FromWorld_NegativeCoordinates_FloorDivideCorrectly()
        {
            Assert.AreEqual(new ChunkCoord(-1, -1), ChunkCoord.FromWorld(new Vector3(-1f, 0f, -1f)));
            Assert.AreEqual(new ChunkCoord(-1, 0), ChunkCoord.FromWorld(new Vector3(-0.001f, 0f, 0f)));
            Assert.AreEqual(new ChunkCoord(-2, 0), ChunkCoord.FromWorld(new Vector3(-ChunkCoord.ChunkSize - 1f, 0f, 0f)));
        }

        [Test]
        public void Origin_RoundTrips_ToSameChunkCoord()
        {
            var coord = new ChunkCoord(-3, 5);
            Assert.AreEqual(coord, ChunkCoord.FromWorld(coord.Origin));
        }

        [Test]
        public void ToLocal_IsAlwaysWithinChunkBounds()
        {
            var world = new Vector3(-ChunkCoord.ChunkSize * 2.5f, 0f, ChunkCoord.ChunkSize * 3.5f);
            var local = ChunkCoord.WorldToLocal(world);

            Assert.GreaterOrEqual(local.x, 0f);
            Assert.Less(local.x, ChunkCoord.ChunkSize);
            Assert.GreaterOrEqual(local.z, 0f);
            Assert.Less(local.z, ChunkCoord.ChunkSize);
        }
    }
}

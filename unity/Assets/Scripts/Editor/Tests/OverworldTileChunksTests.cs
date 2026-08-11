using Den.Tools;
using GameDev.World.Streaming;
using NUnit.Framework;

namespace GameDev.World.Streaming.Tests
{
    public sealed class OverworldTileChunksTests
    {
        [Test]
        public void CoveringTile_ZeroMargin_IncludesEveryChunkTouchingTheTile()
        {
            var tileSize = new Vector2D(256f, 256f); // 4x4 chunks at ChunkCoord.ChunkSize=64
            var chunks = OverworldTileChunks.CoveringTile(new Coord(0, 0), tileSize, marginMeters: 0f);

            Assert.AreEqual(16, chunks.Count);
        }

        [Test]
        public void CoveringTile_ExcludesChunksWhoseCenterFallsInsideTheMargin()
        {
            // ChunkCoord.ChunkSize=64, so chunk 0 spans world x [0,64) with its center at x=32.
            // A margin larger than 32 must exclude it even though its index range still touches
            // the unfiltered tile extent - this is exactly the case a corner-based range check
            // (checking only whether the chunk's INDEX overlaps the inset rectangle, not whether
            // its actual center does) gets wrong.
            var tileSize = new Vector2D(256f, 256f);
            var chunks = OverworldTileChunks.CoveringTile(new Coord(0, 0), tileSize, marginMeters: 40f);

            foreach (var chunk in chunks)
            {
                var center = chunk.Center;
                Assert.GreaterOrEqual(center.x, 40f);
                Assert.LessOrEqual(center.x, 256f - 40f);
                Assert.GreaterOrEqual(center.z, 40f);
                Assert.LessOrEqual(center.z, 256f - 40f);
            }
        }

        [Test]
        public void CoveringTile_SmallMarginBelowChunkSize_StillIncludesEdgeChunkCenteredBeyondIt()
        {
            // With a margin smaller than half a chunk (32m here, half of the 64m chunk size), the
            // very first chunk's center (32m from the true edge) already clears it, so it should
            // still be included - the fix must not overcorrect into excluding everything near an
            // edge regardless of how small the margin is.
            var tileSize = new Vector2D(256f, 256f);
            var chunks = OverworldTileChunks.CoveringTile(new Coord(0, 0), tileSize, marginMeters: 20f);

            var found = false;
            foreach (var chunk in chunks)
            {
                if (chunk.X == 0 && chunk.Z == 0)
                {
                    found = true;
                }
            }

            Assert.IsTrue(found, "chunk (0,0)'s center (32,32) sits beyond a 20m margin and should not be excluded");
        }

        [Test]
        public void CoveringTile_NonOriginTile_OffsetsCorrectly()
        {
            var tileSize = new Vector2D(256f, 256f);
            var chunks = OverworldTileChunks.CoveringTile(new Coord(1, 0), tileSize, marginMeters: 0f);

            foreach (var chunk in chunks)
            {
                Assert.GreaterOrEqual(chunk.Center.x, 256f);
                Assert.Less(chunk.Center.x, 512f);
            }
        }
    }
}

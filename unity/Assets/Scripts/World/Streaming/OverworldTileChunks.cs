using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Streaming
{
    /// <summary>
    /// Chunk coordinates covering one overworld tile, inset from the raw tile edge by a
    /// shared margin. The last strip near a tile's border has less reliable derived data -
    /// VistaHydro height-field sampling already skips a margin there so its neighbour
    /// lookups never read out-of-bounds height (an out-of-bounds read is a synthetic +Infinity
    /// "wall" that would otherwise bias basin shapes near the border) - so a lake that visually
    /// reaches the tile edge can still have its flood mask stop just short of it. POI placement and
    /// road routing must stay out of that same strip, or they end up treating "we don't actually
    /// know if this is dry ground or lake" as free, walkable terrain (observed: a road hugging the
    /// tile edge in a straight line, cutting across what read on screen as open lake, because the
    /// water system had no flood data there to flag it as blocked). Centralised here (rather than
    /// each menu picking its own margin) so POI placement, road routing, and water generation all
    /// agree on where the tile's reliably-sampled interior actually starts.
    /// </summary>
    public static class OverworldTileChunks
    {
        public const float DefaultMarginMeters = 20f;

        public static IReadOnlyList<ChunkCoord> CoveringTile(
            int tileCoordX,
            int tileCoordZ,
            float tileSizeX,
            float tileSizeZ,
            float marginMeters)
        {
            var minX = tileCoordX * tileSizeX + marginMeters;
            var maxX = (tileCoordX + 1) * tileSizeX - marginMeters;
            var minZ = tileCoordZ * tileSizeZ + marginMeters;
            var maxZ = (tileCoordZ + 1) * tileSizeZ - marginMeters;

            // Sweep every chunk touching the raw (unmargined) tile extent, then keep only the ones
            // whose own center actually lands inside the margin-inset rectangle - a corner-based
            // coordinate-range test isn't enough here, since marginMeters can be smaller than a
            // single chunk (ChunkCoord.ChunkSize = 64m), in which case an edge chunk's index would
            // still overlap the unfiltered range even though its center sits inside the excluded
            // border strip.
            var rawMinChunkX = ChunkCoord.FromWorld(new Vector3(tileCoordX * tileSizeX, 0f, 0f)).X;
            var rawMaxChunkX = ChunkCoord.FromWorld(
                new Vector3((tileCoordX + 1) * tileSizeX - 0.01f, 0f, 0f)).X;
            var rawMinChunkZ = ChunkCoord.FromWorld(new Vector3(0f, 0f, tileCoordZ * tileSizeZ)).Z;
            var rawMaxChunkZ = ChunkCoord.FromWorld(
                new Vector3(0f, 0f, (tileCoordZ + 1) * tileSizeZ - 0.01f)).Z;

            var chunks = new List<ChunkCoord>();
            for (var x = rawMinChunkX; x <= rawMaxChunkX; x++)
            {
                for (var z = rawMinChunkZ; z <= rawMaxChunkZ; z++)
                {
                    var chunk = new ChunkCoord(x, z);
                    var center = chunk.Center;
                    if (center.x < minX || center.x > maxX || center.z < minZ || center.z > maxZ)
                    {
                        continue;
                    }

                    chunks.Add(chunk);
                }
            }

            return chunks;
        }
    }
}

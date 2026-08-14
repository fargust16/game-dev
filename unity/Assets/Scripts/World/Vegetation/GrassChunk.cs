using UnityEngine;

namespace GameDev.World.Vegetation
{
    /// <summary>
    /// Baked grass instances for one 64 m <see cref="Streaming.ChunkCoord"/>.
    /// CPU matrices only — no GameObject per clump.
    /// </summary>
    public sealed class GrassChunk
    {
        public readonly int ChunkX;
        public readonly int ChunkZ;
        public readonly Vector3 Origin;
        public readonly Matrix4x4[] Matrices;
        public readonly int Count;

        public GrassChunk(int chunkX, int chunkZ, Vector3 origin, Matrix4x4[] matrices, int count)
        {
            ChunkX = chunkX;
            ChunkZ = chunkZ;
            Origin = origin;
            Matrices = matrices;
            Count = count;
        }

        public Vector3 Center => Origin + new Vector3(
            Streaming.ChunkCoord.ChunkSize * 0.5f,
            0f,
            Streaming.ChunkCoord.ChunkSize * 0.5f);

        public static GrassChunk Empty(int chunkX, int chunkZ, Vector3 origin) =>
            new GrassChunk(chunkX, chunkZ, origin, System.Array.Empty<Matrix4x4>(), 0);
    }
}

using System;
using UnityEngine;

namespace GameDev.World.Streaming
{
    /// <summary>
    /// Integer XZ coordinate of a 64 m overworld chunk. Floor-divides world position so
    /// negative coordinates map correctly (e.g. world x = -1 belongs to chunk x = -1, not 0).
    /// </summary>
    public readonly struct ChunkCoord : IEquatable<ChunkCoord>
    {
        public const float ChunkSize = 64f;

        public readonly int X;
        public readonly int Z;

        public ChunkCoord(int x, int z)
        {
            X = x;
            Z = z;
        }

        public static ChunkCoord FromWorld(Vector3 worldPos)
        {
            return new ChunkCoord(FloorDiv(worldPos.x), FloorDiv(worldPos.z));
        }

        /// <summary>World-space min corner (XZ) of this chunk, Y = 0.</summary>
        public Vector3 Origin => new Vector3(X * ChunkSize, 0f, Z * ChunkSize);

        public Vector3 Center => Origin + new Vector3(ChunkSize * 0.5f, 0f, ChunkSize * 0.5f);

        /// <summary>Position relative to this chunk's origin, each axis in [0, ChunkSize).</summary>
        public Vector3 ToLocal(Vector3 worldPos) => worldPos - Origin;

        public static Vector3 WorldToLocal(Vector3 worldPos) => FromWorld(worldPos).ToLocal(worldPos);

        private static int FloorDiv(float value) => Mathf.FloorToInt(value / ChunkSize);

        public bool Equals(ChunkCoord other) => X == other.X && Z == other.Z;

        public override bool Equals(object obj) => obj is ChunkCoord other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Z;
            }
        }

        public override string ToString() => $"ChunkCoord({X}, {Z})";

        public static bool operator ==(ChunkCoord a, ChunkCoord b) => a.Equals(b);

        public static bool operator !=(ChunkCoord a, ChunkCoord b) => !a.Equals(b);
    }
}

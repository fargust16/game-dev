using UnityEngine;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// Stateless deterministic randomness for terrain rules: a value is a function of a stream key and
    /// integer coordinates, never of evaluation order.
    /// </summary>
    /// <remarks>
    /// A <c>System.Random</c> per cell would be deterministic too, but a million allocations per bake
    /// and a hidden dependency on visiting cells in a fixed order. Hashing the coordinates keeps every
    /// sample independent, so a tile, a test fixture or a future chunked bake that evaluates the same
    /// world cell gets the same answer. Stream keys come from <see cref="GameDev.World.Seed.WorldSeed.Fork(string)"/>, so
    /// they follow the project's seed model; <c>UnityEngine.Random</c> is never used.
    /// </remarks>
    public static class TerrainHash
    {
        /// <summary>A 64-bit key for one named stream of a world seed.</summary>
        public static ulong StreamKey(int seed, string streamId)
        {
            var random = new GameDev.World.Seed.WorldSeed(seed).Fork(streamId);
            var hi = (ulong)(uint)random.Next();
            var lo = (ulong)(uint)random.Next();
            return Mix((hi << 32) | lo);
        }

        /// <summary>SplitMix64 finaliser.</summary>
        public static ulong Mix(ulong value)
        {
            unchecked
            {
                value += 0x9E3779B97F4A7C15UL;
                value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
                value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
                return value ^ (value >> 31);
            }
        }

        public static ulong Hash(ulong key, int a, int b, int c = 0)
        {
            unchecked
            {
                var h = Mix(key ^ (ulong)(uint)a);
                h = Mix(h ^ ((ulong)(uint)b << 1));
                return Mix(h ^ ((ulong)(uint)c << 2));
            }
        }

        /// <summary>Uniform in [0, 1).</summary>
        public static float Unit(ulong key, int a, int b, int c = 0) =>
            (Hash(key, a, b, c) >> 40) / (float)(1UL << 24);

        /// <summary>
        /// Smooth value noise in [0, 1] with features about <paramref name="wavelengthMeters"/> across.
        /// </summary>
        public static float ValueNoise(ulong key, float worldX, float worldZ, float wavelengthMeters)
        {
            var fx = worldX / wavelengthMeters;
            var fz = worldZ / wavelengthMeters;
            var x0 = Mathf.FloorToInt(fx);
            var z0 = Mathf.FloorToInt(fz);
            var tx = fx - x0;
            var tz = fz - z0;
            tx = tx * tx * (3f - 2f * tx);
            tz = tz * tz * (3f - 2f * tz);
            var a = Mathf.Lerp(Unit(key, x0, z0), Unit(key, x0 + 1, z0), tx);
            var b = Mathf.Lerp(Unit(key, x0, z0 + 1), Unit(key, x0 + 1, z0 + 1), tx);
            return Mathf.Lerp(a, b, tz);
        }
    }
}

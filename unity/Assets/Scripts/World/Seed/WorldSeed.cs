using System;

namespace GameDev.World.Seed
{
    /// <summary>
    /// Default <see cref="IWorldSeed"/>: forks a deterministic <see cref="Random"/> per named
    /// stream by mixing the stream id (and optional int keys) into the world seed value.
    /// </summary>
    public sealed class WorldSeed : IWorldSeed
    {
        public int Value { get; }

        public WorldSeed(int value)
        {
            Value = value;
        }

        public Random Fork(string streamId)
        {
            unchecked
            {
                var h = MixStream(Value, streamId);
                if (h == int.MinValue)
                {
                    h = 1;
                }

                return new Random(h);
            }
        }

        public Random Fork(string streamId, int a, int b = 0)
        {
            unchecked
            {
                var h = MixStream(Value, streamId);
                h = (h * 397) ^ a;
                h = (h * 397) ^ b;
                if (h == int.MinValue)
                {
                    h = 1;
                }

                return new Random(h);
            }
        }

        private static int MixStream(int seed, string streamId)
        {
            unchecked
            {
                var h = seed;
                if (!string.IsNullOrEmpty(streamId))
                {
                    for (var i = 0; i < streamId.Length; i++)
                    {
                        h = (h * 397) ^ streamId[i];
                    }
                }

                return h;
            }
        }
    }
}

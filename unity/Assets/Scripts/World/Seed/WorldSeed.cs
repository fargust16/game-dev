using System;

namespace GameDev.World.Seed
{
    /// <summary>
    /// Default <see cref="IWorldSeed"/>: forks a deterministic <see cref="Random"/> per named
    /// stream by mixing the stream id into the world seed value.
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
                var h = Value;
                if (!string.IsNullOrEmpty(streamId))
                {
                    for (var i = 0; i < streamId.Length; i++)
                    {
                        h = (h * 397) ^ streamId[i];
                    }
                }

                if (h == int.MinValue)
                {
                    h = 1;
                }

                return new Random(h);
            }
        }
    }
}

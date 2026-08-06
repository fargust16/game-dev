using System;

namespace GameDev.World
{
    /// <summary>
    /// Deterministic seed streams for city generation stages (roads, parcels, anchors, dressing).
    /// </summary>
    [Serializable]
    public sealed class WorldSeed
    {
        public const int DefaultPrototypeSeed = 110201;

        public int value = DefaultPrototypeSeed;

        public WorldSeed()
        {
        }

        public WorldSeed(int seed)
        {
            value = seed;
        }

        public Random Fork(string stage)
        {
            unchecked
            {
                var h = value;
                if (!string.IsNullOrEmpty(stage))
                {
                    for (var i = 0; i < stage.Length; i++)
                    {
                        h = (h * 397) ^ stage[i];
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

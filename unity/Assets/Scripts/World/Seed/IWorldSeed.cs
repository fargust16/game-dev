using System;

namespace GameDev.World.Seed
{
    /// <summary>
    /// World identity seed. Never mutates after world create — consumers fork named
    /// deterministic streams instead of sharing one Random instance.
    /// </summary>
    public interface IWorldSeed
    {
        int Value { get; }

        Random Fork(string streamId);

        /// <summary>
        /// Per-object stream: mix optional int keys so lake 0 / lake 1 / poi.Shrine.2
        /// stay isolated from each other and from sibling systems.
        /// </summary>
        Random Fork(string streamId, int a, int b = 0);
    }
}

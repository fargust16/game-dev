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
    }
}

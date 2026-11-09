namespace GameDev.World.Seed
{
    /// <summary>A key for <see cref="IWorldSeed.Fork(string, int, int)"/> from a name, the same on every runtime.</summary>
    public static class StableHash
    {
        /// <summary>FNV-1a over the text: string.GetHashCode differs between runtimes, and a draw must not.</summary>
        public static int Of(string text)
        {
            unchecked
            {
                var hash = (int)2166136261;
                foreach (var c in text)
                {
                    hash = (hash ^ c) * 16777619;
                }

                return hash;
            }
        }
    }
}

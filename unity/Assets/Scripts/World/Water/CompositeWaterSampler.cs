using System.Collections.Generic;

namespace GameDev.World.Water
{
    /// <summary>
    /// Combines several <see cref="IWaterSampler"/>s (e.g. lakes + rivers) behind one query, first
    /// hit wins - lets callers that only ever asked "is this scene's <see cref="IWaterSampler"/>
    /// telling me there's water here" (POI placement, road costing, settlement dry-ground search)
    /// keep asking exactly that shape of question without knowing rivers exist as a separate system.
    /// Null entries are skipped, so a caller can build one of these before every source is guaranteed
    /// to exist yet (e.g. no rivers generated this seed) without extra null-checks at every call site.
    /// </summary>
    public sealed class CompositeWaterSampler : IWaterSampler
    {
        private readonly IReadOnlyList<IWaterSampler> sources;

        public CompositeWaterSampler(params IWaterSampler[] sources)
        {
            this.sources = sources;
        }

        public bool TryGetWaterLevel(float worldX, float worldZ, out float waterY)
        {
            for (var i = 0; i < sources.Count; i++)
            {
                var source = sources[i];
                if (source != null && source.TryGetWaterLevel(worldX, worldZ, out waterY))
                {
                    return true;
                }
            }

            waterY = 0f;
            return false;
        }
    }
}

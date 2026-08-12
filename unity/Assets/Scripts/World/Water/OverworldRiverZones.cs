using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Scene-resident holder for the current generation's traced rivers - mirrors
    /// <see cref="OverworldWaterZones"/> exactly, so editor tools can find and query it the same
    /// way. Regenerated every terrain rebuild; never serialized (generation is not persistence).
    /// </summary>
    public sealed class OverworldRiverZones : MonoBehaviour, IWaterSampler
    {
        private List<RiverPath> rivers = new List<RiverPath>();
        private RiverWaterSampler sampler = new RiverWaterSampler(new List<RiverPath>());

        public IReadOnlyList<RiverPath> Rivers => rivers;

        public void SetRivers(IReadOnlyList<RiverPath> newRivers)
        {
            rivers = new List<RiverPath>(newRivers);
            sampler = new RiverWaterSampler(rivers);
        }

        public bool TryGetWaterLevel(float worldX, float worldZ, out float waterY) =>
            sampler.TryGetWaterLevel(worldX, worldZ, out waterY);
    }
}

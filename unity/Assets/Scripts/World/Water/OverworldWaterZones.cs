using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Scene-resident holder for the current generation's water zones, exposed as
    /// <see cref="IWaterSampler"/> so editor tools (vegetation scatter, POI markers) query real
    /// per-position water instead of reading one global plane's <c>transform.position.y</c> - the
    /// old <c>VegetationScatterMenu</c>/<c>PoiMarkerMenu</c> pattern, which broke the moment a
    /// second lake sat at a different height. Regenerated every terrain rebuild; never serialized
    /// (17 Game Systems Architecture: generation is not persistence).
    /// </summary>
    public sealed class OverworldWaterZones : MonoBehaviour, IWaterSampler
    {
        private List<WaterZone> zones = new List<WaterZone>();
        private WaterZoneSampler sampler = new WaterZoneSampler(new List<WaterZone>());

        public IReadOnlyList<WaterZone> Zones => zones;

        public void SetZones(IReadOnlyList<WaterZone> newZones)
        {
            zones = new List<WaterZone>(newZones);
            sampler = new WaterZoneSampler(zones);
        }

        public bool TryGetWaterLevel(float worldX, float worldZ, out float waterY) =>
            sampler.TryGetWaterLevel(worldX, worldZ, out waterY);
    }
}

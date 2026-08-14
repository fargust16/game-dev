using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Scene-resident holder for the current generation's water zones, exposed as
    /// <see cref="IWaterSampler"/> so editor tools query real per-position water.
    /// Also stores planned <see cref="LakeDefinition"/>s for LandscapePlanner (cost / deform)
    /// before and after basin dig.
    /// </summary>
    public sealed class OverworldWaterZones : MonoBehaviour, IWaterSampler
    {
        private List<WaterZone> zones = new List<WaterZone>();
        private List<LakeDefinition> lakeDefinitions = new List<LakeDefinition>();
        private WaterZoneSampler sampler = new WaterZoneSampler(new List<WaterZone>());

        public IReadOnlyList<WaterZone> Zones => zones;
        public IReadOnlyList<LakeDefinition> LakeDefinitions => lakeDefinitions;
        public float PlannedBasinDepthMeters { get; private set; }
        public bool BasinsApplied { get; private set; }

        public void SetZones(IReadOnlyList<WaterZone> newZones)
        {
            zones = new List<WaterZone>(newZones ?? System.Array.Empty<WaterZone>());
            sampler = new WaterZoneSampler(zones);
        }

        public void SetLakePlan(IReadOnlyList<LakeDefinition> defs, float plannedBasinDepthMeters)
        {
            lakeDefinitions = new List<LakeDefinition>(defs ?? System.Array.Empty<LakeDefinition>());
            PlannedBasinDepthMeters = plannedBasinDepthMeters;
            BasinsApplied = false;
            if (lakeDefinitions.Count > 0)
            {
                var zs = new List<WaterZone>(lakeDefinitions.Count);
                for (var i = 0; i < lakeDefinitions.Count; i++)
                {
                    zs.Add(lakeDefinitions[i].Zone);
                }

                SetZones(zs);
            }
        }

        public void MarkBasinsApplied() => BasinsApplied = true;

        public bool TryGetWaterLevel(float worldX, float worldZ, out float waterY) =>
            sampler.TryGetWaterLevel(worldX, worldZ, out waterY);

        /// <summary>
        /// Planned-lake occupancy for road cost before dig: uses shore SDF, not height compare.
        /// </summary>
        public bool TryGetPlannedLakeSignedDistance(float worldX, float worldZ, out float signedDistance)
        {
            signedDistance = float.NegativeInfinity;
            if (lakeDefinitions.Count == 0)
            {
                return false;
            }

            var best = float.NegativeInfinity;
            for (var i = 0; i < lakeDefinitions.Count; i++)
            {
                var d = lakeDefinitions[i].WaterShoreSignedDistance(worldX, worldZ);
                if (d > best)
                {
                    best = d;
                }
            }

            signedDistance = best;
            return true;
        }
    }
}

using UnityEngine;

namespace GameDev.World.Settlement
{
    /// <summary>
    /// Scene-resident record of where the (V0: single) placed settlement actually landed, keyed by
    /// its TownApproach POI id. <see cref="SettlementApproachBinder.TryCreateDryFabricNearApproach"/>
    /// searches a ring of offsets (up to DrySearchRingMax * DrySearchStepMeters ~= 192m) away from
    /// the raw POI marker position looking for dry, flat ground - so anything that needs to actually
    /// reach the settlement (road routing) must read this real anchor position, not the original
    /// marker position, or a "connected" road can end up terminating well short of where the town
    /// fabric actually sits.
    /// Also holds the posed <see cref="SettlementFabricLayout"/> so grass / vegetation can carve
    /// streets and pads without re-deriving the fabric from spawned meshes.
    /// </summary>
    public sealed class OverworldSettlementAnchor : MonoBehaviour
    {
        public string ApproachId { get; private set; }
        public Vector3 Position { get; private set; }

        /// <summary>Posed fabric snapshot; null if only position was recorded.</summary>
        public SettlementFabricLayout Fabric { get; private set; }

        public void Set(string approachId, Vector3 position)
        {
            ApproachId = approachId;
            Position = position;
        }

        public void Set(string approachId, Vector3 position, SettlementFabricLayout fabric)
        {
            ApproachId = approachId;
            Position = position;
            Fabric = fabric;
        }
    }
}

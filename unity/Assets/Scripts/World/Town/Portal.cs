using UnityEngine;

namespace GameDev.World.Town
{
    /// <summary>How the town connects to the outside world at this portal.</summary>
    public enum PortalType
    {
        /// <summary>A fortified town gate (used when walls are enabled).</summary>
        Gate,

        /// <summary>A bridge where an arterial crosses a river.</summary>
        Bridge,

        /// <summary>An informal farm road leading into agricultural hinterland.</summary>
        FarmRoad,

        /// <summary>A road connecting to a neighbouring settlement.</summary>
        SettlementRoad
    }

    /// <summary>
    /// A point on the town boundary where an arterial road exits the town.
    /// </summary>
    public sealed class Portal
    {
        /// <summary>Position on the town boundary polygon (world-space XZ, metres).</summary>
        public Vector2 Pos;

        /// <summary>Functional type of this connection point.</summary>
        public PortalType Type;
    }
}

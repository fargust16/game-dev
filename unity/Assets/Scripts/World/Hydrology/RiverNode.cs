using UnityEngine;

namespace GameDev.World.Hydrology
{
    /// <summary>
    /// One sample along a river centreline, ordered upstream to downstream.
    /// </summary>
    /// <remarks>
    /// The field set is deliberately wider than the MVP needs. Everything a later consumer has to
    /// derive — local slope, vertical drop, discharge-driven width, a bridge site, distance from the
    /// source — comes from these five values plus the neighbouring nodes, so adding waterfalls or
    /// crossings later is a query, not a re-generation. See <c>HYDROLOGY_GENERATION_PLAN.md</c> §5.
    /// </remarks>
    public readonly struct RiverNode
    {
        /// <summary>World-space XZ of the channel centre, in metres.</summary>
        public Vector2 PositionXZ { get; }

        /// <summary>
        /// Planned world Y of the channel floor after the terrain carve, in metres.
        /// </summary>
        /// <remarks>
        /// This is what the carve field is rasterised from, not what the terrain measured before
        /// it. The water surface is derived from the same number, which is why water cannot end up
        /// floating above the bed it was cut for.
        /// </remarks>
        public float BedElevation { get; }

        /// <summary>World Y of the water surface, in metres. Always at or above <see cref="BedElevation"/>.</summary>
        public float WaterElevation { get; }

        /// <summary>Distance from the reach's upstream end along the centreline, in metres.</summary>
        public float CumulativeDistance { get; }

        /// <summary>Upstream contributing area at this node, in square metres.</summary>
        /// <remarks>
        /// The physical discharge proxy. Width, carve depth and Strahler order all follow from it,
        /// which is what keeps the hierarchy emergent instead of hand-assigned.
        /// </remarks>
        public float ContributingAreaSqm { get; }

        public RiverNode(
            Vector2 positionXZ,
            float bedElevation,
            float waterElevation,
            float cumulativeDistance,
            float contributingAreaSqm)
        {
            PositionXZ = positionXZ;
            BedElevation = bedElevation;
            WaterElevation = waterElevation;
            CumulativeDistance = cumulativeDistance;
            ContributingAreaSqm = contributingAreaSqm;
        }

        /// <summary>Water depth at this node, in metres.</summary>
        public float DepthMeters => WaterElevation - BedElevation;
    }
}

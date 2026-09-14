using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Hydrology
{
    /// <summary>
    /// A lake, derived from a filled terrain depression rather than placed.
    /// </summary>
    /// <remarks>
    /// A basin exists here only because priority-flood measured that the terrain holds water there:
    /// its cells are the ones where the filled surface rose above the original, its outline is the
    /// basin's own outline, and <see cref="SurfaceElevation"/> is the elevation at which it spills.
    /// There is no radius and no placement pass. See <c>HYDROLOGY_GENERATION_PLAN.md</c> §9.
    /// <para>
    /// Terminal (endorheic) lakes are not generated: every basin is filled until it spills, so every
    /// lake has an outlet and the ocean-outlet invariant holds by construction.
    /// </para>
    /// </remarks>
    public sealed class LakeBasin
    {
        public LakeId Id { get; }

        /// <summary>Shoreline polygon in world XZ, closed implicitly (last vertex joins the first).</summary>
        public IReadOnlyList<Vector2> Boundary { get; }

        /// <summary>World Y of the water surface — the spill elevation the flood computed.</summary>
        public float SurfaceElevation { get; }

        /// <summary>Deepest point below <see cref="SurfaceElevation"/>, in metres.</summary>
        public float MaxDepth { get; }

        public float AreaSqm { get; }

        /// <summary>The reach carrying the outflow. Always valid — a lake without one cannot exist.</summary>
        public ReachId OutletReach { get; }

        /// <summary>Reaches that empty into this lake. May be empty for a purely rain-fed basin.</summary>
        public IReadOnlyList<ReachId> InletReaches { get; }

        /// <summary>Spill point in world XZ — where the outlet leaves the basin.</summary>
        public Vector2 SpillPointXZ { get; }

        /// <summary>
        /// True when the basin spills straight into the sea, with no mapped reach between.
        /// </summary>
        /// <remarks>
        /// A coastal basin can overflow directly onto the shore, in which case there is no outlet
        /// reach to name and <see cref="OutletReach"/> is invalid. Recording it explicitly is what
        /// lets the ocean-outlet invariant stay a real check: without it, "no outlet reach" would be
        /// indistinguishable from a lake that drains into nothing, and the validator would have to
        /// either reject a legitimate world or wave through a broken one.
        /// </remarks>
        public bool SpillsDirectlyToOcean { get; }

        public LakeBasin(
            LakeId id,
            IReadOnlyList<Vector2> boundary,
            float surfaceElevation,
            float maxDepth,
            float areaSqm,
            ReachId outletReach,
            IReadOnlyList<ReachId> inletReaches,
            Vector2 spillPointXZ,
            bool spillsDirectlyToOcean = false)
        {
            SpillsDirectlyToOcean = spillsDirectlyToOcean;
            Id = id;
            Boundary = boundary;
            SurfaceElevation = surfaceElevation;
            MaxDepth = maxDepth;
            AreaSqm = areaSqm;
            OutletReach = outletReach;
            InletReaches = inletReaches;
            SpillPointXZ = spillPointXZ;
        }

        /// <summary>World Y of the deepest basin floor.</summary>
        public float FloorElevation => SurfaceElevation - MaxDepth;
    }
}

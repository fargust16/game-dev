using GameDev.World.Town.Geometry;
using UnityEngine;

namespace GameDev.World.Town
{
    /// <summary>
    /// A land parcel subdivided from a block — the atomic unit to which a
    /// <see cref="Building"/> is assigned. Each plot must have street frontage or
    /// be explicitly classified as a courtyard/garden.
    /// </summary>
    public sealed class Plot
    {
        /// <summary>Polygon of the plot boundary (world-space XZ, metres).</summary>
        public Polygon2 Outline;

        /// <summary>Index into <see cref="TownModel.Blocks"/> this plot belongs to.</summary>
        public int BlockIndex;

        /// <summary>
        /// True if this plot has direct street frontage (a required structural invariant
        /// for all buildable plots).
        /// </summary>
        public bool HasFrontage;

        /// <summary>
        /// Direction vector pointing from the plot toward its front street edge
        /// (world-space XZ, normalised).
        /// </summary>
        public Vector2 FrontageDir;

        public Plot()
        {
            Outline = new Polygon2();
        }
    }
}

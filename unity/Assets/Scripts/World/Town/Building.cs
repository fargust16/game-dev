using GameDev.World.Town.Geometry;

namespace GameDev.World.Town
{
    /// <summary>Broad architectural/functional category of a building.</summary>
    public enum BuildingKind
    {
        Residential,
        Commercial,
        Civic,
        Industrial,
        Religious,
        Landmark
    }

    /// <summary>
    /// A building instance: a footprint polygon, floor count, and back-reference
    /// to the plot it occupies.
    /// </summary>
    public sealed class Building
    {
        /// <summary>Ground-floor footprint polygon (world-space XZ, metres).</summary>
        public Polygon2 Footprint;

        /// <summary>Number of above-ground floors (may be fractional for rendering).</summary>
        public float Floors;

        /// <summary>Index into <see cref="TownModel.Plots"/>.</summary>
        public int PlotIndex;

        /// <summary>Architectural/functional category.</summary>
        public BuildingKind Kind;

        public Building()
        {
            Footprint = new Polygon2();
        }
    }
}

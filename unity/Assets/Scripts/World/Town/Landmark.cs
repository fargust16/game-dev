using GameDev.World.Town.Geometry;

namespace GameDev.World.Town
{
    /// <summary>Type of the dominant central landmark that anchors the town.</summary>
    public enum LandmarkKind
    {
        MarketSquare,
        Church,
        Castle,
        TownHall
    }

    /// <summary>
    /// The central landmark that anchors the town — its building footprint and the
    /// surrounding plaza polygon.
    /// </summary>
    public sealed class Landmark
    {
        /// <summary>Type of the landmark.</summary>
        public LandmarkKind Kind;

        /// <summary>Building footprint polygon (world-space XZ, metres).</summary>
        public Polygon2 Footprint;

        /// <summary>Surrounding plaza polygon that opens a view cone toward the landmark.</summary>
        public Polygon2 Plaza;

        public Landmark()
        {
            Footprint = new Polygon2();
            Plaza     = new Polygon2();
        }
    }
}

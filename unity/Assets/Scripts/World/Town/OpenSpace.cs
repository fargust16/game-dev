using GameDev.World.Town.Geometry;

namespace GameDev.World.Town
{
    /// <summary>Functional category of an outdoor open space.</summary>
    public enum OpenSpaceKind
    {
        /// <summary>A hard-paved plaza at a major junction or landmark.</summary>
        Plaza,

        /// <summary>A well or fountain, usually placed within a plaza or at a junction.</summary>
        Well,

        /// <summary>A garden — open green space, typically in a rich district.</summary>
        Garden,

        /// <summary>An interior courtyard bounded by surrounding buildings.</summary>
        Courtyard
    }

    /// <summary>
    /// A non-street, non-building outdoor area (plaza, well, garden, or courtyard).
    /// </summary>
    public sealed class OpenSpace
    {
        /// <summary>Bounding polygon of the space (world-space XZ, metres).</summary>
        public Polygon2 Area;

        /// <summary>Functional kind of this space.</summary>
        public OpenSpaceKind Kind;

        public OpenSpace()
        {
            Area = new Polygon2();
        }
    }
}

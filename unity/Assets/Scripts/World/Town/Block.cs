using GameDev.World.Town.Geometry;

namespace GameDev.World.Town
{
    /// <summary>
    /// A city block — the polygon of buildable land enclosed by streets after
    /// the district has been subdivided. Blocks are further subdivided into
    /// <see cref="Plot"/>s.
    /// </summary>
    public sealed class Block
    {
        /// <summary>Outer boundary polygon of the block (world-space XZ, metres).</summary>
        public Polygon2 Outline;

        /// <summary>Index into <see cref="TownModel.Districts"/> this block belongs to.</summary>
        public int DistrictIndex;

        public Block()
        {
            Outline = new Polygon2();
        }
    }
}

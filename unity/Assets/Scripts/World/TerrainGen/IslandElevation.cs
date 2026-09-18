using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Stage 4A's composed height: the frozen structural surface plus the island relief.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A TerrainGen composition only. <c>TerrainAuthorityField</c> does not call it until Stage 4A has
    /// passed its own acceptance; the production height is still <see cref="StructuralElevation.At"/>.
    /// </para>
    /// <para>
    /// The future Stage 3F carve composes after it, <c>H = H4A − C3F</c>: Stage 3 structures were declared
    /// on the structural surface, and a valley has to descend through whatever relief stands above it.
    /// </para>
    /// </remarks>
    public static class IslandElevation
    {
        /// <summary>
        /// <c>H4A = StructuralElevation.At + relief</c>, with the coast distance, <c>H1</c> and <c>U2</c>
        /// evaluated once.
        /// </summary>
        public static float At(
            Stage1Result stage1, MassifUpliftModel uplift, IslandRelief relief, float worldX, float worldZ)
        {
            var signedDistance = stage1.Coast.SignedDistance(worldX, worldZ);
            var island = stage1.Model.HeightAt(signedDistance);
            if (uplift == null)
            {
                return relief == null ? island : island + relief.At(worldX, worldZ, signedDistance, island, 0f);
            }

            var lift = uplift.UpliftAt(worldX, worldZ, signedDistance);
            var structural = island + lift;
            return relief == null
                ? structural
                : structural + relief.At(worldX, worldZ, signedDistance, island, lift);
        }

        public static float At(Stage1Result stage1, MassifUpliftModel uplift, IslandRelief relief, Vector2 p) =>
            At(stage1, uplift, relief, p.x, p.y);
    }
}

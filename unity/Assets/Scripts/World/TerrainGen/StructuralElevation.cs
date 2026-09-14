using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// The Stage 2 structural surface, evaluated at a point rather than read off a lattice.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The authoritative elevation for any stage that must talk about how high structural ground is
    /// without owning a carved field. It is <c>H2</c> reproduced, not re-invented:
    /// <see cref="Stage2BMassifGenerator.EvaluateFields"/> writes
    /// <c>baseField + uplift.UpliftAt(x, z, signedDistance)</c> into every sample of its field, and
    /// this is that expression with the lattice reads replaced by the analytic ones. It agrees with
    /// the field exactly at lattice points and interpolates between them.
    /// </para>
    /// <para>
    /// <b>Pre-carve by construction.</b> It depends on Stage 1 and the massif uplift and on nothing
    /// else - no <c>H3</c>, no <see cref="CarveField"/>, no corridor and no trajectory - so a stage
    /// that reads it is not reading its own output back, and a stage downstream of the carve is free
    /// to disagree with it.
    /// </para>
    /// </remarks>
    public static class StructuralElevation
    {
        /// <summary>Structural ground height at a world point, metres.</summary>
        public static float At(Stage1Result stage1, MassifUpliftModel uplift, float worldX, float worldZ)
        {
            var island = stage1.HeightAt(worldX, worldZ);
            return uplift == null
                ? island
                : island + uplift.UpliftAt(worldX, worldZ, stage1.Coast.SignedDistance(worldX, worldZ));
        }

        public static float At(Stage1Result stage1, MassifUpliftModel uplift, Vector2 p) =>
            At(stage1, uplift, p.x, p.y);
    }
}

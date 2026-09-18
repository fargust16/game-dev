using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Stage 3F's composed height: the island surface with the declared valley corridors cut into it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>H3F = H4A − C3F</c>. Subtractive and nothing else, which is what makes the Stage 2 envelope,
    /// the certified core, the width budget and the coastline survive without being checked for — an
    /// operation that only removes cannot violate them. It is <see cref="Stage3Field"/>'s composition
    /// with <c>H2</c> replaced by <see cref="IslandElevation"/>, because Stage 4A is the surface the
    /// corridors now have to descend through.
    /// </para>
    /// <para>
    /// <b>The allowance absorbs Stage 4A on its own.</b> A corridor may cut below the Stage 1 floor,
    /// but only by <see cref="RidgeSettings.CorridorIncisionMeters"/>; the budget is written against
    /// the ground above the floor, so the relief Stage 4A added is inside the allowance already rather
    /// than competing with it. Stage 4A's own contract bounds the single climb a corridor meets by the
    /// same constant, so a corridor crossing Stage 4A relief stays one trough.
    /// </para>
    /// <para>
    /// <b>The coast cannot move.</b> <see cref="CarveField"/>'s gate is exactly zero at and seaward of
    /// the shoreline, so <c>C3F</c> is zero there by construction, and the sea clamp keeps land land.
    /// Both are the same guarantees Stage 4A carries, from the same signed coast distance.
    /// </para>
    /// <para>
    /// <b>Not on the production path.</b> <c>TerrainAuthorityField</c> evaluates
    /// <see cref="IslandElevation"/>; this layer is validated through its dump and tests first, exactly
    /// as Stage 4A was. It also still reads the carve as <see cref="CarveField"/> binds it today — to
    /// the frozen <see cref="ValleyCorridor"/> trajectories, not to Slice C's
    /// <see cref="FinalValleyTrajectory"/>. Re-binding needs a scoped unfreeze of Stage 3C
    /// (PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md §15.2) and is deliberately not done here.
    /// </para>
    /// </remarks>
    public static class CarvedElevation
    {
        /// <summary>
        /// The carve actually applied at a point, metres: never negative, never past the allowance,
        /// never enough to put land under the sea.
        /// </summary>
        /// <param name="rawDepth"><see cref="CarveField.Depth"/> at the point, before the budget.</param>
        /// <param name="islandHeight"><c>H4A</c> at the point.</param>
        /// <param name="stage1Height"><c>H1</c> at the point — the floor the incision is measured from.</param>
        public static float CarveAt(
            float rawDepth, float islandHeight, float stage1Height, RidgeSettings settings)
        {
            if (rawDepth <= 0f)
            {
                return 0f;
            }

            var allowance = Mathf.Max(0f, islandHeight - stage1Height) + settings.CorridorIncisionMeters;
            var cut = Mathf.Min(rawDepth, allowance);
            cut = Mathf.Min(cut, Mathf.Max(0f, islandHeight - WorldScale.SeaLevelMeters - SeaMarginMeters));
            return cut > 0f ? cut : 0f;
        }

        /// <summary>
        /// How far above the waterline the carve must leave the ground, metres.
        /// </summary>
        /// <remarks>
        /// <see cref="Stage3Field"/>'s constant, kept rather than re-chosen: the carve is allowed to
        /// deepen a valley but never to open a new inlet, and half a metre is the margin that keeps a
        /// carved trough a trough at the 16-bit height quantisation the bake writes.
        /// </remarks>
        public const float SeaMarginMeters = 0.5f;

        /// <summary>
        /// <c>H3F</c> at a world point, metres.
        /// </summary>
        /// <remarks>
        /// A pure function of position: it reads the caller's already-evaluated coast distance and the
        /// immutable structures Stage 1, Stage 2, Stage 3C and Stage 4A built once. The lattice
        /// evaluator calls this same function per sample, so a point query and the bulk field agree to
        /// the bit, and nothing here knows tiles exist.
        /// </remarks>
        public static float At(
            Stage1Result stage1,
            MassifUpliftModel uplift,
            IslandRelief relief,
            CarveField carve,
            RidgeSettings settings,
            float worldX,
            float worldZ)
        {
            var signedDistance = stage1.Coast.SignedDistance(worldX, worldZ);
            var floor = stage1.Model.HeightAt(signedDistance);
            var lift = uplift == null ? 0f : uplift.UpliftAt(worldX, worldZ, signedDistance);
            var island = floor + lift;
            if (relief != null)
            {
                island += relief.At(worldX, worldZ, signedDistance, floor, lift);
            }

            if (carve == null)
            {
                return island;
            }

            var raw = carve.Depth(worldX, worldZ, carve.GateFor(signedDistance), signedDistance > 0f);
            return island - CarveAt(raw, island, floor, settings);
        }

        /// <summary>
        /// The Stage 3F surface and the carve that made it, on the lattice Stage 1 already evaluated.
        /// </summary>
        /// <remarks>
        /// The bulk path reuses Stage 1's signed distance, <c>H1</c> and the caller's <c>H4A</c> rather
        /// than walking the coastline again per sample — the same values <see cref="At"/> computes, read
        /// instead of recomputed, which is what the point/bulk equivalence test pins.
        /// </remarks>
        /// <param name="islandField"><c>H4A</c> on the lattice.</param>
        /// <param name="carveOut">Receives the applied carve per sample, metres, never negative.</param>
        public static WorldHeightField EvaluateField(
            Stage1Result stage1,
            WorldHeightField islandField,
            CarveField carve,
            RidgeSettings settings,
            out WorldHeightField carveOut)
        {
            var width = islandField.Width;
            var height = islandField.Height;
            var result = new WorldHeightField(
                islandField.OriginX, islandField.OriginZ, width, height, islandField.CellSize,
                new float[islandField.SampleCount]);
            carveOut = new WorldHeightField(
                islandField.OriginX, islandField.OriginZ, width, height, islandField.CellSize,
                new float[islandField.SampleCount]);

            for (var z = 0; z < height; z++)
            {
                var worldZ = islandField.WorldZ(z);
                var row = z * width;
                for (var x = 0; x < width; x++)
                {
                    var index = row + x;
                    var island = islandField.Heights[index];
                    if (carve == null)
                    {
                        result.Heights[index] = island;
                        continue;
                    }

                    var worldX = islandField.WorldX(x);
                    var inland = stage1.SignedDistance[index];
                    var raw = carve.Depth(worldX, worldZ, carve.GateFor(inland), inland > 0f);
                    var cut = CarveAt(raw, island, stage1.Field.Heights[index], settings);
                    carveOut.Heights[index] = cut;
                    result.Heights[index] = island - cut;
                }
            }

            return result;
        }
    }
}

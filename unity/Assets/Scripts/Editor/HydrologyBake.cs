#if UNITY_EDITOR
using System.Diagnostics;
using GameDev.World.Hydrology;
using GameDev.World.Terrain;
using UnityEngine;
using Debug = UnityEngine.Debug;
using WorldSeed = GameDev.World.Seed.WorldSeed;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Owns the two-pass handoff that turns baked terrain into carved terrain.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pass A bakes the relief exactly as it always does. The drainage analysis then reads that
    /// relief off the finished <c>TerrainData</c>, and pass B bakes again with the planned valleys
    /// subtracted from the terrain lattice before the surface, trees and ground cover are derived.
    /// </para>
    /// <para>
    /// Two passes, because where channels converge depends on the island silhouette and every massif
    /// together, so the analysis needs finished relief. The carve is one-way: it lowers the lattice
    /// downstream of TerrainGen and is never read back into it, and pass A does not know hydrology
    /// exists, so changing any hydrology setting cannot move a mountain.
    /// </para>
    /// </remarks>
    public static class HydrologyBake
    {
        /// <summary>The plan pass B should carve, or null for an ordinary bake.</summary>
        public static HydrologyDefinition PendingDefinition { get; private set; }

        /// <summary>
        /// Pass A's height field, kept because pass B destroys the terrain before it rebuilds it.
        /// </summary>
        /// <remarks>
        /// The carve depth at a point is the ground before the cut minus the planned bed, so the
        /// rasteriser needs the un-carved surface the plan was made on.
        /// </remarks>
        public static HydrologyGrid PendingGround { get; private set; }

        /// <summary>The deepest cut the last carve applied, metres.</summary>
        public static float LastDeepestCarveMeters { get; private set; }

        public static void SetPending(HydrologyDefinition definition, HydrologyGrid ground)
        {
            PendingDefinition = definition;
            PendingGround = ground;
        }

        public static void ClearPending()
        {
            PendingDefinition = null;
            PendingGround = null;
        }

        /// <summary>
        /// Runs the drainage analysis over the terrain currently in the scene.
        /// </summary>
        /// <remarks>
        /// Reads the scene and writes nothing. A failure is returned as a failure — the caller
        /// reports it and carves nothing, rather than a partial network being carved into the world.
        /// </remarks>
        public static HydrologyPlanner.Result Analyse(
            int seed, HydrologySettings settings, out HydrologyGrid grid)
        {
            var sampleWatch = Stopwatch.StartNew();
            grid = HydrologyTerrainGrid.BuildFromScene(settings.AnalysisStride);
            sampleWatch.Stop();

            if (grid == null)
            {
                return new HydrologyPlanner.Result(
                    HydrologyDefinition.Empty,
                    HydrologyPlanner.FailureReason.NoOcean,
                    "the terrain could not be read into an analysis grid");
            }

            var result = HydrologyPlanner.Plan(grid, settings, new WorldSeed(seed));
            OverworldGenTiming.Log(
                "hydrology.sample", sampleWatch.ElapsedMilliseconds,
                $"{grid.Width}x{grid.Height}@{grid.CellSize:F2}m");
            return result;
        }

        /// <summary>
        /// Lowers the terrain lattice by the pending plan. Does nothing when no plan is pending.
        /// </summary>
        /// <returns>Lattice vertices the carve moved.</returns>
        public static int ApplyPendingCarve(OverworldTerrainField field)
        {
            LastDeepestCarveMeters = 0f;
            if (field == null || PendingDefinition == null || PendingGround == null || PendingDefinition.IsEmpty)
            {
                return 0;
            }

            if (field.Width != field.Depth)
            {
                Debug.LogError(
                    $"[Hydrology] The carve rasteriser needs a square lattice; the world is {field.Width}x{field.Depth}. Nothing carved.");
                return 0;
            }

            var watch = Stopwatch.StartNew();

            // Expanded by half a step, so pixel centres (i + 0.5) land exactly on lattice vertices.
            var step = field.StepMeters;
            var rect = new Rect(-step * 0.5f, -step * 0.5f, field.Width * step, field.Depth * step);
            var carve = HydrologyCarveField.Rasterize(PendingDefinition, rect, field.Width, PendingGround.SampleHeightWorld);

            var deepest = 0f;
            for (var i = 0; i < carve.Length; i++)
            {
                deepest = Mathf.Max(deepest, carve[i]);
            }

            LastDeepestCarveMeters = deepest;
            var width = field.Width;
            var moved = field.ApplyCarve((x, z) => carve[z * width + x]);

            watch.Stop();
            OverworldGenTiming.Log("hydrology.carve", watch.ElapsedMilliseconds, $"vertices={moved} deepest={deepest:F1}m");
            return moved;
        }

        /// <summary>
        /// Checks that the terrain actually moved by the carve that was requested, and nowhere else.
        /// </summary>
        /// <remarks>
        /// Compares the height field before and after the carved bake against the carve field itself,
        /// rather than guessing which cells are "near a river". Two numbers come out of it: how far
        /// the requested cut and the delivered cut disagree, and how far the terrain moved where
        /// nothing was requested at all. Both should be a few centimetres.
        /// </remarks>
        public static bool VerifyCarve(
            HydrologyGrid before, HydrologyDefinition definition, out string message)
        {
            var after = HydrologyTerrainGrid.BuildFromScene();
            if (before == null || after == null
                || after.Width != before.Width || after.Height != before.Height)
            {
                message = "the terrain could not be re-read for comparison";
                return false;
            }

            // The same raster as ApplyPendingCarve: expanded by half a cell, so every pixel centre is a
            // grid vertex and the expected cut is evaluated exactly where the bake applied it.
            var half = before.CellSize * 0.5f;
            var worldRect = new Rect(
                before.OriginX - half,
                before.OriginZ - half,
                before.Width * before.CellSize,
                before.Height * before.CellSize);

            var expected = HydrologyCarveField.Rasterize(
                definition, worldRect, before.Width, before.SampleHeightWorld);

            var worstUnrequested = 0f;
            var worstUnrequestedAt = Vector2.zero;
            var worstError = 0f;
            var worstErrorAt = Vector2.zero;
            var deepestRequested = 0f;
            var deepestDelivered = 0f;

            for (var i = 0; i < before.CellCount; i++)
            {
                var want = expected[i];
                var got = before.Heights[i] - after.Heights[i];
                deepestRequested = Mathf.Max(deepestRequested, want);
                deepestDelivered = Mathf.Max(deepestDelivered, got);

                if (want <= 0.001f)
                {
                    var stray = Mathf.Abs(got);
                    if (stray > worstUnrequested)
                    {
                        worstUnrequested = stray;
                        worstUnrequestedAt = before.PositionXZ(i);
                    }

                    continue;
                }

                var error = Mathf.Abs(got - want);
                if (error > worstError)
                {
                    worstError = error;
                    worstErrorAt = before.PositionXZ(i);
                }
            }

            var summary =
                $"requested up to {deepestRequested:F1} m, delivered up to {deepestDelivered:F1} m; "
                + $"worst disagreement inside the carve {worstError:F2} m at {worstErrorAt}; "
                + $"worst movement outside it {worstUnrequested:F2} m at {worstUnrequestedAt}";

            // A metre is far more than rasterisation and 16-bit quantisation can account for, and far
            // less than any real defect would produce.
            if (worstUnrequested > 1f || worstError > 1f)
            {
                message = "the carve did not land as planned: " + summary;
                return false;
            }

            message = "carve verified — " + summary;
            return true;
        }

        /// <summary>The one-line generation report, matching the project's other step summaries.</summary>
        public static void Report(HydrologyDefinition definition, int seed, int waterMeshes)
        {
            Debug.Log(
                $"[Hydrology] seed={seed} {definition.Stats} "
                + $"waterMeshes={waterMeshes} deepestCarve={LastDeepestCarveMeters:F1}m");
        }
    }
}
#endif

#if UNITY_EDITOR
using System.Collections;
using System.Diagnostics;
using GameDev.World;
using GameDev.World.Hydrology;
using GameDev.World.Terrain;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GameDev.EditorTools
{
    /// <summary>
    /// The hydrology generation step: analyse the baked relief, carve it, then draw the water.
    /// </summary>
    /// <remarks>
    /// The step needs terrain to already exist, because drainage is read off finished relief. Run on
    /// an empty scene it reports that and stops — it never invents a landscape to analyse.
    /// </remarks>
    public static class HydrologyMenu
    {
        /// <summary>Live settings for the step. Held here for the same reason terrain settings are.</summary>
        public static HydrologySettings ActiveSettings { get; set; } = HydrologySettings.Default;

        /// <summary>Draw the diagnostic gizmos for the generated network.</summary>
        /// <remarks>Off by default: diagnostics must not be part of a production generation.</remarks>
        public static bool DrawDiagnostics { get; set; }

        [MenuItem("GameDev/Overworld/Hydrology/Generate Hydrology", false, 20)]
        public static void GenerateMenu()
        {
            var seed = OverworldTerrainMenu.LastSeed != 0
                ? OverworldTerrainMenu.LastSeed
                : OverworldTerrainMenu.DefaultSeed;
            EditorCoroutineUtility.StartCoroutineOwnerless(
                GenerateRoutine(seed, ActiveSettings, OverworldTerrainMenu.LastBakeFlags));
        }

        [MenuItem("GameDev/Overworld/Hydrology/Clear Hydrology", false, 21)]
        public static void ClearMenu()
        {
            Clear();
            Debug.Log(
                "[Hydrology] Cleared the water meshes and the stored definition. "
                + "The terrain keeps the valleys until it is re-baked.");
        }

        [MenuItem("GameDev/Overworld/Hydrology/Toggle Diagnostics Gizmos", false, 22)]
        public static void ToggleDiagnostics()
        {
            DrawDiagnostics = !DrawDiagnostics;
            var visualizer = Object.FindAnyObjectByType<HydrologyDiagnostics>();
            if (visualizer != null)
            {
                visualizer.enabled = DrawDiagnostics;
            }

            Debug.Log($"[Hydrology] Diagnostics gizmos {(DrawDiagnostics ? "on" : "off")}.");
        }

        /// <summary>
        /// Analyse, carve and draw. Yields, because carving is a second terrain bake.
        /// </summary>
        public static IEnumerator GenerateRoutine(
            int seed, HydrologySettings settings, TerrainBakeFlags flags)
        {
            settings.Clamp();
            var total = Stopwatch.StartNew();

            if (OverworldTerrainAccess.FindTerrains().Length == 0)
            {
                Debug.LogError(
                    "[Hydrology] There is no terrain to analyse. Drainage is read off finished "
                    + "relief, so bake the surface first.");
                yield break;
            }

            var result = HydrologyBake.Analyse(seed, settings, out var groundBeforeCarve);
            if (!result.Succeeded)
            {
                // Reported, not hidden, and nothing is carved. Fewer valid rivers beat invalid
                // geography — see HYDROLOGY_GENERATION_PLAN.md §11.
                Debug.LogError(
                    $"[Hydrology] No river network for seed {seed}: {result.Failure} — {result.Message}. "
                    + "Nothing was carved and no water was drawn.");
                HydrologyBake.ClearPending();
                yield break;
            }

            var definition = result.Definition;

            var state = OverworldWorldState.FindOrCreate();
            if (state.Definition == null)
            {
                state.SetDefinition(new WorldDefinition { Seed = seed, Settings = OverworldTerrainMenu.ActiveSettings });
            }

            state.Definition.SetHydrology(definition);

            HydrologyBake.SetPending(definition, groundBeforeCarve);
            var carveWatch = Stopwatch.StartNew();
            yield return OverworldTerrainMenu.GenerateSurfaceRoutine(seed, flags);
            carveWatch.Stop();
            HydrologyBake.ClearPending();

            OverworldGenTiming.Log("hydrology.carvebake", carveWatch.ElapsedMilliseconds);

            if (!HydrologyBake.VerifyCarve(groundBeforeCarve, definition, out var carveReport))
            {
                Debug.LogError($"[Hydrology] {carveReport}");
            }
            else
            {
                Debug.Log($"[Hydrology] {carveReport}");
            }

            // The ocean root has to exist before the inland meshes can be parented under it; the
            // terrain bake cleared it along with everything else derived from height.
            WaterZoneMenu.EnsureOceanOcWPlane();
            var meshes = BuildWaterMeshes(definition);

            EnsureDiagnostics(definition);
            HydrologyBake.Report(definition, seed, meshes);
            OverworldGenTiming.Log(
                "hydrology", total.ElapsedMilliseconds, $"seed={seed} meshes={meshes}");
        }

        /// <summary>Removes the water meshes and the stored definition.</summary>
        public static void Clear()
        {
            var root = GameObject.Find("OverworldWater");
            if (root != null)
            {
                var inland = root.transform.Find(InlandWaterMeshBuilder.InlandRootName);
                if (inland != null)
                {
                    Object.DestroyImmediate(inland.gameObject);
                }
            }

            var visualizer = Object.FindAnyObjectByType<HydrologyDiagnostics>();
            if (visualizer != null)
            {
                Object.DestroyImmediate(visualizer.gameObject);
            }

            HydrologyBake.ClearPending();

            var state = OverworldWorldState.Find();
            state?.Definition?.SetHydrology(HydrologyDefinition.Empty);
        }

        private static int BuildWaterMeshes(HydrologyDefinition definition)
        {
            var root = GameObject.Find("OverworldWater");
            if (root == null)
            {
                Debug.LogWarning(
                    "[Hydrology] No OverworldWater root, so inland water was not drawn.");
                return 0;
            }

            var existing = root.transform.Find(InlandWaterMeshBuilder.InlandRootName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            return InlandWaterMeshBuilder.Build(definition, root.transform);
        }

        private static void EnsureDiagnostics(HydrologyDefinition definition)
        {
            var visualizer = Object.FindAnyObjectByType<HydrologyDiagnostics>();
            if (visualizer == null)
            {
                var go = new GameObject("OverworldHydrologyDiagnostics");
                visualizer = go.AddComponent<HydrologyDiagnostics>();
            }

            visualizer.Bind(definition);
            visualizer.enabled = DrawDiagnostics;
        }
    }
}
#endif

#if UNITY_EDITOR
using System.Collections;
using GameDev.World;
using GameDev.World.Terrain;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Headless "bake one terrain, then measure it" entry point, for A/B experiments and for the
    /// multi-seed check the terrain acceptance criteria ask for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Terrain questions are answered by comparing two bakes, and comparing two bakes by hand is slow
    /// enough that it does not get done. This runs the terrain step (optionally hydrology and the
    /// dressing steps) and then dumps the geometry report.
    /// </para>
    /// <para>
    /// <b>Must run without <c>-quit</c>.</b> The pipeline steps are editor coroutines, so they need the
    /// editor loop to tick; this method exits the process itself when the work is done.
    /// </para>
    /// <example>
    /// <code>
    /// Unity.exe -batchmode -projectPath &lt;project&gt; \
    ///   -executeMethod GameDev.EditorTools.OverworldBatchDiagnostics.BakeAndDump \
    ///   -overworldSeed 110201 -overworldMountains true -logFile out.log
    /// </code>
    /// </example>
    /// </remarks>
    public static class OverworldBatchDiagnostics
    {
        private const string SeedArg = "-overworldSeed";
        private const string MountainsArg = "-overworldMountains";
        private const string ExportPngArg = "-overworldExportPng";
        private const string SkipBakeArg = "-overworldSkipBake";

        /// <summary>Run the hydrology step after the bake, so the dump measures carved terrain.</summary>
        private const string HydrologyArg = "-overworldHydrology";

        /// <summary>
        /// Bake the terrain twice in one process and report how far the second differs.
        /// </summary>
        /// <remarks>
        /// Hydrology is a two-pass system, so before any of its numbers can be trusted it has to be
        /// possible to ask whether two consecutive bakes of the same world already disagree.
        /// </remarks>
        private const string DoubleBakeArg = "-overworldDoubleBake";

        private const string DressingArg = "-overworldDressing";

        /// <summary>Generous ceiling for a terrain-only bake.</summary>
        private const double TimeoutSeconds = 900.0;

        public static void BakeAndDump()
        {
            var seed = ReadIntArg(SeedArg, OverworldTerrainMenu.DefaultSeed);
            var flags = ReadBoolArg(MountainsArg, defaultValue: true)
                ? TerrainBakeFlags.IncludeMountains
                : TerrainBakeFlags.None;
            var exportPng = ReadBoolArg(ExportPngArg, defaultValue: false);
            var skipBake = ReadBoolArg(SkipBakeArg, defaultValue: false);

            Debug.Log(
                $"[BatchDiag] seed={seed} flags={flags} exportPng={exportPng} skipBake={skipBake} " +
                $"hydrology={ReadBoolArg(HydrologyArg, defaultValue: false)}");
            EditorCoroutineUtility.StartCoroutineOwnerless(Run(seed, flags, exportPng, skipBake));
        }

        private static IEnumerator Run(int seed, TerrainBakeFlags flags, bool exportPng, bool skipBake)
        {
            var exitCode = 0;
            var started = EditorApplication.timeSinceStartup;

            OverworldTerrainMenu.OpenOrCreateScene();

            if (skipBake)
            {
                // The bake is what normally records the seed, so without this the dressing below
                // would plan POIs from seed 0 and quietly measure a different world.
                Debug.Log("[BatchDiag] Skipping the bake; measuring the existing terrain.");
                OverworldTerrainMenu.SetLastSeedForBatchDiagnostics(seed);
            }
            else
            {
                yield return OverworldTerrainMenu.GenerateSurfaceRoutine(seed, flags);

                if (EditorApplication.timeSinceStartup - started > TimeoutSeconds)
                {
                    Debug.LogError("[BatchDiag] Bake exceeded the timeout; the dump below may be partial.");
                    exitCode = 1;
                }
            }

            if (ReadBoolArg(DoubleBakeArg, defaultValue: false))
            {
                var before = HydrologyTerrainGrid.BuildFromScene();
                yield return OverworldTerrainMenu.GenerateSurfaceRoutine(seed, flags);
                var after = HydrologyTerrainGrid.BuildFromScene();

                if (before == null || after == null || before.CellCount != after.CellCount)
                {
                    Debug.LogError("[BatchDiag] double bake: the terrain could not be compared.");
                }
                else
                {
                    var worst = 0f;
                    var worstAt = Vector2.zero;
                    var sum = 0.0;
                    for (var i = 0; i < before.CellCount; i++)
                    {
                        var delta = Mathf.Abs(after.Heights[i] - before.Heights[i]);
                        sum += delta;
                        if (delta > worst)
                        {
                            worst = delta;
                            worstAt = before.PositionXZ(i);
                        }
                    }

                    Debug.Log(
                        $"[BatchDiag] double bake, no hydrology: max delta {worst:F2} m at {worstAt}, " +
                        $"mean {sum / before.CellCount:F3} m over {before.CellCount} cells.");
                }
            }

            // Between the bake and the dressing, exactly where the pipeline puts it: the carve moves
            // the ground that POIs and settlements are then placed against.
            if (ReadBoolArg(HydrologyArg, defaultValue: false))
            {
                var hydrologyStart = EditorApplication.timeSinceStartup;
                yield return HydrologyMenu.GenerateRoutine(seed, HydrologyMenu.ActiveSettings, flags);
                Debug.Log($"[BatchDiag] hydrology done in {EditorApplication.timeSinceStartup - hydrologyStart:F0}s");
            }

            // POI -> settlement -> roads -> markers -> player, in the pipeline's own order: roads must
            // follow the settlement so they can route to where the fabric actually landed.
            if (ReadBoolArg(DressingArg, defaultValue: false))
            {
                var dressingStart = EditorApplication.timeSinceStartup;
                OverworldWorldPlan.BuildAndStorePois();
                SettlementOverworldMenu.PlaceSettlementAtTownApproach();
                RoadNetworkMenu.BuildRoadNetworkWithLandscapePlanner();
                PoiMarkerMenu.PlaceMarkers();
                OverworldPlayerMenu.SpawnPlayerAtTown();
                Debug.Log($"[BatchDiag] dressing done in {EditorApplication.timeSinceStartup - dressingStart:F0}s");
            }

            OverworldTerrainDiagnostics.Dump(exportPng);

            Debug.Log($"[BatchDiag] done in {EditorApplication.timeSinceStartup - started:F0}s seed={seed} flags={flags}");
            EditorApplication.Exit(exitCode);
        }

        /// <summary>
        /// Regenerates the overworld scene through Generate All with the default settings and saves it.
        /// </summary>
        /// <remarks>
        /// The scene is generated output, so it is rebuilt by the pipeline rather than edited. Same
        /// command line as <see cref="BakeAndDump"/>; only <c>-overworldSeed</c> is read.
        /// </remarks>
        public static void GenerateAllAndSave()
        {
            var seed = ReadIntArg(SeedArg, OverworldTerrainMenu.DefaultSeed);
            Debug.Log($"[BatchDiag] generate all and save, seed={seed}");
            EditorCoroutineUtility.StartCoroutineOwnerless(RunGenerateAllAndSave(seed));
        }

        private static IEnumerator RunGenerateAllAndSave(int seed)
        {
            var started = EditorApplication.timeSinceStartup;
            OverworldTerrainMenu.OpenOrCreateScene();
            OverworldPipeline.GenerateAll(seed, WorldGenSettings.Default);

            while (OverworldPipeline.IsGenerateAllRunning)
            {
                if (EditorApplication.timeSinceStartup - started > TimeoutSeconds)
                {
                    Debug.LogError("[BatchDiag] Generate All exceeded the timeout; the scene was not saved.");
                    EditorApplication.Exit(1);
                    yield break;
                }

                yield return null;
            }

            var scene = SceneManager.GetActiveScene();
            var saved = EditorSceneManager.SaveScene(scene);
            Debug.Log(
                $"[BatchDiag] generate all done in {EditorApplication.timeSinceStartup - started:F0}s " +
                $"seed={seed} saved={saved} scene={scene.path}");
            EditorApplication.Exit(saved ? 0 : 1);
        }

        private static int ReadIntArg(string name, int fallback)
        {
            var raw = ReadArg(name);
            return int.TryParse(raw, out var value) ? value : fallback;
        }

        private static bool ReadBoolArg(string name, bool defaultValue)
        {
            var raw = ReadArg(name);
            return bool.TryParse(raw, out var value) ? value : defaultValue;
        }

        private static string ReadArg(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
#endif

#if UNITY_EDITOR
using System.Collections;
using GameDev.World.Terrain;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Headless "bake one terrain, then measure it" entry point, for A/B experiments and for the
    /// multi-seed check the terrain acceptance criteria ask for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Terrain questions are answered by comparing two bakes, and comparing two bakes by hand is
    /// slow enough that it does not get done — which is how "the striping is the clamped erosion
    /// sim" survived in the documentation without anyone measuring it. This runs the terrain step
    /// only (no POI, settlement or roads, which are ~90 % of a full Generate All) and then dumps the
    /// geometry report.
    /// </para>
    /// <para>
    /// <b>Must run without <c>-quit</c>.</b> The Vista bake is an editor coroutine, so it needs the
    /// editor loop to tick; <c>-quit</c> would exit the moment this method returns and kill it
    /// mid-generation. This method exits the process itself when the bake and the dump are done.
    /// </para>
    /// <example>
    /// <code>
    /// Unity.exe -batchmode -nographics -projectPath &lt;project&gt; \
    ///   -executeMethod GameDev.EditorTools.OverworldBatchDiagnostics.BakeAndDump \
    ///   -overworldSeed 110201 -overworldMountains false -logFile out.log
    /// </code>
    /// </example>
    /// </remarks>
    public static class OverworldBatchDiagnostics
    {
        private const string SeedArg = "-overworldSeed";
        private const string MountainsArg = "-overworldMountains";
        private const string RebuildArg = "-overworldRebuildGraphs";
        private const string BypassErosionArg = "-overworldBypassErosion";
        private const string NoiseDerivativeArg = "-overworldNoiseDerivative";
        private const string ExportPngArg = "-overworldExportPng";
        private const string SkipBakeArg = "-overworldSkipBake";
        private const string DemoGraphArg = "-overworldDemoGraph";
        private const string BypassShelfArg = "-overworldBypassShelfCurve";
        private const string RefShapingArg = "-overworldReferenceShaping";
        private const string ProbeArg = "-overworldNoiseProbe";
        private const string DetailScaleArg = "-overworldDetailScale";
        private const string NoiseOffsetArg = "-overworldNoiseOffset";
        private const string DressingArg = "-overworldDressing";
        private const string NoSilhouetteArg = "-overworldIslandNoSilhouette";
        private const string NoRemapArg = "-overworldNoiseNoRemapPerLayer";
        private const string NoFlipArg = "-overworldNoiseNoFlipSign";
        private const string OctavesArg = "-overworldNoiseOctaves";
        private const string LacunarityArg = "-overworldNoiseLacunarity";
        private const string WarpArg = "-overworldNoiseWarp";
        private const string WarpIntensityArg = "-overworldNoiseWarpIntensity";
        private const string LayerDumpArg = "-overworldLayerDump";
        private const string PostStackArg = "-overworldPostStackNormalise";
        private const string PostStackCurveArg = "-overworldPostStackCurve";
        private const string RefDetailArg = "-overworldReferenceDetailNoise";
        private const string IslandHeightArg = "-overworldIslandWorldHeight";
        private const string NoSeaFloorArg = "-overworldSkipSeaFloor";
        private const string BiomeWidthArg = "-overworldIslandBiomeWidth";
        private const string NoMaskGraphArg = "-overworldSkipIslandMaskGraph";

        /// <summary>Generous ceiling; a terrain-only bake is minutes, not tens of minutes.</summary>
        private const double TimeoutSeconds = 900.0;

        public static void BakeAndDump()
        {
            var seed = ReadIntArg(SeedArg, OverworldTerrainMenu.DefaultSeed);
            var mountains = ReadBoolArg(MountainsArg, defaultValue: true);
            var rebuild = ReadBoolArg(RebuildArg, defaultValue: false);

            // The control that tells our graphs apart from the pipeline: bake Vista's own sample
            // graph over the same tiles and measure it the same way. If the reference shows the
            // same artifact, the artifact is not ours.
            var flags = ReadBoolArg(DemoGraphArg, defaultValue: false)
                ? TerrainBakeFlags.VistaDemoAsIs
                : mountains
                    ? TerrainBakeFlags.IncludeMountains
                    : TerrainBakeFlags.None;

            // Only meaningful together with a rebuild: these are read while the graph is authored.
            VistaGraphBuild.BypassErosionForDiagnostics = ReadBoolArg(BypassErosionArg, false);
            VistaGraphBuild.NoiseDerivativeOverride = ReadNoiseDerivativeArg();
            IslandPlateauGraphBuilder.BypassShelfCurveForDiagnostics =
                ReadBoolArg(BypassShelfArg, false);
            IslandPlateauGraphBuilder.PostStackNormaliseForDiagnostics =
                ReadBoolArg(PostStackArg, false);
            IslandPlateauGraphBuilder.ReferenceDetailNoiseForDiagnostics =
                ReadBoolArg(RefDetailArg, false);
            IslandPlateauGraphBuilder.PostStackCurveForDiagnostics =
                System.Enum.TryParse<IslandPlateauGraphBuilder.PostStackCurve>(
                    ReadArg(PostStackCurveArg) ?? "", ignoreCase: true, out var curveMode)
                    ? curveMode
                    : IslandPlateauGraphBuilder.PostStackCurve.Shelf;
            IslandPlateauGraphBuilder.ReferenceShapingForDiagnostics =
                ReadBoolArg(RefShapingArg, false);
            IslandPlateauGraphBuilder.NoiseProbeForDiagnostics =
                System.Enum.TryParse<IslandPlateauGraphBuilder.NoiseProbe>(
                    ReadArg(ProbeArg) ?? "", ignoreCase: true, out var probeMode)
                    ? probeMode
                    : IslandPlateauGraphBuilder.NoiseProbe.Off;
            IslandPlateauGraphBuilder.DetailNoiseScaleForDiagnostics =
                float.TryParse(ReadArg(DetailScaleArg), out var ds) ? ds : (float?)null;
            IslandPlateauGraphBuilder.NoiseOffsetForDiagnostics =
                float.TryParse(ReadArg(NoiseOffsetArg), out var off)
                    ? new UnityEngine.Vector2(off, off)
                    : (UnityEngine.Vector2?)null;
            OverworldLpbBake.SkipIslandMaskGraphForDiagnostics =
                ReadBoolArg(NoMaskGraphArg, false);
            OverworldLpbBake.IslandBiomeWidthForDiagnostics =
                float.TryParse(ReadArg(BiomeWidthArg), out var bw) ? bw : (float?)null;
            OverworldLpbBake.SkipSeaFloorForDiagnostics = ReadBoolArg(NoSeaFloorArg, false);
            OverworldLpbBake.IslandWorldHeightForDiagnostics =
                float.TryParse(ReadArg(IslandHeightArg), out var ih) ? ih : (float?)null;
            OverworldLpbBake.IslandWithoutSilhouetteForDiagnostics =
                ReadBoolArg(NoSilhouetteArg, false);
            VistaGraphBuild.DisableNoiseRemapPerLayerForDiagnostics = ReadBoolArg(NoRemapArg, false);
            VistaGraphBuild.DisableNoiseFlipSignForDiagnostics = ReadBoolArg(NoFlipArg, false);
            var octavesRaw = ReadArg(OctavesArg);
            VistaGraphBuild.NoiseLayerCountForDiagnostics =
                int.TryParse(octavesRaw, out var oct) ? oct : (int?)null;
            VistaGraphBuild.NoiseLacunarityForDiagnostics =
                float.TryParse(ReadArg(LacunarityArg), out var lac) ? lac : (float?)null;
            var warpRaw = ReadArg(WarpArg);
            if (!string.IsNullOrEmpty(warpRaw) &&
                System.Enum.TryParse<Pinwheel.Vista.Graph.NoiseNode.WarpMode>(
                    warpRaw, ignoreCase: true, out var warpMode))
            {
                VistaGraphBuild.NoiseWarpForDiagnostics = warpMode;
                VistaGraphBuild.NoiseWarpIntensityForDiagnostics =
                    float.TryParse(ReadArg(WarpIntensityArg), out var wi) ? wi : 1f;
            }

            var exportPng = ReadBoolArg(ExportPngArg, defaultValue: false);
            var skipBake = ReadBoolArg(SkipBakeArg, defaultValue: false);

            Debug.Log(
                $"[BatchDiag] seed={seed} mountains={mountains} rebuildGraphs={rebuild} " +
                $"bypassErosion={VistaGraphBuild.BypassErosionForDiagnostics} " +
                $"noiseDerivative={VistaGraphBuild.NoiseDerivativeOverride?.ToString() ?? "default"} " +
                $"bypassShelfCurve={IslandPlateauGraphBuilder.BypassShelfCurveForDiagnostics} " +
                $"exportPng={exportPng} skipBake={skipBake} flags={flags}");
            EditorCoroutineUtility.StartCoroutineOwnerless(
                Run(seed, flags, rebuild, exportPng, skipBake));
        }

        private static IEnumerator Run(
            int seed, TerrainBakeFlags flags, bool rebuildGraphs, bool exportPng, bool skipBake)
        {
            var exitCode = 0;
            var started = EditorApplication.timeSinceStartup;

            // Changing a *GraphBuilder does nothing until the asset is re-authored from it, so an
            // A/B on graph parameters has to rebuild or it silently measures the old graph twice.
            if (rebuildGraphs)
            {
                Debug.LogWarning(
                    "[BatchDiag] Rebuilding terrain graph assets from code — this discards any edit " +
                    "made in the Vista graph editor.");
                if (VistaGraphBuild.BypassErosionForDiagnostics)
                {
                    Debug.LogWarning(
                        "[BatchDiag] Erosion nodes authored BYPASSED. Geometry is the shaping stage " +
                        "alone and the surface weights are meaningless — restore the graph assets " +
                        "before using this world for anything.");
                }

                IslandPlateauGraphBuilder.Rebuild();
                MountainMassifGraphBuilder.Rebuild();
            }

            OverworldTerrainMenu.OpenOrCreateScene();

            if (skipBake)
            {
                // Measure the terrain that is already on disk — useful for exporting images from a
                // bake that has already been paid for.
                Debug.Log("[BatchDiag] Skipping the bake; measuring the existing terrain.");

                // The bake is what normally records the seed, so without this the dressing below
                // would plan POIs from seed 0 and quietly measure a different world.
                OverworldTerrainMenu.SetLastSeedForBatchDiagnostics(seed);
            }
            else
            {
                // The bake routine is what the pipeline itself yields on, so this measures the same
                // terrain a normal Generate All would produce.
                yield return OverworldTerrainMenu.GenerateSurfaceRoutine(seed, flags);

                if (EditorApplication.timeSinceStartup - started > TimeoutSeconds)
                {
                    Debug.LogError(
                        "[BatchDiag] Bake exceeded the timeout; the dump below may be partial.");
                    exitCode = 1;
                }
            }

            // POI -> settlement -> roads -> player. All synchronous, unlike the Vista bake, and run
            // in the pipeline's own order: roads must follow the settlement so they can route to
            // where the fabric actually landed.
            if (ReadBoolArg(DressingArg, defaultValue: false))
            {
                var dressingStart = EditorApplication.timeSinceStartup;
                OverworldWorldPlan.BuildAndStorePois();
                SettlementOverworldMenu.PlaceSettlementAtTownApproach();
                RoadNetworkMenu.BuildRoadNetworkWithLandscapePlanner();
                PoiMarkerMenu.PlaceMarkers();
                OverworldPlayerMenu.SpawnPlayerAtTown();
                Debug.Log(
                    $"[BatchDiag] dressing done in " +
                    $"{EditorApplication.timeSinceStartup - dressingStart:F0}s");
            }

            OverworldTerrainDiagnostics.Dump(exportPng);

            // Runs here rather than as its own batch invocation because VistaLayerWeightDump does
            // not open the overworld scene itself — on its own it reports "no terrains".
            if (ReadBoolArg(LayerDumpArg, defaultValue: false))
            {
                VistaLayerWeightDump.Dump(exportPng);
            }

            Debug.Log(
                $"[BatchDiag] done in {EditorApplication.timeSinceStartup - started:F0}s " +
                $"seed={seed} flags={flags}");
            EditorApplication.Exit(exitCode);
        }

        private static Pinwheel.Vista.Graph.NoiseNode.LayerDerivativeMode? ReadNoiseDerivativeArg()
        {
            var raw = ReadArg(NoiseDerivativeArg);
            if (string.IsNullOrEmpty(raw))
            {
                return null;
            }

            if (System.Enum.TryParse<Pinwheel.Vista.Graph.NoiseNode.LayerDerivativeMode>(
                    raw, ignoreCase: true, out var mode))
            {
                return mode;
            }

            Debug.LogWarning($"[BatchDiag] Unknown {NoiseDerivativeArg} '{raw}' — using the default.");
            return null;
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

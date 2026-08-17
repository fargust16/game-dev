#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Reflection;
using GameDev.World;
using GameDev.World.Seed;
using GameDev.World.Terrain;
using Pinwheel.Vista;
using Pinwheel.Vista.Graph;
using Pinwheel.Vista.UnityTerrain;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using WorldSeed = GameDev.World.Seed.WorldSeed;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Overworld terrain bake: 2Ã—2 tiles + multi-LPB stack (island base + Mountain LPBs) + water.
    /// </summary>
    public static class OverworldTerrainMenu
    {
        public const string ScenePath = "Assets/Scenes/Overworld_Generator.unity";
        private const string TerrainMaterialPath = "Assets/Settings/OverworldTerrain.mat";
        private const string SkyboxDaytimePath = "Assets/Skybox/Materials/Skybox_Daytime.mat";
        private const string VistaGraphsFolder = "Assets/Settings/Vista";
        private const string DemoMountainRangeSourcePath =
            "Assets/PinwheelStudio/Vista/Personal/Samples/Graphs/MountainRange.asset";
        private const string DemoMountainRangeWorkingPath =
            "Assets/Settings/Vista/Demo_MountainRange_AsIs.asset";

        public const string TerrainShaderName = "Universal Render Pipeline/Terrain/Lit";
        public const string DeprecatedOverworldTerrainShaderName = "GameDev/Overworld/Terrain";
        public const int DefaultSeed = 110201;

        public static int LastSeed { get; private set; }

        /// <summary>
        /// Records the seed without baking, for the batch harness's measure-only mode.
        /// </summary>
        /// <remarks>
        /// The bake is what normally sets <see cref="LastSeed"/>, and every downstream planner reads
        /// it. Measuring an existing terrain and then dressing it would otherwise plan from seed 0.
        /// </remarks>
        public static void SetLastSeedForBatchDiagnostics(int seed) => LastSeed = seed;
        public static WorldGenSettings ActiveSettings { get; set; } = WorldGenSettings.Default;
        public static TerrainBakeFlags LastBakeFlags { get; private set; } = TerrainBakeFlags.Full;

        [MenuItem("GameDev/Overworld/Open Or Create Terrain Scene")]
        public static void OpenOrCreateScene()
        {
            EnsureSceneFolder();
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            else
            {
                CreateFreshSceneAndGenerate(DefaultSeed);
            }
        }

        [MenuItem("GameDev/Overworld/Generate Terrain From Seed")]
        public static void GenerateFromSeed()
        {
            EnsureSceneActive();
            GenerateInternal(DefaultSeed);
        }

        [MenuItem("GameDev/Overworld/Generate Terrain Random Seed")]
        public static void GenerateRandomSeed()
        {
            EnsureSceneActive();
            GenerateInternal(System.Environment.TickCount);
        }

        [MenuItem("GameDev/Overworld/Clear Terrain")]
        public static void ClearTerrain()
        {
            OverworldPipeline.ClearAll();
        }

        /// <summary>
        /// Diagnostic: Vista sample MountainRange on 2Ã—2 tiles, no C# framing/lakes/StampIT.
        /// </summary>
        [MenuItem("GameDev/Overworld/Vista/Bake Demo MountainRange As-Is (2x2)")]
        public static void BakeDemoMountainRangeAsIs()
        {
            EnsureSceneActive();
            EditorCoroutineUtility.StartCoroutineOwnerless(
                GenerateSurfaceRoutine(DefaultSeed, TerrainBakeFlags.VistaDemoAsIs));
        }

        public static void ClearTerrainOnly()
        {
            var manager = Object.FindAnyObjectByType<VistaManager>();
            if (manager != null)
            {
                // Cancel in-flight editor coroutines before destroying the manager (avoids NRE in ProcessBiomesProgressive).
                manager.CancelGeneration();
                Object.DestroyImmediate(manager.gameObject);
            }

            var terrainsRoot = GameObject.Find(OverworldTerrainAccess.TerrainsRootName);
            if (terrainsRoot != null)
            {
                Object.DestroyImmediate(terrainsRoot);
            }

            foreach (var terrain in Object.FindObjectsByType<UnityEngine.Terrain>())
            {
                if (terrain != null)
                {
                    Object.DestroyImmediate(terrain.gameObject);
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static void CreateFreshSceneAndGenerate(int seed)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateSunLight();
            ApplySkyboxAndEnvironment();
            EditorCoroutineUtility.StartCoroutineOwnerless(GenerateSurfaceRoutine(seed, TerrainBakeFlags.Full));
            // Scene save happens after first generate completes inside the routine when path is empty â€” keep immediate save of empty+sun.
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
        }

        private static void CreateSunLight()
        {
            var existing = Object.FindAnyObjectByType<Light>();
            Light light;
            if (existing != null && existing.type == LightType.Directional)
            {
                light = existing;
            }
            else
            {
                var lightObject = new GameObject("Directional Light");
                light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
            }

            light.name = "Sun";
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.useColorTemperature = true;
            light.colorTemperature = 5800f;
            light.color = Color.white;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.85f;
            // Align with Free HDR Sky daytime sun disk (calibrated constant).
            light.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            RenderSettings.sun = light;
            EnsureOverworldVolume();
            EnsureReflectionProbe();
        }

        private static void ApplySkyboxAndEnvironment()
        {
            var sky = AssetDatabase.LoadAssetAtPath<Material>(SkyboxDaytimePath);
            if (sky != null)
            {
                // Rotate so HDRI sun disk matches Directional Light (~-35Â° yaw).
                if (sky.HasProperty("_Rotation"))
                {
                    sky.SetFloat("_Rotation", 325f);
                    EditorUtility.SetDirty(sky);
                }

                RenderSettings.skybox = sky;
            }

            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.fog = false;
            RenderSettings.fogDensity = 0f;
            DynamicGI.UpdateEnvironment();
        }

        private static void EnsureOverworldVolume()
        {
            const string volumeName = "OverworldVolume";
            const string profilePath = "Assets/Settings/OverworldVolumeProfile.asset";

            var go = GameObject.Find(volumeName);
            Volume volume;
            if (go == null)
            {
                go = new GameObject(volumeName);
                volume = go.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 10f;
            }
            else
            {
                volume = go.GetComponent<Volume>() ?? go.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 10f;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "OverworldVolumeProfile";
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            if (!profile.TryGet(out ColorAdjustments colorAdj))
            {
                colorAdj = profile.Add<ColorAdjustments>(true);
            }

            colorAdj.active = true;
            colorAdj.contrast.overrideState = true;
            colorAdj.contrast.value = 6f;
            colorAdj.saturation.overrideState = true;
            colorAdj.saturation.value = 6f;
            colorAdj.postExposure.overrideState = true;
            colorAdj.postExposure.value = 0.02f;

            if (!profile.TryGet(out WhiteBalance whiteBalance))
            {
                whiteBalance = profile.Add<WhiteBalance>(true);
            }

            whiteBalance.active = true;
            whiteBalance.temperature.overrideState = true;
            whiteBalance.temperature.value = 6f;

            if (!profile.TryGet(out Tonemapping tonemap))
            {
                tonemap = profile.Add<Tonemapping>(true);
            }

            tonemap.active = true;
            tonemap.mode.overrideState = true;
            tonemap.mode.value = TonemappingMode.ACES;

            if (!profile.TryGet(out Bloom bloom))
            {
                bloom = profile.Add<Bloom>(true);
            }

            bloom.active = true;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.18f;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.9f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.7f;

            if (profile.TryGet(out ShadowsMidtonesHighlights smh))
            {
                smh.active = false;
            }

            volume.sharedProfile = profile;
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            RenderSettings.fog = false;
        }

        private static void EnsureReflectionProbe()
        {
            const string name = "OverworldReflectionProbe";
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name);
                var probe = go.AddComponent<ReflectionProbe>();
                probe.mode = ReflectionProbeMode.Realtime;
                probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
                probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
                probe.size = new Vector3(
                    WorldScale.WorldExtentXMeters,
                    WorldScale.TerrainHeightBudgetMeters * 2f,
                    WorldScale.WorldExtentZMeters);
                probe.center = new Vector3(
                    WorldScale.WorldExtentXMeters * 0.5f,
                    WorldScale.SeaLevelMeters,
                    WorldScale.WorldExtentZMeters * 0.5f);
                go.transform.position = probe.center;
            }
        }

        private static void EnsureReviewCamera()
        {
            if (Object.FindAnyObjectByType<Camera>() != null)
            {
                return;
            }

            var cameraObject = new GameObject("Review Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.farClipPlane = 6000f;
            camera.clearFlags = CameraClearFlags.Skybox;
            cameraObject.transform.position = new Vector3(
                WorldScale.WorldExtentXMeters * 0.5f,
                2800f,
                WorldScale.WorldExtentZMeters * 0.5f);
            cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private static void GenerateInternal(int seed)
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(GenerateSurfaceRoutine(seed, TerrainBakeFlags.Full));
        }

        /// <summary>
        /// Vista MountainBiome bake (Anchors + Outer falloff). Cascade-clears lakes/POI/roads/veg.
        /// Fire-and-forget for menu/step callers; pipeline should <c>yield return</c>
        /// <see cref="GenerateSurfaceRoutine"/> so later steps wait for Vista.
        /// </summary>
        public static void GenerateSurface(int seed, TerrainBakeFlags flags)
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(GenerateSurfaceRoutine(seed, flags));
        }

        /// <summary>Coroutine entry: waits for Vista editor generation before post-process.</summary>
        public static IEnumerator GenerateSurfaceRoutine(int seed, TerrainBakeFlags flags)
        {
            LastSeed = seed;
            LastBakeFlags = flags;
            var total = OverworldGenTiming.Start();

            EnsureReviewCamera();
            CreateSunLight();
            ApplySkyboxAndEnvironment();

            WaterZoneMenu.ClearWaterZones();
            PoiMarkerMenu.ClearMarkers();
            RoadNetworkMenu.ClearRoadNetwork();
            SettlementOverworldMenu.ClearSettlement();
            OverworldPlayerMenu.ClearPlayer();

            var vistaSw = OverworldGenTiming.Start();
            yield return GenerateVistaTerrainRoutine(seed, flags);
            OverworldGenTiming.Log("terrain.vista", OverworldGenTiming.StopMs(vistaSw));

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            OverworldGenTiming.Log("terrain.total", OverworldGenTiming.StopMs(total), $"seed={seed} flags={flags}");
        }

        private static IEnumerator GenerateVistaTerrainRoutine(int seed, TerrainBakeFlags flags)
        {
            ClearTerrainOnly();

            var manager = VistaManager.CreateInstanceInScene();
            manager.name = OverworldTerrainAccess.VistaManagerName;
            manager.terrainMaxHeight = WorldScale.TerrainHeightBudgetMeters;
            manager.heightMapResolution = 513;
            manager.textureResolution = 512;
            EnsureVistaUnityEvents(manager);

            var system = new UnityTerrainSystem();

            var holder = new GameObject(OverworldTerrainAccess.TerrainsRootName);
            holder.transform.position = Vector3.zero;

            var tileCount = new Vector2Int(WorldScale.OverworldTileCountX, WorldScale.OverworldTileCountZ);
            var context = new TerrainGridCreationContext(
                tileCount,
                new Vector3(
                    WorldScale.OverworldTileMeters,
                    WorldScale.TerrainHeightBudgetMeters,
                    WorldScale.OverworldTileMeters),
                holder.transform);
            var grid = system.CreateTerrainGrid(context);

            EnsureVistaFolder();

            // Pass 1: resolution then pin tile metres (Unity keeps m/px on res change:
            // 1400@33 â†’ 22400@513 if size is not reset after heightmapResolution).
            for (var tz = 0; tz < tileCount.y; tz++)
            {
                for (var tx = 0; tx < tileCount.x; tx++)
                {
                    var terrainGo = grid[tx, tz];
                    if (terrainGo == null)
                    {
                        continue;
                    }

                    var tileTerrain = terrainGo.GetComponent<UnityEngine.Terrain>();
                    if (tileTerrain == null || tileTerrain.terrainData == null)
                    {
                        continue;
                    }

                    TerrainHeightWrite.ForceTileResolutionAndSize(tileTerrain, 513);
                    terrainGo.name = $"OverworldTerrain_{tx}_{tz}";
                    terrainGo.transform.localPosition = new Vector3(
                        tx * WorldScale.OverworldTileMeters,
                        0f,
                        tz * WorldScale.OverworldTileMeters);
                }
            }

            // Pass 2: register with Vista + material + persist TerrainData.
            for (var tz = 0; tz < tileCount.y; tz++)
            {
                for (var tx = 0; tx < tileCount.x; tx++)
                {
                    var terrainGo = grid[tx, tz];
                    if (terrainGo == null)
                    {
                        continue;
                    }

                    system.SetupTile(manager, terrainGo);
                    var tileTerrain = terrainGo.GetComponent<UnityEngine.Terrain>();
                    if (tileTerrain == null || tileTerrain.terrainData == null)
                    {
                        continue;
                    }

                    TerrainHeightWrite.ForceTileResolutionAndSize(tileTerrain, 513);
                    AssignOverworldTerrainMaterial(tileTerrain);

                    var dataPath = $"{VistaGraphsFolder}/OverworldTerrainData_{tx}_{tz}.asset";
                    var existing = AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);
                    if (existing != null)
                    {
                        AssetDatabase.DeleteAsset(dataPath);
                    }

                    AssetDatabase.CreateAsset(tileTerrain.terrainData, dataPath);
                }
            }

            // Remove legacy single-tile terrain data asset if present.
            var legacyData = $"{VistaGraphsFolder}/OverworldTerrainData.asset";
            if (AssetDatabase.LoadAssetAtPath<TerrainData>(legacyData) != null)
            {
                AssetDatabase.DeleteAsset(legacyData);
            }

            VistaDetailDensityRelay.Reset();
            SpawnVistaBiomes(manager, seed, flags);
            yield return WaitForVistaGeneration(manager);

            var terrains = OverworldTerrainAccess.FindTerrains();

            // Read-back only. Vista owns the heightmap; this loop pulls it off the GPU, pins the
            // resolution Unity expects, and measures the result so a mis-authored biome height is
            // reported instead of silently corrected.
            var globalMaxY = 0f;
            for (var i = 0; i < terrains.Length; i++)
            {
                var terrain = terrains[i];
                if (terrain == null)
                {
                    continue;
                }

                TerrainHeightWrite.SyncFromGpu(terrain);
                TerrainHeightWrite.ForceTileResolutionAndSize(terrain, 513);
                TerrainHeightWrite.LogRange(terrain, $"after-vista[{i}]");

                if (terrain.terrainData != null)
                {
                    EditorUtility.SetDirty(terrain.terrainData);
                    var sizeY = terrain.terrainData.size.y;
                    var res = terrain.terrainData.heightmapResolution;
                    var heights = terrain.terrainData.GetHeights(0, 0, res, res);
                    for (var z = 0; z < res; z++)
                    {
                        for (var x = 0; x < res; x++)
                        {
                            globalMaxY = Mathf.Max(globalMaxY, heights[z, x] * sizeY);
                        }
                    }
                }
            }

            // Reported, never corrected. The expected peak is the plateau's Set Biome Height On
            // Spawn plus the massif's, both authored on the template prefabs; if this number looks
            // wrong, those two values (or the graphs) are what to change.
            Debug.Log($"[Overworld] Global max terrain height after Vista = {globalMaxY:F0}m");

            // Seams are Vista's own job: TerrainTile.MatchSeams averages shared height and alphamap
            // edges against each neighbour. Nothing here re-shapes or re-paints the tiles.
            MatchVistaSeams(terrains);
            ApplyTerrainRenderDistances(terrains);

            // Vista's own detail writer loses the density map in the editor; this writes the map it
            // produced. See VistaDetailDensityRelay for what was measured before adding it.
            VistaDetailDensityRelay.Apply();

            Debug.Log(
                $"[Overworld] Vista bake done: tiles={terrains.Length} " +
                $"extent={WorldScale.WorldExtentXMeters:F0}m " +
                $"height={WorldScale.TerrainHeightBudgetMeters:F0} " +
                "(terrain shape, surface weights and vegetation all from the terrain graphs)");

            VistaVegetationDump.LogSummary(terrains);
            EnsureOceanSurface();
        }

        /// <summary>
        /// Averages shared tile edges through Vista's own <c>TerrainTile.MatchSeams</c>.
        /// </summary>
        /// <remarks>
        /// Neighbour wiring has to happen first — <c>MatchSeams</c> reads <c>Terrain.leftNeighbor</c>
        /// and friends to decide which edges to blend.
        /// </remarks>
        /// <summary>
        /// Draw distances for the terrain's own tree and detail renderers.
        /// </summary>
        /// <remarks>
        /// Unity draws terrain details within <c>detailObjectDistance</c> only, and that defaults to
        /// 80 m — so grass exists in the data and is simply never rendered when the camera is looking
        /// at the island from above. 250 m is the engine's maximum. These are view settings on the
        /// Terrain component, not generation: they change what you see, never what Vista produced.
        /// </remarks>
        private static void ApplyTerrainRenderDistances(UnityEngine.Terrain[] terrains)
        {
            if (terrains == null)
            {
                return;
            }

            for (var i = 0; i < terrains.Length; i++)
            {
                var terrain = terrains[i];
                if (terrain == null)
                {
                    continue;
                }

                terrain.detailObjectDistance = 250f;
                terrain.detailObjectDensity = 1f;
                terrain.treeDistance = 5000f;
                terrain.treeBillboardDistance = 200f;
                terrain.drawTreesAndFoliage = true;

                // Geometry LOD and the low-resolution basemap are both distance-driven, and both
                // show as banding across a large gentle surface: the mesh simplifies in strips, and
                // past basemapDistance the terrain stops using the real splat blend. The island is
                // 2.2 km across and mostly flat, so the defaults (pixel error 5, basemap 1000 m) put
                // every wide view firmly in that regime. Ground level looked clean while the aerial
                // view striped, which is the signature of a distance artifact rather than geometry.
                terrain.heightmapPixelError = 1f;
                terrain.basemapDistance = 20000f;
            }
        }

        private static void MatchVistaSeams(UnityEngine.Terrain[] terrains)
        {
            if (terrains == null || terrains.Length < 2)
            {
                return;
            }

            UnityEngine.Terrain.SetConnectivityDirty();
            for (var i = 0; i < terrains.Length; i++)
            {
                if (terrains[i] != null)
                {
                    terrains[i].allowAutoConnect = true;
                }
            }

            for (var i = 0; i < terrains.Length; i++)
            {
                var tile = terrains[i] != null
                    ? terrains[i].GetComponent<Pinwheel.Vista.UnityTerrain.TerrainTile>()
                    : null;
                if (tile != null)
                {
                    tile.MatchSeams();
                }
            }
        }

        /// <summary>
        /// Runtime-created VistaManager can leave UnityEvent fields null; Invoke then NREs.
        /// </summary>
        private static void EnsureVistaUnityEvents(VistaManager manager)
        {
            if (manager == null)
            {
                return;
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = typeof(VistaManager);
            EnsureUnityEventField(manager, type.GetField("m_beforeGeneratingUnityCallback", flags));
            EnsureUnityEventField(manager, type.GetField("m_afterGeneratingUnityCallback", flags));
        }

        private static void EnsureUnityEventField(object target, FieldInfo field)
        {
            if (field == null || field.GetValue(target) != null)
            {
                return;
            }

            field.SetValue(target, new UnityEvent());
        }

        /// <summary>
        /// Spawns the overworld biome stack: seabed + island, plus the massifs when
        /// <see cref="TerrainBakeFlags.IncludeMountains"/> is set.
        /// </summary>
        /// <remarks>
        /// Baking without the mountains is a real configuration, not a no-op: it is the reference
        /// bake for terrain diagnostics, because it isolates the island graph from the massif's
        /// Raise blend. The diagnostic <see cref="TerrainBakeFlags.VistaDemoAsIs"/> path replaces
        /// the whole stack with Vista's own sample graph instead.
        /// </remarks>
        private static void SpawnVistaBiomes(VistaManager manager, int seed, TerrainBakeFlags flags)
        {
            EnsureVistaFolder();

            var biomeSeed = new WorldSeed(seed).Fork("terrain.islandbase").Next();
            var demoAsIs = (flags & TerrainBakeFlags.VistaDemoAsIs) != 0;
            if (demoAsIs)
            {
                var graph = LoadOrCopyDemoMountainRangeGraph();
                Debug.LogWarning(
                    "[VistaDemo] Baking sample MountainRange as-is (dual HydraulicErosion kept).");
                var halfX = WorldScale.WorldExtentXMeters * 0.5f;
                var halfZ = WorldScale.WorldExtentZMeters * 0.5f;
                ConfigureIslandWorldBiome(
                    manager,
                    graph,
                    "DemoMountainRange",
                    biomeSeed,
                    new Vector3(halfX, 0f, halfZ),
                    halfX,
                    halfZ);
                Debug.Log(
                    $"[VistaDemo] World biome: graph=MountainRange (sample copy) extent=" +
                    $"{WorldScale.WorldExtentXMeters:F0}Ã—{WorldScale.WorldExtentZMeters:F0}m " +
                    $"sharedSeed={biomeSeed} â†’ {WorldScale.OverworldTileCountX}Ã—" +
                    $"{WorldScale.OverworldTileCountZ} tiles");
                return;
            }

            var includeMountains = (flags & TerrainBakeFlags.IncludeMountains) != 0;
            OverworldLpbBake.SpawnIslandAndMountainStack(manager, seed, includeMountains);
            if (!includeMountains)
            {
                Debug.Log(
                    "[Overworld] Baking the island without massifs (TerrainBakeFlags.IncludeMountains " +
                    "is off). Terrain geometry is the island graph alone.");
            }
        }

        public static void PersistSpawnedOwnedObjectsPublic(
            GameObject spawnedRoot,
            BiomeTemplateSpawnContext context) =>
            PersistSpawnedOwnedObjects(spawnedRoot, context);

        /// <summary>
        /// Persist in-memory graph clones from template spawn into <see cref="VistaGraphsFolder"/>.
        /// Never writes into package BiomeTemplates/.
        /// </summary>
        private static void PersistSpawnedOwnedObjects(
            GameObject spawnedRoot,
            BiomeTemplateSpawnContext context)
        {
            if (spawnedRoot == null || context == null)
            {
                return;
            }

            EnsureVistaFolder();
            var saved = new System.Collections.Generic.HashSet<Object>();
            foreach (var data in context.ownedObjects)
            {
                if (data == null || EditorUtility.IsPersistent(data) || !saved.Add(data))
                {
                    continue;
                }

                var extension = data is Material ? ".mat" : ".asset";
                var baseName = string.IsNullOrEmpty(data.name)
                    ? $"{spawnedRoot.name}_{data.GetType().Name}"
                    : data.name;
                baseName = baseName.Replace('/', '_').Replace('\\', '_');
                var path = $"{VistaGraphsFolder}/{baseName}{extension}";
                if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                {
                    AssetDatabase.DeleteAsset(path);
                }

                AssetDatabase.CreateAsset(data, path);
            }

            if (saved.Count > 0)
            {
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// After Generate: log acceptance checks (size 1400Ã—700, island land%, height budget, 4 anchors).
        /// </summary>
        [MenuItem("GameDev/Overworld/Vista/Validate Mountain Bake Acceptance")]
        public static void ValidateMountainBakeAcceptance()
        {
            EnsureSceneActive();
            var ok = true;
            var manager = Object.FindAnyObjectByType<VistaManager>();
            if (manager == null)
            {
                Debug.LogError("[MountainBiome] Validate: no VistaManager â€” run Generate first.");
                return;
            }

            if (Mathf.Abs(manager.terrainMaxHeight - WorldScale.TerrainHeightBudgetMeters) > 0.01f)
            {
                Debug.LogError(
                    $"[MountainBiome] Validate FAIL: terrainMaxHeight={manager.terrainMaxHeight} " +
                    $"expected {WorldScale.TerrainHeightBudgetMeters}");
                ok = false;
            }

            var terrains = OverworldTerrainAccess.FindTerrains();
            if (terrains == null || terrains.Length != WorldScale.OverworldTileCountX * WorldScale.OverworldTileCountZ)
            {
                Debug.LogError(
                    $"[MountainBiome] Validate FAIL: tile count={terrains?.Length ?? 0} " +
                    $"expected {WorldScale.OverworldTileCountX * WorldScale.OverworldTileCountZ}");
                ok = false;
            }

            var landPctSum = 0f;
            var landPctCount = 0;
            if (terrains != null)
            {
                for (var i = 0; i < terrains.Length; i++)
                {
                    var terrain = terrains[i];
                    if (terrain == null || terrain.terrainData == null)
                    {
                        continue;
                    }

                    var size = terrain.terrainData.size;
                    if (Mathf.Abs(size.x - WorldScale.OverworldTileMeters) > 0.5f ||
                        Mathf.Abs(size.z - WorldScale.OverworldTileMeters) > 0.5f ||
                        Mathf.Abs(size.y - WorldScale.TerrainHeightBudgetMeters) > 0.5f)
                    {
                        Debug.LogError(
                            $"[MountainBiome] Validate FAIL: tile[{i}] size=({size.x:F0},{size.y:F0},{size.z:F0}) " +
                            $"expected ({WorldScale.OverworldTileMeters:F0},{WorldScale.TerrainHeightBudgetMeters:F0}," +
                            $"{WorldScale.OverworldTileMeters:F0})");
                        ok = false;
                    }

                    TerrainHeightWrite.LogRange(terrain, $"validate[{i}]");
                    var data = terrain.terrainData;
                    var res = data.heightmapResolution;
                    var heights = data.GetHeights(0, 0, res, res);
                    var sea01 = Mathf.Clamp01(WorldScale.SeaLevelMeters / Mathf.Max(0.01f, size.y));
                    var above = 0;
                    for (var z = 0; z < res; z++)
                    {
                        for (var x = 0; x < res; x++)
                        {
                            if (heights[z, x] > sea01)
                            {
                                above++;
                            }
                        }
                    }

                    var landPct = 100f * above / Mathf.Max(1, res * res);
                    landPctSum += landPct;
                    landPctCount++;
                    if (landPct > 98f)
                    {
                        Debug.LogError(
                            $"[MountainBiome] Validate FAIL: tile[{i}] landAboveSea={landPct:F1}% " +
                            "(~full tile â€” inset-rect Outer falloff likely missing)");
                        ok = false;
                    }
                }
            }

            var biome = Object.FindAnyObjectByType<LocalProceduralBiome>();
            if (biome == null)
            {
                Debug.LogError("[MountainBiome] Validate FAIL: no LocalProceduralBiome in scene.");
                ok = false;
            }
            else if (biome.anchors == null || biome.anchors.Length < 3)
            {
                Debug.LogError(
                    $"[MountainBiome] Validate FAIL: anchors={biome.anchors?.Length ?? 0} " +
                    "(the biome has no usable polygon)");
                ok = false;
            }
            else if (biome.falloffDirection != FalloffDirection.Outer || biome.falloffDistance <= 0f)
            {
                // Falloff itself is authored on the template prefab; only assert it is an outward
                // ramp at all, so the island rim can reach the waterline.
                Debug.LogError(
                    $"[MountainBiome] Validate FAIL: falloff={biome.falloffDirection}/" +
                    $"{biome.falloffDistance} expected an Outer falloff with a positive distance");
                ok = false;
            }

            if (landPctCount > 0)
            {
                Debug.Log(
                    $"[MountainBiome] Validate landAvg={landPctSum / landPctCount:F1}% " +
                    $"over {landPctCount} tiles");
            }

            if (ok)
            {
                Debug.Log("[MountainBiome] Validate PASS â€” size/height/anchors/island land% OK.");
            }
        }

        /// <summary>
        /// Working copy of Vista sample MountainRange â€” never mutate the package asset.
        /// </summary>
        private static TerrainGraph LoadOrCopyDemoMountainRangeGraph()
        {
            EnsureVistaFolder();
            var existing = AssetDatabase.LoadAssetAtPath<TerrainGraph>(DemoMountainRangeWorkingPath);
            if (existing != null)
            {
                return existing;
            }

            if (AssetDatabase.LoadAssetAtPath<TerrainGraph>(DemoMountainRangeSourcePath) == null)
            {
                Debug.LogError(
                    $"[VistaDemo] Sample graph missing at {DemoMountainRangeSourcePath}");
                return null;
            }

            if (!AssetDatabase.CopyAsset(DemoMountainRangeSourcePath, DemoMountainRangeWorkingPath))
            {
                Debug.LogError(
                    $"[VistaDemo] Failed to copy MountainRange â†’ {DemoMountainRangeWorkingPath}");
                return null;
            }

            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<TerrainGraph>(DemoMountainRangeWorkingPath);
        }

        /// <summary>
        /// Full-world LPB: metre anchors, World space, shared graph/seed. Vista remaps to tiles.
        /// </summary>
        private static void ConfigureIslandWorldBiome(
            VistaManager manager,
            TerrainGraph sharedGraph,
            string name,
            int biomeSeed,
            Vector3 worldCenter,
            float halfExtentX,
            float halfExtentZ)
        {
            var biome = LocalProceduralBiome.CreateInstanceInScene(manager);
            biome.name = name;
            biome.seed = biomeSeed;
            biome.space = Space.World;
            biome.order = 0;
            biome.baseResolution = 1024;
            biome.terrainGraph = sharedGraph;

            // Identity scale: anchors carry real metres (Blank Â±500 Ã— scale=extent was the OOM bug).
            biome.transform.localPosition = worldCenter;
            biome.transform.localRotation = Quaternion.identity;
            biome.transform.localScale = Vector3.one;

            biome.anchors = new[]
            {
                new Vector3(-halfExtentX, 0f, -halfExtentZ),
                new Vector3(-halfExtentX, 0f, halfExtentZ),
                new Vector3(halfExtentX, 0f, halfExtentZ),
                new Vector3(halfExtentX, 0f, -halfExtentZ),
            };
            // No outer falloff â€” footprint is exactly the world; tiles cover the same area.
            biome.falloffDirection = FalloffDirection.Inner;
            biome.falloffDistance = 0f;

            var opts = biome.blendOptions;
            opts.heightMapBlendMode = BiomeBlendOptions.HeightBlendMode.Replace;
            opts.textureBlendMode = BiomeBlendOptions.TextureBlendMode.Replace;
            biome.blendOptions = opts;
        }

        private static IEnumerator WaitForVistaGeneration(VistaManager manager)
        {
            if (manager == null)
            {
                yield break;
            }

            EnsureVistaUnityEvents(manager);
            var task = manager.GenerateAll();
            var start = EditorApplication.timeSinceStartup;
            // Mountain biome graphs are heavy; do not Thread.Sleep â€” that blocks EditorCoroutines.
            const double timeoutSeconds = 300.0;
            while (task != null && !task.isCompleted)
            {
                if (EditorApplication.timeSinceStartup - start > timeoutSeconds)
                {
                    Debug.LogWarning(
                        "[Overworld] Vista generation timed out â€” cancelling. " +
                        "Partial height will not be used; re-run Generate after checking the Vista console.");
                    manager.CancelGeneration();
                    yield break;
                }

                yield return null;
            }

            if (task != null && task.isCancelled)
            {
                Debug.LogWarning("[Overworld] Vista generation was cancelled.");
            }
        }

        public static void EnsureOceanSurface()
        {
            WaterZoneMenu.EnsureOceanOcWPlane();
        }

        public static Material AssignOverworldTerrainMaterial(UnityEngine.Terrain terrain)
        {
            if (terrain == null)
            {
                return null;
            }

            var mat = EnsureOverworldTerrainMaterial();
            terrain.materialTemplate = mat;
            return mat;
        }

        public static Material EnsureOverworldTerrainMaterial()
        {
            // URP TerrainLit is the terrain material for the overworld. It reads the TerrainLayer
            // assets Vista binds through the graphs' Texture Outputs — albedo, normal, UV tiling,
            // smoothness and diffuse remap all come from those assets and are tuned in the
            // Inspector, never from generation code.
            var existing = AssetDatabase.LoadAssetAtPath<Material>(TerrainMaterialPath);
            if (existing != null && existing.shader != null && existing.shader.name == TerrainShaderName)
            {
                return existing;
            }

            var shader = Shader.Find(TerrainShaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[Overworld] Shader '{TerrainShaderName}' not found.");
                return existing;
            }

            EnsureSettingsFolder();
            var mat = existing != null ? existing : new Material(shader);
            mat.shader = shader;
            mat.name = "OverworldTerrain";
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mat, TerrainMaterialPath);
            }
            else
            {
                EditorUtility.SetDirty(mat);
            }

            AssetDatabase.SaveAssets();
            return mat;
        }

        private static void EnsureSceneActive()
        {
            EnsureSceneFolder();
            if (File.Exists(ScenePath))
            {
                var active = SceneManager.GetActiveScene();
                if (active.path != ScenePath)
                {
                    EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                }
            }
            else
            {
                CreateFreshSceneAndGenerate(DefaultSeed);
            }
        }

        private static void EnsureSceneFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
        }

        private static void EnsureSettingsFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }
        }

        private static void EnsureVistaFolder()
        {
            EnsureSettingsFolder();
            if (!AssetDatabase.IsValidFolder(VistaGraphsFolder))
            {
                AssetDatabase.CreateFolder("Assets/Settings", "Vista");
            }
        }

        private static void AddToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            for (var i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    return;
                }
            }

            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
#endif

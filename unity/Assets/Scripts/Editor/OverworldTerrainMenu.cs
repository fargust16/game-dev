#if UNITY_EDITOR
using System.Collections;
using System.IO;
using GameDev.World;
using GameDev.World.Terrain;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Overworld terrain scene and bake entry points: 2×2 Unity Terrain tiles filled from TerrainGen,
    /// plus the scene's light, sky, volume and ocean.
    /// </summary>
    /// <remarks>
    /// The bake itself is <see cref="OverworldTerrainBake"/>. This type owns the menus, the scene the
    /// bake runs in, and the order of the surface step inside Generate All: clear everything derived
    /// from terrain, bake, restore the ocean.
    /// </remarks>
    public static class OverworldTerrainMenu
    {
        public const string ScenePath = "Assets/Scenes/Overworld_Generator.unity";
        private const string TerrainMaterialPath = "Assets/Settings/OverworldTerrain.mat";
        private const string SkyboxDaytimePath = "Assets/Skybox/Materials/Skybox_Daytime.mat";

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

        /// <summary>Removes the terrain grid only; see <see cref="OverworldTerrainBake.ClearTerrain"/>.</summary>
        public static void ClearTerrainOnly()
        {
            OverworldTerrainBake.ClearTerrain();
        }

        private static void CreateFreshSceneAndGenerate(int seed)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateSunLight();
            ApplySkyboxAndEnvironment();
            EditorCoroutineUtility.StartCoroutineOwnerless(GenerateSurfaceRoutine(seed, TerrainBakeFlags.Full));
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
        /// Terrain bake. Cascade-clears markers, roads, settlements, player and water first.
        /// Fire-and-forget for menu callers; the pipeline yields <see cref="GenerateSurfaceRoutine"/>.
        /// </summary>
        public static void GenerateSurface(int seed, TerrainBakeFlags flags)
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(GenerateSurfaceRoutine(seed, flags));
        }

        /// <summary>Coroutine entry for the surface step; completes when the terrain is baked.</summary>
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

            OverworldTerrainBake.Bake(seed, flags);
            yield return null;

            EnsureOceanSurface();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            OverworldGenTiming.Log("terrain.total", OverworldGenTiming.StopMs(total), $"seed={seed} flags={flags}");
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
            // assets the bake binds to every tile — albedo, normal, UV tiling, smoothness and diffuse
            // remap all come from those assets and are tuned in the Inspector, never from generation
            // code.
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

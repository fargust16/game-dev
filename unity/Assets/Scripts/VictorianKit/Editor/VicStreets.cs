using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameDev.VictorianKit.EditorTools
{
    /// <summary>
    /// Builds the showcase scene of Plans/VICTORIAN_STREETS_PLAN.md: the rich street, then the poor street 60 m
    /// behind it, their ground, the sun, a light probe group and a baked box-projected reflection probe per street,
    /// Static flags, the lightmap bake, and renders by day and night to Logs/VictorianKit.
    /// Batch: -executeMethod GameDev.VictorianKit.EditorTools.VicStreets.Run
    /// </summary>
    public static class VicStreets
    {
        /// <summary>The poor street's offset behind the rich one, Unity Z.</summary>
        public const float PoorZ = 60f;

        private const string LightingPath = VicKitPaths.ArtFolder + "/VIC_StreetsLighting.lighting";
        private const string VolumePath = VicKitPaths.ArtFolder + "/VIC_StreetsVolume.asset";

        [MenuItem("GameDev/Victorian Kit/Build Streets")]
        public static void Run() => Build(bake: true);

        /// <summary>The same scene without the lightmap bake, for quick looks.</summary>
        public static void RunNoBake() => Build(bake: false);

        private static void Build(bool bake)
        {
            VicKitPaths.EnsureKitPrefabs();
            VicTextures.EnsureAll();
            VicMaterials.BuildAll(); // cheap, and keeps the assets in step with the specs
            Directory.CreateDirectory(VicKitPaths.LogFolder);
            Directory.CreateDirectory(VicKitPaths.SceneFolder);

            SeamShots();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var lighting = Lighting();
            VicRooms.Report.Clear();
            var rich = Street("Street_Rich", VicHousePlans.RichStreet(), 0f);
            var poor = Street("Street_Poor", VicHousePlans.PoorStreet(), PoorZ);
            File.WriteAllLines(Path.Combine(VicKitPaths.LogFolder, "rooms.txt"), VicRooms.Report.Concat(VicRooms.SizeReport()));
            Ground();
            var probes = new List<ReflectionProbe>
            {
                Probes(rich.transform, 62f, 14f),
                Probes(poor.transform, 86f, 6f),
            };
            lighting.Probes = probes.ToArray();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), VicKitPaths.StreetScene);

            if (bake)
            {
                if (!Lightmapping.Bake())
                {
                    Debug.LogError("[VicKit] the lightmap bake failed.");
                }

                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            }

            Shots(lighting);
            Debug.Log($"[VicKit] streets built: {VicKitPaths.StreetScene}, bake {bake}.");
        }

        // ------------------------------------------------------------ houses

        private static GameObject Street(string name, List<VicHouseSpec> houses, float z)
        {
            var root = new GameObject(name);
            root.transform.position = new Vector3(0f, 0f, z);
            foreach (var spec in houses)
            {
                var palette = spec.Palette;
                var go = VicHouseFactory.Build(spec.Name, spec.House.Pieces, palette.For);
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector3(spec.X, 0f, 0f);
                MarkStatic(go);
                VicRooms.Build(spec, go.transform); // the rooms behind the glass (§4.4)
            }

            return root;
        }

        /// <summary>
        /// Static for batching, occlusion, reflection probes and GI. Walls, corners, bands and roof decks take the
        /// lightmap; small pieces (trims, inserts, rails, pipes, pots, caps) take light probes.
        /// </summary>
        private static void MarkStatic(GameObject house)
        {
            const StaticEditorFlags flags = StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic |
                                            StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic |
                                            StaticEditorFlags.ReflectionProbeStatic;
            foreach (Transform piece in house.transform)
            {
                var id = piece.name.Substring(0, piece.name.LastIndexOf('_'));
                var lightmapped = id.StartsWith("SM_Vic_Wall_") || id.StartsWith("SM_Vic_Frieze_") || id.StartsWith("SM_Vic_Foundation_") ||
                                  id.StartsWith("SM_Vic_Cornice_") || id.StartsWith("SM_Vic_String_") ||
                                  (id.StartsWith("SM_Vic_Roof_") && (id.EndsWith("_Deck") || id.Contains("GableWall")));
                foreach (var t in piece.GetComponentsInChildren<Transform>(true))
                {
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
                }

                foreach (var r in piece.GetComponentsInChildren<MeshRenderer>(true))
                {
                    r.receiveGI = lightmapped ? ReceiveGI.Lightmaps : ReceiveGI.LightProbes;
                }
            }
        }

        // ------------------------------------------------------------ ground

        /// <summary>Flagstones everywhere, a road of setts before each street; Unity's built-in plane, world-space UVs.</summary>
        private static void Ground()
        {
            var root = new GameObject("Ground");
            Plane(root, "Pave", VicMaterials.Load("Ground_Pave"), new Vector3(43f, 0f, 30f), new Vector2(170f, 140f), 0f);
            Plane(root, "Road_Rich", VicMaterials.Load("Ground_Setts"), new Vector3(43f, 0f, -7f), new Vector2(150f, 8f), 0.01f);
            Plane(root, "Road_Poor", VicMaterials.Load("Ground_Setts"), new Vector3(43f, 0f, PoorZ - 7f), new Vector2(150f, 8f), 0.01f);
        }

        private static void Plane(GameObject root, string name, Material material, Vector3 centre, Vector2 size, float lift)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(root.transform, false);
            go.transform.position = centre + (Vector3.up * lift);
            go.transform.localScale = new Vector3(size.x / 10f, 1f, size.y / 10f);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.receiveGI = ReceiveGI.Lightmaps;
            r.scaleInLightmap = 0.25f;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic);
        }

        // ------------------------------------------------------------ light

        private static VicStreetLighting Lighting()
        {
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.lightmapBakeType = LightmapBakeType.Mixed;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.92f;
            sunGo.transform.rotation = Quaternion.Euler(34f, 32f, 0f);
            RenderSettings.sun = sun;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 60f;
            RenderSettings.fogEndDistance = 420f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;

            var lightingGo = new GameObject("Lighting");
            var lighting = lightingGo.AddComponent<VicStreetLighting>();
            lighting.Sun = sun;
            lighting.DaySkybox = Sky("VIC_SkyDay", 1.1f, new Color(0.5f, 0.5f, 0.5f), new Color(0.37f, 0.35f, 0.34f));
            lighting.NightSkybox = Sky("VIC_SkyNight", 0.06f, new Color(0.25f, 0.3f, 0.5f), new Color(0.05f, 0.05f, 0.06f));
            lighting.Night = 0f;
            lighting.Apply();

            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingPath);
            if (settings == null)
            {
                settings = new LightingSettings { name = "VIC_StreetsLighting" };
                AssetDatabase.CreateAsset(settings, LightingPath);
            }

            settings.bakedGI = true;
            settings.realtimeGI = false;
            settings.mixedBakeMode = MixedLightingMode.IndirectOnly;
            settings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            settings.lightmapResolution = 8f;
            settings.lightmapPadding = 2;
            settings.lightmapMaxSize = 2048;
            settings.directionalityMode = LightmapsMode.NonDirectional;
            // 128 indirect samples left green and pink noise patches on pale stucco; a Gaussian filter, no denoiser,
            // so the bake needs nothing the machine may lack.
            settings.directSampleCount = 32;
            settings.indirectSampleCount = 512;
            settings.environmentSampleCount = 256;
            settings.filteringMode = LightingSettings.FilterMode.Advanced;
            settings.denoiserTypeIndirect = LightingSettings.DenoiserType.None;
            settings.filterTypeIndirect = LightingSettings.FilterType.Gaussian;
            settings.filteringGaussianRadiusIndirect = 3;
            settings.maxBounces = 2;
            settings.lightmapCompression = LightmapCompression.NormalQuality;
            settings.ao = false;
            EditorUtility.SetDirty(settings);
            Lightmapping.lightingSettings = settings;

            var volumeGo = new GameObject("PostVolume");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = Profile();
            return lighting;
        }

        private static VolumeProfile Profile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumePath);
            }

            Component<Tonemapping>(profile).mode.Override(TonemappingMode.Neutral);
            var bloom = Component<Bloom>(profile);
            bloom.intensity.Override(0.25f);
            bloom.threshold.Override(1.2f);
            var color = Component<ColorAdjustments>(profile);
            color.postExposure.Override(0f);
            color.contrast.Override(6f);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        /// <summary>A procedural sky material, rewritten each build: the day sky, and a dark one the night swaps in.</summary>
        private static Material Sky(string name, float exposure, Color tint, Color ground)
        {
            var path = $"{VicKitPaths.ArtFolder}/{name}.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural")) { name = name };
                AssetDatabase.CreateAsset(sky, path);
            }

            sky.SetFloat("_Exposure", exposure);
            sky.SetColor("_SkyTint", tint);
            sky.SetColor("_GroundColor", ground);
            sky.SetFloat("_AtmosphereThickness", 1f);
            sky.SetFloat("_SunSize", 0.04f);
            EditorUtility.SetDirty(sky);
            return sky;
        }

        /// <summary>The profile's override of <typeparamref name="T"/>, added to the asset if it has none.</summary>
        private static T Component<T>(VolumeProfile profile)
            where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var c))
            {
                return c;
            }

            c = profile.Add<T>(true);
            c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(c, profile);
            return c;
        }

        /// <summary>A light probe group round the street's outside and a baked box-projected reflection probe over it.</summary>
        private static ReflectionProbe Probes(Transform street, float length, float depth)
        {
            var probeGo = new GameObject("ReflectionProbe");
            probeGo.transform.SetParent(street, false);
            probeGo.transform.localPosition = new Vector3(length * 0.5f, 8f, -2f);
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            probe.boxProjection = true;
            probe.size = new Vector3(length + 30f, 40f, depth + 40f);
            probe.center = new Vector3(0f, 6f, (depth * 0.5f) - 4f);
            probe.resolution = 256;
            probe.importance = 1;

            var groupGo = new GameObject("LightProbes");
            groupGo.transform.SetParent(street, false);
            var group = groupGo.AddComponent<LightProbeGroup>();
            var positions = new List<Vector3>();
            for (var x = -4f; x <= length + 4f; x += 4f)
            {
                foreach (var y in new[] { 0.5f, 3f, 6f, 9f, 12.5f, 16f })
                {
                    foreach (var z in new[] { -1.4f, -6f, -12f, depth + 2f })
                    {
                        positions.Add(new Vector3(x, y, z));
                    }
                }
            }

            group.probePositions = positions.ToArray();
            return probe;
        }

        // ------------------------------------------------------------ renders

        private static void Shots(VicStreetLighting lighting)
        {
            lighting.Night = 0f;
            lighting.Apply();
            VicShots.Eye("street_rich_overview", new Vector3(-16f, 10f, -32f), new Vector3(31f, 6f, 4f), 50f);
            VicShots.Eye("street_rich_along", new Vector3(-6f, 1.7f, -9f), new Vector3(40f, 6f, 2f), 60f);
            VicShots.Eye("street_rich_R1", new Vector3(10f, 4f, -22f), new Vector3(10f, 7f, 4f), 50f);
            VicShots.Eye("street_rich_R2", new Vector3(33f, 4f, -21f), new Vector3(33f, 7f, 3f), 50f);
            VicShots.Eye("street_rich_R3", new Vector3(54f, 4f, -21f), new Vector3(54f, 7f, 4f), 50f);
            VicShots.Eye("street_rich_roofs", new Vector3(31f, 36f, -26f), new Vector3(31f, 9f, 6f), 50f);
            VicShots.Eye("street_rich_back", new Vector3(70f, 14f, 36f), new Vector3(31f, 6f, 6f), 50f);
            VicShots.Eye("street_rich_porch", new Vector3(10f, 2.2f, -4f), new Vector3(10f, 4f, 4f), 60f);

            var z = PoorZ;
            VicShots.Eye("street_poor_overview", new Vector3(-16f, 9f, z - 30f), new Vector3(43f, 5f, z + 3f), 50f);
            VicShots.Eye("street_poor_along", new Vector3(-6f, 1.7f, z - 9f), new Vector3(50f, 5f, z + 2f), 60f);
            VicShots.Eye("street_poor_front", new Vector3(43f, 6f, z - 36f), new Vector3(43f, 6f, z + 2f), 55f);
            VicShots.Eye("street_poor_roofs", new Vector3(43f, 32f, z - 28f), new Vector3(43f, 6f, z + 3f), 50f);
            VicShots.Eye("street_poor_close", new Vector3(26f, 2f, z - 7f), new Vector3(36f, 4f, z + 1f), 60f);

            // The rooms behind the glass (§4.4): close looks through the windows and plan sections of each storey.
            VicShots.Eye("rooms_rich_R1_ground", new Vector3(3f, 1.9f, -4.5f), new Vector3(3f, 1.7f, 4f), 55f);
            VicShots.Eye("rooms_rich_R2_first", new Vector3(33f, 4.8f, -5.5f), new Vector3(33f, 4.4f, 4f), 50f);
            VicShots.Eye("rooms_rich_R3_ground", new Vector3(54f, 1.9f, -4.5f), new Vector3(54f, 1.7f, 4f), 55f);
            VicShots.Eye("rooms_poor_P2", new Vector3(15f, 2.2f, z - 4.5f), new Vector3(15f, 2.0f, z + 3f), 55f);
            VicShots.Eye("rooms_poor_P4", new Vector3(28f, 1.9f, z - 4f), new Vector3(28f, 1.6f, z + 3f), 55f);
            VicShots.Eye("rooms_poor_P8", new Vector3(80f, 4.6f, z - 5f), new Vector3(80f, 4.2f, z + 3f), 55f);
            for (var storey = 0; storey < 3; storey++)
            {
                var cut = (float)VicKit.FloorZ(storey) + 2.2f;
                VicShots.Section($"plan_rich_{storey}", new Vector3(31f, 0f, 6f), cut, 34f);
                VicShots.Section($"plan_poor_{storey}", new Vector3(45f, 0f, z + 3f), cut, 48f);
            }

            lighting.Night = 1f;
            lighting.Apply();
            VicShots.Eye("street_rich_night", new Vector3(-6f, 1.7f, -9f), new Vector3(40f, 6f, 2f), 60f);
            VicShots.Eye("street_poor_night", new Vector3(-6f, 1.7f, z - 9f), new Vector3(50f, 5f, z + 2f), 60f);
            VicShots.Eye("rooms_rich_night", new Vector3(33f, 4.8f, -5.5f), new Vector3(33f, 4.4f, 4f), 50f);
            lighting.Night = 0f;
            lighting.Apply();
        }

        /// <summary>
        /// The 2 m seam check on House U before the streets (brief, step 1): the house with the kit's materials is
        /// measured by <see cref="VicSeamCheck"/>, then painted common and shot close at a 1 m corner leg, a 2 m seam
        /// and a court corner.
        /// </summary>
        public static void SeamShots()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var house = VicHouseFactory.Build("House_U_Seam", VicHousePlans.HouseU().Pieces);
            var result = VicSeamCheck.Run(house);
            File.WriteAllText(Path.Combine(VicKitPaths.LogFolder, "seam_u.txt"), result.ToString());
            Debug.Log($"[VicKit] seam check on House U: {result.Faces} faces, {result.Broken} broken.");
            Object.DestroyImmediate(house);

            var palette = VicPalette.Common();
            VicHouseFactory.Build("House_U_Common", VicHousePlans.HouseU().Pieces, palette.For);
            var lighting = Lighting();
            lighting.Apply();
            VicShots.Eye("seam_u_corner", new Vector3(-1.4f, 2.3f, -2.4f), new Vector3(0.6f, 2.1f, 0f), 50f);
            VicShots.Eye("seam_u_2m", new Vector3(3f, 2.0f, -1.8f), new Vector3(3f, 2.0f, 0f), 60f);
            VicShots.Eye("seam_u_court", new Vector3(9f, 2.2f, 1.2f), new Vector3(6.3f, 2.2f, 4f), 60f);
            VicShots.Eye("seam_u_front", new Vector3(8f, 3f, -16f), new Vector3(8f, 6f, 4f), 55f);
        }
    }
}

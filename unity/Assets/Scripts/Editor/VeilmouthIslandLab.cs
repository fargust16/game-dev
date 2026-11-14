using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using JBooth.MicroSplat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// The island's lab (VS-ISLAND; Documentation/Terrain/ISLAND_CONCEPTS.md): the approved concept's ground, drawn by
/// MicroSplat under one sheet of sea, in a scene of its own on a terrain of its own. Veilmouth.unity and its terrain
/// are not opened. The ground's heights and the weights of its surfaces are the planner's
/// (tools/slice/island/build.py b --export, into Assets/Data/Slice/Island/); this only lays them on a terrain.
/// <para>
/// Batch, with graphics (the texture arrays are packed on the GPU):
/// -executeMethod VeilmouthIslandLab.Build, then -executeMethod VeilmouthIslandLab.Render.
/// </para>
/// Every tile size, light and view here is an authoring default.
/// </summary>
public static class VeilmouthIslandLab
{
    private const string DataFolder = "Assets/Data/Slice/Island";
    private const string ScenePath = "Assets/Scenes/Slice/Lab/IslandLab.unity";
    private const string TerrainPath = DataFolder + "/IslandLab_Terrain.asset";
    private const string TextureRoot = "Assets/Art/CC0/Terrain";
    private const string LayerFolder = "Assets/Settings/TerrainLayers/Island";
    private const string SplatFolder = DataFolder + "/MicroSplatData";
    private const string WaterMaterial = "Assets/Settings/Water/OverworldWater.mat";
    private const string FallbackTerrainMaterial = "Assets/Settings/OverworldTerrain.mat";
    private const string RenderLoop = "_MSRENDERLOOP_UNITYURP6P3";
    private const float Size = 1000f, Budget = 100f, SeaLevel = 24f, Ocean = 8000f;

    [System.Serializable]
    private sealed class Island
    {
        public string concept;
        public int heightmap, alphamap;
        public string[] layers;
    }

    /// <summary>
    /// A surface's tile, m. Core has nothing against a texture's repeat (the Anti-Tiling module is not installed), and
    /// at one and a half times the photographed patch (3 to 4.5 m) the repeats read as a net over open ground from
    /// 40 m off (seen in the lab, 2026-11-14). So each tile is about three times its patch, and the arrays are 2048 px,
    /// which keeps 230 to 340 px to the metre under the walker's feet.
    /// </summary>
    private static readonly Dictionary<string, float> Tile = new Dictionary<string, float>
    {
        { "grass_ground", 8f }, { "forest_ground_04", 9f }, { "forest_leaves_03", 7f }, { "brown_mud_03", 6f },
        { "dirt_floor", 7f }, { "gravel_ground_01", 6f }, { "river_small_rocks", 6f }, { "rock_face_03", 11f },
        { "mossy_rock", 9f }, { "coast_sand_03", 8f }, { "rocks_ground_06", 8f },
    };

    /// <summary>
    /// A surface's tint over its photograph, toward the slice's palette (WORLD_STYLE_GUIDE §2): the turf was shot dry
    /// and is brought to a damp olive green, the bare earth and the mud are taken down from orange, the rest is left.
    /// </summary>
    private static readonly Dictionary<string, Color> Tint = new Dictionary<string, Color>
    {
        { "grass_ground", new Color(0.74f, 1f, 0.56f) }, { "forest_ground_04", new Color(0.86f, 0.88f, 0.8f) },
        { "forest_leaves_03", new Color(0.82f, 0.8f, 0.74f) }, { "brown_mud_03", new Color(0.8f, 0.8f, 0.78f) },
        { "dirt_floor", new Color(0.6f, 0.59f, 0.56f) }, { "coast_sand_03", new Color(0.96f, 0.94f, 0.9f) },
    };

    private static string Map(string set, string map) => $"{TextureRoot}/{set}/{set}_{map}_2k.{(map == "disp" ? "png" : "jpg")}";

    public static void Build()
    {
        var island = JsonUtility.FromJson<Island>(File.ReadAllText(DataFolder + "/island.json"));
        ImportTextures(island.layers);
        var layers = Layers(island.layers);
        var data = TerrainOf(island, layers);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var ground = Terrain.CreateTerrainGameObject(data);
        ground.name = "Ground";
        ground.transform.position = new Vector3(-Size * 0.5f, 0f, -Size * 0.5f);
        var terrain = ground.GetComponent<Terrain>();
        terrain.heightmapPixelError = 5f;
        terrain.materialTemplate = AssetDatabase.LoadAssetAtPath<Material>(FallbackTerrainMaterial);

        Sheet("Water", SeaLevel, AssetDatabase.LoadAssetAtPath<Material>(WaterMaterial));
        Sheet("Sea Floor", 7.5f, FloorMaterial());

        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.93f, 0.82f);
        sun.intensity = 2f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(40f, 50f, 0f); // from the south-west, 40° up

        // The slice's painted day sky (Free HDR Sky), also as what the water and the wet ground mirror; the lab bakes
        // nothing, so the sky's own cube map is the reflection.
        var sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/Skybox/Materials/Skybox_Daytime.mat");
        if (sky != null)
        {
            RenderSettings.skybox = sky;
        }

        // The sky alone, baked once as the scene's reflection, as VeilmouthReflections does for the city: what the sea
        // and the wet ground mirror.
        var probe = new GameObject("Sky Probe").AddComponent<ReflectionProbe>();
        probe.transform.position = new Vector3(0f, 120f, 0f);
        probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Custom;
        probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
        probe.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.Skybox;
        probe.cullingMask = 0;
        probe.resolution = 256;
        probe.hdr = true;
        var skyPath = DataFolder + "/IslandLab_Sky.exr";
        if (Lightmapping.BakeReflectionProbe(probe, skyPath))
        {
            AssetDatabase.ImportAsset(skyPath, ImportAssetOptions.ForceSynchronousImport);
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = AssetDatabase.LoadAssetAtPath<Texture>(skyPath);
        }
        else
        {
            Debug.LogError("[IslandLab] the sky's reflection did not bake");
        }

        Object.DestroyImmediate(probe.gameObject);

        // The slice's grade (WORLD_STYLE_GUIDE §5): HDR, the Neutral tone mapper, saturation down by about a seventh.
        AssetDatabase.DeleteAsset(DataFolder + "/IslandLab_Grade.asset");
        var grade = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
        AssetDatabase.CreateAsset(grade, DataFolder + "/IslandLab_Grade.asset");
        var tone = grade.Add<UnityEngine.Rendering.Universal.Tonemapping>(true);
        tone.mode.Override(UnityEngine.Rendering.Universal.TonemappingMode.Neutral);
        var colour = grade.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
        colour.saturation.Override(-14f);
        colour.contrast.Override(8f);
        AssetDatabase.AddObjectToAsset(tone, grade);
        AssetDatabase.AddObjectToAsset(colour, grade);
        EditorUtility.SetDirty(grade);
        AssetDatabase.SaveAssets();
        var volume = new GameObject("Grade").AddComponent<UnityEngine.Rendering.Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = grade;

        // The slice's day, as VeilmouthSky sets it: its three ambient colours and its haze.
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.5f, 0.51f, 0.53f);
        RenderSettings.ambientEquatorColor = new Color(0.47f, 0.45f, 0.41f);
        RenderSettings.ambientGroundColor = new Color(0.27f, 0.24f, 0.21f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        // The city's haze is a warm grey, which turns a far sea brown (seen in the lab); over open water it is taken
        // toward the sky's own horizon. VeilmouthSky's haze needs the same when the island is the slice's ground.
        RenderSettings.fogColor = new Color(0.66f, 0.70f, 0.74f);
        RenderSettings.fogDensity = 0.0012f;

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[IslandLab] concept {island.concept}: {island.layers.Length} surfaces on a terrain of {island.heightmap}, in {ScenePath}");

        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-islandTerrainLit") < 0)
        {
            ToMicroSplat(terrain, island.layers, layers);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
    }

    /// <summary>Normal maps as normal maps, the grey maps linear, every map at its 2K: set once, so a second run imports nothing.</summary>
    private static void ImportTextures(string[] sets)
    {
        foreach (var set in sets)
        {
            foreach (var map in new[] { "diff", "nor_gl", "rough", "ao", "disp" })
            {
                var path = Map(set, map);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                {
                    throw new FileNotFoundException("run tools/assets/fetch_terrain_textures.py first: " + path);
                }

                var type = map == "nor_gl" ? TextureImporterType.NormalMap : TextureImporterType.Default;
                var srgb = map == "diff";
                if (importer.textureType == type && importer.sRGBTexture == srgb && importer.maxTextureSize == 2048 && importer.mipmapEnabled)
                {
                    continue;
                }

                importer.textureType = type;
                importer.sRGBTexture = srgb;
                importer.maxTextureSize = 2048;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }
        }
    }

    /// <summary>The island's own terrain layers, one for each surface, made once; the Overworld's are not touched.</summary>
    private static TerrainLayer[] Layers(string[] sets)
    {
        Directory.CreateDirectory(LayerFolder);
        AssetDatabase.Refresh();
        var layers = new TerrainLayer[sets.Length];
        for (var i = 0; i < sets.Length; i++)
        {
            var path = $"{LayerFolder}/Island_{sets[i]}.terrainlayer";
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer == null)
            {
                layer = new TerrainLayer
                {
                    diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Map(sets[i], "diff")),
                    normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Map(sets[i], "nor_gl")),
                    tileSize = Vector2.one * Tile[sets[i]],
                };
                AssetDatabase.CreateAsset(layer, path);
            }

            layers[i] = layer;
        }

        AssetDatabase.SaveAssets();
        return layers;
    }

    /// <summary>
    /// The lab's own terrain data, written again from the planner's files. The unity terrain traps: the resolutions
    /// come before the size, and the data is an asset before its layers and weights.
    /// </summary>
    private static TerrainData TerrainOf(Island island, TerrainLayer[] layers)
    {
        AssetDatabase.DeleteAsset(TerrainPath);
        var data = new TerrainData { heightmapResolution = island.heightmap, alphamapResolution = island.alphamap, baseMapResolution = 1024 };
        data.size = new Vector3(Size, Budget, Size);
        AssetDatabase.CreateAsset(data, TerrainPath);
        data.terrainLayers = layers;

        var n = island.heightmap;
        var raw = Unzip(DataFolder + "/island_heights.bytes");
        var heights = new float[n, n];
        for (var z = 0; z < n; z++)
        {
            for (var x = 0; x < n; x++)
            {
                var at = 2 * ((z * n) + x);
                heights[z, x] = (raw[at] | (raw[at + 1] << 8)) / 65535f;
            }
        }

        data.SetHeights(0, 0, heights);

        var a = island.alphamap;
        var weights = Unzip(DataFolder + "/island_surfaces.bytes");
        var maps = new float[a, a, layers.Length];
        for (var l = 0; l < layers.Length; l++)
        {
            for (var z = 0; z < a; z++)
            {
                for (var x = 0; x < a; x++)
                {
                    maps[z, x, l] = weights[(l * a * a) + (z * a) + x] / 255f;
                }
            }
        }

        data.SetAlphamaps(0, 0, maps);
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        return data;
    }

    private static byte[] Unzip(string path)
    {
        using (var file = File.OpenRead(path))
        using (var gzip = new GZipStream(file, CompressionMode.Decompress))
        using (var memory = new MemoryStream())
        {
            gzip.CopyTo(memory);
            return memory.ToArray();
        }
    }

    /// <summary>A flat sheet far past the terrain: the sea to the horizon, and a bed under it where the terrain ends.</summary>
    private static void Sheet(string name, float y, Material material)
    {
        var sheet = GameObject.CreatePrimitive(PrimitiveType.Plane);
        sheet.name = name;
        Object.DestroyImmediate(sheet.GetComponent<Collider>());
        sheet.transform.position = new Vector3(0f, y, 0f);
        sheet.transform.localScale = new Vector3(Ocean / 10f, 1f, Ocean / 10f);
        if (material != null)
        {
            sheet.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }

    private static Material FloorMaterial()
    {
        var path = DataFolder + "/IslandSeaFloor.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_BaseColor", new Color(0.05f, 0.11f, 0.13f)); // deep water's own colour, where no bed is seen
        material.SetFloat("_Smoothness", 0.1f);
        EditorUtility.SetDirty(material);
        return material;
    }

    /// <summary>
    /// The terrain's material from MicroSplat Core through the URP 6.3 adapter: one texture set for each surface with
    /// its own height, roughness and occlusion maps, height-blended, each surface at its own tile. The adapter is named
    /// by its keyword, since Core overwrites it in its own list (MICROSPLAT_STAMPIT_RESEARCH.md §3.3).
    /// </summary>
    private static void ToMicroSplat(Terrain terrain, string[] sets, TerrainLayer[] layers)
    {
        AssetDatabase.DeleteAsset(SplatFolder);
        Directory.CreateDirectory(SplatFolder);
        AssetDatabase.Refresh();

        var keywords = new[]
        {
            "_MICROSPLAT", "_MICROTERRAIN", "_BRANCHSAMPLES", "_BRANCHSAMPLESAGR", "_USEGRADMIP", RenderLoop,
            "_MAX12TEXTURES", "_PERTEXUVSCALEOFFSET", "_PERTEXTINT",
        };
        var material = MicroSplatShaderGUI.NewShaderAndMaterial(SplatFolder, "VeilmouthIsland", keywords);
        material.enableInstancing = true;

        var config = ScriptableObject.CreateInstance<TextureArrayConfig>();
        for (var i = 0; i < sets.Length; i++)
        {
            config.sourceTextures.Add(new TextureArrayConfig.TextureEntry
            {
                terrainLayer = layers[i],
                diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(Map(sets[i], "diff")),
                normal = AssetDatabase.LoadAssetAtPath<Texture2D>(Map(sets[i], "nor_gl")),
                height = AssetDatabase.LoadAssetAtPath<Texture2D>(Map(sets[i], "disp")),
                smoothness = AssetDatabase.LoadAssetAtPath<Texture2D>(Map(sets[i], "rough")),
                isRoughness = true,
                ao = AssetDatabase.LoadAssetAtPath<Texture2D>(Map(sets[i], "ao")),
            });
        }

        var size = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-islandArrays1k") >= 0
            ? TextureArrayConfig.TextureSize.k1024
            : TextureArrayConfig.TextureSize.k2048;
        config.defaultTextureSettings.diffuseSettings.textureSize = size;
        config.defaultTextureSettings.normalSettings.textureSize = size;
        config.defaultTextureSettings.diffuseSettings.Aniso = 4;
        config.defaultTextureSettings.normalSettings.Aniso = 4;
        AssetDatabase.CreateAsset(config, SplatFolder + "/MicroSplatConfig.asset");
        AssetDatabase.SaveAssets();
        TextureArrayConfigEditor.CompileConfig(config);
        material.SetTexture("_Diffuse", config.diffuseArray);
        material.SetTexture("_NormalSAO", config.normalSAOArray);

        var prop = MicroSplatShaderGUI.FindOrCreatePropTex(material);
        material.SetVector("_UVScale", new Vector4(1f, 1f, 0f, 0f));
        for (var i = 0; i < prop.maxTextures; i++)
        {
            var repeats = i < sets.Length ? Mathf.Round(Size / Tile[sets[i]]) : 1f;
            prop.SetValue(i, MicroSplatPropData.PerTexVector2.SplatUVScale, new Vector2(repeats, repeats));
            prop.SetValue(i, MicroSplatPropData.PerTexVector2.SplatUVOffset, Vector2.zero);
        }

        for (var i = 0; i < prop.maxTextures; i++)
        {
            prop.SetValue(i, MicroSplatPropData.PerTexColor.Tint, i < sets.Length && Tint.TryGetValue(sets[i], out var tint) ? tint : Color.white);
        }

        prop.SetValue(0, 15, Color.white); // each row's "set" mark, or the material's GUI writes its defaults over it
        prop.SetValue(1, 15, Color.white);
        EditorUtility.SetDirty(prop);

        var words = MicroSplatUtilities.FindOrCreateKeywords(material);
        var splat = terrain.GetComponent<MicroSplatTerrain>();
        if (splat == null)
        {
            splat = terrain.gameObject.AddComponent<MicroSplatTerrain>();
        }

        splat.templateMaterial = material;
        splat.propData = prop;
        splat.keywordSO = words;
        EditorUtility.SetDirty(splat);

        var compiler = new MicroSplatShaderGUI.MicroSplatCompiler();
        compiler.Compile(material);
        var loop = compiler.renderLoop == null ? "none" : compiler.renderLoop.GetRenderLoopKeyword();
        MicroSplatTerrain.SyncAll();
        AssetDatabase.SaveAssets();

        var report = new StringBuilder();
        report.AppendLine($"render loop: {loop} (wanted {RenderLoop})");
        report.AppendLine($"shader: {material.shader.name}, supported {material.shader.isSupported}, passes {material.passCount}");
        report.AppendLine($"arrays: diffuse {Describe(config.diffuseArray)}, normalSAO {Describe(config.normalSAOArray)}");
        report.AppendLine($"terrain layers after sync: {string.Join(", ", terrain.terrainData.terrainLayers.Select(l => l == null ? "null" : l.name))}");
        report.AppendLine($"keywords: {string.Join(" ", words.keywords)}");
        foreach (var message in ShaderUtil.GetShaderMessages(material.shader))
        {
            report.AppendLine($"shader message [{message.severity}] {message.message} ({message.file}:{message.line})");
        }

        Directory.CreateDirectory("Logs/IslandLab");
        File.WriteAllText("Logs/IslandLab/microsplat.txt", report.ToString());
        Debug.Log("[IslandLab] MicroSplat\n" + report);
        if (loop != RenderLoop)
        {
            throw new System.InvalidOperationException("MicroSplat compiled for " + loop);
        }
    }

    private static string Describe(Texture2DArray array) =>
        array == null ? "MISSING" : $"{array.width} px x {array.depth} ({array.format})";

    // ----------------------------------------------------------------------------------------------- views

    /// <summary>A view by plan metres (px east, py south) and metres over the sea; an eye of 0 or less stands on the ground.</summary>
    private static readonly (string Name, Vector3 Eye, Vector3 Look, float Fov)[] Views =
    {
        ("01_island_from_the_south_east", new Vector3(1020f, 1160f, 330f), new Vector3(500f, 480f, 0f), 50f),
        ("02_valley_from_the_estuary", new Vector3(770f, 870f, 70f), new Vector3(480f, 470f, 5f), 55f),
        ("03_creek_head", new Vector3(340f, 310f, 26f), new Vector3(256f, 180f, 0f), 55f),
        ("04_east_cliffs", new Vector3(990f, 400f, 36f), new Vector3(890f, 440f, 18f), 55f),
        ("05_kettle_bay_eye", new Vector3(214f, 372f, 0f), new Vector3(140f, 600f, 6f), 60f),
        ("06_meadow_eye", new Vector3(430f, 300f, 0f), new Vector3(300f, 232f, 4f), 60f),
        ("07_east_ridge_wood_eye", new Vector3(752f, 372f, 0f), new Vector3(830f, 320f, 30f), 60f),
        ("08_spit_eye", new Vector3(820f, 800f, 0f), new Vector3(700f, 700f, 4f), 60f),
        ("09_ground_close", new Vector3(404f, 262f, 0f), new Vector3(398f, 256f, -1f), 60f),
        ("10_shore_close", new Vector3(196f, 330f, 0f), new Vector3(190f, 322f, -1f), 60f),
    };

    /// <summary>Batch: the lab built again and its views taken, in one run.</summary>
    public static void BuildAndRender()
    {
        Build();
        Render();
    }

    public static void Render()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var terrain = Object.FindFirstObjectByType<Terrain>();
        Directory.CreateDirectory("Logs/IslandLab");
        var rig = new GameObject("Lab Camera") { hideFlags = HideFlags.HideAndDontSave };
        var camera = rig.AddComponent<Camera>();
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 4000f;
        camera.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.71f, 0.69f, 0.64f);
        camera.allowHDR = true;
        UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(camera).renderPostProcessing = true;
        var target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.DefaultHDR) { antiAliasing = 4 };
        var shot = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        foreach (var (name, eye, look, fov) in Views)
        {
            var from = World(terrain, eye, 1.7f);
            camera.transform.position = from;
            camera.transform.LookAt(World(terrain, look, 0f));
            camera.fieldOfView = fov;
            camera.targetTexture = target;
            camera.Render();
            camera.Render(); // the second frame has the first's depth and opaque textures for the water
            RenderTexture.active = target;
            shot.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            shot.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes($"Logs/IslandLab/{name}.png", shot.EncodeToPNG());
        }

        camera.targetTexture = null;
        Object.DestroyImmediate(rig);
        target.Release();
        Debug.Log($"[IslandLab] {Views.Length} views in Logs/IslandLab");
    }

    private static Vector3 World(Terrain terrain, Vector3 plan, float standing)
    {
        var x = plan.x - (Size * 0.5f);
        var z = (Size * 0.5f) - plan.y;
        var ground = terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;
        var y = plan.z > 0f ? SeaLevel + plan.z : plan.z < 0f ? ground - plan.z - 1f : ground + standing;
        return new Vector3(x, Mathf.Max(y, plan.z > 0f ? y : ground + 0.3f), z);
    }
}

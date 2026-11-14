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
    internal const string SliceSeaMaterial = "Assets/Settings/Water/VeilmouthSea.mat";
    private const string FallbackTerrainMaterial = "Assets/Settings/OverworldTerrain.mat";
    internal const string RenderLoop = "_MSRENDERLOOP_UNITYURP6P3";
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
        { "mossy_rock", 9f }, { "sand_03", 5f }, { "rocks_ground_06", 8f }, { "leafy_grass", 6f },
    };

    /// <summary>
    /// A surface's tint over its photograph, toward the slice's palette (WORLD_STYLE_GUIDE §2): the turf was shot dry
    /// and is brought to a damp olive green, the bare earth and the mud are taken down from orange, the rest is left.
    /// </summary>
    private static readonly Dictionary<string, Color> Tint = new Dictionary<string, Color>
    {
        { "grass_ground", new Color(0.74f, 1f, 0.56f) }, { "forest_ground_04", new Color(0.86f, 0.88f, 0.8f) },
        { "forest_leaves_03", new Color(0.82f, 0.8f, 0.74f) }, { "brown_mud_03", new Color(1.2f, 1.12f, 1f) },
        { "dirt_floor", new Color(0.6f, 0.59f, 0.56f) }, { "sand_03", new Color(1.7f, 1.62f, 1.42f) }, { "leafy_grass", new Color(0.8f, 0.95f, 0.66f) },
    };

    /// <summary>
    /// The city's surfaces on the island's terrain (ISLAND_CITY_REBUILD_PLAN.md §4): the slice's own setts and cobbles,
    /// after the land's sets. They are not in <see cref="FineGrained"/>, so the material never reads them
    /// stochastically (owner, 2026-11-14): a paving's pattern must stay whole.
    /// </summary>
    private const string CityPrefix = "city:";

    private static readonly string[] CitySets = { CityPrefix + "Setts", CityPrefix + "Cobbles" };

    private static bool City(string set) => set.StartsWith(CityPrefix);

    /// <summary>
    /// Batch: the island's ground, the city built on it by the slice's own builder, and their views.
    /// -executeMethod VeilmouthIslandLab.BuildCityAndRender
    /// </summary>
    public static void BuildCityAndRender()
    {
        Build();
        GameDev.Slice.EditorTools.VeilmouthCity.BuildIsland();
        Render();
    }

    private static string Map(string set, string map) => $"{TextureRoot}/{set}/{set}_{map}_2k.{(map == "disp" ? "png" : "jpg")}";

    public static void Build()
    {
        var island = JsonUtility.FromJson<Island>(File.ReadAllText(DataFolder + "/island.json"));
        ImportTextures(island.layers);
        // The city's own surfaces after the land's, with no weight until the city is built and paints them.
        var sets = island.layers.Concat(CitySets).ToArray();
        var layers = Layers(sets);
        var data = TerrainOf(island, layers);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var ground = Terrain.CreateTerrainGameObject(data);
        ground.name = "Ground";
        ground.transform.position = new Vector3(-Size * 0.5f, 0f, -Size * 0.5f);
        var terrain = ground.GetComponent<Terrain>();
        terrain.heightmapPixelError = 2f;   // 5 drew the water's edge in steps from the air (IC3's views)
        terrain.materialTemplate = AssetDatabase.LoadAssetAtPath<Material>(FallbackTerrainMaterial);

        Sheet("Water", SeaLevel, SeaMaterial());
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
        RenderSettings.ambientSkyColor = new Color(0.5f, 0.51f, 0.53f);
        RenderSettings.ambientEquatorColor = new Color(0.47f, 0.45f, 0.41f);
        RenderSettings.ambientGroundColor = new Color(0.27f, 0.24f, 0.21f);
        SetAmbient();
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
            ToMicroSplat(terrain, sets, layers);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
    }

    /// <summary>Normal maps as normal maps, the grey maps linear, every map at its 2K: set once, so a second run imports nothing.</summary>
    internal static void ImportTextures(string[] sets)
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
    internal static TerrainLayer[] Layers(string[] sets)
    {
        Directory.CreateDirectory(LayerFolder);
        AssetDatabase.Refresh();
        var layers = new TerrainLayer[sets.Length];
        for (var i = 0; i < sets.Length; i++)
        {
            if (sets[i].StartsWith(CityPrefix))
            {
                // The slice's own street layers, as C5 made and tuned them; their tile is theirs.
                var street = AssetDatabase.LoadAssetAtPath<TerrainLayer>($"Assets/Settings/TerrainLayers/Slice/Slice_{sets[i].Substring(CityPrefix.Length)}.terrainlayer");
                if (street == null)
                {
                    throw new FileNotFoundException("the slice's street layer is missing: " + sets[i]);
                }

                Tile[sets[i]] = street.tileSize.x;
                layers[i] = street;
                continue;
            }

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
        for (var l = 0; l < island.layers.Length; l++)
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

    /// <summary>
    /// The slice's own sea (owner, 2026-11-14; TERRAIN_POLISH_REPORT.md §4): the project's water material with the
    /// foam off, since WaterWorks draws its foam band's edge by a per-pixel threshold that steps along every shore.
    /// WaterWorks and the Overworld's material are not changed. Made once and kept in git.
    /// </summary>
    internal static Material SeaMaterial()
    {
        var sea = AssetDatabase.LoadAssetAtPath<Material>(SliceSeaMaterial);
        if (sea == null)
        {
            AssetDatabase.CopyAsset(WaterMaterial, SliceSeaMaterial);
            sea = AssetDatabase.LoadAssetAtPath<Material>(SliceSeaMaterial);
        }

        sea.SetFloat("_UseFoam", 0f);
        EditorUtility.SetDirty(sea);
        AssetDatabase.SaveAssets();
        return sea;
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
        var config = Arrays(SplatFolder, sets, layers);
        var material = Splat(SplatFolder, "VeilmouthIsland", Variation, sets, config, Size);
        var loop = Attach(terrain, material);
        var words = MicroSplatUtilities.FindOrCreateKeywords(material);

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

    /// <summary>
    /// What the island's material takes against a texture's repeat, from the project's own module
    /// (<see cref="VeilmouthTerrainVariation"/>; TERRAIN_POLISH_REPORT.md §2): the macro variation and the distance
    /// resampling. The stochastic read was measured and is not taken.
    /// </summary>
    internal static readonly string[] Variation = { "_HYBRIDHEIGHTBLEND", VeilmouthTerrainVariation.Macro, VeilmouthTerrainVariation.Stochastic };

    /// <summary>
    /// The sets with no form larger than a hand and no patches of tone: turf, woodland floor, litter, mud. These alone
    /// take the stochastic read; bare earth and sand were tried and showed the honeycomb from 60 m up (2026-11-14).
    /// </summary>
    private static readonly HashSet<string> FineGrained = new HashSet<string>
    {
        "grass_ground", "forest_ground_04", "forest_leaves_03", "brown_mud_03", "sand_03", "leafy_grass",
    };

    /// <summary>The two packed arrays of the surfaces' texture sets, 2048 px unless -islandArrays1k is given.</summary>
    internal static TextureArrayConfig Arrays(string folder, string[] sets, TerrainLayer[] layers)
    {
        var config = ScriptableObject.CreateInstance<TextureArrayConfig>();
        for (var i = 0; i < sets.Length; i++)
        {
            config.sourceTextures.Add(new TextureArrayConfig.TextureEntry
            {
                terrainLayer = layers[i],
                // A city surface brings its layer's two maps; a land surface its whole set.
                diffuse = City(sets[i]) ? layers[i].diffuseTexture : AssetDatabase.LoadAssetAtPath<Texture2D>(Map(sets[i], "diff")),
                normal = City(sets[i]) ? layers[i].normalMapTexture : AssetDatabase.LoadAssetAtPath<Texture2D>(Map(sets[i], "nor_gl")),
                height = City(sets[i]) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(Map(sets[i], "disp")),
                smoothness = City(sets[i]) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(Map(sets[i], "rough")),
                isRoughness = !City(sets[i]),
                ao = City(sets[i]) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(Map(sets[i], "ao")),
            });
        }

        var size = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-islandArrays1k") >= 0
            ? TextureArrayConfig.TextureSize.k1024
            : TextureArrayConfig.TextureSize.k2048;
        config.defaultTextureSettings.diffuseSettings.textureSize = size;
        config.defaultTextureSettings.normalSettings.textureSize = size;
        config.defaultTextureSettings.diffuseSettings.Aniso = 4;
        config.defaultTextureSettings.normalSettings.Aniso = 4;
        AssetDatabase.CreateAsset(config, folder + "/MicroSplatConfig.asset");
        AssetDatabase.SaveAssets();
        TextureArrayConfigEditor.CompileConfig(config);
        return config;
    }

    /// <summary>
    /// A MicroSplat shader and material in a folder of its own, over arrays already packed: Core's keywords and the
    /// given ones, each surface at its tile on a terrain of the given size (m), and its tint.
    /// </summary>
    internal static Material Splat(string folder, string name, string[] extra, string[] sets, TextureArrayConfig config, float terrainSize)
    {
        var keywords = new[]
        {
            "_MICROSPLAT", "_MICROTERRAIN", "_BRANCHSAMPLES", "_BRANCHSAMPLESAGR", "_USEGRADMIP", RenderLoop,
            sets.Length > 12 ? "_MAX16TEXTURES" : "_MAX12TEXTURES", "_PERTEXUVSCALEOFFSET", "_PERTEXTINT",
        }.Concat(extra).ToArray();
        var material = MicroSplatShaderGUI.NewShaderAndMaterial(folder, name, keywords);
        material.enableInstancing = true;
        if (material.HasProperty("_HybridHeightBlendDistance"))
        {
            // Core's hybrid blend: by the height maps under the walker, plainly by the weights from 140 m off.
            material.SetFloat("_HybridHeightBlendDistance", 140f);
        }

        if (material.HasProperty("_VeilLayers"))
        {
            // The stochastic read takes the fine-grained sets alone: on a set with forms of its own size (stones,
            // rock faces) the patches' honeycomb shows (seen in the tiling lab, 2026-11-14).
            // A trial's switches: -veilAlso set,set adds sets to the stochastic read, -veilSharpness n sets how sharply
            // its patches meet (the default 7; lower is softer and reads more patches a pixel).
            var args = System.Environment.GetCommandLineArgs();
            var also = System.Array.IndexOf(args, "-veilAlso");
            var extraSets = also >= 0 && also + 1 < args.Length ? args[also + 1].Split(',') : new string[0];
            var mask = 0;
            for (var i = 0; i < sets.Length; i++)
            {
                mask |= FineGrained.Contains(sets[i]) || extraSets.Contains(sets[i]) ? 1 << i : 0;
            }

            material.SetFloat("_VeilLayers", mask);
            var sharp = System.Array.IndexOf(args, "-veilSharpness");
            if (sharp >= 0 && sharp + 1 < args.Length && material.HasProperty("_VeilTiling"))
            {
                var tiling = material.GetVector("_VeilTiling");
                tiling.y = float.Parse(args[sharp + 1], System.Globalization.CultureInfo.InvariantCulture);
                material.SetVector("_VeilTiling", tiling);
            }
        }

        material.SetTexture("_Diffuse", config.diffuseArray);
        material.SetTexture("_NormalSAO", config.normalSAOArray);

        var prop =MicroSplatShaderGUI.FindOrCreatePropTex(material);
        material.SetVector("_UVScale", new Vector4(1f, 1f, 0f, 0f));
        for (var i = 0; i < prop.maxTextures; i++)
        {
            var repeats = i < sets.Length ? Mathf.Round(terrainSize / Tile[sets[i]]) : 1f;
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
        return material;
    }

    /// <summary>The material on a terrain: MicroSplat's component, the shader compiled for it, and the adapter it was compiled for.</summary>
    internal static string Attach(Terrain terrain, Material material)
    {
        var splat = terrain.GetComponent<MicroSplatTerrain>();
        if (splat == null)
        {
            splat = terrain.gameObject.AddComponent<MicroSplatTerrain>();
        }

        splat.templateMaterial = material;
        splat.propData = MicroSplatShaderGUI.FindOrCreatePropTex(material);
        splat.keywordSO = MicroSplatUtilities.FindOrCreateKeywords(material);
        EditorUtility.SetDirty(splat);
        var compiler = new MicroSplatShaderGUI.MicroSplatCompiler();
        compiler.Compile(material);
        MicroSplatTerrain.SyncAll();
        AssetDatabase.SaveAssets();
        return compiler.renderLoop == null ? "none" : compiler.renderLoop.GetRenderLoopKeyword();
    }

    internal static string Describe(Texture2DArray array) =>
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
        // The city, once it stands (BuildCityAndRender).
        ("11_town_from_the_south", new Vector3(760f, 900f, 260f), new Vector3(520f, 520f, 0f), 50f),
        ("12_penny_from_the_air", new Vector3(650f, 560f, 110f), new Vector3(520f, 440f, 5f), 55f),
        ("13_long_tongue_eye", new Vector3(337.5f, 439.6f, 0f), new Vector3(395.6f, 555.7f, 3f), 60f),
        ("14_tar_row_eye", new Vector3(693.8f, 724f, 0f), new Vector3(623.1f, 653.3f, 3f), 60f),
        ("15_sleepy_from_the_river", new Vector3(470f, 470f, 28f), new Vector3(370f, 500f, 6f), 55f),
        ("16_creaking_gate_from_the_road", new Vector3(346f, 353f, 0f), new Vector3(371f, 379f, 4f), 60f),
        ("17_lower_veil_quay", new Vector3(622f, 722f, 5f), new Vector3(656f, 694f, 2f), 60f),
        ("18_last_farthing_bridge", new Vector3(482f, 542f, 5f), new Vector3(465f, 508f, 2f), 60f),
        ("19_reek_and_south_gate", new Vector3(470f, 880f, 90f), new Vector3(545f, 760f, 0f), 55f),
        ("20_walls_from_the_north_west", new Vector3(240f, 240f, 220f), new Vector3(480f, 520f, 0f), 50f),
        ("21_the_start_eye", new Vector3(676f, 628f, 0f), new Vector3(648f, 660f, 3f), 60f),
        // Close views from a walker's height (ISLAND_WORLD_AUDIT.md §5.5): two corners of a block in each district, where
        // two rows join; the embankment's wall, the close's wall from the town, a bridge's end.
        ("22_penny_pa_corner", new Vector3(522.0f, 550.9f, 0.0f), new Vector3(529.6f, 528.1f, 5.0f), 60f),
        ("32_penny_pa_far_corner", new Vector3(554.2f, 465.5f, 0.0f), new Vector3(546.6f, 488.3f, 5.0f), 60f),
        ("23_sleepy_sb_corner", new Vector3(403.2f, 443.6f, 0.0f), new Vector3(395.6f, 466.4f, 5.0f), 60f),
        ("33_sleepy_sb_far_corner", new Vector3(377.7f, 531.3f, 0.0f), new Vector3(385.3f, 508.5f, 5.0f), 60f),
        ("24_lower_la_corner", new Vector3(698.1f, 732.5f, 0.0f), new Vector3(698.1f, 708.4f, 5.0f), 60f),
        ("34_lower_la_far_corner", new Vector3(694.6f, 641.2f, 0.0f), new Vector3(694.6f, 665.3f, 5.0f), 60f),
        ("25_parsons_nw1_corner", new Vector3(795.0f, 520.0f, 0.0f), new Vector3(812.0f, 537.0f, 5.0f), 60f),
        ("35_parsons_nw1_far_corner", new Vector3(857.0f, 572.0f, 0.0f), new Vector3(840.0f, 555.0f, 5.0f), 60f),
        ("26_reek_rd_corner", new Vector3(554.4f, 729.3f, 0.0f), new Vector3(554.4f, 753.4f, 5.0f), 60f),
        ("36_reek_rd_far_corner", new Vector3(557.9f, 813.5f, 0.0f), new Vector3(557.9f, 789.4f, 5.0f), 60f),
        ("27_embankments_from_the_bridge", new Vector3(465f, 508f, 7.2f), new Vector3(430f, 440f, 4.5f), 60f),
        ("28_the_close_from_the_town", new Vector3(660f, 530f, 0f), new Vector3(730f, 500f, 6f), 60f),
        ("29_last_farthing_bridge_end", new Vector3(503f, 517f, 0f), new Vector3(486f, 499f, 1.5f), 60f),
        ("30_town_section_from_the_north", new Vector3(470f, 300f, 60f), new Vector3(520f, 560f, 0f), 50f),
    };

    /// <summary>
    /// The slice's day as an ambient probe of the scene's own: the three colours' mean all round, and the sky's lead
    /// over the ground from above. A probe given outright stands in a batch run, where a gradient's does not.
    /// </summary>
    private static void SetAmbient()
    {
        var ambient = new UnityEngine.Rendering.SphericalHarmonicsL2();
        ambient.AddAmbientLight(new Color(0.41f, 0.40f, 0.38f));
        ambient.AddDirectionalLight(Vector3.up, new Color(0.23f, 0.27f, 0.32f), 0.5f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Custom;
        RenderSettings.ambientProbe = ambient;
    }

    /// <summary>Batch: the lab built again and its views taken, in one run.</summary>
    public static void BuildAndRender()
    {
        Build();
        Render();
    }

    public static void Render()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        // A scene opened again has no ambient probe until its colours are set again: without this the shade stood near
        // black in the city's views (IC3). The colours are the slice's day, as Build sets them.
        // Setting the three colours again did not bring it back in a batch run (seen in IC5's views), so the probe is
        // given outright.
        SetAmbient();
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
            File.WriteAllBytes($"Logs/IslandLab/{name}.jpg", shot.EncodeToJPG(90));
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

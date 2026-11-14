using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using JBooth.MicroSplat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Debug = UnityEngine.Debug;

/// <summary>
/// The comparison patch for a texture's repeat (Documentation/Terrain/TERRAIN_POLISH_REPORT.md §2): one terrain of
/// 256 m in four strips, turf, woodland floor, mud and stony ground, drawn in turn by each way of breaking the
/// repeat, seen from the air, from a rooftop's height and from the walker's eye, and timed. The island's own texture
/// sets and tiles; a scene and files of its own, written again by each run and kept out of git.
/// <para>Batch, with graphics: -executeMethod VeilmouthTilingLab.Run</para>
/// Every number here is an authoring default.
/// </summary>
public static class VeilmouthTilingLab
{
    private const string IslandData = "Assets/Data/Slice/Island";
    private const string Folder = IslandData + "/TilingLab";
    private const string ScenePath = "Assets/Scenes/Slice/Lab/TilingLab.unity";
    private const string Out = "Logs/TilingLab";
    private const float Size = 256f, Height = 40f, Strip = 64f;
    private const int Res = 257, Alpha = 512;
    private const int Width = 2560, Tall = 1440, Rounds = 3, WarmUps = 3, Frames = 12;

    /// <summary>Each way's name, the keywords it adds to Core's, and whether each strip is a mix of sets of its kind.</summary>
    private static readonly (string Name, string[] Keywords, bool Mixed)[] Variants =
    {
        ("A_base", new string[0], false),
        ("B_macro", new[] { VeilmouthTerrainVariation.Macro }, false),
        ("C_layers", new[] { Hybrid }, true),
        ("D_stochastic", new[] { VeilmouthTerrainVariation.Stochastic }, false),
        ("E_resample", new[] { VeilmouthTerrainVariation.Resample }, false),
        ("F_macro_layers_resample", new[] { Hybrid, VeilmouthTerrainVariation.Macro, VeilmouthTerrainVariation.Resample }, true),
        ("G_macro_layers_stochastic", new[] { Hybrid, VeilmouthTerrainVariation.Macro, VeilmouthTerrainVariation.Stochastic }, true),
        ("H_macro_layers_stochastic_fine_sets", new[] { Hybrid, VeilmouthTerrainVariation.Macro, VeilmouthTerrainVariation.Stochastic }, true),
    };

    /// <summary>The ways whose stochastic read takes every set; the others keep the island's own choice, the fine-grained sets alone.</summary>
    private static readonly HashSet<string> EverySet = new HashSet<string> { "D_stochastic", "G_macro_layers_stochastic" };

    /// <summary>Core's own: the layers blend by their height maps near the eye and plainly by their weights far off, so a mix of sets is crisp underfoot and soft from the air.</summary>
    private const string Hybrid = "_HYBRIDHEIGHTBLEND";

    /// <summary>The eye and what it looks at, in the terrain's own metres; an eye height of 0 stands on the ground.</summary>
    private static readonly (string Name, Vector3 Eye, Vector3 Look, float Fov)[] Views =
    {
        ("air", new Vector3(128f, 170f, -70f), new Vector3(128f, 0f, 118f), 50f),
        ("mid", new Vector3(128f, 24f, 6f), new Vector3(128f, 0f, 120f), 60f),
        ("eye_turf_wood", new Vector3(64f, 0f, 24f), new Vector3(64f, 0f, 140f), 60f),
        ("eye_mud_stone", new Vector3(192f, 0f, 24f), new Vector3(192f, 0f, 140f), 60f),
    };

    [System.Serializable]
    private sealed class Island
    {
        public string[] layers;
    }

    public static void Run()
    {
        var sets = JsonUtility.FromJson<Island>(File.ReadAllText(IslandData + "/island.json")).layers;
        VeilmouthIslandLab.ImportTextures(sets);
        var layers = VeilmouthIslandLab.Layers(sets);

        AssetDatabase.DeleteAsset(Folder);
        Directory.CreateDirectory(Folder);
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        Directory.CreateDirectory(Out);
        AssetDatabase.Refresh();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var data = new TerrainData { heightmapResolution = Res, alphamapResolution = Alpha, baseMapResolution = 512 };
        data.size = new Vector3(Size, Height, Size);
        AssetDatabase.CreateAsset(data, Folder + "/TilingLab_Terrain.asset");
        data.terrainLayers = layers;
        var heights = new float[Res, Res];
        for (var z = 0; z < Res; z++)
        {
            for (var x = 0; x < Res; x++)
            {
                heights[z, x] = (4f + (5f * Fbm(x, z, 70f, 11)) + (0.5f * Fbm(x, z, 9f, 13))) / Height;
            }
        }

        data.SetHeights(0, 0, heights);
        var single = Weights(sets, false);
        var mixed = Weights(sets, true);
        data.SetAlphamaps(0, 0, single);
        var ground = Terrain.CreateTerrainGameObject(data);
        ground.name = "Ground";
        var terrain = ground.GetComponent<Terrain>();
        terrain.heightmapPixelError = 5f;

        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.93f, 0.82f);
        sun.intensity = 2f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(40f, 50f, 0f);
        var sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/Skybox/Materials/Skybox_Daytime.mat");
        if (sky != null)
        {
            RenderSettings.skybox = sky;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.5f, 0.51f, 0.53f);
        RenderSettings.ambientEquatorColor = new Color(0.47f, 0.45f, 0.41f);
        RenderSettings.ambientGroundColor = new Color(0.27f, 0.24f, 0.21f);
        RenderSettings.fog = false;
        var grade = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(IslandData + "/IslandLab_Grade.asset");
        if (grade != null)
        {
            var volume = new GameObject("Grade").AddComponent<UnityEngine.Rendering.Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = grade;
        }

        var config = VeilmouthIslandLab.Arrays(Folder, sets, layers);
        var report = new StringBuilder();
        report.AppendLine($"Tiling lab, {System.DateTime.Now:yyyy-MM-dd HH:mm}, Unity {Application.unityVersion}, {SystemInfo.graphicsDeviceType}, {SystemInfo.graphicsDeviceName}");
        report.AppendLine($"arrays: diffuse {VeilmouthIslandLab.Describe(config.diffuseArray)} {Megabytes(config.diffuseArray):0.0} MB, normalSAO {VeilmouthIslandLab.Describe(config.normalSAOArray)} {Megabytes(config.normalSAOArray):0.0} MB");

        // Every way's material first, so that the timing can go round them in turn.
        var materials = new List<Material>();
        foreach (var (name, keywords, _) in Variants)
        {
            var folder = $"{Folder}/{name}";
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            var material = VeilmouthIslandLab.Splat(folder, "VeilTiling_" + name, keywords, sets, config, Size);
            if (EverySet.Contains(name))
            {
                material.SetFloat("_VeilLayers", 65535f);
            }
            var loop = VeilmouthIslandLab.Attach(terrain, material);
            var messages = ShaderUtil.GetShaderMessages(material.shader);
            var errors = messages.Count(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error);
            report.AppendLine($"{name}: keywords [{string.Join(" ", keywords)}], adapter {loop}, supported {material.shader.isSupported}, passes {material.passCount}, shader errors {errors}, warnings {messages.Length - errors}");
            foreach (var message in messages.Where(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).Take(4))
            {
                report.AppendLine($"   error: {message.message} (line {message.line})");
            }

            materials.Add(material);
        }

        EditorSceneManager.SaveScene(scene, ScenePath);

        var rig = new GameObject("Lab Camera") { hideFlags = HideFlags.HideAndDontSave };
        var camera = rig.AddComponent<Camera>();
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 2000f;
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.allowHDR = true;
        var extra = camera.GetUniversalAdditionalCameraData();
        var splat = terrain.GetComponent<MicroSplatTerrain>();

        // The views.
        var target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.DefaultHDR) { antiAliasing = 4 };
        var shot = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        var small = new RenderTexture(960, 540, 24, RenderTextureFormat.DefaultHDR) { antiAliasing = 4 };
        var sheets = new Dictionary<string, Texture2D>();
        extra.renderPostProcessing = true;
        for (var v = 0; v < Variants.Length; v++)
        {
            Use(terrain, splat, materials[v], Variants[v].Mixed ? mixed : single);
            foreach (var (name, eye, look, fov) in Views)
            {
                Aim(camera, terrain, eye, look, fov);
                camera.targetTexture = target;
                camera.Render();
                camera.Render();
                RenderTexture.active = target;
                shot.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                shot.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes($"{Out}/{name}_{Variants[v].Name}.png", shot.EncodeToPNG());

                // The same view at half size into the view's sheet: A to D along the top, E to H below.
                if (!sheets.TryGetValue(name, out var sheet))
                {
                    sheets[name] = sheet = new Texture2D(3840, 1080, TextureFormat.RGB24, false);
                    sheet.SetPixels32(new Color32[3840 * 1080]);
                }

                camera.targetTexture = small;
                camera.Render();
                RenderTexture.active = small;
                sheet.ReadPixels(new Rect(0, 0, 960, 540), (v % 4) * 960, v < 4 ? 540 : 0);
                RenderTexture.active = null;
            }
        }

        foreach (var pair in sheets)
        {
            pair.Value.Apply();
            File.WriteAllBytes($"{Out}/sheet_{pair.Key}.jpg", pair.Value.EncodeToJPG(90));
        }

        // The timing: every way in turn, round after round, so that a drift of the machine falls on all alike.
        var samples = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp ? urp.msaaSampleCount : 1;
        var big = new RenderTexture(Width, Tall, 24) { antiAliasing = Mathf.Max(1, samples) };
        var pixel = new Texture2D(1, 1, TextureFormat.RGB24, false);
        extra.renderPostProcessing = false;
        camera.targetTexture = big;
        var times = new Dictionary<(int, int), List<double>>();
        for (var round = 0; round < Rounds; round++)
        {
            for (var v = 0; v < Variants.Length; v++)
            {
                Use(terrain, splat, materials[v], Variants[v].Mixed ? mixed : single);
                for (var w = 0; w < Views.Length; w++)
                {
                    Aim(camera, terrain, Views[w].Eye, Views[w].Look, Views[w].Fov);
                    if (!times.TryGetValue((v, w), out var list))
                    {
                        times[(v, w)] = list = new List<double>();
                    }

                    Time(camera, big, pixel, list);
                }
            }
        }

        report.AppendLine();
        report.AppendLine($"One camera's render at {Width} x {Tall}, MSAA {samples}, no post-processing; median ms of {Rounds} rounds of {Frames} frames after {WarmUps} warm-ups, and the difference from A:");
        report.AppendLine("way".PadRight(30) + string.Join(string.Empty, Views.Select(w => w.Name.PadLeft(22))));
        for (var v = 0; v < Variants.Length; v++)
        {
            var line = new StringBuilder(Variants[v].Name.PadRight(30));
            for (var w = 0; w < Views.Length; w++)
            {
                var ms = Median(times[(v, w)]);
                var baseline = Median(times[(0, w)]);
                line.Append($"{ms:0.00} ({ms - baseline:+0.00;-0.00})".PadLeft(22));
            }

            report.AppendLine(line.ToString());
        }

        // The editor's frame counters read nothing from a camera rendered by hand in batch mode (tried: UnityStats and
        // the profiler's recorders both give 0), so draw calls are not reported here. No way changes the terrain's
        // geometry or its passes, so they are the same for every way by construction.
        report.AppendLine();
        report.AppendLine("Draw calls: not readable from a hand-rendered camera in batch mode; the same for every way, since none changes the terrain's geometry or its 8 passes.");

        camera.targetTexture = null;
        Object.DestroyImmediate(rig);
        target.Release();
        big.Release();
        File.WriteAllText($"{Out}/report.txt", report.ToString());
        Debug.Log("[TilingLab]\n" + report);
    }

    private static double Megabytes(Object asset) => UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(asset) / (1024.0 * 1024.0);

    private static void Use(Terrain terrain, MicroSplatTerrain splat, Material material, float[,,] weights)
    {
        terrain.terrainData.SetAlphamaps(0, 0, weights);
        // MicroSplat's Sync copies a template's properties onto the instance the terrain already wears and leaves
        // that instance's shader, so another way's shader needs a new instance: the terrain is bared first.
        terrain.materialTemplate = null;
        splat.templateMaterial = material;
        splat.propData = MicroSplatShaderGUI.FindOrCreatePropTex(material);
        splat.keywordSO = MicroSplatUtilities.FindOrCreateKeywords(material);
        splat.Sync();
    }

    private static void Aim(Camera camera, Terrain terrain, Vector3 eye, Vector3 look, float fov)
    {
        float Ground(Vector3 p) => terrain.SampleHeight(p) + terrain.transform.position.y;
        var from = new Vector3(eye.x, eye.y > 0f ? eye.y : Ground(eye) + 1.7f, eye.z);
        var to = new Vector3(look.x, Ground(look) + look.y, look.z);
        camera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(to - from));
        camera.fieldOfView = fov;
    }

    private static void Time(Camera camera, RenderTexture target, Texture2D pixel, List<double> into)
    {
        for (var i = 0; i < WarmUps + Frames; i++)
        {
            var watch = Stopwatch.StartNew();
            camera.Render();
            if (target.antiAliasing > 1)
            {
                target.ResolveAntiAliasedSurface();
            }

            RenderTexture.active = target;
            pixel.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
            RenderTexture.active = null;
            watch.Stop();
            if (i >= WarmUps)
            {
                into.Add(watch.Elapsed.TotalMilliseconds);
            }
        }
    }

    private static double Median(List<double> times)
    {
        var sorted = times.OrderBy(t => t).ToList();
        return sorted[sorted.Count / 2];
    }

    /// <summary>
    /// The four strips' weights. Plain: one set to a strip. Mixed: each strip the sets of its kind in clumps, by a
    /// noise of 31 m and one of 9 m, as the island's own surface rule lays them.
    /// </summary>
    private static float[,,] Weights(string[] sets, bool mix)
    {
        int At(string set) => System.Array.IndexOf(sets, set);
        var plain = new[] { At("grass_ground"), At("forest_ground_04"), At("brown_mud_03"), At("rocks_ground_06") };
        var second = new[] { At("forest_ground_04"), At("forest_leaves_03"), At("dirt_floor"), At("gravel_ground_01") };
        var third = new[] { At("dirt_floor"), At("grass_ground"), At("gravel_ground_01"), At("sand_03") };
        var secondShare = new[] { 0.4f, 0.5f, 0.5f, 0.5f };
        var thirdShare = new[] { 0.2f, 0.25f, 0.3f, 0.35f };
        var maps = new float[Alpha, Alpha, sets.Length];
        var metre = Size / Alpha;
        for (var z = 0; z < Alpha; z++)
        {
            for (var x = 0; x < Alpha; x++)
            {
                var px = (x + 0.5f) * metre;
                var pz = (z + 0.5f) * metre;
                var edge = px + (3f * (Fbm(px, pz, 12f, 31) - 0.5f));
                var at = Mathf.Clamp(edge / Strip, 0f, 3.999f);
                var strip = (int)at;
                // A strip's last two metres run into the next.
                var into = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.94f, 1f, at - strip));
                for (var side = 0; side < 2; side++)
                {
                    var s = Mathf.Min(strip + side, 3);
                    var share = side == 0 ? 1f - into : into;
                    if (share <= 0f)
                    {
                        continue;
                    }

                    if (!mix)
                    {
                        maps[z, x, plain[s]] += share;
                        continue;
                    }

                    var b = secondShare[s] * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.4f, 0.64f, (0.65f * Fbm(px, pz, 31f, 41 + s)) + (0.35f * Fbm(px, pz, 9f, 51 + s))));
                    var c = thirdShare[s] * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 0.7f, (0.5f * Fbm(px, pz, 19f, 61 + s)) + (0.5f * Fbm(px, pz, 6f, 71 + s))));
                    maps[z, x, third[s]] += share * c;
                    maps[z, x, second[s]] += share * (1f - c) * b;
                    maps[z, x, plain[s]] += share * (1f - c) * (1f - b);
                }
            }
        }

        return maps;
    }

    private static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            var h = (uint)((x * 374761393) + (y * 668265263) + (seed * 1442695041));
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFFFF) / 16777215f;
        }
    }

    private static float Noise(float x, float y, int seed)
    {
        var ix = Mathf.FloorToInt(x);
        var iy = Mathf.FloorToInt(y);
        var tx = x - ix;
        var ty = y - iy;
        tx = tx * tx * (3f - (2f * tx));
        ty = ty * ty * (3f - (2f * ty));
        var top = Mathf.Lerp(Hash(ix, iy, seed), Hash(ix + 1, iy, seed), tx);
        var bottom = Mathf.Lerp(Hash(ix, iy + 1, seed), Hash(ix + 1, iy + 1, seed), tx);
        return Mathf.Lerp(top, bottom, ty);
    }

    /// <summary>Three octaves of value noise, 0..1, the first of the given cell in metres.</summary>
    private static float Fbm(float x, float y, float cell, int seed) =>
        (0.57f * Noise(x / cell, y / cell, seed)) + (0.29f * Noise(x / (cell * 0.47f), y / (cell * 0.47f), seed + 101)) +
        (0.14f * Noise(x / (cell * 0.23f), y / (cell * 0.23f), seed + 211));
}

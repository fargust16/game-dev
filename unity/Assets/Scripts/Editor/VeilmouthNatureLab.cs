using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using GameDev.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Debug = UnityEngine.Debug;
using Random = System.Random;

/// <summary>
/// The island's living cover on four trial plots of the lab's ground (Documentation/Terrain/TERRAIN_POLISH_REPORT.md
/// §3): woods, copses, shrubs, tall and short grass, boulders, and the passages between meadow, wood, marsh and
/// shore. Nothing is strewn evenly: every kind stands where its ground is (the planner's zones, the slope, the
/// height over the sea, where water gathers) and in clumps (a parent and its offspring, a species to a stand, a
/// large stone with its small ones). Seeded, so a run places the same every time. Veilmouth.unity is not opened.
/// <para>Batch, with graphics: -executeMethod VeilmouthNatureLab.BuildAndRender</para>
/// The models are the slice's kit wrappers already in the project. Every number is an authoring default.
/// </summary>
public static class VeilmouthNatureLab
{
    private const string DataFolder = "Assets/Data/Slice/Island";
    private const string ScenePath = "Assets/Scenes/Slice/Lab/IslandLab.unity";
    private const string Kit = "Assets/Prefabs/SliceKit";
    private const string Out = "Logs/NatureLab";
    private const float Size = 1000f, SeaLevel = 24f;
    private const int Seed = 20261114;
    private const int Forest = 0, Marsh = 1, Farm = 2, Wet = 3, Bare = 4, Sand = 5, Hard = 6;

    /// <summary>A plot by plan metres (px east, py south): its centre and half its side, m.</summary>
    private static readonly (string Name, float X, float Y, float Half)[] Plots =
    {
        ("valley_wood", 420f, 250f, 90f),
        ("east_combes", 690f, 330f, 90f),
        ("west_bay", 205f, 440f, 90f),
        ("marsh_spit", 745f, 850f, 85f),
    };

    private static readonly string[] Canopy =
    {
        "Tree/Kit_Tree_GardenBeechA_1x11", "Tree/Kit_Tree_GardenHornbeamB_1x10", "Tree/Kit_Tree_StreetBeechA_1x9",
        "Tree/Kit_Tree_StreetBeechB_1x9", "Tree/Kit_Tree_StreetHornbeamA_1x10", "Tree/Kit_Tree_StreetHornbeamB_1x9",
    };

    private static readonly string[] Birch = { "Tree/Kit_Tree_Birch1_1x11", "Tree/Kit_Tree_Birch2_1x13", "Tree/Kit_Tree_Birch3_1x13" };
    private static readonly string[] Gnarled = { "Tree/Kit_Tree_Warped1_1x12", "Tree/Kit_Tree_Warped2_1x12", "Tree/Kit_Tree_Warped3_1x9" };
    private static readonly string[] Shrub = { "Bush/Kit_Bush_006_1x2", "Bush/Kit_Bush_011_1x2", "Bush/Kit_Bush_021_1x2" };
    private const string Veteran = "Tree/Kit_Tree_MatureHornbeamA_1x12";
    private const string Boulder = "Rocks/Kit_Rocks_Boulder_2x1";

    [System.Serializable]
    private sealed class Island
    {
        public int zonemap;
        public string[] zones;
    }

    private static byte[] _zones;
    private static int _zoneSize;
    private static Terrain _terrain;
    private static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();

    public static void BuildAndRender()
    {
        Build();
        Render();
    }

    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        _terrain = Object.FindFirstObjectByType<Terrain>();
        var island = JsonUtility.FromJson<Island>(File.ReadAllText(DataFolder + "/island.json"));
        _zoneSize = island.zonemap;
        _zones = Unzip(DataFolder + "/island_zones.bytes");

        var old = GameObject.Find("Nature");
        if (old != null)
        {
            Object.DestroyImmediate(old);
        }

        var root = new GameObject("Nature").transform;
        Wind(root);
        var random = new Random(Seed);
        var taken = new List<(Vector2 At, float Radius)>();
        var counts = new Dictionary<string, int>();

        void Count(string what) => counts[what] = counts.TryGetValue(what, out var n) ? n + 1 : 1;

        foreach (var (name, cx, cy, half) in Plots)
        {
            var plot = new GameObject(name).transform;
            plot.SetParent(root, false);
            var x0 = cx - half - 500f;
            var z0 = 500f - cy - half;
            var side = 2f * half;

            // Woodland: a stand keeps to one species (a noise of 60 m picks it), denser in groves and thin in glades
            // (a noise of 46 m), and no two trees stand closer than their crowns allow.
            foreach (var p in Jittered(x0, z0, side, 3.2f, random))
            {
                var f = Zone(Forest, p);
                if (f < 0.12f || Zone(Bare, p) > 0.3f || Slope(p) > 38f || Above(p) < 1.2f)
                {
                    continue;
                }

                var grove = Fbm(p.x, p.y, 46f, 5);
                if (random.NextDouble() > f * f * (0.2f + (1.3f * Step(0.3f, 0.7f, grove))) * 0.55f)
                {
                    continue;
                }

                string[] kind;
                if (Zone(Wet, p) > 0.4f || Zone(Marsh, p) > 0.2f)
                {
                    kind = Birch;
                }
                else if (Above(p) > 30f || Zone(Hard, p) > 0.6f)
                {
                    kind = Gnarled;
                }
                else
                {
                    kind = Canopy;
                }

                // Which of the kind's models: one a stand, by a noise of 60 m, with a stray among them now and then.
                var pick = Mathf.Clamp((int)(Fbm(p.x, p.y, 60f, 9) * 1.6f * kind.Length) % kind.Length, 0, kind.Length - 1);
                if (random.NextDouble() < 0.18)
                {
                    pick = random.Next(kind.Length);
                }

                var prefab = kind == Canopy && f > 0.8f && random.NextDouble() < 0.035 ? Veteran : kind[pick];
                // Crowns of unequal reach: an even spacing reads as an orchard.
                var radius = (prefab == Veteran ? 6.5f : kind == Canopy ? 3.1f : 2.4f) * Lerp(random, 0.6f, 1.4f, 1f);
                if (Free(taken, p, radius))
                {
                    Place(prefab, plot, p, Lerp(random, 0.75f, 1.45f, 1.6f), random, 0f);
                    taken.Add((p, radius));
                    Count("trees in woods");
                }
            }

            // Copses on open ground: a parent point for about each 70 m square, three to eight trees of one species
            // round it, and shrubs at their feet.
            foreach (var parent in Jittered(x0, z0, side, 70f, random))
            {
                if (random.NextDouble() > 0.45 || !Open(parent, 16f))
                {
                    continue;
                }

                var kind = random.NextDouble() < 0.5 ? Birch : Canopy;
                var model = kind[random.Next(kind.Length)];
                var n = 3 + random.Next(6);
                for (var i = 0; i < n; i++)
                {
                    var p = parent + Gauss(random, 8f);
                    if (Open(p, 20f) && Free(taken, p, 2.6f))
                    {
                        Place(model, plot, p, Lerp(random, 0.8f, 1.2f, 1.5f), random, 0f);
                        taken.Add((p, 2.6f));
                        Count("trees in copses");
                    }
                }

                for (var i = 0; i < 2 + random.Next(4); i++)
                {
                    var p = parent + Gauss(random, 10f);
                    if (Open(p, 24f) && Free(taken, p, 1.2f))
                    {
                        Place(Shrub[random.Next(Shrub.Length)], plot, p, Lerp(random, 0.8f, 1.5f, 1f), random, 0f);
                        taken.Add((p, 1.2f));
                        Count("shrubs");
                    }
                }
            }

            // Shrubs: a wood's edge is a belt of them, and they follow the damp lines across open ground.
            foreach (var p in Jittered(x0, z0, side, 2.4f, random))
            {
                var f = Zone(Forest, p);
                var edge = Step(0.08f, 0.3f, f) * Step(0.62f, 0.4f, f);
                var damp = f < 0.5f ? Step(0.45f, 0.8f, Zone(Wet, p)) : 0f;
                var clump = Step(0.45f, 0.7f, Fbm(p.x, p.y, 17f, 21));
                var under = 0.05f * Step(0.5f, 0.9f, f) * clump;   // an understorey in the woods, thin and in patches
                var chance = (0.2f * edge * clump) + (0.07f * damp * (0.4f + clump)) + under;
                if (chance <= 0f || random.NextDouble() > chance || Zone(Bare, p) > 0.3f || Zone(Farm, p) > 0.5f || Slope(p) > 34f || Above(p) < 1f)
                {
                    continue;
                }

                if (Free(taken, p, 1.1f))
                {
                    var model = Shrub[Mathf.Clamp((int)(Fbm(p.x, p.y, 28f, 23) * 1.6f * Shrub.Length) % Shrub.Length, 0, Shrub.Length - 1)];
                    Place(model, plot, p, Lerp(random, 0.8f, 1.6f, 1.4f), random, 0f);
                    taken.Add((p, 1.1f));
                    Count("shrubs");
                }
            }

            // Stones: on a rocky shore, on steep ground and in the stream beds; a large one with its small ones.
            foreach (var p in Jittered(x0, z0, side, 4f, random))
            {
                var h = Above(p);
                var slope = Slope(p);
                var shore = Zone(Hard, p) > 0.5f && h > 0.1f && h < 5f ? 0.1f : 0f;
                var scree = Step(22f, 30f, slope) * Step(44f, 36f, slope) * 0.07f;
                var bed = Zone(Wet, p) > 0.75f && slope > 5f ? 0.07f : 0f;
                var chance = Mathf.Max(shore, Mathf.Max(scree, bed)) * (0.3f + (1.4f * Step(0.4f, 0.7f, Fbm(p.x, p.y, 13f, 31))));
                if (chance <= 0f || random.NextDouble() > chance || Zone(Bare, p) > 0.5f || h < 0.1f)
                {
                    continue;
                }

                var big = Lerp(random, 0.45f, 1.9f, 2.5f);
                Place(Boulder, plot, p, big, random, 0.28f, true);
                taken.Add((p, big));
                Count("boulders");
                for (var i = random.Next(5); i > 0; i--)
                {
                    var q = p + Gauss(random, 1.2f + big);
                    if (Above(q) > 0.05f && Zone(Bare, q) < 0.5f)
                    {
                        Place(Boulder, plot, q, Lerp(random, 0.15f, 0.45f, 1f), random, 0.3f, true);
                        Count("small stones");
                    }
                }
            }
        }

        Grass(counts);
        EditorSceneManager.SaveScene(scene);

        var report = new StringBuilder();
        report.AppendLine($"Nature lab, {System.DateTime.Now:yyyy-MM-dd HH:mm}: {Plots.Length} plots, {Plots.Sum(p => 4f * p.Half * p.Half) / 10000f:0.0} ha");
        foreach (var pair in counts.OrderBy(c => c.Key))
        {
            report.AppendLine($"  {pair.Key}: {pair.Value:N0}");
        }

        var renderers = root.GetComponentsInChildren<MeshRenderer>(true).Length;
        var triangles = root.GetComponentsInChildren<MeshFilter>(true).Where(m => m.sharedMesh != null)
            .Sum(m => Enumerable.Range(0, m.sharedMesh.subMeshCount).Sum(s => (long)m.sharedMesh.GetIndexCount(s)) / 3);
        report.AppendLine($"  game objects under Nature: {root.GetComponentsInChildren<Transform>(true).Length:N0}; mesh renderers {renderers:N0}; LOD groups {root.GetComponentsInChildren<LODGroup>(true).Length:N0}; triangles in all their meshes, every LOD counted, {triangles:N0}");
        Directory.CreateDirectory(Out);
        File.WriteAllText(Out + "/build.txt", report.ToString());
        Debug.Log("[NatureLab]\n" + report);
    }

    /// <summary>
    /// Grass as the terrain's own details, the Overworld's clump (no second grass system): a short sward nearly
    /// everywhere turf lies, thinner under the trees, and tall grass in clumps of 14 m, along the damp lines and at
    /// the woods' edges. One cell of the detail map is about a metre; a value is the clumps in that cell.
    /// </summary>
    private static void Grass(Dictionary<string, int> counts)
    {
        var data = _terrain.terrainData;
        var source = OverworldVegetationAssets.DetailPrototypes(out var grass);
        if (grass < 0)
        {
            Debug.LogWarning("[NatureLab] the grass clump is missing: no grass laid");
            return;
        }

        // The Overworld's clump in the island's own turf colour: its green is the Overworld's, far brighter than this
        // ground (seen in the lab), and grass that does not match its ground reads as a carpet laid on it.
        var blade = source[grass].prototype;
        var material = new Material(blade.GetComponent<MeshRenderer>().sharedMaterial) { name = "IslandLab_Grass", enableInstancing = true };
        foreach (var (property, colour) in new[] { ("_TopColor", new Color(0.43f, 0.47f, 0.2f)), ("_GroundColor", new Color(0.27f, 0.3f, 0.13f)) })
        {
            if (material.HasProperty(property))
            {
                material.SetColor(property, colour);
            }
            else
            {
                Debug.LogWarning("[NatureLab] the grass material has no " + property);
            }
        }

        // The Polytope plant shader takes its two colours only with this toggle on; off, the clump keeps its texture's green.
        material.SetFloat("_CUSTOMCOLORSTINTING", 1f);
        material.EnableKeyword("_CUSTOMCOLORSTINTING_ON");
        AssetDatabase.DeleteAsset(DataFolder + "/IslandLab_Grass.mat");
        AssetDatabase.CreateAsset(material, DataFolder + "/IslandLab_Grass.mat");
        var tuft = new GameObject("IslandLab_GrassDetail");
        tuft.AddComponent<MeshFilter>().sharedMesh = blade.GetComponent<MeshFilter>().sharedMesh;
        tuft.AddComponent<MeshRenderer>().sharedMaterial = material;
        var own = PrefabUtility.SaveAsPrefabAsset(tuft, DataFolder + "/IslandLab_GrassDetail.prefab");
        Object.DestroyImmediate(tuft);

        DetailPrototype Sized(float low, float high) => new DetailPrototype(source[grass]) { prototype = own, useInstancing = true, minWidth = low, maxWidth = high, minHeight = low, maxHeight = high };
        const int resolution = 1024;
        data.SetDetailResolution(resolution, 32);
        data.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);
        data.detailPrototypes = new[] { Sized(0.45f, 0.8f), Sized(1.1f, 1.9f) };
        var low = new int[resolution, resolution];
        var tall = new int[resolution, resolution];
        var metre = Size / resolution;
        long lowCount = 0, tallCount = 0;
        foreach (var (_, cx, cy, half) in Plots)
        {
            var i0 = Mathf.Max(0, (int)((cx - half) / metre));
            var i1 = Mathf.Min(resolution - 1, (int)((cx + half) / metre));
            var j0 = Mathf.Max(0, (int)((Size - cy - half) / metre));
            var j1 = Mathf.Min(resolution - 1, (int)((Size - cy + half) / metre));
            for (var j = j0; j <= j1; j++)
            {
                for (var i = i0; i <= i1; i++)
                {
                    var p = new Vector2(((i + 0.5f) * metre) - 500f, ((j + 0.5f) * metre) - 500f);
                    var h = Above(p);
                    if (h < 0.8f || Slope(p) > 32f || Zone(Bare, p) > 0.25f || Zone(Sand, p) * Step(4f, 1.5f, h) > 0.4f || Zone(Hard, p) * Step(6f, 2f, h) > 0.5f)
                    {
                        continue;
                    }

                    var f = Zone(Forest, p);
                    var open = 1f - (0.8f * Step(0.3f, 0.8f, f));
                    var sward = open * (0.55f + (0.9f * Fbm(p.x, p.y, 23f, 41))) * (1f - (0.7f * Zone(Farm, p)));
                    low[j, i] = Mathf.Clamp(Mathf.RoundToInt(2.4f * sward), 0, 3);
                    var edge = Step(0.1f, 0.3f, f) * Step(0.6f, 0.35f, f);
                    var rank = Mathf.Max(Step(0.56f, 0.7f, Fbm(p.x, p.y, 14f, 43)), Mathf.Max(0.8f * edge, Mathf.Max(Step(0.4f, 0.8f, Zone(Wet, p)), Zone(Marsh, p))));
                    tall[j, i] = Mathf.Clamp(Mathf.RoundToInt(2f * rank * open), 0, 3);
                    lowCount += low[j, i];
                    tallCount += tall[j, i];
                }
            }
        }

        data.SetDetailLayer(0, 0, 0, low);
        data.SetDetailLayer(0, 0, 1, tall);
        _terrain.detailObjectDistance = 80f;
        _terrain.detailObjectDensity = 1f;
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        counts["grass clumps, short"] = (int)lowCount;
        counts["grass clumps, tall"] = (int)tallCount;
    }

    // ----------------------------------------------------------------------------------------------- views

    /// <summary>A view by plan metres and metres over the sea; an eye of 0 stands on the ground.</summary>
    private static readonly (string Name, Vector3 Eye, Vector3 Look, float Fov)[] Views =
    {
        ("valley_wood_air", new Vector3(520f, 370f, 75f), new Vector3(410f, 245f, 4f), 55f),
        ("valley_wood_eye", new Vector3(452f, 300f, 0f), new Vector3(400f, 230f, 6f), 60f),
        ("east_combes_air", new Vector3(600f, 450f, 80f), new Vector3(700f, 320f, 20f), 55f),
        ("east_combes_eye", new Vector3(640f, 372f, 0f), new Vector3(720f, 310f, 30f), 60f),
        ("west_bay_air", new Vector3(120f, 560f, 60f), new Vector3(215f, 430f, 4f), 55f),
        ("west_bay_eye", new Vector3(196f, 392f, 0f), new Vector3(230f, 470f, 8f), 60f),
        ("marsh_spit_air", new Vector3(860f, 950f, 55f), new Vector3(745f, 845f, 2f), 55f),
        ("marsh_spit_eye", new Vector3(770f, 822f, 0f), new Vector3(720f, 880f, 2f), 60f),
        ("island_air", new Vector3(1020f, 1160f, 330f), new Vector3(500f, 480f, 0f), 50f),
    };

    public static void Render()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        _terrain = Object.FindFirstObjectByType<Terrain>();
        Directory.CreateDirectory(Out);
        var rig = new GameObject("Lab Camera") { hideFlags = HideFlags.HideAndDontSave };
        var camera = rig.AddComponent<Camera>();
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 4000f;
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.allowHDR = true;
        var extra = camera.GetUniversalAdditionalCameraData();
        extra.renderPostProcessing = true;
        var target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.DefaultHDR) { antiAliasing = 4 };
        var shot = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        foreach (var (name, eye, look, fov) in Views)
        {
            Aim(camera, eye, look, fov);
            camera.targetTexture = target;
            camera.Render();
            camera.Render();
            RenderTexture.active = target;
            shot.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            shot.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes($"{Out}/{name}.jpg", shot.EncodeToJPG(90));
        }

        // What the cover costs: each view with all of it, without the grass, and with the bare ground alone.
        var nature = GameObject.Find("Nature");
        var samples = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp ? urp.msaaSampleCount : 1;
        var big = new RenderTexture(1920, 1080, 24) { antiAliasing = Mathf.Max(1, samples) };
        var pixel = new Texture2D(1, 1, TextureFormat.RGB24, false);
        extra.renderPostProcessing = false;
        camera.targetTexture = big;
        var report = new StringBuilder();
        report.AppendLine($"One camera's render at 1920 x 1080, MSAA {samples}, no post-processing, median ms of 3 rounds of 12 frames after 3 warm-ups, {SystemInfo.graphicsDeviceName}:");
        report.AppendLine("view".PadRight(22) + "all the cover".PadLeft(16) + "LOD bias halved".PadLeft(18) + "no cover shadows".PadLeft(18) + "without grass".PadLeft(16) + "bare ground".PadLeft(16));
        var bias = QualitySettings.lodBias;
        var casters = nature == null ? new MeshRenderer[0] : nature.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off).ToArray();
        foreach (var (name, eye, look, fov) in Views)
        {
            Aim(camera, eye, look, fov);
            var all = new List<double>();
            var coarse = new List<double>();
            var unshadowed = new List<double>();
            var noGrass = new List<double>();
            var bare = new List<double>();
            for (var round = 0; round < 3; round++)
            {
                _terrain.drawTreesAndFoliage = true;
                nature?.SetActive(true);
                Time(camera, big, pixel, all);
                QualitySettings.lodBias = bias * 0.5f;   // every model steps to its coarser LOD at half the distance
                Time(camera, big, pixel, coarse);
                QualitySettings.lodBias = bias;
                foreach (var caster in casters)
                {
                    caster.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }

                Time(camera, big, pixel, unshadowed);
                foreach (var caster in casters)
                {
                    caster.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }

                _terrain.drawTreesAndFoliage = false;
                Time(camera, big, pixel, noGrass);
                nature?.SetActive(false);
                Time(camera, big, pixel, bare);
            }

            report.AppendLine(name.PadRight(22) + $"{Median(all):0.00}".PadLeft(16) + $"{Median(coarse):0.00}".PadLeft(18) + $"{Median(unshadowed):0.00}".PadLeft(18) + $"{Median(noGrass):0.00}".PadLeft(16) + $"{Median(bare):0.00}".PadLeft(16));
        }

        report.AppendLine($"LOD bias of the project's quality level: {bias:0.##}");

        _terrain.drawTreesAndFoliage = true;
        nature?.SetActive(true);
        camera.targetTexture = null;
        Object.DestroyImmediate(rig);
        target.Release();
        big.Release();
        File.WriteAllText(Out + "/frame.txt", report.ToString());
        Debug.Log("[NatureLab]\n" + report);
    }

    private static void Aim(Camera camera, Vector3 eye, Vector3 look, float fov)
    {
        Vector3 World(Vector3 plan, float standing)
        {
            var x = plan.x - 500f;
            var z = 500f - plan.y;
            var ground = _terrain.SampleHeight(new Vector3(x, 0f, z)) + _terrain.transform.position.y;
            return new Vector3(x, plan.z > 0f ? Mathf.Max(SeaLevel + plan.z, ground + 1.7f) : ground + standing, z);
        }

        var from = World(eye, 1.7f);
        camera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(World(look, 0f) - from));
        camera.fieldOfView = fov;
    }

    private static void Time(Camera camera, RenderTexture target, Texture2D pixel, List<double> into)
    {
        for (var i = 0; i < 15; i++)
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
            if (i >= 3)
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

    // ----------------------------------------------------------------------------------------------- the ground

    private static float Zone(int kind, Vector2 world)
    {
        var x = Mathf.Clamp((int)((world.x + 500f) / Size * _zoneSize), 0, _zoneSize - 1);
        var z = Mathf.Clamp((int)((world.y + 500f) / Size * _zoneSize), 0, _zoneSize - 1);
        return _zones[(kind * _zoneSize * _zoneSize) + (z * _zoneSize) + x] / 255f;
    }

    /// <summary>The shader's smoothstep: 0 at the first edge, 1 at the second, either way round.</summary>
    private static float Step(float from, float to, float value)
    {
        var t = Mathf.InverseLerp(from, to, value);
        return t * t * (3f - (2f * t));
    }

    /// <summary>Metres over the sea.</summary>
    private static float Above(Vector2 world) =>
        _terrain.SampleHeight(new Vector3(world.x, 0f, world.y)) + _terrain.transform.position.y - SeaLevel;

    /// <summary>The ground's slope, degrees.</summary>
    private static float Slope(Vector2 world) =>
        _terrain.terrainData.GetSteepness((world.x + 500f) / Size, (world.y + 500f) / Size);

    /// <summary>Open ground a tree may stand on away from the woods: dry, not too steep, not a field, a road or built.</summary>
    private static bool Open(Vector2 p, float steepest) =>
        Zone(Forest, p) < 0.2f && Zone(Bare, p) < 0.2f && Zone(Farm, p) < 0.4f && Zone(Marsh, p) < 0.3f &&
        Zone(Sand, p) * Step(5f, 2f, Above(p)) < 0.4f && Slope(p) < steepest && Above(p) > 1.5f;

    private static bool Free(List<(Vector2 At, float Radius)> taken, Vector2 p, float radius)
    {
        for (var i = taken.Count - 1; i >= 0; i--)
        {
            var reach = (taken[i].Radius + radius) * 0.8f;
            if ((taken[i].At - p).sqrMagnitude < reach * reach)
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<Vector2> Jittered(float x0, float z0, float side, float step, Random random)
    {
        var n = Mathf.CeilToInt(side / step);
        for (var j = 0; j < n; j++)
        {
            for (var i = 0; i < n; i++)
            {
                yield return new Vector2(x0 + ((i + (float)random.NextDouble()) * step), z0 + ((j + (float)random.NextDouble()) * step));
            }
        }
    }

    private static Vector2 Gauss(Random random, float sigma)
    {
        var r = sigma * Mathf.Sqrt(-2f * Mathf.Log(Mathf.Max(1e-6f, (float)random.NextDouble())));
        var a = 2f * Mathf.PI * (float)random.NextDouble();
        return new Vector2(r * Mathf.Cos(a), r * Mathf.Sin(a));
    }

    /// <summary>A size between two, the small ones the commoner by the given power.</summary>
    private static float Lerp(Random random, float low, float high, float power) =>
        Mathf.Lerp(low, high, Mathf.Pow((float)random.NextDouble(), power));

    private static void Place(string model, Transform parent, Vector2 at, float scale, Random random, float sunk, bool tumbled = false)
    {
        if (!Prefabs.TryGetValue(model, out var prefab))
        {
            Prefabs[model] = prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Kit}/{model}.prefab");
            if (prefab == null)
            {
                Debug.LogError("[NatureLab] no kit wrapper " + model);
            }
        }

        if (prefab == null)
        {
            return;
        }

        var piece = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        var ground = _terrain.SampleHeight(new Vector3(at.x, 0f, at.y)) + _terrain.transform.position.y;
        piece.transform.position = new Vector3(at.x, ground - (sunk * scale), at.y);
        piece.transform.rotation = tumbled
            ? Quaternion.Euler((float)random.NextDouble() * 360f, (float)random.NextDouble() * 360f, (float)random.NextDouble() * 360f)
            : Quaternion.Euler(((float)random.NextDouble() * 8f) - 4f, (float)random.NextDouble() * 360f, ((float)random.NextDouble() * 8f) - 4f);
        piece.transform.localScale = prefab.transform.localScale * scale;
    }

    /// <summary>The Stylized Trees Pack's wind, as VeilmouthDressing sets it for the city: without one its trees and bushes do not draw.</summary>
    private static void Wind(Transform root)
    {
        var wind = new GameObject("Wind");
        wind.transform.SetParent(root, false);
        wind.transform.rotation = Quaternion.Euler(0f, 60f, 0f);
        var zone = wind.AddComponent<WindZone>();
        zone.mode = WindZoneMode.Directional;
        zone.windMain = 0.6f;
        zone.windTurbulence = 0.3f;
        var type = System.Type.GetType("Growth.WindController, Assembly-CSharp");
        if (type == null)
        {
            Debug.LogError("[NatureLab] Growth.WindController not found: the pack's trees will not show.");
            return;
        }

        var controller = new SerializedObject(wind.AddComponent(type));
        controller.FindProperty("noise").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Stylized Trees Pack/Shared/Textures/Noise/Wind Noise 001.png");
        controller.FindProperty("strength").floatValue = 0.07f;
        controller.FindProperty("speed").floatValue = 2.5f;
        controller.ApplyModifiedPropertiesWithoutUndo();
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

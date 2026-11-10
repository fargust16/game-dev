using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Measures the vendor models of SLICE_ASSET_MAP before they are wrapped as kit blocks: the size of what is seen,
    /// where it sits against its pivot, its colliders, and its shaders. It also draws each model from the front (+Z) and
    /// from above into contact sheets, so the agent can see which way a model faces. It writes only outside Assets,
    /// to Logs/SliceSurvey, and changes no asset or scene. Batch: -executeMethod GameDev.Slice.EditorTools.SliceAssetSurvey.Run
    /// </summary>
    public static class SliceAssetSurvey
    {
        private const string OutputFolder = "Logs/SliceSurvey";
        private const int Tile = 256;
        private const int Columns = 6;

        /// <summary>Groups of (vendor folder, file name without extension).</summary>
        private static readonly (string Group, string Folder, string[] Names)[] Groups =
        {
            ("buildings", "Assets/Modular Victorian City/Prefabs/Buldings",
                new[] { "BuildBlue", "BuildGreen 1", "BuildGreen", "Building 1 (2)", "Building 1", "Building 2", "Building 3", "Building 4", "Building", "BuildRed" }),
            ("buildings", "Assets/Modular European House", new[] { "Modular European House" }),
            ("buildings", "Assets/FantasyMedievalTown_LITE", new[] { "House_01_LITE" }),
            ("buildings", "Assets/3DShapes", new[] { "Church", "Graveyard_ground", "Graveyard_wall", "Grave", "StreetLamp1" }),
            ("market", "Assets/Low-Poly Medieval Market",
                new[] { "BakeryMarket", "Bakery_market_with_food", "Fish_market_with_sections", "Meat_market_with_objects", "Vegetable_market_with_objects", "Weapon_Market_with_objects", "anvil_with_hammer", "leather_dressing_frame", "Armor_stand", "table_with_bottels", "Table_with_weapon", "stand_01", "lamp_post", "Latern", "wooden_box", "stone_fence_01" }),
            ("props", "Assets/Modular Victorian City/Prefabs/Basic elements/Street", new[] { "Lamp post", "Bench", "Wooden bench", "Trash can", "Steps", "Column" }),
            ("props", "Assets/Multistory Dungeons 2/Prefabs/Props",
                new[] { "Barrels_Preset_01", "Barrel_01", "Cupboard_Big_Preset_01", "Cupboard_Preset_01", "Shelf_Preset_01", "Table_Preset_01", "Table_01", "Chair_Wooden_01", "Bench_Wooden_01", "Candlestick_Floor_01_Lit", "Books_Preset_01", "Retort_Preset_01", "Drawer_Preset_01", "Treasure_Chest_01" }),
            ("props", "Assets/LowPolyMedievalPropsLite", new[] { "Barrel_01", "Box_01", "Fence_01", "Furniture_01", "Furniture_02", "Furniture_03", "Firewood_01" }),
            ("props", "Assets/EmaceArt", new[] { "EA03_Fence_Beam_01a_PRE", "EA03_Fence_ByAFieldField_01a_PRE", "EA03_Fence_Beam_Gate_01a_PRE" }),
            ("props", "Assets/LowPolyDungeonsLite", new[] { "Table_01", "Chair_05", "Wall_27", "Ground_01" }),
            ("tripo", "Assets/TripoModels",
                new[] { "ornate_wooden_bar_counter_3d_model", "ornate_cabinet_3d_model", "rusty_iron_gate_3d_model", "rusty_iron_jail_door_3d_model", "ornate_wooden_bed_3d_model", "ornate_notice_board_3d_model", "rugged_rock_formation_3d_model", "flintlock_pistol_3d_model", "ornate_flintlock_rifle_3d_model", "leather_satchel_3d_model" }),
            ("trees", "Assets/Stylized Trees Pack/Assets/Prefabs/Trees", new[] { "Birch 001", "Birch 002", "Birch 003", "Warped 001", "Warped 002", "Warped 003" }),
            ("bushes", "Assets/Stylized Trees Pack/Assets/Prefabs/Bushes",
                Enumerable.Range(1, 25).Select(i => $"Bush {i:000}").ToArray()),
            ("beasts", "Assets/FourEvilDragonsHP/Prefab", new[] { "Grey", "Albino" }),
            ("bridges", "Assets/Julien Tonsuso", new[] { "bridge_01", "bridge_02", "bridge_03", "bridge_04" }),
            ("people", "Assets/Kevin Iglesias/Human Character Dummy/Prefabs", new[] { "HumanDummy_M White" }),

            // C1 (2026-11-05): the modules a house is built from, and the interior kit.
            ("vmodules", "Assets/Modular Victorian City/Prefabs/Modules",
                new[] { "GroundWall1", "GroundDoor1", "GroundDoorC1", "GroundWindow1", "GroundWindowC1", "GroundCornelWall1", "GroundSideWall1", "UpWall", "UpWindow1", "UpWindowC1", "UpCornelWall1", "UpSideWall1", "Roof", "RoofCorner", "RoofUp", "RoofWall", "Roof Window", "Roof WindowC1", "Arch", "Fence", "Sidewalk Straight", "Sidewalk corner", "WoddenDoorBlue", "WoodenWallBlue" }),
            ("vbasic", "Assets/Modular Victorian City/Prefabs/Basic elements",
                new[] { "Door 1", "Door 2", "Double door", "Door frame", "Steps", "Half steps", "Arch", "Arch wall", "Arch entrance", "Wall1", "Wall2", "Wall3", "Wall4", "Fence column", "Top of the fence", "Bottom of the fence", "Straight border", "Сorner border", "Awning blue", "Glass wall  blue", "Door portal  blue", "Window 1", "Pipe" }),
            ("bk", "Assets/BK_AlchemistHouse/Prefabs",
                new[] { "IntWall01a", "IntWall01b", "IntWall01door", "IntWall02a", "IntWall02b", "IntWall02door", "IntWall02window01", "IntWoodFloor01", "StoneFloor01", "Floor", "IntCeiling01", "IntCeilingPillarBend", "VerticalPillar", "VerticalPillarBig", "HorizontalPillar", "WoodStairs", "Door", "WoodDoor01", "IntWoodFence01", "Stove", "TableSink", "WoodCupboard01", "WoodCupboard02", "WoodCupboard03", "WoodShelf01", "Drawer01", "WoodDrawer01", "WoodTableSquare01", "Table01", "Table04", "Chair01", "WoodStool", "Workbench", "Trunk01", "Carpet01", "Caldron01", "CaldronTripod01", "LabSetup01", "Oillamp01", "Candle01", "Books1", "Painting01" }),
        };

        // The owner's interior pack (2026-11-07): every prefab, so the interiors are planned from measurements.
        private const string VictorianInteriors = "Assets/Victorian Interiors/Victorian Interiors/Prefabs";

        private static readonly (string Group, string Folder, string[] Names)[] InteriorGroups =
        {
            ("vinterior", VictorianInteriors,
                Directory.Exists(VictorianInteriors)
                    ? Directory.GetFiles(VictorianInteriors, "*.prefab", SearchOption.AllDirectories).Select(Path.GetFileNameWithoutExtension).OrderBy(n => n).ToArray()
                    : new string[0]),
        };

        // The owner's Tripo models (2026-11-09): every model, before they furnish the interiors.
        private const string TripoModels = "Assets/TripoModels";

        private static readonly (string Group, string Folder, string[] Names)[] TripoGroups =
        {
            ("tripo_all", TripoModels,
                Directory.Exists(TripoModels)
                    ? Directory.GetFiles(TripoModels, "*.fbx", SearchOption.AllDirectories).Select(Path.GetFileNameWithoutExtension).OrderBy(n => n).ToArray()
                    : new string[0]),
        };

        /// <summary>Only the Tripo models. Batch: -executeMethod GameDev.Slice.EditorTools.SliceAssetSurvey.RunTripo</summary>
        public static void RunTripo()
        {
            Survey(TripoGroups, "tripo");
        }

        /// <summary>
        /// The owner's Tripo models of 2026-11-10, before they are wrapped or worn: each drawn with its colour image from
        /// the front (+Z), the side (+X) and above, with its seen size and its triangles. The colour image goes on an
        /// in-memory copy of the material, so no asset changes. Batch:
        /// -executeMethod GameDev.Slice.EditorTools.SliceAssetSurvey.RunTripoNew
        /// </summary>
        public static void RunTripoNew()
        {
            var names = new[]
            {
                "military_officer_3d_model", "victorian_woman_3d_model", "medieval_blacksmith_3d_model", "steampunk_detective_3d_model",
                "vintage_gentleman_3d_model", "victorian_dress_3d_model", "stylized_male_character_3d_model", "historical_woman_3d_model",
                "antique_wooden_cabinet_3d_model_Clone1", "ornate_vase_3d_model", "classical_bust_3d_model", "antique_barometer_3d_model",
                "bronze_statue_3d_model", "vintage_street_lamp_3d_model", "decorative_vase_3d_model_Clone1", "magnifying_glass_3d_model",
                "ornate_ink_desk_3d_model_Clone1", "ornate_radiator_3d_model",
                "military_uniform_3d_model", "human_figure_3d_model", "human_body_3d_model",
            };
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var light = new GameObject("Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientLight = Color.gray;
            var report = new StringBuilder("name\tsize_x\tsize_y\tsize_z\tmin_y\ttriangles\trenderers\tbones\n");
            var tiles = new List<Texture2D>();
            foreach (var name in names)
            {
                var path = Find(TripoModels, name);
                if (path == null)
                {
                    report.Append($"{name}\tMISSING\n");
                    continue;
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                Colour(instance, path);
                var renderers = instance.GetComponentsInChildren<Renderer>();
                var bounds = Bounds(renderers);
                var triangles = renderers.Select(r => r is SkinnedMeshRenderer s ? s.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh)
                    .Where(m => m != null).Sum(m => (long)m.triangles.Length / 3);
                var bones = renderers.OfType<SkinnedMeshRenderer>().Sum(s => s.bones.Length);
                report.Append($"{name}\t{bounds.size.x:0.###}\t{bounds.size.y:0.###}\t{bounds.size.z:0.###}\t{bounds.min.y:0.###}\t{triangles}\t{renderers.Length}\t{bones}\n");
                var size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z, 0.1f);
                var views = new[]
                {
                    Render(bounds.center + Vector3.forward * size * 2f, Quaternion.LookRotation(Vector3.back), size),
                    Render(bounds.center + Vector3.right * size * 2f, Quaternion.LookRotation(Vector3.left), size),
                    Render(bounds.center + Vector3.up * size * 2f, Quaternion.LookRotation(Vector3.down, Vector3.forward), size),
                };
                foreach (var view in views)
                {
                    for (var x = 0; x < Tile; x++)
                    {
                        view.SetPixel(x, Tile - 1, Color.red);
                    }

                    view.Apply();
                }

                tiles.AddRange(views);
                Object.DestroyImmediate(instance);
            }

            // Three views a model: front, side, top, two models a row.
            File.WriteAllBytes($"{OutputFolder}/tripo_new.png", Sheet3(tiles).EncodeToPNG());
            File.WriteAllText($"{OutputFolder}/tripo_new.tsv", report.ToString());
            Debug.Log($"[SliceAssetSurvey] the new Tripo models written to {OutputFolder}/tripo_new.*");
        }

        /// <summary>
        /// Measurements the wrappers of 2026-11-10 need, written to `tripo_profiles.txt`: each model's width across X and Z
        /// in 25 slices up its height (the street lamp's lantern, the statue's pedestal), the turn about Z that lays the
        /// magnifying glass flat, and a close view of the lamp's head, front and side (`lamp_head.png`). Batch:
        /// -executeMethod GameDev.Slice.EditorTools.SliceAssetSurvey.RunTripoProfiles
        /// </summary>
        public static void RunTripoProfiles()
        {
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var light = new GameObject("Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientLight = Color.gray;
            var report = new StringBuilder();
            foreach (var name in new[] { "vintage_street_lamp_3d_model", "bronze_statue_3d_model", "classical_bust_3d_model", "ornate_radiator_3d_model", "magnifying_glass_3d_model" })
            {
                var path = Find(TripoModels, name);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                var points = Points(model);
                var b = Bounds(model.GetComponentsInChildren<Renderer>());
                report.Append($"{name}: bounds min {b.min:F3} max {b.max:F3}\n  slice  y0     width_x width_z centre_x centre_z\n");
                const int slices = 25;
                for (var i = 0; i < slices; i++)
                {
                    var y0 = b.min.y + (b.size.y * i / slices);
                    var y1 = y0 + (b.size.y / slices);
                    var s = points.Where(p => p.y >= y0 && p.y <= y1).ToList();
                    if (s.Count == 0)
                    {
                        continue;
                    }

                    report.Append($"  {i,5}  {y0:0.000}  {s.Max(p => p.x) - s.Min(p => p.x):0.000}   {s.Max(p => p.z) - s.Min(p => p.z):0.000}   " +
                                  $"{(s.Max(p => p.x) + s.Min(p => p.x)) / 2f:0.000}    {(s.Max(p => p.z) + s.Min(p => p.z)) / 2f:0.000}\n");
                }

                if (name.StartsWith("magnifying"))
                {
                    var centre = b.center;
                    var best = Enumerable.Range(-30, 61).SelectMany(pitch => Enumerable.Range(-90, 181).Select(roll => (pitch, roll))).Select(t =>
                    {
                        var q = Quaternion.Euler(t.pitch, 0f, t.roll);
                        var ys = points.Select(p => (q * (p - centre)).y).ToList();
                        return (t.pitch, t.roll, height: ys.Max() - ys.Min());
                    }).OrderBy(t => t.height).First();
                    report.Append($"  flattest turn: {best.pitch}° about X, {best.roll}° about Z (height {best.height:0.000} of {b.size.y:0.000})\n");
                }

                if (name.StartsWith("vintage_street_lamp"))
                {
                    // The lantern's faces: in each 5 mm band, how far out the points of the faces' middles lie (not the corners).
                    report.Append("  lantern  y      n   face_out_max face_out_min\n");
                    for (var y = 0.70f; y < 0.90f; y += 0.005f)
                    {
                        var band = points.Where(p => p.y >= y && p.y < y + 0.005f)
                            .Select(p => (Out: Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.z)), Side: Mathf.Min(Mathf.Abs(p.x), Mathf.Abs(p.z))))
                            .Where(p => p.Side < p.Out * 0.6f && p.Out > 0.012f).ToList();
                        if (band.Count > 0)
                        {
                            report.Append($"  {y:0.000}  {band.Count,4}  {band.Max(p => p.Out):0.0000}  {band.Min(p => p.Out):0.0000}\n");
                        }
                    }
                }

                if (name.StartsWith("vintage_street_lamp"))
                {
                    Colour(model, path);
                    var head = new Bounds(new Vector3(b.center.x, b.max.y - (b.size.y * 0.15f), b.center.z), new Vector3(b.size.x, b.size.y * 0.3f, b.size.z));
                    var size = head.size.y;
                    var views = new List<Texture2D>
                    {
                        Render(head.center + Vector3.forward * size * 2f, Quaternion.LookRotation(Vector3.back), size),
                        Render(head.center + Vector3.right * size * 2f, Quaternion.LookRotation(Vector3.left), size),
                    };
                    File.WriteAllBytes($"{OutputFolder}/lamp_head.png", Sheet3(views).EncodeToPNG());
                }

                Object.DestroyImmediate(model);
            }

            File.WriteAllText($"{OutputFolder}/tripo_profiles.txt", report.ToString());
            Debug.Log($"[SliceAssetSurvey] profiles written to {OutputFolder}/tripo_profiles.txt");
        }

        private static List<Vector3> Points(GameObject model)
        {
            var points = new List<Vector3>();
            foreach (var f in model.GetComponentsInChildren<MeshFilter>())
            {
                var m = f.transform.localToWorldMatrix;
                points.AddRange(f.sharedMesh.vertices.Select(v => m.MultiplyPoint3x4(v)));
            }

            return points;
        }

        /// <summary>The model's colour image on in-memory copies of its materials (the Tripo Bridge leaves the base map empty).</summary>
        internal static void Colour(GameObject model, string source)
        {
            var folder = Path.ChangeExtension(source, ".fbm").Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var colour = AssetDatabase.FindAssets("t:Texture2D", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => Path.GetFileName(p).StartsWith("tripo_image_"));
            var image = colour != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(colour) : null;
            if (image == null)
            {
                return;
            }

            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterials = r.sharedMaterials.Select(m =>
                {
                    if (m == null || !m.HasProperty("_BaseMap"))
                    {
                        return m;
                    }

                    var copy = new Material(m);
                    copy.SetTexture("_BaseMap", image);
                    copy.SetColor("_BaseColor", Color.white);
                    return copy;
                }).ToArray();
            }
        }

        private static Texture2D Sheet3(List<Texture2D> tiles)
        {
            const int perRow = 6;
            var rows = Mathf.CeilToInt(tiles.Count / (float)perRow);
            var sheet = new Texture2D(perRow * Tile, Mathf.Max(1, rows) * Tile, TextureFormat.RGB24, false);
            for (var i = 0; i < tiles.Count; i++)
            {
                sheet.SetPixels(i % perRow * Tile, (rows - 1 - i / perRow) * Tile, Tile, Tile, tiles[i].GetPixels());
                Object.DestroyImmediate(tiles[i]);
            }

            sheet.Apply();
            return sheet;
        }

        [MenuItem("GameDev/Slice/Survey Slice Assets")]
        public static void Run()
        {
            Survey(Groups);
        }

        /// <summary>Only the Victorian Interiors pack. Batch: -executeMethod GameDev.Slice.EditorTools.SliceAssetSurvey.RunVictorianInteriors</summary>
        public static void RunVictorianInteriors()
        {
            Survey(InteriorGroups, "vinterior");
        }

        private static void Survey((string Group, string Folder, string[] Names)[] groups, string reportName = "survey")
        {
            Directory.CreateDirectory(OutputFolder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var light = new GameObject("Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientLight = Color.gray;

            var report = new StringBuilder("group\tname\tpath\tsize_x\tsize_y\tsize_z\tmin_x\tmin_y\tmin_z\tcolliders\trenderers\tshaders\n");
            foreach (var group in groups.GroupBy(g => g.Group))
            {
                var tiles = new List<Texture2D>();
                foreach (var (_, folder, names) in group)
                {
                    foreach (var name in names)
                    {
                        var path = Find(folder, name);
                        if (path == null)
                        {
                            report.Append($"{group.Key}\t{name}\tMISSING in {folder}\n");
                            continue;
                        }

                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                        var renderers = instance.GetComponentsInChildren<Renderer>().Where(r => !(r is ParticleSystemRenderer)).ToArray();
                        var bounds = Bounds(renderers);
                        var shaders = string.Join(",", renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null)
                            .Select(m => m.shader.name).Distinct());
                        report.Append($"{group.Key}\t{name}\t{path}\t{bounds.size.x:0.##}\t{bounds.size.y:0.##}\t{bounds.size.z:0.##}\t" +
                                      $"{bounds.min.x:0.##}\t{bounds.min.y:0.##}\t{bounds.min.z:0.##}\t" +
                                      $"{instance.GetComponentsInChildren<Collider>().Length}\t{renderers.Length}\t{shaders}\n");
                        tiles.Add(Draw(bounds, $"{name}"));
                        Object.DestroyImmediate(instance);
                    }
                }

                // At most 36 tiles a sheet, so a large pack stays readable.
                for (var start = 0; start < tiles.Count; start += 36)
                {
                    var name = tiles.Count > 36 ? $"{group.Key}_{(start / 36) + 1}" : group.Key;
                    File.WriteAllBytes($"{OutputFolder}/{name}.png", Sheet(tiles.Skip(start).Take(36).ToList()).EncodeToPNG());
                }
            }

            File.WriteAllText($"{OutputFolder}/{reportName}.tsv", report.ToString());
            Debug.Log($"Slice asset survey written to {OutputFolder}.");
        }

        private static string Find(string folder, string name)
        {
            return AssetDatabase.FindAssets(name, new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileNameWithoutExtension(p) == name && (p.EndsWith(".prefab") || p.EndsWith(".fbx")))
                .OrderBy(p => p.EndsWith(".prefab") ? 0 : 1)
                .FirstOrDefault();
        }

        private static Bounds Bounds(Renderer[] renderers)
        {
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            var b = renderers[0].bounds;
            foreach (var r in renderers.Skip(1))
            {
                b.Encapsulate(r.bounds);
            }

            return b;
        }

        /// <summary>The front (seen from +Z, looking toward -Z) on the left, the top (+Z up the image) on the right.</summary>
        private static Texture2D Draw(Bounds bounds, string label)
        {
            var size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z, 0.1f);
            var front = Render(bounds.center + Vector3.forward * size * 2f, Quaternion.LookRotation(Vector3.back), size);
            var top = Render(bounds.center + Vector3.up * size * 2f, Quaternion.LookRotation(Vector3.down, Vector3.forward), size);
            var tile = new Texture2D(Tile * 2, Tile, TextureFormat.RGB24, false);
            tile.SetPixels(0, 0, Tile, Tile, front.GetPixels());
            tile.SetPixels(Tile, 0, Tile, Tile, top.GetPixels());
            for (var x = 0; x < Tile * 2; x++)
            {
                tile.SetPixel(x, Tile - 1, Color.red); // a red top edge marks the tile; +Z is up in the right half
            }

            tile.Apply();
            Object.DestroyImmediate(front);
            Object.DestroyImmediate(top);
            return tile;
        }

        private static Texture2D Render(Vector3 position, Quaternion rotation, float size)
        {
            var camera = new GameObject("SurveyCamera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = size * 0.6f;
            camera.transform.SetPositionAndRotation(position, rotation);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = size * 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.75f, 0.85f, 0.95f);
            var target = new RenderTexture(Tile, Tile, 24);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(Tile, Tile, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Tile, Tile), 0, 0);
            image.Apply();
            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(camera.gameObject);
            return image;
        }

        private static Texture2D Sheet(List<Texture2D> tiles)
        {
            var columns = Mathf.Min(Columns, Mathf.Max(1, tiles.Count));
            var rows = Mathf.CeilToInt(tiles.Count / (float)columns);
            var sheet = new Texture2D(columns * Tile * 2, Mathf.Max(1, rows) * Tile, TextureFormat.RGB24, false);
            for (var i = 0; i < tiles.Count; i++)
            {
                var x = i % columns * Tile * 2;
                var y = (rows - 1 - i / columns) * Tile;
                sheet.SetPixels(x, y, Tile * 2, Tile, tiles[i].GetPixels());
                Object.DestroyImmediate(tiles[i]);
            }

            sheet.Apply();
            return sheet;
        }
    }
}

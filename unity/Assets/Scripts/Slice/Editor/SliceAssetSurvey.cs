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

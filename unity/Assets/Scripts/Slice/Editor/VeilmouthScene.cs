using System.IO;
using System.Linq;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Creates the starting point of Veilmouth.unity (VERTICAL_SLICE_IMPLEMENTATION_PLAN S0.6) and of its first
    /// interior. From then on both scenes are hand-built, so this never overwrites one that exists.
    /// <para>
    /// It writes: a flat terrain over the whole area at one level, which <see cref="VeilmouthGround"/> shapes once
    /// from the city plan C4 (kit standard §7); the water at Sea Level beneath it, which shows wherever the ground is
    /// dug below the sea; the C4 map as <see cref="LayoutZone"/> guides; and a stand-in street in Lower Veil with the Soot &amp; Bell, whose door
    /// leads to its interior and back.
    /// </para>
    /// </summary>
    public static class VeilmouthScene
    {
        public const string ScenePath = "Assets/Scenes/Slice/Veilmouth.unity";
        public const string SootAndBellScenePath = "Assets/Scenes/Slice/Interiors/SootAndBell.unity";
        public const string TerrainDataPath = "Assets/Data/Slice/Veilmouth_Terrain.asset";

        public const string SootAndBellId = "soot-and-bell";
        public const string SootAndBellStreetPortalId = "soot-and-bell.street";
        public const string SootAndBellInsidePortalId = "soot-and-bell.inside";

        /// <summary>The playable area's side, city plan C1 §3. The scene's origin is the area's centre; north is +Z.</summary>
        public const float Extent = 1000f;

        public const string MapName = "C4 Map";

        /// <summary>WorldScale.SeaLevelMeters: the Combat Grid reads every surface below it as water.</summary>
        public const float SeaLevel = 24f;

        /// <summary>Authoring default: the untouched ground lies 1.9 m above the sea, so the sea and the river are dug down.</summary>
        public const float TerrainLevel = 25.9f;

        /// <summary>A street tile lies on the ground, so its top is 0.1 m higher (kit standard §2).</summary>
        public const float StreetLevel = 26f;

        /// <summary>
        /// Where the scene starts the player: on Powder Embankment before the Black Puddle and the Soot &amp; Bell (C4, plan
        /// 620, 541; the layout's spawn).
        /// </summary>
        public static readonly Vector3 Spawn = new Vector3(120f, StreetLevel + 0.1f, -41f);

        /// <summary>The Soot &amp; Bell's south-west corner node; its door is 2 m along the south wall.</summary>
        private static readonly Vector3 SootAndBellCorner = new Vector3(100f, StreetLevel, -180f);

        /// <summary>Authoring default: room for hills of up to 70 m above the ground.</summary>
        public const float HeightBudget = 100f;

        private const int HeightmapResolution = VeilmouthGround.HeightmapResolution;
        private const string TerrainMaterialPath = "Assets/Settings/OverworldTerrain.mat";
        private const string WaterMaterialPath = "Assets/Settings/Water/OverworldWater.mat";

        /// <summary>The five canonical layers in TerrainSurfaceLayer order, read and never changed.</summary>
        private static readonly string[] TerrainLayerPaths =
        {
            "Assets/Settings/TerrainLayers/Overworld/Overworld_Grass.terrainlayer",
            "Assets/Settings/TerrainLayers/Overworld/Overworld_Sand.terrainlayer",
            "Assets/Settings/TerrainLayers/Overworld/Overworld_Soil.terrainlayer",
            "Assets/Settings/TerrainLayers/Overworld/Overworld_Cliff.terrainlayer",
            "Assets/Settings/TerrainLayers/Overworld/Overworld_Rock.terrainlayer",
        };

        private static readonly Color Land = new Color(0.45f, 0.8f, 0.35f, 0.9f);
        private static readonly Color Water = new Color(0.25f, 0.55f, 1f, 0.9f);
        private static readonly Color City = new Color(1f, 0.8f, 0.2f, 0.9f);
        private static readonly Color Ritual = new Color(1f, 0.3f, 0.3f, 0.9f);

        /// <summary>
        /// The C4 map (VEILMOUTH_CITY_PLAN.md §28.5 and its svg), from the layout: its named districts, squares and water,
        /// the forest, the village and the Ritual Site Candidates; name, centre (x, z), size (x, z), colour.
        /// </summary>
        private static (string Name, float X, float Z, float SizeX, float SizeZ, Color Color)[] Zones
        {
            get
            {
                var layout = VeilmouthLayout.Load();
                var zones = new System.Collections.Generic.List<(string, float, float, float, float, Color)>();
                void Add(string name, float x0, float y0, float x1, float y1, Color colour) =>
                    zones.Add((name, ((x0 + x1) / 2f) - 500f, 500f - ((y0 + y1) / 2f), x1 - x0, y1 - y0, colour));

                var city = layout.City;
                Add("City", city.x0, city.y0, city.x1, city.y1, City);
                var works = layout.Works;
                Add("The Reek", works.x0, works.y0, works.x1, works.y1, City);
                foreach (var d in layout.districts.Where(d => !string.IsNullOrEmpty(d.name) && d.name != "THE REEK"))
                {
                    Add(Title(d.name), d.x0, d.y0, d.x1, d.y1, City);
                }

                foreach (var s in layout.squares.Where(s => !string.IsNullOrEmpty(s.name)))
                {
                    Add(s.name, s.x0, s.y0, s.x1, s.y1, City);
                }

                foreach (var w in layout.water.Where(w => !string.IsNullOrEmpty(w.name)))
                {
                    Add(w.name, w.x0, w.y0, w.x1, Mathf.Min(w.y1, Extent), Water);
                }

                for (var i = 0; i < layout.forest.rects.Length; i++)
                {
                    var r = layout.forest.rects[i];
                    Add($"Gloomwood {i + 1}", r.x0, r.y0, r.x1, r.y1, Land);
                }

                var hamlet = layout.cottages;
                Add("Crooked Hamlet", hamlet.Min(c => c.a) - 10f, 410f, hamlet.Max(c => c.a) + 20f, 470f, Land);
                foreach (var c in layout.candidates)
                {
                    Add(c.name, c.x - (c.size / 2f), c.y - (c.size / 2f), c.x + (c.size / 2f), c.y + (c.size / 2f), Ritual);
                }

                return zones.ToArray();
            }
        }

        /// <summary>A district's name on the plan, in capitals, as a zone's: "LOWER VEIL" → "Lower Veil".</summary>
        private static string Title(string name) =>
            string.Join(" ", name.Split(' ').Select(w => w.Length > 1 ? w[0] + w.Substring(1).ToLowerInvariant() : w));

        public static string[] ZoneNames => Zones.Select(z => z.Name).ToArray();

        /// <summary>Stand-ins and the player prefab if missing, then both scenes if missing; batch runs use it.</summary>
        [MenuItem("GameDev/Slice/Create Veilmouth Scenes")]
        public static void CreateAll()
        {
            if (!AssetDatabase.IsValidFolder(KitStandIns.Folder))
            {
                KitStandIns.CreateAll();
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(SliceTestScenes.PlayerPrefabPath) == null)
            {
                SliceTestScenes.BuildPlayerPrefab();
            }

            CreateScene();
            CreateSootAndBellInterior();
        }

        public static void CreateScene()
        {
            if (Exists(ScenePath))
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            CreateTerrain();
            CreateWater();
            CreateZones();
            CreateFirstStreet();
            SliceTestScenes.AddPlayerRig(Spawn);
            SliceTestScenes.Save(scene, ScenePath);
        }

        public static void CreateSootAndBellInterior()
        {
            if (Exists(SootAndBellScenePath))
            {
                return;
            }

            SliceTestScenes.BuildRoomScene(SootAndBellScenePath, SootAndBellId, SootAndBellInsidePortalId, ScenePath,
                SootAndBellStreetPortalId);
        }

        /// <summary>A hand-built scene is never rebuilt from code.</summary>
        private static bool Exists(string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            Debug.LogWarning($"{path} exists and is hand-built from here on; it is not overwritten.");
            return true;
        }

        /// <summary>
        /// One flat level; the shape is the owner's, by hand. The unity terrain traps: the resolution is set before the
        /// size and the heights, because its setter discards both, and the data becomes an asset before its layers.
        /// No alphamap is written: the first layer, grass, covers the ground until it is painted.
        /// </summary>
        private static void CreateTerrain()
        {
            var data = new TerrainData { heightmapResolution = HeightmapResolution };
            data.size = new Vector3(Extent, HeightBudget, Extent);
            KitStandIns.EnsureFolder(Path.GetDirectoryName(TerrainDataPath).Replace('\\', '/'));
            AssetDatabase.CreateAsset(data, TerrainDataPath);
            data.terrainLayers = TerrainLayerPaths.Select(AssetDatabase.LoadAssetAtPath<TerrainLayer>).ToArray();

            var heights = new float[HeightmapResolution, HeightmapResolution];
            var level = TerrainLevel / HeightBudget;
            for (var z = 0; z < HeightmapResolution; z++)
            {
                for (var x = 0; x < HeightmapResolution; x++)
                {
                    heights[z, x] = level;
                }
            }

            data.SetHeights(0, 0, heights);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();

            var ground = Terrain.CreateTerrainGameObject(data);
            ground.name = "Ground";
            ground.layer = LayerMask.NameToLayer(KitLayers.Ground);
            ground.transform.position = new Vector3(-Extent * 0.5f, 0f, -Extent * 0.5f);
            ground.isStatic = true;
            var material = AssetDatabase.LoadAssetAtPath<Material>(TerrainMaterialPath);
            if (material != null)
            {
                ground.GetComponent<Terrain>().materialTemplate = material;
            }
        }

        /// <summary>One sheet at Sea Level for the sea and the river; no collider, because the grid reads water by height.</summary>
        private static void CreateWater()
        {
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Water";
            Object.DestroyImmediate(water.GetComponent<Collider>());
            water.layer = LayerMask.NameToLayer(KitLayers.NoGrid);
            water.transform.position = new Vector3(0f, SeaLevel, 0f);
            water.transform.localScale = new Vector3(Extent / 10f, 1f, Extent / 10f); // a plane is 10 × 10 m
            var material = AssetDatabase.LoadAssetAtPath<Material>(WaterMaterialPath);
            if (material != null)
            {
                water.GetComponent<MeshRenderer>().sharedMaterial = material;
            }
        }

        internal static void CreateZones()
        {
            var root = new GameObject(MapName);
            foreach (var zone in Zones)
            {
                var go = new GameObject(zone.Name);
                go.layer = LayerMask.NameToLayer(KitLayers.NoGrid);
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(zone.X, TerrainLevel, zone.Z);
                go.AddComponent<LayoutZone>().Configure(new Vector3(zone.SizeX, 10f, zone.SizeZ), zone.Color);
            }
        }

        /// <summary>
        /// A 32 × 16 m stand-in street south of the Soot &amp; Bell, with a fence and crates for the grid to read, and a
        /// few trunks at the forest's edge. The owner's kit replaces them in place.
        /// </summary>
        private static void CreateFirstStreet()
        {
            for (var x = 88; x < 120; x += 4)
            {
                for (var z = -196; z < -180; z += 4)
                {
                    SliceTestScenes.Place("Kit_Street_StandIn_4x4", new Vector3(x, StreetLevel, z));
                }
            }

            // Paving under the house too, so its walls stand at street level with no gap under them.
            for (var x = 100; x < 108; x += 4)
            {
                for (var z = -180; z < -172; z += 4)
                {
                    SliceTestScenes.Place("Kit_Street_StandIn_4x4", new Vector3(x, StreetLevel, z));
                }
            }

            SliceTestScenes.PlaceHouseShell(SootAndBellCorner, SootAndBellStreetPortalId, SootAndBellScenePath,
                SootAndBellInsidePortalId, false);
            SliceTestScenes.Place("Kit_Fence_StandIn_2x1", new Vector3(90f, StreetLevel, -192f));
            SliceTestScenes.Place("Kit_Fence_StandIn_2x1", new Vector3(92f, StreetLevel, -192f));
            SliceTestScenes.Place("Kit_Crate_StandIn_1x1", new Vector3(110f, StreetLevel, -184f));
            SliceTestScenes.Place("Kit_Crate_StandIn_1x1", new Vector3(111f, StreetLevel, -184f));

            foreach (var (x, z) in new[] { (60, -45), (66, -40), (71, -47), (78, -42), (84, -38), (90, -46), (97, -41), (103, -36) })
            {
                SliceTestScenes.Place("Kit_Tree_StandIn_1x4", new Vector3(x, TerrainLevel, z));
            }
        }
    }
}

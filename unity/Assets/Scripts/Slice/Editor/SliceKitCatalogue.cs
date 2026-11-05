using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameDev.Slice.Kit;
using GameDev.World.Portals;
using UnityEditor;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Wraps the vendor models of SLICE_ASSET_MAP as kit blocks (SLICE_MODULAR_KIT_STANDARD §8): a project prefab
    /// <c>Kit_&lt;Family&gt;_&lt;Part&gt;_&lt;W&gt;x&lt;H&gt;</c> under <c>Prefabs/SliceKit/&lt;Family&gt;/</c> whose root carries the
    /// block marker, the layer, and its own collider, and whose child "Model" is the vendor model with its colliders
    /// removed, scaled where the survey (Logs/SliceSurvey) found it the wrong size, and moved so that what is seen sits
    /// inside a whole-metre footprint beyond the pivot's node, its front toward +Z. A linear block (wall, fence, bars)
    /// lies along +X on the grid line through the pivot. Sizes are authoring defaults, read from the survey of 2026-11-05.
    /// Rebuilding replaces the wrappers; the vendor files are never changed.
    /// </summary>
    public static class SliceKitCatalogue
    {
        public const string Folder = "Assets/Prefabs/SliceKit";
        public const string MaterialFolder = "Assets/Materials/Slice";

        public enum Body
        {
            Box,      // one box over what is seen, clamped to the footprint
            Trunk,    // a capsule of radius 0.3 m in the middle of the cell beyond the pivot: Full Cover
            Trigger,  // a trigger over what is seen: walked through, picked from
            Keep,     // the vendor's own colliders (the bridge's walkable deck)
            Ground,   // a 0.1 m slab under the footprint whose top is the pivot's height
            None,     // NoGrid: seen, never solid
        }

        public enum Anchor
        {
            Centre,   // what is seen is centred in the footprint
            Front,    // centred in X, its front flush with the footprint's +Z edge (buildings, counters, cupboards)
            Linear,   // centred on the grid line through the pivot, along +X
            Pivot,    // the vendor pivot in the middle of the cell beyond the pivot's node (trees, bushes)
        }

        public sealed class Spec
        {
            public string Key, Family, Part, Folder, File, Layer;
            public Body Body = Body.Box;
            public Anchor Anchor = Anchor.Centre;
            public float ModelYaw;
            public float? SizeX, SizeY, SizeZ; // target size of what is seen, m; unspecified axes follow the mean ratio
            public bool Uniform = true;
            public int? Width;    // a linear block's length when it is not the model's own (the cell door)
            public float? DoorX;  // where a building's own door is, from the footprint's local west edge
            public string Material; // a project-side material for a model whose own does not render in URP
        }

        public sealed class Entry
        {
            public string Key, Path;
            public int Width, Depth; // footprint in cells along local X and Z; a linear block's depth is 0
            public float Height;
            public float DoorX;
            public GameObject Prefab => AssetDatabase.LoadAssetAtPath<GameObject>(Path);
        }

        private const string Victorian = "Assets/Modular Victorian City/Prefabs/Buldings";
        private const string Market = "Assets/Low-Poly Medieval Market";
        private const string Dungeon2 = "Assets/Multistory Dungeons 2/Prefabs/Props";
        private const string PropsLite = "Assets/LowPolyMedievalPropsLite";
        private const string DungeonLite = "Assets/LowPolyDungeonsLite";
        private const string Tripo = "Assets/TripoModels";
        private const string Trees = "Assets/Stylized Trees Pack/Assets/Prefabs";
        private const string Colonial = "Assets/3DShapes";
        private const string VictorianStreet = "Assets/Modular Victorian City/Prefabs/Basic elements/Street";

        private static Spec S(string key, string family, string part, string folder, string file, string layer,
            Body body = Body.Box, Anchor anchor = Anchor.Centre) =>
            new Spec { Key = key, Family = family, Part = part, Folder = folder, File = file, Layer = layer, Body = body, Anchor = anchor };

        /// <summary>Every wrapped model of the slice: key, family, part, source, layer, body, anchor, and size.</summary>
        public static IEnumerable<Spec> Specs()
        {
            const string st = KitLayers.Structure, pr = KitLayers.Prop, ve = KitLayers.Vegetation, ia = KitLayers.Interactable;

            // City houses (Modular Victorian City, Modular European House) and the houses outside (FantasyMedievalTown).
            foreach (var (key, file) in new[]
                     {
                         ("House_Blue", "BuildBlue"), ("House_Green", "BuildGreen"), ("House_Red", "BuildRed"),
                         ("House_Tall", "Building"), ("House_TallWide", "Building 1"), ("House_Square", "Building 1 (2)"),
                         ("House_Narrow", "Building 3"), ("House_Wide", "Building 2"), ("House_Arch", "Building 4"),
                     })
            {
                var house = S(key, "House", key.Substring(6), Victorian, file, st, anchor: Anchor.Front);
                house.ModelYaw = 180f; // the renders of 2026-11-05: their shopfronts face -Z
                if (key == "House_Square")
                {
                    house.DoorX = 4.5f; // its own door, from the render of Bell Street
                }

                yield return house;
            }

            yield return S("House_European", "House", "European", "Assets/Modular European House", "Modular European House", st, anchor: Anchor.Front);
            yield return S("House_Farm", "House", "Farm", "Assets/FantasyMedievalTown_LITE", "House_01_LITE", st, anchor: Anchor.Front);
            var church = S("Chapel", "Chapel", "Church", Colonial, "Church", st, anchor: Anchor.Front);
            church.SizeX = 12.6f; // survey: 5.05 m wide, a toy; ×2.5 makes a small chapel
            church.DoorX = 7.5f;  // its arched door stands off the middle, away from the bell tower
            yield return church;
            yield return S("Grave", "Chapel", "Grave", Colonial, "Grave", pr);
            var graveWall = S("GraveWall", "Chapel", "Wall", Colonial, "Graveyard_wall", st, anchor: Anchor.Linear);
            graveWall.SizeX = 4f;
            yield return graveWall;

            // The market and the Master of Item Creation's yard.
            yield return S("Stall_Food", "Market", "Bakery", Market, "Bakery_market_with_food", pr, anchor: Anchor.Front);
            yield return S("Stall_Empty", "Market", "Awning", Market, "BakeryMarket", pr, anchor: Anchor.Front);
            yield return S("Stall_Fish", "Market", "Fish", Market, "Fish_market_with_sections", pr, anchor: Anchor.Front);
            yield return S("Stall_Meat", "Market", "Meat", Market, "Meat_market_with_objects", pr, anchor: Anchor.Front);
            yield return S("Stall_Goods", "Market", "Goods", Market, "Vegetable_market_with_objects", pr, anchor: Anchor.Front);
            yield return S("Stall_Weapons", "Market", "Weapons", Market, "Weapon_Market_with_objects", pr, anchor: Anchor.Front);
            yield return S("Anvil", "Market", "Anvil", Market, "anvil_with_hammer", pr);
            yield return S("LeatherFrame", "Market", "LeatherFrame", Market, "leather_dressing_frame", pr);
            yield return S("ArmourStand", "Market", "ArmourStand", Market, "Armor_stand", pr);
            yield return S("BottleTable", "Market", "BottleTable", Market, "table_with_bottels", pr, anchor: Anchor.Front);
            yield return S("WeaponTable", "Market", "WeaponTable", Market, "Table_with_weapon", pr, anchor: Anchor.Front);
            yield return S("Counter", "Market", "Counter", Market, "stand_01", pr, anchor: Anchor.Front);
            yield return S("Crate", "Market", "Crate", Market, "wooden_box", pr);

            // Street props.
            yield return S("LampPost", "Street", "Lamp", VictorianStreet, "Lamp post", pr);
            yield return S("Bench", "Street", "Bench", VictorianStreet, "Wooden bench", pr, anchor: Anchor.Front);
            yield return S("TrashCan", "Street", "TrashCan", VictorianStreet, "Trash can", pr);
            yield return S("Barrel", "Props", "Barrel", PropsLite, "Barrel_01", pr);
            yield return S("Box", "Props", "Box", PropsLite, "Box_01", pr);
            yield return S("Firewood", "Props", "Firewood", PropsLite, "Firewood_01", pr);
            var fence = S("Fence", "Fence", "Field", PropsLite, "Fence_01", pr, anchor: Anchor.Linear);
            fence.SizeX = 2f;
            yield return fence;

            // Interiors: Multistory Dungeons 2 furniture and alchemy, Low Poly Dungeons Lite walls and floors.
            yield return S("Barrels", "Props", "Barrels", Dungeon2, "Barrels_Preset_01", pr);
            foreach (var (key, file) in new[] { ("CupboardBig", "Cupboard_Big_Preset_01"), ("Cupboard", "Cupboard_Preset_01"), ("Shelf", "Shelf_Preset_01") })
            {
                var cupboard = S(key, "Furniture", key, Dungeon2, file, pr, anchor: Anchor.Front);
                cupboard.ModelYaw = 90f; // survey: their open side faces -X
                yield return cupboard;
            }

            yield return S("Table", "Furniture", "Table", Dungeon2, "Table_Preset_01", pr);
            yield return S("TablePlain", "Furniture", "TablePlain", Dungeon2, "Table_01", pr);
            yield return S("Chair", "Furniture", "Chair", Dungeon2, "Chair_Wooden_01", pr);
            yield return S("Pew", "Furniture", "Pew", Dungeon2, "Bench_Wooden_01", pr);
            yield return S("Rostrum", "Furniture", "Rostrum", Dungeon2, "Rostrum_Preset_01", pr, anchor: Anchor.Front);
            yield return S("Candles", "Furniture", "Candles", Dungeon2, "Candlestick_Floor_01_Lit", pr);
            yield return S("Drawer", "Furniture", "Drawer", Dungeon2, "Drawer_Preset_01", pr, anchor: Anchor.Front);
            yield return S("Chest", "Furniture", "Chest", Dungeon2, "Treasure_Chest_01", ia);
            yield return S("Books", "Furniture", "Books", Dungeon2, "Books_Preset_01", pr);
            yield return S("Retort", "Furniture", "Retort", Dungeon2, "Retort_Preset_01", pr);
            var wall = S("RoomWall", "Room", "Wall", DungeonLite, "Wall_27", st, anchor: Anchor.Linear);
            wall.SizeX = 4f;
            yield return wall;
            yield return S("RoomFloor", "Room", "Floor", DungeonLite, "Ground_01", KitLayers.Ground, Body.Ground);

            // The owner's Tripo models (survey: each normalised to 1 m on its longest side).
            var bar = S("BarCounter", "Tavern", "BarCounter", Tripo, "ornate_wooden_bar_counter_3d_model", pr, anchor: Anchor.Front);
            bar.SizeX = 4f; // 4 × 1.04 × 1.32 m
            yield return bar;
            var cabinet = S("BarCabinet", "Tavern", "BarCabinet", Tripo, "ornate_cabinet_3d_model", pr, anchor: Anchor.Front);
            cabinet.SizeY = 2.4f; // 2.7 × 2.4 × 0.73 m
            yield return cabinet;
            var bed = S("Bed", "Furniture", "Bed", Tripo, "ornate_wooden_bed_3d_model", ia);
            bed.SizeZ = 1.9f; // 1.69 × 1.48 × 1.9 m, so it stands in a 2 × 2 m corner clear of the walls
            yield return bed;
            // The cell's front: the bars, 4 × 2.76 m, have their own doorway, and the cell door stands in it (CellFront).
            var door = S("CellDoor", "Lockup", "Door", Tripo, "rusty_iron_jail_door_3d_model", ia, anchor: Anchor.Linear);
            door.SizeX = 1.2f;
            door.SizeY = 2.13f;
            door.Uniform = false;
            door.Width = 4;
            yield return door;
            var bars = S("CellBars", "Lockup", "Bars", Tripo, "rusty_iron_gate_3d_model", st, Body.None, Anchor.Linear);
            bars.SizeX = 4f;
            yield return bars;
            var board = S("NoticeBoard", "Street", "NoticeBoard", Tripo, "ornate_notice_board_3d_model", ia, anchor: Anchor.Front);
            board.SizeY = 2.2f;
            yield return board;
            var ore = S("IronOre", "Resource", "IronOre", Tripo, "rugged_rock_formation_3d_model", ia);
            ore.SizeX = 2f; // 2 × 1.5 × 1.26 m
            yield return ore;

            // Trees, herbs, and flax (Stylized Trees Pack); a fellable tree is a resource node, the others are cover.
            foreach (var n in new[] { "Birch 001", "Birch 002", "Birch 003", "Warped 001", "Warped 002", "Warped 003" })
            {
                var part = n.Replace(" 00", "");
                yield return S("Tree_" + part, "Tree", part, Trees + "/Trees", n, ve, Body.Trunk, Anchor.Pivot);
                yield return S("Fellable_" + part, "Resource", "Fellable" + part, Trees + "/Trees", n, ia, Body.Trunk, Anchor.Pivot);
            }

            var herb = S("Herb", "Resource", "Herb", Trees + "/Bushes", "Bush 003", ia, Body.Trigger, Anchor.Pivot);
            herb.SizeY = 0.7f;
            yield return herb;
            var flax = S("Flax", "Resource", "Flax", Trees + "/Bushes", "Bush 017", ia, Body.Trigger, Anchor.Pivot);
            flax.SizeY = 0.9f;
            yield return flax;
            foreach (var n in new[] { "Bush 006", "Bush 011", "Bush 021" })
            {
                var bush = S("Bush_" + n.Substring(5), "Bush", n.Substring(5), Trees + "/Bushes", n, KitLayers.NoGrid, Body.None, Anchor.Pivot);
                bush.SizeY = 1.6f;
                yield return bush;
            }

            var bridge = S("Bridge", "Bridge", "Veil", "Assets/Julien Tonsuso", "bridge_01", st, Body.Keep);
            bridge.Material = "Assets/YughuesFreePavementsMaterials/Materials/M_YFPM_Blocs.mat"; // its own is a Substance (.sbsar), magenta without the plugin
            yield return bridge;
        }

        /// <summary>Builds every wrapper and the project's own blocks; returns them by key.</summary>
        public static Dictionary<string, Entry> BuildAll()
        {
            var entries = new Dictionary<string, Entry>();
            foreach (var spec in Specs())
            {
                var entry = Wrap(spec);
                if (entry != null)
                {
                    entries[spec.Key] = entry;
                }
            }

            CellFront(entries["CellBars"], entries["CellDoor"]);
            foreach (var entry in new[] { Paving("Street", "M_YFPM_Pavement01"), Paving("Square", "M_YFPM_Mediterranean01"), PortalLeaf(), RitualCircle() })
            {
                entries[entry.Key] = entry;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Slice kit catalogue: {entries.Count} blocks under {Folder}.\n" +
                      string.Join("\n", entries.Values.Select(e => $"  {e.Key}: {e.Width} x {e.Depth} cells, {e.Height:0.##} m, {e.Path}")));
            return entries;
        }

        private static Entry Wrap(Spec spec)
        {
            var source = Find(spec.Folder, spec.File);
            if (source == null)
            {
                Debug.LogError($"Slice kit catalogue: '{spec.File}' not found under {spec.Folder}.");
                return null;
            }

            var root = new GameObject("wrapper");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source), root.transform);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            if (spec.Folder == Tripo)
            {
                LinkTripoTextures(model, source);
            }

            if (spec.Material != null)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(spec.Material);
                foreach (var r in model.GetComponentsInChildren<Renderer>())
                {
                    r.sharedMaterials = r.sharedMaterials.Select(_ => material).ToArray();
                }
            }

            model.transform.localRotation = Quaternion.Euler(0f, spec.ModelYaw, 0f) * model.transform.localRotation;
            if (spec.Body != Body.Keep)
            {
                foreach (var c in model.GetComponentsInChildren<Collider>(true))
                {
                    Object.DestroyImmediate(c);
                }
            }

            Scale(model, spec);
            var b = Seen(model);
            var linear = spec.Anchor == Anchor.Linear;
            var width = spec.Anchor == Anchor.Pivot ? 1 : Mathf.Max(1, Mathf.CeilToInt(b.size.x - 0.05f));
            var depth = linear ? 0 : spec.Anchor == Anchor.Pivot ? 1 : Mathf.Max(1, Mathf.CeilToInt(b.size.z - 0.05f));
            if (linear)
            {
                width = spec.Width ?? Mathf.Max(1, Mathf.RoundToInt(b.size.x));
            }

            Vector3 offset;
            switch (spec.Anchor)
            {
                case Anchor.Front:
                    offset = new Vector3(width * 0.5f - b.center.x, -b.min.y, depth - b.max.z);
                    break;
                case Anchor.Linear:
                    offset = new Vector3(width * 0.5f - b.center.x, -b.min.y, -b.center.z);
                    break;
                case Anchor.Pivot:
                    offset = new Vector3(0.5f, -Mathf.Min(0f, b.min.y), 0.5f);
                    break;
                default:
                    offset = new Vector3(width * 0.5f - b.center.x, -b.min.y, depth * 0.5f - b.center.z);
                    break;
            }

            model.transform.localPosition += offset;
            b = Seen(model);

            var height = Mathf.Max(1, Mathf.RoundToInt(b.size.y));
            var name = $"Kit_{spec.Family}_{spec.Part}_{width}x{height}";
            root.name = name;
            root.AddComponent<KitBlock>();
            AddBody(root, spec.Body, b, width, depth, linear);
            SetLayer(root, spec.Layer);
            if (spec.Layer != KitLayers.Interactable)
            {
                GameObjectUtility.SetStaticEditorFlags(root, StaticEditorFlags.BatchingStatic);
            }

            var entry = Save(root, spec.Key, spec.Family, width, depth, b.size.y);
            entry.DoorX = spec.DoorX ?? width * 0.5f;
            return entry;
        }

        /// <summary>
        /// The Tripo Bridge leaves the base map empty on nine of the owner's ten models (2026-11-05), though the image
        /// lies in the model's .fbm folder. The wrapper wears a project copy of the material with the image as its base
        /// map; the owner's files are not changed.
        /// </summary>
        private static void LinkTripoTextures(GameObject model, string source)
        {
            var folder = Path.ChangeExtension(source, ".fbm").Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var image = AssetDatabase.FindAssets("t:Texture2D", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Texture2D>).FirstOrDefault();
            if (image == null)
            {
                return;
            }

            KitStandIns.EnsureFolder(MaterialFolder + "/Tripo");
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                var materials = r.sharedMaterials;
                for (var i = 0; i < materials.Length; i++)
                {
                    var m = materials[i];
                    if (m == null || !m.HasProperty("_BaseMap") || m.GetTexture("_BaseMap") != null)
                    {
                        continue;
                    }

                    var path = $"{MaterialFolder}/Tripo/{m.name}.mat";
                    var copy = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (copy == null)
                    {
                        copy = new Material(m);
                        AssetDatabase.CreateAsset(copy, path);
                    }

                    copy.SetTexture("_BaseMap", image);
                    copy.SetColor("_BaseColor", Color.white);
                    EditorUtility.SetDirty(copy);
                    materials[i] = copy;
                }

                r.sharedMaterials = materials;
            }
        }

        /// <summary>
        /// The bars get three boxes round their own doorway, measured from the render of 2026-11-05 (x 1.40–2.63 m,
        /// 2.13 m high, on 4 m bars), and the cell door stands in it as a nested block with its own box, so the cell is
        /// closed and the door alone opens.
        /// </summary>
        private static void CellFront(Entry bars, Entry door)
        {
            var root = PrefabUtility.LoadPrefabContents(bars.Path);
            void Box(float x0, float x1, float y0, float y1)
            {
                var box = root.AddComponent<BoxCollider>();
                box.center = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f);
                box.size = new Vector3(x1 - x0, y1 - y0, 0.3f);
            }

            Box(0.15f, 1.40f, 0f, 2.75f);
            Box(2.63f, 3.85f, 0f, 2.75f);
            Box(1.40f, 2.63f, 2.13f, 2.75f);
            var leaf = (GameObject)PrefabUtility.InstantiatePrefab(door.Prefab, root.transform);
            leaf.name = "Cell door";
            PrefabUtility.SaveAsPrefabAsset(root, bars.Path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void Scale(GameObject model, Spec spec)
        {
            if (spec.SizeX == null && spec.SizeY == null && spec.SizeZ == null)
            {
                return;
            }

            var size = Seen(model).size;
            var ratios = new List<float>();
            float? Ratio(float? target, float now) => target.HasValue && now > 0.001f ? target.Value / now : (float?)null;
            var rx = Ratio(spec.SizeX, size.x);
            var ry = Ratio(spec.SizeY, size.y);
            var rz = Ratio(spec.SizeZ, size.z);
            ratios.AddRange(new[] { rx, ry, rz }.Where(r => r.HasValue).Select(r => r.Value));
            var mean = ratios.Average();
            var factor = spec.Uniform ? Vector3.one * mean : new Vector3(rx ?? mean, ry ?? mean, rz ?? mean);
            model.transform.localScale = Vector3.Scale(model.transform.localScale, factor);
        }

        private static void AddBody(GameObject root, Body body, Bounds seen, int width, int depth, bool linear)
        {
            switch (body)
            {
                case Body.Trunk:
                {
                    var h = Mathf.Min(4f, seen.size.y);
                    var trunk = new GameObject("Trunk");
                    trunk.transform.SetParent(root.transform, false);
                    var capsule = trunk.AddComponent<CapsuleCollider>();
                    capsule.radius = 0.3f;
                    capsule.height = h;
                    capsule.center = new Vector3(0.5f, h * 0.5f, 0.5f);
                    return;
                }
                case Body.Ground:
                {
                    var slab = root.AddComponent<BoxCollider>();
                    slab.center = new Vector3(width * 0.5f, -0.05f, depth * 0.5f);
                    slab.size = new Vector3(width, 0.1f, depth);
                    return;
                }
                case Body.Keep:
                case Body.None:
                    return;
            }

            var min = seen.min;
            var max = seen.max;
            if (linear)
            {
                // Kit standard §2: a linear block stops half its thickness short of each node.
                var half = Mathf.Min(0.15f, (max.z - min.z) * 0.5f);
                min.x = Mathf.Max(min.x, half);
                max.x = Mathf.Min(max.x, width - half);
            }
            else
            {
                min = Vector3.Max(min, new Vector3(0f, 0f, 0f));
                max = Vector3.Min(max, new Vector3(width, max.y, depth));
            }

            var box = root.AddComponent<BoxCollider>();
            box.center = (min + max) * 0.5f;
            box.size = Vector3.Max(max - min, Vector3.one * 0.05f);
            box.isTrigger = body == Body.Trigger;
        }

        /// <summary>A 4 × 4 m paving tile whose top is the pivot's height, in one of Yughues' pavements.</summary>
        private static Entry Paving(string part, string material)
        {
            var root = KitStandIns.Street($"Kit_Paving_{part}_4x0", 4f);
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/YughuesFreePavementsMaterials/Materials/{material}.mat");
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
            {
                r.sharedMaterial = mat;
            }

            GameObjectUtility.SetStaticEditorFlags(root, StaticEditorFlags.BatchingStatic);
            return Save(root, "Paving_" + part, "Paving", 4, 4, 0.1f);
        }

        /// <summary>
        /// A door leaf on a wall's face, marked as a portal (kit standard §6): 1.2 × 2.2 m over the middle of a 3 m
        /// cell edge, 0.1 m beyond the grid line on local +Z, a trigger on the Interactable layer. An arriving player
        /// stands 1 m out on +Z. It replaces the stand-in doorway where the wall is the building's own model.
        /// </summary>
        private static Entry PortalLeaf()
        {
            var root = new GameObject("Kit_Portal_Leaf_3x2");
            root.AddComponent<KitBlock>();
            var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.name = "Leaf";
            leaf.transform.SetParent(root.transform, false);
            leaf.transform.localPosition = new Vector3(1.5f, 1.1f, 0.125f);
            leaf.transform.localScale = new Vector3(1.2f, 2.2f, 0.05f);
            leaf.GetComponent<BoxCollider>().isTrigger = true;

            // Only the trigger: a drawn leaf over the model's own door looked wrong (owner, 2026-11-05).
            Object.DestroyImmediate(leaf.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(leaf.GetComponent<MeshFilter>());
            root.AddComponent<Portal>();
            SetLayer(root, KitLayers.Interactable);
            return Save(root, "PortalLeaf", "Portal", 3, 0, 2.2f);
        }

        /// <summary>
        /// The ritual's magic circle (21 §4; the owner's form, 2026-11-05), a flat mesh of our own on the NoGrid layer:
        /// two rings of 4 m and 3 m radius and three discs, the conductors' places, at 120° on a 3.5 m radius,
        /// joined by a triangle. It lies 0.02 m above the ground and has no collider: the grid reads the place, not it.
        /// </summary>
        private static Entry RitualCircle()
        {
            KitStandIns.EnsureFolder(Folder + "/Ritual");
            var meshPath = Folder + "/Ritual/RitualCircle_Mesh.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null)
            {
                mesh = new Mesh { name = "RitualCircle" };
                AssetDatabase.CreateAsset(mesh, meshPath);
            }

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            void Ring(Vector3 c, float inner, float outer, int steps)
            {
                var start = vertices.Count;
                for (var i = 0; i <= steps; i++)
                {
                    var a = i * Mathf.PI * 2f / steps;
                    var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    vertices.Add(c + d * inner);
                    vertices.Add(c + d * outer);
                }

                for (var i = 0; i < steps; i++)
                {
                    var k = start + i * 2;
                    triangles.AddRange(new[] { k, k + 1, k + 2, k + 1, k + 3, k + 2 });
                }
            }

            void Bar(Vector3 a, Vector3 b, float halfWidth)
            {
                var side = Vector3.Cross(Vector3.up, (b - a).normalized) * halfWidth;
                var k = vertices.Count;
                vertices.AddRange(new[] { a - side, a + side, b - side, b + side });
                triangles.AddRange(new[] { k, k + 1, k + 2, k + 1, k + 3, k + 2 });
            }

            var lift = Vector3.up * 0.02f;
            Ring(lift, 3.85f, 4f, 96);
            Ring(lift, 2.9f, 3f, 96);
            var places = Enumerable.Range(0, 3).Select(i => lift + Quaternion.Euler(0f, 120f * i, 0f) * Vector3.forward * 3.5f).ToArray();
            foreach (var p in places)
            {
                Ring(p, 0f, 0.45f, 32);
            }

            for (var i = 0; i < 3; i++)
            {
                Bar(places[i], places[(i + 1) % 3], 0.05f);
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);

            var root = new GameObject("Kit_Ritual_Circle_8x0");
            root.AddComponent<KitBlock>();
            var drawn = new GameObject("Model");
            drawn.transform.SetParent(root.transform, false);
            drawn.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = drawn.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Colour("Kit_RitualCircle", new Color(0.85f, 0.12f, 0.1f), true);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            SetLayer(root, KitLayers.NoGrid);
            return Save(root, "RitualCircle", "Ritual", 8, 8, 0f);
        }

        /// <summary>A project material: URP Lit with a base colour, or URP Unlit for marks that glow faintly.</summary>
        public static Material Colour(string name, Color colour, bool unlit)
        {
            KitStandIns.EnsureFolder(MaterialFolder);
            var path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetColor("_BaseColor", colour);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Entry Save(GameObject root, string key, string family, int width, int depth, float height)
        {
            var folder = $"{Folder}/{family}";
            KitStandIns.EnsureFolder(folder);
            var path = $"{folder}/{root.name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return new Entry { Key = key, Path = path, Width = width, Depth = depth, Height = height };
        }

        public static string Find(string folder, string name)
        {
            return AssetDatabase.FindAssets(name, new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileNameWithoutExtension(p) == name && (p.EndsWith(".prefab") || p.EndsWith(".fbx")))
                .OrderBy(p => p.EndsWith(".prefab") ? 0 : 1)
                .FirstOrDefault();
        }

        /// <summary>The union of what is seen, in the wrapper root's space (the root stands at the origin).</summary>
        private static Bounds Seen(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>().Where(r => !(r is ParticleSystemRenderer)).ToArray();
            if (renderers.Length == 0)
            {
                return new Bounds(model.transform.position, Vector3.zero);
            }

            var b = renderers[0].bounds;
            foreach (var r in renderers.Skip(1))
            {
                b.Encapsulate(r.bounds);
            }

            return b;
        }

        internal static void SetLayer(GameObject root, string layer)
        {
            var index = LayerMask.NameToLayer(layer);
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = index;
            }
        }
    }
}

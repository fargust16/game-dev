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
    public static partial class SliceKitCatalogue
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
            Doorway,  // a linear wall with an opening in its middle: two side boxes and a lintel
            Leaf,     // a door leaf: one box filling its doorway's opening, 0.1 m thick on the line
        }

        public enum Anchor
        {
            Centre,   // what is seen is centred in the footprint
            Front,    // centred in X, its front flush with the footprint's +Z edge (buildings, counters, cupboards)
            Linear,   // centred on the grid line through the pivot, along +X
            Pivot,    // the vendor pivot in the middle of the cell beyond the pivot's node (trees, bushes)
            Node,     // what is seen is centred on the pivot's node, with no footprint (a corner post)
            Face,     // along +X like Linear, its back on the wall's face (WallFace out on +Z): a shelf, a painting
            Back,     // centred in X, its back on the wall's face (WallFace out on +Z): furniture against a wall
        }

        /// <summary>
        /// Where a BK room's plain wall shows its timbers, m from the wall's line (survey of 2026-11-06: 0.19 m at ×1.2,
        /// so 0.158 m a unit of BK's horizontal scale); the thinner brick is set with its face there too. Furniture
        /// against a wall stands on it.
        /// </summary>
        public const float WallFace = 0.158f * BkArchitecture;

        /// <summary>
        /// Where the plaster or the brick between the timbers shows, m from the wall's line. Measured 2026-11-06 from
        /// each wall's mesh (the largest face turned to the room): in every BK wall it lies 0.127 m behind the beams at
        /// ×1, but the plaster walls' own models put it 0.28 m further back than the brick ones at ×1.6, so a painting
        /// hung on the beams' line stood 0.43 m off the plaster (the owner's play test). Every BK wall is therefore set
        /// by this plane (<see cref="Spec.Infill"/>), which brings all their beams to WallFace, and shelves, boards and
        /// paintings hang on it.
        /// </summary>
        public const float WallInfill = WallFace - 0.127f * BkArchitecture;

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
            public float Factor = 1f; // a uniform scale before sizing (BK's architecture at x1.2, city plan §8)
            public bool Ramp;          // a stair: a smooth slope over its steps' noses, which the grid and the walker read (VI-3)
            public float BodyTop;      // when above 0, the body's colliders stop at this height (Victorian Interiors: 3.4 m, VI-3)
            public float Stretch = 1f; // a vertical scale on top of Factor (BK's rooms: walls 3.6 m, owner 2026-11-06)
            public float Lengthen = 1f; // a scale along X, after ModelYaw, on top of Factor (the balustrade: 2.94 m to 3 m, VI-5)
            public bool Infill;        // a BK wall set with its infill plane (InfillPlane) at WallInfill instead of centred
            public float Shift;        // m added to a centred linear block's depth (BK's door leaf in its aligned frame), past a front-anchored block's front (the stair), or a node block's seen piece off its node toward -X and -Z (the newel)
            public float Out;          // m a flat hung piece stands off the wall's infill (owner's play test, 2026-11-06)
            public bool LineBody;      // a linear block whose body is the full 0.3 m on its line, whatever is seen
            public string Pack;       // a pack whose built-in materials get URP copies (SliceUrpMaterials)
            public bool NoLights;     // removes the vendor's lights (BK's window carries one)
            public float OpenWidth, OpenHeight; // a doorway's opening, m, after Factor
            public (string Folder, string File, float Fx, float Fz)[] On; // a set: things standing on the base's top
            public float? InfillAt;   // where an Infill wall's infill plane is set, m from its line (WallInfill when unset)
            public float? Face;       // where a Back or Face piece's back stands, m from the wall's line (BK's when unset)
            public bool Top;          // what is seen hangs below the pivot: a ceiling, a chandelier
            public string RailPosts;  // the stair: this newel post at each end of each of its rails, as the pack's demo stands them (StairLab)
            public bool KeepY;        // the model's own floor line (y 0) stands on the floor, and what lies below it hangs below: the balustrade's fascia over the well's edge
            public Vector3? Seat;     // a linear leaf seated as the pack's demo seats it: its middle this far along from the block's middle, its foot this high, its own pivot plane this far across the line (StairLab.MeasureDoorways)
            public bool Books;        // a bookcase: rows of the pack's books stand on each of its shelves
            public string[] Goods;    // shelves of goods: the pack's files stand in rows on each shelf, as books do
        }

        public sealed class Entry
        {
            public string Key, Path;
            public int Width, Depth; // footprint in cells along local X and Z; a linear block's depth is 0
            public float Height;
            public float DoorX;
            public float Stoop; // a house door's own steps, m out from its front; the city's steps start beyond them
            public float StoopWidth; // those steps' width, m, when it is not C2's (VictorianHouses.StoopWidth)
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
        private const string Polytope = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs";
        private const string Bk = "Assets/BK_AlchemistHouse/Prefabs";
        // BK's architecture: a 2.5 m wall. The owner's play tests of 2026-11-06: 3 m rooms trap the camera under the
        // ceiling, and rooms on a 3 m module feel cramped beside the character. So the walls are ×1.6 along (a 4 m
        // module, a door 1.76 m wide) and ×1.44 up (walls 3.6 m, a door 2.85 m high). Authoring defaults.
        public const float BkArchitecture = 1.6f;
        public const float BkStretch = 0.9f;
        public const float BkVertical = BkArchitecture * BkStretch;
        public const int BkModule = 4;

        /// <summary>
        /// Furniture and loose props at game scale: ×1.2 their true size (owner's play test, 2026-11-06: beds, benches,
        /// cupboards and tables at true size read small beside the 1.8 m character from a third-person camera).
        /// Authoring default; BK's furniture so keeps the proportion its own architecture had at ×1.2.
        /// </summary>
        public const float FurnitureScale = 1.2f;
        public const float HungOut = 0.04f; // m

        /// <summary>
        /// Loose props (kit standard §4, owner 2026-11-06): the kinds that have mass and gravity, and their mass, kg, as
        /// the real thing of its size weighs: oak furniture, a full barrel, a cast-iron bench (owner's fourth play test,
        /// 2026-11-06: tables and benches flew from a touch). Authoring defaults. Every other block is still. With a
        /// walker's push of 200 N and friction 0.6, what weighs over 34 kg does not slide.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, float> LooseMass = new Dictionary<string, float>
        {
            ["Crate"] = 30f, ["Barrel"] = 140f, ["Barrels"] = 400f, ["Box"] = 15f, ["Firewood"] = 20f, ["TrashCan"] = 15f,
            ["Bench"] = 60f, ["BottleTable"] = 45f, ["WeaponTable"] = 60f, ["Table"] = 45f, ["TablePlain"] = 35f,
            ["Chair"] = 7f, ["Pew"] = 90f, ["Candles"] = 5f, ["Books"] = 4f, ["Retort"] = 3f,
            ["BkChair"] = 7f, ["BkStool"] = 4f, ["BkBasket"] = 2f, ["BkBasketB"] = 2f, ["BkLogs"] = 20f, ["BkGlobe"] = 8f,
            ["BkCauldron"] = 30f, ["BkDiningTable"] = 60f, ["BkKitchenTable"] = 45f, ["BkDesk"] = 70f, ["BkSideTable"] = 15f,
            ["BkRoundTable"] = 40f, ["BkWorkTable"] = 70f, ["BkArmsTable"] = 80f, ["BkLabBench"] = 120f, ["BkTavernTable"] = 50f,
            ["ViChair"] = 7f, ["ViArmchair"] = 14f, ["ViFootstool"] = 5f, ["ViTable"] = 50f, ["ViTableB"] = 40f, ["ViTableRound"] = 20f, // oak, 2 m long: a walker does not shove it
            ["ViDiningTable"] = 90f, ["ViKitchenTable"] = 45f, ["ViBucket"] = 3f, ["ViBox"] = 5f,
        };

        /// <summary>A set's thing, kg, by its file (authoring defaults, 2026-11-06).</summary>
        internal static float ThingMass(string file) =>
            file.StartsWith("Books") || file.StartsWith("LabSetup") || file.StartsWith("Caldron") || file.StartsWith("Globe") ? 3f
            : file.StartsWith("Axe") ? 2.5f
            : file.StartsWith("Sword") || file.StartsWith("Jar") || file.StartsWith("Oillamp") ? 1.5f
            : file.StartsWith("Book") || file.StartsWith("Basket") ? 1f
            : file.StartsWith("Bottle") ? 0.8f
            : file.StartsWith("Candle") ? 0.3f
            : file.StartsWith("Apple") ? 0.2f
            : 0.5f;

        private static Spec Game(Spec spec)
        {
            spec.Factor = FurnitureScale;
            return spec;
        }

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
            yield return Game(S("Crate", "Market", "Crate", Market, "wooden_box", pr));

            // Street props.
            yield return S("LampPost", "Street", "Lamp", VictorianStreet, "Lamp post", pr);
            yield return Game(S("Bench", "Street", "Bench", VictorianStreet, "Wooden bench", pr, anchor: Anchor.Front));
            yield return Game(S("TrashCan", "Street", "TrashCan", VictorianStreet, "Trash can", pr));
            yield return Game(S("Barrel", "Props", "Barrel", PropsLite, "Barrel_01", pr));
            yield return Game(S("Box", "Props", "Box", PropsLite, "Box_01", pr));
            yield return Game(S("Firewood", "Props", "Firewood", PropsLite, "Firewood_01", pr));
            var fence = S("Fence", "Fence", "Field", PropsLite, "Fence_01", pr, anchor: Anchor.Linear);
            fence.SizeX = 2f;
            yield return fence;

            // Interiors: Multistory Dungeons 2 furniture and alchemy, Low Poly Dungeons Lite walls and floors.
            yield return Game(S("Barrels", "Props", "Barrels", Dungeon2, "Barrels_Preset_01", pr));
            foreach (var (key, file) in new[] { ("CupboardBig", "Cupboard_Big_Preset_01"), ("Cupboard", "Cupboard_Preset_01"), ("Shelf", "Shelf_Preset_01") })
            {
                var cupboard = Game(S(key, "Furniture", key, Dungeon2, file, pr, anchor: Anchor.Front));
                cupboard.ModelYaw = 90f; // survey: their open side faces -X
                yield return cupboard;
            }

            yield return Game(S("Table", "Furniture", "Table", Dungeon2, "Table_Preset_01", pr));
            yield return Game(S("TablePlain", "Furniture", "TablePlain", Dungeon2, "Table_01", pr));
            yield return Game(S("Chair", "Furniture", "Chair", Dungeon2, "Chair_Wooden_01", pr));
            yield return Game(S("Pew", "Furniture", "Pew", Dungeon2, "Bench_Wooden_01", pr));
            yield return Game(S("Rostrum", "Furniture", "Rostrum", Dungeon2, "Rostrum_Preset_01", pr, anchor: Anchor.Back));
            yield return Game(S("Candles", "Furniture", "Candles", Dungeon2, "Candlestick_Floor_01_Lit", pr));
            yield return Game(S("Drawer", "Furniture", "Drawer", Dungeon2, "Drawer_Preset_01", pr, anchor: Anchor.Front));
            yield return Game(S("Chest", "Furniture", "Chest", Dungeon2, "Treasure_Chest_01", ia));
            yield return Game(S("Books", "Furniture", "Books", Dungeon2, "Books_Preset_01", pr));
            yield return Game(S("Retort", "Furniture", "Retort", Dungeon2, "Retort_Preset_01", pr));
            var wall = S("RoomWall", "Room", "Wall", DungeonLite, "Wall_27", st, anchor: Anchor.Linear);
            wall.SizeX = 4f;
            yield return wall;
            yield return S("RoomFloor", "Room", "Floor", DungeonLite, "Ground_01", KitLayers.Ground, Body.Ground);

            // The owner's Tripo models (survey: each normalised to 1 m on its longest side).
            var bar = S("BarCounter", "Tavern", "BarCounter", Tripo, "ornate_wooden_bar_counter_3d_model", pr, anchor: Anchor.Front);
            bar.SizeX = 4.8f; // game scale: 4.8 × 1.24 × 1.58 m (true size 4 × 1.04 × 1.32)
            yield return bar;
            var cabinet = S("BarCabinet", "Tavern", "BarCabinet", Tripo, "ornate_cabinet_3d_model", pr, anchor: Anchor.Back);
            cabinet.SizeY = 2.9f; // game scale: 3.2 × 2.9 × 0.86 m
            yield return cabinet;
            var bed = S("Bed", "Furniture", "Bed", Tripo, "ornate_wooden_bed_3d_model", ia);
            bed.SizeZ = 2.3f; // game scale (owner's play test, 2026-11-06: 1.9 m read short): 2.06 × 1.79 × 2.3 m
            yield return bed;
            // The Dwelling's bed (step 4a): the same model with its head on a wall's face, at game scale.
            var wallBed = S("WallBed", "Furniture", "WallBed", Tripo, "ornate_wooden_bed_3d_model", ia, anchor: Anchor.Back);
            wallBed.SizeZ = 2.2f;
            yield return wallBed;
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
            board.SizeY = 3f; // game scale (owner's play test, 2026-11-06: 2.2 m read very small)
            yield return board;
            var ore = S("IronOre", "Resource", "IronOre", Tripo, "rugged_rock_formation_3d_model", ia);
            ore.SizeX = 2f; // 2 × 1.5 × 1.26 m
            yield return ore;
            // The owner's garden ornaments (2026-11-08): a stone birdbath and an angel pouring water, for squares and
            // gardens (city plan §6.1). Sizes are authoring defaults: the birdbath 0.95 m high, the angel 2.2 m with its base.
            var birdbath = S("Birdbath", "Street", "Birdbath", Tripo, "ornate_stone_birdbath_3d_model", pr);
            birdbath.SizeY = 0.95f;
            yield return birdbath;
            var angel = S("AngelStatue", "Street", "AngelStatue", Tripo, "angel_statue_3d_model_1", st);
            angel.SizeY = 2.2f;
            yield return angel;
            // The owner's second set (2026-11-08): two more angels of other proportions, so the city's three differ, and a
            // great marble lion, shown at the palace as the crown's power (city plan §23). Authoring defaults: the angels
            // 2.6 and 2.4 m with their bases, the lion 3.5 m, a monument over a man's head.
            var angelB = S("AngelStatueB", "Street", "AngelStatueB", Tripo, "angel_statue_3d_model", st);
            angelB.SizeY = 2.6f;
            yield return angelB;
            var angelC = S("AngelStatueC", "Street", "AngelStatueC", Tripo, "angel_statue_3d_model_2", st);
            angelC.SizeY = 2.4f;
            yield return angelC;
            var lion = S("LionStatue", "Street", "LionStatue", Tripo, "marble_lion_statue_3d_model", st);
            lion.SizeY = 3.5f;
            yield return lion;

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

            // C1 (city plan §4, §6): the Victorian railing and column, and the plain green cover of Polytope Studio, which
            // is the common tree and shrub; the Stylized Trees Pack's coloured crowns stay for the rare resource trees.
            var railing = S("Railing", "Street", "Railing", "Assets/Modular Victorian City/Prefabs/Modules", "Fence", pr, anchor: Anchor.Linear);
            railing.SizeX = 2f;
            railing.SizeY = 1.04f; // its collider then stands 1.2 m: low cover is 0.8–1.2 m (kit standard §3); at 1.36 m it read as high cover (grid check, 2026-11-06)
            railing.Uniform = false; // 2 m long whatever its height: a uniform scale left it short of its 2 m and the line full of holes (owner's fourth play test, 2026-11-06)
            yield return railing;
            // The same railing 1 m long, for the odd metre of a line (owner's play test, 2026-11-06: gaps in railings).
            var railingShort = S("RailingShort", "Street", "Railing", "Assets/Modular Victorian City/Prefabs/Modules", "Fence", pr, anchor: Anchor.Linear);
            railingShort.SizeX = 1f;
            railingShort.SizeY = railing.SizeY;
            railingShort.Uniform = false;
            yield return railingShort;
            var column = S("Column", "Street", "Column", VictorianStreet, "Column", st);
            column.SizeY = 8f;
            yield return column;
            foreach (var (key, file) in new[] { ("Tree_Fruit", "PT_Fruit_Tree_01_green"), ("Tree_Pine", "PT_Pine_Tree_03_green") })
            {
                yield return S(key, "Tree", key.Substring(5), Polytope + "/Trees", file, ve, Body.Trunk, Anchor.Pivot);
            }

            var shrub = S("Shrub", "Bush", "Shrub", Polytope + "/Shrubs", "PT_Generic_Shrub_01_green", KitLayers.NoGrid, Body.None, Anchor.Pivot);
            shrub.SizeY = 1.2f;
            yield return shrub;
            var grass = S("Grass", "Bush", "Grass", Polytope + "/Plants", "PT_Grass_02", KitLayers.NoGrid, Body.None, Anchor.Pivot);
            grass.SizeY = 0.5f;
            yield return grass;
            var poppy = S("Poppy", "Bush", "Poppy", Polytope + "/Flowers", "PT_Poppy_02", KitLayers.NoGrid, Body.None, Anchor.Pivot);
            poppy.SizeY = 0.5f;
            yield return poppy;

            foreach (var spec in BkSpecs().Concat(ViSpecs()).Concat(GreenerySpecs()).Concat(TripoSpecs()))
            {
                yield return spec;
            }

            var bridge = S("Bridge", "Bridge", "Veil", "Assets/Julien Tonsuso", "bridge_01", st, Body.Keep);
            bridge.Material = "Assets/YughuesFreePavementsMaterials/Materials/M_YFPM_Blocs.mat"; // its own is a Substance (.sbsar), magenta without the plugin
            yield return bridge;
        }

        /// <summary>
        /// BK_AlchemistHouse for the interiors (city plan §8, survey and play tests of 2026-11-06): its architecture at ×1.6
        /// along and ×1.44 up on a 4 m module, its furniture at game scale (FurnitureScale), and sets, a table or a bench with BK's things standing on its top. A shelf,
        /// a board or a painting hangs on a wall's face (Anchor.Face) at the height its block is placed.
        /// </summary>
        private static IEnumerable<Spec> BkSpecs()
        {
            const string st = KitLayers.Structure, pr = KitLayers.Prop, ia = KitLayers.Interactable, ng = KitLayers.NoGrid;
            const string arch = Bk + "/Architecture", furn = Bk + "/Furniture", items = Bk + "/Items";

            Spec A(string key, string file, string layer, Body body, Anchor anchor, int? width = null)
            {
                var spec = S(key, "BkRoom", key.Substring(2), arch, file, layer, body, anchor);
                spec.Factor = BkArchitecture;
                spec.Stretch = BkStretch;
                spec.LineBody = true; // brick, plaster and window walls meet at a node whatever their own thickness
                spec.Pack = "BK";
                spec.NoLights = true;
                spec.Width = width;
                spec.Infill = anchor == Anchor.Linear && body != Body.Leaf;
                return spec;
            }

            Spec Plain(string key, string file, string layer, Body body) => A(key, file, layer, body, Anchor.Linear, BkModule);

            // Walls (probe render of 2026-11-06): IntWall02a/b are brick between timbers, for stone rooms; IntWall01a,
            // both doorways and the window are plaster. Each is drawn on its local +Z side only. Every one shows its infill
            // WallInfill out from its line and its beams at WallFace, whatever its thickness.
            yield return Plain("BkBrick", "IntWall02a", st, Body.Box);
            yield return Plain("BkBrickB", "IntWall02b", st, Body.Box);
            yield return A("BkWindow", "IntWall02window01", st, Body.Box, Anchor.Linear, BkModule);
            var doorway = A("BkDoorway", "IntWall02door", st, Body.Doorway, Anchor.Linear, BkModule);
            doorway.OpenWidth = 1.1f * BkArchitecture;
            doorway.OpenHeight = 1.98f * BkArchitecture * BkStretch;
            yield return doorway;
            yield return Plain("BkPlaster", "IntWall01a", st, Body.Box);
            var plasterDoorway = A("BkPlasterDoorway", "IntWall01door", st, Body.Doorway, Anchor.Linear, BkModule);
            plasterDoorway.OpenWidth = doorway.OpenWidth;
            plasterDoorway.OpenHeight = doorway.OpenHeight;
            yield return plasterDoorway;
            // A wall between two rooms gets a second face turned to the other room, seen and never solid.
            foreach (var (key, file) in new[]
                     {
                         ("BkBrickFace", "IntWall02a"), ("BkBrickBFace", "IntWall02b"), ("BkDoorwayFace", "IntWall02door"),
                         ("BkPlasterFace", "IntWall01a"), ("BkPlasterDoorwayFace", "IntWall01door"),
                     })
            {
                yield return A(key, file, ng, Body.None, Anchor.Linear, BkModule);
            }

            // A shut door is solid: the owner fell out of an interior through the street door (play test, 2026-11-06).
            var door = A("BkDoor", "Door", st, Body.Leaf, Anchor.Linear, BkModule);
            door.OpenWidth = doorway.OpenWidth;
            door.OpenHeight = doorway.OpenHeight;
            door.Shift = 0.14f; // measured: the brick doorway's frame centre once aligned; the deeper plaster frame holds it too
            yield return door;
            yield return A("BkPost", "VerticalPillar", ng, Body.None, Anchor.Node);
            foreach (var flat in new[]
                     {
                         A("BkFloor", "IntWoodFloor01", KitLayers.Ground, Body.Ground, Anchor.Centre),
                         A("BkStoneFloor", "StoneFloor01", KitLayers.Ground, Body.Ground, Anchor.Centre),
                         A("BkCeiling", "IntCeiling01", ng, Body.None, Anchor.Centre),
                     })
            {
                flat.Stretch = 1f;
                yield return flat;
            }

            Spec F(string key, string file, string layer, Body body = Body.Box, Anchor anchor = Anchor.Centre, string folder = furn)
            {
                var spec = Game(S(key, "BkFurniture", key.Substring(2), folder, file, layer, body, anchor));
                spec.Pack = "BK";
                return spec;
            }

            string It(string group) => items + "/" + group;
            // Furniture against a wall stands with its back on the wall's face (owner, 2026-11-06: no gap behind a stove).
            yield return F("BkStove", "StoveLit", pr, anchor: Anchor.Back);
            var sink = F("BkSink", "TableSink", pr, anchor: Anchor.Back);
            sink.ModelYaw = 90f; // survey: its length runs along Z
            yield return sink;
            yield return F("BkCupboard", "WoodCupboard01", pr, anchor: Anchor.Back);
            yield return F("BkCupboardGlass", "WoodCupboard02", pr, anchor: Anchor.Back);
            yield return F("BkCupboardTall", "WoodCupboard03", pr, anchor: Anchor.Back);
            yield return F("BkDrawer", "WoodDrawer01", pr, anchor: Anchor.Back);
            yield return F("BkChair", "Chair01", pr);
            yield return F("BkStool", "WoodStool", pr);
            yield return F("BkAlembic", "Alembic02", pr, anchor: Anchor.Back);
            yield return F("BkCarpet", "Carpet02", ng, Body.None);
            yield return F("BkCarpetLong", "Carpet01", ng, Body.None);
            yield return F("BkCarpetWide", "Carpet03", ng, Body.None);
            // What hangs is a two-cell block with the piece in its middle, so it centres on a 4 m wall segment.
            // A flat piece stands 0.04 m off the plaster, whose relief showed through a painting hung flush (owner's
            // play test, 2026-11-06; authoring default).
            var board = F("BkBoard", "Board", ng, Body.None, Anchor.Face);
            board.Width = 2;
            board.Out = HungOut;
            yield return board;
            var painting = F("BkPainting", "Painting01", ng, Body.None, Anchor.Face, It("Misc"));
            painting.Width = 2;
            painting.Out = HungOut;
            yield return painting;
            yield return F("BkTrunk", "Trunk01", ia, folder: It("Misc"));
            yield return F("BkCauldron", "CaldronTripod01_Bubbles", pr, folder: It("Lab"));
            yield return F("BkBasket", "Basket02", pr, folder: It("Misc"));
            yield return F("BkBasketB", "Basket03", pr, folder: It("Misc"));
            yield return F("BkLogs", "Log01", pr, folder: It("Misc"));
            yield return F("BkGlobe", "Globe", pr, folder: It("Misc"));

            Spec Set(string key, string file, string layer, Body body, Anchor anchor, params (string Folder, string File, float Fx, float Fz)[] on)
            {
                var spec = F(key, file, layer, body, anchor);
                spec.On = on;
                if (anchor == Anchor.Face)
                {
                    spec.Width = 2;
                }

                return spec;
            }

            yield return Set("BkDiningTable", "WoodTableSquare01", pr, Body.Box, Anchor.Centre,
                (It("Dishes"), "ClutterPlate01", 0.3f, 0.3f), (It("Dishes"), "ClutterPlate01", 0.7f, 0.7f),
                (It("Dishes"), "ClutterPichet01", 0.5f, 0.5f), (It("Dishes"), "ClutterGlass01", 0.7f, 0.3f),
                (It("Lights"), "Candle01b_on", 0.3f, 0.7f));
            yield return Set("BkKitchenTable", "WoodTableSquare01b", pr, Body.Box, Anchor.Back,
                (It("Ingredients"), "ClutterBowl03Ing01", 0.25f, 0.5f), (It("Dishes"), "ClutterBowl02", 0.55f, 0.4f),
                (It("Organic"), "Apple", 0.75f, 0.6f), (It("Dishes"), "ClutterPichet03", 0.88f, 0.4f));
            yield return Set("BkDesk", "WoodTableSquare01b", pr, Body.Box, Anchor.Back,
                // Books that lie: BK's row Books1 overhung the top and its single books stood on their edges and toppled (2026-11-06).
                (It("Books"), "Book09", 0.25f, 0.5f), (It("Books"), "Book10", 0.55f, 0.45f),
                (It("Lights"), "Oillamp01_on", 0.88f, 0.6f));
            yield return Set("BkCounter", "WoodTableSquare01b", pr, Body.Box, Anchor.Front,
                (It("Bottles"), "Bottle01", 0.12f, 0.5f), (It("Bottles"), "Bottle02", 0.22f, 0.6f),
                (It("Bottles"), "Bottle03", 0.32f, 0.45f), (It("Bottles"), "Flask02", 0.55f, 0.5f),
                (It("Bottles"), "Flask05", 0.68f, 0.6f), (It("Lights"), "Oillamp01_on", 0.88f, 0.5f));
            // More of BK's things for rooms that read as lived in (owner, 2026-11-06).
            yield return Set("BkDresser", "Drawer03", pr, Body.Box, Anchor.Back,
                (It("Lights"), "Candle01b_on", 0.15f, 0.5f), (It("Books"), "Book06", 0.45f, 0.5f),
                (It("Dishes"), "ClutterBowl01", 0.8f, 0.5f));
            var sideTable = Set("BkSideTable", "Table01", pr, Body.Box, Anchor.Back,
                (It("Lights"), "Oillamp01_on", 0.25f, 0.5f), (It("Books"), "Book09", 0.7f, 0.5f));
            sideTable.ModelYaw = 90f; // survey: its length runs along Z
            yield return sideTable;
            yield return Set("BkRoundTable", "Table04", pr, Body.Box, Anchor.Centre,
                (It("Dishes"), "ClutterPlate01", 0.3f, 0.35f), (It("Dishes"), "ClutterPlate01", 0.7f, 0.65f),
                (It("Dishes"), "ClutterPichet02", 0.5f, 0.5f), (It("Dishes"), "ClutterGlass02", 0.35f, 0.7f),
                (It("Books"), "Book10", 0.68f, 0.3f));
            yield return Set("BkWorkTable", "WoodTableSquare01", pr, Body.Box, Anchor.Centre,
                (It("Lab"), "Caldron01", 0.3f, 0.3f), (It("Lab"), "LabSetup02", 0.7f, 0.6f),
                (It("Ingredients"), "ClutterBowl03Ing02", 0.3f, 0.7f), (It("Ingredients"), "ClutterBowl03Ing05", 0.6f, 0.25f));
            yield return Set("BkArmsTable", "Workbench", pr, Body.Box, Anchor.Back,
                (It("Weapons"), "Sword01", 0.2f, 0.5f), (It("Weapons"), "Axe01", 0.45f, 0.5f),
                (It("Weapons"), "Dagger01", 0.65f, 0.5f), (It("Lights"), "Oillamp01_on", 0.88f, 0.5f));
            yield return Set("BkLabBench", "Workbench", pr, Body.Box, Anchor.Back,
                (It("Lab"), "LabSetup01", 0.2f, 0.5f), (It("Lab"), "LabSetup03", 0.55f, 0.5f),
                (It("Bottles"), "Flask07", 0.8f, 0.4f), (It("Bottles"), "Flask03", 0.9f, 0.65f));
            yield return Set("BkTavernTable", "table03", pr, Body.Box, Anchor.Centre,
                (It("Dishes"), "ClutterTanker01", 0.35f, 0.4f), (It("Dishes"), "ClutterTanker01", 0.65f, 0.6f),
                (It("Lights"), "Candle01b_on", 0.5f, 0.5f));
            yield return Set("BkBottleShelf", "WoodShelf01", ng, Body.None, Anchor.Face,
                (It("Bottles"), "Bottle01b", 0.15f, 0.5f), (It("Bottles"), "Bottle02b", 0.3f, 0.5f),
                (It("Bottles"), "Bottle03b", 0.45f, 0.5f), (It("Bottles"), "Bottle04", 0.6f, 0.5f),
                (It("Bottles"), "Flask04", 0.75f, 0.5f), (It("Bottles"), "Flask06", 0.9f, 0.5f));
            yield return Set("BkBookShelf", "WoodShelf01", ng, Body.None, Anchor.Face,
                (It("Books"), "Books5", 0.5f, 0.5f));
            yield return Set("BkJarShelf", "WoodShelf01", ng, Body.None, Anchor.Face,
                (It("Organic"), "Jar01", 0.2f, 0.5f), (It("Organic"), "Jar01", 0.5f, 0.5f),
                (It("Misc"), "Basket01", 0.8f, 0.5f));
            yield return Set("BkBookShelfB", "WoodShelf01", ng, Body.None, Anchor.Face,
                (It("Books"), "Books3", 0.35f, 0.5f), (It("Lights"), "Candle01", 0.85f, 0.5f));
            yield return Set("BkBookShelfC", "WoodShelf01", ng, Body.None, Anchor.Face,
                (It("Misc"), "Globe", 0.12f, 0.5f), (It("Books"), "Books4", 0.65f, 0.5f));
            yield return Set("BkDishShelf", "WoodShelf01", ng, Body.None, Anchor.Face,
                (It("Dishes"), "ClutterPlate01", 0.15f, 0.5f), (It("Dishes"), "ClutterBowl01", 0.35f, 0.5f),
                (It("Dishes"), "ClutterPichet01", 0.55f, 0.5f), (It("Dishes"), "ClutterGlass01", 0.72f, 0.5f),
                (It("Dishes"), "ClutterGlass02", 0.88f, 0.5f));
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

            if (!_measureOnly)
            {
                CellFront(entries["CellBars"], entries["CellDoor"]);
            }

            foreach (var entry in new[]
                     {
                         Paving("Street", "M_YFPM_Pavement01"), Paving("Square", "M_YFPM_Mediterranean01"), PortalLeaf("PortalLeaf", "Leaf", 3),
                         PortalLeaf("PortalLeafRoom", "LeafRoom", BkModule), RitualCircle(),
                     })
            {
                entries[entry.Key] = entry;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Slice kit catalogue: {entries.Count} blocks under {Folder}.\n" +
                      string.Join("\n", entries.Values.Select(e => $"  {e.Key}: {e.Width} x {e.Depth} cells, {e.Height:0.##} m, {e.Path}")));
            return entries;
        }

        private static bool _measureOnly;
        private static HashSet<string> _rebuild;

        /// <summary>
        /// As <see cref="Load"/>, but the named wrappers are written again in place, keeping their GUIDs, after their
        /// spec changed (a wrapper the owner changed by hand is never named here).
        /// </summary>
        public static Dictionary<string, Entry> LoadRebuilding(params string[] keys)
        {
            _rebuild = new HashSet<string>(keys);
            try
            {
                return Load();
            }
            finally
            {
                _rebuild = null;
            }
        }

        /// <summary>
        /// Every block's entry as <see cref="BuildAll"/> measures it, writing no prefab that is already on disk: the owner
        /// may have changed a wrapper by hand (the BK bottle shelf and counter, 2026-11-07), and a pass that only places
        /// blocks must keep that.
        /// </summary>
        public static Dictionary<string, Entry> Load()
        {
            _measureOnly = true;
            try
            {
                return BuildAll();
            }
            finally
            {
                _measureOnly = false;
            }
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

            if (spec.Pack != null)
            {
                SliceUrpMaterials.Apply(model, spec.Pack);
            }

            VeilmouthLook.Retint(model); // the reference look's olive leaves (reference plan R3 step 8)
            if (spec.Key.StartsWith("City"))
            {
                VeilmouthLook.RetintCity(model); // the city's muted green (city plan §25.1)
            }

            model.transform.localRotation = Quaternion.Euler(0f, spec.ModelYaw, 0f) * model.transform.localRotation;
            var things = new List<(GameObject Thing, string File)>();

            // A sized base (a Tripo set) takes its size before its things stand on it, so they keep their own.
            var sizedBase = spec.On != null && (spec.SizeX.HasValue || spec.SizeY.HasValue || spec.SizeZ.HasValue);
            if (sizedBase)
            {
                Scale(model, spec);
            }

            if (spec.On != null)
            {
                model = StandOn(root, model, spec, things);
            }

            if (spec.NoLights)
            {
                foreach (var light in model.GetComponentsInChildren<Light>(true))
                {
                    Object.DestroyImmediate(light);
                }
            }

            if (!Mathf.Approximately(spec.Stretch, 1f) || !Mathf.Approximately(spec.Lengthen, 1f))
            {
                // A vendor root is often turned (BK's are -90° about X), so the stretch goes on an upright holder.
                var holder = new GameObject("Model");
                holder.transform.SetParent(root.transform, false);
                model.name = "Base";
                model.transform.SetParent(holder.transform, true);
                holder.transform.localScale = new Vector3(spec.Lengthen, spec.Stretch, 1f);
                model = holder;
            }

            model.transform.localScale *= spec.Factor;
            if (spec.Body != Body.Keep)
            {
                foreach (var c in model.GetComponentsInChildren<Collider>(true))
                {
                    Object.DestroyImmediate(c);
                }
            }

            // BK's loose things carry their own dynamic bodies; without their colliders they fell through the shelves
            // (owner's play test, 2026-11-06). The wrapper gives loose props bodies of its own (kit standard §4).
            foreach (var joint in model.GetComponentsInChildren<Joint>(true))
            {
                Object.DestroyImmediate(joint);
            }

            foreach (var body in model.GetComponentsInChildren<Rigidbody>(true))
            {
                Object.DestroyImmediate(body);
            }

            KeepFinestLevel(model);
            if (!sizedBase)
            {
                Scale(model, spec);
            }
            var b = Seen(model);
            var linear = spec.Anchor == Anchor.Linear || spec.Anchor == Anchor.Face;
            var node = spec.Anchor == Anchor.Node;
            var width = node ? 0 : spec.Anchor == Anchor.Pivot ? 1 : Mathf.Max(1, Mathf.CeilToInt(b.size.x - 0.05f));
            var back = spec.Anchor == Anchor.Back ? spec.Face ?? WallFace : 0f;
            var depth = linear || node ? 0 : spec.Anchor == Anchor.Pivot ? 1 : Mathf.Max(1, Mathf.CeilToInt(b.size.z + back - 0.05f));
            if (linear)
            {
                width = spec.Width ?? Mathf.Max(1, Mathf.RoundToInt(b.size.x));
            }

            Vector3 offset;
            switch (spec.Anchor)
            {
                case Anchor.Front:
                    offset = new Vector3(width * 0.5f - b.center.x, -b.min.y, depth - b.max.z + spec.Shift);
                    break;
                case Anchor.Linear when spec.Seat is { } seat:
                    // By its own pivot plane, not its bounds, which take in the knob on one side only.
                    offset = new Vector3(width * 0.5f + seat.x - b.center.x, seat.y - b.min.y, seat.z - model.transform.localPosition.z);
                    break;
                case Anchor.Linear:
                    offset = new Vector3(width * 0.5f - b.center.x, -b.min.y, spec.Infill ? (spec.InfillAt ?? WallInfill) - InfillPlane(model) : spec.Shift - b.center.z);
                    break;
                case Anchor.Back:
                    offset = new Vector3(width * 0.5f - b.center.x, -b.min.y, back - b.min.z);
                    break;
                case Anchor.Pivot:
                    offset = new Vector3(0.5f, -Mathf.Min(0f, b.min.y), 0.5f);
                    break;
                case Anchor.Node:
                    // Shift sets what is seen off the node toward local -X and -Z (the newel in a balustrades' corner).
                    offset = new Vector3(-b.center.x - spec.Shift, -b.min.y, -b.center.z - spec.Shift);
                    break;
                case Anchor.Face:
                    offset = new Vector3(width * 0.5f - b.center.x, -b.min.y, (spec.Face ?? WallInfill) + spec.Out - b.min.z);
                    break;
                default:
                    offset = new Vector3(width * 0.5f - b.center.x, -b.min.y, depth * 0.5f - b.center.z);
                    break;
            }

            if (spec.Top)
            {
                offset.y = -b.max.y;
            }
            else if (spec.KeepY)
            {
                offset.y = 0f;
            }

            model.transform.localPosition += offset;
            if (TripoGlow.TryGetValue(spec.Key, out var glow))
            {
                Glow(root.transform, Seen(model), glow);
            }

            if (spec.RailPosts != null)
            {
                AddRailPosts(root, model, spec);
            }

            if (spec.Books || spec.Goods != null)
            {
                FillShelves(model, spec.Key, spec.Goods);
            }

            b = Seen(model);

            var height = Mathf.Max(1, Mathf.RoundToInt(b.size.y));
            var name = $"Kit_{spec.Family}_{spec.Part}_{width}x{height}";
            root.name = name;
            root.AddComponent<KitBlock>();
            // A set's body covers its base only: its things are bodies of their own (kit standard §4).
            var baseModel = spec.On != null ? model.transform.Find("Base").gameObject : model;
            var loose = LooseMass.TryGetValue(spec.Key, out var mass);
            if (!loose || !Loose(root, baseModel, mass, false))
            {
                loose = false;
                if (!RaisedBack(root, spec, baseModel, things, width, depth))
                {
                    AddBody(root, spec.Body, spec.On != null ? Seen(baseModel) : b, width, depth, linear, spec.OpenWidth, spec.OpenHeight, spec.LineBody);
                }

                if (spec.BodyTop > 0f)
                {
                    CapBody(root, spec.BodyTop);
                }

                if (spec.Ramp)
                {
                    AddRamp(root);
                }
            }

            // A shelf hung on a wall keeps its things still, as part of it: packed against the wall and the board, they
            // were flung out when woken (owner's fourth play test, 2026-11-06). Things on a table, a desk, a counter, or a
            // dresser are loose.
            var looseThings = spec.Body != Body.None;
            SetLayer(root, spec.Layer);
            foreach (var (thing, file) in things)
            {
                if (looseThings && Loose(thing, thing, ThingMass(file), true))
                {
                    SetLayer(thing, KitLayers.NoGrid); // a bottle on the floor never closes a cell
                }
            }

            if (loose || things.Count > 0)
            {
                // A vendor's own static flags would batch what now moves.
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (loose || t.GetComponentInParent<LooseProp>() != null)
                    {
                        GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                    }
                }
            }

            if (!loose && spec.Layer != KitLayers.Interactable)
            {
                GameObjectUtility.SetStaticEditorFlags(root, StaticEditorFlags.BatchingStatic);
            }

            var entry = Save(root, spec.Key, spec.Family, width, depth, b.size.y);
            entry.DoorX = spec.DoorX ?? width * 0.5f;
            return entry;
        }

        /// <summary>
        /// Makes <paramref name="body"/> a loose prop (kit standard §4): a convex collider on each of
        /// <paramref name="shape"/>'s meshes, so a prop tilted to a slope rests on it, a sleeping rigid body of
        /// <paramref name="mass"/> kg, and a LooseProp. False, with nothing added, where no mesh can carry a collider.
        /// </summary>
        private static bool Loose(GameObject body, GameObject shape, float mass, bool thing)
        {
            var filters = shape.GetComponentsInChildren<MeshFilter>().Where(f =>
            {
                var size = f.sharedMesh != null ? f.sharedMesh.bounds.size : Vector3.zero;
                var renderer = f.GetComponent<MeshRenderer>();
                return renderer != null && renderer.enabled && Mathf.Min(size.x, Mathf.Min(size.y, size.z)) >= 0.001f; // a flat flame makes no hull
            }).ToList();
            if (filters.Count == 0)
            {
                return false;
            }

            foreach (var filter in filters)
            {
                var hull = filter.gameObject.AddComponent<MeshCollider>();
                hull.sharedMesh = filter.sharedMesh;
                hull.convex = true;
            }

            var rigidbody = body.AddComponent<Rigidbody>();
            rigidbody.mass = mass;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rigidbody.maxDepenetrationVelocity = 1f; // a prop set slightly into its neighbour eases out, never jumps
            body.AddComponent<LooseProp>().Configure(thing);
            return true;
        }

        /// <summary>
        /// The Tripo Bridge leaves the base map empty on nine of the owner's ten models (2026-11-05), though the image
        /// lies in the model's .fbm folder. The wrapper wears a project copy of the material with the image as its base
        /// map; the owner's files are not changed.
        /// </summary>
        internal static void LinkTripoTextures(GameObject model, string source)
        {
            var folder = Path.ChangeExtension(source, ".fbm").Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            // The colour image only: the later models' folders also hold normal, metallic and roughness maps (2026-11-09),
            // and the first image found was one of those.
            var images = AssetDatabase.FindAssets("t:Texture2D", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath).ToList();
            var colour = images.FirstOrDefault(p => Path.GetFileName(p).StartsWith("tripo_image_")) ?? images.FirstOrDefault(p =>
                !new[] { "normal", "metallic", "roughness", "smoothness" }.Any(Path.GetFileName(p).ToLowerInvariant().Contains));
            var image = colour != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(colour) : null;
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
            var k = bars.Width / 4f; // the 3 m front of the C1 lockup's cells is the same bars stretched to 3 m
            void Box(float x0, float x1, float y0, float y1)
            {
                var box = root.AddComponent<BoxCollider>();
                box.center = new Vector3((x0 + x1) * 0.5f * k, (y0 + y1) * 0.5f, 0f);
                box.size = new Vector3((x1 - x0) * k, y1 - y0, 0.3f);
            }

            Box(0.15f / k, 1.40f, 0f, 2.75f);
            Box(2.63f, 4f - 0.15f / k, 0f, 2.75f);
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

        /// <summary>
        /// A wall model's infill: the depth of the largest area of faces turned to +Z, in 0.01 m bins (the plaster or the
        /// brick between its timbers, as against its beams and posts).
        /// </summary>
        private static float InfillPlane(GameObject model)
        {
            var bins = new Dictionary<int, float>();
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                var v = mesh.vertices;
                var n = mesh.normals;
                var t = mesh.triangles;
                var m = filter.transform.localToWorldMatrix;
                for (var i = 0; i < t.Length; i += 3)
                {
                    if (n.Length == 0 || m.MultiplyVector(n[t[i]]).normalized.z < 0.9f)
                    {
                        continue;
                    }

                    var a = m.MultiplyPoint3x4(v[t[i]]);
                    var b = m.MultiplyPoint3x4(v[t[i + 1]]);
                    var c = m.MultiplyPoint3x4(v[t[i + 2]]);
                    var bin = Mathf.RoundToInt((a.z + b.z + c.z) / 3f * 100f);
                    bins[bin] = (bins.TryGetValue(bin, out var area) ? area : 0f) + Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                }
            }

            if (bins.Count == 0)
            {
                throw new System.InvalidOperationException($"{model.name}: no face turned to +Z to set the wall by");
            }

            return bins.OrderByDescending(kv => kv.Value).First().Key / 100f;
        }

        private static void AddBody(GameObject root, Body body, Bounds seen, int width, int depth, bool linear,
            float openWidth = 0f, float openHeight = 0f, bool lineBody = false)
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
                case Body.Leaf:
                    Box(root, new Vector3(width * 0.5f - openWidth * 0.5f, 0f, -0.05f),
                        new Vector3(width * 0.5f + openWidth * 0.5f, openHeight, 0.05f));
                    return;
            }

            var min = seen.min;
            var max = seen.max;
            if (linear)
            {
                // Kit standard §2: a wall's body is at most 0.3 m thick, centred on its line, however thick it is seen
                // (BK's plaster wall is 0.377 m at ×1.2, measured), and it stops half that thickness short of each
                // node, so two walls meeting at a corner only touch.
                var middle = lineBody ? 0f : (min.z + max.z) * 0.5f;
                var half = lineBody ? 0.15f : Mathf.Min(0.3f, max.z - min.z) * 0.5f;
                min.z = middle - half;
                max.z = middle + half;
                // A wall set off its line (Victorian Interiors, by its face) stops as far short of each node as it
                // reaches from the line, so a wall meeting it at a corner still only touches.
                var reach = lineBody ? 0.15f : Mathf.Max(Mathf.Abs(min.z), Mathf.Abs(max.z));
                min.x = Mathf.Max(min.x, reach);
                max.x = Mathf.Min(max.x, width - reach);
            }
            else if (width > 0 || depth > 0) // a node block has no footprint to clamp to: its body is what is seen (the newel)
            {
                min = Vector3.Max(min, new Vector3(0f, 0f, 0f));
                max = Vector3.Min(max, new Vector3(width, max.y, depth));
            }

            if (body == Body.Doorway)
            {
                // Two side boxes and a lintel around an opening in the middle of the wall (the BK doorway's own).
                var left = width * 0.5f - openWidth * 0.5f;
                var right = width * 0.5f + openWidth * 0.5f;
                Box(root, new Vector3(min.x, min.y, min.z), new Vector3(left, max.y, max.z));
                Box(root, new Vector3(right, min.y, min.z), new Vector3(max.x, max.y, max.z));
                Box(root, new Vector3(left, openHeight, min.z), new Vector3(right, max.y, max.z));
                return;
            }

            var box = root.AddComponent<BoxCollider>();
            box.center = (min + max) * 0.5f;
            box.size = Vector3.Max(max - min, Vector3.one * 0.05f);
            box.isTrigger = body == Body.Trigger;
        }

        /// <summary>
        /// A set whose base rises behind its things, as a writing desk's back does over its top (Victorian Interiors'
        /// desk: top 1.03 m, back 1.44 m, measured 2026-11-07): one box up to the top under the things, and one box for the
        /// raised back behind them. One box over all would hold the things inside it and fling them when woken.
        /// False where the set's top is level with its things.
        /// </summary>
        private static bool RaisedBack(GameObject root, Spec spec, GameObject baseModel, List<(GameObject Thing, string File)> things, int width, int depth)
        {
            if (spec.On == null || spec.Body != Body.Box || things.Count == 0)
            {
                return false;
            }

            var seen = Seen(baseModel);
            var placed = things.Select(t => Seen(t.Thing)).ToList();
            var top = placed.Min(t => t.min.y);
            var front = placed.Min(t => t.min.z) - 0.02f;
            if (top > seen.max.y - 0.05f)
            {
                return false;
            }

            var min = Vector3.Max(seen.min, Vector3.zero);
            var max = Vector3.Min(seen.max, new Vector3(width, seen.max.y, depth));
            Box(root, min, new Vector3(max.x, top, max.z));
            if (front - min.z > 0.05f)
            {
                Box(root, new Vector3(min.x, top, min.z), new Vector3(max.x, max.y, front));
            }

            return true;
        }

        /// <summary>
        /// A ray down onto a stair often strikes a step's nose and reads a wall (normals of 64°-67°, measured), so the Combat
        /// Grid cut the stair in two. A 0.1 m slab lies on the noses, from the floor to the top tread's front edge, about
        /// 28.5° (measured) and walkable; the feet stand at most one riser (0.19 m) over a tread. It replaces the steps' own collider; the
        /// rails keep theirs.
        /// </summary>
        private static void AddRamp(GameObject root)
        {
            var steps = root.GetComponentsInChildren<MeshCollider>(true)
                .Where(m => m.sharedMesh != null && !m.name.Contains("Banister"))
                .OrderByDescending(m => m.sharedMesh.bounds.size.sqrMagnitude)
                .FirstOrDefault();
            if (steps == null)
            {
                Debug.LogWarning($"{root.name}: a ramp asked for, but no steps' collider");
                return;
            }

            // The steps' vertices in the block's frame. The slope is the line under which every vertex lies, through the
            // front edge of the top tread: it touches every nose, which stand in one line.
            var points = steps.sharedMesh.vertices.Select(p => root.transform.InverseTransformPoint(steps.transform.TransformPoint(p))).ToArray();
            var minX = points.Min(p => p.x);
            var maxX = points.Max(p => p.x);
            var top = points.Max(p => p.y);
            var floor = points.Min(p => p.y);
            var edge = points.Where(p => p.y >= top - 0.01f).Min(p => p.z);
            var slope = points.Where(p => p.z < edge - 0.05f).Min(p => (top - p.y) / (edge - p.z));
            var head = new Vector3((minX + maxX) * 0.5f, top, edge);
            var foot = new Vector3(head.x, floor, edge - ((top - floor) / slope));
            var along = head - foot;
            Debug.Log($"{root.name}: ramp from {foot} to {head}, {Mathf.Atan(slope) * Mathf.Rad2Deg:F1}°");
            var rotation = Quaternion.LookRotation(along.normalized, Vector3.up);
            var ramp = new GameObject("Ramp");
            ramp.transform.SetParent(root.transform, false);
            ramp.layer = root.layer;
            ramp.transform.localRotation = rotation;
            ramp.transform.localPosition = ((foot + head) * 0.5f) - (rotation * Vector3.up * 0.05f);
            var box = ramp.AddComponent<BoxCollider>();
            box.size = new Vector3(maxX - minX, 0.1f, along.magnitude);
            Object.DestroyImmediate(steps); // under the slope it would read as a second floor 0.1 m lower
        }

        /// <summary>Cuts the block's own boxes at <paramref name="top"/>; a box wholly above it would be none, and goes.</summary>
        private static void CapBody(GameObject root, float top)
        {
            foreach (var box in root.GetComponents<BoxCollider>())
            {
                var min = box.center - box.size * 0.5f;
                var max = box.center + box.size * 0.5f;
                if (max.y <= top)
                {
                    continue;
                }

                if (min.y >= top - 0.05f)
                {
                    Object.DestroyImmediate(box);
                    continue;
                }

                max.y = top;
                box.center = (min + max) * 0.5f;
                box.size = max - min;
            }
        }

        private static void Box(GameObject root, Vector3 min, Vector3 max)
        {
            var box = root.AddComponent<BoxCollider>();
            box.center = (min + max) * 0.5f;
            box.size = Vector3.Max(max - min, Vector3.one * 0.05f);
        }

        /// <summary>
        /// A set (city plan §8): the base model and BK's things standing on its top, each centred at a fraction of the
        /// top's extent, under one "Model". Things keep their true size. Each stands on the base's surface found under
        /// its place by a ray on the base's meshes, not on its bounds' top, which a shelf's back rail raised above its
        /// board (owner's play test, 2026-11-06: bottles floated over the shelf). BK's volumetric light script, which
        /// needs the built-in pipeline, is removed from its lamps and candles.
        /// </summary>
        private static GameObject StandOn(GameObject root, GameObject baseModel, Spec spec, List<(GameObject, string)> things)
        {
            var holder = new GameObject("Model");
            holder.transform.SetParent(root.transform, false);
            baseModel.name = "Base";
            baseModel.transform.SetParent(holder.transform, true);
            var top = Seen(baseModel);
            foreach (var (folder, file, fx, fz) in spec.On)
            {
                var source = Find(folder, file);
                if (source == null)
                {
                    Debug.LogError($"Slice kit catalogue: '{file}' not found under {folder}.");
                    continue;
                }

                var thing = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source), holder.transform);
                // Some of BK's things carry a script the project lacks; a prefab cannot be saved with it.
                PrefabUtility.UnpackPrefabInstance(thing, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (var t in thing.GetComponentsInChildren<Transform>(true))
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                }

                foreach (var light in thing.GetComponentsInChildren<MonoBehaviour>(true).Where(m => m.GetType().Name == "VolumetricLight").ToList())
                {
                    Object.DestroyImmediate(light);
                }

                if (folder == Tripo)
                {
                    // The owner's Tripo things (§10 of the furnishing rules): their base maps, laid down if flat, sized.
                    LinkTripoTextures(thing, source);
                    var (longest, pitch) = TripoThings.TryGetValue(file, out var t) ? t : (0.3f, 0f);
                    thing.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f) * thing.transform.localRotation;
                    var size = Seen(thing).size;
                    thing.transform.localScale *= longest / UnityEngine.Mathf.Max(size.x, size.y, size.z, 0.001f);
                }
                else
                {
                    SliceUrpMaterials.Apply(thing, spec.Pack ?? "BK");
                }

                var seen = Seen(thing);
                var x = top.min.x + fx * top.size.x;
                var z = top.min.z + fz * top.size.z;
                var at = new Vector3(x, Surface(baseModel, x, z, top.max.y), z);
                thing.transform.position += at - new Vector3(seen.center.x, seen.min.y, seen.center.z);
                if (folder == Tripo && TripoGlow.TryGetValue(file, out var glow))
                {
                    Glow(thing.transform, Seen(thing), glow);
                }

                things.Add((thing, file));
            }

            return holder;
        }

        /// <summary>The highest point of a model's meshes under (x, z), or <paramref name="fallback"/> where none is.</summary>
        private static float Surface(GameObject model, float x, float z, float fallback)
        {
            var best = float.NegativeInfinity;
            var ray = new Ray(new Vector3(x, fallback + 1f, z), Vector3.down);
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
            {
                // Only what is seen, as the loose prop's hulls are: a hidden mesh held a book 0.15 m over a desk (2026-11-06).
                if (filter.sharedMesh == null || !filter.TryGetComponent<MeshRenderer>(out var seen) || !seen.enabled)
                {
                    continue;
                }

                var probe = filter.gameObject.AddComponent<MeshCollider>();
                probe.sharedMesh = filter.sharedMesh;
                if (probe.Raycast(ray, out var hit, fallback + 100f))
                {
                    best = Mathf.Max(best, hit.point.y);
                }

                Object.DestroyImmediate(probe);
            }

            return float.IsNegativeInfinity(best) ? fallback : best;
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
        private static Entry PortalLeaf(string key, string part, int width)
        {
            var root = new GameObject($"Kit_Portal_{part}_{width}x2");
            root.AddComponent<KitBlock>();
            var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.name = "Leaf";
            leaf.transform.SetParent(root.transform, false);
            leaf.transform.localPosition = new Vector3(width * 0.5f, 1.1f, 0.125f);
            leaf.transform.localScale = new Vector3(1.2f, 2.2f, 0.05f);
            leaf.GetComponent<BoxCollider>().isTrigger = true;

            // Only the trigger: a drawn leaf over the model's own door looked wrong (owner, 2026-11-05).
            Object.DestroyImmediate(leaf.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(leaf.GetComponent<MeshFilter>());
            root.AddComponent<Portal>();
            SetLayer(root, KitLayers.Interactable);
            return Save(root, key, "Portal", width, 0, 2.2f);
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
            var rebuilding = _rebuild != null && _rebuild.Contains(key);
            if (rebuilding && !File.Exists(path))
            {
                Resized(folder, root.name, path);
            }

            if (!_measureOnly || !File.Exists(path) || rebuilding)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }

            Object.DestroyImmediate(root);
            return new Entry { Key = key, Path = path, Width = width, Depth = depth, Height = height };
        }

        /// <summary>
        /// A rebuilt wrapper whose footprint changed has a new name: the old one, the same name with another size, is moved
        /// to it, so its GUID and every scene's instance follow, and no stale wrapper is left beside the new (the piano,
        /// 2026-11-09).
        /// </summary>
        private static void Resized(string folder, string name, string path)
        {
            var stem = System.Text.RegularExpressions.Regex.Replace(name, @"_\d+x\d+$", "_");
            var old = Directory.GetFiles(folder, stem + "*.prefab")
                .Select(f => f.Replace('\\', '/'))
                .FirstOrDefault(f => System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileNameWithoutExtension(f), "^" + System.Text.RegularExpressions.Regex.Escape(stem) + @"\d+x\d+$"));
            if (old == null)
            {
                return;
            }

            var error = AssetDatabase.MoveAsset(old, path);
            Debug.Log(string.IsNullOrEmpty(error) ? $"Slice kit catalogue: {old} resized to {path}." : $"Slice kit catalogue: {old} not moved: {error}");
        }

        public static string Find(string folder, string name)
        {
            return AssetDatabase.FindAssets(name, new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileNameWithoutExtension(p) == name && (p.EndsWith(".prefab") || p.EndsWith(".fbx")))
                .OrderBy(p => p.EndsWith(".prefab") ? 0 : 1)
                .FirstOrDefault();
        }

        /// <summary>
        /// The pack's newel post at each end of each of a stair's rails, which the stair's model leaves open (owner, VI-8
        /// follow-up), where the pack's demo stands them (StairLab): on each rail's centre line, measured from the rails'
        /// own vertices, the foot post 0.018 m inside the rails' foot and the head post 0.054 m past their head, on the
        /// floor above. Children of the block, beside its model, so the stair's width scale does not squash them; no body.
        /// </summary>
        private static void AddRailPosts(GameObject root, GameObject model, Spec spec)
        {
            const float footInside = 0.018f, headPast = 0.054f, band = 0.25f;
            var source = Find(spec.Folder, spec.RailPosts);
            var points = model.GetComponentsInChildren<MeshFilter>(true)
                .Where(f => f.sharedMesh != null && f.name.Contains("Banister"))
                .SelectMany(f => f.sharedMesh.vertices.Select(v => root.transform.InverseTransformPoint(f.transform.TransformPoint(v))))
                .ToList();
            if (source == null || points.Count == 0)
            {
                Debug.LogError($"Slice kit catalogue: {spec.Key} has no rail meshes or no '{spec.RailPosts}' for its rail posts.");
                return;
            }

            // The stair climbs toward its front, local +Z: the rails' foot is their least Z, their head their greatest.
            var foot = points.Min(p => p.z);
            var head = points.Max(p => p.z);
            var mid = (points.Min(p => p.x) + points.Max(p => p.x)) * 0.5f;
            foreach (var (end, z, y) in new[] { (foot, foot + footInside, 0f), (head, head + headPast, ViStorey) })
            {
                var near = points.Where(p => Mathf.Abs(p.z - end) < band).ToList();
                foreach (var side in new[] { near.Where(p => p.x < mid).ToList(), near.Where(p => p.x >= mid).ToList() })
                {
                    if (side.Count == 0)
                    {
                        continue;
                    }

                    var post = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source), root.transform);
                    post.name = "Rail post";
                    KeepFinestLevel(post);
                    foreach (var c in post.GetComponentsInChildren<Collider>(true))
                    {
                        Object.DestroyImmediate(c);
                    }

                    post.transform.localPosition = new Vector3((side.Min(p => p.x) + side.Max(p => p.x)) * 0.5f, y, z);
                }
            }
        }

        /// <summary>
        /// Victorian Interiors' walls and furniture carry LOD groups whose coarser meshes differ in their panels and
        /// mouldings, so a wall changed its look as the camera moved (owner's play test VI-5, remark 3). Rooms are seen
        /// from close by, so a wrapper keeps the finest level only and drops the group.
        /// </summary>
        private static void KeepFinestLevel(GameObject model)
        {
            foreach (var group in model.GetComponentsInChildren<LODGroup>(true))
            {
                var lods = group.GetLODs();
                var finest = lods.Length > 0 ? new HashSet<Renderer>(lods[0].renderers.Where(r => r != null)) : new HashSet<Renderer>();
                foreach (var r in lods.Skip(1).SelectMany(l => l.renderers).Where(r => r != null && !finest.Contains(r)).Distinct().ToList())
                {
                    var filter = r.GetComponent<MeshFilter>();
                    Object.DestroyImmediate(r);
                    if (filter != null)
                    {
                        Object.DestroyImmediate(filter);
                    }
                }

                Object.DestroyImmediate(group);
            }
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

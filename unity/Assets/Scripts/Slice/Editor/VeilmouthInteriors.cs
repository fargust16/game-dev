using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameDev.Slice.Kit;
using GameDev.World.Interiors;
using GameDev.World.Portals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The seven interiors of the city plan C1 §8, built once from BK_AlchemistHouse (owner, 2026-11-05): rooms that read
    /// as rooms on BK's 3 m module, with walls, doorways, windows, corner posts, floors, ceilings 3 m up, the street door
    /// with its portal, furniture at true size, a light in each room, and the people at their posts. Room sizes are §8's
    /// rounded to the module (authoring defaults). It replaces C0's stone boxes, and from then on each scene is the
    /// owner's: it refuses an interior that already holds its root, and <see cref="BuildReplacing"/> is for the agent's
    /// own batch iterations only. Plan coordinates: x east, z north, metres from the south-west corner; the street door
    /// is on the south wall, so an arriving player faces north into the room.
    /// </summary>
    public static class VeilmouthInteriors
    {
        public const string RootName = "Interior C1";
        public const string RoomMarker = "Room - ";
        public const int Module = 3;
        // The walls stand 3.6 m (SliceKitCatalogue.BkStretch), but their single-sided frame tops out a little lower: a
        // ceiling 0.97 of the way up closes the slit of sky seen above an outer wall (render of 2026-11-06).
        public const float CeilingHeight = 2.9f * SliceKitCatalogue.BkStretch;

        /// <summary>
        /// The layer of a ceiling's collider: the camera's spring arm meets it (SliceCharacterLook.CameraCollision), the
        /// Combat Grid does not read it as a floor (CombatGridReader.Mask leaves Ignore Raycast out).
        /// </summary>
        public const string CeilingLayer = "Ignore Raycast";

        public static readonly (string Id, string Path)[] All =
        {
            (VeilmouthScene.SootAndBellId, VeilmouthScene.SootAndBellScenePath),
            ("harrow-apothecary", VeilmouthDressing.HarrowScenePath),
            ("faction-house", VeilmouthDressing.FactionHouseScenePath),
            ("wardens-lockup", VeilmouthDressing.LockupScenePath),
            ("missing-room", VeilmouthDressing.MissingRoomScenePath),
            ("soot-stair-cellar", VeilmouthDressing.CellarScenePath),
            ("old-chapel", VeilmouthDressing.ChapelScenePath),
        };

        private static Dictionary<string, SliceKitCatalogue.Entry> _kit;
        private static Transform _parent;

        [MenuItem("GameDev/Slice/Build Veilmouth Interiors from C1 (once)")]
        public static void Build() => Run(false);

        /// <summary>Batch only, for the agent's iterations: replaces this pass's own interiors.</summary>
        public static void BuildReplacing() => Run(true);

        public static void BuildReplacingAndRender()
        {
            Run(true);
            VeilmouthRenders.RenderInteriors();
        }

        private static void Run(bool replace)
        {
            _kit = SliceKitCatalogue.BuildAll();
            foreach (var (id, path) in All)
            {
                if (File.Exists(path) && !replace)
                {
                    var existing = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    if (existing.GetRootGameObjects().Any(g => g.name == RootName))
                    {
                        Debug.LogWarning($"{path} already holds {RootName}; it is the owner's now and is not rebuilt.");
                        continue;
                    }
                }

                BuildOne(id, path);
            }
        }

        // ------------------------------------------------------------------------------------------------ plans

        private enum Opening
        {
            Wall,
            Door,    // an open doorway between rooms
            Street,  // the doorway to the street: BK's door leaf and the portal
            Closed,  // a doorway with BK's door leaf shut (a back door, a door to nowhere the player goes)
            Window,
            Cell,    // a cell's front: the bars with their door
        }

        private sealed class Plan
        {
            public readonly List<(string Name, int X, int Z, int W, int D, bool Stone)> Rooms = new();
            public readonly Dictionary<(int X, int Z, bool AlongZ), Opening> Openings = new();
            public (int X, int Z) Street;

            /// <summary>A room; a stone room has a stone floor and brick walls.</summary>
            public Plan Room(string name, int x, int z, int w, int d, bool stone = false)
            {
                Rooms.Add((name, x, z, w, d, stone));
                return this;
            }

            /// <summary>The 3 m wall segment from node (x, z) along +X, or along +Z.</summary>
            public Plan Open(Opening kind, int x, int z, bool alongZ = false)
            {
                Openings[(x, z, alongZ)] = kind;
                if (kind == Opening.Street)
                {
                    Street = (x, z);
                }

                return this;
            }
        }

        private static void BuildOne(string id, string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var root = new GameObject(RootName).transform;
            var plan = PlanFor(id);

            _parent = Group(root, "Rooms");
            Rooms(plan, id);
            _parent = Group(root, "Furniture");
            VeilmouthDressing.Use(_kit, Group(root, "People"));
            Furnish(id);

            new GameObject("Interior").AddComponent<InteriorRuntime>().Configure(id);
            SliceTestScenes.AddPlayerRig(new Vector3(plan.Street.X + 1.5f, 0.1f, plan.Street.Z + 1.6f));
            SliceTestScenes.Save(scene, path);
            Report(path);
        }

        private static Plan PlanFor(string id)
        {
            switch (id)
            {
                case VeilmouthScene.SootAndBellId:
                    return new Plan()
                        .Room("Taproom", 0, 0, 15, 9).Room("Kitchen", 0, 9, 6, 6, true).Room("Passage", 6, 9, 3, 6)
                        .Room("Let room", 9, 9, 6, 6)
                        .Open(Opening.Street, 6, 0).Open(Opening.Window, 0, 0).Open(Opening.Window, 12, 0)
                        .Open(Opening.Window, 15, 3, true).Open(Opening.Door, 3, 9).Open(Opening.Door, 6, 9)
                        .Open(Opening.Door, 6, 12, true).Open(Opening.Door, 9, 12, true).Open(Opening.Closed, 6, 15)
                        .Open(Opening.Window, 12, 15);
                case "harrow-apothecary":
                    return new Plan()
                        .Room("Shop", 0, 0, 9, 6).Room("Laboratory", 0, 6, 9, 6, true).Room("Store room", 9, 6, 3, 6, true)
                        .Open(Opening.Street, 3, 0).Open(Opening.Window, 0, 0).Open(Opening.Window, 6, 0)
                        .Open(Opening.Door, 6, 6).Open(Opening.Door, 9, 9, true).Open(Opening.Window, 0, 9, true);
                case "faction-house":
                    return new Plan()
                        .Room("Hall", 0, 0, 12, 9).Room("Office", 0, 9, 6, 6).Room("Archive", 6, 9, 6, 6)
                        .Open(Opening.Street, 6, 0).Open(Opening.Window, 0, 0).Open(Opening.Window, 9, 0)
                        .Open(Opening.Window, 0, 3, true).Open(Opening.Window, 12, 3, true)
                        .Open(Opening.Door, 0, 9).Open(Opening.Door, 9, 9).Open(Opening.Window, 0, 12, true);
                case "wardens-lockup":
                    return new Plan()
                        .Room("Front desk", 0, 0, 9, 6).Room("Office", 9, 0, 6, 6).Room("Corridor", 0, 6, 9, 3, true)
                        .Room("Guard room", 9, 6, 6, 6).Room("Cell 1", 0, 9, 3, 3, true).Room("Cell 2", 3, 9, 3, 3, true)
                        .Room("Cell 3", 6, 9, 3, 3, true)
                        .Open(Opening.Street, 3, 0).Open(Opening.Window, 6, 0).Open(Opening.Window, 12, 0)
                        .Open(Opening.Door, 9, 3, true).Open(Opening.Door, 6, 6).Open(Opening.Door, 9, 6, true)
                        .Open(Opening.Cell, 0, 9).Open(Opening.Cell, 3, 9).Open(Opening.Cell, 6, 9);
                case "missing-room":
                    return new Plan()
                        .Room("Landing", 0, 0, 6, 3).Room("Room", 0, 3, 6, 6)
                        .Open(Opening.Street, 3, 0).Open(Opening.Closed, 0, 0).Open(Opening.Door, 0, 3)
                        .Open(Opening.Window, 3, 9);
                case "soot-stair-cellar":
                    return new Plan()
                        .Room("West vault", 0, 0, 6, 6, true).Room("Circle vault", 6, 0, 9, 9, true)
                        .Room("East vault", 15, 0, 6, 6, true)
                        .Open(Opening.Street, 0, 0).Open(Opening.Door, 6, 3, true).Open(Opening.Door, 15, 3, true);
                case "old-chapel":
                    return new Plan()
                        .Room("Nave", 0, 0, 9, 15, true).Room("Vestry", 9, 9, 3, 6, true)
                        .Open(Opening.Street, 3, 0).Open(Opening.Window, 0, 3, true).Open(Opening.Window, 0, 9, true)
                        .Open(Opening.Window, 9, 3, true).Open(Opening.Door, 9, 12, true).Open(Opening.Window, 3, 15);
                default:
                    throw new ArgumentException(id);
            }
        }

        // ------------------------------------------------------------------------------------------------ rooms

        /// <summary>Floors, ceilings, a marker and a light per room; one wall per 3 m segment; a post at every node.</summary>
        private static void Rooms(Plan plan, string id)
        {
            var segments = new Dictionary<(int X, int Z, bool AlongZ), int>();
            void Count((int, int, bool) s) => segments[s] = segments.TryGetValue(s, out var n) ? n + 1 : 1;

            foreach (var (name, x, z, w, d, stone) in plan.Rooms)
            {
                for (var cx = x; cx < x + w; cx += Module)
                {
                    for (var cz = z; cz < z + d; cz += Module)
                    {
                        Put(stone ? "BkStoneFloor" : "BkFloor", cx, cz);
                        Put("BkCeiling", cx, cz, 0, CeilingHeight);
                    }

                    Count((cx, z, false));
                    Count((cx, z + d, false));
                }

                for (var cz = z; cz < z + d; cz += Module)
                {
                    Count((x, cz, true));
                    Count((x + w, cz, true));
                }

                var marker = new GameObject(RoomMarker + name).transform;
                marker.SetParent(_parent, false);
                marker.position = new Vector3(x + w * 0.5f, 0f, z + d * 0.5f);
                marker.localScale = new Vector3(w, CeilingHeight, d);

                // The camera stays under the ceiling (owner's play test, 2026-11-06). Not a kit block: a box the arm meets.
                var ceiling = new GameObject("Ceiling collider - " + name);
                ceiling.transform.SetParent(_parent, false);
                ceiling.layer = LayerMask.NameToLayer(CeilingLayer);
                var box = ceiling.AddComponent<BoxCollider>();
                box.center = new Vector3(x + w * 0.5f, CeilingHeight + 0.15f, z + d * 0.5f);
                box.size = new Vector3(w, 0.3f, d);

                var light = new GameObject("Light - " + name).AddComponent<Light>();
                light.transform.SetParent(_parent, false);
                light.transform.position = new Vector3(x + w * 0.5f, CeilingHeight - 0.4f, z + d * 0.5f);
                light.type = LightType.Point;
                light.color = new Color(1f, 0.82f, 0.6f);
                light.range = Mathf.Max(w, d) * 1.1f;
                light.intensity = 2.5f;
                light.shadows = LightShadows.None;
            }

            var nodes = new HashSet<(int, int)>();
            var face = 0;
            foreach (var ((x, z, alongZ), _) in segments.OrderBy(s => s.Key.X).ThenBy(s => s.Key.Z))
            {
                plan.Openings.TryGetValue((x, z, alongZ), out var kind);
                var brickB = face++ % 3 == 2;
                string Plain(bool stone) => stone ? brickB ? "BkBrickB" : "BkBrick" : "BkPlaster";
                string Doorway(bool stone) => stone ? "BkDoorway" : "BkPlasterDoorway";

                // BK draws a wall on its local +Z side only: the solid wall faces the first room on the segment, a
                // wall between two rooms gets a face turned to the second, and each side takes its room's family,
                // brick for a stone room and plaster for the rest.
                var sides = Sides(plan, x, z, alongZ);
                GameObject Wall(Func<bool, string> family)
                {
                    var solid = Segment(family(sides[0].Stone), x, z, alongZ, sides[0].Facing);
                    if (sides.Count > 1) Segment(family(sides[1].Stone) + "Face", x, z, alongZ, sides[1].Facing);
                    return solid;
                }

                GameObject Leaf(string key) => Segment(key, x, z, alongZ, sides[0].Facing);
                // The shut leaf stands in its doorway's opening as a nested block, as the cell door in its bars.
                void Shut(GameObject doorway) => Leaf("BkDoor").transform.SetParent(doorway.transform, true);
                switch (kind)
                {
                    case Opening.Wall:
                        Wall(Plain);
                        break;
                    case Opening.Window:
                        Leaf("BkWindow");
                        break;
                    case Opening.Door:
                        Wall(Doorway);
                        break;
                    case Opening.Closed:
                        Shut(Wall(Doorway));
                        break;
                    case Opening.Cell:
                        Put("CellBars3", x, z, alongZ ? 3 : 0);
                        break;
                    case Opening.Street:
                        Shut(Leaf(Doorway(sides[0].Stone)));
                        var portal = Put("PortalLeaf", x, z, alongZ ? 3 : 0).GetComponent<Portal>();
                        portal.Configure(id + ".inside", SliceTestScenes.SceneName(VeilmouthScene.ScenePath), id + ".street", true);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(portal);
                        portal.gameObject.name = $"Portal - {id}.inside";
                        break;
                }

                nodes.Add((x, z));
                nodes.Add(alongZ ? (x, z + Module) : (x + Module, z));
            }

            foreach (var (x, z) in nodes)
            {
                Put("BkPost", x, z);
            }
        }

        /// <summary>
        /// The rooms beside a wall segment, north/south or east/west: the direction from the wall into each, and
        /// whether it is a stone room.
        /// </summary>
        private static List<(Vector2Int Facing, bool Stone)> Sides(Plan plan, int x, int z, bool alongZ)
        {
            var sides = new List<(Vector2Int, bool)>();
            void Look(Vector2Int facing, float px, float pz)
            {
                foreach (var r in plan.Rooms.Where(r => px > r.X && px < r.X + r.W && pz > r.Z && pz < r.Z + r.D))
                {
                    sides.Add((facing, r.Stone));
                }
            }

            var mid = Module * 0.5f;
            if (alongZ)
            {
                Look(E, x + 0.5f, z + mid);
                Look(W, x - 0.5f, z + mid);
            }
            else
            {
                Look(N, x + mid, z + 0.5f);
                Look(S, x + mid, z - 0.5f);
            }

            return sides;
        }

        /// <summary>A linear block on the 3 m segment from (x, z), its local +Z turned to <paramref name="facing"/>.</summary>
        private static GameObject Segment(string key, int x, int z, bool alongZ, Vector2Int facing)
        {
            var turns = Turns(facing);
            // Local +X runs +X (N), -X (S), -Z (E), +Z (W): the pivot is the segment's start or its end.
            var pivot = facing == S ? new Vector3(x + Module, 0f, z) : facing == E ? new Vector3(x, 0f, z + Module) : new Vector3(x, 0f, z);
            return At(key, pivot, Quaternion.Euler(0f, 90f * turns, 0f));
        }

        // ------------------------------------------------------------------------------------------------ furniture

        /// <summary>
        /// The rooms of §8 furnished. <c>Against</c> sets a block's back to a wall line with its front into the room,
        /// <c>Hang</c> hangs a shelf, a board or a painting on a wall's face, and <c>Put</c> places by the south-west
        /// corner of the turned footprint.
        /// </summary>
        private static void Furnish(string id)
        {
            switch (id)
            {
                case VeilmouthScene.SootAndBellId:
                    Put("BarCounter", 9, 4, 2).name = "Bar";
                    Against("BarCabinet", S, 9, 9);
                    Put("Barrel", 12, 7);
                    Put("Barrel", 13, 7);
                    Against("BkStove", E, 0, 4).name = "Fireplace";
                    Hang("BkPainting", E, 0, 6, 1.6f);
                    foreach (var (tx, tz) in new[] { (2, 1), (2, 5), (11, 1) })
                    {
                        Put("BkTavernTable", tx, tz);
                        Put("BkStool", tx - 1, tz);
                        Put("BkStool", tx + 2, tz + 1);
                    }

                    Hang("BkBottleShelf", S, 9, 12, 1.5f);
                    Against("BkStove", E, 0, 13);
                    Against("BkSink", S, 15, 3);
                    Against("BkCupboard", W, 6, 10);
                    Put("BkKitchenTable", 2, 11, 1);
                    Put("BkBasket", 1, 9);
                    Put("Bed", 12, 12).name = "Bed (a room for the night)";
                    Against("BkDrawer", S, 15, 10);
                    Put("BkChair", 13, 10);
                    Put("BkCarpet", 11, 10);
                    Hang("BkPainting", N, 0, 4, 1.9f);
                    Hang("BkPainting", W, 15, 7, 1.9f);
                    Against("BkSideTable", N, 0, 3);
                    Put("BkLogs", 1, 3);
                    Hang("BkDishShelf", S, 15, 1, 1.7f);
                    Against("BkDresser", E, 9, 10);
                    Hang("BkPainting", E, 6, 10, 1.9f);
                    Hang("BkPainting", W, 15, 10, 1.9f);
                    Put("BkTrunk", 10, 9);
                    VeilmouthDressing.Person("Yellow", false, 11.5f, 6.8f, 180f, "Tavern keeper", 0f);
                    VeilmouthDressing.Person("Brown", false, 1.5f, 6.5f, 90f, "Veinmarket at a table", 0f);
                    break;

                case "harrow-apothecary":
                    Put("BkCounter", 2, 3, 2).name = "Counter";
                    Put("BkCounter", 4, 3, 2).name = "Counter";
                    Hang("BkBottleShelf", S, 6, 1, 1.4f);
                    Hang("BkBottleShelf", S, 6, 3, 1.4f);
                    Against("BkCupboardGlass", E, 0, 1);
                    Against("BkDrawer", W, 9, 1);
                    Against("BkLabBench", S, 12, 0);
                    Against("BkAlembic", S, 12, 3);
                    Put("BkCauldron", 5, 8);
                    Against("BkDesk", E, 0, 7);
                    Put("BkStool", 2, 8);
                    Hang("BkBookShelf", S, 12, 6, 1.6f);
                    Hang("BkJarShelf", W, 9, 6, 1.5f);
                    Put("Barrel", 10, 10);
                    Put("Barrel", 10, 9);
                    Put("BkBasket", 11, 7);
                    Put("Crate", 10, 7);
                    Hang("BkBottleShelf", S, 6, 1, 2.1f);
                    Hang("BkBottleShelf", S, 6, 3, 2.1f);
                    Put("BkBasketB", 1, 4);
                    Put("Crate", 7, 4);
                    Put("BkWorkTable", 6, 8);
                    Hang("BkBookShelfB", S, 12, 6, 2.2f);
                    Put("BkGlobe", 1, 10);
                    Hang("BkJarShelf", E, 9, 7, 1.5f);
                    VeilmouthDressing.Person("Green", false, 3.5f, 4.8f, 180f, "Harrow", 0f);
                    break;

                case "faction-house":
                    Against("Rostrum", S, 9, 5);
                    foreach (var z in new[] { 2, 4, 6 })
                    {
                        Put("Pew", 1, z);
                        Put("Pew", 8, z);
                    }

                    Put("Candles", 3, 7);
                    Put("Candles", 8, 7);
                    Hang("BkPainting", E, 0, 6, 1.6f);
                    Against("BkDesk", S, 15, 2);
                    Put("BkChair", 3, 13);
                    Against("BkCupboard", E, 0, 10);
                    Put("BkCarpet", 2, 10);
                    foreach (var a in new[] { 6, 8 })
                    {
                        Against("BkCupboardTall", S, 15, a);
                    }

                    Hang("BkBookShelf", E, 6, 10, 1.6f);
                    Against("BkDesk", W, 12, 10);
                    Hang("BkPainting", W, 12, 7, 1.9f);
                    Put("BkCarpetLong", 4, 1);
                    Put("BkCarpetLong", 4, 4);
                    Hang("BkPainting", N, 9, 4, 1.9f);
                    Against("BkSideTable", W, 6, 12);
                    Hang("BkBookShelfB", E, 6, 10, 2.2f);
                    Hang("BkBookShelfC", E, 6, 12, 1.6f);
                    Hang("BkBookShelf", E, 6, 12, 2.2f);
                    Put("BkTrunk", 9, 12);
                    VeilmouthDressing.Person("Teal", false, 6f, 7.4f, 180f, "Faction member", 0f);
                    VeilmouthDressing.Person("Teal", true, 2.5f, 12.4f, 0f, "Faction member", 0f);
                    break;

                case "wardens-lockup":
                    Put("BkDesk", 3, 3, 2).name = "Front desk";
                    Against("Pew", E, 1, 1);
                    Hang("BkBoard", W, 9, 1, 1.0f).name = "Notices";
                    Against("BkDesk", W, 15, 2);
                    Put("BkChair", 12, 3);
                    Against("BkCupboard", S, 6, 11);
                    Hang("BkPainting", N, 0, 10, 1.6f);
                    Put("BkStool", 1, 10);
                    Put("BkBasket", 4, 10);
                    Put("BkLogs", 7, 10);
                    Against("BkStove", W, 15, 9);
                    Put("BkDiningTable", 10, 8);
                    Put("BkStool", 12, 8);
                    Against("BkCupboardTall", S, 12, 9);
                    Hang("BkPainting", N, 0, 1, 1.9f);
                    Against("BkCupboard", E, 0, 4);
                    Against("BkCupboardGlass", N, 0, 9);
                    Hang("BkPainting", S, 6, 12, 1.9f);
                    Against("BkArmsTable", S, 12, 12);
                    VeilmouthDressing.Person("Blue", false, 4f, 4.6f, 180f, "Warden", 0f);
                    VeilmouthDressing.Person("Blue", true, 11f, 10.5f, 180f, "Warden", 0f);
                    break;

                case "missing-room":
                    Put("Bed", 1, 6);
                    Against("BkStove", S, 9, 4);
                    Against("BkDrawer", W, 6, 4).name = "Wash stand";
                    Put("BkDiningTable", 3, 4);
                    Put("BkChair", 5, 3);
                    Put("BkTrunk", 3, 6).name = "The cache";
                    Hang("BkBookShelf", E, 0, 4, 1.6f);
                    Hang("BkPainting", S, 9, 1, 1.9f);
                    Against("BkDresser", S, 3, 3);
                    break;

                case "soot-stair-cellar":
                    Put("Barrels", 1, 3);
                    Put("Barrel", 4, 4);
                    Put("Barrel", 4, 3);
                    Put("Box", 4, 1);
                    Put("Firewood", 3, 1);
                    Put("Barrels", 16, 3);
                    Put("Box", 19, 1);
                    Put("BkLogs", 19, 4);
                    Put("BkBasketB", 1, 5);
                    Put("Crate", 16, 1);
                    Put("Candles", 7, 8);
                    Put("Candles", 14, 8);
                    VeilmouthDressing.Circle(new Vector3(10f, 0f, 4f), "Candidate 2, the cellar under the Soot Stair");
                    break;

                case "old-chapel":
                    Against("Rostrum", S, 15, 4);
                    foreach (var z in new[] { 3, 5, 7, 9, 11 })
                    {
                        Put("Pew", 0, z);
                        Put("Pew", 6, z);
                    }

                    Put("Candles", 2, 13);
                    Put("Candles", 6, 13);
                    Against("BkCupboardTall", S, 15, 9);
                    Against("BkDesk", W, 12, 10);
                    Put("BkCarpetLong", 3, 3);
                    Put("BkCarpetLong", 3, 6);
                    Put("BkCarpetLong", 3, 9);
                    Hang("BkPainting", W, 9, 7, 2.0f);
                    Put("BkTrunk", 10, 12);
                    break;
            }
        }

        // ------------------------------------------------------------------------------------------------ placing

        // The direction a block faces, into the room: its local +Z.
        private static readonly Vector2Int N = new(0, 1), S = new(0, -1), E = new(1, 0), W = new(-1, 0);

        private static int Turns(Vector2Int facing) => facing == N ? 0 : facing == E ? 1 : facing == S ? 2 : 3;

        /// <summary>
        /// A block with its back on the wall line <paramref name="line"/> (an x for E and W, a z for N and S), facing
        /// <paramref name="facing"/>, starting <paramref name="a"/> along the wall.
        /// </summary>
        private static GameObject Against(string key, Vector2Int facing, int line, int a)
        {
            var e = _kit[key];
            var turns = Turns(facing);
            if (facing == N) return Put(key, a, line, turns);
            if (facing == S) return Put(key, a, line - e.Depth, turns);
            if (facing == E) return Put(key, line, a, turns);
            return Put(key, line - e.Depth, a, turns);
        }

        /// <summary>A Face block hung on the wall line, facing into the room, spanning [a, a + width] along it.</summary>
        private static GameObject Hang(string key, Vector2Int facing, int line, int a, float y)
        {
            var e = _kit[key];
            var turns = Turns(facing);
            // Local +X runs +X (N), -X (S), -Z (E), +Z (W), so the pivot is the span's start or end.
            Vector3 pivot;
            if (facing == N) pivot = new Vector3(a, y, line);
            else if (facing == S) pivot = new Vector3(a + e.Width, y, line);
            else if (facing == E) pivot = new Vector3(line, y, a + e.Width);
            else pivot = new Vector3(line, y, a);
            return At(key, pivot, Quaternion.Euler(0f, 90f * turns, 0f));
        }

        /// <summary>
        /// A block whose turned footprint's south-west corner is (x, z), turned by <paramref name="turns"/> × 90° about Y;
        /// a linear or node block starts at the node (x, z) instead.
        /// </summary>
        private static GameObject Put(string key, int x, int z, int turns = 0, float y = 0f)
        {
            var e = _kit[key];
            var rotation = Quaternion.Euler(0f, 90f * turns, 0f);
            var pivot = new Vector3(x, y, z);
            if (e.Depth > 0)
            {
                var corners = new[]
                {
                    Vector3.zero, new Vector3(e.Width, 0f, 0f), new Vector3(0f, 0f, e.Depth), new Vector3(e.Width, 0f, e.Depth),
                }.Select(c => rotation * c).ToArray();
                pivot -= new Vector3(corners.Min(c => c.x), 0f, corners.Min(c => c.z));
            }

            return At(key, pivot, rotation);
        }

        private static GameObject At(string key, Vector3 pivot, Quaternion rotation)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(_kit[key].Prefab, _parent);
            go.transform.SetPositionAndRotation(
                new Vector3(Mathf.Round(pivot.x), Mathf.Round(pivot.y * 10f) / 10f, Mathf.Round(pivot.z)), rotation);
            return go;
        }

        private static Transform Group(Transform root, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(root, false);
            return t;
        }

        private static void Report(string path)
        {
            var blocks = Object.FindObjectsByType<KitBlock>(FindObjectsInactive.Exclude);
            var faults = KitLayoutCheck.Check(blocks);
            Debug.Log($"[VeilmouthInteriors] {path}: {blocks.Length} blocks, {faults.Count} kit faults" +
                      (faults.Count > 0 ? ":\n  " + string.Join("\n  ", faults.Take(40).Select(f => f.ToString())) : "."));
        }
    }
}

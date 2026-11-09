using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameDev.Slice.Interiors;
using GameDev.Slice.Kit;
using GameDev.World.Interiors;
using GameDev.World.Portals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The seven interiors of the city plan rebuilt wholly from Victorian Interiors (owner, 2026-11-07;
    /// VICTORIAN_INTERIORS_PLAN §7, VI-2): ground floors on the pack's 3 m module, walls of three finishes, stone only in
    /// the cellar's vaults, the cells and the chapel, floors, ceilings at the 3.5 m storey, the street door with its
    /// portal, the pack's furniture and §6's themed props, fireplaces where stoves stood, curtains on every window, and
    /// light from the clock (<see cref="InteriorLighting"/>): a lit pane and a spot at each window, lamps and candles,
    /// fire in the hearths. The people stand at their posts. Each scene is the owner's once built: it refuses an interior
    /// that already holds its root, and <see cref="BuildReplacing"/> is for the owner's request and the agent's batch
    /// iterations only. Plan coordinates: x east, z north, metres from the south-west corner; the street door is on the
    /// south wall, so an arriving player faces north into the room. Room sizes are authoring defaults.
    /// </summary>
    public static class VeilmouthInteriors
    {
        public const string RootName = "Interior VI";
        public const string RoomMarker = "Room - ";
        public const int Module = SliceKitCatalogue.ViModule;
        public const float Storey = SliceKitCatalogue.ViStorey;

        /// <summary>The ceiling's underside: the pack's ceiling is 0.23 m thick under the storey's height.</summary>
        public const float CeilingHeight = Storey - 0.23f;

        /// <summary>
        /// The layer of a ceiling's collider: the camera's spring arm meets it (SliceCharacterLook.CameraCollision), the
        /// Combat Grid does not read it as a floor (CombatGridReader.Mask leaves Ignore Raycast out).
        /// </summary>
        public const string CeilingLayer = "Ignore Raycast";

        private const float LampIntensity = 2.5f;
        private const string ChandelierLight = "Chandelier light";
        private const string FireLight = "Fire light";

        /// <summary>The hearths' fire: Vefects' small fire (asset map: Fire; owner, VI-8 remark 6), shared with the Dwellings.</summary>
        internal const string FirePath = "Assets/Vefects/Free Fire VFX URP/Particles/VFX_Fire_01_Small.prefab";

        // Measured (probe front views of 2026-11-07, m from the window's middle): the glass painted on each window wall.
        internal static readonly Dictionary<string, (float U0, float U1, float Y0, float Y1, float ProbeU, float ProbeY)> Glass = new()
        {
            ["ViPaperWindow"] = (-0.68f, 0.68f, 0.71f, 2.85f, 0.45f, 2.4f),
            ["ViDarkWindow"] = (-0.46f, 0.46f, 1.17f, 2.94f, 0.35f, 2.75f),
        };

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
        private static Plan _plan;

        /// <summary>The floor of the storey being built or furnished: every placement stands this much higher (VI-3).</summary>
        private static float _floorY;
        private static readonly List<Light> WindowLights = new();
        private static readonly List<Renderer> Panes = new();
        private static readonly List<InteriorLighting.Hearth> Hearths = new();

        [MenuItem("GameDev/Slice/Build Veilmouth Interiors from Victorian Interiors (once)")]
        public static void Build() => Run(false);

        /// <summary>Batch only, at the owner's request or for the agent's iterations: replaces the seven interiors.</summary>
        public static void BuildReplacing() => Run(true);

        public static void BuildReplacingAndRender()
        {
            Run(true);
            VeilmouthRenders.RenderInteriorsNoonAndNight();
        }

        private static void Run(bool replace)
        {
            // Every other wrapper as it is on disk (the owner's edits stay); the Victorian ones built afresh.
            _kit = SliceKitCatalogue.Load();
            foreach (var kv in SliceKitCatalogue.BuildVictorian())
            {
                _kit[kv.Key] = kv.Value;
            }

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
            Street,  // the doorway to the street: a shut leaf and the portal
            Closed,  // a doorway with its leaf shut (a door to nowhere the player goes)
            Yard,    // a back door to the building's yard: a shut leaf and the portal to the yard (owner, VI-8 second follow-up)
            Window,
            Cell,    // a cell's front: the bars with their door
            None,    // no wall: one room open into the next (the Soot & Bell's back landing, VI-7)
        }

        private const string Stone = "Stone";

        private sealed class Plan
        {
            public readonly List<(string Name, int X, int Z, int W, int D, string Finish, int Level)> Rooms = new();
            public readonly Dictionary<(int X, int Z, bool AlongZ, int Level), Opening> Openings = new();
            public readonly List<(int X, int Z, int W, int D, int Level)> Wells = new();
            public (int X, int Z) Street;

            public bool Windowless => !Openings.Values.Contains(Opening.Window);

            /// <summary>A room of one finish: Paper, Dark, Panel (ViFinishes) or Stone, on storey <paramref name="level"/> (0 the ground).</summary>
            public Plan Room(string name, int x, int z, int w, int d, string finish, int level = 0)
            {
                Rooms.Add((name, x, z, w, d, finish, level));
                return this;
            }

            /// <summary>The wall segment of one module from node (x, z) along +X, or along +Z, on storey <paramref name="level"/>.</summary>
            public Plan Open(Opening kind, int x, int z, bool alongZ = false, int level = 0)
            {
                Openings[(x, z, alongZ, level)] = kind;
                if (kind == Opening.Street)
                {
                    Street = (x, z);
                }

                return this;
            }

            /// <summary>
            /// A stair well: storey <paramref name="level"/> has no floor over these modules, and the storey under it no
            /// ceiling (VI-3). Its edges are the module lines.
            /// </summary>
            public Plan Well(int x, int z, int w, int d, int level)
            {
                Wells.Add((x, z, w, d, level));
                return this;
            }

            public bool InWell(int cx, int cz, int level) =>
                Wells.Any(w => w.Level == level && cx >= w.X && cx < w.X + w.W && cz >= w.Z && cz < w.Z + w.D);
        }

        private static void BuildOne(string id, string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            // No sun indoors: the pack's windows are opaque, and daylight comes through their lit panes (plan §5).
            foreach (var sun in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(sun.gameObject);
            }

            var root = new GameObject(RootName).transform;
            var plan = PlanFor(id);
            _plan = plan;
            WindowLights.Clear();
            Panes.Clear();
            Hearths.Clear();

            _parent = Group(root, "Rooms");
            Rooms(plan, id);
            _parent = Group(root, "Furniture");
            VeilmouthDressing.Use(_kit, Group(root, "People"));
            Furnish(id);
            NestOnStairs(root);

            var hearthLights = new HashSet<Light>(Hearths.Select(h => h.Light));
            var lamps = _parent.GetComponentsInChildren<Light>(true).Where(l => !hearthLights.Contains(l)).ToArray();
            foreach (var lamp in lamps)
            {
                // The pack's lamps were lit for baked light (0.9); realtime, a lamp lights its corner of the room.
                // Authoring defaults: strength 2.5 at least, reach 6–9 m.
                lamp.intensity = Mathf.Max(lamp.intensity, LampIntensity);
                lamp.range = lamp.name == ChandelierLight ? lamp.range : Mathf.Clamp(lamp.range, 6f, 9f);
                lamp.shadows = LightShadows.None;
                PrefabUtility.RecordPrefabInstancePropertyModifications(lamp);
            }

            var lighting = new GameObject("Lighting").AddComponent<InteriorLighting>();
            lighting.transform.SetParent(root, false);
            lighting.Configure(WindowLights.ToArray(), Panes.ToArray(), lamps, Hearths.ToArray(), plan.Windowless);
            lighting.Apply(lighting.PreviewMinute);

            new GameObject("Interior").AddComponent<InteriorRuntime>().Configure(id);
            SliceTestScenes.AddPlayerRig(new Vector3(plan.Street.X + Module * 0.5f, 0.1f, plan.Street.Z + 1.6f));
            SliceTestScenes.Save(scene, path);
            Report(path, $"{WindowLights.Count} windows, {lamps.Length} lamps, {Hearths.Count} hearths", Doorways(plan));
        }

        private static Plan PlanFor(string id)
        {
            const string paper = "Paper", dark = "Dark", panel = "Panel";
            switch (id)
            {
                case VeilmouthScene.SootAndBellId:
                    // The pub plan of the furnishing rules §6 (VI-7). The taproom rises through two storeys, 7 m, with no
                    // floor over it (owner, rules §9): its upper storey is all well, lit by a row of high windows. The
                    // street door is in the middle of the front, the kitchen and stair-hall doors at the ends of the back
                    // wall. The back range is 12 m deep (an authoring default, deeper than VI-2's 9 m) so that the second
                    // let room stands over the stair hall: the stair climbs east from x 11 to the well's end at x 18, and
                    // upstairs the landing keeps the well's south and west sides, the second let room its north. No wall
                    // closes the landing's west strip to the north: the well's west balustrade runs to the module line,
                    // where a wall's body would meet it, so the strip opens into a back landing. The back door to the yard
                    // is in the stair hall's east wall, as the yard lies east of the house in the city (owner, VI-8 second
                    // follow-up: a door leads out or goes).
                    return new Plan()
                        .Room("Taproom", 0, 0, 21, 12, panel).Room("Kitchen", 0, 12, 6, 12, dark).Room("Scullery", 6, 12, 3, 12, dark)
                        .Room("Stair hall", 9, 12, 12, 12, dark)
                        .Room("Taproom", 0, 0, 21, 12, paper, 1)
                        .Room("Landing", 9, 12, 12, 6, dark, 1).Room("Let room", 0, 12, 9, 12, paper, 1)
                        .Room("Back landing", 9, 18, 3, 6, dark, 1).Room("Let room 2", 12, 18, 9, 6, paper, 1)
                        .Well(0, 0, 21, 12, 1).Well(12, 15, 6, 3, 1)
                        .Open(Opening.Street, 9, 0).Open(Opening.Window, 3, 0).Open(Opening.Window, 15, 0)
                        .Open(Opening.Window, 0, 3, true).Open(Opening.Window, 21, 6, true)
                        .Open(Opening.Door, 0, 12).Open(Opening.Door, 18, 12).Open(Opening.Door, 6, 15, true).Open(Opening.Door, 9, 18, true)
                        .Open(Opening.Yard, 21, 21, true).Open(Opening.Window, 0, 18, true).Open(Opening.Window, 0, 21, true).Open(Opening.Window, 3, 24)
                        .Open(Opening.Window, 6, 24).Open(Opening.Window, 15, 24).Open(Opening.Window, 21, 15, true).Open(Opening.Window, 21, 18, true)
                        .Open(Opening.Window, 3, 0, false, 1).Open(Opening.Window, 9, 0, false, 1).Open(Opening.Window, 15, 0, false, 1)
                        .Open(Opening.Window, 0, 6, true, 1).Open(Opening.Window, 21, 6, true, 1)
                        .Open(Opening.Door, 9, 15, true, 1).Open(Opening.Door, 18, 18, false, 1).Open(Opening.None, 9, 18, false, 1)
                        .Open(Opening.Window, 0, 15, true, 1).Open(Opening.Window, 0, 18, true, 1).Open(Opening.Window, 0, 24, false, 1)
                        .Open(Opening.Window, 21, 15, true, 1).Open(Opening.Window, 21, 18, true, 1).Open(Opening.Window, 15, 24, false, 1)
                        .Open(Opening.Window, 18, 24, false, 1);
                case "harrow-apothecary":
                    return new Plan()
                        .Room("Shop", 0, 0, 12, 9, paper).Room("Laboratory", 0, 9, 12, 9, dark).Room("Store room", 12, 9, 3, 9, dark)
                        .Open(Opening.Street, 3, 0).Open(Opening.Window, 9, 0).Open(Opening.Window, 0, 3, true)
                        .Open(Opening.Door, 9, 9).Open(Opening.Door, 12, 12, true).Open(Opening.Window, 0, 9, true)
                        .Open(Opening.Window, 6, 18);
                case "faction-house":
                    // The hall is 21 m wide (VI-7; 18 m before) so that it is symmetric about the street door's axis,
                    // x 10.5: the aisle, the pews, the rostrum, the two hearths and the doors at the back (rules §6).
                    return new Plan()
                        .Room("Hall", 0, 0, 21, 12, panel).Room("Office", 0, 12, 9, 9, paper).Room("Archive", 9, 12, 12, 9, dark)
                        .Open(Opening.Street, 9, 0).Open(Opening.Window, 3, 0).Open(Opening.Window, 15, 0)
                        .Open(Opening.Window, 0, 6, true).Open(Opening.Window, 21, 6, true)
                        .Open(Opening.Door, 3, 12).Open(Opening.Door, 15, 12).Open(Opening.Window, 6, 21)
                        .Open(Opening.Window, 18, 21).Open(Opening.Window, 21, 12, true);
                case "wardens-lockup":
                    return new Plan()
                        .Room("Front desk", 0, 0, 12, 9, dark).Room("Office", 12, 0, 9, 9, paper).Room("Corridor", 0, 9, 12, 3, dark)
                        .Room("Guard room", 12, 9, 9, 9, dark).Room("Cell 1", 0, 12, 3, 6, Stone).Room("Cell 2", 3, 12, 3, 6, Stone)
                        .Room("Cell 3", 6, 12, 3, 6, Stone)
                        .Open(Opening.Street, 3, 0).Open(Opening.Window, 9, 0).Open(Opening.Window, 0, 3, true)
                        .Open(Opening.Door, 6, 9).Open(Opening.Door, 12, 3, true).Open(Opening.Door, 12, 9, true)
                        .Open(Opening.Window, 15, 0).Open(Opening.Window, 21, 3, true).Open(Opening.Window, 21, 12, true)
                        .Open(Opening.Cell, 0, 12).Open(Opening.Cell, 3, 12).Open(Opening.Cell, 6, 12);
                case "missing-room":
                    // The landing had a closed neighbour's door (city plan §14) that led nowhere; it is a wall (owner, VI-8
                    // follow-up: a door that is no way out is taken away).
                    return new Plan()
                        .Room("Landing", 0, 0, 9, 3, dark).Room("Room", 0, 3, 9, 9, dark)
                        .Open(Opening.Street, 3, 0).Open(Opening.Door, 0, 3)
                        .Open(Opening.Window, 3, 12);
                case "soot-stair-cellar":
                    return new Plan()
                        .Room("West vault", 0, 0, 9, 9, Stone).Room("Circle vault", 9, 0, 12, 12, Stone).Room("East vault", 21, 0, 9, 9, Stone)
                        .Open(Opening.Street, 0, 0).Open(Opening.Door, 9, 3, true).Open(Opening.Door, 21, 3, true);
                case "old-chapel":
                    // The nave is 15 m wide: two rows of 4 m pews and a 3 m aisle under the door.
                    return new Plan()
                        .Room("Nave", 0, 0, 15, 21, Stone).Room("Vestry", 15, 12, 3, 9, Stone)
                        .Open(Opening.Street, 6, 0).Open(Opening.Window, 0, 6, true).Open(Opening.Window, 0, 12, true)
                        .Open(Opening.Window, 15, 6, true).Open(Opening.Door, 15, 15, true);
                default:
                    throw new ArgumentException(id);
            }
        }

        // ------------------------------------------------------------------------------------------------ rooms

        /// <summary>
        /// Floors, ceilings, a marker and ceiling colliders per room, storey by storey; one wall per module; posts at
        /// stone nodes. A stair well leaves out the floor over it and the ceiling and its collider under it.
        /// </summary>
        private static void Rooms(Plan plan, string id)
        {
            var segments = new HashSet<(int X, int Z, bool AlongZ, int Level)>();
            foreach (var (name, x, z, w, d, finish, level) in plan.Rooms)
            {
                _floorY = level * Storey;
                var ceiling = new GameObject("Ceiling collider - " + name);
                ceiling.transform.SetParent(_parent, false);
                ceiling.layer = LayerMask.NameToLayer(CeilingLayer);
                for (var cx = x; cx < x + w; cx += Module)
                {
                    for (var cz = z; cz < z + d; cz += Module)
                    {
                        if (!plan.InWell(cx, cz, level))
                        {
                            At(finish == Stone ? "ViStoneFloor" : "ViFloor", new Vector3(cx, 0f, cz), Quaternion.identity);
                        }

                        if (plan.InWell(cx, cz, level + 1))
                        {
                            continue;
                        }

                        At("ViCeiling", new Vector3(cx, Storey, cz), Quaternion.identity);
                        // The camera stays under the ceiling (owner's play test, 2026-11-06). Not a kit block: a box the
                        // arm meets, one per module so a stair well stays open to the camera.
                        var box = ceiling.AddComponent<BoxCollider>();
                        // Under the next floor's slab (3.4 m), never through it, so a person or a prop upstairs stands
                        // on the floor and not on this box (VI-3): 3.14–3.40 m, about the ceiling's underside.
                        box.center = new Vector3(cx + Module * 0.5f, _floorY + CeilingHeight, cz + Module * 0.5f);
                        box.size = new Vector3(Module, 0.26f, Module);
                    }

                    segments.Add((cx, z, false, level));
                    segments.Add((cx, z + d, false, level));
                }

                for (var cz = z; cz < z + d; cz += Module)
                {
                    segments.Add((x, cz, true, level));
                    segments.Add((x + w, cz, true, level));
                }

                // A storey that is all well is the upper half of a tall room below (the taproom, VI-7): no room of its own.
                if (plan.InWell(x, z, level) && plan.InWell(x + w - 1, z + d - 1, level))
                {
                    continue;
                }

                var marker = new GameObject(RoomMarker + name).transform;
                marker.SetParent(_parent, false);
                marker.position = new Vector3(x + w * 0.5f, _floorY, z + d * 0.5f);
                marker.localScale = new Vector3(w, Storey, d);
            }

            var stoneNodes = new HashSet<(int, int, int)>();
            var face = 0;
            foreach (var (x, z, alongZ, level) in segments.OrderBy(s => s.Level).ThenBy(s => s.X).ThenBy(s => s.Z).ThenBy(s => s.AlongZ))
            {
                _floorY = level * Storey;
                plan.Openings.TryGetValue((x, z, alongZ, level), out var kind);
                if (kind == Opening.None)
                {
                    continue;
                }

                var sides = Sides(plan, x, z, alongZ, level);
                if (kind == Opening.Cell)
                {
                    Put("ViCellBars", x, z, alongZ ? 3 : 0).name = "Cell front";
                    continue;
                }

                // The main room is the one listed first: its finish takes the wall, which is drawn on both sides.
                var main = sides.OrderBy(s => s.Index).First();
                var other = sides.Count > 1 ? sides.OrderBy(s => s.Index).Last() : default;
                var stone = main.Finish == Stone;
                if (sides.Count > 1 && (other.Finish == Stone) != stone)
                {
                    throw new InvalidOperationException($"{id}: the wall at ({x}, {z}) stands between stone and a finished room; plan them apart.");
                }

                if (kind == Opening.Window && sides.Count > 1)
                {
                    throw new InvalidOperationException($"{id}: a window at ({x}, {z}) between two rooms.");
                }

                if (stone)
                {
                    StoneWall(kind, x, z, alongZ, main.Facing, sides.Count > 1 ? other.Facing : (Vector2Int?)null, face++ % 3 == 2, id);
                    stoneNodes.Add((x, z, level));
                    stoneNodes.Add(alongZ ? (x, z + Module, level) : (x + Module, z, level));
                }
                else
                {
                    FinishedWall(kind, main.Finish, x, z, alongZ, main.Facing, id);
                }
            }

            foreach (var (x, z, level) in stoneNodes)
            {
                _floorY = level * Storey;
                At("ViStonePost", new Vector3(x, 0f, z), Quaternion.identity);
            }

            _floorY = 0f;
        }

        private static void FinishedWall(Opening kind, string finish, int x, int z, bool alongZ, Vector2Int facing, string id)
        {
            var (plain, doorway, window) = SliceKitCatalogue.ViFinishes[finish];
            doorway ??= SliceKitCatalogue.ViFinishes["Dark"].Doorway;
            window ??= SliceKitCatalogue.ViFinishes["Dark"].Window;
            switch (kind)
            {
                case Opening.Wall:
                    Segment(plain, x, z, alongZ, facing);
                    break;
                case Opening.Door:
                    Segment(doorway, x, z, alongZ, facing);
                    break;
                case Opening.Closed:
                    Shut(Segment(doorway, x, z, alongZ, facing), "ViDoor", x, z, alongZ, facing);
                    break;
                case Opening.Street:
                    Shut(Segment(doorway, x, z, alongZ, facing), "ViDoor", x, z, alongZ, facing).name = "Street door";
                    StreetPortal(id, x, z, alongZ);
                    break;
                case Opening.Yard:
                    Shut(Segment(doorway, x, z, alongZ, facing), "ViDoor", x, z, alongZ, facing).name = "Yard door";
                    YardPortal(id, x, z, alongZ, facing);
                    break;
                case Opening.Window:
                    var wall = Segment(window, x, z, alongZ, facing);
                    Daylight(wall, window, x, z, alongZ, facing);
                    var curtain = Hang("ViCurtain", facing, alongZ ? x : z, alongZ ? z : x, 0.2f);
                    curtain.transform.SetParent(_parent, true);
                    break;
            }
        }

        /// <summary>
        /// BK's stone is drawn on its local +Z side only: the solid wall faces the main room, and a wall between two
        /// stone rooms gets a seen face turned to the other.
        /// </summary>
        private static void StoneWall(Opening kind, int x, int z, bool alongZ, Vector2Int facing, Vector2Int? back, bool brickB, string id)
        {
            var plain = brickB ? "ViStoneWallB" : "ViStoneWall";
            switch (kind)
            {
                case Opening.Wall:
                    Segment(plain, x, z, alongZ, facing);
                    if (back.HasValue) Segment(plain + "Face", x, z, alongZ, back.Value);
                    break;
                case Opening.Door:
                    Segment("ViStoneDoorway", x, z, alongZ, facing);
                    if (back.HasValue) Segment("ViStoneDoorwayFace", x, z, alongZ, back.Value);
                    break;
                case Opening.Closed:
                    Shut(Segment("ViStoneDoorway", x, z, alongZ, facing), "ViStoneDoor", x, z, alongZ, facing);
                    break;
                case Opening.Street:
                    Shut(Segment("ViStoneDoorway", x, z, alongZ, facing), "ViStoneDoor", x, z, alongZ, facing).name = "Street door";
                    StreetPortal(id, x, z, alongZ);
                    break;
                case Opening.Yard:
                    Shut(Segment("ViStoneDoorway", x, z, alongZ, facing), "ViStoneDoor", x, z, alongZ, facing).name = "Yard door";
                    YardPortal(id, x, z, alongZ, facing);
                    break;
                case Opening.Window:
                    var wall = Segment("ViStoneWindow", x, z, alongZ, facing);
                    Daylight(wall, "ViStoneWindow", x, z, alongZ, facing);
                    break;
            }
        }

        /// <summary>The shut leaf stands in its doorway's opening as a nested block (kit standard §6).</summary>
        private static GameObject Shut(GameObject doorway, string leaf, int x, int z, bool alongZ, Vector2Int facing)
        {
            var door = Segment(leaf, x, z, alongZ, facing);
            door.transform.SetParent(doorway.transform, true);
            return door;
        }

        private static void StreetPortal(string id, int x, int z, bool alongZ)
        {
            var portal = Put("PortalLeaf", x, z, alongZ ? 3 : 0).GetComponent<Portal>();
            portal.Configure(id + ".inside", SliceTestScenes.SceneName(VeilmouthScene.ScenePath), id + ".street", true);
            PrefabUtility.RecordPrefabInstancePropertyModifications(portal);
            portal.gameObject.name = $"Portal - {id}.inside";
        }

        /// <summary>
        /// The back door's portal to the building's yard in the city (`{id}.yard`): on the wall segment like the wall
        /// itself, its local +Z into the room, where a player coming in from the yard arrives.
        /// </summary>
        private static void YardPortal(string id, int x, int z, bool alongZ, Vector2Int facing)
        {
            var portal = Segment("PortalLeaf", x, z, alongZ, facing).GetComponent<Portal>();
            portal.Configure(id + ".yard.inside", SliceTestScenes.SceneName(VeilmouthScene.ScenePath), id + ".yard", true);
            PrefabUtility.RecordPrefabInstancePropertyModifications(portal);
            portal.gameObject.name = $"Portal - {id}.yard.inside";
        }

        /// <summary>
        /// A window's daylight (plan §5): a lit pane of our own unlit material on its glass, and a spot just inside
        /// aimed down into the room; <see cref="InteriorLighting"/> sets both from the hour. The pack's glass is painted on
        /// the wall, so its rectangle is measured; BK's stone window is found by rays through its opening.
        /// </summary>
        private static void Daylight(GameObject wall, string key, int x, int z, bool alongZ, Vector2Int facing)
        {
            var f = new Vector3(facing.x, 0f, facing.y);
            var u = alongZ ? Vector3.forward : Vector3.right;
            var centre = new Vector3(x, _floorY, z) + u * (Module * 0.5f);
            var probes = new List<MeshCollider>();
            foreach (var filter in wall.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh != null && filter.TryGetComponent<MeshRenderer>(out var r) && r.enabled)
                {
                    var c = filter.gameObject.AddComponent<MeshCollider>();
                    c.sharedMesh = filter.sharedMesh;
                    probes.Add(c);
                }
            }

            var backfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            Physics.SyncTransforms();
            try
            {
                float? Hit(Vector3 at)
                {
                    float? best = null;
                    foreach (var c in probes)
                    {
                        if (c.Raycast(new Ray(at + f, -f), out var hit, 2f) && (best == null || hit.distance < best))
                        {
                            best = hit.distance;
                        }
                    }

                    return best.HasValue ? 1f - best.Value : (float?)null; // m from the wall's line into the room
                }

                float u0, u1, y0, y1, depth;
                if (Glass.TryGetValue(key, out var g))
                {
                    (u0, u1, y0, y1) = (g.U0, g.U1, g.Y0, g.Y1);
                    depth = Hit(centre + u * g.ProbeU + Vector3.up * g.ProbeY) ?? SliceKitCatalogue.ViFace;
                }
                else
                {
                    // The opening is where the rays go deeper than the wall's face, the most common depth: to its glass
                    // and frame, or through.
                    var samples = new List<(float U, float Y, float? D)>();
                    for (var su = -1.4f; su <= 1.4f; su += 0.05f)
                    {
                        for (var sy = 0.2f; sy <= Storey - 0.2f; sy += 0.05f)
                        {
                            samples.Add((su, sy, Hit(centre + u * su + Vector3.up * sy)));
                        }
                    }

                    var face = samples.Where(o => o.D.HasValue).GroupBy(o => Mathf.Round(o.D.Value * 50f))
                        .OrderByDescending(g => g.Count()).Select(g => g.Key / 50f).DefaultIfEmpty(0f).First();
                    var open = samples.Where(o => o.D == null || o.D < face - 0.04f).ToList();
                    (u0, u1, y0, y1) = open.Count > 20
                        ? (open.Min(o => o.U), open.Max(o => o.U), open.Min(o => o.Y), open.Max(o => o.Y))
                        : (-0.6f, 0.6f, 1.0f, 2.6f);
                    var glass = open.Where(o => o.D.HasValue).Select(o => o.D.Value).OrderBy(d => d).ToList();
                    depth = glass.Count > 0 ? glass[glass.Count / 2] : 0f;
                    Debug.Log($"[VeilmouthInteriors] {key} at ({x}, {z}): face {face:0.00} m, opening u {u0:0.00}..{u1:0.00}, y {y0:0.00}..{y1:0.00}, pane at {depth:0.00} m.");
                    if (open.Count <= 20)
                    {
                        Debug.LogWarning($"[VeilmouthInteriors] {key} at ({x}, {z}): no opening found; the pane takes an authoring default.");
                    }
                }

                var pane = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(pane.GetComponent<Collider>());
                pane.name = "Window pane";
                pane.layer = LayerMask.NameToLayer(KitLayers.NoGrid);
                pane.transform.SetParent(_parent, false);
                pane.transform.position = centre + u * ((u0 + u1) * 0.5f) + Vector3.up * ((y0 + y1) * 0.5f) + f * (depth + 0.005f);
                pane.transform.rotation = Quaternion.LookRotation(-f); // a quad shows its face to its local -Z: into the room
                pane.transform.localScale = new Vector3(u1 - u0, y1 - y0, 1f);
                var renderer = pane.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = SliceKitCatalogue.Colour("ViWindowPane", new Color(0.78f, 0.86f, 0.95f), true);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                Panes.Add(renderer);

                var spot = new GameObject("Window light").AddComponent<Light>();
                spot.transform.SetParent(_parent, false);
                spot.transform.position = centre + Vector3.up * (y1 - 0.1f) + f * 0.5f;
                spot.transform.rotation = Quaternion.LookRotation((f * 0.75f) + (Vector3.down * 0.66f)); // about 41° down
                spot.type = LightType.Spot;
                spot.range = 10f;
                spot.spotAngle = 95f;
                spot.innerSpotAngle = 55f;
                spot.shadows = LightShadows.None;
                WindowLights.Add(spot);
            }
            finally
            {
                Physics.queriesHitBackfaces = backfaces;
                foreach (var c in probes)
                {
                    Object.DestroyImmediate(c);
                }
            }
        }

        /// <summary>
        /// The rooms beside a wall segment, north/south or east/west: the direction from the wall into each, its finish,
        /// and its place in the plan.
        /// </summary>
        private static List<(Vector2Int Facing, string Finish, int Index)> Sides(Plan plan, int x, int z, bool alongZ, int level)
        {
            var sides = new List<(Vector2Int, string, int)>();
            void Look(Vector2Int facing, float px, float pz)
            {
                for (var i = 0; i < plan.Rooms.Count; i++)
                {
                    var r = plan.Rooms[i];
                    if (r.Level == level && px > r.X && px < r.X + r.W && pz > r.Z && pz < r.Z + r.D)
                    {
                        sides.Add((facing, r.Finish, i));
                    }
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

        /// <summary>A linear block on the segment of one module from (x, z), its local +Z turned to <paramref name="facing"/>.</summary>
        private static GameObject Segment(string key, int x, int z, bool alongZ, Vector2Int facing)
        {
            // Local +X runs +X (N), -X (S), -Z (E), +Z (W): the pivot is the segment's start or its end.
            var pivot = facing == S ? new Vector3(x + Module, 0f, z) : facing == E ? new Vector3(x, 0f, z + Module) : new Vector3(x, 0f, z);
            return At(key, pivot, Quaternion.Euler(0f, 90f * Turns(facing), 0f));
        }

        // ------------------------------------------------------------------------------------------------ furniture

        /// <summary>
        /// The rooms of plan §7 furnished from the pack and §6's themed props. <c>Against</c> sets a block's back to a wall
        /// line with its front into the room, <c>Hang</c> hangs a picture, a lamp or a shelf on a wall's face, and
        /// <c>Put</c> places by the south-west corner of the turned footprint. A piece three cells wide never stands in a
        /// corner: the side wall's dado meets it (plan §3).
        /// </summary>
        private static void Furnish(string id)
        {
            switch (id)
            {
                case VeilmouthScene.SootAndBellId:
                    SootAndBell();
                    break;
                case "harrow-apothecary":
                    Harrow();
                    break;
                case "faction-house":
                    FactionHouse();
                    break;
                case "wardens-lockup":
                    Lockup();
                    break;
                case "missing-room":
                    MissingRoom();
                    break;
                case "soot-stair-cellar":
                    Cellar();
                    break;
                case "old-chapel":
                    Chapel();
                    break;
            }
        }

        private static void SootAndBell()
        {
            // Taproom, by the pub plan of rules §6: the bar faces the street door across the room, on its axis, the
            // back-bar cabinet behind it and the keeper between; nothing stands on the 5 m way from the door to the bar.
            // West of it the saloon: carpet, upholstered chairs, the hearth and two snug tables; east the public bar:
            // tables with stools on the bare floor, and a small stage at the end wall on the room's axis, chairs facing it
            // and no table within 2 m of its front, the owner's Tripo piano on it (furnishing rules §10). The low partition
            // and the fixed benches are missing from the pack and wait in SLICE_MODELS_WANTED; a piece is not made up of
            // other pieces (owner, VI-8 remark 5). Three Tripo chandeliers hang in the 7 m height, the one room of the seven
            // higher than a storey; the saloon's chairs are the Tripo wing chairs.
            Fireplace(E, 0, 6);
            Put("BarCounter", 8, 8, 2).name = "Bar";
            Against("BarCabinet", S, 12, 9);
            Put("Barrel", 6, 10);
            Put("Barrel", 7, 10);
            Hang("BkBottleShelf", S, 12, 6, 1.6f);
            Hang("BkBottleShelf", S, 12, 6, 2.3f);

            // The saloon.
            Put("ViCarpet", 2, 4);
            Put("ViSofa", 4, 6, 3);
            Put("ViTableRound", 3, 7);
            Put("TrArmchair", 3, 6);
            Put("TrArmchair", 3, 8, 2);
            Table(1, 2, "TrArmchair");
            Table(4, 2, "TrArmchair");
            Put("ViCoatStand", 7, 0);

            // The public bar and its stage.
            Table(13, 1, "BkStool");
            Table(13, 9, "BkStool");
            foreach (var z in new[] { 4, 5, 6, 7 })
            {
                Put("BkChair", 16, z, 1);
            }

            Stage(18, 3, 1, 2);
            Put("ViPlantC", 19, 0);
            Against("ViClock", W, 21, 10);

            Chandelier(3, 5, Storey, TaproomChandelier);
            Chandelier(9, 3, Storey, TaproomChandelier);
            Chandelier(15, 5, Storey, TaproomChandelier);
            Hang("ViWallLamp", N, 0, 6, 2.2f);
            Hang("ViWallLamp", N, 0, 12, 2.2f);
            Hang("ViWallLamp", E, 0, 9, 2.2f);
            Hang("ViWallLamp", W, 21, 3, 2.2f);
            Hang("ViWallLamp", W, 21, 9, 2.2f);
            Hang("ViPicture3", E, 0, 0, 1.6f);
            Hang("ViPicture6", N, 0, 0, 1.6f);
            Hang("ViPicture1", N, 0, 18, 1.6f);
            Hang("ViPicture7", W, 21, 0, 1.6f);
            Hang("ViPicture2", S, 12, 3, 1.6f);
            Hang("ViPicture4", S, 12, 15, 1.6f);
            VeilmouthDressing.Person("Yellow", false, 10.5f, 10.3f, 180f, "Tavern keeper", 0f);
            VeilmouthDressing.Person("Brown", false, 3.5f, 2.5f, 270f, "Veinmarket at a table", 0f);

            // Kitchen: the hearth burns all day.
            Fireplace(E, 0, 15, true);
            Put("ViKitchenTable", 2, 19);
            Put("ViChair", 2, 18);
            Put("ViChair", 3, 18);
            Put("ViChair", 2, 21, 2);
            Put("ViChair", 3, 21, 2);
            Against("ViCupboardGlass", S, 24, 1);
            Against("ViBasin", W, 6, 22);
            Put("ViBucket", 5, 13);
            Hang("ViWallLamp", N, 12, 3, 2.2f);

            // Scullery: the sink. Its one column clear of both walls is its walk-way, so nothing stands in it (VI-8).
            Against("ViBasin", E, 6, 13).name = "Scullery sink";

            // Stair hall: the stair climbs east to the landing (VI-3). No barrels or boxes: they stand only in stores,
            // warehouses and the port, and at the bar's end (owner, 2026-11-09; furnishing rules G5).
            Put("ViStair", 11, 15, 1).name = "Stair";
            Put("ViCoatStand", 10, 12);
            Against("ViClock", W, 21, 21);
            Hang("ViWallLamp", E, 9, 12, 2.2f);
            Hang("ViWallLamp", W, 21, 12, 2.2f);
            Hang("ViWallLamp", S, 24, 12, 2.2f);
            Hang("ViPicture2", N, 12, 12, 1.6f);

            // Upstairs.
            _floorY = Storey;

            // Landing: a balustrade on the well's south and west edges where the stair's own rails are low; the second
            // let room's wall is its north edge. Each stands on the well's edge, its front toward the well (VI-5).
            foreach (var (bx, bz, bt) in new[] { (12, 14, 0), (15, 14, 0), (11, 15, 1) })
            {
                Put("ViBanister", bx, bz, bt).name = "Balustrade";
            }

            // A newel post where the west and south balustrades meet (VI-8 remark 4).
            foreach (var (node, rotation) in Dwellings.DwellingAssembler.NewelSpots(_parent))
            {
                var post = (GameObject)PrefabUtility.InstantiatePrefab(_kit[Dwellings.DwellingAssembler.NewelKey].Prefab, _parent);
                post.name = "Newel";
                post.transform.SetPositionAndRotation(node, rotation);
            }

            Put("ViPlant", 19, 12);
            Hang("ViWallLamp", N, 12, 12, 2.2f);
            Hang("ViWallLamp", E, 9, 12, 2.2f);
            Hang("ViPicture5", N, 12, 15, 1.6f);

            // The let room over the kitchen: a bed with its nightstand, a wash stand, a chest of drawers, a small table.
            Against("ViBed", S, 24, 3).name = "Bed (a room for the night)";
            Against("TrNightstand", S, 24, 5);
            Against("ViBasinSet", E, 0, 21).name = "Wash stand";
            Against("TrDresser", N, 12, 1);
            Put("ViTableRoundSet", 5, 15);
            Put("ViChair", 6, 15, 3);
            Put("ViRug", 3, 17);
            Hang("ViWallLamp", N, 12, 6, 2.2f);
            Hang("ViPicture8", S, 24, 6, 1.6f);

            // The back landing: a chair by a small table.
            Put("ViTableRoundSet", 10, 21);
            Put("TrArmchair", 10, 22, 2);
            Hang("ViWallLamp", W, 12, 21, 2.2f);

            // Let room 2, over the stair hall.
            Against("ViBed", E, 12, 19);
            Against("TrNightstand", E, 12, 21);
            Against("ViBasinSet", S, 24, 13).name = "Wash stand";
            Against("ViCupboardGlass", W, 21, 22);
            Put("ViTableRoundSet", 16, 21);
            Put("ViChair", 17, 21, 3);
            Hang("ViWallLamp", N, 18, 12, 2.2f);
            Hang("ViPicture1", N, 18, 15, 1.6f);
            _floorY = 0f;
            SootAndBellTripo();
        }

        // ------------------------------------------------------------------------------------------------ Tripo

        // The owner's Tripo pieces added to the seven (owner, 2026-11-09; furnishing rules §10), called by Furnish for a
        // rebuild and by EnrichWithTripo for the saved scenes, so both put the same pieces in the same cells. The keys
        // swapped in place are SwapsFor's; Furnish names the new keys directly. A log basket stands beside every hearth,
        // fire irons beside a hearth of a middling room and up (rules §8).

        private const string TaproomChandelier = "TrChandelier";

        private static void SootAndBellTripo()
        {
            // The piano on the stage, its back on the end wall at the stage's north end, out of the performer's middle.
            Put("TrPiano", 21 - _kit["TrPiano"].Depth, 7, 3, 0.4f);
            Put("TrLogHolder", 0, 5); // the saloon hearth: under the window's sill, the irons on the other side
            Put("TrFireTools", 0, 9);
            Put("TrLogHolder", 0, 18); // the kitchen hearth
        }

        private static void FactionHouseTripo()
        {
            foreach (var x in new[] { 0, 20 })
            {
                Put("TrLogHolder", x, 6);
                Put("TrFireTools", x, 2);
            }
        }

        private static void LockupTripo()
        {
            Put("TrLogHolder", 18, 17);
            Put("TrFireTools", 14, 17);
        }

        private static void MissingRoomTripo() => Put("TrLogHolder", 0, 5); // a poor room: the basket, no irons

        private static void AddTripo(string id)
        {
            switch (id)
            {
                case VeilmouthScene.SootAndBellId:
                    SootAndBellTripo();
                    break;
                case "faction-house":
                    FactionHouseTripo();
                    break;
                case "wardens-lockup":
                    LockupTripo();
                    break;
                case "missing-room":
                    MissingRoomTripo();
                    break;
            }
        }

        /// <summary>What a saved scene swaps (the old key becomes the new one) and takes out, by the owner's word.</summary>
        private static ((string From, string To)[] Swaps, (string Key, RectInt Where)[] Out) SwapsFor(string id)
        {
            var anywhere = new RectInt(-100, -100, 200, 200);
            switch (id)
            {
                case VeilmouthScene.SootAndBellId:
                    return (new[] { ("ViChandelier", TaproomChandelier), ("ViArmchair", "TrArmchair"), ("ViNightstand", "TrNightstand"), ("ViDresser", "TrDresser") },
                        new[] { ("Barrels", new RectInt(9, 12, 12, 12)), ("Crate", new RectInt(9, 12, 12, 12)), ("Box", new RectInt(9, 12, 12, 12)) });
                case "harrow-apothecary":
                    return (new (string, string)[0], new[] { ("Barrel", anywhere) });
                case "faction-house":
                    return (new[] { ("ViDesk", "TrDesk"), ("ViArmchair", "TrArmchair") }, new (string, RectInt)[0]);
                case "wardens-lockup":
                    return (new[] { ("ViDesk", "TrDesk") }, new (string, RectInt)[0]);
                case "soot-stair-cellar":
                    return (new[] { ("Firewood", "TrLogHolder") }, new (string, RectInt)[0]);
                default:
                    return (new (string, string)[0], new (string, RectInt)[0]);
            }
        }

        /// <summary>
        /// Batch only (owner, 2026-11-09): brings the seven saved interiors to the Tripo furnishing in place, so what S2
        /// wrote into them stays. A piece something in the scene refers to is left as it is and reported, but for the
        /// room's lamp list, which is written again from the lights the furniture holds. It can run again: what is done
        /// is found done, and an added piece already standing in its cell is not added twice.
        /// </summary>
        public static void EnrichWithTripo()
        {
            _kit = SliceKitCatalogue.Load();
            foreach (var (id, path) in All)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
                _parent = root != null ? root.transform.Find("Furniture") : null;
                if (_parent == null)
                {
                    Debug.LogWarning($"[VeilmouthInteriors] {path} has no {RootName}/Furniture; not enriched.");
                    continue;
                }

                _plan = PlanFor(id);
                _floorY = 0f;
                var refs = SceneReferences(scene);
                var done = new List<string>();
                var (swaps, outs) = SwapsFor(id);
                foreach (var (from, to) in swaps)
                {
                    done.Add($"{Swap(from, to, refs)} {from} to {to}");
                }

                foreach (var (key, where) in outs)
                {
                    var gone = 0;
                    foreach (var block in Instances(key).Where(g => where.Contains(new Vector2Int(Mathf.FloorToInt(g.transform.position.x), Mathf.FloorToInt(g.transform.position.z)))))
                    {
                        if (Kept(block, refs))
                        {
                            continue;
                        }

                        Object.DestroyImmediate(block);
                        gone++;
                    }

                    done.Add($"{gone} {key} out");
                }

                var standing = new HashSet<(string, Vector3)>(AddedKeys.SelectMany(k => Instances(k).Select(g => (k, g.transform.position))));
                var before = new HashSet<Transform>(_parent.Cast<Transform>());
                AddTripo(id);
                var added = 0;
                foreach (var t in _parent.Cast<Transform>().Where(t => !before.Contains(t)).ToList())
                {
                    var key = AddedKeys.FirstOrDefault(k => PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject) == _kit[k].Prefab);
                    if (key != null && standing.Contains((key, t.position)))
                    {
                        Object.DestroyImmediate(t.gameObject);
                        continue;
                    }

                    added++;
                }

                done.Add($"{added} added");
                done.Add($"{RelinkLamps(root.GetComponentInChildren<InteriorLighting>())} lamps");
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Report(path, "enriched: " + string.Join(", ", done), Doorways(_plan));
            }
        }

        /// <summary>
        /// Batch: the Tripo wrappers (built by Load where missing), the seven enriched, the Dwellings' kit written again,
        /// and their pictures. -executeMethod GameDev.Slice.EditorTools.VeilmouthInteriors.EnrichAndRender
        /// </summary>
        public static void EnrichAndRender()
        {
            // The wrappers whose Tripo lamps gained a light (2026-11-09), written again in place.
            SliceKitCatalogue.LoadRebuilding("TrDesk", "TrNightstandLamp", "TrSconce", "TrLantern");
            EnrichWithTripo();
            VeilmouthDwellings.BuildKit(SliceKitCatalogue.Load());
            VeilmouthRenders.RenderInteriors();
            VeilmouthRenders.RenderDwellings();
        }

        /// <summary>The keys AddTripo places, so a second run can find what the first added.</summary>
        private static readonly string[] AddedKeys = { "TrPiano", "TrLogHolder", "TrFireTools" };

        /// <summary>
        /// The room's lamp list written again from the lights the furniture holds, as BuildOne writes it: every light but a
        /// hearth's, a new one set as BuildOne sets them, and the lamps shown at the scene's preview hour.
        /// </summary>
        private static int RelinkLamps(InteriorLighting lighting)
        {
            var lamps = _parent.GetComponentsInChildren<Light>(true).Where(l => l.name != FireLight).ToArray();
            foreach (var lamp in lamps)
            {
                lamp.intensity = Mathf.Max(lamp.intensity, LampIntensity);
                lamp.range = lamp.name == ChandelierLight ? lamp.range : Mathf.Clamp(lamp.range, 6f, 9f);
                lamp.shadows = LightShadows.None;
                PrefabUtility.RecordPrefabInstancePropertyModifications(lamp);
            }

            var so = new SerializedObject(lighting);
            var list = so.FindProperty("lamps");
            list.arraySize = lamps.Length;
            for (var i = 0; i < lamps.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = lamps[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            lighting.Apply(lighting.PreviewMinute);
            return lamps.Length;
        }

        /// <summary>The instances of a wrapper among the furniture.</summary>
        private static List<GameObject> Instances(string key)
        {
            var prefab = _kit[key].Prefab;
            return _parent.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject)
                .Where(g => PrefabUtility.IsOutermostPrefabInstanceRoot(g) && PrefabUtility.GetCorrespondingObjectFromSource(g) == prefab).ToList();
        }

        /// <summary>
        /// Every object reference a component in the scene holds, but a Transform's and the room's lamp list, which
        /// RelinkLamps writes again.
        /// </summary>
        private static List<(Component From, Object To)> SceneReferences(UnityEngine.SceneManagement.Scene scene)
        {
            var refs = new List<(Component, Object)>();
            foreach (var c in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true)))
            {
                if (c == null || c is Transform)
                {
                    continue;
                }

                var p = new SerializedObject(c).GetIterator();
                while (p.Next(true))
                {
                    if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue != null &&
                        !(c is InteriorLighting && p.propertyPath.StartsWith("lamps")))
                    {
                        refs.Add((c, p.objectReferenceValue));
                    }
                }
            }

            return refs;
        }

        /// <summary>Children added in the scene to a placed piece (a chandelier's light): they move to its replacement.</summary>
        private static List<Transform> Added(GameObject block) =>
            block.transform.Cast<Transform>().Where(c => PrefabUtility.IsAddedGameObjectOverride(c.gameObject)).ToList();

        /// <summary>True, with a warning, when something outside the piece (and its added children) refers into it.</summary>
        private static bool Kept(GameObject block, List<(Component From, Object To)> refs)
        {
            var moving = new HashSet<Transform>(Added(block).SelectMany(c => c.GetComponentsInChildren<Transform>(true)));
            var inside = new HashSet<Object>();
            foreach (var t in block.GetComponentsInChildren<Transform>(true).Where(t => !moving.Contains(t)))
            {
                inside.Add(t.gameObject);
                foreach (var c in t.GetComponents<Component>())
                {
                    inside.Add(c);
                }
            }

            var from = refs.FirstOrDefault(r => inside.Contains(r.To) && !inside.Contains(r.From)).From;
            if (from == null)
            {
                return false;
            }

            Debug.LogWarning($"[VeilmouthInteriors] '{block.name}' at {block.transform.position} is kept: {from.GetType().Name} on '{from.name}' refers to it.");
            return true;
        }

        /// <summary>
        /// Each <paramref name="from"/> becomes a <paramref name="to"/> with its back on the same line and its middle where it
        /// was, as Against and Put place them; its added children move across, and a name given in the scene stays.
        /// </summary>
        private static int Swap(string from, string to, List<(Component From, Object To)> refs)
        {
            var o = _kit[from];
            var e = _kit[to];
            var n = 0;
            foreach (var old in Instances(from))
            {
                if (Kept(old, refs))
                {
                    continue;
                }

                var t = old.transform;
                var go = At(to, t.position + (t.rotation * new Vector3((o.Width - e.Width) * 0.5f, 0f, 0f)), t.rotation);
                go.transform.SetSiblingIndex(t.GetSiblingIndex());
                if (old.name != o.Prefab.name)
                {
                    go.name = old.name;
                }

                foreach (var child in Added(old))
                {
                    child.SetParent(go.transform, false);
                }

                Object.DestroyImmediate(old);
                n++;
            }

            return n;
        }

        private static void Harrow()
        {
            // Shop, by the rules' shop (§6): the counter across the room before the street door, the shop shelves
            // centred behind it with Harrow between, the laboratory door at the back wall's end beside the counter,
            // glazed cupboards on the side wall, BK's bottle shelves (themed, plan §6) on the back wall's free segment.
            Put("ViCounterSet", 3, 4, 2).name = "Counter";
            Put("ViCounterSet", 6, 4, 2).name = "Counter";
            Against("ViShopShelves", S, 9, 4);
            Against("ViShopShelves", S, 9, 6);
            Hang("BkBottleShelf", S, 9, 0, 1.4f);
            Hang("BkBottleShelf", S, 9, 0, 2.1f);

            Against("ViCupboardGlass", E, 0, 6);
            Against("ViShelves", W, 12, 1);
            Against("ViClock", W, 12, 6);
            Put("ViRug", 4, 1);
            Put("ViCoatStand", 1, 1);
            Hang("ViWallLamp", N, 0, 6, 2.2f);
            Hang("ViWallLamp", W, 12, 3, 2.2f);
            Hang("ViPicture4", N, 0, 0, 1.7f);
            Hang("ViPicture1", N, 0, 6, 1.7f);
            VeilmouthDressing.Person("Green", false, 6f, 6.7f, 180f, "Harrow", 0f);

            // Laboratory (themed): bench, alembic, cauldron and work table; a desk and books.
            Against("BkLabBench", S, 18, 1);
            Against("BkAlembic", S, 18, 9);
            Put("BkCauldron", 7, 13);
            Put("BkWorkTable", 4, 12);
            Against("ViDesk", E, 0, 13);
            Put("ViChair", 2, 14, 3);
            Hang("BkJarShelf", W, 12, 15, 1.5f);
            Against("ViBookcaseLow", N, 9, 3);
            Put("BkGlobe", 0, 17); // in the corner beside the desk, out of the store room's doorway (VI-8 remark 8)
            Hang("ViWallLamp", W, 12, 9, 2.2f);

            // Store room: a crate and a basket, no barrels (owner, 2026-11-09).
            // Only the middle cell is clear of both walls' dados.
            Put("Crate", 13, 12);
            Put("BkBasket", 13, 10);
        }

        private static void FactionHouse()
        {
            // Hall, symmetric about the street door's axis (rules §6): the rostrum on the north wall over the clear
            // 3 m aisle, three rows of pews either side, candle stands, a hearth on each side wall, wall lamps in pairs:
            // a chandelier hangs only under a ceiling higher than a storey (VI-8 remark 7).
            var rostrum = _kit["Rostrum"].Width;
            Against("Rostrum", S, 12, (21 - rostrum) / 2);
            foreach (var z in new[] { 3, 5, 7 })
            {
                Put("Pew", 4, z);
                Put("Pew", 13, z);
            }

            Put("Candles", 6, 9);
            Put("Candles", 14, 9);
            Fireplace(E, 0, 3);
            Fireplace(W, 21, 3);
            Hang("ViWallLamp", N, 0, 0, 2.2f);
            Hang("ViWallLamp", N, 0, 18, 2.2f);
            Hang("ViWallLamp", S, 12, 6, 2.2f);
            Hang("ViWallLamp", S, 12, 12, 2.2f);
            Hang("ViWallLamp", E, 0, 9, 2.2f);
            Hang("ViWallLamp", W, 21, 9, 2.2f);
            Hang("ViPicture1", E, 0, 0, 1.7f);
            Hang("ViPicture5", W, 21, 0, 1.7f);
            FactionHouseTripo();
            VeilmouthDressing.Person("Teal", false, 10.5f, 9.2f, 180f, "Faction member", 0f);

            // Office: the Tripo writing desk under the window, a bookcase, wing chairs by a round table.
            Against("TrDesk", S, 21, 2);
            Put("ViChair", 3, 18);
            Against("ViBookcase", E, 0, 15);
            Put("TrArmchair", 6, 14, 3);
            Put("TrArmchair", 6, 16, 3);
            Put("ViTableRound", 5, 15);
            Put("ViRug", 3, 14);
            Against("ViClock", N, 12, 7);
            Hang("ViPicture6", E, 0, 12, 1.7f);
            Hang("ViWallLamp", E, 0, 18, 2.2f);
            VeilmouthDressing.Person("Teal", true, 3.5f, 16.5f, 0f, "Faction member", 0f);

            // Archive: bookcases full of books on three walls, never in a corner, and a reading table.
            Against("ViBookcase", S, 21, 12);
            Against("ViBookcase", S, 21, 15);
            Against("ViBookcase", W, 21, 15);
            Against("ViBookcase", E, 9, 15);
            Against("ViShelves", N, 12, 18);
            Put("ViTable", 13, 16);
            Put("ViChair", 13, 15);
            Put("ViChair", 14, 17, 2);
            Put("ViOilLamp", 14, 16, 0, 1.0f);
            Hang("ViWallLamp", N, 12, 9, 2.2f);
        }

        private static void Lockup()
        {
            // Front desk room.
            // The front desk faces the street door across the room (rules §6), the Warden behind it, the waiting chairs
            // by the window facing it.
            Put("ViCounter", 3, 4, 2).name = "Front desk";
            Put("ViChair", 9, 1);
            Put("ViChair", 10, 1);
            Hang("BkBoard", W, 12, 6, 1.0f).name = "Notices";
            Against("ViCabinet", S, 9, 1);
            Hang("ViPicture7", N, 0, 0, 1.7f);
            Hang("ViWallLamp", E, 0, 0, 2.2f);
            VeilmouthDressing.Person("Blue", false, 4.5f, 6.7f, 180f, "Warden", 0f);

            // Office: the Tripo writing desk and a clock.
            Against("TrDesk", S, 9, 15);
            Put("ViChair", 15, 6);
            Against("ViClock", E, 12, 7);
            Against("ViCupboardGlass", N, 0, 18);
            Put("ViRug", 15, 2);
            Hang("ViPicture3", W, 21, 6, 1.7f);
            Hang("ViWallLamp", N, 0, 12, 2.2f);

            // Corridor and the cells (themed, stone).
            Hang("ViWallLamp", N, 9, 9, 2.2f);
            Put("ViBucket", 1, 16);
            Put("ViBox", 4, 16);
            Put("ViBucket", 7, 16);

            // Guard room: the hearth, a table with chairs, the weapons table (themed).
            Fireplace(S, 18, 15);
            Put("ViKitchenTable", 14, 12);
            Put("ViChair", 14, 11);
            Put("ViChair", 16, 11);
            Put("ViChair", 15, 14, 2);
            Against("BkArmsTable", W, 21, 15);
            Against("ViCupboardGlass", E, 12, 15);
            Hang("ViWallLamp", E, 12, 12, 2.2f);
            LockupTripo();
            VeilmouthDressing.Person("Blue", true, 18.3f, 15.5f, 90f, "Warden", 0f);
        }

        private static void MissingRoom()
        {
            // One poor room: bed, hearth, table and chairs, a wash stand, the cache.
            Against("ViBed", S, 12, 6);
            Fireplace(E, 0, 6);
            Put("ViTableB", 3, 6);
            Put("ViChair", 3, 5);
            Put("ViChair", 4, 7, 2);
            Put("ViCandle", 4, 6, 0, 0.9f);
            Against("ViBasinSet", W, 9, 4).name = "Wash stand";
            Put("ViBucket", 8, 5);
            Put("ViBox", 5, 11).name = "The cache";
            Hang("ViPicture8", N, 3, 3, 1.7f);
            MissingRoomTripo();
        }

        private static void Cellar()
        {
            // Three stone vaults (themed), a store: barrels, boxes, the Tripo log basket; candles burn always, for no
            // window opens here.
            Put("Barrels", 1, 6);
            Put("Barrel", 6, 6);
            Put("Barrel", 7, 6);
            Put("Box", 6, 1);
            Put("TrLogHolder", 4, 1);
            Put("Candles", 7, 3);
            Put("Barrels", 23, 6);
            Put("Box", 27, 1);
            Put("BkLogs", 27, 6);
            Put("Crate", 22, 1);
            Put("Candles", 28, 3);
            Put("Candles", 10, 9);
            Put("Candles", 19, 9);
            VeilmouthDressing.Circle(new Vector3(15f, 0f, 6f), "Candidate 2, the cellar under the Soot Stair");
        }

        private static void Chapel()
        {
            // Nave: the rostrum on the north wall over the aisle, five rows of pews, candle stands (themed).
            var rostrum = _kit["Rostrum"].Width;
            Against("Rostrum", S, 21, (15 - rostrum) / 2);
            foreach (var z in new[] { 4, 7, 10, 13, 16 })
            {
                Put("Pew", 2, z);
                Put("Pew", 9, z);
            }

            Put("Candles", 4, 18);
            Put("Candles", 10, 18);

            // Vestry.
            Against("ViCabinet", W, 18, 13);
            Put("ViCandle", 17, 13, 0, 1.2f);
            Against("ViShelfSmall", W, 18, 18);
            Put("ViChair", 16, 19); // north of the vestry door's way (doorway check, VI-8)
        }

        /// <summary>
        /// A stair's solid bounds are its whole slope, 7 × 3 m and 4.7 m high, so the floor over its foot and the
        /// balustrade at its well's end stand inside them though they touch nothing of it. They are nested under the stair
        /// as its own pieces (kit standard §6), which the kit check reads with it, not against it (VI-3).
        /// </summary>
        private static void NestOnStairs(Transform root)
        {
            Physics.SyncTransforms();
            Bounds? Solid(Component block)
            {
                Bounds? b = null;
                foreach (var c in block.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger && c.GetComponentInParent<KitBlock>() == block))
                {
                    if (b.HasValue) { var u = b.Value; u.Encapsulate(c.bounds); b = u; }
                    else b = c.bounds;
                }

                return b;
            }

            var blocks = root.GetComponentsInChildren<KitBlock>();
            foreach (var stair in blocks.Where(k => k.name == "Stair"))
            {
                var bounds = Solid(stair);
                if (!bounds.HasValue)
                {
                    continue;
                }

                var inside = bounds.Value;
                inside.Expand(-0.02f);
                foreach (var piece in blocks.Where(k => k.name.StartsWith("Kit_ViRoom_Floor") || k.name == "Balustrade" || k.name == "Newel"))
                {
                    var other = Solid(piece);
                    if (other.HasValue && inside.Intersects(other.Value))
                    {
                        piece.transform.SetParent(stair.transform, true);
                    }
                }
            }
        }

        /// <summary>A two-cell table with two seats on either long side, facing it: chairs, armchairs or stools.</summary>
        private static void Table(int x, int z, string seat = "ViChair")
        {
            Put("ViTable", x, z);
            Put(seat, x, z - 1);
            Put(seat, x + 1, z - 1);
            Put(seat, x, z + 1, 2);
            Put(seat, x + 1, z + 1, 2);
        }

        /// <summary>
        /// A small stage (rules §3, §6): one box of our own, 0.4 m high, <paramref name="w"/> × <paramref name="d"/>
        /// modules from (x, z), with the pack's parquet on top and a darker parquet on its sides, its UVs in metres so the
        /// parquet keeps the floor's scale. The pack has no stage, and a piece is not made of other pieces (owner, VI-8
        /// remark 5); a finished stage with an edge and steps waits in SLICE_MODELS_WANTED. Not a kit block: the kit check
        /// does not read it, and its box collider is the floor the Combat Grid reads.
        /// </summary>
        private static void Stage(int x, int z, int w, int d)
        {
            const float height = 0.4f;
            var size = new Vector3(w * Module, height, d * Module);
            var stage = new GameObject("Stage");
            stage.transform.SetParent(_parent, false);
            stage.transform.position = new Vector3(x, _floorY, z);
            var box = stage.AddComponent<BoxCollider>();
            box.center = size * 0.5f;
            box.size = size;
            stage.AddComponent<MeshFilter>().sharedMesh = StageMesh(size);
            var renderer = stage.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { StageMaterial("ViStageTop", 1f), StageMaterial("ViStageSide", 0.55f) };
        }

        private static Mesh StageMesh(Vector3 size)
        {
            var path = $"{ViFrieze.Folder}/ViStage_{size.x:0}x{size.z:0}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null)
            {
                return mesh;
            }

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var top = new List<int>();
            var sides = new List<int>();
            void Face(Vector3 o, Vector3 u, Vector3 v, Vector3 n, List<int> into)
            {
                // UVs in metres over the floor tile's 3 m, so the parquet keeps its scale on every face.
                var i = vertices.Count;
                foreach (var (a, b) in new[] { (0f, 0f), (1f, 0f), (1f, 1f), (0f, 1f) })
                {
                    vertices.Add(o + (u * a) + (v * b));
                    normals.Add(n);
                    uvs.Add(new Vector2(a * u.magnitude, b * v.magnitude) / Module);
                }

                into.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
            }

            float sx = size.x, sy = size.y, sz = size.z;
            Face(new Vector3(0f, sy, 0f), Vector3.right * sx, Vector3.forward * sz, Vector3.up, top);
            Face(Vector3.zero, Vector3.right * sx, Vector3.up * sy, Vector3.back, sides);
            Face(new Vector3(sx, 0f, sz), Vector3.left * sx, Vector3.up * sy, Vector3.forward, sides);
            Face(new Vector3(0f, 0f, sz), Vector3.back * sz, Vector3.up * sy, Vector3.left, sides);
            Face(new Vector3(sx, 0f, 0f), Vector3.forward * sz, Vector3.up * sy, Vector3.right, sides);
            mesh = new Mesh { name = "ViStage" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(top, 0);
            mesh.SetTriangles(sides, 1);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            KitStandIns.EnsureFolder(ViFrieze.Folder);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        /// <summary>Our copy of the pack's parquet, <paramref name="shade"/> times as bright (authoring default).</summary>
        private static Material StageMaterial(string name, float shade)
        {
            var path = $"{ViFrieze.Folder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Victorian Interiors/Victorian Interiors/Materials/Floor.mat"));
                material.SetColor("_BaseColor", new Color(shade, shade, shade, 1f));
                KitStandIns.EnsureFolder(ViFrieze.Folder);
                AssetDatabase.CreateAsset(material, path);
            }

            return material;
        }

        /// <summary>
        /// The pack's mantel against a wall, with the fire in its firebox (<see cref="Dwellings.DwellingAssembler.PlaceFire"/>)
        /// and a warm flickering light, both lit and put out together; a kitchen hearth burns all day.
        /// </summary>
        private static void Fireplace(Vector2Int facing, int line, int a, bool kitchen = false)
        {
            var block = Against("ViFireplace", facing, line, a);
            block.name = kitchen ? "Kitchen hearth" : "Fireplace";
            var flames = Dwellings.DwellingAssembler.PlaceFire(AssetDatabase.LoadAssetAtPath<GameObject>(FirePath), block.transform);
            var light = new GameObject(FireLight).AddComponent<Light>();
            light.transform.SetParent(block.transform, false);
            light.transform.localPosition = new Vector3(Module * 0.5f, 0.6f, 1.1f);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.55f, 0.25f);
            light.range = 7f;
            light.intensity = 2.5f;
            light.shadows = LightShadows.None;
            Hearths.Add(new InteriorLighting.Hearth { Light = light, Flames = flames, AllDay = kitchen });
        }

        /// <summary>A chandelier under the ceiling; it is only emissive, so it carries a warm light of our own.</summary>
        private static void Chandelier(int x, int z, float raised = 0f, string key = "ViChandelier")
        {
            // Under the ceiling; a tall room's ceiling is <paramref name="raised"/> higher (the taproom, 7 m, VI-7).
            var block = Put(key, x, z, 0, 3.3f + raised);
            var light = new GameObject(ChandelierLight).AddComponent<Light>();
            light.transform.SetParent(block.transform, false);
            light.transform.localPosition = new Vector3(1f, -0.9f, 1f);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.8f, 0.55f);
            // Authoring default: a high chandelier reaches further to light the same floor.
            light.range = 9f + raised;
            light.intensity = 3f + raised * 0.6f;
            light.shadows = LightShadows.None;
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

        /// <summary>A hung block on the wall line, facing into the room, on the 3 m segment that holds <paramref name="a"/>.</summary>
        private static GameObject Hang(string key, Vector2Int facing, int line, int a, float y)
        {
            var e = _kit[key];
            var turns = Turns(facing);
            a = a / Module * Module + (Module - e.Width) / 2;
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
                new Vector3(Mathf.Round(pivot.x), Mathf.Round((pivot.y + _floorY) * 10f) / 10f, Mathf.Round(pivot.z)), rotation);
            return go;
        }

        private static Transform Group(Transform root, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(root, false);
            return t;
        }

        private static void Report(string path, string what, List<string> doorways)
        {
            var blocks = Object.FindObjectsByType<KitBlock>(FindObjectsInactive.Exclude);
            var faults = KitLayoutCheck.Check(blocks);
            Debug.Log($"[VeilmouthInteriors] {path}: {blocks.Length} blocks, {what}, {faults.Count} kit faults" +
                      (faults.Count > 0 ? ":\n  " + string.Join("\n  ", faults.Take(40).Select(f => f.ToString())) : ".") +
                      $" {doorways.Count} doorway faults" + (doorways.Count > 0 ? ":\n  " + string.Join("\n  ", doorways) : "."));
        }

        /// <summary>
        /// The walk-way check (furnishing rules G1; owner, VI-8 remark 8): before every doorway, on both sides, the
        /// doorway's middle metre stays clear for two metres of anything solid but the room's own pieces, the stair and
        /// the people. It reports and never moves a piece.
        /// </summary>
        private static List<string> Doorways(Plan plan)
        {
            Physics.SyncTransforms();
            var faults = new List<string>();
            foreach (var kv in plan.Openings)
            {
                if (kv.Value is not (Opening.Door or Opening.Street or Opening.Yard or Opening.Closed or Opening.None))
                {
                    continue;
                }

                var (x, z, alongZ, level) = kv.Key;
                var mid = alongZ ? new Vector2(x, z + (Module * 0.5f)) : new Vector2(x + (Module * 0.5f), z);
                var across = alongZ ? Vector2.right : Vector2.up;
                foreach (var side in new[] { 1f, -1f })
                {
                    foreach (var depth in new[] { 0.5f, 1.5f })
                    {
                        var c = mid + (across * (side * depth));
                        if (!plan.Rooms.Any(r => r.Level == level && c.x > r.X && c.x < r.X + r.W && c.y > r.Z && c.y < r.Z + r.D))
                        {
                            continue;
                        }

                        var centre = new Vector3(c.x, (level * Storey) + 1.05f, c.y);
                        foreach (var hit in Physics.OverlapBox(centre, new Vector3(0.45f, 0.9f, 0.45f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                        {
                            var block = hit.GetComponentInParent<KitBlock>();
                            var name = block != null ? block.name : hit.name == "Stage" ? "Stage" : null;
                            if (name == null || name == "Stair" || (name.StartsWith("Kit_ViRoom_") && !name.StartsWith("Kit_ViRoom_Banister")) ||
                                name.StartsWith("Kit_ViStone_") || name.StartsWith("Portal") || name.StartsWith("Street door") || name.StartsWith("Yard door"))
                            {
                                continue;
                            }

                            var fault = $"'{name}' in the way of the doorway at ({x}, {z}{(alongZ ? ", along Z" : string.Empty)}, storey {level}), {depth + 0.5f:0} m in";
                            if (!faults.Contains(fault))
                            {
                                faults.Add(fault);
                            }
                        }
                    }
                }
            }

            return faults;
        }
    }
}

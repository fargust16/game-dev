using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Slice.Dwellings
{
    /// <summary>
    /// A kit block's footprint in whole cells along its local X and Z, whether it is solid (anything but NoGrid), and
    /// its solid body in the footprint's own metres: the pivot at the back-left corner, the front toward +Z.
    /// </summary>
    public readonly struct Footprint
    {
        public Footprint(int width, int depth, bool solid, Rect body)
        {
            Width = width;
            Depth = depth;
            Solid = solid;
            Body = body;
        }

        public int Width { get; }
        public int Depth { get; }
        public bool Solid { get; }
        public Rect Body { get; }
    }

    public interface IDwellingFootprints
    {
        bool TryGet(string key, out Footprint footprint);
    }

    /// <summary>
    /// Plans a dwelling from its key and its own seed stream (15, *The slice's Dwellings*; city plan C1 §8): the rooms
    /// of its archetype on the 4 m module, turned left or right by the seed, then the furniture its rooms and wealth
    /// call for, and the Stash by 19 §6.3's chances. The same key and seed always give the same plan. Every layout and
    /// list here is an authoring default.
    /// </summary>
    public static class DwellingPlanner
    {
        /// <summary>19 §6.3, authoring defaults checked by burglary_sim.py: a Stash in half the poor homes, 60 % and 70 %.</summary>
        public static double StashChance(DwellingWealth wealth) => wealth == DwellingWealth.Poor ? 0.5 : wealth == DwellingWealth.Middling ? 0.6 : 0.7;

        public const string StashKey = "BkTrunk";

        private const float PaintingHeight = 1.6f;

        /// <summary>
        /// Measured by the kit standard §2: a wall's body is 0.3 m centred on its line, and a post as thick. A solid
        /// body keeps clear of it by the layout check's tolerance, so a piece by a corner never runs into the side wall.
        /// </summary>
        private const float WallClearance = 0.15f + 0.01f;
        private const float ShelfHeight = 1.7f;

        private enum Spot
        {
            Wall,   // against a wall
            Middle, // in the room, clear of the walls
            Free,   // anywhere free
            Hang,   // on a wall's face
        }

        private readonly struct Item
        {
            public Item(string key, Spot spot, DwellingWealth from = DwellingWealth.Poor, int count = 1, float y = 0f)
            {
                Key = key;
                Spot = spot;
                From = from;
                Count = count;
                Y = y;
            }

            public string Key { get; }
            public Spot Spot { get; }
            public DwellingWealth From { get; }
            public int Count { get; }
            public float Y { get; }
        }

        private const DwellingWealth Poor = DwellingWealth.Poor, Middling = DwellingWealth.Middling, Rich = DwellingWealth.Rich;

        /// <summary>Each room kind's furniture, from the poorest household up; a richer home also has the later lines.</summary>
        private static readonly Dictionary<RoomKind, Item[]> Furnishings = new Dictionary<RoomKind, Item[]>
        {
            [RoomKind.Hall] = new[]
            {
                new Item("BkDrawer", Spot.Wall, Middling), new Item("BkPainting", Spot.Hang, Middling, y: PaintingHeight),
                new Item("BkCarpetLong", Spot.Middle, Rich),
            },
            [RoomKind.Parlour] = new[]
            {
                new Item("BkSideTable", Spot.Wall), new Item("BkChair", Spot.Free, count: 2),
                new Item("BkRoundTable", Spot.Middle, Middling), new Item("BkBookShelf", Spot.Hang, Middling, y: ShelfHeight),
                new Item("BkPainting", Spot.Hang, Middling, y: PaintingHeight), new Item("BkCupboardGlass", Spot.Wall, Rich),
                new Item("BkPainting", Spot.Hang, Rich, y: PaintingHeight),
            },
            [RoomKind.Kitchen] = new[]
            {
                new Item("BkStove", Spot.Wall), new Item("BkKitchenTable", Spot.Middle), new Item("BkStool", Spot.Free),
                new Item("BkLogs", Spot.Free), new Item("BkSink", Spot.Wall, Middling), new Item("BkCupboard", Spot.Wall, Middling),
                new Item("BkDishShelf", Spot.Hang, Middling, y: ShelfHeight), new Item("BkBasket", Spot.Free, Middling),
            },
            [RoomKind.Bedroom] = new[]
            {
                new Item("WallBed", Spot.Wall), new Item("BkDresser", Spot.Wall, Middling), new Item("BkChair", Spot.Free, Middling),
                new Item("BkPainting", Spot.Hang, Middling, y: PaintingHeight), new Item("BkCarpet", Spot.Middle, Rich),
            },
            [RoomKind.Room] = new[]
            {
                new Item("WallBed", Spot.Wall), new Item("BkStove", Spot.Wall), new Item("BkKitchenTable", Spot.Middle),
                new Item("BkChair", Spot.Free), new Item("BkBasket", Spot.Free), new Item("BkDrawer", Spot.Wall, Middling),
            },
            [RoomKind.ShopRoom] = new[]
            {
                new Item("BkCounter", Spot.Middle), new Item("BkBottleShelf", Spot.Hang, y: ShelfHeight), new Item("Crate", Spot.Free),
                new Item("BkJarShelf", Spot.Hang, Middling, y: ShelfHeight), new Item("BkCupboard", Spot.Wall, Middling),
                new Item("Barrel", Spot.Free, Middling),
            },
            [RoomKind.BackRoom] = new[]
            {
                new Item("Crate", Spot.Free, count: 2), new Item("Barrel", Spot.Free), new Item("BkSideTable", Spot.Wall),
                new Item("BkDrawer", Spot.Wall, Middling), new Item("BkBasketB", Spot.Free, Middling),
            },
            [RoomKind.DiningRoom] = new[]
            {
                new Item("BkDiningTable", Spot.Middle), new Item("BkChair", Spot.Free, count: 4), new Item("BkCupboardGlass", Spot.Wall),
                new Item("BkDishShelf", Spot.Hang, y: ShelfHeight), new Item("BkPainting", Spot.Hang, y: PaintingHeight),
                new Item("BkCarpetWide", Spot.Middle, Rich),
            },
            [RoomKind.Study] = new[]
            {
                new Item("BkDesk", Spot.Wall), new Item("BkChair", Spot.Free), new Item("BkBookShelfB", Spot.Hang, y: ShelfHeight),
                new Item("BkBookShelfC", Spot.Hang, y: ShelfHeight), new Item("BkGlobe", Spot.Free),
                new Item("BkCupboardTall", Spot.Wall), new Item("BkPainting", Spot.Hang, y: PaintingHeight),
            },
            [RoomKind.Store] = new[]
            {
                new Item("Crate", Spot.Free, count: 4), new Item("Barrel", Spot.Free, count: 3), new Item("BkBasketB", Spot.Free),
            },
            [RoomKind.Office] = new[]
            {
                new Item("BkDesk", Spot.Wall), new Item("BkChair", Spot.Free), new Item("BkDrawer", Spot.Wall),
                new Item("BkBoard", Spot.Hang, y: ShelfHeight),
            },
        };

        /// <summary>Every kit key the planner may place, so the kit asset holds them all.</summary>
        public static IEnumerable<string> Keys => Furnishings.Values.SelectMany(items => items.Select(i => i.Key)).Append(StashKey).Distinct();

        /// <summary>Where a household keeps its Stash, in order of preference.</summary>
        private static readonly RoomKind[] StashRooms =
            { RoomKind.Bedroom, RoomKind.Study, RoomKind.Room, RoomKind.BackRoom, RoomKind.Office, RoomKind.Store, RoomKind.Kitchen };

        private static readonly Vector2Int N = new Vector2Int(0, 1), S = new Vector2Int(0, -1), E = new Vector2Int(1, 0), W = new Vector2Int(-1, 0);

        public static int Turns(Vector2Int facing) => facing == N ? 0 : facing == E ? 1 : facing == S ? 2 : 3;

        public static DwellingPlan Plan(DwellingKey key, Random rng, IDwellingFootprints kit)
        {
            var plan = Layout(key.Archetype, rng);
            if (rng.Next(2) == 1)
            {
                plan = Mirror(plan);
            }

            Furnish(plan, key.Wealth, rng, kit);
            return plan;
        }

        // ------------------------------------------------------------------------------------------- layouts

        /// <summary>The rooms of city plan C1 §8, widened by a third to the 4 m module; the street door is on the front, z = 0.</summary>
        private static DwellingPlan Layout(DwellingArchetype archetype, Random rng)
        {
            switch (archetype)
            {
                case DwellingArchetype.Tenement:
                    // One flat: a room and a kitchen, or one room.
                    return rng.Next(2) == 0
                        ? new DwellingPlan()
                            .Room("Room", RoomKind.Room, 0, 0, 8, 8)
                            .Open(Opening.Street, 0, 0).Open(Opening.Window, 4, 8)
                        : new DwellingPlan()
                            .Room("Room", RoomKind.Room, 0, 0, 8, 8).Room("Kitchen", RoomKind.Kitchen, 8, 0, 4, 8, true)
                            .Open(Opening.Street, 0, 0).Open(Opening.Door, 8, 4, true).Open(Opening.Window, 4, 8).Open(Opening.Window, 8, 8);
                case DwellingArchetype.Terrace:
                    // A hall along the side, the parlour at the front, the kitchen behind it, the bedroom across the back.
                    return new DwellingPlan()
                        .Room("Hall", RoomKind.Hall, 0, 0, 4, 12).Room("Parlour", RoomKind.Parlour, 4, 0, 8, 8)
                        .Room("Kitchen", RoomKind.Kitchen, 4, 8, 8, 4, true).Room("Bedroom", RoomKind.Bedroom, 0, 12, 12, 4)
                        .Open(Opening.Street, 0, 0).Open(Opening.Window, 4, 0).Open(Opening.Window, 8, 0)
                        .Open(Opening.Door, 4, 4, true).Open(Opening.Door, 4, 8, true).Open(Opening.Door, 0, 12)
                        .Open(Opening.Window, 12, 8, true).Open(Opening.Window, 8, 16);
                case DwellingArchetype.Shop:
                    return new DwellingPlan()
                        .Room("Shop", RoomKind.ShopRoom, 0, 0, 12, 8).Room("Back room", RoomKind.BackRoom, 0, 8, 8, 8)
                        .Room("Kitchen", RoomKind.Kitchen, 8, 8, 4, 8, true)
                        .Open(Opening.Street, 4, 0).Open(Opening.Window, 0, 0).Open(Opening.Window, 8, 0)
                        .Open(Opening.Door, 4, 8).Open(Opening.Door, 8, 12, true).Open(Opening.Window, 0, 12, true).Open(Opening.Window, 8, 16);
                case DwellingArchetype.Villa:
                    // A hall up the middle between the parlour and the dining room, the study and the kitchen behind.
                    return new DwellingPlan()
                        .Room("Parlour", RoomKind.Parlour, 0, 0, 8, 8).Room("Hall", RoomKind.Hall, 8, 0, 4, 12)
                        .Room("Dining room", RoomKind.DiningRoom, 12, 0, 8, 8).Room("Study", RoomKind.Study, 0, 8, 8, 8)
                        .Room("Kitchen", RoomKind.Kitchen, 12, 8, 8, 8, true)
                        .Open(Opening.Street, 8, 0).Open(Opening.Window, 0, 0).Open(Opening.Window, 4, 0)
                        .Open(Opening.Window, 12, 0).Open(Opening.Window, 16, 0).Open(Opening.Window, 0, 4, true)
                        .Open(Opening.Window, 20, 4, true).Open(Opening.Window, 8, 12).Open(Opening.Window, 4, 16)
                        .Open(Opening.Window, 16, 16)
                        .Open(Opening.Door, 8, 4, true).Open(Opening.Door, 8, 8, true).Open(Opening.Door, 12, 4, true)
                        .Open(Opening.Door, 12, 8, true);
                case DwellingArchetype.Warehouse:
                    return new DwellingPlan()
                        .Room("Store", RoomKind.Store, 0, 0, 12, 12, true).Room("Office", RoomKind.Office, 12, 0, 8, 8)
                        .Open(Opening.Street, 4, 0).Open(Opening.Door, 12, 4, true).Open(Opening.Window, 16, 0);
                default:
                    throw new ArgumentOutOfRangeException(nameof(archetype), archetype, null);
            }
        }

        /// <summary>The same plan turned left for right about its middle, so neighbours do not all repeat one hand.</summary>
        private static DwellingPlan Mirror(DwellingPlan plan)
        {
            var width = plan.Width;
            var mirrored = new DwellingPlan();
            foreach (var r in plan.Rooms)
            {
                mirrored.Room(r.Name, r.Kind, width - r.X - r.Width, r.Z, r.Width, r.Depth, r.Stone);
            }

            foreach (var pair in plan.Openings)
            {
                var (x, z, alongZ) = pair.Key;
                mirrored.Open(pair.Value, alongZ ? width - x : width - x - DwellingPlan.Module, z, alongZ);
            }

            return mirrored;
        }

        // ------------------------------------------------------------------------------------------- furniture

        private sealed class Room
        {
            public DwellingRoom Data;
            public readonly HashSet<(int, int)> Taken = new HashSet<(int, int)>();
            public readonly HashSet<(int, int, bool)> UsedWalls = new HashSet<(int, int, bool)>();
            public readonly List<(int X, int Z, bool AlongZ, Vector2Int Facing, int Line)> Walls = new List<(int, int, bool, Vector2Int, int)>();
        }

        private static void Furnish(DwellingPlan plan, DwellingWealth wealth, Random rng, IDwellingFootprints kit)
        {
            var rooms = plan.Rooms.Select(r => Prepare(plan, r)).ToList();
            var stashRoom = Array.FindIndex(StashRooms, k => rooms.Any(r => r.Data.Kind == k));
            var stash = rng.NextDouble() < StashChance(wealth) && stashRoom >= 0;

            foreach (var room in rooms)
            {
                // Against the walls first, then what stands in the middle, then what hangs, then the loose pieces round them.
                foreach (var item in Furnishings[room.Data.Kind].Where(i => i.From <= wealth).OrderBy(i => (int)i.Spot))
                {
                    for (var n = 0; n < item.Count; n++)
                    {
                        if (!Place(plan, room, item, rng, kit, false))
                        {
                            plan.Unplaced.Add($"{room.Data.Name}: {item.Key}");
                        }
                    }
                }

                if (stash && room.Data.Kind == StashRooms[stashRoom])
                {
                    stash = !Place(plan, room, new Item(StashKey, Spot.Free), rng, kit, true);
                }
            }
        }

        /// <summary>The room's own wall segments, and the cells kept clear inside each doorway.</summary>
        private static Room Prepare(DwellingPlan plan, DwellingRoom r)
        {
            var room = new Room { Data = r };
            const int m = DwellingPlan.Module;
            for (var x = r.X; x < r.X + r.Width; x += m)
            {
                room.Walls.Add((x, r.Z, false, N, r.Z));
                room.Walls.Add((x, r.Z + r.Depth, false, S, r.Z + r.Depth));
            }

            for (var z = r.Z; z < r.Z + r.Depth; z += m)
            {
                room.Walls.Add((r.X, z, true, E, r.X));
                room.Walls.Add((r.X + r.Width, z, true, W, r.X + r.Width));
            }

            foreach (var wall in room.Walls)
            {
                var kind = plan.OpeningAt(wall.X, wall.Z, wall.AlongZ);
                if (kind != Opening.Door && kind != Opening.Street)
                {
                    continue;
                }

                // A doorway keeps the cells along its segment clear, and behind them the two it opens on: BK's doorway
                // is 1.76 m wide over the segment's middle node (4 m module, 2026-11-06).
                for (var i = 0; i < m; i++)
                {
                    room.Taken.Add(Cell(wall, i, 0));
                }

                room.Taken.Add(Cell(wall, (m / 2) - 1, 1));
                room.Taken.Add(Cell(wall, m / 2, 1));
            }

            return room;
        }

        /// <summary>The cell <paramref name="along"/> cells along a wall segment and <paramref name="deep"/> cells into the room.</summary>
        private static (int, int) Cell((int X, int Z, bool AlongZ, Vector2Int Facing, int Line) wall, int along, int deep)
        {
            if (!wall.AlongZ)
            {
                return (wall.X + along, wall.Facing == N ? wall.Line + deep : wall.Line - 1 - deep);
            }

            return (wall.Facing == E ? wall.Line + deep : wall.Line - 1 - deep, wall.Z + along);
        }

        private static bool Place(DwellingPlan plan, Room room, Item item, Random rng, IDwellingFootprints kit, bool stash)
        {
            if (!kit.TryGet(item.Key, out var fp))
            {
                return false;
            }

            var r = room.Data;
            var width = fp.Width;
            var depth = Mathf.Max(fp.Depth, 1);
            var solid = fp.Solid;
            switch (item.Spot)
            {
                case Spot.Wall:
                case Spot.Hang:
                {
                    var hang = item.Spot == Spot.Hang;
                    if (width > DwellingPlan.Module)
                    {
                        return false;
                    }

                    foreach (var wall in Shuffled(room.Walls, rng))
                    {
                        if (plan.OpeningAt(wall.X, wall.Z, wall.AlongZ) != Opening.Wall || room.UsedWalls.Contains((wall.X, wall.Z, wall.AlongZ)))
                        {
                            continue;
                        }

                        var start = wall.AlongZ ? wall.Z : wall.X;
                        if (hang)
                        {
                            var at = start + ((DwellingPlan.Module - width) / 2);
                            room.UsedWalls.Add((wall.X, wall.Z, wall.AlongZ));
                            plan.Furniture.Add(new Placement(item.Key, PlacementMode.Hang, 0, 0, Turns(wall.Facing), wall.Facing, wall.Line, at, item.Y, stash));
                            return true;
                        }

                        foreach (var offset in Shuffled(Enumerable.Range(0, DwellingPlan.Module - width + 1).ToList(), rng))
                        {
                            var a = start + offset;
                            var (x, z) = AgainstCorner(wall.Facing, wall.Line, a, width, depth);
                            var turns = Turns(wall.Facing);
                            var cells = Cells(x, z, turns % 2 == 0 ? width : depth, turns % 2 == 0 ? depth : width).ToList();
                            if (!Fits(room, cells) || (solid && !Clear(r, fp, x, z, turns)))
                            {
                                continue;
                            }

                            Take(room, cells, solid);
                            room.UsedWalls.Add((wall.X, wall.Z, wall.AlongZ));
                            plan.Furniture.Add(new Placement(item.Key, PlacementMode.Against, x, z, turns, wall.Facing, wall.Line, a, 0f, stash));
                            return true;
                        }
                    }

                    return false;
                }

                default:
                {
                    var middle = item.Spot == Spot.Middle;
                    var turns = middle ? (r.Width >= r.Depth ? 0 : 1) : rng.Next(4);
                    var w = turns % 2 == 0 ? width : depth;
                    var d = turns % 2 == 0 ? depth : width;
                    var inset = middle && r.Width - w >= 2 && r.Depth - d >= 2 ? 1 : 0;
                    var spots = new List<(int X, int Z)>();
                    for (var x = r.X + inset; x + w <= r.X + r.Width - inset; x++)
                    {
                        for (var z = r.Z + inset; z + d <= r.Z + r.Depth - inset; z++)
                        {
                            spots.Add((x, z));
                        }
                    }

                    var centre = new Vector2(r.X + (r.Width - w) / 2f, r.Z + (r.Depth - d) / 2f);
                    var ordered = middle
                        ? spots.OrderBy(s => (new Vector2(s.X, s.Z) - centre).sqrMagnitude).ThenBy(_ => rng.Next()).ToList()
                        : Shuffled(spots, rng);
                    foreach (var (x, z) in ordered)
                    {
                        var cells = Cells(x, z, w, d).ToList();
                        if (solid && (!Fits(room, cells) || !Clear(r, fp, x, z, turns)))
                        {
                            continue;
                        }

                        Take(room, cells, solid);
                        plan.Furniture.Add(new Placement(item.Key, PlacementMode.Put, x, z, turns, N, 0, 0, 0f, stash));
                        return true;
                    }

                    return false;
                }
            }
        }

        /// <summary>The turned footprint's south-west corner of a piece with its back on a wall line.</summary>
        private static (int X, int Z) AgainstCorner(Vector2Int facing, int line, int a, int width, int depth)
        {
            if (facing == N) return (a, line);
            if (facing == S) return (a, line - depth);
            if (facing == E) return (line, a);
            return (line - depth, a);
        }

        /// <summary>
        /// The body of a piece whose turned footprint's south-west corner is (x, z), in the room's metres: turned as the
        /// assembler turns it, so the planner and the scene agree.
        /// </summary>
        public static Rect BodyAt(Footprint fp, int x, int z, int turns)
        {
            var rotation = Quaternion.Euler(0f, 90f * turns, 0f);
            var corners = new[] { Vector3.zero, new Vector3(fp.Width, 0f, 0f), new Vector3(0f, 0f, fp.Depth), new Vector3(fp.Width, 0f, fp.Depth) }
                .Select(c => rotation * c).ToArray();
            var offset = new Vector3(x - corners.Min(c => c.x), 0f, z - corners.Min(c => c.z));
            var b = fp.Body;
            var body = new[] { new Vector3(b.xMin, 0f, b.yMin), new Vector3(b.xMax, 0f, b.yMin), new Vector3(b.xMin, 0f, b.yMax), new Vector3(b.xMax, 0f, b.yMax) }
                .Select(c => (rotation * c) + offset).ToArray();
            return Rect.MinMaxRect(body.Min(c => c.x), body.Min(c => c.z), body.Max(c => c.x), body.Max(c => c.z));
        }

        /// <summary>True when the body keeps clear of the room's walls and posts.</summary>
        private static bool Clear(DwellingRoom r, Footprint fp, int x, int z, int turns)
        {
            var body = BodyAt(fp, x, z, turns);
            return body.xMin >= r.X + WallClearance && body.xMax <= r.X + r.Width - WallClearance
                && body.yMin >= r.Z + WallClearance && body.yMax <= r.Z + r.Depth - WallClearance;
        }

        private static IEnumerable<(int, int)> Cells(int x, int z, int w, int d)
        {
            for (var i = x; i < x + w; i++)
            {
                for (var j = z; j < z + d; j++)
                {
                    yield return (i, j);
                }
            }
        }

        private static bool Fits(Room room, List<(int, int)> cells)
        {
            var r = room.Data;
            return cells.All(c => c.Item1 >= r.X && c.Item1 < r.X + r.Width && c.Item2 >= r.Z && c.Item2 < r.Z + r.Depth && !room.Taken.Contains(c));
        }

        private static void Take(Room room, List<(int, int)> cells, bool solid)
        {
            if (!solid)
            {
                return; // a carpet lies under the rest
            }

            foreach (var c in cells)
            {
                room.Taken.Add(c);
            }
        }

        private static List<T> Shuffled<T>(IList<T> items, Random rng)
        {
            var list = items.ToList();
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }

            return list;
        }
    }
}

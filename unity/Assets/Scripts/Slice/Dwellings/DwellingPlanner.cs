using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Slice.Dwellings
{
    /// <summary>
    /// A kit block's footprint in whole cells along its local X and Z, whether it is solid (anything but NoGrid), its
    /// solid body in the footprint's own metres (the pivot at the back-left corner, the front toward +Z), and the height
    /// of what is seen.
    /// </summary>
    public readonly struct Footprint
    {
        public Footprint(int width, int depth, bool solid, Rect body, float height = 0f)
        {
            Width = width;
            Depth = depth;
            Solid = solid;
            Body = body;
            Height = height;
        }

        public int Width { get; }
        public int Depth { get; }
        public bool Solid { get; }
        public Rect Body { get; }
        public float Height { get; }
    }

    public interface IDwellingFootprints
    {
        bool TryGet(string key, out Footprint footprint);
    }

    /// <summary>
    /// Plans a dwelling from its key and its own seed stream (15, *The slice's Dwellings*; VICTORIAN_INTERIORS_PLAN §4;
    /// INTERIOR_FURNISHING_RULES.md): the rooms of its kind and wealth on the 3 m module, storey by storey, turned left or
    /// right by the seed; the stair and its balustrades; then its rooms furnished place by place (the other part of this
    /// class), and the Stash by 19 §6.3's chances. The same key and seed always give the same plan. Every layout here is
    /// an authoring default.
    /// </summary>
    public static partial class DwellingPlanner
    {
        /// <summary>19 §6.3, authoring defaults checked by burglary_sim.py: a Stash in half the poor homes, 60 % and 70 %.</summary>
        public static double StashChance(DwellingWealth wealth) => wealth == DwellingWealth.Poor ? 0.5 : wealth == DwellingWealth.Middling ? 0.6 : 0.7;

        /// <summary>
        /// Furnishing rules §9 (owner, 2026-11-07; authoring defaults): every rich house has one servant, and half of them
        /// a second.
        /// </summary>
        public const double SecondServantChance = 0.5;

        /// <summary>The pack has no chest, so the Stash stays BK's trunk (plan §6: a themed piece the pack lacks).</summary>
        public const string StashKey = "BkTrunk";

        public const string StairKey = "ViStair", BalustradeKey = "ViBanister", FireplaceKey = "ViFireplace", ChandelierKey = "ViChandelier";

        /// <summary>
        /// The storeys of each house type outside (city plan §5.2; `VictorianHouses` specs): a terrace 3, a villa 3, a
        /// shop house 4, a tenement 4, a warehouse 3.
        /// </summary>
        public static int StoreysOutside(DwellingArchetype archetype) =>
            archetype == DwellingArchetype.Shop || archetype == DwellingArchetype.Tenement ? 4 : 3;

        /// <summary>
        /// Plan §4.1 (owner): a rich house has as many floors inside as its building has storeys outside, two or three;
        /// a middling or poor house one; a tenement flat is one floor of its building and a warehouse is a store, so
        /// both keep one.
        /// </summary>
        public static int Floors(DwellingKey key) =>
            key.Wealth == DwellingWealth.Rich && key.Archetype != DwellingArchetype.Tenement && key.Archetype != DwellingArchetype.Warehouse
                ? Mathf.Clamp(StoreysOutside(key.Archetype), 2, 3)
                : 1;

        private const DwellingWealth Poor = DwellingWealth.Poor, Middling = DwellingWealth.Middling, Rich = DwellingWealth.Rich;

        private static readonly Vector2Int N = new Vector2Int(0, 1), S = new Vector2Int(0, -1), E = new Vector2Int(1, 0), W = new Vector2Int(-1, 0);

        public static int Turns(Vector2Int facing) => facing == N ? 0 : facing == E ? 1 : facing == S ? 2 : 3;

        public static DwellingPlan Plan(DwellingKey key, Random rng, IDwellingFootprints kit)
        {
            var plan = Layout(key.Archetype, key.Wealth, Floors(key), rng);
            if (rng.Next(2) == 1)
            {
                plan = Mirror(plan);
            }

            plan.Servants = key.Wealth == Rich && plan.Rooms.Any(r => r.Kind == RoomKind.ServantRoom)
                ? 1 + (rng.NextDouble() < SecondServantChance ? 1 : 0)
                : 0;
            Furnish(plan, key.Wealth, rng, kit);
            return plan;
        }

        // ------------------------------------------------------------------------------------------- layouts

        private const int M = DwellingPlan.Module;

        /// <summary>
        /// The rooms of furnishing rules §5 on the 3 m module (sizes of the interiors plan §3); the street door is on the
        /// front, z = 0. Every home has somewhere to sleep, to cook and to wash (owner, VI-5): a poor home is entered into
        /// its kitchen-living room and washes in a washroom of 3 × 3 m (rules §9); a middling house has a hall, a parlour,
        /// a kitchen, a bedroom and a bathroom.
        /// </summary>
        private static DwellingPlan Layout(DwellingArchetype archetype, DwellingWealth wealth, int floors, Random rng)
        {
            var poor = wealth == Poor;
            var finish = poor ? Finish.Dark : Finish.Paper;
            var wash = poor ? RoomKind.Washroom : RoomKind.WC;
            var washName = poor ? "Washroom" : "WC";
            if (floors > 1)
            {
                return archetype == DwellingArchetype.Shop ? RichShop(floors) : RichHouse(floors);
            }

            switch (archetype)
            {
                case DwellingArchetype.Tenement:
                    // One flat: a room that is kitchen, living room and bedroom at once, or a kitchen-living room and a
                    // bedroom; a washroom (a WC in a middling flat) either way.
                    return rng.Next(2) == 0
                        ? new DwellingPlan()
                            .Room("Room", RoomKind.Room, 0, 0, 9, 9, finish).Room(washName, wash, 9, 0, 3, 3, Finish.Dark)
                            .Open(Opening.Street, 0, 0).Open(Opening.Window, 3, 9).Open(Opening.Window, 9, 3, true).Open(Opening.Door, 9, 0, true)
                        : new DwellingPlan()
                            .Room("Kitchen", RoomKind.Kitchen, 0, 0, 6, 9, Finish.Dark).Room("Bedroom", RoomKind.Bedroom, 6, 0, 9, 9, finish)
                            .Room(washName, wash, 0, 9, 3, 3, Finish.Dark)
                            .Open(Opening.Street, 0, 0).Open(Opening.Window, 0, 3, true).Open(Opening.Door, 6, 0, true).Open(Opening.Door, 0, 9)
                            .Open(Opening.Window, 9, 9).Open(Opening.Window, 15, 3, true);
                case DwellingArchetype.Terrace:
                case DwellingArchetype.Villa:
                    if (poor)
                    {
                        // The kitchen-living room across the front, entered from the street; the coal store beside its
                        // hearth; a bedroom behind with the washroom off it, and a back bedroom.
                        return new DwellingPlan()
                            .Room("Kitchen", RoomKind.Kitchen, 0, 0, 12, 9, Finish.Dark).Room("Coal store", RoomKind.Pantry, 9, 9, 3, 3, Finish.Dark)
                            .Room("Bedroom", RoomKind.Bedroom, 0, 9, 9, 6, finish).Room("Washroom", RoomKind.Washroom, 9, 12, 3, 3, Finish.Dark)
                            .Room("Back bedroom", RoomKind.Bedroom, 0, 15, 12, 6, finish)
                            .Open(Opening.Street, 3, 0).Open(Opening.Window, 0, 0).Open(Opening.Window, 9, 0).Open(Opening.Window, 12, 3, true)
                            .Open(Opening.Door, 3, 9).Open(Opening.Door, 9, 9).Open(Opening.Door, 9, 12, true).Open(Opening.Door, 3, 15)
                            .Open(Opening.Window, 0, 12, true)
                            .Open(Opening.Window, 0, 18, true).Open(Opening.Window, 3, 21).Open(Opening.Window, 9, 21).Open(Opening.Window, 12, 18, true);
                    }

                    // A hall along the side and the parlour at the front; the kitchen behind them with the bathroom
                    // beside it; the bedroom across the back.
                    return new DwellingPlan()
                        .Room("Hall", RoomKind.Hall, 0, 0, 3, 9, finish).Room("Parlour", RoomKind.Parlour, 3, 0, 9, 9, finish)
                        .Room("Kitchen", RoomKind.Kitchen, 0, 9, 9, 6, Finish.Dark).Room("Bathroom", RoomKind.Bathroom, 9, 9, 3, 6, Finish.Dark)
                        .Room("Bedroom", RoomKind.Bedroom, 0, 15, 12, 6, finish)
                        .Open(Opening.Street, 0, 0).Open(Opening.Window, 3, 0).Open(Opening.Window, 9, 0).Open(Opening.Window, 12, 3, true)
                        .Open(Opening.Door, 3, 3, true).Open(Opening.Door, 0, 9).Open(Opening.Door, 9, 9, true)
                        .Open(Opening.Door, 3, 15).Open(Opening.Window, 0, 12, true)
                        .Open(Opening.Window, 0, 18, true).Open(Opening.Window, 3, 21).Open(Opening.Window, 9, 21).Open(Opening.Window, 12, 18, true);
                case DwellingArchetype.Shop:
                    return poor
                        ? new DwellingPlan()
                            .Room("Shop", RoomKind.ShopRoom, 0, 0, 9, 9, Finish.Dark).Room("Back room", RoomKind.BackRoom, 0, 9, 9, 6, Finish.Dark)
                            .Room("Washroom", RoomKind.Washroom, 9, 9, 3, 3, Finish.Dark)
                            .Open(Opening.Street, 3, 0).Open(Opening.Window, 0, 0).Open(Opening.Window, 6, 0)
                            .Open(Opening.Door, 6, 9).Open(Opening.Door, 9, 9, true).Open(Opening.Window, 0, 12, true).Open(Opening.Window, 6, 15)
                        : new DwellingPlan()
                            .Room("Shop", RoomKind.ShopRoom, 0, 0, 12, 9, Finish.Panel).Room("Kitchen", RoomKind.Kitchen, 0, 9, 9, 6, Finish.Dark)
                            .Room("Bathroom", RoomKind.Bathroom, 9, 9, 3, 6, Finish.Dark)
                            .Room("Back room", RoomKind.BackRoom, 0, 15, 12, 6, finish)
                            .Open(Opening.Street, 3, 0).Open(Opening.Window, 0, 0).Open(Opening.Window, 6, 0).Open(Opening.Window, 9, 0)
                            .Open(Opening.Door, 6, 9).Open(Opening.Door, 9, 9, true).Open(Opening.Door, 3, 15)
                            .Open(Opening.Window, 0, 12, true).Open(Opening.Window, 0, 18, true).Open(Opening.Window, 3, 21).Open(Opening.Window, 9, 21)
                            .Open(Opening.Window, 12, 18, true);
                case DwellingArchetype.Warehouse:
                    return new DwellingPlan()
                        .Room("Store", RoomKind.Store, 0, 0, 12, 12, Finish.Dark).Room("Office", RoomKind.Office, 12, 0, 9, 9, Finish.Dark)
                        .Open(Opening.Street, 3, 0).Open(Opening.Door, 12, 3, true).Open(Opening.Window, 15, 0).Open(Opening.Window, 21, 3, true);
                default:
                    throw new ArgumentOutOfRangeException(nameof(archetype), archetype, null);
            }
        }

        /// <summary>
        /// The stair hall of a house of two or three floors, 12 × 12 from (x, 0) (VI-3's stair, plan §8): the first flight
        /// climbs east in its middle row to a well on the first floor; the second climbs back west in the row behind it,
        /// so the flights stand one beside the other. Landings stand over the hall.
        /// </summary>
        private static DwellingPlan StairHall(DwellingPlan plan, int x, int floors, Finish finish)
        {
            plan.Room("Hall", RoomKind.Hall, x, 0, 12, 12, finish);
            for (var level = 1; level < floors; level++)
            {
                plan.Room(level == 1 ? "Landing" : $"Landing {level + 1}", RoomKind.Landing, x, 0, 12, 12, finish, level);
            }

            plan.Well(x + 3, 3, 1, true);
            if (floors > 2)
            {
                plan.Well(x + 3, 6, 2, false);
            }

            return plan;
        }

        /// <summary>
        /// A rich house (plan §4.2, *Layout by floor*; rules §5): the hall with the stair between the drawing room and the
        /// dining room, the study, the kitchen and the scullery behind; three bedrooms, the dressing room and the bathroom
        /// upstairs; the nursery, the servant's room, a second bathroom and the box room under the roof.
        /// </summary>
        private static DwellingPlan RichHouse(int floors)
        {
            var plan = StairHall(new DwellingPlan(), 6, floors, Finish.Paper)
                .Room("Drawing room", RoomKind.DrawingRoom, 0, 0, 6, 12, Finish.Paper).Room("Dining room", RoomKind.DiningRoom, 18, 0, 6, 12, Finish.Paper)
                .Room("Study", RoomKind.Study, 0, 12, 9, 9, Finish.Panel).Room("Kitchen", RoomKind.Kitchen, 9, 12, 9, 9, Finish.Dark)
                .Room("Scullery", RoomKind.Scullery, 18, 12, 6, 9, Finish.Dark)
                .Open(Opening.Street, 9, 0).Open(Opening.Window, 6, 0).Open(Opening.Window, 15, 0)
                .Open(Opening.Window, 0, 0).Open(Opening.Window, 3, 0).Open(Opening.Window, 0, 3, true).Open(Opening.Window, 0, 6, true)
                .Open(Opening.Window, 18, 0).Open(Opening.Window, 21, 0).Open(Opening.Window, 24, 3, true).Open(Opening.Window, 24, 6, true)
                .Open(Opening.Window, 0, 18, true).Open(Opening.Window, 6, 21).Open(Opening.Window, 12, 21).Open(Opening.Window, 21, 21)
                .Open(Opening.Window, 24, 15, true)
                .Open(Opening.Door, 6, 6, true).Open(Opening.Door, 18, 6, true).Open(Opening.Door, 6, 12).Open(Opening.Door, 12, 12)
                .Open(Opening.Door, 18, 15, true)
                .Room("Bedroom", RoomKind.Bedroom, 0, 0, 6, 12, Finish.Paper, 1).Room("Second bedroom", RoomKind.Bedroom, 18, 0, 6, 12, Finish.Paper, 1)
                .Room("Dressing room", RoomKind.DressingRoom, 0, 12, 6, 9, Finish.Paper, 1).Room("Bathroom", RoomKind.Bathroom, 6, 12, 6, 9, Finish.Paper, 1)
                .Room("Guest room", RoomKind.Bedroom, 12, 12, 12, 9, Finish.Paper, 1)
                .Open(Opening.Window, 6, 0, false, 1).Open(Opening.Window, 9, 0, false, 1).Open(Opening.Window, 12, 0, false, 1).Open(Opening.Window, 15, 0, false, 1)
                .Open(Opening.Window, 0, 0, false, 1).Open(Opening.Window, 0, 6, true, 1).Open(Opening.Window, 21, 0, false, 1).Open(Opening.Window, 24, 6, true, 1)
                .Open(Opening.Window, 0, 15, true, 1).Open(Opening.Window, 9, 21, false, 1).Open(Opening.Window, 15, 21, false, 1).Open(Opening.Window, 24, 15, true, 1)
                .Open(Opening.Door, 6, 0, true, 1).Open(Opening.Door, 18, 3, true, 1).Open(Opening.Door, 3, 12, false, 1)
                .Open(Opening.Door, 9, 12, false, 1).Open(Opening.Door, 12, 12, false, 1);
            if (floors > 2)
            {
                plan.Room("Nursery", RoomKind.Nursery, 0, 0, 6, 12, Finish.Paper, 2).Room("Servant's room", RoomKind.ServantRoom, 18, 0, 6, 12, Finish.Dark, 2)
                    .Room("Top bathroom", RoomKind.Bathroom, 6, 12, 6, 9, Finish.Dark, 2).Room("Box room", RoomKind.BoxRoom, 12, 12, 6, 9, Finish.Dark, 2)
                    .Open(Opening.Window, 3, 0, false, 2).Open(Opening.Window, 0, 6, true, 2).Open(Opening.Window, 9, 0, false, 2).Open(Opening.Window, 15, 0, false, 2)
                    .Open(Opening.Window, 18, 0, false, 2).Open(Opening.Window, 24, 6, true, 2).Open(Opening.Window, 12, 21, false, 2)
                    .Open(Opening.Door, 6, 0, true, 2).Open(Opening.Door, 18, 0, true, 2).Open(Opening.Door, 9, 12, false, 2).Open(Opening.Door, 12, 12, false, 2);
            }

            return plan;
        }

        /// <summary>A rich shop house (plan §4.2): the shop and its back room downstairs, the living rooms moved up.</summary>
        private static DwellingPlan RichShop(int floors)
        {
            var plan = StairHall(new DwellingPlan(), 12, floors, Finish.Paper)
                .Room("Shop", RoomKind.ShopRoom, 0, 0, 12, 12, Finish.Panel).Room("Back room", RoomKind.BackRoom, 0, 12, 12, 9, Finish.Paper)
                .Room("Kitchen", RoomKind.Kitchen, 12, 12, 12, 9, Finish.Dark)
                .Open(Opening.Street, 21, 0).Open(Opening.Window, 0, 0).Open(Opening.Window, 3, 0).Open(Opening.Window, 6, 0).Open(Opening.Window, 9, 0)
                .Open(Opening.Window, 12, 0).Open(Opening.Window, 18, 0).Open(Opening.Window, 0, 6, true).Open(Opening.Window, 24, 6, true)
                .Open(Opening.Window, 0, 15, true).Open(Opening.Window, 6, 21).Open(Opening.Window, 18, 21).Open(Opening.Window, 24, 15, true)
                .Open(Opening.Door, 12, 6, true).Open(Opening.Door, 3, 12).Open(Opening.Door, 15, 12)
                .Room("Drawing room", RoomKind.DrawingRoom, 0, 0, 12, 12, Finish.Paper, 1).Room("Dining room", RoomKind.DiningRoom, 0, 12, 12, 9, Finish.Paper, 1)
                .Room("Bathroom", RoomKind.Bathroom, 12, 12, 6, 9, Finish.Paper, 1).Room("Study", RoomKind.Study, 18, 12, 6, 9, Finish.Panel, 1)
                .Open(Opening.Window, 3, 0, false, 1).Open(Opening.Window, 6, 0, false, 1).Open(Opening.Window, 12, 0, false, 1).Open(Opening.Window, 18, 0, false, 1)
                .Open(Opening.Window, 0, 6, true, 1).Open(Opening.Window, 0, 15, true, 1).Open(Opening.Window, 6, 21, false, 1).Open(Opening.Window, 24, 15, true, 1)
                .Open(Opening.Door, 12, 0, true, 1).Open(Opening.Door, 6, 12, false, 1).Open(Opening.Door, 12, 12, false, 1).Open(Opening.Door, 21, 12, false, 1);
            if (floors > 2)
            {
                plan.Room("Bedroom", RoomKind.Bedroom, 0, 0, 12, 12, Finish.Paper, 2).Room("Second bedroom", RoomKind.Bedroom, 0, 12, 12, 9, Finish.Paper, 2)
                    .Room("Servant's room", RoomKind.ServantRoom, 12, 12, 6, 9, Finish.Dark, 2).Room("Top bathroom", RoomKind.Bathroom, 18, 12, 6, 9, Finish.Dark, 2)
                    .Open(Opening.Window, 3, 0, false, 2).Open(Opening.Window, 6, 0, false, 2).Open(Opening.Window, 15, 0, false, 2).Open(Opening.Window, 0, 6, true, 2)
                    .Open(Opening.Window, 0, 15, true, 2).Open(Opening.Window, 6, 21, false, 2).Open(Opening.Window, 24, 15, true, 2)
                    .Open(Opening.Door, 12, 3, true, 2).Open(Opening.Door, 6, 12, false, 2).Open(Opening.Door, 12, 12, false, 2).Open(Opening.Door, 18, 12, false, 2);
            }

            return plan;
        }

        /// <summary>The same plan turned left for right about its middle, so neighbours do not all repeat one hand.</summary>
        private static DwellingPlan Mirror(DwellingPlan plan)
        {
            var width = plan.Width;
            var mirrored = new DwellingPlan();
            foreach (var r in plan.Rooms)
            {
                mirrored.Room(r.Name, r.Kind, width - r.X - r.Width, r.Z, r.Width, r.Depth, r.Finish, r.Level);
            }

            foreach (var pair in plan.Openings)
            {
                var (x, z, alongZ, level) = pair.Key;
                mirrored.Open(pair.Value, alongZ ? width - x : width - x - M, z, alongZ, level);
            }

            foreach (var w in plan.Wells)
            {
                mirrored.Well(width - w.X - DwellingWell.Width, w.Z, w.Level, !w.ClimbsEast);
            }

            return mirrored;
        }

        // ------------------------------------------------------------------------------------------- stairs

        /// <summary>
        /// Each well's stair on the storey below and its balustrades round the well (VI-3, plan §8): the stair climbs from
        /// one metre under the floor before the well to the module line at its far end; balustrades guard the well's long
        /// sides, unless the next flight stands there, and its low end. The stair, the well and the balustrades block the
        /// way; the stair's foot and head stay clear and are ways in and out of their rooms (rules G1).
        /// </summary>
        private static void Stairs(DwellingPlan plan, List<Room> rooms)
        {
            var stairs = new List<(RectInt Cells, int Level)>();
            foreach (var w in plan.Wells)
            {
                var below = w.Level - 1;
                var x = w.ClimbsEast ? w.X - 1 : w.X;
                var cells = new RectInt(x, w.Z, 7, DwellingWell.Depth);
                stairs.Add((cells, below));
                plan.Furniture.Add(new Placement(StairKey, PlacementMode.Put, x, w.Z, w.ClimbsEast ? 1 : 3, N, 0, 0, 0f, false, below, "Stair"));
                Keep(rooms, cells, below, true, false);
                Keep(rooms, new RectInt(w.ClimbsEast ? x - 1 : x + 7, w.Z, 1, DwellingWell.Depth), below, false, true); // its foot
                // Under its head: the newel stands 0.12 m past the footprint (VI-5), and its rails' bounds reach the floor.
                Keep(rooms, new RectInt(w.ClimbsEast ? x + 7 : x - 1, w.Z, 1, DwellingWell.Depth), below, false, false);
                Keep(rooms, new RectInt(w.X, w.Z, DwellingWell.Width, DwellingWell.Depth), w.Level, true, false);
                Keep(rooms, new RectInt(w.ClimbsEast ? w.X + DwellingWell.Width : w.X - 1, w.Z, 1, DwellingWell.Depth), w.Level, false, true); // its head
            }

            // A balustrade's side stands flush with its footprint's front, which faces the well, so it stands on the well's
            // edge (VI-5): turns 0 faces +Z, 1 +X, 2 −Z, 3 −X.
            foreach (var w in plan.Wells)
            {
                var low = new RectInt(w.ClimbsEast ? w.X - 1 : w.X + DwellingWell.Width, w.Z, 1, DwellingWell.Depth);
                Balustrade(plan, rooms, stairs, low, w.ClimbsEast ? 1 : 3, w.Level);
                foreach (var (z, turns) in new[] { (w.Z - 1, 0), (w.Z + DwellingWell.Depth, 2) })
                {
                    for (var x = w.X; x < w.X + DwellingWell.Width; x += M)
                    {
                        Balustrade(plan, rooms, stairs, new RectInt(x, z, M, 1), turns, w.Level);
                    }
                }
            }
        }

        private static void Balustrade(DwellingPlan plan, List<Room> rooms, List<(RectInt Cells, int Level)> stairs, RectInt cells, int turns, int level)
        {
            if (stairs.Any(s => s.Level == level && s.Cells.Overlaps(cells)))
            {
                return; // the next flight's own rail guards that side
            }

            plan.Furniture.Add(new Placement(BalustradeKey, PlacementMode.Put, cells.x, cells.y, turns, N, 0, 0, 0f, false, level, "Balustrade"));
            Keep(rooms, cells, level, true, false);
        }

        /// <summary>
        /// Keeps cells of a storey free of furniture: <paramref name="block"/> when nobody walks there either (a stair, a
        /// well, a balustrade); <paramref name="entry"/> when the cells are a way into the room that must stay reachable.
        /// </summary>
        private static void Keep(List<Room> rooms, RectInt cells, int level, bool block, bool entry)
        {
            foreach (var room in rooms.Where(r => r.Data.Level == level))
            {
                var inside = Cells(cells.x, cells.y, cells.width, cells.height).Where(room.Inside).ToList();
                foreach (var c in inside)
                {
                    room.Taken.Add(c);
                    if (block)
                    {
                        room.Blocked.Add(c);
                    }
                }

                if (entry && inside.Count > 0)
                {
                    room.Entries.Add(inside[inside.Count / 2]);
                }
            }
        }

        // ------------------------------------------------------------------------------------------- geometry

        /// <summary>The room's own wall segments, its doorways' cells kept clear, and each doorway as a way in.</summary>
        private static Room Prepare(DwellingPlan plan, DwellingRoom r)
        {
            var room = new Room { Data = r };
            for (var x = r.X; x < r.X + r.Width; x += M)
            {
                room.Walls.Add((x, r.Z, false, N, r.Z));
                room.Walls.Add((x, r.Z + r.Depth, false, S, r.Z + r.Depth));
            }

            for (var z = r.Z; z < r.Z + r.Depth; z += M)
            {
                room.Walls.Add((r.X, z, true, E, r.X));
                room.Walls.Add((r.X + r.Width, z, true, W, r.X + r.Width));
            }

            foreach (var wall in room.Walls)
            {
                var kind = plan.OpeningAt(wall.X, wall.Z, wall.AlongZ, r.Level);
                if (kind != Opening.Door && kind != Opening.Street)
                {
                    continue;
                }

                // A doorway keeps the cells along its segment clear, and the one behind its middle: the pack's opening
                // is 1.35 m wide over the segment's middle (plan §3). A closet of one module keeps only the first row,
                // or its WC would have no room for the toilet (rules G1).
                for (var i = 0; i < M; i++)
                {
                    room.Taken.Add(Cell(wall, i, 0));
                }

                if (r.Width > M && r.Depth > M)
                {
                    room.Taken.Add(Cell(wall, M / 2, 1));
                }

                room.Entries.Add(Cell(wall, M / 2, 0));
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

        /// <summary>True when another room of the same storey stands behind the wall segment.</summary>
        private static bool Inner(DwellingPlan plan, DwellingRoom r, (int X, int Z, bool AlongZ, Vector2Int Facing, int Line) wall)
        {
            var mid = M * 0.5f;
            var (px, pz) = wall.AlongZ ? (wall.Line - (wall.Facing.x * 0.5f), wall.Z + mid) : (wall.X + mid, wall.Line - (wall.Facing.y * 0.5f));
            return plan.Rooms.Any(o => o.Level == r.Level && o.Contains(px, pz));
        }

        /// <summary>True when the cell lies on a stair, its well or a balustrade of the room's storey.</summary>
        private static bool Kept(DwellingPlan plan, DwellingRoom r, (int X, int Z) c) =>
            plan.Furniture.Any(f => f.Level == r.Level && f.Name != null && Covers(f, c)) || plan.InWell(c.X, c.Z, r.Level);

        private static bool Covers(Placement f, (int X, int Z) c)
        {
            var (w, d) = f.Key == StairKey ? (7, 3) : (M, 1);
            if (f.Key == BalustradeKey && f.Turns % 2 == 1)
            {
                (w, d) = (1, M);
            }

            return c.X >= f.X && c.X < f.X + w && c.Z >= f.Z && c.Z < f.Z + d;
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

        /// <summary>
        /// Measured (plan §3): a wall's body runs 0.16 m into the room, its dado, and a piece's back stands on it
        /// (`ViBack`). A solid body may touch it within the layout check's tolerance of 0.01 m, so a piece three cells
        /// wide never stands in a corner, where the side wall's dado meets it.
        /// </summary>
        private const float WallClearance = 0.16f - 0.005f;

        /// <summary>True when the body keeps clear of the room's walls.</summary>
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

        /// <summary>True when the body overlaps a piece already put by more than half the kit check's 1 cm.</summary>
        private static bool Touches(Room room, Rect body)
        {
            var inner = Rect.MinMaxRect(body.xMin + 0.005f, body.yMin + 0.005f, body.xMax - 0.005f, body.yMax - 0.005f);
            return room.Bodies.Any(b => b.Overlaps(inner));
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

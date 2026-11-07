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
    /// Plans a dwelling from its key and its own seed stream (15, *The slice's Dwellings*; VICTORIAN_INTERIORS_PLAN §4,
    /// VI-4): the rooms of its kind and wealth on the 3 m module, storey by storey, turned left or right by the seed;
    /// the stair and its balustrades; then the furniture its rooms and wealth call for, from Victorian Interiors, and the
    /// Stash by 19 §6.3's chances. The same key and seed always give the same plan. Every layout and list here is an
    /// authoring default.
    /// </summary>
    public static class DwellingPlanner
    {
        /// <summary>19 §6.3, authoring defaults checked by burglary_sim.py: a Stash in half the poor homes, 60 % and 70 %.</summary>
        public static double StashChance(DwellingWealth wealth) => wealth == DwellingWealth.Poor ? 0.5 : wealth == DwellingWealth.Middling ? 0.6 : 0.7;

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

        private const float PictureHeight = 1.6f, LampHeight = 2.2f, MirrorHeight = 1.5f, TowelHeight = 1.0f;

        /// <summary>Measured (plan §3): a pack table stands 0.93 m; a candle goes on it.</summary>
        private const float TableTop = 0.93f;

        /// <summary>The chandelier's pivot under the ceiling, as in the seven (`VeilmouthInteriors.Chandelier`).</summary>
        private const float ChandelierHeight = 3.3f;

        /// <summary>
        /// Measured (plan §3): a wall's body runs 0.16 m into the room, its dado, and a piece's back stands on it
        /// (`ViBack`). A solid body may touch it within the layout check's tolerance of 0.01 m, so a piece three cells
        /// wide never stands in a corner, where the side wall's dado meets it.
        /// </summary>
        private const float WallClearance = 0.16f - 0.005f;

        private enum Spot
        {
            Wall,    // against a wall
            Inner,   // against a wall with a room behind it, never a window: a fireplace (plan §4.2)
            Middle,  // in the room, clear of the walls
            Free,    // anywhere free
            Hang,    // on a wall's face
            AtTable, // a chair at a table already placed, facing it
            OnTable, // on a table already placed: a candle
            Ceiling, // under the ceiling, in the room's middle: a chandelier
        }

        private readonly struct Item
        {
            public Item(string key, Spot spot, DwellingWealth from = DwellingWealth.Poor, int count = 1, float y = 0f, DwellingWealth upTo = DwellingWealth.Rich)
            {
                Key = key;
                Spot = spot;
                From = from;
                UpTo = upTo;
                Count = count;
                Y = y;
            }

            public string Key { get; }
            public Spot Spot { get; }
            public DwellingWealth From { get; }
            public DwellingWealth UpTo { get; }
            public int Count { get; }
            public float Y { get; }
        }

        private const DwellingWealth Poor = DwellingWealth.Poor, Middling = DwellingWealth.Middling, Rich = DwellingWealth.Rich;

        private static Item Picture(DwellingWealth from = Middling, int count = 1) => new Item("ViPicture", Spot.Hang, from, count, PictureHeight);
        private static Item Lamp(DwellingWealth from = Middling, int count = 1) => new Item("ViWallLamp", Spot.Hang, from, count, LampHeight);
        private static Item Candle(DwellingWealth upTo = Poor) => new Item("ViCandle", Spot.OnTable, Poor, 1, TableTop, upTo);

        /// <summary>
        /// Each room kind's furniture from Victorian Interiors (plan §4.2), from the poorest household up; a richer home
        /// also has the later lines. "ViPicture" takes one of the pack's eight pictures by the seed. Lamps light the
        /// middling and rich rooms, candles the poor ones (plan §5).
        /// </summary>
        private static readonly Dictionary<RoomKind, Item[]> Furnishings = new Dictionary<RoomKind, Item[]>
        {
            [RoomKind.Hall] = new[]
            {
                new Item("ViClock", Spot.Wall, Rich), new Item("ViCoatStand", Spot.Free, Middling), new Item("ViMirror", Spot.Hang, Middling, y: MirrorHeight),
                new Item("ViPlant", Spot.Free, Middling), Lamp(), new Item("ViRug", Spot.Middle, Rich),
            },
            [RoomKind.Landing] = new[]
            {
                new Item("ViPlantB", Spot.Free), new Item("ViArmchair", Spot.Free, Rich), Lamp(Poor), Picture(Poor),
            },
            [RoomKind.DrawingRoom] = new[]
            {
                new Item(FireplaceKey, Spot.Inner), new Item("ViSofaGreen", Spot.Wall), new Item("ViBookcaseLow", Spot.Wall),
                new Item("ViTableRound", Spot.Middle), new Item("ViArmchair", Spot.AtTable, count: 2), new Item("ViCarpet", Spot.Middle),
                new Item(ChandelierKey, Spot.Ceiling), Picture(Poor, 2), new Item("ViPalm", Spot.Free), new Item("ViVase", Spot.Free),
            },
            [RoomKind.Parlour] = new[]
            {
                new Item(FireplaceKey, Spot.Inner), new Item("ViSofa", Spot.Wall), new Item("ViTableRound", Spot.Middle),
                new Item("ViChair", Spot.AtTable, count: 2), Candle(), new Item("ViBookcaseLow", Spot.Wall, Middling),
                new Item("ViRug", Spot.Middle, Middling), Picture(), Lamp(), new Item("ViPlant", Spot.Free, Middling),
            },
            [RoomKind.DiningRoom] = new[]
            {
                new Item("ViDiningTable", Spot.Middle), new Item("ViChair", Spot.AtTable, count: 6), new Item("ViCupboardGlass", Spot.Wall),
                new Item(FireplaceKey, Spot.Inner, Rich), new Item("ViSideboard", Spot.Wall, Rich), new Item(ChandelierKey, Spot.Ceiling),
                new Item("ViCarpetB", Spot.Middle, Rich), Picture(Poor),
            },
            [RoomKind.Study] = new[]
            {
                new Item("ViDesk", Spot.Wall), new Item("ViChair", Spot.Free), new Item("ViBookcase", Spot.Wall, count: 2),
                new Item(FireplaceKey, Spot.Inner, Rich), new Item("ViArmchair", Spot.Free), new Item("ViCarpetB", Spot.Middle),
                Lamp(Poor), Picture(Poor),
            },
            [RoomKind.Kitchen] = new[]
            {
                new Item(FireplaceKey, Spot.Inner), new Item("ViKitchenTable", Spot.Middle), new Item("ViChair", Spot.AtTable, count: 2),
                Candle(), new Item("ViBucket", Spot.Free), new Item("ViBasin", Spot.Wall, Middling), new Item("ViCupboardGlass", Spot.Wall, Middling),
                new Item("ViShelvesB", Spot.Wall, Middling), new Item("ViBox", Spot.Free, Middling), Lamp(),
            },
            [RoomKind.Scullery] = new[]
            {
                new Item("ViBasin", Spot.Wall), new Item("ViBucket", Spot.Free, count: 2), new Item("BkBasket", Spot.Free), new Item("ViBox", Spot.Free),
            },
            [RoomKind.Bedroom] = new[]
            {
                new Item("ViBed", Spot.Wall), new Item(FireplaceKey, Spot.Inner, Rich), new Item("ViCabinet", Spot.Wall, Middling),
                new Item("ViTableB", Spot.Middle, upTo: Poor), new Item("ViChair", Spot.AtTable, upTo: Poor), Candle(),
                new Item("ViChair", Spot.Free, Middling), new Item("ViMirrorSmall", Spot.Hang, Middling, y: MirrorHeight),
                new Item("ViRug", Spot.Middle, Middling, upTo: Middling), new Item("ViCarpet", Spot.Middle, Rich), Lamp(), Picture(),
            },
            [RoomKind.DressingRoom] = new[]
            {
                new Item("ViMirrorTall", Spot.Wall), new Item("ViCabinet", Spot.Wall), new Item("ViChair", Spot.Free),
                new Item("ViCoatStand", Spot.Free, count: 2), new Item("ViRug", Spot.Middle), Lamp(Poor),
            },
            [RoomKind.Bathroom] = new[]
            {
                new Item("ViBath", Spot.Wall), new Item("ViToilet", Spot.Wall), new Item("ViBasin", Spot.Wall), new Item("ViTowelRail", Spot.Wall),
                new Item("ViMirrorSmall", Spot.Hang, y: MirrorHeight), new Item("ViTowelBar", Spot.Hang, y: TowelHeight),
                new Item("ViRugB", Spot.Middle), new Item("ViPlantC", Spot.Free), Lamp(Poor),
            },
            [RoomKind.WC] = new[]
            {
                new Item("ViToilet", Spot.Wall), new Item("ViBasin", Spot.Wall),
            },
            [RoomKind.Nursery] = new[]
            {
                new Item("ViBed", Spot.Wall, count: 2), new Item("ViTableRound", Spot.Middle), new Item("ViChair", Spot.AtTable),
                new Item("ViRug", Spot.Middle), new Item("ViBox", Spot.Free, count: 2), Picture(Poor), Lamp(Poor),
            },
            [RoomKind.ServantRoom] = new[]
            {
                new Item("ViBed", Spot.Wall), new Item("ViTableB", Spot.Middle), new Item("ViChair", Spot.AtTable), Candle(Rich),
            },
            [RoomKind.BoxRoom] = new[]
            {
                new Item("ViBox", Spot.Free, count: 3), new Item("Crate", Spot.Free, count: 2), new Item("BkBasketB", Spot.Free),
            },
            [RoomKind.Room] = new[]
            {
                // The poor family's one room (plan §4.2): they live, sleep, cook and wash here.
                new Item("ViBed", Spot.Wall), new Item(FireplaceKey, Spot.Inner), new Item("ViKitchenTable", Spot.Middle),
                new Item("ViChair", Spot.AtTable, count: 2), Candle(Rich), new Item("ViBasin", Spot.Wall), new Item("ViBucket", Spot.Free),
                new Item("ViCabinet", Spot.Wall, Middling), new Item("ViRug", Spot.Middle, Middling),
            },
            [RoomKind.ShopRoom] = new[]
            {
                new Item("ViCounter", Spot.Middle), new Item("ViShelves", Spot.Wall), new Item("ViShelvesB", Spot.Wall), new Item("Crate", Spot.Free),
                new Item("ViCabinet", Spot.Wall, Middling), new Item("Barrel", Spot.Free, Middling), Lamp(),
            },
            [RoomKind.BackRoom] = new[]
            {
                // The shopkeeper sleeps behind the shop (owner, 2026-11-06: a bed in every home); a poor one cooks and washes here.
                new Item("ViBed", Spot.Wall), new Item(FireplaceKey, Spot.Inner, upTo: Poor), new Item("ViTableB", Spot.Middle),
                new Item("ViChair", Spot.AtTable), Candle(), new Item("ViBasin", Spot.Wall, upTo: Poor), new Item("ViBucket", Spot.Free, upTo: Poor),
                new Item("Crate", Spot.Free), new Item("ViCabinet", Spot.Wall, Middling), Lamp(),
            },
            [RoomKind.Pantry] = new[]
            {
                // A closet of 3 × 3: small things only, which keep clear of its walls (a pantry, a coal store, a shop's store).
                new Item("ViBox", Spot.Free, count: 2), new Item("BkBasket", Spot.Free), new Item("ViBucket", Spot.Free), new Item("BkBasketB", Spot.Free),
            },
            [RoomKind.Store] = new[]
            {
                new Item("Crate", Spot.Free, count: 4), new Item("Barrel", Spot.Free, count: 3), new Item("BkBasketB", Spot.Free),
            },
            [RoomKind.Office] = new[]
            {
                new Item("ViDesk", Spot.Wall), new Item("ViChair", Spot.Free), new Item("ViCabinet", Spot.Wall), new Item("ViShelves", Spot.Wall), Lamp(Poor),
            },
        };

        private static readonly string[] Pictures = Enumerable.Range(1, 8).Select(i => $"ViPicture{i}").ToArray();

        /// <summary>Every kit key the planner may place, so the kit asset holds them all.</summary>
        public static IEnumerable<string> Keys =>
            Furnishings.Values.SelectMany(items => items.Select(i => i.Key)).Where(k => k != "ViPicture").Concat(Pictures)
                .Append(StashKey).Append(StairKey).Append(BalustradeKey).Distinct();

        /// <summary>Where a household keeps its Stash, in order of preference.</summary>
        private static readonly RoomKind[] StashRooms =
        {
            RoomKind.Bedroom, RoomKind.Study, RoomKind.Room, RoomKind.BackRoom, RoomKind.DressingRoom, RoomKind.Office,
            RoomKind.BoxRoom, RoomKind.Store, RoomKind.Kitchen,
        };

        private static readonly Vector2Int N = new Vector2Int(0, 1), S = new Vector2Int(0, -1), E = new Vector2Int(1, 0), W = new Vector2Int(-1, 0);

        public static int Turns(Vector2Int facing) => facing == N ? 0 : facing == E ? 1 : facing == S ? 2 : 3;

        public static DwellingPlan Plan(DwellingKey key, Random rng, IDwellingFootprints kit)
        {
            var plan = Layout(key.Archetype, key.Wealth, Floors(key), rng);
            if (rng.Next(2) == 1)
            {
                plan = Mirror(plan);
            }

            Furnish(plan, key.Wealth, rng, kit);
            return plan;
        }

        // ------------------------------------------------------------------------------------------- layouts

        private const int M = DwellingPlan.Module;

        /// <summary>
        /// The rooms of plan §4.2 on the 3 m module (sizes of §3's table); the street door is on the front, z = 0. A poor
        /// household has no WC: its closet is a coal store (plan §4.2, *Washing by wealth*).
        /// </summary>
        private static DwellingPlan Layout(DwellingArchetype archetype, DwellingWealth wealth, int floors, Random rng)
        {
            var poor = wealth == Poor;
            var finish = poor ? Finish.Dark : Finish.Paper;
            var closet = poor ? RoomKind.Pantry : RoomKind.WC;
            var closetName = poor ? "Coal store" : "WC";
            if (floors > 1)
            {
                return archetype == DwellingArchetype.Shop ? RichShop(floors) : RichHouse(floors);
            }

            switch (archetype)
            {
                case DwellingArchetype.Tenement:
                    // One flat: a room, or a room and a kitchen.
                    return rng.Next(2) == 0
                        ? new DwellingPlan()
                            .Room("Room", RoomKind.Room, 0, 0, 9, 9, finish)
                            .Open(Opening.Street, 0, 0).Open(Opening.Window, 3, 9).Open(Opening.Window, 9, 3, true)
                        : new DwellingPlan()
                            .Room("Room", RoomKind.Room, 0, 0, 9, 9, finish).Room("Kitchen", RoomKind.Kitchen, 9, 0, 6, 9, Finish.Dark)
                            .Open(Opening.Street, 0, 0).Open(Opening.Door, 9, 3, true).Open(Opening.Window, 3, 9).Open(Opening.Window, 12, 9);
                case DwellingArchetype.Terrace:
                case DwellingArchetype.Villa:
                    // A hall along the side and the parlour at the front; the kitchen behind them with its closet and
                    // pantry; the bedroom across the back. A fireplace needs a wall's middle module, so no room with one
                    // is 6 × 6 (plan §3).
                    return new DwellingPlan()
                        .Room("Hall", RoomKind.Hall, 0, 0, 3, 9, finish).Room("Parlour", RoomKind.Parlour, 3, 0, 9, 9, finish)
                        .Room("Kitchen", RoomKind.Kitchen, 0, 9, 9, 6, Finish.Dark).Room(closetName, closet, 9, 9, 3, 3, Finish.Dark)
                        .Room("Pantry", RoomKind.Pantry, 9, 12, 3, 3, Finish.Dark).Room("Bedroom", RoomKind.Bedroom, 0, 15, 12, 6, finish)
                        .Open(Opening.Street, 0, 0).Open(Opening.Window, 3, 0).Open(Opening.Window, 9, 0).Open(Opening.Window, 12, 3, true)
                        .Open(Opening.Door, 3, 3, true).Open(Opening.Door, 0, 9).Open(Opening.Door, 9, 9, true).Open(Opening.Door, 9, 12, true)
                        .Open(Opening.Door, 3, 15).Open(Opening.Window, 0, 12, true)
                        .Open(Opening.Window, 0, 18, true).Open(Opening.Window, 3, 21).Open(Opening.Window, 9, 21).Open(Opening.Window, 12, 18, true);
                case DwellingArchetype.Shop:
                    return poor
                        ? new DwellingPlan()
                            .Room("Shop", RoomKind.ShopRoom, 0, 0, 9, 9, Finish.Dark).Room("Back room", RoomKind.BackRoom, 0, 9, 9, 6, Finish.Dark)
                            .Open(Opening.Street, 3, 0).Open(Opening.Window, 0, 0).Open(Opening.Window, 6, 0)
                            .Open(Opening.Door, 6, 9).Open(Opening.Window, 0, 12, true).Open(Opening.Window, 6, 15)
                        : new DwellingPlan()
                            .Room("Shop", RoomKind.ShopRoom, 0, 0, 12, 9, Finish.Panel).Room("Kitchen", RoomKind.Kitchen, 0, 9, 9, 6, Finish.Dark)
                            .Room(closetName, closet, 9, 9, 3, 3, Finish.Dark).Room("Store", RoomKind.Pantry, 9, 12, 3, 3, Finish.Dark)
                            .Room("Back room", RoomKind.BackRoom, 0, 15, 12, 6, finish)
                            .Open(Opening.Street, 3, 0).Open(Opening.Window, 0, 0).Open(Opening.Window, 6, 0).Open(Opening.Window, 9, 0)
                            .Open(Opening.Door, 6, 9).Open(Opening.Door, 9, 9, true).Open(Opening.Door, 9, 12, true).Open(Opening.Door, 3, 15)
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
        /// A rich house (plan §4.2, *Layout by floor*): the hall with the stair between the drawing room and the dining
        /// room, the study, the kitchen and the scullery behind; the bedrooms, the dressing room and the bathroom upstairs;
        /// the nursery, the servant's room and the box room under the roof.
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
                    .Room("Box room", RoomKind.BoxRoom, 6, 12, 12, 9, Finish.Dark, 2)
                    .Open(Opening.Window, 3, 0, false, 2).Open(Opening.Window, 0, 6, true, 2).Open(Opening.Window, 9, 0, false, 2).Open(Opening.Window, 15, 0, false, 2)
                    .Open(Opening.Window, 18, 0, false, 2).Open(Opening.Window, 24, 6, true, 2).Open(Opening.Window, 12, 21, false, 2)
                    .Open(Opening.Door, 6, 0, true, 2).Open(Opening.Door, 18, 0, true, 2).Open(Opening.Door, 9, 12, false, 2);
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
                    .Room("Servant's room", RoomKind.ServantRoom, 12, 12, 6, 9, Finish.Dark, 2).Room("Box room", RoomKind.BoxRoom, 18, 12, 6, 9, Finish.Dark, 2)
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

        // ------------------------------------------------------------------------------------------- furniture

        private sealed class Room
        {
            public DwellingRoom Data;
            public readonly HashSet<(int, int)> Taken = new HashSet<(int, int)>();
            public readonly HashSet<(int, int, bool)> UsedWalls = new HashSet<(int, int, bool)>();
            public readonly List<(int X, int Z, bool AlongZ, Vector2Int Facing, int Line)> Walls = new List<(int, int, bool, Vector2Int, int)>();
            public readonly List<RectInt> Tables = new List<RectInt>();

            /// <summary>The bodies of the solid pieces put free in the room; a crate's body juts 1 cm past its cell.</summary>
            public readonly List<Rect> Bodies = new List<Rect>();
        }

        private static void Furnish(DwellingPlan plan, DwellingWealth wealth, Random rng, IDwellingFootprints kit)
        {
            var rooms = plan.Rooms.Select(r => Prepare(plan, r)).ToList();
            Stairs(plan, rooms);
            var stashRoom = Array.FindIndex(StashRooms, k => rooms.Any(r => r.Data.Kind == k));
            var stash = rng.NextDouble() < StashChance(wealth) && stashRoom >= 0;

            foreach (var room in rooms)
            {
                // In the list's order: what stands against the walls and in the middle comes before the chairs at its
                // tables and the candles on them; pictures and lamps take what wall is left.
                foreach (var item in Furnishings[room.Data.Kind].Where(i => i.From <= wealth && wealth <= i.UpTo).OrderBy(Order))
                {
                    for (var n = 0; n < item.Count; n++)
                    {
                        var key = item.Key == "ViPicture" ? Pictures[rng.Next(Pictures.Length)] : item.Key;
                        if (!Place(plan, room, new Item(key, item.Spot, item.From, 1, item.Y), rng, kit, false))
                        {
                            plan.Unplaced.Add($"{room.Data.Name}: {key}");
                        }
                    }
                }

                if (stash && room.Data.Kind == StashRooms[stashRoom])
                {
                    stash = !Place(plan, room, new Item(StashKey, Spot.Free), rng, kit, true);
                }
            }
        }

        private static int Order(Item item)
        {
            switch (item.Spot)
            {
                case Spot.Inner: return 0;
                case Spot.Wall: return 1;
                case Spot.Middle: return 2;
                case Spot.AtTable: return 3;
                case Spot.OnTable: return 4;
                case Spot.Free: return 5;
                default: return 6;
            }
        }

        /// <summary>
        /// Each well's stair on the storey below and its balustrades round the well (VI-3, plan §8): the stair climbs from
        /// one metre under the floor before the well to the module line at its far end; balustrades guard the well's long
        /// sides, unless the next flight stands there, and its low end. The well, the stair, its foot and its head are
        /// kept clear of furniture.
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
                Keep(rooms, cells, below);
                Keep(rooms, new RectInt(w.ClimbsEast ? x - 1 : x + 7, w.Z, 1, DwellingWell.Depth), below); // its foot
                // Under its head: the newel stands 0.12 m past the footprint (VI-5), and its rails' bounds reach the floor.
                Keep(rooms, new RectInt(w.ClimbsEast ? x + 7 : x - 1, w.Z, 1, DwellingWell.Depth), below);
                Keep(rooms, new RectInt(w.X, w.Z, DwellingWell.Width, DwellingWell.Depth), w.Level);
                Keep(rooms, new RectInt(w.ClimbsEast ? w.X + DwellingWell.Width : w.X - 1, w.Z, 1, DwellingWell.Depth), w.Level); // its head
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
            Keep(rooms, cells, level);
        }

        private static void Keep(List<Room> rooms, RectInt cells, int level)
        {
            foreach (var room in rooms.Where(r => r.Data.Level == level))
            {
                foreach (var c in cells.allPositionsWithin)
                {
                    room.Taken.Add((c.x, c.y));
                }
            }
        }

        /// <summary>The room's own wall segments, and the cells kept clear inside each doorway.</summary>
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
                // or its WC would have no room for the toilet.
                for (var i = 0; i < M; i++)
                {
                    room.Taken.Add(Cell(wall, i, 0));
                }

                if (r.Width > M && r.Depth > M)
                {
                    room.Taken.Add(Cell(wall, M / 2, 1));
                }
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
                case Spot.Inner:
                case Spot.Hang:
                {
                    var hang = item.Spot == Spot.Hang;
                    // A piece wider than a module (the desk, 4 m) spans the next segment along the wall too.
                    var span = width > M ? 2 : 1;
                    if (width > span * M || (hang && span > 1))
                    {
                        return false;
                    }

                    var walls = Shuffled(room.Walls, rng);
                    if (item.Spot == Spot.Inner)
                    {
                        // A fireplace's back faces another room, never a window (plan §4.2); an outer wall only if none.
                        walls = walls.Where(w => Inner(plan, r, w)).Concat(walls.Where(w => !Inner(plan, r, w))).ToList();
                    }

                    foreach (var wall in walls)
                    {
                        if (plan.OpeningAt(wall.X, wall.Z, wall.AlongZ, r.Level) != Opening.Wall || room.UsedWalls.Contains((wall.X, wall.Z, wall.AlongZ)))
                        {
                            continue;
                        }

                        var next = wall.AlongZ ? (wall.X, wall.Z + M, true) : (wall.X + M, wall.Z, false);
                        if (span > 1 && (!room.Walls.Any(w => w.X == next.Item1 && w.Z == next.Item2 && w.AlongZ == next.Item3 && w.Line == wall.Line)
                                         || plan.OpeningAt(next.Item1, next.Item2, next.Item3, r.Level) != Opening.Wall || room.UsedWalls.Contains(next)))
                        {
                            continue;
                        }

                        var start = wall.AlongZ ? wall.Z : wall.X;
                        if (hang)
                        {
                            var at = start + ((M - width) / 2);
                            room.UsedWalls.Add((wall.X, wall.Z, wall.AlongZ));
                            plan.Furniture.Add(new Placement(item.Key, PlacementMode.Hang, 0, 0, Turns(wall.Facing), wall.Facing, wall.Line, at, item.Y, stash, r.Level));
                            return true;
                        }

                        foreach (var offset in Shuffled(Enumerable.Range(0, (span * M) - width + 1).ToList(), rng))
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
                            if (span > 1)
                            {
                                room.UsedWalls.Add(next);
                            }

                            plan.Furniture.Add(new Placement(item.Key, PlacementMode.Against, x, z, turns, wall.Facing, wall.Line, a, 0f, stash, r.Level));
                            return true;
                        }
                    }

                    return false;
                }

                case Spot.AtTable:
                {
                    // Along a table's long sides first, facing it (as the seven's tables: `VeilmouthInteriors.Table`).
                    foreach (var t in room.Tables)
                    {
                        var seats = new List<(int X, int Z, int Turns, bool Long)>();
                        for (var x = t.xMin; x < t.xMax; x++)
                        {
                            seats.Add((x, t.yMin - 1, 0, t.width >= t.height));
                            seats.Add((x, t.yMax, 2, t.width >= t.height));
                        }

                        for (var z = t.yMin; z < t.yMax; z++)
                        {
                            seats.Add((t.xMin - 1, z, 1, t.height > t.width));
                            seats.Add((t.xMax, z, 3, t.height > t.width));
                        }

                        foreach (var (x, z, turns, _) in seats.OrderByDescending(s => s.Long).ThenBy(_ => rng.Next()))
                        {
                            var cells = Cells(x, z, 1, 1).ToList();
                            if (Fits(room, cells) && (!solid || Clear(r, fp, x, z, turns)))
                            {
                                Take(room, cells, solid);
                                plan.Furniture.Add(new Placement(item.Key, PlacementMode.Put, x, z, turns, N, 0, 0, 0f, stash, r.Level));
                                return true;
                            }
                        }
                    }

                    return false;
                }

                case Spot.OnTable:
                {
                    if (room.Tables.Count == 0)
                    {
                        return false;
                    }

                    var t = room.Tables[rng.Next(room.Tables.Count)];
                    plan.Furniture.Add(new Placement(item.Key, PlacementMode.Put, t.x + rng.Next(t.width), t.y + rng.Next(t.height), rng.Next(4), N, 0, 0, item.Y, stash, r.Level));
                    return true;
                }

                case Spot.Ceiling:
                {
                    plan.Furniture.Add(new Placement(item.Key, PlacementMode.Put, r.X + ((r.Width - width) / 2), r.Z + ((r.Depth - depth) / 2), 0, N, 0, 0, item.Y > 0f ? item.Y : ChandelierHeight, stash, r.Level));
                    return true;
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
                        if (solid && (!Fits(room, cells) || !Clear(r, fp, x, z, turns) || Touches(room, BodyAt(fp, x, z, turns))))
                        {
                            continue;
                        }

                        if (solid)
                        {
                            room.Bodies.Add(BodyAt(fp, x, z, turns));
                        }

                        // A carpet lies under the rest, but never over a well or a stair.
                        if (!solid && cells.Any(c => room.Taken.Contains(c) && Kept(plan, r, c)))
                        {
                            continue;
                        }

                        Take(room, cells, solid);
                        if (item.Key.Contains("Table") || item.Key == "ViCounter")
                        {
                            room.Tables.Add(new RectInt(x, z, w, d));
                        }

                        plan.Furniture.Add(new Placement(item.Key, PlacementMode.Put, x, z, turns, N, 0, 0, 0f, stash, r.Level));
                        return true;
                    }

                    return false;
                }
            }
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

        /// <summary>True when the body overlaps a piece already put free by more than half the kit check's 1 cm.</summary>
        private static bool Touches(Room room, Rect body)
        {
            var inner = Rect.MinMaxRect(body.xMin + 0.005f, body.yMin + 0.005f, body.xMax - 0.005f, body.yMax - 0.005f);
            return room.Bodies.Any(b => b.Overlaps(inner));
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

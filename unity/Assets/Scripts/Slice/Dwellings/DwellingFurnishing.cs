using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Slice.Dwellings
{
    /// <summary>
    /// Furnishing by rules (INTERIOR_FURNISHING_RULES.md, VI-6): a piece belongs to a place, a place to a room, a room to
    /// a home. Places are data (§3): an anchor, where it stands, its companions and how they stand to it. Rooms list
    /// their places by wealth (§4). The planner fills a room in the order of G7, keeps every doorway, stair and place in
    /// reach of the others (G1), stands large pieces against walls and in corners (G2), builds a place symmetrically
    /// about its anchor where the room allows (G3), and puts small things on surfaces as part of their pieces (G5, kit
    /// sets). Every list and number here is an authoring default.
    /// </summary>
    public static partial class DwellingPlanner
    {
        // ------------------------------------------------------------------------------------------- keys

        /// <summary>Kit sets of Victorian Interiors: a base with the pack's small things on its surfaces (rules G5).</summary>
        public const string NightstandKey = "ViNightstand", NightstandPoorKey = "ViNightstandPoor", BasinKey = "ViBasinSet",
            BathKey = "ViBathSet", WashShelfKey = "ViShelfWash", SideTableKey = "ViTableRoundSet", SmallTableKey = "ViTableBSet",
            HallTableKey = "ViHallTable", CounterKey = "ViCounterSet", SideboardKey = "ViSideboardSet", ShopShelvesKey = "ViShopShelves",
            DresserKey = "ViDresser";

        /// <summary>
        /// The owner's Tripo furniture (furnishing rules §10, 2026-11-09): a true nightstand, with a candle and a book or,
        /// in a rich room, a table lamp; a wardrobe, a chest of drawers, a cheval glass, a screen, a wing chair, a piano,
        /// a gramophone, a sewing table, a coat stand, a floor vase, a glazed cabinet, the hearth's log basket and fire
        /// irons, a wall clock, and the rich home's sconces and the middling hall's lantern.
        /// </summary>
        public const string TrNightstandKey = "TrNightstand", TrNightstandLampKey = "TrNightstandLamp", TrDresserKey = "TrDresser",
            LogsKey = "TrLogHolder", FireToolsKey = "TrFireTools";

        public const string LampKey = "ViWallLamp", SconceKey = "TrSconce", LanternKey = "TrLantern";
        private const string PictureKey = "ViPicture";
        private static readonly string[] Pictures = Enumerable.Range(1, 8).Select(i => $"ViPicture{i}").ToArray();

        private const float PictureHeight = 1.6f, LampHeight = 2.2f, MirrorHeight = 1.5f;


        /// <summary>Authoring default, m: a piece this low may stand under a window (a bucket, a box).</summary>
        private const float LowHeight = 0.6f;

        /// <summary>Rules G5: what may lie on a floor, and the rooms whose floors may hold it.</summary>
        private static readonly HashSet<string> FloorThings = new HashSet<string> { "ViBox", "ViBucket", "BkBasket", "BkBasketB", "Crate", "Barrel" };

        private static readonly HashSet<RoomKind> FloorRooms = new HashSet<RoomKind>
        {
            RoomKind.Kitchen, RoomKind.Scullery, RoomKind.Pantry, RoomKind.Store, RoomKind.BoxRoom, RoomKind.BackRoom, RoomKind.Room,
            RoomKind.Washroom, RoomKind.Nursery, RoomKind.Office,
        };

        /// <summary>Rules G4: what a seat may face.</summary>
        private static readonly HashSet<string> SeatTargets = new HashSet<string>
        {
            "ViKitchenTable", "ViDiningTable", SmallTableKey, SideTableKey, "ViDesk", "TrDesk", FireplaceKey, "TrPiano", "TrSewing",
        };

        private static readonly HashSet<string> Seats = new HashSet<string> { "ViChair", "ViArmchair", "ViFootstool", "TrArmchair" };

        // ------------------------------------------------------------------------------------------- places (rules §3)

        private enum At
        {
            Wall,       // against any wall
            WallCentre, // against a wall, the place centred on it
            InnerWall,  // as WallCentre, on a wall with a room behind it where one exists (G8)
            Segment,    // against a wall, centred on one 3 m segment (a basin under its mirror)
            Opposite,   // against the wall facing the room's hearth, on its axis (a settee)
            Centre,     // a centre group in the room (a table and its chairs)
            Corner,     // in a corner (a plant, a coat stand)
            Sell,       // a shop's counter across the room and its shelves behind (rules §6)
            Racks,      // shelves along the walls (a store)
            Floor,      // small things on a store's or kitchen's floor, by the walls (G5)
            Stacks,     // barrels in the middle of a warehouse, its aisle clear (rules §6)
        }

        private enum Rel
        {
            Flank,      // a pair beside the anchor on its wall
            FlankOne,   // one beside the anchor on its wall
            FrontFlank, // a pair of seats beside the anchor's front, facing each other across it
            Before,     // centred in front of the anchor
            Seat,       // one seat at the anchor's front, facing it (a desk's chair)
            Seats,      // seats on a table's long sides, N on each, facing it
            Over,       // hung on the wall over the anchor
            Corners,    // a pair in the corners at the ends of the anchor's wall
        }

        private enum Use
        {
            None,  // reached from its companions
            Front, // the row before it
            Bed,   // the row before its foot and its sides
        }

        private sealed class With
        {
            public string Key;
            public Rel Rel;
            public DwellingWealth From, UpTo;
            public float Y;
            public int N;
        }

        private sealed class Place
        {
            public string Name;
            public At At;
            public string PoorKey, MiddlingKey, RichKey;
            public Use Use;
            public With[] With;
            public string[] Things; // Floor and Stacks: what they put, in turn

            public string Anchor(DwellingWealth w) => w == Rich ? RichKey : w == Middling ? MiddlingKey : PoorKey;
        }

        private static Place P(string name, At at, string poor, string middling, string rich, Use use, params With[] with) =>
            new Place { Name = name, At = at, PoorKey = poor, MiddlingKey = middling, RichKey = rich, Use = use, With = with };

        private static Place P(string name, At at, string key, Use use, params With[] with) => P(name, at, key, key, key, use, with);

        private static With C(string key, Rel rel, DwellingWealth from = Poor, DwellingWealth upTo = Rich, float y = 0f, int n = 1) =>
            new With { Key = key, Rel = rel, From = from, UpTo = upTo, Y = y, N = n };

        private static readonly Place Sleep = P("Sleep", At.WallCentre, "ViBed", Use.Bed,
            C(NightstandPoorKey, Rel.FlankOne, Poor, Poor), C(TrNightstandKey, Rel.Flank, Middling, Middling), C(TrNightstandLampKey, Rel.Flank, Rich),
            C(PictureKey, Rel.Over, Middling, y: 2.0f));

        private static readonly Place SleepPlain = P("Sleep", At.WallCentre, "ViBed", Use.Bed, C(NightstandPoorKey, Rel.FlankOne));

        private static readonly Place Dress = P("Dress", At.Wall, null, "TrWardrobe", "TrWardrobe", Use.Front,
            C("ViMirrorTall", Rel.FlankOne, Middling, Middling), C("TrMirror", Rel.FlankOne, Rich));

        /// <summary>A chest of drawers with a mirror over it (rules §3, Dress).</summary>
        private static readonly Place Drawers = P("Drawers", At.Wall, null, TrDresserKey, TrDresserKey, Use.Front, C("ViMirrorSmall", Rel.Over, Middling, y: MirrorHeight));

        private static readonly Place Screen = P("Screen", At.Wall, null, "TrScreen", "TrScreen", Use.None);

        private static readonly Place Coats = P("Coats", At.Corner, null, "ViCoatStand", "TrCoatRack", Use.None);

        // The hearth's log basket on either side and, from a middling home up, its fire irons on the other (rules G5:
        // what lies by a hearth); both give way to the hearth's chairs and the walk-ways.
        private static readonly Place Hearth = P("Hearth", At.InnerWall, FireplaceKey, Use.Front,
            C("ViChair", Rel.FrontFlank, Poor, Poor), C("ViArmchair", Rel.FrontFlank, Middling, Middling), C("TrArmchair", Rel.FrontFlank, Rich),
            C("ViRug", Rel.Before, Middling), C(LogsKey, Rel.FlankOne), C(FireToolsKey, Rel.FlankOne, Middling));

        private static readonly Place Cook = P("Cook", At.InnerWall, FireplaceKey, Use.Front, C(LogsKey, Rel.FlankOne));

        /// <summary>The kitchen-living room's hearth with a chair each side (rules §4, poor).</summary>
        private static readonly Place CookAndSit = P("Cook", At.InnerWall, FireplaceKey, Use.Front, C("ViChair", Rel.FrontFlank), C(LogsKey, Rel.FlankOne));

        private static readonly Place Dresser = P("Dresser", At.Wall, DresserKey, Use.Front);

        private static readonly Place Basin = P("Basin", At.Segment, BasinKey, Use.Front,
            C("ViBucket", Rel.FlankOne, Poor, Poor), C("ViMirrorSmall", Rel.Over, Middling, y: MirrorHeight));

        private static readonly Place Bath = P("Bath", At.WallCentre, BathKey, Use.Front);

        /// <summary>
        /// Measured: the toilet's and the basin's bodies come within 0.13 m and 0.11 m of their cells' sides, inside a
        /// side wall's 0.16 m, so neither stands in a corner; both stand in a segment's middle.
        /// </summary>
        private static readonly Place Toilet = P("Toilet", At.Segment, "ViToilet", Use.Front);

        /// <summary>
        /// A closet of one module (a poor washroom, a flat's WC): the toilet's body reaches 1.07 m from its wall, so in
        /// 3 × 3 m the basin's front cell lies under the toilet's footprint, while the floor between the door and the
        /// toilet's front (measured: 1.6 m deep) stays free; the basin is used from there.
        /// </summary>
        private static readonly Place ClosetBasin = P("Basin", At.Segment, BasinKey, Use.None,
            C("ViBucket", Rel.FlankOne, Poor, Poor), C("ViMirrorSmall", Rel.Over, Middling, y: MirrorHeight));

        private static readonly Place WashShelf = P("Wash shelf", At.Wall, null, WashShelfKey, WashShelfKey, Use.Front);

        private static readonly Place TowelRail = P("Towels", At.Wall, null, null, "ViTowelRail", Use.Front);

        private static readonly Place EatKitchen = P("Eat", At.Centre, "ViKitchenTable", Use.None,
            C("ViChair", Rel.Seats, Poor, Middling, n: 1), C("ViChair", Rel.Seats, Rich, Rich, n: 3));

        private static readonly Place EatSmall = P("Eat", At.Centre, SmallTableKey, Use.None, C("ViChair", Rel.Seats, n: 1));

        private static readonly Place Dine = P("Dine", At.Centre, "ViDiningTable", Use.None,
            C("ViChair", Rel.Seats, Poor, Middling, n: 2), C("ViChair", Rel.Seats, Rich, Rich, n: 4));

        private static readonly Place Sideboard = P("Sideboard", At.WallCentre, null, SideboardKey, SideboardKey, Use.Front);

        private static readonly Place Display = P("Display", At.Wall, null, "ViCupboardGlass", "TrDisplay", Use.Front);

        private static readonly Place Sit = P("Sit", At.Opposite, "ViSofa", "ViSofa", "ViSofaGreen", Use.Front,
            C(SideTableKey, Rel.Before, Middling), C(PictureKey, Rel.Over, Middling, y: 1.7f), C("ViPalm", Rel.Corners, Rich));

        private static readonly Place Books = P("Books", At.Wall, "ViBookcaseLow", "ViBookcaseLow", "ViBookcase", Use.Front);

        private static readonly Place Work = P("Work", At.Wall, "ViDesk", "ViDesk", "TrDesk", Use.None, C("ViChair", Rel.Seat),
            C("TrWallClock", Rel.Over, Rich, y: 1.6f));

        private static readonly Place WorkSmall = P("Table", At.Wall, SmallTableKey, Use.None, C("ViChair", Rel.Seat));

        private static readonly Place Clock = P("Clock", At.Wall, null, null, "ViClock", Use.Front);

        /// <summary>
        /// A rich drawing room's upright piano with a chair at its keys (rules §4; the pack lacked one, §8). The pack's
        /// footstool is 1.4 cm too wide for a seat cell by a wall (kit check, 2026-11-09), and it is a pouffe, not a stool.
        /// </summary>
        private static readonly Place Piano = P("Piano", At.Wall, null, null, "TrPiano", Use.None, C("ViChair", Rel.Seat));

        private static readonly Place Gramophone = P("Gramophone", At.Wall, null, null, "TrGramophone", Use.Front);

        /// <summary>A sewing table by the wall with its chair: the poor and middling home's work.</summary>
        private static readonly Place Sew = P("Sew", At.Wall, "TrSewing", "TrSewing", null, Use.None, C("ViChair", Rel.Seat));

        /// <summary>A rich floor vase in a corner (G6).</summary>
        private static readonly Place Vase = P("Vase", At.Corner, null, null, "TrVase", Use.None);

        private static readonly Place HallTable = P("Hall table", At.Wall, null, HallTableKey, HallTableKey, Use.Front,
            C("ViMirror", Rel.Over, Middling, y: MirrorHeight));

        /// <summary>Measured: only the palm's body keeps clear of both walls of a corner.</summary>
        private static readonly Place Plant = P("Plant", At.Corner, null, "ViPalm", "ViPalm", Use.None);

        private static readonly Place Settee = P("Seat", At.WallCentre, null, null, "ViSettee", Use.Front);

        private static readonly Place Play = P("Play", At.Centre, SideTableKey, Use.None, C("ViFootstool", Rel.Seats, n: 1));

        private static readonly Place Sell = P("Sell", At.Sell, CounterKey, Use.None);

        private static readonly Place Racks = P("Racks", At.Racks, ShopShelvesKey, Use.Front);

        private static readonly Place Goods = WithThings(P("Goods", At.Floor, "ViBox", Use.None), "ViBox", "BkBasket", "ViBucket", "BkBasketB");

        private static readonly Place Kitchenware = WithThings(P("Goods", At.Floor, "ViBucket", Use.None), "ViBucket", "BkBasket");

        private static readonly Place Toys = WithThings(P("Toys", At.Floor, "ViBox", Use.None), "ViBox");

        private static readonly Place Stacks = WithThings(P("Stacks", At.Stacks, "Barrel", Use.None), "Barrel", "Crate");

        private static Place WithThings(Place p, params string[] things)
        {
            p.Things = things;
            return p;
        }

        private static readonly Place[] AllPlaces =
        {
            Sleep, SleepPlain, Dress, Drawers, Screen, Coats, Hearth, Cook, CookAndSit, Dresser, Basin, ClosetBasin, Bath, Toilet, WashShelf, TowelRail,
            EatKitchen, EatSmall, Dine, Sideboard, Display, Sit, Books, Work, WorkSmall, Clock, Piano, Gramophone, Sew, Vase, HallTable, Plant, Settee,
            Play, Sell, Racks, Goods, Kitchenware, Toys, Stacks,
        };

        // ------------------------------------------------------------------------------------------- rooms (rules §4)

        private readonly struct Step
        {
            public Step(Place place, bool required, DwellingWealth from, DwellingWealth upTo, int count)
            {
                Place = place;
                Required = required;
                From = from;
                UpTo = upTo;
                Count = count;
            }

            public Place Place { get; }
            public bool Required { get; }
            public DwellingWealth From { get; }
            public DwellingWealth UpTo { get; }

            /// <summary>How many times; 0 for as many as the household has servants.</summary>
            public int Count { get; }
        }

        private static Step Must(Place p, DwellingWealth from = Poor, DwellingWealth upTo = Rich, int count = 1) => new Step(p, true, from, upTo, count);

        private static Step May(Place p, DwellingWealth from = Poor, DwellingWealth upTo = Rich, int count = 1) => new Step(p, false, from, upTo, count);

        /// <summary>Rules §4: each room's places, required first, then the optional ones in order.</summary>
        private static readonly Dictionary<RoomKind, Step[]> RoomRules = new Dictionary<RoomKind, Step[]>
        {
            [RoomKind.Hall] = new[] { May(HallTable, Middling), May(Clock, Rich), May(Coats, Middling), May(Plant, Rich), May(Vase, Rich) },
            [RoomKind.Landing] = new[] { May(Settee, Rich), May(Plant, Middling) },
            [RoomKind.Parlour] = new[] { Must(Hearth), May(Sit), May(Books, Middling), May(Sew, Middling, Middling) },
            [RoomKind.DrawingRoom] = new[] { Must(Hearth), Must(Sit), May(Piano, Rich), May(Books), May(Display), May(Gramophone, Rich), May(Vase, Rich) },
            [RoomKind.DiningRoom] = new[] { May(Hearth, Rich), Must(Dine), May(Sideboard), May(Display) },
            [RoomKind.Study] = new[] { May(Hearth, Rich), Must(Work), May(Books, count: 2) },
            [RoomKind.Kitchen] = new[]
            {
                Must(CookAndSit, Poor, Poor), Must(Cook, Middling), Must(EatKitchen), May(Dresser), May(Basin, Poor, Middling), May(Kitchenware),
                May(Sew, Poor, Poor),
            },
            [RoomKind.Scullery] = new[] { Must(Basin), May(Dresser), May(Kitchenware, count: 2) },
            [RoomKind.Bedroom] = new[] { May(Hearth, Rich), Must(Sleep), May(Dress, Middling), May(Drawers, Middling), May(Coats, Rich), May(Screen, Rich) },
            [RoomKind.DressingRoom] = new[] { Must(Dress), May(Drawers), May(Dress), May(Screen), May(Coats) },
            [RoomKind.Bathroom] = new[] { Must(Bath), Must(Toilet), Must(Basin), May(WashShelf), May(TowelRail) },
            [RoomKind.WC] = new[] { Must(Toilet), Must(ClosetBasin), May(WashShelf) },
            [RoomKind.Washroom] = new[] { Must(Toilet), Must(ClosetBasin) },
            [RoomKind.Nursery] = new[] { Must(SleepPlain, count: 2), May(Play), May(Toys, count: 2) },
            [RoomKind.ServantRoom] = new[] { Must(SleepPlain, count: 0), May(WorkSmall) },
            [RoomKind.BoxRoom] = new[] { May(Goods, count: 4) },
            [RoomKind.Room] = new[] { Must(Cook), Must(Sleep), May(EatSmall), May(Sew, Poor, Poor), May(Dresser), May(Kitchenware) },
            [RoomKind.ShopRoom] = new[] { Must(Sell) },
            [RoomKind.BackRoom] = new[]
            {
                Must(Cook, Poor, Poor), Must(Sleep, Poor, Middling), May(EatSmall, Poor, Poor), May(Work, Rich), May(Racks, Middling, count: 3),
                May(Goods, count: 2),
            },
            [RoomKind.Pantry] = new[] { May(Racks), May(Goods, count: 2) },
            [RoomKind.Store] = new[] { May(Racks, count: 8), May(Stacks) },
            [RoomKind.Office] = new[] { Must(Work), May(Books), May(Display) },
        };

        /// <summary>Where a household keeps its Stash, in order of preference.</summary>
        private static readonly RoomKind[] StashRooms =
        {
            RoomKind.Bedroom, RoomKind.Study, RoomKind.Room, RoomKind.BackRoom, RoomKind.DressingRoom, RoomKind.Office,
            RoomKind.BoxRoom, RoomKind.Store, RoomKind.Kitchen,
        };

        /// <summary>Every kit key the planner may place, so the kit asset holds them all.</summary>
        public static IEnumerable<string> Keys =>
            AllPlaces.SelectMany(p => new[] { p.PoorKey, p.MiddlingKey, p.RichKey }.Concat(p.With.Select(w => w.Key)).Concat(p.Things ?? Array.Empty<string>()))
                .Concat(new[] { ShopShelvesKey, "ViCupboardGlass", LampKey, SconceKey, LanternKey, "ViCarpet", "ViCarpetB", StashKey, StairKey, BalustradeKey, ChandelierKey })
                .Concat(Pictures)
                .Where(k => k != null && k != PictureKey).Distinct();

        // ------------------------------------------------------------------------------------------- state

        private sealed class Room
        {
            public DwellingRoom Data;

            /// <summary>No piece may stand here: doorways, the stair's foot and head, what is already placed.</summary>
            public readonly HashSet<(int, int)> Taken = new HashSet<(int, int)>();

            /// <summary>Nobody walks here: solid pieces, a stair, a well, a balustrade.</summary>
            public readonly HashSet<(int, int)> Blocked = new HashSet<(int, int)>();

            /// <summary>The ways into the room: each doorway's middle cell, the stair's foot or head.</summary>
            public readonly List<(int, int)> Entries = new List<(int, int)>();

            /// <summary>Each placed piece's use cells: at least one of each must stay in reach (G1).</summary>
            public readonly List<(int, int)[]> Uses = new List<(int, int)[]>();

            public readonly List<(int X, int Z, bool AlongZ, Vector2Int Facing, int Line)> Walls = new List<(int, int, bool, Vector2Int, int)>();

            /// <summary>The height a hung piece hangs at on each wall segment.</summary>
            public readonly Dictionary<(int, int, bool), float> Hung = new Dictionary<(int, int, bool), float>();

            /// <summary>The tallest piece standing against each wall segment.</summary>
            public readonly Dictionary<(int, int, bool), float> Heights = new Dictionary<(int, int, bool), float>();

            public readonly List<Rect> Bodies = new List<Rect>();

            /// <summary>Cells under a rug or a carpet: one never lies on another.</summary>
            public readonly HashSet<(int, int)> Covered = new HashSet<(int, int)>();

            public readonly List<Piece> Pieces = new List<Piece>();

            public bool Inside((int X, int Z) c) => c.X >= Data.X && c.X < Data.X + Data.Width && c.Z >= Data.Z && c.Z < Data.Z + Data.Depth;
        }

        private readonly struct Piece
        {
            public Piece(string key, string place, RectInt cells, Side? side, int along, int width, int depth)
            {
                Key = key;
                Place = place;
                Cells = cells;
                Side = side;
                Along = along;
                Width = width;
                Depth = depth;
            }

            public string Key { get; }
            public string Place { get; }
            public RectInt Cells { get; }
            public Side? Side { get; }
            public int Along { get; }
            public int Width { get; }
            public int Depth { get; }
        }

        /// <summary>One of a room's four walls, seen from inside: cells run along it from <see cref="Start"/>.</summary>
        private readonly struct Side
        {
            public Side(Vector2Int facing, int line, int start, int length, bool alongZ)
            {
                Facing = facing;
                Line = line;
                Start = start;
                Length = length;
                AlongZ = alongZ;
            }

            public Vector2Int Facing { get; }
            public int Line { get; }
            public int Start { get; }
            public int Length { get; }
            public bool AlongZ { get; }

            public Vector2Int U => AlongZ ? new Vector2Int(0, 1) : new Vector2Int(1, 0);

            /// <summary>The cell <paramref name="along"/> cells along the wall and <paramref name="deep"/> cells into the room.</summary>
            public (int, int) At(int along, int deep) =>
                AlongZ ? (Facing == E ? Line + deep : Line - 1 - deep, Start + along) : (Start + along, Facing == N ? Line + deep : Line - 1 - deep);

            /// <summary>The south-west corner of a footprint w along the wall and d into the room, from (along, deep).</summary>
            public (int X, int Z) Corner(int along, int deep, int w, int d)
            {
                if (Facing == N) return (Start + along, Line + deep);
                if (Facing == S) return (Start + along, Line - deep - d);
                if (Facing == E) return (Line + deep, Start + along);
                return (Line - deep - d, Start + along);
            }

            public (int, int, bool) Segment(int along) => AlongZ ? (Line, Start + (along / M * M), true) : (Start + (along / M * M), Line, false);

            public (int X, int Z, bool AlongZ, Vector2Int Facing, int Line) Wall(int along)
            {
                var (x, z, alongZ) = Segment(along);
                return (x, z, alongZ, Facing, Line);
            }

            /// <summary>How far a cell lies from this wall, in cells.</summary>
            public int Depth((int X, int Z) c) => AlongZ ? (Facing == E ? c.X - Line : Line - 1 - c.X) : (Facing == N ? c.Z - Line : Line - 1 - c.Z);

            public int AlongOf((int X, int Z) c) => (AlongZ ? c.Z : c.X) - Start;
        }

        private static Side[] SidesOf(DwellingRoom r) => new[]
        {
            new Side(N, r.Z, r.X, r.Width, false), new Side(S, r.Z + r.Depth, r.X, r.Width, false),
            new Side(E, r.X, r.Z, r.Depth, true), new Side(W, r.X + r.Width, r.Z, r.Depth, true),
        };

        private sealed class Job
        {
            public DwellingPlan Plan;
            public Room Room;
            public DwellingWealth Wealth;
            public Random Rng;
            public IDwellingFootprints Kit;
        }

        private sealed class Mark
        {
            public int Furniture, Bodies, Uses, Pieces;
            public HashSet<(int, int)> Taken, Blocked, Covered;
            public Dictionary<(int, int, bool), float> Hung, Heights;
        }

        private static Mark Begin(Job j) => new Mark
        {
            Furniture = j.Plan.Furniture.Count, Bodies = j.Room.Bodies.Count, Uses = j.Room.Uses.Count, Pieces = j.Room.Pieces.Count,
            Taken = new HashSet<(int, int)>(j.Room.Taken), Blocked = new HashSet<(int, int)>(j.Room.Blocked),
            Covered = new HashSet<(int, int)>(j.Room.Covered),
            Hung = new Dictionary<(int, int, bool), float>(j.Room.Hung), Heights = new Dictionary<(int, int, bool), float>(j.Room.Heights),
        };

        private static void Undo(Job j, Mark m)
        {
            j.Plan.Furniture.RemoveRange(m.Furniture, j.Plan.Furniture.Count - m.Furniture);
            j.Room.Bodies.RemoveRange(m.Bodies, j.Room.Bodies.Count - m.Bodies);
            j.Room.Uses.RemoveRange(m.Uses, j.Room.Uses.Count - m.Uses);
            j.Room.Pieces.RemoveRange(m.Pieces, j.Room.Pieces.Count - m.Pieces);
            Refill(j.Room.Taken, m.Taken);
            Refill(j.Room.Blocked, m.Blocked);
            Refill(j.Room.Covered, m.Covered);
            Refill(j.Room.Hung, m.Hung);
            Refill(j.Room.Heights, m.Heights);
        }

        private static void Refill<T>(HashSet<T> set, HashSet<T> from)
        {
            set.Clear();
            set.UnionWith(from);
        }

        private static void Refill<TK, TV>(Dictionary<TK, TV> map, Dictionary<TK, TV> from)
        {
            map.Clear();
            foreach (var kv in from)
            {
                map[kv.Key] = kv.Value;
            }
        }

        // ------------------------------------------------------------------------------------------- the planner (rules §7)

        private static void Furnish(DwellingPlan plan, DwellingWealth wealth, Random rng, IDwellingFootprints kit)
        {
            var rooms = plan.Rooms.Select(r => Prepare(plan, r)).ToList();
            Stairs(plan, rooms);
            var stashRoom = Array.FindIndex(StashRooms, k => rooms.Any(r => r.Data.Kind == k));
            var stash = rng.NextDouble() < StashChance(wealth) && stashRoom >= 0;

            foreach (var room in rooms)
            {
                var j = new Job { Plan = plan, Room = room, Wealth = wealth, Rng = rng, Kit = kit };
                foreach (var step in RoomRules[room.Data.Kind].Where(s => s.From <= wealth && wealth <= s.UpTo))
                {
                    var count = step.Count > 0 ? step.Count : Mathf.Max(1, plan.Servants);
                    for (var n = 0; n < count; n++)
                    {
                        if (!Fill(j, step.Place) && step.Required)
                        {
                            plan.Unplaced.Add($"{room.Data.Name}: {step.Place.Name}");
                        }
                    }
                }

                Decorate(j);
                if (stash && room.Data.Kind == StashRooms[stashRoom])
                {
                    stash = !Stash(j);
                }
            }
        }

        /// <summary>One place in the room, anchor first, then its companions (G7); false when its anchor found no spot.</summary>
        private static bool Fill(Job j, Place p)
        {
            var key = p.Anchor(j.Wealth);
            if (key == null || !j.Kit.TryGet(key, out var fp))
            {
                return false;
            }

            switch (p.At)
            {
                case At.Centre:
                    return Table(j, p, key, fp);
                case At.Corner:
                    return InCorner(j, key, p.Name);
                case At.Sell:
                    return Counter(j);
                case At.Racks:
                    return Rack(j, key);
                case At.Floor:
                    return FloorThing(j, p);
                case At.Stacks:
                    return Stack(j, p);
            }

            foreach (var (s, a) in WallSpots(j, p, key, fp))
            {
                var m = Begin(j);
                if (!Against(j, key, s, a, 0, p.Name) || !AddUse(j, AnchorUse(j, p.Use, s, a, fp)) || !Connected(j.Room))
                {
                    Undo(j, m);
                    continue;
                }

                foreach (var with in p.With.Where(w => w.From <= j.Wealth && j.Wealth <= w.UpTo))
                {
                    var mw = Begin(j);
                    if (!Companion(j, with, s, a, fp) || !Connected(j.Room))
                    {
                        Undo(j, mw);
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// The anchor's spots against the walls, best first: the place's whole group free on its wall, centred on the
        /// wall or segment its rule asks, away from the door, on an inner wall for a hearth (G2, G3, G8).
        /// </summary>
        private static IEnumerable<(Side, int)> WallSpots(Job j, Place p, string key, Footprint fp)
        {
            var r = j.Room.Data;
            var depth = Mathf.Max(fp.Depth, 1);
            var (left, right) = (0, 0);
            foreach (var w in p.With.Where(w => w.From <= j.Wealth && j.Wealth <= w.UpTo && (w.Rel == Rel.Flank || w.Rel == Rel.FlankOne)))
            {
                var cw = j.Kit.TryGet(w.Key, out var cf) ? cf.Width : 1;
                right = cw;
                left = w.Rel == Rel.Flank ? cw : 0;
            }

            var hearth = j.Room.Pieces.Where(x => x.Place == Hearth.Name && x.Side.HasValue).Select(x => (Piece?)x).FirstOrDefault();
            var spots = new List<(Side, int, double)>();
            foreach (var s in SidesOf(r))
            {
                var doorOnSide = Enumerable.Range(0, s.Length / M).Any(i => OpeningOf(j, s, i * M) is Opening.Door or Opening.Street);
                var inner = Enumerable.Range(0, s.Length / M).Any(i => OpeningOf(j, s, i * M) == Opening.Wall && Inner(j.Plan, r, s.Wall(i * M)));
                for (var a = 0; a + fp.Width <= s.Length; a++)
                {
                    var score = j.Rng.NextDouble() * 0.5;
                    var g0 = a - left;
                    var g1 = a + fp.Width + right;
                    if (g0 < 0 || g1 > s.Length || !GroupFree(j, s, g0, g1, depth))
                    {
                        score += 100;
                    }

                    var centre = (g0 + g1) * 0.5;
                    var segCentre = (Math.Floor((a + fp.Width * 0.5) / M) * M) + (M * 0.5);
                    switch (p.At)
                    {
                        case At.WallCentre:
                        case At.InnerWall:
                            score += Math.Abs(centre - s.Length * 0.5) * 2 + (doorOnSide ? 3 : 0);
                            if (p.At == At.InnerWall)
                            {
                                var run = Enumerable.Range(a, fp.Width).All(i => Inner(j.Plan, r, s.Wall(i)));
                                score += run ? 0 : inner ? 60 : 20;
                            }

                            break;
                        case At.Opposite:
                            if (hearth.HasValue)
                            {
                                var h = hearth.Value;
                                var axis = h.Along + (h.Width * 0.5);
                                score += (s.Facing == -h.Side.Value.Facing ? 0 : 30) + Math.Abs(centre - axis) * 2;
                            }
                            else
                            {
                                score += Math.Abs(centre - s.Length * 0.5) * 2 + (doorOnSide ? 3 : 0);
                            }

                            break;
                        case At.Segment:
                            score += Math.Abs((a + fp.Width * 0.5) - segCentre) * 10;
                            break;
                        default:
                            score += Math.Abs((a + fp.Width * 0.5) - segCentre);
                            break;
                    }

                    spots.Add((s, a, score));
                }
            }

            return spots.OrderBy(t => t.Item3).Select(t => (t.Item1, t.Item2));
        }

        private static Opening OpeningOf(Job j, Side s, int along)
        {
            var (x, z, alongZ) = s.Segment(along);
            return j.Plan.OpeningAt(x, z, alongZ, j.Room.Data.Level);
        }

        /// <summary>True when the cells from g0 to g1 along the wall, d deep, are free and the wall there is plain.</summary>
        private static bool GroupFree(Job j, Side s, int g0, int g1, int d)
        {
            for (var i = g0; i < g1; i++)
            {
                if (OpeningOf(j, s, i) != Opening.Wall)
                {
                    return false;
                }

                for (var k = 0; k < d; k++)
                {
                    var c = s.At(i, k);
                    if (!j.Room.Inside(c) || j.Room.Taken.Contains(c))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>A piece with its back on the wall <paramref name="s"/> at <paramref name="along"/>, or <paramref name="deep"/> cells out from it.</summary>
        private static bool Against(Job j, string key, Side s, int along, int deep, string place, bool stash = false)
        {
            if (!j.Kit.TryGet(key, out var fp))
            {
                return false;
            }

            var d = Mathf.Max(fp.Depth, 1);
            if (along < 0 || along + fp.Width > s.Length)
            {
                return false;
            }

            if (deep == 0)
            {
                var low = fp.Height <= LowHeight;
                for (var i = along; i < along + fp.Width; i++)
                {
                    var o = OpeningOf(j, s, i);
                    if (o != Opening.Wall && !(low && o == Opening.Window))
                    {
                        return false;
                    }
                }
            }

            var (x, z) = s.Corner(along, deep, fp.Width, d);
            return Put(j, key, x, z, Turns(s.Facing), deep == 0 ? s : (Side?)null, along, place, stash);
        }

        /// <summary>Puts a piece whose turned footprint's south-west corner is (x, z), if it fits the room and its rules.</summary>
        private static bool Put(Job j, string key, int x, int z, int turns, Side? side = null, int along = 0, string place = null, bool stash = false, float y = 0f)
        {
            if (!j.Kit.TryGet(key, out var fp))
            {
                return false;
            }

            var r = j.Room.Data;
            var depth = Mathf.Max(fp.Depth, 1);
            var (w, d) = turns % 2 == 0 ? (fp.Width, depth) : (depth, fp.Width);
            var cells = Cells(x, z, w, d).ToList();
            if (!cells.All(j.Room.Inside))
            {
                return false;
            }

            if (fp.Solid)
            {
                var body = BodyAt(fp, x, z, turns);
                if (cells.Any(j.Room.Taken.Contains) || !Clear(r, fp, x, z, turns) || Touches(j.Room, body))
                {
                    return false;
                }

                if (side.HasValue)
                {
                    for (var i = along; i < along + fp.Width; i++)
                    {
                        if (j.Room.Hung.TryGetValue(side.Value.Segment(i), out var hungAt) && fp.Height > hungAt - 0.05f)
                        {
                            return false; // it would stand in front of a picture or a mirror
                        }
                    }
                }

                j.Room.Bodies.Add(body);
                foreach (var c in cells)
                {
                    j.Room.Taken.Add(c);
                    j.Room.Blocked.Add(c);
                }

                if (side.HasValue)
                {
                    for (var i = along; i < along + fp.Width; i++)
                    {
                        var seg = side.Value.Segment(i);
                        j.Room.Heights[seg] = Mathf.Max(j.Room.Heights.TryGetValue(seg, out var h) ? h : 0f, fp.Height);
                    }
                }
            }
            else if (cells.Any(c => Kept(j.Plan, r, c) || j.Room.Covered.Contains(c)))
            {
                return false; // a carpet never lies over a stair, a well or another rug
            }
            else
            {
                j.Room.Covered.UnionWith(cells);
            }

            var s = side.GetValueOrDefault();
            j.Plan.Furniture.Add(new Placement(key, side.HasValue ? PlacementMode.Against : PlacementMode.Put, x, z, turns, side.HasValue ? s.Facing : N,
                side.HasValue ? s.Line : 0, side.HasValue ? s.Start + along : 0, y, stash, r.Level));
            j.Room.Pieces.Add(new Piece(key, place, new RectInt(x, z, w, d), side, along, fp.Width, depth));
            return true;
        }

        /// <summary>A hung piece on the wall segment that holds <paramref name="along"/>, over what stands below it.</summary>
        private static bool Hang(Job j, string key, Side s, int along, float y)
        {
            var seg = s.Segment(along);
            if (OpeningOf(j, s, along) != Opening.Wall || j.Room.Hung.ContainsKey(seg) ||
                (j.Room.Heights.TryGetValue(seg, out var h) && h > y - 0.05f) || !j.Kit.TryGet(key, out _))
            {
                return false;
            }

            j.Room.Hung[seg] = y;
            j.Plan.Furniture.Add(new Placement(key, PlacementMode.Hang, 0, 0, Turns(s.Facing), s.Facing, s.Line, s.Start + (along / M * M), y, false, j.Room.Data.Level));
            return true;
        }

        private static (int, int)[] AnchorUse(Job j, Use use, Side s, int a, Footprint fp)
        {
            var d = Mathf.Max(fp.Depth, 1);
            var cells = new List<(int, int)>();
            if (use == Use.None)
            {
                return null;
            }

            cells.AddRange(Enumerable.Range(a, fp.Width).Select(i => s.At(i, d)));
            if (use == Use.Bed)
            {
                for (var k = 1; k < d; k++)
                {
                    cells.Add(s.At(a - 1, k));
                    cells.Add(s.At(a + fp.Width, k));
                }
            }

            return cells.Where(j.Room.Inside).ToArray();
        }

        /// <summary>Records what must stay in reach; false when nothing of it lies in the room.</summary>
        private static bool AddUse(Job j, (int, int)[] cells)
        {
            if (cells == null)
            {
                return true;
            }

            if (cells.Length == 0)
            {
                return false;
            }

            j.Room.Uses.Add(cells);
            return true;
        }

        private static (int, int)[] Around((int X, int Z) c, Vector2Int except) =>
            new[] { N, S, E, W }.Where(d => d != except).Select(d => (c.X + d.x, c.Z + d.y)).ToArray();

        private static bool Companion(Job j, With with, Side s, int a, Footprint anchor)
        {
            var ad = Mathf.Max(anchor.Depth, 1);
            if (!j.Kit.TryGet(with.Key == PictureKey ? Pictures[0] : with.Key, out var fp))
            {
                return false;
            }

            var cw = fp.Width;
            var cd = Mathf.Max(fp.Depth, 1);
            switch (with.Rel)
            {
                case Rel.Flank:
                    return Against(j, with.Key, s, a - cw, 0, null) && Against(j, with.Key, s, a + anchor.Width, 0, null);
                case Rel.FlankOne:
                    return j.Rng.Next(2) == 0
                        ? Against(j, with.Key, s, a + anchor.Width, 0, null) || Against(j, with.Key, s, a - cw, 0, null)
                        : Against(j, with.Key, s, a - cw, 0, null) || Against(j, with.Key, s, a + anchor.Width, 0, null);
                case Rel.FrontFlank:
                {
                    // Beside the anchor's front row, each facing across it (a chair each side of the hearth).
                    var deep = ad - 1;
                    var lc = s.At(a - 1, deep);
                    var rc = s.At(a + anchor.Width, deep);
                    return Put(j, with.Key, lc.Item1, lc.Item2, Turns(s.U)) && AddUse(j, Around(lc, s.U).Where(j.Room.Inside).ToArray())
                        && Put(j, with.Key, rc.Item1, rc.Item2, Turns(-s.U)) && AddUse(j, Around(rc, -s.U).Where(j.Room.Inside).ToArray());
                }
                case Rel.Before:
                {
                    var (x, z) = s.Corner(a + ((anchor.Width - cw) / 2), ad, cw, cd);
                    if (!Put(j, with.Key, x, z, Turns(s.Facing)))
                    {
                        return false;
                    }

                    if (!fp.Solid)
                    {
                        j.Room.Taken.UnionWith(Cells(x, z, cw, cd)); // nothing stands on the hearth rug
                    }

                    return true;
                }
                case Rel.Seat:
                {
                    var c = s.At(a + ((anchor.Width - 1) / 2), ad);
                    return Put(j, with.Key, c.Item1, c.Item2, Turns(-s.Facing)) && AddUse(j, Around(c, -s.Facing).Where(j.Room.Inside).ToArray());
                }
                case Rel.Over:
                {
                    // Over the anchor's middle when that falls within a segment's middle metre, so the two line up.
                    var centre = a + (anchor.Width * 0.5f);
                    var seg = Mathf.FloorToInt(centre / M);
                    var key = with.Key == PictureKey ? Pictures[j.Rng.Next(Pictures.Length)] : with.Key;
                    return Mathf.Abs(centre - ((seg * M) + (M * 0.5f))) <= 0.5f && Hang(j, key, s, seg * M, with.Y);
                }
                case Rel.Corners:
                {
                    var (x0, z0) = s.Corner(0, 0, cw, cd);
                    var (x1, z1) = s.Corner(s.Length - cw, 0, cw, cd);
                    return Put(j, with.Key, x0, z0, Turns(s.Facing)) && Put(j, with.Key, x1, z1, Turns(s.Facing));
                }
                default:
                    return false;
            }
        }

        /// <summary>
        /// A centre group (G2, G3): the table in the room's middle along its long axis, its seats on its long sides, as
        /// many on each as the wealth asks and the room allows, centred on the table.
        /// </summary>
        private static bool Table(Job j, Place p, string key, Footprint fp)
        {
            var r = j.Room.Data;
            var seats = p.With.FirstOrDefault(w => w.Rel == Rel.Seats && w.From <= j.Wealth && j.Wealth <= w.UpTo);
            var turnsAlong = (r.Width >= r.Depth) == (fp.Width >= fp.Depth) ? 0 : 1;
            var spots = new List<(int X, int Z, int Turns, double Score)>();
            foreach (var turns in new[] { turnsAlong, 1 - turnsAlong })
            {
                var (w, d) = turns == 0 ? (fp.Width, Mathf.Max(fp.Depth, 1)) : (Mathf.Max(fp.Depth, 1), fp.Width);
                var cx = r.X + ((r.Width - w) * 0.5);
                var cz = r.Z + ((r.Depth - d) * 0.5);
                for (var x = r.X; x + w <= r.X + r.Width; x++)
                {
                    for (var z = r.Z; z + d <= r.Z + r.Depth; z++)
                    {
                        spots.Add((x, z, turns, Math.Abs(x - cx) + Math.Abs(z - cz) + (turns == turnsAlong ? 0 : 2) + j.Rng.NextDouble() * 0.3));
                    }
                }
            }

            var best = spots.OrderBy(t => t.Score).Take(60).ToList();
            // Seats on both long sides before seats on one, and as many as the wealth asks before fewer (G3): a table
            // moves along the room before it loses a chair.
            var tries = seats == null
                ? new[] { (Both: true, N: 0) }
                : new[] { true, false }.SelectMany(both => Enumerable.Range(1, seats.N).Reverse().Select(n => (Both: both, N: n))).ToArray();
            foreach (var (both, n) in tries)
            {
                foreach (var (x, z, turns, _) in best)
                {
                    var m = Begin(j);
                    if (!Put(j, key, x, z, turns, null, 0, p.Name) || !Connected(j.Room))
                    {
                        Undo(j, m);
                        continue;
                    }

                    if (seats == null)
                    {
                        return true;
                    }

                    var table = j.Room.Pieces[j.Room.Pieces.Count - 1].Cells;
                    if (SeatsAt(j, seats.Key, table, n, both) && Connected(j.Room))
                    {
                        return true;
                    }

                    Undo(j, m);
                }
            }

            return false;
        }

        private static bool SeatsAt(Job j, string key, RectInt t, int n, bool both)
        {
            var alongX = t.width >= t.height;
            var length = alongX ? t.width : t.height;
            if (n > length)
            {
                return false;
            }

            var start = (length - n) / 2;
            var sides = alongX
                ? new[] { (Row: t.yMin - 1, Turns: 0, Toward: N), (Row: t.yMax, Turns: 2, Toward: S) }
                : new[] { (Row: t.xMin - 1, Turns: 1, Toward: E), (Row: t.xMax, Turns: 3, Toward: W) };
            var order = j.Rng.Next(2) == 0 ? sides : sides.Reverse().ToArray();
            foreach (var side in both ? order : order.Take(1))
            {
                for (var i = start; i < start + n; i++)
                {
                    var c = alongX ? (t.xMin + i, side.Row) : (side.Row, t.yMin + i);
                    if (!Put(j, key, c.Item1, c.Item2, side.Turns, null, 0, "Seat") || !AddUse(j, Around(c, side.Toward).Where(j.Room.Inside).ToArray()))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>A piece in a corner, the corner nearest the way in first (a coat stand by the door).</summary>
        private static bool InCorner(Job j, string key, string place)
        {
            if (!j.Kit.TryGet(key, out var fp))
            {
                return false;
            }

            var entry = j.Room.Entries.Count > 0 ? j.Room.Entries[0] : (j.Room.Data.X, j.Room.Data.Z);
            var spots = new List<(Side S, int A, double Score)>();
            foreach (var s in SidesOf(j.Room.Data))
            {
                foreach (var a in new[] { 0, s.Length - fp.Width })
                {
                    var c = s.At(a, 0);
                    spots.Add((s, a, Mathf.Abs(c.Item1 - entry.Item1) + Mathf.Abs(c.Item2 - entry.Item2) + j.Rng.NextDouble()));
                }
            }

            foreach (var (s, a, _) in spots.OrderBy(t => t.Score))
            {
                var m = Begin(j);
                var (x, z) = s.Corner(a, 0, fp.Width, Mathf.Max(fp.Depth, 1));
                if (Put(j, key, x, z, Turns(s.Facing), null, 0, place) && Connected(j.Room))
                {
                    return true;
                }

                Undo(j, m);
            }

            return false;
        }

        /// <summary>
        /// Rules §6: the shop greets the customer with its counter across most of the room's width, a third of the way in,
        /// facing the way in, with its flap at the end nearer the door to the back; the shelves of goods stand on the wall
        /// behind it, and a glazed cupboard on each side wall in the customers' half.
        /// </summary>
        private static bool Counter(Job j)
        {
            var r = j.Room.Data;
            var sides = SidesOf(r);
            Side? way = null;
            foreach (var s in sides)
            {
                for (var i = 0; i < s.Length; i += M)
                {
                    var o = OpeningOf(j, s, i);
                    if (o == Opening.Street || (o == Opening.Door && way == null && Beyond(j, s, i) is { Kind: RoomKind.Hall }))
                    {
                        way = s;
                    }
                }
            }

            if (way == null)
            {
                return false;
            }

            var front = way.Value;
            var back = sides.First(s => s.Facing == -front.Facing);
            var across = front.AlongZ ? r.Width : r.Depth;
            var count = (front.Length - 1) / M;
            var otherDoors = sides.Where(s => s.Facing != front.Facing)
                .SelectMany(s => Enumerable.Range(0, s.Length / M).Where(i => OpeningOf(j, s, i * M) == Opening.Door).Select(i => front.AlongOf(s.At((i * M) + 1, 0))))
                .ToList();
            var flapHigh = otherDoors.Count > 0 ? otherDoors.Average() >= front.Length * 0.5 : j.Rng.Next(2) == 0;
            var run0 = flapHigh ? 0 : front.Length - (count * M);
            if (!j.Kit.TryGet(CounterKey, out var counter))
            {
                return false;
            }

            var placed = false;
            foreach (var k in new[] { Mathf.Max(3, across / 3), Mathf.Max(3, across / 3) + 1, 2 })
            {
                var m = Begin(j);
                var ok = true;
                for (var i = 0; i < count && ok; i++)
                {
                    var (x, z) = front.Corner(run0 + (i * M), k, counter.Width, Mathf.Max(counter.Depth, 1));
                    ok = Put(j, CounterKey, x, z, Turns(-front.Facing), null, 0, Sell.Name);
                }

                var run = Enumerable.Range(run0, count * M);
                ok = ok && AddUse(j, run.Select(i => front.At(i, k - 1)).Where(j.Room.Inside).ToArray())
                        && AddUse(j, run.Select(i => front.At(i, k + Mathf.Max(counter.Depth, 1))).Where(j.Room.Inside).ToArray())
                        && Connected(j.Room);
                if (ok)
                {
                    placed = true;
                    // The shelves behind, from the counter's middle outwards.
                    if (j.Kit.TryGet(ShopShelvesKey, out var shelf))
                    {
                        var middle = run0 + (count * M * 0.5);
                        foreach (var a in Enumerable.Range(0, back.Length - shelf.Width + 1).OrderBy(a => Math.Abs(a + (shelf.Width * 0.5) - middle)))
                        {
                            var ms = Begin(j);
                            if (!Against(j, ShopShelvesKey, back, a, 0, "Shelves") || !AddUse(j, AnchorUse(j, Use.Front, back, a, shelf)) || !Connected(j.Room))
                            {
                                Undo(j, ms);
                            }
                        }
                    }

                    // A glazed cupboard on each side wall in the customers' half, at the same distance from the way in.
                    if (j.Kit.TryGet("ViCupboardGlass", out var display))
                    {
                        var pair = sides.Where(s => s.AlongZ != front.AlongZ).ToArray();
                        for (var a = 0; a + display.Width <= pair[0].Length; a++)
                        {
                            var cells = Enumerable.Range(a, display.Width).Select(i => pair[0].At(i, 0)).ToList();
                            if (cells.Any(c => front.Depth(c) > k - 2))
                            {
                                continue;
                            }

                            var md = Begin(j);
                            if (pair.All(s => Against(j, "ViCupboardGlass", s, a, 0, "Display") && AddUse(j, AnchorUse(j, Use.Front, s, a, display))) && Connected(j.Room))
                            {
                                break;
                            }

                            Undo(j, md);
                        }
                    }

                    break;
                }

                Undo(j, m);
            }

            return placed;
        }

        /// <summary>The room on the other side of a wall segment, if any.</summary>
        private static DwellingRoom? Beyond(Job j, Side s, int along)
        {
            var (x, z) = s.At(along + 1, -1);
            foreach (var o in j.Plan.Rooms)
            {
                if (o.Level == j.Room.Data.Level && o.Contains(x + 0.5f, z + 0.5f))
                {
                    return o;
                }
            }

            return null;
        }

        /// <summary>Shelves along the walls (a store), each where it keeps the room's ways open.</summary>
        private static bool Rack(Job j, string key)
        {
            if (!j.Kit.TryGet(key, out var fp))
            {
                return false;
            }

            foreach (var s in Shuffled(SidesOf(j.Room.Data), j.Rng))
            {
                for (var a = 0; a + fp.Width <= s.Length; a++)
                {
                    var m = Begin(j);
                    if (Against(j, key, s, a, 0, Racks.Name) && AddUse(j, AnchorUse(j, Use.Front, s, a, fp)) && Connected(j.Room))
                    {
                        return true;
                    }

                    Undo(j, m);
                }
            }

            return false;
        }

        /// <summary>One small thing on the floor by a wall, where G5 allows it.</summary>
        private static bool FloorThing(Job j, Place p)
        {
            var key = p.Things[j.Rng.Next(p.Things.Length)];
            if (!j.Kit.TryGet(key, out var fp))
            {
                return false;
            }

            var spots = SidesOf(j.Room.Data).SelectMany(s => Enumerable.Range(0, s.Length - fp.Width + 1).Select(a => (s, a))).ToList();
            foreach (var (s, a) in Shuffled(spots, j.Rng))
            {
                var m = Begin(j);
                if (Against(j, key, s, a, 0, p.Name) && Connected(j.Room))
                {
                    return true;
                }

                Undo(j, m);
            }

            return false;
        }

        /// <summary>
        /// Rules §6: a warehouse keeps an aisle from its door straight through the store, and its barrels stand in
        /// clusters of four off the walls, a crate between them.
        /// </summary>
        private static bool Stack(Job j, Place p)
        {
            var r = j.Room.Data;
            foreach (var s in SidesOf(r))
            {
                for (var i = 0; i < s.Length; i += M)
                {
                    if (OpeningOf(j, s, i) == Opening.Street)
                    {
                        for (var k = 0; k < (s.AlongZ ? r.Width : r.Depth); k++)
                        {
                            for (var u = i; u < i + M; u++)
                            {
                                j.Room.Taken.Add(s.At(u, k)); // the aisle, 3 m wide
                            }
                        }
                    }
                }
            }

            var any = false;
            for (var x = r.X + 1; x + 2 <= r.X + r.Width - 1; x += 3)
            {
                for (var z = r.Z + 1; z + 2 <= r.Z + r.Depth - 1; z += 3)
                {
                    var m = Begin(j);
                    var barrels = Cells(x, z, 2, 2).All(c => Put(j, p.Things[0], c.Item1, c.Item2, 0, null, 0, p.Name));
                    if (barrels && Connected(j.Room))
                    {
                        any = true;
                        continue;
                    }

                    Undo(j, m);
                    var mc = Begin(j);
                    if (!(Put(j, p.Things[1], x, z, 0, null, 0, p.Name) && Connected(j.Room)))
                    {
                        Undo(j, mc);
                    }
                }
            }

            return any;
        }

        // ------------------------------------------------------------------------------------------- decoration (G9)

        private static readonly HashSet<RoomKind> PictureRooms = new HashSet<RoomKind>
        {
            RoomKind.Parlour, RoomKind.DrawingRoom, RoomKind.DiningRoom, RoomKind.Study, RoomKind.Bedroom, RoomKind.Hall, RoomKind.Landing,
            RoomKind.Nursery, RoomKind.Room,
        };

        private static readonly HashSet<RoomKind> Unlit = new HashSet<RoomKind> { RoomKind.Pantry, RoomKind.BoxRoom, RoomKind.Washroom, RoomKind.WC };

        /// <summary>
        /// Light follows use (G9): wall lamps beside the places used at night, one more in a rich main room; a chandelier
        /// hangs only under a ceiling higher than one storey, which no home has (owner, VI-8 remark 7); pictures on what
        /// wall is left; a carpet under a rich room's group. A poor home is lit by the candles of its sets.
        /// </summary>
        private static void Decorate(Job j)
        {
            var r = j.Room.Data;
            var kind = r.Kind;
            if (j.Wealth != Poor && !Unlit.Contains(kind))
            {
                var main = j.Wealth == Rich && (kind == RoomKind.DrawingRoom || kind == RoomKind.DiningRoom || kind == RoomKind.Hall);
                var lamps = (j.Wealth == Rich && r.Width * r.Depth >= 72 ? 2 : 1) + (main ? 1 : 0);

                // A rich home's brass sconces; a middling hall's and landing's bracket lantern (furnishing rules §10).
                var lamp = j.Wealth == Rich ? SconceKey : kind == RoomKind.Hall || kind == RoomKind.Landing ? LanternKey : LampKey;
                for (var n = 0; n < lamps; n++)
                {
                    HangNear(j, lamp, LampHeight);
                }
            }

            if (PictureRooms.Contains(kind))
            {
                var pictures = j.Wealth == Rich ? 2 : j.Wealth == Middling ? 1 : kind == RoomKind.Room ? 1 : 0;
                for (var n = 0; n < pictures; n++)
                {
                    HangNear(j, Pictures[j.Rng.Next(Pictures.Length)], PictureHeight);
                }
            }

            var carpet = j.Wealth == Rich
                ? kind == RoomKind.DrawingRoom || kind == RoomKind.Bedroom ? "ViCarpet" : kind == RoomKind.DiningRoom || kind == RoomKind.Study ? "ViCarpetB" : null
                : null;
            if (carpet != null && j.Kit.TryGet(carpet, out var cf))
            {
                var target = j.Room.Pieces.Where(p => p.Place == Dine.Name).Select(p => p.Cells.center).DefaultIfEmpty(new Vector2(r.X + r.Width * 0.5f, r.Z + r.Depth * 0.5f)).First();
                var spots = new List<(int X, int Z, int Turns, float Score)>();
                foreach (var turns in new[] { 0, 1 })
                {
                    var (w, d) = turns == 0 ? (cf.Width, cf.Depth) : (cf.Depth, cf.Width);
                    for (var x = r.X; x + w <= r.X + r.Width; x++)
                    {
                        for (var z = r.Z; z + d <= r.Z + r.Depth; z++)
                        {
                            spots.Add((x, z, turns, Mathf.Abs(x + (w * 0.5f) - target.x) + Mathf.Abs(z + (d * 0.5f) - target.y)));
                        }
                    }
                }

                // A carpet lies under a centre group or on open floor, never under a piece against a wall.
                var group = new HashSet<(int, int)>(j.Room.Pieces.Where(p => p.Place == Dine.Name || p.Place == "Seat")
                    .SelectMany(p => Cells(p.Cells.x, p.Cells.y, p.Cells.width, p.Cells.height)));
                foreach (var (x, z, turns, _) in spots.OrderBy(t => t.Score))
                {
                    var (w, d) = turns == 0 ? (cf.Width, cf.Depth) : (cf.Depth, cf.Width);
                    if (Cells(x, z, w, d).All(c => !j.Room.Blocked.Contains(c) || group.Contains(c)) && Put(j, carpet, x, z, turns))
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>A hung piece on a free wall segment, the one nearest the room's anchors first.</summary>
        private static void HangNear(Job j, string key, float y)
        {
            var anchors = j.Room.Pieces.Where(p => p.Place != null).Select(p => p.Cells.center).ToList();
            var spots = new List<(Side S, int A, double Score)>();
            foreach (var s in SidesOf(j.Room.Data))
            {
                for (var a = 0; a < s.Length; a += M)
                {
                    var c = s.At(a + 1, 0);
                    var near = anchors.Count == 0 ? 0 : anchors.Min(p => Mathf.Abs(p.x - c.Item1) + Mathf.Abs(p.y - c.Item2));
                    spots.Add((s, a, near + j.Rng.NextDouble() * 2));
                }
            }

            foreach (var (s, a, _) in spots.OrderBy(t => t.Score))
            {
                if (Hang(j, key, s, a, y))
                {
                    return;
                }
            }
        }

        /// <summary>The Stash (19 §6.3): at the foot of the bed where one stands, else against a wall.</summary>
        private static bool Stash(Job j)
        {
            foreach (var bed in j.Room.Pieces.Where(p => p.Key == "ViBed" && p.Side.HasValue).ToList())
            {
                var s = bed.Side.Value;
                var m = Begin(j);
                var (x, z) = s.Corner(bed.Along + ((bed.Width - 1) / 2), bed.Depth, 1, 1);
                if (Put(j, StashKey, x, z, Turns(s.Facing), null, 0, null, true) && Connected(j.Room))
                {
                    return true;
                }

                Undo(j, m);
            }

            var spots = SidesOf(j.Room.Data).SelectMany(s => Enumerable.Range(0, s.Length).Select(a => (s, a))).ToList();
            foreach (var (s, a) in Shuffled(spots, j.Rng))
            {
                var m = Begin(j);
                if (Against(j, StashKey, s, a, 0, null, true) && Connected(j.Room))
                {
                    return true;
                }

                Undo(j, m);
            }

            return false;
        }

        // ------------------------------------------------------------------------------------------- reach (G1)

        /// <summary>True when every way in, and a use cell of every placed piece, lie in reach of the first way in.</summary>
        private static bool Connected(Room room)
        {
            if (room.Entries.Count == 0)
            {
                return room.Uses.Count == 0;
            }

            var seen = Reach(room, room.Entries[0]);
            return room.Entries.All(seen.Contains) && room.Uses.All(u => u.Any(seen.Contains));
        }

        private static HashSet<(int, int)> Reach(Room room, (int, int) from)
        {
            var seen = new HashSet<(int, int)>();
            if (room.Blocked.Contains(from))
            {
                return seen;
            }

            var queue = new Queue<(int X, int Z)>();
            seen.Add(from);
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var (x, z) = queue.Dequeue();
                foreach (var d in new[] { N, S, E, W })
                {
                    var c = (x + d.x, z + d.y);
                    if (room.Inside(c) && !room.Blocked.Contains(c) && seen.Add(c))
                    {
                        queue.Enqueue(c);
                    }
                }
            }

            return seen;
        }

        // ------------------------------------------------------------------------------------------- the check (rules §7.5)

        /// <summary>
        /// Rules §7.5: what the furnishing rules promise, read back from a finished plan. Every home can sleep, cook and
        /// wash; every room's ways in reach one another past its furniture (G1); nothing stands in a doorway; small things
        /// lie on floors only where G5 allows; every seat faces a table, a desk or a hearth (G4). Empty when the plan keeps
        /// them all.
        /// </summary>
        public static List<string> Audit(DwellingPlan plan, IDwellingFootprints kit, bool home)
        {
            var faults = new List<string>();
            var rooms = plan.Rooms.Select(r => Prepare(plan, r)).ToList();
            var furniture = plan.Furniture.ToList();
            plan.Furniture.RemoveAll(f => f.Name != null);
            Stairs(plan, rooms); // puts the stair and its balustrades back as they were
            plan.Furniture.Clear();
            plan.Furniture.AddRange(furniture);
            var doorCells = rooms.ToDictionary(r => r, r => new HashSet<(int, int)>(r.Taken));

            var covers = new List<(Placement F, RectInt Cells, Room Room, Footprint Fp)>();
            foreach (var f in plan.Furniture.Where(f => f.Mode != PlacementMode.Hang && f.Name == null))
            {
                if (!kit.TryGet(f.Key, out var fp))
                {
                    faults.Add($"{f.Key}: not in the kit");
                    continue;
                }

                var depth = Mathf.Max(fp.Depth, 1);
                var (w, d) = f.Turns % 2 == 0 ? (fp.Width, depth) : (depth, fp.Width);
                var cells = new RectInt(f.X, f.Z, w, d);
                var room = rooms.FirstOrDefault(r => r.Data.Level == f.Level && r.Inside((f.X, f.Z)));
                if (room == null || f.Key == ChandelierKey)
                {
                    continue;
                }

                covers.Add((f, cells, room, fp));
                if (!fp.Solid)
                {
                    continue;
                }

                foreach (var c in cells.allPositionsWithin)
                {
                    room.Blocked.Add((c.x, c.y));
                    if (doorCells[room].Contains((c.x, c.y)))
                    {
                        faults.Add($"{room.Data.Name}: {f.Key} stands in a doorway or on a stair's foot");
                    }
                }

                if (FloorThings.Contains(f.Key) && !FloorRooms.Contains(room.Data.Kind))
                {
                    faults.Add($"{room.Data.Name}: {f.Key} lies on the floor (G5)");
                }
            }

            foreach (var room in rooms.Where(r => r.Entries.Count > 0))
            {
                var seen = Reach(room, room.Entries[0]);
                if (!room.Entries.All(seen.Contains))
                {
                    faults.Add($"{room.Data.Name}: its ways in are cut off from one another (G1)");
                }
            }

            foreach (var (f, cells, room, _) in covers.Where(c => Seats.Contains(c.F.Key)))
            {
                var facing = new[] { N, E, S, W }[f.Turns];
                var faces = false;
                for (var k = 1; k <= 2 && !faces; k++)
                {
                    var c = (cells.x + (facing.x * k), cells.y + (facing.y * k));
                    faces = covers.Any(o => o.Room == room && SeatTargets.Contains(o.F.Key) && o.Cells.Contains(new Vector2Int(c.Item1, c.Item2)));
                }

                if (!faces)
                {
                    faults.Add($"{room.Data.Name}: a {f.Key} at ({f.X}, {f.Z}) faces no table, desk or hearth (G4)");
                }
            }

            if (home)
            {
                var keys = plan.Furniture.Select(f => f.Key).ToList();
                if (!keys.Contains("ViBed"))
                {
                    faults.Add("the home has nowhere to sleep");
                }

                var cooks = covers.Any(c => c.F.Key == FireplaceKey && (c.Room.Data.Kind == RoomKind.Kitchen || c.Room.Data.Kind == RoomKind.Room || c.Room.Data.Kind == RoomKind.BackRoom));
                if (!cooks)
                {
                    faults.Add("the home has nowhere to cook");
                }

                if (!keys.Contains(BasinKey) || !keys.Contains("ViToilet"))
                {
                    faults.Add("the home has nowhere to wash");
                }
            }

            return faults;
        }
    }
}

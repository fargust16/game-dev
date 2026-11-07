using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Victorian Interiors as kit blocks (VICTORIAN_INTERIORS_PLAN §3, owner 2026-11-07): the families <c>ViRoom</c>,
    /// the architecture on the pack's 3 m module, and <c>ViFurniture</c>, everything else. The pack is built at game
    /// scale already, so nothing is scaled (measured: a table 0.93 m, a wall 3.5 m). Its materials are URP Lit.
    /// </summary>
    public static partial class SliceKitCatalogue
    {
        private const string Vi = "Assets/Victorian Interiors/Victorian Interiors/Prefabs";

        /// <summary>Measured (probe of 2026-11-07): the pack's walls, floor and ceiling are 3 m modules.</summary>
        public const int ViModule = 3;

        /// <summary>Measured: a storey's walls stand 3.5 m, and the ceiling's top is there.</summary>
        public const float ViStorey = 3.5f;

        /// <summary>
        /// Measured (sample build of 2026-11-07): a plain wall is 0.22 m thick, both of its sides drawn, and its main
        /// face (the largest area turned to +Z: the paper, the panels) lies 0.03 m (dark, panelled) to 0.08 m (paper)
        /// behind its dado. Every wall is set with that face 0.08 m from its line, so the dark and panelled walls are
        /// centred, every finish shows its face on one plane, and pictures hang on it.
        /// </summary>
        public const float ViFace = 0.08f;

        /// <summary>
        /// Measured: the paper finish's dado then stands 0.16 m from the line, the furthest any plain wall reaches, so
        /// furniture against a wall stands there: flush on paper, 0.05 m off the dark and panelled walls.
        /// </summary>
        public const float ViBack = 0.16f;

        /// <summary>Measured (probe of 2026-11-07): the clear opening of the doorway walls, 1.35 × 2.18 m.</summary>
        public const float ViOpenWidth = 1.35f, ViOpenHeight = 2.18f;

        /// <summary>BK's 2.5 m wall module at x1.2 is the pack's 3 m; stretched up, its 2.5 m wall stands the 3.5 m storey.</summary>
        public const float ViStoneFactor = 1.2f, ViStoneStretch = ViStorey / 2.5f / ViStoneFactor;

        /// <summary>Authoring default, m: a stone wall's brick stands here from its line, its timbers about 0.15 m further in.</summary>
        public const float ViStoneInfill = 0.03f;

        /// <summary>
        /// Measured: the next storey's floor slab is 0.1 m under its floor (Body.Ground), so the bodies of the pack's
        /// walls and of anything that reaches the ceiling stop at 3.4 m, and the slab rests on them, touching (VI-3).
        /// </summary>
        public const float ViBodyTop = ViStorey - 0.1f;

        /// <summary>Measured (VI-3): how far the stair's top newel stands past its top step, m.</summary>
        public const float ViStairNewel = 0.12f;

        /// <summary>The keys of the room family's walls, by finish: plain, doorway, window (null where the pack has none).</summary>
        public static readonly IReadOnlyDictionary<string, (string Plain, string Doorway, string Window)> ViFinishes =
            new Dictionary<string, (string, string, string)>
            {
                ["Paper"] = ("ViPaper", "ViPaperDoorway", "ViPaperWindow"),
                ["Dark"] = ("ViDark", "ViDarkDoorway", "ViDarkWindow"),
                ["Panel"] = ("ViPanel", null, null),
            };

        public static IEnumerable<Spec> ViSpecs()
        {
            const string st = KitLayers.Structure, pr = KitLayers.Prop, ia = KitLayers.Interactable, ng = KitLayers.NoGrid;

            Spec R(string key, string file, string layer, Body body, Anchor anchor)
            {
                var spec = S(key, "ViRoom", key.Substring(2), Vi, file, layer, body, anchor);
                spec.BodyTop = ViBodyTop;
                if (anchor == Anchor.Linear)
                {
                    spec.Width = ViModule;
                }

                return spec;
            }

            Spec Wall(string key, string file, Body body = Body.Box)
            {
                var spec = R(key, file, st, body, Anchor.Linear);
                spec.Infill = true;
                spec.InfillAt = ViFace;
                spec.OpenWidth = ViOpenWidth;
                spec.OpenHeight = ViOpenHeight;
                return spec;
            }

            // The probe's sheets: Wall_1, 2, 3 are patterned paper over a dark dado (plain, doorway, window); Wall_5, 8, 6
            // the plain dark finish under a patterned frieze; Wall_4 wood panelling. The windows' panes are part of the
            // wall and opaque (no glass, no hole), so daylight comes in as a lit pane and its light (plan §5).
            yield return Wall("ViPaper", "Wall_1");
            yield return Wall("ViPaperDoorway", "Wall_2", Body.Doorway);
            yield return Wall("ViPaperWindow", "Wall_3");
            yield return Wall("ViDark", "Wall_5");
            yield return Wall("ViDarkDoorway", "Wall_8", Body.Doorway);
            yield return Wall("ViDarkWindow", "Wall_6");
            yield return Wall("ViPanel", "Wall_4");
            foreach (var (key, file) in new[] { ("ViDoor", "Door_1"), ("ViDoorB", "Door_2") })
            {
                // A shut leaf is solid, centred on its line in the doorway's opening.
                var door = R(key, file, st, Body.Leaf, Anchor.Linear);
                door.OpenWidth = ViOpenWidth;
                door.OpenHeight = ViOpenHeight;
                yield return door;
            }

            yield return R("ViFloor", "Floor", KitLayers.Ground, Body.Ground, Anchor.Centre);
            var ceiling = R("ViCeiling", "Ceiling", ng, Body.None, Anchor.Centre);
            ceiling.Top = true; // placed at the storey's height, it hangs 0.23 m below
            yield return ceiling;
            // The stair (VI-3): it rises 3.54 m in 19 steps of about 0.19 × 0.35 m (28°) over 6.1 m, measured; turned so
            // it climbs toward its front. Its rails' newel stands 0.12 m past the top step (measured), so the stair is
            // set 0.12 m further on than its bounds' front: the top step meets the upper floor at the module line, and the
            // newel stands on the landing (owner's play test VI-5, remark 1). Its own rails carry the pack's colliders.
            var stair = R("ViStair", "Stair", st, Body.Keep, Anchor.Front);
            stair.ModelYaw = 180f;
            stair.Ramp = true;
            stair.Shift = ViStairNewel;
            yield return stair;
            // The balustrade round a stair well on the upper floor: solid, so nobody walks into the well (VI-3). It lies
            // along X, 3 m, with its side flush with its footprint's front, so placed in the row beside the well it stands
            // on the well's edge, not in the middle of the row half a metre off (owner's play test VI-5, remark 1).
            var banister = R("ViBanister", "Banister", st, Body.Box, Anchor.Front);
            banister.ModelYaw = 90f;
            banister.Lengthen = 3f / 2.94f; // measured 2.94 m: two in a row left a 6 cm gap (VI-5)
            yield return banister;

            // Stone (owner, 2026-11-07: the cellar's vaults, the cells and the chapel's nave): the pack has none, so BK's
            // brick between timbers and its stone floor, at x1.2 along (a 3 m module) and stretched to the 3.5 m storey.
            // Each BK wall is drawn on its local +Z side only, so a wall between two stone rooms gets a seen face turned
            // to the second. Its infill stands ViStoneInfill from its line, its timbers 0.15 m further in.
            Spec Stone(string key, string file, string layer, Body body, Anchor anchor)
            {
                var spec = S(key, "ViStone", key.Substring(7), Bk + "/Architecture", file, layer, body, anchor);
                spec.Factor = ViStoneFactor;
                spec.Stretch = anchor == Anchor.Centre ? 1f : ViStoneStretch;
                spec.Pack = "BK";
                spec.NoLights = true;
                spec.LineBody = true;
                if (anchor == Anchor.Linear)
                {
                    spec.Width = ViModule;
                    spec.Infill = body != Body.Leaf;
                    spec.InfillAt = ViStoneInfill;
                    spec.OpenWidth = 1.1f * ViStoneFactor;
                    spec.OpenHeight = 1.98f * ViStoneFactor * ViStoneStretch;
                }

                return spec;
            }

            yield return Stone("ViStoneWall", "IntWall02a", st, Body.Box, Anchor.Linear);
            yield return Stone("ViStoneWallB", "IntWall02b", st, Body.Box, Anchor.Linear);
            yield return Stone("ViStoneDoorway", "IntWall02door", st, Body.Doorway, Anchor.Linear);
            foreach (var (key, file) in new[] { ("ViStoneWallFace", "IntWall02a"), ("ViStoneWallBFace", "IntWall02b"), ("ViStoneDoorwayFace", "IntWall02door") })
            {
                yield return Stone(key, file, ng, Body.None, Anchor.Linear);
            }

            var stoneDoor = Stone("ViStoneDoor", "Door", st, Body.Leaf, Anchor.Linear);
            stoneDoor.Shift = 0.14f * ViStoneFactor / BkArchitecture; // BK's measured frame centre, at this scale
            yield return stoneDoor;
            yield return Stone("ViStoneWindow", "IntWall02window01", st, Body.Box, Anchor.Linear);
            yield return Stone("ViStonePost", "VerticalPillar", ng, Body.None, Anchor.Node);

            // The cells' fronts on the 3 m module: the owner's Tripo bars squeezed to 3 m at their own height, and the cell
            // door in their doorway (CellFront, as for the 4 m bars).
            var bars = S("ViCellBars", "ViStone", "CellBars", Tripo, "rusty_iron_gate_3d_model", st, Body.None, Anchor.Linear);
            bars.SizeX = ViModule;
            bars.SizeY = 2.76f;
            bars.Uniform = false;
            yield return bars;
            var cellDoor = S("ViCellDoor", "ViStone", "CellDoor", Tripo, "rusty_iron_jail_door_3d_model", ia, anchor: Anchor.Linear);
            cellDoor.SizeX = 0.9f;
            cellDoor.SizeY = 2.13f;
            cellDoor.Uniform = false;
            cellDoor.Width = ViModule;
            yield return cellDoor;
            yield return Stone("ViStoneFloor", "StoneFloor01", KitLayers.Ground, Body.Ground, Anchor.Centre);

            Spec F(string key, string file, string layer = pr, Body body = Body.Box, Anchor anchor = Anchor.Centre)
            {
                var spec = S(key, "ViFurniture", key.Substring(2), Vi, file, layer, body, anchor);
                spec.BodyTop = ViBodyTop;
                if (anchor == Anchor.Back || anchor == Anchor.Face)
                {
                    spec.Face = anchor == Anchor.Back ? ViBack : ViFace;
                }

                return spec;
            }

            Spec Back(string key, string file, string layer = pr) => F(key, file, layer, Body.Box, Anchor.Back);

            Spec Hung(string key, string file, float out_ = HungOut)
            {
                var spec = F(key, file, ng, Body.None, Anchor.Face);
                spec.Width = ViModule; // centred on its 3 m wall segment
                spec.Out = out_;
                return spec;
            }

            Spec Set(Spec spec, params (string File, float Fx, float Fz)[] on)
            {
                spec.On = on.Select(t => (Vi, t.File, t.Fx, t.Fz)).ToArray();
                return spec;
            }

            // Against a wall, its back on the wall's face.
            yield return Back("ViFireplace", "Mantel");
            var bookcase = Back("ViBookcase", "BookShelf_1");
            bookcase.Books = true;
            yield return bookcase;
            var bookcaseLow = Back("ViBookcaseLow", "BookShelf_2");
            bookcaseLow.Books = true;
            yield return bookcaseLow;
            yield return Back("ViShelves", "Shelve_3");
            yield return Back("ViShelvesB", "Shelve_4");
            yield return Back("ViCabinet", "Shelve_5");
            yield return Back("ViCupboardGlass", "Cupboard_2");
            yield return Back("ViSideboard", "Cupboard_4");
            yield return Back("ViShelfSmall", "Shelf_4");
            yield return Back("ViClock", "Clock");
            yield return Back("ViSettee", "Couch_1");
            yield return Back("ViSofa", "Couch_2_Brown");
            yield return Back("ViSofaGreen", "Couch_2_Green");
            yield return Back("ViBed", "Bed", ia); // measured on the sheets: its head is at its back
            yield return Back("ViBath", "Bathtub");
            yield return Back("ViToilet", "Toilet_Seat");
            yield return Back("ViBasin", "Sink");
            yield return Back("ViMirrorTall", "Mirror_1");
            yield return Back("ViTowelRail", "Hanger_2");
            yield return Back("ViCounter", "Counter");
            yield return Set(Back("ViDesk", "Table_6"),
                ("Ink", 0.62f, 0.35f), ("Pen", 0.66f, 0.35f), ("Paper_1", 0.45f, 0.3f), ("Paper_2", 0.5f, 0.35f),
                ("Book_1", 0.2f, 0.35f), ("Candle_2", 0.85f, 0.4f));

            // Sets of the furnishing rules (G5, VI-6): small things on their pieces, never on the floor.
            yield return Set(Back("ViNightstand", "Shelve_5"), ("Candle_1", 0.3f, 0.5f), ("Book_1", 0.7f, 0.5f));
            yield return Set(Back("ViNightstandPoor", "Shelve_5"), ("Candle_2", 0.5f, 0.5f));
            yield return Set(Back("ViBasinSet", "Sink"), ("Soap", 0.15f, 0.5f), ("Brush_1", 0.85f, 0.5f));
            yield return Set(Back("ViBathSet", "Bathtub"), ("Sponge_1", 0.06f, 0.5f), ("Soap", 0.94f, 0.5f));
            yield return Set(Back("ViShelfWash", "Shelf_4"), ("Bottle_1", 0.3f, 0.5f), ("Brush_2", 0.7f, 0.5f));
            yield return Set(Back("ViCounterSet", "Counter"), ("Box_2", 0.2f, 0.5f), ("Paper_3", 0.5f, 0.5f), ("Bottle_2", 0.8f, 0.5f));
            yield return Set(Back("ViSideboardSet", "Cupboard_4"), ("Vase_2", 0.15f, 0.5f), ("Candle_3", 0.5f, 0.5f), ("Vase_2", 0.85f, 0.5f));
            var shopShelves = Back("ViShopShelves", "Shelve_3");
            shopShelves.Goods = ShopGoods;
            yield return shopShelves;
            var dresser = Back("ViDresser", "Shelve_4");
            dresser.Goods = DresserGoods;
            yield return dresser;
            yield return Set(F("ViTableRoundSet", "Table_3"), ("Cup_1", 0.35f, 0.4f), ("Vase_3", 0.6f, 0.6f));
            yield return Set(F("ViTableBSet", "Table_2"), ("Candle_2", 0.3f, 0.5f), ("Cup_2", 0.7f, 0.5f));
            yield return Set(F("ViHallTable", "Table_2"), ("Candle_1", 0.25f, 0.5f), ("Vase_2", 0.7f, 0.5f));

            // In the room.
            yield return Set(F("ViDiningTable", "Table_4"),
                ("Plate_1", 0.2f, 0.25f), ("Plate_1", 0.4f, 0.25f), ("Plate_1", 0.6f, 0.25f), ("Plate_1", 0.8f, 0.25f),
                ("Plate_1", 0.2f, 0.75f), ("Plate_1", 0.4f, 0.75f), ("Plate_1", 0.6f, 0.75f), ("Plate_1", 0.8f, 0.75f),
                ("Cup_1", 0.3f, 0.3f), ("Cup_1", 0.7f, 0.7f), ("Candle_3", 0.5f, 0.5f), ("Vase_1", 0.3f, 0.5f));
            yield return Set(F("ViKitchenTable", "Table_5"),
                ("Plate_2", 0.25f, 0.5f), ("Cup_2", 0.45f, 0.4f), ("Cup_1", 0.6f, 0.55f), ("Pot", 0.8f, 0.5f)); // the pack's knife stands on its edge and falls (VI-6)
            yield return F("ViTable", "Table_1");
            yield return F("ViTableB", "Table_2");
            yield return F("ViTableRound", "Table_3");
            yield return F("ViChair", "Chair_1");
            yield return F("ViArmchair", "Chair_2");
            yield return F("ViFootstool", "Chair_Small");
            yield return F("ViCoatStand", "Hanger");
            yield return F("ViStandLamp", "Holder");
            yield return F("ViPalm", "Plant_1");
            yield return F("ViPlant", "Plant_2");
            yield return F("ViPlantB", "Plant_3");
            yield return F("ViPlantC", "Plant_4");
            yield return F("ViVase", "Vase_6");
            yield return F("ViBucket", "Bucket");
            yield return F("ViBox", "Box_1");
            yield return F("ViCandle", "Candle_2", ng, Body.None);
            yield return F("ViCandelabrum", "Candle_3", ng, Body.None);
            yield return F("ViOilLamp", "Candle_1", ng, Body.None);
            yield return F("ViMantelClock", "Clock_Small Variant", ng, Body.None); // the sheets: a clock that stands, not hangs
            foreach (var (key, file) in new[] { ("ViCarpet", "Carpet_1"), ("ViCarpetB", "Carpet_2"), ("ViRug", "Carpet_3"), ("ViRugB", "Carpet_4") })
            {
                yield return F(key, file, ng, Body.None);
            }

            // On a wall's face, centred on its 3 m segment at the height its block is placed.
            for (var i = 1; i <= 8; i++)
            {
                yield return Hung($"ViPicture{i}", $"Picture_{i}");
            }

            yield return Hung("ViMirror", "Mirror_2");
            yield return Hung("ViMirrorSquare", "Mirror_3");
            yield return Hung("ViMirrorSmall", "Mirror");
            yield return Hung("ViWallLamp", "Lamp_2");
            yield return Hung("ViTowelBar", "Hanger_4");
            // Inside a window wall, clear of its frame (measured: the window walls' frames stand 0.22 m from the line).
            yield return Hung("ViCurtain", "Curtain", 0.15f);

            // Under the ceiling.
            var chandelier = F("ViChandelier", "Lamp_1", ng, Body.None);
            chandelier.Top = true;
            yield return chandelier;
        }

        /// <summary>
        /// Builds the Victorian Interiors blocks only, leaving every other wrapper as it is on disk; returns them by key.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.SliceKitCatalogue.BuildVictorianBatch
        /// </summary>
        public static Dictionary<string, Entry> BuildVictorian()
        {
            var entries = new Dictionary<string, Entry>();
            foreach (var spec in ViSpecs())
            {
                var entry = Wrap(spec);
                if (entry != null)
                {
                    entries[spec.Key] = entry;
                }
            }

            CellFront(entries["ViCellBars"], entries["ViCellDoor"]);
            AssetDatabase.SaveAssets();
            Debug.Log($"Slice kit catalogue: {entries.Count} Victorian Interiors blocks.\n" +
                      string.Join("\n", entries.Values.Select(e => $"  {e.Key}: {e.Width} x {e.Depth} cells, {e.Height:0.##} m, {e.Path}")));
            return entries;
        }

        public static void BuildVictorianBatch() => BuildVictorian();

        // Standing books of the pack, by file: 0.23–0.25 m deep, 0.35 m high, 0.05–0.07 m thick along their local Z.
        private static readonly string[] StandingBooks = { "Book_3", "Book_4", "Book_5", "Book_6", "Book_7", "Book_8", "Book_9", "Book_10" };

        /// <summary>Authoring defaults: what a shop's shelves and a kitchen dresser hold, files of the pack (rules G5).</summary>
        private static readonly string[] ShopGoods = { "Bottle_1", "Bottle_2", "Bottle_3", "Bottle_4", "Pot_1", "Pot_2", "Box_1", "Box_2", "Vase_4", "Vase_5" };

        private static readonly string[] DresserGoods = { "Plate_3", "Pot_1", "Pot_2", "Cup_2", "Bottle_3", "Plate_2" };

        /// <summary>Authoring default, m: the space between goods standing on a shelf.</summary>
        private const float GoodsGap = 0.04f;

        /// <summary>Authoring default, m: a shelf takes books when it has this much room above it; each book must still fit.</summary>
        private const float BookRoom = 0.3f;

        /// <summary>
        /// Rows of the pack's standing books on every shelf of a bookcase with room for them, the same for the same key.
        /// The shelves and the bays between dividers are found by rays on the case's own meshes; a span behind
        /// a door takes none. The books are still, part of the case (kit standard §4).
        /// </summary>
        private static void FillShelves(GameObject model, string key, string[] goods = null)
        {
            var probes = new List<MeshCollider>();
            foreach (var f in model.GetComponentsInChildren<MeshFilter>())
            {
                if (f.sharedMesh != null && f.TryGetComponent<MeshRenderer>(out var r) && r.enabled)
                {
                    var c = f.gameObject.AddComponent<MeshCollider>();
                    c.sharedMesh = f.sharedMesh;
                    probes.Add(c);
                }
            }

            var backfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            Physics.SyncTransforms();
            try
            {
                var b = Seen(model);
                var z = b.max.z - 0.2f; // the books' middle, 0.2 m in from the case's front
                // Every surface a ray meets from start along dir, in order.
                List<float> Crossings(Vector3 start, Vector3 dir, float length)
                {
                    var found = new List<float>();
                    var travelled = 0f;
                    while (travelled < length)
                    {
                        var origin = start + dir * travelled;
                        var best = float.PositiveInfinity;
                        foreach (var c in probes)
                        {
                            if (c.Raycast(new Ray(origin, dir), out var hit, length - travelled) && hit.distance < best)
                            {
                                best = hit.distance;
                            }
                        }

                        if (float.IsPositiveInfinity(best))
                        {
                            break;
                        }

                        travelled += best + 0.002f;
                        found.Add(travelled);
                    }

                    return found;
                }

                // A span is open when a ray from in front reaches the books' middle before any board or door.
                bool OpenAtFront(float x, float y)
                {
                    var front = b.max.z + 0.3f;
                    var c = Crossings(new Vector3(x, y, front), Vector3.back, front - z);
                    return c.Count == 0;
                }

                // Shelves: free spans between two surfaces met in turn, tall enough and open at the front. Some boards
                // are single planes, so the crossings do not alternate between entering and leaving.
                var shelves = new List<(float Y, float Room)>();
                var diagnosis = new List<string>();
                // A case whose cornice stands proud of its shelves is probed further back (measured: Shelve_3 and _4).
                foreach (var depthIn in new[] { 0.2f, 0.35f, 0.5f })
                {
                    if (shelves.Count > 0)
                    {
                        break;
                    }

                    z = b.max.z - depthIn;
                    foreach (var fx in new[] { 0.2f, 0.5f, 0.8f })
                    {
                        var x = Mathf.Lerp(b.min.x, b.max.x, fx);
                        var top = b.max.y + 0.5f;
                        var c = Crossings(new Vector3(x, top, z), Vector3.down, b.size.y + 1f);
                        diagnosis.Add($"x {x:0.00}: " + string.Join(" ", c.Select(d => (top - d).ToString("0.00"))));
                        for (var i = 0; i + 1 < c.Count; i++)
                        {
                            var above = top - c[i];
                            var below = top - c[i + 1];
                            if (above - below >= BookRoom && !shelves.Any(s => Mathf.Abs(s.Y - below) < 0.03f) && OpenAtFront(x, (above + below) / 2f))
                            {
                                shelves.Add((below, above - below));
                            }
                        }
                    }
                }

                var rng = new Random(key.Aggregate(17, (h, ch) => (h * 31) + ch));
                var count = 0;
                foreach (var (y, room) in shelves.OrderBy(s => s.Y))
                {
                    // Bays: free spans along X between leaving one upright and entering the next.
                    var left = b.min.x - 0.2f;
                    var c = Crossings(new Vector3(left, y + 0.15f, z), Vector3.right, b.size.x + 0.4f);
                    for (var i = 0; i + 1 < c.Count; i++)
                    {
                        var xa = left + c[i] + 0.02f;
                        var xb = left + c[i + 1] - 0.02f;
                        if (xb - xa < 0.15f)
                        {
                            continue; // inside an upright, not a bay
                        }

                        var x = xa;
                        while (true)
                        {
                            if (rng.NextDouble() < 0.08)
                            {
                                x += 0.1f + (float)rng.NextDouble() * 0.15f; // a gap where books were taken
                            }

                            var book = (GameObject)PrefabUtility.InstantiatePrefab(
                                AssetDatabase.LoadAssetAtPath<GameObject>(Find(Vi, goods == null ? StandingBooks[rng.Next(StandingBooks.Length)] : goods[rng.Next(goods.Length)])), model.transform);
                            foreach (var col in book.GetComponentsInChildren<Collider>(true))
                            {
                                Object.DestroyImmediate(col);
                            }

                            foreach (var body in book.GetComponentsInChildren<Rigidbody>(true))
                            {
                                Object.DestroyImmediate(body);
                            }

                            if (goods == null)
                            {
                                book.transform.rotation = Quaternion.Euler(0f, 90f, 0f) * book.transform.rotation; // its thickness along the shelf
                            }

                            var s = Seen(book);
                            if (s.size.y > room - 0.01f)
                            {
                                Object.DestroyImmediate(book); // too tall for this shelf
                                x += 0.02f;
                                if (x > xb)
                                {
                                    break;
                                }

                                continue;
                            }

                            if (x + s.size.x > xb)
                            {
                                Object.DestroyImmediate(book);
                                break;
                            }

                            book.transform.position += new Vector3(x - s.min.x, y - s.min.y, z - s.center.z);
                            book.name = goods == null ? "Book" : "Goods";
                            x += s.size.x + (goods == null ? 0.003f : GoodsGap);
                            count++;
                        }
                    }
                }

                if (count == 0)
                {
                    Debug.LogWarning($"Slice kit catalogue: no shelf of '{key}' took a book; {shelves.Count} shelves, bounds {b.min} .. {b.max}, z {z:0.00}; downward crossings {string.Join("; ", diagnosis)}");
                }
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
    }
}

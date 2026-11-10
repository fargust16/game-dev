using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// C4's districts and outskirts (VEILMOUTH_CITY_PLAN.md §28.9): what the layout names besides its streets and
    /// houses. Stone Grandad, the cathedral and the works are stone masses until their models come
    /// (SLICE_MODELS_WANTED.md § C4). The parks, the cemetery and the garth are planted, the market is set out on
    /// Bawling Square, and cargo stands on the quay and Tipsy Pier only. The cellar door opens on Deaf Boiler Yard, and
    /// the three ritual circles wait hidden at their candidates. Outside the walls are Crooked Hamlet's cottages and farm
    /// yards, Gloomwood with its glades, beasts and herbs, and the iron veins at the foot of the area's rock. Every size
    /// and count is an authoring default; the randomness is a fixed System.Random, never the world seed.
    /// </summary>
    public static partial class VeilmouthCity
    {
        private const string CathedralStone = "VIC_Stone_Light", WorksBrick = "VIC_Brick_Poor";

        /// <summary>Stone Grandad's placeholder (§28.1 item 8): a stepped plinth, a pedestal and the figure, 12 m in all.</summary>
        private const float PlinthHeight = 1.2f, PedestalHeight = 3.8f, PedestalHalf = 2f, FigureHalf = 1f;

        /// <summary>A farm yard's depth before a cottage: the West Road runs 6 m before their fronts.</summary>
        private const int CottageYard = 5;

        /// <summary>Gloomwood's glades (§28.5): plan points well inside the forest, away from the rising edges.</summary>
        private static readonly Vector2[] Glades =
        {
            new Vector2(120, 120), new Vector2(300, 100), new Vector2(500, 120), new Vector2(640, 150),
            new Vector2(150, 300), new Vector2(260, 560), new Vector2(120, 800), new Vector2(520, 860),
        };

        /// <summary>The beast territories (§2 of the implementation plan): about 30 m across and at least 60 m apart.</summary>
        private static readonly (int Px, int Py)[] WolfTerritories = { (200, 150), (420, 80), (600, 90), (90, 330), (200, 700), (450, 900) };
        private static readonly (int Px, int Py)[] BoarTerritories = { (330, 300), (100, 600), (300, 880), (620, 800) };

        /// <summary>
        /// Gloomwood's cover (§28.9): one chance of a tree in each 6 m cell, taken most often in the belt along the forest's
        /// edges that face open land, so it reads as a wall of trees from the hamlet, the river and the walls; often in
        /// the stands a value noise on a 45 m lattice marks; seldom between them, which leaves clearings.
        /// </summary>
        private const int ForestCell = 6, StandLattice = 45, Veins = 12;

        private const float EdgeBelt = 25f, StandAbove = 0.58f, BeltChance = 0.85f, StandChance = 0.45f, OpenChance = 0.06f;

        /// <summary>The fellable trees, the rare coloured crowns round the glades.</summary>
        private static readonly string[] Fellable = { "Birch1", "Birch2", "Birch3", "Warped1", "Warped2", "Warped3" };

        // ------------------------------------------------------------------------------------------- the city's masses

        /// <summary>The cathedral's and the works' stone masses, each group its own merged mesh with box colliders.</summary>
        private static void Masses()
        {
            var cathedral = new Works(Group("The cathedral (placeholder)"), true);
            var works = new Works(Group("The works (placeholder)"), true);
            foreach (var b in _layout.boxes)
            {
                var quad = Rect(b.x0, b.y0, b.x1, b.y1);
                var (min, max) = GroundUnder(quad);
                Take(quad, Use.Solid);
                var bottom = min - 0.5f;
                var top = max + b.@base + b.height;
                var centre = World((b.x0 + b.x1) / 2f, (b.y0 + b.y1) / 2f, (bottom + top) / 2f);
                (b.material == "cathedral" ? cathedral : works).Box(centre, new Vector3(b.x1 - b.x0, top - bottom, b.y1 - b.y0), 0f);
            }

            cathedral.Finish(CathedralStone);
            works.Finish(WorksBrick);
        }

        /// <summary>Stone Grandad on the Penny's island, and four benches facing him across the grass.</summary>
        private static void Statue()
        {
            var p = _layout.plaza;
            var statue = new Works(Group($"{p.name} (placeholder)"), true);
            var g = Ground(p.cx, p.cy);
            void Stage(float half, float y0, float y1, string material) =>
                statue.Box(World(p.cx, p.cy, (y0 + y1) / 2f), new Vector3(half * 2f, y1 - y0, half * 2f), 0f, true, material);
            Stage(p.plinth, g - 0.5f, g + PlinthHeight, KerbStone);
            Stage(PedestalHalf, g + PlinthHeight, g + PlinthHeight + PedestalHeight, StepStone);
            Stage(FigureHalf, g + PlinthHeight + PedestalHeight, g + p.statue, WallStone);
            statue.Finish(KerbStone);
            Take(Rect(p.cx - p.plinth, p.cy - p.plinth, p.cx + p.plinth, p.cy + p.plinth), Use.Solid);
            foreach (var (dx, dy, f) in new[] { (-1f, -11f, Facing.S), (-1f, 10f, Facing.N), (-11f, -1f, Facing.E), (10f, -1f, Facing.W) })
            {
                Prop("Bench", f, p.cx + dx, p.cy + dy);
            }
        }

        /// <summary>The cellar door in works block 6's west wall, opening on Deaf Boiler Yard (§5.3).</summary>
        private static void CellarDoor()
        {
            var yard = _layout.SquareById("deaf_boiler");
            var px = _layout.boxes.First(b => b.id == "works_6").x0;
            // The door's middle on the half metre, so the leaf's hinge, 1.5 m off, keeps the 1 m grid.
            var py = Mathf.Round((yard.y0 + yard.y1) / 2f) - 0.5f;
            var floor = Up(Ground(px - 1f, py));
            _works.Box(World(px - 0.05f, py, floor + 1.2f), new Vector3(0.1f, 2.4f, 1.6f), 0f, false, "VIC_Wood_Oxblood");
            var leaf = (GameObject)PrefabUtility.InstantiatePrefab(_kit["PortalLeaf"].Prefab, _parent);
            // The leaf's origin is its hinge side, 1.5 m to its left of the door's middle; facing west, that is plan +y.
            leaf.transform.SetPositionAndRotation(World(px, py + 1.5f, floor), Quaternion.Euler(0f, 270f, 0f));
            Configure(leaf, "soot-stair-cellar.street", VeilmouthDressing.CellarScenePath, "soot-stair-cellar.inside");
            _doors.Add((new Vector2(px, py), new Vector2(-1f, 0f)));
        }

        // ------------------------------------------------------------------------------------------- parks and yards

        /// <summary>Last Turning Cemetery: graves in rows, its walls on the lane's and the garden's sides with a gate.</summary>
        private static void Cemetery()
        {
            var c = _layout.SquareById("cemetery");
            var graves = 0;
            for (var px = c.x0 + 4; px <= c.x1 - 4; px += 4)
            {
                for (var py = c.y0 + 6; py <= c.y1 - 6; py += 5)
                {
                    graves += Prop("Grave", Facing.S, px, py) != null ? 1 : 0;
                }
            }

            var gate = (c.y0 + c.y1) / 2f;
            for (var py = c.y0 + 2; py + 4 <= c.y1 - 2; py += 4)
            {
                if (Mathf.Abs(py + 2f - gate) > 3f)
                {
                    Prop("GraveWall", Facing.E, c.x0 + 0.2f, py);
                }

                Prop("GraveWall", Facing.E, c.x1 - 1.2f, py);
            }

            Debug.Log($"[VeilmouthCity] Last Turning Cemetery: {graves} graves.");
        }

        /// <summary>
        /// Bishop's Garden: two lines of garden trees along its middle walk, benches on the walk. Quiet Whistle Garden: a
        /// grove north of the faction's plot. The Brothers' garth: a lawn, one tree in its middle, a bench on each side.
        /// </summary>
        private static void Parks()
        {
            var rng = new Random(480);
            var trees = 0;
            var bishops = _layout.SquareById("bishops");
            var mid = (bishops.x0 + bishops.x1) / 2f;
            for (var py = bishops.y0 + 5; py < bishops.y1 - 8; py += 11)
            {
                trees += Prop(GardenTree(rng), Facing.N, bishops.x0 + 3, py) != null ? 1 : 0;
                trees += Prop(GardenTree(rng), Facing.N, bishops.x1 - 6, py + 5) != null ? 1 : 0;
            }

            for (var py = bishops.y0 + 14; py < bishops.y1 - 10; py += 22)
            {
                Prop("Bench", Facing.E, mid - 3f, py);
                Prop("Bench", Facing.W, mid + 2f, py + 11f);
            }

            var quiet = _layout.SquareById("quiet");
            for (var px = quiet.x0 + 4; px < quiet.x1 - 4; px += 10)
            {
                for (var py = quiet.y0 + 5; py < quiet.y0 + 45; py += 10)
                {
                    trees += Prop(GardenTree(rng), Facing.N, px + rng.Next(-2, 3), py + rng.Next(-2, 3)) != null ? 1 : 0;
                }
            }

            Prop("Bench", Facing.S, quiet.x0 + 12, quiet.y0 + 48);
            Prop("Bench", Facing.S, quiet.x0 + 26, quiet.y0 + 48);

            var garth = _layout.SquareById("garth");
            var cx = (garth.x0 + garth.x1) / 2f;
            var cy = (garth.y0 + garth.y1) / 2f;
            trees += Prop(GardenTree(rng), Facing.N, cx - 1f, cy - 1f) != null ? 1 : 0;
            foreach (var (dx, dy, f) in new[] { (-1f, -9f, Facing.S), (-1f, 8f, Facing.N), (-9f, -1f, Facing.E), (8f, -1f, Facing.W) })
            {
                Prop("Bench", f, cx + dx, cy + dy);
            }

            Lawn(Rect(garth.x0 + 1f, garth.y0 + 1f, garth.x1 - 1f, garth.y1 - 1f), rng);
            Debug.Log($"[VeilmouthCity] parks: {trees} garden trees in Bishop's Garden, Quiet Whistle Garden and the garth.");
        }

        /// <summary>
        /// The market on Bawling Square (§6.1): a row of stalls with the Masters' counters and the seer's, a row of food
        /// stalls, benches in the aisle between, and the Item Creation corner; 6 m clear before every front.
        /// </summary>
        private static void Market()
        {
            var s = _layout.SquareById("bawling");
            float X(float dx) => s.x0 + dx;
            float Y(float dy) => s.y0 + dy;
            Prop("Stall_Goods", Facing.S, X(8), Y(9), "The trader's stall");
            Prop("Stall_Empty", Facing.S, X(16), Y(9), "Master of Enchanting");
            Prop("Stall_Empty", Facing.S, X(24), Y(9), "Master of First Aid");
            Prop("Counter", Facing.S, X(30), Y(10), "The seer's counter");
            Prop("Candles", Facing.S, X(32), Y(10));
            foreach (var dx in new[] { 12, 20, 28 })
            {
                Prop("Bench", Facing.N, X(dx), Y(19));
            }

            Prop("Stall_Food", Facing.N, X(12), Y(28));
            Prop("Stall_Fish", Facing.N, X(20), Y(28));
            Prop("Stall_Meat", Facing.N, X(28), Y(28));
            Prop("Stall_Weapons", Facing.N, X(9), Y(37), "Master of Item Creation");
            Prop("Anvil", Facing.N, X(16), Y(39));
            Prop("LeatherFrame", Facing.N, X(20), Y(39));
            Prop("ArmourStand", Facing.N, X(24), Y(39));
        }

        /// <summary>Cargo only where the port works (owner, 2026-11-09): on Fishlip Quay before the warehouses, and on Tipsy Pier.</summary>
        private static void Cargo()
        {
            var quay = _layout.SquareById("quay");
            var rng = new Random(812);
            var keep = _layout.squares.Where(q => q.id == "tipsy" || q.id == "one_eye_pier").Select(q => (q.y0, q.y1))
                .Concat(_layout.streets.Where(t => t.kind == "main").Select(t => (y0: t.c0, y1: t.c1))).ToList();
            for (var py = quay.y0 + 6f; py < quay.y1 - 4f; py += 18f)
            {
                if (keep.Any(k => py > k.y0 - 8f && py < k.y1 + 8f))
                {
                    continue;
                }

                Prop(rng.Next(2) == 0 ? "Barrels" : "Crate", Facing.N, quay.x0 + 8 + rng.Next(3), py + rng.Next(3));
                Prop("Box", Facing.N, quay.x0 + 12 + rng.Next(3), py + 2 + rng.Next(3));
            }

            var tipsy = _layout.SquareById("tipsy");
            Prop("Barrels", Facing.N, tipsy.x1 - 14, tipsy.y0 + 1);
            Prop("Crate", Facing.N, tipsy.x1 - 9, tipsy.y1 - 4);
        }

        // ------------------------------------------------------------------------------------------- outside the walls

        /// <summary>
        /// Crooked Hamlet (§28.1 item 15): the cottages along the West Road, a fenced yard and a bed before each farm, fruit
        /// trees behind it, posts along the road, and grass and shrubs in the open land between the road and the forest.
        /// </summary>
        private static void Hamlet()
        {
            var rng = new Random(2525);
            var farms = 0;
            foreach (var c in _layout.cottages)
            {
                var f = FacingOf(c.facing);
                var key = KitKey(c.key);
                var e = _kit[key];
                var house = House(key, f, c.front, c.a, name: key == "House_Farm" ? "Farm" : "Cottage", force: true);
                if (house == null || key != "House_Farm")
                {
                    continue;
                }

                FarmYard(c.a, c.front, f, rng, CottageYard);
                farms++;
                var centre = Centre(Footprint(f, c.front, c.a, e.Width, e.Depth));
                var back = -Outward(f) * ((e.Depth / 2f) + 5f);
                for (var i = 0; i < 2; i++)
                {
                    Prop("Tree_Fruit", Facing.N, centre.x + back.x + rng.Next(-6, 7), centre.y + back.y + rng.Next(-2, 3));
                }
            }

            var road = _layout.roads.First(r => r.name == "West Road");
            var posts = 0;
            for (var px = road.x0 + 30f; px < road.x1 - 6f; px += PostEvery)
            {
                posts += Prop("RoadPost", Facing.N, px, road.y1 + 0.5f) != null ? 1 : 0;
            }

            for (var i = 0; i < 140; i++)
            {
                var px = 230f + rng.Next(145);
                var py = 404f + rng.Next(72);
                if (py > road.y0 - 2f && py < road.y1 + 2f)
                {
                    continue;
                }

                Prop(i % 6 == 0 ? "Shrub" : i % 9 == 0 ? "Poppy" : "Grass", Facing.N, px, py);
            }

            Debug.Log($"[VeilmouthCity] Crooked Hamlet: {_layout.cottages.Length} cottages, {farms} farm yards, {posts} road posts.");
        }

        /// <summary>
        /// Gloomwood: five fellable trees and up to four herb patches at each glade, cover trees by <see cref="ForestCell"/>,
        /// none in the water or the glades, the beast territories with their stand-ins, and twelve iron veins at the foot of the
        /// area's rising edges, where the rock begins.
        /// </summary>
        private static void Gloomwood()
        {
            var rng = new Random(20261110);
            var rects = _layout.forest.rects;
            bool Wet(float px, float py) =>
                _layout.Rivers.Any(w => px > w.x0 - 4f && px < w.x1 + 4f && py > w.y0 - 4f && py < w.y1 + 4f) || px > _layout.OpenCoast - 12f;
            bool InForest(float px, float py) => rects.Any(r => r.Contains(px, py)) && !Wet(px, py);

            var fellable = 0;
            var herbs = 0;
            foreach (var g in Glades)
            {
                for (var tries = 0; tries < 60 && fellable < 5 * (System.Array.IndexOf(Glades, g) + 1); tries++)
                {
                    var a = rng.NextDouble() * Mathf.PI * 2f;
                    var r = 9f + ((float)rng.NextDouble() * 6f);
                    var px = Mathf.Round(g.x + (float)(System.Math.Cos(a) * r));
                    var py = Mathf.Round(g.y + (float)(System.Math.Sin(a) * r));
                    fellable += Prop("Fellable_" + Fellable[rng.Next(Fellable.Length)], Facing.N, px, py) != null ? 1 : 0;
                }

                for (var placed = 0; placed < 4 && herbs < 30;)
                {
                    if (Prop("Herb", Facing.N, g.x + rng.Next(-6, 7), g.y + rng.Next(-6, 7)) != null)
                    {
                        placed++;
                        herbs++;
                    }
                }

                for (var i = 0; i < 12; i++)
                {
                    Prop(i % 4 == 0 ? "Poppy" : "Grass", Facing.N, g.x + rng.Next(-10, 11), g.y + rng.Next(-10, 11));
                }
            }

            // The edges that face open land: every 5 m along each rectangle's sides where the ground 2 m outside is in no
            // forest, inside the area and short of the sea's rocks.
            var size = VeilmouthScene.Extent;
            var edge = new List<Vector2>();
            foreach (var r in rects)
            {
                for (var t = 0f; t <= 1f; t += 5f / Mathf.Max(r.x1 - r.x0, r.y1 - r.y0))
                {
                    foreach (var (on, outside) in new[]
                             {
                                 (new Vector2(Mathf.Lerp(r.x0, r.x1, t), r.y0), new Vector2(0f, -2f)), (new Vector2(Mathf.Lerp(r.x0, r.x1, t), r.y1), new Vector2(0f, 2f)),
                                 (new Vector2(r.x0, Mathf.Lerp(r.y0, r.y1, t)), new Vector2(-2f, 0f)), (new Vector2(r.x1, Mathf.Lerp(r.y0, r.y1, t)), new Vector2(2f, 0f)),
                             })
                    {
                        var o = on + outside;
                        if (o.x > 0f && o.y > 0f && o.y < size && o.x < _layout.OpenCoast - 12f && !rects.Any(q => q.Contains(o.x, o.y)))
                        {
                            edge.Add(on);
                        }
                    }
                }
            }

            float Hash(int ix, int iy) => ((((ix * 73856093) ^ (iy * 19349663)) & 0x7fffffff) % 10007) / 10006f;
            float Stand(float px, float py)
            {
                var fx = px / StandLattice;
                var fy = py / StandLattice;
                var ix = Mathf.FloorToInt(fx);
                var iy = Mathf.FloorToInt(fy);
                var tx = Mathf.SmoothStep(0f, 1f, fx - ix);
                var ty = Mathf.SmoothStep(0f, 1f, fy - iy);
                return Mathf.Lerp(Mathf.Lerp(Hash(ix, iy), Hash(ix + 1, iy), tx), Mathf.Lerp(Hash(ix, iy + 1), Hash(ix + 1, iy + 1), tx), ty);
            }

            var trees = 0;
            var lo = new Vector2(rects.Min(r => r.x0), rects.Min(r => r.y0));
            var hi = new Vector2(rects.Max(r => r.x1), rects.Max(r => r.y1));
            for (var cx = lo.x; cx < hi.x; cx += ForestCell)
            {
                for (var cy = lo.y; cy < hi.y; cy += ForestCell)
                {
                    var px = cx + rng.Next(ForestCell - 1);
                    var py = cy + rng.Next(ForestCell - 1);
                    var p = new Vector2(px, py);
                    var roll = rng.NextDouble();
                    if (!InForest(px, py) || Glades.Any(g => (g - p).sqrMagnitude < 18f * 18f))
                    {
                        continue;
                    }

                    var chance = edge.Any(e => (e - p).sqrMagnitude < EdgeBelt * EdgeBelt) ? BeltChance : Stand(px, py) > StandAbove ? StandChance : OpenChance;
                    if (roll >= chance)
                    {
                        continue;
                    }

                    var kind = rng.NextDouble();
                    trees += Prop(kind < 0.75 ? "Tree_Pine" : kind < 0.85 ? "Tree_Fruit" : "Shrub", Facing.N, px, py) != null ? 1 : 0;
                }
            }

            var territories = Group("Beast Territories");
            for (var i = 0; i < WolfTerritories.Length; i++)
            {
                var (px, py) = WolfTerritories[i];
                Territory(territories, $"Wolf Territory {i + 1}", px, py);
                foreach (var (dx, dy, yaw) in new[] { (0f, 0f, 30f), (2.5f, 1.5f, 80f), (-1.5f, 2.5f, 200f) })
                {
                    Beast(VeilmouthDressing.WolfPath, 1.6f, px + dx, py + dy, yaw, $"Wolf {i + 1} (dragon stand-in)");
                }
            }

            for (var i = 0; i < BoarTerritories.Length; i++)
            {
                var (px, py) = BoarTerritories[i];
                Territory(territories, $"Boar Territory {i + 1}", px, py);
                Beast(VeilmouthDressing.BoarPath, 1.5f, px, py, 45f * i, $"Boar {i + 1} (dragon stand-in)");
            }

            // The veins at the foot of the north, west and south edges, which rise from 45 m in (VeilmouthGround), never
            // in the West Road's pass, 30 m apart at least.
            var veins = new List<Vector2>();
            for (var attempt = 0; attempt < 4000 && veins.Count < Veins; attempt++)
            {
                var side = rng.Next(3);
                // Along the west edge from 60 m to 60 m short of the south; along the north and south short of the sea's rocks.
                var along = 60f + rng.Next(side == 1 ? (int)size - 120 : (int)_layout.OpenCoast - 110);
                var depth = 36f + rng.Next(10);
                var at = side == 0 ? new Vector2(along, depth) : side == 1 ? new Vector2(depth, along) : new Vector2(along, size - depth);
                if ((side == 1 && at.y > 400f && at.y < 480f) || Wet(at.x, at.y) || veins.Any(v => (v - at).sqrMagnitude < 30f * 30f))
                {
                    continue;
                }

                if (Prop("IronOre", Facing.N, at.x, at.y, $"Iron ore vein {veins.Count + 1}") != null)
                {
                    veins.Add(at);
                }
            }

            Debug.Log($"[VeilmouthCity] Gloomwood: {trees} trees, {fellable} fellable at {Glades.Length} glades, {herbs} herbs, " +
                      $"{WolfTerritories.Length} wolf and {BoarTerritories.Length} boar territories, {veins.Count} iron veins.");
        }
    }
}

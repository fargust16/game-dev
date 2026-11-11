using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The districts and outskirts (VEILMOUTH_CITY_PLAN.md §28.9, C5's §29): what the layout names besides its streets and
    /// houses. Stone Grandad, the cathedral and the works are stone masses until their models come
    /// (SLICE_MODELS_WANTED.md § C4). The parks, the cemetery and the garth are planted, the market is set out round the
    /// statue as Bawling Row, and cargo stands on the quay and Tipsy Pier only. The cellar door opens on Deaf Boiler Yard, and
    /// the three ritual circles wait hidden at their candidates. Outside the walls are Crooked Hamlet's cottages and farm
    /// yards, Gloomwood with its glades, beasts and herbs, and the iron veins at the foot of the area's rock. Every size
    /// and count is an authoring default; the randomness is a fixed System.Random, never the world seed.
    /// </summary>
    public static partial class VeilmouthCity
    {
        private const string CathedralStone = "VIC_Stone_Light", WorksBrick = "VIC_Brick_Poor";

        /// <summary>Stone Grandad's placeholder (§28.1 item 8): a stepped plinth, a pedestal and the figure, 12 m in all.</summary>
        private const float PlinthHeight = 1.2f, PedestalHeight = 3.8f, PedestalHalf = 2f, FigureHalf = 1f; // the plain figure, until §37

        /// <summary>A farm yard's depth before a cottage: the West Road runs 6 m before their fronts.</summary>
        private const int CottageYard = 5;

        /// <summary>Gloomwood's glades (§28.5): plan points well inside the forest, away from the rising edges.</summary>
        private static readonly Vector2[] Glades =
        {
            new Vector2(120, 120), new Vector2(300, 100), new Vector2(500, 120), new Vector2(640, 150),
            new Vector2(150, 300), new Vector2(260, 560), new Vector2(120, 800), new Vector2(520, 860),
        };

        /// <summary>
        /// The beast territories (§2 of the implementation plan): 25 m round each den since the owner's play test of S5a,
        /// and at least 98 m apart. Ten wolf packs and seven boars since then, the last four and three added (authoring
        /// defaults; six and four before).
        /// </summary>
        /// <summary>A territory zone's side in the editor, m: 25 m round its den (BeastRules).</summary>
        private const float TerritorySize = 50f;

        internal static readonly (int Px, int Py)[] WolfTerritories =
        {
            (200, 150), (420, 80), (600, 90), (90, 330), (200, 700), (450, 900),
            (320, 200), (520, 200), (300, 640), (90, 900),
        };

        internal static readonly (int Px, int Py)[] BoarTerritories =
        {
            (330, 300), (100, 600), (300, 880), (620, 800),
            (240, 260), (480, 760), (330, 760),
        };

        /// <summary>
        /// Once, in the owner's Veilmouth, without building the city again: the territories the play test of S5a added
        /// join Beast Territories, every territory's zone shows its 25 m, and the beasts are placed again in all of them,
        /// those before at the same places (SliceBeastsSetup.Populate).
        /// </summary>
        [MenuItem("GameDev/Slice/Beasts: Add the Play Test's Territories to Veilmouth (once)")]
        public static void AddBeastTerritories()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            var territories = root != null ? root.transform.Find(SliceBeastsSetup.TerritoriesName) : null;
            if (territories == null)
            {
                Debug.LogError("[VeilmouthCity] Veilmouth has no beast territories.");
                return;
            }

            _terrain = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Terrain>()).Single();
            var added = 0;
            void Add(string name, int px, int py)
            {
                if (territories.Find(name) == null)
                {
                    Territory(territories, name, px, py);
                    added++;
                }
            }

            for (var i = 0; i < WolfTerritories.Length; i++)
            {
                Add($"Wolf Territory {i + 1}", WolfTerritories[i].Px, WolfTerritories[i].Py);
            }

            for (var i = 0; i < BoarTerritories.Length; i++)
            {
                Add($"Boar Territory {i + 1}", BoarTerritories[i].Px, BoarTerritories[i].Py);
            }

            foreach (var zone in territories.GetComponentsInChildren<LayoutZone>())
            {
                zone.Configure(new Vector3(TerritorySize, 6f, TerritorySize), new Color(0.6f, 0.35f, 0.15f, 0.9f));
                EditorUtility.SetDirty(zone);
            }

            var placed = SliceBeastsSetup.Populate(root.transform);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log($"[VeilmouthCity] {added} territories added; {placed} beasts stand in {territories.childCount} territories.");
        }

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
            var cathedralGroup = Group(CathedralGroup);
            var cathedral = new Works(cathedralGroup, true);
            var cathedralMasses = new List<Bounds>();
            var works = new Works(Group(WorksGroup), true);
            foreach (var b in _layout.boxes)
            {
                var quad = Rect(b.x0, b.y0, b.x1, b.y1);
                var (min, max) = GroundUnder(quad);
                Take(quad, Use.Solid);
                var bottom = min - 0.5f;
                var top = max + b.@base + b.height;
                var centre = World((b.x0 + b.x1) / 2f, (b.y0 + b.y1) / 2f, (bottom + top) / 2f);
                var size = new Vector3(b.x1 - b.x0, top - bottom, b.y1 - b.y0);
                (b.material == "cathedral" ? cathedral : works).Box(centre, size, 0f);
                if (b.material == "cathedral")
                {
                    cathedralMasses.Add(new Bounds(centre, size));
                }
            }

            cathedral.Finish(CathedralStone);
            GothicDress(cathedralGroup, cathedralMasses); // §35
            works.Finish(WorksBrick);
        }

        /// <summary>Stone Grandad on the Penny's island, inside the ring of Bawling Row's stalls (<see cref="Market"/>).</summary>
        private static void Statue()
        {
            var p = _layout.plaza;
            var statueGroup = Group($"{p.name} (placeholder)");
            var statue = new Works(statueGroup, true);
            var g = Ground(p.cx, p.cy);
            void Stage(float half, float y0, float y1, string material) =>
                statue.Box(World(p.cx, p.cy, (y0 + y1) / 2f), new Vector3(half * 2f, y1 - y0, half * 2f), 0f, true, material);
            Stage(p.plinth, g - 0.5f, g + PlinthHeight, KerbStone);
            Stage(PedestalHalf, g + PlinthHeight, g + PlinthHeight + PedestalHeight, StepStone);
            statue.Finish(KerbStone);
            SaintFigure(statueGroup, World(p.cx, p.cy, g + PlinthHeight + PedestalHeight), Vector3.left); // §37
            Take(Rect(p.cx - p.plinth, p.cy - p.plinth, p.cx + p.plinth, p.cy + p.plinth), Use.Solid);
        }

        /// <summary>The cellar door in the east shed's west wall, opening on Deaf Boiler Yard (§5.3), outside the ritual circle.</summary>
        private static void CellarDoor()
        {
            var yard = _layout.SquareById("deaf_boiler");
            var px = _layout.boxes.First(b => b.id == "ES2_e").x0;
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
        /// The yards by their block's wealth (§29.1 item 4, §29.3): a rich yard is a small park, its grass with
        /// Veilmouth's garden trees in its corners and two benches facing its middle; a middling yard keeps its cobbles
        /// with a bench and a tree; a poor yard is bare earth with dustbins and firewood in its corners and a drunkard, two
        /// in a yard over 500 m². A tree keeps 3 m off every house, and a middling yard's stands in a pit flush with its
        /// cobbles (<see cref="YardTree"/>). Everything keeps 1.5 m off the yard's edges, where the back doors open, and the benches
        /// stand on sides with no passage. The drunkards stand until their poses come (the wanted list); rubbish heaps are
        /// wanted too, so the kit's dustbin stands for them. Then a bench in each of the Penny's four paved corners (§29.3),
        /// against the side wall, facing the square.
        /// </summary>
        // Authoring default, m: a yard tree's trunk keeps this far from every house on both plan axes, so its crown of
        // about 5 m clears the walls (owner's play test of 2026-11-11: a tree stood on the cobbles hard by a house).
        private const float TreeFromHouse = 3f;

        /// <summary>
        /// A yard's tree, from its corner toward the yard's middle, at the first metre where its trunk keeps
        /// <see cref="TreeFromHouse"/> off every house and its block is free. On cobbles, which are neither earth nor grass,
        /// it stands in a square pit at the ground's level, as a street tree does (owner's play test of 2026-11-11).
        /// </summary>
        private static bool YardTree(string key, float tx, float ty, float cx, float cy, bool paved)
        {
            var to = new Vector2(cx - tx, cy - ty);
            var far = to.magnitude;
            var step = far > 0.01f ? to / far : Vector2.zero;
            for (var d = 0f; d <= far; d += 1f)
            {
                var at = new Vector2(tx, ty) + (step * d);
                var (px, py) = (Mathf.Round(at.x), Mathf.Round(at.y));
                var e = _kit[key];
                var trunk = new Vector2(px + (e.Width / 2f), py + (Mathf.Max(e.Depth, 1) / 2f));
                var clear = Rect(trunk.x - TreeFromHouse, trunk.y - TreeFromHouse, trunk.x + TreeFromHouse, trunk.y + TreeFromHouse);
                if (_houses.Any(h => Overlap(h, clear)))
                {
                    continue;
                }

                var tree = Prop(key, Facing.N, px, py);
                if (tree == null)
                {
                    continue;
                }

                if (paved)
                {
                    var model = tree.transform.Find("Model");
                    _pavements.Pit(Plan(model != null ? model.position : tree.transform.position));
                }

                return true;
            }

            return false;
        }

        private static void Yards()
        {
            var rng = new Random(2911);
            int trees = 0, benches = 0, bins = 0, drunk = 0;
            foreach (var b in _layout.blocks.Where(b => b.wealth == "rich" || b.wealth == "middling" || b.wealth == "poor"))
            {
                var y = b.yard;
                var (cx, cy) = ((y.x0 + y.x1) / 2f, (y.y0 + y.y1) / 2f);
                // A bench is 3 × 2 m (kit); turned east or west it is 2 × 3. The 15 m houses reach 5 m past the plan's
                // yard edge, so the benches face each other across the yard's middle, 2.5 m off it, or 1.5 m in a small
                // yard; the first pair that fits, then any single one.
                var seats = new[] { 2.5f, 1.5f }.SelectMany(g => new (Facing F, float X, float Y)[]
                {
                    (Facing.S, cx - 1.5f, cy - g - 2f), (Facing.N, cx - 1.5f, cy + g),
                    (Facing.E, cx - g - 2f, cy - 1.5f), (Facing.W, cx + g, cy - 1.5f),
                }).ToList();

                var corners = new[] { (y.x0 + 1.5f, y.y0 + 1.5f), (y.x1 - 2.5f, y.y0 + 1.5f), (y.x1 - 2.5f, y.y1 - 2.5f), (y.x0 + 1.5f, y.y1 - 2.5f) };
                switch (b.wealth)
                {
                    case "rich":
                        foreach (var (tx, ty) in new[] { (y.x0 + 3f, y.y0 + 3f), (y.x1 - 5f, y.y0 + 3f), (y.x1 - 5f, y.y1 - 5f), (y.x0 + 3f, y.y1 - 5f) })
                        {
                            trees += YardTree(GardenTree(rng), tx, ty, cx, cy, false) ? 1 : 0;
                        }

                        benches += Seat(seats, 2);
                        break;
                    case "middling":
                        var (mx, my) = (rng.Next(2) == 0 ? y.x0 + 3f : y.x1 - 5f, rng.Next(2) == 0 ? y.y0 + 3f : y.y1 - 5f);
                        trees += YardTree(GardenTree(rng), mx, my, cx, cy, true) ? 1 : 0;
                        benches += Seat(seats, 1);
                        break;
                    default:
                        for (var k = 0; k < corners.Length; k++)
                        {
                            var (px, py) = corners[k];
                            bins += Prop(k == 3 ? "Firewood" : "TrashCan", Facing.N, px, py, k == 3 ? "Firewood" : "Dustbin") != null ? 1 : 0;
                        }

                        var count = (y.x1 - y.x0) * (y.y1 - y.y0) > 500f ? 2 : 1;
                        for (var k = 0; k < count; k++)
                        {
                            Person("Purple", false, cx + rng.Next(-3, 4) + (k * 4f), cy + rng.Next(-2, 3), rng.Next(360), "Drunkard");
                            drunk++;
                        }

                        break;
                }
            }

            var corner = 0;
            foreach (var b in _layout.blocks.Where(b => b.HasNotch))
            {
                var n = b.notch;
                // The notch's corner inside the block, and the way out of it toward the square.
                var ix = n.x0 == b.x0 ? n.x1 : n.x0;
                var iy = n.y0 == b.y0 ? n.y1 : n.y0;
                var sx = ix == n.x0 ? 1f : -1f;
                var sy = iy == n.y0 ? 1f : -1f;
                var px0 = Mathf.Min(ix + sx, ix + (sx * 3f));
                var py0 = Mathf.Min(iy + (sy * 4f), iy + (sy * 7f));
                corner += Prop("Bench", sx > 0 ? Facing.E : Facing.W, px0, py0, "Bench") != null ? 1 : 0;
            }

            Debug.Log($"[VeilmouthCity] yards: {trees} garden trees, {benches} benches, {bins} dustbins and firewood, {drunk} drunkards; " +
                      $"{corner} of 4 benches in the Penny's corners.");
        }

        /// <summary>Up to <paramref name="wanted"/> benches from the seats in order: a facing pair first (seats come in N, S, E, W fours).</summary>
        private static int Seat(List<(Facing F, float X, float Y)> seats, int wanted)
        {
            var placed = 0;
            for (var i = 0; i + 1 < seats.Count && placed < wanted; i += 2)
            {
                if (wanted - placed >= 2)
                {
                    // A pair only if both halves are free, so a yard never gets one bench of a pair facing a wall.
                    var turned = seats[i].F == Facing.E || seats[i].F == Facing.W;
                    var (w, d) = turned ? (2f, 3f) : (3f, 2f);
                    if (!Free(Rect(seats[i].X, seats[i].Y, seats[i].X + w, seats[i].Y + d), false) ||
                        !Free(Rect(seats[i + 1].X, seats[i + 1].Y, seats[i + 1].X + w, seats[i + 1].Y + d), false))
                    {
                        continue;
                    }

                    placed += Prop("Bench", seats[i].F, seats[i].X, seats[i].Y, "Bench") != null ? 1 : 0;
                    placed += Prop("Bench", seats[i + 1].F, seats[i + 1].X, seats[i + 1].Y, "Bench") != null ? 1 : 0;
                }
                else
                {
                    placed += new[] { seats[i], seats[i + 1] }.Any(s => Prop("Bench", s.F, s.X, s.Y, "Bench") != null) ? 1 : 0;
                }
            }

            return placed;
        }

        /// <summary>
        /// Bawling Row (§29.1 item 5, §29.4): the market round Stone Grandad on the Penny's island, every stall facing the
        /// ring's road with the island's walk before it for the customers, its back 2 m off the plinth. The kit's stalls
        /// are 6 to 9 m long (measured), so a side holds two, not the plan's three: six stalls and five small pieces in the
        /// corners, every corner within 12.2 m of the statue, inside the ring's 48-sided kerb at 12.9 m. North, toward
        /// Lenten Street: the trader and the Master of Enchanting; east: food and the seer's counter with her candles;
        /// south: the Master of First Aid and fish; west: the Master of Item Creation between the armour stand and the
        /// anvil; the leather frame in the south-east corner. Footprints in whole metres from the statue (north-west corner,
        /// plan x east and y south), so the world grid rounds none of them.
        /// </summary>
        private static void Market()
        {
            var p = _layout.plaza;
            var pieces = new (string Key, Facing F, int X, int Y, string Name)[]
            {
                ("Stall_Goods", Facing.N, -7, -10, "The trader's stall"),
                ("Stall_Empty", Facing.N, 1, -10, "Master of Enchanting"),
                ("Stall_Food", Facing.E, 7, -2, null),
                ("Counter", Facing.E, 9, -5, "The seer's counter"),
                ("Candles", Facing.E, 8, -5, null),
                ("Stall_Empty", Facing.S, -7, 6, "Master of First Aid"),
                ("Stall_Fish", Facing.S, 0, 6, null),
                ("Stall_Weapons", Facing.W, -11, -4, "Master of Item Creation"),
                ("ArmourStand", Facing.W, -10, -6, null),
                ("Anvil", Facing.W, -10, 6, null),
                ("QuenchBucket", Facing.W, -10, 5, null), // the slack tub's bucket, north of the anvil: the smith stands east of it
                ("LeatherFrame", Facing.S, 8, 6, null),
            };
            var placed = pieces.Count(s => Prop(s.Key, s.F, p.cx + s.X, p.cy + s.Y, s.Name) != null);
            Debug.Log($"[VeilmouthCity] {p.stalls.name}: {placed} of {pieces.Length} stalls and pieces round {p.name}.");

            // The pawnbroker's counter in the Black Puddle's north-east corner, 3 m off the fronts: well outside the circle.
            var puddle = _layout.SquareById("black_puddle");
            Prop("Counter", Facing.S, puddle.x1 - 8f, puddle.y0 + 3f, "Pawn shop counter");
            Prop("Chest", Facing.S, puddle.x1 - 4f, puddle.y0 + 3f, "Pawn shop chest");
        }

        /// <summary>
        /// Cargo only where the port works (owner, 2026-11-09): on Fishlip Quay before the warehouses, and on Tipsy Pier.
        /// Each pile has sacks beside it and every other one a handcart, and one wagon stands on the quay lengthwise (CC0 kit,
        /// 2026-11-11), from their own stream so the crates and boxes keep their places.
        /// </summary>
        private static void Cargo()
        {
            var quay = _layout.SquareById("quay");
            var rng = new Random(812);
            var load = new Random(813);
            var wagon = false;
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
                Prop(load.Next(2) == 0 ? "Sacks" : "SacksSmall", Facing.N, quay.x0 + 8 + load.Next(3), py + 7);
                if (load.Next(2) == 0)
                {
                    Prop("Handcart", Facing.N, quay.x0 + 12 + load.Next(2), py + 10);
                }
                else if (!wagon)
                {
                    wagon = Prop("WoodCart", Facing.E, quay.x0 + 12, py + 9) != null;
                }
            }

            var tipsy = _layout.SquareById("tipsy");
            Prop("Barrels", Facing.N, tipsy.x1 - 14, tipsy.y0 + 1);
            Prop("Crate", Facing.N, tipsy.x1 - 9, tipsy.y1 - 4);

            // §12, §46: the pinnace moored along Tipsy Pier's south side, her bow to the sea, afloat at her draught, and
            // coils of rope on the pier by her (CC0, 2026-11-11).
            var ship = PutPlan("Ship", Facing.E, tipsy.x0 + 12f, tipsy.y1 + 1f, VeilmouthScene.SeaLevel - SliceKitCatalogue.ShipDraught);
            ship.name = "The pinnace at Tipsy Pier";
            Prop("RopeCoil", Facing.N, tipsy.x0 + 16, tipsy.y1 - 2);
            Prop("RopeCoil", Facing.N, tipsy.x0 + 34, tipsy.y1 - 2);
        }

        // ------------------------------------------------------------------------------------------- people

        /// <summary>
        /// The people (§28.10, on C5 by §29.6), placed after every building, lamp and tree so a walker's route goes round
        /// them: Wardens, blue, at the Assize's door, on Salt Gullet and by the Black Puddle; the faction, teal, before its
        /// house; Veinmarket, brown, on Draught Lane; the Masters, green, and the sellers, orange, on Bawling Row's island
        /// walk before their stalls; the pawnbroker at her counter in the Black Puddle's north-east corner, clear of the
        /// ritual circle; townsfolk, yellow, on the main streets, the Penny, the embankment, the quay and the squares; a
        /// farmer on the West Road. <see cref="SlicePeople"/> puts the named people in place of the Dummies of these names.
        /// </summary>
        private static void People()
        {
            var assize = _layout.buildings.First(b => b.id == "assize");
            var outward = Turns(FacingOf(assize.facing)) * 90f;
            foreach (var along in new[] { -2f, 2f })
            {
                var at = BeforeFront(assize, along, 3f);
                Person("Blue", false, at.x, at.y, outward, "Warden at the station");
            }

            var gullet = _layout.StreetById("main_e");
            Person("Blue", false, (gullet.a0 + gullet.a1) / 2f + 5f, gullet.c0 + (gullet.walk0 / 2f) + 0.5f, 90f, "Warden on Salt Gullet");
            var puddle = _layout.SquareById("black_puddle");
            Person("Blue", true, puddle.x0 + 5f, puddle.y0 + 3.5f, 135f, "Warden at the Black Puddle");

            // The faction's members before its house in the cathedral's close, facing the street.
            var faction = _layout.buildings.First(b => b.id == "faction_house");
            var fy = Turns(FacingOf(faction.facing)) * 90f;
            foreach (var (along, female) in new[] { (-4f, false), (4f, true) })
            {
                var at = BeforeFront(faction, along, 3f);
                Person("Teal", female, at.x, at.y, fy, "Faction member");
            }

            // Veinmarket on Draught Lane's north pavement, by Lower Veil's tenements.
            var lane = _layout.StreetById("draught");
            Person("Brown", false, lane.a1 - 24f, lane.c0 + (lane.walk0 / 2f), 0f, "Veinmarket");
            Person("Brown", false, lane.a1 - 22f, lane.c0 + (lane.walk0 / 2f), 270f, "Veinmarket");

            // The Masters and the sellers on the island's walk before their stalls on Bawling Row (Market), facing out to
            // their customers; the roster's sex for each.
            var pz = _layout.plaza;
            Person("Orange", false, pz.cx - 4f, pz.cy - 11.5f, 0f, "The trader");
            Person("Green", false, pz.cx + 4f, pz.cy - 11.5f, 0f, "Master of Enchanting");
            Person("Green", true, pz.cx - 4f, pz.cy + 11.5f, 180f, "Master of First Aid");
            Person("Orange", true, pz.cx + 11f, pz.cy - 4f, 90f, "The seer");
            Person("Green", true, pz.cx - 12f, pz.cy, 270f, "Master of Item Creation");

            Person("Orange", true, puddle.x1 - 6.5f, puddle.y0 + 2f, 180f, "The pawnbroker");

            // Townsfolk: on pavements, the Penny's corners, the embankment's river walk, the quay and the squares. A street's
            // pavements lie inside its c0..c1, along its edges (Band(c0, c0 + walk0) and Band(c1 - walk1, c1)).
            Vector2 Pave(string id, float along, bool far)
            {
                var st = _layout.StreetById(id);
                var across = far ? st.c1 - (st.walk1 / 2f) : st.c0 + (st.walk0 / 2f);
                return st.AlongX ? new Vector2(along, across) : new Vector2(across, along);
            }

            foreach (var (at, yaw, female) in new (Vector2, float, bool)[]
                     {
                         (Pave("main_w", 470f, true), 90f, true), (Pave("main_w", 440f, false), 270f, false),
                         (Pave("main_e", 600f, true), 90f, false), (Pave("main_e", 640f, false), 270f, true),
                         (new Vector2(521.5f, 421.5f), 45f, true), (new Vector2(558.5f, 421.5f), 315f, false), // the Penny's frame
                         (Pave("lenten", 385f, false), 0f, true), (Pave("smoked", 505f, true), 180f, false),
                         (new Vector2(450f, 355f), 90f, true), (Pave("widows", 520f, true), 270f, false), // the chapel's court
                         (Pave("embankment", 480f, true), 90f, true), (Pave("embankment", 600f, true), 270f, false),
                         (new Vector2(666f, 400f), 0f, false), (new Vector2(666f, 505f), 180f, true), // the quay
                         (new Vector2(540f, 335f), 180f, true), (Pave("thimble", 476f, false), 0f, false), // Drowsy Steps
                         (Pave("works_s", 620f, false), 0f, false), (Pave("works_x", 480f, false), 90f, true),
                         // A mill woman on Draught Lane's north pavement, 5 m east of POI B's stage, whom its fear keeps at home (SliceResidue's 30 m).
                         (Pave("draught", 580f, false), 90f, true),
                     })
            {
                Person("Yellow", female, at.x, at.y, yaw, "Townsperson");
            }

            var road = _layout.roads.First(r => r.name == "West Road");
            Person("Yellow", false, 300f, (road.y0 + road.y1) / 2f, 90f, "Farmer");
            foreach (var (at, female) in MoreWalkers(_layout))
            {
                Person("Yellow", female, at.x, at.y, 0f, "Townsperson");
            }
        }

        /// <summary>
        /// The owner's play test of S5a (2026-11-11), "an empty city": 26 more walkers on pavements across every district,
        /// 13 women and 13 men, clear of the first twenty and of POI B's stage on Draught Lane. Authoring defaults.
        /// </summary>
        internal static IEnumerable<(Vector2 At, bool Female)> MoreWalkers(VeilmouthLayout layout)
        {
            Vector2 Pave(string id, float along, bool far)
            {
                var st = layout.StreetById(id);
                var across = far ? st.c1 - (st.walk1 / 2f) : st.c0 + (st.walk0 / 2f);
                return st.AlongX ? new Vector2(along, across) : new Vector2(across, along);
            }

            var female = true;
            foreach (var (id, along, far) in new (string, float, bool)[]
                     {
                         // Parsons' End
                         ("crows_walk", 470f, false), ("crows_walk", 600f, true), ("cold_shoulder", 300f, true), ("last_turning", 320f, false),
                         ("nettle", 290f, false), ("nettle", 345f, true), ("widows", 450f, false),
                         // Sleepy Quarter
                         ("cold_shoulder", 420f, true), ("cold_shoulder", 500f, true), ("thimble", 410f, true), ("button", 450f, false),
                         ("thimble", 520f, false),
                         // Salt End
                         ("nettle", 420f, true), ("tar_row", 420f, false), ("main_e", 590f, false),
                         // round the market
                         ("lenten", 400f, true), ("smoked", 480f, false), ("main_w", 485f, false),
                         // Lower Veil
                         ("nettle", 475f, false), ("draught", 650f, true), ("embankment", 540f, true), ("tar_row", 480f, false),
                         // the Reek
                         ("ash_bank", 500f, false), ("ash_bank", 620f, true), ("cinder", 640f, true), ("kettle", 660f, false),
                     })
            {
                yield return (Pave(id, along, far), female);
                female = !female;
            }
        }

        /// <summary>
        /// Once, in the owner's Veilmouth, without building the city again: the walkers of <see cref="MoreWalkers"/> join
        /// the first twenty under People, each on a route planned round its place, in a model by its block's wealth. It
        /// refuses to run where they already walk.
        /// </summary>
        [MenuItem("GameDev/Slice/Townsfolk/Add the Play Test's 26 Walkers to Veilmouth (once)")]
        public static void AddMoreWalkers()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            var people = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName)?.transform.Find("People");
            if (people == null)
            {
                Debug.LogError("[VeilmouthCity] Veilmouth has no People group.");
                return;
            }

            if (people.GetComponentsInChildren<GameDev.Slice.People.TownsfolkWalker>(true).Length > 20)
            {
                Debug.LogWarning("[VeilmouthCity] The play test's walkers already walk in Veilmouth; they are not added again.");
                return;
            }

            var layout = VeilmouthLayout.Load();
            var terrain = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Terrain>()).Single();
            Physics.SyncTransforms();
            var placed = new List<GameObject>();
            foreach (var (plan, female) in MoreWalkers(layout))
            {
                var at = World(plan.x, plan.y);
                at.y = FloorAt(terrain, at);
                var route = SliceTownsfolk.PlanRoute(at);
                var prefab = SlicePeopleModels.WalkerLook(female, at, layout).Walking;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefab), people);
                go.name = female ? "Townswoman" : "Townsperson";
                go.SetActive(false); // a route planned later does not swerve round one placed before it
                go.transform.SetPositionAndRotation(route.Length > 0 ? route[0] : at, Quaternion.identity);
                var walk = go.GetComponent<GameDev.Slice.People.TownsfolkWalker>();
                walk.Configure(route, walk.Speed, SliceTownsfolk.GroundMask);
                PrefabUtility.RecordPrefabInstancePropertyModifications(walk);
                placed.Add(go);
                Debug.Log($"[VeilmouthCity] {go.name} at plan ({plan.x:0.#}, {plan.y:0.#}): {System.IO.Path.GetFileNameWithoutExtension(prefab)}, a route of {route.Length} points, {walk.Length:0} m.");
            }

            foreach (var go in placed)
            {
                go.SetActive(true);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log($"[VeilmouthCity] {placed.Count} walkers added; {people.GetComponentsInChildren<GameDev.Slice.People.TownsfolkWalker>(true).Length} walk in Veilmouth.");
        }

        /// <summary>The floor a walker stands on at <paramref name="at"/>: a raised pavement's top or the ground, never a roof.</summary>
        private static float FloorAt(Terrain terrain, Vector3 at)
        {
            var bare = terrain.SampleHeight(at) + terrain.transform.position.y;
            var best = bare;
            foreach (var hit in Physics.RaycastAll(new Vector3(at.x, bare + 3f, at.z), Vector3.down, 6f, SliceTownsfolk.GroundMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.point.y <= bare + 1f && hit.point.y > best)
                {
                    best = hit.point.y;
                }
            }

            return best;
        }

        /// <summary>A plan point <paramref name="ahead"/> m before a building's front, <paramref name="along"/> m from its middle.</summary>
        private static Vector2 BeforeFront(VeilmouthLayout.BuildingSpec b, float along, float ahead)
        {
            var f = FacingOf(b.facing);
            var e = _kit[KitKey(b.key)];
            var o = Outward(f);
            var mid = Centre(Footprint(f, b.front, b.a, e.Width, e.Depth)) + (o * (e.Depth / 2f));
            return mid + (o * ahead) + (new Vector2(-o.y, o.x) * along);
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
            }

            for (var i = 0; i < BoarTerritories.Length; i++)
            {
                var (px, py) = BoarTerritories[i];
                Territory(territories, $"Boar Territory {i + 1}", px, py);
            }

            // S3b.2: the wolves and boars of the territories, as SliceBeastsSetup places them in the owner's scene.
            SliceBeastsSetup.Populate(_root);

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

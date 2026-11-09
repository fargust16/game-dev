using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.Kit;
using GameDev.World.Portals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Builds Veilmouth once from the city plan C3 (VEILMOUTH_CITY_PLAN.md §24, by the rules of §2–§7 and §19), asked
    /// for by the owner on 2026-11-08, on the ground <see cref="VeilmouthGround"/> shaped from it. Everything goes under
    /// one root, <c>Veilmouth C3</c>, and from then on the owner moves, removes and adds by hand: it refuses to run twice.
    /// <para>
    /// It paints the streets into the ground (three appended layers after the canonical five, which keep their order),
    /// lays kerbs; raises the town wall with its gates and the kit's towers, the Ridge's retaining wall, the Soot Stair's
    /// blind walls, the embankments, the quays and the Veil Bridge as stone works; puts up the owner's Victorian Kit at
    /// ×1.25 (<see cref="VeilmouthKitHouses"/>) in rows along every street, each row of one family with its ends faced,
    /// on plinths and steps where the ground falls, with Tanners Slant at 45° under a <see cref="KitFrame"/> (§12); the
    /// palace, the Town Hall with its clock tower, and the Assize; the seven buildings that open, each with its door;
    /// the lamps by the furniture rules (§4); the Market Place, the yards, the squares, the garden and the avenue's
    /// trees; and outside the walls the fields, the forest with its glades, beasts and herbs, and the ore hill.
    /// </para>
    /// <para>
    /// Plan coordinates as in the plan: px east and py south from the area's north-west corner, so world X = px − 500 and
    /// Z = 500 − py. Every size here is an authoring default. Layout randomness is a fixed System.Random, never the world
    /// seed. <see cref="BuildReplacing"/> replaces the root and is for the agent's iterations in batch runs only.
    /// </para>
    /// </summary>
    public static partial class VeilmouthCity
    {
        public const string RootName = "Veilmouth C3";
        private static readonly string[] OldRootNames = { "Veilmouth C2", "Veilmouth C1" }; // C3 replaces their layouts (city plan §24)
        public const int AlphamapResolution = 1024;
        public const string LayerFolder = "Assets/Settings/TerrainLayers/Slice";

        // Terrain layers: the canonical five (0 grass … 4 rock), then three street surfaces appended after them. URP's
        // terrain draws eight layers at most (TerrainLayerBudgetTests), so carriageways share one surface, and squares,
        // lanes and yards another; the plan's asphalt (§2) has no texture in the agreed packs yet.
        private const int Grass = 0, Setts = 5, Flags = 6, Cobbles = 7, Asphalt = Setts, Square = Cobbles;

        private static readonly (string Name, string Texture, float Tile)[] StreetLayers =
        {
            ("Setts", "Rough01", 4f), ("Flags", "SmoothSquare", 2f), ("Cobbles", "Mediterranean01", 4f),
        };

        private const string VictorianMaterials = "Assets/Modular Victorian City/Materials/";

        // The stone works take the kit's own stones (WORLD_STYLE_GUIDE.md §3): one style for houses, walls and steps.
        private const string WallStone = "VIC_Stone_Dirty", KerbStone = "VIC_Stone_Medium", StepStone = "VIC_Stone_Light", DeckStone = "VIC_Ground_Setts";
        private const float Low = VeilmouthGround.Low;
        private const float RidgeLevel = VeilmouthGround.Ridge;

        private enum Use { Road, Walk, Solid }

        private enum Facing { N, E, S, W }

        private sealed class Lane
        {
            public string Street;
            public Vector2[] Quad;
            public int Layer;
            public bool Carriage;
            public bool PaintOnly; // painted, but neither reserved nor a street that breaks a kerb
            public Use Use;

            /// <summary>A carriageway: setts that carts drive on, which a pavement never crosses (R4).</summary>
            public bool Road => !PaintOnly && Use == Use.Road && Layer == Setts;

            /// <summary>A raised pavement: a street's pavement lane, or Crown Avenue's mall between its carriageways (R4).</summary>
            public bool Pavement => !PaintOnly && Use == Use.Walk && (Layer == Flags || (Street == "Crown Avenue" && Layer == Square));
        }

        private static Dictionary<string, SliceKitCatalogue.Entry> _kit;
        private static Terrain _terrain;
        private static Transform _root, _parent, _people;
        private static List<(Vector2[] Quad, Use Use)> _taken;
        private static List<Lane> _lanes;
        private static List<(Vector2 At, Vector2 Out)> _doors;   // plan point and outward direction
        private static List<(bool AlongX, float Front, float At)> _partyWalls;
        private static Works _works;
        private static VeilmouthPavements _pavements; // the raised pavements (R4), known once the streets are
        private static int _passages, _openSpans; // R5: passages left between houses, spans no house fits
        private static List<Vector2[]> _houses;  // every house's plan footprint, in the order built
        private static List<Vector2[]> _stoops;  // the plan bounds of the houses' own stoops, side doors' among them
        private static List<(Facing F, float Front, int A0, int A1, int Depth)> _backs; // passages' backs, placed after the houses
        private static HashSet<(bool AlongX, float Line, int A0, int A1)> _fenced; // railing lines already set
        private static Transform _gardens;
        private static float _stepsClear; // the least room left between a plot's front steps and its railings, m

        /// <summary>
        /// The Terraces' plots (§24.3, §19): each row's house front line, the side its doors face, and its plots of 16 m
        /// along the street, each holding one detached house of 10 × 10 m in its middle with a front garden of 4 m and 2 m
        /// behind it.
        /// </summary>
        private static readonly (Facing F, float Front, (int S0, int S1)[] Spans)[] TerracePlots =
        {
            (Facing.E, 512f, new[] { (684, 700), (700, 716) }), (Facing.W, 532f, new[] { (684, 700), (700, 716) }),
        };

        private const int TerraceGarden = 4, TerraceBehind = 2;

        /// <summary>The Ridge's villa plots north of the Parade (§24.3, §19), railed on its pavement's line; the faction's house has its own.</summary>
        private static readonly (int S0, int S1)[] VillaPlots = { (530, 558), (558, 586), (676, 707), (707, 738) };

        /// <summary>A villa's front 6 m behind the Parade's pavement (py 668); its back garden runs to py 628.</summary>
        private const float VillaFront = 662f;
        private const int VillaGarden = 6, VillaBehind = 19;
        private const int FactionPlot0 = 638, FactionPlot1 = 676, FactionAt = 649;

        [MenuItem("GameDev/Slice/Build Veilmouth City from C3 (once)")]
        public static void Build() => Run(false);

        /// <summary>Batch only, for the agent's iterations: replaces this builder's own root.</summary>
        public static void BuildReplacing() => Run(true);

        public static void BuildReplacingAndRender()
        {
            Run(true);
            VeilmouthRenders.RenderAll();
        }

        /// <summary>
        /// Batch only, the agent's C3 rebuild in one run (§24): the ground shaped again from C3, the city, then the
        /// passes that live under or beside its root, the dogs, the ornaments and the mystic layer.
        /// </summary>
        public static void RebuildC3()
        {
            VeilmouthGround.ShapeReplacing();
            Run(true);
            SlicePeople.PlaceAll(); // the city stands Dummies at the named people's places; the roster's people replace them
            SliceWorldSetup.AddToVeilmouth(); // the Notice Board's tips (S1.3) and the player's map, C3
            SliceDogs.PlaceReplacing();
            VeilmouthOrnaments.Place();
            VeilmouthMystic.Place();
            VeilmouthRenders.RenderC3();
            VeilmouthRenders.RenderMystic();
        }

        /// <summary>
        /// Reference plan R6: the porches' columns and bare beams out of the baked houses, the city rebuilt on them, and
        /// the doors rendered close up. Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthCity.RebuildPorches -lookLabel r6
        /// </summary>
        public static void RebuildPorches()
        {
            VeilmouthKitHouses.StripPorches();
            RebuildStreets();
            VeilmouthRenders.RenderPorches();
        }

        /// <summary>
        /// Batch only, the reference plan's step 5 (R4, R5): the city built again on its present ground, which is the
        /// owner's to change by hand and is not shaped again, with the raised pavements and the passages; then the passes
        /// beside its root, the street probes baked again, and the views. Batch:
        /// -executeMethod GameDev.Slice.EditorTools.VeilmouthCity.RebuildStreets -lookLabel r4
        /// </summary>
        public static void RebuildStreets()
        {
            Run(true);
            SlicePeople.PlaceAll();
            SliceWorldSetup.AddToVeilmouth();
            SliceDogs.PlaceReplacing();
            VeilmouthOrnaments.Place();
            VeilmouthMystic.Place();
            VeilmouthReflections.Bake();
            VeilmouthRenders.RenderC3();
            VeilmouthRenders.RenderPavements();
            VeilmouthRenders.RenderLook();
        }

        private static void Run(bool replace)
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var existing = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            if (existing != null)
            {
                if (!replace)
                {
                    Debug.LogWarning($"{VeilmouthScene.ScenePath} already holds the C3 city; the builder does not run twice.");
                    return;
                }
            }

            // Load, never BuildAll: placing blocks must not bake the wrappers again. A rebake under the owner's newer packs
            // and editor turned the lockup's cell bars into another model with no collider (2026-11-08).
            _kit = SliceKitCatalogue.Load();
            // The kit's houses are baked once (VeilmouthKitHouses.BakeAllBatch); only a type not baked yet is baked here.
            var houses = VeilmouthKitHouses.Load();
            var missing = VeilmouthKitHouses.Types().Select(t => t.Key).Where(k => !houses.ContainsKey(k)).ToList();
            if (missing.Count > 0)
            {
                foreach (var pair in VeilmouthKitHouses.BakeAll(missing.Contains))
                {
                    houses[pair.Key] = pair.Value;
                }
            }

            foreach (var pair in houses)
            {
                _kit[pair.Key] = pair.Value;
            }

            // Opened again: baking builds and drops its assemblies in the open scene.
            scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == RootName || OldRootNames.Contains(g.name) || g.name == VeilmouthDressing.RootName))
            {
                Object.DestroyImmediate(old); // C0's dressing, C1's and C2's cities: C3 replaces their layout (city plan §24)
            }

            // The layout zones again, now that the cellar's door and the chapel's forecourt have their places.
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == VeilmouthScene.MapName || g.name == "C2 Map"))
            {
                Object.DestroyImmediate(old);
            }

            VeilmouthScene.CreateZones();

            _terrain = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Terrain>()).Single();
            _taken = new List<(Vector2[], Use)>();
            _lanes = new List<Lane>();
            _doors = new List<(Vector2, Vector2)>();
            _partyWalls = new List<(bool, float, float)>();
            _houses = new List<Vector2[]>();
            _stoops = new List<Vector2[]>();
            _backs = new List<(Facing, float, int, int, int)>();
            _fenced = new HashSet<(bool, float, int, int)>();
            _stepsClear = float.MaxValue;
            _lampsAt.Clear();
            _postsAt.Clear();
            _postsAdded = _postsHidden = 0;
            _pavements = null;
            _passages = 0;
            _openSpans = 0;

            _root = new GameObject(RootName).transform;
            _people = Group("People");
            _gardens = Group("Gardens");
            VeilmouthDressing.Wind(_root);
            _works = new Works(Group("Stone Works"), true);

            Streets();
            PaintStreets();
            // The raised pavements (R4) are known from the streets, so everything placed after stands on them.
            _pavements = new VeilmouthPavements(_lanes.Where(l => l.Pavement).Select(l => l.Quad),
                _lanes.Where(l => l.Road).Select(l => (l.Street, l.Quad)), BareGround);

            _parent = Group("Walls and Gates");
            Walls();
            _parent = Group("Government Quarter");
            Government();
            _parent = Group("Openable Buildings");
            Openable();
            Slant();
            _parent = Group("Houses");
            Houses();
            foreach (var b in _backs)
            {
                PlaceBack(b.F, b.Front, b.A0, b.A1, b.Depth);
            }

            _backs.Clear();
            BackDoors();
            _parent = Group("Squares and Yards");
            Market();
            Squares();
            _parent = Group("Lamps");
            AllLamps();
            _parent = Group("Street Trees");
            StreetTrees();
            _parent = Group("Outside the Walls");
            Fields();
            Forest();
            Hill();
            Townsfolk();
            // A street door on every dwelling (15, *The slice's Dwellings*), after every building stands.
            VeilmouthDwellings.AddDoors(_root);

            _works.Finish(WallStone);
            var works = SliceKitCatalogue.Folder + "/Works";
            var built = _pavements.Build(_root, _houses, works);
            AssetDatabase.DeleteAsset($"{works}/Kerbs - {KerbStone}.asset"); // C3's kerb ribbons, replaced by the islands
            Debug.Log($"[VeilmouthCity] pavements (R4): {built.Islands} outlines, {built.Area:0} m², {built.Triangles} triangles " +
                      $"({built.Slabs} whole slabs), {built.KerbLength:0} m of kerb, {built.Pits} tree pits; " +
                      $"{_pavements.Carried} carriageway ends carried on, {_pavements.Fillets} rounded corners. " +
                      $"Passages (R5): {_passages} left between houses, {_openSpans} spans with no house, {Slots()} gaps under {PassageWidth} m in a row. " +
                      $"Back doors: {_backDoorsOpen} open, {_backDoorsBlind} walled up. Railing posts: {_postsAdded} added at free ends, {_postsHidden} hidden at houses.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Report();
        }

        private static Transform Group(string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(_root, false);
            return t;
        }

        // ------------------------------------------------------------------------------------------- geometry

        private static Vector3 World(float px, float py, float y = 0f) => new Vector3(px - 500f, y, 500f - py);

        private static Vector2 Plan(Vector3 world) => new Vector2(world.x + 500f, 500f - world.z);

        private static float BareGround(float px, float py) =>
            _terrain.SampleHeight(World(px, py)) + _terrain.transform.position.y;

        /// <summary>What stands at a plan point stands on: a raised pavement's top (R4), or the ground.</summary>
        private static float Ground(float px, float py) =>
            _pavements != null && _pavements.On(px, py) ? _pavements.Top(px, py) : BareGround(px, py);

        private static Vector2[] Rect(float px0, float py0, float px1, float py1) =>
            new[] { new Vector2(px0, py0), new Vector2(px1, py0), new Vector2(px1, py1), new Vector2(px0, py1) };

        /// <summary>An oriented rectangle along the segment a→b, <paramref name="half"/> either side, extended at both ends.</summary>
        private static Vector2[] Along(Vector2 a, Vector2 b, float half, float extend = 0f)
        {
            var d = (b - a).normalized;
            var n = new Vector2(-d.y, d.x) * half;
            a -= d * extend;
            b += d * extend;
            return new[] { a - n, b - n, b + n, a + n };
        }

        private static bool Overlap(Vector2[] a, Vector2[] b)
        {
            foreach (var poly in new[] { a, b })
            {
                for (var i = 0; i < poly.Length; i++)
                {
                    var e = poly[(i + 1) % poly.Length] - poly[i];
                    var axis = new Vector2(-e.y, e.x).normalized;
                    float minA = float.MaxValue, maxA = float.MinValue, minB = float.MaxValue, maxB = float.MinValue;
                    foreach (var p in a)
                    {
                        var d = Vector2.Dot(p, axis);
                        minA = Mathf.Min(minA, d);
                        maxA = Mathf.Max(maxA, d);
                    }

                    foreach (var p in b)
                    {
                        var d = Vector2.Dot(p, axis);
                        minB = Mathf.Min(minB, d);
                        maxB = Mathf.Max(maxB, d);
                    }

                    if (maxA <= minB + 0.01f || maxB <= minA + 0.01f)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Inside(Vector2[] quad, Vector2 p)
        {
            var sign = 0;
            for (var i = 0; i < quad.Length; i++)
            {
                var a = quad[i];
                var b = quad[(i + 1) % quad.Length];
                var cross = ((b.x - a.x) * (p.y - a.y)) - ((b.y - a.y) * (p.x - a.x));
                var s = cross > 0f ? 1 : cross < 0f ? -1 : 0;
                if (s != 0)
                {
                    if (sign != 0 && s != sign)
                    {
                        return false;
                    }

                    sign = s;
                }
            }

            return true;
        }

        /// <summary>A building may stand only where nothing is; a prop also on a pavement, a square or a yard.</summary>
        private static bool Free(Vector2[] quad, bool building) =>
            _taken.All(t => (!building && t.Use == Use.Walk) || !Overlap(t.Quad, quad));

        private static void Take(Vector2[] quad, Use use) => _taken.Add((quad, use));

        private static (float Min, float Max) GroundUnder(Vector2[] quad)
        {
            float min = float.MaxValue, max = float.MinValue;
            var lo = new Vector2(quad.Min(p => p.x), quad.Min(p => p.y));
            var hi = new Vector2(quad.Max(p => p.x), quad.Max(p => p.y));
            for (var x = lo.x; x <= hi.x + 0.01f; x += Mathf.Max(0.5f, Mathf.Min(1f, (hi.x - lo.x) / 2f)))
            {
                for (var y = lo.y; y <= hi.y + 0.01f; y += Mathf.Max(0.5f, Mathf.Min(1f, (hi.y - lo.y) / 2f)))
                {
                    if (!Inside(quad, new Vector2(x, y)) && quad.Length == 4 && !Axis(quad))
                    {
                        continue;
                    }

                    var g = Ground(x, y);
                    min = Mathf.Min(min, g);
                    max = Mathf.Max(max, g);
                }
            }

            if (min == float.MaxValue)
            {
                var c = Centre(quad);
                min = max = Ground(c.x, c.y);
            }

            return (min, max);
        }

        private static bool Axis(Vector2[] quad) => quad.All(p => Mathf.Abs(p.x - quad[0].x) < 0.01f || Mathf.Abs(p.y - quad[0].y) < 0.01f);

        private static Vector2 Centre(Vector2[] quad) => quad.Aggregate(Vector2.zero, (s, p) => s + p) / quad.Length;

        private static float Up(float y) => Mathf.Ceil((y * 10f) - 0.001f) / 10f;

        private static int Turns(Facing f) => f switch { Facing.N => 0, Facing.E => 1, Facing.S => 2, _ => 3 };

        private static Vector2 Outward(Facing f) => f switch
        {
            Facing.N => new Vector2(0f, -1f), Facing.E => new Vector2(1f, 0f), Facing.S => new Vector2(0f, 1f), _ => new Vector2(-1f, 0f),
        };

        /// <summary>The plan footprint of a block whose front stands on <paramref name="front"/> facing <paramref name="f"/>, from <paramref name="a"/> along it.</summary>
        private static Vector2[] Footprint(Facing f, float front, float a, float width, float depth) => f switch
        {
            Facing.S => Rect(a, front - depth, a + width, front),
            Facing.N => Rect(a, front, a + width, front + depth),
            Facing.E => Rect(front - depth, a, front, a + width),
            _ => Rect(front, a, front + depth, a + width),
        };

        // ------------------------------------------------------------------------------------------- placing

        /// <summary>Instantiates a kit block under <paramref name="parent"/> whose rotated footprint's minimum corner, in the parent's axes, is (x, z).</summary>
        private static GameObject Put(string key, Transform parent, float x, float z, int turns, float y)
        {
            var entry = _kit[key];
            var rotation = Quaternion.Euler(0f, 90f * turns, 0f);
            var pivot = new Vector3(x, y, z);
            if (entry.Depth > 0)
            {
                var corners = new[]
                {
                    Vector3.zero, new Vector3(entry.Width, 0f, 0f), new Vector3(0f, 0f, entry.Depth), new Vector3(entry.Width, 0f, entry.Depth),
                }.Select(c => rotation * c).ToArray();
                pivot -= new Vector3(corners.Min(c => c.x), 0f, corners.Min(c => c.z));
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(entry.Prefab, parent);
            go.transform.localPosition = new Vector3(Mathf.Round(pivot.x), Mathf.Round(pivot.y * 10f) / 10f, Mathf.Round(pivot.z));
            go.transform.localRotation = rotation;
            return go;
        }

        /// <summary>A block on the world grid by its plan footprint: the plan rectangle [px0, px0 + extent] × [py0, py0 + extent].</summary>
        private static GameObject PutPlan(string key, Facing f, float px0, float py0, float y)
        {
            var e = _kit[key];
            var turned = f == Facing.E || f == Facing.W;
            var extentY = e.Depth > 0 ? (turned ? e.Width : e.Depth) : 0f;
            return Put(key, _parent, px0 - 500f, 500f - py0 - extentY, Turns(f), y);
        }

        /// <summary>
        /// A prop if its cells are free, or, when <paramref name="force"/>, wherever no house stands; it takes them.
        /// (px0, py0) is the footprint's north-west corner. A city tree's crown takes <paramref name="crownYaw"/>, degrees,
        /// where it is given.
        /// </summary>
        private static GameObject Prop(string key, Facing f, float px0, float py0, string name = null, bool force = false, float? crownYaw = null)
        {
            var e = _kit[key];
            var turned = f == Facing.E || f == Facing.W;
            var w = e.Depth > 0 ? (turned ? e.Depth : e.Width) : (turned ? 0.2f : e.Width);
            var d = e.Depth > 0 ? (turned ? e.Width : e.Depth) : (turned ? e.Width : 0.2f);
            var quad = Rect(px0, py0, px0 + w, py0 + d);
            // Forced, it still keeps 0.2 m off a house, whose pilasters and door frames stand 0.16 m proud of its plot.
            if (force ? _houses.Any(h => Overlap(h, Rect(px0 - 0.2f, py0 - 0.2f, px0 + w + 0.2f, py0 + d + 0.2f))) : !Free(quad, false))
            {
                return null;
            }

            Take(quad, Use.Solid);
            var (min, max) = GroundUnder(quad);
            // A plant is sunk below the lowest ground under it rather than left floating on a slope (owner, 2026-11-06).
            var plant = key.StartsWith("Tree_") || key.StartsWith("Bush_") || key.StartsWith("CityTree_") || key.StartsWith("CityBush_") ||
                        key == "Shrub" || key == "Grass" || key == "Poppy";
            // A loose prop stands on the ground at its centre, not on the rounded height, or it would drop when woken
            // (owner's fourth play test, 2026-11-06); Settle then tilts it to the slope.
            var loose = SliceKitCatalogue.LooseMass.ContainsKey(key);
            var centre = new Vector2(quad.Average(p => p.x), quad.Average(p => p.y));
            var y = plant ? min - 0.2f : loose ? Ground(centre.x, centre.y) : Up(Mathf.Max(min, max - 0.15f));
            var go = PutPlan(key, f, px0, py0, y);
            if (name != null)
            {
                go.name = name;
            }

            if (!plant && !Upright.Contains(key))
            {
                Settle(go, quad, e.Depth == 0, turned);
            }

            // §27: a city tree's crown turned about its trunk by its plan point, so the two variants of a row never
            // repeat, or as the caller turns it (a road's tree, §27.10); the trunk, its body and its cell stay put.
            var crown = key.StartsWith("CityTree_") ? go.transform.Find("Model") : null;
            if (crown != null)
            {
                var hash = (((int)(px0 * 10f) * 73856093) ^ ((int)(py0 * 10f) * 19349663)) & 0x7fffffff;
                crown.rotation = Quaternion.AngleAxis(crownYaw ?? hash % 360, Vector3.up) * crown.rotation;
                PrefabUtility.RecordPrefabInstancePropertyModifications(crown);
            }

            return go;
        }

        /// <summary>What stands plumb whatever the ground: posts, a column, a board on its legs, a grave, a stand.</summary>
        private static readonly HashSet<string> Upright = new HashSet<string>
        {
            "LampPost", "Column", "NoticeBoard", "Grave", "GraveWall", "ArmourStand", "Candles", "RoadPost",
        };

        /// <summary>
        /// Tilts a prop's seen model to the ground's plane under its footprint, about the footprint's centre on the
        /// ground (owner's play test, 2026-11-06: fences stood square on slopes, props stood over a hollow). A fence or
        /// a railing only pitches along its length. Only the model turns: the block and its body stay level on the
        /// grid, as the kit standard requires. At most 25° (authoring default).
        /// </summary>
        private static void Settle(GameObject go, Vector2[] quad, bool linear, bool turned)
        {
            var model = go.transform.Find("Model");
            if (model == null)
            {
                return;
            }

            var lo = new Vector2(quad.Min(p => p.x), quad.Min(p => p.y));
            var hi = new Vector2(quad.Max(p => p.x), quad.Max(p => p.y));
            var c = (lo + hi) / 2f;
            // Gradients per plan metre; plan y runs to world -Z. A linear block runs along plan x unless turned.
            var gx = hi.x - lo.x > 0.5f && !(linear && turned) ? (Ground(hi.x, c.y) - Ground(lo.x, c.y)) / (hi.x - lo.x) : 0f;
            var gy = hi.y - lo.y > 0.5f && !(linear && !turned) ? (Ground(c.x, hi.y) - Ground(c.x, lo.y)) / (hi.y - lo.y) : 0f;
            var normal = new Vector3(-gx, 1f, gy).normalized;
            var angle = Vector3.Angle(Vector3.up, normal);
            if (angle < 0.5f)
            {
                return;
            }

            var tilt = Quaternion.AngleAxis(Mathf.Min(angle, 25f), Vector3.Cross(Vector3.up, normal).normalized);
            var ground = World(c.x, c.y, Ground(c.x, c.y));
            var level = new Vector3(ground.x, go.transform.position.y, ground.z);
            model.SetPositionAndRotation(ground + (tilt * (model.position - level)), tilt * model.rotation);
            PrefabUtility.RecordPrefabInstancePropertyModifications(model);
        }

        /// <summary>
        /// A house on a plan footprint if it is free: on the highest ground under it, rounded up to 0.1 m, plus
        /// <paramref name="raise"/>; a stone plinth where the ground falls away under it, and steps down from each door.
        /// </summary>
        private static GameObject House(string key, Facing f, float front, float a, float raise = 0f, string name = null, bool force = false,
            float ceiling = float.MaxValue)
        {
            var e = _kit[key];
            var quad = Footprint(f, front, a, e.Width, e.Depth);
            if (!force && !Free(quad, true))
            {
                return null;
            }

            Take(quad, Use.Solid);
            _houses.Add(quad);
            var (min, max) = GroundUnder(quad);
            // A door is never below the pavement before it (R4: steps start from the pavement's top).
            var outward = Outward(f);
            var run = new Vector2(Mathf.Abs(outward.y), Mathf.Abs(outward.x));
            var start = f == Facing.N || f == Facing.S ? new Vector2(a, front) : new Vector2(front, a);
            foreach (var t in new[] { 0.1f, 0.5f, 0.9f })
            {
                var q = start + (run * (e.Width * t)) + (outward * 0.5f);
                if (_pavements != null && _pavements.On(q.x, q.y))
                {
                    max = Mathf.Max(max, _pavements.Top(q.x, q.y));
                }
            }

            var floor = Mathf.Min(Up(max) + raise, ceiling);
            var lo = new Vector2(quad.Min(p => p.x), quad.Min(p => p.y));
            var hi = new Vector2(quad.Max(p => p.x), quad.Max(p => p.y));
            var house = PutPlan(key, f, lo.x, lo.y, floor);
            if (name != null)
            {
                house.name = name;
            }

            if (floor - min > 0.25f)
            {
                _works.Box(World((lo.x + hi.x) / 2f, (lo.y + hi.y) / 2f, (floor + min - 0.6f) / 2f),
                    new Vector3(hi.x - lo.x, floor - min + 0.6f, hi.y - lo.y), 0f);
            }

            var along = f == Facing.N || f == Facing.S;
            _partyWalls.Add((along, front, along ? lo.x : lo.y));
            _partyWalls.Add((along, front, along ? hi.x : hi.y));
            foreach (Transform child in house.transform)
            {
                if (child.name == VictorianHouses.DoorMarker)
                {
                    Doorway(child.position, Outward(f), floor, e.Stoop, e.StoopWidth);
                }
            }

            Stoops(house);
            return house;
        }

        /// <summary>
        /// Records a house's own stoops. A corner house's side door has its stoop outside the plot, in the passage beside
        /// it. The baked house marks its steps with box colliders under "Stoop" (VeilmouthKitHouses), read here corner by
        /// corner, since a collider's bounds wait for the physics scene.
        /// </summary>
        private static void Stoops(GameObject house)
        {
            foreach (Transform child in house.transform)
            {
                if (child.name != "Stoop")
                {
                    continue;
                }

                foreach (var box in child.GetComponentsInChildren<BoxCollider>())
                {
                    var corners = Enumerable.Range(0, 8).Select(k => Plan(box.transform.TransformPoint(box.center + Vector3.Scale(box.size * 0.5f,
                        new Vector3((k & 1) == 0 ? -1f : 1f, (k & 2) == 0 ? -1f : 1f, (k & 4) == 0 ? -1f : 1f))))).ToArray();
                    _stoops.Add(Rect(corners.Min(c => c.x), corners.Min(c => c.y), corners.Max(c => c.x), corners.Max(c => c.y)));
                }
            }
        }

        /// <summary>
        /// Records a door, and builds steps from it down to the ground outside when it stands above it. The steps
        /// continue a house door's own stoop from its foot, <paramref name="stoop"/> out, in the kit's light stone and at
        /// the kit steps' width (<paramref name="stoopWidth"/>), tread and rise, so the two read as one stair; under the
        /// stoop the same stone goes down to the ground, so it never stands over a hollow on a slope (owner's play tests
        /// of 2026-11-06: steps in two styles with a gap between).
        /// </summary>
        private static void Doorway(Vector3 world, Vector2 outward, float floor, float stoop = 0f, float stoopWidth = 0f)
        {
            var at = Plan(world);
            _doors.Add((at, outward));
            var side = new Vector2(-outward.y, outward.x);
            var width = stoopWidth > 0f ? stoopWidth : VictorianHouses.StoopWidth;
            Vector3 Size(float deep, float high) => new Vector3(Mathf.Abs(side.x) * width + Mathf.Abs(outward.x) * deep, high,
                Mathf.Abs(side.y) * width + Mathf.Abs(outward.y) * deep);

            var p = at + (outward * stoop);
            if (stoop > 0f)
            {
                var under = at + (outward * (stoop / 2f));
                var low = Mathf.Min(Ground(at.x, at.y), Ground(p.x, p.y)) - 0.3f;
                if (floor - low > 0.32f)
                {
                    _works.Box(World(under.x, under.y, (floor + low) / 2f), Size(stoop, floor - low), 0f, false, StepStone);
                }
            }

            var outside = p + (outward * 1.2f);
            var rise = floor - Ground(outside.x, outside.y);
            if (rise <= VeilmouthKitHouses.StepRise * 0.5f)
            {
                return;
            }

            var n = Mathf.CeilToInt(rise / VeilmouthKitHouses.StepRise);
            var step = rise / n;
            var tread = Mathf.Max(VeilmouthKitHouses.StepTread, 0.28f); // the kit standard's least run (§3)
            // Below a stoop the first tread is at its foot, the floor's level; below a shop's door, one riser down.
            var first = stoop > 0f ? 0 : 1;
            for (var k = first; k < n; k++)
            {
                var top = floor - (k * step);
                var c = p + (outward * ((k - first + 0.5f) * tread));
                var bottom = Mathf.Min(Ground(c.x, c.y), top) - 0.3f;
                _works.Box(World(c.x, c.y, (top + bottom) / 2f), Size(tread, top - bottom), 0f, true, StepStone);
            }

            var reach = (n - first) * tread;
            if (reach <= 0f)
            {
                return;
            }

            var a = p - (side * (width / 2f));
            var b = p + (side * (width / 2f)) + (outward * reach);
            Take(Rect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y)), Use.Solid);
        }

        /// <summary>A portal leaf in a house's doorway, centred on its first door, at the door's floor.</summary>
        private static void Entrance(GameObject house, string id, string targetScenePath, string targetId)
        {
            var door = house.transform.Cast<Transform>().First(t => t.name == VictorianHouses.DoorMarker);
            var right = house.transform.right;
            var start = door.position - (right * 1.5f);
            var leaf = (GameObject)PrefabUtility.InstantiatePrefab(_kit["PortalLeaf"].Prefab, house.transform.parent);
            leaf.transform.SetPositionAndRotation(new Vector3(Mathf.Round(start.x), Mathf.Round(door.position.y * 10f) / 10f, Mathf.Round(start.z)),
                house.transform.rotation);
            Configure(leaf, id, targetScenePath, targetId);
        }

        private static void Configure(GameObject leaf, string id, string targetScenePath, string targetId)
        {
            var portal = leaf.GetComponent<Portal>();
            portal.Configure(id, SliceTestScenes.SceneName(targetScenePath), targetId, true);
            PrefabUtility.RecordPrefabInstancePropertyModifications(portal);
            leaf.name = $"Portal - {id}";
        }

        /// <summary>
        /// A row of houses along a street: fronts on <paramref name="front"/>, from a0 to a1 along it, drawn from a
        /// palette. A terrace (no <paramref name="gap"/>) is laid span by span between what already stands: wall to
        /// wall, a corner house at each end of a span with its second facade on the side street, and what does not fill
        /// a house left as a passage of 4 m or more (reference plan R5; it replaced the yard walls of the owner's play
        /// test of 2026-11-06, which read as partitions).
        /// </summary>
        private static void Row(Facing f, float front, int a0, int a1, string[] palette, int seed, float raise = 0f, int gap = 0)
        {
            var rng = new Random(seed);
            if (gap == 0)
            {
                var depth = palette.Max(k => _kit[k].Depth);
                foreach (var (s0, s1) in FreeSpans(f, front, a0, a1, depth).ToList())
                {
                    Terrace(f, front, s0, s1, palette, rng, raise);
                }

                return;
            }

            var a = a0;
            while (a < a1)
            {
                var fits = palette.Where(k => a + _kit[k].Width <= a1).ToArray();
                if (fits.Length == 0)
                {
                    break;
                }

                var key = fits[rng.Next(fits.Length)];
                if (House(key, f, front, a, raise) != null)
                {
                    a += _kit[key].Width + gap;
                    continue;
                }

                var narrow = fits.OrderBy(k => _kit[k].Width).First();
                if (House(narrow, f, front, a, raise) != null)
                {
                    a += _kit[narrow].Width + gap;
                    continue;
                }

                a += 1;
            }
        }

        /// <summary>The runs of whole metres along a row where a house of <paramref name="depth"/> could stand.</summary>
        private static IEnumerable<(int S0, int S1)> FreeSpans(Facing f, float front, int a0, int a1, int depth)
        {
            var start = -1;
            for (var a = a0; a <= a1; a++)
            {
                var free = a < a1 && Free(Footprint(f, front, a, 1, depth), true);
                if (free && start < 0)
                {
                    start = a;
                }
                else if (!free && start >= 0)
                {
                    yield return (start, a);
                    start = -1;
                }
            }
        }

        /// <summary>
        /// One span of a terrace, filled as <see cref="Row"/> describes, with passages instead of partitions (reference
        /// plan R5). A kit house is open at its party walls and faced at a row's ends (VicCityHouses), so each run of
        /// houses gets its faced ends: a corner house at each end, a detached one when it stands alone. A faced eave
        /// overhangs its wall by 0.375 m, so a span never meets a building flush: it leaves a passage of
        /// <see cref="PassageWidth"/> to the yard behind. What no house fills joins that passage, or stands in the middle
        /// as a passage of its own; a remainder under 4 m is filled again with 4 m less of house, and a span no house then
        /// fits stays open. So every gap between houses is 0 or at least 4 m, and no yard wall stands on a front.
        /// </summary>
        private static void Terrace(Facing f, float front, int s0, int s1, string[] palette, Random rng, float raise)
        {
            var depth = palette.Max(k => _kit[k].Depth);
            bool Building(int a) => _houses.Any(h => Overlap(h, Footprint(f, front, a + 0.25f, 0.5f, depth)));
            int Width(List<string> k) => k.Sum(x => _kit[x].Width);
            // A building within a passage's width beyond a span's end leaves the rest of the passage to the span.
            int Gap(int from, int step)
            {
                for (var k = 0; k < PassageWidth; k++)
                {
                    if (Building(from + (step * k)))
                    {
                        return PassageWidth - k;
                    }
                }

                return 0;
            }

            var gap0 = Gap(s0 - 1, -1);
            var gap1 = Gap(s1, 1);
            var room = s1 - s0 - gap0 - gap1;
            var keys = room > 0 ? Fill(room, palette, rng) : new List<string>();
            var left = room - Width(keys);
            if (keys.Count > 0 && gap0 == 0 && gap1 == 0 && left > 0 && left < PassageWidth)
            {
                keys = Fill(room - PassageWidth, palette, rng);
                left = room - Width(keys);
            }

            if (keys.Count == 0)
            {
                _openSpans++;
                Debug.Log($"[VeilmouthCity] R5: {s1 - s0} m left open on {f} {front} at {s0} to {s1}; no house fits.");
                Take(Footprint(f, front, s0, s1 - s0, depth), Use.Walk);
                if (s1 - s0 >= PassageWidth)
                {
                    PassageBack(f, front, s0, s1, depth);
                }

                return;
            }

            // The remainder joins an end's passage when there is one, else stands in the middle as a passage.
            if (gap0 > 0)
            {
                gap0 += left;
                left = 0;
            }
            else if (gap1 > 0)
            {
                gap1 += left;
                left = 0;
            }

            // Local −X of a house is the row's start for fronts facing N or E, its end for S and W.
            var startSide = f == Facing.N || f == Facing.E ? VictorianHouses.Side.Left : VictorianHouses.Side.Right;
            var endSide = startSide == VictorianHouses.Side.Left ? VictorianHouses.Side.Right : VictorianHouses.Side.Left;
            string Corner(string key, VictorianHouses.Side side)
            {
                if (_kit.ContainsKey(VictorianHouses.CornerKey(key, side)))
                {
                    return VictorianHouses.CornerKey(key, side);
                }

                var alike = palette.Where(k => _kit[k].Width == _kit[key].Width && _kit.ContainsKey(VictorianHouses.CornerKey(k, side))).ToArray();
                if (alike.Length == 0)
                {
                    Debug.LogWarning($"[VeilmouthCity] {key} has no {side} end; its party wall shows on {f} {front}.");
                }

                return alike.Length > 0 ? VictorianHouses.CornerKey(alike[rng.Next(alike.Length)], side) : key;
            }

            // A middle passage splits the houses into two runs, each with its own faced ends.
            var passageAt = left > 0 ? keys.Count / 2 : keys.Count;
            foreach (var (from, to) in new[] { (0, passageAt), (passageAt, keys.Count) })
            {
                if (to - from == 1)
                {
                    keys[from] = Corner(keys[from], VictorianHouses.Side.Both);
                }
                else if (to - from > 1)
                {
                    keys[from] = Corner(keys[from], startSide);
                    keys[to - 1] = Corner(keys[to - 1], endSide);
                }
            }

            if (gap0 > 0)
            {
                Passage(f, front, s0, s0 + gap0, depth);
            }

            var a = s0 + gap0;
            for (var i = 0; i < keys.Count; i++)
            {
                if (i == passageAt && left > 0)
                {
                    Passage(f, front, a, a + left, depth);
                    a += left;
                }

                if (House(keys[i], f, front, a, raise) == null)
                {
                    Debug.LogWarning($"[VeilmouthCity] a terrace house did not fit at {a} on {f} {front}.");
                }

                a += _kit[keys[i]].Width;
            }

            if (gap1 > 0)
            {
                Passage(f, front, s1 - gap1, s1, depth);
            }
        }

        /// <summary>The least gap between two houses of a row, m (R5): a real passage to the yard behind, never a slot.</summary>
        private const int PassageWidth = 4;

        private static void Passage(Facing f, float front, int a0, int a1, int depth)
        {
            _passages++;
            PassageBack(f, front, a0, a1, depth);
        }

        /// <summary>
        /// R5's check: a gap between two buildings that share a front or back line is 0 or at least
        /// <see cref="PassageWidth"/>. Counts and logs the gaps that are neither.
        /// </summary>
        private static int Slots()
        {
            var boxes = _houses.Select(q => (Min: new Vector2(q.Min(p => p.x), q.Min(p => p.y)), Max: new Vector2(q.Max(p => p.x), q.Max(p => p.y)))).ToList();
            var slots = 0;
            for (var i = 0; i < boxes.Count; i++)
            {
                for (var j = i + 1; j < boxes.Count; j++)
                {
                    var (a, b) = (boxes[i], boxes[j]);
                    for (var axis = 0; axis < 2; axis++)
                    {
                        var other = 1 - axis;
                        var gap = Mathf.Max(a.Min[axis] - b.Max[axis], b.Min[axis] - a.Max[axis]);
                        var line = Mathf.Abs(a.Min[other] - b.Min[other]) < 0.3f || Mathf.Abs(a.Max[other] - b.Max[other]) < 0.3f;
                        if (line && gap > 0.05f && gap < PassageWidth - 0.05f)
                        {
                            slots++;
                            Debug.Log($"[VeilmouthCity] R5: a gap of {gap:0.00} m between buildings at {a.Min}–{a.Max} and {b.Min}–{b.Max}.");
                        }
                    }
                }
            }

            return slots;
        }

        /// <summary>
        /// A passage's ground is taken as walk, so no later row builds into it and leaves a slot; its back waits until
        /// every house stands, so it knows the stoops of the houses on both sides.
        /// </summary>
        private static void PassageBack(Facing f, float front, int a0, int a1, int depth)
        {
            Take(Footprint(f, front, a0, a1 - a0, depth), Use.Walk);
            _backs.Add((f, front, a0, a1, depth));
        }

        /// <summary>
        /// A passage's back on the houses' back line: a railing across it with an opening of 2 m in its middle, to the
        /// yard behind (R5). The kit has no gate; a piece that would meet a house, a stoop or anything solid is left out.
        /// </summary>
        private static void PlaceBack(Facing f, float front, int a0, int a1, int depth)
        {
            var o = Outward(f);
            var back = front - ((o.x + o.y) * depth);
            var alongX = f == Facing.N || f == Facing.S;
            var g0 = ((a0 + a1) / 2) - 1;
            // Never across a stoop: a corner house's side door opens into the passage.
            bool Stoop(int a, int length) => _stoops.Any(s => Overlap(s, alongX
                ? Rect(a - 0.2f, back - 0.3f, a + length + 0.2f, back + 1.1f)
                : Rect(back - 0.3f, a - 0.2f, back + 1.1f, a + length + 0.2f)));
            RailingRun(alongX, back, a0, g0, false, Stoop);
            RailingRun(alongX, back, g0 + 2, a1, false, Stoop);
        }

        /// <summary>
        /// Houses for a span: random from the palette while much is left, then the combination of up to four that
        /// leaves the least, shuffled.
        /// </summary>
        private static List<string> Fill(int length, string[] palette, Random rng)
        {
            var keys = new List<string>();
            var left = length;
            var widest = palette.Max(k => _kit[k].Width);
            while (left >= 3 * widest)
            {
                var k = palette[rng.Next(palette.Length)];
                keys.Add(k);
                left -= _kit[k].Width;
            }

            var widths = palette.Select(k => _kit[k].Width).Distinct().OrderBy(w => w).ToArray();
            var best = new List<int>();
            void Search(List<int> chosen, int sum, int from)
            {
                if (sum > left)
                {
                    return;
                }

                if (sum > best.Sum())
                {
                    best = chosen.ToList();
                }

                if (chosen.Count == 4)
                {
                    return;
                }

                for (var w = from; w < widths.Length; w++)
                {
                    chosen.Add(widths[w]);
                    Search(chosen, sum + widths[w], w);
                    chosen.RemoveAt(chosen.Count - 1);
                }
            }

            Search(new List<int>(), 0, 0);
            foreach (var w in best)
            {
                var alike = palette.Where(k => _kit[k].Width == w).ToArray();
                keys.Add(alike[rng.Next(alike.Length)]);
            }

            for (var i = keys.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (keys[i], keys[j]) = (keys[j], keys[i]);
            }

            return keys;
        }

        private static readonly string[] TerraceKeys = Keys("Terrace8", "white", "rich")
            .Select(k => VictorianHouses.CornerKey(k, VictorianHouses.Side.Both)).ToArray();

        private static readonly string[] VillaKeys = Keys("Villa", "stucco", "white");

        // The row families (§24.4): houses of one family share height, depth and roof, so their open ends meet.
        private static readonly string[] ShopRow = VeilmouthKitHouses.Keys("Shop8", "Shop12");
        private static readonly string[] TownRow = VeilmouthKitHouses.Keys("Terrace8", "ShopTown8");
        private static readonly string[] TenementRow = VeilmouthKitHouses.Keys("Tenement8", "Tenement12");
        private static readonly string[] LowTenementRow = VeilmouthKitHouses.Keys("TenementLow8");
        // A warehouse and a tenement are one family: three storeys under a gable of the kit's 6 m (VicCityHouses).
        private static readonly string[] WarehouseRow = VeilmouthKitHouses.Keys("Warehouse12", "Tenement8");

        /// <summary>A plot's plan rectangle: from <paramref name="garden"/> m before the front to <paramref name="behind"/> m behind the house.</summary>
        private static Vector2[] PlotQuad(Facing f, float front, int s0, int s1, int depth, int garden, int behind)
        {
            var o = Outward(f);
            var s = o.x + o.y;
            var c0 = front + (s * garden);
            var c1 = front - (s * (depth + behind));
            var (lo, hi) = (Mathf.Min(c0, c1), Mathf.Max(c0, c1));
            return f == Facing.N || f == Facing.S ? Rect(s0, lo, s1, hi) : Rect(lo, s0, hi, s1);
        }

        /// <summary>
        /// A detached house on its own plot (§19): the house in the plot's middle, or at <paramref name="at"/>; railings
        /// all round with one gate of 3 m opposite its door; and its garden. Returns the house, or null.
        /// </summary>
        private static GameObject Plot(Facing f, float front, int s0, int s1, string key, int garden, int behind, Random rng,
            string name = null, float? at = null, bool force = false)
        {
            var e = _kit[key];
            var a = at ?? s0 + Mathf.Floor((s1 - s0 - e.Width) / 2f);
            // On a slope rising away from the street the floor is held down so the door's steps end 1.5 m inside the
            // railings (§19); the house's back then stands into the slope. A house's local −X is the row's start for
            // fronts facing N or E, its end for S and W.
            var o = Outward(f);
            var alongX = f == Facing.N || f == Facing.S;
            var doorAlong = f == Facing.N || f == Facing.E ? a + e.DoorX : a + e.Width - e.DoorX;
            var foot = (alongX ? new Vector2(doorAlong, front) : new Vector2(front, doorAlong)) + (o * (e.Stoop + 1.2f));
            var tread = Mathf.Max(VeilmouthKitHouses.StepTread, 0.28f);
            var treads = Mathf.Max(0, Mathf.FloorToInt((garden - 1.5f - e.Stoop) / tread));
            var ceiling = Ground(foot.x, foot.y) + (treads * VeilmouthKitHouses.StepRise) - 0.05f; // a house's height is set to 0.1 m
            var house = House(key, f, front, a, 0f, name, force, ceiling);
            if (house == null)
            {
                Debug.LogWarning($"[VeilmouthCity] no house fits the plot {s0}–{s1} on {f} {front}.");
                return null;
            }

            var s = o.x + o.y;
            var depth = garden + e.Depth + behind;
            var line = front + (s * garden);
            var back = line - (s * depth);
            var door = house.transform.Cast<Transform>().FirstOrDefault(t => t.name == VictorianHouses.DoorMarker);
            var doorAt = door != null ? Plan(door.position) : Vector2.zero;
            var gate = door != null ? (alongX ? doorAt.x : doorAt.y) : a + (e.Width / 2f);

            var parent = _parent;
            _parent = _gardens;
            var (lo, hi) = ((int)Mathf.Min(line, back), (int)Mathf.Max(line, back));
            Fence(alongX, line, s0, s1, gate);
            Fence(alongX, back, s0, s1, null);
            Fence(!alongX, s0, lo, hi, null);
            Fence(!alongX, s1, lo, hi, null);
            Garden(f, line, s0, s1, a, e.Width, e.Depth, garden, behind, gate, rng);
            _parent = parent;

            // The room left between the foot of the door's steps and the railings (§19 asks for 1.5 m at least).
            if (door != null)
            {
                var floor = house.transform.position.y;
                var outside = doorAt + (o * (e.Stoop + 1.2f));
                var rise = floor - Ground(outside.x, outside.y);
                var steps = rise <= VeilmouthKitHouses.StepRise * 0.5f ? 0 : Mathf.CeilToInt(rise / VeilmouthKitHouses.StepRise);
                var reach = e.Stoop + (steps * Mathf.Max(VeilmouthKitHouses.StepTread, 0.28f));
                _stepsClear = Mathf.Min(_stepsClear, garden - reach);
                if (garden - reach < 1.5f)
                {
                    Debug.LogWarning($"[VeilmouthCity] plot {s0}–{s1} on {f} {front}: steps reach {reach:0.00} m of a {garden} m garden (rise {rise:0.00} m).");
                }
            }

            return house;
        }

        /// <summary>
        /// A plot's railing line from a0 to a1, in segments of 2 m and one of 1 m for an odd metre, with no hole but the
        /// gate, 3 m wide, centred on <paramref name="gate"/> when one is given (§19). A line two plots share is set once.
        /// </summary>
        private static void Fence(bool alongX, float line, int a0, int a1, float? gate)
        {
            if (!_fenced.Add((alongX, line, a0, a1)))
            {
                return;
            }

            if (gate is float g)
            {
                var g0 = Mathf.Clamp(Mathf.RoundToInt(g - 1.5f), a0, a1);
                RailingRun(alongX, line, a0, g0, true);
                RailingRun(alongX, line, Mathf.Min(a1, g0 + 3), a1, true);
            }
            else
            {
                RailingRun(alongX, line, a0, a1, true);
            }
        }

        /// <summary>
        /// A plot's garden (§19): a small tree in each back corner with room for it, shrubs and flowers along the side
        /// railings, flowers along the front railings off the gate's path, and a bench in the wider side garden facing
        /// the house. u runs along the street, v in from the front railings.
        /// </summary>
        private static void Garden(Facing f, float line, int s0, int s1, float a, int width, int houseDepth, int garden, int behind, float gate, Random rng)
        {
            var o = Outward(f);
            var s = o.x + o.y;
            var alongX = f == Facing.N || f == Facing.S;
            var depth = garden + houseDepth + behind;
            Vector2 Corner(float u0, float u1, float v0, float v1)
            {
                var c = Mathf.Min(line - (v0 * s), line - (v1 * s));
                var u = Mathf.Min(u0, u1);
                return alongX ? new Vector2(u, c) : new Vector2(c, u);
            }

            void Plant(string key, float u, float v)
            {
                var p = Corner(u, u + 1f, v, v + 1f);
                Prop(key, Facing.N, p.x, p.y);
            }

            var left = a - s0;
            var right = s1 - (a + width);
            if (left >= 3f)
            {
                Plant(GardenTree(rng), s0 + 1f, depth - 2f);
            }

            if (right >= 3f)
            {
                Plant(GardenTree(rng), s1 - 2f, depth - 2f);
            }

            for (var v = 2f; v < depth - 3f; v += 2f)
            {
                Plant(rng.Next(4) == 0 ? "Poppy" : GardenBush(rng), s0 + 0.2f, v);
                Plant(rng.Next(4) == 0 ? "Poppy" : GardenBush(rng), s1 - 1.2f, v);
            }

            // A clipped hedge 1 m inside the front railings, broken at the gate's path (§25.4).
            HedgeLine(alongX, line - s, s0 + 1, s1 - 1, rng, u => Mathf.Abs(u + 0.5f - gate) < 2.5f);

            if (Mathf.Max(left, right) >= 3f)
            {
                var toward = left >= right;
                var u = toward ? s0 + (left / 2f) - 0.5f : s1 - (right / 2f) - 0.5f;
                var v = garden + (houseDepth / 2f) - 1.5f;
                var p = Corner(u, u + 1f, v, v + 3f);
                var facing = alongX ? (toward ? Facing.E : Facing.W) : (toward ? Facing.S : Facing.N);
                Prop("Bench", facing, p.x, p.y);
            }

            // Grass on the garden's earth, last, round what stands in it (§25.4).
            Lawn(PlotQuad(f, line - (s * garden), s0, s1, houseDepth, garden, behind), rng);
        }

        /// <summary>
        /// Railings on a line along a front area, in segments of 2 m and a 1 m one for an odd metre at the end. A gate
        /// is left only before a door that opens toward the line, and a segment is set whatever prop was put there
        /// before, though never through a house (owner's play test, 2026-11-06: gaps stood where no way in or out was
        /// meant).
        /// </summary>
        private static void Railings(bool alongX, float line, int a0, int a1) =>
            RailingRun(alongX, line, a0, a1, true, (a, length) => _doors.Any(d =>
            {
                var across = (alongX ? d.At.y : d.At.x) - line;
                var facing = alongX ? d.Out.y : d.Out.x;
                return Mathf.Abs(across) < 5f && across * facing < 0f && Mathf.Abs(facing) > 0.5f &&
                       Mathf.Abs((alongX ? d.At.x : d.At.y) - (a + (length / 2f))) < 1.6f;
            }));

        /// <summary>
        /// A railing on a grid line from <paramref name="from"/> to <paramref name="to"/>, in segments of 2 m and one of
        /// 1 m for an odd metre at the end, leaving any segment <paramref name="skip"/> names or that cannot stand; each
        /// unbroken run then gets its posts (<see cref="RailingEnds"/>).
        /// </summary>
        private static void RailingRun(bool alongX, float line, int from, int to, bool force, Func<int, int, bool> skip = null)
        {
            var run = new List<GameObject>();
            var start = from;
            for (var a = from; a < to;)
            {
                var length = to - a >= 2 ? 2 : 1;
                var key = length == 2 ? "Railing" : "RailingShort";
                var go = skip != null && skip(a, length) ? null
                    : alongX ? Prop(key, Facing.N, a, line - 0.1f, force: force) : Prop(key, Facing.E, line - 0.1f, a, force: force);
                if (go == null)
                {
                    RailingEnds(run, alongX, line, start, a);
                    run = new List<GameObject>();
                    start = a + length;
                }
                else
                {
                    run.Add(go);
                }

                a += length;
            }

            RailingEnds(run, alongX, line, start, to);
        }

        private static readonly List<Vector2> _postsAt = new List<Vector2>();
        private static int _postsAdded, _postsHidden;

        /// <summary>
        /// The owner's fifth play test (2026-11-09): a railing's posts stand where a run ends free, at a gate, a gap or a
        /// corner, and never at an end against a house, which its bare end abuts. The kit's module carries one post, at
        /// one end: a free end with none gets a copy of its neighbour's post, unless a post of another run already stands
        /// there, and a post at a house is hidden.
        /// </summary>
        private static void RailingEnds(List<GameObject> run, bool alongX, float line, int from, int to)
        {
            var posts = run.SelectMany(g => g.GetComponentsInChildren<Renderer>(true)).Where(r => r.name.StartsWith("Fence column")).ToList();
            if (posts.Count == 0)
            {
                return;
            }

            foreach (var (end, beyond) in new[] { (from, -1), (to, 1) })
            {
                var p = alongX ? new Vector2(end, line) : new Vector2(line, end);
                var near = posts.Where(r => r.gameObject.activeSelf && (Plan(r.bounds.center) - p).magnitude < 0.45f).ToList();
                float b0 = beyond < 0 ? end - 0.5f : end, b1 = beyond < 0 ? end : end + 0.5f;
                var probe = alongX ? Rect(b0, line - 0.4f, b1, line + 0.4f) : Rect(line - 0.4f, b0, line + 0.4f, b1);
                if (_houses.Any(h => Overlap(h, probe)))
                {
                    foreach (var r in near)
                    {
                        r.gameObject.SetActive(false);
                        _postsHidden++;
                    }
                }
                else if (near.Count == 0 && !_postsAt.Any(q => (q - p).sqrMagnitude < 0.35f * 0.35f))
                {
                    var nearest = posts.Where(r => r.gameObject.activeSelf).OrderBy(r => (Plan(r.bounds.center) - p).sqrMagnitude).FirstOrDefault();
                    if (nearest == null)
                    {
                        continue;
                    }

                    var at = Plan(nearest.bounds.center);
                    var copy = UnityEngine.Object.Instantiate(nearest.gameObject, nearest.transform.parent);
                    copy.name = nearest.name + " (end)";
                    copy.transform.position = nearest.transform.position + World(p.x, p.y, Ground(p.x, p.y) - Ground(at.x, at.y)) - World(at.x, at.y, 0f);
                    posts.Add(copy.GetComponent<Renderer>());
                    _postsAdded++;
                }
            }

            _postsAt.AddRange(posts.Where(r => r.gameObject.activeSelf).Select(r => Plan(r.bounds.center)));
        }

        private static string[] Keys(string type, params string[] facades) => facades.Select(f => VictorianHouses.Key(type, f)).ToArray();

        // ------------------------------------------------------------------------------------------- streets

        private static void Lanes(string street, int layer, bool carriage, Vector2[] quad, Use use)
        {
            _lanes.Add(new Lane { Street = street, Quad = quad, Layer = layer, Carriage = carriage, Use = use });
            Take(quad, use);
        }

        /// <summary>A street running along X: pavements of <paramref name="walk"/> m on both sides and a carriageway between.</summary>
        private static void StreetX(string name, int px0, int px1, int py0, int py1, float walk, int surface)
        {
            if (walk > 0)
            {
                Lanes(name, Flags, false, Rect(px0, py0, px1, py0 + walk), Use.Walk);
                Lanes(name, Flags, false, Rect(px0, py1 - walk, px1, py1), Use.Walk);
            }

            Lanes(name, surface, walk > 0, Rect(px0, py0 + walk, px1, py1 - walk), Use.Road);
        }

        private static void StreetY(string name, int px0, int px1, int py0, int py1, float walk, int surface)
        {
            if (walk > 0)
            {
                Lanes(name, Flags, false, Rect(px0, py0, px0 + walk, py1), Use.Walk);
                Lanes(name, Flags, false, Rect(px1 - walk, py0, px1, py1), Use.Walk);
            }

            Lanes(name, surface, walk > 0, Rect(px0 + walk, py0, px1 - walk, py1), Use.Road);
        }

        private static readonly Vector2 SlantStart = new Vector2(486f, 744f);
        private static readonly Vector2 SlantEnd = new Vector2(516f, 774f);

        private static void Streets()
        {
            // The low town's yards and courts are paved with cobbles; the streets are painted over them.
            _lanes.Add(new Lane { Street = "Yards", Quad = Rect(480, 680, 780, 830), Layer = Cobbles, PaintOnly = true });
            _lanes.Add(new Lane { Street = "Soot Stair", Quad = Rect(700, 677, 708, 714), Layer = Cobbles, PaintOnly = true });
            // The plots' gardens (§19) are green all over.
            var terraceDepth = _kit[TerraceKeys[0]].Depth;
            foreach (var (f, front, spans) in TerracePlots)
            {
                foreach (var (s0, s1) in spans)
                {
                    _lanes.Add(new Lane { Street = "Gardens", Quad = PlotQuad(f, front, s0, s1, terraceDepth, TerraceGarden, TerraceBehind), Layer = Grass, PaintOnly = true });
                }
            }

            var villaDepth = _kit[VillaKeys[0]].Depth;
            foreach (var (s0, s1) in VillaPlots.Append((FactionPlot0, FactionPlot1)))
            {
                _lanes.Add(new Lane { Street = "Gardens", Quad = PlotQuad(Facing.S, VillaFront, s0, s1, villaDepth, VillaGarden, VillaBehind), Layer = Grass, PaintOnly = true });
            }

            // §24.2 sections on C3's plan (§24.3), the pavements widened from the carriageways by the owner (§25.2). Order
            // matters for paint: a later lane covers an earlier one where they cross.
            // The Parade: a pavement, the carriageway and the promenade along the retaining wall, whose parapet is its balustrade.
            Lanes("The Parade", Flags, false, Rect(480, 668, 752, 668 + ParadeWalk), Use.Walk);
            Lanes("The Parade", Setts, true, Rect(480, 668 + ParadeWalk, 752, 677), Use.Road);
            Lanes("The Parade", Flags, false, Rect(480, 677, 752, 680), Use.Walk);
            StreetY("North Road", 742, 752, 626, 668, 0, Setts);
            StreetX("High Street", 480, 778, 726, 744, HighStreetWalk, Asphalt);
            StreetY("Terrace Street", 516, 528, 680, 726, ByelawWalk, Setts);
            StreetX("Mercers Row", 480, 550, 772, 784, ByelawWalk, Setts);
            StreetX("Bell Street", 618, 701, 772, 784, ByelawWalk, Setts);
            StreetY("Cheap Street", 516, 528, 744, 812, ByelawWalk, Setts);
            StreetY("Market Street", 606, 618, 744, 812, ByelawWalk, Setts);
            StreetY("Dock Road", 701, 713, 744, 812, ByelawWalk, Setts);
            StreetX("Loom Lane", 618, 701, 796, 802, 0, Cobbles);
            StreetX("Ridge Lane", 642, 700, 696, 702, 0, Cobbles);
            StreetX("Stair Lane", 721, 778, 696, 702, 0, Cobbles);
            StreetY("Ridge Alley", 669, 673, 702, 726, 0, Cobbles); // a passage of 4 m (reference plan R5)
            StreetX("Garden Walk", 528, 572, 680, 684, 0, Cobbles);
            // Passages of 4 m through the rows, open to the sky (R4's sections, from C3's 3 m): to Wardley Garden, and to
            // the Tenement Court from the High Street and from Bell Street (§24.2–§24.3).
            StreetY("Wardley Passage", 557, 561, 712, 726, 0, Cobbles);
            StreetY("Court Passage", 650, 654, 744, 754, 0, Cobbles);
            StreetY("Bell Passage", 655, 659, 762, 772, 0, Cobbles);
            Lanes("Sea Quay", Setts, false, Rect(480, 812, 740, 830), Use.Walk);
            Lanes("Basin Quay", Setts, false, Rect(713, 784, 780, 790), Use.Walk);

            // Crown Avenue (§24.2): 3 m pavements, two carriageways of 6 m and the mall of 6 m between them, from the
            // Parade down to the High Street.
            Lanes("Crown Avenue", Flags, false, Rect(600, 680, 603, 726), Use.Walk);
            Lanes("Crown Avenue", Flags, false, Rect(621, 680, 624, 726), Use.Walk);
            Lanes("Crown Avenue", Asphalt, true, Rect(603, 680, 609, 726), Use.Road);
            Lanes("Crown Avenue", Asphalt, true, Rect(615, 680, 621, 726), Use.Road);
            Lanes("Crown Avenue", Square, false, Rect(609, 680, 615, 726), Use.Walk);

            // Squares and yards: no building stands on them; props may.
            Lanes("Palace Forecourt", Square, false, Rect(586, 650, 638, 668), Use.Walk);
            Lanes("Market Place", Square, false, Rect(550, 744, 606, 784), Use.Walk);
            Lanes("Ritual Square", Square, false, Rect(630, 784, 656, 810), Use.Walk);
            Lanes("Chapel Forecourt", Square, false, Rect(480, 650, 502, 668), Use.Walk);
            Lanes("Wardley Garden", Grass, false, Rect(546, 684, 570, 712), Use.Walk);
            Lanes("Town Hall Yard", Cobbles, false, Rect(570, 686, 582, 716), Use.Walk);
            Lanes("Cellar Yard", Cobbles, false, Rect(709, 690, 721, 726), Use.Walk);
            Lanes("Soot & Bell Yard", Cobbles, false, Rect(695, 757, 701, 772), Use.Walk);
            Lanes("Tenement Court", Cobbles, false, Rect(646, 754, 660, 762), Use.Walk);
            Lanes("Pawn Corner", Cobbles, false, Rect(690, 802, 696, 810), Use.Walk);

            // Tanners Slant, at 45° from the High Street to Cheap Street (§12): setts, 8 m, no pavements, dark.
            Lanes("Tanners Slant", Setts, false, Along(SlantStart, SlantEnd, 4f, 6f), Use.Road);

            // What is not ground to build on: the river, the basin, the Soot Stair, the town wall's line, the bridge.
            Take(Rect(778, 616, 830, 830), Use.Solid);
            Take(Rect(740, 790, 860, 830), Use.Solid);
            Take(Rect(698, 677, 710, 726), Use.Solid);
            Take(Rect(470, 616, 480, 840), Use.Solid);
            Take(Rect(470, 616, 790, 626), Use.Solid);
            Take(Rect(480, 679, 780, 681), Use.Solid); // the retaining wall
        }

        private static void PaintStreets()
        {
            var data = _terrain.terrainData;
            var res = data.heightmapResolution;
            var raw = data.GetHeights(0, 0, res, res);
            var heights = new float[res, res];
            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    heights[z, x] = raw[z, x] * data.size.y;
                }
            }

            KitStandIns.EnsureFolder(LayerFolder);
            var layers = data.terrainLayers.Take(5).ToList();
            foreach (var (name, texture, tile) in StreetLayers)
            {
                layers.Add(StreetLayer(name, texture, tile));
            }

            // Unity terrain traps: the resolution setter discards the weights, which are all painted again below.
            data.alphamapResolution = AlphamapResolution;
            data.terrainLayers = layers.ToArray();
            var canonical = VeilmouthGround.Paint(heights, AlphamapResolution);
            var maps = new float[AlphamapResolution, AlphamapResolution, layers.Count];
            for (var z = 0; z < AlphamapResolution; z++)
            {
                for (var x = 0; x < AlphamapResolution; x++)
                {
                    for (var l = 0; l < 5; l++)
                    {
                        maps[z, x, l] = canonical[z, x, l];
                    }
                }
            }

            var cell = VeilmouthScene.Extent / AlphamapResolution;
            foreach (var lane in _lanes)
            {
                // No pavement paint any more (R4): under the raised islands, and where a carriageway now crosses a
                // pavement lane, the ground is the carriageway's setts.
                var layer = lane.Layer == Flags ? Setts : lane.Layer;
                var x0 = Mathf.Max(0, Mathf.FloorToInt(lane.Quad.Min(p => p.x) / cell) - 1);
                var x1 = Mathf.Min(AlphamapResolution - 1, Mathf.CeilToInt(lane.Quad.Max(p => p.x) / cell) + 1);
                var z0 = Mathf.Max(0, Mathf.FloorToInt((VeilmouthScene.Extent - lane.Quad.Max(p => p.y)) / cell) - 1);
                var z1 = Mathf.Min(AlphamapResolution - 1, Mathf.CeilToInt((VeilmouthScene.Extent - lane.Quad.Min(p => p.y)) / cell) + 1);
                for (var z = z0; z <= z1; z++)
                {
                    for (var x = x0; x <= x1; x++)
                    {
                        var p = new Vector2((x + 0.5f) * cell, VeilmouthScene.Extent - ((z + 0.5f) * cell));
                        if (!Inside(lane.Quad, p))
                        {
                            continue;
                        }

                        for (var l = 0; l < layers.Count; l++)
                        {
                            maps[z, x, l] = l == layer ? 1f : 0f;
                        }
                    }
                }
            }

            data.SetAlphamaps(0, 0, maps);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }

        /// <summary>A street surface from Yughues' pavements, created once; its look is then tuned in the Inspector.</summary>
        private static TerrainLayer StreetLayer(string name, string texture, float tile)
        {
            var path = $"{LayerFolder}/Slice_{name}.terrainlayer";
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer != null)
            {
                return layer;
            }

            const string textures = "Assets/YughuesFreePavementsMaterials/Textures/";
            layer = new TerrainLayer
            {
                diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{textures}T_YFPM_{texture}_d.tga"),
                normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{textures}T_YFPM_{texture}_n.tga"),
                tileSize = new Vector2(tile, tile),
            };
            AssetDatabase.CreateAsset(layer, path);
            return layer;
        }

        // ------------------------------------------------------------------------------------------- walls

        private static void Walls()
        {
            // The old town wall (§3.8): 2 m thick, 6 m over the ground, along the west and north, with the West Gate,
            // the North Gate and the chapel's postern; a tower either side of each gate.
            // A run stops at its tower's face and the lintel spans between the towers at the wall's own thickness and
            // top: no two faces lie in one plane (the owner saw the gate flicker, 2026-11-06).
            WallRun(true, 478f, 480f, 626, 723, 6f);
            WallRun(true, 478f, 480f, 747, 830, 6f);
            WallRun(false, 624f, 626f, 478, 735, 6f);
            WallRun(false, 624f, 626f, 759, 780, 6f);
            Lintel(true, 478f, 480f, 728f, 742f, 6f);
            Lintel(false, 624f, 626f, 740f, 754f, 6f);
            // The gate towers are the kit's (§24.3): five storeys of dark brick under a hipped pyramid, over the wall's 6 m.
            var tower = VictorianHouses.Key("Tower5", "dark");
            House(tower, Facing.W, 475f, 723, name: "West Gate - north tower", force: true);
            House(tower, Facing.W, 475f, 742, name: "West Gate - south tower", force: true);
            House(tower, Facing.N, 621f, 735, name: "North Gate - west tower", force: true);
            House(tower, Facing.N, 621f, 754, name: "North Gate - east tower", force: true);

            // The Ridge's retaining wall, with a parapet of 1 m, wherever the two levels differ: the Parade's promenade
            // keeps it as its balustrade (§24.2). It stops where a street crosses its line (the owner found Crown Avenue
            // walled off, 2026-11-06): the avenue's ramp and the Soot Stair.
            for (var px = 480; px < 780; px += 2)
            {
                var cell = Rect(px, 679.3f, px + 2, 680.7f);
                if ((px >= 698 && px < 710) || (px >= 600 && px < 624) ||
                    _lanes.Any(l => !l.PaintOnly && l.Quad.Min(q => q.y) < 679f && l.Quad.Max(q => q.y) > 681f && Overlap(l.Quad, cell)))
                {
                    continue;
                }

                var top = Mathf.Max(Ground(px + 1f, 678.5f), Ground(px + 1f, 677f));
                var bottom = Mathf.Min(Ground(px + 1f, 681.5f), Ground(px + 1f, 683f));
                if (top - bottom < 0.3f)
                {
                    continue;
                }

                Slab(px, px + 2, 679.3f, 680.7f, bottom - 0.5f, top + 1f);
            }

            // The Soot Stair's blind walls, either side of its causeway, at least 3.2 m over the low ground.
            for (var py = 680; py < 714; py += 2)
            {
                var causeway = Mathf.Max(Ground(704f, py), Ground(704f, py + 2f));
                var top = Mathf.Max(causeway + 1f, Low + 3.2f);
                Slab(698.6f, 700f, py, py + 2, Low - 0.5f, top);
                Slab(708f, 709f, py, py + 2, Low - 0.5f, top);
            }

            Slab(698.6f, 709f, 714f, 714.6f, Low - 0.5f, Low + 0.3f); // the stair's last step

            // The cellar door in the stair's east wall, opening on its yard (§5.3).
            _works.Box(World(709.05f, 700.5f, Low + 1.2f), new Vector3(0.1f, 2.4f, 1.6f), 0f, false, "VIC_Wood_Oxblood");
            var leaf = (GameObject)PrefabUtility.InstantiatePrefab(_kit["PortalLeaf"].Prefab, _parent);
            leaf.transform.SetPositionAndRotation(World(709f, 699f, Up(Low)), Quaternion.Euler(0f, 90f, 0f));
            Configure(leaf, "soot-stair-cellar.street", VeilmouthDressing.CellarScenePath, "soot-stair-cellar.inside");
            _doors.Add((new Vector2(709f, 700.5f), new Vector2(1f, 0f)));

            // The river's embankment along the city, with a parapet; the basin's walls; the sea quay's edge.
            // It runs on to the bridge's parapets, so no corner is left open (owner's fourth play test, 2026-11-06).
            for (var py = 626; py < 790; py += 4)
            {
                var top = Ground(778f, py + 2f) + 1f;
                if (py + 4 > BridgeNorth && py < BridgeSouth)
                {
                    if (py < BridgeNorth)
                    {
                        Slab(779f, 780.5f, py, BridgeNorth, VeilmouthGround.RiverBed - 0.5f, top);
                    }

                    if (py + 4 > BridgeSouth)
                    {
                        Slab(779f, 780.5f, BridgeSouth, py + 4, VeilmouthGround.RiverBed - 0.5f, top);
                    }

                    continue;
                }

                Slab(779f, 780.5f, py, py + 4, VeilmouthGround.RiverBed - 0.5f, top);
            }

            for (var py = 790; py < 830; py += 4)
            {
                Slab(739f, 740.5f, py, py + 4, VeilmouthGround.SeaBed - 2f, Ground(738f, py + 2f) + 0.3f);
            }

            for (var px = 740; px < 780; px += 4)
            {
                Slab(px, px + 4, 789f, 790.5f, VeilmouthGround.SeaBed - 1f, Ground(px + 2f, 787f) + 0.3f);
            }

            for (var px = 480; px < 740; px += 4)
            {
                Slab(px, px + 4, 829.5f, 831f, VeilmouthGround.SeaBed - 3f, Ground(px + 2f, 828f) + 0.3f);
            }

            Bridge();
        }

        /// <summary>A run of wall along X (or Y) in segments of 4 m that follow the ground, with optional gaps.</summary>
        private static void WallRun(bool alongY, float t0, float t1, int a0, int a1, float height, params (int, int)[] gaps)
        {
            for (var a = a0; a < a1; a += 4)
            {
                var b = Mathf.Min(a + 4, a1);
                if (gaps.Any(g => a < g.Item2 && b > g.Item1))
                {
                    foreach (var (g0, g1) in gaps)
                    {
                        if (a < g0)
                        {
                            Segment(a, Mathf.Min(b, g0));
                        }

                        if (b > g1)
                        {
                            Segment(Mathf.Max(a, g1), b);
                        }
                    }

                    continue;
                }

                Segment(a, b);
            }

            void Segment(float s0, float s1)
            {
                var mid = (t0 + t1) / 2f;
                var g0 = alongY ? Ground(mid, s0) : Ground(s0, mid);
                var g1 = alongY ? Ground(mid, s1) : Ground(s1, mid);
                var gm = alongY ? Ground(mid, (s0 + s1) / 2f) : Ground((s0 + s1) / 2f, mid);
                var lo = Mathf.Min(g0, Mathf.Min(g1, gm));
                var hi = Mathf.Max(g0, Mathf.Max(g1, gm));
                if (alongY)
                {
                    Slab(t0, t1, s0, s1, lo - 1f, hi + height);
                }
                else
                {
                    Slab(s0, s1, t0, t1, lo - 1f, hi + height);
                }
            }
        }

        /// <summary>A gate's lintel: the wall's thickness, from 4.5 m over the gate's ground up to the wall's top.</summary>
        private static void Lintel(bool alongY, float t0, float t1, float s0, float s1, float height)
        {
            var mid = (t0 + t1) / 2f;
            float G(float s) => alongY ? Ground(mid, s) : Ground(s, mid);
            var g = Mathf.Max(G(s0), Mathf.Max(G(s1), G((s0 + s1) / 2f)));
            var top = Mathf.Max(G(s0 - 2f), G(s1 + 2f)) + height;
            if (alongY)
            {
                Slab(t0, t1, s0, s1, g + 4.5f, Mathf.Max(top, g + 5.5f));
            }
            else
            {
                Slab(s0, s1, t0, t1, g + 4.5f, Mathf.Max(top, g + 5.5f));
            }
        }

        private static void Slab(float px0, float px1, float py0, float py1, float y0, float y1, string material = null) =>
            _works.Box(World((px0 + px1) / 2f, (py0 + py1) / 2f, (y0 + y1) / 2f), new Vector3(px1 - px0, y1 - y0, py1 - py0), 0f,
                true, material);

        private const float BridgeNorth = 726.4f, BridgeSouth = 743.6f; // the parapets' outer faces

        /// <summary>
        /// The Veil Bridge (§3.8): a stone deck at street level over the river on piers, with parapets, to the east bank.
        /// Each end runs 2 m on to the land and an abutment fills the bank under it down to the river bed, so no gap is
        /// left between the deck and the ground (owner's fourth play test, 2026-11-06).
        /// </summary>
        private static void Bridge()
        {
            const float deck = Low + 0.1f;
            Slab(778f, 854f, 727f, 743f, deck - 1.2f, deck, DeckStone);
            Slab(780f, 852f, BridgeNorth, 727f, deck - 1.2f, deck + 1.1f);
            Slab(780f, 852f, 743f, BridgeSouth, deck - 1.2f, deck + 1.1f);
            Slab(778f, 780.5f, BridgeNorth, BridgeSouth, VeilmouthGround.RiverBed - 0.5f, deck - 1.2f);
            Slab(840f, 854f, BridgeNorth, BridgeSouth, VeilmouthGround.RiverBed - 0.5f, deck - 1.2f);
            foreach (var px in new[] { 792f, 806f, 820f, 834f })
            {
                Slab(px - 1.5f, px + 1.5f, 725f, 745f, VeilmouthGround.RiverBed - 1f, deck - 1.2f);
            }

            Take(Rect(778, 725, 852, 745), Use.Solid);
        }

        // ------------------------------------------------------------------------------------------- buildings

        private static void Government()
        {
            // The Governor's Palace closes the Crown Axis (§24.3): 40 × 15 m in white stucco behind its forecourt.
            House(VictorianHouses.Key("Palace", "white"), Facing.S, 650f, 592, name: "The Governor's Palace", force: true);

            // The Town Hall on the avenue's west flank, facing it from its podium over the ramp, and its clock tower at
            // the corner of the avenue and the High Street: six storeys, the tallest thing in the city.
            House(VictorianHouses.Key("Civic", "rich"), Facing.E, 597f, 686, name: "The Town Hall", force: true);
            House(VictorianHouses.Key("Tower6", "white"), Facing.E, 597f, 721, name: "The clock tower", force: true);
        }

        private static void Openable()
        {
            // The Wardens' station and lock-up in the Assize, on the avenue's east flank (§3.1, §5.3).
            var assize = House(VictorianHouses.Key("Civic", "stucco"), Facing.W, 627f, 686, name: "The Assize and the Wardens' station", force: true);
            Entrance(assize, "wardens-lockup.street", VeilmouthDressing.LockupScenePath, "wardens-lockup.inside");

            // The faction's house, a villa north of the Parade in the largest plot (§19, §24.3).
            var faction = Plot(Facing.S, VillaFront, FactionPlot0, FactionPlot1, VictorianHouses.Key("Villa", "stucco"), VillaGarden, VillaBehind,
                new Random(FactionPlot0), "The faction's house", FactionAt, true);
            Entrance(faction, "faction-house.street", VeilmouthDressing.FactionHouseScenePath, "faction-house.inside");

            // The Soot & Bell on Bell Street with its yard beside it on Dock Road, its back door to that yard, and
            // Harrow & Tincture by it.
            var bell = House(VictorianHouses.Key("Tavern", "brick"), Facing.S, 772f, 680, name: "The Soot & Bell", force: true);
            Entrance(bell, VeilmouthScene.SootAndBellStreetPortalId, VeilmouthScene.SootAndBellScenePath, VeilmouthScene.SootAndBellInsidePortalId);
            VeilmouthYardDoor.Leaf(bell.transform, _kit, bell.transform.parent);
            var harrow = House(VictorianHouses.CornerKey(VictorianHouses.Key("ShopTown8", "dark"), VictorianHouses.Side.Both), Facing.S, 772f, 670,
                name: "Harrow & Tincture", force: true);
            Entrance(harrow, "harrow-apothecary.street", VeilmouthDressing.HarrowScenePath, "harrow-apothecary.inside");
            Prop("NoticeBoard", Facing.S, 697f, 768f, "Notice Board");

            // The missing person's tenement on Loom Lane.
            var missing = House(VictorianHouses.CornerKey(VictorianHouses.Key("Tenement8", "poor"), VictorianHouses.Side.Both), Facing.N, 802f, 662,
                name: "The missing person's tenement", force: true);
            Entrance(missing, "missing-room.street", VeilmouthDressing.MissingRoomScenePath, "missing-room.inside");

            // The old chapel in the north-west corner, its forecourt on the Parade (§24.3).
            var chapelKey = "Chapel";
            var chapel = House(chapelKey, Facing.S, 650f, 484, name: "The old chapel", force: true);
            var right = chapel.transform.right;
            var door = chapel.transform.position + (chapel.transform.rotation * new Vector3(_kit[chapelKey].DoorX, 0f, _kit[chapelKey].Depth));
            var leaf = (GameObject)PrefabUtility.InstantiatePrefab(_kit["PortalLeaf"].Prefab, _parent);
            var start = door - (right * 1.5f);
            leaf.transform.SetPositionAndRotation(new Vector3(Mathf.Round(start.x), Up(door.y), Mathf.Round(start.z)), chapel.transform.rotation);
            Configure(leaf, "old-chapel.street", VeilmouthDressing.ChapelScenePath, "old-chapel.inside");
            _doors.Add((Plan(door), new Vector2(0f, 1f)));
        }

        /// <summary>Tanners Slant at 45° (§12): a frame on a world node; its houses keep the grid and the turns in its axes.</summary>
        private static void Slant()
        {
            var frame = new GameObject("Tanners Slant (45°)");
            frame.transform.SetParent(_root, false);
            frame.transform.SetPositionAndRotation(World(SlantStart.x, SlantStart.y), Quaternion.Euler(0f, 45f, 0f));
            frame.AddComponent<KitFrame>();
            // The poor end of the Market Quarter: low tenements of 10 m, a run on each side with its ends faced.
            var palette = LowTenementRow;
            var rng = new Random(45);
            foreach (var side in new[] { 1, -1 })
            {
                // In the frame's axes a house's local −X lies toward the slant's end (+X) on the side whose fronts turn
                // round (side 1), toward its start on the other.
                var placed = new List<(string Key, int X, Vector2[] Quad, float Z0)>();
                for (var x = 6; x + 10 <= 36; x += 10)
                {
                    var key = palette[rng.Next(palette.Length)];
                    var e = _kit[key];
                    var z0 = side > 0 ? 4f : -4f - e.Depth;
                    var local = new[] { new Vector2(x, z0), new Vector2(x + e.Width, z0), new Vector2(x + e.Width, z0 + e.Depth), new Vector2(x, z0 + e.Depth) };
                    var quad = local.Select(l => Plan(frame.transform.TransformPoint(new Vector3(l.x, 0f, l.y)))).ToArray();
                    if (!Free(quad, true))
                    {
                        break; // a run stays unbroken, or its open ends would show
                    }

                    placed.Add((key, x, quad, z0));
                }

                var endSide = side > 0 ? VictorianHouses.Side.Left : VictorianHouses.Side.Right;
                var startSide = side > 0 ? VictorianHouses.Side.Right : VictorianHouses.Side.Left;
                for (var i = 0; i < placed.Count; i++)
                {
                    var (key, x, quad, z0) = placed[i];
                    key = placed.Count == 1 ? VictorianHouses.CornerKey(key, VictorianHouses.Side.Both)
                        : i == 0 ? VictorianHouses.CornerKey(key, startSide)
                        : i == placed.Count - 1 ? VictorianHouses.CornerKey(key, endSide) : key;
                    var e = _kit[key];
                    Take(quad, Use.Solid);
                    _houses.Add(quad);
                    var (min, max) = GroundUnder(quad);
                    var turns = side > 0 ? 2 : 0;
                    var house = Put(key, frame.transform, x, z0, turns, Up(max));
                    foreach (Transform child in house.transform)
                    {
                        if (child.name == VictorianHouses.DoorMarker)
                        {
                            var f = house.transform.forward;
                            Doorway(child.position, new Vector2(f.x, -f.z), Up(max), e.Stoop, e.StoopWidth);
                        }
                    }

                    Stoops(house);

                    if (Up(max) - min > 0.25f)
                    {
                        var centre = frame.transform.TransformPoint(new Vector3(x + (e.Width / 2f), 0f, z0 + (e.Depth / 2f)));
                        _works.Box(new Vector3(centre.x, (Up(max) + min - 0.6f) / 2f, centre.z), new Vector3(e.Width, Up(max) - min + 0.6f, e.Depth), 45f);
                    }
                }
            }
        }

        private static void Houses()
        {
            // High Street (§24.3): shop houses of four storeys and one cornice height, wall to wall, broken by the
            // passages; the Market Place opens off its south side.
            Row(Facing.S, 726f, 480, 778, ShopRow, 1);
            Row(Facing.N, 744f, 480, 778, ShopRow, 2);

            // The Terraces and the Ridge: detached houses, each on its own railed plot with its garden (§19).
            var rng = new Random(3);
            foreach (var (f, front, spans) in TerracePlots)
            {
                foreach (var (s0, s1) in spans)
                {
                    Plot(f, front, s0, s1, TerraceKeys[rng.Next(TerraceKeys.Length)], TerraceGarden, TerraceBehind, new Random(s0 * 7 + (int)front));
                }
            }

            foreach (var (s0, s1) in VillaPlots)
            {
                Plot(Facing.S, VillaFront, s0, s1, VillaKeys[rng.Next(VillaKeys.Length)], VillaGarden, VillaBehind, new Random(s0));
            }

            // The gate lodge east of North Road.
            Plot(Facing.S, VillaFront, 754, 778, VictorianHouses.CornerKey(VictorianHouses.Key("Terrace8", "rich"), VictorianHouses.Side.Both),
                VillaGarden, VillaBehind, new Random(754), "The gate lodge");

            // Under the Ridge, on Ridge Lane and Stair Lane: tenements 7.5 m deep, with yards behind them.
            Row(Facing.S, 696f, 642, 700, TenementRow, 9);
            Row(Facing.N, 702f, 642, 700, TenementRow, 10);
            Row(Facing.S, 696f, 721, 778, TenementRow, 24);
            Row(Facing.N, 702f, 721, 778, TenementRow, 25);

            // The Market Place's own sides first (§24.3): shops on the west and south. The west row stands 1 m into the
            // square, so the back steps of its yard doors clear Cheap Street's houses behind (a 3 m yard, not 2 m).
            Row(Facing.E, 551f, 744, 772, ShopRow, 16);
            Row(Facing.N, 784f, 550, 606, ShopRow, 15);

            // Lower Veil: Bell Street's two sides, Loom Lane's tenements, Dock Road's warehouses.
            Row(Facing.S, 772f, 618, 701, TownRow, 11);
            Row(Facing.N, 784f, 618, 701, TownRow, 12);
            Row(Facing.N, 802f, 618, 701, TenementRow, 20);
            Row(Facing.E, 701f, 744, 812, WarehouseRow, 21);
            Row(Facing.W, 713f, 744, 784, WarehouseRow, 22);

            // Warehouses face the Sea Quay along the Market Quarter, with yards behind them.
            Row(Facing.S, 812f, 480, 606, WarehouseRow, 23);

            // The Market Quarter's side streets: town houses on Mercers Row, Cheap Street and Market Street.
            Row(Facing.S, 772f, 480, 550, TownRow, 17);
            Row(Facing.N, 784f, 480, 550, TownRow, 18);
            Row(Facing.E, 516f, 744, 812, TownRow, 19);
            Row(Facing.W, 528f, 744, 812, TownRow, 26);
            Row(Facing.E, 606f, 744, 812, TownRow, 14);
            Row(Facing.W, 618f, 744, 812, TownRow, 13);

            // The Customs House on the Basin Quay.
            House(VictorianHouses.Key("Civic", "white"), Facing.S, 784f, 745, name: "The Customs House", force: true);
        }

        // ------------------------------------------------------------------------------------------- squares

        private static void Market()
        {
            // The Market Place (§24.3, §6.1): two rows of stalls with an aisle and benches between them, the Masters'
            // counters and the seer's booth on the north row, the Item Creation yard hedged in the south-west corner: no
            // field fence stands inside the walls, and no barrel or firewood is set down for its own sake (§25.6).
            Prop("Stall_Goods", Facing.S, 560f, 750f, "The trader's stall");
            Prop("Stall_Empty", Facing.S, 569f, 750f, "Master of Enchanting");
            Prop("Stall_Empty", Facing.S, 578f, 750f, "Master of First Aid");
            Prop("BottleTable", Facing.S, 587f, 751f);
            Prop("Counter", Facing.S, 593f, 751f, "The seer's counter");
            Prop("Candles", Facing.S, 597f, 751f);
            Prop("Stall_Food", Facing.N, 570f, 768f);
            Prop("Stall_Fish", Facing.N, 579f, 768f);
            Prop("Stall_Meat", Facing.N, 588f, 768f);
            Prop("Stall_Weapons", Facing.E, 553f, 772f, "Master of Item Creation");
            HedgeLine(false, 565f, 770, 782, new Random(565)); // short of the south row's steps at 782–784

            Prop("Anvil", Facing.N, 559f, 775f);
            Prop("LeatherFrame", Facing.N, 560f, 779f);
            Prop("ArmourStand", Facing.N, 556f, 781f);
            foreach (var (px, py) in new[] { (572, 760), (580, 760), (588, 760) })
            {
                Prop("Bench", Facing.N, px, py);
            }
        }

        private static void Squares()
        {
            // Crown Avenue's mall (§24.2): a tree every 10 m down its middle, benches either side between them, and the
            // Crown Column at its foot.
            // Its trees are the road's tree (§27.10), the avenue running along plan y.
            for (var py = 684; py < 714; py += 10)
            {
                Prop(RoadTree, Facing.N, 611f, py, crownYaw: RoadYaw(false, new Vector2(611f, py)));
                Prop("Bench", Facing.E, 609f, py + 4f);
                Prop("Bench", Facing.W, 614f, py + 4f);
            }

            Prop("Column", Facing.N, 611f, 719f, "The Crown Column");
            foreach (var (px, py) in new[] { (588, 653), (636, 653) })
            {
                Prop(CornerTree(px), Facing.N, px, py);
            }

            var rng = new Random(606);
            Parterre(rng);

            // Wardley Garden: railings round a lawn, trees at its corners, benches, a gate from the Garden Walk and one
            // to the passage down to the High Street.
            Railings(true, 685f, 547, 555);
            Railings(true, 685f, 561, 569);
            Railings(true, 711f, 547, 555);
            Railings(true, 711f, 561, 569);
            Railings(false, 547f, 685, 711);
            Railings(false, 569f, 685, 711);
            foreach (var (px, py) in new[] { (551, 689), (564, 689), (551, 706), (564, 706), (557, 697) })
            {
                Prop(GardenTree(rng), Facing.N, px, py);
            }

            foreach (var (px, py, f) in new[] { (550, 693, Facing.S), (561, 693, Facing.S), (550, 701, Facing.N), (561, 701, Facing.N) })
            {
                Prop("Bench", f, px, py);
            }

            WardleyBeds(rng);

            // The chapel's graveyard and forecourt; Ritual Site Candidate 3 is the forecourt (§24.3).
            for (var px = 505; px <= 526; px += 3)
            {
                for (var py = 631; py <= 643; py += 4)
                {
                    Prop("Grave", Facing.S, px, py);
                }
            }

            for (var px = 503; px < 527; px += 4)
            {
                Prop("GraveWall", Facing.N, px, 627.9f);
                Prop("GraveWall", Facing.N, px, 649.9f);
            }

            Lawn(Rect(503, 629, 527, 649), rng); // grass between the graves (§25.4)

            Circle(World(491f, 659f, Up(Ground(491f, 659f))), "Candidate 3, before the old chapel");

            // Ritual Site Candidate 1, the Ritual Square in Lower Veil, between Bell Street and the sea, across Loom Lane.
            Prop("Bench", Facing.N, 633f, 786f);
            Prop("Bench", Facing.N, 650f, 786f);
            Prop("TrashCan", Facing.N, 631f, 807f);
            Circle(World(643f, 797f, Up(Ground(643f, 797f))), "Candidate 1, the Ritual Square");

            // The Soot & Bell's yard, the tenement court and the cellar yard stay clear: barrels, crates and boxes stand
            // only in the port, on Dock Road and in stores (owner, 2026-11-09; §25.6).
            // The pawnbroker's counter in its corner off Loom Lane.
            Prop("Counter", Facing.N, 691f, 803f, "Pawn shop counter");
            Prop("Chest", Facing.N, 693f, 808f, "Pawn shop chest");

            // The docks (§6.1): cargo on the quays, and at the warehouses on Dock Road.
            var cargo = new Random(812);
            for (var px = 490; px < 730; px += 22)
            {
                Prop(cargo.Next(2) == 0 ? "Barrels" : "Crate", Facing.N, px + cargo.Next(6), 820f + cargo.Next(4));
                Prop("Box", Facing.N, px + 8 + cargo.Next(4), 821f + cargo.Next(4));
            }

            Prop("Barrels", Facing.N, 716f, 785f);
            Prop("Crate", Facing.N, 726f, 785f);
        }

        // ------------------------------------------------------------------------------------------- lamps

        /// <summary>
        /// The furniture rules (§4), with the owner's fifth play test (2026-11-09, §26): a lamp on the carriageway by the
        /// kerb, its post <see cref="LampFromKerb"/> out from it, at a party wall where one is near, never within 2 m of a
        /// door along the street and never where a carriageway or a rounded corner breaks the kerb; 25–30 m apart, the two
        /// sides staggered on streets wider than 12 m. The lamps that stood on the crossroads' corners are gone: a corner
        /// is the kerb's rounding, and the rows' lamps light it.
        /// </summary>
        private static void AllLamps()
        {
            // The High Street, 18 m, staggered; the Parade by its kerbs at the pavement and the promenade.
            Lamps(true, 726f + HighStreetWalk, 1, 480, 778, 28f, 4f);
            Lamps(true, 744f - HighStreetWalk, -1, 480, 778, 28f, 18f);
            Lamps(true, 668f + ParadeWalk, 1, 480, 752, 27f, 6f);
            Lamps(true, 677f, -1, 480, 742, 27f, 6f);
            // Crown Avenue by its outer kerbs, and the byelaw streets of 12 m by both kerbs.
            Lamps(false, 603f, 1, 680, 726, 25f, 5f);
            Lamps(false, 621f, -1, 680, 726, 25f, 5f);
            foreach (var (alongX, near, far, a0, a1, spacing, phase) in new[]
                     {
                         (false, 516f, 528f, 680, 726, 26f, 8f), (true, 772f, 784f, 480, 550, 27f, 5f), (true, 772f, 784f, 618, 701, 27f, 5f),
                         (false, 516f, 528f, 744, 812, 27f, 12f), (false, 606f, 618f, 744, 812, 27f, 12f), (false, 701f, 713f, 744, 812, 27f, 12f),
                     })
            {
                Lamps(alongX, near + ByelawWalk, 1, a0, a1, spacing, phase);
                Lamps(alongX, far - ByelawWalk, -1, a0, a1, spacing, phase);
            }

            // The Sea Quay has no carriageway: its lamps stand in the cell along its edge.
            Lamps(true, 813f, 0, 480, 740, 30f, 10f);

            // The palace forecourt's gate, the Market Place's corners, the Ritual Square's and the courts' single lamps.
            foreach (var (px, py) in new[]
                     {
                         (587, 666), (636, 666), (551, 745), (604, 745), (551, 782), (604, 782), (631, 785), (654, 808), (658, 758), (709, 724),
                     })
            {
                Prop("LampPost", Facing.N, px, py);
            }
        }

        private static readonly List<Vector2> _lampsAt = new List<Vector2>();

        /// <summary>
        /// A row of lamps by the kerb on the plan line <paramref name="line"/>, its carriageway on the side
        /// <paramref name="side"/> (+1 toward the greater coordinate); with side 0, in the cell past the line instead.
        /// </summary>
        private static void Lamps(bool alongX, float line, int side, int a0, int a1, float spacing, float phase)
        {
            var across = side == 0 ? line + 0.5f : line + (side * LampFromKerb);
            for (var target = a0 + phase; target < a1 - 1f; target += spacing)
            {
                var walls = _partyWalls.Where(w => w.AlongX == alongX && Mathf.Abs(w.Front - line) < 6f && Mathf.Abs(w.At - target) <= 5f)
                    .Select(w => w.At).OrderBy(w => Mathf.Abs(w - target)).ToList();
                walls.Add(Mathf.Round(target));
                foreach (var at in walls.Distinct())
                {
                    var along = at - 0.5f;
                    var c = alongX ? new Vector2(along, across) : new Vector2(across, along);
                    if (side != 0 ? !KerbRuns(alongX, line, side, along, 0.6f) : _pavements.Cut(c.x, c.y))
                    {
                        continue;
                    }

                    // The door's 8 m reach counts to the lamp's centre.
                    if (_doors.Any(d => Mathf.Abs((alongX ? d.At.y : d.At.x) - across) < 8.5f && Mathf.Abs((alongX ? d.At.x : d.At.y) - along) < 2.2f))
                    {
                        continue;
                    }

                    var placed = side == 0
                        ? alongX ? Prop("LampPost", Facing.N, along - 0.5f, line) : Prop("LampPost", Facing.N, line, along - 0.5f)
                        : OnCarriageway("LampPost", c, 1f);
                    if (placed != null)
                    {
                        _lampsAt.Add(c);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Whether the kerb on the plan line runs unbroken for <paramref name="half"/> m either side of a point along it:
        /// the pavement on its inside, no carriageway or rounded corner crossing it.
        /// </summary>
        private static bool KerbRuns(bool alongX, float line, int side, float along, float half)
        {
            var inside = line - (side * 0.3f);
            for (var d = -half; d <= half + 0.001f; d += half)
            {
                var p = alongX ? new Vector2(along + d, inside) : new Vector2(inside, along + d);
                if (!_pavements.On(p.x, p.y))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// A block of one cell at a carriageway's plan point, if nothing solid stands within the square of
        /// <paramref name="size"/> m round it, which it then takes; set on the carriageway's own ground. The block keeps
        /// its grid cell, and what is seen moves inside the cell to the point with its body, so the Combat Grid reads it in
        /// the same cell: a lamp's post 0.3 m from the kerb, a tree's trunk in the middle of its pit (§26.1), its crown
        /// turned to <paramref name="crownYaw"/> where it is given.
        /// </summary>
        private static GameObject OnCarriageway(string key, Vector2 c, float size, float? crownYaw = null)
        {
            var area = Rect(c.x - (size / 2f), c.y - (size / 2f), c.x + (size / 2f), c.y + (size / 2f));
            if (_taken.Any(t => t.Use == Use.Solid && Overlap(t.Quad, area)))
            {
                return null;
            }

            var go = Prop(key, Facing.N, c.x - 0.5f, c.y - 0.5f, force: true, crownYaw: crownYaw);
            if (go == null)
            {
                return null;
            }

            Take(area, Use.Solid);
            var d = c - Plan(go.transform.TransformPoint(new Vector3(0.5f, 0f, 0.5f)));
            var shift = new Vector3(d.x, 0f, -d.y);
            if (shift.sqrMagnitude > 1e-6f)
            {
                var model = go.transform.Find("Model");
                if (model != null)
                {
                    model.position += shift;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(model);
                }

                var local = go.transform.InverseTransformVector(shift);
                foreach (var body in go.GetComponents<Collider>())
                {
                    switch (body)
                    {
                        case CapsuleCollider capsule: capsule.center += local; break;
                        case BoxCollider box: box.center += local; break;
                        case SphereCollider sphere: sphere.center += local; break;
                    }

                    PrefabUtility.RecordPrefabInstancePropertyModifications(body);
                }
            }
            if (!key.StartsWith("CityTree_"))
            {
                var p = go.transform.position;
                // On the kit's 0.1 m height step, its foot sunk at most that much into the carriageway.
                go.transform.position = new Vector3(p.x, Mathf.Floor((Ground(c.x, c.y) * 10f) + 0.02f) / 10f, p.z);
            }

            return go;
        }

        // ------------------------------------------------------------------------------------------- outside

        private static void Fields()
        {
            var farm = "House_Farm";
            foreach (var (px, py, f) in new[] { (150f, 640f, Facing.S), (200f, 700f, Facing.N), (110f, 705f, Facing.N), (230f, 600f, Facing.E) })
            {
                House(farm, f, f == Facing.S ? py : f == Facing.N ? py : px, (int)(f == Facing.E ? py : px), force: true);
            }

            // The flax field, fenced with Roadside Tales' field fence (§25.5), a gap in its south side.
            var fence = 0;
            string FieldFence() => FieldFences[fence++ % FieldFences.Length];
            for (var px = 170; px < 186; px += 2)
            {
                if (px != 178)
                {
                    Prop(FieldFence(), Facing.N, px, 776f - 0.1f, force: true);
                }

                Prop(FieldFence(), Facing.N, px, 760f - 0.1f, force: true);
            }

            for (var py = 760; py < 776; py += 2)
            {
                Prop(FieldFence(), Facing.E, 170f - 0.1f, py, force: true);
                Prop(FieldFence(), Facing.E, 186f - 0.1f, py, force: true);
            }

            Roadside();

            for (var i = 0; i < 5; i++)
            {
                for (var j = 0; j < 2; j++)
                {
                    Prop("Flax", Facing.N, 172 + (3 * i), 764 + (6 * j));
                }
            }

            var rng = new Random(250);
            for (var i = 0; i < 260; i++)
            {
                var px = 60 + rng.Next(410);
                var py = 520 + rng.Next(280);
                if (Mathf.Abs(py - 735) < 8)
                {
                    continue;
                }

                Prop(i % 6 == 0 ? "Shrub" : i % 9 == 0 ? "Poppy" : "Grass", Facing.N, px, py);
            }

            foreach (var (px, py) in new[] { (140, 620), (148, 622), (160, 618), (210, 720), (218, 724), (96, 690) })
            {
                Prop("Tree_Fruit", Facing.N, px, py);
            }
        }

        private static readonly string[] Fellable = { "Birch1", "Birch2", "Birch3", "Warped1", "Warped2", "Warped3" };

        private static void Forest()
        {
            var rng = new Random(20261105); // authoring layout only, fixed; never the world seed
            var glades = new[]
            {
                new Vector2(130, 250), new Vector2(250, 330), new Vector2(360, 180), new Vector2(470, 380), new Vector2(560, 240),
                new Vector2(700, 300), new Vector2(250, 120), new Vector2(420, 420),
            };
            bool InForest(float px, float py) => px > 60 && px < 735 && py > 60 && py < 455; // the North Road runs at px 742–752

            // Glades: about 40 fellable trees (the rare coloured crowns) round them and 30 herb patches in them.
            var herbs = 0;
            foreach (var g in glades)
            {
                for (var placed = 0; placed < 5;)
                {
                    var a = rng.NextDouble() * Math.PI * 2;
                    var r = 9 + (rng.NextDouble() * 6);
                    var px = Mathf.Round(g.x + (float)(Math.Cos(a) * r));
                    var py = Mathf.Round(g.y + (float)(Math.Sin(a) * r));
                    if (Prop("Fellable_" + Fellable[rng.Next(Fellable.Length)], Facing.N, px, py) != null)
                    {
                        placed++;
                    }
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

            // Cover: the plain green trees, pines and fruit trees, and shrubs; none in the glades or on the north road.
            var trees = 0;
            for (var attempt = 0; attempt < 20000 && trees < 650; attempt++)
            {
                var px = (float)rng.Next(62, 738);
                var py = (float)rng.Next(62, 453);
                if (!InForest(px, py) || glades.Any(g => (g - new Vector2(px, py)).sqrMagnitude < 18 * 18))
                {
                    continue;
                }

                var key = rng.NextDouble() < 0.8 ? (rng.NextDouble() < 0.65 ? "Tree_Pine" : "Tree_Fruit") : "Shrub";
                var quad = Rect(px - 3f, py - 3f, px + 4f, py + 4f);
                if (!Free(quad, false))
                {
                    continue;
                }

                if (Prop(key, Facing.N, px, py) != null)
                {
                    Take(quad, Use.Walk); // spacing only
                    trees++;
                }
            }

            // Beast territories, about 30 m across and at least 60 m apart (§2 of the implementation plan), with their stand-ins.
            var territories = Group("Beast Territories");
            var wolves = new[] { (150, 150), (300, 100), (450, 90), (690, 140), (150, 360), (520, 300) };
            for (var i = 0; i < wolves.Length; i++)
            {
                var (px, py) = wolves[i];
                Territory(territories, $"Wolf Territory {i + 1}", px, py);
                foreach (var (dx, dy, yaw) in new[] { (0f, 0f, 30f), (2.5f, 1.5f, 80f), (-1.5f, 2.5f, 200f) })
                {
                    Beast(VeilmouthDressing.WolfPath, 1.6f, px + dx, py + dy, yaw, $"Wolf {i + 1} (dragon stand-in)");
                }
            }

            var boars = new[] { (250, 410), (380, 260), (680, 390), (90, 430) };
            for (var i = 0; i < boars.Length; i++)
            {
                var (px, py) = boars[i];
                Territory(territories, $"Boar Territory {i + 1}", px, py);
                Beast(VeilmouthDressing.BoarPath, 1.5f, px, py, 45f * i, $"Boar {i + 1} (dragon stand-in)");
            }
        }

        private static void Hill()
        {
            // The ore hill across the bridge: twelve iron veins on its flanks, shrubs and a few pines.
            var rng = new Random(5);
            var veins = 0;
            for (var attempt = 0; attempt < 2000 && veins < 12; attempt++)
            {
                var px = rng.Next(862, 950);
                var py = rng.Next(220, 500);
                var d = Mathf.Sqrt(Mathf.Pow((px - 920f) / 75f, 2f) + Mathf.Pow((py - 360f) / 170f, 2f));
                if (d > 0.85f || _taken.Any(t => t.Use == Use.Solid && (Centre(t.Quad) - new Vector2(px, py)).sqrMagnitude < 18 * 18))
                {
                    continue;
                }

                if (Prop("IronOre", Facing.N, px, py, $"Iron ore vein {veins + 1}") != null)
                {
                    veins++;
                }
            }

            for (var i = 0; i < 40; i++)
            {
                var px = rng.Next(855, 952);
                var py = rng.Next(200, 520);
                Prop(i % 3 == 0 ? "Tree_Pine" : "Shrub", Facing.N, px, py);
            }
        }

        // ------------------------------------------------------------------------------------------- people

        private static void Townsfolk()
        {
            // Wardens, blue: at the Assize's door, on the High Street and Bell Street.
            Person("Blue", false, 625.5f, 697f, 270f, "Warden at the station");
            Person("Blue", false, 625.5f, 705f, 270f, "Warden at the station");
            Person("Blue", false, 650f, 735f, 90f, "Warden on the High Street");
            Person("Blue", true, 640f, 777f, 270f, "Warden on Bell Street");

            // The faction, teal, in its front garden; Veinmarket, brown, on Loom Lane.
            Person("Teal", false, 652f, 666f, 180f, "Faction member");
            Person("Teal", true, 661f, 666f, 180f, "Faction member");
            Person("Brown", false, 680f, 799f, 0f, "Veinmarket");
            Person("Brown", false, 682f, 798f, 270f, "Veinmarket");

            // Masters, green, and sellers, orange, at their stalls in the Market Place; the pawnbroker in his corner.
            Person("Green", false, 557f, 776f, 90f, "Master of Item Creation");
            Person("Green", true, 572f, 756f, 180f, "Master of Enchanting");
            Person("Green", false, 581f, 756f, 180f, "Master of First Aid");
            Person("Orange", false, 563f, 756f, 180f, "The trader");
            Person("Orange", true, 595f, 755f, 180f, "The seer");
            Person("Orange", false, 692.5f, 806f, 0f, "The pawnbroker");

            // Townsfolk, yellow.
            foreach (var (px, py, yaw, female) in new[]
                     {
                         (575f, 777f, 0f, false), (590f, 778f, 200f, true), (660f, 778f, 90f, true), (676f, 777f, 270f, false),
                         (540f, 674f, 90f, true), (660f, 674f, 270f, false), (622f, 799f, 90f, true), (540f, 822f, 180f, false),
                         (560f, 820f, 0f, true), (643f, 790f, 30f, false), (500f, 735f, 90f, false), (580f, 735f, 270f, true),
                         (612f, 699f, 0f, false), (557f, 691f, 180f, true),
                     })
            {
                Person("Yellow", female, px, py, yaw, "Townsperson");
            }

            Person("Yellow", false, 152f, 646f, 180f, "Farmer");
        }

        private static void Person(string colour, bool female, float px, float py, float yaw, string name)
        {
            if (colour == "Yellow")
            {
                // Ordinary townsfolk are the owner's gentleman and woman, walking a closed route round their place (2026-11-06).
                Physics.SyncTransforms();
                var at = World(px, py, Ground(px, py));
                var route = SliceTownsfolk.PlanRoute(at);
                var prefab = female ? SliceTownsfolk.WomanPrefabPath : SliceTownsfolk.PrefabPath;
                var walker = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefab), _people);
                walker.name = female && name == "Townsperson" ? "Townswoman" : name;
                walker.transform.SetPositionAndRotation(route.Length > 0 ? route[0] : at, Quaternion.Euler(0f, yaw, 0f));
                var walk = walker.GetComponent<GameDev.Slice.People.TownsfolkWalker>();
                walk.Configure(route, walk.Speed, SliceTownsfolk.GroundMask);
                return;
            }

            var custom = colour == "Teal" || colour == "Brown";
            var path = $"{VeilmouthDressing.Dummies}/HumanDummy_{(female ? "F" : "M")} {(custom ? "White" : colour)}.prefab";
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), _people);
            go.name = name;
            go.transform.SetPositionAndRotation(World(px, py, Ground(px, py)), Quaternion.Euler(0f, yaw, 0f));
            VeilmouthDressing.Stand(go, 1.8f);
            SliceCharacterLook.Fix(go);
            if (custom)
            {
                var material = colour == "Teal"
                    ? SliceKitCatalogue.Colour("Dummy_Teal", new Color(0.1f, 0.58f, 0.58f), false)
                    : SliceKitCatalogue.Colour("Dummy_Brown", new Color(0.45f, 0.29f, 0.16f), false);
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    r.sharedMaterials = r.sharedMaterials.Select(_ => material).ToArray();
                }
            }
        }

        private static void Beast(string path, float length, float px, float py, float yaw, string name)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), _people);
            go.name = name;
            go.transform.SetPositionAndRotation(World(px, py, Ground(px, py)), Quaternion.Euler(0f, yaw, 0f));
            var size = VeilmouthDressing.Seen(go).size;
            go.transform.localScale = Vector3.one * (length / Mathf.Max(size.x, size.z));
            VeilmouthDressing.Stand(go, 0f);
            SliceCharacterLook.Fix(go);
        }

        private static void Territory(Transform parent, string name, float px, float py)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer(KitLayers.NoGrid);
            go.transform.SetParent(parent, false);
            go.transform.position = World(px, py, Ground(px, py));
            go.AddComponent<LayoutZone>().Configure(new Vector3(30f, 6f, 30f), new Color(0.6f, 0.35f, 0.15f, 0.9f));
        }

        /// <summary>The ritual's magic circle at a candidate, hidden: the ritual system shows the one the seed picks.</summary>
        private static void Circle(Vector3 centre, string candidate)
        {
            var circle = (GameObject)PrefabUtility.InstantiatePrefab(_kit["RitualCircle"].Prefab, _parent);
            circle.transform.SetPositionAndRotation(new Vector3(Mathf.Round(centre.x), centre.y, Mathf.Round(centre.z)), Quaternion.identity);
            circle.name = $"Ritual Circle - {candidate}";
            circle.SetActive(false);
        }

        private static void Report()
        {
            // The rooms behind the windows are dressing whose KitBlock VicRooms switched off: not blocks of the city.
            var blocks = Object.FindObjectsByType<KitBlock>(FindObjectsInactive.Exclude).Where(b => b.enabled).ToArray();
            var faults = KitLayoutCheck.Check(blocks);
            Debug.Log($"[VeilmouthCity] plots: the least room between a door's steps and its railings is {_stepsClear:0.00} m (§19 asks 1.5 m).");
            Debug.Log($"[VeilmouthCity] {blocks.Length} blocks, {_doors.Count} doors, {_lanes.Count} lanes, {faults.Count} kit faults" +
                      (faults.Count > 0 ? ":\n  " + string.Join("\n  ", faults.Take(60).Select(f => f.ToString())) : "."));
        }

        // ------------------------------------------------------------------------------------------- stone works

        /// <summary>Boxes and ribbons merged into one mesh per material, with a box collider for each solid box.</summary>
        private sealed class Works
        {
            private readonly Transform _owner;
            private readonly bool _solid;
            private readonly Dictionary<string, (List<Vector3> V, List<Vector3> N, List<Vector2> UV, List<int> I)> _parts =
                new Dictionary<string, (List<Vector3>, List<Vector3>, List<Vector2>, List<int>)>();

            public Works(Transform owner, bool solid)
            {
                _owner = owner;
                _solid = solid;
                SliceKitCatalogue.SetLayer(owner.gameObject, solid ? KitLayers.Structure : KitLayers.NoGrid);
            }

            private (List<Vector3> V, List<Vector3> N, List<Vector2> UV, List<int> I) Part(string material)
            {
                if (!_parts.TryGetValue(material, out var p))
                {
                    p = (new List<Vector3>(), new List<Vector3>(), new List<Vector2>(), new List<int>());
                    _parts[material] = p;
                }

                return p;
            }

            public void Box(Vector3 centre, Vector3 size, float yaw, bool collide = true, string material = null)
            {
                var p = Part(material ?? "");
                var rotation = Quaternion.Euler(0f, yaw, 0f);
                var h = size / 2f;
                var faces = new[]
                {
                    (Vector3.up, Vector3.right, Vector3.forward), (Vector3.down, Vector3.right, Vector3.back),
                    (Vector3.right, Vector3.forward, Vector3.up), (Vector3.left, Vector3.back, Vector3.up),
                    (Vector3.forward, Vector3.left, Vector3.up), (Vector3.back, Vector3.right, Vector3.up),
                };
                foreach (var (n, u, v) in faces)
                {
                    var c = Vector3.Scale(n, h);
                    var du = Vector3.Scale(u, h);
                    var dv = Vector3.Scale(v, h);
                    var su = Vector3.Scale(u, size).magnitude;
                    var sv = Vector3.Scale(v, size).magnitude;
                    var start = p.V.Count;
                    var corners = new[] { c - du - dv, c + du - dv, c + du + dv, c - du + dv };
                    var uvs = new[] { new Vector2(0f, 0f), new Vector2(su, 0f), new Vector2(su, sv), new Vector2(0f, sv) };
                    for (var k = 0; k < 4; k++)
                    {
                        var world = centre + (rotation * corners[k]);
                        p.V.Add(world);
                        p.N.Add(rotation * n);
                        p.UV.Add(uvs[k] / 2.5f);
                    }

                    p.I.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
                }

                if (!collide || !_solid)
                {
                    return;
                }

                var holder = _owner;
                if (Mathf.Abs(yaw) > 0.01f)
                {
                    holder = new GameObject("Turned").transform;
                    holder.SetParent(_owner, false);
                    holder.SetPositionAndRotation(centre, rotation);
                    holder.gameObject.layer = _owner.gameObject.layer;
                    centre = Vector3.zero;
                }

                var box = holder.gameObject.AddComponent<BoxCollider>();
                box.center = holder == _owner ? centre : Vector3.zero;
                box.size = size;
            }

            public void Finish(string defaultMaterial)
            {
                foreach (var pair in _parts)
                {
                    var name = pair.Key == "" ? defaultMaterial : pair.Key;
                    var (v, n, uv, i) = pair.Value;
                    var meshName = $"{_owner.name} - {name}";
                    var path = $"{SliceKitCatalogue.Folder}/Works/{meshName.Replace("&", "and")}.asset";
                    KitStandIns.EnsureFolder(SliceKitCatalogue.Folder + "/Works");
                    // The asset's own mesh is written in place: a copy into it leaves the old geometry on the GPU for the
                    // rest of the session, so renders made after a rebuild showed C1's walls (2026-11-06).
                    var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    var created = mesh == null;
                    if (created)
                    {
                        mesh = new Mesh();
                    }

                    mesh.Clear();
                    mesh.name = meshName;
                    mesh.indexFormat = IndexFormat.UInt32;
                    mesh.SetVertices(v);
                    mesh.SetNormals(n);
                    mesh.SetUVs(0, uv);
                    mesh.SetTriangles(i, 0);
                    mesh.RecalculateBounds();
                    mesh.RecalculateTangents();
                    if (created)
                    {
                        AssetDatabase.CreateAsset(mesh, path);
                    }
                    else
                    {
                        EditorUtility.SetDirty(mesh);
                    }

                    var go = new GameObject(name);
                    go.transform.SetParent(_owner, false);
                    go.layer = _owner.gameObject.layer;
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var material = name.StartsWith("VIC_")
                        ? $"{GameDev.VictorianKit.EditorTools.VicKitPaths.MaterialFolder}/{name}.mat"
                        : $"{VictorianMaterials}{name}.mat";
                    go.AddComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(material);
                    GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
                }

                AssetDatabase.SaveAssets();
            }
        }
    }
}

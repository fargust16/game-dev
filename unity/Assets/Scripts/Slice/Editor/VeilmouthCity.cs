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
    /// Builds Veilmouth once from the city plan C5 (VEILMOUTH_CITY_PLAN.md §29, by the rules of §2–§7, §19 and §25–§27),
    /// the owner's map of 2026-11-10 cut into blocks, on the ground <see cref="VeilmouthGround"/> shaped from C4, whose walls,
    /// river and sea C5 keeps. Everything goes under one root, <c>Veilmouth C5</c>, and from then on the owner moves, removes
    /// and adds by hand: it refuses to run twice.
    /// <para>
    /// The layout is the planner's (<see cref="VeilmouthLayout"/>); this builder places what it names by the kit's rules.
    /// It paints the streets into the ground (three appended layers after the canonical five, which keep their order),
    /// raises the pavements, the town wall with its gate and towers, the stone banks, the quay wall, the bridges and the
    /// piers as stone works, and puts up the owner's Victorian Kit at ×1.25 (<see cref="VeilmouthKitHouses"/>) round the
    /// blocks as the planner placed every house, the lamps by the furniture rules (§4) and the trees by §26–§27.
    /// </para>
    /// <para>
    /// Plan coordinates as in the plan: px east and py south from the area's north-west corner, so world X = px − 500 and
    /// Z = 500 − py. Every size here is an authoring default. Layout randomness is a fixed System.Random, never the world
    /// seed. <see cref="BuildReplacing"/> replaces the root and is for the agent's iterations in batch runs only.
    /// </para>
    /// </summary>
    public static partial class VeilmouthCity
    {
        public const string RootName = "Veilmouth C5";
        private static readonly string[] OldRootNames = { "Veilmouth C4", "Veilmouth C3", "Veilmouth C2", "Veilmouth C1" }; // C5 replaces their layouts (city plan §29)
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

            /// <summary>
            /// A carriageway that carts drive on, setts or a lane's cobbles, which a pavement never crosses (R4; §29.1 item 2:
            /// every street has pavements and its carriageway runs unbroken, a lane's too).
            /// </summary>
            public bool Road => !PaintOnly && Use == Use.Road && Carriage;

            /// <summary>A raised pavement: a street's pavement lane (R4).</summary>
            public bool Pavement => !PaintOnly && Use == Use.Walk && Layer == Flags;
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

        [MenuItem("GameDev/Slice/Build Veilmouth City from C5 (once)")]
        public static void Build() => Run(false);

        /// <summary>Batch only, for the agent's iterations: replaces this builder's own root.</summary>
        public static void BuildReplacing() => Run(true);

        public static void BuildReplacingAndRender()
        {
            Run(true);
            VeilmouthRenders.RenderAll();
        }

        /// <summary>Batch only, the agent's C5 steps (§29.3): the city built again on the present ground, then its views.</summary>
        public static void BuildC5AndRender()
        {
            Run(true);
            VeilmouthRenders.RenderC5();
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
        /// Batch only, the whole chain (C5, §29.4): the city built again on its present ground, which is the owner's to
        /// change by hand and is not shaped again; then the passes beside its root (the named people, the world driver,
        /// the dogs, the ornaments, the mystic layer), the street probes baked again, the POIs staged and the residue
        /// placed again, and C5's views. Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthCity.RebuildStreets
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
            Restage();
            VeilmouthRenders.RenderC5();
        }

        private static void Run(bool replace)
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var existing = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            if (existing != null)
            {
                if (!replace)
                {
                    Debug.LogWarning($"{VeilmouthScene.ScenePath} already holds the C5 city; the builder does not run twice.");
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
                Object.DestroyImmediate(old); // C0's dressing, C1's to C4's cities: C5 replaces their layout (city plan §29)
            }

            // The layout zones again, now that the cellar's door and the chapel's forecourt have their places.
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == VeilmouthScene.MapName || VeilmouthScene.OldMapNames.Contains(g.name)))
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
            _lampsDropped = 0;
            _postsAt.Clear();
            _postsAdded = _postsHidden = _railingsDrawnOut = 0;
            _pavements = null;
            _passages = 0;
            _openSpans = 0;

            _layout = VeilmouthLayout.Load();
            _decks.Clear();
            _sunk.Clear();
            (_terraceCuts, _terraceLift) = (0, 0f);
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
            WallsC4();
            _parent = Group("Bridges and Quays");
            Waterside();
            _parent = Group("Civic Buildings");
            Buildings();
            _parent = Group("Houses");
            Houses();
            foreach (var b in _backs)
            {
                PlaceBack(b.F, b.Front, b.A0, b.A1, b.Depth);
            }

            _backs.Clear();
            BackDoors();
            _parent = Group("Districts");
            Masses();
            Statue();
            CellarDoor();
            Cemetery();
            Market();
            Cargo();
            _parent = Group("Ritual Circles");
            foreach (var c in _layout.candidates)
            {
                Circle(World(c.x, c.y, Up(Ground(c.x, c.y))), c.name);
            }

            var lampGroup = Group("Lamps");
            _parent = lampGroup;
            LampsC4();
            _parent = Group("Street Trees");
            StreetTreesC4();
            _parent = lampGroup;
            SquareLamps();
            _parent = Group("Parks");
            Parks();
            _parent = Group("Yards");
            Yards();
            _parent = Group("Outskirts");
            Hamlet();
            Gloomwood();
            People();
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
                      $"Back doors: {_backDoorsOpen} open, {_backDoorsBlind} walled up. Railing posts: {_postsAdded} added at free ends, {_postsHidden} hidden at walls; {_railingsDrawnOut} railing ends drawn out to a wall (§31).");
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

        /// <summary>What stands at a plan point stands on: a deck's top, a raised pavement's top (R4), or the ground.</summary>
        private static float Ground(float px, float py)
        {
            foreach (var (quad, top) in _decks)
            {
                if (Inside(quad, new Vector2(px, py)))
                {
                    return top;
                }
            }

            return _pavements != null && _pavements.On(px, py) ? _pavements.Top(px, py) : BareGround(px, py);
        }

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
            var min = GroundUnder(quad).Min;

            // A house on a plain stoop is sunk, so only its top step shows (§28.1 item 5).
            var floor = Mathf.Min(OwnFloor(key, f, front, a) + raise, ceiling) - (_sinking ? Sink : 0f);
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
            if (_sinking)
            {
                SinkStoops(house);
            }

            return house;
        }

        /// <summary>
        /// A house's own floor on a footprint, before any raise or sink: the highest ground under it, or the pavement's
        /// top before its front if higher (R4: steps start from the pavement's top), rounded up to 0.1 m.
        /// </summary>
        private static float OwnFloor(string key, Facing f, float front, float a)
        {
            var e = _kit[key];
            var max = GroundUnder(Footprint(f, front, a, e.Width, e.Depth)).Max;
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

            return Up(max);
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
                    var world = Enumerable.Range(0, 8).Select(k => box.transform.TransformPoint(box.center + Vector3.Scale(box.size * 0.5f,
                        new Vector3((k & 1) == 0 ? -1f : 1f, (k & 2) == 0 ? -1f : 1f, (k & 4) == 0 ? -1f : 1f)))).ToArray();
                    var corners = world.Select(Plan).ToArray();
                    var rect = Rect(corners.Min(c => c.x), corners.Min(c => c.y), corners.Max(c => c.x), corners.Max(c => c.y));
                    // A sunk house's buried steps are no stoop: only a step that shows over the ground counts (§28.1 item 5).
                    var c0 = Centre(rect);
                    if (world.Max(w => w.y) < Ground(c0.x, c0.y) + 0.05f)
                    {
                        continue;
                    }

                    _stoops.Add(rect);
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

            var at = new int[keys.Count];
            var next = s0 + gap0;
            for (var i = 0; i < keys.Count; i++)
            {
                next += i == passageAt && left > 0 ? left : 0;
                at[i] = next;
                next += _kit[keys[i]].Width;
            }

            // §32: each run of houses wall to wall, the two sides of a middle passage apart, on one level.
            var lift = new float[keys.Count];
            foreach (var (from, to) in new[] { (0, passageAt), (passageAt, keys.Count) })
            {
                var part = Level(Enumerable.Range(from, to - from).Select(i => OwnFloor(keys[i], f, front, at[i])).ToList());
                part.CopyTo(lift, from);
            }

            for (var i = 0; i < keys.Count; i++)
            {
                if (i == passageAt && left > 0)
                {
                    Passage(f, front, at[i] - left, at[i], depth);
                }

                if (House(keys[i], f, front, at[i], raise + lift[i]) == null)
                {
                    Debug.LogWarning($"[VeilmouthCity] a terrace house did not fit at {at[i]} on {f} {front}.");
                }
            }

            if (gap1 > 0)
            {
                Passage(f, front, s1 - gap1, s1, depth);
            }
        }

        /// <summary>The least gap between two houses of a row, m (R5): a real passage to the yard behind, never a slot.</summary>
        private const int PassageWidth = 4;

        /// <summary>
        /// Authoring default, m (§32): the most a run's own floors may spread and stand on one level, eight steps of 0.15 m.
        /// </summary>
        private const float TerraceRise = 1.2f;

        private static int _terraceCuts;
        private static float _terraceLift;

        /// <summary>
        /// §32: one run of houses wall to wall, by their floors in order along it, set on one level, its highest floor, so the
        /// bands of their fronts meet flush at every party wall. A run whose floors spread more than <see cref="TerraceRise"/>
        /// is cut at party walls into the fewest such levels, from its start. Returns each house's lift.
        /// </summary>
        private static float[] Level(IReadOnlyList<float> floors)
        {
            var lift = new float[floors.Count];
            var i = 0;
            while (i < floors.Count)
            {
                var (lo, hi, j) = (floors[i], floors[i], i + 1);
                while (j < floors.Count && Mathf.Max(hi, floors[j]) - Mathf.Min(lo, floors[j]) <= TerraceRise + 0.001f)
                {
                    (lo, hi) = (Mathf.Min(lo, floors[j]), Mathf.Max(hi, floors[j]));
                    j++;
                }

                _terraceCuts += j < floors.Count ? 1 : 0;
                for (var k = i; k < j; k++)
                {
                    lift[k] = hi - floors[k];
                    _terraceLift = Mathf.Max(_terraceLift, lift[k]);
                }

                i = j;
            }

            return lift;
        }

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
        private static readonly string[] TownRow = VeilmouthKitHouses.Keys("Terrace8", "Terrace12", "ShopTown8");
        private static readonly string[] TenementRow = VeilmouthKitHouses.Keys("Tenement8", "Tenement12");
        private static readonly string[] LowTenementRow = VeilmouthKitHouses.Keys("TenementLow8", "TenementLow12");
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
        private static int _postsAdded, _postsHidden, _railingsDrawnOut;

        /// <summary>Authoring default, m (§31): a railing's end this near a wall along its line is drawn out to meet it.</summary>
        private const float RailingReach = 1.6f;

        /// <summary>
        /// The owner's fifth play test (2026-11-09) and walk (2026-11-10, §31): a railing's posts stand where a run ends
        /// free, at a gate, a gap or a corner; an end with a wall within <see cref="RailingReach"/> m along its line is drawn
        /// out to meet it (<see cref="DrawOut"/>), and its bare end abuts the wall with no post. The wall is read from the
        /// colliders, not the plots, which reach past a house's face. The kit's module carries one post, at one end: a free
        /// end with none gets a copy of its neighbour's post, unless a post of another run already stands there.
        /// </summary>
        private static void RailingEnds(List<GameObject> run, bool alongX, float line, int from, int to)
        {
            if (run.Count == 0 || Posts(run).Count == 0)
            {
                return;
            }

            Physics.SyncTransforms();

            // The post a free end copies, taken before any is hidden: a run of one module whose only post stood at its wall
            // end still gets a post at its free end (§31, the owner's walk).
            var template = Posts(run)[0];
            var templateAt = Plan(template.bounds.center);
            var templatePosition = template.transform.position;
            foreach (var (end, beyond, module) in new[] { (from, -1, run[0]), (to, 1, run[run.Count - 1]) })
            {
                var p = alongX ? new Vector2(end, line) : new Vector2(line, end);
                var dir = alongX ? new Vector3(beyond, 0f, 0f) : new Vector3(0f, 0f, -beyond);
                var gap = WallAhead(World(p.x, p.y, Ground(p.x, p.y) + 0.5f) - (dir * 0.05f), dir, RailingReach + 0.05f) - 0.05f;
                var posts = Posts(run);
                if (gap < RailingReach)
                {
                    if (gap > 0.02f)
                    {
                        DrawOut(module, dir, gap);
                        _railingsDrawnOut++;
                    }

                    var at = p + (new Vector2(dir.x, -dir.z) * Mathf.Max(gap, 0f));
                    foreach (var r in Posts(run).Where(r => (Plan(r.bounds.center) - at).magnitude < 0.45f))
                    {
                        r.gameObject.SetActive(false);
                        _postsHidden++;
                    }

                    // The bars stopped at the hidden post's inner face: drawn out again until they meet the wall.
                    var bars = module.GetComponentsInChildren<Renderer>(false).Where(r => !r.name.StartsWith("Fence column")).ToList();
                    if (bars.Count > 0)
                    {
                        Physics.SyncTransforms();
                        var front = bars.Max(r => Vector3.Dot(r.bounds.center, dir) + (Mathf.Abs(Vector3.Dot(r.bounds.size, dir)) / 2f));
                        var barsEnd = World(p.x, p.y, Ground(p.x, p.y) + 0.5f);
                        barsEnd += dir * (front - Vector3.Dot(barsEnd, dir));
                        var rest = WallAhead(barsEnd - (dir * 0.05f), dir, 0.6f) - 0.05f;
                        if (rest > 0.01f && rest < 0.55f)
                        {
                            DrawOut(module, dir, rest);
                        }
                    }
                }
                else if (!posts.Any(r => (Plan(r.bounds.center) - p).magnitude < 0.45f) && !_postsAt.Any(q => (q - p).sqrMagnitude < 0.35f * 0.35f))
                {
                    var nearest = posts.OrderBy(r => (Plan(r.bounds.center) - p).sqrMagnitude).FirstOrDefault() ?? template;
                    var at = nearest == template ? templateAt : Plan(nearest.bounds.center);
                    var from3 = nearest == template ? templatePosition : nearest.transform.position;
                    var copy = UnityEngine.Object.Instantiate(nearest.gameObject, nearest.transform.parent);
                    copy.name = nearest.name + " (end)";
                    copy.SetActive(true);
                    copy.transform.position = from3 + World(p.x, p.y, Ground(p.x, p.y) - Ground(at.x, at.y)) - World(at.x, at.y, 0f);
                    _postsAdded++;
                }
            }

            _postsAt.AddRange(Posts(run).Select(r => Plan(r.bounds.center)));
        }

        /// <summary>A run's posts that stand: the kit's "Fence column" renderers, active.</summary>
        private static List<Renderer> Posts(List<GameObject> run) =>
            run.SelectMany(g => g.GetComponentsInChildren<Renderer>(false)).Where(r => r.name.StartsWith("Fence column")).ToList();

        /// <summary>
        /// The distance to the first wall along a level ray: a collider at least 1.5 m high, upright, that is no street
        /// furniture and no tree; infinity when none within <paramref name="reach"/>.
        /// </summary>
        private static float WallAhead(Vector3 from, Vector3 dir, float reach)
        {
            var best = float.PositiveInfinity;
            foreach (var h in Physics.RaycastAll(from, dir, reach, ~0, QueryTriggerInteraction.Ignore))
            {
                var block = h.collider.GetComponentInParent<KitBlock>();
                if (Mathf.Abs(h.normal.y) > 0.3f || h.collider.bounds.size.y < 1.5f ||
                    (block != null && (block.name.StartsWith("Kit_Street_") || block.name.StartsWith("Kit_Tree_"))))
                {
                    continue;
                }

                best = Mathf.Min(best, h.distance);
            }

            return best;
        }

        /// <summary>
        /// A railing's end module drawn out by <paramref name="gap"/> m along <paramref name="dir"/> to meet a wall (§31):
        /// its model stretched along the run, its posts keeping their width, its other end where it was, the block on its
        /// cell. At most 1.6 m on a 2 m module, so its bars stand at most 1.8 times as far apart.
        /// </summary>
        private static void DrawOut(GameObject module, Vector3 dir, float gap)
        {
            var model = module.transform.Find("Model");
            var rs = model == null ? new Renderer[0] : model.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0)
            {
                return;
            }

            Bounds Seen()
            {
                var b = rs[0].bounds;
                foreach (var r in rs)
                {
                    b.Encapsulate(r.bounds);
                }

                return b;
            }

            float Back(Bounds b) => Vector3.Dot(b.center, dir) - (Mathf.Abs(Vector3.Dot(b.size, dir)) / 2f);
            var before = Seen();
            var length = Mathf.Abs(Vector3.Dot(before.size, dir));
            var k = (length + gap) / length;
            var scale = model.localScale;
            scale[Axis(model, dir)] *= k;
            model.localScale = scale;
            foreach (var post in model.GetComponentsInChildren<Renderer>(true).Where(r => r.name.StartsWith("Fence column")))
            {
                var s = post.transform.localScale;
                s[Axis(post.transform, dir)] /= k;
                post.transform.localScale = s;
                PrefabUtility.RecordPrefabInstancePropertyModifications(post.transform);
            }

            model.position += dir * (Back(before) - Back(Seen()));
            PrefabUtility.RecordPrefabInstancePropertyModifications(model);
        }

        /// <summary>The index of a transform's local axis that lies nearest a world direction.</summary>
        private static int Axis(Transform t, Vector3 dir)
        {
            float x = Mathf.Abs(Vector3.Dot(t.right, dir)), y = Mathf.Abs(Vector3.Dot(t.up, dir)), z = Mathf.Abs(Vector3.Dot(t.forward, dir));
            return x >= y && x >= z ? 0 : y >= z ? 1 : 2;
        }

        private static string[] Keys(string type, params string[] facades) => facades.Select(f => VictorianHouses.Key(type, f)).ToArray();

        // ------------------------------------------------------------------------------------------- streets

        private static void Lanes(string street, int layer, bool carriage, Vector2[] quad, Use use)
        {
            _lanes.Add(new Lane { Street = street, Quad = quad, Layer = layer, Carriage = carriage, Use = use });
            Take(quad, use);
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
            // Every carriageway last, so no pavement, square or yard painted after it breaks it (§29.1 item 2).
            foreach (var lane in _lanes.Where(l => !l.Road).Concat(_lanes.Where(l => l.Road)))
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

        /// <summary>A run of wall along X (or Y) in segments of 4 m that follow the ground, less its gaps.</summary>
        private static void WallRun(bool alongY, float t0, float t1, int a0, int a1, float height, params (int, int)[] gaps)
        {
            for (var a = a0; a < a1; a += 4)
            {
                var pieces = new List<(float S0, float S1)> { (a, Mathf.Min(a + 4, a1)) };
                foreach (var (g0, g1) in gaps)
                {
                    pieces = pieces.SelectMany(p => p.S1 <= g0 || p.S0 >= g1 ? new[] { p } : new[] { (S0: p.S0, S1: Mathf.Min(p.S1, g0)), (S0: Mathf.Max(p.S0, g1), S1: p.S1) })
                        .Where(p => p.S1 - p.S0 > 0.01f).ToList();
                }

                foreach (var (s0, s1) in pieces)
                {
                    Segment(s0, s1);
                }
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

        // ------------------------------------------------------------------------------------------- lamps

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
                    if (_doors.Any(d => Mathf.Abs((alongX ? d.At.y : d.At.x) - across) < 8.5f && Mathf.Abs((alongX ? d.At.x : d.At.y) - along) < 2.2f) ||
                        !LampRoom(c, null, 0, alongX, side != 0))
                    {
                        continue;
                    }

                    var placed = side == 0
                        ? alongX ? Prop("LampPost", Facing.N, along - 0.5f, line) : Prop("LampPost", Facing.N, line, along - 0.5f)
                        : OnCarriageway("LampPost", c, 1f);
                    if (placed != null)
                    {
                        _lamps.Add((c, null, 0));
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
                // The block on the kit's 0.1 m height step; what is seen stands on the carriageway itself. Left on the step, a
                // lamp's foot sank up to 0.1 m into the setts (§31, the owner's walk: lamps stuck in the paving).
                var ground = Ground(c.x, c.y);
                var step = Mathf.Floor((ground * 10f) + 0.02f) / 10f;
                go.transform.position = new Vector3(p.x, step, p.z);
                var seen = go.transform.Find("Model");
                if (seen != null && ground - step > 0.001f)
                {
                    seen.position += Vector3.up * (ground - step);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(seen);
                }
            }

            return go;
        }

        // ------------------------------------------------------------------------------------------- outside

        // ------------------------------------------------------------------------------------------- people

        private static void Person(string colour, bool female, float px, float py, float yaw, string name)
        {
            if (colour == "Yellow")
            {
                // Ordinary townsfolk walk a closed route round their place (2026-11-06), in a model by the nearest block's
                // wealth (owner's people, 2026-11-10).
                Physics.SyncTransforms();
                var at = World(px, py, Ground(px, py));
                var route = SliceTownsfolk.PlanRoute(at);
                var prefab = name == "Farmer" ? SliceTownsfolk.PrefabPath : SlicePeopleModels.WalkerLook(female, at, _layout).Walking;
                var walker = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefab), _people);
                walker.name = female && name == "Townsperson" ? "Townswoman" : name;
                walker.transform.SetPositionAndRotation(route.Length > 0 ? route[0] : at, Quaternion.Euler(0f, yaw, 0f));
                var walk = walker.GetComponent<GameDev.Slice.People.TownsfolkWalker>();
                walk.Configure(route, walk.Speed, SliceTownsfolk.GroundMask);
                return;
            }

            SlicePeopleModels.Instantiate(name, female, colour, _people, World(px, py, Ground(px, py)), yaw);
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
            Debug.Log(_stepsClear < float.MaxValue
                ? $"[VeilmouthCity] plots: the least room between a door's steps and its railings is {_stepsClear:0.00} m (§19 asks 1.5 m)."
                : "[VeilmouthCity] plots: none, every house stands in a block (§29).");
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
                    // §31 (owner's walk, 2026-11-10): the texture laid in the world's own frame, so boxes side by side read as
                    // one length of stone, with no joint where one ends.
                    var wu = rotation * u;
                    var wv = rotation * v;
                    for (var k = 0; k < 4; k++)
                    {
                        var world = centre + (rotation * corners[k]);
                        p.V.Add(world);
                        p.N.Add(rotation * n);
                        p.UV.Add(new Vector2(Vector3.Dot(world, wu), Vector3.Dot(world, wv)) / 2.5f);
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

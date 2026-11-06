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
    /// Builds Veilmouth once from the city plan C1 (VEILMOUTH_CITY_PLAN.md §2–§7, §11 step 3), accepted by the owner on
    /// 2026-11-05, on the ground <see cref="VeilmouthGround"/> shaped from it. Everything goes under one root,
    /// <c>Veilmouth C1</c>, and from then on the owner moves, removes and adds by hand: it refuses to run twice.
    /// <para>
    /// It paints the streets into the ground (five appended layers after the canonical five, which keep their order),
    /// lays kerbs; raises the town wall with its gates and towers, the Ridge's retaining wall, the Soot Stair's blind
    /// walls, the embankments, the quays and the Veil Bridge as stone works; puts up the houses baked from the Victorian
    /// modules (<see cref="VictorianHouses"/>) in rows along every street, on plinths and steps where the ground falls,
    /// with Tanners Slant at 45° under a <see cref="KitFrame"/> (§12); the palace, the Town Hall and the Assize; the seven
    /// buildings that open, each with its door; the lamps by the furniture rules (§4); the market, the yards, the squares,
    /// the garden and the avenue's trees; and outside the walls the fields, the forest with its glades, beasts and herbs,
    /// and the ore hill.
    /// </para>
    /// <para>
    /// Plan coordinates as in the plan: px east and py south from the area's north-west corner, so world X = px − 500 and
    /// Z = 500 − py. Every size here is an authoring default. Layout randomness is a fixed System.Random, never the world
    /// seed. <see cref="BuildReplacing"/> replaces the root and is for the agent's iterations in batch runs only.
    /// </para>
    /// </summary>
    public static class VeilmouthCity
    {
        public const string RootName = "Veilmouth C1";
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
        }

        private static Dictionary<string, SliceKitCatalogue.Entry> _kit;
        private static Terrain _terrain;
        private static Transform _root, _parent, _people;
        private static List<(Vector2[] Quad, Use Use)> _taken;
        private static List<Lane> _lanes;
        private static List<(Vector2 At, Vector2 Out)> _doors;   // plan point and outward direction
        private static List<(bool AlongX, float Front, float At)> _partyWalls;
        private static Works _works;
        private static Works _kerbs;
        private static List<Vector2[]> _houses;  // every house's plan footprint, in the order built
        private static HashSet<(bool AlongX, float Line, int A0, int A1)> _fenced; // railing lines already set
        private static Transform _gardens;
        private static float _stepsClear; // the least room left between a plot's front steps and its railings, m

        /// <summary>
        /// The Terraces' plots (§3.3, §19): each row's house front line, the side its doors face, and its plots along the
        /// street, each holding one detached house in its middle with a front garden of 4 m and 2 m behind it.
        /// </summary>
        private static readonly (Facing F, float Front, (int S0, int S1)[] Spans)[] TerracePlots =
        {
            (Facing.E, 300f, new[] { (607, 629), (629, 651) }), (Facing.W, 320f, new[] { (607, 629) }),
            (Facing.E, 410f, new[] { (607, 629), (629, 651) }), (Facing.W, 430f, new[] { (607, 629), (629, 651) }),
        };

        private const int TerraceGarden = 4, TerraceBehind = 2;

        /// <summary>The Ridge's villa plots on Ridge Street's north side (§3.4, §19); the faction's house has its own.</summary>
        private static readonly (int S0, int S1)[] VillaPlots = { (330, 359), (359, 388), (388, 417), (417, 446), (556, 581), (629, 663), (663, 697) };

        private const float VillaFront = 574f;
        private const int VillaGarden = 5, VillaBehind = 3;
        private const int FactionPlot0 = 581, FactionPlot1 = 617, FactionAt = 588;

        /// <summary>The small green beside the arch house to Wardley Garden (§19), plan metres.</summary>
        private static readonly (float X0, float Y0, float X1, float Y1) ArchGreen = (316f, 641f, 332f, 651f);

        [MenuItem("GameDev/Slice/Build Veilmouth City from C1 (once)")]
        public static void Build() => Run(false);

        /// <summary>Batch only, for the agent's iterations: replaces this builder's own root.</summary>
        public static void BuildReplacing() => Run(true);

        public static void BuildReplacingAndRender()
        {
            Run(true);
            VeilmouthRenders.RenderAll();
        }

        private static void Run(bool replace)
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var existing = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            if (existing != null)
            {
                if (!replace)
                {
                    Debug.LogWarning($"{VeilmouthScene.ScenePath} already holds the C1 city; the builder does not run twice.");
                    return;
                }
            }

            _kit = SliceKitCatalogue.BuildAll();
            foreach (var pair in VictorianHouses.BakeAll())
            {
                _kit[pair.Key] = pair.Value;
            }

            // Opened again: baking builds and drops its assemblies in the open scene.
            scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == RootName || g.name == VeilmouthDressing.RootName))
            {
                Object.DestroyImmediate(old); // C0's dressing pass: C1 replaces its layout (city plan, status)
            }

            // The layout zones again, now that the cellar's door and the chapel's forecourt have their places.
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == VeilmouthScene.MapName))
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
            _fenced = new HashSet<(bool, float, int, int)>();
            _stepsClear = float.MaxValue;

            _root = new GameObject(RootName).transform;
            _people = Group("People");
            _gardens = Group("Gardens");
            VeilmouthDressing.Wind(_root);
            _works = new Works(Group("Stone Works"), true);
            _kerbs = new Works(Group("Kerbs"), false);

            Streets();
            PaintStreets();
            Kerbs();

            _parent = Group("Walls and Gates");
            Walls();
            _parent = Group("Government Quarter");
            Government();
            _parent = Group("Openable Buildings");
            Openable();
            Slant();
            _parent = Group("Houses");
            Houses();
            _parent = _gardens;
            Gardens();
            _parent = Group("Squares and Yards");
            Market();
            Squares();
            _parent = Group("Lamps");
            AllLamps();
            _parent = Group("Outside the Walls");
            Fields();
            Forest();
            Hill();
            Townsfolk();
            // A street door on every dwelling (15, *The slice's Dwellings*), after every building stands.
            VeilmouthDwellings.AddDoors(_root);

            _works.Finish("Stone wall");
            _kerbs.Finish("Grey");
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

        private static float Ground(float px, float py) =>
            _terrain.SampleHeight(World(px, py)) + _terrain.transform.position.y;

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
        /// (px0, py0) is the footprint's north-west corner.
        /// </summary>
        private static GameObject Prop(string key, Facing f, float px0, float py0, string name = null, bool force = false)
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
            var plant = key.StartsWith("Tree_") || key.StartsWith("Bush_") || key == "Shrub" || key == "Grass" || key == "Poppy";
            var go = PutPlan(key, f, px0, py0, plant ? min - 0.2f : Up(Mathf.Max(min, max - 0.15f)));
            if (name != null)
            {
                go.name = name;
            }

            if (!plant && !Upright.Contains(key))
            {
                Settle(go, quad, e.Depth == 0, turned);
            }

            return go;
        }

        /// <summary>What stands plumb whatever the ground: posts, a column, a board on its legs, a grave, a stand.</summary>
        private static readonly HashSet<string> Upright = new HashSet<string>
        {
            "LampPost", "Column", "NoticeBoard", "Grave", "GraveWall", "ArmourStand", "Candles",
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
                    Doorway(child.position, Outward(f), floor, e.Stoop);
                }
            }

            return house;
        }

        /// <summary>
        /// Records a door, and builds steps from it down to the ground outside when it stands above it. The steps
        /// continue a house door's own stoop from its foot, <paramref name="stoop"/> out, in the pack's Porch stone and at
        /// the stoop's width, tread and rise, so the two read as one stair; under the stoop the same stone goes down to
        /// the ground, so it never stands over a hollow on a slope (owner's play tests of 2026-11-06: steps in two styles
        /// with a gap between).
        /// </summary>
        private static void Doorway(Vector3 world, Vector2 outward, float floor, float stoop = 0f)
        {
            var at = Plan(world);
            _doors.Add((at, outward));
            var side = new Vector2(-outward.y, outward.x);
            var width = VictorianHouses.StoopWidth;
            Vector3 Size(float deep, float high) => new Vector3(Mathf.Abs(side.x) * width + Mathf.Abs(outward.x) * deep, high,
                Mathf.Abs(side.y) * width + Mathf.Abs(outward.y) * deep);

            var p = at + (outward * stoop);
            if (stoop > 0f)
            {
                var under = at + (outward * (stoop / 2f));
                var low = Mathf.Min(Ground(at.x, at.y), Ground(p.x, p.y)) - 0.3f;
                if (floor - low > 0.32f)
                {
                    _works.Box(World(under.x, under.y, (floor + low) / 2f), Size(stoop, floor - low), 0f, false, VictorianHouses.StoopMaterial);
                }
            }

            var outside = p + (outward * 1.2f);
            var rise = floor - Ground(outside.x, outside.y);
            if (rise <= VictorianHouses.StoopRise * 0.5f)
            {
                return;
            }

            var n = Mathf.CeilToInt(rise / VictorianHouses.StoopRise);
            var step = rise / n;
            var tread = Mathf.Max(VictorianHouses.StoopTread, 0.28f); // the kit standard's least run (§3), 5 mm over the stoop's
            // Below a stoop the first tread is at its foot, the floor's level; below a shop's door, one riser down.
            var first = stoop > 0f ? 0 : 1;
            for (var k = first; k < n; k++)
            {
                var top = floor - (k * step);
                var c = p + (outward * ((k - first + 0.5f) * tread));
                var bottom = Mathf.Min(Ground(c.x, c.y), top) - 0.3f;
                _works.Box(World(c.x, c.y, (top + bottom) / 2f), Size(tread, top - bottom), 0f, true, VictorianHouses.StoopMaterial);
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
        /// a house (less than the narrowest) closed by a yard wall in the middle (owner's play test, 2026-11-06: no
        /// empty corners of varying width).
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

        /// <summary>One span of a terrace, filled as <see cref="Row"/> describes.</summary>
        private static void Terrace(Facing f, float front, int s0, int s1, string[] palette, Random rng, float raise)
        {
            var keys = Fill(s1 - s0, palette, rng);
            if (keys.Count == 0)
            {
                return;
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
                return alike.Length > 0 ? VictorianHouses.CornerKey(alike[rng.Next(alike.Length)], side) : key;
            }

            keys[0] = Corner(keys[0], startSide);
            if (keys.Count > 1)
            {
                keys[keys.Count - 1] = Corner(keys[keys.Count - 1], endSide);
            }

            var left = s1 - s0 - keys.Sum(k => _kit[k].Width);
            var wallAt = keys.Count / 2;
            var a = (float)s0;
            for (var i = 0; i < keys.Count; i++)
            {
                if (i == wallAt && left > 0)
                {
                    YardWall(f, front, a, a + left);
                    a += left;
                }

                if (House(keys[i], f, front, a, raise) == null)
                {
                    Debug.LogWarning($"[VeilmouthCity] a terrace house did not fit at {a} on {f} {front}.");
                }

                a += _kit[keys[i]].Width;
            }
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

        /// <summary>A yard wall between two houses of a terrace where no house fits: brick, 3.5 m, on the front line.</summary>
        private static void YardWall(Facing f, float front, float a0, float a1)
        {
            var along = f == Facing.N || f == Facing.S;
            var outward = Outward(f);
            var mid = (a0 + a1) / 2f;
            var c = (along ? new Vector2(mid, front) : new Vector2(front, mid)) - (outward * 0.3f);
            var ground = Ground(c.x, c.y);
            var size = along ? new Vector3(a1 - a0, 4f, 0.6f) : new Vector3(0.6f, 4f, a1 - a0);
            _works.Box(World(c.x, c.y, ground + 1.5f), size, 0f);
            var lo = c - new Vector2(size.x, size.z) / 2f;
            Take(Rect(lo.x, lo.y, lo.x + size.x, lo.y + size.z), Use.Solid);
        }

        private static readonly string[] TerraceKeys = Keys("Terrace4", "brick", "stucco", "brick2", "grey")
            .Select(k => VictorianHouses.CornerKey(k, VictorianHouses.Side.Both)).ToArray();

        private static readonly string[] VillaKeys = Keys("Villa6", "stucco", "grey");

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
            var foot = (alongX ? new Vector2(doorAlong, front) : new Vector2(front, doorAlong)) + (o * (VictorianHouses.Stoop + 1.2f));
            var tread = Mathf.Max(VictorianHouses.StoopTread, 0.28f);
            var treads = Mathf.Max(0, Mathf.FloorToInt((garden - 1.5f - VictorianHouses.Stoop) / tread));
            var ceiling = Ground(foot.x, foot.y) + (treads * VictorianHouses.StoopRise) - 0.05f; // a house's height is set to 0.1 m
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
                var outside = doorAt + (o * (VictorianHouses.Stoop + 1.2f));
                var rise = floor - Ground(outside.x, outside.y);
                var steps = rise <= VictorianHouses.StoopRise * 0.5f ? 0 : Mathf.CeilToInt(rise / VictorianHouses.StoopRise);
                var reach = VictorianHouses.Stoop + (steps * Mathf.Max(VictorianHouses.StoopTread, 0.28f));
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

            void Run(int from, int to)
            {
                for (var a = from; a < to;)
                {
                    var length = to - a >= 2 ? 2 : 1;
                    var key = length == 2 ? "Railing" : "RailingShort";
                    if (alongX)
                    {
                        Prop(key, Facing.N, a, line - 0.1f, force: true);
                    }
                    else
                    {
                        Prop(key, Facing.E, line - 0.1f, a, force: true);
                    }

                    a += length;
                }
            }

            if (gate is float g)
            {
                var g0 = Mathf.Clamp(Mathf.RoundToInt(g - 1.5f), a0, a1);
                Run(a0, g0);
                Run(Mathf.Min(a1, g0 + 3), a1);
            }
            else
            {
                Run(a0, a1);
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
                Plant("Tree_Fruit", s0 + 1f, depth - 2f);
            }

            if (right >= 3f)
            {
                Plant("Tree_Fruit", s1 - 2f, depth - 2f);
            }

            for (var v = 1f; v < depth - 3f; v += 2f)
            {
                Plant(rng.Next(4) == 0 ? "Poppy" : "Shrub", s0 + 0.2f, v);
                Plant(rng.Next(4) == 0 ? "Poppy" : "Shrub", s1 - 1.2f, v);
            }

            for (var u = s0 + 1.5f; u < s1 - 1.5f; u += 2f)
            {
                if (Mathf.Abs(u + 0.5f - gate) >= 2.5f)
                {
                    Plant(rng.Next(2) == 0 ? "Poppy" : "Shrub", u, 0.2f);
                }
            }

            if (Mathf.Max(left, right) >= 3f)
            {
                var toward = left >= right;
                var u = toward ? s0 + (left / 2f) - 0.5f : s1 - (right / 2f) - 0.5f;
                var v = garden + (houseDepth / 2f) - 1.5f;
                var p = Corner(u, u + 1f, v, v + 3f);
                var facing = alongX ? (toward ? Facing.E : Facing.W) : (toward ? Facing.S : Facing.N);
                Prop("Bench", facing, p.x, p.y);
            }
        }

        /// <summary>
        /// Railings on a line along a front area, in segments of 2 m and a 1 m one for an odd metre at the end. A gate
        /// is left only before a door that opens toward the line, and a segment is set whatever prop was put there
        /// before, though never through a house (owner's play test, 2026-11-06: gaps stood where no way in or out was
        /// meant).
        /// </summary>
        private static void Railings(bool alongX, float line, int a0, int a1)
        {
            var segments = new List<(int A, int Length)>();
            for (var a = a0; a + 2 <= a1; a += 2)
            {
                segments.Add((a, 2));
            }

            if ((a1 - a0) % 2 == 1)
            {
                segments.Add((a1 - 1, 1));
            }

            foreach (var (a, length) in segments)
            {
                var key = length == 2 ? "Railing" : "RailingShort";
                var mid = a + (length / 2f);
                if (_doors.Any(d =>
                    {
                        var across = (alongX ? d.At.y : d.At.x) - line;
                        var facing = alongX ? d.Out.y : d.Out.x;
                        return Mathf.Abs(across) < 5f && across * facing < 0f && Mathf.Abs(facing) > 0.5f &&
                               Mathf.Abs((alongX ? d.At.x : d.At.y) - mid) < 1.6f;
                    }))
                {
                    continue;
                }

                if (alongX)
                {
                    Prop(key, Facing.N, a, line - 0.1f, force: true);
                }
                else
                {
                    Prop(key, Facing.E, line - 0.1f, a, force: true);
                }
            }
        }

        private static string[] Keys(string type, params string[] facades) => facades.Select(f => VictorianHouses.Key(type, f)).ToArray();

        // ------------------------------------------------------------------------------------------- streets

        private static void Lanes(string street, int layer, bool carriage, Vector2[] quad, Use use)
        {
            _lanes.Add(new Lane { Street = street, Quad = quad, Layer = layer, Carriage = carriage });
            Take(quad, use);
        }

        /// <summary>A street running along X: pavements of <paramref name="walk"/> m on both sides and a carriageway between.</summary>
        private static void StreetX(string name, int px0, int px1, int py0, int py1, int walk, int surface)
        {
            if (walk > 0)
            {
                Lanes(name, Flags, false, Rect(px0, py0, px1, py0 + walk), Use.Walk);
                Lanes(name, Flags, false, Rect(px0, py1 - walk, px1, py1), Use.Walk);
            }

            Lanes(name, surface, walk > 0, Rect(px0, py0 + walk, px1, py1 - walk), Use.Road);
        }

        private static void StreetY(string name, int px0, int px1, int py0, int py1, int walk, int surface)
        {
            if (walk > 0)
            {
                Lanes(name, Flags, false, Rect(px0, py0, px0 + walk, py1), Use.Walk);
                Lanes(name, Flags, false, Rect(px1 - walk, py0, px1, py1), Use.Walk);
            }

            Lanes(name, surface, walk > 0, Rect(px0 + walk, py0, px1 - walk, py1), Use.Road);
        }

        private static readonly Vector2 SlantStart = new Vector2(325f, 680f);
        private static readonly Vector2 SlantEnd = new Vector2(370f, 725f);

        private static void Streets()
        {
            // The low town's yards and courts are paved with cobbles; the streets are painted over them.
            _lanes.Add(new Lane { Street = "Yards", Quad = Rect(280, 600, 780, 830), Layer = Cobbles, PaintOnly = true });
            _lanes.Add(new Lane { Street = "Soot Stair", Quad = Rect(700, 592, 708, 650), Layer = Cobbles, PaintOnly = true });
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

            _lanes.Add(new Lane { Street = "Gardens", Quad = Rect(ArchGreen.X0, ArchGreen.Y0, ArchGreen.X1, ArchGreen.Y1), Layer = Grass, PaintOnly = true });

            // §2 sections. Order matters for paint: a later lane covers an earlier one where they cross.
            StreetX("Ridge Street", 280, 708, 580, 592, 4, Setts);
            StreetY("North Road", 618, 628, 490, 580, 0, Setts);
            StreetX("High Street", 280, 475, 662, 680, 5, Asphalt);
            StreetX("High Street East", 525, 780, 662, 680, 5, Asphalt); // the Circus between them, round its column
            StreetY("Terrace Street West", 304, 316, 606, 662, 4, Setts);
            StreetY("Terrace Street East", 414, 426, 606, 662, 4, Setts);
            StreetX("Back Lane", 280, 414, 600, 606, 0, Cobbles);
            StreetX("Mercers Row", 280, 370, 730, 740, 3, Setts);
            StreetX("Bell Street", 430, 724, 730, 740, 3, Setts);
            StreetY("Cheap Street", 454, 466, 680, 812, 3, Setts);
            StreetY("Dock Road", 724, 736, 680, 812, 3, Setts);
            StreetX("Loom Lane", 560, 724, 778, 784, 0, Cobbles);
            StreetX("Ridge Lane", 545, 698, 620, 626, 0, Cobbles);
            Lanes("Sea Quay", Setts, false, Rect(280, 812, 740, 830), Use.Walk);
            Lanes("Basin Quay", Setts, false, Rect(736, 780, 780, 790), Use.Walk);
            StreetY("Court Alley", 620, 623, 680, 730, 0, Cobbles);
            StreetY("Loom Alley", 670, 673, 740, 778, 0, Cobbles);
            StreetY("Ridge Alley", 600, 603, 626, 662, 0, Cobbles);
            StreetY("Chapel Lane", 306, 314, 548, 580, 0, Cobbles);
            StreetX("Garden Passage", 328, 340, 630, 640, 0, Cobbles);

            // Crown Avenue (§3.1): pavements, two carriageways and the mall between them, from the Circus to the forecourt.
            Lanes("Crown Avenue", Flags, false, Rect(482, 560, 486, 646), Use.Walk);
            Lanes("Crown Avenue", Flags, false, Rect(514, 560, 518, 646), Use.Walk);
            Lanes("Crown Avenue", Asphalt, true, Rect(486, 560, 493, 646), Use.Road);
            Lanes("Crown Avenue", Asphalt, true, Rect(507, 560, 514, 646), Use.Road);
            Lanes("Crown Avenue", Square, false, Rect(493, 560, 507, 646), Use.Walk);

            // Squares and yards: no building stands on them; props may.
            Lanes("Crown Circus", Square, false, Rect(475, 646, 525, 696), Use.Walk);
            Lanes("Palace Forecourt", Square, false, Rect(460, 530, 540, 560), Use.Walk);
            Lanes("Market Square", Square, false, Rect(370, 720, 430, 765), Use.Walk);
            Lanes("Ritual Square", Square, false, Rect(642, 740, 667, 778), Use.Walk);
            Lanes("Chapel Forecourt", Square, false, Rect(300, 540, 322, 556), Use.Walk);
            Lanes("Wardley Garden", Grass, false, Rect(340, 606, 390, 644), Use.Walk);
            Lanes("Cellar Yard", Cobbles, false, Rect(709, 626, 721, 662), Use.Walk);
            Lanes("Soot & Bell Yard", Cobbles, false, Rect(696, 703, 716, 715), Use.Walk);
            Lanes("Soot & Bell Gate", Cobbles, false, Rect(711, 715, 716, 730), Use.Walk);
            Lanes("Pawn Corner", Cobbles, false, Rect(700, 768, 706, 778), Use.Walk);

            // Tanners Slant, at 45° from the High Street to the Market Square (§12): setts, 8 m, no pavements, dark.
            Lanes("Tanners Slant", Setts, false, Along(SlantStart, SlantEnd, 4f, 6f), Use.Road);

            // What is not ground to build on: the river, the basin, the Soot Stair, the town wall's line, the bridge.
            Take(Rect(778, 490, 830, 830), Use.Solid);
            Take(Rect(740, 790, 860, 830), Use.Solid);
            Take(Rect(698, 592, 710, 662), Use.Solid);
            Take(Rect(270, 480, 280, 840), Use.Solid);
            Take(Rect(270, 480, 790, 490), Use.Solid);
            Take(Rect(280, 599, 780, 601), Use.Solid); // the retaining wall
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
                            maps[z, x, l] = l == lane.Layer ? 1f : 0f;
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

        /// <summary>Stone kerbs, 0.15 m, along every pavement's edge, broken where another street crosses.</summary>
        private static void Kerbs()
        {
            foreach (var lane in _lanes.Where(l => l.Carriage))
            {
                var lo = new Vector2(lane.Quad.Min(p => p.x), lane.Quad.Min(p => p.y));
                var hi = new Vector2(lane.Quad.Max(p => p.x), lane.Quad.Max(p => p.y));
                var alongX = hi.x - lo.x > hi.y - lo.y;
                foreach (var line in alongX ? new[] { lo.y, hi.y } : new[] { lo.x, hi.x })
                {
                    var inward = line == (alongX ? lo.y : lo.x) ? -1f : 1f; // the kerb stands on the pavement's side
                    var cuts = _lanes.Where(o => o.Street != lane.Street && !o.PaintOnly).Select(o => (
                            Lo: new Vector2(o.Quad.Min(p => p.x), o.Quad.Min(p => p.y)),
                            Hi: new Vector2(o.Quad.Max(p => p.x), o.Quad.Max(p => p.y))))
                        .Where(o => alongX ? o.Lo.y < line + 4.5f && o.Hi.y > line - 4.5f : o.Lo.x < line + 4.5f && o.Hi.x > line - 4.5f)
                        .Select(o => alongX ? (o.Lo.x, o.Hi.x) : (o.Lo.y, o.Hi.y)).OrderBy(c => c.Item1).ToList();
                    var start = alongX ? lo.x : lo.y;
                    var end = alongX ? hi.x : hi.y;
                    var a = start;
                    foreach (var (c0, c1) in cuts)
                    {
                        if (c1 <= a || c0 >= end)
                        {
                            continue;
                        }

                        if (c0 > a)
                        {
                            Kerb(alongX, line + (inward * 0.15f), a, c0);
                        }

                        a = Mathf.Max(a, c1);
                    }

                    if (a < end)
                    {
                        Kerb(alongX, line + (inward * 0.15f), a, end);
                    }
                }
            }
        }

        private static void Kerb(bool alongX, float line, float a0, float a1)
        {
            if (a1 - a0 < 0.5f)
            {
                return;
            }

            for (var a = a0; a < a1 - 0.01f; a += 1f)
            {
                var b = Mathf.Min(a + 1f, a1);
                var p0 = alongX ? new Vector2(a, line) : new Vector2(line, a);
                var p1 = alongX ? new Vector2(b, line) : new Vector2(line, b);
                _kerbs.Ribbon(p0, p1, 0.3f, 0.15f);
            }
        }

        // ------------------------------------------------------------------------------------------- walls

        private static void Walls()
        {
            // The old town wall (§3.8): 2 m thick, 6 m over the ground, along the west and north, with the West Gate,
            // the North Gate and the chapel's postern; a tower either side of each gate.
            // A run stops at its tower's face and the lintel spans between the towers at the wall's own thickness and
            // top: no two faces lie in one plane (the owner saw the gate flicker, 2026-11-06).
            WallRun(true, 278f, 280f, 490, 660, 6f);
            WallRun(true, 278f, 280f, 684, 830, 6f);
            WallRun(false, 488f, 490f, 278, 611, 6f, (330, 333));
            WallRun(false, 488f, 490f, 635, 780, 6f);
            Lintel(true, 278f, 280f, 665f, 679f, 6f);
            Lintel(false, 488f, 490f, 616f, 630f, 6f);
            House(VictorianHouses.Key("Tower2", "stone"), Facing.W, 275f, 660, name: "West Gate - north tower", force: true);
            House(VictorianHouses.Key("Tower2", "stone"), Facing.W, 275f, 679, name: "West Gate - south tower", force: true);
            House(VictorianHouses.Key("Tower2", "stone"), Facing.N, 485f, 611, name: "North Gate - west tower", force: true);
            House(VictorianHouses.Key("Tower2", "stone"), Facing.N, 485f, 630, name: "North Gate - east tower", force: true);

            // The Ridge's retaining wall, with a parapet of 1 m, wherever the two levels differ.
            // It stops where a street crosses its line (the owner found Crown Avenue walled off, 2026-11-06).
            for (var px = 280; px < 780; px += 2)
            {
                var cell = Rect(px, 599.3f, px + 2, 600.7f);
                if ((px >= 698 && px < 710) || _lanes.Any(l => !l.PaintOnly && l.Quad.Min(q => q.y) < 599f && l.Quad.Max(q => q.y) > 601f && Overlap(l.Quad, cell)))
                {
                    continue;
                }

                var top = Mathf.Max(Ground(px + 1f, 598.5f), Ground(px + 1f, 597f));
                var bottom = Mathf.Min(Ground(px + 1f, 601.5f), Ground(px + 1f, 603f));
                if (top - bottom < 0.3f)
                {
                    continue;
                }

                Slab(px, px + 2, 599.3f, 600.7f, bottom - 0.5f, top + 1f);
            }

            // The Soot Stair's blind walls, either side of its causeway, at least 3.2 m over the low ground.
            for (var py = 600; py < 650; py += 2)
            {
                var causeway = Mathf.Max(Ground(704f, py), Ground(704f, py + 2f));
                var top = Mathf.Max(causeway + 1f, Low + 3.2f);
                Slab(698.6f, 700f, py, py + 2, Low - 0.5f, top);
                Slab(708f, 709f, py, py + 2, Low - 0.5f, top);
            }

            Slab(698.6f, 709f, 650f, 650.6f, Low - 0.5f, Low + 0.3f); // the stair's last step

            // The cellar door in the stair's east wall, opening on its yard (§5.3).
            _works.Box(World(709.05f, 634.5f, Low + 1.2f), new Vector3(0.1f, 2.4f, 1.6f), 0f, false, "Dark grey");
            var leaf = (GameObject)PrefabUtility.InstantiatePrefab(_kit["PortalLeaf"].Prefab, _parent);
            leaf.transform.SetPositionAndRotation(World(709f, 633f, Up(Low)), Quaternion.Euler(0f, 90f, 0f));
            Configure(leaf, "soot-stair-cellar.street", VeilmouthDressing.CellarScenePath, "soot-stair-cellar.inside");
            _doors.Add((new Vector2(709f, 634.5f), new Vector2(1f, 0f)));

            // The river's embankment along the city, with a parapet; the basin's walls; the sea quay's edge.
            for (var py = 490; py < 790; py += 4)
            {
                if (py + 4 > 664 && py < 680)
                {
                    continue;
                }

                Slab(779f, 780.5f, py, py + 4, VeilmouthGround.RiverBed - 0.5f, Ground(778f, py + 2f) + 1f);
            }

            for (var py = 790; py < 830; py += 4)
            {
                Slab(739f, 740.5f, py, py + 4, VeilmouthGround.SeaBed - 2f, Ground(738f, py + 2f) + 0.3f);
            }

            for (var px = 740; px < 780; px += 4)
            {
                Slab(px, px + 4, 789f, 790.5f, VeilmouthGround.SeaBed - 1f, Ground(px + 2f, 787f) + 0.3f);
            }

            for (var px = 280; px < 740; px += 4)
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

        /// <summary>The Veil Bridge (§3.8): a stone deck at street level over the river on piers, with parapets, to the east bank.</summary>
        private static void Bridge()
        {
            const float deck = Low + 0.1f;
            Slab(780f, 852f, 664f, 680f, deck - 1.2f, deck, "Paving stones");
            Slab(780f, 852f, 663.4f, 664f, deck - 1.2f, deck + 1.1f);
            Slab(780f, 852f, 680f, 680.6f, deck - 1.2f, deck + 1.1f);
            foreach (var px in new[] { 792f, 806f, 820f, 834f })
            {
                Slab(px - 1.5f, px + 1.5f, 662f, 682f, VeilmouthGround.RiverBed - 1f, deck - 1.2f);
            }

            Take(Rect(778, 662, 852, 682), Use.Solid);
        }

        // ------------------------------------------------------------------------------------------- buildings

        private static void Government()
        {
            // The Governor's Palace closes the avenue (§3.1): the marble centre with a wing either side, on the forecourt.
            House(VictorianHouses.Key("Civic16", "marble"), Facing.S, 530f, 480, name: "The Governor's Palace", force: true);
            House(VictorianHouses.Key("Wing12", "marble"), Facing.S, 530f, 450, name: "The Governor's Palace - west wing", force: true);
            House(VictorianHouses.Key("Wing12", "marble"), Facing.S, 530f, 520, name: "The Governor's Palace - east wing", force: true);

            // The Town Hall on the avenue's west flank, facing it, on its podium over the slope.
            House(VictorianHouses.Key("Civic16", "stone"), Facing.E, 478f, 606, name: "The Town Hall");
        }

        private static void Openable()
        {
            // The Wardens' station and lock-up in the Assize, on the avenue's east flank (§3.1, §5.3).
            var assize = House(VictorianHouses.Key("Civic12", "stone"), Facing.W, 522f, 610, name: "The Assize and the Wardens' station", force: true);
            Entrance(assize, "wardens-lockup.street", VeilmouthDressing.LockupScenePath, "wardens-lockup.inside");

            // The faction's house, a villa on Ridge Street in the largest plot (§19).
            var faction = Plot(Facing.S, VillaFront, FactionPlot0, FactionPlot1, VictorianHouses.Key("Villa6", "stucco"), VillaGarden, VillaBehind,
                new Random(FactionPlot0), "The faction's house", FactionAt, true);
            Entrance(faction, "faction-house.street", VeilmouthDressing.FactionHouseScenePath, "faction-house.inside");

            // The Soot & Bell, the corner house on Bell Street with its yard behind, and Harrow & Tincture by it.
            var bell = House(VictorianHouses.Key("Tavern6", "brick"), Facing.S, 730f, 696, name: "The Soot & Bell", force: true);
            Entrance(bell, VeilmouthScene.SootAndBellStreetPortalId, VeilmouthScene.SootAndBellScenePath, VeilmouthScene.SootAndBellInsidePortalId);
            var harrow = House(VictorianHouses.Key("Shop4", "brick"), Facing.S, 730f, 686, name: "Harrow & Tincture", force: true);
            Entrance(harrow, "harrow-apothecary.street", VeilmouthDressing.HarrowScenePath, "harrow-apothecary.inside");
            Prop("NoticeBoard", Facing.S, 711f, 728f, "Notice Board");

            // The missing person's tenement on Loom Lane.
            var missing = House(VictorianHouses.Key("Tenement4", "brick2"), Facing.S, 778f, 625, name: "The missing person's tenement", force: true);
            Entrance(missing, "missing-room.street", VeilmouthDressing.MissingRoomScenePath, "missing-room.inside");

            // The old chapel in the north-west corner, its forecourt toward Chapel Lane (§3.7).
            var chapelKey = "Chapel";
            var chapel = House(chapelKey, Facing.S, 540f, 304, name: "The old chapel", force: true);
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
            var palette = Keys("Tenement4", "brick", "brick2", "grey").Concat(Keys("Shop4", "brick", "grey")).ToArray();
            var rng = new Random(45);
            foreach (var side in new[] { 1, -1 })
            {
                for (var x = 10; x < 56;)
                {
                    var key = palette[rng.Next(palette.Length)];
                    var e = _kit[key];
                    var z0 = side > 0 ? 4f : -4f - e.Depth;
                    var local = new[] { new Vector2(x, z0), new Vector2(x + e.Width, z0), new Vector2(x + e.Width, z0 + e.Depth), new Vector2(x, z0 + e.Depth) };
                    var quad = local.Select(l => Plan(frame.transform.TransformPoint(new Vector3(l.x, 0f, l.y)))).ToArray();
                    if (x + e.Width > 56 || !Free(quad, true))
                    {
                        x += 1;
                        continue;
                    }

                    Take(quad, Use.Solid);
                    var (min, max) = GroundUnder(quad);
                    var turns = side > 0 ? 2 : 0;
                    var house = Put(key, frame.transform, x, z0, turns, Up(max));
                    foreach (Transform child in house.transform)
                    {
                        if (child.name == VictorianHouses.DoorMarker)
                        {
                            var f = house.transform.forward;
                            Doorway(child.position, new Vector2(f.x, -f.z), Up(max));
                        }
                    }

                    if (Up(max) - min > 0.25f)
                    {
                        var centre = frame.transform.TransformPoint(new Vector3(x + (e.Width / 2f), 0f, z0 + (e.Depth / 2f)));
                        _works.Box(new Vector3(centre.x, (Up(max) + min - 0.6f) / 2f, centre.z), new Vector3(e.Width, Up(max) - min + 0.6f, e.Depth), 45f);
                    }

                    x += e.Width;
                }
            }
        }

        private static void Houses()
        {
            var shops = Keys("Shop4", "brick", "brick2", "stucco", "grey", "brick", "brick2", "blue").Concat(Keys("Shop6", "stucco", "brick")).ToArray();
            var tenements = Keys("Tenement4", "brick", "brick2", "grey", "brick2").Concat(Keys("Tenement2", "brick2", "grey")).ToArray();
            var market = Keys("Shop4", "brick", "brick2", "grey", "stucco").Concat(Keys("Tenement4", "brick", "grey")).Concat(Keys("Tenement2", "brick2", "grey")).ToArray();
            var warehouses = Keys("Warehouse6", "warehouse");

            // The arch house of the terraces: through it from Terrace Street West into Wardley Garden (§7).
            House(VictorianHouses.Key("Arch4", "stucco"), Facing.W, 320f, 630, name: "Arch house - to Wardley Garden");
            // The arch house of Lower Veil: from Mercers Row into the tenement court behind it.
            House(VictorianHouses.Key("Arch4", "brick"), Facing.S, 730f, 600, name: "Arch house - to the tenement court");

            // High Street (§3.2): four storeys of one cornice height, shops below, wall to wall.
            Row(Facing.S, 662f, 280, 778, shops, 1);
            Row(Facing.N, 680f, 280, 778, shops, 2);

            // The Terraces (§3.3) and the Ridge (§3.4): detached houses, each on its own railed plot with its garden (§19).
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

            // Under the Ridge and Ridge Lane: tenements.
            Row(Facing.S, 620f, 545, 698, tenements, 9);
            Row(Facing.N, 626f, 545, 698, tenements, 10);

            // Market Quarter (§3.5) and Lower Veil (§3.6).
            Row(Facing.S, 730f, 280, 724, market, 11);
            Row(Facing.N, 740f, 280, 724, market, 12);
            Row(Facing.S, 720f, 370, 430, market, 13);
            Row(Facing.N, 765f, 370, 430, market, 14);
            Row(Facing.E, 370f, 720, 765, market, 15);
            Row(Facing.W, 430f, 720, 765, market, 16);
            Row(Facing.E, 454f, 690, 812, market, 17);
            Row(Facing.W, 466f, 690, 812, market, 18);
            Row(Facing.S, 778f, 560, 724, tenements, 19);
            Row(Facing.N, 784f, 560, 724, tenements, 20);
            Row(Facing.E, 724f, 690, 812, tenements, 21);
            Row(Facing.W, 736f, 690, 780, warehouses, 22);
            Row(Facing.S, 812f, 280, 724, warehouses, 23);

            // The Customs House at the basin's head.
            House(VictorianHouses.Key("Civic12", "stone"), Facing.S, 780f, 744, name: "The Customs House");
        }

        // ------------------------------------------------------------------------------------------- squares

        private static void Market()
        {
            // Market Square (§3.5, §6.1): stalls in rows with an aisle, the Masters' counters, the seer's booth.
            Prop("Stall_Goods", Facing.S, 378f, 721f, "The trader's stall");
            Prop("Stall_Empty", Facing.S, 388f, 721f, "Master of Enchanting");
            Prop("Stall_Empty", Facing.S, 398f, 721f, "Master of First Aid");
            Prop("BottleTable", Facing.S, 408f, 722f);
            Prop("Counter", Facing.S, 414f, 722f, "The seer's counter");
            Prop("Candles", Facing.S, 418f, 722f);
            Prop("Stall_Food", Facing.N, 388f, 758f);
            Prop("Stall_Fish", Facing.N, 398f, 758f);
            Prop("Stall_Meat", Facing.N, 408f, 758f);
            Prop("Stall_Weapons", Facing.E, 372f, 746f, "Master of Item Creation");
            for (var py = 744; py < 764; py += 2)
            {
                Prop("Fence", Facing.E, 384f, py);
            }

            Prop("Anvil", Facing.N, 377f, 752f);
            Prop("LeatherFrame", Facing.N, 379f, 757f);
            Prop("ArmourStand", Facing.N, 375f, 760f);
            Prop("Firewood", Facing.N, 381f, 760f);
            Prop("Barrel", Facing.N, 382f, 748f);
            foreach (var (px, py) in new[] { (396, 742), (406, 742), (416, 742) })
            {
                Prop("Bench", Facing.N, px, py);
            }
        }

        /// <summary>The small green beside the arch house to Wardley Garden (§19): unrailed, a tree, shrubs, a bench.</summary>
        private static void Gardens()
        {
            Prop("Tree_Fruit", Facing.N, 325f, 646f);
            foreach (var (px, py, key) in new[] { (329f, 642f, "Shrub"), (329f, 649f, "Poppy"), (322f, 649.5f, "Shrub"), (317.5f, 649.5f, "Poppy") })
            {
                Prop(key, Facing.N, px, py);
            }

            Prop("Bench", Facing.W, 318f, 642f);
        }

        private static void Squares()
        {
            // Crown Avenue's mall: trees in pairs every 20 m, benches between them (§3.1).
            for (var py = 566; py < 640; py += 20)
            {
                Prop("Tree_Fruit", Facing.N, 495f, py);
                Prop("Tree_Fruit", Facing.N, 504f, py);
                Prop("Bench", Facing.E, 494f, py + 9f);
                Prop("Bench", Facing.W, 504f, py + 9f);
            }

            Prop("Column", Facing.N, 499f, 670f, "The Crown Column");
            foreach (var (px, py) in new[] { (470, 538), (528, 538), (470, 552), (528, 552) })
            {
                Prop("Tree_Fruit", Facing.N, px, py);
            }

            // Wardley Garden (§3.3): railings round a lawn, trees at its corners, benches, a gate on each side.
            Railings(true, 607f, 341, 355);
            Railings(true, 607f, 361, 389);
            Railings(true, 643f, 341, 389);
            Railings(false, 341f, 607, 629);
            Railings(false, 341f, 641, 643);
            Railings(false, 389f, 607, 643);
            foreach (var (px, py) in new[] { (346, 612), (382, 612), (346, 636), (382, 636), (364, 624) })
            {
                Prop("Tree_Fruit", Facing.N, px, py);
            }

            foreach (var (px, py, f) in new[] { (356, 620, Facing.S), (370, 620, Facing.S), (356, 630, Facing.N), (370, 630, Facing.N) })
            {
                Prop("Bench", f, px, py);
            }

            var rng = new Random(606);
            for (var i = 0; i < 40; i++)
            {
                Prop(i % 4 == 0 ? "Poppy" : "Shrub", Facing.N, 341 + rng.Next(48), 607 + rng.Next(36));
            }

            // The chapel's graveyard and forecourt; Ritual Site Candidate 3 is the forecourt (§3.7).
            for (var px = 328; px <= 340; px += 3)
            {
                for (var py = 512; py <= 532; py += 4)
                {
                    Prop("Grave", Facing.S, px, py);
                }
            }

            for (var px = 326; px < 344; px += 4)
            {
                Prop("GraveWall", Facing.N, px, 509.9f);
                Prop("GraveWall", Facing.N, px, 537.9f);
            }

            Circle(World(311f, 548f, Up(Ground(311f, 548f))), "Candidate 3, before the old chapel");

            // Ritual Site Candidate 1, the small square in Lower Veil, between Bell Street and Loom Lane.
            Prop("Bench", Facing.N, 645f, 746f);
            Prop("Bench", Facing.N, 660f, 746f);
            Prop("TrashCan", Facing.N, 643f, 774f);
            Circle(World(654.5f, 760f, Up(Ground(654.5f, 760f))), "Candidate 1, the Lower Veil square");

            // The Soot & Bell's yard (§6.1) and the tenement court.
            Prop("Barrels", Facing.N, 698f, 705f);
            Prop("Barrel", Facing.N, 703f, 705f);
            Prop("Firewood", Facing.N, 706f, 705f);
            Prop("Crate", Facing.N, 712f, 706f);
            Prop("Box", Facing.N, 713f, 709f);
            Prop("Barrel", Facing.N, 712f, 703f);
            Prop("Barrel", Facing.N, 610f, 712f);
            Prop("Crate", Facing.N, 612f, 716f);
            Prop("Box", Facing.N, 608f, 714f);

            // The pawnbroker's counter in a gap on Loom Lane, and the cellar yard.
            Prop("Counter", Facing.S, 701f, 775f, "Pawn shop counter");
            Prop("Chest", Facing.S, 701f, 769f, "Pawn shop chest");
            Prop("Barrel", Facing.N, 715f, 630f);
            Prop("Box", Facing.N, 718f, 628f);

            // The docks (§6.1): cargo on the quays.
            var cargo = new Random(812);
            for (var px = 290; px < 730; px += 22)
            {
                Prop(cargo.Next(2) == 0 ? "Barrels" : "Crate", Facing.N, px + cargo.Next(6), 820f + cargo.Next(4));
                Prop("Box", Facing.N, px + 8 + cargo.Next(4), 821f + cargo.Next(4));
            }

            Prop("Barrels", Facing.N, 750f, 783f);
            Prop("Crate", Facing.N, 760f, 784f);
        }

        // ------------------------------------------------------------------------------------------- lamps

        /// <summary>
        /// The furniture rules (§4): a lamp in the pavement's cell by the kerb, at a party wall where one is near, never
        /// within 2 m of a door along the street; 25–30 m apart, the two sides staggered on streets wider than 12 m.
        /// </summary>
        private static void AllLamps()
        {
            Lamps(true, 666f, 280, 780, 28f, 4f);
            Lamps(true, 675f, 280, 780, 28f, 18f);
            Lamps(true, 583f, 280, 708, 27f, 6f);
            Lamps(true, 588f, 280, 708, 27f, 6f);
            Lamps(false, 485f, 560, 646, 25f, 5f);
            Lamps(false, 514f, 560, 646, 25f, 5f);
            Lamps(false, 307f, 606, 662, 26f, 8f);
            Lamps(false, 312f, 606, 662, 26f, 21f);
            Lamps(false, 417f, 606, 662, 26f, 8f);
            Lamps(false, 422f, 606, 662, 26f, 21f);
            Lamps(true, 732f, 280, 724, 27f, 5f);
            Lamps(true, 737f, 280, 724, 27f, 18f);
            Lamps(false, 456f, 680, 812, 27f, 12f);
            Lamps(false, 463f, 680, 812, 27f, 25f);
            Lamps(false, 726f, 680, 812, 27f, 12f);
            Lamps(false, 733f, 680, 812, 27f, 25f);
            Lamps(true, 813f, 280, 740, 30f, 10f);
            foreach (var (px, py) in new[] { (461, 531), (538, 531), (461, 558), (538, 558), (476, 647), (523, 647), (476, 694), (523, 694) })
            {
                Prop("LampPost", Facing.N, px, py);
            }
        }

        private static void Lamps(bool alongX, float cell, int a0, int a1, float spacing, float phase)
        {
            for (var target = a0 + phase; target < a1 - 1f; target += spacing)
            {
                var walls = _partyWalls.Where(w => w.AlongX == alongX && Mathf.Abs(w.Front - cell) < 6f && Mathf.Abs(w.At - target) <= 5f)
                    .Select(w => w.At).OrderBy(w => Mathf.Abs(w - target)).ToList();
                walls.Add(Mathf.Round(target));
                foreach (var at in walls.Distinct())
                {
                    var centre = at - 0.5f;
                    // A lamp's centre is half a cell past its cell line, so the door's 8 m reach counts to that centre.
                    if (_doors.Any(d => Mathf.Abs((alongX ? d.At.y : d.At.x) - cell) < 8.5f &&Mathf.Abs((alongX ? d.At.x : d.At.y) - centre) < 2.2f))
                    {
                        continue;
                    }

                    var placed = alongX ? Prop("LampPost", Facing.N, at - 1f, cell) : Prop("LampPost", Facing.N, cell, at - 1f);
                    if (placed != null)
                    {
                        break;
                    }
                }
            }
        }

        // ------------------------------------------------------------------------------------------- outside

        private static void Fields()
        {
            var farm = "House_Farm";
            foreach (var (px, py, f) in new[] { (150f, 640f, Facing.S), (200f, 700f, Facing.N), (110f, 705f, Facing.N), (230f, 600f, Facing.E) })
            {
                House(farm, f, f == Facing.S ? py : f == Facing.N ? py : px, (int)(f == Facing.E ? py : px), force: true);
            }

            // The flax field, fenced, a gap in its south side.
            for (var px = 170; px < 186; px += 2)
            {
                if (px != 178)
                {
                    Prop("Fence", Facing.N, px, 776f);
                }

                Prop("Fence", Facing.N, px, 760f);
            }

            for (var py = 760; py < 776; py += 2)
            {
                Prop("Fence", Facing.E, 170f, py);
                Prop("Fence", Facing.E, 186f, py);
            }

            for (var i = 0; i < 5; i++)
            {
                for (var j = 0; j < 2; j++)
                {
                    Prop("Flax", Facing.N, 172 + (3 * i), 764 + (6 * j));
                }
            }

            var rng = new Random(250);
            for (var i = 0; i < 160; i++)
            {
                var px = 60 + rng.Next(210);
                var py = 520 + rng.Next(280);
                if (Mathf.Abs(py - 672) < 8)
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
            bool InForest(float px, float py) => px > 60 && px < 740 && py > 60 && py < 455 && (px < 606 || px > 640);

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
            Person("Blue", false, 519.5f, 621f, 270f, "Warden at the station");
            Person("Blue", false, 519.5f, 629f, 270f, "Warden at the station");
            Person("Blue", false, 600f, 671f, 90f, "Warden on the High Street");
            Person("Blue", true, 650f, 735f, 270f, "Warden on Bell Street");

            // The faction, teal, at its house; Veinmarket, brown, on Loom Lane.
            Person("Teal", false, 593f, 578.6f, 180f, "Faction member");
            Person("Teal", true, 600f, 578.6f, 180f, "Faction member");
            Person("Brown", false, 690f, 781f, 0f, "Veinmarket");
            Person("Brown", false, 692f, 780f, 270f, "Veinmarket");

            // Masters, green, and sellers, orange, in Market Square; the pawnbroker on Loom Lane.
            Person("Green", false, 376f, 750f, 90f, "Master of Item Creation");
            Person("Green", true, 391f, 727f, 180f, "Master of Enchanting");
            Person("Green", false, 401f, 727f, 180f, "Master of First Aid");
            Person("Orange", false, 381f, 727f, 180f, "The trader");
            Person("Orange", true, 416f, 726f, 180f, "The seer");
            Person("Orange", false, 703f, 776f, 0f, "The pawnbroker");

            // Townsfolk, yellow.
            foreach (var (px, py, yaw, female) in new[]
                     {
                         (400f, 735f, 0f, false), (404f, 736f, 200f, true), (680f, 734f, 90f, true), (695f, 732f, 270f, false),
                         (560f, 586f, 90f, true), (600f, 585f, 270f, false), (600f, 781f, 90f, true), (480f, 822f, 180f, false),
                         (500f, 820f, 0f, true), (654f, 756f, 30f, false), (430f, 664f, 90f, false), (520f, 678f, 270f, true),
                         (497f, 600f, 0f, false), (365f, 625f, 180f, true),
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
            var blocks = Object.FindObjectsByType<KitBlock>(FindObjectsInactive.Exclude);
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

            /// <summary>A strip from a to b (plan) on the ground, <paramref name="width"/> wide and <paramref name="height"/> above it.</summary>
            public void Ribbon(Vector2 a, Vector2 b, float width, float height)
            {
                var ga = Ground(a.x, a.y);
                var gb = Ground(b.x, b.y);
                var wa = World(a.x, a.y, ga);
                var wb = World(b.x, b.y, gb);
                var dir = (wb - wa).normalized;
                var side = Vector3.Cross(Vector3.up, dir).normalized * (width / 2f);
                var up = Vector3.up * height;
                var down = Vector3.up * -0.2f;
                var p = Part("");
                void Quad(Vector3 q0, Vector3 q1, Vector3 q2, Vector3 q3, Vector3 n, float len)
                {
                    var s = p.V.Count;
                    p.V.AddRange(new[] { q0, q1, q2, q3 });
                    p.N.AddRange(new[] { n, n, n, n });
                    p.UV.AddRange(new[] { new Vector2(0f, 0f), new Vector2(len, 0f), new Vector2(len, 0.3f), new Vector2(0f, 0.3f) });
                    p.I.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 });
                }

                var len = Vector3.Distance(wa, wb) / 2.5f;
                Quad(wa - side + up, wa + side + up, wb + side + up, wb - side + up, Vector3.up, len);
                Quad(wa + side + down, wb + side + down, wb + side + up, wa + side + up, side.normalized, len);
                Quad(wb - side + down, wa - side + down, wa - side + up, wb - side + up, -side.normalized, len);
            }

            public void Finish(string defaultMaterial)
            {
                foreach (var pair in _parts)
                {
                    var name = pair.Key == "" ? defaultMaterial : pair.Key;
                    var (v, n, uv, i) = pair.Value;
                    var mesh = new Mesh { name = $"{_owner.name} - {name}", indexFormat = IndexFormat.UInt32 };
                    mesh.SetVertices(v);
                    mesh.SetNormals(n);
                    mesh.SetUVs(0, uv);
                    mesh.SetTriangles(i, 0);
                    mesh.RecalculateBounds();
                    mesh.RecalculateTangents();
                    var path = $"{SliceKitCatalogue.Folder}/Works/{mesh.name.Replace("&", "and")}.asset";
                    KitStandIns.EnsureFolder(SliceKitCatalogue.Folder + "/Works");
                    var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (existing != null)
                    {
                        EditorUtility.CopySerialized(mesh, existing);
                        mesh = existing;
                    }
                    else
                    {
                        AssetDatabase.CreateAsset(mesh, path);
                    }

                    var go = new GameObject(name);
                    go.transform.SetParent(_owner, false);
                    go.layer = _owner.gameObject.layer;
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    go.AddComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{VictorianMaterials}{name}.mat");
                    GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
                }

                AssetDatabase.SaveAssets();
            }
        }
    }
}

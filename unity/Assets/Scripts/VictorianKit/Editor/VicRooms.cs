using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace GameDev.VictorianKit.EditorTools
{
    /// <summary>
    /// The rooms behind a kit house's windows (Plans/VICTORIAN_STREETS_PLAN.md §4.4, owner 2026-11-08): floors,
    /// ceilings, wall finishes, partitions with doorways, a stair and furniture of Victorian Interiors, cut from the
    /// house's plan and placed by its openings, so that a look through the glass finds a room and not a box.
    /// <para>
    /// Dressing only: nobody enters, so every piece is a plain renderer, with no collider, body, light or script; the rooms take
    /// light probes, cast no shadows and are switched off by distance (<see cref="VicRoomsSwitch"/>) behind glass the
    /// window shader has already turned to its mapped room. The furniture follows Plans/INTERIOR_FURNISHING_RULES.md in short:
    /// an anchor centred on the wall the windows face (G2, G3), its companions round it (G4), nothing tall before a
    /// window (G8), wall lamps where the room is a storey high (G9).
    /// </para>
    /// <para>
    /// Positions are the house's kit units: plan x is Unity X, plan y is Unity Z, height is Unity Y. The pack's walls,
    /// floors, ceilings and stair stretch to the rooms; its furniture keeps its own size under a house scaled by
    /// houseScale, and a piece taller than the room shrinks to fit it. Layout randomness is a System.Random from the
    /// house's name. Every size here is an authoring default unless marked measured.
    /// </para>
    /// </summary>
    public static class VicRooms
    {
        public const string RoomFolder = "Assets/Prefabs/SliceKit/ViRoom";
        public const string FurnitureFolder = "Assets/Prefabs/SliceKit/ViFurniture";
        public const string RootName = "Rooms";

        /// <summary>One line per room of every house built since it was last cleared: kind, size, openings, what stands and what did not fit.</summary>
        public static readonly List<string> Report = new List<string>();

        /// <summary>The measured bounds of every block used so far, one line each: name, size and centre in its own metres.</summary>
        public static IEnumerable<string> SizeReport() =>
            Sizes.OrderBy(p => p.Key).Select(p => $"size\t{Path.GetFileNameWithoutExtension(p.Key)}\t{p.Value.size.x:F2} x {p.Value.size.y:F2} x {p.Value.size.z:F2}\tcentre {p.Value.center.x:F2}, {p.Value.center.y:F2}, {p.Value.center.z:F2}");

        // Measured on the pack (SliceKitCatalogue): its walls stand 3.5 m, a plain wall is 0.22 m thick with its face
        // 0.08 m before its line, the stair rises 3.54 m, and a balustrade's nosing reaches 0.219 m past the well's edge.
        private const float PackStorey = 3.5f, PackStairRise = 3.54f, WallBehind = 0.14f, RailOverhang = 0.219f;

        /// <summary>A partition's line from its cell edge, so its 0.22 m body is centred on the edge.</summary>
        private const float InnerLine = 0.03f;

        /// <summary>A lining's line from the outline: its back on the kit wall's inner face, 0.25 m in.</summary>
        private static readonly float OuterLine = (float)VicKit.WallT + WallBehind;

        /// <summary>
        /// In the mansard, the knee wall's back stands this far inside the edge of the flat deck (VicRoofs.Mansard):
        /// 2.4 m from a straight outline, as before, and never under a slope.
        /// </summary>
        private const float KneeInset = 0.4f;

        /// <summary>Half a window's or door's 2 m module, along the wall: nothing tall stands in it.</summary>
        private const float OpeningHalf = 0.95f;

        /// <summary>A piece taller than this, kit m, may not stand before a window (the sill is 0.85 m).</summary>
        private const float SillTop = 0.8f;

        private static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, Bounds> Sizes = new Dictionary<string, Bounds>();

        // ------------------------------------------------------------------------------------------------ data

        private sealed class Opening
        {
            public bool AlongX;  // on an outline edge along X (else along Z)
            public float Line;   // the edge's Z (along X) or X (along Z)
            public float Centre; // its middle along the edge
            public bool Door;
        }

        private sealed class Side
        {
            public int Index;      // 0 south (−Z, toward the street), 1 east, 2 north, 3 west
            public Vector3 In;     // into the room
            public Vector3 U;      // along the wall, cross(up, In): a block's local +X when its +Z is In
            public Vector3 Start;  // where along is 0, on the line
            public float Length;
            public float Edge;     // the cell edge: Z for south and north, X for east and west
            public bool Exterior;  // the whole side is the house's outer wall
            public readonly List<(float A, float B)> Windows = new List<(float, float)>();
            public readonly List<(float A, float B)> Doors = new List<(float, float)>();
            public readonly List<(float A, float B, float Top)> Standing = new List<(float, float, float)>();

            public float Along(Vector3 p) => Vector3.Dot(p - Start, U);

            public Vector3 At(float along, float deep) => Start + (U * along) + (In * deep);

            public bool Open(float a, float b) =>
                Windows.Concat(Doors).Any(w => a < w.B && b > w.A);
        }

        private sealed class Room
        {
            public int Index;
            public RectInt Cells;
            public int Storey;
            public bool Attic;
            public string Kind;
            public string Finish;
            public float Floor, Height;
            public Side[] Sides;
            public readonly List<Rect> Taken = new List<Rect>();
            public readonly List<Rect> BeforeWindows = new List<Rect>();
            public Rect Box; // the furniture lines

            /// <summary>A window, or a glazed street door, shows the room from outside.</summary>
            public bool Seen => StreetDoor || Sides.Any(s => s.Windows.Count > 0);

            public bool StreetDoor;
            public bool Front;
            public readonly List<string> Pieces = new List<string>();
            public readonly List<string> Missed = new List<string>();
            public readonly Dictionary<string, int> Why = new Dictionary<string, int>(); // failed tries by reason, for the report
        }

        private struct Placed
        {
            public Rect Foot;
            public float A0, A1, Depth, Top;
        }

        private sealed class Stair
        {
            public Vector3 Foot; // the foot's middle, on the floor plane
            public Vector3 Dir;  // the way it climbs
            public Vector3 Across; // toward the open side of its well
            public float Width, Length;
            public RectInt Cells; // the room it stands in
        }

        private sealed class House
        {
            public VicHouseSpec Spec;
            public Transform Root;
            public float Scale;   // 1 / houseScale: the furniture's own size in kit units
            public Random Rng;
            public string Wealth; // Rich, Common or Poor
            public readonly Dictionary<int, List<Opening>> Openings = new Dictionary<int, List<Opening>>();
            public readonly List<Room> Rooms = new List<Room>();
            public readonly Dictionary<(int Storey, int I, int J), Room> At = new Dictionary<(int, int, int), Room>();
            public Stair Stair;
            public int Storeys; // with the attic, if any
            public readonly HashSet<(int I, int J)> Flat = new HashSet<(int, int)>(); // the mansard's flat deck tiles, by cell
        }

        // ------------------------------------------------------------------------------------------------ entry

        /// <summary>
        /// Builds the rooms of <paramref name="spec"/> under <paramref name="house"/> (a child named <see cref="RootName"/>)
        /// and returns that child. <paramref name="houseScale"/> is the house root's uniform scale: 1 on the show
        /// streets, 1.25 in Veilmouth.
        /// </summary>
        public static GameObject Build(VicHouseSpec spec, Transform house, float houseScale = 1f)
        {
            if (spec.Plan == null)
            {
                throw new ArgumentException($"{spec.Name} has no plan to cut rooms from.");
            }

            var root = new GameObject(RootName).transform;
            root.SetParent(house, false);
            var h = new House
            {
                Spec = spec,
                Root = root,
                Scale = 1f / houseScale,
                Rng = new Random(spec.Name.Aggregate(17, (a, c) => (a * 31) + c)),
                Wealth = spec.Palette?.Name ?? "Common",
            };

            ReadOpenings(h);
            CutRooms(h);
            PlaceStair(h);
            foreach (var room in h.Rooms)
            {
                Shell(h, room);
            }

            Partitions(h);
            StairPieces(h);
            foreach (var room in h.Rooms.Where(r => r.Seen))
            {
                Furnish(h, room);
            }

            foreach (var r in h.Rooms)
            {
                var sides = string.Join(" ", r.Sides.Select(s => $"{"SENW"[s.Index]}{(s.Exterior ? "x" : "")}{s.Windows.Count}/{s.Doors.Count}"));
                Report.Add($"{spec.Name}\t{r.Storey}\t{r.Kind}\t{r.Cells}\tbox {r.Box.width:F2}x{r.Box.height:F2}\t{(r.Front ? "front" : "back")}\t{(r.Seen ? "seen" : "-")}\t{sides}"
                    + $"\tput {r.Pieces.Count}: {string.Join(",", r.Pieces)}\tmissed: {string.Join(",", r.Missed)}\twhy: {string.Join(",", r.Why.Select(p => $"{p.Key} {p.Value}"))}"
                    + $"\ttaken: {string.Join(" ", r.Taken.Select(t => $"[{t.xMin:F1},{t.yMin:F1},{t.xMax:F1},{t.yMax:F1}]"))}\tbox [{r.Box.xMin:F1},{r.Box.yMin:F1},{r.Box.xMax:F1},{r.Box.yMax:F1}]");
            }

            Panes(h, root.gameObject);
            Finish(root.gameObject);
            return root.gameObject;
        }

        // ------------------------------------------------------------------------------------------------ the panes

        /// <summary>Panes told their room, and panes matched to none, since the counts were last cleared.</summary>
        public static int PanesTold, PanesUntold;

        /// <summary>
        /// Records on the rooms' root which room stands behind each pane of the house's windows
        /// (<see cref="VicWindowRooms"/>, reference plan R2 step 4): the room's finish, its back wall's depth behind the
        /// glass, and its side walls, floor and ceiling from the pane's middle and sill, in the pane's own UV metres. A
        /// pane belongs to the room whose window or glazed door it stands in; a pane matched to none keeps the window
        /// shader's made-up room.
        /// </summary>
        private static void Panes(House h, GameObject root)
        {
            var model = h.Root.parent;
            var panes = new List<MeshRenderer>();
            var rooms = new List<uint>();
            foreach (var r in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.transform.IsChildOf(h.Root))
                {
                    continue;
                }

                var slot = Array.FindIndex(r.sharedMaterials, m => m != null && m.shader != null && m.shader.name == VicMaterials.WindowShader);
                var filter = r.GetComponent<MeshFilter>();
                var mesh = filter != null ? filter.sharedMesh : null;
                if (slot < 0 || mesh == null || slot >= mesh.subMeshCount)
                {
                    continue;
                }

                var room = PaneShape(model, r.transform, mesh, slot) is { } shape ? PaneRoom(h, shape) : 0u;
                if (room == 0u)
                {
                    PanesUntold++;
                    continue;
                }

                panes.Add(r);
                rooms.Add(room);
                PanesTold++;
            }

            if (panes.Count > 0)
            {
                root.AddComponent<VicWindowRooms>().Configure(panes.ToArray(), rooms.ToArray());
            }
        }

        /// <summary>
        /// A pane's frame in the house's space, fitted to its glass's vertices by least squares: the point where its UV
        /// is zero (the pane's middle at the sill) and the steps of one UV metre along u and along v.
        /// </summary>
        private static (Vector3 Anchor, Vector3 PerU, Vector3 PerV)? PaneShape(Transform model, Transform pane, Mesh mesh, int slot)
        {
            var vertices = mesh.vertices;
            var uvs = mesh.uv;
            if (uvs.Length != vertices.Length)
            {
                return null;
            }

            var used = mesh.GetIndices(slot).Distinct().ToArray();
            if (used.Length < 3)
            {
                return null;
            }

            // Normal equations of P = anchor + u·perU + v·perV over the glass's vertices.
            var a = Matrix4x4.zero;
            Vector3 s1 = Vector3.zero, su = Vector3.zero, sv = Vector3.zero;
            foreach (var i in used)
            {
                var (u, v) = (uvs[i].x, uvs[i].y);
                var p = model.InverseTransformPoint(pane.TransformPoint(vertices[i]));
                a[0, 0] += 1f;
                a[0, 1] += u;
                a[0, 2] += v;
                a[1, 1] += u * u;
                a[1, 2] += u * v;
                a[2, 2] += v * v;
                s1 += p;
                su += u * p;
                sv += v * p;
            }

            a[1, 0] = a[0, 1];
            a[2, 0] = a[0, 2];
            a[2, 1] = a[1, 2];
            a[3, 3] = 1f;
            if (Mathf.Abs(a.determinant) < 1e-6f)
            {
                return null;
            }

            var inv = a.inverse;
            Vector3 Row(int k) => (inv[k, 0] * s1) + (inv[k, 1] * su) + (inv[k, 2] * sv);
            return (Row(0), Row(1), Row(2));
        }

        /// <summary>The packed room behind a pane (<see cref="VicWindowRooms.Pack"/>), or 0 when it stands in no room's opening.</summary>
        private static uint PaneRoom(House h, (Vector3 Anchor, Vector3 PerU, Vector3 PerV) pane)
        {
            var (anchor, perU, perV) = pane;
            var scale = perU.magnitude;
            var rise = perV.magnitude;
            if (scale < 0.1f || rise < 0.1f)
            {
                return 0u;
            }

            Room best = null;
            Side side = null;
            var bestOut = float.MaxValue;
            foreach (var r in h.Rooms)
            {
                if (anchor.y < r.Floor - 0.2f || anchor.y > r.Floor + r.Height)
                {
                    continue;
                }

                foreach (var s in r.Sides)
                {
                    var outside = -Vector3.Dot(anchor - s.Start, s.In); // how far the glass stands before the side's line
                    var along = s.Along(anchor);
                    if (outside < -0.3f || outside > 1.5f || !s.Windows.Concat(s.Doors).Any(w => along > w.A - 0.3f && along < w.B + 0.3f))
                    {
                        continue;
                    }

                    if (outside < bestOut)
                    {
                        (best, side, bestOut) = (r, s, outside);
                    }
                }
            }

            if (best == null)
            {
                return 0u;
            }

            var at = side.Along(anchor);
            var plus = Vector3.Dot(perU, side.U) >= 0f;
            var (left, right) = plus ? (at, side.Length - at) : (side.Length - at, at);
            var across = (Mathf.Abs(side.In.x) * best.Cells.width * 2f) + (Mathf.Abs(side.In.z) * best.Cells.height * 2f);
            var depth = across - (2f * OuterLine) + bestOut;
            var finish = best.Finish switch { "Dark" => 1, "Panel" => 2, _ => 3 };
            return VicWindowRooms.Pack(finish, depth / scale, left / scale, right / scale, (best.Floor + best.Height - anchor.y) / rise, (anchor.y - best.Floor) / rise);
        }

        // ------------------------------------------------------------------------------------------------ openings

        /// <summary>Every window, door and lucarne of the house, by storey, on the outline edge it stands in.</summary>
        private static void ReadOpenings(House h)
        {
            var plan = h.Spec.Plan;
            var attic = -1;
            foreach (var p in h.Spec.House.Pieces)
            {
                if (p.Id == "SM_Vic_Roof_Flat_Deck")
                {
                    h.Flat.Add((Mathf.RoundToInt((float)p.X / 2f), Mathf.RoundToInt((float)p.Y / 2f)));
                    continue;
                }

                var window = p.Id.StartsWith("SM_Vic_Window_", StringComparison.Ordinal);
                var door = p.Id.StartsWith("SM_Vic_Door_", StringComparison.Ordinal);
                var lucarne = p.Id.StartsWith("SM_Vic_Lucarne_", StringComparison.Ordinal);
                if (!window && !door && !lucarne)
                {
                    continue;
                }

                int storey;
                if (lucarne)
                {
                    storey = h.Spec.Floors;
                    attic = storey;
                }
                else
                {
                    storey = Mathf.FloorToInt((float)((p.Z - VicKit.Foundation + 0.1) / VicKit.Storey));
                }

                var best = (Distance: float.MaxValue, Edge: (VicEdge)null);
                foreach (var e in plan.Edges)
                {
                    var alongX = e.Dy == 0;
                    var line = alongX ? e.Y0 : e.X0;
                    var a = alongX ? (float)p.X : (float)p.Y;
                    var lo = Mathf.Min(alongX ? e.X0 : e.Y0, alongX ? e.X1 : e.Y1);
                    var hi = Mathf.Max(alongX ? e.X0 : e.Y0, alongX ? e.X1 : e.Y1);
                    var d = Mathf.Abs((alongX ? (float)p.Y : (float)p.X) - line);
                    if (a >= lo - 0.5f && a <= hi + 0.5f && d < best.Distance)
                    {
                        best = (d, e);
                    }
                }

                if (best.Edge == null || best.Distance > (lucarne ? 2.5f : 1.5f))
                {
                    continue;
                }

                var edge = best.Edge;
                var along = edge.Dy == 0;
                if (!h.Openings.TryGetValue(storey, out var list))
                {
                    h.Openings[storey] = list = new List<Opening>();
                }

                var centre = along ? (float)p.X : (float)p.Y;
                if (list.Any(o => o.AlongX == along && Mathf.Abs(o.Centre - centre) < 0.3f && Mathf.Abs(o.Line - (along ? edge.Y0 : edge.X0)) < 0.1f))
                {
                    continue; // a lucarne of two parts, or a door's two leaves
                }

                list.Add(new Opening { AlongX = along, Line = along ? edge.Y0 : edge.X0, Centre = centre, Door = door });
            }

            h.Storeys = attic >= 0 ? h.Spec.Floors + 1 : h.Spec.Floors;
        }

        // ------------------------------------------------------------------------------------------------ rooms

        /// <summary>
        /// The plan's cells cut into rectangles row by row, a hall strip at the front door where the house is 8 m deep or
        /// more, each part into rooms of 4–6 m, and a front and a back band where it is 8 m deep. Every storey keeps the
        /// same cut, so walls stand over walls. No wall meets the outline in a door's opening, and none in a window's
        /// where another cut serves: in a house with 1 m corners the modules' middles fall on the 2 m cells' lines, and a
        /// partition stood behind the middle of a door (owner, fifth play test, 2026-11-09).
        /// </summary>
        private static void CutRooms(House h)
        {
            var plan = h.Spec.Plan;
            var rects = Rectangles(plan);
            var door = FrontDoor(h);
            var parts = new List<(RectInt Cells, bool Hall)>();
            foreach (var r in rects)
            {
                var strip = door.HasValue && r.Contains(new Vector2Int(door.Value.i, door.Value.j)) && door.Value.alongX && r.height >= 4 && r.width >= 3
                    ? HallStrip(h, r, door.Value.at.x)
                    : null;
                if (strip == null)
                {
                    parts.Add((r, false));
                    continue;
                }

                var (h0, h1) = strip.Value;
                if (h0 > r.xMin)
                {
                    parts.Add((new RectInt(r.xMin, r.yMin, h0 - r.xMin, r.height), false));
                }

                parts.Add((new RectInt(h0, r.yMin, h1 - h0, r.height), true));
                if (h1 < r.xMax)
                {
                    parts.Add((new RectInt(h1, r.yMin, r.xMax - h1, r.height), false));
                }
            }

            var cut = new List<(RectInt Cells, bool Hall)>();
            foreach (var (cells, hall) in parts)
            {
                if (hall)
                {
                    cut.Add((cells, true));
                    continue;
                }

                foreach (var (y, depth) in Bands(h, cells))
                {
                    var x = cells.xMin;
                    foreach (var w in Widths(h, new RectInt(cells.xMin, y, cells.width, depth)))
                    {
                        cut.Add((new RectInt(x, y, w, depth), false));
                        x += w;
                    }
                }
            }

            // A room is a front room when some of its street side looks onto the street, not onto a part of the house before it.
            bool FacesStreet(RectInt cells) => Enumerable.Range(cells.xMin, cells.width).Any(i => !plan.Inside(i, cells.yMin - 1));
            for (var storey = 0; storey < h.Storeys; storey++)
            {
                var attic = storey >= h.Spec.Floors;
                foreach (var (whole, hall) in cut)
                {
                    var under = attic ? UnderFlat(h, whole) : whole;
                    if (under == null)
                    {
                        continue;
                    }

                    var cells = under.Value;
                    var room = new Room
                    {
                        Index = h.Rooms.Count,
                        Cells = cells,
                        Storey = storey,
                        Attic = attic,
                        Floor = (float)(attic ? VicKit.WallTop(h.Spec.Floors) : VicKit.FloorZ(storey)),
                        Height = attic ? (float)(VicKit.RoofLift + VicKit.MansardRise - 0.05) : (float)VicKit.Storey,
                        Kind = hall ? (storey == 0 ? "Hall" : "Landing") : null,
                        Front = FacesStreet(cells),
                    };
                    h.Rooms.Add(room);
                    foreach (var c in Cells(cells))
                    {
                        h.At[(storey, c.x, c.y)] = room;
                    }
                }
            }

            foreach (var room in h.Rooms)
            {
                room.Sides = SidesOf(h, room);
            }

            Kinds(h);
        }

        /// <summary>
        /// The largest part of <paramref name="cells"/> under the mansard's flat, read from the house's own flat deck
        /// tiles (a row's flat runs to an open end, and stops 2 m inside a faced one); null where none is. An attic room
        /// stands only there: the slope rises from the outline over every other cell, and attic walls built over a
        /// pavilion stood out through it (seen 2026-11-08).
        /// </summary>
        private static RectInt? UnderFlat(House h, RectInt cells)
        {
            RectInt? best = null;
            for (var x0 = cells.xMin; x0 < cells.xMax; x0++)
            {
                for (var y0 = cells.yMin; y0 < cells.yMax; y0++)
                {
                    for (var x1 = x0 + 1; x1 <= cells.xMax; x1++)
                    {
                        for (var y1 = y0 + 1; y1 <= cells.yMax; y1++)
                        {
                            var r = new RectInt(x0, y0, x1 - x0, y1 - y0);
                            if ((best == null || r.width * r.height > best.Value.width * best.Value.height) && Cells(r).All(c => h.Flat.Contains((c.x, c.y))))
                            {
                                best = r;
                            }
                        }
                    }
                }
            }

            return best;
        }

        private static List<RectInt> Rectangles(VicPlan plan)
        {
            var left = new HashSet<(int i, int j)>(plan.Cells);
            var rects = new List<RectInt>();
            while (left.Count > 0)
            {
                var (i0, j0) = left.OrderBy(c => c.j).ThenBy(c => c.i).First();
                var i1 = i0;
                while (left.Contains((i1 + 1, j0)))
                {
                    i1++;
                }

                var j1 = j0;
                while (Enumerable.Range(i0, i1 - i0 + 1).All(i => left.Contains((i, j1 + 1))))
                {
                    j1++;
                }

                for (var i = i0; i <= i1; i++)
                {
                    for (var j = j0; j <= j1; j++)
                    {
                        left.Remove((i, j));
                    }
                }

                rects.Add(new RectInt(i0, j0, i1 - i0 + 1, j1 - j0 + 1));
            }

            return rects;
        }

        /// <summary>A partition this near an opening's middle, kit m, stands in its hole: a double door's is 0.70 m, a double window's 0.75 m.</summary>
        private const float OpeningClear = 0.85f;

        /// <summary>
        /// What a partition on <paramref name="line"/> meets where it reaches the outline at <paramref name="wall"/>: 2 in a
        /// door's opening, 1 in a window's, 0 in neither. <paramref name="alongX"/> is the wall's direction; storeys of the
        /// house only, since the attic's walls stop at the knee wall.
        /// </summary>
        private static int Meets(House h, bool alongX, float line, float wall)
        {
            var worst = 0;
            for (var storey = 0; storey < h.Spec.Floors; storey++)
            {
                if (!h.Openings.TryGetValue(storey, out var list))
                {
                    continue;
                }

                foreach (var o in list)
                {
                    if (o.AlongX == alongX && Mathf.Abs(o.Line - wall) < 0.05f && Mathf.Abs(o.Centre - line) < OpeningClear)
                    {
                        worst = Mathf.Max(worst, o.Door ? 2 : 1);
                    }
                }
            }

            return worst;
        }

        /// <summary>What a partition across a part along Z, at cell line <paramref name="i"/>, meets at the part's front and back.</summary>
        private static int MeetsAcross(House h, RectInt part, int i) =>
            Mathf.Max(Meets(h, true, i * 2f, part.yMin * 2f), Meets(h, true, i * 2f, part.yMax * 2f));

        /// <summary>
        /// The hall strip at the street door at <paramref name="doorX"/>: one cell, or two where the door's middle falls on a
        /// cell line, with the door inside it and neither wall in an opening; or none, and the door opens into a room.
        /// </summary>
        private static (int, int)? HallStrip(House h, RectInt part, float doorX)
        {
            foreach (var width in new[] { 1, 2 })
            {
                if (part.width < width + 2)
                {
                    continue;
                }

                for (var h0 = part.xMin; h0 + width <= part.xMax; h0++)
                {
                    var h1 = h0 + width;
                    if (doorX < (h0 * 2f) + OpeningClear || doorX > (h1 * 2f) - OpeningClear)
                    {
                        continue;
                    }

                    if ((h0 == part.xMin || MeetsAcross(h, part, h0) == 0) && (h1 == part.xMax || MeetsAcross(h, part, h1) == 0))
                    {
                        return (h0, h1);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// A part 8 m deep or more as a front and a back band, split as near the middle as a line allows that meets no
        /// door in the sides and the fewest windows; a shallower part, or one whose every line meets a door, as one band.
        /// </summary>
        private static IEnumerable<(int Y, int Depth)> Bands(House h, RectInt part)
        {
            if (part.height < 4)
            {
                return new[] { (part.yMin, part.height) };
            }

            var middle = part.yMin + ((part.height + 1) / 2);
            int Meeting(int j) => Mathf.Max(Meets(h, false, j * 2f, part.xMin * 2f), Meets(h, false, j * 2f, part.xMax * 2f));
            var split = Enumerable.Range(part.yMin + 2, part.height - 3).Where(j => Meeting(j) < 2)
                .OrderBy(j => Meeting(j)).ThenBy(j => Mathf.Abs(j - middle)).Select(j => (int?)j).FirstOrDefault();
            return split.HasValue
                ? new[] { (part.yMin, split.Value - part.yMin), (split.Value, part.yMax - split.Value) }
                : new[] { (part.yMin, part.height) };
        }

        /// <summary>
        /// Room widths in 2 m cells across a band: rooms of 4–6 m, the larger first, with no wall in a door's opening and
        /// one in a window's only where every other cut is worse (a room of 2 m, or one wider than 8 m).
        /// </summary>
        private static IEnumerable<int> Widths(House h, RectInt band)
        {
            float Size(int w) => w == 2 || w == 3 ? 0f : w == 4 ? 1f : w == 1 ? 3f : w - 3f;
            var n = band.width;
            var cost = new float[n + 1];
            var count = new int[n + 1];
            var step = new int[n + 1];
            for (var k = n - 1; k >= 0; k--)
            {
                cost[k] = float.MaxValue;
                for (var w = n - k; w >= 1; w--)
                {
                    var end = k + w;
                    var meets = end == n ? 0 : MeetsAcross(h, band, band.xMin + end);
                    if (meets == 2)
                    {
                        continue;
                    }

                    var c = Size(w) + (meets * 4f) + cost[end];
                    if (c < cost[k] - 0.001f || (Mathf.Abs(c - cost[k]) <= 0.001f && 1 + count[end] < count[k]))
                    {
                        cost[k] = c;
                        count[k] = 1 + count[end];
                        step[k] = w;
                    }
                }
            }

            for (var k = 0; k < n; k += step[k])
            {
                yield return step[k];
            }
        }

        private static IEnumerable<Vector2Int> Cells(RectInt r)
        {
            for (var i = r.xMin; i < r.xMax; i++)
            {
                for (var j = r.yMin; j < r.yMax; j++)
                {
                    yield return new Vector2Int(i, j);
                }
            }
        }

        /// <summary>The street door: the ground-floor door nearest the street; its inner cell, and whether its edge runs along X.</summary>
        private static (int i, int j, bool alongX, Vector3 at, Vector3 inward)? FrontDoor(House h)
        {
            if (!h.Openings.TryGetValue(0, out var list))
            {
                return null;
            }

            var door = list.Where(o => o.Door).OrderBy(o => o.AlongX ? o.Line : 99f).ThenBy(o => o.Centre).FirstOrDefault();
            if (door == null)
            {
                return null;
            }

            var plan = h.Spec.Plan;
            var at = door.AlongX ? new Vector3(door.Centre, 0f, door.Line) : new Vector3(door.Line, 0f, door.Centre);
            foreach (var inward in new[] { Vector3.forward, Vector3.back, Vector3.right, Vector3.left })
            {
                var p = at + (inward * 0.5f);
                var cell = (Mathf.FloorToInt(p.x / 2f), Mathf.FloorToInt(p.z / 2f));
                if ((door.AlongX ? Mathf.Abs(inward.z) > 0.5f : Mathf.Abs(inward.x) > 0.5f) && plan.Inside(cell.Item1, cell.Item2))
                {
                    return (cell.Item1, cell.Item2, door.AlongX, at, inward);
                }
            }

            return null;
        }

        private static Side[] SidesOf(House h, Room r)
        {
            var c = r.Cells;
            float Off(int index)
            {
                var exterior = SideSegments(h, r, index).Any(s => s.Neighbour == null);
                return !exterior ? InnerLine : r.Attic ? KneeInset + WallBehind : OuterLine;
            }

            var offs = Enumerable.Range(0, 4).Select(Off).ToArray();
            var x0 = (c.xMin * 2) + offs[3];
            var x1 = (c.xMax * 2) - offs[1];
            var z0 = (c.yMin * 2) + offs[0];
            var z1 = (c.yMax * 2) - offs[2];
            r.Box = Rect.MinMaxRect(x0, z0, x1, z1);
            var sides = new[]
            {
                new Side { Index = 0, In = Vector3.forward, U = Vector3.right, Start = new Vector3(x0, 0f, z0), Length = x1 - x0, Edge = c.yMin * 2 },
                new Side { Index = 1, In = Vector3.left, U = Vector3.forward, Start = new Vector3(x1, 0f, z0), Length = z1 - z0, Edge = c.xMax * 2 },
                new Side { Index = 2, In = Vector3.back, U = Vector3.left, Start = new Vector3(x1, 0f, z1), Length = x1 - x0, Edge = c.yMax * 2 },
                new Side { Index = 3, In = Vector3.right, U = Vector3.back, Start = new Vector3(x0, 0f, z1), Length = z1 - z0, Edge = c.xMin * 2 },
            };
            foreach (var s in sides)
            {
                var segments = SideSegments(h, r, s.Index).ToList();
                s.Exterior = segments.All(g => g.Neighbour == null);
                if (!h.Openings.TryGetValue(r.Storey, out var openings))
                {
                    continue;
                }

                var alongX = s.Index == 0 || s.Index == 2;
                // An attic side stands on the flat's edge, 2 m in from the outline whose lucarnes it looks out through.
                var line = r.Attic ? s.Edge - ((float)VicKit.MansardInset * (alongX ? s.In.z : s.In.x)) : s.Edge;
                foreach (var o in openings.Where(o => o.AlongX == alongX && Mathf.Abs(o.Line - line) < 0.05f))
                {
                    // Only where this side is the outer wall.
                    var p = alongX ? new Vector3(o.Centre, 0f, s.Edge) : new Vector3(s.Edge, 0f, o.Centre);
                    if (!segments.Any(g => g.Neighbour == null && g.A <= o.Centre && o.Centre <= g.B))
                    {
                        continue;
                    }

                    var a = s.Along(p - (s.U * OpeningHalf));
                    var b = s.Along(p + (s.U * OpeningHalf));
                    var span = (Mathf.Min(a, b), Mathf.Max(a, b));
                    if (o.Door)
                    {
                        s.Doors.Add(span);
                        r.Taken.Add(Strip(s, span.Item1, span.Item2, 1.2f));
                        r.StreetDoor = true;
                    }
                    else
                    {
                        s.Windows.Add(span);
                        r.BeforeWindows.Add(Strip(s, span.Item1, span.Item2, 0.75f));
                    }
                }
            }

            return sides;
        }

        /// <summary>The 2 m segments of one side of a room, in plan metres along its edge, and the room beyond each (null: outside).</summary>
        private static IEnumerable<(float A, float B, Room Neighbour)> SideSegments(House h, Room r, int side)
        {
            var c = r.Cells;
            var alongX = side == 0 || side == 2;
            var n = alongX ? c.width : c.height;
            for (var k = 0; k < n; k++)
            {
                var (i, j) = side switch
                {
                    0 => (c.xMin + k, c.yMin - 1),
                    1 => (c.xMax, c.yMin + k),
                    2 => (c.xMin + k, c.yMax),
                    _ => (c.xMin - 1, c.yMin + k),
                };
                var a = ((alongX ? c.xMin : c.yMin) + k) * 2f;
                h.At.TryGetValue((r.Storey, i, j), out var beyond);
                yield return (a, a + 2f, h.Spec.Plan.Inside(i, j) ? beyond : null);
            }
        }

        /// <summary>A strip along a side, from along a to b and depth deep into the room, in plan XZ.</summary>
        private static Rect Strip(Side s, float a, float b, float deep) => Foot(s.At(a, 0f), s.At(b, deep));

        private static Rect Foot(Vector3 p, Vector3 q) => Rect.MinMaxRect(Mathf.Min(p.x, q.x), Mathf.Min(p.z, q.z), Mathf.Max(p.x, q.x), Mathf.Max(p.z, q.z));

        /// <summary>What each room is for, by wealth, storey and whether it looks onto the street (rules §5).</summary>
        private static void Kinds(House h)
        {
            string[] Cycle(int storey, bool front, bool attic)
            {
                if (attic)
                {
                    return new[] { "Servant" };
                }

                switch (h.Wealth)
                {
                    case "Rich":
                        if (storey == 0)
                        {
                            return front ? new[] { "Dining", "Study", "Parlour" } : new[] { "Kitchen", "Scullery", "Study" };
                        }

                        if (storey == 1)
                        {
                            return front ? new[] { "Drawing", "Study", "Parlour" } : new[] { "Bedroom", "Bathroom" };
                        }

                        return front ? new[] { "Bedroom", "Bedroom", "Parlour" } : new[] { "Bedroom", "Bathroom", "Bedroom" };
                    case "Poor":
                        if (storey == 0)
                        {
                            return front ? new[] { "Living", "Living" } : new[] { "Washroom", "Living" };
                        }

                        return new[] { "Bedroom", "Bedroom", "Living" };
                    default:
                        if (storey == 0)
                        {
                            return front ? new[] { "Parlour", "Dining" } : new[] { "Kitchen", "Scullery" };
                        }

                        return front ? new[] { "Bedroom", "Parlour" } : new[] { "Bedroom", "Bathroom" };
                }
            }

            foreach (var storey in h.Rooms.GroupBy(r => r.Storey))
            {
                var counts = new Dictionary<bool, int>();
                var rooms = storey.Where(r => r.Kind == null).OrderBy(r => r.Cells.yMin).ThenBy(r => r.Cells.xMin).ToList();
                foreach (var r in rooms)
                {
                    var front = r.Front || rooms.Count == 1;
                    counts.TryGetValue(front, out var n);
                    var cycle = Cycle(r.Storey, front, r.Attic);
                    r.Kind = rooms.Count == 1 && !r.Attic ? (r.Storey == 0 ? (h.Wealth == "Poor" ? "Living" : "Parlour") : "Bedroom") : cycle[n % cycle.Length];
                    counts[front] = n + 1;
                }

                foreach (var r in storey)
                {
                    r.Finish = r.Kind switch
                    {
                        "Hall" or "Landing" or "Dining" or "Kitchen" or "Scullery" or "Washroom" => "Dark",
                        "Study" => h.Wealth == "Rich" ? "Panel" : "Dark",
                        "Living" => "Dark",
                        _ => "Paper",
                    };
                }
            }
        }

        // ------------------------------------------------------------------------------------------------ the stair

        /// <summary>
        /// One straight flight per storey, stacked: in the hall against its east wall, climbing away from the door; or,
        /// in a shallow house, along the back wall of the room the door opens into, away from the door. Measured on
        /// the pack: 3.54 m in 19 steps over 6.1 m; here at about 38–48°, as a town house's narrow stair.
        /// </summary>
        private static void PlaceStair(House h)
        {
            if (h.Spec.Floors < 2)
            {
                return;
            }

            var door = FrontDoor(h);
            if (door == null)
            {
                return;
            }

            var d = door.Value;
            var hall = h.Rooms.FirstOrDefault(r => r.Storey == 0 && r.Kind == "Hall");
            var storey = (float)VicKit.Storey;
            if (hall != null)
            {
                const float faceOff = 0.08f; // a wall's face stands this far before its line
                var width = Mathf.Min(1.0f, (hall.Box.width + 0.16f) * 0.55f);
                var dir = d.inward;
                var length = Mathf.Min(storey / Mathf.Tan(38f * Mathf.Deg2Rad), hall.Box.height - 1.5f);
                if (length < 2.6f)
                {
                    return;
                }

                var x = hall.Box.xMax - faceOff - (width * 0.5f); // against the east wall's face
                var z = dir.z > 0f ? hall.Box.yMin + 1.2f : hall.Box.yMax - 1.2f;
                h.Stair = new Stair { Foot = new Vector3(x, 0f, z), Dir = dir, Across = Vector3.left, Width = width, Length = length, Cells = hall.Cells };
                return;
            }

            if (!h.At.TryGetValue((0, d.i, d.j), out var room))
            {
                return;
            }

            // The wall opposite the door, along it, at the end away from the door.
            var back = room.Sides.First(s => Vector3.Dot(s.In, d.inward) < -0.5f);
            var len = Mathf.Clamp(back.Length - 0.5f, 0f, storey / Mathf.Tan(40f * Mathf.Deg2Rad));
            if (len < 2.6f)
            {
                return;
            }

            var doorAlong = back.Along(d.at);
            var w = 0.9f;
            var startAlong = doorAlong < back.Length * 0.5f ? back.Length - 0.2f - len : 0.2f;
            var climb = doorAlong < back.Length * 0.5f ? back.U : -back.U;
            var footAlong = climb == back.U ? startAlong : startAlong + len;
            var foot = back.At(footAlong, 0.08f + (w * 0.5f));
            h.Stair = new Stair { Foot = foot, Dir = climb, Across = back.In, Width = w, Length = len, Cells = room.Cells };
        }

        private static Rect StairRect(Stair s, float from, float to, float pad) =>
            Foot(s.Foot + (s.Dir * from) - (Vector3.Cross(Vector3.up, s.Dir) * ((s.Width * 0.5f) + pad)),
                 s.Foot + (s.Dir * to) + (Vector3.Cross(Vector3.up, s.Dir) * ((s.Width * 0.5f) + pad)));

        /// <summary>The well in a storey's floor over the flight below: past the first 1.4 m, so the stair has headroom.</summary>
        private static Rect? Well(House h, int storey)
        {
            if (h.Stair == null || storey < 1 || storey >= h.Spec.Floors)
            {
                return null;
            }

            return StairRect(h.Stair, 1.4f, h.Stair.Length, 0.02f);
        }

        private static void StairPieces(House h)
        {
            var s = h.Stair;
            if (s == null)
            {
                return;
            }

            var stair = Prefab("ViRoom", "Stair");
            var rail = Prefab("ViRoom", "Banister_3x1");
            if (stair == null)
            {
                return;
            }

            var b = Size(stair);
            var rot = Quaternion.LookRotation(s.Dir, Vector3.up);
            for (var storey = 0; storey < h.Spec.Floors; storey++)
            {
                var room = h.At[(storey, s.Cells.xMin, s.Cells.yMin)];
                var floor = room.Floor;
                if (storey < h.Spec.Floors - 1)
                {
                    var scale = new Vector3(s.Width / b.size.x, (float)VicKit.Storey / PackStairRise, s.Length / b.size.z);
                    var local = Vector3.Scale(scale, new Vector3(b.center.x, 0f, b.min.z));
                    var pivot = s.Foot + (Vector3.up * floor) - (rot * local);
                    Put(h.Root, stair, pivot, rot, scale, $"Stair_{storey}");
                    room.Taken.Add(StairRect(s, -1.0f, s.Length + 0.3f, 0.05f));
                }

                if (storey >= 1 && rail != null)
                {
                    // The balustrade round the well: along its open side and across its near end.
                    var rb = Size(rail);
                    var edgeAt = rb.max.z - RailOverhang;
                    void Balustrade(Vector3 a, Vector3 c, Vector3 towardWell)
                    {
                        var r = Quaternion.LookRotation(towardWell, Vector3.up);
                        var u = Vector3.Cross(Vector3.up, towardWell);
                        var start = Vector3.Dot(c - a, u) >= 0f ? a : c;
                        var len = Vector3.Distance(a, c);
                        var sc = new Vector3(len / rb.size.x, h.Scale, h.Scale);
                        var pv = start + (Vector3.up * floor) - (r * Vector3.Scale(sc, new Vector3(rb.min.x, 0f, edgeAt)));
                        Put(h.Root, rail, pv, r, sc, $"Balustrade_{storey}");
                    }

                    var half = s.Across * (s.Width * 0.5f);
                    Balustrade(s.Foot + (s.Dir * 1.4f) + half, s.Foot + (s.Dir * s.Length) + half, -s.Across);
                    Balustrade(s.Foot + (s.Dir * 1.4f) + half, s.Foot + (s.Dir * 1.4f) - half, s.Dir);
                    room.Taken.Add(StairRect(s, 1.0f, s.Length + 0.2f, 0.25f));
                }
            }
        }

        // ------------------------------------------------------------------------------------------------ shell

        /// <summary>A room's floor and ceiling (less the stair's well) and the linings of its outer walls between the windows.</summary>
        private static void Shell(House h, Room r)
        {
            var c = r.Cells;
            var outer = (float)VicKit.WallT;
            var segs = Enumerable.Range(0, 4).Select(i => SideSegments(h, r, i).ToList()).ToArray();
            float Trim(int side) => segs[side].Any(g => g.Neighbour == null) ? outer : 0f;
            var area = Rect.MinMaxRect((c.xMin * 2) + Trim(3), (c.yMin * 2) + Trim(0), (c.xMax * 2) - Trim(1), (c.yMax * 2) - Trim(2));

            var floorHole = Well(h, r.Storey);
            var ceilingHole = Well(h, r.Storey + 1);
            foreach (var part in Without(area, floorHole))
            {
                Tiles(h, Prefab("ViRoom", "Floor"), part, r.Floor, 1f);
            }

            if (r.Attic)
            {
                // An attic room stands under the flat only (UnderFlat), so all of it has the flat's ceiling.
                Tiles(h, Prefab("ViRoom", "Ceiling"), area, r.Floor + r.Height + 0.05f, 1f);
            }
            else
            {
                foreach (var part in Without(area, ceilingHole))
                {
                    Tiles(h, Prefab("ViRoom", "Ceiling"), part, r.Floor + r.Height, 1f);
                }
            }

            // Linings: the outer runs of each side, less the windows and doors; in the attic the knee wall.
            foreach (var s in r.Sides)
            {
                var runs = Runs(segs[s.Index], null);
                foreach (var (a, b) in runs)
                {
                    var (sa, sb) = AlongRun(s, a, b, r);
                    var gaps = r.Attic ? new List<(float A, float B)>() : s.Windows.Concat(s.Doors).OrderBy(w => w.A).ToList();
                    var from = sa;
                    foreach (var g in gaps.Where(g => g.B > sa && g.A < sb))
                    {
                        WallRun(h, r, s, from, Mathf.Max(from, g.A), r.Finish);
                        from = Mathf.Max(from, g.B);
                    }

                    WallRun(h, r, s, from, sb, r.Finish);
                }
            }
        }

        /// <summary>Plan runs (along the edge) of consecutive segments whose neighbour is <paramref name="beyond"/>.</summary>
        private static List<(float A, float B)> Runs(List<(float A, float B, Room Neighbour)> segs, Room beyond)
        {
            var runs = new List<(float, float)>();
            foreach (var g in segs.Where(g => g.Neighbour == beyond))
            {
                if (runs.Count > 0 && Mathf.Abs(runs[runs.Count - 1].Item2 - g.A) < 0.01f)
                {
                    runs[runs.Count - 1] = (runs[runs.Count - 1].Item1, g.B);
                }
                else
                {
                    runs.Add((g.A, g.B));
                }
            }

            return runs;
        }

        /// <summary>A plan run along a side's edge as along-coordinates on its line, out to the next walls' lines at the room's corners.</summary>
        private static (float A, float B) AlongRun(Side s, float a, float b, Room r)
        {
            var alongX = s.Index == 0 || s.Index == 2;
            var lo = alongX ? r.Cells.xMin * 2f : r.Cells.yMin * 2f;
            var hi = alongX ? r.Cells.xMax * 2f : r.Cells.yMax * 2f;
            var boxLo = alongX ? r.Box.xMin : r.Box.yMin;
            var boxHi = alongX ? r.Box.xMax : r.Box.yMax;
            var pa = Mathf.Abs(a - lo) < 0.01f ? boxLo : a;
            var pb = Mathf.Abs(b - hi) < 0.01f ? boxHi : b;
            Vector3 P(float v) => alongX ? new Vector3(v, 0f, s.Start.z) : new Vector3(s.Start.x, 0f, v);
            var x = s.Along(P(pa));
            var y = s.Along(P(pb));
            return (Mathf.Min(x, y), Mathf.Max(x, y));
        }

        /// <summary>The pack's wall blocks along a side from along a to b on its line, stretched to whole pieces; a doorway in one.</summary>
        /// <summary>
        /// A wall from a to b along the side, of whole pieces stretched to fit. With <paramref name="doorEnd"/> 0 or 1 a
        /// doorway piece of 2.4 m stands at that end, by the corner as Victorian rooms have their doors, so the rest of
        /// the wall keeps its length for furniture (G2); a run of 3.2 m or less is one doorway piece.
        /// </summary>
        private static (float Centre, float Half)? WallRun(House h, Room r, Side s, float a, float b, string finish, int doorEnd = -1)
        {
            var length = b - a;
            if (length < 0.25f)
            {
                return null;
            }

            var plain = Prefab("ViRoom", finish);
            var opening = Prefab("ViRoom", (finish == "Panel" ? "Dark" : finish) + "Doorway");
            if (plain == null)
            {
                return null;
            }

            var rot = Quaternion.LookRotation(s.In, Vector3.up);
            void Run(float from, float to, GameObject block)
            {
                if (to - from < 0.05f)
                {
                    return;
                }

                var n = Mathf.Max(1, Mathf.RoundToInt((to - from) / 3f));
                var piece = (to - from) / n;
                for (var k = 0; k < n; k++)
                {
                    Put(h.Root, block, s.At(from + (k * piece), 0f) + (Vector3.up * r.Floor), rot, new Vector3(piece / 3f, r.Height / PackStorey, 1f), null);
                }
            }

            if (doorEnd < 0 || opening == null)
            {
                Run(a, b, plain);
                return null;
            }

            var width = length <= 3.2f ? length : 2.4f;
            var d0 = doorEnd == 0 ? a : b - width;
            Run(doorEnd == 0 ? a + width : a, doorEnd == 0 ? b : b - width, plain);
            Run(d0, d0 + width, opening);
            return (d0 + (width * 0.5f), 0.675f * width / 3f * 1.2f);
        }

        /// <summary>A rectangle less one hole: up to four rectangles round it.</summary>
        private static IEnumerable<Rect> Without(Rect area, Rect? hole)
        {
            if (hole == null || !hole.Value.Overlaps(area))
            {
                yield return area;
                yield break;
            }

            var w = hole.Value;
            var x0 = Mathf.Max(area.xMin, w.xMin);
            var x1 = Mathf.Min(area.xMax, w.xMax);
            var z0 = Mathf.Max(area.yMin, w.yMin);
            var z1 = Mathf.Min(area.yMax, w.yMax);
            var parts = new[]
            {
                Rect.MinMaxRect(area.xMin, area.yMin, area.xMax, z0),
                Rect.MinMaxRect(area.xMin, z1, area.xMax, area.yMax),
                Rect.MinMaxRect(area.xMin, z0, x0, z1),
                Rect.MinMaxRect(x1, z0, area.xMax, z1),
            };
            foreach (var p in parts.Where(p => p.width > 0.05f && p.height > 0.05f))
            {
                yield return p;
            }
        }

        /// <summary>The pack's 3 m floor or ceiling blocks over a rectangle, stretched to whole tiles.</summary>
        private static void Tiles(House h, GameObject tile, Rect area, float y, float lift)
        {
            if (tile == null || area.width < 0.05f || area.height < 0.05f)
            {
                return;
            }

            var nx = Mathf.Max(1, Mathf.CeilToInt((area.width / 3f) - 0.01f));
            var nz = Mathf.Max(1, Mathf.CeilToInt((area.height / 3f) - 0.01f));
            var sx = area.width / (3f * nx);
            var sz = area.height / (3f * nz);
            for (var i = 0; i < nx; i++)
            {
                for (var k = 0; k < nz; k++)
                {
                    var pivot = new Vector3(area.xMin + (i * 3f * sx), y, area.yMin + (k * 3f * sz));
                    Put(h.Root, tile, pivot, Quaternion.identity, new Vector3(sx, lift, sz), null);
                }
            }
        }

        /// <summary>
        /// Which rooms of a storey a doorway joins: each room opens once, to the hall or landing where it touches it, else
        /// to the neighbour nearer the hall (a tree from the hall, the street door's room, or the first room, by the
        /// longest shared wall first), as Victorian rooms open from the hall and keep their other walls for furniture.
        /// </summary>
        private static HashSet<(int, int)> DoorTree(House h)
        {
            var tree = new HashSet<(int, int)>();
            float Shared(Room a, Room b) => a.Sides.Sum(s => Runs(SideSegments(h, a, s.Index).ToList(), b).Sum(run => run.B - run.A));
            foreach (var storey in h.Rooms.GroupBy(q => q.Storey))
            {
                var rooms = storey.ToList();
                var start = rooms.FirstOrDefault(q => q.Kind == "Hall" || q.Kind == "Landing") ?? rooms.FirstOrDefault(q => q.StreetDoor) ?? rooms[0];
                var reached = new HashSet<Room> { start };
                var queue = new Queue<Room>();
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    var q = queue.Dequeue();
                    foreach (var (n, _) in rooms.Where(n => !reached.Contains(n)).Select(n => (n, Shared(q, n))).Where(p => p.Item2 > 1.5f).OrderByDescending(p => p.Item2))
                    {
                        reached.Add(n);
                        tree.Add((Mathf.Min(q.Index, n.Index), Mathf.Max(q.Index, n.Index)));
                        queue.Enqueue(n);
                    }
                }
            }

            return tree;
        }

        /// <summary>
        /// One wall between two rooms on each shared run, built once, on the line 0.03 m into the lower-numbered room so its
        /// body is centred on the edge; a doorway by its corner where <see cref="DoorTree"/> joins the two rooms, on their
        /// longest shared run. Its ends stop at the outer walls' inner face.
        /// </summary>
        private static void Partitions(House h)
        {
            var tree = DoorTree(h);
            foreach (var r in h.Rooms)
            {
                foreach (var s in r.Sides)
                {
                    var segs = SideSegments(h, r, s.Index).ToList();
                    foreach (var other in segs.Select(g => g.Neighbour).Where(n => n != null && n.Index > r.Index).Distinct())
                    {
                        var runs = Runs(segs, other);
                        var doorRun = tree.Contains((r.Index, other.Index)) ? runs.OrderByDescending(run => run.B - run.A).First() : (A: float.NaN, B: float.NaN);
                        foreach (var (a, b) in runs)
                        {
                            var alongX = s.Index == 0 || s.Index == 2;
                            float Trim(float end, float dir)
                            {
                                // An end on the outline stops at the outer wall's inner face; in the attic, at the flat's
                                // edge, it stops at the knee wall's back, since a partition carried to the outer wall
                                // stood out through the slope and the lucarnes (seen 2026-11-08).
                                var p = end + (dir * 0.5f);
                                var edgeCell = Mathf.FloorToInt(s.Edge / 2f);
                                var cellA = alongX ? (Mathf.FloorToInt(p / 2f), edgeCell) : (edgeCell, Mathf.FloorToInt(p / 2f));
                                var cellB = alongX ? (Mathf.FloorToInt(p / 2f), edgeCell - 1) : (edgeCell - 1, Mathf.FloorToInt(p / 2f));
                                var outside = !h.At.ContainsKey((r.Storey, cellA.Item1, cellA.Item2)) || !h.At.ContainsKey((r.Storey, cellB.Item1, cellB.Item2));
                                var wall = r.Attic ? KneeInset : (float)VicKit.WallT;
                                return outside ? end - (dir * (wall + 0.02f)) : end;
                            }

                            var pa = Trim(a, -1f);
                            var pb = Trim(b, 1f);
                            // The partition's own line: 0.03 m into room r, measured from the edge, along r's side.
                            var line = new Side { Index = s.Index, In = s.In, U = s.U, Edge = s.Edge };
                            var origin = alongX ? new Vector3(0f, 0f, s.Edge) : new Vector3(s.Edge, 0f, 0f);
                            line.Start = origin + (s.In * InnerLine);
                            Vector3 P(float v) => alongX ? new Vector3(v, 0f, s.Edge) : new Vector3(s.Edge, 0f, v);
                            var x = Vector3.Dot(P(pa) - origin, s.U);
                            var y = Vector3.Dot(P(pb) - origin, s.U);
                            // The doorway goes to the end nearer the hall, or without one nearer the street door.
                            var hall = h.Rooms.FirstOrDefault(q => q.Storey == r.Storey && (q.Kind == "Hall" || q.Kind == "Landing"));
                            var front = FrontDoor(h);
                            var toward = hall != null ? new Vector3(hall.Box.center.x, 0f, hall.Box.center.y)
                                : front.HasValue ? new Vector3(front.Value.at.x, 0f, front.Value.at.z) : Vector3.zero;
                            float Score(Vector3 p) => Vector3.Distance(p, toward) + (0.01f * p.z);
                            var end = Score(line.At(Mathf.Min(x, y), 0f)) <= Score(line.At(Mathf.Max(x, y), 0f)) ? 0 : 1;
                            var door = WallRun(h, r, line, Mathf.Min(x, y), Mathf.Max(x, y), r.Finish, a == doorRun.A && b == doorRun.B && b - a > 1.5f ? end : -1);
                            if (door.HasValue)
                            {
                                var mid = line.At(door.Value.Centre, 0f);
                                MarkDoorway(r, mid, 0.8f);
                                MarkDoorway(other, mid, 0.8f);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>A doorway in a room's wall at a plan point on its edge: nothing stands within 0.8 m of it or 1 m into the room.</summary>
        private static void MarkDoorway(Room r, Vector3 at, float half)
        {
            foreach (var s in r.Sides)
            {
                var alongX = s.Index == 0 || s.Index == 2;
                if (Mathf.Abs((alongX ? at.z : at.x) - s.Edge) > 0.05f)
                {
                    continue;
                }

                var c = s.Along(at);
                s.Doors.Add((c - half, c + half));
                r.Taken.Add(Strip(s, c - half, c + half, 1.0f));
            }
        }

        // ------------------------------------------------------------------------------------------------ furniture

        /// <summary>
        /// The places of rules §3 for each room kind, in short: the anchor centred on the wall the windows face (the back
        /// wall), its companions round it, a second group on a side wall, plants in the back corners, wall lamps.
        /// </summary>
        private static void Furnish(House h, Room r)
        {
            var back = Back(r);
            // The other plain walls, those beside the anchor's wall first, then by their clear length.
            var sides = r.Sides.Where(s => s != back && !s.Windows.Any())
                .OrderByDescending(s => Mathf.Abs(Vector3.Dot(s.In, back.In)) < 0.5f ? 1 : 0).ThenByDescending(Clear).ToList();
            var rich = h.Wealth == "Rich";
            var poor = h.Wealth == "Poor";
            switch (r.Kind)
            {
                case "Parlour":
                case "Drawing":
                    var hearth = Hearth(h, r, back);
                    Sit(h, r, sides.FirstOrDefault(s => s != hearth) ?? back, r.Kind == "Drawing" || rich, hearth);

                    Corners(h, r, back, rich ? "Palm" : "Plant", "Plant");
                    if (r.Kind == "Drawing" && sides.Count > 1)
                    {
                        Against(h, r, sides[1], "CupboardGlass", Mid(sides[1]), 1.5f, out _);
                    }

                    break;
                case "Dining":
                    if (Anchor(h, r, back, rich ? "SideboardSet" : "Sideboard", out var board, out var boardWall))
                    {
                        Picture(h, r, boardWall, board);
                    }

                    Table(h, r, rich ? "DiningTable" : "TableB", rich ? 3 : 1);
                    Corners(h, r, back, "Plant", "PlantB");
                    break;
                case "Study":
                    if (!Pair(h, r, back, rich ? "Bookcase" : "BookcaseLow"))
                    {
                        Anchor(h, r, back, "BookcaseLow", out _, out _);
                    }

                    if (sides.Count > 0 && Against(h, r, sides[0], "Desk", WindowEnd(r, sides[0], 1.4f), 1.5f, out var desk))
                    {
                        Seat(h, r, sides[0], desk, "Chair");
                    }

                    Corners(h, r, back, "StandLamp", null);
                    break;
                case "Kitchen":
                case "Living":
                    Hearth(h, r, back, seats: false);
                    Table(h, r, "KitchenTable", 1);
                    if (r.Kind == "Living")
                    {
                        Anchor(h, r, sides.Count > 0 ? sides[0] : back, "Bed", out _, out _);
                    }
                    else if (sides.Count > 0)
                    {
                        Against(h, r, sides[0], "Dresser", Mid(sides[0]), 1.5f, out _);
                    }

                    if (sides.Count > 1)
                    {
                        Against(h, r, sides[1], poor ? "Shelves" : "Dresser", Mid(sides[1]), 1.5f, out _);
                    }

                    Corners(h, r, back, "Bucket", null);
                    break;
                case "Scullery":
                    Against(h, r, back, "BasinSet", Mid(back), 1.5f, out _);
                    if (sides.Count > 0)
                    {
                        Against(h, r, sides[0], "Shelves", Mid(sides[0]), 1.5f, out _);
                    }

                    Corners(h, r, back, "Bucket", "Box");
                    break;
                case "Bedroom":
                    if (Anchor(h, r, back, "Bed", out var bed, out var bedWall))
                    {
                        var stand = poor ? "NightstandPoor" : "Nightstand";
                        Flank(h, r, bedWall, bed, stand, poor ? 1 : 2);
                        Picture(h, r, bedWall, bed);
                        if (rich)
                        {
                            Free(h, r, bedWall.At((bed.A0 + bed.A1) * 0.5f, bed.Depth + 0.5f), bedWall.In, "Rug", out _);
                        }
                    }
                    else
                    {
                        // The pack's one bed is 3.1 m long with its steps; a small room keeps its dresser and chair instead.
                        Anchor(h, r, back, "Dresser", out _, out _);
                    }

                    if (sides.Count > 0)
                    {
                        Against(h, r, sides[0], rich ? "Sideboard" : "Dresser", Mid(sides[0]), 1.5f, out _);
                    }

                    if (sides.Count > 1)
                    {
                        Against(h, r, sides[1], "Chair", WindowEnd(r, sides[1], 0.6f), 0.8f, out _);
                    }

                    break;
                case "Bathroom":
                case "Washroom":
                    Anchor(h, r, back, poor ? "Bucket" : "BathSet", out _, out _);
                    if (sides.Count > 0)
                    {
                        Against(h, r, sides[0], "Toilet", sides[0].Length * 0.3f, 1f, out _);
                        if (Against(h, r, sides[0], "BasinSet", sides[0].Length * 0.7f, 1f, out var basin))
                        {
                            Hang(h, r, sides[0], "MirrorSmall", (basin.A0 + basin.A1) * 0.5f, basin.Top + 0.25f);
                        }
                    }

                    break;
                case "Hall":
                case "Landing":
                    var wall = r.Sides.Where(s => !s.Exterior || s.Windows.Count == 0).OrderByDescending(s => s.Length)
                        .FirstOrDefault(s => Vector3.Dot(s.In, Vector3.left) < 0.5f) ?? r.Sides[3];
                    if (r.Kind == "Hall" && Against(h, r, wall, "HallTable", wall.Length * 0.35f, 1.5f, out var table))
                    {
                        Hang(h, r, wall, "Mirror", (table.A0 + table.A1) * 0.5f, table.Top + 0.3f);
                    }

                    Against(h, r, wall, r.Kind == "Hall" ? "CoatStand" : "Plant", wall.Length * 0.12f, 1f, out _);
                    if (rich)
                    {
                        Against(h, r, wall, "Clock", wall.Length * 0.75f, 1f, out _);
                    }

                    break;
                case "Servant":
                    if (Anchor(h, r, back, "Bed", out var cot, out var cotWall))
                    {
                        Flank(h, r, cotWall, cot, "NightstandPoor", 1);
                    }

                    Corners(h, r, back, "Box", "Chair");
                    break;
            }

            // Light follows use (G9): a storey-high room takes wall lamps, one on each side wall clear of tall pieces.
            foreach (var s in sides.Take(2))
            {
                Hang(h, r, s, "WallLamp", s.Length * 0.5f, 1.85f * h.Scale);
            }
        }

        /// <summary>
        /// The wall the room is arranged on: of the sides without a window, the one with the longest stretch clear of
        /// doorways, the side the windows look at winning a tie within 0.5 m; with no plain side, the side the windows look at.
        /// </summary>
        private static Side Back(Room r)
        {
            var lit = r.Sides.OrderByDescending(s => s.Windows.Sum(w => w.B - w.A)).First();
            var opposite = r.Sides.First(s => Vector3.Dot(s.In, lit.In) < -0.5f);
            return r.Sides.Where(s => s.Windows.Count == 0)
                .OrderByDescending(s => Clear(s) + (s == opposite ? 0.5f : 0f)).FirstOrDefault() ?? opposite;
        }

        /// <summary>The longest stretch of a side clear of its doorways and windows, m.</summary>
        private static float Clear(Side s)
        {
            var best = 0f;
            var from = 0f;
            foreach (var (a, b) in s.Windows.Concat(s.Doors).OrderBy(o => o.A))
            {
                best = Mathf.Max(best, a - from);
                from = Mathf.Max(from, b);
            }

            return Mathf.Max(best, s.Length - from);
        }

        /// <summary>The middle of a side's longest stretch clear of doorways and windows: where its anchor is centred (G2, G3).</summary>
        private static float Mid(Side s)
        {
            var best = (A: 0f, B: 0f);
            var from = 0f;
            foreach (var (a, b) in s.Windows.Concat(s.Doors).OrderBy(o => o.A).Append((s.Length, s.Length)))
            {
                if (a - from > best.B - best.A)
                {
                    best = (from, a);
                }

                from = Mathf.Max(from, b);
            }

            return (best.A + best.B) * 0.5f;
        }

        /// <summary>Along a side wall, a point near the end where the windows are (a desk takes the light from the side).</summary>
        private static float WindowEnd(Room r, Side s, float off)
        {
            var lit = r.Sides.OrderByDescending(x => x.Windows.Sum(w => w.B - w.A)).First();
            var nearStart = Vector3.Dot(s.U, lit.In) > 0f;
            return nearStart ? off : s.Length - off;
        }

        /// <summary>
        /// A piece the room is arranged round (a fireplace, a bed, a sideboard), at the clear middle of its wall, or
        /// failing that on another side, plain sides first, anywhere along it; <paramref name="on"/> is the wall it took.
        /// </summary>
        private static bool Anchor(House h, Room r, Side first, string part, out Placed placed, out Side on, Side except = null)
        {
            var walls = new[] { first }.Concat(r.Sides.Where(x => x != first).OrderBy(x => x.Windows.Count > 0 ? 1 : 0).ThenByDescending(Clear))
                .Where(x => x != null && x != except);
            foreach (var s in walls)
            {
                if (Against(h, r, s, part, Mid(s), s.Length * 0.5f, out placed, note: false))
                {
                    on = s;
                    return true;
                }
            }

            r.Missed.Add(part);
            placed = default;
            on = null;
            return false;
        }

        /// <summary>A fireplace centred on the wall, an armchair each side facing in, a rug before it (rules §3, Hearth); returns its wall.</summary>
        private static Side Hearth(House h, Room r, Side s, bool seats = true)
        {
            if (!Anchor(h, r, s, "Fireplace", out var f, out s))
            {
                return null;
            }

            if (!seats)
            {
                return s;
            }

            var mid = (f.A0 + f.A1) * 0.5f;
            foreach (var sign in new[] { -1f, 1f })
            {
                var along = sign < 0f ? f.A0 - 0.5f : f.A1 + 0.5f;
                Free(h, r, s.At(along, f.Depth * 0.6f + 0.35f), s.U * -sign, "Armchair", out _);
            }

            Free(h, r, s.At(mid, f.Depth + 0.75f), s.In, "Rug", out _);
            return s;
        }

        /// <summary>A settee against the wall, a round table before it, a lamp beside it (rules §3, Sit); never on the hearth's wall.</summary>
        private static void Sit(House h, Room r, Side s, bool rich, Side hearth)
        {
            if (!Anchor(h, r, s, rich ? "Sofa" : "Settee", out var settee, out s, hearth))
            {
                return;
            }

            Free(h, r, s.At((settee.A0 + settee.A1) * 0.5f, settee.Depth + 0.55f), s.In, "TableRoundSet", out _);
            Against(h, r, s, "StandLamp", settee.A1 + 0.35f, 0.3f, out _);
            Picture(h, r, s, settee);
        }

        /// <summary>Two pieces side by side, centred on the wall (rules G3: tall pieces in pairs); false when neither stands.</summary>
        private static bool Pair(House h, Room r, Side s, string part)
        {
            var b = Size(Prefab("ViFurniture", part));
            var w = b.size.x * Fit(h, r, b);
            var mid = Mid(s);
            if (Clear(s) > (2f * w) + 0.3f)
            {
                var one = Against(h, r, s, part, mid - (w * 0.5f) - 0.03f, 0.2f, out _);
                return Against(h, r, s, part, mid + (w * 0.5f) + 0.03f, 0.2f, out _) || one;
            }

            return Against(h, r, s, part, mid, 1f, out _);
        }

        /// <summary>A companion each side of an anchor against the same wall (a nightstand by a bed).</summary>
        private static void Flank(House h, Room r, Side s, Placed anchor, string part, int count)
        {
            var b = Size(Prefab("ViFurniture", part));
            var w = b.size.x * Fit(h, r, b);
            Against(h, r, s, part, anchor.A1 + (w * 0.5f) + 0.05f, 0.1f, out _);
            if (count > 1)
            {
                Against(h, r, s, part, anchor.A0 - (w * 0.5f) - 0.05f, 0.1f, out _);
            }
        }

        /// <summary>A picture over a piece against the wall, if the wall is free there.</summary>
        private static void Picture(House h, Room r, Side s, Placed under)
        {
            var key = $"Picture{1 + h.Rng.Next(8)}";
            Hang(h, r, s, key, (under.A0 + under.A1) * 0.5f, Mathf.Max(under.Top + 0.2f, 1.3f * h.Scale));
        }

        /// <summary>Tall things in the wall's two corners, clear of windows and doors.</summary>
        private static void Corners(House h, Room r, Side s, string first, string second)
        {
            foreach (var (part, end) in new[] { (first, 0), (second, 1) })
            {
                if (part == null)
                {
                    continue;
                }

                var b = Size(Prefab("ViFurniture", part));
                var w = b.size.x * Fit(h, r, b);
                Against(h, r, s, part, end == 0 ? (w * 0.5f) + 0.05f : s.Length - (w * 0.5f) - 0.05f, 0.4f, out _);
            }
        }

        /// <summary>A table in the room's middle, its long side along the room's long side, chairs at its sides facing it.</summary>
        private static void Table(House h, Room r, string part, int perSide)
        {
            var centre = new Vector3(r.Box.center.x, 0f, r.Box.center.y);
            var facing = r.Box.width >= r.Box.height ? Vector3.forward : Vector3.right;
            if (!Free(h, r, centre, facing, part, out var t))
            {
                return;
            }

            var length = r.Box.width >= r.Box.height ? t.Foot.width : t.Foot.height;
            var depth = r.Box.width >= r.Box.height ? t.Foot.height : t.Foot.width;
            var u = Vector3.Cross(Vector3.up, facing);
            for (var k = 0; k < perSide; k++)
            {
                var along = perSide == 1 ? 0f : ((k / (perSide - 1f)) - 0.5f) * (length - 0.8f);
                foreach (var sign in new[] { -1f, 1f })
                {
                    var at = centre + (u * along) + (facing * sign * ((depth * 0.5f) + 0.3f));
                    Free(h, r, at, -facing * sign, "Chair", out _);
                }
            }
        }

        /// <summary>A chair before a piece against the wall, facing it (a desk's chair).</summary>
        private static void Seat(House h, Room r, Side s, Placed at, string part)
        {
            Free(h, r, s.At((at.A0 + at.A1) * 0.5f, at.Depth + 0.3f), -s.In, part, out _);
        }

        // ------------------------------------------------------------------------------------------------ placing

        /// <summary>The furniture's scale in kit units: its own size, shrunk to fit under the ceiling.</summary>
        private static float Fit(House h, Room r, Bounds b)
        {
            var top = b.max.y * h.Scale;
            var room = r.Height - 0.12f;
            return top > room ? room / b.max.y : h.Scale;
        }

        /// <summary>
        /// A piece with its back on the wall's line, centred at along (or the nearest free spot within spread), in the
        /// room, clear of what is placed, of doorways, and, when it is taller than the sill, of the windows.
        /// </summary>
        private static bool Against(House h, Room r, Side s, string part, float along, float spread, out Placed placed, bool note = true)
        {
            placed = default;
            var prefab = Prefab("ViFurniture", part);
            if (prefab == null)
            {
                return false;
            }

            var b = Size(prefab);
            var k = Fit(h, r, b);
            var rot = Quaternion.LookRotation(s.In, Vector3.up);
            for (var step = 0f; step <= spread + 1e-3f; step += 0.25f)
            {
                foreach (var sign in step == 0f ? new[] { 1f } : new[] { -1f, 1f })
                {
                    var at = along + (sign * step);
                    var pivot = s.At(at - (k * b.center.x), 0f) + (Vector3.up * (r.Floor - (k * b.min.y)));
                    if (Try(h, r, prefab, b, k, pivot, rot, out placed))
                    {
                        placed.A0 = at - (k * b.size.x * 0.5f);
                        placed.A1 = at + (k * b.size.x * 0.5f);
                        placed.Depth = k * b.max.z;
                        s.Standing.Add((placed.A0, placed.A1, placed.Top));
                        return true;
                    }
                }
            }

            if (note)
            {
                r.Missed.Add(part);
            }

            return false;
        }

        /// <summary>A piece standing free, the middle of what is seen at centre, its front toward facing.</summary>
        private static bool Free(House h, Room r, Vector3 centre, Vector3 facing, string part, out Placed placed)
        {
            placed = default;
            var prefab = Prefab("ViFurniture", part);
            if (prefab == null)
            {
                return false;
            }

            var b = Size(prefab);
            var k = Fit(h, r, b);
            var rot = Quaternion.LookRotation(facing, Vector3.up);
            var pivot = centre - (rot * new Vector3(k * b.center.x, 0f, k * b.center.z)) + (Vector3.up * (r.Floor - (k * b.min.y)));
            // A rug lies under its group (G4): it takes no room and is kept from nothing but the walls.
            var flat = part.StartsWith("Rug", StringComparison.Ordinal) || part.StartsWith("Carpet", StringComparison.Ordinal);
            if (Try(h, r, prefab, b, k, pivot, rot, out placed, flat))
            {
                return true;
            }

            r.Missed.Add(part);
            return false;
        }

        private static bool Try(House h, Room r, GameObject prefab, Bounds b, float k, Vector3 pivot, Quaternion rot, out Placed placed, bool flat = false)
        {
            placed = default;
            var corners = new[] { new Vector3(b.min.x, 0f, b.min.z), new Vector3(b.max.x, 0f, b.min.z), new Vector3(b.min.x, 0f, b.max.z), new Vector3(b.max.x, 0f, b.max.z) }
                .Select(c => pivot + (rot * (c * k))).ToList();
            var foot = Rect.MinMaxRect(corners.Min(c => c.x), corners.Min(c => c.z), corners.Max(c => c.x), corners.Max(c => c.z));
            var top = b.max.y * k;
            var box = r.Box;
            const float give = 0.03f;
            if (foot.xMin < box.xMin - give || foot.xMax > box.xMax + give || foot.yMin < box.yMin - give || foot.yMax > box.yMax + give)
            {
                r.Why["wall"] = r.Why.GetValueOrDefault("wall") + 1;
                return false;
            }

            var inner = Rect.MinMaxRect(foot.xMin + give, foot.yMin + give, foot.xMax - give, foot.yMax - give);
            if (!flat && r.Taken.Any(t => t.Overlaps(inner)))
            {
                r.Why["taken"] = r.Why.GetValueOrDefault("taken") + 1;
                return false;
            }

            if (!flat && top > SillTop && r.BeforeWindows.Any(t => t.Overlaps(inner)))
            {
                r.Why["window"] = r.Why.GetValueOrDefault("window") + 1;
                return false;
            }

            Put(h.Root, prefab, pivot, rot, Vector3.one * k, null);
            r.Pieces.Add(prefab.name.Replace("Kit_ViFurniture_", ""));
            if (!flat)
            {
                r.Taken.Add(foot);
            }

            placed = new Placed { Foot = foot, Top = top };
            return true;
        }

        /// <summary>A flat piece hung on the wall's face, its bottom at the given height above the floor, where nothing tall stands.</summary>
        private static void Hang(House h, Room r, Side s, string part, float along, float bottom)
        {
            var prefab = Prefab("ViFurniture", part);
            if (prefab == null)
            {
                return;
            }

            var b = Size(prefab);
            var k = h.Scale;
            var a0 = along - (k * b.size.x * 0.5f);
            var a1 = along + (k * b.size.x * 0.5f);
            if (a0 < 0.1f || a1 > s.Length - 0.1f || s.Open(a0 - 0.1f, a1 + 0.1f) || bottom + (k * b.size.y) > r.Height - 0.1f)
            {
                return;
            }

            if (s.Standing.Any(t => a0 < t.B && a1 > t.A && t.Top > bottom - 0.05f))
            {
                return;
            }

            var rot = Quaternion.LookRotation(s.In, Vector3.up);
            var pivot = s.At(along - (k * b.center.x), 0f) + (Vector3.up * (r.Floor + bottom - (k * b.min.y)));
            Put(h.Root, prefab, pivot, rot, Vector3.one * k, null);
            s.Standing.Add((a0, a1, bottom + (k * b.size.y)));
        }

        // ------------------------------------------------------------------------------------------------ blocks

        /// <summary>A kit block of the pack by family and part ("ViFurniture", "Bed"), whatever its size suffix; null if missing.</summary>
        private static GameObject Prefab(string family, string part)
        {
            var id = family + "/" + part;
            if (Prefabs.TryGetValue(id, out var cached))
            {
                return cached;
            }

            var folder = family == "ViRoom" ? RoomFolder : FurnitureFolder;
            var exact = $"Kit_{family}_{part}";
            var prefix = exact + "_";
            var path = AssetDatabase.FindAssets("t:Prefab", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileNameWithoutExtension(p) == exact || Path.GetFileNameWithoutExtension(p).StartsWith(prefix, StringComparison.Ordinal))
                .OrderBy(p => p.Length)
                .FirstOrDefault();
            var prefab = path == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[VicKit] rooms: no block {prefix}* in {folder}.");
            }

            Prefabs[id] = prefab;
            return prefab;
        }

        /// <summary>What a block shows, in its own space, measured once.</summary>
        private static Bounds Size(GameObject prefab)
        {
            if (prefab == null)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            if (Sizes.TryGetValue(prefab.name, out var b))
            {
                return b;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            go.transform.localScale = Vector3.one;
            var first = true;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled && (r is MeshRenderer || r is SkinnedMeshRenderer)))
            {
                if (first)
                {
                    b = r.bounds;
                    first = false;
                }
                else
                {
                    b.Encapsulate(r.bounds);
                }
            }

            Object.DestroyImmediate(go);
            Sizes[prefab.name] = b;
            return b;
        }

        /// <summary>Pieces placed since the counts were last cleared, and parts that needed a holder (see <see cref="Put"/>).</summary>
        public static int PiecesPut, PartsHeld;

        /// <summary>
        /// A block placed as dressing that only draws (Plans/VEILMOUTH_REFERENCE_PLAN.md R1.3): one game object for each
        /// mesh the block shows, with its mesh, materials and layer, and none of the block's wrapper, colliders, body,
        /// lights or scripts, which nobody behind a window would use. The city loads them as plain renderers, not as
        /// tens of thousands of components switched off. A block with a LOD group gives its first level only. A part
        /// whose place is no single transform (a stretch over a turned part would shear) keeps a holder with the
        /// placement and the part beneath it.
        /// </summary>
        private static void Put(Transform parent, GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale, string name)
        {
            PiecesPut++;
            var place = Matrix4x4.TRS(position, rotation, scale);
            Transform holder = null;
            foreach (var part in Parts(prefab))
            {
                var go = new GameObject(name ?? part.Name);
                go.layer = part.Layer;
                go.AddComponent<MeshFilter>().sharedMesh = part.Mesh;
                go.AddComponent<MeshRenderer>().sharedMaterials = part.Materials;
                if (Exact(place * part.Local, out var p, out var q, out var k))
                {
                    go.transform.SetParent(parent, false);
                }
                else
                {
                    if (holder == null)
                    {
                        holder = new GameObject(name ?? prefab.name).transform;
                        holder.SetParent(parent, false);
                        holder.localPosition = position;
                        holder.localRotation = rotation;
                        holder.localScale = scale;
                    }

                    PartsHeld++;
                    Exact(part.Local, out p, out q, out k);
                    go.transform.SetParent(holder, false);
                }

                go.transform.localPosition = p;
                go.transform.localRotation = q;
                go.transform.localScale = k;
            }
        }

        private static readonly Dictionary<GameObject, (string Name, Mesh Mesh, Material[] Materials, Matrix4x4 Local, int Layer)[]> PartCache =
            new Dictionary<GameObject, (string, Mesh, Material[], Matrix4x4, int)[]>();

        /// <summary>The meshes a block shows, each in the block's own space: enabled, active, and of a LOD group's first level only.</summary>
        private static (string Name, Mesh Mesh, Material[] Materials, Matrix4x4 Local, int Layer)[] Parts(GameObject prefab)
        {
            if (PartCache.TryGetValue(prefab, out var cached) && cached.All(c => c.Mesh != null))
            {
                return cached;
            }

            var later = new HashSet<Renderer>(prefab.GetComponentsInChildren<LODGroup>(true)
                .SelectMany(g => g.GetLODs().Skip(1))
                .SelectMany(l => l.renderers)
                .Where(r => r != null));
            var toBlock = prefab.transform.worldToLocalMatrix;
            var parts = prefab.GetComponentsInChildren<MeshRenderer>(true)
                .Where(r => r.enabled && !later.Contains(r) && Active(r.transform, prefab.transform))
                .Select(r => (r, f: r.GetComponent<MeshFilter>()))
                .Where(x => x.f != null && x.f.sharedMesh != null)
                .Select(x => (x.r.gameObject.name, x.f.sharedMesh, x.r.sharedMaterials, toBlock * x.r.transform.localToWorldMatrix, x.r.gameObject.layer))
                .ToArray();
            PartCache[prefab] = parts;
            return parts;
        }

        private static bool Active(Transform t, Transform root)
        {
            for (; t != null; t = t == root ? null : t.parent)
            {
                if (!t.gameObject.activeSelf)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Splits <paramref name="m"/> into a transform's parts; false when they do not give it back (a shear).</summary>
        private static bool Exact(Matrix4x4 m, out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            // A mirrored part turns one axis inside out: split it with that axis flipped back, then flip its scale.
            var mirror = m.determinant < 0f;
            var n = m;
            if (mirror)
            {
                n.SetColumn(0, -m.GetColumn(0));
            }

            position = m.GetColumn(3);
            rotation = n.rotation;
            scale = n.lossyScale;
            if (mirror)
            {
                scale.x = -scale.x;
            }

            var back = Matrix4x4.TRS(position, rotation, scale);
            for (var i = 0; i < 16; i++)
            {
                if (Mathf.Abs(back[i] - m[i]) > 1e-4f)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Static for batching, lit by probes, no shadows, and switched off by distance (<see cref="Switch"/>).</summary>
        private static void Finish(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                if (r is MeshRenderer mesh)
                {
                    mesh.receiveGI = ReceiveGI.LightProbes;
                }

                r.lightProbeUsage = LightProbeUsage.BlendProbes;
                r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }

            Switch(root);
        }

        /// <summary>
        /// Gives built rooms their distance switch, with the box of every renderer under <paramref name="root"/> in the
        /// root's own space, and removes the LOD group that culled them by screen size before (it depended on the
        /// lodBias and on the room's centre, not its nearest point). Run on a fresh build and on a baked house alike.
        /// </summary>
        public static VicRoomsSwitch Switch(GameObject root)
        {
            var old = root.GetComponent<LODGroup>();
            if (old != null)
            {
                Object.DestroyImmediate(old, true);
            }

            var toRoot = root.transform.worldToLocalMatrix;
            var box = default(Bounds);
            var first = true;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var local = r.localBounds;
                var m = toRoot * r.transform.localToWorldMatrix;
                for (var corner = 0; corner < 8; corner++)
                {
                    var p = m.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1)));
                    if (first)
                    {
                        box = new Bounds(p, Vector3.zero);
                        first = false;
                    }
                    else
                    {
                        box.Encapsulate(p);
                    }
                }
            }

            var rooms = root.GetComponent<VicRoomsSwitch>();
            if (rooms == null)
            {
                rooms = root.AddComponent<VicRoomsSwitch>();
            }

            rooms.Box = box;
            return rooms;
        }
    }
}

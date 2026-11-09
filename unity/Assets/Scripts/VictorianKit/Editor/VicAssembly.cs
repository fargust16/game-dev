using System;
using System.Collections.Generic;
using System.Globalization;

namespace GameDev.VictorianKit.EditorTools
{
    /// <summary>A point in the kit's own frame: Blender metres, Z up, the street facade facing −Y.</summary>
    public readonly struct V3
    {
        public readonly double X, Y, Z;

        public V3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public V3 WithZ(double z) => new V3(X, Y, z);

        public static V3 operator +(V3 a, V3 b) => new V3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

    /// <summary>
    /// One placed module: the kit id, its pivot in the house's own frame (Blender metres) and its turn about Z in
    /// degrees, as a kit manifest writes it. <see cref="Stucco"/> paints the wall's brick slot with stucco
    /// (СБОРКА.md, Материалы: the mesh stays the brick wall).
    /// </summary>
    public readonly struct VicPiece
    {
        public readonly string Id;
        public readonly double X, Y, Z, Yaw;
        public readonly bool Stucco;

        public VicPiece(string id, double x, double y, double z, double yaw, bool stucco)
        {
            Id = id;
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
            Stucco = stucco;
        }

        /// <summary>The yaw folded into (−180, 180], so equal turns compare equal.</summary>
        public double NormalYaw
        {
            get
            {
                var y = Math.Round(Yaw, 3) % 360.0;
                if (y <= -180.0)
                {
                    y += 360.0;
                }
                else if (y > 180.0)
                {
                    y -= 360.0;
                }

                return Math.Abs(y + 180.0) < 1e-6 ? 180.0 : y;
            }
        }

        /// <summary>Id, position to the manifest's 4 decimals, and the folded yaw: two equal placements share a key.</summary>
        public string Key
        {
            get
            {
                var inv = CultureInfo.InvariantCulture;
                string R(double v) => (Math.Round(v, 3) + 0.0).ToString("F3", inv);
                return $"{Id}|{R(X)}|{R(Y)}|{R(Z)}|{R(NormalYaw)}";
            }
        }
    }

    /// <summary>The kit's grid and heights (СБОРКА.md, Сетка; common.py), metres.</summary>
    public static class VicKit
    {
        public const double Module = 2.0;
        public const double Storey = 3.0;
        public const double WallT = 0.25;
        public const double Overhang = 0.30;
        public const double RoofLift = 0.45;
        public const double Foundation = 0.60;
        public const double CorniceH = 0.22;
        public const double CorniceTopDrop = 0.11;
        public const double WinSill = 0.85;
        public const double MansardInset = 2.0;
        public const double ChimneyTop = 1.56;

        /// <summary>Every slope, the mansard and the oriel cap rise at 42°.</summary>
        public static readonly double Tan = Math.Tan(42.0 * Math.PI / 180.0);

        public static double MansardRise => MansardInset * Tan;

        /// <summary>Half the span of a D4 or D6 roof: the depth it covers is twice this.</summary>
        public static double HalfSpan(int depth) => depth * 0.5;

        public static double FloorZ(int floor) => Foundation + (floor * Storey);

        public static double WallTop(int floors) => Foundation + (floors * Storey);

        /// <summary>The roof's spring: the frieze's top, where every slope's pivot sits.</summary>
        public static double Spring(int floors) => WallTop(floors) + RoofLift;

        public static double CorniceZ(int floors) => Spring(floors) - CorniceTopDrop - CorniceH;

        /// <summary>The eave line's height: the slope run out 0.30 m past the wall.</summary>
        public static double EaveZ(double spring, double tan) => spring - (Overhang * tan);

        public static (double x, double y) Rotated(double x, double y, double rot)
        {
            var c = Math.Cos(rot);
            var s = Math.Sin(rot);
            return ((c * x) - (s * y), (s * x) + (c * y));
        }

        public static V3 WorldPoint(V3 origin, double rot, double x, double y, double z)
        {
            var (rx, ry) = Rotated(x, y, rot);
            return new V3(origin.X + rx, origin.Y + ry, origin.Z + z);
        }
    }

    /// <summary>A house as a list of placed modules; the dressing helpers below append to it.</summary>
    public sealed class VicHouse
    {
        public readonly string Name;
        public readonly List<VicPiece> Pieces = new List<VicPiece>();

        public VicHouse(string name)
        {
            Name = name;
        }

        public void Drop(string id, V3 at, double rot, bool stucco = false) =>
            Pieces.Add(new VicPiece(id, at.X, at.Y, at.Z, rot * 180.0 / Math.PI, stucco));
    }

    /// <summary>
    /// The kit's wall dressing, ported from its Blender assembly (proof_houses.py, houses.py, roof_junctions.py) so a
    /// house placed here is placed as the kit places its own House U. Positions are in the house's frame, Blender
    /// metres; rotations in radians about Z.
    /// </summary>
    public static class VicDress
    {
        public const double HalfPi = Math.PI * 0.5;

        public static readonly Dictionary<string, string> Walls = new Dictionary<string, string>
        {
            { "plain", "SM_Vic_Wall_Plain_2m" },
            { "win_rect_s", "SM_Vic_Wall_Window_Rect_Single_2m" },
            { "win_arch_s", "SM_Vic_Wall_Window_Arch_Single_2m" },
            { "win_rect_d", "SM_Vic_Wall_Window_Rect_Double_2m" },
            { "win_arch_d", "SM_Vic_Wall_Window_Arch_Double_2m" },
            { "door_rect", "SM_Vic_Wall_Door_Rect_Single_2m" },
            { "door_arch", "SM_Vic_Wall_Door_Arch_Single_2m" },
            { "door_double", "SM_Vic_Wall_Door_Rect_Double_2m" },
            { "door_arch_double", "SM_Vic_Wall_Door_Arch_Double_2m" },
            { "bay", "SM_Vic_Wall_Bay_2m" },
        };

        /// <summary>Trim set, insert and the insert's height above the storey floor.</summary>
        private static readonly Dictionary<string, (string trim, string insert, double sill, bool hasSill)> Inserts =
            new Dictionary<string, (string, string, double, bool)>
            {
                { "win_rect_s", ("SM_Vic_Trim_Window_Rect_Single", "SM_Vic_Window_Rect_Single", VicKit.WinSill, true) },
                { "win_arch_s", ("SM_Vic_Trim_Window_Arch_Single", "SM_Vic_Window_Arch_Single", VicKit.WinSill, true) },
                { "win_rect_d", ("SM_Vic_Trim_Window_Rect_Double", "SM_Vic_Window_Rect_Double", VicKit.WinSill, true) },
                { "win_arch_d", ("SM_Vic_Trim_Window_Arch_Double", "SM_Vic_Window_Arch_Double", VicKit.WinSill, true) },
                { "door_rect", ("SM_Vic_Trim_Door_Rect_Single", "SM_Vic_Door_Rect_Single", 0.0, false) },
                { "door_arch", ("SM_Vic_Trim_Door_Arch_Single", "SM_Vic_Door_Arch_Single", 0.0, false) },
                { "door_double", ("SM_Vic_Trim_Door_Rect_Double", "SM_Vic_Door_Rect_Double", 0.0, false) },
                { "door_arch_double", ("SM_Vic_Trim_Door_Arch_Double", "SM_Vic_Door_Arch_Double", 0.0, false) },
            };

        public static bool IsDoor(string key) => key.StartsWith("door", StringComparison.Ordinal);

        public static bool IsWindow(string key) => key.StartsWith("win", StringComparison.Ordinal);

        /// <summary>A window or door's trim and leaf on a wall module at <paramref name="origin"/>.</summary>
        public static void Opening(VicHouse h, V3 origin, double rot, string key) => OpeningAt(h, origin, rot, key, 1.0, 0.0, 0.0);

        public static void OpeningAt(VicHouse h, V3 origin, double rot, string key, double x, double y, double extra)
        {
            var (trim, insert, sill, hasSill) = Inserts[key];
            var at = VicKit.WorldPoint(origin, rot, x, y, sill);
            var face = rot + extra;
            h.Drop(trim + "_Surround", at, face);
            h.Drop(trim + "_Lintel", at, face);
            if (hasSill)
            {
                h.Drop(trim + "_Sill", at, face);
            }

            h.Drop(insert, at, face);
        }

        /// <summary>Steps and one pair of rails at a ground-floor door: iron, or stone.</summary>
        public static void Entry(VicHouse h, V3 origin, double rot, string key, bool stone)
        {
            var at = VicKit.WorldPoint(origin, rot, 1.0, 0.0, 0.0);
            var wide = key == "door_double" || key == "door_arch_double";
            var rail = stone ? (wide ? "SM_Vic_Rail_Stone_Double" : "SM_Vic_Rail_Stone_Single") : (wide ? "SM_Vic_Rail_Double" : "SM_Vic_Rail_Single");
            h.Drop(wide ? "SM_Vic_Steps_Double" : "SM_Vic_Steps_Single", at, rot);
            h.Drop(rail + "_L", at, rot);
            h.Drop(rail + "_R", at, rot);
        }

        /// <summary>A 2 m wall module with its foundation, string course, cornice and frieze by storey, and its opening.</summary>
        public static void DressWall(VicHouse h, V3 loc, double rot, string key, int floor, int floors, bool stoneRail = false, bool stucco = false)
        {
            h.Drop(Walls[key], loc, rot, stucco);
            Bands(h, loc, rot, floor, floors, "_Straight_2m", "SM_Vic_Frieze_Straight_2m");
            if (key == "bay")
            {
                Bay(h, loc, rot);
            }
            else if (Inserts.ContainsKey(key))
            {
                Opening(h, loc, rot, key);
            }

            if (floor == 0 && IsDoor(key))
            {
                Entry(h, loc, rot, key, stoneRail);
            }
        }

        /// <summary>A 1 m plain wall with its bands (houses._plain_meter).</summary>
        public static void PlainMeter(VicHouse h, V3 loc, double rot, int floor, int floors, bool stucco = false)
        {
            h.Drop("SM_Vic_Wall_Plain_1m", loc, rot, stucco);
            Bands(h, loc, rot, floor, floors, "_Straight_1m", "SM_Vic_Frieze_Straight_1m");
        }

        private static void Bands(VicHouse h, V3 loc, double rot, int floor, int floors, string suffix, string frieze)
        {
            if (floor == 0)
            {
                h.Drop("SM_Vic_Foundation" + suffix, loc.WithZ(0.0), rot);
            }

            if (floor < floors - 1)
            {
                h.Drop("SM_Vic_String" + suffix, loc.WithZ(VicKit.FloorZ(floor + 1)), rot);
            }

            if (floor == floors - 1)
            {
                h.Drop("SM_Vic_Cornice" + suffix, loc.WithZ(VicKit.CorniceZ(floors)), rot);
                h.Drop(frieze, loc.WithZ(VicKit.WallTop(floors)), rot);
            }
        }

        /// <summary>
        /// A corner of one storey with its bands. "out" is a street corner (with a quoin when asked); "reentry" and
        /// "reentry_y" are the two hands of a court's inside corner (СБОРКА.md, Углы стен).
        /// </summary>
        public static void DressCorner(VicHouse h, V3 loc, double rot, int floor, int floors, string kind, bool quoin, bool stucco = false)
        {
            var cornice = VicKit.CorniceZ(floors);
            var frieze = VicKit.WallTop(floors);
            if (kind == "reentry" || kind == "reentry_y")
            {
                var y = kind == "reentry_y";
                var trim = y ? "In_Y" : "In";
                h.Drop(y ? "SM_Vic_Wall_Corner_Reentry_1m_Y" : "SM_Vic_Wall_Corner_Reentry_1m", loc, rot, stucco);
                if (floor == 0)
                {
                    h.Drop("SM_Vic_Foundation_Corner_" + trim, loc.WithZ(0.0), rot);
                }

                if (floor < floors - 1)
                {
                    h.Drop("SM_Vic_String_Corner_" + trim, loc.WithZ(VicKit.FloorZ(floor + 1)), rot);
                }

                if (floor == floors - 1)
                {
                    h.Drop("SM_Vic_Cornice_Corner_" + trim, loc.WithZ(cornice), rot);
                    h.Drop(y ? "SM_Vic_Frieze_Corner_Reentry_Y" : "SM_Vic_Frieze_Corner_Reentry", loc.WithZ(frieze), rot);
                }

                if (quoin)
                {
                    h.Drop("SM_Vic_Quoin_In_3m", loc, rot);
                }

                return;
            }

            h.Drop("SM_Vic_Wall_Corner_Out_1m", loc, rot, stucco);
            if (quoin)
            {
                h.Drop("SM_Vic_Quoin_Out_3m", loc, rot);
            }

            if (floor == 0)
            {
                h.Drop("SM_Vic_Foundation_Corner_Out", loc.WithZ(0.0), rot);
            }

            if (floor < floors - 1)
            {
                h.Drop("SM_Vic_String_Corner_Out", loc.WithZ(VicKit.FloorZ(floor + 1)), rot);
            }

            if (floor == floors - 1)
            {
                h.Drop("SM_Vic_Cornice_Corner_Out", loc.WithZ(cornice), rot);
                h.Drop("SM_Vic_Frieze_Corner_Out", loc.WithZ(frieze), rot);
            }
        }

        /// <summary>The ground-floor bay window: base, cheeks, front, its own roof and three windows (proof_houses._bay).</summary>
        public static void Bay(VicHouse h, V3 origin, double rot)
        {
            foreach (var part in new[] { "SM_Vic_Bay_Base", "SM_Vic_Bay_Cheek_L", "SM_Vic_Bay_Cheek_R", "SM_Vic_Bay_Front", "SM_Vic_Bay_Roof" })
            {
                h.Drop(part, origin, rot);
            }

            foreach (var (id, x, y, extra) in BayWindows())
            {
                h.Drop(id, VicKit.WorldPoint(origin, rot, x, y, VicKit.WinSill), rot + extra);
            }
        }

        /// <summary>trim.bay_window_locals: the two cheek windows face away from the bay's inside, the front one forward.</summary>
        private static IEnumerable<(string id, double x, double y, double extra)> BayWindows()
        {
            var interior = (x: 1.0, y: -0.36);
            foreach (var (p0, p1) in new[] { ((0.16, 0.0), (0.38, -0.72)), ((1.84, 0.0), (1.62, -0.72)) })
            {
                var dx = p1.Item1 - p0.Item1;
                var dy = p1.Item2 - p0.Item2;
                var n = Math.Sqrt((dx * dx) + (dy * dy));
                dx /= n;
                dy /= n;
                var left = (x: dy, y: -dx);
                var mid = (x: (p0.Item1 + p1.Item1) * 0.5, y: (p0.Item2 + p1.Item2) * 0.5);
                double Len(double ax, double ay) => Math.Sqrt((ax * ax) + (ay * ay));
                var towards = Len(mid.x + left.x - interior.x, mid.y + left.y - interior.y);
                var away = Len(mid.x - left.x - interior.x, mid.y - left.y - interior.y);
                var outward = towards < away ? (x: -left.x, y: -left.y) : left;
                yield return ("SM_Vic_Window_Rect_Narrow", mid.x, mid.y, Math.Atan2(outward.x, -outward.y));
            }

            yield return ("SM_Vic_Window_Rect_Single", 1.0, -0.72, 0.0);
        }

        /// <summary>A stone or iron balcony on a wall module, its slab top at <paramref name="zTop"/> (proof_houses._balcony).</summary>
        public static void Balcony(VicHouse h, V3 origin, double rot, double zTop, bool stone)
        {
            var front = stone ? "SM_Vic_Balcony_Stone_Front_2m" : "SM_Vic_Balcony_Rail_Front_2m";
            var ret = stone ? "SM_Vic_Balcony_Stone_Return" : "SM_Vic_Balcony_Rail_Return";
            var slab = origin.WithZ(zTop);
            h.Drop("SM_Vic_Balcony_Slab_2m", slab, rot);
            h.Drop(front, VicKit.WorldPoint(slab, rot, 0.0, -0.80, 0.0), rot);
            h.Drop(ret, slab, rot);
            h.Drop(ret + "_R", VicKit.WorldPoint(slab, rot, 2.0, 0.0, 0.0), rot);
            foreach (var x in new[] { 0.40, 1.60 })
            {
                h.Drop("SM_Vic_Bracket_Step", VicKit.WorldPoint(slab, rot, x, 0.0, 0.0), rot);
            }
        }

        /// <summary>
        /// A porch before a ground-floor door module: pilasters on the wall, columns on the pavement clear of the steps,
        /// and the beam on their capitals at 3.0 m (СБОРКА.md, Вход и балкон).
        /// </summary>
        public static void Porch(VicHouse h, V3 door, double rot)
        {
            var y = -((4 * 0.28) + 0.20);
            Doorcase(h, door, rot);
            foreach (var x in new[] { 0.20, 1.80 })
            {
                h.Drop("SM_Vic_Column_3m", VicKit.WorldPoint(door, rot, x, y, -VicKit.Foundation), rot);
            }

            h.Drop("SM_Vic_Porch_Beam_2m", VicKit.WorldPoint(door, rot, 0.0, y, 2.40), rot);
        }

        /// <summary>
        /// A doorcase: the porch's two pilasters on the wall, without its columns and beam. Veilmouth's houses use it
        /// until the kit has a porch roof, a door hood or a canopy: the owner found the bare beam out of place, and the
        /// balcony slab, 0.86 m deep, cannot roof columns 1.32 m out (reference plan R6; SLICE_MODELS_WANTED.md).
        /// </summary>
        public static void Doorcase(VicHouse h, V3 door, double rot)
        {
            foreach (var x in new[] { 0.28, 1.72 })
            {
                h.Drop("SM_Vic_Pilaster_3m", VicKit.WorldPoint(door, rot, x, 0.0, 0.0), rot);
            }
        }

        /// <summary>The oriel's 4 m storey with its bands and three windows (proof_houses._oriel).</summary>
        public static void Oriel(VicHouse h, V3 origin, double rot, int floor, int floors, bool arch, bool stucco = false)
        {
            h.Drop(arch ? "SM_Vic_Wall_Oriel_Arch_4m" : "SM_Vic_Wall_Oriel_Rect_4m", origin, rot, stucco);
            if (floor == 0)
            {
                h.Drop("SM_Vic_Foundation_Oriel_4m", origin.WithZ(0.0), rot);
            }

            if (floor < floors - 1)
            {
                h.Drop("SM_Vic_String_Oriel_4m", origin.WithZ(VicKit.FloorZ(floor + 1)), rot);
            }

            if (floor == floors - 1)
            {
                h.Drop("SM_Vic_Cornice_Oriel_4m", origin.WithZ(VicKit.CorniceZ(floors)), rot);
                h.Drop("SM_Vic_Frieze_Oriel_4m", origin.WithZ(VicKit.WallTop(floors)), rot);
            }

            const double run = 1.15;
            const double span = 4.0;
            var kind = arch ? "win_arch_s" : "win_rect_s";
            OpeningAt(h, origin, rot, kind, run * 0.5, -run * 0.5, -Math.PI * 0.25);
            OpeningAt(h, origin, rot, kind, span * 0.5, -run, 0.0);
            OpeningAt(h, origin, rot, kind, span - (run * 0.5), -run * 0.5, Math.PI * 0.25);
        }

        /// <summary>The oriel cap's fascia, gutters and side soffits, at the cap's own pivot (proof_houses._oriel_border).</summary>
        public static void OrielCap(VicHouse h, V3 origin, double rot)
        {
            h.Drop("SM_Vic_Roof_Oriel_Cap", origin, rot);
            foreach (var name in new[] { "SideL", "CheekL", "Front", "CheekR", "SideR" })
            {
                h.Drop("SM_Vic_Roof_Eave_Oriel_" + name, origin, rot);
                h.Drop("SM_Vic_Rain_Gutter_Oriel_" + name, origin, rot);
            }

            var drop = -VicKit.Overhang * VicKit.Tan;
            h.Drop("SM_Vic_Roof_Soffit_Oriel_Side", VicKit.WorldPoint(origin, rot, 0.0, -VicKit.Overhang, drop), rot);
            h.Drop("SM_Vic_Roof_Soffit_Oriel_Side", VicKit.WorldPoint(origin, rot, 1.0 + 4.0, -VicKit.Overhang, drop), rot);
        }

        public static (string straight, string corner) SoffitIds(double tan) =>
            tan > 1.2 ? ("SM_Vic_Roof_Soffit_Mansard_2m", "SM_Vic_Roof_Soffit_Mansard_Corner") : ("SM_Vic_Roof_Soffit_42_2m", "SM_Vic_Roof_Soffit_42_Corner");

        public static void GutterRun(VicHouse h, V3 loc, double rot, string soffit)
        {
            h.Drop("SM_Vic_Roof_Eave_2m", loc, rot);
            h.Drop("SM_Vic_Rain_Gutter_2m", loc, rot);
            h.Drop(soffit, loc, rot);
        }

        /// <summary>The outlet, elbow, hopper, pipes, clips and shoe under an eave point (proof_houses._downpipe).</summary>
        public static void Downpipe(VicHouse h, V3 eave, double rot)
        {
            h.Drop("SM_Vic_Rain_Outlet", eave, rot);
            var (px, py) = VicKit.Rotated(0.0, VicKit.Overhang - 0.08, rot);
            var (wx, wy) = VicKit.Rotated(0.0, VicKit.Overhang, rot);
            var pipe = new V3(eave.X + px, eave.Y + py, 0.0);
            var wall = new V3(eave.X + wx, eave.Y + wy, 0.0);
            h.Drop("SM_Vic_Rain_Shoe", pipe.WithZ(0.02), rot);
            var socket = eave.Z - 0.38;
            var z = 0.18;
            while (z + 3.0 <= socket)
            {
                h.Drop("SM_Vic_Rain_Pipe_3m", pipe.WithZ(z), 0.0);
                h.Drop("SM_Vic_Rain_Bracket", wall.WithZ(z + 0.4), rot);
                z += 3.0;
            }

            while (z + 1.0 <= socket)
            {
                h.Drop("SM_Vic_Rain_Pipe_1m", pipe.WithZ(z), 0.0);
                h.Drop("SM_Vic_Rain_Bracket", wall.WithZ(z + 0.4), rot);
                z += 1.0;
            }

            h.Drop("SM_Vic_Rain_Elbow", eave, rot);
            h.Drop("SM_Vic_Rain_Hopper", wall.WithZ(eave.Z - 0.40), rot);
        }

        private static readonly Dictionary<string, (double x, double y)[]> PotSlots = new Dictionary<string, (double, double)[]>
        {
            { "SM_Vic_Chimney_2", new[] { (-0.18, 0.0), (0.18, 0.0) } },
            { "SM_Vic_Chimney_4", new[] { (-0.18, -0.18), (-0.18, 0.18), (0.18, -0.18), (0.18, 0.18) } },
        };

        public static void Pots(VicHouse h, string chimney, V3 at)
        {
            var slots = PotSlots[chimney];
            for (var i = 0; i < slots.Length; i++)
            {
                var pot = i % 2 == 0 ? "SM_Vic_ChimneyPot_Crown" : "SM_Vic_ChimneyPot_Roll";
                h.Drop(pot, VicKit.WorldPoint(at, 0.0, slots[i].x, slots[i].y, VicKit.ChimneyTop), 0.0);
            }
        }

        /// <summary>
        /// Chimney_4 on a mansard's flat top with its flat flashing (СБОРКА.md, Трубы): its centre is a flat tile's centre,
        /// so at least 1 m from the flat's edge.
        /// </summary>
        public static void FlatChimney(VicHouse h, V3 at)
        {
            h.Drop("SM_Vic_Chimney_4", at, 0.0);
            h.Drop("SM_Vic_Roof_Flashing_Flat", at, 0.0);
            Pots(h, "SM_Vic_Chimney_4", at);
        }

        /// <summary>
        /// Chimney_2 on the side of a 42° slope, <paramref name="up"/> metres in from the wall line of a slope whose wall
        /// runs from <paramref name="wallStart"/> along <paramref name="rot"/> (proof_houses._slope_chimney, generalised
        /// to any side). Its flashing (±0.56 m) must lie on that one plane.
        /// </summary>
        public static void SlopeChimney(VicHouse h, V3 wallStart, double rot, double along, double up, double spring)
        {
            var at = VicKit.WorldPoint(wallStart.WithZ(spring), rot, along, up, up * VicKit.Tan);
            h.Drop("SM_Vic_Roof_Flashing_Slope42", at, rot);
            h.Drop("SM_Vic_Chimney_2", at, rot);
            Pots(h, "SM_Vic_Chimney_2", at);
        }
    }
}

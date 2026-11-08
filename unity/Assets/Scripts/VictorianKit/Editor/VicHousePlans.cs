using System;
using System.Collections.Generic;
using System.Linq;

namespace GameDev.VictorianKit.EditorTools
{
    /// <summary>A house the streets use: its pieces, plan size and the facts the street rules check.</summary>
    public sealed class VicHouseSpec
    {
        public string Name;
        public VicPalette Palette;
        public int Width, Depth, Floors;
        public string Roof;
        public VicHouse House;

        /// <summary>The plan's outline kind, for the rule that neighbours differ: "U", "L oriel", "8x4" …</summary>
        public string PlanKind;

        /// <summary>The roof's highest line above the ground, metres.</summary>
        public double Ridge;

        /// <summary>Where the house stands on its street, metres along X from the street's start.</summary>
        public int X;
    }

    /// <summary>
    /// The houses of the two show streets (Plans/VICTORIAN_STREETS_PLAN.md §5–6) and the kit's own House U, which
    /// checks the assembler: built here it must equal the package's House_U_Modular.json piece for piece.
    /// </summary>
    public static class VicHousePlans
    {
        /// <summary>
        /// The kit's House U (СБОРКА.md, Правила; roof_junctions.assemble_house): 16 × 10 m, a 6 × 4 m court open to the
        /// street, three storeys all round, one mansard from the third storey's plate.
        /// </summary>
        public static VicHouse HouseU()
        {
            var h = new VicHouse("House_U_Modular");
            var plan = new VicPlan().Rect(0, 0, 6, 10).Rect(6, 4, 6, 6).Rect(12, 0, 4, 10).Trace();
            var facade = new VicFacade
            {
                Quoin = c => true,
                Key = (e, b, f) =>
                {
                    if (e.Side == "front" && e.Y0 == 4 && b == 0 && f == 0)
                    {
                        return "door_rect";
                    }

                    // The back's bays at the wings' junctions are two plain metres each.
                    if (e.Side == "back" && (b == 1 || b == 4))
                    {
                        return "meters";
                    }

                    return f == 2 ? "win_arch_s" : "win_rect_s";
                },
            };
            VicRoofs.Shell(h, plan, 3, facade);
            var o = new VicMansardOptions();
            o.Chimneys.Add((4, 3));
            VicRoofs.Mansard(h, plan, 3, o);
            return h;
        }

        // ------------------------------------------------------------ the rich street

        /// <summary>The rich street, R1 to R3 from the left, 4 m apart.</summary>
        public static List<VicHouseSpec> RichStreet()
        {
            var street = new List<VicHouseSpec> { CourtHouse(), OrielHouse(), PavilionHouse() };
            Place(street, 4);
            return street;
        }

        /// <summary>
        /// R1: a U of 20 × 12 m with 6 m wings and an 8 × 4 m court open to the street; stucco cream. A porch with
        /// columns in the court's middle bay, bay windows and a stone balcony on each wing front, arched top windows,
        /// lucarnes over the wings and the porch, two chimneys on the wings' flat.
        /// </summary>
        public static VicHouseSpec CourtHouse()
        {
            var h = new VicHouse("R1_CourtHouse");
            var plan = new VicPlan().Rect(0, 0, 6, 12).Rect(6, 4, 8, 8).Rect(14, 0, 6, 12).Trace();
            var facade = new VicFacade
            {
                Quoin = c => true,
                Stucco = true,
                Key = (e, b, f) =>
                {
                    if (e.Side == "front" && e.Y0 == 0)
                    {
                        return f == 0 ? "bay" : f == 1 ? "win_rect_d" : "win_arch_s";
                    }

                    if (e.Side == "front" && e.Y0 == 4)
                    {
                        if (f == 0)
                        {
                            return b == 1 ? "door_arch" : "win_rect_s";
                        }

                        return f == 1 && b == 1 ? "win_arch_d" : f == 2 ? "win_arch_s" : "win_rect_s";
                    }

                    if (e.Side == "back")
                    {
                        if (f == 0 && b == 4)
                        {
                            return "door_rect";
                        }

                        return b % 2 == 1 ? "plain" : f == 2 ? "win_arch_s" : "win_rect_s";
                    }

                    return f == 2 ? "win_arch_s" : "win_rect_s";
                },
            };
            VicRoofs.Shell(h, plan, 3, facade);
            var balconyZ = VicKit.FloorZ(1) + VicKit.WinSill - 0.02;
            VicDress.Balcony(h, new V3(1, 0, VicKit.FloorZ(1)), 0.0, balconyZ, true);
            VicDress.Balcony(h, new V3(17, 0, VicKit.FloorZ(1)), 0.0, balconyZ, true);
            VicDress.Porch(h, new V3(9, 4, VicKit.FloorZ(0)), 0.0);

            var o = new VicMansardOptions();
            o.Chimneys.Add((1, 3));
            o.Chimneys.Add((8, 3));
            o.Lucarnes.Add((0, 3.0, false, false));
            o.Lucarnes.Add((4, 3.0, false, false));
            o.Lucarnes.Add((2, 4.0, true, false));
            VicRoofs.Mansard(h, plan, 3, o);
            return Spec(h, VicPalette.Rich("Brick_Rich", "Stucco_Cream"), plan, 3, "mansard", "U court");
        }

        /// <summary>
        /// R2: an L, 18 × 8 m with a 6 × 6 m rear wing on the right; warm brick. A full-height oriel over bays 3–4 with
        /// its cap and an arched lucarne on it, an arched double door with stone rails and a stone balcony above,
        /// arched top windows, rect lucarnes at 3 m and 15 m, chimneys on the flat.
        /// </summary>
        public static VicHouseSpec OrielHouse()
        {
            var h = new VicHouse("R2_OrielHouse");
            var plan = new VicPlan().Rect(0, 0, 18, 8).Rect(12, 8, 6, 6).Trace();
            var facade = new VicFacade
            {
                Quoin = c => true,
                StoneRail = (e, b) => e.Side == "front" && b == 1,
                Key = (e, b, f) =>
                {
                    if (e.Index == 0)
                    {
                        if (b == 3)
                        {
                            return f == 2 ? "oriel_arch" : "oriel_rect";
                        }

                        if (b == 4)
                        {
                            return "skip";
                        }

                        if (f == 0 && b == 1)
                        {
                            return "door_arch_double";
                        }

                        return f == 2 ? "win_arch_s" : f == 1 && b == 6 ? "win_rect_d" : "win_rect_s";
                    }

                    if (e.Index == 4 && f == 0 && b == 2)
                    {
                        return "door_rect";
                    }

                    return f == 2 ? "win_arch_s" : "win_rect_s";
                },
            };
            VicRoofs.Shell(h, plan, 3, facade);
            VicDress.Balcony(h, new V3(3, 0, VicKit.FloorZ(1)), 0.0, VicKit.FloorZ(1) + VicKit.WinSill - 0.02, true);

            var o = new VicMansardOptions();
            o.OrielCaps.Add((0, 3));
            o.Lucarnes.Add((0, 9.0, true, true));
            o.Lucarnes.Add((0, 3.0, false, false));
            o.Lucarnes.Add((0, 15.0, false, false));
            o.Chimneys.Add((1, 2));
            o.Chimneys.Add((7, 4));
            VicRoofs.Mansard(h, plan, 3, o);
            return Spec(h, VicPalette.Rich("Brick_Rich"), plan, 3, "mansard", "L oriel");
        }

        /// <summary>
        /// R3: 16 × 10 m with an 8 × 2 m front pavilion; stucco white. A porch with columns at the pavilion's centre and
        /// a stone balcony over it, bay windows on its side bays, arched top windows, a wide flat top with two chimneys
        /// and the weather vane, lucarnes over the pavilion and both side fronts.
        /// </summary>
        public static VicHouseSpec PavilionHouse()
        {
            var h = new VicHouse("R3_PavilionHouse");
            var plan = new VicPlan().Rect(0, 2, 16, 10).Rect(4, 0, 8, 2).Trace();
            var facade = new VicFacade
            {
                Quoin = c => true,
                Stucco = true,
                Key = (e, b, f) =>
                {
                    if (e.Side == "front" && e.Y0 == 0)
                    {
                        if (f == 0)
                        {
                            return b == 1 ? "door_double" : "bay";
                        }

                        return f == 1 ? (b == 1 ? "win_arch_d" : "win_rect_d") : "win_arch_s";
                    }

                    if (e.Side == "front")
                    {
                        return f == 0 ? "win_rect_d" : f == 1 ? "win_rect_s" : "win_arch_s";
                    }

                    if (e.Side == "back")
                    {
                        if (f == 0 && b == 3)
                        {
                            return "door_rect";
                        }

                        return b % 2 == 1 ? "plain" : f == 2 ? "win_arch_s" : "win_rect_s";
                    }

                    return f == 2 ? "win_arch_s" : "win_rect_s";
                },
            };
            VicRoofs.Shell(h, plan, 3, facade);
            VicDress.Porch(h, new V3(7, 0, VicKit.FloorZ(0)), 0.0);
            VicDress.Balcony(h, new V3(7, 0, VicKit.FloorZ(1)), 0.0, VicKit.FloorZ(1) + VicKit.WinSill - 0.02, true);

            var o = new VicMansardOptions();
            o.Chimneys.Add((1, 3));
            o.Chimneys.Add((6, 3));
            o.Vane = (4, 1);
            o.Lucarnes.Add((0, 4.0, true, false));
            o.Lucarnes.Add((6, 3.0, false, false));
            o.Lucarnes.Add((2, 1.0, false, false));
            VicRoofs.Mansard(h, plan, 3, o);
            return Spec(h, VicPalette.Rich("Brick_Rich", "Stucco_White"), plan, 3, "mansard", "pavilion");
        }

        // ------------------------------------------------------------ the poor street

        /// <summary>The poor street, P1 to P8 from the left, 2 m apart (§6).</summary>
        public static List<VicHouseSpec> PoorStreet()
        {
            var street = new List<VicHouseSpec>
            {
                Poor("P1", 8, 4, 2, "gable", new[] { 0 }, VicPalette.Poor("Brick_Poor", "Wood_Green"), false, 6.0),
                Poor("P2", 10, 6, 3, "hip", new[] { 2 }, VicPalette.Common("Brick_Common", "Wood_Brown"), true, 5.0,
                    (e, b, f) => e.Side == "front" && f == 2 && b == 2 ? "win_arch_s" : null),
                Poor("P3", 6, 4, 2, "hip", new[] { 1 }, VicPalette.Poor("Brick_Stock", "Wood_Oxblood"), false, 3.0),
                Poor("P4", 12, 6, 2, "gable", new[] { 0, 4 }, VicPalette.Common("Brick_Common", "Wood_Green"), false, 3.0,
                    (e, b, f) => e.Side != "front" ? null : f == 0 && (b == 1 || b == 3) ? "win_rect_d" : f == 1 && b == 2 ? "win_arch_s" : null),
                Poor("P5", 8, 6, 3, "gable", new[] { 2 }, VicPalette.Poor("Brick_Poor", "Wood_Oxblood"), false, 2.0),
                Poor("P6", 10, 4, 3, "gable", new[] { 1 }, VicPalette.Common("Brick_Stock", "Wood_Brown"), true, 7.0,
                    (e, b, f) => e.Side == "front" && f == 0 && b == 1 ? "door_arch" : null),
                Poor("P7", 6, 6, 2, "hip", new[] { 0 }, VicPalette.Poor("Brick_Poor", "Wood_Green"), false, 3.0),
                Poor("P8", 12, 4, 3, "hip", new[] { 3 }, VicPalette.Common("Brick_Common", "Wood_Oxblood"), false, 6.0),
            };
            Place(street, 2);
            return street;
        }

        /// <summary>
        /// A poor house: one rectangle under one roof of its own height. The front's doors stand in the given bays,
        /// the rest rectangular windows; the back has its door in the last bay and windows in every other bay; the
        /// narrow sides show one window a storey above the ground floor. <paramref name="extra"/> overrides a bay.
        /// </summary>
        private static VicHouseSpec Poor(
            string name, int w, int d, int floors, string roof, int[] doors, VicPalette palette, bool quoins, double chimneyX,
            Func<VicEdge, int, int, string> extra = null)
        {
            var h = new VicHouse(name);
            var plan = new VicPlan().Rect(0, 0, w, d).Trace();
            var lastBack = ((w - 2) / 2) - 1;
            var facade = new VicFacade
            {
                Quoin = c => quoins && c.Convex && c.Y == 0,
                Key = (e, b, f) =>
                {
                    var k = extra?.Invoke(e, b, f);
                    if (k != null)
                    {
                        return k;
                    }

                    if (e.Side == "front")
                    {
                        return f == 0 && doors.Contains(b) ? "door_rect" : "win_rect_s";
                    }

                    if (e.Side == "back")
                    {
                        if (f == 0 && b == lastBack)
                        {
                            return "door_rect";
                        }

                        return b % 2 == 0 ? "win_rect_s" : "plain";
                    }

                    return f > 0 ? "win_rect_s" : "plain";
                },
            };
            VicRoofs.Shell(h, plan, floors, facade);
            if (roof == "gable")
            {
                VicRoofs.Gable(h, w, d, floors, chimneyX);
            }
            else
            {
                VicRoofs.Hip(h, w, d, floors, chimneyX);
            }

            return Spec(h, palette, plan, floors, roof, $"{w}x{d}");
        }

        // ------------------------------------------------------------ helpers

        private static VicHouseSpec Spec(VicHouse h, VicPalette palette, VicPlan plan, int floors, string roof, string kind)
        {
            var spring = VicKit.Spring(floors);
            var ridge = roof == "mansard" ? spring + VicKit.MansardRise : spring + (VicKit.HalfSpan(plan.Depth) * VicKit.Tan);
            return new VicHouseSpec
            {
                Name = h.Name,
                Palette = palette,
                Width = plan.Width,
                Depth = plan.Depth,
                Floors = floors,
                Roof = roof,
                House = h,
                PlanKind = kind,
                Ridge = ridge,
            };
        }

        /// <summary>Sets each house's X: side by side from 0, <paramref name="gap"/> metres apart, all on the 2 m grid.</summary>
        private static void Place(List<VicHouseSpec> street, int gap)
        {
            var x = 0;
            foreach (var s in street)
            {
                s.X = x;
                x += s.Width + gap;
            }
        }

        /// <summary>Pieces in <paramref name="a"/> missing from <paramref name="b"/>, by placement key, with multiplicity.</summary>
        public static List<string> Missing(IEnumerable<VicPiece> a, IEnumerable<VicPiece> b)
        {
            var have = b.GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.Count());
            var missing = new List<string>();
            foreach (var p in a)
            {
                if (have.TryGetValue(p.Key, out var n) && n > 0)
                {
                    have[p.Key] = n - 1;
                }
                else
                {
                    missing.Add(p.Key);
                }
            }

            return missing;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameDev.VictorianKit.EditorTools
{
    /// <summary>How a row house ends on one side: open to the next house of its row, or faced like a street corner.</summary>
    public enum VicEnd
    {
        Open,
        Faced,
    }

    /// <summary>
    /// The house types of Veilmouth's plan C3 (VEILMOUTH_CITY_PLAN.md §24.4), built from the kit as its own houses are.
    /// <para>
    /// A row house is one rectangle of the row it stands in. Its open side continues into the next house of the same
    /// type, as one terrace: the walls end in a plain metre, and the bands, eaves, slopes and parapet run on to the line,
    /// where the neighbour's run on from it. No piece is laid over another and no end is left showing: an open side
    /// stands only against a house of the same height, depth and roof, which the city's rows see to. A faced side is a
    /// street corner with its corner pieces, quoins, hip or gable.
    /// </para>
    /// <para>
    /// The mansard of a row house is the kit's own mansard over the plan extended 4 m past each open side, keeping the
    /// pieces that lie wholly within the house: everything at the line then is a straight piece, as in the middle of a
    /// longer house. Kit metres throughout; the city scales the house by 1.25.
    /// </para>
    /// </summary>
    public static class VicCityHouses
    {
        /// <summary>How far a row house's mansard plan runs past an open side before its pieces are kept, m.</summary>
        private const int Extension = 4;

        /// <summary>Kit palettes by the short name the city's keys carry.</summary>
        public static VicPalette Palette(string name) => name switch
        {
            "stucco" => VicPalette.Rich("Brick_Rich", "Stucco_Cream"),
            "white" => VicPalette.Rich("Brick_Rich", "Stucco_White"),
            "rich" => VicPalette.Rich("Brick_Rich"),
            "brick" => VicPalette.Common("Brick_Common", "Wood_Brown"),
            "stock" => VicPalette.Common("Brick_Stock", "Wood_Oxblood"),
            "poor" => VicPalette.Poor("Brick_Poor", "Wood_Green"),
            "dark" => VicPalette.Poor("Brick_Stock", "Wood_Oxblood"),
            _ => throw new ArgumentException($"No kit palette is called {name}."),
        };

        // ------------------------------------------------------------------------------------------------ row houses

        /// <summary>A row house's walls and roof.</summary>
        public sealed class RowOptions
        {
            public string Name;
            public int Width, Depth, Floors;
            public string Roof = "mansard"; // or "gable"
            public VicEnd Left = VicEnd.Faced, Right = VicEnd.Faced;
            public VicPalette Palette;
            public string Kind;

            /// <summary>The wall key of a bay: side ("front", "back", "left", "right"), bay, storey.</summary>
            public Func<string, int, int, string> Key = (s, b, f) => "plain";

            public bool Quoins, Stucco;
            public Func<string, int, bool> StoneRail = (s, b) => false;

            /// <summary>A doorcase round the front door in this bay, or none (reference plan R6: no porch until the kit has a roof for one).</summary>
            public int Porch = -1;

            /// <summary>Lucarnes over every other front bay that a straight slope carries.</summary>
            public bool Lucarnes = true;

            public bool ArchLucarnes;
            public bool Vane;
        }

        /// <summary>A rectangle W × D with open or faced ends; mansard D ≥ 6, gable D 4 or 6.</summary>
        public static VicHouseSpec Row(RowOptions o)
        {
            if (o.Width % 4 != 0 || o.Depth % 2 != 0)
            {
                throw new ArgumentException($"{o.Name}: a city house is a whole number of 4 m wide and an even number of metres deep.");
            }

            var h = new VicHouse(o.Name);
            RowShell(h, o);
            if (o.Roof == "mansard")
            {
                RowMansard(h, o);
            }
            else
            {
                RowGable(h, o);
            }

            if (o.Porch >= 0)
            {
                VicDress.Doorcase(h, new V3(1 + (2 * o.Porch), 0, VicKit.FloorZ(0)), 0.0);
            }

            return Spec(h, o.Palette, new VicPlan().Rect(0, 0, o.Width, o.Depth).Trace(), o.Floors, o.Roof, o.Kind ?? $"{o.Width}x{o.Depth}");
        }

        /// <summary>Every storey's walls: corners on faced ends, a plain metre at an open end, 2 m bays between.</summary>
        private static void RowShell(VicHouse h, RowOptions o)
        {
            int w = o.Width, d = o.Depth, floors = o.Floors;
            var bays = (w - 2) / 2;
            var sideBays = (d - 2) / 2;
            for (var f = 0; f < floors; f++)
            {
                var z = VicKit.FloorZ(f);
                if (o.Left == VicEnd.Faced)
                {
                    VicDress.DressCorner(h, new V3(0, 0, z), VicPlan.QuarterRot(1, 1), f, floors, "out", o.Quoins, o.Stucco);
                    VicDress.DressCorner(h, new V3(0, d, z), VicPlan.QuarterRot(1, -1), f, floors, "out", o.Quoins, o.Stucco);
                    for (var b = 0; b < sideBays; b++)
                    {
                        Bay(h, o, "left", b, f, new V3(0, d - 1 - (2 * b), z), -VicDress.HalfPi);
                    }
                }
                else
                {
                    VicDress.PlainMeter(h, new V3(0, 0, z), 0.0, f, floors, o.Stucco);
                    VicDress.PlainMeter(h, new V3(1, d, z), Math.PI, f, floors, o.Stucco);
                }

                if (o.Right == VicEnd.Faced)
                {
                    VicDress.DressCorner(h, new V3(w, 0, z), VicPlan.QuarterRot(-1, 1), f, floors, "out", o.Quoins, o.Stucco);
                    VicDress.DressCorner(h, new V3(w, d, z), VicPlan.QuarterRot(-1, -1), f, floors, "out", o.Quoins, o.Stucco);
                    for (var b = 0; b < sideBays; b++)
                    {
                        Bay(h, o, "right", b, f, new V3(w, 1 + (2 * b), z), VicDress.HalfPi);
                    }
                }
                else
                {
                    VicDress.PlainMeter(h, new V3(w - 1, 0, z), 0.0, f, floors, o.Stucco);
                    VicDress.PlainMeter(h, new V3(w, d, z), Math.PI, f, floors, o.Stucco);
                }

                for (var b = 0; b < bays; b++)
                {
                    Bay(h, o, "front", b, f, new V3(1 + (2 * b), 0, z), 0.0);
                    Bay(h, o, "back", b, f, new V3(w - 1 - (2 * b), d, z), Math.PI);
                }
            }
        }

        private static void Bay(VicHouse h, RowOptions o, string side, int b, int f, V3 at, double rot)
        {
            var key = o.Key(side, b, f);
            if (key == "meters")
            {
                var (dx, dy) = VicKit.Rotated(1.0, 0.0, rot);
                VicDress.PlainMeter(h, at, rot, f, o.Floors, o.Stucco);
                VicDress.PlainMeter(h, new V3(at.X + dx, at.Y + dy, at.Z), rot, f, o.Floors, o.Stucco);
                return;
            }

            VicDress.DressWall(h, at, rot, key, f, o.Floors, o.StoneRail(side, b), o.Stucco);
        }

        /// <summary>
        /// The kit's mansard over the plan run on <see cref="Extension"/> m past each open side, keeping what lies within
        /// the house; then lucarnes, chimneys on the flat by the left line (the party wall's stack), and a downpipe there.
        /// </summary>
        private static void RowMansard(VicHouse h, RowOptions o)
        {
            int w = o.Width, d = o.Depth;
            if (d < 6)
            {
                throw new ArgumentException($"{o.Name}: a row mansard needs a flat top, 6 m deep at least.");
            }

            var x0 = o.Left == VicEnd.Open ? -Extension : 0;
            var x1 = o.Right == VicEnd.Open ? w + Extension : w;
            var plan = new VicPlan().Rect(x0, 0, x1 - x0, d).Trace();
            var roof = new VicHouse("roof");
            var options = new VicMansardOptions();

            // The flat runs from 2 m inside a faced side, or to the open line; its tiles are i0 … i1 − 1.
            var i0 = o.Left == VicEnd.Open ? 0 : 1;
            var i1 = o.Right == VicEnd.Open ? w / 2 : (w / 2) - 1;
            options.Chimneys.Add((i0, 1));
            if (w >= 12)
            {
                options.Chimneys.Add((i1 - 1, (d / 2) - 2));
            }

            if (o.Vane)
            {
                options.Vane = ((i0 + i1) / 2, (d / 4));
            }

            VicRoofs.Mansard(roof, plan, o.Floors, options);
            foreach (var p in roof.Pieces)
            {
                var (lo, hi) = Span(p);
                if ((o.Left == VicEnd.Open && lo < -0.05) || (o.Right == VicEnd.Open && hi > w + 0.05))
                {
                    continue;
                }

                h.Pieces.Add(p);
            }

            var spring = VicKit.Spring(o.Floors);
            if (o.Left == VicEnd.Open)
            {
                VicDress.Downpipe(h, new V3(0.45, -VicKit.Overhang, VicKit.EaveZ(spring, VicKit.Tan)), 0.0);
            }

            if (!o.Lucarnes)
            {
                return;
            }

            // Over every other front bay from the first, where the slope under it is straight: never beside a corner piece.
            var placed = 0;
            var bays = (w - 2) / 2;
            for (var b = 0; b < bays; b += 2)
            {
                placed += Lucarne(h, o, 2.0 + (2 * b), spring) ? 1 : 0;
            }

            if (placed == 0)
            {
                Lucarne(h, o, w * 0.5, spring);
            }
        }

        private static bool Lucarne(VicHouse h, RowOptions o, double along, double spring)
        {
            const double half = 0.7;
            if ((o.Left == VicEnd.Faced && along - half < VicKit.MansardInset) || (o.Right == VicEnd.Faced && along + half > o.Width - VicKit.MansardInset))
            {
                return false;
            }

            h.Drop(o.ArchLucarnes ? "SM_Vic_Lucarne_Arch" : "SM_Vic_Lucarne_Rect", VicKit.WorldPoint(new V3(0, 0, spring), 0.0, along, 0.40, 0.40 * VicKit.Tan), 0.0);
            return true;
        }

        /// <summary>
        /// A gabled roof over the row house (the kit's gable_roof): slopes and ridge in whole 2 m pieces across it, the
        /// gable wall and barge boards on a faced end only, eaves front and back, their stops on a faced end, a Chimney_2
        /// up the front slope by the left line, and the downpipe.
        /// </summary>
        private static void RowGable(VicHouse h, RowOptions o)
        {
            int w = o.Width, d = o.Depth;
            var key = d == 4 ? "D4" : d == 6 ? "D6" : throw new ArgumentException($"{o.Name}: a pitched roof spans 4 or 6 m.");
            var half = VicKit.HalfSpan(d);
            var spring = VicKit.Spring(o.Floors);
            var rise = half * VicKit.Tan;
            var z = VicKit.EaveZ(spring, VicKit.Tan);
            var oh = VicKit.Overhang;
            var (straight, _) = VicDress.SoffitIds(VicKit.Tan);
            for (var i = 0; i < w / 2; i++)
            {
                h.Drop($"SM_Vic_Roof_Gable_{key}_Deck", new V3(2 * i, 0.0, spring), 0.0);
                h.Drop($"SM_Vic_Roof_Gable_{key}_Deck", new V3(2 * (i + 1), d, spring), Math.PI);
                h.Drop("SM_Vic_Roof_RidgeCap_2m", new V3(2 * i, half, spring + rise), 0.0);
                VicDress.GutterRun(h, new V3(2 * i, -oh, z), 0.0, straight);
                VicDress.GutterRun(h, new V3(2 * (i + 1), d + oh, z), Math.PI, straight);
            }

            if (o.Left == VicEnd.Faced)
            {
                h.Drop($"SM_Vic_Roof_GableWall_{key}", new V3(0.0, 0.0, spring), 0.0);
                h.Drop($"SM_Vic_Roof_Barge_{key}", new V3(0.0, 0.0, spring), 0.0);
                h.Drop($"SM_Vic_Roof_Barge_{key}", new V3(0.0, d, spring), Math.PI);
                h.Drop("SM_Vic_Rain_Gutter_Stop", new V3(0.0, -oh, z), 0.0);
                h.Drop("SM_Vic_Rain_Gutter_Stop", new V3(0.0, d + oh, z), Math.PI);
            }

            if (o.Right == VicEnd.Faced)
            {
                h.Drop($"SM_Vic_Roof_GableWall_{key}", new V3(w, d, spring), Math.PI);
                h.Drop($"SM_Vic_Roof_Barge_{key}", new V3(w, 0.0, spring), 0.0);
                h.Drop($"SM_Vic_Roof_Barge_{key}", new V3(w, d, spring), Math.PI);
                h.Drop("SM_Vic_Rain_Gutter_Stop", new V3(w, -oh, z), 0.0);
                h.Drop("SM_Vic_Rain_Gutter_Stop", new V3(w, d + oh, z), Math.PI);
            }

            VicDress.SlopeChimney(h, new V3(0.0, 0.0, 0.0), 0.0, 1.0, 0.85, spring);
            if (w >= 12)
            {
                VicDress.SlopeChimney(h, new V3(w, d, 0.0), Math.PI, 1.0, 0.85, spring);
            }

            VicDress.Downpipe(h, new V3(0.45, -oh, z), 0.0);
        }

        // ------------------------------------------------------------------------------------------------ the types

        /// <summary>A shop house: four storeys, a mansard with lucarnes; double windows either side of the shop's double door.</summary>
        public static VicHouseSpec Shop(string name, int w, string palette, VicEnd left, VicEnd right)
        {
            var rich = palette == "stucco" || palette == "white" || palette == "rich";
            var bays = (w - 2) / 2;
            var door = bays / 2;
            return Row(new RowOptions
            {
                Name = name, Width = w, Depth = 8, Floors = 4, Left = left, Right = right, Palette = Palette(palette), Kind = $"shop {w}",
                Quoins = rich, Stucco = palette == "stucco" || palette == "white", ArchLucarnes = rich,
                StoneRail = (s, b) => rich && s == "front",
                Key = (s, b, f) =>
                {
                    switch (s)
                    {
                        case "front":
                            return f == 0 ? (b == door ? (rich ? "door_arch_double" : "door_double") : "win_rect_d")
                                : f == 1 ? (b == door && rich ? "win_arch_d" : "win_rect_d")
                                : f == 3 ? "win_arch_s" : "win_rect_s";
                        case "back":
                            return f == 0 && b == bays - 1 ? "door_rect" : b % 2 == 1 ? "plain" : "win_rect_s";
                        default:
                            return f == 0 ? "win_rect_d" : f == 3 ? "win_arch_s" : "win_rect_s";
                    }
                },
            });
        }

        /// <summary>A town house of three storeys under a mansard: bay windows either side of its door, or a shop front.</summary>
        public static VicHouseSpec Town(string name, int w, string palette, VicEnd left, VicEnd right, bool shop)
        {
            var bays = (w - 2) / 2;
            var door = bays / 2;
            var poor = palette == "poor" || palette == "dark";
            return Row(new RowOptions
            {
                Name = name, Width = w, Depth = 8, Floors = 3, Left = left, Right = right, Palette = Palette(palette), Kind = $"town {w}",
                Lucarnes = !poor,
                Key = (s, b, f) =>
                {
                    switch (s)
                    {
                        case "front":
                            if (f == 0)
                            {
                                return b == door ? (shop ? "door_double" : "door_arch") : shop ? "win_rect_d" : poor ? "win_rect_s" : "bay";
                            }

                            return f == 2 && !poor ? "win_arch_s" : "win_rect_s";
                        case "back":
                            return f == 0 && b == bays - 1 ? "door_rect" : b % 2 == 1 ? "plain" : "win_rect_s";
                        default:
                            return f == 0 ? "plain" : "win_rect_s";
                    }
                },
            });
        }

        /// <summary>A tenement under a gable, 6 m deep: one door per house, single windows, the back's door to its yard.</summary>
        public static VicHouseSpec Tenement(string name, int w, int floors, string palette, VicEnd left, VicEnd right)
        {
            var bays = (w - 2) / 2;
            var door = bays / 2;
            return Row(new RowOptions
            {
                Name = name, Width = w, Depth = 6, Floors = floors, Roof = "gable", Left = left, Right = right, Palette = Palette(palette),
                Kind = $"tenement {w}x{floors}",
                Key = (s, b, f) =>
                {
                    switch (s)
                    {
                        case "front":
                            return f == 0 && b == door ? "door_rect" : "win_rect_s";
                        case "back":
                            return f == 0 && b == bays - 1 ? "door_rect" : b % 2 == 0 ? "win_rect_s" : "plain";
                        default:
                            return f == 0 ? "plain" : "win_rect_s";
                    }
                },
            });
        }

        /// <summary>A warehouse of three storeys under a gable: a loading door, its storeys' windows, arches at the top.</summary>
        public static VicHouseSpec Warehouse(string name, int w, string palette, VicEnd left, VicEnd right)
        {
            var bays = (w - 2) / 2;
            var door = bays / 2;
            return Row(new RowOptions
            {
                Name = name, Width = w, Depth = 6, Floors = 3, Roof = "gable", Left = left, Right = right, Palette = Palette(palette),
                Kind = $"warehouse {w}",
                Key = (s, b, f) =>
                {
                    switch (s)
                    {
                        case "front":
                            return f == 0 ? (b == door ? "door_arch_double" : "plain") : b == door ? "win_rect_d" : f == 2 ? "win_arch_s" : "win_rect_s";
                        case "back":
                            return b % 2 == 0 && f > 0 ? "win_rect_s" : "plain";
                        default:
                            return f == 0 ? "plain" : "win_rect_s";
                    }
                },
            });
        }

        /// <summary>A detached house of 8 × 8 m: three storeys, a mansard, quoins, bay windows and a doorcase at its door.</summary>
        public static VicHouseSpec Detached(string name, string palette)
        {
            var stucco = palette == "stucco" || palette == "white";
            return Row(new RowOptions
            {
                Name = name, Width = 8, Depth = 8, Floors = 3, Palette = Palette(palette), Kind = "detached", Quoins = true, Stucco = stucco,
                Porch = 1, ArchLucarnes = true, StoneRail = (s, b) => s == "front",
                Key = (s, b, f) =>
                {
                    switch (s)
                    {
                        case "front":
                            return f == 0 ? (b == 1 ? "door_arch" : "bay") : f == 1 ? (b == 1 ? "win_arch_d" : "win_rect_d") : "win_arch_s";
                        case "back":
                            return f == 0 && b == 2 ? "door_rect" : "win_rect_s";
                        default:
                            return f == 2 ? "win_arch_s" : "win_rect_s";
                    }
                },
            });
        }

        /// <summary>
        /// A villa: 12 × 10 m with a 4 × 2 m pavilion, three storeys. A doorcase at the pavilion's door and an iron balcony
        /// over it (Plans/VEILMOUTH_REFERENCE_PLAN.md R7.3: the Ridge's balconies have iron rails), bay windows beside, lucarnes on the main fronts, chimneys and the vane on the flat (as R3, smaller).
        /// </summary>
        public static VicHouseSpec Villa(string name, string palette)
        {
            var h = new VicHouse(name);
            var plan = new VicPlan().Rect(0, 2, 12, 10).Rect(4, 0, 4, 2).Trace();
            var stucco = palette == "stucco" || palette == "white";
            var facade = new VicFacade
            {
                Quoin = c => true,
                Stucco = stucco,
                StoneRail = (e, b) => e.Side == "front",
                Key = (e, b, f) =>
                {
                    if (e.Side == "front" && e.Y0 == 0)
                    {
                        return f == 0 ? "door_double" : f == 1 ? "win_arch_d" : "win_arch_s";
                    }

                    if (e.Side == "front")
                    {
                        return f == 0 ? "bay" : f == 1 ? "win_rect_d" : "win_arch_s";
                    }

                    if (e.Side == "back")
                    {
                        return f == 0 && b == 4 ? "door_rect" : b % 2 == 1 ? "plain" : f == 2 ? "win_arch_s" : "win_rect_s";
                    }

                    return f == 2 ? "win_arch_s" : "win_rect_s";
                },
            };
            VicRoofs.Shell(h, plan, 3, facade);
            VicDress.Doorcase(h, new V3(5, 0, VicKit.FloorZ(0)), 0.0);
            VicDress.Balcony(h, new V3(5, 0, VicKit.FloorZ(1)), 0.0, VicKit.FloorZ(1) + VicKit.WinSill - 0.02, false);
            var o = new VicMansardOptions();
            o.Chimneys.Add((1, 3));
            o.Chimneys.Add((4, 3));
            o.Vane = (2, 2);
            o.Lucarnes.Add((2, 1.0, false, false));
            o.Lucarnes.Add((6, 3.0, false, false));
            VicRoofs.Mansard(h, plan, 3, o);
            return Spec(h, Palette(palette), plan, 3, "mansard", "villa");
        }

        /// <summary>
        /// A civic building: a main body <paramref name="w"/> × 10 m with a centred pavilion <paramref name="pavilion"/> × 2 m,
        /// three storeys. Its door, double, in the pavilion's middle bay in a doorcase under a stone balcony; tall windows,
        /// arched on the piano nobile; quoins; lucarnes over the pavilion and the main fronts; chimneys and a vane.
        /// </summary>
        public static VicHouseSpec Civic(string name, string palette, int w = 24, int pavilion = 8, int floors = 3)
        {
            var h = new VicHouse(name);
            var px = (w - pavilion) / 2;
            var plan = new VicPlan().Rect(0, 2, w, 10).Rect(px, 0, pavilion, 2).Trace();
            var stucco = palette == "stucco" || palette == "white";
            var door = ((pavilion - 2) / 2) / 2;
            var facade = new VicFacade
            {
                Quoin = c => true,
                Stucco = stucco,
                StoneRail = (e, b) => true,
                Key = (e, b, f) =>
                {
                    if (e.Side == "front" && e.Y0 == 0)
                    {
                        // The pavilion's ground-floor windows are single, so the doorcase's pilasters stand clear between them.
                        return f == 0 ? (b == door ? "door_arch_double" : "win_arch_s") : f == 1 ? "win_arch_d" : "win_arch_s";
                    }

                    if (e.Side == "front")
                    {
                        return f == 1 ? "win_arch_d" : f == 0 ? "win_rect_d" : "win_rect_s";
                    }

                    if (e.Side == "back")
                    {
                        return f == 0 && b == 1 ? "door_rect" : f == 1 ? "win_arch_s" : "win_rect_s";
                    }

                    return f == 1 ? "win_arch_s" : "win_rect_s";
                },
            };
            VicRoofs.Shell(h, plan, floors, facade);
            var doorX = px + 1 + (2 * door);
            VicDress.Doorcase(h, new V3(doorX, 0, VicKit.FloorZ(0)), 0.0, wide: true);
            VicDress.Balcony(h, new V3(doorX, 0, VicKit.FloorZ(1)), 0.0, VicKit.FloorZ(1) + VicKit.WinSill - 0.02, true);
            var o = new VicMansardOptions();
            o.Chimneys.Add((1, 3));
            o.Chimneys.Add(((w / 2) - 2, 3));
            o.Vane = ((w / 4) - 1 + (w % 8 == 0 ? 0 : 0), 3);
            o.Lucarnes.Add((0, pavilion * 0.5, true, false));
            var side = px * 0.5;
            o.Lucarnes.Add((2, side, false, false));
            o.Lucarnes.Add((plan.Edges.Count - 2, side, false, false));
            VicRoofs.Mansard(h, plan, floors, o);
            return Spec(h, Palette(palette), plan, floors, "mansard", "civic");
        }

        /// <summary>The Soot &amp; Bell: 12 × 12 m, three storeys, a mansard; its yard door on its right side near the back.</summary>
        public static VicHouseSpec Tavern(string name, string palette)
        {
            return Row(new RowOptions
            {
                Name = name, Width = 12, Depth = 12, Floors = 3, Palette = Palette(palette), Kind = "tavern",
                Key = (s, b, f) =>
                {
                    switch (s)
                    {
                        case "front":
                            return f == 0 ? (b == 2 ? "door_double" : "win_rect_d") : f == 2 ? "win_arch_s" : "win_rect_s";
                        case "right":
                            return f == 0 ? (b == YardDoorBay ? "door_rect" : "plain") : "win_rect_s";
                        case "back":
                            return b % 2 == 1 ? "plain" : "win_rect_s";
                        default:
                            return f == 0 ? "win_rect_d" : "win_rect_s";
                    }
                },
            });
        }

        /// <summary>The tavern's yard door: this bay of its right side, counted from the front.</summary>
        public const int YardDoorBay = 4;

        /// <summary>
        /// A tower 4 × 4 m of <paramref name="floors"/> storeys under a pyramid of four hip corners (the kit's Hip with no
        /// run between its corners and no chimney on its apex): a door on the front's ground storey, or none.
        /// </summary>
        public static VicHouseSpec Tower(string name, int floors, string palette, bool door)
        {
            var h = new VicHouse(name);
            var plan = new VicPlan().Rect(0, 0, 4, 4).Trace();
            var facade = new VicFacade
            {
                Quoin = c => true,
                Key = (e, b, f) => f == 0 ? (door && e.Side == "front" ? "door_arch" : "plain") : f == floors - 1 ? "win_arch_s" : f % 2 == 0 ? "win_rect_s" : "plain",
            };
            VicRoofs.Shell(h, plan, floors, facade);
            var spring = VicKit.Spring(floors);
            foreach (var (x, y, rot) in new[] { (0.0, 0.0, 0.0), (4.0, 0.0, VicDress.HalfPi), (4.0, 4.0, Math.PI), (0.0, 4.0, -VicDress.HalfPi) })
            {
                h.Drop("SM_Vic_Roof_HipCorner_D4_Deck", new V3(x, y, spring), rot);
                h.Drop("SM_Vic_Roof_HipCap_D4", new V3(x, y, spring), rot);
            }

            VicRoofs.Eaves(h, plan, spring, VicKit.Tan);
            VicDress.Downpipe(h, new V3(0.45, -VicKit.Overhang, VicKit.EaveZ(spring, VicKit.Tan)), 0.0);
            var spec = Spec(h, Palette(palette), plan, floors, "hip", "tower");
            spec.Ridge = spring + (2.0 * VicKit.Tan);
            return spec;
        }

        // ------------------------------------------------------------------------------------------------ helpers

        public static VicHouseSpec Spec(VicHouse h, VicPalette palette, VicPlan plan, int floors, string roof, string kind)
        {
            var spring = VicKit.Spring(floors);
            var ridge = roof == "mansard" ? spring + VicKit.MansardRise : spring + (VicKit.HalfSpan(plan.Depth) * VicKit.Tan);
            return new VicHouseSpec
            {
                Name = h.Name, Palette = palette, Width = plan.Width, Depth = plan.Depth, Floors = floors, Roof = roof, House = h,
                PlanKind = kind, Plan = plan, Ridge = ridge,
            };
        }

        private static readonly Dictionary<string, (double x0, double x1, double y0, double y1)> Extents =
            new Dictionary<string, (double, double, double, double)>();

        /// <summary>
        /// A module's footprint in its own kit frame, from its mesh: the mesh holds the Blender point (x, y, z) as
        /// (−x, y, z) under its node's scale (VicHouseFactory).
        /// </summary>
        public static (double x0, double x1, double y0, double y1) Extent(string id)
        {
            if (Extents.TryGetValue(id, out var e))
            {
                return e;
            }

            var prefab = VicKitPaths.LoadModule(id);
            if (prefab == null)
            {
                throw new InvalidOperationException($"The kit has no module {id}.");
            }

            double x0 = double.MaxValue, x1 = double.MinValue, y0 = double.MaxValue, y1 = double.MinValue;
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                var b = filter.sharedMesh.bounds;
                var s = filter.transform.lossyScale.x;
                x0 = Math.Min(x0, -b.max.x * s);
                x1 = Math.Max(x1, -b.min.x * s);
                y0 = Math.Min(y0, b.min.y * s);
                y1 = Math.Max(y1, b.max.y * s);
            }

            e = x0 == double.MaxValue ? (0, 0, 0, 0) : (x0, x1, y0, y1);
            Extents[id] = e;
            return e;
        }

        /// <summary>A placed piece's reach along the house's X, from its module's footprint turned by its yaw.</summary>
        public static (double lo, double hi) Span(VicPiece p)
        {
            var (x0, x1, y0, y1) = Extent(p.Id);
            var rot = p.Yaw * Math.PI / 180.0;
            double lo = double.MaxValue, hi = double.MinValue;
            foreach (var (cx, cy) in new[] { (x0, y0), (x1, y0), (x1, y1), (x0, y1) })
            {
                var (rx, _) = VicKit.Rotated(cx, cy, rot);
                lo = Math.Min(lo, p.X + rx);
                hi = Math.Max(hi, p.X + rx);
            }

            return (lo, hi);
        }
    }
}

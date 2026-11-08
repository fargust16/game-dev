using System;
using System.Collections.Generic;
using System.Linq;

namespace GameDev.VictorianKit.EditorTools
{
    /// <summary>A straight run of the outline between two corners, walked counter-clockwise: the house is on its left.</summary>
    public sealed class VicEdge
    {
        public int Index;
        public int X0, Y0, X1, Y1;

        /// <summary>Unit direction of the walk.</summary>
        public int Dx, Dy;

        public bool StartConvex, EndConvex;

        public int Length => Math.Abs(X1 - X0) + Math.Abs(Y1 - Y0);

        /// <summary>The walk's direction as a kit rotation: the house on local +Y, the facade facing local −Y.</summary>
        public double Rot => Math.Atan2(Dy, Dx);

        /// <summary>Unit vector out of the house, to the walk's right.</summary>
        public (int x, int y) Out => (Dy, -Dx);

        /// <summary>Which side of the house the edge faces: front (−Y, the street), back, left (−X) or right.</summary>
        public string Side => Dx == 1 ? "front" : Dx == -1 ? "back" : Dy == 1 ? "right" : "left";

        public double Mid(int axis) => axis == 0 ? (X0 + X1) * 0.5 : (Y0 + Y1) * 0.5;
    }

    /// <summary>A corner of the outline: convex (a street corner) or a court's re-entrant corner.</summary>
    public sealed class VicCorner
    {
        public int X, Y;
        public bool Convex;
        public VicEdge In, Out;

        /// <summary>The empty quarter seen from a re-entrant corner, as signs of X and Y.</summary>
        public (int x, int y) Empty => (In.Out.x + Out.Out.x, In.Out.y + Out.Out.y);
    }

    /// <summary>
    /// A house's footprint on the 2 m grid: cells of 2 × 2 m in the kit's frame (X along the street, Y back from it,
    /// the street facade on Y = 0). It yields the outline walked counter-clockwise from the front-left corner, its
    /// straight edges and its corners. One outline, no holes.
    /// </summary>
    public sealed class VicPlan
    {
        private readonly HashSet<(int i, int j)> cells = new HashSet<(int, int)>();

        public IReadOnlyCollection<(int i, int j)> Cells => cells;

        public readonly List<VicEdge> Edges = new List<VicEdge>();
        public readonly List<VicCorner> Corners = new List<VicCorner>();

        /// <summary>Adds a rectangle of whole cells: x, y, width and depth in metres, all even.</summary>
        public VicPlan Rect(int x, int y, int width, int depth)
        {
            if (x % 2 != 0 || y % 2 != 0 || width % 2 != 0 || depth % 2 != 0)
            {
                throw new ArgumentException("A plan's rectangles sit on the 2 m grid.");
            }

            for (var i = x / 2; i < (x + width) / 2; i++)
            {
                for (var j = y / 2; j < (y + depth) / 2; j++)
                {
                    cells.Add((i, j));
                }
            }

            return this;
        }

        public bool Inside(int i, int j) => cells.Contains((i, j));

        public int Width => (cells.Max(c => c.i) + 1 - cells.Min(c => c.i)) * 2;

        public int Depth => (cells.Max(c => c.j) + 1 - cells.Min(c => c.j)) * 2;

        public int MinX => cells.Min(c => c.i) * 2;

        public int MinY => cells.Min(c => c.j) * 2;

        /// <summary>Walks the outline into <see cref="Edges"/> and <see cref="Corners"/>. Throws on a hole or a 2 m wing.</summary>
        public VicPlan Trace()
        {
            Edges.Clear();
            Corners.Clear();
            var next = new Dictionary<(int, int), (int, int)>();
            foreach (var (i, j) in cells)
            {
                int x = i * 2, y = j * 2;
                if (!Inside(i, j - 1))
                {
                    next.Add((x, y), (x + 2, y));
                }

                if (!Inside(i + 1, j))
                {
                    next.Add((x + 2, y), (x + 2, y + 2));
                }

                if (!Inside(i, j + 1))
                {
                    next.Add((x + 2, y + 2), (x, y + 2));
                }

                if (!Inside(i - 1, j))
                {
                    next.Add((x, y + 2), (x, y));
                }
            }

            var start = next.Keys.OrderBy(p => p.Item2).ThenBy(p => p.Item1).First();
            var loop = new List<(int x, int y)> { start };
            var at = next[start];
            while (at != start)
            {
                loop.Add(at);
                at = next[at];
            }

            if (loop.Count != next.Count)
            {
                throw new InvalidOperationException("A plan has one outline and no hole.");
            }

            // Corners are the turning points; the start is a convex corner by its choice.
            var turns = new List<(int x, int y)>();
            for (var k = 0; k < loop.Count; k++)
            {
                var a = loop[(k + loop.Count - 1) % loop.Count];
                var b = loop[k];
                var c = loop[(k + 1) % loop.Count];
                if ((b.x - a.x) != (c.x - b.x) || (b.y - a.y) != (c.y - b.y))
                {
                    turns.Add(b);
                }
            }

            for (var k = 0; k < turns.Count; k++)
            {
                var a = turns[k];
                var b = turns[(k + 1) % turns.Count];
                Edges.Add(new VicEdge
                {
                    Index = k,
                    X0 = a.x,
                    Y0 = a.y,
                    X1 = b.x,
                    Y1 = b.y,
                    Dx = Math.Sign(b.x - a.x),
                    Dy = Math.Sign(b.y - a.y),
                });
            }

            for (var k = 0; k < Edges.Count; k++)
            {
                var incoming = Edges[(k + Edges.Count - 1) % Edges.Count];
                var outgoing = Edges[k];
                var cross = (incoming.Dx * outgoing.Dy) - (incoming.Dy * outgoing.Dx);
                var corner = new VicCorner { X = outgoing.X0, Y = outgoing.Y0, Convex = cross > 0, In = incoming, Out = outgoing };
                Corners.Add(corner);
                incoming.EndConvex = corner.Convex;
                outgoing.StartConvex = corner.Convex;
            }

            foreach (var e in Edges)
            {
                if (e.Length < 4 && !(e.StartConvex ^ e.EndConvex))
                {
                    throw new InvalidOperationException($"Edge {e.Index} is {e.Length} m: a wing under 4 m has no roof in the kit.");
                }
            }

            return this;
        }

        /// <summary>
        /// The roof height in plan metres at a point inside the outline: its L∞ distance to the outside. For an
        /// orthogonal outline this is the 45°-in-plan skeleton every kit roof follows; a mansard clips it at its inset.
        /// </summary>
        public double Reach(double x, double y)
        {
            var best = double.MaxValue;
            var i0 = (int)Math.Floor(x / 2.0);
            var j0 = (int)Math.Floor(y / 2.0);
            for (var r = 0; r < 64 && r * 2.0 - 2.0 < best; r++)
            {
                for (var i = i0 - r; i <= i0 + r; i++)
                {
                    for (var j = j0 - r; j <= j0 + r; j++)
                    {
                        if (Math.Max(Math.Abs(i - i0), Math.Abs(j - j0)) != r || Inside(i, j))
                        {
                            continue;
                        }

                        var dx = Math.Max(0.0, Math.Max(i * 2.0 - x, x - (i * 2.0 + 2.0)));
                        var dy = Math.Max(0.0, Math.Max(j * 2.0 - y, y - (j * 2.0 + 2.0)));
                        best = Math.Min(best, Math.Max(dx, dy));
                    }
                }
            }

            return best;
        }

        /// <summary>A rotation that puts the given quarter on local +X/+Y: the convention of every kit corner piece.</summary>
        public static double QuarterRot(int sx, int sy)
        {
            if (sx > 0 && sy > 0)
            {
                return 0.0;
            }

            if (sx < 0 && sy > 0)
            {
                return VicDress.HalfPi;
            }

            if (sx < 0 && sy < 0)
            {
                return Math.PI;
            }

            return -VicDress.HalfPi;
        }
    }
}

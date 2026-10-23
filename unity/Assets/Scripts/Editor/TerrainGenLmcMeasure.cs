#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GameDev.World;
using GameDev.World.Hydrology;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>A declared drainage axis, ordered from its source to its mouth (spec §7.1 R7 (c)).</summary>
    public sealed class LmcAxis
    {
        public string Name;
        public List<Vector2> Points = new List<Vector2>();
    }

    /// <summary>
    /// What a G3e (LMC) reading needs: a lattice and its region map, plus — for LMC candidates — the layout and the
    /// synthesis' own drainage, or — for the continuity check on G3c's grids — G3c's recorded sites and summit.
    /// </summary>
    public sealed class LmcSubject
    {
        public string Name;
        public int Seed;
        public int N;
        public float Cell;
        public float[] H;
        public byte[] Region;
        public int[] Instance;
        public LmcLayout Layout;
        public LmcContour Contour;
        public float[] ConfiguredAngles = new float[0];
        public List<LmcAxis> Axes = new List<LmcAxis>();
        public int[] ChannelReceivers;
        public double[] ChannelArea;
        public Vector2? SummitHint;
        public List<Vector2> RecordedSites;
    }

    /// <summary>Shared fields of one lattice, in float64 as the Python reference computes them (spec §1).</summary>
    public sealed class LmcBase
    {
        public const double Sea = 24.0;
        public const double Tan15 = 0.26794919243112270;

        public int N;
        public double Cell;
        public double[] H;
        public bool[] Land;
        public bool[] Ocean;
        public bool[] Shore;
        public double[] Gx;
        public double[] Gz;
        public double[] VertexSlope;
        public double[] TriSlope;
        public bool[] TriLand;
        public bool[] TriWalk;
        public double[] VertexMaxTri;
        public int[] TriLabel;
        public bool[] TriShoreReach;
        public bool[] WalkVertex;
        public double[] Filled;
        public int[] Recv;
        public double[] Area;
        public int[] Order;

        public int Q => N - 1;

        public static LmcBase Build(float[] h32, int n, float cell)
        {
            var b = new LmcBase { N = n, Cell = cell, H = new double[n * n] };
            var count = n * n;
            for (var i = 0; i < count; i++) b.H[i] = h32[i];
            b.Land = new bool[count];
            for (var i = 0; i < count; i++) b.Land[i] = b.H[i] > Sea;
            b.Ocean = OceanMask(b.H, n);
            b.Shore = new bool[count];
            for (var z = 0; z < n; z++)
            for (var x = 0; x < n; x++)
            {
                var i = z * n + x;
                if (!b.Land[i]) continue;
                b.Shore[i] = (z > 0 && b.Ocean[i - n]) || (z < n - 1 && b.Ocean[i + n]) || (x > 0 && b.Ocean[i - 1]) || (x < n - 1 && b.Ocean[i + 1]);
            }

            Gradient(b.H, n, b.Cell, out b.Gx, out b.Gz);
            b.VertexSlope = new double[count];
            for (var i = 0; i < count; i++) b.VertexSlope[i] = Math.Atan(Math.Sqrt(b.Gx[i] * b.Gx[i] + b.Gz[i] * b.Gz[i])) * 180.0 / Math.PI;
            b.Triangles();
            b.TEdge();
            b.Flood();
            return b;
        }

        /// <summary>numpy.gradient: central differences inside, one-sided at the border.</summary>
        public static void Gradient(double[] h, int n, double cell, out double[] gx, out double[] gz)
        {
            gx = new double[n * n];
            gz = new double[n * n];
            for (var z = 0; z < n; z++)
            for (var x = 0; x < n; x++)
            {
                var i = z * n + x;
                gx[i] = x == 0 ? (h[i + 1] - h[i]) / cell : x == n - 1 ? (h[i] - h[i - 1]) / cell : (h[i + 1] - h[i - 1]) / (2.0 * cell);
                gz[i] = z == 0 ? (h[i + n] - h[i]) / cell : z == n - 1 ? (h[i] - h[i - n]) / cell : (h[i + n] - h[i - n]) / (2.0 * cell);
            }
        }

        public static bool[] OceanMask(double[] h, int n)
        {
            var ocean = new bool[n * n];
            var queue = new Queue<int>();
            void Seed(int i)
            {
                if (h[i] <= Sea && !ocean[i]) { ocean[i] = true; queue.Enqueue(i); }
            }

            for (var z = 0; z < n; z++) { Seed(z * n); Seed(z * n + n - 1); }
            for (var x = 0; x < n; x++) { Seed(x); Seed((n - 1) * n + x); }
            while (queue.Count > 0)
            {
                var i = queue.Dequeue();
                var x = i % n; var z = i / n;
                if (z + 1 < n) Seed(i + n);
                if (z > 0) Seed(i - n);
                if (x + 1 < n) Seed(i + 1);
                if (x > 0) Seed(i - 1);
            }

            return ocean;
        }

        private void Triangles()
        {
            var n = N;
            var q = Q;
            TriSlope = new double[2 * q * q];
            TriLand = new bool[2 * q * q];
            TriWalk = new bool[2 * q * q];
            VertexMaxTri = new double[n * n];
            for (var z = 0; z < q; z++)
            for (var x = 0; x < q; x++)
            {
                var h00 = H[z * n + x];
                var h10 = H[z * n + x + 1];
                var h01 = H[(z + 1) * n + x];
                var h11 = H[(z + 1) * n + x + 1];
                var s1 = Hypot((h10 - h00) / Cell, (h11 - h10) / Cell);
                var s2 = Hypot((h11 - h01) / Cell, (h01 - h00) / Cell);
                var t = 2 * (z * q + x);
                TriSlope[t] = Math.Atan(s1) * 180.0 / Math.PI;
                TriSlope[t + 1] = Math.Atan(s2) * 180.0 / Math.PI;
                TriLand[t] = (h00 + h10 + h11) / 3.0 > Sea;
                TriLand[t + 1] = (h00 + h11 + h01) / 3.0 > Sea;
                TriWalk[t] = TriLand[t] && s1 < 1.0;
                TriWalk[t + 1] = TriLand[t + 1] && s2 < 1.0;
                var m = Math.Atan(Math.Max(s1, s2)) * 180.0 / Math.PI;
                foreach (var v in new[] { z * n + x, z * n + x + 1, (z + 1) * n + x, (z + 1) * n + x + 1 })
                {
                    if (m > VertexMaxTri[v]) VertexMaxTri[v] = m;
                }
            }
        }

        public static double Hypot(double a, double b) => Math.Sqrt(a * a + b * b);

        public static void Nb(int t, int q, out int a, out int b, out int c)
        {
            var quad = t >> 1;
            var x = quad % q;
            var z = quad / q;
            if ((t & 1) == 0)
            {
                a = t + 1;
                b = z > 0 ? 2 * ((z - 1) * q + x) + 1 : -1;
                c = x < q - 1 ? 2 * (z * q + x + 1) + 1 : -1;
            }
            else
            {
                a = t - 1;
                b = x > 0 ? 2 * (z * q + x - 1) : -1;
                c = z < q - 1 ? 2 * ((z + 1) * q + x) : -1;
            }
        }

        private void TEdge()
        {
            var q = Q;
            var count = 2 * q * q;
            TriLabel = new int[count];
            for (var t = 0; t < count; t++) TriLabel[t] = -1;
            var compShore = new List<bool>();
            var queue = new Queue<int>();
            var comp = 0;
            for (var t0 = 0; t0 < count; t0++)
            {
                if (!TriWalk[t0] || TriLabel[t0] != -1) continue;
                TriLabel[t0] = comp;
                var touches = false;
                queue.Enqueue(t0);
                while (queue.Count > 0)
                {
                    var t = queue.Dequeue();
                    Nb(t, q, out var a, out var b, out var c);
                    foreach (var u in new[] { a, b, c })
                    {
                        if (u < 0 || !TriLand[u]) { touches = true; continue; }
                        if (TriWalk[u] && TriLabel[u] == -1) { TriLabel[u] = comp; queue.Enqueue(u); }
                    }
                }

                compShore.Add(touches);
                comp++;
            }

            TriShoreReach = new bool[count];
            for (var t = 0; t < count; t++) TriShoreReach[t] = TriLabel[t] >= 0 && compShore[TriLabel[t]];

            // walkable vertices: every touching triangle walkable (spec §7.1 R9, K2)
            var n = N;
            WalkVertex = new bool[n * n];
            for (var z = 1; z < n - 1; z++)
            for (var x = 1; x < n - 1; x++)
            {
                var ok = true;
                for (var dz = -1; dz <= 0 && ok; dz++)
                for (var dx = -1; dx <= 0 && ok; dx++)
                {
                    var tq = 2 * ((z + dz) * q + x + dx);
                    ok = TriWalk[tq] && TriWalk[tq + 1];
                }

                WalkVertex[z * n + x] = ok;
            }
        }

        /// <summary>The T-edge component of a vertex and whether it is shore-reachable (Python <c>vertex_component</c>).</summary>
        public int VertexComponent(int x, int z, out bool reachable, out int triangle)
        {
            var q = Q;
            var best = -1;
            triangle = -1;
            reachable = false;
            for (var dz = -1; dz <= 0; dz++)
            for (var dx = -1; dx <= 0; dx++)
            {
                var qx = x + dx; var qz = z + dz;
                if (qx < 0 || qz < 0 || qx >= q || qz >= q) continue;
                var t0 = 2 * (qz * q + qx);
                for (var t = t0; t <= t0 + 1; t++)
                {
                    if (TriLabel[t] < 0) continue;
                    if (TriShoreReach[t]) { reachable = true; triangle = t; return TriLabel[t]; }
                    best = TriLabel[t];
                    triangle = t;
                }
            }

            return best;
        }

        public bool VertexReachable(int x, int z)
        {
            VertexComponent(x, z, out var reachable, out _);
            return reachable;
        }

        public Vector2 Centroid(int t)
        {
            var q = Q;
            var quad = t >> 1;
            var x = quad % q;
            var z = quad / q;
            return (t & 1) == 0
                ? new Vector2((float)((x + 2.0 / 3.0) * Cell), (float)((z + 1.0 / 3.0) * Cell))
                : new Vector2((float)((x + 1.0 / 3.0) * Cell), (float)((z + 2.0 / 3.0) * Cell));
        }

        /// <summary>Priority flood (ε 1 mm, 8-connected) to the ocean and D8 on the filled surface (Python <c>flood_d8</c>).</summary>
        private void Flood()
        {
            var n = N;
            var count = n * n;
            var hh = (double[])H.Clone();
            var done = (bool[])Ocean.Clone();
            var heap = new LmcHeapD(1 << 16);
            for (var i = 0; i < count; i++)
            {
                if (Ocean[i]) continue;
                var x = i % n; var z = i / n;
                var seed = x == 0 || z == 0 || x == n - 1 || z == n - 1;
                if (!seed)
                {
                    foreach (var j in new[] { i - 1, i + 1, i - n, i + n, i - n - 1, i - n + 1, i + n - 1, i + n + 1 })
                    {
                        if (Ocean[j]) { seed = true; break; }
                    }
                }

                if (seed) { heap.Push(hh[i], i); done[i] = true; }
            }

            while (heap.Count > 0)
            {
                heap.Pop(out var hz, out var i);
                var x = i % n; var z = i / n;
                for (var dz = -1; dz <= 1; dz++)
                {
                    var zz = z + dz;
                    if (zz < 0 || zz >= n) continue;
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        if (dz == 0 && dx == 0) continue;
                        var xx = x + dx;
                        if (xx < 0 || xx >= n) continue;
                        var j = zz * n + xx;
                        if (done[j]) continue;
                        done[j] = true;
                        var v = hh[j];
                        if (v < hz + 1e-3) { v = hz + 1e-3; hh[j] = v; }
                        heap.Push(v, j);
                    }
                }
            }

            Filled = hh;
            Recv = new int[count];
            var best = new double[count];
            for (var i = 0; i < count; i++) Recv[i] = -1;
            for (var dz = -1; dz <= 1; dz++)
            for (var dx = -1; dx <= 1; dx++)
            {
                if (dz == 0 && dx == 0) continue;
                var d = Cell * Hypot(dz, dx);
                for (var z = 0; z < n; z++)
                {
                    var zz = z + dz;
                    for (var x = 0; x < n; x++)
                    {
                        var i = z * n + x;
                        if (Ocean[i]) continue;
                        var xx = x + dx;
                        double drop;
                        var nbOcean = false;
                        var j = -1;
                        if (zz < 0 || zz >= n || xx < 0 || xx >= n)
                        {
                            drop = double.NegativeInfinity;
                        }
                        else
                        {
                            j = zz * n + xx;
                            nbOcean = Ocean[j];
                            drop = nbOcean ? double.PositiveInfinity : (Filled[i] - Filled[j]) / d;
                        }

                        if (drop > best[i])
                        {
                            best[i] = drop;
                            Recv[i] = nbOcean ? -1 : j;
                        }
                    }
                }
            }

            var land = new List<int>();
            for (var i = 0; i < count; i++) if (!Ocean[i]) land.Add(i);
            var keys = new double[land.Count];
            for (var k = 0; k < land.Count; k++) keys[k] = Filled[land[k]];
            var idx = land.ToArray();
            Array.Sort(keys, idx, Comparer<double>.Default); // ties: indices were ascending, Array.Sort is not stable
            // restore stability: sort ties by index
            var start = 0;
            for (var k = 1; k <= idx.Length; k++)
            {
                if (k == idx.Length || keys[k] != keys[start])
                {
                    if (k - start > 1) Array.Sort(idx, start, k - start);
                    start = k;
                }
            }

            Order = idx;
            Area = new double[count];
            var a0 = Cell * Cell;
            for (var i = 0; i < count; i++) Area[i] = Ocean[i] ? 0.0 : a0;
            for (var k = Order.Length - 1; k >= 0; k--)
            {
                var i = Order[k];
                var r = Recv[i];
                if (r >= 0) Area[r] += Area[i];
            }
        }
    }

    public sealed class LmcHeapD
    {
        private double[] m_keys;
        private int[] m_values;

        public int Count { get; private set; }

        public LmcHeapD(int capacity)
        {
            m_keys = new double[Mathf.Max(16, capacity)];
            m_values = new int[m_keys.Length];
        }

        public void Push(double key, int value)
        {
            if (Count == m_keys.Length)
            {
                Array.Resize(ref m_keys, Count * 2);
                Array.Resize(ref m_values, Count * 2);
            }

            var i = Count++;
            while (i > 0)
            {
                var parent = (i - 1) / 2;
                if (Less(m_keys[parent], m_values[parent], key, value)) break;
                m_keys[i] = m_keys[parent];
                m_values[i] = m_values[parent];
                i = parent;
            }

            m_keys[i] = key;
            m_values[i] = value;
        }

        public void Pop(out double key, out int value)
        {
            key = m_keys[0];
            value = m_values[0];
            Count--;
            if (Count == 0) return;
            var lastKey = m_keys[Count];
            var lastValue = m_values[Count];
            var i = 0;
            while (true)
            {
                var child = 2 * i + 1;
                if (child >= Count) break;
                if (child + 1 < Count && Less(m_keys[child + 1], m_values[child + 1], m_keys[child], m_values[child])) child++;
                if (Less(lastKey, lastValue, m_keys[child], m_values[child])) break;
                m_keys[i] = m_keys[child];
                m_values[i] = m_values[child];
                i = child;
            }

            m_keys[i] = lastKey;
            m_values[i] = lastValue;
        }

        private static bool Less(double ka, int va, double kb, int vb) => ka < kb || (ka == kb && va < vb);
    }

    public sealed class LmcPlateauReading
    {
        public int Id;
        public double TopKm2;
        public int Rim;
        public double EdgeHeight = double.NaN;
        public double EscShare;
        public double JoinedShare;
        public double Cv = double.NaN;
        public int Reentrants;
        public double ClassMin;
        public double ClassMax;
        public double DeclaredEdge = double.NaN;
        public bool ClassFlag;
        public double TopMedianHeight;
        public int TopVertex = -1;
        public bool TopReachable;
        public List<int> Feet = new List<int>();
        public List<int> EscSamples = new List<int>();
    }

    /// <summary>Every G3e reading of one lattice (spec §§2, 7, 8, 13): gates, indicators, diagnostics, Q1–Q9.</summary>
    public sealed class LmcReadings
    {
        public Dictionary<string, object> Values = new Dictionary<string, object>();
        public List<string> Lines = new List<string>();
        public List<LmcPlateauReading> Plateaus = new List<LmcPlateauReading>();
        public List<G3cSite> Sites = new List<G3cSite>();
        public List<string> Flags = new List<string>();
        public Dictionary<string, bool> Gates = new Dictionary<string, bool>();
        public int PassCol = -1;
        public Vector2 MainSummit;
        public List<(int index, int cls)> ShoreClass = new List<(int, int)>();

        public void Set(string key, object value) => Values[key] = value;
    }

    public static class LmcMeasure
    {
        public const double BuildableDegrees = 6.84;
        public const float SiteFraction = 0.90f;
        public const float SiteSpacing = 450f;
        public const float CoastalReach = 100f;

        // ------------------------------------------------------------------ §2.2 summit

        public static int MainSummitIndex(LmcBase b, byte[] region)
        {
            var best = -1;
            for (var i = 0; i < b.H.Length; i++)
            {
                if (region[i] == LmcCodes.Highland && b.Land[i] && (best < 0 || b.H[i] > b.H[best])) best = i;
            }

            return best;
        }

        // ------------------------------------------------------------------ Q1 (spec §8)

        public static void Q1(LmcBase b, byte[] region, out double all, out Dictionary<int, double> byRegion)
        {
            var n = b.N;
            const int half = 7;
            const int k = 2 * half + 1;
            double su2 = 0;
            for (var u = -half; u <= half; u++) su2 += k * u * u;
            var cnt = 0; var hit = 0;
            var rc = new Dictionary<int, int[]>();
            for (var z = half; z < n - half; z++)
            for (var x = half; x < n - half; x++)
            {
                var i = z * n + x;
                if (!b.Land[i] || !(b.VertexSlope[i] > 5.0)) continue;
                var hc = b.H[i];
                double s = 0, sx = 0, sz = 0, s2 = 0;
                for (var dz = -half; dz <= half; dz++)
                {
                    var row = (z + dz) * n + x;
                    for (var dx = -half; dx <= half; dx++)
                    {
                        var v = b.H[row + dx] - hc;
                        s += v; sx += dx * v; sz += dz * v; s2 += v * v;
                    }
                }

                var ss = s2 - s * s / (k * k) - sx * sx / su2 - sz * sz / su2;
                var rms = Math.Sqrt(Math.Max(ss, 0.0) / (k * k));
                var planar = rms < 0.05;
                cnt++;
                if (planar) hit++;
                var code = region[i];
                if (!rc.TryGetValue(code, out var acc)) { acc = new int[2]; rc[code] = acc; }
                acc[0]++;
                if (planar) acc[1]++;
            }

            all = cnt > 0 ? hit / (double)cnt : double.NaN;
            byRegion = new Dictionary<int, double>();
            foreach (var kv in rc) byRegion[kv.Key] = kv.Value[1] / (double)kv.Value[0];
        }

        // ------------------------------------------------------------------ Q2

        public static double Q2(LmcBase b, out int systems)
        {
            var n = b.N;
            var count = n * n;
            var sys = new int[count];
            for (var i = 0; i < count; i++) sys[i] = -1;
            foreach (var i in b.Order)
            {
                var r = b.Recv[i];
                sys[i] = r < 0 ? i : sys[r];
            }

            var bySys = new Dictionary<int, List<(double la, double ls)>>();
            for (var i = 0; i < count; i++)
            {
                if (!(b.Area[i] >= 30000.0) || b.Recv[i] < 0) continue;
                var r = b.Recv[i];
                var d = b.Cell * LmcBase.Hypot(i / n - r / n, i % n - r % n);
                var s = (b.H[i] - b.H[r]) / d;
                if (!(s > 0)) continue;
                if (!bySys.TryGetValue(sys[i], out var list)) { list = new List<(double, double)>(); bySys[sys[i]] = list; }
                list.Add((Math.Log(b.Area[i]), Math.Log(s)));
            }

            var thetas = new List<double>();
            foreach (var kv in bySys)
            {
                var pts = kv.Value;
                if (pts.Count < 10) continue;
                double min = double.PositiveInfinity, max = double.NegativeInfinity, mx = 0, my = 0;
                foreach (var p in pts) { min = Math.Min(min, p.la); max = Math.Max(max, p.la); mx += p.la; my += p.ls; }
                if (max - min < 1e-6) continue;
                mx /= pts.Count; my /= pts.Count;
                double sxy = 0, sxx = 0;
                foreach (var p in pts) { sxy += (p.la - mx) * (p.ls - my); sxx += (p.la - mx) * (p.la - mx); }
                thetas.Add(-(sxy / sxx));
            }

            systems = thetas.Count;
            return Median(thetas);
        }

        // ------------------------------------------------------------------ Q3 and the crest set (Q4m)

        private static readonly (int dz, int dx, double f)[] CrestDirs =
        {
            (0, 1, 1.0), (1, 0, 1.0), (1, 1, 1.4142135623730951), (1, -1, 1.4142135623730951),
        };

        public static bool[] Crest(LmcBase b, bool[] mask, out double[] width, out double[] kappa)
        {
            var n = b.N;
            var count = n * n;
            var crestAny = new bool[count];
            width = new double[count];
            kappa = new double[count];
            for (var i = 0; i < count; i++) { width[i] = double.NaN; kappa[i] = double.NaN; }
            foreach (var (dz, dx, f) in CrestDirs)
            {
                var step = b.Cell * f;
                var k30 = (int)Math.Round(30.0 / step, MidpointRounding.ToEven);
                for (var z = 0; z < n; z++)
                for (var x = 0; x < n; x++)
                {
                    var i = z * n + x;
                    if (!mask[i]) continue;
                    var h = b.H[i];
                    if (!Inside(x + dx, z + dz, n) || !Inside(x - dx, z - dz, n)) continue;
                    if (!Inside(x + k30 * dx, z + k30 * dz, n) || !Inside(x - k30 * dx, z - k30 * dz, n)) continue;
                    var p1 = b.H[(z + dz) * n + x + dx];
                    var m1 = b.H[(z - dz) * n + x - dx];
                    var pk = b.H[(z + k30 * dz) * n + x + k30 * dx];
                    var mk = b.H[(z - k30 * dz) * n + x - k30 * dx];
                    if (!(h >= p1 && h >= m1 && h > Math.Max(pk, mk))) continue;
                    var kap = (pk + mk - 2 * h) / ((k30 * step) * (k30 * step));
                    var w = 1.0;
                    for (var sgn = -1; sgn <= 1; sgn += 2)
                    {
                        for (var s = 1; s <= 110; s++)
                        {
                            var xx = x + sgn * s * dx; var zz = z + sgn * s * dz;
                            if (!Inside(xx, zz, n) || !(b.H[zz * n + xx] >= h - 5.0)) break;
                            w += 1.0;
                        }
                    }

                    w *= step;
                    if (!crestAny[i] || w < width[i])
                    {
                        width[i] = w;
                        kappa[i] = kap;
                    }

                    crestAny[i] = true;
                }
            }

            return crestAny;
        }

        private static bool Inside(int x, int z, int n) => x >= 0 && z >= 0 && x < n && z < n;

        public static void Q3(LmcBase b, bool[] hmask, out double width, out double kappa, out int crestCount)
        {
            var crest = Crest(b, hmask, out var w, out var k);
            var ws = new List<double>();
            var ks = new List<double>();
            for (var i = 0; i < crest.Length; i++)
            {
                if (!crest[i]) continue;
                ws.Add(w[i]);
                ks.Add(k[i]);
            }

            crestCount = ws.Count;
            width = Median(ws);
            kappa = Median(ks);
        }

        // ------------------------------------------------------------------ Q4m (spec §13.2, K4)

        public static void Q4m(LmcBase b, bool[] hmask, Vector2? summit, out double ratio, out double peaksPerKm, out double lengthKm, out List<int> path)
        {
            var n = b.N;
            ratio = double.NaN; peaksPerKm = double.NaN; lengthKm = 0; path = new List<int>();
            var crest = Crest(b, hmask, out _, out _);
            var any = false;
            foreach (var c in crest) any |= c;
            if (!any) return;
            var dil = new bool[n * n];
            for (var z = 0; z < n; z++)
            for (var x = 0; x < n; x++)
            {
                if (!crest[z * n + x]) continue;
                for (var dz = -1; dz <= 1; dz++)
                for (var dx = -1; dx <= 1; dx++)
                {
                    var xx = (x + dx + n) % n; var zz = (z + dz + n) % n;
                    dil[zz * n + xx] = true;
                }
            }

            for (var i = 0; i < dil.Length; i++) dil[i] &= hmask[i];
            var comps = Components8(dil, n);
            List<int> comp = null;
            if (summit.HasValue)
            {
                var sz = (int)Math.Round(summit.Value.y / b.Cell, MidpointRounding.ToEven);
                var sx = (int)Math.Round(summit.Value.x / b.Cell, MidpointRounding.ToEven);
                var bestD = int.MaxValue;
                foreach (var cells in comps)
                {
                    foreach (var i in cells)
                    {
                        var d = (i / n - sz) * (i / n - sz) + (i % n - sx) * (i % n - sx);
                        if (d <= 36 && d < bestD) { bestD = d; comp = cells; }
                    }
                }
            }

            if (comp == null)
            {
                foreach (var cells in comps) if (comp == null || cells.Count > comp.Count) comp = cells;
            }

            var set = new HashSet<int>(comp);
            var d1 = CellDijkstra(set, comp[0], n, b.Cell, out _, out var order1);
            var u = Farthest(d1, order1);
            var d2 = CellDijkstra(set, u, n, b.Cell, out var prev, out var order2);
            var v = Farthest(d2, order2);
            for (var p = v; ; p = prev[p])
            {
                path.Add(p);
                if (p == u) break;
            }

            path.Reverse();
            var arc = new double[path.Count];
            var hs = new double[path.Count];
            for (var k = 0; k < path.Count; k++)
            {
                if (k > 0) arc[k] = arc[k - 1] + b.Cell * LmcBase.Hypot(path[k] / n - path[k - 1] / n, path[k] % n - path[k - 1] % n);
                hs[k] = b.H[path[k]];
            }

            var len = arc[arc.Length - 1];
            lengthKm = len / 1000.0;
            if (len < 40) return;
            var prof = new List<double>();
            for (var k = 0; k * 20.0 < len + 1e-9; k++) prof.Add(Interp(k * 20.0, arc, hs)); // numpy.arange(0, L + 1e-9, 20)
            var kk = prof.Count;
            var lo = (int)(0.2 * kk);
            var hi = Math.Max(lo + 1, (int)(0.8 * kk) + 1);
            var mid = prof.GetRange(lo, Math.Min(hi, kk) - lo);
            var hvals = new List<double>();
            for (var i = 0; i < hmask.Length; i++) if (hmask[i]) hvals.Add(b.H[i]);
            var foot = Percentile(hvals, 10);
            ratio = (Percentile(mid, 90) - Percentile(mid, 10)) / Math.Max(1e-6, Median(mid) - foot);
            peaksPerKm = ProminencePeaks(prof, 15.0) / (len / 1000.0);
        }

        private static int Farthest(Dictionary<int, double> dist, List<int> order)
        {
            var best = order[0];
            foreach (var p in order) if (dist[p] > dist[best]) best = p;
            return best;
        }

        private static Dictionary<int, double> CellDijkstra(HashSet<int> cells, int src, int n, double cell, out Dictionary<int, int> prev, out List<int> order)
        {
            var dist = new Dictionary<int, double> { [src] = 0.0 };
            prev = new Dictionary<int, int>();
            order = new List<int> { src };
            var heap = new LmcHeapD(256);
            heap.Push(0.0, src);
            while (heap.Count > 0)
            {
                heap.Pop(out var d, out var p);
                if (d > dist[p]) continue;
                var z = p / n; var x = p % n;
                for (var dz = -1; dz <= 1; dz++)
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (dz == 0 && dx == 0) continue;
                    var q = (z + dz) * n + x + dx;
                    if (!cells.Contains(q)) continue;
                    var nd = d + cell * LmcBase.Hypot(dz, dx);
                    if (!dist.TryGetValue(q, out var old))
                    {
                        dist[q] = nd; prev[q] = p; order.Add(q); heap.Push(nd, q);
                    }
                    else if (nd < old)
                    {
                        dist[q] = nd; prev[q] = p; heap.Push(nd, q);
                    }
                }
            }

            return dist;
        }

        private static int ProminencePeaks(List<double> y, double minProm)
        {
            var n = y.Count;
            var count = 0;
            for (var i = 1; i < n - 1; i++)
            {
                if (!(y[i] > y[i - 1] && y[i] >= y[i + 1])) continue;
                var lmin = y[i];
                for (var j = i - 1; j >= 0 && y[j] <= y[i]; j--) lmin = Math.Min(lmin, y[j]);
                var rmin = y[i];
                for (var j = i + 1; j < n && y[j] <= y[i]; j++) rmin = Math.Min(rmin, y[j]);
                if (y[i] - Math.Max(lmin, rmin) >= minProm) count++;
            }

            return count;
        }

        private static double Interp(double t, double[] xs, double[] ys)
        {
            if (t <= xs[0]) return ys[0];
            if (t >= xs[xs.Length - 1]) return ys[ys.Length - 1];
            var lo = 0; var hi = xs.Length - 1;
            while (hi - lo > 1)
            {
                var mid = (lo + hi) / 2;
                if (xs[mid] <= t) lo = mid; else hi = mid;
            }

            var w = (t - xs[lo]) / (xs[hi] - xs[lo]);
            return ys[lo] + w * (ys[hi] - ys[lo]);
        }

        // ------------------------------------------------------------------ Q5

        public static double Q5(LmcBase b, Vector2 centre)
        {
            var n = b.N;
            double sw = 0, swc = 0;
            for (var z = 0; z < n; z++)
            for (var x = 0; x < n; x++)
            {
                var i = z * n + x;
                var px = x * b.Cell - centre.x;
                var pz = z * b.Cell - centre.y;
                var r = Math.Sqrt(px * px + pz * pz);
                var g = Math.Sqrt(b.Gx[i] * b.Gx[i] + b.Gz[i] * b.Gz[i]);
                if (!(r >= 100.0 && r <= 400.0 && b.H[i] > LmcBase.Sea && g > 1e-6)) continue;
                var cos = -(b.Gx[i] * px + b.Gz[i] * pz) / (Math.Max(g, 1e-12) * Math.Max(r, 1e-9));
                sw += g;
                swc += g * cos;
            }

            return sw > 0 ? swc / sw : double.NaN;
        }

        // ------------------------------------------------------------------ §2.4 plateau edge (K1) and Q6

        public static LmcPlateauReading Plateau(LmcBase b, bool[] top)
        {
            var n = b.N;
            var reading = new LmcPlateauReading();
            var topCount = 0;
            var heights = new List<double>();
            double cx = 0, cz = 0;
            for (var i = 0; i < top.Length; i++)
            {
                if (!top[i]) continue;
                topCount++;
                heights.Add(b.H[i]);
                cx += i % n; cz += i / n;
            }

            reading.TopKm2 = topCount * b.Cell * b.Cell / 1e6;
            if (topCount == 0) return reading;
            reading.TopMedianHeight = Median(heights);
            cx /= topCount; cz /= topCount;
            var bestD = double.PositiveInfinity;
            for (var i = 0; i < top.Length; i++)
            {
                if (!top[i]) continue;
                var d = (i % n - cx) * (i % n - cx) + (i / n - cz) * (i / n - cz);
                if (d < bestD) { bestD = d; reading.TopVertex = i; }
            }

            reading.TopReachable = b.VertexReachable(reading.TopVertex % n, reading.TopVertex / n);
            var r15 = 15.0 / b.Cell;
            var o15 = (int)Math.Floor(r15);
            var drops = new List<double>();
            int esc = 0, joined = 0, rim = 0;
            for (var z = 0; z < n; z++)
            for (var x = 0; x < n; x++)
            {
                var i = z * n + x;
                if (!top[i]) continue;
                var isRim = false;
                for (var dz = -1; dz <= 1 && !isRim; dz++)
                for (var dx = -1; dx <= 1 && !isRim; dx++)
                {
                    var xx = (x + dx + n) % n; var zz = (z + dz + n) % n;
                    isRim = !top[zz * n + xx];
                }

                if (!isRim) continue;
                rim++;
                var drop = WalkDrop(b, top, x, z, out var foot);
                var steep = 0.0;
                for (var dz = -o15; dz <= o15; dz++)
                for (var dx = -o15; dx <= o15; dx++)
                {
                    if (dz * dz + dx * dx > r15 * r15) continue;
                    var xx = x + dx; var zz = z + dz;
                    if (!Inside(xx, zz, n)) continue;
                    var j = zz * n + xx;
                    if (!top[j]) steep = Math.Max(steep, b.VertexMaxTri[j]);
                }

                if (drop >= 10 && steep >= 45)
                {
                    esc++;
                    drops.Add(drop);
                    reading.EscSamples.Add(i);
                    reading.Feet.Add(foot);
                }
                else if (drop < 10)
                {
                    joined++;
                }
            }

            reading.Rim = rim;
            reading.EdgeHeight = drops.Count > 0 ? Median(drops) : double.NaN;
            reading.EscShare = esc / (double)Math.Max(1, rim);
            reading.JoinedShare = joined / (double)Math.Max(1, rim);
            if (drops.Count > 1)
            {
                double mean = 0;
                foreach (var d in drops) mean += d;
                mean /= drops.Count;
                double var = 0;
                foreach (var d in drops) var += (d - mean) * (d - mean);
                reading.Cv = Math.Sqrt(var / drops.Count) / mean;
            }

            var exits = new bool[n * n];
            var anyExit = false;
            for (var i = 0; i < top.Length; i++)
            {
                if (!top[i]) continue;
                var r = b.Recv[i];
                if (r >= 0 && b.Area[i] >= 30000.0 && !top[r]) { exits[i] = true; anyExit = true; }
            }

            reading.Reentrants = anyExit ? Components8(exits, n).Count : 0;
            return reading;
        }

        private static readonly (int dz, int dx)[] Nb8 =
        {
            (-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1),
        };

        private static double WalkDrop(LmcBase b, bool[] top, int x, int z, out int foot) => WalkDrop(b.H, b.Land, b.N, b.Cell, top, x, z, out foot);

        public static double WalkDrop(double[] H, bool[] Land, int n, double cell, bool[] top, int x, int z, out int foot)
        {
            var hz = H[z * n + x];
            int cx = x, cz = z;
            var path = 0.0;
            var gentle = 0;
            var lastSteep = hz;
            foot = z * n + x;
            while (path < 150.0)
            {
                var found = false;
                double bs = 0, bd = 0;
                int bx = 0, bz = 0;
                foreach (var (dz, dx) in Nb8)
                {
                    var xx = cx + dx; var zz = cz + dz;
                    if (!Inside(xx, zz, n)) continue;
                    var j = zz * n + xx;
                    if (top[j] || !Land[j]) continue;
                    var d = cell * LmcBase.Hypot(dz, dx);
                    var s = (H[cz * n + cx] - H[j]) / d;
                    if (s > 0 && (!found || s > bs)) { found = true; bs = s; bx = xx; bz = zz; bd = d; }
                }

                if (!found) break;
                cx = bx; cz = bz;
                path += bd;
                if (bs < LmcBase.Tan15)
                {
                    gentle++;
                    if (gentle >= 2) break;
                }
                else
                {
                    gentle = 0;
                    lastSteep = H[cz * n + cx];
                    foot = cz * n + cx;
                }
            }

            return hz - lastSteep;
        }


        /// <summary>Spec §2.4 plateau edge height (K1) on raw heights, for the syntheses' height targeting.</summary>
        public static double EdgeHeightLight(double[] h, bool[] land, int n, double cell, bool[] top, List<int> topCells)
        {
            var r15 = 15.0 / cell;
            var o15 = (int)Math.Floor(r15);
            var drops = new List<double>();
            foreach (var i in topCells)
            {
                var x = i % n; var z = i / n;
                var isRim = false;
                for (var dz = -1; dz <= 1 && !isRim; dz++)
                for (var dx = -1; dx <= 1 && !isRim; dx++)
                {
                    var xx = (x + dx + n) % n; var zz = (z + dz + n) % n;
                    isRim = !top[zz * n + xx];
                }

                if (!isRim) continue;
                var drop = WalkDrop(h, land, n, cell, top, x, z, out _);
                if (drop < 10) continue;
                var steep = false;
                for (var dz = -o15; dz <= o15 && !steep; dz++)
                for (var dx = -o15; dx <= o15 && !steep; dx++)
                {
                    if (dz * dz + dx * dx > r15 * r15) continue;
                    var xx = x + dx; var zz = z + dz;
                    if (!Inside(xx, zz, n) || top[zz * n + xx]) continue;
                    steep = VertexMaxTriAt(h, n, cell, xx, zz) >= 45.0;
                }

                if (steep) drops.Add(drop);
            }

            return drops.Count > 0 ? Median(drops) : double.NaN;
        }

        /// <summary>The steepest of the up to eight triangles touching a vertex, in degrees (as <c>LmcBase.VertexMaxTri</c>).</summary>
        public static double VertexMaxTriAt(double[] h, int n, double cell, int x, int z)
        {
            var m = 0.0;
            for (var qz = z - 1; qz <= z; qz++)
            for (var qx = x - 1; qx <= x; qx++)
            {
                if (qx < 0 || qz < 0 || qx >= n - 1 || qz >= n - 1) continue;
                var h00 = h[qz * n + qx]; var h10 = h[qz * n + qx + 1]; var h01 = h[(qz + 1) * n + qx]; var h11 = h[(qz + 1) * n + qx + 1];
                var s1 = LmcBase.Hypot((h10 - h00) / cell, (h11 - h10) / cell);
                var s2 = LmcBase.Hypot((h11 - h01) / cell, (h01 - h00) / cell);
                m = Math.Max(m, Math.Atan(Math.Max(s1, s2)) * 180.0 / Math.PI);
            }

            return m;
        }

        // ------------------------------------------------------------------ Q7

        public sealed class CoastReading
        {
            public double[] Shares = new double[3];
            public int Total;
            public double LongestCliffOfCliff;
            public double LongestCliffOfShore;
            public int Runs;
            public double ChangesPerKm;
            public Dictionary<int, int> ClassOf = new Dictionary<int, int>();
        }

        public static CoastReading Q7(LmcBase b)
        {
            var n = b.N;
            var rad = 60.0 / b.Cell;
            var rr = (int)Math.Floor(rad);
            var cr = new CoastReading();
            for (var z = 0; z < n; z++)
            for (var x = 0; x < n; x++)
            {
                var i = z * n + x;
                if (!b.Shore[i]) continue;
                var max = double.NegativeInfinity;
                for (var dz = -rr; dz <= rr; dz++)
                for (var dx = -rr; dx <= rr; dx++)
                {
                    if (dz * dz + dx * dx > rad * rad) continue;
                    var xx = x + dx; var zz = z + dz;
                    if (!Inside(xx, zz, n)) continue;
                    var j = zz * n + xx;
                    if (b.H[j] > LmcBase.Sea && b.H[j] > max) max = b.H[j];
                }

                var rise = max - LmcBase.Sea;
                cr.ClassOf[i] = rise < 4 ? 0 : rise < 15 ? 1 : 2;
            }

            cr.Total = cr.ClassOf.Count;
            var counts = new int[3];
            foreach (var kv in cr.ClassOf) counts[kv.Value]++;
            for (var k = 0; k < 3; k++) cr.Shares[k] = 100.0 * counts[k] / Math.Max(1, cr.Total);
            var seen = new HashSet<int>();
            var cliffTotal = 0; var longest = 0;
            var keys = new List<int>(cr.ClassOf.Keys);
            keys.Sort();
            foreach (var p in keys)
            {
                if (seen.Contains(p)) continue;
                var k = cr.ClassOf[p];
                seen.Add(p);
                var queue = new Queue<int>();
                queue.Enqueue(p);
                var size = 0;
                while (queue.Count > 0)
                {
                    var c = queue.Dequeue();
                    size++;
                    foreach (var (dz, dx) in Nb8)
                    {
                        var xx = c % n + dx; var zz = c / n + dz;
                        if (!Inside(xx, zz, n)) continue;
                        var q = zz * n + xx;
                        if (cr.ClassOf.TryGetValue(q, out var kq) && kq == k && seen.Add(q)) queue.Enqueue(q);
                    }
                }

                cr.Runs++;
                if (k == 2) { cliffTotal += size; longest = Math.Max(longest, size); }
            }

            cr.LongestCliffOfCliff = longest / (double)Math.Max(1, cliffTotal);
            cr.LongestCliffOfShore = longest / (double)Math.Max(1, cr.Total);
            cr.ChangesPerKm = cr.Runs / (cr.Total * b.Cell / 1000.0);
            return cr;
        }

        // ------------------------------------------------------------------ D-cap (with K5) and D-reg

        public static Dictionary<string, object> DCap(LmcBase b, float[] angles)
        {
            var s = new List<double>();
            for (var t = 0; t < b.TriSlope.Length; t++) if (b.TriLand[t]) s.Add(b.TriSlope[t]);
            var n = (double)s.Count;
            var result = new Dictionary<string, object>();
            foreach (var a in angles)
            {
                int c = 0, l = 0, r = 0;
                foreach (var v in s)
                {
                    if (v >= a - 1 && v <= a + 1) c++;
                    if (v >= a - 4 && v < a - 2) l++;
                    if (v > a + 2 && v <= a + 4) r++;
                }

                var share = c / n;
                var lf = l / n; var rf = r / n;
                var raw = (lf + rf) / 2 > 0 ? share / ((lf + rf) / 2) : (share > 0 ? double.PositiveInfinity : double.NaN);
                var keepL = FlankKept(a - 4, a - 2, a, angles);
                var keepR = FlankKept(a + 2, a + 4, a, angles);
                double k5;
                if (keepL && keepR) k5 = raw;
                else if (keepL) k5 = lf > 0 ? share / lf : double.NaN;
                else if (keepR) k5 = rf > 0 ? share / rf : double.NaN;
                else k5 = double.NaN;
                result[a.ToString("0.###", CultureInfo.InvariantCulture)] = new Dictionary<string, object>
                {
                    ["share"] = share, ["ratio_raw"] = raw, ["ratio_k5"] = k5,
                    ["flanks"] = (keepL ? "L" : "") + (keepR ? "R" : ""),
                };
            }

            return result;
        }

        private static bool FlankKept(double lo, double hi, double alpha, float[] angles)
        {
            lo = Math.Max(0, lo); hi = Math.Min(90, hi);
            if (hi <= lo) return false;
            foreach (var beta in angles)
            {
                if (Math.Abs(beta - alpha) < 1e-6) continue;
                if (Math.Max(lo, beta - 1) < Math.Min(hi, beta + 1)) return false;
            }

            return true;
        }

        public static Dictionary<string, object> DReg(LmcBase b, byte[] region)
        {
            var n = b.N;
            var q = b.Q;
            var edges = new[] { 0.0, 5, 15, 30, 45, 91 };
            var per = new SortedDictionary<int, List<double>>();
            for (var z = 0; z < q; z++)
            for (var x = 0; x < q; x++)
            {
                var code = region[z * n + x];
                if (code == 0) continue;
                for (var t = 2 * (z * q + x); t <= 2 * (z * q + x) + 1; t++)
                {
                    if (!b.TriLand[t]) continue;
                    if (!per.TryGetValue(code, out var list)) { list = new List<double>(); per[code] = list; }
                    list.Add(b.TriSlope[t]);
                }
            }

            var result = new Dictionary<string, object>();
            foreach (var kv in per)
            {
                var bands = new List<object>();
                for (var k = 0; k < 5; k++)
                {
                    var c = 0;
                    foreach (var v in kv.Value) if (v >= edges[k] && v < edges[k + 1]) c++;
                    bands.Add(c / (double)kv.Value.Count);
                }

                result[kv.Key.ToString(CultureInfo.InvariantCulture)] = new Dictionary<string, object>
                {
                    ["bands"] = bands, ["median"] = Median(kv.Value), ["n"] = kv.Value.Count,
                };
            }

            return result;
        }

        // ------------------------------------------------------------------ R9 detour (K2), T-edge routes

        public static int NearestWalkVertex(LmcBase b, int x, int z, int r = 6)
        {
            var n = b.N;
            var best = -1; var bestD = int.MaxValue;
            for (var dz = -r; dz <= r; dz++)
            for (var dx = -r; dx <= r; dx++)
            {
                var xx = x + dx; var zz = z + dz;
                if (!Inside(xx, zz, n) || !b.WalkVertex[zz * n + xx]) continue;
                var d = dz * dz + dx * dx;
                if (d < bestD) { bestD = d; best = zz * n + xx; }
            }

            return best;
        }

        public static double[] VertexDijkstra(LmcBase b, int src)
        {
            var n = b.N;
            var dist = new double[n * n];
            for (var i = 0; i < dist.Length; i++) dist[i] = double.PositiveInfinity;
            dist[src] = 0;
            var heap = new LmcHeapD(1 << 16);
            heap.Push(0, src);
            while (heap.Count > 0)
            {
                heap.Pop(out var d, out var i);
                if (d > dist[i]) continue;
                var z = i / n; var x = i % n;
                foreach (var (dz, dx) in Nb8)
                {
                    var xx = x + dx; var zz = z + dz;
                    if (!Inside(xx, zz, n)) continue;
                    var j = zz * n + xx;
                    if (!b.WalkVertex[j]) continue;
                    var nd = d + b.Cell * LmcBase.Hypot(dz, dx);
                    if (nd < dist[j]) { dist[j] = nd; heap.Push(nd, j); }
                }
            }

            return dist;
        }

        public static double Octile(int a, int c, int n, double cell)
        {
            var dz = Math.Abs(a / n - c / n);
            var dx = Math.Abs(a % n - c % n);
            return cell * (Math.Max(dz, dx) + (Math.Sqrt(2) - 1) * Math.Min(dz, dx));
        }

        /// <summary>Shortest T-edge route over triangle centroids from <paramref name="src"/> (all targets at once).</summary>
        public static double[] TriDijkstra(LmcBase b, int src, bool[] banned, out int[] prev)
        {
            var q = b.Q;
            var count = 2 * q * q;
            var dist = new double[count];
            prev = new int[count];
            for (var t = 0; t < count; t++) { dist[t] = double.PositiveInfinity; prev[t] = -1; }
            dist[src] = 0;
            var heap = new LmcHeapD(1 << 16);
            heap.Push(0, src);
            while (heap.Count > 0)
            {
                heap.Pop(out var d, out var t);
                if (d > dist[t]) continue;
                var ct = b.Centroid(t);
                LmcBase.Nb(t, q, out var u0, out var u1, out var u2);
                foreach (var u in new[] { u0, u1, u2 })
                {
                    if (u < 0 || !b.TriWalk[u] || (banned != null && banned[u])) continue;
                    var nd = d + Vector2.Distance(ct, b.Centroid(u));
                    if (nd < dist[u]) { dist[u] = nd; prev[u] = t; heap.Push(nd, u); }
                }
            }

            return dist;
        }

        // ------------------------------------------------------------------ R4 sites (carried from G3c, research §14.1)

        public static List<G3cSite> Sites(LmcBase b, float[] h32, float[] sd, Func<int, int> regionId, out bool capital, out bool coastal, out int regions)
        {
            var n = b.N;
            var cell = (float)b.Cell;
            var land = b.Land;
            var slope = new float[n * n];
            for (var z = 0; z < n; z++)
            for (var x = 0; x < n; x++)
            {
                var i = z * n + x;
                if (land[i]) slope[i] = IslandReliefMetrics.SlopeDegrees(h32, n, cell, x, z);
            }

            var w = n + 1;
            var psSea = new int[n * w];
            var psUnb = new int[n * w];
            var psSlope = new double[n * w];
            for (var z = 0; z < n; z++)
            for (var x = 0; x < n; x++)
            {
                var i = z * n + x;
                var o = z * w + x;
                psSea[o + 1] = psSea[o] + (land[i] ? 0 : 1);
                psUnb[o + 1] = psUnb[o] + (land[i] && slope[i] <= BuildableDegrees ? 0 : 1);
                psSlope[o + 1] = psSlope[o] + (land[i] ? slope[i] : 90f);
            }

            List<(int index, float mean)> Valid(float radius)
            {
                var list = new List<(int, float)>();
                var rc = Mathf.FloorToInt(radius / cell);
                var half = new int[2 * rc + 1];
                var area = 0;
                for (var dz = -rc; dz <= rc; dz++)
                {
                    half[dz + rc] = Mathf.FloorToInt(Mathf.Sqrt(rc * rc - dz * dz));
                    area += 2 * half[dz + rc] + 1;
                }

                for (var z = rc; z < n - rc; z += 2)
                for (var x = rc; x < n - rc; x += 2)
                {
                    var i = z * n + x;
                    if (!land[i] || sd[i] < radius) continue;
                    int sea = 0, unb = 0;
                    double sl = 0;
                    for (var dz = -rc; dz <= rc && sea == 0; dz++)
                    {
                        var hw = half[dz + rc];
                        var row = (z + dz) * w;
                        var lo = row + x - hw;
                        var hi = row + x + hw + 1;
                        sea += psSea[hi] - psSea[lo];
                        unb += psUnb[hi] - psUnb[lo];
                        sl += psSlope[hi] - psSlope[lo];
                    }

                    if (sea == 0 && unb <= (1f - SiteFraction) * area) list.Add((i, (float)(sl / area)));
                }

                list.Sort((p, q) => p.Item2 != q.Item2 ? p.Item2.CompareTo(q.Item2) : p.Item1.CompareTo(q.Item1));
                return list;
            }

            var cap = Valid(150f);
            var town = Valid(90f);
            var village = Valid(60f);
            var chosen = new List<G3cSite>();
            bool Far(Vector2 p)
            {
                foreach (var s in chosen) if (Vector2.Distance(s.Position, p) < SiteSpacing) return false;
                return true;
            }

            Vector2 P(int i) => new Vector2((i % n) * cell, (i / n) * cell);
            void Take((int index, float mean) c, float radius)
            {
                chosen.Add(new G3cSite { Position = P(c.index), RadiusMeters = radius, Coastal = sd[c.index] <= radius + CoastalReach, Region = regionId(c.index), MeanSlope = c.mean });
            }

            capital = false;
            if (cap.Count > 0) { Take(cap[0], 150f); capital = true; }
            foreach (var c in village)
            {
                if (sd[c.index] <= 60f + CoastalReach && Far(P(c.index))) { Take(c, 60f); break; }
            }

            foreach (var c in town) { if (chosen.Count >= 5) break; if (Far(P(c.index))) Take(c, 90f); }
            foreach (var c in village) { if (chosen.Count >= 5) break; if (Far(P(c.index))) Take(c, 60f); }
            coastal = false;
            var set = new HashSet<int>();
            foreach (var s in chosen) { set.Add(s.Region); coastal |= s.Coastal; }
            regions = set.Count;
            return chosen;
        }

        /// <summary>The ocean outlet each land cell drains to on the hydrology package's D8 (copied from G3c).</summary>
        public static int[] Catchments(GeographyAnalysis a, int n, out Dictionary<int, int> area)
        {
            var count = n * n;
            var label = new int[count];
            for (var i = 0; i < count; i++) label[i] = a.Land[i] ? -2 : -1;
            var stack = new List<int>();
            for (var i = 0; i < count; i++)
            {
                if (label[i] != -2) continue;
                stack.Clear();
                var cur = i;
                int lab;
                while (true)
                {
                    if (label[cur] != -2) { lab = label[cur]; break; }
                    stack.Add(cur);
                    var d = a.Directions[cur];
                    if (d == FlowRouting.NoDirection) { lab = cur; break; }
                    var x = cur % n + HydrologyGrid.NeighborDx[d];
                    var z = cur / n + HydrologyGrid.NeighborDz[d];
                    if (x < 0 || z < 0 || x >= n || z >= n) { lab = cur; break; }
                    var next = z * n + x;
                    if (!a.Land[next]) { lab = cur; break; }
                    cur = next;
                    if (stack.Count > count) { lab = cur; break; }
                }

                foreach (var j in stack) label[j] = lab;
            }

            area = new Dictionary<int, int>();
            for (var i = 0; i < count; i++) if (label[i] >= 0) area[label[i]] = area.TryGetValue(label[i], out var v) ? v + 1 : 1;
            return label;
        }

        // ------------------------------------------------------------------ helpers

        public static List<List<int>> Components8(bool[] mask, int n)
        {
            var label = new int[mask.Length];
            for (var i = 0; i < label.Length; i++) label[i] = -1;
            var result = new List<List<int>>();
            var queue = new Queue<int>();
            for (var s = 0; s < mask.Length; s++)
            {
                if (!mask[s] || label[s] >= 0) continue;
                var cells = new List<int>();
                label[s] = result.Count;
                queue.Enqueue(s);
                while (queue.Count > 0)
                {
                    var c = queue.Dequeue();
                    cells.Add(c);
                    var z = c / n; var x = c % n;
                    for (var dz = -1; dz <= 1; dz++)
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var xx = x + dx; var zz = z + dz;
                        if (!Inside(xx, zz, n)) continue;
                        var j = zz * n + xx;
                        if (mask[j] && label[j] < 0) { label[j] = result.Count; queue.Enqueue(j); }
                    }
                }

                result.Add(cells);
            }

            return result;
        }

        public static double Median(List<double> values)
        {
            if (values.Count == 0) return double.NaN;
            var s = new List<double>(values);
            s.Sort();
            var m = s.Count / 2;
            return s.Count % 2 == 1 ? s[m] : 0.5 * (s[m - 1] + s[m]);
        }

        /// <summary>numpy.percentile with linear interpolation.</summary>
        public static double Percentile(List<double> values, double p)
        {
            if (values.Count == 0) return double.NaN;
            var s = new List<double>(values);
            s.Sort();
            var pos = p / 100.0 * (s.Count - 1);
            var lo = (int)Math.Floor(pos);
            var hi = Math.Min(s.Count - 1, lo + 1);
            return s[lo] + (pos - lo) * (s[hi] - s[lo]);
        }

        public static int Nearest(LmcBase b, Vector2 p) => LmcGrid.Index(p, b.N, (float)b.Cell);
    }

    /// <summary>All G3e readings of one LMC candidate (spec §§2, 7, 8, 13). Q values and diagnostics never gate.</summary>
    public static class LmcReadingsBuilder
    {
        public const double FlagQ1 = 0.109;
        public const double FlagQ5 = 0.74;
        public const double FlagQ6Joined = 0.003;
        public const double FlagQ7Cliff = 44.6;
        public const double FlagQ7Run = 0.148;

        public static LmcReadings Measure(LmcSubject s, out LmcBase b, out GeographyAnalysis a)
        {
            var c = CultureInfo.InvariantCulture;
            var n = s.N;
            var cell = (double)s.Cell;
            var count = n * n;
            var L = s.Layout;
            var m = new LmcReadings();
            b = LmcBase.Build(s.H, n, s.Cell);
            a = GeographyAnalysis.Build(s.H, n, s.Cell, s.Seed);
            var region = s.Region;

            // ---------------- process: the land mask equals contour A's (V-L2 on the lattice)
            var maskDiff = 0;
            for (var i = 0; i < count; i++) if (b.Land[i] != s.Contour.Land[i]) maskDiff++;
            m.Gates["mask"] = maskDiff == 0;
            m.Set("mask_differences", maskDiff);

            // ---------------- §2 elevations
            var mainIdx = LmcMeasure.MainSummitIndex(b, region);
            m.MainSummit = LmcGrid.P(mainIdx, n, s.Cell);
            var summit = b.H[mainIdx];
            m.Set("main_summit", summit);
            m.Set("main_summit_xz", new List<object> { m.MainSummit.x, m.MainSummit.y });
            m.Set("main_summit_in_target", summit >= 330.0 && summit <= 430.0);
            m.Set("main_summit_target", L.Main != null ? L.Main.Target : double.NaN);
            var culms = new List<object>();
            foreach (var node in L.Culminations)
            {
                var hmax = double.NegativeInfinity;
                var r = (int)Math.Ceiling(150.0 / cell);
                var cx = (int)Math.Round(node.P.x / cell); var cz = (int)Math.Round(node.P.y / cell);
                for (var dz = -r; dz <= r; dz++)
                for (var dx = -r; dx <= r; dx++)
                {
                    if ((dx * dx + dz * dz) * cell * cell > 150.0 * 150.0) continue;
                    var x = cx + dx; var z = cz + dz;
                    if (x < 0 || z < 0 || x >= n || z >= n) continue;
                    var i = z * n + x;
                    if (region[i] == LmcCodes.Highland) hmax = Math.Max(hmax, b.H[i]);
                }

                culms.Add(new Dictionary<string, object> { ["target"] = node.Target, ["measured"] = hmax, ["xz"] = new List<object> { node.P.x, node.P.y } });
            }

            m.Set("culminations", culms);
            var peaks = GeographyMetrics.MergeTree(s.H, a.Land, n);
            // M-1 (spec v0.5 §14): the col joining the two realised flanking culminations, found only within 150 m of
            // the declared pass; a missing col is "not found" and no other vertex is substituted
            string passFailure = null;
            if (L.Pass != null)
            {
                var status = PassColM1(s, b, a.Land, out var col, out var colDistance, out var flanking);
                var level = LmcFluvial.AssociatedLowland(L, b.H);
                var found = status == "found";
                m.PassCol = found ? col : -1;
                var reachable = found && b.VertexReachable(col % n, col / n);
                m.Set("pass_m1_status", status);
                m.Set("pass_flanking_culminations", flanking);
                m.Set("pass_col_found", found);
                m.Set("pass_col_xz", col >= 0 ? new List<object> { (col % n) * cell, (col / n) * cell } : null);
                m.Set("pass_col_distance_m", col >= 0 ? (object)colDistance : null);
                m.Set("pass_col_height", found ? (object)b.H[col] : null);
                m.Set("pass_lowland_level", level);
                m.Set("pass_offset", found ? (object)(b.H[col] - level) : null);
                m.Set("pass_offset_target", L.Pass.Target);
                m.Set("pass_offset_in_range", found && b.H[col] - level >= 40.0 && b.H[col] - level <= 80.0);
                m.Set("pass_col_reachable", found ? (object)reachable : null);
                passFailure = !found
                    ? (status == "not found" ? string.Format(c, "col not found ({0:F1} m from the declared pass)", colDistance) : status)
                    : (reachable ? null : "col unreachable");
                m.Set("pass_target_met", passFailure == null);
                m.Set("pass_failure_reason", passFailure);
            }
            else
            {
                passFailure = "pass not declared";
            }

            // ---------------- plateaus (§2.4 K1, §2.5, Q6, tiers)
            var plist = new List<object>();
            foreach (var pl in L.Plateaus)
            {
                var top = new bool[count];
                for (var i = 0; i < count; i++) top[i] = region[i] == LmcCodes.PlateauTop && s.Instance[i] == pl.Id;
                var pr = LmcMeasure.Plateau(b, top);
                pr.Id = pl.Id;
                pr.ClassMin = pl.ClassIndex == 0 ? 18.0 : 60.0;
                pr.ClassMax = pl.ClassIndex == 0 ? 40.0 : 120.0;
                pr.DeclaredEdge = pl.EdgeTarget;
                pr.ClassFlag = double.IsNaN(pr.EdgeHeight) || pr.EdgeHeight < 0.75 * pr.ClassMin || pr.EdgeHeight > 1.25 * pr.ClassMax;
                m.Plateaus.Add(pr);
                var diameter = 2.0 * Math.Sqrt(pr.TopKm2 * 1e6 / Math.PI);
                var tier = pr.EdgeHeight >= 18 && pr.EdgeHeight <= 40
                    ? (diameter >= 150 && diameter <= 450 ? "T2-conform" : "T2-height, footprint outside 150-450 m")
                    : pr.EdgeHeight < 18 ? "below T2" : "above T2 (between T2 and T1)";
                var q6flag = pr.JoinedShare <= FlagQ6Joined && pr.Reentrants == 0;
                if (q6flag) m.Flags.Add(string.Format(c, "Q6 plateau {0}: joined share {1:F3} and no re-entrant (drum)", pl.Id, pr.JoinedShare));
                if (pr.ClassFlag) m.Flags.Add(string.Format(c, "class crossing plateau {0}: edge {1:F1} m vs class {2}-{3} m", pl.Id, pr.EdgeHeight, pr.ClassMin, pr.ClassMax));
                plist.Add(new Dictionary<string, object>
                {
                    ["id"] = pl.Id, ["top_km2"] = pr.TopKm2, ["class"] = pl.ClassIndex == 0 ? "18-40" : "60-120", ["declared_edge"] = pl.EdgeTarget,
                    ["edge_height"] = pr.EdgeHeight, ["esc_share"] = pr.EscShare, ["joined_share"] = pr.JoinedShare, ["cv"] = pr.Cv,
                    ["reentrants"] = pr.Reentrants, ["rim_samples"] = pr.Rim, ["class_flag"] = pr.ClassFlag, ["q6_flag"] = q6flag,
                    ["top_median_height"] = pr.TopMedianHeight, ["top_below_main_summit"] = summit - pr.TopMedianHeight,
                    ["equivalent_diameter"] = diameter, ["tier"] = tier, ["top_reachable"] = pr.TopReachable,
                    ["layout_rim_joint"] = pl.Share(LmcCodes.RimJoint), ["layout_rim_escarpment"] = pl.Share(LmcCodes.RimEscarpment),
                });
            }

            m.Set("plateaus", plist);

            // ---------------- region ids (R3, R4): basins, highland, plateaus, upland and corridor ground by catchment
            var outletOf = LmcMeasure.Catchments(a, n, out var catchArea);
            int RegionId(int i)
            {
                switch (region[i])
                {
                    case LmcCodes.Lowland: return 10 + s.Instance[i];
                    case LmcCodes.Highland: return 300;
                    case LmcCodes.PlateauTop:
                    case LmcCodes.PlateauBand:
                    case LmcCodes.PlateauRamp: return 200 + s.Instance[i];
                    case LmcCodes.Upland:
                    case LmcCodes.Corridor: return 1000 + Math.Max(0, outletOf[i]);
                    default: return -1;
                }
            }

            // ---------------- R4 sites + T-edge component clause
            var sites = LmcMeasure.Sites(b, s.H, s.Contour.Sd, RegionId, out var capital, out var coastal, out var siteRegions);
            m.Sites = sites;
            var comps = new List<int>();
            var siteList = new List<object>();
            foreach (var st in sites)
            {
                var i = LmcGrid.Index(st.Position, n, s.Cell);
                var comp = b.VertexComponent(i % n, i / n, out var reach, out _);
                comps.Add(comp);
                siteList.Add(new Dictionary<string, object>
                {
                    ["xz"] = new List<object> { st.Position.x, st.Position.y }, ["radius"] = st.RadiusMeters, ["coastal"] = st.Coastal,
                    ["region"] = st.Region, ["region_code"] = (int)region[i], ["mean_slope"] = st.MeanSlope, ["component"] = comp, ["reachable"] = reach,
                });
            }

            var oneComp = comps.Count > 0 && comps[0] >= 0;
            foreach (var k in comps) oneComp &= k == comps[0];
            m.Gates["R4"] = capital && sites.Count >= 3 && coastal && siteRegions >= 3 && oneComp;
            m.Set("sites", siteList);
            m.Set("site_regions", siteRegions);
            m.Set("sites_one_component", oneComp);

            // ---------------- R5 / R10
            R5R10(s, a, m);

            // ---------------- R7
            var s8 = Depressions(a, s.H, n, s.Cell, m.Lines, region);
            var lakes = a.Hydrology.Succeeded ? a.Hydrology.Definition.Lakes.Count : -1;
            var axes = new List<object>();
            var climbing = 0;
            var worst = 0.0;
            foreach (var ax in s.Axes)
            {
                var min = double.PositiveInfinity; var climb = 0.0;
                foreach (var p in ax.Points)
                {
                    var v = b.H[LmcGrid.Index(p, n, s.Cell)];
                    if (!double.IsInfinity(min)) climb = Math.Max(climb, v - min);
                    min = Math.Min(min, v);
                }

                if (climb > 0.25) climbing++;
                worst = Math.Max(worst, climb);
                axes.Add(new Dictionary<string, object> { ["name"] = ax.Name, ["climb"] = climb });
            }

            var chClimb = 0.0; var chClimbing = 0;
            if (s.ChannelReceivers != null) ChannelClimb(s, b, out chClimb, out chClimbing);
            m.Gates["R7"] = s8 == 0 && lakes == 0 && climbing == 0 && chClimbing == 0;
            m.Set("r7_s8", s8);
            m.Set("r7_lakes", lakes);
            m.Set("r7_axes", axes);
            m.Set("r7_axis_worst_climb", worst);
            m.Set("r7_channel_worst_climb", chClimb);
            m.Set("r7_channel_climbing_vertices", chClimbing);

            // ---------------- R8
            var unreachable = new List<object>();
            foreach (var st in sites)
            {
                var i = LmcGrid.Index(st.Position, n, s.Cell);
                if (!b.VertexReachable(i % n, i / n)) unreachable.Add(string.Format(c, "site ({0:F0}, {1:F0})", st.Position.x, st.Position.y));
            }

            foreach (var pr in m.Plateaus)
            {
                if (pr.TopVertex < 0 || !pr.TopReachable) unreachable.Add($"plateau {pr.Id} top");
            }

            if (passFailure != null) unreachable.Add("pass: " + passFailure); // M-1: the pass target is never skipped
            m.Gates["R8"] = unreachable.Count == 0 && sites.Count > 0;
            m.Set("r8_unreachable", unreachable);

            // ---------------- R9
            R9(s, b, m, sites, comps);

            // ---------------- indicators R1, R2, R6 (carried)
            R1(s, a, b, m, peaks, outletOf, catchArea);
            R2(s, b, m);
            var coast = LmcMeasure.Q7(b);
            R6(s, b, m, coast);

            // ---------------- diagnostics
            R3(s, b, m, RegionId);
            R11(s, a, b, m, peaks, outletOf, catchArea, coast);
            Walls(b, m);
            Bands(s, b, m);
            m.Set("dcap", LmcMeasure.DCap(b, s.ConfiguredAngles));
            m.Set("dreg", LmcMeasure.DReg(b, region));

            // ---------------- Q1–Q9
            LmcMeasure.Q1(b, region, out var q1, out var q1r);
            var q1map = new Dictionary<string, object>();
            foreach (var kv in q1r) q1map[kv.Key.ToString(c)] = kv.Value;
            m.Set("q1_all", q1);
            m.Set("q1_by_region", q1map);
            if (q1 >= FlagQ1) m.Flags.Add(string.Format(c, "Q1 planar share {0:F3} >= 0.109", q1));
            m.Set("q2_theta", LmcMeasure.Q2(b, out var systems));
            m.Set("q2_systems", systems);
            var hmask = new bool[count];
            for (var i = 0; i < count; i++) hmask[i] = region[i] == LmcCodes.Highland;
            LmcMeasure.Q3(b, hmask, out var q3w, out var q3k, out var crestCount);
            m.Set("q3_width_median", q3w);
            m.Set("q3_kappa_median", q3k);
            m.Set("q3_crest_vertices", crestCount);
            Q4(s, b, m);
            LmcMeasure.Q4m(b, hmask, m.MainSummit, out var ratio, out var ppk, out var lkm, out _);
            m.Set("q4m", new Dictionary<string, object> { ["ratio"] = ratio, ["peaks_per_km"] = ppk, ["length_km"] = lkm });
            var q5 = new List<object>();
            var q5main = LmcMeasure.Q5(b, m.MainSummit);
            foreach (var pk in peaks)
            {
                if (pk.Prominence < GeographyMetrics.SummitProminenceMeters || region[pk.Index] != LmcCodes.Highland) continue;
                q5.Add(new Dictionary<string, object> { ["xz"] = new List<object> { (pk.Index % n) * cell, (pk.Index / n) * cell }, ["height"] = pk.Height, ["prominence"] = pk.Prominence, ["radial"] = LmcMeasure.Q5(b, LmcGrid.P(pk.Index, n, s.Cell)) });
            }

            m.Set("q5_main", q5main);
            m.Set("q5_culminations", q5);
            if (q5main >= FlagQ5) m.Flags.Add(string.Format(c, "Q5 radial index at the main summit {0:F3} >= 0.74", q5main));
            var outletClasses = new List<object>();
            foreach (var o in L.Outlets) outletClasses.Add(ShoreClassAt(b, coast, o));
            m.Set("q7", new Dictionary<string, object>
            {
                ["shares"] = new List<object> { coast.Shares[0], coast.Shares[1], coast.Shares[2] }, ["total"] = coast.Total,
                ["longest_cliff_of_cliff"] = coast.LongestCliffOfCliff, ["longest_cliff_of_shore"] = coast.LongestCliffOfShore,
                ["runs"] = coast.Runs, ["changes_per_km"] = coast.ChangesPerKm, ["outlet_classes"] = outletClasses,
            });
            if (coast.Shares[2] >= FlagQ7Cliff) m.Flags.Add(string.Format(c, "Q7 cliff share {0:F1} % >= 44.6 %", coast.Shares[2]));
            if (coast.LongestCliffOfShore >= FlagQ7Run) m.Flags.Add(string.Format(c, "Q7 longest cliff run {0:F3} of the shoreline >= 0.148", coast.LongestCliffOfShore));
            m.Set("q8", "n/a in calibration (defined over the ten held-out lattices)");
            m.Set("flags", new List<object>(m.Flags));
            var gates = new Dictionary<string, object>();
            foreach (var kv in m.Gates) gates[kv.Key] = kv.Value;
            m.Set("gates", gates);
            return m;
        }

        /// <summary>
        /// M-1 (spec v0.5 §14). Flanking culminations: the nearest declared main-divide culminations on each side of the
        /// declared pass by arc; each one's vertex is the highest mountain-region vertex within 150 m of its declared point and
        /// counts as realised when it is a merge-tree peak with prominence ≥ 30 m (G0's order and tie rules; a peak that
        /// never merges into higher ground has prominence = height − sea level). The col is where the components of the two
        /// realised culminations first join; it is found only within 150 m of the declared pass.
        /// </summary>
        private static string PassColM1(LmcSubject s, LmcBase b, bool[] land, out int col, out double colDistance, out List<object> flanking)
        {
            var L = s.Layout;
            var n = s.N;
            var cell = (double)s.Cell;
            var h = s.H;
            col = -1;
            colDistance = double.NaN;
            flanking = new List<object>();
            LmcNode left = null, right = null;
            foreach (var node in L.Nodes)
            {
                if (node.Kind != "culmination" || node.Ridge != 0) continue;
                if (node.Arc < L.Pass.Arc && (left == null || node.Arc > left.Arc)) left = node;
                if (node.Arc > L.Pass.Arc && (right == null || node.Arc < right.Arc)) right = node;
            }

            if (left == null || right == null) return "pass not declared between two culminations";

            int VertexOf(LmcNode c)
            {
                var best = -1;
                for (var i = 0; i < h.Length; i++)
                {
                    if (s.Region[i] != LmcCodes.Highland) continue;
                    var dx = (i % n) * cell - c.P.x;
                    var dz = (i / n) * cell - c.P.y;
                    if (Math.Sqrt(dx * dx + dz * dz) > 150.0) continue;
                    if (best < 0 || h[i] > h[best]) best = i;
                }

                return best;
            }

            var va = VertexOf(left);
            var vb = VertexOf(right);
            if (va < 0 || vb < 0) return "culmination not realised";

            // G0's merge tree (copied: descending height, ascending index, 8-connectivity), tracking the two vertices
            var keys = new List<ulong>(h.Length / 2);
            for (var i = 0; i < h.Length; i++)
            {
                if (!land[i]) continue;
                var bits = (uint)BitConverter.SingleToInt32Bits(h[i]);
                keys.Add(((ulong)(uint.MaxValue - bits) << 32) | (uint)i);
            }

            var order = keys.ToArray();
            Array.Sort(order);
            var parent = new int[h.Length];
            var peak = new int[h.Length];
            var done = new bool[h.Length];
            double promA = double.NaN, promB = double.NaN;
            int Find(int i)
            {
                while (parent[i] != i)
                {
                    parent[i] = parent[parent[i]];
                    i = parent[i];
                }

                return i;
            }

            foreach (var key in order)
            {
                var i = (int)(key & 0xffffffffUL);
                done[i] = true;
                parent[i] = i;
                peak[i] = i;
                var x = i % n;
                var z = i / n;
                for (var dz = -1; dz <= 1; dz++)
                for (var dx = -1; dx <= 1; dx++)
                {
                    var nx = x + dx; var nz = z + dz;
                    if ((dx == 0 && dz == 0) || nx < 0 || nz < 0 || nx >= n || nz >= n) continue;
                    var j = nz * n + nx;
                    if (!done[j]) continue;
                    var ri = Find(i);
                    var rj = Find(j);
                    if (ri == rj) continue;
                    var pi = peak[ri];
                    var pj = peak[rj];
                    var iHigher = h[pi] > h[pj] || (h[pi] == h[pj] && pi < pj);
                    var low = iHigher ? rj : ri;
                    var high = iHigher ? ri : rj;
                    if (peak[low] == va && double.IsNaN(promA)) promA = h[va] - h[i];
                    if (peak[low] == vb && double.IsNaN(promB)) promB = h[vb] - h[i];
                    parent[low] = high;
                }

                if (col < 0 && done[va] && done[vb] && Find(va) == Find(vb)) col = i;
            }

            var sea = WorldScale.SeaLevelMeters;
            if (double.IsNaN(promA)) promA = h[va] - sea;
            if (double.IsNaN(promB)) promB = h[vb] - sea;
            foreach (var (v, prom, node) in new[] { (va, promA, left), (vb, promB, right) })
            {
                flanking.Add(new Dictionary<string, object>
                {
                    ["declared_xz"] = new List<object> { node.P.x, node.P.y }, ["vertex_xz"] = new List<object> { (v % n) * cell, (v / n) * cell },
                    ["height"] = (double)h[v], ["prominence"] = prom, ["realised"] = prom >= 30.0,
                });
            }

            if (promA < 30.0 || promB < 30.0)
            {
                col = -1;
                return "culmination not realised";
            }

            var cx = (col % n) * cell - L.Pass.P.x;
            var cz = (col / n) * cell - L.Pass.P.y;
            colDistance = Math.Sqrt(cx * cx + cz * cz);
            return colDistance <= 150.0 ? "found" : "not found";
        }

        private static string ShoreClassAt(LmcBase b, LmcMeasure.CoastReading coast, Vector2 p)
        {
            var best = -1; var bestD = double.PositiveInfinity;
            foreach (var kv in coast.ClassOf)
            {
                var d = (LmcGrid.P(kv.Key, b.N, (float)b.Cell) - p).sqrMagnitude;
                if (d < bestD) { bestD = d; best = kv.Value; }
            }

            return best == 0 ? "low" : best == 1 ? "rocky" : best == 2 ? "cliff" : "none";
        }

        private static void R5R10(LmcSubject s, GeographyAnalysis a, LmcReadings m)
        {
            var hyd = a.Hydrology;
            var n = s.N;
            m.Gates["R10"] = hyd.Succeeded;
            if (!hyd.Succeeded)
            {
                m.Gates["R5"] = false;
                m.Set("r10_failure", $"{hyd.Failure}: {hyd.Message}");
                m.Set("r10_diagnosis", DiagnoseR10(s, hyd.Message));
                return;
            }

            var def = hyd.Definition;
            var byId = new Dictionary<int, RiverReach>();
            foreach (var reach in def.Reaches) byId[reach.Id.Value] = reach;
            var lakes = new Dictionary<int, LakeBasin>();
            foreach (var lake in def.Lakes) lakes[lake.Id.Value] = lake;
            var elevated = new Dictionary<int, bool>();
            var mouth = new Dictionary<int, Vector2>();
            foreach (var reach in def.Reaches)
            {
                var cur = reach;
                var end = cur.Nodes.Count > 0 ? cur.Nodes[cur.Nodes.Count - 1].PositionXZ : Vector2.zero;
                var terminal = -1;
                for (var guard = 0; guard < 10000; guard++)
                {
                    if (cur.Termination == ReachTermination.Ocean) { terminal = cur.Id.Value; end = cur.Nodes[cur.Nodes.Count - 1].PositionXZ; break; }
                    if (cur.DownstreamId.IsValid && byId.TryGetValue(cur.DownstreamId.Value, out var next)) { cur = next; continue; }
                    if (cur.DownstreamLake.IsValid && lakes.TryGetValue(cur.DownstreamLake.Value, out var lake))
                    {
                        if (lake.OutletReach.IsValid && byId.TryGetValue(lake.OutletReach.Value, out var outlet)) { cur = outlet; continue; }
                        terminal = -1000 - lake.Id.Value; end = lake.SpillPointXZ; break;
                    }

                    terminal = -2000 - cur.Id.Value;
                    break;
                }

                mouth[terminal] = end;
                if (!elevated.ContainsKey(terminal)) elevated[terminal] = false;
                if (reach.IsSource && reach.Nodes.Count > 0)
                {
                    var code = s.Region[LmcGrid.Index(reach.Nodes[0].PositionXZ, n, s.Cell)];
                    if (code == LmcCodes.Highland || code == LmcCodes.PlateauTop) elevated[terminal] = true;
                }
            }

            int elev = 0, undesignated = 0;
            var list = new List<object>();
            foreach (var kv in elevated)
            {
                if (!kv.Value) continue;
                elev++;
                var p = mouth[kv.Key];
                var designated = false;
                foreach (var o in s.Layout.Outlets) designated |= Vector2.Distance(o, p) <= 150f;
                if (!designated) undesignated++;
                list.Add(new Dictionary<string, object> { ["mouth"] = new List<object> { p.x, p.y }, ["designated"] = designated });
            }

            m.Gates["R5"] = elev >= 1 && undesignated == 0;
            m.Set("r5_systems", elevated.Count);
            m.Set("r5_elevated", elev);
            m.Set("r5_elevated_undesignated", undesignated);
            m.Set("r5_elevated_mouths", list);
            var strahler = 0;
            var largest = 0.0;
            foreach (var reach in def.Reaches) { strahler = Math.Max(strahler, reach.StrahlerOrder); largest = Math.Max(largest, reach.ContributingAreaSqm); }
            m.Set("strahler_max", strahler);
            m.Set("tributaries", def.Stats.TributaryCount);
            m.Set("river_km", def.Stats.TotalRiverLengthMeters / 1000.0);
            m.Set("largest_reach_area_km2", largest / 1e6);
        }

        /// <summary>R10 on failure: the failing reach's node positions, rebuilt from the planner's public stages, with lattice heights there.</summary>
        private static string DiagnoseR10(LmcSubject s, string message)
        {
            var rx = new Regex(@"(\S+) flows uphill at node (\d+): ([\d.]+) m to ([\d.]+) m");
            var mm = rx.Match(message ?? "");
            if (!mm.Success) return "no uphill-node message to diagnose";
            try
            {
                var n = s.N;
                var settings = HydrologySettings.Default;
                settings.Clamp();
                var grid = new HydrologyGrid(0f, 0f, n, n, s.Cell, (float[])s.H.Clone(), WorldScale.SeaLevelMeters);
                var ocean = grid.BuildOceanMask();
                var filled = DepressionFill.Fill(grid, ocean);
                var dirs = FlowRouting.ComputeD8(grid, filled, ocean);
                var acc = FlowRouting.Accumulate(grid, dirs, ocean, out _);
                var lakeField = BasinExtraction.Extract(grid, filled, ocean, settings);
                var rng = new GameDev.World.Seed.WorldSeed(s.Seed).Fork(RiverNetworkBuilder.SeedStream);
                var network = RiverNetworkBuilder.Build(grid, filled, ocean, dirs, acc, lakeField, settings, rng);
                var node = int.Parse(mm.Groups[2].Value, CultureInfo.InvariantCulture);
                foreach (var reach in network.Reaches)
                {
                    if (reach.Id.ToString() != mm.Groups[1].Value || node >= reach.Nodes.Count || node < 1) continue;
                    var p0 = reach.Nodes[node - 1].PositionXZ;
                    var p1 = reach.Nodes[node].PositionXZ;
                    return string.Format(CultureInfo.InvariantCulture,
                        "{0} node {1}: beds {2} -> {3} m; positions ({4:F1}, {5:F1}) -> ({6:F1}, {7:F1}); lattice nearest {8:F2} -> {9:F2} m, bilinear {10:F2} -> {11:F2} m; regions {12} -> {13}",
                        mm.Groups[1].Value, node, mm.Groups[3].Value, mm.Groups[4].Value, p0.x, p0.y, p1.x, p1.y,
                        s.H[LmcGrid.Index(p0, n, s.Cell)], s.H[LmcGrid.Index(p1, n, s.Cell)],
                        G3cMeasure.Bilinear(s.H, n, s.Cell, p0), G3cMeasure.Bilinear(s.H, n, s.Cell, p1),
                        s.Region[LmcGrid.Index(p0, n, s.Cell)], s.Region[LmcGrid.Index(p1, n, s.Cell)]);
                }

                return "reach not found when the network was rebuilt";
            }
            catch (Exception e)
            {
                return "diagnosis failed: " + e.Message;
            }
        }

        private static int Depressions(GeographyAnalysis a, float[] h, int n, float cell, List<string> lines, byte[] region)
        {
            var count = h.Length;
            var depth = new float[count];
            for (var i = 0; i < count; i++) depth[i] = a.Land[i] ? a.Filled[i] - h[i] : 0f;
            var seen = new bool[count];
            var s8 = 0;
            var stack = new Stack<int>();
            for (var s = 0; s < count; s++)
            {
                if (seen[s] || depth[s] <= 0.05f) continue;
                seen[s] = true;
                stack.Push(s);
                var cells = 0; var deepest = 0f; var at = s;
                while (stack.Count > 0)
                {
                    var i = stack.Pop();
                    cells++;
                    if (depth[i] > deepest) { deepest = depth[i]; at = i; }
                    var x = i % n; var z = i / n;
                    if (x > 0 && !seen[i - 1] && depth[i - 1] > 0.05f) { seen[i - 1] = true; stack.Push(i - 1); }
                    if (x < n - 1 && !seen[i + 1] && depth[i + 1] > 0.05f) { seen[i + 1] = true; stack.Push(i + 1); }
                    if (z > 0 && !seen[i - n] && depth[i - n] > 0.05f) { seen[i - n] = true; stack.Push(i - n); }
                    if (z < n - 1 && !seen[i + n] && depth[i + n] > 0.05f) { seen[i + n] = true; stack.Push(i + n); }
                }

                if (deepest >= IslandReliefMetrics.DepressionMinDepthMeters && cells * cell * cell >= IslandReliefMetrics.DepressionMinAreaSqm)
                {
                    s8++;
                    if (lines.Count < 400) lines.Add(string.Format(CultureInfo.InvariantCulture, "S8 depression at ({0:F0}, {1:F0}): depth {2:F2} m, {3:F0} m2, region {4}", (at % n) * cell, (at / n) * cell, deepest, cells * cell * cell, region[at]));
                }
            }

            return s8;
        }

        /// <summary>R7 (c) for the synthesis' own channels (area ≥ 30 000 m²): the largest rise over any upstream channel vertex.</summary>
        private static void ChannelClimb(LmcSubject s, LmcBase b, out double worst, out int climbing)
        {
            var count = s.N * s.N;
            var recv = s.ChannelReceivers;
            var area = s.ChannelArea;
            var channel = new bool[count];
            for (var i = 0; i < count; i++) channel[i] = recv[i] >= 0 && area[i] >= 30000.0;
            var indeg = new int[count];
            for (var i = 0; i < count; i++) if (channel[i] && channel[recv[i]]) indeg[recv[i]]++;
            var minUp = new double[count];
            for (var i = 0; i < count; i++) minUp[i] = double.PositiveInfinity;
            var queue = new Queue<int>();
            for (var i = 0; i < count; i++) if (channel[i] && indeg[i] == 0) queue.Enqueue(i);
            worst = 0; climbing = 0;
            while (queue.Count > 0)
            {
                var i = queue.Dequeue();
                if (!double.IsInfinity(minUp[i]))
                {
                    var climb = b.H[i] - minUp[i];
                    if (climb > worst) worst = climb;
                    if (climb > 0.25) climbing++;
                }

                var r = recv[i];
                if (r < 0 || !channel[r]) continue;
                minUp[r] = Math.Min(minUp[r], Math.Min(minUp[i], b.H[i]));
                if (--indeg[r] == 0) queue.Enqueue(r);
            }
        }

        private static void R9(LmcSubject s, LmcBase b, LmcReadings m, List<G3cSite> sites, List<int> comps)
        {
            var n = s.N;
            var walk = new List<int>();
            var tri = new List<int>();
            foreach (var st in sites)
            {
                var i = LmcGrid.Index(st.Position, n, s.Cell);
                walk.Add(LmcMeasure.NearestWalkVertex(b, i % n, i / n));
                b.VertexComponent(i % n, i / n, out _, out var t);
                tri.Add(t);
            }

            var pairs = new List<object>();
            var allConnected = true;
            var detours = new List<double>();
            for (var i = 0; i < sites.Count; i++)
            {
                var vd = walk[i] >= 0 ? LmcMeasure.VertexDijkstra(b, walk[i]) : null;
                double[] td = null; int[] tprev = null;
                if (tri[i] >= 0) td = LmcMeasure.TriDijkstra(b, tri[i], null, out tprev);
                for (var j = i + 1; j < sites.Count; j++)
                {
                    var connected = comps[i] >= 0 && comps[i] == comps[j];
                    allConnected &= connected;
                    object detour = null; object maxSlope = null; object alternative = null;
                    if (connected && vd != null && walk[j] >= 0 && !double.IsInfinity(vd[walk[j]]))
                    {
                        var dv = vd[walk[j]] / LmcMeasure.Octile(walk[i], walk[j], n, b.Cell);
                        detour = dv;
                        detours.Add(dv);
                    }

                    if (connected && td != null && tri[j] >= 0 && !double.IsInfinity(td[tri[j]]))
                    {
                        var route = new List<int>();
                        for (var t = tri[j]; t >= 0; t = tprev[t]) route.Add(t);
                        route.Reverse();
                        var ms = 0.0;
                        foreach (var t in route) ms = Math.Max(ms, b.TriSlope[t]);
                        maxSlope = ms;
                        alternative = Alternative(b, route, tri[i], tri[j]);
                    }

                    pairs.Add(new Dictionary<string, object>
                    {
                        ["pair"] = new List<object> { i, j }, ["regions"] = new List<object> { sites[i].Region, sites[j].Region },
                        ["connected"] = connected, ["detour"] = detour, ["max_slope"] = maxSlope, ["alternative"] = alternative,
                    });
                }
            }

            m.Gates["R9"] = allConnected && sites.Count > 1;
            m.Set("r9_pairs", pairs);
            m.Set("r9_detour_median", LmcMeasure.Median(detours));
            m.Set("r9_detour_max", detours.Count > 0 ? detours.Max() : double.NaN);
        }

        /// <summary>Remove walkable triangles within 100 m of the middle half of the route (the two end triangles kept) and test the pair again.</summary>
        private static bool Alternative(LmcBase b, List<int> route, int src, int dst)
        {
            var arc = new double[route.Count];
            for (var k = 1; k < route.Count; k++) arc[k] = arc[k - 1] + Vector2.Distance(b.Centroid(route[k - 1]), b.Centroid(route[k]));
            var total = arc[route.Count - 1];
            var q = b.Q;
            var banned = new bool[2 * q * q];
            var r = (int)Math.Ceiling(100.0 / b.Cell) + 1;
            for (var k = 0; k < route.Count; k++)
            {
                if (arc[k] < 0.25 * total || arc[k] > 0.75 * total) continue;
                var p = b.Centroid(route[k]);
                var cx = (int)(p.x / b.Cell); var cz = (int)(p.y / b.Cell);
                for (var dz = -r; dz <= r; dz++)
                for (var dx = -r; dx <= r; dx++)
                {
                    var x = cx + dx; var z = cz + dz;
                    if (x < 0 || z < 0 || x >= q || z >= q) continue;
                    for (var t = 2 * (z * q + x); t <= 2 * (z * q + x) + 1; t++)
                    {
                        if (!banned[t] && Vector2.Distance(b.Centroid(t), p) <= 100f) banned[t] = true;
                    }
                }
            }

            banned[src] = false;
            banned[dst] = false;
            var d = LmcMeasure.TriDijkstra(b, src, banned, out _);
            return !double.IsInfinity(d[dst]);
        }

        private static void R1(LmcSubject s, GeographyAnalysis a, LmcBase b, LmcReadings m, List<GeographyPeak> peaks, int[] outletOf, Dictionary<int, int> catchArea)
        {
            var n = s.N;
            var cell = s.Cell;
            var region = s.Region;
            var land = a.Land;
            var heights = new List<float>();
            for (var i = 0; i < land.Length; i++) if (land[i]) heights.Add(s.H[i]);
            heights.Sort();
            var p90 = heights.Count > 0 ? heights[Mathf.Clamp((int)(0.9f * heights.Count), 0, heights.Count - 1)] : 0f;
            int culm = 0, separate = 0, passes = 0, branch = 0;
            foreach (var pk in peaks)
            {
                if (pk.Prominence < GeographyMetrics.SummitProminenceMeters) continue;
                if (region[pk.Index] == LmcCodes.Highland) culm++;
                if (pk.Col < 0 || pk.ColHeight < p90) separate++;
                if (pk.Col >= 0 && region[pk.Col] == LmcCodes.Highland)
                {
                    var distinct = new HashSet<int>();
                    var cx = pk.Col % n; var cz = pk.Col / n;
                    for (var dz = -1; dz <= 1; dz++)
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var x = cx + dx; var z = cz + dz;
                        if (x < 0 || z < 0 || x >= n || z >= n) continue;
                        var o = outletOf[z * n + x];
                        if (o >= 0 && catchArea.TryGetValue(o, out var ar) && ar * cell * cell >= 100_000f) distinct.Add(o);
                    }

                    if (distinct.Count >= 2 && b.VertexReachable(cx, cz)) passes++;
                }
            }

            for (var i = 0; i < land.Length; i++) if (a.BranchMask[i] && region[i] == LmcCodes.Highland) branch++;
            var dome = SingleDome(s, a, p90, separate, out var aligned, out var eta, out var p90share);
            m.Set("r1", new Dictionary<string, object>
            {
                ["culminations"] = culm, ["branch_points"] = branch, ["passes"] = passes, ["separate_summits"] = separate, ["single_dome"] = dome,
                ["coast_aligned"] = aligned, ["eta_squared"] = eta, ["p90_share"] = p90share,
                ["met"] = culm >= 2 && branch >= 1 && passes >= 1 && !dome,
            });
            m.Set("q4_separate_summits", separate);
        }

        private static bool SingleDome(LmcSubject s, GeographyAnalysis a, float p90, int separate, out float aligned, out float eta, out float p90Share)
        {
            var n = s.N;
            var cell = s.Cell;
            var h = s.H;
            var sd = s.Contour.Sd;
            int al = 0, total = 0;
            var bins = new Dictionary<int, double[]>();
            double sum = 0, sumSq = 0;
            long cnt = 0;
            for (var z = 0; z < n; z++)
            for (var x = 0; x < n; x++)
            {
                var i = z * n + x;
                if (!(a.Land[i] && (s.Region[i] == LmcCodes.Lowland || s.Region[i] == LmcCodes.Upland))) continue;
                var key = Mathf.FloorToInt(sd[i] / GeographyMetrics.CoastBinMeters);
                if (!bins.TryGetValue(key, out var acc)) { acc = new double[3]; bins[key] = acc; }
                acc[0]++; acc[1] += h[i]; acc[2] += (double)h[i] * h[i];
                sum += h[i]; sumSq += (double)h[i] * h[i]; cnt++;
                if (sd[i] <= 250f) continue;
                IslandReliefMetrics.Gradient(h, n, cell, x, z, out var hx, out var hz);
                IslandReliefMetrics.Gradient(sd, n, cell, x, z, out var dx, out var dz);
                var hm = Mathf.Sqrt(hx * hx + hz * hz);
                var dm = Mathf.Sqrt(dx * dx + dz * dz);
                if (hm <= 0.01f || dm <= 0.5f) continue;
                total++;
                if ((hx * dx + hz * dz) / (hm * dm) > 0.95f) al++;
            }

            aligned = total > 0 ? 100f * al / total : 0f;
            double within = 0;
            foreach (var acc in bins.Values) within += acc[2] - acc[1] * acc[1] / acc[0];
            var totalVar = cnt > 0 ? sumSq - sum * sum / cnt : 0.0;
            eta = totalVar > 1e-9 ? (float)(1.0 - within / totalVar) : 1f;
            var above = new bool[h.Length];
            var aboveCount = 0;
            for (var i = 0; i < h.Length; i++) { above[i] = a.Land[i] && h[i] >= p90; if (above[i]) aboveCount++; }
            var label = GeographyGrid.Components8(above, n, out var comps);
            var sizes = GeographyGrid.ComponentSizes(label, comps);
            var largest = 0;
            foreach (var size in sizes) largest = Mathf.Max(largest, size);
            p90Share = aboveCount > 0 ? largest / (float)aboveCount : 0f;
            return aligned > GeographyMetrics.DomeCoastAlignedPercent && eta >= GeographyMetrics.DomeEtaSquared && separate <= GeographyMetrics.DomeMaxSummits && p90Share >= GeographyMetrics.DomeP90Share;
        }

        private static void R2(LmcSubject s, LmcBase b, LmcReadings m)
        {
            var n = s.N;
            var cell = s.Cell;
            var count = n * n;
            var highOrUp = new bool[count];
            for (var i = 0; i < count; i++) highOrUp[i] = s.Region[i] == LmcCodes.Highland || s.Region[i] == LmcCodes.Upland;
            var dHighUp = LmcGrid.Edt(highOrUp, n, cell);
            var verified = new List<Vector2>();
            var lines = new List<object>();
            foreach (var pl in s.Layout.Plateaus)
            {
                int top = 0, flat = 0, reachable = 0;
                for (var i = 0; i < count; i++)
                {
                    if (s.Region[i] != LmcCodes.PlateauTop || s.Instance[i] != pl.Id) continue;
                    top++;
                    if (b.VertexSlope[i] <= 5.0) flat++;
                    if (b.VertexReachable(i % n, i / n)) reachable++;
                }

                var perimeter = 0f;
                for (var k = 0; k < 360; k++) perimeter += Vector2.Distance(pl.RimPoint(k), pl.RimPoint(k + 1));
                var samples = Mathf.Max(12, Mathf.CeilToInt(perimeter / 10f));
                var escarp = 0; var adjoin = false; var maxDrop = 0.0;
                for (var k = 0; k < samples; k++)
                {
                    var deg = 360f * k / samples;
                    var start = pl.RimPoint(deg);
                    var dir = new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));
                    if (dHighUp[LmcGrid.Index(start, n, cell)] <= 150f) adjoin = true;
                    var drop = SteepRunDrop(b, s.H, start, dir, 2f * pl.EdgeTarget + 40f, 20f);
                    maxDrop = Math.Max(maxDrop, drop);
                    if (drop >= 10f) escarp++;
                }

                var flatShare = flat / (double)Math.Max(1, top);
                var escShare = escarp / (double)samples;
                var ok = top > 0 && flatShare >= 0.6 && escShare >= 0.2 && adjoin && reachable > 0;
                if (ok) verified.Add(pl.Centre);
                lines.Add(new Dictionary<string, object> { ["id"] = pl.Id, ["top_flat_share"] = flatShare, ["escarpment_rays"] = escShare, ["adjoins"] = adjoin, ["reachable_top"] = reachable > 0, ["max_steep_run_drop"] = maxDrop, ["verified"] = ok });
            }

            var sep = 0f;
            for (var i = 0; i < verified.Count; i++)
            for (var j = i + 1; j < verified.Count; j++)
                sep = Mathf.Max(sep, Vector2.Distance(verified[i], verified[j]));
            m.Set("r2", new Dictionary<string, object> { ["plateaus"] = lines, ["verified"] = verified.Count, ["max_separation"] = sep, ["met"] = verified.Count >= 2 && sep >= 500f });
        }

        /// <summary>Drop of the first continuous run of served slope ≥ 45° starting within <paramref name="startWithin"/> of the rim (G3c's R2 ray).</summary>
        private static double SteepRunDrop(LmcBase b, float[] h, Vector2 start, Vector2 dir, float maxDist, float startWithin)
        {
            var n = b.N;
            var cell = (float)b.Cell;
            var inRun = false;
            var runStartH = 0f;
            var drop = 0f;
            var step = 0.5f * cell;
            for (var d = 0f; d <= maxDist; d += step)
            {
                var p = start + dir * d;
                if (p.x < 0f || p.y < 0f || p.x >= (n - 1) * cell || p.y >= (n - 1) * cell) break;
                var fx = p.x / cell; var fz = p.y / cell;
                var x = Mathf.Clamp((int)fx, 0, n - 2); var z = Mathf.Clamp((int)fz, 0, n - 2);
                var t = 2 * (z * (n - 1) + x) + (fx - x >= fz - z ? 0 : 1);
                var steep = b.TriSlope[t] >= 45.0;
                var hp = G3cMeasure.Bilinear(h, n, cell, p);
                if (steep && !inRun)
                {
                    if (d > startWithin) break;
                    inRun = true;
                    runStartH = hp;
                }
                else if (!steep && inRun)
                {
                    return runStartH - hp;
                }

                if (inRun) drop = runStartH - hp;
            }

            return inRun ? drop : 0f;
        }

        private static void R6(LmcSubject s, LmcBase b, LmcReadings m, LmcMeasure.CoastReading coast)
        {
            var n = s.N;
            var cell = s.Cell;
            var river = new bool[n * n];
            foreach (var t in s.Layout.Trunks) foreach (var p in t.Points) river[LmcGrid.Index(p, n, cell)] = true;
            var dRiver = LmcGrid.Edt(river, n, cell);
            var r300 = Mathf.CeilToInt(300f / cell);
            int low = 0, lowOk = 0, cliff = 0, cliffOk = 0;
            foreach (var kv in coast.ClassOf)
            {
                var i = kv.Key;
                var x = i % n; var z = i / n;
                int lowland = 0, high = 0, totalLand = 0;
                for (var dz = -r300; dz <= r300; dz += 3)
                for (var dx = -r300; dx <= r300; dx += 3)
                {
                    if (dx * dx + dz * dz > r300 * r300) continue;
                    var xx = x + dx; var zz = z + dz;
                    if (xx < 0 || zz < 0 || xx >= n || zz >= n) continue;
                    var j = zz * n + xx;
                    if (!b.Land[j]) continue;
                    totalLand++;
                    if (s.Region[j] == LmcCodes.Lowland) lowland++; else high++;
                }

                if (kv.Value == 0) { low++; if (lowland * 2 > totalLand || dRiver[i] <= 150f) lowOk++; }
                else if (kv.Value == 2) { cliff++; if (high * 2 > totalLand) cliffOk++; }
            }

            var outletsLow = 0;
            foreach (var o in s.Layout.Outlets) if (ShoreClassAt(b, coast, o) == "low") outletsLow++;
            var lowC = low > 0 ? lowOk / (double)low : 0.0;
            var cliffC = cliff > 0 ? cliffOk / (double)cliff : 0.0;
            m.Set("r6", new Dictionary<string, object>
            {
                ["low_consistent"] = lowC, ["cliff_consistent"] = cliffC, ["outlets_on_low"] = outletsLow, ["outlets"] = s.Layout.Outlets.Count,
                ["met"] = coast.Shares[0] >= 5 && coast.Shares[1] >= 5 && coast.Shares[2] >= 5 && lowC >= 0.8 && cliffC >= 0.8 && outletsLow == s.Layout.Outlets.Count,
            });
        }

        private static void R3(LmcSubject s, LmcBase b, LmcReadings m, Func<int, int> regionId)
        {
            var n = s.N;
            var count = n * n;
            var buildable = new bool[count];
            for (var i = 0; i < count; i++) buildable[i] = b.Land[i] && b.VertexSlope[i] <= LmcMeasure.BuildableDegrees;
            var regions = new HashSet<int>();
            var seen = new bool[count];
            var stack = new Stack<int>();
            for (var st = 0; st < count; st++)
            {
                if (!buildable[st] || seen[st]) continue;
                var cells = new List<int>();
                seen[st] = true;
                stack.Push(st);
                while (stack.Count > 0)
                {
                    var i = stack.Pop();
                    cells.Add(i);
                    var x = i % n; var z = i / n;
                    if (x > 0 && buildable[i - 1] && !seen[i - 1]) { seen[i - 1] = true; stack.Push(i - 1); }
                    if (x < n - 1 && buildable[i + 1] && !seen[i + 1]) { seen[i + 1] = true; stack.Push(i + 1); }
                    if (z > 0 && buildable[i - n] && !seen[i - n]) { seen[i - n] = true; stack.Push(i - n); }
                    if (z < n - 1 && buildable[i + n] && !seen[i + n]) { seen[i + n] = true; stack.Push(i + n); }
                }

                if (cells.Count * b.Cell * b.Cell < 100_000.0) continue;
                var votes = new Dictionary<int, int>();
                foreach (var i in cells) { var id = regionId(i); votes[id] = votes.TryGetValue(id, out var v) ? v + 1 : 1; }
                var best = 0; var bestId = -1;
                foreach (var kv in votes) if (kv.Value > best || (kv.Value == best && kv.Key < bestId)) { best = kv.Value; bestId = kv.Key; }
                if (bestId < 200 || bestId >= 1000) regions.Add(bestId);
            }

            m.Set("r3_regions_with_10ha", regions.Count);
        }

        private static void R11(LmcSubject s, GeographyAnalysis a, LmcBase b, LmcReadings m, List<GeographyPeak> peaks, int[] outletOf, Dictionary<int, int> catchArea, LmcMeasure.CoastReading coast)
        {
            var n = s.N;
            var cell = s.Cell;
            var count = n * n;
            var classes = new SortedDictionary<string, List<int>>(StringComparer.Ordinal);
            void Add(string k, int i) { if (!classes.TryGetValue(k, out var l)) { l = new List<int>(); classes[k] = l; } l.Add(i); }
            foreach (var pk in peaks) if (pk.Prominence >= 30f) Add("culmination or viewpoint", pk.Index);
            foreach (var pk in peaks)
            {
                if (pk.Col < 0) continue;
                var distinct = new HashSet<int>();
                var cx = pk.Col % n; var cz = pk.Col / n;
                for (var dz = -1; dz <= 1; dz++)
                for (var dx = -1; dx <= 1; dx++)
                {
                    var x = cx + dx; var z = cz + dz;
                    if (x < 0 || z < 0 || x >= n || z >= n) continue;
                    var o = outletOf[z * n + x];
                    if (o >= 0 && catchArea.TryGetValue(o, out var ar) && ar * cell * cell >= 100_000f) distinct.Add(o);
                }

                if (distinct.Count >= 2) Add("pass", pk.Col);
            }

            for (var i = 0; i < count; i++)
            {
                if (!b.Land[i]) continue;
                if (s.Region[i] == LmcCodes.PlateauTop && b.VertexSlope[i] <= 5.0) Add("plateau top", i);
                if (b.Area[i] >= 30000.0 && b.Recv[i] >= 0)
                {
                    var drop = 0.0; var path = 0.0; var j = i;
                    while (path < 30.0 && b.Recv[j] >= 0)
                    {
                        var r = b.Recv[j];
                        path += b.Cell * LmcBase.Hypot(j / n - r / n, j % n - r % n);
                        drop = b.H[i] - b.H[r];
                        j = r;
                    }

                    if (drop >= 10.0) Add("fall site", i);
                    if (b.Area[i] >= 150000.0 && b.VertexSlope[i] <= LmcMeasure.BuildableDegrees) Add("valley floor or river bank", i);
                }

                if ((s.Region[i] == LmcCodes.Lowland || s.Region[i] == LmcCodes.Upland) && b.VertexSlope[i] <= LmcMeasure.BuildableDegrees && s.Contour.Sd[i] > 60f) Add("plain or meadow", i);
            }

            foreach (var pr in m.Plateaus)
            {
                foreach (var i in pr.EscSamples) Add("plateau rim", i);
                foreach (var i in pr.Feet) Add("escarpment foot", i);
            }

            var shoreKeys = new List<int>(coast.ClassOf.Keys);
            shoreKeys.Sort();
            foreach (var i in shoreKeys)
            {
                if (coast.ClassOf[i] == 2) Add("headland or cliff coast", i);
                if (coast.ClassOf[i] == 0) Add("cove or beach", i);
            }

            if (a.Hydrology.Succeeded)
            {
                foreach (var reach in a.Hydrology.Definition.Reaches)
                {
                    if (reach.Termination == ReachTermination.Ocean && reach.Nodes.Count > 0) Add("river mouth", LmcGrid.Index(reach.Nodes[reach.Nodes.Count - 1].PositionXZ, n, cell));
                }
            }

            var result = new Dictionary<string, object>();
            foreach (var kv in classes)
            {
                var kept = new List<int>();
                var grid = new Dictionary<long, List<int>>();
                foreach (var i in kv.Value)
                {
                    var p = LmcGrid.P(i, n, cell);
                    var gx = (long)Math.Floor(p.x / 150.0); var gz = (long)Math.Floor(p.y / 150.0);
                    var ok = true;
                    for (var dz = -1; dz <= 1 && ok; dz++)
                    for (var dx = -1; dx <= 1 && ok; dx++)
                    {
                        if (!grid.TryGetValue((gx + dx) * 100000 + gz + dz, out var list)) continue;
                        foreach (var j in list) if (Vector2.Distance(LmcGrid.P(j, n, cell), p) < 150f) { ok = false; break; }
                    }

                    if (!ok) continue;
                    kept.Add(i);
                    var key = gx * 100000 + gz;
                    if (!grid.TryGetValue(key, out var cellList)) { cellList = new List<int>(); grid[key] = cellList; }
                    cellList.Add(i);
                }

                var reach = 0;
                var regions = new HashSet<int>();
                foreach (var i in kept)
                {
                    if (b.VertexReachable(i % n, i / n)) reach++;
                    regions.Add(s.Region[i] * 1000 + (s.Instance != null && s.Instance[i] >= 0 ? s.Instance[i] : 0));
                }

                result[kv.Key] = new Dictionary<string, object> { ["sites"] = kept.Count, ["reachable_share"] = kept.Count > 0 ? reach / (double)kept.Count : double.NaN, ["regions"] = regions.Count };
            }

            m.Set("r11", result);
        }

        private static void Walls(LmcBase b, LmcReadings m)
        {
            int land = 0, s1 = 0, s2 = 0, s3 = 0, reach = 0;
            for (var t = 0; t < b.TriSlope.Length; t++)
            {
                if (!b.TriLand[t]) continue;
                land++;
                if (b.TriShoreReach[t]) reach++;
                var d = b.TriSlope[t];
                if (d >= 45 && d < 60) s1++;
                else if (d >= 60 && d < 70) s2++;
                else if (d >= 70) s3++;
            }

            m.Set("walls", new Dictionary<string, object>
            {
                ["share_45_60"] = s1 / (double)land, ["share_60_70"] = s2 / (double)land, ["share_over_70"] = s3 / (double)land,
                ["note"] = "walls over 70 deg are outside the tested jump range, never called impassable",
                ["reachable_land_share"] = reach / (double)Math.Max(1, land),
            });
        }

        private static void Bands(LmcSubject s, LmcBase b, LmcReadings m)
        {
            var land = new int[3];
            var build = new int[3];
            var total = 0;
            for (var i = 0; i < b.H.Length; i++)
            {
                if (!b.Land[i]) continue;
                total++;
                var sd = s.Contour.Sd[i];
                var k = sd <= 60f ? 0 : sd <= 200f ? 1 : 2;
                land[k]++;
                if (b.VertexSlope[i] <= LmcMeasure.BuildableDegrees) build[k]++;
            }

            var names = new[] { "0-60 m", "60-200 m", "interior" };
            var result = new Dictionary<string, object>();
            for (var k = 0; k < 3; k++) result[names[k]] = new Dictionary<string, object> { ["land_share"] = land[k] / (double)total, ["buildable_share"] = build[k] / (double)Math.Max(1, land[k]) };
            m.Set("bands", result);
        }

        private static void Q4(LmcSubject s, LmcBase b, LmcReadings m)
        {
            var L = s.Layout;
            if (L.Ridges.Count == 0) return;
            var main = L.Ridges[0];
            var hs = new List<double>();
            for (var k = 0; k * 20.0 <= main.Length; k++) hs.Add(b.H[LmcGrid.Index(main.At((float)(k * 20.0)), s.N, s.Cell)]);
            double mean = 0; foreach (var v in hs) mean += v; mean /= Math.Max(1, hs.Count);
            double var = 0; foreach (var v in hs) var += (v - mean) * (v - mean);
            var cv = hs.Count > 1 ? Math.Sqrt(var / hs.Count) / mean : double.NaN;
            var saddles = new List<object>();
            var culms = new List<LmcNode>();
            foreach (var node in L.Nodes) if (node.Kind == "culmination" && node.Ridge == 0) culms.Add(node);
            culms.Sort((p, q) => p.Arc.CompareTo(q.Arc));
            for (var k = 0; k + 1 < culms.Count; k++)
            {
                var lowest = double.PositiveInfinity;
                for (var t = culms[k].Arc; t <= culms[k + 1].Arc; t += 10f) lowest = Math.Min(lowest, b.H[LmcGrid.Index(main.At(t), s.N, s.Cell)]);
                var c0 = b.H[LmcGrid.Index(culms[k].P, s.N, s.Cell)];
                var c1 = b.H[LmcGrid.Index(culms[k + 1].P, s.N, s.Cell)];
                saddles.Add(new Dictionary<string, object> { ["between"] = new List<object> { k, k + 1 }, ["depth_below_lower"] = Math.Min(c0, c1) - lowest });
            }

            m.Set("q4", new Dictionary<string, object> { ["main_divide_cv"] = cv, ["main_divide_samples"] = hs.Count, ["saddle_depths"] = saddles });
        }
    }


    /// <summary>A minimal JSON writer for the reports (invariant culture, NaN and infinities as null).</summary>
    public static class LmcJson
    {
        public static string Write(object value)
        {
            var sb = new StringBuilder();
            Append(sb, value);
            return sb.ToString();
        }

        private static void Append(StringBuilder sb, object v)
        {
            switch (v)
            {
                case null:
                    sb.Append("null");
                    break;
                case bool b:
                    sb.Append(b ? "true" : "false");
                    break;
                case string s:
                    sb.Append('"');
                    foreach (var ch in s)
                    {
                        if (ch == '"' || ch == '\\') sb.Append('\\').Append(ch);
                        else if (ch == '\n') sb.Append("\\n");
                        else if (ch < ' ') sb.Append(' ');
                        else sb.Append(ch);
                    }

                    sb.Append('"');
                    break;
                case double d:
                    sb.Append(double.IsNaN(d) || double.IsInfinity(d) ? "null" : d.ToString("R", CultureInfo.InvariantCulture));
                    break;
                case float f:
                    Append(sb, (double)f);
                    break;
                case int i:
                    sb.Append(i.ToString(CultureInfo.InvariantCulture));
                    break;
                case long l:
                    sb.Append(l.ToString(CultureInfo.InvariantCulture));
                    break;
                case Dictionary<string, object> map:
                    sb.Append('{');
                    var first = true;
                    foreach (var kv in map)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        Append(sb, kv.Key);
                        sb.Append(':');
                        Append(sb, kv.Value);
                    }

                    sb.Append('}');
                    break;
                case System.Collections.IEnumerable list:
                    sb.Append('[');
                    var f2 = true;
                    foreach (var item in list)
                    {
                        if (!f2) sb.Append(',');
                        f2 = false;
                        Append(sb, item);
                    }

                    sb.Append(']');
                    break;
                default:
                    Append(sb, Convert.ToString(v, CultureInfo.InvariantCulture));
                    break;
            }
        }
    }
}
#endif

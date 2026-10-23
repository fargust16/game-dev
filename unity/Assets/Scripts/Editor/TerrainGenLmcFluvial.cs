#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using GameDev.World;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>One synthesised candidate lattice with the construction's own record.</summary>
    public sealed class LmcSurface
    {
        public string Synth;
        public float[] H;
        public int[] Receivers;
        public double[] Area;
        public List<LmcAxis> Axes = new List<LmcAxis>();
        public float[] Angles = new float[0];
        public List<string> Report = new List<string>();
        public List<string> Defects = new List<string>();
        public Dictionary<string, object> Values = new Dictionary<string, object>();
    }

    /// <summary>Diagnostic record of S-LM3's receiver iteration (diagnostic run 6); reading only, never changes a result.</summary>
    public sealed class LmcFluvialDiag
    {
        public readonly System.Text.StringBuilder Csv = new System.Text.StringBuilder(
            "phase,iter,changed,ret2,area_below_ac,area_at_or_above_ac,turn45,turn90,turn135,turn180,turn_from_or_to_none,r1,r2,r3,r4,r5,r6,r7,unit_outlet,unit_coastal,floor,terminal_changed,terminal_same_outlet,terminal_outlet_switch,terminal_outlet_coastal,terminal_coastal_coastal,median_abs_dlnA\n");
        public readonly Dictionary<string, byte[]> ChangeMaps = new Dictionary<string, byte[]>();
    }

    /// <summary>Authoring defaults of S-LM3 (spec §5); every value is printed into the report.</summary>
    public sealed class LmcFluvialSettings
    {
        public double Theta = 0.45;                 // [AUTHORING] spec §5.2
        public double Ac = 2000.0;                  // [AUTHORING] m²
        public double CapGeneralDeg = 38.0;         // [AUTHORING]
        public double CapWallDeg = 70.0;            // [AUTHORING] escarpment and cliff bands
        public double FloorAreaMin = 150000.0;      // [AUTHORING] m²
        public double FloorHalfWidthMax = 85.0;     // HydrologySettings.ValleyHalfWidthMaxMeters [FACT]
        public double FloorSlopeDeg = 1.0;          // [AUTHORING]
        public double BaseLevel = 24.5;             // [CARRIED]
        public double StartScale = 0.05;            // [AUTHORING]
        public int MaxIterations = 30;              // [AUTHORING]
        public int BisectionSteps = 12;             // [AUTHORING]
        public double MaxHeight = 680.0;            // [AUTHORING]
        public double KLowland = 3.0;               // initial k per zone (m^(2θ)); targeted ones move
        public double KUpland = 8.0;
        public double KHighland = 25.0;
        public double KCorridor = 3.0;
        public double KPass = 10.0;
        public double KCliff = 60.0;
        public double KTop = 1.0;
        public double KBand = 300.0;
        public double KRamp = 8.0;
        public double CulminationRadius = 300.0;    // [AUTHORING] m, Gaussian factor around each culmination
        public double MainAmplitude = 1.0;          // [AUTHORING] the main culmination's factor amplitude
        public double CulminationWindow = 150.0;    // a culmination's height = highest H vertex within this
        public double CoastFringe = 100.0;          // PD-1 [AUTHORING] m, ground that may drain straight to the sea
        public double OutletReach = 100.0;          // PD-1 [AUTHORING] m, shore that is a designated outlet's base level
        public double CliffBand = 60.0;             // [AUTHORING] m, coastal cliff band behind cliff-intent shore
        public double AxisHalfWidth = 50.0;         // [CARRIED] corridor half-width; trunk and pass axes
        public double OutletSectorDeg = 12.0;       // rim outlet half-angle (layout re-entrant width)
        public double CostAxis = 0.5;               // PD-1 start-distance weights per region
        public double CostCorridor = 0.75;
        public double CostLowland = 1.0;
        public double CostUpland = 1.25;
        public double CostHighland = 1.5;
        public double CostPlateau = 1.5;

        public static LmcFluvialSettings Default => new LmcFluvialSettings();

        public IEnumerable<string> Lines()
        {
            foreach (var f in typeof(LmcFluvialSettings).GetFields())
            {
                yield return $"lm3.{f.Name} = {Convert.ToString(f.GetValue(this), CultureInfo.InvariantCulture)}";
            }
        }
    }

    /// <summary>
    /// S-LM3 — the steady state of a detachment-limited stream-power law on a drainage tree constrained by the LM2
    /// layout (spec §5). Heights are integrated upstream from base level, so every land vertex stands above its
    /// receiver: no closed depression can form. Research only.
    /// </summary>
    public static class LmcFluvial
    {
        private const int ZLow = 0, ZUp = 1, ZHi = 2, ZCor = 3, ZPass = 4, ZCliff = 5, ZPlateau0 = 6;

        private sealed class State
        {
            public int N;
            public double Cell;
            public bool[] Land;
            public bool[] Shore;
            public int[] Zone;
            public double[] Mult;
            public double[] CapTan;
            public bool[] UnitE;
            public LmcFluvialDiag Diag;
            public int[] OutletOfShore;      // diagnostic run 6: designated outlet index of an outlet-unit shore vertex, else -1
            public int[] PlateauOf;          // -1 or plateau index for codes 4–6
            public float[][] DTop;           // per plateau, distance to its top (m)
            public int[] Recv;
            public double[] Area;
            public int[] Order;
            public bool[] Floor;
            public double[] K;               // per zone
            public double[] Amp;             // per culmination node
            public double[] H;
            public List<int> CulmNodes = new List<int>();
            public double[][] CulmGauss;     // per culmination: sparse Gaussian weights for H vertices
            public List<int>[] CulmCells;
            public bool[] AxisMask;
            public int[] Sector;             // Q9 #5: 10-degree rim sector of a band vertex, else -1
            public double[][] SectorMult;    // per plateau and sector, the band's k multiplier
        }

        private static LmcSurface Build(LmcLayout L, LmcContour c, LmcFluvialSettings s, int seed)
        {
            var surface = new LmcSurface { Synth = "lm3" };
            var st = Prepare(L, c, s, surface);
            Start(L, c, s, st, surface);
            st.Diag = s_diag;
            if (st.Diag != null) st.OutletOfShore = OutletOfShore(L, st, s);
            var iterations = Iterate(st, s, seed, surface, "initial");
            Target(L, s, st, surface, "pass 1");
            iterations += Iterate(st, s, seed, surface, "after targeting");
            Target(L, s, st, surface, "pass 2");
            Integrate(st, s);

            var n = st.N;
            surface.H = new float[n * n];
            var stage = c.Stage1.Field.Heights;
            var max = 0.0;
            for (var i = 0; i < n * n; i++)
            {
                surface.H[i] = st.Land[i] ? (float)st.H[i] : stage[i];
                if (st.Land[i]) max = Math.Max(max, st.H[i]);
            }

            if (max >= s.MaxHeight) surface.Defects.Add($"height over budget: {max:F1} m >= {s.MaxHeight} m");
            var orphans = 0;
            for (var i = 0; i < n * n; i++) if (st.Land[i] && !st.Shore[i] && st.Recv[i] < 0) orphans++;
            surface.Values["land_without_receiver"] = orphans;
            if (orphans > 0) surface.Defects.Add($"{orphans} non-shore land vertices have no receiver (no allowed path to base level); they sit at base level");
            surface.Receivers = st.Recv;
            surface.Area = st.Area;
            surface.Angles = new[] { (float)s.CapGeneralDeg, (float)s.CapWallDeg, (float)s.FloorSlopeDeg };
            foreach (var t in L.Trunks) surface.Axes.Add(new LmcAxis { Name = $"trunk {t.Id}", Points = t.Points });
            surface.Values["max_height"] = max;
            surface.Values["receiver_iterations"] = iterations;
            surface.Values["constrained_differs_share"] = DiffersShare(st);
            var kmap = new Dictionary<string, object>();
            for (var z = 0; z < st.K.Length; z++) kmap[ZoneName(z)] = st.K[z];
            surface.Values["k"] = kmap;
            var amps = new List<object>();
            foreach (var a in st.Amp) amps.Add(a);
            surface.Values["culmination_amplitudes"] = amps;
            return surface;
        }

        private static string ZoneName(int z)
        {
            switch (z)
            {
                case ZLow: return "lowland";
                case ZUp: return "upland";
                case ZHi: return "highland";
                case ZCor: return "corridor";
                case ZPass: return "pass axis";
                case ZCliff: return "cliff band";
                default:
                    var p = (z - ZPlateau0) / 3;
                    var k = (z - ZPlateau0) % 3;
                    return $"plateau {p} " + (k == 0 ? "top" : k == 1 ? "band" : "ramp");
            }
        }

        // ------------------------------------------------------------------ zones, caps, units

        private static State Prepare(LmcLayout L, LmcContour c, LmcFluvialSettings s, LmcSurface surface)
        {
            var n = L.N;
            var cell = (double)L.Cell;
            var count = n * n;
            var st = new State { N = n, Cell = cell, Land = c.Land, Zone = new int[count], Mult = new double[count], CapTan = new double[count], PlateauOf = new int[count] };
            var ocean = LmcBase.OceanMask(Heights(c), n);
            st.Shore = new bool[count];
            for (var z = 0; z < n; z++)
            for (var x = 0; x < n; x++)
            {
                var i = z * n + x;
                if (!st.Land[i]) continue;
                st.Shore[i] = (z > 0 && ocean[i - n]) || (z < n - 1 && ocean[i + n]) || (x > 0 && ocean[i - 1]) || (x < n - 1 && ocean[i + 1]);
            }

            var inland = 0;
            for (var i = 0; i < count; i++) if (!st.Land[i] && !ocean[i]) inland++;
            if (inland > 0) surface.Report.Add($"note: {inland} non-land vertices are not ocean (inland water in the contour)");

            var passAxis = new bool[count];
            var axisMask = new bool[count];
            foreach (var p in L.PassCorridors) Mark(passAxis, p.Points, n, (float)cell, s.AxisHalfWidth);
            foreach (var p in L.PassCorridors) Mark(axisMask, p.Points, n, (float)cell, s.AxisHalfWidth);
            foreach (var t in L.Trunks) Mark(axisMask, t.Points, n, (float)cell, s.AxisHalfWidth);
            var cliffShore = new bool[count];
            for (var i = 0; i < count; i++) cliffShore[i] = L.ShoreIntent[i] == LmcCodes.ShoreCliff;
            var dCliff = LmcGrid.Edt(cliffShore, n, (float)cell);
            var tanGeneral = Math.Tan(s.CapGeneralDeg * Math.PI / 180.0);
            var tanWall = Math.Tan(s.CapWallDeg * Math.PI / 180.0);
            var P = L.Plateaus.Count;
            st.K = new double[ZPlateau0 + 3 * P];
            st.K[ZLow] = s.KLowland; st.K[ZUp] = s.KUpland; st.K[ZHi] = s.KHighland; st.K[ZCor] = s.KCorridor;
            st.K[ZPass] = s.KPass; st.K[ZCliff] = s.KCliff;
            for (var p = 0; p < P; p++)
            {
                st.K[ZPlateau0 + 3 * p] = s.KTop;
                st.K[ZPlateau0 + 3 * p + 1] = s.KBand;
                st.K[ZPlateau0 + 3 * p + 2] = s.KRamp;
            }

            for (var i = 0; i < count; i++)
            {
                st.Mult[i] = 1.0;
                st.CapTan[i] = tanGeneral;
                st.PlateauOf[i] = -1;
                if (!st.Land[i]) { st.Zone[i] = -1; continue; }
                var code = L.Region[i];
                switch (code)
                {
                    case LmcCodes.Lowland: st.Zone[i] = ZLow; break;
                    case LmcCodes.Highland: st.Zone[i] = passAxis[i] ? ZPass : ZHi; break;
                    case LmcCodes.Corridor: st.Zone[i] = ZCor; break;
                    case LmcCodes.PlateauTop:
                    case LmcCodes.PlateauBand:
                    case LmcCodes.PlateauRamp:
                        st.PlateauOf[i] = L.Instance[i];
                        st.Zone[i] = ZPlateau0 + 3 * L.Instance[i] + (code - LmcCodes.PlateauTop);
                        if (code == LmcCodes.PlateauBand) st.CapTan[i] = tanWall;
                        break;
                    default: st.Zone[i] = ZUp; break;
                }

                if (st.PlateauOf[i] < 0 && dCliff[i] <= s.CliffBand && code != LmcCodes.Lowland)
                {
                    st.Zone[i] = ZCliff;
                    st.CapTan[i] = tanWall;
                }
            }

            st.Sector = new int[count];
            st.SectorMult = new double[P][];
            for (var p = 0; p < P; p++) { st.SectorMult[p] = new double[36]; for (var k = 0; k < 36; k++) st.SectorMult[p][k] = 1.0; }
            for (var i = 0; i < count; i++)
            {
                st.Sector[i] = -1;
                if (L.Region[i] != LmcCodes.PlateauBand) continue;
                var b = L.Plateaus[L.Instance[i]].BearingOf(LmcGrid.P(i, n, (float)cell));
                st.Sector[i] = Mathf.Clamp((int)(b / 10f), 0, 35);
            }

            st.DTop = new float[P][];
            for (var p = 0; p < P; p++)
            {
                var top = new bool[count];
                for (var i = 0; i < count; i++) top[i] = L.Region[i] == LmcCodes.PlateauTop && L.Instance[i] == p;
                st.DTop[p] = LmcGrid.Edt(top, n, (float)cell);
            }

            // culmination factors (spec §5.4): Gaussian bumps around each declared culmination
            var nodes = new List<int>();
            for (var k = 0; k < L.Nodes.Count; k++) if (L.Nodes[k].Kind == "culmination") nodes.Add(k);
            st.CulmNodes = nodes;
            st.Amp = new double[nodes.Count];
            st.CulmGauss = new double[nodes.Count][];
            st.CulmCells = new List<int>[nodes.Count];
            for (var k = 0; k < nodes.Count; k++)
            {
                st.Amp[k] = k == 0 ? s.MainAmplitude : 0.5;
                var cells = new List<int>();
                var w = new List<double>();
                var cp = L.Nodes[nodes[k]].P;
                var r = (int)Math.Ceiling(3.0 * s.CulminationRadius / cell);
                var cx = (int)Math.Round(cp.x / cell);
                var cz = (int)Math.Round(cp.y / cell);
                for (var dz = -r; dz <= r; dz++)
                for (var dx = -r; dx <= r; dx++)
                {
                    var x = cx + dx; var z = cz + dz;
                    if (x < 0 || z < 0 || x >= n || z >= n) continue;
                    var i = z * n + x;
                    if (st.Zone[i] != ZHi) continue;
                    var d = Math.Sqrt(dx * dx + dz * dz) * cell / s.CulminationRadius;
                    cells.Add(i);
                    w.Add(Math.Exp(-d * d));
                }

                st.CulmCells[k] = cells;
                st.CulmGauss[k] = w.ToArray();
            }

            ApplyMult(st);

            // PD-1 drainage units: designated-outlet units and a coastal fringe that may drain straight to the sea
            st.UnitE = new bool[count];
            surface.Report.Add("PD-1 drainage units: shore within " + s.OutletReach + " m of a designated outlet is its base level; other shore is coastal base level reaching only ground within " + s.CoastFringe + " m of the coast outside plateaus; water may not pass from an outlet unit into a coastal unit");
            st.Area = new double[count];
            st.Recv = new int[count];
            st.H = new double[count];
            st.Floor = new bool[count];
            surface.Values["zones"] = st.K.Length;
            st.AxisMask = axisMask;
            return st;
        }

        private static double[] Heights(LmcContour c)
        {
            var h = c.Stage1.Field.Heights;
            var r = new double[h.Length];
            for (var i = 0; i < h.Length; i++) r[i] = h[i];
            return r;
        }

        private static void Mark(bool[] mask, List<Vector2> pts, int n, float cell, double halfWidth)
        {
            var d = LmcGrid.Edt(LmcGrid.Rasterize(pts, n, cell), n, cell);
            for (var i = 0; i < mask.Length; i++) if (d[i] <= halfWidth) mask[i] = true;
        }

        private static void ApplyMult(State st)
        {
            for (var i = 0; i < st.Mult.Length; i++) st.Mult[i] = 1.0;
            for (var k = 0; k < st.CulmCells.Length; k++)
            {
                var cells = st.CulmCells[k];
                var w = st.CulmGauss[k];
                for (var j = 0; j < cells.Count; j++) st.Mult[cells[j]] += st.Amp[k] * w[j];
            }

            for (var i = 0; i < st.Mult.Length; i++) st.Mult[i] = Math.Max(0.05, st.Mult[i]);
        }

        // ------------------------------------------------------------------ allowed moves (spec §5.3 + PD-1)

        private static bool Allowed(LmcLayout L, State st, LmcFluvialSettings s, int u, int v)
        {
            if (st.UnitE[u] && !st.UnitE[v]) return false;
            return Static(L, st, s, u, v);
        }

        private static bool Static(LmcLayout L, State st, LmcFluvialSettings s, int u, int v)
        {
            if (!st.Land[v]) return false;
            var pu = st.PlateauOf[u];
            var pv = st.PlateauOf[v];
            var ru = L.Region[u];
            var rv = L.Region[v];
            if (pu >= 0)
            {
                if (ru == LmcCodes.PlateauTop)
                {
                    if (pv == pu && rv == LmcCodes.PlateauTop) return true;
                    return OutletSector(L.Plateaus[pu], LmcGrid.P(v, st.N, (float)st.Cell), s.OutletSectorDeg) && rv != LmcCodes.PlateauRamp;
                }

                if (ru == LmcCodes.PlateauBand)
                {
                    if (pv == pu && rv == LmcCodes.PlateauTop) return false;
                    return st.DTop[pu][v] > st.DTop[pu][u];
                }

                if (ru == LmcCodes.PlateauRamp)
                {
                    return !(pv == pu && rv == LmcCodes.PlateauTop);
                }
            }

            if (pv >= 0 && rv == LmcCodes.PlateauTop)
            {
                // into a top only across its joint or gradual rim (never from its own band or ramp)
                var b = L.Plateaus[pv].RimAt(L.Plateaus[pv].BearingOf(LmcGrid.P(u, st.N, (float)st.Cell)));
                return b == LmcCodes.RimJoint || b == LmcCodes.RimGradual;
            }

            return true;
        }

        private static bool OutletSector(LmcPlateau pl, Vector2 p, double halfDeg)
        {
            var b = pl.BearingOf(p);
            foreach (var o in pl.OutletBearings) if (LmcPlateau.Delta(b, o) <= halfDeg) return true;
            return false;
        }

        // ------------------------------------------------------------------ start (constrained shortest path)

        private static void Start(LmcLayout L, LmcContour c, LmcFluvialSettings s, State st, LmcSurface surface)
        {
            var n = st.N;
            var count = n * n;
            var cell = st.Cell;
            var dist = new double[count];
            var label = new sbyte[count]; // 0 unset, 1 outlet unit, 2 coastal
            for (var i = 0; i < count; i++) dist[i] = double.PositiveInfinity;
            var outletMask = new bool[count];
            foreach (var o in L.Outlets) outletMask[LmcGrid.Index(o, n, (float)cell)] = true;
            var dOutlet = LmcGrid.Edt(outletMask, n, (float)cell);
            var heap = new LmcHeapD(1 << 16);
            for (var i = 0; i < count; i++)
            {
                if (!st.Shore[i]) continue;
                dist[i] = 0;
                label[i] = (sbyte)(dOutlet[i] <= s.OutletReach ? 1 : 2);
                heap.Push(0, i);
            }

            double W(int i)
            {
                if (st.AxisMask[i]) return s.CostAxis;
                switch (L.Region[i])
                {
                    case LmcCodes.Lowland: return s.CostLowland;
                    case LmcCodes.Corridor: return s.CostCorridor;
                    case LmcCodes.Highland: return s.CostHighland;
                    case LmcCodes.PlateauTop:
                    case LmcCodes.PlateauBand:
                    case LmcCodes.PlateauRamp: return s.CostPlateau;
                    default: return s.CostUpland;
                }
            }

            var prev = new int[count];
            for (var i = 0; i < count; i++) prev[i] = -1;
            for (var pass = 0; pass < 2; pass++)
            {
                if (pass == 1)
                {
                    // fallback: ground no designated outlet reaches (islets) joins the coastal units
                    var missing = 0;
                    for (var i = 0; i < count; i++)
                    {
                        if (st.Land[i] && label[i] == 0) missing++;
                        else if (st.Land[i]) heap.Push(dist[i], i);
                    }

                    if (missing == 0) break;
                    surface.Report.Add($"PD-1 fallback: {missing} land vertices reached by no outlet unit join coastal units");
                }

                while (heap.Count > 0)
                {
                    heap.Pop(out var d, out var u);
                    if (d > dist[u]) continue;
                    var x = u % n; var z = u / n;
                    for (var dz = -1; dz <= 1; dz++)
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        if (dz == 0 && dx == 0) continue;
                        var xx = x + dx; var zz = z + dz;
                        if (xx < 0 || zz < 0 || xx >= n || zz >= n) continue;
                        var v = zz * n + xx;
                        if (!st.Land[v] || st.Shore[v]) continue;
                        if (label[u] == 2 && pass == 0 && (st.PlateauOf[v] >= 0 || c.Sd[v] > s.CoastFringe)) continue;
                        if (pass == 1 && label[v] != 0) continue; // Q9 #4: the fallback only fills unlabelled ground
                        if (!Static(L, st, s, v, u)) continue;
                        var nd = d + cell * Math.Sqrt(dx * dx + dz * dz) * 0.5 * (W(u) + W(v));
                        if (nd < dist[v])
                        {
                            dist[v] = nd;
                            label[v] = label[u];
                            prev[v] = u;
                            heap.Push(nd, v);
                        }
                    }
                }
            }

            var e = 0; var cc = 0;
            for (var i = 0; i < count; i++)
            {
                st.UnitE[i] = label[i] == 1;
                if (!st.Land[i]) continue;
                if (label[i] == 1) e++; else cc++;
                st.H[i] = st.Shore[i] ? s.BaseLevel : s.BaseLevel + s.StartScale * dist[i];
                st.Recv[i] = st.Shore[i] ? -1 : prev[i];
            }

            surface.Values["outlet_unit_share"] = e / (double)Math.Max(1, e + cc);
            surface.Report.Add($"PD-1 units: outlet units {e} vertices, coastal units {cc} vertices");
        }

        // ------------------------------------------------------------------ receivers, area, heights

        private static int Iterate(State st, LmcFluvialSettings s, int seed, LmcSurface surface, string tag)
        {
            var n = st.N;
            var count = n * n;
            var cell = st.Cell;
            var L = Layout;
            Topology(st, surface);
            Integrate(st, s);
            var iterations = 0;
            var changed = 0;
            var history = new List<string>();
            int[] before = null;
            for (var it = 0; it < s.MaxIterations; it++)
            {
                iterations++;
                // Q9 #9: receivers by one ordered sweep from base level with the drainage areas of the previous
                // iterate held fixed: each vertex takes the allowed, already-settled neighbour that gives it the lowest
                // height h_j + d s_i, so on the sweep's own heights its receiver is its steepest descent (the contract's
                // rule) — evaluated in order instead of all at once, which oscillated without settling
                var next = Sweep(st, s, seed, L);
                changed = 0;
                var cycle2 = 0;
                for (var i = 0; i < count; i++)
                {
                    if (next[i] == st.Recv[i]) continue;
                    changed++;
                    if (before != null && next[i] == before[i]) cycle2++;
                }

                history.Add(changed + "/" + cycle2);
                int[] oldTerm = null;
                double[] oldArea = null;
                if (st.Diag != null)
                {
                    oldTerm = Terminals(st.Recv, st.Order, count);
                    oldArea = (double[])st.Area.Clone();
                }

                before = st.Recv;
                var old = st.Recv;
                st.Recv = next;
                if (!Topology(st, surface))
                {
                    st.Recv = old;
                    Topology(st, surface);
                    surface.Defects.Add($"{tag}: receiver update {it + 1} formed a cycle; previous receivers kept");
                    break;
                }

                if (st.Diag != null) Record(st, s, tag, it + 1, old, oldTerm, oldArea, changed, cycle2);

                Integrate(st, s);
                if (changed == 0) break;
            }

            surface.Report.Add($"receiver iteration ({tag}): {iterations} iterations, last change {changed} vertices{(changed == 0 ? " (converged)" : "")}; changed/returned-to-two-ago per iteration: {string.Join(" ", history)}");
            if (changed > 0) surface.Defects.Add($"receiver iteration ({tag}) did not converge in {s.MaxIterations}: {changed} vertices still changing; last iterate used");
            return iterations;
        }

        private static int[] Terminals(int[] recv, int[] order, int count)
        {
            var term = new int[count];
            for (var i = 0; i < count; i++) term[i] = -1;
            foreach (var i in order) term[i] = recv[i] < 0 ? i : term[recv[i]];
            return term;
        }

        private static int[] OutletOfShore(LmcLayout L, State st, LmcFluvialSettings s)
        {
            var n = st.N;
            var result = new int[n * n];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = -1;
                if (!st.Shore[i] || !st.UnitE[i]) continue;
                var p = LmcGrid.P(i, n, (float)st.Cell);
                var best = double.PositiveInfinity;
                for (var k = 0; k < L.Outlets.Count; k++)
                {
                    var d = Vector2.Distance(p, L.Outlets[k]);
                    if (d <= s.OutletReach && d < best) { best = d; result[i] = k; }
                }
            }

            return result;
        }

        /// <summary>Diagnostic run 6: one CSV row per iteration and a per-vertex change count; reads state only.</summary>
        private static void Record(State st, LmcFluvialSettings s, string tag, int iter, int[] oldRecv, int[] oldTerm, double[] oldArea, int changed, int cycle2)
        {
            var n = st.N;
            var count = n * n;
            var L = Layout;
            var newTerm = Terminals(st.Recv, st.Order, count);
            if (!st.Diag.ChangeMaps.TryGetValue(tag, out var map)) { map = new byte[count]; st.Diag.ChangeMaps[tag] = map; }
            int small = 0, large = 0, t45 = 0, t90 = 0, t135 = 0, t180 = 0, tNone = 0, uE = 0, uC = 0, floor = 0;
            var reg = new int[8];
            for (var i = 0; i < count; i++)
            {
                if (!st.Land[i] || st.Recv[i] == oldRecv[i]) continue;
                if (map[i] < 255) map[i]++;
                if (oldArea[i] < s.Ac) small++; else large++;
                var a = oldRecv[i];
                var b = st.Recv[i];
                if (a < 0 || b < 0)
                {
                    tNone++;
                }
                else
                {
                    double ax = a % n - i % n, az = a / n - i / n, bx = b % n - i % n, bz = b / n - i / n;
                    var cos = (ax * bx + az * bz) / (Math.Sqrt(ax * ax + az * az) * Math.Sqrt(bx * bx + bz * bz));
                    var deg = Math.Acos(Math.Max(-1.0, Math.Min(1.0, cos))) * 180.0 / Math.PI;
                    var k = (int)Math.Round(deg / 45.0);
                    if (k <= 1) t45++;
                    else if (k == 2) t90++;
                    else if (k == 3) t135++;
                    else t180++;
                }

                var code = L.Region[i];
                if (code >= 1 && code <= 7) reg[code]++;
                if (st.UnitE[i]) uE++; else uC++;
                if (st.Floor[i]) floor++;
            }

            int tChanged = 0, tSame = 0, tSwitch = 0, tOC = 0, tCC = 0;
            var dln = new List<double>();
            for (var i = 0; i < count; i++)
            {
                if (!st.Land[i]) continue;
                if (oldArea[i] > 0 && st.Area[i] > 0) dln.Add(Math.Abs(Math.Log(st.Area[i] / oldArea[i])));
                if (st.Shore[i] || oldTerm[i] == newTerm[i]) continue;
                tChanged++;
                var o1 = oldTerm[i] >= 0 ? st.OutletOfShore[oldTerm[i]] : -1;
                var o2 = newTerm[i] >= 0 ? st.OutletOfShore[newTerm[i]] : -1;
                if (o1 >= 0 && o2 >= 0) { if (o1 == o2) tSame++; else tSwitch++; }
                else if (o1 >= 0 || o2 >= 0) tOC++;
                else tCC++;
            }

            var c = CultureInfo.InvariantCulture;
            st.Diag.Csv.Append(string.Join(",", new[]
            {
                tag.Replace(' ', '_'), iter.ToString(c), changed.ToString(c), cycle2.ToString(c), small.ToString(c), large.ToString(c),
                t45.ToString(c), t90.ToString(c), t135.ToString(c), t180.ToString(c), tNone.ToString(c),
                reg[1].ToString(c), reg[2].ToString(c), reg[3].ToString(c), reg[4].ToString(c), reg[5].ToString(c), reg[6].ToString(c), reg[7].ToString(c),
                uE.ToString(c), uC.ToString(c), floor.ToString(c), tChanged.ToString(c), tSame.ToString(c), tSwitch.ToString(c), tOC.ToString(c), tCC.ToString(c),
                LmcMeasure.Median(dln).ToString("R", c),
            })).Append('\n');
        }

        /// <summary>Q9 #9: one ordered sweep (Dijkstra on height from base level), drainage areas held from the previous iterate.</summary>
        private static int[] Sweep(State st, LmcFluvialSettings s, int seed, LmcLayout L)
        {
            var n = st.N;
            var count = n * n;
            var cell = st.Cell;
            var floorParent = new int[count];
            for (var i = 0; i < count; i++) floorParent[i] = -1;
            ApplyFloor(st, s, floorParent); // writes parents only for floor vertices and sets st.Floor
            var acTheta = Math.Pow(s.Ac, -s.Theta);
            var floorTan = Math.Tan(s.FloorSlopeDeg * Math.PI / 180.0);
            var h = new double[count];
            var rank = new ulong[count];
            var recv = new int[count];
            var done = new bool[count];
            for (var i = 0; i < count; i++) { h[i] = double.PositiveInfinity; recv[i] = -1; }
            var heap = new LmcHeapD(1 << 16);
            for (var i = 0; i < count; i++)
            {
                if (!st.Land[i] || !st.Shore[i]) continue;
                h[i] = s.BaseLevel;
                heap.Push(h[i], i);
            }

            double Slope(int i)
            {
                var k = st.K[st.Zone[i]] * (st.Zone[i] == ZHi ? st.Mult[i] : 1.0) * (st.Sector[i] >= 0 ? st.SectorMult[st.PlateauOf[i]][st.Sector[i]] : 1.0);
                var a = st.Area[i];
                return a >= s.Ac ? Math.Min(st.CapTan[i], k * Math.Pow(a, -s.Theta)) : Math.Min(st.CapTan[i], k * acTheta) * (a / s.Ac);
            }

            while (heap.Count > 0)
            {
                heap.Pop(out var hj, out var j);
                if (done[j] || hj > h[j]) continue;
                done[j] = true;
                var x = j % n; var z = j / n;
                for (var k = 0; k < 8; k++)
                {
                    var xx = x + Dx[k]; var zz = z + Dz[k];
                    if (xx < 0 || zz < 0 || xx >= n || zz >= n) continue;
                    var i = zz * n + xx;
                    if (!st.Land[i] || st.Shore[i] || done[i]) continue;
                    var d = cell * (k < 4 ? 1.0 : 1.4142135623730951);
                    double cand;
                    if (st.Floor[i])
                    {
                        if (floorParent[i] != j) continue;
                        cand = hj + d * floorTan;
                    }
                    else
                    {
                        if (!Allowed(L, st, s, i, j)) continue;
                        cand = hj + d * Slope(i);
                    }

                    var r = Hash((ulong)seed, (ulong)i * 8UL + (ulong)k);
                    if (cand < h[i] || (cand == h[i] && r < rank[i]))
                    {
                        h[i] = cand;
                        rank[i] = r;
                        recv[i] = j;
                        heap.Push(cand, i);
                    }
                }
            }

            // ground the sweep cannot reach keeps its previous receiver (reported as orphans if it has none)
            for (var i = 0; i < count; i++) if (st.Land[i] && !st.Shore[i] && recv[i] < 0) recv[i] = st.Recv[i];
            return recv;
        }

        [ThreadStatic] private static LmcLayout s_layout;
        [ThreadStatic] private static LmcFluvialDiag s_diag;
        private static LmcLayout Layout => s_layout;

        public static LmcSurface Run(LmcLayout L, LmcContour c, LmcFluvialSettings s, int seed, LmcFluvialDiag diag = null)
        {
            s_layout = L;
            s_diag = diag;
            try
            {
                return Build(L, c, s, seed);
            }
            finally
            {
                s_layout = null;
                s_diag = null;
            }
        }

        private static readonly int[] Dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] Dz = { 0, 0, 1, -1, 1, -1, 1, -1 };

        private static ulong Hash(ulong a, ulong b)
        {
            var x = a * 0x9E3779B97F4A7C15UL ^ (b + 0x632BE59BD9B4E019UL);
            x ^= x >> 30; x *= 0xBF58476D1CE4E5B9UL;
            x ^= x >> 27; x *= 0x94D049BB133111EBUL;
            x ^= x >> 31;
            return x;
        }

        /// <summary>Floor rule (spec §5.2): lowland and corridor ground beside a large trunk drains to it at 1°.</summary>
        private static void ApplyFloor(State st, LmcFluvialSettings s, int[] recv)
        {
            var n = st.N;
            var count = n * n;
            var cell = st.Cell;
            var L = Layout;
            for (var i = 0; i < count; i++) st.Floor[i] = false;
            var dist = new double[count];
            var reach = new double[count];
            for (var i = 0; i < count; i++) dist[i] = double.PositiveInfinity;
            var heap = new LmcHeapD(1 << 14);
            var trunk = new bool[count];
            for (var i = 0; i < count; i++)
            {
                if (!st.Land[i] || st.Area[i] < s.FloorAreaMin) continue;
                trunk[i] = true;
                dist[i] = 0;
                reach[i] = Math.Min(s.FloorHalfWidthMax, 0.25 * Math.Sqrt(st.Area[i]));
                heap.Push(0, i);
            }

            var parent = new int[count];
            for (var i = 0; i < count; i++) parent[i] = -1;
            while (heap.Count > 0)
            {
                heap.Pop(out var d, out var u);
                if (d > dist[u]) continue;
                var x = u % n; var z = u / n;
                for (var k = 0; k < 8; k++)
                {
                    var xx = x + Dx[k]; var zz = z + Dz[k];
                    if (xx < 0 || zz < 0 || xx >= n || zz >= n) continue;
                    var v = zz * n + xx;
                    if (trunk[v] || st.Shore[v] || !st.Land[v]) continue;
                    if (st.Zone[v] != ZLow && st.Zone[v] != ZCor) continue;
                    var nd = d + cell * (k < 4 ? 1.0 : 1.4142135623730951);
                    if (nd > reach[u] || nd >= dist[v]) continue;
                    if (!Allowed(L, st, s, v, u)) continue;
                    dist[v] = nd;
                    reach[v] = reach[u];
                    parent[v] = u;
                    heap.Push(nd, v);
                }
            }

            for (var i = 0; i < count; i++)
            {
                if (parent[i] < 0) continue;
                recv[i] = parent[i];
                st.Floor[i] = true;
            }

            // trunk vertices never drain into floor ground (keeps the receiver graph acyclic)
            for (var i = 0; i < count; i++)
            {
                if (!trunk[i] || recv[i] < 0 || !st.Floor[recv[i]]) continue;
                recv[i] = st.Recv[i] >= 0 && !st.Floor[st.Recv[i]] ? st.Recv[i] : recv[i];
            }
        }

        /// <summary>Topological order from base level (downstream first) and drainage area; false on a cycle.</summary>
        private static bool Topology(State st, LmcSurface surface)
        {
            var n = st.N;
            var count = n * n;
            var donors = new int[count];
            for (var i = 0; i < count; i++) if (st.Land[i] && st.Recv[i] >= 0) donors[st.Recv[i]]++;
            // children lists via counting sort
            var startIdx = new int[count + 1];
            for (var i = 0; i < count; i++) if (st.Land[i] && st.Recv[i] >= 0) startIdx[st.Recv[i] + 1]++;
            for (var i = 0; i < count; i++) startIdx[i + 1] += startIdx[i];
            var fill = (int[])startIdx.Clone();
            var children = new int[startIdx[count]];
            for (var i = 0; i < count; i++) if (st.Land[i] && st.Recv[i] >= 0) children[fill[st.Recv[i]]++] = i;
            var order = new List<int>(count);
            for (var i = 0; i < count; i++) if (st.Land[i] && st.Recv[i] < 0) order.Add(i);
            for (var k = 0; k < order.Count; k++)
            {
                var u = order[k];
                for (var j = startIdx[u]; j < startIdx[u + 1]; j++) order.Add(children[j]);
            }

            var land = 0;
            for (var i = 0; i < count; i++) if (st.Land[i]) land++;
            if (order.Count != land) return false;
            st.Order = order.ToArray();
            var a0 = st.Cell * st.Cell;
            for (var i = 0; i < count; i++) st.Area[i] = st.Land[i] ? a0 : 0.0;
            for (var k = st.Order.Length - 1; k >= 0; k--)
            {
                var i = st.Order[k];
                var r = st.Recv[i];
                if (r >= 0) st.Area[r] += st.Area[i];
            }

            return true;
        }

        private static void Integrate(State st, LmcFluvialSettings s)
        {
            var n = st.N;
            var cell = st.Cell;
            var acTheta = Math.Pow(s.Ac, -s.Theta);
            var floorTan = Math.Tan(s.FloorSlopeDeg * Math.PI / 180.0);
            foreach (var i in st.Order)
            {
                var r = st.Recv[i];
                if (r < 0) { st.H[i] = s.BaseLevel; continue; }
                var d = (i % n != r % n && i / n != r / n ? 1.4142135623730951 : 1.0) * cell;
                double slope;
                if (st.Floor[i])
                {
                    slope = floorTan;
                }
                else
                {
                    var k = st.K[st.Zone[i]] * (st.Zone[i] == ZHi ? st.Mult[i] : 1.0) * (st.Sector[i] >= 0 ? st.SectorMult[st.PlateauOf[i]][st.Sector[i]] : 1.0);
                    var a = st.Area[i];
                    slope = a >= s.Ac ? Math.Min(st.CapTan[i], k * Math.Pow(a, -s.Theta)) : Math.Min(st.CapTan[i], k * acTheta) * (a / s.Ac);
                }

                st.H[i] = st.H[r] + d * slope;
            }
        }

        private static void IntegrateSubset(State st, LmcFluvialSettings s, List<int> ordered)
        {
            var n = st.N;
            var acTheta = Math.Pow(s.Ac, -s.Theta);
            foreach (var i in ordered)
            {
                var r = st.Recv[i];
                if (r < 0) continue;
                var d = (i % n != r % n && i / n != r / n ? 1.4142135623730951 : 1.0) * st.Cell;
                var k = st.K[st.Zone[i]] * (st.Sector[i] >= 0 ? st.SectorMult[st.PlateauOf[i]][st.Sector[i]] : 1.0);
                var a = st.Area[i];
                var slope = a >= s.Ac ? Math.Min(st.CapTan[i], k * Math.Pow(a, -s.Theta)) : Math.Min(st.CapTan[i], k * acTheta) * (a / s.Ac);
                st.H[i] = st.H[r] + d * slope;
            }
        }

        private static double DiffersShare(State st)
        {
            var n = st.N;
            int total = 0, differ = 0;
            for (var i = 0; i < n * n; i++)
            {
                if (!st.Land[i] || st.Shore[i]) continue;
                var x = i % n; var z = i / n;
                var best = -1; var bestS = 0.0;
                for (var k = 0; k < 8; k++)
                {
                    var xx = x + Dx[k]; var zz = z + Dz[k];
                    if (xx < 0 || zz < 0 || xx >= n || zz >= n) continue;
                    var j = zz * n + xx;
                    if (!st.Land[j]) continue;
                    var sl = (st.H[i] - st.H[j]) / (st.Cell * (k < 4 ? 1.0 : 1.4142135623730951));
                    if (sl > bestS) { bestS = sl; best = j; }
                }

                total++;
                if (best != st.Recv[i]) differ++;
            }

            return differ / (double)Math.Max(1, total);
        }

        // ------------------------------------------------------------------ height targeting (spec §5.4)

        private static void Target(LmcLayout L, LmcFluvialSettings s, State st, LmcSurface surface, string tag)
        {
            var n = st.N;
            var cell = st.Cell;
            var count = n * n;
            var c = CultureInfo.InvariantCulture;

            double MainSummit()
            {
                var m = double.NegativeInfinity;
                for (var i = 0; i < count; i++) if (st.Land[i] && L.Region[i] == LmcCodes.Highland && st.H[i] > m) m = st.H[i];
                return m;
            }

            double Culm(int k)
            {
                var p = L.Nodes[st.CulmNodes[k]].P;
                var r = (int)Math.Ceiling(s.CulminationWindow / cell);
                var cx = (int)Math.Round(p.x / cell); var cz = (int)Math.Round(p.y / cell);
                var m = double.NegativeInfinity;
                for (var dz = -r; dz <= r; dz++)
                for (var dx = -r; dx <= r; dx++)
                {
                    if ((dx * dx + dz * dz) * cell * cell > s.CulminationWindow * s.CulminationWindow) continue;
                    var x = cx + dx; var z = cz + dz;
                    if (x < 0 || z < 0 || x >= n || z >= n) continue;
                    var i = z * n + x;
                    if (st.Land[i] && L.Region[i] == LmcCodes.Highland && st.H[i] > m) m = st.H[i];
                }

                return m;
            }

            // 1. main summit (k of the highland zone)
            if (L.Main != null)
            {
                var target = L.Main.Target;
                var got = Bisect(st, s, v => st.K[ZHi] = v, st.K[ZHi], MainSummit, target, 2.0, true, out var steps);
                surface.Report.Add(string.Format(c, "{0}: main summit target {1:F1} m, achieved {2:F1} m in {3} steps (k highland {4:G4})", tag, target, got, steps, st.K[ZHi]));
                if (Math.Abs(got - target) > 2.0) surface.Defects.Add(string.Format(c, "{0}: main summit target {1:F1} m not reached ({2:F1} m)", tag, target, got));
            }

            // 2. secondary culminations (their factor amplitudes)
            for (var k = 1; k < st.CulmNodes.Count; k++)
            {
                var kk = k;
                var target = L.Nodes[st.CulmNodes[k]].Target;
                var got = BisectLinear(st, s, v => { st.Amp[kk] = v; ApplyMult(st); }, -0.9, 6.0, () => Culm(kk), target, 2.0, out var steps);
                surface.Report.Add(string.Format(c, "{0}: culmination {1} target {2:F1} m, achieved {3:F1} m in {4} steps (amplitude {5:F3})", tag, k, target, got, steps, st.Amp[kk]));
                if (Math.Abs(got - target) > 2.0) surface.Defects.Add(string.Format(c, "{0}: culmination {1} target {2:F1} m not reached ({3:F1} m)", tag, k, target, got));
            }

            // 3. plateaus: escarpment height (band k), top relief (top k), ramp meeting the top (ramp k)
            for (var p = 0; p < L.Plateaus.Count; p++)
            {
                var pl = L.Plateaus[p];
                var pp = p;
                var top = new bool[count];
                var topCells = new List<int>();
                var rampEdge = new List<(int ramp, int top)>();
                for (var i = 0; i < count; i++)
                {
                    if (L.Region[i] == LmcCodes.PlateauTop && L.Instance[i] == p) { top[i] = true; topCells.Add(i); }
                }

                for (var i = 0; i < count; i++)
                {
                    if (L.Region[i] != LmcCodes.PlateauRamp || L.Instance[i] != p) continue;
                    var x = i % n; var z = i / n;
                    for (var k = 0; k < 8; k++)
                    {
                        var xx = x + Dx[k]; var zz = z + Dz[k];
                        if (xx < 0 || zz < 0 || xx >= n || zz >= n) continue;
                        if (top[zz * n + xx]) { rampEdge.Add((i, zz * n + xx)); break; }
                    }
                }

                double EdgeRaw() => LmcMeasure.EdgeHeightLight(st.H, st.Land, n, cell, top, topCells);
                double Edge() { var e = EdgeRaw(); return double.IsNaN(e) ? 0.0 : e; } // no escarpment sample yet reads as no height
                double Relief()
                {
                    double lo = double.PositiveInfinity, hi = double.NegativeInfinity;
                    foreach (var i in topCells) { lo = Math.Min(lo, st.H[i]); hi = Math.Max(hi, st.H[i]); }
                    return hi - lo;
                }

                double RampGap()
                {
                    var gaps = new List<double>();
                    foreach (var (r, t) in rampEdge) gaps.Add(st.H[r] - st.H[t]);
                    return gaps.Count > 0 ? LmcMeasure.Median(gaps) : 0.0;
                }

                var zt = ZPlateau0 + 3 * p;
                var edge = 0.0; var se = 0; var lipFails = 0;
                for (var rep = 0; rep < 3; rep++)
                {
                    edge = Bisect(st, s, v => st.K[zt + 1] = v, st.K[zt + 1], Edge, pl.EdgeTarget, 2.0, true, out se);
                    lipFails = MatchLips(L, s, st, p, top, n);
                    Integrate(st, s);
                }
                var relief = Relief();
                var sr = 0;
                if (relief > 5.0)
                {
                    BisectBelow(st, s, v => st.K[zt] = v, st.K[zt], Relief, 5.0, out sr);
                    relief = Relief();
                }

                var gap = rampEdge.Count > 0 ? Bisect(st, s, v => st.K[zt + 2] = v, st.K[zt + 2], RampGap, 0.0, 2.0, true, out var sg, 1024.0, 4096.0) : double.NaN; // Q9 #10: wider range
                var edge2 = EdgeRaw();
                surface.Report.Add(string.Format(c, "{0}: plateau {1} edge target {2:F1} m, achieved {3:F1} m ({4} steps; after top and ramp {5:F1} m), top relief {6:F1} m ({7} steps), ramp-to-top gap {8:F1} m, rim sectors whose band cannot meet the top {9}",
                    tag, p, pl.EdgeTarget, edge, se, edge2, relief, sr, gap, lipFails));
                if (double.IsNaN(edge2) || Math.Abs(edge2 - pl.EdgeTarget) > 2.0) surface.Defects.Add(string.Format(c, "{0}: plateau {1} edge target {2:F1} m not held ({3:F1} m)", tag, p, pl.EdgeTarget, edge2));
                if (relief > 5.0) surface.Defects.Add(string.Format(c, "{0}: plateau {1} top relief {2:F1} m > 5 m", tag, p, relief));
                if (!double.IsNaN(gap) && Math.Abs(gap) > 2.0) surface.Defects.Add(string.Format(c, "{0}: plateau {1} ramp does not meet the top (gap {2:F1} m)", tag, p, gap));
            }

            // 4. the pass offset (k of the pass axis)
            if (L.Pass != null && L.PassCorridors.Count == 2)
            {
                double Offset() => PassOffset(L, st);
                var target = L.Pass.Target;
                var got = Bisect(st, s, v => st.K[ZPass] = v, st.K[ZPass], Offset, target, 5.0, true, out var steps);
                surface.Report.Add(string.Format(c, "{0}: pass offset target {1:F1} m, achieved {2:F1} m in {3} steps (k pass {4:G4})", tag, target, got, steps, st.K[ZPass]));
                if (Math.Abs(got - target) > 5.0) surface.Defects.Add(string.Format(c, "{0}: pass offset target {1:F1} m not reached ({2:F1} m)", tag, target, got));
            }
        }

        /// <summary>
        /// Q9 #5: per 10° rim sector without a rim outlet, set the band's k multiplier so its inner edge meets the top's
        /// rim (median gap −0.5 ± 1 m). The band drains away from the top, so without this its inner edge stands above a
        /// top whose height is set by its outlet — a raised lip around a dish. Returns the sectors that cannot meet it
        /// (their foot already stands above the top: the rim there reads as joined).
        /// </summary>
        private static int MatchLips(LmcLayout L, LmcFluvialSettings s, State st, int p, bool[] top, int n)
        {
            var pl = L.Plateaus[p];
            var pairs = new List<(int band, int top)>[36];
            var members = new List<int>[36];
            for (var k = 0; k < 36; k++) { pairs[k] = new List<(int, int)>(); members[k] = new List<int>(); }
            foreach (var i in st.Order)
            {
                if (st.PlateauOf[i] != p || st.Sector[i] < 0) continue;
                members[st.Sector[i]].Add(i);
                var x = i % n; var z = i / n;
                for (var k = 0; k < 8; k++)
                {
                    var xx = x + Dx[k]; var zz = z + Dz[k];
                    if (xx < 0 || zz < 0 || xx >= n || zz >= n) continue;
                    if (top[zz * n + xx]) { pairs[st.Sector[i]].Add((i, zz * n + xx)); break; }
                }
            }

            var fails = 0;
            for (var sec = 0; sec < 36; sec++)
            {
                if (pairs[sec].Count == 0) continue;
                var outlet = false;
                foreach (var o in pl.OutletBearings) outlet |= LmcPlateau.Delta(sec * 10f + 5f, o) <= s.OutletSectorDeg + 5f;
                if (outlet) continue;
                double Gap()
                {
                    var g = new List<double>();
                    foreach (var (b, t) in pairs[sec]) g.Add(st.H[b] - st.H[t]);
                    return LmcMeasure.Median(g);
                }

                double lo = 1.0 / 512.0, hi = 16.0;
                var best = st.SectorMult[p][sec]; var bestErr = double.PositiveInfinity;
                for (var step = 0; step < s.BisectionSteps; step++)
                {
                    var mid = Math.Sqrt(lo * hi);
                    st.SectorMult[p][sec] = mid;
                    IntegrateSubset(st, s, members[sec]);
                    var gap = Gap();
                    var err = Math.Abs(gap + 0.5);
                    if (err < bestErr) { bestErr = err; best = mid; }
                    if (err <= 1.0) break;
                    if (gap > -0.5) hi = mid; else lo = mid;
                }

                st.SectorMult[p][sec] = best;
                IntegrateSubset(st, s, members[sec]);
                if (bestErr > 1.0) fails++;
            }

            return fails;
        }

        /// <summary>Pass col proxy (highest point of the route basin–pass–basin along the declared corridors) minus the associated lowland level.</summary>
        private static double PassOffset(LmcLayout L, State st) => PassOffset(L, st.H, st.N, st.Cell);

        public static double PassOffset(LmcLayout L, double[] h, int n, double cell)
        {
            var col = double.NegativeInfinity;
            foreach (var corridor in L.PassCorridors)
            {
                foreach (var p in corridor.Points) col = Math.Max(col, h[LmcGrid.Index(p, n, (float)cell)]);
            }

            return col - AssociatedLowland(L, h);
        }

        public static double AssociatedLowland(LmcLayout L, double[] h)
        {
            var level = double.NegativeInfinity;
            foreach (var b in new[] { L.PassBasinA, L.PassBasinB })
            {
                var list = new List<double>();
                for (var i = 0; i < h.Length; i++) if (L.Region[i] == LmcCodes.Lowland && L.Instance[i] == b) list.Add(h[i]);
                if (list.Count > 0) level = Math.Max(level, LmcMeasure.Median(list));
            }

            return level;
        }

        /// <summary>Monotone bisection on a positive scale in log space; returns the achieved value.</summary>
        private static double Bisect(State st, LmcFluvialSettings s, Action<double> set, double start, Func<double> measure, double target, double tol, bool increasing, out int steps, double down = 64.0, double up = 64.0)
        {
            double lo = start / down, hi = start * up;
            set(start);
            Integrate(st, s);
            var v = measure();
            steps = 0;
            if (double.IsNaN(v)) return v;
            if (Math.Abs(v - target) <= tol) return v;
            var best = start; var bestErr = Math.Abs(v - target);
            for (steps = 1; steps <= s.BisectionSteps; steps++)
            {
                var mid = Math.Sqrt(lo * hi);
                set(mid);
                Integrate(st, s);
                v = measure();
                if (double.IsNaN(v)) break;
                var err = Math.Abs(v - target);
                if (err < bestErr) { bestErr = err; best = mid; }
                if (err <= tol) return v;
                if ((v < target) == increasing) lo = mid; else hi = mid;
            }

            set(best);
            Integrate(st, s);
            return measure();
        }

        private static double BisectLinear(State st, LmcFluvialSettings s, Action<double> set, double lo, double hi, Func<double> measure, double target, double tol, out int steps)
        {
            var best = 0.0; var bestErr = double.PositiveInfinity; var v = 0.0;
            for (steps = 1; steps <= s.BisectionSteps; steps++)
            {
                var mid = 0.5 * (lo + hi);
                set(mid);
                Integrate(st, s);
                v = measure();
                var err = Math.Abs(v - target);
                if (err < bestErr) { bestErr = err; best = mid; }
                if (err <= tol) return v;
                if (v < target) lo = mid; else hi = mid;
            }

            set(best);
            Integrate(st, s);
            return measure();
        }

        /// <summary>The largest scale whose measure stays at or below the limit (the measure grows with the scale).</summary>
        private static void BisectBelow(State st, LmcFluvialSettings s, Action<double> set, double start, Func<double> measure, double limit, out int steps)
        {
            double lo = start / 1024.0, hi = start;
            var ok = lo;
            for (steps = 1; steps <= s.BisectionSteps; steps++)
            {
                var mid = Math.Sqrt(lo * hi);
                set(mid);
                Integrate(st, s);
                if (measure() <= limit) { ok = mid; lo = mid; } else hi = mid;
            }

            set(ok);
            Integrate(st, s);
        }
    }
}
#endif

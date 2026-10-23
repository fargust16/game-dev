#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using GameDev.World.Seed;
using UnityEngine;
using Random = System.Random;
using WorldSeed = GameDev.World.Seed.WorldSeed;

namespace GameDev.EditorTools
{
    /// <summary>Authoring defaults of S-LM1 (spec §6, technical draft §5.1); every value is printed into the report.</summary>
    public sealed class LmcSkeletonSettings
    {
        public double BaseLevel = 24.5;            // [CARRIED]
        public double UplandRelief = 40.0;         // [AUTHORING] m above base level at the mountain boundary
        public double UplandPower = 1.3;           // [AUTHORING] upland rise v^p, v = Sd / (Sd + dH)
        public double LowlandRelief = 12.0;        // [AUTHORING] m, rise from outlet to the far end of a basin
        public double LowlandPower = 1.4;          // [AUTHORING] concave long profile (h ~ s^p)
        public double LowlandCross = 4.0;          // [AUTHORING] m, rise from the trunk to the basin edge
        public double RegionBlend = 100.0;         // [AUTHORING] m, partition-of-unity band (60–150 m)
        public double FrameBlend = 100.0;          // [AUTHORING] m, blend between two ridge frames
        public double BankDeg = 30.0;              // Q9 #16 [AUTHORING] trough banks where a floor is lowered
        public double BankReach = 40.0;            // Q9 #16 [AUTHORING] m
        public double CrestKeep = 30.0;            // Q9 #8 [AUTHORING] m, frame blending fades in away from the crest
        public double DrainPower = 1.2;            // Q9 #6 [AUTHORING] drainage surface ~ distance^p (concave long profile)
        public double CostAxis = 0.5;              // Q9 #6 [AUTHORING] flow-distance weights per region
        public double CostCorridor = 0.75;
        public double CostLowland = 1.0;
        public double CostUpland = 1.5;
        public double CostHighland = 2.0;
        public double CostPlateau = 1.5;
        public double GammaMin = 1.4;              // [AUTHORING] long flank profile exponent
        public double GammaMax = 2.0;
        public double SpurSpacing = 0.85;          // RidgeSettings.SpurSpacingPerHalfWidth [FACT]
        public double SpurJitter = 0.25;           // RidgeSettings.SpurStationJitter [FACT]
        public double ValleyDepth = 0.30;          // RidgeSettings.ValleyDepthPerCrest [FACT]
        public double ValleyOnset = 0.20;          // [AUTHORING] u where valleys reach full depth
        public double FloorFraction = 0.20;        // [AUTHORING] share of a strip that is valley floor
        public double EndShare = 0.30;             // [AUTHORING] ridge end height: toe + share × (nearest node − toe)
        public double CorridorHalfWidth = 50.0;    // [CARRIED]
        public double CorridorPower = 1.6;         // [AUTHORING] concave corridor long profile
        public double PassHalfWidth = 60.0;        // [AUTHORING]
        public double WallDeg = 70.0;              // [AUTHORING] 65–75°
        public double TalusDeg = 33.0;             // [AUTHORING] 30–35°
        public double TalusWidth = 30.0;           // [AUTHORING] 20–40 m
        public double TalusShare = 0.35;           // [AUTHORING] talus height ≤ share × class height
        public double RampDeg = 18.0;              // [AUTHORING] ≤ 20°
        public double RampWidth = 40.0;            // ramp rule (b) [AUTHORING] m, the layout's minimum ramp width (V-L6)
        public double TopFallDeg = 1.2;            // [AUTHORING] ≤ 1.5° toward rim outlets
        public double GradualWidthMin = 60.0;      // Q9 #12 [AUTHORING] joint and gradual rim blend width (was 100 m)
        public double GradualWidthPerHeight = 1.5; // Q9 #12 [AUTHORING] (was 3)
        public int EdgeCorrections = 3;            // PD-S3 [AUTHORING] top-height corrections toward the class height
        public double CoastBandLow = 120.0;        // [AUTHORING] m
        public double CoastBandRocky = 50.0;
        public double CoastBandCliff = 25.0;
        public double CoastLowDeg = 2.0;           // ≤ 3°
        public double CoastRockyDeg = 10.0;        // Q9 #13 ≤ 30° (was 25°: a rocky shore rose > 15 m within 60 m and read as cliff)
        public double CoastCliffDeg = 65.0;        // 60–75°
        public double CliffRim = 25.0;             // [AUTHORING] m, hinterland raised behind cliff shores
        public double CliffRimWidth = 60.0;        // [AUTHORING] m
        public double IntentSmoothing = 40.0;      // [AUTHORING] m, partition of unity along the shore

        public static LmcSkeletonSettings Default => new LmcSkeletonSettings();

        public IEnumerable<string> Lines()
        {
            foreach (var f in typeof(LmcSkeletonSettings).GetFields())
            {
                yield return $"lm1.{f.Name} = {Convert.ToString(f.GetValue(this), CultureInfo.InvariantCulture)}";
            }
        }
    }

    /// <summary>
    /// S-LM1 — a generalised skeleton strip partition (spec §6): every land vertex is a point function of the LM2
    /// layout. Owners: plateau outline, then the coastal band, then the flank frame of a divide ridge, then the
    /// lowland or upland region; where two owners meet, a partition of unity across a declared band interpolates
    /// their heights. Research only.
    /// </summary>
    public static class LmcSkeleton
    {
        private sealed class Frame
        {
            public int Ridge;
            public float[] Dist;
            public int[] Owner;
            public List<Vector2> Points = new List<Vector2>();
            public List<double> Arc = new List<double>();
            public double[] Gamma = new double[2];
            public List<double>[] Stations = { new List<double>(), new List<double>() };
            public bool[] FirstIsSpur = new bool[2];
            public double[] NodeArc;
            public double[] NodeH;
            public double[] Slopes;
        }

        public static LmcSurface Build(LmcLayout L, LmcContour c, LmcSkeletonSettings s, int seed, string diagDir = null)
        {
            var surface = new LmcSurface { Synth = "lm1" };
            // diagnostic run 6: copies of each construction stage, written outside Assets; never read back
            void Stage(string name, double[] a)
            {
                if (diagDir == null) return;
                var f = new float[a.Length];
                for (var i = 0; i < a.Length; i++) f[i] = (float)a[i];
                var bytes = new byte[f.Length * 4];
                Buffer.BlockCopy(f, 0, bytes, 0, bytes.Length);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(diagDir, "lm1_stage_" + name + ".f32"), bytes);
            }

            var rng = new WorldSeed(seed).Fork("research.lmc.skeleton");
            var n = L.N;
            var cell = L.Cell;
            var count = n * n;
            var land = c.Land;
            var region = L.Region;
            var highland = LmcLayoutBuilder.Mask(region, LmcCodes.Highland);
            var notHigh = new bool[count];
            for (var i = 0; i < count; i++) notHigh[i] = !highland[i];
            var dH = LmcGrid.Edt(highland, n, cell);     // distance to the mountain region
            var dB = LmcGrid.Edt(notHigh, n, cell);      // inside it: distance to its boundary

            // ---------------- shore intents as a partition of unity along the shore (weights low / rocky / cliff)
            var wIntent = IntentWeights(L, c, s, n, cell, out var shoreOwner);

            // ---------------- base (Q9 #6): min of a drainage surface rising with the weighted flow distance to the
            // designated outlets and a coastal surface rising from the shore by its intent — both free of closed
            // depressions, so their minimum is too; ground where the drainage surface is lower drains to an outlet
            var B = Base(L, c, s, n, cell, wIntent, highland, out var diagZE, out var diagZC, out var diagDE);
            Stage("01_drainage_surface_zE", diagZE);
            Stage("02_coastal_surface_zC", diagZC);
            Stage("03_flow_distance_to_outlets", diagDE);
            Stage("04_base_B", B);
            var z = (double[])B.Clone();

            // ---------------- the mountain: ridge frames, crest profile, flanks with spurs and valleys
            // recording run 10 (review §19): the mountain stage's terms per vertex, only with diagnostics; never read back
            Dictionary<string, float[]> mtn = null;
            float[] toeSrc = null;
            if (diagDir != null)
            {
                mtn = new Dictionary<string, float[]>();
                foreach (var name in MountainRecordFields)
                {
                    var a = new float[count];
                    for (var i = 0; i < count; i++) a[i] = name == "f1" || name == "f2" ? -1f : float.NaN;
                    mtn[name] = a;
                }

                toeSrc = new float[count];
                for (var i = 0; i < count; i++) toeSrc[i] = -1f;
            }

            var frames = Frames(L, s, rng, highland, dB, n, cell);
            var toe = ToeField(highland, B, n, cell, out var toeIterations, out var toeResidual);
            surface.Report.Add(string.Format(CultureInfo.InvariantCulture, "toe: harmonic (T1), conjugate gradients {0} iterations, relative residual {1:E2}", toeIterations, toeResidual));
            if (toeResidual > 1e-12) surface.Defects.Add(string.Format(CultureInfo.InvariantCulture, "toe solve not converged: {0} iterations, relative residual {1:E2}", toeIterations, toeResidual));
            // a harmonic toe has no source vertex: lm1_mtn_toe_src stays -1 (run 11 pre-registration)
            foreach (var f in frames) CrestProfile(L, s, f, toe, n, cell, surface);
            Mountain(L, s, frames, highland, dB, toe, z, n, cell, mtn);
            Stage("05_toe", toe);
            Stage("06_after_mountain", z);
            if (mtn != null)
            {
                mtn["toe_src"] = toeSrc;
                foreach (var kv in mtn)
                {
                    var bytes = new byte[kv.Value.Length * 4];
                    Buffer.BlockCopy(kv.Value, 0, bytes, 0, bytes.Length);
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(diagDir, "lm1_mtn_" + kv.Key + ".f32"), bytes);
                }
            }

            // ---------------- the pass: crest node set in the profile; corridors descend from it to both basins
            if (L.Pass != null)
            {
                foreach (var corridor in L.PassCorridors) Corridor(L, s, corridor, z, B, n, cell, c, 255, s.PassHalfWidth, PassHeight(L, B));
            }

            Stage("07_after_pass_corridors", z);

            // ---------------- option (b) (review §17.5): declared drainage the plateau stage never raises — corridor
            // ground and the floors of the declared axes; plateau tops are exempt (they are the plateau's outline)
            var valleyAxes = ValleyAxes(frames, highland, n, cell);
            var protect = new bool[count];
            for (var i = 0; i < count; i++) protect[i] = region[i] == LmcCodes.Corridor;
            foreach (var t in L.Trunks) foreach (var q in LmcLayoutBuilder.Resample(t.Points, 0.5f * cell)) protect[LmcGrid.Index(q, n, cell)] = true;
            foreach (var p in L.PassCorridors) foreach (var q in LmcLayoutBuilder.Resample(p.Points, 0.5f * cell)) protect[LmcGrid.Index(q, n, cell)] = true;
            foreach (var ax in valleyAxes) foreach (var q in ax.Points) protect[LmcGrid.Index(q, n, cell)] = true;
            for (var i = 0; i < count; i++) if (region[i] == LmcCodes.PlateauTop) protect[i] = false;

            // ---------------- plateaus as skeleton elements (PD-S3: top height corrected toward the class height)
            var plateauLines = new List<object>();
            foreach (var pl in L.Plateaus) plateauLines.Add(Plateau(L, s, pl, z, B, highland, n, cell, land, surface, protect));
            surface.Values["plateaus"] = plateauLines;
            Stage("08_after_plateaus_before_running_minimum", z);

            // the coast is the base's coastal surface (Q9 #6), so no separate coastal pass follows

            // ---------------- running minimum along every declared valley axis (after blending)
            var axes = new List<LmcAxis>();
            foreach (var t in L.Trunks) axes.Add(new LmcAxis { Name = $"trunk {t.Id}", Points = t.Points });
            foreach (var p in L.PassCorridors) axes.Add(new LmcAxis { Name = $"pass corridor {p.Id}", Points = p.Points });
            // Q9 #11: the running minimum walks the declared axes vertex by vertex (a point every 20 m left one-vertex
            // notches — closed depressions); the mountain's valley lines are monotone by construction and only measured
            var dense = new List<LmcAxis>();
            foreach (var ax in axes) dense.Add(new LmcAxis { Name = ax.Name, Points = LmcLayoutBuilder.Resample(ax.Points, 0.5f * cell) });
            axes.AddRange(valleyAxes);
            var lowered = 0;
            var passes = 0;
            for (var pass = 0; pass < 16; pass++)
            {
                passes++;
                var round = 0;
                foreach (var ax in dense) round += RunningMinimum(ax, z, n, cell, land, s.BaseLevel, Math.Tan(s.BankDeg * Math.PI / 180.0), s.BankReach);
                lowered += round;
                if (round == 0) break; // Q9 #7: repeated until no axis is lowered (axes share vertices)
            }

            surface.Report.Add($"running minimum along {axes.Count} declared valley axes lowered {lowered} vertices in {passes} passes");
            surface.Axes = axes;
            Stage("09_after_running_minimum", z);
            if (diagDir != null)
            {
                List<object> Pts(List<Vector2> pts)
                {
                    var list = new List<object>();
                    foreach (var q in pts) list.Add(new List<object> { q.x, q.y });
                    return list;
                }

                var axesJson = new List<object>();
                foreach (var ax in axes) axesJson.Add(new Dictionary<string, object> { ["name"] = ax.Name, ["points"] = Pts(ax.Points) });
                var denseJson = new List<object>();
                foreach (var ax in dense) denseJson.Add(new Dictionary<string, object> { ["name"] = ax.Name, ["points"] = Pts(ax.Points) });
                var framesJson = new List<object>();
                foreach (var f in frames)
                {
                    framesJson.Add(new Dictionary<string, object>
                    {
                        ["ridge"] = f.Ridge, ["node_arc"] = new List<double>(f.NodeArc), ["node_height"] = new List<double>(f.NodeH),
                        ["gamma"] = new List<double>(f.Gamma),
                    });
                }

                System.IO.File.WriteAllText(System.IO.Path.Combine(diagDir, "lm1_axes_and_frames.json"), LmcJson.Write(new Dictionary<string, object>
                {
                    ["axes_measured"] = axesJson, ["axes_running_minimum"] = denseJson, ["frames"] = framesJson,
                }));
            }

            surface.H = new float[count];
            var stage = c.Stage1.Field.Heights;
            var max = 0.0;
            for (var i = 0; i < count; i++)
            {
                surface.H[i] = land[i] ? (float)Math.Max(s.BaseLevel, z[i]) : stage[i];
                if (land[i]) max = Math.Max(max, z[i]);
            }

            surface.Values["max_height"] = max;
            surface.Angles = new[] { (float)s.WallDeg, (float)s.TalusDeg, (float)s.RampDeg, (float)s.TopFallDeg, (float)s.CoastLowDeg, (float)s.CoastRockyDeg, (float)s.CoastCliffDeg };
            var gam = new List<object>();
            foreach (var f in frames) gam.Add(new List<object> { f.Ridge, f.Gamma[0], f.Gamma[1], f.Stations[0].Count, f.Stations[1].Count });
            surface.Values["frames"] = gam;
            return surface;
        }

        private static double Smooth(double t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return t * t * (3 - 2 * t);
        }

        // ------------------------------------------------------------------ shore intents

        private static double[] IntentWeights(LmcLayout L, LmcContour c, LmcSkeletonSettings s, int n, float cell, out int[] owner)
        {
            var count = n * n;
            var shore = new List<int>();
            for (var i = 0; i < count; i++) if (L.ShoreIntent[i] != LmcCodes.ShoreNone) shore.Add(i);
            var sw = new double[3 * shore.Count];
            var r = (int)Math.Ceiling(s.IntentSmoothing / cell);
            var index = new Dictionary<int, int>();
            for (var k = 0; k < shore.Count; k++) index[shore[k]] = k;
            for (var k = 0; k < shore.Count; k++)
            {
                var i = shore[k];
                var x = i % n; var zz = i / n;
                var acc = new double[3];
                var tot = 0.0;
                for (var dz = -r; dz <= r; dz++)
                for (var dx = -r; dx <= r; dx++)
                {
                    var xx = x + dx; var z2 = zz + dz;
                    if (xx < 0 || z2 < 0 || xx >= n || z2 >= n) continue;
                    var j = z2 * n + xx;
                    var intent = L.ShoreIntent[j];
                    if (intent == LmcCodes.ShoreNone) continue;
                    var d = Math.Sqrt(dx * dx + dz * dz) * cell;
                    if (d > s.IntentSmoothing) continue;
                    var w = 1.0 - Smooth(d / s.IntentSmoothing);
                    acc[intent] += w;
                    tot += w;
                }

                for (var q = 0; q < 3; q++) sw[3 * k + q] = tot > 0 ? acc[q] / tot : (q == 1 ? 1 : 0);
            }

            var landMask = c.Land;
            LmcLayoutBuilder.WeightedGeodesic(landMask, shore, n, cell, _ => 1f, out owner);
            var weights = new double[3 * count];
            for (var i = 0; i < count; i++)
            {
                if (!landMask[i] || owner[i] < 0) { weights[3 * i + 1] = 1; continue; }
                for (var q = 0; q < 3; q++) weights[3 * i + q] = sw[3 * owner[i] + q];
            }

            return weights;
        }


        /// <summary>Q9 #6: the base = min(drainage surface to the designated outlets, coastal surface by shore intent).</summary>
        private static double[] Base(LmcLayout L, LmcContour c, LmcSkeletonSettings s, int n, float cell, double[] wIntent, bool[] highland, out double[] zEOut, out double[] zCOut, out double[] dEOut)
        {
            var count = n * n;
            var land = c.Land;
            var axis = new bool[count];
            foreach (var t in L.Trunks) MarkAxis(axis, t.Points, n, cell, s.CorridorHalfWidth);
            foreach (var p in L.PassCorridors) MarkAxis(axis, p.Points, n, cell, s.CorridorHalfWidth);
            float Cost(int i)
            {
                if (axis[i] && !highland[i]) return (float)s.CostAxis;
                switch (L.Region[i])
                {
                    case LmcCodes.Lowland: return (float)s.CostLowland;
                    case LmcCodes.Corridor: return (float)s.CostCorridor;
                    case LmcCodes.Highland: return (float)s.CostHighland;
                    case LmcCodes.PlateauTop:
                    case LmcCodes.PlateauBand:
                    case LmcCodes.PlateauRamp: return (float)s.CostPlateau;
                    default: return (float)s.CostUpland;
                }
            }

            var src = new List<int>();
            foreach (var o in L.Outlets) src.Add(LmcGrid.Index(o, n, cell));
            var dE = LmcLayoutBuilder.WeightedGeodesic(land, src, n, cell, Cost, out _);
            // scale: the median flow distance at the mountain's boundary reaches the upland relief
            var atToe = new List<double>();
            for (var i = 0; i < count; i++)
            {
                if (!highland[i] || float.IsInfinity(dE[i])) continue;
                var x = i % n; var z = i / n;
                if (x > 0 && x < n - 1 && z > 0 && z < n - 1 && (!highland[i - 1] || !highland[i + 1] || !highland[i - n] || !highland[i + n])) atToe.Add(dE[i]);
            }

            var dRef = atToe.Count > 0 ? LmcMeasure.Median(atToe) : 1000.0;
            var a = s.UplandRelief / Math.Pow(Math.Max(1.0, dRef), s.DrainPower);
            var tl = Math.Tan(s.CoastLowDeg * Math.PI / 180.0);
            var tr = Math.Tan(s.CoastRockyDeg * Math.PI / 180.0);
            var tc = Math.Tan(s.CoastCliffDeg * Math.PI / 180.0);
            var b = new double[count];
            zEOut = new double[count];
            zCOut = new double[count];
            dEOut = new double[count];
            for (var i = 0; i < count; i++)
            {
                if (!land[i]) continue;
                var zE = float.IsInfinity(dE[i]) ? double.PositiveInfinity : s.BaseLevel + a * Math.Pow(dE[i], s.DrainPower);
                var t = wIntent[3 * i] * tl + wIntent[3 * i + 1] * tr + wIntent[3 * i + 2] * tc;
                var zC = s.BaseLevel + Math.Max(0.0, c.Sd[i]) * t;
                b[i] = Math.Min(zE, zC);
                zEOut[i] = zE;
                zCOut[i] = zC;
                dEOut[i] = dE[i];
            }

            return b;
        }

        private static void MarkAxis(bool[] mask, List<Vector2> pts, int n, float cell, double halfWidth)
        {
            var d = LmcGrid.Edt(LmcGrid.Rasterize(pts, n, cell), n, cell);
            for (var i = 0; i < mask.Length; i++) if (d[i] <= halfWidth) mask[i] = true;
        }

        // ------------------------------------------------------------------ lowland basins

        private static double[] LowlandField(LmcLayout L, LmcContour c, LmcSkeletonSettings s, int n, float cell, bool[] lowland)
        {
            var count = n * n;
            var h = new double[count];
            for (var i = 0; i < count; i++) h[i] = double.NaN;
            var trunkMask = new bool[count];
            foreach (var t in L.Trunks) foreach (var p in t.Points) trunkMask[LmcGrid.Index(p, n, cell)] = true;
            var dTrunk = LmcGrid.Edt(trunkMask, n, cell);
            var notLow = new bool[count];
            for (var i = 0; i < count; i++) notLow[i] = !lowland[i];
            var dEdge = LmcGrid.Edt(notLow, n, cell);
            for (var b = 0; b < L.BasinCount; b++)
            {
                var domain = new bool[count];
                for (var i = 0; i < count; i++) domain[i] = lowland[i] && L.Instance[i] == b;
                var src = new List<int>();
                for (var k = 0; k < L.Outlets.Count; k++) if (L.OutletBasin[k] == b) src.Add(LmcGrid.Index(L.Outlets[k], n, cell));
                if (src.Count == 0) continue;
                var d = LmcLayoutBuilder.WeightedGeodesic(domain, src, n, cell, _ => 1f, out _);
                var dmax = 1.0;
                for (var i = 0; i < count; i++) if (domain[i] && !float.IsInfinity(d[i])) dmax = Math.Max(dmax, d[i]);
                for (var i = 0; i < count; i++)
                {
                    if (!domain[i]) continue;
                    var along = float.IsInfinity(d[i]) ? 1.0 : d[i] / dmax;
                    var cross = dTrunk[i] / Math.Max(1e-6, dTrunk[i] + dEdge[i]);
                    h[i] = s.BaseLevel + s.LowlandRelief * Math.Pow(along, s.LowlandPower) + s.LowlandCross * Math.Pow(cross, 1.5);
                }
            }

            // extend each basin's surface a blend band outward (nearest lowland vertex, the cross term saturated)
            var srcs = new List<int>();
            for (var i = 0; i < count; i++) if (lowland[i] && !double.IsNaN(h[i])) srcs.Add(i);
            var land = c.Land;
            var dist = LmcLayoutBuilder.WeightedGeodesic(land, srcs, n, cell, _ => 1f, out var own);
            for (var i = 0; i < count; i++)
            {
                if (lowland[i] || own[i] < 0 || dist[i] > s.RegionBlend) continue;
                h[i] = h[srcs[own[i]]] + s.LowlandCross * 0.25;
            }

            return h;
        }

        // ------------------------------------------------------------------ corridors (trunk heads and the pass)

        private static void Corridor(LmcLayout L, LmcSkeletonSettings s, LmcRidge path, double[] z, double[] B, int n, float cell, LmcContour c, byte code, double halfWidth, double headHeight)
        {
            if (path.Points.Count < 2) return;
            // the corridor reaches the basin where the path first enters lowland
            var endArc = path.Length;
            for (var k = 0; k < path.Points.Count; k++)
            {
                if (L.Region[LmcGrid.Index(path.Points[k], n, cell)] == LmcCodes.Lowland) { endArc = path.Arc[k]; break; }
            }

            var head = double.IsNaN(headHeight) ? B[LmcGrid.Index(path.Points[0], n, cell)] : headHeight;
            var junction = B[LmcGrid.Index(path.At((float)endArc), n, cell)];
            if (junction > head) junction = head;
            var rc = (int)Math.Ceiling(halfWidth / cell) + 1;
            var count = n * n;
            var best = new Dictionary<int, (double d, double arc)>();
            for (var k = 0; k < path.Points.Count; k++)
            {
                if (path.Arc[k] > endArc + halfWidth) break;
                var p = path.Points[k];
                var cx = (int)Math.Round(p.x / cell); var cz = (int)Math.Round(p.y / cell);
                for (var dz = -rc; dz <= rc; dz++)
                for (var dx = -rc; dx <= rc; dx++)
                {
                    var x = cx + dx; var zz = cz + dz;
                    if (x < 0 || zz < 0 || x >= n || zz >= n) continue;
                    var i = zz * n + x;
                    if (!c.Land[i]) continue;
                    var dist = Vector2.Distance(new Vector2(x * cell, zz * cell), p);
                    if (dist > halfWidth) continue;
                    if (!best.TryGetValue(i, out var cur) || dist < cur.d) best[i] = (dist, path.Arc[k]);
                }
            }

            foreach (var kv in best)
            {
                var i = kv.Key;
                var reg = L.Region[i];
                if (reg == LmcCodes.Lowland || reg == LmcCodes.PlateauTop || reg == LmcCodes.PlateauBand || reg == LmcCodes.PlateauRamp) continue;
                if (code != 255 && reg != code) continue;
                var arc = Math.Min(kv.Value.arc, endArc);
                var floor = junction + (head - junction) * Math.Pow(1 - arc / Math.Max(1e-6, endArc), s.CorridorPower);
                if (floor >= z[i]) continue;
                var w = Smooth(kv.Value.d / halfWidth);
                z[i] = floor + (z[i] - floor) * w;
            }
        }

        private static double PassHeight(LmcLayout L, double[] B) => LmcFluvial.AssociatedLowland(L, B) + L.Pass.Target;

        // ------------------------------------------------------------------ the mountain

        private static List<Frame> Frames(LmcLayout L, LmcSkeletonSettings s, Random rng, bool[] highland, float[] dB, int n, float cell)
        {
            var frames = new List<Frame>();
            foreach (var ridge in L.Ridges)
            {
                var f = new Frame { Ridge = ridge.Id };
                var dense = LmcLayoutBuilder.Resample(ridge.Points, 0.5f * cell);
                var arc = 0.0;
                for (var k = 0; k < dense.Count; k++)
                {
                    if (k > 0) arc += Vector2.Distance(dense[k - 1], dense[k]);
                    f.Points.Add(dense[k]);
                    f.Arc.Add(arc);
                }

                var src = new List<int>();
                foreach (var p in f.Points) src.Add(LmcGrid.Index(p, n, cell));
                f.Dist = LmcLayoutBuilder.WeightedGeodesic(highland, src, n, cell, _ => 1f, out f.Owner);
                for (var side = 0; side < 2; side++)
                {
                    f.Gamma[side] = s.GammaMin + rng.NextDouble() * (s.GammaMax - s.GammaMin);
                    f.FirstIsSpur[side] = rng.NextDouble() < 0.5;
                    var st = rng.NextDouble();
                    var a = 0.0;
                    var total = f.Arc[f.Arc.Count - 1];
                    var first = true;
                    while (a <= total + 1e-6)
                    {
                        var hw = Math.Max(30.0, dB[LmcGrid.Index(At(f, a), n, cell)]);
                        var delta = s.SpurSpacing * hw * (1 + s.SpurJitter * (2 * rng.NextDouble() - 1));
                        a += first ? st * delta : delta;
                        first = false;
                        f.Stations[side].Add(a);
                    }
                }

                frames.Add(f);
            }

            return frames;
        }

        private static Vector2 At(Frame f, double arc)
        {
            var k = f.Arc.BinarySearch(arc);
            if (k < 0) k = ~k;
            k = Math.Max(0, Math.Min(f.Points.Count - 1, k));
            return f.Points[k];
        }

        /// <summary>
        /// T1 (review §22): the harmonic toe. Mountain-boundary vertices keep their own base; interior mountain vertices solve
        /// the discrete Laplace equation 4 t_i − Σ t_j = 0 over their four neighbours with those boundary values, by conjugate
        /// gradients in double precision (ascending index order; today's toe — the base at the geodesic-nearest boundary
        /// vertex — as the start; stop at a relative residual of 1e-12 or 50 000 iterations).
        /// </summary>
        private static double[] ToeField(bool[] highland, double[] B, int n, float cell, out int iterations, out double relativeResidual)
        {
            var count = n * n;
            var boundary = new List<int>();
            var isBoundary = new bool[count];
            for (var i = 0; i < count; i++)
            {
                if (!highland[i]) continue;
                var x = i % n; var z = i / n;
                if (x == 0 || z == 0 || x == n - 1 || z == n - 1 || !highland[i - 1] || !highland[i + 1] || !highland[i - n] || !highland[i + n])
                {
                    boundary.Add(i);
                    isBoundary[i] = true;
                }
            }

            LmcLayoutBuilder.WeightedGeodesic(highland, boundary, n, cell, _ => 1f, out var owner);
            var toe = new double[count];
            for (var i = 0; i < count; i++) toe[i] = highland[i] && owner[i] >= 0 ? B[boundary[owner[i]]] : B[i];

            var interior = new List<int>();
            var pos = new int[count];
            for (var i = 0; i < count; i++)
            {
                pos[i] = -1;
                if (highland[i] && !isBoundary[i]) { pos[i] = interior.Count; interior.Add(i); }
            }

            var m = interior.Count;
            var nbr = new int[4 * m];
            var b = new double[m];
            var sol = new double[m];
            for (var k = 0; k < m; k++)
            {
                var i = interior[k];
                var q = 0;
                foreach (var j in new[] { i - 1, i + 1, i - n, i + n })
                {
                    nbr[4 * k + q++] = pos[j];
                    if (pos[j] < 0) b[k] += B[j]; // a boundary neighbour: its value is fixed
                }

                sol[k] = toe[i];
            }

            void Laplace(double[] v, double[] result)
            {
                for (var k = 0; k < m; k++)
                {
                    var s = 4 * v[k];
                    for (var q = 0; q < 4; q++)
                    {
                        var j = nbr[4 * k + q];
                        if (j >= 0) s -= v[j];
                    }

                    result[k] = s;
                }
            }

            double Dot(double[] u, double[] v)
            {
                var s = 0.0;
                for (var k = 0; k < m; k++) s += u[k] * v[k];
                return s;
            }

            var r = new double[m];
            var p = new double[m];
            var ap = new double[m];
            Laplace(sol, ap);
            for (var k = 0; k < m; k++) { r[k] = b[k] - ap[k]; p[k] = r[k]; }
            var rs = Dot(r, r);
            var r0 = Math.Sqrt(rs);
            iterations = 0;
            while (Math.Sqrt(rs) > 1e-12 * r0 && iterations < 50000)
            {
                Laplace(p, ap);
                var alpha = rs / Dot(p, ap);
                for (var k = 0; k < m; k++) { sol[k] += alpha * p[k]; r[k] -= alpha * ap[k]; }
                var rn = Dot(r, r);
                var beta = rn / rs;
                for (var k = 0; k < m; k++) p[k] = r[k] + beta * p[k];
                rs = rn;
                iterations++;
            }

            relativeResidual = r0 > 0 ? Math.Sqrt(rs) / r0 : 0.0;
            for (var k = 0; k < m; k++) toe[interior[k]] = sol[k];
            foreach (var i in boundary) toe[i] = B[i];
            return toe;
        }

        /// <summary>Monotone piecewise cubic (Fritsch–Carlson) through the ridge's nodes (technical draft §5.1).</summary>
        private static void CrestProfile(LmcLayout L, LmcSkeletonSettings s, Frame f, double[] toe, int n, float cell, LmcSurface surface)
        {
            var ridge = L.Ridges[f.Ridge];
            var pts = new List<(double arc, double h)>();
            var total = f.Arc[f.Arc.Count - 1];
            foreach (var node in L.Nodes)
            {
                if (node.Ridge != f.Ridge) continue;
                var arc = ProjectArc(f, node.P);
                var h = node.Kind == "pass" ? PassHeightFromToe(L, toe, n) : node.Target;
                pts.Add((arc, h));
            }

            if (f.Ridge > 0 && ridge.Parent >= 0)
            {
                // the branch root takes the parent's crest at the attachment (evaluated later through the parent frame)
                pts.Add((0.0, double.NaN));
            }

            pts.Sort((a, b) => a.arc.CompareTo(b.arc));
            double Nearest(double arc)
            {
                var best = double.NaN; var bd = double.PositiveInfinity;
                foreach (var p in pts) if (!double.IsNaN(p.h) && Math.Abs(p.arc - arc) < bd) { bd = Math.Abs(p.arc - arc); best = p.h; }
                return double.IsNaN(best) ? L.Main.Target * 0.5 : best;
            }

            var startToe = toe[LmcGrid.Index(f.Points[0], n, cell)];
            var endToe = toe[LmcGrid.Index(f.Points[f.Points.Count - 1], n, cell)];
            var list = new List<(double arc, double h)>();
            if (pts.Count == 0 || pts[0].arc > 1e-3) list.Add((0.0, startToe + s.EndShare * (Nearest(0) - startToe)));
            foreach (var p in pts) list.Add(p);
            if (list[list.Count - 1].arc < total - 1e-3) list.Add((total, endToe + s.EndShare * (Nearest(total) - endToe)));
            // de-duplicate arcs
            var clean = new List<(double arc, double h)>();
            foreach (var p in list) if (clean.Count == 0 || p.arc - clean[clean.Count - 1].arc > 1.0) clean.Add(p);
            f.NodeArc = new double[clean.Count];
            f.NodeH = new double[clean.Count];
            for (var k = 0; k < clean.Count; k++) { f.NodeArc[k] = clean[k].arc; f.NodeH[k] = clean[k].h; }
            surface.Report.Add($"ridge {f.Ridge}: {clean.Count} crest nodes over {total:F0} m");
        }

        private static double PassHeightFromToe(LmcLayout L, double[] toe, int n) => double.NaN; // set from the base after it exists

        private static double ProjectArc(Frame f, Vector2 p)
        {
            var best = 0.0; var bd = double.PositiveInfinity;
            for (var k = 0; k < f.Points.Count; k++)
            {
                var d = (f.Points[k] - p).sqrMagnitude;
                if (d < bd) { bd = d; best = f.Arc[k]; }
            }

            return best;
        }

        private static void FinishProfile(Frame f, Func<double, double> rootHeight, double passHeight)
        {
            for (var k = 0; k < f.NodeH.Length; k++)
            {
                if (!double.IsNaN(f.NodeH[k])) continue;
                f.NodeH[k] = k == 0 && rootHeight != null ? rootHeight(f.NodeArc[k]) : passHeight;
            }

            var m = f.NodeH.Length;
            var d = new double[Math.Max(1, m - 1)];
            for (var k = 0; k + 1 < m; k++) d[k] = (f.NodeH[k + 1] - f.NodeH[k]) / (f.NodeArc[k + 1] - f.NodeArc[k]);
            f.Slopes = new double[m];
            for (var k = 0; k < m; k++)
            {
                if (k == 0) f.Slopes[k] = m > 1 ? d[0] : 0;
                else if (k == m - 1) f.Slopes[k] = d[m - 2];
                else f.Slopes[k] = d[k - 1] * d[k] <= 0 ? 0 : (d[k - 1] + d[k]) / 2;
            }

            for (var k = 0; k + 1 < m; k++)
            {
                if (d[k] == 0) { f.Slopes[k] = 0; f.Slopes[k + 1] = 0; continue; }
                var a = f.Slopes[k] / d[k];
                var b = f.Slopes[k + 1] / d[k];
                var r = a * a + b * b;
                if (r > 9)
                {
                    var t = 3 / Math.Sqrt(r);
                    f.Slopes[k] = t * a * d[k];
                    f.Slopes[k + 1] = t * b * d[k];
                }
            }
        }

        private static double Crest(Frame f, double arc)
        {
            var m = f.NodeArc.Length;
            if (m == 1) return f.NodeH[0];
            if (arc <= f.NodeArc[0]) return f.NodeH[0];
            if (arc >= f.NodeArc[m - 1]) return f.NodeH[m - 1];
            var k = Array.BinarySearch(f.NodeArc, arc);
            if (k < 0) k = ~k - 1;
            k = Math.Max(0, Math.Min(m - 2, k));
            var h = f.NodeArc[k + 1] - f.NodeArc[k];
            var t = (arc - f.NodeArc[k]) / h;
            var t2 = t * t; var t3 = t2 * t;
            return (2 * t3 - 3 * t2 + 1) * f.NodeH[k] + (t3 - 2 * t2 + t) * h * f.Slopes[k] + (-2 * t3 + 3 * t2) * f.NodeH[k + 1] + (t3 - t2) * h * f.Slopes[k + 1];
        }

        private static readonly string[] MountainRecordFields = { "f1", "f2", "d1", "d2", "w2", "h1", "h2", "arc", "side", "u", "dB", "crest", "R", "flank", "v", "w", "depth" };

        private static void Mountain(LmcLayout L, LmcSkeletonSettings s, List<Frame> frames, bool[] highland, float[] dB, double[] toe, double[] z, int n, float cell, Dictionary<string, float[]> rec = null)
        {
            var count = n * n;
            // finish profiles: the pass node from the base, branch roots from the parent's crest
            var passH = L.Pass != null ? LmcFluvial.AssociatedLowland(L, z) + L.Pass.Target : double.NaN;
            FinishProfile(frames[0], null, passH);
            for (var k = 1; k < frames.Count; k++)
            {
                var parent = frames[0];
                var root = frames[k].Points[0];
                var rootArc = ProjectArc(parent, root);
                FinishProfile(frames[k], _ => Crest(parent, rootArc), passH);
            }

            var kd = new double[frames.Count, 2];
            for (var k = 0; k < frames.Count; k++)
            for (var side = 0; side < 2; side++)
                kd[k, side] = 1.0 / Math.Pow(1 - s.ValleyOnset, frames[k].Gamma[side]);

            for (var i = 0; i < count; i++)
            {
                if (!highland[i]) continue;
                // two nearest frames
                int f1 = -1, f2 = -1;
                for (var k = 0; k < frames.Count; k++)
                {
                    var d = frames[k].Dist[i];
                    if (float.IsInfinity(d)) continue;
                    if (f1 < 0 || d < frames[f1].Dist[i]) { f2 = f1; f1 = k; }
                    else if (f2 < 0 || d < frames[f2].Dist[i]) f2 = k;
                }

                if (f1 < 0) continue;
                var terms = rec != null ? new double[10] : null;
                var h1 = FrameHeight(L, s, frames[f1], kd, f1, i, dB, toe, n, cell, terms);
                if (rec != null)
                {
                    rec["f1"][i] = f1;
                    rec["d1"][i] = frames[f1].Dist[i];
                    rec["h1"][i] = (float)h1;
                    rec["w2"][i] = 0f;
                    for (var k = 0; k < terms.Length; k++) rec[MountainRecordFields[7 + k]][i] = (float)terms[k];
                }

                if (f2 >= 0)
                {
                    var gap = (frames[f2].Dist[i] - frames[f1].Dist[i]) / s.FrameBlend;
                    var w2 = 0.5 * (1 - Smooth(gap)) * Smooth(frames[f1].Dist[i] / s.CrestKeep); // Q9 #8: the crest itself is never blended
                    if (rec != null)
                    {
                        rec["f2"][i] = f2;
                        rec["d2"][i] = frames[f2].Dist[i];
                        rec["w2"][i] = (float)w2;
                        rec["h2"][i] = (float)FrameHeight(L, s, frames[f2], kd, f2, i, dB, toe, n, cell); // a pure function: nothing changes
                    }

                    if (w2 > 1e-6) h1 = (1 - w2) * h1 + w2 * FrameHeight(L, s, frames[f2], kd, f2, i, dB, toe, n, cell);
                }

                z[i] = h1;
            }
        }

        private static double FrameHeight(LmcLayout L, LmcSkeletonSettings s, Frame f, double[,] kd, int fi, int i, float[] dB, double[] toe, int n, float cell, double[] rec = null)
        {
            var o = f.Owner[i];
            var arc = f.Arc[o];
            var rp = f.Points[o];
            var p = new Vector2((i % n) * cell, (i / n) * cell);
            var a = f.Points[Math.Max(0, o - 1)];
            var b = f.Points[Math.Min(f.Points.Count - 1, o + 1)];
            var t = b - a;
            var side = t.x * (p.y - rp.y) - t.y * (p.x - rp.x) >= 0 ? 0 : 1;
            var d = f.Dist[i];
            var u = d / Math.Max(1e-6, d + dB[i]);
            var crest = Crest(f, arc);
            var R = Math.Max(5.0, crest - toe[i]);
            var gamma = f.Gamma[side];
            var flank = Math.Pow(1 - u, gamma);
            var st = f.Stations[side];
            var k = st.BinarySearch(arc);
            if (k < 0) k = ~k;
            // stations alternate spur / valley; k is the first station beyond arc
            var s0 = k == 0 ? 0.0 : st[k - 1];
            var s1 = k < st.Count ? st[k] : s0 + (s0 - (k >= 2 ? st[k - 2] : 0.0));
            var spurFirst = ((k - 1) % 2 == 0) == f.FirstIsSpur[side];
            var frac = (arc - s0) / Math.Max(1e-6, s1 - s0);
            var v = spurFirst ? frac : 1 - frac;
            var w = v >= 1 - s.FloorFraction ? 1.0 : Smooth(v / (1 - s.FloorFraction));
            var depth = s.ValleyDepth * kd[fi, side] * Smooth(u / s.ValleyOnset) * w;
            if (rec != null)
            {
                // recording run 10: arc, side, u, dB, crest, R, flank, v, w, depth (MountainRecordFields from "arc" on)
                rec[0] = arc; rec[1] = side; rec[2] = u; rec[3] = dB[i]; rec[4] = crest;
                rec[5] = R; rec[6] = flank; rec[7] = v; rec[8] = w; rec[9] = depth;
            }

            return toe[i] + R * flank * (1 - depth);
        }

        // ------------------------------------------------------------------ plateaus

        private static Dictionary<string, object> Plateau(LmcLayout L, LmcSkeletonSettings s, LmcPlateau pl, double[] z, double[] B, bool[] highland, int n, float cell, bool[] land, LmcSurface surface, bool[] protect)
        {
            var count = n * n;
            var top = new bool[count];
            var topCells = new List<int>();
            for (var i = 0; i < count; i++) if (L.Region[i] == LmcCodes.PlateauTop && L.Instance[i] == pl.Id) { top[i] = true; topCells.Add(i); }
            var feet = new List<double>();
            for (var k = 0; k < 360; k += 2)
            {
                if (pl.Rim[k] != LmcCodes.RimEscarpment) continue;
                var r = pl.Radius[k] + pl.BandWidth + 10f;
                var a = k * Mathf.Deg2Rad;
                var p = pl.Centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                var i = LmcGrid.Index(p, n, cell);
                if (land[i]) feet.Add(B[i]);
            }

            var footRef = feet.Count > 0 ? LmcMeasure.Median(feet) : B[LmcGrid.Index(pl.Centre, n, cell)];
            var outletPts = new List<Vector2>();
            foreach (var o in pl.OutletBearings) outletPts.Add(pl.RimPoint(o));
            var distOut = new Dictionary<int, double>();
            var dlist = new List<double>();
            foreach (var i in topCells)
            {
                var p = LmcGrid.P(i, n, cell);
                var d = double.PositiveInfinity;
                foreach (var q in outletPts) d = Math.Min(d, Vector2.Distance(p, q));
                if (double.IsInfinity(d)) d = 0;
                distOut[i] = d;
                dlist.Add(d);
            }

            var dmed = dlist.Count > 0 ? LmcMeasure.Median(dlist) : 0.0;
            var zTop = footRef + pl.EdgeTarget;
            var baseZ = (double[])z.Clone();
            var measured = double.NaN;
            for (var it = 0; it <= s.EdgeCorrections; it++)
            {
                Array.Copy(baseZ, z, count);
                Apply(L, s, pl, z, B, highland, top, topCells, distOut, dmed, zTop, footRef, n, cell, land, baseZ, protect);
                measured = LmcMeasure.EdgeHeightLight(z, land, n, cell, top, topCells);
                if (double.IsNaN(measured) || Math.Abs(measured - pl.EdgeTarget) <= 2.0 || it == s.EdgeCorrections) break;
                zTop += pl.EdgeTarget - measured;
            }

            if (double.IsNaN(measured) || Math.Abs(measured - pl.EdgeTarget) > 2.0)
                surface.Defects.Add(string.Format(CultureInfo.InvariantCulture, "plateau {0} edge target {1:F1} m not held ({2:F1} m)", pl.Id, pl.EdgeTarget, measured));
            surface.Report.Add(string.Format(CultureInfo.InvariantCulture, "plateau {0}: foot reference {1:F1} m, top {2:F1} m, edge target {3:F1} m, measured {4:F1} m", pl.Id, footRef, zTop, pl.EdgeTarget, measured));
            return new Dictionary<string, object> { ["id"] = pl.Id, ["foot_ref"] = footRef, ["top"] = zTop, ["edge_target"] = pl.EdgeTarget, ["edge_measured"] = measured };
        }

        private static void Apply(LmcLayout L, LmcSkeletonSettings s, LmcPlateau pl, double[] z, double[] B, bool[] highland, bool[] top, List<int> topCells, Dictionary<int, double> distOut, double dmed, double zTop, double footRef, int n, float cell, bool[] land, double[] baseZ, bool[] protect)
        {
            var fall = Math.Tan(s.TopFallDeg * Math.PI / 180.0);
            foreach (var i in topCells) z[i] = zTop + fall * (distOut[i] - dmed);
            var tanWall = Math.Tan(s.WallDeg * Math.PI / 180.0);
            var tanTalus = Math.Tan(s.TalusDeg * Math.PI / 180.0);
            var tanRamp = Math.Tan(s.RampDeg * Math.PI / 180.0);
            var talusH = Math.Min(s.TalusShare * pl.EdgeTarget, s.TalusWidth * tanTalus);
            var gradualW = Math.Max(s.GradualWidthMin, s.GradualWidthPerHeight * pl.EdgeTarget);
            var rMax = 0f;
            for (var k = 0; k < 360; k++) rMax = Mathf.Max(rMax, pl.Radius[k]);
            var reach = rMax + Math.Max(gradualW, Math.Max((zTop - footRef) / tanRamp, (zTop - footRef) / tanTalus + 20)) + 10;
            // ramp rule (b) (review §13.3): a strip of constant width along the ramp bearing, not the widening sector
            var rampCos = Math.Cos(pl.RampBearing * Math.PI / 180.0);
            var rampSin = Math.Sin(pl.RampBearing * Math.PI / 180.0);
            var rampRim = (double)pl.RadiusAt(pl.RampBearing);
            var rampReach = (zTop - footRef) / tanRamp + 10;
            var rc = (int)Math.Ceiling(reach / cell);
            var cx = (int)Math.Round(pl.Centre.x / cell); var cz = (int)Math.Round(pl.Centre.y / cell);
            for (var dz = -rc; dz <= rc; dz++)
            for (var dx = -rc; dx <= rc; dx++)
            {
                var x = cx + dx; var zz = cz + dz;
                if (x < 0 || zz < 0 || x >= n || zz >= n) continue;
                var i = zz * n + x;
                if (!land[i] || top[i]) continue;
                var p = new Vector2(x * cell, zz * cell);
                var bearing = pl.BearingOf(p);
                var rho = Vector2.Distance(p, pl.Centre) - pl.RadiusAt(bearing);
                if (rho <= 0 || rho > reach) continue;
                var zRim = zTop;
                var type = pl.RimAt(bearing);
                if (L.Region[i] == LmcCodes.PlateauTop) continue;
                if (protect[i]) continue; // option (b): declared drainage is never raised
                var rx = p.x - (double)pl.Centre.x;
                var rz = p.y - (double)pl.Centre.y;
                var along = rx * rampCos + rz * rampSin - rampRim; // ρ along the ray beyond the rim point at the ramp bearing
                if (Math.Abs(rz * rampCos - rx * rampSin) <= 0.5 * s.RampWidth && along > 0)
                {
                    if (along <= rampReach) z[i] = Math.Max(z[i], zRim - along * tanRamp);
                    continue;
                }

                if (type == LmcCodes.RimRamp) type = LmcCodes.RimEscarpment; // ramp bearings outside the strip
                double prof;
                switch (type)
                {
                    case LmcCodes.RimEscarpment:
                        var rhoW = Math.Max(0, (zRim - (footRef + talusH)) / tanWall);
                        prof = rho <= rhoW ? zRim - rho * tanWall : zRim - rhoW * tanWall - (rho - rhoW) * tanTalus;
                        z[i] = Math.Max(z[i], prof);
                        break;
                    default:
                        var t = Smooth(rho / gradualW);
                        prof = (1 - t) * zRim + t * z[i];
                        z[i] = Math.Max(z[i], prof);
                        break;
                }
            }

            // option (b): banks — ground this plateau raised within the bank reach of protected ground is capped at that
            // ground + distance × tan(bank angle), never below its height before the plateau (Q9 #16's trough banks)
            var bankTan = Math.Tan(s.BankDeg * Math.PI / 180.0);
            var br = (int)Math.Ceiling(s.BankReach / cell);
            for (var dz = -rc; dz <= rc; dz++)
            for (var dx = -rc; dx <= rc; dx++)
            {
                var x = cx + dx; var zz = cz + dz;
                if (x < 0 || zz < 0 || x >= n || zz >= n) continue;
                var i = zz * n + x;
                if (!land[i] || top[i] || protect[i] || L.Region[i] == LmcCodes.PlateauTop || z[i] <= baseZ[i]) continue;
                var cap = double.PositiveInfinity;
                for (var bz = -br; bz <= br; bz++)
                for (var bx = -br; bx <= br; bx++)
                {
                    var xj = x + bx; var zj = zz + bz;
                    if (xj < 0 || zj < 0 || xj >= n || zj >= n) continue;
                    var j = zj * n + xj;
                    if (!protect[j] || !land[j]) continue;
                    var d = Math.Sqrt(bx * bx + bz * bz) * cell;
                    if (d > s.BankReach) continue;
                    cap = Math.Min(cap, z[j] + d * bankTan);
                }

                if (z[i] > cap) z[i] = Math.Max(baseZ[i], cap);
            }
        }

        // ------------------------------------------------------------------ coast

        private static void Coast(LmcSkeletonSettings s, LmcContour c, double[] w, double[] z, int n, bool[] land)
        {
            var tl = Math.Tan(s.CoastLowDeg * Math.PI / 180.0);
            var tr = Math.Tan(s.CoastRockyDeg * Math.PI / 180.0);
            var tc = Math.Tan(s.CoastCliffDeg * Math.PI / 180.0);
            for (var i = 0; i < n * n; i++)
            {
                if (!land[i]) continue;
                var sd = Math.Max(0.0, c.Sd[i]);
                double acc = 0;
                var bands = new[] { s.CoastBandLow, s.CoastBandRocky, s.CoastBandCliff };
                var tans = new[] { tl, tr, tc };
                for (var k = 0; k < 3; k++)
                {
                    var wk = w[3 * i + k];
                    if (wk <= 0) continue;
                    var t = Smooth(sd / bands[k]);
                    var limit = s.BaseLevel + sd * tans[k];
                    var zk = (1 - t) * Math.Min(limit, z[i]) + t * z[i];
                    acc += wk * zk;
                }

                z[i] = acc;
            }
        }

        // ------------------------------------------------------------------ declared valley axes

        private static List<LmcAxis> ValleyAxes(List<Frame> frames, bool[] highland, int n, float cell)
        {
            var axes = new List<LmcAxis>();
            var hcells = new List<int>();
            for (var i = 0; i < n * n; i++) if (highland[i]) hcells.Add(i);
            var nearest = new int[n * n];
            foreach (var i in hcells)
            {
                nearest[i] = -1;
                var best = float.PositiveInfinity;
                for (var k = 0; k < frames.Count; k++) if (frames[k].Dist[i] < best) { best = frames[k].Dist[i]; nearest[i] = k; }
            }
            for (var fi = 0; fi < frames.Count; fi++)
            {
                var f = frames[fi];
                for (var side = 0; side < 2; side++)
                {
                    var st = f.Stations[side];
                    for (var k = 0; k < st.Count; k++)
                    {
                        var isValley = (k % 2 == 0) != f.FirstIsSpur[side];
                        if (!isValley) continue;
                        var cells = new List<(double d, int i)>();
                        foreach (var i in hcells)
                        {
                            if (f.Owner[i] < 0 || nearest[i] != fi) continue; // Q9 #15: only where this frame owns the vertex
                            if (Math.Abs(f.Arc[f.Owner[i]] - st[k]) > 0.6 * cell) continue;
                            var rp = f.Points[f.Owner[i]];
                            var a = f.Points[Math.Max(0, f.Owner[i] - 1)];
                            var b = f.Points[Math.Min(f.Points.Count - 1, f.Owner[i] + 1)];
                            var t = b - a;
                            var p = LmcGrid.P(i, n, cell);
                            var sd = t.x * (p.y - rp.y) - t.y * (p.x - rp.x) >= 0 ? 0 : 1;
                            if (sd != side) continue;
                            cells.Add((f.Dist[i], i));
                        }

                        if (cells.Count < 4) continue;
                        cells.Sort((p, q) => p.d.CompareTo(q.d));
                        var ax = new LmcAxis { Name = $"ridge {f.Ridge} side {side} valley {k}" };
                        foreach (var cc in cells) ax.Points.Add(LmcGrid.P(cc.i, n, cell));
                        axes.Add(ax);
                    }
                }
            }

            return axes;
        }

        /// <summary>
        /// Running minimum along an axis; Q9 #16: where the floor is lowered, ground within the bank reach is capped at the
        /// lowered floor + distance × tan(bank angle), so the floor is a sloped trough, not a one-vertex slot.
        /// </summary>
        private static int RunningMinimum(LmcAxis ax, double[] z, int n, float cell, bool[] land, double baseLevel, double bankTan, double bankReach)
        {
            var min = double.PositiveInfinity;
            var lowered = 0;
            var r = (int)Math.Ceiling(bankReach / cell);
            foreach (var p in ax.Points)
            {
                var i = LmcGrid.Index(p, n, cell);
                if (!land[i]) continue;
                if (z[i] > min + 1e-6)
                {
                    z[i] = Math.Max(baseLevel, min);
                    lowered++;
                    var cx = i % n; var cz = i / n;
                    for (var dz = -r; dz <= r; dz++)
                    for (var dx = -r; dx <= r; dx++)
                    {
                        var x = cx + dx; var zz = cz + dz;
                        if (x < 0 || zz < 0 || x >= n || zz >= n) continue;
                        var j = zz * n + x;
                        if (!land[j]) continue;
                        var d = Math.Sqrt(dx * dx + dz * dz) * cell;
                        if (d > bankReach) continue;
                        var cap = z[i] + d * bankTan;
                        if (z[j] > cap) z[j] = cap;
                    }
                }

                min = Math.Min(min, z[i]);
            }

            return lowered;
        }
    }
}
#endif

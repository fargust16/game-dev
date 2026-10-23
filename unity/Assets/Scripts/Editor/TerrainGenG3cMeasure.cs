#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GameDev.World;
using GameDev.World.Hydrology;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>One settlement site found by the R4 rule.</summary>
    public struct G3cSite
    {
        public Vector2 Position;
        public float RadiusMeters;
        public bool Coastal;
        public int Region;
        public float MeanSlope;
    }

    /// <summary>R1–R8 and the diagnostics beside them, as defined in research record §14.1.</summary>
    public sealed class G3cReadings
    {
        public bool[] R = new bool[9];
        public int Culminations;
        public int BranchPoints;
        public int Passes;
        public bool SingleDome;
        public float CoastAligned;
        public float EtaSquared;
        public int SeparateSummits;
        public float P90Share;
        public int PlateausDeclared;
        public int PlateausVerified;
        public float PlateauMaxSeparation;
        public int BuildablePatchRegions;
        public int Sites;
        public bool Capital;
        public bool CoastalSite;
        public int SiteRegions;
        public int ElevatedSystems;
        public int ElevatedSystemsUndesignated;
        public int Systems;
        public bool HydrologyOk;
        public float LowShare;
        public float RockyShare;
        public float CliffShare;
        public float LowConsistent;
        public float CliffConsistent;
        public int OutletsOnLow;
        public int Outlets;
        public int Depressions;
        public float DepressionMaxDepth;
        public int ClimbingAxes;
        public float WorstClimb;
        public int Rivers;
        public int TargetsUnreachable;
        public int StrahlerMax;
        public int Tributaries;
        public float RiverKm;
        public float LargestCatchmentSqm;
        public float LandUnder5;
        public float LandOver45;
        public float BuildShare;
        public float[] BandShare = new float[3];
        public float[] BandBuild = new float[3];
        public float Steep45To60;
        public float Steep60To70;
        public float SteepOver70;
        public float ReachableLand;
        public float Highest;
        public List<string> PlateauLines = new List<string>();
        public List<G3cSite> SiteList = new List<G3cSite>();
        public List<Vector2> PassList = new List<Vector2>();
        public int PassesBeforeReach;
        public List<string> DefectLines = new List<string>();

        public int Met
        {
            get
            {
                var k = 0;
                for (var i = 1; i <= 8; i++)
                {
                    if (R[i])
                    {
                        k++;
                    }
                }

                return k;
            }
        }

        public string Line(CultureInfo c)
        {
            var sb = new StringBuilder();
            for (var i = 1; i <= 8; i++)
            {
                sb.Append($"R{i}={(R[i] ? "Y" : "n")} ");
            }

            sb.Append($"met={Met} ");
            sb.Append($"culm={Culminations} branch={BranchPoints} passes={Passes}/{PassesBeforeReach} dome={(SingleDome ? "FLAG" : "no")} ");
            sb.Append($"plateaus={PlateausVerified}/{PlateausDeclared} sep={PlateauMaxSeparation.ToString("F0", c)} ");
            sb.Append($"patchRegions={BuildablePatchRegions} sites={Sites} capital={(Capital ? 1 : 0)} coastalSite={(CoastalSite ? 1 : 0)} siteRegions={SiteRegions} ");
            sb.Append($"elevSys={ElevatedSystems} elevUndesig={ElevatedSystemsUndesignated} systems={Systems} hyd={(HydrologyOk ? "ok" : "FAIL")} ");
            sb.Append($"coast low/rocky/cliff={Pct(LowShare, c)}/{Pct(RockyShare, c)}/{Pct(CliffShare, c)} lowOk={Pct(LowConsistent, c)} cliffOk={Pct(CliffConsistent, c)} outletsLow={OutletsOnLow}/{Outlets} ");
            sb.Append($"S8={Depressions} deepest={DepressionMaxDepth.ToString("F2", c)} climbing={ClimbingAxes}/{Rivers} worstClimb={WorstClimb.ToString("F2", c)} unreachable={TargetsUnreachable} ");
            sb.Append($"strahler={StrahlerMax} tribs={Tributaries} riverKm={RiverKm.ToString("F2", c)} largestCatch={(LargestCatchmentSqm / 1e6f).ToString("F3", c)}km2 ");
            sb.Append($"lt5={LandUnder5.ToString("F1", c)} gt45={LandOver45.ToString("F1", c)} build={BuildShare.ToString("F1", c)} ");
            sb.Append($"band0-60={BandShare[0].ToString("F1", c)}%/{BandBuild[0].ToString("F0", c)}%b band60-200={BandShare[1].ToString("F1", c)}%/{BandBuild[1].ToString("F0", c)}%b interior={BandShare[2].ToString("F1", c)}%/{BandBuild[2].ToString("F0", c)}%b ");
            sb.Append($"steep45-60={Steep45To60.ToString("F2", c)} 60-70={Steep60To70.ToString("F2", c)} gt70={SteepOver70.ToString("F2", c)} reach={ReachableLand.ToString("F1", c)} highest={Highest.ToString("F0", c)}");
            return sb.ToString();
        }

        private static string Pct(float v, CultureInfo c) => (100f * v).ToString("F0", c);
    }

    public static class G3cMeasure
    {
        public const float BuildableDegrees = 6.84f; // atan(0.12), the road corridor maxGrade (brief §4.1)
        public const float SiteFraction = 0.90f;
        public const float SiteSpacing = 450f;
        public const float CoastalReach = 100f;
        public const float PatchMinSqm = 100_000f;

        public static G3cReadings Measure(G3cResult r, G3cContour c, out GeographyAnalysis a)
        {
            var n = r.N;
            var cell = r.Cell;
            var count = n * n;
            var h = r.Heights;
            var m = new G3cReadings();
            a = GeographyAnalysis.Build(h, n, cell, r.Seed);
            var land = a.Land;

            var slope = new float[count];
            var landCount = 0;
            var under5 = 0;
            var over45 = 0;
            var build = 0;
            m.Highest = float.NegativeInfinity;
            var bandLand = new int[3];
            var bandBuild = new int[3];
            for (var z = 0; z < n; z++)
            {
                for (var x = 0; x < n; x++)
                {
                    var i = z * n + x;
                    if (!land[i])
                    {
                        continue;
                    }

                    landCount++;
                    m.Highest = Mathf.Max(m.Highest, h[i]);
                    slope[i] = IslandReliefMetrics.SlopeDegrees(h, n, cell, x, z);
                    if (slope[i] < 5f) under5++;
                    if (slope[i] > 45f) over45++;
                    var b = c.Sd[i] <= 60f ? 0 : c.Sd[i] <= 200f ? 1 : 2;
                    bandLand[b]++;
                    if (slope[i] <= BuildableDegrees)
                    {
                        build++;
                        bandBuild[b]++;
                    }
                }
            }

            m.LandUnder5 = 100f * under5 / Mathf.Max(1, landCount);
            m.LandOver45 = 100f * over45 / Mathf.Max(1, landCount);
            m.BuildShare = 100f * build / Mathf.Max(1, landCount);
            for (var b = 0; b < 3; b++)
            {
                m.BandShare[b] = 100f * bandLand[b] / Mathf.Max(1, landCount);
                m.BandBuild[b] = 100f * bandBuild[b] / Mathf.Max(1, bandLand[b]);
            }

            var tri = new G3cTriangles(h, n, cell);
            m.ReachableLand = tri.ReachableLandShare * 100f;
            m.Steep45To60 = tri.Share45To60 * 100f;
            m.Steep60To70 = tri.Share60To70 * 100f;
            m.SteepOver70 = tri.ShareOver70 * 100f;

            // Catchments: the ocean outlet each land cell drains to on the filled D8 surface.
            var outletOf = Catchments(a, n, out var catchArea);
            foreach (var kv in catchArea)
            {
                m.LargestCatchmentSqm = Mathf.Max(m.LargestCatchmentSqm, kv.Value * cell * cell);
            }

            var regionId = new int[count];
            for (var i = 0; i < count; i++)
            {
                regionId[i] = RegionId(r, i, outletOf);
            }

            // ---------- R1 ----------
            var peaks = GeographyMetrics.MergeTree(h, land, n);
            var heights = new List<float>(landCount);
            for (var i = 0; i < count; i++)
            {
                if (land[i])
                {
                    heights.Add(h[i]);
                }
            }

            heights.Sort();
            var p90 = heights.Count > 0 ? heights[Mathf.Clamp((int)(0.9f * heights.Count), 0, heights.Count - 1)] : 0f;
            foreach (var peak in peaks)
            {
                if (peak.Prominence < GeographyMetrics.SummitProminenceMeters)
                {
                    continue;
                }

                if (r.HighlandMask[peak.Index])
                {
                    m.Culminations++;
                }

                if (peak.Col < 0 || peak.ColHeight < p90)
                {
                    m.SeparateSummits++;
                }

                if (r.HighlandMask[peak.Index])
                {
                    var colInfo = peak.Col >= 0
                        ? string.Format(CultureInfo.InvariantCulture, "col ({0:F0}, {1:F0}) {2:F0} m, col in highland {3}", (peak.Col % n) * cell, (peak.Col / n) * cell, peak.ColHeight, r.HighlandMask[peak.Col])
                        : "col: the sea";
                    m.DefectLines.Add(string.Format(CultureInfo.InvariantCulture, "highland summit ({0:F0}, {1:F0}) {2:F0} m, prominence {3:F0} m, {4}",
                        (peak.Index % n) * cell, (peak.Index / n) * cell, peak.Height, peak.Prominence, colInfo));
                }

                if (peak.Col >= 0 && r.HighlandMask[peak.Col])
                {
                    var distinct = new HashSet<int>();
                    var cx = peak.Col % n;
                    var cz = peak.Col / n;
                    for (var dz = -1; dz <= 1; dz++)
                    {
                        for (var dx = -1; dx <= 1; dx++)
                        {
                            var x = cx + dx;
                            var z = cz + dz;
                            if (x < 0 || z < 0 || x >= n || z >= n)
                            {
                                continue;
                            }

                            var o = outletOf[z * n + x];
                            if (o >= 0 && catchArea.TryGetValue(o, out var area) && area * cell * cell >= 100_000f)
                            {
                                distinct.Add(o);
                            }
                        }
                    }

                    m.DefectLines.Add($"  key col drains to {distinct.Count} outlet catchments >= 0.1 km2");
                    if (distinct.Count >= 2)
                    {
                        m.PassesBeforeReach++;
                        if (tri.VertexReachable(cx, cz))
                        {
                            m.Passes++;
                            m.PassList.Add(G3cGrid.P(peak.Col, n, cell));
                        }
                    }
                }
            }

            for (var i = 0; i < count; i++)
            {
                if (a.BranchMask[i] && r.HighlandMask[i])
                {
                    m.BranchPoints++;
                }
            }

            SingleDome(r, c, a, slope, p90, m);
            m.R[1] = m.Culminations >= 2 && m.BranchPoints >= 1 && m.Passes >= 1 && !m.SingleDome;

            // ---------- R2 ----------
            var highOrUp = new bool[count];
            for (var i = 0; i < count; i++)
            {
                highOrUp[i] = r.Region[i] == G3cResult.Highland || r.Region[i] == G3cResult.Upland;
            }

            var dHighUp = G3cGrid.Edt(highOrUp, n, cell);
            var verified = new List<Vector2>();
            var topTargets = new List<int>();
            m.PlateausDeclared = r.Plateaus.Count;
            foreach (var pl in r.Plateaus)
            {
                int top = 0, flat = 0, reachable = 0;
                for (var i = 0; i < count; i++)
                {
                    if (r.PlateauId[i] != pl.Index || r.Region[i] != G3cResult.PlateauTop)
                    {
                        continue;
                    }

                    top++;
                    if (slope[i] <= 5f) flat++;
                    if (tri.VertexReachable(i % n, i / n)) reachable++;
                }

                var perimeter = 0f;
                for (var k = 0; k < 360; k++)
                {
                    var t0 = k * Mathf.Deg2Rad;
                    var t1 = (k + 1) * Mathf.Deg2Rad;
                    perimeter += Vector2.Distance(Rim(pl, t0), Rim(pl, t1));
                }

                var samples = Mathf.Max(12, Mathf.CeilToInt(perimeter / 10f));
                var escarp = 0;
                var adjoin = false;
                var maxDrop = 0f;
                for (var k = 0; k < samples; k++)
                {
                    var theta = 2f * Mathf.PI * k / samples;
                    var start = Rim(pl, theta);
                    var dir = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
                    var si = G3cGrid.Index(start, n, cell);
                    if (dHighUp[si] <= 150f)
                    {
                        adjoin = true;
                    }

                    var drop = SteepRunDrop(h, tri, start, dir, cell, n, 2f * pl.Height + 40f, 20f);
                    maxDrop = Mathf.Max(maxDrop, drop);
                    if (drop >= 10f)
                    {
                        escarp++;
                    }
                }

                var flatShare = flat / (float)Mathf.Max(1, top);
                var escShare = escarp / (float)samples;
                var ok = top > 0 && flatShare >= 0.6f && escShare >= 0.2f && adjoin && reachable > 0;
                if (ok)
                {
                    verified.Add(pl.Centre);
                    for (var i = 0; i < count; i++)
                    {
                        if (r.PlateauId[i] == pl.Index && r.Region[i] == G3cResult.PlateauTop && tri.VertexReachable(i % n, i / n))
                        {
                            topTargets.Add(i);
                            break;
                        }
                    }
                }

                var topArea = top * cell * cell;
                var diameter = 2f * Mathf.Sqrt(topArea / Mathf.PI);
                var tier = pl.Height >= 18f && pl.Height <= 40f
                    ? (diameter >= 150f && diameter <= 450f ? "T2-conform" : "T2-height, footprint outside 150-450 m")
                    : (pl.Height < 18f ? "below T2" : "between T2 and T1 (2.5x rule)");
                m.PlateauLines.Add(string.Format(CultureInfo.InvariantCulture,
                    "plateau {0}: class {1} height {2:F0} m, top {3:F3} km2 (equivalent diameter {4:F0} m), top <=5deg {5:F0}%, escarpment rim {6:F0}%, max steep-run drop {7:F0} m, reachable top {8}, verified {9}, tier {10}",
                    pl.Index, pl.Tall ? "tall" : "T2", pl.Height, topArea / 1e6f, diameter, 100f * flatShare, 100f * escShare, maxDrop, reachable > 0, ok, tier));
            }

            m.PlateausVerified = verified.Count;
            for (var i = 0; i < verified.Count; i++)
            {
                for (var j = i + 1; j < verified.Count; j++)
                {
                    m.PlateauMaxSeparation = Mathf.Max(m.PlateauMaxSeparation, Vector2.Distance(verified[i], verified[j]));
                }
            }

            m.R[2] = m.PlateausVerified >= 2 && m.PlateauMaxSeparation >= 500f;

            // ---------- R3 ----------
            var buildable = new bool[count];
            for (var i = 0; i < count; i++)
            {
                buildable[i] = land[i] && slope[i] <= BuildableDegrees;
            }

            var patchRegions = new HashSet<int>();
            foreach (var patch in Components4(buildable, n))
            {
                if (patch.Count * cell * cell < PatchMinSqm)
                {
                    continue;
                }

                var votes = new Dictionary<int, int>();
                foreach (var i in patch)
                {
                    var id = regionId[i];
                    votes[id] = votes.TryGetValue(id, out var v) ? v + 1 : 1;
                }

                var best = 0;
                var bestId = -1;
                foreach (var kv in votes)
                {
                    if (kv.Value > best || (kv.Value == best && kv.Key < bestId))
                    {
                        best = kv.Value;
                        bestId = kv.Key;
                    }
                }

                if (bestId < 200 || bestId >= 1000)
                {
                    patchRegions.Add(bestId); // lowland basins and upland catchments only
                }
            }

            m.BuildablePatchRegions = patchRegions.Count;
            m.R[3] = m.BuildablePatchRegions >= 2;

            // ---------- R4 ----------
            Sites(r, c, slope, land, regionId, m);
            m.R[4] = m.Capital && m.Sites >= 3 && m.CoastalSite && m.SiteRegions >= 3;

            // ---------- R5 ----------
            Rivers(r, a, m, cell);
            // As written in research record §14.1: vacuously true when no system rises in the highland or on a
            // plateau; the count is reported beside it so a vacuous pass stays visible.
            m.R[5] = m.HydrologyOk && m.ElevatedSystemsUndesignated == 0;

            // ---------- R6 ----------
            Coast(r, c, a, m);
            m.R[6] = m.LowShare >= 0.05f && m.RockyShare >= 0.05f && m.CliffShare >= 0.05f
                     && m.LowConsistent >= 0.8f && m.CliffConsistent >= 0.8f && m.OutletsOnLow == m.Outlets;

            // ---------- R7 ----------
            Depressions(a, h, n, cell, out m.Depressions, out m.DepressionMaxDepth, r, m.DefectLines);
            Climbing(r, h, n, cell, out m.ClimbingAxes, out m.WorstClimb, m.DefectLines);
            m.Rivers = r.Rivers.Count;
            m.R[7] = m.Depressions == 0 && m.ClimbingAxes == 0;

            // ---------- R8 ----------
            var unreachable = 0;
            foreach (var site in m.SiteList)
            {
                var i = G3cGrid.Index(site.Position, n, cell);
                if (!tri.VertexReachable(i % n, i / n)) unreachable++;
            }

            foreach (var i in topTargets)
            {
                if (!tri.VertexReachable(i % n, i / n)) unreachable++;
            }

            foreach (var p in m.PassList)
            {
                var i = G3cGrid.Index(p, n, cell);
                if (!tri.VertexReachable(i % n, i / n)) unreachable++;
            }

            m.TargetsUnreachable = unreachable;
            m.R[8] = unreachable == 0 && (m.SiteList.Count + topTargets.Count + m.PassList.Count) > 0;
            return m;
        }

        public static Vector2 Rim(G3cPlateau pl, float theta) =>
            pl.Centre + new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * pl.RadiusAt(theta);

        /// <summary>Drop of the first continuous run of served slope >= 45° starting within <paramref name="startWithin"/> of the rim.</summary>
        private static float SteepRunDrop(float[] h, G3cTriangles tri, Vector2 start, Vector2 dir, float cell, int n, float maxDist, float startWithin)
        {
            var inRun = false;
            var runStartH = 0f;
            var drop = 0f;
            var step = 0.5f * cell;
            for (var d = 0f; d <= maxDist; d += step)
            {
                var p = start + dir * d;
                if (p.x < 0f || p.y < 0f || p.x >= (n - 1) * cell || p.y >= (n - 1) * cell)
                {
                    break;
                }

                var steep = tri.SlopeAt(p) >= 45f;
                var hp = Bilinear(h, n, cell, p);
                if (steep && !inRun)
                {
                    if (d > startWithin)
                    {
                        break;
                    }

                    inRun = true;
                    runStartH = hp;
                }
                else if (!steep && inRun)
                {
                    drop = runStartH - hp;
                    return drop;
                }

                if (inRun)
                {
                    drop = runStartH - hp;
                }
            }

            return inRun ? drop : 0f;
        }

        public static float Bilinear(float[] h, int n, float cell, Vector2 p)
        {
            var fx = Mathf.Clamp(p.x / cell, 0f, n - 1.001f);
            var fz = Mathf.Clamp(p.y / cell, 0f, n - 1.001f);
            var x = (int)fx;
            var z = (int)fz;
            var tx = fx - x;
            var tz = fz - z;
            var a = Mathf.Lerp(h[z * n + x], h[z * n + x + 1], tx);
            var b = Mathf.Lerp(h[(z + 1) * n + x], h[(z + 1) * n + x + 1], tx);
            return Mathf.Lerp(a, b, tz);
        }

        private static int[] Catchments(GeographyAnalysis a, int n, out Dictionary<int, int> area)
        {
            var count = n * n;
            var label = new int[count];
            for (var i = 0; i < count; i++)
            {
                label[i] = a.Land[i] ? -2 : -1;
            }

            var stack = new List<int>();
            for (var i = 0; i < count; i++)
            {
                if (label[i] != -2)
                {
                    continue;
                }

                stack.Clear();
                var cur = i;
                var lab = -1;
                while (true)
                {
                    if (label[cur] != -2)
                    {
                        lab = label[cur];
                        break;
                    }

                    stack.Add(cur);
                    var d = a.Directions[cur];
                    if (d == FlowRouting.NoDirection)
                    {
                        lab = cur;
                        break;
                    }

                    var x = cur % n + HydrologyGrid.NeighborDx[d];
                    var z = cur / n + HydrologyGrid.NeighborDz[d];
                    if (x < 0 || z < 0 || x >= n || z >= n)
                    {
                        lab = cur;
                        break;
                    }

                    var next = z * n + x;
                    if (!a.Land[next])
                    {
                        lab = cur;
                        break;
                    }

                    cur = next;
                    if (stack.Count > count)
                    {
                        lab = cur;
                        break;
                    }
                }

                foreach (var j in stack)
                {
                    label[j] = lab;
                }
            }

            area = new Dictionary<int, int>();
            for (var i = 0; i < count; i++)
            {
                if (label[i] >= 0)
                {
                    area[label[i]] = area.TryGetValue(label[i], out var v) ? v + 1 : 1;
                }
            }

            return label;
        }

        private static int RegionId(G3cResult r, int i, int[] outletOf)
        {
            switch (r.Region[i])
            {
                case G3cResult.Lowland:
                    return 10 + r.Basin[i];
                case G3cResult.Highland:
                    return 300;
                case G3cResult.PlateauTop:
                case G3cResult.PlateauWall:
                case G3cResult.PlateauRamp:
                    return 200 + r.PlateauId[i];
                case G3cResult.Upland:
                    return 1000 + Mathf.Max(0, outletOf[i]);
                default:
                    return -1;
            }
        }

        private static void SingleDome(G3cResult r, G3cContour c, GeographyAnalysis a, float[] slope, float p90, G3cReadings m)
        {
            var n = r.N;
            var cell = r.Cell;
            var h = r.Heights;
            var sd = c.Sd;
            int aligned = 0, total = 0;
            var bins = new Dictionary<int, double[]>();
            double sum = 0, sumSq = 0;
            long cnt = 0;
            for (var z = 0; z < n; z++)
            {
                for (var x = 0; x < n; x++)
                {
                    var i = z * n + x;
                    var floor = a.Land[i] && (r.Region[i] == G3cResult.Lowland || r.Region[i] == G3cResult.Upland);
                    if (!floor)
                    {
                        continue;
                    }

                    var key = Mathf.FloorToInt(sd[i] / GeographyMetrics.CoastBinMeters);
                    if (!bins.TryGetValue(key, out var acc))
                    {
                        acc = new double[3];
                        bins[key] = acc;
                    }

                    acc[0]++;
                    acc[1] += h[i];
                    acc[2] += (double)h[i] * h[i];
                    sum += h[i];
                    sumSq += (double)h[i] * h[i];
                    cnt++;

                    if (sd[i] <= 250f)
                    {
                        continue;
                    }

                    IslandReliefMetrics.Gradient(h, n, cell, x, z, out var hx, out var hz);
                    IslandReliefMetrics.Gradient(sd, n, cell, x, z, out var dx, out var dz);
                    var hm = Mathf.Sqrt(hx * hx + hz * hz);
                    var dm = Mathf.Sqrt(dx * dx + dz * dz);
                    if (hm <= 0.01f || dm <= 0.5f)
                    {
                        continue;
                    }

                    total++;
                    if ((hx * dx + hz * dz) / (hm * dm) > 0.95f)
                    {
                        aligned++;
                    }
                }
            }

            m.CoastAligned = total > 0 ? 100f * aligned / total : 0f;
            var keys = new List<int>(bins.Keys);
            keys.Sort();
            double within = 0;
            foreach (var key in keys)
            {
                var acc = bins[key];
                within += acc[2] - acc[1] * acc[1] / acc[0];
            }

            var totalVar = cnt > 0 ? sumSq - sum * sum / cnt : 0.0;
            m.EtaSquared = totalVar > 1e-9 ? (float)(1.0 - within / totalVar) : 1f;

            var above = new bool[h.Length];
            var aboveCount = 0;
            for (var i = 0; i < h.Length; i++)
            {
                above[i] = a.Land[i] && h[i] >= p90;
                if (above[i]) aboveCount++;
            }

            var label = GeographyGrid.Components8(above, n, out var components);
            var sizes = GeographyGrid.ComponentSizes(label, components);
            var largest = 0;
            foreach (var size in sizes)
            {
                largest = Mathf.Max(largest, size);
            }

            m.P90Share = aboveCount > 0 ? largest / (float)aboveCount : 0f;
            m.SingleDome = m.CoastAligned > GeographyMetrics.DomeCoastAlignedPercent
                           && m.EtaSquared >= GeographyMetrics.DomeEtaSquared
                           && m.SeparateSummits <= GeographyMetrics.DomeMaxSummits
                           && m.P90Share >= GeographyMetrics.DomeP90Share;
        }

        private static List<List<int>> Components4(bool[] mask, int n)
        {
            var seen = new bool[mask.Length];
            var result = new List<List<int>>();
            var stack = new Stack<int>();
            for (var s = 0; s < mask.Length; s++)
            {
                if (!mask[s] || seen[s])
                {
                    continue;
                }

                var comp = new List<int>();
                seen[s] = true;
                stack.Push(s);
                while (stack.Count > 0)
                {
                    var i = stack.Pop();
                    comp.Add(i);
                    var x = i % n;
                    var z = i / n;
                    if (x > 0) Visit(i - 1);
                    if (x < n - 1) Visit(i + 1);
                    if (z > 0) Visit(i - n);
                    if (z < n - 1) Visit(i + n);
                }

                result.Add(comp);
            }

            return result;

            void Visit(int j)
            {
                if (mask[j] && !seen[j])
                {
                    seen[j] = true;
                    stack.Push(j);
                }
            }
        }

        // ---------- R4 ----------

        private struct Candidate
        {
            public int Index;
            public float Mean;
        }

        private static void Sites(G3cResult r, G3cContour c, float[] slope, bool[] land, int[] regionId, G3cReadings m)
        {
            var n = r.N;
            var cell = r.Cell;
            var w = n + 1;
            var psSea = new int[n * w];
            var psUnb = new int[n * w];
            var psSlope = new double[n * w];
            for (var z = 0; z < n; z++)
            {
                for (var x = 0; x < n; x++)
                {
                    var i = z * n + x;
                    var o = z * w + x;
                    psSea[o + 1] = psSea[o] + (land[i] ? 0 : 1);
                    psUnb[o + 1] = psUnb[o] + (land[i] && slope[i] <= BuildableDegrees ? 0 : 1);
                    psSlope[o + 1] = psSlope[o] + (land[i] ? slope[i] : 90f);
                }
            }

            List<Candidate> Valid(float radius)
            {
                var list = new List<Candidate>();
                var rc = Mathf.FloorToInt(radius / cell);
                var half = new int[2 * rc + 1];
                var area = 0;
                for (var dz = -rc; dz <= rc; dz++)
                {
                    half[dz + rc] = Mathf.FloorToInt(Mathf.Sqrt(rc * rc - dz * dz));
                    area += 2 * half[dz + rc] + 1;
                }

                for (var z = rc; z < n - rc; z += 2)
                {
                    for (var x = rc; x < n - rc; x += 2)
                    {
                        var i = z * n + x;
                        if (!land[i] || c.Sd[i] < radius)
                        {
                            continue;
                        }

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

                        if (sea == 0 && unb <= (1f - SiteFraction) * area)
                        {
                            list.Add(new Candidate { Index = i, Mean = (float)(sl / area) });
                        }
                    }
                }

                list.Sort((p, q) => p.Mean != q.Mean ? p.Mean.CompareTo(q.Mean) : p.Index.CompareTo(q.Index));
                return list;
            }

            var capital = Valid(150f);
            var town = Valid(90f);
            var village = Valid(60f);
            var chosen = new List<G3cSite>();

            bool Far(Vector2 p)
            {
                foreach (var s in chosen)
                {
                    if (Vector2.Distance(s.Position, p) < SiteSpacing)
                    {
                        return false;
                    }
                }

                return true;
            }

            void Take(Candidate cand, float radius)
            {
                var p = G3cGrid.P(cand.Index, n, cell);
                chosen.Add(new G3cSite
                {
                    Position = p,
                    RadiusMeters = radius,
                    Coastal = c.Sd[cand.Index] <= radius + CoastalReach,
                    Region = regionId[cand.Index],
                    MeanSlope = cand.Mean,
                });
            }

            if (capital.Count > 0)
            {
                Take(capital[0], 150f);
                m.Capital = true;
            }

            foreach (var cand in village)
            {
                if (c.Sd[cand.Index] <= 60f + CoastalReach && Far(G3cGrid.P(cand.Index, n, cell)))
                {
                    Take(cand, 60f);
                    break;
                }
            }

            foreach (var cand in town)
            {
                if (chosen.Count >= 5) break;
                if (Far(G3cGrid.P(cand.Index, n, cell))) Take(cand, 90f);
            }

            foreach (var cand in village)
            {
                if (chosen.Count >= 5) break;
                if (Far(G3cGrid.P(cand.Index, n, cell))) Take(cand, 60f);
            }

            m.SiteList = chosen;
            m.Sites = chosen.Count;
            var regions = new HashSet<int>();
            foreach (var s in chosen)
            {
                regions.Add(s.Region);
                m.CoastalSite |= s.Coastal;
            }

            m.SiteRegions = regions.Count;
        }

        // ---------- R5 ----------

        private static void Rivers(G3cResult r, GeographyAnalysis a, G3cReadings m, float cell)
        {
            var hyd = a.Hydrology;
            m.HydrologyOk = hyd.Succeeded;
            if (!hyd.Succeeded)
            {
                m.DefectLines.Add($"hydrology planner failed: {hyd.Failure} {hyd.Message}");
                return;
            }

            var def = hyd.Definition;
            var byId = new Dictionary<int, RiverReach>();
            foreach (var reach in def.Reaches)
            {
                byId[reach.Id.Value] = reach;
                m.StrahlerMax = Mathf.Max(m.StrahlerMax, reach.StrahlerOrder);
            }

            m.Tributaries = def.Stats.TributaryCount;
            m.RiverKm = def.Stats.TotalRiverLengthMeters / 1000f;
            var lakes = new Dictionary<int, LakeBasin>();
            foreach (var lake in def.Lakes)
            {
                lakes[lake.Id.Value] = lake;
            }

            var n = r.N;
            var systemOf = new Dictionary<int, int>();
            var elevated = new Dictionary<int, bool>();
            var mouth = new Dictionary<int, Vector2>();
            foreach (var reach in def.Reaches)
            {
                var cur = reach;
                var guard = 0;
                Vector2 end = cur.Nodes.Count > 0 ? cur.Nodes[cur.Nodes.Count - 1].PositionXZ : Vector2.zero;
                var terminal = -1;
                while (guard++ < 10000)
                {
                    if (cur.Termination == ReachTermination.Ocean)
                    {
                        terminal = cur.Id.Value;
                        end = cur.Nodes.Count > 0 ? cur.Nodes[cur.Nodes.Count - 1].PositionXZ : end;
                        break;
                    }

                    if (cur.DownstreamId.IsValid && byId.TryGetValue(cur.DownstreamId.Value, out var next))
                    {
                        cur = next;
                        continue;
                    }

                    if (cur.DownstreamLake.IsValid && lakes.TryGetValue(cur.DownstreamLake.Value, out var lake))
                    {
                        if (lake.OutletReach.IsValid && byId.TryGetValue(lake.OutletReach.Value, out var outlet))
                        {
                            cur = outlet;
                            continue;
                        }

                        terminal = -1000 - lake.Id.Value;
                        end = lake.SpillPointXZ;
                        break;
                    }

                    terminal = -2000 - cur.Id.Value;
                    break;
                }

                systemOf[reach.Id.Value] = terminal;
                mouth[terminal] = end;
                if (!elevated.ContainsKey(terminal))
                {
                    elevated[terminal] = false;
                }

                if (reach.IsSource && reach.Nodes.Count > 0)
                {
                    var src = G3cGrid.Index(reach.Nodes[0].PositionXZ, n, cell);
                    var region = r.Region[src];
                    if (region == G3cResult.Highland || region == G3cResult.PlateauTop || region == G3cResult.PlateauWall || region == G3cResult.PlateauRamp)
                    {
                        elevated[terminal] = true;
                    }
                }
            }

            m.Systems = elevated.Count;
            foreach (var kv in elevated)
            {
                if (!kv.Value)
                {
                    continue;
                }

                m.ElevatedSystems++;
                var p = mouth[kv.Key];
                var designated = false;
                foreach (var o in r.Outlets)
                {
                    designated |= Vector2.Distance(o, p) <= 150f;
                }

                if (!designated)
                {
                    m.ElevatedSystemsUndesignated++;
                }
            }
        }

        // ---------- R6 ----------

        private static void Coast(G3cResult r, G3cContour c, GeographyAnalysis a, G3cReadings m)
        {
            var n = r.N;
            var cell = r.Cell;
            var h = r.Heights;
            var land = a.Land;
            var ocean = a.Ocean;
            var riverMask = new bool[n * n];
            foreach (var p in r.NodePos)
            {
                riverMask[G3cGrid.Index(p, n, cell)] = true;
            }

            var dRiver = G3cGrid.Edt(riverMask, n, cell);
            var r60 = Mathf.CeilToInt(60f / cell);
            var r300 = Mathf.CeilToInt(300f / cell);
            int low = 0, rocky = 0, cliff = 0, lowOk = 0, cliffOk = 0;
            var shoreCells = new List<int>();
            var classOf = new Dictionary<int, int>();
            for (var z = 1; z < n - 1; z++)
            {
                for (var x = 1; x < n - 1; x++)
                {
                    var i = z * n + x;
                    if (!land[i] || !(ocean[i - 1] || ocean[i + 1] || ocean[i - n] || ocean[i + n]))
                    {
                        continue;
                    }

                    shoreCells.Add(i);
                    var max = h[i];
                    for (var dz = -r60; dz <= r60; dz++)
                    {
                        for (var dx = -r60; dx <= r60; dx++)
                        {
                            if (dx * dx + dz * dz > r60 * r60) continue;
                            var xx = x + dx;
                            var zz = z + dz;
                            if (xx < 0 || zz < 0 || xx >= n || zz >= n) continue;
                            var j = zz * n + xx;
                            if (land[j]) max = Mathf.Max(max, h[j]);
                        }
                    }

                    var rise = max - WorldScale.SeaLevelMeters;
                    var cls = rise < 4f ? 0 : rise < GeographyMetrics.CliffRiseMeters ? 1 : 2;
                    classOf[i] = cls;

                    int lowland = 0, high = 0, totalLand = 0;
                    for (var dz = -r300; dz <= r300; dz += 3)
                    {
                        for (var dx = -r300; dx <= r300; dx += 3)
                        {
                            if (dx * dx + dz * dz > r300 * r300) continue;
                            var xx = x + dx;
                            var zz = z + dz;
                            if (xx < 0 || zz < 0 || xx >= n || zz >= n) continue;
                            var j = zz * n + xx;
                            if (!land[j]) continue;
                            totalLand++;
                            var reg = r.Region[j];
                            if (reg == G3cResult.Lowland) lowland++;
                            else high++;
                        }
                    }

                    if (cls == 0)
                    {
                        low++;
                        if (lowland * 2 > totalLand || dRiver[i] <= 150f) lowOk++;
                    }
                    else if (cls == 1)
                    {
                        rocky++;
                    }
                    else
                    {
                        cliff++;
                        if (high * 2 > totalLand) cliffOk++;
                    }
                }
            }

            var shore = Mathf.Max(1, shoreCells.Count);
            m.LowShare = low / (float)shore;
            m.RockyShare = rocky / (float)shore;
            m.CliffShare = cliff / (float)shore;
            m.LowConsistent = low > 0 ? lowOk / (float)low : 0f;
            m.CliffConsistent = cliff > 0 ? cliffOk / (float)cliff : 0f;
            m.Outlets = r.Outlets.Count;
            foreach (var o in r.Outlets)
            {
                var best = -1;
                var bestD = float.PositiveInfinity;
                foreach (var i in shoreCells)
                {
                    var d = (G3cGrid.P(i, n, cell) - o).sqrMagnitude;
                    if (d < bestD)
                    {
                        bestD = d;
                        best = i;
                    }
                }

                if (best >= 0 && classOf[best] == 0)
                {
                    m.OutletsOnLow++;
                }
            }
        }

        // ---------- R7 ----------

        private static void Depressions(GeographyAnalysis a, float[] h, int n, float cell, out int countOut, out float maxDepth, G3cResult r, List<string> lines)
        {
            var depth = new float[h.Length];
            for (var i = 0; i < h.Length; i++)
            {
                depth[i] = a.Land[i] ? a.Filled[i] - h[i] : 0f;
            }

            var seen = new bool[h.Length];
            var stack = new Stack<int>();
            countOut = 0;
            maxDepth = 0f;
            for (var s = 0; s < h.Length; s++)
            {
                if (seen[s] || depth[s] <= 0.05f)
                {
                    continue;
                }

                seen[s] = true;
                stack.Push(s);
                var cells = 0;
                var deepest = 0f;
                var deepestAt = s;
                while (stack.Count > 0)
                {
                    var i = stack.Pop();
                    cells++;
                    if (depth[i] > deepest)
                    {
                        deepest = depth[i];
                        deepestAt = i;
                    }
                    var x = i % n;
                    var z = i / n;
                    if (x > 0) Visit(i - 1);
                    if (x < n - 1) Visit(i + 1);
                    if (z > 0) Visit(i - n);
                    if (z < n - 1) Visit(i + n);
                }

                if (deepest >= IslandReliefMetrics.DepressionMinDepthMeters && cells * cell * cell >= IslandReliefMetrics.DepressionMinAreaSqm)
                {
                    countOut++;
                    maxDepth = Mathf.Max(maxDepth, deepest);
                    lines.Add(string.Format(CultureInfo.InvariantCulture, "S8 depression at ({0:F0}, {1:F0}): depth {2:F2} m, {3:F0} m2, region {4}, plateau {5}",
                        (deepestAt % n) * cell, (deepestAt / n) * cell, deepest, cells * cell * cell, r.Region[deepestAt], r.PlateauId[deepestAt]));
                }
            }

            void Visit(int j)
            {
                if (!seen[j] && depth[j] > 0.05f)
                {
                    seen[j] = true;
                    stack.Push(j);
                }
            }
        }

        private static void Climbing(G3cResult r, float[] h, int n, float cell, out int climbing, out float worst, List<string> lines)
        {
            climbing = 0;
            worst = 0f;
            foreach (var river in r.Rivers)
            {
                var min = float.PositiveInfinity;
                var climb = 0f;
                var at = -1;
                for (var k = river.Nodes.Count - 1; k >= 0; k--)
                {
                    // The lattice vertex nearest the node: the cells the construction writes the river into.
                    var v = h[G3cGrid.Index(r.NodePos[river.Nodes[k]], n, cell)];
                    if (!float.IsInfinity(min) && v - min > climb)
                    {
                        climb = v - min;
                        at = river.Nodes[k];
                    }

                    min = Mathf.Min(min, v);
                }

                worst = Mathf.Max(worst, climb);
                if (climb > 0.25f)
                {
                    climbing++;
                    var p = r.NodePos[at];
                    var ci = G3cGrid.Index(p, n, cell);
                    lines.Add(string.Format(CultureInfo.InvariantCulture, "climbing river {0} at ({1:F0}, {2:F0}): {3:F2} m, designed z {4:F1}, lattice {5:F1}, region {6}, plateau {7}",
                        river.Id, p.x, p.y, climb, r.NodeZ[at], h[ci], r.Region[ci], r.PlateauId[ci]));
                }
            }
        }
    }

    /// <summary>The Triangle Reading of a lattice (each quad split south-west to north-east) and T-edge reachability.</summary>
    public sealed class G3cTriangles
    {
        private readonly int m_n;
        private readonly float m_cell;
        private readonly float[] m_slope;
        private readonly bool[] m_reach;

        public float ReachableLandShare { get; }
        public float Share45To60 { get; }
        public float Share60To70 { get; }
        public float ShareOver70 { get; }

        public G3cTriangles(float[] h, int n, float cell)
        {
            m_n = n;
            m_cell = cell;
            var q = n - 1;
            var count = 2 * q * q;
            m_slope = new float[count];
            var landTri = new bool[count];
            var walk = new bool[count];
            m_reach = new bool[count];
            var sea = WorldScale.SeaLevelMeters;
            int landCount = 0, s1 = 0, s2 = 0, s3 = 0;
            for (var z = 0; z < q; z++)
            {
                for (var x = 0; x < q; x++)
                {
                    var h00 = h[z * n + x];
                    var h10 = h[z * n + x + 1];
                    var h01 = h[(z + 1) * n + x];
                    var h11 = h[(z + 1) * n + x + 1];
                    var t = 2 * (z * q + x);
                    // T1: (0,0),(1,0),(1,1)
                    var gx = (h10 - h00) / cell;
                    var gz = (h11 - h10) / cell;
                    Set(t, gx, gz, (h00 + h10 + h11) / 3f);
                    // T2: (0,0),(1,1),(0,1)
                    gx = (h11 - h01) / cell;
                    gz = (h01 - h00) / cell;
                    Set(t + 1, gx, gz, (h00 + h11 + h01) / 3f);
                }
            }

            ReachableLandShare = 0f;
            Share45To60 = s1 / (float)Mathf.Max(1, landCount);
            Share60To70 = s2 / (float)Mathf.Max(1, landCount);
            ShareOver70 = s3 / (float)Mathf.Max(1, landCount);

            var stack = new Stack<int>();
            for (var t = 0; t < count; t++)
            {
                if (!walk[t])
                {
                    continue;
                }

                foreach (var u in Neighbours(t, q))
                {
                    if (u >= 0 && !landTri[u])
                    {
                        m_reach[t] = true;
                        stack.Push(t);
                        break;
                    }
                }
            }

            while (stack.Count > 0)
            {
                var t = stack.Pop();
                foreach (var u in Neighbours(t, q))
                {
                    if (u >= 0 && walk[u] && !m_reach[u])
                    {
                        m_reach[u] = true;
                        stack.Push(u);
                    }
                }
            }

            var reached = 0;
            for (var t = 0; t < count; t++)
            {
                if (landTri[t] && m_reach[t]) reached++;
            }

            ReachableLandShare = reached / (float)Mathf.Max(1, landCount);

            void Set(int t, float gx, float gz, float centroid)
            {
                var g2 = gx * gx + gz * gz;
                var deg = Mathf.Atan(Mathf.Sqrt(g2)) * Mathf.Rad2Deg;
                m_slope[t] = deg;
                landTri[t] = centroid > sea;
                walk[t] = landTri[t] && g2 < 1f;
                if (!landTri[t]) return;
                landCount++;
                if (deg >= 45f && deg < 60f) s1++;
                else if (deg >= 60f && deg < 70f) s2++;
                else if (deg >= 70f) s3++;
            }
        }

        private static IEnumerable<int> Neighbours(int t, int q)
        {
            var quad = t / 2;
            var x = quad % q;
            var z = quad / q;
            if ((t & 1) == 0)
            {
                yield return t + 1;
                yield return z > 0 ? 2 * ((z - 1) * q + x) + 1 : -1;
                yield return x < q - 1 ? 2 * (z * q + x + 1) + 1 : -1;
            }
            else
            {
                yield return t - 1;
                yield return x > 0 ? 2 * (z * q + x - 1) : -1;
                yield return z < q - 1 ? 2 * ((z + 1) * q + x) : -1;
            }
        }

        /// <summary>Slope of the triangle under a world point.</summary>
        public float SlopeAt(Vector2 p)
        {
            var q = m_n - 1;
            var fx = p.x / m_cell;
            var fz = p.y / m_cell;
            var x = Mathf.Clamp((int)fx, 0, q - 1);
            var z = Mathf.Clamp((int)fz, 0, q - 1);
            var u = fx - x;
            var w = fz - z;
            var t = 2 * (z * q + x) + (u >= w ? 0 : 1);
            return m_slope[t];
        }

        /// <summary>True when any triangle touching lattice vertex (x, z) is T-edge reachable.</summary>
        public bool VertexReachable(int x, int z)
        {
            var q = m_n - 1;
            for (var dz = -1; dz <= 0; dz++)
            {
                for (var dx = -1; dx <= 0; dx++)
                {
                    var qx = x + dx;
                    var qz = z + dz;
                    if (qx < 0 || qz < 0 || qx >= q || qz >= q) continue;
                    var t = 2 * (qz * q + qx);
                    if (m_reach[t] || m_reach[t + 1]) return true;
                }
            }

            return false;
        }
    }
}
#endif

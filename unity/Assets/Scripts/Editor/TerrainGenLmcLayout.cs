#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GameDev.World;
using GameDev.World.Seed;
using WorldSeed = GameDev.World.Seed.WorldSeed;
using GameDev.World.TerrainGen;
using UnityEngine;
using Random = System.Random;

namespace GameDev.EditorTools
{
    /// <summary>
    /// G3e (LMC) research: the shared LM2 region layout (technical specification <c>GEOGRAPHY_LMC_TECHNICAL_SPEC.md</c>
    /// §§3–4). Editor-only, referenced by nothing; random draws use <c>research.lmc.layout</c>, a stream no production
    /// stage reads. Grid helpers are copied from the G3c research file so that G3c stays byte-identical.
    /// </summary>
    public static class LmcCodes
    {
        public const byte Sea = 0;
        public const byte Lowland = 1;
        public const byte Upland = 2;
        public const byte Highland = 3;
        public const byte PlateauTop = 4;
        public const byte PlateauBand = 5;
        public const byte PlateauRamp = 6;
        public const byte Corridor = 7;

        public const byte RimGradual = 0;
        public const byte RimJoint = 1;
        public const byte RimEscarpment = 2;
        public const byte RimRamp = 3;

        public const byte ShoreNone = 255;
        public const byte ShoreLow = 0;
        public const byte ShoreRocky = 1;
        public const byte ShoreCliff = 2;
    }

    /// <summary>One fixed shoreline (contour A, or G3c's contour B for the continuity check only).</summary>
    public sealed class LmcContour
    {
        public string Id;
        public int StageSeed;
        public Stage1Result Stage1;
        public int N;
        public float Cell;
        public bool[] Land;
        public bool[] Mainland;
        public float[] Sd;
        public float LandAreaSqm;
        public string FieldHash;

        public const string PinnedStage1Hash110201 = "7599879a71250a06";

        public static LmcContour Build(string id, int seed, float landMin, float landMax)
        {
            var settings = TerrainGenSettings.Default;
            if (landMin > 0f)
            {
                settings.LandFractionTargetMin = landMin;
                settings.LandFractionTargetMax = landMax;
            }

            var stage1 = Stage1LandmassGenerator.Generate(seed, settings);
            var field = stage1.Field;
            var n = field.Width;
            var c = new LmcContour
            {
                Id = id,
                StageSeed = seed,
                Stage1 = stage1,
                N = n,
                Cell = field.CellSize,
                Land = new bool[n * n],
                Sd = stage1.SignedDistance,
                FieldHash = field.Hash(),
            };

            var sea = WorldScale.SeaLevelMeters;
            var count = 0;
            for (var i = 0; i < n * n; i++)
            {
                c.Land[i] = field.Heights[i] > sea;
                if (c.Land[i])
                {
                    count++;
                }
            }

            c.LandAreaSqm = count * c.Cell * c.Cell;
            c.Mainland = LmcGrid.LargestComponent(c.Land, n);
            return c;
        }

        /// <summary>Contour A: the Stage 1 coastline of seed 110201 with production settings.</summary>
        public static LmcContour A() => Build("A", 110201, 0f, 0f);

        /// <summary>G3c's contour B (seed 110201, land fraction [0.49, 0.52]) — read only for the continuity check.</summary>
        public static LmcContour G3cB() => Build("B", 110201, 0.49f, 0.52f);
    }

    public sealed class LmcRidge
    {
        public int Id;
        public int Parent = -1;
        public List<Vector2> Points = new List<Vector2>();
        public List<float> Arc = new List<float>();

        public float Length => Arc.Count > 0 ? Arc[Arc.Count - 1] : 0f;

        public void Measure()
        {
            Arc.Clear();
            var s = 0f;
            for (var k = 0; k < Points.Count; k++)
            {
                if (k > 0)
                {
                    s += Vector2.Distance(Points[k - 1], Points[k]);
                }

                Arc.Add(s);
            }
        }

        public Vector2 At(float arc)
        {
            if (Points.Count == 0)
            {
                return Vector2.zero;
            }

            if (arc <= 0f)
            {
                return Points[0];
            }

            for (var k = 1; k < Points.Count; k++)
            {
                if (Arc[k] >= arc)
                {
                    var t = (arc - Arc[k - 1]) / Mathf.Max(1e-4f, Arc[k] - Arc[k - 1]);
                    return Vector2.Lerp(Points[k - 1], Points[k], t);
                }
            }

            return Points[Points.Count - 1];
        }

        /// <summary>Arc position of the polyline point nearest <paramref name="p"/>.</summary>
        public float Project(Vector2 p, out float distance)
        {
            var best = float.PositiveInfinity;
            var bestArc = 0f;
            for (var k = 1; k < Points.Count; k++)
            {
                var a = Points[k - 1];
                var b = Points[k];
                var ab = b - a;
                var len2 = Mathf.Max(1e-6f, ab.sqrMagnitude);
                var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                var q = a + ab * t;
                var d = (p - q).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    bestArc = Arc[k - 1] + t * (Arc[k] - Arc[k - 1]);
                }
            }

            distance = Mathf.Sqrt(best);
            return bestArc;
        }
    }

    public sealed class LmcNode
    {
        public string Kind; // culmination, saddle, pass
        public int Ridge;
        public float Arc;
        public Vector2 P;
        public float Target; // world height (culmination, saddle); for the pass: offset above its lowland
    }

    public sealed class LmcPlateau
    {
        public int Id;
        public Vector2 Centre;
        public float BaseRadius;
        public float[] Radius = new float[360];
        public byte[] Rim = new byte[360];
        public List<float> OutletBearings = new List<float>(); // degrees
        public float RampBearing; // degrees
        public float RampHalfAngle; // degrees
        public float RampLength;
        public float BandWidth;
        public int ClassIndex; // 0: 18–40 m, 1: 60–120 m
        public float EdgeTarget;
        public float TopAreaSqm;

        public static float Wrap(float deg)
        {
            deg %= 360f;
            return deg < 0f ? deg + 360f : deg;
        }

        public static float Delta(float a, float b)
        {
            var d = Mathf.Abs(Wrap(a) - Wrap(b));
            return d > 180f ? 360f - d : d;
        }

        public float RadiusAt(float deg)
        {
            deg = Wrap(deg);
            var k0 = Mathf.FloorToInt(deg) % 360;
            var k1 = (k0 + 1) % 360;
            var t = deg - Mathf.Floor(deg);
            return Mathf.Lerp(Radius[k0], Radius[k1], t);
        }

        public byte RimAt(float deg) => Rim[Mathf.FloorToInt(Wrap(deg)) % 360];

        public float BearingOf(Vector2 p)
        {
            var d = p - Centre;
            return Wrap(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        public Vector2 RimPoint(float deg)
        {
            var r = RadiusAt(deg);
            var a = deg * Mathf.Deg2Rad;
            return Centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }

        public float Share(byte type)
        {
            var k = 0;
            foreach (var t in Rim)
            {
                if (t == type)
                {
                    k++;
                }
            }

            return k / 360f;
        }
    }

    public sealed class LmcLayout
    {
        public int Seed;
        public int N;
        public float Cell;
        public byte[] Region;
        public int[] Instance;
        public byte[] ShoreIntent;
        public List<LmcRidge> Ridges = new List<LmcRidge>();
        public List<LmcNode> Nodes = new List<LmcNode>();
        public int MainCulmination = -1;
        public int PassNode = -1;
        public int PassBasinA = -1;
        public int PassBasinB = -1;
        public List<Vector2> Outlets = new List<Vector2>();
        public List<int> OutletBasin = new List<int>();
        public List<LmcRidge> Trunks = new List<LmcRidge>();
        public List<int> TrunkBasin = new List<int>();
        public List<LmcRidge> PassCorridors = new List<LmcRidge>();
        public List<LmcPlateau> Plateaus = new List<LmcPlateau>();
        public int BasinCount;
        public float CoreT;
        public Vector2 Core;
        public float HighlandShare;
        public float LowlandShare;
        public string Hash;
        public bool Valid;
        public List<string> Checks = new List<string>();
        public List<string> Notes = new List<string>();

        public LmcNode Main => MainCulmination >= 0 ? Nodes[MainCulmination] : null;
        public LmcNode Pass => PassNode >= 0 ? Nodes[PassNode] : null;

        public IEnumerable<LmcNode> Culminations
        {
            get
            {
                foreach (var node in Nodes)
                {
                    if (node.Kind == "culmination")
                    {
                        yield return node;
                    }
                }
            }
        }

        public string Lines()
        {
            var c = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine($"layout seed {Seed}: hash {Hash}, valid {Valid}, core t {CoreT.ToString("F3", c)} at ({Core.x.ToString("F0", c)}, {Core.y.ToString("F0", c)}), highland {(100f * HighlandShare).ToString("F1", c)} %, lowland {(100f * LowlandShare).ToString("F1", c)} %, basins {BasinCount}, ridges {Ridges.Count}, plateaus {Plateaus.Count}, outlets {Outlets.Count}");
            foreach (var node in Nodes)
            {
                sb.AppendLine($"  {node.Kind} ridge {node.Ridge} arc {node.Arc.ToString("F0", c)} at ({node.P.x.ToString("F0", c)}, {node.P.y.ToString("F0", c)}) target {node.Target.ToString("F1", c)}");
            }

            foreach (var pl in Plateaus)
            {
                sb.AppendLine($"  plateau {pl.Id}: centre ({pl.Centre.x.ToString("F0", c)}, {pl.Centre.y.ToString("F0", c)}), top {(pl.TopAreaSqm / 1e6f).ToString("F3", c)} km2, class {(pl.ClassIndex == 0 ? "18-40" : "60-120")}, edge target {pl.EdgeTarget.ToString("F1", c)} m, rim joint {(100f * pl.Share(LmcCodes.RimJoint)).ToString("F0", c)} % escarpment {(100f * pl.Share(LmcCodes.RimEscarpment)).ToString("F0", c)} % ramp {(100f * pl.Share(LmcCodes.RimRamp)).ToString("F0", c)} %, outlets {pl.OutletBearings.Count}, band {pl.BandWidth.ToString("F0", c)} m, ramp length {pl.RampLength.ToString("F0", c)} m");
            }

            for (var k = 0; k < Outlets.Count; k++)
            {
                sb.AppendLine($"  outlet basin {OutletBasin[k]} at ({Outlets[k].x.ToString("F0", c)}, {Outlets[k].y.ToString("F0", c)})");
            }

            foreach (var line in Checks)
            {
                sb.AppendLine("  check " + line);
            }

            foreach (var line in Notes)
            {
                sb.AppendLine("  note " + line);
            }

            return sb.ToString();
        }
    }

    /// <summary>Authoring defaults of the layout (spec §4.1); every value is printed into the report.</summary>
    public sealed class LmcLayoutSettings
    {
        public float HighlandShare = 0.30f;            // [CARRIED] G3c s30
        public float LowlandShare = 0.30f;             // [CARRIED]
        public float CoreMin = 0.30f;
        public float CoreMax = 0.70f;
        public float HighlandMinShoreDistance = 15f;   // [CARRIED]
        public float HighlandClosingMeters = 20f;      // [CARRIED]
        public float SpineWeight = 1.5f;
        public float BranchMinMeters = 200f;           // UpliftCoastGateMeters [FACT]
        public int BranchMin = 2;
        public int BranchMax = 3;
        public int CulminationMin = 2;
        public int CulminationMax = 4;
        public float CulminationSpacing = 450f;
        public float MainSummitMin = 330f;              // world height (spec §2.1)
        public float MainSummitMax = 430f;
        public float SecondaryMin = 0.62f;             // SecondaryHeightMin [FACT]
        public float SecondaryMax = 0.82f;
        public float SaddleMin = 0.55f;                // SaddleFractionMin [FACT]
        public float SaddleMax = 0.72f;
        public float PassOffsetMin = 40f;
        public float PassOffsetMax = 80f;
        public int BasinMin = 2;
        public int BasinMax = 3;
        public float BasinSeedSeparation = 600f;       // [CARRIED]
        public float BasinSeedMinShoreDistance = 60f;  // [CARRIED] fallback room
        public float BasinMinFrontage = 150f;          // Stage 4A lowland frontage [FACT]
        public float OutletSeparation = 400f;          // [CARRIED]
        public float CorridorHalfWidth = 50f;          // [CARRIED]
        public int PlateauMin = 2;
        public int PlateauMax = 4;
        public float PlateauTopMinSqm = 30000f;        // [CARRIED]
        public float PlateauTopMaxSqm = 150000f;       // [CARRIED]
        public float PlateauSeparation = 500f;         // [CARRIED]
        public float PlateauReachToHighland = 150f;
        public float PlateauGapToHighland = 10f;       // Q9 #2 (was 20 m)
        public float PlateauGapToLowland = 30f;        // Q9 #2 (was 60 m)
        public float PlateauGapToCoast = 40f;          // Q9 #2 (was 80 m)
        public float JointShareMin = 0.15f;
        public float JointShareMax = 0.25f;
        public float EscarpmentShareMin = 0.45f;
        public float EscarpmentShareMax = 0.65f;
        public float RampHalfAngle = 25f;              // [CARRIED] RampHalfAngleDeg
        public float RampSlopeDeg = 18f;
        public float WallSlopeDeg = 65f;
        public float ClassLowMin = 18f;
        public float ClassLowMax = 40f;
        public float ClassHighMin = 60f;
        public float ClassHighMax = 120f;
        public float ShoreOutletReach = 100f;
        public float ShoreHinterland = 300f;
        public float ShoreMouthReach = 150f;
        public float ShoreSmooth = 40f;

        public static LmcLayoutSettings Default => new LmcLayoutSettings();

        public IEnumerable<string> Lines()
        {
            foreach (var f in typeof(LmcLayoutSettings).GetFields())
            {
                yield return $"layout.{f.Name} = {Convert.ToString(f.GetValue(this), CultureInfo.InvariantCulture)}";
            }
        }
    }

    public static class LmcLayoutBuilder
    {
        public static LmcLayout Build(LmcContour c, int seed, LmcLayoutSettings s)
        {
            var rng = new WorldSeed(seed).Fork("research.lmc.layout");
            var n = c.N;
            var cell = c.Cell;
            var count = n * n;
            var L = new LmcLayout { Seed = seed, N = n, Cell = cell, Region = new byte[count], Instance = new int[count], ShoreIntent = new byte[count] };
            for (var i = 0; i < count; i++)
            {
                L.Region[i] = c.Land[i] ? LmcCodes.Upland : LmcCodes.Sea;
                L.Instance[i] = -1;
                L.ShoreIntent[i] = LmcCodes.ShoreNone;
            }

            var land = 0;
            for (var i = 0; i < count; i++)
            {
                if (c.Land[i]) land++;
            }

            // ---- spine and core ----
            var spineAllowed = new bool[count];
            for (var i = 0; i < count; i++)
            {
                spineAllowed[i] = c.Mainland[i] && c.Sd[i] >= s.HighlandMinShoreDistance;
            }

            var spine = new LmcRidge { Id = 0 };
            spine.Points = Resample(LmcGrid.WeightedDiameter(spineAllowed, c.Sd, n, cell, 4), 20f);
            spine.Measure();
            L.CoreT = (float)(s.CoreMin + rng.NextDouble() * (s.CoreMax - s.CoreMin));
            L.Core = spine.At(L.CoreT * spine.Length);

            // ---- highland: geodesic distance from the core plus a penalty off the spine, thresholded to the share ----
            var coreIndex = LmcGrid.Index(L.Core, n, cell);
            var geo = LmcGrid.Geodesic(spineAllowed, new List<int> { coreIndex }, n, cell, out _, out _);
            var dSpine = LmcGrid.Edt(LmcGrid.Rasterize(spine.Points, n, cell), n, cell);
            var score = new float[count];
            var scores = new List<float>();
            for (var i = 0; i < count; i++)
            {
                score[i] = spineAllowed[i] && !float.IsInfinity(geo[i]) ? geo[i] + s.SpineWeight * dSpine[i] : float.PositiveInfinity;
                if (!float.IsInfinity(score[i])) scores.Add(score[i]);
            }

            scores.Sort();
            var target = s.HighlandShare * land;
            float lo = 0f, hi = scores.Count > 0 ? scores[scores.Count - 1] : 0f;
            bool[] highland = null;
            for (var iteration = 0; iteration < 14; iteration++)
            {
                var mid = 0.5f * (lo + hi);
                var raw = new bool[count];
                for (var i = 0; i < count; i++) raw[i] = score[i] <= mid;
                var h = LmcGrid.FillHoles(LmcGrid.LargestComponent(LmcGrid.Close(raw, n, cell, s.HighlandClosingMeters), n), n);
                for (var i = 0; i < count; i++) h[i] &= spineAllowed[i];
                var k = 0;
                for (var i = 0; i < count; i++) if (h[i]) k++;
                highland = h;
                if (k > target) hi = mid; else lo = mid;
            }

            for (var i = 0; i < count; i++)
            {
                if (highland[i]) L.Region[i] = LmcCodes.Highland;
            }

            // ---- divide tree: the spine inside the highland, then branches by greatest protrusion ----
            var main = new LmcRidge { Id = 0 };
            var run = new List<Vector2>();
            var coreArc = L.CoreT * spine.Length;
            var bestRun = new List<Vector2>();
            var containsCore = false;
            for (var k = 0; k < spine.Points.Count; k++)
            {
                var inside = highland[LmcGrid.Index(spine.Points[k], n, cell)];
                if (inside)
                {
                    run.Add(spine.Points[k]);
                    if (Mathf.Abs(spine.Arc[k] - coreArc) < 25f) containsCore = true;
                }

                if (!inside || k == spine.Points.Count - 1)
                {
                    if (run.Count > bestRun.Count || (containsCore && run.Count > 1))
                    {
                        if (containsCore || bestRun.Count == 0 || run.Count > bestRun.Count)
                        {
                            bestRun = new List<Vector2>(run);
                        }

                        if (containsCore)
                        {
                            break;
                        }
                    }

                    run.Clear();
                    containsCore = false;
                }
            }

            main.Points = bestRun;
            main.Measure();
            L.Ridges.Add(main);
            var dInside = LmcGrid.Edt(Not(highland), n, cell); // distance to the highland's edge
            var branches = s.BranchMin + rng.Next(s.BranchMax - s.BranchMin + 1);
            var treeMask = LmcGrid.Rasterize(main.Points, n, cell);
            for (var b = 0; b < branches; b++)
            {
                var sources = new List<int>();
                for (var i = 0; i < count; i++) if (treeMask[i] && highland[i]) sources.Add(i);
                if (sources.Count == 0) break;
                var dist = LmcGrid.Geodesic(highland, sources, n, cell, out _, out var prev);
                var tip = -1;
                var best = 0f;
                for (var i = 0; i < count; i++)
                {
                    if (!highland[i] || float.IsInfinity(dist[i])) continue;
                    var v = dist[i] - 0.5f * dInside[i];
                    if (v > best) { best = v; tip = i; }
                }

                if (tip < 0 || dist[tip] < s.BranchMinMeters) break;
                var path = new List<Vector2>();
                for (var i = tip; i >= 0; i = prev[i]) path.Add(LmcGrid.P(i, n, cell));
                path.Reverse(); // from the attachment to the tip
                var ridge = new LmcRidge { Id = L.Ridges.Count, Parent = 0 };
                ridge.Points = Resample(Smooth(path, 3), 20f);
                ridge.Measure();
                L.Ridges.Add(ridge);
                var m = LmcGrid.Rasterize(ridge.Points, n, cell);
                for (var i = 0; i < count; i++) treeMask[i] |= m[i];
            }

            // ---- culminations: widest points of the divide tree, the main one nearest the core ----
            var cands = new List<(int ridge, float arc, Vector2 p, float width)>();
            foreach (var ridge in L.Ridges)
            {
                for (var k = 0; k < ridge.Points.Count; k += 2)
                {
                    var w = dInside[LmcGrid.Index(ridge.Points[k], n, cell)];
                    cands.Add((ridge.Id, ridge.Arc[k], ridge.Points[k], w));
                }
            }

            var chosen = new List<(int ridge, float arc, Vector2 p, float width)>();
            if (cands.Count > 0)
            {
                // Q9 #3: the main culmination and a second one stand on the main divide, so a saddle (and the pass) exists between them
                var first = cands[0];
                var firstScore = float.NegativeInfinity;
                foreach (var cnd in cands)
                {
                    if (cnd.ridge != 0) continue;
                    var sc = cnd.width - 0.25f * Vector2.Distance(cnd.p, L.Core);
                    if (sc > firstScore) { firstScore = sc; first = cnd; }
                }

                chosen.Add(first);
                var wanted = s.CulminationMin + rng.Next(s.CulminationMax - s.CulminationMin + 1);
                var sorted = new List<(int ridge, float arc, Vector2 p, float width)>(cands);
                sorted.Sort((a, b) => b.width != a.width ? b.width.CompareTo(a.width) : a.arc.CompareTo(b.arc));
                foreach (var cnd in sorted)
                {
                    if (cnd.ridge != 0) continue;
                    if (Vector2.Distance(first.p, cnd.p) >= s.CulminationSpacing) { chosen.Add(cnd); break; }
                }

                if (chosen.Count < 2) L.Notes.Add("the main divide holds no second culmination 450 m from the first");
                foreach (var cnd in sorted)
                {
                    if (chosen.Count >= wanted) break;
                    var far = true;
                    foreach (var q in chosen) far &= Vector2.Distance(q.p, cnd.p) >= s.CulminationSpacing;
                    if (far) chosen.Add(cnd);
                }
            }

            var mainHeight = (float)(s.MainSummitMin + rng.NextDouble() * (s.MainSummitMax - s.MainSummitMin));
            for (var k = 0; k < chosen.Count; k++)
            {
                var h = k == 0 ? mainHeight : mainHeight * (float)(s.SecondaryMin + rng.NextDouble() * (s.SecondaryMax - s.SecondaryMin));
                L.Nodes.Add(new LmcNode { Kind = "culmination", Ridge = chosen[k].ridge, Arc = chosen[k].arc, P = chosen[k].p, Target = h });
            }

            L.MainCulmination = L.Nodes.Count > 0 ? 0 : -1;

            // ---- lowland basins ----
            var nonHigh = new bool[count];
            for (var i = 0; i < count; i++) nonHigh[i] = c.Mainland[i] && !highland[i];
            var dHigh = LmcGrid.Edt(highland, n, cell);
            var basinsWanted = s.BasinMin + rng.Next(s.BasinMax - s.BasinMin + 1);
            var seedCands = new List<int>();
            for (var i = 0; i < count; i++) if (nonHigh[i] && c.Sd[i] >= s.BasinSeedMinShoreDistance && dHigh[i] >= 150f) seedCands.Add(i);
            seedCands.Sort((a, b) => c.Sd[b] != c.Sd[a] ? c.Sd[b].CompareTo(c.Sd[a]) : a.CompareTo(b));
            var basinSeeds = new List<int>();
            foreach (var i in seedCands)
            {
                if (basinSeeds.Count >= basinsWanted) break;
                var p = LmcGrid.P(i, n, cell);
                var far = true;
                foreach (var j in basinSeeds) far &= Vector2.Distance(LmcGrid.P(j, n, cell), p) >= s.BasinSeedSeparation;
                if (far) basinSeeds.Add(i);
            }

            L.BasinCount = basinSeeds.Count;
            var sdMax = 1f;
            for (var i = 0; i < count; i++) if (c.Land[i]) sdMax = Mathf.Max(sdMax, c.Sd[i]);
            var basinDist = WeightedGeodesic(nonHigh, basinSeeds, n, cell, i => 1f + 1.5f * Mathf.Clamp01(c.Sd[i] / sdMax), out var basinOwner);
            var order = new List<int>();
            for (var i = 0; i < count; i++) if (nonHigh[i] && basinOwner[i] >= 0) order.Add(i);
            order.Sort((a, b) => basinDist[a] != basinDist[b] ? basinDist[a].CompareTo(basinDist[b]) : a.CompareTo(b));
            var lowTarget = Mathf.RoundToInt(s.LowlandShare * land);
            for (var k = 0; k < order.Count && k < lowTarget; k++)
            {
                L.Region[order[k]] = LmcCodes.Lowland;
                L.Instance[order[k]] = basinOwner[order[k]];
            }

            // frontage: a basin that does not reach its own coast with 150 m gets a strip toward the nearest shore
            var shore = new bool[count];
            for (var z = 1; z < n - 1; z++)
            for (var x = 1; x < n - 1; x++)
            {
                var i = z * n + x;
                shore[i] = c.Land[i] && (!c.Land[i - 1] || !c.Land[i + 1] || !c.Land[i - n] || !c.Land[i + n]);
            }

            for (var b = 0; b < L.BasinCount; b++)
            {
                var front = 0;
                for (var i = 0; i < count; i++) if (shore[i] && L.Region[i] == LmcCodes.Lowland && L.Instance[i] == b) front++;
                if (front * cell >= s.BasinMinFrontage) continue;
                var src = new List<int>();
                for (var i = 0; i < count; i++) if (L.Region[i] == LmcCodes.Lowland && L.Instance[i] == b) src.Add(i);
                var dd = LmcGrid.Geodesic(nonHigh, src, n, cell, out _, out var pv);
                var best = -1;
                for (var i = 0; i < count; i++) if (shore[i] && nonHigh[i] && !float.IsInfinity(dd[i]) && (best < 0 || dd[i] < dd[best])) best = i;
                if (best < 0) continue;
                var path = new List<Vector2>();
                for (var i = best; i >= 0; i = pv[i]) path.Add(LmcGrid.P(i, n, cell));
                var strip = LmcGrid.Edt(LmcGrid.Rasterize(path, n, cell), n, cell);
                for (var i = 0; i < count; i++)
                {
                    if (nonHigh[i] && strip[i] <= 0.5f * s.BasinMinFrontage && L.Region[i] == LmcCodes.Upland)
                    {
                        L.Region[i] = LmcCodes.Lowland;
                        L.Instance[i] = b;
                    }
                }

                L.Notes.Add($"basin {b}: frontage strip added toward the nearest shore");
            }

            // ---- outlets: per basin, its shore vertex farthest from the highland, 400 m apart ----
            for (var b = 0; b < L.BasinCount; b++)
            {
                var opts = new List<int>();
                for (var i = 0; i < count; i++) if (shore[i] && L.Region[i] == LmcCodes.Lowland && L.Instance[i] == b) opts.Add(i);
                opts.Sort((p, q) => dHigh[q] != dHigh[p] ? dHigh[q].CompareTo(dHigh[p]) : p.CompareTo(q));
                foreach (var i in opts)
                {
                    var pt = LmcGrid.P(i, n, cell);
                    var far = true;
                    foreach (var o in L.Outlets) far &= Vector2.Distance(o, pt) >= s.OutletSeparation;
                    if (!far) continue;
                    L.Outlets.Add(pt);
                    L.OutletBasin.Add(b);
                    break;
                }
            }

            // ---- trunks: from the highland edge nearest each outlet, through the basin, to the outlet ----
            var trunkDomain = new bool[count];
            for (var i = 0; i < count; i++) trunkDomain[i] = c.Mainland[i];
            for (var k = 0; k < L.Outlets.Count; k++)
            {
                var oi = LmcGrid.Index(L.Outlets[k], n, cell);
                var dd = WeightedGeodesic(trunkDomain, new List<int> { oi }, n, cell, i => highland[i] ? 25f : (L.Region[i] == LmcCodes.Lowland ? 1f : 2f), out _, out var pv);
                var best = -1;
                for (var i = 0; i < count; i++)
                {
                    if (!highland[i] || float.IsInfinity(dd[i])) continue;
                    var x = i % n; var z = i / n;
                    var edge = x > 0 && x < n - 1 && z > 0 && z < n - 1 && (!highland[i - 1] || !highland[i + 1] || !highland[i - n] || !highland[i + n]);
                    if (edge && (best < 0 || dd[i] < dd[best])) best = i;
                }

                if (best < 0) continue;
                var path = new List<Vector2>();
                for (var i = best; i >= 0; i = pv[i]) path.Add(LmcGrid.P(i, n, cell));
                var trunk = new LmcRidge { Id = L.Trunks.Count };
                trunk.Points = Resample(Smooth(path, 3), 20f);
                trunk.Measure();
                L.Trunks.Add(trunk);
                L.TrunkBasin.Add(L.OutletBasin[k]);
                PaintCorridor(L, trunk, s.CorridorHalfWidth, n, cell);
            }

            // ---- saddles and the pass on the main divide ----
            var mainCulms = new List<LmcNode>();
            foreach (var node in L.Nodes) if (node.Ridge == 0) mainCulms.Add(node);
            mainCulms.Sort((a, b) => a.Arc.CompareTo(b.Arc));
            // Q9 #1: the geodesic owner is an index into the source list; map it to the basin instance
            var basinCells = BasinCells(L, -1);
            LmcGrid.Geodesic(c.Mainland, basinCells, n, cell, out var basinOwnerIdx, out _);
            var basinOwnerAll = new int[count];
            for (var i = 0; i < count; i++) basinOwnerAll[i] = basinOwnerIdx[i] >= 0 ? L.Instance[basinCells[basinOwnerIdx[i]]] : -1;
            var passCandidates = new List<LmcNode>();
            for (var k = 0; k + 1 < mainCulms.Count; k++)
            {
                var a0 = mainCulms[k].Arc;
                var a1 = mainCulms[k + 1].Arc;
                var bestArc = 0.5f * (a0 + a1);
                var bestW = float.PositiveInfinity;
                for (var j = 0; j < main.Points.Count; j++)
                {
                    if (main.Arc[j] <= a0 + 0.2f * (a1 - a0) || main.Arc[j] >= a1 - 0.2f * (a1 - a0)) continue;
                    var w = dInside[LmcGrid.Index(main.Points[j], n, cell)];
                    if (w < bestW) { bestW = w; bestArc = main.Arc[j]; }
                }

                var lower = Mathf.Min(mainCulms[k].Target, mainCulms[k + 1].Target);
                var saddle = new LmcNode { Kind = "saddle", Ridge = 0, Arc = bestArc, P = main.At(bestArc), Target = lower * (float)(s.SaddleMin + rng.NextDouble() * (s.SaddleMax - s.SaddleMin)) };
                L.Nodes.Add(saddle);
                passCandidates.Add(saddle);
            }

            LmcNode passSaddle = null;
            var passSides = (-1, -1);
            foreach (var cand in passCandidates)
            {
                var sides = Sides(main, cand.Arc, basinOwnerAll, n, cell, c.Mainland);
                if (sides.Item1 >= 0 && sides.Item2 >= 0 && sides.Item1 != sides.Item2 && (passSaddle == null || cand.Target < passSaddle.Target))
                {
                    passSaddle = cand;
                    passSides = sides;
                }
            }

            if (passSaddle != null)
            {
                var pass = new LmcNode { Kind = "pass", Ridge = 0, Arc = passSaddle.Arc, P = passSaddle.P, Target = (float)(s.PassOffsetMin + rng.NextDouble() * (s.PassOffsetMax - s.PassOffsetMin)) };
                L.Nodes.Remove(passSaddle);
                L.Nodes.Add(pass);
                L.PassNode = L.Nodes.Count - 1;
                L.PassBasinA = passSides.Item1;
                L.PassBasinB = passSides.Item2;
                foreach (var b in new[] { passSides.Item1, passSides.Item2 })
                {
                    var pi = LmcGrid.Index(pass.P, n, cell);
                    var dd = WeightedGeodesic(c.Mainland, new List<int> { pi }, n, cell, i => highland[i] ? 4f : 1f, out _, out var pv);
                    var best = -1;
                    for (var i = 0; i < count; i++) if (L.Region[i] == LmcCodes.Lowland && L.Instance[i] == b && !float.IsInfinity(dd[i]) && (best < 0 || dd[i] < dd[best])) best = i;
                    if (best < 0) continue;
                    var path = new List<Vector2>();
                    for (var i = best; i >= 0; i = pv[i]) path.Add(LmcGrid.P(i, n, cell));
                    path.Reverse(); // from the pass down to the basin
                    var corridor = new LmcRidge { Id = L.PassCorridors.Count };
                    corridor.Points = Resample(Smooth(path, 3), 20f);
                    corridor.Measure();
                    L.PassCorridors.Add(corridor);
                    PaintCorridor(L, corridor, s.CorridorHalfWidth, n, cell);
                }
            }
            else
            {
                L.Notes.Add("no saddle on the main divide separates two basins: no pass declared");
            }

            // ---- plateaus ----
            BuildPlateaus(L, c, s, rng, highland, dHigh, basinOwnerAll);

            // ---- shore intents ----
            ShoreIntents(L, c, s, shore);

            // ---- shares, hash, validation ----
            int hc = 0, lc = 0;
            for (var i = 0; i < count; i++)
            {
                if (L.Region[i] == LmcCodes.Highland) hc++;
                if (L.Region[i] == LmcCodes.Lowland) lc++;
            }

            L.HighlandShare = hc / (float)land;
            L.LowlandShare = lc / (float)land;
            L.Hash = HashOf(L);
            Validate(L, c, s);
            return L;
        }

        // ------------------------------------------------------------------ plateaus

        private static void BuildPlateaus(LmcLayout L, LmcContour c, LmcLayoutSettings s, Random rng, bool[] highland, float[] dHigh, int[] basinOwner)
        {
            var n = L.N;
            var cell = L.Cell;
            var count = n * n;
            var wanted = s.PlateauMin + rng.Next(s.PlateauMax - s.PlateauMin + 1);
            var dLow = LmcGrid.Edt(Mask(L.Region, LmcCodes.Lowland), n, cell);
            var mainRidge = L.Ridges.Count > 0 ? L.Ridges[0] : null;
            var used = new List<(Vector2 p, int basin, int side)>();
            var candidates = new List<int>();
            for (var i = 0; i < count; i += 3)
            {
                if (L.Region[i] == LmcCodes.Upland && c.Mainland[i]) candidates.Add(i);
            }

            // Q9 #2: search the feasible ground for each drawn top size instead of 400 blind draws; windows widened
            var area = (float)(s.PlateauTopMinSqm + rng.NextDouble() * (s.PlateauTopMaxSqm - s.PlateauTopMinSqm));
            for (var attempt = 0; attempt < 60 && L.Plateaus.Count < wanted; attempt++)
            {
                var r0 = Mathf.Sqrt(area / Mathf.PI);
                var feasible = new List<(int i, int basin, int side)>();
                var preferred = new List<(int i, int basin, int side)>();
                foreach (var cand in candidates)
                {
                    if (dHigh[cand] < r0 + s.PlateauGapToHighland || dHigh[cand] > r0 + s.PlateauReachToHighland) continue;
                    if (dLow[cand] < r0 + s.PlateauGapToLowland || c.Sd[cand] < r0 + s.PlateauGapToCoast) continue;
                    var pc = LmcGrid.P(cand, n, cell);
                    var far = true;
                    foreach (var u in used) far &= Vector2.Distance(u.p, pc) >= s.PlateauSeparation;
                    if (!far) continue;
                    var sideC = 0;
                    if (mainRidge != null && mainRidge.Points.Count > 1)
                    {
                        var arcC = mainRidge.Project(pc, out _);
                        var aC = mainRidge.At(Mathf.Max(0f, arcC - 10f));
                        var bC = mainRidge.At(Mathf.Min(mainRidge.Length, arcC + 10f));
                        var tC = bC - aC;
                        sideC = (tC.x * (pc.y - aC.y) - tC.y * (pc.x - aC.x)) >= 0f ? 1 : -1;
                    }

                    feasible.Add((cand, basinOwner[cand], sideC));
                    var fresh = true;
                    foreach (var u in used) fresh &= !(u.basin == basinOwner[cand] && u.side == sideC);
                    if (fresh) preferred.Add((cand, basinOwner[cand], sideC));
                }

                if (feasible.Count == 0)
                {
                    if (area <= s.PlateauTopMinSqm) break;
                    area = Mathf.Max(s.PlateauTopMinSqm, 0.75f * area);
                    continue;
                }

                var pool = preferred.Count > 0 ? preferred : feasible;
                var chosenCand = pool[rng.Next(pool.Count)];
                var pick = chosenCand.i;
                var p = LmcGrid.P(pick, n, cell);
                var basin = chosenCand.basin;
                var side = chosenCand.side;

                var pl = new LmcPlateau { Id = L.Plateaus.Count, Centre = p, BaseRadius = r0 };
                var ph1 = (float)(rng.NextDouble() * 360.0);
                var ph2 = (float)(rng.NextDouble() * 360.0);
                for (var k = 0; k < 360; k++)
                {
                    var a = k * Mathf.Deg2Rad;
                    pl.Radius[k] = r0 * (1f + 0.12f * Mathf.Sin(2f * a + ph1 * Mathf.Deg2Rad) + 0.08f * Mathf.Sin(3f * a + ph2 * Mathf.Deg2Rad));
                }

                // joint toward the highland, ramp toward the lowest ground (the nearest lowland), escarpment opposite the joint
                var hb = BearingToMask(p, highland, n, cell, 600f);
                var rampB = BearingToMask(p, Mask(L.Region, LmcCodes.Lowland), n, cell, 1500f);
                if (PlateauDelta(hb, rampB) < 60f) rampB = LmcPlateau.Wrap(hb + 150f);
                var jointHalf = 0.5f * 360f * (float)(s.JointShareMin + rng.NextDouble() * (s.JointShareMax - s.JointShareMin));
                for (var k = 0; k < 360; k++)
                {
                    pl.Rim[k] = LmcCodes.RimGradual;
                    if (PlateauDelta(k, hb) <= jointHalf) pl.Rim[k] = LmcCodes.RimJoint;
                }

                pl.RampBearing = rampB;
                pl.RampHalfAngle = s.RampHalfAngle;
                for (var k = 0; k < 360; k++)
                {
                    if (PlateauDelta(k, rampB) <= s.RampHalfAngle && pl.Rim[k] != LmcCodes.RimJoint) pl.Rim[k] = LmcCodes.RimRamp;
                }

                var escShare = (float)(s.EscarpmentShareMin + rng.NextDouble() * (s.EscarpmentShareMax - s.EscarpmentShareMin));
                var free = new List<int>();
                for (var k = 0; k < 360; k++) if (pl.Rim[k] == LmcCodes.RimGradual) free.Add(k);
                free.Sort((a, b) => PlateauDelta(b, hb) != PlateauDelta(a, hb) ? PlateauDelta(b, hb).CompareTo(PlateauDelta(a, hb)) : a.CompareTo(b));
                var escCount = Mathf.Min(free.Count, Mathf.RoundToInt(escShare * 360f));
                for (var k = 0; k < escCount; k++) pl.Rim[free[k]] = LmcCodes.RimEscarpment;

                // rim outlets (re-entrants) inside escarpment arcs
                var outlets = 1 + rng.Next(3);
                var escDeg = new List<int>();
                for (var k = 0; k < 360; k++) if (pl.Rim[k] == LmcCodes.RimEscarpment) escDeg.Add(k);
                for (var t = 0; t < 40 && pl.OutletBearings.Count < outlets && escDeg.Count > 0; t++)
                {
                    var bdeg = escDeg[rng.Next(escDeg.Count)];
                    var far = true;
                    foreach (var o in pl.OutletBearings) far &= PlateauDelta(o, bdeg) >= 40f;
                    if (far) pl.OutletBearings.Add(bdeg);
                }

                foreach (var o in pl.OutletBearings)
                {
                    for (var k = 0; k < 360; k++)
                    {
                        var d = PlateauDelta(k, o);
                        if (d < 12f) pl.Radius[k] *= 1f - 0.35f * (1f - d / 12f);
                    }
                }

                pl.ClassIndex = rng.NextDouble() < 0.5 ? 0 : 1;
                pl.EdgeTarget = pl.ClassIndex == 0
                    ? (float)(s.ClassLowMin + rng.NextDouble() * (s.ClassLowMax - s.ClassLowMin))
                    : (float)(s.ClassHighMin + rng.NextDouble() * (s.ClassHighMax - s.ClassHighMin));
                pl.BandWidth = pl.EdgeTarget / Mathf.Tan(s.WallSlopeDeg * Mathf.Deg2Rad) + 10f;
                pl.RampLength = 1.15f * pl.EdgeTarget / Mathf.Tan(s.RampSlopeDeg * Mathf.Deg2Rad);

                // paint: only on upland or corridor ground; reject if the top would touch anything else
                var rMax = 0f;
                for (var k = 0; k < 360; k++) rMax = Mathf.Max(rMax, pl.Radius[k]);
                var reach = rMax + Mathf.Max(pl.BandWidth, pl.RampLength) + 5f;
                var rc = Mathf.CeilToInt(reach / cell);
                var cx = Mathf.RoundToInt(p.x / cell);
                var cz = Mathf.RoundToInt(p.y / cell);
                var tops = new List<int>();
                var bands = new List<int>();
                var ramps = new List<int>();
                var clash = false;
                for (var dz = -rc; dz <= rc && !clash; dz++)
                for (var dx = -rc; dx <= rc; dx++)
                {
                    var x = cx + dx; var z = cz + dz;
                    if (x < 0 || z < 0 || x >= n || z >= n) continue;
                    var i = z * n + x;
                    var q = new Vector2(x * cell, z * cell);
                    var rho = Vector2.Distance(q, p);
                    var bdeg = pl.BearingOf(q);
                    var rr = pl.RadiusAt(bdeg);
                    var free2 = L.Region[i] == LmcCodes.Upland || L.Region[i] == LmcCodes.Corridor;
                    if (rho <= rr)
                    {
                        if (!free2) { clash = true; break; }
                        tops.Add(i);
                    }
                    else if (pl.RimAt(bdeg) == LmcCodes.RimEscarpment && rho <= rr + pl.BandWidth && free2)
                    {
                        bands.Add(i);
                    }
                    else if (pl.RimAt(bdeg) == LmcCodes.RimRamp && rho <= rr + pl.RampLength && free2)
                    {
                        ramps.Add(i);
                    }
                }

                if (clash || tops.Count == 0)
                {
                    area = Mathf.Max(s.PlateauTopMinSqm, 0.85f * area);
                    continue;
                }
                foreach (var i in tops) { L.Region[i] = LmcCodes.PlateauTop; L.Instance[i] = pl.Id; }
                foreach (var i in bands) { L.Region[i] = LmcCodes.PlateauBand; L.Instance[i] = pl.Id; }
                foreach (var i in ramps) { L.Region[i] = LmcCodes.PlateauRamp; L.Instance[i] = pl.Id; }
                pl.TopAreaSqm = tops.Count * cell * cell;
                L.Plateaus.Add(pl);
                used.Add((p, basin, side));
                area = (float)(s.PlateauTopMinSqm + rng.NextDouble() * (s.PlateauTopMaxSqm - s.PlateauTopMinSqm));
            }
        }

        private static float PlateauDelta(float a, float b) => LmcPlateau.Delta(a, b);

        private static float BearingToMask(Vector2 p, bool[] mask, int n, float cell, float reach)
        {
            var rc = Mathf.CeilToInt(reach / cell);
            var cx = Mathf.RoundToInt(p.x / cell);
            var cz = Mathf.RoundToInt(p.y / cell);
            var best = float.PositiveInfinity;
            var bearing = 0f;
            for (var dz = -rc; dz <= rc; dz += 2)
            for (var dx = -rc; dx <= rc; dx += 2)
            {
                var x = cx + dx; var z = cz + dz;
                if (x < 0 || z < 0 || x >= n || z >= n || !mask[z * n + x]) continue;
                var d = dx * dx + dz * dz;
                if (d < best)
                {
                    best = d;
                    bearing = LmcPlateau.Wrap(Mathf.Atan2(dz, dx) * Mathf.Rad2Deg);
                }
            }

            return bearing;
        }

        // ------------------------------------------------------------------ shore intents

        private static void ShoreIntents(LmcLayout L, LmcContour c, LmcLayoutSettings s, bool[] shore)
        {
            var n = L.N;
            var cell = L.Cell;
            var count = n * n;
            var outletMask = new bool[count];
            foreach (var o in L.Outlets) outletMask[LmcGrid.Index(o, n, cell)] = true;
            var dOutlet = LmcGrid.Edt(outletMask, n, cell);
            var lowOrCorr = new bool[count];
            for (var i = 0; i < count; i++) lowOrCorr[i] = L.Region[i] == LmcCodes.Lowland || L.Region[i] == LmcCodes.Corridor;
            var dLowCorr = LmcGrid.Edt(lowOrCorr, n, cell);
            var r300 = Mathf.CeilToInt(s.ShoreHinterland / cell);
            for (var i = 0; i < count; i++)
            {
                if (!shore[i]) continue;
                var x = i % n; var z = i / n;
                int lowland = 0, high = 0, total = 0;
                for (var dz = -r300; dz <= r300; dz += 3)
                for (var dx = -r300; dx <= r300; dx += 3)
                {
                    if (dx * dx + dz * dz > r300 * r300) continue;
                    var xx = x + dx; var zz = z + dz;
                    if (xx < 0 || zz < 0 || xx >= n || zz >= n) continue;
                    var j = zz * n + xx;
                    if (!c.Land[j]) continue;
                    total++;
                    var reg = L.Region[j];
                    if (reg == LmcCodes.Lowland || reg == LmcCodes.Corridor) lowland++;
                    if (reg == LmcCodes.Highland || reg == LmcCodes.PlateauTop || reg == LmcCodes.PlateauBand || reg == LmcCodes.PlateauRamp) high++;
                }

                byte intent;
                if (dOutlet[i] <= s.ShoreOutletReach || lowland * 2 > total) intent = LmcCodes.ShoreLow;
                else if (high > 0 && dOutlet[i] > s.ShoreMouthReach && dLowCorr[i] > s.ShoreMouthReach) intent = LmcCodes.ShoreCliff;
                else intent = LmcCodes.ShoreRocky;
                L.ShoreIntent[i] = intent;
            }

            // merge short runs: majority of shore intents within 40 m, three passes
            var rs = Mathf.CeilToInt(s.ShoreSmooth / cell);
            for (var pass = 0; pass < 3; pass++)
            {
                var next = (byte[])L.ShoreIntent.Clone();
                for (var i = 0; i < count; i++)
                {
                    if (L.ShoreIntent[i] == LmcCodes.ShoreNone) continue;
                    var x = i % n; var z = i / n;
                    var votes = new int[3];
                    for (var dz = -rs; dz <= rs; dz++)
                    for (var dx = -rs; dx <= rs; dx++)
                    {
                        var xx = x + dx; var zz = z + dz;
                        if (xx < 0 || zz < 0 || xx >= n || zz >= n) continue;
                        var v = L.ShoreIntent[zz * n + xx];
                        if (v != LmcCodes.ShoreNone) votes[v]++;
                    }

                    var best = L.ShoreIntent[i];
                    for (var k = 0; k < 3; k++) if (votes[k] > votes[best]) best = (byte)k;
                    if (dOutlet[i] <= s.ShoreOutletReach) best = LmcCodes.ShoreLow;
                    next[i] = best;
                }

                L.ShoreIntent = next;
            }
        }

        // ------------------------------------------------------------------ validation (spec §4.3)

        private static void Validate(LmcLayout L, LmcContour c, LmcLayoutSettings s)
        {
            var n = L.N;
            var cell = L.Cell;
            var count = n * n;
            var ok = true;

            void Check(string id, bool pass, string detail)
            {
                L.Checks.Add($"{id} {(pass ? "PASS" : "FAIL")} {detail}");
                ok &= pass;
            }

            var bad = 0;
            for (var i = 0; i < count; i++)
            {
                var land = c.Land[i];
                if (land && (L.Region[i] < 1 || L.Region[i] > 7)) bad++;
                if (!land && L.Region[i] != LmcCodes.Sea) bad++;
            }

            Check("V-L1", bad == 0, $"{bad} vertices without a valid code");
            Check("V-L2", bad == 0, "land mask equals contour A (codes are written on land only)");
            var hs = 100f * L.HighlandShare;
            var ls = 100f * L.LowlandShare;
            Check("V-L3", Mathf.Abs(hs - 30f) <= 2f && Mathf.Abs(ls - 30f) <= 2f, string.Format(CultureInfo.InvariantCulture, "highland {0:F1} %, lowland {1:F1} %", hs, ls));

            var conn = true;
            var detail = new StringBuilder();
            var hiComps = Components(Mask(L.Region, LmcCodes.Highland), n);
            conn &= hiComps == 1;
            detail.Append($"highland components {hiComps}; ");
            for (var b = 0; b < L.BasinCount; b++)
            {
                var m = new bool[count];
                for (var i = 0; i < count; i++) m[i] = L.Region[i] == LmcCodes.Lowland && L.Instance[i] == b;
                var k = Components(m, n);
                conn &= k == 1;
                detail.Append($"basin {b} components {k}; ");
            }

            foreach (var pl in L.Plateaus)
            {
                var m = new bool[count];
                for (var i = 0; i < count; i++) m[i] = L.Region[i] == LmcCodes.PlateauTop && L.Instance[i] == pl.Id;
                var k = Components(m, n);
                conn &= k == 1;
                detail.Append($"plateau {pl.Id} top components {k}; ");
            }

            Check("V-L4", conn, detail.ToString());

            var outletsOk = true;
            var od = new StringBuilder();
            for (var b = 0; b < L.BasinCount; b++)
            {
                var k = 0;
                for (var j = 0; j < L.Outlets.Count; j++)
                {
                    if (L.OutletBasin[j] != b) continue;
                    k++;
                    var oi = LmcGrid.Index(L.Outlets[j], n, cell);
                    outletsOk &= L.Region[oi] == LmcCodes.Lowland && L.Instance[oi] == b;
                }

                outletsOk &= k == 1;
                od.Append($"basin {b}: {k} outlet; ");
            }

            for (var a = 0; a < L.Outlets.Count; a++)
            for (var b = a + 1; b < L.Outlets.Count; b++)
                outletsOk &= Vector2.Distance(L.Outlets[a], L.Outlets[b]) >= s.OutletSeparation;
            Check("V-L5", outletsOk && L.BasinCount >= 2, od.ToString());

            var platOk = L.Plateaus.Count >= s.PlateauMin && L.Plateaus.Count <= s.PlateauMax;
            var pd = new StringBuilder($"{L.Plateaus.Count} plateaus; ");
            foreach (var pl in L.Plateaus)
            {
                var j = pl.Share(LmcCodes.RimJoint);
                var e = pl.Share(LmcCodes.RimEscarpment);
                var rampDeg = pl.Share(LmcCodes.RimRamp) * 360f;
                var rampWidth = rampDeg * Mathf.Deg2Rad * pl.RadiusAt(pl.RampBearing);
                var good = j >= 0.15f && e >= 0.20f && e <= 0.70f && rampWidth >= 40f;
                platOk &= good;
                pd.Append(string.Format(CultureInfo.InvariantCulture, "p{0}: joint {1:F2} esc {2:F2} ramp {3:F0} m; ", pl.Id, j, e, rampWidth));
            }

            Check("V-L6", platOk, pd.ToString());

            var culms = new List<LmcNode>(L.Culminations);
            var spaced = true;
            for (var a = 0; a < culms.Count; a++)
            for (var b = a + 1; b < culms.Count; b++)
                spaced &= Vector2.Distance(culms[a].P, culms[b].P) >= s.CulminationSpacing;
            var passOk = L.Pass != null && L.PassBasinA >= 0 && L.PassBasinB >= 0 && L.PassBasinA != L.PassBasinB && L.PassCorridors.Count == 2;
            Check("V-L7", culms.Count >= 2 && culms.Count <= 4 && spaced && passOk, $"{culms.Count} culminations, spaced {spaced}, pass {passOk} (basins {L.PassBasinA}/{L.PassBasinB}, corridors {L.PassCorridors.Count})");
            L.Valid = ok;
        }

        // ------------------------------------------------------------------ helpers

        private static (int, int) Sides(LmcRidge ridge, float arc, int[] owner, int n, float cell, bool[] land)
        {
            var a = ridge.At(Mathf.Max(0f, arc - 20f));
            var b = ridge.At(Mathf.Min(ridge.Length, arc + 20f));
            var t = (b - a).normalized;
            var normal = new Vector2(-t.y, t.x);
            var p = ridge.At(arc);
            var left = -1;
            var right = -1;
            for (var d = 200f; d <= 1200f && (left < 0 || right < 0); d += 50f)
            {
                var l = p + normal * d;
                var r = p - normal * d;
                if (left < 0) { var i = LmcGrid.Index(l, n, cell); if (land[i] && owner[i] >= 0) left = owner[i]; }
                if (right < 0) { var i = LmcGrid.Index(r, n, cell); if (land[i] && owner[i] >= 0) right = owner[i]; }
            }

            return (left, right);
        }

        private static List<int> BasinCells(LmcLayout L, int basin)
        {
            var list = new List<int>();
            for (var i = 0; i < L.Region.Length; i++)
            {
                if (L.Region[i] == LmcCodes.Lowland && (basin < 0 || L.Instance[i] == basin)) list.Add(i);
            }

            return list;
        }

        private static void PaintCorridor(LmcLayout L, LmcRidge path, float halfWidth, int n, float cell)
        {
            var d = LmcGrid.Edt(LmcGrid.Rasterize(path.Points, n, cell), n, cell);
            for (var i = 0; i < d.Length; i++)
            {
                if (d[i] <= halfWidth && L.Region[i] == LmcCodes.Upland) L.Region[i] = LmcCodes.Corridor;
            }
        }

        public static bool[] Mask(byte[] region, byte code)
        {
            var m = new bool[region.Length];
            for (var i = 0; i < m.Length; i++) m[i] = region[i] == code;
            return m;
        }

        private static bool[] Not(bool[] m)
        {
            var r = new bool[m.Length];
            for (var i = 0; i < m.Length; i++) r[i] = !m[i];
            return r;
        }

        private static int Components(bool[] mask, int n)
        {
            GeographyGrid.Components8(mask, n, out var k);
            return k;
        }

        public static List<Vector2> Resample(List<Vector2> pts, float step)
        {
            var result = new List<Vector2>();
            if (pts.Count == 0) return result;
            result.Add(pts[0]);
            var carry = 0f;
            for (var k = 1; k < pts.Count; k++)
            {
                var a = pts[k - 1];
                var b = pts[k];
                var len = Vector2.Distance(a, b);
                var t = step - carry;
                while (t <= len)
                {
                    result.Add(Vector2.Lerp(a, b, t / Mathf.Max(1e-6f, len)));
                    t += step;
                }

                carry = len - (t - step);
            }

            if (Vector2.Distance(result[result.Count - 1], pts[pts.Count - 1]) > 0.25f * step) result.Add(pts[pts.Count - 1]);
            return result;
        }

        public static List<Vector2> Smooth(List<Vector2> pts, int half)
        {
            var r = new List<Vector2>(pts.Count);
            for (var k = 0; k < pts.Count; k++)
            {
                if (k == 0 || k == pts.Count - 1) { r.Add(pts[k]); continue; }
                var acc = Vector2.zero; var cnt = 0;
                for (var j = Mathf.Max(0, k - half); j <= Mathf.Min(pts.Count - 1, k + half); j++) { acc += pts[j]; cnt++; }
                r.Add(acc / cnt);
            }

            return r;
        }

        /// <summary>Geodesic distance with a per-cell cost, multi-source with owners.</summary>
        public static float[] WeightedGeodesic(bool[] domain, List<int> sources, int n, float cell, Func<int, float> cost, out int[] owner)
        {
            return WeightedGeodesic(domain, sources, n, cell, cost, out owner, out _);
        }

        public static float[] WeightedGeodesic(bool[] domain, List<int> sources, int n, float cell, Func<int, float> cost, out int[] owner, out int[] prev)
        {
            var count = n * n;
            var dist = new float[count];
            owner = new int[count];
            prev = new int[count];
            for (var i = 0; i < count; i++) { dist[i] = float.PositiveInfinity; owner[i] = -1; prev[i] = -1; }
            var heap = new LmcHeap(1024);
            for (var k = 0; k < sources.Count; k++)
            {
                var i = sources[k];
                dist[i] = 0f;
                owner[i] = k;
                heap.Push(0f, i);
            }

            while (heap.Count > 0)
            {
                heap.Pop(out var key, out var i);
                if (key > dist[i]) continue;
                var x = i % n; var z = i / n;
                var ci = cost(i);
                for (var dz = -1; dz <= 1; dz++)
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dz == 0) continue;
                    var xx = x + dx; var zz = z + dz;
                    if (xx < 0 || zz < 0 || xx >= n || zz >= n) continue;
                    var j = zz * n + xx;
                    if (!domain[j]) continue;
                    var len = (dx != 0 && dz != 0 ? 1.41421356f : 1f) * cell;
                    var nd = key + len * 0.5f * (ci + cost(j));
                    if (nd < dist[j]) { dist[j] = nd; owner[j] = owner[i]; prev[j] = i; heap.Push(nd, j); }
                }
            }

            return dist;
        }

        public static string HashOf(LmcLayout L)
        {
            ulong h = 1469598103934665603UL;
            void Mix(byte b) { h ^= b; h *= 1099511628211UL; }
            foreach (var b in L.Region) Mix(b);
            foreach (var v in L.Instance) { Mix((byte)v); Mix((byte)(v >> 8)); }
            foreach (var b in L.ShoreIntent) Mix(b);
            foreach (var node in L.Nodes)
            {
                foreach (var f in new[] { node.P.x, node.P.y, node.Target })
                {
                    var bits = BitConverter.GetBytes(f);
                    foreach (var b in bits) Mix(b);
                }
            }

            foreach (var pl in L.Plateaus)
            {
                foreach (var b in pl.Rim) Mix(b);
                foreach (var b in BitConverter.GetBytes(pl.EdgeTarget)) Mix(b);
            }

            return h.ToString("x16");
        }
    }

    // ---------------------------------------------------------------------- copied grid helpers (from G3c, unchanged)

    public sealed class LmcHeap
    {
        private float[] m_keys;
        private int[] m_values;

        public int Count { get; private set; }

        public LmcHeap(int capacity)
        {
            m_keys = new float[Mathf.Max(16, capacity)];
            m_values = new int[m_keys.Length];
        }

        public void Push(float key, int value)
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

        public void Pop(out float key, out int value)
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

        private static bool Less(float ka, int va, float kb, int vb) => ka < kb || (ka == kb && va < vb);
    }

    public static class LmcGrid
    {
        public static Vector2 P(int i, int n, float cell) => new Vector2((i % n) * cell, (i / n) * cell);

        public static int Index(Vector2 p, int n, float cell)
        {
            var x = Mathf.Clamp(Mathf.RoundToInt(p.x / cell), 0, n - 1);
            var z = Mathf.Clamp(Mathf.RoundToInt(p.y / cell), 0, n - 1);
            return z * n + x;
        }

        public static bool[] Rasterize(List<Vector2> pts, int n, float cell)
        {
            var mask = new bool[n * n];
            for (var k = 0; k < pts.Count; k++)
            {
                var a = pts[k];
                var b = k + 1 < pts.Count ? pts[k + 1] : a;
                var steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / (0.5f * cell)));
                for (var t = 0; t <= steps; t++) mask[Index(Vector2.Lerp(a, b, t / (float)steps), n, cell)] = true;
            }

            return mask;
        }

        /// <summary>Exact Euclidean distance (metres) to the nearest feature cell (Felzenszwalb–Huttenlocher).</summary>
        public static float[] Edt(bool[] feature, int n, float cell)
        {
            const double inf = 1e30;
            var f = new double[n * n];
            for (var i = 0; i < f.Length; i++) f[i] = feature[i] ? 0 : inf;
            var g = new double[n];
            var d = new double[n];
            var v = new int[n];
            var zz = new double[n + 1];
            for (var x = 0; x < n; x++)
            {
                for (var z = 0; z < n; z++) g[z] = f[z * n + x];
                Dt1(g, d, v, zz, n);
                for (var z = 0; z < n; z++) f[z * n + x] = d[z];
            }

            for (var z = 0; z < n; z++)
            {
                for (var x = 0; x < n; x++) g[x] = f[z * n + x];
                Dt1(g, d, v, zz, n);
                for (var x = 0; x < n; x++) f[z * n + x] = d[x];
            }

            var result = new float[n * n];
            for (var i = 0; i < f.Length; i++) result[i] = f[i] >= inf * 0.5 ? float.PositiveInfinity : (float)(Math.Sqrt(f[i]) * cell);
            return result;
        }

        private static void Dt1(double[] f, double[] d, int[] v, double[] z, int n)
        {
            var k = 0;
            v[0] = 0;
            z[0] = double.NegativeInfinity;
            z[1] = double.PositiveInfinity;
            for (var q = 1; q < n; q++)
            {
                var s = ((f[q] + (double)q * q) - (f[v[k]] + (double)v[k] * v[k])) / (2.0 * q - 2.0 * v[k]);
                while (s <= z[k])
                {
                    k--;
                    s = ((f[q] + (double)q * q) - (f[v[k]] + (double)v[k] * v[k])) / (2.0 * q - 2.0 * v[k]);
                }

                k++;
                v[k] = q;
                z[k] = s;
                z[k + 1] = double.PositiveInfinity;
            }

            k = 0;
            for (var q = 0; q < n; q++)
            {
                while (z[k + 1] < q) k++;
                d[q] = (double)(q - v[k]) * (q - v[k]) + f[v[k]];
            }
        }

        public static bool[] Close(bool[] mask, int n, float cell, float radius)
        {
            var dOut = Edt(mask, n, cell);
            var dilated = new bool[mask.Length];
            for (var i = 0; i < mask.Length; i++) dilated[i] = dOut[i] <= radius;
            var inv = new bool[mask.Length];
            for (var i = 0; i < mask.Length; i++) inv[i] = !dilated[i];
            var dIn = Edt(inv, n, cell);
            var closed = new bool[mask.Length];
            for (var i = 0; i < mask.Length; i++) closed[i] = dilated[i] && dIn[i] > radius;
            return closed;
        }

        public static bool[] LargestComponent(bool[] mask, int n)
        {
            var label = GeographyGrid.Components8(mask, n, out var count);
            if (count == 0) return mask;
            var sizes = GeographyGrid.ComponentSizes(label, count);
            var best = 0;
            for (var k = 1; k < count; k++) if (sizes[k] > sizes[best]) best = k;
            var result = new bool[mask.Length];
            for (var i = 0; i < mask.Length; i++) result[i] = label[i] == best;
            return result;
        }

        public static bool[] FillHoles(bool[] mask, int n)
        {
            var outside = new bool[mask.Length];
            var stack = new Stack<int>();
            for (var k = 0; k < n; k++)
            {
                foreach (var i in new[] { k, (n - 1) * n + k, k * n, k * n + n - 1 })
                {
                    if (!mask[i] && !outside[i]) { outside[i] = true; stack.Push(i); }
                }
            }

            while (stack.Count > 0)
            {
                var i = stack.Pop();
                var x = i % n; var z = i / n;
                if (x > 0) Visit(i - 1);
                if (x < n - 1) Visit(i + 1);
                if (z > 0) Visit(i - n);
                if (z < n - 1) Visit(i + n);
            }

            var result = new bool[mask.Length];
            for (var i = 0; i < mask.Length; i++) result[i] = mask[i] || !outside[i];
            return result;

            void Visit(int j)
            {
                if (!mask[j] && !outside[j]) { outside[j] = true; stack.Push(j); }
            }
        }

        public static float[] Geodesic(bool[] domain, List<int> sources, int n, float cell, out int[] owner, out int[] prev)
        {
            return LmcLayoutBuilder.WeightedGeodesic(domain, sources, n, cell, _ => 1f, out owner, out prev);
        }

        /// <summary>Weighted diameter of the allowed set on a coarse grid (the G3 spine rule).</summary>
        public static List<Vector2> WeightedDiameter(bool[] allowed, float[] sd, int n, float cell, int stride)
        {
            var m = (n - 1) / stride + 1;
            var valid = new bool[m * m];
            var rmax = 1f;
            for (var gz = 0; gz < m; gz++)
            for (var gx = 0; gx < m; gx++)
            {
                var i = gz * stride * n + gx * stride;
                valid[gz * m + gx] = allowed[i];
                if (allowed[i]) rmax = Mathf.Max(rmax, sd[i]);
            }

            float Cost(int g)
            {
                var gx = g % m;
                var gz = g / m;
                var r = sd[gz * stride * n + gx * stride] / rmax;
                return 1f + 6f * (1f - r) * (1f - r);
            }

            var start = -1;
            var best = float.NegativeInfinity;
            for (var g = 0; g < valid.Length; g++)
            {
                if (!valid[g]) continue;
                var v = sd[(g / m) * stride * n + (g % m) * stride];
                if (v > best) { best = v; start = g; }
            }

            if (start < 0) return new List<Vector2>();
            var step = stride * cell;
            Run(start, out var d1, out _);
            var a = Farthest(d1);
            Run(a, out var d2, out var prev);
            var b = Farthest(d2);
            var path = new List<Vector2>();
            for (var g = b; g >= 0; g = prev[g])
            {
                path.Add(new Vector2((g % m) * step, (g / m) * step));
                if (g == a) break;
            }

            var smooth = new List<Vector2>(path.Count);
            for (var k = 0; k < path.Count; k++)
            {
                var acc = Vector2.zero; var cnt = 0;
                for (var j = Mathf.Max(0, k - 3); j <= Mathf.Min(path.Count - 1, k + 3); j++) { acc += path[j]; cnt++; }
                smooth.Add(k == 0 || k == path.Count - 1 ? path[k] : acc / cnt);
            }

            return smooth;

            int Farthest(float[] dist)
            {
                var far = -1;
                var fv = float.NegativeInfinity;
                for (var g = 0; g < dist.Length; g++) if (!float.IsInfinity(dist[g]) && dist[g] > fv) { fv = dist[g]; far = g; }
                return far;
            }

            void Run(int src, out float[] dist, out int[] prevOut)
            {
                dist = new float[m * m];
                prevOut = new int[m * m];
                for (var g = 0; g < dist.Length; g++) { dist[g] = float.PositiveInfinity; prevOut[g] = -1; }
                var heap = new LmcHeap(1024);
                dist[src] = 0f;
                heap.Push(0f, src);
                while (heap.Count > 0)
                {
                    heap.Pop(out var key, out var g);
                    if (key > dist[g]) continue;
                    var gx = g % m; var gz = g / m;
                    for (var dz = -1; dz <= 1; dz++)
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        var xx = gx + dx; var zz = gz + dz;
                        if (xx < 0 || zz < 0 || xx >= m || zz >= m) continue;
                        var hh = zz * m + xx;
                        if (!valid[hh]) continue;
                        var len = (dx != 0 && dz != 0 ? 1.41421356f : 1f) * step;
                        var nd = key + len * 0.5f * (Cost(g) + Cost(hh));
                        if (nd < dist[hh]) { dist[hh] = nd; prevOut[hh] = g; heap.Push(nd, hh); }
                    }
                }
            }
        }
    }
}
#endif

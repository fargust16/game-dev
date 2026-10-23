#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using GameDev.World;
using UnityEngine;
using Random = System.Random;

namespace GameDev.EditorTools
{
    /// <summary>One G3c configuration: highland share (P2) and placement (P4).</summary>
    public sealed class G3cConfig
    {
        public string Id;
        public float HighlandShare;
        public bool OffCentre;

        public static readonly string[] All = { "s20-med", "s20-off", "s30-med", "s30-off" };

        public static G3cConfig Parse(string raw)
        {
            var id = raw.Trim();
            if (Array.IndexOf(All, id) < 0)
            {
                throw new ArgumentException($"Unknown G3c configuration '{id}'.");
            }

            return new G3cConfig
            {
                Id = id,
                HighlandShare = id.StartsWith("s30", StringComparison.Ordinal) ? 0.30f : 0.20f,
                OffCentre = id.EndsWith("off", StringComparison.Ordinal),
            };
        }
    }

    /// <summary>
    /// G3c construction values. Every value is an <b>authoring default</b> of this experiment in world units
    /// unless it names an existing project setting; none is measured, none approved.
    /// </summary>
    public sealed class G3cSettings
    {
        // regions
        public float LowlandShare = 0.30f;
        public float HighlandMinShoreDistance = 15f;
        public float HighlandClosingMeters = 20f;
        public float SpineSdWeight = 0.5f;
        public float SpineSdWeightJitter = 0.2f;   // seeded layout variation (defect D1)
        public float SpineTrimMax = 0.15f;
        public float ZoneFractionMin = 0.35f;
        public float ZoneFractionMax = 0.55f;
        public float ZoneAxisJitterDeg = 30f;
        public float OffCentrePenaltyMeters = 2000f;
        public float WaistEndExclusion = 0.2f;
        public float HighlandBufferMeters = 60f;
        public float LowlandSeedSeparationMeters = 600f;
        public float LowlandMinRoomMeters = 100f;
        public float LowlandMinRoomFallbackMeters = 60f;
        public int MaxLowlands = 3;
        public float CorridorHalfWidthMeters = 50f;
        public float OutletSeparationMeters = 400f;
        public float SecondaryReachMeters = 400f;
        public float SecondarySpacingMeters = 450f;
        public int MaxSecondaryOutlets = 4;
        public float CulminationSpacingMeters = 550f; // MassifSettings.SecondCulminationLengthMeters / 2, as in G3

        // plateaus
        public int PlateauCountMin = 2;
        public int PlateauCountMax = 4;
        public float PlateauTopMinSqm = 30_000f;
        public float PlateauTopMaxSqm = 150_000f;
        public float PlateauAspectMax = 1.6f;
        public float T2HeightMin = 18f;   // WORLD_RELIEF_PRINCIPLES.md §1 T2 row
        public float T2HeightMax = 40f;
        public float TallHeightMin = 60f;
        public float TallHeightMax = 120f;
        public float TallClassProbability = 0.5f;
        public float PlateauSeparationMeters = 500f;
        public float PlateauHighlandReachMeters = 300f;
        public float WallSlopeDeg = 75f;
        public float TalusSlopeDeg = 35f;
        public float RampSlopeDeg = 20f;
        public float WallShareOfHeight = 0.7f;
        public float WallMinWidthMeters = 8.2f; // three lattice steps: narrower walls are smoothed below 45° by the eikonal solver (defect D4)
        public float RampHalfAngleDeg = 25f;
        public float RampTurnMaxDeg = 60f;
        public float TopCost = 0.02f;              // plateau top tilt toward its ramp, rise per metre

        // rivers
        public float RiverStepMeters = 30f;
        public float SpacingLowland = 200f;
        public float SpacingUpland = 150f;
        public float SpacingHighland = 220f;       // 130 m: ridges never reached the envelope, one summit (defect D11c)
        public float RiverSlopeLowland = 0.006f;
        public float RiverSlopeUpland = 0.025f;
        public float RiverSlopeHighland = 0.14f;
        public float RiverSlopeJitter = 0.2f;
        public float BranchLowland = 0.10f;
        public float BranchUpland = 0.22f;
        public float BranchHighland = 0.30f;
        public float BranchAngleMinDeg = 50f;
        public float BranchAngleMaxDeg = 75f;
        public float DirectionJitterDeg = 12f;
        public float InlandBias = 0.5f;
        public float RiverMinShoreDistance = 25f;
        public float TrunkAttractWeight = 0.6f;
        public float AttractorReachMeters = 80f;
        public float PassApproachMeters = 60f;
        public float PassRadiusMeters = 150f;
        public int MaxRiverNodes = 40_000;
        public int InfillPasses = 16;              // tributaries started from existing nodes until the island is drained (defect D3)
        public float InfillAngleMinDeg = 55f;
        public float InfillAngleMaxDeg = 95f;
        public float ConsistencyFactor = 0.88f;    // a node may not stand above what a nearby node's valley side reaches (defect D2)
        public float ConsistencyRadiusMeters = 300f;
        public float ConsistencyMarginMeters = 0.5f;
        public float FloorRadiusLowland = 15f;
        public float FloorRadiusUpland = 6f;

        // terrain (slope = rise per metre the eikonal surface climbs away from the drainage and the shore)
        public float CostLowland = 0.035f;
        public float CostUpland = 0.10f;
        public float CostHighland = 0.8f;          // 0.62 with D11c
        public float CulminationAmplitude = 0.6f;  // 0.35 left one summit 30 m above its key col (defect D11)
        public float CulminationSigmaMeters = 170f; // 250 m left the col between culminations ~40 m down (defect D11)
        public float PassCostReduction = 0.35f;
        public float PassSigmaMeters = 150f;
        public float MassifUpliftMeters = 160f;    // the highland envelope the valleys cut into (defect D8)
        public float MassifCoreShare = 0.8f;       // the envelope reaches full height at this share of the highland's half-width
        public float UpliftBias = 1.0f;            // highland tips lean up the envelope, so valleys run down from the divide
        public float UpliftRiverShare = 0.65f;     // river floors climb this share of the envelope, so valleys deepen toward the core (D8b; 0.55 before D11c)
        public float PlateauHarmonic2Max = 0.25f;  // irregular plateau outlines (defect D13)
        public float PlateauHarmonic3Max = 0.15f;
        public float PlateauHarmonic5Max = 0.06f;
        public float WallShareSpread = 0.2f;       // uneven scarp along the rim: wall share 0.7 +- this (D13)
        public float ShoreBandSpread = 0.5f;       // shore band width 30 m x (1 +- this), varying along the coast (D14)
        public int SmoothPasses = 2;               // 3x3 smoothing of the eikonal facets away from rivers, shore and plateaus (D15)
        public float ShoreBandMeters = 30f;
        public float ShoreBandCost = 1.5f;         // cliffs where the highland or a plateau stands behind the shore
        public float RockyBandCost = 0.25f;        // rocky shore where uplands meet the sea (defect D7)
        public float CliffHinterlandMeters = 300f;
        public float OutletBeachRadiusMeters = 120f;
        public float VariationAmplitude = 0.12f;
        public float VariationWavelengthMin = 300f;
        public float VariationWavelengthMax = 600f;
        public float ShoreLevelMeters = 24.5f;

        public static G3cSettings Default => new G3cSettings();

        public IEnumerable<string> Lines()
        {
            var c = CultureInfo.InvariantCulture;
            foreach (var f in typeof(G3cSettings).GetFields())
            {
                var v = f.GetValue(this);
                yield return f.Name + " = " + (v is float fv ? fv.ToString("R", c) : Convert.ToString(v, c));
            }
        }
    }

    public sealed class G3cPlateau
    {
        public int Index;
        public Vector2 Centre;
        public float TopRadius;
        public float Aspect;
        public float Angle;
        public float A2;
        public float A3;
        public float A5;
        public float P2;
        public float P3;
        public float P5;
        public float WallPhase;
        public bool Tall;
        public float Height;
        public float RampBearing; // radians, direction the ramp leaves the top
        public float WallWidth;
        public float TalusWidth;
        public float RampLength;
        public float TopAreaSqm;
        public float NominalTop;

        /// <summary>Top boundary radius at polar angle <paramref name="theta"/> (radians, world frame).</summary>
        public float RadiusAt(float theta)
        {
            var local = theta - Angle;
            var harmonic = 1f + A2 * Mathf.Cos(2f * (local - P2)) + A3 * Mathf.Cos(3f * (local - P3)) + A5 * Mathf.Cos(5f * (local - P5));
            var ellipse = 1f / Mathf.Sqrt(Mathf.Cos(local) * Mathf.Cos(local) / (Aspect * Aspect) + Mathf.Sin(local) * Mathf.Sin(local));
            return TopRadius * harmonic * ellipse / Mathf.Sqrt(Aspect);
        }

        /// <summary>Signed distance-like offset from the top boundary along the radial direction.</summary>
        public float Offset(Vector2 p, out float theta)
        {
            var d = p - Centre;
            theta = Mathf.Atan2(d.y, d.x);
            return d.magnitude - RadiusAt(theta);
        }

        public bool InRampSector(float theta, float halfAngle)
        {
            var diff = Mathf.DeltaAngle(theta * Mathf.Rad2Deg, RampBearing * Mathf.Rad2Deg);
            return Mathf.Abs(diff) <= halfAngle;
        }
    }

    public sealed class G3cRiver
    {
        public int Id;
        public int Outlet;
        public int ParentRiver;
        public bool Trunk;
        public List<int> Nodes = new List<int>();
    }

    public sealed class G3cResult
    {
        public const byte Sea = 0;
        public const byte Lowland = 1;
        public const byte Upland = 2;
        public const byte Highland = 3;
        public const byte PlateauTop = 4;
        public const byte PlateauWall = 5;
        public const byte PlateauRamp = 6;

        public string ContourId;
        public string ConfigId;
        public int Seed;
        public int N;
        public float Cell;
        public float[] Heights;
        public byte[] Region;
        public byte[] PlateauId;
        public sbyte[] Basin;
        public bool[] HighlandMask;
        public float[] Uplift;
        public float FootFillVolume;
        public int FootFillCells;
        public List<Vector2> Spine = new List<Vector2>();
        public Vector2 Waist;
        public List<Vector2> Culminations = new List<Vector2>();
        public List<Vector2> LowlandSeeds = new List<Vector2>();
        public List<Vector2> Outlets = new List<Vector2>();
        public int PrimaryOutlets;
        public List<G3cPlateau> Plateaus = new List<G3cPlateau>();
        public List<Vector2> NodePos = new List<Vector2>();
        public List<float> NodeZ = new List<float>();
        public List<G3cRiver> Rivers = new List<G3cRiver>();
        public int End;
        public float HighlandShareActual;
        public float LowlandShareActual;
        public float UplandShareActual;
        public int PassPairA = -1;
        public int PassPairB = -1;
        public List<string> Notes = new List<string>();

        /// <summary>Why river steps were refused: outside, blocked, shore, spacing, ceiling, floor.</summary>
        public int[] AdmitFails = new int[6];

        /// <summary>Refusals by region of the refused step [region, reason] (regions 0–3).</summary>
        public int[,] AdmitFailsByRegion = new int[4, 6];
    }

    /// <summary>
    /// The G3c construction (research record §14.2). Region layout from the fixed land mask; a drainage
    /// tree grown from designated outlets; the land surface as the eikonal distance from that tree and the
    /// shore, climbing at a region-dependent slope, so ridges are the equidistant lines between valleys and
    /// no closed depression can form; plateaus as flat tops with walls, talus and a ramp where they stand
    /// above their surroundings.
    /// </summary>
    public static class G3cGeography
    {
        public static G3cResult Build(G3cContour c, G3cConfig cfg, int seed, G3cSettings s)
        {
            var n = c.N;
            var cell = c.Cell;
            var count = n * n;
            var r = new G3cResult
            {
                ContourId = c.Id,
                ConfigId = cfg.Id,
                Seed = seed,
                N = n,
                Cell = cell,
                Region = new byte[count],
                PlateauId = new byte[count],
                Basin = new sbyte[count],
            };

            var root = new GameDev.World.Seed.WorldSeed(seed);
            var mainland = c.Mainland;
            var sd = c.Sd;
            var mainlandCount = 0;
            for (var i = 0; i < count; i++)
            {
                r.Basin[i] = -1;
                if (mainland[i])
                {
                    mainlandCount++;
                }
            }

            // ---------- highland (P2 share, P4 placement) ----------
            PrincipalAxis(mainland, n, cell, out var centroid, out var axis, out var tMin, out var tMax);
            var allowed = new bool[count];
            var layoutRng = root.Fork("research.g3c.layout");
            r.End = layoutRng.NextDouble() < 0.5 ? -1 : 1;
            // Seeded layout variation (defect D1): zone axis, zone extent, spine weighting and trimmed ends.
            var axisTurn = ((float)layoutRng.NextDouble() * 2f - 1f) * s.ZoneAxisJitterDeg * Mathf.Deg2Rad;
            var zoneAxis = new Vector2(axis.x * Mathf.Cos(axisTurn) - axis.y * Mathf.Sin(axisTurn), axis.x * Mathf.Sin(axisTurn) + axis.y * Mathf.Cos(axisTurn));
            var zoneFraction = Mathf.Lerp(s.ZoneFractionMin, s.ZoneFractionMax, (float)layoutRng.NextDouble());
            var sdWeight = s.SpineSdWeight + ((float)layoutRng.NextDouble() * 2f - 1f) * s.SpineSdWeightJitter;
            var trimA = (float)layoutRng.NextDouble() * s.SpineTrimMax;
            var trimB = (float)layoutRng.NextDouble() * s.SpineTrimMax;
            var zMin = float.PositiveInfinity;
            var zMax = float.NegativeInfinity;
            for (var i = 0; i < count; i++)
            {
                if (mainland[i])
                {
                    var t = Vector2.Dot(G3cGrid.P(i, n, cell) - centroid, zoneAxis);
                    zMin = Mathf.Min(zMin, t);
                    zMax = Mathf.Max(zMax, t);
                }
            }

            var tCut = r.End > 0 ? zMax - zoneFraction * (zMax - zMin) : zMin + zoneFraction * (zMax - zMin);
            var inZone = new bool[count];
            for (var i = 0; i < count; i++)
            {
                if (!mainland[i] || sd[i] < 30f)
                {
                    continue;
                }

                var t = Vector2.Dot(G3cGrid.P(i, n, cell) - centroid, zoneAxis);
                inZone[i] = r.End > 0 ? t >= tCut : t <= tCut;
                allowed[i] = !cfg.OffCentre || inZone[i];
            }

            r.Spine = Trim(G3cGrid.WeightedDiameter(allowed, sd, n, cell, 4), trimA, trimB);
            var spineMask = G3cGrid.Rasterize(r.Spine, n, cell);
            var dSpine = G3cGrid.Edt(spineMask, n, cell);
            var target = Mathf.RoundToInt(cfg.HighlandShare * mainlandCount);
            var keys = new List<KeyValuePair<float, int>>(mainlandCount);
            for (var i = 0; i < count; i++)
            {
                if (!mainland[i] || sd[i] < s.HighlandMinShoreDistance)
                {
                    continue;
                }

                var score = dSpine[i] - sdWeight * sd[i];
                if (cfg.OffCentre && !inZone[i])
                {
                    score += s.OffCentrePenaltyMeters;
                }

                keys.Add(new KeyValuePair<float, int>(score, i));
            }

            keys.Sort((a, b) => a.Key != b.Key ? a.Key.CompareTo(b.Key) : a.Value.CompareTo(b.Value));
            var highland = new bool[count];
            for (var k = 0; k < Mathf.Min(target, keys.Count); k++)
            {
                highland[keys[k].Value] = true;
            }

            highland = G3cGrid.Close(highland, n, cell, s.HighlandClosingMeters);
            highland = G3cGrid.LargestComponent(highland, n);
            highland = G3cGrid.FillHoles(highland, n);
            for (var i = 0; i < count; i++)
            {
                highland[i] &= mainland[i];
            }

            r.HighlandMask = highland;
            var dIn = G3cGrid.Edt(Not(highland), n, cell);
            var dOut = G3cGrid.Edt(highland, n, cell);

            // Waist: the narrowest highland point on the spine, away from its ends.
            r.Waist = Waist(r.Spine, dIn, n, cell, s.WaistEndExclusion);

            // Culminations on the spine, >= CulminationSpacing apart.
            var culmRng = root.Fork("research.g3c.culmination");
            var cum = Cumulative(r.Spine);
            var total = cum.Count > 0 ? cum[cum.Count - 1] : 0f;
            // Culminations at the widest highland points of the spine (as G3, research record §11.2), seeded among
            // near-equal candidates, >= CulminationSpacing apart (defect D11).
            var culmCount = 2 + culmRng.Next(0, 2);
            var widths = new List<KeyValuePair<float, int>>();
            for (var k = 0; k < r.Spine.Count; k++)
            {
                if (cum[k] < 0.1f * total || cum[k] > 0.9f * total)
                {
                    continue;
                }

                var si = G3cGrid.Index(r.Spine[k], n, cell);
                var w = dIn[si];
                if (highland[si] && !float.IsInfinity(w) && w > 0f) // only spine points the highland covers (defect D18)
                {
                    widths.Add(new KeyValuePair<float, int>(-(w * (0.9f + 0.2f * (float)culmRng.NextDouble())), k));
                }
            }

            widths.Sort((a, b) => a.Key != b.Key ? a.Key.CompareTo(b.Key) : a.Value.CompareTo(b.Value));
            foreach (var kv in widths)
            {
                if (r.Culminations.Count >= culmCount)
                {
                    break;
                }

                var p = r.Spine[kv.Value];
                var ok = Vector2.Distance(p, r.Waist) >= 0.35f * s.CulminationSpacingMeters;
                foreach (var q in r.Culminations)
                {
                    ok &= Vector2.Distance(p, q) >= s.CulminationSpacingMeters;
                }

                if (ok)
                {
                    r.Culminations.Add(p);
                }
            }

            // Massif envelope (defect D8): rises from the highland edge to its core, scaled by the culminations
            // and lowered at the waist; river floors inside the highland climb with it, so valleys cut into it.
            var maxIn = 0f;
            for (var i = 0; i < count; i++)
            {
                if (highland[i] && !float.IsInfinity(dIn[i]))
                {
                    maxIn = Mathf.Max(maxIn, dIn[i]);
                }
            }

            var core = Mathf.Max(1f, s.MassifCoreShare * maxIn);
            r.Uplift = new float[count];
            for (var i = 0; i < count; i++)
            {
                if (highland[i])
                {
                    r.Uplift[i] = s.MassifUpliftMeters * Culmination(G3cGrid.P(i, n, cell), r, s) * Mathf.SmoothStep(0f, 1f, dIn[i] / core);
                }
            }

            // ---------- lowlands and outlets ----------
            var nonHigh = new bool[count];
            for (var i = 0; i < count; i++)
            {
                nonHigh[i] = mainland[i] && !highland[i];
            }

            var seeds = new List<int>();
            foreach (var minRoom in new[] { s.LowlandMinRoomMeters, s.LowlandMinRoomFallbackMeters })
            {
                seeds.Clear();
                var room = new List<KeyValuePair<float, int>>();
                for (var i = 0; i < count; i++)
                {
                    if (!nonHigh[i] || dOut[i] < s.HighlandBufferMeters)
                    {
                        continue;
                    }

                    var v = Mathf.Min(sd[i], dOut[i]);
                    if (v >= minRoom)
                    {
                        room.Add(new KeyValuePair<float, int>(-v, i));
                    }
                }

                // Seeded choice weighted by room squared (defect D1), in index order for determinism.
                var lowRng = root.Fork("research.g3c.lowland", (int)(minRoom * 10f));
                for (var pick = 0; pick < s.MaxLowlands; pick++)
                {
                    double totalWeight = 0;
                    foreach (var kv in room)
                    {
                        if (FarFromAll(kv.Value, seeds, n, cell, s.LowlandSeedSeparationMeters))
                        {
                            totalWeight += (double)kv.Key * kv.Key;
                        }
                    }

                    if (totalWeight <= 0)
                    {
                        break;
                    }

                    var target01 = lowRng.NextDouble() * totalWeight;
                    foreach (var kv in room)
                    {
                        if (!FarFromAll(kv.Value, seeds, n, cell, s.LowlandSeedSeparationMeters))
                        {
                            continue;
                        }

                        target01 -= (double)kv.Key * kv.Key;
                        if (target01 <= 0)
                        {
                            seeds.Add(kv.Value);
                            break;
                        }
                    }
                }

                if (seeds.Count >= 2)
                {
                    break;
                }

                r.Notes.Add($"lowland seeds with room >= {minRoom} m: {seeds.Count}");
            }

            var growDomain = new bool[count];
            for (var i = 0; i < count; i++)
            {
                growDomain[i] = nonHigh[i] && dOut[i] >= s.HighlandBufferMeters;
            }

            var geo = G3cGrid.Geodesic(growDomain, seeds, n, cell, out var owner, out _);
            var order = new List<KeyValuePair<float, int>>();
            for (var i = 0; i < count; i++)
            {
                if (owner[i] >= 0)
                {
                    order.Add(new KeyValuePair<float, int>(geo[i], i));
                }
            }

            order.Sort((a, b) => a.Key != b.Key ? a.Key.CompareTo(b.Key) : a.Value.CompareTo(b.Value));
            var lowTarget = Mathf.RoundToInt(s.LowlandShare * mainlandCount);
            for (var k = 0; k < Mathf.Min(lowTarget, order.Count); k++)
            {
                var i = order[k].Value;
                r.Basin[i] = (sbyte)owner[i];
            }

            var shore = new bool[count];
            for (var z = 1; z < n - 1; z++)
            {
                for (var x = 1; x < n - 1; x++)
                {
                    var i = z * n + x;
                    shore[i] = mainland[i] && (!c.Land[i - 1] || !c.Land[i + 1] || !c.Land[i - n] || !c.Land[i + n]);
                }
            }

            foreach (var seedCell in seeds)
            {
                r.LowlandSeeds.Add(G3cGrid.P(seedCell, n, cell));
            }

            for (var j = 0; j < seeds.Count; j++)
            {
                var dist = G3cGrid.Geodesic(nonHigh, new List<int> { seeds[j] }, n, cell, out _, out var prev);
                var best = -1;
                var bestD = float.PositiveInfinity;
                for (var i = 0; i < count; i++)
                {
                    if (!shore[i] || float.IsInfinity(dist[i]) || dist[i] >= bestD)
                    {
                        continue;
                    }

                    var p = G3cGrid.P(i, n, cell);
                    var far = true;
                    foreach (var o in r.Outlets)
                    {
                        far &= Vector2.Distance(p, o) >= s.OutletSeparationMeters;
                    }

                    if (far)
                    {
                        best = i;
                        bestD = dist[i];
                    }
                }

                if (best < 0)
                {
                    r.Notes.Add($"lowland {j}: no outlet found");
                    r.Outlets.Add(r.LowlandSeeds[j]);
                    continue;
                }

                r.Outlets.Add(G3cGrid.P(best, n, cell));
                // Corridor from the outlet back to the seed joins the lowland to its coast.
                var path = new List<Vector2>();
                for (var i = best; i >= 0; i = prev[i])
                {
                    path.Add(G3cGrid.P(i, n, cell));
                    if (i == seeds[j])
                    {
                        break;
                    }
                }

                var corridor = G3cGrid.Edt(G3cGrid.Rasterize(path, n, cell), n, cell);
                for (var i = 0; i < count; i++)
                {
                    if (nonHigh[i] && r.Basin[i] < 0 && corridor[i] <= s.CorridorHalfWidthMeters)
                    {
                        r.Basin[i] = (sbyte)j;
                    }
                }
            }

            r.PrimaryOutlets = r.Outlets.Count;

            // Secondary outlets where the highland comes near the coast, away from the primary ones.
            for (var k = 0; k < s.MaxSecondaryOutlets; k++)
            {
                var best = -1;
                var bestV = float.PositiveInfinity;
                for (var i = 0; i < count; i++)
                {
                    if (!shore[i] || dOut[i] > s.SecondaryReachMeters || dOut[i] >= bestV)
                    {
                        continue;
                    }

                    var p = G3cGrid.P(i, n, cell);
                    var far = true;
                    foreach (var o in r.Outlets)
                    {
                        far &= Vector2.Distance(p, o) >= s.SecondarySpacingMeters;
                    }

                    if (far)
                    {
                        best = i;
                        bestV = dOut[i];
                    }
                }

                if (best < 0)
                {
                    break;
                }

                r.Outlets.Add(G3cGrid.P(best, n, cell));
            }

            var lowlandCount = 0;
            var highCount = 0;
            for (var i = 0; i < count; i++)
            {
                if (!c.Land[i])
                {
                    r.Region[i] = G3cResult.Sea;
                }
                else if (highland[i])
                {
                    r.Region[i] = G3cResult.Highland;
                    highCount++;
                }
                else if (r.Basin[i] >= 0)
                {
                    r.Region[i] = G3cResult.Lowland;
                    lowlandCount++;
                }
                else
                {
                    r.Region[i] = G3cResult.Upland;
                }
            }

            r.HighlandShareActual = highCount / (float)Mathf.Max(1, mainlandCount);
            r.LowlandShareActual = lowlandCount / (float)Mathf.Max(1, mainlandCount);

            // ---------- plateaus ----------
            var plateauRng = root.Fork("research.g3c.plateau");
            var scale = c.PlateauScale;
            var plateauCount = plateauRng.Next(s.PlateauCountMin, s.PlateauCountMax + 1);
            var candidates = new List<int>();
            for (var z = 0; z < n; z += 4)
            {
                for (var x = 0; x < n; x += 4)
                {
                    var i = z * n + x;
                    if (r.Region[i] == G3cResult.Upland && dOut[i] <= s.PlateauHighlandReachMeters + 250f)
                    {
                        candidates.Add(i);
                    }
                }
            }

            for (var attempt = 0; attempt < 600 && r.Plateaus.Count < plateauCount && candidates.Count > 0; attempt++)
            {
                var ci = candidates[plateauRng.Next(candidates.Count)];
                var area = Mathf.Lerp(s.PlateauTopMinSqm, s.PlateauTopMaxSqm, (float)plateauRng.NextDouble()) * scale;
                var tall = plateauRng.NextDouble() < s.TallClassProbability;
                var height = tall
                    ? Mathf.Lerp(s.TallHeightMin, s.TallHeightMax, (float)plateauRng.NextDouble())
                    : Mathf.Lerp(s.T2HeightMin, s.T2HeightMax, (float)plateauRng.NextDouble());
                var pl = new G3cPlateau
                {
                    Index = r.Plateaus.Count + 1,
                    Centre = G3cGrid.P(ci, n, cell),
                    TopRadius = Mathf.Sqrt(area / Mathf.PI),
                    Aspect = Mathf.Lerp(1f, s.PlateauAspectMax, (float)plateauRng.NextDouble()),
                    Angle = (float)(plateauRng.NextDouble() * Math.PI),
                    A2 = s.PlateauHarmonic2Max * (float)plateauRng.NextDouble(),
                    A3 = s.PlateauHarmonic3Max * (float)plateauRng.NextDouble(),
                    A5 = s.PlateauHarmonic5Max * (float)plateauRng.NextDouble(),
                    P2 = (float)(plateauRng.NextDouble() * Math.PI),
                    P3 = (float)(plateauRng.NextDouble() * Math.PI),
                    P5 = (float)(plateauRng.NextDouble() * Math.PI),
                    WallPhase = (float)(plateauRng.NextDouble() * Math.PI * 2),
                    Tall = tall,
                    Height = height,
                    TopAreaSqm = area,
                };
                var turn = ((float)plateauRng.NextDouble() * 2f - 1f) * s.RampTurnMaxDeg * Mathf.Deg2Rad;
                var dirAway = AwayFromHighland(pl.Centre, dOut, n, cell);
                pl.RampBearing = Mathf.Atan2(dirAway.y, dirAway.x) + turn;
                pl.WallWidth = Mathf.Max(s.WallMinWidthMeters, s.WallShareOfHeight * height / Mathf.Tan(s.WallSlopeDeg * Mathf.Deg2Rad));
                pl.TalusWidth = (1f - s.WallShareOfHeight) * height / Mathf.Tan(s.TalusSlopeDeg * Mathf.Deg2Rad);
                pl.RampLength = height / Mathf.Tan(s.RampSlopeDeg * Mathf.Deg2Rad);

                if (!PlateauFits(pl, r, c, dOut, s, out var why))
                {
                    continue;
                }

                r.Plateaus.Add(pl);
            }

            if (r.Plateaus.Count < plateauCount)
            {
                r.Notes.Add($"plateaus placed {r.Plateaus.Count} of {plateauCount}");
            }

            // Footprints (top + rings + ramp) block the drainage tree.
            var blocked = new bool[count];
            foreach (var pl in r.Plateaus)
            {
                Stamp(pl, n, cell, (i, offset, theta) =>
                {
                    var reach = Mathf.Max(pl.RampLength, pl.WallWidth + pl.TalusWidth + 1.5f * pl.Height);
                    if (offset <= reach + 30f)
                    {
                        blocked[i] = true;
                    }
                }, pl.RampLength + 1.5f * pl.Height + 35f);
            }

            // ---------- cost field (read by the tree's height bounds, so built first) ----------
            var variation = Variation(root.Fork("research.g3c.variation"), s);
            var shoreWave = Wave(root.Fork("research.g3c.shoreband"), 150f, 400f);
            var cost = new float[count];
            for (var i = 0; i < count; i++)
            {
                if (!c.Land[i])
                {
                    continue;
                }

                var p = G3cGrid.P(i, n, cell);
                float k;
                switch (r.Region[i])
                {
                    case G3cResult.Lowland:
                        k = s.CostLowland;
                        break;
                    case G3cResult.Highland:
                        k = s.CostHighland * Culmination(p, r, s);
                        break;
                    default:
                        k = s.CostUpland;
                        break;
                }

                if (!mainland[i])
                {
                    k = s.CostUpland;
                }

                if (NearOutlet(p, r.Outlets, s.OutletBeachRadiusMeters))
                {
                    k = Mathf.Min(k, s.CostLowland); // a low mouth plain at every designated outlet (defect D5)
                }
                else if (sd[i] < s.ShoreBandMeters * (1f + s.ShoreBandSpread * shoreWave(p)) && r.Region[i] != G3cResult.Lowland)
                {
                    // Cliffs where the highland or a plateau stands behind the shore, rocky shores before uplands (D7).
                    var cliffCoast = dOut[i] <= s.CliffHinterlandMeters || NearPlateau(p, r.Plateaus, 200f);
                    k = Mathf.Max(k, cliffCoast ? s.ShoreBandCost : s.RockyBandCost);
                }

                cost[i] = k * variation(p);
            }

            // The surface the shore alone would give: no river floor may stand above it (defect D2).
            var hShore = G3cMarch.Solve(cost, c.Land, Sources(r, c, s), n, cell);

            // ---------- drainage tree ----------
            G3cRivers.Grow(r, c, highland, blocked, dOut, cost, hShore, s, root);

            // ---------- surface ----------
            var sources = Sources(r, c, s);
            var h = G3cMarch.Solve(cost, c.Land, sources, n, cell);

            // Highland envelope cap (defect D8b): inside the highland the surface is the lower of the eikonal
            // relief and the foot surface carried flat under the massif plus the envelope; both have no local
            // minimum, so neither has their lower envelope. Valleys cut below the envelope, ridges meet it.
            var footSrc = new float[count];
            for (var i = 0; i < count; i++)
            {
                footSrc[i] = float.NaN;
                if (!highland[i])
                {
                    continue;
                }

                var x = i % n;
                var z = i / n;
                var edge = (x > 0 && !highland[i - 1]) || (x < n - 1 && !highland[i + 1]) || (z > 0 && !highland[i - n]) || (z < n - 1 && !highland[i + n]);
                if (edge)
                {
                    footSrc[i] = h[i];
                }
            }

            // The foot is the harmonic interpolation of the highland's edge values (defect D17: an eikonal foot
            // carried the lowest edge point across the whole highland and cut a trench along its high edges).
            var footSurface = G3cGrid.Harmonic(highland, footSrc, n);

            // River floors may not stand above the cap (defect D16): clamp each river, mouth to head, to the cap
            // while keeping it rising upstream, then solve the surface again from the clamped floors.
            var capAt = new float[count];
            for (var i = 0; i < count; i++)
            {
                capAt[i] = highland[i] ? footSurface[i] + r.Uplift[i] : float.PositiveInfinity;
            }

            if (ClampRivers(r, capAt, n, cell, s.ConsistencyMarginMeters) > 0)
            {
                sources = Sources(r, c, s);
                h = G3cMarch.Solve(cost, c.Land, sources, n, cell);
                for (var i = 0; i < count; i++)
                {
                    if (!float.IsNaN(footSrc[i]))
                    {
                        footSrc[i] = h[i];
                    }
                }

                footSurface = G3cGrid.Harmonic(highland, footSrc, n);
            }

            for (var i = 0; i < count; i++)
            {
                if (highland[i] && !float.IsNaN(footSurface[i]) && float.IsNaN(sources[i]))
                {
                    h[i] = Mathf.Min(h[i], footSurface[i] + r.Uplift[i]); // river floors were clamped above
                }
            }

            // Light smoothing of the eikonal facets (defect D15); river floors and the shore keep their values.
            for (var pass = 0; pass < s.SmoothPasses; pass++)
            {
                var copy = (float[])h.Clone();
                for (var z = 1; z < n - 1; z++)
                {
                    for (var x = 1; x < n - 1; x++)
                    {
                        var i = z * n + x;
                        if (!c.Land[i] || !float.IsNaN(sources[i]))
                        {
                            continue;
                        }

                        var sum = 0f;
                        var k = 0;
                        for (var dz = -1; dz <= 1; dz++)
                        {
                            for (var dx = -1; dx <= 1; dx++)
                            {
                                var j = i + dz * n + dx;
                                if (c.Land[j])
                                {
                                    sum += copy[j];
                                    k++;
                                }
                            }
                        }

                        h[i] = sum / k;
                    }
                }
            }

            // ---------- plateaus composed over the surface (defect D6) ----------
            // A top tilted toward its ramp, a wall and talus falling away from its rim, a ramp in its sector;
            // the plateau shows only where it stands above the surface, so its uphill side meets the ground.
            var rampTan = Mathf.Tan(s.RampSlopeDeg * Mathf.Deg2Rad);
            var skirtTan = Mathf.Tan(s.TalusSlopeDeg * Mathf.Deg2Rad);
            foreach (var pl in r.Plateaus)
            {
                // The ramp leaves the rim toward the lowest ground at ramp length, so it always descends (defect D9).
                var bestTheta = pl.RampBearing;
                var bestH = float.PositiveInfinity;
                for (var deg = 0; deg < 360; deg += 5)
                {
                    var th = deg * Mathf.Deg2Rad;
                    var foot = pl.Centre + new Vector2(Mathf.Cos(th), Mathf.Sin(th)) * (pl.RadiusAt(th) + pl.RampLength);
                    var fi = G3cGrid.Index(foot, n, cell);
                    if (!c.Land[fi] || foot.x < 0f || foot.y < 0f || foot.x > (n - 1) * cell || foot.y > (n - 1) * cell)
                    {
                        continue;
                    }

                    if (h[fi] < bestH)
                    {
                        bestH = h[fi];
                        bestTheta = th;
                    }
                }

                var rampRng = root.Fork("research.g3c.ramp", pl.Index);
                pl.RampBearing = bestTheta + ((float)rampRng.NextDouble() * 2f - 1f) * 15f * Mathf.Deg2Rad;
                pl.NominalTop = h[G3cGrid.Index(pl.Centre, n, cell)] + pl.Height;
                var rampVec = new Vector2(Mathf.Cos(pl.RampBearing), Mathf.Sin(pl.RampBearing));
                var id = (byte)pl.Index;
                Stamp(pl, n, cell, (i, offset, theta) =>
                {
                    if (!c.Land[i] || !float.IsNaN(sources[i]))
                    {
                        return; // river floors and the shore keep their values
                    }

                    var p = G3cGrid.P(i, n, cell);
                    var top = pl.NominalTop - s.TopCost * Vector2.Dot(p - pl.Centre, rampVec);
                    float v;
                    byte kind;
                    if (offset <= 0f)
                    {
                        v = top;
                        kind = G3cResult.PlateauTop;
                    }
                    else if (pl.InRampSector(theta, s.RampHalfAngleDeg))
                    {
                        v = top - offset * rampTan;
                        kind = G3cResult.PlateauRamp;
                    }
                    else
                    {
                        var ww = pl.WallWidth;
                        var wt = pl.TalusWidth;
                        var height = pl.Height;
                        var ws = Mathf.Clamp(s.WallShareOfHeight + s.WallShareSpread * Mathf.Cos(3f * theta + pl.WallPhase), 0.3f, 0.95f); // uneven scarp (D13)
                        float drop;
                        if (offset <= ww)
                        {
                            drop = ws * height * offset / ww;
                        }
                        else if (offset <= ww + wt)
                        {
                            drop = ws * height + (1f - ws) * height * (offset - ww) / wt;
                        }
                        else
                        {
                            drop = height + (offset - ww - wt) * skirtTan;
                        }

                        v = top - drop;
                        kind = G3cResult.PlateauWall;
                    }

                    if (v > h[i])
                    {
                        h[i] = v;
                        r.Region[i] = kind;
                        r.PlateauId[i] = id;
                    }
                }, pl.RampLength + 3f * pl.Height + 60f);
            }

            // Plateau feet (defect D12): where the composed plateau traps water against rising ground, fill the
            // trough to its spill level, as talus would; the filled volume is reported, never hidden.
            if (r.Plateaus.Count > 0)
            {
                var zone = new bool[count];
                foreach (var pl in r.Plateaus)
                {
                    Stamp(pl, n, cell, (i, offset, theta) => zone[i] = true, pl.RampLength + 3f * pl.Height + 90f);
                }

                var grid = new GameDev.World.Hydrology.HydrologyGrid(0f, 0f, n, n, cell, (float[])h.Clone(), WorldScale.SeaLevelMeters);
                for (var i = 0; i < count; i++)
                {
                    if (!c.Land[i])
                    {
                        grid.Heights[i] = c.Ocean[i];
                    }
                }

                var oceanMask = grid.BuildOceanMask();
                var filled = GameDev.World.Hydrology.DepressionFill.Fill(grid, oceanMask);
                for (var i = 0; i < count; i++)
                {
                    if (zone[i] && c.Land[i] && filled[i] > h[i])
                    {
                        r.FootFillVolume += (filled[i] - h[i]) * cell * cell;
                        r.FootFillCells++;
                        h[i] = filled[i];
                    }
                }
            }

            r.Heights = new float[count];
            var ocean = c.Ocean;
            var upCount = 0;
            for (var i = 0; i < count; i++)
            {
                if (c.Land[i])
                {
                    r.Heights[i] = Mathf.Max(s.ShoreLevelMeters, h[i]);
                    if (r.Region[i] == G3cResult.Upland)
                    {
                        upCount++;
                    }
                }
                else
                {
                    r.Heights[i] = ocean[i];
                }
            }

            r.UplandShareActual = upCount / (float)Mathf.Max(1, mainlandCount);
            return r;
        }

        // ---------- helpers ----------

        /// <summary>
        /// Lowers river nodes to at most <paramref name="cap"/> − margin, walking each river from its mouth or
        /// junction upstream so floors keep rising. Returns the number of nodes changed.
        /// </summary>
        private static int ClampRivers(G3cResult r, float[] cap, int n, float cell, float margin)
        {
            var order = new List<G3cRiver>(r.Rivers);
            order.Sort((a, b) =>
            {
                var za = r.NodeZ[a.Nodes[0]];
                var zb = r.NodeZ[b.Nodes[0]];
                return za != zb ? za.CompareTo(zb) : a.Id.CompareTo(b.Id);
            });

            var changed = 0;
            foreach (var river in order)
            {
                for (var k = 1; k < river.Nodes.Count; k++)
                {
                    var id = river.Nodes[k];
                    var below = r.NodeZ[river.Nodes[k - 1]];
                    var limit = cap[G3cGrid.Index(r.NodePos[id], n, cell)] - margin;
                    var z = Mathf.Max(below + 0.02f, Mathf.Min(r.NodeZ[id], limit));
                    if (z != r.NodeZ[id])
                    {
                        r.NodeZ[id] = z;
                        changed++;
                    }
                }
            }

            return changed;
        }

        private static bool FarFromAll(int i, List<int> seeds, int n, float cell, float separation)
        {
            var p = G3cGrid.P(i, n, cell);
            foreach (var q in seeds)
            {
                if (Vector2.Distance(p, G3cGrid.P(q, n, cell)) < separation)
                {
                    return false;
                }
            }

            return true;
        }

        private static List<Vector2> Trim(List<Vector2> pts, float fromStart, float fromEnd)
        {
            if (pts.Count < 4)
            {
                return pts;
            }

            var cum = Cumulative(pts);
            var total = cum[cum.Count - 1];
            var kept = new List<Vector2>();
            for (var k = 0; k < pts.Count; k++)
            {
                if (cum[k] >= fromStart * total && cum[k] <= (1f - fromEnd) * total)
                {
                    kept.Add(pts[k]);
                }
            }

            return kept.Count >= 2 ? kept : pts;
        }

        private static bool[] Not(bool[] a)
        {
            var b = new bool[a.Length];
            for (var i = 0; i < a.Length; i++)
            {
                b[i] = !a[i];
            }

            return b;
        }

        private static void PrincipalAxis(bool[] mask, int n, float cell, out Vector2 centroid, out Vector2 axis, out float tMin, out float tMax)
        {
            double sx = 0, sz = 0, cnt = 0;
            for (var i = 0; i < mask.Length; i++)
            {
                if (!mask[i])
                {
                    continue;
                }

                var p = G3cGrid.P(i, n, cell);
                sx += p.x;
                sz += p.y;
                cnt++;
            }

            centroid = new Vector2((float)(sx / cnt), (float)(sz / cnt));
            double cxx = 0, czz = 0, cxz = 0;
            for (var i = 0; i < mask.Length; i++)
            {
                if (!mask[i])
                {
                    continue;
                }

                var d = G3cGrid.P(i, n, cell) - centroid;
                cxx += d.x * d.x;
                czz += d.y * d.y;
                cxz += d.x * d.y;
            }

            var angle = 0.5 * Math.Atan2(2 * cxz, cxx - czz);
            axis = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            tMin = float.PositiveInfinity;
            tMax = float.NegativeInfinity;
            for (var i = 0; i < mask.Length; i++)
            {
                if (!mask[i])
                {
                    continue;
                }

                var t = Vector2.Dot(G3cGrid.P(i, n, cell) - centroid, axis);
                tMin = Mathf.Min(tMin, t);
                tMax = Mathf.Max(tMax, t);
            }
        }

        private static Vector2 Waist(List<Vector2> spine, float[] dIn, int n, float cell, float exclusion)
        {
            if (spine.Count == 0)
            {
                return Vector2.zero;
            }

            var cum = Cumulative(spine);
            var total = cum[cum.Count - 1];
            var best = spine[spine.Count / 2];
            var bestV = float.PositiveInfinity;
            for (var k = 0; k < spine.Count; k++)
            {
                if (cum[k] < exclusion * total || cum[k] > (1f - exclusion) * total)
                {
                    continue;
                }

                var v = dIn[G3cGrid.Index(spine[k], n, cell)];
                if (v > 0f && v < bestV)
                {
                    bestV = v;
                    best = spine[k];
                }
            }

            return best;
        }

        public static List<float> Cumulative(List<Vector2> pts)
        {
            var cum = new List<float>(pts.Count);
            var acc = 0f;
            for (var k = 0; k < pts.Count; k++)
            {
                if (k > 0)
                {
                    acc += Vector2.Distance(pts[k - 1], pts[k]);
                }

                cum.Add(acc);
            }

            return cum;
        }

        public static Vector2 At(List<Vector2> pts, List<float> cum, float arc)
        {
            for (var k = 1; k < pts.Count; k++)
            {
                if (cum[k] >= arc)
                {
                    var t = (arc - cum[k - 1]) / Mathf.Max(1e-4f, cum[k] - cum[k - 1]);
                    return Vector2.Lerp(pts[k - 1], pts[k], t);
                }
            }

            return pts[pts.Count - 1];
        }

        private static Vector2 AwayFromHighland(Vector2 p, float[] dOut, int n, float cell)
        {
            var i = G3cGrid.Index(p, n, cell);
            var x = i % n;
            var z = i / n;
            var xp = Mathf.Min(n - 1, x + 2);
            var xm = Mathf.Max(0, x - 2);
            var zp = Mathf.Min(n - 1, z + 2);
            var zm = Mathf.Max(0, z - 2);
            var g = new Vector2(dOut[z * n + xp] - dOut[z * n + xm], dOut[zp * n + x] - dOut[zm * n + x]);
            return g.sqrMagnitude > 1e-6f ? g.normalized : Vector2.right;
        }

        private static bool PlateauFits(G3cPlateau pl, G3cResult r, G3cContour c, float[] dOut, G3cSettings s, out string why)
        {
            var n = c.N;
            var cell = c.Cell;
            var ci = G3cGrid.Index(pl.Centre, n, cell);
            if (c.Sd[ci] < 0.6f * pl.TopRadius)
            {
                why = "too near the coast";
                return false;
            }

            if (dOut[ci] < 0.5f * pl.TopRadius || dOut[ci] > pl.TopRadius + s.PlateauHighlandReachMeters)
            {
                why = "not beside the highland";
                return false;
            }

            foreach (var other in r.Plateaus)
            {
                var need = Mathf.Max(s.PlateauSeparationMeters, 1.6f * (other.TopRadius + pl.TopRadius));
                if (Vector2.Distance(other.Centre, pl.Centre) < need)
                {
                    why = "too near another plateau";
                    return false;
                }
            }

            int total = 0, low = 0, high = 0, sea = 0;
            foreach (var o in r.Outlets)
            {
                if (Vector2.Distance(o, pl.Centre) < pl.TopRadius * 1.6f + pl.RampLength)
                {
                    why = "on an outlet";
                    return false;
                }
            }

            Stamp(pl, n, cell, (i, offset, theta) =>
            {
                if (offset > pl.WallWidth + pl.TalusWidth)
                {
                    return;
                }

                total++;
                if (!c.Land[i])
                {
                    sea++;
                }
                else if (r.Region[i] == G3cResult.Lowland)
                {
                    low++;
                }
                else if (r.Region[i] == G3cResult.Highland)
                {
                    high++;
                }
            }, pl.WallWidth + pl.TalusWidth + 5f);

            if (sea > 0 || low > 0.05f * total || high > 0.4f * total)
            {
                why = "footprint on sea, lowland or deep in the highland";
                return false;
            }

            why = "";
            return true;
        }

        /// <summary>Calls <paramref name="visit"/> for every cell within the plateau's top radius + <paramref name="margin"/>.</summary>
        public static void Stamp(G3cPlateau pl, int n, float cell, Action<int, float, float> visit, float margin)
        {
            var reach = pl.TopRadius * 1.9f + margin;
            var x0 = Mathf.Max(0, Mathf.FloorToInt((pl.Centre.x - reach) / cell));
            var x1 = Mathf.Min(n - 1, Mathf.CeilToInt((pl.Centre.x + reach) / cell));
            var z0 = Mathf.Max(0, Mathf.FloorToInt((pl.Centre.y - reach) / cell));
            var z1 = Mathf.Min(n - 1, Mathf.CeilToInt((pl.Centre.y + reach) / cell));
            for (var z = z0; z <= z1; z++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    var p = new Vector2(x * cell, z * cell);
                    var offset = pl.Offset(p, out var theta);
                    if (offset <= margin)
                    {
                        visit(z * n + x, offset, theta);
                    }
                }
            }
        }

        private static float Culmination(Vector2 p, G3cResult r, G3cSettings s)
        {
            var m = 1f;
            var s2 = s.CulminationSigmaMeters * s.CulminationSigmaMeters;
            foreach (var q in r.Culminations)
            {
                m += s.CulminationAmplitude * Mathf.Exp(-(p - q).sqrMagnitude / s2);
            }

            var w2 = s.PassSigmaMeters * s.PassSigmaMeters;
            m -= s.PassCostReduction * Mathf.Exp(-(p - r.Waist).sqrMagnitude / w2);
            return m;
        }

        private static bool NearPlateau(Vector2 p, List<G3cPlateau> plateaus, float margin)
        {
            foreach (var pl in plateaus)
            {
                var reach = pl.TopRadius * 1.9f + margin;
                if ((pl.Centre - p).sqrMagnitude <= reach * reach)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool NearOutlet(Vector2 p, List<Vector2> outlets, float radius)
        {
            foreach (var o in outlets)
            {
                if ((o - p).sqrMagnitude <= radius * radius)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>A smooth seeded wave in [-1, 1]: two plane waves with wavelengths between the bounds.</summary>
        private static Func<Vector2, float> Wave(Random rng, float lMin, float lMax)
        {
            var a1 = (float)(rng.NextDouble() * Math.PI * 2);
            var a2 = (float)(rng.NextDouble() * Math.PI * 2);
            var l1 = Mathf.Lerp(lMin, lMax, (float)rng.NextDouble());
            var l2 = Mathf.Lerp(lMin, lMax, (float)rng.NextDouble());
            var p1 = (float)(rng.NextDouble() * Math.PI * 2);
            var p2 = (float)(rng.NextDouble() * Math.PI * 2);
            var d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
            var d2 = new Vector2(Mathf.Cos(a2), Mathf.Sin(a2));
            return p => 0.5f * (Mathf.Cos(2f * Mathf.PI * Vector2.Dot(p, d1) / l1 + p1) + Mathf.Cos(2f * Mathf.PI * Vector2.Dot(p, d2) / l2 + p2));
        }

        private static Func<Vector2, float> Variation(Random rng, G3cSettings s)
        {
            var a1 = (float)(rng.NextDouble() * Math.PI * 2);
            var a2 = (float)(rng.NextDouble() * Math.PI * 2);
            var l1 = Mathf.Lerp(s.VariationWavelengthMin, s.VariationWavelengthMax, (float)rng.NextDouble());
            var l2 = Mathf.Lerp(s.VariationWavelengthMin, s.VariationWavelengthMax, (float)rng.NextDouble());
            var p1 = (float)(rng.NextDouble() * Math.PI * 2);
            var p2 = (float)(rng.NextDouble() * Math.PI * 2);
            var d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
            var d2 = new Vector2(Mathf.Cos(a2), Mathf.Sin(a2));
            var amp = s.VariationAmplitude;
            return p => 1f + amp * 0.5f * (Mathf.Cos(2f * Mathf.PI * Vector2.Dot(p, d1) / l1 + p1) + Mathf.Cos(2f * Mathf.PI * Vector2.Dot(p, d2) / l2 + p2));
        }

        /// <summary>River lines (with floor widths) and the shoreline as eikonal sources.</summary>
        private static float[] Sources(G3cResult r, G3cContour c, G3cSettings s)
        {
            var n = c.N;
            var cell = c.Cell;
            var src = new float[n * n];
            for (var i = 0; i < src.Length; i++)
            {
                src[i] = float.NaN;
            }

            void Set(int i, float v)
            {
                if (i < 0 || i >= src.Length || !c.Land[i])
                {
                    return;
                }

                if (float.IsNaN(src[i]) || v < src[i])
                {
                    src[i] = v;
                }
            }

            foreach (var river in r.Rivers)
            {
                for (var k = 1; k < river.Nodes.Count; k++)
                {
                    var a = r.NodePos[river.Nodes[k - 1]];
                    var b = r.NodePos[river.Nodes[k]];
                    var za = r.NodeZ[river.Nodes[k - 1]];
                    var zb = r.NodeZ[river.Nodes[k]];
                    var steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / (0.5f * cell)));
                    for (var t = 0; t <= steps; t++)
                    {
                        var u = t / (float)steps;
                        var p = Vector2.Lerp(a, b, u);
                        var z = Mathf.Lerp(za, zb, u);
                        var ci = G3cGrid.Index(p, n, cell);
                        var region = r.Region[ci];
                        var radius = region == G3cResult.Lowland ? s.FloorRadiusLowland : region == G3cResult.Upland ? s.FloorRadiusUpland : 0f;
                        if (radius <= 0f)
                        {
                            Set(ci, z);
                            continue;
                        }

                        var rc = Mathf.CeilToInt(radius / cell);
                        var cx = ci % n;
                        var cz = ci / n;
                        for (var dz = -rc; dz <= rc; dz++)
                        {
                            for (var dx = -rc; dx <= rc; dx++)
                            {
                                var x = cx + dx;
                                var zz = cz + dz;
                                if (x < 0 || zz < 0 || x >= n || zz >= n)
                                {
                                    continue;
                                }

                                if ((dx * dx + dz * dz) * cell * cell <= radius * radius)
                                {
                                    Set(zz * n + x, z);
                                }
                            }
                        }
                    }
                }
            }

            for (var z = 1; z < n - 1; z++)
            {
                for (var x = 1; x < n - 1; x++)
                {
                    var i = z * n + x;
                    if (!c.Land[i])
                    {
                        continue;
                    }

                    var coast = false;
                    for (var dz = -1; dz <= 1 && !coast; dz++)
                    {
                        for (var dx = -1; dx <= 1; dx++)
                        {
                            if (!c.Land[i + dz * n + dx])
                            {
                                coast = true;
                                break;
                            }
                        }
                    }

                    if (coast)
                    {
                        Set(i, s.ShoreLevelMeters);
                    }
                }
            }

            return src;
        }
    }

    /// <summary>The drainage tree, grown from the designated outlets (research record §14.2).</summary>
    public static class G3cRivers
    {
        private sealed class Tip
        {
            public int River;
            public int Node;
            public Vector2 Dir;
            public float Slope;
            public List<Vector2> Attractors = new List<Vector2>();
            public bool Pass;
            public long Order;
        }

        public static void Grow(G3cResult r, G3cContour c, bool[] highland, bool[] blocked, float[] dOut, float[] cost, float[] hShore, G3cSettings s, GameDev.World.Seed.WorldSeed root)
        {
            var n = c.N;
            var cell = c.Cell;
            var buckets = new Dictionary<long, List<int>>();
            const float bucket = 50f;
            var nodeRiver = new List<int>();
            var nodeSeq = new List<int>();
            var heap = new SortedSet<(float z, long order)>();
            var tips = new Dictionary<long, Tip>();
            long serial = 0;
            var rng = root.Fork("research.g3c.rivers");

            void AddNode(Vector2 p, float z, int river)
            {
                var id = r.NodePos.Count;
                r.NodePos.Add(p);
                r.NodeZ.Add(z);
                nodeRiver.Add(river);
                nodeSeq.Add(r.Rivers[river].Nodes.Count);
                r.Rivers[river].Nodes.Add(id);
                var key = Key(Mathf.FloorToInt(p.x / bucket), Mathf.FloorToInt(p.y / bucket));
                if (!buckets.TryGetValue(key, out var list))
                {
                    list = new List<int>();
                    buckets[key] = list;
                }

                list.Add(id);
            }

            long Key(int kx, int kz) => ((long)kx << 32) ^ (uint)kz;

            void Push(Tip t)
            {
                t.Order = serial++;
                heap.Add((r.NodeZ[t.Node], t.Order));
                tips[t.Order] = t;
            }

            byte RegionAt(Vector2 p) => r.Region[G3cGrid.Index(p, n, cell)];

            float Spacing(byte region) => region == G3cResult.Lowland ? s.SpacingLowland : region == G3cResult.Highland ? s.SpacingHighland : s.SpacingUpland;

            float SlopeOf(byte region) => region == G3cResult.Lowland ? s.RiverSlopeLowland : region == G3cResult.Highland ? s.RiverSlopeHighland : s.RiverSlopeUpland;

            float Branch(byte region) => region == G3cResult.Lowland ? s.BranchLowland : region == G3cResult.Highland ? s.BranchHighland : s.BranchUpland;

            // The surface's rise between two points along a straight line: the cost field integrated every ~10 m.
            float PathCost(Vector2 a, Vector2 b)
            {
                var d = Vector2.Distance(a, b);
                var steps = Mathf.Max(1, Mathf.CeilToInt(d / 10f));
                var acc = 0f;
                for (var k = 0; k <= steps; k++)
                {
                    acc += cost[G3cGrid.Index(Vector2.Lerp(a, b, k / (float)steps), n, cell)];
                }

                return acc / (steps + 1) * d;
            }

            Vector2 Inland(Vector2 p)
            {
                var i = G3cGrid.Index(p, n, cell);
                var x = i % n;
                var z = i / n;
                var xp = Mathf.Min(n - 1, x + 2);
                var xm = Mathf.Max(0, x - 2);
                var zp = Mathf.Min(n - 1, z + 2);
                var zm = Mathf.Max(0, z - 2);
                var g = new Vector2(c.Sd[z * n + xp] - c.Sd[z * n + xm], c.Sd[zp * n + x] - c.Sd[zm * n + x]);
                return g.sqrMagnitude > 1e-6f ? g.normalized : Vector2.zero;
            }

            float UpAt(Vector2 p) => r.Uplift == null ? 0f : r.Uplift[G3cGrid.Index(p, n, cell)];

            Vector2 UpliftGrad(Vector2 p)
            {
                if (r.Uplift == null)
                {
                    return Vector2.zero;
                }

                var i = G3cGrid.Index(p, n, cell);
                var x = i % n;
                var z = i / n;
                var xp = Mathf.Min(n - 1, x + 2);
                var xm = Mathf.Max(0, x - 2);
                var zp = Mathf.Min(n - 1, z + 2);
                var zm = Mathf.Max(0, z - 2);
                var g = new Vector2(r.Uplift[z * n + xp] - r.Uplift[z * n + xm], r.Uplift[zp * n + x] - r.Uplift[zm * n + x]);
                return g.sqrMagnitude > 1e-6f ? g.normalized : Vector2.zero;
            }

            bool Excluded(int river, int seq, int id)
            {
                var other = nodeRiver[id];
                if (other == river && nodeSeq[id] >= seq - 6)
                {
                    return true;
                }

                // A tributary needs about spacing / (step · sin 37°) steps to clear its parent; until then the parent's
                // nodes near the junction do not count against it (defect D10).
                var parent = r.Rivers[river].ParentRiver;
                if (other != parent)
                {
                    return false;
                }

                var junction = r.NodePos[r.Rivers[river].Nodes[0]];
                var window = Mathf.CeilToInt(Spacing(RegionAt(junction)) / (0.6f * s.RiverStepMeters)) + 1;
                return seq <= window && Vector2.Distance(r.NodePos[id], junction) <= window * s.RiverStepMeters;
            }

            // Spacing, land, blocking and the height-consistency bounds (defect D2) for a new node of `river`.
            bool Admit(int river, bool pass, Vector2 q, float zFrom, float desired, out float z)
            {
                z = 0f;
                if (q.x < 0f || q.y < 0f || q.x > (n - 1) * cell || q.y > (n - 1) * cell)
                {
                    r.AdmitFails[0]++;
                    return false;
                }

                var i = G3cGrid.Index(q, n, cell);
                if (!c.Mainland[i] || blocked[i])
                {
                    r.AdmitFails[c.Mainland[i] ? 1 : 0]++;
                    return false;
                }

                var seq = r.Rivers[river].Nodes.Count;
                if (c.Sd[i] < s.RiverMinShoreDistance && seq > 3)
                {
                    r.AdmitFails[2]++;
                    return false;
                }

                var region = RegionAt(q);
                var spacing = Spacing(region);
                var hi = hShore[i];
                var lo = float.NegativeInfinity;
                var radius = Mathf.Max(spacing, s.ConsistencyRadiusMeters);
                var rb = Mathf.CeilToInt(radius / bucket);
                var kx = Mathf.FloorToInt(q.x / bucket);
                var kz = Mathf.FloorToInt(q.y / bucket);
                for (var dz = -rb; dz <= rb; dz++)
                {
                    for (var dx = -rb; dx <= rb; dx++)
                    {
                        if (!buckets.TryGetValue(Key(kx + dx, kz + dz), out var list))
                        {
                            continue;
                        }

                        foreach (var id in list)
                        {
                            if (Excluded(river, seq, id))
                            {
                                continue;
                            }

                            var other = nodeRiver[id];
                            var d = Vector2.Distance(r.NodePos[id], q);
                            var need = other == river ? 0.8f * spacing : spacing;
                            if (pass && r.Rivers[other].Trunk && Vector2.Distance(q, r.Waist) <= s.PassRadiusMeters)
                            {
                                need = Mathf.Min(need, s.PassApproachMeters);
                            }

                            if (d < need)
                            {
                                r.AdmitFails[3]++;
                                r.AdmitFailsByRegion[Mathf.Min(3, (int)region), 3]++;
                                return false;
                            }

                            if (d <= s.ConsistencyRadiusMeters)
                            {
                                var rise = s.ConsistencyFactor * PathCost(r.NodePos[id], q);
                                hi = Mathf.Min(hi, r.NodeZ[id] + rise);
                                lo = Mathf.Max(lo, r.NodeZ[id] - rise);
                            }
                        }
                    }
                }

                z = Mathf.Min(desired, hi - s.ConsistencyMarginMeters);
                if (z < zFrom + 0.02f)
                {
                    r.AdmitFails[4]++;
                    r.AdmitFailsByRegion[Mathf.Min(3, (int)region), 4]++;
                    return false;
                }

                if (z < lo + s.ConsistencyMarginMeters)
                {
                    r.AdmitFails[5]++;
                    r.AdmitFailsByRegion[Mathf.Min(3, (int)region), 5]++;
                    return false;
                }

                return true;
            }

            int NewRiver(int outlet, int parent, bool trunk)
            {
                var river = new G3cRiver { Id = r.Rivers.Count, Outlet = outlet, ParentRiver = parent, Trunk = trunk };
                r.Rivers.Add(river);
                return river.Id;
            }

            // Pass pair: the two primary outlets whose lowland seeds lie most opposite around the waist.
            if (r.PrimaryOutlets >= 2)
            {
                var best = -2f;
                for (var a = 0; a < r.PrimaryOutlets && a < r.LowlandSeeds.Count; a++)
                {
                    for (var b = a + 1; b < r.PrimaryOutlets && b < r.LowlandSeeds.Count; b++)
                    {
                        var va = (r.LowlandSeeds[a] - r.Waist).normalized;
                        var vb = (r.LowlandSeeds[b] - r.Waist).normalized;
                        var score = -Vector2.Dot(va, vb);
                        if (score > best)
                        {
                            best = score;
                            r.PassPairA = a;
                            r.PassPairB = b;
                        }
                    }
                }
            }

            for (var o = 0; o < r.Outlets.Count; o++)
            {
                var river = NewRiver(o, -1, true);
                var p = r.Outlets[o];
                AddNode(p, s.ShoreLevelMeters, river);
                var tip = new Tip
                {
                    River = river,
                    Node = r.Rivers[river].Nodes[0],
                    Dir = Inland(p),
                    Slope = 1f + s.RiverSlopeJitter * ((float)rng.NextDouble() * 2f - 1f),
                };
                if (tip.Dir == Vector2.zero)
                {
                    tip.Dir = Vector2.up;
                }

                if (o < r.PrimaryOutlets && o < r.LowlandSeeds.Count)
                {
                    tip.Attractors.Add(r.LowlandSeeds[o]);
                }

                if (o == r.PassPairA || o == r.PassPairB)
                {
                    tip.Attractors.Add(r.Waist);
                    tip.Pass = true;
                }
                else if (r.Spine.Count > 0)
                {
                    tip.Attractors.Add(Nearest(r.Spine, p));
                }

                Push(tip);
            }

            var rotations = new[] { 0f, 15f, -15f, 30f, -30f, 45f, -45f, 60f, -60f };

            void Extend()
            {
                while (heap.Count > 0 && r.NodePos.Count < s.MaxRiverNodes)
                {
                    var first = heap.Min;
                    heap.Remove(first);
                    var t = tips[first.order];
                    tips.Remove(first.order);
                    var p = r.NodePos[t.Node];
                    var z = r.NodeZ[t.Node];

                    while (t.Attractors.Count > 0 && Vector2.Distance(p, t.Attractors[0]) <= s.AttractorReachMeters)
                    {
                        t.Attractors.RemoveAt(0);
                        if (t.Attractors.Count == 0)
                        {
                            t.Pass = false;
                        }
                    }

                    Vector2 baseDir;
                    if (t.Attractors.Count > 0)
                    {
                        var toward = (t.Attractors[0] - p).normalized;
                        baseDir = (s.TrunkAttractWeight * toward + (1f - s.TrunkAttractWeight) * t.Dir).normalized;
                    }
                    else if (RegionAt(p) == G3cResult.Highland)
                    {
                        baseDir = (t.Dir + s.UpliftBias * UpliftGrad(p)).normalized; // valleys run down from the divide (D8)
                    }
                    else
                    {
                        baseDir = (t.Dir + s.InlandBias * Inland(p)).normalized;
                    }

                    if (baseDir == Vector2.zero)
                    {
                        baseDir = t.Dir;
                    }

                    var jitter = ((float)rng.NextDouble() * 2f - 1f) * s.DirectionJitterDeg;
                    var flip = rng.NextDouble() < 0.5 ? 1f : -1f;
                    var moved = false;
                    foreach (var rot in rotations)
                    {
                        var d = Rotate(baseDir, flip * rot + jitter);
                        var q = p + d * s.RiverStepMeters;
                        var region = q.x >= 0f && q.y >= 0f && q.x <= (n - 1) * cell && q.y <= (n - 1) * cell ? RegionAt(q) : G3cResult.Sea;
                        var desired = z + SlopeOf(region) * t.Slope * s.RiverStepMeters + s.UpliftRiverShare * Mathf.Max(0f, UpAt(q) - UpAt(p));
                        if (!Admit(t.River, t.Pass, q, z, desired, out var nz))
                        {
                            continue;
                        }

                        AddNode(q, nz, t.River);
                        var nodeId = r.NodePos.Count - 1;
                        t.Node = nodeId;
                        t.Dir = d;
                        moved = true;

                        if (rng.NextDouble() < Branch(region))
                        {
                            var angle = Mathf.Lerp(s.BranchAngleMinDeg, s.BranchAngleMaxDeg, (float)rng.NextDouble()) * (rng.NextDouble() < 0.5 ? 1f : -1f);
                            var child = NewRiver(r.Rivers[t.River].Outlet, t.River, false);
                            r.Rivers[child].Nodes.Add(nodeId);
                            Push(new Tip
                            {
                                River = child,
                                Node = nodeId,
                                Dir = Rotate(d, angle),
                                Slope = 1f + s.RiverSlopeJitter * ((float)rng.NextDouble() * 2f - 1f),
                            });
                        }

                        break;
                    }

                    if (moved)
                    {
                        Push(t);
                    }
                }
            }

            Extend();

            // Infill (defect D3): start tributaries from existing nodes, lowest first, until nothing fits.
            for (var pass = 0; pass < s.InfillPasses && r.NodePos.Count < s.MaxRiverNodes; pass++)
            {
                var order = new List<int>(r.NodePos.Count);
                for (var k = 0; k < r.NodePos.Count; k++)
                {
                    order.Add(k);
                }

                order.Sort((a, b) => r.NodeZ[a] != r.NodeZ[b] ? r.NodeZ[a].CompareTo(r.NodeZ[b]) : a.CompareTo(b));
                var started = 0;
                foreach (var k in order)
                {
                    if (r.NodePos.Count >= s.MaxRiverNodes)
                    {
                        break;
                    }

                    var river = nodeRiver[k];
                    var seq = nodeSeq[k];
                    var p = r.NodePos[k];
                    Vector2 along;
                    if (seq > 0)
                    {
                        along = p - r.NodePos[r.Rivers[river].Nodes[seq - 1]];
                    }
                    else
                    {
                        along = Inland(p);
                    }

                    if (along.sqrMagnitude < 1e-6f)
                    {
                        along = Vector2.up;
                    }

                    along.Normalize();
                    var angle = Mathf.Lerp(s.InfillAngleMinDeg, s.InfillAngleMaxDeg, (float)rng.NextDouble());
                    var side = rng.NextDouble() < 0.5 ? 1f : -1f;
                    for (var attempt = 0; attempt < 2; attempt++, side = -side)
                    {
                        var d = Rotate(along, side * angle);
                        var q = p + d * s.RiverStepMeters;
                        if (q.x < 0f || q.y < 0f || q.x > (n - 1) * cell || q.y > (n - 1) * cell)
                        {
                            continue;
                        }

                        var child = NewRiver(r.Rivers[river].Outlet, river, false);
                        r.Rivers[child].Nodes.Add(k);
                        var slope = 1f + s.RiverSlopeJitter * ((float)rng.NextDouble() * 2f - 1f);
                        var desired = r.NodeZ[k] + SlopeOf(RegionAt(q)) * slope * s.RiverStepMeters + s.UpliftRiverShare * Mathf.Max(0f, UpAt(q) - UpAt(p));
                        if (!Admit(child, false, q, r.NodeZ[k], desired, out var nz))
                        {
                            r.Rivers.RemoveAt(r.Rivers.Count - 1);
                            continue;
                        }

                        AddNode(q, nz, child);
                        Push(new Tip { River = child, Node = r.NodePos.Count - 1, Dir = d, Slope = slope });
                        started++;
                        break;
                    }
                }

                if (started == 0)
                {
                    break;
                }

                Extend();
            }

            // Drop rivers that never grew past their junction.
            r.Rivers.RemoveAll(river => river.Nodes.Count < 2);
        }

        private static Vector2 Nearest(List<Vector2> pts, Vector2 p)
        {
            var best = pts[0];
            var bestD = float.PositiveInfinity;
            foreach (var q in pts)
            {
                var d = (q - p).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = q;
                }
            }

            return best;
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            var a = degrees * Mathf.Deg2Rad;
            var cs = Mathf.Cos(a);
            var sn = Mathf.Sin(a);
            return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
        }
    }

    /// <summary>First-order fast marching on the lattice: |grad T| = cost, T = source value at sources.</summary>
    public static class G3cMarch
    {
        public static float[] Solve(float[] cost, bool[] domain, float[] source, int n, float cell)
        {
            var count = n * n;
            var t = new float[count];
            var state = new byte[count]; // 0 far, 1 trial, 2 known
            var heap = new G3cHeap(count / 8);
            for (var i = 0; i < count; i++)
            {
                t[i] = float.PositiveInfinity;
                if (domain[i] && !float.IsNaN(source[i]))
                {
                    t[i] = source[i];
                    state[i] = 1;
                    heap.Push(t[i], i);
                }
            }

            while (heap.Count > 0)
            {
                heap.Pop(out var key, out var i);
                if (state[i] == 2 || key > t[i])
                {
                    continue;
                }

                state[i] = 2;
                var x = i % n;
                var z = i / n;
                Relax(x - 1, z);
                Relax(x + 1, z);
                Relax(x, z - 1);
                Relax(x, z + 1);
            }

            return t;

            void Relax(int x, int z)
            {
                if (x < 0 || z < 0 || x >= n || z >= n)
                {
                    return;
                }

                var j = z * n + x;
                if (!domain[j] || state[j] == 2 || !float.IsNaN(source[j]))
                {
                    return; // sources are fixed values (defect D2: a river must keep its designed floor)
                }

                var a = Math.Min(KnownAt(x - 1, z), KnownAt(x + 1, z));
                var b = Math.Min(KnownAt(x, z - 1), KnownAt(x, z + 1));
                var f = (double)cost[j] * cell;
                double v;
                if (double.IsInfinity(a) && double.IsInfinity(b))
                {
                    return;
                }

                if (double.IsInfinity(a) || double.IsInfinity(b) || Math.Abs(a - b) >= f)
                {
                    v = Math.Min(a, b) + f;
                }
                else
                {
                    v = 0.5 * (a + b + Math.Sqrt(2 * f * f - (a - b) * (a - b)));
                }

                if (v < t[j])
                {
                    t[j] = (float)v;
                    state[j] = 1;
                    heap.Push(t[j], j);
                }
            }

            double KnownAt(int x, int z)
            {
                if (x < 0 || z < 0 || x >= n || z >= n)
                {
                    return double.PositiveInfinity;
                }

                var j = z * n + x;
                return state[j] == 2 ? t[j] : double.PositiveInfinity;
            }
        }
    }

    public sealed class G3cHeap
    {
        private float[] m_keys;
        private int[] m_values;

        public int Count { get; private set; }

        public G3cHeap(int capacity)
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
                if (Less(m_keys[parent], m_values[parent], key, value))
                {
                    break;
                }

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
            if (Count == 0)
            {
                return;
            }

            var lastKey = m_keys[Count];
            var lastValue = m_values[Count];
            var i = 0;
            while (true)
            {
                var child = 2 * i + 1;
                if (child >= Count)
                {
                    break;
                }

                if (child + 1 < Count && Less(m_keys[child + 1], m_values[child + 1], m_keys[child], m_values[child]))
                {
                    child++;
                }

                if (Less(lastKey, lastValue, m_keys[child], m_values[child]))
                {
                    break;
                }

                m_keys[i] = m_keys[child];
                m_values[i] = m_values[child];
                i = child;
            }

            m_keys[i] = lastKey;
            m_values[i] = lastValue;
        }

        // Ties broken by value so the order is fully deterministic.
        private static bool Less(float ka, int va, float kb, int vb) => ka < kb || (ka == kb && va < vb);
    }

    /// <summary>Lattice utilities for G3c: distances, components, paths.</summary>
    public static class G3cGrid
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
                for (var t = 0; t <= steps; t++)
                {
                    mask[Index(Vector2.Lerp(a, b, t / (float)steps), n, cell)] = true;
                }
            }

            return mask;
        }

        /// <summary>Exact Euclidean distance (metres) to the nearest feature cell (Felzenszwalb–Huttenlocher).</summary>
        public static float[] Edt(bool[] feature, int n, float cell)
        {
            const double inf = 1e30;
            var f = new double[n * n];
            for (var i = 0; i < f.Length; i++)
            {
                f[i] = feature[i] ? 0 : inf;
            }

            var g = new double[n];
            var d = new double[n];
            var v = new int[n];
            var zz = new double[n + 1];
            for (var x = 0; x < n; x++)
            {
                for (var z = 0; z < n; z++)
                {
                    g[z] = f[z * n + x];
                }

                Dt1(g, d, v, zz, n);
                for (var z = 0; z < n; z++)
                {
                    f[z * n + x] = d[z];
                }
            }

            for (var z = 0; z < n; z++)
            {
                for (var x = 0; x < n; x++)
                {
                    g[x] = f[z * n + x];
                }

                Dt1(g, d, v, zz, n);
                for (var x = 0; x < n; x++)
                {
                    f[z * n + x] = d[x];
                }
            }

            var result = new float[n * n];
            for (var i = 0; i < f.Length; i++)
            {
                result[i] = f[i] >= inf * 0.5 ? float.PositiveInfinity : (float)(Math.Sqrt(f[i]) * cell);
            }

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
                while (z[k + 1] < q)
                {
                    k++;
                }

                d[q] = (double)(q - v[k]) * (q - v[k]) + f[v[k]];
            }
        }

        public static bool[] Close(bool[] mask, int n, float cell, float radius)
        {
            var dOut = Edt(mask, n, cell);
            var dilated = new bool[mask.Length];
            for (var i = 0; i < mask.Length; i++)
            {
                dilated[i] = dOut[i] <= radius;
            }

            var inv = new bool[mask.Length];
            for (var i = 0; i < mask.Length; i++)
            {
                inv[i] = !dilated[i];
            }

            var dIn = Edt(inv, n, cell);
            var closed = new bool[mask.Length];
            for (var i = 0; i < mask.Length; i++)
            {
                closed[i] = dilated[i] && dIn[i] > radius;
            }

            return closed;
        }

        public static bool[] LargestComponent(bool[] mask, int n)
        {
            var label = GeographyGrid.Components8(mask, n, out var count);
            if (count == 0)
            {
                return mask;
            }

            var sizes = GeographyGrid.ComponentSizes(label, count);
            var best = 0;
            for (var k = 1; k < count; k++)
            {
                if (sizes[k] > sizes[best])
                {
                    best = k;
                }
            }

            var result = new bool[mask.Length];
            for (var i = 0; i < mask.Length; i++)
            {
                result[i] = label[i] == best;
            }

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
                    if (!mask[i] && !outside[i])
                    {
                        outside[i] = true;
                        stack.Push(i);
                    }
                }
            }

            while (stack.Count > 0)
            {
                var i = stack.Pop();
                var x = i % n;
                var z = i / n;
                if (x > 0) Visit(i - 1);
                if (x < n - 1) Visit(i + 1);
                if (z > 0) Visit(i - n);
                if (z < n - 1) Visit(i + n);
            }

            var result = new bool[mask.Length];
            for (var i = 0; i < mask.Length; i++)
            {
                result[i] = mask[i] || !outside[i];
            }

            return result;

            void Visit(int j)
            {
                if (!mask[j] && !outside[j])
                {
                    outside[j] = true;
                    stack.Push(j);
                }
            }
        }

        /// <summary>
        /// Harmonic interpolation inside <paramref name="domain"/> of the values given at its edge cells (non-NaN
        /// entries of <paramref name="edge"/>), by successive over-relaxation; NaN outside the domain.
        /// </summary>
        public static float[] Harmonic(bool[] domain, float[] edge, int n)
        {
            var count = n * n;
            var v = new float[count];
            double sum = 0;
            var k = 0;
            for (var i = 0; i < count; i++)
            {
                v[i] = float.NaN;
                if (domain[i] && !float.IsNaN(edge[i]))
                {
                    sum += edge[i];
                    k++;
                }
            }

            if (k == 0)
            {
                return v;
            }

            var mean = (float)(sum / k);
            var interior = new List<int>();
            for (var i = 0; i < count; i++)
            {
                if (!domain[i])
                {
                    continue;
                }

                if (!float.IsNaN(edge[i]))
                {
                    v[i] = edge[i];
                }
                else
                {
                    v[i] = mean;
                    interior.Add(i);
                }
            }

            const float omega = 1.9f;
            for (var iteration = 0; iteration < 3000; iteration++)
            {
                var maxChange = 0f;
                foreach (var i in interior)
                {
                    var x = i % n;
                    var z = i / n;
                    var acc = 0f;
                    var cnt = 0;
                    if (x > 0 && domain[i - 1]) { acc += v[i - 1]; cnt++; }
                    if (x < n - 1 && domain[i + 1]) { acc += v[i + 1]; cnt++; }
                    if (z > 0 && domain[i - n]) { acc += v[i - n]; cnt++; }
                    if (z < n - 1 && domain[i + n]) { acc += v[i + n]; cnt++; }
                    if (cnt == 0)
                    {
                        continue;
                    }

                    var target = acc / cnt;
                    var next = v[i] + omega * (target - v[i]);
                    maxChange = Mathf.Max(maxChange, Mathf.Abs(next - v[i]));
                    v[i] = next;
                }

                if (maxChange < 0.005f)
                {
                    break;
                }
            }

            return v;
        }

        /// <summary>Multi-source 8-connected geodesic distance inside <paramref name="domain"/>.</summary>
        public static float[] Geodesic(bool[] domain, List<int> sources, int n, float cell, out int[] owner, out int[] prev)
        {
            var count = n * n;
            var dist = new float[count];
            owner = new int[count];
            prev = new int[count];
            for (var i = 0; i < count; i++)
            {
                dist[i] = float.PositiveInfinity;
                owner[i] = -1;
                prev[i] = -1;
            }

            var heap = new G3cHeap(1024);
            for (var k = 0; k < sources.Count; k++)
            {
                var i = sources[k];
                dist[i] = 0f;
                owner[i] = k;
                heap.Push(0f, i);
            }

            var diag = cell * 1.41421356f;
            while (heap.Count > 0)
            {
                heap.Pop(out var key, out var i);
                if (key > dist[i])
                {
                    continue;
                }

                var x = i % n;
                var z = i / n;
                for (var dz = -1; dz <= 1; dz++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0)
                        {
                            continue;
                        }

                        var xx = x + dx;
                        var zz = z + dz;
                        if (xx < 0 || zz < 0 || xx >= n || zz >= n)
                        {
                            continue;
                        }

                        var j = zz * n + xx;
                        if (!domain[j])
                        {
                            continue;
                        }

                        var nd = key + (dx != 0 && dz != 0 ? diag : cell);
                        if (nd < dist[j])
                        {
                            dist[j] = nd;
                            owner[j] = owner[i];
                            prev[j] = i;
                            heap.Push(nd, j);
                        }
                    }
                }
            }

            return dist;
        }

        /// <summary>Weighted diameter of the allowed set on a coarse grid (the G3 spine rule, §11.2).</summary>
        public static List<Vector2> WeightedDiameter(bool[] allowed, float[] sd, int n, float cell, int stride)
        {
            var m = (n - 1) / stride + 1;
            var valid = new bool[m * m];
            var rmax = 1f;
            for (var gz = 0; gz < m; gz++)
            {
                for (var gx = 0; gx < m; gx++)
                {
                    var i = gz * stride * n + gx * stride;
                    valid[gz * m + gx] = allowed[i];
                    if (allowed[i])
                    {
                        rmax = Mathf.Max(rmax, sd[i]);
                    }
                }
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
                if (valid[g])
                {
                    var gx = g % m;
                    var gz = g / m;
                    var v = sd[gz * stride * n + gx * stride];
                    if (v > best)
                    {
                        best = v;
                        start = g;
                    }
                }
            }

            if (start < 0)
            {
                return new List<Vector2>();
            }

            var step = stride * cell;
            Run(start, out var d1, out _);
            var a = Farthest(d1);
            Run(a, out var d2, out var prev);
            var b = Farthest(d2);
            var path = new List<Vector2>();
            for (var g = b; g >= 0; g = prev[g])
            {
                path.Add(new Vector2((g % m) * step, (g / m) * step));
                if (g == a)
                {
                    break;
                }
            }

            // Moving average +-3 points, ends fixed.
            var smooth = new List<Vector2>(path.Count);
            for (var k = 0; k < path.Count; k++)
            {
                var acc = Vector2.zero;
                var cnt = 0;
                for (var j = Mathf.Max(0, k - 3); j <= Mathf.Min(path.Count - 1, k + 3); j++)
                {
                    acc += path[j];
                    cnt++;
                }

                smooth.Add(k == 0 || k == path.Count - 1 ? path[k] : acc / cnt);
            }

            return smooth;

            int Farthest(float[] dist)
            {
                var far = -1;
                var fv = float.NegativeInfinity;
                for (var g = 0; g < dist.Length; g++)
                {
                    if (!float.IsInfinity(dist[g]) && dist[g] > fv)
                    {
                        fv = dist[g];
                        far = g;
                    }
                }

                return far;
            }

            void Run(int src, out float[] dist, out int[] prevOut)
            {
                dist = new float[m * m];
                prevOut = new int[m * m];
                for (var g = 0; g < dist.Length; g++)
                {
                    dist[g] = float.PositiveInfinity;
                    prevOut[g] = -1;
                }

                var heap = new G3cHeap(1024);
                dist[src] = 0f;
                heap.Push(0f, src);
                while (heap.Count > 0)
                {
                    heap.Pop(out var key, out var g);
                    if (key > dist[g])
                    {
                        continue;
                    }

                    var gx = g % m;
                    var gz = g / m;
                    for (var dz = -1; dz <= 1; dz++)
                    {
                        for (var dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dz == 0)
                            {
                                continue;
                            }

                            var xx = gx + dx;
                            var zz = gz + dz;
                            if (xx < 0 || zz < 0 || xx >= m || zz >= m)
                            {
                                continue;
                            }

                            var h = zz * m + xx;
                            if (!valid[h])
                            {
                                continue;
                            }

                            var len = (dx != 0 && dz != 0 ? 1.41421356f : 1f) * step;
                            var nd = key + len * 0.5f * (Cost(g) + Cost(h));
                            if (nd < dist[h])
                            {
                                dist[h] = nd;
                                prevOut[h] = g;
                                heap.Push(nd, h);
                            }
                        }
                    }
                }
            }
        }
    }
}
#endif

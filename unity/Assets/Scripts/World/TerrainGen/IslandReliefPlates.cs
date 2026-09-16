using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>One macro plate as Stage 4A reads it.</summary>
    public sealed class ReliefPlate
    {
        public int Index { get; internal set; }
        public Vector2 Site { get; internal set; }

        /// <summary>Land inside the plate's own Voronoi cell, square metres.</summary>
        public float OwnLandAreaSqm { get; internal set; }

        /// <summary>Coastline whose segment midpoints lie in the plate's own cell, metres.</summary>
        public float OwnFrontageMeters { get; internal set; }

        /// <summary>True when the plate holds enough land to be a district of its own.</summary>
        public bool IsDistrict { get; internal set; }

        /// <summary>The district this plate is read as: itself, or the one a sliver was merged into.</summary>
        public int EffectiveIndex { get; internal set; }

        /// <summary>District land including merged slivers; zero for a merged plate.</summary>
        public float LandAreaSqm { get; internal set; }

        /// <summary>District frontage including merged slivers; zero for a merged plate.</summary>
        public float FrontageMeters { get; internal set; }

        /// <summary>Net overriding length at convergent boundaries (or land area under the craton rule).</summary>
        public float Score { get; internal set; }

        /// <summary>Rank among districts, 0 = lowest; -1 for a merged plate.</summary>
        public int Rank { get; internal set; }

        /// <summary>The level Λ this plate stands at, in [0, 1]; a merged plate carries its district's.</summary>
        public float Level { get; internal set; }
    }

    /// <summary>One macro boundary as Stage 4A scores it.</summary>
    public sealed class ReliefBoundary
    {
        public int Index { get; internal set; }
        public int PlateA { get; internal set; }
        public int PlateB { get; internal set; }
        public float Convergence { get; internal set; }
        public float LengthMeters { get; internal set; }
        public float OnLandLengthMeters { get; internal set; }

        /// <summary>Counted towards the plate scores.</summary>
        public bool Scored { get; internal set; }

        /// <summary>District that overrides at this boundary, or -1 when not scored.</summary>
        public int UpperPlate { get; internal set; }

        /// <summary>District that is overridden, or -1 when not scored.</summary>
        public int LowerPlate { get; internal set; }
    }

    /// <summary>An on-land junction of at least three districts, where the level field relaxes.</summary>
    public sealed class ReliefJunction
    {
        /// <summary>Index into <see cref="MacroPlates.Junctions"/>.</summary>
        public int Index { get; internal set; }

        public Vector2 Position { get; internal set; }

        /// <summary>Distinct districts meeting here, ascending.</summary>
        public int[] Plates { get; internal set; }

        /// <summary>Mean level of those districts: what the field relaxes towards.</summary>
        public float MeanLevel { get; internal set; }
    }

    public sealed partial class IslandRelief
    {
        /// <summary>Raster the plate land areas are measured on, metres. Matches the partition raster.</summary>
        private const float PlateLandRasterMeters = 10f;

        /// <summary>Step the on-land length of a boundary is sampled at, metres.</summary>
        private const float BoundarySampleMeters = 5f;

        public IReadOnlyList<ReliefPlate> Plates => plates;

        public IReadOnlyList<ReliefBoundary> Boundaries => boundaries;

        public IReadOnlyList<ReliefJunction> Junctions => junctions;

        /// <summary>Districts, ascending by plate index.</summary>
        public int DistrictCount { get; private set; }

        /// <summary>True when levels came from the craton rule because no convergent boundary scored.</summary>
        public bool UsedCratonRule { get; private set; }

        private ReliefPlate[] plates = Array.Empty<ReliefPlate>();
        private ReliefBoundary[] boundaries = Array.Empty<ReliefBoundary>();
        private ReliefJunction[] junctions = Array.Empty<ReliefJunction>();

        // Evaluation data: plate sites, the level each carries, and 1 / (2 |p_k − p_j|) per ordered pair.
        private double[] siteX = Array.Empty<double>();
        private double[] siteZ = Array.Empty<double>();
        private float[] plateLevel = Array.Empty<float>();
        private double[] inverseTwoSpan = Array.Empty<double>();
        private bool anyLevel;

        private void BuildPlates(Stage1Result stage1, MacroPlates macro)
        {
            var count = macro != null ? macro.PlateCount : 0;
            plates = new ReliefPlate[count];
            for (var k = 0; k < count; k++)
            {
                plates[k] = new ReliefPlate { Index = k, Site = macro.Sites[k], EffectiveIndex = k, Rank = -1 };
            }

            if (count == 0)
            {
                return;
            }

            MeasureLand(stage1.Coast, macro);
            MeasureFrontage(stage1.Coast, macro);
            MeasureBoundaries(stage1.Coast, macro);
            MergeSlivers(macro);
            AssignLevels();
            BuildJunctions(stage1.Coast, macro);

            siteX = new double[count];
            siteZ = new double[count];
            plateLevel = new float[count];
            inverseTwoSpan = new double[count * count];
            for (var k = 0; k < count; k++)
            {
                siteX[k] = macro.Sites[k].x;
                siteZ[k] = macro.Sites[k].y;
                plateLevel[k] = plates[k].Level;
                anyLevel |= plateLevel[k] > 0f;
            }

            for (var k = 0; k < count; k++)
            {
                for (var j = 0; j < count; j++)
                {
                    if (j == k)
                    {
                        continue;
                    }

                    var dx = siteX[j] - siteX[k];
                    var dz = siteZ[j] - siteZ[k];
                    var span = Math.Sqrt(dx * dx + dz * dz);
                    inverseTwoSpan[k * count + j] = span > 1e-3 ? 1.0 / (2.0 * span) : 0.0;
                }
            }
        }

        /// <summary>
        /// Land per plate on a 10 m raster: the coastline's own scanline predicate, the plates' own
        /// nearest-site rule.
        /// </summary>
        private void MeasureLand(CoastQuery coast, MacroPlates macro)
        {
            var cells = Mathf.RoundToInt(WorldScale.WorldExtentXMeters / PlateLandRasterMeters);
            var rows = Mathf.RoundToInt(WorldScale.WorldExtentZMeters / PlateLandRasterMeters);
            var cellArea = PlateLandRasterMeters * PlateLandRasterMeters;
            var crossings = new List<float>(16);
            var area = new double[plates.Length];

            for (var z = 0; z < rows; z++)
            {
                var worldZ = (z + 0.5f) * PlateLandRasterMeters;
                coast.RowCrossings(worldZ, crossings);
                for (var x = 0; x < cells; x++)
                {
                    var worldX = (x + 0.5f) * PlateLandRasterMeters;
                    if (CoastQuery.InsideFromSortedCrossings(crossings, worldX))
                    {
                        area[macro.PlateAt(worldX, worldZ)] += cellArea;
                    }
                }
            }

            for (var k = 0; k < plates.Length; k++)
            {
                plates[k].OwnLandAreaSqm = (float)area[k];
            }
        }

        private void MeasureFrontage(CoastQuery coast, MacroPlates macro)
        {
            var frontage = new double[plates.Length];
            for (var r = 0; r < coast.Rings.Count; r++)
            {
                var ring = coast.Rings[r];
                if (ring == null || ring.Length < 2)
                {
                    continue;
                }

                for (var i = 0; i < ring.Length; i++)
                {
                    var a = ring[i];
                    var b = ring[(i + 1) % ring.Length];
                    var mid = (a + b) * 0.5f;
                    frontage[macro.PlateAt(mid.x, mid.y)] += (b - a).magnitude;
                }
            }

            for (var k = 0; k < plates.Length; k++)
            {
                plates[k].OwnFrontageMeters = (float)frontage[k];
            }
        }

        private void MeasureBoundaries(CoastQuery coast, MacroPlates macro)
        {
            boundaries = new ReliefBoundary[macro.Boundaries.Length];
            var crossings = new List<float>(16);

            for (var e = 0; e < macro.Boundaries.Length; e++)
            {
                var b = macro.Boundaries[e];
                var steps = Mathf.Max(1, Mathf.CeilToInt(b.Length / BoundarySampleMeters));
                var stepLength = b.Length / steps;
                var inside = 0;
                for (var i = 0; i < steps; i++)
                {
                    var p = Vector2.LerpUnclamped(b.A, b.B, (i + 0.5f) / steps);
                    coast.RowCrossings(p.y, crossings);
                    if (CoastQuery.InsideFromSortedCrossings(crossings, p.x))
                    {
                        inside++;
                    }
                }

                boundaries[e] = new ReliefBoundary
                {
                    Index = e,
                    PlateA = b.PlateA,
                    PlateB = b.PlateB,
                    Convergence = b.Convergence,
                    LengthMeters = b.Length,
                    OnLandLengthMeters = inside * stepLength,
                    UpperPlate = -1,
                    LowerPlate = -1,
                };
            }
        }

        /// <summary>
        /// Plates with too little land become part of the neighbouring district they share the most land
        /// boundary with.
        /// </summary>
        /// <remarks>
        /// A sliver is an accident of where a partition bisector happened to clip the coast, not a
        /// district. Plates with no land at all are read the same way so the level field near the coast
        /// takes the level of the land it touches. Fallbacks, in order, when no boundary to a district
        /// crosses land: the longest boundary to a district, then the nearest district site.
        /// </remarks>
        private void MergeSlivers(MacroPlates macro)
        {
            for (var k = 0; k < plates.Length; k++)
            {
                plates[k].IsDistrict = plates[k].OwnLandAreaSqm >= Settings.MinPlateLandAreaSqm;
            }

            for (var k = 0; k < plates.Length; k++)
            {
                if (plates[k].IsDistrict)
                {
                    plates[k].EffectiveIndex = k;
                    continue;
                }

                var best = -1;
                var bestLand = -1f;
                var bestLength = -1f;
                for (var e = 0; e < boundaries.Length; e++)
                {
                    var b = boundaries[e];
                    int other;
                    if (b.PlateA == k)
                    {
                        other = b.PlateB;
                    }
                    else if (b.PlateB == k)
                    {
                        other = b.PlateA;
                    }
                    else
                    {
                        continue;
                    }

                    if (!plates[other].IsDistrict)
                    {
                        continue;
                    }

                    var better = b.OnLandLengthMeters > bestLand
                                 || (b.OnLandLengthMeters == bestLand && b.LengthMeters > bestLength)
                                 || (b.OnLandLengthMeters == bestLand && b.LengthMeters == bestLength && other < best);
                    if (better)
                    {
                        best = other;
                        bestLand = b.OnLandLengthMeters;
                        bestLength = b.LengthMeters;
                    }
                }

                if (best < 0)
                {
                    var bestSq = float.PositiveInfinity;
                    for (var j = 0; j < plates.Length; j++)
                    {
                        if (!plates[j].IsDistrict)
                        {
                            continue;
                        }

                        var sq = (macro.Sites[j] - macro.Sites[k]).sqrMagnitude;
                        if (sq < bestSq)
                        {
                            bestSq = sq;
                            best = j;
                        }
                    }
                }

                plates[k].EffectiveIndex = best >= 0 ? best : k;
            }

            for (var k = 0; k < plates.Length; k++)
            {
                if (plates[k].IsDistrict)
                {
                    plates[k].LandAreaSqm += plates[k].OwnLandAreaSqm;
                    plates[k].FrontageMeters += plates[k].OwnFrontageMeters;
                    DistrictCount++;
                    continue;
                }

                var target = plates[k].EffectiveIndex;
                if (target != k)
                {
                    plates[target].LandAreaSqm += plates[k].OwnLandAreaSqm;
                    plates[target].FrontageMeters += plates[k].OwnFrontageMeters;
                }
            }
        }

        /// <summary>
        /// Levels from each district's role in the island's collision, ranked and normalised. No RNG.
        /// </summary>
        /// <remarks>
        /// <para>
        /// At every convergent boundary that crosses enough land, the district with more land overrides
        /// (buoyant continental crust rides up) and gains the boundary's on-land length; the other loses
        /// it. Ties go to the lower plate index. With no scoring boundary the craton rule applies: the
        /// district with the most land stands highest.
        /// </para>
        /// <para>
        /// Ranking rather than scaling by score is what guarantees an island with two or more districts
        /// always has both a lowland (level 0) and an upland (level 1). A single district has nothing to
        /// stand above, so every level is 0 and the plate term vanishes.
        /// </para>
        /// </remarks>
        private void AssignLevels()
        {
            var score = new double[plates.Length];
            var scored = 0;

            for (var e = 0; e < boundaries.Length; e++)
            {
                var b = boundaries[e];
                var ea = plates[b.PlateA].EffectiveIndex;
                var eb = plates[b.PlateB].EffectiveIndex;
                if (ea == eb
                    || !plates[ea].IsDistrict
                    || !plates[eb].IsDistrict
                    || b.Convergence < Settings.ConvergenceThreshold
                    || b.OnLandLengthMeters < Settings.ConvergentMinLandLengthMeters)
                {
                    continue;
                }

                int upper;
                if (plates[ea].LandAreaSqm > plates[eb].LandAreaSqm)
                {
                    upper = ea;
                }
                else if (plates[eb].LandAreaSqm > plates[ea].LandAreaSqm)
                {
                    upper = eb;
                }
                else
                {
                    upper = Mathf.Min(ea, eb);
                }

                var lower = upper == ea ? eb : ea;
                score[upper] += b.OnLandLengthMeters;
                score[lower] -= b.OnLandLengthMeters;
                b.Scored = true;
                b.UpperPlate = upper;
                b.LowerPlate = lower;
                scored++;
            }

            var districts = new List<int>();
            for (var k = 0; k < plates.Length; k++)
            {
                if (plates[k].IsDistrict)
                {
                    districts.Add(k);
                }
            }

            UsedCratonRule = scored == 0 && districts.Count > 1;
            for (var i = 0; i < districts.Count; i++)
            {
                var k = districts[i];
                plates[k].Score = UsedCratonRule ? plates[k].LandAreaSqm : (float)score[k];
            }

            // Ascending score; equal scores rank the lower plate index lower.
            districts.Sort((a, b) =>
            {
                var byScore = plates[a].Score.CompareTo(plates[b].Score);
                return byScore != 0 ? byScore : a.CompareTo(b);
            });

            // The lowest district must reach the sea, or its lowland would be a basin with no outlet. The
            // lowest-ranked district that does takes rank 0; everything below it moves up one rank.
            if (districts.Count > 1 && plates[districts[0]].FrontageMeters < Settings.MinLowlandFrontageMeters)
            {
                for (var i = 1; i < districts.Count; i++)
                {
                    if (plates[districts[i]].FrontageMeters < Settings.MinLowlandFrontageMeters)
                    {
                        continue;
                    }

                    var promoted = districts[i];
                    districts.RemoveAt(i);
                    districts.Insert(0, promoted);
                    break;
                }
            }

            for (var i = 0; i < districts.Count; i++)
            {
                var k = districts[i];
                plates[k].Rank = i;
                plates[k].Level = districts.Count > 1 ? i / (float)(districts.Count - 1) : 0f;
            }

            for (var k = 0; k < plates.Length; k++)
            {
                if (!plates[k].IsDistrict)
                {
                    var target = plates[k].EffectiveIndex;
                    plates[k].Level = plates[target].IsDistrict ? plates[target].Level : 0f;
                }
            }
        }

        /// <summary>On-land junctions of three or more districts, in a fixed positional order.</summary>
        private void BuildJunctions(CoastQuery coast, MacroPlates macro)
        {
            var list = new List<ReliefJunction>();
            if (Settings.JunctionRelaxRadiusMeters <= 0f)
            {
                junctions = list.ToArray();
                return;
            }

            for (var v = 0; v < macro.Junctions.Length; v++)
            {
                var members = new List<int>(4);
                var incident = macro.JunctionBoundaries[v];
                for (var i = 0; i < incident.Length; i++)
                {
                    var b = macro.Boundaries[incident[i]];
                    AddDistinct(members, plates[b.PlateA].EffectiveIndex);
                    AddDistinct(members, plates[b.PlateB].EffectiveIndex);
                }

                if (members.Count < 3)
                {
                    continue;
                }

                var position = macro.Junctions[v];
                if (!coast.IsInside(position.x, position.y))
                {
                    continue;
                }

                members.Sort();
                var sum = 0.0;
                for (var i = 0; i < members.Count; i++)
                {
                    sum += plates[members[i]].Level;
                }

                list.Add(new ReliefJunction
                {
                    Index = v,
                    Position = position,
                    Plates = members.ToArray(),
                    MeanLevel = (float)(sum / members.Count),
                });
            }

            list.Sort((a, b) =>
            {
                var byX = a.Position.x.CompareTo(b.Position.x);
                if (byX != 0)
                {
                    return byX;
                }

                var byZ = a.Position.y.CompareTo(b.Position.y);
                return byZ != 0 ? byZ : a.Index.CompareTo(b.Index);
            });

            junctions = list.ToArray();
        }

        private static void AddDistinct(List<int> list, int value)
        {
            if (!list.Contains(value))
            {
                list.Add(value);
            }
        }

        /// <summary>
        /// The level field Λ at a world point: plate levels blended across the actual plate boundaries, then
        /// relaxed at on-land junctions.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Each plate's soft membership is the product, over every other site, of a smoothstep of the signed
        /// perpendicular distance to their bisector - which is the line the boundary segment lies on. Deep
        /// inside a cell every factor is 1 and Λ is the plate's level; across a single boundary the two
        /// memberships are one smoothstep and its complement over twice
        /// <see cref="IslandReliefSettings.PlateEdgeHalfWidthMeters"/>, so the edge is one smooth ramp with
        /// no step and no landing. Near a junction the products blend all the plates involved and stay
        /// continuous from every side, which a per-boundary sum does not.
        /// </para>
        /// <para>
        /// Sums run in plate index order and junctions in their fixed order, so the value does not depend
        /// on evaluation order in floating point.
        /// </para>
        /// </remarks>
        private float LevelAt(float worldX, float worldZ)
        {
            var count = siteX.Length;
            if (count == 0 || !anyLevel)
            {
                return 0f;
            }

            var halfWidth = (double)Settings.PlateEdgeHalfWidthMeters;
            Span<double> distanceSq = stackalloc double[count];
            for (var k = 0; k < count; k++)
            {
                var dx = worldX - siteX[k];
                var dz = worldZ - siteZ[k];
                distanceSq[k] = dx * dx + dz * dz;
            }

            var weighted = 0.0;
            var total = 0.0;
            for (var k = 0; k < count; k++)
            {
                var membership = 1.0;
                for (var j = 0; j < count; j++)
                {
                    if (j == k)
                    {
                        continue;
                    }

                    // Signed distance to the bisector of k and j, positive on k's side.
                    var delta = (distanceSq[j] - distanceSq[k]) * inverseTwoSpan[k * count + j];
                    if (delta >= halfWidth)
                    {
                        continue;
                    }

                    if (delta <= -halfWidth)
                    {
                        membership = 0.0;
                        break;
                    }

                    var u = (delta + halfWidth) / (2.0 * halfWidth);
                    membership *= u * u * (3.0 - 2.0 * u);
                }

                if (membership <= 0.0)
                {
                    continue;
                }

                weighted += membership * plateLevel[k];
                total += membership;
            }

            var level = total > 0.0 ? (float)(weighted / total) : 0f;

            var radius = Settings.JunctionRelaxRadiusMeters;
            for (var i = 0; i < junctions.Length; i++)
            {
                var offset = new Vector2(worldX, worldZ) - junctions[i].Position;
                var distance = offset.magnitude;
                if (distance >= radius)
                {
                    continue;
                }

                var pull = 1f - Smooth01(distance / radius);
                level += pull * (junctions[i].MeanLevel - level);
            }

            return level;
        }
    }
}

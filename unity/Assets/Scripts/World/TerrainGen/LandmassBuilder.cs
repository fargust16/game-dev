using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Grows the landmass out of the crustal partition and publishes it as coastline polygons.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The landmass is a connected set of crustal cells, grown from one core outward along seeded
    /// per-edge weights until it reaches a land-area budget. Per-edge rather than per-site weights
    /// on purpose: per-site weights make growth "cheapest cells first" globally, which produces a
    /// blob, while an edge weight gives the growth a direction it has to pay for, which is what
    /// makes peninsulas and bays.
    /// </para>
    /// <para>
    /// A cell may only become land if every one of its raster cells sits inside the world rect inset
    /// by <see cref="WorldScale.IslandOceanMarginMeters"/>. That is the rule that keeps coast off the
    /// world border, and it is enforced on the cell rather than on the final polygon so the coast is
    /// never clamped into a straight run along the margin.
    /// </para>
    /// </remarks>
    public static class LandmassBuilder
    {
        public readonly struct Result
        {
            /// <summary>Published coastline polygons, ring 0 being the largest component.</summary>
            public IReadOnlyList<Vector2[]> CoastRings { get; }

            /// <summary>Crustal sites that became land, ascending.</summary>
            public IReadOnlyList<int> LandSites { get; }

            /// <summary>Land mask on the partition raster, before any polygon refinement.</summary>
            public bool[] LandRaster { get; }

            public int IsletCount { get; }
            public int ComponentCount { get; }
            public float TargetLandFraction { get; }
            public float RasterLandFraction { get; }

            /// <summary>Raster cells the enclosed-sea fill converted to land.</summary>
            public int EnclosedSeaFilled { get; }

            /// <summary>Displacement attempts per ring; &gt; 1 means a ring self-intersected and was redrawn.</summary>
            public IReadOnlyList<int> RingDisplaceAttempts { get; }

            /// <summary>Rings that fell back to the undisplaced outline because every attempt crossed.</summary>
            public int RingsFallenBack { get; }

            /// <summary>Growth ran out of eligible cells before it reached the area budget.</summary>
            public bool LandBudgetShort { get; }

            public Result(
                IReadOnlyList<Vector2[]> coastRings,
                IReadOnlyList<int> landSites,
                bool[] landRaster,
                int isletCount,
                int componentCount,
                float targetLandFraction,
                float rasterLandFraction,
                int enclosedSeaFilled,
                IReadOnlyList<int> ringDisplaceAttempts,
                int ringsFallenBack,
                bool landBudgetShort)
            {
                CoastRings = coastRings;
                LandSites = landSites;
                LandRaster = landRaster;
                IsletCount = isletCount;
                ComponentCount = componentCount;
                TargetLandFraction = targetLandFraction;
                RasterLandFraction = rasterLandFraction;
                EnclosedSeaFilled = enclosedSeaFilled;
                RingDisplaceAttempts = ringDisplaceAttempts;
                RingsFallenBack = ringsFallenBack;
                LandBudgetShort = landBudgetShort;
            }
        }

        /// <summary>How many times a ring is redrawn at reduced roughness before giving up on it.</summary>
        private const int MaxDisplaceAttempts = 4;

        public static Result Build(
            Seed.IWorldSeed worldSeed, TerrainGenSettings settings, CrustalPartition partition)
        {
            var margin = WorldScale.IslandOceanMarginMeters;
            var worldArea = WorldScale.WorldExtentXMeters * WorldScale.WorldExtentZMeters;

            var eligible = new bool[partition.SiteCount];
            for (var i = 0; i < partition.SiteCount; i++)
            {
                eligible[i] = partition.IsInsideMargin(i, margin);
            }

            var crustRnd = worldSeed.Fork("terrain.crust");

            // Drawn before the core so the two decisions do not depend on each other's cost.
            var targetFraction = Mathf.Lerp(
                settings.LandFractionTargetMin,
                settings.LandFractionTargetMax,
                (float)crustRnd.NextDouble());
            var targetArea = worldArea * targetFraction;

            var coreAnchor = new Vector2(
                WorldScale.WorldExtentXMeters * Mathf.Lerp(0.35f, 0.65f, (float)crustRnd.NextDouble()),
                WorldScale.WorldExtentZMeters * Mathf.Lerp(0.35f, 0.65f, (float)crustRnd.NextDouble()));

            var core = NearestEligibleSite(partition, eligible, coreAnchor);
            if (core < 0)
            {
                return new Result(
                    System.Array.Empty<Vector2[]>(), System.Array.Empty<int>(),
                    new bool[partition.Cells.Length], 0, 0, targetFraction, 0f, 0,
                    System.Array.Empty<int>(), 0, true);
            }

            var isLand = new bool[partition.SiteCount];
            var edgeWeights = new Dictionary<long, float>(512);
            var area = Grow(
                worldSeed, partition, eligible, isLand, core, targetArea, edgeWeights,
                out var budgetShort);

            var isletSites = PlaceIslets(worldSeed, settings, partition, eligible, isLand);
            for (var i = 0; i < isletSites.Count; i++)
            {
                isLand[isletSites[i]] = true;
                area += partition.SiteCellCount[isletSites[i]] * partition.CellAreaSqm;
            }

            var landRaster = new bool[partition.Cells.Length];
            for (var i = 0; i < landRaster.Length; i++)
            {
                landRaster[i] = isLand[partition.Cells[i]];
            }

            var enclosedFilled = CoastOutline.FillEnclosedSea(
                landRaster, partition.RasterWidth, partition.RasterHeight);
            area += enclosedFilled * partition.CellAreaSqm;

            var componentCount = CoastOutline.LabelComponents(
                landRaster, partition.RasterWidth, partition.RasterHeight, out var labels);

            var rings = new List<Vector2[]>(componentCount);
            var attempts = new List<int>(componentCount);
            var fallbacks = 0;

            for (var label = 0; label < componentCount; label++)
            {
                var traced = CoastOutline.TraceComponent(
                    labels, partition.RasterWidth, partition.RasterHeight, label,
                    partition.CellMeters);
                if (traced.Length < 4)
                {
                    attempts.Add(0);
                    continue;
                }

                var simplified = CoastOutline.Simplify(traced, settings.SimplifyToleranceMeters);
                var refined = simplified;
                var used = 0;

                for (var attempt = 0; attempt < MaxDisplaceAttempts; attempt++)
                {
                    used = attempt + 1;
                    var rnd = worldSeed.Fork("terrain.coast", label, attempt);
                    var roughness = settings.CoastRoughness * Mathf.Pow(0.5f, attempt);
                    var candidate = CoastOutline.Displace(
                        simplified,
                        roughness,
                        settings.CoastMinSegmentMeters,
                        rnd,
                        margin,
                        WorldScale.WorldExtentXMeters - margin,
                        WorldScale.WorldExtentZMeters - margin);

                    if (CoastOutline.IsSimple(candidate))
                    {
                        refined = candidate;
                        break;
                    }

                    // Halving the roughness and redrawing is deterministic and keeps the ring's
                    // character; abandoning displacement entirely is the last resort, because an
                    // unrefined ring is visibly a chain of cell edges.
                    refined = simplified;
                }

                if (ReferenceEquals(refined, simplified))
                {
                    fallbacks++;
                }

                rings.Add(refined);
                attempts.Add(used);
            }

            // Largest first, so ring 0 is the mainland and downstream never has to guess.
            var order = new List<int>(rings.Count);
            for (var i = 0; i < rings.Count; i++)
            {
                order.Add(i);
            }

            var areas = new float[rings.Count];
            for (var i = 0; i < rings.Count; i++)
            {
                areas[i] = Mathf.Abs(Town.Geometry.Geo2.PolygonArea(rings[i]));
            }

            order.Sort((a, b) =>
            {
                var byArea = areas[b].CompareTo(areas[a]);
                return byArea != 0 ? byArea : a.CompareTo(b);
            });

            var orderedRings = new List<Vector2[]>(rings.Count);
            var orderedAttempts = new List<int>(rings.Count);
            for (var i = 0; i < order.Count; i++)
            {
                orderedRings.Add(rings[order[i]]);
                orderedAttempts.Add(attempts[order[i]]);
            }

            var landSites = new List<int>();
            for (var i = 0; i < isLand.Length; i++)
            {
                if (isLand[i])
                {
                    landSites.Add(i);
                }
            }

            return new Result(
                orderedRings,
                landSites,
                landRaster,
                isletSites.Count,
                componentCount,
                targetFraction,
                area / worldArea,
                enclosedFilled,
                orderedAttempts,
                fallbacks,
                budgetShort);
        }

        private static int NearestEligibleSite(
            CrustalPartition partition, bool[] eligible, Vector2 anchor)
        {
            var best = -1;
            var bestSq = float.PositiveInfinity;
            for (var i = 0; i < partition.SiteCount; i++)
            {
                if (!eligible[i])
                {
                    continue;
                }

                var sq = (partition.SiteCentroid[i] - anchor).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = i;
                }
            }

            return best;
        }

        private static float Grow(
            Seed.IWorldSeed worldSeed,
            CrustalPartition partition,
            bool[] eligible,
            bool[] isLand,
            int core,
            float targetArea,
            Dictionary<long, float> edgeWeights,
            out bool budgetShort)
        {
            // A total order over (weight, candidate, from) — no equal elements, so the pop order is
            // fully determined and never depends on insertion or hash iteration order.
            var frontier = new SortedSet<(float weight, int candidate, int from)>();

            void PushEdges(int site)
            {
                var neighbours = partition.SiteNeighbours[site];
                for (var k = 0; k < neighbours.Length; k++)
                {
                    var n = neighbours[k];
                    if (isLand[n] || !eligible[n])
                    {
                        continue;
                    }

                    frontier.Add((EdgeWeight(worldSeed, edgeWeights, site, n), n, site));
                }
            }

            isLand[core] = true;
            var area = partition.SiteCellCount[core] * partition.CellAreaSqm;
            PushEdges(core);

            while (area < targetArea && frontier.Count > 0)
            {
                var next = frontier.Min;
                frontier.Remove(next);

                if (isLand[next.candidate] || !eligible[next.candidate])
                {
                    continue;
                }

                isLand[next.candidate] = true;
                area += partition.SiteCellCount[next.candidate] * partition.CellAreaSqm;
                PushEdges(next.candidate);
            }

            budgetShort = area < targetArea;
            return area;
        }

        private static float EdgeWeight(
            Seed.IWorldSeed worldSeed, Dictionary<long, float> cache, int a, int b)
        {
            var lo = Mathf.Min(a, b);
            var hi = Mathf.Max(a, b);
            var key = ((long)lo << 32) | (uint)hi;
            if (cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var value = (float)worldSeed.Fork("terrain.crust.edge", lo, hi).NextDouble();
            cache[key] = value;
            return value;
        }

        /// <summary>
        /// Detached islets: eligible cells at least two adjacency steps from the mainland, so an
        /// islet is a separate landmass rather than a cell that nearly joined.
        /// </summary>
        private static List<int> PlaceIslets(
            Seed.IWorldSeed worldSeed,
            TerrainGenSettings settings,
            CrustalPartition partition,
            bool[] eligible,
            bool[] isLand)
        {
            var chosen = new List<int>();
            if (settings.MaxIslets <= 0)
            {
                return chosen;
            }

            var nearLand = new bool[partition.SiteCount];
            for (var i = 0; i < partition.SiteCount; i++)
            {
                if (!isLand[i])
                {
                    continue;
                }

                var neighbours = partition.SiteNeighbours[i];
                for (var k = 0; k < neighbours.Length; k++)
                {
                    nearLand[neighbours[k]] = true;
                }
            }

            var nearNearLand = (bool[])nearLand.Clone();
            for (var i = 0; i < partition.SiteCount; i++)
            {
                if (!nearLand[i])
                {
                    continue;
                }

                var neighbours = partition.SiteNeighbours[i];
                for (var k = 0; k < neighbours.Length; k++)
                {
                    nearNearLand[neighbours[k]] = true;
                }
            }

            var candidates = new List<(float weight, int site)>();
            for (var i = 0; i < partition.SiteCount; i++)
            {
                if (!eligible[i] || isLand[i] || nearNearLand[i])
                {
                    continue;
                }

                candidates.Add(((float)worldSeed.Fork("terrain.crust.islet", i).NextDouble(), i));
            }

            if (candidates.Count == 0)
            {
                return chosen;
            }

            candidates.Sort((a, b) =>
            {
                var byWeight = a.weight.CompareTo(b.weight);
                return byWeight != 0 ? byWeight : a.site.CompareTo(b.site);
            });

            var rnd = worldSeed.Fork("terrain.crust.isletcount");
            var wanted = Mathf.Min(rnd.Next(0, settings.MaxIslets + 1), candidates.Count);
            for (var i = 0; i < wanted; i++)
            {
                chosen.Add(candidates[i].site);
            }

            chosen.Sort();
            return chosen;
        }
    }
}

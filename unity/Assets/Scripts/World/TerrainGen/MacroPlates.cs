using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// A coarse structural partition of the world into a handful of drifting plates, with its
    /// boundaries as exact straight segments.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the macro-scale source Stage 2 was missing. The crustal partition Stage 1 grows the
    /// landmass from has sites 200 m apart, so its boundaries bend every 100-300 m; a belt 300 m wide
    /// cannot follow a curve whose bends are the same size as the belt. This layer partitions the same
    /// world at about a kilometre, where a boundary is straight for most of its length and turns only
    /// at a junction.
    /// </para>
    /// <para>
    /// The sites are a deterministic thinning of Stage 1's own crustal sites rather than an unrelated
    /// point set, so the two layers are the same crust described at two scales: every macro plate is a
    /// union of fine cells up to the raster's resolution, and "which plates does this island span" is a
    /// meaningful question. Nothing about the macro geometry is inherited from the fine boundaries
    /// themselves -- a macro boundary is the bisector of two points a kilometre apart and knows nothing
    /// about the fine chain that wanders near it.
    /// </para>
    /// <para>
    /// Computed analytically, not from a raster. With a dozen sites the exact Voronoi edge between two
    /// of them is the bisector line clipped by every other site's half-plane, which is a handful of
    /// scalar comparisons and gives segments that are straight to float precision. Stage 1 rasterises
    /// because it has hundreds of sites and needs cell membership per sample; here neither is true, and
    /// the raster's quantisation is exactly the wiggle this layer exists to avoid.
    /// </para>
    /// </remarks>
    public sealed class MacroPlates
    {
        /// <summary>One boundary between two plates.</summary>
        public readonly struct Boundary
        {
            /// <summary>Lower plate index of the pair.</summary>
            public int PlateA { get; }

            /// <summary>Higher plate index of the pair.</summary>
            public int PlateB { get; }

            public Vector2 A { get; }
            public Vector2 B { get; }
            public float Length { get; }

            /// <summary>Unit normal pointing from <see cref="PlateA"/>'s site to <see cref="PlateB"/>'s.</summary>
            public Vector2 Normal { get; }

            /// <summary>
            /// How hard the two plates press into each other across this boundary, in [-2, 2].
            /// </summary>
            /// <remarks>
            /// Positive is convergence, which is the only case that raises mountains. This is the
            /// answer to "why is this range here" -- not a seed, not a noise octave, but a specific
            /// pair of plates moving into each other.
            /// </remarks>
            public float Convergence { get; }

            public Boundary(int plateA, int plateB, Vector2 a, Vector2 b, Vector2 normal, float convergence)
            {
                PlateA = plateA;
                PlateB = plateB;
                A = a;
                B = b;
                Length = (b - a).magnitude;
                Normal = normal;
                Convergence = convergence;
            }

            public Vector2 Mid => (A + B) * 0.5f;

            public Vector2 Direction => Length > 1e-4f ? (B - A) / Length : Vector2.right;
        }

        /// <summary>Plate sites in world XZ, ascending by the order they were thinned in.</summary>
        public Vector2[] Sites { get; }

        /// <summary>Unit drift direction of each plate.</summary>
        public Vector2[] Drift { get; }

        /// <summary>The world's compression axis: the direction plates are being pushed along.</summary>
        /// <remarks>
        /// One regional stress field for the whole world, which is what gives its ranges a common
        /// grain. Without it every plate drifts independently, convergence across any given boundary is
        /// a coin toss, and a world can easily come out with no convergent boundary on its island at
        /// all -- which is exactly what the first measured pass produced.
        /// </remarks>
        public Vector2 StressAxisDirection { get; private set; }

        /// <summary>Boundaries, ordered by plate pair so nothing depends on traversal order.</summary>
        public Boundary[] Boundaries { get; }

        /// <summary>Junction index of each boundary's A and B ends, as <c>[boundary * 2 + 0/1]</c>.</summary>
        public int[] EndpointJunction { get; }

        /// <summary>Junction positions.</summary>
        public Vector2[] Junctions { get; }

        /// <summary>Boundary indices meeting at each junction, ascending.</summary>
        public int[][] JunctionBoundaries { get; }

        public int PlateCount => Sites.Length;

        /// <summary>Plates with at least one boundary inside the world rectangle.</summary>
        public int PlatesInWorld { get; }

        private MacroPlates(
            Vector2[] sites,
            Vector2[] drift,
            Boundary[] boundaries,
            int[] endpointJunction,
            Vector2[] junctions,
            int[][] junctionBoundaries,
            int platesInWorld)
        {
            Sites = sites;
            Drift = drift;
            Boundaries = boundaries;
            EndpointJunction = endpointJunction;
            Junctions = junctions;
            JunctionBoundaries = junctionBoundaries;
            PlatesInWorld = platesInWorld;
        }

        /// <summary>Shortest boundary worth keeping, metres. Below this it is a near-cocircular sliver.</summary>
        private const float MinBoundaryLengthMeters = 60f;

        /// <summary>Endpoints closer than this are the same junction, metres.</summary>
        /// <remarks>
        /// Tight because the endpoints are computed rather than rasterised: three bisectors through one
        /// Voronoi vertex agree to float precision, and a loose tolerance would start merging genuinely
        /// distinct junctions of a coarse partition.
        /// </remarks>
        private const float JunctionToleranceMeters = 1f;

        /// <summary>Plates whose site sits on a crustal cell that became land.</summary>
        public int PlatesOnLand { get; private set; }

        public static MacroPlates Build(
            Seed.IWorldSeed worldSeed,
            MacroStructureSettings settings,
            CrustalPartition fine,
            IReadOnlyList<int> landSites)
        {
            settings.Clamp();

            var stress = StressAxis(worldSeed);
            var sites = ThinSites(worldSeed, settings, fine, landSites, stress, out var onLand);
            var drift = BuildDrift(worldSeed, settings, sites, stress);

            var minX = -settings.ClipMarginMeters;
            var minZ = -settings.ClipMarginMeters;
            var maxX = WorldScale.WorldExtentXMeters + settings.ClipMarginMeters;
            var maxZ = WorldScale.WorldExtentZMeters + settings.ClipMarginMeters;

            var boundaries = new List<Boundary>(64);
            for (var i = 0; i < sites.Length; i++)
            {
                for (var j = i + 1; j < sites.Length; j++)
                {
                    if (!TryBisectorSegment(sites, i, j, minX, minZ, maxX, maxZ, out var a, out var b))
                    {
                        continue;
                    }

                    if ((b - a).magnitude < MinBoundaryLengthMeters)
                    {
                        continue;
                    }

                    var normal = (sites[j] - sites[i]).normalized;
                    var convergence = Vector2.Dot(drift[i] - drift[j], normal);
                    boundaries.Add(new Boundary(i, j, a, b, normal, convergence));
                }
            }

            BuildJunctions(
                boundaries, out var endpointJunction, out var junctions, out var junctionBoundaries);

            var touchesWorld = new bool[sites.Length];
            for (var e = 0; e < boundaries.Count; e++)
            {
                var mid = boundaries[e].Mid;
                if (mid.x < 0f || mid.y < 0f
                    || mid.x > WorldScale.WorldExtentXMeters || mid.y > WorldScale.WorldExtentZMeters)
                {
                    continue;
                }

                touchesWorld[boundaries[e].PlateA] = true;
                touchesWorld[boundaries[e].PlateB] = true;
            }

            var platesInWorld = 0;
            for (var i = 0; i < touchesWorld.Length; i++)
            {
                if (touchesWorld[i])
                {
                    platesInWorld++;
                }
            }

            return new MacroPlates(
                sites, drift, boundaries.ToArray(),
                endpointJunction, junctions, junctionBoundaries, platesInWorld)
            {
                PlatesOnLand = onLand,
                StressAxisDirection = stress,
            };
        }

        /// <summary>Plate a world point belongs to, by nearest site.</summary>
        public int PlateAt(float worldX, float worldZ)
        {
            var best = 0;
            var bestSq = float.PositiveInfinity;
            for (var i = 0; i < Sites.Length; i++)
            {
                var dx = Sites[i].x - worldX;
                var dz = Sites[i].y - worldZ;
                var sq = dx * dx + dz * dz;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = i;
                }
            }

            return best;
        }

        /// <summary>Mean length of the boundaries whose midpoint lies inside the world.</summary>
        public float MeanInteriorBoundaryLength()
        {
            var total = 0f;
            var count = 0;
            for (var e = 0; e < Boundaries.Length; e++)
            {
                var mid = Boundaries[e].Mid;
                if (mid.x < 0f || mid.y < 0f
                    || mid.x > WorldScale.WorldExtentXMeters || mid.y > WorldScale.WorldExtentZMeters)
                {
                    continue;
                }

                total += Boundaries[e].Length;
                count++;
            }

            return count > 0 ? total / count : 0f;
        }

        /// <summary>
        /// Land sites ordered from the two ends of the compression axis inwards.
        /// </summary>
        /// <remarks>
        /// Greedy acceptance takes the first entries it can, so putting the extremes first is what
        /// decides that the island's two plates sit across the collision rather than side by side
        /// along it. With a random order the island's own boundary was convergent by luck, and on two
        /// of the five canonical seeds the luck ran out.
        /// </remarks>
        private static List<int> FromTheEndsInwards(
            CrustalPartition fine, IReadOnlyList<int> landSites, Vector2 stress)
        {
            var ordered = new List<int>();
            if (landSites == null || landSites.Count == 0)
            {
                return ordered;
            }

            var byProjection = new List<int>(landSites);
            byProjection.Sort((u, v) =>
            {
                var du = Vector2.Dot(fine.Sites[u], stress);
                var dv = Vector2.Dot(fine.Sites[v], stress);
                var c = du.CompareTo(dv);
                return c != 0 ? c : u.CompareTo(v);
            });

            var low = 0;
            var high = byProjection.Count - 1;
            var takeHigh = true;
            while (low <= high)
            {
                if (takeHigh)
                {
                    ordered.Add(byProjection[high--]);
                }
                else
                {
                    ordered.Add(byProjection[low++]);
                }

                takeHigh = !takeHigh;
            }

            return ordered;
        }

        /// <summary>The seeded direction the world is being compressed along.</summary>
        private static Vector2 StressAxis(Seed.IWorldSeed worldSeed)
        {
            var rnd = worldSeed.Fork("terrain.macro.stress");
            var angle = (float)(rnd.NextDouble() * (System.Math.PI * 2.0));
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        /// <summary>
        /// Plate motion under one regional stress field, plus a per-plate share of its own.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The systematic term makes plates on opposite sides of the world move towards each other, so
        /// a boundary lying across the compression axis converges and one lying along it does not. That
        /// is what a mountain belt's orientation should be a consequence of, and it makes the answer to
        /// "why does this range run this way" a property of the world rather than of a seed.
        /// </para>
        /// <para>
        /// The jitter is what keeps the belts from all being parallel. It is a per-plate direction, not
        /// a field sampled anywhere -- there is no noise function here, and nothing about it varies
        /// with position within a plate.
        /// </para>
        /// </remarks>
        private static Vector2[] BuildDrift(
            Seed.IWorldSeed worldSeed, MacroStructureSettings settings, Vector2[] sites, Vector2 stress)
        {
            var centre = new Vector2(
                WorldScale.WorldExtentXMeters * 0.5f, WorldScale.WorldExtentZMeters * 0.5f);
            var scale = Mathf.Max(1f, settings.StressScaleMeters);

            var drift = new Vector2[sites.Length];
            for (var i = 0; i < sites.Length; i++)
            {
                var along = Mathf.Clamp(Vector2.Dot(sites[i] - centre, stress) / scale, -1f, 1f);

                var rnd = worldSeed.Fork("terrain.macro.drift", i);
                var angle = (float)(rnd.NextDouble() * (System.Math.PI * 2.0));
                var jitter = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * settings.DriftJitter;

                var motion = -along * stress + jitter;
                drift[i] = motion.sqrMagnitude < 1e-8f ? stress : motion.normalized;
            }

            return drift;
        }

        /// <summary>
        /// Thins Stage 1's crustal sites down to macro spacing, deterministically.
        /// </summary>
        /// <remarks>
        /// A seeded permutation followed by greedy acceptance, which is dart throwing over a finite
        /// candidate set: the result is blue noise at the macro spacing and does not depend on the
        /// order Stage 1 happened to accept its own sites in.
        /// </remarks>
        private static Vector2[] ThinSites(
            Seed.IWorldSeed worldSeed,
            MacroStructureSettings settings,
            CrustalPartition fine,
            IReadOnlyList<int> landSites,
            Vector2 stress,
            out int onLand)
        {
            var rnd = worldSeed.Fork("terrain.macro.sites");

            var minX = -settings.MarginMeters;
            var minZ = -settings.MarginMeters;
            var maxX = WorldScale.WorldExtentXMeters + settings.MarginMeters;
            var maxZ = WorldScale.WorldExtentZMeters + settings.MarginMeters;
            var spacingSq = settings.SiteSpacingMeters * settings.SiteSpacingMeters;

            var accepted = new List<Vector2>(32);

            void Sweep(IReadOnlyList<int> candidates, bool shuffle = false)
            {
                var order = new int[candidates.Count];
                for (var i = 0; i < order.Length; i++)
                {
                    order[i] = candidates[i];
                }

                if (shuffle)
                {
                    for (var i = order.Length - 1; i > 0; i--)
                    {
                        var j = rnd.Next(i + 1);
                        (order[i], order[j]) = (order[j], order[i]);
                    }
                }

                for (var k = 0; k < order.Length; k++)
                {
                    var p = fine.Sites[order[k]];
                    if (p.x < minX || p.y < minZ || p.x > maxX || p.y > maxZ)
                    {
                        continue;
                    }

                    var ok = true;
                    for (var a = 0; a < accepted.Count; a++)
                    {
                        if ((accepted[a] - p).sqrMagnitude < spacingSq)
                        {
                            ok = false;
                            break;
                        }
                    }

                    if (ok)
                    {
                        accepted.Add(p);
                    }
                }
            }

            // Land first, from the ends of the compression axis inwards. A plate boundary is the
            // bisector of two sites, so it runs between them and across the line joining them: taking
            // the two land sites furthest apart along the compression axis puts a boundary through the
            // middle of the island, square to the direction the plates are being pushed. That is the
            // island straddling a collision zone, which is why it has mountains at all.
            Sweep(FromTheEndsInwards(fine, landSites, stress));
            onLand = accepted.Count;

            var rest = new List<int>(fine.SiteCount);
            for (var i = 0; i < fine.SiteCount; i++)
            {
                rest.Add(i);
            }

            Sweep(rest, shuffle: true);

            // Sorted so the plate index is a function of position rather than of acceptance order,
            // which keeps the boundary ordering stable when the spacing is retuned. The land count is
            // taken before this, because after it the land plates are no longer the first entries.
            accepted.Sort((u, v) =>
            {
                var c = u.x.CompareTo(v.x);
                return c != 0 ? c : u.y.CompareTo(v.y);
            });

            return accepted.ToArray();
        }

        /// <summary>
        /// The exact Voronoi edge between two sites: their bisector, clipped by every other site.
        /// </summary>
        private static bool TryBisectorSegment(
            Vector2[] sites, int i, int j,
            float minX, float minZ, float maxX, float maxZ,
            out Vector2 a, out Vector2 b)
        {
            a = Vector2.zero;
            b = Vector2.zero;

            var pi = sites[i];
            var pj = sites[j];
            var delta = pj - pi;
            var span = delta.magnitude;
            if (span < 1e-3f)
            {
                return false;
            }

            var mid = (pi + pj) * 0.5f;
            var along = new Vector2(-delta.y, delta.x) / span;

            var lo = float.NegativeInfinity;
            var hi = float.PositiveInfinity;

            if (!ClipSlab(mid.x, along.x, minX, maxX, ref lo, ref hi))
            {
                return false;
            }

            if (!ClipSlab(mid.y, along.y, minZ, maxZ, ref lo, ref hi))
            {
                return false;
            }

            for (var k = 0; k < sites.Length; k++)
            {
                if (k == i || k == j)
                {
                    continue;
                }

                // Keep only the part at least as close to pi as to pk:
                //   |x - pi|^2 <= |x - pk|^2  <=>  2 x . (pk - pi) <= |pk|^2 - |pi|^2
                var w = sites[k] - pi;
                var slope = 2f * Vector2.Dot(along, w);
                var limit = sites[k].sqrMagnitude - pi.sqrMagnitude - 2f * Vector2.Dot(mid, w);

                if (slope > 1e-6f)
                {
                    hi = Mathf.Min(hi, limit / slope);
                }
                else if (slope < -1e-6f)
                {
                    lo = Mathf.Max(lo, limit / slope);
                }
                else if (limit < 0f)
                {
                    return false;
                }

                if (hi <= lo)
                {
                    return false;
                }
            }

            if (!(hi > lo) || float.IsInfinity(lo) || float.IsInfinity(hi))
            {
                return false;
            }

            a = mid + along * lo;
            b = mid + along * hi;
            return true;
        }

        /// <summary>Narrows <c>[lo, hi]</c> to the part of <c>origin + t * direction</c> inside a slab.</summary>
        private static bool ClipSlab(
            float origin, float direction, float low, float high, ref float lo, ref float hi)
        {
            if (Mathf.Abs(direction) < 1e-9f)
            {
                return origin >= low && origin <= high;
            }

            var t0 = (low - origin) / direction;
            var t1 = (high - origin) / direction;
            if (t0 > t1)
            {
                (t0, t1) = (t1, t0);
            }

            lo = Mathf.Max(lo, t0);
            hi = Mathf.Min(hi, t1);
            return hi > lo;
        }

        private static void BuildJunctions(
            List<Boundary> boundaries,
            out int[] endpointJunction,
            out Vector2[] junctions,
            out int[][] junctionBoundaries)
        {
            var count = boundaries.Count * 2;
            var points = new Vector2[count];
            for (var e = 0; e < boundaries.Count; e++)
            {
                points[e * 2] = boundaries[e].A;
                points[e * 2 + 1] = boundaries[e].B;
            }

            endpointJunction = new int[count];
            for (var i = 0; i < count; i++)
            {
                endpointJunction[i] = -1;
            }

            var centres = new List<Vector2>(count);
            var members = new List<List<int>>(count);
            var toleranceSq = JunctionToleranceMeters * JunctionToleranceMeters;

            for (var i = 0; i < count; i++)
            {
                var vertex = -1;
                for (var v = 0; v < centres.Count; v++)
                {
                    if ((centres[v] - points[i]).sqrMagnitude <= toleranceSq)
                    {
                        vertex = v;
                        break;
                    }
                }

                if (vertex < 0)
                {
                    vertex = centres.Count;
                    centres.Add(points[i]);
                    members.Add(new List<int>(4));
                }

                endpointJunction[i] = vertex;
                var boundary = i / 2;
                if (!members[vertex].Contains(boundary))
                {
                    members[vertex].Add(boundary);
                }
            }

            junctions = centres.ToArray();
            junctionBoundaries = new int[members.Count][];
            for (var v = 0; v < members.Count; v++)
            {
                members[v].Sort();
                junctionBoundaries[v] = members[v].ToArray();
            }
        }
    }
}

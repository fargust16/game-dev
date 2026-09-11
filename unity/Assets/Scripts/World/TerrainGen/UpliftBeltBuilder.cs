using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Selects one or two uplift belts from the crustal boundaries and gives each one its profiles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every interior boundary between two land cells gets a seeded convergence weight. Chains of
    /// adjacent boundaries are grown from the strongest of them, and the longest chains become range
    /// spines. That is the whole answer to "why is this mountain here": because these crustal blocks
    /// meet along this line, and this line converged.
    /// </para>
    /// <para>
    /// Growth is scored on convergence <em>and</em> straightness. Three boundaries meet at a crustal
    /// vertex at roughly 120 degrees, so continuing through one means turning about 60; taking the
    /// strongest edge without regard to direction produces a belt that doubles back on itself, which
    /// is a random walk with a geological name. The turn cap and the straightness term are what make
    /// the result a belt.
    /// </para>
    /// </remarks>
    public static class UpliftBeltBuilder
    {
        /// <summary>Minimum convergence weight for a boundary to be part of a belt at all.</summary>
        private const float ConvergenceThreshold = 0.35f;

        /// <summary>How much weight counts against straightness when choosing the next boundary.</summary>
        private const float WeightShare = 0.6f;

        private const int MaxChainEdges = 24;
        private const float MaxChainLengthMeters = 2400f;

        /// <summary>
        /// Spacing the spine is resampled to before it becomes a range, metres.
        /// </summary>
        /// <remarks>
        /// A belt is evaluated as the maximum over its crest samples, so the samples have to be
        /// close together relative to the narrowest flank or the envelope scallops and the ridge
        /// reads as a string of beads. The narrowest flank this configuration can produce is the
        /// scarp side at the minimum half-width - about 120 m - so 15 m keeps roughly eight samples
        /// across it. Tying the spacing to that rather than to the number of smoothing passes means
        /// a short chain and a long one are equally smooth.
        /// </remarks>
        private const float SpineSampleMeters = 15f;

        /// <summary>Corner-cutting passes applied to a chain before it becomes a spine.</summary>
        /// <remarks>
        /// A chain of crustal boundaries turns by roughly 60 degrees at every junction, which leaves a
        /// curvature radius near 100 m - far tighter than the belt is wide. The axis parameterisation
        /// is only single-valued while the half-width stays under that radius, so the spine has to be
        /// smooth enough to carry the width the range wants. This is geometry correction, not
        /// cosmetic smoothing: MountainRange still caps the width by the curvature that remains, and
        /// reports how much of the range that cap touched.
        /// </remarks>
        private const int SmoothingPasses = 4;

        /// <summary>
        /// What the chaining actually found, so a world with no mountains reports why.
        /// </summary>
        /// <remarks>
        /// A generator that silently produces nothing is the hardest kind to debug. These four
        /// numbers separate "no boundaries converged" from "chains grew but none was long enough".
        /// </remarks>
        public readonly struct BuildReport
        {
            public int CandidateEdges { get; }
            public int ChainsGrown { get; }
            public int ChainsAccepted { get; }
            public float LongestChainMeters { get; }

            public BuildReport(int candidateEdges, int chainsGrown, int chainsAccepted, float longest)
            {
                CandidateEdges = candidateEdges;
                ChainsGrown = chainsGrown;
                ChainsAccepted = chainsAccepted;
                LongestChainMeters = longest;
            }

            public override string ToString() =>
                $"candidates={CandidateEdges} grown={ChainsGrown} accepted={ChainsAccepted} "
                + $"longest={LongestChainMeters:F0}m";
        }

        public static List<MountainRange> Build(
            Seed.IWorldSeed worldSeed,
            TerrainGenSettings settings,
            CrustalEdges edges,
            CoastQuery coast) => Build(worldSeed, settings, edges, coast, out _);

        /// <inheritdoc cref="Build(Seed.IWorldSeed, TerrainGenSettings, CrustalEdges, CoastQuery)"/>
        public static List<MountainRange> Build(
            Seed.IWorldSeed worldSeed,
            TerrainGenSettings settings,
            CrustalEdges edges,
            CoastQuery coast,
            out BuildReport report)
        {
            var ranges = new List<MountainRange>();
            report = default;
            if (edges.Edges.Length == 0)
            {
                return ranges;
            }

            var weights = new float[edges.Edges.Length];
            for (var i = 0; i < weights.Length; i++)
            {
                var e = edges.Edges[i];
                weights[i] = (float)worldSeed.Fork("terrain.range.edge", e.SiteA, e.SiteB).NextDouble();
            }

            var used = new bool[edges.Edges.Length];
            var chains = GrowChains(
                edges, weights, used, settings, settings.MinSpineLengthMeters,
                out var candidateEdges, out var chainsGrown, out var longestChain);
            report = new BuildReport(candidateEdges, chainsGrown, chains.Count, longestChain);

            // Longest first: the design keeps the longest chains above a minimum, and a belt's length
            // is what makes it a system rather than a hill.
            chains.Sort((a, b) =>
            {
                var byLength = b.LengthMeters.CompareTo(a.LengthMeters);
                return byLength != 0 ? byLength : a.SeedEdge.CompareTo(b.SeedEdge);
            });

            var systemCount = Mathf.Min(settings.MaxMountainSystems, chains.Count);
            for (var i = 0; i < systemCount; i++)
            {
                var range = BuildRange(
                    worldSeed, settings, edges, chains[i], ranges.Count, -1, 1f, 1f, coast);
                if (range != null)
                {
                    ranges.Add(range);
                }
            }

            if (settings.MaxBranchesPerRange <= 0)
            {
                return ranges;
            }

            // Branches are grown from boundaries that meet the spine at a crustal vertex, so a
            // branch is another belt of the same partition rather than a decorative arm.
            var primaryCount = ranges.Count;
            for (var i = 0; i < primaryCount; i++)
            {
                var parentChain = chains[i];
                var branchRnd = worldSeed.Fork("terrain.range.branch", i);
                var wanted = branchRnd.Next(0, settings.MaxBranchesPerRange + 1);
                if (wanted <= 0)
                {
                    continue;
                }

                var placed = 0;
                for (var v = 1; v < parentChain.Vertices.Count - 1 && placed < wanted; v++)
                {
                    var vertex = parentChain.Vertices[v];
                    var branchChains = GrowChainsFromVertex(
                        edges, weights, used, settings, vertex, settings.MinBranchLengthMeters);
                    if (branchChains.Count == 0)
                    {
                        continue;
                    }

                    branchChains.Sort((a, b) =>
                    {
                        var byLength = b.LengthMeters.CompareTo(a.LengthMeters);
                        return byLength != 0 ? byLength : a.SeedEdge.CompareTo(b.SeedEdge);
                    });

                    var crestScale = Mathf.Lerp(
                        settings.BranchCrestScaleMin,
                        settings.BranchCrestScaleMax,
                        (float)branchRnd.NextDouble());

                    var branch = BuildRange(
                        worldSeed, settings, edges, branchChains[0], ranges.Count, i, crestScale,
                        0.72f, coast);
                    if (branch != null)
                    {
                        ranges.Add(branch);
                        placed++;
                    }
                }
            }

            return ranges;
        }

        /// <summary>A chain of crustal boundaries, as the vertex path it traces.</summary>
        private sealed class Chain
        {
            public List<int> Vertices = new List<int>();
            public List<int> Edges = new List<int>();
            public float LengthMeters;
            public int SeedEdge;
        }

        private static List<Chain> GrowChains(
            CrustalEdges edges,
            float[] weights,
            bool[] used,
            TerrainGenSettings settings,
            float minLengthMeters) =>
            GrowChains(edges, weights, used, settings, minLengthMeters, out _, out _, out _);

        private static List<Chain> GrowChains(
            CrustalEdges edges,
            float[] weights,
            bool[] used,
            TerrainGenSettings settings,
            float minLengthMeters,
            out int candidateEdges,
            out int chainsGrown,
            out float longestChainMeters)
        {
            candidateEdges = 0;
            chainsGrown = 0;
            longestChainMeters = 0f;

            var order = new List<int>(edges.Edges.Length);
            for (var i = 0; i < edges.Edges.Length; i++)
            {
                if (weights[i] >= ConvergenceThreshold)
                {
                    order.Add(i);
                }
            }

            order.Sort((a, b) =>
            {
                var byWeight = weights[b].CompareTo(weights[a]);
                return byWeight != 0 ? byWeight : a.CompareTo(b);
            });

            candidateEdges = order.Count;

            // Discovery first, acceptance second. Growing into the shared used-set as we went meant
            // the first chain to clear the minimum consumed boundaries a longer chain needed, so the
            // result depended on which boundary happened to be the strongest rather than on which
            // path was the longest. Each candidate now grows in isolation.
            var discovered = new List<Chain>();
            var scratch = new bool[edges.Edges.Length];

            for (var k = 0; k < order.Count; k++)
            {
                System.Array.Clear(scratch, 0, scratch.Length);
                var chain = GrowFromSeed(edges, weights, scratch, settings, order[k]);
                chainsGrown++;
                longestChainMeters = Mathf.Max(longestChainMeters, chain.LengthMeters);
                if (chain.LengthMeters >= minLengthMeters)
                {
                    discovered.Add(chain);
                }
            }

            discovered.Sort((a, b) =>
            {
                var byLength = b.LengthMeters.CompareTo(a.LengthMeters);
                return byLength != 0 ? byLength : a.SeedEdge.CompareTo(b.SeedEdge);
            });

            var chains = new List<Chain>();
            for (var i = 0; i < discovered.Count; i++)
            {
                var chain = discovered[i];
                var overlaps = false;
                for (var e = 0; e < chain.Edges.Count && !overlaps; e++)
                {
                    overlaps = used[chain.Edges[e]];
                }

                if (overlaps)
                {
                    continue;
                }

                for (var e = 0; e < chain.Edges.Count; e++)
                {
                    used[chain.Edges[e]] = true;
                }

                chains.Add(chain);
            }

            return chains;
        }

        private static List<Chain> GrowChainsFromVertex(
            CrustalEdges edges,
            float[] weights,
            bool[] used,
            TerrainGenSettings settings,
            int vertex,
            float minLengthMeters)
        {
            var chains = new List<Chain>();
            var candidates = edges.VertexEdges[vertex];
            for (var i = 0; i < candidates.Length; i++)
            {
                var seed = candidates[i];
                if (used[seed] || weights[seed] < ConvergenceThreshold)
                {
                    continue;
                }

                var chain = GrowFromSeed(edges, weights, used, settings, seed);
                if (chain.LengthMeters >= minLengthMeters)
                {
                    chains.Add(chain);
                }
                else
                {
                    for (var e = 0; e < chain.Edges.Count; e++)
                    {
                        used[chain.Edges[e]] = false;
                    }
                }
            }

            return chains;
        }

        private static Chain GrowFromSeed(
            CrustalEdges edges,
            float[] weights,
            bool[] used,
            TerrainGenSettings settings,
            int seed)
        {
            var chain = new Chain { SeedEdge = seed };
            used[seed] = true;
            chain.Edges.Add(seed);

            var startVertex = edges.EndpointVertex[seed * 2];
            var endVertex = edges.EndpointVertex[seed * 2 + 1];
            chain.Vertices.Add(startVertex);
            chain.Vertices.Add(endVertex);
            chain.LengthMeters = edges.Edges[seed].Length;

            Extend(edges, weights, used, settings, chain, forward: true);
            Extend(edges, weights, used, settings, chain, forward: false);
            return chain;
        }

        private static void Extend(
            CrustalEdges edges,
            float[] weights,
            bool[] used,
            TerrainGenSettings settings,
            Chain chain,
            bool forward)
        {
            var maxTurn = Mathf.Cos(settings.MaxSpineTurnDegrees * Mathf.Deg2Rad);

            while (chain.Edges.Count < MaxChainEdges && chain.LengthMeters < MaxChainLengthMeters)
            {
                var tipIndex = forward ? chain.Vertices.Count - 1 : 0;
                var priorIndex = forward ? chain.Vertices.Count - 2 : 1;
                var tip = chain.Vertices[tipIndex];
                var prior = chain.Vertices[priorIndex];

                var tipEdge = forward ? chain.Edges[chain.Edges.Count - 1] : chain.Edges[0];
                var incoming = (VertexPoint(edges, tip) - VertexPoint(edges, prior)).normalized;

                var bestEdge = -1;
                var bestScore = float.NegativeInfinity;
                var bestOther = -1;

                var candidates = edges.VertexEdges[tip];
                for (var i = 0; i < candidates.Length; i++)
                {
                    var candidate = candidates[i];
                    if (used[candidate] || weights[candidate] < ConvergenceThreshold)
                    {
                        continue;
                    }

                    var other = OtherVertex(edges, candidate, tip);
                    if (other < 0 || chain.Vertices.Contains(other))
                    {
                        continue;
                    }

                    // The belt has to stay a walk along block boundaries: at a triple junction it
                    // turns onto another boundary of one of the same two blocks. Endpoints are
                    // snapped by proximity rather than matched exactly, so without this a chain can
                    // hop between two boundaries that merely happen to end near each other and share
                    // no crustal block at all.
                    if (!SharesSite(edges.Edges[tipEdge], edges.Edges[candidate]))
                    {
                        continue;
                    }


                    var outgoing = (VertexPoint(edges, other) - VertexPoint(edges, tip)).normalized;
                    var alignment = Vector2.Dot(incoming, outgoing);
                    if (alignment < maxTurn)
                    {
                        continue;
                    }

                    // Convergence decides among comparably straight continuations; straightness
                    // keeps the belt from folding back through the crustal vertex it just left.
                    var straightness = Mathf.InverseLerp(maxTurn, 1f, alignment);
                    var score = weights[candidate] * WeightShare + straightness * (1f - WeightShare);
                    if (score <= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    bestEdge = candidate;
                    bestOther = other;
                }

                if (bestEdge < 0)
                {
                    return;
                }

                used[bestEdge] = true;
                chain.LengthMeters += edges.Edges[bestEdge].Length;

                // Edges are kept in path order, not in the order they were found, so the recorded
                // provenance really is the chain the spine follows: consecutive boundaries share a
                // crustal site, and a reader can check that.
                if (forward)
                {
                    chain.Edges.Add(bestEdge);
                    chain.Vertices.Add(bestOther);
                }
                else
                {
                    chain.Edges.Insert(0, bestEdge);
                    chain.Vertices.Insert(0, bestOther);
                }
            }
        }

        /// <summary>True when two boundaries have a crustal block in common.</summary>
        private static bool SharesSite(CrustalEdges.Edge a, CrustalEdges.Edge b) =>
            a.SiteA == b.SiteA || a.SiteA == b.SiteB || a.SiteB == b.SiteA || a.SiteB == b.SiteB;

        private static Vector2 VertexPoint(CrustalEdges edges, int vertex)
        {
            // Any endpoint mapped to this vertex will do - they agree to the snapping tolerance -
            // and taking the first edge's matching endpoint keeps it a pure function of the vertex.
            var edge = edges.VertexEdges[vertex][0];
            return edges.EndpointVertex[edge * 2] == vertex ? edges.Edges[edge].A : edges.Edges[edge].B;
        }

        private static int OtherVertex(CrustalEdges edges, int edge, int vertex)
        {
            var a = edges.EndpointVertex[edge * 2];
            var b = edges.EndpointVertex[edge * 2 + 1];
            if (a == vertex)
            {
                return b;
            }

            return b == vertex ? a : -1;
        }

        private static MountainRange BuildRange(
            Seed.IWorldSeed worldSeed,
            TerrainGenSettings settings,
            CrustalEdges edges,
            Chain chain,
            int index,
            int parentIndex,
            float crestScale,
            float widthScale,
            CoastQuery coast)
        {
            var raw = new List<Vector2>(chain.Vertices.Count);
            for (var i = 0; i < chain.Vertices.Count; i++)
            {
                raw.Add(VertexPoint(edges, chain.Vertices[i]));
            }

            if (raw.Count < 2)
            {
                return null;
            }

            var spine = Smooth(raw, coast);
            if (spine.Length < 2)
            {
                return null;
            }

            spine = Resample(spine, SpineSampleMeters);
            if (spine.Length < 2)
            {
                return null;
            }

            var rnd = worldSeed.Fork("terrain.range", index);

            var crestHeight = Mathf.Lerp(
                settings.CrestHeightMinMeters,
                settings.CrestHeightMaxMeters,
                (float)rnd.NextDouble()) * crestScale;

            var spineLength = PolylineLength(spine);

            // A belt has to be clearly longer than it is wide or it reads as a hill, and the honest
            // way to hold that is to let the width follow the length that was actually found rather
            // than to lower the length a belt must reach. A short structural path becomes a narrow
            // ridge, not a dome.
            var halfWidth = Mathf.Lerp(
                settings.RangeHalfWidthMinMeters,
                settings.RangeHalfWidthMaxMeters,
                (float)rnd.NextDouble()) * widthScale;
            halfWidth = Mathf.Min(halfWidth, spineLength / settings.MinLengthToHalfWidthRatio);

            // The axis has to be smooth enough to carry the belt's width. A chain of crustal
            // boundaries, corner-cut and resampled, still wanders by a few metres between samples,
            // and the reach of such a polyline collapses to tens of metres however wide the belt is
            // meant to be - which then crushes the belt to a wall rather than fixing anything. A
            // mountain 450 m across does not have 15 m kinks in its axis, so the axis is relaxed
            // until it can carry the width, and what that cost is reported rather than hidden.
            spine = RelaxUntilCarrying(spine, halfWidth / MountainRange.CurvatureWidthFactor);
            spineLength = PolylineLength(spine);
            halfWidth = Mathf.Min(halfWidth, spineLength / settings.MinLengthToHalfWidthRatio);

            var asymmetry = Mathf.Lerp(
                settings.RangeAsymmetryMin,
                settings.RangeAsymmetryMax,
                (float)rnd.NextDouble());

            var steepSide = rnd.NextDouble() < 0.5 ? -1 : 1;

            // The summit is never at the midpoint. A range whose highest point sits exactly halfway
            // along it is one of the artificiality tells WORLD_RELIEF_PRINCIPLES bans by name.
            var lowHalf = rnd.NextDouble() < 0.5;
            var peakT = lowHalf
                ? Mathf.Lerp(0.28f, 0.44f, (float)rnd.NextDouble())
                : Mathf.Lerp(0.56f, 0.72f, (float)rnd.NextDouble());
            var subT = lowHalf
                ? Mathf.Lerp(0.62f, 0.82f, (float)rnd.NextDouble())
                : Mathf.Lerp(0.18f, 0.38f, (float)rnd.NextDouble());

            // The shoulders sit high on purpose. With a dominant summit and low shoulders the belt
            // reads as one dome with foothills; keeping the crest above two thirds of its maximum
            // between the two summits is what makes it read as a ridge line that happens to have a
            // high point.
            var subValue = Mathf.Lerp(0.72f, 0.90f, (float)rnd.NextDouble());
            var saddleValue = Mathf.Lerp(0.60f, 0.74f, (float)rnd.NextDouble());
            var saddleT = (peakT + subT) * 0.5f;

            var crest = BuildCrestProfile(peakT, subT, saddleT, subValue, saddleValue);
            var width = new ProfileCurve(
                new[] { 0f, peakT, 1f },
                new[]
                {
                    Mathf.Lerp(0.48f, 0.62f, (float)rnd.NextDouble()),
                    1f,
                    Mathf.Lerp(0.44f, 0.58f, (float)rnd.NextDouble()),
                });

            var pairs = new int[chain.Edges.Count * 2];
            for (var i = 0; i < chain.Edges.Count; i++)
            {
                pairs[i * 2] = edges.Edges[chain.Edges[i]].SiteA;
                pairs[i * 2 + 1] = edges.Edges[chain.Edges[i]].SiteB;
            }

            return new MountainRange(
                index, parentIndex, spine, crest, width,
                crestHeight, halfWidth, asymmetry, steepSide, pairs);
        }

        /// <summary>
        /// The crest profile: foothills, a dominant summit off-centre, a saddle, and a subsidiary
        /// summit, tapering to nothing at both ends.
        /// </summary>
        /// <remarks>
        /// Tapering to zero at the ends is what produces foothills instead of a range that stops
        /// mid-slope, and the second summit is what stops a belt reading as a single smooth arch.
        /// Both are authored control points, not a second field summed on top.
        /// </remarks>
        private static ProfileCurve BuildCrestProfile(
            float peakT, float subT, float saddleT, float subValue, float saddleValue)
        {
            var keys = new List<(float t, float v)>
            {
                (0f, 0f),
                (0.08f, 0.35f),
                (peakT, 1f),
                (saddleT, saddleValue),
                (subT, subValue),
                (0.92f, 0.32f),
                (1f, 0f),
            };

            keys.Sort((a, b) => a.t.CompareTo(b.t));

            var t = new List<float>(keys.Count);
            var v = new List<float>(keys.Count);
            for (var i = 0; i < keys.Count; i++)
            {
                // ProfileCurve requires strictly increasing keys; a peak that lands on an anchor is
                // nudged rather than dropped, so the shape survives the collision.
                if (t.Count > 0 && keys[i].t <= t[t.Count - 1] + 1e-4f)
                {
                    continue;
                }

                t.Add(keys[i].t);
                v.Add(keys[i].v);
            }

            return new ProfileCurve(t.ToArray(), v.ToArray());
        }

        /// <summary>
        /// Corner-cutting on an open polyline, keeping both ends.
        /// </summary>
        /// <remarks>
        /// A chain of crustal boundaries turns by roughly 60 degrees at every vertex, because three
        /// boundaries meet there. Left alone that reads as a zigzag, not a range. Smoothed points
        /// that leave the landmass are pulled back to the chain vertex they came from - the belt is a
        /// feature of the land and its axis has to stay on it.
        /// </remarks>
        private static Vector2[] Smooth(List<Vector2> raw, CoastQuery coast)
        {
            var current = new List<Vector2>(raw);

            for (var pass = 0; pass < SmoothingPasses; pass++)
            {
                if (current.Count < 3)
                {
                    break;
                }

                var next = new List<Vector2>(current.Count * 2) { current[0] };
                for (var i = 0; i + 1 < current.Count; i++)
                {
                    var a = current[i];
                    var b = current[i + 1];
                    next.Add(a + (b - a) * 0.25f);
                    next.Add(a + (b - a) * 0.75f);
                }

                next.Add(current[current.Count - 1]);
                current = next;
            }

            if (coast != null)
            {
                for (var i = 0; i < current.Count; i++)
                {
                    if (coast.IsInside(current[i].x, current[i].y))
                    {
                        continue;
                    }

                    current[i] = NearestOf(raw, current[i]);
                }
            }

            return current.ToArray();
        }

        private static float PolylineLength(Vector2[] points)
        {
            var total = 0f;
            for (var i = 0; i + 1 < points.Length; i++)
            {
                total += (points[i + 1] - points[i]).magnitude;
            }

            return total;
        }

        /// <summary>
        /// Relaxes the axis until a tube of the given half-width fits around it without folding.
        /// </summary>
        /// <remarks>
        /// Laplacian relaxation with the ends pinned, re-measured after every pass against the same
        /// reach the range itself uses, and stopped the moment the condition holds. That stopping
        /// rule is the difference between correcting the geometry and blurring it: the axis is
        /// relaxed exactly as far as the belt's own width requires and no further.
        /// </remarks>
        /// <summary>How the last axis relaxation went. Read by the diagnostics, not by generation.</summary>
        public static string LastRelaxReport { get; private set; } = string.Empty;

        private static Vector2[] RelaxUntilCarrying(Vector2[] spine, float requiredReach)
        {
            const int maxPasses = 80;
            var current = spine;
            var before = MinReach(current);

            for (var pass = 0; pass < maxPasses; pass++)
            {
                var reach = MinReach(current);
                if (reach >= requiredReach)
                {
                    LastRelaxReport = $"passes={pass} reach {before:F0}->{reach:F0} "
                                      + $"required={requiredReach:F0} converged";
                    return Resample(current, SpineSampleMeters);
                }

                var next = new Vector2[current.Length];
                next[0] = current[0];
                next[current.Length - 1] = current[current.Length - 1];
                for (var i = 1; i + 1 < current.Length; i++)
                {
                    next[i] = current[i] * 0.5f + (current[i - 1] + current[i + 1]) * 0.25f;
                }

                // Deliberately no resampling inside the loop. Resampling a polyline by linear
                // interpolation puts corners back at the sample spacing, so the reach never improved
                // and the loop ran to its cap while the belts were crushed to fins - measured at
                // 73 m of half-width against a 262 m nominal, with the cap binding on all of the
                // range. The point set stays fixed and is resampled once at the end.
                current = next;
            }

            LastRelaxReport = $"passes={maxPasses} reach {before:F0}->{MinReach(current):F0} "
                              + $"required={requiredReach:F0} NOT CONVERGED";
            return Resample(current, SpineSampleMeters);
        }

        /// <summary>Smallest reach over the polyline, by the same measure the range uses.</summary>
        private static float MinReach(Vector2[] spine)
        {
            var smallest = float.MaxValue;
            for (var i = 0; i < spine.Length; i++)
            {
                var ahead = spine[Mathf.Min(spine.Length - 1, i + 1)];
                var behind = spine[Mathf.Max(0, i - 1)];
                var direction = ahead - behind;
                var t = direction.sqrMagnitude < 1e-10f ? Vector2.right : direction.normalized;

                for (var j = 0; j < spine.Length; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    var v = spine[j] - spine[i];
                    var lengthSq = v.sqrMagnitude;
                    if (lengthSq < 1e-6f)
                    {
                        continue;
                    }

                    var offAxis = Mathf.Abs(t.x * v.y - t.y * v.x);
                    if (offAxis < 1e-4f)
                    {
                        continue;
                    }

                    smallest = Mathf.Min(smallest, lengthSq / (2f * offAxis));
                }
            }

            return smallest;
        }

        /// <summary>Resamples a polyline to a uniform step, keeping both endpoints.</summary>
        private static Vector2[] Resample(Vector2[] points, float stepMeters)
        {
            var total = 0f;
            for (var i = 0; i + 1 < points.Length; i++)
            {
                total += (points[i + 1] - points[i]).magnitude;
            }

            if (total <= stepMeters || stepMeters <= 0f)
            {
                return points;
            }

            var count = Mathf.Max(2, Mathf.RoundToInt(total / stepMeters) + 1);
            var result = new Vector2[count];
            result[0] = points[0];
            result[count - 1] = points[points.Length - 1];

            var segment = 0;
            var walked = 0f;
            var segmentLength = (points[1] - points[0]).magnitude;

            for (var i = 1; i < count - 1; i++)
            {
                var target = total * i / (count - 1);
                while (segment + 2 < points.Length && walked + segmentLength < target)
                {
                    walked += segmentLength;
                    segment++;
                    segmentLength = (points[segment + 1] - points[segment]).magnitude;
                }

                var u = segmentLength < 1e-5f ? 0f : Mathf.Clamp01((target - walked) / segmentLength);
                result[i] = Vector2.LerpUnclamped(points[segment], points[segment + 1], u);
            }

            return result;
        }

        private static Vector2 NearestOf(List<Vector2> points, Vector2 p)
        {
            var best = points[0];
            var bestSq = float.PositiveInfinity;
            for (var i = 0; i < points.Count; i++)
            {
                var sq = (points[i] - p).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = points[i];
                }
            }

            return best;
        }
    }
}

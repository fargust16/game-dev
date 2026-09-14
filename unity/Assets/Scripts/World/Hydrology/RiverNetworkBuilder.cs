using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Hydrology
{
    /// <summary>Reaches and lakes, wired to each other.</summary>
    public sealed class RiverNetwork
    {
        public static readonly RiverNetwork Empty =
            new RiverNetwork(Array.Empty<RiverReach>(), Array.Empty<LakeBasin>(), 0);

        public IReadOnlyList<RiverReach> Reaches { get; }
        public IReadOnlyList<LakeBasin> Lakes { get; }

        /// <summary>Channel cells that had nothing draining into them — the sources.</summary>
        public int SourceCount { get; }

        public RiverNetwork(
            IReadOnlyList<RiverReach> reaches, IReadOnlyList<LakeBasin> lakes, int sourceCount)
        {
            Reaches = reaches;
            Lakes = lakes;
            SourceCount = sourceCount;
        }
    }

    /// <summary>
    /// Cuts one shared drainage network into reaches, and connects the lakes to them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rivers are not generated one at a time. The flow accumulation field already contains every
    /// watercourse in the world at once; this class only decides which parts of it are large enough
    /// to draw and where to cut the result into named segments. That is what makes main stems,
    /// tributaries, mountain streams and lowland rivers different sizes of the same thing rather
    /// than separate categories somebody had to invent. See
    /// <c>HYDROLOGY_GENERATION_PLAN.md</c> §10.
    /// </para>
    /// <para>
    /// Density has exactly one knob, <see cref="HydrologySettings.ChannelInitiationAreaSqm"/>.
    /// Raising it removes the smallest headwaters first, and it cannot produce parallel duplicate
    /// networks because D8 gives every cell one downstream neighbour.
    /// </para>
    /// </remarks>
    public static class RiverNetworkBuilder
    {
        /// <summary>Deterministic seed stream this stage draws from.</summary>
        public const string SeedStream = "hydrology";

        /// <summary>
        /// Fall forced between consecutive nodes when the sampled ground would otherwise rise.
        /// </summary>
        /// <remarks>
        /// A millimetre. The centreline follows steepest descent on the filled surface, so the raw
        /// ground under it is already descending almost everywhere; this only bridges the shallow
        /// filled dimples that were not big enough to become lakes. Making it any larger would let a
        /// long flat reach dig itself an artificial trench.
        /// </remarks>
        private const float MinimumFallPerNodeMeters = 0.001f;

        /// <summary>Chaikin passes over the raw staircase of D8 cell centres.</summary>
        private const int SmoothingPasses = 2;

        /// <summary>Meander amplitude as a fraction of the channel half width.</summary>
        /// <remarks>
        /// The one place the hydrology seed genuinely decides something. A D8 path is a staircase on
        /// a lattice, and smoothing alone leaves it reading as an axis-aligned zigzag; a low-frequency
        /// sideways offset breaks that up. It is capped well inside the channel so a meander can
        /// never carry the water out of the valley the same numbers carved.
        /// </remarks>
        private const float MeanderAmplitudeFraction = 0.55f;

        private const float MeanderWavelengthMeters = 140f;

        /// <summary>Distance over which a lake's outflow works up to its full carve depth.</summary>
        /// <remarks>
        /// A lake exists because its rim holds water back, so cutting a full valley through the spill
        /// would drain the basin the analysis just measured — and leave the water surface, which was
        /// computed from the pre-carve spill, floating over a gorge. Real lake outlets are sills for
        /// the same reason. The taper is comfortably longer than the widest valley shoulder, so by
        /// the time the channel reaches full depth its side slopes no longer reach back to the rim.
        /// </remarks>
        private const float LakeOutletTaperMeters = 220f;

        public static RiverNetwork Build(
            HydrologyGrid grid,
            float[] filled,
            bool[] oceanMask,
            byte[] directions,
            float[] accumulation,
            LakeField lakeField,
            HydrologySettings settings,
            Random rng)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            var isChannel = BuildChannelMask(grid, oceanMask, directions, accumulation, lakeField, settings);
            var inDegree = BuildChannelInDegree(grid, directions, isChannel);
            var startCells = CollectReachStarts(grid, directions, isChannel, inDegree, lakeField);

            var reachIndexByStart = new Dictionary<int, int>(startCells.Count);
            for (var i = 0; i < startCells.Count; i++)
            {
                reachIndexByStart[startCells[i]] = i;
            }

            var paths = new List<int>[startCells.Count];
            var terminations = new ReachTermination[startCells.Count];
            var downstreamReach = new int[startCells.Count];
            var downstreamLake = new int[startCells.Count];

            for (var i = 0; i < startCells.Count; i++)
            {
                downstreamReach[i] = -1;
                downstreamLake[i] = -1;
                paths[i] = TracePath(
                    grid,
                    directions,
                    isChannel,
                    inDegree,
                    oceanMask,
                    lakeField,
                    startCells[i],
                    reachIndexByStart,
                    out terminations[i],
                    out downstreamReach[i],
                    out downstreamLake[i]);
            }

            var orders = ComputeStrahlerOrders(paths, downstreamReach);
            var lakeOutletReach = BuildLakeOutletReachIndices(
                grid, directions, lakeField, reachIndexByStart);
            var keep = SelectRiverSystems(
                paths, downstreamReach, downstreamLake, lakeOutletReach, accumulation, settings);
            var meanderPhase = rng != null ? (float)(rng.NextDouble() * Mathf.PI * 2f) : 0f;

            var reaches = new List<RiverReach>(startCells.Count);
            for (var i = 0; i < startCells.Count; i++)
            {
                var path = keep[i] ? paths[i] : null;
                if (path == null || path.Count < 2)
                {
                    // A one-cell reach carries no geometry and cannot be smoothed or carved. Dropping
                    // it here rather than emitting a degenerate polyline keeps every consumer simple;
                    // its drainage still reaches the sea through the reach below it.
                    reaches.Add(null);
                    continue;
                }

                var lake = downstreamLake[i] >= 0 ? lakeField.Lakes[downstreamLake[i]] : null;
                var lakeAtStart = FindLakeFeedingStart(grid, directions, lakeField, startCells[i]);
                var areaAtOutflow = accumulation[path[path.Count - 1]];

                var nodes = BuildNodes(
                    grid, accumulation, path, settings, meanderPhase, lakeAtStart, lake);
                if (nodes.Count < 2)
                {
                    reaches.Add(null);
                    continue;
                }

                reaches.Add(new RiverReach(
                    new ReachId(i),
                    downstreamReach[i] >= 0 ? new ReachId(downstreamReach[i]) : ReachId.None,
                    downstreamLake[i] >= 0 ? new LakeId(downstreamLake[i]) : LakeId.None,
                    nodes,
                    orders[i],
                    areaAtOutflow,
                    settings.ChannelHalfWidth(areaAtOutflow) * 2f,
                    terminations[i],
                    inDegree[startCells[i]] == 0 && lakeAtStart == null));
            }

            var kept = Compact(reaches, downstreamReach, out var remap);
            var lakes = BuildLakes(
                grid, directions, oceanMask, lakeField, reaches, downstreamReach, remap,
                downstreamLake, reachIndexByStart);

            var sourceCount = 0;
            for (var i = 0; i < kept.Count; i++)
            {
                if (kept[i].IsSource)
                {
                    sourceCount++;
                }
            }

            return kept.Count == 0 && lakes.Count == 0
                ? RiverNetwork.Empty
                : new RiverNetwork(kept, lakes, sourceCount);
        }

        /// <summary>
        /// Cells large enough to carry a mapped watercourse.
        /// </summary>
        /// <remarks>
        /// Lake interiors are excluded — a lake is water, not a river running through water — and
        /// every lake's outflow is then forced into the mask whatever its contributing area, because
        /// a body of water big enough to qualify as a lake visibly drains somewhere and the outlet
        /// is what carries the ocean-outlet invariant across it.
        /// </remarks>
        private static bool[] BuildChannelMask(
            HydrologyGrid grid,
            bool[] oceanMask,
            byte[] directions,
            float[] accumulation,
            LakeField lakeField,
            HydrologySettings settings)
        {
            var isChannel = new bool[grid.CellCount];
            for (var i = 0; i < grid.CellCount; i++)
            {
                isChannel[i] = !oceanMask[i]
                    && !lakeField.IsLake(i)
                    && accumulation[i] >= settings.ChannelInitiationAreaSqm;
            }

            for (var l = 0; l < lakeField.Lakes.Count; l++)
            {
                var cell = FlowRouting.Downstream(
                    grid, lakeField.Lakes[l].SpillCell, directions[lakeField.Lakes[l].SpillCell]);
                var guard = grid.CellCount;
                while (cell >= 0 && guard-- > 0)
                {
                    if (oceanMask[cell] || lakeField.IsLake(cell) || isChannel[cell])
                    {
                        break;
                    }

                    isChannel[cell] = true;
                    cell = FlowRouting.Downstream(grid, cell, directions[cell]);
                }
            }

            return isChannel;
        }

        private static int[] BuildChannelInDegree(
            HydrologyGrid grid, byte[] directions, bool[] isChannel)
        {
            var inDegree = new int[grid.CellCount];
            for (var i = 0; i < grid.CellCount; i++)
            {
                if (!isChannel[i])
                {
                    continue;
                }

                var target = FlowRouting.Downstream(grid, i, directions[i]);
                if (target >= 0 && isChannel[target])
                {
                    inDegree[target]++;
                }
            }

            return inDegree;
        }

        /// <summary>
        /// Cells a reach begins at: a source, a confluence, or the first cell below a lake.
        /// </summary>
        /// <remarks>
        /// Returned in ascending cell index, which is what fixes reach ids: the same terrain always
        /// produces the same numbering, without the ids depending on hash iteration order anywhere.
        /// </remarks>
        private static List<int> CollectReachStarts(
            HydrologyGrid grid, byte[] directions, bool[] isChannel, int[] inDegree, LakeField lakeField)
        {
            var starts = new HashSet<int>();
            for (var i = 0; i < grid.CellCount; i++)
            {
                if (isChannel[i] && inDegree[i] != 1)
                {
                    starts.Add(i);
                }
            }

            for (var l = 0; l < lakeField.Lakes.Count; l++)
            {
                var spill = lakeField.Lakes[l].SpillCell;
                var outlet = FlowRouting.Downstream(grid, spill, directions[spill]);
                if (outlet >= 0 && isChannel[outlet])
                {
                    starts.Add(outlet);
                }
            }

            var list = new List<int>(starts);
            list.Sort();
            return list;
        }

        private static List<int> TracePath(
            HydrologyGrid grid,
            byte[] directions,
            bool[] isChannel,
            int[] inDegree,
            bool[] oceanMask,
            LakeField lakeField,
            int startCell,
            IReadOnlyDictionary<int, int> reachIndexByStart,
            out ReachTermination termination,
            out int downstreamReachIndex,
            out int downstreamLakeIndex)
        {
            var path = new List<int> { startCell };
            termination = ReachTermination.Ocean;
            downstreamReachIndex = -1;
            downstreamLakeIndex = -1;

            var current = startCell;
            var guard = grid.CellCount;

            while (guard-- > 0)
            {
                var next = FlowRouting.Downstream(grid, current, directions[current]);
                if (next < 0 || oceanMask[next])
                {
                    termination = ReachTermination.Ocean;
                    return path;
                }

                if (lakeField.IsLake(next))
                {
                    termination = ReachTermination.Lake;
                    downstreamLakeIndex = lakeField.Labels[next];
                    return path;
                }

                if (!isChannel[next])
                {
                    // Cannot happen: accumulation never falls downstream, so a channel cell's target
                    // is a channel cell, the sea or a lake. Treated as an ocean termination so a
                    // corrupted field produces a validation failure instead of an infinite walk.
                    termination = ReachTermination.Ocean;
                    return path;
                }

                if (inDegree[next] >= 2 && reachIndexByStart.TryGetValue(next, out var index))
                {
                    termination = ReachTermination.Confluence;
                    downstreamReachIndex = index;
                    return path;
                }

                path.Add(next);
                current = next;
            }

            return path;
        }

        /// <summary>
        /// Decides which whole drainage systems are worth drawing as rivers.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Without this an island grows a watercourse at every coastal gully — the «dozens of
        /// parallel rivers» failure the requirements call out. The judgement is made per <b>system</b>
        /// and applies to the entire tree above an outlet, so a system is either drawn whole or not
        /// at all; truncating one would leave a river ending inland.
        /// </para>
        /// <para>
        /// Two criteria, both geographic. An absolute floor rules out catchments too small to carry a
        /// permanent stream, and a relative one keeps only systems comparable to the largest on the
        /// same landmass — the standard way a map decides which watercourses to show, and the part
        /// that stops the count swinging when the island's size changes between seeds.
        /// </para>
        /// <para>
        /// A system holding a lake is always kept. A lake is a body of water big enough to have
        /// passed its own area and depth filters, and its outflow is what carries the ocean-outlet
        /// invariant across it — dropping that outlet would leave a lake draining into nothing.
        /// </para>
        /// </remarks>
        /// <summary>The reach index each lake spills into, or -1.</summary>
        private static int[] BuildLakeOutletReachIndices(
            HydrologyGrid grid,
            byte[] directions,
            LakeField lakeField,
            IReadOnlyDictionary<int, int> reachIndexByStart)
        {
            var outlets = new int[lakeField.Lakes.Count];
            for (var l = 0; l < lakeField.Lakes.Count; l++)
            {
                var spill = lakeField.Lakes[l].SpillCell;
                var cell = FlowRouting.Downstream(grid, spill, directions[spill]);
                outlets[l] = cell >= 0 && reachIndexByStart.TryGetValue(cell, out var index)
                    ? index
                    : -1;
            }

            return outlets;
        }

        private static bool[] SelectRiverSystems(
            List<int>[] paths,
            int[] downstreamReach,
            int[] downstreamLake,
            int[] lakeOutletReach,
            float[] accumulation,
            HydrologySettings settings)
        {
            var count = paths.Length;
            var keep = new bool[count];
            if (count == 0)
            {
                return keep;
            }

            // A lake is part of the system that flows through it, so the walk hops from an inlet to
            // the lake's outflow. Without that hop every tributary above a lake would be judged as a
            // system of its own and dropped for being too small.
            var next = new int[count];
            for (var i = 0; i < count; i++)
            {
                if (downstreamReach[i] >= 0)
                {
                    next[i] = downstreamReach[i];
                }
                else if (downstreamLake[i] >= 0 && downstreamLake[i] < lakeOutletReach.Length)
                {
                    next[i] = lakeOutletReach[downstreamLake[i]];
                }
                else
                {
                    next[i] = -1;
                }
            }

            var root = new int[count];
            for (var i = 0; i < count; i++)
            {
                var current = i;
                var guard = count + 1;
                while (next[current] >= 0 && guard-- > 0)
                {
                    current = next[current];
                }

                root[i] = current;
            }

            var rootArea = new float[count];
            var rootHasLake = new bool[count];
            var largest = 0f;

            // Both halves of "a system holding a lake is always kept": the reaches that feed a lake,
            // and the outflow that drains it. Marking only the inlets leaves a lake whose outlet was
            // pruned draining into nothing, which is the one thing a filled basin must never do.
            for (var l = 0; l < lakeOutletReach.Length; l++)
            {
                if (lakeOutletReach[l] >= 0)
                {
                    rootHasLake[root[lakeOutletReach[l]]] = true;
                }
            }

            for (var i = 0; i < count; i++)
            {
                var path = paths[i];
                if (downstreamLake[i] >= 0)
                {
                    rootHasLake[root[i]] = true;
                }

                if (root[i] != i || path == null || path.Count == 0)
                {
                    continue;
                }

                rootArea[i] = accumulation[path[path.Count - 1]];
                if (rootArea[i] > largest)
                {
                    largest = rootArea[i];
                }
            }

            var threshold = Mathf.Max(
                settings.MinRiverSystemAreaSqm,
                largest * settings.MinRiverSystemFractionOfLargest);

            for (var i = 0; i < count; i++)
            {
                var r = root[i];
                keep[i] = rootHasLake[r] || rootArea[r] >= threshold;
            }

            return keep;
        }

        /// <summary>
        /// Strahler order over the reach tree: two equal orders meeting make the next one up.
        /// </summary>
        /// <remarks>
        /// Computed by counting how many upstream reaches each one has and resolving them in
        /// dependency order, so a deep network cannot recurse into a stack overflow. The order is
        /// what the renderer and the carve read to decide how big a river looks — nothing assigns a
        /// river a rank by hand.
        /// </remarks>
        private static int[] ComputeStrahlerOrders(List<int>[] paths, int[] downstreamReach)
        {
            var count = paths.Length;
            var orders = new int[count];
            var pending = new int[count];
            for (var i = 0; i < count; i++)
            {
                var target = downstreamReach[i];
                if (target >= 0)
                {
                    pending[target]++;
                }
            }

            var maxUpstream = new int[count];
            var maxUpstreamCount = new int[count];
            var ready = new Stack<int>();
            for (var i = 0; i < count; i++)
            {
                if (pending[i] == 0)
                {
                    ready.Push(i);
                }
            }

            while (ready.Count > 0)
            {
                var i = ready.Pop();
                orders[i] = maxUpstream[i] == 0
                    ? 1
                    : maxUpstreamCount[i] >= 2 ? maxUpstream[i] + 1 : maxUpstream[i];

                var target = downstreamReach[i];
                if (target < 0)
                {
                    continue;
                }

                if (orders[i] > maxUpstream[target])
                {
                    maxUpstream[target] = orders[i];
                    maxUpstreamCount[target] = 1;
                }
                else if (orders[i] == maxUpstream[target])
                {
                    maxUpstreamCount[target]++;
                }

                if (--pending[target] == 0)
                {
                    ready.Push(target);
                }
            }

            return orders;
        }

        /// <summary>The lake this reach drains, or null when it starts at a source or a confluence.</summary>
        /// <remarks>
        /// Decided by the flow field, not by adjacency: a reach that merely runs past a shore is not
        /// that lake's outflow, and treating it as one would start it at the wrong water level.
        /// </remarks>
        private static LakeCandidate FindLakeFeedingStart(
            HydrologyGrid grid, byte[] directions, LakeField lakeField, int startCell)
        {
            for (var l = 0; l < lakeField.Lakes.Count; l++)
            {
                var spill = lakeField.Lakes[l].SpillCell;
                if (FlowRouting.Downstream(grid, spill, directions[spill]) == startCell)
                {
                    return lakeField.Lakes[l];
                }
            }

            return null;
        }

        /// <summary>
        /// Turns a staircase of cell centres into evenly spaced, smoothed, strictly descending nodes.
        /// </summary>
        private static List<RiverNode> BuildNodes(
            HydrologyGrid grid,
            float[] accumulation,
            List<int> path,
            HydrologySettings settings,
            float meanderPhase,
            LakeCandidate lakeAtStart,
            LakeCandidate lakeAtEnd)
        {
            var raw = new Vector2[path.Count];
            var rawArea = new float[path.Count];
            for (var i = 0; i < path.Count; i++)
            {
                raw[i] = grid.PositionXZ(path[i]);
                rawArea[i] = accumulation[path[i]];
            }

            var smoothed = Chaikin(raw, SmoothingPasses);
            var resampled = Resample(smoothed, settings.NodeSpacingMeters);
            ApplyMeander(grid, resampled, settings, rawArea[rawArea.Length - 1], meanderPhase);

            var nodes = new List<RiverNode>(resampled.Count);
            var totalLength = PolylineLength(resampled);
            var travelled = 0f;
            var previousBed = float.MaxValue;

            for (var i = 0; i < resampled.Count; i++)
            {
                if (i > 0)
                {
                    travelled += Vector2.Distance(resampled[i - 1], resampled[i]);
                }

                var t = totalLength > 0.001f ? travelled / totalLength : 0f;
                var area = Mathf.Lerp(rawArea[0], rawArea[rawArea.Length - 1], t);

                var ground = grid.SampleHeightWorld(resampled[i].x, resampled[i].y);
                var carve = settings.ValleyDepth(area) + settings.ChannelDepth(area);
                if (lakeAtStart != null)
                {
                    carve *= Mathf.SmoothStep(0f, 1f, travelled / LakeOutletTaperMeters);
                }

                var bed = ground - carve;

                // The centreline follows steepest descent, so this only bites inside the shallow
                // filled dimples that never became lakes. Clamped against the carve budget so a long
                // flat stretch cannot dig a trench deeper than the carve budget.
                if (bed > previousBed - MinimumFallPerNodeMeters)
                {
                    bed = previousBed - MinimumFallPerNodeMeters;
                }

                bed = Mathf.Max(bed, ground - settings.MaxCarveDepthMeters);
                previousBed = bed;

                var water = bed + settings.FlowDepth(area);
                nodes.Add(new RiverNode(resampled[i], bed, water, travelled, area));
            }

            if (lakeAtStart != null && nodes.Count > 0)
            {
                nodes[0] = WithWaterElevation(nodes[0], lakeAtStart.SurfaceElevation);
            }

            if (lakeAtEnd != null && nodes.Count > 0)
            {
                // Meeting the lake at its own surface, so the inlet does not read as a step down
                // into standing water that is actually higher than the stream.
                nodes[nodes.Count - 1] =
                    WithWaterElevation(nodes[nodes.Count - 1], lakeAtEnd.SurfaceElevation);
            }

            return nodes;
        }

        private static RiverNode WithWaterElevation(RiverNode node, float waterElevation) =>
            new RiverNode(
                node.PositionXZ,
                Mathf.Min(node.BedElevation, waterElevation - 0.2f),
                waterElevation,
                node.CumulativeDistance,
                node.ContributingAreaSqm);

        private static Vector2[] Chaikin(Vector2[] points, int passes)
        {
            var current = points;
            for (var pass = 0; pass < passes && current.Length > 2; pass++)
            {
                var next = new Vector2[(current.Length - 2) * 2 + 2];
                next[0] = current[0];
                var w = 1;
                for (var i = 0; i < current.Length - 1; i++)
                {
                    if (i > 0)
                    {
                        next[w++] = Vector2.Lerp(current[i], current[i + 1], 0.25f);
                    }

                    if (i < current.Length - 2)
                    {
                        next[w++] = Vector2.Lerp(current[i], current[i + 1], 0.75f);
                    }
                }

                next[w] = current[current.Length - 1];
                current = next;
            }

            return current;
        }

        private static List<Vector2> Resample(Vector2[] points, float spacing)
        {
            var result = new List<Vector2> { points[0] };
            if (points.Length < 2)
            {
                return result;
            }

            var carried = 0f;
            for (var i = 1; i < points.Length; i++)
            {
                var from = points[i - 1];
                var to = points[i];
                var segment = Vector2.Distance(from, to);
                if (segment < 1e-5f)
                {
                    continue;
                }

                var travelled = spacing - carried;
                while (travelled <= segment)
                {
                    result.Add(Vector2.Lerp(from, to, travelled / segment));
                    travelled += spacing;
                }

                carried = segment - (travelled - spacing);
            }

            var last = points[points.Length - 1];
            if (Vector2.Distance(result[result.Count - 1], last) > spacing * 0.35f)
            {
                result.Add(last);
            }
            else
            {
                result[result.Count - 1] = last;
            }

            return result;
        }

        private static void ApplyMeander(
            HydrologyGrid grid,
            List<Vector2> points,
            HydrologySettings settings,
            float areaSqm,
            float phase)
        {
            if (points.Count < 3)
            {
                return;
            }

            // A reach running along the world edge would otherwise be nudged off the grid, which the
            // validator correctly rejects as a river outside the world.
            var minX = grid.OriginX;
            var minZ = grid.OriginZ;
            var maxX = grid.OriginX + (grid.Width - 1) * grid.CellSize;
            var maxZ = grid.OriginZ + (grid.Height - 1) * grid.CellSize;

            var amplitude = settings.ChannelHalfWidth(areaSqm) * MeanderAmplitudeFraction;
            var travelled = 0f;
            for (var i = 1; i < points.Count - 1; i++)
            {
                travelled += Vector2.Distance(points[i - 1], points[i]);

                var tangent = (points[i + 1] - points[i - 1]).normalized;
                var normal = new Vector2(-tangent.y, tangent.x);
                var offset = Mathf.Sin(phase + travelled * (Mathf.PI * 2f / MeanderWavelengthMeters));
                var moved = points[i] + normal * (offset * amplitude);
                points[i] = new Vector2(
                    Mathf.Clamp(moved.x, minX, maxX), Mathf.Clamp(moved.y, minZ, maxZ));
            }
        }

        private static float PolylineLength(List<Vector2> points)
        {
            var length = 0f;
            for (var i = 1; i < points.Count; i++)
            {
                length += Vector2.Distance(points[i - 1], points[i]);
            }

            return length;
        }

        /// <summary>
        /// Drops the reaches that came out degenerate and renumbers the survivors.
        /// </summary>
        /// <remarks>
        /// Ids stay dense and stable, and a dropped reach's dependants are re-pointed at whatever it
        /// pointed to, so removing a one-cell stub cannot break the downstream chain the ocean
        /// invariant walks.
        /// </remarks>
        private static List<RiverReach> Compact(
            List<RiverReach> reaches, int[] downstreamReach, out int[] remap)
        {
            remap = new int[reaches.Count];
            var next = 0;
            for (var i = 0; i < reaches.Count; i++)
            {
                remap[i] = reaches[i] != null ? next++ : -1;
            }

            var result = new List<RiverReach>(next);
            for (var i = 0; i < reaches.Count; i++)
            {
                var reach = reaches[i];
                if (reach == null)
                {
                    continue;
                }

                var downstream = ReachId.None;
                var termination = reach.Termination;
                if (reach.DownstreamId.IsValid)
                {
                    var resolved = ResolveDownstream(
                        reaches, downstreamReach, remap, reach.DownstreamId.Value);
                    if (resolved >= 0)
                    {
                        downstream = new ReachId(resolved);
                    }
                    else
                    {
                        // Everything below was degenerate, so this reach now runs to the coast on
                        // its own. Reported as such rather than left dangling, which the ocean
                        // invariant would otherwise flag.
                        termination = ReachTermination.Ocean;
                    }
                }

                result.Add(new RiverReach(
                    new ReachId(remap[i]),
                    downstream,
                    reach.DownstreamLake,
                    reach.Nodes,
                    reach.StrahlerOrder,
                    reach.ContributingAreaSqm,
                    reach.WidthMeters,
                    termination,
                    reach.IsSource));
            }

            return result;
        }

        /// <summary>
        /// Follows the raw downstream chain past dropped reaches to the first survivor.
        /// </summary>
        /// <remarks>
        /// The raw link array is walked rather than the reach objects, because a dropped reach is
        /// null and no longer carries its own downstream id. Returns -1 when the whole chain below
        /// was dropped, which the caller turns into an ocean termination.
        /// </remarks>
        private static int ResolveDownstream(
            List<RiverReach> reaches, int[] downstreamReach, int[] remap, int index)
        {
            var guard = reaches.Count + 1;
            while (index >= 0 && guard-- > 0)
            {
                if (reaches[index] != null)
                {
                    return remap[index];
                }

                index = downstreamReach[index];
            }

            return -1;
        }

        private static List<LakeBasin> BuildLakes(
            HydrologyGrid grid,
            byte[] directions,
            bool[] oceanMask,
            LakeField lakeField,
            List<RiverReach> rawReaches,
            int[] downstreamReach,
            int[] remap,
            int[] downstreamLake,
            IReadOnlyDictionary<int, int> reachIndexByStart)
        {
            var lakes = new List<LakeBasin>(lakeField.Lakes.Count);
            for (var l = 0; l < lakeField.Lakes.Count; l++)
            {
                var candidate = lakeField.Lakes[l];

                // The outlet is whatever the spill cell actually drains into — read from the flow
                // field, not guessed from adjacency, so an inlet arriving beside the spill cannot be
                // mistaken for the outflow.
                var outlet = ReachId.None;
                var outletCell = FlowRouting.Downstream(
                    grid, candidate.SpillCell, directions[candidate.SpillCell]);
                if (outletCell >= 0 && reachIndexByStart.TryGetValue(outletCell, out var rawOutlet))
                {
                    // Through any dropped stub to the first surviving reach, so a lake whose
                    // immediate outflow was too short to draw still names the river it feeds.
                    var resolved = ResolveDownstream(rawReaches, downstreamReach, remap, rawOutlet);
                    if (resolved >= 0)
                    {
                        outlet = new ReachId(resolved);
                    }
                }

                // Nothing left to name downstream means the basin overflows onto the shore itself:
                // the channel mask forces a lake's outflow to be a channel all the way to the sea,
                // so the only way to run out of reaches is to already be there.
                var spillsToOcean = !outlet.IsValid
                    && (outletCell < 0 || oceanMask[outletCell]);

                var inlets = new List<ReachId>();
                for (var r = 0; r < downstreamLake.Length; r++)
                {
                    if (downstreamLake[r] == l && remap[r] >= 0)
                    {
                        inlets.Add(new ReachId(remap[r]));
                    }
                }

                inlets.Sort();

                lakes.Add(new LakeBasin(
                    new LakeId(lakes.Count),
                    candidate.Boundary,
                    candidate.SurfaceElevation,
                    candidate.MaxDepth,
                    candidate.AreaSqm,
                    outlet,
                    inlets,
                    candidate.SpillPointXZ,
                    spillsToOcean));
            }

            return lakes;
        }
    }
}

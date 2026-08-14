using System.Collections.Generic;
using GameDev.World.Terrain;
using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>Tunable knobs for <see cref="RiverTracer.Trace"/>. <see cref="Default"/> is sized
    /// for <see cref="FlowNetwork"/> grids sampled at the same 3m cell size
    /// <c>WaterZoneMenu</c> already uses for lakes.</summary>
    public readonly struct RiverTracerSettings
    {
        /// <summary>A cell becomes part of a channel once at least this many cells drain through
        /// it - the flow-accumulation-threshold stream extraction technique (standard, deterministic,
        /// no simulation required).</summary>
        public readonly int ChannelAccumulationThreshold;

        /// <summary>Accumulation at which width/depth reach their max - a river keeps widening as
        /// more of the catchment feeds it, up to this point.</summary>
        public readonly int ReferenceAccumulation;

        public readonly float MinWidthMeters;
        public readonly float MaxWidthMeters;
        public readonly float MinCarveDepthMeters;
        public readonly float MaxCarveDepthMeters;

        /// <summary>Fraction of the carved channel's depth the water surface fills, measured up
        /// from the bed - less than 1 so the carved banks stand above the waterline.</summary>
        public readonly float WaterFillFraction;

        public readonly int MaxRivers;

        /// <summary>Discard a traced run shorter than this many cells - a channel cell that
        /// immediately empties into a lake isn't a river, it's noise at the shoreline.</summary>
        public readonly int MinPathCells;

        /// <summary>A head only counts as a real river SOURCE if the ground there sits at or above
        /// this real-world height - "the river starts from a mountain", not from an arbitrary point
        /// in the middle of a field where flow happened to converge. See the incident note on
        /// <see cref="Trace"/> for why this exists.</summary>
        public readonly float MinSourceElevationY;

        public RiverTracerSettings(
            int channelAccumulationThreshold, int referenceAccumulation,
            float minWidthMeters, float maxWidthMeters,
            float minCarveDepthMeters, float maxCarveDepthMeters,
            float waterFillFraction, int maxRivers, int minPathCells,
            float minSourceElevationY)
        {
            ChannelAccumulationThreshold = channelAccumulationThreshold;
            ReferenceAccumulation = referenceAccumulation;
            MinWidthMeters = minWidthMeters;
            MaxWidthMeters = maxWidthMeters;
            MinCarveDepthMeters = minCarveDepthMeters;
            MaxCarveDepthMeters = maxCarveDepthMeters;
            WaterFillFraction = waterFillFraction;
            MaxRivers = maxRivers;
            MinPathCells = minPathCells;
            MinSourceElevationY = minSourceElevationY;
        }

        // A ~1360x1360m sampled tile at 6m cells is roughly 227x227 = ~51500 cells total. The first
        // pass at this (threshold 220, ~0.4% of the grid) was far too low - any modest, ordinary
        // convergence of flow on gently sloped ground crossed it, so huge swaths of ordinary terrain
        // read as "channel", carving absurdly wide river-shaped lakes instead of actual rivers.
        // Raised by roughly an order of magnitude so a cell needs a real, substantial catchment
        // (~5% of the sampled grid) before it counts as a stream - worth re-tuning once seen: lower
        // it if rivers feel too sparse/short, raise it further if wide channels still show up on
        // gentle ground.
        // <para>
        // MinSourceElevationY must be a <b>world-space</b> Y (terrain pivot + GreenMaxHeight * height
        // scale). Passing bare globals.height broke mountain sourcing once HeightSampler switched to
        // Terrain.SampleHeight + pivot (plains and peaks both sat ~1000m above the old 0..250 gate).
        // </para>
        public static RiverTracerSettings Default(float terrainBaseY, float heightScaleMeters) => new RiverTracerSettings(
            channelAccumulationThreshold: 60,
            referenceAccumulation: 60 * 10,
            minWidthMeters: WorldScale.RiverSourceWidthMeters,
            maxWidthMeters: WorldScale.RiverMouthWidthMeters,
            minCarveDepthMeters: 4f,
            maxCarveDepthMeters: 9f,
            // Low fill → ribbon hugs the carved bed (high values floated metres above the trench).
            waterFillFraction: 0.35f,
            maxRivers: 12,
            minPathCells: 12,
            minSourceElevationY: terrainBaseY +
                Mathf.Lerp(
                    BiomeClassifier.GreenMaxHeight,
                    BiomeClassifier.HighlandMaxHeight,
                    0.2f) * Mathf.Max(heightScaleMeters, 1f));
    }

    /// <summary>
    /// Finds river sources - cells where flow accumulation first crosses a threshold ("enough of
    /// the mountain drains through here to be a stream") AND that sit at or above
    /// <see cref="RiverTracerSettings.MinSourceElevationY"/> ("actually on the mountain, not just
    /// somewhere flow happened to converge on flat ground") - and traces each one downhill along
    /// <see cref="FlowGrid"/>'s steepest-descent directions. D8 flow direction is strictly descending
    /// by construction (<see cref="FlowGrid"/> only ever points to a lower neighbour), so every trace
    /// is guaranteed to terminate - no cycle is possible - but a trace can still end in a
    /// <see cref="WaterZone"/> (a lake it feeds), another already-traced river's territory (a
    /// confluence), or a dead end (off the sampled tile, or an interior dip too small to be a lake -
    /// see <see cref="RiverTermination"/>). Only traces that reach a lake are kept: "always mountain
    /// to nearest lake, or it doesn't exist", not a fragment that stops wherever channel-strength
    /// flow happened to run out.
    /// </summary>
    public static class RiverTracer
    {
        private static readonly (int Dx, int Dz)[] Neighbors8 =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1),
            (1, 1), (1, -1), (-1, 1), (-1, -1),
        };

        public static RiverPath[] Trace(FlowGrid grid, IReadOnlyList<WaterZone> lakes, RiverTracerSettings settings)
        {
            bool IsChannel(int x, int z) => grid.AccumulationAt(x, z) >= settings.ChannelAccumulationThreshold;
            bool IsValidSource(int x, int z) => grid.HeightAt(x, z) >= settings.MinSourceElevationY;

            var heads = FindHeads(grid, IsChannel, IsValidSource);

            // Highest source first (mountain peaks before foothills), then strongest catchment.
            heads.Sort((a, b) =>
            {
                var ha = grid.HeightAt(a.X, a.Z);
                var hb = grid.HeightAt(b.X, b.Z);
                var byHeight = hb.CompareTo(ha);
                if (byHeight != 0)
                {
                    return byHeight;
                }

                var byAccum = b.Accum.CompareTo(a.Accum);
                if (byAccum != 0)
                {
                    return byAccum;
                }

                var byX = a.X.CompareTo(b.X);
                return byX != 0 ? byX : a.Z.CompareTo(b.Z);
            });

            var claimed = new bool[grid.Width, grid.Depth];
            var rivers = new List<RiverPath>();
            var acceptedStarts = new List<Vector2>();
            var maxSteps = grid.Width * grid.Depth + 1;
            // Parallel twin drains off the same cliff rim — keep one per catchment neighbourhood.
            var minStartSeparation = Mathf.Max(grid.CellSize * 28f, 80f);
            var minStartSeparationSq = minStartSeparation * minStartSeparation;

            foreach (var head in heads)
            {
                if (rivers.Count >= settings.MaxRivers)
                {
                    break;
                }

                if (claimed[head.X, head.Z])
                {
                    continue;
                }

                var headWorld = grid.CellToWorld(head.X, head.Z);
                var tooClose = false;
                for (var s = 0; s < acceptedStarts.Count; s++)
                {
                    var dx = headWorld.x - acceptedStarts[s].x;
                    var dz = headWorld.y - acceptedStarts[s].y;
                    if (dx * dx + dz * dz < minStartSeparationSq)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                {
                    continue;
                }

                var path = TraceOne(grid, lakes, claimed, head.X, head.Z, maxSteps, settings, rivers.Count);

                if (path != null && path.Termination == RiverTermination.Lake)
                {
                    rivers.Add(path);
                    acceptedStarts.Add(new Vector2(headWorld.x, headWorld.y));
                    ClaimPathCorridor(claimed, path, grid, radiusCells: 3);
                }
            }

            EnsureLakeFeeders(grid, lakes, rivers, claimed, acceptedStarts, settings, maxSteps);
            EnsureSpineRiver(grid, lakes, rivers, claimed, acceptedStarts, settings, maxSteps);
            return rivers.ToArray();
        }

        /// <summary>
        /// One long mountain-origin river that spans a large fraction of the tile (lake mouth or
        /// OffTile border exit). Reserved outside the normal <see cref="RiverTracerSettings.MaxRivers"/>
        /// cap so lake feeders are not displaced.
        /// </summary>
        private static void EnsureSpineRiver(
            FlowGrid grid,
            IReadOnlyList<WaterZone> lakes,
            List<RiverPath> rivers,
            bool[,] claimed,
            List<Vector2> acceptedStarts,
            RiverTracerSettings settings,
            int maxSteps)
        {
            var tileSpan = Mathf.Max(grid.Width, grid.Depth) * grid.CellSize;
            var minLength = tileSpan * 0.45f;

            for (var i = 0; i < rivers.Count; i++)
            {
                if (IsLongSpineCandidate(rivers[i], minLength))
                {
                    return;
                }
            }

            if (!TryFindHighestChannelHead(grid, settings, out var hx, out var hz))
            {
                return;
            }

            // Scratch claim so a failed spine attempt does not pollute the feeder corridors.
            var scratchClaimed = (bool[,])claimed.Clone();
            scratchClaimed[hx, hz] = false;

            var path = TraceOne(grid, lakes, scratchClaimed, hx, hz, maxSteps, settings, rivers.Count);
            if (path == null || !IsLongSpineCandidate(path, minLength))
            {
                path = BuildDescendingPathTowardFarBorder(grid, hx, hz, settings, rivers.Count);
            }

            if (path == null || PathLengthXZ(path) < minLength * 0.85f)
            {
                return;
            }

            // Valid terminals only: Lake or map edge. Never keep a Sink mid-terrain.
            if (path.Termination != RiverTermination.Lake &&
                path.Termination != RiverTermination.OffTile)
            {
                return;
            }

            rivers.Add(path);
            var start = grid.CellToWorld(hx, hz);
            acceptedStarts.Add(new Vector2(start.x, start.y));
            ClaimPathCorridor(claimed, path, grid, radiusCells: 3);
        }

        private static bool IsLongSpineCandidate(RiverPath path, float minLength)
        {
            if (path == null || path.Centerline.Count < 2)
            {
                return false;
            }

            if (PathLengthXZ(path) < minLength)
            {
                return false;
            }

            return path.Termination == RiverTermination.Lake ||
                   path.Termination == RiverTermination.OffTile;
        }

        private static float PathLengthXZ(RiverPath path)
        {
            var len = 0f;
            for (var i = 1; i < path.Centerline.Count; i++)
            {
                var a = path.Centerline[i - 1];
                var b = path.Centerline[i];
                var dx = b.x - a.x;
                var dz = b.z - a.z;
                len += Mathf.Sqrt(dx * dx + dz * dz);
            }

            return len;
        }

        private static bool TryFindHighestChannelHead(
            FlowGrid grid, RiverTracerSettings settings, out int bestX, out int bestZ)
        {
            bestX = 0;
            bestZ = 0;
            bool IsChannel(int x, int z) => grid.AccumulationAt(x, z) >= settings.ChannelAccumulationThreshold;
            bool IsValidSource(int x, int z) => grid.HeightAt(x, z) >= settings.MinSourceElevationY;
            var heads = FindHeads(grid, IsChannel, IsValidSource);
            if (heads.Count == 0)
            {
                return false;
            }

            var bestY = float.NegativeInfinity;
            var bestAccum = -1;
            var found = false;
            for (var i = 0; i < heads.Count; i++)
            {
                var h = heads[i];
                var y = grid.HeightAt(h.X, h.Z);
                if (y < bestY || (Mathf.Approximately(y, bestY) && h.Accum <= bestAccum))
                {
                    continue;
                }

                bestY = y;
                bestAccum = h.Accum;
                bestX = h.X;
                bestZ = h.Z;
                found = true;
            }

            return found;
        }

        private static RiverPath BuildDescendingPathTowardFarBorder(
            FlowGrid grid,
            int startX,
            int startZ,
            RiverTracerSettings settings,
            int riverIndex)
        {
            var startWorld = grid.CellToWorld(startX, startZ);
            var midX = grid.OriginX + grid.Width * grid.CellSize * 0.5f;
            var midZ = grid.OriginZ + grid.Depth * grid.CellSize * 0.5f;

            // Push toward the opposite tile border from the source.
            float targetX;
            float targetZ;
            if (Mathf.Abs(startWorld.x - midX) >= Mathf.Abs(startWorld.y - midZ))
            {
                targetX = startWorld.x < midX
                    ? grid.OriginX + (grid.Width - 1) * grid.CellSize
                    : grid.OriginX;
                targetZ = startWorld.y;
            }
            else
            {
                targetX = startWorld.x;
                targetZ = startWorld.y < midZ
                    ? grid.OriginZ + (grid.Depth - 1) * grid.CellSize
                    : grid.OriginZ;
            }

            var cellsX = new List<int>();
            var cellsZ = new List<int>();
            var cx = startX;
            var cz = startZ;
            var maxSteps = grid.Width + grid.Depth;

            for (var step = 0; step < maxSteps; step++)
            {
                cellsX.Add(cx);
                cellsZ.Add(cz);

                if (grid.IsBorderCell(cx, cz) && step > 4)
                {
                    break;
                }

                var bestX = cx;
                var bestZ = cz;
                var bestScore = float.PositiveInfinity;
                var curY = grid.HeightAt(cx, cz);
                foreach (var (dx, dz) in Neighbors8)
                {
                    var nx = cx + dx;
                    var nz = cz + dz;
                    if (!grid.InBounds(nx, nz))
                    {
                        continue;
                    }

                    var ny = grid.HeightAt(nx, nz);
                    if (ny > curY + 0.08f)
                    {
                        continue;
                    }

                    var nw = grid.CellToWorld(nx, nz);
                    var ddx = nw.x - targetX;
                    var ddz = nw.y - targetZ;
                    var distSq = ddx * ddx + ddz * ddz;
                    var score = distSq + (ny - curY) * 25f;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestX = nx;
                        bestZ = nz;
                    }
                }

                if (bestX == cx && bestZ == cz)
                {
                    break;
                }

                cx = bestX;
                cz = bestZ;
            }

            if (cellsX.Count < settings.MinPathCells)
            {
                return null;
            }

            var termination = grid.IsBorderCell(cellsX[cellsX.Count - 1], cellsZ[cellsZ.Count - 1])
                ? RiverTermination.OffTile
                : RiverTermination.Sink;
            return BuildPath(grid, cellsX, cellsZ, termination, settings, riverIndex);
        }

        /// <summary>
        /// Every large lake gets at least one river from elevated ground. Natural flow heads are
        /// preferred; if none reach the lake, a descending walk from the nearest highland peak is
        /// forced so "no rivers" seeds cannot ship.
        /// </summary>
        private static void EnsureLakeFeeders(
            FlowGrid grid,
            IReadOnlyList<WaterZone> lakes,
            List<RiverPath> rivers,
            bool[,] claimed,
            List<Vector2> acceptedStarts,
            RiverTracerSettings settings,
            int maxSteps)
        {
            if (lakes == null || lakes.Count == 0)
            {
                return;
            }

            for (var li = 0; li < lakes.Count; li++)
            {
                if (rivers.Count >= settings.MaxRivers)
                {
                    break;
                }

                var lake = lakes[li];
                if (lake.EquivalentRadiusMeters < 30f)
                {
                    continue;
                }

                if (RiverFeedsLake(rivers, lake))
                {
                    continue;
                }

                if (!TryFindFeederHead(grid, lake, settings.MinSourceElevationY, out var hx, out var hz))
                {
                    if (!TryFindNearestHighlandTowardLake(grid, lake, settings.MinSourceElevationY, out hx, out hz))
                    {
                        continue;
                    }
                }

                // Allow ensure-pass to reuse space near the lake — clear a small start cell only.
                claimed[hx, hz] = false;
                var ensureSettings = new RiverTracerSettings(
                    settings.ChannelAccumulationThreshold,
                    settings.ReferenceAccumulation,
                    settings.MinWidthMeters,
                    settings.MaxWidthMeters,
                    settings.MinCarveDepthMeters,
                    settings.MaxCarveDepthMeters,
                    settings.WaterFillFraction,
                    settings.MaxRivers,
                    minPathCells: Mathf.Max(8, settings.MinPathCells / 2),
                    settings.MinSourceElevationY);

                var path = TraceOne(grid, lakes, claimed, hx, hz, maxSteps, ensureSettings, rivers.Count);
                if (path == null || path.Termination != RiverTermination.Lake)
                {
                    path = BuildDescendingPathToLake(grid, lake, hx, hz, ensureSettings, rivers.Count);
                }

                if (path == null)
                {
                    continue;
                }

                // Only keep if this path actually reaches *this* lake (or any lake — still better
                // than zero rivers). Prefer this lake when possible.
                rivers.Add(path);
                var start = grid.CellToWorld(hx, hz);
                acceptedStarts.Add(new Vector2(start.x, start.y));
                ClaimPathCorridor(claimed, path, grid, radiusCells: 2);
            }
        }

        private static bool RiverFeedsLake(IReadOnlyList<RiverPath> rivers, WaterZone lake)
        {
            for (var i = 0; i < rivers.Count; i++)
            {
                var path = rivers[i];
                if (path.Termination != RiverTermination.Lake || path.Centerline.Count == 0)
                {
                    continue;
                }

                var end = path.Centerline[path.Centerline.Count - 1];
                if (lake.Contains(end.x, end.z))
                {
                    return true;
                }

                var c = lake.GetFloodCentroidXZ();
                var dx = end.x - c.x;
                var dz = end.z - c.z;
                if (dx * dx + dz * dz <= lake.EquivalentRadiusMeters * lake.EquivalentRadiusMeters * 1.4f)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindFeederHead(
            FlowGrid grid,
            WaterZone lake,
            float minSourceY,
            out int bestX,
            out int bestZ)
        {
            bestX = 0;
            bestZ = 0;
            var bestY = float.NegativeInfinity;
            var maxSteps = grid.Width * grid.Depth + 1;
            // Stride keeps editor cost sane on ~450² grids while still finding highland heads.
            const int stride = 3;

            for (var x = 0; x < grid.Width; x += stride)
            {
                for (var z = 0; z < grid.Depth; z += stride)
                {
                    var y = grid.HeightAt(x, z);
                    if (y < minSourceY || y <= bestY)
                    {
                        continue;
                    }

                    if (FlowReachesLake(grid, lake, x, z, maxSteps))
                    {
                        bestY = y;
                        bestX = x;
                        bestZ = z;
                    }
                }
            }

            return !float.IsNegativeInfinity(bestY);
        }

        private static bool TryFindNearestHighlandTowardLake(
            FlowGrid grid,
            WaterZone lake,
            float minSourceY,
            out int bestX,
            out int bestZ)
        {
            bestX = 0;
            bestZ = 0;
            var centroid = lake.GetFloodCentroidXZ();
            var bestScore = float.NegativeInfinity;
            const int stride = 3;

            for (var x = 0; x < grid.Width; x += stride)
            {
                for (var z = 0; z < grid.Depth; z += stride)
                {
                    var y = grid.HeightAt(x, z);
                    if (y < minSourceY)
                    {
                        continue;
                    }

                    var w = grid.CellToWorld(x, z);
                    var dx = w.x - centroid.x;
                    var dz = w.y - centroid.z;
                    var dist = Mathf.Sqrt(dx * dx + dz * dz);
                    if (dist < lake.EquivalentRadiusMeters + 40f)
                    {
                        continue;
                    }

                    var score = y - dist * 0.15f;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestX = x;
                        bestZ = z;
                    }
                }
            }

            return !float.IsNegativeInfinity(bestScore);
        }

        private static bool FlowReachesLake(FlowGrid grid, WaterZone lake, int startX, int startZ, int maxSteps)
        {
            var cx = startX;
            var cz = startZ;
            for (var step = 0; step < maxSteps; step++)
            {
                var world = grid.CellToWorld(cx, cz);
                if (lake.Contains(world.x, world.y))
                {
                    return true;
                }

                if (!grid.TryGetDownstream(cx, cz, out var nx, out var nz))
                {
                    return false;
                }

                cx = nx;
                cz = nz;
            }

            return false;
        }

        private static RiverPath BuildDescendingPathToLake(
            FlowGrid grid,
            WaterZone lake,
            int startX,
            int startZ,
            RiverTracerSettings settings,
            int riverIndex)
        {
            var cellsX = new List<int>();
            var cellsZ = new List<int>();
            var cx = startX;
            var cz = startZ;
            var target = lake.GetFloodCentroidXZ();
            var maxSteps = grid.Width + grid.Depth;

            for (var step = 0; step < maxSteps; step++)
            {
                cellsX.Add(cx);
                cellsZ.Add(cz);
                var world = grid.CellToWorld(cx, cz);
                if (lake.Contains(world.x, world.y))
                {
                    break;
                }

                var bestX = cx;
                var bestZ = cz;
                var bestScore = float.PositiveInfinity;
                var curY = grid.HeightAt(cx, cz);
                foreach (var (dx, dz) in Neighbors8)
                {
                    var nx = cx + dx;
                    var nz = cz + dz;
                    if (!grid.InBounds(nx, nz))
                    {
                        continue;
                    }

                    var ny = grid.HeightAt(nx, nz);
                    if (ny > curY + 0.05f)
                    {
                        continue;
                    }

                    var nw = grid.CellToWorld(nx, nz);
                    var ddx = nw.x - target.x;
                    var ddz = nw.y - target.z;
                    var distSq = ddx * ddx + ddz * ddz;
                    var score = distSq + (ny - curY) * 40f;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestX = nx;
                        bestZ = nz;
                    }
                }

                if (bestX == cx && bestZ == cz)
                {
                    break;
                }

                cx = bestX;
                cz = bestZ;
            }

            if (cellsX.Count < settings.MinPathCells)
            {
                return null;
            }

            var end = grid.CellToWorld(cellsX[cellsX.Count - 1], cellsZ[cellsZ.Count - 1]);
            var termination = lake.Contains(end.x, end.y)
                ? RiverTermination.Lake
                : RiverTermination.Sink;
            if (termination != RiverTermination.Lake)
            {
                return null;
            }

            var claimed = new bool[grid.Width, grid.Depth];
            AppendMouthOverlap(grid, lake, cellsX, cellsZ, claimed, overlapCells: 20);

            return BuildPath(grid, cellsX, cellsZ, termination, settings, riverIndex);
        }

        private static void ClaimPathCorridor(bool[,] claimed, RiverPath path, FlowGrid grid, int radiusCells)
        {
            for (var i = 0; i < path.Centerline.Count; i++)
            {
                var p = path.Centerline[i];
                var cx = Mathf.FloorToInt((p.x - grid.OriginX) / grid.CellSize);
                var cz = Mathf.FloorToInt((p.z - grid.OriginZ) / grid.CellSize);
                for (var dz = -radiusCells; dz <= radiusCells; dz++)
                {
                    for (var dx = -radiusCells; dx <= radiusCells; dx++)
                    {
                        var x = cx + dx;
                        var z = cz + dz;
                        if (grid.InBounds(x, z))
                        {
                            claimed[x, z] = true;
                        }
                    }
                }
            }
        }

        private static List<(int Accum, int X, int Z)> FindHeads(
            FlowGrid grid, System.Func<int, int, bool> isChannel, System.Func<int, int, bool> isValidSource)
        {
            var heads = new List<(int Accum, int X, int Z)>();

            for (var x = 0; x < grid.Width; x++)
            {
                for (var z = 0; z < grid.Depth; z++)
                {
                    if (!isChannel(x, z) || !isValidSource(x, z))
                    {
                        continue;
                    }

                    var hasUpstreamChannelNeighbor = false;
                    foreach (var (dx, dz) in Neighbors8)
                    {
                        var nx = x + dx;
                        var nz = z + dz;
                        if (!grid.InBounds(nx, nz) || !isChannel(nx, nz))
                        {
                            continue;
                        }

                        if (grid.TryGetDownstream(nx, nz, out var tx, out var tz) && tx == x && tz == z)
                        {
                            hasUpstreamChannelNeighbor = true;
                            break;
                        }
                    }

                    if (!hasUpstreamChannelNeighbor)
                    {
                        heads.Add((grid.AccumulationAt(x, z), x, z));
                    }
                }
            }

            return heads;
        }

        private static RiverPath TraceOne(
            FlowGrid grid, IReadOnlyList<WaterZone> lakes, bool[,] claimed,
            int startX, int startZ, int maxSteps, RiverTracerSettings settings, int riverIndex)
        {
            var cellsX = new List<int>();
            var cellsZ = new List<int>();
            var termination = RiverTermination.Sink;

            var cx = startX;
            var cz = startZ;
            var first = true;
            var steps = 0;

            while (true)
            {
                if (!first && claimed[cx, cz])
                {
                    cellsX.Add(cx);
                    cellsZ.Add(cz);
                    termination = RiverTermination.Confluence;
                    break;
                }

                var world = grid.CellToWorld(cx, cz);
                WaterZone hitLake = null;
                for (var i = 0; i < lakes.Count; i++)
                {
                    if (lakes[i].Contains(world.x, world.y))
                    {
                        hitLake = lakes[i];
                        break;
                    }
                }

                cellsX.Add(cx);
                cellsZ.Add(cz);
                claimed[cx, cz] = true;

                if (hitLake != null)
                {
                    termination = RiverTermination.Lake;
                    // Keep walking into the lake so the ribbon overlaps the water plane (stopping on
                    // the first flood cell leaves a dry gap after soft bank carve / shore pad).
                    AppendMouthOverlap(grid, hitLake, cellsX, cellsZ, claimed, overlapCells: 20);
                    break;
                }

                first = false;

                if (!grid.TryGetDownstream(cx, cz, out var nx, out var nz))
                {
                    termination = grid.IsBorderCell(cx, cz) ? RiverTermination.OffTile : RiverTermination.Sink;
                    break;
                }

                cx = nx;
                cz = nz;
                steps++;

                if (steps > maxSteps)
                {
                    // Defensive only - D8 flow strictly decreases in height every step, so a cycle
                    // is impossible; this just bounds worst case if that invariant is ever broken.
                    termination = RiverTermination.Sink;
                    break;
                }
            }

            if (cellsX.Count < settings.MinPathCells)
            {
                return null;
            }

            return BuildPath(grid, cellsX, cellsZ, termination, settings, riverIndex);
        }

        /// <summary>
        /// Extra cells past the shoreline, prefer downstream flow while still in the lake, else
        /// step toward the lake centroid.
        /// </summary>
        private static void AppendMouthOverlap(
            FlowGrid grid,
            WaterZone lake,
            List<int> cellsX,
            List<int> cellsZ,
            bool[,] claimed,
            int overlapCells)
        {
            var cx = cellsX[cellsX.Count - 1];
            var cz = cellsZ[cellsZ.Count - 1];
            var target = lake.GetFloodCentroidXZ();

            for (var k = 0; k < overlapCells; k++)
            {
                var nextX = cx;
                var nextZ = cz;
                if (grid.TryGetDownstream(cx, cz, out var fx, out var fz))
                {
                    var fw = grid.CellToWorld(fx, fz);
                    if (lake.Contains(fw.x, fw.y))
                    {
                        nextX = fx;
                        nextZ = fz;
                    }
                }

                if (nextX == cx && nextZ == cz)
                {
                    var bestScore = float.PositiveInfinity;
                    foreach (var (dx, dz) in Neighbors8)
                    {
                        var nx = cx + dx;
                        var nz = cz + dz;
                        if (!grid.InBounds(nx, nz))
                        {
                            continue;
                        }

                        var nw = grid.CellToWorld(nx, nz);
                        if (!lake.Contains(nw.x, nw.y))
                        {
                            continue;
                        }

                        var ddx = nw.x - target.x;
                        var ddz = nw.y - target.z;
                        var score = ddx * ddx + ddz * ddz;
                        if (score < bestScore)
                        {
                            bestScore = score;
                            nextX = nx;
                            nextZ = nz;
                        }
                    }
                }

                if (nextX == cx && nextZ == cz)
                {
                    break;
                }

                cx = nextX;
                cz = nextZ;
                cellsX.Add(cx);
                cellsZ.Add(cz);
                claimed[cx, cz] = true;
            }
        }

        private static RiverPath BuildPath(
            FlowGrid grid, List<int> cellsX, List<int> cellsZ, RiverTermination termination,
            RiverTracerSettings settings, int riverIndex)
        {
            var count = cellsX.Count;
            var centerline = new List<Vector3>(count);
            var widths = new List<float>(count);
            var carveDepths = new List<float>(count);
            var waterSurfaceY = new List<float>(count);

            var accumRange = Mathf.Max(1, settings.ReferenceAccumulation - settings.ChannelAccumulationThreshold);

            // Arc-length profile: width grows from source → mouth (t^growthExponent).
            var arc = new float[count];
            for (var i = 1; i < count; i++)
            {
                var wa = grid.CellToWorld(cellsX[i - 1], cellsZ[i - 1]);
                var wb = grid.CellToWorld(cellsX[i], cellsZ[i]);
                var dx = wb.x - wa.x;
                var dz = wb.y - wa.y;
                arc[i] = arc[i - 1] + Mathf.Sqrt(dx * dx + dz * dz);
            }

            var totalLen = Mathf.Max(arc[count - 1], 1e-3f);
            var growth = WorldScale.RiverWidthGrowthExponent;

            for (var i = 0; i < count; i++)
            {
                var x = cellsX[i];
                var z = cellsZ[i];
                var world = grid.CellToWorld(x, z);
                var groundY = grid.HeightAt(x, z);

                var accum = grid.AccumulationAt(x, z);
                var accumT = Mathf.Clamp01((accum - settings.ChannelAccumulationThreshold) / (float)accumRange);
                var pathT = Mathf.Pow(Mathf.Clamp01(arc[i] / totalLen), growth);
                // Downstream distance dominates; accumulation still widens confluence stretches.
                var t = Mathf.Max(pathT, accumT * 0.65f);

                var width = Mathf.Lerp(settings.MinWidthMeters, settings.MaxWidthMeters, t);
                var carveDepth = Mathf.Lerp(settings.MinCarveDepthMeters, settings.MaxCarveDepthMeters, t);
                var surfaceY = groundY - carveDepth * (1f - settings.WaterFillFraction);

                centerline.Add(new Vector3(world.x, groundY, world.y));
                widths.Add(width);
                carveDepths.Add(carveDepth);
                waterSurfaceY.Add(surfaceY);
            }

            return new RiverPath(
                $"river_{riverIndex}", termination,
                centerline, widths, carveDepths, waterSurfaceY,
                grid.OriginX, grid.OriginZ, grid.CellSize, grid.Width, grid.Depth);
        }
    }
}

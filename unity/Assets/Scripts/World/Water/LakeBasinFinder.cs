using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Derives lake basins from already-generated terrain height instead of assuming a global
    /// water plane (root cause of the old dry-crater / floating-plane bugs: a single hardcoded Y
    /// compared against noise-carved dips that may or may not actually reach it). Seeds are local
    /// minima found directly in the sampled heightmap - a topological test, not an elevation band -
    /// so a depression works as a lake seed whether it sits in a lowland or partway up a mountain.
    /// Each seed then grows via priority-flood (always into the lowest unflooded neighbour next,
    /// Barnes et al.-style watershed fill); <see cref="WaterZone.WaterLevelY"/> is the height of
    /// the last cell the flood accepted - the zone's own spill elevation, read from real terrain,
    /// never a caller-supplied constant. Cells are claimed once used, so a second, higher local
    /// minimum inside an already-flooded basin can't spawn a redundant second lake on top of the
    /// first.
    /// <para>
    /// The flood stops once the next candidate cell would sit more than <c>maxDepth</c> above the
    /// seed's own height (see <see cref="Flood"/>), not after a fixed cell count: a plain cell-count
    /// cap has no way to tell "reached the basin's rim" apart from "ran out of budget partway up
    /// the basin's actual slope", so a cap loose enough to avoid truncating a wide, shallow lake is
    /// also loose enough to swallow the flat plain around a small, deep one once the true rim cells
    /// are exhausted (there is nothing else lower left to stop the flood from continuing outward).
    /// A max-depth cap instead asks a physically meaningful question - "how deep can this lake
    /// plausibly be" - so it naturally follows an irregular basin's real footprint at any size
    /// while still refusing to keep climbing once the terrain around it is clearly high ground, not
    /// lakebed.
    /// </para>
    /// </summary>
    public static class LakeBasinFinder
    {
        private static readonly (int Dx, int Dz)[] Neighbors = { (1, 0), (-1, 0), (0, 1), (0, -1) };

        /// <param name="worldHeightSampler">World-space height in metres at (x, z).</param>
        /// <param name="maxDepth">
        /// Max plausible lake depth in metres above a basin's own lowest point - the flood stops
        /// growing once every remaining candidate cell exceeds this, rather than after a fixed
        /// cell count. This is what actually shapes each zone; see the type doc for why a cell
        /// count alone can't do this job.
        /// </param>
        /// <param name="cellCountSafetyCap">
        /// Defensive upper bound on cells per zone, only reachable if terrain has no enclosing rim
        /// within <paramref name="maxDepth"/> at all (e.g. a monotonic slope) - should be set far
        /// larger than any real basin so it never shapes a normal lake.
        /// </param>
        public static WaterZone[] FindZones(
            Func<float, float, float> worldHeightSampler,
            float minX, float minZ, float sizeX, float sizeZ,
            float cellSize,
            float maxDepth,
            int cellCountSafetyCap,
            int minCellsPerZone,
            int maxZones)
        {
            var width = Mathf.Max(2, Mathf.RoundToInt(sizeX / cellSize));
            var depth = Mathf.Max(2, Mathf.RoundToInt(sizeZ / cellSize));
            var heights = new float[width, depth];

            for (var x = 0; x < width; x++)
            {
                for (var z = 0; z < depth; z++)
                {
                    heights[x, z] = worldHeightSampler(minX + (x + 0.5f) * cellSize, minZ + (z + 0.5f) * cellSize);
                }
            }

            var candidates = new List<(float H, int X, int Z)>();
            for (var x = 0; x < width; x++)
            {
                for (var z = 0; z < depth; z++)
                {
                    if (IsLocalMinimum(heights, x, z, width, depth))
                    {
                        candidates.Add((heights[x, z], x, z));
                    }
                }
            }

            candidates.Sort((a, b) => a.H.CompareTo(b.H));

            var claimed = new bool[width, depth];
            var zones = new List<WaterZone>();

            foreach (var candidate in candidates)
            {
                if (zones.Count >= maxZones)
                {
                    break;
                }

                if (claimed[candidate.X, candidate.Z])
                {
                    continue;
                }

                var (flooded, level, count) = Flood(
                    heights, claimed, candidate.X, candidate.Z, width, depth, maxDepth, cellCountSafetyCap);
                if (count < minCellsPerZone)
                {
                    continue;
                }

                zones.Add(new WaterZone($"lake_{zones.Count}", level, minX, minZ, cellSize, flooded, heights));
            }

            return zones.ToArray();
        }

        private static bool IsLocalMinimum(float[,] heights, int x, int z, int width, int depth)
        {
            var h = heights[x, z];
            var hasHigherNeighbor = false;

            foreach (var (dx, dz) in Neighbors)
            {
                var nx = x + dx;
                var nz = z + dz;
                if (nx < 0 || nz < 0 || nx >= width || nz >= depth)
                {
                    continue;
                }

                var nh = heights[nx, nz];
                if (nh < h)
                {
                    return false;
                }

                if (nh > h)
                {
                    hasHigherNeighbor = true;
                }
            }

            // Requiring a strictly higher neighbour excludes perfectly flat ground, which has
            // nowhere lower to be a "minimum" relative to.
            return hasHigherNeighbor;
        }

        private static (bool[,] Flooded, float Level, int Count) Flood(
            float[,] heights, bool[,] claimed, int seedX, int seedZ, int width, int depth,
            float maxDepth, int cellCountSafetyCap)
        {
            var flooded = new bool[width, depth];
            var queued = new bool[width, depth];
            var frontier = new SortedSet<(float H, int X, int Z)>();

            void TryEnqueue(int x, int z)
            {
                if (x < 0 || z < 0 || x >= width || z >= depth || queued[x, z] || claimed[x, z])
                {
                    return;
                }

                queued[x, z] = true;
                frontier.Add((heights[x, z], x, z));
            }

            TryEnqueue(seedX, seedZ);
            var seedHeight = heights[seedX, seedZ];
            var level = seedHeight;
            var count = 0;

            while (frontier.Count > 0 && count < cellCountSafetyCap)
            {
                var cell = frontier.Min;

                // Cells come off in ascending height order, so the moment one sits too high above
                // the seed to still be part of the same lake, every remaining candidate does too -
                // this is the basin's rim, not just a budget running out.
                if (cell.H - seedHeight > maxDepth)
                {
                    break;
                }

                frontier.Remove(cell);

                flooded[cell.X, cell.Z] = true;
                claimed[cell.X, cell.Z] = true;
                if (cell.H > level)
                {
                    level = cell.H;
                }

                count++;

                foreach (var (dx, dz) in Neighbors)
                {
                    TryEnqueue(cell.X + dx, cell.Z + dz);
                }
            }

            return (flooded, level, count);
        }
    }
}

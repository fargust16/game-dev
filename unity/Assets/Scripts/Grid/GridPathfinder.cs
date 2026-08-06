using System.Collections.Generic;
using UnityEngine;

namespace GameDev.Grid
{
    /// <summary>
    /// Grid A* pathfinding for click-to-move. Design: 10.1 Grid World.
    /// </summary>
    public static class GridPathfinder
    {
        private static readonly GridCoord[] Ortho =
        {
            new GridCoord(1, 0), new GridCoord(-1, 0), new GridCoord(0, 1), new GridCoord(0, -1)
        };

        private static readonly GridCoord[] Diagonal =
        {
            new GridCoord(1, 1), new GridCoord(1, -1), new GridCoord(-1, 1), new GridCoord(-1, -1)
        };

        public static bool TryFindPath(GridMap grid, GridCoord start, GridCoord goal, List<GridCoord> pathOut)
        {
            pathOut.Clear();
            if (grid == null || !grid.HasCells)
            {
                return false;
            }

            if (!grid.IsWalkable(start))
            {
                return false;
            }

            if (!grid.IsWalkable(goal))
            {
                if (!TryFindNearestWalkable(grid, goal, 3, out goal))
                {
                    return false;
                }
            }

            if (start.Equals(goal))
            {
                pathOut.Add(start);
                return true;
            }

            var width = grid.Width;
            var height = grid.Height;
            var len = width * height;
            var cameFrom = new int[len];
            var gScore = new float[len];
            var closed = new bool[len];
            for (var i = 0; i < len; i++)
            {
                cameFrom[i] = -1;
                gScore[i] = float.PositiveInfinity;
            }

            var open = new List<int>(64);
            var startIndex = Index(start, width);
            gScore[startIndex] = 0f;
            open.Add(startIndex);

            var goalIndex = Index(goal, width);
            var found = false;

            while (open.Count > 0)
            {
                var bestOpen = 0;
                var bestF = float.PositiveInfinity;
                for (var i = 0; i < open.Count; i++)
                {
                    var idx = open[i];
                    var c = FromIndex(idx, width);
                    var f = gScore[idx] + Heuristic(c, goal, grid.DiagonalCostsFifteen);
                    if (f < bestF)
                    {
                        bestF = f;
                        bestOpen = i;
                    }
                }

                var currentIndex = open[bestOpen];
                open.RemoveAt(bestOpen);

                if (currentIndex == goalIndex)
                {
                    found = true;
                    break;
                }

                if (closed[currentIndex])
                {
                    continue;
                }

                closed[currentIndex] = true;
                var current = FromIndex(currentIndex, width);

                Expand(grid, current, currentIndex, Ortho, 1f, open, gScore, cameFrom, closed, width);

                if (grid.AllowDiagonalMove)
                {
                    var diagCost = grid.DiagonalCostsFifteen ? 1.5f : 1f;
                    ExpandDiagonal(grid, current, currentIndex, diagCost, open, gScore, cameFrom, closed, width);
                }
            }

            if (!found)
            {
                return false;
            }

            Reconstruct(cameFrom, goalIndex, width, pathOut);
            return pathOut.Count > 0;
        }

        private static void Expand(
            GridMap grid,
            GridCoord current,
            int currentIndex,
            GridCoord[] dirs,
            float stepCost,
            List<int> open,
            float[] gScore,
            int[] cameFrom,
            bool[] closed,
            int width)
        {
            foreach (var dir in dirs)
            {
                var next = new GridCoord(current.x + dir.x, current.y + dir.y);
                if (!grid.IsWalkable(next))
                {
                    continue;
                }

                var nextIndex = Index(next, width);
                if (closed[nextIndex])
                {
                    continue;
                }

                var tentative = gScore[currentIndex] + stepCost + TraversalPenalty(grid, next);
                if (tentative >= gScore[nextIndex])
                {
                    continue;
                }

                cameFrom[nextIndex] = currentIndex;
                gScore[nextIndex] = tentative;
                if (!open.Contains(nextIndex))
                {
                    open.Add(nextIndex);
                }
            }
        }

        private static void ExpandDiagonal(
            GridMap grid,
            GridCoord current,
            int currentIndex,
            float stepCost,
            List<int> open,
            float[] gScore,
            int[] cameFrom,
            bool[] closed,
            int width)
        {
            foreach (var dir in Diagonal)
            {
                var next = new GridCoord(current.x + dir.x, current.y + dir.y);
                if (!grid.IsWalkable(next))
                {
                    continue;
                }

                // No corner cutting through walls.
                if (!grid.IsWalkable(new GridCoord(current.x + dir.x, current.y)) ||
                    !grid.IsWalkable(new GridCoord(current.x, current.y + dir.y)))
                {
                    continue;
                }

                var nextIndex = Index(next, width);
                if (closed[nextIndex])
                {
                    continue;
                }

                var tentative = gScore[currentIndex] + stepCost + TraversalPenalty(grid, next);
                if (tentative >= gScore[nextIndex])
                {
                    continue;
                }

                cameFrom[nextIndex] = currentIndex;
                gScore[nextIndex] = tentative;
                if (!open.Contains(nextIndex))
                {
                    open.Add(nextIndex);
                }
            }
        }

        private static float TraversalPenalty(GridMap grid, GridCoord coord)
        {
            if (!grid.TryGetCell(coord, out var cell))
            {
                return 0f;
            }

            // Prefer ordinary streets; env traversal is allowed but slightly costlier.
            var special = GridCellFlags.Vaultable | GridCellFlags.Window | GridCellFlags.Climbable | GridCellFlags.Stairs;
            return (cell.flags & special) != 0 ? 0.35f : 0f;
        }

        private static float Heuristic(GridCoord a, GridCoord b, bool diagonalFifteen)
        {
            var dx = Mathf.Abs(a.x - b.x);
            var dy = Mathf.Abs(a.y - b.y);
            if (!diagonalFifteen)
            {
                return Mathf.Max(dx, dy);
            }

            var min = Mathf.Min(dx, dy);
            var max = Mathf.Max(dx, dy);
            return min * 1.5f + (max - min);
        }

        private static void Reconstruct(int[] cameFrom, int goalIndex, int width, List<GridCoord> pathOut)
        {
            var stack = new Stack<GridCoord>();
            var idx = goalIndex;
            while (idx >= 0)
            {
                stack.Push(FromIndex(idx, width));
                idx = cameFrom[idx];
            }

            while (stack.Count > 0)
            {
                pathOut.Add(stack.Pop());
            }
        }

        public static bool TryFindNearestWalkable(GridMap grid, GridCoord center, int radius, out GridCoord found)
        {
            found = center;
            if (grid.IsWalkable(center))
            {
                return true;
            }

            for (var r = 1; r <= radius; r++)
            {
                for (var y = -r; y <= r; y++)
                {
                    for (var x = -r; x <= r; x++)
                    {
                        if (Mathf.Abs(x) != r && Mathf.Abs(y) != r)
                        {
                            continue;
                        }

                        var c = new GridCoord(center.x + x, center.y + y);
                        if (grid.IsWalkable(c))
                        {
                            found = c;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static int Index(GridCoord coord, int width) => coord.y * width + coord.x;

        private static GridCoord FromIndex(int index, int width) =>
            new GridCoord(index % width, index / width);
    }
}

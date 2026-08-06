using System;
using GameDev.Grid;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameDev.World.Town
{
    /// <summary>
    /// Flood-fill blocks inside the town disk and place greybox buildings along street frontage.
    /// </summary>
    public static class TownBlockParcelizer
    {
        public static void ApplyRoadsToGrid(TownRoadGraph g, GridMap grid)
        {
            for (var y = 0; y < g.Height; y++)
            {
                for (var x = 0; x < g.Width; x++)
                {
                    var c = new GridCoord(x, y);
                    if (g.IsStreet(x, y))
                    {
                        grid.SetFlags(c, GridCellFlags.Walkable, walkable: true);
                        grid.SetHeight(c, g.HeightOf(c));
                    }
                    else if (g.InTown[x, y])
                    {
                        // Building fabric — blocked until plots carve nothing (buildings sit on blocked).
                        grid.SetFlags(c, GridCellFlags.BlocksLoS, walkable: false);
                        grid.SetHeight(c, g.HeightOf(c));
                    }
                    else
                    {
                        // Fields outside town — lightly walkable fringe near gates only later; default blocked scrub.
                        grid.SetFlags(c, GridCellFlags.None, walkable: false);
                        grid.SetHeight(c, 0);
                    }
                }
            }
        }

        public static void PlaceFactory(TownRoadGraph g, GridMap grid, Transform parent,
            GameObject building2x2, GameObject building1x1, GameObject wallSegment)
        {
            var x0 = g.FactoryRectMin.x;
            var y0 = g.FactoryRectMin.y;
            var x1 = g.FactoryRectMax.x;
            var y1 = g.FactoryRectMax.y;
            for (var y = y0; y <= y1; y++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    if (!g.InBounds(x, y))
                    {
                        continue;
                    }

                    var c = new GridCoord(x, y);
                    grid.SetFlags(c, GridCellFlags.BlocksLoS | GridCellFlags.Climbable, walkable: false);
                    grid.SetHeight(c, 2);
                    g.Road[x, y] = false;
                    g.Alley[x, y] = false;
                    g.Plaza[x, y] = false;
                }
            }

            // Keep a road collar on the west face of the factory.
            for (var y = y0; y <= y1; y++)
            {
                var x = x0 - 1;
                if (g.InBounds(x, y))
                {
                    g.MarkStreet(x, y, TownGrowthParams.MajorWidthVeil, alley: false);
                    grid.SetFlags(new GridCoord(x, y), GridCellFlags.Walkable, walkable: true);
                    grid.SetHeight(new GridCoord(x, y), g.HeightOf(new GridCoord(x, y)));
                }
            }

            PlaceBuildingMass(parent, grid, building2x2, building1x1, x0, y0, x1, y1);
            PlaceWallStrip(parent, grid, wallSegment, x0, y0, y1);
        }

        public static void PlaceFrontageBuildings(
            TownRoadGraph g,
            GridMap grid,
            Transform parent,
            Random rng,
            GameObject building1x1,
            GameObject building2x2)
        {
            var root = new GameObject("TownBuildings");
            root.transform.SetParent(parent, false);

            var visited = new bool[g.Width, g.Height];
            for (var y = 1; y < g.Height - 1; y++)
            {
                for (var x = 1; x < g.Width - 1; x++)
                {
                    if (visited[x, y] || g.IsStreet(x, y) || !g.InTown[x, y])
                    {
                        continue;
                    }

                    // Only cells adjacent to a street (frontage).
                    if (!TouchesStreet(g, x, y))
                    {
                        continue;
                    }

                    var wealth = g.WealthOf(new GridCoord(x, y));
                    var depth = wealth == TownWealth.Rich ? 3 + rng.Next(0, 3)
                        : wealth == TownWealth.Poor ? 1 + rng.Next(0, 2)
                        : 2 + rng.Next(0, 2);
                    var width = wealth == TownWealth.Rich ? 2 + rng.Next(0, 3)
                        : wealth == TownWealth.Poor ? 1
                        : 1 + rng.Next(0, 2);

                    // Skip some for gardens / gaps (rich more gardens).
                    if (wealth == TownWealth.Rich && rng.Next(0, 100) < 18)
                    {
                        MarkGarden(g, grid, x, y, visited);
                        continue;
                    }

                    if (rng.Next(0, 100) < 8)
                    {
                        visited[x, y] = true;
                        continue;
                    }

                    PlacePlot(g, grid, root.transform, rng, building1x1, building2x2,
                        x, y, width, depth, visited);
                }
            }
        }

        private static bool TouchesStreet(TownRoadGraph g, int x, int y)
        {
            return g.IsStreet(x + 1, y) || g.IsStreet(x - 1, y) || g.IsStreet(x, y + 1) || g.IsStreet(x, y - 1);
        }

        private static void MarkGarden(TownRoadGraph g, GridMap grid, int x, int y, bool[,] visited)
        {
            for (var dy = 0; dy <= 1; dy++)
            {
                for (var dx = 0; dx <= 1; dx++)
                {
                    var cx = x + dx;
                    var cy = y + dy;
                    if (!g.InBounds(cx, cy) || g.IsStreet(cx, cy) || !g.InTown[cx, cy])
                    {
                        continue;
                    }

                    visited[cx, cy] = true;
                    // Soft open — still blocked for pathing (private garden).
                    grid.SetFlags(new GridCoord(cx, cy), GridCellFlags.BlocksLoS, walkable: false);
                }
            }
        }

        private static void PlacePlot(
            TownRoadGraph g,
            GridMap grid,
            Transform parent,
            Random rng,
            GameObject building1x1,
            GameObject building2x2,
            int x,
            int y,
            int width,
            int depth,
            bool[,] visited)
        {
            // Grow away from street: pick normal from nearest street neighbor.
            var ndx = 0;
            var ndy = 0;
            if (g.IsStreet(x + 1, y))
            {
                ndx = -1;
            }
            else if (g.IsStreet(x - 1, y))
            {
                ndx = 1;
            }
            else if (g.IsStreet(x, y + 1))
            {
                ndy = -1;
            }
            else
            {
                ndy = 1;
            }

            var px = ndy != 0 ? 1 : 0;
            var py = ndx != 0 ? 1 : 0;

            for (var d = 0; d < depth; d++)
            {
                for (var w = 0; w < width; w++)
                {
                    var cx = x + ndx * d + px * w;
                    var cy = y + ndy * d + py * w;
                    if (!g.InBounds(cx, cy) || g.IsStreet(cx, cy) || !g.InTown[cx, cy] || visited[cx, cy])
                    {
                        continue;
                    }

                    visited[cx, cy] = true;
                    grid.SetFlags(new GridCoord(cx, cy), GridCellFlags.BlocksLoS, walkable: false);
                    grid.SetHeight(new GridCoord(cx, cy), g.HeightOf(new GridCoord(cx, cy)));
                }
            }

            // Visual: 2x2 if rich/mixed and space, else 1x1 along front.
            if (building2x2 != null && width >= 2 && depth >= 2 && rng.Next(0, 100) < 55)
            {
                SpawnBuilding(building2x2, parent, grid, x, y, $"B2_{x}_{y}", scale2x2: true);
            }
            else if (building1x1 != null)
            {
                for (var w = 0; w < width; w++)
                {
                    var cx = x + px * w;
                    var cy = y + py * w;
                    SpawnBuilding(building1x1, parent, grid, cx, cy, $"B1_{cx}_{cy}", scale2x2: false);
                }
            }
        }

        private static void PlaceBuildingMass(
            Transform parent,
            GridMap grid,
            GameObject building2x2,
            GameObject building1x1,
            int x0,
            int y0,
            int x1,
            int y1)
        {
            var root = new GameObject($"FactoryMass_{x0}_{y0}");
            root.transform.SetParent(parent, false);
            var y = y0;
            while (y <= y1)
            {
                var use2 = building2x2 != null && y + 1 <= y1;
                var x = x0;
                while (x <= x1)
                {
                    if (use2 && x + 1 <= x1)
                    {
                        SpawnBuilding(building2x2, root.transform, grid, x, y, $"Factory2_{x}_{y}", true);
                        x += 2;
                    }
                    else if (building1x1 != null)
                    {
                        SpawnBuilding(building1x1, root.transform, grid, x, y, $"Factory1_{x}_{y}", false);
                        x += 1;
                    }
                    else
                    {
                        x += 1;
                    }
                }

                y += use2 ? 2 : 1;
            }
        }

        private static void PlaceWallStrip(
            Transform parent,
            GridMap grid,
            GameObject wallSegment,
            int x,
            int y0,
            int y1)
        {
            if (wallSegment == null)
            {
                return;
            }

            var root = new GameObject("FactoryWallStrip");
            root.transform.SetParent(parent, false);
            for (var y = y0; y <= y1; y++)
            {
                SpawnBuilding(wallSegment, root.transform, grid, x, y, $"Wall_{x}_{y}", false);
            }
        }

        private static void SpawnBuilding(
            GameObject prefab,
            Transform parent,
            GridMap grid,
            int x,
            int y,
            string name,
            bool scale2x2)
        {
            GameObject go;
            if (prefab != null)
            {
#if UNITY_EDITOR
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
#else
                go = UnityEngine.Object.Instantiate(prefab, parent);
#endif
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.SetParent(parent, false);
            }

            go.name = name;
            if (scale2x2)
            {
                var a = grid.CoordToWorldCenter(new GridCoord(x, y));
                var b = grid.CoordToWorldCenter(new GridCoord(x + 1, y + 1));
                var pos = (a + b) * 0.5f;
                pos.y = 0f;
                go.transform.position = pos;
            }
            else
            {
                var pos = grid.CoordToWorldCenter(new GridCoord(x, y));
                pos.y = 0f;
                go.transform.position = pos;
            }
        }
    }
}

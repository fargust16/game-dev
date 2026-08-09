using System.Collections.Generic;
using GameDev.Grid;
using GameDev.World.Town.Geometry;
using UnityEngine;

namespace GameDev.World.Town
{
    /// <summary>
    /// P0 — rasterize OrganicTown v2 roads + enterable interiors into a limited-radius <see cref="GridMap"/>.
    /// </summary>
    public static class TownGridBaker
    {
        public const string RootName = "Town_PlayableGrid";
        public const float DefaultPlayableRadiusMetres = 90f;
        public const float DefaultCellSize = 1f;

        public struct BakeResult
        {
            public GridMap Grid;
            public GridCoord Spawn;
            public Vector2 Center;
            public float RadiusMetres;
            public int WalkableCount;
            public int RoadCellCount;
            public int InteriorCellCount;
            public int DoorCellCount;
            public bool SpawnWalkable;
            public bool HasPortalDirectionWalkable;
        }

        public static BakeResult Bake(
            Transform parent,
            TownModel model,
            float playableRadiusMetres = DefaultPlayableRadiusMetres,
            float cellSize = DefaultCellSize)
        {
            Clear(parent);

            var result = new BakeResult
            {
                RadiusMetres = Mathf.Max(10f, playableRadiusMetres),
                Center = ResolveCenter(model)
            };

            if (model == null)
            {
                Debug.LogWarning("[TownGridBaker] No TownModel.");
                return result;
            }

            var half = result.RadiusMetres;
            var origin = new Vector3(result.Center.x - half, 0f, result.Center.y - half);
            var dim = Mathf.CeilToInt((half * 2f) / Mathf.Max(0.25f, cellSize));
            dim = Mathf.Max(8, dim);

            var root = new GameObject(RootName);
            root.transform.SetParent(parent, false);
            var grid = root.AddComponent<GridMap>();
            grid.Configure(dim, dim, cellSize, origin);
            root.AddComponent<GridDebugDrawer>();
            BlockAll(grid);

            var radiusSq = result.RadiusMetres * result.RadiusMetres;
            PaintPlaza(grid, model, result.Center, radiusSq, ref result);
            PaintRoads(grid, model, result.Center, radiusSq, ref result);
            PaintInteriors(grid, model, result.Center, radiusSq, ref result);
            TagDoorAdjacentWindows(grid);

            result.Grid = grid;
            result.WalkableCount = CountWalkable(grid);
            result.Spawn = FindSpawn(grid, result.Center);
            result.SpawnWalkable = grid.IsWalkable(result.Spawn);
            result.HasPortalDirectionWalkable = CheckPortalDirection(grid, model, result.Center, result.Spawn);

            Debug.Log(
                $"[TownGridBaker] radius={result.RadiusMetres:0}m cells={dim}x{dim} " +
                $"walkable={result.WalkableCount} roads={result.RoadCellCount} " +
                $"interiors={result.InteriorCellCount} doors={result.DoorCellCount} " +
                $"spawn={result.Spawn} ok={result.SpawnWalkable}");

            return result;
        }

        public static void Clear(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child != null && child.name == RootName)
                {
                    SafeDestroy(child.gameObject);
                }
            }
        }

        public static Vector2 ResolveCenter(TownModel model)
        {
            if (model?.Landmark?.Plaza != null && model.Landmark.Plaza.Count >= 3)
            {
                return model.Landmark.Plaza.Centroid;
            }

            if (model?.Landmark?.Footprint != null && model.Landmark.Footprint.Count >= 3)
            {
                return model.Landmark.Footprint.Centroid;
            }

            return model != null ? model.Center : Vector2.zero;
        }

        private static void BlockAll(GridMap grid)
        {
            for (var y = 0; y < grid.Height; y++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    grid.SetFlags(new GridCoord(x, y), GridCellFlags.None, walkable: false);
                }
            }
        }

        private static void PaintPlaza(
            GridMap grid,
            TownModel model,
            Vector2 center,
            float radiusSq,
            ref BakeResult result)
        {
            var plaza = model.Landmark?.Plaza;
            if (plaza == null || plaza.Count < 3)
            {
                return;
            }

            var verts = plaza.Vertices;
            for (var y = 0; y < grid.Height; y++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    var c = new GridCoord(x, y);
                    var w = WorldXZ(grid, c);
                    if ((w - center).sqrMagnitude > radiusSq)
                    {
                        continue;
                    }

                    if (!Geo2.PointInPolygon(w, verts))
                    {
                        continue;
                    }

                    grid.SetFlags(c, GridCellFlags.Walkable, walkable: true);
                    result.RoadCellCount++;
                }
            }
        }

        private static void PaintRoads(
            GridMap grid,
            TownModel model,
            Vector2 center,
            float radiusSq,
            ref BakeResult result)
        {
            var sampleStep = Mathf.Max(0.25f, grid.CellSize * 0.5f);
            for (var ei = 0; ei < model.RoadEdges.Count; ei++)
            {
                var edge = model.RoadEdges[ei];
                if (edge?.Polyline == null || edge.Polyline.Count < 2)
                {
                    continue;
                }

                var halfWidth = Mathf.Max(0.5f, edge.Width * 0.5f);
                var samples = Geo2.ResamplePolyline(edge.Polyline, sampleStep);
                for (var si = 0; si < samples.Count; si++)
                {
                    var p = samples[si];
                    if ((p - center).sqrMagnitude > radiusSq)
                    {
                        continue;
                    }

                    PaintDisk(grid, p, halfWidth, GridCellFlags.Walkable, ref result.RoadCellCount);
                }
            }
        }

        private static void PaintInteriors(
            GridMap grid,
            TownModel model,
            Vector2 center,
            float radiusSq,
            ref BakeResult result)
        {
            for (var bi = 0; bi < model.Buildings.Count; bi++)
            {
                var building = model.Buildings[bi];
                if (building == null || !building.HasTemplate)
                {
                    continue;
                }

                if (building.Kind == BuildingKind.Landmark)
                {
                    continue;
                }

                var footprintCenter = building.Footprint != null && building.Footprint.Count >= 3
                    ? building.Footprint.Centroid
                    : building.TemplateOrigin;
                if ((footprintCenter - center).sqrMagnitude > radiusSq)
                {
                    continue;
                }

                var template = BuildingTemplateCatalog.Get(building.TemplateId);
                if (template == null)
                {
                    continue;
                }

                for (var ly = 0; ly < template.Height; ly++)
                {
                    for (var lx = 0; lx < template.Width; lx++)
                    {
                        var kind = template.GetCell(lx, ly);
                        if (kind == BuildingInteriorCellKind.Empty ||
                            kind == BuildingInteriorCellKind.Wall)
                        {
                            continue;
                        }

                        var world = TownFootprintUtil.CellCenterWorld(building, template, lx, ly);
                        if ((world - center).sqrMagnitude > radiusSq)
                        {
                            continue;
                        }

                        var coord = grid.WorldToCoord(Geo2.ToVector3(world));
                        if (!grid.InBounds(coord))
                        {
                            continue;
                        }

                        if (kind == BuildingInteriorCellKind.Door)
                        {
                            grid.SetFlags(coord, GridCellFlags.Walkable | GridCellFlags.Door, walkable: true);
                            result.DoorCellCount++;
                        }
                        else
                        {
                            grid.SetFlags(coord, GridCellFlags.Walkable, walkable: true);
                        }

                        result.InteriorCellCount++;
                    }
                }
            }
        }

        private static void TagDoorAdjacentWindows(GridMap grid)
        {
            // Soft P0 stub: mark blocked cells next to doors as Window (climb-through candidates).
            var doors = new List<GridCoord>(64);
            for (var y = 0; y < grid.Height; y++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    var c = new GridCoord(x, y);
                    if (!grid.TryGetCell(c, out var cell))
                    {
                        continue;
                    }

                    if ((cell.flags & GridCellFlags.Door) != 0)
                    {
                        doors.Add(c);
                    }
                }
            }

            var offsets = new[]
            {
                new GridCoord(1, 0), new GridCoord(-1, 0),
                new GridCoord(0, 1), new GridCoord(0, -1)
            };

            for (var i = 0; i < doors.Count; i++)
            {
                var door = doors[i];
                for (var oi = 0; oi < offsets.Length; oi++)
                {
                    var n = new GridCoord(door.x + offsets[oi].x, door.y + offsets[oi].y);
                    if (!grid.TryGetCell(n, out var cell))
                    {
                        continue;
                    }

                    if ((cell.flags & GridCellFlags.Walkable) != 0)
                    {
                        continue;
                    }

                    // Only tag one side wall per door for env placement later.
                    grid.SetFlags(n, GridCellFlags.Window, walkable: false);
                    break;
                }
            }
        }

        private static void PaintDisk(
            GridMap grid,
            Vector2 worldXZ,
            float radiusMetres,
            GridCellFlags flags,
            ref int paintedCount)
        {
            var min = grid.WorldToCoord(new Vector3(worldXZ.x - radiusMetres, 0f, worldXZ.y - radiusMetres));
            var max = grid.WorldToCoord(new Vector3(worldXZ.x + radiusMetres, 0f, worldXZ.y + radiusMetres));
            var rSq = radiusMetres * radiusMetres;
            for (var y = min.y; y <= max.y; y++)
            {
                for (var x = min.x; x <= max.x; x++)
                {
                    var c = new GridCoord(x, y);
                    if (!grid.InBounds(c))
                    {
                        continue;
                    }

                    var w = WorldXZ(grid, c);
                    if ((w - worldXZ).sqrMagnitude > rSq)
                    {
                        continue;
                    }

                    if (!grid.IsWalkable(c))
                    {
                        paintedCount++;
                    }

                    grid.SetFlags(c, flags, walkable: true);
                }
            }
        }

        private static GridCoord FindSpawn(GridMap grid, Vector2 center)
        {
            var preferred = grid.WorldToCoord(Geo2.ToVector3(center));
            if (grid.IsWalkable(preferred))
            {
                return preferred;
            }

            GridCoord best = preferred;
            var bestDist = int.MaxValue;
            for (var y = 0; y < grid.Height; y++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    var c = new GridCoord(x, y);
                    if (!grid.IsWalkable(c))
                    {
                        continue;
                    }

                    var dx = c.x - preferred.x;
                    var dy = c.y - preferred.y;
                    var d = dx * dx + dy * dy;
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = c;
                    }
                }
            }

            return best;
        }

        private static bool CheckPortalDirection(
            GridMap grid,
            TownModel model,
            Vector2 center,
            GridCoord spawn)
        {
            if (model.Portals == null || model.Portals.Count == 0)
            {
                return false;
            }

            // Any walkable cell farther from center toward a portal counts as "portal direction".
            for (var i = 0; i < model.Portals.Count; i++)
            {
                var portal = model.Portals[i].Pos;
                var dir = (portal - center);
                if (dir.sqrMagnitude < 1f)
                {
                    continue;
                }

                dir.Normalize();
                var probe = center + dir * Mathf.Min(40f, grid.Width * 0.35f * grid.CellSize);
                var coord = grid.WorldToCoord(Geo2.ToVector3(probe));
                if (grid.IsWalkable(coord))
                {
                    return true;
                }

                // Ring search around probe.
                for (var r = 1; r <= 6; r++)
                {
                    for (var dy = -r; dy <= r; dy++)
                    {
                        for (var dx = -r; dx <= r; dx++)
                        {
                            var c = new GridCoord(coord.x + dx, coord.y + dy);
                            if (grid.IsWalkable(c) && !c.Equals(spawn))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static int CountWalkable(GridMap grid)
        {
            var n = 0;
            for (var y = 0; y < grid.Height; y++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    if (grid.IsWalkable(new GridCoord(x, y)))
                    {
                        n++;
                    }
                }
            }

            return n;
        }

        private static Vector2 WorldXZ(GridMap grid, GridCoord c)
        {
            var w = grid.CoordToWorldCenter(c);
            return new Vector2(w.x, w.z);
        }

        private static void SafeDestroy(Object obj)
        {
            if (obj == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(obj);
                return;
            }
#endif
            Object.Destroy(obj);
        }
    }
}

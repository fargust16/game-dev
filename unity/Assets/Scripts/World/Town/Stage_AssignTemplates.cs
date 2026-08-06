using System.Collections.Generic;
using GameDev.World.Town.Geometry;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Town
{
    /// <summary>
    /// Milestone D1.1 — assign enterable <see cref="BuildingTemplate"/> schematics to footprints.
    /// Door faces street frontage; buildings that cannot fit keep <see cref="BuildingTemplateId.None"/>.
    /// </summary>
    public static class Stage_AssignTemplates
    {
        private static readonly List<BuildingTemplate> FitBuffer = new List<BuildingTemplate>(8);
        private static readonly List<BuildingTemplate> PreferBuffer = new List<BuildingTemplate>(8);

        public static void Run(TownModel model, TownProfile profile, Random rng)
        {
            if (model == null)
            {
                return;
            }

            _ = profile;
            var catalog = BuildingTemplateCatalog.All;
            for (var bi = 0; bi < model.Buildings.Count; bi++)
            {
                var building = model.Buildings[bi];
                building.ClearTemplate();

                if (building.Kind == BuildingKind.Landmark)
                {
                    continue;
                }

                if (building.Footprint == null || building.Footprint.Count < 3)
                {
                    continue;
                }

                var frontage = ResolveFrontage(model, building);
                CollectFits(building, frontage, catalog, FitBuffer);
                if (FitBuffer.Count == 0)
                {
                    continue;
                }

                var pick = PickTemplate(FitBuffer, building, model, rng);
                if (pick == null)
                {
                    continue;
                }

                if (!TryComputePlacement(building.Footprint, frontage, pick, out var origin, out var yawDeg))
                {
                    continue;
                }

                building.TemplateId = pick.Id;
                building.TemplateOrigin = origin;
                building.TemplateYawDeg = yawDeg;
            }
        }

        private static Vector2 ResolveFrontage(TownModel model, Building building)
        {
            if (building.PlotIndex >= 0 && building.PlotIndex < model.Plots.Count)
            {
                var dir = model.Plots[building.PlotIndex].FrontageDir;
                if (dir.sqrMagnitude > 1e-6f)
                {
                    return dir.normalized;
                }
            }

            if (!TownFootprintUtil.TryObb(building.Footprint, out _, out var axis, out _, out _))
            {
                return Vector2.up;
            }

            // Treat OBB long axis as depth; door faces opposite of axis.
            return -axis.normalized;
        }

        private static void CollectFits(
            Building building,
            Vector2 frontage,
            IReadOnlyList<BuildingTemplate> catalog,
            List<BuildingTemplate> into)
        {
            into.Clear();
            if (!TryMeasureAlongFrontage(building.Footprint, frontage, out var width, out var depth))
            {
                return;
            }

            for (var i = 0; i < catalog.Count; i++)
            {
                var t = catalog[i];
                if (!t.SupportsKind(building.Kind))
                {
                    continue;
                }

                if (t.WorldWidth <= width + 0.05f && t.WorldDepth <= depth + 0.05f)
                {
                    into.Add(t);
                }
            }
        }

        private static BuildingTemplate PickTemplate(
            List<BuildingTemplate> fits,
            Building building,
            TownModel model,
            Random rng)
        {
            if (fits.Count == 1)
            {
                return fits[0];
            }

            var wealth = WealthClass.Mixed;
            if (building.PlotIndex >= 0 && building.PlotIndex < model.Plots.Count)
            {
                var plot = model.Plots[building.PlotIndex];
                if (plot.BlockIndex >= 0 && plot.BlockIndex < model.Blocks.Count)
                {
                    var block = model.Blocks[plot.BlockIndex];
                    if (block.DistrictIndex >= 0 && block.DistrictIndex < model.Districts.Count)
                    {
                        wealth = model.Districts[block.DistrictIndex].Wealth;
                    }
                }
            }

            // Prefer wealth match; fall back to all fits. Sorted by Id for stable ties.
            fits.Sort((a, b) => a.Id.CompareTo(b.Id));
            PreferBuffer.Clear();
            for (var i = 0; i < fits.Count; i++)
            {
                if (fits[i].PrefersWealth(wealth))
                {
                    PreferBuffer.Add(fits[i]);
                }
            }

            var pool = PreferBuffer.Count > 0 ? PreferBuffer : fits;
            return pool[rng.Next(0, pool.Count)];
        }

        private static bool TryComputePlacement(
            Polygon2 footprint,
            Vector2 frontage,
            BuildingTemplate template,
            out Vector2 origin,
            out float yawDeg)
        {
            origin = default;
            yawDeg = 0f;
            if (frontage.sqrMagnitude < 1e-8f)
            {
                frontage = Vector2.up;
            }
            else
            {
                frontage = frontage.normalized;
            }

            // Local +Z (depth) points into the building; door on local −Z faces the street.
            var depth = -frontage;
            var right = new Vector2(depth.y, -depth.x);
            var center = footprint.Centroid;
            var needW = template.WorldWidth;
            var needD = template.WorldDepth;
            origin = center - right * (needW * 0.5f) - depth * (needD * 0.5f);
            yawDeg = Mathf.Atan2(depth.x, depth.y) * Mathf.Rad2Deg;
            return true;
        }

        private static bool TryMeasureAlongFrontage(
            Polygon2 footprint,
            Vector2 frontage,
            out float width,
            out float depth)
        {
            width = 0f;
            depth = 0f;
            var verts = footprint.Vertices;
            if (verts == null || verts.Count < 3)
            {
                return false;
            }

            if (frontage.sqrMagnitude < 1e-8f)
            {
                frontage = Vector2.up;
            }
            else
            {
                frontage = frontage.normalized;
            }

            var depthAxis = -frontage;
            var right = new Vector2(depthAxis.y, -depthAxis.x);
            var minR = float.MaxValue;
            var maxR = float.MinValue;
            var minD = float.MaxValue;
            var maxD = float.MinValue;
            for (var i = 0; i < verts.Count; i++)
            {
                var v = verts[i];
                var pr = Vector2.Dot(v, right);
                var pd = Vector2.Dot(v, depthAxis);
                if (pr < minR) minR = pr;
                if (pr > maxR) maxR = pr;
                if (pd < minD) minD = pd;
                if (pd > maxD) maxD = pd;
            }

            width = maxR - minR;
            depth = maxD - minD;
            return width > 0.5f && depth > 0.5f;
        }
    }

    /// <summary>Shared footprint OBB helpers for baker + template placement.</summary>
    public static class TownFootprintUtil
    {
        public static bool TryObb(
            Polygon2 poly,
            out Vector2 center,
            out Vector2 axis,
            out float sizeX,
            out float sizeZ)
        {
            center = poly != null ? poly.Centroid : Vector2.zero;
            axis = Vector2.up;
            sizeX = 1f;
            sizeZ = 1f;

            if (poly == null)
            {
                return false;
            }

            var verts = poly.Vertices;
            if (verts == null || verts.Count < 3)
            {
                return false;
            }

            var (longAxis, splitDir) = Geo2.ObbLongestAxis(verts);
            if (longAxis.sqrMagnitude < 1e-8f)
            {
                return false;
            }

            axis = longAxis.normalized;
            var perp = splitDir.sqrMagnitude > 1e-8f
                ? splitDir.normalized
                : new Vector2(-axis.y, axis.x);

            var minA = float.MaxValue;
            var maxA = float.MinValue;
            var minP = float.MaxValue;
            var maxP = float.MinValue;
            for (var i = 0; i < verts.Count; i++)
            {
                var v = verts[i];
                var pa = Vector2.Dot(v, axis);
                var pp = Vector2.Dot(v, perp);
                if (pa < minA) minA = pa;
                if (pa > maxA) maxA = pa;
                if (pp < minP) minP = pp;
                if (pp > maxP) maxP = pp;
            }

            sizeZ = Mathf.Max(0.5f, maxA - minA);
            sizeX = Mathf.Max(0.5f, maxP - minP);
            center = axis * ((minA + maxA) * 0.5f) + perp * ((minP + maxP) * 0.5f);
            return true;
        }

        /// <summary>World centre of template cell (x,y) after placement.</summary>
        public static Vector2 CellCenterWorld(Building building, BuildingTemplate template, int x, int y)
        {
            var cs = template.CellSize;
            var localX = (x + 0.5f) * cs;
            var localZ = (y + 0.5f) * cs;
            var yaw = building.TemplateYawDeg * Mathf.Deg2Rad;
            var cos = Mathf.Cos(yaw);
            var sin = Mathf.Sin(yaw);
            // Match Unity Y-rotation used by greybox baker.
            var wx = localX * cos + localZ * sin;
            var wz = -localX * sin + localZ * cos;
            return building.TemplateOrigin + new Vector2(wx, wz);
        }
    }
}

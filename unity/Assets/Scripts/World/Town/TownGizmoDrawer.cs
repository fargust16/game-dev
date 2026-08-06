using GameDev.World.Town.Geometry;
using UnityEngine;

namespace GameDev.World.Town
{
    /// <summary>Editor/runtime gizmo visualization for <see cref="TownModel"/>.</summary>
    public static class TownGizmoDrawer
    {
        public static void Draw(TownGenerator generator)
        {
            if (generator == null)
            {
                return;
            }

            var model = generator.Model;
            if (model == null || model.Bounds == null || model.Bounds.Count < 3)
            {
                return;
            }

            if (generator.DrawBoundary)
            {
                Gizmos.color = Color.white;
                DrawPoly(model.Bounds, y: 0.05f);
            }

            if (generator.DrawTerrain)
            {
                Gizmos.color = new Color(0.2f, 0.45f, 0.9f, 1f);
                for (var r = 0; r < model.Terrain.RiverPolylines.Count; r++)
                {
                    DrawPolyline(model.Terrain.RiverPolylines[r], 0.08f);
                }

                if (model.Terrain.Ridge != null)
                {
                    Gizmos.color = new Color(0.45f, 0.3f, 0.15f, 1f);
                    DrawPolyline(model.Terrain.Ridge, 0.12f);
                }
            }

            if (generator.DrawRoads)
            {
                for (var i = 0; i < model.RoadEdges.Count; i++)
                {
                    var e = model.RoadEdges[i];
                    Gizmos.color = ColorForRoad(e.Class);
                    DrawPolyline(e.Polyline, 0.1f);
                }
            }

            if (generator.DrawDistricts)
            {
                for (var i = 0; i < model.Districts.Count; i++)
                {
                    Gizmos.color = ColorForWealth(model.Districts[i].Wealth);
                    DrawPoly(model.Districts[i].Area, y: 0.02f);
                }
            }

            if (generator.DrawBlocks)
            {
                Gizmos.color = new Color(0.15f, 0.15f, 0.15f, 1f);
                for (var i = 0; i < model.Blocks.Count; i++)
                {
                    DrawPoly(model.Blocks[i].Outline, y: 0.04f);
                }
            }

            if (generator.DrawPlots)
            {
                Gizmos.color = new Color(0.35f, 0.35f, 0.4f, 1f);
                for (var i = 0; i < model.Plots.Count; i++)
                {
                    DrawPoly(model.Plots[i].Outline, y: 0.06f);
                }
            }

            if (generator.DrawBuildings)
            {
                Gizmos.color = new Color(0.7f, 0.55f, 0.35f, 1f);
                for (var i = 0; i < model.Buildings.Count; i++)
                {
                    DrawPolyFilledApprox(model.Buildings[i].Footprint, y: 0.15f);
                }
            }

            if (generator.DrawOpenSpaces)
            {
                for (var i = 0; i < model.OpenSpaces.Count; i++)
                {
                    var os = model.OpenSpaces[i];
                    Gizmos.color = os.Kind == OpenSpaceKind.Garden || os.Kind == OpenSpaceKind.Courtyard
                        ? new Color(0.3f, 0.7f, 0.35f, 1f)
                        : os.Kind == OpenSpaceKind.Well
                            ? new Color(0.5f, 0.75f, 1f, 1f)
                            : new Color(0.85f, 0.8f, 0.55f, 1f);
                    DrawPoly(os.Area, y: 0.07f);
                }
            }

            if (generator.DrawLandmark && model.Landmark != null)
            {
                Gizmos.color = Color.magenta;
                DrawPoly(model.Landmark.Plaza, y: 0.09f);
                Gizmos.color = new Color(1f, 0.2f, 0.6f, 1f);
                DrawPolyFilledApprox(model.Landmark.Footprint, y: 0.25f);
            }

            if (generator.DrawPortals)
            {
                for (var i = 0; i < model.Portals.Count; i++)
                {
                    var p = model.Portals[i];
                    Gizmos.color = ColorForPortal(p.Type);
                    Gizmos.DrawSphere(Geo2.ToVector3(p.Pos, 1f), 3f);
                }
            }
        }

        private static void DrawPoly(Polygon2 poly, float y)
        {
            if (poly == null || poly.Count < 2)
            {
                return;
            }

            for (var i = 0; i < poly.Count; i++)
            {
                var a = Geo2.ToVector3(poly[i], y);
                var b = Geo2.ToVector3(poly[(i + 1) % poly.Count], y);
                Gizmos.DrawLine(a, b);
            }
        }

        private static void DrawPolyFilledApprox(Polygon2 poly, float y)
        {
            DrawPoly(poly, y);
            if (poly == null || poly.Count < 3)
            {
                return;
            }

            var c = Geo2.ToVector3(poly.Centroid, y);
            for (var i = 0; i < poly.Count; i++)
            {
                Gizmos.DrawLine(c, Geo2.ToVector3(poly[i], y));
            }
        }

        private static void DrawPolyline(System.Collections.Generic.IReadOnlyList<Vector2> pts, float y)
        {
            if (pts == null || pts.Count < 2)
            {
                return;
            }

            for (var i = 0; i < pts.Count - 1; i++)
            {
                Gizmos.DrawLine(Geo2.ToVector3(pts[i], y), Geo2.ToVector3(pts[i + 1], y));
            }
        }

        private static Color ColorForRoad(RoadClass cls)
        {
            switch (cls)
            {
                case RoadClass.Arterial: return new Color(1f, 0.75f, 0.2f, 1f);
                case RoadClass.Secondary: return new Color(1f, 0.55f, 0.15f, 1f);
                case RoadClass.Tertiary: return new Color(1f, 0.85f, 0.3f, 1f);
                case RoadClass.Alley: return new Color(0.55f, 0.55f, 0.55f, 1f);
                default: return new Color(0.45f, 0.45f, 0.5f, 1f);
            }
        }

        private static Color ColorForWealth(WealthClass w)
        {
            switch (w)
            {
                case WealthClass.Rich: return new Color(0.2f, 0.7f, 0.35f, 0.35f);
                case WealthClass.Poor: return new Color(0.85f, 0.25f, 0.2f, 0.35f);
                default: return new Color(0.2f, 0.55f, 0.55f, 0.35f);
            }
        }

        private static Color ColorForPortal(PortalType t)
        {
            switch (t)
            {
                case PortalType.Gate: return Color.yellow;
                case PortalType.Bridge: return Color.cyan;
                case PortalType.FarmRoad: return new Color(0.6f, 0.9f, 0.3f);
                default: return new Color(1f, 0.5f, 0.2f);
            }
        }
    }
}

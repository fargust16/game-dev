using System.Collections.Generic;
using GameDev.World.Town.Geometry;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Town
{
    public static class Stage_PrimaryRoads
    {
        public static void Run(TownModel model, TownProfile profile, Random rng)
        {
            var portalCount = rng.Next(profile.PortalCountMin, profile.PortalCountMax + 1);
            portalCount = Mathf.Max(3, portalCount);

            // Evenly spaced around boundary with jitter.
            for (var i = 0; i < portalCount; i++)
            {
                var t = (i + TownGraphUtil.NextRange(rng, -0.08f, 0.08f)) / portalCount;
                t = (t % 1f + 1f) % 1f;
                var pos = TownGraphUtil.PointOnBoundary(model.Bounds, t);
                var type = PickPortalType(model, profile, pos, rng);
                model.Portals.Add(new Portal { Pos = pos, Type = type });
            }

            var centerNode = TownGraphUtil.AddNode(model, model.Center);

            for (var i = 0; i < model.Portals.Count; i++)
            {
                var portal = model.Portals[i];
                var portalNode = TownGraphUtil.AddNode(model, portal.Pos);
                // Milder curvature than profile defaults so arterials stay corridor-like.
                var mild = new RoadClassSettings
                {
                    WidthMin = profile.Arterial.WidthMin,
                    WidthMax = profile.Arterial.WidthMax,
                    CurvatureAmplitude = profile.Arterial.CurvatureAmplitude * 0.4f,
                    JitterAmplitude = profile.Arterial.JitterAmplitude * 0.5f
                };
                var poly = TownGraphUtil.BuildCurvedArterial(
                    model.Center, portal.Pos, mild, rng, controlPoints: 2 + rng.Next(0, 2));
                var width = TownGraphUtil.RoadWidth(profile, RoadClass.Arterial, rng);
                TownGraphUtil.AddEdge(model, centerNode, portalNode, poly, RoadClass.Arterial, width);
            }

            // Broken ring at ~55% radius.
            CarveBrokenRing(model, profile, rng);
        }

        private static PortalType PickPortalType(TownModel model, TownProfile profile, Vector2 pos, Random rng)
        {
            if (profile.HasWall)
            {
                // Near river → bridge, else gate.
                if (NearRiver(model, pos, 40f))
                {
                    return PortalType.Bridge;
                }

                return PortalType.Gate;
            }

            if (NearRiver(model, pos, 40f))
            {
                return PortalType.Bridge;
            }

            return (PortalType)TownGraphUtil.WeightedIndex(rng, profile.PortalWeights.ToArray());
        }

        private static bool NearRiver(TownModel model, Vector2 pos, float dist)
        {
            for (var r = 0; r < model.Terrain.RiverPolylines.Count; r++)
            {
                var line = model.Terrain.RiverPolylines[r];
                for (var i = 0; i < line.Count - 1; i++)
                {
                    var c = Geo2.ClosestPointOnSegment(pos, line[i], line[i + 1]);
                    if ((c - pos).sqrMagnitude <= dist * dist)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void CarveBrokenRing(TownModel model, TownProfile profile, Random rng)
        {
            var radius = Mathf.Sqrt(model.Bounds.Area / Mathf.PI) * 0.55f;
            var gaps = 2 + rng.Next(0, 2);
            var gapStart = new float[gaps];
            var gapWidth = new float[gaps];
            for (var i = 0; i < gaps; i++)
            {
                gapStart[i] = TownGraphUtil.NextRange(rng, 0f, Mathf.PI * 2f);
                gapWidth[i] = TownGraphUtil.NextRange(rng, 0.4f, 0.9f);
            }

            var steps = 48;
            var segment = new List<Vector2>(16);
            int? startNode = null;
            Vector2? prev = null;

            for (var s = 0; s <= steps; s++)
            {
                var ang = s / (float)steps * Mathf.PI * 2f;
                var inGap = false;
                for (var g = 0; g < gaps; g++)
                {
                    var d = Mathf.Abs(Mathf.DeltaAngle(ang * Mathf.Rad2Deg, gapStart[g] * Mathf.Rad2Deg)) *
                            Mathf.Deg2Rad;
                    if (d < gapWidth[g] * 0.5f)
                    {
                        inGap = true;
                        break;
                    }
                }

                if (inGap)
                {
                    FlushRingSegment(model, profile, rng, ref segment, ref startNode, ref prev);
                    continue;
                }

                var rr = radius + TownGraphUtil.NextRange(rng, -6f, 6f);
                var pt = model.Center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rr;
                if (!Geo2.PointInPolygon(pt, model.Bounds.Vertices))
                {
                    FlushRingSegment(model, profile, rng, ref segment, ref startNode, ref prev);
                    continue;
                }

                segment.Add(pt);
                prev = pt;
            }

            FlushRingSegment(model, profile, rng, ref segment, ref startNode, ref prev);
        }

        private static void FlushRingSegment(
            TownModel model,
            TownProfile profile,
            Random rng,
            ref List<Vector2> segment,
            ref int? startNode,
            ref Vector2? prev)
        {
            if (segment.Count < 2)
            {
                segment.Clear();
                startNode = null;
                prev = null;
                return;
            }

            var a = TownGraphUtil.FindOrAddNode(model, segment[0], 12f);
            var b = TownGraphUtil.FindOrAddNode(model, segment[segment.Count - 1], 12f);
            var poly = Geo2.SmoothCatmullRom(segment, 0.2f, 3);
            poly[0] = model.RoadNodes[a].Pos;
            poly[poly.Count - 1] = model.RoadNodes[b].Pos;
            var width = TownGraphUtil.RoadWidth(profile, RoadClass.Secondary, rng);
            TownGraphUtil.AddEdge(model, a, b, poly, RoadClass.Secondary, width);
            segment = new List<Vector2>(16);
            startNode = null;
            prev = null;
        }
    }
}

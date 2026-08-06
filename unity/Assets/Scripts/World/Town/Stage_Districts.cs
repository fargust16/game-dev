using System.Collections.Generic;
using GameDev.World.Town.Geometry;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Town
{
    /// <summary>
    /// Partition town into wedge districts between consecutive portal angles (robust fallback).
    /// </summary>
    public static class Stage_Districts
    {
        public static void Run(TownModel model, TownProfile profile, Random rng)
        {
            if (model.Portals.Count < 2)
            {
                AddWholeTownDistrict(model, profile, WealthClass.Mixed, DistrictType.Residential);
                return;
            }

            // Sort portals by angle around center.
            var order = new List<int>(model.Portals.Count);
            for (var i = 0; i < model.Portals.Count; i++)
            {
                order.Add(i);
            }

            order.Sort((a, b) =>
            {
                var aa = Angle(model.Center, model.Portals[a].Pos);
                var bb = Angle(model.Center, model.Portals[b].Pos);
                return aa.CompareTo(bb);
            });

            for (var i = 0; i < order.Count; i++)
            {
                var p0 = model.Portals[order[i]].Pos;
                var p1 = model.Portals[order[(i + 1) % order.Count]].Pos;
                var wedge = BuildWedge(model, p0, p1);
                if (wedge.Count < 3)
                {
                    continue;
                }

                var centroid = Geo2.PolygonCentroid(wedge);
                var wealth = ClassifyWealth(model, centroid, rng);
                var type = ClassifyType(wealth, i, order.Count, rng);
                var district = new District
                {
                    Area = TownGraphUtil.ToPolygon(wedge),
                    Wealth = wealth,
                    Type = type,
                    Profile = new DistrictProfile(profile.GetDefaultDistrictProfile(wealth))
                };
                model.Districts.Add(district);
            }

            if (model.Districts.Count == 0)
            {
                AddWholeTownDistrict(model, profile, WealthClass.Mixed, DistrictType.Residential);
            }
        }

        private static void AddWholeTownDistrict(
            TownModel model, TownProfile profile, WealthClass wealth, DistrictType type)
        {
            model.Districts.Add(new District
            {
                Area = TownGraphUtil.ToPolygon(model.Bounds.ToList()),
                Wealth = wealth,
                Type = type,
                Profile = new DistrictProfile(profile.GetDefaultDistrictProfile(wealth))
            });
        }

        private static float Angle(Vector2 center, Vector2 p) =>
            Mathf.Atan2(p.y - center.y, p.x - center.x);

        private static List<Vector2> BuildWedge(TownModel model, Vector2 portalA, Vector2 portalB)
        {
            // Center → A along boundary arc (shorter) → B → center.
            // Approximate: sample boundary points between the two portal parameters.
            var ta = BoundaryParam(model.Bounds, portalA);
            var tb = BoundaryParam(model.Bounds, portalB);
            if (tb < ta)
            {
                tb += 1f;
            }

            var verts = new List<Vector2>(24) { model.Center };
            var samples = 10;
            for (var s = 0; s <= samples; s++)
            {
                var t = ta + (tb - ta) * (s / (float)samples);
                t = t % 1f;
                if (t < 0f)
                {
                    t += 1f;
                }

                verts.Add(TownGraphUtil.PointOnBoundary(model.Bounds, t));
            }

            // Shrink slightly from center so plaza is not inside district fabric.
            var plazaR = model.Landmark != null
                ? Mathf.Sqrt(model.Landmark.Plaza.Area / Mathf.PI)
                : 12f;
            for (var i = 1; i < verts.Count; i++)
            {
                var d = verts[i] - model.Center;
                var len = d.magnitude;
                if (len > plazaR + 4f)
                {
                    verts[i] = model.Center + d * ((len - plazaR * 0.5f) / len);
                }
            }

            TownGraphUtil.EnsureCcW(verts);
            return verts;
        }

        private static float BoundaryParam(Polygon2 bounds, Vector2 pos)
        {
            var verts = bounds.Vertices;
            var bestT = 0f;
            var bestD = float.MaxValue;
            var total = 0f;
            var lengths = new float[verts.Count];
            for (var i = 0; i < verts.Count; i++)
            {
                lengths[i] = Vector2.Distance(verts[i], verts[(i + 1) % verts.Count]);
                total += lengths[i];
            }

            var acc = 0f;
            for (var i = 0; i < verts.Count; i++)
            {
                var a = verts[i];
                var b = verts[(i + 1) % verts.Count];
                var c = Geo2.ClosestPointOnSegment(pos, a, b);
                var d = (c - pos).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    var segT = lengths[i] > 1e-5f ? Vector2.Distance(a, c) / lengths[i] : 0f;
                    bestT = (acc + segT * lengths[i]) / Mathf.Max(1e-5f, total);
                }

                acc += lengths[i];
            }

            return bestT;
        }

        private static WealthClass ClassifyWealth(TownModel model, Vector2 centroid, Random rng)
        {
            var dist = (centroid - model.Center).magnitude;
            var townR = Mathf.Sqrt(model.Bounds.Area / Mathf.PI);
            var norm = townR > 1f ? dist / townR : 0f;

            var ridgeBias = 0f;
            if (model.Terrain.Ridge != null && model.Terrain.Ridge.Count > 1)
            {
                var closest = float.MaxValue;
                for (var i = 0; i < model.Terrain.Ridge.Count - 1; i++)
                {
                    var c = Geo2.ClosestPointOnSegment(centroid, model.Terrain.Ridge[i], model.Terrain.Ridge[i + 1]);
                    closest = Mathf.Min(closest, (c - centroid).magnitude);
                }

                if (closest < townR * 0.35f)
                {
                    ridgeBias = 0.35f;
                }
            }

            var score = (1f - norm) * 0.5f + ridgeBias + TownGraphUtil.NextRange(rng, -0.1f, 0.1f);
            if (score > 0.55f)
            {
                return WealthClass.Rich;
            }

            if (score < 0.28f)
            {
                return WealthClass.Poor;
            }

            return WealthClass.Mixed;
        }

        private static DistrictType ClassifyType(WealthClass wealth, int index, int count, Random rng)
        {
            if (index == 0)
            {
                return DistrictType.Market;
            }

            if (index == count / 2)
            {
                return DistrictType.Civic;
            }

            if (wealth == WealthClass.Poor && rng.NextDouble() < 0.35)
            {
                return DistrictType.Industrial;
            }

            return DistrictType.Residential;
        }
    }
}

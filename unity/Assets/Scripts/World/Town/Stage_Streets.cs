using System.Collections.Generic;
using GameDev.World.Town.Geometry;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Town
{
    /// <summary>Recursive OBB subdivision of districts → blocks + minor streets.</summary>
    public static class Stage_Streets
    {
        private const int MaxDepth = 8;

        public static void Run(TownModel model, TownProfile profile, Random rng)
        {
            for (var di = 0; di < model.Districts.Count; di++)
            {
                var district = model.Districts[di];
                var target = Mathf.Max(18f, district.Profile.BlockSize);
                Subdivide(model, profile, di, district.Area.ToList(), target, 0, rng, district.Profile);
            }

            // If subdivision produced nothing, each district is one block.
            if (model.Blocks.Count == 0)
            {
                for (var di = 0; di < model.Districts.Count; di++)
                {
                    model.Blocks.Add(new Block
                    {
                        Outline = TownGraphUtil.ToPolygon(model.Districts[di].Area.ToList()),
                        DistrictIndex = di
                    });
                }
            }
        }

        private static void Subdivide(
            TownModel model,
            TownProfile profile,
            int districtIndex,
            List<Vector2> poly,
            float targetBlockSize,
            int depth,
            Random rng,
            DistrictProfile dp)
        {
            if (poly == null || poly.Count < 3)
            {
                return;
            }

            TownGraphUtil.EnsureCcW(poly);
            var area = Mathf.Abs(Geo2.PolygonArea(poly));
            var approxSide = Mathf.Sqrt(area);
            if (depth >= MaxDepth || approxSide <= targetBlockSize * (1.1f - dp.StreetDensity * 0.25f))
            {
                model.Blocks.Add(new Block
                {
                    Outline = TownGraphUtil.ToPolygon(poly),
                    DistrictIndex = districtIndex
                });
                return;
            }

            var (axis, splitDir) = Geo2.ObbLongestAxis(poly);
            var centroid = Geo2.PolygonCentroid(poly);

            // T-junction bias: offset split point along axis.
            var offset = axis * TownGraphUtil.NextRange(rng, -approxSide * 0.15f, approxSide * 0.15f);
            var splitPoint = centroid + offset;

            // Prefer nearly perpendicular split; slight rotation for organic feel.
            var ang = TownGraphUtil.NextRange(rng, -0.2f, 0.2f);
            var c = Mathf.Cos(ang);
            var s = Mathf.Sin(ang);
            var dir = new Vector2(splitDir.x * c - splitDir.y * s, splitDir.x * s + splitDir.y * c);

            var (left, right) = Geo2.SplitPolygonByLine(poly, splitPoint, dir);
            if (left.Count < 3 || right.Count < 3)
            {
                model.Blocks.Add(new Block
                {
                    Outline = TownGraphUtil.ToPolygon(poly),
                    DistrictIndex = districtIndex
                });
                return;
            }

            // Street along split: connect midpoints of shared edge approximately.
            AddSplitStreet(model, profile, left, right, dp, rng);

            Subdivide(model, profile, districtIndex, left, targetBlockSize, depth + 1, rng, dp);
            Subdivide(model, profile, districtIndex, right, targetBlockSize, depth + 1, rng, dp);

            // Occasional dead-end alley stub in poor/dense districts.
            if (rng.NextDouble() < dp.DeadEndProbability)
            {
                AddDeadEndStub(model, profile, poly, rng);
            }
        }

        private static void AddSplitStreet(
            TownModel model,
            TownProfile profile,
            List<Vector2> left,
            List<Vector2> right,
            DistrictProfile dp,
            Random rng)
        {
            var a = Geo2.PolygonCentroid(left);
            var b = Geo2.PolygonCentroid(right);
            // Pull toward shared mid.
            var mid = (a + b) * 0.5f;
            var from = Vector2.Lerp(a, mid, 0.35f);
            var to = Vector2.Lerp(b, mid, 0.35f);

            var cls = dp.StreetDensity > 0.6f ? RoadClass.Alley
                : dp.StreetDensity > 0.4f ? RoadClass.Tertiary
                : RoadClass.Secondary;
            var settings = profile.GetRoadClassSettings(cls);
            var poly = TownGraphUtil.BuildCurvedArterial(from, to, settings, rng, controlPoints: 2);
            var na = TownGraphUtil.AddNode(model, poly[0]);
            var nb = TownGraphUtil.AddNode(model, poly[poly.Count - 1]);
            var width = TownGraphUtil.RoadWidth(profile, cls, rng);
            var shortcut = rng.NextDouble() < dp.ShortcutProbability;
            TownGraphUtil.AddEdge(model, na, nb, poly, cls, width, deadEnd: false, shortcut: shortcut);
        }

        private static void AddDeadEndStub(TownModel model, TownProfile profile, List<Vector2> poly, Random rng)
        {
            var c = Geo2.PolygonCentroid(poly);
            var edge = poly[rng.Next(0, poly.Count)];
            var settings = profile.Alley;
            var line = TownGraphUtil.BuildCurvedArterial(edge, c, settings, rng, 1);
            var na = TownGraphUtil.AddNode(model, line[0]);
            var nb = TownGraphUtil.AddNode(model, line[line.Count - 1]);
            TownGraphUtil.AddEdge(model, na, nb, line, RoadClass.Alley,
                TownGraphUtil.RoadWidth(profile, RoadClass.Alley, rng), deadEnd: true);
        }
    }
}

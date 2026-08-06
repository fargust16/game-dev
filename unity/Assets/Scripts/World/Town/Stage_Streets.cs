using System.Collections.Generic;
using GameDev.World.Town.Geometry;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Town
{
    /// <summary>
    /// Recursive OBB subdivision of districts → blocks + minor streets.
    /// Streets follow split chords (shared block edges); leaf blocks are inset
    /// by half street width so building fabric does not swallow corridors.
    /// </summary>
    public static class Stage_Streets
    {
        private const int MaxDepth = 8;
        private const float NodeSnapMetres = 12f;
        private const float MinBlockArea = 40f;

        public static void Run(TownModel model, TownProfile profile, Random rng)
        {
            for (var di = 0; di < model.Districts.Count; di++)
            {
                var district = model.Districts[di];
                var target = Mathf.Max(18f, district.Profile.BlockSize);
                // Pull fabric off arterial / district rim a little before subdividing.
                var seedPoly = CarveDistrictSeed(district.Area.ToList(), profile);
                Subdivide(model, profile, di, seedPoly, target, 0, rng, district.Profile);
            }

            if (model.Blocks.Count == 0)
            {
                for (var di = 0; di < model.Districts.Count; di++)
                {
                    AddLeafBlock(model, di, model.Districts[di].Area.ToList(), profile, model.Districts[di].Profile);
                }
            }

            TownGraphUtil.MergeNearbyNodes(model, NodeSnapMetres * 0.75f);
        }

        private static List<Vector2> CarveDistrictSeed(List<Vector2> area, TownProfile profile)
        {
            var halfArterial = Mathf.Max(2f, profile.Arterial.WidthMin * 0.45f);
            var carved = Geo2.InsetPolygon(area, halfArterial);
            if (carved.Count >= 3 && Mathf.Abs(Geo2.PolygonArea(carved)) > MinBlockArea * 4f)
            {
                TownGraphUtil.EnsureCcW(carved);
                return carved;
            }

            TownGraphUtil.EnsureCcW(area);
            return area;
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
                AddLeafBlock(model, districtIndex, poly, profile, dp);
                return;
            }

            var (axis, splitDir) = Geo2.ObbLongestAxis(poly);
            var centroid = Geo2.PolygonCentroid(poly);

            // T-junction bias: offset split point along longest axis.
            var offset = axis * TownGraphUtil.NextRange(rng, -approxSide * 0.15f, approxSide * 0.15f);
            var splitPoint = centroid + offset;

            var ang = TownGraphUtil.NextRange(rng, -0.2f, 0.2f);
            var c = Mathf.Cos(ang);
            var s = Mathf.Sin(ang);
            var dir = new Vector2(splitDir.x * c - splitDir.y * s, splitDir.x * s + splitDir.y * c);

            if (!Geo2.TrySplitChord(poly, splitPoint, dir, out var chordA, out var chordB))
            {
                AddLeafBlock(model, districtIndex, poly, profile, dp);
                return;
            }

            var (left, right) = Geo2.SplitPolygonByLine(poly, splitPoint, dir);
            if (left.Count < 3 || right.Count < 3)
            {
                AddLeafBlock(model, districtIndex, poly, profile, dp);
                return;
            }

            var cls = PickStreetClass(dp);
            var width = TownGraphUtil.RoadWidth(profile, cls, rng);

            AddCorridorStreet(model, profile, chordA, chordB, cls, width, dp, rng);

            // Recurse on raw halves; leaf blocks inset once so corridors stay between fabric.
            Subdivide(model, profile, districtIndex, left, targetBlockSize, depth + 1, rng, dp);
            Subdivide(model, profile, districtIndex, right, targetBlockSize, depth + 1, rng, dp);

            if (rng.NextDouble() < dp.DeadEndProbability)
            {
                AddDeadEndStub(model, profile, chordA, chordB, left, right, rng);
            }
        }

        private static void AddLeafBlock(
            TownModel model,
            int districtIndex,
            List<Vector2> poly,
            TownProfile profile,
            DistrictProfile dp)
        {
            var cls = PickStreetClass(dp);
            var halfW = Mathf.Max(1.25f, profile.GetRoadClassSettings(cls).WidthMin * 0.5f);
            var outline = InsetOrFallback(poly, halfW);
            if (Mathf.Abs(Geo2.PolygonArea(outline)) < MinBlockArea)
            {
                return;
            }

            model.Blocks.Add(new Block
            {
                Outline = TownGraphUtil.ToPolygon(outline),
                DistrictIndex = districtIndex
            });
        }

        private static List<Vector2> InsetOrFallback(List<Vector2> poly, float distance)
        {
            var inset = Geo2.InsetPolygon(poly, distance);
            if (inset.Count >= 3 && Mathf.Abs(Geo2.PolygonArea(inset)) >= MinBlockArea)
            {
                TownGraphUtil.EnsureCcW(inset);
                return inset;
            }

            TownGraphUtil.EnsureCcW(poly);
            return poly;
        }

        private static RoadClass PickStreetClass(DistrictProfile dp)
        {
            if (dp.StreetDensity > 0.6f)
            {
                return RoadClass.Alley;
            }

            if (dp.StreetDensity > 0.4f)
            {
                return RoadClass.Tertiary;
            }

            return RoadClass.Secondary;
        }

        private static void AddCorridorStreet(
            TownModel model,
            TownProfile profile,
            Vector2 chordA,
            Vector2 chordB,
            RoadClass cls,
            float width,
            DistrictProfile dp,
            Random rng)
        {
            var settings = profile.GetRoadClassSettings(cls);
            var poly = TownGraphUtil.BuildCorridorPolyline(chordA, chordB, settings, rng);
            if (poly.Count < 2)
            {
                return;
            }

            var na = TownGraphUtil.FindOrAddNode(model, poly[0], NodeSnapMetres);
            var nb = TownGraphUtil.FindOrAddNode(model, poly[poly.Count - 1], NodeSnapMetres);
            // Keep polyline endpoints consistent with snapped node positions.
            poly[0] = model.RoadNodes[na].Pos;
            poly[poly.Count - 1] = model.RoadNodes[nb].Pos;

            var shortcut = rng.NextDouble() < dp.ShortcutProbability;
            TownGraphUtil.AddEdge(model, na, nb, poly, cls, width, deadEnd: false, shortcut: shortcut);
        }

        /// <summary>
        /// Short alley stub from the corridor into one half — not a cut through the whole block.
        /// </summary>
        private static void AddDeadEndStub(
            TownModel model,
            TownProfile profile,
            Vector2 chordA,
            Vector2 chordB,
            List<Vector2> left,
            List<Vector2> right,
            Random rng)
        {
            var chordMid = (chordA + chordB) * 0.5f;
            var chordDir = chordB - chordA;
            if (chordDir.sqrMagnitude < 1e-4f)
            {
                return;
            }

            chordDir = chordDir.normalized;
            var perp = new Vector2(-chordDir.y, chordDir.x);
            var leftC = Geo2.PolygonCentroid(left);
            var towardLeft = Vector2.Dot(leftC - chordMid, perp) >= 0f ? 1f : -1f;
            if (rng.Next(0, 2) == 0)
            {
                towardLeft = -towardLeft;
            }

            var stubLen = TownGraphUtil.NextRange(rng, 8f, 14f);
            var tip = chordMid + perp * (towardLeft * stubLen);
            var settings = profile.Alley;
            var line = TownGraphUtil.BuildCorridorPolyline(chordMid, tip, settings, rng);
            var na = TownGraphUtil.FindOrAddNode(model, line[0], NodeSnapMetres);
            var nb = TownGraphUtil.FindOrAddNode(model, line[line.Count - 1], NodeSnapMetres * 0.5f);
            line[0] = model.RoadNodes[na].Pos;
            line[line.Count - 1] = model.RoadNodes[nb].Pos;
            TownGraphUtil.AddEdge(model, na, nb, line, RoadClass.Alley,
                TownGraphUtil.RoadWidth(profile, RoadClass.Alley, rng), deadEnd: true);
        }
    }
}

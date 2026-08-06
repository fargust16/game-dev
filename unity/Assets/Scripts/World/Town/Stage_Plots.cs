using System.Collections.Generic;
using GameDev.World.Town.Geometry;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Town
{
    public static class Stage_Plots
    {
        public static void Run(TownModel model, TownProfile profile, Random rng)
        {
            for (var bi = 0; bi < model.Blocks.Count; bi++)
            {
                var block = model.Blocks[bi];
                var district = model.Districts[block.DistrictIndex];
                var dp = district.Profile;

                var insetDist = Mathf.Max(1f, dp.Setback + 1.5f);
                var inset = Geo2.InsetPolygon(block.Outline.Vertices, insetDist);
                if (inset.Count < 3 || Mathf.Abs(Geo2.PolygonArea(inset)) < 20f)
                {
                    // Keep whole block as one plot.
                    AddPlot(model, bi, block.Outline.ToList(), hasFrontage: true, frontageDir: Vector2.right);
                    continue;
                }

                // Courtyard leftover in dense districts.
                if (rng.NextDouble() < dp.CourtyardProbability)
                {
                    var courtyard = Geo2.InsetPolygon(inset, Mathf.Max(3f, dp.PlotDepthMin * 0.35f));
                    if (courtyard.Count >= 3)
                    {
                        model.OpenSpaces.Add(new OpenSpace
                        {
                            Area = TownGraphUtil.ToPolygon(courtyard),
                            Kind = OpenSpaceKind.Courtyard
                        });
                    }
                }

                SubdividePlots(model, bi, inset, dp, 0, rng);
            }
        }

        private static void SubdividePlots(
            TownModel model,
            int blockIndex,
            List<Vector2> poly,
            DistrictProfile dp,
            int depth,
            Random rng)
        {
            if (poly.Count < 3 || depth > 6)
            {
                AddPlot(model, blockIndex, poly, true, EstimateFrontageDir(poly));
                return;
            }

            var area = Mathf.Abs(Geo2.PolygonArea(poly));
            var targetFront = TownGraphUtil.NextRange(rng, dp.PlotFrontageMin, dp.PlotFrontageMax);
            var targetDepth = TownGraphUtil.NextRange(rng, dp.PlotDepthMin, dp.PlotDepthMax);
            var targetArea = targetFront * targetDepth;

            if (area <= targetArea * 1.35f)
            {
                if (rng.NextDouble() < dp.GardenProbability)
                {
                    model.OpenSpaces.Add(new OpenSpace
                    {
                        Area = TownGraphUtil.ToPolygon(poly),
                        Kind = OpenSpaceKind.Garden
                    });
                    return;
                }

                AddPlot(model, blockIndex, poly, true, EstimateFrontageDir(poly));
                return;
            }

            var (_, splitDir) = Geo2.ObbLongestAxis(poly);
            var centroid = Geo2.PolygonCentroid(poly);
            var (left, right) = Geo2.SplitPolygonByLine(poly, centroid, splitDir);
            if (left.Count < 3 || right.Count < 3)
            {
                AddPlot(model, blockIndex, poly, true, EstimateFrontageDir(poly));
                return;
            }

            SubdividePlots(model, blockIndex, left, dp, depth + 1, rng);
            SubdividePlots(model, blockIndex, right, dp, depth + 1, rng);
        }

        private static void AddPlot(
            TownModel model, int blockIndex, List<Vector2> poly, bool hasFrontage, Vector2 frontageDir)
        {
            TownGraphUtil.EnsureCcW(poly);
            model.Plots.Add(new Plot
            {
                Outline = TownGraphUtil.ToPolygon(poly),
                BlockIndex = blockIndex,
                HasFrontage = hasFrontage,
                FrontageDir = frontageDir.sqrMagnitude > 1e-6f ? frontageDir.normalized : Vector2.right
            });
        }

        private static Vector2 EstimateFrontageDir(List<Vector2> poly)
        {
            // Longest edge outward normal approximation → toward street.
            var bestLen = 0f;
            var bestDir = Vector2.right;
            for (var i = 0; i < poly.Count; i++)
            {
                var a = poly[i];
                var b = poly[(i + 1) % poly.Count];
                var e = b - a;
                var len = e.magnitude;
                if (len > bestLen)
                {
                    bestLen = len;
                    bestDir = new Vector2(-e.y, e.x).normalized;
                }
            }

            return bestDir;
        }
    }
}

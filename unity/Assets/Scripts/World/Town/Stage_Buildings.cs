using System.Collections.Generic;
using GameDev.World.Town.Geometry;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Town
{
    public static class Stage_Buildings
    {
        public static void Run(TownModel model, TownProfile profile, Random rng)
        {
            for (var pi = 0; pi < model.Plots.Count; pi++)
            {
                var plot = model.Plots[pi];
                if (!plot.HasFrontage)
                {
                    continue;
                }

                var block = model.Blocks[plot.BlockIndex];
                var district = model.Districts[block.DistrictIndex];
                var dp = district.Profile;

                if (rng.NextDouble() < dp.GardenProbability * 0.5f)
                {
                    continue;
                }

                var setback = Mathf.Max(0.4f, dp.Setback);
                var foot = Geo2.InsetPolygon(plot.Outline.Vertices, setback);
                if (foot.Count < 3 || Mathf.Abs(Geo2.PolygonArea(foot)) < 8f)
                {
                    foot = plot.Outline.ToList();
                }

                // Occasional L-corner: drop one vertex region by re-splitting.
                if (rng.NextDouble() < 0.12 && foot.Count >= 4)
                {
                    foot = MakeLShape(foot, rng);
                }

                var floors = TownGraphUtil.NextRange(rng, dp.FloorsMin, dp.FloorsMax + 0.99f);
                var kind = PickKind(district, rng);

                model.Buildings.Add(new Building
                {
                    Footprint = TownGraphUtil.ToPolygon(foot),
                    Floors = Mathf.Max(1f, floors),
                    PlotIndex = pi,
                    Kind = kind
                });
            }

            // Landmark building already exists as Landmark; also mirror as BuildingKind.Landmark.
            if (model.Landmark != null)
            {
                model.Buildings.Add(new Building
                {
                    Footprint = TownGraphUtil.ToPolygon(model.Landmark.Footprint.ToList()),
                    Floors = model.Landmark.Kind == LandmarkKind.Castle ? 4f : 2.5f,
                    PlotIndex = -1,
                    Kind = BuildingKind.Landmark
                });
            }
        }

        private static BuildingKind PickKind(District district, Random rng)
        {
            switch (district.Type)
            {
                case DistrictType.Market:
                    return rng.NextDouble() < 0.7 ? BuildingKind.Commercial : BuildingKind.Residential;
                case DistrictType.Civic:
                    if (rng.NextDouble() < 0.2)
                    {
                        return BuildingKind.Religious;
                    }

                    return rng.NextDouble() < 0.5 ? BuildingKind.Civic : BuildingKind.Residential;
                case DistrictType.Industrial:
                    return rng.NextDouble() < 0.6 ? BuildingKind.Industrial : BuildingKind.Residential;
                default:
                    return BuildingKind.Residential;
            }
        }

        private static List<Vector2> MakeLShape(List<Vector2> foot, Random rng)
        {
            var c = Geo2.PolygonCentroid(foot);
            var (_, splitDir) = Geo2.ObbLongestAxis(foot);
            var (left, right) = Geo2.SplitPolygonByLine(foot, c, splitDir);
            if (left.Count >= 3 && right.Count >= 3)
            {
                return rng.Next(0, 2) == 0 ? left : right;
            }

            return foot;
        }
    }
}

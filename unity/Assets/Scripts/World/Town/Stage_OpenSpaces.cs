using GameDev.World.Town.Geometry;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Town
{
    public static class Stage_OpenSpaces
    {
        public static void Run(TownModel model, TownProfile profile, Random rng)
        {
            // Landmark plaza.
            if (model.Landmark != null)
            {
                model.OpenSpaces.Add(new OpenSpace
                {
                    Area = TownGraphUtil.ToPolygon(model.Landmark.Plaza.ToList()),
                    Kind = OpenSpaceKind.Plaza
                });
            }

            // Junction plazas near high-degree road nodes.
            var plazaBudget = 4 + rng.Next(0, 5);
            for (var ni = 0; ni < model.RoadNodes.Count && plazaBudget > 0; ni++)
            {
                var node = model.RoadNodes[ni];
                if (node.Edges.Count < 3)
                {
                    continue;
                }

                if (rng.NextDouble() > 0.35)
                {
                    continue;
                }

                var r = TownGraphUtil.NextRange(rng, profile.PlazaSizeMin * 0.45f, profile.PlazaSizeMax * 0.65f);
                var verts = TownGraphUtil.MakeIrregularDisk(node.Pos, r, r * 0.2f, 8, rng);
                model.OpenSpaces.Add(new OpenSpace
                {
                    Area = TownGraphUtil.ToPolygon(verts),
                    Kind = OpenSpaceKind.Plaza
                });
                plazaBudget--;
            }

            // Wells by density.
            var areaKm2 = model.Bounds.Area / 1_000_000f;
            var wellTarget = Mathf.Max(1, Mathf.RoundToInt(areaKm2 * profile.WellDensityPerKm2));
            for (var w = 0; w < wellTarget; w++)
            {
                Vector2 pos;
                if (model.OpenSpaces.Count > 0 && rng.NextDouble() < 0.7)
                {
                    var os = model.OpenSpaces[rng.Next(0, model.OpenSpaces.Count)];
                    pos = os.Area.Centroid;
                }
                else if (model.RoadNodes.Count > 0)
                {
                    pos = model.RoadNodes[rng.Next(0, model.RoadNodes.Count)].Pos;
                }
                else
                {
                    pos = model.Center;
                }

                var well = TownGraphUtil.MakeRegularPolygon(pos, 1.2f, 6);
                model.OpenSpaces.Add(new OpenSpace
                {
                    Area = TownGraphUtil.ToPolygon(well),
                    Kind = OpenSpaceKind.Well
                });
            }
        }
    }
}

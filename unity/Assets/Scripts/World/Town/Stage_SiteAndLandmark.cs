using System.Collections.Generic;
using GameDev.World.Town.Geometry;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Town
{
    public static class Stage_SiteAndLandmark
    {
        public static void Run(TownModel model, TownProfile profile, Random rng)
        {
            model.Center = Vector2.zero;
            var radius = TownGraphUtil.NextRange(rng, profile.TownRadiusMin, profile.TownRadiusMax);
            var sides = 18 + rng.Next(0, 8);
            var boundary = TownGraphUtil.MakeIrregularDisk(
                model.Center, radius, profile.BoundaryIrregularity, sides, rng);
            model.Bounds = TownGraphUtil.ToPolygon(boundary);

            // Optional river / coast along one side of the town.
            if (rng.NextDouble() < profile.RiverCoastProbability)
            {
                var angle = TownGraphUtil.NextRange(rng, 0f, Mathf.PI * 2f);
                var river = new List<Vector2>(12);
                var along = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var across = new Vector2(-along.y, along.x);
                var offset = across * (radius * 0.75f);
                for (var i = 0; i < 12; i++)
                {
                    var t = (i / 11f) * 2f - 1f;
                    var pt = model.Center + offset + along * (t * radius * 1.2f);
                    pt += across * TownGraphUtil.NextRange(rng, -8f, 8f);
                    river.Add(pt);
                }

                model.Terrain.RiverPolylines.Add(river);
            }

            if (profile.HasRidge)
            {
                var ridgeAngle = TownGraphUtil.NextRange(rng, 0f, Mathf.PI);
                var ridgeDir = new Vector2(Mathf.Cos(ridgeAngle), Mathf.Sin(ridgeAngle));
                var ridge = new List<Vector2>(8);
                for (var i = 0; i < 8; i++)
                {
                    var t = (i / 7f) * 2f - 1f;
                    ridge.Add(model.Center + ridgeDir * (t * radius * 0.85f) +
                              new Vector2(-ridgeDir.y, ridgeDir.x) * TownGraphUtil.NextRange(rng, -12f, 12f));
                }

                model.Terrain.Ridge = ridge;
            }

            var kind = (LandmarkKind)TownGraphUtil.WeightedIndex(rng, profile.LandmarkWeights.ToArray());
            var plazaR = TownGraphUtil.NextRange(rng, profile.PlazaSizeMin, profile.PlazaSizeMax) * 1.4f;
            var plazaVerts = TownGraphUtil.MakeIrregularDisk(model.Center, plazaR, plazaR * 0.18f, 10, rng);
            var footR = plazaR * 0.35f;
            var footVerts = TownGraphUtil.MakeRegularPolygon(model.Center, footR, 6, TownGraphUtil.NextRange(rng, 0f, 1f));

            model.Landmark = new Landmark
            {
                Kind = kind,
                Plaza = TownGraphUtil.ToPolygon(plazaVerts),
                Footprint = TownGraphUtil.ToPolygon(footVerts)
            };
        }
    }
}

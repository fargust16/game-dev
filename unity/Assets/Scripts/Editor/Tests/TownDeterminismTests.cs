using System.Collections.Generic;
using GameDev.World;
using GameDev.World.Town;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Town.Tests
{
    public sealed class TownDeterminismTests
    {
        [Test]
        public void SameSeed_ProducesIdenticalCountsAndSampleGeometry()
        {
            var profile = ScriptableObject.CreateInstance<TownProfile>();
            const int seed = 110201;

            var a = Generate(profile, seed);
            var b = Generate(profile, seed);

            Assert.AreEqual(a.Portals.Count, b.Portals.Count);
            Assert.AreEqual(a.RoadEdges.Count, b.RoadEdges.Count);
            Assert.AreEqual(a.Districts.Count, b.Districts.Count);
            Assert.AreEqual(a.Blocks.Count, b.Blocks.Count);
            Assert.AreEqual(a.Plots.Count, b.Plots.Count);
            Assert.AreEqual(a.Buildings.Count, b.Buildings.Count);

            Assert.AreEqual(a.Center.x, b.Center.x, 1e-4f);
            Assert.AreEqual(a.Center.y, b.Center.y, 1e-4f);
            Assert.AreEqual(a.Bounds.Count, b.Bounds.Count);
            if (a.Bounds.Count > 0)
            {
                Assert.AreEqual(a.Bounds[0].x, b.Bounds[0].x, 1e-3f);
                Assert.AreEqual(a.Bounds[0].y, b.Bounds[0].y, 1e-3f);
            }

            if (a.RoadEdges.Count > 0 && a.RoadEdges[0].Polyline.Count > 0)
            {
                Assert.AreEqual(a.RoadEdges[0].Polyline[0].x, b.RoadEdges[0].Polyline[0].x, 1e-3f);
            }

            Object.DestroyImmediate(profile);
        }

        private static TownModel Generate(TownProfile profile, int seed)
        {
            var model = new TownModel();
            model.Reset(seed);
            var ws = new WorldSeed(seed);
            var log = new List<string>();
            Stage_SiteAndLandmark.Run(model, profile, ws.Fork("site"));
            Stage_PrimaryRoads.Run(model, profile, ws.Fork("primaryRoads"));
            Stage_Districts.Run(model, profile, ws.Fork("districts"));
            Stage_Streets.Run(model, profile, ws.Fork("streets"));
            Stage_Plots.Run(model, profile, ws.Fork("plots"));
            Stage_Buildings.Run(model, profile, ws.Fork("buildings"));
            Stage_OpenSpaces.Run(model, profile, ws.Fork("openSpaces"));
            Stage_ConnectivityAndScale.Run(model, profile, ws.Fork("connectivity"), log);
            return model;
        }
    }
}

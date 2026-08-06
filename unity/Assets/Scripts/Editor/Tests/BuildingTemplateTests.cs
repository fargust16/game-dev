using System.Collections.Generic;
using GameDev.World;
using GameDev.World.Town;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Town.Tests
{
    public sealed class BuildingTemplateTests
    {
        [Test]
        public void Catalog_StarterTemplates_HaveDoorAndOuterWall()
        {
            var all = BuildingTemplateCatalog.All;
            Assert.GreaterOrEqual(all.Count, 5);

            for (var i = 0; i < all.Count; i++)
            {
                var t = all[i];
                Assert.Greater(t.CountCells(BuildingInteriorCellKind.Door), 0, t.DisplayName);
                Assert.Greater(t.CountCells(BuildingInteriorCellKind.Wall), 0, t.DisplayName);
                Assert.Greater(t.CountCells(BuildingInteriorCellKind.Floor), 0, t.DisplayName);

                // Door must sit on the street edge (local y == 0).
                var doorOnSouth = false;
                for (var x = 0; x < t.Width; x++)
                {
                    if (t.GetCell(x, 0) == BuildingInteriorCellKind.Door)
                    {
                        doorOnSouth = true;
                        break;
                    }
                }

                Assert.IsTrue(doorOnSouth, t.DisplayName + " door not on local -Z edge");
            }
        }

        [Test]
        public void AssignTemplates_SameSeed_IsDeterministicAndCoversManyBuildings()
        {
            var profile = ScriptableObject.CreateInstance<TownProfile>();
            const int seed = 110201;
            var a = Generate(profile, seed);
            var b = Generate(profile, seed);

            var templatedA = 0;
            var templatedB = 0;
            Assert.AreEqual(a.Buildings.Count, b.Buildings.Count);
            for (var i = 0; i < a.Buildings.Count; i++)
            {
                Assert.AreEqual(a.Buildings[i].TemplateId, b.Buildings[i].TemplateId);
                Assert.AreEqual(a.Buildings[i].TemplateYawDeg, b.Buildings[i].TemplateYawDeg, 1e-3f);
                if (a.Buildings[i].HasTemplate)
                {
                    templatedA++;
                }

                if (b.Buildings[i].HasTemplate)
                {
                    templatedB++;
                }
            }

            Assert.AreEqual(templatedA, templatedB);
            Assert.Greater(templatedA, a.Buildings.Count / 5);
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
            Stage_AssignTemplates.Run(model, profile, ws.Fork("templates"));
            Stage_OpenSpaces.Run(model, profile, ws.Fork("openSpaces"));
            Stage_ConnectivityAndScale.Run(model, profile, ws.Fork("connectivity"), log);
            return model;
        }
    }
}

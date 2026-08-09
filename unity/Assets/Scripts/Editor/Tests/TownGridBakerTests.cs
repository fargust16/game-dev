using System.Collections.Generic;
using GameDev.World;
using GameDev.World.Town;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Town.Tests
{
    public sealed class TownGridBakerTests
    {
        [Test]
        public void Bake_Seed110201_HasWalkableSpawnAndPortalDirection()
        {
            var profile = ScriptableObject.CreateInstance<TownProfile>();
            var model = Generate(profile, 110201);

            var host = new GameObject("TownGridBakerTestHost");
            try
            {
                var bake = TownGridBaker.Bake(host.transform, model, playableRadiusMetres: 90f);
                Assert.IsNotNull(bake.Grid);
                Assert.Greater(bake.WalkableCount, 0, "Expected walkable cells inside playable radius.");
                Assert.IsTrue(bake.SpawnWalkable, "Spawn must be walkable.");
                Assert.IsTrue(bake.Grid.IsWalkable(bake.Spawn));
                Assert.Greater(bake.RoadCellCount, 0);
                Assert.IsTrue(
                    bake.HasPortalDirectionWalkable,
                    "Expected walkable cells toward at least one portal inside radius.");
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void Bake_PaintsInteriorDoorsAsWalkable()
        {
            var profile = ScriptableObject.CreateInstance<TownProfile>();
            var model = Generate(profile, 110201);
            var host = new GameObject("TownGridBakerDoorHost");
            try
            {
                var bake = TownGridBaker.Bake(host.transform, model, playableRadiusMetres: 90f);
                Assert.Greater(bake.InteriorCellCount, 0, "Expected templated interiors in radius.");
                Assert.Greater(bake.DoorCellCount, 0, "Expected door cells painted.");
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(profile);
            }
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

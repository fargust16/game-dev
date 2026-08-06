using System.Collections.Generic;
using GameDev.World;
using GameDev.World.Town;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Town.Tests
{
    public sealed class BuildingSocketTests
    {
        [Test]
        public void InferRole_Tavern_FirstNpcIsKeeper_FirstPropIsCounter()
        {
            Assert.AreEqual(
                BuildingSocketRole.Keeper,
                BuildingTemplateSocketUtil.InferRole(
                    BuildingTemplateId.Tavern, BuildingInteriorCellKind.NpcSocket, 0));
            Assert.AreEqual(
                BuildingSocketRole.Counter,
                BuildingTemplateSocketUtil.InferRole(
                    BuildingTemplateId.Tavern, BuildingInteriorCellKind.PropSocket, 0));
            Assert.AreEqual(
                BuildingSocketRole.Seating,
                BuildingTemplateSocketUtil.InferRole(
                    BuildingTemplateId.Tavern, BuildingInteriorCellKind.PropSocket, 1));
        }

        [Test]
        public void ExplicitSockets_OverrideCellScan()
        {
            var template = BuildingTemplateCatalog.FromAscii(
                BuildingTemplateId.Cottage,
                "Cottage Explicit",
                new[]
                {
                    "WWWW",
                    "WFFW",
                    "WPNW",
                    "WFFW",
                    "WFFW",
                    "WWDW"
                },
                new[] { BuildingKind.Residential },
                null);

            template.ExplicitSockets = new List<TemplateSocketDef>
            {
                new TemplateSocketDef
                {
                    LocalX = 1,
                    LocalY = 1,
                    CellKind = BuildingInteriorCellKind.NpcSocket,
                    RoleOverride = BuildingSocketRole.Scholar
                }
            };

            var defs = new List<TemplateSocketDef>();
            BuildingTemplateSocketUtil.CollectTemplateSockets(template, defs);
            Assert.AreEqual(1, defs.Count);
            Assert.AreEqual(BuildingSocketRole.Scholar, defs[0].RoleOverride);
        }

        [Test]
        public void AssignTemplates_PlacesDeterministicSockets_WithRolesAndQuery()
        {
            var profile = ScriptableObject.CreateInstance<TownProfile>();
            const int seed = 110201;
            var a = Generate(profile, seed);
            var b = Generate(profile, seed);

            var socketsA = 0;
            var socketsB = 0;
            for (var i = 0; i < a.Buildings.Count; i++)
            {
                var sa = a.Buildings[i].Sockets;
                var sb = b.Buildings[i].Sockets;
                var ca = sa != null ? sa.Count : 0;
                var cb = sb != null ? sb.Count : 0;
                Assert.AreEqual(ca, cb);
                socketsA += ca;
                socketsB += cb;
                for (var si = 0; si < ca; si++)
                {
                    Assert.AreEqual(sa[si].Role, sb[si].Role);
                    Assert.AreEqual(sa[si].WorldPos.x, sb[si].WorldPos.x, 1e-3f);
                    Assert.AreEqual(sa[si].WorldPos.y, sb[si].WorldPos.y, 1e-3f);
                }
            }

            Assert.AreEqual(socketsA, socketsB);
            Assert.Greater(socketsA, 0);
            Assert.Greater(TownSocketQuery.CountRole(a, BuildingSocketRole.Keeper), 0);

            var keepers = new List<TownSocketRef>();
            TownSocketQuery.CollectByRole(a, BuildingSocketRole.Keeper, keepers);
            Assert.AreEqual(TownSocketQuery.CountRole(a, BuildingSocketRole.Keeper), keepers.Count);

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

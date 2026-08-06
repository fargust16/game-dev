using System.Collections.Generic;
using GameDev.World.Town;
using GameDev.World.Town.Geometry;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Town.Tests
{
    public sealed class BuildingVolumeTests
    {
        [Test]
        public void Contains_InsideFootprintAndHeight_ReturnsTrue()
        {
            var building = new Building
            {
                Footprint = TownGraphUtil.ToPolygon(new List<Vector2>
                {
                    new Vector2(0, 0),
                    new Vector2(6, 0),
                    new Vector2(6, 8),
                    new Vector2(0, 8)
                }),
                Floors = 2f
            };

            var volume = BuildingVolume.FromBuilding(0, building);
            Assert.IsTrue(volume.IsValid);
            Assert.IsTrue(volume.Contains(new Vector3(3f, 1f, 4f)));
            Assert.IsTrue(volume.ContainsXZ(new Vector2(3f, 4f)));
        }

        [Test]
        public void Contains_OutsideFootprintOrAboveRoof_ReturnsFalse()
        {
            var building = new Building
            {
                Footprint = TownGraphUtil.ToPolygon(new List<Vector2>
                {
                    new Vector2(0, 0),
                    new Vector2(4, 0),
                    new Vector2(4, 4),
                    new Vector2(0, 4)
                }),
                Floors = 1f
            };

            var volume = BuildingVolume.FromBuilding(3, building);
            Assert.IsFalse(volume.Contains(new Vector3(10f, 1f, 10f)));
            Assert.IsFalse(volume.Contains(new Vector3(2f, 10f, 2f)));
            Assert.AreEqual(3, volume.BuildingIndex);
            Assert.AreEqual(BuildingVolume.DefaultStoreyMetres, volume.MaxY, 1e-3f);
        }

        [Test]
        public void InferRoleStillStable_AfterD2Files_DoNotBreakD1Sockets()
        {
            // Guardrail: D2 must not regress D1 role table.
            Assert.AreEqual(
                BuildingSocketRole.Keeper,
                BuildingTemplateSocketUtil.InferRole(
                    BuildingTemplateId.Tavern, BuildingInteriorCellKind.NpcSocket, 0));
        }
    }
}

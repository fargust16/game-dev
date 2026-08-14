using GameDev.World.Terrain;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Terrain.Tests
{
    public sealed class TerrainBiomePaletteTests
    {
        [Test]
        public void Fields_IsWarmNotNeon()
        {
            Assert.Less(TerrainBiomePalette.Fields.RemapMax.y, 0.7f);
            Assert.Greater(TerrainBiomePalette.Fields.RemapMax.x, 0.4f);
        }

        [Test]
        public void Forest_HasCoolerShadowThanFields()
        {
            Assert.Less(TerrainBiomePalette.Forest.RemapMin.y, TerrainBiomePalette.Fields.RemapMin.y);
            Assert.Greater(TerrainBiomePalette.Forest.RemapMax.y, TerrainBiomePalette.Forest.RemapMax.x);
        }

        [Test]
        public void Swamp_HasBlueGreyBias()
        {
            Assert.GreaterOrEqual(
                TerrainBiomePalette.Swamp.RemapMax.z,
                TerrainBiomePalette.Swamp.RemapMax.x);
        }

        [Test]
        public void RoadCenter_IsLighterThanSwampDirt()
        {
            Assert.Greater(TerrainBiomePalette.RoadCenter.RemapMax.x, TerrainBiomePalette.Swamp.RemapMax.x);
        }

        [Test]
        public void Apply_SetsTileAndRemap()
        {
            var layer = new TerrainLayer();
            TerrainBiomePalette.Apply(layer, TerrainBiomePalette.Forest, 8f);
            Assert.AreEqual(8f, layer.tileSize.x, 1e-4f);
            Assert.AreEqual(TerrainBiomePalette.Forest.RemapMax.y, layer.diffuseRemapMax.y, 1e-4f);
        }
    }
}

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
            Assert.Less(TerrainBiomePalette.Fields.RemapMax.y, 0.85f);
            Assert.Greater(TerrainBiomePalette.Fields.RemapMax.x, 0.4f);
            Assert.Greater(TerrainBiomePalette.Fields.RemapMax.y, TerrainBiomePalette.Forest.RemapMax.y);
        }

        [Test]
        public void Forest_HasCoolerShadowThanFields()
        {
            Assert.Less(TerrainBiomePalette.Forest.RemapMin.y, TerrainBiomePalette.Fields.RemapMin.y);
            Assert.Greater(TerrainBiomePalette.Forest.RemapMax.y, TerrainBiomePalette.Forest.RemapMax.x);
        }

        [Test]
        public void Swamp_IsMurkyDesaturated()
        {
            var max = TerrainBiomePalette.Swamp.RemapMax;
            Assert.Less(max.y, 0.45f);
            Assert.Greater(max.y, max.x);
        }

        [Test]
        public void RoadCenter_IsLighterThanSwampDirt()
        {
            Assert.Greater(TerrainBiomePalette.RoadCenter.RemapMax.x, TerrainBiomePalette.Swamp.RemapMax.x);
        }

        [Test]
        public void AlbedoTiles_AreWithinMemoBand()
        {
            Assert.GreaterOrEqual(TerrainBiomePalette.AlbedoTileMeters, 8f);
            Assert.LessOrEqual(TerrainBiomePalette.SnowTileMeters, 16f);
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

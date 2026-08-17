using GameDev.EditorTools;
using GameDev.World.Roads;
using GameDev.World.Terrain;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Terrain.Tests
{
    public sealed class OverworldVisualDirectionTests
    {
        [Test]
        public void TerrainShaderName_IsUrpTerrainLit()
        {
            Assert.AreEqual(
                "Universal Render Pipeline/Terrain/Lit",
                OverworldTerrainMenu.TerrainShaderName);
            Assert.AreEqual(
                "GameDev/Overworld/Terrain",
                OverworldTerrainMenu.DeprecatedOverworldTerrainShaderName);
        }

        [Test]
        public void SoftRim_AtLeastTwoPointFiveAlphamapTexels()
        {
            const float mPerPx = 1400f / 512f;
            var rim = RoadSurfaceStyle.SoftRimMeters(mPerPx);
            Assert.GreaterOrEqual(rim, mPerPx * RoadSurfaceStyle.MinSoftRimAlphamapTexels - 1e-4f);
            Assert.GreaterOrEqual(rim, RoadSurfaceStyle.ClampedTransitionMeters);
            Assert.LessOrEqual(rim, RoadSurfaceStyle.TransitionMaxMeters);
        }

        [Test]
        public void SettlementDust_SoftRect_FadesOutside()
        {
            var core = GameDev.World.Settlement.SettlementDustPaint.SoftRectCoverage(
                5f, 5f, 20f, 20f, 0.8f);
            var edge = GameDev.World.Settlement.SettlementDustPaint.SoftRectCoverage(
                21f, 10f, 20f, 20f, 0.8f);
            var far = GameDev.World.Settlement.SettlementDustPaint.SoftRectCoverage(
                40f, 10f, 20f, 20f, 0.8f);
            Assert.Greater(core, 0.5f);
            Assert.Greater(core, edge);
            Assert.AreEqual(0f, far, 1e-5f);
        }

    }
}

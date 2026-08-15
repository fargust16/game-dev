using GameDev.EditorTools;
using GameDev.World.Roads;
using GameDev.World.Terrain;
using GameDev.World.Vegetation;
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
        public void GrassHeights_MatchVisualDirection()
        {
            Assert.AreEqual(0.3f, GrassDistribution.ShortHeightMeters, 1e-4f);
            Assert.AreEqual(1.5f, GrassDistribution.TallHeightMeters, 1e-4f);
            Assert.Less(GrassDistribution.ShortHeightMeters, GrassDistribution.TallHeightMeters);
        }

        [Test]
        public void GrassLandDensityFloor_IsAtLeastPointThree()
        {
            Assert.GreaterOrEqual(GrassMask.LandDensityMin, 0.3f);
            Assert.LessOrEqual(GrassMask.LandDensityMax, 1f);
        }

        [Test]
        public void GrassClearCore_MatchesPrimaryHalfWidth()
        {
            Assert.AreEqual(
                GameDev.World.WorldScale.RoadPrimaryWidthMeters * 0.5f,
                GrassMask.RoadClearCoreMeters,
                1e-4f);
        }

        [Test]
        public void RoadLawn_ZeroOnPackedBed()
        {
            Assert.AreEqual(0f, GrassMask.RoadLawnMinAtCenter, 1e-5f);
            Assert.AreEqual(
                RoadSurfaceStyle.TransitionMeters,
                GrassMask.RoadEdgeFalloffMeters,
                1e-4f);
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
        public void GrassBakeQueue_IsTimeSliced()
        {
            Assert.AreEqual(1, GrassField.MaxBakesPerFrame);
            Assert.Greater(GrassField.WarmAheadMeters, 0f);
        }

        [Test]
        public void AlbedoTiles_AreWithinEightToSixteen()
        {
            Assert.GreaterOrEqual(TerrainBiomePalette.AlbedoTileMeters, 8f);
            Assert.LessOrEqual(TerrainBiomePalette.SnowTileMeters, 16f);
            Assert.LessOrEqual(TerrainBiomePalette.AlbedoTileMeters, TerrainBiomePalette.SnowTileMeters);
        }

        [Test]
        public void HeightFactor_FadesTowardPeaks()
        {
            var low = GrassMask.HeightFactor(20f, 220f);
            var high = GrassMask.HeightFactor(200f, 220f);
            Assert.Greater(low, 0.9f);
            Assert.Less(high, 0.5f);
        }

        [Test]
        public void LakeWater_ShallowIsBrighterAndMoreCyanThanDeep()
        {
            var shallow = new Color(0.22f, 0.48f, 0.46f);
            var deep = new Color(0.06f, 0.16f, 0.22f);
            var shallowLum = 0.2126f * shallow.r + 0.7152f * shallow.g + 0.0722f * shallow.b;
            var deepLum = 0.2126f * deep.r + 0.7152f * deep.g + 0.0722f * deep.b;
            Assert.Greater(shallowLum, deepLum);
            Assert.Greater(shallow.g + shallow.b, shallow.r * 2f);
        }

        [Test]
        public void GrassTint_MatchesTerrainPaletteMid()
        {
            var meadow = GrassTintMask.TintForBiome(BiomeId.Meadow);
            var fieldsMidY = (TerrainBiomePalette.Fields.RemapMin.y + TerrainBiomePalette.Fields.RemapMax.y) * 0.5f;
            Assert.AreEqual(fieldsMidY, meadow.g, 1e-4f);
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

        [Test]
        public void GrassTint_MeadowIsWarmerThanForest()
        {
            var meadow = GrassTintMask.TintForBiome(BiomeId.Meadow);
            var forest = GrassTintMask.TintForBiome(BiomeId.Forest);
            Assert.Greater(meadow.r + meadow.g, forest.r + forest.g);
        }

        [Test]
        public void SoftBiomeBiases_AreContinuousAcrossForestMeadowBand()
        {
            var h = BiomeClassifier.LowerGreenMaxHeight;
            VegetationEcology.SoftBiomeBiases(h - 0.02f, 0.05f, 0.3f, out var fA, out _, out var gA);
            VegetationEcology.SoftBiomeBiases(h + 0.02f, 0.05f, 0.3f, out var fB, out _, out var gB);
            Assert.Less(Mathf.Abs(fA - fB), 0.55f);
            Assert.Less(Mathf.Abs(gA - gB), 0.55f);
            Assert.Greater(gA, 0.4f);
            Assert.Greater(gB, 0.4f);
        }

        [Test]
        public void GrassTintSoft_BlendsAcrossBand()
        {
            var h = BiomeClassifier.LowerGreenMaxHeight;
            var a = GrassTintMask.TintSoft(h - 0.02f, 0.05f, 0.3f);
            var b = GrassTintMask.TintSoft(h + 0.02f, 0.05f, 0.3f);
            var hardA = GrassTintMask.TintForBiome(BiomeClassifier.Classify(h - 0.02f, 0.05f, 0.3f));
            var hardB = GrassTintMask.TintForBiome(BiomeClassifier.Classify(h + 0.02f, 0.05f, 0.3f));
            var softDelta = Mathf.Abs(a.g - b.g);
            var hardDelta = Mathf.Abs(hardA.g - hardB.g);
            Assert.LessOrEqual(softDelta, hardDelta + 1e-4f);
        }
    }
}

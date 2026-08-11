using GameDev.World.Terrain;
using NUnit.Framework;

namespace GameDev.World.Terrain.Tests
{
    public sealed class BiomeClassifierTests
    {
        // Flat (slope 0) and dry (moisture 0) so these match the height-only bands exactly -
        // slope/moisture-specific behaviour gets its own tests below.
        private const float Flat = 0f;
        private const float Dry = 0f;

        [Test]
        public void Classify_BelowSwampMax_DryIsMeadow_WetIsSwamp()
        {
            Assert.AreEqual(BiomeId.Meadow, BiomeClassifier.Classify(0f, Flat, Dry));
            Assert.AreEqual(BiomeId.Swamp, BiomeClassifier.Classify(0f, Flat, 1f));
            Assert.AreEqual(BiomeId.Meadow, BiomeClassifier.Classify(BiomeClassifier.SwampMaxHeight - 0.01f, Flat, Dry));
        }

        [Test]
        public void Classify_BelowLowerGreenMax_DryIsMeadow_WetIsForest()
        {
            Assert.AreEqual(BiomeId.Meadow, BiomeClassifier.Classify(BiomeClassifier.SwampMaxHeight, Flat, Dry));
            Assert.AreEqual(BiomeId.Forest, BiomeClassifier.Classify(BiomeClassifier.SwampMaxHeight, Flat, 1f));
            Assert.AreEqual(BiomeId.Meadow, BiomeClassifier.Classify(BiomeClassifier.LowerGreenMaxHeight - 0.01f, Flat, Dry));
        }

        [Test]
        public void Classify_BelowGreenMax_IsForest_RegardlessOfMoisture()
        {
            Assert.AreEqual(BiomeId.Forest, BiomeClassifier.Classify(BiomeClassifier.LowerGreenMaxHeight, Flat, Dry));
            Assert.AreEqual(BiomeId.Forest, BiomeClassifier.Classify(BiomeClassifier.GreenMaxHeight - 0.01f, Flat, 1f));
        }

        [Test]
        public void Classify_BelowHighlandMax_IsHighland_UnlessSteep()
        {
            Assert.AreEqual(BiomeId.Highland, BiomeClassifier.Classify(BiomeClassifier.GreenMaxHeight, Flat, Dry));
            Assert.AreEqual(BiomeId.Highland, BiomeClassifier.Classify(BiomeClassifier.HighlandMaxHeight - 0.01f, Flat, Dry));
        }

        [Test]
        public void Classify_AtOrAboveHighlandMax_IsMountain()
        {
            Assert.AreEqual(BiomeId.Mountain, BiomeClassifier.Classify(BiomeClassifier.HighlandMaxHeight, Flat, Dry));
            Assert.AreEqual(BiomeId.Mountain, BiomeClassifier.Classify(1f, Flat, Dry));
        }

        [Test]
        public void Classify_SteepSlopeInHighlandBand_IsMountain()
        {
            Assert.AreEqual(BiomeId.Mountain,
                BiomeClassifier.Classify(BiomeClassifier.GreenMaxHeight, BiomeClassifier.SteepSlope, Dry),
                "a steep valley wall in the highland band should read as Mountain");
        }

        [Test]
        public void Classify_SteepSlopeBelowGreenMax_DoesNotForceMountain()
        {
            Assert.AreEqual(BiomeId.Forest,
                BiomeClassifier.Classify(BiomeClassifier.GreenMaxHeight - 0.01f, BiomeClassifier.SteepSlope, Dry),
                "a carved lake pit's steep rim in flat lowlands should read as shoreline, not Mountain");
        }

        [Test]
        public void Classify_GentleSlopeInHighlandBand_StaysHighland()
        {
            Assert.AreEqual(BiomeId.Highland,
                BiomeClassifier.Classify(BiomeClassifier.GreenMaxHeight, BiomeClassifier.SteepSlope - 0.01f, Dry));
        }

        [Test]
        public void Classify_ClampsOutOfRangeInput()
        {
            Assert.AreEqual(BiomeId.Meadow, BiomeClassifier.Classify(-5f, Flat, Dry));
            Assert.AreEqual(BiomeId.Mountain, BiomeClassifier.Classify(5f, Flat, Dry));
            Assert.AreEqual(BiomeId.Mountain, BiomeClassifier.Classify(BiomeClassifier.GreenMaxHeight, 5f, Dry));
            Assert.AreEqual(BiomeId.Swamp, BiomeClassifier.Classify(0f, Flat, 5f));
        }

        [Test]
        public void Classify_GreenBiomes_CoverAtLeastHalfTheMapArea()
        {
            // The actual area guarantee lives in rank/percentile space (RankGreenMaxHeight etc.),
            // not in the compressed elevation values Classify() checks against - GreenMaxHeight and
            // SwampMaxHeight are deliberately close together in elevation (that's the whole point
            // of PlainsElevationCap, flat buildable ground), so this must assert against the rank
            // constants, not the elevation ones. Meadow ∪ Forest spans [0, RankGreenMaxHeight) of
            // area minus whatever the wet/dry splits hand to Swamp - even in the worst case
            // (everywhere wet, maximising Swamp), Green is still
            // RankGreenMaxHeight - RankSwampMaxHeight of the map's area, which must stay >= 0.5.
            Assert.GreaterOrEqual(BiomeClassifier.RankGreenMaxHeight - BiomeClassifier.RankSwampMaxHeight, 0.5f);
        }

        [Test]
        public void ElevationThresholds_StayMonotonicallyOrdered()
        {
            // Sanity check on the derived-constant arithmetic: whatever PlainsElevationCap and the
            // Rank* percentiles are set to, the resulting elevation thresholds must still come out
            // in ascending order, or Classify()'s band logic silently breaks (e.g. a "higher"
            // rank band mapping to a lower elevation value than the band below it).
            Assert.Less(BiomeClassifier.SwampMaxHeight, BiomeClassifier.LowerGreenMaxHeight);
            Assert.Less(BiomeClassifier.LowerGreenMaxHeight, BiomeClassifier.GreenMaxHeight);
            Assert.Less(BiomeClassifier.GreenMaxHeight, BiomeClassifier.HighlandMaxHeight);
            Assert.Less(BiomeClassifier.HighlandMaxHeight, 1f);
        }
    }
}

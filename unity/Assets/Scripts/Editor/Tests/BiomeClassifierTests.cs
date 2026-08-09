using GameDev.World.Terrain;
using NUnit.Framework;

namespace GameDev.World.Terrain.Tests
{
    public sealed class BiomeClassifierTests
    {
        [Test]
        public void Classify_BelowSwampMax_IsSwamp()
        {
            Assert.AreEqual(BiomeId.Swamp, BiomeClassifier.Classify(0f));
            Assert.AreEqual(BiomeId.Swamp, BiomeClassifier.Classify(BiomeClassifier.SwampMaxHeight - 0.01f));
        }

        [Test]
        public void Classify_BelowMeadowMax_IsMeadow()
        {
            Assert.AreEqual(BiomeId.Meadow, BiomeClassifier.Classify(BiomeClassifier.SwampMaxHeight));
            Assert.AreEqual(BiomeId.Meadow, BiomeClassifier.Classify(BiomeClassifier.MeadowMaxHeight - 0.01f));
        }

        [Test]
        public void Classify_BelowForestMax_IsForest()
        {
            Assert.AreEqual(BiomeId.Forest, BiomeClassifier.Classify(BiomeClassifier.MeadowMaxHeight));
            Assert.AreEqual(BiomeId.Forest, BiomeClassifier.Classify(BiomeClassifier.ForestMaxHeight - 0.01f));
        }

        [Test]
        public void Classify_AtOrAboveForestMax_IsMountain()
        {
            Assert.AreEqual(BiomeId.Mountain, BiomeClassifier.Classify(BiomeClassifier.ForestMaxHeight));
            Assert.AreEqual(BiomeId.Mountain, BiomeClassifier.Classify(1f));
        }

        [Test]
        public void Classify_ClampsOutOfRangeInput()
        {
            Assert.AreEqual(BiomeId.Swamp, BiomeClassifier.Classify(-5f));
            Assert.AreEqual(BiomeId.Mountain, BiomeClassifier.Classify(5f));
        }
    }
}

using GameDev.EditorTools;
using GameDev.World;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Terrain.Tests
{
    public sealed class MountainBiomeBakeContractTests
    {
        private const float HalfExtent = 940f;

        [Test]
        public void HeightBudget_Is700()
        {
            Assert.AreEqual(700f, WorldScale.TerrainHeightBudgetMeters, 0.001f);
        }

        [Test]
        public void IslandSilhouette_IsIrregularAndInsideTheAuthoredExtent()
        {
            var anchors = OverworldLpbBake.BuildIslandSilhouette(
                HalfExtent, HalfExtent, OverworldTerrainMenu.DefaultSeed);
            Assert.AreEqual(OverworldLpbBake.IslandAnchorCount, anchors.Length);

            var radii = new float[anchors.Length];
            for (var i = 0; i < anchors.Length; i++)
            {
                Assert.AreEqual(0f, anchors[i].y, 0.01f);
                // The silhouette must stay inside whatever Set Biome Area On Spawn authored,
                // otherwise changing the area in the Inspector would stop controlling the size.
                Assert.LessOrEqual(Mathf.Abs(anchors[i].x), HalfExtent + 0.01f);
                Assert.LessOrEqual(Mathf.Abs(anchors[i].z), HalfExtent + 0.01f);
                radii[i] = new Vector2(anchors[i].x, anchors[i].z).magnitude;
            }

            Assert.Greater(
                Mathf.Max(radii) - Mathf.Min(radii),
                20f,
                "coast should vary with seed (not a perfect circle/square)");
        }

        [Test]
        public void IslandSilhouette_ScalesWithTheAuthoredExtent()
        {
            var small = OverworldLpbBake.BuildIslandSilhouette(400f, 400f, 110201);
            var large = OverworldLpbBake.BuildIslandSilhouette(800f, 800f, 110201);

            for (var i = 0; i < small.Length; i++)
            {
                Assert.AreEqual(small[i].x * 2f, large[i].x, 0.01f);
                Assert.AreEqual(small[i].z * 2f, large[i].z, 0.01f);
            }
        }

        [Test]
        public void IslandSilhouette_SameSeed_IsDeterministic()
        {
            var a = OverworldLpbBake.BuildIslandSilhouette(HalfExtent, HalfExtent, 110201);
            var b = OverworldLpbBake.BuildIslandSilhouette(HalfExtent, HalfExtent, 110201);
            Assert.AreEqual(a.Length, b.Length);
            for (var i = 0; i < a.Length; i++)
            {
                Assert.AreEqual(a[i].x, b[i].x, 1e-5f);
                Assert.AreEqual(a[i].z, b[i].z, 1e-5f);
            }
        }
    }
}

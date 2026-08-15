using GameDev.EditorTools;
using GameDev.World;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Terrain.Tests
{
    public sealed class MountainBiomeBakeContractTests
    {
        [Test]
        public void HeightBudget_Is700()
        {
            Assert.AreEqual(700f, WorldScale.TerrainHeightBudgetMeters, 0.001f);
        }

        [Test]
        public void NaturalIsland_LocalAnchors_AreIrregularAndInscribed()
        {
            var go = new GameObject("NaturalIslandAnchorTest");
            try
            {
                go.transform.localPosition = new Vector3(500f, 0f, 500f);
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = new Vector3(1.05f, 1f, 1.05f);
                var local = OverworldLpbBake.BuildSeededNaturalIslandLocalAnchors(
                    go.transform,
                    OverworldTerrainMenu.DefaultSeed);
                Assert.AreEqual(OverworldLpbBake.IslandAnchorCount, local.Length);

                var center = new Vector3(
                    WorldScale.WorldExtentXMeters * 0.5f,
                    0f,
                    WorldScale.WorldExtentZMeters * 0.5f);
                var radii = new float[local.Length];
                for (var i = 0; i < local.Length; i++)
                {
                    var world = go.transform.TransformPoint(local[i]);
                    Assert.AreEqual(0f, world.y, 0.01f);
                    Assert.Greater(world.x, 0f);
                    Assert.Less(world.x, WorldScale.WorldExtentXMeters);
                    Assert.Greater(world.z, 0f);
                    Assert.Less(world.z, WorldScale.WorldExtentZMeters);
                    radii[i] = Vector2.Distance(
                        new Vector2(world.x, world.z),
                        new Vector2(center.x, center.z));
                }

                var minR = Mathf.Min(radii);
                var maxR = Mathf.Max(radii);
                Assert.Greater(maxR - minR, 20f, "coast should vary with seed (not a perfect circle/square)");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void NaturalIsland_SameSeed_IsDeterministic()
        {
            var go = new GameObject("NaturalIslandDet");
            try
            {
                go.transform.localPosition = new Vector3(500f, 0f, 500f);
                go.transform.localScale = new Vector3(1.05f, 1f, 1.05f);
                var a = OverworldLpbBake.BuildSeededNaturalIslandLocalAnchors(go.transform, 110201);
                var b = OverworldLpbBake.BuildSeededNaturalIslandLocalAnchors(go.transform, 110201);
                Assert.AreEqual(a.Length, b.Length);
                for (var i = 0; i < a.Length; i++)
                {
                    Assert.AreEqual(a[i].x, b[i].x, 1e-5f);
                    Assert.AreEqual(a[i].z, b[i].z, 1e-5f);
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}

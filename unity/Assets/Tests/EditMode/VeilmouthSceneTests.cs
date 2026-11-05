using System.Linq;
using GameDev.Slice.EditorTools;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>S0.6: Veilmouth.unity holds the C1 map, ground over the whole area shaped from C1, and the sea under it.</summary>
    public sealed class VeilmouthSceneTests
    {
        private UnityEngine.SceneManagement.Scene _scene;

        [SetUp]
        public void SetUp()
        {
            _scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Additive);
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.CloseScene(_scene, true);
        }

        private T[] All<T>() where T : Component =>
            _scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<T>()).ToArray();

        [Test]
        public void TheGroundCoversTheAreaAboveTheSea()
        {
            var terrain = All<Terrain>().Single();
            var size = terrain.terrainData.size;
            Assert.AreEqual(VeilmouthScene.Extent, size.x, 0.01f);
            Assert.AreEqual(VeilmouthScene.Extent, size.z, 0.01f);
            Assert.AreEqual(-VeilmouthScene.Extent * 0.5f, terrain.transform.position.x, 0.01f);
            Assert.AreEqual(-VeilmouthScene.Extent * 0.5f, terrain.transform.position.z, 0.01f);

            var ground = terrain.SampleHeight(VeilmouthScene.Spawn) + terrain.transform.position.y;
            Assert.Greater(ground, VeilmouthScene.SeaLevel, "the grid reads every surface below Sea Level as water");
            Assert.AreEqual(VeilmouthScene.TerrainLevel, ground, 0.01f);
        }

        /// <summary>C1 §9: the Ridge 8 m up, the avenue's climb, the river and the sea dug below Sea Level, the hill.</summary>
        [TestCase(-100f, -86f, VeilmouthGround.Ridge, 0.05f)] // Ridge Street
        [TestCase(-100f, -171f, VeilmouthGround.Low, 0.05f)] // High Street
        [TestCase(0f, -103f, 29.9f, 0.1f)] // halfway up Crown Avenue
        [TestCase(204f, -121f, 29.9f, 0.3f)] // halfway down the Soot Stair
        [TestCase(300f, -100f, VeilmouthGround.RiverBed, 0.05f)]
        [TestCase(0f, -400f, VeilmouthGround.SeaBed, 0.5f)]
        [TestCase(420f, 140f, VeilmouthGround.HillTop, 4f)] // the ore hill's top
        public void TheGroundFollowsC1(float x, float z, float height, float tolerance)
        {
            var terrain = All<Terrain>().Single();
            var ground = terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;
            Assert.AreEqual(height, ground, tolerance);
        }

        [Test]
        public void TheAreaEdgeIsTooSteepToWalkAndTheRoadsLeaveThroughPasses()
        {
            var terrain = All<Terrain>().Single();
            float At(float x, float z) => terrain.SampleHeight(new Vector3(x, 0f, z));
            Assert.Greater(At(-498f, 100f) - At(-460f, 100f), 30f, "the west edge rises");
            Assert.Greater(Mathf.Atan((At(-480f, 100f) - At(-470f, 100f)) / 10f) * Mathf.Rad2Deg, 45f, "steeper than 45 degrees");
            Assert.Less(At(-498f, -172f) - At(-460f, -172f), 8f, "the West road leaves through a pass");
        }

        [Test]
        public void TheWaterLiesAtSeaLevelUnderTheGround()
        {
            var water = _scene.GetRootGameObjects().Single(go => go.name == "Water");
            Assert.AreEqual(VeilmouthScene.SeaLevel, water.transform.position.y, 0.001f);
            Assert.IsNull(water.GetComponent<Collider>());
            Assert.Less(water.transform.position.y, VeilmouthScene.TerrainLevel);
        }

        [Test]
        public void TheC1MapIsLaidOut()
        {
            var zones = All<LayoutZone>().Where(z => z.transform.parent != null && z.transform.parent.name == VeilmouthScene.MapName).ToArray();
            CollectionAssert.AreEquivalent(VeilmouthScene.ZoneNames, zones.Select(z => z.name));

            var zone = zones.ToDictionary(z => z.name);
            Assert.IsTrue(zone["Lower Veil"].Covers(VeilmouthScene.Spawn), "the player starts in Lower Veil");
            Assert.IsTrue(zone["City"].Covers(zone["Ridge"].transform.position));
            Assert.IsFalse(zone["City"].Covers(zone["River Veil"].transform.position), "the river runs east of the city");
            Assert.IsTrue(zone["Sea"].transform.position.z < zone["City"].transform.position.z, "the sea is south");
        }
    }
}

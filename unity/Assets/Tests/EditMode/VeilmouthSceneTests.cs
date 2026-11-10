using System.Linq;
using GameDev.Slice.EditorTools;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>S0.6, §28: Veilmouth.unity holds the C4 map, ground over the whole area shaped from C4, and the sea under it.</summary>
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

        /// <summary>C4 §28.5: the low rise under the Penny and the cathedral, the level town, the river and the sea dug below Sea Level.</summary>
        [TestCase(40f, 60f, VeilmouthGround.Low + VeilmouthGround.Rise, 0.05f)] // the Penny's statue
        [TestCase(40f, 178f, VeilmouthGround.Low + VeilmouthGround.Rise, 0.05f)] // the cathedral's west front
        [TestCase(-75f, 60f, VeilmouthGround.Low, 0.1f)] // Long Tongue by Creaking Gate
        [TestCase(-50f, -40f, VeilmouthGround.Low, 0.05f)] // Powder Embankment
        [TestCase(-102f, 200f, VeilmouthGround.RiverBed, 0.05f)] // the river's west leg
        [TestCase(0f, -68f, VeilmouthGround.RiverBed, 0.05f)] // the river's south leg
        [TestCase(200f, 50f, VeilmouthGround.SeaBed, 0.1f)] // the sea by the quay
        public void TheGroundFollowsC4(float x, float z, float height, float tolerance)
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
            Assert.Less(At(-498f, 60f) - At(-460f, 60f), 8f, "the West Road leaves through a pass");
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
        public void TheC4MapIsLaidOut()
        {
            Assert.AreEqual("C4 Map", VeilmouthScene.MapName);
            var zones = All<LayoutZone>().Where(z => z.transform.parent != null && z.transform.parent.name == VeilmouthScene.MapName).ToArray();
            CollectionAssert.AreEquivalent(VeilmouthScene.ZoneNames, zones.Select(z => z.name));

            var zone = zones.ToDictionary(z => z.name);
            Assert.IsTrue(zone["Lower Veil"].Covers(VeilmouthScene.Spawn), "the player starts in Lower Veil");
            Assert.IsTrue(zone["Lower Veil"].Covers(zone["Black Puddle"].transform.position), "the Black Puddle lies in Lower Veil");
            Assert.IsTrue(zone["City"].Covers(zone["The Penny"].transform.position));
            Assert.IsFalse(zone["City"].Covers(zone["River Veil"].transform.position), "the river runs west of the city");
            Assert.IsTrue(zone["The sea"].transform.position.x > zone["City"].transform.position.x, "the sea is east");
            Assert.IsTrue(zone["The Reek"].transform.position.z < zone["City"].transform.position.z, "the works lie south, over the river");
        }
    }
}

using System.Linq;
using GameDev.Slice.EditorTools;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>S0.6: Veilmouth.unity holds the C0 map, ground over the whole area, and the sea under it.</summary>
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

        [Test]
        public void TheWaterLiesAtSeaLevelUnderTheGround()
        {
            var water = _scene.GetRootGameObjects().Single(go => go.name == "Water");
            Assert.AreEqual(VeilmouthScene.SeaLevel, water.transform.position.y, 0.001f);
            Assert.IsNull(water.GetComponent<Collider>());
            Assert.Less(water.transform.position.y, VeilmouthScene.TerrainLevel);
        }

        [Test]
        public void TheC0MapIsLaidOut()
        {
            var zones = All<LayoutZone>().Where(z => z.transform.parent != null && z.transform.parent.name == "C0 Map").ToArray();
            CollectionAssert.AreEquivalent(VeilmouthScene.ZoneNames, zones.Select(z => z.name));

            var zone = zones.ToDictionary(z => z.name);
            Assert.IsTrue(zone["Lower Veil"].Covers(VeilmouthScene.Spawn), "the player starts in Lower Veil");
            Assert.IsTrue(zone["City"].Covers(zone["Ridge"].transform.position));
            Assert.IsFalse(zone["City"].Covers(zone["River Veil"].transform.position), "the river runs east of the city");
            Assert.IsTrue(zone["Sea"].transform.position.z < zone["City"].transform.position.z, "the sea is south");
        }
    }
}

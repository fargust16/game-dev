using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.EditorTools;
using GameDev.Slice.Kit;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    public sealed class KitLayoutCheckTests
    {
        private readonly List<GameObject> _made = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _made)
            {
                Object.DestroyImmediate(go);
            }

            _made.Clear();
        }

        private GameObject Place(GameObject block, float x, float y, float z, float yaw = 0f)
        {
            block.transform.SetPositionAndRotation(new Vector3(x, y, z), Quaternion.Euler(0f, yaw, 0f));
            _made.Add(block);
            return block;
        }

        private GameObject Crate(float x, float z) => Place(KitStandIns.Crate("Crate"), x, 0f, z);

        private List<KitFault> CheckAll()
        {
            return KitLayoutCheck.Check(_made.Select(go => go.GetComponent<KitBlock>()).ToList());
        }

        [Test]
        public void ACornerOfWallsPostsDoorAndStreetIsClean()
        {
            Place(KitStandIns.Street("Street", 4f), 0f, 0f, 0f);
            Place(KitStandIns.Street("Street", 4f), 0f, 0f, 4f);
            Place(KitStandIns.Post("Post", 3f), 0f, 0f, 0f);
            Place(KitStandIns.Linear("Wall", KitLayers.Structure, 4f, 3f, KitStandIns.WallThickness), 0f, 0f, 0f);
            Place(KitStandIns.Post("Post", 3f), 4f, 0f, 0f);
            Place(KitStandIns.Doorway("Door"), 4f, 0f, 0f, -90f); // runs along +Z from the corner
            Place(KitStandIns.Post("Post", 3f), 4f, 0f, 3f);
            Place(KitStandIns.Trunk("Tree", 4f), 1f, 0f, 2f);

            CollectionAssert.IsEmpty(CheckAll().Select(f => f.ToString()));
        }

        [TestCase(SliceTestScenes.MovementScenePath, 100)]
        [TestCase(SliceTestScenes.TavernScenePath, 20)]
        [TestCase(VeilmouthScene.ScenePath, 60)]
        [TestCase(VeilmouthScene.SootAndBellScenePath, 20)]
        public void ATestSceneIsClean(string path, int minimumBlocks)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var blocks = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<KitBlock>()).ToList();
                Assert.Greater(blocks.Count, minimumBlocks);
                CollectionAssert.IsEmpty(KitLayoutCheck.Check(blocks).Select(f => f.ToString()));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void TouchingCratesAreClean()
        {
            Crate(0f, 0f);
            Crate(1f, 0f);
            Crate(0f, 1f);
            CollectionAssert.IsEmpty(CheckAll());
        }

        [Test]
        public void OverlappingBlocksAreReported()
        {
            Crate(0f, 0f);
            Crate(0f, 0f);
            var faults = CheckAll();
            Assert.AreEqual(1, faults.Count);
            Assert.AreEqual(KitFaultKind.Overlap, faults[0].Kind);
        }

        [Test]
        public void CrossingWallsOverlap()
        {
            Place(KitStandIns.Linear("Wall", KitLayers.Structure, 4f, 3f, KitStandIns.WallThickness), 0f, 0f, 0f);
            Place(KitStandIns.Linear("Wall", KitLayers.Structure, 4f, 3f, KitStandIns.WallThickness), 2f, 0f, -2f, -90f);
            Assert.AreEqual(KitFaultKind.Overlap, CheckAll().Single().Kind);
        }

        [Test]
        public void BlocksOffTheGridAreReported()
        {
            Crate(0.5f, 0f);
            Place(KitStandIns.Crate("Turned"), 3f, 0f, 0f, 45f);
            var scaled = Crate(6f, 0f);
            scaled.transform.localScale = new Vector3(2f, 1f, 1f);
            Place(KitStandIns.Crate("Raised"), 9f, 0.05f, 0f);

            var faults = CheckAll();
            Assert.AreEqual(4, faults.Count);
            Assert.IsTrue(faults.All(f => f.Kind == KitFaultKind.OffGrid));
        }

        [Test]
        public void LayersAndCollidersAreChecked()
        {
            var wrongLayer = Crate(0f, 0f);
            wrongLayer.layer = 0;

            var hollow = new GameObject("Hollow", typeof(KitBlock)) { layer = LayerMask.NameToLayer(KitLayers.Prop) };
            Place(hollow, 3f, 0f, 0f);

            var solidDecoration = Crate(6f, 0f);
            solidDecoration.layer = LayerMask.NameToLayer(KitLayers.NoGrid);

            var kinds = CheckAll().Select(f => f.Kind).OrderBy(k => k).ToList();
            CollectionAssert.AreEqual(
                new[] { KitFaultKind.WrongLayer, KitFaultKind.MissingCollider, KitFaultKind.ColliderOnNoGrid },
                kinds);
        }
    }
}

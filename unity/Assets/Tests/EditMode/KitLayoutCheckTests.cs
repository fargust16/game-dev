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
        [TestCase(VeilmouthScene.ScenePath, 1000)]
        [TestCase(VeilmouthScene.SootAndBellScenePath, 20)]
        [TestCase(VeilmouthDressing.HarrowScenePath, 15)]
        [TestCase(VeilmouthDressing.FactionHouseScenePath, 15)]
        [TestCase(VeilmouthDressing.LockupScenePath, 15)]
        [TestCase(VeilmouthDressing.MissingRoomScenePath, 15)]
        [TestCase(VeilmouthDressing.CellarScenePath, 15)]
        [TestCase(VeilmouthDressing.ChapelScenePath, 15)]
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

        /// <summary>C1 §12: a street at 45° is a frame on a world grid node; its blocks keep the grid in its axes.</summary>
        private GameObject Frame(float x, float z, float yaw)
        {
            var frame = new GameObject("Frame", typeof(KitFrame));
            frame.transform.SetPositionAndRotation(new Vector3(x, 0f, z), Quaternion.Euler(0f, yaw, 0f));
            _made.Add(frame);
            return frame;
        }

        private GameObject InFrame(GameObject frame, GameObject block, float x, float z, float yaw = 0f)
        {
            block.transform.SetParent(frame.transform, false);
            block.transform.localPosition = new Vector3(x, 0f, z);
            block.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            _made.Add(block);
            return block;
        }

        private List<KitFault> CheckFramed()
        {
            return KitLayoutCheck.Check(_made.Select(go => go.GetComponent<KitBlock>()).Where(b => b != null).ToList());
        }

        [Test]
        public void TouchingBlocksOnAStreetAt45AreClean()
        {
            var frame = Frame(10f, 3f, 45f);
            InFrame(frame, KitStandIns.Crate("Crate"), 0f, 0f);
            InFrame(frame, KitStandIns.Crate("Crate"), 1f, 0f);
            InFrame(frame, KitStandIns.Crate("Crate"), 0f, 1f);
            InFrame(frame, KitStandIns.Linear("Wall", KitLayers.Structure, 4f, 3f, KitStandIns.WallThickness), 2f, 0f);
            InFrame(frame, KitStandIns.Linear("Wall", KitLayers.Structure, 4f, 3f, KitStandIns.WallThickness), 6f, 0f);
            CollectionAssert.IsEmpty(CheckFramed().Select(f => f.ToString()));
        }

        [Test]
        public void OverlapOnAStreetAt45IsReported()
        {
            var frame = Frame(10f, 3f, 45f);
            InFrame(frame, KitStandIns.Crate("Crate"), 0f, 0f);
            InFrame(frame, KitStandIns.Crate("Crate"), 0.5f, 0f);
            var faults = CheckFramed();
            Assert.IsTrue(faults.Any(f => f.Kind == KitFaultKind.Overlap));
        }

        [Test]
        public void AFrameOffAGridNodeOrAngleAndABlockOffItsGridAreReported()
        {
            InFrame(Frame(10.5f, 3f, 45f), KitStandIns.Crate("Crate"), 0f, 0f);
            InFrame(Frame(30f, 3f, 30f), KitStandIns.Crate("Crate"), 0f, 0f);
            InFrame(Frame(50f, 3f, 45f), KitStandIns.Crate("Crate"), 0.5f, 0f);
            var faults = CheckFramed();
            Assert.AreEqual(3, faults.Count, string.Join("\n", faults));
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

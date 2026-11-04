using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.EditorTools;
using GameDev.World.Portals;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>S0.4: every door in the test scenes leads to a door that leads back, and stands where it should.</summary>
    public sealed class PortalPairingTests
    {
        private static readonly string[] ScenePaths = { SliceTestScenes.MovementScenePath, SliceTestScenes.TavernScenePath };

        private readonly List<UnityEngine.SceneManagement.Scene> _opened = new List<UnityEngine.SceneManagement.Scene>();

        [TearDown]
        public void TearDown()
        {
            foreach (var scene in _opened)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            _opened.Clear();
        }

        private Dictionary<string, Portal[]> OpenAll()
        {
            var portals = new Dictionary<string, Portal[]>();
            foreach (var path in ScenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                _opened.Add(scene);
                portals[scene.name] = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Portal>()).ToArray();
            }

            return portals;
        }

        [Test]
        public void EveryDoorLeadsToADoorThatLeadsBack()
        {
            var portals = OpenAll();
            var inBuild = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
            CollectionAssert.IsSubsetOf(ScenePaths, inBuild, "a door loads its target by name from Build Settings");

            var count = 0;
            foreach (var pair in portals)
            {
                foreach (var portal in pair.Value)
                {
                    count++;
                    Assert.IsTrue(portal.Target.IsValid, $"{pair.Key}#{portal.Id} has no target");
                    Assert.IsTrue(portals.ContainsKey(portal.Target.SceneName), $"{pair.Key}#{portal.Id} leads to an unknown scene");
                    var paired = portals[portal.Target.SceneName].SingleOrDefault(p => p.Id == portal.Target.ArrivalPortalId);
                    Assert.IsNotNull(paired, $"{pair.Key}#{portal.Id} leads to a missing door {portal.Target}");
                    Assert.AreEqual(pair.Key, paired.Target.SceneName);
                    Assert.AreEqual(portal.Id, paired.Target.ArrivalPortalId);
                }
            }

            Assert.AreEqual(2, count);
        }

        [Test]
        public void TheStreetDoorPutsThePlayerOutsideFacingAway()
        {
            var street = OpenAll()[SliceTestScenes.SceneName(SliceTestScenes.MovementScenePath)]
                .Single(p => p.Id == SliceTestScenes.TavernStreetPortalId);

            // The door block at (4, 0, 8) along +X; the house is north of it.
            var pose = street.ArrivalPose;
            Assert.AreEqual(5.5f, pose.position.x, 0.01f);
            Assert.AreEqual(7f, pose.position.z, 0.01f);
            Assert.AreEqual(0f, pose.position.y, 0.01f);
            Assert.Less(Vector3.Angle(pose.forward, Vector3.back), 1f);
            Assert.IsTrue(street.InReach(pose.position));
        }
    }
}

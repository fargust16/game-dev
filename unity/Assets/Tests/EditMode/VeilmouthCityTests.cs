using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.EditorTools;
using GameDev.Slice.Kit;
using GameDev.World.Portals;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>S0.6, §4–§5, §12, §21: the city built from the plan C2 by <see cref="VeilmouthCity"/>.</summary>
    public sealed class VeilmouthCityTests
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
            _scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<T>(true)).ToArray();

        /// <summary>
        /// The owner's first remark of 2026-11-05: a lamp never stands in front of a door. The furniture rules (§4):
        /// nothing within 2 m of a door along its front, up to 8 m out from it.
        /// </summary>
        [Test]
        public void NoLampStandsInFrontOfADoor()
        {
            var lamps = All<KitBlock>().Where(b => b.name.StartsWith("Kit_Street_Lamp")).ToArray();
            Assert.Greater(lamps.Length, 60, "the streets are lit");

            var doors = new List<(Vector3 At, Vector3 Out, Vector3 Along)>();
            foreach (var marker in All<Transform>().Where(t => t.name == VictorianHouses.DoorMarker))
            {
                doors.Add((marker.position, marker.forward, marker.right));
            }

            foreach (var portal in All<Portal>())
            {
                var t = portal.transform;
                doors.Add((t.position + (t.right * 1.5f), t.forward, t.right));
            }

            Assert.Greater(doors.Count, 120); // C2 (city plan §21): 173 doors
            var faults = new List<string>();
            foreach (var lamp in lamps)
            {
                var centre = lamp.transform.position + (lamp.transform.rotation * new Vector3(0.5f, 0f, 0.5f));
                foreach (var (at, outward, along) in doors)
                {
                    var d = centre - at;
                    d.y = 0f;
                    var outside = Vector3.Dot(d, outward);
                    if (outside > 0f && outside < 8f && Mathf.Abs(Vector3.Dot(d, along)) < 2f)
                    {
                        faults.Add($"{lamp.name} at {centre} before the door at {at}");
                    }
                }
            }

            CollectionAssert.IsEmpty(faults);
        }

        /// <summary>§5.3: the seven buildings that open, each with its door on the street.</summary>
        [Test]
        public void TheSevenBuildingsOpenFromTheStreet()
        {
            // Every other door leads to the shared Dwelling scene (DwellingTests).
            var ids = All<Portal>().Where(p => p.Target.SceneName != SliceTestScenes.SceneName(VeilmouthDwellings.ScenePath))
                .Select(p => p.name).ToArray();
            foreach (var id in new[]
                     {
                         VeilmouthScene.SootAndBellStreetPortalId, "harrow-apothecary.street", "faction-house.street", "wardens-lockup.street",
                         "missing-room.street", "soot-stair-cellar.street", "old-chapel.street",
                     })
            {
                Assert.AreEqual(1, ids.Count(n => n == $"Portal - {id}"), id);
            }

            Assert.AreEqual(7, ids.Length);
        }

        /// <summary>§12: Tanners Slant stands at 45° under a kit frame on a world node, with houses along it.</summary>
        [Test]
        public void TheSlantStandsAt45DegreesUnderAFrame()
        {
            var frame = All<KitFrame>().Single();
            Assert.AreEqual(45f, frame.transform.eulerAngles.y, 0.01f);
            Assert.AreEqual(Mathf.Round(frame.transform.position.x), frame.transform.position.x, 0.001f);
            Assert.AreEqual(Mathf.Round(frame.transform.position.z), frame.transform.position.z, 0.001f);
            Assert.Greater(frame.GetComponentsInChildren<KitBlock>().Length, 4);
        }

        /// <summary>The street surfaces follow the canonical five layers, within the eight URP's terrain draws.</summary>
        [Test]
        public void TheStreetsAreAppendedWithinTheTerrainLayerBudget()
        {
            var layers = All<Terrain>().Single().terrainData.terrainLayers;
            Assert.LessOrEqual(layers.Length, 8);
            Assert.Greater(layers.Length, 5);
            StringAssert.StartsWith("Overworld_", layers[0].name);
            StringAssert.StartsWith("Overworld_", layers[4].name);
        }

        /// <summary>The C0 dressing and the C1 city are gone; the C2 city stands under its own root.</summary>
        [Test]
        public void TheC2CityReplacesTheEarlierLayouts()
        {
            var roots = _scene.GetRootGameObjects().Select(g => g.name).ToArray();
            CollectionAssert.Contains(roots, VeilmouthCity.RootName);
            CollectionAssert.DoesNotContain(roots, VeilmouthDressing.RootName);
            CollectionAssert.DoesNotContain(roots, "Veilmouth C1");
        }
    }
}

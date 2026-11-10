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
    /// <summary>S0.6, §4–§5, §12, §28: the city built from the plan C4 by <see cref="VeilmouthCity"/>, with its mystic layer.</summary>
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

            Assert.Greater(doors.Count, 120); // C4 (city plan §28.8): street doors plus the Dwelling and building portals
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

        /// <summary>§5.3: the seven buildings that open, each with its door on the street, and the Soot & Bell's yard door (§13).</summary>
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

            Assert.AreEqual(1, ids.Count(n => n == $"Portal - {VeilmouthScene.SootAndBellId}.yard"), "the Soot & Bell's back door to its yard");
            Assert.AreEqual(8, ids.Length);
        }

        /// <summary>§12, §28.8: Squint Lane stands at 45° under a kit frame on a world node, with houses along it.</summary>
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

        /// <summary>The C0 dressing and the C1 to C3 cities are gone; the C4 city stands under its own root (§28).</summary>
        [Test]
        public void TheC4CityReplacesTheEarlierLayouts()
        {
            var roots = _scene.GetRootGameObjects().Select(g => g.name).ToArray();
            Assert.AreEqual("Veilmouth C4", VeilmouthCity.RootName);
            CollectionAssert.Contains(roots, VeilmouthCity.RootName);
            CollectionAssert.DoesNotContain(roots, VeilmouthDressing.RootName);
            CollectionAssert.DoesNotContain(roots, "Veilmouth C1");
            CollectionAssert.DoesNotContain(roots, "Veilmouth C2");
            CollectionAssert.DoesNotContain(roots, "Veilmouth C3");
        }

        /// <summary>
        /// The mystic layer (WORLD_STYLE_GUIDE §6) is only seen: nothing under it has a collider or a kit block, all of
        /// it is on NoGrid, every street lamp has its gas light, the moon is down at the preview noon, and fog and wards stand.
        /// </summary>
        [Test]
        public void TheMysticLayerIsOnlySeen()
        {
            var root = _scene.GetRootGameObjects().Single(g => g.name == VeilmouthMystic.RootName);
            CollectionAssert.IsEmpty(root.GetComponentsInChildren<Collider>(true).Select(c => c.name));
            CollectionAssert.IsEmpty(root.GetComponentsInChildren<KitBlock>(true).Select(b => b.name));
            var noGrid = LayerMask.NameToLayer(KitLayers.NoGrid);
            CollectionAssert.IsEmpty(root.GetComponentsInChildren<Transform>(true).Where(t => t.gameObject.layer != noGrid).Select(t => t.name));

            var lamps = All<KitBlock>().Count(b => b.name.StartsWith("Kit_Street_Lamp"));
            Assert.AreEqual(lamps, root.GetComponentsInChildren<Light>(true).Count(l => l.name == "Gaslight"));
            Assert.Greater(root.GetComponentsInChildren<ParticleSystem>(true).Length, 8, "fog banks");
            // About one door in eight in Lower Veil and the Reek, whose doors number 44 in C4, 4 of them warded (city plan §28.10).
            Assert.GreaterOrEqual(root.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("Ward - door")), 3);
            Assert.Greater(root.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("Runes - ")), 8);
            Assert.IsFalse(root.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Crimson Moon").gameObject.activeSelf);
            Assert.IsNotNull(root.GetComponent<GameDev.Slice.Atmosphere.VeilmouthSky>());
        }
    }
}

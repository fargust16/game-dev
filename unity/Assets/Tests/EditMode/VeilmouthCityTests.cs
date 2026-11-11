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
    /// <summary>S0.6, §4–§5, §12, §28, §29: the city built from the plan C5 by <see cref="VeilmouthCity"/>, with its mystic layer.</summary>
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

            Assert.Greater(doors.Count, 120); // C5 (city plan §29.4): street doors plus the Dwelling and building portals
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

        /// <summary>
        /// §29.2: C5's blocks are rectangular for now, so Squint Lane and its 45° kit frame are dropped; every block's
        /// yard holds its wealth's dressing (§29.3).
        /// </summary>
        [Test]
        public void TheBlocksAreRectangularWithTheirYardsDressed()
        {
            CollectionAssert.IsEmpty(All<KitFrame>().Select(f => f.name));
            var yards = _scene.GetRootGameObjects().Single(g => g.name == VeilmouthCity.RootName).transform.Find("Yards");
            Assert.IsNotNull(yards, "the yards' group");
            Assert.Greater(yards.GetComponentsInChildren<KitBlock>().Count(b => b.name.Contains("Bench")), 8, "benches in the park yards");
            Assert.Greater(All<Transform>().Count(t => t.name == "Drunkard"), 4, "drunkards in the poor yards");
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

        /// <summary>The C0 dressing and the C1 to C4 cities are gone; the C5 city stands under its own root (§29).</summary>
        [Test]
        public void TheC5CityReplacesTheEarlierLayouts()
        {
            var roots = _scene.GetRootGameObjects().Select(g => g.name).ToArray();
            Assert.AreEqual("Veilmouth C5", VeilmouthCity.RootName);
            CollectionAssert.Contains(roots, VeilmouthCity.RootName);
            CollectionAssert.DoesNotContain(roots, VeilmouthDressing.RootName);
            CollectionAssert.DoesNotContain(roots, "Veilmouth C1");
            CollectionAssert.DoesNotContain(roots, "Veilmouth C2");
            CollectionAssert.DoesNotContain(roots, "Veilmouth C3");
            CollectionAssert.DoesNotContain(roots, "Veilmouth C4");
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
            // About one door in eight in Lower Veil and the Reek (city plan §28.10, on C5 by §29.4).
            Assert.GreaterOrEqual(root.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("Ward - door")), 3);
            Assert.Greater(root.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("Runes - ")), 8);
            Assert.IsFalse(root.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Crimson Moon").gameObject.activeSelf);
            Assert.IsNotNull(root.GetComponent<GameDev.Slice.Atmosphere.VeilmouthSky>());
        }

        /// <summary>
        /// Reference plan R3 step 2's fix: the facade shader measures its plinth dirt from the terrain under the city,
        /// whose streets lie at 26 m, not from world height 0, and the globals map a terrain's corners onto its heightmap's
        /// first and last texel centres.
        /// </summary>
        [Test]
        public void TheFacadesMeasureTheirPlinthFromTheGround()
        {
            var root = _scene.GetRootGameObjects().Single(g => g.name == VeilmouthMystic.RootName);
            var ground = root.GetComponent<GameDev.VictorianKit.VicGround>();
            Assert.IsNotNull(ground, "the mystic layer gives the facades their ground");
            Assert.IsNotNull(ground.Terrain);
            Assert.AreEqual(All<Terrain>().Single(), ground.Terrain);

            const int resolution = 513;
            var (rect, level) = GameDev.VictorianKit.VicGround.Globals(new Vector3(-500f, 2f, -500f), new Vector3(1000f, 80f, 1000f), resolution);
            float U(float x) => Mathf.Clamp01((x - rect.x) * rect.z) + level.w;
            Assert.AreEqual(0.5f / resolution, U(-500f), 1e-6f, "the west edge is the first texel's centre");
            Assert.AreEqual((resolution - 0.5f) / resolution, U(500f), 1e-6f, "the east edge is the last texel's centre");
            Assert.AreEqual(2f + 80f, level.x + ((32766f / 65535f) * level.y), 1e-3f, "a full texel is the terrain's full height");
            Assert.AreEqual(1f, level.z);
        }

        /// <summary>
        /// Reference plan R3 step 2's soot under the cornice: the eaves map holds, under each kit house's body, the top
        /// of its walls, which its body colliders reach, and 0 where no house stands.
        /// </summary>
        [Test]
        public void TheEavesMapHoldsEachHousesWallTop()
        {
            var ground = _scene.GetRootGameObjects().Single(g => g.name == VeilmouthMystic.RootName).GetComponent<GameDev.VictorianKit.VicGround>();
            var map = ground.Eaves;
            Assert.IsNotNull(map, "the eaves map is baked with the ground");
            var rect = ground.EavesRect;
            float At(Vector3 p)
            {
                var t = map.GetPixel(Mathf.FloorToInt((p.x - rect.x) * rect.z * map.width), Mathf.FloorToInt((p.z - rect.y) * rect.w * map.height)).r;
                return t > 0f ? GameDev.VictorianKit.VicGround.EavesBase + (t * 255f * GameDev.VictorianKit.VicGround.EavesStep) : 0f;
            }

            var bodies = All<KitBlock>().Where(b => b.transform.Find("Model") != null)
                .SelectMany(b => b.GetComponents<BoxCollider>()).Where(c => c.size.x > 3f && c.size.z > 3f).ToList();
            Assert.Greater(bodies.Count, 100, "the city's houses");
            foreach (var box in bodies.Take(40))
            {
                var centre = box.transform.TransformPoint(box.center);
                var top = box.transform.TransformPoint(box.center + (Vector3.up * (box.size.y / 2f))).y;
                Assert.GreaterOrEqual(At(centre), top - 0.13f, $"{box.name}'s wall top under its middle, to the map's step of 0.25 m");
            }

            Assert.AreEqual(0f, At(new Vector3(rect.x + 0.25f, 0f, rect.y + 0.25f)), "no house stands in the map's margin");
        }

        /// <summary>
        /// §38: the One-Eye's lantern burns with the gas lamps, its panes glowing at night and grimy by day, but it is no
        /// street lamp, so it adds nothing to the Darkness of 10.3 §3.
        /// </summary>
        [Test]
        public void TheOneEyeBurnsWithTheLampsButLightsNoStreet()
        {
            var sky = All<GameDev.Slice.Atmosphere.VeilmouthSky>().Single();
            var beacon = sky.Beacons.Single();
            Assert.AreEqual("The One-Eye's beacon", beacon.Light.name);
            Assert.IsNotNull(beacon.Renderer, "the lantern's panes swap their glass");
            Assert.IsFalse(sky.StreetLamps.Any(l => l.Light == beacon.Light), "the beacon is not a street lamp");

            sky.Apply(23 * 60);
            Assert.IsTrue(beacon.Light.enabled, "lit at night");
            Assert.AreEqual("Veilmouth_Gaslight", beacon.Renderer.sharedMaterials[beacon.Slot].name);
            sky.Apply(12 * 60);
            Assert.IsFalse(beacon.Light.enabled, "out by day");
            Assert.AreEqual("VIC_Glass_Grimy", beacon.Renderer.sharedMaterials[beacon.Slot].name);
        }
    }
}

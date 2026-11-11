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
        /// Reference plan R3 step 2: the sill map holds, behind every pane, the sills of its column, so the facade shader
        /// finds each window's own sill to run its stain from: the lowest in R, the second in G, the highest in B, and
        /// every sill above the second one storey over the one under it.
        /// </summary>
        [Test]
        public void TheSillMapHoldsEachWindowsSill()
        {
            var ground = _scene.GetRootGameObjects().Single(g => g.name == VeilmouthMystic.RootName).GetComponent<GameDev.VictorianKit.VicGround>();
            var map = ground.Sills;
            Assert.IsNotNull(map, "the sill map is baked with the ground");
            var rect = ground.SillsRect;
            const float step = GameDev.VictorianKit.VicGround.SillsStep, storey = GameDev.VictorianKit.VicGround.Storey;
            float Height(float t) => t > 0f ? GameDev.VictorianKit.VicGround.SillsBase + (Mathf.Round(t * 255f) * step) : 0f;
            var panes = All<GameDev.VictorianKit.VicWindowRooms>().SelectMany(r => Enumerable.Range(0, r.Count).Select(i => r[i].Pane)).Where(p => p != null && !p.name.Contains("_Door_")).ToList();
            Assert.Greater(panes.Count, 1000, "the city's windows");
            var missed = 0;
            foreach (var pane in panes.Where((_, i) => i % 25 == 0))
            {
                var (centre, _, normal, _, sill) = VeilmouthEaves.Footprint(pane);
                var p = centre + (normal * 0.5f);
                var texel = map.GetPixel(Mathf.FloorToInt((p.x - rect.x) * rect.z * map.width), Mathf.FloorToInt((p.y - rect.y) * rect.w * map.height));
                var (low, second, high) = (Height(texel.r), Height(texel.g), Height(texel.b));
                Assert.Greater(texel.a, 0.5f, $"{pane.name}'s column covers the texel behind its middle");
                // A stored height is the sill rounded up to the map's step, so it stands up to one step over the glass's bottom.
                bool Near(float y) => y >= sill - 0.01f && y <= sill + step + 0.01f;
                var found = Near(low) || (second > 0f && sill > second - 0.5f && sill < high + 0.5f &&
                                          Near(second + (Mathf.Round((sill - second) / storey) * storey)));
                if (!found)
                {
                    missed++;
                }
            }

            Assert.AreEqual(0, missed, "every sampled pane's sill is in its column");
            Assert.AreEqual(0f, map.GetPixel(0, 0).a, "no window stands in the map's margin");
        }

        /// <summary>
        /// §40: Last Turning Cemetery's stones stand in its graves' cells, which keep their kit roots, blocks and colliders,
        /// so the layout and the Combat Grid read them as before; three graves keep the pack's tomb.
        /// </summary>
        [Test]
        public void TheCemeterysStonesStandInItsGraves()
        {
            var city = _scene.GetRootGameObjects().Single(g => g.name == VeilmouthCity.RootName).transform;
            var graves = city.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Kit_Chapel_Grave")).ToList();
            Assert.Greater(graves.Count, 20, "the cemetery's graves in rows");
            Assert.IsTrue(graves.All(g => g.GetComponent<BoxCollider>() != null && g.GetComponent<KitBlock>() != null), "every grave keeps its collider and block");
            Assert.AreEqual(3, graves.Count(g => g.Find("Model").gameObject.activeSelf), "three graves keep the pack's tomb");
            var stones = city.Find("Last Turning's stones");
            Assert.IsNotNull(stones, "the cemetery's stones");
            var bounds = stones.GetComponentsInChildren<MeshRenderer>().Select(r => r.bounds).ToList();
            Assert.Greater(bounds.Count, 0, "the stones are drawn");
            var all = bounds.Aggregate((a, b) => { a.Encapsulate(b); return a; });
            foreach (var g in graves.Where(g => !g.Find("Model").gameObject.activeSelf))
            {
                var cell = g.TransformPoint(g.GetComponent<BoxCollider>().center);
                Assert.IsTrue(all.Contains(new Vector3(cell.x, all.center.y, cell.z)), $"{g.name} at {cell} has its stone");
            }

            Assert.LessOrEqual(all.max.y - graves.Max(g => g.position.y), 2.6f, "no stone stands over the wanted list's 2.5 m by much");
        }

        /// <summary>
        /// §37–§38: the One-Eye's lantern and Stone Grandad's burn with the gas lamps, and the clock's dials glow with
        /// them, in their own glass or stucco by day, but none is a street lamp, so they add nothing to the Darkness of
        /// 10.3 §3.
        /// </summary>
        [Test]
        public void TheBeaconsBurnWithTheLampsButLightNoStreet()
        {
            var sky = All<GameDev.Slice.Atmosphere.VeilmouthSky>().Single();
            CollectionAssert.AreEquivalent(new[] { "The One-Eye's beacon", "Stone Grandad's lantern" }, sky.Beacons.Where(b => b.Light != null).Select(b => b.Light.name));
            Assert.AreEqual("VIC_Stucco_Cream", sky.Beacons.Single(b => b.Light == null).Renderer.name, "the clock's dials glow with no light");
            foreach (var beacon in sky.Beacons)
            {
                Assert.IsNotNull(beacon.Renderer, $"{beacon.Renderer}'s panes swap their glass");
                Assert.IsNotNull(beacon.Unlit);
                Assert.IsFalse(beacon.Light != null && sky.StreetLamps.Any(l => l.Light == beacon.Light), $"{beacon.Light} is not a street lamp");
            }

            sky.Apply(23 * 60);
            foreach (var beacon in sky.Beacons)
            {
                Assert.IsTrue(beacon.Light == null || beacon.Light.enabled, $"{beacon.Light} lit at night");
                Assert.AreEqual("Veilmouth_Gaslight", beacon.Renderer.sharedMaterials[beacon.Slot].name);
            }

            sky.Apply(12 * 60);
            foreach (var beacon in sky.Beacons)
            {
                Assert.IsTrue(beacon.Light == null || !beacon.Light.enabled, $"{beacon.Light} out by day");
                Assert.AreEqual(beacon.Unlit.name, beacon.Renderer.sharedMaterials[beacon.Slot].name);
            }
        }
    }
}

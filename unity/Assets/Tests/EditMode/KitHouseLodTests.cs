using System;
using System.IO;
using System.Linq;
using GameDev.Slice.EditorTools;
using GameDev.VictorianKit;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// The smooth LOD of the kit houses (VEILMOUTH_REFERENCE_PLAN.md R2 steps 2–3): only the small details cross-fade
    /// out with distance, and the rooms behind the windows switch by distance behind glass that is already opaque.
    /// </summary>
    public sealed class KitHouseLodTests
    {
        private const string WindowShader = "GameDev/VictorianKit/Window";

        private static string[] Houses() =>
            Directory.GetFiles(VeilmouthKitHouses.Folder, "Kit_House_*.prefab").Select(p => p.Replace('\\', '/')).OrderBy(p => p, StringComparer.Ordinal).ToArray();

        [Test]
        public void EveryHouseFadesOnlyItsDetailsAndSwitchesItsRoomsByDistance()
        {
            var houses = Houses();
            Assert.Greater(houses.Length, 50, "the city's house types are baked");
            foreach (var path in houses)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var model = prefab.transform.Find("Model");
                Assert.IsNotNull(model, path);

                var lod = model.GetComponent<LODGroup>();
                Assert.IsNotNull(lod, $"{path}: the details' LOD group");
                Assert.AreEqual(LODFadeMode.CrossFade, lod.fadeMode, path);
                Assert.IsTrue(lod.animateCrossFading, path);
                var levels = lod.GetLODs();
                Assert.AreEqual(1, levels.Length, $"{path}: one level, the details");
                Assert.AreEqual(VeilmouthKitHouses.DetailScreen, levels[0].screenRelativeTransitionHeight, 1e-5f, path);
                foreach (var r in levels[0].renderers)
                {
                    var piece = r.transform;
                    while (piece.parent != model)
                    {
                        piece = piece.parent;
                    }

                    Assert.IsTrue(VeilmouthKitHouses.Details.Any(piece.name.StartsWith), $"{path}: {piece.name} fades out but is no detail");
                }

                var rooms = model.Find("Rooms");
                if (rooms == null)
                {
                    continue;
                }

                Assert.IsNull(rooms.GetComponent<LODGroup>(), $"{path}: the rooms are no longer culled by screen size");
                var switcher = rooms.GetComponent<VicRoomsSwitch>();
                Assert.IsNotNull(switcher, $"{path}: the rooms' distance switch");
                Assert.Greater(switcher.Box.size.x, 1f, path);
                Assert.Greater(switcher.Box.size.z, 1f, path);
            }
        }

        // Measured: how far any window of a house lies outside its rooms' box. The rooms come on at OnDistance from the
        // box, so the nearest glass is then at least OnDistance minus this, and must be opaque there (_RoomFar).
        [Test]
        public void TheRoomsComeOnBehindOpaqueGlass()
        {
            var roomFar = AssetDatabase.FindAssets("t:Material").Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Material>)
                .Where(m => m != null && m.shader != null && m.shader.name == WindowShader)
                .Select(m => m.GetFloat("_RoomFar")).ToList();
            Assert.IsNotEmpty(roomFar, "the window materials");

            var margin = 0f;
            var worst = "";
            foreach (var path in Houses())
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var rooms = prefab.transform.Find("Model/Rooms");
                var switcher = rooms != null ? rooms.GetComponent<VicRoomsSwitch>() : null;
                if (switcher == null)
                {
                    continue;
                }

                var toRooms = rooms.worldToLocalMatrix;
                var scale = rooms.lossyScale.x;
                foreach (var r in prefab.transform.Find("Model").GetComponentsInChildren<Renderer>(true))
                {
                    // The far LOD's glass is the pieces' glass with the same material, so at any distance it hides as much.
                    if (r.transform.IsChildOf(rooms) || r.transform.parent.name == VeilmouthFarLod.Name ||
                        !r.sharedMaterials.Any(m => m != null && m.shader != null && m.shader.name == WindowShader))
                    {
                        continue;
                    }

                    var local = r.localBounds;
                    var m = toRooms * r.transform.localToWorldMatrix;
                    for (var corner = 0; corner < 8; corner++)
                    {
                        var p = m.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1)));
                        var d = Mathf.Sqrt(switcher.Box.SqrDistance(p)) * scale;
                        if (d > margin)
                        {
                            (margin, worst) = (d, $"{Path.GetFileNameWithoutExtension(path)} {r.name}");
                        }
                    }
                }
            }

            Debug.Log($"[KitHouseLodTests] the farthest window lies {margin:0.00} m outside its rooms' box ({worst}); _RoomFar {roomFar.Max():0.#} m, rooms on at {VicRoomsSwitch.OnDistance} m.");
            Assert.LessOrEqual(roomFar.Max() + margin, VicRoomsSwitch.OnDistance, $"glass still clear when the rooms come on ({worst})");
        }

        [Test]
        public void TheSwitchKeepsItsStateBetweenTheTwoDistances()
        {
            var house = new GameObject("House");
            try
            {
                var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.transform.SetParent(house.transform, false);
                var rooms = house.AddComponent<VicRoomsSwitch>();
                rooms.Box = new Bounds(Vector3.zero, Vector3.one * 2f);
                var r = piece.GetComponent<Renderer>();

                bool At(float d)
                {
                    rooms.Decide(new Vector3(1f + d, 0f, 0f));
                    Assert.AreEqual(!rooms.Shown, r.forceRenderingOff);
                    return rooms.Shown;
                }

                Assert.IsTrue(At(VicRoomsSwitch.OnDistance - 1f), "near: on");
                Assert.IsTrue(At(VicRoomsSwitch.OffDistance - 1f), "between, coming from near: still on");
                Assert.IsFalse(At(VicRoomsSwitch.OffDistance + 1f), "far: off");
                Assert.IsFalse(At(VicRoomsSwitch.OnDistance + 1f), "between, coming from far: still off");
                Assert.IsTrue(At(VicRoomsSwitch.OnDistance - 0.5f), "near again: on");
            }
            finally
            {
                Object.DestroyImmediate(house);
            }
        }
    }
}

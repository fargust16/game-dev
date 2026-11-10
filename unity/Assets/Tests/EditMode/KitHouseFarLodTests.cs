using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameDev.Slice.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// The merged far LOD of each kit house type (VEILMOUTH_CITY_PLAN.md §29.10): beyond 50 m a house is drawn by one
    /// renderer for each shadow setting, holding every triangle of the pieces it replaces with the same materials, and
    /// each pane keeps its built room.
    /// </summary>
    public sealed class KitHouseFarLodTests
    {
        private static string[] Houses() =>
            Directory.GetFiles(VeilmouthKitHouses.Folder, "Kit_House_*.prefab").Select(p => p.Replace('\\', '/')).OrderBy(p => p, StringComparer.Ordinal).ToArray();

        [Test]
        public void EveryHouseSwitchesToItsFarLodAtFiftyMetresWithNoCrossFade()
        {
            var houses = Houses();
            Assert.Greater(houses.Length, 50, "the city's house types are baked");
            foreach (var path in houses)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var model = prefab.transform.Find("Model");
                var far = model.Find(VeilmouthFarLod.Name);
                var lod = prefab.GetComponent<LODGroup>();
                Assert.IsNotNull(far, $"{path}: the far LOD");
                Assert.IsNotNull(lod, $"{path}: the root's LOD group");
                Assert.AreEqual(LODFadeMode.None, lod.fadeMode, path);
                Assert.IsFalse(lod.animateCrossFading, path);

                var levels = lod.GetLODs();
                Assert.AreEqual(2, levels.Length, path);
                Assert.AreEqual(0f, levels[1].screenRelativeTransitionHeight, $"{path}: the far LOD is never culled");
                Assert.AreEqual(VeilmouthFarLod.FarDistance, VeilmouthFarLod.SwitchDistance(lod), 0.5f, path);
                CollectionAssert.AreEquivalent(VeilmouthFarLod.Sources(model).Select(s => (Renderer)s.Renderer), levels[0].renderers, $"{path}: LOD0 is the pieces merged");
                CollectionAssert.AreEquivalent(far.GetComponentsInChildren<MeshRenderer>(true), levels[1].renderers, path);
                foreach (var t in far.GetComponentsInChildren<Transform>(true))
                {
                    Assert.AreEqual((StaticEditorFlags)0, GameObjectUtility.GetStaticEditorFlags(t.gameObject), $"{path}: {t.name} is not static, so its mesh stays shared");
                }

                foreach (var r in levels[0].renderers)
                {
                    var piece = r.transform;
                    while (piece.parent != model)
                    {
                        piece = piece.parent;
                    }

                    Assert.IsFalse(VeilmouthKitHouses.IsDetail(piece), $"{path}: {piece.name} is a detail, which keeps its own fade");
                    Assert.AreNotEqual("Rooms", piece.name, path);
                }
            }
        }

        [Test]
        public void EveryFarLodHoldsEveryTriangleOfItsPiecesWithTheSameMaterials()
        {
            foreach (var path in Houses())
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var model = prefab.transform.Find("Model");
                AssertSame(path, VeilmouthFarLod.Sources(model), model.Find(VeilmouthFarLod.Name).GetComponentsInChildren<MeshRenderer>(true));
            }
        }

        [Test]
        public void EveryPaneKeepsItsRoomInTheFarLod()
        {
            var panes = 0;
            foreach (var path in Houses())
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var model = prefab.transform.Find("Model");
                var sources = VeilmouthFarLod.Sources(model);
                var expected = new Dictionary<uint, int>();
                foreach (var s in sources.Where(s => s.Room != 0u))
                {
                    expected[s.Room] = (expected.TryGetValue(s.Room, out var n) ? n : 0) + s.Mesh.vertexCount;
                    panes++;
                }

                var actual = new Dictionary<uint, int>();
                foreach (var r in model.Find(VeilmouthFarLod.Name).GetComponentsInChildren<MeshFilter>(true))
                {
                    foreach (var room in Rooms(r.sharedMesh).Where(room => room != 0u))
                    {
                        actual[room] = (actual.TryGetValue(room, out var n) ? n : 0) + 1;
                    }
                }

                CollectionAssert.AreEquivalent(expected, actual, $"{path}: every pane's vertices carry its room, and no other vertex one");
            }

            Assert.Greater(panes, 0, "panes with built rooms");
        }

        /// <summary>
        /// The far LOD's glass differs from the kit's only by the window shader's far switch, which writes depth and reads
        /// the panes' rooms from the mesh; kit glass never sets it, since a kit mesh has no room channel.
        /// </summary>
        [Test]
        public void TheFarGlassIsTheKitGlassWithOnlyTheFarSwitchOn()
        {
            var materials = AssetDatabase.FindAssets("t:Material").Select(AssetDatabase.GUIDToAssetPath)
                .Select(p => (Path: p, Material: AssetDatabase.LoadAssetAtPath<Material>(p)))
                .Where(m => m.Material != null && m.Material.shader != null && m.Material.shader.name == VeilmouthFarLod.WindowShader).ToList();
            var far = materials.Where(m => m.Path.StartsWith(VeilmouthFarLod.GlassFolder + "/", StringComparison.Ordinal)).ToList();
            Assert.IsNotEmpty(far, "far glass");
            foreach (var (path, m) in far)
            {
                Assert.IsNotNull(m.parent, $"{path} is a variant");
                Assert.AreEqual(VeilmouthFarLod.WindowShader, m.parent.shader.name, path);
                Assert.AreEqual(1f, m.GetFloat(VeilmouthFarLod.FarLodProperty), path);
                for (var i = 0; i < m.shader.GetPropertyCount(); i++)
                {
                    var name = m.shader.GetPropertyName(i);
                    Assert.AreEqual(name == VeilmouthFarLod.FarLodProperty, m.IsPropertyOverriden(name), $"{path}: {name} follows the kit glass");
                }
            }

            foreach (var (path, m) in materials.Except(far))
            {
                Assert.AreEqual(0f, m.GetFloat(VeilmouthFarLod.FarLodProperty), $"{path} is kit glass");
            }
        }

        /// <summary>
        /// The far renderers hold the sources' triangles: for each shadow setting and material the same count, and the
        /// same corners in the Model's space.
        /// </summary>
        public static void AssertSame(string what, List<VeilmouthFarLod.Source> sources, IEnumerable<MeshRenderer> far)
        {
            var expected = new Dictionary<(VeilmouthFarLod.Key, Material), long>();
            var min = Vector3.positiveInfinity;
            var max = Vector3.negativeInfinity;
            foreach (var s in sources)
            {
                for (var k = 0; k < s.Materials.Length; k++)
                {
                    if (s.Materials[k] != null)
                    {
                        var key = (s.Key, s.Materials[k]);
                        expected[key] = (expected.TryGetValue(key, out var n) ? n : 0) + (s.Mesh.GetIndexCount(Mathf.Min(k, s.Mesh.subMeshCount - 1)) / 3);
                    }
                }

                var b = s.Mesh.bounds;
                for (var c = 0; c < 8; c++)
                {
                    var p = s.ToModel.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1)));
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                }
            }

            var actual = new Dictionary<(VeilmouthFarLod.Key, Material), long>();
            var farMin = Vector3.positiveInfinity;
            var farMax = Vector3.negativeInfinity;
            foreach (var r in far.Where(r => r.enabled))
            {
                var mesh = r.GetComponent<MeshFilter>().sharedMesh;
                Assert.IsNotNull(mesh, $"{what}: {r.name}'s mesh");
                Assert.AreEqual(mesh.subMeshCount, r.sharedMaterials.Length, $"{what}: {r.name} has one material for each submesh");
                for (var k = 0; k < mesh.subMeshCount; k++)
                {
                    // Kit glass is drawn by its far variant, every other material by itself.
                    var drawn = r.sharedMaterials[k];
                    var origin = VeilmouthFarLod.Origin(drawn);
                    var glass = origin != null && origin.shader.name == VeilmouthFarLod.WindowShader;
                    Assert.AreEqual(glass, drawn != origin, $"{what}: {r.name} draws {drawn?.name} for {origin?.name}");
                    Assert.AreEqual(glass ? 1f : 0f, glass ? drawn.GetFloat(VeilmouthFarLod.FarLodProperty) : 0f, $"{what}: {drawn?.name}");
                    var key = (VeilmouthFarLod.Key.Of(r), origin);
                    actual[key] = (actual.TryGetValue(key, out var n) ? n : 0) + (mesh.GetIndexCount(k) / 3);
                }

                farMin = Vector3.Min(farMin, mesh.bounds.min);
                farMax = Vector3.Max(farMax, mesh.bounds.max);
            }

            CollectionAssert.AreEquivalent(expected, actual, $"{what}: triangles by shadow setting and material");
            // A piece's bounds may be wider than its vertices once turned, so the far mesh lies within them.
            Assert.IsTrue(farMin.x >= min.x - 0.01f && farMin.y >= min.y - 0.01f && farMin.z >= min.z - 0.01f &&
                          farMax.x <= max.x + 0.01f && farMax.y <= max.y + 0.01f && farMax.z <= max.z + 0.01f, $"{what}: the far mesh lies within its pieces");
        }

        /// <summary>The room of each vertex of a far mesh, from its third UV channel's two UNorm16 halves.</summary>
        public static uint[] Rooms(Mesh mesh)
        {
            using var data = Mesh.AcquireReadOnlyMeshData(mesh);
            var md = data[0];
            Assert.AreEqual(VertexAttributeFormat.UNorm16, md.GetVertexAttributeFormat(VertexAttribute.TexCoord2), mesh.name);
            var stream = md.GetVertexAttributeStream(VertexAttribute.TexCoord2);
            var offset = md.GetVertexAttributeOffset(VertexAttribute.TexCoord2);
            var stride = md.GetVertexBufferStride(stream);
            var raw = md.GetVertexData<byte>(stream);
            var rooms = new uint[md.vertexCount];
            for (var i = 0; i < rooms.Length; i++)
            {
                var at = (i * stride) + offset;
                rooms[i] = (uint)(raw[at] | (raw[at + 1] << 8)) | ((uint)(raw[at + 2] | (raw[at + 3] << 8)) << 16);
            }

            return rooms;
        }
    }
}

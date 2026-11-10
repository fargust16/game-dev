using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using GameDev.VictorianKit;
using GameDev.VictorianKit.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The merged far LOD of each kit house type (owner, 2026-11-10; Plans/VEILMOUTH_CITY_PLAN.md §29.10): the kit's own
    /// geometry of a house, every piece but the <see cref="VeilmouthKitHouses.Details"/> and the rooms, baked into one
    /// mesh for each shadow setting with one submesh for each material, drawn beyond <see cref="FarDistance"/> by a second
    /// LOD group on the house's root. Nothing is simplified: the same triangles with the same materials, so the swap
    /// cannot be seen and needs no cross-fade; each pane's room rides in the mesh's third UV channel for the window shader.
    /// </summary>
    /// <remarks>
    /// Built in place in the baked prefabs, as <see cref="VeilmouthKitHouses.RedressAll"/> builds the rooms, so every
    /// piece keeps its file ID. The houses the city changed after placing them (strange glass, walled-up back doors) are
    /// synced over Veilmouth by <see cref="SyncScene"/>. Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthFarLod.Run
    /// </remarks>
    public static class VeilmouthFarLod
    {
        /// <summary>The far LOD's child under a house's Model.</summary>
        public const string Name = "FarLod";

        /// <summary>Authoring default, m: beyond the built rooms' <see cref="VicRoomsSwitch.OnDistance"/>, so a far house is a shell.</summary>
        public const float FarDistance = 50f;

        /// <summary>Measured: the player's camera (Veilmouth.unity, FrameProbe's report).</summary>
        public const float FieldOfView = 60f;

        public static readonly string MeshFolder = VeilmouthKitHouses.Folder + "/FarLod";
        private static readonly string SceneMeshFolder = MeshFolder + "/Veilmouth";

        /// <summary>The far LOD's glass: a material variant of each kit glass with <see cref="FarLodProperty"/> on.</summary>
        public static readonly string GlassFolder = MeshFolder + "/Glass";

        public const string WindowShader = "GameDev/VictorianKit/Window";

        /// <summary>
        /// The window shader's switch for merged glass: depth written, since one renderer's panes draw in their index
        /// order and a pane behind, seen through the empty house, would otherwise draw over the one before it; and each
        /// pane's room read from the mesh, since a kit mesh has no such channel.
        /// </summary>
        public const string FarLodProperty = "_FarLod";

        private const string ReportPath = "Logs/SliceSurvey/c5_farlod.txt";

        /// <summary>What a renderer must share with the others to be drawn by the same far renderer.</summary>
        public struct Key : IEquatable<Key>
        {
            public ShadowCastingMode Shadows;
            public bool Receive;
            public LightProbeUsage Probes;
            public ReflectionProbeUsage Reflections;
            public MotionVectorGenerationMode Motion;
            public uint RenderingLayers;
            public int Layer;

            public static Key Of(Renderer r) => new Key
            {
                Shadows = r.shadowCastingMode,
                Receive = r.receiveShadows,
                Probes = r.lightProbeUsage,
                Reflections = r.reflectionProbeUsage,
                Motion = r.motionVectorGenerationMode,
                RenderingLayers = r.renderingLayerMask,
                Layer = r.gameObject.layer,
            };

            public bool Equals(Key o) => Shadows == o.Shadows && Receive == o.Receive && Probes == o.Probes && Reflections == o.Reflections &&
                                         Motion == o.Motion && RenderingLayers == o.RenderingLayers && Layer == o.Layer;

            public override bool Equals(object obj) => obj is Key k && Equals(k);

            public override int GetHashCode() => HashCode.Combine(Shadows, Receive, Probes, Reflections, Motion, RenderingLayers, Layer);

            public override string ToString() => $"shadows {Shadows}, receive {Receive}, probes {Probes}, reflections {Reflections}, layer {Layer}";
        }

        /// <summary>One renderer the far LOD replaces.</summary>
        public sealed class Source
        {
            public MeshRenderer Renderer;
            public Mesh Mesh;
            public Material[] Materials;
            public Matrix4x4 ToModel;
            public Key Key;
            public uint Room;
        }

        /// <summary>The sources of one far renderer: its materials in the order of its submeshes.</summary>
        public sealed class Group
        {
            public Key Key;
            public string Label;
            public readonly List<Source> Sources = new List<Source>();
            public readonly List<Material> Materials = new List<Material>();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Vertex
        {
            public Vector3 Position;
            public short NormalX, NormalY, NormalZ, NormalW;
            public short TangentX, TangentY, TangentZ, TangentW;
            public Vector2 Uv;
            public ushort RoomLow;
            public ushort RoomHigh;
        }

        private static readonly VertexAttributeDescriptor[] Layout =
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.SNorm16, 4),
            new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.SNorm16, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.UNorm16, 2),
        };

        private static readonly List<string> Report = new List<string>();
        private static int _noTangents, _noNormals, _kitUv2;

        // ------------------------------------------------------------------------------------------------ batch

        /// <summary>Batch: the 67 types, then the scene.</summary>
        public static void Run()
        {
            Report.Clear();
            BakeTypes();
            SyncScene();
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllLines(ReportPath, Report);
            Debug.Log($"[VeilmouthFarLod] done; report in {ReportPath}.");
        }

        /// <summary>Batch: the types only, the scene left as it is.</summary>
        public static void RunTypes()
        {
            Report.Clear();
            BakeTypes();
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllLines(ReportPath, Report);
        }

        // ------------------------------------------------------------------------------------------------ the types

        /// <summary>Every baked house type given its far LOD in place; stale type meshes removed.</summary>
        public static void BakeTypes()
        {
            EnsureFolder(MeshFolder);
            _noTangents = _noNormals = _kitUv2 = 0;
            var made = new HashSet<string>();
            long vertices = 0, bytes = 0, nearDraws = 0, farDraws = 0;
            int types = 0, sources = 0;
            var keys = new Dictionary<Key, int>();
            foreach (var path in Directory.GetFiles(VeilmouthKitHouses.Folder, "Kit_House_*.prefab").Select(p => p.Replace('\\', '/')).OrderBy(p => p, StringComparer.Ordinal))
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var stem = Path.GetFileNameWithoutExtension(path);
                    var groups = Build(root, stem, made);
                    foreach (var g in groups)
                    {
                        var mesh = root.transform.Find($"Model/{Name}/{g.Label}").GetComponent<MeshFilter>().sharedMesh;
                        vertices += mesh.vertexCount;
                        bytes += Bytes(mesh);
                        keys[g.Key] = keys.TryGetValue(g.Key, out var n) ? n + 1 : 1;
                    }

                    var list = groups.SelectMany(g => g.Sources).ToList();
                    sources += list.Count;
                    nearDraws += list.Sum(s => s.Materials.Length);
                    farDraws += groups.Sum(g => g.Materials.Count);
                    Report.Add($"{stem}: {list.Count} renderers, {list.Sum(s => s.Materials.Length)} draws near, " +
                               $"{groups.Sum(g => g.Materials.Count)} far in {groups.Count} renderers ({string.Join("; ", groups.Select(g => $"{g.Label} {g.Materials.Count}"))})");
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    types++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            foreach (var stale in Directory.GetFiles(MeshFolder, "*.asset").Select(p => p.Replace('\\', '/')).Where(p => !made.Contains(p)).ToList())
            {
                AssetDatabase.DeleteAsset(stale);
            }

            AssetDatabase.SaveAssets();
            var files = made.Sum(p => new FileInfo(p).Length);
            var line = $"[VeilmouthFarLod] {types} types: {sources} renderers, {nearDraws} draws near against {farDraws} far (one of each type); " +
                       $"{made.Count} far meshes, {vertices:N0} vertices, {bytes / 1048576.0:0.0} MB of vertex and index data, {files / 1048576.0:0.0} MB of asset files; " +
                       $"far renderers by setting: {string.Join("; ", keys.Select(k => $"{k.Value}× ({k.Key})"))}; " +
                       $"kit meshes without normals {_noNormals}, without tangents {_noTangents}, with a third UV channel {_kitUv2}.";
            Report.Insert(0, line);
            Debug.Log(line);
        }

        /// <summary>
        /// Gives one house (a prefab's contents or a fresh bake) its far LOD, replacing an older one: the meshes saved as
        /// assets named after <paramref name="stem"/>, a renderer for each group under Model/FarLod, and the house's
        /// root LOD group. <paramref name="made"/> collects the meshes' paths.
        /// </summary>
        public static List<Group> Build(GameObject root, string stem, HashSet<string> made = null)
        {
            var model = root.transform.Find("Model");
            if (model == null)
            {
                throw new InvalidOperationException($"{stem} has no Model.");
            }

            EnsureFolder(MeshFolder);
            var sources = Sources(model);
            var groups = Groups(sources);

            // An older far LOD is refilled, not made again, so its objects keep their file IDs and the city's overrides hold.
            var old = model.Find(Name);
            var far = old != null ? old.gameObject : new GameObject(Name);
            far.layer = model.gameObject.layer;
            Place(far.transform, model);
            foreach (var stale in far.transform.Cast<Transform>().Where(t => groups.All(g => g.Label != t.name)).ToList())
            {
                Object.DestroyImmediate(stale.gameObject);
            }

            var renderers = new List<Renderer>();
            foreach (var g in groups)
            {
                var path = $"{MeshFolder}/{stem}_{g.Label}.asset";
                var mesh = MeshAsset(path);
                Fill(mesh, $"{stem}_{g.Label}", g);
                Unreadable(mesh);
                made?.Add(path);
                renderers.Add(Renderer(far.transform, g, mesh));
            }

            var lod = root.GetComponent<LODGroup>();
            if (lod == null)
            {
                lod = root.AddComponent<LODGroup>();
            }

            Levels(lod, sources.Select(s => (Renderer)s.Renderer).ToArray(), renderers.ToArray(), null);
            return groups;
        }

        /// <summary>
        /// LOD0 the pieces, LOD1 the far LOD and never culled, switching at <see cref="FarDistance"/> for the group's own
        /// size with the camera's <see cref="FieldOfView"/> (High quality's LOD bias is 1), with no cross-fade; the size kept
        /// when <paramref name="keep"/> is given.
        /// </summary>
        public static void Levels(LODGroup lod, Renderer[] near, Renderer[] far, float? keep)
        {
            lod.fadeMode = LODFadeMode.None;
            lod.animateCrossFading = false;
            float height;
            if (keep.HasValue)
            {
                height = keep.Value;
            }
            else
            {
                lod.SetLODs(new[] { new LOD(0.5f, near), new LOD(0f, far) });
                lod.RecalculateBounds();
                height = Mathf.Clamp(lod.size / (2f * Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad) * FarDistance), 0.001f, 0.99f);
            }

            lod.SetLODs(new[] { new LOD(height, near), new LOD(0f, far) });
        }

        /// <summary>The distance at which a house's LOD group switches to its far LOD, in metres.</summary>
        public static float SwitchDistance(LODGroup lod) =>
            lod.size / (2f * Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad) * lod.GetLODs()[0].screenRelativeTransitionHeight);

        // ------------------------------------------------------------------------------------------------ sources

        /// <summary>
        /// The renderers a house's far LOD replaces, in hierarchy order: every enabled mesh renderer of an active piece
        /// under <paramref name="model"/> that is no detail and no part of a yard door, with each pane's room
        /// (<see cref="VicWindowRooms"/>). A yard door's pieces stay out because the city may wall it up
        /// (<see cref="VeilmouthKitHouses.AtYardDoor"/>): drawn at every distance, they let every house keep its type's
        /// far mesh. Works on prefab assets too, where nothing is active in a hierarchy.
        /// </summary>
        public static List<Source> Sources(Transform model)
        {
            var rooms = new Dictionary<MeshRenderer, uint>();
            var table = model.Find(VicRooms.RootName)?.GetComponent<VicWindowRooms>();
            for (var i = 0; table != null && i < table.Count; i++)
            {
                var (pane, room) = table[i];
                if (pane != null)
                {
                    rooms[pane] = room;
                }
            }

            var doors = model.parent.Cast<Transform>().Where(t => t.name == VeilmouthKitHouses.YardDoorMarker || t.name == VeilmouthCity.BlindDoorMarker)
                .Select(t => t.position).ToList();
            var toModel = model.worldToLocalMatrix;
            var list = new List<Source>();
            foreach (Transform piece in model)
            {
                if (!VeilmouthKitHouses.IsPiece(piece) || VeilmouthKitHouses.IsDetail(piece) || !piece.gameObject.activeSelf ||
                    VeilmouthKitHouses.AtYardDoor(piece, doors))
                {
                    continue;
                }

                foreach (var r in piece.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var f = r.GetComponent<MeshFilter>();
                    if (!r.enabled || f == null || f.sharedMesh == null || !Active(r.transform, piece))
                    {
                        continue;
                    }

                    list.Add(new Source
                    {
                        Renderer = r,
                        Mesh = f.sharedMesh,
                        Materials = r.sharedMaterials,
                        ToModel = toModel * r.transform.localToWorldMatrix,
                        Key = Key.Of(r),
                        Room = rooms.TryGetValue(r, out var room) ? room : 0u,
                    });
                }
            }

            return list;
        }

        private static bool Active(Transform t, Transform piece)
        {
            for (; t != null && t != piece; t = t.parent)
            {
                if (!t.gameObject.activeSelf)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>The sources by their <see cref="Key"/>, in order of first appearance; each group's materials likewise.</summary>
        public static List<Group> Groups(List<Source> sources)
        {
            var groups = new List<Group>();
            foreach (var s in sources)
            {
                var g = groups.FirstOrDefault(x => x.Key.Equals(s.Key));
                if (g == null)
                {
                    g = new Group { Key = s.Key };
                    groups.Add(g);
                }

                g.Sources.Add(s);
                foreach (var m in s.Materials.Where(m => m != null && !g.Materials.Contains(m)))
                {
                    g.Materials.Add(m);
                }
            }

            foreach (var g in groups)
            {
                var label = g.Key.Shadows == ShadowCastingMode.Off ? "NoShadows" : g.Key.Shadows.ToString();
                var same = groups.Where(x => (x.Key.Shadows == ShadowCastingMode.Off ? "NoShadows" : x.Key.Shadows.ToString()) == label).ToList();
                g.Label = same.Count > 1 ? $"{label}{same.IndexOf(g)}" : label;
            }

            return groups;
        }

        // ------------------------------------------------------------------------------------------------ meshes

        /// <summary>
        /// The group's sources baked into the Model's space: positions by the source's matrix, normals by its inverse
        /// transpose, tangents by the matrix with their sign flipped where it mirrors, and the winding reversed there.
        /// A renderer with more materials than submeshes draws its last submesh again, as Unity does.
        /// </summary>
        private static void Fill(Mesh mesh, string name, Group g)
        {
            var vertices = new List<Vertex>();
            var triangles = g.Materials.Select(_ => new List<int>()).ToList();
            var p = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<Vector4>();
            var uv = new List<Vector2>();
            foreach (var s in g.Sources)
            {
                var m = s.Mesh;
                m.GetVertices(p);
                m.GetNormals(n);
                m.GetTangents(t);
                m.GetUVs(0, uv);
                if (n.Count != p.Count)
                {
                    _noNormals++;
                }

                if (t.Count != p.Count)
                {
                    _noTangents++;
                }

                if (m.HasVertexAttribute(VertexAttribute.TexCoord2))
                {
                    _kitUv2++;
                }

                var basis = s.ToModel;
                var normals = basis.inverse.transpose;
                var mirror = basis.determinant < 0f;
                var start = vertices.Count;
                var low = (ushort)(s.Room & 0xFFFF);
                var high = (ushort)(s.Room >> 16);
                for (var i = 0; i < p.Count; i++)
                {
                    var tangent = i < t.Count ? t[i] : new Vector4(1f, 0f, 0f, 1f);
                    var tw = basis.MultiplyVector(new Vector3(tangent.x, tangent.y, tangent.z)).normalized;
                    var nw = normals.MultiplyVector(i < n.Count ? n[i] : Vector3.up).normalized;
                    vertices.Add(new Vertex
                    {
                        Position = basis.MultiplyPoint3x4(p[i]),
                        NormalX = Snorm(nw.x), NormalY = Snorm(nw.y), NormalZ = Snorm(nw.z),
                        TangentX = Snorm(tw.x), TangentY = Snorm(tw.y), TangentZ = Snorm(tw.z), TangentW = Snorm(mirror ? -tangent.w : tangent.w),
                        Uv = i < uv.Count ? uv[i] : Vector2.zero,
                        RoomLow = low,
                        RoomHigh = high,
                    });
                }

                for (var k = 0; k < s.Materials.Length; k++)
                {
                    if (s.Materials[k] == null || m.subMeshCount == 0)
                    {
                        continue;
                    }

                    var sub = Mathf.Min(k, m.subMeshCount - 1);
                    if (m.GetTopology(sub) != MeshTopology.Triangles)
                    {
                        throw new InvalidOperationException($"{m.name} submesh {sub} is {m.GetTopology(sub)}, not triangles.");
                    }

                    var source = m.GetIndices(sub, true);
                    var into = triangles[g.Materials.IndexOf(s.Materials[k])];
                    for (var i = 0; i < source.Length; i += 3)
                    {
                        into.Add(start + source[i]);
                        into.Add(start + (mirror ? source[i + 2] : source[i + 1]));
                        into.Add(start + (mirror ? source[i + 1] : source[i + 2]));
                    }
                }
            }

            mesh.Clear();
            mesh.name = name;
            mesh.indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertexBufferParams(vertices.Count, Layout);
            mesh.SetVertexBufferData(vertices, 0, 0, vertices.Count);
            mesh.subMeshCount = triangles.Count;
            for (var k = 0; k < triangles.Count; k++)
            {
                mesh.SetIndices(triangles[k], MeshTopology.Triangles, k, false);
            }

            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
        }

        /// <summary>A unit component as SNorm16: 1 in 32 767, far finer than a far house shows.</summary>
        private static short Snorm(float v) => (short)Mathf.RoundToInt(Mathf.Clamp(v, -1f, 1f) * 32767f);

        private static Mesh MeshAsset(string path)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null)
            {
                return mesh;
            }

            mesh = new Mesh();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        /// <summary>No CPU copy in the build: the far meshes are only drawn.</summary>
        private static void Unreadable(Mesh mesh)
        {
            var so = new SerializedObject(mesh);
            so.FindProperty("m_IsReadable").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static long Bytes(Mesh mesh)
        {
            long indices = 0;
            for (var k = 0; k < mesh.subMeshCount; k++)
            {
                indices += mesh.GetIndexCount(k);
            }

            return ((long)mesh.vertexCount * mesh.GetVertexBufferStride(0)) + (indices * (mesh.indexFormat == IndexFormat.UInt32 ? 4 : 2));
        }

        /// <summary>A far renderer under <paramref name="far"/> with the group's first source's settings; not static, so its mesh stays shared.</summary>
        private static MeshRenderer Renderer(Transform far, Group g, Mesh mesh)
        {
            var old = far.Find(g.Label);
            var go = old != null ? old.gameObject : new GameObject(g.Label);
            go.layer = g.Key.Layer;
            Place(go.transform, far);
            var filter = go.GetComponent<MeshFilter>();
            if (filter == null)
            {
                filter = go.AddComponent<MeshFilter>();
            }

            filter.sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            if (r == null)
            {
                r = go.AddComponent<MeshRenderer>();
            }

            var from = g.Sources[0].Renderer;
            r.shadowCastingMode = from.shadowCastingMode;
            r.receiveShadows = from.receiveShadows;
            r.lightProbeUsage = from.lightProbeUsage;
            r.reflectionProbeUsage = from.reflectionProbeUsage;
            r.motionVectorGenerationMode = from.motionVectorGenerationMode;
            r.renderingLayerMask = from.renderingLayerMask;
            r.allowOcclusionWhenDynamic = from.allowOcclusionWhenDynamic;
            r.staticShadowCaster = from.staticShadowCaster;
            r.receiveGI = ReceiveGI.LightProbes;
            r.sharedMaterials = g.Materials.Select(Far).ToArray();
            return r;
        }

        /// <summary>
        /// The material a far renderer draws in place of <paramref name="m"/>: for kit glass its far variant, made once
        /// and kept, a material variant of the glass so that a later change to the glass reaches it; any other material
        /// itself.
        /// </summary>
        public static Material Far(Material m)
        {
            if (m == null || m.shader == null || m.shader.name != WindowShader || Origin(m) != m)
            {
                return m;
            }

            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m, out var guid, out long _))
            {
                throw new InvalidOperationException($"{m.name} is no asset, so it has no far variant.");
            }

            EnsureFolder(GlassFolder);
            var path = $"{GlassFolder}/{Safe(m.name)}_{guid.Substring(0, 8)}.mat";
            var far = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (far == null)
            {
                far = new Material(m) { name = Path.GetFileNameWithoutExtension(path) };
                far.parent = m;
                far.SetFloat(FarLodProperty, 1f);
                AssetDatabase.CreateAsset(far, path);
            }
            else if (far.parent != m || far.GetFloat(FarLodProperty) < 0.5f)
            {
                far.parent = m;
                far.SetFloat(FarLodProperty, 1f);
                EditorUtility.SetDirty(far);
            }

            return far;
        }

        /// <summary>The kit material a far renderer's material stands for: a far glass variant's parent, else itself.</summary>
        public static Material Origin(Material m) =>
            m != null && m.parent != null && AssetDatabase.GetAssetPath(m).StartsWith(GlassFolder + "/", StringComparison.Ordinal) ? m.parent : m;

        /// <summary>A far LOD object under <paramref name="parent"/> with no transform of its own and no static flags, so its mesh stays shared.</summary>
        private static void Place(Transform t, Transform parent)
        {
            t.SetParent(parent, false);
            t.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            t.localScale = Vector3.one;
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        }

        // ------------------------------------------------------------------------------------------------ the scene

        /// <summary>
        /// Every kit house in Veilmouth checked against its type: one that differs only in materials keeps the type's
        /// mesh with its own materials mapped; one whose pieces differ gets a far mesh of its own; either way its LOD0 is
        /// its own pieces. Earlier syncs are undone first, so it can run again. Saves the scene.
        /// </summary>
        public static void SyncScene()
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            if (AssetDatabase.IsValidFolder(SceneMeshFolder))
            {
                AssetDatabase.DeleteAsset(SceneMeshFolder);
            }

            int houses = 0, same = 0, painted = 0, own = 0;
            long nearDraws = 0, farDraws = 0, ownBytes = 0;
            var lines = new List<string>();
            var houseRoots = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject)
                .Where(PrefabUtility.IsOutermostPrefabInstanceRoot).ToList();
            foreach (var house in houseRoots)
            {
                var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(house);
                if (path == null || !path.StartsWith(VeilmouthKitHouses.Folder + "/Kit_House_"))
                {
                    continue;
                }

                var model = house.transform.Find("Model");
                var far = model != null ? model.Find(Name) : null;
                var lod = house.GetComponent<LODGroup>();
                if (far == null || lod == null)
                {
                    throw new InvalidOperationException($"{house.name} ({path}) has no far LOD: bake the types first.");
                }

                houses++;
                var where = $"{house.name} at ({house.transform.position.x:0}, {house.transform.position.z:0})";
                Unsync(far, lod);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var theirs = Sources(asset.transform.Find("Model"));
                var mine = Sources(model);
                var typeGroups = Groups(theirs);
                var farRenderers = far.Cast<Transform>().Select(t => t.GetComponent<MeshRenderer>()).ToList();
                nearDraws += mine.Sum(s => s.Materials.Length);

                var shape = mine.Count == theirs.Count && Enumerable.Range(0, mine.Count).All(i => SameShape(mine[i], theirs[i]));
                if (shape && Enumerable.Range(0, mine.Count).All(i => mine[i].Materials.SequenceEqual(theirs[i].Materials)))
                {
                    same++;
                    farDraws += typeGroups.Sum(g => g.Materials.Count);
                    continue;
                }

                if (shape && Remap(mine, theirs, typeGroups, out var mapped))
                {
                    for (var gi = 0; gi < typeGroups.Count; gi++)
                    {
                        farRenderers[gi].sharedMaterials = mapped[gi].Select(Far).ToArray();
                        PrefabUtility.RecordPrefabInstancePropertyModifications(farRenderers[gi]);
                    }

                    painted++;
                    farDraws += typeGroups.Sum(g => g.Materials.Count);
                    lines.Add($"{where} ({Path.GetFileNameWithoutExtension(path)}): its own materials on the type's far mesh");
                    continue;
                }

                // Pieces differ: a far mesh of its own for each group, on the type's renderer of the same setting or a new one.
                var groups = Groups(mine);
                var used = new List<Renderer>();
                foreach (var g in groups)
                {
                    EnsureFolder(SceneMeshFolder);
                    var stem = $"{own:000}_{Safe(Path.GetFileNameWithoutExtension(path))}_{g.Label}";
                    var mesh = MeshAsset($"{SceneMeshFolder}/{stem}.asset");
                    Fill(mesh, stem, g);
                    Unreadable(mesh);
                    ownBytes += Bytes(mesh);
                    var at = typeGroups.FindIndex(x => x.Key.Equals(g.Key));
                    MeshRenderer r;
                    if (at >= 0)
                    {
                        r = farRenderers[at];
                        var filter = r.GetComponent<MeshFilter>();
                        filter.sharedMesh = mesh;
                        r.sharedMaterials = g.Materials.Select(Far).ToArray();
                        PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                    }
                    else
                    {
                        r = Renderer(far, g, mesh);
                    }

                    used.Add(r);
                }

                foreach (var r in farRenderers.Where(r => !used.Contains(r)))
                {
                    r.enabled = false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                }

                Levels(lod, mine.Select(s => (Renderer)s.Renderer).ToArray(), used.ToArray(), lod.GetLODs()[0].screenRelativeTransitionHeight);
                PrefabUtility.RecordPrefabInstancePropertyModifications(lod);
                own++;
                farDraws += groups.Sum(g => g.Materials.Count);
                lines.Add($"{where} ({Path.GetFileNameWithoutExtension(path)}): a far mesh of its own, {mine.Count} renderers against the type's {theirs.Count}");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            var line = $"[VeilmouthFarLod] Veilmouth: {houses} kit houses, {same} as their type, {painted} with their own materials, {own} with a far mesh of their own " +
                       $"({ownBytes / 1048576.0:0.0} MB); {nearDraws} draws near against {farDraws} far.";
            Report.Add(string.Empty);
            Report.Add(line);
            Report.AddRange(lines);
            Debug.Log(line);
        }

        /// <summary>An earlier sync undone on one house: its far renderers, filters and root LOD group back to the prefab's, added renderers gone.</summary>
        private static void Unsync(Transform far, LODGroup lod)
        {
            foreach (var added in far.Cast<Transform>().Where(t => PrefabUtility.IsAddedGameObjectOverride(t.gameObject)).ToList())
            {
                Object.DestroyImmediate(added.gameObject);
            }

            foreach (Transform t in far)
            {
                PrefabUtility.RevertObjectOverride(t.GetComponent<MeshRenderer>(), InteractionMode.AutomatedAction);
                PrefabUtility.RevertObjectOverride(t.GetComponent<MeshFilter>(), InteractionMode.AutomatedAction);
            }

            PrefabUtility.RevertObjectOverride(lod, InteractionMode.AutomatedAction);
        }

        private static bool SameShape(Source a, Source b)
        {
            if (a.Mesh != b.Mesh || !a.Key.Equals(b.Key) || a.Room != b.Room || a.Materials.Length != b.Materials.Length)
            {
                return false;
            }

            for (var i = 0; i < 16; i++)
            {
                if (Mathf.Abs(a.ToModel[i] - b.ToModel[i]) > 1e-4f)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Whether the house's materials are the type's under one mapping in each group (each type material always
        /// becomes the same one), and the far renderers' materials under it.
        /// </summary>
        private static bool Remap(List<Source> mine, List<Source> theirs, List<Group> groups, out Material[][] mapped)
        {
            mapped = new Material[groups.Count][];
            for (var gi = 0; gi < groups.Count; gi++)
            {
                var map = new Dictionary<Material, Material>();
                for (var i = 0; i < theirs.Count; i++)
                {
                    if (!theirs[i].Key.Equals(groups[gi].Key))
                    {
                        continue;
                    }

                    for (var k = 0; k < theirs[i].Materials.Length; k++)
                    {
                        var from = theirs[i].Materials[k];
                        var to = mine[i].Materials[k];
                        if (from == null || to == null)
                        {
                            if (from != to)
                            {
                                return false;
                            }

                            continue;
                        }

                        if (map.TryGetValue(from, out var was) && was != to)
                        {
                            return false;
                        }

                        map[from] = to;
                    }
                }

                mapped[gi] = groups[gi].Materials.Select(m => map.TryGetValue(m, out var to) ? to : m).ToArray();
            }

            return true;
        }

        private static string Safe(string name)
        {
            var sb = new StringBuilder();
            foreach (var c in name)
            {
                sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            }

            return sb.ToString();
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Houses for the city plan C1 (§5.1), assembled from the Modular Victorian City modules and baked into one mesh per
    /// house type, so a city of hundreds of houses is hundreds of objects, not tens of thousands.
    /// <para>
    /// The modules (survey of 2026-11-05) are 2 m cells, 3 m storeys, pivoted in the cell's middle with their wall on
    /// the cell's +Z edge; a corner module walls the +Z and −X edges. A house is a ring of such cells per storey, each
    /// turned so its wall faces out, under a ring of roof slopes round a flat top (a mansard). It is baked at ×1.25
    /// (city plan §2): a 2.5 m module, a 3.75 m storey. The vendor files are never changed.
    /// </para>
    /// </summary>
    public static class VictorianHouses
    {
        public const float Scale = 1.25f;
        public const float Module = 2f * Scale;
        public const float Storey = 3f * Scale;

        private const string Modules = "Assets/Modular Victorian City/Prefabs/Modules/";
        private const string Basic = "Assets/Modular Victorian City/Prefabs/Basic elements/";
        private const string Materials = "Assets/Modular Victorian City/Materials/";
        public const string Folder = SliceKitCatalogue.Folder + "/Houses";

        /// <summary>The name of the empty child that marks a doorway on a baked house's front.</summary>
        public const string DoorMarker = "Door";

        public enum Ground { Door, Window, Wall, Shop, ShopDoor }

        public sealed class Spec
        {
            public string Name;
            public int Front;      // modules along the street
            public int Depth;      // modules from the street back
            public int Storeys;
            public int[] Doors = { };     // front modules with the house door
            public bool Shop;             // the ground floor is a shop front with awnings
            public string Facade = "Wall1"; // the walls' material: Wall1–Wall4, Brick01, Brick02, Stone wall
            public bool Dormers = true;
            public int Arch = -1;         // the first of the two front modules an arch passes under, or none
            public Side Faced;            // a corner house: the side with a second facade on a side street
        }

        /// <summary>A house's sides by its own axes, its front on +Z: −X is Left.</summary>
        public enum Side { None, Left, Right }

        /// <summary>Survey of 2026-11-06, at ×1: the house door's stoop, 0.66 m out from the front and 0.4 m high.</summary>
        private const float StoopOut = 0.66f, StoopHigh = 0.4f, StoopHalfWidth = 0.87f;

        /// <summary>Survey: the pilasters and the door frames stand up to 0.13 m proud of the front at ×1.</summary>
        private const float Proud = 0.13f;

        /// <summary>How far a house door's stoop reaches out from the front, m at ×1.25; the city's steps start there.</summary>
        public const float Stoop = StoopOut * Scale;

        /// <summary>The key of a type's corner variant, its second facade on <paramref name="side"/>.</summary>
        public static string CornerKey(string key, Side side) => key + (side == Side.Left ? "_cornerL" : "_cornerR");

        /// <summary>The types baked with a corner variant on each side: the terrace types a row ends with.</summary>
        public static readonly string[] CornerTypes = { "Shop4", "Terrace4", "Tenement4" };

        private static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path + ".prefab");

        /// <summary>
        /// Places the modules of a house under <paramref name="parent"/>, in module metres (×1). The owner's play test
        /// of 2026-11-06 set three rules: a shop's ground floor is its shop front to the corners, with no stone corner
        /// between (one colour set per floor); the cornice runs round the roof's sides too; and a corner house shows a
        /// second facade on its side street.
        /// </summary>
        public static void Assemble(Transform parent, Spec spec)
        {
            for (var s = 0; s < spec.Storeys; s++)
            {
                var y = s * 3f;
                var up = s > 0;
                for (var i = 0; i < spec.Front; i++)
                {
                    for (var j = 0; j < spec.Depth; j++)
                    {
                        var front = j == spec.Depth - 1;
                        var back = j == 0;
                        var left = i == 0;
                        var right = i == spec.Front - 1;
                        if (!(front || back || left || right))
                        {
                            continue;
                        }

                        var centre = new Vector3((i * 2f) + 1f, y, (j * 2f) + 1f);
                        if (spec.Arch >= 0 && s < 2 && (i == spec.Arch || i == spec.Arch + 1))
                        {
                            continue; // the arch's own model walls its passage (below)
                        }

                        var faced = (left && spec.Faced == Side.Left) || (right && spec.Faced == Side.Right);
                        var sideYaw = left ? 270f : 90f;
                        if (front && (left || right) && spec.Shop && !up)
                        {
                            // The shop front runs to the corner: its end post (−X on the module) covers the corner, so the
                            // right-hand one is mirrored. The side is a shop front too on a side street, else a plain wall.
                            Put(parent, Modules + "WoodenWallBlue", centre, 0f, 0.95f, mirror: right);
                            Put(parent, Modules + (faced ? "WoodenWallBlue" : "GroundSideWall1"), centre, sideYaw,
                                faced ? 0.95f : 0f, mirror: faced && left);
                            continue;
                        }

                        if ((front || back) && (left || right))
                        {
                            var yaw = front ? (left ? 0f : 90f) : (right ? 180f : 270f);
                            Put(parent, Modules + (up ? "UpCornelWall1" : "GroundCornelWall1"), centre, yaw);
                            continue;
                        }

                        if (front)
                        {
                            var module = FrontModule(spec, i, up);
                            Put(parent, Modules + module, centre, 0f, module.StartsWith("Wo") ? 0.95f : 0f); // shop fronts stand 1 m back
                        }
                        else if (back)
                        {
                            Put(parent, Modules + (up ? "UpWindow1" : "GroundWall1"), centre, 180f);
                        }
                        else if (faced)
                        {
                            var module = up ? "UpWindow1" : spec.Shop ? "WoodenWallBlue" : "GroundWindow1";
                            Put(parent, Modules + module, centre, sideYaw, module.StartsWith("Wo") ? 0.95f : 0f);
                        }
                        else
                        {
                            Put(parent, Modules + (up ? "UpSideWall1" : "GroundSideWall1"), centre, sideYaw);
                        }
                    }
                }
            }

            if (spec.Arch >= 0)
            {
                // Modules/Arch: 4 m wide, two storeys, its passage 6.25 m back from its front at the pivot.
                Put(parent, Modules + "Arch", new Vector3((spec.Arch + 1) * 2f, 0f, spec.Depth * 2f), 0f);
            }

            var top = spec.Storeys * 3f;
            for (var i = 0; i < spec.Front; i++)
            {
                for (var j = 0; j < spec.Depth; j++)
                {
                    var front = j == spec.Depth - 1;
                    var back = j == 0;
                    var left = i == 0;
                    var right = i == spec.Front - 1;
                    var centre = new Vector3((i * 2f) + 1f, top, (j * 2f) + 1f);
                    // The pack's roof is a mansard of slopes along the front and the back, a gable end on each
                    // (RoofCorner: a +Z slope with its gable on −X, mirrored for the other end), and a flat top
                    // whose ends are closed by a plain upper wall.
                    if ((front || back) && (left || right))
                    {
                        var mirror = front ? right : left;
                        Put(parent, Modules + "RoofCorner", centre, front ? 0f : 180f, mirror: mirror);
                    }
                    else if (front || back)
                    {
                        var dormer = spec.Dormers && front && i % 2 == 1;
                        Put(parent, Modules + (dormer ? "Roof Window" : "Roof"), centre, front ? 0f : 180f);
                    }
                    else
                    {
                        Put(parent, Modules + "RoofUp", centre, 0f);
                        if (left || right)
                        {
                            var yaw = left ? 270f : 90f;
                            Put(parent, Modules + "UpSideWall1", centre, yaw);
                            // The corner's cornice ("Frame top (1)") goes on along the gable end: a Roof module's
                            // own cornice, turned to the side, without its slope.
                            var cornice = Put(parent, Modules + "Roof", centre, yaw);
                            if (cornice != null)
                            {
                                PrefabUtility.UnpackPrefabInstance(cornice, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                                foreach (var slope in cornice.GetComponentsInChildren<Transform>(true).Where(t => t.name == "The roof is sloping").ToArray())
                                {
                                    Object.DestroyImmediate(slope.gameObject);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static string FrontModule(Spec spec, int i, bool up)
        {
            if (up)
            {
                return "UpWindow1";
            }

            if (spec.Doors.Contains(i))
            {
                return spec.Shop ? "WoddenDoorBlue" : "GroundDoor1";
            }

            return spec.Shop ? "WoodenWallBlue" : "GroundWindow1";
        }

        private static GameObject Put(Transform parent, string path, Vector3 position, float yaw, float forward = 0f, bool mirror = false)
        {
            var prefab = Load(path);
            if (prefab == null)
            {
                Debug.LogError($"[VictorianHouses] missing {path}");
                return null;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localPosition = position + (go.transform.localRotation * (Vector3.forward * forward));
            if (mirror)
            {
                go.transform.localScale = new Vector3(-1f, 1f, 1f);
            }

            return go;
        }

        /// <summary>
        /// Bakes a house into one mesh (a submesh per material) at ×1.25 and saves its kit wrapper
        /// <c>Kit_House_&lt;Name&gt;_&lt;W&gt;x&lt;H&gt;</c>: footprint [0, W] × [0, D] beyond the pivot's node, the front on +Z,
        /// a box collider over the footprint up to the eaves (none where an arch passes), on the Structure layer.
        /// </summary>
        public static SliceKitCatalogue.Entry Bake(Spec spec, IReadOnlyDictionary<string, string> facade = null)
        {
            KitStandIns.EnsureFolder(Folder);
            var temp = new GameObject("assembly");
            Assemble(temp.transform, spec);
            temp.transform.localScale = Vector3.one * Scale;

            var groups = new Dictionary<Material, Group>();
            var order = new List<Material>();
            foreach (var r in temp.GetComponentsInChildren<MeshRenderer>())
            {
                var filter = r.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                // The shop door stands on the pavement (owner's play test, 2026-11-06): the pack lifts its double door
                // 0.96 m and its portal 0.6 m onto a sill strip, which goes.
                var drop = r.name == "Double door" ? 0.96f : r.name == "Door portal" ? 0.6f : 0f;
                if (r.name == "Wooden base end")
                {
                    continue;
                }

                var mesh = filter.sharedMesh;
                var m = Matrix4x4.Translate(Vector3.down * (drop * Scale)) * r.transform.localToWorldMatrix; // the assembly stands at the origin with the scale on its root
                var normalMatrix = m.inverse.transpose;
                var flip = m.determinant < 0f;
                var verts = mesh.vertices;
                var norms = mesh.normals;
                var tans = mesh.tangents;
                var uvs = mesh.uv;
                for (var sub = 0; sub < mesh.subMeshCount && sub < r.sharedMaterials.Length; sub++)
                {
                    var material = r.sharedMaterials[sub];
                    if (material == null)
                    {
                        continue;
                    }

                    if (facade != null && facade.TryGetValue(material.name, out var swap))
                    {
                        material = AssetDatabase.LoadAssetAtPath<Material>(Materials + swap + ".mat") ?? material;
                    }

                    if (!groups.TryGetValue(material, out var g))
                    {
                        g = new Group();
                        groups[material] = g;
                        order.Add(material);
                    }

                    var map = new Dictionary<int, int>();
                    var tris = mesh.GetTriangles(sub);
                    for (var k = 0; k < tris.Length; k += 3)
                    {
                        var corners = flip ? new[] { tris[k], tris[k + 2], tris[k + 1] } : new[] { tris[k], tris[k + 1], tris[k + 2] };
                        foreach (var src in corners)
                        {
                            if (!map.TryGetValue(src, out var dst))
                            {
                                dst = g.V.Count;
                                map[src] = dst;
                                g.V.Add(m.MultiplyPoint3x4(verts[src]));
                                g.N.Add(norms.Length > src ? normalMatrix.MultiplyVector(norms[src]).normalized : Vector3.up);
                                if (tans.Length > src)
                                {
                                    var tv = m.MultiplyVector(new Vector3(tans[src].x, tans[src].y, tans[src].z)).normalized;
                                    g.T.Add(new Vector4(tv.x, tv.y, tv.z, tans[src].w * (flip ? -1f : 1f)));
                                }
                                else
                                {
                                    g.T.Add(new Vector4(1f, 0f, 0f, 1f));
                                }

                                g.UV.Add(uvs.Length > src ? uvs[src] : Vector2.zero);
                            }

                            g.I.Add(dst);
                        }
                    }
                }
            }

            Object.DestroyImmediate(temp);

            var baked = new Mesh { name = spec.Name, indexFormat = IndexFormat.UInt32 };
            var all = order.Select(mat => groups[mat]).ToList();
            baked.SetVertices(all.SelectMany(g => g.V).ToList());
            baked.SetNormals(all.SelectMany(g => g.N).ToList());
            baked.SetTangents(all.SelectMany(g => g.T).ToList());
            baked.SetUVs(0, all.SelectMany(g => g.UV).ToList());
            baked.subMeshCount = all.Count;
            var offset = 0;
            for (var k = 0; k < all.Count; k++)
            {
                var start = offset;
                baked.SetTriangles(all[k].I.Select(x => x + start).ToList(), k, true);
                offset += all[k].V.Count;
            }

            baked.RecalculateBounds();
            var meshPath = $"{Folder}/{spec.Name}_Mesh.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(baked, existing);
                baked = existing;
            }
            else
            {
                AssetDatabase.CreateAsset(baked, meshPath);
            }

            var width = spec.Front * Module;
            var depth = spec.Depth * Module;
            var eaves = spec.Storeys * Storey;
            var height = Mathf.RoundToInt(eaves + Storey);
            var root = new GameObject($"Kit_House_{spec.Name}_{Mathf.RoundToInt(width)}x{height}");
            root.AddComponent<KitBlock>();
            var model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            model.AddComponent<MeshFilter>().sharedMesh = baked;
            model.AddComponent<MeshRenderer>().sharedMaterials = order.ToArray();

            // The body reaches out over the pilasters and door frames, so a walker meets what is seen and its head does
            // not pass into the front (owner's play test, 2026-11-06). What stands proud of the footprint, this strip
            // and each stoop, is a nested block of its own, so the kit check measures it for itself.
            var proud = depth + (Proud * Scale);
            var front = Nested(root, "Front");
            // The strip stops short of each end by its own depth: where two rows meet in a square's inner corner, the
            // fronts would otherwise cross in a 0.16 m square (kit check, 2026-11-06). A walker cannot reach the notch.
            var inset = Proud * Scale;
            if (spec.Arch >= 0)
            {
                // The passage through the house (city plan §5.2, §7): open between its piers for two storeys.
                var x0 = spec.Arch * Module;
                var x1 = x0 + (2 * Module);
                Box(root, 0f, x0, 0f, eaves, 0f, depth);
                Box(root, x1, width, 0f, eaves, 0f, depth);
                Box(root, x0, x1, 2f * Storey, eaves, 0f, depth);
                Box(front, inset, x0, 0f, eaves, depth, proud);
                Box(front, x1, width - inset, 0f, eaves, depth, proud);
                Box(front, x0, x1, 2f * Storey, eaves, depth, proud);
            }
            else
            {
                Box(root, 0f, width, 0f, eaves, 0f, depth);
                Box(front, inset, width - inset, 0f, eaves, depth, proud);
            }

            if (!spec.Shop)
            {
                // The stoop before each house door is climbed (owner's play test, 2026-11-06): three steps whose fronts
                // follow the pack's own, measured at ×1.
                foreach (var i in spec.Doors)
                {
                    var stoop = Nested(root, "Stoop");
                    var mid = (i + 0.5f) * Module;
                    var half = StoopHalfWidth * Scale;
                    for (var k = 1; k <= 3; k++)
                    {
                        var reach = StoopOut * Scale * (4 - k) / 3f;
                        Box(stoop, mid - half, mid + half, 0f, StoopHigh * Scale * k / 3f, proud, depth + reach);
                    }
                }
            }

            // A marker in each doorway, on the front at floor level, facing out: the street furniture rules (city plan §4)
            // and their test read doors from these.
            foreach (var i in spec.Doors)
            {
                var door = new GameObject(DoorMarker);
                door.transform.SetParent(root.transform, false);
                door.transform.localPosition = new Vector3((i + 0.5f) * Module, 0f, depth);
            }

            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = LayerMask.NameToLayer(KitLayers.Structure);
            }

            GameObjectUtility.SetStaticEditorFlags(root, StaticEditorFlags.BatchingStatic);
            var path = $"{Folder}/{root.name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return new SliceKitCatalogue.Entry
            {
                Key = "House_" + spec.Name, Path = path, Width = Mathf.RoundToInt(width), Depth = Mathf.RoundToInt(depth),
                Height = height, DoorX = DoorX(spec), Stoop = spec.Shop ? 0f : Stoop,
            };
        }

        private sealed class Group
        {
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<Vector3> N = new List<Vector3>();
            public readonly List<Vector4> T = new List<Vector4>();
            public readonly List<Vector2> UV = new List<Vector2>();
            public readonly List<int> I = new List<int>();
        }

        /// <summary>
        /// Facade colours (city plan §2: brick and stucco, few painted fronts, against the toy look): the pack's upper
        /// wall (Wall1) and ground wall (Wall2) and its blue shop front swapped for other pack materials.
        /// </summary>
        public static Dictionary<string, string> Facade(string variant, string shop = "blue", bool shopHouse = false)
        {
            var map = new Dictionary<string, string>();
            switch (variant)
            {
                case "brick": map["Wall1"] = "Brick01"; map["Wall2"] = "Stone wall"; break;
                case "brick2": map["Wall1"] = "Brick02"; map["Wall2"] = "Stone wall"; break;
                case "stucco": map["Wall1"] = "Stone wall"; map["Wall2"] = "Grey"; break;
                case "grey": map["Wall1"] = "Grey"; map["Wall2"] = "Stone wall"; break;
                case "stone": map["Wall1"] = "Stone wall"; map["Wall2"] = "Stone wall"; break;
                case "marble": map["Wall1"] = "Marble"; map["Wall2"] = "Stone wall"; break;
                case "warehouse": map["Wall1"] = "Brick02"; map["Wall2"] = "Brick02"; break;
                default: map["Wall2"] = "Stone wall"; break; // the pack's blue, rare
            }

            if (shopHouse)
            {
                // A shop's ground floor is its shop front; what little ground wall it shows (the sides, the back) is the
                // upper floors' own, so the house reads as one (owner's play test, 2026-11-06: "not like lego").
                map["Wall2"] = map.TryGetValue("Wall1", out var upper) ? upper : "Wall1";
            }

            if (shop != "blue")
            {
                foreach (var part in new[] { "Wooden wall", "Wood border", "Double door", "Awning" })
                {
                    map[$"{part} blue"] = $"{part} {shop}";
                }
            }

            return map;
        }

        /// <summary>The house types of C1 §5.2 at ×1.25, in modules: front, depth, storeys, doors, shop, arch.</summary>
        public static readonly Spec[] Types =
        {
            new Spec { Name = "Shop4", Front = 4, Depth = 4, Storeys = 4, Doors = new[] { 1 }, Shop = true },
            new Spec { Name = "Shop6", Front = 6, Depth = 4, Storeys = 4, Doors = new[] { 1, 4 }, Shop = true },
            new Spec { Name = "Terrace4", Front = 4, Depth = 4, Storeys = 3, Doors = new[] { 1 } },
            new Spec { Name = "Tenement4", Front = 4, Depth = 4, Storeys = 4, Doors = new[] { 2 } },
            new Spec { Name = "Tenement2", Front = 2, Depth = 4, Storeys = 4 },
            new Spec { Name = "Villa6", Front = 6, Depth = 6, Storeys = 3, Doors = new[] { 2 } },
            new Spec { Name = "Warehouse6", Front = 6, Depth = 6, Storeys = 3, Doors = new[] { 2, 3 }, Dormers = false },
            new Spec { Name = "Civic12", Front = 12, Depth = 8, Storeys = 3, Doors = new[] { 5, 6 } },
            new Spec { Name = "Civic16", Front = 16, Depth = 8, Storeys = 3, Doors = new[] { 7, 8 } },
            new Spec { Name = "Wing12", Front = 12, Depth = 6, Storeys = 2 },
            new Spec { Name = "Arch4", Front = 4, Depth = 3, Storeys = 4, Arch = 1 },
            new Spec { Name = "Tavern6", Front = 6, Depth = 6, Storeys = 3, Doors = new[] { 2 }, Shop = true },
            new Spec { Name = "Tower2", Front = 2, Depth = 2, Storeys = 5, Dormers = false },
        };

        /// <summary>The variants baked for each type: facade and shop colour.</summary>
        public static readonly (string Type, string Facade, string Shop)[] Variants =
        {
            ("Shop4", "brick", "green"), ("Shop4", "brick2", "red"), ("Shop4", "stucco", "blue"), ("Shop4", "grey", "green"),
            ("Shop4", "blue", "red"), ("Shop6", "stucco", "red"), ("Shop6", "brick", "blue"),
            ("Terrace4", "brick", "blue"), ("Terrace4", "stucco", "blue"), ("Terrace4", "brick2", "blue"), ("Terrace4", "grey", "blue"),
            ("Tenement4", "brick", "blue"), ("Tenement4", "brick2", "blue"), ("Tenement4", "grey", "blue"),
            ("Tenement2", "brick2", "blue"), ("Tenement2", "grey", "blue"),
            ("Villa6", "stucco", "blue"), ("Villa6", "grey", "blue"), ("Warehouse6", "warehouse", "blue"),
            ("Civic12", "stone", "blue"), ("Civic16", "stone", "blue"), ("Civic16", "marble", "blue"), ("Wing12", "marble", "blue"),
            ("Arch4", "brick", "blue"), ("Arch4", "stucco", "blue"), ("Tavern6", "brick", "green"), ("Tower2", "stone", "blue"),
        };

        public static string Key(string type, string facade) => $"House_{type}_{facade}";

        /// <summary>Bakes every variant; returns them by <see cref="Key"/>.</summary>
        public static Dictionary<string, SliceKitCatalogue.Entry> BakeAll()
        {
            var entries = new Dictionary<string, SliceKitCatalogue.Entry>();
            foreach (var (type, facade, shop) in Variants)
            {
                var source = Types.Single(t => t.Name == type);
                var sides = CornerTypes.Contains(type) ? new[] { Side.None, Side.Left, Side.Right } : new[] { Side.None };
                foreach (var side in sides)
                {
                    var key = Key(type, facade);
                    var spec = new Spec
                    {
                        Name = side == Side.None ? $"{type}_{facade}" : CornerKey($"{type}_{facade}", side), Front = source.Front,
                        Depth = source.Depth, Storeys = source.Storeys, Doors = source.Doors, Shop = source.Shop,
                        Dormers = source.Dormers, Arch = source.Arch, Faced = side,
                    };
                    var entry = Bake(spec, Facade(facade, shop, source.Shop));
                    entry.Key = side == Side.None ? key : CornerKey(key, side);
                    entries[entry.Key] = entry;
                }
            }

            AssetDatabase.SaveAssets();
            return entries;
        }

        /// <summary>The house door's middle from the footprint's west edge, or the middle of the front.</summary>
        public static float DoorX(Spec spec) => spec.Doors.Length > 0 ? (spec.Doors[0] + 0.5f) * Module : spec.Front * Module * 0.5f;

        private static GameObject Nested(GameObject root, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            child.AddComponent<KitBlock>();
            return child;
        }

        private static void Box(GameObject root, float x0, float x1, float y0, float y1, float z0, float z1)
        {
            if (x1 - x0 < 0.01f || y1 - y0 < 0.01f)
            {
                return;
            }

            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f);
            box.size = new Vector3(x1 - x0, y1 - y0, z1 - z0);
        }

        /// <summary>Batch: the Yughues pavements in a 5-wide grid, row by row in name order, from above.</summary>
        public static void RenderPavements()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var paths = AssetDatabase.FindAssets("t:Material", new[] { "Assets/YughuesFreePavementsMaterials/Materials" })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToArray();
            for (var k = 0; k < paths.Length; k++)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.transform.SetPositionAndRotation(new Vector3((k % 5) * 4.4f, 0f, -(k / 5) * 4.4f), Quaternion.Euler(90f, 0f, 0f));
                quad.transform.localScale = Vector3.one * 4f;
                quad.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(paths[k]);
                Debug.Log($"[VictorianHouses] pavement {k}: {Path.GetFileNameWithoutExtension(paths[k])}");
            }

            var camera = new GameObject("Cam").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 9.5f;
            camera.transform.SetPositionAndRotation(new Vector3(8.8f, 20f, -6.6f), Quaternion.Euler(90f, 0f, 0f));
            var target = new RenderTexture(1600, 1300, 24);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(1600, 1300, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1600, 1300), 0, 0);
            image.Apply();
            File.WriteAllBytes("Logs/SliceSurvey/pavements.png", image.EncodeToPNG());
        }

        /// <summary>Batch: the material names of an assembled house, to choose the facade swaps.</summary>
        public static void LogMaterials()
        {
            var temp = new GameObject("assembly");
            Assemble(temp.transform, new Spec { Name = "Probe", Front = 4, Depth = 4, Storeys = 2, Doors = new[] { 1 }, Shop = true });
            var names = temp.GetComponentsInChildren<MeshRenderer>().SelectMany(r => r.sharedMaterials).Where(m => m != null)
                .GroupBy(m => m.name).Select(g => $"{g.Key} x{g.Count()}");
            Debug.Log("[VictorianHouses] materials: " + string.Join(", ", names));
            Object.DestroyImmediate(temp);
        }

        /// <summary>Batch: each roof module in a row, a red post on its +Z side, rendered from +X+Z and from −X.</summary>
        public static void RenderRoofModules()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var names = new[] { "Roof", "RoofCorner", "RoofUp", "RoofWall", "Roof Window", "GroundCornelWall1", "GroundSideWall1" };
            for (var k = 0; k < names.Length; k++)
            {
                var holder = new GameObject(names[k]);
                holder.transform.position = new Vector3(k * 5f, 0f, 0f);
                Put(holder.transform, Modules + names[k], Vector3.zero, 0f);
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.transform.position = new Vector3(k * 5f, 0.5f, 2.5f);
                post.transform.localScale = new Vector3(0.3f, 1f, 0.3f);
                post.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = Color.red };
            }

            var light = Object.FindFirstObjectByType<Light>();
            light.transform.rotation = Quaternion.Euler(50f, 30f, 0f);
            foreach (var (n, d) in new[] { ("pp", new Vector3(1f, 0.7f, 1f)), ("np", new Vector3(-1f, 0.7f, 1f)), ("nn", new Vector3(-1f, 0.7f, -1f)), ("pn", new Vector3(1f, 0.7f, -1f)) })
            {
                Shot("roofcorner_" + n, new Vector3(5f, 1.5f, 0f), d, 7f);
            }
        }

        private static void Shot(string name, Vector3 centre, Vector3 dir, float distance)
        {
            var camera = new GameObject("Cam").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.62f, 0.74f, 0.86f);
            camera.fieldOfView = 50f;
            camera.transform.position = centre + (dir.normalized * distance);
            camera.transform.LookAt(centre);
            var target = new RenderTexture(1400, 800, 24);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(1400, 800, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1400, 800), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Logs/SliceSurvey");
            File.WriteAllBytes($"Logs/SliceSurvey/{name}.png", image.EncodeToPNG());
            RenderTexture.active = null;
            Object.DestroyImmediate(camera.gameObject);
        }

        /// <summary>Batch: assembles a sample house and renders it from four sides into Logs/SliceSurvey/house_*.png.</summary>
        public static void RenderSample()
        {
            var entries = BakeAll();
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var x = 0f;
            foreach (var e in entries.Values.Where(e => !e.Key.Contains("Civic") && !e.Key.Contains("Wing")).Take(14))
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(e.Prefab);
                go.transform.position = new Vector3(x, 0f, 0f);
                x += e.Width;
            }

            var light = Object.FindFirstObjectByType<Light>();
            light.transform.rotation = Quaternion.Euler(50f, 30f, 0f);
            var c = new Vector3(x * 0.5f, 8f, 5f);
            foreach (var (name, dir, dist) in new[] { ("street", new Vector3(0f, 0.12f, 1f), 70f), ("high", new Vector3(0.3f, 0.8f, 1f), 90f), ("eye", new Vector3(0.8f, 0.02f, 0.25f), 40f), ("back", new Vector3(0.2f, 0.5f, -1f), 80f) })
            {
                var camera = new GameObject("Cam").AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.62f, 0.74f, 0.86f);
                camera.fieldOfView = 50f;
                camera.transform.position = c + (dir.normalized * dist);
                camera.transform.LookAt(c);
                var target = new RenderTexture(1600, 800, 24);
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                var image = new Texture2D(1600, 800, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1600, 800), 0, 0);
                image.Apply();
                Directory.CreateDirectory("Logs/SliceSurvey");
                File.WriteAllBytes($"Logs/SliceSurvey/house_{name}.png", image.EncodeToPNG());
                RenderTexture.active = null;
                Object.DestroyImmediate(camera.gameObject);
            }
        }
    }
}

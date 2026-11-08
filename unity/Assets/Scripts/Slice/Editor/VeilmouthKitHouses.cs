using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameDev.Slice.Kit;
using GameDev.VictorianKit.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Veilmouth's houses from the owner's Victorian Kit at ×1.25 (VEILMOUTH_CITY_PLAN.md §24.4): each type of
    /// <see cref="VicCityHouses"/> baked once as a kit block prefab whose modules stay prefab instances, with its rooms
    /// behind the windows, the kit standard's body, front and stoop colliders, and a <c>Door</c> marker at each street
    /// door, so the city places them as it placed C2's houses (<see cref="VictorianHouses"/>).
    /// <para>
    /// The block's footprint is (0, 0)–(Width, Depth) with its front on +Z = Depth, as C2's. The kit house stands under
    /// it turned 180° and scaled 1.25: the kit point (x, y, z) is the block's (Width − 1.25x, 1.25z, Depth − 1.25y), so
    /// the kit's right end is the block's local −X, which C2 calls its Left side.
    /// </para>
    /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthKitHouses.BakeAllBatch
    /// </summary>
    public static class VeilmouthKitHouses
    {
        public const float Scale = 1.25f;
        public const string Folder = SliceKitCatalogue.Folder + "/KitHouses";

        /// <summary>The marker of a door that opens off the street front, the Soot &amp; Bell's to its yard.</summary>
        public const string YardDoorMarker = "YardDoor";

        /// <summary>How far the bands, trims and frames stand proud of the front, m: the front strip's depth (kit 0.1 m and more).</summary>
        private const float Proud = 0.16f;

        /// <summary>The steps' risers the stoop's colliders follow (the kit's steps: 0.6 m up over 1.1 m, at ×1).</summary>
        private const int StepCount = 4;

        /// <summary>The kit steps' riser and tread in the city, m (measured: 0.6 m up over 1.1 m in four steps at ×1), which the city's steps continue.</summary>
        public const float StepRise = Scale * 0.6f / StepCount, StepTread = Scale * 1.1f / StepCount;

        /// <summary>Beyond this share of the screen a house shows only its walls, openings and roofs, not its trims and pipes.</summary>
        private const float DetailScreen = 0.15f;

        private static readonly string[] Details =
        {
            "SM_Vic_Trim_", "SM_Vic_Rain_", "SM_Vic_ChimneyPot_", "SM_Vic_Rail_", "SM_Vic_Quoin_", "SM_Vic_Bracket", "SM_Vic_Roof_Flashing",
            "SM_Vic_Roof_Soffit", "SM_Vic_WeatherVane", "SM_Vic_Roof_MansardEdge", "SM_Vic_Pilaster",
        };

        private delegate VicHouseSpec RowMaker(string name, int width, string palette, VicEnd left, VicEnd right);

        /// <summary>
        /// The row types: each in its palettes, as a house in the middle of a row, with its kit right or left end faced,
        /// and with both. Types of one family share height, depth and roof, so a row may mix them.
        /// </summary>
        private static readonly (string Type, int Width, string[] Palettes, RowMaker Make)[] RowTypes =
        {
            ("Shop8", 8, new[] { "stucco", "brick" }, VicCityHouses.Shop),
            ("Shop12", 12, new[] { "rich" }, VicCityHouses.Shop),
            ("Terrace8", 8, new[] { "brick", "stock" }, (n, w, p, l, r) => VicCityHouses.Town(n, w, p, l, r, false)),
            ("ShopTown8", 8, new[] { "dark" }, (n, w, p, l, r) => VicCityHouses.Town(n, w, p, l, r, true)),
            ("Tenement8", 8, new[] { "poor", "brick" }, (n, w, p, l, r) => VicCityHouses.Tenement(n, w, 3, p, l, r)),
            ("Tenement12", 12, new[] { "dark" }, (n, w, p, l, r) => VicCityHouses.Tenement(n, w, 3, p, l, r)),
            ("TenementLow8", 8, new[] { "poor" }, (n, w, p, l, r) => VicCityHouses.Tenement(n, w, 2, p, l, r)),
            ("Warehouse12", 12, new[] { "stock" }, VicCityHouses.Warehouse),
        };

        /// <summary>Every type the city uses, by its key: rows with their corners, detached houses and the single buildings.</summary>
        public static IEnumerable<(string Key, Func<string, VicHouseSpec> Make)> Types()
        {
            foreach (var (type, width, palettes, make) in RowTypes)
            {
                foreach (var palette in palettes)
                {
                    var key = VictorianHouses.Key(type, palette);
                    var (t, w, p) = (type, width, palette);
                    yield return (key, n => make(n, w, p, VicEnd.Open, VicEnd.Open));
                    // C2's Left is the block's local −X: the kit's right end.
                    yield return (VictorianHouses.CornerKey(key, VictorianHouses.Side.Left), n => make(n, w, p, VicEnd.Open, VicEnd.Faced));
                    yield return (VictorianHouses.CornerKey(key, VictorianHouses.Side.Right), n => make(n, w, p, VicEnd.Faced, VicEnd.Open));
                    // Both ends faced: a house of a row that stands alone in its span (Harrow & Tincture, the missing
                    // person's tenement, a lone house between yard walls).
                    yield return (VictorianHouses.CornerKey(key, VictorianHouses.Side.Both), n => make(n, w, p, VicEnd.Faced, VicEnd.Faced));
                }
            }

            // Detached: the Terraces' houses and the gate lodge.
            foreach (var palette in new[] { "white", "rich" })
            {
                var p = palette;
                yield return (VictorianHouses.CornerKey(VictorianHouses.Key("Terrace8", p), VictorianHouses.Side.Both), n => VicCityHouses.Detached(n, p));
            }

            yield return (VictorianHouses.Key("Villa", "stucco"), n => VicCityHouses.Villa(n, "stucco"));
            yield return (VictorianHouses.Key("Villa", "white"), n => VicCityHouses.Villa(n, "white"));
            yield return (VictorianHouses.Key("Civic", "white"), n => VicCityHouses.Civic(n, "white"));
            yield return (VictorianHouses.Key("Civic", "rich"), n => VicCityHouses.Civic(n, "rich"));
            yield return (VictorianHouses.Key("Civic", "stucco"), n => VicCityHouses.Civic(n, "stucco"));
            yield return (VictorianHouses.Key("Palace", "white"), n => VicCityHouses.Civic(n, "white", 32, 12));
            yield return (VictorianHouses.Key("Tavern", "brick"), n => VicCityHouses.Tavern(n, "brick"));
            yield return (VictorianHouses.Key("Tower6", "white"), n => VicCityHouses.Tower(n, 6, "white", true));
            yield return (VictorianHouses.Key("Tower5", "dark"), n => VicCityHouses.Tower(n, 5, "dark", false));
        }

        /// <summary>The row keys of a family for the city's palettes: the plain (middle) keys of the given types.</summary>
        public static string[] Keys(params string[] types) =>
            RowTypes.Where(t => types.Contains(t.Type)).SelectMany(t => t.Palettes.Select(p => VictorianHouses.Key(t.Type, p))).ToArray();

        public static void BakeAllBatch()
        {
            var entries = BakeAll();
            Debug.Log($"[VeilmouthKitHouses] baked {entries.Count} types.");
        }

        /// <summary>A few types, for a look and for the prefabs' size, without the whole set.</summary>
        public static void BakeProbe()
        {
            var keys = new[]
            {
                "House_Shop8_stucco", "House_Shop8_stucco_cornerR", "House_Tenement8_poor_cornerL", "House_Terrace8_white_detached",
                "House_Civic_white", "House_Tower6_white",
            };
            var entries = BakeAll(keys.Contains);
            foreach (var e in entries.Values)
            {
                Debug.Log($"[VeilmouthKitHouses] {e.Key}: {e.Width} x {e.Depth} m, {e.Height} m high, door at {e.DoorX:0.00}, " +
                          $"{new FileInfo(e.Path).Length / 1024} KB");
            }
        }

        /// <summary>
        /// Stills of the baked types in an empty scene, written to Logs/VictorianKit: two houses of a row joined at their open
        /// ends, and the single types beside them.
        /// </summary>
        public static void ProbeShots()
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(38f, 142f, 0f);
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.6f, 0.7f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.44f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.23f, 0.21f);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(40f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(200f, 1f, 120f);

            var entries = Load();
            void Put(string key, float x, float z = 0f)
            {
                if (entries.TryGetValue(key, out var e))
                {
                    var house = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(e.Path));
                    house.transform.position = new Vector3(x, 0f, z);
                }
            }

            Put("House_Shop8_stucco", 0f);
            Put("House_Shop8_stucco_cornerR", 10f);
            Put("House_Tenement8_poor_cornerL", -24f);
            Put("House_Terrace8_white_detached", 28f);
            Put("House_Civic_white", 46f);
            Put("House_Tower6_white", 84f);

            VicShots.Eye("KitHouses_Join_Front", new Vector3(4f, 7f, 32f), new Vector3(12f, 9f, 10f));
            VicShots.Eye("KitHouses_Join_Roof", new Vector3(10f, 34f, 24f), new Vector3(10f, 15f, 4f));
            VicShots.Eye("KitHouses_Join_Back", new Vector3(14f, 9f, -22f), new Vector3(10f, 9f, 4f));
            VicShots.Eye("KitHouses_Stoop", new Vector3(8f, 2.5f, 17f), new Vector3(5f, 1.2f, 10f));
            VicShots.Eye("KitHouses_All", new Vector3(30f, 34f, 78f), new Vector3(36f, 6f, 4f), 60f);
            VicShots.Eye("KitHouses_Civic", new Vector3(58f, 8f, 48f), new Vector3(62f, 8f, 12f));
            VicShots.Eye("KitHouses_Back", new Vector3(36f, 26f, -50f), new Vector3(36f, 8f, 6f), 60f);
        }

        /// <summary>Bakes every type (or those <paramref name="only"/> keeps) in place: a prefab keeps its path and guid.</summary>
        public static Dictionary<string, SliceKitCatalogue.Entry> BakeAll(Func<string, bool> only = null)
        {
            VicKitPaths.EnsureKitPrefabs();
            VicTextures.EnsureAll();
            VicMaterials.BuildAll();
            KitStandIns.EnsureFolder(Folder);
            var entries = new Dictionary<string, SliceKitCatalogue.Entry>();
            foreach (var (key, make) in Types())
            {
                if (only != null && !only(key))
                {
                    continue;
                }

                entries[key] = Bake(key, make(key));
            }

            AssetDatabase.SaveAssets();
            return entries;
        }

        /// <summary>The prefabs already baked, by key, read from their names without baking again.</summary>
        public static Dictionary<string, SliceKitCatalogue.Entry> Load()
        {
            var entries = new Dictionary<string, SliceKitCatalogue.Entry>();
            foreach (var (key, make) in Types())
            {
                var path = Baked(PrefabStem(key)).FirstOrDefault();
                if (path == null)
                {
                    continue;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    entries[key] = Read(key, path, prefab);
                }
            }

            return entries;
        }

        private static string PrefabStem(string key) => "Kit_House_" + key.Substring("House_".Length);

        /// <summary>The prefabs baked for a stem, whatever their size: the stem and "_{width}x{height}" only, never a longer key's.</summary>
        private static IEnumerable<string> Baked(string stem) =>
            !Directory.Exists(Folder)
                ? Enumerable.Empty<string>()
                : Directory.GetFiles(Folder, stem + "_*.prefab")
                    .Select(p => p.Replace('\\', '/'))
                    .Where(p => System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileNameWithoutExtension(p).Substring(stem.Length), @"^_\d+x\d+$"));

        // ------------------------------------------------------------------------------------------------ baking

        public static SliceKitCatalogue.Entry Bake(string key, VicHouseSpec spec)
        {
            var width = Mathf.RoundToInt(spec.Width * Scale);
            if (Mathf.Abs((spec.Width * Scale) - width) > 0.001f)
            {
                throw new ArgumentException($"{key}: {spec.Width} kit metres are not whole metres at ×{Scale}.");
            }

            var depth = Mathf.CeilToInt((spec.Depth * Scale) - 0.001f);
            var height = Mathf.RoundToInt((float)spec.Ridge * Scale);
            var stem = PrefabStem(key);
            var path = $"{Folder}/{stem}_{width}x{height}.prefab";
            foreach (var stale in Baked(stem).Where(p => p != path).ToList())
            {
                AssetDatabase.DeleteAsset(stale);
            }

            Vector3 Root(double x, double y, double z) => new Vector3(width - (Scale * (float)x), Scale * (float)z, depth - (Scale * (float)y));

            var root = new GameObject(Path.GetFileNameWithoutExtension(path));
            root.AddComponent<KitBlock>();
            var model = VicHouseFactory.Build("Model", spec.House.Pieces, spec.Palette.For);
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = new Vector3(width, 0f, depth);
            model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            model.transform.localScale = Vector3.one * Scale;
            Dress(model);
            VicRooms.Build(spec, model.transform, Scale);

            // The body: the plan's cells as boxes from the ground to the wall's top.
            var top = Scale * (float)VicKit.WallTop(spec.Floors);
            foreach (var (i0, i1, j0, j1) in Boxes(spec.Plan))
            {
                var a = Root(i1 * 2, j1 * 2, 0);
                var b = Root(i0 * 2, j0 * 2, 0);
                Box(root, a.x, b.x, 0f, top, a.z, b.z);
            }

            // The front strip over the bands, frames and trims of the frontmost front, short of a faced corner by its depth.
            var pieces = spec.House.Pieces;
            var frontY = spec.Plan.MinY;
            var front = Nested(root, "Front");
            foreach (var e in spec.Plan.Edges.Where(e => e.Side == "front" && e.Y0 == frontY))
            {
                bool Corner(int x) => pieces.Any(p => p.Id == "SM_Vic_Wall_Corner_Out_1m" && Math.Abs(p.X - x) < 0.01 && Math.Abs(p.Y - frontY) < 0.01);
                var a = Root(e.X1, frontY, 0);
                var b = Root(e.X0, frontY, 0);
                Box(front, a.x + (Corner(e.X1) ? Proud : 0f), b.x - (Corner(e.X0) ? Proud : 0f), 0f, top, depth, depth + Proud);
            }

            // What stands out from the walls at a walker's height: each door's steps, the bay windows, the porch columns.
            var stoopWidth = 0f;
            foreach (var p in pieces)
            {
                var kind = p.Id.StartsWith("SM_Vic_Steps_") ? "Stoop" : p.Id == "SM_Vic_Bay_Base" ? "Bay" : p.Id == "SM_Vic_Column_3m" ? "Column" : null;
                if (kind == null)
                {
                    continue;
                }

                var (lo, hi) = Footprint(p, Root);
                var rot = p.Yaw * Math.PI / 180.0;
                var (ox, oy) = VicKit.Rotated(0.0, -1.0, rot);
                var outward = new Vector3((float)-ox, 0f, (float)-oy); // the kit's (dx, dy) is the block's (−dx, −dy)
                var onFront = Mathf.Abs(outward.z - 1f) < 0.01f && Mathf.Abs(hi.z - lo.z) > 0f && lo.z >= depth - 0.01f;
                var block = Nested(root, kind);
                if (kind == "Column")
                {
                    Box(block, lo.x, hi.x, 0f, Scale * 3f, lo.z, hi.z);
                    continue;
                }

                var near = onFront ? depth + Proud : 0f;
                if (kind == "Bay")
                {
                    // A bay's base reaches back into the wall: its box starts at the front's face, never inside the front's box.
                    var from = outward.z > 0.99f && hi.z > depth + Proud ? Mathf.Max(lo.z, depth + Proud) : Mathf.Max(lo.z, near);
                    Box(block, lo.x, hi.x, 0f, Scale * (float)VicKit.FloorZ(1), from, hi.z);
                    continue;
                }

                if (stoopWidth == 0f && onFront)
                {
                    stoopWidth = Mathf.Abs(outward.z) > 0.5f ? hi.x - lo.x : hi.z - lo.z;
                }

                var rise = Scale * (float)VicKit.Foundation;
                for (var k = 1; k <= StepCount; k++)
                {
                    var share = (StepCount + 1f - k) / StepCount;
                    var y = rise * k / StepCount;
                    if (Mathf.Abs(outward.z) > 0.5f)
                    {
                        var wall = outward.z > 0f ? lo.z : hi.z;
                        var far = wall + (outward.z * (hi.z - lo.z) * share);
                        var start = onFront ? near : wall;
                        Box(block, lo.x, hi.x, 0f, y, Mathf.Min(start, far), Mathf.Max(start, far));
                    }
                    else
                    {
                        var wall = outward.x > 0f ? lo.x : hi.x;
                        var far = wall + (outward.x * (hi.x - lo.x) * share);
                        Box(block, Mathf.Min(wall, far), Mathf.Max(wall, far), 0f, y, lo.z, hi.z);
                    }
                }
            }

            // A marker in each doorway at its sill, facing out: a street door on the frontmost front, any other a yard door.
            var doors = new List<float>();
            foreach (var p in pieces.Where(p => p.Id.StartsWith("SM_Vic_Wall_Door_") && Math.Abs(p.Z - VicKit.FloorZ(0)) < 0.01))
            {
                var rot = p.Yaw * Math.PI / 180.0;
                var (cx, cy) = VicKit.Rotated(1.0, 0.0, rot);
                var (ox, oy) = VicKit.Rotated(0.0, -1.0, rot);
                var street = Math.Abs(p.NormalYaw) < 0.01 && Math.Abs(p.Y - frontY) < 0.01;
                var marker = new GameObject(street ? VictorianHouses.DoorMarker : YardDoorMarker);
                marker.transform.SetParent(root.transform, false);
                marker.transform.localPosition = Root(p.X + cx, p.Y + cy, VicKit.FloorZ(0));
                marker.transform.localRotation = Quaternion.LookRotation(new Vector3((float)-ox, 0f, (float)-oy));
                if (street)
                {
                    doors.Add(marker.transform.localPosition.x);
                }
            }

            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = LayerMask.NameToLayer(KitLayers.Structure);
            }

            GameObjectUtility.SetStaticEditorFlags(root, StaticEditorFlags.BatchingStatic);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            var entry = Read(key, path, AssetDatabase.LoadAssetAtPath<GameObject>(path));
            entry.StoopWidth = stoopWidth;
            return entry;
        }

        /// <summary>An entry from a baked prefab: its size from its name and its first street door's marker.</summary>
        private static SliceKitCatalogue.Entry Read(string key, string path, GameObject prefab)
        {
            var size = Path.GetFileNameWithoutExtension(path).Split('_').Last().Split('x');
            var body = prefab.GetComponents<BoxCollider>();
            var depth = Mathf.RoundToInt(body.Length > 0 ? body.Max(b => b.center.z + (b.size.z * 0.5f)) : 0f);
            var door = prefab.transform.Cast<Transform>().FirstOrDefault(t => t.name == VictorianHouses.DoorMarker);
            var stoop = prefab.transform.Cast<Transform>().Where(t => t.name == "Stoop").SelectMany(t => t.GetComponents<BoxCollider>()).ToList();
            return new SliceKitCatalogue.Entry
            {
                Key = key,
                Path = path,
                Width = int.Parse(size[0]),
                Depth = depth,
                Height = int.Parse(size[1]),
                DoorX = door != null ? door.localPosition.x : int.Parse(size[0]) * 0.5f,
                Stoop = door != null && stoop.Count > 0 ? stoop.Max(b => b.center.z + (b.size.z * 0.5f)) - depth : 0f,
                StoopWidth = stoop.Count > 0 ? stoop.Max(b => b.size.x) : 0f,
            };
        }

        /// <summary>
        /// Static for batching, occlusion and probes, lit by light probes (Veilmouth has no lightmaps); one LOD group that
        /// drops the trims, pipes, pots and rails beyond <see cref="DetailScreen"/> and keeps walls, openings and roofs.
        /// </summary>
        private static void Dress(GameObject model)
        {
            const StaticEditorFlags flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic |
                                            StaticEditorFlags.ReflectionProbeStatic;
            var all = new List<Renderer>();
            var kept = new List<Renderer>();
            foreach (Transform piece in model.transform)
            {
                var id = piece.name.Substring(0, piece.name.LastIndexOf('_'));
                var detail = Details.Any(id.StartsWith);
                foreach (var t in piece.GetComponentsInChildren<Transform>(true))
                {
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
                }

                foreach (var r in piece.GetComponentsInChildren<MeshRenderer>(true))
                {
                    r.receiveGI = ReceiveGI.LightProbes;
                    all.Add(r);
                    if (!detail)
                    {
                        kept.Add(r);
                    }
                }
            }

            var lod = model.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(DetailScreen, all.ToArray()), new LOD(0.004f, kept.ToArray()) });
            lod.RecalculateBounds();
        }

        /// <summary>A placed piece's footprint in the block, from its module's own footprint (lowest and highest corners).</summary>
        private static (Vector3 Lo, Vector3 Hi) Footprint(VicPiece p, Func<double, double, double, Vector3> root)
        {
            var (x0, x1, y0, y1) = VicCityHouses.Extent(p.Id);
            var rot = p.Yaw * Math.PI / 180.0;
            var lo = new Vector3(float.MaxValue, 0f, float.MaxValue);
            var hi = new Vector3(float.MinValue, 0f, float.MinValue);
            foreach (var (cx, cy) in new[] { (x0, y0), (x1, y0), (x1, y1), (x0, y1) })
            {
                var (rx, ry) = VicKit.Rotated(cx, cy, rot);
                var w = root(p.X + rx, p.Y + ry, 0.0);
                lo = Vector3.Min(lo, w);
                hi = Vector3.Max(hi, w);
            }

            return (lo, hi);
        }

        /// <summary>The plan's cells merged into rectangles: runs along X per row, joined across rows that repeat them.</summary>
        private static List<(int I0, int I1, int J0, int J1)> Boxes(VicPlan plan)
        {
            var boxes = new List<(int I0, int I1, int J0, int J1)>();
            var open = new List<(int I0, int I1, int J0)>();
            var rows = plan.Cells.GroupBy(c => c.j).OrderBy(g => g.Key).ToList();
            var last = int.MinValue;
            foreach (var row in rows)
            {
                var runs = new List<(int, int)>();
                var cells = row.Select(c => c.i).OrderBy(i => i).ToList();
                var start = cells[0];
                for (var k = 1; k <= cells.Count; k++)
                {
                    if (k == cells.Count || cells[k] != cells[k - 1] + 1)
                    {
                        runs.Add((start, cells[k - 1] + 1));
                        if (k < cells.Count)
                        {
                            start = cells[k];
                        }
                    }
                }

                var next = new List<(int I0, int I1, int J0)>();
                foreach (var o in open)
                {
                    if (row.Key == last + 1 && runs.Contains((o.I0, o.I1)))
                    {
                        next.Add(o);
                        runs.Remove((o.I0, o.I1));
                    }
                    else
                    {
                        boxes.Add((o.I0, o.I1, o.J0, last + 1));
                    }
                }

                next.AddRange(runs.Select(r => (r.Item1, r.Item2, row.Key)));
                open = next;
                last = row.Key;
            }

            boxes.AddRange(open.Select(o => (o.I0, o.I1, o.J0, last + 1)));
            return boxes;
        }

        private static GameObject Nested(GameObject root, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            child.AddComponent<KitBlock>();
            return child;
        }

        private static void Box(GameObject owner, float x0, float x1, float y0, float y1, float z0, float z1)
        {
            if (x1 - x0 < 0.01f || y1 - y0 < 0.01f || z1 - z0 < 0.01f)
            {
                return;
            }

            var box = owner.AddComponent<BoxCollider>();
            box.center = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f);
            box.size = new Vector3(x1 - x0, y1 - y0, z1 - z0);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Last Turning Cemetery's stones (VEILMOUTH_CITY_PLAN.md §40; SLICE_MODELS_WANTED.md § C4, "Gravestones, crosses and a
    /// tomb or two", whose placeholder is plain stone slabs). The Colonial pack's grave, a small roofed tomb, stood on
    /// every grave alike in a strict grid, and read as a toy. Each grave keeps its kit root, its block and its collider,
    /// so the layout and the Combat Grid read the cemetery as before. Three graves keep the pack's tomb, the "tomb or two";
    /// on the rest the pack's model is switched off and a Victorian stone stands in its cell instead: a round-headed or a
    /// pointed headstone, a cross on a stepped base, or an obelisk, in three weathered stones, leaning a little with age,
    /// most of the headstones and crosses with a low kerb round the plot before them. One merged mesh for each stone,
    /// with no collider of its own. Every size is an authoring default, in metres, within the wanted list's 0.6–2.5 m.
    /// </summary>
    public static partial class VeilmouthCity
    {
        private const string GravesDressName = "Last Turning's stones", GravePrefab = "Kit_Chapel_Grave";
        private static readonly string[] GraveStones = { "VIC_Stone_Dirty", "VIC_Stone_Medium", "VIC_Stone_Light" };
        private const int KeptTombs = 3;

        // The plot's kerb: its inner size, its stone's width and height above the grass; and how deep every stone is sunk.
        private const float PlotWidth = 0.8f, PlotLength = 1.8f, PlotKerb = 0.09f, PlotKerbHeight = 0.12f, Sunk = 0.12f;

        /// <summary>
        /// Gives the saved Veilmouth scene's cemetery its stones again, and saves the scene.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthCity.DressGraves
        /// </summary>
        public static void DressGraves()
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var city = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            if (city == null)
            {
                Debug.LogError($"[VeilmouthCity] no {RootName} in the scene.");
                return;
            }

            GravesDressOf(city.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>Builds the cemetery's stones under <paramref name="root"/>, replacing older ones.</summary>
        private static void GravesDressOf(Transform root)
        {
            var old = root.Find(GravesDressName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var graves = root.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith(GravePrefab) && t.GetComponent<BoxCollider>() != null && t.Find("Model") != null)
                .OrderBy(t => Hash(t.position))
                .ToList();
            if (graves.Count == 0)
            {
                Debug.Log("[VeilmouthCity] the cemetery's stones: no graves under the city.");
                return;
            }

            var owner = new GameObject(GravesDressName).transform;
            owner.SetParent(root, false);
            var stones = new Works(owner, false);
            var terrain = Object.FindFirstObjectByType<Terrain>();
            var kinds = new int[4];
            for (var k = 0; k < graves.Count; k++)
            {
                var grave = graves[k];
                var model = grave.Find("Model").gameObject;
                // The graves are ordered by their hash, so the kept tombs fall anywhere in the rows.
                var tomb = k < KeptTombs;
                model.SetActive(tomb);
                PrefabUtility.RecordPrefabInstancePropertyModifications(model);
                if (tomb)
                {
                    continue;
                }

                var box = grave.GetComponent<BoxCollider>();
                var cell = grave.TransformPoint(box.center);
                kinds[Gravestone(stones, grave, cell, terrain)]++;
            }

            stones.Finish(GraveStones[0]);
            Debug.Log($"[VeilmouthCity] the cemetery's stones: {graves.Count} graves, {KeptTombs} tombs kept, " +
                      $"{kinds[0]} round-headed and {kinds[1]} pointed headstones, {kinds[2]} crosses, {kinds[3]} obelisks.");
        }

        /// <summary>One grave's stone in its cell at <paramref name="cell"/>; returns its kind (0 round, 1 pointed, 2 cross, 3 obelisk).</summary>
        private static int Gravestone(Works stones, Transform grave, Vector3 cell, Terrain terrain)
        {
            var hash = Hash(cell);
            float R(int salt) => ((hash * (salt * 2654435761u + 97u)) >> 8 & 0xffff) / 65535f;

            // The stone faces the way the grave faces, and its plot lies before it.
            var facing = grave.forward;
            facing.y = 0f;
            facing.Normalize();
            var right = Vector3.Cross(Vector3.up, facing);
            var foot = cell + (right * ((R(1) - 0.5f) * 0.2f)) - (facing * (0.25f + (R(2) * 0.1f)));
            foot.y = GroundAt(terrain, foot, grave.position.y - 0.15f) - Sunk;
            var yaw = Quaternion.LookRotation(facing) * Quaternion.Euler(0f, (R(3) - 0.5f) * 8f, 0f);
            // Age: a lean of up to 5° forward or back and 3° to a side, about the stone's foot.
            var lean = Quaternion.Euler((R(4) - 0.5f) * 10f, 0f, (R(5) - 0.5f) * 6f);
            var m = Matrix4x4.TRS(foot, yaw * lean, Vector3.one);
            var r = R(6);
            var stone = GraveStones[r < 0.5f ? 0 : r < 0.8f ? 1 : 2];
            var pick = R(7);
            var kind = pick < 0.36f ? 0 : pick < 0.56f ? 1 : pick < 0.82f ? 2 : 3;
            switch (kind)
            {
                case 0: // a round-headed slab on a low plinth
                {
                    var w = 0.55f + (R(8) * 0.2f);
                    var h = 0.75f + (R(9) * 0.4f);
                    Block(stones, m, new Vector3(0f, 0.06f + Sunk / 2f, 0f), new Vector3(w + 0.12f, 0.12f + Sunk, 0.3f), stone);
                    var outline = new List<Vector2> { new Vector2(w / 2f, 0f), new Vector2(w / 2f, h - (w / 2f)) };
                    for (var a = 1; a < 8; a++)
                    {
                        var t = a * Mathf.PI / 8f;
                        outline.Add(new Vector2(Mathf.Cos(t) * w / 2f, h - (w / 2f) + (Mathf.Sin(t) * w / 2f)));
                    }

                    outline.Add(new Vector2(-w / 2f, h - (w / 2f)));
                    outline.Add(new Vector2(-w / 2f, 0f));
                    Prism(stones, m, outline, 0.1f + Sunk, -0.065f, 0.065f, stone);
                    break;
                }
                case 1: // a pointed slab, its shoulders cut
                {
                    var w = 0.55f + (R(8) * 0.15f);
                    var h = 0.8f + (R(9) * 0.25f);
                    var outline = new List<Vector2>
                    {
                        new Vector2(w / 2f, 0f), new Vector2(w / 2f, h), new Vector2(w * 0.32f, h + 0.1f), new Vector2(0f, h + 0.22f + (R(10) * 0.1f)),
                        new Vector2(-w * 0.32f, h + 0.1f), new Vector2(-w / 2f, h), new Vector2(-w / 2f, 0f),
                    };
                    Prism(stones, m, outline, 0f, -0.07f, 0.07f, stone);
                    break;
                }
                case 2: // a cross on two steps
                {
                    var top = 1.45f + (R(8) * 0.4f);
                    Block(stones, m, new Vector3(0f, 0.11f + Sunk / 2f, 0f), new Vector3(0.6f, 0.22f + Sunk, 0.45f), stone);
                    Block(stones, m, new Vector3(0f, Sunk + 0.31f, 0f), new Vector3(0.42f, 0.18f, 0.32f), stone);
                    Block(stones, m, new Vector3(0f, Sunk + ((0.4f + top) / 2f), 0f), new Vector3(0.13f, top - 0.4f, 0.11f), stone);
                    var arm = 0.5f + (R(9) * 0.12f);
                    Block(stones, m, new Vector3(0f, Sunk + top - 0.33f, 0f), new Vector3(arm, 0.12f, 0.11f), stone);
                    break;
                }
                default: // an obelisk on a base and a die
                {
                    var shaft = 1.15f + (R(8) * 0.3f);
                    Block(stones, m, new Vector3(0f, 0.14f + Sunk / 2f, 0f), new Vector3(0.7f, 0.28f + Sunk, 0.7f), stone);
                    Block(stones, m, new Vector3(0f, Sunk + 0.28f + 0.03f, 0f), new Vector3(0.58f, 0.06f, 0.58f), stone);
                    Block(stones, m, new Vector3(0f, Sunk + 0.34f + 0.25f, 0f), new Vector3(0.5f, 0.5f, 0.5f), stone);
                    var y0 = Sunk + 0.84f;
                    Taper(stones, m, y0, 0.17f, y0 + shaft, 0.11f, y0 + shaft + 0.22f, stone);
                    return kind;
                }
            }

            // Most plots before a headstone or a cross have a low kerb. It does not lean with the stone, and it is set on
            // the ground at the plot's middle, so it neither floats nor sinks over its length.
            if (R(11) < 0.65f)
            {
                var near = 0.3f;
                var mid = near + (PlotLength / 2f);
                var middle = foot + (yaw * new Vector3(0f, 0f, mid));
                var kerb = Matrix4x4.TRS(new Vector3(foot.x, GroundAt(terrain, middle, foot.y + Sunk) - Sunk, foot.z), yaw, Vector3.one);
                var tall = PlotKerbHeight + Sunk;
                var x = (PlotWidth + PlotKerb) / 2f;
                Block(stones, kerb, new Vector3(x, tall / 2f, mid), new Vector3(PlotKerb, tall, PlotLength + (PlotKerb * 2f)), stone);
                Block(stones, kerb, new Vector3(-x, tall / 2f, mid), new Vector3(PlotKerb, tall, PlotLength + (PlotKerb * 2f)), stone);
                Block(stones, kerb, new Vector3(0f, tall / 2f, near + PlotLength + (PlotKerb / 2f)), new Vector3(PlotWidth, tall, PlotKerb), stone);
            }

            return kind;
        }

        /// <summary>The ground's height under <paramref name="p"/>, else <paramref name="fallback"/>.</summary>
        private static float GroundAt(Terrain terrain, Vector3 p, float fallback) =>
            terrain != null ? terrain.SampleHeight(p) + terrain.transform.position.y : fallback;

        /// <summary>A stable hash of a point to a tenth of a metre, for the stones' choices.</summary>
        private static uint Hash(Vector3 p) =>
            (uint)(((int)Mathf.Round(p.x * 10f) * 73856093) ^ ((int)Mathf.Round(p.z * 10f) * 19349663)) * 2246822519u + 374761393u;

        /// <summary>A box of <paramref name="size"/> about <paramref name="centre"/> in the frame <paramref name="m"/>.</summary>
        private static void Block(Works works, Matrix4x4 m, Vector3 centre, Vector3 size, string material)
        {
            var h = size / 2f;
            var outline = new List<Vector2> { new Vector2(h.x, -h.y), new Vector2(h.x, h.y), new Vector2(-h.x, h.y), new Vector2(-h.x, -h.y) };
            Prism(works, m * Matrix4x4.Translate(new Vector3(centre.x, centre.y, 0f)), outline, 0f, centre.z - h.z, centre.z + h.z, material);
        }

        /// <summary>
        /// A convex outline in the frame's x (right) and y (up), raised by <paramref name="lift"/> and drawn out along its
        /// z (front) from <paramref name="z0"/> to <paramref name="z1"/>: its two faces and its sides.
        /// </summary>
        private static void Prism(Works works, Matrix4x4 m, IReadOnlyList<Vector2> outline, float lift, float z0, float z1, string material)
        {
            var front = outline.Select(p => m.MultiplyPoint3x4(new Vector3(p.x, p.y + lift, z1))).ToList();
            var back = outline.Select(p => m.MultiplyPoint3x4(new Vector3(p.x, p.y + lift, z0))).ToList();
            works.Poly(front, m.MultiplyVector(Vector3.forward), material);
            works.Poly(back, m.MultiplyVector(Vector3.back), material);
            var centre = outline.Aggregate(Vector2.zero, (s, p) => s + p) / outline.Count;
            for (var k = 0; k < outline.Count; k++)
            {
                var n = (k + 1) % outline.Count;
                var mid = ((outline[k] + outline[n]) / 2f) - centre;
                var edge = outline[n] - outline[k];
                var normal = new Vector3(edge.y, -edge.x, 0f);
                if (Vector2.Dot(new Vector2(normal.x, normal.y), mid) < 0f)
                {
                    normal = -normal;
                }

                works.Poly(new[] { front[k], front[n], back[n], back[k] }, m.MultiplyVector(normal.normalized), material);
            }
        }

        /// <summary>A four-sided shaft from half-width <paramref name="r0"/> at <paramref name="y0"/> to <paramref name="r1"/> at <paramref name="y1"/>, capped by a point at <paramref name="apex"/>.</summary>
        private static void Taper(Works works, Matrix4x4 m, float y0, float r0, float y1, float r1, float apex, string material)
        {
            var corners = new[] { new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(-1f, -1f), new Vector2(1f, -1f) };
            var low = corners.Select(c => m.MultiplyPoint3x4(new Vector3(c.x * r0, y0, c.y * r0))).ToArray();
            var high = corners.Select(c => m.MultiplyPoint3x4(new Vector3(c.x * r1, y1, c.y * r1))).ToArray();
            var top = m.MultiplyPoint3x4(new Vector3(0f, apex, 0f));
            for (var k = 0; k < 4; k++)
            {
                var n = (k + 1) % 4;
                var side = (corners[k] + corners[n]) / 2f;
                var shaftOut = new Vector3(side.x, (r0 - r1) / (y1 - y0), side.y);
                works.Poly(new[] { low[k], low[n], high[n], high[k] }, m.MultiplyVector(shaftOut), material);
                var capOut = new Vector3(side.x, r1 / (apex - y1), side.y);
                works.Poly(new[] { high[k], high[n], top }, m.MultiplyVector(capOut), material);
            }
        }
    }
}

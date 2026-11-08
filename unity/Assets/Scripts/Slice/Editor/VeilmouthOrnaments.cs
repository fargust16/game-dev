using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The owner's garden ornaments in Veilmouth (2026-11-08, city plan §23): three different angels, each in a green place
    /// in another part of the city (Wardley Garden, where the plan asks for "a fountain or a statue" in a square, §6.1,
    /// in place of the garden's middle tree; the old chapel's graveyard; the east villa's garden on the Ridge); a pair of
    /// great lions before the Governor's Palace, the crown's power at the head of the avenue; stone
    /// birdbaths in the gardens of the villas, the faction's house, the detached houses and Wardley Garden. The scene is
    /// the owner's, so this pass touches only its own group, `Ornaments`, and that one tree; run again, it replaces its
    /// own group. Each ornament takes the grid node nearest its wished centre whose footprint lies in its place, on even
    /// ground, with <see cref="Clear"/> free all round for walking. Then each is rendered from the front.
    /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthOrnaments.Place
    /// </summary>
    internal static class VeilmouthOrnaments
    {
        private const string GroupName = "Ornaments";

        /// <summary>Authoring default: room kept clear round an ornament, m.</summary>
        private const float Clear = 0.5f;

        /// <summary>Authoring default: the most the ground may fall across a footprint, m.</summary>
        private const float Even = 0.35f;

        // (key, wished centre px, py, its place px0, py0, px1, py1 (the footprint stays inside), quarter turns, name).
        // Every model faces +Z (north, toward the smaller py), as the renders show: no turn faces north, two face south.
        // The lions lie along X with the head at the east end, so turned twice the head is west, looking down the avenue.
        private static readonly (string Key, float Px, float Py, int X0, int Y0, int X1, int Y1, int Turns, string Name)[] Spots =
        {
            ("AngelStatue", 557.5f, 697f, 557, 696, 558, 698, 0, "Angel fountain, Wardley Garden"),
            // City plan C3 (§24.3): the villas' side gardens north of the Parade, the forecourt either side of the Crown
            // Axis (px 612), and the Terraces' plots of 16 m.
            ("AngelStatueB", 512f, 646f, 503, 629, 529, 649, 0, "Angel, the old chapel's graveyard"),
            ("AngelStatueC", 734f, 655f, 731, 640, 737, 661, 2, "Angel, the east villa's garden"),
            ("LionStatue", 602f, 657f, 588, 651, 609, 666, 2, "Lion, the palace forecourt (west)"),
            ("LionStatue", 622f, 657f, 615, 651, 636, 666, 2, "Lion, the palace forecourt (east)"),
            ("Birdbath", 533f, 655f, 531, 640, 535, 661, 0, "Birdbath, Ridge villa (west)"),
            ("Birdbath", 582.5f, 655f, 580, 640, 585, 661, 0, "Birdbath, Ridge villa (stucco)"),
            ("Birdbath", 643f, 655f, 639, 640, 648, 661, 0, "Birdbath, the faction's garden"),
            ("Birdbath", 680f, 655f, 677, 640, 683, 661, 0, "Birdbath, Ridge villa (white)"),
            ("Birdbath", 711f, 655f, 708, 640, 714, 661, 0, "Birdbath, Ridge villa (east)"),
            ("Birdbath", 507f, 698.5f, 501, 697, 515, 700, 0, "Birdbath, detached house (west)"),
            ("Birdbath", 537f, 698.5f, 529, 697, 543, 700, 0, "Birdbath, detached house (east)"),
            ("Birdbath", 550f, 698f, 546, 686, 556, 710, 0, "Birdbath, Wardley Garden (west)"),
            ("Birdbath", 566f, 698f, 560, 686, 569, 710, 0, "Birdbath, Wardley Garden (east)"),
        };

        [MenuItem("GameDev/Slice/Place Veilmouth Ornaments")]
        public static void Place()
        {
            var kit = SliceKitCatalogue.Load();
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == GroupName);
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }

            // The middle tree of Wardley Garden gives its place to the angel.
            var heart = World(557.5f, 696.5f);
            foreach (var tree in Object.FindObjectsByType<KitBlock>(FindObjectsInactive.Include)
                         .Where(k => k.name.Contains("Tree") && Flat(k.transform.position + new Vector3(0.5f, 0f, 0.5f) - heart) < 1.2f).ToList())
            {
                Debug.Log($"[VeilmouthOrnaments] '{tree.name}' at {tree.transform.position:F1} gives its place to the angel.");
                Object.DestroyImmediate(tree.gameObject);
            }

            var group = new GameObject(GroupName).transform;
            var placed = new List<string>();
            var shots = new List<(Vector3 Centre, Vector3 Facing, float Height)>();
            foreach (var (key, px, py, x0, y0, x1, y1, turns, name) in Spots)
            {
                var entry = kit[key];
                var (wx, wz) = turns % 2 == 0 ? (entry.Width, entry.Depth) : (entry.Depth, entry.Width);
                Physics.SyncTransforms();
                var spot = Candidates(px, py, x0, y0, x1, y1, wx, wz)
                    .Select(c => (c.X, c.Y, Floor: Free(c.X, c.Y, wx, wz)))
                    .FirstOrDefault(c => c.Floor.HasValue);
                if (!spot.Floor.HasValue)
                {
                    placed.Add($"{name}: NO ROOM in px {x0}-{x1}, py {y0}-{y1}");
                    continue;
                }

                var centre = World(spot.X + (wx * 0.5f), spot.Y + (wz * 0.5f)) + (Vector3.up * spot.Floor.Value);
                var rotation = Quaternion.Euler(0f, 90f * turns, 0f);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(entry.Prefab, group);
                go.name = name;
                go.transform.SetPositionAndRotation(centre - (rotation * new Vector3(entry.Width * 0.5f, 0f, entry.Depth * 0.5f)), rotation);
                placed.Add($"{name}: {key} {wx}x{wz} at px {spot.X}, py {spot.Y}, y {spot.Floor.Value:F1}, " +
                           $"{Flat(centre - World(px, py)):F1} m from its wish");
                shots.Add((centre, rotation * Vector3.forward, entry.Height));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[VeilmouthOrnaments] {placed.Count} ornaments:\n  " + string.Join("\n  ", placed));

            var i = 0;
            foreach (var (centre, facing, height) in shots)
            {
                var far = Mathf.Max(4f, height * 2.2f);
                VeilmouthRenders.Eye($"orn_{i++:00}", centre + (facing * far) + (Vector3.up * (1.6f + (height * 0.3f))),
                    centre + (Vector3.up * (height * 0.45f)), 55f);
            }
        }

        // Every north-west corner whose footprint lies in the place, nearest the wished centre first.
        private static IEnumerable<(int X, int Y)> Candidates(float px, float py, int x0, int y0, int x1, int y1, int wx, int wz)
        {
            var all = new List<(int X, int Y)>();
            for (var x = x0; x + wx <= x1; x++)
            {
                for (var y = y0; y + wz <= y1; y++)
                {
                    all.Add((x, y));
                }
            }

            return all.OrderBy(c => new Vector2(c.X + (wx * 0.5f) - px, c.Y + (wz * 0.5f) - py).sqrMagnitude)
                .ThenBy(c => c.X).ThenBy(c => c.Y);
        }

        // The footprint's floor height when nothing but the ground stands within Clear of it and the ground is even.
        private static float? Free(int x, int y, int wx, int wz)
        {
            var heights = new List<float>();
            for (var dx = 0f; dx <= wx; dx += 0.5f)
            {
                for (var dy = 0f; dy <= wz; dy += 0.5f)
                {
                    var w = World(x + dx, y + dy);
                    if (!Physics.Raycast(new Vector3(w.x, 300f, w.z), Vector3.down, out var hit, 400f, LayerMask.GetMask(KitLayers.Ground),
                            QueryTriggerInteraction.Ignore))
                    {
                        return null;
                    }

                    heights.Add(hit.point.y);
                }
            }

            if (heights.Max() - heights.Min() > Even)
            {
                return null;
            }

            var floor = Mathf.Ceil(Mathf.Max(heights.Min(), heights.Max() - 0.15f) * 10f) / 10f;
            var centre = World(x + (wx * 0.5f), y + (wz * 0.5f));
            var half = new Vector3((wx * 0.5f) + Clear, 1.2f, (wz * 0.5f) + Clear);
            var ground = LayerMask.NameToLayer(KitLayers.Ground);
            if (Physics.OverlapBox(new Vector3(centre.x, floor + 1.35f, centre.z), half, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)
                .Any(c => c.gameObject.layer != ground))
            {
                return null;
            }

            // Blocks without a body (shrubs, flowers), by their pivots.
            var blocked = Object.FindObjectsByType<KitBlock>(FindObjectsInactive.Exclude).Any(k =>
            {
                var p = k.transform.position;
                return k.gameObject.layer != ground && Mathf.Abs(p.x - centre.x) < half.x && Mathf.Abs(p.z - centre.z) < half.z
                       && Mathf.Abs(p.y - floor) < 3f && k.GetComponentInChildren<Collider>() == null;
            });
            return blocked ? null : floor;
        }

        /// <summary>
        /// Read only: what stands in each place the ornaments may go, by block name, with the plan's metres.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthOrnaments.Probe
        /// </summary>
        public static void Probe()
        {
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var areas = new (string Name, int X0, int Y0, int X1, int Y1)[]
            {
                ("graves", 498, 626, 526, 650), ("palace forecourt", 586, 646, 634, 664), ("villa 524", 522, 636, 557, 664),
                ("villa 555", 553, 636, 588, 664), ("faction", 632, 636, 672, 664), ("villa 670", 668, 636, 706, 664),
                ("villa 704", 702, 636, 739, 664), ("Wardley", 544, 682, 572, 714), ("house 500", 498, 682, 518, 708),
                ("house 528", 526, 682, 546, 708), ("Customs", 736, 762, 780, 794), ("Circus", 584, 715, 626, 746),
            };
            var log = new System.Text.StringBuilder("[VeilmouthOrnaments] probe\n");
            var blocks = Object.FindObjectsByType<KitBlock>(FindObjectsInactive.Exclude);
            foreach (var (name, x0, y0, x1, y1) in areas)
            {
                log.AppendLine($"== {name} px {x0}-{x1} py {y0}-{y1}");
                foreach (var g in blocks.Select(b => (b, p: b.transform.position))
                             .Where(t => t.p.x + 500f >= x0 && t.p.x + 500f <= x1 && 500f - t.p.z >= y0 && 500f - t.p.z <= y1)
                             .GroupBy(t => System.Text.RegularExpressions.Regex.Replace(t.b.name, @"\s*\(\d+\)$", string.Empty)))
                {
                    log.AppendLine($"  {g.Key} x{g.Count()}: " + string.Join(" ", g.Take(12).Select(t => $"({t.p.x + 500f:F0},{500f - t.p.z:F0})")));
                }
            }

            Debug.Log(log.ToString());
        }

        private static Vector3 World(float px, float py) => new Vector3(px - 500f, 0f, 500f - py);

        private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;
    }
}

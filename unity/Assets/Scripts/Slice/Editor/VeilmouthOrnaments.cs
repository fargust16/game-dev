using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The owner's garden ornaments in Veilmouth (2026-11-08, city plan §23, placed on C5 by §29.4): three different
    /// angels, each in a green place in another part of the city (Bishop's Garden, Last Turning Cemetery, the Sleepy
    /// Quarter's park yard); a pair of great lions before the Governor's House, the crown's power facing Long Tongue;
    /// stone birdbaths in Bishop's and Quiet Whistle Gardens and the rich blocks' park yards.
    /// The scene is the owner's, so this pass touches only its own group, `Ornaments`; run again, it replaces its own group. Each ornament takes the grid node nearest its wished centre whose footprint lies in its place, on even
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
        // City plan C5 (§29.3, §29.4): Bishop's Garden's middle walk, the cemetery, and the Sleepy Quarter's park yard for
        // the angels; the birdbaths in the two gardens and the rich blocks' park yards, the houses now standing flush on
        // the pavement with no front gardens.
        private static readonly (string Key, float Px, float Py, int X0, int Y0, int X1, int Y1, int Turns, string Name)[] Fixed =
        {
            ("AngelStatue", 499f, 330f, 486, 312, 512, 348, 2, "Angel, Bishop's Garden"),
            ("AngelStatueB", 452f, 338f, 434, 328, 470, 349, 2, "Angel, Last Turning Cemetery"),
            ("AngelStatueC", 452f, 406f, 444, 396, 460, 417, 2, "Angel, the Sleepy Quarter's yard"),
            ("Birdbath", 499f, 300f, 486, 290, 512, 310, 0, "Birdbath, Bishop's Garden"),
            ("Birdbath", 581f, 300f, 568, 280, 594, 320, 0, "Birdbath, Quiet Whistle Garden"),
            ("Birdbath", 452f, 517.5f, 444, 513, 460, 522, 0, "Birdbath, the embankment yard"),
            ("Birdbath", 506.5f, 471f, 496, 463, 517, 479, 0, "Birdbath, the Governor's yard"),
            ("Birdbath", 573.5f, 406.5f, 563, 396, 584, 417, 0, "Birdbath, the Assize's yard"),
            ("Birdbath", 512f, 400f, 505, 396, 517, 405, 0, "Birdbath, the Town Hall's yard"),
        };

        /// <summary>
        /// Every ornament's place: the fixed ones, and the pair of great lions either side of the Governor's House's door on
        /// Long Tongue, along its front. A lion lies along X with its head at the east end, its face turned north.
        /// </summary>
        private static IEnumerable<(string Key, float Px, float Py, int X0, int Y0, int X1, int Y1, int Turns, string Name)> Spots(Dictionary<string, SliceKitCatalogue.Entry> kit)
        {
            foreach (var spot in Fixed)
            {
                yield return spot;
            }

            var governor = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == "The Governor's House");
            if (governor == null)
            {
                Debug.LogWarning("[VeilmouthOrnaments] no Governor's House: the lions are not placed.");
                yield break;
            }

            // C5 (§29.4): the house fronts Long Tongue's 6 m pavement, with its stoop at the door and the street trees in
            // pits by the kerb, so a lion turned to the street (4 m deep) leaves no way past it. The pair lies unturned along
            // the front instead, both faces on the street; a half turn would set the west lion's face on the wall. It takes
            // the first spacing from the door, and distance from the front, at which both lions are free, nearest the wall
            // first; a lion alone is never placed.
            var bounds = governor.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            var (mid, front) = (bounds.center.x + 500f, 500f - bounds.max.z);
            var (wx, wz) = (kit["LionStatue"].Width, kit["LionStatue"].Depth);
            Physics.SyncTransforms();
            foreach (var off in new[] { 5f, 6f, 7f, 8f, 9f, 10f, 11f, 12f })
            {
                foreach (var back in new[] { -2, -1, 0, 1, 2 })
                {
                    var y = Mathf.FloorToInt(front) - wz - back;
                    var pair = new[] { -1, 1 }.Select(side => Mathf.RoundToInt(mid + (side * off) - (wx * 0.5f))).ToArray();
                    if (pair.Any(x => !Free(x, y, wx, wz).HasValue))
                    {
                        continue;
                    }

                    for (var k = 0; k < 2; k++)
                    {
                        yield return ("LionStatue", pair[k] + (wx * 0.5f), y + (wz * 0.5f), pair[k], y, pair[k] + wx, y + wz, 0,
                            k == 0 ? "Lion, the Governor's House (west)" : "Lion, the Governor's House (east)");
                    }

                    yield break;
                }
            }

            Debug.LogWarning("[VeilmouthOrnaments] no room for the pair of lions before the Governor's House.");
        }

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

            var group = new GameObject(GroupName).transform;
            var placed = new List<string>();
            var shots = new List<(Vector3 Centre, Vector3 Facing, float Height)>();
            foreach (var (key, px, py, x0, y0, x1, y1, turns, name) in Spots(kit).ToList())
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
                // The garden's grass (city plan §25.4) gives way to an ornament, as the middle tree gives way to the angel.
                foreach (var grass in Object.FindObjectsByType<KitBlock>(FindObjectsInactive.Include).Where(k => IsGrass(k) &&
                             Mathf.Abs(k.transform.position.x + 0.5f - centre.x) < (wx * 0.5f) + Clear + 0.5f &&
                             Mathf.Abs(k.transform.position.z + 0.5f - centre.z) < (wz * 0.5f) + Clear + 0.5f).ToList())
                {
                    Object.DestroyImmediate(grass.gameObject);
                }

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
                       && Mathf.Abs(p.y - floor) < 3f && k.GetComponentInChildren<Collider>() == null && !IsGrass(k);
            });
            return blocked ? null : floor;
        }

        /// <summary>A grass clump of the city's lawns (city plan §25.4): an ornament stands in its place.</summary>
        private static bool IsGrass(KitBlock k) => k.name.StartsWith("Kit_Bush_Grass_");

        /// <summary>
        /// Read only: what stands in each place the ornaments may go, by block name, with the plan's metres.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthOrnaments.Probe
        /// </summary>
        public static void Probe()
        {
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var areas = new (string Name, int X0, int Y0, int X1, int Y1)[]
            {
                ("Bishop's Garden", 480, 262, 503, 352), ("cemetery", 440, 262, 478, 334), ("villa 456", 456, 373, 490, 400),
                ("Governor's front", 495, 466, 585, 482), ("faction", 580, 330, 614, 346), ("Widows' detached", 422, 373, 454, 400),
                ("embankment plots", 422, 510, 519, 530),
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

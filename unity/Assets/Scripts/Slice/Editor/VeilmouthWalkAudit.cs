using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The owner's walk of 2026-11-10 (city plan §31), measured in the saved Veilmouth scene: lamps against lamps, trees,
    /// buildings and the kerb; wards off their wall; railing ends; holes in houses; slits at the water's edge; and the
    /// interior pieces' heights. It changes nothing and writes Logs/SliceSurvey/walk_audit.txt with renders of the worst
    /// spots. Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthWalkAudit.Run
    /// </summary>
    public static class VeilmouthWalkAudit
    {
        private const string Report = "Logs/SliceSurvey/walk_audit.txt";

        // Thresholds of §31, m (authoring defaults; the crown radius is measured).
        private const float LampToLamp = 12f, LampToTree = 6.5f, LampToWall = 2f;

        private static readonly StringBuilder Out = new StringBuilder();
        private static readonly List<(string Name, Vector3 At, Vector3 Look)> Spots = new List<(string, Vector3, Vector3)>();

        public static void Run()
        {
            Out.Clear();
            Spots.Clear();
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
            var blocks = Object.FindObjectsByType<KitBlock>(FindObjectsSortMode.None);
            Lamps(blocks);
            Wards();
            Railings(blocks);
            Holes();
            Water();
            Directory.CreateDirectory(Path.GetDirectoryName(Report));
            File.WriteAllText(Report, Out.ToString());
            foreach (var (name, at, look) in Spots.Take(48))
            {
                VeilmouthRenders.Eye("walk_" + name, at, look, 60f);
            }

            Debug.Log($"[VeilmouthWalkAudit] written to {Report}, {Mathf.Min(40, Spots.Count)} renders.");
            Furniture();
        }

        /// <summary>
        /// Batch: the city built again, its people and far LODs brought up to it, then this audit.
        /// -executeMethod GameDev.Slice.EditorTools.VeilmouthWalkAudit.RebuildAndRun
        /// </summary>
        public static void RebuildAndRun()
        {
            VeilmouthCity.RebuildStreets();
            SlicePeopleModels.ScenesOnly();
            VeilmouthFarLod.SyncScene();
            Run();
        }

        /// <summary>
        /// Batch: as <see cref="RebuildAndRun"/>, with the interiors' resized pieces (furnishing rules §11) written and the
        /// seven and the Dwellings brought up to them before the audit.
        /// -executeMethod GameDev.Slice.EditorTools.VeilmouthWalkAudit.RebuildAllAndRun
        /// </summary>
        public static void RebuildAllAndRun()
        {
            // The resized wrappers first, moved to their new names with their GUIDs, before the city's measuring Load runs.
            SliceKitCatalogue.LoadRebuilding(VeilmouthInteriors.Resized);
            VeilmouthCity.RebuildStreets();
            SlicePeopleModels.ScenesOnly();
            VeilmouthFarLod.SyncScene();
            VeilmouthInteriors.EnrichAndRender();
            Run();
        }

        /// <summary>
        /// Batch: the interiors' resized pieces written and the seven and the Dwellings brought up to them, then this audit,
        /// with the city as it stands. -executeMethod GameDev.Slice.EditorTools.VeilmouthWalkAudit.InteriorsAndRun
        /// </summary>
        public static void InteriorsAndRun()
        {
            VeilmouthInteriors.EnrichAndRender();
            Run();
        }

        /// <summary>The interior pieces alone, from the kit: no scene is opened.</summary>
        public static void Furniture()
        {
            Out.Clear();
            FurnitureSizes();
            Directory.CreateDirectory(Path.GetDirectoryName(Report));
            File.WriteAllText("Logs/SliceSurvey/walk_furniture.txt", Out.ToString());
            Debug.Log("[VeilmouthWalkAudit] furniture written.");
        }

        private static Vector2 Flat(Vector3 v) => new Vector2(v.x, v.z);

        private static Vector3 Post(KitBlock b)
        {
            var model = b.transform.Find("Model");
            return model != null ? model.position : b.transform.position;
        }

        // ------------------------------------------------------------------------------------------- lamps

        private static void Lamps(KitBlock[] blocks)
        {
            var lamps = blocks.Where(b => b.name.StartsWith("Kit_Street_Lamp")).ToList();
            var trees = blocks.Where(b => b.name.Contains("MatureHornbeam")).Select(Post).ToList();
            Out.AppendLine($"== Lamps: {lamps.Count}, road trees: {trees.Count}");

            // The base's half-width: the lamp model's widest point in its lowest 0.3 m, from the post's axis.
            var prefab = SliceKitCatalogue.Load()["LampPost"].Prefab;
            var probe = (GameObject)Object.Instantiate(prefab);
            var axis = Post(probe.GetComponent<KitBlock>());
            var foot = float.MaxValue;
            var verts = new List<Vector3>();
            foreach (var mf in probe.GetComponentsInChildren<MeshFilter>())
            {
                foreach (var v in mf.sharedMesh.vertices)
                {
                    var w = mf.transform.TransformPoint(v);
                    verts.Add(w);
                    foot = Mathf.Min(foot, w.y);
                }
            }

            var baseHalf = verts.Where(w => w.y < foot + 0.3f).Select(w => Flat(w - axis).magnitude).DefaultIfEmpty(0f).Max();
            var postHalf = verts.Where(w => w.y > foot + 1f && w.y < foot + 2f).Select(w => Flat(w - axis).magnitude).DefaultIfEmpty(0f).Max();
            Out.AppendLine($"lamp base half-width {baseHalf:0.000} m (lowest 0.3 m), post half-width {postHalf:0.000} m (1-2 m up); foot at {foot - probe.transform.position.y:0.000} m");
            Object.DestroyImmediate(probe);

            int pairs = 0, nearTree = 0, nearWall = 0, kerb = 0;
            for (var i = 0; i < lamps.Count; i++)
            {
                var p = Post(lamps[i]);
                for (var j = i + 1; j < lamps.Count; j++)
                {
                    var d = Flat(Post(lamps[j]) - p).magnitude;
                    if (d < LampToLamp)
                    {
                        pairs++;
                        var q = Post(lamps[j]);
                        Out.AppendLine($"  lamp pair {d:0.0} m at plan ({p.x + 500f:0.0}, {500f - p.z:0.0}) and ({q.x + 500f:0.0}, {500f - q.z:0.0})");
                        if (pairs <= 6)
                        {
                            Spots.Add(($"lamppair_{pairs}", p + new Vector3(6f, 3f, 6f), p + Vector3.up));
                        }
                    }
                }

                var tree = trees.Select(t => Flat(t - p).magnitude).DefaultIfEmpty(99f).Min();
                if (tree < LampToTree)
                {
                    nearTree++;
                }

                // A wall within 2 m: eight level rays from 1.5 m up the post, hitting a collider at least 2 m wide that is not a
                // lamp, a tree or a railing.
                var from = p + (Vector3.up * 1.5f);
                var wall = 99f;
                for (var k = 0; k < 8; k++)
                {
                    var dir = Quaternion.Euler(0f, 45f * k, 0f) * Vector3.forward;
                    foreach (var h in Physics.RaycastAll(from, dir, 3f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        var owner = h.collider.GetComponentInParent<KitBlock>();
                        if (owner == lamps[i] || (owner != null && (owner.name.Contains("Tree") || owner.name.Contains("Railing") || owner.name.Contains("Lamp"))))
                        {
                            continue;
                        }

                        var size = h.collider.bounds.size;
                        if (Mathf.Abs(h.normal.y) < 0.3f && Mathf.Max(size.x, size.z) >= 2f && size.y >= 1.5f)
                        {
                            wall = Mathf.Min(wall, h.distance);
                        }
                    }
                }

                if (wall < LampToWall)
                {
                    nearWall++;
                    Out.AppendLine($"  lamp {wall:0.00} m from a wall at plan ({p.x + 500f:0.0}, {500f - p.z:0.0}) [{lamps[i].transform.parent?.name}]");
                    if (nearWall <= 6)
                    {
                        Spots.Add(($"lampwall_{nearWall}", p + new Vector3(5f, 2.5f, -5f), p + Vector3.up));
                    }
                }

                // The kerb under the base: ground rising 0.08 m over the foot within the base's half-width.
                var rise = 0f;
                for (var k = 0; k < 16; k++)
                {
                    var dir = Quaternion.Euler(0f, 22.5f * k, 0f) * Vector3.forward;
                    var at = p + (dir * (baseHalf - 0.02f)) + (Vector3.up * 3f);
                    foreach (var h in Physics.RaycastAll(at, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
                    {
                        if (h.collider.GetComponentInParent<KitBlock>() == lamps[i])
                        {
                            continue;
                        }

                        rise = Mathf.Max(rise, h.point.y - p.y);
                        break;
                    }
                }

                if (rise > 0.08f)
                {
                    kerb++;
                    if (kerb <= 30)
                    {
                        Out.AppendLine($"  lamp base in the kerb, {rise:0.00} m, at plan ({p.x + 500f:0.0}, {500f - p.z:0.0})");
                    }

                    if (kerb <= 3)
                    {
                        Spots.Add(($"lampkerb_{kerb}", p + new Vector3(1.6f, 1.2f, 1.6f), p + (Vector3.up * 0.2f)));
                    }
                }
            }

            Out.AppendLine($"lamps under {LampToLamp} m of another: {pairs} pairs; within {LampToTree} m of a road tree: {nearTree}; within {LampToWall} m of a wall: {nearWall}; base in the kerb: {kerb}");
            var treePairs = 0;
            for (var i = 0; i < trees.Count; i++)
            {
                for (var j = i + 1; j < trees.Count; j++)
                {
                    treePairs += Flat(trees[j] - trees[i]).magnitude < 8f ? 1 : 0;
                }
            }

            Out.AppendLine($"road trees under 8 m of another: {treePairs}");
        }

        // ------------------------------------------------------------------------------------------- wards

        private static void Wards()
        {
            var root = GameObject.Find(VeilmouthMystic.RootName);
            var wards = root == null ? new List<Transform>() : root.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Ward - ") || t.name.StartsWith("Runes - ")).ToList();
            var floating = 0;
            foreach (var w in wards)
            {
                var misses = 0;
                for (var u = -2; u <= 2; u++)
                {
                    for (var v = -1; v <= 1; v++)
                    {
                        var local = new Vector3(u * 0.25f, v * 0.5f, 0f);
                        var at = w.TransformPoint(local);
                        if (!Physics.Raycast(at - (w.forward * 0.3f), w.forward, out var h, 0.6f, ~0, QueryTriggerInteraction.Ignore) || Mathf.Abs(h.distance - 0.36f) > 0.15f)
                        {
                            misses++;
                        }
                    }
                }

                if (misses > 0)
                {
                    floating++;
                    Out.AppendLine($"  {w.name} with {misses}/15 points off a wall at plan ({w.position.x + 500f:0.0}, {500f - w.position.z:0.0})");
                    if (floating <= 4)
                    {
                        Spots.Add(($"ward_{floating}", w.position - (w.forward * 3.5f) + (Vector3.down * 0.8f), w.position));
                    }
                }
            }

            Out.AppendLine($"== Wards: {wards.Count}, off their wall: {floating}");
        }

        // ------------------------------------------------------------------------------------------- railings

        private static void Railings(KitBlock[] blocks)
        {
            var rails = blocks.Where(b => b.name.StartsWith("Kit_Street_Railing")).ToList();
            var ends = new List<(Vector3 P, Vector3 Out, KitBlock B)>();
            foreach (var r in rails)
            {
                var bars = r.GetComponentsInChildren<Renderer>().Where(x => !x.name.StartsWith("Fence column")).ToList();
                if (bars.Count == 0)
                {
                    continue;
                }

                var b = bars.Select(x => x.bounds).Aggregate((p, q) => { p.Encapsulate(q); return p; });
                var alongX = b.size.x >= b.size.z;
                var dir = alongX ? Vector3.right : Vector3.forward;
                var half = alongX ? b.extents.x : b.extents.z;
                var mid = new Vector3(b.center.x, b.min.y + 0.5f, b.center.z);
                ends.Add((mid - (dir * half), -dir, r));
                ends.Add((mid + (dir * half), dir, r));
            }

            var posts = rails.SelectMany(r => r.GetComponentsInChildren<Renderer>()).Where(x => x.name.StartsWith("Fence column")).Select(x => Flat(x.bounds.center)).ToList();
            int gaps = 0, bare = 0;
            foreach (var (p, outward, owner) in ends)
            {
                if (ends.Any(e => e.B != owner && Flat(e.P - p).magnitude < 0.2f))
                {
                    continue;
                }

                var wall = 99f;
                foreach (var h in Physics.RaycastAll(p - (outward * 0.05f), outward, 1.5f, ~0, QueryTriggerInteraction.Ignore))
                {
                    var o = h.collider.GetComponentInParent<KitBlock>();
                    if (o != null && (o.name.Contains("Railing") || o.name.Contains("Lamp") || o.name.Contains("Tree")))
                    {
                        continue;
                    }

                    if (Mathf.Abs(h.normal.y) < 0.3f && h.collider.bounds.size.y > 1.5f)
                    {
                        wall = Mathf.Min(wall, h.distance - 0.05f);
                    }
                }

                if (wall > 0.1f && wall < 1.5f)
                {
                    gaps++;
                    Out.AppendLine($"  railing end {wall:0.00} m short of a wall at plan ({p.x + 500f:0.0}, {500f - p.z:0.0})");
                    if (gaps <= 5)
                    {
                        Spots.Add(($"railgap_{gaps}", p - (outward * 2.5f) + Vector3.Cross(outward, Vector3.up) * 2.5f + (Vector3.up * 1.2f), p));
                    }
                }

                if (wall > 0.1f && !posts.Any(q => (q - Flat(p)).magnitude < 0.3f))
                {
                    bare++;
                    Out.AppendLine($"  railing end with no post at plan ({p.x + 500f:0.0}, {500f - p.z:0.0})");
                    if (bare <= 5)
                    {
                        Spots.Add(($"railpost_{bare}", p - (outward * 2f) + Vector3.Cross(outward, Vector3.up) * 2f + (Vector3.up * 1f), p));
                    }
                }
            }

            Out.AppendLine($"== Railings: {rails.Count} pieces, {ends.Count} ends; short of a wall: {gaps}; free ends with no post: {bare}");
        }

        // ------------------------------------------------------------------------------------------- holes

        private static int _floating;

        private static void Holes()
        {
            _floating = 0;
            var city = GameObject.Find(VeilmouthCity.RootName);
            var houses = city.GetComponentsInChildren<Transform>(true).Where(t => t.Find("Model") != null && t.GetComponent<KitBlock>() != null).ToList();
            var blind = city.GetComponentsInChildren<Transform>(true).Where(t => t.name == VeilmouthCity.BlindDoorMarker).ToList();
            var bareBlind = 0;
            foreach (var m in blind)
            {
                var model = m.parent.Find("Model");
                var plain = model != null && model.Cast<Transform>().Any(t => t.name.EndsWith(VeilmouthKitHouses.WalledUp) && Flat(t.position - m.position).magnitude < 2.5f);
                if (!plain)
                {
                    bareBlind++;
                    var hidden = model == null ? "" : string.Join(", ", model.Cast<Transform>().Where(t => !t.gameObject.activeSelf && Flat(t.position - m.position).magnitude < 3f).Select(t => t.name));
                    Out.AppendLine($"  walled-up door with no plain wall: {m.parent.name} at plan ({m.position.x + 500f:0.0}, {500f - m.position.z:0.0}); hidden: {hidden}");
                    if (bareBlind <= 4)
                    {
                        Spots.Add(($"blind_{bareBlind}", m.position + (m.forward * 4f) + (Vector3.up * 1.6f), m.position + (Vector3.up * 1.2f)));
                    }
                }
            }

            Out.AppendLine($"== Walled-up back doors: {blind.Count}, with no plain wall in the door's place: {bareBlind}");

            // Rays into each house's walls, against mesh colliders added for the test: a hit deeper than 0.7 m in from the
            // walls' outer plane, a back face, or none, is a hole.
            var rects = new Dictionary<Transform, Bounds>();
            foreach (var h in houses)
            {
                var walls = h.Find("Model").GetComponentsInChildren<Renderer>().Where(r => r.name.StartsWith("SM_Vic_Wall") || r.transform.parent.name.StartsWith("SM_Vic_Wall")).ToList();
                if (walls.Count > 0)
                {
                    rects[h] = walls.Select(r => r.bounds).Aggregate((p, q) => { p.Encapsulate(q); return p; });
                }
            }

            var hitBack = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            var flagged = 0;
            try
            {
                foreach (var (house, box) in rects)
                {
                    var added = new List<Collider>();
                    foreach (var mf in house.Find("Model").GetComponentsInChildren<MeshFilter>())
                    {
                        // The far LOD's merged mesh is not readable, and a box of its bounds would close every hole: left out.
                        if (mf.sharedMesh == null || mf.GetComponent<MeshRenderer>() == null || !mf.GetComponent<MeshRenderer>().enabled ||
                            mf.GetComponentsInParent<Transform>(true).Any(t => t.name == "FarLod"))
                        {
                            continue;
                        }

                        // A mesh built by code is not readable and cannot be cooked: the windows' glass is, so it is a
                        // thin box of its own bounds.
                        if (!mf.sharedMesh.isReadable)
                        {
                            var bc = mf.gameObject.AddComponent<BoxCollider>();
                            bc.center = mf.sharedMesh.bounds.center;
                            bc.size = Vector3.Max(mf.sharedMesh.bounds.size, Vector3.one * 0.01f);
                            added.Add(bc);
                            continue;
                        }

                        var mc = mf.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                        added.Add(mc);
                    }

                    Physics.SyncTransforms();
                    var mine = new HashSet<Collider>(added);
                    var holes = new List<(Vector3 P, Vector3 N)>();
                    foreach (var (normal, along) in new[] { (Vector3.right, Vector3.forward), (Vector3.left, Vector3.forward), (Vector3.forward, Vector3.right), (Vector3.back, Vector3.right) })
                    {
                        // Each side against its own wall: a ray reaching 0.8 m past the side's median hit, a back face, or
                        // nothing within 4 m, is a hole.
                        var side = new List<(Vector3 At, float D)>();
                        var face = box.center + Vector3.Scale(normal, box.extents);
                        var span = Mathf.Abs(Vector3.Dot(box.extents, along));
                        for (var a = -span + 0.4f; a <= span - 0.4f; a += 0.5f)
                        {
                            for (var y = box.min.y + 0.5f; y < box.max.y - 1f; y += 1f)
                            {
                                var start = new Vector3(face.x, y, face.z) + (along * a) + (normal * 1f);
                                if (rects.Any(r => r.Key != house && r.Value.Contains(start - (normal * 0.9f))))
                                {
                                    continue; // a neighbour against this side
                                }

                                var first = Physics.RaycastAll(start, -normal, 4f, ~0, QueryTriggerInteraction.Ignore).Where(x => mine.Contains(x.collider)).OrderBy(x => x.distance).FirstOrDefault();
                                side.Add((start, first.collider == null ? 99f : Vector3.Dot(first.normal, -normal) > 0.1f ? 98f : first.distance));
                            }
                        }

                        if (side.Count == 0)
                        {
                            continue;
                        }

                        var median = side.Select(s => s.D).OrderBy(d => d).ElementAt(side.Count / 2);
                        holes.AddRange(side.Where(s => s.D > median + 0.8f).Select(s => (s.At - (normal * (median + 0.3f)), normal)));
                    }

                    // The ground round the house, 0.4 m out from its walls: a level ray 5 cm over it, in toward the house, that
                    // meets nothing of the house within 1.2 m (its walls, its plinth, its doors) finds a slit under it. A point
                    // inside a neighbour is left out.
                    var under = new List<(Vector3 At, float Gap)>();
                    for (var t = 0f; t < 1f; t += 0.02f)
                    {
                        var e = box.extents + new Vector3(0.4f, 0f, 0.4f);
                        var side = (int)(t * 4f);
                        var f = (t * 4f) - side;
                        var px = side == 0 ? -e.x + (2f * e.x * f) : side == 1 ? e.x : side == 2 ? e.x - (2f * e.x * f) : -e.x;
                        var pz = side == 0 ? -e.z : side == 1 ? -e.z + (2f * e.z * f) : side == 2 ? e.z : e.z - (2f * e.z * f);
                        var inward = side == 0 ? Vector3.forward : side == 1 ? Vector3.left : side == 2 ? Vector3.back : Vector3.right;
                        var top = new Vector3(box.center.x + px, box.min.y + 2f, box.center.z + pz);
                        if (rects.Any(r => r.Key != house && r.Value.Contains(new Vector3(top.x, r.Value.center.y, top.z))))
                        {
                            continue;
                        }

                        var g = Physics.RaycastAll(top, Vector3.down, 5f, ~0, QueryTriggerInteraction.Ignore).Where(x => !mine.Contains(x.collider))
                            .OrderBy(x => x.distance).FirstOrDefault();
                        if (g.collider == null)
                        {
                            continue;
                        }

                        var low = g.point + (Vector3.up * 0.05f);
                        if (!Physics.RaycastAll(low, inward, 1.2f, ~0, QueryTriggerInteraction.Ignore).Any(x => mine.Contains(x.collider)))
                        {
                            under.Add((g.point, box.min.y - g.point.y));
                        }
                    }

                    if (under.Count >= 2)
                    {
                        var worst = under.OrderByDescending(u => u.Gap).First();
                        Out.AppendLine($"  {house.name} '{house.parent?.name}': open 5 cm over the ground at {under.Count} points (walls' foot {worst.Gap:0.00} m over it), e.g. plan ({worst.At.x + 500f:0.0}, {500f - worst.At.z:0.0})");
                        _floating++;
                        if (_floating <= 4)
                        {
                            var n = worst.At - box.center;
                            n.y = 0f;
                            Spots.Add(($"under_{_floating}", worst.At + (n.normalized * 3f) + (Vector3.up * 0.6f), worst.At + (Vector3.up * 0.1f)));
                        }
                    }

                    foreach (var mc in added)
                    {
                        Object.DestroyImmediate(mc);
                    }

                    if (holes.Count > 0)
                    {
                        flagged++;
                        var (c, n) = holes[holes.Count / 2];
                        var type = PrefabUtility.GetCorrespondingObjectFromSource(house.gameObject)?.name ?? "-";
                        var hidden = house.Find("Model").GetComponentsInChildren<Transform>(true)
                            .Where(t => !t.gameObject.activeSelf && Flat(t.position - c).magnitude < 3f && Mathf.Abs(t.position.y - c.y) < 3f)
                            .Select(t => t.name).Distinct().Take(6).ToList();
                        Out.AppendLine($"  {house.name} [{type}] '{house.parent?.name}': {holes.Count} ray(s) through, e.g. plan ({c.x + 500f:0.0}, {500f - c.z:0.0}) {c.y:0.0} m, facing ({n.x:0}, {n.z:0}); hidden near: {(hidden.Count == 0 ? "none" : string.Join(", ", hidden))}");
                        if (flagged <= 12)
                        {
                            // From outside, 2.5 m out along the side's normal and level with the hole, and 7 m out and higher.
                            Spots.Add(($"hole_{flagged}", c + (n * 2.5f) + (Vector3.up * 0.2f), c));
                            Spots.Add(($"hole_{flagged}_far", c + (n * 7f) + (Vector3.up * 2.5f), c));
                        }
                    }
                }
            }
            finally
            {
                Physics.queriesHitBackfaces = hitBack;
            }

            Out.AppendLine($"== Houses: {rects.Count}, with rays through a wall: {flagged}; over the ground: {_floating}");
        }

        // ------------------------------------------------------------------------------------------- water

        /// <summary>
        /// Profiles across every water edge every 0.5 m: a run of ground below the streets' level by 1 m between built ground and
        /// a structure on the water side within 1.2 m is a slit.
        /// </summary>
        private static void Water()
        {
            var layout = VeilmouthLayout.Load();
            var low = VeilmouthGround.Low;
            var edges = new List<(string Name, bool AlongX, float Line, float A0, float A1, int Toward)>();
            foreach (var r in layout.Rivers)
            {
                if (r.x1 - r.x0 < r.y1 - r.y0)
                {
                    edges.Add(("river x0", false, r.x0, r.y0, r.y1, 1));
                    edges.Add(("river x1", false, r.x1, r.y0, r.y1, -1));
                }
                else
                {
                    edges.Add(("river y0", true, r.y0, r.x0, r.x1, 1));
                    edges.Add(("river y1", true, r.y1, r.x0, r.x1, -1));
                }
            }

            foreach (var b in layout.bridges)
            {
                var alongX = b.axis == "x";
                var (s0, s1) = alongX ? (b.x0, b.x1) : (b.y0, b.y1);
                var (c0, c1) = alongX ? (b.y0, b.y1) : (b.x0, b.x1);
                foreach (var end in new[] { s0, s1 })
                {
                    edges.Add(($"bridge end {end:0}", !alongX, end, c0, c1, 0));
                    var corner = alongX ? new Vector2(end, c0) : new Vector2(c0, end);
                    var w = new Vector3(corner.x - 500f, low, 500f - corner.y);
                    var axis = alongX ? Vector3.right : Vector3.back;
                    var outward = end == s0 ? -axis : axis;
                    var across = alongX ? Vector3.forward : Vector3.right;
                    Spots.Add(($"bridge_{b.name}_{end:0}", w + (outward * 3f) + (across * 4f) + (Vector3.up * 1.2f), w - (outward * 1.5f)));
                }

                foreach (var c in new[] { c0, c1 })
                {
                    edges.Add(($"bridge side {c:0}", alongX, c, s0 - 3f, s1 + 3f, 0));
                }
            }

            var slits = 0;
            var near = new List<Vector2>();
            foreach (var (name, alongX, line, a0, a1, toward) in edges)
            {
                var step = name.StartsWith("bridge") ? 0.02f : 0.1f;
                var reach = name.StartsWith("bridge") ? 1.5f : 4f;
                for (var a = a0; a <= a1; a += name.StartsWith("bridge") ? 0.25f : 0.5f)
                {
                    var profile = new List<(float D, float H)>();
                    for (var d = -reach; d <= reach; d += step)
                    {
                        var plan = alongX ? new Vector2(a, line + d) : new Vector2(line + d, a);
                        var from = new Vector3(plan.x - 500f, low + 12f, 500f - plan.y);
                        var h = Physics.Raycast(from, Vector3.down, out var hit, 30f, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : -99f;
                        profile.Add((d, h));
                    }

                    // A low run with high ground on both sides, at most 1.2 m wide.
                    for (var i = 1; i < profile.Count - 1; i++)
                    {
                        if (profile[i].H >= low - 1f || profile[i - 1].H < low - 0.6f)
                        {
                            continue;
                        }

                        var j = i;
                        while (j < profile.Count && profile[j].H < low - 1f)
                        {
                            j++;
                        }

                        if (j < profile.Count && (j - i) * step <= 1.21f && profile[j].H >= low - 0.6f)
                        {
                            var at = alongX ? new Vector2(a, line + profile[i].D) : new Vector2(line + profile[i].D, a);
                            if (!near.Any(n => (n - at).magnitude < 3f))
                            {
                                slits++;
                                near.Add(at);
                                Out.AppendLine($"  slit {(j - i) * step:0.00} m wide at plan ({at.x:0.0}, {at.y:0.0}) by the {name}");
                                if (slits <= 6)
                                {
                                    var w = new Vector3(at.x - 500f, low + 1.6f, 500f - at.y);
                                    var side = alongX ? Vector3.right : Vector3.forward;
                                    Spots.Add(($"slit_{slits}", w + (side * 4f) + (Vector3.up * 1f), w + (Vector3.down * 1.5f)));
                                }
                            }
                        }

                        i = j;
                    }
                }
            }

            Out.AppendLine($"== Water edges: {edges.Count}, slits: {slits}");

            // The promenade's parapet: its top along the south river's city side.
            var south = layout.Rivers.First(r => r.x1 - r.x0 > r.y1 - r.y0);
            var tops = new List<float>();
            for (var px = south.x0; px < south.x1; px += 1f)
            {
                if (Physics.Raycast(new Vector3(px - 500f, low + 12f, 500f - (south.y0 + 0.5f)), Vector3.down, out var h, 30f, ~0, QueryTriggerInteraction.Ignore))
                {
                    tops.Add(h.point.y);
                }
            }

            if (tops.Count > 0)
            {
                var steps = tops.Zip(tops.Skip(1), (p, q) => Mathf.Abs(q - p)).Count(s => s > 0.02f && s < 1f);
                Out.AppendLine($"promenade parapet top: {tops.Min():0.00}-{tops.Max():0.00} m, {steps} steps between 1 m samples");
            }
        }

        // ------------------------------------------------------------------------------------------- furniture

        /// <summary>
        /// Each interior piece's height and its working top: the highest level with at least a fifth of the piece's upward
        /// facing area, rounded to 2 cm; a window wall's sill.
        /// </summary>
        private static void FurnitureSizes()
        {
            var kit = SliceKitCatalogue.Load();
            Out.AppendLine("key | width x depth x height (m, as built) | working top (m) | share of upward area");
            foreach (var (key, e) in kit.Where(p => p.Key.StartsWith("Vi") || p.Key.StartsWith("Tr") || p.Key.StartsWith("BK")).OrderBy(p => p.Key))
            {
                var prefab = e.Prefab;
                if (prefab == null)
                {
                    continue;
                }

                var go = (GameObject)Object.Instantiate(prefab);
                var renderers = go.GetComponentsInChildren<MeshRenderer>();
                if (renderers.Length == 0)
                {
                    Object.DestroyImmediate(go);
                    continue;
                }

                var b = renderers.Select(r => r.bounds).Aggregate((p, q) => { p.Encapsulate(q); return p; });
                var area = new Dictionary<int, float>();
                var total = 0f;
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                {
                    var m = mf.sharedMesh;
                    Vector3[] v;
                    int[] t;
                    try
                    {
                        v = m.vertices;
                        t = m.triangles;
                    }
                    catch (System.Exception)
                    {
                        continue;
                    }
                    for (var i = 0; i < t.Length; i += 3)
                    {
                        var p0 = mf.transform.TransformPoint(v[t[i]]);
                        var p1 = mf.transform.TransformPoint(v[t[i + 1]]);
                        var p2 = mf.transform.TransformPoint(v[t[i + 2]]);
                        var n = Vector3.Cross(p1 - p0, p2 - p0);
                        var a = n.magnitude / 2f;
                        if (a < 1e-7f || n.normalized.y < 0.95f)
                        {
                            continue;
                        }

                        var y = Mathf.RoundToInt((((p0.y + p1.y + p2.y) / 3f) - b.min.y) / 0.02f);
                        area[y] = (area.TryGetValue(y, out var s) ? s : 0f) + a;
                        total += a;
                    }
                }

                var top = area.Where(p => p.Value >= total * 0.2f).Select(p => p.Key).DefaultIfEmpty(-1).Max();
                var share = top >= 0 ? area[top] / total : 0f;
                var sill = area.Where(p => p.Key * 0.02f > 0.3f && p.Key * 0.02f < 1.6f).OrderByDescending(p => p.Value).Select(p => p.Key * 0.02f).DefaultIfEmpty(-1f).First();
                Out.AppendLine($"{key} | {b.size.x:0.00} x {b.size.z:0.00} x {b.size.y:0.00} | {(top >= 0 ? (top * 0.02f).ToString("0.00") : "-")} | {share:0.00}" +
                               (key.Contains("Window") ? $" | sill {sill:0.00}" : ""));
                Object.DestroyImmediate(go);
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The cathedral placeholder's Gothic dress (VEILMOUTH_CITY_PLAN.md §35; WORLD_STYLE_GUIDE.md §6, the Gothic skyline
    /// the owner chose). The cathedral stays the four stone masses of §28.9, with their colliders, until its model comes
    /// (SLICE_MODELS_WANTED.md § C4). Over them stands what makes the masses read as a Gothic church from the streets, the
    /// pier and the Penny:
    /// <list type="bullet">
    /// <item>a slate gable roof over the nave, the transept and the apse, at 45°;</item>
    /// <item>on the tower, a cornice, a crenellated parapet, four corner pinnacles, and an octagonal slate spire with an iron cross;</item>
    /// <item>buttresses and lancet windows in bays along the walls, a string course and a cornice;</item>
    /// <item>on the front, a pointed portal, two lancets beside it, and a rose window.</item>
    /// </list>
    /// One merged mesh for each material, on NoGrid with no collider, so nothing in play changes. Every size is an
    /// authoring default, in metres.
    /// </summary>
    public static partial class VeilmouthCity
    {
        private const string CathedralGroup = "The cathedral (placeholder)", DressName = "The cathedral - Gothic dress";
        private const string RoofSlate = "VIC_Slate_Rough", DarkGlass = "VIC_Iron", PortalWood = "VIC_Wood_Oxblood", FrameStone = "VIC_Stone_Medium";

        private const float RoofRise = 1f, Eaves = 0.4f, CathedralBay = 6f;
        private const float LancetWidth = 1.6f, LancetSill = 5f, LancetHead = 5f, LancetPoint = 1.3f;
        private const float ButtressWidth = 1.1f, ButtressDepth = 1.3f, ButtressBelowTop = 2.5f, ButtressCap = 1.6f;
        private const float Pinnacle = 2.4f, PinnacleShaft = 4.5f, PinnacleCap = 4f, SpireHeight = 24f, SpireClear = 3.2f;
        private const float RoseHeight = 20f, RoseRadius = 3.4f, PortalWidth = 5f, PortalHeight = 6f, PortalPoint = 2.6f;

        /// <summary>
        /// Dresses the cathedral of the saved Veilmouth scene again, from its masses' colliders, and saves the scene.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthCity.DressCathedral
        /// </summary>
        public static void DressCathedral()
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var city = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            var group = city != null ? city.transform.Find(CathedralGroup) : null;
            if (group == null)
            {
                Debug.LogError($"[VeilmouthCity] no '{CathedralGroup}' under {RootName}.");
                return;
            }

            var masses = group.GetComponents<BoxCollider>().Select(c =>
            {
                var a = c.transform.TransformPoint(c.center - (c.size / 2f));
                var b = c.transform.TransformPoint(c.center + (c.size / 2f));
                var bounds = new Bounds((a + b) / 2f, Vector3.zero);
                bounds.Encapsulate(a);
                bounds.Encapsulate(b);
                return bounds;
            }).ToList();
            GothicDress(group, masses);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>Builds the dress over <paramref name="masses"/>, the cathedral's stone boxes in world space, replacing an older one.</summary>
        private static void GothicDress(Transform group, IReadOnlyList<Bounds> masses)
        {
            var old = group.Find(DressName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            if (masses.Count == 0)
            {
                return;
            }

            var owner = new GameObject(DressName).transform;
            owner.SetParent(group, false);
            var dress = new Works(owner, false);
            var tower = masses.OrderByDescending(m => m.max.y).First();
            var body = masses.Where(m => m != tower).ToList();
            var bodyCentre = body.Count > 0 ? body.Aggregate(Vector3.zero, (s, m) => s + m.center) / body.Count : tower.center;
            var away = tower.center - bodyCentre;
            var front = Mathf.Abs(away.x) > Mathf.Abs(away.z) ? new Vector3(Mathf.Sign(away.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(away.z));
            var count = new int[3]; // lancets, buttresses, roofs
            foreach (var m in body)
            {
                Roof(dress, m);
                count[2]++;
                foreach (var wall in Walls(m))
                {
                    Bays(dress, m, wall, masses, count);
                }
            }

            Tower(dress, tower, front, count);
            dress.Finish(CathedralStone);
            Debug.Log($"[VeilmouthCity] the cathedral's Gothic dress: {count[2]} roofs, {count[0]} lancets, {count[1]} buttresses, the spire's cross {tower.max.y + SpireHeight + 2f - GroundOf(tower):0.0} m over the ground, the front facing {front}.");
        }

        private readonly struct Wall
        {
            public readonly Vector3 Start, Along, Out;
            public readonly float Length;

            public Wall(Vector3 start, Vector3 along, Vector3 outward, float length)
            {
                Start = start;
                Along = along;
                Out = outward;
                Length = length;
            }

            public Vector3 At(float s, float y) => new Vector3(Start.x, y, Start.z) + (Along * s);
        }

        private static IEnumerable<Wall> Walls(Bounds m)
        {
            yield return new Wall(new Vector3(m.min.x, 0f, m.min.z), Vector3.right, Vector3.back, m.size.x);
            yield return new Wall(new Vector3(m.min.x, 0f, m.max.z), Vector3.right, Vector3.forward, m.size.x);
            yield return new Wall(new Vector3(m.min.x, 0f, m.min.z), Vector3.forward, Vector3.left, m.size.z);
            yield return new Wall(new Vector3(m.max.x, 0f, m.min.z), Vector3.forward, Vector3.right, m.size.z);
        }

        private static float GroundOf(Bounds m) => m.min.y + 0.5f;

        /// <summary>Whether <paramref name="p"/> lies inside a mass other than <paramref name="own"/>, so what stands there is hidden.</summary>
        private static bool Hidden(Vector3 p, Bounds own, IReadOnlyList<Bounds> masses) => masses.Any(m => m != own
            && p.x > m.min.x - 0.05f && p.x < m.max.x + 0.05f && p.z > m.min.z - 0.05f && p.z < m.max.z + 0.05f && p.y < m.max.y);

        private static void Roof(Works dress, Bounds m)
        {
            var t = m.max.y;
            var alongX = m.size.x >= m.size.z;
            var half = (alongX ? m.size.z : m.size.x) / 2f;
            var rise = half * RoofRise;
            var eave = t - (Eaves * RoofRise);
            if (alongX)
            {
                var zc = m.center.z;
                float x0 = m.min.x - Eaves, x1 = m.max.x + Eaves;
                dress.Poly(new[] { new Vector3(x0, eave, m.min.z - Eaves), new Vector3(x1, eave, m.min.z - Eaves), new Vector3(x1, t + rise, zc), new Vector3(x0, t + rise, zc) }, new Vector3(0f, 1f, -1f), RoofSlate, true);
                dress.Poly(new[] { new Vector3(x0, eave, m.max.z + Eaves), new Vector3(x1, eave, m.max.z + Eaves), new Vector3(x1, t + rise, zc), new Vector3(x0, t + rise, zc) }, new Vector3(0f, 1f, 1f), RoofSlate, true);
                dress.Poly(new[] { new Vector3(m.min.x, t, m.min.z), new Vector3(m.min.x, t, m.max.z), new Vector3(m.min.x, t + rise, zc) }, Vector3.left);
                dress.Poly(new[] { new Vector3(m.max.x, t, m.min.z), new Vector3(m.max.x, t, m.max.z), new Vector3(m.max.x, t + rise, zc) }, Vector3.right);
            }
            else
            {
                var xc = m.center.x;
                float z0 = m.min.z - Eaves, z1 = m.max.z + Eaves;
                dress.Poly(new[] { new Vector3(m.min.x - Eaves, eave, z0), new Vector3(m.min.x - Eaves, eave, z1), new Vector3(xc, t + rise, z1), new Vector3(xc, t + rise, z0) }, new Vector3(-1f, 1f, 0f), RoofSlate, true);
                dress.Poly(new[] { new Vector3(m.max.x + Eaves, eave, z0), new Vector3(m.max.x + Eaves, eave, z1), new Vector3(xc, t + rise, z1), new Vector3(xc, t + rise, z0) }, new Vector3(1f, 1f, 0f), RoofSlate, true);
                dress.Poly(new[] { new Vector3(m.min.x, t, m.min.z), new Vector3(m.max.x, t, m.min.z), new Vector3(xc, t + rise, m.min.z) }, Vector3.back);
                dress.Poly(new[] { new Vector3(m.min.x, t, m.max.z), new Vector3(m.max.x, t, m.max.z), new Vector3(xc, t + rise, m.max.z) }, Vector3.forward);
            }
        }

        /// <summary>A wall's bays: buttresses between lancets, a string course under the sills and a cornice at the top.</summary>
        private static void Bays(Works dress, Bounds m, Wall wall, IReadOnlyList<Bounds> masses, int[] count)
        {
            var ground = GroundOf(m);
            var top = m.max.y;
            var bays = Mathf.FloorToInt(wall.Length / CathedralBay);
            var margin = (wall.Length - (bays * CathedralBay)) / 2f;
            Band(dress, wall, ground + LancetSill - 0.6f, 0.35f, 0.5f);
            Band(dress, wall, top - 0.25f, 0.5f, 0.7f);
            for (var k = 0; k <= bays; k++)
            {
                var s = margin + (k * CathedralBay);
                var foot = wall.At(s, ground + 1f) + (wall.Out * (ButtressDepth * 0.6f));
                var head = wall.At(s, top - ButtressBelowTop) + (wall.Out * (ButtressDepth * 0.6f));
                if (s > 1.5f && s < wall.Length - 1.5f && !Hidden(foot, m, masses) && !Hidden(head, m, masses))
                {
                    var y0 = ground - 0.3f;
                    var y1 = top - ButtressBelowTop;
                    var c = wall.At(s, (y0 + y1) / 2f) + (wall.Out * (ButtressDepth / 2f));
                    dress.Box(c, Size(wall, ButtressWidth, y1 - y0, ButtressDepth), 0f, false);
                    Pyramid(dress, wall.At(s, y1) + (wall.Out * (ButtressDepth / 2f)), Size(wall, ButtressWidth, 0f, ButtressDepth) / 2f, ButtressCap, null);
                    count[1]++;
                }

                if (k == bays)
                {
                    continue;
                }

                var mid = s + (CathedralBay / 2f);
                var sill = ground + LancetSill;
                var lintel = top - LancetHead;
                if (lintel - sill < 3f || Hidden(wall.At(mid, sill) + (wall.Out * 0.3f), m, masses) || Hidden(wall.At(mid, lintel + LancetPoint) + (wall.Out * 0.3f), m, masses))
                {
                    continue;
                }

                Arch(dress, wall.At(mid, sill), wall.Along, wall.Out, LancetWidth, lintel - sill, LancetPoint, DarkGlass, 0.05f);
                count[0]++;
            }
        }

        private static Vector3 Size(Wall wall, float along, float height, float depth) =>
            new Vector3(Mathf.Abs(wall.Along.x) > 0.5f ? along : depth, height, Mathf.Abs(wall.Along.x) > 0.5f ? depth : along);

        private static void Band(Works dress, Wall wall, float y, float height, float depth) =>
            dress.Box(wall.At(wall.Length / 2f, y), Size(wall, wall.Length + depth, height, depth), 0f, false);

        private static void Tower(Works dress, Bounds tower, Vector3 front, int[] count)
        {
            var t = tower.max.y;
            var ground = GroundOf(tower);
            foreach (var wall in Walls(tower))
            {
                Band(dress, wall, t - 0.3f, 0.6f, 0.8f);
                Band(dress, wall, t - 16f, 0.4f, 0.5f);
                // A crenellated parapet: a low band and merlons of 1 m every 2 m, their outer faces on the wall's.
                dress.Box(wall.At(wall.Length / 2f, t + 0.25f) - (wall.Out * 0.3f), Size(wall, wall.Length, 0.5f, 0.6f), 0f, false);
                for (var s = 1.5f; s < wall.Length - 1f; s += 2f)
                {
                    dress.Box(wall.At(s, t + 0.9f) - (wall.Out * 0.3f), Size(wall, 1f, 0.8f, 0.6f), 0f, false);
                }

                if (Vector3.Dot(wall.Out, front) < -0.5f)
                {
                    continue; // the face toward the nave meets its roof
                }

                // Two tall belfry lancets on each free face.
                foreach (var side in new[] { -1f, 1f })
                {
                    Arch(dress, wall.At((wall.Length / 2f) + (side * wall.Length * 0.2f), t - 13f), wall.Along, wall.Out, 1.8f, 7.5f, 1.4f, DarkGlass, 0.05f);
                    count[0]++;
                }

                if (Vector3.Dot(wall.Out, front) > 0.5f)
                {
                    Front(dress, wall, ground, count);
                }
            }

            // Corner pinnacles, each a shaft and a pyramid, flush with the tower's faces.
            foreach (var (cx, cz) in new[] { (tower.min.x, tower.min.z), (tower.min.x, tower.max.z), (tower.max.x, tower.min.z), (tower.max.x, tower.max.z) })
            {
                var x = cx + (cx < tower.center.x ? Pinnacle / 2f : -Pinnacle / 2f);
                var z = cz + (cz < tower.center.z ? Pinnacle / 2f : -Pinnacle / 2f);
                dress.Box(new Vector3(x, t + (PinnacleShaft / 2f), z), new Vector3(Pinnacle, PinnacleShaft, Pinnacle), 0f, false);
                Pyramid(dress, new Vector3(x, t + PinnacleShaft, z), new Vector3(Pinnacle / 2f, 0f, Pinnacle / 2f), PinnacleCap, RoofSlate);
            }

            // The spire: an octagonal slate pyramid inside the pinnacles, and an iron cross at its point.
            var radius = (Mathf.Min(tower.size.x, tower.size.z) / 2f) - SpireClear;
            var apex = new Vector3(tower.center.x, t + SpireHeight, tower.center.z);
            for (var k = 0; k < 8; k++)
            {
                var a0 = (22.5f + (k * 45f)) * Mathf.Deg2Rad;
                var a1 = (22.5f + ((k + 1) * 45f)) * Mathf.Deg2Rad;
                var p0 = new Vector3(tower.center.x + (Mathf.Cos(a0) * radius), t, tower.center.z + (Mathf.Sin(a0) * radius));
                var p1 = new Vector3(tower.center.x + (Mathf.Cos(a1) * radius), t, tower.center.z + (Mathf.Sin(a1) * radius));
                var mid = (a0 + a1) / 2f;
                dress.Poly(new[] { p0, p1, apex }, new Vector3(Mathf.Cos(mid), radius / SpireHeight, Mathf.Sin(mid)), RoofSlate);
            }

            dress.Box(apex + (Vector3.up * 1f), new Vector3(0.2f, 2f, 0.2f), 0f, false, DarkGlass);
            var acrossX = Mathf.Abs(front.x) < 0.5f;
            dress.Box(apex + (Vector3.up * 1.35f), new Vector3(acrossX ? 0.9f : 0.2f, 0.2f, acrossX ? 0.2f : 0.9f), 0f, false, DarkGlass);
        }

        /// <summary>The front: a pointed portal in a stone frame, a lancet either side of it and the rose window above.</summary>
        private static void Front(Works dress, Wall wall, float ground, int[] count)
        {
            var mid = wall.Length / 2f;
            Arch(dress, wall.At(mid, ground - 0.1f), wall.Along, wall.Out, PortalWidth + 1.2f, PortalHeight + 0.3f, PortalPoint + 0.6f, FrameStone, 0.03f);
            Arch(dress, wall.At(mid, ground - 0.1f), wall.Along, wall.Out, PortalWidth, PortalHeight, PortalPoint, PortalWood, 0.06f);
            foreach (var side in new[] { -1f, 1f })
            {
                Arch(dress, wall.At(mid + (side * 6f), ground + 3f), wall.Along, wall.Out, LancetWidth, 9f, LancetPoint, DarkGlass, 0.05f);
                count[0]++;
            }

            var centre = wall.At(mid, ground + RoseHeight);
            Disc(dress, centre + (wall.Out * 0.03f), wall.Along, wall.Out, RoseRadius + 0.6f, FrameStone);
            Disc(dress, centre + (wall.Out * 0.06f), wall.Along, wall.Out, RoseRadius, DarkGlass);
            for (var k = 0; k < 6; k++)
            {
                var a = k * 30f * Mathf.Deg2Rad;
                var u = (wall.Along * Mathf.Cos(a)) + (Vector3.up * Mathf.Sin(a));
                var v = Vector3.Cross(wall.Out, u).normalized;
                Rect(dress, centre + (wall.Out * 0.09f), u, v, RoseRadius, 0.11f, wall.Out, FrameStone);
            }

            Disc(dress, centre + (wall.Out * 0.1f), wall.Along, wall.Out, 0.7f, FrameStone);
        }

        /// <summary>A pointed (equilateral) arch on a wall: its sill's middle at <paramref name="foot"/>, flat and just proud.</summary>
        private static void Arch(Works dress, Vector3 foot, Vector3 along, Vector3 outward, float width, float height, float point, string material, float proud)
        {
            var half = width / 2f;
            var lift = point / Mathf.Sin(60f * Mathf.Deg2Rad);
            var loop = new List<Vector2> { new Vector2(-half, 0f), new Vector2(half, 0f) };
            for (var k = 0; k <= 4; k++)
            {
                var phi = k * 15f * Mathf.Deg2Rad;
                loop.Add(new Vector2(-half + (width * Mathf.Cos(phi)), height + (lift * Mathf.Sin(phi))));
            }

            for (var k = 3; k >= 0; k--)
            {
                var phi = k * 15f * Mathf.Deg2Rad;
                loop.Add(new Vector2(half - (width * Mathf.Cos(phi)), height + (lift * Mathf.Sin(phi))));
            }

            dress.Poly(loop.Select(p => foot + (along * p.x) + (Vector3.up * p.y) + (outward * proud)).ToList(), outward, material);
        }

        private static void Disc(Works dress, Vector3 centre, Vector3 along, Vector3 outward, float radius, string material)
        {
            var loop = new List<Vector3>();
            for (var k = 0; k < 24; k++)
            {
                var a = k * 15f * Mathf.Deg2Rad;
                loop.Add(centre + (along * (Mathf.Cos(a) * radius)) + (Vector3.up * (Mathf.Sin(a) * radius)));
            }

            dress.Poly(loop, outward, material);
        }

        private static void Rect(Works dress, Vector3 centre, Vector3 u, Vector3 v, float halfU, float halfV, Vector3 outward, string material) =>
            dress.Poly(new[] { centre - (u * halfU) - (v * halfV), centre + (u * halfU) - (v * halfV), centre + (u * halfU) + (v * halfV), centre - (u * halfU) + (v * halfV) }, outward, material);

        /// <summary>A pyramid with no base over <paramref name="baseCentre"/>, its half sizes in x and z.</summary>
        private static void Pyramid(Works dress, Vector3 baseCentre, Vector3 half, float height, string material)
        {
            var apex = baseCentre + (Vector3.up * height);
            var c = new[]
            {
                baseCentre + new Vector3(-half.x, 0f, -half.z), baseCentre + new Vector3(half.x, 0f, -half.z),
                baseCentre + new Vector3(half.x, 0f, half.z), baseCentre + new Vector3(-half.x, 0f, half.z),
            };
            var outs = new[] { new Vector3(0f, half.z / height, -1f), new Vector3(1f, half.x / height, 0f), new Vector3(0f, half.z / height, 1f), new Vector3(-1f, half.x / height, 0f) };
            for (var k = 0; k < 4; k++)
            {
                dress.Poly(new[] { c[k], c[(k + 1) % 4], apex }, outs[k], material);
            }
        }
    }
}

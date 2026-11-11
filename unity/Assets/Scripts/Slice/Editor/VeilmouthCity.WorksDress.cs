using System.Collections.Generic;
using System.Linq;
using GameDev.World.Portals;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The works' placeholder dress (VEILMOUTH_CITY_PLAN.md §36; WORLD_STYLE_GUIDE.md §2, sooty brick in the Reek). The
    /// works stay §28.9's brick masses, with their colliders, until their models come (SLICE_MODELS_WANTED.md § C4). Over
    /// them stands what makes a Victorian works read from its yards and from across the city:
    /// <list type="bullet">
    /// <item>brick piers every 5 m and at the corners, a stone plinth, a corbel course and a stone cornice;</item>
    /// <item>tall round-headed windows of dark iron glazing with stone sills, two rows where the wall is high enough;</item>
    /// <item>north-light sawtooth roofs over the deep sheds, their glazing turned north, and slate gables over the narrow ones;</item>
    /// <item>one tapering brick chimney stack of 30–36 m for each works, from its tallest shed, with no smoke
    /// (the owner did not choose the soot layer).</item>
    /// </list>
    /// One merged mesh for each material, on NoGrid with no collider: the piers and the plinth stand at most 0.15 m
    /// proud, and the stacks rise out of the roofs. Nothing stands within reach of a door. Every size is an authoring
    /// default, in metres.
    /// </summary>
    public static partial class VeilmouthCity
    {
        private const string WorksGroup = "The works (placeholder)", WorksDressName = "The works - dress", WorksStone = "VIC_Stone_Dirty";
        private const float WorksBay = 5f, PierWidth = 0.7f, PierProud = 0.15f, WorksWindow = 2f, WorksSill = 3.2f, WorksArch = 1f;
        private const float SawTooth = 7f, SawRise = 3.2f, SawLeast = 12f;
        private const float StackLeast = 12.9f, StackFrom = 30f, StackSpread = 6f, StackBase = 3f, StackTop = 2f, StackEnd = 4f;
        private const float DoorKeep = 3.5f;

        /// <summary>
        /// Dresses the works of the saved Veilmouth scene again, from their masses' colliders, and saves the scene.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthCity.DressWorks
        /// </summary>
        public static void DressWorks()
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var city = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            var group = city != null ? city.transform.Find(WorksGroup) : null;
            if (group == null)
            {
                Debug.LogError($"[VeilmouthCity] no '{WorksGroup}' under {RootName}.");
                return;
            }

            WorksDressOf(group);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>Builds the works' dress over the masses of <paramref name="group"/>, keeping clear of every portal's door.</summary>
        private static void WorksDressOf(Transform group)
        {
            var masses = group.GetComponents<BoxCollider>().Select(c =>
            {
                var bounds = new Bounds(c.transform.TransformPoint(c.center), Vector3.zero);
                bounds.Encapsulate(c.transform.TransformPoint(c.center - (c.size / 2f)));
                bounds.Encapsulate(c.transform.TransformPoint(c.center + (c.size / 2f)));
                return bounds;
            }).OrderBy(m => m.center.x).ThenBy(m => m.center.z).ToList();
            var doors = Object.FindObjectsByType<Portal>(FindObjectsInactive.Include).Select(p => p.transform.position).ToList();
            WorksDress(group, masses, doors);
        }

        private static void WorksDress(Transform group, IReadOnlyList<Bounds> masses, IReadOnlyList<Vector3> doors)
        {
            var old = group.Find(WorksDressName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            if (masses.Count == 0)
            {
                return;
            }

            var owner = new GameObject(WorksDressName).transform;
            owner.SetParent(group, false);
            var dress = new Works(owner, false);
            var count = new int[4]; // windows, piers, sawtooth roofs, stacks
            for (var i = 0; i < masses.Count; i++)
            {
                var m = masses[i];
                if (Mathf.Min(m.size.x, m.size.z) >= SawLeast)
                {
                    SawRoof(dress, m);
                    count[2]++;
                }
                else
                {
                    Roof(dress, m);
                }

                foreach (var wall in Walls(m))
                {
                    WorksWall(dress, m, wall, masses, doors, count);
                }

                if (m.max.y - GroundOf(m) >= StackLeast)
                {
                    Stack(dress, m, count[3]);
                    count[3]++;
                }
            }

            dress.Finish(WorksBrick);
            Debug.Log($"[VeilmouthCity] the works' dress: {count[0]} windows, {count[1]} piers, {count[2]} sawtooth roofs, {count[3]} chimney stacks.");
        }

        private static bool NearDoor(Vector3 p, IReadOnlyList<Vector3> doors) =>
            doors.Any(d => new Vector2(d.x - p.x, d.z - p.z).sqrMagnitude < DoorKeep * DoorKeep);

        private static void WorksWall(Works dress, Bounds m, Wall wall, IReadOnlyList<Bounds> masses, IReadOnlyList<Vector3> doors, int[] count)
        {
            var ground = GroundOf(m);
            var top = m.max.y;
            Band(dress, wall, ground + 0.3f, 1.2f, PierProud * 2f * 0.8f, WorksStone);
            Band(dress, wall, top - 1.1f, 0.3f, 0.3f);
            Band(dress, wall, top - 0.35f, 0.7f, 0.6f, WorksStone);
            var bays = Mathf.Max(1, Mathf.FloorToInt(wall.Length / WorksBay));
            var bay = wall.Length / bays;
            for (var k = 0; k <= bays; k++)
            {
                var s = Mathf.Clamp(k * bay, PierWidth / 2f, wall.Length - (PierWidth / 2f));
                var probe = wall.At(s, ground + 1f) + (wall.Out * 0.3f);
                if (!Hidden(probe, m, masses) && !NearDoor(probe, doors))
                {
                    var y0 = ground - 0.3f;
                    var y1 = top - 1.1f;
                    dress.Box(wall.At(s, (y0 + y1) / 2f), Size(wall, PierWidth, y1 - y0, PierProud * 2f), 0f, false);
                    count[1]++;
                }

                if (k == bays)
                {
                    continue;
                }

                var mid = (k + 0.5f) * bay;
                var height = top - ground;
                var rows = new List<(float Sill, float Rect)>();
                if (height >= 11f)
                {
                    rows.Add((WorksSill, 3.4f));
                    var second = WorksSill + 4.8f;
                    var rect = Mathf.Min(3.4f, height - 2.4f - second - WorksArch);
                    if (rect >= 1.5f)
                    {
                        rows.Add((second, rect));
                    }
                }
                else
                {
                    rows.Add((WorksSill, height - 2.2f - WorksSill - WorksArch));
                }

                foreach (var (sill, rect) in rows.Where(r => r.Rect >= 1.5f))
                {
                    var foot = wall.At(mid, ground + sill);
                    var head = wall.At(mid, ground + sill + rect + WorksArch);
                    if (Hidden(foot + (wall.Out * 0.3f), m, masses) || Hidden(head + (wall.Out * 0.3f), m, masses) || NearDoor(foot, doors))
                    {
                        continue;
                    }

                    dress.Box(foot - (Vector3.up * 0.09f), Size(wall, WorksWindow + 0.4f, 0.18f, 0.3f), 0f, false, WorksStone);
                    RoundArch(dress, foot, wall.Along, wall.Out, WorksWindow, rect, Glazing, 0.05f);
                    count[0]++;
                }
            }
        }

        /// <summary>A north-light sawtooth roof: teeth across the shed's depth, each a slate slope rising north to a glazed face.</summary>
        private static void SawRoof(Works dress, Bounds m)
        {
            var t = m.max.y;
            var alongX = m.size.x >= m.size.z;
            var depth = alongX ? m.size.z : m.size.x;
            var teeth = Mathf.Max(1, Mathf.RoundToInt(depth / SawTooth));
            var w = depth / teeth;
            for (var i = 0; i < teeth; i++)
            {
                if (alongX)
                {
                    float z0 = m.min.z + (i * w), z1 = z0 + w, x0 = m.min.x, x1 = m.max.x;
                    dress.Poly(new[] { new Vector3(x0, t, z0), new Vector3(x1, t, z0), new Vector3(x1, t + SawRise, z1), new Vector3(x0, t + SawRise, z1) }, new Vector3(0f, w, -SawRise), RoofSlate);
                    dress.Poly(new[] { new Vector3(x0, t, z1), new Vector3(x1, t, z1), new Vector3(x1, t + SawRise, z1), new Vector3(x0, t + SawRise, z1) }, Vector3.forward, Glazing);
                    dress.Poly(new[] { new Vector3(x0, t, z0), new Vector3(x0, t, z1), new Vector3(x0, t + SawRise, z1) }, Vector3.left);
                    dress.Poly(new[] { new Vector3(x1, t, z0), new Vector3(x1, t, z1), new Vector3(x1, t + SawRise, z1) }, Vector3.right);
                }
                else
                {
                    float x0 = m.min.x + (i * w), x1 = x0 + w, z0 = m.min.z, z1 = m.max.z;
                    dress.Poly(new[] { new Vector3(x0, t, z0), new Vector3(x0, t, z1), new Vector3(x1, t + SawRise, z1), new Vector3(x1, t + SawRise, z0) }, new Vector3(-SawRise, w, 0f), RoofSlate);
                    dress.Poly(new[] { new Vector3(x1, t, z0), new Vector3(x1, t, z1), new Vector3(x1, t + SawRise, z1), new Vector3(x1, t + SawRise, z0) }, Vector3.right, Glazing);
                    dress.Poly(new[] { new Vector3(x0, t, z0), new Vector3(x1, t, z0), new Vector3(x1, t + SawRise, z0) }, Vector3.back);
                    dress.Poly(new[] { new Vector3(x0, t, z1), new Vector3(x1, t, z1), new Vector3(x1, t + SawRise, z1) }, Vector3.forward);
                }
            }
        }

        /// <summary>A tapering brick stack rising out of the shed's roof near one end, with a corbelled cap and a dark flue.</summary>
        private static void Stack(Works dress, Bounds m, int index)
        {
            var alongX = m.size.x >= m.size.z;
            var end = index % 2 == 0 ? -1f : 1f;
            var centre = alongX
                ? new Vector3(m.center.x + (end * ((m.size.x / 2f) - StackEnd)), 0f, m.center.z)
                : new Vector3(m.center.x, 0f, m.center.z + (end * ((m.size.z / 2f) - StackEnd)));
            var y0 = m.max.y - 0.5f;
            var y1 = GroundOf(m) + StackFrom + (StackSpread * Mathf.Repeat(index * 0.618f, 1f));
            float b = StackBase / 2f, a = StackTop / 2f;
            var lift = (b - a) / (y1 - y0);
            var bottom = new[] { new Vector3(-b, 0f, -b), new Vector3(b, 0f, -b), new Vector3(b, 0f, b), new Vector3(-b, 0f, b) };
            var outs = new[] { new Vector3(0f, lift, -1f), new Vector3(1f, lift, 0f), new Vector3(0f, lift, 1f), new Vector3(-1f, lift, 0f) };
            for (var k = 0; k < 4; k++)
            {
                var p0 = bottom[k];
                var p1 = bottom[(k + 1) % 4];
                dress.Poly(new[]
                {
                    centre + p0 + (Vector3.up * y0), centre + p1 + (Vector3.up * y0),
                    centre + (p1 * (a / b)) + (Vector3.up * y1), centre + (p0 * (a / b)) + (Vector3.up * y1),
                }, outs[k]);
            }

            dress.Box(centre + (Vector3.up * (y1 - 0.45f)), new Vector3(StackTop + 0.5f, 0.9f, StackTop + 0.5f), 0f, false);
            dress.Box(centre + (Vector3.up * (y1 - 2.2f)), new Vector3(StackTop + 0.3f, 0.35f, StackTop + 0.3f), 0f, false, DarkGlass);
            var f = (StackTop / 2f) - 0.3f;
            dress.Poly(new[] { centre + new Vector3(-f, y1 + 0.01f, -f), centre + new Vector3(f, y1 + 0.01f, -f), centre + new Vector3(f, y1 + 0.01f, f), centre + new Vector3(-f, y1 + 0.01f, f) }, Vector3.up, DarkGlass);
        }

        /// <summary>A round-headed window on a wall: its sill's middle at <paramref name="foot"/>, upright to <paramref name="rect"/> and a half circle over it.</summary>
        private static void RoundArch(Works dress, Vector3 foot, Vector3 along, Vector3 outward, float width, float rect, string material, float proud)
        {
            var half = width / 2f;
            var loop = new List<Vector2> { new Vector2(-half, 0f), new Vector2(half, 0f) };
            for (var k = 0; k <= 8; k++)
            {
                var phi = k * 22.5f * Mathf.Deg2Rad;
                loop.Add(new Vector2(half * Mathf.Cos(phi), rect + (half * Mathf.Sin(phi))));
            }

            dress.Poly(loop.Select(p => foot + (along * p.x) + (Vector3.up * p.y) + (outward * proud)).ToList(), outward, material);
        }
    }
}

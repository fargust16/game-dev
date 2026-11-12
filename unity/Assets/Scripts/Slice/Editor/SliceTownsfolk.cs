using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameDev.Slice.Kit;
using GameDev.Slice.People;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The ordinary townsfolk (owner, 2026-11-06): each walks a closed route round its place until S5c gives it a
    /// schedule. A route is planned on 1 m cells round the person's place, over ground a person's capsule fits on, then
    /// straightened where a person fits along the straight line. The first townsfolk models, the stylized man and the
    /// historical woman, and the builders that made them are retired (owner, 2026-11-12); the townsfolk wear the
    /// people's looks of <see cref="SlicePeopleModels"/>.
    /// </summary>
    public static class SliceTownsfolk
    {
        /// <summary>
        /// The ordinary townsman and townswoman walking: the gentleman's and the working woman's walkers since the owner
        /// retired the stylized man and the historical woman, the first townsfolk models, as out of style (2026-11-12).
        /// </summary>
        public const string PrefabPath = "Assets/Prefabs/Slice/People/GentlemanWalker.prefab";

        public const string WomanPrefabPath = "Assets/Prefabs/Slice/People/WorkingWomanWalker.prefab";

        // Authoring defaults, 2026-11-06.
        public const float Height = 1.75f;       // m
        private const float Radius = 0.3f;       // the controller's radius (kit standard §3)
        private const float Step = 0.4f;         // the controller's step (kit standard §3)
        private const int Reach = 22;            // cells round the person's place a route may use
        private const float LoopMin = 8f;        // m from the place to a turning point
        private const float LoopMax = 18f;       // m

        private static int BlockMask =>
            LayerMask.GetMask("Default", KitLayers.Ground, KitLayers.Structure, KitLayers.Prop, KitLayers.Vegetation, KitLayers.Interactable, KitLayers.Character);

        public static LayerMask GroundMask => LayerMask.GetMask(KitLayers.Ground, KitLayers.Structure);

        // What stands in a person's way above the line it walks: the ground's own bumps are left to the feet.
        private static int StandMask =>
            LayerMask.GetMask("Default", KitLayers.Structure, KitLayers.Prop, KitLayers.Vegetation, KitLayers.Interactable);

        /// <summary>The saved townsfolk prefabs (the loose-props pass patches them in place).</summary>
        internal static readonly string[] Prefabs = { PrefabPath, WomanPrefabPath };

        /// <summary>
        /// A walker shoulders loose props aside (kit standard §4): a kinematic body, which the walker still moves by its
        /// transform, so a prop it walks into wakes and is pushed out of its way.
        /// </summary>
        internal static void ShoulderLooseProps(GameObject root)
        {
            if (!root.TryGetComponent<Rigidbody>(out var body))
            {
                body = root.AddComponent<Rigidbody>();
            }

            body.isKinematic = true;
            body.useGravity = false;
        }

        // ------------------------------------------------------------------------------------------- routes

        /// <summary>
        /// A closed route round <paramref name="place"/>: up to four turning points 8–18 m away, one in each quarter,
        /// joined by the shortest walkable cell paths, then straightened. With fewer than two turning points the person
        /// walks out to the farthest one and back.
        /// </summary>
        public static Vector3[] PlanRoute(Vector3 place)
        {
            var cx = Mathf.FloorToInt(place.x);
            var cz = Mathf.FloorToInt(place.z);
            var cells = new Dictionary<(int, int), float>();
            if (!TrySurface(cx, cz, place.y, out var y0))
            {
                return new Vector3[0];
            }

            // Every cell reachable from the place, with its floor height.
            cells[(cx, cz)] = y0;
            var queue = new Queue<(int, int)>();
            queue.Enqueue((cx, cz));
            while (queue.Count > 0)
            {
                var (x, z) = queue.Dequeue();
                foreach (var (nx, nz) in Neighbours(x, z))
                {
                    if (cells.ContainsKey((nx, nz)) || Mathf.Abs(nx - cx) > Reach || Mathf.Abs(nz - cz) > Reach)
                    {
                        continue;
                    }

                    if (TrySurface(nx, nz, cells[(x, z)], out var ny) && Passable(Centre(x, z, cells[(x, z)]), Centre(nx, nz, ny)))
                    {
                        cells[(nx, nz)] = ny;
                        queue.Enqueue((nx, nz));
                    }
                }
            }

            var home = Paths(cells, (cx, cz));
            var turns = new List<(int, int)>();
            for (var quarter = 0; quarter < 4; quarter++)
            {
                var best = default((int, int));
                var bestScore = -1f;
                foreach (var cell in cells.Keys)
                {
                    var d = new Vector2(cell.Item1 - cx, cell.Item2 - cz);
                    var distance = d.magnitude;
                    if (distance < LoopMin || distance > LoopMax || Quarter(d) != quarter
                        || !home.Distance.TryGetValue(cell, out var walked) || walked > distance * 1.6f)
                    {
                        continue;
                    }

                    if (distance > bestScore)
                    {
                        bestScore = distance;
                        best = cell;
                    }
                }

                if (bestScore > 0f)
                {
                    turns.Add(best);
                }
            }

            var path = new List<(int, int)>();
            if (turns.Count >= 2)
            {
                for (var i = 0; i < turns.Count && path != null; i++)
                {
                    var leg = Paths(cells, turns[i]).To(turns[(i + 1) % turns.Count]);
                    if (leg == null)
                    {
                        path = null;
                        break;
                    }

                    path.AddRange(leg.Take(leg.Count - 1));
                }
            }

            if (path == null || path.Count == 0)
            {
                path = new List<(int, int)>();
                var far = home.Distance.OrderByDescending(c => c.Value).First().Key;
                var leg = home.To(far);
                path.AddRange(leg);
                path.AddRange(Enumerable.Reverse(leg).Skip(1).Take(leg.Count - 2));
            }

            var points = path.Select(c => Centre(c.Item1, c.Item2, cells[c])).ToList();
            return Straighten(points, cells).ToArray();
        }

        private static IEnumerable<(int, int)> Neighbours(int x, int z)
        {
            yield return (x + 1, z);
            yield return (x - 1, z);
            yield return (x, z + 1);
            yield return (x, z - 1);
        }

        private static int Quarter(Vector2 d) => d.x >= 0 ? (d.y >= 0 ? 0 : 3) : (d.y >= 0 ? 1 : 2);

        private static Vector3 Centre(int x, int z, float y) => new Vector3(x + 0.5f, y, z + 0.5f);

        /// <summary>A floor a person stands on in this cell, near <paramref name="nearY"/>: on the ground or a building's
        /// floor, not too steep, and with room for the person's capsule.</summary>
        private static bool TrySurface(int x, int z, float nearY, out float y)
        {
            y = 0f;
            var origin = new Vector3(x + 0.5f, nearY + 1.2f, z + 0.5f);
            // A door's steps are no walkway: since every back door opens (city plan §26.1) a route would climb a house's
            // back steps and down again.
            if (!Physics.Raycast(origin, Vector3.down, out var hit, 2.4f, BlockMask, QueryTriggerInteraction.Ignore)
                || (GroundMask.value & (1 << hit.collider.gameObject.layer)) == 0
                || hit.collider.gameObject.name == "Stoop"
                || Vector3.Angle(hit.normal, Vector3.up) > 40f)
            {
                return false;
            }

            y = hit.point.y;
            return !Physics.CheckCapsule(hit.point + (Vector3.up * (Step + Radius)), hit.point + (Vector3.up * (Height - Radius)), Radius, BlockMask, QueryTriggerInteraction.Ignore);
        }

        private static bool Passable(Vector3 a, Vector3 b)
        {
            if (Mathf.Abs(b.y - a.y) > Step)
            {
                return false;
            }

            var delta = b - a;
            return !Physics.CapsuleCast(
                a + (Vector3.up * (Step + Radius)), a + (Vector3.up * (Height - Radius)), Radius,
                delta.normalized, delta.magnitude, BlockMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Drops the points a straight walk can skip: every 0.25 m of the straight line stands on a reachable
        /// cell, near its floor, with room for the person.</summary>
        private static List<Vector3> Straighten(List<Vector3> points, Dictionary<(int, int), float> cells)
        {
            if (points.Count < 3)
            {
                return points;
            }

            var result = new List<Vector3> { points[0] };
            var i = 0;
            while (i < points.Count - 1)
            {
                var j = i + 1;
                for (var k = Mathf.Min(points.Count - 1, i + 30); k > i + 1; k--)
                {
                    if (Straight(points[i], points[k], cells))
                    {
                        j = k;
                        break;
                    }
                }

                result.Add(points[j]);
                i = j;
            }

            // The loop closes from the last point to the first.
            if ((result[result.Count - 1] - result[0]).sqrMagnitude < 0.01f)
            {
                result.RemoveAt(result.Count - 1);
            }

            return result;
        }

        private static bool Straight(Vector3 a, Vector3 b, Dictionary<(int, int), float> cells)
        {
            var length = new Vector2(b.x - a.x, b.z - a.z).magnitude;
            var steps = Mathf.CeilToInt(length / 0.25f);
            var previousY = a.y;
            for (var s = 1; s < steps; s++)
            {
                var p = Vector3.Lerp(a, b, s / (float)steps);
                if (!cells.TryGetValue((Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.z)), out var y) || Mathf.Abs(y - previousY) > Step)
                {
                    return false;
                }

                // The person walks the straight line between the points, so its capsule must also fit at the line's own
                // height, not only on each cell's floor: a door's stone step 0.2 m up lifts the floor's capsule clear of
                // the stoop beside it while the line passes through its corner (owner's fifth play test, 2026-11-09).
                var line = p;
                p.y = y;
                if (Physics.CheckCapsule(p + (Vector3.up * (Step + Radius)), p + (Vector3.up * (Height - Radius)), Radius, BlockMask, QueryTriggerInteraction.Ignore)
                    || Physics.CheckCapsule(line + (Vector3.up * (Step + Radius)), line + (Vector3.up * (Height - Radius)), Radius, StandMask, QueryTriggerInteraction.Ignore))
                {
                    return false;
                }

                previousY = y;
            }

            return true;
        }

        private sealed class PathTree
        {
            public readonly Dictionary<(int, int), int> Distance = new Dictionary<(int, int), int>();
            public readonly Dictionary<(int, int), (int, int)> Parent = new Dictionary<(int, int), (int, int)>();
            public (int, int) Source;

            /// <summary>The cells from the source to <paramref name="target"/>, both included, or null if it is not reached.</summary>
            public List<(int, int)> To((int, int) target)
            {
                var path = new List<(int, int)> { target };
                while (!path[path.Count - 1].Equals(Source))
                {
                    if (!Parent.TryGetValue(path[path.Count - 1], out var parent))
                    {
                        return null;
                    }

                    path.Add(parent);
                }

                path.Reverse();
                return path;
            }
        }

        private static PathTree Paths(Dictionary<(int, int), float> cells, (int, int) source)
        {
            var tree = new PathTree { Source = source };
            tree.Distance[source] = 0;
            var queue = new Queue<(int, int)>();
            queue.Enqueue(source);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                foreach (var n in Neighbours(c.Item1, c.Item2))
                {
                    if (!cells.ContainsKey(n) || tree.Distance.ContainsKey(n)
                        || !Passable(Centre(c.Item1, c.Item2, cells[c]), Centre(n.Item1, n.Item2, cells[n])))
                    {
                        continue;
                    }

                    tree.Distance[n] = tree.Distance[c] + 1;
                    tree.Parent[n] = c;
                    queue.Enqueue(n);
                }
            }

            return tree;
        }
    }
}

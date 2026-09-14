#if UNITY_EDITOR
using System.Collections.Generic;
using GameDev.World.Hydrology;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Builds the visible water surface from a finished <see cref="HydrologyDefinition"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rendering only. Where the water is was decided by the analysis; this reads that decision and
    /// never becomes the record of it. The previous, removed hydrology made the opposite choice —
    /// per-lake scene objects were the truth — and every other system then had to search the scene
    /// to find out where water was.
    /// </para>
    /// <para>
    /// One mesh per river system and one per lake, not one per segment. A world of a few thousand
    /// river segments would otherwise become a few thousand GameObjects in the canonical scene,
    /// which is both a serialisation problem and a rendering one.
    /// </para>
    /// <para>
    /// Surface elevations come from the nodes' planned water level, which is derived from the same
    /// bed the carve was cut to. Resampling the terrain afterwards would let a half-pixel of
    /// rasterisation error show up as water hovering over its own channel.
    /// </para>
    /// </remarks>
    public static class InlandWaterMeshBuilder
    {
        public const string InlandRootName = "InlandWater";

        private const string WaterMaterialPath = "Assets/Settings/Water/OverworldWater.mat";

        /// <summary>
        /// Lifts the surface slightly off the planned bed so the two do not z-fight.
        /// </summary>
        /// <remarks>
        /// The carve is rasterised at 2.73 m per pixel while the ribbon is a smooth polyline, so the
        /// ground under the centreline lands a few centimetres either side of the plan. This is
        /// smaller than the shallowest channel and larger than that error.
        /// </remarks>
        private const float SurfaceLiftMeters = 0.05f;

        /// <summary>Builds the meshes under <paramref name="parent"/>. Returns how many it made.</summary>
        public static int Build(HydrologyDefinition definition, Transform parent)
        {
            if (definition == null || definition.IsEmpty || parent == null)
            {
                return 0;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(WaterMaterialPath);
            if (material == null)
            {
                Debug.LogWarning(
                    $"[Hydrology] No water material at {WaterMaterialPath}; inland water will render "
                    + "with the default material.");
            }

            var root = new GameObject(InlandRootName);
            root.transform.SetParent(parent, false);

            var built = 0;
            built += BuildRivers(definition, root.transform, material);
            built += BuildLakes(definition, root.transform, material);
            return built;
        }

        /// <summary>Groups reaches by the ocean outlet they drain to, one mesh per system.</summary>
        private static int BuildRivers(
            HydrologyDefinition definition, Transform parent, Material material)
        {
            var systems = new Dictionary<int, List<RiverReach>>();
            for (var i = 0; i < definition.Reaches.Count; i++)
            {
                var reach = definition.Reaches[i];
                var outlet = FindOutlet(definition, reach);
                if (outlet < 0)
                {
                    continue;
                }

                if (!systems.TryGetValue(outlet, out var list))
                {
                    list = new List<RiverReach>();
                    systems.Add(outlet, list);
                }

                list.Add(reach);
            }

            // Sorted so the scene objects come out in the same order every generation; a dictionary's
            // own order is not something the project can rely on.
            var outlets = new List<int>(systems.Keys);
            outlets.Sort();

            var built = 0;
            for (var i = 0; i < outlets.Count; i++)
            {
                var vertices = new List<Vector3>();
                var triangles = new List<int>();
                var uvs = new List<Vector2>();

                var reaches = systems[outlets[i]];
                reaches.Sort((a, b) => a.Id.Value.CompareTo(b.Id.Value));
                for (var r = 0; r < reaches.Count; r++)
                {
                    AppendRibbon(reaches[r], definition.Settings, vertices, triangles, uvs);
                }

                if (triangles.Count == 0)
                {
                    continue;
                }

                CreateMeshObject(
                    $"River_{outlets[i]}", parent, material, vertices, triangles, uvs);
                built++;
            }

            return built;
        }

        private static int BuildLakes(
            HydrologyDefinition definition, Transform parent, Material material)
        {
            var built = 0;
            for (var l = 0; l < definition.Lakes.Count; l++)
            {
                var lake = definition.Lakes[l];
                var ring = new List<Vector2>(lake.Boundary);
                var indices = EarClip.Triangulate(ring);
                if (indices.Count == 0)
                {
                    Debug.LogWarning(
                        $"[Hydrology] {lake.Id} could not be triangulated and was not drawn.");
                    continue;
                }

                var y = lake.SurfaceElevation + SurfaceLiftMeters;
                var vertices = new List<Vector3>(ring.Count);
                var uvs = new List<Vector2>(ring.Count);
                for (var i = 0; i < ring.Count; i++)
                {
                    vertices.Add(new Vector3(ring[i].x, y, ring[i].y));
                    uvs.Add(new Vector2(ring[i].x * 0.01f, ring[i].y * 0.01f));
                }

                CreateMeshObject($"Lake_{lake.Id.Value}", parent, material, vertices, indices, uvs);
                built++;
            }

            return built;
        }

        /// <summary>Sweeps a quad strip along one reach's centreline.</summary>
        private static void AppendRibbon(
            RiverReach reach,
            HydrologySettings settings,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            var nodes = reach.Nodes;
            if (nodes.Count < 2)
            {
                return;
            }

            var first = vertices.Count;
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];

                // Widened from each node's own contributing area rather than from the reach's single
                // downstream width, so a river grows along its length instead of stepping to full
                // size at the confluence above it.
                var halfWidth = Mathf.Max(1f, settings.ChannelHalfWidth(node.ContributingAreaSqm));
                var tangent = Tangent(nodes, i);
                var normal = new Vector2(-tangent.y, tangent.x);
                var y = node.WaterElevation + SurfaceLiftMeters;

                var left = node.PositionXZ - normal * halfWidth;
                var right = node.PositionXZ + normal * halfWidth;

                vertices.Add(new Vector3(left.x, y, left.y));
                vertices.Add(new Vector3(right.x, y, right.y));

                var v = node.CumulativeDistance * 0.05f;
                uvs.Add(new Vector2(0f, v));
                uvs.Add(new Vector2(1f, v));
            }

            for (var i = 0; i < nodes.Count - 1; i++)
            {
                var a = first + i * 2;
                var b = a + 1;
                var c = a + 2;
                var d = a + 3;

                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);

                triangles.Add(b);
                triangles.Add(c);
                triangles.Add(d);
            }
        }

        private static Vector2 Tangent(IReadOnlyList<RiverNode> nodes, int i)
        {
            var from = nodes[Mathf.Max(0, i - 1)].PositionXZ;
            var to = nodes[Mathf.Min(nodes.Count - 1, i + 1)].PositionXZ;
            var tangent = to - from;
            return tangent.sqrMagnitude < 1e-6f ? Vector2.right : tangent.normalized;
        }

        /// <summary>Walks downstream to the reach that meets the sea.</summary>
        /// <remarks>
        /// Hops through lakes, the same way the validator and the system selector do. Without the
        /// hop every tributary above a lake looks like a system of its own and gets its own mesh,
        /// which is how a six-river world came out as nine river objects.
        /// </remarks>
        private static int FindOutlet(HydrologyDefinition definition, RiverReach reach)
        {
            var current = reach;
            var guard = definition.Reaches.Count + 1;
            while (guard-- > 0)
            {
                if (current.Termination == ReachTermination.Ocean)
                {
                    return current.Id.Value;
                }

                var nextId = current.DownstreamId;
                if (current.Termination == ReachTermination.Lake)
                {
                    var lake = definition.FindLake(current.DownstreamLake);
                    if (lake == null || lake.SpillsDirectlyToOcean)
                    {
                        return current.Id.Value;
                    }

                    nextId = lake.OutletReach;
                }

                var next = definition.FindReach(nextId);
                if (next == null)
                {
                    // Nothing resolvable downstream. Grouped under its own id rather than dropped,
                    // so the water still renders.
                    return current.Id.Value;
                }

                current = next;
            }

            return -1;
        }

        private static void CreateMeshObject(
            string name,
            Transform parent,
            Material material,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            var mesh = new Mesh { name = name };
            if (vertices.Count > 65000)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = go.AddComponent<MeshRenderer>();
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>
        /// Ear clipping for the lake outlines.
        /// </summary>
        /// <remarks>
        /// A centroid fan is simpler but only correct for convex rings, and a basin outline traced
        /// from terrain is routinely concave — bays would be filled with water lying over dry land.
        /// </remarks>
        private static class EarClip
        {
            public static List<int> Triangulate(List<Vector2> ring)
            {
                var triangles = new List<int>();
                if (ring == null || ring.Count < 3)
                {
                    return triangles;
                }

                var indices = new List<int>(ring.Count);
                var clockwise = SignedArea(ring) < 0f;
                for (var i = 0; i < ring.Count; i++)
                {
                    indices.Add(clockwise ? ring.Count - 1 - i : i);
                }

                var guard = indices.Count * indices.Count;
                while (indices.Count > 3 && guard-- > 0)
                {
                    var clipped = false;
                    for (var i = 0; i < indices.Count; i++)
                    {
                        var prev = indices[(i + indices.Count - 1) % indices.Count];
                        var current = indices[i];
                        var next = indices[(i + 1) % indices.Count];

                        if (!IsEar(ring, indices, prev, current, next))
                        {
                            continue;
                        }

                        triangles.Add(prev);
                        triangles.Add(current);
                        triangles.Add(next);
                        indices.RemoveAt(i);
                        clipped = true;
                        break;
                    }

                    if (!clipped)
                    {
                        // A self-intersecting ring has no ear left. Bail out with what was produced
                        // rather than looping, and let the caller report the lake as undrawable.
                        return new List<int>();
                    }
                }

                if (indices.Count == 3)
                {
                    triangles.Add(indices[0]);
                    triangles.Add(indices[1]);
                    triangles.Add(indices[2]);
                }

                return triangles;
            }

            private static bool IsEar(
                List<Vector2> ring, List<int> indices, int prev, int current, int next)
            {
                var a = ring[prev];
                var b = ring[current];
                var c = ring[next];
                if (Cross(b - a, c - b) <= 0f)
                {
                    return false;
                }

                for (var i = 0; i < indices.Count; i++)
                {
                    var index = indices[i];
                    if (index == prev || index == current || index == next)
                    {
                        continue;
                    }

                    if (InTriangle(ring[index], a, b, c))
                    {
                        return false;
                    }
                }

                return true;
            }

            private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
            {
                var d1 = Cross(b - a, p - a);
                var d2 = Cross(c - b, p - b);
                var d3 = Cross(a - c, p - c);
                var hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
                var hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
                return !(hasNegative && hasPositive);
            }

            private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

            /// <summary>Shoelace area: positive counter-clockwise, negative clockwise.</summary>
            /// <remarks>
            /// Written out rather than using the trapezoid form, whose sign convention is the
            /// opposite way round. Getting it backwards flips every ring into the winding the ear
            /// test rejects, so no ear is ever found and every lake silently fails to triangulate —
            /// which is exactly what the first real bake produced.
            /// </remarks>
            private static float SignedArea(List<Vector2> ring)
            {
                var area = 0f;
                for (var i = 0; i < ring.Count; i++)
                {
                    var a = ring[i];
                    var b = ring[(i + 1) % ring.Count];
                    area += a.x * b.y - b.x * a.y;
                }

                return area * 0.5f;
            }
        }
    }
}
#endif

using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Roads
{
    /// <summary>
    /// Builds a ribbon-strip road mesh from a centerline of world-space points (already resolved to
    /// real ground Y by the caller, e.g. RoadNetworkMenu's raycast height probe) - replaces the
    /// original debug LineRenderer with real geometry that can carry a textured material. Width is
    /// applied as a purely horizontal offset (perpendicular to the path's flattened XZ tangent) so
    /// the strip doesn't pinch or skew on steep terrain, and UVs tile along the path's actual
    /// travelled distance (not per-segment) so the texture reads at a consistent scale regardless of
    /// how far apart consecutive centerline samples are.
    /// </summary>
    public static class RoadMeshBuilder
    {
        public static Mesh Build(IReadOnlyList<Vector3> centerline, float width, float uvTileLength)
        {
            var vertices = new List<Vector3>(centerline.Count * 2);
            var uvs = new List<Vector2>(centerline.Count * 2);
            var triangles = new List<int>(System.Math.Max(0, centerline.Count - 1) * 6);

            var halfWidth = width * 0.5f;
            var tileLength = Mathf.Max(uvTileLength, 0.01f);
            var traveled = 0f;

            for (var i = 0; i < centerline.Count; i++)
            {
                var point = centerline[i];
                var right = Right(Tangent(centerline, i));

                if (i > 0)
                {
                    traveled += Vector3.Distance(centerline[i - 1], point);
                }

                var v = traveled / tileLength;

                vertices.Add(point - right * halfWidth);
                vertices.Add(point + right * halfWidth);
                uvs.Add(new Vector2(0f, v));
                uvs.Add(new Vector2(1f, v));

                if (i == 0)
                {
                    continue;
                }

                // Quad corners for the [i-1, i] step: L(i-1)=base, R(i-1)=base+1, L(i)=base+2,
                // R(i)=base+3 - this index pattern (base, base+3, base+1) / (base, base+2, base+3)
                // gives an upward-facing normal for any tangent direction, since "right" is always
                // Cross(up, flattened tangent) and stays a consistent local frame along the ribbon.
                var baseIndex = (i - 1) * 2;
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 3);
                triangles.Add(baseIndex + 1);

                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 3);
            }

            var mesh = new Mesh { name = "RoadMesh" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // Central difference at interior points (smoother mitering at bends), forward/backward
        // difference at the endpoints.
        private static Vector3 Tangent(IReadOnlyList<Vector3> points, int i)
        {
            if (points.Count < 2)
            {
                return Vector3.forward;
            }

            if (i == 0)
            {
                return (points[1] - points[0]).normalized;
            }

            if (i == points.Count - 1)
            {
                return (points[i] - points[i - 1]).normalized;
            }

            return (points[i + 1] - points[i - 1]).normalized;
        }

        private static Vector3 Right(Vector3 tangent)
        {
            var flat = new Vector3(tangent.x, 0f, tangent.z);
            if (flat.sqrMagnitude < 1e-8f)
            {
                return Vector3.right;
            }

            return Vector3.Cross(Vector3.up, flat.normalized);
        }
    }
}

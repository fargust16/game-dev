using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Builds a ribbon-strip mesh for one <see cref="RiverPath"/>'s water surface - same technique
    /// as <see cref="GameDev.World.Roads.RoadMeshBuilder"/> (width as a horizontal offset off the
    /// flattened tangent, UVs tiled by travelled distance), except width varies per point (a river
    /// widens toward its mouth, a road doesn't) and Y comes from the path's own
    /// <see cref="RiverPath.WaterSurfaceY"/> instead of a re-sampled ground raycast - the water
    /// surface is data this system already computed, not something to re-derive from the (now
    /// carved) terrain.
    /// </summary>
    public static class RiverMeshBuilder
    {
        public static Mesh Build(RiverPath river, float uvTileLength)
        {
            var centerline = river.Centerline;
            var widths = river.Widths;
            var count = centerline.Count;

            var vertices = new List<Vector3>(count * 2);
            var uvs = new List<Vector2>(count * 2);
            var triangles = new List<int>(System.Math.Max(0, count - 1) * 6);

            var tileLength = Mathf.Max(uvTileLength, 0.01f);
            var traveled = 0f;

            for (var i = 0; i < count; i++)
            {
                var point = new Vector3(centerline[i].x, river.WaterSurfaceY[i], centerline[i].z);
                var right = Right(Tangent(centerline, i));
                var halfWidth = widths[i] * 0.5f;

                if (i > 0)
                {
                    var previous = new Vector3(centerline[i - 1].x, river.WaterSurfaceY[i - 1], centerline[i - 1].z);
                    traveled += Vector3.Distance(previous, point);
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

                // Same index pattern as RoadMeshBuilder: base=L(i-1), base+1=R(i-1), base+2=L(i),
                // base+3=R(i) - gives an upward-facing normal for any tangent direction.
                var baseIndex = (i - 1) * 2;
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 3);
                triangles.Add(baseIndex + 1);

                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 3);
            }

            var mesh = new Mesh { name = $"RiverMesh_{river.Id}" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // Central difference at interior points, forward/backward at the endpoints - flattened to
        // XZ so width offset stays horizontal even as the water surface Y falls downstream.
        private static Vector3 Tangent(IReadOnlyList<Vector3> points, int i)
        {
            if (points.Count < 2)
            {
                return Vector3.forward;
            }

            Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

            if (i == 0)
            {
                return (Flat(points[1]) - Flat(points[0])).normalized;
            }

            if (i == points.Count - 1)
            {
                return (Flat(points[i]) - Flat(points[i - 1])).normalized;
            }

            return (Flat(points[i + 1]) - Flat(points[i - 1])).normalized;
        }

        private static Vector3 Right(Vector3 flatTangent)
        {
            if (flatTangent.sqrMagnitude < 1e-8f)
            {
                return Vector3.right;
            }

            return Vector3.Cross(Vector3.up, flatTangent.normalized);
        }
    }
}

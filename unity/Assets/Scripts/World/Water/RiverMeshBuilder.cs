using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Builds one continuous ribbon mesh per <see cref="RiverPath"/> (no strip seams).
    /// Centerline is densified before triangulation so the surface follows banks smoothly instead
    /// of faceted segments with visible gaps at the shoreline.
    /// </summary>
    public static class RiverMeshBuilder
    {
        public const float DensifyStepMeters = 2.5f;
        public const float WidthPadMeters = 0.35f;

        public static Mesh Build(RiverPath river, float uvTileLength)
        {
            Densify(river, DensifyStepMeters, out var centerline, out var widths, out var surfaceY);

            var count = centerline.Count;
            var vertices = new List<Vector3>(count * 2);
            var uvs = new List<Vector2>(count * 2);
            var triangles = new List<int>(System.Math.Max(0, count - 1) * 6);

            var tileLength = Mathf.Max(uvTileLength, 0.01f);
            var traveled = 0f;

            for (var i = 0; i < count; i++)
            {
                var point = new Vector3(centerline[i].x, surfaceY[i], centerline[i].z);
                var right = Right(Tangent(centerline, i));
                var halfWidth = (widths[i] * 0.5f) + WidthPadMeters;

                if (i > 0)
                {
                    var previous = new Vector3(centerline[i - 1].x, surfaceY[i - 1], centerline[i - 1].z);
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

        private static void Densify(
            RiverPath river,
            float stepMeters,
            out List<Vector3> centerline,
            out List<float> widths,
            out List<float> surfaceY)
        {
            var srcC = river.Centerline;
            var srcW = river.Widths;
            var srcY = river.WaterSurfaceY;
            centerline = new List<Vector3>(srcC.Count * 2);
            widths = new List<float>(srcC.Count * 2);
            surfaceY = new List<float>(srcC.Count * 2);

            if (srcC.Count == 0)
            {
                return;
            }

            centerline.Add(srcC[0]);
            widths.Add(srcW[0]);
            surfaceY.Add(srcY[0]);

            for (var i = 1; i < srcC.Count; i++)
            {
                var a = srcC[i - 1];
                var b = srcC[i];
                var flatDist = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
                var segments = Mathf.Max(1, Mathf.CeilToInt(flatDist / Mathf.Max(stepMeters, 0.5f)));
                for (var s = 1; s <= segments; s++)
                {
                    var t = s / (float)segments;
                    centerline.Add(Vector3.Lerp(a, b, t));
                    widths.Add(Mathf.Lerp(srcW[i - 1], srcW[i], t));
                    surfaceY.Add(Mathf.Lerp(srcY[i - 1], srcY[i], t));
                }
            }
        }

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

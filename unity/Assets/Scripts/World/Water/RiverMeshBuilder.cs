using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Continuous river water ribbon: smoothed centerline + miter-limited offsets so sharp D8
    /// bends do not self-intersect into overlapping quads. Y comes from
    /// <see cref="RiverPath.WaterSurfaceY"/> (synced to the carved bed after carve).
    /// </summary>
    public static class RiverMeshBuilder
    {
        public const float DensifyStepMeters = 1.5f;
        /// <summary>Stay inside the carved U-floor — full width put edges on banks.</summary>
        public const float WidthScale = 0.55f;
        public const float WidthPadMeters = 0.05f;
        public const float MiterLimit = 1.75f;
        /// <summary>Chaikin cut corners off the trench and twisted the ribbon — densify only.</summary>
        public const int SmoothIterations = 0;

        public const float MeshFreeboardMeters = 0.5f;

        public static Mesh Build(RiverPath river, float uvTileLength) =>
            Build(river, uvTileLength, sampleTerrainHeightY: null);

        /// <param name="sampleTerrainHeightY">
        /// World XZ → terrain Y after carve. Required to keep the ribbon above ground after path
        /// smoothing (Chaikin moves XZ off the original samples).
        /// </param>
        public static Mesh Build(
            RiverPath river,
            float uvTileLength,
            System.Func<float, float, float> sampleTerrainHeightY)
        {
            BuildPolylines(river, out var centerline, out var widths, out var surfaceY);
            ResampleSurfaceToTerrain(centerline, widths, surfaceY, sampleTerrainHeightY);

            var count = centerline.Count;
            var vertices = new List<Vector3>(count * 2);
            var uvs = new List<Vector2>(count * 2);
            var triangles = new List<int>(System.Math.Max(0, count - 1) * 6);

            var tileLength = Mathf.Max(uvTileLength, 0.01f);
            var traveled = 0f;

            for (var i = 0; i < count; i++)
            {
                var halfWidth = widths[i] * 0.5f * WidthScale + WidthPadMeters;
                var offset = MiterOffset(centerline, i, halfWidth);
                // Same Y on both edges — per-edge terrain lift crumpled the ribbon into folds.
                var y = surfaceY[i];
                if (sampleTerrainHeightY != null)
                {
                    y = Mathf.Max(y, sampleTerrainHeightY(centerline[i].x, centerline[i].z) + 0.08f);
                }

                var point = new Vector3(centerline[i].x, y, centerline[i].z);
                var left = point - offset;
                var right = point + offset;

                if (i > 0)
                {
                    var prev = new Vector3(centerline[i - 1].x, surfaceY[i - 1], centerline[i - 1].z);
                    traveled += Vector3.Distance(prev, point);
                }

                var v = traveled / tileLength;
                vertices.Add(left);
                vertices.Add(right);
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

        /// <summary>Smooth → densify → light Y smooth. Exposed for tests.</summary>
        public static void BuildPolylines(
            RiverPath river,
            out List<Vector3> centerline,
            out List<float> widths,
            out List<float> surfaceY)
        {
            centerline = new List<Vector3>(river.Centerline.Count);
            widths = new List<float>(river.Widths.Count);
            surfaceY = new List<float>(river.WaterSurfaceY.Count);
            for (var i = 0; i < river.Centerline.Count; i++)
            {
                centerline.Add(river.Centerline[i]);
                widths.Add(river.Widths[i]);
                surfaceY.Add(river.WaterSurfaceY[i]);
            }

            for (var s = 0; s < SmoothIterations; s++)
            {
                Chaikin(ref centerline, ref widths, ref surfaceY);
            }

            Densify(ref centerline, ref widths, ref surfaceY, DensifyStepMeters);
            SmoothSurfaceY(surfaceY, passes: 2);
        }

        /// <summary>
        /// After Chaikin, XZ no longer matches Sync samples — re-sit on the carved bed so the
        /// ribbon cannot bury under the bank.
        /// </summary>
        public static void ResampleSurfaceToTerrain(
            List<Vector3> centerline,
            List<float> widths,
            List<float> surfaceY,
            System.Func<float, float, float> sampleTerrainHeightY)
        {
            if (sampleTerrainHeightY == null || centerline.Count == 0)
            {
                return;
            }

            var beds = new float[centerline.Count];
            for (var i = 0; i < centerline.Count; i++)
            {
                beds[i] = sampleTerrainHeightY(centerline[i].x, centerline[i].z);
                // Authority: sit on local bed. Do NOT max with sync lakeY while still on the
                // approach (that floated a 30–40m sheet over grass and crumpled at the shore).
                surfaceY[i] = beds[i] + MeshFreeboardMeters;
            }

            // Mild smooth, then re-clamp to bed; limit upward jumps (downhill may drop freely).
            SmoothSurfaceY(surfaceY, passes: 1);
            for (var i = 0; i < centerline.Count; i++)
            {
                surfaceY[i] = Mathf.Max(surfaceY[i], beds[i] + MeshFreeboardMeters);
                if (i > 0)
                {
                    var maxRise = 1.25f;
                    if (surfaceY[i] > surfaceY[i - 1] + maxRise)
                    {
                        surfaceY[i] = surfaceY[i - 1] + maxRise;
                    }

                    surfaceY[i] = Mathf.Max(surfaceY[i], beds[i] + MeshFreeboardMeters);
                }
            }
        }

        /// <summary>
        /// Half-width offset to the river's right. Miter from adjacent segment normals, length
        /// clamped so sharp bends bevel instead of folding over themselves.
        /// </summary>
        public static Vector3 MiterOffset(IReadOnlyList<Vector3> points, int i, float halfWidth)
        {
            halfWidth = Mathf.Max(halfWidth, 0.05f);
            if (points.Count < 2)
            {
                return Vector3.right * halfWidth;
            }

            Vector3 FlatDir(Vector3 from, Vector3 to)
            {
                var d = new Vector3(to.x - from.x, 0f, to.z - from.z);
                return d.sqrMagnitude < 1e-8f ? Vector3.forward : d.normalized;
            }

            Vector3 RightOf(Vector3 flatTangent) => Vector3.Cross(Vector3.up, flatTangent).normalized;

            if (i <= 0)
            {
                return RightOf(FlatDir(points[0], points[1])) * halfWidth;
            }

            if (i >= points.Count - 1)
            {
                return RightOf(FlatDir(points[i - 1], points[i])) * halfWidth;
            }

            var tPrev = FlatDir(points[i - 1], points[i]);
            var tNext = FlatDir(points[i], points[i + 1]);
            var nPrev = RightOf(tPrev);
            var nNext = RightOf(tNext);

            // Very sharp / reversal → use averaged tangent (no giant miter).
            var turnDot = Vector3.Dot(tPrev, tNext);
            if (turnDot < 0.15f)
            {
                var t = (tPrev + tNext).normalized;
                if (t.sqrMagnitude < 1e-6f)
                {
                    t = tPrev;
                }

                return RightOf(t) * halfWidth;
            }

            var miter = nPrev + nNext;
            if (miter.sqrMagnitude < 1e-8f)
            {
                return nPrev * halfWidth;
            }

            miter.Normalize();
            var denom = Vector3.Dot(miter, nPrev);
            if (denom < 0.25f)
            {
                // Near-parallel normals with bad projection — bevel to segment normal.
                return nPrev * halfWidth;
            }

            var miterLen = halfWidth / denom;
            miterLen = Mathf.Min(miterLen, halfWidth * MiterLimit);
            return miter * miterLen;
        }

        private static void Chaikin(
            ref List<Vector3> points,
            ref List<float> widths,
            ref List<float> surfaceY)
        {
            if (points.Count < 3)
            {
                return;
            }

            var n = points.Count;
            var nextP = new List<Vector3>(n * 2);
            var nextW = new List<float>(n * 2);
            var nextY = new List<float>(n * 2);

            nextP.Add(points[0]);
            nextW.Add(widths[0]);
            nextY.Add(surfaceY[0]);

            for (var i = 0; i < n - 1; i++)
            {
                var p0 = points[i];
                var p1 = points[i + 1];
                var q = Vector3.Lerp(p0, p1, 0.25f);
                var r = Vector3.Lerp(p0, p1, 0.75f);
                // Keep polyline on XZ; Y on points is unused (surfaceY carries water height).
                q.y = 0f;
                r.y = 0f;
                nextP.Add(q);
                nextW.Add(Mathf.Lerp(widths[i], widths[i + 1], 0.25f));
                nextY.Add(Mathf.Lerp(surfaceY[i], surfaceY[i + 1], 0.25f));
                if (i < n - 2)
                {
                    nextP.Add(r);
                    nextW.Add(Mathf.Lerp(widths[i], widths[i + 1], 0.75f));
                    nextY.Add(Mathf.Lerp(surfaceY[i], surfaceY[i + 1], 0.75f));
                }
            }

            nextP.Add(points[n - 1]);
            nextW.Add(widths[n - 1]);
            nextY.Add(surfaceY[n - 1]);

            points = nextP;
            widths = nextW;
            surfaceY = nextY;
        }

        private static void Densify(
            ref List<Vector3> points,
            ref List<float> widths,
            ref List<float> surfaceY,
            float stepMeters)
        {
            if (points.Count < 2)
            {
                return;
            }

            var srcP = points;
            var srcW = widths;
            var srcY = surfaceY;
            points = new List<Vector3>(srcP.Count * 2);
            widths = new List<float>(srcP.Count * 2);
            surfaceY = new List<float>(srcP.Count * 2);

            points.Add(srcP[0]);
            widths.Add(srcW[0]);
            surfaceY.Add(srcY[0]);

            for (var i = 1; i < srcP.Count; i++)
            {
                var a = srcP[i - 1];
                var b = srcP[i];
                var flatDist = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
                var segments = Mathf.Max(1, Mathf.CeilToInt(flatDist / Mathf.Max(stepMeters, 0.5f)));
                for (var s = 1; s <= segments; s++)
                {
                    var t = s / (float)segments;
                    var p = Vector3.Lerp(a, b, t);
                    p.y = 0f;
                    points.Add(p);
                    widths.Add(Mathf.Lerp(srcW[i - 1], srcW[i], t));
                    surfaceY.Add(Mathf.Lerp(srcY[i - 1], srcY[i], t));
                }
            }
        }

        private static void SmoothSurfaceY(List<float> surfaceY, int passes)
        {
            if (surfaceY.Count < 3 || passes <= 0)
            {
                return;
            }

            var tmp = new float[surfaceY.Count];
            for (var pass = 0; pass < passes; pass++)
            {
                tmp[0] = surfaceY[0];
                tmp[surfaceY.Count - 1] = surfaceY[surfaceY.Count - 1];
                for (var i = 1; i < surfaceY.Count - 1; i++)
                {
                    tmp[i] = (surfaceY[i - 1] + surfaceY[i] * 2f + surfaceY[i + 1]) * 0.25f;
                }

                for (var i = 0; i < surfaceY.Count; i++)
                {
                    surfaceY[i] = tmp[i];
                }
            }
        }
    }
}

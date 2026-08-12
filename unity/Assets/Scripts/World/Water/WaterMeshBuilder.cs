using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// One continuous flat water surface per lake at <see cref="WaterZone.WaterLevelY"/>.
    /// Silhouette is a <b>smooth inflated flood contour</b> (SDF + small plan expand) — slightly
    /// larger than the carved pit in top-down overlap — not a terrain-gated swiss-cheese mask.
    /// Terrain height is not used to punch holes in the shoreline (that caused torn slits).
    /// </summary>
    public static class WaterMeshBuilder
    {
        public const float MeshStepMeters = 0.5f;

        /// <summary>
        /// How far past the flood mask (metres) the water sheet extends in plan view — a slight
        /// overlap over the bank so the rim reads solid, not under-sized vs the pit.
        /// </summary>
        public const float ShoreExpandMeters = 2.75f;

        /// <summary>Optional blur radius on the fine mesh field (in cells) before marching squares.</summary>
        public const int FieldSmoothPasses = 2;

        public static Mesh Build(WaterZone zone, IReadOnlyList<WaterZone> allZones) =>
            Build(zone, allZones, sampleTerrainY: null, clipXZ: null);

        public static Mesh Build(
            WaterZone zone,
            IReadOnlyList<WaterZone> allZones,
            System.Func<float, float, float> sampleTerrainY) =>
            Build(zone, allZones, sampleTerrainY, clipXZ: null);

        public static Mesh Build(
            WaterZone zone,
            IReadOnlyList<WaterZone> allZones,
            System.Func<float, float, float> sampleTerrainY,
            Rect? clipXZ)
        {
            _ = allZones;
            _ = sampleTerrainY; // kept for API compatibility; silhouette is flood-SDF only

            if (!TryGetFloodBounds(zone, out var minX, out var minZ, out var maxX, out var maxZ))
            {
                return new Mesh { name = $"WaterZoneMesh_{zone.Id}_empty" };
            }

            var y = zone.WaterLevelY;
            var step = Mathf.Max(MeshStepMeters, 0.35f);
            var sdf = LakeShoreField.BuildSignedDistance(zone, exteriorCapMeters: ShoreExpandMeters + 12f);

            var pad = ShoreExpandMeters + step * 2f;
            minX -= pad;
            minZ -= pad;
            maxX += pad;
            maxZ += pad;

            if (clipXZ.HasValue)
            {
                const float edgeInset = 1.25f;
                var c = clipXZ.Value;
                minX = Mathf.Max(minX, c.xMin + edgeInset);
                minZ = Mathf.Max(minZ, c.yMin + edgeInset);
                maxX = Mathf.Min(maxX, c.xMax - edgeInset);
                maxZ = Mathf.Min(maxZ, c.yMax - edgeInset);
                if (maxX <= minX || maxZ <= minZ)
                {
                    return new Mesh { name = $"WaterZoneMesh_{zone.Id}_empty" };
                }
            }

            var cellsX = Mathf.Max(1, Mathf.CeilToInt((maxX - minX) / step));
            var cellsZ = Mathf.Max(1, Mathf.CeilToInt((maxZ - minZ) / step));

            // Continuous field: zero-crossing at the expanded flood contour (smooth iso-line).
            // Positive = inside water sheet. No terrain depth punches — those tore the shore.
            var field = new float[cellsX + 1, cellsZ + 1];
            var anyWet = false;
            for (var iz = 0; iz <= cellsZ; iz++)
            {
                for (var ix = 0; ix <= cellsX; ix++)
                {
                    var wx = minX + ix * step;
                    var wz = minZ + iz * step;
                    if (clipXZ.HasValue)
                    {
                        var c = clipXZ.Value;
                        if (wx < c.xMin + 0.5f || wx > c.xMax - 0.5f ||
                            wz < c.yMin + 0.5f || wz > c.yMax - 0.5f)
                        {
                            field[ix, iz] = -1f;
                            continue;
                        }
                    }

                    var shore = LakeShoreField.Sample(zone, sdf, wx, wz);
                    var value = shore + ShoreExpandMeters;
                    field[ix, iz] = value;
                    if (value >= 0f)
                    {
                        anyWet = true;
                    }
                }
            }

            if (!anyWet)
            {
                return new Mesh { name = $"WaterZoneMesh_{zone.Id}_empty" };
            }

            SmoothField(field, cellsX + 1, cellsZ + 1, FieldSmoothPasses);

            var verts = new List<Vector3>(cellsX * cellsZ * 4);
            var uvs = new List<Vector2>(cellsX * cellsZ * 4);
            var tris = new List<int>(cellsX * cellsZ * 6);
            var spanX = Mathf.Max(maxX - minX, 0.01f);
            var spanZ = Mathf.Max(maxZ - minZ, 0.01f);

            for (var iz = 0; iz < cellsZ; iz++)
            {
                for (var ix = 0; ix < cellsX; ix++)
                {
                    var x0 = minX + ix * step;
                    var z0 = minZ + iz * step;
                    var x1 = x0 + step;
                    var z1 = z0 + step;

                    var s00 = field[ix, iz];
                    var s10 = field[ix + 1, iz];
                    var s11 = field[ix + 1, iz + 1];
                    var s01 = field[ix, iz + 1];

                    var mask =
                        (s00 >= 0f ? 1 : 0) |
                        (s10 >= 0f ? 2 : 0) |
                        (s11 >= 0f ? 4 : 0) |
                        (s01 >= 0f ? 8 : 0);

                    if (mask == 0)
                    {
                        continue;
                    }

                    if (mask == 15)
                    {
                        AddQuad(verts, uvs, tris, y, x0, z0, x1, z1, minX, minZ, spanX, spanZ);
                        continue;
                    }

                    EmitCell(verts, uvs, tris, y, mask, x0, z0, x1, z1, s00, s10, s11, s01, minX, minZ, spanX, spanZ);
                }
            }

            var mesh = new Mesh { name = $"WaterZoneMesh_{zone.Id}" };
            if (verts.Count == 0)
            {
                return mesh;
            }

            if (verts.Count > 65000)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void SmoothField(float[,] field, int width, int height, int passes)
        {
            if (passes <= 0)
            {
                return;
            }

            var copy = new float[width, height];
            for (var p = 0; p < passes; p++)
            {
                for (var z = 0; z < height; z++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        copy[x, z] = field[x, z];
                    }
                }

                for (var z = 1; z < height - 1; z++)
                {
                    for (var x = 1; x < width - 1; x++)
                    {
                        var sum =
                            copy[x - 1, z - 1] + copy[x, z - 1] + copy[x + 1, z - 1] +
                            copy[x - 1, z] + copy[x, z] * 4f + copy[x + 1, z] +
                            copy[x - 1, z + 1] + copy[x, z + 1] + copy[x + 1, z + 1];
                        field[x, z] = sum / 12f;
                    }
                }
            }
        }

        private static void AddQuad(
            List<Vector3> verts, List<Vector2> uvs, List<int> tris,
            float y, float x0, float z0, float x1, float z1,
            float minX, float minZ, float spanX, float spanZ)
        {
            var i = verts.Count;
            verts.Add(new Vector3(x0, y, z0));
            verts.Add(new Vector3(x0, y, z1));
            verts.Add(new Vector3(x1, y, z1));
            verts.Add(new Vector3(x1, y, z0));
            uvs.Add(new Vector2((x0 - minX) / spanX, (z0 - minZ) / spanZ));
            uvs.Add(new Vector2((x0 - minX) / spanX, (z1 - minZ) / spanZ));
            uvs.Add(new Vector2((x1 - minX) / spanX, (z1 - minZ) / spanZ));
            uvs.Add(new Vector2((x1 - minX) / spanX, (z0 - minZ) / spanZ));
            tris.Add(i);
            tris.Add(i + 1);
            tris.Add(i + 2);
            tris.Add(i);
            tris.Add(i + 2);
            tris.Add(i + 3);
        }

        private static void EmitCell(
            List<Vector3> verts, List<Vector2> uvs, List<int> tris,
            float y, int mask,
            float x0, float z0, float x1, float z1,
            float s00, float s10, float s11, float s01,
            float minX, float minZ, float spanX, float spanZ)
        {
            var e0 = LerpEdge(x0, z0, x1, z0, s00, s10);
            var e1 = LerpEdge(x1, z0, x1, z1, s10, s11);
            var e2 = LerpEdge(x1, z1, x0, z1, s11, s01);
            var e3 = LerpEdge(x0, z1, x0, z0, s01, s00);

            var c00 = new Vector2(x0, z0);
            var c10 = new Vector2(x1, z0);
            var c11 = new Vector2(x1, z1);
            var c01 = new Vector2(x0, z1);

            switch (mask)
            {
                case 1: AddTri(verts, uvs, tris, y, c00, e0, e3, minX, minZ, spanX, spanZ); break;
                case 2: AddTri(verts, uvs, tris, y, c10, e1, e0, minX, minZ, spanX, spanZ); break;
                case 3: AddQuadPts(verts, uvs, tris, y, c00, e3, e1, c10, minX, minZ, spanX, spanZ); break;
                case 4: AddTri(verts, uvs, tris, y, c11, e2, e1, minX, minZ, spanX, spanZ); break;
                case 5:
                    AddTri(verts, uvs, tris, y, c00, e0, e3, minX, minZ, spanX, spanZ);
                    AddTri(verts, uvs, tris, y, c11, e2, e1, minX, minZ, spanX, spanZ);
                    break;
                case 6: AddQuadPts(verts, uvs, tris, y, c10, e0, e2, c11, minX, minZ, spanX, spanZ); break;
                case 7: AddPent(verts, uvs, tris, y, c00, e3, e2, c11, c10, minX, minZ, spanX, spanZ); break;
                case 8: AddTri(verts, uvs, tris, y, c01, e3, e2, minX, minZ, spanX, spanZ); break;
                case 9: AddQuadPts(verts, uvs, tris, y, c00, c01, e2, e0, minX, minZ, spanX, spanZ); break;
                case 10:
                    AddTri(verts, uvs, tris, y, c10, e1, e0, minX, minZ, spanX, spanZ);
                    AddTri(verts, uvs, tris, y, c01, e3, e2, minX, minZ, spanX, spanZ);
                    break;
                case 11: AddPent(verts, uvs, tris, y, c00, c01, e2, e1, c10, minX, minZ, spanX, spanZ); break;
                case 12: AddQuadPts(verts, uvs, tris, y, c01, c11, e1, e3, minX, minZ, spanX, spanZ); break;
                case 13: AddPent(verts, uvs, tris, y, c00, c01, c11, e1, e0, minX, minZ, spanX, spanZ); break;
                case 14: AddPent(verts, uvs, tris, y, c10, e0, e3, c01, c11, minX, minZ, spanX, spanZ); break;
            }
        }

        private static Vector2 LerpEdge(float ax, float az, float bx, float bz, float sa, float sb)
        {
            var denom = sa - sb;
            var t = Mathf.Abs(denom) < 1e-5f ? 0.5f : Mathf.Clamp01(sa / denom);
            return new Vector2(Mathf.Lerp(ax, bx, t), Mathf.Lerp(az, bz, t));
        }

        private static void AddTri(
            List<Vector3> verts, List<Vector2> uvs, List<int> tris,
            float y, Vector2 a, Vector2 b, Vector2 c,
            float minX, float minZ, float spanX, float spanZ)
        {
            var i = verts.Count;
            Push(verts, uvs, y, a, minX, minZ, spanX, spanZ);
            Push(verts, uvs, y, b, minX, minZ, spanX, spanZ);
            Push(verts, uvs, y, c, minX, minZ, spanX, spanZ);
            tris.Add(i);
            tris.Add(i + 1);
            tris.Add(i + 2);
        }

        private static void AddQuadPts(
            List<Vector3> verts, List<Vector2> uvs, List<int> tris,
            float y, Vector2 a, Vector2 b, Vector2 c, Vector2 d,
            float minX, float minZ, float spanX, float spanZ)
        {
            AddTri(verts, uvs, tris, y, a, b, c, minX, minZ, spanX, spanZ);
            AddTri(verts, uvs, tris, y, a, c, d, minX, minZ, spanX, spanZ);
        }

        private static void AddPent(
            List<Vector3> verts, List<Vector2> uvs, List<int> tris,
            float y, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Vector2 e,
            float minX, float minZ, float spanX, float spanZ)
        {
            AddTri(verts, uvs, tris, y, a, b, c, minX, minZ, spanX, spanZ);
            AddTri(verts, uvs, tris, y, a, c, d, minX, minZ, spanX, spanZ);
            AddTri(verts, uvs, tris, y, a, d, e, minX, minZ, spanX, spanZ);
        }

        private static void Push(
            List<Vector3> verts, List<Vector2> uvs, float y, Vector2 p,
            float minX, float minZ, float spanX, float spanZ)
        {
            verts.Add(new Vector3(p.x, y, p.y));
            uvs.Add(new Vector2((p.x - minX) / spanX, (p.y - minZ) / spanZ));
        }

        private static bool TryGetFloodBounds(
            WaterZone zone, out float minX, out float minZ, out float maxX, out float maxZ)
        {
            var aabb = zone.GetFloodAabbXZ();
            if (aabb.width <= 0f || aabb.height <= 0f)
            {
                minX = minZ = maxX = maxZ = 0f;
                return false;
            }

            minX = aabb.xMin;
            minZ = aabb.yMin;
            maxX = aabb.xMax;
            maxZ = aabb.yMax;
            return true;
        }
    }
}

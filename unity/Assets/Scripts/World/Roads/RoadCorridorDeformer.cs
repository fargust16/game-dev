using System.Collections.Generic;
using UnityEngine;
using WorldScale = GameDev.World.WorldScale;

namespace GameDev.World.Roads
{
    /// <summary>
    /// Conforms terrain to road corridors with road / shoulder / outer-blend zones (smoothstep).
    /// Never creates vertical canyon walls — cut/fill are soft lerps toward the road profile.
    /// </summary>
    public static class RoadCorridorDeformer
    {
        public const float DensifyStepMeters = 2.5f;

        public static void Apply(
            UnityEngine.Terrain terrain,
            IReadOnlyList<RoadDefinition> roads,
            TerrainLayer centerLayer = null,
            TerrainLayer gravelLayer = null,
            TerrainLayer edgeLayer = null)
        {
            if (terrain == null || terrain.terrainData == null || roads == null || roads.Count == 0)
                return;

#if UNITY_EDITOR
            // Split so the [Gen] log distinguishes "stamping heights" from "repainting alphamaps".
            // The roads step dominates Generate All and the two halves have nothing in common:
            // one is bounded by corridor length, the other by alphamap resolution times layer count.
            var heightTimer = System.Diagnostics.Stopwatch.StartNew();
#endif
            var data = terrain.terrainData;
            var resolution = data.heightmapResolution;
            var heights = data.GetHeights(0, 0, resolution, resolution);
            var original = (float[,])heights.Clone();
            var terrainPos = terrain.transform.position;
            var size = data.size;
            var worldPerPixelX = size.x / (resolution - 1);
            var worldPerPixelZ = size.z / (resolution - 1);
            var worldHeight = Mathf.Max(size.y, 0.01f);
            var touched = new bool[resolution, resolution];

            for (var r = 0; r < roads.Count; r++)
            {
                ApplyRoad(
                    heights, original, touched, resolution,
                    terrainPos, worldPerPixelX, worldPerPixelZ, worldHeight, roads[r]);
            }

            data.SetHeights(0, 0, heights);

#if UNITY_EDITOR
            heightTimer.Stop();
            UnityEngine.Debug.Log(
                $"[Gen] step=roads.heights ms={heightTimer.ElapsedMilliseconds} | " +
                $"{roads.Count} corridors, heightmap {resolution}x{resolution}");
            var paintTimer = System.Diagnostics.Stopwatch.StartNew();
#endif

            if (centerLayer != null)
                RoadSurfacePaint.Paint(terrain, roads, centerLayer, edgeLayer, gravelLayer);

#if UNITY_EDITOR
            paintTimer.Stop();
            UnityEngine.Debug.Log(
                $"[Gen] step=roads.paint ms={paintTimer.ElapsedMilliseconds} | " +
                $"alphamap {data.alphamapResolution}x{data.alphamapResolution}, " +
                $"{data.terrainLayers.Length} layers");
#endif

            var collider = terrain.GetComponent<TerrainCollider>();
            if (collider != null)
                collider.terrainData = terrain.terrainData;
        }

        private static void ApplyRoad(
            float[,] heights,
            float[,] original,
            bool[,] touched,
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            float worldHeight,
            RoadDefinition road)
        {
            if (road.CenterlineXZ == null || road.CenterlineXZ.Count < 2 ||
                road.ProfileY == null || road.ProfileY.Count < 2)
                return;

            var dense = DensifyWithProfile(road.CenterlineXZ, road.ProfileY, DensifyStepMeters);
            var halfRoad = road.RoadWidthMeters * 0.5f;
            var shoulderEnd = halfRoad + road.ShoulderMeters;
            var blendEnd = shoulderEnd + road.BlendMeters;
            var maxCut = WorldScale.RoadMaxCutDepthMeters;
            var maxFill = WorldScale.RoadMaxFillHeightMeters;

            for (var i = 0; i < dense.Count - 1; i++)
            {
                var a = dense[i].xz;
                var b = dense[i + 1].xz;
                var ya = dense[i].y;
                var yb = dense[i + 1].y;
                var ab = b - a;
                var len = ab.magnitude;
                if (len < 1e-4f)
                    continue;
                var dir = ab / len;
                var pad = blendEnd + worldPerPixelX * 2f;
                var minX = Mathf.Min(a.x, b.x) - pad;
                var maxX = Mathf.Max(a.x, b.x) + pad;
                var minZ = Mathf.Min(a.y, b.y) - pad;
                var maxZ = Mathf.Max(a.y, b.y) + pad;
                var minPx = Mathf.Max(0, Mathf.FloorToInt((minX - terrainPos.x) / worldPerPixelX));
                var maxPx = Mathf.Min(resolution - 1, Mathf.CeilToInt((maxX - terrainPos.x) / worldPerPixelX));
                var minPz = Mathf.Max(0, Mathf.FloorToInt((minZ - terrainPos.z) / worldPerPixelZ));
                var maxPz = Mathf.Min(resolution - 1, Mathf.CeilToInt((maxZ - terrainPos.z) / worldPerPixelZ));

                for (var pz = minPz; pz <= maxPz; pz++)
                {
                    for (var px = minPx; px <= maxPx; px++)
                    {
                        var worldX = terrainPos.x + px * worldPerPixelX;
                        var worldZ = terrainPos.z + pz * worldPerPixelZ;
                        var p = new Vector2(worldX, worldZ);
                        var ap = p - a;
                        var t = Mathf.Clamp01(Vector2.Dot(ap, dir) / len);
                        var closest = a + dir * (t * len);
                        var dist = Vector2.Distance(p, closest);
                        if (dist > blendEnd)
                            continue;

                        var roadY = Mathf.Lerp(ya, yb, t);
                        var originalY = terrainPos.y + original[pz, px] * worldHeight;
                        var delta = roadY - originalY;
                        if (delta < -maxCut)
                            roadY = originalY - maxCut;
                        else if (delta > maxFill)
                            roadY = originalY + maxFill;

                        float influence;
                        if (dist <= halfRoad)
                            influence = 1f;
                        else if (dist <= shoulderEnd)
                        {
                            var u = (dist - halfRoad) / Mathf.Max(road.ShoulderMeters, 0.01f);
                            influence = 1f - 0.15f * Smooth01(u);
                        }
                        else
                        {
                            var u = (dist - shoulderEnd) / Mathf.Max(road.BlendMeters, 0.01f);
                            influence = 1f - Smooth01(u);
                        }

                        var targetY = Mathf.Lerp(originalY, roadY, influence);
                        var candidate = Mathf.Clamp01((targetY - terrainPos.y) / worldHeight);
                        if (!touched[pz, px] || influence > 0.5f)
                        {
                            heights[pz, px] = candidate;
                            touched[pz, px] = true;
                        }
                        else
                            heights[pz, px] = Mathf.Lerp(heights[pz, px], candidate, influence);
                    }
                }
            }
        }

        private static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static List<(Vector2 xz, float y)> DensifyWithProfile(
            IReadOnlyList<Vector2> xz,
            IReadOnlyList<float> y,
            float step)
        {
            var result = new List<(Vector2, float)>();
            if (xz.Count == 0)
                return result;
            result.Add((xz[0], y[0]));
            for (var i = 0; i < xz.Count - 1; i++)
            {
                var a = xz[i];
                var b = xz[i + 1];
                var ya = y[Mathf.Min(i, y.Count - 1)];
                var yb = y[Mathf.Min(i + 1, y.Count - 1)];
                var dist = Vector2.Distance(a, b);
                var segments = Mathf.Max(1, Mathf.CeilToInt(dist / step));
                for (var s = 1; s <= segments; s++)
                {
                    var t = s / (float)segments;
                    result.Add((Vector2.Lerp(a, b, t), Mathf.Lerp(ya, yb, t)));
                }
            }
            return result;
        }
    }
}

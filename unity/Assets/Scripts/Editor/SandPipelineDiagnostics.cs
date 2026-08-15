#if UNITY_EDITOR
using System.IO;
using System.Text;
using GameDev.World;
using GameDev.World.Terrain;
using JBooth.MicroSplat;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Problem B diagnostics: find where coastal sand disappears
    /// (mask → alphamap → MicroSplat control → shader). Does not change height.
    /// </summary>
    public static class SandPipelineDiagnostics
    {
        public const string OverlayFolder = "Assets/Settings/Vista/SandDebug";
        public const int ShoreLayerIndex = 1;

        [MenuItem("GameDev/Overworld/Debug/Diagnose Sand Pipeline")]
        public static void DiagnoseMenu()
        {
            Diagnose(OverworldTerrainAccess.FindTerrains(), OverworldTerrainMenu.DefaultSeed, exportOverlay: true);
        }

        [MenuItem("GameDev/Overworld/Debug/Sand Mask Overlay")]
        public static void OverlayMenu()
        {
            Diagnose(OverworldTerrainAccess.FindTerrains(), OverworldTerrainMenu.DefaultSeed, exportOverlay: true);
        }

        public static void Diagnose(UnityEngine.Terrain[] terrains, int worldSeed, bool exportOverlay)
        {
            if (terrains == null || terrains.Length == 0)
            {
                Debug.LogWarning("[SandDiag] No terrains.");
                return;
            }

            var beachYawDeg = worldSeed != 0
                ? CoastalBeachShelf.SampleBeachYawDegrees(worldSeed)
                : 0f;
            var useBeachArc = worldSeed != 0;

            var totalNearSea = 0;
            var totalUnderwaterSand = 0;
            var totalWetSand = 0;
            var totalDrySand = 0;
            var totalSandWeightSum = 0f;
            var totalSandSamples = 0;
            var sandWeightMax = 0f;

            EnsureOverlayFolder();

            for (var t = 0; t < terrains.Length; t++)
            {
                var terrain = terrains[t];
                if (terrain == null || terrain.terrainData == null)
                    continue;

                DiagnoseTile(
                    terrain,
                    beachYawDeg,
                    useBeachArc,
                    exportOverlay,
                    ref totalNearSea,
                    ref totalUnderwaterSand,
                    ref totalWetSand,
                    ref totalDrySand,
                    ref totalSandWeightSum,
                    ref totalSandSamples,
                    ref sandWeightMax);
            }

            var mean = totalSandSamples > 0 ? totalSandWeightSum / totalSandSamples : 0f;
            Debug.Log(
                $"[SandDiag] TOTAL nearSea={totalNearSea} underwaterSand={totalUnderwaterSand} " +
                $"wetSand={totalWetSand} drySand={totalDrySand} " +
                $"sandWeightMean={mean:F4} sandWeightMax={sandWeightMax:F3} beachYaw={beachYawDeg:F0}°");

            if (totalNearSea > 0 && sandWeightMax < 0.05f)
            {
                Debug.LogWarning(
                    "[SandDiag] BRANCH: nearSea>0 but sandWeight≈0 → sand mask / wetSand classification.");
            }
            else if (totalUnderwaterSand > 0 || totalWetSand > 0)
            {
                Debug.Log(
                    "[SandDiag] BRANCH: underwater/wet sand exists in alphamap → if Scene view has no sand, " +
                    "check TerrainLayer / MicroSplat control / _HYBRIDHEIGHTBLEND / texture array.");
            }
            else if (totalNearSea > 0 && totalWetSand == 0)
            {
                Debug.LogWarning(
                    "[SandDiag] BRANCH: nearSea>0 but wetSand==0 → wet classification (sea01 / beachW / dLand).");
            }
        }

        private static void DiagnoseTile(
            UnityEngine.Terrain terrain,
            float beachYawDeg,
            bool useBeachArc,
            bool exportOverlay,
            ref int totalNearSea,
            ref int totalUnderwaterSand,
            ref int totalWetSand,
            ref int totalDrySand,
            ref float totalSandWeightSum,
            ref int totalSandSamples,
            ref float sandWeightMax)
        {
            var data = terrain.terrainData;
            var layers = data.terrainLayers;
            var hRes = data.heightmapResolution;
            var aRes = data.alphamapResolution;
            var heights = data.GetHeights(0, 0, hRes, hRes);
            var maps = data.GetAlphamaps(0, 0, aRes, aRes);
            var sizeY = Mathf.Max(0.01f, data.size.y);
            var sizeX = Mathf.Max(0.01f, data.size.x);
            var sizeZ = Mathf.Max(0.01f, data.size.z);
            var origin = terrain.GetPosition();
            var terrainBaseY = origin.y;
            var seaLevel = WorldScale.SeaLevelMeters;
            var sea01 = (seaLevel - terrainBaseY) / sizeY;
            var sandFullEndAsl = seaLevel + 2.5f;
            var sandFadeEndAsl = seaLevel + 30f;
            var cellMeters = Mathf.Max(0.25f, 0.5f * (sizeX + sizeZ) / Mathf.Max(1, hRes - 1));
            const float underwaterSandMaxDistMeters = 320f;
            var maxDistCells = Mathf.Max(
                4,
                Mathf.CeilToInt(underwaterSandMaxDistMeters / cellMeters) + 2);
            var distLand = BuildFloodDist(heights, hRes, maxDistCells, h => h > sea01);
            var cx = WorldScale.WorldExtentXMeters * 0.5f;
            var cz = WorldScale.WorldExtentZMeters * 0.5f;
            var shoreIndex = FindShoreLayerIndex(layers);

            var layerNames = new StringBuilder();
            if (layers != null)
            {
                for (var i = 0; i < layers.Length; i++)
                {
                    if (i > 0) layerNames.Append(", ");
                    layerNames.Append(layers[i] != null ? layers[i].name : "null");
                }
            }

            float tMin = float.MaxValue, tMax = float.MinValue;
            for (var z = 0; z < hRes; z++)
            {
                for (var x = 0; x < hRes; x++)
                {
                    var asl = terrainBaseY + heights[z, x] * sizeY;
                    tMin = Mathf.Min(tMin, asl);
                    tMax = Mathf.Max(tMax, asl);
                }
            }

            var nearSea = 0;
            var underWater = 0;
            var underwaterSand = 0;
            var wetSand = 0;
            var drySand = 0;
            var sandSum = 0f;
            var sandCount = 0;
            var localMax = 0f;

            Texture2D overlay = null;
            if (exportOverlay)
                overlay = new Texture2D(aRes, aRes, TextureFormat.RGBA32, false);

            // Probe picks
            var probeUnder = new Vector2Int(-1, -1);
            var probeShore = new Vector2Int(-1, -1);
            var probeDry = new Vector2Int(-1, -1);
            var probeGrass = new Vector2Int(-1, -1);
            var bestUnderSand = -1f;
            var bestShore = float.MaxValue;
            var bestDry = -1f;
            var bestGrassFar = -1f;

            for (var z = 0; z < aRes; z++)
            {
                var vz = z / (float)(aRes - 1);
                var hz = Mathf.RoundToInt(vz * (hRes - 1));
                for (var x = 0; x < aRes; x++)
                {
                    var ux = x / (float)(aRes - 1);
                    var hx = Mathf.RoundToInt(ux * (hRes - 1));
                    var h = heights[hz, hx];
                    var asl = terrainBaseY + h * sizeY;
                    var dLand = MinDist(distLand, hRes, hz, hx);
                    var dLandM = dLand < 0 ? 1e6f : dLand * cellMeters;
                    var isUnder = h <= sea01;
                    var inHeightSandBand = !isUnder && asl <= sandFadeEndAsl;
                    var isNearSea = isUnder
                        ? dLandM <= underwaterSandMaxDistMeters
                        : inHeightSandBand;
                    if (isNearSea)
                        nearSea++;
                    if (isUnder)
                        underWater++;

                    var sandW = 0f;
                    if (shoreIndex >= 0 && shoreIndex < maps.GetLength(2))
                        sandW = maps[z, x, shoreIndex];
                    sandSum += sandW;
                    sandCount++;
                    localMax = Mathf.Max(localMax, sandW);

                    var maskWet = isUnder && dLandM <= underwaterSandMaxDistMeters;
                    var maskDry = !isUnder && sandW > 0.45f;
                    if (maskWet)
                        wetSand++;
                    if (isUnder && sandW > 0.15f && dLandM <= underwaterSandMaxDistMeters)
                        underwaterSand++;
                    if (maskDry)
                        drySand++;

                    // Probes
                    if (isUnder && dLandM < 80f && sandW > bestUnderSand)
                    {
                        bestUnderSand = sandW;
                        probeUnder = new Vector2Int(x, z);
                    }

                    if (!isUnder && asl < bestShore && asl <= sandFullEndAsl + 4f)
                    {
                        bestShore = asl;
                        probeShore = new Vector2Int(x, z);
                    }

                    if (!isUnder && sandW > bestDry && asl <= sandFadeEndAsl)
                    {
                        bestDry = sandW;
                        probeDry = new Vector2Int(x, z);
                    }

                    if (!isUnder && sandW < 0.1f && asl > sandFadeEndAsl + 4f)
                    {
                        var heightAbove = asl - sandFadeEndAsl;
                        if (heightAbove > bestGrassFar)
                        {
                            bestGrassFar = heightAbove;
                            probeGrass = new Vector2Int(x, z);
                        }
                    }

                    if (overlay != null)
                    {
                        // Actual alphamap view: cyan=underwater sand, yellow=dry sand,
                        // magenta=wet-mask with no alphamap sand, green=grass, brown=rock/other
                        Color c;
                        if (isUnder && sandW > 0.15f)
                            c = new Color(0.1f, 0.9f, 1f); // underwater sand
                        else if (maskWet && sandW <= 0.15f)
                            c = new Color(1f, 0.1f, 1f); // wet mask but missing splat
                        else if (!isUnder && sandW > 0.55f)
                            c = new Color(1f, 0.85f, 0.1f); // dry sand
                        else if (!isUnder && sandW > 0.15f)
                            c = new Color(0.95f, 0.55f, 0.15f); // weak dry sand
                        else if (!isUnder)
                            c = new Color(0.15f, 0.75f, 0.2f); // grass/land
                        else
                            c = new Color(0.25f, 0.15f, 0.08f); // underwater rock
                        overlay.SetPixel(x, z, c);
                    }
                }
            }

            var msControlShoreMean = SampleMicroSplatControlShoreMean(terrain, shoreIndex);
            var mat = terrain.materialTemplate;
            var shaderName = mat != null && mat.shader != null ? mat.shader.name : "(null)";
            var hybrid = KeywordEnabled(terrain, "_HYBRIDHEIGHTBLEND");

            Debug.Log(
                $"[SandDiag] tile='{terrain.name}' seaLevel={seaLevel:F1} " +
                $"sandASL={sandFullEndAsl:F1}..{sandFadeEndAsl:F1} " +
                $"terrainMin={tMin:F1} terrainMax={tMax:F1}\n" +
                $"  heightBand={nearSea} underWater={underWater} underwaterSand={underwaterSand} " +
                $"wetSand={wetSand} drySand={drySand} " +
                $"sandWeightMean={(sandCount > 0 ? sandSum / sandCount : 0f):F4} sandWeightMax={localMax:F3}\n" +
                $"  shoreIndex={shoreIndex} layers=[{layerNames}] shader={shaderName} " +
                $"hybridHeightBlend={hybrid} msControlShoreMean={msControlShoreMean:F4}");

            LogProbe(terrain, "underwaterNearShore", probeUnder, heights, maps, hRes, aRes, sizeY, terrainBaseY,
                sea01, distLand, cellMeters, beachYawDeg, useBeachArc, shoreIndex, cx, cz, origin, sizeX, sizeZ);
            LogProbe(terrain, "shoreline", probeShore, heights, maps, hRes, aRes, sizeY, terrainBaseY,
                sea01, distLand, cellMeters, beachYawDeg, useBeachArc, shoreIndex, cx, cz, origin, sizeX, sizeZ);
            LogProbe(terrain, "dryBeach", probeDry, heights, maps, hRes, aRes, sizeY, terrainBaseY,
                sea01, distLand, cellMeters, beachYawDeg, useBeachArc, shoreIndex, cx, cz, origin, sizeX, sizeZ);
            LogProbe(terrain, "grassBehind", probeGrass, heights, maps, hRes, aRes, sizeY, terrainBaseY,
                sea01, distLand, cellMeters, beachYawDeg, useBeachArc, shoreIndex, cx, cz, origin, sizeX, sizeZ);

            if (overlay != null)
            {
                overlay.Apply();
                var path = $"{OverlayFolder}/SandOverlay_{terrain.name}.png";
                File.WriteAllBytes(path, overlay.EncodeToPNG());
                Object.DestroyImmediate(overlay);
                AssetDatabase.ImportAsset(path);
                Debug.Log($"[SandDiag] Overlay PNG → {path}");
            }

            totalNearSea += nearSea;
            totalUnderwaterSand += underwaterSand;
            totalWetSand += wetSand;
            totalDrySand += drySand;
            totalSandWeightSum += sandSum;
            totalSandSamples += sandCount;
            sandWeightMax = Mathf.Max(sandWeightMax, localMax);
        }

        private static void LogProbe(
            UnityEngine.Terrain terrain,
            string label,
            Vector2Int px,
            float[,] heights,
            float[,,] maps,
            int hRes,
            int aRes,
            float sizeY,
            float terrainBaseY,
            float sea01,
            int[,] distLand,
            float cellMeters,
            float beachYawDeg,
            bool useBeachArc,
            int shoreIndex,
            float cx,
            float cz,
            Vector3 origin,
            float sizeX,
            float sizeZ)
        {
            if (px.x < 0)
            {
                Debug.Log($"[SandDiag] probe {label} on '{terrain.name}': (none found)");
                return;
            }

            var ux = px.x / (float)(aRes - 1);
            var vz = px.y / (float)(aRes - 1);
            var hx = Mathf.RoundToInt(ux * (hRes - 1));
            var hz = Mathf.RoundToInt(vz * (hRes - 1));
            var h = heights[hz, hx];
            var asl = terrainBaseY + h * sizeY;
            var dLand = MinDist(distLand, hRes, hz, hx);
            var worldX = origin.x + ux * sizeX;
            var worldZ = origin.z + vz * sizeZ;
            var yawDeg = Mathf.Atan2(worldZ - cz, worldX - cx) * Mathf.Rad2Deg;
            var beachW = useBeachArc
                ? CoastalBeachShelf.BeachArcWeight(yawDeg, beachYawDeg)
                : 1f;
            var sandW = shoreIndex >= 0 ? maps[px.y, px.x, shoreIndex] : 0f;
            var under = h <= sea01;
            Debug.Log(
                $"[SandDiag] probe {label} '{terrain.name}' asl={asl:F2} under={under} " +
                $"dLand={(dLand < 0 ? -1 : dLand * cellMeters):F1}m " +
                $"beachW={Mathf.Clamp01(beachW):F2} sandWeight={sandW:F3} yaw={yawDeg:F0}°");
        }

        private static int FindShoreLayerIndex(TerrainLayer[] layers)
        {
            if (layers == null)
                return ShoreLayerIndex;
            for (var i = 0; i < layers.Length; i++)
            {
                if (layers[i] == null)
                    continue;
                var n = layers[i].name.ToLowerInvariant();
                if (n.Contains("shore") || n.Contains("sand"))
                    return i;
            }

            return layers.Length > ShoreLayerIndex ? ShoreLayerIndex : -1;
        }

        private static float SampleMicroSplatControlShoreMean(UnityEngine.Terrain terrain, int shoreIndex)
        {
            if (terrain == null || terrain.terrainData == null || shoreIndex < 0)
                return -1f;
            var alphas = terrain.terrainData.alphamapTextures;
            if (alphas == null || alphas.Length == 0 || alphas[0] == null)
                return -1f;

            // First control: R=0 G=1 B=2 A=3
            var tex = alphas[0];
            var w = tex.width;
            var h = tex.height;
            var pixels = tex.GetPixels32();
            double sum = 0;
            for (var i = 0; i < pixels.Length; i++)
            {
                float v;
                switch (shoreIndex)
                {
                    case 0: v = pixels[i].r / 255f; break;
                    case 1: v = pixels[i].g / 255f; break;
                    case 2: v = pixels[i].b / 255f; break;
                    case 3: v = pixels[i].a / 255f; break;
                    default: v = 0f; break;
                }

                sum += v;
            }

            return (float)(sum / Mathf.Max(1, pixels.Length));
        }

        private static bool KeywordEnabled(UnityEngine.Terrain terrain, string keyword)
        {
            var ms = terrain.GetComponent<MicroSplatTerrain>();
            if (ms == null || ms.keywordSO == null || ms.keywordSO.keywords == null)
                return false;
            return ms.keywordSO.keywords.Contains(keyword);
        }

        private static int MinDist(int[,] dist, int hRes, int hz, int hx)
        {
            if (hz < 0 || hx < 0 || hz >= hRes || hx >= hRes)
                return -1;
            return dist[hz, hx];
        }

        private static int[,] BuildFloodDist(
            float[,] heights,
            int hRes,
            int maxDistCells,
            System.Func<float, bool> isSeed)
        {
            var dist = new int[hRes, hRes];
            var queue = new int[hRes * hRes];
            var head = 0;
            var tail = 0;
            for (var z = 0; z < hRes; z++)
            {
                for (var x = 0; x < hRes; x++)
                {
                    if (isSeed(heights[z, x]))
                    {
                        dist[z, x] = 0;
                        queue[tail++] = (z << 16) | x;
                    }
                    else
                        dist[z, x] = -1;
                }
            }

            while (head < tail)
            {
                var packed = queue[head++];
                var z = packed >> 16;
                var x = packed & 0xFFFF;
                var d = dist[z, x];
                if (d >= maxDistCells)
                    continue;
                Try(z - 1, x, d + 1);
                Try(z + 1, x, d + 1);
                Try(z, x - 1, d + 1);
                Try(z, x + 1, d + 1);
            }

            void Try(int nz, int nx, int d)
            {
                if (nz < 0 || nx < 0 || nz >= hRes || nx >= hRes)
                    return;
                if (dist[nz, nx] != -1)
                    return;
                dist[nz, nx] = d;
                queue[tail++] = (nz << 16) | nx;
            }

            return dist;
        }

        private static void EnsureOverlayFolder()
        {
            if (AssetDatabase.IsValidFolder(OverlayFolder))
                return;
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");
            if (!AssetDatabase.IsValidFolder("Assets/Settings/Vista"))
                AssetDatabase.CreateFolder("Assets/Settings", "Vista");
            if (!AssetDatabase.IsValidFolder(OverlayFolder))
                AssetDatabase.CreateFolder("Assets/Settings/Vista", "SandDebug");
        }
    }
}
#endif

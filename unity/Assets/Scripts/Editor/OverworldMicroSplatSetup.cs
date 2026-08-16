#if UNITY_EDITOR
using System.Collections.Generic;
using GameDev.World;
using GameDev.World.Terrain;
using JBooth.MicroSplat;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// MicroSplat Convert for the 2×2 overworld. V0 paints degree-based MS_* layers then
    /// Convert (Vista TextureOutputs alone left white/missing arrays across tiles).
    /// </summary>
    public static class OverworldMicroSplatSetup
    {
        private const string LayersFolder = "Assets/Settings/TerrainLayers/MicroSplat";
        private const string MicroSplatTextures =
            "Packages/com.jbooth.microsplat.core/Examples/Textures";
        private const string MicroSplatDataFolder = "Assets/Settings/Vista/MicroSplatData";

        /// <summary>Fallback UV tile for any layer not in <see cref="RoleTileMeters"/>.</summary>
        private const float UnifiedTileMeters = 12f;

        private const string VistaSamples =
            "Assets/PinwheelStudio/Vista/Personal/Samples/TerrainLayers";

        /// <summary>
        /// Per-role UV tile in metres. One shared 12 m tile made every rock face read as wallpaper.
        /// The Triplanar / Anti-Tiling / Texture Clusters MicroSplat modules are not installed, so
        /// <c>_TRIPLANAR</c> and <c>_STOCHASTIC</c> do not exist here and tile size is the only knob.
        /// <para>
        /// On its own it is a weak lever (18 m vs 12 m only drops a 500 m wall from ~42 repeats to ~28).
        /// What actually breaks the periodic read is pairing it with the noise/curvature rock↔cliff mix
        /// in <see cref="PaintUnifiedIslandSplats"/>: two different rock textures whose repeat grids
        /// (1400/18 = 78 and 1400/14 = 100) beat at ~350 m, interleaved on a 520 m noise.
        /// </para>
        /// <para>
        /// MicroSplat needs no manual setup: Convert enables <c>_PERTEXUVSCALEOFFSET</c> and
        /// <c>_USEGRADMIP</c> by itself once tileSizes differ, and writes per-layer scales into propData.
        /// Its "wildly varying UV scales" warning triggers on <c>Vector2.Distance(minTile, maxTile) &gt; 10</c>
        /// — i.e. on the tile sizes themselves, not the repeat counts. The 11..18 spread here gives 9.90,
        /// just under it. Widening the table past a 7 m spread starts logging that warning.
        /// </para>
        /// Sand stays at 12 m deliberately: the coast is separately tuned and must not shift.
        /// </summary>
        private static readonly (string Name, float TileMeters)[] RoleTileMeters =
        {
            ("MS_Shore_Sand", 12f),
            ("MS_Moss_Fields", 11f),
            ("MS_Snow_Peak", 13f),
            ("MS_Rock_Cliff", 14f),
            ("MS_Cliff_Rock", 18f),
        };

        private static float TileMetersFor(string layerName)
        {
            if (string.IsNullOrEmpty(layerName))
                return UnifiedTileMeters;
            for (var i = 0; i < RoleTileMeters.Length; i++)
            {
                if (string.Equals(RoleTileMeters[i].Name, layerName, System.StringComparison.Ordinal))
                    return RoleTileMeters[i].TileMeters;
            }

            return UnifiedTileMeters;
        }

        /// <summary>StampIT heightmaps recommended for Vista LoadTexture / island detail (not albedo).</summary>
        public static readonly string[] RecommendedStampItHeightmaps =
        {
            "Assets/Rowlan/Terrain/Stamps/Examples/Heightmaps/Islands 07.png",
            "Assets/Rowlan/Terrain/Stamps/Examples/Heightmaps/Islands 23.png",
            "Assets/Rowlan/Terrain/Stamps/Examples/Heightmaps/Shores 09.png",
            "Assets/Rowlan/Terrain/Stamps/Examples/Heightmaps/Sandy Beaches 05.png",
            "Assets/Rowlan/Terrain/Stamps/Examples/Heightmaps/Meadows 12.png",
            "Assets/Rowlan/Terrain/Stamps/Examples/Heightmaps/Highlands 09.png",
            "Assets/Rowlan/Terrain/Stamps/Examples/Heightmaps/Alpine Mountains 23.png",
            "Assets/Rowlan/Terrain/Stamps/Examples/Heightmaps/Tundra 01.png",
            "Assets/Rowlan/Terrain/Stamps/Examples/Heightmaps/Rugged Rocks 14.png",
        };

        [MenuItem("GameDev/Overworld/Setup MicroSplat Terrain Layers")]
        public static void SetupLayersMenu()
        {
            var layers = EnsureFallbackLayers();
            var terrain = OverworldTerrainAccess.FindTerrain();
            if (terrain != null && terrain.terrainData != null && layers.Length > 0)
            {
                terrain.terrainData.terrainLayers = layers;
                EditorUtility.SetDirty(terrain.terrainData);
            }

            Debug.Log(
                $"[Overworld] Fallback MicroSplat TerrainLayers ready ({layers.Length}). " +
                "Prefer Vista Mountain layers after Generate; Convert preserves them.");
        }

        /// <summary>
        /// Always paint a degree-based splat with known MicroSplat example textures.
        /// Vista TextureOutputs can leave white/missing array slots across the 2×2 after Convert;
        /// geography paint is the reliable V0 look until Vista layer stacks are validated.
        /// </summary>
        public static void PaintOrPreserveVistaSplats(UnityEngine.Terrain[] terrains, int worldSeed = 0)
        {
            if (terrains == null || terrains.Length == 0)
                return;

            PaintUnifiedIslandSplats(terrains, worldSeed);
        }

        /// <summary>
        /// The real material source of truth for the overworld: this replaces terrainLayers and every
        /// alphamap weight, so the Vista graphs' TextureOutputs never reach the screen.
        /// <para>
        /// Land classification is multi-criteria — slope, elevation, three octaves of spatial noise and
        /// local relief (convex ridge / concave gully) all move the thresholds, so no single slope or
        /// height contour can run around the whole mountain. Rock vs cliff is a noise+curvature mix of
        /// one "not grass" mass rather than a second slope threshold, which is what used to draw the
        /// horizontal band at 55°.
        /// </para>
        /// Dry sand = ASL height band (not flood-distance ring). Underwater sand = near-land only.
        /// Yaw arc biases cliff coasts only — never gates height-based sand.
        /// </summary>
        public static void PaintUnifiedIslandSplats(UnityEngine.Terrain[] terrains, int worldSeed = 0)
        {
            if (terrains == null || terrains.Length == 0)
                return;

            var layers = EnsureFallbackLayers();
            if (layers == null || layers.Length < 5)
            {
                Debug.LogWarning("[Overworld] PaintUnifiedIslandSplats: need ≥5 fallback layers.");
                return;
            }

            // 0 grass, 1 shore, 2 cliff, 3 rock, 4 snow
            const int grass = 0;
            const int shore = 1;
            const int cliff = 2;
            const int rock = 3;
            const int snow = 4;

            // Local-relief neighbourhood: big enough to separate a ridge crest from a gully floor,
            // small enough not to smear the mountain's macro shape into the mean.
            const float reliefRadiusMeters = 26f;
            const float reliefScaleMeters = 6f;

            var beachYawDeg = worldSeed != 0
                ? CoastalBeachShelf.SampleBeachYawDegrees(worldSeed)
                : 0f;
            var useBeachArc = worldSeed != 0;

            // Height-primary sand band (ASL metres). Wide soft fade above FullEnd; noise breaks contour rings.
            var seaAsl = WorldScale.SeaLevelMeters;
            const float sandFullEndAboveSea = 2.5f;
            const float sandFadeEndAboveSea = 30f;
            var sandFullEndAsl = seaAsl + sandFullEndAboveSea;
            var sandFadeEndAsl = seaAsl + sandFadeEndAboveSea;
            const float sandThresholdNoiseMeters = 7f;
            const float sandNoiseMetersPerCycle = 95f;
            // Underwater: still need land proximity so deep ocean floor is not all sand.
            const float underwaterSandMaxDistMeters = 320f;

            for (var t = 0; t < terrains.Length; t++)
            {
                var terrain = terrains[t];
                if (terrain == null || terrain.terrainData == null)
                    continue;

                var data = terrain.terrainData;
                data.terrainLayers = layers;
                var res = data.alphamapResolution;
                var hRes = data.heightmapResolution;
                var maps = new float[res, res, layers.Length];
                // Explicit flag instead of inferring "underwater" from weights. The old test
                // (grass < 0.02 && rock > 0.05) also matches bare and snow-capped summits, which would
                // silently exclude the whole peak from the feather/soften passes.
                var isUnderwater = new bool[res, res];
                var heights = data.GetHeights(0, 0, hRes, hRes);
                var sizeY = Mathf.Max(0.01f, data.size.y);
                var sizeX = Mathf.Max(0.01f, data.size.x);
                var sizeZ = Mathf.Max(0.01f, data.size.z);
                var stepX = sizeX / Mathf.Max(1, hRes - 1);
                var stepZ = sizeZ / Mathf.Max(1, hRes - 1);
                var origin = terrain.GetPosition();
                var terrainBaseY = origin.y;
                var cx = WorldScale.WorldExtentXMeters * 0.5f;
                var cz = WorldScale.WorldExtentZMeters * 0.5f;
                // Match CoastalBeachShelf sea sampling (origin.y aware).
                var sea01 = (seaAsl - terrainBaseY) / sizeY;
                var cellMeters = Mathf.Max(0.25f, 0.5f * (sizeX + sizeZ) / Mathf.Max(1, hRes - 1));
                var maxDistCells = Mathf.Max(
                    4,
                    Mathf.CeilToInt(underwaterSandMaxDistMeters / cellMeters) + 2);
                // Only dist-to-land for underwater sand; dry sand ignores flood rings.
                var distLand = BuildFloodDist(heights, hRes, maxDistCells, h => h > sea01);
                var reliefRadiusCells = Mathf.Max(1, Mathf.RoundToInt(reliefRadiusMeters / cellMeters));
                var localMean = BuildLocalMeanHeight(heights, hRes, reliefRadiusCells);

                var drySandPx = 0;
                var wetSandPx = 0;
                var underwaterSandPx = 0;
                var heightSandPx = 0;

                for (var z = 0; z < res; z++)
                {
                    var vz = z / (float)(res - 1);
                    var hz = Mathf.RoundToInt(vz * (hRes - 1));
                    for (var x = 0; x < res; x++)
                    {
                        var ux = x / (float)(res - 1);
                        var hx = Mathf.RoundToInt(ux * (hRes - 1));
                        var dLand = MinDistNeighborhood(distLand, hRes, hz, hx);
                        var h = heights[hz, hx];
                        var asl = terrainBaseY + h * sizeY;

                        for (var l = 0; l < layers.Length; l++)
                            maps[z, x, l] = 0f;

                        var worldX = origin.x + ux * sizeX;
                        var worldZ = origin.z + vz * sizeZ;
                        var yawDeg = Mathf.Atan2(worldZ - cz, worldX - cx) * Mathf.Rad2Deg;
                        var beachW = useBeachArc
                            ? CoastalBeachShelf.BeachArcWeight(yawDeg, beachYawDeg)
                            : 1f;

                        if (h <= sea01)
                        {
                            isUnderwater[z, x] = true;
                            var dLandM = dLand < 0 ? 1e6f : dLand * cellMeters;
                            // Strong underwater sand near land (must read as sand, not rock tint).
                            if (dLandM <= underwaterSandMaxDistMeters)
                            {
                                var proxim = 1f - Mathf.SmoothStep(
                                    8f,
                                    underwaterSandMaxDistMeters,
                                    dLandM);
                                var wetW = Mathf.Clamp01(Mathf.Lerp(0.55f, 1f, proxim));
                                if (dLandM <= 60f)
                                    wetW = Mathf.Max(wetW, 0.88f);
                                maps[z, x, shore] = wetW;
                                maps[z, x, rock] = 1f - wetW;
                                wetSandPx++;
                                if (wetW > 0.15f)
                                    underwaterSandPx++;
                            }
                            else
                                maps[z, x, rock] = 1f;
                            continue;
                        }

                        if (asl <= sandFadeEndAsl)
                            heightSandPx++;

                        var hL = heights[hz, Mathf.Max(0, hx - 1)];
                        var hR = heights[hz, Mathf.Min(hRes - 1, hx + 1)];
                        var hD = heights[Mathf.Max(0, hz - 1), hx];
                        var hU = heights[Mathf.Min(hRes - 1, hz + 1), hx];
                        var riseX = Mathf.Abs(hR - hL) * sizeY;
                        var riseZ = Mathf.Abs(hU - hD) * sizeY;
                        // True gradient magnitude. max(axisX, axisZ) under-reports diagonals, which
                        // nudged material boundaries into aligning with the X/Z axes.
                        var gradX = riseX / (stepX * 2f);
                        var gradZ = riseZ / (stepZ * 2f);
                        var slopeDeg = Mathf.Atan(Mathf.Sqrt(gradX * gradX + gradZ * gradZ)) * Mathf.Rad2Deg;

                        // Convex crest / concave gully, in metres off the local mean.
                        var reliefM = (h - localMean[hz, hx]) * sizeY;
                        var convex01 = Mathf.Clamp01(reliefM / reliefScaleMeters);
                        var concave01 = Mathf.Clamp01(-reliefM / reliefScaleMeters);

                        // Height-primary dry sand + spatial noise so the edge is not a contour ring.
                        var n01 = SplatNoise01(worldX, worldZ, worldSeed, sandNoiseMetersPerCycle);
                        var nSigned = (n01 - 0.5f) * 2f;
                        var fullEnd = sandFullEndAsl + nSigned * (sandThresholdNoiseMeters * 0.45f);
                        var fadeEnd = sandFadeEndAsl + nSigned * sandThresholdNoiseMeters;
                        if (fadeEnd < fullEnd + 6f)
                            fadeEnd = fullEnd + 6f;

                        var shoreW = 0f;
                        if (asl <= fadeEnd)
                        {
                            if (asl <= fullEnd)
                                shoreW = 1f;
                            else
                                shoreW = 1f - Mathf.SmoothStep(fullEnd, fadeEnd, asl);

                            // Drop sand only on sheer walls.
                            shoreW *= 1f - Mathf.InverseLerp(58f, 78f, slopeDeg);
                            shoreW = Mathf.Clamp01(shoreW);
                        }

                        if (shoreW > 0.45f)
                            drySandPx++;

                        // Low ASL shelf: sand/grass win — no sawtooth rock ribbons on terrace faces.
                        var inSandBand = asl <= fadeEnd;
                        var cliffBoost = useBeachArc && beachW < 0.12f && inSandBand && slopeDeg > 55f
                            ? Mathf.InverseLerp(55f, 75f, slopeDeg)
                            : 0f;

                        // Three octaves of threshold noise (massif-scale, mid, fine). One 70 m octave at
                        // ±4° was not enough to stop a rock/grass join reading as a contour line.
                        var nMacro = SplatNoise01(worldX, worldZ, worldSeed ^ 0x0A53F1, 520f);
                        var nMid = SplatNoise01(worldX + 310f, worldZ - 140f, worldSeed ^ 0x17B9C3, 170f);
                        var nFine = SplatNoise01(worldX + 40f, worldZ - 17f, worldSeed ^ 0x5F3759DF, 70f);
                        var bandDeg = ((nMacro - 0.5f) * 2f) * 10f
                                      + ((nMid - 0.5f) * 2f) * 5f
                                      + ((nFine - 0.5f) * 2f) * 3f;

                        // Rock onset also depends on elevation and local relief, so the threshold is a
                        // different angle on every part of the massif.
                        var elev01 = Mathf.InverseLerp(seaAsl + 60f, seaAsl + 330f, asl);
                        var rockStartDeg = 36f
                                           - 14f * elev01 * (0.55f + 0.45f * nMacro)
                                           - 6f * convex01
                                           + 4f * concave01
                                           + bandDeg;
                        var rockEndDeg = rockStartDeg + Mathf.Lerp(30f, 18f, elev01);

                        var sandDominant = shoreW > 0.25f || (inSandBand && slopeDeg < 50f);
                        float cliffW;
                        float rockW;
                        if (sandDominant)
                        {
                            // Coast shelf keeps its own tuned behaviour — sand/grass must win here.
                            cliffW = Mathf.InverseLerp(70f, 88f, slopeDeg) * (1f - shoreW) * 0.35f;
                            rockW = Mathf.InverseLerp(62f, 80f, slopeDeg) * (1f - shoreW) * 0.15f;
                        }
                        else
                        {
                            // One "bare rock" mass, then split between the two rock layers by large-scale
                            // noise and curvature instead of a second slope threshold. The old
                            // InverseLerp(55, 78) drew a cliff belt right around the mountain.
                            var hardW = Mathf.InverseLerp(rockStartDeg, rockEndDeg, slopeDeg) * (1f - shoreW);
                            hardW = Mathf.Max(hardW, cliffBoost);
                            var steepBias = Mathf.Lerp(0.30f, 1f, Mathf.InverseLerp(40f, 72f, slopeDeg));
                            var cliffAffinity = Mathf.Clamp01(
                                0.50f
                                + 0.35f * ((nMacro - 0.5f) * 2f)
                                + 0.30f * convex01
                                - 0.20f * concave01);
                            cliffW = hardW * cliffAffinity * steepBias;
                            rockW = Mathf.Max(0f, hardW - cliffW);
                        }

                        // Kill mid-slope rock ribbons in the beach transition (the "ladder" look).
                        if (inSandBand && slopeDeg < 58f)
                        {
                            cliffW *= 0.08f;
                            rockW *= 0.12f;
                        }

                        // Debris/soil collects in hollows, not on crests.
                        var soilW = sandDominant
                            ? 0f
                            : Mathf.InverseLerp(8f, 28f, slopeDeg) * (1f - rockW) * (1f - cliffW) *
                              (1f - shoreW) * Mathf.Lerp(0.75f, 1.25f, concave01);

                        // AlpineSnowLineMeters, not SnowLineMeters: the latter is 95 m (119 m ASL), which
                        // is near the foot of a 500-580 m peak — harmless while snow was folded into rock
                        // at 0.65×, but it would bury the whole massif now that snow has a white layer.
                        // Line wanders ±80 m and is scoured off crests / drifted into hollows, so it never
                        // reads as a horizontal contour.
                        var snowLineAsl = seaAsl + WorldScale.AlpineSnowLineMeters
                                          + ((nMacro - 0.5f) * 2f) * 58f
                                          + ((nMid - 0.5f) * 2f) * 22f;
                        var snowW = Mathf.InverseLerp(snowLineAsl, snowLineAsl + 90f, asl);
                        snowW *= 1f - Mathf.InverseLerp(38f, 62f, slopeDeg);
                        snowW *= Mathf.Lerp(1f, 0.35f, convex01);
                        snowW *= Mathf.Lerp(0.85f, 1f, concave01);
                        snowW = Mathf.Clamp01(snowW);

                        var grassW = Mathf.Clamp01(1f - shoreW - cliffW - rockW - soilW - snowW);
                        maps[z, x, grass] = grassW;
                        maps[z, x, shore] = shoreW;
                        maps[z, x, cliff] = cliffW;
                        // Soil still has no layer of its own and rides the rock channel.
                        maps[z, x, rock] = Mathf.Clamp01(rockW + soilW * 0.45f);
                        maps[z, x, snow] = snowW;

                        var sum = 0f;
                        for (var l = 0; l < layers.Length; l++)
                            sum += maps[z, x, l];

                        if (sum > 1e-5f)
                        {
                            for (var l = 0; l < layers.Length; l++)
                                maps[z, x, l] /= sum;
                        }
                        else
                        {
                            maps[z, x, grass] = 1f;
                        }
                    }
                }

                // Soft transitions: wide sand↔grass feather, then global alphamap blur (all layers).
                FeatherShoreGrassBlend(maps, res, isUnderwater, shore, grass, rock, cliff, blurStrength: 0.7f, radius: 4);
                FeatherShoreGrassBlend(maps, res, isUnderwater, shore, grass, rock, cliff, blurStrength: 0.55f, radius: 3);
                FeatherShoreGrassBlend(maps, res, isUnderwater, shore, grass, rock, cliff, blurStrength: 0.4f, radius: 2);
                SoftenAllLayerBlends(maps, res, isUnderwater, blurStrength: 0.5f, radius: 2);
                SoftenAllLayerBlends(maps, res, isUnderwater, blurStrength: 0.35f, radius: 1);

                var shorePx = 0;
                var shoreStrong = 0;
                var shoreSum = 0f;
                for (var z = 0; z < res; z++)
                {
                    for (var x = 0; x < res; x++)
                    {
                        var w = maps[z, x, shore];
                        shoreSum += w;
                        if (w > 0.15f)
                            shorePx++;
                        if (w > 0.55f)
                            shoreStrong++;
                    }
                }

                data.SetAlphamaps(0, 0, maps);
                EditorUtility.SetDirty(data);
                Debug.Log(
                    $"[Overworld] splat '{terrain.name}' shoreMean={shoreSum / (res * res):F3} " +
                    $"shorePx>0.15={shorePx} drySand={drySandPx} wetSand={wetSandPx} " +
                    $"underwaterSand={underwaterSandPx} heightBandPx={heightSandPx} " +
                    $"(of {res * res}) sandASL={sandFullEndAsl:F1}..{sandFadeEndAsl:F1} " +
                    $"beachYaw={(useBeachArc ? beachYawDeg.ToString("F0") : "off")}° " +
                    $"sea={seaAsl:F1}");
            }

            Debug.Log(
                $"[Overworld] Unified island splats on {terrains.Length} tiles " +
                "(height-band sand + underwater near-land; cliff coast prefers rock).");
        }

        private static int MinDistNeighborhood(int[,] dist, int hRes, int hz, int hx)
        {
            var best = int.MaxValue;
            for (var dz = -1; dz <= 1; dz++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    var zz = hz + dz;
                    var xx = hx + dx;
                    if (zz < 0 || xx < 0 || zz >= hRes || xx >= hRes)
                        continue;
                    var d = dist[zz, xx];
                    if (d >= 0 && d < best)
                        best = d;
                }
            }

            return best == int.MaxValue ? -1 : best;
        }

        /// <summary>
        /// Separable box mean of the heightmap. <c>height - mean</c> is the local relief used to tell a
        /// convex crest from a concave gully, which is what keeps rock/cliff/snow off pure slope and
        /// height contours. Two O(hRes² · radius) passes — ~11 M ops per 513² tile at radius 10.
        /// </summary>
        private static float[,] BuildLocalMeanHeight(float[,] heights, int hRes, int radiusCells)
        {
            radiusCells = Mathf.Clamp(radiusCells, 1, Mathf.Max(1, hRes / 4));
            var rowPass = new float[hRes, hRes];
            var result = new float[hRes, hRes];

            for (var z = 0; z < hRes; z++)
            {
                for (var x = 0; x < hRes; x++)
                {
                    var acc = 0f;
                    var n = 0;
                    var from = Mathf.Max(0, x - radiusCells);
                    var to = Mathf.Min(hRes - 1, x + radiusCells);
                    for (var xx = from; xx <= to; xx++)
                    {
                        acc += heights[z, xx];
                        n++;
                    }

                    rowPass[z, x] = acc / Mathf.Max(1, n);
                }
            }

            for (var z = 0; z < hRes; z++)
            {
                var from = Mathf.Max(0, z - radiusCells);
                var to = Mathf.Min(hRes - 1, z + radiusCells);
                for (var x = 0; x < hRes; x++)
                {
                    var acc = 0f;
                    var n = 0;
                    for (var zz = from; zz <= to; zz++)
                    {
                        acc += rowPass[zz, x];
                        n++;
                    }

                    result[z, x] = acc / Mathf.Max(1, n);
                }
            }

            return result;
        }

        /// <summary>BFS distance in cells from seed cells matching <paramref name="isSeed"/>.</summary>
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

        /// <summary>
        /// Blur shore↔grass (and bleed cliff down) for a soft beach fringe.
        /// Skips cells flagged underwater by the paint pass so they stay sand, not rock.
        /// </summary>
        private static void FeatherShoreGrassBlend(
            float[,,] maps,
            int res,
            bool[,] isUnderwater,
            int shore,
            int grass,
            int rock,
            int cliff,
            float blurStrength = 0.55f,
            int radius = 1)
        {
            if (maps == null || res < 3)
                return;

            blurStrength = Mathf.Clamp01(blurStrength);
            radius = Mathf.Clamp(radius, 1, 5);
            var shoreBlur = new float[res, res];
            var cliffBlur = new float[res, res];

            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    var accS = 0f;
                    var accC = 0f;
                    var w = 0f;
                    for (var dz = -radius; dz <= radius; dz++)
                    {
                        for (var dx = -radius; dx <= radius; dx++)
                        {
                            var zz = z + dz;
                            var xx = x + dx;
                            if (zz < 0 || xx < 0 || zz >= res || xx >= res)
                                continue;
                            var dist = Mathf.Abs(dx) + Mathf.Abs(dz);
                            var k = dist == 0 ? 3f : (dist == 1 ? 1.5f : 1f);
                            accS += maps[zz, xx, shore] * k;
                            accC += maps[zz, xx, cliff] * k;
                            w += k;
                        }
                    }

                    shoreBlur[z, x] = accS / Mathf.Max(1e-5f, w);
                    cliffBlur[z, x] = accC / Mathf.Max(1e-5f, w);
                }
            }

            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    // Underwater wet sand carries its own grass-free mix — do not feather.
                    if (isUnderwater[z, x])
                        continue;

                    var oldShore = maps[z, x, shore];
                    var newShore = Mathf.Lerp(oldShore, shoreBlur[z, x], blurStrength);
                    var delta = newShore - oldShore;
                    maps[z, x, shore] = newShore;
                    maps[z, x, grass] = Mathf.Clamp01(maps[z, x, grass] - delta);

                    // Soften cliff ribbons along the fringe.
                    var oldCliff = maps[z, x, cliff];
                    var newCliff = Mathf.Lerp(oldCliff, cliffBlur[z, x], blurStrength * 0.85f);
                    newCliff *= 1f - maps[z, x, shore] * 0.65f;
                    var cliffDelta = oldCliff - newCliff;
                    maps[z, x, cliff] = Mathf.Clamp01(newCliff);
                    maps[z, x, grass] = Mathf.Clamp01(maps[z, x, grass] + cliffDelta * 0.55f);
                    maps[z, x, rock] = Mathf.Clamp01(maps[z, x, rock] * (1f - maps[z, x, shore] * 0.35f));

                    var sum = 0f;
                    for (var l = 0; l < maps.GetLength(2); l++)
                        sum += maps[z, x, l];
                    if (sum > 1e-5f)
                    {
                        for (var l = 0; l < maps.GetLength(2); l++)
                            maps[z, x, l] /= sum;
                    }
                }
            }
        }

        /// <summary>
        /// Box-blur every alphamap layer then renormalize — softens rock/grass and residual hard edges.
        /// Skips cells flagged underwater by the paint pass.
        /// </summary>
        private static void SoftenAllLayerBlends(
            float[,,] maps,
            int res,
            bool[,] isUnderwater,
            float blurStrength = 0.45f,
            int radius = 2)
        {
            if (maps == null || res < 3)
                return;

            blurStrength = Mathf.Clamp01(blurStrength);
            radius = Mathf.Clamp(radius, 1, 4);
            var layerCount = maps.GetLength(2);
            var blurred = new float[res, res, layerCount];

            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    for (var l = 0; l < layerCount; l++)
                    {
                        var acc = 0f;
                        var w = 0f;
                        for (var dz = -radius; dz <= radius; dz++)
                        {
                            for (var dx = -radius; dx <= radius; dx++)
                            {
                                var zz = z + dz;
                                var xx = x + dx;
                                if (zz < 0 || xx < 0 || zz >= res || xx >= res)
                                    continue;
                                var dist = Mathf.Abs(dx) + Mathf.Abs(dz);
                                var k = dist == 0 ? 4f : (dist == 1 ? 2f : 1f);
                                acc += maps[zz, xx, l] * k;
                                w += k;
                            }
                        }

                        blurred[z, x, l] = acc / Mathf.Max(1e-5f, w);
                    }
                }
            }

            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    if (isUnderwater[z, x])
                        continue;

                    var sum = 0f;
                    for (var l = 0; l < layerCount; l++)
                    {
                        maps[z, x, l] = Mathf.Lerp(maps[z, x, l], blurred[z, x, l], blurStrength);
                        sum += maps[z, x, l];
                    }

                    if (sum > 1e-5f)
                    {
                        for (var l = 0; l < layerCount; l++)
                            maps[z, x, l] /= sum;
                    }
                }
            }
        }

        /// <summary>
        /// Deterministic 0..1 spatial noise for splat thresholds (breaks contour rings).
        /// </summary>
        private static float SplatNoise01(float worldX, float worldZ, int seed, float metersPerCycle)
        {
            metersPerCycle = Mathf.Max(8f, metersPerCycle);
            var s = seed * 0.00137f;
            var fx = worldX / metersPerCycle + s;
            var fz = worldZ / metersPerCycle + s * 1.73f;
            var n = Mathf.PerlinNoise(fx, fz);
            n = n * 0.62f + Mathf.PerlinNoise(fx * 2.17f + 19.1f, fz * 2.17f - 11.3f) * 0.38f;
            return Mathf.Clamp01(n);
        }

        /// <summary>
        /// True when Vista already wrote multi-layer weights (not a single flat channel).
        /// </summary>
        public static bool HasMeaningfulVistaLayerWeights(UnityEngine.Terrain[] terrains)
        {
            if (terrains == null)
                return false;

            for (var t = 0; t < terrains.Length; t++)
            {
                var terrain = terrains[t];
                if (terrain == null || terrain.terrainData == null)
                    continue;

                var data = terrain.terrainData;
                var layers = data.terrainLayers;
                if (layers == null || layers.Length < 2)
                    continue;

                var res = data.alphamapResolution;
                if (res < 8)
                    continue;

                var maps = data.GetAlphamaps(0, 0, res, res);
                var layerCount = maps.GetLength(2);
                if (layerCount < 2)
                    continue;

                var secondary = 0f;
                var samples = 0;
                for (var z = 0; z < res; z += Mathf.Max(1, res / 16))
                {
                    for (var x = 0; x < res; x += Mathf.Max(1, res / 16))
                    {
                        for (var l = 1; l < layerCount; l++)
                            secondary += maps[z, x, l];
                        samples++;
                    }
                }

                if (samples > 0 && secondary / samples > 0.04f)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Official MicroSplat workflow on the 2×2: shared layers (Vista's) → Convert → SyncAll.
        /// </summary>
        public static void ConvertAllTilesToMicroSplat(UnityEngine.Terrain[] terrains, int worldSeed = 0)
        {
            if (terrains == null || terrains.Length == 0)
            {
                Debug.LogWarning("[Overworld] Convert MicroSplat: no terrains.");
                return;
            }

            // Stale MicroSplatData (old arrays / white snow material) breaks re-bakes.
            DeleteMicroSplatCache();

            var list = new List<UnityEngine.Terrain>();
            for (var i = 0; i < terrains.Length; i++)
            {
                var terrain = terrains[i];
                if (terrain == null || terrain.terrainData == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(terrain.gameObject.name) ||
                    terrain.gameObject.name.StartsWith("Terrain", System.StringComparison.Ordinal))
                {
                    terrain.gameObject.name = $"OverworldTerrain_{i}";
                }

                list.Add(terrain);
            }

            if (list.Count == 0)
            {
                Debug.LogWarning("[Overworld] Convert MicroSplat: no valid terrains.");
                return;
            }

            // Shared layers from unified paint — do not prefer stale Vista snow weights.
            var shared = list[0].terrainData.terrainLayers;
            if (shared == null || shared.Length == 0)
            {
                shared = EnsureFallbackLayers();
            }

            if (shared == null || shared.Length == 0)
            {
                Debug.LogError("[Overworld] Convert MicroSplat: no TerrainLayers on tiles.");
                return;
            }

            ApplyRoleTileSizes(shared);

            for (var i = 0; i < list.Count; i++)
            {
                list[i].terrainData.terrainLayers = shared;
                EditorUtility.SetDirty(list[i].terrainData);
            }

            // Fresh Convert each bake (Clear recreates tiles). Drop stale MicroSplatTerrain if any.
            for (var i = 0; i < list.Count; i++)
            {
                var stale = list[i].GetComponent<JBooth.MicroSplat.MicroSplatTerrain>();
                if (stale != null)
                {
                    Object.DestroyImmediate(stale);
                }
            }

            JBooth.MicroSplat.MicroSplatTerrainEditor.ConvertTerrains(list.ToArray(), shared);
            JBooth.MicroSplat.MicroSplatTerrain.SyncAll();
            UnifyMicroSplatMaterialsAcrossTiles(list);
            // Convert always enables _HYBRIDHEIGHTBLEND — height channels in grass/rock crush sand
            // even when alphamap shore weight is high. Splat weights must win for coast sand.
            DisableHybridHeightBlend(list);
            // Convert can leave control maps stale; re-paint after Sync so sand sticks.
            PaintUnifiedIslandSplats(list.ToArray(), worldSeed);
            // Per-tile alphamap blur does not cross tile borders — stitch shared edges.
            var arr = list.ToArray();
            TerrainTileSeams.DiagnoseAndWarn(arr, "after-splat-pre-stitch");
            TerrainTileSeams.StitchAlphamaps(arr);
            TerrainTileSeams.WireNeighbors(arr);
            JBooth.MicroSplat.MicroSplatTerrain.SyncAll();
            TerrainTileSeams.DiagnoseAndWarn(arr, "after-seam-fix");
            Debug.Log(
                $"[Overworld] MicroSplat Convert done: tiles={list.Count} layers={shared.Length} " +
                $"(beach-arc splat + Convert + no HybridHeightBlend + re-paint, " +
                $"per-role UV tiles {DescribeTileSizes(shared)}, URP6.3).");
            SandPipelineDiagnostics.Diagnose(list.ToArray(), worldSeed, exportOverlay: true);
        }

        /// <summary>
        /// Drop _HYBRIDHEIGHTBLEND so alphamap shore weights render as sand (Problem B).
        /// </summary>
        private static void DisableHybridHeightBlend(List<UnityEngine.Terrain> list)
        {
            if (list == null || list.Count == 0)
                return;

            Material template = null;
            MicroSplatKeywords keywords = null;
            for (var i = 0; i < list.Count; i++)
            {
                var ms = list[i].GetComponent<MicroSplatTerrain>();
                if (ms == null)
                    continue;
                if (template == null && ms.templateMaterial != null)
                    template = ms.templateMaterial;
                if (keywords == null && ms.keywordSO != null)
                    keywords = ms.keywordSO;
                if (template != null && keywords != null)
                    break;
            }

            if (keywords == null || keywords.keywords == null)
            {
                Debug.LogWarning("[Overworld] DisableHybridHeightBlend: no keywordSO.");
                return;
            }

            if (!keywords.IsKeywordEnabled("_HYBRIDHEIGHTBLEND"))
            {
                Debug.Log("[Overworld] HybridHeightBlend already off.");
                return;
            }

            keywords.DisableKeyword("_HYBRIDHEIGHTBLEND");
            if (template != null)
            {
                var compiler = new MicroSplatShaderGUI.MicroSplatCompiler();
                compiler.Compile(template);
                Debug.Log("[Overworld] Removed _HYBRIDHEIGHTBLEND and recompiled MicroSplat shader.");
            }

            for (var i = 0; i < list.Count; i++)
            {
                var ms = list[i].GetComponent<MicroSplatTerrain>();
                if (ms != null)
                    ms.keywordSO = keywords;
            }

            MicroSplatTerrain.SyncAll();
        }

        /// <summary>
        /// Prevents half-and-half Scene view: every tile must share the same MicroSplat template
        /// and a high basemap distance (far basemap baking looks like flat solid green).
        /// </summary>
        private static void UnifyMicroSplatMaterialsAcrossTiles(List<UnityEngine.Terrain> list)
        {
            Material template = null;
            JBooth.MicroSplat.MicroSplatPropData propData = null;
            JBooth.MicroSplat.MicroSplatKeywords keywords = null;

            for (var i = 0; i < list.Count; i++)
            {
                var ms = list[i].GetComponent<JBooth.MicroSplat.MicroSplatTerrain>();
                if (ms == null || ms.templateMaterial == null)
                {
                    continue;
                }

                template = ms.templateMaterial;
                propData = ms.propData;
                keywords = ms.keywordSO;
                break;
            }

            if (template == null)
            {
                Debug.LogWarning("[Overworld] MicroSplat Unify: no templateMaterial after Convert.");
                return;
            }

            for (var i = 0; i < list.Count; i++)
            {
                var terrain = list[i];
                var ms = terrain.GetComponent<JBooth.MicroSplat.MicroSplatTerrain>();
                if (ms == null)
                {
                    ms = terrain.gameObject.AddComponent<JBooth.MicroSplat.MicroSplatTerrain>();
                }

                ms.templateMaterial = template;
                if (propData != null)
                {
                    ms.propData = propData;
                }

                if (keywords != null)
                {
                    ms.keywordSO = keywords;
                }

                // Far basemap = wrong solid colour on distant tiles (classic half-island look).
                terrain.basemapDistance = 20000f;
                terrain.drawInstanced = true;
            }

            JBooth.MicroSplat.MicroSplatTerrain.SyncAll();

            for (var i = 0; i < list.Count; i++)
            {
                var terrain = list[i];
                var mat = terrain.materialTemplate;
                var shaderName = mat != null && mat.shader != null ? mat.shader.name : "(null)";
                Debug.Log($"[Overworld] tile[{i}] '{terrain.name}' material={shaderName}");
            }
        }

        [MenuItem("GameDev/Overworld/MicroSplat/Delete Cache")]
        public static void DeleteMicroSplatCacheMenu()
        {
            DeleteMicroSplatCache();
        }

        /// <summary>
        /// Removes generated MicroSplatData so the next Convert is clean (no stale arrays/mats).
        /// </summary>
        public static void DeleteMicroSplatCache()
        {
            if (!AssetDatabase.IsValidFolder(MicroSplatDataFolder))
            {
                return;
            }

            if (AssetDatabase.DeleteAsset(MicroSplatDataFolder))
            {
                AssetDatabase.Refresh();
                Debug.Log($"[Overworld] Deleted MicroSplat cache: {MicroSplatDataFolder}");
            }
            else
            {
                Debug.LogWarning($"[Overworld] Failed to delete MicroSplat cache: {MicroSplatDataFolder}");
            }
        }

        [MenuItem("GameDev/Overworld/MicroSplat/Convert All Tiles")]
        public static void ConvertAllTilesMenu()
        {
            ConvertAllTilesToMicroSplat(OverworldTerrainAccess.FindTerrains());
        }

        /// <summary>
        /// Only when a tile has no layers yet. Do not call this to replace Vista's painted stack.
        /// </summary>
        public static void ApplyLayersToTerrainIfEmpty(UnityEngine.Terrain terrain)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                return;
            }

            var existing = terrain.terrainData.terrainLayers;
            if (existing != null && existing.Length > 0)
            {
                return;
            }

            terrain.terrainData.terrainLayers = EnsureFallbackLayers();
            EditorUtility.SetDirty(terrain.terrainData);
        }

        /// <summary>
        /// Fallback stack when Vista left no layers. Index order 0..3 is load-bearing —
        /// <see cref="SandPipelineDiagnostics.ShoreLayerIndex"/> and its control-channel switch assume
        /// shore == 1 — so snow is appended at the end rather than inserted.
        /// </summary>
        public static TerrainLayer[] EnsureFallbackLayers()
        {
            EnsureFolder();
            var list = new List<TerrainLayer>
            {
                // Vista Grass Dark = real green. MicroSplat moss_albedo is olive/mustard and reads as "sand inland".
                MakeLayerFromPaths(
                    "MS_Moss_Fields",
                    $"{VistaSamples}/Grass Dark/grass_dark_diff.jpg",
                    $"{VistaSamples}/Grass Dark/grass_dark_nor_gl.exr"),
                // Vista sample = real beige sand.
                MakeLayerFromPaths(
                    "MS_Shore_Sand",
                    $"{VistaSamples}/Sand/sand_diff.png",
                    $"{VistaSamples}/Sand/sand_nor_gl.png"),
                // Cliff uses Vista RockFace, not MicroSplat's cliff_albedo: the two MicroSplat example
                // rocks look near-identical, so the noise/curvature mix between cliff and rock was
                // invisible. A genuinely different rock at a 30 m tile against rock's 17 m is what
                // actually breaks the repeat.
                MakeLayerFromPaths(
                    "MS_Cliff_Rock",
                    $"{VistaSamples}/RockFace/rock_face_diff_1k.jpg",
                    $"{VistaSamples}/RockFace/rock_face_nor_gl_1k.exr"),
                MakeLayer("MS_Rock_Cliff", "rock_cliff_albedo.png", "rock_cliff_norm.tga"),
                // No real snow texture exists in this project (the only candidate was a synthetic white
                // asset that was already rejected once). Sediment remapped toward white keeps real grain
                // instead of reading as flat paint — same diffuseRemap trick already used for sand/moss.
                MakeLayerFromPaths(
                    "MS_Snow_Peak",
                    $"{VistaSamples}/Sediment/sediment_diff.jpg",
                    $"{VistaSamples}/Sediment/sediment_nor_gl.exr"),
            };
            AssetDatabase.SaveAssets();
            return list.ToArray();
        }

        /// <summary>Legacy name — prefer <see cref="EnsureFallbackLayers"/>.</summary>
        public static TerrainLayer[] EnsureLayers() => EnsureFallbackLayers();

        private static TerrainLayer[] ResolveSharedLayers(List<UnityEngine.Terrain> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var layers = list[i].terrainData.terrainLayers;
                if (layers != null && layers.Length > 0)
                {
                    return layers;
                }
            }

            return EnsureFallbackLayers();
        }

        /// <summary>
        /// Applies the per-role UV tile table. Must run before MicroSplat Convert so Convert sees the
        /// differing sizes and switches on per-texture UV scaling.
        /// </summary>
        private static void ApplyRoleTileSizes(TerrainLayer[] layers)
        {
            if (layers == null)
            {
                return;
            }

            for (var i = 0; i < layers.Length; i++)
            {
                if (layers[i] == null)
                {
                    continue;
                }

                var tile = TileMetersFor(layers[i].name);
                layers[i].tileSize = new Vector2(tile, tile);
                EditorUtility.SetDirty(layers[i]);
            }
        }

        private static string DescribeTileSizes(TerrainLayer[] layers)
        {
            if (layers == null || layers.Length == 0)
                return "(none)";
            var parts = new string[layers.Length];
            for (var i = 0; i < layers.Length; i++)
            {
                parts[i] = layers[i] == null
                    ? "null"
                    : $"{layers[i].name}={layers[i].tileSize.x:F0}m";
            }

            return string.Join(", ", parts);
        }

        private static TerrainLayer MakeLayer(string name, string albedoFile, string normalFile)
        {
            return MakeLayerFromPaths(
                name,
                $"{MicroSplatTextures}/{albedoFile}",
                $"{MicroSplatTextures}/{normalFile}");
        }

        private static TerrainLayer MakeLayerFromPaths(
            string name,
            string albedoPath,
            string normalPath)
        {
            var tileMeters = TileMetersFor(name);
            var path = $"{LayersFolder}/{name}.terrainlayer";
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer == null)
            {
                layer = new TerrainLayer { name = name };
                AssetDatabase.CreateAsset(layer, path);
            }

            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (albedo == null)
            {
                Debug.LogError($"[Overworld] TerrainLayer '{name}': missing albedo at '{albedoPath}'");
            }
            else
            {
                layer.diffuseTexture = albedo;
            }

            if (normal != null)
            {
                layer.normalMapTexture = normal;
            }

            layer.tileSize = new Vector2(tileMeters, tileMeters);
            if (name != null && name.IndexOf("Sand", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                layer.diffuseRemapMin = new Vector4(0f, 0f, 0f, 0f);
                layer.diffuseRemapMax = new Vector4(1.12f, 1.05f, 0.92f, 1f);
                Debug.Log($"[Overworld] Shore layer albedo='{(albedo != null ? albedo.name : "NULL")}'");
            }
            else if (name != null && name.IndexOf("Snow", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Sediment lifted to near-white with a cool cast — keeps grain so the peak does not
                // read as flat white paint.
                layer.diffuseRemapMin = new Vector4(0.72f, 0.75f, 0.80f, 0f);
                layer.diffuseRemapMax = new Vector4(1f, 1f, 1.05f, 1f);
                Debug.Log($"[Overworld] Snow layer albedo='{(albedo != null ? albedo.name : "NULL")}'");
            }
            else if (name != null && name.IndexOf("Moss", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Push plains toward green so they don't read as dry sand from afar.
                layer.diffuseRemapMin = new Vector4(0f, 0.05f, 0f, 0f);
                layer.diffuseRemapMax = new Vector4(0.75f, 1.05f, 0.55f, 1f);
                Debug.Log($"[Overworld] Grass layer albedo='{(albedo != null ? albedo.name : "NULL")}'");
            }

            EditorUtility.SetDirty(layer);
            return layer;
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Settings/TerrainLayers"))
            {
                AssetDatabase.CreateFolder("Assets/Settings", "TerrainLayers");
            }

            if (!AssetDatabase.IsValidFolder(LayersFolder))
            {
                AssetDatabase.CreateFolder("Assets/Settings/TerrainLayers", "MicroSplat");
            }
        }
    }
}
#endif

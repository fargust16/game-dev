#if UNITY_EDITOR
using System.Collections.Generic;
using Den.Tools;
using GameDev.World.Streaming;
using GameDev.World.Water;
using MapMagic.Core;
using MapMagic.Terrains;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Builds overworld lakes as <b>filled 2D areas</b> and rivers as separate line corridors.
    /// Nearby floods are merged into one lake / one plane / one pit. Basin dig is an absolute
    /// bowl under the water plane (min ~36 m for large lakes) for sunk-city volume.
    /// </summary>
    public static class WaterZoneMenu
    {
        private const string RootObjectName = "OverworldWater";
        private const string RiverRootObjectName = "OverworldRivers";
        private const float CellSize = 3f;
        private const int MinCellsPerZone = 28;
        private const int MaxZones = 20;
        private const float MaxLakeDepthMeters = 14f;
        private const int MaxCellsPerZone = 2400;
        private const int CellCountSafetyCap = 2400;
        private const float LakeMergeGapMeters = 6f;
        private const string BankLayerPath = "Assets/Settings/TerrainLayers/Dirt_Scaled.terrainlayer";
        private const string WaterMaterialPath = "Assets/Polytope Studio/Lowpoly_Environments/Sources/Materials/PT_Water_mat.mat";

        [MenuItem("GameDev/Overworld/Generate Water Zones")]
        public static void GenerateWaterZonesMenuItem()
        {
            var mapMagic = Object.FindFirstObjectByType<MapMagicObject>();
            if (mapMagic == null || mapMagic.graph == null)
            {
                return;
            }

            GenerateForTile(mapMagic, new Coord(0, 0));
        }

        public static void GenerateForTile(MapMagicObject mapMagic, Coord tileCoord)
        {
            var total = OverworldGenTiming.Start();
            ClearWaterZones();
            ClearRiverZones();

            var tile = mapMagic.tiles[tileCoord];
            if (tile == null)
            {
                return;
            }

            OverworldGenTiming.Time("water.wait-tile", () => MapMagicTileSync.WaitForTileReady(tile));

            var terrain = tile.main?.terrain;
            var holder = terrain != null ? terrain.GetComponent<DirectMatricesHolder>() : null;
            if (holder == null)
            {
                return;
            }

            float HeightSampler(float x, float z)
            {
                if (terrain != null)
                {
                    return terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;
                }

                var worldHeight = mapMagic.globals.height;
                return holder.ContainsPosition(x, z)
                    ? holder.ValueAtPositionInterpolated("height", x, z) * worldHeight
                    : float.PositiveInfinity;
            }

            var inset = OverworldTileChunks.DefaultMarginMeters;
            var minX = (float)(tileCoord.x * mapMagic.tileSize.x) + inset;
            var minZ = (float)(tileCoord.z * mapMagic.tileSize.z) + inset;
            var sizeX = (float)mapMagic.tileSize.x - 2f * inset;
            var sizeZ = (float)mapMagic.tileSize.z - 2f * inset;

            var lakeRng = new GameDev.World.Seed.WorldSeed(OverworldTerrainMenu.LastSeed).Fork("water.lakes");
            WaterZone[] forced = null;
            OverworldGenTiming.Time("water.lakes-place", () =>
            {
                forced = DistributedLakePlacer.Place(
                    HeightSampler, minX, minZ, sizeX, sizeZ, CellSize, lakeRng,
                    minLakes: DistributedLakePlacer.DefaultMinLakes,
                    targetLakes: DistributedLakePlacer.DefaultTargetLakes);
            });
            var zones = LakeZoneMerger.CullOverlapping(forced, maxOverlapFraction: 0.12f, maxCellsPerLake: 4000);
            zones = LakeZoneMerger.CapCount(zones, maxCount: 3);
            if (zones.Length < DistributedLakePlacer.DefaultMinLakes)
            {
                zones = LakeZoneMerger.EnsureMinDistinct(
                    zones, forced, DistributedLakePlacer.DefaultMinLakes,
                    minCenterSeparationMeters: DistributedLakePlacer.DefaultMinSeparationMeters);
            }

            LogSilhouetteDiagnostics(zones);

            var mountainLocalHeight = MeasureMountainLocalHeight(terrain, HeightSampler, minX, minZ, sizeX, sizeZ);
            var basinDepth = Mathf.Clamp(
                mountainLocalHeight * 0.2f,
                LakeTerrainCarver.MinLargeBasinDepthMeters,
                LakeTerrainCarver.MaxLargeBasinDepthMeters);

            var terrainBaseY = terrain != null ? terrain.transform.position.y : 0f;
            var heightScale = terrain != null
                ? Mathf.Max(terrain.terrainData.size.y, mountainLocalHeight)
                : Mathf.Max(mapMagic.globals.height, mountainLocalHeight);
            var riverSettings = RiverTracerSettings.Default(terrainBaseY, heightScale);
            FlowGrid flowGrid = null;
            RiverPath[] rivers = null;
            OverworldGenTiming.Time("water.flow+rivers", () =>
            {
                flowGrid = FlowNetwork.Compute(HeightSampler, minX, minZ, sizeX, sizeZ, CellSize);
                rivers = RiverTracer.Trace(flowGrid, zones, riverSettings);
            });

            if (terrain != null && zones.Length > 0)
            {
                var lakeBankLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(BankLayerPath);
                OverworldGenTiming.Time("water.lake-carve", () =>
                    LakeTerrainCarver.Carve(terrain, zones, basinDepth, lakeBankLayer));
            }

            System.Func<float, float, float> sampleTerrainY = null;
            if (terrain != null)
            {
                sampleTerrainY = (x, z) =>
                    terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;
            }

            if (terrain != null && rivers != null && rivers.Length > 0)
            {
                var bankLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(BankLayerPath);
                OverworldGenTiming.Time("water.river-carve", () =>
                {
                    Debug.Log($"[River] carve begin count={rivers.Length} bankLayer={(bankLayer != null ? bankLayer.name : "null")}");
                    RiverTerrainCarver.Carve(terrain, rivers, bankLayer);
                    for (var i = 0; i < rivers.Length; i++)
                    {
                        rivers[i].SyncWaterSurfaceAfterCarve(sampleTerrainY, riverSettings.WaterFillFraction, zones);
                    }

                    Debug.Log("[River] carve+sync done — diagnostics");
                    RiverDiagnostics.LogAfterCarveSync(rivers, sampleTerrainY, riverSettings.WaterFillFraction);
                });
            }

            rivers ??= System.Array.Empty<RiverPath>();

            var root = new GameObject(RootObjectName);
            var holderComponent = root.AddComponent<OverworldWaterZones>();
            holderComponent.SetZones(zones);

            var waterMaterial = CreateWaterMaterial();
            Debug.Log(
                $"[Water] material='{waterMaterial.name}' shader='{waterMaterial.shader.name}' " +
                $"(PT colors/normals, no scene-color refraction)");
            var planesCreated = 0;
            Rect? terrainClip = null;
            if (terrain != null)
            {
                var tp = terrain.transform.position;
                var ts = terrain.terrainData.size;
                terrainClip = new Rect(tp.x, tp.z, ts.x, ts.z);
            }

            OverworldGenTiming.Time("water.meshes", () =>
            {
                foreach (var zone in zones)
                {
                    var zoneObject = new GameObject($"WaterZone_{zone.Id}");
                    zoneObject.transform.SetParent(root.transform, worldPositionStays: true);
                    var meshFilter = zoneObject.AddComponent<MeshFilter>();
                    meshFilter.sharedMesh = WaterMeshBuilder.Build(zone, zones, sampleTerrainY, terrainClip);
                    var renderer = zoneObject.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = waterMaterial;
                    planesCreated++;
                }

                var riverRoot = new GameObject(RiverRootObjectName);
                var riverHolder = riverRoot.AddComponent<OverworldRiverZones>();
                riverHolder.SetRivers(rivers);

                foreach (var river in rivers)
                {
                    var riverObject = new GameObject($"River_{river.Id}");
                    riverObject.transform.SetParent(riverRoot.transform, worldPositionStays: true);
                    var meshFilter = riverObject.AddComponent<MeshFilter>();
                    meshFilter.sharedMesh = RiverMeshBuilder.Build(river, uvTileLength: 8f, sampleTerrainY);
                    var renderer = riverObject.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = waterMaterial;
                    var vCount = meshFilter.sharedMesh != null ? meshFilter.sharedMesh.vertexCount : 0;
                    Debug.Log($"[River] {river.Id} mesh assigned verts={vCount} mat={waterMaterial.shader.name}");
                }

                if (rivers.Length > 0)
                {
                    RiverDiagnostics.LogAfterMesh(rivers, sampleTerrainY);
                }
            });

            LogOverworldSummary(
                terrain, zones, rivers,
                mapMagic.globals.height, mountainLocalHeight, basinDepth, planesCreated, forced.Length);
            OverworldGenTiming.Log(
                "water.total",
                OverworldGenTiming.StopMs(total),
                $"lakes={zones.Length} rivers={rivers.Length}");
        }

        private static void LogSilhouetteDiagnostics(WaterZone[] zones)
        {
            if (zones == null || zones.Length == 0)
            {
                Debug.Log("[Water] silhouette: no lakes");
                return;
            }

            for (var i = 0; i < zones.Length; i++)
            {
                var z = zones[i];
                if (z.Silhouette == null)
                {
                    Debug.LogWarning(
                        $"[Water] {z.Id}: Silhouette=NULL — mesh/carve will use pixel flood mask (stairs)");
                    continue;
                }

                var s = z.Silhouette;
                var r0 = s.ShoreRadiusAtAngle(0f);
                var r1 = s.ShoreRadiusAtAngle(Mathf.PI * 0.5f);
                var r2 = s.ShoreRadiusAtAngle(Mathf.PI);
                var jump = Mathf.Max(Mathf.Abs(r1 - r0), Mathf.Abs(r2 - r1));
                Debug.Log(
                    $"[Water] {z.Id}: Silhouette OK center=({s.CenterX:F0},{s.CenterZ:F0}) " +
                    $"R={s.RadiusMeters:F1}m aspect={s.Aspect:F2} bay={s.HasBay} " +
                    $"curveR(0/90/180)=({r0:F1},{r1:F1},{r2:F1}) maxStep≈{jump:F1}m");
            }
        }

        private static float MeasureMountainLocalHeight(
            UnityEngine.Terrain terrain,
            System.Func<float, float, float> heightSampler,
            float minX, float minZ, float sizeX, float sizeZ)
        {
            var peakWorld = 0f;
            for (var i = 0; i < 48; i++)
            {
                for (var j = 0; j < 48; j++)
                {
                    var x = minX + sizeX * (i + 0.5f) / 48f;
                    var z = minZ + sizeZ * (j + 0.5f) / 48f;
                    var y = heightSampler(x, z);
                    if (!float.IsInfinity(y) && y > peakWorld)
                    {
                        peakWorld = y;
                    }
                }
            }

            var baseY = terrain != null ? terrain.transform.position.y : 0f;
            return Mathf.Max(peakWorld - baseY, 1f);
        }

        private static void LogOverworldSummary(
            UnityEngine.Terrain terrain,
            WaterZone[] zones,
            RiverPath[] rivers,
            float globalsHeight,
            float mountainLocalHeight,
            float basinDepth,
            int waterPlaneCount,
            int rawZoneCount)
        {
            Debug.Log(
                $"[Overworld] 1) Mountain height: {mountainLocalHeight:F1}m above terrain base " +
                $"(globals.height {globalsHeight:F0}m, lake dig target {basinDepth:F1}m " +
                $"(clamp {LakeTerrainCarver.MinLargeBasinDepthMeters:F0}–{LakeTerrainCarver.MaxLargeBasinDepthMeters:F0}m), " +
                $"headroomShift={LakeTerrainCarver.LastHeadroomShiftMeters:F1}m)");

            var digs = LakeTerrainCarver.LastPerLakeMaxDigMeters;
            var targets = LakeTerrainCarver.LastPerLakeTargetDepthMeters;
            if (zones.Length == 0)
            {
                Debug.Log("[Overworld] 2) Lake basin depths: none (no lakes)");
            }
            else
            {
                var parts = new System.Text.StringBuilder();
                for (var i = 0; i < zones.Length; i++)
                {
                    var dig = i < digs.Count ? digs[i] : 0f;
                    var target = i < targets.Count ? targets[i] : 0f;
                    var c = zones[i].GetFloodCentroidXZ();
                    var aabb = zones[i].GetFloodAabbXZ();
                    MeasureLakeBed(terrain, zones[i], out var bedMin, out var bedMedian, out var wetFrac);
                    var column = zones[i].WaterLevelY - bedMin;
                    if (i > 0)
                    {
                        parts.Append("; ");
                    }

                    parts.Append(
                        $"{zones[i].Id} sil={(zones[i].Silhouette != null ? "yes" : "NO")} " +
                        $"center=({c.x:F0},{c.z:F0}) " +
                        $"r≈{zones[i].EquivalentRadiusMeters:F0}m " +
                        $"aabb=({aabb.xMin:F0},{aabb.yMin:F0})-({aabb.xMax:F0},{aabb.yMax:F0}) " +
                        $"targetDig={target:F1}m maxDig={dig:F1}m " +
                        $"waterY={zones[i].WaterLevelY:F1}m bedMin={bedMin:F1}m bedMed={bedMedian:F1}m " +
                        $"waterColumn={column:F1}m wetFrac={wetFrac:P0}");
                }

                Debug.Log(
                    $"[Overworld] 2) Lake basin depths: max={LakeTerrainCarver.LastMaxDigMeters:F1}m " +
                    $"(raw floods {rawZoneCount} → culled {zones.Length}) | {parts}");
            }

            Debug.Log($"[Overworld] 3) Lakes: {zones.Length}, water planes: {waterPlaneCount}");
            Debug.Log($"[Overworld] 4) Rivers: see [River] diagnostics above (count={rivers.Length})");
        }

        private static void MeasureLakeBed(
            UnityEngine.Terrain terrain,
            WaterZone zone,
            out float bedMin,
            out float bedMedian,
            out float wetFraction)
        {
            bedMin = float.PositiveInfinity;
            bedMedian = zone.WaterLevelY;
            wetFraction = 0f;
            if (terrain == null)
            {
                bedMin = zone.WaterLevelY;
                return;
            }

            var samples = new List<float>(256);
            var wet = 0;
            var total = 0;
            var step = Mathf.Max(1, Mathf.Min(zone.Width, zone.Depth) / 24);
            for (var x = 0; x < zone.Width; x += step)
            {
                for (var z = 0; z < zone.Depth; z += step)
                {
                    if (!zone.IsFlooded(x, z))
                    {
                        continue;
                    }

                    var wx = zone.OriginX + (x + 0.5f) * zone.CellSize;
                    var wz = zone.OriginZ + (z + 0.5f) * zone.CellSize;
                    var y = terrain.SampleHeight(new Vector3(wx, 0f, wz)) + terrain.transform.position.y;
                    samples.Add(y);
                    total++;
                    if (y < zone.WaterLevelY - 0.15f)
                    {
                        wet++;
                    }

                    if (y < bedMin)
                    {
                        bedMin = y;
                    }
                }
            }

            if (samples.Count == 0)
            {
                bedMin = zone.WaterLevelY;
                return;
            }

            samples.Sort();
            bedMedian = samples[samples.Count / 2];
            wetFraction = total > 0 ? wet / (float)total : 0f;
        }

        public static void ClearWaterZones()
        {
            var root = GameObject.Find(RootObjectName);
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        public static void ClearRiverZones()
        {
            var root = GameObject.Find(RiverRootObjectName);
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Same look as <c>PT_Water_mat</c> (Deep/Shallow, foam, scrolling normals, opacity)
        /// via <c>GameDev/Overworld/LakeWater</c> — no <c>_CameraOpaqueTexture</c> refraction
        /// (that is why the stock Polytope shader goes black in Scene/Game).
        /// </summary>
        private static Material CreateWaterMaterial()
        {
            var preset = AssetDatabase.LoadAssetAtPath<Material>(WaterMaterialPath);
            return CreatePolytopeLikeLakeWater(preset);
        }

        private static Material CreatePolytopeLikeLakeWater(Material colorSource)
        {
            var shader = Shader.Find("GameDev/Overworld/LakeWater");
            if (shader == null)
            {
                Debug.LogWarning("[Water] GameDev/Overworld/LakeWater missing — falling back to URP Lit tint.");
                return CreateTintedTransparentWaterFallback(colorSource);
            }

            var mat = new Material(shader) { name = "OverworldWater (PT-like, no-refraction)" };

            var deep = colorSource != null && colorSource.HasProperty("_DeepColor")
                ? colorSource.GetColor("_DeepColor")
                : new Color(0.214f, 0.611f, 0.623f, 1f);
            var shallow = colorSource != null && colorSource.HasProperty("_ShallowColor")
                ? colorSource.GetColor("_ShallowColor")
                : new Color(0.220f, 0.465f, 0.537f, 1f);
            var foam = colorSource != null && colorSource.HasProperty("_FoamColor")
                ? colorSource.GetColor("_FoamColor")
                : new Color(0.30f, 1f, 0.91f, 1f);

            // Dense, slightly olive-teal body — less glassy than stock PT preview.
            deep = Color.Lerp(deep, new Color(0.05f, 0.16f, 0.14f, 1f), 0.48f);
            shallow = Color.Lerp(shallow, new Color(0.09f, 0.24f, 0.22f, 1f), 0.40f);
            // Tiny green push on top of the teal (very subtle).
            deep = new Color(deep.r * 0.92f, Mathf.Min(deep.g * 1.08f, 0.38f), deep.b * 0.95f, 1f);
            shallow = new Color(shallow.r * 0.94f, Mathf.Min(shallow.g * 1.06f, 0.42f), shallow.b * 0.96f, 1f);
            deep.a = 1f;
            shallow.a = 0.98f;
            mat.SetColor("_DeepColor", deep);
            mat.SetColor("_ShallowColor", shallow);
            mat.SetColor("_FoamColor", foam);

            // Prefer a denser body of water; PT _TRANSPARENCY alone reads too clear in Scene.
            var opacity = 0.99f;
            if (colorSource != null && colorSource.HasProperty("_TRANSPARENCY"))
            {
                opacity = Mathf.Clamp(Mathf.Max(colorSource.GetFloat("_TRANSPARENCY"), 0.98f), 0.98f, 1f);
            }

            mat.SetFloat("_Opacity", opacity);
            if (mat.HasProperty("_DepthFade"))
            {
                mat.SetFloat("_DepthFade", 3f);
            }

            if (colorSource != null && colorSource.HasProperty("_Smootness"))
            {
                mat.SetFloat("_Smoothness", Mathf.Clamp01(colorSource.GetFloat("_Smootness")));
            }
            else if (colorSource != null && colorSource.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", Mathf.Clamp01(colorSource.GetFloat("_Smoothness")));
            }
            else
            {
                mat.SetFloat("_Smoothness", 0.84f);
            }

            if (colorSource != null && colorSource.HasProperty("_NormalMapWavesSpeed"))
            {
                mat.SetFloat("_NormalSpeed", colorSource.GetFloat("_NormalMapWavesSpeed"));
            }

            if (colorSource != null && colorSource.HasProperty("_NormalMapsWavesSize"))
            {
                mat.SetFloat("_NormalTiling", colorSource.GetFloat("_NormalMapsWavesSize"));
            }

            if (colorSource != null && colorSource.HasProperty("_FoamAmount"))
            {
                mat.SetFloat("_ShoreFoam", colorSource.GetFloat("_FoamAmount"));
            }

            Texture nrm = null;
            if (colorSource != null && colorSource.HasProperty("_NormalMapTexture"))
            {
                nrm = colorSource.GetTexture("_NormalMapTexture");
            }

            if (nrm == null && colorSource != null && colorSource.HasProperty("_normalmap"))
            {
                nrm = colorSource.GetTexture("_normalmap");
            }

            if (nrm != null)
            {
                mat.SetTexture("_NormalMap", nrm);
            }

            mat.renderQueue = (int)RenderQueue.Transparent;
            return mat;
        }

        private static Material CreateTintedTransparentWaterFallback(Material colorSource)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = "OverworldWater (Lit fallback)" };
            var deep = colorSource != null && colorSource.HasProperty("_DeepColor")
                ? colorSource.GetColor("_DeepColor")
                : new Color(0.214f, 0.611f, 0.623f, 1f);
            deep.a = 0.85f;
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", deep);
            }
            else
            {
                mat.color = deep;
            }

            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
            }

            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            return mat;
        }
    }
}
#endif

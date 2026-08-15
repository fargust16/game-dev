using System.Collections.Generic;
using GameDev.Player;
using GameDev.World;
using GameDev.World.Roads;
using GameDev.World.Settlement;
using GameDev.World.Streaming;
using GameDev.World.Terrain;
using GameDev.World.Water;
using UnityEngine;
using UnityEngine.Rendering;
using Random = System.Random;

namespace GameDev.World.Vegetation
{
    /// <summary>
    /// Scene-resident GPU grass coverage. Mesh/material/bake params are serialized so Play mode
    /// keeps working after domain reload; matrices are lazy-baked per resident 64 m chunk.
    /// </summary>
    [ExecuteAlways]
    public sealed class GrassField : MonoBehaviour
    {
        public const string RootObjectName = "OverworldGrass";

        public const float DrawDistanceMeters = 50f;
        public const float CullDistanceMeters = 64f;
        public const float FullDensityDistanceMeters = 40f;
        public const int MaxVisibleInstances = 120000;

        public const float PlayerBendRadius = 1.4f;
        public const float PlayerBendStrength = 0.55f;

        private static readonly int WindDirId = Shader.PropertyToID("_GrassWindDir");
        private static readonly int WindStrengthId = Shader.PropertyToID("_GrassWindStrength");
        private static readonly int WindSpeedId = Shader.PropertyToID("_GrassWindSpeed");
        private static readonly int PlayerPosId = Shader.PropertyToID("_GrassPlayerPos");
        private static readonly int PlayerRadiusId = Shader.PropertyToID("_GrassPlayerRadius");
        private static readonly int PlayerStrengthId = Shader.PropertyToID("_GrassPlayerStrength");

        [SerializeField] private Mesh grassMesh;
        [SerializeField] private Material grassMaterial;
        [SerializeField] private int worldSeed;
        [SerializeField] private float tileMinX;
        [SerializeField] private float tileMinZ;
        [SerializeField] private float tileSizeX = 1400f;
        [SerializeField] private float tileSizeZ = 1400f;
        [SerializeField] private float offsetMacroX;
        [SerializeField] private float offsetMacroZ;
        [SerializeField] private float offsetMesoX;
        [SerializeField] private float offsetMesoZ;
        [SerializeField] private float offsetThicketX;
        [SerializeField] private float offsetThicketZ;
        [SerializeField] private float offsetGrassX;
        [SerializeField] private float offsetGrassZ;
        [SerializeField] private bool bakeConfigured;

        [SerializeField] private Vector2 windDir = new Vector2(1f, 0.35f);
        [SerializeField] private float windStrength = 0.22f;
        [SerializeField] private float windSpeed = 1.1f;

        private readonly Dictionary<long, GrassChunk> chunkCache = new Dictionary<long, GrassChunk>(64);
        private readonly Queue<long> bakeQueue = new Queue<long>(32);
        private readonly HashSet<long> queuedKeys = new HashSet<long>();
        private readonly Matrix4x4[] drawBatch = new Matrix4x4[GrassRenderer.BatchSize];
        private MaterialPropertyBlock drawProps;
        private Transform player;
        private bool drawing;
        private int cachedInstanceTotal;
        private int bakedChunkCount;
        private Vector3 lastFocus;
        private Vector3 focusVelocity;

        /// <summary>Max grass chunks baked per Update (avoids 0.5s hitch on chunk entry).</summary>
        public const int MaxBakesPerFrame = 1;

        /// <summary>Warm-ahead distance along movement (metres).</summary>
        public const float WarmAheadMeters = 48f;

        // Scene services resolved lazily (not serialized).
        private VistaTerrainSampler sampler;
        private UnityEngine.Terrain terrain;
        private VegetationClearance clearance;
        private IWaterSampler water;
        private GrassExclusion settlement;
        private bool servicesReady;
        private List<Vector2> treeTrunks = new List<Vector2>(512);

        public int ChunkCount => bakedChunkCount;
        public int TotalInstances => cachedInstanceTotal;
        public Mesh Mesh => grassMesh;
        public Material Material => grassMaterial;
        public bool IsConfigured => bakeConfigured && grassMesh != null && grassMaterial != null;

        public static GrassField Find()
        {
            var go = GameObject.Find(RootObjectName);
            return go != null ? go.GetComponent<GrassField>() : null;
        }

        public static GrassField FindOrCreate()
        {
            var existing = Find();
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(RootObjectName);
            return go.AddComponent<GrassField>();
        }

        public static void ClearInScene()
        {
            var field = Find();
            if (field == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(field.gameObject);
            }
            else
            {
                DestroyImmediate(field.gameObject);
            }
        }

        /// <summary>
        /// Persist bake inputs (survives Play mode). Chunks are baked lazily near the camera.
        /// </summary>
        public void ConfigureBake(
            Mesh mesh,
            Material material,
            int seed,
            float minX,
            float minZ,
            float sizeX,
            float sizeZ,
            VegetationEcology.NoiseOffsets offsets)
        {
            grassMesh = mesh;
            grassMaterial = material;
            if (grassMaterial != null)
            {
                grassMaterial.enableInstancing = true;
            }

            worldSeed = seed;
            tileMinX = minX;
            tileMinZ = minZ;
            tileSizeX = sizeX;
            tileSizeZ = sizeZ;
            offsetMacroX = offsets.MacroX;
            offsetMacroZ = offsets.MacroZ;
            offsetMesoX = offsets.MesoX;
            offsetMesoZ = offsets.MesoZ;
            offsetThicketX = offsets.ThicketX;
            offsetThicketZ = offsets.ThicketZ;
            offsetGrassX = offsets.GrassX;
            offsetGrassZ = offsets.GrassZ;
            bakeConfigured = true;

            chunkCache.Clear();
            bakeQueue.Clear();
            queuedKeys.Clear();
            cachedInstanceTotal = 0;
            bakedChunkCount = 0;
            servicesReady = false;
            treeTrunks.Clear();
            drawProps = new MaterialPropertyBlock();
        }

        public void SetPlayer(Transform playerTransform)
        {
            player = playerTransform;
        }

        /// <summary>Warm-bake chunks around a world focus so Scene view shows grass immediately.</summary>
        public void WarmBakeAround(Vector3 focus, float radiusMeters)
        {
            if (!IsConfigured)
            {
                return;
            }

            EnsureServices();
            if (!servicesReady)
            {
                return;
            }

            var r = Mathf.Max(radiusMeters, ChunkCoord.ChunkSize);
            var minCx = Mathf.FloorToInt((focus.x - r) / ChunkCoord.ChunkSize);
            var maxCx = Mathf.FloorToInt((focus.x + r) / ChunkCoord.ChunkSize);
            var minCz = Mathf.FloorToInt((focus.z - r) / ChunkCoord.ChunkSize);
            var maxCz = Mathf.FloorToInt((focus.z + r) / ChunkCoord.ChunkSize);
            for (var cx = minCx; cx <= maxCx; cx++)
            {
                for (var cz = minCz; cz <= maxCz; cz++)
                {
                    BakeChunkNow(cx, cz);
                }
            }
        }

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        private void Update()
        {
            if (!IsConfigured || !servicesReady)
            {
                return;
            }

            ProcessBakeQueue();
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (drawing || cam == null || !IsConfigured)
            {
                return;
            }

            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection)
            {
                return;
            }

            drawing = true;
            try
            {
                EnsureServices();
                if (!servicesReady)
                {
                    return;
                }

                var focus = cam.transform.position;
                var delta = focus - lastFocus;
                if (delta.sqrMagnitude > 1e-6f && Time.deltaTime > 1e-4f)
                {
                    focusVelocity = delta / Time.deltaTime;
                }

                lastFocus = focus;
                PushShaderGlobals();
                EnqueueResidentChunks(focus);
                DrawResidentChunks(focus);
            }
            finally
            {
                drawing = false;
            }
        }

        private void EnsureServices()
        {
            if (servicesReady && terrain != null && sampler != null)
            {
                return;
            }

            terrain = FindTerrain();
            if (terrain == null)
            {
                servicesReady = false;
                return;
            }

            sampler = new VistaTerrainSampler(terrain, worldSeed);
            water = SeaLevelOceanSampler.FromTerrain(terrain);
            clearance = BuildClearanceFromScene();
            var anchor = FindAnyObjectByType<OverworldSettlementAnchor>();
            settlement = anchor != null && anchor.Fabric != null
                ? GrassExclusion.FromFabric(anchor.Fabric)
                : GrassExclusion.None;
            CollectTreeTrunks();

            // Prefer existing CPU lawn bake; only rebuild when missing (avoid hitch on first Play frame).
            if (!RoadLawnMask.HasCpuBake)
            {
                RoadLawnMask.BakeFromScene();
            }
            else
            {
                RoadLawnMask.BindEdgeParams();
            }

            GrassTintMask.BakeFromScene();
            servicesReady = true;
            // Rebuild clearance after lawn CPU bake so Multiplier uses O(1) sample.
            clearance = BuildClearanceFromScene();
        }

        private void CollectTreeTrunks()
        {
            treeTrunks.Clear();
            var veg = GameObject.Find(VegetationScatterMenuRootName);
            if (veg == null)
            {
                return;
            }

            var trees = veg.transform.Find("Trees");
            if (trees == null)
            {
                return;
            }

            for (var i = 0; i < trees.childCount; i++)
            {
                var p = trees.GetChild(i).position;
                treeTrunks.Add(new Vector2(p.x, p.z));
            }
        }

        private const string VegetationScatterMenuRootName = "OverworldVegetation";

        private static UnityEngine.Terrain FindTerrain()
        {
            return OverworldTerrainAccess.FindTerrain();
        }

        private void PushShaderGlobals()
        {
            var dir = windDir.sqrMagnitude > 0.0001f ? windDir.normalized : Vector2.right;
            Shader.SetGlobalVector(WindDirId, new Vector4(dir.x, dir.y, 0f, 0f));
            Shader.SetGlobalFloat(WindStrengthId, windStrength);
            Shader.SetGlobalFloat(WindSpeedId, windSpeed);

            var pos = ResolvePlayerPosition();
            Shader.SetGlobalVector(PlayerPosId, new Vector4(pos.x, pos.y, pos.z, 0f));
            Shader.SetGlobalFloat(PlayerRadiusId, PlayerBendRadius);
            Shader.SetGlobalFloat(PlayerStrengthId, PlayerBendStrength);

            RoadLawnMask.BindEdgeParams();
        }

        private Vector3 ResolvePlayerPosition()
        {
            if (player != null)
            {
                return player.position;
            }

            var controller = FindAnyObjectByType<OverworldPlayerController>();
            if (controller != null)
            {
                player = controller.transform;
                return player.position;
            }

            var cam = Camera.main;
            return cam != null ? cam.transform.position : Vector3.zero;
        }

        private void EnqueueResidentChunks(Vector3 focus)
        {
            var ahead = focusVelocity;
            ahead.y = 0f;
            if (ahead.sqrMagnitude > 1f)
            {
                ahead = ahead.normalized * WarmAheadMeters;
            }
            else
            {
                ahead = Vector3.zero;
            }

            EnqueueAround(focus, CullDistanceMeters);
            if (ahead.sqrMagnitude > 1f)
            {
                EnqueueAround(focus + ahead, ChunkCoord.ChunkSize * 1.5f);
            }
        }

        private void EnqueueAround(Vector3 focus, float radiusMeters)
        {
            var r = Mathf.Max(radiusMeters, ChunkCoord.ChunkSize * 0.5f);
            var minCx = Mathf.FloorToInt((focus.x - r) / ChunkCoord.ChunkSize);
            var maxCx = Mathf.FloorToInt((focus.x + r) / ChunkCoord.ChunkSize);
            var minCz = Mathf.FloorToInt((focus.z - r) / ChunkCoord.ChunkSize);
            var maxCz = Mathf.FloorToInt((focus.z + r) / ChunkCoord.ChunkSize);
            for (var cx = minCx; cx <= maxCx; cx++)
            {
                for (var cz = minCz; cz <= maxCz; cz++)
                {
                    if (!ChunkOverlapsTile(cx, cz))
                    {
                        continue;
                    }

                    var key = PackKey(cx, cz);
                    if (chunkCache.ContainsKey(key) || queuedKeys.Contains(key))
                    {
                        continue;
                    }

                    bakeQueue.Enqueue(key);
                    queuedKeys.Add(key);
                }
            }
        }

        private void ProcessBakeQueue()
        {
            var n = 0;
            while (n < MaxBakesPerFrame && bakeQueue.Count > 0)
            {
                var key = bakeQueue.Dequeue();
                queuedKeys.Remove(key);
                UnpackKey(key, out var cx, out var cz);
                BakeChunkNow(cx, cz);
                n++;
            }
        }

        private void DrawResidentChunks(Vector3 focus)
        {
            if (drawProps == null)
            {
                drawProps = new MaterialPropertyBlock();
            }

            var drawn = 0;
            var cullSq = CullDistanceMeters * CullDistanceMeters;
            var fullSq = FullDensityDistanceMeters * FullDensityDistanceMeters;
            var r = CullDistanceMeters;
            var minCx = Mathf.FloorToInt((focus.x - r) / ChunkCoord.ChunkSize);
            var maxCx = Mathf.FloorToInt((focus.x + r) / ChunkCoord.ChunkSize);
            var minCz = Mathf.FloorToInt((focus.z - r) / ChunkCoord.ChunkSize);
            var maxCz = Mathf.FloorToInt((focus.z + r) / ChunkCoord.ChunkSize);

            for (var cx = minCx; cx <= maxCx; cx++)
            {
                for (var cz = minCz; cz <= maxCz; cz++)
                {
                    if (!ChunkOverlapsTile(cx, cz))
                    {
                        continue;
                    }

                    var center = new Vector3(
                        (cx + 0.5f) * ChunkCoord.ChunkSize,
                        focus.y,
                        (cz + 0.5f) * ChunkCoord.ChunkSize);
                    var distSq = (center - focus).sqrMagnitude;
                    if (distSq > cullSq)
                    {
                        continue;
                    }

                    var dist = Mathf.Sqrt(distSq);
                    if (dist > DrawDistanceMeters)
                    {
                        continue;
                    }

                    var key = PackKey(cx, cz);
                    if (!chunkCache.TryGetValue(key, out var chunk) || chunk == null || chunk.Count == 0)
                    {
                        continue;
                    }

                    FlushChunk(chunk, distSq > fullSq, ref drawn);
                    if (drawn >= MaxVisibleInstances)
                    {
                        return;
                    }
                }
            }
        }

        private bool ChunkOverlapsTile(int cx, int cz)
        {
            var ox = cx * ChunkCoord.ChunkSize;
            var oz = cz * ChunkCoord.ChunkSize;
            var ex = ox + ChunkCoord.ChunkSize;
            var ez = oz + ChunkCoord.ChunkSize;
            return ex > tileMinX && ox < tileMinX + tileSizeX
                   && ez > tileMinZ && oz < tileMinZ + tileSizeZ;
        }

        private void BakeChunkNow(int cx, int cz)
        {
            var key = PackKey(cx, cz);
            if (chunkCache.ContainsKey(key))
            {
                return;
            }

            var offsets = new VegetationEcology.NoiseOffsets(
                offsetMacroX, offsetMacroZ,
                offsetMesoX, offsetMesoZ,
                offsetThicketX, offsetThicketZ,
                offsetGrassX, offsetGrassZ);
            var salt = unchecked(worldSeed * 397 ^ (cx * 73856093) ^ (cz * 19349663));
            if (salt == int.MinValue)
            {
                salt = 1;
            }

            var rng = new Random(salt);
            var chunk = GrassDistribution.BakeChunk(
                cx, cz, rng, sampler, terrain, offsets,
                clearance, water, settlement,
                GrassDistribution.ShortHeightMeters,
                GrassDistribution.TallHeightMeters,
                treeTrunks);
            chunkCache[key] = chunk;
            bakedChunkCount++;
            cachedInstanceTotal += chunk.Count;
        }

        private static void UnpackKey(long key, out int cx, out int cz)
        {
            cx = (int)(key >> 32);
            cz = (int)(key & 0xFFFFFFFFL);
        }

        private void FlushChunk(GrassChunk chunk, bool farLod, ref int drawn)
        {
            var matrices = chunk.Matrices;
            var written = 0;

            for (var i = 0; i < chunk.Count; i++)
            {
                if (farLod && (i % 3) == 2)
                {
                    continue;
                }

                if (drawn >= MaxVisibleInstances)
                {
                    break;
                }

                drawBatch[written++] = matrices[i];
                drawn++;
                if (written >= GrassRenderer.BatchSize)
                {
                    Graphics.DrawMeshInstanced(
                        grassMesh, 0, grassMaterial, drawBatch, written, drawProps,
                        ShadowCastingMode.Off, false, gameObject.layer);
                    written = 0;
                }
            }

            if (written > 0)
            {
                Graphics.DrawMeshInstanced(
                    grassMesh, 0, grassMaterial, drawBatch, written, drawProps,
                    ShadowCastingMode.Off, false, gameObject.layer);
            }
        }

        private static long PackKey(int cx, int cz) => ((long)cx << 32) ^ (uint)cz;

        private static VegetationClearance BuildClearanceFromScene()
        {
            var roadClearRadius = GrassMask.RoadClearCoreMeters;
            var mPerPx = WorldScale.OverworldTileMeters / 512f;
            var roadFalloff = RoadSurfaceStyle.SoftRimMeters(mPerPx);
            const float settlementClearRadius = 28f;
            const float settlementFalloff = 22f;
            const float poiClearRadius = 14f;
            const float poiFalloff = 8f;
            const float campClearRadius = 18f;
            const float campFalloff = 10f;
            const float townApproachClearRadius = 24f;
            const float townApproachFalloff = 14f;

            var disks = new List<VegetationClearance.Disk>();
            var settlementAnchor = FindAnyObjectByType<OverworldSettlementAnchor>();
            if (settlementAnchor != null && settlementAnchor.Fabric == null)
            {
                var p = settlementAnchor.Position;
                disks.Add(new VegetationClearance.Disk(
                    new Vector2(p.x, p.z), settlementClearRadius, settlementFalloff));
            }

            var poiRoot = GameObject.Find("OverworldPoi");
            if (poiRoot != null)
            {
                foreach (Transform child in poiRoot.transform)
                {
                    var pos = child.position;
                    var name = child.name;
                    float clearR;
                    float falloff;
                    if (name.IndexOf("TownApproach", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        clearR = townApproachClearRadius;
                        falloff = townApproachFalloff;
                    }
                    else if (name.IndexOf("Camp", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        clearR = campClearRadius;
                        falloff = campFalloff;
                    }
                    else
                    {
                        clearR = poiClearRadius;
                        falloff = poiFalloff;
                    }

                    disks.Add(new VegetationClearance.Disk(new Vector2(pos.x, pos.z), clearR, falloff));
                }
            }

            var roadSamples = new List<Vector2>(512);
            var roadClearance = FindAnyObjectByType<OverworldRoadClearance>();
            if (roadClearance != null && roadClearance.Samples != null)
            {
                for (var i = 0; i < roadClearance.Samples.Count; i++)
                {
                    roadSamples.Add(roadClearance.Samples[i]);
                }
            }

            return new VegetationClearance(
                disks, roadSamples, roadClearRadius, roadFalloff,
                roadMinAtCore: GrassMask.RoadLawnMinAtCenter);
        }
    }
}

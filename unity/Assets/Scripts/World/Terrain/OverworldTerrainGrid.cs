using UnityEngine;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// Builds the overworld Unity Terrain grid: tiles, sizes, resolutions, layers, colliders and
    /// neighbour links. Plain <c>UnityEngine.Terrain</c> and <c>TerrainData</c>, no third party.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is a container builder, not a terrain generator.</b> It never writes a height, never
    /// paints a weight beyond a flat base layer, and never places a tree. A fresh
    /// <c>TerrainData</c> is already zero everywhere, and that is exactly what this leaves behind:
    /// a correctly configured, empty tile grid that downstream systems can query.
    /// </para>
    /// <para>
    /// It exists so that "who owns the Unity Terrain" and "who fills it with relief" become two
    /// separate questions. Vista currently answers both; with this, it only answers the second, and
    /// a future generator can answer the second differently without touching any of this.
    /// </para>
    /// </remarks>
    public static class OverworldTerrainGrid
    {
        /// <summary>Heightmap resolution the overworld tiles are pinned to (2^n + 1).</summary>
        public const int DefaultHeightmapResolution = 513;

        /// <summary>Alphamap (splat) resolution - matches <c>VistaManager.textureResolution</c>.</summary>
        public const int DefaultAlphamapResolution = 512;

        /// <summary>Detail map resolution - matches <see cref="TerrainHeightWrite"/>.</summary>
        public const int DefaultDetailResolution = 512;

        /// <summary>Detail patch size - matches <see cref="TerrainHeightWrite"/>.</summary>
        public const int DefaultDetailResolutionPerPatch = 16;

        /// <summary>
        /// Everything the grid needs in order to exist, and nothing about what it contains.
        /// </summary>
        public readonly struct Spec
        {
            public readonly int TileCountX;
            public readonly int TileCountZ;
            public readonly float TileMeters;
            public readonly float HeightBudgetMeters;
            public readonly int HeightmapResolution;
            public readonly int AlphamapResolution;

            public Spec(
                int tileCountX,
                int tileCountZ,
                float tileMeters,
                float heightBudgetMeters,
                int heightmapResolution = DefaultHeightmapResolution,
                int alphamapResolution = DefaultAlphamapResolution)
            {
                TileCountX = Mathf.Max(1, tileCountX);
                TileCountZ = Mathf.Max(1, tileCountZ);
                TileMeters = Mathf.Max(1f, tileMeters);
                HeightBudgetMeters = Mathf.Max(1f, heightBudgetMeters);
                HeightmapResolution = Mathf.Max(33, heightmapResolution);
                AlphamapResolution = Mathf.Max(16, alphamapResolution);
            }

            /// <summary>The overworld as <see cref="WorldScale"/> describes it.</summary>
            public static Spec Default => new Spec(
                WorldScale.OverworldTileCountX,
                WorldScale.OverworldTileCountZ,
                WorldScale.OverworldTileMeters,
                WorldScale.TerrainHeightBudgetMeters);

            public Vector3 TileSize => new Vector3(TileMeters, HeightBudgetMeters, TileMeters);
        }

        /// <summary>Tile object name, shared with the Vista bake so diagnostics read the same.</summary>
        public static string TileName(int x, int z) => $"OverworldTerrain_{x}_{z}";

        /// <summary>
        /// Creates the tile grid under <paramref name="parent"/>, returned in row-major
        /// <c>[x + z * TileCountX]</c> order.
        /// </summary>
        /// <param name="layers">
        /// Existing <c>TerrainLayer</c> assets to bind. Optional; when given, the alphamap is set to
        /// a flat weight of 1 on layer 0 so the tile renders one surface. That is a base state, not
        /// painting - a weight distribution that means anything needs a terrain/biome generator.
        /// </param>
        /// <param name="material">Terrain material template (URP TerrainLit). Optional.</param>
        public static UnityEngine.Terrain[] Create(
            Transform parent,
            Spec spec,
            TerrainLayer[] layers = null,
            Material material = null)
        {
            var terrains = new UnityEngine.Terrain[spec.TileCountX * spec.TileCountZ];

            for (var z = 0; z < spec.TileCountZ; z++)
            {
                for (var x = 0; x < spec.TileCountX; x++)
                {
                    var data = CreateTerrainData(spec, layers);
                    var go = UnityEngine.Terrain.CreateTerrainGameObject(data);
                    go.name = TileName(x, z);
                    if (parent != null)
                    {
                        go.transform.SetParent(parent, false);
                    }

                    // Unity Terrain pivots at its lower-left corner, so the grid grows along +X/+Z
                    // from the parent - the same layout the Vista-created grid uses.
                    go.transform.localPosition =
                        new Vector3(x * spec.TileMeters, 0f, z * spec.TileMeters);

                    var terrain = go.GetComponent<UnityEngine.Terrain>();
                    terrain.drawInstanced = true;
                    terrain.allowAutoConnect = true;
                    terrain.groupingID = 0;
                    if (material != null)
                    {
                        terrain.materialTemplate = material;
                    }

                    OverworldTerrainTile.Stamp(go, x, z);
                    terrains[x + z * spec.TileCountX] = terrain;
                }
            }

            ConnectNeighbours(terrains, spec);
            return terrains;
        }

        /// <summary>
        /// A configured but empty <c>TerrainData</c>: correct metres, resolutions and layer stack,
        /// every height zero.
        /// </summary>
        public static TerrainData CreateTerrainData(Spec spec, TerrainLayer[] layers = null)
        {
            var data = new TerrainData();

            // Resolution first, then metres. Unity preserves metres-per-pixel across a resolution
            // change, so setting size first and resolution second explodes the tile
            // (1400 m at 33 -> 22400 m at 513). Same ordering as TerrainHeightWrite.
            data.heightmapResolution = spec.HeightmapResolution;
            data.size = spec.TileSize;
            data.alphamapResolution = spec.AlphamapResolution;
            data.SetDetailResolution(DefaultDetailResolution, DefaultDetailResolutionPerPatch);
            data.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);

            if (layers != null && layers.Length > 0)
            {
                data.terrainLayers = layers;
                SetFlatBaseWeights(data);
            }

            return data;
        }

        /// <summary>
        /// Puts the whole alphamap on layer 0.
        /// </summary>
        /// <remarks>
        /// The minimum a tile needs in order to render one surface instead of an undefined blend. It
        /// is not a material pass: a weight distribution that means anything (grass vs sand vs
        /// cliff) is a function of relief and biome, and there is no relief and no biome generator
        /// on this path yet.
        /// </remarks>
        public static void SetFlatBaseWeights(TerrainData data)
        {
            if (data == null || data.terrainLayers == null || data.terrainLayers.Length == 0)
            {
                return;
            }

            var res = data.alphamapResolution;
            var maps = new float[res, res, data.terrainLayers.Length];
            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    maps[z, x, 0] = 1f;
                }
            }

            data.SetAlphamaps(0, 0, maps);
        }

        /// <summary>Links each tile to its four edge neighbours so LOD seams match.</summary>
        public static void ConnectNeighbours(UnityEngine.Terrain[] terrains, Spec spec)
        {
            if (terrains == null)
            {
                return;
            }

            UnityEngine.Terrain.SetConnectivityDirty();
            for (var z = 0; z < spec.TileCountZ; z++)
            {
                for (var x = 0; x < spec.TileCountX; x++)
                {
                    var terrain = At(terrains, spec, x, z);
                    if (terrain == null)
                    {
                        continue;
                    }

                    terrain.SetNeighbors(
                        At(terrains, spec, x - 1, z),
                        At(terrains, spec, x, z + 1),
                        At(terrains, spec, x + 1, z),
                        At(terrains, spec, x, z - 1));
                }
            }
        }

        private static UnityEngine.Terrain At(
            UnityEngine.Terrain[] terrains, Spec spec, int x, int z)
        {
            if (x < 0 || z < 0 || x >= spec.TileCountX || z >= spec.TileCountZ)
            {
                return null;
            }

            var index = x + z * spec.TileCountX;
            return index >= 0 && index < terrains.Length ? terrains[index] : null;
        }
    }
}

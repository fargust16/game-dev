#if UNITY_EDITOR
using System.Collections.Generic;
using Den.Tools;
using GameDev.World.Poi;
using GameDev.World.Seed;
using GameDev.World.Streaming;
using GameDev.World.Terrain;
using MapMagic.Core;
using MapMagic.Terrains;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Overworld V0 POI groundwork (WORLD_GENERATION_PIPELINE.md Iteration 08 / OVERWORLD_V0_PLAN
    /// W0.6): runs <see cref="StubPoiPlacer"/> over the current MapMagic tile's chunk range and
    /// drops a colored debug marker per placed POI, raycast onto the real terrain surface (same
    /// pattern as VegetationScatterMenu). Candidate chunks cover the actual generated tile extent
    /// rather than the doc's illustrative 512m-centered-on-origin stub, since our tile is
    /// currently a single ~1000m MapMagic tile, not the real W0.5 chunk streamer.
    /// </summary>
    public static class PoiMarkerMenu
    {
        private const string RootObjectName = "OverworldPoi";
        private const string WaterObjectName = "OverworldWater";

        private static readonly Dictionary<PoiKind, Color> KindColors = new Dictionary<PoiKind, Color>
        {
            { PoiKind.Ruin, new Color(0.6f, 0.6f, 0.6f) },
            { PoiKind.Camp, new Color(0.8f, 0.5f, 0.1f) },
            { PoiKind.Shrine, new Color(0.7f, 0.2f, 0.9f) },
            { PoiKind.TownApproach, new Color(0.9f, 0.85f, 0.1f) },
            { PoiKind.CaveMouth, new Color(0.1f, 0.1f, 0.1f) },
        };

        [MenuItem("GameDev/Overworld/Place POI Markers")]
        public static void PlaceMarkers()
        {
            var mapMagic = Object.FindFirstObjectByType<MapMagicObject>();
            if (mapMagic == null || mapMagic.graph == null)
            {
                Debug.LogWarning("[Poi] No generated terrain - run GameDev/Overworld/Generate Terrain From Seed first.");
                return;
            }

            var tileCoord = new Coord(0, 0);
            var tile = mapMagic.tiles[tileCoord];
            var terrain = tile?.main?.terrain;
            var holder = terrain != null ? terrain.GetComponent<DirectMatricesHolder>() : null;
            if (holder == null)
            {
                Debug.LogWarning("[Poi] No DirectMatricesHolder on the terrain - regenerate with OverworldTerrainMenu first.");
                return;
            }

            ClearMarkers();
            var root = new GameObject(RootObjectName);

            var sampler = new MapMagicBiomeSampler(holder);
            var worldSeed = new WorldSeed(OverworldTerrainMenu.LastSeed);
            var candidateChunks = ChunksCoveringTile(tileCoord, mapMagic.tileSize);
            var placer = new StubPoiPlacer(worldSeed, sampler, candidateChunks);

            var waterY = FindWaterLevelY();
            var placed = 0;
            foreach (var chunk in candidateChunks)
            {
                foreach (var marker in placer.PlaceForChunk(chunk))
                {
                    if (!TryFindSurface(marker.Position.x, marker.Position.z, out var y) || y <= waterY)
                    {
                        continue;
                    }

                    var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    visual.name = $"Poi_{marker.Id}";
                    visual.transform.SetParent(root.transform, worldPositionStays: false);
                    visual.transform.position = new Vector3(marker.Position.x, y + 4f, marker.Position.z);
                    visual.transform.localScale = new Vector3(6f, 8f, 6f);

                    var color = KindColors.TryGetValue(marker.Kind, out var c) ? c : Color.white;
                    var renderer = visual.GetComponent<Renderer>();
                    var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    material.color = color;
                    renderer.sharedMaterial = material;

                    placed++;
                }
            }

            Debug.Log($"[Poi] Placed {placed} POI markers (seed {OverworldTerrainMenu.LastSeed}).");
        }

        [MenuItem("GameDev/Overworld/Clear POI Markers")]
        public static void ClearMarkers()
        {
            var root = GameObject.Find(RootObjectName);
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        private static IReadOnlyList<ChunkCoord> ChunksCoveringTile(Coord tileCoord, Vector2D tileSize)
        {
            var minX = ChunkCoord.FromWorld(new Vector3(tileCoord.x * tileSize.x, 0f, 0f)).X;
            var maxX = ChunkCoord.FromWorld(new Vector3((tileCoord.x + 1) * tileSize.x - 0.01f, 0f, 0f)).X;
            var minZ = ChunkCoord.FromWorld(new Vector3(0f, 0f, tileCoord.z * tileSize.z)).Z;
            var maxZ = ChunkCoord.FromWorld(new Vector3(0f, 0f, (tileCoord.z + 1) * tileSize.z - 0.01f)).Z;

            var chunks = new List<ChunkCoord>();
            for (var x = minX; x <= maxX; x++)
            {
                for (var z = minZ; z <= maxZ; z++)
                {
                    chunks.Add(new ChunkCoord(x, z));
                }
            }

            return chunks;
        }

        private static bool TryFindSurface(float x, float z, out float y)
        {
            const float rayStartY = 400f;
            if (Physics.Raycast(new Vector3(x, rayStartY, z), Vector3.down, out var hit, rayStartY * 2f))
            {
                y = hit.point.y;
                return true;
            }

            y = 0f;
            return false;
        }

        private static float FindWaterLevelY()
        {
            var water = GameObject.Find(WaterObjectName);
            return water != null ? water.transform.position.y : float.NegativeInfinity;
        }
    }
}
#endif

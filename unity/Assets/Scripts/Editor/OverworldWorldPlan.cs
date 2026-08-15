#if UNITY_EDITOR
using System.Collections.Generic;
using GameDev.World;
using GameDev.World.Landscape;
using GameDev.World.Poi;
using GameDev.World.Seed;
using GameDev.World.Streaming;
using GameDev.World.Terrain;
using UnityEngine;
using WorldSeed = GameDev.World.Seed.WorldSeed;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Builds / reads the shared <see cref="WorldDefinition"/> POI plan so settlement, roads,
    /// and marker visuals never re-run placement independently.
    /// </summary>
    public static class OverworldWorldPlan
    {
        public static WorldDefinition EnsureDefinition(int seed, WorldGenSettings settings)
        {
            settings.Clamp();
            var state = OverworldWorldState.FindOrCreate();
            if (state.Definition != null &&
                state.Definition.Seed == seed)
            {
                state.Definition.Settings = settings;
                return state.Definition;
            }

            var def = new WorldDefinition
            {
                Seed = seed,
                Settings = settings,
            };
            state.SetDefinition(def);
            return def;
        }

        public static IReadOnlyList<PoiMarker> GetOrBuildPois()
        {
            var settings = OverworldTerrainMenu.ActiveSettings;
            settings.Clamp();
            var def = EnsureDefinition(OverworldTerrainMenu.LastSeed, settings);

            if (def.Pois != null && def.Pois.Count > 0)
            {
                return def.Pois;
            }

            return BuildAndStorePois(def);
        }

        public static IReadOnlyList<PoiMarker> BuildAndStorePois(WorldDefinition def = null)
        {
            var terrain = OverworldTerrainAccess.FindTerrain();
            var sampler = OverworldTerrainAccess.CreateSampler(OverworldTerrainMenu.LastSeed);
            if (terrain == null || sampler == null)
            {
                return System.Array.Empty<PoiMarker>();
            }

            var settings = OverworldTerrainMenu.ActiveSettings;
            settings.Clamp();
            def ??= EnsureDefinition(OverworldTerrainMenu.LastSeed, settings);

            var worldSeed = new WorldSeed(OverworldTerrainMenu.LastSeed);
            var tileSize = WorldScale.OverworldTileMeters;
            var chunks = OverworldTileChunks.CoveringTile(
                0, 0, tileSize, tileSize, OverworldTileChunks.DefaultMarginMeters);
            var margin = OverworldTileChunks.DefaultMarginMeters;
            var minX = margin;
            var minZ = margin;
            var usableSizeX = tileSize - 2f * margin;
            var usableSizeZ = tileSize - 2f * margin;
            float HeightMeters(float x, float z) => sampler.SampleWorldHeight(x, z);
            var analysis = new LandscapeCostField(
                minX, minZ, usableSizeX, usableSizeZ,
                WorldScale.LandscapeCostCellMeters,
                HeightMeters,
                (x, z) => sampler.SampleBiome(x, z));

            var placer = new StubPoiPlacer(
                worldSeed,
                sampler,
                chunks,
                PoiDemoScenario.BuildRequests(settings),
                analysis);

            var markers = placer.AllMarkers();
            def.SetPois(markers);
            return def.Pois;
        }

        public static void StoreRoads(IReadOnlyList<GameDev.World.Roads.RoadDefinition> roads)
        {
            var state = OverworldWorldState.Find();
            if (state?.Definition == null)
            {
                return;
            }

            state.Definition.SetRoads(roads);
        }
    }
}
#endif

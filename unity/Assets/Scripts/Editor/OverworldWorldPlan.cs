#if UNITY_EDITOR
using System.Collections.Generic;
using Den.Tools;
using GameDev.World;
using GameDev.World.Landscape;
using GameDev.World.Poi;
using GameDev.World.Seed;
using GameDev.World.Streaming;
using GameDev.World.Terrain;
using GameDev.World.Water;
using MapMagic.Core;
using MapMagic.Terrains;
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

        public static IReadOnlyList<PoiMarker> GetOrBuildPois(
            MapMagicObject mapMagic,
            Coord tileCoord)
        {
            var settings = OverworldTerrainMenu.ActiveSettings;
            settings.Clamp();
            var def = EnsureDefinition(OverworldTerrainMenu.LastSeed, settings);

            if (def.Pois != null && def.Pois.Count > 0)
            {
                return def.Pois;
            }

            return BuildAndStorePois(mapMagic, tileCoord, def);
        }

        public static IReadOnlyList<PoiMarker> BuildAndStorePois(
            MapMagicObject mapMagic,
            Coord tileCoord,
            WorldDefinition def = null)
        {
            var tile = mapMagic.tiles[tileCoord];
            var terrain = tile?.main?.terrain;
            var holder = terrain != null ? terrain.GetComponent<DirectMatricesHolder>() : null;
            if (holder == null)
            {
                return System.Array.Empty<PoiMarker>();
            }

            var settings = OverworldTerrainMenu.ActiveSettings;
            settings.Clamp();
            def ??= EnsureDefinition(OverworldTerrainMenu.LastSeed, settings);

            var sampler = new MapMagicBiomeSampler(holder);
            var worldSeed = new WorldSeed(OverworldTerrainMenu.LastSeed);
            var chunks = OverworldTileChunks.CoveringTile(
                tileCoord, mapMagic.tileSize, OverworldTileChunks.DefaultMarginMeters);

            var lakeZones = Object.FindFirstObjectByType<OverworldWaterZones>();
            var margin = OverworldTileChunks.DefaultMarginMeters;
            var minX = tileCoord.x * mapMagic.tileSize.x + margin;
            var minZ = tileCoord.z * mapMagic.tileSize.z + margin;
            var usableSizeX = mapMagic.tileSize.x - 2f * margin;
            var usableSizeZ = mapMagic.tileSize.z - 2f * margin;
            var worldHeightScale = mapMagic.globals.height;
            float HeightMeters(float x, float z) => sampler.SampleHeight(x, z) * worldHeightScale;

            var lakes = lakeZones != null
                ? lakeZones.LakeDefinitions
                : System.Array.Empty<LakeDefinition>();

            if (lakes.Count > 0)
            {
                def.SetLakes(lakes);
            }

            var analysis = new LandscapeCostField(
                minX, minZ, usableSizeX, usableSizeZ,
                WorldScale.LandscapeCostCellMeters,
                HeightMeters,
                (x, z) => sampler.SampleBiome(x, z),
                lakes);

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

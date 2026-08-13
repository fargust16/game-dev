#if UNITY_EDITOR
using System.Collections.Generic;
using Den.Tools;
using GameDev.World.Buildings.Shell;
using GameDev.World.Poi;
using GameDev.World.Seed;
using GameDev.World.Streaming;
using GameDev.World.Terrain;
using GameDev.World.Water;
using MapMagic.Core;
using MapMagic.Terrains;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Overworld V0 POI groundwork (WORLD_GENERATION_PIPELINE.md Iteration 08 / OVERWORLD_V0_PLAN
    /// W0.6): runs <see cref="StubPoiPlacer"/> over the current MapMagic tile's chunk range and
    /// drops a visual per placed POI, grounded via <c>Terrain.SampleHeight</c> (same pattern as
    /// VegetationScatterMenu — Physics raycasts miss when tile height exceeds the old 400m ray). Most kinds get a real Low Poly Environment/Village prop (see
    /// ASSET_LIBRARY_REFERENCE.md - no dedicated ruin/shrine/cave asset exists, so each reuses the
    /// closest-fitting prop from the same library VegetationScatterMenu already draws from); a
    /// "city" (TownApproach) or Camp instead borrows a single generated building from the Exterior
    /// Shell Grammar system (<see cref="GameDev.World.Buildings.Shell"/>, normally used for whole
    /// settlement fabrics) as a stand-in - an Elite mansion for a city, a Poor house for a camp -
    /// until real settlement placement replaces these markers outright. Candidate chunks cover the
    /// actual generated tile extent rather than the doc's illustrative 512m-centered-on-origin stub,
    /// since our tile is currently a single ~1400m MapMagic tile, not the real W0.5 chunk streamer.
    /// </summary>
    public static class PoiMarkerMenu
    {
        private const string RootObjectName = "OverworldPoi";
        private const string PrefabRoot = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs";

        // Scaled up from these props' normal ambient-scatter size so a POI still reads as
        // deliberately placed rather than blending into VegetationScatterMenu's background growth.
        private const float MarkerScale = 1.6f;

        private struct Entry
        {
            public string PrefabPath;
            public float Weight;
        }

        // TownApproach and Camp are handled separately via SpawnBuilding (see BuildingKindDistricts
        // below) instead of a prop - a generated building reads far better as "this is a
        // settlement/camp" than any single static prop the imported library has.
        private static readonly Dictionary<PoiKind, DistrictKind> BuildingKindDistricts = new Dictionary<PoiKind, DistrictKind>
        {
            { PoiKind.TownApproach, DistrictKind.Elite },
            { PoiKind.Camp, DistrictKind.Poor },
        };

        // No dedicated ruin/shrine/cave asset exists in the imported library (see
        // ASSET_LIBRARY_REFERENCE.md) - each kind reuses the closest-fitting prop: a standing stone
        // as a shrine, rock rubble/piles as ruined stonework or a cave mouth's boulders.
        private static readonly Dictionary<PoiKind, Entry[]> KindProps = new Dictionary<PoiKind, Entry[]>
        {
            {
                PoiKind.Shrine, new[]
                {
                    new Entry { PrefabPath = $"{PrefabRoot}/Rocks/PT_Menhir_Rock_02.prefab", Weight = 1f },
                }
            },
            {
                PoiKind.Ruin, new[]
                {
                    new Entry { PrefabPath = $"{PrefabRoot}/Rocks/PT_Ore_Rock_01_split.prefab", Weight = 0.6f },
                    new Entry { PrefabPath = $"{PrefabRoot}/Rocks/PT_Generic_Rock_01.prefab", Weight = 0.4f },
                }
            },
            {
                PoiKind.CaveMouth, new[]
                {
                    new Entry { PrefabPath = $"{PrefabRoot}/Rocks/PT_River_Rock_Pile_02.prefab", Weight = 1f },
                }
            },
        };

        // Landmark is the one kind requested per-biome (PoiDemoScenario) rather than as a flat
        // kind-wide look, so its prop varies by the marker's own biome instead of a single shared
        // entry - a dead tree in a Swamp landmark reads differently than a lone boulder in Highland.
        private static readonly Dictionary<BiomeId, Entry[]> LandmarkPropsByBiome = new Dictionary<BiomeId, Entry[]>
        {
            { BiomeId.Swamp, new[] { new Entry { PrefabPath = $"{PrefabRoot}/Trees/PT_Fruit_Tree_01_dead.prefab", Weight = 1f } } },
            {
                BiomeId.Meadow, new[]
                {
                    new Entry { PrefabPath = $"{PrefabRoot}/Trees/PT_Fruit_Tree_01_apples.prefab", Weight = 0.5f },
                    new Entry { PrefabPath = $"{PrefabRoot}/Flowers/PT_Poppy_02.prefab", Weight = 0.5f },
                }
            },
            { BiomeId.Forest, new[] { new Entry { PrefabPath = $"{PrefabRoot}/Trees/PT_Pine_Tree_03_stump.prefab", Weight = 1f } } },
            { BiomeId.Highland, new[] { new Entry { PrefabPath = $"{PrefabRoot}/Rocks/PT_Generic_Rock_01.prefab", Weight = 1f } } },
            { BiomeId.Mountain, new[] { new Entry { PrefabPath = $"{PrefabRoot}/Rocks/PT_Ore_Rock_01.prefab", Weight = 1f } } },
        };

        [MenuItem("GameDev/Overworld/Place POI Markers")]
        public static void PlaceMarkers()
        {
            var mapMagic = Object.FindFirstObjectByType<MapMagicObject>();
            if (mapMagic == null || mapMagic.graph == null)
            {
                return;
            }

            var tileCoord = new Coord(0, 0);
            var tile = mapMagic.tiles[tileCoord];
            if (tile == null)
            {
                return;
            }

            MapMagicTileSync.WaitForTileReady(tile);

            var terrain = tile.main?.terrain;
            var holder = terrain != null ? terrain.GetComponent<DirectMatricesHolder>() : null;
            if (terrain == null || holder == null)
            {
                Debug.LogWarning("[POI] PlaceMarkers aborted: terrain or DirectMatricesHolder missing.");
                return;
            }

            ClearMarkers();
            var root = new GameObject(RootObjectName);

            var sampler = new MapMagicBiomeSampler(holder);
            var worldSeed = new WorldSeed(OverworldTerrainMenu.LastSeed);
            var candidateChunks = OverworldTileChunks.CoveringTile(tileCoord, mapMagic.tileSize, OverworldTileChunks.DefaultMarginMeters);
            var placer = new StubPoiPlacer(worldSeed, sampler, candidateChunks, PoiDemoScenario.BuildRequests());

            var lakeZones = Object.FindFirstObjectByType<OverworldWaterZones>();
            var riverZones = Object.FindFirstObjectByType<OverworldRiverZones>();
            IWaterSampler waterZones = new CompositeWaterSampler(lakeZones, riverZones);
            var visualRng = worldSeed.Fork("poi-visual");
            var buildingArchetypes = ShellCatalogFactory.CreateDefaultArchetypes();
            var placed = 0;
            var skippedNoSurface = 0;
            var skippedWater = 0;
            var skippedNoPrefab = 0;
            foreach (var chunk in candidateChunks)
            {
                foreach (var marker in placer.PlaceForChunk(chunk))
                {
                    if (!TrySampleTerrainHeight(terrain, marker.Position.x, marker.Position.z, out var y))
                    {
                        skippedNoSurface++;
                        continue;
                    }

                    if (waterZones != null &&
                        waterZones.TryGetWaterLevel(marker.Position.x, marker.Position.z, out var waterY) &&
                        y <= waterY)
                    {
                        skippedWater++;
                        continue;
                    }

                    if (BuildingKindDistricts.TryGetValue(marker.Kind, out var districtKind))
                    {
                        SpawnBuilding(root.transform, marker, districtKind, y, worldSeed, buildingArchetypes);
                        placed++;
                        continue;
                    }

                    var prefab = PickPrefab(marker.Kind, marker.Biome, visualRng);
                    if (prefab == null)
                    {
                        skippedNoPrefab++;
                        continue;
                    }

                    var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    visual.name = $"Poi_{marker.Id}";
                    visual.transform.SetParent(root.transform, worldPositionStays: false);
                    visual.transform.position = new Vector3(marker.Position.x, y, marker.Position.z);
                    visual.transform.rotation = Quaternion.Euler(0f, (float)visualRng.NextDouble() * 360f, 0f);
                    visual.transform.localScale = Vector3.one * MarkerScale;
                    placed++;
                }
            }

            Debug.Log(
                $"[POI] placed={placed} (seed={OverworldTerrainMenu.LastSeed}, markers={placer.AllMarkers().Count}) " +
                $"skipped: noSurface={skippedNoSurface} water={skippedWater} noPrefab={skippedNoPrefab}");
        }

        // Temporary stand-in (per WORLD_GENERATION_PIPELINE.md Iteration 08/10 notes) for real
        // settlement placement: borrows the Exterior Shell Grammar's single-building path (same
        // Select -> Sample -> Bind -> SpawnShellRoot sequence ShellSandboxLab.GenerateSingle uses
        // for a whole street) to drop exactly one building per city/camp marker, instead of a
        // static prop.
        private static void SpawnBuilding(
            Transform parent,
            PoiMarker marker,
            DistrictKind districtKind,
            float groundY,
            IWorldSeed worldSeed,
            IReadOnlyList<BuildingArchetypeDef> archetypes)
        {
            var district = ShellCatalogFactory.CreateDistrict(districtKind);
            var origin = new Vector3(marker.Position.x, groundY, marker.Position.z);

            var plotRng = worldSeed.Fork($"poi-building-plot_{marker.Id.Value}");
            var width = district.RollWidthBays(plotRng);
            var depth = district.RollDepthBays(plotRng);
            var yaw = plotRng.Next(0, 4) * 90f;
            var plot = PlotFootprint.Rect(width, depth, origin, yaw);
            var street = StreetContext.StreetFrontYardBack();

            var selector = new ShellArchetypeSelector(archetypes);
            var archRng = worldSeed.Fork($"poi-building-arch_{marker.Id.Value}");
            var archetype = selector.Select(plot, district, archRng);

            // Shop-biased picks can want a plot slightly bigger than what was rolled - re-roll width
            // once, same as ShellSandboxLab.GenerateSingle does.
            if (archetype != null && !archetype.FitsPlot(plot))
            {
                width = Mathf.Clamp(width + 1, district.MinWidthBays, district.MaxWidthBays);
                plot = PlotFootprint.Rect(width, depth, origin, yaw);
                archetype = selector.Select(plot, district, archRng);
            }

            if (archetype == null)
            {
                return;
            }

            var dnaRng = worldSeed.Fork($"poi-building-dna_{marker.Id.Value}");
            var dna = ShellDnaSampler.Sample(archetype, district, plot, street, OverworldTerrainMenu.LastSeed, 0, dnaRng);

            var binder = new BuildingShellBinder();
            var shell = binder.Bind(dna, plot, street, archetype, district);
            ShellPlaceholderFactory.SpawnShellRoot(shell, parent, $"Poi_{marker.Id}_{district.Id}");
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

        private static bool TrySampleTerrainHeight(UnityEngine.Terrain terrain, float x, float z, out float y)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                y = 0f;
                return false;
            }

            var pos = terrain.transform.position;
            var size = terrain.terrainData.size;
            if (x < pos.x || z < pos.z || x > pos.x + size.x || z > pos.z + size.z)
            {
                y = 0f;
                return false;
            }

            y = terrain.SampleHeight(new Vector3(x, 0f, z)) + pos.y;
            return true;
        }

        private static GameObject PickPrefab(PoiKind kind, BiomeId biome, System.Random rng)
        {
            var entries = kind == PoiKind.Landmark
                ? (LandmarkPropsByBiome.TryGetValue(biome, out var biomeEntries) ? biomeEntries : null)
                : (KindProps.TryGetValue(kind, out var kindEntries) ? kindEntries : null);

            if (entries == null || entries.Length == 0)
            {
                return null;
            }

            var totalWeight = 0f;
            foreach (var entry in entries)
            {
                totalWeight += entry.Weight;
            }

            var roll = (float)rng.NextDouble() * totalWeight;
            var accum = 0f;
            foreach (var entry in entries)
            {
                accum += entry.Weight;
                if (roll <= accum)
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(entry.PrefabPath);
                }
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(entries[entries.Length - 1].PrefabPath);
        }
    }
}
#endif

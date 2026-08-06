using GameDev.World.Town.Geometry;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameDev.World.Town
{
    /// <summary>
    /// Milestone B/D1 greybox layer over <see cref="TownModel"/>.
    /// Streets + exterior shells or enterable template cells + landmark.
    /// No GridMap bridge, narrative POIs, or occlusion.
    /// </summary>
    public static class TownGreyboxBaker
    {
        public const string RootName = "TownGreybox";
        private const string PrefabFolder = "Assets/Prefabs/S0_Greybox/";
        private const float FloorY = 0.02f;
        private const float MinSegmentLength = 0.35f;
        /// <summary>Landmark_Block Visual local scale on XZ.</summary>
        private const float LandmarkPrefabXZ = 0.8f;
        /// <summary>Landmark_Block Visual local scale on Y (height at root scale 1).</summary>
        private const float LandmarkPrefabY = 1.5f;
        private const float StoreyMetres = 2f;
        /// <summary>Wall_Segment_1x3 Visual thickness on local Z at root scale 1.</summary>
        private const float WallPrefabThickness = 0.3f;

        public sealed class PrefabSet
        {
            public GameObject FloorTile;
            public GameObject Building1x1;
            public GameObject LandmarkBlock;
            public GameObject WallSegment;
        }

        public static void Clear(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child.name != RootName)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Object.Destroy(child.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        public static void Bake(Transform parent, TownModel model, PrefabSet prefs)
        {
            if (parent == null || model == null)
            {
                return;
            }

            if (prefs == null)
            {
                prefs = new PrefabSet();
            }

            ResolvePrefabs(prefs);
            Clear(parent);

            var root = new GameObject(RootName);
            root.transform.SetParent(parent, false);

            var streets = new GameObject("Streets");
            streets.transform.SetParent(root.transform, false);
            BakeStreets(streets.transform, model, prefs.FloorTile);

            var buildings = new GameObject("Buildings");
            buildings.transform.SetParent(root.transform, false);
            BakeBuildings(buildings.transform, model, prefs);

            var landmarkRoot = new GameObject("Landmark");
            landmarkRoot.transform.SetParent(root.transform, false);
            BakeLandmark(landmarkRoot.transform, model, prefs.LandmarkBlock);

            EnsureSun(root.transform);

            CountBuildingBake(model, out var shells, out var interiors);
            Debug.Log(
                $"[TownGreybox] baked roads={model.RoadEdges.Count} " +
                $"shells={shells} interiors={interiors} landmark={(model.Landmark != null ? 1 : 0)}");
        }

        private static void CountBuildingBake(TownModel model, out int shells, out int interiors)
        {
            shells = 0;
            interiors = 0;
            for (var i = 0; i < model.Buildings.Count; i++)
            {
                var b = model.Buildings[i];
                if (b.Kind == BuildingKind.Landmark)
                {
                    continue;
                }

                if (b.HasTemplate)
                {
                    interiors++;
                }
                else
                {
                    shells++;
                }
            }
        }

        private static void BakeStreets(Transform parent, TownModel model, GameObject floorPrefab)
        {
            for (var ei = 0; ei < model.RoadEdges.Count; ei++)
            {
                var edge = model.RoadEdges[ei];
                if (edge.Polyline == null || edge.Polyline.Count < 2)
                {
                    continue;
                }

                // Width is full corridor width in metres (TownProfile road ranges).
                var width = Mathf.Max(1f, edge.Width);
                var edgeRoot = new GameObject($"Road_{ei}_{edge.Class}");
                edgeRoot.transform.SetParent(parent, false);

                for (var i = 0; i < edge.Polyline.Count - 1; i++)
                {
                    var a = edge.Polyline[i];
                    var b = edge.Polyline[i + 1];
                    var delta = b - a;
                    var len = delta.magnitude;
                    if (len < MinSegmentLength)
                    {
                        continue;
                    }

                    var mid = (a + b) * 0.5f;
                    var go = SpawnPrefab(floorPrefab, edgeRoot.transform, $"Seg_{i}");
                    go.transform.position = Geo2.ToVector3(mid, FloorY);
                    go.transform.rotation = Quaternion.LookRotation(Geo2.ToVector3(delta / len), Vector3.up);
                    go.transform.localScale = new Vector3(width, 1f, len);
                    DisableColliders(go);
                }
            }
        }

        private static void BakeBuildings(Transform parent, TownModel model, PrefabSet prefs)
        {
            for (var bi = 0; bi < model.Buildings.Count; bi++)
            {
                var building = model.Buildings[bi];
                if (building.Kind == BuildingKind.Landmark)
                {
                    continue;
                }

                if (building.Footprint == null || building.Footprint.Count < 3)
                {
                    continue;
                }

                if (building.HasTemplate)
                {
                    var template = BuildingTemplateCatalog.Get(building.TemplateId);
                    if (template != null)
                    {
                        var root = new GameObject($"Interior_{bi}_{template.Id}");
                        root.transform.SetParent(parent, false);
                        BakeInterior(root.transform, building, template, prefs);
                        BakeSocketMarkers(root.transform, building);
                        continue;
                    }
                }

                if (!TownFootprintUtil.TryObb(building.Footprint, out var center, out var axis, out var sizeX, out var sizeZ))
                {
                    continue;
                }

                // Solid shell fallback when no template fits.
                var go = SpawnPrefab(prefs.Building1x1, parent, $"Building_{bi}_{building.Kind}");
                PlaceOrientedShell(go, center, axis, sizeX, sizeZ, building.Floors);
            }
        }

        private static void BakeInterior(
            Transform parent,
            Building building,
            BuildingTemplate template,
            PrefabSet prefs)
        {
            var floors = Mathf.Max(0.5f, building.Floors);
            var yaw = building.TemplateYawDeg;
            var cs = template.CellSize;

            for (var y = 0; y < template.Height; y++)
            {
                for (var x = 0; x < template.Width; x++)
                {
                    var kind = template.GetCell(x, y);
                    if (kind == BuildingInteriorCellKind.Empty)
                    {
                        continue;
                    }

                    var center = TownFootprintUtil.CellCenterWorld(building, template, x, y);
                    var label = $"{kind}_{x}_{y}";

                    if (kind == BuildingInteriorCellKind.Wall)
                    {
                        var wallPrefab = prefs.WallSegment != null ? prefs.WallSegment : prefs.Building1x1;
                        var wall = SpawnPrefab(wallPrefab, parent, label);
                        wall.transform.position = Geo2.ToVector3(center, 0f);
                        wall.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                        if (prefs.WallSegment != null && wallPrefab == prefs.WallSegment)
                        {
                            // Fill 1 m cell: X = cell, Z thickens the thin wall visual.
                            wall.transform.localScale = new Vector3(cs, floors, cs / WallPrefabThickness);
                        }
                        else
                        {
                            wall.transform.localScale = new Vector3(cs, floors, cs);
                        }

                        DisableColliders(wall);
                        continue;
                    }

                    // Floor / door / sockets — walkable paint (door = opening, no wall).
                    var floor = SpawnPrefab(prefs.FloorTile, parent, label);
                    floor.transform.position = Geo2.ToVector3(center, FloorY);
                    floor.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    floor.transform.localScale = new Vector3(cs, 1f, cs);
                    DisableColliders(floor);
                }
            }
        }

        /// <summary>
        /// Tiny greybox pedestals for placed sockets (debug/readability). C1 will replace with real content.
        /// </summary>
        private static void BakeSocketMarkers(Transform parent, Building building)
        {
            var sockets = building.Sockets;
            if (sockets == null || sockets.Count == 0)
            {
                return;
            }

            var root = new GameObject("Sockets");
            root.transform.SetParent(parent, false);
            for (var i = 0; i < sockets.Count; i++)
            {
                var s = sockets[i];
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = $"Socket_{s.Index}_{s.Role}";
                marker.transform.SetParent(root.transform, false);
                var height = s.CellKind == BuildingInteriorCellKind.NpcSocket ? 1.2f : 0.55f;
                marker.transform.position = Geo2.ToVector3(s.WorldPos, height * 0.5f);
                marker.transform.localScale = new Vector3(0.35f, height, 0.35f);
                DisableColliders(marker);
            }
        }

        private static void BakeLandmark(Transform parent, TownModel model, GameObject landmarkPrefab)
        {
            if (model.Landmark == null || model.Landmark.Footprint == null || model.Landmark.Footprint.Count < 3)
            {
                return;
            }

            if (!TownFootprintUtil.TryObb(model.Landmark.Footprint, out var center, out var axis, out var sizeX, out var sizeZ))
            {
                return;
            }

            var floors = model.Landmark.Kind == LandmarkKind.Castle ? 4f : 2.5f;
            var go = SpawnPrefab(landmarkPrefab, parent, $"Landmark_{model.Landmark.Kind}");
            var yaw = Mathf.Atan2(axis.x, axis.y) * Mathf.Rad2Deg;
            var sx = Mathf.Max(2f, sizeX) / LandmarkPrefabXZ;
            var sz = Mathf.Max(2f, sizeZ) / LandmarkPrefabXZ;
            var sy = Mathf.Max(1f, floors * StoreyMetres / LandmarkPrefabY);
            go.transform.position = Geo2.ToVector3(center, 0f);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = new Vector3(sx, sy, sz);
        }

        private static void PlaceOrientedShell(
            GameObject go,
            Vector2 center,
            Vector2 axis,
            float sizeX,
            float sizeZ,
            float floors)
        {
            var yaw = Mathf.Atan2(axis.x, axis.y) * Mathf.Rad2Deg;
            // Building_Shell_1x1 Visual is ~2 m tall at root Y scale 1 → root Y = floor count.
            go.transform.position = Geo2.ToVector3(center, 0f);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = new Vector3(
                Mathf.Max(1f, sizeX),
                Mathf.Max(0.5f, floors),
                Mathf.Max(1f, sizeZ));
        }

        private static void EnsureSun(Transform root)
        {
            var sun = new GameObject("Sun_Greybox");
            sun.transform.SetParent(root, false);
            sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.88f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
        }

        private static GameObject SpawnPrefab(GameObject prefab, Transform parent, string label)
        {
            GameObject go;
            if (prefab != null)
            {
#if UNITY_EDITOR
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
#else
                go = Object.Instantiate(prefab, parent);
#endif
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.SetParent(parent, false);
                Debug.LogWarning($"[TownGreybox] Missing prefab for '{label}'. Using cube fallback.");
            }

            go.name = label;
            return go;
        }

        private static void DisableColliders(GameObject go)
        {
            var cols = go.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < cols.Length; i++)
            {
                cols[i].enabled = false;
            }
        }

        private static void ResolvePrefabs(PrefabSet prefs)
        {
#if UNITY_EDITOR
            if (prefs.FloorTile == null)
            {
                prefs.FloorTile = LoadPrefab("Floor_Tile_1x1.prefab");
            }

            if (prefs.Building1x1 == null)
            {
                prefs.Building1x1 = LoadPrefab("Building_Shell_1x1.prefab");
            }

            if (prefs.LandmarkBlock == null)
            {
                prefs.LandmarkBlock = LoadPrefab("Landmark_Block.prefab");
            }

            if (prefs.WallSegment == null)
            {
                prefs.WallSegment = LoadPrefab("Wall_Segment_1x3.prefab");
            }
#endif
        }

#if UNITY_EDITOR
        private static GameObject LoadPrefab(string fileName) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + fileName);
#endif
    }
}

using GameDev.World.Town.Geometry;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameDev.World.Town
{
    /// <summary>
    /// Milestone B — thin greybox visual layer over <see cref="TownModel"/>.
    /// Street ribbons + exterior shells + landmark. No GridMap, interiors, or POIs.
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

        public sealed class PrefabSet
        {
            public GameObject FloorTile;
            public GameObject Building1x1;
            public GameObject LandmarkBlock;
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

            Debug.Log(
                $"[TownGreybox] baked roads={model.RoadEdges.Count} " +
                $"shells={CountNonLandmarkBuildings(model)} landmark={(model.Landmark != null ? 1 : 0)}");
        }

        private static int CountNonLandmarkBuildings(TownModel model)
        {
            var n = 0;
            for (var i = 0; i < model.Buildings.Count; i++)
            {
                if (model.Buildings[i].Kind != BuildingKind.Landmark)
                {
                    n++;
                }
            }

            return n;
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

                if (!TryFootprintObb(building.Footprint, out var center, out var axis, out var sizeX, out var sizeZ))
                {
                    continue;
                }

                // Always 1×1 shell: unit footprint base so localScale maps 1:1 to metres.
                var go = SpawnPrefab(prefs.Building1x1, parent, $"Building_{bi}_{building.Kind}");
                PlaceOrientedShell(go, center, axis, sizeX, sizeZ, building.Floors);
            }
        }

        private static void BakeLandmark(Transform parent, TownModel model, GameObject landmarkPrefab)
        {
            if (model.Landmark == null || model.Landmark.Footprint == null || model.Landmark.Footprint.Count < 3)
            {
                return;
            }

            if (!TryFootprintObb(model.Landmark.Footprint, out var center, out var axis, out var sizeX, out var sizeZ))
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

        /// <summary>
        /// Footprint OBB: <paramref name="axis"/> aligns with local +Z;
        /// <paramref name="sizeX"/> is width across axis, <paramref name="sizeZ"/> along axis.
        /// </summary>
        private static bool TryFootprintObb(
            Polygon2 poly,
            out Vector2 center,
            out Vector2 axis,
            out float sizeX,
            out float sizeZ)
        {
            center = poly.Centroid;
            axis = Vector2.up;
            sizeX = 1f;
            sizeZ = 1f;

            var verts = poly.Vertices;
            if (verts.Count < 3)
            {
                return false;
            }

            var (longAxis, splitDir) = Geo2.ObbLongestAxis(verts);
            if (longAxis.sqrMagnitude < 1e-8f)
            {
                return false;
            }

            axis = longAxis.normalized;
            var perp = splitDir.sqrMagnitude > 1e-8f
                ? splitDir.normalized
                : new Vector2(-axis.y, axis.x);

            var minA = float.MaxValue;
            var maxA = float.MinValue;
            var minP = float.MaxValue;
            var maxP = float.MinValue;
            for (var i = 0; i < verts.Count; i++)
            {
                var v = verts[i];
                var pa = Vector2.Dot(v, axis);
                var pp = Vector2.Dot(v, perp);
                if (pa < minA) minA = pa;
                if (pa > maxA) maxA = pa;
                if (pp < minP) minP = pp;
                if (pp > maxP) maxP = pp;
            }

            sizeZ = Mathf.Max(0.5f, maxA - minA);
            sizeX = Mathf.Max(0.5f, maxP - minP);
            center = axis * ((minA + maxA) * 0.5f) + perp * ((minP + maxP) * 0.5f);
            return true;
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
#endif
        }

#if UNITY_EDITOR
        private static GameObject LoadPrefab(string fileName) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + fileName);
#endif
    }
}

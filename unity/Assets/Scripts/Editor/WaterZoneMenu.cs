#if UNITY_EDITOR
using GameDev.World;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Ocean plane only. Inland lake/river generation removed.
    /// </summary>
    public static class WaterZoneMenu
    {
        private const string RootObjectName = "OverworldWater";
        private const string RiverRootObjectName = "OverworldRivers";
        private const string WorldWaterObjectName = "WorldWater";
        private const string WaterWorksPlanePrefabPath = "Assets/WaterWorks/Water_Plane.prefab";
        private const string OverworldWaterMaterialPath = "Assets/Settings/Water/OverworldWater.mat";

        [MenuItem("GameDev/Overworld/Ensure Ocean Plane", false, 40)]
        public static void EnsureOceanMenu() => EnsureOceanOcWPlane();

        public static void ClearWaterZones()
        {
            var root = GameObject.Find(RootObjectName);
            if (root != null)
                Object.DestroyImmediate(root);
            ClearRiverZones();
        }

        public static void ClearRiverZones()
        {
            foreach (var name in new[]
                     {
                         RiverRootObjectName, "OverworldRivers_Vista", "VistaHydroRivers"
                     })
            {
                var go = GameObject.Find(name);
                if (go != null)
                    Object.DestroyImmediate(go);
            }
        }

        public static void ClearWaterMeshesOnly()
        {
            var root = GameObject.Find(RootObjectName);
            if (root == null)
            {
                EnsureOceanOcWPlane();
                return;
            }

            for (var i = root.transform.childCount - 1; i >= 0; i--)
            {
                var child = root.transform.GetChild(i).gameObject;
                if (child.name == WorldWaterObjectName)
                    continue;
                if (child.name.StartsWith("Lake_") ||
                    child.name.StartsWith("River_") ||
                    child.name.StartsWith("WaterZone_"))
                    Object.DestroyImmediate(child);
            }

            EnsureOceanOcWPlane();
        }

        public static void EnsureOceanOcWPlane()
        {
            var waterRoot = GameObject.Find(RootObjectName);
            if (waterRoot == null)
                waterRoot = new GameObject(RootObjectName);

            for (var i = waterRoot.transform.childCount - 1; i >= 0; i--)
            {
                var child = waterRoot.transform.GetChild(i).gameObject;
                if (child.name == WorldWaterObjectName)
                    continue;
                if (child.name == "Ocean" ||
                    child.name.StartsWith("WaterZone_") ||
                    child.name.StartsWith("Lake_") ||
                    child.name.StartsWith("River_"))
                    Object.DestroyImmediate(child);
            }

            var waterTf = waterRoot.transform.Find(WorldWaterObjectName);
            GameObject water;
            if (waterTf == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WaterWorksPlanePrefabPath);
                if (prefab != null)
                {
                    water = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    water.name = WorldWaterObjectName;
                    water.transform.SetParent(waterRoot.transform, worldPositionStays: true);
                }
                else
                {
                    water = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    water.name = WorldWaterObjectName;
                    water.transform.SetParent(waterRoot.transform, worldPositionStays: true);
                    var col = water.GetComponent<Collider>();
                    if (col != null)
                        Object.DestroyImmediate(col);
                }
            }
            else
                water = waterTf.gameObject;

            var extent = Mathf.Max(WorldScale.WorldExtentXMeters, WorldScale.WorldExtentZMeters);
            var planeScale = extent / 10f;
            water.transform.position = new Vector3(
                WorldScale.WorldExtentXMeters * 0.5f,
                WorldScale.SeaLevelMeters,
                WorldScale.WorldExtentZMeters * 0.5f);
            water.transform.rotation = Quaternion.identity;
            water.transform.localScale = new Vector3(planeScale, 1f, planeScale);

            var renderer = water.GetComponent<MeshRenderer>();
            if (renderer == null)
                renderer = water.AddComponent<MeshRenderer>();
            var mat = AssetDatabase.LoadAssetAtPath<Material>(OverworldWaterMaterialPath);
            if (mat != null)
                renderer.sharedMaterial = mat;
        }
    }
}
#endif

#if UNITY_EDITOR
using GameDev.World;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameDev.EditorTools
{
    /// <summary>
    /// The overworld's ocean: one WaterWorks plane at <see cref="WorldScale.SeaLevelMeters"/>.
    /// </summary>
    /// <remarks>
    /// The ocean sheet and nothing else. Inland rivers and lakes are drawn by
    /// <c>InlandWaterMeshBuilder</c> from a generated <c>HydrologyDefinition</c>, parented under this
    /// file's <c>OverworldWater</c> root so one clear takes all water down together. Geography is
    /// decided there and read here — nothing in this file should ever become the record of where
    /// water is, which is exactly what sank the removed first attempt.
    /// </remarks>
    public static class WaterZoneMenu
    {
        private const string RootObjectName = "OverworldWater";
        private const string WorldWaterObjectName = "WorldWater";
        private const string WaterWorksPlanePrefabPath = "Assets/WaterWorks/Water_Plane.prefab";
        private const string OverworldWaterMaterialPath = "Assets/Settings/Water/OverworldWater.mat";

        [MenuItem("GameDev/Overworld/Ensure Ocean Plane", false, 40)]
        public static void EnsureOceanMenu() => EnsureOceanOcWPlane();

        /// <summary>Removes the water root entirely; the next bake re-creates the ocean.</summary>
        public static void ClearWaterZones()
        {
            var root = GameObject.Find(RootObjectName);
            if (root != null)
                Object.DestroyImmediate(root);
        }

        /// <summary>Drops everything under the water root except the ocean sheet itself.</summary>
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
                Object.DestroyImmediate(child);
            }

            EnsureOceanOcWPlane();
        }

        public static void EnsureOceanOcWPlane()
        {
            var waterRoot = GameObject.Find(RootObjectName);
            if (waterRoot == null)
                waterRoot = new GameObject(RootObjectName);

            // Anything under the root that is neither the ocean sheet nor the generated inland water
            // is left over from an older bake. The inland root has to survive: the pipeline calls
            // this again after roads, long after hydrology built its meshes, and clearing it there
            // would delete every river and lake in the world.
            for (var i = waterRoot.transform.childCount - 1; i >= 0; i--)
            {
                var child = waterRoot.transform.GetChild(i).gameObject;
                if (child.name == WorldWaterObjectName
                    || child.name == InlandWaterMeshBuilder.InlandRootName)
                    continue;
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

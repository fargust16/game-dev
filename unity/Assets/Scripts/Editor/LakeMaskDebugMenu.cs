#if UNITY_EDITOR
using GameDev.World.Water;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Visualises lake representation as a <b>filled 2D area</b> (green) vs river corridors (cyan).
    /// If lakes look like thin lines here, the flood mask itself is wrong — not the mesh.
    /// </summary>
    public static class LakeMaskDebugMenu
    {
        private const string DebugRootName = "OverworldLakeMaskDebug";

        [MenuItem("GameDev/Overworld/Debug/Show Lake Filled Masks")]
        public static void ShowLakeFilledMasks()
        {
            Clear();
            var lakes = Object.FindFirstObjectByType<OverworldWaterZones>();
            if (lakes == null || lakes.Zones == null || lakes.Zones.Count == 0)
            {
                Debug.LogWarning("[Water][Debug] No OverworldWaterZones — generate water first.");
                return;
            }

            var root = new GameObject(DebugRootName);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"))
            {
                color = new Color(0.1f, 0.85f, 0.2f, 0.55f),
                name = "LakeMaskDebugMat"
            };
            if (mat.HasProperty("_Cull"))
            {
                mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            }

            var cells = 0;
            foreach (var zone in lakes.Zones)
            {
                var lakeGo = new GameObject(zone.Id + "_filled_mask");
                lakeGo.transform.SetParent(root.transform, false);
                for (var x = 0; x < zone.Width; x++)
                {
                    for (var z = 0; z < zone.Depth; z++)
                    {
                        if (!zone.IsFlooded(x, z))
                        {
                            continue;
                        }

                        cells++;
                        var cx = zone.OriginX + (x + 0.5f) * zone.CellSize;
                        var cz = zone.OriginZ + (z + 0.5f) * zone.CellSize;
                        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        Object.DestroyImmediate(quad.GetComponent<Collider>());
                        quad.name = $"cell_{x}_{z}";
                        quad.transform.SetParent(lakeGo.transform, false);
                        quad.transform.position = new Vector3(cx, zone.WaterLevelY + 0.5f, cz);
                        quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                        quad.transform.localScale = new Vector3(zone.CellSize * 0.95f, zone.CellSize * 0.95f, 1f);
                        quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
                    }
                }

                Debug.Log($"[Water][Debug] {zone.Id}: filledCells={zone.FloodedCellCount} " +
                          $"waterLevelY={zone.WaterLevelY:F2} radius≈{zone.EquivalentRadiusMeters:F1}m");
            }

            Debug.Log($"[Water][Debug] Drew {cells} filled lake cells under '{DebugRootName}'. " +
                      "Must look like solid green blobs, NOT thin lines. Rivers are separate (OverworldRivers).");
            Selection.activeGameObject = root;
        }

        [MenuItem("GameDev/Overworld/Debug/Clear Lake Mask Debug")]
        public static void Clear()
        {
            var root = GameObject.Find(DebugRootName);
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
#endif

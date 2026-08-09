#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameDev.EditorTools
{
    /// <summary>
    /// P0 dress setup: enable URP Decal Renderer Feature on the project renderer asset.
    /// HAZE / store materials are assigned after Asset Store import (see Assets/Art/TownDress/README.md).
    /// </summary>
    public static class TownUrpDressSetup
    {
        private const string RendererPath = "Assets/URP_Renderer.asset";

        [MenuItem("GameDev/Town/Enable URP Decal Feature")]
        public static void EnableUrpDecalFeature()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                Debug.LogError($"[TownDress] Missing UniversalRendererData at {RendererPath}");
                return;
            }

            var features = renderer.rendererFeatures;
            for (var i = 0; i < features.Count; i++)
            {
                if (features[i] is DecalRendererFeature)
                {
                    Debug.Log("[TownDress] Decal renderer feature already present.");
                    Selection.activeObject = renderer;
                    return;
                }
            }

            var feature = ScriptableObject.CreateInstance<DecalRendererFeature>();
            feature.name = "DecalRendererFeature";
            AssetDatabase.AddObjectToAsset(feature, renderer);
            features.Add(feature);
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(feature);
            AssetDatabase.SaveAssets();
            Debug.Log("[TownDress] Added DecalRendererFeature to URP_Renderer. Import HAZE + materials next.");
            Selection.activeObject = renderer;
        }
    }
}
#endif

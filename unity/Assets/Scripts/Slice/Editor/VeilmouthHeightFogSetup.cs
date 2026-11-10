using System.Linq;
using GameDev.Slice.Atmosphere;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Puts the height fog's pass (reference plan R3 step 5; <see cref="HeightFogFeature"/>) on the project's one URP
    /// renderer, once; running it again changes nothing. The pass draws only in a scene whose <c>VeilmouthSky</c> owns
    /// the fog. Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthHeightFogSetup.Install
    /// </summary>
    public static class VeilmouthHeightFogSetup
    {
        public const string RendererPath = "Assets/URP_Renderer.asset";
        public const string ShaderPath = "Assets/Shaders/Slice/HeightFog.shader";

        [MenuItem("GameDev/Slice/Install Height Fog")]
        public static void Install()
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (data == null || shader == null)
            {
                Debug.LogError($"[HeightFog] missing {(data == null ? RendererPath : ShaderPath)}.");
                return;
            }

            var feature = data.rendererFeatures.OfType<HeightFogFeature>().FirstOrDefault();
            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<HeightFogFeature>();
                feature.name = "HeightFog";
                feature.hideFlags |= HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(feature, data);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

                // As URP's renderer editor adds a feature: the list and its map of local ids side by side.
                var serialized = new SerializedObject(data);
                var features = serialized.FindProperty("m_RendererFeatures");
                var map = serialized.FindProperty("m_RendererFeatureMap");
                features.arraySize++;
                features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                serialized.ApplyModifiedProperties();
            }

            feature.SetShader(shader);
            feature.SetActive(true);
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(data);
            data.SetDirty();
            AssetDatabase.SaveAssets();
            Debug.Log($"[HeightFog] the height fog's pass is on {RendererPath}.");
        }

        /// <summary>
        /// The materials in Veilmouth drawn in the transparent queue, particles aside, with how many renderer slots use
        /// each and where it lives: the fog is drawn before them, so they take Unity's distance haze but not the low fog.
        /// Batch: -executeMethod GameDev.Slice.EditorTools.VeilmouthHeightFogSetup.AuditTransparents
        /// </summary>
        public static void AuditTransparents()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            var transparent = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude)
                .Where(r => r.enabled && !(r is ParticleSystemRenderer))
                .SelectMany(r => r.sharedMaterials.Where(m => m != null))
                .Where(m => m.renderQueue >= 2500)
                .GroupBy(m => m)
                .OrderByDescending(g => g.Count())
                .ToList();
            foreach (var g in transparent)
            {
                Debug.Log($"[HeightFog] transparent: '{g.Key.name}' ({g.Key.shader.name}, queue {g.Key.renderQueue}), {g.Count()} slots, {AssetDatabase.GetAssetPath(g.Key)}");
            }

            Debug.Log($"[HeightFog] {transparent.Count} transparent materials.");
        }
    }
}

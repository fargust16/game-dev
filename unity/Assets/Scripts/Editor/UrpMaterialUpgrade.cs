#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Moves Built-in Render Pipeline materials onto URP shaders.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A Built-in <c>Standard</c> material in a URP project is not an error — it is <b>invisible</b>.
    /// URP only executes passes tagged <c>RenderPipeline = UniversalPipeline</c>, and Standard's
    /// passes are tagged <c>ForwardBase</c>, so the renderer walks past the mesh and draws nothing.
    /// There is no console message and no magenta: magenta means a <em>missing</em> shader, while
    /// this shader exists and simply has no pass URP is willing to run.
    /// </para>
    /// <para>
    /// That is why a selected character shows a selection outline over empty ground — the outline is
    /// drawn by the editor from the mesh, independently of the material.
    /// </para>
    /// </remarks>
    public static class UrpMaterialUpgrade
    {
        /// <summary>Built-in shaders this converter knows how to translate.</summary>
        private static readonly HashSet<string> BuiltInShaders = new HashSet<string>
        {
            "Standard",
            "Standard (Specular setup)",
            "Legacy Shaders/Diffuse",
            "Legacy Shaders/Bumped Diffuse",
            "Legacy Shaders/Specular",
            "Legacy Shaders/Bumped Specular",
            "Legacy Shaders/Transparent/Cutout/Diffuse",
            "Legacy Shaders/Transparent/Cutout/Bumped Diffuse",
            "Mobile/Diffuse",
            "Mobile/Bumped Diffuse",
        };

        private const string CharacterFolder = "Assets/Kevin Iglesias";

        [MenuItem("GameDev/Overworld/Player/Fix Character Materials (Built-in -> URP)")]
        public static void UpgradeCharacterMaterialsMenu()
        {
            var upgraded = UpgradeFolder(CharacterFolder);
            Debug.Log(
                upgraded == 0
                    ? $"[URP] No Built-in materials left under '{CharacterFolder}'."
                    : $"[URP] Converted {upgraded} Built-in material(s) under '{CharacterFolder}' to URP/Lit. " +
                      "The character renders now — a Built-in Standard material draws nothing in URP.");
        }

        /// <summary>Converts every Built-in material under <paramref name="folder"/>; returns the count.</summary>
        public static int UpgradeFolder(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogError($"[URP] No folder at '{folder}'.");
                return 0;
            }

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                Debug.LogError("[URP] 'Universal Render Pipeline/Lit' not found — is URP installed?");
                return 0;
            }

            var upgraded = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || material.shader == null || !BuiltInShaders.Contains(material.shader.name))
                {
                    continue;
                }

                Debug.Log($"[URP] '{path}': {material.shader.name} -> Universal Render Pipeline/Lit");
                Upgrade(material, lit);
                EditorUtility.SetDirty(material);
                upgraded++;
            }

            if (upgraded > 0)
            {
                AssetDatabase.SaveAssets();
            }

            return upgraded;
        }

        /// <summary>
        /// Copies the Standard properties URP understands onto the Lit shader.
        /// </summary>
        /// <remarks>
        /// The property names have to be read <b>before</b> the shader swap: assigning
        /// <c>material.shader</c> discards values whose names the new shader does not declare, and
        /// <c>_MainTex</c>/<c>_Color</c>/<c>_Glossiness</c> are exactly the ones URP renames.
        /// </remarks>
        private static void Upgrade(Material material, Shader lit)
        {
            var albedo = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
            var albedoScale = material.HasProperty("_MainTex") ? material.GetTextureScale("_MainTex") : Vector2.one;
            var albedoOffset = material.HasProperty("_MainTex") ? material.GetTextureOffset("_MainTex") : Vector2.zero;
            var tint = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            var metallicMap = material.HasProperty("_MetallicGlossMap") ? material.GetTexture("_MetallicGlossMap") : null;
            var metallic = material.HasProperty("_Metallic") ? material.GetFloat("_Metallic") : 0f;
            var smoothness = ReadSmoothness(material, metallicMap != null);
            var bumpMap = material.HasProperty("_BumpMap") ? material.GetTexture("_BumpMap") : null;
            var bumpScale = material.HasProperty("_BumpScale") ? material.GetFloat("_BumpScale") : 1f;
            var occlusionMap = material.HasProperty("_OcclusionMap") ? material.GetTexture("_OcclusionMap") : null;
            var occlusion = material.HasProperty("_OcclusionStrength") ? material.GetFloat("_OcclusionStrength") : 1f;
            var cutoff = material.HasProperty("_Cutoff") ? material.GetFloat("_Cutoff") : 0.5f;
            var mode = material.HasProperty("_Mode") ? material.GetFloat("_Mode") : 0f;

            // The Standard material keeps an _EmissionColor even with emission switched off, and its
            // _EmissionMap is often a leftover. Carrying either across turns an unlit prop into a
            // glowing one, so emission only survives when the keyword says it was actually on.
            var emissionOn = material.IsKeywordEnabled("_EMISSION");
            var emissionMap = emissionOn && material.HasProperty("_EmissionMap") ? material.GetTexture("_EmissionMap") : null;
            var emissionColor = emissionOn && material.HasProperty("_EmissionColor")
                ? material.GetColor("_EmissionColor")
                : Color.black;

            material.shader = lit;

            material.SetTexture("_BaseMap", albedo);
            material.SetTextureScale("_BaseMap", albedoScale);
            material.SetTextureOffset("_BaseMap", albedoOffset);
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_WorkflowMode", 1f); // Metallic
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Cutoff", cutoff);

            SetMap(material, "_MetallicGlossMap", metallicMap, "_METALLICSPECGLOSSMAP");
            SetMap(material, "_BumpMap", bumpMap, "_NORMALMAP");
            material.SetFloat("_BumpScale", bumpScale);
            SetMap(material, "_OcclusionMap", occlusionMap, "_OCCLUSIONMAP");
            material.SetFloat("_OcclusionStrength", occlusion);

            material.SetTexture("_EmissionMap", emissionMap);
            material.SetColor("_EmissionColor", emissionColor);
            SetKeyword(material, "_EMISSION", emissionOn);
            material.globalIlluminationFlags = emissionOn
                ? MaterialGlobalIlluminationFlags.RealtimeEmissive
                : MaterialGlobalIlluminationFlags.EmissiveIsBlack;

            ApplySurfaceMode(material, mode);
        }

        /// <summary>
        /// Standard's smoothness lives in one of two properties depending on where it is sampled
        /// from; URP keeps a single <c>_Smoothness</c>.
        /// </summary>
        private static float ReadSmoothness(Material material, bool hasMetallicMap)
        {
            if (hasMetallicMap && material.HasProperty("_GlossMapScale"))
            {
                return material.GetFloat("_GlossMapScale");
            }

            return material.HasProperty("_Glossiness") ? material.GetFloat("_Glossiness") : 0.5f;
        }

        private static void ApplySurfaceMode(Material material, float standardMode)
        {
            // Standard: 0 Opaque, 1 Cutout, 2 Fade, 3 Transparent.
            var cutout = Mathf.Approximately(standardMode, 1f);
            var transparent = standardMode >= 2f;

            material.SetFloat("_Surface", transparent ? 1f : 0f);
            material.SetFloat("_AlphaClip", cutout ? 1f : 0f);
            SetKeyword(material, "_ALPHATEST_ON", cutout);
            SetKeyword(material, "_SURFACE_TYPE_TRANSPARENT", transparent);

            if (transparent)
            {
                material.SetFloat("_Blend", 0f); // Alpha
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else
            {
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                material.SetFloat("_ZWrite", 1f);
                material.renderQueue = cutout
                    ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest
                    : (int)UnityEngine.Rendering.RenderQueue.Geometry;
            }
        }

        private static void SetMap(Material material, string property, Texture texture, string keyword)
        {
            material.SetTexture(property, texture);
            SetKeyword(material, keyword, texture != null);
        }

        private static void SetKeyword(Material material, string keyword, bool enabled)
        {
            if (enabled)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }
    }
}
#endif

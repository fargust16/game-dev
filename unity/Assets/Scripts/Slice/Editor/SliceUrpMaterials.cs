using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// URP Lit copies of a pack's built-in materials, kept under <c>Materials/Slice/&lt;pack&gt;/</c>, so the vendor's
    /// files stay as they are (as the Tripo and Dummy copies, 2026-11-05). Standard maps by its own properties: base map
    /// and colour, normal map, metallic map, smoothness, occlusion, emission, and its render mode (opaque, cutout, fade
    /// or transparent). A shader the copy does not know (BK's Refraction glass) becomes clear glass: URP Lit,
    /// transparent, its tint kept. A legacy particle shader (BK's flame and smoke) becomes URP Particles/Unlit, additive
    /// or alpha-blended as its shader was.
    /// </summary>
    public static class SliceUrpMaterials
    {
        private const string Root = "Assets/Materials/Slice";

        /// <summary>Gives every renderer under <paramref name="root"/> URP copies of its built-in materials.</summary>
        public static void Apply(GameObject root, string pack)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterials = r.sharedMaterials.Select(m => Copy(m, pack)).ToArray();
            }
        }

        /// <summary>Batch: <c>-executeMethod GameDev.Slice.EditorTools.SliceUrpMaterials.ConvertBk</c>; logs what it made.</summary>
        [MenuItem("GameDev/Slice/URP Copies of BK_AlchemistHouse Materials")]
        public static void ConvertBk()
        {
            var made = AssetDatabase.FindAssets("t:Material", new[] { "Assets/BK_AlchemistHouse" })
                .Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)))
                .Select(m => (m.shader.name, Copy(m, "BK")))
                .ToList();
            AssetDatabase.SaveAssets();
            foreach (var group in made.GroupBy(x => x.Item1 + " -> " + x.Item2.shader.name))
            {
                Debug.Log($"[SliceUrpMaterials] {group.Count()} x {group.Key}");
            }
        }

        public static Material Copy(Material source, string pack)
        {
            if (source == null || source.shader.name.StartsWith("Universal Render Pipeline") || source.shader.name.StartsWith("Shader Graphs"))
            {
                return source;
            }

            var folder = $"{Root}/{pack}";
            var path = $"{folder}/{source.name}.mat";
            var copy = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (copy != null)
            {
                return copy;
            }

            Directory.CreateDirectory(folder);
            copy = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = source.name };
            var shader = source.shader.name;
            if (shader.Contains("Particles"))
            {
                copy.shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                Particles(source, copy, shader.Contains("Additive"));
            }
            else if (shader.Contains("Refract") || shader.Contains("Glass") || shader.Contains("Transparent"))
            {
                Glass(source, copy);
            }
            else
            {
                Standard(source, copy); // Standard and the legacy shaders share _MainTex, _Color and _BumpMap
            }

            AssetDatabase.CreateAsset(copy, path);
            return copy;
        }

        private static void Standard(Material s, Material c)
        {
            Texture("_MainTex", "_BaseMap", s, c);
            c.SetColor("_BaseColor", s.HasProperty("_Color") ? s.GetColor("_Color") : Color.white);
            if (Texture("_BumpMap", "_BumpMap", s, c))
            {
                c.SetFloat("_BumpScale", s.HasProperty("_BumpScale") ? s.GetFloat("_BumpScale") : 1f);
                c.EnableKeyword("_NORMALMAP");
            }

            if (Texture("_MetallicGlossMap", "_MetallicGlossMap", s, c))
            {
                c.EnableKeyword("_METALLICSPECGLOSSMAP");
                c.SetFloat("_Smoothness", s.HasProperty("_GlossMapScale") ? s.GetFloat("_GlossMapScale") : 0.5f);
            }
            else
            {
                c.SetFloat("_Metallic", s.HasProperty("_Metallic") ? s.GetFloat("_Metallic") : 0f);
                c.SetFloat("_Smoothness", s.HasProperty("_Glossiness") ? s.GetFloat("_Glossiness") : 0.5f);
            }

            if (Texture("_OcclusionMap", "_OcclusionMap", s, c))
            {
                c.EnableKeyword("_OCCLUSIONMAP");
            }

            var emission = s.HasProperty("_EmissionColor") ? s.GetColor("_EmissionColor") : Color.black;
            if (s.IsKeywordEnabled("_EMISSION") && emission.maxColorComponent > 0.001f)
            {
                Texture("_EmissionMap", "_EmissionMap", s, c);
                c.SetColor("_EmissionColor", emission);
                c.EnableKeyword("_EMISSION");
                c.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }

            var mode = s.HasProperty("_Mode") ? Mathf.RoundToInt(s.GetFloat("_Mode")) : 0;
            if (mode == 1)
            {
                c.SetFloat("_AlphaClip", 1f);
                c.SetFloat("_Cutoff", s.HasProperty("_Cutoff") ? s.GetFloat("_Cutoff") : 0.5f);
                c.EnableKeyword("_ALPHATEST_ON");
                c.renderQueue = (int)RenderQueue.AlphaTest;
            }
            else if (mode >= 2)
            {
                Transparent(c);
            }
        }

        private static void Glass(Material s, Material c)
        {
            var tint = s.HasProperty("_Color") ? s.GetColor("_Color") : new Color(0.85f, 0.95f, 1f, 0.25f);
            tint.a = Mathf.Clamp(tint.a, 0.15f, 0.45f);
            c.SetColor("_BaseColor", tint);
            Texture("_MainTex", "_BaseMap", s, c);
            c.SetFloat("_Smoothness", 0.9f);
            Transparent(c);
        }

        private static void Particles(Material s, Material c, bool additive)
        {
            Texture("_MainTex", "_BaseMap", s, c);
            var tint = s.HasProperty("_TintColor") ? s.GetColor("_TintColor") * 2f : Color.white;
            tint.a = Mathf.Clamp01(tint.a);
            c.SetColor("_BaseColor", tint);
            Transparent(c);
            if (additive)
            {
                c.SetFloat("_Blend", 2f);
                c.SetFloat("_DstBlend", (float)BlendMode.One);
            }
        }

        private static void Transparent(Material c)
        {
            c.SetFloat("_Surface", 1f);
            c.SetFloat("_Blend", 0f);
            c.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            c.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            c.SetFloat("_ZWrite", 0f);
            c.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            c.SetOverrideTag("RenderType", "Transparent");
            c.renderQueue = (int)RenderQueue.Transparent;
        }

        private static bool Texture(string from, string to, Material s, Material c)
        {
            if (!s.HasProperty(from) || s.GetTexture(from) == null)
            {
                return false;
            }

            c.SetTexture(to, s.GetTexture(from));
            c.SetTextureScale(to, s.GetTextureScale(from));
            c.SetTextureOffset(to, s.GetTextureOffset(from));
            return true;
        }
    }
}

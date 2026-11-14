using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JBooth.MicroSplat;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The project's own MicroSplat module against a texture's repeat on the slice's terrain
/// (Documentation/Terrain/TERRAIN_POLISH_REPORT.md §2). Core alone has nothing against it, and the vendor's Anti-Tiling
/// module is not bought; this is written from the published techniques, not from that module, and it enters the
/// generated shader through Core's own extension points: a FeatureDescriptor found by reflection, with its
/// properties, its functions, and <see cref="OnPostGeneration"/> for the two places Core has no call for.
/// <para>Three features, each a keyword of its own:</para>
/// <list type="bullet">
/// <item><c>_VEILMACRO</c>, macro variation: the finished albedo is made lighter and darker and a little warmer and
/// cooler by a value noise of three world scales. No texture is read; normals, smoothness and occlusion are not
/// touched.</item>
/// <item><c>_VEILSTOCHASTIC</c>, stochastic tiling: every array read is taken on a triangle grid of patches, each with
/// its own offset and, for half of them, a half turn (Heitz and Neyret 2018, the blend by sharpened weights of
/// Mikkelsen 2022). A half turn negates a tangent-space normal's x and y, so normal maps stay right. One to three
/// reads where there was one.</item>
/// <item><c>_VEILRESAMPLE</c>, distance resampling: where a tile is minified past a set mip the same set is read at a
/// larger tile and takes over, so from afar no tile is small enough to read as a net. One read near and far, two
/// in between.</item>
/// </list>
/// The last two change how the arrays are read and exclude each other. A surface that must keep its pattern (setts,
/// cobbles) is left out by <c>_VeilLayers</c>, a mask of bits by layer.
/// Every default number is an authoring default.
/// </summary>
public sealed class VeilmouthTerrainVariation : FeatureDescriptor
{
    public const string Macro = "_VEILMACRO", Stochastic = "_VEILSTOCHASTIC", Resample = "_VEILRESAMPLE";

    private bool _macro, _stochastic, _resample;

    public override string GetVersion() => MicroSplatShaderGUI.MicroSplatVersion;

    public override string ModuleName() => "Veilmouth Variation";

    public override void InitCompiler(string[] paths)
    {
    }

    public override void Unpack(string[] keywords)
    {
        _macro = keywords.Contains(Macro);
        _stochastic = keywords.Contains(Stochastic);
        _resample = keywords.Contains(Resample) && !_stochastic;
    }

    public override string[] Pack()
    {
        var list = new List<string>();
        if (_macro)
        {
            list.Add(Macro);
        }

        if (_stochastic)
        {
            list.Add(Stochastic);
        }
        else if (_resample)
        {
            list.Add(Resample);
        }

        return list.ToArray();
    }

    public override void DrawFeatureGUI(MicroSplatKeywords keywords)
    {
        _macro = EditorGUILayout.Toggle(new GUIContent("Veilmouth macro variation", "The albedo varied by a noise of three world scales; no texture read."), _macro);
        _stochastic = EditorGUILayout.Toggle(new GUIContent("Veilmouth stochastic tiling", "Every array read on a triangle grid of offset, half-turned patches; up to three reads for one."), _stochastic);
        using (new EditorGUI.DisabledScope(_stochastic))
        {
            _resample = EditorGUILayout.Toggle(new GUIContent("Veilmouth distance resampling", "A larger tile takes over where the texture is minified; two reads in between."), _resample && !_stochastic);
        }
    }

    public override void DrawShaderGUI(MicroSplatShaderGUI shaderGUI, MicroSplatKeywords keywords, Material mat, MaterialEditor materialEditor, MaterialProperty[] props)
    {
        if (!_macro && !_stochastic && !_resample)
        {
            return;
        }

        if (!MicroSplatUtilities.DrawRollup("Veilmouth Variation"))
        {
            return;
        }

        foreach (var name in new[] { "_VeilMacroScale", "_VeilMacroStrength", "_VeilTiling", "_VeilLayers" })
        {
            if (mat.HasProperty(name))
            {
                materialEditor.ShaderProperty(System.Array.Find(props, p => p.name == name), name);
            }
        }
    }

    public override void WriteProperties(string[] features, StringBuilder sb)
    {
        if (features.Contains(Macro))
        {
            sb.AppendLine("      _VeilMacroScale(\"Veilmouth macro scales (m): fine, middle, broad, hue\", Vector) = (13, 47, 173, 97)");
            sb.AppendLine("      _VeilMacroStrength(\"Veilmouth macro strength: fine, middle, broad (stops), hue\", Vector) = (0.10, 0.20, 0.16, 0.05)");
        }

        if (features.Contains(Stochastic) || features.Contains(Resample))
        {
            sb.AppendLine("      _VeilTiling(\"Veilmouth tiling: patches to a tile, blend sharpness, resample from mip, over mips\", Vector) = (3.464, 7, 3.2, 2.0)");
            sb.AppendLine("      _VeilLayers(\"Veilmouth layers mask (bits)\", Float) = 65535");
        }
    }

    public override void WritePerMaterialCBuffer(string[] features, StringBuilder sb)
    {
        if (features.Contains(Macro))
        {
            sb.AppendLine("      float4 _VeilMacroScale;");
            sb.AppendLine("      float4 _VeilMacroStrength;");
        }

        if (features.Contains(Stochastic) || features.Contains(Resample))
        {
            sb.AppendLine("      float4 _VeilTiling;");
            sb.AppendLine("      float _VeilLayers;");
        }
    }

    public override void WriteSharedFunctions(string[] features, StringBuilder sb)
    {
        if (features.Contains(Macro))
        {
            sb.AppendLine(MacroCode);
        }

        if (features.Contains(Stochastic))
        {
            sb.AppendLine(StochasticCode);
        }
        else if (features.Contains(Resample))
        {
            sb.AppendLine(ResampleCode);
        }
    }

    public override void WriteFunctions(string[] features, StringBuilder sb)
    {
    }

    public override void ComputeSampleCounts(string[] features, ref int arraySampleCount, ref int textureSampleCount, ref int maxSamples, ref int tessellationSamples, ref int depTexReadLevel)
    {
        if (features.Contains(Stochastic))
        {
            arraySampleCount *= 3;
        }
        else if (features.Contains(Resample))
        {
            arraySampleCount *= 2;
        }
    }

    /// <summary>
    /// The two places Core has no call for. The array reads all go through two macros (microsplat_shared.txt), which
    /// are pointed at this module's read; the macro variation is laid on the finished albedo just before Core hands
    /// it over (microsplat_terrain_body.txt). A place not found is an error, never a silent pass.
    /// </summary>
    public override void OnPostGeneration(ref StringBuilder sb, string[] features, string name, string baseName = null, MicroSplatShaderGUI.MicroSplatCompiler.AuxShader auxShader = null)
    {
        var macro = features.Contains(Macro);
        var read = features.Contains(Stochastic) || features.Contains(Resample);
        if (!macro && !read)
        {
            return;
        }

        var text = sb.ToString();
        if (read)
        {
            text = Replace(text, @"#define MICROSPLAT_SAMPLE_DIFFUSE\(u, cl, l\) MICROSPLAT_SAMPLE\(_Diffuse, u, l\)",
                "#define MICROSPLAT_SAMPLE_DIFFUSE(u, cl, l) VeilRead(TEXTURE2D_ARRAY_ARGS(_Diffuse, sampler_Diffuse), u, l, _VeilTiling, _VeilLayers, 0.0)", name);
            text = Replace(text, @"#define MICROSPLAT_SAMPLE_NORMAL\(u, cl, l\) MICROSPLAT_SAMPLE\(_NormalSAO, u, l\)",
                "#define MICROSPLAT_SAMPLE_NORMAL(u, cl, l) VeilRead(TEXTURE2D_ARRAY_ARGS(_NormalSAO, sampler_NormalSAO), u, l, _VeilTiling, _VeilLayers, 1.0)", name);
        }

        if (macro)
        {
            text = Replace(text, @"o\.Albedo = albedo\.rgb;(\s*)o\.Height = albedo\.a;",
                "VeilMacroVariation(albedo, i.worldPos, _VeilMacroScale, _VeilMacroStrength);$1o.Albedo = albedo.rgb;$1o.Height = albedo.a;", name);
        }

        sb = new StringBuilder(text);
    }

    private static string Replace(string text, string pattern, string with, string shader)
    {
        var regex = new Regex(pattern);
        if (!regex.IsMatch(text))
        {
            throw new System.InvalidOperationException($"Veilmouth Variation: MicroSplat's shader '{shader}' has no place matching {pattern}; Core changed, and this module must follow it.");
        }

        return regex.Replace(text, with);
    }

    private const string MacroCode = @"
      // Veilmouth macro variation: value noise at three world scales on the albedo's brightness, a fourth on its hue.
      float VeilHash(float2 p)
      {
         float3 p3 = frac(float3(p.xyx) * 0.1031);
         p3 += dot(p3, p3.yzx + 33.33);
         return frac((p3.x + p3.y) * p3.z);
      }

      float VeilValue(float2 p)
      {
         float2 c = floor(p);
         float2 f = frac(p);
         f = f * f * (3.0 - 2.0 * f);
         float a = VeilHash(c);
         float b = VeilHash(c + float2(1, 0));
         float d = VeilHash(c + float2(0, 1));
         float e = VeilHash(c + float2(1, 1));
         return lerp(lerp(a, b, f.x), lerp(d, e, f.x), f.y) * 2.0 - 1.0;
      }

      void VeilMacroVariation(inout half4 albedo, float3 worldPos, float4 scales, float4 strength)
      {
         float2 p = worldPos.xz;
         // Each scale is two octaves turned against each other, so no lattice shows.
         float fine = VeilValue(p / scales.x) + 0.5 * VeilValue(p.yx / (scales.x * 0.47) + 19.1);
         float middle = VeilValue(p / scales.y + 7.3) + 0.5 * VeilValue(p.yx / (scales.y * 0.43) + 3.7);
         float broad = VeilValue(p / scales.z + 31.7) + 0.5 * VeilValue(p.yx / (scales.z * 0.51) + 11.9);
         float stops = (fine * strength.x + middle * strength.y + broad * strength.z) / 1.5;
         float hue = (VeilValue(p / scales.w + 53.1) + 0.5 * VeilValue(p.yx / (scales.w * 0.37) + 71.3)) / 1.5 * strength.w;
         albedo.rgb *= exp2(stops) * half3(1.0 + hue, 1.0, 1.0 - 1.4 * hue);
      }
";

    private const string StochasticCode = @"
      // Veilmouth stochastic tiling: a triangle grid of patches, each read with its own offset and, for half, a half turn.
      float3 VeilHash3(float2 p)
      {
         float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
         p3 += dot(p3, p3.yxz + 33.33);
         return frac((p3.xxy + p3.yzz) * p3.zyx);
      }

      half4 VeilTap(TEXTURE2D_ARRAY_PARAM(tex, ss), float3 u, float4 l, float3 h, float isNormal)
      {
         float turned = step(0.5, h.z);
         float2 uv = lerp(u.xy, -u.xy, turned) + h.xy;
         half4 s = SAMPLE_TEXTURE2D_ARRAY_GRAD(tex, ss, uv, u.z, l.xy, l.zw);
         // MicroSplat keeps a normal's x and y in g and a; a half turn negates both.
         s.ga = lerp(s.ga, 1.0 - s.ga, turned * isNormal);
         return s;
      }

      half4 VeilRead(TEXTURE2D_ARRAY_PARAM(tex, ss), float3 u, float4 l, float4 tiling, float layers, float isNormal)
      {
         if (fmod(floor(layers / exp2(u.z)), 2.0) < 0.5)
         {
            return SAMPLE_TEXTURE2D_ARRAY_GRAD(tex, ss, u.xy, u.z, l.xy, l.zw);
         }

         float2 skewed = mul(float2x2(1.0, 0.0, -0.57735027, 1.15470054), u.xy * tiling.x);
         float2 base = floor(skewed);
         float3 t = float3(frac(skewed), 0.0);
         t.z = 1.0 - t.x - t.y;
         float3 w;
         float2 v1, v2, v3;
         if (t.z > 0.0)
         {
            w = float3(t.z, t.y, t.x);
            v1 = base; v2 = base + float2(0, 1); v3 = base + float2(1, 0);
         }
         else
         {
            w = float3(-t.z, 1.0 - t.y, 1.0 - t.x);
            v1 = base + float2(1, 1); v2 = base + float2(1, 0); v3 = base + float2(0, 1);
         }

         w = pow(w, tiling.y);
         w /= (w.x + w.y + w.z);
         float2 seed = float2(u.z * 17.0, u.z * 5.0);
         half4 result = 0;
         if (w.x > 0.01) { result += w.x * VeilTap(TEXTURE2D_ARRAY_ARGS(tex, ss), u, l, VeilHash3(v1 + seed), isNormal); }
         if (w.y > 0.01) { result += w.y * VeilTap(TEXTURE2D_ARRAY_ARGS(tex, ss), u, l, VeilHash3(v2 + seed), isNormal); }
         if (w.z > 0.01) { result += w.z * VeilTap(TEXTURE2D_ARRAY_ARGS(tex, ss), u, l, VeilHash3(v3 + seed), isNormal); }
         return result;
      }
";

    private const string ResampleCode = @"
      // Veilmouth distance resampling: where a tile is minified past tiling.z mips, the same set at 3.7 times its tile.
      half4 VeilRead(TEXTURE2D_ARRAY_PARAM(tex, ss), float3 u, float4 l, float4 tiling, float layers, float isNormal)
      {
         float t = 0.0;
         if (fmod(floor(layers / exp2(u.z)), 2.0) > 0.5)
         {
            float2 size;
            float count;
            tex.GetDimensions(size.x, size.y, count);
            float footprint = max(dot(l.xy, l.xy), dot(l.zw, l.zw)) * size.x * size.x;
            t = saturate((0.5 * log2(max(footprint, 1e-12)) - tiling.z) / tiling.w);
         }

         half4 near = 0, far = 0;
         if (t < 0.999) { near = SAMPLE_TEXTURE2D_ARRAY_GRAD(tex, ss, u.xy, u.z, l.xy, l.zw); }
         if (t > 0.001) { far = SAMPLE_TEXTURE2D_ARRAY_GRAD(tex, ss, u.xy * 0.27 + float2(0.37, 0.61) * (u.z + 1.0), u.z, l.xy * 0.27, l.zw * 0.27); }
         return lerp(near, far, t);
      }
";
}

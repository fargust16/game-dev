#if UNITY_EDITOR
using System.Collections.Generic;
using Pinwheel.Vista;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Reads the detail density map at the exact moment Vista hands it to the tile.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every other link in the ground-cover chain has been measured and is sound: the template is
    /// valid, Unity accepts the prototype, the green weight the density mask is thresholded against
    /// reaches 1.0, the output shader exists, and the CPU read-back is synchronous. The one value
    /// still unmeasured is the content of the render texture itself, and it sits on the boundary
    /// between the graph and Vista's writer — which is exactly the boundary the answer depends on:
    /// </para>
    /// <list type="bullet">
    /// <item>a non-zero texture here means the graph is fine and Vista's CPU write is losing it;</item>
    /// <item>a zero texture means the Detail Density Output node produced black despite a healthy
    /// green mask, and the fault is upstream in the graph.</item>
    /// </list>
    /// <para>
    /// <c>VistaManager.detailDensityPopulated</c> fires synchronously with the live textures, so
    /// this reads them while they are still valid. It only reads — the density map belongs to Vista,
    /// and writing detail layers here would be building the second vegetation system this project
    /// exists to avoid.
    /// </para>
    /// </remarks>
    [InitializeOnLoad]
    public static class VistaDetailDensityProbe
    {
        private const string EnabledKey = "GameDev.VistaDetailDensityProbe";

        static VistaDetailDensityProbe()
        {
            VistaManager.detailDensityPopulated -= OnDetailDensityPopulated;
            VistaManager.detailDensityPopulated += OnDetailDensityPopulated;
        }

        /// <summary>Off by default: this logs once per template per tile during every bake.</summary>
        private static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledKey, false);
            set => EditorPrefs.SetBool(EnabledKey, value);
        }

        [MenuItem("GameDev/Overworld/Vista/Probe Detail Density Maps")]
        private static void ToggleMenu()
        {
            Enabled = !Enabled;
            Debug.Log(
                Enabled
                    ? "[DensityProbe] On. Generate again — every detail density map will be sampled as " +
                      "Vista hands it over."
                    : "[DensityProbe] Off.");
        }

        [MenuItem("GameDev/Overworld/Vista/Probe Detail Density Maps", validate = true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked("GameDev/Overworld/Vista/Probe Detail Density Maps", Enabled);
            return true;
        }

        private static void OnDetailDensityPopulated(
            VistaManager sender, ITile tile, List<DetailTemplate> templates, List<RenderTexture> densityMaps)
        {
            if (!Enabled || templates == null || densityMaps == null)
            {
                return;
            }

            for (var i = 0; i < densityMaps.Count && i < templates.Count; i++)
            {
                var name = templates[i] != null ? templates[i].name : "<null template>";
                var map = densityMaps[i];
                if (map == null)
                {
                    Debug.LogWarning($"[DensityProbe] '{name}': density map is NULL at populate time.");
                    continue;
                }

                if (!map.IsCreated())
                {
                    Debug.LogWarning($"[DensityProbe] '{name}': density map render texture is not created.");
                    continue;
                }

                Debug.Log($"[DensityProbe] '{name}': {Describe(map)}");
            }
        }

        private static string Describe(RenderTexture map)
        {
            var readable = new Texture2D(map.width, map.height, TextureFormat.RFloat, false);
            try
            {
                var previous = RenderTexture.active;
                RenderTexture.active = map;
                readable.ReadPixels(new Rect(0, 0, map.width, map.height), 0, 0);
                RenderTexture.active = previous;

                var pixels = readable.GetPixelData<float>(0);
                var min = float.MaxValue;
                var max = float.MinValue;
                var sum = 0.0;
                var nonZero = 0;
                for (var i = 0; i < pixels.Length; i++)
                {
                    var v = pixels[i];
                    sum += v;
                    if (v < min)
                    {
                        min = v;
                    }

                    if (v > max)
                    {
                        max = v;
                    }

                    if (v > 0f)
                    {
                        nonZero++;
                    }
                }

                var coverage = 100f * nonZero / Mathf.Max(1, pixels.Length);
                return
                    $"{map.width}x{map.height} {map.format} " +
                    $"min={min:F3} avg={sum / Mathf.Max(1, pixels.Length):F3} max={max:F3} " +
                    $"nonZero={coverage:F1}%";
            }
            finally
            {
                Object.DestroyImmediate(readable);
            }
        }
    }
}
#endif

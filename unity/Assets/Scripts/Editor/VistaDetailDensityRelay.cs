#if UNITY_EDITOR
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.UnityTerrain;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Lands Vista's detail density map on the terrain, because Vista's own writer does not in the
    /// editor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is a vendor-bug workaround, not a second vegetation system.</b> Every value written
    /// here is Vista's: the density map comes from the graph's Detail Density Output, the base
    /// density from the Detail Template, and the arithmetic is copied from
    /// <c>TerrainTile.FillDensityArray</c> including its stochastic rounding. Nothing decides
    /// <em>where</em> grass goes; that stays entirely in the terrain graph. Delete this file the day
    /// Vista's own path works.
    /// </para>
    /// <para>
    /// What was measured, in order, before writing any of it: the detail template is valid; Unity
    /// accepts the prototype; the painted green weight the density mask is thresholded against
    /// reaches 1.0; the output shader exists; the read-back is synchronous; the detail resolution is
    /// pinned before the bake so nothing is destroyed afterwards. And finally the probe read the
    /// render texture at the moment Vista hands it to the tile —
    /// <c>512x512 RFloat max=1.000 nonZero=40%</c>. Correct data goes in, nothing comes out.
    /// </para>
    /// <para>
    /// Vista writes it through <c>PopulateLayerDensityProgressive</c>, one patch row per editor
    /// frame, from a <c>Texture2D</c> that the parent coroutine destroys as soon as its own wait
    /// returns — and that wait is <c>yield return c.coroutine</c> on an <c>EditorCoroutine</c>, which
    /// is neither an <c>IEnumerator</c> nor a <c>CustomYieldInstruction</c>, so it resumes after a
    /// single tick instead of when the writer finishes. Writing the whole map in one call sidesteps
    /// the frame budget the race depends on.
    /// </para>
    /// </remarks>
    [InitializeOnLoad]
    public static class VistaDetailDensityRelay
    {
        /// <summary>Density accumulated per tile per template, merged the way Vista merges: additively.</summary>
        private static readonly Dictionary<Terrain, Dictionary<DetailTemplate, float[]>> s_captured =
            new Dictionary<Terrain, Dictionary<DetailTemplate, float[]>>();

        private static int s_resolution;

        private const string EnabledKey = "GameDev.VistaDetailDensityRelay";

        static VistaDetailDensityRelay()
        {
            VistaManager.detailDensityPopulated -= OnDetailDensityPopulated;
            VistaManager.detailDensityPopulated += OnDetailDensityPopulated;
        }

        /// <summary>
        /// On by default, because with it off there is no ground cover at all.
        /// </summary>
        /// <remarks>
        /// Turn it off to test whether Vista's own writer works for a given detail template — that
        /// is the only way to tell a content problem from the writer problem, and it is the reason
        /// this is a switch rather than something permanently wired in.
        /// </remarks>
        public static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledKey, true);
            set => EditorPrefs.SetBool(EnabledKey, value);
        }

        [MenuItem("GameDev/Overworld/Vista/Detail Density Relay (workaround)")]
        private static void ToggleMenu()
        {
            Enabled = !Enabled;
            Debug.Log(
                Enabled
                    ? "[DetailRelay] On — the density map Vista produces is written directly."
                    : "[DetailRelay] Off — ground cover now depends entirely on Vista's own writer.");
        }

        [MenuItem("GameDev/Overworld/Vista/Detail Density Relay (workaround)", validate = true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked("GameDev/Overworld/Vista/Detail Density Relay (workaround)", Enabled);
            return true;
        }

        /// <summary>Drops anything captured by an earlier bake. Call before generating.</summary>
        public static void Reset()
        {
            s_captured.Clear();
            s_resolution = 0;
        }

        private static void OnDetailDensityPopulated(
            VistaManager sender, ITile tile, List<DetailTemplate> templates, List<RenderTexture> densityMaps)
        {
            if (!(tile is TerrainTile terrainTile) || terrainTile.terrain == null || templates == null)
            {
                return;
            }

            var terrain = terrainTile.terrain;
            for (var i = 0; i < densityMaps.Count && i < templates.Count; i++)
            {
                var template = templates[i];
                var map = densityMaps[i];
                if (template == null || map == null || !map.IsCreated())
                {
                    continue;
                }

                Accumulate(terrain, template, map);
            }
        }

        private static void Accumulate(Terrain terrain, DetailTemplate template, RenderTexture map)
        {
            var pixels = ReadFloats(map);
            if (pixels == null)
            {
                return;
            }

            s_resolution = map.width;
            if (!s_captured.TryGetValue(terrain, out var perTemplate))
            {
                perTemplate = new Dictionary<DetailTemplate, float[]>();
                s_captured[terrain] = perTemplate;
            }

            if (!perTemplate.TryGetValue(template, out var total))
            {
                perTemplate[template] = pixels;
                return;
            }

            // Two graphs can output the same template — the island and the massif both did — and
            // Vista merges those additively. Same here, so a tile that both biomes touch is not
            // decided by whichever ran last.
            var count = Mathf.Min(total.Length, pixels.Length);
            for (var i = 0; i < count; i++)
            {
                total[i] = Mathf.Min(1f, total[i] + pixels[i]);
            }
        }

        private static float[] ReadFloats(RenderTexture map)
        {
            if (map.width != map.height)
            {
                return null;
            }

            var readable = new Texture2D(map.width, map.height, TextureFormat.RFloat, false);
            try
            {
                var previous = RenderTexture.active;
                RenderTexture.active = map;
                readable.ReadPixels(new Rect(0, 0, map.width, map.height), 0, 0);
                RenderTexture.active = previous;
                return readable.GetPixelData<float>(0).ToArray();
            }
            finally
            {
                Object.DestroyImmediate(readable);
            }
        }

        [MenuItem("GameDev/Overworld/Vista/Apply Captured Detail Density")]
        private static void ApplyMenu()
        {
            var written = Apply();
            Debug.Log(
                written == 0
                    ? "[DetailRelay] Nothing captured — generate first."
                    : $"[DetailRelay] Wrote {written} detail layer(s) from the maps Vista produced.");
        }

        /// <summary>Writes every captured density map into its terrain. Returns the layer count written.</summary>
        public static int Apply()
        {
            if (!Enabled)
            {
                return 0;
            }

            var written = 0;
            foreach (var pair in s_captured)
            {
                var terrain = pair.Key;
                if (terrain == null || terrain.terrainData == null)
                {
                    continue;
                }

                var data = terrain.terrainData;
                if (data.detailResolution != s_resolution)
                {
                    Debug.LogWarning(
                        $"[DetailRelay] '{terrain.name}': detail resolution {data.detailResolution} does " +
                        $"not match the density map ({s_resolution}); skipping rather than guessing a rescale.");
                    continue;
                }

                var wroteAny = false;
                foreach (var entry in pair.Value)
                {
                    if (WriteLayer(data, entry.Key, entry.Value))
                    {
                        written++;
                        wroteAny = true;
                    }
                }

                if (wroteAny)
                {
                    // The TerrainData is a saved asset, so an in-memory change that is never marked
                    // dirty is discarded the next time the asset is reloaded. That is the second half
                    // of why Vista's own path lost the grass and kept the trees: trees are written
                    // synchronously, before the bake marks the data dirty, and detail layers land
                    // frames later, after it.
                    EditorUtility.SetDirty(data);
                }
            }

            return written;
        }

        private static bool WriteLayer(TerrainData data, DetailTemplate template, float[] density)
        {
            var prototypes = data.detailPrototypes;
            var layer = System.Array.FindIndex(prototypes, p => p.prototype == template.prefab);
            if (layer < 0)
            {
                Debug.LogWarning(
                    $"[DetailRelay] No detail prototype matches template '{template.name}' — Vista did " +
                    "not register it, so there is nothing to write into.");
                return false;
            }

            var res = s_resolution;
            var counts = new int[res, res];

            // Copied from TerrainTile.FillDensityArray, stochastic rounding included: a density of
            // 0.4 should scatter instances across four cells in ten, not round every cell to zero.
            Random.InitState(layer * 12345);
            var baseDensity = template.density;
            for (var y = 0; y < res; y++)
            {
                var row = y * res;
                for (var x = 0; x < res; x++)
                {
                    var value = density[row + x] * baseDensity;
                    var whole = (int)value;
                    if (value - whole > Random.value)
                    {
                        whole++;
                    }

                    counts[y, x] = whole;
                }
            }

            data.SetDetailLayer(0, 0, layer, counts);
            return true;
        }
    }
}
#endif

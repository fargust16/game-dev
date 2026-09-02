using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World
{
    /// <summary>
    /// Shared URP/Lit materials for generated placeholder geometry, keyed by the only two things
    /// that ever differ between them: tint and render queue.
    /// </summary>
    /// <remarks>
    /// The settlement and POI spawners used to call <c>new Material(...)</c> once per piece. One
    /// measured scene held 1 799 inline materials with 62 distinct definitions — one shader, one
    /// keyword set, one texture set, one float set, one set of GI flags, and only `_BaseColor` and
    /// `m_CustomRenderQueue` varying — and each of those 119-line blocks was serialised into the
    /// scene, 214 081 lines in all, better than half the file.
    ///
    /// Sharing is safe because nothing mutates these materials once they reach a renderer: every
    /// property write happens inside <see cref="Create"/>, before the caller assigns
    /// <c>sharedMaterial</c>. Renderers never go through <c>renderer.material</c>, so Unity does not
    /// clone them either.
    ///
    /// Lifetime is one generation. <see cref="Reset"/> runs where a generation step clears its
    /// previous output, and every lookup re-checks liveness regardless: a Material whose scene was
    /// closed, or which Unity collected as unused, compares equal to null, and the cache replaces it
    /// rather than handing back a destroyed object.
    /// </remarks>
    public static class GeneratedTintMaterials
    {
        /// <summary>Leave the shader's own queue alone — serialises as `m_CustomRenderQueue: -1`.</summary>
        public const int ShaderRenderQueue = -1;

        private static readonly Dictionary<(Color Tint, int RenderQueue), Material> Cache = new();

        /// <summary>How many distinct materials the current generation has needed. For tests.</summary>
        public static int CachedCount => Cache.Count;

        /// <summary>Drop everything, so the next generation builds its own materials.</summary>
        public static void Reset() => Cache.Clear();

        public static Material Get(Color tint, int renderQueue = ShaderRenderQueue)
        {
            var key = (tint, renderQueue);
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var created = Create(tint, renderQueue);
            Cache[key] = created;
            return created;
        }

        private static Material Create(Color tint, int renderQueue)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            var mat = shader != null ? new Material(shader) : new Material(Shader.Find("Hidden/InternalErrorShader"));
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", tint);
            }

            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", tint);
            }
            else
            {
                mat.color = tint;
            }

            if (renderQueue != ShaderRenderQueue)
            {
                mat.renderQueue = renderQueue;
            }

            return mat;
        }
    }
}

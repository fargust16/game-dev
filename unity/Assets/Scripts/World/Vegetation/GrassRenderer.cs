using UnityEngine;

namespace GameDev.World.Vegetation
{
    /// <summary>
    /// Batched <see cref="Graphics.RenderMeshInstanced"/> draws (max 1023 per call).
    /// No shadows for the coverage carpet (perf budget).
    /// </summary>
    public sealed class GrassRenderer
    {
        public const int BatchSize = 1023;

        private readonly Mesh mesh;
        private readonly Material material;
        private readonly Matrix4x4[] batch = new Matrix4x4[BatchSize];
        private readonly MaterialPropertyBlock props = new MaterialPropertyBlock();
        private readonly RenderParams renderParams;

        public GrassRenderer(Mesh mesh, Material material)
        {
            this.mesh = mesh;
            this.material = material;
            renderParams = new RenderParams(material)
            {
                shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                receiveShadows = false,
                lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off,
                matProps = props
            };
        }

        public void Draw(GrassChunk chunk, float lodSkipChance, System.Random lodRng)
        {
            if (chunk == null || chunk.Count == 0 || mesh == null || material == null)
            {
                return;
            }

            var matrices = chunk.Matrices;
            var count = chunk.Count;
            var written = 0;

            for (var i = 0; i < count; i++)
            {
                if (lodSkipChance > 0f && lodRng != null && lodRng.NextDouble() < lodSkipChance)
                {
                    continue;
                }

                batch[written++] = matrices[i];
                if (written >= BatchSize)
                {
                    Graphics.RenderMeshInstanced(renderParams, mesh, 0, batch, written);
                    written = 0;
                }
            }

            if (written > 0)
            {
                Graphics.RenderMeshInstanced(renderParams, mesh, 0, batch, written);
            }
        }

        public void DrawAll(System.Collections.Generic.IReadOnlyList<GrassChunk> chunks)
        {
            if (chunks == null)
            {
                return;
            }

            for (var c = 0; c < chunks.Count; c++)
            {
                Draw(chunks[c], 0f, null);
            }
        }
    }
}

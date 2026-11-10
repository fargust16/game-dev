using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace GameDev.Slice.Atmosphere
{
    /// <summary>
    /// What the height fog draws, set by <see cref="VeilmouthSky"/> by the clock (reference plan R3 step 5): the fog's
    /// shader globals, and the lit gas lamps whose halos it shows. The fog draws only while its owner, the scene's sky,
    /// lives, so another scene never shows Veilmouth's fog.
    /// </summary>
    public static class HeightFog
    {
        /// <summary>The most lamps one camera's fog shows halos for: the nearest ahead of the eye.</summary>
        public const int MaxLamps = 16;

        /// <summary>How far a lamp's halo is looked for, m: beyond it the haze has taken it (authoring default).</summary>
        public const float LampReach = 140f;

        public static readonly int ColourId = Shader.PropertyToID("_HeightFogColour");
        public static readonly int ParamsId = Shader.PropertyToID("_HeightFogParams");
        public static readonly int SunId = Shader.PropertyToID("_HeightFogSun");
        public static readonly int SunColourId = Shader.PropertyToID("_HeightFogSunColour");
        public static readonly int DriftId = Shader.PropertyToID("_HeightFogDrift");
        public static readonly int LampParamsId = Shader.PropertyToID("_HeightFogLampParams");
        public static readonly int ShaftParamsId = Shader.PropertyToID("_HeightFogShaftParams");

        private static readonly List<(Vector3 Position, Color Colour)> Lamps = new();
        private static readonly float[] Distances = new float[MaxLamps];

        /// <summary>The fog draws while this lives.</summary>
        public static Object Owner { get; private set; }

        public static bool Active => Owner != null;

        /// <summary>Starts the fog for <paramref name="owner"/>'s scene.</summary>
        public static void Own(Object owner) => Owner = owner;

        /// <summary>The lit lamps' glass and light, linear colour times intensity; none by day.</summary>
        public static void SetLamps(IEnumerable<(Vector3 Position, Color Colour)> lamps)
        {
            Lamps.Clear();
            if (lamps != null)
            {
                Lamps.AddRange(lamps);
            }
        }

        /// <summary>The lit lamps nearest <paramref name="eye"/> and not behind it, nearest first; returns how many.</summary>
        public static int Nearest(Vector3 eye, Vector3 forward, Vector4[] positions, Vector4[] colours)
        {
            var count = 0;
            var distances = Distances;
            foreach (var (position, colour) in Lamps)
            {
                var to = position - eye;
                var far = to.sqrMagnitude;
                if (far > LampReach * LampReach || Vector3.Dot(to, forward) < -4f)
                {
                    continue; // a lamp just behind the eye still lights the fog ahead of it
                }

                var at = count < MaxLamps ? count++ : MaxLamps;
                if (at == MaxLamps)
                {
                    if (far >= distances[MaxLamps - 1])
                    {
                        continue;
                    }

                    at = MaxLamps - 1;
                }

                while (at > 0 && distances[at - 1] > far)
                {
                    distances[at] = distances[at - 1];
                    positions[at] = positions[at - 1];
                    colours[at] = colours[at - 1];
                    at--;
                }

                distances[at] = far;
                positions[at] = position;
                colours[at] = colour;
            }

            return count;
        }
    }

    /// <summary>
    /// The height fog's pass (reference plan R3 step 5): one full-screen draw after the opaque world and the sky that reads the
    /// picture and the depth and writes the fogged picture, with the nearest lamps' halos. Cameras for previews and
    /// reflections, orthographic plan views and scenes without Veilmouth's sky are left as they are.
    /// </summary>
    public sealed class HeightFogFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader shader;

        private Material _material;
        private HeightFogPass _pass;

        /// <summary>The fog's shader, kept by reference so a build includes it.</summary>
        public Shader Shader => shader;

        /// <summary>Editor setup: the fog's shader.</summary>
        public void SetShader(Shader fog) => shader = fog;

        public override void Create()
        {
            if (shader == null)
            {
                shader = Shader.Find("Hidden/GameDev/Slice/HeightFog");
            }

            _pass = new HeightFogPass { renderPassEvent = RenderPassEvent.BeforeRenderingTransparents };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            if (!HeightFog.Active || shader == null || camera.orthographic
                || camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection)
            {
                return;
            }

            if (_material == null)
            {
                _material = CoreUtils.CreateEngineMaterial(shader);
            }

            _pass.Setup(_material);
            _pass.ConfigureInput(ScriptableRenderPassInput.Depth);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_material);
            _material = null;
        }

        private sealed class HeightFogPass : ScriptableRenderPass
        {
            private static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
            private static readonly int BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
            private static readonly int LampsId = Shader.PropertyToID("_HeightFogLamps");
            private static readonly int LampColoursId = Shader.PropertyToID("_HeightFogLampColours");
            private static readonly int LampCountId = Shader.PropertyToID("_HeightFogLampCount");
            private static readonly int ShaftsId = Shader.PropertyToID("_HeightFogShafts");
            private static readonly int ShaftFadeId = Shader.PropertyToID("_HeightFogShaftFade");

            private readonly MaterialPropertyBlock _block = new();
            private readonly MaterialPropertyBlock _shaftBlock = new();
            private Material _material;

            public HeightFogPass()
            {
                profilingSampler = new ProfilingSampler("Height Fog");
            }

            public void Setup(Material material) => _material = material;

            private sealed class PassData
            {
                internal Material Material;
                internal MaterialPropertyBlock Block;
                internal TextureHandle Source;
                internal TextureHandle Shafts;
                internal float Fade;
                internal readonly Vector4[] Lamps = new Vector4[HeightFog.MaxLamps];
                internal readonly Vector4[] Colours = new Vector4[HeightFog.MaxLamps];
                internal int Count;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                if (resources.isActiveTargetBackBuffer || !resources.cameraDepthTexture.IsValid())
                {
                    return;
                }

                var source = resources.cameraColor;
                var description = renderGraph.GetTextureDesc(source);
                description.name = "_HeightFogColour";
                description.clearBuffer = false;
                var destination = renderGraph.CreateTexture(description);
                var eye = cameraData.camera.transform;
                var shafts = Shafts(renderGraph, resources, cameraData, description, eye.forward, out var fade);

                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Height Fog", out var data, profilingSampler))
                {
                    data.Material = _material;
                    data.Block = _block;
                    data.Source = source;
                    data.Shafts = shafts;
                    data.Fade = fade;
                    data.Count = HeightFog.Nearest(eye.position, eye.forward, data.Lamps, data.Colours);
                    builder.UseTexture(source);
                    builder.UseTexture(resources.cameraDepthTexture);
                    builder.UseTexture(shafts);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    builder.SetRenderFunc(static (PassData d, RasterGraphContext context) =>
                    {
                        d.Block.Clear();
                        d.Block.SetTexture(BlitTextureId, d.Source);
                        d.Block.SetVector(BlitScaleBiasId, new Vector4(1f, 1f, 0f, 0f));
                        d.Block.SetTexture(ShaftsId, d.Shafts);
                        d.Block.SetFloat(ShaftFadeId, d.Fade);
                        d.Block.SetVectorArray(LampsId, d.Lamps);
                        d.Block.SetVectorArray(LampColoursId, d.Colours);
                        d.Block.SetFloat(LampCountId, d.Count);
                        context.cmd.DrawProcedural(Matrix4x4.identity, d.Material, 0, MeshTopology.Triangles, 3, 1, d.Block);
                    });
                }

                resources.cameraColor = destination;
            }

            /// <summary>
            /// The light's shafts at half size (reference plan R3 step 6), drawn only while the sun or the moon stands
            /// ahead of the eye and the clock gives them strength; otherwise black. <paramref name="fade"/> takes them
            /// out as the light turns away from the eye.
            /// </summary>
            private TextureHandle Shafts(RenderGraph renderGraph, UniversalResourceData resources, UniversalCameraData cameraData,
                TextureDesc colour, Vector3 forward, out float fade)
            {
                var sun = Shader.GetGlobalVector(HeightFog.SunId);
                var strength = Shader.GetGlobalVector(HeightFog.ShaftParamsId).x;
                fade = strength > 0f ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 0.45f, Vector3.Dot(forward, sun))) : 0f;
                if (fade <= 0f)
                {
                    return renderGraph.defaultResources.blackTexture;
                }

                var description = colour;
                description.name = "_HeightFogShafts";
                description.sizeMode = TextureSizeMode.Explicit;
                description.width = Mathf.Max(1, cameraData.cameraTargetDescriptor.width / 2);
                description.height = Mathf.Max(1, cameraData.cameraTargetDescriptor.height / 2);
                description.format = GraphicsFormat.R16_SFloat;
                description.msaaSamples = MSAASamples.None;
                description.depthBufferBits = DepthBits.None;
                description.useMipMap = false;
                description.clearBuffer = false;
                var shafts = renderGraph.CreateTexture(description);

                using var builder = renderGraph.AddRasterRenderPass<ShaftData>("Height Fog Shafts", out var data, profilingSampler);
                data.Material = _material;
                data.Block = _shaftBlock;
                builder.UseTexture(resources.cameraDepthTexture);
                builder.SetRenderAttachment(shafts, 0, AccessFlags.Write);
                builder.SetRenderFunc(static (ShaftData d, RasterGraphContext context) =>
                {
                    d.Block.Clear();
                    d.Block.SetVector(BlitScaleBiasId, new Vector4(1f, 1f, 0f, 0f));
                    context.cmd.DrawProcedural(Matrix4x4.identity, d.Material, 1, MeshTopology.Triangles, 3, 1, d.Block);
                });
                return shafts;
            }

            private sealed class ShaftData
            {
                internal Material Material;
                internal MaterialPropertyBlock Block;
            }
        }
    }
}

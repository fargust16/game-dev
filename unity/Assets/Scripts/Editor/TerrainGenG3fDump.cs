#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using GameDev.World;
using GameDev.World.Hydrology;
using GameDev.World.Terrain;
using GameDev.World.TerrainGen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace GameDev.EditorTools
{
    /// <summary>
    /// G3f research batch steps (GEOGRAPHY_G3F_TECHNICAL_SPEC.md §13). The surfaces and most readings are computed by the
    /// G3f Python package (tools/g3f); this file only does what needs the Unity project: <c>contour</c> exports
    /// contour A (LmcContour.A()) and its Stage 1 heights; <c>hydro</c> runs the production hydrology planner on given
    /// lattices (GF-R5, GF-R10); <c>views</c> renders a camera list with G1's presentation; <c>h4a</c> checks the pinned
    /// production hash. Editor-only, referenced by nothing, writes only outside Assets, never saves a scene or asset.
    /// </summary>
    public static class TerrainGenG3fDump
    {
        private static readonly int[] Calibration = { 110201, 1337 };

        public static void DumpBatch()
        {
            var code = 0;
            try
            {
                code = Run(System.Environment.GetCommandLineArgs());
            }
            catch (Exception e)
            {
                Debug.LogError("G3f: exception " + e);
                code = 2;
            }

            EditorApplication.Exit(code);
        }

        private static string Arg(string[] args, string name)
        {
            for (var i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }

        private static int Run(string[] args)
        {
            var step = Arg(args, "-g3fStep") ?? "";
            var outDir = Arg(args, "-g3fOut");
            if (string.IsNullOrEmpty(outDir)) throw new ArgumentException("-g3fOut is required");
            outDir = Path.GetFullPath(outDir);
            if (outDir.StartsWith(Path.GetFullPath(Application.dataPath), StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("output inside Assets is refused");
            Directory.CreateDirectory(outDir);
            var log = new StringBuilder();
            log.AppendLine($"G3f step {step} - {DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture)}");
            var manifest = Arg(args, "-g3fProtected");
            if (!TerrainGenLmcDump.Protected(manifest, log, "before"))
            {
                File.WriteAllText(Path.Combine(outDir, "run_" + step + ".log"), log.ToString());
                return 3;
            }

            switch (step)
            {
                case "contour": Contour(outDir, log); break;
                case "h4a": H4A(log); break;
                case "hydro": Hydro(Arg(args, "-g3fIn"), Arg(args, "-g3fFreeze"), outDir, log); break;
                case "views": Views(Arg(args, "-g3fIn"), outDir, log); break;
                default: throw new ArgumentException($"unknown -g3fStep '{step}'");
            }

            var ok = TerrainGenLmcDump.Protected(manifest, log, "after");
            File.WriteAllText(Path.Combine(outDir, "run_" + step + ".log"), log.ToString());
            return ok ? 0 : 3;
        }

        // ------------------------------------------------------------------ contour A and the pinned production hash

        private static void Contour(string outDir, StringBuilder log)
        {
            var c = LmcContour.A();
            var ok = c.FieldHash == LmcContour.PinnedStage1Hash110201;
            log.AppendLine($"contour A: Stage 1 hash {c.FieldHash} (pinned {LmcContour.PinnedStage1Hash110201}) {(ok ? "OK" : "MISMATCH")}");
            if (!ok) throw new InvalidOperationException("contour A does not match its pinned Stage 1 hash");
            var n = c.N;
            var land = new byte[n * n];
            for (var i = 0; i < land.Length; i++) land[i] = c.Land[i] ? (byte)1 : (byte)0;
            File.WriteAllBytes(Path.Combine(outDir, "contourA_land.u8"), land);
            TerrainGenLmcDump.WriteFloats(Path.Combine(outDir, "contourA_stage1.f32"), c.Stage1.Field.Heights);
            File.WriteAllText(Path.Combine(outDir, "contourA.txt"), $"stage1_hash {c.FieldHash}\nn {n}\ncell {c.Cell.ToString("R", CultureInfo.InvariantCulture)}\nland_km2 {(c.LandAreaSqm / 1e6f).ToString("F6", CultureInfo.InvariantCulture)}\n");
            var world = H4A(log);
            // the production H4A 110201 lattice: the labelled reference panel of every sheet (phase plan §9.7)
            TerrainGenLmcDump.WriteFloats(Path.Combine(outDir, "h4a_110201.f32"), world.H4A);
        }

        private static IslandReliefWorld H4A(StringBuilder log)
        {
            var world = IslandReliefWorld.Build(110201, IslandReliefSettings.Default, withStage3: true);
            var h4a = world.H4AField.Hash();
            var pinned = TerrainGenGeographyDump.PinnedH4AHashes[110201];
            log.AppendLine($"production H4A 110201: {h4a} (pinned {pinned}) {(h4a == pinned ? "OK" : "MISMATCH")}");
            if (h4a != pinned) throw new InvalidOperationException("production H4A hash changed");
            return world;
        }

        // ------------------------------------------------------------------ hydrology (GF-R5, GF-R10, R7 (b))

        /// <summary>Input: a text file of lines "label|seed|path.f32". Seeds 1005-1024 are refused; held-out seeds
        /// (5001-5020) only when the freeze record exists.</summary>
        private static void Hydro(string listPath, string freeze, string outDir, StringBuilder log)
        {
            if (string.IsNullOrEmpty(listPath) || !File.Exists(listPath)) throw new ArgumentException("-g3fIn (lattice list) is required");
            H4A(log);
            var inv = CultureInfo.InvariantCulture;
            foreach (var line in File.ReadAllLines(listPath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split('|');
                var label = parts[0];
                var seed = int.Parse(parts[1], inv);
                if (seed >= 1005 && seed <= 1024) throw new InvalidOperationException($"seed {seed} is reserved for the island-scale evaluation (D-6 (f))");
                if (Array.IndexOf(Calibration, seed) < 0)
                {
                    if (seed < 5001 || seed > 5020) throw new InvalidOperationException($"seed {seed} is neither calibration nor G3f held-out");
                    if (string.IsNullOrEmpty(freeze) || !File.Exists(freeze)) throw new InvalidOperationException($"held-out seed {seed} without a freeze record");
                }

                var n = 1025;
                var cell = 2.734375f;
                var h = TerrainGenLmcDump.ReadFloats(parts[2], n * n);
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var grid = new HydrologyGrid(0f, 0f, n, n, cell, (float[])h.Clone(), WorldScale.SeaLevelMeters);
                var result = HydrologyPlanner.Plan(grid, HydrologySettings.Default, new GameDev.World.Seed.WorldSeed(seed));
                var json = new StringBuilder();
                json.Append("{\"label\":\"").Append(label).Append("\",\"seed\":").Append(seed);
                json.Append(",\"lattice_md5\":\"").Append(TerrainGenLmcDump.Md5(parts[2])).Append('"');
                json.Append(",\"succeeded\":").Append(result.Succeeded ? "true" : "false");
                json.Append(",\"failure\":\"").Append(result.Failure).Append("\",\"message\":\"").Append(Escape(result.Message)).Append('"');
                json.Append(",\"seconds\":").Append(watch.Elapsed.TotalSeconds.ToString("F2", inv));
                json.Append(",\"reaches\":[");
                if (result.Succeeded)
                {
                    var first = true;
                    foreach (var r in result.Definition.Reaches)
                    {
                        if (!first) json.Append(',');
                        first = false;
                        var a = r.Nodes.Count > 0 ? r.Nodes[0].PositionXZ : Vector2.zero;
                        var b = r.Nodes.Count > 0 ? r.Nodes[r.Nodes.Count - 1].PositionXZ : Vector2.zero;
                        json.Append("{\"id\":").Append(r.Id.Value).Append(",\"is_source\":").Append(r.IsSource ? "true" : "false");
                        json.Append(",\"termination\":\"").Append(r.Termination).Append('"');
                        json.Append(",\"downstream\":").Append(r.DownstreamId.IsValid ? r.DownstreamId.Value : -1);
                        json.Append(",\"lake\":").Append(r.DownstreamLake.IsValid ? r.DownstreamLake.Value : -1);
                        json.Append(",\"first_xz\":[").Append(a.x.ToString("R", inv)).Append(',').Append(a.y.ToString("R", inv)).Append(']');
                        json.Append(",\"last_xz\":[").Append(b.x.ToString("R", inv)).Append(',').Append(b.y.ToString("R", inv)).Append(']');
                        json.Append(",\"nodes\":").Append(r.Nodes.Count).Append(",\"strahler\":").Append(r.StrahlerOrder);
                        json.Append(",\"area_m2\":").Append(r.ContributingAreaSqm.ToString("R", inv)).Append('}');
                    }
                }

                json.Append("],\"lakes\":[");
                if (result.Succeeded)
                {
                    var first = true;
                    foreach (var lake in result.Definition.Lakes)
                    {
                        if (!first) json.Append(',');
                        first = false;
                        json.Append("{\"id\":").Append(lake.Id.Value).Append(",\"outlet\":").Append(lake.OutletReach.IsValid ? lake.OutletReach.Value : -1);
                        json.Append(",\"spill_xz\":[").Append(lake.SpillPointXZ.x.ToString("R", inv)).Append(',').Append(lake.SpillPointXZ.y.ToString("R", inv)).Append(']');
                        json.Append(",\"area_m2\":").Append(lake.AreaSqm.ToString("R", inv)).Append('}');
                    }
                }

                json.Append("]}");
                File.WriteAllText(Path.Combine(outDir, "hydro_" + label + ".json"), json.ToString());
                log.AppendLine($"hydro {label} seed {seed}: {(result.Succeeded ? "Succeeded" : result.Failure + " " + result.Message)} ({watch.Elapsed.TotalSeconds.ToString("F1", inv)} s)");
            }
        }

        private static string Escape(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");

        // ------------------------------------------------------------------ views (spec §10)

        /// <summary>Input: a text file. Lines "L|label|path.f32" name lattices; lines
        /// "C|id|fov|eyeX|eyeZ|eyeMode|eyeValue|targetX|targetZ|targetAbove|near|far|width|height|shadow|contour" name
        /// cameras (eyeMode "abs" or "above"). Every camera is rendered on every lattice to "label_id.png".</summary>
        private static void Views(string listPath, string outDir, StringBuilder log)
        {
            if (string.IsNullOrEmpty(listPath) || !File.Exists(listPath)) throw new ArgumentException("-g3fIn (view list) is required");
            var inv = CultureInfo.InvariantCulture;
            var lattices = new List<(string label, string path)>();
            var cams = new List<string[]>();
            foreach (var line in File.ReadAllLines(listPath))
            {
                var p = line.Split('|');
                if (p[0] == "L") lattices.Add((p[1], p[2]));
                else if (p[0] == "C") cams.Add(p);
            }

            var session = new Session();
            try
            {
                session.Begin();
                foreach (var (label, path) in lattices)
                {
                    var h = TerrainGenLmcDump.ReadFloats(path, 1025 * 1025);
                    session.Load(h, 1025, 2.734375f);
                    foreach (var c in cams)
                    {
                        float F(int k) => float.Parse(c[k], inv);
                        var eye = new Vector2(F(3), F(4));
                        var target = new Vector2(F(7), F(8));
                        var eyeY = c[5] == "abs" ? F(6) : session.Ground(eye) + F(6);
                        var targetY = session.Ground(target) + F(9);
                        var px = session.Capture(F(2), F(10), F(11), (int)F(12), (int)F(13), F(14), F(15), new Vector3(eye.x, eyeY, eye.y), new Vector3(target.x, targetY, target.y));
                        GeographyImages.WritePng(Path.Combine(outDir, $"{label}_{c[1]}.png"), px, (int)F(12));
                    }

                    session.Unload();
                    log.AppendLine($"views {label}: {cams.Count} cameras, lattice md5 {TerrainGenLmcDump.Md5(path)}");
                }
            }
            finally
            {
                session.End();
            }
        }

        // Rendering session copied from TerrainGenLmcViews.Session (G1's presentation, unchanged in substance); G3e files
        // are never edited, so the copy lives here.
        private sealed class Session
        {
            private readonly List<Object> m_owned = new List<Object>();
            private TerrainLayer m_layer;
            private Material m_material;
            private Material m_contourMaterial;
            private Camera m_camera;
            private UniversalRenderPipelineAsset m_pipeline;
            private float m_shadowDistance;
            private GameObject m_root;
            private UnityEngine.Terrain[] m_tiles;
            private readonly List<TerrainData> m_datas = new List<TerrainData>();
            private Mesh m_contour;
            private List<Vector2> m_segments;
            private OverworldTerrainGrid.Spec m_spec;

            public void Begin()
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                m_pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                m_shadowDistance = m_pipeline != null ? m_pipeline.shadowDistance : 0f;
                RenderSettings.fog = false;
                RenderSettings.skybox = null;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = GeometryFirstReview.Ambient;
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                RenderSettings.customReflectionTexture = null;
                RenderSettings.reflectionIntensity = 0f;
                var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { name = "G3fReview_Neutral" };
                var fill = new Color32[16];
                var g = GeometryFirstReview.NeutralGround;
                for (var i = 0; i < fill.Length; i++) fill[i] = new Color32(g.r, g.g, g.b, 0);
                texture.SetPixels32(fill);
                texture.Apply(false, false);
                m_owned.Add(texture);
                m_layer = new TerrainLayer { name = "G3fReview_Neutral", diffuseTexture = texture, tileSize = new Vector2(20f, 20f), smoothness = 0f, metallic = 0f };
                m_owned.Add(m_layer);
                m_material = new Material(Shader.Find(OverworldTerrainMenu.TerrainShaderName)) { name = "G3fReview_Terrain" };
                m_material.SetFloat("_EnableInstancedPerPixelNormal", 1f);
                m_material.EnableKeyword("_TERRAIN_INSTANCED_PERPIXEL_NORMAL");
                m_owned.Add(m_material);
                m_contourMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "G3fReview_Contour" };
                m_contourMaterial.SetColor("_BaseColor", GeometryFirstReview.SeaContour);
                m_contourMaterial.SetFloat("_Cull", 0f);
                m_owned.Add(m_contourMaterial);
                var sun = new GameObject("G3fReview_Sun");
                m_owned.Add(sun);
                sun.transform.rotation = Quaternion.Euler(GeometryFirstReview.SunEuler);
                var light = sun.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = Color.white;
                light.intensity = GeometryFirstReview.SunIntensity;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 1f;
                RenderSettings.sun = light;
                var cameraObject = new GameObject("G3fReview_Camera");
                m_owned.Add(cameraObject);
                m_camera = cameraObject.AddComponent<Camera>();
                m_camera.enabled = false;
                m_camera.clearFlags = CameraClearFlags.SolidColor;
                m_camera.backgroundColor = GeometryFirstReview.Background;
                m_camera.allowHDR = false;
                m_camera.allowMSAA = false;
                m_camera.useOcclusionCulling = false;
                var data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing = false;
                data.antialiasing = AntialiasingMode.None;
                data.renderShadows = true;
                data.requiresColorOption = CameraOverrideOption.Off;
                data.requiresDepthOption = CameraOverrideOption.Off;
                data.dithering = false;
                data.stopNaN = false;
            }

            public void Load(float[] heights, int n, float cell)
            {
                m_spec = OverworldTerrainGrid.Spec.Default;
                m_root = new GameObject("G3fReview");
                m_tiles = OverworldTerrainGrid.Create(m_root.transform, m_spec, new[] { m_layer }, m_material);
                var field = OverworldTerrainField.FromSamples(m_spec, (float x, float z, out float up, out float sd) =>
                {
                    up = 0f;
                    sd = 0f;
                    var ix = Mathf.Clamp(Mathf.RoundToInt(x / cell), 0, n - 1);
                    var iz = Mathf.Clamp(Mathf.RoundToInt(z / cell), 0, n - 1);
                    return heights[iz * n + ix];
                });
                m_datas.Clear();
                for (var tz = 0; tz < m_spec.TileCountZ; tz++)
                for (var tx = 0; tx < m_spec.TileCountX; tx++)
                {
                    var tile = m_tiles[tx + tz * m_spec.TileCountX];
                    m_datas.Add(tile.terrainData);
                    OverworldTerrainBuilder.WriteHeights(tile, field, tx, tz);
                    tile.drawTreesAndFoliage = false;
                    tile.heightmapPixelError = 1f;
                    tile.basemapDistance = 20000f;
                    tile.shadowCastingMode = ShadowCastingMode.TwoSided;
                    tile.reflectionProbeUsage = ReflectionProbeUsage.Off;
                }

                var contourObject = new GameObject("SeaLevelContour");
                contourObject.transform.SetParent(m_root.transform, false);
                m_contour = new Mesh { name = "SeaLevelContour", indexFormat = IndexFormat.UInt32 };
                contourObject.AddComponent<MeshFilter>().sharedMesh = m_contour;
                var renderer = contourObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = m_contourMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                m_segments = SeaLevelSegments(heights, n, cell);
            }

            public float Ground(Vector2 p)
            {
                var tx = Mathf.Clamp(Mathf.FloorToInt(p.x / m_spec.TileMeters), 0, m_spec.TileCountX - 1);
                var tz = Mathf.Clamp(Mathf.FloorToInt(p.y / m_spec.TileMeters), 0, m_spec.TileCountZ - 1);
                var tile = m_tiles[tx + tz * m_spec.TileCountX];
                return tile.SampleHeight(new Vector3(p.x, 0f, p.y)) + tile.transform.position.y;
            }

            public void Unload()
            {
                Object.DestroyImmediate(m_contour);
                Object.DestroyImmediate(m_root);
                foreach (var d in m_datas) Object.DestroyImmediate(d);
                m_datas.Clear();
            }

            public void End()
            {
                if (m_pipeline != null) m_pipeline.shadowDistance = m_shadowDistance;
                for (var i = m_owned.Count - 1; i >= 0; i--) if (m_owned[i] != null) Object.DestroyImmediate(m_owned[i]);
                m_owned.Clear();
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            public Color32[] Capture(float fov, float near, float far, int width, int height, float shadow, float contour, Vector3 eye, Vector3 target)
            {
                BuildRibbon(m_contour, m_segments, contour);
                if (m_pipeline != null) m_pipeline.shadowDistance = shadow;
                var camera = m_camera;
                camera.orthographic = false;
                camera.fieldOfView = fov;
                camera.nearClipPlane = near;
                camera.farClipPlane = far;
                camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(target - eye, Vector3.up));
                camera.aspect = width / (float)height;
                var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 1, name = "G3fReview_RT" };
                rt.Create();
                var readback = new Texture2D(width, height, TextureFormat.RGBA32, false);
                var previous = RenderTexture.active;
                try
                {
                    camera.targetTexture = rt;
                    var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
                    if (RenderPipeline.SupportsRenderRequest(camera, request)) RenderPipeline.SubmitRenderRequest(camera, request);
                    else camera.Render();
                    RenderTexture.active = rt;
                    readback.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                    readback.Apply(false, false);
                    return readback.GetPixels32();
                }
                finally
                {
                    camera.targetTexture = null;
                    RenderTexture.active = previous;
                    rt.Release();
                    Object.DestroyImmediate(rt);
                    Object.DestroyImmediate(readback);
                }
            }

            private static List<Vector2> SeaLevelSegments(float[] h, int n, float cell)
            {
                var sea = WorldScale.SeaLevelMeters;
                var segments = new List<Vector2>();
                var crossing = new Vector2[4];
                for (var z = 0; z + 1 < n; z++)
                for (var x = 0; x + 1 < n; x++)
                {
                    var a = h[z * n + x] - sea;
                    var b = h[z * n + x + 1] - sea;
                    var d = h[(z + 1) * n + x] - sea;
                    var e = h[(z + 1) * n + x + 1] - sea;
                    var k = 0;
                    if ((a > 0f) != (b > 0f)) crossing[k++] = new Vector2(x + a / (a - b), z);
                    if ((b > 0f) != (e > 0f)) crossing[k++] = new Vector2(x + 1, z + b / (b - e));
                    if ((d > 0f) != (e > 0f)) crossing[k++] = new Vector2(x + d / (d - e), z + 1);
                    if ((a > 0f) != (d > 0f)) crossing[k++] = new Vector2(x, z + a / (a - d));
                    if (k == 2) { segments.Add(crossing[0] * cell); segments.Add(crossing[1] * cell); }
                    else if (k == 4)
                    {
                        var centreLand = (a + b + d + e) * 0.25f > 0f;
                        if (centreLand == a > 0f) { segments.Add(crossing[0] * cell); segments.Add(crossing[1] * cell); segments.Add(crossing[2] * cell); segments.Add(crossing[3] * cell); }
                        else { segments.Add(crossing[0] * cell); segments.Add(crossing[3] * cell); segments.Add(crossing[1] * cell); segments.Add(crossing[2] * cell); }
                    }
                }

                return segments;
            }

            private static void BuildRibbon(Mesh mesh, List<Vector2> segments, float width)
            {
                var y = WorldScale.SeaLevelMeters + 0.5f;
                var count = segments.Count / 2;
                var vertices = new Vector3[count * 4];
                var indices = new int[count * 6];
                for (var s = 0; s < count; s++)
                {
                    var a = segments[2 * s];
                    var b = segments[2 * s + 1];
                    var dir = b - a;
                    var length = dir.magnitude;
                    dir = length > 1e-6f ? dir / length : Vector2.right;
                    var side = new Vector2(-dir.y, dir.x) * (width * 0.5f);
                    var extend = dir * (width * 0.5f);
                    var p = a - extend;
                    var q = b + extend;
                    vertices[4 * s] = new Vector3(p.x - side.x, y, p.y - side.y);
                    vertices[4 * s + 1] = new Vector3(p.x + side.x, y, p.y + side.y);
                    vertices[4 * s + 2] = new Vector3(q.x + side.x, y, q.y + side.y);
                    vertices[4 * s + 3] = new Vector3(q.x - side.x, y, q.y - side.y);
                    indices[6 * s] = 4 * s; indices[6 * s + 1] = 4 * s + 1; indices[6 * s + 2] = 4 * s + 2;
                    indices[6 * s + 3] = 4 * s; indices[6 * s + 4] = 4 * s + 2; indices[6 * s + 5] = 4 * s + 3;
                }

                mesh.Clear();
                mesh.vertices = vertices;
                mesh.triangles = indices;
                mesh.RecalculateBounds();
            }
        }
    }
}
#endif

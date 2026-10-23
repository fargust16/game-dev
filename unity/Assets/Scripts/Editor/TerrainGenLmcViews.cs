#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using GameDev.World;
using GameDev.World.Terrain;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace GameDev.EditorTools
{
    /// <summary>
    /// G3e views (spec §9): G1's V1–V6 through its public entry point on the candidate lattice, structure-anchored
    /// views whose cameras come from the LM2 layout (so both syntheses of a seed share every camera record), a plan
    /// with the declared structure, and blinded X/Y contact sheets. Rendering uses an unsaved empty scene and G1's
    /// presentation (neutral untextured terrain, production sun, flat ambient, no water, fog or post-processing).
    /// </summary>
    public static class TerrainGenLmcViews
    {
        public sealed class View
        {
            public string Id;
            public string Scale;
            public string Placement;
            public float Fov = 50f;
            public Vector2 EyeXZ;
            public float EyeAboveGround = float.NaN;
            public float EyeAbsolute = float.NaN;
            public Vector2 TargetXZ;
            public float TargetAboveGround;
            public float Near = 0.5f;
            public float Far = 10000f;
            public int Width = GeometryFirstReview.ObliqueWidth;
            public int Height = GeometryFirstReview.ObliqueHeight;
            public float Shadow = 2500f;
            public float Contour = 1.5f;
        }

        /// <summary>Per-candidate views written during calibration (G1 and the structure plan); oblique sets come from <see cref="RenderSet"/>.</summary>
        public static void Render(string dir, string tag, float[] h, LmcLayout L, LmcContour c, LmcReadings m)
        {
            var g1 = Path.Combine(dir, "unblinded", "g1_" + tag);
            Directory.CreateDirectory(g1);
            GeometryFirstReview.Run(new[] { 110201 }, g1, Path.Combine(dir, tag + ".f32"), tag);
            WritePlan(Path.Combine(dir, "unblinded", "plan_" + tag + ".png"), h, L, c, m);
        }

        /// <summary>The layout's camera records (identical for every lattice rendered on that layout).</summary>
        public static List<View> Cameras(LmcLayout L, LmcContour c)
        {
            var sea = WorldScale.SeaLevelMeters;
            var anchors = GeographyAnchors.From(c.Stage1);
            var cxz = anchors.Centroid;
            var radius = Mathf.Max(1f, anchors.EqualAreaRadius);
            var views = new List<View>
            {
                new View { Id = "OVR", Scale = "island", Placement = "c + 2.2 r at bearing 200 deg, 1300 m, looking at the centroid", Fov = 40f,
                    EyeXZ = anchors.PointAt(200f, 2.2f * radius, c.Stage1, false), EyeAbsolute = 1300f, TargetXZ = cxz, TargetAboveGround = 0f, Shadow = 6000f, Contour = 3f },
            };

            var main = L.Main != null ? L.Main.P : cxz;
            // the shore vertex farthest from the main culmination
            var far = cxz; var farD = -1f;
            for (var i = 0; i < L.ShoreIntent.Length; i++)
            {
                if (L.ShoreIntent[i] == LmcCodes.ShoreNone) continue;
                var p = LmcGrid.P(i, L.N, L.Cell);
                var d = Vector2.Distance(p, main);
                if (d > farD) { farD = d; far = p; }
            }

            var outward = Outward(c, far);
            views.Add(new View { Id = "MTN", Scale = "regional", Placement = "200 m offshore of the coast farthest from the main culmination, 350 m above sea, looking at the main culmination", Fov = 45f,
                EyeXZ = far + outward * 200f, EyeAbsolute = sea + 350f, TargetXZ = main, TargetAboveGround = 0f, Shadow = 5000f, Contour = 2f });

            foreach (var pl in L.Plateaus)
            {
                // middle of the widest escarpment arc
                int bestStart = 0, bestLen = 0;
                for (var k = 0; k < 360; k++)
                {
                    if (pl.Rim[k] != LmcCodes.RimEscarpment || pl.Rim[(k + 359) % 360] == LmcCodes.RimEscarpment) continue;
                    var len = 0;
                    while (len < 360 && pl.Rim[(k + len) % 360] == LmcCodes.RimEscarpment) len++;
                    if (len > bestLen) { bestLen = len; bestStart = k; }
                }

                var bearing = bestLen > 0 ? bestStart + 0.5f * bestLen : pl.RampBearing + 180f;
                var dir = new Vector2(Mathf.Cos(bearing * Mathf.Deg2Rad), Mathf.Sin(bearing * Mathf.Deg2Rad));
                var rim = pl.RimPoint(bearing);
                views.Add(new View { Id = "PLT" + pl.Id, Scale = "regional", Placement = $"plateau {pl.Id}: 400 m outside the middle of its widest escarpment arc (bearing {bearing:F0} deg), 35 deg down onto the rim, looking at the top centre", Fov = 50f,
                    EyeXZ = rim + dir * 400f, EyeAboveGround = 400f * Mathf.Tan(35f * Mathf.Deg2Rad), TargetXZ = pl.Centre, TargetAboveGround = 0f });
            }

            if (L.Pass != null && L.PassCorridors.Count > 0)
            {
                var corr = L.PassCorridors[0];
                var away = corr.At(Mathf.Min(500f, corr.Length));
                var d = (away - L.Pass.P).sqrMagnitude > 1f ? (away - L.Pass.P).normalized : Vector2.right;
                views.Add(new View { Id = "PASS", Scale = "regional", Placement = "500 m from the pass along its first corridor, 150 m above ground, looking at the pass", Fov = 50f,
                    EyeXZ = L.Pass.P + d * 500f, EyeAboveGround = 150f, TargetXZ = L.Pass.P, TargetAboveGround = 0f });
                views.Add(new View { Id = "GND-PASS", Scale = "player", Placement = "eye 1.7 m at the declared pass, looking level along the second corridor", Fov = 60f,
                    EyeXZ = L.Pass.P, EyeAboveGround = 1.7f, TargetXZ = L.PassCorridors.Count > 1 ? L.PassCorridors[1].At(Mathf.Min(300f, L.PassCorridors[1].Length)) : away, TargetAboveGround = 1.7f, Near = 0.1f, Shadow = 1500f, Contour = 0.5f });
            }

            for (var k = 0; k < Mathf.Min(2, L.Outlets.Count); k++)
            {
                var o = L.Outlets[k];
                var inland = -Outward(c, o);
                views.Add(new View { Id = "OUT" + k, Scale = "regional", Placement = $"outlet {k}: 350 m offshore, 60 m above sea, looking 300 m inland", Fov = 50f,
                    EyeXZ = o - inland * 350f, EyeAbsolute = sea + 60f, TargetXZ = o + inland * 300f, TargetAboveGround = 0f });
            }

            foreach (var (code, name) in new[] { (LmcCodes.ShoreLow, "low"), (LmcCodes.ShoreRocky, "rocky"), (LmcCodes.ShoreCliff, "cliff") })
            {
                var run = LongestIntentRun(L, code);
                if (run < 0) continue;
                var p = LmcGrid.P(run, L.N, L.Cell);
                var outw = Outward(c, p);
                views.Add(new View { Id = "SHORE-" + name, Scale = "regional", Placement = $"the longest declared {name} shore run: 250 m offshore of its middle, 40 m above sea, looking at it", Fov = 50f,
                    EyeXZ = p + outw * 250f, EyeAbsolute = sea + 40f, TargetXZ = p - outw * 40f, TargetAboveGround = 0f });
            }

            // ground view anchored on the layout (the basin-0 vertex farthest from the coast), so both candidates share it
            var cap = cxz; var capSd = -1f;
            for (var i = 0; i < L.Region.Length; i++)
            {
                if (L.Region[i] != LmcCodes.Lowland || L.Instance[i] != 0) continue;
                if (c.Sd[i] > capSd) { capSd = c.Sd[i]; cap = LmcGrid.P(i, L.N, L.Cell); }
            }

            views.Add(new View { Id = "GND-CAP", Scale = "player", Placement = "eye 1.7 m at basin 0's point farthest from the coast (layout anchor for the capital view), looking level at the main culmination", Fov = 60f,
                EyeXZ = cap, EyeAboveGround = 1.7f, TargetXZ = main, TargetAboveGround = 1.7f, Near = 0.1f, Shadow = 1500f, Contour = 0.5f });
            return views;
        }

        private static int LongestIntentRun(LmcLayout L, byte code)
        {
            var n = L.N;
            var mask = new bool[L.ShoreIntent.Length];
            for (var i = 0; i < mask.Length; i++) mask[i] = L.ShoreIntent[i] == code;
            List<int> best = null;
            foreach (var comp in LmcMeasure.Components8(mask, n)) if (best == null || comp.Count > best.Count) best = comp;
            return best == null ? -1 : best[best.Count / 2];
        }

        private static Vector2 Outward(LmcContour c, Vector2 p)
        {
            var n = c.N;
            var i = LmcGrid.Index(p, n, c.Cell);
            var x = i % n; var z = i / n;
            var g = new Vector2(c.Sd[z * n + Mathf.Min(n - 1, x + 3)] - c.Sd[z * n + Mathf.Max(0, x - 3)], c.Sd[Mathf.Min(n - 1, z + 3) * n + x] - c.Sd[Mathf.Max(0, z - 3) * n + x]);
            return g.sqrMagnitude > 1e-6f ? -g.normalized : Vector2.up;
        }

        /// <summary>Renders the layout's camera set on each named lattice into <paramref name="dir"/>; returns the camera records.</summary>
        public static string RenderSet(string dir, LmcLayout L, LmcContour c, IList<(string label, float[] h)> lattices)
        {
            var views = Cameras(L, c);
            var session = new Session();
            var records = new StringBuilder();
            var inv = CultureInfo.InvariantCulture;
            try
            {
                session.Begin();
                foreach (var (label, h) in lattices)
                {
                    session.Load(h, L.N, L.Cell);
                    foreach (var v in views)
                    {
                        var eyeY = !float.IsNaN(v.EyeAbsolute) ? v.EyeAbsolute : session.Ground(v.EyeXZ) + v.EyeAboveGround;
                        var targetY = session.Ground(v.TargetXZ) + v.TargetAboveGround;
                        var pixels = session.Capture(v, new Vector3(v.EyeXZ.x, eyeY, v.EyeXZ.y), new Vector3(v.TargetXZ.x, targetY, v.TargetXZ.y));
                        GeographyImages.WritePng(Path.Combine(dir, $"{label}_{v.Id}.png"), pixels, v.Width);
                    }

                    session.Unload();
                }
            }
            finally
            {
                session.End();
            }

            foreach (var v in views)
            {
                records.AppendLine(string.Format(inv, "{0} [{1}]: {2}; eye xz ({3:F1}, {4:F1}){5}; target xz ({6:F1}, {7:F1}) +{8:F1} m; fov {9:F0}",
                    v.Id, v.Scale, v.Placement, v.EyeXZ.x, v.EyeXZ.y,
                    !float.IsNaN(v.EyeAbsolute) ? string.Format(inv, " at {0:F1} m", v.EyeAbsolute) : string.Format(inv, " +{0:F1} m above ground", v.EyeAboveGround),
                    v.TargetXZ.x, v.TargetXZ.y, v.TargetAboveGround, v.Fov));
            }

            return records.ToString();
        }

        /// <summary>A 2 × 2 sheet: top X | Y, bottom references (positions fixed and listed in the sheet index, never the synthesis names).</summary>
        public static void Sheet(string path, string[] panels)
        {
            var images = new Texture2D[4];
            var w = 0; var h = 0;
            for (var k = 0; k < 4; k++)
            {
                if (panels[k] == null || !File.Exists(panels[k])) continue;
                images[k] = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                images[k].LoadImage(File.ReadAllBytes(panels[k]));
                w = Math.Max(w, images[k].width);
                h = Math.Max(h, images[k].height);
            }

            if (w == 0) return;
            var gap = 8;
            var W = 2 * w + gap; var H = 2 * h + gap;
            var outPixels = new Color32[W * H];
            for (var i = 0; i < outPixels.Length; i++) outPixels[i] = new Color32(255, 255, 255, 255);
            for (var k = 0; k < 4; k++)
            {
                if (images[k] == null) continue;
                var px = images[k].GetPixels32();
                var ox = (k % 2) * (w + gap);
                var oy = (k < 2 ? 1 : 0) * (h + gap); // row 0 (top) = panels 0, 1 — textures are bottom-up
                for (var y = 0; y < images[k].height; y++)
                for (var x = 0; x < images[k].width; x++)
                    outPixels[(oy + y) * W + ox + x] = px[y * images[k].width + x];
                Object.DestroyImmediate(images[k]);
            }

            GeographyImages.WritePng(path, outPixels, W);
        }

        /// <summary>Plan of a candidate with the layout's structure and the measured sites.</summary>
        public static void WritePlan(string path, float[] h, LmcLayout L, LmcContour c, LmcReadings m)
        {
            var n = L.N;
            var cell = L.Cell;
            var land = c.Land;
            var pixels = GeographyImages.Hillshaded(h, n, cell, land);
            for (var i = 0; i < pixels.Length; i++)
            {
                switch (L.Region[i])
                {
                    case LmcCodes.Lowland: pixels[i] = GeographyImages.Blend(pixels[i], new Color32(90, 170, 70, 255), 0.25f); break;
                    case LmcCodes.Highland: pixels[i] = GeographyImages.Blend(pixels[i], new Color32(150, 100, 60, 255), 0.20f); break;
                    case LmcCodes.PlateauTop: pixels[i] = GeographyImages.Blend(pixels[i], new Color32(230, 210, 80, 255), 0.35f); break;
                    case LmcCodes.PlateauBand: pixels[i] = GeographyImages.Blend(pixels[i], new Color32(200, 60, 40, 255), 0.35f); break;
                    case LmcCodes.PlateauRamp: pixels[i] = GeographyImages.Blend(pixels[i], new Color32(240, 140, 30, 255), 0.35f); break;
                    case LmcCodes.Corridor: pixels[i] = GeographyImages.Blend(pixels[i], new Color32(60, 140, 200, 255), 0.25f); break;
                }
            }

            foreach (var r in L.Ridges) GeographyImages.DrawPolyline(pixels, n, cell, r.Points.ToArray(), new Color32(255, 255, 255, 255));
            foreach (var t in L.Trunks) GeographyImages.DrawPolyline(pixels, n, cell, t.Points.ToArray(), new Color32(20, 40, 180, 255));
            foreach (var p in L.PassCorridors) GeographyImages.DrawPolyline(pixels, n, cell, p.Points.ToArray(), new Color32(0, 200, 200, 255));
            foreach (var o in L.Outlets) GeographyImages.Disc(pixels, n, LmcGrid.Index(o, n, cell), 6, new Color32(255, 230, 0, 255));
            foreach (var node in L.Nodes)
            {
                var col = node.Kind == "culmination" ? new Color32(255, 255, 255, 255) : node.Kind == "pass" ? new Color32(230, 0, 0, 255) : new Color32(120, 120, 120, 255);
                GeographyImages.Disc(pixels, n, LmcGrid.Index(node.P, n, cell), 5, col);
            }

            if (m != null)
            {
                foreach (var site in m.Sites)
                {
                    var rc = Mathf.RoundToInt(site.RadiusMeters / cell);
                    var idx = LmcGrid.Index(site.Position, n, cell);
                    for (var k = 0; k < 360; k += 2)
                    {
                        var x = idx % n + Mathf.RoundToInt(rc * Mathf.Cos(k * Mathf.Deg2Rad));
                        var z = idx / n + Mathf.RoundToInt(rc * Mathf.Sin(k * Mathf.Deg2Rad));
                        if (x >= 0 && z >= 0 && x < n && z < n) pixels[z * n + x] = site.Coastal ? new Color32(255, 0, 255, 255) : new Color32(255, 120, 255, 255);
                    }
                }
            }

            GeographyImages.WritePng(path, pixels, n);
        }

        // ------------------------------------------------------------------ rendering session (copied from G3c's views, unchanged in substance)

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
            private List<TerrainData> m_datas = new List<TerrainData>();
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
                var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { name = "LmcReview_Neutral" };
                var fill = new Color32[16];
                var g = GeometryFirstReview.NeutralGround;
                for (var i = 0; i < fill.Length; i++) fill[i] = new Color32(g.r, g.g, g.b, 0);
                texture.SetPixels32(fill);
                texture.Apply(false, false);
                m_owned.Add(texture);
                m_layer = new TerrainLayer { name = "LmcReview_Neutral", diffuseTexture = texture, tileSize = new Vector2(20f, 20f), smoothness = 0f, metallic = 0f };
                m_owned.Add(m_layer);
                m_material = new Material(Shader.Find(OverworldTerrainMenu.TerrainShaderName)) { name = "LmcReview_Terrain" };
                m_material.SetFloat("_EnableInstancedPerPixelNormal", 1f);
                m_material.EnableKeyword("_TERRAIN_INSTANCED_PERPIXEL_NORMAL");
                m_owned.Add(m_material);
                m_contourMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "LmcReview_Contour" };
                m_contourMaterial.SetColor("_BaseColor", GeometryFirstReview.SeaContour);
                m_contourMaterial.SetFloat("_Cull", 0f);
                m_owned.Add(m_contourMaterial);
                var sun = new GameObject("LmcReview_Sun");
                m_owned.Add(sun);
                sun.transform.rotation = Quaternion.Euler(GeometryFirstReview.SunEuler);
                var light = sun.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = Color.white;
                light.intensity = GeometryFirstReview.SunIntensity;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 1f;
                RenderSettings.sun = light;
                var cameraObject = new GameObject("LmcReview_Camera");
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
                m_root = new GameObject("LmcReview");
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

            public Color32[] Capture(View view, Vector3 eye, Vector3 target)
            {
                BuildRibbon(m_contour, m_segments, view.Contour);
                if (m_pipeline != null) m_pipeline.shadowDistance = view.Shadow;
                var camera = m_camera;
                camera.orthographic = false;
                camera.fieldOfView = view.Fov;
                camera.nearClipPlane = view.Near;
                camera.farClipPlane = view.Far;
                camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(target - eye, Vector3.up));
                camera.aspect = view.Width / (float)view.Height;
                var rt = new RenderTexture(view.Width, view.Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 1, name = "LmcReview_" + view.Id };
                rt.Create();
                var readback = new Texture2D(view.Width, view.Height, TextureFormat.RGBA32, false);
                var previous = RenderTexture.active;
                try
                {
                    camera.targetTexture = rt;
                    var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
                    if (RenderPipeline.SupportsRenderRequest(camera, request)) RenderPipeline.SubmitRenderRequest(camera, request);
                    else camera.Render();
                    RenderTexture.active = rt;
                    readback.ReadPixels(new Rect(0, 0, view.Width, view.Height), 0, 0, false);
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

#if UNITY_EDITOR
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
    /// <summary>Plan image of a G3c candidate with its declared structure and the readings' targets.</summary>
    public static class G3cImages
    {
        public static void WriteStructure(string path, G3cResult r, G3cReadings m, GeographyAnalysis a)
        {
            var n = r.N;
            var cell = r.Cell;
            var pixels = GeographyImages.Hillshaded(r.Heights, n, cell, a.Land);
            for (var i = 0; i < pixels.Length; i++)
            {
                switch (r.Region[i])
                {
                    case G3cResult.Lowland:
                        pixels[i] = GeographyImages.Blend(pixels[i], new Color32(90, 170, 70, 255), 0.30f);
                        break;
                    case G3cResult.Highland:
                        pixels[i] = GeographyImages.Blend(pixels[i], new Color32(150, 100, 60, 255), 0.25f);
                        break;
                    case G3cResult.PlateauTop:
                        pixels[i] = GeographyImages.Blend(pixels[i], new Color32(230, 210, 80, 255), 0.45f);
                        break;
                    case G3cResult.PlateauWall:
                        pixels[i] = GeographyImages.Blend(pixels[i], new Color32(200, 60, 40, 255), 0.45f);
                        break;
                    case G3cResult.PlateauRamp:
                        pixels[i] = GeographyImages.Blend(pixels[i], new Color32(240, 140, 30, 255), 0.45f);
                        break;
                }

                if (a.Land[i] && a.Channel[i])
                {
                    pixels[i] = GeographyImages.Blend(pixels[i], new Color32(40, 110, 240, 255), 0.7f);
                }
            }

            foreach (var river in r.Rivers)
            {
                var pts = new Vector2[river.Nodes.Count];
                for (var k = 0; k < pts.Length; k++)
                {
                    pts[k] = r.NodePos[river.Nodes[k]];
                }

                GeographyImages.DrawPolyline(pixels, n, cell, pts, river.Trunk ? new Color32(10, 30, 160, 255) : new Color32(60, 80, 200, 255));
            }

            GeographyImages.DrawPolyline(pixels, n, cell, r.Spine.ToArray(), new Color32(255, 255, 255, 255));
            foreach (var o in r.Outlets)
            {
                GeographyImages.Disc(pixels, n, G3cGrid.Index(o, n, cell), 6, new Color32(255, 230, 0, 255));
            }

            foreach (var p in r.Culminations)
            {
                GeographyImages.Disc(pixels, n, G3cGrid.Index(p, n, cell), 5, new Color32(255, 255, 255, 255));
            }

            GeographyImages.Disc(pixels, n, G3cGrid.Index(r.Waist, n, cell), 5, new Color32(0, 230, 230, 255));
            foreach (var p in m.PassList)
            {
                GeographyImages.Disc(pixels, n, G3cGrid.Index(p, n, cell), 5, new Color32(230, 0, 0, 255));
            }

            foreach (var site in m.SiteList)
            {
                var rc = Mathf.RoundToInt(site.RadiusMeters / cell);
                var idx = G3cGrid.Index(site.Position, n, cell);
                var cx = idx % n;
                var cz = idx / n;
                for (var k = 0; k < 360; k += 2)
                {
                    var x = cx + Mathf.RoundToInt(rc * Mathf.Cos(k * Mathf.Deg2Rad));
                    var z = cz + Mathf.RoundToInt(rc * Mathf.Sin(k * Mathf.Deg2Rad));
                    if (x >= 0 && z >= 0 && x < n && z < n)
                    {
                        pixels[z * n + x] = site.Coastal ? new Color32(255, 0, 255, 255) : new Color32(255, 120, 255, 255);
                    }
                }
            }

            GeographyImages.WritePng(path, pixels, n);
        }
    }

    /// <summary>
    /// Views of a G3c candidate with the Geometry-First Review's presentation (neutral untextured terrain,
    /// production sun, flat ambient, no water surface, fog, post-processing or vegetation). G1 itself is not
    /// changed: it anchors on a seed's production Stage 1 and cannot frame contour B, so these views use its
    /// public constants and the same camera rules with the contour's own Stage 1 anchors (brief §8.3).
    /// </summary>
    public sealed class G3cViews
    {
        private readonly List<Object> m_owned = new List<Object>();
        private TerrainLayer m_layer;
        private Material m_material;
        private Material m_contourMaterial;
        private Camera m_camera;
        private UniversalRenderPipelineAsset m_pipeline;
        private float m_shadowDistance;

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

            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { name = "G3cReview_Neutral" };
            var fill = new Color32[16];
            var g = GeometryFirstReview.NeutralGround;
            for (var i = 0; i < fill.Length; i++)
            {
                fill[i] = new Color32(g.r, g.g, g.b, 0);
            }

            texture.SetPixels32(fill);
            texture.Apply(false, false);
            m_owned.Add(texture);
            m_layer = new TerrainLayer { name = "G3cReview_Neutral", diffuseTexture = texture, tileSize = new Vector2(20f, 20f), smoothness = 0f, metallic = 0f };
            m_owned.Add(m_layer);
            m_material = new Material(Shader.Find(OverworldTerrainMenu.TerrainShaderName)) { name = "G3cReview_Terrain" };
            m_material.SetFloat("_EnableInstancedPerPixelNormal", 1f);
            m_material.EnableKeyword("_TERRAIN_INSTANCED_PERPIXEL_NORMAL");
            m_owned.Add(m_material);
            m_contourMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "G3cReview_Contour" };
            m_contourMaterial.SetColor("_BaseColor", GeometryFirstReview.SeaContour);
            m_contourMaterial.SetFloat("_Cull", 0f);
            m_owned.Add(m_contourMaterial);

            var sun = new GameObject("G3cReview_Sun");
            m_owned.Add(sun);
            sun.transform.rotation = Quaternion.Euler(GeometryFirstReview.SunEuler);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = GeometryFirstReview.SunIntensity;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 1f;
            RenderSettings.sun = light;

            var cameraObject = new GameObject("G3cReview_Camera");
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

        public void End()
        {
            if (m_pipeline != null)
            {
                m_pipeline.shadowDistance = m_shadowDistance;
            }

            for (var i = m_owned.Count - 1; i >= 0; i--)
            {
                if (m_owned[i] != null)
                {
                    Object.DestroyImmediate(m_owned[i]);
                }
            }

            m_owned.Clear();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private sealed class View
        {
            public string Id;
            public string Placement;
            public bool Orthographic;
            public float Size;
            public float Fov;
            public Vector3 Position;
            public Vector3 Target;
            public Vector3 Up = Vector3.up;
            public float Near = 0.5f;
            public float Far = 10000f;
            public int Width = GeometryFirstReview.ObliqueWidth;
            public int Height = GeometryFirstReview.ObliqueHeight;
            public float Shadow = 3000f;
            public float Contour = 3f;
        }

        public void Render(string directory, string tag, G3cContour c, G3cResult r, G3cReadings m)
        {
            var n = r.N;
            var cell = r.Cell;
            var heights = r.Heights;
            var spec = OverworldTerrainGrid.Spec.Default;
            var root = new GameObject("G3cReview");
            var tiles = OverworldTerrainGrid.Create(root.transform, spec, new[] { m_layer }, m_material);
            var field = OverworldTerrainField.FromSamples(spec, (float x, float z, out float up, out float sd) =>
            {
                up = 0f;
                sd = 0f;
                var ix = Mathf.Clamp(Mathf.RoundToInt(x / cell), 0, n - 1);
                var iz = Mathf.Clamp(Mathf.RoundToInt(z / cell), 0, n - 1);
                return heights[iz * n + ix];
            });

            var datas = new List<TerrainData>();
            for (var tz = 0; tz < spec.TileCountZ; tz++)
            {
                for (var tx = 0; tx < spec.TileCountX; tx++)
                {
                    var tile = tiles[tx + tz * spec.TileCountX];
                    datas.Add(tile.terrainData);
                    OverworldTerrainBuilder.WriteHeights(tile, field, tx, tz);
                    tile.drawTreesAndFoliage = false;
                    tile.heightmapPixelError = 1f;
                    tile.basemapDistance = 20000f;
                    tile.shadowCastingMode = ShadowCastingMode.TwoSided;
                    tile.reflectionProbeUsage = ReflectionProbeUsage.Off;
                }
            }

            var contourObject = new GameObject("SeaLevelContour");
            contourObject.transform.SetParent(root.transform, false);
            var contourMesh = new Mesh { name = "SeaLevelContour", indexFormat = IndexFormat.UInt32 };
            contourObject.AddComponent<MeshFilter>().sharedMesh = contourMesh;
            var contourRenderer = contourObject.AddComponent<MeshRenderer>();
            contourRenderer.sharedMaterial = m_contourMaterial;
            contourRenderer.shadowCastingMode = ShadowCastingMode.Off;
            contourRenderer.receiveShadows = false;
            var segments = SeaLevelSegments(heights, n, cell);

            float Ground(Vector2 p)
            {
                var tx = Mathf.Clamp(Mathf.FloorToInt(p.x / spec.TileMeters), 0, spec.TileCountX - 1);
                var tz = Mathf.Clamp(Mathf.FloorToInt(p.y / spec.TileMeters), 0, spec.TileCountZ - 1);
                var tile = tiles[tx + tz * spec.TileCountX];
                return tile.SampleHeight(new Vector3(p.x, 0f, p.y)) + tile.transform.position.y;
            }

            var views = Views(c, r, m, Ground);
            var records = new StringBuilder();
            var inv = CultureInfo.InvariantCulture;
            foreach (var view in views)
            {
                BuildRibbon(contourMesh, segments, view.Contour);
                if (m_pipeline != null)
                {
                    m_pipeline.shadowDistance = view.Shadow;
                }

                var pixels = Capture(view);
                var file = $"review_g3c_{tag}_{view.Id}.png";
                GeographyImages.WritePng(Path.Combine(directory, file), pixels, view.Width);
                records.AppendLine($"{file}: {view.Id} {view.Placement}; position ({view.Position.x.ToString("F1", inv)}, {view.Position.y.ToString("F1", inv)}, {view.Position.z.ToString("F1", inv)}) target ({view.Target.x.ToString("F1", inv)}, {view.Target.y.ToString("F1", inv)}, {view.Target.z.ToString("F1", inv)}) fov {view.Fov.ToString("F0", inv)}");
            }

            File.WriteAllText(Path.Combine(directory, $"review_g3c_{tag}.txt"), records.ToString());
            Object.DestroyImmediate(contourMesh);
            Object.DestroyImmediate(root);
            foreach (var d in datas)
            {
                Object.DestroyImmediate(d);
            }
        }

        private static List<View> Views(G3cContour c, G3cResult r, G3cReadings m, System.Func<Vector2, float> ground)
        {
            var sea = WorldScale.SeaLevelMeters;
            var half = WorldScale.WorldExtentXMeters * 0.5f;
            var anchors = GeographyAnchors.From(c.Stage1);
            var cxz = anchors.Centroid;
            var radius = Mathf.Max(1f, anchors.EqualAreaRadius);
            var centre = new Vector3(cxz.x, sea, cxz.y);
            var views = new List<View>
            {
                new View
                {
                    Id = "V1", Placement = "G1 rule: orthographic plan of the world", Orthographic = true, Size = half,
                    Position = new Vector3(half, 1500f, half), Target = new Vector3(half, 0f, half), Up = Vector3.forward,
                    Near = 1f, Far = 2000f, Width = GeometryFirstReview.PlanSize, Height = GeometryFirstReview.PlanSize, Shadow = 2000f,
                },
            };

            foreach (var (id, bearing) in new[] { ("V2", 225f), ("V3", 45f) })
            {
                var xz = anchors.PointAt(bearing, 1.8f * radius, c.Stage1, false);
                var position = new Vector3(xz.x, 900f, xz.y);
                views.Add(new View
                {
                    Id = id, Placement = $"G1 rule: c + 1.8 r at bearing {bearing} deg, 900 m, looking at c", Fov = 40f,
                    Position = position, Target = centre, Shadow = Vector3.Distance(position, centre) + 1.5f * radius,
                });
            }

            // Structure-anchored views (rules fixed in brief §8.3 / research record §14).
            if (r.Plateaus.Count > 0)
            {
                var pl = r.Plateaus[0];
                foreach (var other in r.Plateaus)
                {
                    if (other.Height > pl.Height)
                    {
                        pl = other;
                    }
                }

                var bearing = pl.RampBearing + 60f * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(bearing), Mathf.Sin(bearing));
                var eye = pl.Centre + dir * (pl.TopRadius * 1.6f + 350f);
                var topH = ground(pl.Centre);
                views.Add(new View
                {
                    Id = "PLT", Placement = $"tallest plateau ({pl.Height:F0} m): 350 m beyond its rim, 60 deg from the ramp, 150 m above ground, looking at the top centre",
                    Fov = 50f, Position = new Vector3(eye.x, ground(eye) + 150f, eye.y), Target = new Vector3(pl.Centre.x, topH, pl.Centre.y), Shadow = 2500f, Contour = 1.5f,
                });
            }

            var passFrom = r.PassPairA >= 0 && r.PassPairA < r.LowlandSeeds.Count ? r.LowlandSeeds[r.PassPairA] : (r.LowlandSeeds.Count > 0 ? r.LowlandSeeds[0] : cxz);
            var pd = (passFrom - r.Waist).sqrMagnitude > 1f ? (passFrom - r.Waist).normalized : Vector2.right;
            var passEye = r.Waist + pd * 500f;
            views.Add(new View
            {
                Id = "PASS", Placement = "500 m from the waist toward the pass pair's first lowland, 150 m above ground, looking at the waist",
                Fov = 50f, Position = new Vector3(passEye.x, ground(passEye) + 150f, passEye.y), Target = new Vector3(r.Waist.x, ground(r.Waist), r.Waist.y), Shadow = 2500f, Contour = 1.5f,
            });

            if (r.Outlets.Count > 0)
            {
                var o = r.Outlets[0];
                var inland = Inland(c, o);
                var eye = o - inland * 350f;
                var look = o + inland * 300f;
                views.Add(new View
                {
                    Id = "OUT", Placement = "first outlet: 350 m offshore, 60 m above sea, looking 300 m inland",
                    Fov = 50f, Position = new Vector3(eye.x, sea + 60f, eye.y), Target = new Vector3(look.x, ground(look), look.y), Shadow = 2500f, Contour = 1.5f,
                });
            }

            var stand = m.SiteList.Count > 0 ? m.SiteList[0].Position : (r.LowlandSeeds.Count > 0 ? r.LowlandSeeds[0] : cxz);
            var spineMid = r.Spine.Count > 0 ? r.Spine[r.Spine.Count / 2] : cxz;
            var gEye = ground(stand) + 1.7f;
            views.Add(new View
            {
                Id = "GND", Placement = "eye height 1.7 m at the capital site (else the first lowland seed), looking level at the spine's middle",
                Fov = 60f, Position = new Vector3(stand.x, gEye, stand.y), Target = new Vector3(spineMid.x, gEye, spineMid.y), Near = 0.1f, Shadow = 1500f, Contour = 0.5f,
            });

            return views;
        }

        private static Vector2 Inland(G3cContour c, Vector2 p)
        {
            var n = c.N;
            var i = G3cGrid.Index(p, n, c.Cell);
            var x = i % n;
            var z = i / n;
            var xp = Mathf.Min(n - 1, x + 3);
            var xm = Mathf.Max(0, x - 3);
            var zp = Mathf.Min(n - 1, z + 3);
            var zm = Mathf.Max(0, z - 3);
            var g = new Vector2(c.Sd[z * n + xp] - c.Sd[z * n + xm], c.Sd[zp * n + x] - c.Sd[zm * n + x]);
            return g.sqrMagnitude > 1e-6f ? g.normalized : Vector2.up;
        }

        private Color32[] Capture(View view)
        {
            var camera = m_camera;
            camera.orthographic = view.Orthographic;
            camera.orthographicSize = view.Size;
            camera.fieldOfView = view.Orthographic ? 60f : view.Fov;
            camera.nearClipPlane = view.Near;
            camera.farClipPlane = view.Far;
            camera.transform.SetPositionAndRotation(view.Position, Quaternion.LookRotation(view.Target - view.Position, view.Up));
            camera.aspect = view.Width / (float)view.Height;
            var rt = new RenderTexture(view.Width, view.Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 1, name = "G3cReview_" + view.Id };
            rt.Create();
            var readback = new Texture2D(view.Width, view.Height, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = rt;
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
                if (RenderPipeline.SupportsRenderRequest(camera, request))
                {
                    RenderPipeline.SubmitRenderRequest(camera, request);
                }
                else
                {
                    camera.Render();
                }

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
            {
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
                    if (k == 2)
                    {
                        segments.Add(crossing[0] * cell);
                        segments.Add(crossing[1] * cell);
                    }
                    else if (k == 4)
                    {
                        var centreLand = (a + b + d + e) * 0.25f > 0f;
                        if (centreLand == a > 0f)
                        {
                            segments.Add(crossing[0] * cell);
                            segments.Add(crossing[1] * cell);
                            segments.Add(crossing[2] * cell);
                            segments.Add(crossing[3] * cell);
                        }
                        else
                        {
                            segments.Add(crossing[0] * cell);
                            segments.Add(crossing[3] * cell);
                            segments.Add(crossing[1] * cell);
                            segments.Add(crossing[2] * cell);
                        }
                    }
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
                indices[6 * s] = 4 * s;
                indices[6 * s + 1] = 4 * s + 1;
                indices[6 * s + 2] = 4 * s + 2;
                indices[6 * s + 3] = 4 * s;
                indices[6 * s + 4] = 4 * s + 2;
                indices[6 * s + 5] = 4 * s + 3;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = indices;
            mesh.RecalculateBounds();
        }
    }
}
#endif

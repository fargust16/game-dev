#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Geometry-First Review (G1 of <c>GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md</c> §§3, 9): renders a height
    /// lattice on neutral, untextured terrain from a fixed camera set, so relief is judged as shape.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Heights only, in memory, never saved.</b> The tool opens an untitled scene, builds the 2×2
    /// tiles with <see cref="OverworldTerrainGrid.Create"/> and one neutral in-memory layer and material,
    /// and writes the lattice with <see cref="OverworldTerrainBuilder.WriteHeights"/> and nothing else:
    /// no <c>PaintSurface</c>, <c>PlaceTrees</c>, <c>PlaceGrass</c>, ocean, volume, fog, skybox or
    /// reflection probe. No <c>TerrainData</c> becomes an asset and no scene is saved; the production
    /// scene and its tile assets are never opened for writing. It is not a bake path: the lattice it
    /// shows is read, never decided.
    /// </para>
    /// <para>
    /// Input is any 1025 × 1025 world lattice: by default production <c>H4A</c> from the dump path
    /// (<see cref="IslandReliefWorld"/>), or a little-endian float32 file
    /// (<c>-geoReviewLattice</c>, row-major <c>z · 1025 + x</c>, metres) for a candidate field. Declared
    /// structure, anchors and drainage overlays always come from the seed.
    /// </para>
    /// <para>
    /// Comparison views (V2–V5) are anchored on <b>Stage 1 only</b> — the mainland coast polygon's
    /// centroid <c>c</c> and equal-area radius <c>r</c> — so they do not move when the relief changes.
    /// Bearings, distances, heights and fields of view are authoring defaults (research record §3.4).
    /// V4 and V5 stand on the served terrain surface of the reviewed lattice, which is the only part of
    /// a pose that depends on it; every pose is written to the JSON record.
    /// </para>
    /// <code>
    /// Unity.exe -batchmode -quit -projectPath &lt;project&gt; \
    ///   -executeMethod GameDev.EditorTools.GeometryFirstReview.RunBatch \
    ///   [-geoReviewSeeds 110201,1337] [-geoReviewOut &lt;folder outside Assets&gt;] \
    ///   [-geoReviewLattice &lt;file.f32&gt; -geoReviewCandidate &lt;id&gt;]
    /// </code>
    /// </remarks>
    public static class GeometryFirstReview
    {
        public const int ObliqueWidth = 1920;
        public const int ObliqueHeight = 1080;
        public const int PlanSize = 2048;

        /// <summary>The production sun direction (<c>OverworldTerrainMenu.CreateSunLight</c>).</summary>
        public static readonly Vector3 SunEuler = new Vector3(42f, -35f, 0f);

        public const float SunIntensity = 1.2f;
        public static readonly Color Ambient = new Color(0.40f, 0.41f, 0.43f, 1f);
        public static readonly Color Background = new Color(0.80f, 0.83f, 0.87f, 1f);
        public static readonly Color32 NeutralGround = new Color32(160, 160, 154, 255);
        public static readonly Color SeaContour = new Color(0.05f, 0.20f, 0.55f, 1f);

        private const string SeedsArg = "-geoReviewSeeds";
        private const string OutputArg = "-geoReviewOut";
        private const string LatticeArg = "-geoReviewLattice";
        private const string CandidateArg = "-geoReviewCandidate";

        [MenuItem("GameDev/Overworld/Terrain Gen/Geometry-First Review G1 (seed 110201)", false, 231)]
        public static void ReviewMenu()
        {
            Debug.Log(Run(
                new[] { OverworldTerrainMenu.DefaultSeed },
                Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "geometry-review"),
                null,
                "H4A"));
        }

        /// <summary>Batch entry; see the type remarks for arguments.</summary>
        public static void RunBatch()
        {
            var args = System.Environment.GetCommandLineArgs();
            var directory = TerrainGenGeographyDump.ReadArg(args, OutputArg);
            if (string.IsNullOrEmpty(directory))
            {
                directory = Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "geometry-review");
            }

            var seeds = new List<int>();
            var raw = TerrainGenGeographyDump.ReadArg(args, SeedsArg);
            if (!string.IsNullOrEmpty(raw))
            {
                foreach (var part in raw.Split(','))
                {
                    if (int.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                    {
                        seeds.Add(value);
                    }
                }
            }

            if (seeds.Count == 0)
            {
                seeds.Add(OverworldTerrainMenu.DefaultSeed);
            }

            var lattice = TerrainGenGeographyDump.ReadArg(args, LatticeArg);
            var candidate = TerrainGenGeographyDump.ReadArg(args, CandidateArg);
            if (string.IsNullOrEmpty(candidate))
            {
                candidate = string.IsNullOrEmpty(lattice) ? "H4A" : "file:" + Path.GetFileName(lattice);
            }

            Debug.Log(Run(seeds.ToArray(), directory, lattice, candidate));
        }

        public static string Run(int[] seeds, string directory, string latticePath, string candidate)
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return "Geometry-First Review cancelled: the open scene has unsaved changes.";
            }

            Directory.CreateDirectory(directory);
            var previousScene = SceneManager.GetActiveScene().path;
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var shadowDistance = pipeline != null ? pipeline.shadowDistance : 0f;
            var log = new StringBuilder();
            var timings = new StringBuilder("seed world_ms analysis_ms scene_ms render_ms\n");
            var session = new Session();

            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                session.Prepare();
                foreach (var seed in seeds)
                {
                    log.AppendLine(ReviewSeed(seed, directory, latticePath, candidate, pipeline, session, timings));
                }
            }
            finally
            {
                // The pipeline asset is shared project state: restore the one value the review moves,
                // and never mark it dirty.
                if (pipeline != null)
                {
                    pipeline.shadowDistance = shadowDistance;
                }

                session.Dispose();
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                if (!Application.isBatchMode && !string.IsNullOrEmpty(previousScene))
                {
                    EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
                }
            }

            File.WriteAllText(Path.Combine(directory, "review_timings.txt"), timings.ToString());
            return log.ToString();
        }

        private static string ReviewSeed(
            int seed, string directory, string latticePath, string candidate,
            UniversalRenderPipelineAsset pipeline, Session session, StringBuilder timings)
        {
            var c = CultureInfo.InvariantCulture;
            var watch = Stopwatch.StartNew();
            var world = IslandReliefWorld.Build(seed, IslandReliefSettings.Default, withStage3: true);
            var worldMs = watch.ElapsedMilliseconds;

            var n = world.Width;
            var cell = world.Cell;
            var heights = string.IsNullOrEmpty(latticePath) ? world.H4A : ReadLattice(latticePath, n);
            var latticeHash = new WorldHeightField(world.H4AField.OriginX, world.H4AField.OriginZ, n, n, cell, heights).Hash();
            var anchors = GeographyAnchors.From(world.Stage1);

            watch.Restart();
            var analysis = GeographyAnalysis.Build(heights, n, cell, seed);
            var analysisMs = watch.ElapsedMilliseconds;

            watch.Restart();
            var spec = OverworldTerrainGrid.Spec.Default;
            var root = new GameObject("GeometryFirstReview");
            session.Track(root);
            var tiles = OverworldTerrainGrid.Create(root.transform, spec, new[] { session.Layer }, session.Material);
            var field = OverworldTerrainField.FromSamples(spec, (float x, float z, out float up, out float sd) =>
            {
                up = 0f;
                sd = 0f;
                var ix = Mathf.Clamp(Mathf.RoundToInt(x / cell), 0, n - 1);
                var iz = Mathf.Clamp(Mathf.RoundToInt(z / cell), 0, n - 1);
                return heights[iz * n + ix];
            });

            for (var tz = 0; tz < spec.TileCountZ; tz++)
            {
                for (var tx = 0; tx < spec.TileCountX; tx++)
                {
                    var tile = tiles[tx + tz * spec.TileCountX];
                    session.Track(tile.terrainData);
                    OverworldTerrainBuilder.WriteHeights(tile, field, tx, tz);
                    tile.drawTreesAndFoliage = false;
                    tile.heightmapPixelError = 1f;
                    tile.basemapDistance = 20000f;
                    tile.shadowCastingMode = ShadowCastingMode.TwoSided;
                    tile.reflectionProbeUsage = ReflectionProbeUsage.Off;
                }
            }

            var contour = SeaLevelSegments(heights, n, cell);
            var contourObject = new GameObject("SeaLevelContour");
            contourObject.transform.SetParent(root.transform, false);
            var contourMesh = new Mesh { name = "SeaLevelContour", indexFormat = IndexFormat.UInt32 };
            session.Track(contourMesh);
            contourObject.AddComponent<MeshFilter>().sharedMesh = contourMesh;
            var contourRenderer = contourObject.AddComponent<MeshRenderer>();
            contourRenderer.sharedMaterial = session.ContourMaterial;
            contourRenderer.shadowCastingMode = ShadowCastingMode.Off;
            contourRenderer.receiveShadows = false;

            var sceneMs = watch.ElapsedMilliseconds;
            watch.Restart();

            var views = Views(world, anchors, tiles, spec);
            var records = new List<string>();
            Color32[] planPixels = null;
            foreach (var view in views)
            {
                BuildRibbon(contourMesh, contour, view.ContourWidthMeters);
                if (pipeline != null)
                {
                    pipeline.shadowDistance = view.ShadowDistance;
                }

                var file = $"review_{seed}_{Sanitize(candidate)}_{view.Id}.png";
                var pixels = Render(session, view);
                if (view.Id == "V1")
                {
                    planPixels = (Color32[])pixels.Clone();
                }

                GeographyImages.WritePng(Path.Combine(directory, file), pixels, view.Width);
                records.Add(view.ToJson(file, PixelHash(pixels), c));
            }

            if (planPixels != null)
            {
                var drainage = DrainageOverlay(planPixels, world, analysis);
                var file = $"review_{seed}_{Sanitize(candidate)}_V6.png";
                GeographyImages.WritePng(Path.Combine(directory, file), drainage, PlanSize);
                records.Add(
                    "{\"id\":\"V6\",\"file\":\"" + file + "\",\"derivedFrom\":\"V1\",\"pixelHash\":\"" + PixelHash(drainage)
                    + "\",\"overlays\":\"depressions orange (fill depth > 0.05 m), D8 channels blue (contributing area >= "
                    + analysis.ChannelAreaSqm.ToString("F0", c)
                    + " m2), D8 flow-divide skeleton red (main divide bright), hydrology reaches navy by Strahler order, ocean outlets yellow, "
                    + "massif crest lines and primary spurs magenta, Stage 3C trajectories cyan, Stage 3E basin outlines green\"}");
            }

            var renderMs = watch.ElapsedMilliseconds;

            var json = new StringBuilder();
            json.Append('{');
            json.Append($"\"tool\":\"GeometryFirstReview G1\",\"seed\":{seed},\"generatorVersion\":{world.Stage1.Settings.GeneratorVersion},");
            json.Append($"\"candidate\":\"{Escape(candidate)}\",\"latticeSource\":\"{(string.IsNullOrEmpty(latticePath) ? "IslandReliefWorld.H4A (dump path)" : Escape(Path.GetFileName(latticePath)))}\",");
            json.Append($"\"latticeHash\":\"{latticeHash}\",\"latticeSize\":{n},\"latticeStepMeters\":{cell.ToString("R", c)},");
            json.Append($"\"pinnedH4AHash\":\"{(TerrainGenGeographyDump.PinnedH4AHashes.TryGetValue(seed, out var pinned) ? pinned : "-")}\",");
            json.Append($"\"unity\":\"{Application.unityVersion}\",\"graphics\":\"{Escape(SystemInfo.graphicsDeviceType.ToString())}\",");
            json.Append($"\"anchors\":{{\"rule\":\"Stage 1 mainland coast polygon\",\"cx\":{anchors.Centroid.x.ToString("R", c)},\"cz\":{anchors.Centroid.y.ToString("R", c)},\"r\":{anchors.EqualAreaRadius.ToString("R", c)}}},");
            json.Append($"\"light\":{{\"euler\":[{SunEuler.x.ToString("R", c)},{SunEuler.y.ToString("R", c)},{SunEuler.z.ToString("R", c)}],\"intensity\":{SunIntensity.ToString("R", c)},\"shadows\":\"soft\",");
            json.Append($"\"ambient\":[{Ambient.r.ToString("R", c)},{Ambient.g.ToString("R", c)},{Ambient.b.ToString("R", c)}]}},");
            json.Append("\"presentation\":{\"material\":\"URP Terrain/Lit, one in-memory layer, flat RGB " + NeutralGround.r + "," + NeutralGround.g + "," + NeutralGround.b
                        + " alpha 0 (smoothness 0)\",\"writes\":\"OverworldTerrainBuilder.WriteHeights only\",\"trees\":false,\"groundCover\":false,\"water\":\"none; the 24 m contour drawn as an unlit ribbon 0.5 m above sea level\","
                        + "\"postProcessing\":false,\"fog\":false,\"skybox\":false,\"reflectionProbes\":false,\"msaa\":false,\"heightmapPixelError\":1},");
            json.Append("\"views\":[").Append(string.Join(",", records)).Append("]}");
            File.WriteAllText(Path.Combine(directory, $"review_{seed}_{Sanitize(candidate)}.json"), json.ToString());

            timings.AppendLine($"{seed} {worldMs} {analysisMs} {sceneMs} {renderMs}");

            Object.DestroyImmediate(root);
            return $"[GeometryFirstReview] seed {seed} candidate {candidate} lattice {latticeHash}: {records.Count} views written to {directory}";
        }

        // ---------- views ----------

        private sealed class View
        {
            public string Id;
            public string Purpose;
            public bool Orthographic;
            public float OrthographicSize;
            public float FieldOfView;
            public Vector3 Position;
            public Vector3 Target;
            public Vector3 Up = Vector3.up;
            public float Near;
            public float Far;
            public int Width;
            public int Height;
            public float ShadowDistance;
            public float ContourWidthMeters;
            public string Placement;

            public Quaternion Rotation => Quaternion.LookRotation(Target - Position, Up);

            public string ToJson(string file, string pixelHash, CultureInfo c)
            {
                var e = Rotation.eulerAngles;
                return "{"
                       + $"\"id\":\"{Id}\",\"file\":\"{file}\",\"purpose\":\"{Purpose}\",\"placement\":\"{Placement}\","
                       + $"\"projection\":\"{(Orthographic ? "orthographic" : "perspective")}\","
                       + (Orthographic ? $"\"orthographicSize\":{OrthographicSize.ToString("R", c)}," : $"\"fov\":{FieldOfView.ToString("R", c)},")
                       + $"\"position\":[{Position.x.ToString("R", c)},{Position.y.ToString("R", c)},{Position.z.ToString("R", c)}],"
                       + $"\"target\":[{Target.x.ToString("R", c)},{Target.y.ToString("R", c)},{Target.z.ToString("R", c)}],"
                       + $"\"euler\":[{e.x.ToString("R", c)},{e.y.ToString("R", c)},{e.z.ToString("R", c)}],"
                       + $"\"near\":{Near.ToString("R", c)},\"far\":{Far.ToString("R", c)},\"width\":{Width},\"height\":{Height},"
                       + $"\"shadowDistance\":{ShadowDistance.ToString("R", c)},\"contourWidthMeters\":{ContourWidthMeters.ToString("R", c)},"
                       + $"\"pixelHash\":\"{pixelHash}\"}}";
            }
        }

        private static List<View> Views(
            IslandReliefWorld world, GeographyAnchors anchors, UnityEngine.Terrain[] tiles, OverworldTerrainGrid.Spec spec)
        {
            var sea = WorldScale.SeaLevelMeters;
            var half = WorldScale.WorldExtentXMeters * 0.5f;
            var cxz = anchors.Centroid;
            var r = Mathf.Max(1f, anchors.EqualAreaRadius);
            var centre = new Vector3(cxz.x, sea, cxz.y);
            var views = new List<View>();

            views.Add(new View
            {
                Id = "V1",
                Purpose = "plan: silhouette, regions",
                Placement = "orthographic top-down over the whole world, north up",
                Orthographic = true,
                OrthographicSize = half,
                Position = new Vector3(half, 1500f, half),
                Target = new Vector3(half, 0f, half),
                Up = Vector3.forward,
                Near = 1f,
                Far = 2000f,
                Width = PlanSize,
                Height = PlanSize,
                ShadowDistance = 2000f,
                ContourWidthMeters = 3f,
            });

            foreach (var (id, bearing) in new[] { ("V2", 225f), ("V3", 45f) })
            {
                var xz = anchors.PointAt(bearing, 1.8f * r, world.Stage1, false);
                var position = new Vector3(xz.x, 900f, xz.y);
                var distance = Vector3.Distance(position, centre);
                views.Add(new View
                {
                    Id = id,
                    Purpose = id == "V2" ? "distant oblique: silhouette, mountain system against the sky" : "opposite oblique: recognisability from a second side",
                    Placement = $"c + 1.8 r at bearing {bearing.ToString(CultureInfo.InvariantCulture)} deg, 900 m, looking at c on sea level",
                    FieldOfView = 40f,
                    Position = position,
                    Target = centre,
                    Near = 1f,
                    Far = 10000f,
                    Width = ObliqueWidth,
                    Height = ObliqueHeight,
                    ShadowDistance = distance + 1.5f * r,
                    ContourWidthMeters = 3f,
                });
            }

            var p4 = anchors.PointAt(180f, 0.6f * r, world.Stage1, true);
            var g4 = GroundAt(tiles, spec, p4);
            var v4 = new Vector3(p4.x, Mathf.Max(g4, sea) + 120f, p4.y);
            views.Add(new View
            {
                Id = "V4",
                Purpose = "mid-distance: ridge, valley, foothill transition",
                Placement = "Stage 1 land point at bearing 180 deg, 0.6 r from c, 120 m above the served surface, looking at c on sea level",
                FieldOfView = 50f,
                Position = v4,
                Target = centre,
                Near = 0.5f,
                Far = 10000f,
                Width = ObliqueWidth,
                Height = ObliqueHeight,
                ShadowDistance = 2.5f * r,
                ContourWidthMeters = 1.5f,
            });

            var p5 = anchors.PointAt(270f, 0.5f * r, world.Stage1, true);
            var g5 = GroundAt(tiles, spec, p5);
            var v5 = new Vector3(p5.x, g5 + 1.7f, p5.y);
            views.Add(new View
            {
                Id = "V5",
                Purpose = "ground: scale of nearby landforms",
                Placement = "Stage 1 land point at bearing 270 deg, 0.5 r from c, eye height 1.7 m above the served surface, looking level toward c",
                FieldOfView = 60f,
                Position = v5,
                Target = new Vector3(cxz.x, v5.y, cxz.y),
                Near = 0.1f,
                Far = 10000f,
                Width = ObliqueWidth,
                Height = ObliqueHeight,
                ShadowDistance = 1500f,
                ContourWidthMeters = 0.5f,
            });

            return views;
        }

        /// <summary>The served surface under a world point — what Unity renders and collides with.</summary>
        private static float GroundAt(UnityEngine.Terrain[] tiles, OverworldTerrainGrid.Spec spec, Vector2 p)
        {
            var tx = Mathf.Clamp(Mathf.FloorToInt(p.x / spec.TileMeters), 0, spec.TileCountX - 1);
            var tz = Mathf.Clamp(Mathf.FloorToInt(p.y / spec.TileMeters), 0, spec.TileCountZ - 1);
            var tile = tiles[tx + tz * spec.TileCountX];
            return tile.SampleHeight(new Vector3(p.x, 0f, p.y)) + tile.transform.position.y;
        }

        // ---------- rendering ----------

        private static Color32[] Render(Session session, View view)
        {
            var camera = session.Camera;
            camera.orthographic = view.Orthographic;
            camera.orthographicSize = view.OrthographicSize;
            camera.fieldOfView = view.Orthographic ? 60f : view.FieldOfView;
            camera.nearClipPlane = view.Near;
            camera.farClipPlane = view.Far;
            camera.transform.SetPositionAndRotation(view.Position, view.Rotation);
            camera.aspect = view.Width / (float)view.Height;

            var rt = new RenderTexture(view.Width, view.Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                antiAliasing = 1,
                name = "GeometryFirstReview_" + view.Id,
            };
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

        /// <summary>Scene-level presentation objects that live for one review run.</summary>
        private sealed class Session : IDisposable
        {
            private readonly List<Object> m_owned = new List<Object>();

            public TerrainLayer Layer { get; private set; }
            public Material Material { get; private set; }
            public Material ContourMaterial { get; private set; }
            public Camera Camera { get; private set; }

            public void Prepare()
            {
                RenderSettings.fog = false;
                RenderSettings.skybox = null;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = Ambient;
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                RenderSettings.customReflectionTexture = null;
                RenderSettings.reflectionIntensity = 0f;

                var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { name = "GeometryFirstReview_Neutral" };
                // URP Terrain/Lit reads smoothness from the diffuse alpha (TerrainLayer smoothness
                // source), and the basemap does too: alpha 0 keeps the ground matte in every mode.
                var fill = new Color32[16];
                for (var i = 0; i < fill.Length; i++)
                {
                    fill[i] = new Color32(NeutralGround.r, NeutralGround.g, NeutralGround.b, 0);
                }

                texture.SetPixels32(fill);
                texture.Apply(false, false);
                Track(texture);

                Layer = new TerrainLayer
                {
                    name = "GeometryFirstReview_Neutral",
                    diffuseTexture = texture,
                    tileSize = new Vector2(20f, 20f),
                    smoothness = 0f,
                    metallic = 0f,
                };
                Track(Layer);

                var shader = Shader.Find(OverworldTerrainMenu.TerrainShaderName);
                Material = new Material(shader) { name = "GeometryFirstReview_Terrain" };
                Material.SetFloat("_EnableInstancedPerPixelNormal", 1f);
                Material.EnableKeyword("_TERRAIN_INSTANCED_PERPIXEL_NORMAL");
                Track(Material);

                ContourMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "GeometryFirstReview_Contour" };
                ContourMaterial.SetColor("_BaseColor", SeaContour);
                ContourMaterial.SetFloat("_Cull", 0f);
                Track(ContourMaterial);

                var sun = new GameObject("GeometryFirstReview_Sun");
                Track(sun);
                sun.transform.rotation = Quaternion.Euler(SunEuler);
                var light = sun.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = Color.white;
                light.intensity = SunIntensity;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 1f;
                RenderSettings.sun = light;

                var cameraObject = new GameObject("GeometryFirstReview_Camera");
                Track(cameraObject);
                Camera = cameraObject.AddComponent<Camera>();
                Camera.enabled = false;
                Camera.clearFlags = CameraClearFlags.SolidColor;
                Camera.backgroundColor = Background;
                Camera.allowHDR = false;
                Camera.allowMSAA = false;
                Camera.useOcclusionCulling = false;
                var data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing = false;
                data.antialiasing = AntialiasingMode.None;
                data.renderShadows = true;
                data.requiresColorOption = CameraOverrideOption.Off;
                data.requiresDepthOption = CameraOverrideOption.Off;
                data.dithering = false;
                data.stopNaN = false;
            }

            public void Track(Object owned)
            {
                if (owned != null)
                {
                    m_owned.Add(owned);
                }
            }

            public void Dispose()
            {
                for (var i = m_owned.Count - 1; i >= 0; i--)
                {
                    if (m_owned[i] != null)
                    {
                        Object.DestroyImmediate(m_owned[i]);
                    }
                }

                m_owned.Clear();
            }
        }

        // ---------- sea-level contour ----------

        /// <summary>Marching-squares segments of the 24 m contour on the lattice, in world XZ.</summary>
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
                    // Edges in order: bottom (a-b), right (b-e), top (d-e), left (a-d).
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
                        // Saddle cell: pair crossings so the centre's side stays connected.
                        var centreLand = (a + b + d + e) * 0.25f > 0f;
                        var aLand = a > 0f;
                        if (centreLand == aLand)
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

        /// <summary>
        /// Flat ribbon along the contour segments, half a metre above sea level so the shore slope does
        /// not swallow its landward half.
        /// </summary>
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

        // ---------- V6 drainage overlay ----------

        private static Color32[] DrainageOverlay(Color32[] plan, IslandReliefWorld w, GeographyAnalysis a)
        {
            var pixels = (Color32[])plan.Clone();
            var n = a.N;
            var scale = PlanSize / WorldScale.WorldExtentXMeters;

            for (var i = 0; i < n * n; i++)
            {
                if (!a.Land[i])
                {
                    continue;
                }

                var depth = a.Filled[i] - a.Heights[i];
                if (depth > 0.05f)
                {
                    Square(pixels, i, n, a.Cell, scale, new Color32(255, 140, 0, 255), 0.75f);
                }
                else if (a.Channel[i])
                {
                    Square(pixels, i, n, a.Cell, scale, new Color32(40, 110, 240, 255), 0.85f);
                }
            }

            for (var i = 0; i < n * n; i++)
            {
                if (a.Skeleton[i])
                {
                    var main = a.SkeletonComponent[i] == a.MainComponent;
                    Square(pixels, i, n, a.Cell, scale, main ? new Color32(235, 25, 25, 255) : new Color32(150, 30, 30, 255), 1f);
                }
            }

            if (w.Basins != null)
            {
                foreach (var basin in w.Basins.Basins)
                {
                    Polyline(pixels, basin.Boundary, scale, new Color32(40, 200, 60, 255), 0, true);
                }
            }

            if (w.Trajectories != null)
            {
                foreach (var t in w.Trajectories.Trajectories)
                {
                    Polyline(pixels, t.Points, scale, new Color32(0, 225, 225, 255), 1, false);
                }
            }

            foreach (var line in GeographyMetrics.DeclaredDivides(w, out _))
            {
                Polyline(pixels, line, scale, new Color32(225, 0, 225, 255), 1, false);
            }

            if (a.Hydrology.Succeeded)
            {
                foreach (var reach in a.Hydrology.Definition.Reaches)
                {
                    var points = new Vector2[reach.Nodes.Count];
                    for (var k = 0; k < points.Length; k++)
                    {
                        points[k] = reach.Nodes[k].PositionXZ;
                    }

                    Polyline(pixels, points, scale, new Color32(10, 20, 120, 255), Mathf.Clamp(reach.StrahlerOrder - 1, 0, 3), false);
                    if (reach.Termination == ReachTermination.Ocean && points.Length > 0)
                    {
                        Dot(pixels, points[points.Length - 1] * scale, 6, new Color32(255, 230, 0, 255));
                    }
                }
            }

            return pixels;
        }

        private static void Square(Color32[] pixels, int index, int n, float cell, float scale, Color32 colour, float alpha)
        {
            var x = (index % n) * cell * scale;
            var z = (index / n) * cell * scale;
            var half = cell * scale * 0.5f;
            for (var pz = Mathf.FloorToInt(z - half); pz <= Mathf.FloorToInt(z + half); pz++)
            {
                for (var px = Mathf.FloorToInt(x - half); px <= Mathf.FloorToInt(x + half); px++)
                {
                    if (px >= 0 && pz >= 0 && px < PlanSize && pz < PlanSize)
                    {
                        var k = pz * PlanSize + px;
                        pixels[k] = alpha >= 1f ? colour : GeographyImages.Blend(pixels[k], colour, alpha);
                    }
                }
            }
        }

        private static void Polyline(Color32[] pixels, Vector2[] points, float scale, Color32 colour, int thickness, bool closed)
        {
            if (points == null || points.Length < 2)
            {
                return;
            }

            var segments = closed ? points.Length : points.Length - 1;
            for (var k = 0; k < segments; k++)
            {
                var p = points[k] * scale;
                var q = points[(k + 1) % points.Length] * scale;
                var steps = Mathf.Max(1, Mathf.CeilToInt((q - p).magnitude * 2f));
                for (var s = 0; s <= steps; s++)
                {
                    Dot(pixels, Vector2.Lerp(p, q, s / (float)steps), thickness, colour);
                }
            }
        }

        private static void Dot(Color32[] pixels, Vector2 p, int radius, Color32 colour)
        {
            var cx = Mathf.FloorToInt(p.x);
            var cz = Mathf.FloorToInt(p.y);
            for (var dz = -radius; dz <= radius; dz++)
            {
                for (var dx = -radius; dx <= radius; dx++)
                {
                    var x = cx + dx;
                    var z = cz + dz;
                    if (dx * dx + dz * dz <= radius * radius && x >= 0 && z >= 0 && x < PlanSize && z < PlanSize)
                    {
                        pixels[z * PlanSize + x] = colour;
                    }
                }
            }
        }

        // ---------- plumbing ----------

        private static float[] ReadLattice(string path, int n)
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length != n * n * 4)
            {
                throw new ArgumentException($"Lattice file {path} holds {bytes.Length} bytes; a {n} x {n} float32 lattice needs {n * n * 4}.");
            }

            var heights = new float[n * n];
            Buffer.BlockCopy(bytes, 0, heights, 0, bytes.Length);
            return heights;
        }

        /// <summary>FNV-1a over the RGBA bytes — what two runs must agree on.</summary>
        private static string PixelHash(Color32[] pixels)
        {
            unchecked
            {
                var h = 14695981039346656037UL;
                foreach (var p in pixels)
                {
                    h = (h ^ p.r) * 1099511628211UL;
                    h = (h ^ p.g) * 1099511628211UL;
                    h = (h ^ p.b) * 1099511628211UL;
                    h = (h ^ p.a) * 1099511628211UL;
                }

                return h.ToString("x16");
            }
        }

        private static string Sanitize(string id)
        {
            var sb = new StringBuilder();
            foreach (var ch in id)
            {
                sb.Append(char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' ? ch : '_');
            }

            return sb.ToString();
        }

        private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
#endif

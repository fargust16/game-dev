using System.Linq;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Shapes Veilmouth's ground once from the city plan C2 (VEILMOUTH_CITY_PLAN.md §9, §21), at the owner's request of
    /// 2026-11-06 (C1's ground of 2026-11-05 is replaced); the ground is the owner's afterwards, so the tool refuses to run on ground it has already shaped. It is
    /// authored content, not the production terrain pipeline, and touches nothing of TerrainGen.
    /// <para>
    /// Positions are the plan's: <c>px</c> runs east and <c>py</c> south, 1 m each, world X = px − 500 and world Z =
    /// 500 − py. Every height is an authoring default in metres above the scene's zero.
    /// </para>
    /// </summary>
    public static class VeilmouthGround
    {
        public const int HeightmapResolution = 1025;
        public const int AlphamapResolution = 512;

        public const float Low = VeilmouthScene.TerrainLevel;
        public const float Ridge = Low + 4f; // C2 (§21): 4 m, so the shorter avenue keeps its 10 % climb
        public const float RiverBed = 21f;
        public const float SeaBed = 19.5f;
        public const float HillTop = 70f;

        private const float Size = VeilmouthScene.Extent;

        // City rectangle and its features, in plan metres (C2 svg, VEILMOUTH_CITY_PLAN_C2.svg).
        private const float CityWest = 480f, CityEast = 780f, CityNorth = 626f, CitySouth = 830f;
        private const float RidgeEdge = 680f;
        private const float AvenueX = 610f, AvenueFoot = 715f, AvenueHead = 674f;
        private const float RiverWest = 780f, RiverEast = 820f;
        private const float StairWest = 700f, StairEast = 708f, StairTop = 680f, StairFoot = 714f;

        [MenuItem("GameDev/Slice/Shape Veilmouth Ground from C2 (once)")]
        public static void Shape()
        {
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(VeilmouthScene.TerrainDataPath);
            if (data == null)
            {
                Debug.LogError($"[VeilmouthGround] {VeilmouthScene.TerrainDataPath} is missing; create the scene first.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            if (roots.Any(r => r.name == VeilmouthScene.MapName))
            {
                Debug.LogWarning("[VeilmouthGround] the ground is already shaped from C2 and is the owner's now; nothing changed.");
                return;
            }

            // Unity terrain traps: the resolution setters discard data, so they come before the size and the writes; the
            // data is already an asset, so the painted weights stay.
            data.heightmapResolution = HeightmapResolution;
            data.alphamapResolution = AlphamapResolution;
            data.size = new Vector3(Size, VeilmouthScene.HeightBudget, Size);

            var heights = Heights();
            var normalised = new float[HeightmapResolution, HeightmapResolution];
            for (var z = 0; z < HeightmapResolution; z++)
            {
                for (var x = 0; x < HeightmapResolution; x++)
                {
                    normalised[z, x] = Mathf.Clamp01(heights[z, x] / VeilmouthScene.HeightBudget);
                }
            }

            data.SetHeights(0, 0, normalised);
            // The city's street layers may follow the canonical five (C2 reshapes built ground); they are zero here
            // until VeilmouthCity paints them again.
            var canonical = Paint(heights);
            var weights = new float[AlphamapResolution, AlphamapResolution, Mathf.Max(5, data.terrainLayers.Length)];
            for (var z = 0; z < AlphamapResolution; z++)
            {
                for (var x = 0; x < AlphamapResolution; x++)
                {
                    for (var l = 0; l < 5; l++)
                    {
                        weights[z, x, l] = canonical[z, x, l];
                    }
                }
            }

            data.SetAlphamaps(0, 0, weights);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();

            var ground = roots.SelectMany(r => r.GetComponentsInChildren<Terrain>()).Single();
            ground.transform.position = new Vector3(-Size * 0.5f, 0f, -Size * 0.5f);
            var water = roots.Single(r => r.name == "Water");
            water.transform.position = new Vector3(0f, VeilmouthScene.SeaLevel, 0f);
            water.transform.localScale = new Vector3(Size / 10f, 1f, Size / 10f);

            var player = roots.FirstOrDefault(r => r.GetComponentInChildren<CharacterController>() != null);
            foreach (var old in roots.Where(r => r.name == "C0 Map" || r.name == "C1 Map"))
            {
                Object.DestroyImmediate(old);
            }

            VeilmouthScene.CreateZones();
            if (player != null)
            {
                player.transform.position = VeilmouthScene.Spawn;
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[VeilmouthGround] shaped from C2: 1000 × 1000 m; the ground is the owner's from here on.");
        }

        /// <summary>Heights in metres on the heightmap's lattice, [z, x], z north and x east.</summary>
        public static float[,] Heights()
        {
            var heights = new float[HeightmapResolution, HeightmapResolution];
            var step = Size / (HeightmapResolution - 1);
            for (var z = 0; z < HeightmapResolution; z++)
            {
                for (var x = 0; x < HeightmapResolution; x++)
                {
                    heights[z, x] = Mathf.Min(Height(x * step, Size - (z * step)), VeilmouthScene.HeightBudget - 1f);
                }
            }

            return heights;
        }

        /// <summary>The designed height at a plan point.</summary>
        public static float Height(float px, float py)
        {
            var h = InCity(px, py) ? City(px, py) : Natural(px, py);
            h += Edge(px, py);
            h = River(px, py, h);
            return Sea(px, py, h);
        }

        public static bool InCity(float px, float py) => px >= CityWest && px <= CityEast && py >= CityNorth && py <= CitySouth;

        /// <summary>The coastal plain at the low level, rising to the upland north of the Ridge's line, with the ore hill.</summary>
        private static float Natural(float px, float py)
        {
            var h = Low + ((Ridge - Low) * Smooth(710f, 650f, py)) + (4f * Smooth(640f, 540f, py));
            var amplitude = 0.5f + (3.5f * Smooth(560f, 300f, py));
            amplitude *= Smooth(0f, 40f, DistanceToCity(px, py)) * (1f - (0.8f * Roads(px, py)));
            h += Noise(px, py) * amplitude;

            var d = Mathf.Sqrt(Sq((px - 920f) / 75f) + Sq((py - 360f) / 170f));
            if (d < 1f)
            {
                var bump = Smooth(1f, 0.1f, d);
                h += ((HillTop - Ridge) * bump) + (3f * bump * Noise(px * 2.3f, py * 2.3f));
            }

            return h;
        }

        /// <summary>The Ridge 4 m up behind its retaining wall; Crown Avenue's 10 % climb; the Soot Stair's causeway.</summary>
        private static float City(float px, float py)
        {
            var h = py < RidgeEdge ? Ridge : Low;

            var avenue = Low + ((Ridge - Low) * Mathf.Clamp01((AvenueFoot - py) / (AvenueFoot - AvenueHead)));
            if (py >= 640f && py <= AvenueFoot)
            {
                var w = 1f - Smooth(28f, 40f, Mathf.Abs(px - AvenueX));
                h = Mathf.Lerp(h, avenue, w);
            }

            if (px >= StairWest && px <= StairEast && py >= StairTop && py <= StairFoot)
            {
                h = Ridge - ((Ridge - Low) * (py - StairTop) / (StairFoot - StairTop));
            }

            return h;
        }

        /// <summary>The area's north, west, and east edges rise steeper than 45°, with a pass for each road out.</summary>
        private static float Edge(float px, float py)
        {
            var d = Mathf.Min(px, Mathf.Min(Size - px, py));
            return 45f * Smooth(45f, 5f, d) * Smooth(840f, 760f, py) * (1f - (0.85f * Roads(px, py)));
        }

        /// <summary>A 40 m channel, its bed at 21 m: stone embankments in the city, banks of 25 m outside it.</summary>
        private static float River(float px, float py, float h)
        {
            var inBasin = px >= 740f && px <= 860f && py >= 790f && py <= CitySouth;
            if ((px >= RiverWest && px <= RiverEast) || inBasin)
            {
                return py > 790f ? Mathf.Lerp(RiverBed, SeaBed, (py - 790f) / 40f) : RiverBed;
            }

            var walled = py >= CityNorth && px < RiverWest;
            var distance = px < RiverWest ? RiverWest - px : px - RiverEast;
            return walled ? h : Mathf.Lerp(RiverBed, h, Smooth(0f, 25f, distance));
        }

        /// <summary>The sea south of 830: quay walls along the city and the basin, a beach elsewhere.</summary>
        private static float Sea(float px, float py, float h)
        {
            var bed = SeaBed - (1.5f * Smooth(850f, Size, py));
            var quay = px >= CityWest && px <= 860f;
            if (quay)
            {
                return py > CitySouth ? bed : h;
            }

            return Mathf.Lerp(h, bed, Smooth(812f, 850f, py));
        }

        /// <summary>Weights for the five canonical layers (grass, sand, soil, cliff, rock), by priority cover.</summary>
        public static float[,,] Paint(float[,] heights, int resolution = AlphamapResolution)
        {
            var maps = new float[resolution, resolution, 5];
            var cell = Size / (HeightmapResolution - 1);
            for (var z = 0; z < resolution; z++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var hx = Mathf.RoundToInt(x * (HeightmapResolution - 1f) / (resolution - 1f));
                    var hz = Mathf.RoundToInt(z * (HeightmapResolution - 1f) / (resolution - 1f));
                    var h = heights[hz, hx];
                    var gx = (heights[hz, Mathf.Min(hx + 1, HeightmapResolution - 1)] - heights[hz, Mathf.Max(hx - 1, 0)]) / (2f * cell);
                    var gz = (heights[Mathf.Min(hz + 1, HeightmapResolution - 1), hx] - heights[Mathf.Max(hz - 1, 0), hx]) / (2f * cell);
                    var slope = Mathf.Atan(Mathf.Sqrt((gx * gx) + (gz * gz))) * Mathf.Rad2Deg;

                    var px = hx * cell;
                    var py = Size - (hz * cell);
                    var w = Weights(px, py, h, slope);
                    for (var l = 0; l < 5; l++)
                    {
                        maps[z, x, l] = w[l];
                    }
                }
            }

            return maps;
        }

        private static float[] Weights(float px, float py, float h, float slope)
        {
            var w = new float[5];
            const int grass = 0, sand = 1, soil = 2, cliff = 3, rock = 4;
            if (slope > 40f)
            {
                w[h > 45f ? rock : cliff] = 1f;
            }
            else if (h < 25.2f)
            {
                w[sand] = 1f;
            }
            else if (InCity(px, py))
            {
                var garden = py < RidgeEdge ? 0.7f : 0f;
                w[grass] = garden;
                w[soil] = 1f - garden;
            }
            else if (Roads(px, py) > 0.5f)
            {
                w[soil] = 1f;
            }
            else if (h > 50f || Edge(px, py) > 8f)
            {
                w[rock] = 0.7f;
                w[grass] = 0.3f;
            }
            else if (px < 260f && py > 480f)
            {
                var strip = Mathf.Sin(py / 9f) > 0f ? 0.85f : 0.2f; // field strips
                w[soil] = strip;
                w[grass] = 1f - strip;
            }
            else if (py < 470f)
            {
                var floor = 0.25f + (0.25f * Noise(px * 3f, py * 3f));
                w[soil] = floor;
                w[grass] = 1f - floor;
            }
            else
            {
                w[grass] = 1f;
            }

            return w;
        }

        /// <summary>1 on the West road, the North road, and the East road past the bridge; 0 from 20 m away.</summary>
        private static float Roads(float px, float py)
        {
            var west = px < CityWest ? 1f - Smooth(4f, 20f, Mathf.Abs(py - 735f)) : 0f;
            var east = px > RiverEast ? 1f - Smooth(4f, 20f, Mathf.Abs(py - 735f)) : 0f;
            var north = py < CityNorth ? 1f - Smooth(4f, 20f, Mathf.Abs(px - 747f)) : 0f;
            return Mathf.Max(west, Mathf.Max(east, north));
        }

        private static float DistanceToCity(float px, float py)
        {
            var dx = Mathf.Max(CityWest - px, Mathf.Max(0f, px - CityEast));
            var dy = Mathf.Max(CityNorth - py, Mathf.Max(0f, py - CitySouth));
            return Mathf.Sqrt((dx * dx) + (dy * dy));
        }

        /// <summary>Three octaves of Perlin noise in −1…1 over 120 m; deterministic, with fixed offsets.</summary>
        private static float Noise(float px, float py)
        {
            var sum = 0f;
            var amplitude = 0.55f;
            var frequency = 1f / 120f;
            for (var o = 0; o < 3; o++)
            {
                sum += amplitude * ((Mathf.PerlinNoise((px * frequency) + 31.7f + (o * 17f), (py * frequency) + 7.3f + (o * 11f)) * 2f) - 1f);
                amplitude *= 0.5f;
                frequency *= 2.1f;
            }

            return sum;
        }

        /// <summary>Smoothstep from <paramref name="from"/> (0) to <paramref name="to"/> (1); either direction.</summary>
        private static float Smooth(float from, float to, float v)
        {
            var t = Mathf.Clamp01((v - from) / (to - from));
            return t * t * (3f - (2f * t));
        }

        private static float Sq(float v) => v * v;
    }
}

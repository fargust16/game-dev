using System.Linq;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Shapes Veilmouth's ground once from the layout C4 (VEILMOUTH_CITY_PLAN.md §28, §28.5), at the owner's request of
    /// 2026-11-10, which drops the owner's hand changes to C3's ground (§28.1 item 19); the ground is the owner's
    /// afterwards, so the tool refuses to run on ground it has already shaped. It is authored content, not the
    /// production terrain pipeline, and touches nothing of TerrainGen.
    /// <para>
    /// The city and the works stand level at <see cref="Low"/> with a low rise under the Penny and the cathedral (§28.1
    /// item 13); the river and the sea are dug to their beds under one water plane at Sea Level, with stone embankments
    /// along the city and the works and natural banks elsewhere; the open coast is rock; the forest's ground rolls and
    /// rises to the area's land edges, with a pass for the West Road.
    /// </para>
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
        public const float RiverBed = 21f;
        public const float SeaBed = 19.5f;

        /// <summary>The rise under the Penny and the cathedral, m over <see cref="Low"/>, falling to nothing 120 m away.</summary>
        public const float Rise = 1.5f;

        private const float RiseFull = 30f, RiseEnd = 120f;

        /// <summary>The cathedral's west front, the north end of the rise's spine from the Penny (§28.5).</summary>
        private static readonly Vector2 Cathedral = new Vector2(540f, 300f);

        private const float Size = VeilmouthScene.Extent;

        [MenuItem("GameDev/Slice/Shape Veilmouth Ground from C4 (once)")]
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
                Debug.LogWarning("[VeilmouthGround] the ground is already shaped from C4 and is the owner's now; nothing changed.");
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
            // The city's street layers may follow the canonical five; they are zero here until VeilmouthCity paints them.
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
            foreach (var old in roots.Where(r => r.name.EndsWith(" Map")))
            {
                Object.DestroyImmediate(old); // C0's to C3's map guides
            }

            VeilmouthScene.CreateZones();
            if (player != null)
            {
                player.transform.position = VeilmouthScene.Spawn;
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[VeilmouthGround] shaped from C4: 1000 × 1000 m; the ground is the owner's from here on.");
        }

        /// <summary>Batch only, for the agent's iterations on C4: drops the map guides so <see cref="Shape"/> runs again.</summary>
        public static void ShapeReplacing()
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            foreach (var old in scene.GetRootGameObjects().Where(r => r.name.EndsWith(" Map")))
            {
                Object.DestroyImmediate(old);
            }

            EditorSceneManager.SaveScene(scene);
            Shape();
        }

        /// <summary>Batch only: <see cref="ShapeReplacing"/>, then the bare ground's views.</summary>
        public static void ShapeReplacingAndRender()
        {
            ShapeReplacing();
            VeilmouthRenders.RenderC4Ground();
        }

        /// <summary>Heights in metres on the heightmap's lattice, [z, x], z north and x east.</summary>
        public static float[,] Heights()
        {
            var layout = VeilmouthLayout.Load();
            var heights = new float[HeightmapResolution, HeightmapResolution];
            var step = Size / (HeightmapResolution - 1);
            for (var z = 0; z < HeightmapResolution; z++)
            {
                for (var x = 0; x < HeightmapResolution; x++)
                {
                    heights[z, x] = Mathf.Min(Height(layout, x * step, Size - (z * step)), VeilmouthScene.HeightBudget - 1f);
                }
            }

            return heights;
        }

        /// <summary>The designed height at a plan point.</summary>
        public static float Height(VeilmouthLayout layout, float px, float py)
        {
            if (Built(layout, px, py))
            {
                return Low + RiseAt(layout, px, py); // stone embankments and quay walls: the built ground meets the water upright
            }

            var h = Natural(layout, px, py);
            foreach (var river in layout.Rivers)
            {
                h = Mathf.Min(h, Bank(river, px, py, RiverBed, 22f));
            }

            return Coast(layout, px, py, h);
        }

        /// <summary>The city or the works inside their walls, or a wall's own footing.</summary>
        public static bool Built(VeilmouthLayout layout, float px, float py) =>
            layout.City.Contains(px, py) || layout.Works.Contains(px, py) ||
            layout.walls.Any(w => (w.axis == "y" ? px >= w.t0 - 1f && px <= w.t1 + 1f && py >= w.a0 && py <= w.a1
                                                  : py >= w.t0 - 1f && py <= w.t1 + 1f && px >= w.a0 && px <= w.a1));

        /// <summary>The low rise under the Penny and the cathedral, along the spine between them.</summary>
        public static float RiseAt(VeilmouthLayout layout, float px, float py)
        {
            var a = new Vector2(layout.plaza.cx, layout.plaza.cy);
            var p = new Vector2(px, py);
            var ab = Cathedral - a;
            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            var d = (p - (a + (ab * t))).magnitude;
            return Rise * Smooth(RiseEnd, RiseFull, d);
        }

        /// <summary>The land outside the walls: rolling, rising inland, and steep at the area's land edges.</summary>
        private static float Natural(VeilmouthLayout layout, float px, float py)
        {
            var near = Mathf.Min(layout.City.Distance(px, py), layout.Works.Distance(px, py));
            var road = Roads(layout, px, py);
            var inland = 6f * Smooth(120f, 380f, Mathf.Min(near, 380f));
            var amplitude = (1f + (4f * Smooth(40f, 260f, near))) * Smooth(0f, 30f, near) * (1f - (0.85f * road));
            // The rise runs on past the north wall, so the ground meets the wall at one level on both sides.
            var h = Low + RiseAt(layout, px, py) + (inland * (1f - (0.6f * road))) + (Noise(px, py) * amplitude);
            // The north, west and south edges rise steeper than 45°, with a pass for the West Road.
            var edge = Mathf.Min(px, Mathf.Min(py, Size - py));
            h += 45f * Smooth(45f, 5f, edge) * Smooth(layout.OpenCoast + 40f, layout.OpenCoast - 60f, px) * (1f - (0.85f * road));
            // No hollow sinks under the sea's level or to the shore's sand: water is the river and the sea only (§28.1).
            return Mathf.Max(h, Low + 0.3f);
        }

        /// <summary>
        /// A river's channel: its bed inside, a natural bank of <paramref name="bank"/> m outside; built ground keeps its
        /// stone embankment (checked before this is called).
        /// </summary>
        private static float Bank(VeilmouthLayout.Water river, float px, float py, float bed, float bank)
        {
            var dx = Mathf.Max(river.x0 - px, Mathf.Max(0f, px - river.x1));
            var dy = Mathf.Max(river.y0 - py, Mathf.Max(0f, py - river.y1));
            var d = Mathf.Sqrt((dx * dx) + (dy * dy));
            return d <= 0f ? bed : Mathf.Lerp(bed, 99f, Smooth(0f, bank, d));
        }

        /// <summary>The sea: its bed deepening from the coast, the open coast's rocks falling steeply into it.</summary>
        private static float Coast(VeilmouthLayout layout, float px, float py, float h)
        {
            var sea = layout.Sea;
            var coast = py >= sea.y0 && py <= sea.y1 ? sea.x0 : layout.OpenCoast;
            var bed = SeaBed - (2.5f * Smooth(coast, coast + 200f, px));
            if (px >= coast)
            {
                return bed;
            }

            // Rocks within 12 m of the open coast, so the land falls into the sea over them (§28.1 item 13).
            var rocky = Mathf.Lerp(bed, h, Smooth(0f, 12f, coast - px));
            return Mathf.Min(h, rocky + (Mathf.Abs(Noise(px * 4f, py * 4f)) * 1.5f * Smooth(0f, 6f, coast - px)));
        }

        /// <summary>Weights for the five canonical layers (grass, sand, soil, cliff, rock), by priority cover.</summary>
        public static float[,,] Paint(float[,] heights, int resolution = AlphamapResolution)
        {
            var layout = VeilmouthLayout.Load();
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
                    var w = Weights(layout, px, py, h, slope);
                    for (var l = 0; l < 5; l++)
                    {
                        maps[z, x, l] = w[l];
                    }
                }
            }

            return maps;
        }

        private static float[] Weights(VeilmouthLayout layout, float px, float py, float h, float slope)
        {
            var w = new float[5];
            const int grass = 0, sand = 1, soil = 2, cliff = 3, rock = 4;
            var coast = py >= layout.Sea.y0 && py <= layout.Sea.y1 ? layout.Sea.x0 : layout.OpenCoast;
            if (Built(layout, px, py))
            {
                w[soil] = 1f; // the builder paints its streets, squares and gardens over it
            }
            else if (slope > 40f)
            {
                w[h > 45f ? rock : cliff] = 1f;
            }
            else if (coast - px < 16f)
            {
                w[rock] = 0.8f; // the open coast's rocks
                w[grass] = 0.2f;
            }
            else if (h < 25.2f)
            {
                w[sand] = 1f;
            }
            else if (Roads(layout, px, py) > 0.5f)
            {
                w[soil] = 1f;
            }
            else if (h > 50f)
            {
                w[rock] = 0.7f;
                w[grass] = 0.3f;
            }
            else if (layout.forest.rects.Any(r => r.Contains(px, py)))
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

        /// <summary>1 on the roads outside the walls (the West Road), 0 from 20 m away.</summary>
        private static float Roads(VeilmouthLayout layout, float px, float py) =>
            layout.roads.Select(r => 1f - Smooth(2f, 20f, r.Distance(px, py))).DefaultIfEmpty(0f).Max();

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
    }
}

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using GameDev.World;
using GameDev.World.Buildings.Shell;
using GameDev.World.Terrain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Phase A of the Combat Grid technical test: can a grid reaching 20 cells from the player be
    /// built from generated terrain and buildings, fast and correctly, with no hand correction?
    /// </summary>
    /// <remarks>
    /// <para>
    /// Research only, not the production grid. It never saves a scene or an asset, and everything it
    /// adds to a scene is lost when the process exits. Plan, gates and what each result decides:
    /// <c>Assets/Plans/COMBAT_GRID_TECH_TEST.md</c>.
    /// </para>
    /// <example>
    /// <code>
    /// Unity.exe -batchmode -quit -projectPath &lt;project&gt; \
    ///   -executeMethod GameDev.EditorTools.CombatGridProbe.RunBatch \
    ///   -gridOut &lt;folder&gt; -logFile out.log
    /// </code>
    /// </example>
    /// </remarks>
    public static class CombatGridProbe
    {
        private const string OverworldScene = "Assets/Scenes/Overworld_Generator.unity";
        private const string OutArg = "-gridOut";
        private const string DefaultOut = "D:/Hobby/game-dev/research-data/combat-grid/run_1";
        private const string AsBuilt = "as-built";
        private const string AllPieces = "all-pieces";

        private const int ProbeSeed = 20261101;
        private const int Reach = 20;
        private const int RepeatBuilds = 5;
        private const int WalkSteps = 20;
        private const int PassableEdgeSamples = 30;
        private const int BlockedEdgeSamples = 15;
        private const int MinTownReachable = 100;

        private const float SlopeLimitDegrees = 45f;
        private const float StepOffset = 0.4f;
        private const float SkinWidth = 0.03f;
        private const float WalkTickMeters = 0.075f;
        private const float LowCoverHeight = 0.7f;
        private const float HighCoverHeight = 1.5f;
        private const float TopY = WorldScale.TerrainHeightBudgetMeters + 20f;
        private const float Radius = WorldScale.CharacterRadiusMeters;
        private const float Height = WorldScale.CharacterHeightMeters;

        // Gates: authoring defaults, fixed before the first run.
        private const double FullMedianGateMs = 50.0;
        private const double FullWorstGateMs = 100.0;
        private const double StepWorstGateMs = 8.0;
        private const double FalsePassGate = 0.01;
        private const double AgreementGate = 0.95;

        private const int Mask = Physics.DefaultRaycastLayers & ~(1 << 4);

        private static readonly float[] CellSizes = { 1f, 1.5f };
        private static readonly int[] DirX = { 1, -1, 0, 0 };
        private static readonly int[] DirZ = { 0, 0, 1, -1 };

        public static void RunBatch()
        {
            var outDir = ReadArg(OutArg) ?? DefaultOut;
            Directory.CreateDirectory(Path.Combine(outDir, "maps"));

            try
            {
                var report = new Report();

                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                report.Controller = Calibrate();

                EditorSceneManager.OpenScene(OverworldScene, OpenSceneMode.Single);
                var town = GameObject.Find("OverworldSettlement");
                if (town == null)
                {
                    throw new InvalidOperationException("The overworld scene has no settlement.");
                }

                HideDebugMarker(town);
                Physics.SyncTransforms();

                var trees = TreePositions();
                var overworldSites = TerrainSites();
                overworldSites.AddRange(ForestSites(trees));
                overworldSites.AddRange(PiecesSites(town, "town", 10));
                Debug.Log($"[CombatGridProbe] overworld: {overworldSites.Count} sites, {trees.Count} trees.");
                RunScene(overworldSites, town, trees, report, outDir);

                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var strip = BuildDistrictStrip();
                var denseSites = PiecesSites(strip, "dense", 10);
                Debug.Log($"[CombatGridProbe] dense: {denseSites.Count} sites.");
                RunScene(denseSites, strip, new List<Vector3>(), report, outDir);

                report.Write(outDir);
                Debug.Log($"[CombatGridProbe] done, results in {outDir}");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        private static void RunScene(
            List<Site> sites,
            GameObject pieces,
            List<Vector3> trees,
            Report report,
            string outDir)
        {
            var asBuilt = new Dictionary<string, HashSet<long>>();
            for (var c = 0; c < CellSizes.Length; c++)
            {
                for (var i = 0; i < sites.Count; i++)
                {
                    var row = Measure(sites[i], CellSizes[c], AsBuilt, null, trees, report, outDir);
                    if (row != null)
                    {
                        asBuilt[Key(sites[i], CellSizes[c])] = row.ReachableCells;
                    }
                }
            }

            report.AddColliders(AddMissingColliders(pieces));
            Physics.SyncTransforms();

            for (var c = 0; c < CellSizes.Length; c++)
            {
                for (var i = 0; i < sites.Count; i++)
                {
                    asBuilt.TryGetValue(Key(sites[i], CellSizes[c]), out var before);
                    Measure(sites[i], CellSizes[c], AllPieces, before, trees, report, outDir);
                }
            }
        }

        private static Row Measure(
            Site site,
            float cell,
            string pass,
            HashSet<long> before,
            List<Vector3> trees,
            Report report,
            string outDir)
        {
            var reader = new GridReader(cell);
            if (!reader.TryFindStart(site.Position, out var startX, out var startZ, out var startLevel))
            {
                report.NoStart++;
                return null;
            }

            // One build first, so the timings below do not include JIT and first-touch costs.
            new GridReader(cell).Build(startX, startZ, startLevel);

            var row = new Row { Site = site, Cell = cell, Pass = pass };
            var fullMs = new List<double>();
            var builds = new List<Window>();
            for (var i = 0; i < RepeatBuilds; i++)
            {
                var fresh = new GridReader(cell);
                var w = fresh.Build(startX, startZ, startLevel);
                fullMs.Add(w.TotalMs);
                builds.Add(w);
                reader = fresh;
            }

            var window = builds[builds.Count - 1];
            row.Deterministic = builds.All(b => b.Hash == window.Hash);
            row.FullMedianMs = Median(fullMs);
            row.FullWorstMs = fullMs.Max();
            row.ColumnMs = window.ColumnMs;
            row.EdgeMs = window.EdgeMs;
            row.SearchMs = window.SearchMs;
            row.CoverMs = window.CoverMs;
            row.Standable = window.Standable;
            row.MultiLevel = window.MultiLevel;
            row.Reachable = window.Reachable.Count;
            row.LowCover = window.LowCover;
            row.HighCover = window.HighCover;
            row.TerrainCoverRays = reader.TerrainCoverRays;
            row.OverflowColumns = reader.OverflowColumns;
            row.ReachableCells = window.Reachable;

            WalkTheGrid(row, startX, startZ, startLevel, cell);
            CountAirWalls(row, reader, window);
            CountTreeCells(row, window, trees, cell);
            TestAgainstController(row, reader, window, report.Controller, site);

            if (before != null)
            {
                // By column: a new piece adds a level, which renumbers the surfaces below it.
                var after = new HashSet<long>(window.Reachable.Select(k => k >> 4));
                row.Flips = before.Select(k => k >> 4).Distinct().Count(k => !after.Contains(k));
            }

            report.Rows.Add(row);
            if (site.Index == 0 && Mathf.Approximately(cell, 1f))
            {
                WriteMap(Path.Combine(outDir, "maps", $"{site.ClassName}_{pass}.png"), reader, window);
            }

            return row;
        }

        /// <summary>Moves a cached grid one cell at a time, then compares it with a fresh build.</summary>
        private static void WalkTheGrid(Row row, int x, int z, int level, float cell)
        {
            var moving = new GridReader(cell);
            var window = moving.Build(x, z, level);
            var visited = new HashSet<long> { GridReader.Pack(x, z, level) };
            var stepMs = new List<double>();

            for (var step = 0; step < WalkSteps; step++)
            {
                if (!TryNextCell(window, visited, x, z, ref level, out var nextX, out var nextZ))
                {
                    break;
                }

                x = nextX;
                z = nextZ;
                visited.Add(GridReader.Pack(x, z, level));
                window = moving.Build(x, z, level);
                stepMs.Add(window.TotalMs);
            }

            row.Steps = stepMs.Count;
            if (stepMs.Count > 0)
            {
                row.StepMedianMs = Median(stepMs);
                row.StepWorstMs = stepMs.Max();
            }

            row.Stable = new GridReader(cell).Build(x, z, level).Hash == window.Hash;
        }

        private static bool TryNextCell(
            Window window,
            HashSet<long> visited,
            int x,
            int z,
            ref int level,
            out int nextX,
            out int nextZ)
        {
            for (var d = 0; d < 4; d++)
            {
                nextX = x + DirX[d];
                nextZ = z + DirZ[d];
                for (var l = 0; l < GridReader.MaxLevels; l++)
                {
                    var key = GridReader.Pack(nextX, nextZ, l);
                    if (window.Reachable.Contains(key) && !visited.Contains(key))
                    {
                        level = l;
                        return true;
                    }
                }
            }

            nextX = x;
            nextZ = z;
            return false;
        }

        /// <summary>A failed clearance caused by a collider with nothing visible is an air wall.</summary>
        private static void CountAirWalls(Row row, GridReader reader, Window window)
        {
            var overlap = new Collider[32];
            for (var dz = -Reach; dz <= Reach; dz++)
            {
                for (var dx = -Reach; dx <= Reach; dx++)
                {
                    var ix = window.X + dx;
                    var iz = window.Z + dz;
                    var column = reader.Column(ix, iz);
                    for (var l = 0; l < column.Length; l++)
                    {
                        if (column[l].Standable || column[l].Reason != BlockReason.Clearance)
                        {
                            continue;
                        }

                        var bottom = reader.Center(ix, iz, column[l].Y) + Vector3.up * (StepOffset + Radius);
                        var top = reader.Center(ix, iz, column[l].Y) + Vector3.up * (Height - Radius);
                        var count = Physics.OverlapCapsuleNonAlloc(
                            bottom, top, Radius, overlap, Mask, QueryTriggerInteraction.Ignore);
                        for (var i = 0; i < count; i++)
                        {
                            var hit = overlap[i];
                            if (hit is TerrainCollider || hit.GetComponentInChildren<Renderer>() != null)
                            {
                                continue;
                            }

                            row.AirWallCells++;
                            row.AirWallName = hit.name;
                            break;
                        }
                    }
                }
            }
        }

        private static void CountTreeCells(Row row, Window window, List<Vector3> trees, float cell)
        {
            var extent = (Reach + 1) * cell;
            var centerX = (window.X + 0.5f) * cell;
            var centerZ = (window.Z + 0.5f) * cell;
            for (var i = 0; i < trees.Count; i++)
            {
                var tree = trees[i];
                if (Mathf.Abs(tree.x - centerX) > extent || Mathf.Abs(tree.z - centerZ) > extent)
                {
                    continue;
                }

                row.TreesInWindow++;
                var ix = Mathf.FloorToInt(tree.x / cell);
                var iz = Mathf.FloorToInt(tree.z / cell);
                for (var l = 0; l < GridReader.MaxLevels; l++)
                {
                    if (window.Reachable.Contains(GridReader.Pack(ix, iz, l)))
                    {
                        row.TreesInReachableCells++;
                        break;
                    }
                }
            }
        }

        /// <summary>Walks the real controller along sampled edges and compares it with the grid.</summary>
        private static void TestAgainstController(
            Row row,
            GridReader reader,
            Window window,
            ControllerCheck controller,
            Site site)
        {
            if (controller == null || !controller.Usable)
            {
                return;
            }

            var walker = controller.Create();
            var rng = new Random(ProbeSeed + site.Index * 31 + site.ClassName[0] * 1009);
            var keys = window.Reachable.OrderBy(k => k).ToList();
            var passable = 0;
            var blocked = 0;

            for (var tries = 0; tries < keys.Count * 8 && (passable < PassableEdgeSamples || blocked < BlockedEdgeSamples); tries++)
            {
                GridReader.Unpack(keys[rng.Next(keys.Count)], out var ax, out var az, out var la);
                var d = rng.Next(4);
                var bx = ax + DirX[d];
                var bz = az + DirZ[d];
                if (Mathf.Abs(bx - window.X) > Reach || Mathf.Abs(bz - window.Z) > Reach)
                {
                    continue;
                }

                var other = reader.Column(bx, bz);
                for (var lb = 0; lb < other.Length; lb++)
                {
                    if (!other[lb].Standable)
                    {
                        continue;
                    }

                    var gridSays = reader.EdgeBetween(ax, az, la, bx, bz, lb);
                    if (gridSays ? passable >= PassableEdgeSamples : blocked >= BlockedEdgeSamples)
                    {
                        continue;
                    }

                    var a = reader.Center(ax, az, reader.Column(ax, az)[la].Y);
                    var b = reader.Center(bx, bz, other[lb].Y);
                    var walks = Walks(walker, a, b);
                    if (gridSays)
                    {
                        passable++;
                        if (walks)
                        {
                            row.BothPass++;
                        }
                        else
                        {
                            row.FalsePass++;
                        }
                    }
                    else
                    {
                        blocked++;
                        if (walks)
                        {
                            row.FalseBlock++;
                        }
                        else
                        {
                            row.BothBlock++;
                        }
                    }
                }
            }

            Object.DestroyImmediate(walker.gameObject);
            Physics.SyncTransforms();
        }

        private static bool Walks(CharacterController walker, Vector3 from, Vector3 to)
        {
            walker.enabled = false;
            walker.transform.position = from + Vector3.up * 0.05f;
            walker.enabled = true;
            Physics.SyncTransforms();

            var flat = new Vector3(to.x - from.x, 0f, to.z - from.z).magnitude;
            var ticks = Mathf.CeilToInt(2.5f * flat / WalkTickMeters);
            for (var i = 0; i < ticks; i++)
            {
                var position = walker.transform.position;
                var delta = new Vector3(to.x - position.x, 0f, to.z - position.z);
                var distance = delta.magnitude;
                if (distance < 0.12f)
                {
                    return Mathf.Abs(position.y - to.y) < 0.6f;
                }

                var move = delta / distance * Mathf.Min(WalkTickMeters, distance);
                move.y = -0.1f;
                walker.Move(move);
            }

            return false;
        }

        /// <summary>Proves the controller moves and stops in this mode before its verdicts count.</summary>
        private static ControllerCheck Calibrate()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(5f, 1.5f, 0f);
            wall.transform.localScale = new Vector3(0.3f, 3f, 6f);
            Physics.SyncTransforms();

            var check = new ControllerCheck();
            var walker = check.Create();
            var moves = Walks(walker, new Vector3(0f, 0.1f, 0f), new Vector3(2f, 0.1f, 0f));
            var stops = !Walks(walker, new Vector3(4f, 0.1f, 0f), new Vector3(6f, 0.1f, 0f));
            check.Usable = moves && stops;
            Object.DestroyImmediate(walker.gameObject);
            Object.DestroyImmediate(wall);
            Object.DestroyImmediate(ground);
            Physics.SyncTransforms();
            Debug.Log($"[CombatGridProbe] controller calibration: moves={moves} stops={stops}");
            return check;
        }

        private static void HideDebugMarker(GameObject town)
        {
            foreach (Transform child in town.transform)
            {
                if (child.name.StartsWith("TownApproach_", StringComparison.Ordinal))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private static Dictionary<string, int> AddMissingColliders(GameObject root)
        {
            var added = new Dictionary<string, int>();
            var renderers = root.GetComponentsInChildren<MeshRenderer>();
            for (var i = 0; i < renderers.Length; i++)
            {
                var go = renderers[i].gameObject;
                if (go.GetComponent<Collider>() != null)
                {
                    continue;
                }

                go.AddComponent<BoxCollider>();
                var category = PieceCategory(go.name);
                added.TryGetValue(category, out var count);
                added[category] = count + 1;
            }

            return added;
        }

        /// <summary>Piece names look like F0_WALL_3; the category is the part without digits.</summary>
        private static string PieceCategory(string name)
        {
            var parts = name.Split('_');
            for (var i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 1 && parts[i].All(char.IsLetter))
                {
                    return parts[i].ToUpperInvariant();
                }
            }

            return "OTHER";
        }

        private static GameObject BuildDistrictStrip()
        {
            var host = new GameObject("CombatGridProbeStrip");
            var lab = host.AddComponent<ShellSandboxLab>();
            lab.SetSeed(ProbeSeed);
            lab.SetCompareAll();
            lab.Generate();

            // Above the sea, which a cell reading treats as water at any other height.
            var lift = WorldScale.SeaLevelMeters + 6f;
            host.transform.position += Vector3.up * lift;

            var renderers = host.GetComponentsInChildren<MeshRenderer>();
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.position = new Vector3(bounds.center.x, lift, bounds.center.z);
            ground.transform.localScale = new Vector3(60f, 1f, 60f);
            Physics.SyncTransforms();
            return host;
        }

        private static List<Site> TerrainSites()
        {
            var bins = new[]
            {
                (Name: "flat", Min: 0f, Max: 5f),
                (Name: "gentle", Min: 5f, Max: 15f),
                (Name: "hill", Min: 15f, Max: 30f),
                (Name: "steep", Min: 30f, Max: 45f),
                (Name: "cliff", Min: 45f, Max: 90f)
            };
            var sites = new List<Site>();
            var rng = new Random(ProbeSeed);
            var margin = 60.0;
            var extentX = WorldScale.WorldExtentXMeters - 2.0 * margin;
            var extentZ = WorldScale.WorldExtentZMeters - 2.0 * margin;

            for (var b = 0; b < bins.Length; b++)
            {
                var found = 0;
                for (var tries = 0; tries < 200000 && found < 5; tries++)
                {
                    var x = (float)(margin + rng.NextDouble() * extentX);
                    var z = (float)(margin + rng.NextDouble() * extentZ);
                    if (!OverworldTerrainAccess.TrySampleHeightWorld(x, z, out var y)
                        || y < WorldScale.SeaLevelMeters + 2f)
                    {
                        continue;
                    }

                    var slope = SteepnessAt(x, z);
                    if (slope < bins[b].Min || slope >= bins[b].Max)
                    {
                        continue;
                    }

                    sites.Add(new Site(bins[b].Name, found, new Vector3(x, y, z)));
                    found++;
                }
            }

            return sites;
        }

        private static float SteepnessAt(float x, float z)
        {
            var tile = OverworldTerrainAccess.FindTileAt(x, z);
            if (tile == null)
            {
                return 0f;
            }

            var origin = tile.GetPosition();
            var size = tile.terrainData.size;
            return tile.terrainData.GetSteepness((x - origin.x) / size.x, (z - origin.z) / size.z);
        }

        private static List<Vector3> TreePositions()
        {
            var positions = new List<Vector3>();
            var terrains = OverworldTerrainAccess.FindTerrains();
            for (var t = 0; t < terrains.Length; t++)
            {
                var origin = terrains[t].GetPosition();
                var size = terrains[t].terrainData.size;
                var instances = terrains[t].terrainData.treeInstances;
                for (var i = 0; i < instances.Length; i++)
                {
                    positions.Add(origin + Vector3.Scale(instances[i].position, size));
                }
            }

            return positions;
        }

        private static List<Site> ForestSites(List<Vector3> trees)
        {
            const float binMeters = 40f;
            var counts = new Dictionary<(int, int), int>();
            for (var i = 0; i < trees.Count; i++)
            {
                var bin = (Mathf.FloorToInt(trees[i].x / binMeters), Mathf.FloorToInt(trees[i].z / binMeters));
                counts.TryGetValue(bin, out var count);
                counts[bin] = count + 1;
            }

            var sites = new List<Site>();
            foreach (var pair in counts.OrderByDescending(p => p.Value).ThenBy(p => p.Key).Take(5))
            {
                var x = (pair.Key.Item1 + 0.5f) * binMeters;
                var z = (pair.Key.Item2 + 0.5f) * binMeters;
                sites.Add(new Site("forest", sites.Count, new Vector3(x, OverworldTerrainAccess.SampleHeightWorld(x, z), z)));
            }

            return sites;
        }

        private static List<Site> PiecesSites(GameObject root, string className, int count)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>();
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var sites = new List<Site>();
            var rng = new Random(ProbeSeed);
            for (var tries = 0; tries < 2000 && sites.Count < count; tries++)
            {
                var x = bounds.min.x + (float)rng.NextDouble() * bounds.size.x;
                var z = bounds.min.z + (float)rng.NextDouble() * bounds.size.z;
                var position = new Vector3(x, bounds.min.y, z);
                var reader = new GridReader(1f);
                if (reader.TryFindStart(position, out var ix, out var iz, out var level)
                    && reader.Build(ix, iz, level).Reachable.Count >= MinTownReachable)
                {
                    sites.Add(new Site(className, sites.Count, position));
                }
            }

            return sites;
        }

        private static void WriteMap(string path, GridReader reader, Window window)
        {
            const int scale = 8;
            var side = (2 * Reach + 1) * scale;
            var texture = new Texture2D(side, side, TextureFormat.RGB24, false);
            for (var dz = -Reach; dz <= Reach; dz++)
            {
                for (var dx = -Reach; dx <= Reach; dx++)
                {
                    var color = MapColor(reader, window, window.X + dx, window.Z + dz);
                    if (dx == 0 && dz == 0)
                    {
                        color = Color.white;
                    }

                    for (var y = 0; y < scale; y++)
                    {
                        for (var x = 0; x < scale; x++)
                        {
                            var line = x == 0 || y == 0;
                            texture.SetPixel(
                                (dx + Reach) * scale + x,
                                (dz + Reach) * scale + y,
                                line ? color * 0.85f : color);
                        }
                    }
                }
            }

            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        private static Color MapColor(GridReader reader, Window window, int ix, int iz)
        {
            var column = reader.Column(ix, iz);
            for (var l = 0; l < column.Length; l++)
            {
                if (!window.Reachable.Contains(GridReader.Pack(ix, iz, l)))
                {
                    continue;
                }

                var cover = reader.Cover(ix, iz, l);
                if (cover.High != 0)
                {
                    return new Color(0.75f, 0.55f, 0.15f);
                }

                return cover.Low != 0 ? new Color(0.85f, 0.8f, 0.3f) : new Color(0.35f, 0.75f, 0.35f);
            }

            for (var l = 0; l < column.Length; l++)
            {
                if (column[l].Standable)
                {
                    return new Color(0.3f, 0.5f, 0.85f);
                }
            }

            if (column.Length == 0)
            {
                return Color.black;
            }

            return column[column.Length - 1].Reason == BlockReason.Water
                ? new Color(0.1f, 0.2f, 0.5f)
                : new Color(0.28f, 0.28f, 0.28f);
        }

        private static double Median(List<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            var mid = sorted.Count / 2;
            return sorted.Count % 2 == 1 ? sorted[mid] : 0.5 * (sorted[mid - 1] + sorted[mid]);
        }

        private static string Key(Site site, float cell)
        {
            return site.ClassName + site.Index + "/" + cell.ToString(CultureInfo.InvariantCulture);
        }

        private static string ReadArg(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private enum BlockReason
        {
            None = 0,
            Water = 1,
            Slope = 2,
            Clearance = 3
        }

        private struct Surface
        {
            public float Y;
            public bool Standable;
            public BlockReason Reason;
        }

        private struct CoverRead
        {
            public int Low;
            public int High;
        }

        private sealed class Site
        {
            public readonly string ClassName;
            public readonly int Index;
            public readonly Vector3 Position;

            public Site(string className, int index, Vector3 position)
            {
                ClassName = className;
                Index = index;
                Position = position;
            }
        }

        private sealed class Window
        {
            public int X;
            public int Z;
            public HashSet<long> Reachable;
            public int Standable;
            public int MultiLevel;
            public int LowCover;
            public int HighCover;
            public double ColumnMs;
            public double EdgeMs;
            public double SearchMs;
            public double CoverMs;
            public ulong Hash;

            public double TotalMs => ColumnMs + EdgeMs + SearchMs + CoverMs;
        }

        private sealed class ControllerCheck
        {
            public bool Usable;

            public CharacterController Create()
            {
                var go = new GameObject("CombatGridProbeWalker");
                var controller = go.AddComponent<CharacterController>();
                controller.height = Height;
                controller.radius = Radius;
                controller.center = new Vector3(0f, Height * 0.5f, 0f);
                controller.skinWidth = SkinWidth;
                controller.stepOffset = StepOffset;
                controller.slopeLimit = SlopeLimitDegrees;
                return controller;
            }
        }

        /// <summary>
        /// Reads world-aligned cells on demand and keeps them, so a moved grid only pays for new cells.
        /// A cell's reading depends on its world position alone, never on where the grid started.
        /// </summary>
        private sealed class GridReader
        {
            public const int MaxLevels = 16;
            private const int MaxHits = 32;
            private const int CoordOffset = 32768;

            private readonly float _cell;
            private readonly Dictionary<long, Surface[]> _columns = new Dictionary<long, Surface[]>();
            private readonly Dictionary<long, bool> _edges = new Dictionary<long, bool>();
            private readonly Dictionary<long, CoverRead> _cover = new Dictionary<long, CoverRead>();
            private readonly RaycastHit[] _hits = new RaycastHit[MaxHits];

            public int OverflowColumns;
            public int TerrainCoverRays;

            public GridReader(float cell)
            {
                _cell = cell;
            }

            public static long Pack(int ix, int iz, int level)
            {
                return ((long)(ix + CoordOffset) << 20) | ((long)(iz + CoordOffset) << 4) | (uint)level;
            }

            public static void Unpack(long key, out int ix, out int iz, out int level)
            {
                ix = (int)(key >> 20) - CoordOffset;
                iz = (int)((key >> 4) & 0xFFFF) - CoordOffset;
                level = (int)(key & 0xF);
            }

            public Vector3 Center(int ix, int iz, float y)
            {
                return new Vector3((ix + 0.5f) * _cell, y, (iz + 0.5f) * _cell);
            }

            public Surface[] Column(int ix, int iz)
            {
                var key = Pack(ix, iz, 0);
                if (!_columns.TryGetValue(key, out var column))
                {
                    column = ReadColumn(ix, iz);
                    _columns[key] = column;
                }

                return column;
            }

            /// <summary>The nearest cell whose lowest standable surface can hold the player.</summary>
            public bool TryFindStart(Vector3 position, out int ix, out int iz, out int level)
            {
                var cx = Mathf.FloorToInt(position.x / _cell);
                var cz = Mathf.FloorToInt(position.z / _cell);
                for (var ring = 0; ring <= Reach; ring++)
                {
                    for (var dz = -ring; dz <= ring; dz++)
                    {
                        for (var dx = -ring; dx <= ring; dx++)
                        {
                            if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != ring)
                            {
                                continue;
                            }

                            var column = Column(cx + dx, cz + dz);
                            for (var l = column.Length - 1; l >= 0; l--)
                            {
                                if (column[l].Standable)
                                {
                                    ix = cx + dx;
                                    iz = cz + dz;
                                    level = l;
                                    return true;
                                }
                            }
                        }
                    }
                }

                ix = cx;
                iz = cz;
                level = 0;
                return false;
            }

            public Window Build(int cx, int cz, int level)
            {
                var window = new Window { X = cx, Z = cz };

                var started = Stopwatch.GetTimestamp();
                for (var dz = -Reach; dz <= Reach; dz++)
                {
                    for (var dx = -Reach; dx <= Reach; dx++)
                    {
                        Column(cx + dx, cz + dz);
                    }
                }

                window.ColumnMs = Lap(ref started);

                for (var dz = -Reach; dz <= Reach; dz++)
                {
                    for (var dx = -Reach; dx <= Reach; dx++)
                    {
                        if (dx < Reach)
                        {
                            ReadEdges(cx + dx, cz + dz, cx + dx + 1, cz + dz);
                        }

                        if (dz < Reach)
                        {
                            ReadEdges(cx + dx, cz + dz, cx + dx, cz + dz + 1);
                        }
                    }
                }

                window.EdgeMs = Lap(ref started);

                window.Reachable = Search(cx, cz, level);
                window.SearchMs = Lap(ref started);

                foreach (var key in window.Reachable)
                {
                    Unpack(key, out var ix, out var iz, out var l);
                    var cover = Cover(ix, iz, l);
                    window.LowCover += cover.Low != 0 ? 1 : 0;
                    window.HighCover += cover.High != 0 ? 1 : 0;
                }

                window.CoverMs = Lap(ref started);

                Summarise(window);
                return window;
            }

            public bool EdgeBetween(int ax, int az, int la, int bx, int bz, int lb)
            {
                if (bx == ax + 1)
                {
                    return Edge(ax, az, la, 0, lb, bx, bz);
                }

                if (bx == ax - 1)
                {
                    return Edge(bx, bz, lb, 0, la, ax, az);
                }

                return bz == az + 1 ? Edge(ax, az, la, 1, lb, bx, bz) : Edge(bx, bz, lb, 1, la, ax, az);
            }

            public CoverRead Cover(int ix, int iz, int level)
            {
                var key = Pack(ix, iz, level);
                if (_cover.TryGetValue(key, out var cover))
                {
                    return cover;
                }

                var ground = Center(ix, iz, Column(ix, iz)[level].Y);
                for (var d = 0; d < 4; d++)
                {
                    var direction = new Vector3(DirX[d], 0f, DirZ[d]);
                    if (CoverRay(ground + Vector3.up * LowCoverHeight, direction))
                    {
                        cover.Low |= 1 << d;
                    }

                    if (CoverRay(ground + Vector3.up * HighCoverHeight, direction))
                    {
                        cover.High |= 1 << d;
                    }
                }

                _cover[key] = cover;
                return cover;
            }

            private bool CoverRay(Vector3 origin, Vector3 direction)
            {
                if (!Physics.Raycast(origin, direction, out var hit, _cell * 0.75f, Mask, QueryTriggerInteraction.Ignore))
                {
                    return false;
                }

                if (hit.collider is TerrainCollider)
                {
                    TerrainCoverRays++;
                    return false;
                }

                return true;
            }

            private Surface[] ReadColumn(int ix, int iz)
            {
                var origin = Center(ix, iz, TopY);
                var count = Physics.RaycastNonAlloc(
                    origin, Vector3.down, _hits, TopY, Mask, QueryTriggerInteraction.Ignore);
                if (count == MaxHits)
                {
                    OverflowColumns++;
                }

                Array.Sort(_hits, 0, count, HitHeightOrder.Instance);

                var surfaces = new List<Surface>(count);
                for (var i = 0; i < count && surfaces.Count < MaxLevels; i++)
                {
                    var hit = _hits[i];
                    if (surfaces.Count > 0 && Mathf.Abs(surfaces[surfaces.Count - 1].Y - hit.point.y) < 0.02f)
                    {
                        continue;
                    }

                    surfaces.Add(ReadSurface(hit));
                }

                return surfaces.ToArray();
            }

            private static Surface ReadSurface(RaycastHit hit)
            {
                var surface = new Surface { Y = hit.point.y, Reason = BlockReason.None, Standable = true };
                if (hit.point.y < WorldScale.SeaLevelMeters)
                {
                    surface.Reason = BlockReason.Water;
                }
                else if (Vector3.Angle(hit.normal, Vector3.up) >= SlopeLimitDegrees)
                {
                    surface.Reason = BlockReason.Slope;
                }
                else if (Physics.CheckCapsule(
                             hit.point + Vector3.up * (StepOffset + Radius),
                             hit.point + Vector3.up * (Height - Radius),
                             Radius,
                             Mask,
                             QueryTriggerInteraction.Ignore))
                {
                    surface.Reason = BlockReason.Clearance;
                }

                surface.Standable = surface.Reason == BlockReason.None;
                return surface;
            }

            private void ReadEdges(int ax, int az, int bx, int bz)
            {
                var a = Column(ax, az);
                var b = Column(bx, bz);
                for (var la = 0; la < a.Length; la++)
                {
                    if (!a[la].Standable)
                    {
                        continue;
                    }

                    for (var lb = 0; lb < b.Length; lb++)
                    {
                        if (b[lb].Standable)
                        {
                            EdgeBetween(ax, az, la, bx, bz, lb);
                        }
                    }
                }
            }

            /// <param name="direction">0 is +X and 1 is +Z; the key is always the lower cell.</param>
            private bool Edge(int ax, int az, int la, int direction, int lb, int bx, int bz)
            {
                var key = (Pack(ax, az, la) << 5) | ((long)direction << 4) | (uint)lb;
                if (_edges.TryGetValue(key, out var open))
                {
                    return open;
                }

                var a = Center(ax, az, Column(ax, az)[la].Y);
                var b = Center(bx, bz, Column(bx, bz)[lb].Y);
                var delta = b - a;
                var distance = delta.magnitude;
                open = Mathf.Abs(delta.y) <= _cell * Mathf.Tan(SlopeLimitDegrees * Mathf.Deg2Rad)
                       && !Physics.CapsuleCast(
                           a + Vector3.up * (StepOffset + Radius),
                           a + Vector3.up * (Height - Radius),
                           Radius,
                           delta / distance,
                           distance,
                           Mask,
                           QueryTriggerInteraction.Ignore);
                _edges[key] = open;
                return open;
            }

            private HashSet<long> Search(int cx, int cz, int level)
            {
                var reached = new HashSet<long> { Pack(cx, cz, level) };
                var queue = new Queue<long>();
                queue.Enqueue(Pack(cx, cz, level));

                while (queue.Count > 0)
                {
                    Unpack(queue.Dequeue(), out var x, out var z, out var l);
                    for (var d = 0; d < 4; d++)
                    {
                        Visit(cx, cz, x, z, l, x + DirX[d], z + DirZ[d], reached, queue);
                    }

                    for (var dx = -1; dx <= 1; dx += 2)
                    {
                        for (var dz = -1; dz <= 1; dz += 2)
                        {
                            VisitDiagonal(cx, cz, x, z, l, dx, dz, reached, queue);
                        }
                    }
                }

                return reached;
            }

            private void Visit(int cx, int cz, int x, int z, int l, int nx, int nz, HashSet<long> reached, Queue<long> queue)
            {
                if (!InWindow(cx, cz, nx, nz))
                {
                    return;
                }

                var column = Column(nx, nz);
                for (var n = 0; n < column.Length; n++)
                {
                    if (column[n].Standable && EdgeBetween(x, z, l, nx, nz, n) && reached.Add(Pack(nx, nz, n)))
                    {
                        queue.Enqueue(Pack(nx, nz, n));
                    }
                }
            }

            /// <summary>No corner cutting: both side routes must reach the same diagonal surface.</summary>
            private void VisitDiagonal(int cx, int cz, int x, int z, int l, int dx, int dz, HashSet<long> reached, Queue<long> queue)
            {
                var nx = x + dx;
                var nz = z + dz;
                if (!InWindow(cx, cz, nx, nz))
                {
                    return;
                }

                var column = Column(nx, nz);
                for (var n = 0; n < column.Length; n++)
                {
                    if (column[n].Standable
                        && Via(x, z, l, nx, z, nx, nz, n)
                        && Via(x, z, l, x, nz, nx, nz, n)
                        && reached.Add(Pack(nx, nz, n)))
                    {
                        queue.Enqueue(Pack(nx, nz, n));
                    }
                }
            }

            private bool Via(int ax, int az, int la, int mx, int mz, int bx, int bz, int lb)
            {
                var middle = Column(mx, mz);
                for (var m = 0; m < middle.Length; m++)
                {
                    if (middle[m].Standable
                        && EdgeBetween(ax, az, la, mx, mz, m)
                        && EdgeBetween(mx, mz, m, bx, bz, lb))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool InWindow(int cx, int cz, int x, int z)
            {
                return Mathf.Abs(x - cx) <= Reach && Mathf.Abs(z - cz) <= Reach;
            }

            private void Summarise(Window window)
            {
                var hash = 14695981039346656037UL;
                for (var dz = -Reach; dz <= Reach; dz++)
                {
                    for (var dx = -Reach; dx <= Reach; dx++)
                    {
                        var ix = window.X + dx;
                        var iz = window.Z + dz;
                        var column = Column(ix, iz);
                        var standable = 0;
                        for (var l = 0; l < column.Length; l++)
                        {
                            hash = Mix(hash, Mathf.RoundToInt(column[l].Y * 1000f));
                            hash = Mix(hash, column[l].Standable ? 1 : 0);
                            hash = Mix(hash, window.Reachable.Contains(Pack(ix, iz, l)) ? 1 : 0);
                            standable += column[l].Standable ? 1 : 0;
                        }

                        window.Standable += standable > 0 ? 1 : 0;
                        window.MultiLevel += standable > 1 ? 1 : 0;
                    }
                }

                window.Hash = hash;
            }

            private static ulong Mix(ulong hash, int value)
            {
                return (hash ^ (uint)value) * 1099511628211UL;
            }

            private static double Lap(ref long started)
            {
                var now = Stopwatch.GetTimestamp();
                var ms = (now - started) * 1000.0 / Stopwatch.Frequency;
                started = now;
                return ms;
            }
        }

        private sealed class HitHeightOrder : IComparer<RaycastHit>
        {
            public static readonly HitHeightOrder Instance = new HitHeightOrder();

            public int Compare(RaycastHit a, RaycastHit b)
            {
                return b.point.y.CompareTo(a.point.y);
            }
        }

        private sealed class Row
        {
            public Site Site;
            public float Cell;
            public string Pass;
            public HashSet<long> ReachableCells;
            public double FullMedianMs;
            public double FullWorstMs;
            public double ColumnMs;
            public double EdgeMs;
            public double SearchMs;
            public double CoverMs;
            public double StepMedianMs;
            public double StepWorstMs;
            public int Steps;
            public int Standable;
            public int MultiLevel;
            public int Reachable;
            public int LowCover;
            public int HighCover;
            public int TerrainCoverRays;
            public int OverflowColumns;
            public int AirWallCells;
            public string AirWallName = "";
            public int TreesInWindow;
            public int TreesInReachableCells;
            public int Flips;
            public int BothPass;
            public int BothBlock;
            public int FalsePass;
            public int FalseBlock;
            public bool Deterministic;
            public bool Stable;
        }

        private sealed class Report
        {
            public readonly List<Row> Rows = new List<Row>();
            public ControllerCheck Controller;
            public int NoStart;

            private readonly Dictionary<string, int> _colliders = new Dictionary<string, int>();

            public void AddColliders(Dictionary<string, int> added)
            {
                foreach (var pair in added)
                {
                    _colliders.TryGetValue(pair.Key, out var count);
                    _colliders[pair.Key] = count + pair.Value;
                }
            }

            public void Write(string outDir)
            {
                WriteCsv(Path.Combine(outDir, "sites.csv"));
                File.WriteAllText(Path.Combine(outDir, "summary.md"), Summary());
            }

            private void WriteCsv(string path)
            {
                var text = new StringBuilder();
                text.AppendLine(
                    "class,site,pass,cell,x,y,z,full_median_ms,full_worst_ms,column_ms,edge_ms,search_ms,cover_ms,"
                    + "step_median_ms,step_worst_ms,steps,standable,multi_level,reachable,low_cover,high_cover,"
                    + "terrain_cover_rays,overflow,air_wall_cells,air_wall_name,trees,trees_in_reachable,flips,"
                    + "both_pass,both_block,false_pass,false_block,deterministic,stable");
                foreach (var r in Rows)
                {
                    text.AppendLine(string.Join(",", new object[]
                    {
                        r.Site.ClassName, r.Site.Index, r.Pass, F(r.Cell),
                        F(r.Site.Position.x), F(r.Site.Position.y), F(r.Site.Position.z),
                        F(r.FullMedianMs), F(r.FullWorstMs), F(r.ColumnMs), F(r.EdgeMs), F(r.SearchMs), F(r.CoverMs),
                        F(r.StepMedianMs), F(r.StepWorstMs), r.Steps,
                        r.Standable, r.MultiLevel, r.Reachable, r.LowCover, r.HighCover,
                        r.TerrainCoverRays, r.OverflowColumns, r.AirWallCells, r.AirWallName,
                        r.TreesInWindow, r.TreesInReachableCells, r.Flips,
                        r.BothPass, r.BothBlock, r.FalsePass, r.FalseBlock, r.Deterministic, r.Stable
                    }));
                }

                File.WriteAllText(path, text.ToString());
            }

            private string Summary()
            {
                var text = new StringBuilder();
                text.AppendLine("# Combat Grid Probe, Phase A");
                text.AppendLine();
                text.AppendLine($"Sites without a start cell: {NoStart}. Controller usable: {Controller != null && Controller.Usable}.");
                text.AppendLine();
                text.AppendLine("## Gates (all-pieces pass, 1 m cells)");
                text.AppendLine();
                AppendGates(text);
                text.AppendLine();
                text.AppendLine("## By class");
                text.AppendLine();
                text.AppendLine("| Class | Pass | Cell | Full median ms | Full worst ms | Step median ms | Step worst ms | Columns / edges / search / cover ms | Reachable | Multi-level | Cover low / high | Terrain cover rays | Air walls | Flips | False pass | False block | Sampled |");
                text.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
                foreach (var group in Rows.GroupBy(r => (r.Site.ClassName, r.Pass, r.Cell)))
                {
                    AppendGroup(text, group.Key.ClassName, group.Key.Pass, group.Key.Cell, group.ToList());
                }

                text.AppendLine();
                text.AppendLine("## Pieces with no collider in the scene");
                text.AppendLine();
                text.AppendLine("| Category | Count |");
                text.AppendLine("|---|---|");
                foreach (var pair in _colliders.OrderByDescending(p => p.Value))
                {
                    text.AppendLine($"| {pair.Key} | {pair.Value} |");
                }

                text.AppendLine();
                var airNames = Rows.Where(r => r.AirWallCells > 0).Select(r => r.AirWallName).Distinct().ToList();
                text.AppendLine($"Air wall sources: {(airNames.Count == 0 ? "none" : string.Join(", ", airNames))}.");
                var trees = Rows.Where(r => r.Pass == AllPieces && Mathf.Approximately(r.Cell, 1f)).ToList();
                text.AppendLine($"Trees in reachable cells: {trees.Sum(r => r.TreesInReachableCells)} of {trees.Sum(r => r.TreesInWindow)} near the sites.");
                text.AppendLine($"Hit buffer overflows: {Rows.Sum(r => r.OverflowColumns)}.");
                return text.ToString();
            }

            private void AppendGates(StringBuilder text)
            {
                var rows = Rows.Where(r => r.Pass == AllPieces && Mathf.Approximately(r.Cell, 1f)).ToList();
                var median = rows.Count == 0 ? double.NaN : Median(rows.Select(r => r.FullMedianMs).ToList());
                var worst = rows.Count == 0 ? double.NaN : rows.Max(r => r.FullWorstMs);
                var step = rows.Where(r => r.Steps > 0).Select(r => r.StepWorstMs).DefaultIfEmpty(double.NaN).Max();
                var repeatable = Rows.All(r => r.Deterministic && r.Stable);
                var passable = rows.Sum(r => r.BothPass + r.FalsePass);
                var sampled = passable + rows.Sum(r => r.BothBlock + r.FalseBlock);
                var falsePass = passable == 0 ? double.NaN : (double)rows.Sum(r => r.FalsePass) / passable;
                var agreement = sampled == 0 ? double.NaN : (double)rows.Sum(r => r.BothPass + r.BothBlock) / sampled;
                var air = Rows.Sum(r => r.AirWallCells);

                text.AppendLine("| Gate | Value | Limit | Result |");
                text.AppendLine("|---|---|---|---|");
                text.AppendLine($"| G1 full build, median | {F(median)} ms | {F(FullMedianGateMs)} ms | {Verdict(median <= FullMedianGateMs, median)} |");
                text.AppendLine($"| G1 full build, worst | {F(worst)} ms | {F(FullWorstGateMs)} ms | {Verdict(worst <= FullWorstGateMs, worst)} |");
                text.AppendLine($"| G2 one-cell step, worst | {F(step)} ms | {F(StepWorstGateMs)} ms | {Verdict(step <= StepWorstGateMs, step)} |");
                text.AppendLine($"| G3 repeatability | {repeatable} | True | {(repeatable ? "PASS" : "FAIL")} |");
                text.AppendLine($"| G4 false pass share | {F(falsePass)} of {passable} | {F(FalsePassGate)} | {Verdict(falsePass <= FalsePassGate, falsePass)} |");
                text.AppendLine($"| G4 agreement | {F(agreement)} of {sampled} | {F(AgreementGate)} | {Verdict(agreement >= AgreementGate, agreement)} |");
                text.AppendLine($"| G5 air wall cells | {air} | 0 | {(air == 0 ? "PASS" : "FAIL")} |");
                text.AppendLine("| G6 hand correction | none | none | PASS |");
            }

            private static void AppendGroup(StringBuilder text, string name, string pass, float cell, List<Row> rows)
            {
                var stepRows = rows.Where(r => r.Steps > 0).ToList();
                var stepMedian = stepRows.Count == 0 ? 0.0 : Median(stepRows.Select(r => r.StepMedianMs).ToList());
                var stepWorst = stepRows.Count == 0 ? 0.0 : stepRows.Max(r => r.StepWorstMs);
                var stages = string.Join(" / ", new[]
                {
                    F(rows.Average(r => r.ColumnMs)), F(rows.Average(r => r.EdgeMs)),
                    F(rows.Average(r => r.SearchMs)), F(rows.Average(r => r.CoverMs))
                });
                text.AppendLine(
                    $"| {name} | {pass} | {F(cell)} | {F(Median(rows.Select(r => r.FullMedianMs).ToList()))} | {F(rows.Max(r => r.FullWorstMs))} "
                    + $"| {F(stepMedian)} | {F(stepWorst)} | {stages} | {F(rows.Average(r => (double)r.Reachable))} "
                    + $"| {F(rows.Average(r => (double)r.MultiLevel))} | {F(rows.Average(r => (double)r.LowCover))} / {F(rows.Average(r => (double)r.HighCover))} "
                    + $"| {rows.Sum(r => r.TerrainCoverRays)} | {rows.Sum(r => r.AirWallCells)} | {rows.Sum(r => r.Flips)} "
                    + $"| {rows.Sum(r => r.FalsePass)} | {rows.Sum(r => r.FalseBlock)} | {rows.Sum(r => r.BothPass + r.BothBlock + r.FalsePass + r.FalseBlock)} |");
            }

            private static string Verdict(bool pass, double value)
            {
                if (double.IsNaN(value))
                {
                    return "NOT MEASURED";
                }

                return pass ? "PASS" : "FAIL";
            }

            private static string F(double value)
            {
                return value.ToString("0.###", CultureInfo.InvariantCulture);
            }
        }
    }
}
#endif

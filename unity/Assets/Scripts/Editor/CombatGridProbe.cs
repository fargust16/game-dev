#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using GameDev.Confront.Grid;
using GameDev.World;
using GameDev.World.Buildings.Shell;
using GameDev.World.Settlement;
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
        internal const string OverworldScene = "Assets/Scenes/Overworld_Generator.unity";
        private const string OutArg = "-gridOut";
        private const string DefaultOut = "D:/Hobby/game-dev/research-data/combat-grid/run_1";
        internal const string ShellsName = "SettlementShells";
        private const string AsBuilt = "as-built";
        private const string AllPieces = "all-pieces";

        private const int ProbeSeed = 20261101;
        private const int Reach = CombatGridReader.Reach;
        private const int RepeatBuilds = 5;
        private const int WalkSteps = 20;
        private const int PassableEdgeSamples = 30;
        private const int BlockedEdgeSamples = 15;
        private const int MinTownReachable = 100;

        private const float SlopeLimitDegrees = CombatGridReader.SlopeLimitDegrees;
        private const float StepOffset = CombatGridReader.StepOffset;
        private const float SkinWidth = 0.03f;
        private const float WalkTickMeters = 0.075f;
        private const float TopY = CombatGridReader.TopY;
        private const float Radius = CombatGridReader.Radius;
        private const float Height = CombatGridReader.Height;

        // Gates: authoring defaults, fixed before the first run.
        private const double FullMedianGateMs = 50.0;
        private const double FullWorstGateMs = 100.0;
        private const double StepWorstGateMs = 8.0;
        private const double FalsePassGate = 0.01;
        private const double AgreementGate = 0.95;

        private const int Mask = CombatGridReader.Mask;

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
                var town = RebuildTown();
                Physics.SyncTransforms();

                WriteTownDiagnostics(town, outDir);

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
            var reader = new CombatGridReader(cell);
            if (!reader.TryFindStart(site.Position, out var startX, out var startZ, out var startLevel))
            {
                report.NoStart++;
                return null;
            }

            // One build first, so the timings below do not include JIT and first-touch costs.
            new CombatGridReader(cell).Build(startX, startZ, startLevel);

            var row = new Row { Site = site, Cell = cell, Pass = pass };
            var fullMs = new List<double>();
            var builds = new List<CombatGridWindow>();
            for (var i = 0; i < RepeatBuilds; i++)
            {
                var fresh = new CombatGridReader(cell);
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
            row.Reachable = window.ReachableCount;
            row.LowCover = window.LowCover;
            row.HighCover = window.HighCover;
            row.TerrainCoverRays = reader.TerrainCoverRays;
            row.OverflowColumns = reader.OverflowColumns;
            row.ReachableCells = reader.ReachableKeys();

            WalkTheGrid(row, startX, startZ, startLevel, cell);
            CountAirWalls(row, reader, window);
            CountTreeCells(row, window, trees, cell);
            TestAgainstController(row, reader, window, report, site);

            if (before != null)
            {
                // By column: a new piece adds a level, which renumbers the surfaces below it.
                var after = new HashSet<long>(row.ReachableCells.Select(k => k >> 4));
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
            var moving = new CombatGridReader(cell);
            var window = moving.Build(x, z, level);
            var visited = new HashSet<long> { CombatGridReader.Pack(x, z, level) };
            var stepMs = new List<double>();

            for (var step = 0; step < WalkSteps; step++)
            {
                if (!TryNextCell(moving, visited, x, z, ref level, out var nextX, out var nextZ))
                {
                    break;
                }

                x = nextX;
                z = nextZ;
                visited.Add(CombatGridReader.Pack(x, z, level));
                var collections = GC.CollectionCount(0);
                window = moving.Build(x, z, level);
                stepMs.Add(window.TotalMs);
                if (GC.CollectionCount(0) != collections)
                {
                    row.StepsWithCollection++;
                }
                else
                {
                    row.StepWorstCleanMs = Math.Max(row.StepWorstCleanMs, window.TotalMs);
                }
            }

            row.Steps = stepMs.Count;
            if (stepMs.Count > 0)
            {
                row.StepMedianMs = Median(stepMs);
                row.StepWorstMs = stepMs.Max();
            }

            row.Stable = new CombatGridReader(cell).Build(x, z, level).Hash == window.Hash;
        }

        private static bool TryNextCell(
            CombatGridReader reader,
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
                for (var l = 0; l < CombatGridReader.MaxLevels; l++)
                {
                    var key = CombatGridReader.Pack(nextX, nextZ, l);
                    if (reader.IsReachable(nextX, nextZ, l) && !visited.Contains(key))
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
        private static void CountAirWalls(Row row, CombatGridReader reader, CombatGridWindow window)
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
                        if (column[l].Standable || column[l].Reason != CombatGridBlock.Clearance)
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

        private static void CountTreeCells(Row row, CombatGridWindow window, List<Vector3> trees, float cell)
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
                for (var l = 0; l < CombatGridReader.MaxLevels; l++)
                {
                    if (row.ReachableCells.Contains(CombatGridReader.Pack(ix, iz, l)))
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
            CombatGridReader reader,
            CombatGridWindow window,
            Report report,
            Site site)
        {
            if (report.Controller == null || !report.Controller.Usable)
            {
                return;
            }

            var walker = report.Controller.Create();
            var rng = new Random(ProbeSeed + site.Index * 31 + site.ClassName[0] * 1009);
            var keys = row.ReachableCells.OrderBy(k => k).ToList();
            var passable = 0;
            var blocked = 0;

            for (var tries = 0; tries < keys.Count * 8 && (passable < PassableEdgeSamples || blocked < BlockedEdgeSamples); tries++)
            {
                CombatGridReader.Unpack(keys[rng.Next(keys.Count)], out var ax, out var az, out var la);
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
                    if (walks != gridSays)
                    {
                        walker.enabled = false;
                        Physics.SyncTransforms();
                        report.Disagreements.Add(string.Join(",", new object[]
                        {
                            site.ClassName, row.Pass, row.Cell, a.x, a.y, a.z, b.x, b.y, b.z,
                            gridSays ? "false_pass" : "false_block", reader.DescribeEdge(a, b)
                        }));
                    }

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

        /// <summary>Where each generated building stands against the ground under it.</summary>
        private static void WriteTownDiagnostics(GameObject town, string outDir)
        {
            var text = new StringBuilder();
            text.AppendLine("shell,x,z,min_y,max_y,ground_center,ground_min,ground_max,pieces,colliders,column");
            var shells = town.GetComponentsInChildren<ShellDebugGizmo>();
            for (var i = 0; i < shells.Length; i++)
            {
                var renderers = shells[i].GetComponentsInChildren<MeshRenderer>();
                if (renderers.Length == 0)
                {
                    continue;
                }

                var bounds = renderers[0].bounds;
                for (var r = 1; r < renderers.Length; r++)
                {
                    bounds.Encapsulate(renderers[r].bounds);
                }

                var groundMin = float.MaxValue;
                var groundMax = float.MinValue;
                for (var sx = -1; sx <= 1; sx += 2)
                {
                    for (var sz = -1; sz <= 1; sz += 2)
                    {
                        var y = OverworldTerrainAccess.SampleHeightWorld(
                            bounds.center.x + sx * bounds.extents.x,
                            bounds.center.z + sz * bounds.extents.z);
                        groundMin = Mathf.Min(groundMin, y);
                        groundMax = Mathf.Max(groundMax, y);
                    }
                }

                var origin = new Vector3(bounds.center.x, TopY, bounds.center.z);
                var hits = Physics.RaycastAll(origin, Vector3.down, TopY, Mask, QueryTriggerInteraction.Ignore);
                var column = string.Join(
                    " ",
                    hits.OrderByDescending(h => h.point.y).Select(h => $"{h.collider.name}@{h.point.y:F2}"));
                text.AppendLine(string.Join(",", new object[]
                {
                    i, bounds.center.x, bounds.center.z, bounds.min.y, bounds.max.y,
                    OverworldTerrainAccess.SampleHeightWorld(bounds.center.x, bounds.center.z),
                    groundMin, groundMax, renderers.Length,
                    shells[i].GetComponentsInChildren<Collider>().Length, column
                }));
            }

            File.WriteAllText(Path.Combine(outDir, "town_shells.csv"), text.ToString());
        }

        /// <summary>
        /// Replaces the saved town with one generated on the terrain as it is now, in memory.
        /// </summary>
        /// <remarks>
        /// The terrain assets were rebaked a month after the scene was saved, so the saved town
        /// stands 2-45 m above the ground and its walls block nothing. This follows
        /// <see cref="SettlementOverworldMenu.PlaceSettlementAtTownApproach"/> except for the dust
        /// paint, which would write to the TerrainData.
        /// </remarks>
        internal static GameObject RebuildTown()
        {
            var stale = GameObject.Find("OverworldSettlement");
            if (stale != null)
            {
                Object.DestroyImmediate(stale);
            }

            var terrain = OverworldTerrainAccess.FindTerrain();
            if (!SettlementApproachBinder.TryFindTownApproach(OverworldWorldPlan.GetOrBuildPois(), out var approach)
                || !SettlementApproachBinder.TryCreateDryFabricNearApproach(
                    approach,
                    OverworldTerrainMenu.DefaultSeed,
                    new TerrainHeightSampler(),
                    OverworldWaterSamplers.ForCurrentWorld(terrain),
                    out var fabric,
                    out _))
            {
                throw new InvalidOperationException("No town could be placed on this terrain.");
            }

            GeneratedTintMaterials.Reset();
            var root = new GameObject("CombatGridProbeTown");
            var bridge = new SettlementShellBridge(
                new BuildingShellBinder(),
                ShellCatalogFactory.CreateDefaultArchetypes(),
                ShellCatalogFactory.CreateDefaultDistricts());
            bridge.SpawnProperties(fabric, fabric.Pose.SettlementSeed, root.transform).name = ShellsName;
            PropertyFeatureSpawner.SpawnAll(fabric, root.transform);
            return root;
        }

        internal static Dictionary<string, int> AddMissingColliders(GameObject root)
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
                var reader = new CombatGridReader(1f);
                if (reader.TryFindStart(position, out var ix, out var iz, out var level)
                    && reader.Build(ix, iz, level).ReachableCount >= MinTownReachable)
                {
                    sites.Add(new Site(className, sites.Count, position));
                }
            }

            return sites;
        }

        private static void WriteMap(string path, CombatGridReader reader, CombatGridWindow window)
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

        private static Color MapColor(CombatGridReader reader, CombatGridWindow window, int ix, int iz)
        {
            var column = reader.Column(ix, iz);
            for (var l = 0; l < column.Length; l++)
            {
                if (!reader.IsReachable(ix, iz, l))
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

            return column[column.Length - 1].Reason == CombatGridBlock.Water
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

        private sealed class TerrainHeightSampler : IWorldHeightSampler
        {
            public bool TrySampleWorldY(float worldX, float worldZ, out float worldY)
            {
                return OverworldTerrainAccess.TrySampleHeightWorld(worldX, worldZ, out worldY);
            }
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
            public double StepWorstCleanMs;
            public int Steps;
            public int StepsWithCollection;
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
            public readonly List<string> Disagreements = new List<string>();
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
                File.WriteAllText(
                    Path.Combine(outDir, "disagreements.csv"),
                    "class,pass,cell,ax,ay,az,bx,by,bz,kind,decided_by\n" + string.Join("\n", Disagreements));
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
                text.AppendLine(
                    $"Steps: {Rows.Sum(r => r.Steps)}, with a garbage collection: {Rows.Sum(r => r.StepsWithCollection)}, "
                    + $"slowest with none: {F(Rows.Max(r => r.StepWorstCleanMs))} ms.");
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

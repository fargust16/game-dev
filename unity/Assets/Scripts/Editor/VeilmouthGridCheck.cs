#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GameDev.Confront.Grid;
using GameDev.Slice.Dwellings;
using GameDev.Slice.EditorTools;
using GameDev.Slice.Kit;
using GameDev.World.Portals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static GameDev.Slice.EditorTools.SliceGridReadings;

namespace GameDev.EditorTools
{
    /// <summary>
    /// S0's acceptance in the slice's own scenes (implementation plan, S0): the Combat Grid, read in Veilmouth, its seven
    /// interiors, and sample dwellings, reads a street as the kit standard expects. A house front closes the edges into
    /// the house, a railing or fence is low cover and closes its edge, and a doorway between rooms lets one cell through.
    /// It only reads scenes and writes <c>Logs/SliceGrid</c>; it never saves a scene.
    /// Batch: -executeMethod GameDev.EditorTools.VeilmouthGridCheck.RunBatch (exits 0 when every reading passes).
    /// </summary>
    public static class VeilmouthGridCheck
    {
        private const string OutDir = "Logs/SliceGrid";

        // Authoring defaults for the acceptance, 2026-11-06.
        private const int MinStreetCells = 200;     // cells reached from a street site, of the 41 × 41 window
        private const float MinFrontsClosed = 0.99f; // house fronts whose edge into the house is closed
        private const float MinLowCover = 0.9f;      // railing and fence sides that read low cover, not high
        private const float MinRailEdges = 0.95f;    // railing and fence crossings the grid closes

        [MenuItem("GameDev/Slice/Check the Combat Grid in Veilmouth")]
        public static void RunMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Run();
            }
        }

        public static void RunBatch()
        {
            var ok = false;
            try
            {
                ok = Run();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool Run()
        {
            Directory.CreateDirectory(OutDir);
            var report = new StringBuilder();
            report.AppendLine($"Veilmouth Combat Grid check, {DateTime.Now:yyyy-MM-dd HH:mm}. 1 m cells, reach {CombatGridReader.Reach}.");
            var ok = true;

            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
            var reader = new CombatGridReader(1f);
            ok &= Streets(reader, report);
            ok &= HouseFronts(reader, report);
            ok &= Railings(reader, report);

            report.AppendLine();
            report.AppendLine("Doorways (each between two rooms lets its cells through, two over a node and one over a cell; one with a door leaf none):");
            foreach (var path in InteriorScenes())
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Physics.SyncTransforms();
                ok &= Doorways(Path.GetFileNameWithoutExtension(path), new CombatGridReader(1f), report);
            }

            foreach (DwellingArchetype archetype in Enum.GetValues(typeof(DwellingArchetype)))
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var wealth = archetype == DwellingArchetype.Villa ? DwellingWealth.Rich : DwellingWealth.Middling;
                VeilmouthDwellings.BuildAndCheck(VeilmouthDwellings.PreviewKey(archetype, wealth, 77, -77), VeilmouthDwellings.Kit, VeilmouthDwellings.Settings, out _);
                Physics.SyncTransforms();
                ok &= Doorways($"Dwelling {archetype}", new CombatGridReader(1f), report);
            }

            report.AppendLine();
            report.AppendLine(ok ? "RESULT: PASS" : "RESULT: FAIL");
            File.WriteAllText(Path.Combine(OutDir, "report.txt"), report.ToString());
            Debug.Log("[VeilmouthGridCheck]\n" + report);
            return ok;
        }

        // ------------------------------------------------------------------------------------------- streets

        private static bool Streets(CombatGridReader reader, StringBuilder report)
        {
            var sites = new List<(string Name, Vector3 At)>();
            var player = UnityEngine.Object.FindAnyObjectByType<CharacterController>();
            if (player != null)
            {
                sites.Add(("player start", player.transform.position));
            }

            foreach (var portal in UnityEngine.Object.FindObjectsByType<Portal>(FindObjectsSortMode.None)
                         .Where(p => p.Target.SceneName != SliceTestScenes.SceneName(VeilmouthDwellings.ScenePath))
                         .OrderBy(p => p.Id))
            {
                sites.Add((portal.Id, portal.transform.position + (portal.transform.forward * 1.5f)));
            }

            report.AppendLine();
            report.AppendLine($"Streets (each site reaches at least {MinStreetCells} cells):");
            var ok = true;
            foreach (var (name, at) in sites)
            {
                if (!reader.TryFindStart(at, out var ix, out var iz, out var level))
                {
                    report.AppendLine($"  FAIL {name}: no standable cell near {at}");
                    ok = false;
                    continue;
                }

                var window = reader.Build(ix, iz, level);
                var pass = window.ReachableCount >= MinStreetCells;
                ok &= pass;
                report.AppendLine($"  {(pass ? "ok  " : "FAIL")} {name}: reached {window.ReachableCount}, standable {window.Standable}, low cover {window.LowCover}, high cover {window.HighCover}");
                WriteMap(Path.Combine(OutDir, $"street_{Safe(name)}.png"), reader, window);
            }

            return ok;
        }

        // ------------------------------------------------------------------------------------------- house fronts

        private static bool HouseFronts(CombatGridReader reader, StringBuilder report)
        {
            var dwellingScene = SliceTestScenes.SceneName(VeilmouthDwellings.ScenePath);
            int closed = 0, open = 0, unmeasured = 0;
            var failures = new List<string>();
            foreach (var door in UnityEngine.Object.FindObjectsByType<Portal>(FindObjectsSortMode.None).Where(p => p.Target.SceneName == dwellingScene))
            {
                // From the street's own surface: a door's threshold stands above the street in the C3 city (S3a.3).
                switch (EntersHouse(reader, door.transform))
                {
                    case 0: closed++; break;
                    case 1: open++; failures.Add(door.Id); break;
                    default: unmeasured++; break;
                }
            }

            var share = closed / (float)Math.Max(1, closed + open);
            var pass = share >= MinFrontsClosed && closed > 0;
            report.AppendLine();
            report.AppendLine($"House fronts (from the street before each door the grid reaches no cell behind the front): {(pass ? "ok" : "FAIL")}, {closed} closed, {open} open, {unmeasured} with no street cell to stand on ({share:P1} closed; at least {MinFrontsClosed:P0}).");
            foreach (var id in failures.Take(20))
            {
                report.AppendLine($"  open: {id}");
            }

            return pass;
        }

        // ------------------------------------------------------------------------------------------- railings

        private static bool Railings(CombatGridReader reader, StringBuilder report)
        {
            int sides = 0, low = 0, high = 0, none = 0, crossings = 0, crossingsClosed = 0;
            var failures = new List<string>();
            foreach (var block in UnityEngine.Object.FindObjectsByType<KitBlock>(FindObjectsSortMode.None))
            {
                var name = block.gameObject.name;
                if (!name.Contains("_Railing_") && !name.StartsWith("Kit_Fence_"))
                {
                    continue;
                }

                if (!TryBody(block.transform, out var body))
                {
                    continue;
                }

                var thinX = body.size.x < body.size.z;
                var across = thinX ? Vector3.right : Vector3.forward;
                var c = body.center;
                var a = c - (across * 0.6f);
                var b = c + (across * 0.6f);
                var y = body.min.y;

                foreach (var (from, toward) in new[] { (a, across), (b, -across) })
                {
                    var ix = Mathf.FloorToInt(from.x);
                    var iz = Mathf.FloorToInt(from.z);
                    var level = LevelNear(reader.Column(ix, iz), y);
                    if (level < 0)
                    {
                        continue;
                    }

                    sides++;
                    var cover = reader.Cover(ix, iz, level);
                    var bit = 1 << Direction(toward);
                    if ((cover.High & bit) != 0)
                    {
                        high++;
                        failures.Add($"high: {name} at {Round(c)}, ground {reader.Column(ix, iz)[level].Y - body.min.y:+0.00;-0.00} m from its foot, top {body.max.y - reader.Column(ix, iz)[level].Y:0.00} m above the cell; at 1.5 m: {HighHits(reader.Column(ix, iz)[level].Y, ix, iz, toward)}");
                    }
                    else if ((cover.Low & bit) != 0)
                    {
                        low++;
                    }
                    else
                    {
                        none++;
                        failures.Add($"none: {name} at {Round(c)}");
                    }
                }

                var passes = Passes(reader, a, b, y);
                if (passes >= 0)
                {
                    crossings++;
                    crossingsClosed += passes == 0 ? 1 : 0;
                }
            }

            var lowShare = low / (float)Math.Max(1, sides);
            var closedShare = crossingsClosed / (float)Math.Max(1, crossings);
            var pass = sides > 0 && lowShare >= MinLowCover && closedShare >= MinRailEdges;
            report.AppendLine();
            report.AppendLine($"Railings and fences (a side reads low cover, not high; the grid does not cross them): {(pass ? "ok" : "FAIL")}, {sides} sides: {low} low ({lowShare:P1}; at least {MinLowCover:P0}), {high} high, {none} none; {crossingsClosed} of {crossings} crossings closed ({closedShare:P1}; at least {MinRailEdges:P0}).");
            foreach (var line in failures.Take(20))
            {
                report.AppendLine($"  {line}");
            }

            return pass;
        }

        // ------------------------------------------------------------------------------------------- doorways

        private static bool Doorways(string sceneName, CombatGridReader reader, StringBuilder report)
        {
            var blocks = UnityEngine.Object.FindObjectsByType<KitBlock>(FindObjectsSortMode.None);
            var leaves = blocks.Where(b => IsDoorLeaf(b.gameObject.name))
                .Select(b => TryBody(b.transform, out var body) ? body : new Bounds()).Where(b => b.size != Vector3.zero).ToList();
            int open = 0, leafed = 0, outer = 0, faults = 0;
            var notes = new List<string>();
            foreach (var block in blocks)
            {
                var name = block.gameObject.name;
                if (!IsDoorway(name) || !TryBody(block.transform, out var body))
                {
                    continue;
                }

                var thinX = body.size.x < body.size.z;
                var across = thinX ? Vector3.right : Vector3.forward;
                var along = thinX ? Vector3.forward : Vector3.right;
                var hasLeaf = leaves.Any(l => l.Intersects(body));
                var passes = 0;
                var measured = 0;
                // The four cells round the doorway's middle, sampled at their own middles, never on a grid line.
                var at = Vector3.Dot(body.center, along);
                for (var k = -2; k <= 1; k++)
                {
                    var c = body.center + (along * (Mathf.Round(at) + k + 0.5f - at));
                    var p = Passes(reader, c - (across * 0.6f), c + (across * 0.6f), body.min.y);
                    if (p >= 0)
                    {
                        measured++;
                        passes += p;
                    }
                }

                // A doorway over a node passes the two cells either side of it (BK's 4 m wall, its 1.76 m opening over
                // the middle node, 2026-11-06); one over a cell's middle passes that cell.
                var expected = hasLeaf ? 0 : Mathf.Abs(at - Mathf.Round(at)) < 0.25f ? 2 : 1;
                if (measured == 0 && !hasLeaf)
                {
                    outer++; // the entry in the room's outer wall: no floor beyond it, the portal takes one through
                }
                else if (passes == expected && (hasLeaf || measured > 0))
                {
                    if (hasLeaf) leafed++;
                    else open++;
                }
                else
                {
                    faults++;
                    notes.Add($"{name} at {Round(body.center)}: {passes} cells through ({measured} measured), expected {expected}{(hasLeaf ? " (door leaf)" : string.Empty)}");
                }
            }

            report.AppendLine($"  {(faults == 0 ? "ok  " : "FAIL")} {sceneName}: {open} open doorways pass their cells, {leafed} with a leaf pass none, {outer} in the outer wall, {faults} faults");
            foreach (var note in notes.Take(10))
            {
                report.AppendLine($"       {note}");
            }

            return faults == 0;
        }

        private static IEnumerable<string> InteriorScenes() =>
            new[]
            {
                VeilmouthScene.SootAndBellScenePath, VeilmouthDressing.HarrowScenePath, VeilmouthDressing.FactionHouseScenePath,
                VeilmouthDressing.LockupScenePath, VeilmouthDressing.MissingRoomScenePath, VeilmouthDressing.CellarScenePath,
                VeilmouthDressing.ChapelScenePath,
            };

        // ------------------------------------------------------------------------------------------- readings
        // Passes, LevelNear and TryBody are SliceGridReadings', shared with the slice's grid tests (S3a.3).

        /// <summary>What the high cover strip of a cell meets, as CombatGridReader's cover reads it.</summary>
        private static string HighHits(float groundY, int ix, int iz, Vector3 toward)
        {
            var origin = new Vector3(ix + 0.5f, groundY + 1.5f, iz + 0.5f);
            var centre = origin + (toward * 0.6f);
            var across = Vector3.Cross(Vector3.up, toward);
            var half = (toward * 0.3f) + (across * 0.5f);
            var hits = Physics.OverlapBox(centre, new Vector3(Mathf.Abs(half.x), 0.05f, Mathf.Abs(half.z)), Quaternion.identity, CombatGridReader.Mask, QueryTriggerInteraction.Ignore);
            return string.Join(", ", hits.Select(h => h.transform.root == h.transform ? h.name : $"{h.transform.parent?.name}/{h.name}").Distinct().Take(3));
        }

        private static int Direction(Vector3 toward) => (int)Side(toward);

        private static string Round(Vector3 v) => $"({v.x:0.0}, {v.y:0.0}, {v.z:0.0})";

        private static string Safe(string name) => new string(name.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());

        /// <summary>A picture of a window: reached cells green, with low cover yellow and high cover orange; standable but
        /// not reached grey; nothing to stand on dark.</summary>
        private static void WriteMap(string path, CombatGridReader reader, CombatGridWindow window)
        {
            const int scale = 8;
            var size = (CombatGridReader.Reach * 2) + 1;
            var texture = new Texture2D(size * scale, size * scale, TextureFormat.RGB24, false);
            for (var dz = -CombatGridReader.Reach; dz <= CombatGridReader.Reach; dz++)
            {
                for (var dx = -CombatGridReader.Reach; dx <= CombatGridReader.Reach; dx++)
                {
                    var ix = window.X + dx;
                    var iz = window.Z + dz;
                    var column = reader.Column(ix, iz);
                    var colour = new Color(0.12f, 0.12f, 0.14f);
                    for (var l = 0; l < column.Length; l++)
                    {
                        if (reader.IsReachable(ix, iz, l))
                        {
                            var cover = reader.Cover(ix, iz, l);
                            colour = cover.High != 0 ? new Color(0.95f, 0.55f, 0.15f) : cover.Low != 0 ? new Color(0.95f, 0.85f, 0.2f) : new Color(0.3f, 0.75f, 0.35f);
                            break;
                        }

                        if (column[l].Standable)
                        {
                            colour = new Color(0.5f, 0.5f, 0.52f);
                        }
                    }

                    if (dx == 0 && dz == 0)
                    {
                        colour = Color.white;
                    }

                    for (var py = 0; py < scale; py++)
                    {
                        for (var px = 0; px < scale; px++)
                        {
                            var edge = px == 0 || py == 0;
                            texture.SetPixel(((dx + CombatGridReader.Reach) * scale) + px, ((dz + CombatGridReader.Reach) * scale) + py, edge ? colour * 0.8f : colour);
                        }
                    }
                }
            }

            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
#endif

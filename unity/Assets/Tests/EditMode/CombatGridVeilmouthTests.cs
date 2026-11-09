using System.Collections.Generic;
using System.Linq;
using GameDev.Confront.Grid;
using GameDev.Slice.EditorTools;
using GameDev.Slice.Kit;
using GameDev.World.Portals;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using static GameDev.Slice.EditorTools.SliceGridReadings;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S3a.3's acceptance in Veilmouth's own scenes: walls close the edges into the houses, a fence is Half Cover, a
    /// doorway lets its cells through and a door leaf none, and a step of the window allocates nothing. The readings
    /// and their gates are S0's (<c>VeilmouthGridCheck</c>, 2026-11-06), now on the grid of GameDev.Confront.
    /// </summary>
    public sealed class CombatGridVeilmouthTests
    {
        private const float MinFrontsClosed = 0.99f;
        private const float MinHalfCover = 0.9f;
        private const float MinRailEdges = 0.95f;
        private const int WalkCells = 4;

        private static IEnumerable<string> Interiors() =>
            new[]
            {
                VeilmouthScene.SootAndBellScenePath, VeilmouthDressing.HarrowScenePath, VeilmouthDressing.FactionHouseScenePath,
                VeilmouthDressing.LockupScenePath, VeilmouthDressing.MissingRoomScenePath, VeilmouthDressing.CellarScenePath,
                VeilmouthDressing.ChapelScenePath,
            };

        private static void Open(string path)
        {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        [Test]
        public void WallsCloseTheEdgesFromTheStreetIntoTheHouses()
        {
            Open(VeilmouthScene.ScenePath);
            var grid = new CombatGridReader(1f);
            var dwellings = SliceTestScenes.SceneName(VeilmouthDwellings.ScenePath);
            var closed = 0;
            var unmeasured = 0;
            var open = new List<string>();
            foreach (var door in Object.FindObjectsByType<Portal>(FindObjectsSortMode.None).Where(p => p.Target.SceneName == dwellings))
            {
                switch (EntersHouse(grid, door.transform))
                {
                    case 0: closed++; break;
                    case 1: open.Add(door.Id); break;
                    default: unmeasured++; break;
                }
            }

            var share = closed / (float)Mathf.Max(1, closed + open.Count);
            TestContext.WriteLine($"House fronts: from the street before {closed + open.Count} doors the grid reaches no cell behind the front at {closed}, {unmeasured} with no street cell ({share:P1} closed).");
            Assert.Greater(closed, 50, "Veilmouth's dwelling doors are read");
            Assert.GreaterOrEqual(share, MinFrontsClosed, "open: " + string.Join(", ", open.Take(10)));
        }

        [Test]
        public void ARailingOrAFenceIsHalfCoverAndClosesItsEdge()
        {
            Open(VeilmouthScene.ScenePath);
            var grid = new CombatGridReader(1f);
            int sides = 0, half = 0, full = 0, crossings = 0, closed = 0;
            var notes = new List<string>();
            CombatCell? sample = null;
            var sampleSide = GridSide.PlusX;
            foreach (var block in Object.FindObjectsByType<KitBlock>(FindObjectsSortMode.None))
            {
                var name = block.gameObject.name;
                if ((!name.Contains("_Railing_") && !name.StartsWith("Kit_Fence_")) || !TryBody(block.transform, out var body))
                {
                    continue;
                }

                var across = body.size.x < body.size.z ? Vector3.right : Vector3.forward;
                var a = body.center - (across * 0.6f);
                var b = body.center + (across * 0.6f);
                foreach (var (from, toward) in new[] { (a, across), (b, -across) })
                {
                    int ix = Mathf.FloorToInt(from.x), iz = Mathf.FloorToInt(from.z);
                    var level = LevelNear(grid.Column(ix, iz), body.min.y);
                    if (level < 0)
                    {
                        continue;
                    }

                    sides++;
                    switch (grid.Cover(ix, iz, level).On(Side(toward)))
                    {
                        case CoverLevel.Half:
                            half++;
                            if (sample == null)
                            {
                                sample = new CombatCell(ix, iz, level);
                                sampleSide = Side(toward);
                            }

                            break;
                        case CoverLevel.Full:
                            full++;
                            notes.Add($"full: {name} at {body.center}");
                            break;
                        default:
                            notes.Add($"none: {name} at {body.center}");
                            break;
                    }
                }

                var passes = Passes(grid, a, b, body.min.y);
                if (passes >= 0)
                {
                    crossings++;
                    closed += passes == 0 ? 1 : 0;
                }
            }

            var halfShare = half / (float)Mathf.Max(1, sides);
            var closedShare = closed / (float)Mathf.Max(1, crossings);
            TestContext.WriteLine($"Railings and fences: {sides} sides, {half} Half Cover ({halfShare:P1}), {full} Full; {closed} of {crossings} crossings closed ({closedShare:P1}).");
            Assert.Greater(sides, 50, "Veilmouth's railings are read");
            Assert.GreaterOrEqual(halfShare, MinHalfCover, string.Join("\n", notes.Take(10)));
            Assert.GreaterOrEqual(closedShare, MinRailEdges);

            // The same reading through ICombatGrid, with the window over the railing.
            var cell = sample.Value;
            ICombatGrid fight = grid;
            grid.Build(cell.X, cell.Z, cell.Level);
            Assert.AreEqual(CoverLevel.Half, fight.CoverOn(cell, sampleSide), $"{cell} toward {sampleSide}");
        }

        [Test]
        public void ADoorwayLetsItsCellsThroughAndADoorLeafNone([ValueSource(nameof(Interiors))] string scene)
        {
            Open(scene);
            var grid = new CombatGridReader(1f);
            var blocks = Object.FindObjectsByType<KitBlock>(FindObjectsSortMode.None);
            var leaves = blocks.Where(b => IsDoorLeaf(b.gameObject.name))
                .Select(b => TryBody(b.transform, out var body) ? body : new Bounds()).Where(b => b.size != Vector3.zero).ToList();
            int open = 0, one = 0, leafed = 0, outer = 0;
            var faults = new List<string>();
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
                    var p = Passes(grid, c - (across * 0.6f), c + (across * 0.6f), body.min.y);
                    if (p >= 0)
                    {
                        measured++;
                        passes += p;
                    }
                }

                // A doorway over a cell's middle lets that one cell through; one over a node, as BK's 1.76 m opening
                // in its 4 m wall, the two cells either side of it (2026-11-06); a closed door leaf none.
                var overNode = Mathf.Abs(at - Mathf.Round(at)) < 0.25f;
                var expected = hasLeaf ? 0 : overNode ? 2 : 1;
                if (measured == 0 && !hasLeaf)
                {
                    // A doorway in the room's outer wall, the entry from the street, has no floor beyond it: the room's
                    // own scene ends there, and the portal, not the grid, takes the character through.
                    outer++;
                }
                else if (passes == expected && (hasLeaf || measured > 0))
                {
                    leafed += hasLeaf ? 1 : 0;
                    open += hasLeaf ? 0 : 1;
                    one += !hasLeaf && !overNode ? 1 : 0;
                }
                else
                {
                    faults.Add($"{name} at {body.center}: {passes} cells through ({measured} measured), expected {expected}{(hasLeaf ? " (door leaf)" : string.Empty)}");
                }
            }

            TestContext.WriteLine($"{scene}: {open} open doorways pass their cells ({one} over a cell's middle, one cell), {leafed} with a leaf pass none, {outer} in the outer wall with no floor beyond.");
            Assert.Greater(open + leafed, 0, "the interior has doorways");
            Assert.IsEmpty(faults, string.Join("\n", faults));
        }

        [Test]
        public void AStepOfTheWindowAllocatesNothing()
        {
            Open(VeilmouthScene.ScenePath);
            var player = Object.FindAnyObjectByType<CharacterController>();
            Assert.IsNotNull(player, "the player's start");
            var grid = new CombatGridReader(1f);
            Assert.IsTrue(grid.Follow(player.transform.position), "the grid forms at the player's start");
            var walk = StraightWalk(grid, WalkCells);
            Assert.IsNotNull(walk, $"a straight line of {WalkCells} reachable cells from the start");

            // The first walk reads the columns past the window and meets their colliders for the first time, when Unity
            // makes its managed objects for them. The ring then lets those columns go, and the second walk reads them
            // again, as every walk after the first does.
            foreach (var (position, cell) in walk)
            {
                Assert.IsTrue(grid.Follow(position), $"a step to {cell}");
                Assert.AreEqual(cell, grid.Origin, "the window follows");
            }

            var ms = new double[walk.Length];
            var moves = 0;
            Assert.That(() =>
            {
                for (var i = 0; i < walk.Length; i++)
                {
                    moves += grid.Follow(walk[i].Position) ? 1 : 0;
                    ms[i] = grid.Window.TotalMs;
                }
            }, UnityEngine.TestTools.Constraints.Is.Not.AllocatingGCMemory());

            Assert.AreEqual(walk.Length, moves, "every position was a step");
            var sorted = ms.OrderBy(m => m).ToArray();
            TestContext.WriteLine($"{walk.Length} steps, none allocating; median {sorted[sorted.Length / 2]:F2} ms, worst {sorted[sorted.Length - 1]:F2} ms (G2: worst at most 8 ms).");
        }

        /// <summary>Out along the first side with <paramref name="cells"/> reachable cells in a line, and back.</summary>
        private static (Vector3 Position, CombatCell Cell)[] StraightWalk(CombatGridReader grid, int cells)
        {
            var start = grid.Origin;
            var y = grid.Column(start.X, start.Z)[start.Level].Y;
            int[] dirX = { 1, -1, 0, 0 }, dirZ = { 0, 0, 1, -1 };
            for (var d = 0; d < 4; d++)
            {
                var line = new List<CombatCell>();
                for (var k = 1; k <= cells; k++)
                {
                    int x = start.X + (k * dirX[d]), z = start.Z + (k * dirZ[d]);
                    var level = grid.LevelNear(x, z, y);
                    if (level < 0 || !grid.IsReachable(new CombatCell(x, z, level)))
                    {
                        break;
                    }

                    line.Add(new CombatCell(x, z, level));
                }

                if (line.Count < cells)
                {
                    continue;
                }

                var back = line.Take(cells - 1).Reverse().Append(start);
                return line.Concat(back).Select(c => (grid.Center(c) + (Vector3.up * 0.05f), c)).ToArray();
            }

            return null;
        }
    }
}

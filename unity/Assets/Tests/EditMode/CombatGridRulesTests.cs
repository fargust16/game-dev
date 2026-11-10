using System.Collections.Generic;
using GameDev.Confront.Grid;
using GameDev.Slice.Kit;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S3a.3: the Combat Grid's rules on a small built place, a floor of 20 × 20 one-metre cells with its top at 0 m:
    /// walls close edges and corners, cover on each side, the 10.1 tags, occupancy apart from the place, and paths.
    /// </summary>
    public sealed class CombatGridRulesTests
    {
        private CombatGridReader _grid;

        [SetUp]
        public void BuildThePlace()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Block("Floor", 0f, 0f, 20f, 20f, -1f, 0f);
        }

        private static GameObject Block(string name, float x0, float z0, float sizeX, float sizeZ, float bottom, float top)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.position = new Vector3(x0 + (sizeX * 0.5f), (bottom + top) * 0.5f, z0 + (sizeZ * 0.5f));
            block.transform.localScale = new Vector3(sizeX, top - bottom, sizeZ);
            return block;
        }

        private CombatGridReader Grid(int x, int z)
        {
            Physics.SyncTransforms();
            _grid = new CombatGridReader(1f);
            Assert.IsTrue(_grid.Follow(new Vector3(x + 0.5f, 0.05f, z + 0.5f)), "the grid forms");
            Assert.AreEqual(new CombatCell(x, z, 0), _grid.Origin);
            return _grid;
        }

        private static CombatCell Cell(int x, int z) => new CombatCell(x, z, 0);

        [Test]
        public void AWallClosesItsEdgeAndTheDiagonalBesideItNeverCutsTheCorner()
        {
            Block("Pillar", 6f, 5f, 1f, 1f, 0f, 3f);
            var grid = Grid(5, 5);

            Assert.IsFalse(grid.IsWalkable(new CombatCell(6, 5, 1)), "the floor under the pillar has no room to stand");
            Assert.IsFalse(grid.IsReachable(Cell(6, 5)), "the pillar's top, 3 m up, is out of a step's reach");
            Assert.IsFalse(grid.CanStep(Cell(5, 5), Cell(6, 5)), "the wall closes the edge");
            Assert.IsFalse(grid.CanStep(Cell(5, 5), Cell(6, 6)), "a diagonal past the pillar's corner would cut it");
            Assert.IsFalse(grid.CanStep(Cell(5, 5), Cell(6, 4)), "nor on its other side");
            Assert.IsTrue(grid.CanStep(Cell(5, 5), Cell(4, 4)), "an open diagonal");
            Assert.IsTrue(grid.CanStep(Cell(5, 5), Cell(5, 6)), "an open side");

            var path = new List<CombatCell>();
            Assert.AreEqual(4, grid.FindPath(Cell(5, 5), Cell(7, 5), path), "round the pillar without cutting a corner");
            Assert.AreEqual(4, path.Count);
            Assert.AreEqual(Cell(7, 5), path[path.Count - 1]);
            var from = Cell(5, 5);
            foreach (var next in path)
            {
                Assert.IsTrue(grid.CanStep(from, next), $"{from} to {next}");
                Assert.AreNotEqual(Cell(6, 5), next);
                from = next;
            }
        }

        [Test]
        public void ADiagonalStepCostsOneAsASideDoes()
        {
            var grid = Grid(5, 5);
            var path = new List<CombatCell>();
            Assert.AreEqual(3, grid.FindPath(Cell(5, 5), Cell(8, 8), path), "three diagonals, 1 AP each (10.1)");
            CollectionAssert.AreEqual(new[] { Cell(6, 6), Cell(7, 7), Cell(8, 8) }, path);

            Assert.AreEqual(4, grid.FindPath(Cell(5, 5), Cell(9, 6), path), "the longer axis counts");
            var diagonals = 0;
            var from = Cell(5, 5);
            foreach (var next in path)
            {
                diagonals += next.X != from.X && next.Z != from.Z ? 1 : 0;
                from = next;
            }

            Assert.AreEqual(1, diagonals, "of the shortest paths, the one with the fewest diagonals");
            Assert.AreEqual(0, grid.FindPath(Cell(5, 5), Cell(5, 5), path));
            Assert.AreEqual(-1, grid.FindPath(Cell(5, 5), Cell(30, 5), path), "outside the window");
        }

        [Test]
        public void AFenceIsHalfCoverAndAWallFullCoverOnTheirSides()
        {
            // A fence 1 m high just past the line x = 6, a wall 3 m high just short of the line x = 5: the cell (5, 5)
            // stands between them.
            Block("Fence", 6.02f, 3f, 0.1f, 5f, 0f, 1f);
            Block("Wall", 4.88f, 3f, 0.1f, 5f, 0f, 3f);
            var grid = Grid(5, 5);

            Assert.AreEqual(CoverLevel.Half, grid.CoverOn(Cell(5, 5), GridSide.PlusX), "a fence is Half Cover");
            Assert.AreEqual(CoverLevel.Full, grid.CoverOn(Cell(5, 5), GridSide.MinusX), "a wall is Full Cover");
            Assert.AreEqual(CoverLevel.None, grid.CoverOn(Cell(5, 5), GridSide.PlusZ));
            Assert.AreEqual(CoverLevel.None, grid.CoverOn(Cell(5, 5), GridSide.MinusZ));
            Assert.AreEqual(CoverLevel.Half, grid.CoverOn(Cell(6, 5), GridSide.MinusX), "the fence covers its other side too");
            Assert.IsFalse(grid.CanStep(Cell(5, 5), Cell(6, 5)), "a fence closes its edge");
            Assert.IsFalse(grid.CanStep(Cell(5, 5), Cell(4, 5)), "a wall closes its edge");
        }

        [Test]
        public void AWallOnTheGridLineCoversOnlyTheSideItStandsOn()
        {
            // S3a.6: a side's strip is 0.6 m wide, the cell's middle, so a wall along one side never reads on the two
            // sides it meets at the corners.
            Block("Wall", 5.9f, 3f, 0.2f, 5f, 0f, 3f);
            var grid = Grid(5, 5);

            Assert.AreEqual(CoverLevel.Full, grid.CoverOn(Cell(5, 5), GridSide.PlusX), "the wall's side");
            Assert.AreEqual(CoverLevel.None, grid.CoverOn(Cell(5, 5), GridSide.PlusZ), "not the side it meets");
            Assert.AreEqual(CoverLevel.None, grid.CoverOn(Cell(5, 5), GridSide.MinusZ), "nor the other");
            Assert.AreEqual(CoverLevel.Full, grid.CoverOn(Cell(6, 5), GridSide.MinusX), "and the cell across it is covered the other way");
        }

        [Test]
        public void TheTagOfTheBlockInTheWayComesFromKitEnvironmentAction()
        {
            // The tag sits on the block's root and the collider on its child, as on a kit wrapper.
            var wrapper = new GameObject("LowWall");
            wrapper.AddComponent<KitEnvironmentAction>().Set(EnvironmentActionKind.VaultJump, EnvironmentActionBand.Easy);
            Block("Body", 5.9f, 3f, 0.2f, 5f, 0f, 1f).transform.SetParent(wrapper.transform, true);
            Block("Wall", 4.9f, 3f, 0.2f, 5f, 0f, 3f);
            var crate = Block("Crate", 5f, 6f, 1f, 1f, 0f, 1.2f);
            crate.AddComponent<KitEnvironmentAction>().Set(EnvironmentActionKind.MantleOnto, EnvironmentActionBand.Hard);
            var grid = Grid(5, 5);

            Assert.IsTrue(grid.TryGetEnvironmentAction(Cell(5, 5), Cell(6, 5), out var vault), "the low wall's tag");
            Assert.AreEqual((EnvironmentActionKind.VaultJump, EnvironmentActionBand.Easy), (vault.Action, vault.Band));
            Assert.IsTrue(grid.TryGetEnvironmentAction(Cell(5, 5), Cell(5, 6), out var mantle), "the crate's tag");
            Assert.AreEqual((EnvironmentActionKind.MantleOnto, EnvironmentActionBand.Hard), (mantle.Action, mantle.Band));
            Assert.IsFalse(grid.TryGetEnvironmentAction(Cell(5, 5), Cell(4, 5), out _), "a wall with no tag offers nothing");
            Assert.IsFalse(grid.TryGetEnvironmentAction(Cell(5, 5), Cell(5, 4), out _), "an open step needs no action");
        }

        [Test]
        public void OccupantsAreALayerApartFromThePlace()
        {
            var grid = Grid(5, 5);
            var reachable = grid.Window.ReachableCount;
            Assert.IsTrue(grid.Occupy(1, Cell(6, 5)));
            Assert.AreEqual(1, grid.OccupantAt(Cell(6, 5)));
            Assert.IsTrue(grid.IsWalkable(Cell(6, 5)), "an occupant changes nothing in the place");
            Assert.IsTrue(grid.CanStep(Cell(5, 5), Cell(6, 5)));
            grid.Build(5, 5, 0);
            Assert.AreEqual(reachable, grid.Window.ReachableCount, "nor what the place joins");

            var path = new List<CombatCell>();
            Assert.AreEqual(2, grid.FindPath(Cell(5, 5), Cell(7, 5), path), "round the occupant, two diagonals");
            CollectionAssert.DoesNotContain(path, Cell(6, 5));
            Assert.AreEqual(-1, grid.FindPath(Cell(5, 5), Cell(6, 5), path), "nobody walks into an occupied cell");

            Assert.IsFalse(grid.Occupy(2, Cell(6, 5)), "one occupant a cell");
            Assert.IsTrue(grid.Occupy(1, Cell(6, 6)), "an occupant moves");
            Assert.AreEqual(-1, grid.OccupantAt(Cell(6, 5)));
            grid.Vacate(1);
            Assert.AreEqual(-1, grid.OccupantAt(Cell(6, 6)));
            Assert.AreEqual(2, grid.FindPath(Cell(5, 5), Cell(7, 5), path));
            CollectionAssert.AreEqual(new[] { Cell(6, 5), Cell(7, 5) }, path, "straight, once the cell is free");
        }

        [Test]
        public void TheWindowFollowsThePlayerCellByCell()
        {
            var grid = Grid(5, 5);
            Assert.IsFalse(grid.Follow(new Vector3(5.9f, 0.05f, 5.1f)), "the same cell: no move");
            Assert.IsTrue(grid.Follow(new Vector3(6.2f, 0.05f, 5.5f)));
            Assert.AreEqual(Cell(6, 5), grid.Origin);
            Assert.IsTrue(grid.InBounds(6 + CombatGridReader.Reach, 5));
            Assert.IsFalse(grid.InBounds(7 + CombatGridReader.Reach, 5));
            Assert.IsTrue(grid.IsReachable(Cell(0, 0)) && grid.IsReachable(Cell(19, 19)), "the whole floor is joined");
            Assert.IsFalse(grid.IsWalkable(Cell(-1, 5)), "past the floor's edge nothing stands");

            // A grid moved cell by cell reads as one built where it stopped.
            for (var x = 7; x <= 12; x++)
            {
                grid.Follow(new Vector3(x + 0.5f, 0.05f, 5.5f));
            }

            var fresh = new CombatGridReader(1f);
            fresh.Follow(new Vector3(12.5f, 0.05f, 5.5f));
            Assert.AreEqual(fresh.Window.Hash, grid.Window.Hash);
            Assert.AreEqual(fresh.Window.ReachableCount, grid.Window.ReachableCount);
        }
    }
}

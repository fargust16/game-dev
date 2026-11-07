using System.Linq;
using GameDev.Grid;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Grid.Tests
{
    /// <summary>
    /// The Combat Grid on an upper storey (VICTORIAN_INTERIORS_PLAN VI-3): a fight on the Soot &amp; Bell's landing reads
    /// the landing, not the hall under it, and the stair joins the two storeys on the grid.
    /// </summary>
    public sealed class CombatGridStoreyTests
    {
        private const string SootAndBell = "Assets/Scenes/Slice/Interiors/SootAndBell.unity";
        private const float Storey = 3.5f;

        [Test]
        public void AFightOnTheLandingReadsTheLandingAndTheStairJoinsTheStoreys()
        {
            EditorSceneManager.OpenScene(SootAndBell, OpenSceneMode.Single);
            Physics.SyncTransforms();
            var reader = new CombatGridReader(1f);

            // On the landing past the stair's top, over the stair hall.
            Assert.IsTrue(reader.TryFindStart(new Vector3(19.5f, Storey + 0.1f, 16.5f), out var ix, out var iz, out var level), "a cell on the landing");
            var start = reader.Column(ix, iz)[level].Y;
            Assert.AreEqual(Storey, start, 0.1f, "the fight's cell is on the landing");

            var window = reader.Build(ix, iz, level);
            var heights = window.Reachable.Select(key =>
            {
                CombatGridReader.Unpack(key, out var x, out var z, out var l);
                return reader.Column(x, z)[l].Y;
            }).ToList();
            Assert.Greater(heights.Count(y => Mathf.Abs(y - Storey) < 0.1f), 40, "the landing and the let rooms are on the grid");
            Assert.Greater(heights.Count(y => y > 0.5f && y < Storey - 0.5f), 3, "the stair's steps are on the grid");
            Assert.Greater(heights.Count(y => Mathf.Abs(y) < 0.1f), 20, "the ground floor is reached down the stair");

            // In the hall under the landing, a fight reads the hall.
            Assert.IsTrue(reader.TryFindStart(new Vector3(19.5f, 0.1f, 16.5f), out ix, out iz, out level), "a cell in the hall");
            Assert.AreEqual(0f, reader.Column(ix, iz)[level].Y, 0.1f, "the fight's cell is on the ground floor");
        }
    }
}

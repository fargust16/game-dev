#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Slice.People;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// The ordinary townsfolk walk their routes in Veilmouth, on the ground (owner, 2026-11-06), and stand a while at
    /// some of its turns (the owner's play test of S5a).
    /// </summary>
    public sealed class TownsfolkTests : SliceInputTestBase
    {
        [UnityTest]
        public IEnumerator InVeilmouthTheTownsfolkWalkTheirRoutesOnTheGround()
        {
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var walkers = Object.FindObjectsByType<TownsfolkWalker>(FindObjectsSortMode.None);
            Assert.AreEqual(46, walkers.Length, "every yellow townsperson walks: nineteen and their farmer, on C5 (city plan §28.10, §29.4), and the play test's 26");
            var start = walkers.Select(w => w.transform.position).ToArray();
            var stoodAtStart = walkers.Select(w => w.Standing > 0f).ToArray();
            var stopsAtStart = walkers.Select(w => w.Stops).ToArray();

            yield return new WaitForSeconds(3f);

            var ground = LayerMask.GetMask("Ground", "Structure");
            var standing = 0;
            for (var i = 0; i < walkers.Length; i++)
            {
                var w = walkers[i];
                var at = w.transform.position;
                var animator = w.GetComponentInChildren<Animator>();
                var stopped = stoodAtStart[i] || w.Stops != stopsAtStart[i];
                if (stopped)
                {
                    // A stop lasts 4 s at least, so one begun in these 3 s still stands.
                    standing++;
                    Assert.IsTrue(stoodAtStart[i] || w.Standing > 0f, $"{w.name} stands at its turn");
                    Assert.IsTrue(w.Standing <= 0f || animator.GetCurrentAnimatorStateInfo(0).IsName("Idle") || animator.IsInTransition(0), $"{w.name} stands in its idle");
                }
                else
                {
                    var moved = new Vector2(at.x - start[i].x, at.z - start[i].z).magnitude;
                    Assert.Greater(moved, 1.5f, $"{w.name} walks");
                    Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"), $"{w.name} plays its walk");
                }

                Assert.IsTrue(Physics.Raycast(at + (Vector3.up * 1.5f), Vector3.down, out var hit, 4f, ground), $"{w.name} has ground under it");
                Assert.AreEqual(hit.point.y, at.y, 0.1f, $"{w.name} stands on the ground");
                Assert.Less(DistanceToRoute(w.Route, at), 0.5f, $"{w.name} keeps to its route");
            }

            Assert.Less(standing, walkers.Length / 2, "most walk, some stand");
        }

        private static float DistanceToRoute(Vector3[] route, Vector3 at)
        {
            var p = new Vector2(at.x, at.z);
            var best = float.MaxValue;
            for (var i = 0; i < route.Length; i++)
            {
                var a = new Vector2(route[i].x, route[i].z);
                var b = new Vector2(route[(i + 1) % route.Length].x, route[(i + 1) % route.Length].z);
                var t = Mathf.Clamp01(Vector2.Dot(p - a, b - a) / Mathf.Max(1e-6f, (b - a).sqrMagnitude));
                best = Mathf.Min(best, (a + ((b - a) * t) - p).magnitude);
            }

            return best;
        }
    }
}
#endif

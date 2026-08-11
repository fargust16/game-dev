using System.Collections.Generic;
using GameDev.World.Roads;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Roads.Tests
{
    public sealed class RoadPathDensifierTests
    {
        [Test]
        public void Densify_SinglePoint_ReturnsItUnchanged()
        {
            var result = RoadPathDensifier.Densify(new List<Vector2> { new Vector2(5f, 5f) }, 2f);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(new Vector2(5f, 5f), result[0]);
        }

        [Test]
        public void Densify_StartsAtTheFirstCoarsePoint()
        {
            var coarse = new List<Vector2> { new Vector2(0f, 0f), new Vector2(100f, 0f) };
            var result = RoadPathDensifier.Densify(coarse, 5f);
            Assert.AreEqual(new Vector2(0f, 0f), result[0]);
        }

        [Test]
        public void Densify_EndsAtTheLastCoarsePoint()
        {
            var coarse = new List<Vector2> { new Vector2(0f, 0f), new Vector2(100f, 0f) };
            var result = RoadPathDensifier.Densify(coarse, 5f);
            Assert.AreEqual(new Vector2(100f, 0f), result[result.Count - 1]);
        }

        [Test]
        public void Densify_NoOutputStepExceedsRequestedSpacingByMuch()
        {
            var coarse = new List<Vector2> { new Vector2(0f, 0f), new Vector2(97f, 0f), new Vector2(97f, 33f) };
            var result = RoadPathDensifier.Densify(coarse, 4f);

            for (var i = 1; i < result.Count; i++)
            {
                Assert.LessOrEqual(Vector2.Distance(result[i - 1], result[i]), 4f * 1.5f,
                    "rounding to a whole number of steps per segment should never blow the spacing far past the requested step");
            }
        }

        [Test]
        public void Densify_PreservesEveryOriginalWaypoint()
        {
            // Each coarse waypoint (not just the first/last) should still land exactly on the dense
            // output, since it's where the original path actually turns.
            var coarse = new List<Vector2> { new Vector2(0f, 0f), new Vector2(50f, 0f), new Vector2(50f, 50f) };
            var result = RoadPathDensifier.Densify(coarse, 7f);

            foreach (var waypoint in coarse)
            {
                var found = false;
                foreach (var p in result)
                {
                    if (Vector2.Distance(p, waypoint) < 0.001f)
                    {
                        found = true;
                        break;
                    }
                }

                Assert.IsTrue(found, $"waypoint {waypoint} missing from densified output");
            }
        }

        [Test]
        public void Densify_DuplicateConsecutivePoints_DoesNotProduceNaNOrInfiniteLoop()
        {
            var coarse = new List<Vector2> { new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10f, 0f) };
            var result = RoadPathDensifier.Densify(coarse, 3f);

            Assert.Greater(result.Count, 0);
            foreach (var p in result)
            {
                Assert.IsFalse(float.IsNaN(p.x) || float.IsNaN(p.y));
            }
        }
    }
}

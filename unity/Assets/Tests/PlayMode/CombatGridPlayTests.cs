#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Confront.Grid;
using GameDev.Slice.Kit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S3a.3 in play: the player walks a Veilmouth street with the Combat Grid following it, a cell at a time, and once
    /// the walk has been made a first time no step of the grid allocates.
    /// </summary>
    public sealed class CombatGridPlayTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";
        private const float Speed = 2f; // m/s, a walk
        private const int Cells = 5;

        [UnityTest]
        public IEnumerator TheGridFollowsThePlayerDownAStreetAndItsStepsAllocateNothing()
        {
            yield return LoadScene(Veilmouth);
            var controller = Object.FindObjectsByType<CharacterController>(FindObjectsInactive.Exclude)
                .FirstOrDefault(c => c.GetComponent<PushLooseProps>() != null);
            Assert.IsNotNull(controller, "the player");

            var grid = new CombatGridReader(1f);
            Assert.IsTrue(grid.Follow(controller.transform.position), "the grid forms round the player");
            var start = grid.Origin;
            var home = grid.Center(start);
            var direction = Vector3.zero;
            int[] dirX = { 1, -1, 0, 0 }, dirZ = { 0, 0, 1, -1 };
            for (var d = 0; d < 4 && direction == Vector3.zero; d++)
            {
                var open = true;
                for (var k = 1; k <= Cells + 1 && open; k++)
                {
                    int x = start.X + (k * dirX[d]), z = start.Z + (k * dirZ[d]);
                    var level = grid.LevelNear(x, z, home.y);
                    open = level >= 0 && grid.IsReachable(new CombatCell(x, z, level));
                }

                direction = open ? new Vector3(dirX[d], 0f, dirZ[d]) : Vector3.zero;
            }

            Assert.AreNotEqual(Vector3.zero, direction, $"a street with {Cells + 1} cells in a line from the start");
            controller.enabled = false;
            controller.transform.position = home + (Vector3.up * 0.05f);
            controller.enabled = true;
            yield return new WaitForFixedUpdate();

            var position = Vector3.zero;
            var moved = false;
            TestDelegate follow = () => moved = grid.Follow(position);
            var measured = 0;
            for (var pass = 0; pass < 2; pass++)
            {
                foreach (var target in new[] { home + (direction * Cells), home })
                {
                    for (var time = 0f; time < 10f; time += Time.deltaTime)
                    {
                        var toward = target - controller.transform.position;
                        toward.y = 0f;
                        if (toward.magnitude < 0.1f)
                        {
                            break;
                        }

                        controller.Move(((toward.normalized * Speed) + (Vector3.down * 2f)) * Time.deltaTime);
                        position = controller.transform.position;
                        if (pass == 0)
                        {
                            follow();
                        }
                        else
                        {
                            Assert.That(follow, Is.Not.AllocatingGCMemory(), $"a step to {position}");
                        }

                        if (moved)
                        {
                            measured += pass;
                            Assert.AreEqual(Mathf.FloorToInt(position.x), grid.Origin.X, $"the grid's cell under the player at {position}");
                            Assert.AreEqual(Mathf.FloorToInt(position.z), grid.Origin.Z, $"the grid's cell under the player at {position}");
                        }

                        yield return null;
                    }

                    var arrived = target - controller.transform.position;
                    arrived.y = 0f;
                    Assert.Less(arrived.magnitude, 0.5f, $"the walk reached {target}; it stopped at {controller.transform.position}");
                }
            }

            Assert.GreaterOrEqual(measured, 2 * Cells, "the measured walk stepped the grid out and back");
        }
    }
}
#endif

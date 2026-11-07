#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Slice.Dwellings;
using GameDev.Slice.Kit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// Every stair of the interiors is walked up and down by the player's own character controller
    /// (VICTORIAN_INTERIORS_PLAN VI-3): from its foot to the floor beyond its top, a storey higher, and back.
    /// </summary>
    public sealed class StairTests : SliceInputTestBase
    {
        private const float Storey = 3.5f;
        private const float Speed = 2f; // m/s, a walk

        [UnityTest]
        public IEnumerator ThePlayerWalksUpAndDownEveryStair([Values("SootAndBell")] string scene)
        {
            yield return LoadScene($"Assets/Scenes/Slice/Interiors/{scene}.unity");
            yield return WalkEveryStair(scene);
        }

        /// <summary>A rich house of each kind, built in the Dwelling scene, walked up to its top floor and down (VI-4).</summary>
        [UnityTest]
        public IEnumerator ThePlayerWalksEveryStairOfARichHouse([Values("Terrace", "Villa", "Shop")] string archetype)
        {
            yield return LoadScene("Assets/Scenes/Slice/Interiors/Dwelling.unity");
            var runtime = DwellingRuntime.Current;
            Assert.IsNotNull(runtime, "the Dwelling scene");
            var kind = (DwellingArchetype)System.Enum.Parse(typeof(DwellingArchetype), archetype);
            foreach (var x in new[] { 1000, 1037 })
            {
                runtime.Build(new DwellingKey(DwellingKey.IdAt(x, 0), "Veilmouth", x, 0, kind, DwellingWealth.Rich));
                yield return new WaitForFixedUpdate();
                yield return WalkEveryStair($"a rich {archetype} at {x}", 2);
            }
        }

        private IEnumerator WalkEveryStair(string scene, int count = 0)
        {
            var stairs = Object.FindObjectsByType<KitBlock>(FindObjectsInactive.Exclude).Where(b => b.name == "Stair").ToArray();
            Assert.IsNotEmpty(stairs, $"{scene} has a stair");
            if (count > 0)
            {
                Assert.AreEqual(count, stairs.Length, $"{scene}: a stair to each floor");
            }
            var controller = Object.FindObjectsByType<CharacterController>(FindObjectsInactive.Exclude)
                .FirstOrDefault(c => c.GetComponent<PushLooseProps>() != null);
            Assert.IsNotNull(controller, "the player");

            foreach (var stair in stairs)
            {
                // The stair's local frame: its footprint from its corner, 3 m across X, climbing along +Z to its top at 7 m.
                var t = stair.transform;
                var foot = t.TransformPoint(new Vector3(1.5f, 0f, -0.6f));
                var up = t.forward;
                var floor = t.position.y;
                controller.enabled = false;
                controller.transform.position = foot + (Vector3.up * 0.05f);
                controller.enabled = true;
                yield return new WaitForFixedUpdate();

                yield return Walk(controller, up, () => Vector3.Dot(controller.transform.position - foot, up) > 8.2f);
                Assert.AreEqual(floor + Storey, controller.transform.position.y, 0.2f, $"{stair.name} at {t.position}: up a storey");

                yield return Walk(controller, -up, () => Vector3.Dot(controller.transform.position - foot, up) < 0f);
                Assert.AreEqual(floor, controller.transform.position.y, 0.2f, $"{stair.name} at {t.position}: back down");
            }
        }

        private static IEnumerator Walk(CharacterController controller, Vector3 direction, System.Func<bool> arrived)
        {
            for (var time = 0f; time < 10f && !arrived(); time += Time.deltaTime)
            {
                controller.Move(((direction * Speed) + (Vector3.down * 2f)) * Time.deltaTime);
                yield return null;
            }

            Assert.IsTrue(arrived(), $"the walk {direction} arrived; stopped at {controller.transform.position}");
            for (var i = 0; i < 10; i++)
            {
                controller.Move(Vector3.down * 2f * Time.deltaTime);
                yield return null;
            }
        }
    }
}
#endif

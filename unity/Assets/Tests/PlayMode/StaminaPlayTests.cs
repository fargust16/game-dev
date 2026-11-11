#if UNITY_EDITOR
using System.Collections;
using GameDev.Locomotion;
using GameDev.Slice.Gathering;
using GameDev.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// The owner's play test of S5a in Veilmouth (07.2 §3.2): the sprint spends Stamina, its bar shows while any is spent,
    /// and once it runs out the character cannot sprint until a tenth has come back after the rest.
    /// </summary>
    public sealed class StaminaPlayTests : SliceInputTestBase
    {
        [UnityTest]
        public IEnumerator TheSprintSpendsStaminaAndRunningOutStopsIt()
        {
            yield return ForgetTheWorld();
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var world = SliceWorldDriver.Running;
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var hud = Object.FindAnyObjectByType<HudView>();
            Assert.IsNotNull(world?.Stamina, "the world keeps Stamina");
            Assert.AreEqual(1d, world.Stamina.Share, 1e-9);
            Assert.IsFalse(hud.IsStaminaShown, "no bar while Stamina is full");

            Hold(Key.W, Key.LeftShift);
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(player.IsSprinting, Describe(player));
            Hold();
            yield return null;
            Assert.That(world.Stamina.Share, Is.InRange(0.95d, 0.995d), "about 2 % a second of sprint");
            Assert.IsTrue(hud.IsStaminaShown, "the bar shows while Stamina is spent");
            Assert.Less(hud.StaminaShown, 1f);

            world.Stamina.Spend(1d);
            Hold(Key.W, Key.LeftShift);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(world.Stamina.Tired);
            Assert.IsFalse(player.SprintAllowed, "run out, no sprint");
            Assert.IsFalse(player.IsSprinting);
            Assert.AreEqual(0d, world.Stamina.Share, 1e-9, "nothing comes back in the rest");
            Hold();
            yield return null;

            // A tick after the rest of 2.5 s, a quarter of a tick, gives back three quarters of 5 %: still tired.
            world.Clock.AdvanceTicks(1);
            yield return null;
            Assert.That(world.Stamina.Share, Is.InRange(0.03d, 0.06d), "5 % a tick after the rest");
            Assert.IsTrue(world.Stamina.Tired, "tired until a tenth has come back");
            world.Clock.AdvanceTicks(2);
            yield return null;
            yield return null;
            Assert.IsFalse(world.Stamina.Tired);
            Assert.IsTrue(player.SprintAllowed, "a tenth back, the sprint again");

            world.State.Set(SliceStamina.SpentKey, 0);
            yield return null;
            Assert.IsFalse(hud.IsStaminaShown, "full, the bar goes");
        }
    }
}
#endif

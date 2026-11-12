#if UNITY_EDITOR
using System.Collections;
using GameDev.Items;
using GameDev.Locomotion;
using GameDev.Slice.Fights;
using GameDev.Slice.Gear;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// 10.2 §5c in free exploration, in Veilmouth (WA2): R draws the active Weapon Set and sheathes it again; with no
    /// weapon the character stands in the fists' guard with empty hands, with a sword the sword is in its right hand; a
    /// click plays the blow at the air, the body held still while it lasts; a sprint sheathes the weapon.
    /// </summary>
    public sealed class ArmsPlayTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";
        private static readonly int AttackState = Animator.StringToHash("Attack");

        [UnityTest]
        public IEnumerator RDrawsTheSetAndAClickStrikesTheAir()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            var arms = world?.Arms;
            Assert.IsNotNull(arms, "the world carries the player's arms");
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var animator = player.GetComponentInChildren<Animator>();
            for (var t = 0f; t < 2f && !player.IsGrounded; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsTrue(player.IsGrounded, Describe(player));

            // Fists: R draws nothing into the hands, and the body stands in the fists' guard.
            yield return Tap(Key.R);
            Assert.IsTrue(arms.Drawn, "R draws");
            Assert.AreEqual(FightBeats.Fists, arms.Motion, "with no weapon the character fights with its fists");
            var held = player.GetComponent<HeldWeapons>();
            Assert.IsTrue(held == null || held.Main == null || !held.Main.activeSelf, "fists show no model");
            yield return Wait(0.2f);
            Assert.IsTrue(animator.GetBool("Fighting"), "standing still, the body holds its guard");

            // A click strikes the air: the blow plays and the body stays where it is until it ends.
            yield return Wait(PlayerArms.DrawSeconds);
            Assert.IsTrue(arms.Strike(), "a click after the draw strikes");
            Assert.IsTrue(arms.Striking);
            Assert.IsFalse(player.enabled, "the body is held while the blow lasts");
            yield return Wait(0.15f);
            Assert.IsTrue(Playing(animator, AttackState), "the blow plays");
            for (var t = 0f; t < 3f && arms.Striking; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsFalse(arms.Striking, "the blow ends");
            Assert.IsTrue(player.enabled, "and the body moves again");

            yield return Tap(Key.R);
            Assert.IsFalse(arms.Drawn, "R again sheathes");
            Assert.IsFalse(animator.GetBool("Fighting"), "and the guard is left");

            // A sword: R draws it into the right hand.
            var sword = new ItemPiece("sword");
            Assert.IsTrue(world.Bag.AddPiece(sword));
            Assert.IsNull(world.Outfit.Wear(sword));
            yield return Tap(Key.R);
            Assert.IsTrue(arms.Drawn);
            Assert.AreEqual(FightBeats.OneHand, arms.Motion, "a sword's guard and blow");
            held = player.GetComponent<HeldWeapons>();
            Assert.IsNotNull(held, "the body holds its weapons");
            Assert.IsNotNull(held.Main, "the sword has its model");
            Assert.IsTrue(held.Main.activeSelf, "the sword is in sight");
            Assert.AreSame(animator.GetBoneTransform(HumanBodyBones.RightHand), held.Main.transform.parent, "in the right hand");

            // A sprint puts it away.
            Hold(Key.W, Key.LeftShift);
            for (var t = 0f; t < 2f && arms.Drawn; t += Time.deltaTime)
            {
                yield return null;
            }

            Hold();
            Assert.IsFalse(arms.Drawn, "a sprint sheathes the weapon");
            Assert.IsFalse(held.Main.activeSelf, "and it is out of sight");
        }

        private static bool Playing(Animator animator, int state) =>
            animator.GetCurrentAnimatorStateInfo(0).shortNameHash == state || animator.GetNextAnimatorStateInfo(0).shortNameHash == state;

        private static IEnumerator Wait(float seconds)
        {
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                yield return null;
            }
        }
    }
}
#endif

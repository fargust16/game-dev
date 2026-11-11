#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.Kit;
using GameDev.Slice.People;
using GameDev.Slice.Stealth;
using GameDev.Slice.Theft;
using GameDev.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5a.8 in Veilmouth: beside a walking townsperson E shows the Reaction Check's bar; Space with the marker in the
    /// good zone lifts the purse unseen, and a marker run out means the next person notices, cries out and the Deeds
    /// Record keeps it.
    /// </summary>
    public sealed class TheftPlayTests : SliceInputTestBase
    {
        // The Deeds Record's Reputation in the world state (`SocialStanding.ReputationKey`).
        private const string Reputation = "deeds.reputation";

        private string _language;

        [SetUp]
        public void KeepLanguage()
        {
            _language = Loc.Language;
            Loc.Language = Loc.English;
        }

        [TearDown]
        public void RestoreLanguage()
        {
            Loc.Language = _language;
        }

        [UnityTest]
        public IEnumerator APurseIsLiftedAndATheftNoticed()
        {
            yield return ForgetTheWorld();
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var world = SliceWorldDriver.Running;
            Assert.IsNotNull(world?.Theft, "the world keeps theft");
            var targets = Object.FindObjectsByType<PurseTarget>(FindObjectsInactive.Exclude);
            Assert.AreEqual(Object.FindObjectsByType<TownsfolkWalker>(FindObjectsInactive.Exclude).Length, targets.Length, "a purse on every walker");
            Assert.AreEqual(targets.Length, targets.Select(t => t.Person).Distinct().Count(), "each one its own name");

            // A practised hand: the zone at its widest, 60 %.
            world.Character.Set(CharacterAttribute.Dexterity, 30);
            world.Character.Set(CharacterSkill.Theft, 30);
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var hud = Object.FindAnyObjectByType<HudView>();
            yield return Choose(targets, null, player);
            var first = _chosen;
            Assert.IsNotNull(first, "a walker with no other prompt beside it");
            Assert.AreEqual("E  Lift the purse", first.PromptFor(player.transform.position));
            var bronze = world.Bag.Bronze;
            yield return Tap(Key.E);
            Assert.IsTrue(world.Reaction.Running, "E shows the bar");
            Assert.IsTrue(hud.IsReactionShown);
            Assert.IsFalse(player.enabled, "the character stands still while it shows");
            Assert.AreEqual(0.6d, world.Reaction.Bar.GoodWidth, 1e-9);

            var bar = world.Reaction.Bar;
            for (var t = 0f; t < 3f && world.Reaction.Marker < bar.GoodStart + (bar.GoodWidth * 0.3d); t += Time.deltaTime)
            {
                yield return null;
            }

            yield return Tap(Key.Space);
            Assert.IsFalse(world.Reaction.Running, "Space stops the marker");
            Assert.IsFalse(hud.IsReactionShown);
            Assert.IsTrue(player.enabled);
            Assert.That(world.Bag.Bronze - bronze, Is.InRange(SliceTheft.PurseLeast, SliceTheft.PurseMost), "the purse lifted unseen");
            Assert.IsTrue(world.Theft.TriedToday(first.Person));
            Assert.AreEqual(0, world.State.Get(Reputation));

            // A clumsy hand who lets the marker run out.
            world.Character.Set(CharacterAttribute.Dexterity, 1);
            world.Character.Set(CharacterSkill.Theft, 1);
            yield return Choose(targets, first, player);
            var second = _chosen;
            Assert.IsNotNull(second, "a second walker");
            bronze = world.Bag.Bronze;
            yield return Tap(Key.E);
            Assert.IsTrue(world.Reaction.Running);
            for (var t = 0f; t < 3f && world.Reaction.Running; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsFalse(world.Reaction.Running, "the marker leaves the bar");
            Assert.AreEqual(bronze, world.Bag.Bronze, "nothing taken");
            Assert.Less(world.State.Get(Reputation), 0, "the noticed theft counts against Reputation");
            var eyes = second.GetComponent<StealthObserver>();
            if (eyes != null)
            {
                Assert.AreEqual("Thief!", eyes.Line, "the person cries out");
                Assert.IsTrue(eyes.Meter == null || eyes.Meter.Noticed, "and has noticed the character");
            }

            Assert.AreEqual("Noticed! Nothing taken.", second.PromptFor(player.transform.position));
        }

        private PurseTarget _chosen;

        /// <summary>The first walker, other than <paramref name="not"/>, behind which no other prompt answers E: no door, talk or counter.</summary>
        private IEnumerator Choose(PurseTarget[] targets, PurseTarget not, PlayerLocomotion player)
        {
            _chosen = null;
            foreach (var target in targets.Where(t => t != not))
            {
                yield return StandBehind(target, player);
                var at = player.transform.position;
                var others = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude)
                    .OfType<IPromptSource>().Where(s => !ReferenceEquals(s, target) && s.PromptFor(at) != null);
                if (target.PromptFor(at) != null && !others.Any())
                {
                    _chosen = target;
                    yield break;
                }

                target.GetComponent<TownsfolkWalker>().enabled = true;
            }
        }

        /// <summary>Holds the walker where it stands and puts the character a metre behind it.</summary>
        private static IEnumerator StandBehind(PurseTarget target, PlayerLocomotion player)
        {
            var walker = target.GetComponent<TownsfolkWalker>();
            walker.enabled = false;
            var back = -target.transform.forward;
            back.y = 0f;
            back = back.sqrMagnitude > 1e-4f ? back.normalized : Vector3.back;
            var probe = target.transform.position + back;
            var ground = LayerMask.GetMask(KitLayers.Ground, KitLayers.Structure);
            var at = Physics.Raycast(probe + (Vector3.up * 3f), Vector3.down, out var hit, 10f, ground, QueryTriggerInteraction.Ignore)
                ? hit.point : probe;
            player.Teleport(at + (Vector3.up * 0.05f), Quaternion.LookRotation(-back));
            yield return new WaitForSeconds(0.3f);
        }
    }
}
#endif

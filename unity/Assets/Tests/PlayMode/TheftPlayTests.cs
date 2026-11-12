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
    /// S5a.8 in Veilmouth: beside a walking townsperson E sends the crouched hand to the pocket, where the Reaction
    /// Check's bar shows, the world running on (AN3); Space with the marker in the good zone lifts the purse unseen, a
    /// marker run out means the next person notices, cries out and the Deeds Record keeps it, and a person who moves 1 m
    /// calls the try off unspent.
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

            // A practised hand: the zone at its widest, 35 %, and a quiet step so the walkers do not notice it sneak up.
            world.Character.Set(CharacterAttribute.Dexterity, 30);
            world.Character.Set(CharacterSkill.Theft, 30);
            world.Character.Set(CharacterSkill.Stealth, 30);
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var hud = Object.FindAnyObjectByType<HudView>();
            PurseTarget first = null;
            yield return Choose(targets, null, player, t => first = t);
            Assert.IsNotNull(first, "a walker with no other prompt beside it");
            Assert.AreEqual("E  Lift the purse", first.PromptFor(player.transform.position));
            world.Hiding.SetActive(false);
            Assert.IsNull(first.PromptFor(player.transform.position), "theft only in Stealth Mode");
            Assert.IsTrue(world.Hiding.SetActive(true));
            var bronze = world.Bag.Bronze;
            yield return Tap(Key.E);
            Assert.IsTrue(world.Work.Busy, "E sends the hand to the pocket");
            yield return GatheringPlayTests.AtKey(world);
            Assert.IsTrue(world.Reaction.Running, "where it reaches in the bar shows");
            Assert.IsTrue(hud.IsReactionShown);
            Assert.IsFalse(player.enabled, "the character stands still while it shows");
            Assert.IsFalse(world.Clock.IsPaused, "theft does not pause the world (the owner, 2026-11-11)");
            Assert.AreEqual(0.35d, world.Reaction.Bar.GoodWidth, 1e-9);
            Assert.AreEqual(1.5d, world.Reaction.Bar.Seconds, 1e-9, "the slowest marker");

            var bar = world.Reaction.Bar;
            for (var t = 0f; t < 3f && world.Reaction.Marker < bar.GoodStart + (bar.GoodWidth * 0.3d); t += Time.deltaTime)
            {
                yield return null;
            }

            yield return Tap(Key.Space);
            Assert.IsFalse(world.Reaction.Running, "Space stops the marker");
            Assert.IsFalse(hud.IsReactionShown);
            Assert.IsNotNull(hud.MessageText, "the outcome is said on the screen at once (owner, 2026-11-12)");
            yield return GatheringPlayTests.PlayedOut(world);
            Assert.IsTrue(player.enabled, "the hand drawn back, the character moves again");
            Assert.That(world.Bag.Bronze - bronze, Is.InRange(SliceTheft.PurseLeast, SliceTheft.PurseMost), "the purse lifted unseen");
            Assert.IsTrue(world.Theft.TriedToday(first.Person));
            Assert.AreEqual(0, world.State.Get(Reputation));

            // A clumsy hand who lets the marker run out.
            world.Character.Set(CharacterAttribute.Dexterity, 1);
            world.Character.Set(CharacterSkill.Theft, 1);
            PurseTarget second = null;
            yield return Choose(targets, first, player, t => second = t);
            Assert.IsNotNull(second, "a second walker");
            bronze = world.Bag.Bronze;
            yield return Tap(Key.E);
            yield return GatheringPlayTests.AtKey(world);
            Assert.IsTrue(world.Reaction.Running);
            Assert.Less(world.Reaction.Bar.Seconds, 1.5d, "a clumsy hand's marker runs faster");
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

            Assert.AreEqual(Loc.Get("ui.theft.noticed"), hud.MessageText, "on the screen, not on the person");
            Assert.AreNotEqual(hud.MessageText, second.PromptFor(player.transform.position));
            yield return GatheringPlayTests.PlayedOut(world);

            // A person who moves on calls the try off: the bar goes, nothing is spent, and it may be tried again.
            PurseTarget third = null;
            yield return Choose(targets.Where(t => t != first).ToArray(), second, player, t => third = t);
            Assert.IsNotNull(third, "a third walker");
            yield return Tap(Key.E);
            yield return GatheringPlayTests.AtKey(world);
            Assert.IsTrue(world.Reaction.Running);
            third.transform.position += Vector3.ProjectOnPlane(third.transform.forward, Vector3.up).normalized * 1.2f;
            yield return null;
            yield return null;
            Assert.IsFalse(world.Reaction.Running, "1 m away the bar goes");
            Assert.IsFalse(world.Work.Busy, "and the work with it");
            Assert.IsFalse(world.Theft.TriedToday(third.Person), "the try is not spent");
            Assert.AreEqual(Loc.Get("ui.theft.called_off"), hud.MessageText, "why is said on the screen");
            Assert.AreEqual(Loc.Get("ui.prompt.theft"), third.PromptFor(third.transform.position),
                "and the Screen Message keeps nothing from the next try (owner, 2026-11-12)");
        }

        /// <summary>
        /// Finds the first walker, other than <paramref name="not"/>, behind which no other prompt answers E: no door,
        /// talk or counter. It stands held with the character behind it in Stealth Mode, and <paramref name="found"/> is
        /// given it.
        /// </summary>
        internal static IEnumerator Choose(PurseTarget[] targets, PurseTarget not, PlayerLocomotion player, System.Action<PurseTarget> found)
        {
            foreach (var target in targets.Where(t => t != not))
            {
                yield return StandBehind(target, player);
                SliceWorldDriver.Running.Hiding.SetActive(true);
                var at = player.transform.position;
                var others = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude)
                    .OfType<IPromptSource>().Where(s => !ReferenceEquals(s, target) && s.PromptFor(at) != null);
                if (target.PromptFor(at) != null && !others.Any())
                {
                    found(target);
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

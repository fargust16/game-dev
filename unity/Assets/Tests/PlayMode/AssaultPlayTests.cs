#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Confront.Turns;
using GameDev.Core.Localization;
using GameDev.Items;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Player.Standing;
using GameDev.Slice.Fights;
using GameDev.Slice.Gear;
using GameDev.Slice.People;
using GameDev.Slice.Stealth;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// 10.2 §5c in Veilmouth (WA3): a click at a resident who is not fighting is an Assault. A fight starts at once, the
    /// one struck stops its walk and fights back, and the Wardens nearest the place come: one on post a few metres away
    /// joins at the end of the first round, or, if the fight is over first, fights the character where it stands.
    /// </summary>
    public sealed class AssaultPlayTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";

        [UnityTest]
        public IEnumerator AClickAtAResidentStartsAFightAndTheWardensCome()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            var fights = world.Fights;
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            for (var t = 0f; t < 2f && !player.IsGrounded; t += Time.deltaTime)
            {
                yield return null;
            }

            // A Warden on post, and a walker brought before it; the character beside the walker, facing it.
            var warden = PeopleAround.Wardens(player.transform.position).FirstOrDefault();
            Assert.IsNotNull(warden, "Veilmouth has Wardens on post");
            var walker = Object.FindObjectsByType<TownsfolkWalker>(FindObjectsSortMode.InstanceID).First(w => w.isActiveAndEnabled && !w.Inside);
            walker.enabled = false;
            var way = Vector3.ProjectOnPlane(warden.forward, Vector3.up).normalized;
            var place = Ground(warden.position + (way * 4f));
            walker.transform.position = place;
            player.Teleport(Ground(place + (way * 1.2f)), Quaternion.LookRotation(-way));
            for (var t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                yield return null;
            }

            // A strong character with a sword and the plate set, its turns played by the rule: it stands against the Wardens'
            // firearms (10.2 §5b, plate beats shooters; PB6 brings every Warden within the call, and the one struck runs).
            var character = world.Character;
            character.Set(CharacterAttribute.Strength, 20);
            character.Set(CharacterAttribute.Endurance, 20);
            character.Set(CharacterAttribute.Dexterity, 10);
            character.Set(CharacterAttribute.Perception, 8);
            character.Set(CharacterSkill.MeleeSkill, 20);
            foreach (var id in new[] { "sword", "plate_head", "plate_body", "plate_shoulders", "plate_gloves", "plate_trousers", "plate_boots" })
            {
                var piece = new ItemPiece(id);
                Assert.IsTrue(world.Bag.AddPiece(piece), id);
                Assert.IsNull(world.Outfit.Wear(piece), id);
            }
            fights.TurnPause = 0f;
            fights.AutoPlayer = true;
            fights.BeatPace = 6f;

            Assert.IsTrue(world.Arms.Draw(), "R draws");
            for (var t = 0f; t < PlayerArms.DrawSeconds + 0.05f; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsTrue(world.Arms.Strike(), "a click strikes " + Describe(player));
            Assert.IsTrue(fights.InFight, "a blow at a resident starts a fight at once");
            Assert.IsTrue(fights.Assaulting, "an Assault");
            Assert.IsFalse(walker.enabled, "the one struck leaves its walk");
            Assert.That(fights.WardensComing, Is.GreaterThanOrEqualTo(SliceConfront.AnsweringWardens), "every Warden within the call and the two nearest beyond it come");
            Assert.IsTrue(fights.Turns.Order.Any(c => c.Id == 1), "the one struck is in the fight");

            // PB6, 10.2 §5a: a resident fights back only with a chance, runs for help otherwise, and calls; those near hear.
            var struck = fights.Turns.Order.First(c => c.Id == 1);
            Assert.AreEqual(SliceConfront.FightsBackAt, struck.FightsAt, "the one struck fights only with a chance");
            Assert.IsTrue(struck.HelpAt.HasValue, "and knows where the nearest Warden is");
            var cry = walker.GetComponentInChildren<StealthObserver>();
            Assert.IsNotNull(cry, "the walker watches the city");
            Assert.AreEqual(Loc.Get("ui.fight.help.call"), cry.Line, "the one struck calls for help");
            Assert.Greater(cry.LineUntil, Time.time, "its line hangs over its head");
            var heard = warden.GetComponentInChildren<StealthObserver>();
            Assert.IsNotNull(heard, "the Warden watches the city");
            Assert.AreEqual(Loc.Get("ui.fight.help.warden"), heard.Line, "the Warden four metres off hears it");

            var wardenFought = false;
            for (var t = 0f; t < 180f && (fights.InFight || fights.WardensComing > 0); t += Time.deltaTime)
            {
                wardenFought |= fights.InFight && warden.TryGetComponent<SliceFighter>(out var fighter) && fighter.Combatant != null &&
                                fighter.Combatant.State == CombatantState.Fighting;
                yield return null;
            }

            Assert.IsFalse(fights.InFight, "the fight ends");
            Assert.IsTrue(wardenFought, "the Warden on post nearby fights the one who struck");
            Assert.IsFalse(fights.Assaulting);
            Assert.IsFalse(fights.Dead, "the strong character stands");

            // WA4: the Assault and its harm cost Reputation, and each one killed who was not an enemy 10 more.
            var standing = world.Standing;
            var walkerFell = Lying(walker.transform);
            var fallen = new[] { walker.transform, warden }.Count(b => b.gameObject.activeSelf && Lying(b));
            Assert.That(fights.LastHarm, Is.GreaterThan(0d), "the people struck lost HP");
            Assert.That(standing.ReputationPoints, Is.LessThanOrEqualTo(-4 - (10 * fallen)), $"Reputation {standing.ReputationPoints}, {fallen} fallen");
            Assert.That(world.State.Get(SocialStanding.KillsKey), Is.GreaterThanOrEqualTo(fallen), "every kill counts toward Danger");
            if (fallen > 0)
            {
                Assert.IsTrue(standing.Accused, "a kill brings Reputation to an Accusation");
                Assert.That(fights.FallenLying, Is.GreaterThanOrEqualTo(fallen), "the fallen lie where they fell");
            }

            // The fallen lie where they fell; a free Warden is sent to take them up, and after a day they are gone.
            yield return null;
            yield return null;
            if (fights.FallenLying > 0)
            {
                Assert.AreEqual(1, Object.FindObjectsByType<WardenErrand>(FindObjectsSortMode.None).Length, "a free Warden goes to the fallen");
            }

            world.Clock.AdvanceTicks(SliceConfront.BodyTicks);
            yield return null;
            yield return null;
            Assert.AreEqual(0, fights.FallenLying, "after a day no body lies");
            if (walkerFell)
            {
                Assert.IsFalse(walker.gameObject.activeSelf, "the one struck and killed is gone");
            }
        }

        private static bool Lying(Transform body)
        {
            var animator = body.GetComponentInChildren<Animator>(true);
            return animator != null && animator.parameters.Any(p => p.name == "Down") && animator.GetBool("Down");
        }

        private static Vector3 Ground(Vector3 at) =>
            Physics.Raycast(at + (Vector3.up * 3f), Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore) ? hit.point : at;
    }
}
#endif

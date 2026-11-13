#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
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
    /// one struck stops its walk and fights back, and the Wardens who hear its call come (§5e, PB8a): one on post a few
    /// metres away joins at the end of the first round, or, if the fight is over first, fights the character where it
    /// stands. Far from every post nobody hears it, and only a runner fetches the Wardens of the post it reaches.
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
            character.Set(CharacterSkill.LightMelee, 20);
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
            Assert.That(fights.WardensComing, Is.GreaterThanOrEqualTo(1), "the Warden on post nearby comes");
            Assert.IsTrue(fights.ComingBodies.All(b => SliceAlarm.Hears(place, b.position)), "only the Wardens who hear the call come (10.2 §5e)");
            Assert.IsTrue(fights.Turns.Order.Any(c => c.Id == 1), "the one struck is in the fight");

            // PB6, 10.2 §5a: a resident fights back only with a chance, runs for help otherwise, and calls; those near hear.
            var struck = fights.Turns.Order.First(c => c.Id == 1);
            Assert.AreEqual(SliceConfront.FightsBackAt, struck.FightsAt, "the one struck fights only with a chance");
            Assert.IsTrue(struck.HelpAt.HasValue, "and knows where the nearest post is");
            Assert.IsFalse(fights.ComingBodies.Any(b => (b.position - struck.HelpAt.Value).sqrMagnitude < 0.01f), "a post that did not answer");
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

        [UnityTest]
        public IEnumerator FarFromThePostsOnlyARunnerFetchesTheWardens()
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

            // A walker no Warden hears, more than thirty cells from every one; the character beside it, facing it.
            var walker = Object.FindObjectsByType<TownsfolkWalker>(FindObjectsSortMode.InstanceID).FirstOrDefault(w =>
                w.isActiveAndEnabled && !w.Inside && PeopleAround.Wardens(w.transform.position).All(k => !SliceAlarm.Hears(w.transform.position, k.position)));
            Assert.IsNotNull(walker, "a walker out of every Warden's hearing");
            walker.enabled = false;
            var place = walker.transform.position;
            var way = Vector3.ProjectOnPlane(walker.transform.forward, Vector3.up).normalized;
            player.Teleport(Ground(place + (way * 1.2f)), Quaternion.LookRotation(-way));
            for (var t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                yield return null;
            }

            var piece = new ItemPiece("sword");
            Assert.IsTrue(world.Bag.AddPiece(piece));
            Assert.IsNull(world.Outfit.Wear(piece));
            fights.TurnPause = 0f;
            Assert.IsTrue(world.Arms.Draw(), "R draws");
            for (var t = 0f; t < PlayerArms.DrawSeconds + 0.05f; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsTrue(world.Arms.Strike(), "a click strikes " + Describe(player));
            Assert.IsTrue(fights.Assaulting, "an Assault");
            Assert.AreEqual(0, fights.WardensComing, "nobody hears the call, and no Warden beyond it comes (10.2 §5e)");

            // The posts, nearest first: the Wardens within a few metres of each other stand at one.
            var posts = new List<List<Transform>>();
            foreach (var k in PeopleAround.Wardens(place))
            {
                var post = posts.Find(p => (p[0].position - k.position).sqrMagnitude <= SliceConfront.PostReach * SliceConfront.PostReach);
                if (post == null)
                {
                    posts.Add(new List<Transform> { k });
                }
                else
                {
                    post.Add(k);
                }
            }

            Assert.GreaterOrEqual(posts.Count, 3, "Veilmouth has three posts or more");
            var struck = fights.Turns.Order.First(c => c.Id == 1);
            Assert.IsTrue(struck.HelpAt.HasValue && (struck.HelpAt.Value - posts[0][0].position).sqrMagnitude < 0.01f, "the one struck would run to the nearest post");

            // Three runners: one reaches the nearest post, one an empty post, and one is stopped on its way.
            var postAt = posts.ConvertAll(p => p[0].position);
            posts[1].ForEach(k => k.gameObject.SetActive(false));
            var runner = new GameObject("runner").transform;
            var toEmpty = new GameObject("runner to an empty post").transform;
            var stopped = new GameObject("runner stopped").transform;
            foreach (var body in new[] { runner, toEmpty, stopped })
            {
                body.position = place;
            }

            fights.SendRunner(runner, postAt[0]);
            fights.SendRunner(toEmpty, postAt[1]);
            fights.SendRunner(stopped, postAt[2]);
            Assert.AreEqual(3, fights.Runners);
            stopped.gameObject.SetActive(false); // by a spell or by death
            using (world.Clock.Pause())
            {
                for (var tick = 0; tick < 500 && fights.Runners > 0; tick++)
                {
                    world.Clock.AdvanceTicks(1);
                }
            }

            Assert.AreEqual(0, fights.Runners, "every runner is there, or fell");
            var coming = fights.ComingBodies;
            Assert.IsTrue(posts[0].All(coming.Contains), "the runner sets out the Wardens of the post it reached");
            Assert.IsFalse(posts[2].Any(coming.Contains), "a runner stopped on its way brings nobody");
            Assert.AreEqual(posts[0].Count, coming.Count, "and an empty post sends nobody");
            var heard = posts[0][0].GetComponentInChildren<StealthObserver>();
            Assert.IsNotNull(heard);
            Assert.AreEqual(Loc.Get("ui.fight.help.warden"), heard.Line, "the Warden set out says so");

            fights.Turns.Stop();
            for (var t = 0f; t < 60f && fights.InFight; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsFalse(fights.InFight, "the fight ends");
            posts[1].ForEach(k => k.gameObject.SetActive(true));
        }

        [UnityTest]
        public IEnumerator InAFightWithTheCultTheWardensWhoHearJoinTheCharacter()
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

            // The character six metres before a Warden on post, and two cultists with pistols four cells off.
            var warden = PeopleAround.Wardens(player.transform.position).FirstOrDefault();
            Assert.IsNotNull(warden, "Veilmouth has Wardens on post");
            var way = Vector3.ProjectOnPlane(warden.forward, Vector3.up).normalized;
            player.Teleport(Ground(warden.position + (way * 6f)), Quaternion.LookRotation(way));
            for (var t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                yield return null;
            }

            fights.TurnPause = 0f;
            fights.BeatPace = 6f;
            fights.AutoPlayer = true;
            Assert.IsTrue(fights.StartDevelopmentFight(SliceConfront.Cult), "a fight with two cultists");
            var me = fights.PlayerCombatant;

            // VS-ALARM (i): a shot is heard, and the Warden runs to the character's side.
            SliceAlarm.Raise(new Alarm(AlarmKind.Shot, player.transform.position, null));
            Assert.IsTrue(fights.ComingBodies.Contains(warden), "the Warden six metres off hears the shot and comes");
            Assert.IsTrue(warden.TryGetComponent<SliceFighter>(out var part) && part.Combatant != null, "as a fighter");
            var ally = part.Combatant;
            Assert.AreEqual(me.Side, ally.Side, "on the character's side");
            Assert.IsFalse(fights.Innocent(ally), "its harm is no harm the character did");

            var joined = false;
            for (var t = 0f; t < 120f && fights.InFight; t += Time.deltaTime)
            {
                joined |= ally.State == CombatantState.Fighting && fights.Turns.Order.Contains(ally);
                if (joined)
                {
                    break;
                }

                yield return null;
            }

            Assert.IsTrue(joined || !fights.InFight, "it joins at the end of its round, unless the fight is over first");
            if (fights.Turns.IsRunning)
            {
                fights.Turns.Stop();
            }

            for (var t = 0f; t < 60f && fights.InFight; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsFalse(fights.InFight, "the fight ends");
            yield return null;
            Assert.IsFalse(warden.TryGetComponent<SliceFighter>(out var after) && after.Combatant != null, "the Warden fights no more");
            for (var t = 0f; t < 3f; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsFalse(fights.InFight, "a Warden come to the character's help never turns on it");
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

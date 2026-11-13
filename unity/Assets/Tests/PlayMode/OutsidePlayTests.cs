#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Cameras;
using GameDev.Confront.Rules;
using GameDev.Core.Localization;
using GameDev.Dialogue;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.Fights;
using GameDev.Slice.People;
using GameDev.Slice.Stealth;
using GameDev.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5b.8 (05.1 *Вне боя*): in Veilmouth each ability of the slice's Limits works in free exploration at its fight's
    /// price, by its number key, its Will Power shown on the HUD: Внушение, Пелена, Дрёма, Чужой сон and Двойник of Жрица
    /// with Чуткое ухо's mark on a lie; Стой! of Император with Родство's sense both ways; and full exhaustion.
    /// </summary>
    public sealed class OutsidePlayTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";

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
            var world = SliceWorldDriver.Running;
            if (world != null && world.Fights != null)
            {
                world.Fights.ContestChance = null;
            }
        }

        [UnityTest]
        public IEnumerator PriestessesAbilitiesWorkInExploration()
        {
            yield return Arrive();
            var world = SliceWorldDriver.Running;
            var fights = world.Fights;
            var hud = Object.FindAnyObjectByType<HudView>();
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            fights.Initiate(Arcana.Priestess, 8);
            var keys = fights.OutsideAbilities().Select(a => a.Key).ToList();
            CollectionAssert.AreEquivalent(new[] { "suggestion", "screen", "drowse", "dream_touch", "double" }, keys);

            // Внушение by its number key: +2 for the next Persuasion check, at its price, the bar shown.
            yield return Tap(Key.Digit1 + keys.IndexOf("suggestion"));
            Assert.AreEqual(SliceConfront.SuggestionBonus, fights.SuggestionFor(CharacterSkill.Persuasion), "the key used it");
            Assert.AreEqual(0, fights.SuggestionFor(CharacterSkill.Intimidation));
            Assert.AreEqual(1f - Ability(fights, "suggestion").Cost, fights.WillPowerShare, 0.0015f, "the price a fight asks");
            yield return null;
            Assert.IsTrue(hud.IsWillPowerShown, "Will Power's bar out of a fight");
            Assert.AreEqual("ui.ability.fault.on", fights.CastOutside(Ability(fights, "suggestion")));
            var attitude = world.Standing.Attitude("nell");
            fights.Persuaded(CharacterSkill.Persuasion, "nell", false);
            Assert.AreEqual(attitude - 1, world.Standing.Attitude("nell"), "a failed check: the attempt noticed, the opinion lower");
            Assert.AreEqual(0, fights.SuggestionFor(CharacterSkill.Persuasion), "spent on the check");

            // Чуткое ухо marks a line that lies, never one that does not, and only for one who knows it.
            var sweep = AssetDatabase.LoadAssetAtPath<DialogueDefinition>("Assets/Data/Slice/Dialogue/SweepDialogue.asset");
            StringAssert.EndsWith(Loc.Get("ui.ability.keen_ear.lie"), PersonTalk.Heard(sweep, sweep.Node("shrug")));
            Assert.AreEqual(sweep.Line(sweep.Node("yard")), PersonTalk.Heard(sweep, sweep.Node("yard")));

            // Пелена: a cloud within 8 m that puts out a look through it, gone after 2 ticks.
            Assert.IsNull(fights.CastOutside(Ability(fights, "screen")));
            Assert.AreEqual(1, fights.OutsideClouds);
            var haze = GameObject.Find("Haze");
            Assert.IsNotNull(haze);
            var middle = haze.transform.position;
            Assert.LessOrEqual(Vector3.ProjectOnPlane(middle - player.transform.position, Vector3.up).magnitude, 8.01f, "within 8 cells");
            Assert.IsTrue(world.Hiding.Screened(middle + (Vector3.left * 5f), middle + (Vector3.right * 5f)), "no look through it");
            Assert.IsFalse(world.Hiding.Screened(middle + new Vector3(-5f, 10f, 0f), middle + new Vector3(5f, 10f, 0f)));
            world.Clock.AdvanceTicks(2);
            yield return null;
            Assert.AreEqual(0, fights.OutsideClouds, "it lasts 2 ticks");

            // Дрёма on a Warden in front: asleep, its eyes closed, for 10 ticks.
            var warden = Before(world, player, 4f);
            yield return LookAt(warden);
            fights.ContestChance = 1d;
            world.State.Set(SliceConfront.WillPowerLostKey, 0);
            Assert.IsNull(fights.CastOutside(Ability(fights, "drowse")));
            Assert.IsTrue(HeldStill.Sleeping(warden), "asleep");
            Assert.AreEqual(world.Clock.Tick + SliceConfront.SleepTicks, HeldStill.Of(warden).Until);
            var eyes = warden.GetComponentInChildren<StealthObserver>(true);
            if (eyes != null)
            {
                CollectionAssert.DoesNotContain(StealthObserver.All.ToList(), eyes, "a sleeper notices nothing");
            }

            StringAssert.StartsWith(Loc.Get("ui.ability.drowse.slept"), hud.MessageText, "the outcome first, a level reached under it");

            // Чужой сон: a touch reads the sleeper; a failed one wakes it and costs Sanity.
            player.Teleport(Ground(warden.position + (Flat(warden.forward) * 1.4f)), Quaternion.LookRotation(-Flat(warden.forward)));
            yield return null;
            Assert.IsNull(fights.CastOutside(Ability(fights, "dream_touch")));
            StringAssert.StartsWith(Loc.Format("ui.ability.dream.line", Loc.Get("tip.dream.warden.line")), hud.MessageText, "a Warden's dream (SLICE_PEOPLE §5)");
            Assert.IsTrue(world.Tips.HasHeard(world.Tips.Find("dream.warden")), "into the Character Notes");
            Assert.IsTrue(HeldStill.Sleeping(warden), "a dream read, it sleeps on");
            fights.ContestChance = 0d;
            var sanity = world.Mind.Sanity.Value;
            Assert.IsNull(fights.CastOutside(Ability(fights, "dream_touch")));
            Assert.IsFalse(HeldStill.Sleeping(warden), "a failed touch wakes it");
            Assert.AreEqual(sanity - SliceConfront.DreamSanity, world.Mind.Sanity.Value, "and costs a little Sanity");
            Assert.AreEqual("ui.ability.fault.sleeper", fights.CastOutside(Ability(fights, "dream_touch")), "nobody asleep now");

            // Двойник: unseen for a tick, its copy standing.
            Assert.IsNull(fights.CastOutside(Ability(fights, "double")));
            Assert.IsTrue(fights.Doubled && world.Hiding.Unseen);
            var watcher = StealthObserver.All.FirstOrDefault(o => !o.Managed);
            if (watcher != null)
            {
                Assert.IsFalse(world.Hiding.Sees(watcher, player.transform.position, false), "no cone sees the character");
            }

            world.Clock.AdvanceTicks(1);
            yield return null;
            Assert.IsFalse(fights.Doubled || world.Hiding.Unseen, "seen again after a tick");

            // Spending the last Will Power is full exhaustion, and nothing is left to spend.
            world.State.Set(SliceConfront.WillPowerLostKey, 1000 - Mathf.RoundToInt(Ability(fights, "screen").Cost * 1000f));
            Assert.IsNull(fights.CastOutside(Ability(fights, "screen")));
            StringAssert.Contains(Loc.Get("ui.ability.exhausted"), hud.MessageText);
            Assert.AreEqual(0f, fights.WillPowerShare, 1e-6f);
            Assert.AreEqual("ui.ability.fault.wp", fights.CastOutside(Ability(fights, "suggestion")));
        }

        [UnityTest]
        public IEnumerator EmperorsAbilitiesWorkInExploration()
        {
            yield return Arrive();
            var world = SliceWorldDriver.Running;
            var fights = world.Fights;
            var hud = Object.FindAnyObjectByType<HudView>();
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            fights.Initiate(Arcana.Emperor, 8);
            CollectionAssert.AreEquivalent(new[] { "halt" }, fights.OutsideAbilities().Select(a => a.Key).ToList());
            var sweep = AssetDatabase.LoadAssetAtPath<DialogueDefinition>("Assets/Data/Slice/Dialogue/SweepDialogue.asset");
            Assert.AreEqual(sweep.Line(sweep.Node("shrug")), PersonTalk.Heard(sweep, sweep.Node("shrug")), "Чуткое ухо is Жрица's");

            // Стой! on a Warden in front: frozen for 2 ticks.
            var warden = Before(world, player, 5f);
            yield return LookAt(warden);
            fights.ContestChance = 1d;
            yield return Tap(Key.Digit1);
            Assert.IsTrue(HeldStill.Frozen(warden), "the key froze it");
            StringAssert.StartsWith(Loc.Get("ui.ability.halt.frozen"), hud.MessageText);
            world.Clock.AdvanceTicks(SliceConfront.HaltTicks);
            yield return null;
            Assert.IsFalse(HeldStill.Frozen(warden), "for 2 ticks");

            fights.ContestChance = 0d;
            Assert.IsNull(fights.CastOutside(Ability(fights, "halt")));
            Assert.IsFalse(HeldStill.Frozen(warden));
            StringAssert.StartsWith(Loc.Get("ui.ability.halt.failed"), hud.MessageText);

            // Родство: a follower within 30 m senses the character, walls and cones or none, and is marked; farther, not.
            var look = Object.FindAnyObjectByType<ThirdPersonCamera>();
            var ahead = Flat(Camera.main.transform.forward);
            var follower = new GameObject("Follower");
            follower.transform.SetPositionAndRotation(player.transform.position + (ahead * 10f), Quaternion.LookRotation(ahead));
            follower.AddComponent<EmperorFollower>();
            var kin = follower.AddComponent<StealthObserver>();
            Assert.IsTrue(world.Hiding.Sees(kin, player.transform.position, false), "it senses the character, its back turned");
            yield return null;
            yield return null;
            Assert.AreEqual(Loc.Get("ui.ability.kinship.mark"), Object.FindAnyObjectByType<StealthHud>().LineOver(kin), "the character senses it");
            follower.transform.position = player.transform.position + (ahead * 40f);
            Assert.IsFalse(world.Hiding.Sees(kin, player.transform.position, false), "beyond 30 m");
            follower.transform.position = player.transform.position + (ahead * 10f);
            fights.Initiate(Arcana.Priestess, 9);
            yield return null;
            Assert.IsFalse(world.Hiding.Sees(kin, player.transform.position, false), "only a character of Император");
            Object.Destroy(follower);
            Assert.IsNotNull(look);
        }

        private static IEnumerator Arrive()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            for (var t = 0f; t < 2f && !player.IsGrounded; t += Time.deltaTime)
            {
                yield return null;
            }
        }

        private static FightAbility Ability(SliceConfront fights, string key) => fights.KnownAbilities().Single(a => a.Key == key);

        /// <summary>The character <paramref name="metres"/> before a Warden on post, facing it.</summary>
        private static Transform Before(SliceWorldDriver world, PlayerLocomotion player, float metres)
        {
            var warden = PeopleAround.Wardens(player.transform.position).FirstOrDefault(w => !HeldStill.Sleeping(w));
            Assert.IsNotNull(warden, "Veilmouth has Wardens on post");
            var way = Flat(warden.forward);
            player.Teleport(Ground(warden.position + (way * metres)), Quaternion.LookRotation(-way));
            return warden;
        }

        private static IEnumerator LookAt(Transform body)
        {
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var look = Object.FindAnyObjectByType<ThirdPersonCamera>();
            var to = Flat(body.position - player.transform.position);
            look.Look(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, look.Pitch);
            yield return null;
            yield return null;
        }

        private static Vector3 Flat(Vector3 v) => Vector3.ProjectOnPlane(v, Vector3.up).normalized;

        private static Vector3 Ground(Vector3 at) =>
            Physics.Raycast(at + (Vector3.up * 3f), Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore) ? hit.point : at;
    }
}
#endif

#if UNITY_EDITOR
using System.Collections;
using GameDev.Core.Localization;
using GameDev.Dialogue;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Slice.Alchemy;
using GameDev.Slice.Fights;
using GameDev.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5b.7 (05.1 *Conduct Progress in the slice*): in Veilmouth the deeds of the character's Arcana, done after Limit 9,
    /// are felt in one line each with no counter shown, and 8 points open Limit 8; a fight's deeds are read as it ends.
    /// </summary>
    public sealed class ConductPlayTests : SliceInputTestBase
    {
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
        public IEnumerator EightPointsOpenLimitEight()
        {
            yield return ForgetTheWorld();
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var world = SliceWorldDriver.Running;
            Assert.IsNotNull(world?.Conduct, "the world keeps Conduct Progress");
            var hud = Object.FindAnyObjectByType<HudView>();
            world.State.Set(SliceConfront.ArcanaKey, (int)Arcana.Priestess);
            world.State.Set(SliceConfront.LimitKey, 9);

            // Three purses lifted unseen: Жрица's deed, each from another person.
            foreach (var person in new[] { "walker.a", "walker.b", "walker.c" })
            {
                Assert.AreEqual(GameDev.Slice.Theft.TheftOutcome.Lifted, world.Theft.Resolve(person, ReactionResult.Good, out _));
            }

            Assert.AreEqual(3, world.Conduct.Points);
            Assert.AreEqual(Loc.Get("ui.conduct.deed.priestess"), hud.MessageText, "felt in one short line");
            world.Theft.Resolve("walker.d", ReactionResult.Good, out _);
            Assert.AreEqual(3, world.Conduct.Points, "a fourth theft counts no more");

            // A Convince check that settles a POI is worth 2, two more won are worth 1 each.
            Assert.IsTrue(world.Conduct.Checked(Approach.Convince, "wardens", settles: true));
            Assert.IsTrue(world.Conduct.Checked(Approach.Convince, "neighbours", false));
            Assert.AreEqual(6, world.Conduct.Points);
            var potion = new ItemPiece("potion_priestess_8", 8);
            world.Bag.AddPiece(potion);
            var item = world.Bag.Catalogue.Find(potion.item);
            Assert.AreEqual("ui.drink.fault.ready", world.Advancement.Fault(potion, item));

            Assert.IsTrue(world.Conduct.Deed(ConductDeed.Overhear, "overheard.poi_a"));
            Assert.IsTrue(world.Conduct.Checked(Approach.Convince, "keeper", false));
            Assert.AreEqual(Loc.Get("ui.conduct.ready"), hud.MessageText, "ready for the next potion");
            Assert.IsNull(world.Advancement.Fault(potion, item), "8 points open Limit 8");
            Assert.IsTrue(world.Advancement.Drink(potion, item));
            for (var until = Time.realtimeSinceStartup + 20f; world.Advancement.Assimilating && Time.realtimeSinceStartup < until;)
            {
                yield return null;
            }

            yield return null;
            Assert.AreEqual(8, world.Advancement.CurrentLimit);
            Assert.AreEqual(0, world.Conduct.Points, "counted afresh toward the next Limit");
            Assert.IsFalse(world.Conduct.Checked(Approach.Convince, "someone", false), "no deed counts past Limit 9 in the slice");
        }

        [UnityTest]
        public IEnumerator AFightsDeedsAreReadAsItEnds()
        {
            yield return ForgetTheWorld();
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var world = SliceWorldDriver.Running;
            var fights = world.Fights;
            world.State.Set(SliceConfront.ArcanaKey, (int)Arcana.Emperor);
            world.State.Set(SliceConfront.LimitKey, 9);
            world.State.Set(SliceAdvancement.ConductKey, 2);

            world.Theft.Resolve("walker.a", ReactionResult.Bad, out _);
            Assert.AreEqual(1, world.Conduct.Points, "a theft, even a noticed one, is against Император");

            fights.AutoPlayer = true;
            fights.BeatPace = 6f;
            Assert.IsTrue(fights.StartDevelopmentFight(SliceConfront.Cult), "a fight with two cultists");
            for (var time = 0f; fights.InFight && time < 120f; time += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsFalse(fights.InFight, "the fight ends");
            var won = !fights.Dead && !fights.BrokeAway;
            Assert.AreEqual(won && fights.LastFightEstimate <= 1d ? 1 : 0, world.Conduct.Count(ConductDeed.StrongerSide),
                $"a side at least as strong beaten counts once; estimate {fights.LastFightEstimate:0.00}, won {won}");
            if (fights.Dead)
            {
                Object.Destroy(Object.FindAnyObjectByType<SliceDeathScreen>().gameObject);
                yield return null;
            }
        }
    }
}
#endif

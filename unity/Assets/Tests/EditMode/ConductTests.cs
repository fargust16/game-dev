using System.Collections.Generic;
using GameDev.Dialogue;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Slice.Alchemy;
using GameDev.Slice.Fights;
using GameDev.Slice.Mind;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEditor;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5b.7 (05.1 *Conduct Progress in the slice*): a deed of the Arcana is worth 1, and 2 when it settles a POI; only at
    /// Limit 9; each kind three times at most, each time a different person or place; an act against the Arcana takes a
    /// point away, never below zero; each is felt in one line, and the threshold in another; 8 points open Limit 8.
    /// </summary>
    public sealed class ConductTests
    {
        private WorldState _state;
        private SliceAdvancement _advancement;
        private SliceConduct _conduct;
        private List<string> _felt;

        [SetUp]
        public void SetUp()
        {
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogue>("Assets/Data/Items/Items.asset");
            var sheet = new CharacterSheet();
            _state = new WorldState();
            _advancement = new SliceAdvancement(_state, new GameClock(1d, 9 * 60), SliceWorldDriver.StartingBag(items, sheet), sheet,
                new SliceSanity(_state), null, () => false);
            _conduct = new SliceConduct(_state, _advancement);
            _felt = new List<string>();
            _conduct.Felt += _felt.Add;
        }

        private void Enter(Arcana arcana, int limit)
        {
            _state.Set(SliceConfront.ArcanaKey, (int)arcana);
            _state.Set(SliceConfront.LimitKey, limit);
        }

        [Test]
        public void DeedsCountOnlyAtLimitNineOfTheirOwnArcana()
        {
            Assert.IsFalse(_conduct.Deed(ConductDeed.Convince, "a"), "uninitiated");
            Enter(Arcana.Emperor, 9);
            Assert.IsFalse(_conduct.Deed(ConductDeed.Convince, "a"), "Жрица's deed by one of Император");
            Assert.IsTrue(_conduct.Deed(ConductDeed.Threaten, "a"));
            Enter(Arcana.Emperor, 8);
            Assert.IsFalse(_conduct.Deed(ConductDeed.Threaten, "b"), "Limit 8 leads nowhere further in the slice");
            Assert.AreEqual(1, _conduct.Points);
            CollectionAssert.AreEqual(new[] { "ui.conduct.deed.emperor" }, _felt);
        }

        [Test]
        public void EachKindCountsThreeTimesEachWithAnotherPersonOrPlace()
        {
            Enter(Arcana.Priestess, 9);
            Assert.IsTrue(_conduct.Deed(ConductDeed.TakeUnseen, "theft.a"));
            Assert.IsFalse(_conduct.Deed(ConductDeed.TakeUnseen, "theft.a"), "the same person again");
            Assert.IsTrue(_conduct.Deed(ConductDeed.Convince, "theft.a"), "another kind may meet the same person");
            Assert.IsTrue(_conduct.Deed(ConductDeed.TakeUnseen, "theft.b"));
            Assert.IsTrue(_conduct.Deed(ConductDeed.TakeUnseen, "theft.c"));
            Assert.IsFalse(_conduct.Deed(ConductDeed.TakeUnseen, "theft.d"), "never one action repeated forever");
            Assert.AreEqual((3, 4), (_conduct.Count(ConductDeed.TakeUnseen), _conduct.Points));
        }

        [Test]
        public void ADeedThatSettlesAPoiIsWorthTwoAndAnActAgainstTakesOneNeverBelowZero()
        {
            Enter(Arcana.Priestess, 9);
            Assert.IsFalse(_conduct.Against(Arcana.Priestess), "nothing to lose");
            Assert.IsTrue(_conduct.Checked(Approach.Convince, "warden", settles: true));
            Assert.AreEqual(2, _conduct.Points);
            Assert.IsFalse(_conduct.Checked(Approach.Threaten, "warden", false), "a threat is no deed of Жрица");
            Assert.IsFalse(_conduct.Against(Arcana.Emperor), "nor against it");
            Assert.IsTrue(_conduct.Against(Arcana.Priestess));
            Assert.AreEqual(1, _conduct.Points);
            CollectionAssert.AreEqual(new[] { "ui.conduct.deed.priestess", "ui.conduct.against.priestess" }, _felt);
        }

        [Test]
        public void EightPointsOpenLimitEightAndAreFeltOnce()
        {
            Enter(Arcana.Emperor, 9);
            var potion = new ItemPiece("potion_emperor_8", 8);
            var item = AssetDatabase.LoadAssetAtPath<ItemCatalogue>("Assets/Data/Items/Items.asset").Find(potion.item);
            Assert.AreEqual("ui.drink.fault.ready", _advancement.Fault(potion, item));

            // POIs A, B and C settled in the Arcana's way give 6, two small deeds the rest (05.1 *The run's budget*).
            Assert.IsTrue(_conduct.Checked(Approach.Threaten, "poi.a", true));
            Assert.IsTrue(_conduct.Deed(ConductDeed.StrongerSide, "fight.1.1", true));
            Assert.IsTrue(_conduct.Deed(ConductDeed.Stand, "fight.2.2", true));
            Assert.IsTrue(_conduct.Deed(ConductDeed.StrongerFoe, "beast.10.20"));
            Assert.AreEqual(7, _conduct.Points);
            Assert.AreEqual("ui.drink.fault.ready", _advancement.Fault(potion, item));
            Assert.IsTrue(_conduct.Deed(ConductDeed.Threaten, "talk.someone"));
            Assert.AreEqual("ui.conduct.ready", _felt[_felt.Count - 1], "the threshold is felt");
            Assert.IsNull(_advancement.Fault(potion, item), "8 points open Limit 8");
            Assert.IsTrue(_conduct.Deed(ConductDeed.StrongerFoe, "person.x"));
            Assert.AreEqual("ui.conduct.deed.emperor", _felt[_felt.Count - 1], "only once");
        }
    }
}

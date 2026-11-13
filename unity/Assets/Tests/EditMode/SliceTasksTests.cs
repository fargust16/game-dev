using System.Linq;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Slice.Tasks;
using GameDev.World.State;
using NUnit.Framework;
using UnityEditor;

namespace GameDev.Slice.Tests
{
    /// <summary>S5c.7: the paid tasks of SLICE_TRIAL §3, from the Notice Board, the Masters and the faction.</summary>
    public sealed class SliceTasksTests
    {
        private const int Day = 1440;

        private WorldState _state;
        private Bag _bag;
        private long _tick;
        private SliceTasks _tasks;

        [SetUp]
        public void SetUp()
        {
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogue>("Assets/Data/Items/Items.asset");
            _state = new WorldState();
            _bag = SliceWorldDriver.StartingBag(items, new CharacterSheet());
            _tick = 9 * 60;
            _tasks = new SliceTasks(_state, _bag, () => _tick);
        }

        [TearDown]
        public void TearDown() => _tasks.Detach();

        [Test]
        public void ATaskOfTheSecondDayIsOfferedFromItsMorningAndEddasOreOnlyOnTheFirst()
        {
            Assert.IsTrue(_tasks.Offered(SliceTasks.Find("firewood"), _tick));
            Assert.IsFalse(_tasks.Offered(SliceTasks.Find("letter"), _tick), "the letter is a notice of the second day");
            Assert.IsFalse(_tasks.Offered(SliceTasks.Find("letter"), Day + (7 * 60)), "not before its morning");
            Assert.IsTrue(_tasks.Offered(SliceTasks.Find("letter"), Day + (8 * 60)));

            Assert.IsTrue(_tasks.Offered(SliceTasks.Find("edda.ore"), _tick));
            Assert.IsFalse(_tasks.Offered(SliceTasks.Find("edda.ore"), Day + (9 * 60)), "Edda wants the ore on the first day only");
            Assert.IsTrue(_tasks.Offered(SliceTasks.Find("edda.jerkin"), Day + (9 * 60)));
            CollectionAssert.AreEquivalent(new[] { "firewood", "locket" }, _tasks.OfferedBy(SliceTasks.Board, _tick).Select(t => t.Id));
        }

        [Test]
        public void HandingInTakesWhatItWantsPaysItAndEndsIt()
        {
            Assert.IsTrue(_tasks.Take("ada.herbs", _tick));
            Assert.IsFalse(_tasks.Take("ada.herbs", _tick), "taken once");
            Assert.AreEqual(-1, _tasks.HandIn("ada.herbs", _tick), "it wants six herbs");

            _bag.Add("herb", 7);
            _tasks.Keep();
            Assert.AreEqual(1, _state.Get(SliceTasks.Key("ada.herbs", "ready")), "the conversation key follows the bag");
            var bronze = _bag.Bronze;
            Assert.AreEqual(1200, _tasks.HandIn("ada.herbs", _tick));
            Assert.AreEqual(1, _bag.Count("herb"));
            Assert.AreEqual(bronze + 1200, _bag.Bronze);
            Assert.IsTrue(_tasks.PaidOut("ada.herbs"));
            Assert.IsFalse(_tasks.Held("ada.herbs"));
            Assert.IsFalse(_tasks.Offered(SliceTasks.Find("ada.herbs"), _tick + Day), "an order is done once");
        }

        [Test]
        public void TheJerkinMustBeOfTheCharactersOwnMaking()
        {
            _tick = Day + (9 * 60);
            Assert.IsTrue(_tasks.Take("edda.jerkin", _tick));
            _bag.AddPiece(new ItemPiece("leather_body"));
            Assert.IsFalse(_tasks.Ready(SliceTasks.Find("edda.jerkin")), "a bought jerkin is not one's own making");

            _bag.AddPiece(new ItemPiece("leather_body") { made = true });
            Assert.AreEqual(2000, _tasks.HandIn("edda.jerkin", _tick));
            Assert.AreEqual(1, _bag.Pieces.Count(p => p.item == "leather_body"), "the bought one stays");
            Assert.IsFalse(_bag.Pieces.Any(p => p.item == "leather_body" && p.made));
        }

        [Test]
        public void TheCrateIsGivenOnTakingAndPaysNothingWhenLate()
        {
            Assert.IsTrue(_tasks.Take("tobias.crate", _tick));
            Assert.AreEqual(1, _bag.Count("crate"), "Tobias hands the crate over");
            Assert.AreEqual("tobias.crate", _tasks.HandInAt("warehouse")?.Id);
            Assert.IsFalse(_tasks.Late("tobias.crate", _tick + 29));
            Assert.IsTrue(_tasks.Late("tobias.crate", _tick + 30));

            var bronze = _bag.Bronze;
            Assert.AreEqual(0, _tasks.HandIn("tobias.crate", _tick + 30));
            Assert.AreEqual(bronze, _bag.Bronze, "too late: nobody pays");
            Assert.AreEqual(0, _bag.Count("crate"));
            Assert.IsTrue(_tasks.Failed("tobias.crate"));
            Assert.IsNull(_tasks.HandInAt("warehouse"));
        }

        [Test]
        public void TheLocketIsNoticedThenPickedUpAndReturned()
        {
            Assert.IsTrue(_tasks.Take("locket", _tick));
            Assert.AreEqual(0, _bag.Count("locket"), "it lies by the angel");
            Assert.AreEqual("locket", _tasks.HiddenAt("angel")?.Id);
            Assert.IsNull(_tasks.PickUpAt("angel"), "not seen yet");

            _tasks.Notice("locket");
            Assert.IsNull(_tasks.HiddenAt("angel"));
            Assert.AreEqual("locket", _tasks.PickUpAt("angel")?.Id);
            Assert.IsTrue(_tasks.PickUp("locket"));
            Assert.IsNull(_tasks.PickUpAt("angel"));
            Assert.AreEqual(800, _tasks.HandIn(_tasks.HandInAt("widow").Id, _tick));
        }

        [Test]
        public void TheFactionsTasksAreForMembersOneAtATimeInOrder()
        {
            var censer = SliceTasks.Find("censer");
            Assert.IsFalse(_tasks.Offered(censer, _tick), "for members only");

            _state.Raise(SliceTasks.MemberKey);
            Assert.IsTrue(_tasks.Offered(censer, _tick));
            Assert.IsFalse(_tasks.Offered(SliceTasks.Find("chalk"), _tick), "in the table's order");
            Assert.IsTrue(_tasks.Take("censer", _tick));
            Assert.IsFalse(_tasks.Offered(SliceTasks.Find("chalk"), _tick), "one at a time");

            Assert.IsFalse(_tasks.CenserIn("dwelling.1.1"));
            Assert.IsTrue(_tasks.CenserIn(SliceTasks.CenserDwelling));
            Assert.IsTrue(_tasks.TakeCenser(SliceTasks.CenserDwelling));
            Assert.IsFalse(_tasks.CenserIn(SliceTasks.CenserDwelling), "taken once");

            _tasks.Keep();
            Assert.AreEqual(1, _state.Get(SliceTasks.Key("censer", "ready")));
            _state.Raise(SliceTasks.Key("censer", "handed")); // Ashgrave's conversation
            Assert.IsTrue(_tasks.PaidOut("censer"));
            Assert.AreEqual(0, _bag.Count(SliceTasks.CenserItem));
            Assert.IsTrue(_tasks.Offered(SliceTasks.Find("chalk"), _tick));
            Assert.IsFalse(_tasks.Offered(SliceTasks.Find("dace"), Day + (9 * 60)), "Dace waits for the chalk");
        }

        [Test]
        public void ChalkCountsEachDoorAtNightUnseenAndFeelsUnnoticedOnce()
        {
            _state.Raise(SliceTasks.MemberKey);
            _state.Raise(SliceTasks.Key("censer", "taken"));
            _state.Raise(SliceTasks.Key("censer", "paid"));
            Assert.IsTrue(_tasks.Take("chalk", _tick));
            var felt = 0;
            _tasks.Unnoticed = _ => felt++;

            var night = 23 * 60L;
            Assert.IsFalse(_tasks.PassDoor(1, _tick, false), "by day it does not count");
            Assert.IsFalse(_tasks.PassDoor(1, night, true), "seen, it does not count");
            Assert.IsTrue(_tasks.PassDoor(1, night, false));
            Assert.IsFalse(_tasks.PassDoor(1, night + 5, false), "each door once");
            Assert.IsTrue(_tasks.PassDoor(2, Day + (3 * 60), false), "before six in the morning");
            Assert.IsFalse(_tasks.Ready(SliceTasks.Find("chalk")));
            Assert.IsTrue(_tasks.PassDoor(3, Day + (5 * 60), false));
            Assert.IsTrue(_tasks.Ready(SliceTasks.Find("chalk")));
            Assert.AreEqual(1, felt);
            Assert.AreEqual(2000, _tasks.HandIn("chalk", Day + (9 * 60)));
        }

        [Test]
        public void DacesWordMakesHisTaskReady()
        {
            _state.Raise(SliceTasks.MemberKey);
            foreach (var done in new[] { "censer", "chalk" })
            {
                _state.Raise(SliceTasks.Key(done, "taken"));
                _state.Raise(SliceTasks.Key(done, "paid"));
            }

            _tick = Day + (9 * 60);
            Assert.IsTrue(_tasks.Take("dace", _tick));
            Assert.IsFalse(_tasks.Ready(SliceTasks.Find("dace")));
            _state.Raise(SliceTasks.DaceWordKey);
            Assert.AreEqual(1, _state.Get(SliceTasks.Key("dace", "ready")), "the word keeps the conversation key");
            Assert.AreEqual(2500, _tasks.HandIn("dace", _tick));
        }
    }
}

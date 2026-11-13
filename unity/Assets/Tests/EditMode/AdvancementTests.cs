using System.Linq;
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
    /// S5b.5 (20 §3 *Drinking a Limit potion*; 05.1 *What a Limit grants*, *Requirements and Sanity in the slice*): the order a
    /// potion can be drunk in, its Sanity cost, its 60 ticks of Assimilation, and a character reaching Limit 9 and then 8.
    /// </summary>
    public sealed class AdvancementTests
    {
        private ItemCatalogue _items;
        private CharacterSheet _sheet;
        private Bag _bag;
        private WorldState _state;
        private GameClock _clock;
        private SliceSanity _sanity;
        private SliceAdvancement _advancement;
        private bool _fighting;

        [SetUp]
        public void SetUp()
        {
            _items = AssetDatabase.LoadAssetAtPath<ItemCatalogue>("Assets/Data/Items/Items.asset");
            _sheet = new CharacterSheet();
            _bag = SliceWorldDriver.StartingBag(_items, _sheet);
            _state = new WorldState();
            _clock = new GameClock(1d, 9 * 60);
            _sanity = new SliceSanity(_state);
            _fighting = false;
            _advancement = new SliceAdvancement(_state, _clock, _bag, _sheet, _sanity, null, () => _fighting);
        }

        private ItemPiece Potion(string id, int perfect = 0, int bad = 0)
        {
            var piece = new ItemPiece(id, _items.Find(id).stage) { perfect = perfect, bad = bad };
            Assert.IsTrue(_bag.AddPiece(piece), id);
            return piece;
        }

        private string Fault(ItemPiece piece) => _advancement.Fault(piece, _items.Find(piece.item));

        private bool Drink(ItemPiece piece) => _advancement.Drink(piece, _items.Find(piece.item));

        /// <summary>Drinks and lets the 60 ticks pass, settling on each tick as the driver does.</summary>
        private void DrinkAndSettle(ItemPiece piece)
        {
            Assert.IsTrue(Drink(piece), piece.item);
            for (var i = 0; i < SliceAdvancement.AssimilationTicks; i++)
            {
                _clock.AdvanceTicks(1);
                _advancement.Settle();
            }
        }

        [Test]
        public void APotionIsDrunkOnlyInItsOrder()
        {
            var emperor8 = Potion("potion_emperor_8");
            var priestess9 = Potion("potion_priestess_9");
            Assert.AreEqual("ui.drink.fault.potion", _advancement.Fault(priestess9, _items.Find("herb")));
            Assert.AreEqual("ui.drink.fault.order", Fault(emperor8), "an Uninitiated character starts at Limit 9 (05 *No skipping*)");
            _fighting = true;
            Assert.AreEqual("ui.drink.fault.fight", Fault(priestess9), "never in a fight");
            _fighting = false;
            Assert.IsNull(Fault(priestess9), "either Arcana's Limit 9 potion enters it");

            Assert.IsTrue(Drink(priestess9));
            Assert.AreEqual(0, _bag.Pieces.Count(p => p.item == "potion_priestess_9"), "the potion leaves the bag");
            var second = Potion("potion_priestess_9");
            Assert.AreEqual("ui.drink.fault.settling", Fault(second));
            Assert.IsFalse(Drink(second));

            _clock.AdvanceTicks(SliceAdvancement.AssimilationTicks);
            Assert.IsTrue(_advancement.Settle());
            Assert.AreEqual("ui.drink.fault.arcana", Fault(emperor8), "a character keeps its Arcana");
            Assert.AreEqual("ui.drink.fault.order", Fault(second), "a Limit is entered once");
            var priestess8 = Potion("potion_priestess_8");
            Assert.AreEqual("ui.drink.fault.ready", Fault(priestess8), "Limit 8 needs its Conduct Progress");
            _state.Set(SliceAdvancement.ConductKey, SliceAdvancement.ConductForLimit8 - 1);
            Assert.AreEqual("ui.drink.fault.ready", Fault(priestess8));
            _state.Set(SliceAdvancement.ConductKey, SliceAdvancement.ConductForLimit8);
            Assert.IsNull(Fault(priestess8));
        }

        [Test]
        public void TheSanityCostReadsTheBrewingAndThePreparation()
        {
            // Every attribute at 1: Limit 9 asks 3 of each primary and 2 of the secondary: 2 × (2 + 2) + 1 × 1.
            Assert.AreEqual(9, _advancement.PreparationPenalty(Arcana.Priestess, 9));
            Assert.AreEqual(19, _advancement.SanityCost(Arcana.Priestess, 9, 0, 0), "10 × 1 + 9");
            Assert.AreEqual(13, _advancement.SanityCost(Arcana.Priestess, 9, 6, 0), "six perfect results: 10 × 0.4 + 9");
            Assert.AreEqual(24, _advancement.SanityCost(Arcana.Priestess, 9, 0, 2), "two bad ones: 10 × 1.5 + 9");

            _sheet.Set(CharacterAttribute.Will, 3);
            _sheet.Set(CharacterAttribute.Strength, 3);
            _sheet.Set(CharacterAttribute.Endurance, 2);
            Assert.AreEqual(0, _advancement.PreparationPenalty(Arcana.Emperor, 9), "a prepared character pays only the mandatory cost");
            Assert.AreEqual(2 * (4 + 4) + 2, _advancement.PreparationPenalty(Arcana.Emperor, 8), "Limit 8 asks 7 and 4");
            Assert.AreEqual(3, SliceAdvancement.PrimaryRiseOf(9));
            Assert.AreEqual(4, SliceAdvancement.PrimaryRiseOf(8));
            Assert.AreEqual(1, SliceAdvancement.SecondaryRiseOf(9));
            Assert.AreEqual(2, SliceAdvancement.SecondaryRiseOf(8));
        }

        [Test]
        public void ACharacterReachesLimitNineAndThenEight()
        {
            int? reached = null;
            var paid = 0;
            _advancement.Advanced += (arcana, limit, cost) =>
            {
                Assert.AreEqual(Arcana.Priestess, arcana);
                reached = limit;
                paid = cost;
            };
            _state.Set(SliceConfront.PreparedKey, 5);
            _state.Set(SliceConfront.WillPowerLostKey, 7);

            Assert.IsNull(_advancement.CurrentLimit, "Uninitiated");
            Assert.IsTrue(Drink(Potion("potion_priestess_9", perfect: 6)));
            Assert.IsTrue(_advancement.Assimilating);
            Assert.AreEqual(60, _advancement.AssimilationLeft);
            _clock.AdvanceTicks(SliceAdvancement.AssimilationTicks - 1);
            Assert.IsFalse(_advancement.Settle(), "nothing takes effect before the 60 ticks are spent");
            Assert.AreEqual(1, _advancement.AssimilationLeft);
            Assert.IsNull(_advancement.CurrentLimit);
            Assert.AreEqual(1, _sheet.Get(CharacterAttribute.Perception));
            _clock.AdvanceTicks(1);
            Assert.IsTrue(_advancement.Settle());
            Assert.IsFalse(_advancement.Assimilating);
            Assert.AreEqual(0, _advancement.AssimilationLeft);

            Assert.AreEqual((Arcana.Priestess, (int?)9), (_advancement.CurrentArcana, _advancement.CurrentLimit));
            Assert.AreEqual(9, reached);
            Assert.AreEqual(1 + 3 + 2, _sheet.Get(CharacterAttribute.Perception), "the rise of 3, and 2 for six perfect results");
            Assert.AreEqual(1 + 3 + 2, _sheet.Get(CharacterAttribute.Intelligence));
            Assert.AreEqual(1 + 1, _sheet.Get(CharacterAttribute.Will), "the secondary's rise of 1");
            Assert.AreEqual(13, paid);
            Assert.AreEqual(100 - 13, _sanity.Value);
            Assert.AreEqual(0, _state.Get(SliceConfront.PreparedKey), "a new Arcana prepares its first abilities");
            Assert.AreEqual(0, _state.Get(SliceConfront.WillPowerLostKey), "and comes into its full Will Power");

            _state.Set(SliceAdvancement.ConductKey, SliceAdvancement.ConductForLimit8);
            _state.Set(SliceConfront.PreparedKey, 3);
            DrinkAndSettle(Potion("potion_priestess_8"));
            Assert.AreEqual((Arcana.Priestess, (int?)8), (_advancement.CurrentArcana, _advancement.CurrentLimit));
            Assert.AreEqual(8, reached);
            Assert.AreEqual(6 + 4, _sheet.Get(CharacterAttribute.Perception));
            Assert.AreEqual(2 + 2, _sheet.Get(CharacterAttribute.Will));
            Assert.AreEqual(15 + (2 * (1 + 1)) + 2, paid, "15, and the shortfall against 7, 7 and 4");
            Assert.AreEqual(100 - 13 - 21, _sanity.Value);
            Assert.AreEqual(0, _advancement.Conduct, "Conduct Progress counts toward the next Limit afresh");
            Assert.AreEqual(3, _state.Get(SliceConfront.PreparedKey), "the same Arcana keeps what it prepared");
            Assert.AreEqual("ui.drink.fault.order", Fault(Potion("potion_priestess_8")), "V1's player stops at Limit 8");
        }

        [Test]
        public void BadResultsTakeAShareOfTheRise()
        {
            DrinkAndSettle(Potion("potion_emperor_9", bad: 2));
            Assert.AreEqual(1 + 2, _sheet.Get(CharacterAttribute.Will), "3 × 0.7, rounded");
            Assert.AreEqual(1 + 2, _sheet.Get(CharacterAttribute.Strength));
            Assert.AreEqual(1 + 1, _sheet.Get(CharacterAttribute.Endurance), "1 × 0.7, rounded");
            Assert.AreEqual(100 - (15 + 9), _sanity.Value);
        }

        [Test]
        public void APotionSpentInAFightSettlesAfterIt()
        {
            Assert.IsTrue(Drink(Potion("potion_priestess_9")));
            _fighting = true;
            _clock.AdvanceTicks(SliceAdvancement.AssimilationTicks);
            Assert.AreEqual(0, _advancement.AssimilationLeft, "each round counted as a tick");
            Assert.IsFalse(_advancement.Settle(), "a fighter never changes mid-fight");
            _fighting = false;
            Assert.IsTrue(_advancement.Settle());
            Assert.AreEqual(9, _advancement.CurrentLimit);
        }

        [Test]
        public void TheAssimilationIsKeptInTheWorldState()
        {
            Assert.IsTrue(Drink(Potion("potion_priestess_9", perfect: 3, bad: 1)));
            _clock.AdvanceTicks(20);
            var again = new SliceAdvancement(_state, _clock, _bag, _sheet, _sanity);
            Assert.IsTrue(again.Assimilating, "a restored world state holds the settling potion");
            Assert.AreEqual(40, again.AssimilationLeft);
            _clock.AdvanceTicks(40);
            Assert.IsTrue(again.Settle());
            Assert.AreEqual(1 + 3 + 1, _sheet.Get(CharacterAttribute.Perception), "three perfect add 1; one bad keeps 3 × 0.85");
        }
    }
}

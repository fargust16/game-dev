using System.Linq;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Slice.Alchemy;
using GameDev.Slice.Shops;
using GameDev.World.Poi;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEditor;
using Random = System.Random;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5b.4 (20 §3; SLICE_TRIAL §2, §4): the brewing check's difficulty by Limit and its floor, the three benches' hours, fees
    /// and doors, a potion brewed from its formula's ingredients with its six results, Alchemy's growth, and the 6 ticks after.
    /// </summary>
    public sealed class AlchemyTests
    {
        private const double Tight = 1e-9;

        private ItemCatalogue _items;
        private CharacterSheet _sheet;
        private Bag _bag;
        private WorldState _state;
        private GameClock _clock;
        private SliceShops _shops;
        private SliceAlchemy _alchemy;

        [SetUp]
        public void SetUp()
        {
            _items = AssetDatabase.LoadAssetAtPath<ItemCatalogue>("Assets/Data/Items/Items.asset");
            var rules = AssetDatabase.LoadAssetAtPath<ShopCatalogue>("Assets/Data/Slice/Shops.asset");
            _sheet = new CharacterSheet();
            _bag = SliceWorldDriver.StartingBag(_items, _sheet);
            _state = new WorldState();
            _clock = new GameClock(1d, 9 * 60);
            _shops = new SliceShops(rules, _items, new WorldSeed(110204), _clock, _bag, _state);
            _alchemy = new SliceAlchemy(_shops, _items, new WorldSeed(110204), _clock, _bag, _state, _sheet);
        }

        [Test]
        public void TheFormulasAreSliceTrialsAndHarrowTeachesAlchemy()
        {
            CollectionAssert.AreEqual(new[] { "potion_priestess_9", "potion_priestess_8", "potion_emperor_9", "potion_emperor_8" },
                _alchemy.Potions.Select(p => p.id));
            CollectionAssert.AreEqual(new[] { ("herb", 3), ("wolf_fang", 1), ("glow_essence_9", 1) },
                _items.Find("potion_priestess_9").recipe.Select(p => (p.item, p.count)));
            CollectionAssert.AreEqual(new[] { ("herb", 3), ("boar_tusk", 2), ("iron_ore", 1), ("glow_essence_8", 1) },
                _items.Find("potion_emperor_8").recipe.Select(p => (p.item, p.count)));
            Assert.AreEqual(Arcana.Emperor, _items.Find("potion_emperor_9").arcana);
            Assert.AreEqual(8, _items.Find("potion_priestess_8").stage, "a potion's Limit is its stage");
            Assert.AreEqual(0f, _items.Find("formula_priestess_9").weight, "a formula is a note of weight 0 (11)");
            Assert.IsTrue(_items.Find("formula_priestess_9").Stacks);
            Assert.IsFalse(_items.Find("potion_priestess_9").Stacks, "potions are carried one by one, each with its brewing");
            Assert.AreEqual("formula_emperor_8", SliceAlchemy.FormulaOf("potion_emperor_8"));
            Assert.IsEmpty(_items.Problems());
            CollectionAssert.Contains(_shops.Find("harrow").lessons, SliceAlchemy.Lesson);
            Assert.IsEmpty(_shops.Shelf("harrow"), "Harrow sells nothing; Ada keeps the herbs");
        }

        [Test]
        public void TheChecksGrowHarderByLimitAndKeepBrewingsFloor()
        {
            CollectionAssert.AreEqual(new[] { 55, 45, 36, 28, 21, 15, 10, 6, 3, 1 }, Enumerable.Range(0, 10).Select(SliceAlchemy.DifficultyOf));
            Assert.AreEqual(1d - 1d, _alchemy.Delta(9), Tight, "a new character, Intelligence 1 and Alchemy 1, at Limit 9: even odds");
            Assert.AreEqual(1d - 3d, _alchemy.Delta(8), Tight);

            var even = SliceAlchemy.BarFor(0d, new Random(1));
            Assert.AreEqual(0.20d, even.GoodWidth, Tight, "07.1 §8a's 20 % at even odds");
            Assert.AreEqual(1.5d, even.Seconds, Tight);
            Assert.AreEqual(0.05d, even.PerfectWidth, Tight, "a perfect zone a quarter of the good one");
            Assert.AreEqual(even.GoodStart + 0.075d, even.PerfectStart, Tight, "in its middle");

            var hardest = SliceAlchemy.BarFor(1d - 55d, new Random(1));
            Assert.AreEqual(SliceAlchemy.LeastWidth, hardest.GoodWidth, Tight, "a Limit 0 potion stays passable: never narrower than 12 %");
            Assert.AreEqual(SliceAlchemy.FastestSeconds, hardest.Seconds, Tight, "and never faster than 1.0 s");
            Assert.AreEqual(0.12d, hardest.GoodWidth * hardest.Seconds, Tight, "the shortest good window: 0.12 s");
            Assert.AreEqual(0.03d, hardest.PerfectWidth * hardest.Seconds, Tight, "the perfect one 0.03 s, a feat");
            Assert.That(hardest.GoodStart, Is.GreaterThanOrEqualTo(ReactionCheck.Lead));
        }

        [Test]
        public void EachBenchKeepsItsHoursFeeAndDoor()
        {
            var potion = _items.Find("potion_priestess_9");
            Assert.AreEqual("ui.brew.fault.lesson", _alchemy.BenchFault(BenchKind.Harrow));
            _state.Set(SliceShops.LessonKey(SliceAlchemy.Lesson), 1);
            Assert.IsNull(_alchemy.BenchFault(BenchKind.Harrow), "09:00: Harrow's shop is open");
            Assert.AreEqual("ui.brew.fault.formula", _alchemy.Fault(BenchKind.Harrow, potion));
            _bag.Add("formula_priestess_9");
            Assert.AreEqual("ui.brew.fault.ingredients", _alchemy.Fault(BenchKind.Harrow, potion));

            Assert.AreEqual("ui.brew.fault.member", _alchemy.BenchFault(BenchKind.Faction));
            _state.Set(SliceAlchemy.MemberKey, 1);
            Assert.IsNull(_alchemy.BenchFault(BenchKind.Faction), "07:00 to 23:00 for members");

            Assert.AreEqual("ui.brew.fault.unknown", _alchemy.BenchFault(BenchKind.Den), "an ordinary warehouse until heard of");
            _state.Set(TipBook.HeardKeyOf("kit.den"), 1);
            Assert.AreEqual("ui.brew.fault.closed", _alchemy.BenchFault(BenchKind.Den), "the den opens at 22:00");
            Assert.AreEqual(22, _alchemy.Opens(BenchKind.Den));

            _clock.AdvanceTicks(13 * 60 + 30); // 22:30
            Assert.IsNull(_alchemy.BenchFault(BenchKind.Den));
            Assert.AreEqual("ui.brew.fault.closed", _alchemy.BenchFault(BenchKind.Harrow), "Harrow's closes at 20:00");
            Assert.IsNull(_alchemy.BenchFault(BenchKind.Faction));
            _clock.AdvanceTicks(60); // 23:30
            Assert.AreEqual("ui.brew.fault.closed", _alchemy.BenchFault(BenchKind.Faction));
            _clock.AdvanceTicks(5 * 60); // 04:30
            Assert.IsNull(_alchemy.BenchFault(BenchKind.Den), "until 05:00");
            _clock.AdvanceTicks(60); // 05:30
            Assert.AreEqual("ui.brew.fault.closed", _alchemy.BenchFault(BenchKind.Den));

            _state.Set(SliceAlchemy.AgainstVeinmarketKey, 1);
            Assert.AreEqual("ui.brew.fault.barred", _alchemy.BenchFault(BenchKind.Den), "never to one who went against Veinmarket in POI B");

            Assert.AreEqual(100, SliceAlchemy.Fee(BenchKind.Harrow));
            Assert.AreEqual(0, SliceAlchemy.Fee(BenchKind.Faction));
            Assert.AreEqual(300, SliceAlchemy.Fee(BenchKind.Den));
        }

        [Test]
        public void APotionIsBrewedWithItsResultsAndAlchemyGrows()
        {
            var potion = _items.Find("potion_priestess_9");
            _state.Set(SliceShops.LessonKey(SliceAlchemy.Lesson), 1);
            _bag.Add("formula_priestess_9");
            _bag.Add("herb", 3);
            _bag.Add("wolf_fang", 1);
            _bag.Add("glow_essence_9", 1);
            var bronze = _bag.Bronze;
            Assert.IsNull(_alchemy.Fault(BenchKind.Harrow, potion));

            var results = new[]
            {
                ReactionResult.Perfect, ReactionResult.Perfect, ReactionResult.Good, ReactionResult.Good, ReactionResult.Good, ReactionResult.Bad,
            };
            Assert.AreEqual(BrewOutcome.Brewed, _alchemy.Brew(BenchKind.Harrow, potion, results, out var made));
            Assert.AreEqual(2, made.perfect);
            Assert.AreEqual(1, made.bad);
            Assert.AreEqual(9, made.stage);
            CollectionAssert.Contains(_bag.Pieces, made);
            Assert.AreEqual(bronze - 100, _bag.Bronze, "Harrow's fee");
            Assert.AreEqual(0, _bag.Count("herb"));
            Assert.AreEqual(0, _bag.Count("glow_essence_9"));
            Assert.AreEqual(1, _bag.Count("formula_priestess_9"), "the formula stays");

            // Six checks at stage 9: 2 × 20 + 3 × 10 + 5 = 75 XP, short of the 77.2 that level 7 asks (10, 11, 12.1, 13.3, 14.6, 16.1).
            Assert.AreEqual(6, _sheet.Get(CharacterSkill.Alchemy), "every check grows Alchemy at the potion's Limit");
            Assert.AreEqual(0.75d, _sheet.Xp(CharacterAttribute.Intelligence), 1e-6, "and Intelligence a hundredth");
            Assert.AreEqual(1, _state.Get(SliceAlchemy.BrewedKey));

            Assert.AreEqual(BrewOutcome.Interrupted, _alchemy.Brew(BenchKind.Harrow, potion, results, out _), "no ingredients left");
            Assert.AreEqual(BrewOutcome.Refused, _alchemy.Brew(BenchKind.Harrow, potion, results.Take(5).ToArray(), out _), "six checks or none");
        }

        [Test]
        public void TheBrewingsTicksPassAndAFightStopsThem()
        {
            var start = _clock.Tick;
            Assert.AreEqual(6, _alchemy.PassTicks(null));
            Assert.AreEqual(start + 6, _clock.Tick);

            var calls = 0;
            Assert.AreEqual(2, _alchemy.PassTicks(() => ++calls >= 2), "a fight that begins stops the time passing");
            Assert.AreEqual(start + 8, _clock.Tick);
        }

        [Test]
        public void TheBrewingFactorFollowsThePerfectAndBadResults()
        {
            Assert.AreEqual(1d, SliceAlchemy.BrewingFactor(0, 0), Tight, "six good results: the standard cost");
            Assert.AreEqual(0.40d, SliceAlchemy.BrewingFactor(6, 0), Tight, "six perfect take 40 % of it");
            Assert.AreEqual(2.50d, SliceAlchemy.BrewingFactor(0, 6), Tight, "six bad 250 %");
            Assert.AreEqual(1.45d, SliceAlchemy.BrewingFactor(3, 3), Tight);
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 1, 1, 1, 2 }, Enumerable.Range(0, 7).Select(SliceAlchemy.PerfectBonus), "1 for three perfect, 2 for six");
            Assert.AreEqual(1d, SliceAlchemy.RiseShare(0), Tight);
            Assert.AreEqual(0.85d, SliceAlchemy.RiseShare(1), Tight);
            Assert.AreEqual(0.10d, SliceAlchemy.RiseShare(6), Tight, "six bad leave a tenth of the rise");
        }

        [Test]
        public void APotionsLineShowsWhatItsBrewingGave()
        {
            var potion = _items.Find("potion_emperor_9");
            var line = BagPage.Potion(potion, new ItemPiece(potion.id, 9) { perfect = 1, bad = 2 });
            Assert.AreEqual(GameDev.Core.Localization.Loc.Format("ui.hud.bag.potion", GameDev.Core.Localization.Loc.Get(potion.NameKey), 1, 3, 2, "0.2"), line);
        }
    }
}

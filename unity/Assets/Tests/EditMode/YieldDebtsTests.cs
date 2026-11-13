using System.Linq;
using GameDev.Dialogue;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Player.Standing;
using GameDev.Slice.BreakIns;
using GameDev.Slice.EditorTools;
using GameDev.Slice.Fights;
using GameDev.Slice.Gear;
using GameDev.Slice.People;
using GameDev.World.Poi;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEditor;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5c.7: a Yield's terms paid from the bag (10.2 §9) and the Exchange in coin of POI B and C (11.1): the robbers'
    /// take, the Wardens' lock-up with its store of weapons and its fine, and a dialogue option that costs bronze.
    /// </summary>
    public sealed class YieldDebtsTests
    {
        private SliceSettings _settings;
        private CharacterSheet _sheet;
        private Bag _bag;
        private Outfit _outfit;
        private WorldState _state;
        private GameClock _clock;
        private WorldSeed _seed;
        private SocialStanding _standing;
        private SliceBreakIns _breakIns;
        private YieldDebts _debts;

        [SetUp]
        public void SetUp()
        {
            _settings = AssetDatabase.LoadAssetAtPath<SliceSettings>(SliceConfrontSetup.SettingsPath);
            _sheet = new CharacterSheet();
            _bag = new Bag(_settings.Items, _sheet);
            _outfit = new Outfit(_bag);
            _state = new WorldState();
            _clock = new GameClock(1d, 23 * 60);
            _seed = new WorldSeed(110213);
            _standing = new SocialStanding(_state, _settings.Standing, _clock, _seed, SliceSeedStreams.Lies);
            _breakIns = new SliceBreakIns(_bag, _state, _clock, _sheet, _standing, _seed);
            _debts = new YieldDebts(_state, _bag, _outfit, _breakIns);
        }

        [TearDown]
        public void TearDown() => _standing?.Dispose();

        private ItemPiece Magic(string id)
        {
            var piece = ItemMaker.MakeWith(_settings.Items.Find(id), 9, 1, new System.Random(3));
            Assert.AreEqual(ItemQuality.Magic, piece.Quality);
            return piece;
        }

        [Test]
        public void RobbersTakeEveryCoinAndTheBestWornPieceUnderAThousand()
        {
            var clothes = new ItemPiece(SliceWorldDriver.PlainClothes);
            var ring = Magic("ring");
            Assert.IsTrue(_bag.AddPiece(clothes) && _bag.AddPiece(ring));
            Assert.IsNull(_outfit.Wear(clothes));
            Assert.IsNull(_outfit.Wear(ring));
            _bag.Earn(400);

            var (bronze, piece) = _debts.Rob();
            Assert.AreEqual(400, bronze, "every coin");
            Assert.AreEqual(0, _bag.Bronze);
            Assert.AreSame(ring, piece, "the worn piece of the highest price");
            CollectionAssert.DoesNotContain(_bag.Pieces, ring);
            CollectionAssert.Contains(_bag.Pieces, clothes);

            _bag.Earn(YieldDebts.RobbersSpare);
            (bronze, piece) = _debts.Rob();
            Assert.AreEqual(YieldDebts.RobbersSpare, bronze);
            Assert.IsNull(piece, "a thousand or more, and they leave the clothes on its back");
            CollectionAssert.Contains(_bag.Pieces, clothes);
        }

        [Test]
        public void TheLockUpHoldsTheWeaponsGivesTheLootBackAndTheFinePaysForThem()
        {
            var pistol = new ItemPiece("pistol");
            var pouch = new ItemPiece("cartridge_pouch");
            var rifle = new ItemPiece("rifle");
            var clothes = new ItemPiece(SliceWorldDriver.PlainClothes);
            Assert.IsTrue(_bag.AddPiece(pistol) && _bag.AddPiece(pouch) && _bag.AddPiece(rifle) && _bag.AddPiece(clothes));
            Assert.IsNull(_outfit.Wear(pistol), "the pistol in hand, its pouch with it");
            _bag.Add("herb", 3);
            _breakIns.Took(200, "herb", 2);
            _breakIns.Noticed("dwelling.1.2", false);
            Assert.IsTrue(_breakIns.Wanted);
            _bag.Earn(500);

            Assert.AreEqual(3, _debts.LockUp(SliceConfront.WardensFine), "pistol, pouch and rifle");
            CollectionAssert.AreEquivalent(new[] { pistol, pouch, rifle }, _debts.Held);
            CollectionAssert.AreEqual(new[] { clothes }, _bag.Pieces, "only the clothes are left");
            Assert.IsNull(_outfit[WearSlot.WeaponOne], "nothing in hand");
            Assert.AreEqual(1, _bag.Count("herb"), "the stolen herbs went back");
            Assert.AreEqual(300, _bag.Bronze, "and the stolen coins");
            Assert.IsFalse(_breakIns.Wanted, "the lock-up settles the crime");
            Assert.AreEqual(SliceConfront.WardensFine, _debts.Owed);
            Assert.AreEqual(1, _state.Get(SliceConfront.WeaponsHeldKey));

            var saved = _debts.Capture();
            var again = new YieldDebts(_state, _bag, _outfit, _breakIns);
            again.Restore(saved);
            Assert.AreEqual(3, again.Held.Count, "the store keeps across a save");
            Assert.AreEqual(new[] { "pistol", "cartridge_pouch", "rifle" }.OrderBy(s => s), again.Held.Select(p => p.item).OrderBy(s => s));

            Assert.IsFalse(_debts.Pay(), "300 bronze does not pay 1 000");
            Assert.AreEqual(3, _debts.Held.Count);
            _bag.Earn(SliceConfront.WardensFine);
            Assert.IsTrue(_debts.Pay());
            Assert.AreEqual(300, _bag.Bronze);
            Assert.AreEqual(0, _debts.Owed);
            Assert.IsEmpty(_debts.Held);
            CollectionAssert.IsSubsetOf(new[] { pistol, pouch, rifle }, _bag.Pieces, "the weapons are back in the bag");
            Assert.AreEqual(0, _state.Get(SliceConfront.WeaponsHeldKey));
            Assert.IsFalse(_debts.Owing);
        }

        [Test]
        public void TheSweepTellsForAHundredBronze()
        {
            var clock = new GameClock(1d, 10 * 60);
            var pois = new[] { "PoiA", "PoiB", "PoiC" }.Select(n => AssetDatabase.LoadAssetAtPath<PoiDefinition>("Assets/Data/Slice/Poi/" + n + ".asset")).ToArray();
            using var director = new PoiDirector(pois, _state, _seed, SliceSeedStreams.PoiUnattended, clock);
            _state.Set("poi.c.phase", 1);
            var context = new DialogueContext
            {
                State = _state,
                Clock = clock,
                Seed = _seed,
                CheckStream = SliceSeedStreams.SocialChecks,
                Rules = _settings.SocialChecks,
                Standing = _standing,
                Character = _sheet,
                Director = director,
                Tips = new TipBook(AssetDatabase.LoadAssetAtPath<TipCatalogue>("Assets/Data/Slice/Poi/Tips.asset"), _state, director),
                Purse = () => _bag.Bronze,
                Spend = b => _bag.Spend(b),
            };
            var sweep = AssetDatabase.LoadAssetAtPath<DialogueDefinition>("Assets/Data/Slice/Dialogue/SweepDialogue.asset");

            _bag.Earn(99);
            var poor = new DialogueSession(sweep, context);
            Assert.IsFalse(poor.Options.Any(o => o.id == "pay"), "offered only to one who carries a hundred");
            Assert.IsTrue(poor.Options.Any(o => o.id == "exchange"), "the rods are still offered");
            poor.Leave();

            _bag.Earn(1);
            var talk = new DialogueSession(sweep, context);
            var pay = talk.Options.Single(o => o.id == "pay");
            Assert.AreEqual(Approach.Exchange, pay.approach);
            Assert.AreEqual(100, pay.bronze);
            var before = clock.Tick;
            talk.Choose(pay);
            Assert.AreEqual(0, _bag.Bronze, "paid");
            Assert.IsTrue(context.Tips.HasHeard(context.Tips.Find("sweep.coach")), "and told");
            Assert.Less(clock.Tick - before, 30, "with no half hour at the rods");
        }

        [Test]
        public void TheCroneSellsTheStairForFiftyWithNoPromise()
        {
            var crone = AssetDatabase.LoadAssetAtPath<DialogueDefinition>("Assets/Data/Slice/Dialogue/PoiBDialogue.asset");
            var node = crone.Node("crone");
            var pay = node.options.Single(o => o.id == "pay");
            Assert.AreEqual(Approach.Exchange, pay.approach);
            Assert.AreEqual(50, pay.bronze);
            CollectionAssert.Contains(pay.success.raises, "poi.b.heard.stair");
            CollectionAssert.DoesNotContain(pay.success.raises, "promise.crone", "paid, no promise is owed");
            Assert.AreEqual("stair", pay.success.next);
        }
    }
}

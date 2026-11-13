using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Slice.BreakIns;
using GameDev.Slice.Caches;
using GameDev.Slice.EditorTools;
using GameDev.World.Poi;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEditor;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5c.6: the Hidden Caches of SLICE_TRIAL §5: the forty spots, the seed's twenty and its locked third, the contents'
    /// shares and ranges, Luck on the coins, the notice check's training, the lock, and the notes as Tips.
    /// </summary>
    public sealed class HiddenCacheTests
    {
        private const string PoiFolder = "Assets/Data/Slice/Poi/";

        private SliceSettings _settings;
        private CharacterSheet _sheet;
        private Bag _bag;
        private WorldState _state;
        private WorldSeed _seed;
        private PoiDirector _director;
        private TipBook _tips;
        private SliceCaches _caches;

        [SetUp]
        public void SetUp()
        {
            _settings = AssetDatabase.LoadAssetAtPath<SliceSettings>(SliceConfrontSetup.SettingsPath);
            _sheet = new CharacterSheet();
            _bag = new Bag(_settings.Items, _sheet);
            _state = new WorldState();
            _seed = new WorldSeed(110213);
            var clock = new GameClock(1d, 14 * 60);
            var pois = new[] { "PoiA", "PoiB", "PoiC" }.Select(n => AssetDatabase.LoadAssetAtPath<PoiDefinition>(PoiFolder + n + ".asset")).ToArray();
            _director = new PoiDirector(pois, _state, _seed, SliceSeedStreams.PoiUnattended, clock);
            _tips = new TipBook(AssetDatabase.LoadAssetAtPath<TipCatalogue>(PoiFolder + "Tips.asset"), _state, _director);
            _caches = new SliceCaches(_state, _bag, _sheet, _seed, _settings.Items, _tips, () => clock.Tick);
        }

        [TearDown]
        public void TearDown() => _director?.Dispose();

        [Test]
        public void TheFortySpotsKeepSliceTrialsDistricts()
        {
            Assert.AreEqual(40, CacheSpots.All.Count);
            CollectionAssert.AreEqual(Enumerable.Range(1, 40), CacheSpots.All.Select(s => s.Number), "numbered 1–40 in order");
            var count = CacheSpots.All.GroupBy(s => s.District).ToDictionary(g => g.Key, g => g.Count());
            Assert.AreEqual(7, count[CacheSpots.ParsonsEnd]);
            Assert.AreEqual(5, count[CacheSpots.SleepyQuarter]);
            Assert.AreEqual(3, count[CacheSpots.ThePenny]);
            Assert.AreEqual(3, count[CacheSpots.SaltEnd]);
            Assert.AreEqual(6, count[CacheSpots.LowerVeil]);
            Assert.AreEqual(4, count[CacheSpots.TheReek]);
            Assert.AreEqual(12, count[CacheSpots.Outside]);
            Assert.IsTrue(CacheSpots.All.All(s => s.Stage == 8 || s.Stage == 9), "stage 9 or 8");
            Assert.AreEqual(2, SliceCaches.Lock(9));
            Assert.AreEqual(4, SliceCaches.Lock(8));
            Assert.AreEqual(2, SliceCaches.NoticeDifficulty(9));
            Assert.AreEqual(4, SliceCaches.NoticeDifficulty(8));
        }

        [Test]
        public void TheSeedChoosesTwentyWithSixOutsideAndThreeADistrictAtMost()
        {
            for (var s = 1; s <= 200; s++)
            {
                var seed = new WorldSeed(s);
                var chosen = SliceCaches.Choose(seed);
                Assert.AreEqual(SliceCaches.Chosen, chosen.Count, $"seed {s}: twenty");
                Assert.AreEqual(chosen.Count, chosen.Distinct().Count(), $"seed {s}: distinct");
                var spots = chosen.Select(CacheSpots.Of).ToList();
                Assert.GreaterOrEqual(spots.Count(p => p.Outside), SliceCaches.OutsideAtLeast, $"seed {s}: six outside at least");
                Assert.IsTrue(spots.Where(p => !p.Outside).GroupBy(p => p.District).All(g => g.Count() <= SliceCaches.PerDistrictAtMost), $"seed {s}: three a district at most");
                CollectionAssert.AreEqual(chosen, SliceCaches.Choose(new WorldSeed(s)), $"seed {s}: the same seed, the same caches");

                var locked = SliceCaches.Locked(seed, chosen);
                Assert.AreEqual(SliceCaches.LockedCount, locked.Count, $"seed {s}: a third locked");
                CollectionAssert.IsSubsetOf(locked, chosen);
            }

            CollectionAssert.AreNotEqual(SliceCaches.Choose(new WorldSeed(1)), SliceCaches.Choose(new WorldSeed(2)), "another seed, other caches");
        }

        [Test]
        public void TheContentsKeepTheTablesSharesAndRanges()
        {
            var gifts = new Dictionary<CacheGift, int>();
            var runs = 0;
            for (var s = 1; s <= 400; s++)
            {
                var seed = new WorldSeed(s);
                foreach (var number in new[] { 1, 17, 33 })
                {
                    foreach (var locked in new[] { false, true })
                    {
                        var c = SliceCaches.Contents(seed, _settings.Items, number, locked);
                        Assert.AreEqual(c.Gift, SliceCaches.Contents(new WorldSeed(s), _settings.Items, number, locked).Gift, "rolled from the seed and spot, so a save keeps it");
                        gifts.TryGetValue(c.Gift, out var n);
                        gifts[c.Gift] = n + 1;
                        runs++;
                        switch (c.Gift)
                        {
                            case CacheGift.Coins:
                                Assert.That(c.Bronze, locked ? Is.InRange(150, 600) : Is.InRange(50, 300));
                                break;
                            case CacheGift.Supplies:
                                Assert.AreEqual(locked ? 2 : 1, c.Supplies.Count, "two kinds in a locked cache");
                                Assert.AreEqual(c.Supplies.Count, c.Supplies.Select(p => p.Id).Distinct().Count());
                                Assert.IsTrue(c.Supplies.All(p => SliceCaches.SupplyKinds.Contains(p)));
                                Assert.IsTrue(c.Supplies.All(p => _settings.Items.Find(p.Id) != null), "every supply is in the catalogue");
                                break;
                            case CacheGift.Jewel:
                                Assert.IsNotNull(c.Jewel);
                                CollectionAssert.Contains(SliceCaches.Jewels, c.Jewel.item);
                                Assert.IsNotNull(_settings.Items.Find(c.Jewel.item), "every jewel is in the catalogue");
                                break;
                            case CacheGift.Note:
                                Assert.That(c.Note, Is.InRange(1, SliceCaches.Notes));
                                Assert.AreEqual(locked ? SliceCaches.NoteBronze : 0, c.Bronze);
                                break;
                        }
                    }
                }
            }

            double Share(CacheGift g) => gifts.TryGetValue(g, out var n) ? (double)n / runs : 0d;
            Assert.AreEqual(SliceCaches.CoinsShare, Share(CacheGift.Coins), 0.05d, "60 % coins");
            Assert.AreEqual(SliceCaches.SuppliesShare, Share(CacheGift.Supplies), 0.05d, "25 % supplies");
            Assert.AreEqual(SliceCaches.JewelShare, Share(CacheGift.Jewel), 0.04d, "10 % a jewel");
            Assert.AreEqual(1d - SliceCaches.CoinsShare - SliceCaches.SuppliesShare - SliceCaches.JewelShare, Share(CacheGift.Note), 0.03d, "5 % a note");
        }

        [Test]
        public void LuckMultipliesTheCoins()
        {
            Assert.AreEqual(100, SliceCaches.WithLuck(100, 0));
            Assert.AreEqual(110, SliceCaches.WithLuck(100, 10));
            Assert.AreEqual(200, SliceCaches.WithLuck(100, 100));
        }

        [Test]
        public void ANoticeTrainsPerceptionAndANoticedCacheGivesLuck()
        {
            var number = _caches.ChosenSpots.First();
            var perception = _sheet.Xp(CharacterAttribute.Perception);
            var luck = _sheet.Xp(CharacterAttribute.Luck);
            Assert.IsFalse(_caches.TryNotice(number, 0d, new System.Random(1)), "a failed roll");
            Assert.Greater(_sheet.Xp(CharacterAttribute.Perception), perception, "a failure trains Perception, by half (07.1 §3)");
            Assert.AreEqual(luck, _sheet.Xp(CharacterAttribute.Luck), "no Luck for a cache not noticed");
            Assert.IsFalse(_caches.Noticed(number));

            var failed = _sheet.Xp(CharacterAttribute.Perception) - perception;
            perception = _sheet.Xp(CharacterAttribute.Perception);
            Assert.IsTrue(_caches.TryNotice(number, 1d, new System.Random(1)));
            Assert.AreEqual(failed * 2d, _sheet.Xp(CharacterAttribute.Perception) - perception, 1e-9, "a success trains twice a failure");
            Assert.AreEqual(luck + Growth.CacheLuckXp, _sheet.Xp(CharacterAttribute.Luck), 1e-9, "Luck grows a little");
            Assert.IsTrue(_caches.Noticed(number));
            Assert.IsFalse(_caches.TryNotice(number, 1d, new System.Random(1)), "noticed once");

            var elsewhere = Enumerable.Range(1, 40).First(n => !_caches.Holds(n));
            Assert.IsFalse(_caches.TryNotice(elsewhere, 1d, new System.Random(1)), "no cache, nothing to notice");
        }

        [Test]
        public void ACacheIsTakenOnceAndOnlyNoticedAndOpen()
        {
            _sheet.Set(CharacterAttribute.Luck, 20);
            foreach (var number in _caches.ChosenSpots)
            {
                Assert.IsNull(_caches.Take(number), "not noticed yet");
                Assert.IsTrue(_caches.TryNotice(number, 1d, new System.Random(number)));
                if (_caches.IsLocked(number))
                {
                    Assert.IsTrue(_caches.Shut(number));
                    Assert.IsNull(_caches.Take(number), "a locked one needs its lock picked");
                    _bag.Add(SliceBreakIns.LockpicksItem, 2);
                    var picks = _caches.Picks;
                    Assert.IsFalse(_caches.Resolve(number, ReactionResult.Bad));
                    Assert.AreEqual(picks - 1, _caches.Picks, "a bad try breaks a pick");
                    Assert.IsTrue(_caches.Shut(number));
                    Assert.IsTrue(_caches.Resolve(number, ReactionResult.Good));
                    Assert.IsFalse(_caches.Shut(number), "open for good");
                }

                var contents = _caches.ContentsOf(number);
                var bronze = _bag.Bronze;
                var stacks = contents.Supplies.ToDictionary(p => p.Id, p => _bag.Count(p.Id));
                var pieces = _bag.Pieces.Count;
                var taken = _caches.Take(number);
                Assert.IsNotNull(taken);
                Assert.AreEqual(SliceCaches.WithLuck(contents.Bronze, _sheet.Get(CharacterAttribute.Luck)), taken.Value.Bronze, "Luck on the coins");
                Assert.AreEqual(bronze + taken.Value.Bronze, _bag.Bronze);
                foreach (var (id, count) in contents.Supplies)
                {
                    Assert.AreEqual(stacks[id] + count, _bag.Count(id), $"{id} in the bag");
                }

                Assert.AreEqual(pieces + (contents.Jewel != null ? 1 : 0), _bag.Pieces.Count, "the jewel in the bag");
                if (contents.Note > 0)
                {
                    Assert.IsTrue(_state.IsSet(SliceCaches.NoteFoundKey(contents.Note)));
                    Assert.IsTrue(_tips.HasHeard(_tips.Find(SliceCaches.NoteTip(contents.Note))), "the note is a Tip heard, in the Character Notes");
                }

                Assert.IsTrue(_caches.Taken(number));
                Assert.IsNull(_caches.Take(number), "taken once");
            }
        }

        [Test]
        public void TheNotesAreTipsWithBothLanguages()
        {
            var catalogue = AssetDatabase.LoadAssetAtPath<TipCatalogue>(PoiFolder + "Tips.asset");
            for (var n = 1; n <= SliceCaches.Notes; n++)
            {
                var id = SliceCaches.NoteTip(n);
                var tip = _tips.Find(id);
                Assert.IsNotNull(tip, $"{id} in the catalogue");
                Assert.AreEqual(TipSource.PhysicalTrace, tip.source, "found where it lies");
                Assert.IsNull(_tips.Tell(id, 0), "told only once found");
                _state.Raise(SliceCaches.NoteFoundKey(n));
                Assert.IsNotNull(_tips.Tell(id, 0), "found, it is heard");
                foreach (var key in new[] { $"tip.{id}.line", $"tip.{id}.note" })
                {
                    Assert.IsTrue(Loc.Tables.Has(key, Loc.English) && Loc.Tables.Has(key, Loc.Russian), $"{key} has its Russian and English text");
                }
            }

            Assert.IsNotNull(catalogue);
            foreach (var key in new[] { "ui.prompt.cache.take", "ui.cache.noticed", "ui.cache.bronze", "ui.cache.goods", "ui.cache.part", "ui.cache.note" })
            {
                Assert.IsTrue(Loc.Tables.Has(key, Loc.English) && Loc.Tables.Has(key, Loc.Russian), $"{key} has its Russian and English text");
            }
        }
    }
}

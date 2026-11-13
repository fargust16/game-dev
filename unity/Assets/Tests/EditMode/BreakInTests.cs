using System.Linq;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Player.Standing;
using GameDev.Slice.BreakIns;
using GameDev.Slice.Dwellings;
using GameDev.Slice.EditorTools;
using GameDev.Slice.News;
using GameDev.Slice.People;
using GameDev.Slice.Theft;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEditor;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5c.5: break-ins by 10.3 §6.1–6.2 and 19 §6.3: the lock and its picks, a noticed crime and the Wanted hours, the
    /// Stash read from the household, a resident's report, who is at home by the hour, the named people's homes, and a
    /// theft's purses and answers.
    /// </summary>
    public sealed class BreakInTests
    {
        private CharacterSheet _sheet;
        private Bag _bag;
        private WorldState _state;
        private GameClock _clock;
        private SocialStanding _standing;
        private WorldSeed _seed;
        private SliceBreakIns _breakIns;

        [SetUp]
        public void SetUp()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SliceSettings>(SliceConfrontSetup.SettingsPath);
            _sheet = new CharacterSheet();
            _bag = SliceWorldDriver.StartingBag(settings.Items, _sheet);
            _state = new WorldState();
            _clock = new GameClock(1d, 23 * 60); // 23:00 of day 1
            _seed = new WorldSeed(110208);
            _standing = new SocialStanding(_state, settings.Standing, _clock, _seed, SliceSeedStreams.Lies);
            _breakIns = new SliceBreakIns(_bag, _state, _clock, _sheet, _standing, _seed);
        }

        [Test]
        public void APickBreaksOnABadTryAndAGoodOneOpensUntilMorning()
        {
            _bag.Add(SliceBreakIns.LockpicksItem, 5);
            Assert.AreEqual(5, _breakIns.Picks);
            Assert.IsFalse(_breakIns.IsOpen("dwelling.1.2"));
            Assert.AreEqual(ReactionCheck.Delta(_sheet.Get(CharacterAttribute.Dexterity), _sheet.Get(CharacterSkill.Theft), 4), _breakIns.Delta(4), 1e-9,
                "07.1 §4: Δ = (Dexterity + Theft) / 2 − Lock");

            var theftBefore = _sheet.Get(CharacterSkill.Theft);
            Assert.IsFalse(_breakIns.Resolve("dwelling.1.2", 4, ReactionResult.Bad));
            Assert.AreEqual(4, _breakIns.Picks, "a bad try breaks a pick");
            Assert.IsFalse(_breakIns.IsOpen("dwelling.1.2"));

            Assert.IsTrue(_breakIns.Resolve("dwelling.1.2", 4, ReactionResult.Good));
            Assert.AreEqual(4, _breakIns.Picks, "a good one breaks none");
            Assert.IsTrue(_breakIns.IsOpen("dwelling.1.2"));
            Assert.GreaterOrEqual(_sheet.Get(CharacterSkill.Theft), theftBefore, "each try grows Theft (07.1 §3)");

            _clock.AdvanceTicks((7 * 60) - 1); // to 05:59 of day 2
            Assert.IsTrue(_breakIns.IsOpen("dwelling.1.2"), "open until 06:00");
            _clock.AdvanceTicks(1);
            Assert.IsFalse(_breakIns.IsOpen("dwelling.1.2"), "shut again from 06:00");
        }

        [Test]
        public void ANoticedCrimeCostsReputationOnceAPlaceADayAndIsWantedUntil0800()
        {
            var before = _standing.ReputationPoints;
            _breakIns.Noticed("dwelling.1.2", false);
            var after = _standing.ReputationPoints;
            Assert.Less(after, before, "10.4 §4: a noticed break-in costs Reputation");
            _breakIns.Noticed("dwelling.1.2", false);
            Assert.AreEqual(after, _standing.ReputationPoints, "once a place a day");
            Assert.IsTrue(_breakIns.Wanted);
            Assert.IsFalse(_breakIns.Seen);
            _breakIns.Noticed(null, true);
            Assert.IsTrue(_breakIns.Seen, "a Warden saw it");
            Assert.AreEqual(after, _standing.ReputationPoints, "no place, no Reputation: a noticed theft counted already");

            _clock.AdvanceTicks((9 * 60) - 1); // to 07:59 of day 2
            Assert.IsTrue(_breakIns.Wanted, "Wanted until 08:00 (10.3 §6.2)");
            _clock.AdvanceTicks(1);
            Assert.IsFalse(_breakIns.Wanted);
            Assert.IsFalse(_breakIns.Seen);
        }

        [Test]
        public void TheStashReadsFromTheHouseholdAndIsTakenOnce()
        {
            for (var wealth = 0; wealth < 3; wealth++)
            {
                for (var i = 0; i < 40; i++)
                {
                    var held = _breakIns.StashOf($"dwelling.{wealth}.{i}", wealth);
                    var (least, most) = SliceBreakIns.StashBronze[wealth];
                    Assert.That(held.Bronze, Is.InRange(least, most), "19 §6.3's range");
                    Assert.That(held.Papers, Is.InRange(0, SliceBreakIns.PaperKinds));
                    Assert.AreEqual(held.Bronze, _breakIns.StashOf($"dwelling.{wealth}.{i}", wealth).Bronze, "the same in every look");
                }
            }

            var papers = Enumerable.Range(0, 700).Count(i => _breakIns.StashOf($"dwelling.p.{i}", 1).Papers > 0);
            Assert.That(papers, Is.InRange(60, 140), "about one in seven holds papers");

            var goods = Enumerable.Range(0, 40).Select(i => _breakIns.StashOf($"dwelling.t.{i}", 1, "bandage", 200)).ToList();
            Assert.IsTrue(goods.Any(g => g.Count > 0) && goods.Any(g => g.Count == 0), "a trade's goods one time in two");
            Assert.IsTrue(goods.Where(g => g.Count > 0).All(g => g.Item == "bandage" && g.Count >= 1 && g.Count <= 5), "the goods by value: 300–1000 at 200 each");

            var bronze = _bag.Bronze;
            var taken = _breakIns.TakeStash("dwelling.9.9", 2);
            Assert.IsTrue(taken.HasValue);
            Assert.AreEqual(bronze + taken.Value.Bronze, _bag.Bronze);
            Assert.IsTrue(_breakIns.StashTaken("dwelling.9.9"));
            Assert.IsNull(_breakIns.TakeStash("dwelling.9.9", 2), "empty for the run");
            Assert.AreEqual(taken.Value.Bronze, _breakIns.LootBronze, "the night's loot");
            Assert.IsFalse(_breakIns.NothingStolenCarried, "the bronze is on the character");
            Assert.IsTrue(_breakIns.CanGiveBack);

            _breakIns.GiveBack();
            Assert.AreEqual(bronze, _bag.Bronze, "given back");
            Assert.AreEqual(0, _breakIns.LootBronze);
            Assert.IsTrue(_breakIns.NothingStolenCarried);
        }

        [Test]
        public void AResidentsReportWaitsForTheStreetAndOnlyWhileWanted()
        {
            Assert.IsFalse(_breakIns.HasReport);
            _breakIns.Noticed("dwelling.120.340", false);
            _breakIns.ReportLater("dwelling.120.340", 120, 340);
            Assert.IsTrue(_breakIns.HasReport);
            Assert.IsTrue(_breakIns.ReportedAt(120, 340));
            Assert.IsFalse(_breakIns.ReportedAt(121, 340));
            _clock.AdvanceTicks(5);
            Assert.IsTrue(_breakIns.TakeReport(out var door, out var since));
            Assert.AreEqual(12f, door.X, 1e-4f, "decimetres to metres");
            Assert.AreEqual(34f, door.Z, 1e-4f);
            Assert.AreEqual(5, since);
            Assert.IsFalse(_breakIns.HasReport, "taken once");

            _breakIns.ReportLater("dwelling.120.340", 120, 340);
            _breakIns.ClearWanted();
            Assert.IsFalse(_breakIns.TakeReport(out _, out _), "the Arrest settled: nobody comes");
        }

        [Test]
        public void WhoIsAtHomeFollowsTheHour()
        {
            var terrace = new DwellingKey("dwelling.100.200", "Veilmouth", 100, 200, DwellingArchetype.Terrace, DwellingWealth.Rich);
            var office = new DwellingKey("dwelling.300.200", "Veilmouth", 300, 200, DwellingArchetype.Office, DwellingWealth.Middling);
            Assert.IsEmpty(DwellingResidents.Household(office, 0, null, 1, 23 * 60, _seed), "an office has no residents");

            var night = DwellingResidents.Household(terrace, 2, null, 1, 23 * 60, _seed);
            Assert.That(night.Count(m => !m.Servant), Is.InRange(1, DwellingResidents.HouseholdMost));
            Assert.AreEqual(2, night.Count(m => m.Servant), "the servants sleep in");
            Assert.IsTrue(night.All(m => m.Asleep), "asleep at night");

            var evening = DwellingResidents.Household(terrace, 2, null, 1, 19 * 60, _seed);
            Assert.AreEqual(night.Count, evening.Count, "everyone at home in the evening");
            Assert.IsTrue(evening.All(m => !m.Asleep));

            var days = Enumerable.Range(1, 400).Select(d => DwellingResidents.Household(terrace, 0, null, d, 12 * 60, _seed).Count).ToList();
            var share = days.Sum() / (double)(days.Count * night.Count(m => !m.Servant));
            Assert.That(share, Is.InRange(0.15, 0.35), "by day home one day in four");
            Assert.AreEqual(2, DwellingResidents.Household(terrace, 2, null, 1, 12 * 60, _seed).Count(m => m.Servant), "the servants home 06:00–22:00");

            // A named person is home only while its hours name no post, and asleep by them (SLICE_PEOPLE §4).
            Assert.IsFalse(DwellingResidents.Household(terrace, 0, "edda", 1, 12 * 60, _seed).Any(m => m.Person == "edda"), "at her stall");
            Assert.IsTrue(DwellingResidents.Household(terrace, 0, "edda", 1, 23 * 60, _seed).Single(m => m.Person == "edda").Asleep);
            Assert.IsFalse(DwellingResidents.Household(terrace, 0, "edda", 1, 23 * 60, _seed, p => p == "edda").Any(m => m.Person == "edda"), "the fallen are gone for good");
        }

        [Test]
        public void EachNamedPersonGetsAHomeOfItsClassInItsPlace()
        {
            SliceHomes.Forget();
            var wealthOf = new System.Collections.Generic.Dictionary<string, int>();
            try
            {
                foreach (var (person, place, wealth) in SliceHomes.Table)
                {
                    var area = SlicePlaces.All.First(p => p.key == place);
                    for (var i = 0; i < 3; i++)
                    {
                        var x = area.x0 + ((area.x1 - area.x0) * (i + 1) / 4f);
                        var z = (area.z0 + area.z1) / 2f;
                        wealthOf[$"dwelling.{person}.{i}"] = (wealth + i) % 3;
                        SliceHomes.Know($"dwelling.{person}.{i}", x, z, (wealth + i) % 3);
                    }
                }

                var homes = SliceHomes.Table.Select(h => SliceHomes.HomeOf(h.Person, _seed)).ToList();
                Assert.IsTrue(homes.All(h => h != null), "every named person has a home");
                Assert.AreEqual(homes.Count, homes.Distinct().Count(), "two people never in one");
                foreach (var (person, _, _) in SliceHomes.Table)
                {
                    Assert.AreEqual(person, SliceHomes.PersonAt(SliceHomes.HomeOf(person, _seed), _seed));
                }

                foreach (var (person, _, wealth) in SliceHomes.Table)
                {
                    Assert.AreEqual(wealth, wealthOf[SliceHomes.HomeOf(person, _seed)], $"{person}'s home of its class, where one is");
                }
                CollectionAssert.AreEqual(homes, SliceHomes.Table.Select(h => SliceHomes.HomeOf(h.Person, new WorldSeed(110208))).ToList(), "the same seed, the same homes");
            }
            finally
            {
                SliceHomes.Forget();
            }
        }

        [Test]
        public void APursesKindSetsItsRangeAndAnswersAreThree()
        {
            var theft = new SliceTheft(_bag, _state, _clock, _sheet, _standing, _seed);
            for (var i = 0; i < 50; i++)
            {
                Assert.That(theft.Purse($"p{i}"), Is.InRange(100, 600));
                Assert.That(theft.Purse($"p{i}", PurseKind.Guard), Is.InRange(300, 800));
                Assert.That(theft.Purse($"p{i}", PurseKind.Merchant), Is.InRange(1000, 3000));
            }

            var answers = Enumerable.Range(0, 300).Select(i => theft.Answer($"p{i}")).ToList();
            foreach (TheftAnswer answer in System.Enum.GetValues(typeof(TheftAnswer)))
            {
                Assert.That(answers.Count(a => a == answer), Is.InRange(60, 140), $"{answer} about one time in three");
            }

            Assert.AreEqual(theft.Answer("p1"), theft.Answer("p1"), "the same person answers the same that day");
        }
    }
}

using System.Linq;
using GameDev.Core.Localization;
using GameDev.Slice.News;
using GameDev.World.Seed;
using GameDev.World.State;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// 09 *News Pool* and 10.2 §5e (PB8d): a death enters Veilmouth's pool 1–2 game hours after it and stays 8 game hours;
    /// it is told with the place the map names, how long ago, and who killed if someone saw it.
    /// </summary>
    public sealed class SliceNewsTests
    {
        private string _language;

        [SetUp]
        public void English()
        {
            _language = Loc.Language;
            Loc.Language = Loc.English;
        }

        [TearDown]
        public void Back() => Loc.Language = _language;

        private static Vector3 Middle(string key)
        {
            var p = SlicePlaces.All.First(x => x.key == key);
            return new Vector3((p.x0 + p.x1) / 2f, 0f, (p.z0 + p.z1) / 2f);
        }

        [Test]
        public void ADeathEntersAnHourOrTwoLaterAndStaysEightHours()
        {
            var news = new SliceNews(new WorldState(), new WorldSeed(110201));
            var at = Middle("map.black_puddle");
            for (var i = 0; i < 20; i++)
            {
                var n = news.AddDeath(1000 + i, at, NewsVictim.Townsman, NewsKiller.Unseen);
                var item = news.Get(n);
                Assert.That(item.Enters - item.Happened, Is.InRange(SliceNews.DelayLeast, SliceNews.DelayMost), "1–2 game hours later");
            }

            var first = news.Get(1);
            Assert.IsFalse(news.Told(first.Enters - 1).Any(x => x.Index == 1), "not before it enters");
            Assert.IsTrue(news.Told(first.Enters).Any(x => x.Index == 1), "told once it enters");
            Assert.IsTrue(news.Told(first.Enters + SliceNews.StaysTicks - 1).Any(x => x.Index == 1), "for 8 game hours");
            Assert.IsFalse(news.Told(first.Enters + SliceNews.StaysTicks).Any(x => x.Index == 1), "and then gone");
            var told = news.Told(first.Enters + 200);
            Assert.AreEqual(told.OrderByDescending(x => x.Index).Select(x => x.Index), told.Select(x => x.Index), "the newest first");
        }

        [Test]
        public void ADeathIsToldWithThePlaceHowLongAgoAndWhoKilledIfSeen()
        {
            var news = new SliceNews(new WorldState(), new WorldSeed(7));
            var seen = news.Get(news.AddDeath(500, Middle("map.black_puddle"), NewsVictim.Warden, NewsKiller.Character));
            Assert.AreEqual("map.black_puddle", seen.Place, "the square the map names");
            var line = SliceNews.Line(seen, 500 + 125);
            StringAssert.Contains("a Warden", line);
            StringAssert.Contains("Black Puddle", line);
            StringAssert.Contains("about two hours ago", line);
            StringAssert.Contains("Witnesses saw who did it: a stranger.", line);

            var unseen = news.Get(news.AddDeath(500, new Vector3(-400f, 0f, 400f), NewsVictim.Townswoman, NewsKiller.Unseen));
            Assert.AreEqual(SlicePlaces.OutsideKey, unseen.Place, "far from every place: outside the walls");
            var second = SliceNews.Line(unseen, 500 + 60);
            StringAssert.Contains("a townswoman", second);
            StringAssert.Contains("outside the walls", second);
            StringAssert.Contains("about an hour ago", second);
            StringAssert.Contains("Nobody saw who did it.", second);
        }

        [Test]
        public void ThePlacesAreTheMapsNarrowestFirst()
        {
            Assert.Greater(SlicePlaces.All.Count, 30, "the places were baked from layout C5");
            foreach (var key in new[] { "map.black_puddle", "map.the_penny" })
            {
                Assert.AreEqual(key, SlicePlaces.KeyAt(Middle(key)), $"{key}: its own middle");
            }

            var street = SlicePlaces.All.First(p => p.kind == SlicePlaces.Street && !SlicePlaces.All.Any(q => q != p && q.kind is SlicePlaces.Square or SlicePlaces.Bridge
                && q.x0 <= (p.x0 + p.x1) / 2f && q.x1 >= (p.x0 + p.x1) / 2f && q.z0 <= (p.z0 + p.z1) / 2f && q.z1 >= (p.z0 + p.z1) / 2f));
            Assert.AreEqual(street.kind, SlicePlaces.All.First(p => p.key == SlicePlaces.KeyAt(Middle(street.key))).kind, "a street's middle is a street, not its district");
            Assert.AreEqual(SlicePlaces.OutsideKey, SlicePlaces.KeyAt(new Vector3(-600f, 0f, -600f)));
            Assert.AreEqual("Sleepy Quarter", SlicePlaces.Name("map.sleepy_quarter"), "a district in capitals on the map, in ordinary case");
            Assert.AreEqual(SlicePlaces.TownKey, SlicePlaces.KeyOf(StableHash.Of(SlicePlaces.TownKey)));
        }

        [Test]
        public void HowLongAgoIsInWholeHoursFromOneToTen()
        {
            Assert.AreEqual("news.ago.1", SliceNews.AgoKey(20));
            Assert.AreEqual("news.ago.1", SliceNews.AgoKey(89));
            Assert.AreEqual("news.ago.2", SliceNews.AgoKey(90));
            Assert.AreEqual("news.ago.8", SliceNews.AgoKey(480));
            Assert.AreEqual("news.ago.10", SliceNews.AgoKey(2000));
        }
    }
}

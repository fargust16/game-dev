using System.Collections.Generic;
using System.Linq;
using GameDev.World.Poi;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>The tips of S1.3: what the sources say when, and 09's rule from Hinted to Marked.</summary>
    public sealed class TipBookTests
    {
        private const string Folder = "Assets/Data/Slice/Poi/";
        private readonly List<Object> _made = new List<Object>();
        private PoiDefinition _a;
        private PoiDefinition _b;
        private PoiDefinition _c;
        private WorldState _state;
        private GameClock _clock;
        private PoiDirector _director;
        private TipBook _book;

        [SetUp]
        public void Begin()
        {
            _a = AssetDatabase.LoadAssetAtPath<PoiDefinition>(Folder + "PoiA.asset");
            _b = AssetDatabase.LoadAssetAtPath<PoiDefinition>(Folder + "PoiB.asset");
            _c = AssetDatabase.LoadAssetAtPath<PoiDefinition>(Folder + "PoiC.asset");
            _state = new WorldState();
            _clock = new GameClock(1d, 480);
            _director = new PoiDirector(new[] { _a, _b, _c }, _state, new WorldSeed(110201), SliceSeedStreams.PoiUnattended, _clock);
            _book = new TipBook(AssetDatabase.LoadAssetAtPath<TipCatalogue>(Folder + "Tips.asset"), _state, _director);
        }

        [TearDown]
        public void End()
        {
            _director.Dispose();
            foreach (var made in _made)
            {
                Object.DestroyImmediate(made);
            }

            _made.Clear();
        }

        [Test]
        public void TheBoardSaysItsMorningNoticesAndPinsTheSeizureAtTwo()
        {
            CollectionAssert.AreEqual(new[] { "board.girls", "board.wren" }, Ids(TipSource.NoticeBoard));
            _clock.AdvanceTicks(840 - 480);
            CollectionAssert.AreEqual(new[] { "board.girls", "board.wren", "board.seizure" }, Ids(TipSource.NoticeBoard));
            _clock.AdvanceTicks(1440);
            CollectionAssert.AreEqual(new[] { "board.wren" }, Ids(TipSource.NoticeBoard), "day 2: the Wren notice stays");
        }

        [Test]
        public void ReadingTheSeizureNoticeMarksPoiA()
        {
            _clock.AdvanceTicks(840 - 480);
            _book.Read(TipSource.NoticeBoard, _clock.Tick);

            Assert.AreEqual(PoiDiscovery.Marked, _director.Discovery(_a), "the notice names the shop");
            Assert.AreEqual(PoiDiscovery.Hinted, _director.Discovery(_b), "'near Draught Lane' only hints");
        }

        [Test]
        public void HintsFromTwoDifferentSourcesMarkAPoi()
        {
            _book.Read(TipSource.StreetRumour, _clock.Tick);
            Assert.AreEqual(PoiDiscovery.Hinted, _director.Discovery(_b));
            Assert.AreEqual(PoiDiscovery.Hinted, _director.Discovery(_a));

            _book.Read(TipSource.NoticeBoard, _clock.Tick);
            Assert.AreEqual(PoiDiscovery.Marked, _director.Discovery(_b), "the newsboy and the board both hinted B");
            Assert.AreEqual(PoiDiscovery.Hinted, _director.Discovery(_a), "the morning board says nothing of A");
        }

        [Test]
        public void TwoHintsFromOneSourceDoNotMark()
        {
            var poi = ScriptableObject.CreateInstance<PoiDefinition>();
            _made.Add(poi);
            poi.Configure("x", PoiWindowClass.Standard, 600, null, 60, -1, null, Vector3.zero, 30f);
            var catalogue = ScriptableObject.CreateInstance<TipCatalogue>();
            _made.Add(catalogue);
            catalogue.Configure(new[]
            {
                new Tip { id = "r1", source = TipSource.StreetRumour, poi = poi, effect = TipEffect.Hints },
                new Tip { id = "r2", source = TipSource.StreetRumour, poi = poi, effect = TipEffect.Hints },
            });
            using var director = new PoiDirector(new[] { poi }, _state, new WorldSeed(1), SliceSeedStreams.PoiUnattended, _clock);
            new TipBook(catalogue, _state, director).Read(TipSource.StreetRumour, _clock.Tick);

            Assert.AreEqual(PoiDiscovery.Hinted, director.Discovery(poi));
        }

        [Test]
        public void ThePublicanSpeaksFromFourAndKeepsTheApproachTipForS2()
        {
            CollectionAssert.IsEmpty(Ids(TipSource.Bartender), "Nell has nothing before 16:00");
            _clock.AdvanceTicks(960 - 480);
            CollectionAssert.AreEqual(new[] { "nell.harrow" }, Ids(TipSource.Bartender), "the Draught Lane tip needs an Approach");
        }

        [Test]
        public void DayTwoTipsFollowWhatHappened()
        {
            _clock.AdvanceTicks(1440 + 960 - 480);
            Assert.AreEqual(PoiPhase.Open, _director.Phase(_c));
            Assert.Contains("nell.chalk", Ids(TipSource.Bartender).ToList());

            var rumours = Ids(TipSource.StreetRumour).ToList();
            Assert.AreEqual(_state.IsSet("aftermath.harrow.shop_sealed"), rumours.Contains("kit.seal"), "the seal rumour only after the Wardens sealed the shop");
            Assert.Contains("kit.glove", rumours, "B resolved in the night");
        }

        [Test]
        public void HeardTipsKeepTheirOrderAndAreHeardOnce()
        {
            var heard = new List<string>();
            _book.Heard += t => heard.Add(t.id);

            _book.Read(TipSource.StreetRumour, _clock.Tick);
            _book.Read(TipSource.NoticeBoard, _clock.Tick);
            _book.Read(TipSource.StreetRumour, _clock.Tick);

            var order = new[] { "kit.drawers", "kit.coachcoats", "kit.den", "board.girls", "board.wren" };
            CollectionAssert.AreEqual(order, heard);
            CollectionAssert.AreEqual(order, _book.HeardInOrder().Select(t => t.id));

            var loaded = new WorldState();
            loaded.Restore(_state.Capture());
            using var director = new PoiDirector(new[] { _a, _b, _c }, loaded, new WorldSeed(110201), SliceSeedStreams.PoiUnattended, _clock);
            var book = new TipBook(AssetDatabase.LoadAssetAtPath<TipCatalogue>(Folder + "Tips.asset"), loaded, director);
            CollectionAssert.AreEqual(order, book.HeardInOrder().Select(t => t.id), "a save keeps what was heard");
        }

        [Test]
        public void EveryPoiHasAtLeastTwoSources()
        {
            var tips = AssetDatabase.LoadAssetAtPath<TipCatalogue>(Folder + "Tips.asset").Tips;
            foreach (var poi in new[] { _a, _b, _c })
            {
                var sources = tips.Where(t => t.poi == poi && t.effect != TipEffect.None && !t.needsApproach).Select(t => t.source).Distinct().Count();
                Assert.GreaterOrEqual(sources, 2, $"POI {poi.Id}: missing one source never hides it (09)");
            }
        }

        private IEnumerable<string> Ids(TipSource source) => _book.Available(source, _clock.Tick).Select(t => t.id);
    }
}

using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Dialogue;
using GameDev.Player.Progression;
using GameDev.Player.Standing;
using GameDev.Slice.EditorTools;
using GameDev.Slice.Fights;
using GameDev.Slice.News;
using GameDev.World.Poi;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5c.7: POI A's and B's News (SLICE_TRIAL §6; 09 *News Pool*): told by the board, the bartender and Street Rumour in
    /// their own words, a lasting event in the present tense while it lasts, an outcome in the past tense for 8 hours;
    /// every item told is heard and noted in the order things are heard.
    /// </summary>
    public sealed class NewsPoolTests
    {
        private string _language;
        private WorldState _state;
        private WorldSeed _seed;
        private SliceNews _news;

        [SetUp]
        public void SetUp()
        {
            _language = Loc.Language;
            Loc.Language = Loc.English;
            _state = new WorldState();
            _seed = new WorldSeed(110214);
            _news = new SliceNews(_state, _seed);
        }

        [TearDown]
        public void Back() => Loc.Language = _language;

        private List<string> Told(TipSource teller, long tick, System.Func<string, bool> byTip = null) =>
            _news.Tell(teller, tick, byTip).Select(f => f()).ToList();

        [Test]
        public void EveryItemHasItsWordsInBothLanguagesForEachTeller()
        {
            foreach (var e in SliceNews.Events)
            {
                foreach (var teller in e.Tellers)
                {
                    var key = SliceNews.LineKey(e, teller);
                    Assert.IsTrue(Loc.Tables.Has(key, Loc.English) && Loc.Tables.Has(key, Loc.Russian), $"no words for {key}");
                }

                var note = SliceNews.EventNoteKey(e);
                Assert.IsTrue(Loc.Tables.Has(note, Loc.English) && Loc.Tables.Has(note, Loc.Russian), $"no note for {e.Id}");
            }

            Assert.IsTrue(Loc.Tables.Has("news.death.note", Loc.Russian));
        }

        [Test]
        public void ALastingEventIsToldWhileItLastsAndAnOutcomeForEightHours()
        {
            _state.Set("poi.a.phase", (int)PoiPhase.Open);
            CollectionAssert.AreEqual(new[] { Loc.Get("news.a.arrest.nell") }, Told(TipSource.Bartender, 100));
            CollectionAssert.AreEqual(new[] { Loc.Get("news.a.arrest.board") }, Told(TipSource.NoticeBoard, 100));
            _state.Set("poi.b.phase", (int)PoiPhase.Open);
            CollectionAssert.AreEquivalent(new[] { Loc.Get("news.a.arrest.kit"), Loc.Get("news.b.snatch.kit") }, Told(TipSource.StreetRumour, 100));
            CollectionAssert.DoesNotContain(Told(TipSource.Bartender, 100), Loc.Get("news.b.snatch.kit"), "only Street Rumour tells the snatch");

            // POI A ends: the arrest is no longer told, and its outcome enters on the tick its Aftermath is raised.
            _state.Set("poi.a.phase", (int)PoiPhase.Resolved);
            _state.Set("poi.b.phase", (int)PoiPhase.Resolved);
            _state.Raise("aftermath.harrow.taken");
            _state.Raise("aftermath.harrow.shop_sealed");
            _news.Notice(200);
            CollectionAssert.AreEqual(new[] { Loc.Get("news.a.sealed.nell") }, Told(TipSource.Bartender, 210));
            CollectionAssert.AreEqual(new[] { Loc.Get("news.a.sealed.board") }, Told(TipSource.NoticeBoard, 210));
            CollectionAssert.AreEqual(new[] { Loc.Get("news.a.sealed.nell") }, Told(TipSource.Bartender, 200 + SliceNews.StaysTicks - 1), "for 8 game hours");
            CollectionAssert.IsEmpty(Told(TipSource.Bartender, 200 + SliceNews.StaysTicks), "and then gone");

            // The newest first, and an outcome a teller's tip already tells is left out.
            _state.Raise("aftermath.loom_lane.girls_taken");
            _news.Notice(300);
            CollectionAssert.AreEqual(new[] { Loc.Get("news.b.taken.nell"), Loc.Get("news.a.sealed.nell") }, Told(TipSource.Bartender, 310));
            CollectionAssert.AreEqual(new[] { Loc.Get("news.b.taken.nell") }, Told(TipSource.Bartender, 310, key => key == "aftermath.harrow.shop_sealed"));

            // An outcome the board does not tell stays the people's.
            _state.Raise("aftermath.loom_lane.snatch_spoiled");
            _news.Notice(320);
            CollectionAssert.DoesNotContain(Told(TipSource.NoticeBoard, 330), Loc.Get("news.b.spoiled.nell"));
            CollectionAssert.Contains(Told(TipSource.StreetRumour, 330), Loc.Get("news.b.spoiled.kit"));
        }

        [Test]
        public void AnItemToldIsHeardOnceAndNotedInTheOrderThingsAreHeard()
        {
            var clock = new GameClock(1d, 8 * 60);
            var pois = new[] { "PoiA", "PoiB", "PoiC" }.Select(n => AssetDatabase.LoadAssetAtPath<PoiDefinition>("Assets/Data/Slice/Poi/" + n + ".asset")).ToArray();
            using var director = new PoiDirector(pois, _state, _seed, SliceSeedStreams.PoiUnattended, clock);
            var tips = new TipBook(AssetDatabase.LoadAssetAtPath<TipCatalogue>("Assets/Data/Slice/Poi/Tips.asset"), _state, director);
            var notes = new CharacterNotes(tips);
            _news.Place = tips.TakePlace;
            notes.Include(_news.Notes);
            var added = new List<string>();
            notes.Added += added.Add;
            _news.Heard += notes.Noted;

            var first = tips.Read(TipSource.NoticeBoard, clock.Tick).Where(t => t.HasNote).ToList();
            Assume.That(first, Is.Not.Empty, "the board says something on day 1 at 08:00");
            _state.Raise("aftermath.harrow.vanished");
            _news.Notice(clock.Tick);
            Told(TipSource.Bartender, clock.Tick);
            Told(TipSource.StreetRumour, clock.Tick);
            Assert.IsTrue(_news.HasHeard("a.gone"));
            Assert.AreEqual(1, added.Count(l => l == Loc.Get("news.a.gone.note")), "noted once, from whichever teller first");

            var death = _news.AddDeath(clock.Tick, Vector3.zero, NewsVictim.Warden, NewsKiller.Unseen);
            Told(TipSource.NoticeBoard, _news.Get(death).Enters);
            var lines = notes.Lines;
            Assert.AreEqual(Loc.Get("news.a.gone.note"), lines[first.Count], "after the board's tips heard before it");
            Assert.AreEqual(SliceNews.Note(_news.Get(death)), lines[first.Count + 1], "the death read on the board after it");
            StringAssert.Contains("day 1", lines[first.Count + 1]);
        }

        [Test]
        public void NellTellsTheNewsAfterHerTips()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SliceSettings>(SliceConfrontSetup.SettingsPath);
            var clock = new GameClock(1d, 18 * 60);
            var pois = new[] { "PoiA", "PoiB", "PoiC" }.Select(n => AssetDatabase.LoadAssetAtPath<PoiDefinition>("Assets/Data/Slice/Poi/" + n + ".asset")).ToArray();
            using var director = new PoiDirector(pois, _state, _seed, SliceSeedStreams.PoiUnattended, clock);
            using var standing = new SocialStanding(_state, settings.Standing, clock, _seed, SliceSeedStreams.Lies);
            var tips = new TipBook(AssetDatabase.LoadAssetAtPath<TipCatalogue>("Assets/Data/Slice/Poi/Tips.asset"), _state, director);
            _state.Raise("aftermath.harrow.defended");
            _news.Notice(clock.Tick);
            var context = new DialogueContext
            {
                State = _state,
                Clock = clock,
                Seed = _seed,
                CheckStream = SliceSeedStreams.SocialChecks,
                Rules = settings.SocialChecks,
                Standing = standing,
                Character = new CharacterSheet(),
                Director = director,
                Tips = tips,
                News = (source, said) => _news.Tell(source, clock.Tick, key => said.Any(t => t.requiresKey == key)),
            };

            var nell = new DialogueSession(AssetDatabase.LoadAssetAtPath<DialogueDefinition>("Assets/Data/Slice/Dialogue/NellDialogue.asset"), context);
            CollectionAssert.AreEqual(new[] { Loc.Get("news.a.defended.nell") }, nell.NewsSaid.Select(f => f()).ToList());
            Assert.IsTrue(_news.HasHeard("a.defended"));
            nell.Leave();
        }
    }
}

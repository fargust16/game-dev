using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Dialogue;
using GameDev.Player.Progression;
using GameDev.Player.Standing;
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
    /// S2.4: Nell's and Kit's conversations over the slice's own tips and POIs (SLICE_PEOPLE §2), and a check's tag by
    /// Intelligence (10.4 §3), on a clock driven by hand.
    /// </summary>
    public sealed class SliceConversationTests
    {
        private const string PoiFolder = "Assets/Data/Slice/Poi/";
        private const long Evening = 16 * 60;

        private WorldState _state;
        private GameClock _clock;
        private PoiDirector _director;
        private TipBook _tips;
        private SocialStanding _standing;
        private CharacterSheet _character;
        private DialogueContext _world;
        private PoiDefinition _poiB;
        private string _language;

        [SetUp]
        public void KeepLanguage()
        {
            _language = Loc.Language;
            Loc.Language = Loc.English;
        }

        [TearDown]
        public void Clean()
        {
            _standing?.Dispose();
            _director?.Dispose();
            _standing = null;
            _director = null;
            Loc.Language = _language;
        }

        /// <summary>A fresh world whose clock reads <paramref name="tick"/>.</summary>
        private void Begin(long tick = Evening)
        {
            _state = new WorldState();
            _clock = new GameClock(10d, tick);
            var seed = new WorldSeed(110201);
            var pois = new[] { "PoiA", "PoiB", "PoiC" }.Select(n => AssetDatabase.LoadAssetAtPath<PoiDefinition>(PoiFolder + n + ".asset")).ToArray();
            _poiB = pois[1];
            _director = new PoiDirector(pois, _state, seed, SliceSeedStreams.PoiUnattended, _clock);
            _director.Evaluate(_clock.Tick);
            _tips = new TipBook(AssetDatabase.LoadAssetAtPath<TipCatalogue>(PoiFolder + "Tips.asset"), _state, _director);
            _standing = new SocialStanding(_state, Rules<StandingRules>("StandingRules"), _clock, seed, SliceSeedStreams.Lies);
            _character = new CharacterSheet();
            _world = new DialogueContext
            {
                State = _state,
                Clock = _clock,
                Seed = seed,
                CheckStream = SliceSeedStreams.SocialChecks,
                Rules = Rules<SocialCheckRules>("SocialCheckRules"),
                Standing = _standing,
                Character = _character,
                Director = _director,
                Tips = _tips,
                Withheld = new HashSet<Approach> { Approach.Veil, Approach.Confront },
            };
        }

        [Test]
        public void ATipAskedForIsToldOnlyWhenItsSourceWouldSayIt()
        {
            Begin();
            var collectors = _tips.Find("nell.collectors");
            Assert.IsTrue(collectors.needsApproach);
            Assert.IsFalse(_tips.Available(TipSource.Bartender, _clock.Tick).Contains(collectors), "an Approach's tip is not said by itself");
            Assert.IsTrue(_tips.CanTell("nell.collectors", _clock.Tick));
            Assert.IsFalse(_tips.CanTell("nell.collectors", Evening - 1), "not before the evening");
            Assert.IsFalse(_tips.CanTell("nell.collectors", Evening + 1440), "nor on another day");
            Assert.IsNull(_tips.Tell("nell.collectors", Evening - 1));

            Assert.AreSame(collectors, _tips.Tell("nell.collectors", _clock.Tick));
            Assert.IsTrue(_tips.HasHeard(collectors));
            Assert.AreEqual(PoiDiscovery.Marked, _director.Discovery(_poiB), "Nell's tip marks POI B");
            Assert.IsFalse(_tips.CanTell("nell.collectors", _clock.Tick), "a heard tip is not told again");
            Assert.IsNull(_tips.Tell("nell.collectors", _clock.Tick));
        }

        [Test]
        public void NellSaysHerEveningTipAndOffersHerLoomLaneTipForAnApproach()
        {
            Begin();
            var nell = Talk("NellDialogue");

            CollectionAssert.AreEqual(new[] { "nell.harrow" }, nell.Said.Select(t => t.id), "her evening tip is said on arriving");
            CollectionAssert.AreEqual(new[] { "threaten", "exchange", "leave" }, nell.Options.Select(o => o.id),
                "Threaten has its Argument, the tip she just told; Convince has none yet");
            nell.Leave();

            _tips.Read(TipSource.NoticeBoard, _clock.Tick);
            var again = Talk("NellDialogue");
            CollectionAssert.AreEqual(new[] { "convince", "threaten", "exchange", "lie", "leave" }, again.Options.Select(o => o.id),
                "the board's missing girls are an Argument for Convince, and the Wren notice for a lie (S2.7)");
            again.Leave();
        }

        [Test]
        public void ExchangeTellsTheLoomLaneTipForAnHoursWorkAndMarksPoiB()
        {
            Begin();
            var nell = Talk("NellDialogue");
            var before = _clock.Tick;
            nell.Choose(nell.Options.Single(o => o.id == "exchange"));

            Assert.AreEqual("told", nell.Node.id);
            CollectionAssert.AreEqual(new[] { "nell.collectors" }, nell.Said.Select(t => t.id), "the tip takes the place of the node's line");
            Assert.AreEqual(PoiDiscovery.Marked, _director.Discovery(_poiB));
            Assert.AreEqual(before + 60, _clock.Tick, "the service takes an hour of the world's time");
            Assert.IsTrue(_clock.IsPaused, "the conversation still holds the world");

            nell.Choose(nell.Options.Single(o => o.id == "thanks"));
            Assert.AreEqual(DialogueEnd.Finished, nell.EndedHow);
            Assert.AreEqual(before + 61, _clock.Tick, "and one tick when it ends");

            var later = Talk("NellDialogue");
            CollectionAssert.AreEqual(new[] { "leave" }, later.Options.Select(o => o.id), "a tip told is not asked for again");
            later.Leave();
        }

        [Test]
        public void BeforeTheEveningNellHasNothingToSayAndNothingToAskFor()
        {
            Begin(9 * 60);
            var nell = Talk("NellDialogue");

            CollectionAssert.IsEmpty(nell.Said);
            Assert.AreEqual("evening", nell.Node.id, "her line is said instead: come back of an evening");
            CollectionAssert.AreEqual(new[] { "leave" }, nell.Options.Select(o => o.id));
            nell.Leave();
        }

        [Test]
        public void KitSaysTheDaysRumours()
        {
            Begin(8 * 60);
            var kit = Talk("KitDialogue");

            CollectionAssert.AreEqual(new[] { "kit.drawers", "kit.coachcoats" }, kit.Said.Select(t => t.id));
            Assert.AreEqual(PoiDiscovery.Hinted, _director.Discovery(_poiB), "one Hinted source only hints B");
            kit.Choose(kit.Options.Single(o => o.id == "leave"));
            Assert.IsFalse(kit.IsOpen);
        }

        [Test]
        public void ACheckShowsItsChanceFromIntelligenceTenAndItsModifiersFromTwenty()
        {
            Begin();
            _tips.Read(TipSource.NoticeBoard, _clock.Tick);
            var nell = Talk("NellDialogue");
            var convince = nell.Options.Single(o => o.id == "convince");
            var preview = nell.Preview(convince);
            var rules = _world.Rules;

            // Nell's Listener Value is 1, so a Small request is 6 against a Character Value of 0; the board's two notices
            // are two Arguments, the second adding +1: 2d6 of 5 or more, 30 throws in 36, times Luck 1's 1.01.
            Assert.AreEqual(1, preview.Arguments);
            Assert.AreEqual(30d / 36d * 1.01d, preview.Chance, 1e-9);
            Assert.AreEqual("[Persuasion]", PersonTalk.CheckTag(rules, convince.skill, preview, 9));
            Assert.AreEqual("[Persuasion: 84 %]", PersonTalk.CheckTag(rules, convince.skill, preview, 10));
            Assert.AreEqual("[Persuasion: 84 %] (Attitude 0, Reputation 0, Arguments +1)", PersonTalk.CheckTag(rules, convince.skill, preview, 20));
            nell.Leave();
        }

        [Test]
        public void EveryTipAConversationNamesIsInTheCatalogue()
        {
            var catalogue = AssetDatabase.LoadAssetAtPath<TipCatalogue>(PoiFolder + "Tips.asset");
            foreach (var guid in AssetDatabase.FindAssets("t:DialogueDefinition"))
            {
                var dialogue = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var option in dialogue.Nodes.SelectMany(n => n.options))
                {
                    var named = option.success.tells.Concat(option.failure.tells).Append(option.asks).Where(id => !string.IsNullOrEmpty(id));
                    foreach (var id in named)
                    {
                        Assert.IsTrue(catalogue.Tips.Any(t => t.id == id), $"{dialogue.Id}.{option.id}: no tip '{id}'");
                    }
                }
            }
        }

        private IDialogueRunner Talk(string name)
        {
            var dialogue = AssetDatabase.LoadAssetAtPath<DialogueDefinition>($"Assets/Data/Slice/Dialogue/{name}.asset");
            Assert.IsNotNull(dialogue, name);
            return new DialogueSession(dialogue, _world);
        }

        private static T Rules<T>(string name) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>($"Assets/Data/Progression/{name}.asset");
            Assert.IsNotNull(asset, name);
            return asset;
        }
    }
}

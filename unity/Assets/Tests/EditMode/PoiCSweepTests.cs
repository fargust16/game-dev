using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Dialogue;
using GameDev.Player.Progression;
using GameDev.Player.Standing;
using GameDev.Slice.EditorTools;
using GameDev.World.Poi;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S4.2: POI C's sweep on Deaf Boiler Yard, a Witness (11.1 *Slice build (S4)*): each way his conversation goes,
    /// his hours and his leaving. The checks' dice are fixed, so each test reads one way and not its odds.
    /// </summary>
    public sealed class PoiCSweepTests
    {
        private const string PoiFolder = "Assets/Data/Slice/Poi/";
        private const long Afternoon = 14 * 60;
        private const double Succeeds = 0d;
        private const double Fails = 0.9999d;

        private WorldState _state;
        private GameClock _clock;
        private PoiDirector _director;
        private TipBook _tips;
        private SocialStanding _standing;
        private DialogueContext _world;
        private PoiDefinition _c;
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

        /// <summary>A world at 14:00 of day 1 where the character settled POI A, so C is open; its checks all roll <paramref name="roll"/>.</summary>
        private void Begin(double roll = Succeeds)
        {
            _state = new WorldState();
            _clock = new GameClock(1d, Afternoon);
            var seed = new WorldSeed(110201);
            var pois = new[] { "PoiA", "PoiB", "PoiC" }.Select(n => AssetDatabase.LoadAssetAtPath<PoiDefinition>(PoiFolder + n + ".asset")).ToArray();
            _c = pois[2];
            _director = new PoiDirector(pois, _state, seed, SliceSeedStreams.PoiUnattended, _clock);
            _director.Evaluate(_clock.Tick);
            _director.ResolveParticipated(pois[0]);
            _tips = new TipBook(AssetDatabase.LoadAssetAtPath<TipCatalogue>(PoiFolder + "Tips.asset"), _state, _director);
            _standing = new SocialStanding(_state, Rules<StandingRules>("StandingRules"), _clock, seed, SliceSeedStreams.Lies);
            _world = new DialogueContext
            {
                State = _state,
                Clock = _clock,
                Seed = new FixedDice(roll),
                CheckStream = SliceSeedStreams.SocialChecks,
                Rules = Rules<SocialCheckRules>("SocialCheckRules"),
                Standing = _standing,
                Character = new CharacterSheet(),
                Director = _director,
                Tips = _tips,
                Withheld = new HashSet<Approach>(),
            };
            Assert.AreEqual(PoiPhase.Open, _director.Phase(_c), "C opens once A has resolved");
        }

        [Test]
        public void TheConversationIsWholeAndItsTipAWitnessThatMarksC()
        {
            var dialogue = Dialogue();
            CollectionAssert.IsEmpty(dialogue.Problems());
            foreach (var key in dialogue.Keys())
            {
                Assert.IsTrue(Loc.Tables.Has(key, "en") && Loc.Tables.Has(key, "ru"), $"no words for {key}");
            }

            var rules = Rules<SocialCheckRules>("SocialCheckRules");
            var sweep = dialogue.Listener("sweep").Sheet();
            Assert.AreEqual(0, SocialCheck.ListenerValue(rules, sweep, CharacterSkill.Persuasion), "11.1: his Listener Value 0");
            Assert.AreEqual(0, SocialCheck.ListenerValue(rules, sweep, CharacterSkill.Intimidation));

            var tip = AssetDatabase.LoadAssetAtPath<TipCatalogue>(PoiFolder + "Tips.asset").Tips.Single(t => t.id == "sweep.coach");
            Assert.AreEqual(TipSource.Witness, tip.source);
            Assert.AreEqual(TipEffect.Marks, tip.effect);
            Assert.IsTrue(tip.needsApproach, "he says it only when asked");
            Assert.IsTrue(Loc.Tables.Has(tip.LineKey, "ru") && Loc.Tables.Has(tip.NoteKey, "ru"));
        }

        [Test]
        public void WithNothingFoundOnlyTheServiceIsOffered()
        {
            Begin();
            var talk = Talk();
            CollectionAssert.AreEqual(new[] { "exchange", "leave" }, Ids(talk), "a check needs an Argument (10.4 §1)");
            talk.Leave();
        }

        [Test]
        public void AnyTellOrTipOfTheYardIsAnArgument()
        {
            foreach (var heard in new[] { "trace.wax", "nell.chalk", "kit.glove" })
            {
                Begin();
                _state.Set("tip." + heard + ".heard", 1);
                var talk = Talk();
                CollectionAssert.AreEqual(new[] { "convince", "threaten", "exchange", "leave" }, Ids(talk), heard);
                talk.Leave();
                Clean();
            }
        }

        [Test]
        public void ConvincingHimTellsTheCrestlessCoach()
        {
            Begin();
            Assert.IsNotNull(_tips.Tell("trace.chalk", _clock.Tick));
            var notes = new CharacterNotes(_tips);
            var talk = Talk();

            Assert.AreEqual(CheckResult.Success, talk.Choose(Option(talk, "convince")));

            Assert.AreEqual("told", talk.Node.id);
            Assert.AreEqual("sweep.coach", talk.Said.Single().id, "the tip is said in place of the node's line");
            Assert.AreEqual(Loc.Get("tip.sweep.coach.note"), notes.Lines.Last());
            Assert.IsFalse(_state.IsSet(SlicePoiC.SweepGone), "he stays");
            talk.Choose(Option(talk, "thanks"));

            var again = Talk();
            CollectionAssert.AreEqual(new[] { "leave" }, Ids(again), "told once, he has nothing more");
            again.Leave();
        }

        [Test]
        public void AFailedConvinceIsSilenceAndNotTriedAgain()
        {
            Begin(Fails);
            _tips.Tell("trace.chalk", _clock.Tick);
            var talk = Talk();

            Assert.AreEqual(CheckResult.Failure, talk.Choose(Option(talk, "convince")));

            Assert.AreEqual("shrug", talk.Node.id);
            Assert.IsFalse(_tips.HasHeard(_tips.Find("sweep.coach")), "silence: the trace on the ground stays (11.1)");
            talk.Choose(Option(talk, "go"));
            CollectionAssert.AreEqual(new[] { "threaten", "exchange", "leave" }, Ids(Talk()), "a failed check is not tried again");
        }

        [Test]
        public void AThreatThatWorksTellsButSendsHimOffForGood()
        {
            Begin();
            _tips.Tell("trace.chalk", _clock.Tick);
            var talk = Talk();

            Assert.AreEqual(CheckResult.Success, talk.Choose(Option(talk, "threaten")));

            Assert.AreEqual("scared", talk.Node.id);
            Assert.IsTrue(_tips.HasHeard(_tips.Find("sweep.coach")));
            Assert.IsTrue(_state.IsSet(SlicePoiC.SweepGone), "11.1: if Threaten, the sweep vanishes from future tips");
            Assert.Less(_standing.Attitude("sweep"), 0, "a threat costs Attitude (10.4 §5)");
        }

        [Test]
        public void AFailedThreatSendsHimOffWithNothingSaid()
        {
            Begin(Fails);
            _tips.Tell("trace.chalk", _clock.Tick);
            var talk = Talk();

            Assert.AreEqual(CheckResult.Failure, talk.Choose(Option(talk, "threaten")));

            Assert.AreEqual("gone", talk.Node.id);
            Assert.IsFalse(_tips.HasHeard(_tips.Find("sweep.coach")));
            Assert.IsTrue(_state.IsSet(SlicePoiC.SweepGone));
        }

        [Test]
        public void TheServiceTakesHalfAnHourAndTellsTheCoach()
        {
            Begin();
            var talk = Talk();
            var before = _clock.Tick;

            Assert.AreEqual(CheckResult.None, talk.Choose(Option(talk, "exchange")));

            Assert.AreEqual(before + 30, _clock.Tick, "the rods up the works' flue, 30 minutes");
            Assert.AreEqual("told", talk.Node.id);
            Assert.IsTrue(_tips.HasHeard(_tips.Find("sweep.coach")));
            Assert.AreEqual(PoiDiscovery.Marked, _director.Discovery(_c), "a Witness marks C");
        }

        [Test]
        public void HeIsOnTheYardByDayUntilAThreat()
        {
            var state = new WorldState();
            bool At(long tick) => PoiStage.Present(state, tick, SlicePoiC.SweepFrom, SlicePoiC.SweepUntil, SlicePoiC.SweepGone);

            Assert.IsFalse(At((7 * 60) + 59));
            Assert.IsTrue(At(8 * 60));
            Assert.IsTrue(At(1440 + (17 * 60) + 59), "on day 2 as on day 1");
            Assert.IsFalse(At(1440 + (18 * 60)), "gone at 18:00");
            Assert.IsTrue(PoiStage.Present(state, 3 * 60, -1, -1, ""), "a scene with no hours keeps any hour");

            state.Raise(SlicePoiC.SweepGone);
            Assert.IsFalse(At(12 * 60), "sent off for good");
        }

        [Test]
        public void TheSweepStandsOnTheYardClearOfTheTraces()
        {
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var stage = Object.FindObjectsByType<PoiStage>(FindObjectsInactive.Include).Single(s => s.name == SlicePoiC.SweepName);
            var c = AssetDatabase.LoadAssetAtPath<PoiDefinition>(PoiFolder + "PoiC.asset");

            Assert.AreEqual(c, stage.Poi);
            Assert.AreEqual(SlicePoiC.SweepFrom, stage.FromMinute);
            Assert.AreEqual(SlicePoiC.SweepUntil, stage.UntilMinute);
            Assert.AreEqual(SlicePoiC.SweepGone, stage.GoneKey);
            Assert.AreEqual(Dialogue(), stage.GetComponent<PersonTalk>().Dialogue);
            Assert.IsFalse(stage.Cast.activeSelf, "hidden until the player comes while he is there");
            Assert.Less(SlicePoiC.FlatDistance(stage.transform.position, c.Place), 12f, "on the yard before the cellar door");
            foreach (var trace in Object.FindObjectsByType<TraceSpot>(FindObjectsInactive.Include))
            {
                Assert.GreaterOrEqual(SlicePoiC.FlatDistance(stage.transform.position, trace.transform.position), SlicePoiC.SweepClear, trace.name);
            }
        }

        private static DialogueDefinition Dialogue()
        {
            var dialogue = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(SlicePoiC.SweepDialoguePath);
            Assert.IsNotNull(dialogue);
            return dialogue;
        }

        private IDialogueRunner Talk() => new DialogueSession(Dialogue(), _world);

        private static DialogueOption Option(IDialogueRunner talk, string id)
        {
            var option = talk.Options.SingleOrDefault(o => o.id == id);
            Assert.IsNotNull(option, $"'{id}' is not offered at '{talk.Node?.id}': {string.Join(", ", Ids(talk))}");
            return option;
        }

        private static List<string> Ids(IDialogueRunner talk) => talk.Options.Select(o => o.id).ToList();

        private static T Rules<T>(string name) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>($"Assets/Data/Progression/{name}.asset");
            Assert.IsNotNull(asset, name);
            return asset;
        }

        /// <summary>Dice that always roll the same, so a check's way is chosen by the test, not by its odds.</summary>
        private sealed class FixedDice : IWorldSeed
        {
            private readonly double _roll;

            public FixedDice(double roll) => _roll = roll;

            public int Value => 0;

            public Random Fork(string streamId) => new Fixed(_roll);

            public Random Fork(string streamId, int a, int b = 0) => new Fixed(_roll);

            private sealed class Fixed : Random
            {
                private readonly double _roll;

                public Fixed(double roll) => _roll = roll;

                public override double NextDouble() => _roll;

                protected override double Sample() => _roll;
            }
        }
    }
}

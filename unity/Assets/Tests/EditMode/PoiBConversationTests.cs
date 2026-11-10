using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Decisions;
using GameDev.Dialogue;
using GameDev.Player.Progression;
using GameDev.Player.Standing;
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
    /// S2.6's EditMode done-when (VERTICAL_SLICE_IMPLEMENTATION_PLAN): POI B's card against its conversation, and each
    /// option's outcome and residue (11.1 POI B; 11 § Side events and the invasion), at night on a clock driven by hand.
    /// The collectors are Veinmarket's men, so a check on them is Final (10.4 §6). The checks' dice are fixed, so each test
    /// reads one way through the scene and not its odds.
    /// </summary>
    public sealed class PoiBConversationTests
    {
        private const string PoiFolder = "Assets/Data/Slice/Poi/";
        private const long Morning = 8 * 60;
        private const long Night = (23 * 60) + 30;
        private const long WindowEnds = 26 * 60;
        private const double Succeeds = 0d;
        private const double Fails = 0.9999d;

        private static readonly string[] Protected = { "aftermath.loom_lane.girls_freed", "against.veinmarket", "poi.b.heard.harrow_buyers" };
        private static readonly string[] Fetched = { "aftermath.loom_lane.snatch_spoiled", "aftermath.loom_lane.wardens_fetched", "debt.wardens" };
        private static readonly string[] Fled = { "aftermath.loom_lane.girls_taken", "poi.b.fled" };

        private WorldState _state;
        private GameClock _clock;
        private PoiDirector _director;
        private TipBook _tips;
        private SocialStanding _standing;
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

        /// <summary>A fresh world run from 08:00 to 23:30, so POI B opened at the eleventh bell, whose checks all roll <paramref name="roll"/>.</summary>
        private void Begin(double roll = Succeeds)
        {
            _state = new WorldState();
            _clock = new GameClock(10d, Morning);
            var seed = new WorldSeed(110201);
            var pois = new[] { "PoiA", "PoiB", "PoiC" }.Select(n => AssetDatabase.LoadAssetAtPath<PoiDefinition>(PoiFolder + n + ".asset")).ToArray();
            _poiB = pois[1];
            _director = new PoiDirector(pois, _state, seed, SliceSeedStreams.PoiUnattended, _clock);
            _director.Evaluate(_clock.Tick);
            _clock.AdvanceTicks((int)(Night - Morning));
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
                Withheld = new HashSet<Approach> { Approach.Veil, Approach.Confront },
            };
            Assert.AreEqual(PoiPhase.Open, _director.Phase(_poiB), "POI B's window opens at 23:00");
        }

        [Test]
        public void TheCardIsWholeAndMatchesItsConversation()
        {
            var node = AssetDatabase.LoadAssetAtPath<DecisionNodeDefinition>("Assets/Data/Slice/Decisions/PoiBNode.asset");
            Assert.IsNotNull(node);
            CollectionAssert.IsEmpty(node.Problems());
            CollectionAssert.IsEmpty(node.Scene.Problems());
            Assert.AreEqual("b", node.Poi.Id);
            Assert.AreEqual("poib", node.Scene.Id);
            Assert.AreEqual(DecisionAxis.Allegiance, node.PrimaryAxis);
            Assert.AreEqual(DecisionAxis.Approach, node.SecondaryAxis);
            CollectionAssert.AreEqual(new[] { "Protect the victims now", "Follow the coach instead", "Signal or fetch Wardens", "Exchange for a tip before moving", "Leave Draught Lane" },
                node.Options.Select(o => o.name), "11.1's five options");
            CollectionAssert.AreEqual(new[] { "S3" }, node.Options.Where(o => o.dialogueOptions.Length == 0).Select(o => o.later), "following the coach waits for S3");
            Assert.IsTrue(node.Options.Single(o => o.name == "Exchange for a tip before moving").prepares, "the tip settles nothing by itself");

            var choices = node.Options.SelectMany(o => o.dialogueOptions).ToList();
            var withApproach = node.Scene.Nodes.SelectMany(n => n.options).Where(o => o.approach != Approach.None).Select(o => o.id);
            CollectionAssert.AreEquivalent(withApproach, choices, "every Approach in the conversation is one of the card's options");
            Assert.IsTrue(node.Scene.Listeners.Single(l => l.id == "collectors").enemy, "the collectors are Veinmarket's men");
            foreach (var key in node.Scene.Keys())
            {
                Assert.IsTrue(Loc.Exists(key), $"no words for {key}");
            }
        }

        [Test]
        public void WithNothingHeardTheLaneOffersTheDoorwayTheWardensAndLeaving()
        {
            Begin();
            var scene = Talk();
            scene.Choose(Option(scene, "step"));

            Assert.AreEqual("lane", scene.Node.id);
            CollectionAssert.AreEqual(new[] { "ask_crone", "run", "leave" }, Ids(scene), "a check needs an Argument (10.4 §1)");
            scene.Leave();
        }

        [Test]
        public void TheLanesPeopleSwarmByConvinceAndTheGirlsGoFree()
        {
            Begin();
            _tips.Read(TipSource.NoticeBoard, _clock.Tick);
            Assert.IsTrue(_state.IsSet("tip.board.girls.heard"), "the board's notice of girls gone missing");
            var scene = Lane();
            CollectionAssert.Contains(Ids(scene), "swarm");

            Assert.AreEqual(CheckResult.Success, scene.Choose(Option(scene, "swarm")));

            Assert.AreEqual("freed", scene.Node.id);
            AssertSettled(Protected, Fetched.Concat(Fled));
            scene.Choose(Option(scene, "home"));
            Assert.AreEqual(DialogueEnd.Finished, scene.EndedHow);
        }

        [Test]
        public void AFailedSwarmShutsTheDoorsAndTheOtherWaysStay()
        {
            Begin(Fails);
            _tips.Read(TipSource.NoticeBoard, _clock.Tick);
            var scene = Lane();

            Assert.AreEqual(CheckResult.Failure, scene.Choose(Option(scene, "swarm")));

            Assert.AreEqual("shut", scene.Node.id);
            Assert.AreEqual(PoiPhase.Open, _director.Phase(_poiB), "the lane's people are not the enemy: no Final Check");
            scene.Choose(Option(scene, "turn"));
            CollectionAssert.AreEqual(new[] { "ask_crone", "run", "leave" }, Ids(scene), "a failed check is not tried again");
            scene.Leave();
        }

        [Test]
        public void ThreateningTheCollectorsFreesTheGirls()
        {
            Begin();
            _tips.Read(TipSource.StreetRumour, _clock.Tick);
            Assert.IsTrue(_state.IsSet("tip.kit.coachcoats.heard"), "Kit's line about the coach-coats");
            var scene = Lane();

            Assert.AreEqual(CheckResult.Success, scene.Choose(Option(scene, "threaten")));

            Assert.AreEqual("freed", scene.Node.id);
            AssertSettled(Protected, Fetched.Concat(Fled));
            scene.Leave();
        }

        [Test]
        public void AFailedThreatIsAFinalCheckAndTheCollectorsTakeTheGirls()
        {
            Begin(Fails);
            _tips.Read(TipSource.StreetRumour, _clock.Tick);
            var scene = Lane();

            Assert.AreEqual(CheckResult.Failure, scene.Choose(Option(scene, "threaten")));

            Assert.IsFalse(scene.IsOpen);
            Assert.AreEqual(DialogueEnd.Fight, scene.EndedHow, "a failed Final Check ends in a fight (10.4 §6)");
            AssertSettled(Fled, Protected.Concat(Fetched));
        }

        [Test]
        public void TheOldWomansTipIsAPromiseThatGivesAnArgumentAndSettlesNothing()
        {
            Begin();
            var scene = Lane();
            scene.Choose(Option(scene, "ask_crone"));
            Assert.AreEqual("crone", scene.Node.id);

            Assert.AreEqual(CheckResult.None, scene.Choose(Option(scene, "promise")));

            Assert.AreEqual("stair", scene.Node.id);
            Assert.IsTrue(_state.IsSet("poi.b.heard.stair"), "the coach waits by the coal stair");
            Assert.IsTrue(_state.IsSet("promise.crone"), "a promise, as paid Exchange waits for S5a");
            Assert.AreEqual(PoiPhase.Open, _director.Phase(_poiB));
            scene.Choose(Option(scene, "onward"));
            CollectionAssert.AreEqual(new[] { "threaten", "run", "leave" }, Ids(scene), "asked once; what she said is an Argument against the collectors");
            scene.Leave();
        }

        [Test]
        public void UrgingAWardenByConvinceFetchesThemAndPutsThemInDebt()
        {
            Begin();
            _tips.Read(TipSource.NoticeBoard, _clock.Tick);
            var scene = Warden();
            CollectionAssert.AreEqual(new[] { "urge", "promise_tip", "back" }, Ids(scene));
            var before = _clock.Tick;

            Assert.AreEqual(CheckResult.Success, scene.Choose(Option(scene, "urge")));

            Assert.AreEqual("whistle", scene.Node.id);
            AssertSettled(Fetched, Protected.Concat(Fled));
            scene.Choose(Option(scene, "breathe"));
            Assert.GreaterOrEqual(_clock.Tick, before + 5, "running for the Warden takes five minutes");
        }

        [Test]
        public void AShruggingWardenLeavesTheTipPromiseOpen()
        {
            Begin(Fails);
            _tips.Read(TipSource.NoticeBoard, _clock.Tick);
            var scene = Warden();

            Assert.AreEqual(CheckResult.Failure, scene.Choose(Option(scene, "urge")));

            Assert.AreEqual("shrugged", scene.Node.id);
            Assert.AreEqual(PoiPhase.Open, _director.Phase(_poiB), "a Warden is no enemy: no Final Check");
            scene.Choose(Option(scene, "away"));
            CollectionAssert.AreEqual(new[] { "promise_tip", "back" }, Ids(scene));
            scene.Leave();
        }

        [Test]
        public void PromisingAWardenATipFetchesThemWithoutACheck()
        {
            Begin();
            var scene = Warden();
            CollectionAssert.AreEqual(new[] { "promise_tip", "back" }, Ids(scene), "nothing heard: no Argument to urge them with");

            Assert.AreEqual(CheckResult.None, scene.Choose(Option(scene, "promise_tip")));

            Assert.AreEqual("whistle", scene.Node.id);
            AssertSettled(Fetched, Protected.Concat(Fled));
            Assert.IsTrue(_state.IsSet("promise.wardens.tip"), "the Wardens are owed a tip");
            scene.Leave();
        }

        [Test]
        public void LeavingLetsBResolveUnattendedAtTwo()
        {
            Begin();
            var scene = Lane();
            scene.Choose(Option(scene, "leave"));

            Assert.IsFalse(scene.IsOpen);
            Assert.AreEqual(PoiPhase.Open, _director.Phase(_poiB), "the world goes on without the player");
            _clock.AdvanceTicks((int)(WindowEnds - _clock.Tick));

            Assert.AreEqual(PoiResolution.Unattended, _director.Resolution(_poiB), "the window closes at 02:00");
            CollectionAssert.Contains(new[] { "wardens", "veinmarket" }, _director.Outcome(_poiB));
            Assert.IsFalse(new[] { "debt.wardens", "against.veinmarket", "poi.b.fled" }.Any(_state.IsSet), "an unattended outcome puts nobody in debt (11)");
        }

        private void AssertSettled(IEnumerable<string> raised, IEnumerable<string> notRaised)
        {
            Assert.AreEqual(PoiPhase.Resolved, _director.Phase(_poiB));
            Assert.AreEqual(PoiResolution.Participated, _director.Resolution(_poiB));
            Assert.IsNull(_director.Outcome(_poiB), "no unattended outcome is drawn");
            var keys = raised.ToList();
            foreach (var key in keys)
            {
                Assert.IsTrue(_state.IsSet(key), key);
            }

            foreach (var key in notRaised.Where(k => !keys.Contains(k)))
            {
                Assert.IsFalse(_state.IsSet(key), key);
            }
        }

        private IDialogueRunner Talk()
        {
            var dialogue = AssetDatabase.LoadAssetAtPath<DialogueDefinition>("Assets/Data/Slice/Dialogue/PoiBDialogue.asset");
            Assert.IsNotNull(dialogue);
            return new DialogueSession(dialogue, _world);
        }

        /// <summary>The conversation, past its opening, before the collectors.</summary>
        private IDialogueRunner Lane()
        {
            var scene = Talk();
            scene.Choose(Option(scene, "step"));
            return scene;
        }

        /// <summary>The conversation with the Warden the player ran to.</summary>
        private IDialogueRunner Warden()
        {
            var scene = Lane();
            scene.Choose(Option(scene, "run"));
            Assert.AreEqual("warden", scene.Node.id);
            return scene;
        }

        private static DialogueOption Option(IDialogueRunner scene, string id)
        {
            var option = scene.Options.SingleOrDefault(o => o.id == id);
            Assert.IsNotNull(option, $"'{id}' is not offered at '{scene.Node?.id}': {string.Join(", ", Ids(scene))}");
            return option;
        }

        private static List<string> Ids(IDialogueRunner scene) => scene.Options.Select(o => o.id).ToList();

        private static T Rules<T>(string name) where T : UnityEngine.Object
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

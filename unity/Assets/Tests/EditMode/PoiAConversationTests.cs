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
    /// S2.5's EditMode done-when (VERTICAL_SLICE_IMPLEMENTATION_PLAN): POI A's card against its conversation, and each
    /// option's outcome and residue (11.1 POI A; 11 § Side events and the invasion), on a clock driven by hand. The
    /// checks' dice are fixed, so each test reads one way through the scene and not its odds.
    /// </summary>
    public sealed class PoiAConversationTests
    {
        private const string PoiFolder = "Assets/Data/Slice/Poi/";
        private const long Morning = 8 * 60;
        private const long Afternoon = 15 * 60;
        private const double Succeeds = 0d;
        private const double Fails = 0.9999d;

        private static readonly string[] Defended = { "aftermath.harrow.defended", "debt.veinmarket", "debt.harrow" };
        private static readonly string[] Taken = { "aftermath.harrow.taken", "aftermath.harrow.shop_sealed", "debt.wardens" };

        private WorldState _state;
        private GameClock _clock;
        private PoiDirector _director;
        private TipBook _tips;
        private SocialStanding _standing;
        private DialogueContext _world;
        private PoiDefinition _poiA;
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

        /// <summary>A fresh world run from 08:00 to 15:00, so POI A opened at 14:00, whose checks all roll <paramref name="roll"/>.</summary>
        private void Begin(double roll = Succeeds)
        {
            _state = new WorldState();
            _clock = new GameClock(10d, Morning);
            var seed = new WorldSeed(110201);
            var pois = new[] { "PoiA", "PoiB", "PoiC" }.Select(n => AssetDatabase.LoadAssetAtPath<PoiDefinition>(PoiFolder + n + ".asset")).ToArray();
            _poiA = pois[0];
            _director = new PoiDirector(pois, _state, seed, SliceSeedStreams.PoiUnattended, _clock);
            _director.Evaluate(_clock.Tick);
            _clock.AdvanceTicks((int)(Afternoon - Morning));
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
            Assert.AreEqual(PoiPhase.Open, _director.Phase(_poiA), "POI A's window opens at 14:00");
        }

        [Test]
        public void TheCardIsWholeAndMatchesItsConversation()
        {
            var node = AssetDatabase.LoadAssetAtPath<DecisionNodeDefinition>("Assets/Data/Slice/Decisions/PoiANode.asset");
            Assert.IsNotNull(node);
            CollectionAssert.IsEmpty(node.Problems());
            CollectionAssert.IsEmpty(node.Scene.Problems());
            Assert.AreEqual("a", node.Poi.Id);
            Assert.AreEqual("poia", node.Scene.Id);
            Assert.AreEqual(DecisionAxis.Allegiance, node.PrimaryAxis);
            Assert.AreEqual(DecisionAxis.Approach, node.SecondaryAxis);
            CollectionAssert.AreEqual(new[] { "Defend Harrow", "Help the Wardens seize him", "Veil and listen first", "Buy a minute", "Walk away before the whistle" },
                node.Options.Select(o => o.name), "11.1's five options");
            CollectionAssert.AreEqual(new[] { "S3", "S5a" }, node.Options.Where(o => o.dialogueOptions.Length == 0).Select(o => o.later),
                "Veil waits for S3, a paid Exchange for S5a");

            var choices = node.Options.SelectMany(o => o.dialogueOptions).ToList();
            var withApproach = node.Scene.Nodes.SelectMany(n => n.options).Where(o => o.approach != Approach.None).Select(o => o.id);
            CollectionAssert.AreEquivalent(withApproach, choices, "every Approach in the conversation is one of the card's options");
            foreach (var key in node.Scene.Keys())
            {
                Assert.IsTrue(Loc.Exists(key), $"no words for {key}");
            }
        }

        [Test]
        public void WithNothingHeardOnlyTheServiceAndWalkingAwayAreChoices()
        {
            Begin();
            var scene = Talk();
            CollectionAssert.AreEqual(new[] { "step" }, Ids(scene));
            scene.Choose(Option(scene, "step"));

            Assert.AreEqual("street", scene.Node.id);
            CollectionAssert.AreEqual(new[] { "ask", "hold", "walk" }, Ids(scene), "a check needs an Argument (10.4 §1)");
            scene.Leave();
        }

        [Test]
        public void AskingTheNeighboursGivesAnArgumentForDefendingHim()
        {
            Begin();
            var scene = Street();
            scene.Choose(Option(scene, "ask"));

            Assert.AreEqual("neighbours", scene.Node.id);
            Assert.IsTrue(_state.IsSet("poi.a.heard.neighbours"));
            scene.Choose(Option(scene, "back"));
            CollectionAssert.AreEqual(new[] { "defend", "threaten", "hold", "walk" }, Ids(scene), "asked once");
            scene.Leave();
        }

        [Test]
        public void DefendingHimByConvinceSettlesAAndPutsVeinmarketInDebt()
        {
            Begin();
            var scene = Street();
            scene.Choose(Option(scene, "ask"));
            scene.Choose(Option(scene, "back"));

            Assert.AreEqual(CheckResult.Success, scene.Choose(Option(scene, "defend")));

            Assert.AreEqual("freed", scene.Node.id);
            AssertSettled(Defended, Taken);
            scene.Choose(Option(scene, "done"));
            Assert.AreEqual(DialogueEnd.Finished, scene.EndedHow);
        }

        [Test]
        public void ThreateningTheWardensDefendsHimAndCostsTheirAttitude()
        {
            Begin();
            _tips.Read(TipSource.StreetRumour, _clock.Tick);
            Assert.IsTrue(_state.IsSet("tip.kit.drawers.heard"), "Kit's line: they want the drawers more than the man");
            var scene = Street();
            CollectionAssert.Contains(Ids(scene), "threaten", "Kit's line alone is an Argument");

            Assert.AreEqual(CheckResult.Success, scene.Choose(Option(scene, "threaten")));

            AssertSettled(Defended, Taken);
            Assert.Less(_standing.Attitude("wardens"), 0, "a threat costs Attitude either way (10.4 §5)");
            scene.Leave();
        }

        [Test]
        public void AFailedDefenceTeachesTheCabinetAndLeavesTheOtherWays()
        {
            Begin(Fails);
            var scene = Street();
            scene.Choose(Option(scene, "ask"));
            scene.Choose(Option(scene, "back"));

            Assert.AreEqual(CheckResult.Failure, scene.Choose(Option(scene, "defend")));

            Assert.AreEqual("rebuffed", scene.Node.id);
            Assert.IsTrue(_state.IsSet("poi.a.heard.cabinet"), "the cabinet mattered more than the man (11.1)");
            Assert.AreEqual(PoiPhase.Open, _director.Phase(_poiA));
            scene.Choose(Option(scene, "retreat"));
            CollectionAssert.AreEqual(new[] { "threaten", "hold", "walk" }, Ids(scene), "a failed check is not tried again");
            scene.Leave();
        }

        [Test]
        public void TestifyingByConvinceSealsTheShopAndPutsTheWardensInDebt()
        {
            Begin();
            _tips.Read(TipSource.NoticeBoard, _clock.Tick);
            Assert.IsTrue(_state.IsSet("tip.board.seizure.heard"), "the seizure notice, pinned at 14:00");
            var scene = Street();

            Assert.AreEqual(CheckResult.Success, scene.Choose(Option(scene, "testify")));

            Assert.AreEqual("taken", scene.Node.id);
            AssertSettled(Taken, Defended);
            Assert.IsTrue(_state.IsSet("aftermath.harrow.testified"));
            scene.Choose(Option(scene, "nod"));
            Assert.AreEqual(DialogueEnd.Finished, scene.EndedHow);
        }

        [Test]
        public void AFailedTestimonyIsJeeredAndHoldingTheCrowdStaysOpen()
        {
            Begin(Fails);
            _tips.Read(TipSource.NoticeBoard, _clock.Tick);
            var scene = Street();

            Assert.AreEqual(CheckResult.Failure, scene.Choose(Option(scene, "testify")));

            Assert.AreEqual("jeered", scene.Node.id);
            Assert.AreEqual(PoiPhase.Open, _director.Phase(_poiA));
            scene.Choose(Option(scene, "shrug"));
            CollectionAssert.AreEqual(new[] { "ask", "hold", "walk" }, Ids(scene));
            scene.Leave();
        }

        [Test]
        public void HoldingTheCrowdIsAServiceThatSealsTheShop()
        {
            Begin();
            var scene = Street();

            Assert.AreEqual(CheckResult.None, scene.Choose(Option(scene, "hold")));

            Assert.AreEqual("taken", scene.Node.id);
            AssertSettled(Taken, Defended);
            Assert.IsTrue(_state.IsSet("aftermath.harrow.crowd_held"));
            scene.Leave();
        }

        [Test]
        public void WalkingAwayLeavesAToResolveUnattendedAtItsWindowsEnd()
        {
            Begin();
            var scene = Street();
            scene.Choose(Option(scene, "walk"));

            Assert.IsFalse(scene.IsOpen);
            Assert.AreEqual(PoiPhase.Open, _director.Phase(_poiA), "the world goes on without the player");
            _clock.AdvanceTicks((int)(20 * 60 - _clock.Tick));

            Assert.AreEqual(PoiResolution.Unattended, _director.Resolution(_poiA), "the second-shift whistle at 20:00");
            CollectionAssert.Contains(new[] { "wardens", "veinmarket" }, _director.Outcome(_poiA));
            Assert.IsFalse(Defended.Concat(new[] { "debt.wardens" }).Any(_state.IsSet), "an unattended outcome puts nobody in debt (11)");
        }

        private void AssertSettled(IEnumerable<string> raised, IEnumerable<string> notRaised)
        {
            Assert.AreEqual(PoiPhase.Resolved, _director.Phase(_poiA));
            Assert.AreEqual(PoiResolution.Participated, _director.Resolution(_poiA));
            Assert.IsNull(_director.Outcome(_poiA), "no unattended outcome is drawn");
            foreach (var key in raised)
            {
                Assert.IsTrue(_state.IsSet(key), key);
            }

            foreach (var key in notRaised.Where(k => !raised.Contains(k)))
            {
                Assert.IsFalse(_state.IsSet(key), key);
            }
        }

        private IDialogueRunner Talk()
        {
            var dialogue = AssetDatabase.LoadAssetAtPath<DialogueDefinition>("Assets/Data/Slice/Dialogue/PoiADialogue.asset");
            Assert.IsNotNull(dialogue);
            return new DialogueSession(dialogue, _world);
        }

        /// <summary>The conversation, past its opening, before the Wardens.</summary>
        private IDialogueRunner Street()
        {
            var scene = Talk();
            scene.Choose(Option(scene, "step"));
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

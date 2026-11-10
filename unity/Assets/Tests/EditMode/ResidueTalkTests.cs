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
using Random = System.Random;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S2.7's EditMode done-when (VERTICAL_SLICE_IMPLEMENTATION_PLAN): what Nell and Kit say the next day follows what
    /// happened to Harrow and on Draught Lane (11.1 §6; SLICE_PEOPLE §2), and a lie to Nell that comes out at the day
    /// boundary turns her cold the next morning (10.4 §5), on a clock driven by hand.
    /// </summary>
    public sealed class ResidueTalkTests
    {
        private const string PoiFolder = "Assets/Data/Slice/Poi/";
        private const long Evening = 16 * 60;
        private const long Midnight = 24 * 60;
        private const long NextMorning = Midnight + (8 * 60) + 30;
        private const double Succeeds = 0d;
        private const double Fails = 0.9999d;

        private WorldState _state;
        private GameClock _clock;
        private PoiDirector _director;
        private TipBook _tips;
        private SocialStanding _standing;
        private DialogueContext _world;
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

        /// <summary>A fresh world whose clock reads <paramref name="tick"/>; checks roll <paramref name="check"/>, and a lie comes out on <paramref name="lie"/>.</summary>
        private void Begin(long tick, double check = Succeeds, double lie = Succeeds)
        {
            _state = new WorldState();
            _clock = new GameClock(10d, tick);
            var seed = new WorldSeed(110201);
            var pois = new[] { "PoiA", "PoiB", "PoiC" }.Select(n => AssetDatabase.LoadAssetAtPath<PoiDefinition>(PoiFolder + n + ".asset")).ToArray();
            _director = new PoiDirector(pois, _state, seed, SliceSeedStreams.PoiUnattended, _clock);
            _tips = new TipBook(AssetDatabase.LoadAssetAtPath<TipCatalogue>(PoiFolder + "Tips.asset"), _state, _director);
            _standing = new SocialStanding(_state, Rules<StandingRules>("StandingRules"), _clock, new FixedDice(lie), SliceSeedStreams.Lies);
            _world = new DialogueContext
            {
                State = _state,
                Clock = _clock,
                Seed = new FixedDice(check),
                CheckStream = SliceSeedStreams.SocialChecks,
                Rules = Rules<SocialCheckRules>("SocialCheckRules"),
                Standing = _standing,
                Character = new CharacterSheet(),
                Director = _director,
                Tips = _tips,
                Withheld = new HashSet<Approach> { Approach.Veil, Approach.Confront },
            };
        }

        [TestCase("aftermath.harrow.defended", "nell.harrow_open")]
        [TestCase("aftermath.harrow.shop_sealed", "nell.harrow_sealed")]
        [TestCase("aftermath.harrow.vanished", "nell.harrow_gone")]
        [TestCase("aftermath.loom_lane.girls_freed", "nell.lane_freed")]
        [TestCase("aftermath.loom_lane.girls_taken", "nell.lane_taken")]
        [TestCase("aftermath.loom_lane.snatch_spoiled", "nell.lane_spoiled")]
        public void NellTellsTheNextMorningWhatHappened(string aftermath, string line)
        {
            Begin(NextMorning);
            _state.Raise(aftermath);

            var nell = Talk("NellDialogue");

            CollectionAssert.AreEqual(new[] { line }, nell.Said.Select(t => t.id), "one line for what happened, and none for what did not");
            Assert.IsNull(_tips.Find(line).poi, "residue points to no POI: it is talk, not a lead");
            nell.Leave();
        }

        [Test]
        public void NellSaysBothNightsAndNothingTheDayItHappened()
        {
            Begin(Evening + 60);
            _state.Raise("aftermath.harrow.shop_sealed");
            _state.Raise("aftermath.loom_lane.girls_freed");
            var sameDay = Talk("NellDialogue");
            CollectionAssert.DoesNotContain(sameDay.Said.Select(t => t.id), "nell.harrow_sealed", "residue shows the next day");
            sameDay.Leave();

            // The POIs are left out of the night, so no unattended outcome adds its own line: this test reads the talk.
            _director.Dispose();
            _clock.AdvanceTicks((int)(NextMorning - _clock.Tick));
            var nell = Talk("NellDialogue");

            CollectionAssert.AreEqual(new[] { "nell.harrow_sealed", "nell.lane_freed" }, nell.Said.Select(t => t.id));
            nell.Leave();
        }

        [TestCase("aftermath.harrow.shop_sealed", true)]
        [TestCase("aftermath.harrow.defended", false)]
        public void KitSpeaksOfTheSealOnlyWhenThereIsOne(string aftermath, bool seal)
        {
            Begin(NextMorning);
            _state.Raise(aftermath);

            var kit = Talk("KitDialogue");

            Assert.AreEqual(seal, kit.Said.Any(t => t.id == "kit.seal"), "Kit's day-2 rumour by what happened (SLICE_PEOPLE §2)");
            kit.Leave();
        }

        [Test]
        public void ALieToNellThatComesOutAtMidnightTurnsHerColdTheNextMorning()
        {
            Begin(Evening);
            _tips.Read(TipSource.NoticeBoard, _clock.Tick);
            var nell = Talk("NellDialogue");
            var lie = Option(nell, "lie");
            Assert.AreEqual(CharacterSkill.Deception, lie.skill);
            Assert.AreEqual(Approach.Convince, lie.approach, "Deception serves Convince (10.4)");

            Assert.AreEqual(CheckResult.Success, nell.Choose(lie));

            Assert.IsTrue(_state.IsSet("tip.nell.collectors.heard"), "the lie gets her Draught Lane tip");
            Assert.AreEqual(1, _state.Get("lie.ordinary.nell"), "an Ordinary lie may come out at the next boundary");
            nell.Leave();
            _clock.AdvanceTicks((int)(Midnight - _clock.Tick));

            Assert.AreEqual(1, _state.Get(SocialStanding.ExposedKey("nell")), "it came out at midnight");
            Assert.AreEqual(-2, _standing.Attitude("nell"));
            Assert.AreEqual(-5, _standing.ReputationPoints);

            _clock.AdvanceTicks((int)(NextMorning - _clock.Tick));
            var morning = Talk("NellDialogue");
            Assert.AreEqual(CheckResult.None, morning.Choose(Option(morning, "cold")));
            Assert.AreEqual("found_out", morning.Node.id);
            morning.Choose(Option(morning, "sorry"));

            var later = Talk("NellDialogue");
            CollectionAssert.DoesNotContain(later.Options.Select(o => o.id), "cold", "she says it once");
            later.Leave();
        }

        [Test]
        public void ALieThatStaysHiddenLeavesNellAsSheWas()
        {
            Begin(Evening, Succeeds, Fails);
            _tips.Read(TipSource.NoticeBoard, _clock.Tick);
            var nell = Talk("NellDialogue");
            nell.Choose(Option(nell, "lie"));
            nell.Leave();

            _clock.AdvanceTicks((int)(NextMorning - _clock.Tick));

            Assert.AreEqual(0, _state.Get("lie.ordinary.nell"), "a lie has one chance");
            Assert.AreEqual(0, _state.Get(SocialStanding.ExposedKey("nell")));
            Assert.AreEqual(0, _standing.Attitude("nell"));
            var morning = Talk("NellDialogue");
            CollectionAssert.DoesNotContain(morning.Options.Select(o => o.id), "cold");
            morning.Leave();
        }

        private IDialogueRunner Talk(string name)
        {
            var dialogue = AssetDatabase.LoadAssetAtPath<DialogueDefinition>($"Assets/Data/Slice/Dialogue/{name}.asset");
            Assert.IsNotNull(dialogue, name);
            return new DialogueSession(dialogue, _world);
        }

        private static DialogueOption Option(IDialogueRunner scene, string id)
        {
            var option = scene.Options.SingleOrDefault(o => o.id == id);
            Assert.IsNotNull(option, $"'{id}' is not offered at '{scene.Node?.id}': {string.Join(", ", scene.Options.Select(o => o.id))}");
            return option;
        }

        private static T Rules<T>(string name) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>($"Assets/Data/Progression/{name}.asset");
            Assert.IsNotNull(asset, name);
            return asset;
        }

        /// <summary>Dice that always roll the same, so a check's way and a lie's fate are chosen by the test.</summary>
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

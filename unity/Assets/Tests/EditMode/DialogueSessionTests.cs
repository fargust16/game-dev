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
    /// <summary>S2.3: a conversation as data and the session that runs it (10.4), on a clock driven by hand.</summary>
    public sealed class DialogueSessionTests
    {
        private const int Seed = 110201;
        private const long Noon = 12 * 60;

        private WorldState _state;
        private GameClock _clock;
        private SocialStanding _standing;
        private CharacterSheet _character;
        private DialogueContext _world;
        private DialogueDefinition _dialogue;
        private readonly List<Object> _made = new List<Object>();

        [SetUp]
        public void Build()
        {
            _state = new WorldState();
            _clock = new GameClock(10d, Noon);
            var seed = new WorldSeed(Seed);
            _standing = new SocialStanding(_state, Asset<StandingRules>("StandingRules"), _clock, seed, SliceSeedStreams.Lies);
            _character = new CharacterSheet();
            _world = new DialogueContext
            {
                State = _state,
                Clock = _clock,
                Seed = seed,
                CheckStream = SliceSeedStreams.SocialChecks,
                Rules = Asset<SocialCheckRules>("SocialCheckRules"),
                Standing = _standing,
                Character = _character,
            };
            _dialogue = Make<DialogueDefinition>();
            _dialogue.Configure("test", Listeners(), Nodes());
        }

        [TearDown]
        public void Clean()
        {
            _standing.Dispose();
            foreach (var made in _made)
            {
                Object.DestroyImmediate(made);
            }

            _made.Clear();
        }

        [Test]
        public void TheTestDialogueIsWholeAsData()
        {
            CollectionAssert.IsEmpty(_dialogue.Problems(), string.Join("\n", _dialogue.Problems()));
        }

        [Test]
        public void ProblemsAreFound()
        {
            var broken = Make<DialogueDefinition>();
            broken.Configure("broken", Listeners(), new[]
            {
                new DialogueNode
                {
                    id = "start",
                    speaker = "nell",
                    options = new[]
                    {
                        new DialogueOption { id = "bare", kind = OptionKind.Check, skill = CharacterSkill.Persuasion },
                        new DialogueOption { id = "lost", success = new DialogueOutcome { next = "nowhere" } },
                        new DialogueOption { id = "lost" },
                    },
                },
            });

            var problems = broken.Problems();
            Assert.IsTrue(problems.Any(p => p.Contains("bare") && p.Contains("Argument")), string.Join("\n", problems));
            Assert.IsTrue(problems.Any(p => p.Contains("nowhere")));
            Assert.IsTrue(problems.Any(p => p.Contains("taken")));
        }

        [Test]
        public void TheConversationPausesTheWorldAndCostsOneTickWhenItEnds()
        {
            var session = Open();
            Assert.IsTrue(_clock.IsPaused);
            _clock.AdvanceRealTime(600d);
            Assert.AreEqual(Noon, _clock.Tick, "Nothing moves while the character talks.");

            var ended = new List<DialogueEnd>();
            session.Ended += ended.Add;
            session.Choose(Option(session, "bye"));

            Assert.IsFalse(session.IsOpen);
            Assert.IsFalse(_clock.IsPaused);
            Assert.AreEqual(Noon + 1, _clock.Tick);
            CollectionAssert.AreEqual(new[] { DialogueEnd.Finished }, ended);

            var walked = Open();
            walked.Leave();
            walked.Leave();
            Assert.AreEqual(DialogueEnd.Left, walked.EndedHow);
            Assert.AreEqual(Noon + 2, _clock.Tick, "Walking away costs the tick too, once.");
        }

        [Test]
        public void AnOptionWithoutItsArgumentDoesNotAppear()
        {
            var session = Open();
            Assert.IsFalse(Offers(session, "convince"));
            _state.Raise("seen.x");
            Assert.IsTrue(Offers(session, "convince"));
        }

        [Test]
        public void TalkMovesBetweenNodes()
        {
            var session = Open();
            session.Choose(Option(session, "ask"));
            Assert.AreEqual("more", session.Node.id);
            Assert.IsTrue(session.IsOpen);
            session.Choose(Option(session, "back"));
            Assert.AreEqual("start", session.Node.id);
        }

        [Test]
        public void ThePreviewHasTheChancesOf10_4()
        {
            _state.Raise("seen.x");
            var session = Open();
            var preview = session.Preview(Option(session, "convince"));
            Assert.AreEqual(0, preview.CharacterValue, "A new character has a value of 0.");
            Assert.AreEqual(1, preview.ListenerValue, "A resident of value 1.");
            Assert.AreEqual(8, preview.Difficulty);
            Assert.AreEqual(StandingSource.Reputation, preview.Standing.Source);
            Assert.AreEqual(42, Mathf.RoundToInt((float)(preview.Chance * 100)), "10.4 §7: 42 % on an ordinary request.");

            _state.Raise("seen.y");
            Assert.AreEqual(1, session.Preview(Option(session, "convince")).Arguments);
            _state.Raise("seen.z");
            _state.Raise("tip.a.heard");
            preview = session.Preview(Option(session, "convince"));
            Assert.AreEqual(2, preview.Arguments, "Each further Argument adds +1, at most +2.");

            _standing.ChangeAttitude("nell", 2);
            _state.Set(SocialStanding.ReputationKey, 15);
            preview = session.Preview(Option(session, "convince"));
            Assert.AreEqual(4, preview.Modifiers, "2 + 2 + 2 is held at +4.");
            Assert.AreEqual(33d / 36d * 1.01, preview.Chance, 1e-9, "8 − 4: two dice of 4 or more, times Luck 1.");
        }

        [Test]
        public void AFailedCheckCannotBeTriedAgain()
        {
            _state.Raise("seen.x");
            var session = Open();
            Assert.AreEqual(CheckResult.Failure, session.Choose(Option(session, "hopeless")), "1/36 against a listener of value 10.");
            Assert.AreEqual("start", session.Node.id, "A friendly listener leaves the talk going.");
            Assert.IsFalse(Offers(session, "hopeless"));
            session.Leave();

            var again = Open();
            Assert.IsFalse(Offers(again, "hopeless"), "Locked for good, in the world state.");
            Assert.IsTrue(Offers(again, "convince"), "Only the failed option is locked.");
        }

        [Test]
        public void AFailedFinalCheckEndsInAFight()
        {
            _state.Raise("seen.x");
            var session = Open();
            var result = session.Choose(Option(session, "deceive"));

            Assert.AreEqual(CheckResult.Failure, result, "1/36 against the collector.");
            Assert.IsFalse(session.IsOpen);
            Assert.AreEqual(DialogueEnd.Fight, session.EndedHow);
            Assert.AreEqual("collector", session.FightWith.id);
            Assert.IsTrue(_state.IsSet("collector.alerted"), "The failure's keys are raised before the fight.");
            Assert.AreEqual(-5, _standing.ReputationPoints, "A failed lie costs 5 Reputation.");
            Assert.AreEqual(Noon + 1, _clock.Tick);
        }

        [Test]
        public void ThreatsCostAttitudeAndALieThatWorkedWaitsForMidnight()
        {
            _state.Raise("seen.x");
            _state.Raise("seen.y");
            _state.Raise("seen.z");
            _standing.ChangeAttitude("nell", 2);
            var session = Open();

            Assert.AreEqual(CheckResult.Success, session.Choose(Option(session, "threaten")), "35/36: Attitude +2, Danger −1, Arguments +2 against 6.");
            Assert.AreEqual(1, _standing.Attitude("nell"), "Intimidation costs a step even when it works.");
            Assert.IsTrue(_state.IsSet("nell.cowed"));

            var lying = Open();
            _standing.ChangeAttitude("pip", 2);
            Assert.AreEqual(CheckResult.Success, lying.Choose(Option(lying, "lie")), "35/36: +4 against 7.");
            Assert.IsTrue(_state.Keys.Contains("lie.ordinary.pip"), "The lie waits for the next day boundary.");
        }

        [Test]
        public void WithheldApproachesMissingItemsAndUnmetNeedsAreHidden()
        {
            _world.Withheld = new HashSet<Approach> { Approach.Veil, Approach.Confront };
            var session = Open();
            Assert.IsFalse(Offers(session, "veil"), "S3 builds Veil.");
            Assert.IsFalse(Offers(session, "show"), "No inventory, no item.");
            Assert.IsFalse(Offers(session, "dayTwo"));
            session.Leave();

            _world.Withheld = new HashSet<Approach>();
            _world.Holds = item => item == "ledger";
            _state.Raise("day2");
            var later = Open();
            Assert.IsTrue(Offers(later, "veil"));
            Assert.IsTrue(Offers(later, "show"));
            Assert.IsTrue(Offers(later, "dayTwo"));
        }

        [Test]
        public void AnOutcomeRaisesItsKeysAndSettlesItsPoi()
        {
            var poi = Make<PoiDefinition>();
            poi.Configure("t", PoiWindowClass.Standard, 0, null, 600, -1, new PoiOutcome[0], Vector3.zero, 30f);
            var director = new PoiDirector(new[] { poi }, _state, new WorldSeed(Seed), SliceSeedStreams.PoiUnattended, _clock);
            director.Evaluate(_clock.Tick);
            Assert.AreEqual(PoiPhase.Open, director.Phase(poi));

            _world.Director = director;
            _dialogue.Node("start").options.First(o => o.id == "bye").success.settles = poi;
            var session = Open();
            session.Choose(Option(session, "bye"));
            director.Dispose();

            Assert.IsTrue(_state.IsSet("talk.done"));
            Assert.AreEqual(PoiPhase.Resolved, director.Phase(poi));
            Assert.AreEqual(PoiResolution.Participated, director.Resolution(poi));
        }

        [Test]
        public void EveryDialogueAssetIsWholeAndHasItsWords()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:DialogueDefinition"))
            {
                var dialogue = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                CollectionAssert.IsEmpty(dialogue.Problems(), string.Join("\n", dialogue.Problems()));
                foreach (var key in dialogue.Keys())
                {
                    Assert.IsTrue(Loc.Exists(key), $"{dialogue.Id}: '{key}'");
                }
            }
        }

        private DialogueSession Open() => new DialogueSession(_dialogue, _world);

        private static DialogueOption Option(IDialogueRunner session, string id) =>
            session.Dialogue.Nodes.SelectMany(n => n.options).First(o => o.id == id);

        private static bool Offers(IDialogueRunner session, string id) => session.Options.Any(o => o.id == id);

        private T Make<T>() where T : ScriptableObject
        {
            var made = ScriptableObject.CreateInstance<T>();
            _made.Add(made);
            return made;
        }

        private static T Asset<T>(string name) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>($"Assets/Data/Progression/{name}.asset");
            Assert.IsNotNull(asset, name);
            return asset;
        }

        private static DialogueListener[] Listeners() => new[]
        {
            new DialogueListener { id = "nell", nameKey = "person.nell.name", intelligence = 10, will = 10, strength = 10 },
            new DialogueListener { id = "pip", nameKey = "person.pip.name" },
            new DialogueListener { id = "sage", nameKey = "person.nell.name", intelligence = 100, will = 100, strength = 100 },
            new DialogueListener { id = "collector", nameKey = "person.nell.name", enemy = true, intelligence = 100, will = 100, strength = 100 },
        };

        private static DialogueNode[] Nodes()
        {
            var arguments = new[] { "seen.x", "seen.y", "seen.z", "tip.a.heard" };
            return new[]
            {
                new DialogueNode
                {
                    id = "start",
                    speaker = "nell",
                    options = new[]
                    {
                        new DialogueOption { id = "ask", success = new DialogueOutcome { next = "more" } },
                        new DialogueOption
                        {
                            id = "convince", approach = Approach.Convince, kind = OptionKind.Check, skill = CharacterSkill.Persuasion,
                            request = RequestSize.Ordinary, arguments = arguments,
                            success = new DialogueOutcome { raises = new[] { "nell.told" } },
                            failure = new DialogueOutcome { next = "start" },
                        },
                        new DialogueOption
                        {
                            id = "threaten", approach = Approach.Threaten, kind = OptionKind.Check, skill = CharacterSkill.Intimidation,
                            request = RequestSize.Small, arguments = arguments, hiddenBy = new[] { "nell.cowed" },
                            success = new DialogueOutcome { raises = new[] { "nell.cowed" } },
                            failure = new DialogueOutcome { next = "start" },
                        },
                        new DialogueOption
                        {
                            id = "lie", approach = Approach.Convince, kind = OptionKind.Check, skill = CharacterSkill.Deception,
                            request = RequestSize.Ordinary, arguments = arguments, listener = "pip",
                            failure = new DialogueOutcome { next = "start" },
                        },
                        new DialogueOption
                        {
                            id = "hopeless", approach = Approach.Convince, kind = OptionKind.Check, skill = CharacterSkill.Persuasion,
                            request = RequestSize.Huge, arguments = new[] { "seen.x" }, listener = "sage",
                            failure = new DialogueOutcome { next = "start" },
                        },
                        new DialogueOption
                        {
                            id = "deceive", approach = Approach.Convince, kind = OptionKind.Check, skill = CharacterSkill.Deception,
                            request = RequestSize.Huge, arguments = new[] { "seen.x" }, listener = "collector",
                            failure = new DialogueOutcome { raises = new[] { "collector.alerted" }, next = "start" },
                        },
                        new DialogueOption { id = "veil", approach = Approach.Veil },
                        new DialogueOption { id = "show", kind = OptionKind.ShowItem, item = "ledger" },
                        new DialogueOption { id = "dayTwo", needs = new[] { "day2" } },
                        new DialogueOption { id = "bye", approach = Approach.Withdraw, success = new DialogueOutcome { raises = new[] { "talk.done" } } },
                    },
                },
                new DialogueNode
                {
                    id = "more",
                    speaker = "nell",
                    options = new[] { new DialogueOption { id = "back", success = new DialogueOutcome { next = "start" } } },
                },
            };
        }
    }
}

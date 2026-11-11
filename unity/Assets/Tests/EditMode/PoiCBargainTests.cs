using System.Collections.Generic;
using System.Linq;
using GameDev.Confront.Stealth;
using GameDev.Core.Localization;
using GameDev.Decisions;
using GameDev.Dialogue;
using GameDev.Player.Progression;
using GameDev.Player.Standing;
using GameDev.Slice.EditorTools;
using GameDev.Slice.Kit;
using GameDev.Slice.Stealth;
using GameDev.World.Poi;
using GameDev.World.Portals;
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
    /// S4.3 and S4.4: POI C's nook, its stranger and the black drop (11.1 *Slice build (S4)*): the card against its
    /// conversation, the price said before any choice, each choice's outcome, what the drop changes in what the
    /// character sees and in its Darkness, and the cellar as placed. The checks' dice are fixed.
    /// </summary>
    public sealed class PoiCBargainTests
    {
        private const string PoiFolder = "Assets/Data/Slice/Poi/";
        private const long Afternoon = 14 * 60;
        private const double Succeeds = 0d;
        private const double Fails = 0.9999d;
        private static readonly string[] Choices = { "drink", "leave", "smash", "keep_note", "s_drink", "s_refuse", "s_smash" };

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
            Assert.AreEqual(PoiPhase.Open, _director.Phase(_c));
        }

        [Test]
        public void TheCardIsWholeAndMatchesItsConversation()
        {
            var node = AssetDatabase.LoadAssetAtPath<DecisionNodeDefinition>("Assets/Data/Slice/Decisions/PoiCNode.asset");
            Assert.IsNotNull(node);
            CollectionAssert.IsEmpty(node.Problems());
            CollectionAssert.IsEmpty(node.Scene.Problems());
            Assert.AreEqual("c", node.Poi.Id);
            Assert.AreEqual("poic", node.Scene.Id);
            Assert.AreEqual(DecisionAxis.Attention, node.PrimaryAxis, "11.1 POI C: Primary Attention");
            Assert.AreEqual(DecisionAxis.Approach, node.SecondaryAxis);
            Assert.AreEqual(7, node.Options.Count, "11.1's six options, with threatening the stranger of its Discoverable");

            var choices = node.Options.SelectMany(o => o.dialogueOptions).ToList();
            var withApproach = node.Scene.Nodes.SelectMany(n => n.options).Where(o => o.approach != Approach.None).Select(o => o.id);
            CollectionAssert.IsSubsetOf(withApproach, choices, "every Approach in the conversation is one of the card's options");
            foreach (var key in node.Scene.Keys())
            {
                Assert.IsTrue(Loc.Tables.Has(key, "en") && Loc.Tables.Has(key, "ru"), $"no words for {key}");
            }
        }

        [Test]
        public void ThePriceIsSaidBeforeAnyChoice()
        {
            var dialogue = Dialogue();
            var choosing = dialogue.Nodes.Where(n => n.options.Any(o => Choices.Contains(o.id))).Select(n => n.id).ToList();
            CollectionAssert.AreEquivalent(new[] { "note", "offer", "note_empty" }, choosing, "the choices stand only after the note");
            foreach (var entry in new[] { NookSpot.NookNode, NookSpot.EmptyNode, NookSpot.StrangerNode })
            {
                Assert.IsFalse(dialogue.Node(entry).options.Any(o => o.success.settles != null), $"nothing is settled on '{entry}', before the note");
            }

            foreach (var option in dialogue.Nodes.SelectMany(n => n.options).Where(o => choosing.Contains(o.success.next)))
            {
                CollectionAssert.Contains(option.success.tells, "drop.note", $"'{option.id}' leads to a choice by telling the note");
            }

            Begin();
            var talk = Talk(NookSpot.NookNode);
            CollectionAssert.AreEqual(new[] { "unfold", "close" }, Ids(talk));
            talk.Choose(Option(talk, "unfold"));
            Assert.AreEqual("drop.note", talk.Said.Single().id);
            StringAssert.Contains("no shadow will hide you", talk.Said.Single().Line, "the price, in the note's words");
            CollectionAssert.AreEqual(new[] { "drink", "leave", "smash", "walk" }, Ids(talk));
        }

        [Test]
        public void DrinkingSettlesCAndTheFogSpeaksBothWays()
        {
            Begin();
            var rules = AssetDatabase.LoadAssetAtPath<StealthRules>("Assets/Data/Confront/StealthRules.asset");
            var host = new GameObject("Stealth under test");
            try
            {
                var stealth = host.AddComponent<SliceStealth>();
                stealth.Configure(rules, new CharacterSheet(), _clock, () => false);
                stealth.FogSpeaks = () => _state.IsSet(SliceStealth.FogKey);
                Assert.AreEqual(1, stealth.SightPerception);
                Assert.AreEqual(1, stealth.SightIntelligence);

                var talk = Talk(NookSpot.NookNode);
                talk.Choose(Option(talk, "unfold"));
                talk.Choose(Option(talk, "drink"));

                AssertSettled("poi.c.drank", SliceStealth.FogKey);
                Assert.AreEqual("drank", talk.Node.id, "the breath rasps once");
                Assert.AreEqual(rules.MeterFromPerception, stealth.SightPerception, "every Notice Meter, as Perception 4 shows (10.3 §4)");
                Assert.AreEqual(rules.ConesClearFromIntelligence, stealth.SightIntelligence, "the cones clear, as Intelligence 6 shows");
                Assert.IsTrue(rules.ShowsMeter(stealth.SightPerception));
                Assert.AreEqual(rules.ConeOpacity(rules.ConesClearFromIntelligence), rules.ConeOpacity(stealth.SightIntelligence));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }

            Assert.AreEqual(4, SliceStealth.HiddenBy(4, false), "night's Darkness adds to Stealth skill");
            Assert.AreEqual(0, SliceStealth.HiddenBy(4, true), "the price: no shadow hides one the fog speaks of");
            Assert.AreEqual(7, SliceStealth.FogValue(7, 4, true), "a sharper eye stays sharper");
        }

        [Test]
        public void LeavingItKeepsTheWordsAsKnowledge()
        {
            Begin();
            var notes = new CharacterNotes(_tips);
            var talk = Talk(NookSpot.NookNode);
            talk.Choose(Option(talk, "unfold"));
            talk.Choose(Option(talk, "leave"));

            AssertSettled("poi.c.left");
            Assert.IsFalse(_state.IsSet(SliceStealth.FogKey));
            Assert.AreEqual(Loc.Get("tip.drop.note.note"), notes.Lines.Last(), "the note's words stay in the Notes");
        }

        [Test]
        public void SmashingItStainsTheNooksStone()
        {
            Begin();
            var talk = Talk(NookSpot.NookNode);
            talk.Choose(Option(talk, "unfold"));
            talk.Choose(Option(talk, "smash"));

            AssertSettled("poi.c.smashed", "poi.c.smashed.nook");
            Assert.IsFalse(_state.IsSet(SliceStealth.FogKey));
        }

        [Test]
        public void WalkingAwayLeavesCOpenAndTheWordsSaidAgain()
        {
            Begin();
            var talk = Talk(NookSpot.NookNode);
            talk.Choose(Option(talk, "unfold"));
            talk.Choose(Option(talk, "walk"));
            Assert.IsFalse(talk.IsOpen);
            Assert.AreEqual(PoiPhase.Open, _director.Phase(_c), "C stays open until the invasion closes it");

            var again = Talk(NookSpot.NookNode);
            again.Choose(Option(again, "unfold"));
            CollectionAssert.IsEmpty(again.Said, "the note is heard once");
            Assert.AreEqual(Loc.Get("tip.drop.note.line"), Dialogue().Line(again.Node), "its words are the node's own line, said again before the choice");
            CollectionAssert.Contains(Ids(again), "drink");
        }

        [Test]
        public void TheStrangerOffersAndRefusalSendsHimOff()
        {
            Begin();
            var talk = Talk(NookSpot.StrangerNode);
            CollectionAssert.AreEqual(new[] { "hear", "not_now" }, Ids(talk), "a threat needs an Argument");
            talk.Choose(Option(talk, "hear"));
            Assert.AreEqual("offer", talk.Node.id);
            Assert.AreEqual("drop.note", talk.Said.Single().id, "he makes the offer with the note's words");

            talk.Choose(Option(talk, "s_refuse"));

            AssertSettled("poi.c.refused", NookSpot.StrangerGone);
        }

        [Test]
        public void AThreatThatWorksSendsHimOffAndLeavesTheDrop()
        {
            Begin();
            _tips.Tell("trace.chalk", _clock.Tick);
            var talk = Talk(NookSpot.StrangerNode);
            Assert.AreEqual(CheckResult.Success, talk.Choose(Option(talk, "threaten")));

            Assert.AreEqual("fled", talk.Node.id);
            Assert.IsTrue(_state.IsSet(NookSpot.StrangerGone));
            Assert.AreEqual(PoiPhase.Open, _director.Phase(_c), "the drop stays on the shelf");
            Assert.AreEqual(NookSpot.NookNode, NookSpot.Entry(false, _state));
            Assert.Less(_standing.Attitude("stranger"), 0);
        }

        [Test]
        public void AFailedThreatTakesTheDropAndLeavesOnlyTheNote()
        {
            Begin(Fails);
            _tips.Tell("trace.chalk", _clock.Tick);
            var talk = Talk(NookSpot.StrangerNode);
            Assert.AreEqual(CheckResult.Failure, talk.Choose(Option(talk, "threaten")));

            Assert.AreEqual("took", talk.Node.id);
            Assert.IsTrue(_state.IsSet(NookSpot.DropTaken) && _state.IsSet(NookSpot.StrangerGone));
            Assert.AreEqual(NookSpot.EmptyNode, NookSpot.Entry(false, _state), "the shutters open on the note alone");

            var nook = Talk(NookSpot.EmptyNode);
            nook.Choose(Option(nook, "unfold_empty"));
            Assert.AreEqual("drop.note", nook.Said.Single().id);
            CollectionAssert.AreEqual(new[] { "keep_note", "walk_empty" }, Ids(nook), "no drop to drink");
            nook.Choose(Option(nook, "keep_note"));
            AssertSettled("poi.c.note_kept");
        }

        [Test]
        public void NellsNextLineFollowsTheTellProved()
        {
            Begin();
            CollectionAssert.IsEmpty(Bartender().Intersect(new[] { "nell.c_link", "nell.c_coach" }), "nothing proved, nothing said");

            _tips.Tell("trace.misread", _clock.Tick);
            CollectionAssert.DoesNotContain(Bartender(), "nell.c_link", "a misreading proves nothing");
            _tips.Tell("trace.link", _clock.Tick);
            CollectionAssert.Contains(Bartender(), "nell.c_link", "the close look's link: one buyer for the medicine and the girls");
            CollectionAssert.DoesNotContain(Bartender(), "nell.c_coach");
            _tips.Tell("sweep.coach", _clock.Tick);
            CollectionAssert.Contains(Bartender(), "nell.c_coach", "the sweep's coach: up the hill");
            foreach (var id in new[] { "nell.c_link", "nell.c_coach" })
            {
                Assert.IsTrue(Loc.Tables.Has($"tip.{id}.line", "ru") && Loc.Tables.Has($"tip.{id}.note", "ru"), id);
            }
        }

        [Test]
        public void HeSeesAStandingCharacterInSightAndAHiderOnlyOnceNoticed()
        {
            var meter = new NoticeMeter(AssetDatabase.LoadAssetAtPath<StealthRules>("Assets/Data/Confront/StealthRules.asset"));
            Assert.IsTrue(NookSpot.Sees(false, true, meter), "a standing character hides nothing");
            Assert.IsFalse(NookSpot.Sees(false, false, meter), "out of his cone or sight");
            meter.See(10d, 1f, Vector3.zero);
            Assert.IsFalse(NookSpot.Sees(true, true, meter), "in Stealth Mode his meter must fill first (10.3 §3)");
            meter.See(10d, 10f, Vector3.zero);
            Assert.IsTrue(NookSpot.Sees(true, true, meter), "noticed and in sight");
            Assert.AreEqual(NookSpot.StrangerNode, NookSpot.Entry(true, new WorldState()), "seen, he makes the offer himself");
            Assert.AreEqual(NookSpot.NookNode, NookSpot.Entry(false, new WorldState()), "unseen, the shutters open");
        }

        [Test]
        public void TheCellarHasTheNookInHisSightAndTheStairOutOfIt()
        {
            EditorSceneManager.OpenScene(VeilmouthDressing.CellarScenePath, OpenSceneMode.Single);
            var nook = Object.FindObjectsByType<NookSpot>(FindObjectsInactive.Include).Single();
            var rules = AssetDatabase.LoadAssetAtPath<StealthRules>("Assets/Data/Confront/StealthRules.asset");
            var stair = Object.FindObjectsByType<Portal>(FindObjectsInactive.Include).Single(p => p.Id == "soot-stair-cellar.inside");

            Assert.AreEqual("c", nook.Poi.Id);
            Assert.AreEqual(Dialogue(), nook.Talk.Dialogue);
            Assert.AreEqual(NookSpot.NookNode, nook.Talk.StartNode);
            Assert.IsFalse(nook.Talk.OpensItself, "the nook opens it");
            Assert.AreEqual(3, nook.Watcher.Perception, "like POI B's lookout");
            Assert.AreEqual(NookSpot.StrangerGone, nook.Stranger.GoneKey);
            Assert.AreEqual(NookSpot.StrangerNode, nook.Stranger.GetComponent<PersonTalk>().StartNode);
            Assert.IsFalse(nook.Stranger.Cast.activeSelf);
            Assert.IsFalse(stair.InReach(nook.transform.position), "the stair door does not take the nook's Interact");

            var eyes = nook.Watcher.transform;
            Assert.IsTrue(Seen(rules, eyes, nook.transform.position), "he sees the nook");
            Assert.IsFalse(Seen(rules, eyes, stair.transform.position + stair.transform.forward), "and not the stair's foot");
        }

        private static bool Seen(StealthRules rules, Transform eyes, Vector3 at)
        {
            if (!rules.InCone(eyes.position, eyes.forward, at, 1f))
            {
                return false;
            }

            var notCharacters = ~LayerMask.GetMask(KitLayers.Character);
            return !Physics.Linecast(eyes.position + (Vector3.up * rules.EyeHeight), at + (Vector3.up * 1.6f), notCharacters, QueryTriggerInteraction.Ignore);
        }

        private void AssertSettled(params string[] raised)
        {
            Assert.AreEqual(PoiPhase.Resolved, _director.Phase(_c));
            Assert.AreEqual(PoiResolution.Participated, _director.Resolution(_c), "each choice settles C (11.1 Slice Linkage Rules 4)");
            foreach (var key in raised)
            {
                Assert.IsTrue(_state.IsSet(key), key);
            }
        }

        private static DialogueDefinition Dialogue()
        {
            var dialogue = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(SlicePoiC.PoiCDialoguePath);
            Assert.IsNotNull(dialogue);
            return dialogue;
        }

        private IDialogueRunner Talk(string node) => new DialogueSession(Dialogue(), _world, node);

        private static DialogueOption Option(IDialogueRunner talk, string id)
        {
            var option = talk.Options.SingleOrDefault(o => o.id == id);
            Assert.IsNotNull(option, $"'{id}' is not offered at '{talk.Node?.id}': {string.Join(", ", Ids(talk))}");
            return option;
        }

        private static List<string> Ids(IDialogueRunner talk) => talk.Options.Select(o => o.id).ToList();

        private List<string> Bartender() => _tips.Available(TipSource.Bartender, _clock.Tick).Select(t => t.id).ToList();

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

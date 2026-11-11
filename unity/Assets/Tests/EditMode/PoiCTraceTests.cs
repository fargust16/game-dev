using System.Linq;
using GameDev.Core.Localization;
using GameDev.Player.Progression;
using GameDev.Slice.EditorTools;
using GameDev.World.Poi;
using GameDev.World.Portals;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>S4.1: POI C's Physical Traces on Deaf Boiler Yard (11.1 *Slice build (S4)*).</summary>
    public sealed class PoiCTraceTests
    {
        private const string Folder = "Assets/Data/Slice/Poi/";
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
            _clock = new GameClock(1d, 840);
            _director = new PoiDirector(new[] { _a, _b, _c }, _state, new WorldSeed(110201), SliceSeedStreams.PoiUnattended, _clock);
            _book = new TipBook(AssetDatabase.LoadAssetAtPath<TipCatalogue>(Folder + "Tips.asset"), _state, _director);
            _director.Evaluate(_clock.Tick);
        }

        [TearDown]
        public void End() => _director.Dispose();

        [Test]
        public void TheTellsLieByWhatTheFirstDaysLeftUntilTheSweeper()
        {
            var always = new StateCondition[0];
            var wax = new[] { new StateCondition("aftermath.harrow.shop_sealed") };
            var rag = new[] { new StateCondition("aftermath.loom_lane.girls_freed"), new StateCondition("aftermath.loom_lane.snatch_spoiled") };

            Assert.IsFalse(TraceSpot.Lies(_state, PoiPhase.Pending, 900, always, -1), "nothing lies before C opens");
            Assert.IsTrue(TraceSpot.Lies(_state, PoiPhase.Open, 900, always, -1), "the chalk lies once C is open");
            Assert.IsTrue(TraceSpot.Lies(_state, PoiPhase.Resolved, 2800, always, -1), "and stays after C is settled");
            Assert.IsFalse(TraceSpot.Lies(_state, PoiPhase.Open, 900, wax, SlicePoiC.SweptAt), "no wax while the shop is open");

            _state.Raise("aftermath.harrow.shop_sealed");
            _state.Raise("aftermath.loom_lane.snatch_spoiled");
            Assert.IsTrue(TraceSpot.Lies(_state, PoiPhase.Open, 900, wax, SlicePoiC.SweptAt), "the Wardens' wax");
            Assert.IsTrue(TraceSpot.Lies(_state, PoiPhase.Open, 900, rag, SlicePoiC.SweptAt), "the rag, by either way the snatch failed");
            Assert.IsTrue(TraceSpot.Lies(_state, PoiPhase.Open, SlicePoiC.SweptAt - 1, wax, SlicePoiC.SweptAt));
            Assert.IsFalse(TraceSpot.Lies(_state, PoiPhase.Open, SlicePoiC.SweptAt, wax, SlicePoiC.SweptAt), "the sweeper clears it at day 2, 20:00");
            Assert.AreEqual(1440 + 1200, SlicePoiC.SweptAt);
        }

        [Test]
        public void TheFirstTraceMarksPoiCAndNotesItsFact()
        {
            var notes = new CharacterNotes(_book);
            _director.Reveal(_a, PoiDiscovery.Marked);
            _director.ResolveParticipated(_a);
            Assert.AreEqual(PoiPhase.Open, _director.Phase(_c), "C opens once A has resolved");
            Assert.AreEqual(PoiDiscovery.Unknown, _director.Discovery(_c));

            Assert.IsNotNull(_book.Tell("trace.chalk", _clock.Tick));

            Assert.AreEqual(PoiDiscovery.Marked, _director.Discovery(_c), "11.1: Marked when the first Physical Trace is found");
            Assert.AreEqual(Loc.Get("tip.trace.chalk.note"), notes.Lines.Last());
            Assert.IsNull(_book.Tell("trace.chalk", _clock.Tick), "a trace is heard once");
        }

        [Test]
        public void NoTraceIsToldBeforePoiCOpens()
        {
            Assert.IsFalse(_book.CanTell("trace.chalk", _clock.Tick));
            Assert.IsEmpty(_book.Available(TipSource.PhysicalTrace, _clock.Tick), "before C, the yard holds nothing");
        }

        [Test]
        public void TheCloseLookHasASolidTellAndAMisreading()
        {
            var rules = AssetDatabase.LoadAssetAtPath<ActionCheckRules>("Assets/Data/Progression/ActionCheckRules.asset");
            var character = new CharacterSheet();

            // 07.1: Δ = 1 − 3 = −2 gives 50 %, Intelligence 1 against difficulty 3 adds a third of its 10 %, and Luck 1 adds 1 %.
            var chance = TraceSpot.LookChance(rules, character, 3);
            Assert.AreEqual(0.5 * (1d + (0.1 / 3d)) * 1.01, chance, 1e-9);

            Assert.IsTrue(TraceSpot.LookSucceeds(rules, character, 3, new ScriptedRolls(new[] { chance - 0.01 })));
            Assert.IsFalse(TraceSpot.LookSucceeds(rules, character, 3, new ScriptedRolls(new[] { chance + 0.01 })));

            character.Set(CharacterAttribute.Perception, 4);
            Assert.Greater(TraceSpot.LookChance(rules, character, 3), chance, "a sharper eye reads more");
        }

        [Test]
        public void EveryTraceIsAPhysicalTraceThatMarksCInBothLanguages()
        {
            var traces = AssetDatabase.LoadAssetAtPath<TipCatalogue>(Folder + "Tips.asset").Tips.Where(t => t.id.StartsWith("trace.")).ToList();
            Assert.AreEqual(8, traces.Count);
            foreach (var tip in traces)
            {
                Assert.AreEqual(TipSource.PhysicalTrace, tip.source, tip.id);
                Assert.AreEqual(_c, tip.poi, tip.id);
                Assert.AreEqual(TipEffect.Marks, tip.effect, tip.id);
                foreach (var language in new[] { "en", "ru" })
                {
                    Assert.IsTrue(Loc.Tables.Has(tip.LineKey, language) && Loc.Tables.Has(tip.NoteKey, language), $"{tip.id} in {language}");
                }
            }
        }

        [Test]
        public void TheTracesLieOnTheYardBeforeTheCellarDoor()
        {
            EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var door = Object.FindObjectsByType<Portal>(FindObjectsInactive.Include).Single(p => p.Id == "soot-stair-cellar.street");
            var spots = Object.FindObjectsByType<TraceSpot>(FindObjectsInactive.Include);

            CollectionAssert.AreEquivalent(new[] { "trace.chalk", "trace.wax", "trace.glass", "trace.oil", "trace.glove", "trace.rag" }, spots.Select(s => s.Tip));
            Assert.AreEqual(1, spots.Count(s => s.CloseLook), "only the chalk allows the close look");
            foreach (var spot in spots)
            {
                Assert.AreEqual(_c, spot.Poi, spot.name);
                Assert.Less(Vector3.Distance(spot.transform.position, door.transform.position), 9f, $"{spot.name} lies before the cellar door");
                Assert.IsFalse(door.InReach(spot.transform.position), $"{spot.name} stands out of the door's reach, so the door does not take its Interact");
            }
        }
    }
}

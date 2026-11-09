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
    /// <summary>The POI Director (S1.2): Active Windows on the clock's ticks and the unattended resolution's Aftermath.</summary>
    public sealed class PoiDirectorTests
    {
        private const int Seed = 110201;
        private readonly List<Object> _made = new List<Object>();
        private PoiDefinition _a;
        private PoiDefinition _b;
        private PoiDefinition _c;

        [SetUp]
        public void MakePois()
        {
            _a = Make("a", 840, null, 360, -1,
                new PoiOutcome { name = "wardens", weight = 1f, raises = new[] { "aftermath.harrow.taken" } },
                new PoiOutcome { name = "veinmarket", weight = 1f, raises = new[] { "aftermath.harrow.vanished" } });
            _b = Make("b", 1380, null, 180, -1,
                new PoiOutcome { name = "veinmarket", weight = 1f, raises = new[] { "aftermath.loom_lane.girls_taken" } });
            _c = Make("c", -1, new[] { _a, _b }, 0, 2880,
                new PoiOutcome { name = "trace_faded", weight = 1f, raises = new[] { "aftermath.soot_stair.trace_faded" } });
        }

        [TearDown]
        public void Destroy()
        {
            foreach (var made in _made)
            {
                Object.DestroyImmediate(made);
            }

            _made.Clear();
        }

        [Test]
        public void WindowsOpenAtTheirMinutesAndNotBefore()
        {
            var (director, clock, _) = Run(0);

            clock.AdvanceTicks(839);
            Assert.AreEqual(PoiPhase.Pending, director.Phase(_a));
            clock.AdvanceTicks(1);
            Assert.AreEqual(PoiPhase.Open, director.Phase(_a), "A opens at 14:00");
            Assert.AreEqual(PoiPhase.Pending, director.Phase(_b));

            clock.AdvanceTicks(1380 - 840);
            Assert.AreEqual(PoiPhase.Open, director.Phase(_b), "B opens at 23:00");
        }

        [Test]
        public void APoiLeftAloneClosesOnTimeAndWritesItsAftermath()
        {
            var (director, clock, state) = Run(0);
            var heard = new List<(string, PoiResolution, bool)>();
            director.Resolved += (poi, how, known) => heard.Add((poi.Id, how, known));

            clock.AdvanceTicks(1199);
            Assert.AreEqual(PoiPhase.Open, director.Phase(_a));
            clock.AdvanceTicks(1);

            Assert.AreEqual(PoiPhase.Resolved, director.Phase(_a), "A's window ends at 20:00");
            Assert.AreEqual(PoiResolution.Unattended, director.Resolution(_a));
            var outcome = _a.Unattended.Single(o => o.name == director.Outcome(_a));
            Assert.IsTrue(state.IsSet(outcome.raises[0]), "the drawn outcome raises its Aftermath");
            var other = _a.Unattended.Single(o => o != outcome);
            Assert.IsFalse(state.IsSet(other.raises[0]), "the other outcome does not happen");
            CollectionAssert.AreEqual(new[] { ("a", PoiResolution.Unattended, false) }, heard, "nobody heard of it, so it ends silently");
        }

        [Test]
        public void TheSameSeedDrawsTheSameOutcomeAndSeedsDiffer()
        {
            var outcomes = new HashSet<string>();
            for (var seed = 1; seed <= 20; seed++)
            {
                var first = Resolve(seed);
                Assert.AreEqual(first, Resolve(seed), "a seed always draws the same outcome");
                outcomes.Add(first);
            }

            CollectionAssert.AreEquivalent(new[] { "wardens", "veinmarket" }, outcomes, "both outcomes happen across seeds");
        }

        [Test]
        public void CAppearsWhenAResolvesAndFadesAtItsEnd()
        {
            var (director, clock, state) = Run(0);

            clock.AdvanceTicks(1199);
            Assert.AreEqual(PoiPhase.Pending, director.Phase(_c));
            clock.AdvanceTicks(1);
            Assert.AreEqual(PoiPhase.Open, director.Phase(_c), "C follows A in the same tick");
            Assert.AreEqual(1200, state.Get("poi.c.opened_at"));

            clock.AdvanceTicks(2880 - 1200);
            Assert.AreEqual(PoiPhase.Resolved, director.Phase(_c));
            Assert.IsTrue(state.IsSet("aftermath.soot_stair.trace_faded"));
        }

        [Test]
        public void KnowledgeComesBeforeTheWindowAndNeverFalls()
        {
            var (director, clock, _) = Run(0);
            var known = false;
            director.Resolved += (poi, _, k) => known |= poi == _b && k;

            director.Reveal(_b, PoiDiscovery.Marked);
            director.Reveal(_b, PoiDiscovery.Hinted);
            Assert.AreEqual(PoiDiscovery.Marked, director.Discovery(_b), "the morning notice marks B before 23:00");
            Assert.AreEqual(PoiPhase.Pending, director.Phase(_b));

            clock.AdvanceTicks(1560);
            Assert.IsTrue(known, "a POI the player heard of is announced when it ends");
        }

        [Test]
        public void ThePlayersSceneEndsAPoiWithoutTheUnattendedAftermath()
        {
            var (director, clock, state) = Run(0);
            clock.AdvanceTicks(900);

            director.ResolveParticipated(_a);

            Assert.AreEqual(PoiResolution.Participated, director.Resolution(_a));
            Assert.IsNull(director.Outcome(_a));
            Assert.IsFalse(state.IsSet("aftermath.harrow.taken") || state.IsSet("aftermath.harrow.vanished"));
            Assert.AreEqual(PoiPhase.Open, director.Phase(_c), "C follows a participated A too");
            Assert.Throws<System.InvalidOperationException>(() => director.ResolveParticipated(_a));
        }

        [Test]
        public void ALoadedStateCatchesUpOnItsFirstEvaluation()
        {
            var (_, clock, state) = Run(0);
            clock.AdvanceTicks(1000);
            var saved = state.Capture();

            var loaded = new WorldState();
            loaded.Restore(saved);
            var later = new GameClock(1d, 1300);
            var director = new PoiDirector(new[] { _a, _b, _c }, loaded, new WorldSeed(Seed), SliceSeedStreams.PoiUnattended, later);
            director.Evaluate(later.Tick);

            Assert.AreEqual(PoiPhase.Resolved, director.Phase(_a), "A's window passed while the save lay unread");
            Assert.AreEqual(PoiPhase.Open, director.Phase(_c));
            Assert.AreEqual(PoiPhase.Pending, director.Phase(_b));
            director.Dispose();
        }

        [Test]
        public void TheSlicePoiAssetsKeepTheirTimes()
        {
            var a = AssetDatabase.LoadAssetAtPath<PoiDefinition>("Assets/Data/Slice/Poi/PoiA.asset");
            var b = AssetDatabase.LoadAssetAtPath<PoiDefinition>("Assets/Data/Slice/Poi/PoiB.asset");
            var c = AssetDatabase.LoadAssetAtPath<PoiDefinition>("Assets/Data/Slice/Poi/PoiC.asset");

            Assert.AreEqual(("a", 840, 360), (a.Id, a.AppearsAtMinute, a.WindowMinutes), "14:00, until the second-shift bell");
            Assert.AreEqual(("b", 1380, 180), (b.Id, b.AppearsAtMinute, b.WindowMinutes), "the eleventh bell, until about 02:00");
            Assert.AreEqual(("c", -1, 2880), (c.Id, c.AppearsAtMinute, c.ClosesAtMinute));
            CollectionAssert.AreEquivalent(new[] { a, b }, c.AppearsAfterAny);
            Assert.AreEqual(2, a.Unattended.Length);
            Assert.AreEqual(2, b.Unattended.Length);
            Assert.IsTrue(new[] { a, b, c }.All(p => p.Unattended.All(o => o.raises.Length > 0)), "every unattended outcome leaves Aftermath");
        }

        private string Resolve(int seed)
        {
            var state = new WorldState();
            var clock = new GameClock(1d, 0);
            using var director = new PoiDirector(new[] { _a }, state, new WorldSeed(seed), SliceSeedStreams.PoiUnattended, clock);
            clock.AdvanceTicks(1200);
            return director.Outcome(_a);
        }

        private (PoiDirector, GameClock, WorldState) Run(long start)
        {
            var state = new WorldState();
            var clock = new GameClock(1d, start);
            var director = new PoiDirector(new[] { _a, _b, _c }, state, new WorldSeed(Seed), SliceSeedStreams.PoiUnattended, clock);
            return (director, clock, state);
        }

        private PoiDefinition Make(string id, int appearsAt, PoiDefinition[] after, int window, int closesAt, params PoiOutcome[] outcomes)
        {
            var poi = ScriptableObject.CreateInstance<PoiDefinition>();
            poi.Configure(id, PoiWindowClass.Standard, appearsAt, after, window, closesAt, outcomes, Vector3.zero, 30f);
            _made.Add(poi);
            return poi;
        }
    }
}

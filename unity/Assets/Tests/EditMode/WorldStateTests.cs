using System;
using System.Collections.Generic;
using GameDev.Core.Save;
using GameDev.World.State;
using NUnit.Framework;

namespace GameDev.Slice.Tests
{
    /// <summary>The world state store (S1.1): what residue and Aftermath write, the world reads, and a save keeps.</summary>
    public sealed class WorldStateTests
    {
        [Test]
        public void AKeyNeverWrittenReadsAsZero()
        {
            var state = new WorldState();
            Assert.AreEqual(0, state.Get("poi.a.aftermath"));
            Assert.IsFalse(state.IsSet("poi.a.aftermath"));
            CollectionAssert.IsEmpty(state.Keys);
        }

        [Test]
        public void ValuesAndFlagsAreWrittenAndRead()
        {
            var state = new WorldState();
            state.Raise("poi.a.harrow_taken");
            state.Set("poi.b.girls_taken", 2);

            Assert.IsTrue(state.IsSet("poi.a.harrow_taken"));
            Assert.AreEqual(2, state.Get("poi.b.girls_taken"));
            CollectionAssert.AreEqual(new[] { "poi.a.harrow_taken", "poi.b.girls_taken" }, state.Keys);

            state.Set("poi.b.girls_taken", 0);
            CollectionAssert.AreEqual(new[] { "poi.a.harrow_taken" }, state.Keys, "a value set to zero leaves the store");
        }

        [Test]
        public void AChangeIsAnnouncedOnceWithItsOldAndNewValue()
        {
            var state = new WorldState();
            var heard = new List<(string, int, int)>();
            state.Changed += (k, o, n) => heard.Add((k, o, n));

            state.Set("residue.tincture_row.wardens", 3);
            state.Set("residue.tincture_row.wardens", 3);
            state.Set("residue.tincture_row.wardens", 1);

            CollectionAssert.AreEqual(new[] { ("residue.tincture_row.wardens", 0, 3), ("residue.tincture_row.wardens", 3, 1) }, heard);
        }

        [Test]
        public void AnEmptyKeyIsRefused()
        {
            var state = new WorldState();
            Assert.Throws<ArgumentException>(() => state.Set("", 1));
            Assert.Throws<ArgumentException>(() => state.Get(" "));
        }

        [Test]
        public void TheStateComesBackFromASaveWithoutAnnouncingIt()
        {
            var state = new WorldState();
            state.Raise("poi.a.harrow_taken");
            state.Set("poi.b.girls_taken", -1);
            var file = SaveAssembler.Capture(new ISaveContributor[] { new WorldStateSaveContributor(state) });

            var loaded = new WorldState();
            loaded.Raise("left.from.before");
            var announced = 0;
            loaded.Changed += (_, _, _) => announced++;
            var missing = SaveAssembler.Restore(file, new ISaveContributor[] { new WorldStateSaveContributor(loaded) });

            CollectionAssert.IsEmpty(missing);
            CollectionAssert.AreEqual(state.Keys, loaded.Keys, "the load replaces the whole store");
            Assert.AreEqual(1, loaded.Get("poi.a.harrow_taken"));
            Assert.AreEqual(-1, loaded.Get("poi.b.girls_taken"));
            Assert.AreEqual(0, announced, "a load is not an event in the world");
        }
    }
}

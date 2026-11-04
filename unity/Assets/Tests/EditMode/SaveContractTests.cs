using System;
using System.Linq;
using GameDev.Core.Save;
using GameDev.UI;
using GameDev.World.Timekeeping;
using NUnit.Framework;

namespace GameDev.Slice.Tests
{
    public sealed class SaveContractTests
    {
        private sealed class FakeContributor : ISaveContributor
        {
            public FakeContributor(string key, int version = 1)
            {
                SectionKey = key;
                SectionVersion = version;
            }

            public string SectionKey { get; }
            public int SectionVersion { get; }
            public string Payload = "state";
            public string Restored;

            public string CapturePayload() => Payload;

            public void RestorePayload(int version, string payload) => Restored = payload;
        }

        [Test]
        public void TheClockComesBackExactly()
        {
            var clock = new GameClock(10d, 480);
            clock.AdvanceRealTime(1234.5d);
            var file = SaveAssembler.Capture(new ISaveContributor[] { new GameClockSaveContributor(clock) });

            var restored = new GameClock(10d, 0);
            var missing = SaveAssembler.Restore(file, new ISaveContributor[] { new GameClockSaveContributor(restored) });

            CollectionAssert.IsEmpty(missing);
            Assert.AreEqual(clock.Tick, restored.Tick);
            Assert.AreEqual(clock.TickProgress, restored.TickProgress, 1e-9);
            Assert.AreEqual(SaveSchema.Version, file.schemaVersion);
        }

        [Test]
        public void SectionsAreWrittenInSchemaOrder()
        {
            var file = SaveAssembler.Capture(new ISaveContributor[]
            {
                new FakeContributor(SaveSchema.WorldInvasion), new FakeContributor(SaveSchema.WorldSeed),
            });

            CollectionAssert.AreEqual(new[] { SaveSchema.WorldSeed, SaveSchema.WorldInvasion }, file.sections.Select(s => s.key));
        }

        [Test]
        public void ASaveFromANewerBuildIsRefusedWhole()
        {
            var seed = new FakeContributor(SaveSchema.WorldSeed);
            var file = SaveAssembler.Capture(new ISaveContributor[] { seed, new FakeContributor(SaveSchema.WorldClock, 2) });

            var olderClock = new FakeContributor(SaveSchema.WorldClock);
            Assert.Throws<SaveVersionException>(() => SaveAssembler.Restore(file, new ISaveContributor[] { seed, olderClock }));
            Assert.IsNull(seed.Restored, "nothing is half-read");

            file.schemaVersion = SaveSchema.Version + 1;
            Assert.Throws<SaveVersionException>(() => SaveAssembler.Restore(file, new ISaveContributor[] { seed }));
        }

        [Test]
        public void AMissingSectionIsReportedAndTheRestRestored()
        {
            var file = SaveAssembler.Capture(new ISaveContributor[] { new FakeContributor(SaveSchema.WorldSeed) });
            var seed = new FakeContributor(SaveSchema.WorldSeed);
            var people = new FakeContributor(SaveSchema.WorldPeople);

            var missing = SaveAssembler.Restore(file, new ISaveContributor[] { seed, people });

            CollectionAssert.AreEqual(new[] { SaveSchema.WorldPeople }, missing);
            Assert.AreEqual("state", seed.Restored);
            Assert.IsNull(people.Restored);
        }

        [Test]
        public void UnknownAndDuplicateSectionsAreRefused()
        {
            Assert.Throws<ArgumentException>(() => SaveAssembler.Capture(new ISaveContributor[] { new FakeContributor("world.weather") }));
            Assert.Throws<ArgumentException>(() => SaveAssembler.Capture(new ISaveContributor[]
            {
                new FakeContributor(SaveSchema.WorldSeed), new FakeContributor(SaveSchema.WorldSeed),
            }));
            CollectionAssert.AllItemsAreUnique(SaveSchema.Sections);
        }

        [TestCase(1, 480, "Day 1 · 08:00")]
        [TestCase(2, 0, "Day 2 · 00:00")]
        [TestCase(2, 1439, "Day 2 · 23:59")]
        public void TheHudWritesDayAndTime(int day, int minute, string expected)
        {
            Assert.AreEqual(expected, HudText.DayAndTime(day, minute));
        }
    }
}

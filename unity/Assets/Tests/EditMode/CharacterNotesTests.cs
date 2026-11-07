using System.Collections.Generic;
using System.Linq;
using GameDev.World.Poi;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEditor;

namespace GameDev.Slice.Tests
{
    /// <summary>The Character Notes of S1.4: one line for each fact found, in order, in the character's words.</summary>
    public sealed class CharacterNotesTests
    {
        private const string Folder = "Assets/Data/Slice/Poi/";

        [Test]
        public void ATipsFactAppearsInTheNotesInTheOrderFound()
        {
            var (book, notes, clock, director, _) = Make(new WorldState());
            var added = new List<string>();
            notes.Added += added.Add;

            CollectionAssert.IsEmpty(notes.Lines);
            clock.AdvanceTicks(840 - 480);
            book.Read(TipSource.StreetRumour, clock.Tick);
            book.Read(TipSource.NoticeBoard, clock.Tick);
            book.Read(TipSource.NoticeBoard, clock.Tick);

            var tips = Catalogue().Tips.ToDictionary(t => t.id);
            var expected = new[] { "kit.drawers", "kit.coachcoats", "board.girls", "board.wren", "board.seizure" }.Select(id => tips[id].note).ToList();
            CollectionAssert.AreEqual(expected, notes.Lines);
            CollectionAssert.AreEqual(expected, added, "each fact is noted once, when found");
            director.Dispose();
        }

        [Test]
        public void EveryTipHasAFactInTheCharactersOwnWords()
        {
            foreach (var tip in Catalogue().Tips)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(tip.note), $"{tip.id} has a note");
                Assert.AreNotEqual(tip.line, tip.note, $"{tip.id}: the note is the character's words, not the source's");
            }
        }

        [Test]
        public void TheNotesComeBackWithASave()
        {
            var state = new WorldState();
            var (book, notes, clock, director, _) = Make(state);
            book.Read(TipSource.NoticeBoard, clock.Tick);
            var lines = notes.Lines;
            director.Dispose();

            var loaded = new WorldState();
            loaded.Restore(state.Capture());
            var (_, again, _, otherDirector, _) = Make(loaded);
            CollectionAssert.AreEqual(lines, again.Lines);
            otherDirector.Dispose();
        }

        private static TipCatalogue Catalogue() => AssetDatabase.LoadAssetAtPath<TipCatalogue>(Folder + "Tips.asset");

        private static (TipBook, CharacterNotes, GameClock, PoiDirector, WorldState) Make(WorldState state)
        {
            var pois = new[] { "PoiA", "PoiB", "PoiC" }.Select(n => AssetDatabase.LoadAssetAtPath<PoiDefinition>(Folder + n + ".asset")).ToArray();
            var clock = new GameClock(1d, 480);
            var director = new PoiDirector(pois, state, new WorldSeed(110201), SliceSeedStreams.PoiUnattended, clock);
            var book = new TipBook(Catalogue(), state, director);
            return (book, new CharacterNotes(book), clock, director, state);
        }
    }
}

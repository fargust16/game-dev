#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Locomotion;
using GameDev.UI;
using GameDev.World.Poi;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S2.0 in Veilmouth (17 X.7): the language key changes every word shown at once. That the HUD draws Russian was
    /// checked by eye in a render of it (2026-11-09): its labels use Unity's default runtime font, which code cannot
    /// read, and a project font, when one is chosen, brings its own glyph test.
    /// </summary>
    public sealed class LocalizationPlayTests : SliceInputTestBase
    {
        private const string City = "Assets/Scenes/Slice/Veilmouth.unity";
        private string _language;

        [SetUp]
        public void KeepLanguage()
        {
            _language = Loc.Language;
        }

        [TearDown]
        public void RestoreLanguage()
        {
            Loc.Language = _language;
        }

        [UnityTest]
        public IEnumerator TheLanguageKeyChangesEveryWordAtOnce()
        {
            Loc.Language = Loc.English;
            yield return LoadScene(City);
            var world = SliceWorldDriver.Running;
            if (world.Clock.Tick < 840)
            {
                world.Clock.AdvanceTicks((int)(840 - world.Clock.Tick));
            }

            var board = Object.FindObjectsByType<TipSourcePoint>(FindObjectsSortMode.None).Single(s => s.Source == TipSource.NoticeBoard);
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            player.Teleport(board.transform.position + (board.transform.forward * 1.5f) + (Vector3.up * 0.1f), player.transform.rotation);
            yield return new WaitForSeconds(0.3f);
            var hud = Object.FindAnyObjectByType<HudView>();
            var tables = Loc.Tables;

            // The world lives through the tests, so what the board says depends on the time earlier tests left: the test
            // checks that whatever it says changes language, line by line.
            yield return Tap(Key.E);
            Assert.AreEqual(tables.Get("source.board.title", Loc.English), hud.ReadingTitle);
            StringAssert.StartsWith("Day ", hud.TimeText);
            var english = hud.ReadingText;

            yield return Tap(Key.F10);

            Assert.AreEqual(Loc.Russian, Loc.Language, "F10 goes to the next language");
            Assert.AreEqual(tables.Get("source.board.title", Loc.Russian), hud.ReadingTitle, "an open panel follows the language");
            Assert.AreNotEqual(english, hud.ReadingText);
            foreach (var key in tables.Keys.Where(k => k.StartsWith("tip.board.") || k == "source.board.nothing"))
            {
                Assert.AreEqual(english.Contains(tables.Get(key, Loc.English)), hud.ReadingText.Contains(tables.Get(key, Loc.Russian)), key);
            }

            StringAssert.StartsWith("День ", hud.TimeText, "the clock is written again at once");

            yield return Tap(Key.E);
            yield return Tap(Key.J);
            Assert.IsTrue(hud.NoteLines.All(l => IsText(l, Loc.Russian)), string.Join(" | ", hud.NoteLines));

            yield return Tap(Key.F10);

            Assert.AreEqual(Loc.English, Loc.Language);
            Assert.IsTrue(hud.NoteLines.All(l => IsText(l, Loc.English)), "open Notes follow the language: " + string.Join(" | ", hud.NoteLines));

            yield return Tap(Key.M);
            var root = hud.GetComponent<UIDocument>().rootVisualElement;
            var map = root.Q<VisualElement>("map");
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(tables.Get("ui.hud.map.title", Loc.English), map.Q<Label>(className: "hud__notes-title").text);
            CollectionAssert.Contains(PlanWords(map), "Draught Lane");

            yield return Tap(Key.F10);
            yield return null;

            Assert.AreEqual(tables.Get("ui.hud.map.title", Loc.Russian), map.Q<Label>(className: "hud__notes-title").text);
            CollectionAssert.Contains(PlanWords(map), tables.Get("map.draught_lane", Loc.Russian), "the plan's names follow the language");
            CollectionAssert.DoesNotContain(PlanWords(map), "Draught Lane");
            foreach (var poi in world.Director.Pois)
            {
                Assert.IsFalse(hud.MapMarks.Any(m => m.Label.StartsWith(tables.Get(poi.TitleKey, Loc.English))), $"{poi.Id}: a mark's words follow the language");
            }

            yield return Tap(Key.M);
        }

        private static bool IsText(string text, string language) => Loc.Tables.Keys.Any(k => Loc.Tables.Get(k, language) == text);

        private static List<string> PlanWords(VisualElement map) => map.Q<CityMapElement>().Query<Label>().ToList().Select(l => l.text).ToList();
    }
}
#endif

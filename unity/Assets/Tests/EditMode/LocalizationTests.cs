using System.Linq;
using System.Xml.Linq;
using GameDev.Core.Localization;
using GameDev.Slice.EditorTools;
using GameDev.Slice.People;
using GameDev.UI;
using GameDev.World.Poi;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>S2.0 (17 X.7): every player-facing string of the slice is a key with its Russian and English text.</summary>
    public sealed class LocalizationTests
    {
        private const string PoiFolder = "Assets/Data/Slice/Poi/";

        [Test]
        public void TheTablesHaveRussianAndEnglishForEveryKey()
        {
            var tables = Loc.Tables;
            CollectionAssert.IsEmpty(tables.Problems, string.Join("\n", tables.Problems));
            CollectionAssert.IsSupersetOf(tables.Languages, new[] { Loc.English, Loc.Russian });
            Assert.Greater(tables.Keys.Count, 0);
            foreach (var key in tables.Keys)
            {
                foreach (var language in tables.Languages)
                {
                    Assert.IsTrue(tables.Has(key, language), $"'{key}' has no {language} text");
                }
            }
        }

        [Test]
        public void EveryTipPoiAndPersonHasItsKeys()
        {
            var tips = AssetDatabase.LoadAssetAtPath<TipCatalogue>(PoiFolder + "Tips.asset");
            Assert.AreEqual(58, tips.Tips.Length, "S1's ten, Nell's six lines of the next day (S2.7), POI C's eight Physical Traces (S4.1), the sweep's coach (S4.2), the black drop's note (S4.4), Nell's two lines by the tell proved (S4.5), Kit's word of the den (S5b.4), Чужой сон's twenty fragments (S5b.8), the trial's three traces and two notes (S5c.4) and the Hidden Caches' four notes (S5c.6)");
            foreach (var tip in tips.Tips)
            {
                Assert.IsTrue(Loc.Exists(tip.LineKey), tip.LineKey);
                Assert.IsTrue(Loc.Exists(tip.NoteKey), tip.NoteKey);
            }

            foreach (var name in new[] { "PoiA", "PoiB", "PoiC" })
            {
                var poi = AssetDatabase.LoadAssetAtPath<PoiDefinition>(PoiFolder + name + ".asset");
                Assert.IsTrue(Loc.Exists(poi.TitleKey), poi.TitleKey);
                Assert.IsTrue(Loc.Exists(poi.UrgencyKey), poi.UrgencyKey);
            }

            foreach (var entry in SliceRoster.All)
            {
                Assert.IsTrue(Loc.Exists(SliceRoster.NameKey(entry.Id)), SliceRoster.NameKey(entry.Id));
                Assert.IsTrue(Loc.Exists(SliceRoster.RoleKey(entry.Id)), SliceRoster.RoleKey(entry.Id));
            }
        }

        [Test]
        public void EveryTipSourceInTheScenesHasItsKeys()
        {
            var found = 0;
            foreach (var scene in new[] { SliceRoster.Veilmouth, SliceRoster.Tavern })
            {
                EditorSceneManager.OpenScene(SlicePeople.ScenePath(scene), OpenSceneMode.Single);
                foreach (var source in Object.FindObjectsByType<TipSourcePoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var data = new SerializedObject(source);
                    foreach (var field in new[] { "titleKey", "nothingKey" })
                    {
                        var key = data.FindProperty(field).stringValue;
                        Assert.IsTrue(Loc.Exists(key), $"{source.name} in {scene}: {field} '{key}'");
                    }

                    found++;
                }
            }

            Assert.AreEqual(1, found, "the board; the newsboy and the publican are talked to since S2.4");
        }

        [Test]
        public void EveryNameOnTheCityPlanHasAKey()
        {
            var plan = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/Slice/Map/VeilmouthC5Plan.xml");
            var names = XElement.Parse(plan.text).Descendants().Where(e => e.Name.LocalName == "text").Select(e => e.Value).Distinct().ToList();
            Assert.Greater(names.Count, 30);
            foreach (var name in names)
            {
                var key = CityMapElement.PlanKey(name);
                Assert.IsTrue(Loc.Exists(key), $"'{name}' needs {key}");
                Assert.AreEqual(name, Loc.Tables.Get(key, Loc.English), $"{key}: the English text is the plan's own name");
            }
        }

        [Test]
        public void EveryWordOfTheHudIsAKey()
        {
            var uxml = XElement.Parse(System.IO.File.ReadAllText("Assets/UI/Hud/Hud.uxml"));
            var labels = uxml.Descendants().Where(e => e.Name.LocalName == "Label").ToList();
            var localized = labels.Where(e => ((string)e.Attribute("class") ?? string.Empty).Split(' ').Contains("hud__loc")).ToList();
            Assert.Greater(localized.Count, 0);
            foreach (var label in localized)
            {
                Assert.IsTrue(Loc.Exists((string)label.Attribute("text")), (string)label.Attribute("text"));
            }

            foreach (var label in labels.Except(localized))
            {
                Assert.IsEmpty((string)label.Attribute("text") ?? string.Empty, $"'{label.Attribute("name")}' is written by HudView, so it carries no words");
            }
        }

        [Test]
        public void AMissingTextFallsBackToEnglishThenToTheKey()
        {
            var tables = StringTables.Parse(new[] { ("t", "key,en,ru\nboth,One,Один\nonly.en,Two,\n") });
            Assert.AreEqual("Один", tables.Get("both", "ru"));
            Assert.AreEqual("Two", tables.Get("only.en", "ru"), "a gap in Russian shows the English");
            Assert.AreEqual("nowhere", tables.Get("nowhere", "ru"), "a missing key shows itself");
            Assert.IsFalse(tables.Has("only.en", "ru"));
            Assert.IsFalse(tables.Has("nowhere", "en"));
        }

        [Test]
        public void TheTablesReadQuotedFieldsCommentsAndANewLanguageColumn()
        {
            var csv = "﻿key,en,ru,de\r\n# a note to the translator\r\n\r\nq,\"Commas, and \"\"quotes\"\"\",\"Два\nряда\",Zwei\r\n";
            var tables = StringTables.Parse(new[] { ("t", csv), ("u", "key,en,ru\nq,Again,Снова\nshort,One\n") });
            CollectionAssert.AreEqual(new[] { "en", "ru", "de" }, tables.Languages, "a new language is a new column");
            Assert.AreEqual("Commas, and \"quotes\"", tables.Get("q", "en"));
            Assert.AreEqual("Два\nряда", tables.Get("q", "ru"));
            Assert.AreEqual("Zwei", tables.Get("q", "de"));
            Assert.IsFalse(tables.Exists("# a note to the translator"));
            Assert.AreEqual(2, tables.Problems.Count, "a key twice, and a row short of fields: " + string.Join(" | ", tables.Problems));
        }

        [Test]
        public void TheLanguageIsKeptAndTheSwitchGoesRound()
        {
            var was = Loc.Language;
            var changes = 0;
            void Count() => changes++;
            Loc.Changed += Count;
            try
            {
                Loc.Language = Loc.English;
                changes = 0;
                Loc.Next();
                Assert.AreEqual(Loc.Russian, Loc.Language);
                Assert.AreEqual(Loc.Russian, PlayerPrefs.GetString(Loc.Setting), "kept as the machine setting");
                Assert.AreEqual("Доска объявлений", Loc.Get("source.board.title"));
                Loc.Next();
                Assert.AreEqual(Loc.English, Loc.Language);
                Assert.AreEqual(2, changes);
                Loc.Language = "xx";
                Assert.AreEqual(Loc.English, Loc.Language, "a language the tables lack is English");
            }
            finally
            {
                Loc.Changed -= Count;
                Loc.Language = was;
            }
        }
    }
}

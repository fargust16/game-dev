using System;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Slice.EditorTools;
using GameDev.Slice.Fights;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// The fight's hotbar (owner's play test, 2026-11-11): a slot for each action of the turn, with its icon, its words in
    /// both languages, its key and its AP; a click takes a usable action once, and a dimmed slot takes none.
    /// </summary>
    public sealed class FightHotbarTests
    {
        [Test]
        public void EverySlotHasItsWordsAndItsIcon()
        {
            foreach (var (_, id) in FightHotbar.Slots)
            {
                foreach (var key in new[] { $"ui.fight.action.{id}", $"ui.fight.action.{id}.tip", $"ui.fight.action.{id}.key" })
                {
                    Assert.IsTrue(Loc.Tables.Has(key, Loc.English) && Loc.Tables.Has(key, Loc.Russian), $"'{key}' has its Russian and English text");
                }
            }

            var settings = AssetDatabase.LoadAssetAtPath<SliceSettings>(SliceConfrontSetup.SettingsPath);
            Assert.AreEqual(FightHotbar.Slots.Length, settings.FightIcons.Length, "an icon for each slot");
            Assert.IsTrue(settings.FightIcons.All(i => i != null), "and none missing");
        }

        [Test]
        public void AClickTakesAUsableActionOnceAndADimmedOneNever()
        {
            var bar = new FightHotbar(null);
            var panel = new VisualElement();
            var row = new VisualElement();
            var tip = new Label();
            panel.Add(row);
            panel.Add(tip);
            bar.Bind(panel, row, tip);
            Assert.AreEqual(FightHotbar.Slots.Length, bar.Count, "a slot for each action");

            int Slot(FightAction a) => Array.FindIndex(FightHotbar.Slots, s => s.Action == a);
            bar.Refresh(a => (a != FightAction.Spare, a == FightAction.FirstAid ? 4 : 0));
            Assert.IsTrue(row[Slot(FightAction.Spare)].ClassListContains("hud__fight-slot--off"), "Spare with nobody asking mercy is dimmed");
            Assert.AreEqual("4", ((Label)row[Slot(FightAction.FirstAid)][2]).text, "First Aid shows its AP");

            bar.Click(Slot(FightAction.Spare));
            Assert.AreEqual(FightAction.None, bar.Take(), "a dimmed slot takes no click");
            bar.Click(Slot(FightAction.FirstAid));
            Assert.AreEqual(FightAction.FirstAid, bar.Take(), "a usable slot takes its action");
            Assert.AreEqual(FightAction.None, bar.Take(), "once");

            bar.Refresh(null);
            bar.Click(Slot(FightAction.EndTurn));
            Assert.AreEqual(FightAction.None, bar.Take(), "out of the player's turn every slot is dimmed");
        }
    }
}

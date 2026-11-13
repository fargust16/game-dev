#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Slice.Creation;
using GameDev.Slice.Mind;
using GameDev.Slice.Stealth;
using GameDev.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5b.6 (07.2 §3.3–3.4): in Veilmouth each band of Sanity shows its penalties and its symptoms, the page saying the
    /// band's words and never a number, Fatigue taking its share, and Sanity 0 ending the run.
    /// </summary>
    public sealed class MindPlayTests : SliceInputTestBase
    {
        private string _language;

        [SetUp]
        public void KeepLanguage()
        {
            _language = Loc.Language;
            Loc.Language = Loc.English;
        }

        [TearDown]
        public void RestoreLanguage()
        {
            Loc.Language = _language;
        }

        [UnityTest]
        public IEnumerator EachBandsPenaltiesAndSymptomsShow()
        {
            yield return ForgetTheWorld();
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var world = SliceWorldDriver.Running;
            Assert.IsNotNull(world?.Mind, "the world keeps the mind");
            Assert.IsNotNull(world.Symptoms);
            var hud = Object.FindAnyObjectByType<HudView>();
            var calm = world.Fights.PlayerFighter();
            Assert.AreEqual(SanityBand.Stable, world.Mind.Band);
            Assert.AreEqual(1d, world.Reaction.Narrowing(), 1e-9);

            yield return Page(hud, "Mind: clear", none: "Fatigue");

            SanityTo(world, 80);
            Provoke(world, MindSymptom.Whisper);
            yield return Page(hud, "Mind: mild disturbance");

            SanityTo(world, 50);
            var troubled = world.Fights.PlayerFighter();
            Assert.AreEqual(calm.Modifiers.accuracy - 5, troubled.Modifiers.accuracy, "−5 to hit");
            Assert.AreEqual(-1, troubled.CheckShift, "−1 on every check");
            Provoke(world, MindSymptom.Glimpse);
            Assert.IsTrue(world.Symptoms.GlimpseShown, "a figure at the edge of the screen");
            var look = Object.FindAnyObjectByType<GameDev.Cameras.ThirdPersonCamera>();
            look.Look(look.Yaw + 20f, look.Pitch);
            yield return null;
            yield return null;
            Assert.IsFalse(world.Symptoms.GlimpseShown, "gone when the camera turns");
            yield return Page(hud, "Mind: significant disturbance");

            SanityTo(world, 20);
            Assert.AreEqual(calm.Modifiers.willPower - 0.25f, world.Fights.PlayerFighter().Modifiers.willPower, 1e-6, "maximum Will Power −25 %");
            Provoke(world, MindSymptom.FalseAlarm);
            yield return null;
            yield return null;
            Assert.IsTrue(Object.FindAnyObjectByType<StealthHud>().ShowsPhantom, "a Notice Meter fills from nobody");

            SanityTo(world, 5);
            Assert.AreEqual(0.5d, world.Reaction.Narrowing(), 1e-9, "the zones half as wide");
            yield return new WaitForSecondsRealtime(1f);
            Assert.Greater(world.Symptoms.ColourDrain, 0.3f, "the colour drains from the world");
            Assert.AreEqual(1, world.Symptoms.Count(MindSymptom.Whisper), "a whisper, the first band's, came at its own pace");
            yield return Page(hud, "Mind: on the edge");

            world.Clock.AdvanceTicks(SliceMind.TiredHours * 60);
            Assert.AreEqual(FatigueState.Tired, world.Mind.Fatigue);
            Assert.AreEqual(0.4d, world.Reaction.Narrowing(), 1e-9, "Critical's half of Tired's four fifths");
            yield return Page(hud, "Fatigue: tired");

            world.Sanity.Lose(10);
            yield return null;
            Assert.IsTrue(Fights.SliceDeathScreen.Shown, "Sanity 0 ends the run");
            Assert.AreEqual(Fights.SliceDeathScreen.SanityTitle, Fights.SliceDeathScreen.ShownTitle);
            Object.Destroy(Object.FindAnyObjectByType<Fights.SliceDeathScreen>().gameObject);
            yield return null;
        }

        private static void SanityTo(SliceWorldDriver world, int value) => world.Sanity.Lose(world.Sanity.Value - value);

        private static void Provoke(SliceWorldDriver world, MindSymptom symptom)
        {
            var before = world.Symptoms.Count(symptom);
            world.Symptoms.Provoke(symptom);
            Assert.AreEqual(before + 1, world.Symptoms.Count(symptom), symptom.ToString());
        }

        /// <summary>Opens the Character Page on L, finds <paramref name="line"/>, and closes it.</summary>
        private IEnumerator Page(HudView hud, string line, string none = null)
        {
            yield return Tap(Key.L);
            yield return null;
            var lines = hud.ShopLines.ToList();
            Assert.That(lines, Has.Some.EqualTo(line));
            Assert.That(lines, Has.None.Matches<string>(l => l.Any(char.IsDigit) && l.StartsWith("Mind")), "never a number");
            if (none != null)
            {
                Assert.That(lines, Has.None.StartsWith(none));
            }

            yield return Tap(Key.L);
            yield return null;
        }
    }
}
#endif

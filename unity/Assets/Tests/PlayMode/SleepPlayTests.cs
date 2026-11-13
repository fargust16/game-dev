#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using GameDev.Confront.Rules;
using GameDev.Core.Localization;
using GameDev.Core.Scenes;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.Beasts;
using GameDev.Slice.Fights;
using GameDev.Slice.Mind;
using GameDev.Slice.Save;
using GameDev.UI;
using GameDev.World.Portals;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5c.2 (18 §3, 11 *Sleep*) in Veilmouth: Z in a street opens the page, and a sleep of 6 hours passes with the world
    /// going on, clears Fatigue, gives back Sanity, Will Power and HP, while a thief takes half the coins; outside the walls
    /// a beast finds the sleeper and the fight opens; the Soot &amp; Bell's room is paid, safe, and autosaves the run.
    /// </summary>
    public sealed class SleepPlayTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";

        private string _language;
        private float _speed;

        [SetUp]
        public void Prepare()
        {
            _language = Loc.Language;
            Loc.Language = Loc.English;
            _speed = SliceTuning.Current.SleepTicksPerSecond;
            SliceTuning.Current.SleepTicksPerSecond = 2000f;
            SliceSaveSlot.Path = Path.Combine(Application.temporaryCachePath, "sleep-test.json");
            SliceSaveSlot.Delete();
        }

        [TearDown]
        public void Restore()
        {
            Loc.Language = _language;
            SliceTuning.Current.SleepTicksPerSecond = _speed;
            var world = SliceWorldDriver.Running;
            if (world != null && world.Sleep != null)
            {
                world.Sleep.VisitChance = null;
            }

            SliceSaveSlot.Delete();
            SliceSaveSlot.Path = null;
        }

        [UnityTest]
        public IEnumerator ASleepInTheStreetPassesSixHoursAndAThiefTakesHalfTheCoins()
        {
            yield return Arrive();
            var world = SliceWorldDriver.Running;
            var hud = Object.FindAnyObjectByType<HudView>();
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            Assert.IsTrue(SliceSleep.InsideWalls(player.transform.position), "the run starts inside the walls");
            world.Fights.Initiate(Arcana.Priestess, 9);
            world.State.Set(SliceConfront.WillPowerLostKey, 500);
            world.State.Set(SliceConfront.HpLostKey, 400);
            world.State.Set(SliceMind.AwakeKey, 20 * 60);
            world.Mind.Sanity.Lose(10);
            var sanity = world.Mind.Sanity.Value;
            var bronze = world.Bag.Bronze;
            Assert.AreEqual(FatigueState.Tired, world.Mind.Fatigue);
            world.Sleep.VisitChance = 1d;

            yield return Tap(Key.Z);
            Assert.IsTrue(world.Sleep.IsPageOpen, "Z opens the page of sleep");
            Assert.AreEqual(Loc.Get("ui.sleep.title"), hud.ShopTitle);
            CollectionAssert.Contains(hud.ShopLines.ToList(), Loc.Get("ui.sleep.heading.street"));
            var six = hud.ShopLines.ToList().IndexOf(Loc.Format("ui.sleep.six", "6"));
            Assert.Greater(six, 0);
            var start = world.Clock.Tick;
            Assert.IsTrue(hud.ClickShopLine(six));
            Assert.IsTrue(world.Sleep.Sleeping && world.Mind.Asleep, "asleep");
            Assert.IsFalse(world.Sleep.IsPageOpen);
            Assert.AreEqual("ui.save.refused.sleep", world.Save.RefusalKey(), "no save while asleep");
            Assert.IsFalse(player.enabled, "the sleeper lies still");

            for (var t = 0f; t < 10f && world.Sleep.Sleeping; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsFalse(world.Sleep.Sleeping);
            Assert.AreEqual(start + 360, world.Clock.Tick, "six hours of the world's ticks");
            Assert.AreEqual(0, world.Mind.MinutesAwake, "a full sleep clears Fatigue");
            Assert.AreEqual(System.Math.Min(100, sanity + SliceMind.SleepSanity), world.Mind.Sanity.Value, "and gives back 3 Sanity");
            Assert.AreEqual(0, world.State.Get(SliceConfront.WillPowerLostKey), "Will Power restored");
            Assert.AreEqual(0, world.State.Get(SliceConfront.HpLostKey), "HP restored, no condition blocking it");
            Assert.AreEqual(bronze - SliceSleep.ThiefTakes(bronze), world.Bag.Bronze, "the thief took half the coins");
            StringAssert.Contains(Loc.Format("ui.sleep.robbed", GameDev.Items.Coins.Show(SliceSleep.ThiefTakes(bronze))), hud.MessageText);
            Assert.IsTrue(player.enabled, "awake and on its feet");
            Assert.IsTrue(SliceSaveSlot.Exists, "the game autosaves after a full sleep (17 X.3)");
        }

        [UnityTest]
        public IEnumerator ABeastFindsASleeperOutsideTheWalls()
        {
            yield return Arrive();
            var world = SliceWorldDriver.Running;
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var beasts = world.Beasts;
            for (var t = 0f; t < 5f && beasts.Beasts.Count == 0; t += Time.deltaTime)
            {
                yield return null;
            }

            var wolf = beasts.Beasts.First(b => b.Kind == BeastKind.Wolf && b.isActiveAndEnabled && b.State == BeastState.Resting && !beasts.GlowingPacks.Contains(b.Pack));
            Assert.IsTrue(Away(wolf.Home, beasts.Rules.TerritoryRadius + 15f, out var at), "a quiet place outside the walls, beyond the pack's territory");
            player.Teleport(at + (Vector3.up * 0.05f), Quaternion.identity);
            yield return null;
            yield return null;
            Assert.IsFalse(SliceSleep.InsideWalls(player.transform.position));
            world.Sleep.VisitChance = 1d;
            var start = world.Clock.Tick;
            Assert.IsNull(world.Sleep.Begin(SliceSleep.Outdoors(player.transform.position), SleepLength.SixHours));
            for (var t = 0f; t < 10f && !world.Fights.InFight; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsTrue(world.Fights.InFight, "the beast's attack opens a fight");
            Assert.IsFalse(world.Sleep.Sleeping || world.Mind.Asleep, "the sleeper woke");
            Assert.Less(world.Clock.Tick - start, 360, "cut short");
        }

        [UnityTest]
        public IEnumerator TheLetRoomIsPaidAndSafe()
        {
            yield return Arrive();
            var world = SliceWorldDriver.Running;
            var door = Object.FindObjectsByType<Portal>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).First(p => p.Id == "soot-and-bell.street");
            Assert.IsTrue(SceneTransition.Travel(door.Target));
            while (SceneTransition.IsTraveling)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(SliceWorldDriver.TavernScene, SceneManager.GetActiveScene().name);
            var bed = Object.FindAnyObjectByType<SleepBed>();
            Assert.IsNotNull(bed, "the let room's bed is rented");
            yield return Tap(Key.Z);
            Assert.IsFalse(world.Sleep.IsPageOpen, "indoors Z only says where one may sleep");

            Assert.IsTrue(world.Sleep.OpenPage(SleepPlace.Room));
            var hud = Object.FindAnyObjectByType<HudView>();
            CollectionAssert.Contains(hud.ShopLines.ToList(), Loc.Format("ui.sleep.heading.room", GameDev.Items.Coins.Show(SliceSleep.RoomPrice)));
            world.Sleep.ClosePage();
            world.Sleep.VisitChance = 1d;
            var bronze = world.Bag.Bronze;
            var start = world.Clock.Tick;
            var end = SliceSleep.EndOf(start, SleepLength.Morning);
            Assert.IsNull(world.Sleep.Begin(SleepPlace.Room, SleepLength.Morning));
            Assert.AreEqual(bronze - SliceSleep.RoomPrice, world.Bag.Bronze, "paid as the character lies down");
            for (var t = 0f; t < 15f && world.Sleep.Sleeping; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.AreEqual(end, world.Clock.Tick, "until 07:00");
            Assert.AreEqual(7 * 60, (int)(world.Clock.Tick % 1440));
            Assert.AreEqual(bronze - SliceSleep.RoomPrice, world.Bag.Bronze, "nobody comes to a tavern room");
            Assert.IsFalse(world.Fights.InFight);

            world.Bag.Spend(world.Bag.Bronze);
            Assert.AreEqual("ui.shop.fault.money", world.Sleep.Begin(SleepPlace.Room, SleepLength.SixHours), "no coin, no room");
        }

        private static IEnumerator Arrive()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            for (var t = 0f; t < 2f && !player.IsGrounded; t += Time.deltaTime)
            {
                yield return null;
            }
        }

        /// <summary>Dry ground outside the walls, <paramref name="metres"/> from <paramref name="home"/>.</summary>
        private static bool Away(Vector3 home, float metres, out Vector3 at)
        {
            for (var i = 0; i < 16; i++)
            {
                var angle = i * Mathf.PI / 8f;
                var probe = home + (new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * metres);
                if (!Physics.Raycast(probe + (Vector3.up * 40f), Vector3.down, out var hit, 80f, ~0, QueryTriggerInteraction.Ignore)
                    || hit.point.y < 24.5f || Vector3.Angle(hit.normal, Vector3.up) > 20f || SliceSleep.InsideWalls(hit.point))
                {
                    continue;
                }

                at = hit.point;
                return true;
            }

            at = default;
            return false;
        }
    }
}
#endif

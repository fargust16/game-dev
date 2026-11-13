#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Cameras;
using GameDev.Core.Scenes;
using GameDev.Locomotion;
using GameDev.Slice.People;
using GameDev.World.Portals;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5c.3 (SLICE_PEOPLE §4; 18 *Townsfolk in the slice*) with the hours kept: shopkeepers stand at their counters and
    /// their shops sell only in their working hours, Kit Mallow stands at the Soot &amp; Bell's door in the evening, the pub
    /// fills with the evening's drinkers and empties when Nell Garrow sleeps, fewer walk at night, a person the player
    /// watches waits before leaving, and a fallen person never comes back.
    /// </summary>
    public sealed class PersonHoursPlayTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";

        [SetUp]
        public void On() => PersonHours.Live = true;

        [TearDown]
        public void Off() => PersonHours.Live = false;

        [UnityTest]
        public IEnumerator ShopkeepersKeepTheirHours()
        {
            yield return Arrive();
            var world = SliceWorldDriver.Running;
            Assert.AreEqual(8 * 60, world.Clock.MinuteOfDay, "the run starts at 08:00");
            Assert.IsTrue(Post("tobias", SliceHours.Work).gameObject.activeSelf, "Tobias Penn at his stall");
            Assert.IsTrue(world.Shops.IsOpenFor("tobias"));
            Assert.IsTrue(Post(SliceHours.MarketTrader, SliceHours.Work).gameObject.activeSelf, "the market trader at the food stall");
            Assert.IsFalse(Post("kettle", SliceHours.Work).gameObject.activeSelf, "the pawnbroker opens at 09:00");
            Assert.IsFalse(world.Shops.IsOpenFor("kettle"));
            Assert.AreEqual(9, world.Shops.Opens("kettle"), "his closed sign says nine");

            yield return To(world, 9 * 60);
            Assert.IsTrue(Post("kettle", SliceHours.Work).gameObject.activeSelf);
            Assert.IsTrue(world.Shops.IsOpenFor("kettle"), "Mortimer Kettle's counter opens");

            yield return To(world, (19 * 60) + 30);
            Assert.IsFalse(Post("kit", SliceHours.Work).gameObject.activeSelf, "Kit Mallow leaves the market at 19:00");
            var door = Post("kit", SliceHours.TavernDoor);
            Assert.IsTrue(door.gameObject.activeSelf, "and stands at the Soot & Bell's door");
            Assert.IsNotNull(door.GetComponent<PersonTalk>(), "where she is talked to as at the market");
            Assert.IsFalse(Post("kettle", SliceHours.Work).gameObject.activeSelf, "the pawnbroker shut at 19:00");
            Assert.IsFalse(world.Shops.IsOpenFor("kettle"));
            Assert.IsFalse(Post(SliceHours.MarketTrader, SliceHours.Work).gameObject.activeSelf, "the market trader went at 18:00");

            yield return To(world, (20 * 60) + 30);
            Assert.IsFalse(Post("tobias", SliceHours.Work).gameObject.activeSelf, "Tobias Penn went to the pub at 20:00");
            Assert.IsFalse(world.Shops.IsOpenFor("tobias"));
            Assert.IsTrue(Post("ilse", SliceHours.Forecourt).gameObject.activeSelf, "Madame Ilse on the old chapel's forecourt");
            Assert.IsFalse(Post("ilse", SliceHours.Work).gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator TheSootAndBellFillsInTheEveningAndEmptiesWhenNellSleeps()
        {
            yield return Arrive();
            var world = SliceWorldDriver.Running;
            yield return To(world, (20 * 60) + 30);
            var door = Object.FindObjectsByType<Portal>(FindObjectsInactive.Exclude).First(p => p.Id == "soot-and-bell.street");
            Assert.IsTrue(SceneTransition.Travel(door.Target));
            while (SceneTransition.IsTraveling)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(SliceWorldDriver.TavernScene, SceneManager.GetActiveScene().name);
            foreach (var person in new[] { "pip", "silas", "tobias", "lottie", "dace" })
            {
                Assert.IsTrue(Post(person, SliceHours.Tavern).gameObject.activeSelf, $"{person} drinks at the Soot & Bell at 20:30");
            }

            Assert.IsTrue(Post("nell", SliceHours.Work).gameObject.activeSelf, "Nell Garrow serves");
            var drinkers = Object.FindObjectsByType<TownsfolkWalker>(FindObjectsInactive.Include)
                .Where(w => w.gameObject.scene.name == SliceWorldDriver.TavernScene && !w.Roams && w.Home != null).ToList();
            Assert.Greater(drinkers.Count, 0, "the pub's nameless drinkers");
            Assert.IsTrue(drinkers.All(w => w.gameObject.activeSelf), "they drink while Nell serves");

            yield return To(world, 3 * 60);
            Assert.IsFalse(Post("nell", SliceHours.Work).gameObject.activeSelf, "Nell Garrow sleeps from 02:00");
            foreach (var person in new[] { "pip", "silas", "tobias", "lottie", "dace" })
            {
                Assert.IsFalse(Post(person, SliceHours.Tavern).gameObject.activeSelf, $"{person} has gone by 03:00");
            }

            Assert.IsTrue(drinkers.All(w => !w.gameObject.activeSelf), "nobody drinks in a shut pub");
        }

        [UnityTest]
        public IEnumerator FewerWalkAtNight()
        {
            yield return Arrive();
            var world = SliceWorldDriver.Running;
            var roamers = Object.FindObjectsByType<TownsfolkWalker>(FindObjectsInactive.Include).Where(w => w.Roams && w.GetComponent<FallenBody>() == null).ToList();
            var day = roamers.Count(w => w.gameObject.activeSelf);
            Assert.Greater(day, 20, "Veilmouth's walkers by day");

            yield return To(world, 23 * 60);
            var night = roamers.Count(w => w.gameObject.activeSelf);
            Assert.Less(night, day * 0.6f, $"{night} of {day} walk at 23:00");
            foreach (var walker in roamers.Where(w => w.gameObject.activeSelf))
            {
                Assert.IsTrue(!SliceHours.HomeAtNight(walker.RouteSeed) || !walker.FreeToGoHome, $"{walker.name} walks on through the night, or is not free to go home yet");
            }

            yield return To(world, (6 * 60) + 30);
            Assert.GreaterOrEqual(roamers.Count(w => w.gameObject.activeSelf), day, "every walker is out again by 06:30");
        }

        [UnityTest]
        public IEnumerator AWatchedPersonWaitsBeforeLeaving()
        {
            yield return Arrive();
            var world = SliceWorldDriver.Running;
            var tobias = Post("tobias", SliceHours.Work);
            var camera = UnityEngine.Camera.main;
            Object.FindAnyObjectByType<ThirdPersonCamera>().enabled = false;
            var face = tobias.transform.position + (Vector3.up * 1.2f);
            camera.transform.position = face + (tobias.transform.forward * 4f) + (Vector3.up * 0.4f);
            camera.transform.LookAt(face);
            yield return null;

            world.Clock.AdvanceTicks((20 * 60) - world.Clock.MinuteOfDay + 1);
            yield return null;
            yield return null;
            Assert.IsTrue(tobias.gameObject.activeSelf, "Tobias Penn, watched, waits past 20:00");
            Assert.Greater(world.Hours.Waiting, 0);

            world.Clock.AdvanceTicks(SliceTuning.Current.ScheduleWaitTicks);
            yield return null;
            yield return null;
            Assert.IsFalse(tobias.gameObject.activeSelf, $"and goes after {SliceTuning.Current.ScheduleWaitTicks} ticks of being watched");
        }

        [UnityTest]
        public IEnumerator AFallenPersonIsGoneForGood()
        {
            yield return Arrive();
            var world = SliceWorldDriver.Running;
            var tobias = Post("tobias", SliceHours.Work);
            FallenBody.Mark(tobias.transform, true, 0f);
            world.Hours.Apply(true);
            Assert.IsTrue(world.Hours.Gone("tobias"), "a fallen person is gone once out of a fight");
            Assert.IsTrue(world.State.IsSet(PersonHours.GoneKey("tobias")), "in the world's state, so a save keeps it");
            Assert.IsFalse(world.Shops.IsOpenFor("tobias"), "his stall sells no more");

            yield return To(world, 20 * 60);
            yield return To(world, 8 * 60);
            Assert.IsFalse(world.Hours.AtWork("tobias"), "not at work the next morning");
            Assert.IsFalse(world.Shops.IsOpenFor("tobias"), "and his stall still sells nothing");
        }

        /// <summary>The body of <paramref name="person"/> at <paramref name="post"/> in the loaded scenes.</summary>
        private static PersonPost Post(string person, string post)
        {
            var found = Object.FindObjectsByType<PersonPost>(FindObjectsInactive.Include).FirstOrDefault(p => p.Person == person && p.Post == post);
            Assert.IsNotNull(found, $"{person} at {post}");
            return found;
        }

        /// <summary>The clock on to <paramref name="minute"/> of the day, and every person where the hour wants it.</summary>
        private static IEnumerator To(SliceWorldDriver world, int minute)
        {
            world.Clock.AdvanceTicks((((minute - world.Clock.MinuteOfDay) % 1440) + 1440) % 1440);
            yield return null;
            world.Hours.Apply(true);
            yield return null;
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
    }
}
#endif

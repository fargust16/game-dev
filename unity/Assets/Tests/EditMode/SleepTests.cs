using GameDev.Slice.Mind;
using GameDev.World.Seed;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5c.2 (18 §3, 11 *Sleep*): when a sleep ends, until 07:00 or for 6 hours, and which is full; where Veilmouth's walls
    /// stand; and who comes to a sleeper, one draw a sleep: a thief one street sleep in three, a beast one wild sleep in two.
    /// </summary>
    public sealed class SleepTests
    {
        private const int Day = 1440;

        [Test]
        public void ASleepEndsAtSevenOrAfterSixHours()
        {
            var tenAtNight = 22 * 60;
            Assert.AreEqual(Day + (7 * 60), SliceSleep.EndOf(tenAtNight, SleepLength.Morning), "the next 07:00");
            Assert.AreEqual(tenAtNight + 360, SliceSleep.EndOf(tenAtNight, SleepLength.SixHours));
            Assert.AreEqual(7 * 60, SliceSleep.EndOf((7 * 60) - 1, SleepLength.Morning), "a minute before 07:00");
            Assert.AreEqual(Day + (7 * 60), SliceSleep.EndOf(7 * 60, SleepLength.Morning), "at 07:00, the next one");
            Assert.AreEqual((2 * Day) + (7 * 60), SliceSleep.EndOf(Day + (23 * 60), SleepLength.Morning), "on day 2");
        }

        [Test]
        public void SixHoursOrMoreIsAFullSleep()
        {
            Assert.IsTrue(SliceSleep.Full(360));
            Assert.IsFalse(SliceSleep.Full(359));
            var threeAtNight = 3 * 60;
            Assert.IsFalse(SliceSleep.Full(SliceSleep.EndOf(threeAtNight, SleepLength.Morning) - threeAtNight), "03:00 until 07:00 is not full (S5-6)");
        }

        [Test]
        public void TheWallsCloseTheCityAndTheWorks()
        {
            Assert.IsTrue(SliceSleep.InsideWalls(new Vector3(40f, 30f, 150f)), "the statue on the Market square, plan (540, 350)");
            Assert.IsTrue(SliceSleep.InsideWalls(new Vector3(0f, 25f, -150f)), "the works, plan (500, 650)");
            Assert.IsFalse(SliceSleep.InsideWalls(new Vector3(40f, 30f, 300f)), "the forest north of the wall, plan y 200");
            Assert.IsFalse(SliceSleep.InsideWalls(new Vector3(-200f, 25f, 100f)), "the West road's farms, plan x 300");
            Assert.IsFalse(SliceSleep.InsideWalls(new Vector3(0f, 25f, -250f)), "south of the works' wall, plan y 750");
            Assert.AreEqual(SleepPlace.Street, SliceSleep.Outdoors(new Vector3(40f, 30f, 150f)));
            Assert.AreEqual(SleepPlace.Wild, SliceSleep.Outdoors(new Vector3(40f, 30f, 300f)));
        }

        [Test]
        public void AVisitorComesOnceASleepAsOftenAsThePlaceSays()
        {
            var seed = new WorldSeed(110201);
            Assert.AreEqual(SliceSleep.Visit(seed, SleepPlace.Street, 1320, 1860), SliceSleep.Visit(seed, SleepPlace.Street, 1320, 1860), "the seed decides");
            int street = 0, wild = 0;
            const int sleeps = 3000;
            for (var start = 0; start < sleeps; start++)
            {
                Assert.IsNull(SliceSleep.Visit(seed, SleepPlace.Room, start, start + 480), "a tavern room is safe");
                if (SliceSleep.Visit(seed, SleepPlace.Street, start, start + 480) is long thief)
                {
                    street++;
                    Assert.That(thief, Is.GreaterThan((long)start).And.LessThan((long)start + 480), "within the sleep");
                }

                if (SliceSleep.Visit(seed, SleepPlace.Wild, start, start + 480) is long beast)
                {
                    wild++;
                    Assert.That(beast, Is.GreaterThan((long)start).And.LessThan((long)start + 480));
                }
            }

            Assert.AreEqual(1d / 3d, street / (double)sleeps, 0.03d, "a thief about one night in three");
            Assert.AreEqual(1d / 2d, wild / (double)sleeps, 0.03d, "a beast one sleep in two");
            Assert.IsNotNull(SliceSleep.Visit(seed, SleepPlace.Street, 5, 365, 1d), "a test's sure visit");
            Assert.IsNull(SliceSleep.Visit(seed, SleepPlace.Street, 5, 365, 0d));
        }

        [Test]
        public void TheThiefTakesHalfTheCoins()
        {
            Assert.AreEqual(250, SliceSleep.ThiefTakes(501));
            Assert.AreEqual(0, SliceSleep.ThiefTakes(1));
            Assert.AreEqual(0, SliceSleep.ThiefTakes(0));
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.EditorTools;
using GameDev.Slice.People;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5c.3 (SLICE_PEOPLE §4; 18 *Townsfolk in the slice*): the people's hours as a table, every post of it standing once
    /// in its scene, and the night's share of walkers at home.
    /// </summary>
    public sealed class SliceHoursTests
    {
        [Test]
        public void EveryPersonKeepsADayAndItWrapsPastMidnight()
        {
            foreach (var entry in SliceRoster.All)
            {
                Assert.IsTrue(SliceHours.Keeps(entry.Id), $"{entry.Name} keeps hours");
            }

            Assert.IsTrue(SliceHours.Keeps(SliceHours.MarketTrader), "and the nameless market trader");
            Assert.AreEqual(SliceRoster.All.Count + 1, SliceHours.Days.Count);
            foreach (var pair in SliceHours.Days)
            {
                for (var minute = 0; minute < 1440; minute += 5)
                {
                    Assert.LessOrEqual(pair.Value.Count(s => s.Holds(minute)), 1, $"{pair.Key}: one stint at a time, {minute / 60:00}:{minute % 60:00}");
                }

                Assert.IsTrue(pair.Value.Any(s => s.Asleep), $"{pair.Key} sleeps");
            }

            Assert.AreEqual(SliceHours.Work, SliceHours.PostAt("nell", 60), "Nell serves past midnight");
            Assert.IsNull(SliceHours.PostAt("nell", 3 * 60));
            Assert.IsTrue(SliceHours.Asleep("nell", 3 * 60), "and sleeps from 02:00 to 10:00");
            Assert.AreEqual(SliceHours.Lane, SliceHours.PostAt("dace", 2 * 60), "Dace's lane runs from 23:00 to 05:00");
            Assert.AreEqual(SliceHours.Tavern, SliceHours.PostAt("pip", 21 * 60));
            Assert.AreEqual(SliceHours.TavernDoor, SliceHours.PostAt("kit", 20 * 60));
            Assert.AreEqual(SliceHours.Forecourt, SliceHours.PostAt("ilse", (20 * 60) + 30));
            Assert.IsNull(SliceHours.PostAt("lottie", (17 * 60) + 45), "an hour no stint covers is spent at home");
            Assert.IsFalse(SliceHours.Asleep("lottie", (17 * 60) + 45));
            Assert.AreEqual(SliceHours.Work, SliceHours.PostAt("kettle", (9 * 60) + 1440), "a minute past a day wraps");

            Assert.AreEqual(10, SliceHours.WorkOpens("nell"));
            Assert.AreEqual(9, SliceHours.WorkOpens("kettle"));
            Assert.IsNull(SliceHours.WorkOpens("lottie"), "no work post");
            Assert.IsNull(SliceHours.WorkOpens("nobody"));

            Assert.IsTrue(SliceHours.IsNight(SliceHours.NightFrom) && SliceHours.IsNight((5 * 60) + 59));
            Assert.IsFalse(SliceHours.IsNight(SliceHours.NightTo) || SliceHours.IsNight(12 * 60));
            CollectionAssert.AreEqual(new[] { false, true, true, false }, new uint[] { 0, 1, 2, 3 }.Select(SliceHours.HomeAtNight).ToArray());
            Assert.AreEqual("nell", SliceHours.PlaceKeeper(SliceRoster.Tavern));
            Assert.AreEqual("hale", SliceHours.PlaceKeeper(SliceRoster.Chapel));
            Assert.IsNull(SliceHours.PlaceKeeper(SliceRoster.Veilmouth));
        }

        /// <summary>
        /// Every post of the table stands once, in its scene, on a floor: the roster's body at the primary post, saved on,
        /// and a body of the same person at each other post, saved off.
        /// </summary>
        [Test]
        public void EveryPostStandsOnceInItsScene()
        {
            var expected = SliceHours.Days.SelectMany(p => SliceHours.PostsOf(p.Key).Select(post => $"{p.Key}@{post}")).ToList();
            var found = new List<string>();
            foreach (var scene in SliceRoster.All.Select(e => e.Scene).Distinct())
            {
                EditorSceneManager.OpenScene(SlicePeople.ScenePath(scene), OpenSceneMode.Single);
                Physics.SyncTransforms();
                foreach (var post in Object.FindObjectsByType<PersonPost>(FindObjectsInactive.Include))
                {
                    var name = $"{post.Person}@{post.Post}";
                    found.Add(name);
                    Assert.AreEqual(SliceHoursPosts.SceneOf(post.Person, post.Post), scene, $"{name} stands in its scene");
                    Assert.AreEqual(post.Post == SliceHoursPosts.PrimaryPost(post.Person), post.Primary, $"{name}: the roster's body is the primary post");
                    Assert.AreEqual(post.Primary, post.gameObject.activeSelf, $"{name}: a primary post saved on, any other off");
                    if (post.Person != SliceHours.MarketTrader)
                    {
                        Assert.AreEqual(post.Person, post.GetComponent<NamedPerson>()?.Id, $"{name} is the person");
                    }

                    Assert.IsTrue(Physics.Raycast(post.transform.position + Vector3.up, Vector3.down, 1.3f, ~LayerMask.GetMask("Character")),
                        $"{name} stands on a floor at {post.transform.position}");
                }
            }

            CollectionAssert.AreEquivalent(expected, found, "each post of the table once");
        }

        /// <summary>Two walkers in three are at home at night (authoring default), by their route's seed.</summary>
        [Test]
        public void TheNightKeepsTwoWalkersInThreeAtHome()
        {
            EditorSceneManager.OpenScene(SlicePeople.ScenePath(SliceRoster.Veilmouth), OpenSceneMode.Single);
            var roamers = Object.FindObjectsByType<TownsfolkWalker>(FindObjectsInactive.Include).Where(w => w.Roams).ToList();
            Assert.Greater(roamers.Count, 20, "Veilmouth's walkers");
            var home = roamers.Count(w => SliceHours.HomeAtNight(w.RouteSeed)) / (double)roamers.Count;
            Assert.That(home, Is.InRange(0.5, 0.8), $"{home:P0} of {roamers.Count} walkers at home at night");
        }
    }
}

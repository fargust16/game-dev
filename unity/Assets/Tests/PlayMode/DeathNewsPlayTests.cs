#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using GameDev.Slice.Fights;
using GameDev.Slice.News;
using GameDev.Slice.People;
using GameDev.UI;
using GameDev.World.Poi;
using GameDev.World.Portals;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// 10.2 §5e in Veilmouth (PB8d): a death that makes two within the game hour is noticed one time in two, and the
    /// church's people initiated and armed come out of the old chapel's door and run to the place; every death enters the
    /// News Pool an hour or two later, and the Notice Board pins it.
    /// </summary>
    public sealed class DeathNewsPlayTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";

        private float _timeScale = 1f;

        [SetUp]
        public void KeepTime() => _timeScale = Time.timeScale;

        [TearDown]
        public void GiveTimeBack() => Time.timeScale = _timeScale;

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        [UnityTest]
        public IEnumerator TwoDeathsWithinAnHourBringTheChurchAndTheBoardPinsThem()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            var fights = world.Fights;
            yield return new WaitForSeconds(0.5f);

            var door = Object.FindObjectsByType<Portal>(FindObjectsInactive.Exclude).FirstOrDefault(p => p.Id == SliceConfront.ChurchDoorId);
            Assert.IsNotNull(door, "the old chapel's door stands in Veilmouth");
            var at = PeopleAround.Ground(door.transform.position + (door.transform.forward * 12f), 3f, 8f);

            fights.PersonDied(at, NewsVictim.Townsman, NewsKiller.Unseen);
            Assert.AreEqual(0, fights.GodNoticed, "one death alone is not noticed");
            for (var i = 0; i < 12 && fights.GodNoticed == 0; i++)
            {
                fights.PersonDied(at, NewsVictim.Townswoman, NewsKiller.Character);
            }

            Assert.AreEqual(1, fights.GodNoticed, "a death that makes two within the hour is noticed, one time in two by the seed");

            // 10.2 §5a's roles: the old priest is always initiated, the acolyte one time in two by the seed, the maid carries no firearm.
            var settings = UnityEditor.AssetDatabase.LoadAssetAtPath<SliceSettings>("Assets/Data/Slice/SliceSettings.asset");
            var armed = settings.ChurchPeople.Where(p => p != null)
                .Select(p => (p.name, made: ResidentRoles.Make(p.GetComponent<PersonRole>().Role, p.name, world.Seed)))
                .Where(x => x.made.Limit != null && x.made.Firearm != RoleFirearm.None).Select(x => x.name).ToList();
            Assert.Contains("OldPriest", armed, "the old priest is initiated and armed");
            Assert.AreEqual(armed.Count, fights.ChurchOut.Count, $"only the initiated who carry a firearm come: {string.Join(", ", armed)}");
            Assert.IsTrue(fights.ChurchOut.All(b => b.GetComponent<PersonRole>().Role is ResidentRole.OldPriest or ResidentRole.Acolyte));

            Time.timeScale = 6f;
            var end = Time.realtimeSinceStartup + 10f;
            while (!fights.ChurchOut.All(b => b != null && Flat(b.position - at).magnitude < 6f) && Time.realtimeSinceStartup < end)
            {
                yield return null;
            }

            Time.timeScale = 1f;
            Assert.IsTrue(fights.ChurchOut.All(b => b != null && Flat(b.position - at).magnitude < 6f),
                "they run to the place: " + string.Join(", ", fights.ChurchOut.Select(b => b == null ? "gone" : $"{b.name} {Flat(b.position - at).magnitude:0.0} m, active {b.gameObject.activeInHierarchy}"))
                + $"; the door {Flat(door.transform.position - at).magnitude:0.0} m from the place");

            // The news: nothing until an hour or two has passed; then the board pins every death, the newest first.
            Assert.AreEqual(0, world.News.Told(world.Clock.Tick).Count, "not at once");
            world.Clock.AdvanceTicks(SliceNews.DelayMost);
            var told = world.News.Told(world.Clock.Tick);
            Assert.AreEqual(fights.Deaths, told.Count, "every death is told");
            var board = Object.FindObjectsByType<TipSourcePoint>(FindObjectsInactive.Exclude).First(b => b.Source == TipSource.NoticeBoard);
            board.Use();
            var hud = Object.FindAnyObjectByType<HudView>();
            Assert.IsTrue(hud.IsReadingOpen, "the board opens");
            StringAssert.Contains(SliceNews.Line(told[0], world.Clock.Tick), hud.ReadingText, "and pins the news");
            Debug.Log($"[DeathNews] {fights.Deaths} deaths, the board says:\n{hud.ReadingText}");
        }
    }
}
#endif

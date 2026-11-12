#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using GameDev.Slice.Beasts;
using GameDev.Slice.Fights;
using GameDev.Slice.People;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// 10.2 §5e, the Alarm Check of the townsfolk and the beasts (PB8c). A walker who goes to look walks its route, the
    /// shorter way, to the point nearest the place and on to 4 m short of it, looks round 3 ticks and walks back to its
    /// round; one who walks away walks its route, the shorter way, until it is 30 cells off, waits a tick and walks back.
    /// In Veilmouth an Alarm sends the walkers who hear it, each by its Boldness; wolves converge on the place inside their
    /// territory, and a boar runs to the far side of its own.
    /// </summary>
    public sealed class AlarmCheckPlayTests : SliceInputTestBase
    {
        private const string MovementScene = "Assets/Scenes/Slice/S0_Movement_Test.unity";
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";
        private const string ManWalker = "Assets/Prefabs/Slice/People/GentlemanWalker.prefab";
        private static readonly Vector3 Area = new Vector3(-300f, 0f, 300f);

        private float _timeScale = 1f;

        [SetUp]
        public void KeepTime() => _timeScale = Time.timeScale;

        [TearDown]
        public void GiveTimeBack() => Time.timeScale = _timeScale;

        private static IEnumerator Ground()
        {
            yield return LoadScene(MovementScene);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Alarm test ground";
            ground.layer = LayerMask.NameToLayer("Ground");
            ground.transform.position = Area + (Vector3.down * 0.5f);
            ground.transform.localScale = new Vector3(120f, 1f, 40f);
            Physics.SyncTransforms();
        }

        /// <summary>A walker on a line 80 m long west to east, standing on its west end, so it sets out east.</summary>
        private static TownsfolkWalker Walker()
        {
            var west = Area + new Vector3(-40f, 0f, -8f);
            var east = Area + new Vector3(40f, 0f, -8f);
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ManWalker), west, Quaternion.Euler(0f, 90f, 0f));
            go.GetComponentInChildren<Animator>().cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var walker = go.GetComponent<TownsfolkWalker>();
            walker.Configure(new[] { west, east }, walker.Speed, LayerMask.GetMask("Ground", "Structure"));
            return walker;
        }

        private static IEnumerator Until(Func<bool> condition, float seconds, string what)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < end)
            {
                yield return null;
            }

            Assert.IsTrue(condition(), $"within {seconds} s: {what}");
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        [UnityTest]
        public IEnumerator OneWhoWalksAwayGoesThirtyCellsByItsRouteWaitsATickAndComesBack()
        {
            yield return Ground();
            var walker = Walker();
            yield return new WaitForSeconds(1f);
            var left = walker.transform.position;
            Assert.Greater(left.x, Area.x - 40f, "it walks east");

            // The Alarm west of it, past the line's end: east is the shorter way out of its hearing.
            var at = Area + new Vector3(-45f, 0f, -6f);
            Assert.IsTrue(SliceAlarm.Hears(at, left), "it hears the Alarm");
            Assert.IsTrue(walker.Heed(at, false), "it answers");
            Assert.IsTrue(walker.Heeding && !walker.GoesToLook && walker.Hurry, "walking away, in a hurry");
            Assert.IsFalse(walker.Heed(at, true), "one Alarm at a time");
            Assert.Greater(walker.HeedGoal.x, left.x, "the shorter way, east along its route");
            Assert.AreEqual(TownsfolkWalker.ClearCells, TownsfolkWalker.Cells(at, walker.HeedGoal), 0.05f, "to the edge of the Alarm's hearing");
            Assert.AreEqual(-8f, walker.HeedGoal.z - Area.z, 0.01f, "on its route");

            Time.timeScale = 8f;
            yield return Until(() => walker.HeedThere, 10f, "it gets there");
            Assert.IsFalse(SliceAlarm.Hears(at, walker.transform.position), "out of the Alarm's hearing");
            Assert.IsFalse(walker.Hurry, "and strolls again");
            yield return Until(() => walker.HeedReturning, 10f, "after a tick it walks back");
            yield return Until(() => !walker.Heeding, 20f, "to where it left its round");
            Assert.Less(Flat(walker.transform.position - left).magnitude, 0.6f, "where it left");
            Assert.AreEqual(1, walker.Heeds);
            var back = walker.transform.position;
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(walker.transform.position.x, back.x, "and takes up its round east");
        }

        [UnityTest]
        public IEnumerator OneWhoGoesToLookStandsShortOfThePlaceLooksRoundAndComesBack()
        {
            yield return Ground();
            var walker = Walker();
            yield return new WaitForSeconds(1f);
            var left = walker.transform.position;

            // 6 m north of its line, 12 m east: it walks to the line's nearest point and on to 4 m short of the place.
            var at = Area + new Vector3(12f, 0f, -2f);
            Assert.IsTrue(walker.Heed(at, true), "it answers");
            Assert.IsTrue(walker.GoesToLook, "going to look");
            Assert.Less(Flat(walker.HeedGoal - (Area + new Vector3(12f, 0f, -6f))).magnitude, 0.05f, "4 m short of the place, the way there clear");

            Time.timeScale = 8f;
            yield return Until(() => walker.HeedThere, 10f, "it gets there");
            Assert.Less(Flat(walker.transform.position - walker.HeedGoal).magnitude, 0.05f, "where it went");
            var looks = new System.Collections.Generic.List<float>();
            while (walker.HeedThere)
            {
                looks.Add(walker.transform.eulerAngles.y);
                yield return null;
            }

            Assert.Greater(looks.Max() - looks.Min(), 40f, "it looks round, turning either way");
            yield return Until(() => !walker.Heeding, 20f, "and walks back to its round");
            Assert.Less(Flat(walker.transform.position - left).magnitude, 0.6f, "where it left");
        }

        /// <summary>In Veilmouth a shot out of a fight sends the walkers who hear it, each by its Boldness, and no Warden among them.</summary>
        [UnityTest]
        public IEnumerator InVeilmouthAnAlarmSendsTheWalkersWhoHearItByTheirBoldness()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var fights = SliceWorldDriver.Running.Fights;
            yield return new WaitForSeconds(0.5f);
            var walker = Object.FindObjectsByType<TownsfolkWalker>(FindObjectsSortMode.InstanceID)
                .First(w => w.isActiveAndEnabled && !w.Inside && w.Route.Length >= 2 && !w.Away && !w.Seated);
            var at = walker.transform.position + new Vector3(3f, 0f, 3f);
            var hearers = Object.FindObjectsByType<TownsfolkWalker>(FindObjectsSortMode.InstanceID)
                .Where(w => w.isActiveAndEnabled && !w.Inside && w.Route.Length >= 2 && SliceAlarm.Hears(at, w.transform.position)).ToList();
            SliceAlarm.Raise(new Alarm(AlarmKind.Shot, at, null));
            Assert.Greater(fights.LastLooked + fights.LastAway, 0, "the walkers who hear it answer");
            Assert.LessOrEqual(fights.LastLooked + fights.LastAway, hearers.Count, "only those who hear it");
            foreach (var w in hearers.Where(h => h.Heeding))
            {
                Assert.AreEqual(fights.GoesToLook(w.transform), w.GoesToLook, $"{w.name} goes by its Boldness");
                Assert.IsFalse(PeopleAround.IsWarden(w.transform), "a Warden answers by its own part");
            }

            var looked = fights.LastLooked;
            var away = fights.LastAway;
            Debug.Log($"[AlarmCheck] {hearers.Count} walkers hear the shot: {looked} go to look, {away} walk away.");
        }

        /// <summary>Wolves converge on an Alarm in their territory, Alert; a boar runs to the far side of its own.</summary>
        [UnityTest]
        public IEnumerator WolvesConvergeOnAnAlarmAndABoarScatters()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var beasts = SliceWorldDriver.Running.Beasts;
            yield return new WaitForSeconds(0.5f);

            var wolf = beasts.Beasts.First(b => b.Kind == BeastKind.Wolf && b.isActiveAndEnabled && b.State == BeastState.Resting && !beasts.GlowingPacks.Contains(b.Pack));
            var way = Flat(wolf.Home - wolf.transform.position);
            way = way.sqrMagnitude > 1f ? way.normalized : Vector3.right;
            var at = PeopleAround.Ground(wolf.Home + (way * 10f), 30f, 60f);
            var from = Flat(wolf.transform.position - at).magnitude;
            SliceAlarm.Raise(new Alarm(AlarmKind.Shot, at, null));
            Assert.AreEqual(1, beasts.HeardBy(wolf.Pack), "the pack heard it");
            Assert.IsTrue(beasts.Converged(wolf.Pack), "wolves converge");
            Assert.IsTrue(beasts.EyesOf(wolf) == null || beasts.EyesOf(wolf).Meter == null || beasts.EyesOf(wolf).Meter.Alert, "Alert, as on a character noticed");
            yield return Until(() => !wolf.Moving, 20f, "it runs there");
            Assert.Less(Flat(wolf.transform.position - at).magnitude, Mathf.Max(4f, from - 5f), "nearer the place");
            Assert.LessOrEqual(Flat(wolf.transform.position - wolf.Home).magnitude, beasts.Rules.TerritoryRadius + 3f, "inside its territory");

            var boar = beasts.Beasts.First(b => b.Kind == BeastKind.Boar && b.isActiveAndEnabled && b.State == BeastState.Resting && !beasts.GlowingPacks.Contains(b.Pack));
            var near = PeopleAround.Ground(boar.Home + (Vector3.forward * 6f), 30f, 60f);
            SliceAlarm.Raise(new Alarm(AlarmKind.Call, near, null));
            Assert.AreEqual(1, beasts.HeardBy(boar.Pack), "the boar heard it");
            Assert.IsFalse(beasts.Converged(boar.Pack), "a boar scatters (owner, 2026-11-12)");
            yield return Until(() => !boar.Moving, 20f, "it runs");
            Assert.Greater(Flat(boar.transform.position - near).magnitude, beasts.Rules.TerritoryRadius * 0.6f, "to the far side, away from the place");
            Assert.LessOrEqual(Flat(boar.transform.position - boar.Home).magnitude, beasts.Rules.TerritoryRadius + 3f, "keeping to its territory");
        }
    }
}
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.Dwellings;
using GameDev.Slice.EditorTools;
using GameDev.World.Portals;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>Step 4a: the shared Dwelling scene and a street door on every dwelling (15, *The slice's Dwellings*).</summary>
    public sealed class DwellingTests
    {
        private static readonly (DwellingArchetype, DwellingWealth)[] Kinds =
        {
            (DwellingArchetype.Tenement, DwellingWealth.Poor), (DwellingArchetype.Terrace, DwellingWealth.Middling),
            (DwellingArchetype.Terrace, DwellingWealth.Rich), (DwellingArchetype.Shop, DwellingWealth.Poor),
            (DwellingArchetype.Shop, DwellingWealth.Middling), (DwellingArchetype.Villa, DwellingWealth.Rich),
            (DwellingArchetype.Warehouse, DwellingWealth.Poor),
        };

        [Test]
        public void AKeySurvivesTheJourney()
        {
            var key = VeilmouthDwellings.PreviewKey(DwellingArchetype.Shop, DwellingWealth.Poor, 1234, -5678);
            Assert.IsTrue(DwellingKey.TryParse(key.ToContext(), out var back));
            Assert.AreEqual(key, back);
            Assert.AreEqual(2, back.Lock);
            Assert.IsFalse(DwellingKey.TryParse("", out _));
            Assert.IsFalse(DwellingKey.TryParse("dwelling.1.2|Veilmouth|x|2|Shop|Poor", out _));
        }

        [Test]
        public void TheSameHouseAlwaysGivesTheSameRooms()
        {
            var kit = VeilmouthDwellings.Kit;
            var settings = VeilmouthDwellings.Settings;
            foreach (var (archetype, wealth) in Kinds)
            {
                var key = VeilmouthDwellings.PreviewKey(archetype, wealth, 42, -42);
                Assert.AreEqual(Describe(DwellingRuntime.PlanFor(key, settings, kit)), Describe(DwellingRuntime.PlanFor(key, settings, kit)));
            }
        }

        [Test]
        public void HousesOfOneKindDiffer()
        {
            var kit = VeilmouthDwellings.Kit;
            var settings = VeilmouthDwellings.Settings;
            var plans = Enumerable.Range(0, 12)
                .Select(n => Describe(DwellingRuntime.PlanFor(VeilmouthDwellings.PreviewKey(DwellingArchetype.Terrace, DwellingWealth.Middling, n * 25, 0), settings, kit)))
                .Distinct().Count();
            Assert.Greater(plans, 6);
        }

        /// <summary>Every kind at its wealth, eight houses each: no kit fault, its rooms, its door back to the street.</summary>
        [Test]
        public void EverySampleDwellingIsCleanAndLeadsBack()
        {
            var kit = VeilmouthDwellings.Kit;
            var settings = VeilmouthDwellings.Settings;
            var faults = new List<string>();
            foreach (var (archetype, wealth) in Kinds)
            {
                for (var n = 0; n < 8; n++)
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    var key = VeilmouthDwellings.PreviewKey(archetype, wealth, 300 + (n * 31), -700 - (n * 17));
                    faults.AddRange(VeilmouthDwellings.BuildAndCheck(key, kit, settings, out var root).Select(f => $"{archetype} {wealth} #{n}: {f}"));

                    var door = root.GetComponentsInChildren<Portal>().Single();
                    Assert.AreEqual(key.StreetScene, door.Target.SceneName);
                    Assert.AreEqual(key.StreetPortalId, door.Target.ArrivalPortalId);
                    var rooms = root.GetComponentsInChildren<Transform>().Count(t => t.name.StartsWith(DwellingAssembler.RoomMarker));
                    Assert.GreaterOrEqual(rooms, archetype == DwellingArchetype.Tenement ? 1 : 2, $"{archetype}");
                    Assert.LessOrEqual(rooms, wealth == DwellingWealth.Rich ? 17 : 6, $"{archetype}");
                    var furniture = root.Find("Furniture");
                    Assert.GreaterOrEqual(furniture.childCount, rooms * 2, $"{archetype} {wealth} #{n} is furnished");
                    // Every home has a bed (owner, 2026-11-06); a warehouse is no home.
                    if (archetype != DwellingArchetype.Warehouse)
                    {
                        Assert.IsTrue(furniture.Cast<Transform>().Any(t => t.name.StartsWith("Kit_ViFurniture_Bed")), $"{archetype} {wealth} #{n} has a bed");
                    }
                }
            }

            CollectionAssert.IsEmpty(faults);
        }

        /// <summary>
        /// VICTORIAN_INTERIORS_PLAN §4.1 (owner): a rich house has three floors inside, as its building has outside, with
        /// a stair to each and a well over it; a middling, poor, tenement or warehouse dwelling keeps one floor.
        /// </summary>
        [Test]
        public void RichHousesRiseAsHighAsTheirBuildings()
        {
            var kit = VeilmouthDwellings.Kit;
            var settings = VeilmouthDwellings.Settings;
            foreach (var (archetype, wealth) in Kinds.Append((DwellingArchetype.Shop, DwellingWealth.Rich)))
            {
                for (var n = 0; n < 4; n++)
                {
                    var key = VeilmouthDwellings.PreviewKey(archetype, wealth, 500 + (n * 41), 900 - (n * 23));
                    var plan = DwellingRuntime.PlanFor(key, settings, kit);
                    var floors = wealth == DwellingWealth.Rich && archetype != DwellingArchetype.Tenement && archetype != DwellingArchetype.Warehouse ? 3 : 1;
                    Assert.AreEqual(floors, plan.Floors, $"{archetype} {wealth}");
                    Assert.AreEqual(floors - 1, plan.Wells.Count, $"{archetype} {wealth}: a well over each stair");
                    Assert.AreEqual(floors - 1, plan.Furniture.Count(f => f.Name == "Stair"), $"{archetype} {wealth}: a stair to each floor");
                    for (var level = 1; level < floors; level++)
                    {
                        Assert.IsTrue(plan.Rooms.Any(r => r.Level == level && r.Kind == RoomKind.Landing), $"{archetype} {wealth}: a landing on floor {level + 1}");
                    }
                }
            }
        }

        /// <summary>
        /// Furnishing rules §5, §9 (owner): every home washes. A rich house and a middling house have a bathroom, a middling
        /// flat a WC; a poor home a washroom of 3 × 3 m with a basin, a bucket and a toilet, and no bath.
        /// </summary>
        [Test]
        public void WashingFollowsWealth()
        {
            var kit = VeilmouthDwellings.Kit;
            var settings = VeilmouthDwellings.Settings;
            foreach (var (archetype, wealth) in Kinds.Where(k => k.Item1 != DwellingArchetype.Warehouse))
            {
                for (var n = 0; n < 4; n++)
                {
                    var plan = DwellingRuntime.PlanFor(VeilmouthDwellings.PreviewKey(archetype, wealth, 70 + (n * 19), 30 + (n * 11)), settings, kit);
                    var keys = plan.Furniture.Select(f => f.Key).ToList();
                    var what = $"{archetype} {wealth} #{n}: {string.Join(", ", plan.Unplaced)}";
                    if (wealth == DwellingWealth.Poor)
                    {
                        var washroom = plan.Rooms.Single(r => r.Kind == RoomKind.Washroom);
                        Assert.AreEqual((3, 3), (washroom.Width, washroom.Depth), what);
                        CollectionAssert.IsSubsetOf(new[] { DwellingPlanner.BasinKey, "ViBucket", "ViToilet" }, keys, what);
                        CollectionAssert.DoesNotContain(keys, DwellingPlanner.BathKey, what);
                    }
                    else if (archetype == DwellingArchetype.Tenement)
                    {
                        Assert.IsTrue(plan.Rooms.Any(r => r.Kind == RoomKind.WC), what);
                        CollectionAssert.IsSubsetOf(new[] { DwellingPlanner.BasinKey, "ViToilet" }, keys, what);
                    }
                    else
                    {
                        Assert.IsTrue(plan.Rooms.Any(r => r.Kind == RoomKind.Bathroom), what);
                        CollectionAssert.IsSubsetOf(new[] { DwellingPlanner.BathKey, "ViToilet", DwellingPlanner.BasinKey }, keys, what);
                    }
                }
            }
        }

        /// <summary>
        /// Furnishing rules §7.5, VI-6's acceptance: every sample home can sleep, cook and wash, its ways in reach one
        /// another past its furniture, nothing stands in a doorway, no small thing lies on a floor where G5 forbids it,
        /// and every seat faces a table, a desk or a hearth; every required place found its spot.
        /// </summary>
        [Test]
        public void SampleHomesKeepTheFurnishingRules()
        {
            var kit = VeilmouthDwellings.Kit;
            var settings = VeilmouthDwellings.Settings;
            foreach (var (archetype, wealth) in Kinds)
            {
                for (var n = 0; n < 12; n++)
                {
                    var plan = DwellingRuntime.PlanFor(VeilmouthDwellings.PreviewKey(archetype, wealth, 11 + (n * 37), -7 - (n * 23)), settings, kit);
                    var faults = DwellingPlanner.Audit(plan, kit, archetype != DwellingArchetype.Warehouse);
                    var what = $"{archetype} {wealth} #{n}";
                    CollectionAssert.IsEmpty(faults, $"{what}: {string.Join("; ", faults)}");
                    CollectionAssert.IsEmpty(plan.Unplaced, $"{what}: unplaced {string.Join("; ", plan.Unplaced)}");
                }
            }
        }

        /// <summary>Furnishing rules §9 (owner): a rich house has one servant, half of them a second, each with a bed.</summary>
        [Test]
        public void RichHousesKeepServants()
        {
            var kit = VeilmouthDwellings.Kit;
            var settings = VeilmouthDwellings.Settings;
            const int houses = 200;
            var two = 0;
            for (var n = 0; n < houses; n++)
            {
                var plan = DwellingRuntime.PlanFor(VeilmouthDwellings.PreviewKey(DwellingArchetype.Villa, DwellingWealth.Rich, n * 17, n * 5), settings, kit);
                Assert.That(plan.Servants, Is.InRange(1, 2), $"#{n}");
                var room = plan.Rooms.Single(r => r.Kind == RoomKind.ServantRoom);
                var beds = plan.Furniture.Count(f => f.Key == "ViBed" && f.Level == room.Level && room.Contains(f.X + 0.5f, f.Z + 0.5f));
                Assert.AreEqual(plan.Servants, beds, $"#{n}: a bed for each servant; {string.Join(", ", plan.Unplaced)}");
                two += plan.Servants == 2 ? 1 : 0;
            }

            Assert.AreEqual(DwellingPlanner.SecondServantChance, two / (double)houses, 0.1);
            var poor = DwellingRuntime.PlanFor(VeilmouthDwellings.PreviewKey(DwellingArchetype.Terrace, DwellingWealth.Middling, 3, 3), settings, kit);
            Assert.AreEqual(0, poor.Servants);
        }

        /// <summary>Plan §4.2 (owner): the pack's mantel stands wherever a stove stood, the kitchen's hearth included.</summary>
        [Test]
        public void EveryHomeHasAFireplace()
        {
            var kit = VeilmouthDwellings.Kit;
            var settings = VeilmouthDwellings.Settings;
            foreach (var (archetype, wealth) in Kinds.Where(k => k.Item1 != DwellingArchetype.Warehouse))
            {
                for (var n = 0; n < 6; n++)
                {
                    var plan = DwellingRuntime.PlanFor(VeilmouthDwellings.PreviewKey(archetype, wealth, n * 29, n * 13), settings, kit);
                    Assert.IsTrue(plan.Furniture.Any(f => f.Key == DwellingPlanner.FireplaceKey), $"{archetype} {wealth} #{n}: {string.Join(", ", plan.Unplaced)}");
                }
            }
        }

        /// <summary>19 §6.3: a Stash in half the poor homes, 60 % of the middling ones and 70 % of the rich ones.</summary>
        [Test]
        public void StashesComeAtTheirChances()
        {
            var kit = VeilmouthDwellings.Kit;
            var settings = VeilmouthDwellings.Settings;
            foreach (var (archetype, wealth) in new[] { (DwellingArchetype.Tenement, DwellingWealth.Poor), (DwellingArchetype.Shop, DwellingWealth.Middling), (DwellingArchetype.Villa, DwellingWealth.Rich) })
            {
                const int houses = 400;
                var withStash = Enumerable.Range(0, houses)
                    .Count(n => DwellingRuntime.PlanFor(VeilmouthDwellings.PreviewKey(archetype, wealth, n * 13, n * 7), settings, kit).HasStash);
                Assert.AreEqual(DwellingPlanner.StashChance(wealth), withStash / (double)houses, 0.08, $"{wealth}");
            }
        }

        [Test]
        public void EveryDwellingInVeilmouthHasAStreetDoor()
        {
            Assert.IsTrue(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == VeilmouthDwellings.ScenePath), "the Dwelling scene is in Build Settings");
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var doors = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Portal>())
                .Where(p => p.Target.SceneName == SliceTestScenes.SceneName(VeilmouthDwellings.ScenePath)).ToList();
            Assert.Greater(doors.Count, 60); // C2, the smaller city (city plan §21): 77 dwellings; C1 had 269
            Assert.AreEqual(doors.Count, doors.Select(d => d.Id).Distinct().Count(), "one id per door");

            var kinds = new HashSet<DwellingArchetype>();
            foreach (var door in doors)
            {
                Assert.AreEqual(DwellingRuntime.ArrivalPortalId, door.Target.ArrivalPortalId);
                Assert.IsTrue(DwellingKey.TryParse(door.Target.Context, out var key), door.Id);
                Assert.AreEqual(door.Id, key.StreetPortalId);
                Assert.AreEqual(scene.name, key.StreetScene);
                kinds.Add(key.Archetype);
            }

            CollectionAssert.AreEquivalent(Enum.GetValues(typeof(DwellingArchetype)), kinds, "every kind of house has dwellings");

            // The seven that open keep their own doors.
            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Portal>()).ToList();
            Assert.AreEqual(7, all.Count - doors.Count);
        }

        private static string Describe(DwellingPlan plan) =>
            string.Join(";", plan.Rooms.Select(r => $"{r.Name}@{r.X},{r.Z}")) + "|" +
            string.Join(";", plan.Furniture.Select(f => $"{f.Key}@{f.Mode},{f.X},{f.Z},{f.Turns},{f.Line},{f.Along}{(f.Stash ? "!" : "")}"));
    }
}

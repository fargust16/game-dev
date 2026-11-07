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
                    Assert.LessOrEqual(rooms, wealth == DwellingWealth.Rich ? 16 : 6, $"{archetype}");
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

        /// <summary>Plan §4.2 (owner): a rich house has a bathroom, a middling one a WC; a poor one only a basin and a bucket.</summary>
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
                    var what = $"{archetype} {wealth} #{n}";
                    switch (wealth)
                    {
                        case DwellingWealth.Rich:
                            Assert.IsTrue(plan.Rooms.Any(r => r.Kind == RoomKind.Bathroom), what);
                            CollectionAssert.IsSubsetOf(new[] { "ViBath", "ViToilet", "ViBasin" }, keys, what);
                            break;
                        case DwellingWealth.Middling:
                            Assert.IsFalse(plan.Rooms.Any(r => r.Kind == RoomKind.Bathroom), what);
                            if (archetype != DwellingArchetype.Tenement)
                            {
                                Assert.IsTrue(plan.Rooms.Any(r => r.Kind == RoomKind.WC), what);
                                CollectionAssert.Contains(keys, "ViToilet", what);
                            }

                            break;
                        default:
                            Assert.IsFalse(plan.Rooms.Any(r => r.Kind == RoomKind.Bathroom || r.Kind == RoomKind.WC), what);
                            CollectionAssert.DoesNotContain(keys, "ViToilet", what);
                            CollectionAssert.DoesNotContain(keys, "ViBath", what);
                            CollectionAssert.IsSubsetOf(new[] { "ViBasin", "ViBucket" }, keys, what);
                            break;
                    }
                }
            }
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

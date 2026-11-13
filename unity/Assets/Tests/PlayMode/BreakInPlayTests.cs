#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Core.Scenes;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.BreakIns;
using GameDev.Slice.Dwellings;
using GameDev.Slice.People;
using GameDev.UI;
using GameDev.World.Portals;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5c.5 in Veilmouth (10.3 §6.1–6.2, 10.4 §6, 19 §6.3): a dwelling's door is locked and picked with Lockpicks; in the
    /// evening its household stands in the rooms and one who sees the character cries out, runs to the Wardens and the
    /// character is Wanted, a Warden coming to the door once the street is back; the Stash is taken once; by night the
    /// household sleeps in its beds; and a Warden who sees a Wanted character opens an Arrest.
    /// </summary>
    public sealed class BreakInPlayTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";
        private const string KitPath = "Assets/Data/Slice/DwellingKit.asset";

        [UnityTest]
        public IEnumerator ADwellingsDoorIsLockedAndPickedOpenWithLockpicks()
        {
            yield return Arrive(9);
            var world = SliceWorldDriver.Running;
            var (portal, key) = Dwelling(world, p => true);
            var door = portal.GetComponent<LockedDoor>();
            Assert.IsNotNull(door, "every dwelling's door is locked");
            Assert.AreEqual(key.Lock, door.Lock);
            Stand(portal);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(portal.Sealed);
            Assert.AreEqual(Loc.Get("ui.prompt.locked"), Object.FindAnyObjectByType<HudView>().PromptText, "no picks: locked");

            world.Bag.Add(SliceBreakIns.LockpicksItem, 2);
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(Loc.Get("ui.prompt.lock.pick"), Object.FindAnyObjectByType<HudView>().PromptText);

            world.BreakIns.Resolve(key.DwellingId, key.Lock, ReactionResult.Good);
            yield return new WaitForSeconds(0.2f);
            Assert.IsFalse(portal.Sealed, "picked open, the door leads in");
        }

        [UnityTest]
        public IEnumerator AResidentSeesTheCharacterRunsToTheWardensAndTheStashIsTakenOnce()
        {
            yield return Arrive(19); // the evening: the household is at home, awake
            var world = SliceWorldDriver.Running;
            var settings = AssetDatabase.LoadAssetAtPath<SliceSettings>("Assets/Data/Slice/SliceSettings.asset");
            var kit = AssetDatabase.LoadAssetAtPath<DwellingKit>(KitPath);
            var (portal, key) = Dwelling(world, k => DwellingRuntime.PlanFor(k, settings, kit).HasStash
                && DwellingResidents.Household(k, 0, SliceHomes.PersonAt(k.DwellingId, world.Seed), world.Clock.Day, world.Clock.MinuteOfDay, world.Seed).Count(m => m.Person == null) > 0);
            world.BreakIns.Open(key.DwellingId);
            Stand(portal);
            yield return new WaitForSeconds(0.3f);
            yield return Through("Dwelling");

            var residents = Object.FindAnyObjectByType<DwellingResidents>();
            Assert.IsNotNull(residents);
            Assert.IsTrue(residents.Watched, "someone is at home");
            var body = residents.People.FirstOrDefault();
            Assert.IsNotNull(body, "the household stands in its rooms");

            // The Stash first, unseen if nobody has looked yet.
            var stash = Object.FindAnyObjectByType<DwellingStash>();
            Assert.IsNotNull(stash, "the plan put a Stash in the house");
            var bronze = world.Bag.Bronze;
            stash.Open(world);
            Assert.IsTrue(world.BreakIns.StashTaken(key.DwellingId));
            Assert.IsTrue(world.Bag.Bronze > bronze || world.BreakIns.LootItems().Any(), "what it held is in the bag");
            var loot = world.BreakIns.LootBronze;
            stash.Open(world);
            Assert.AreEqual(loot, world.BreakIns.LootBronze, "empty for the run");

            // Then in front of a resident, out of Stealth Mode: it notices at once.
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var way = Vector3.ProjectOnPlane(body.forward, Vector3.up).normalized;
            player.Teleport(body.position + (way * 1.5f), Quaternion.LookRotation(-way));
            for (var t = 0f; t < 2f && !world.BreakIns.Wanted; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsTrue(world.BreakIns.Wanted, "noticed inside: Wanted until 08:00");
            Assert.IsTrue(residents.NoticedHere);
            Assert.IsTrue(world.BreakIns.HasReport, "the resident runs to the Wardens");
            Assert.AreEqual(world.Clock.Day + 1, world.State.Get(DwellingResidents.FledKey(key.DwellingId)), "the household is out until morning");
            yield return new WaitForSeconds(DwellingResidents.RunOutSeconds + 0.5f);
            Assert.IsFalse(body.gameObject.activeSelf, "out of the house");

            // Back in the street, a Warden comes to the door.
            var exit = Object.FindObjectsByType<Portal>(FindObjectsInactive.Exclude).First(p => p.Target.SceneName == "Veilmouth");
            Stand(exit);
            yield return new WaitForSeconds(0.3f);
            yield return Through("Veilmouth");
            Assert.IsFalse(world.BreakIns.HasReport, "the report is taken to a Warden");
            Assert.GreaterOrEqual(world.Fights.WardensComing, 1, "a Warden is on its way to the door");
        }

        [UnityTest]
        public IEnumerator ByNightTheHouseholdSleepsInItsBeds()
        {
            yield return Arrive(23);
            var world = SliceWorldDriver.Running;
            var (portal, key) = Dwelling(world, k => k.Archetype == DwellingArchetype.Terrace
                && DwellingResidents.Household(k, 0, SliceHomes.PersonAt(k.DwellingId, world.Seed), world.Clock.Day, world.Clock.MinuteOfDay, world.Seed).Any(m => m.Person == null));
            world.BreakIns.Open(key.DwellingId);
            Stand(portal);
            yield return new WaitForSeconds(0.3f);
            yield return Through("Dwelling");
            Assert.Greater(Slumber.All.Count, 0, "asleep in its beds");
            Assert.IsTrue(Slumber.All.All(s => Slumber.Asleep(s.transform)));
        }

        [UnityTest]
        public IEnumerator AWardenWhoSeesAWantedCharacterOpensAnArrest()
        {
            yield return Arrive(9);
            var world = SliceWorldDriver.Running;
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            world.BreakIns.Noticed("dwelling.test", false); // heard of, not seen by a Warden
            var warden = PeopleAround.Wardens(player.transform.position).FirstOrDefault();
            Assert.IsNotNull(warden);
            var way = Vector3.ProjectOnPlane(warden.forward, Vector3.up).normalized;
            player.Teleport(Ground(warden.position + (way * 4f)), Quaternion.LookRotation(-way));
            for (var t = 0f; t < 2f && !world.Accusation.IsOpen; t += Time.deltaTime)
            {
                yield return null;
            }

            var arrest = world.Accusation;
            Assert.IsTrue(arrest.IsArrest, "a Wanted character is arrested, not accused");
            var choices = arrest.Choices.ToList();
            CollectionAssert.DoesNotContain(choices, WardenAccusation.Choice.Pay, "an Arrest has no fine to pay");
            CollectionAssert.Contains(choices, WardenAccusation.Choice.LockUp);
            CollectionAssert.Contains(choices, WardenAccusation.Choice.Deceive, "unseen, with nothing stolen on it: a lie");

            arrest.Choose(WardenAccusation.Choice.Deceive);
            if (world.BreakIns.Wanted)
            {
                Assert.IsTrue(arrest.IsOpen, "a failed lie leaves the Arrest open");
                CollectionAssert.DoesNotContain(arrest.Choices.ToList(), WardenAccusation.Choice.Deceive, "and is not tried again");
            }
            else
            {
                Assert.IsFalse(arrest.IsOpen, "believed, the Warden lets the character go");
            }
        }

        private IEnumerator Arrive(int hour)
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            var ahead = ((hour * 60) - world.Clock.MinuteOfDay + 1440) % 1440;
            world.Clock.AdvanceTicks(ahead);
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            for (var t = 0f; t < 2f && !player.IsGrounded; t += Time.deltaTime)
            {
                yield return null;
            }
        }

        private static (Portal Portal, DwellingKey Key) Dwelling(SliceWorldDriver world, System.Func<DwellingKey, bool> wanted)
        {
            foreach (var portal in Object.FindObjectsByType<Portal>(FindObjectsInactive.Exclude).OrderBy(p => p.Id, System.StringComparer.Ordinal))
            {
                if (portal.Target.SceneName == "Dwelling" && DwellingKey.TryParse(portal.Target.Context, out var key)
                    && key.Archetype != DwellingArchetype.Office && key.Archetype != DwellingArchetype.Warehouse
                    && key.StreetPortalId == key.DwellingId && wanted(key))
                {
                    return (portal, key);
                }
            }

            Assert.Fail("no such dwelling in Veilmouth");
            return default;
        }

        private static void Stand(Portal portal)
        {
            var pose = portal.ArrivalPose;
            Object.FindAnyObjectByType<PlayerLocomotion>().Teleport(pose.position, pose.rotation);
        }

        private IEnumerator Through(string sceneName)
        {
            yield return Tap(Key.E);
            var deadline = Time.realtimeSinceStartup + 10f;
            while ((SceneManager.GetActiveScene().name != sceneName || SceneTransition.IsTraveling) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.AreEqual(sceneName, SceneManager.GetActiveScene().name, "the door should have led to the other scene");
            yield return new WaitForSeconds(0.3f);
        }

        private static Vector3 Ground(Vector3 at) =>
            Physics.Raycast(at + (Vector3.up * 3f), Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore) ? hit.point : at;
    }
}
#endif

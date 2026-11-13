#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameDev.Core.Scenes;
using GameDev.Items;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.Gear;
using GameDev.Slice.People;
using GameDev.Slice.Save;
using GameDev.World.Portals;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5c.1 (17 X.3; the owner's answer to S5-7): a Suspend Save written in Veilmouth and continued after the run went on
    /// leaves every checked value as it was at the save: the clock, a fact of the world, the character, the bag and what is
    /// worn, the player's place, every walker's place and what it was doing, and a body that fell still lying. In an
    /// interior the run comes back inside it.
    /// </summary>
    public sealed class SuspendSavePlayTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";
        private const string Fact = "test.save.fact";

        [SetUp]
        public void UseTheTestsOwnSlot()
        {
            SliceSaveSlot.Path = Path.Combine(Application.temporaryCachePath, "suspend-test.json");
            SliceSaveSlot.Delete();
        }

        [TearDown]
        public void ForgetTheSlot()
        {
            SliceSaveSlot.Delete();
            SliceSaveSlot.Path = null;
        }

        [UnityTest]
        public IEnumerator ASaveAndALoadInTheCityLeaveTheRunAsItWas()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            yield return new WaitForSeconds(3f); // the townsfolk on their ways, some at their places

            // What a run changes: a fact, a value, goods, what is worn, a body fallen, the player's place.
            var clock = GameClockDriver.Running;
            world.State.Set(Fact, 7);
            world.Character.Set(CharacterAttribute.Strength, 6);
            Assert.IsTrue(world.Bag.Add("iron_ore", 3));
            world.Bag.Earn(123);
            var helm = new ItemPiece("plate_head");
            Assert.IsTrue(world.Bag.AddPiece(helm));
            Assert.IsNull(world.Outfit.Wear(helm));
            var dead = Object.FindObjectsByType<TownsfolkWalker>(FindObjectsSortMode.InstanceID).First(w => w.isActiveAndEnabled && !w.Inside && !w.Away);
            FallenBody.Mark(dead.transform, true, 2f);
            dead.GetComponent<FallenBody>().Lay();
            var deadId = SavedObjects.PathOf(dead.transform);
            var here = PeopleAround.Ground(player.transform.position + (player.transform.forward * 2f), 3f, 8f);
            player.Teleport(here, Quaternion.Euler(0f, 37f, 0f));
            yield return null;
            yield return null;

            Assert.IsNull(world.Save.RefusalKey(), "a save may be written in the street at peace");
            var tick = clock.Tick;
            var at = player.transform.position;
            var walkers = Walkers();
            Assert.Greater(walkers.Count, 10, "Veilmouth's townsfolk are saved");
            Assert.IsNull(world.Save.Write());
            Assert.IsTrue(SliceSaveSlot.Exists, "the save is on disk");

            // The run goes on and changes again.
            yield return new WaitForSeconds(2f);
            world.State.Set(Fact, 9);
            world.Character.Set(CharacterAttribute.Strength, 12);
            Assert.IsTrue(world.Bag.Spend(100));
            world.Outfit.TakeOff(helm);
            player.Teleport(PeopleAround.Ground(at + (Vector3.right * 6f), 3f, 8f), Quaternion.identity);
            yield return null;

            yield return world.Save.Load(SliceSaveSlot.Read());

            Assert.AreEqual(SceneManager.GetSceneByName("Veilmouth"), SceneManager.GetActiveScene(), "the run is back in the city");
            Assert.AreSame(world, SliceWorldDriver.Running, "the same world runs");
            Assert.AreEqual(tick, clock.Tick, "the clock");
            Assert.AreEqual(7, world.State.Get(Fact), "the world state");
            Assert.AreEqual(6, world.Character.Get(CharacterAttribute.Strength), "the character");
            Assert.AreEqual(3, world.Bag.Count("iron_ore"), "the bag");
            Assert.AreEqual(SliceWorldDriver.StartingBronze + 123, world.Bag.Bronze, "the purse");
            Assert.AreEqual("plate_head", world.Outfit[WearSlot.Head]?.item, "what is worn");
            player = Object.FindAnyObjectByType<PlayerLocomotion>();
            Assert.Less(Vector3.Distance(at, player.transform.position), 0.05f, "the player's place");
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(37f, player.transform.eulerAngles.y)), 1f, "the player's facing");

            var back = Walkers();
            foreach (var pair in walkers)
            {
                Assert.IsTrue(back.TryGetValue(pair.Key, out var now), pair.Key);
                Assert.Less(Vector3.Distance(pair.Value.At, now.At), 0.05f, pair.Key + " stands where it stood");
                Assert.AreEqual(pair.Value.Away, now.Away, pair.Key + " is away or on its route as it was");
                Assert.AreEqual(pair.Value.Seated, now.Seated, pair.Key + " sits as it sat");
                Assert.AreEqual(pair.Value.Inside, now.Inside, pair.Key + " is inside a door as it was");
            }

            var fallen = SavedObjects.FindPath(deadId);
            Assert.IsNotNull(fallen, "the fallen walker is found again");
            Assert.IsNotNull(fallen.GetComponent<FallenBody>(), "one who fell does not rise again");
            Assert.IsFalse(fallen.GetComponent<TownsfolkWalker>().enabled, "and walks no more");
        }

        [UnityTest]
        public IEnumerator ASaveInsideTheSootAndBellComesBackInside()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            var door = Object.FindObjectsByType<Portal>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).First(p => p.Id == "soot-and-bell.street");
            Assert.IsTrue(SceneTransition.Travel(door.Target));
            while (SceneTransition.IsTraveling)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);
            var inside = SceneManager.GetActiveScene().name;
            Assert.AreEqual(door.Target.SceneName, inside);
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var at = player.transform.position + (player.transform.forward * 0.5f);
            player.Teleport(at, player.transform.rotation);
            yield return null;
            at = player.transform.position;
            Assert.IsNull(world.Save.Write());

            yield return world.Save.Load(SliceSaveSlot.Read());

            Assert.AreEqual(inside, SceneManager.GetActiveScene().name, "the run is back inside");
            Assert.AreEqual("Veilmouth", SceneTransition.KeptScene.name, "the city is kept behind it");
            player = Object.FindAnyObjectByType<PlayerLocomotion>();
            Assert.Less(Vector3.Distance(at, player.transform.position), 0.05f, "the player's place inside");
        }

        private readonly struct Pose
        {
            public Pose(TownsfolkWalker w)
            {
                At = w.transform.position;
                Away = w.Away;
                Seated = w.Seated;
                Inside = w.Inside;
            }

            public Vector3 At { get; }

            public bool Away { get; }

            public bool Seated { get; }

            public bool Inside { get; }
        }

        private static Dictionary<string, Pose> Walkers()
        {
            var walkers = new Dictionary<string, Pose>();
            foreach (var w in Object.FindObjectsByType<TownsfolkWalker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (w.enabled && !w.Greeting && w.gameObject.scene.name == "Veilmouth")
                {
                    walkers[SavedObjects.PathOf(w.transform)] = new Pose(w);
                }
            }

            return walkers;
        }
    }
}
#endif

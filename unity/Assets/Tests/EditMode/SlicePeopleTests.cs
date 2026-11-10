using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.EditorTools;
using GameDev.Slice.People;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>S1.5: every named person of SLICE_PEOPLE §1 stands at its place, once; Nell and Kit are talked to (S2.4).</summary>
    public sealed class SlicePeopleTests
    {
        [Test]
        public void EveryNamedPersonStandsAtItsPlaceOnce()
        {
            var found = new List<(string id, string scene)>();
            foreach (var scene in SliceRoster.All.Select(e => e.Scene).Distinct())
            {
                EditorSceneManager.OpenScene(SlicePeople.ScenePath(scene), OpenSceneMode.Single);
                Physics.SyncTransforms();
                foreach (var person in Object.FindObjectsByType<NamedPerson>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    found.Add((person.Id, scene));
                    var entry = SliceRoster.All.SingleOrDefault(e => e.Id == person.Id);
                    Assert.IsNotNull(entry, $"{person.name} in {scene} is on the roster");
                    Assert.AreEqual(entry.Name, person.DisplayName);
                    Assert.AreEqual(entry.ObjectName, person.name, "the object carries the person's English name");
                    // The owner's Tripo person for the role (owner, 2026-11-10; asset map §1), of the person's sex.
                    var prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(person.gameObject);
                    var look = SlicePeopleModels.LookFor(entry.ObjectName, entry.Female);
                    Assert.AreEqual(look.Standing, prefab, $"{entry.Name}'s model");
                    Assert.AreEqual(entry.Female, look.Female, $"{entry.Name}'s model is of the person's sex");
                    Assert.IsTrue(Physics.Raycast(person.transform.position + Vector3.up, Vector3.down, 1.3f, ~LayerMask.GetMask("Character")),
                        $"{entry.Name} stands on a floor at {person.transform.position}");

                    Assert.IsNull(person.GetComponent<TipSourcePoint>(), $"{entry.Name} is asked in conversation, not read as a board");
                    var talk = person.GetComponent<PersonTalk>();
                    if (person.Id == "nell" || person.Id == "kit")
                    {
                        Assert.IsNotNull(talk, $"{entry.Name} can be talked to");
                        Assert.AreSame(SlicePeople.DialogueOf(person.Id), talk.Dialogue, $"{entry.Name}'s conversation");
                        Assert.IsNotNull(talk.Dialogue);
                    }
                    else
                    {
                        Assert.IsNull(talk, $"{entry.Name} has no conversation before its milestone");
                    }
                }
            }

            foreach (var entry in SliceRoster.All)
            {
                CollectionAssert.AreEqual(new[] { (entry.Id, entry.Scene) }, found.Where(f => f.id == entry.Id).ToArray(), $"{entry.Name} stands once, in {entry.Scene}");
            }

            Assert.AreEqual(14, SliceRoster.All.Count);
        }

        /// <summary>The owner's female Warden and the player's two bodies (owner, 2026-11-10; asset map §1).</summary>
        [Test]
        public void TheFemaleWardensAndThePlayerWearTheOwnersModels()
        {
            foreach (var (scene, name) in new[] { (VeilmouthScene.ScenePath, "Warden at the Black Puddle"), (VeilmouthDressing.LockupScenePath, "Warden") })
            {
                EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
                var wardens = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(t => t.name == name && PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject))
                    .Select(t => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)).ToList();
                Assert.AreEqual(1, wardens.Count(p => p == SlicePeopleModels.WardenWoman.Standing), $"{name} in {scene}: one woman Warden");
            }

            foreach (var (path, look) in new[] { (SliceTestScenes.PlayerPrefabPath, SlicePeopleModels.PlayerMan), (SlicePeopleModels.PlayerWomanPrefab, SlicePeopleModels.PlayerWoman) })
            {
                var player = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsNotNull(player, path);
                var animator = player.transform.Find("Model").GetComponent<Animator>();
                Assert.AreEqual(look.Model, AssetDatabase.GetAssetPath(animator.avatar), $"{path} wears {look.File}");
                Assert.IsTrue(animator.avatar.isHuman);
                Assert.IsNotNull(animator.runtimeAnimatorController);
                Assert.AreEqual(1, player.GetComponentsInChildren<Animator>(true).Length, $"{path} has one body");
            }
        }
    }
}

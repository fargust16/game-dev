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
                    var prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(person.gameObject);
                    StringAssert.Contains(entry.Female ? "HumanDummy_F" : "HumanDummy_M", prefab, $"{entry.Name}'s Dummy");
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
    }
}

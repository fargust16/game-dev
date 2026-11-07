using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.EditorTools;
using GameDev.Slice.People;
using GameDev.World.Poi;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>S1.5: every named person of SLICE_PEOPLE §1 stands at its place, once.</summary>
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
                    Assert.AreEqual(entry.Name, person.name, "the object carries the person's name");
                    var prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(person.gameObject);
                    StringAssert.Contains(entry.Female ? "HumanDummy_F" : "HumanDummy_M", prefab, $"{entry.Name}'s Dummy");
                    Assert.IsTrue(Physics.Raycast(person.transform.position + Vector3.up, Vector3.down, 1.3f, ~LayerMask.GetMask("Character")),
                        $"{entry.Name} stands on a floor at {person.transform.position}");

                    var source = person.GetComponent<TipSourcePoint>();
                    if (person.Id == "nell" || person.Id == "kit")
                    {
                        Assert.IsNotNull(source, $"{entry.Name} is a tip source");
                        Assert.AreEqual(person.Id == "nell" ? TipSource.Bartender : TipSource.StreetRumour, source.Source);
                    }
                    else
                    {
                        Assert.IsNull(source, $"{entry.Name} gives no S1 tips");
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

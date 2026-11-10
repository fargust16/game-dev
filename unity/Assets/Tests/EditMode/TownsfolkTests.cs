using System.Linq;
using GameDev.Slice.EditorTools;
using GameDev.Slice.People;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>The ordinary townsfolk: ten men and ten women of the owner's people replace every yellow Dummy, each on a clear closed route.</summary>
    public sealed class TownsfolkTests
    {
        [TestCase(SliceTownsfolk.PrefabPath)]
        [TestCase(SliceTownsfolk.WomanPrefabPath)]
        [TestCase(SlicePeopleModels.Folder + "/GentlemanWalker.prefab")]
        [TestCase(SlicePeopleModels.Folder + "/LadyWalker.prefab")]
        [TestCase(SlicePeopleModels.Folder + "/WorkingWomanWalker.prefab")]
        public void TheTownsfolkWalkAndIdleInLoops(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab);
            var walker = prefab.GetComponent<TownsfolkWalker>();
            Assert.That(walker.Speed, Is.InRange(0.6f, 2f));
            var controller = (AnimatorController)prefab.GetComponentInChildren<Animator>().runtimeAnimatorController;
            var machine = controller.layers[0].stateMachine;
            Assert.AreEqual("Walk", machine.defaultState.name);
            foreach (var state in machine.states)
            {
                Assert.IsTrue(((AnimationClip)state.state.motion).isLooping, state.state.name);
            }
        }

        [Test]
        public void InVeilmouthEveryYellowDummyIsAWalkingTownsperson()
        {
            var scene = EditorSceneManager.OpenScene(VeilmouthScene.ScenePath, OpenSceneMode.Single);
            var people = scene.GetRootGameObjects().First(g => g.name == VeilmouthCity.RootName).transform.Find("People");
            // A named person (S1.5) may be a yellow Dummy, as Kit Mallow is; only the unnamed townsfolk walk.
            Assert.IsFalse(people.Cast<Transform>().Any(t => t.GetComponent<GameDev.Slice.People.NamedPerson>() == null
                && PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject) is GameObject s && s.name.Contains("Yellow")), "no unnamed yellow Dummy is left");
            var walkers = people.GetComponentsInChildren<TownsfolkWalker>();
            Assert.AreEqual(20, walkers.Length, "the townsfolk and their farmer, on C5 (city plan §28.10, §29.4)");
            // Each walks in a model by its block's wealth (owner's people, 2026-11-10; asset map §1).
            var looks = walkers.Select(w => SlicePeopleModels.All.Single(l => l.Walking == PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(w.gameObject))).ToList();
            Assert.AreEqual(10, looks.Count(l => l.Female), "townswomen");
            Assert.AreEqual(10, looks.Count(l => !l.Female), "townsmen");
            Physics.SyncTransforms();

            // A person's capsule fits all along each route; the walkers themselves are left out.
            var blocking = LayerMask.GetMask("Default", "Structure", "Prop", "Vegetation", "Interactable");
            foreach (var w in walkers)
            {
                Assert.GreaterOrEqual(w.Route.Length, 3, w.name);
                Assert.Greater(w.Length, 30f, w.name);
                for (var i = 0; i < w.Route.Length; i++)
                {
                    var a = w.Route[i];
                    var b = w.Route[(i + 1) % w.Route.Length];
                    var steps = Mathf.CeilToInt(Vector3.Distance(a, b) / 0.5f);
                    for (var s = 0; s <= steps; s++)
                    {
                        var p = Vector3.Lerp(a, b, s / (float)Mathf.Max(1, steps));
                        Assert.IsFalse(Physics.CheckCapsule(p + (Vector3.up * 0.75f), p + (Vector3.up * 1.45f), 0.25f, blocking, QueryTriggerInteraction.Ignore),
                            $"{w.name}: something stands on its route at {p}");
                    }
                }
            }
        }
    }
}

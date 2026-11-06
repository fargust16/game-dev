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
    /// <summary>The ordinary townsfolk: the owner's gentleman replaces every yellow Dummy, each on a clear closed route.</summary>
    public sealed class TownsfolkTests
    {
        [Test]
        public void TheTownspersonWalksAndIdlesInLoops()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SliceTownsfolk.PrefabPath);
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
            Assert.IsFalse(people.Cast<Transform>().Any(t => PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject) is GameObject s && s.name.Contains("Yellow")), "no yellow Dummy is left");
            var walkers = people.GetComponentsInChildren<TownsfolkWalker>();
            Assert.AreEqual(15, walkers.Length);
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

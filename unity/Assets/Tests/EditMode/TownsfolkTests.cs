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
    /// <summary>
    /// The ordinary townsfolk: ten men and ten women of the owner's people replace every yellow Dummy, and 26 more walk
    /// since the owner's play test of S5a, each on a clear closed route, stopping at some of its turns; since AN6 they
    /// sit, bargain, greet and drink in their own clips (ActivityTests).
    /// </summary>
    public sealed class TownsfolkTests
    {
        [TestCase(SlicePeopleModels.Folder + "/GentlemanWalker.prefab")]
        [TestCase(SlicePeopleModels.Folder + "/LadyWalker.prefab")]
        [TestCase(SlicePeopleModels.Folder + "/WorkingWomanWalker.prefab")]
        public void TheTownsfolkWalkIdleAndPassTheirTimeInTheirClips(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab);
            var walker = prefab.GetComponent<TownsfolkWalker>();
            Assert.That(walker.Speed, Is.InRange(0.6f, 2f));
            var controller = (AnimatorController)prefab.GetComponentInChildren<Animator>().runtimeAnimatorController;
            var machine = controller.layers[0].stateMachine;
            Assert.AreEqual("Walk", machine.defaultState.name);

            // AN6: the walk and the idle, sitting on a bench and talking there, bargaining at a stall, greeting, drinking.
            var clips = machine.states.ToDictionary(s => s.state.name, s => (AnimationClip)s.state.motion);
            CollectionAssert.AreEquivalent(new[] { "Walk", "Idle", "Idle2", "Idle3", "FoldArms", "StandToSit", "Sit", "SitTalk", "SitToStand", "TalkIdle", "Nod", "ShakeHead", "Drink", "Gather2" }, clips.Keys);
            foreach (var loop in new[] { "Walk", "Idle", "Idle2", "Idle3", "FoldArms", "Sit", "SitTalk", "TalkIdle", "ShakeHead" })
            {
                Assert.IsTrue(clips[loop].isLooping, loop);
            }

            foreach (var once in new[] { "StandToSit", "SitToStand", "Nod", "Drink", "Gather2" })
            {
                Assert.IsFalse(clips[once].isLooping, once);
            }

            // A knocked chair is set up in the gathering clip, its length the walker's (owner, 2026-11-12).
            Assert.AreEqual(clips["Gather2"].length, TownsfolkWalker.FetchSeconds, 0.02f);

            // The walker times its one-shot clips by their lengths.
            Assert.AreEqual(clips["StandToSit"].length, TownsfolkWalker.StandToSitSeconds, 0.02f);
            Assert.AreEqual(clips["SitToStand"].length, TownsfolkWalker.SitToStandSeconds, 0.02f);
            Assert.AreEqual(clips["Nod"].length, TownsfolkWalker.NodSeconds, 0.02f);
            Assert.AreEqual(clips["Drink"].length, TownsfolkWalker.DrinkSeconds, 0.02f);
        }

        [Test]
        public void AWalkerStopsAtAThirdOfItsTurnsForFourToTwelveSeconds()
        {
            var stops = 0;
            for (var seed = 1u; seed <= 20u; seed++)
            {
                for (var turn = 0; turn < 30; turn++)
                {
                    var stop = TownsfolkWalker.StopAt(seed * 7919u, turn);
                    Assert.AreEqual(stop, TownsfolkWalker.StopAt(seed * 7919u, turn), "the same turn stops alike on every run");
                    if (stop > 0f)
                    {
                        stops++;
                        Assert.That(stop, Is.InRange(TownsfolkWalker.StopShortest, TownsfolkWalker.StopLongest));
                    }
                }
            }

            Assert.That(stops / 600f, Is.InRange(0.27f, 0.40f), "about one turn in three");
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
            Assert.AreEqual(46, walkers.Length, "the townsfolk and their farmer, on C5 (city plan §28.10, §29.4), and the play test's 26");
            // Each walks in a model by its block's wealth (owner's people, 2026-11-10; asset map §1).
            var looks = walkers.Select(w => SlicePeopleModels.All.Single(l => l.Walking == PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(w.gameObject))).ToList();
            Assert.AreEqual(23, looks.Count(l => l.Female), "townswomen");
            Assert.AreEqual(23, looks.Count(l => !l.Female), "townsmen");
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

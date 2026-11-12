using System.Collections.Generic;
using System.Linq;
using GameDev.Locomotion;
using GameDev.Slice.EditorTools;
using GameDev.Slice.People;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
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

        /// <summary>
        /// A standing person's controller plays Kevin Iglesias' clips (owner's second direction, 2026-11-11;
        /// ANIMATION_PIPELINE_PLAN.md §8): its idle, the talk while Talking, and the fight's guard, blow, hit and death,
        /// every clip Humanoid and a woman's from the women's takes. No Mixamo or Blender clip is left.
        /// </summary>
        [TestCase(SlicePeopleModels.ManController, "Idle", false)]
        [TestCase(SlicePeopleModels.WomanController, "Idle", true)]
        [TestCase(SliceClips.WardenController, "Attention", false)]
        [TestCase(SliceClips.WardenWomanController, "Attention", true)]
        [TestCase(SliceClips.BlacksmithController, "HammerDown", false)]
        public void AStandingPersonTalksAndFightsInKevinIglesiasClips(string path, string idle, bool female)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            Assert.IsNotNull(controller, path);
            var states = controller.layers[0].stateMachine.states.Select(s => s.state).ToDictionary(s => s.name);
            foreach (var name in new[] { "Idle", "Walk", "Talk", "Fight", "Attack", "Hit", "Die" })
            {
                Assert.IsTrue(states.ContainsKey(name), $"{path} has {name}");
            }

            Assert.AreSame(SliceClips.Get(idle, female), states["Idle"].motion, $"{path} idles in {idle}");
            CollectionAssert.IsSubsetOf(new[] { "Talking", "Fighting", "Weapon", "Attack", "Hit", "Down" }, controller.parameters.Select(p => p.name).ToList());
            foreach (var clip in controller.animationClips)
            {
                Assert.IsTrue(clip.humanMotion, $"{clip.name} is Humanoid");
                var clipPath = AssetDatabase.GetAssetPath(clip);
                Assert.IsTrue(clipPath.StartsWith($"{SliceClips.KevinFolder}/{(female ? "Female" : "Male")}/"), $"{clip.name} is a Kevin Iglesias clip of the person's sex: {clipPath}");
            }

            var guard = (BlendTree)states["Fight"].motion;
            Assert.AreEqual("Weapon", guard.blendParameter);
            CollectionAssert.AreEqual(new[] { 1f, 2f, 3f, 4f, 5f }, guard.children.Select(c => c.threshold).ToArray(), "one guard a weapon kind");
        }

        /// <summary>
        /// AN2: the player's controller moves in eight directions: its Locomotion is a 2D blend on MoveX and MoveZ with the
        /// idle at the centre and every walk, run and sprint of the table at its direction and gait, each played at the pace
        /// the body moves; it turns on the spot; the woman player's override plays the women's takes; and the body faces the
        /// camera.
        /// </summary>
        [Test]
        public void ThePlayerMovesInEightDirectionsFacingTheCamera()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(SlicePeopleModels.PlayerController);
            Assert.IsNotNull(controller, SlicePeopleModels.PlayerController);
            var names = controller.parameters.Select(p => p.name).ToList();
            CollectionAssert.IsSubsetOf(new[] { "Speed", "MoveX", "MoveZ", "TurnRate" }, names);
            var states = controller.layers[0].stateMachine.states.Select(s => s.state).ToList();
            var tree = (BlendTree)states.Single(s => s.name == "Locomotion").motion;
            Assert.AreEqual(BlendTreeType.FreeformDirectional2D, tree.blendType);
            Assert.AreEqual(("MoveX", "MoveZ"), (tree.blendParameter, tree.blendParameterY));
            var moves = SliceClips.Moves().ToList();
            Assert.AreEqual(21, moves.Count, "eight walks, eight runs and five sprints");
            Assert.AreEqual(moves.Count + 1, tree.children.Length);
            Assert.AreSame(SliceClips.Get("Idle"), tree.children[0].motion);
            Assert.AreEqual(Vector2.zero, tree.children[0].position);
            foreach (var (use, gait, way) in moves)
            {
                var child = tree.children.Single(c => c.motion == SliceClips.Get(use));
                Assert.Less(Vector2.Distance(way * gait, child.position), 0.01f, $"{use} at {child.position}");
                Assert.That(child.timeScale, Is.InRange(0.6f, 1.3f), $"{use} plays at the body's pace");
            }

            Assert.IsTrue(states.Any(s => s.name == "TurnLeft") && states.Any(s => s.name == "TurnRight"), "it turns on the spot");

            var woman = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(SlicePeopleModels.WomanPlayerController);
            Assert.IsNotNull(woman, SlicePeopleModels.WomanPlayerController);
            Assert.AreSame(SliceClips.Get("Walk", true), woman[SliceClips.Get("Walk")], "the woman walks in the women's take");
            var womanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlicePeopleModels.PlayerWomanPrefab);
            Assert.AreSame(woman, womanPrefab.GetComponentInChildren<Animator>(true).runtimeAnimatorController);

            var locomotion = AssetDatabase.LoadAssetAtPath<GameObject>(SliceTestScenes.PlayerPrefabPath).GetComponentInChildren<PlayerLocomotion>(true);
            Assert.IsTrue(new SerializedObject(locomotion).FindProperty("faceCamera").boolValue, "the body faces the camera");
        }

        /// <summary>
        /// AN3: the player's controller carries every work clip PlayerWork plays, and each key frame falls inside its clip,
        /// the hips of a patch and a kneel beginning to rise there (pipeline plan §10).
        /// </summary>
        [Test]
        public void ThePlayerWorksEachClipToItsKeyFrame()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(SlicePeopleModels.PlayerController);
            var states = controller.layers[0].stateMachine.states.Select(s => s.state).ToDictionary(s => s.name);
            foreach (var clip in PlayerWork.Clips)
            {
                Assert.IsTrue(states.ContainsKey(clip), $"{clip} in the player's controller");
                Assert.AreSame(SliceClips.Get(clip), states[clip].motion, clip);
            }

            foreach (var key in PlayerWork.Keys)
            {
                CollectionAssert.Contains(PlayerWork.Clips, key.Key);
                var length = SliceClips.Get(key.Key).length;
                Assert.That(key.Value, Is.InRange(0.1f, length + 0.01f), $"{key.Key}'s key frame inside its {length:0.00} s");
            }

            Assert.IsFalse(SliceClips.Get("Harvest").isLooping, "the patch is stooped over and risen from once");
            Assert.IsFalse(SliceClips.Get("Kneel").isLooping, "and the kneel");
            Assert.IsTrue(SliceClips.Get("Chop").isLooping && SliceClips.Get("Mine").isLooping, "a tree and a vein swing on");
        }

        /// <summary>
        /// Every clip of the table, Kevin Iglesias' a man's and a woman's take and Quaternius' cut from its two files, is
        /// Humanoid with muscle curves and loops as the table says; the Mixamo and Blender folders are gone, and the
        /// blacksmith at the anvil hammers.
        /// </summary>
        [Test]
        public void EveryClipIsHumanoidFromKevinIglesiasOrQuaterniusAndTheBlacksmithHammers()
        {
            foreach (var e in SliceClips.Clips)
            {
                foreach (var female in e.Kevin ? new[] { false, true } : new[] { false })
                {
                    var clip = SliceClips.Get(e.Use, female);
                    Assert.IsTrue(clip.humanMotion, $"{e.Use} is Humanoid");
                    Assert.Greater(AnimationUtility.GetCurveBindings(clip).Count(b => b.type == typeof(Animator)), 90, $"{e.Use} has its muscle curves");
                    Assert.AreEqual(e.Loop, clip.isLooping, $"{e.Use} loops: {e.Loop}");
                    var path = AssetDatabase.GetAssetPath(clip);
                    Assert.IsTrue(path.StartsWith(SliceClips.KevinFolder + "/") || path == SliceClips.Q1 || path == SliceClips.Q2, $"{e.Use} from {path}");
                }
            }

            Assert.IsFalse(AssetDatabase.IsValidFolder("Assets/Animation/Mixamo"), "the Mixamo clips are gone");
            Assert.IsFalse(AssetDatabase.IsValidFolder("Assets/Animation/Blender"), "the Blender clips are gone");

            var blacksmith = AssetDatabase.LoadAssetAtPath<GameObject>(SlicePeopleModels.Blacksmith.Standing);
            Assert.IsNotNull(blacksmith, SlicePeopleModels.Blacksmith.Standing);
            var controller = blacksmith.GetComponentInChildren<Animator>(true).runtimeAnimatorController;
            Assert.AreEqual(SliceClips.BlacksmithController, AssetDatabase.GetAssetPath(controller), "the blacksmith's controller");
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

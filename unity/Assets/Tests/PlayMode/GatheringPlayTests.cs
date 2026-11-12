#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.Gathering;
using GameDev.Slice.Kit;
using GameDev.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5a.5 in Gloomwood: E at a fellable tree with an axe in the bag fells it, a check at each swing's key frame with the
    /// world paused, until it is gone; E at a herb patch picks it in one check as the character begins to rise (AN3).
    /// </summary>
    public sealed class GatheringPlayTests : SliceInputTestBase
    {
        private string _language;

        [SetUp]
        public void KeepLanguage()
        {
            _language = Loc.Language;
            Loc.Language = Loc.English;
        }

        [TearDown]
        public void RestoreLanguage()
        {
            Loc.Language = _language;
        }

        [UnityTest]
        public IEnumerator ATreeIsFelledToItsEnd()
        {
            yield return ForgetTheWorld();
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var world = SliceWorldDriver.Running;
            Assert.IsNotNull(world?.Gathering, "the world keeps gathering");
            var nodes = Object.FindObjectsByType<GatherNode>(FindObjectsInactive.Exclude);
            Assert.AreEqual(40, nodes.Count(n => n.Spot.Rule.kind == GatherKind.Tree), "Gloomwood's 40 fellable trees");
            Assert.AreEqual(30, nodes.Count(n => n.Spot.Rule.kind == GatherKind.Herb), "its 30 herb patches");
            Assert.AreEqual(12, nodes.Count(n => n.Spot.Rule.kind == GatherKind.Ore), "the twelve iron veins");
            Assert.AreEqual(10, nodes.Count(n => n.Spot.Rule.kind == GatherKind.Flax), "the ten flax patches in Crooked Hamlet's beds");

            // A strong woodcutter with an axe, at the tree farthest from every beast.
            world.Character.Set(CharacterAttribute.Strength, 30);
            world.Character.Set(CharacterSkill.Woodcutting, 30);
            world.Bag.Add("woodcutting_axe");
            var homes = world.Beasts != null ? world.Beasts.Beasts.Select(b => b.Home).ToList() : new System.Collections.Generic.List<Vector3>();
            var tree = nodes.Where(n => n.Spot.Rule.kind == GatherKind.Tree)
                .OrderByDescending(n => homes.Count == 0 ? 0f : homes.Min(h => Vector3.Distance(h, n.transform.position))).First();
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            Assert.IsTrue(Stand(tree, out var at), "room beside the tree");
            player.Teleport(at, Quaternion.LookRotation(Flat(tree.transform.position - at)));
            yield return new WaitForSeconds(0.5f);

            var hud = Object.FindAnyObjectByType<HudView>();
            Assert.AreEqual("E  Fell the tree", hud.PromptText, Describe(player));
            yield return Tap(Key.E);
            Assert.IsTrue(tree.IsActive, "E begins felling");
            Assert.IsTrue(world.Work.Busy, "the character swings");

            yield return AtKey(world);
            Assert.IsTrue(world.Reaction.Running, "the blade meeting the trunk brings the swing's bar");
            Assert.IsTrue(world.Work.AtKey);
            Assert.AreEqual(0f, player.GetComponentInChildren<Animator>().speed, "the swing stands still at its key frame");
            Assert.IsTrue(world.Clock.IsPaused, "and the world with it");
            Assert.IsFalse(player.enabled, "the character stands still while it shows");
            Assert.Greater(world.Reaction.Bar.PerfectWidth, 0d, "a perfect zone for two units");
            var cycles = 0;
            for (; cycles < 30 && tree.Shown; cycles++)
            {
                yield return StopInZone(world.Reaction);
            }

            Assert.IsFalse(tree.Shown, "the felled tree is gone, models and colliders");
            Assert.That(cycles, Is.InRange(3, 6), "two units for a perfect stop, one for a good one");
            Assert.AreEqual(cycles * SliceGathering.WearGood, world.Gathering.Wear("woodcutting_axe"), "each stop in the zone wears the axe 1");
            Assert.IsFalse(tree.GetComponentsInChildren<Renderer>().Any(r => r.enabled));
            Assert.AreEqual(6, world.Bag.Count("wood"), "its six units");
            Assert.AreEqual("ui.gather.stop.spent", world.Gathering.Stopped);
            Assert.Less(world.Stamina.Share, 1d, "felling spent Stamina");
            Assert.Greater(world.State.Get(tree.Spot.EmptiedKey), 0, "the run remembers it");
            yield return PlayedOut(world);
            Assert.IsTrue(player.enabled, "the last swing played out, the character walks again");
        }

        [UnityTest]
        public IEnumerator AHerbPatchIsPickedInOneCheck()
        {
            yield return ForgetTheWorld();
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var world = SliceWorldDriver.Running;
            var homes = world.Beasts != null ? world.Beasts.Beasts.Select(b => b.Home).ToList() : new System.Collections.Generic.List<Vector3>();
            var patch = Object.FindObjectsByType<GatherNode>(FindObjectsInactive.Exclude).Where(n => n.Spot.Rule.kind == GatherKind.Herb && n.Shown)
                .OrderByDescending(n => homes.Count == 0 ? 0f : homes.Min(h => Vector3.Distance(h, n.transform.position))).First();
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            Assert.IsTrue(Stand(patch, out var at), "room beside the patch");
            player.Teleport(at, Quaternion.LookRotation(Flat(patch.transform.position - at)));
            yield return new WaitForSeconds(0.5f);

            var hud = Object.FindAnyObjectByType<HudView>();
            Assert.AreEqual(Loc.Get("ui.prompt.gather.herb"), hud.PromptText, Describe(player));
            yield return Tap(Key.E);

            // Patches grow close together: E takes whichever is in reach, so the one begun is the one checked.
            patch = Object.FindObjectsByType<GatherNode>(FindObjectsInactive.Exclude).Single(n => n.IsActive);
            Assert.IsTrue(patch.Patch, "a herb patch begun");
            yield return AtKey(world);
            Assert.IsTrue(world.Reaction.Running, "the hands done at the ground bring the one bar");
            var animator = player.GetComponentInChildren<Animator>();
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Harvest"), "stooping over the patch");
            Assert.Greater(world.Reaction.Bar.PerfectWidth, 0d, "a perfect zone for one more");
            yield return StopInZone(world.Reaction);

            Assert.That(world.Bag.Count("herb"), Is.InRange(3, 4), "the patch's three units, a perfect stop one more");
            Assert.IsFalse(patch.Shown, "the patch is picked clean in one check");
            Assert.AreEqual("ui.gather.stop.picked", world.Gathering.Stopped);
            Assert.IsTrue(world.Work.Busy, "the character rises after the check");
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Harvest"), "rising on the same clip");
            yield return PlayedOut(world);
            Assert.IsTrue(player.enabled, "and walks again once it stands");
        }

        /// <summary>Waits for the work's clip to reach its key frame, where the bar shows.</summary>
        internal static IEnumerator AtKey(SliceWorldDriver world)
        {
            for (var t = 0f; t < 6f && !world.Reaction.Running; t += Time.deltaTime)
            {
                yield return null;
            }
        }

        /// <summary>Waits for the work to play out to its clip's end.</summary>
        internal static IEnumerator PlayedOut(SliceWorldDriver world)
        {
            for (var t = 0f; t < 6f && world.Work.Busy; t += Time.deltaTime)
            {
                yield return null;
            }

            yield return null;
        }

        /// <summary>Waits for a Reaction Check's bar and stops its marker in the middle of its zones, as Space does.</summary>
        internal static IEnumerator StopInZone(SliceReaction reaction)
        {
            for (var t = 0f; t < 6f && !reaction.Running; t += Time.deltaTime)
            {
                yield return null;
            }

            var bar = reaction.Bar;
            for (var t = 0f; t < 3f && reaction.Running && reaction.Marker < bar.GoodStart + (bar.GoodWidth * 0.5d); t += Time.deltaTime)
            {
                yield return null;
            }

            reaction.Press();
            yield return null;
            yield return null;
        }

        /// <summary>A place on the ground 1.2 m from the tree's trunk with room for the character.</summary>
        internal static bool Stand(GatherNode tree, out Vector3 at)
        {
            var ground = LayerMask.GetMask(KitLayers.Ground, KitLayers.Structure);
            var things = ~LayerMask.GetMask(KitLayers.Ground, KitLayers.Character);
            for (var a = 0; a < 16; a++)
            {
                var dir = Quaternion.Euler(0f, a * 22.5f, 0f) * Vector3.forward;
                var p = tree.transform.position + (dir * 1.2f);
                if (Physics.Raycast(p + (Vector3.up * 10f), Vector3.down, out var hit, 30f, ground, QueryTriggerInteraction.Ignore)
                    && !Physics.CheckCapsule(hit.point + (Vector3.up * 0.45f), hit.point + (Vector3.up * 1.6f), 0.3f, things, QueryTriggerInteraction.Ignore))
                {
                    at = hit.point + (Vector3.up * 0.05f);
                    return true;
                }
            }

            at = default;
            return false;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
        }
    }
}
#endif

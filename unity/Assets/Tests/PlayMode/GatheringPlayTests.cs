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
    /// <summary>S5a.5 in Gloomwood: E at a fellable tree with an axe in the bag fells it, a cycle a tick, until it is gone.</summary>
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

            for (var i = 0; i < 30 && tree.Shown; i++)
            {
                world.Clock.AdvanceTicks(1);
                yield return null;
            }

            Assert.IsFalse(tree.Shown, "the felled tree is gone, models and colliders");
            Assert.IsFalse(tree.GetComponentsInChildren<Renderer>().Any(r => r.enabled));
            Assert.AreEqual(6, world.Bag.Count("wood"), "its six units");
            Assert.AreEqual("ui.gather.stop.spent", world.Gathering.Stopped);
            Assert.Less(world.Stamina.Share, 1d, "felling spent Stamina");
            Assert.Greater(world.State.Get(tree.Spot.EmptiedKey), 0, "the run remembers it");
        }

        /// <summary>A place on the ground 1.2 m from the tree's trunk with room for the character.</summary>
        private static bool Stand(GatherNode tree, out Vector3 at)
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

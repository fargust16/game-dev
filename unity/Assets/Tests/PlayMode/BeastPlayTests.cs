#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Items;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.Beasts;
using GameDev.Slice.Gathering;
using GameDev.Slice.Kit;
using GameDev.Slice.Shops;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S3b.2 in play, in Veilmouth's wolf territories: the beasts stand in their territories, two or three of them glow,
    /// each alone; a character walking into a pack's territory is noticed, warned twice, a tick each, and then attacked;
    /// the pack breaks when its first wolf falls, the rest running and gone for the run, and the carcass stays. E at the
    /// carcass takes its fang with no lesson, and its hide only after the Skinning lesson (S5a.6).
    /// </summary>
    public sealed class BeastPlayTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";
        private const float Approach = 9f; // m from the den, inside the 15 m territory and beyond the 3 cells of an attack at once

        [UnityTest]
        public IEnumerator APackWarnsAttacksAndBreaks()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            var beasts = world.Beasts;
            Assert.IsNotNull(beasts, "the beasts run in the slice");
            for (var t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                yield return null; // the beasts are found and the seed's glow chosen
            }

            Assert.AreEqual(19, beasts.Beasts.Count, "six wolf packs of three and two and four boars");
            Assert.That(beasts.GlowingPacks.Count, Is.InRange(2, 3), "two or three glow");
            foreach (var pack in beasts.GlowingPacks)
            {
                var standing = beasts.Beasts.Where(b => b.Pack == pack && b.isActiveAndEnabled).ToList();
                Assert.AreEqual(1, standing.Count, "a glowing beast is alone");
                Assert.IsTrue(standing[0].Glowing && standing[0].GetComponentInChildren<Light>() != null, "and its glow shows");
            }

            // A pack of wolves that does not glow, the fewest first.
            var wolves = beasts.Beasts.Where(b => b.Kind == BeastKind.Wolf && b.isActiveAndEnabled && !beasts.GlowingPacks.Contains(b.Pack))
                .GroupBy(b => b.Pack).OrderBy(g => g.Count()).First().ToList();
            Assert.That(wolves.Count, Is.InRange(2, 3));
            var number = wolves[0].Pack;

            // The character: a strong body, playing its turns by the rule.
            var character = world.Character;
            character.Set(CharacterAttribute.Strength, 14);
            character.Set(CharacterAttribute.Endurance, 10);
            character.Set(CharacterAttribute.Dexterity, 6);
            character.Set(CharacterAttribute.Perception, 5);
            character.Set(CharacterSkill.MeleeSkill, 10);

            // It fights with what it carries (S5a.2): a sword and the leather set.
            foreach (var id in new[] { "sword", "leather_head", "leather_body", "leather_shoulders", "leather_gloves", "leather_trousers", "leather_boots" })
            {
                var piece = new ItemPiece(id);
                Assert.IsTrue(world.Bag.AddPiece(piece), id);
                Assert.IsNull(world.Outfit.Wear(piece), id);
            }

            var fights = world.Fights;
            fights.TurnPause = 0f;
            fights.AutoPlayer = true;
            fights.BeatPace = 6f;
            var warned = new List<int>();
            var attacked = false;
            beasts.Warned += (pack, n) =>
            {
                if (pack == number)
                {
                    warned.Add(n);
                }
            };
            beasts.Attacked += pack => attacked |= pack == number;

            // Just after a tick, so the clock's own next tick cannot come between the warnings below.
            var ticked = false;
            world.Clock.TickBoundary += _ => ticked = true;
            for (var t = 0f; t < 12f && !ticked; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsTrue(ticked);

            // Into the territory, upright, beyond the reach of an attack at once.
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            Assert.IsTrue(Spot(wolves, out var at), "a place in the territory 9 m from the den");
            player.Teleport(at + (Vector3.up * 0.05f), Quaternion.LookRotation(Flat(wolves[0].Home - at)));
            for (var t = 0f; t < 1f && warned.Count == 0; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsTrue(beasts.HasNoticed(number), "the pack notices the character");
            CollectionAssert.AreEqual(new[] { 1 }, warned, "and warns at once");
            Assert.AreEqual(Loc.Get("ui.beast.growl"), beasts.EyesOf(wolves[0]).Line, "a growl over its head");
            Assert.IsFalse(attacked);

            world.Clock.AdvanceTicks(1);
            CollectionAssert.AreEqual(new[] { 1, 2 }, warned, "the second warning comes a tick later");
            Assert.IsFalse(attacked, "no attack yet");
            world.Clock.AdvanceTicks(1);
            Assert.IsTrue(attacked, "the tick after, the pack attacks");
            Assert.IsTrue(fights.InFight);
            Assert.AreEqual(1 + wolves.Count, fights.Turns.Order.Count, "the character and the whole pack fight");

            for (var t = 0f; t < 90f && fights.InFight; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsFalse(fights.InFight, "the fight ends");
            var dead = wolves.Where(w => w.State == BeastState.Dead).ToList();
            Assert.AreEqual(1, dead.Count, "one wolf falls, and the pack breaks");
            Assert.AreEqual(1, world.State.Get(SliceBeasts.DeadKey(dead[0])), "the run remembers its death");
            Assert.IsTrue(dead[0].gameObject.activeInHierarchy, "its carcass stays where it fell");
            foreach (var w in wolves.Where(w => w != dead[0]))
            {
                Assert.AreEqual(BeastState.Gone, w.State, "the rest run");
                Assert.AreEqual(1, world.State.Get(SliceBeasts.GoneKey(w)), "and are gone for the run");
            }

            for (var t = 0f; t < 15f && wolves.Any(w => w.State == BeastState.Gone && w.gameObject.activeInHierarchy); t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsFalse(wolves.Any(w => w.State == BeastState.Gone && w.gameObject.activeInHierarchy), "they run off and do not come back");
            Assert.IsFalse(fights.Turns.IsRunning);

            // S5a.6: the carcass gives its fang to anyone, and its hide only after the lesson.
            var carcass = dead[0];
            var remains = carcass.GetComponent<BeastRemains>();
            Assert.IsNotNull(remains, "the driver made every beast's remains");
            Assert.IsTrue(Beside(carcass.transform.position, out var by), "room beside the carcass, away from every node");
            player.Teleport(by, Quaternion.LookRotation(Flat(carcass.transform.position - by)));
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(Loc.Get("ui.prompt.remains.parts"), remains.PromptFor(player.transform.position));
            var fangs = world.Bag.Count("wolf_fang");
            yield return Tap(Key.E);
            Assert.AreEqual(fangs + 1, world.Bag.Count("wolf_fang"), "E takes its fang, with no lesson");
            Assert.IsTrue(world.Remains.PartsTaken(carcass.Id));
            yield return new WaitForSeconds(BeastRemains.NoteSeconds + 0.2f);
            Assert.AreEqual(Loc.Get("ui.remains.fault.lesson"), remains.PromptFor(player.transform.position), "the hide waits for the lesson");
            yield return Tap(Key.E);
            Assert.IsFalse(world.Remains.Skinned(carcass.Id), "no skinning without it");

            world.State.Set(SliceShops.LessonKey(SliceRemains.Lesson), 1);
            yield return new WaitForSeconds(BeastRemains.NoteSeconds + 0.2f);
            Assert.AreEqual(Loc.Format("ui.prompt.remains.skin", 3), remains.PromptFor(player.transform.position));
            var tick = world.Clock.Tick;
            var leather = world.Bag.Count("leather");
            yield return Tap(Key.E);
            Assert.IsTrue(world.Remains.Skinned(carcass.Id), "E skins it once the lesson is learned");
            Assert.GreaterOrEqual(world.Clock.Tick - tick, 3L, "in 3 ticks");
            Assert.That(world.Bag.Count("leather") - leather, Is.InRange(0, 1), "a wolf's leather, or none on a failure");
        }

        /// <summary>A place on the ground 1.2 m from the carcass with room for the character and no gathering node in reach.</summary>
        private static bool Beside(Vector3 carcass, out Vector3 at)
        {
            var nodes = Object.FindObjectsByType<GatherNode>(FindObjectsInactive.Exclude);
            var things = ~LayerMask.GetMask(KitLayers.Ground, KitLayers.Character);
            for (var k = 0; k < 16; k++)
            {
                var probe = carcass + (Quaternion.Euler(0f, k * 22.5f, 0f) * Vector3.forward * 1.2f);
                if (!Physics.Raycast(probe + (Vector3.up * 10f), Vector3.down, out var hit, 30f, ~LayerMask.GetMask("Character", "NoGrid"), QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                var point = hit.point;
                if (Vector3.Angle(hit.normal, Vector3.up) < 30f && !nodes.Any(n => n.Shown && n.InReach(point))
                    && !Physics.CheckCapsule(point + (Vector3.up * 0.45f), point + (Vector3.up * 1.6f), 0.3f, things, QueryTriggerInteraction.Ignore))
                {
                    at = point + (Vector3.up * 0.05f);
                    return true;
                }
            }

            at = default;
            return false;
        }

        /// <summary>A free place on the ground 9 m from the den, more than 3 m from every wolf.</summary>
        internal static bool Spot(List<SliceBeast> wolves, out Vector3 at)
        {
            var den = wolves[0].Home;
            for (var k = 0; k < 16; k++)
            {
                var probe = den + (Quaternion.Euler(0f, k * 22.5f, 0f) * Vector3.forward * Approach);
                if (!Physics.Raycast(probe + (Vector3.up * 30f), Vector3.down, out var hit, 60f, ~LayerMask.GetMask("Character", "NoGrid"), QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                at = hit.point;
                var point = at;
                if (wolves.All(w => Flat(w.transform.position - point).magnitude > 4f) && Vector3.Angle(hit.normal, Vector3.up) < 30f)
                {
                    return true;
                }
            }

            at = default;
            return false;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
#endif

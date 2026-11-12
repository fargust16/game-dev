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

            Assert.AreEqual(32, beasts.Beasts.Count, "ten wolf packs of three and two and seven boars (the owner's play test of S5a)");
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

            // Into a wolf's cone in the territory, upright, beyond the reach of an attack at once.
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            Assert.IsTrue(Spot(wolves, out var at), "a place in the territory, in a wolf's cone");
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
            var hud = Object.FindAnyObjectByType<GameDev.UI.HudView>();
            Assert.That(hud.MessageText, Does.Contain(Loc.Get("item.wolf_fang")), "what was taken is said on the screen (owner, 2026-11-12)");
            Assert.AreEqual(Loc.Get("ui.remains.fault.lesson"), remains.PromptFor(player.transform.position),
                "and the prompt goes on at once: the hide waits for the lesson");
            yield return Tap(Key.E);
            Assert.IsFalse(world.Remains.Skinned(carcass.Id), "no skinning without it");

            world.State.Set(SliceShops.LessonKey(SliceRemains.Lesson), 1);
            Assert.AreEqual(Loc.Format("ui.prompt.remains.skin", 3), remains.PromptFor(player.transform.position));
            var tick = world.Clock.Tick;
            var leather = world.Bag.Count("leather");
            yield return Tap(Key.E);
            Assert.IsTrue(world.Work.Busy, "E kneels at the carcass once the lesson is learned");
            Assert.GreaterOrEqual(world.Clock.Tick - tick, 3L, "its 3 ticks pass");
            Assert.IsFalse(world.Remains.Skinned(carcass.Id), "and the hide waits for the check");
            yield return GatheringPlayTests.AtKey(world);
            Assert.IsTrue(world.Reaction.Running, "where the character begins to rise");
            Assert.IsTrue(world.Clock.IsPaused, "the world paused");
            Assert.AreEqual(0d, world.Reaction.Bar.PerfectWidth, "a good zone only");
            yield return GatheringPlayTests.StopInZone(world.Reaction);
            Assert.IsTrue(world.Remains.Skinned(carcass.Id), "the hide off");
            Assert.AreEqual(1, world.Bag.Count("leather") - leather, "a wolf's whole leather for a good stop");
            yield return GatheringPlayTests.PlayedOut(world);
            Assert.IsTrue(player.enabled, "the character stands and walks again");
        }

        /// <summary>
        /// The owner's play test of S5a (10.2): a boar wanders in its 25 m territory; the beasts' cones show near the
        /// character out of Stealth Mode and not beyond 30 m; an attack decided from beyond 10 cells is a chase, which gives
        /// up when the character leaves the territory, and otherwise starts the fight within 10 cells.
        /// </summary>
        [UnityTest]
        public IEnumerator ABoarWandersShowsItsConeAndChases()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            var beasts = world.Beasts;
            for (var t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                yield return null;
            }

            var boars = beasts.Beasts.Where(b => b.Kind == BeastKind.Boar && b.isActiveAndEnabled && b.State == BeastState.Resting && !beasts.GlowingPacks.Contains(b.Pack)).ToList();
            Assert.GreaterOrEqual(boars.Count, 2);
            var boar = boars[0];

            // It wanders: a place in its territory, the next wander 8–20 s later.
            var from = boar.transform.position;
            Assert.IsTrue(beasts.Wander(boar.Pack), "the boar sets out for a place in its territory");
            Assert.That(beasts.WanderIn(boar.Pack), Is.InRange(SliceBeasts.WanderLeast, SliceBeasts.WanderMost));
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(boar.Moving || Flat(boar.transform.position - from).magnitude > 0.5f, "it walks");
            for (var t = 0f; t < 30f && boar.Moving; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsFalse(boar.Moving, "and arrives");
            Assert.LessOrEqual(Flat(boar.transform.position - boar.Home).magnitude, beasts.Rules.TerritoryRadius + 1f, "inside its territory");

            // Its cone shows within 30 m of the character upright, and none from the city.
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var hud = Object.FindAnyObjectByType<GameDev.Slice.Stealth.StealthHud>();
            var city = player.transform.position;
            Assert.IsFalse(world.Hiding.Active);
            Assert.IsTrue(Ground(boar.transform.position - (Flat(boar.transform.forward).normalized * 20f), out var behind), "ground behind the boar");
            player.Teleport(behind, boar.transform.rotation);
            yield return new WaitForSeconds(0.3f);
            Assert.GreaterOrEqual(hud.AlwaysShownCones, 1, "a beast's cone shows within 30 m, out of Stealth Mode");
            Assert.IsFalse(beasts.HasNoticed(boar.Pack), "behind it, out of its cone, the boar has not noticed the character");
            player.Teleport(city, Quaternion.identity);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(0, hud.AlwaysShownCones, "none from the city");

            // A chase gives up when the character leaves the territory.
            yield return Chase(world, boar, player);
            Assert.IsTrue(beasts.IsChasing(boar.Pack), "an attack from beyond 10 cells is a chase");
            Assert.IsFalse(world.Fights.InFight);
            Assert.IsTrue(Away(boar, out var away), "ground 35 m from the den, outside its territory");
            player.Teleport(away, Quaternion.identity);
            yield return null;
            yield return null;
            Assert.IsFalse(beasts.IsChasing(boar.Pack), "outside its territory it gives up");
            Assert.IsFalse(beasts.HasNoticed(boar.Pack));

            // Another boar's chase reaches the character, and the fight begins within 10 cells.
            Arm(world);
            var fights = world.Fights;
            fights.TurnPause = 0f;
            fights.AutoPlayer = true;
            fights.BeatPace = 6f;
            var other = boars.First(b => b.Pack != boar.Pack);
            yield return Chase(world, other, player);
            Assert.IsTrue(beasts.IsChasing(other.Pack));
            for (var t = 0f; t < 15f && !fights.InFight; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsTrue(fights.InFight, "the chase reaches the character and the fight begins");
            Assert.LessOrEqual(Flat(other.transform.position - player.transform.position).magnitude, SliceBeasts.FightCells + 1.5f, "within 10 cells");
            for (var t = 0f; t < 90f && fights.InFight; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsFalse(fights.InFight, "the fight ends");
        }

        /// <summary>
        /// Brings the boar to a chase: the character comes into its cone, is noticed and warned, steps back more than 10
        /// cells inside the territory, and the second tick after brings the attack.
        /// </summary>
        private static IEnumerator Chase(SliceWorldDriver world, SliceBeast boar, PlayerLocomotion player)
        {
            // Toward its den first, so the ground before it lies in its territory after a wander.
            boar.Stop();
            boar.Face(boar.Home, 360f);
            Assert.IsTrue(Spot(new List<SliceBeast> { boar }, out var at), "a place in the boar's cone");
            player.Teleport(at + (Vector3.up * 0.05f), Quaternion.LookRotation(Flat(boar.transform.position - at)));
            for (var t = 0f; t < 1f && !world.Beasts.HasNoticed(boar.Pack); t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsTrue(world.Beasts.HasNoticed(boar.Pack), "the boar notices the character in its cone");
            Assert.IsTrue(Back(world, boar, out var back), "a place in the territory more than 12 m from the boar");
            player.Teleport(back, Quaternion.LookRotation(Flat(boar.transform.position - back)));
            yield return null;
            world.Clock.AdvanceTicks(1);
            world.Clock.AdvanceTicks(1);
        }

        private static void Arm(SliceWorldDriver world)
        {
            var character = world.Character;
            character.Set(CharacterAttribute.Strength, 14);
            character.Set(CharacterAttribute.Endurance, 10);
            character.Set(CharacterAttribute.Dexterity, 6);
            character.Set(CharacterAttribute.Perception, 5);
            character.Set(CharacterSkill.MeleeSkill, 10);
            foreach (var id in new[] { "sword", "leather_head", "leather_body", "leather_shoulders", "leather_gloves", "leather_trousers", "leather_boots" })
            {
                var piece = new ItemPiece(id);
                Assert.IsTrue(world.Bag.AddPiece(piece), id);
                Assert.IsNull(world.Outfit.Wear(piece), id);
            }
        }

        /// <summary>A place in the boar's territory more than 12 m from it, on ground the character stands on.</summary>
        private static bool Back(SliceWorldDriver world, SliceBeast boar, out Vector3 at)
        {
            foreach (var reach in new[] { 20f, 15f, 10f })
            {
                for (var k = 0; k < 16; k++)
                {
                    if (Ground(boar.Home + (Quaternion.Euler(0f, k * 22.5f, 0f) * Vector3.forward * reach), out at)
                        && world.Beasts.Inside(boar, at) && Flat(at - boar.transform.position).magnitude > 12f)
                    {
                        return true;
                    }
                }
            }

            at = default;
            return false;
        }

        /// <summary>Ground 35 m from the boar's den, above the water.</summary>
        private static bool Away(SliceBeast boar, out Vector3 at)
        {
            for (var k = 0; k < 16; k++)
            {
                if (Ground(boar.Home + (Quaternion.Euler(0f, k * 22.5f, 0f) * Vector3.forward * 35f), out at) && at.y > GameDev.Confront.Grid.CombatGridReader.SeaLevel + 0.5f)
                {
                    return true;
                }
            }

            at = default;
            return false;
        }

        private static bool Ground(Vector3 near, out Vector3 at)
        {
            if (Physics.Raycast(near + (Vector3.up * 30f), Vector3.down, out var hit, 60f, ~LayerMask.GetMask("Character", "NoGrid"), QueryTriggerInteraction.Ignore)
                && Vector3.Angle(hit.normal, Vector3.up) < 30f)
            {
                at = hit.point + (Vector3.up * 0.05f);
                return true;
            }

            at = default;
            return false;
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

        /// <summary>
        /// A free place on the ground in the first wolf's Vision Cone and in its sight, 5–8 m before it, so within the
        /// fight's 10 cells and beyond the 3 of an attack at once, more than 4 m from every wolf and inside the territory.
        /// </summary>
        internal static bool Spot(List<SliceBeast> wolves, out Vector3 at)
        {
            var world = SliceWorldDriver.Running;
            var lead = wolves[0];
            var eyes = world.Beasts.EyesOf(lead);
            var ahead = Flat(lead.transform.forward).normalized;
            foreach (var distance in new[] { 6f, 7f, 5f, 8f })
            {
                foreach (var angle in new[] { 0f, -12f, 12f, -24f, 24f })
                {
                    var probe = lead.transform.position + (Quaternion.Euler(0f, angle, 0f) * ahead * distance);
                    if (!Physics.Raycast(probe + (Vector3.up * 30f), Vector3.down, out var hit, 60f, ~LayerMask.GetMask("Character", "NoGrid"), QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }

                    var point = hit.point;
                    if (wolves.All(w => Flat(w.transform.position - point).magnitude > 4f) && Vector3.Angle(hit.normal, Vector3.up) < 30f
                        && world.Beasts.Inside(lead, point) && world.Hiding.Sees(eyes, point, false))
                    {
                        at = point;
                        return true;
                    }
                }
            }

            at = default;
            return false;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
#endif

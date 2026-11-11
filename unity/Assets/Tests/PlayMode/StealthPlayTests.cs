#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.Stealth;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S3b.1 in play, in a Veilmouth street: a guard with Perception 4 notices a new character crouching five cells in
    /// front of it on a lit street at noon, in the time 10.3 §3 gives, says its line, and the character stands up; at
    /// 23:00, away from every lamp, the same guard misses the character crouching behind its back, and notices it only
    /// once it turns round. The town's own people are told to look away, so only the guard watches.
    /// </summary>
    public sealed class StealthPlayTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";
        private const float Ahead = 5f;   // m from the character to the guard on the lit street
        private const float Behind = 3f;  // m at night

        [UnityTest]
        public IEnumerator AGuardNoticesACharacterOnALitStreet()
        {
            yield return Ready(12 * 60);
            var world = SliceWorldDriver.Running;
            var hiding = world.Hiding;
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var feet = player.transform.position;
            Assert.IsTrue(Spot(feet, Ahead, out var guardAt, out _), "a free place five cells from the character");
            var guard = Guard(guardAt, feet - guardAt);
            world.Character.Set(CharacterAttribute.Dexterity, 3);
            world.Character.Set(CharacterAttribute.Perception, 4);
            yield return null;

            yield return Tap(Key.C);
            Assert.IsTrue(hiding.Active, "C enters Stealth Mode");
            yield return null;
            Assert.IsTrue(hiding.Sees(guard, feet, true), "the guard has the character in its cone and sight");
            var darkness = hiding.Darkness(feet);
            Assert.That(darkness, Is.EqualTo(0).Or.EqualTo(2), "daylight, or a house's shadow");
            var expected = (float)hiding.TimeToFill(guard, feet);
            Assert.AreEqual(darkness == 0 ? 1.33f : 2f, expected, 0.05f, "10.3 §3: Dexterity 3, Stealth 1 against Perception 4 at five cells");

            var start = Time.time;
            var meterShown = false;
            var hud = Object.FindAnyObjectByType<StealthHud>();
            for (var t = 0f; t < 6f && hiding.LastNoticer != guard; t += Time.deltaTime)
            {
                meterShown |= hud.ShowsMeterOver(guard);
                yield return null;
            }

            var took = Time.time - start;
            Assert.AreEqual(guard, hiding.LastNoticer, "the guard notices");
            Assert.AreEqual(expected, took, 0.5f, "in the time to fill");
            Assert.IsTrue(meterShown, "Perception 4 shows the meter over it");
            Assert.IsFalse(hiding.Active, "the character stands up");
            var lines = Enumerable.Range(1, SliceStealth.Lines).Select(i => Loc.Get($"ui.stealth.noticed.{i}")).ToList();
            CollectionAssert.Contains(lines, guard.Line, "its line");
            yield return null;
            Assert.AreEqual(guard.Line, hud.LineOver(guard), "said over its head");

            yield return Tap(Key.C);
            Assert.IsFalse(hiding.Active, "no hiding again while it watches");
        }

        [UnityTest]
        public IEnumerator AGuardMissesACharacterBehindItsBackAtNight()
        {
            yield return Ready(23 * 60);
            var world = SliceWorldDriver.Running;
            var hiding = world.Hiding;
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            world.Character.Set(CharacterAttribute.Dexterity, 3);
            yield return null;

            // A dark street: farther than 6 m from every lit lamp, with room for the guard three cells off.
            var from = player.transform.position;
            Vector3 dark = default, guardAt = default;
            var found = false;
            for (var ring = 4f; ring <= 60f && !found; ring += 2f)
            {
                for (var k = 0; k < 16 && !found; k++)
                {
                    var at = Ground(from + (Quaternion.Euler(0f, k * 22.5f, 0f) * Vector3.forward * ring));
                    found = at.HasValue && Clear(from, at.Value) && hiding.Darkness(at.Value) == 4 && Spot(at.Value, Behind, out guardAt, out _)
                        && hiding.Darkness(guardAt) == 4;
                    if (found)
                    {
                        dark = at.Value;
                    }
                }
            }

            Assert.IsTrue(found, "a dark street near the start");
            player.Teleport(dark + (Vector3.up * 0.05f), Quaternion.identity);
            yield return null;
            var feet = player.transform.position;
            var guard = Guard(guardAt, guardAt - feet); // its back to the character
            yield return Tap(Key.C);
            Assert.IsTrue(hiding.Active);

            for (var t = 0f; t < 5f; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsFalse(hiding.Sees(guard, player.transform.position, true), "out of its cone");
            Assert.AreEqual(0f, guard.Meter.Fill, "its meter never moves");
            Assert.IsNull(hiding.LastNoticer, "nobody noticed");
            Assert.IsTrue(hiding.Active, "still hidden");

            // Turned round, it makes the character out slowly in the dark: Δ 0 at three cells.
            guard.transform.rotation = Quaternion.LookRotation(Flat(player.transform.position - guard.transform.position));
            yield return null;
            feet = player.transform.position;
            Assert.AreEqual(4, hiding.Darkness(feet), "a street at night with no lamp");
            var expected = (float)hiding.TimeToFill(guard, feet);
            Assert.AreEqual(3f * (0.5f + (Behind / 10f)), expected, 0.15f, "10.3 §3: Δ 0");
            var start = Time.time;
            for (var t = 0f; t < 6f && hiding.LastNoticer != guard; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.AreEqual(guard, hiding.LastNoticer, "facing it, the guard notices");
            Assert.AreEqual(expected, Time.time - start, 0.5f);
        }

        /// <summary>Veilmouth at the minute given, the town's own observers looking away.</summary>
        private IEnumerator Ready(int minute)
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            Assert.IsNotNull(world.Hiding, "stealth runs in the slice");
            var clock = world.Clock;
            clock.AdvanceTicks(((minute - clock.MinuteOfDay) + 1440) % 1440);
            for (var t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                yield return null; // the lamps follow the clock, and the town's people are enlisted
            }

            foreach (var o in StealthObserver.All.ToList())
            {
                o.enabled = false;
            }
        }

        private static StealthObserver Guard(Vector3 at, Vector3 facing)
        {
            var body = new GameObject("Test Guard");
            body.transform.SetPositionAndRotation(at, Quaternion.LookRotation(Flat(facing)));
            var guard = body.AddComponent<StealthObserver>();
            guard.Perception = 4;
            return guard;
        }

        /// <summary>A free spot on the ground at this distance from a place, in sight of it.</summary>
        private static bool Spot(Vector3 from, float distance, out Vector3 at, out Vector3 direction)
        {
            for (var k = 0; k < 16; k++)
            {
                direction = Quaternion.Euler(0f, k * 22.5f, 0f) * Vector3.forward;
                var ground = Ground(from + (direction * distance));
                if (ground.HasValue && Clear(from, ground.Value))
                {
                    at = ground.Value;
                    return true;
                }
            }

            at = direction = default;
            return false;
        }

        private static Vector3? Ground(Vector3 near) =>
            Physics.Raycast(near + (Vector3.up * 1.5f), Vector3.down, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponent<CharacterController>() == null
                ? hit.point
                : null;

        /// <summary>Nothing between two places at a crouching head's height and at a standing eye's.</summary>
        private static bool Clear(Vector3 a, Vector3 b) =>
            !Hits(a + (Vector3.up * 0.9f), b + (Vector3.up * 0.9f)) && !Hits(a + (Vector3.up * 1.6f), b + (Vector3.up * 1.6f))
            && !Hits(a + (Vector3.up * 0.4f), b + (Vector3.up * 0.4f));

        private static bool Hits(Vector3 a, Vector3 b) =>
            Physics.RaycastAll(a, (b - a).normalized, (b - a).magnitude, ~0, QueryTriggerInteraction.Ignore)
                .Any(h => h.collider.GetComponent<CharacterController>() == null && h.collider.GetComponentInParent<StealthObserver>() == null);

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
#endif

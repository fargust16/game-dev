#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.BreakIns;
using GameDev.Slice.Caches;
using GameDev.UI;
using GameDev.World.Portals;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5c.6 in Veilmouth (SLICE_TRIAL §5): the run's twenty caches stand at their spots, every one of the forty spots lies
    /// on the scene's ground and can be seen from beside it; walking near one notices it with a glint, E takes it, and a
    /// locked one asks for Lockpicks first.
    /// </summary>
    public sealed class HiddenCachePlayTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";

        /// <summary>How far beside a spot the character stands to notice and take it, m.</summary>
        private const float Beside = 1.5f;

        [UnityTest]
        public IEnumerator TheRunsCachesStandOnTheGroundAtTheirSpots()
        {
            yield return Arrive();
            var world = SliceWorldDriver.Running;
            Assert.IsNotNull(world.Caches);
            var caches = Object.FindObjectsByType<HiddenCache>(FindObjectsInactive.Include);
            CollectionAssert.AreEquivalent(world.Caches.ChosenSpots, caches.Select(c => c.Number), "the seed's twenty, once each");

            var wrong = new System.Collections.Generic.List<string>();
            foreach (var spot in CacheSpots.All)
            {
                if (!Physics.Raycast(new Vector3(spot.X, spot.Y + 1.2f, spot.Z), Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore))
                {
                    wrong.Add($"{spot.Number}: nothing solid under it");
                    continue;
                }

                var ground = HiddenCache.Ground(spot);
                if (Mathf.Abs(spot.Y - ground.y) > 1.0f)
                {
                    wrong.Add($"{spot.Number}: at {ground.y:0.00}, not on the ground or the prop it names");
                }

                // What lies over a cache (a birdbath's lip, a bench) is part of its spot; buried is what fills the place itself.
                if (Physics.OverlapSphere(ground + (Vector3.up * 0.15f), 0.1f, ~0, QueryTriggerInteraction.Ignore)
                    .Any(c => c.GetComponentInParent<Animator>() == null && c.GetComponentInChildren<Animator>() == null && c.GetComponentInParent<PlayerLocomotion>() == null))
                {
                    wrong.Add($"{spot.Number}: inside a wall or a prop");
                }

                if (!StandBeside(ground, out _))
                {
                    wrong.Add($"{spot.Number}: seen from no place beside it on the same level");
                }
            }

            CollectionAssert.IsEmpty(wrong, "every spot lies on the ground, free, and can be seen");
        }

        [UnityTest]
        public IEnumerator WalkingNearNoticesACacheAndETakesIt()
        {
            yield return Arrive();
            var world = SliceWorldDriver.Running;
            world.Character.Set(CharacterAttribute.Perception, 100);
            world.Character.Set(CharacterAttribute.Intelligence, 100);
            var cache = Object.FindObjectsByType<HiddenCache>(FindObjectsInactive.Exclude)
                .Where(c => !world.Caches.IsLocked(c.Number))
                .OrderBy(c => c.Number)
                .First(c => StandBeside(c.transform.position, out var at) && !Doors().Any(d => d.InReach(at)));
            StandBeside(cache.transform.position, out var stand);
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var away = stand + ((stand - cache.transform.position).normalized * (SliceCaches.ReturnDistance + 2f));

            for (var tries = 0; tries < 6 && !world.Caches.Noticed(cache.Number); tries++)
            {
                player.Teleport(away, Quaternion.identity); // gone 10 m away, the next approach rolls again
                yield return new WaitForSeconds(0.2f);
                player.Teleport(stand, Quaternion.LookRotation(Vector3.ProjectOnPlane(cache.transform.position - stand, Vector3.up)));
                yield return new WaitForSeconds(0.4f);
            }

            Assert.IsTrue(world.Caches.Noticed(cache.Number), "noticed within 3 m by a sharp eye");
            Assert.IsNotNull(cache.GetComponentInChildren<Light>(), "a faint glint shows it");
            yield return new WaitForSeconds(0.3f);
            var hud = Object.FindAnyObjectByType<HudView>();
            Assert.AreEqual(Loc.Get("ui.prompt.cache.take"), hud.PromptText);

            var contents = world.Caches.ContentsOf(cache.Number);
            var bronze = world.Bag.Bronze;
            yield return Tap(Key.E);
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(world.Caches.Taken(cache.Number), "E takes it");
            Assert.AreEqual(bronze + SliceCaches.WithLuck(contents.Bronze, world.Character.Get(CharacterAttribute.Luck)), world.Bag.Bronze);
            Assert.IsFalse(string.IsNullOrEmpty(hud.MessageText), "the screen says what it held");
            Assert.IsFalse(hud.IsPromptVisible && hud.PromptText == Loc.Get("ui.prompt.cache.take"), "nothing left to take");
        }

        [UnityTest]
        public IEnumerator ALockedCacheAsksForLockpicksFirst()
        {
            yield return Arrive();
            var world = SliceWorldDriver.Running;
            var cache = Object.FindObjectsByType<HiddenCache>(FindObjectsInactive.Exclude)
                .Where(c => world.Caches.IsLocked(c.Number))
                .OrderBy(c => c.Number)
                .First(c => StandBeside(c.transform.position, out var at) && !Doors().Any(d => d.InReach(at)));
            Assert.IsTrue(world.Caches.TryNotice(cache.Number, 1d, new System.Random(1)));
            world.Bag.Remove(SliceBreakIns.LockpicksItem, world.Bag.Count(SliceBreakIns.LockpicksItem));
            StandBeside(cache.transform.position, out var stand);
            Object.FindAnyObjectByType<PlayerLocomotion>().Teleport(stand, Quaternion.LookRotation(Vector3.ProjectOnPlane(cache.transform.position - stand, Vector3.up)));
            yield return new WaitForSeconds(0.4f);
            var hud = Object.FindAnyObjectByType<HudView>();
            Assert.AreEqual(Loc.Get("ui.prompt.locked"), hud.PromptText, "no picks: locked");
            yield return Tap(Key.E);
            yield return null;
            Assert.AreEqual(Loc.Get("ui.lock.nopicks"), hud.MessageText);
            Assert.IsFalse(world.Caches.Taken(cache.Number));

            world.Bag.Add(SliceBreakIns.LockpicksItem, 2);
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(Loc.Get("ui.prompt.lock.pick"), hud.PromptText);
            Assert.IsTrue(world.Caches.Resolve(cache.Number, ReactionResult.Good));
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(Loc.Get("ui.prompt.cache.take"), hud.PromptText, "picked open, it can be taken");
        }

        private IEnumerator Arrive()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            world.Clock.AdvanceTicks(((10 * 60) - world.Clock.MinuteOfDay + 1440) % 1440); // 10:00, broad day
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            for (var t = 0f; t < 2f && !player.IsGrounded; t += Time.deltaTime)
            {
                yield return null;
            }
        }

        private static Portal[] Doors() => Object.FindObjectsByType<Portal>(FindObjectsInactive.Exclude);

        /// <summary>A place <see cref="Beside"/> from <paramref name="spot"/> on the same level, with room to stand, from whose eyes the spot is seen.</summary>
        private static bool StandBeside(Vector3 spot, out Vector3 stand)
        {
            for (var i = 0; i < 16; i++)
            {
                var way = Quaternion.Euler(0f, i * 22.5f, 0f) * Vector3.forward;
                var at = spot + (way * Beside);
                if (!Physics.Raycast(at + (Vector3.up * 2.5f), Vector3.down, out var hit, 5f, ~0, QueryTriggerInteraction.Ignore)
                    || Mathf.Abs(hit.point.y - spot.y) > 1.2f
                    || Physics.CheckCapsule(hit.point + (Vector3.up * 0.5f), hit.point + (Vector3.up * 1.5f), 0.25f, ~0, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                var eye = hit.point + (Vector3.up * 1.6f);
                var target = spot + (Vector3.up * 0.25f);
                var blocked = Physics.RaycastAll(eye, (target - eye).normalized, Vector3.Distance(eye, target), ~0, QueryTriggerInteraction.Ignore)
                    .Any(h => Vector3.Distance(h.point, target) > 1f && h.collider.GetComponentInParent<Animator>() == null);
                if (!blocked)
                {
                    stand = hit.point;
                    return true;
                }
            }

            stand = spot;
            return false;
        }
    }
}
#endif

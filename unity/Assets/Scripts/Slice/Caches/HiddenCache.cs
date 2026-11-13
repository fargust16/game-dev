using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Core.Scenes;
using GameDev.Input;
using GameDev.Items;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.UI;
using GameDev.World.Portals;
using GameDev.World.Seed;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDev.Slice.Caches
{
    /// <summary>
    /// A Hidden Cache in Veilmouth (S5c.6; SLICE_TRIAL §5). It shows nothing until noticed: when the character is within
    /// 3 m and sees the spot, one hidden Perception check at the spot's stage is rolled, and a failed one is rolled again
    /// only after the character has gone 10 m away and come back. Noticed, a faint glint shows and the screen says so;
    /// within reach E takes it, or with Lockpicks picks its lock first, as any lock: the try's tick, the hands at the lock,
    /// and the world paused at the clip's key frame for the Reaction Check. Placed by <see cref="Place"/> when the city loads.
    /// </summary>
    public sealed class HiddenCache : MonoBehaviour, IPromptSource
    {
        public const string RootName = "Hidden Caches";

        /// <summary>The hands at the lock, as a door's (07.1 §8a).</summary>
        public const string Clip = "Interact";

        /// <summary>Authoring defaults, m: the eyes above the feet, how near the spot a thing still counts as part of it, how high the glint stands.</summary>
        private const float EyeHeight = 1.6f;
        private const float SpotSize = 1.0f;
        private const float GlintHeight = 0.25f;
        private const float SameLevel = 1.5f;

        /// <summary>Authoring defaults: the glint's reach, m, its brightness and its pulse, per second.</summary>
        private const float GlintRange = 1.2f;
        private const float GlintIntensity = 0.5f;
        private const float GlintPulse = 3f;

        private PlayerLocomotion _player;
        private HudView _hud;
        private Portal[] _doors;
        private Light _glint;
        private bool _armed = true;

        public int Number { get; private set; }

        public CacheSpot Spot => CacheSpots.Of(Number);

        /// <summary>The scene's ground at the spot: the highest thing within a step under it that is no person or beast.</summary>
        public static Vector3 Ground(CacheSpot spot)
        {
            var from = new Vector3(spot.X, spot.Y + 1.2f, spot.Z);
            var hits = Physics.RaycastAll(from, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
            var best = new Vector3(spot.X, spot.Y, spot.Z);
            var top = float.MinValue;
            foreach (var hit in hits)
            {
                if (Creature(hit.collider))
                {
                    continue;
                }

                if (hit.point.y > top)
                {
                    top = hit.point.y;
                    best = hit.point;
                }
            }

            return best;
        }

        /// <summary>Puts the run's caches not yet taken into the city's scene, under one root; true when placed.</summary>
        public static bool Place(Scene scene, SliceCaches caches)
        {
            if (caches == null || !scene.IsValid() || !scene.isLoaded || scene.GetRootGameObjects().Any(r => r.name == RootName))
            {
                return false;
            }

            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            foreach (var number in caches.ChosenSpots.Where(n => !caches.Taken(n)))
            {
                var cache = new GameObject($"Hidden Cache {number}").AddComponent<HiddenCache>();
                cache.transform.SetParent(root.transform, false);
                cache.Number = number;
                cache.transform.position = Ground(CacheSpots.Of(number));
            }

            Object.FindAnyObjectByType<HudView>()?.FindPromptSources();
            return true;
        }

        public bool InReach(Vector3 position, float reach) =>
            Mathf.Abs(position.y - transform.position.y) <= SameLevel
            && new Vector2(position.x - transform.position.x, position.z - transform.position.z).sqrMagnitude <= reach * reach;

        public string PromptFor(Vector3 player)
        {
            var caches = Caches();
            if (caches == null || !caches.Noticed(Number) || caches.Taken(Number) || !InReach(player, SliceCaches.OpenReach))
            {
                return null;
            }

            return Loc.Get(!caches.Shut(Number) ? "ui.prompt.cache.take" : caches.Picks > 0 ? "ui.prompt.lock.pick" : "ui.prompt.locked");
        }

        /// <summary>Whether eyes at <paramref name="eye"/> see the spot: nothing solid between, but what lies at the spot itself.</summary>
        public bool Sees(Vector3 eye)
        {
            var target = transform.position + (Vector3.up * GlintHeight);
            var way = target - eye;
            foreach (var hit in Physics.RaycastAll(eye, way.normalized, way.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Distance(hit.point, target) <= SpotSize || Creature(hit.collider))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        /// <summary>A person, a beast, a dog or the character: an animated body, whose collider may sit above or beside its Animator.</summary>
        private static bool Creature(Collider collider) =>
            collider.GetComponentInParent<PlayerLocomotion>() != null || collider.GetComponentInParent<Animator>() != null
            || collider.GetComponentInChildren<Animator>() != null;

        private static SliceCaches Caches() => SliceWorldDriver.Running != null ? SliceWorldDriver.Running.Caches : null;

        private void Update()
        {
            var world = SliceWorldDriver.Running;
            var caches = Caches();
            if (caches == null)
            {
                return;
            }

            Glint(caches.Noticed(Number) && !caches.Taken(Number));
            if (caches.Taken(Number))
            {
                enabled = false;
                return;
            }

            _player = _player != null ? _player : FindAnyObjectByType<PlayerLocomotion>();
            if (_player == null || world.Clock.IsPaused || SceneTransition.IsTraveling || (world.Fights != null && world.Fights.InFight))
            {
                return;
            }

            var at = _player.transform.position;
            var flat = new Vector2(at.x - transform.position.x, at.z - transform.position.z).magnitude;
            if (flat > SliceCaches.ReturnDistance)
            {
                _armed = true; // gone 10 m away: the next approach rolls again
            }

            if (!caches.Noticed(Number) && _armed && InReach(at, SliceCaches.NoticeReach) && Sees(at + (Vector3.up * EyeHeight)))
            {
                _armed = false;
                Notice(world, caches);
            }

            var input = GameInputService.Instance;
            if (input == null || !input.InteractPressed || !InReach(at, SliceCaches.OpenReach) || !caches.Noticed(Number))
            {
                return;
            }

            // E belongs to the cache only while the HUD shows its prompt, a door in reach keeps it, and the character is free.
            _hud = _hud != null && _hud.isActiveAndEnabled ? _hud : FindAnyObjectByType<HudView>();
            _doors ??= FindObjectsByType<Portal>(FindObjectsInactive.Exclude);
            if (_hud == null || _hud.AnyWindowOpen || _hud.PromptText != PromptFor(at)
                || _doors.Any(d => d != null && d.isActiveAndEnabled && d.InReach(at)) || world.Work == null || world.Work.Busy)
            {
                return;
            }

            if (!caches.Shut(Number))
            {
                Open(caches);
                return;
            }

            if (caches.Picks <= 0)
            {
                _hud.ShowMessage(Loc.Get("ui.lock.nopicks"));
                return;
            }

            Pick(world, caches);
        }

        /// <summary>One notice check of 07.1 at the spot's stage, from the action checks' stream forked by the spot and the tick.</summary>
        private void Notice(SliceWorldDriver world, SliceCaches caches)
        {
            if (world.ActionChecks == null)
            {
                return;
            }

            var rolls = world.Seed.Fork(SliceSeedStreams.ActionChecks, StableHash.Of(SliceCaches.NoticedKey(Number)), (int)world.Clock.Tick);
            var chance = TraceSpot.LookChance(world.ActionChecks, world.Character, SliceCaches.NoticeDifficulty(Spot.Stage), world.Mind != null ? world.Mind.CheckPenalty : 0);
            if (caches.TryNotice(Number, chance, rolls))
            {
                _hud = _hud != null && _hud.isActiveAndEnabled ? _hud : FindAnyObjectByType<HudView>();
                _hud?.ShowMessage(Loc.Get("ui.cache.noticed"));
            }
        }

        /// <summary>Begins a try at the cache's lock: its tick passes first, as any lock's (10.3 §6.1).</summary>
        public bool Pick(SliceWorldDriver world, SliceCaches caches)
        {
            world.Clock.AdvanceTicks(1);
            return world.Work.Begin(new WorkStroke
            {
                Clip = Clip,
                Bar = () => caches.Bar(Number, world.Clock.Tick),
                CaptionKey = "ui.lock.caption",
                PauseWorld = true,
                Steady = true,
                Settle = result =>
                {
                    var opened = caches.Resolve(Number, result);
                    _hud = _hud != null && _hud.isActiveAndEnabled ? _hud : FindAnyObjectByType<HudView>();
                    _hud?.ShowMessage(opened ? Loc.Get("ui.lock.opened") : Loc.Format("ui.lock.broke", caches.Picks));
                },
                Face = transform,
                StandOff = 0.6f,
            });
        }

        /// <summary>Takes the cache and says what it held on the screen.</summary>
        public void Open(SliceCaches caches)
        {
            var taken = caches.Take(Number);
            if (taken == null)
            {
                return;
            }

            _hud = _hud != null && _hud.isActiveAndEnabled ? _hud : FindAnyObjectByType<HudView>();
            _hud?.ShowMessage(Says(taken.Value));
            Glint(false);
        }

        /// <summary>The Screen Message of what a cache held.</summary>
        public static string Says(CacheContents taken)
        {
            var parts = new List<string>();
            foreach (var (id, count) in taken.Supplies)
            {
                parts.Add(Loc.Format("ui.cache.part", Loc.Get(new ItemDefinition { id = id }.NameKey), count));
            }

            if (taken.Jewel != null)
            {
                parts.Add(Loc.Get(new ItemDefinition { id = taken.Jewel.item }.NameKey));
            }

            var line = taken.Note > 0
                ? Loc.Format("ui.cache.note", Loc.Get($"tip.{SliceCaches.NoteTip(taken.Note)}.line"))
                : parts.Count > 0 ? Loc.Format("ui.cache.goods", string.Join(", ", parts)) : string.Empty;
            if (taken.Bronze > 0)
            {
                var coins = Loc.Format("ui.cache.bronze", Coins.Show(taken.Bronze));
                line = line.Length > 0 ? line + " " + coins : coins;
            }

            return line;
        }

        private void Glint(bool on)
        {
            if (on && _glint == null)
            {
                var glint = new GameObject("Glint");
                glint.transform.SetParent(transform, false);
                glint.transform.localPosition = Vector3.up * GlintHeight;
                _glint = glint.AddComponent<Light>();
                _glint.type = LightType.Point;
                _glint.range = GlintRange;
                _glint.color = new Color(1f, 0.9f, 0.6f);
                _glint.shadows = LightShadows.None;
            }

            if (_glint == null)
            {
                return;
            }

            _glint.enabled = on;
            if (on)
            {
                _glint.intensity = GlintIntensity * (0.6f + (0.4f * Mathf.Sin(Time.unscaledTime * GlintPulse)));
            }
        }
    }
}

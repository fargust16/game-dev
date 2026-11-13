using System.Linq;
using GameDev.Core.Scenes;
using GameDev.Locomotion;
using GameDev.Slice.Fights;
using GameDev.World.Timekeeping;
using UnityEngine;

namespace GameDev.Slice.People
{
    /// <summary>
    /// 10.4 §6 *Accusation* and *Allies who can fight* (VS-ASSAULT (h), owner-confirmed 2026-11-13; S5c.7): a Warden the
    /// character ran from follows it and warns it twice, the second time «or I shoot», each warning leaving it one world
    /// tick to stop. Ignored a third time, it fights: the Accusation's Fight, an Assault on the Warden. It runs straight at
    /// the Wardens' run while it sees the character within <see cref="SightReach"/>, and to where it last saw it when it
    /// does not; there, with the character still out of sight, or stopped by a wall, it gives up and walks back to its
    /// post, and for <see cref="WardenAccusation.QuietTicks"/> ticks no Warden stops the character. A character who stops
    /// where it sees it is caught up with, and the Accusation opens again. The world paused, it waits.
    /// </summary>
    public sealed class WardenPursuit : MonoBehaviour
    {
        /// <summary>Authoring defaults: how far it sees the character, m, the Alarm's hearing; how near it comes to speak, m; below which speed, m/s, the character has stopped.</summary>
        public const float SightReach = 30f;

        public const float CatchUp = 2f;
        public const float StoppedSpeed = 0.3f;

        /// <summary>Authoring default, s: how long the character stands for it to have stopped.</summary>
        public const float StopSeconds = 0.5f;

        /// <summary>10.4 *Allies who can fight*: two warnings, each leaving a tick; the third time is a fight.</summary>
        public const int Warnings = 2;

        private const float Arrive = 0.8f;
        private const float NearSlack = 0.1f; // m, so a Warden that stopped at its reach counts as near
        private const float EyeHeight = 1.6f;
        private static readonly int RunState = Animator.StringToHash("Run");
        private static readonly int DownHash = Animator.StringToHash("Down");

        private WardenAccusation _accusation;
        private PlayerLocomotion _player;
        private Animator _animator;
        private Vector3 _seen;
        private Vector3 _lastFeet;
        private long _warnedAt;
        private float _still;
        private bool _ended;

        /// <summary>The warnings given so far, 0–2.</summary>
        public int Warned { get; private set; }

        /// <summary>Its post, where it walks back to, and its facing there.</summary>
        public Vector3 Post { get; private set; }

        public Quaternion PostFacing { get; private set; }

        /// <summary>
        /// <paramref name="warden"/> follows the character, having warned it <paramref name="warned"/> times already; it warns
        /// once more at once, the character having run again.
        /// </summary>
        public static WardenPursuit Begin(Transform warden, WardenAccusation accusation, int warned, Vector3 post, Quaternion facing)
        {
            var pursuit = warden.gameObject.AddComponent<WardenPursuit>();
            pursuit._accusation = accusation;
            pursuit.Warned = Mathf.Clamp(warned, 0, Warnings);
            pursuit.Post = post;
            pursuit.PostFacing = facing;
            pursuit._animator = warden.GetComponentInChildren<Animator>();
            pursuit._player = FindAnyObjectByType<PlayerLocomotion>();
            pursuit._seen = pursuit._player != null ? pursuit._player.transform.position : warden.position;
            pursuit._lastFeet = pursuit._seen;
            pursuit._warnedAt = GameClockDriver.Running != null ? GameClockDriver.Running.Tick : 0;
            if (pursuit.Warned < Warnings)
            {
                pursuit.Warn();
            }

            return pursuit;
        }

        /// <summary>Whether the Warden at <paramref name="from"/> sees the character's head: within reach, nothing but people between.</summary>
        public static bool Sees(Transform from, Vector3 feet, float reach)
        {
            var eye = from.position + (Vector3.up * EyeHeight);
            var head = feet + (Vector3.up * EyeHeight);
            var line = head - eye;
            if (line.sqrMagnitude > reach * reach)
            {
                return false;
            }

            return Physics.RaycastAll(eye, line.normalized, line.magnitude, ~0, QueryTriggerInteraction.Ignore).All(h => PeopleAround.IsPerson(h.collider));
        }

        private void Update()
        {
            var clock = GameClockDriver.Running;
            var world = SliceWorldDriver.Running;
            if (_ended || clock == null || clock.IsPaused || SceneTransition.IsTraveling)
            {
                return;
            }

            _player = _player != null ? _player : FindAnyObjectByType<PlayerLocomotion>();
            if (world == null || world.Fights == null || _player == null || Lying())
            {
                Finish(false);
                return;
            }

            if (world.Fights.InFight)
            {
                Finish(false); // another fight took the character: that fight decides
                return;
            }

            var feet = _player.transform.position;
            var moved = Vector3.ProjectOnPlane(feet - _lastFeet, Vector3.up).magnitude;
            _still = Time.deltaTime > 0f && moved / Time.deltaTime < StoppedSpeed ? _still + Time.deltaTime : 0f;
            var stopped = _still >= StopSeconds;
            _lastFeet = feet;
            var sees = Sees(transform, feet, SightReach);
            if (sees)
            {
                _seen = feet;
            }

            // It runs first: to the character it sees, to where it last saw it otherwise.
            var target = sees ? feet : _seen;
            var arrived = FightSearch.Step(transform, target, SliceConfront.WardenRunSpeed, sees ? CatchUp : Arrive, stopAtWall: true);
            var flat = Vector3.ProjectOnPlane(feet - transform.position, Vector3.up).magnitude;

            // Near: within its reach, or as near as a wall lets it come within the Accusation's own reach, from where a Warden first speaks.
            var near = flat <= CatchUp + NearSlack || (sees && arrived && flat <= WardenAccusation.Reach);
            if (sees && stopped && near)
            {
                Finish(false);
                _accusation.CaughtUp(transform, Warned, Post, PostFacing); // complied: the talk again
                return;
            }

            // Each warning leaves the character a tick to stop; a tick out of sight runs on until it sees the character again.
            if (sees && !stopped && clock.Tick >= _warnedAt + 1)
            {
                if (Warned < Warnings)
                {
                    Warn();
                }
                else if (world.Fights.StartAssault(transform, opened: true))
                {
                    world.Fights.SendBack(transform, Post, PostFacing);
                    Finish(false);
                    return;
                }
            }

            Play(RunState);
            if (arrived && !sees)
            {
                Finish(true); // the place it last saw the character, or a wall: it gives up
            }
        }

        private void Warn()
        {
            Warned++;
            _warnedAt = GameClockDriver.Running != null ? GameClockDriver.Running.Tick : 0;
            SliceConfront.Say(transform, Warned == 1 ? "ui.pursuit.warn.1" : "ui.pursuit.warn.2");
        }

        private void Finish(bool gaveUp)
        {
            if (_ended)
            {
                return;
            }

            _ended = true;
            if (gaveUp)
            {
                WardenErrand.Send(transform, new GameObject[0], Post, PostFacing); // it walks back, taking up nobody
            }

            _accusation?.PursuitOver(this, gaveUp);
            Destroy(this);
        }

        private bool Lying() =>
            _animator != null && _animator.isActiveAndEnabled && _animator.parameters.Any(p => p.nameHash == DownHash) && _animator.GetBool(DownHash);

        private int _playing;

        private void Play(int state)
        {
            if (_playing == state || _animator == null || !_animator.isActiveAndEnabled)
            {
                return;
            }

            var playing = _animator.HasState(0, state) ? state : FightSearch.WalkStateHash;
            if (_animator.HasState(0, playing))
            {
                _playing = state;
                _animator.CrossFadeInFixedTime(playing, 0.2f, 0);
            }
        }

        private void OnDestroy()
        {
            if (!_ended)
            {
                _ended = true;
                _accusation?.PursuitOver(this, false);
            }
        }
    }
}

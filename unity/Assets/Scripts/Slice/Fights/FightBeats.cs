using System;
using System.Collections.Generic;
using GameDev.Confront.Rules;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// What the fight shows, one beat after another (S3a.5): the turns resolve at once, and the bodies catch up, walking
    /// their paths a cell at a time in the walk clip and playing each blow, then the hit or the death it brought. The
    /// next turn waits while a beat plays, so a fight reads as it happened. Presentation only: it changes no combatant.
    /// </summary>
    public sealed class FightBeats
    {
        // Authoring defaults, S3a.5: the walk's speed, m/s, as the player's walk; the blow's moment of impact for a body
        // with no blow clip and the hold after any impact, s; the turn of a body toward where it goes, degrees a second.
        public const float WalkSpeed = 2f;
        public const float Impact = 0.35f;
        public const float StrikeHold = 0.65f;
        public const float TurnSpeed = 720f;

        // Measured (SliceAnimationCheck, owner's play test of 2026-11-11: the hit came late): the moment each blow clip
        // reaches its target, s into the clip, by the Weapon value 1–5 (sword slash, greatsword slash, stab, pistol shot,
        // rifle shot); and the Hit clip's first frame of motion, 0.13 s, entered a little before it so the body answers
        // the frame the blow lands. Blows and hits are cross-faded into directly, never through the guard's trigger.
        private static readonly float[] Contact = { 0.57f, 0.90f, 0.53f, 0.37f, 0.23f };
        public const float HitOnset = 0.10f;
        private const float BlowFade = 0.08f;
        private const float HitFade = 0.04f;

        private static readonly int FightingHash = Animator.StringToHash("Fighting");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DownHash = Animator.StringToHash("Down");
        private static readonly int WeaponHash = Animator.StringToHash("Weapon");
        private static readonly int WalkState = Animator.StringToHash("Walk");
        private static readonly int FightState = Animator.StringToHash("Fight");
        private static readonly int AttackState = Animator.StringToHash("Attack");
        private static readonly int HitState = Animator.StringToHash("Hit");

        private readonly Queue<Beat> _beats = new Queue<Beat>();
        private readonly Func<Transform, Animator> _animated;
        private readonly Action<Transform, Vector3, Quaternion> _place;
        private Beat _current;
        private float _time;
        private float _impact;
        private int _leg;
        private bool _struck;

        /// <param name="animated">The body's Animator with the fight clips, or null.</param>
        /// <param name="place">Sets a body where it stands and how it faces.</param>
        public FightBeats(Func<Transform, Animator> animated, Action<Transform, Vector3, Quaternion> place)
        {
            _animated = animated;
            _place = place;
        }

        /// <summary>Speeds every beat up, for tests.</summary>
        public float Pace { get; set; } = 1f;

        /// <summary>True while a beat plays or waits.</summary>
        public bool Busy => _current != null || _beats.Count > 0;

        /// <summary>Raised as a blow lands, with the attacker's body, the target's and what it did, for the fight's log.</summary>
        public event Action<Beat> Landed;

        /// <summary>Raised as a beat begins, a walk or a blow, for bodies that play their own clips (S3b.2's beasts).</summary>
        public event Action<Beat> Started;

        public void Clear()
        {
            _beats.Clear();
            _current = null;
        }

        /// <summary>A body walks through these points, the centres of the cells it stepped into.</summary>
        public void Walk(Transform body, IReadOnlyList<Vector3> points)
        {
            if (body == null || points.Count == 0)
            {
                return;
            }

            var beat = new Beat { Body = body, Points = new Vector3[points.Count] };
            for (var i = 0; i < points.Count; i++)
            {
                beat.Points[i] = points[i];
            }

            _beats.Enqueue(beat);
        }

        /// <summary>The attacker turns to its target and strikes; at the impact the target is hit, or falls.</summary>
        public void Strike(Transform attacker, Transform target, float weaponMotion, StrikeResult result, object attackerKey, object targetKey)
        {
            if (attacker == null)
            {
                return;
            }

            _beats.Enqueue(new Beat
            {
                Body = attacker,
                Target = target,
                Weapon = weaponMotion,
                Result = result,
                AttackerKey = attackerKey,
                TargetKey = targetKey,
            });
        }

        /// <summary>Plays the beats on by the frame's time.</summary>
        public void Tick(float deltaTime)
        {
            var dt = deltaTime * Pace;
            while (dt > 0f)
            {
                if (_current == null)
                {
                    if (_beats.Count == 0)
                    {
                        return;
                    }

                    _current = _beats.Dequeue();
                    _time = 0f;
                    _leg = 0;
                    _struck = false;
                    Begin(_current);
                }

                dt = _current.Points != null ? Walking(_current, dt) : Striking(_current, dt);
            }
        }

        private void Begin(Beat beat)
        {
            Started?.Invoke(beat);
            _impact = Impact;
            var animator = beat.Body != null ? _animated(beat.Body) : null;
            if (animator == null)
            {
                return;
            }

            if (beat.Points != null)
            {
                // Out of the guard into the walk: the player's locomotion at a walk, a person's own walk.
                animator.SetBool(FightingHash, false);
                if (Has(animator, SpeedHash))
                {
                    animator.SetFloat(SpeedHash, 1f);
                }
                else if (animator.HasState(0, WalkState))
                {
                    animator.CrossFadeInFixedTime(WalkState, 0.15f);
                }

                return;
            }

            animator.SetFloat(WeaponHash, beat.Weapon);
            if (animator.HasState(0, AttackState))
            {
                // Straight into the blow from whatever the body was doing, so it lands at its clip's own moment.
                animator.SetBool(FightingHash, true);
                animator.CrossFadeInFixedTime(AttackState, BlowFade, 0);
                _impact = ContactOf(beat.Weapon);
                return;
            }

            animator.SetTrigger(AttackHash);
        }

        /// <summary>The moment the blow clip of this Weapon value reaches its target, s.</summary>
        public static float ContactOf(float weapon)
        {
            var i = Mathf.RoundToInt(weapon) - 1;
            return i >= 0 && i < Contact.Length ? Contact[i] : Impact;
        }

        /// <summary>Moves the body along its points; returns the time left over once it arrives.</summary>
        private float Walking(Beat beat, float dt)
        {
            var body = beat.Body;
            if (body == null)
            {
                _current = null;
                return dt;
            }

            while (_leg < beat.Points.Length && dt > 0f)
            {
                var to = beat.Points[_leg];
                var at = body.position;
                var flat = to - at;
                flat.y = 0f;
                var distance = flat.magnitude;
                var facing = distance > 1e-3f ? Quaternion.RotateTowards(body.rotation, Quaternion.LookRotation(flat), TurnSpeed * dt) : body.rotation;
                var step = WalkSpeed * dt;
                if (step >= distance)
                {
                    dt -= distance / WalkSpeed;
                    _place(body, to, facing);
                    _leg++;
                    continue;
                }

                var next = Vector3.MoveTowards(at, to, step);
                _place(body, next, facing);
                dt = 0f;
            }

            if (_leg < beat.Points.Length)
            {
                return 0f;
            }

            var animator = _animated(body);
            if (animator != null)
            {
                if (Has(animator, SpeedHash))
                {
                    animator.SetFloat(SpeedHash, 0f);
                    animator.SetBool(FightingHash, true);
                }
                else
                {
                    animator.SetBool(FightingHash, true);
                    animator.CrossFadeInFixedTime(FightState, 0.2f);
                }
            }

            _current = null;
            return dt;
        }

        /// <summary>Turns the attacker, lands the blow at its moment, and holds to the blow's end.</summary>
        private float Striking(Beat beat, float dt)
        {
            var body = beat.Body;
            if (body != null && beat.Target != null)
            {
                var look = beat.Target.position - body.position;
                look.y = 0f;
                if (look.sqrMagnitude > 1e-4f)
                {
                    _place(body, body.position, Quaternion.RotateTowards(body.rotation, Quaternion.LookRotation(look), TurnSpeed * dt));
                }
            }

            var before = _time;
            _time += dt;
            if (!_struck && _time >= _impact)
            {
                _struck = true;
                Land(beat);
            }

            if (_time < _impact + StrikeHold)
            {
                return 0f;
            }

            _current = null;
            return _time - Mathf.Max(before, _impact + StrikeHold);
        }

        private void Land(Beat beat)
        {
            var struck = beat.Target != null ? _animated(beat.Target) : null;
            if (struck != null)
            {
                if (beat.Result.Downed)
                {
                    struck.SetBool(DownHash, true);
                }
                else if (beat.Result.Damage > 0d && struck.HasState(0, HitState))
                {
                    struck.CrossFadeInFixedTime(HitState, HitFade, 0, HitOnset);
                }
                else if (beat.Result.Damage > 0d)
                {
                    struck.SetTrigger(HitHash);
                }
            }

            Landed?.Invoke(beat);
        }

        private static bool Has(Animator animator, int hash)
        {
            foreach (var p in animator.parameters)
            {
                if (p.nameHash == hash)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>One walk or one blow.</summary>
        public sealed class Beat
        {
            internal Transform Body;
            internal Vector3[] Points;
            internal Transform Target;
            internal float Weapon;

            public StrikeResult Result { get; internal set; }

            /// <summary>The attacker, as the fight's host named it.</summary>
            public object AttackerKey { get; internal set; }

            /// <summary>The target, as the fight's host named it.</summary>
            public object TargetKey { get; internal set; }
        }
    }
}

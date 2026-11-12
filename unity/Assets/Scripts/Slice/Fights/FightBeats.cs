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

        // AN4 (pipeline plan §11): the Weapon values of the controllers' guard, blow and load blends, which SliceClips
        // builds from these tables: a sword, an axe or a mace; a two-handed weapon; a dagger; a pistol; a rifle; a weapon
        // and a shield; two weapons, the main hand's blow and the other hand's.
        public const int OneHand = 1;
        public const int TwoHand = 2;
        public const int Dagger = 3;
        public const int Pistol = 4;
        public const int Rifle = 5;
        public const int Shield = 6;
        public const int PairMain = 7;
        public const int PairOff = 8;

        public static readonly string[] Guards = { "Guard1H", "Guard2H", "Guard1H", "PistolAim", "RifleAim", "ShieldGuard", "Guard1H", "Guard1H" };
        public static readonly string[] Blows = { "Attack1H", "Attack2H", "Attack1H", "PistolShot", "RifleShot", "ShieldAttack", "Attack1H", "Attack1HLeft" };

        /// <summary>A shot's load, played after it (10.2 §5b: the shot includes loading); null where nothing loads.</summary>
        public static readonly string[] Loads = { null, null, null, "PistolLoad", "RifleLoad", null, null, null };

        // The hit blend's Struck values: three plain hits in turn, a critical hit, a hit through a guard that failed, and a
        // parry or a block that held, raised before the blow lands. The death blend's Death values, in turn.
        public const int HitPlain = 1;
        public const int HitCritical = 4;
        public const int HitThroughGuard = 5;
        public const int HitGuarded = 6;
        public static readonly string[] Hits = { "Hit", "HitChest", "HitHead", "Hit2", "GuardHit", "Block" };
        public static readonly string[] Deaths = { "Die", "Die2", "Die3" };

        // Measured by SliceAnimationCheck on the player man (Logs/SliceSurvey/animation_check.txt, AN4): the moment each
        // blow reaches its target, s into the clip, by Weapon value (the striking hand furthest out, a shot's recoil);
        // how long a shot's beat holds after it, the rest of the shot and the whole load; the first frame of motion of
        // each hit, entered a little before it so the body answers the frame the blow lands; the Block clip's height,
        // where a guard that holds meets the blow and the Defensive Stance stands; and the knockback's onset.
        private static readonly float[] Contact = { 0.33f, 0.57f, 0.33f, 0.03f, 0.10f, 0.37f, 0.33f, 0.40f };
        private static readonly float[] ShotHold = { 0f, 0f, 0f, 2.47f, 2.40f, 0f, 0f, 0f };
        private static readonly float[] Onset = { 0.14f, 0.04f, 0f, 0f, 0.04f, 0f };
        public const float BlockPeak = 0.63f;
        public const float KnockOnset = 0f;
        private const float BlowFade = 0.08f;
        private const float HitFade = 0.04f;
        private const float GuardFade = 0.06f;

        private static readonly int FightingHash = Animator.StringToHash("Fighting");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int MoveZHash = Animator.StringToHash("MoveZ");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DownHash = Animator.StringToHash("Down");
        private static readonly int WeaponHash = Animator.StringToHash("Weapon");
        private static readonly int StruckHash = Animator.StringToHash("Struck");
        private static readonly int DeathHash = Animator.StringToHash("Death");
        private static readonly int WalkState = Animator.StringToHash("Walk");
        private static readonly int FightState = Animator.StringToHash("Fight");
        private static readonly int AttackState = Animator.StringToHash("Attack");
        private static readonly int HitState = Animator.StringToHash("Hit");
        private static readonly int KnockbackState = Animator.StringToHash("Knockback");

        private readonly Queue<Beat> _beats = new Queue<Beat>();
        private readonly Func<Transform, Animator> _animated;
        private readonly Action<Transform, Vector3, Quaternion> _place;
        private Beat _current;
        private float _time;
        private float _impact;
        private float _hold;
        private int _leg;
        private bool _struck;
        private bool _guarded;
        private int _hits;
        private int _deaths;

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

        /// <summary>Raised as a blow's beat ends, so its attacker can take back the pose it fights in (a Defensive Stance).</summary>
        public event Action<Beat> Finished;

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

        /// <summary>A change of a body's pose in its turn among the beats, such as a stance taken or mercy asked.</summary>
        public void Pose(Transform body, Action apply)
        {
            if (body != null && apply != null)
            {
                _beats.Enqueue(new Beat { Body = body, Apply = apply });
            }
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
                    _guarded = false;
                    if (_current.Apply != null)
                    {
                        _current.Apply();
                        _current = null;
                        continue;
                    }

                    Begin(_current);
                }

                dt = _current.Points != null ? Walking(_current, dt) : Striking(_current, dt);
            }
        }

        private void Begin(Beat beat)
        {
            Started?.Invoke(beat);
            _impact = Impact;
            _hold = StrikeHold;
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
                    if (Has(animator, MoveZHash))
                    {
                        animator.SetFloat(MoveZHash, 1f); // the player's eight-way blend, walking forward
                    }
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
                _hold = HoldOf(beat.Weapon);
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

        /// <summary>How long a blow's beat holds after its contact, s: a shot's rest and its whole load, else <see cref="StrikeHold"/>.</summary>
        public static float HoldOf(float weapon)
        {
            var i = Mathf.RoundToInt(weapon) - 1;
            return i >= 0 && i < ShotHold.Length && ShotHold[i] > 0f ? ShotHold[i] : StrikeHold;
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
                    if (Has(animator, MoveZHash))
                    {
                        animator.SetFloat(MoveZHash, 0f);
                    }

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
            if (!_guarded && beat.Result.Guarded && _time >= _impact - BlockPeak)
            {
                // A parry or a block that holds is raised so that its height meets the blow.
                _guarded = true;
                React(beat.Target, HitGuarded, Mathf.Max(0f, BlockPeak - _impact), GuardFade);
            }

            if (!_struck && _time >= _impact)
            {
                _struck = true;
                Land(beat);
            }

            if (_time < _impact + _hold)
            {
                return 0f;
            }

            _current = null;
            Finished?.Invoke(beat);
            return _time - Mathf.Max(before, _impact + _hold);
        }

        /// <summary>The target into one of its hits, at the given point of the clip.</summary>
        private void React(Transform target, int kind, float offset, float fade)
        {
            var struck = target != null ? _animated(target) : null;
            if (struck == null || !struck.HasState(0, HitState))
            {
                return;
            }

            struck.SetFloat(StruckHash, kind);
            struck.CrossFadeInFixedTime(HitState, fade, 0, offset);
        }

        /// <summary>
        /// The blow lands: the target falls in one of its deaths in turn; a stun knocks it down, and it gets up; a critical hit,
        /// a hit through a guard that failed, or one of three plain hits in turn; a guard that held is already up.
        /// </summary>
        private void Land(Beat beat)
        {
            var struck = beat.Target != null ? _animated(beat.Target) : null;
            var result = beat.Result;
            if (struck != null)
            {
                if (result.Downed)
                {
                    struck.SetFloat(DeathHash, 1 + (_deaths++ % Deaths.Length));
                    struck.SetBool(DownHash, true);
                }
                else if (result.Guarded)
                {
                    // raised before the blow
                }
                else if (result.Stunned && struck.HasState(0, KnockbackState))
                {
                    struck.CrossFadeInFixedTime(KnockbackState, HitFade, 0, KnockOnset);
                }
                else if (result.Damage > 0d && struck.HasState(0, HitState))
                {
                    var kind = result.Critical ? HitCritical : result.GuardTried ? HitThroughGuard : HitPlain + (_hits++ % 3);
                    React(beat.Target, kind, Onset[kind - 1], HitFade);
                }
                else if (result.Damage > 0d)
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
            internal Action Apply;

            public StrikeResult Result { get; internal set; }

            /// <summary>The attacker, as the fight's host named it.</summary>
            public object AttackerKey { get; internal set; }

            /// <summary>The target, as the fight's host named it.</summary>
            public object TargetKey { get; internal set; }
        }
    }
}

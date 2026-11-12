using System;
using System.Collections.Generic;
using GameDev.Confront.Rules;
using GameDev.Input;
using GameDev.Locomotion;
using GameDev.Slice.Fights;
using UnityEngine;

namespace GameDev.Slice.Gear
{
    /// <summary>
    /// 10.2 §5c in free exploration: R draws the active Weapon Set and sheathes it again; drawn, the character holds its
    /// guard while it stands and walks and runs with the weapon in hand, and a click strikes. A melee blow reaches the
    /// person straight ahead; a blow at someone who is not fighting is an Assault and goes to <see cref="Assault"/>, which
    /// starts the fight, and with nobody there the blow plays at the air. A firearm's click is a shot (§5e, PB8b): the
    /// projectile from the muzzle at what the screen's centre shows, aimed through the sights or not
    /// (<see cref="PlayerAim"/>), and only what it touches is rolled. While the aim is held or the firearm loads, the legs
    /// keep the walk. A sprint, the water, a crouch, work or E sheathes the weapon first. A fight draws and sheathes on its own.
    /// </summary>
    public sealed class PlayerArms : MonoBehaviour
    {
        // Authoring defaults, 10.2 §5c: the draw's 0.4 s before a blow can follow; a melee blow's 1.5 m and 45° either side of
        // the facing; the speed under which the body stands in its guard, m/s.
        public const float DrawSeconds = 0.4f;
        public const float Reach = 1.5f;
        public const float ReachAngle = 45f;
        public const float StandingSpeed = 0.25f;

        private static readonly int FightingHash = Animator.StringToHash("Fighting");
        private static readonly int WeaponHash = Animator.StringToHash("Weapon");
        private static readonly int AttackState = Animator.StringToHash("Attack");

        private SliceSettings _settings;
        private Func<Fighter> _fighter;
        private Func<bool> _inFight;
        private Func<bool> _busy;
        private PlayerLocomotion _player;
        private Animator _animator;
        private HeldWeapons _held;
        private float _drawnAt;
        private float _blow = -1f;
        private float _length;
        private int _motion;
        private int _punches;

        /// <summary>True while the weapon is drawn in free exploration.</summary>
        public bool Drawn { get; private set; }

        /// <summary>True while a blow at the air plays.</summary>
        public bool Striking => _blow >= 0f;

        /// <summary>The FightBeats value of the drawn set's guard and blow.</summary>
        public int Motion => _motion;

        /// <summary>The bodies a blow can reach: the people of the loaded scenes.</summary>
        public Func<IEnumerable<Transform>> People { get; set; }

        /// <summary>
        /// A melee blow at a person: true when it started a fight, which then shows the blow as its opening strike; false
        /// lets the blow play at the air.
        /// </summary>
        public Func<Transform, bool> Assault { get; set; }

        /// <summary>The firearm's aim and shot out of a fight (10.2 §5e); with none a firearm's click does nothing.</summary>
        public PlayerAim Aim { get; set; }

        /// <summary>True while a firearm is drawn.</summary>
        public bool Ranged => _motion == FightBeats.Pistol || _motion == FightBeats.Rifle;

        /// <param name="fighter">The character as a fighter in what it wears and holds.</param>
        /// <param name="inFight">True while a fight runs: it draws and sheathes the weapons itself.</param>
        /// <param name="busy">True while the character works, talks or reads: the weapon goes away.</param>
        public void Configure(SliceSettings settings, Func<Fighter> fighter, Func<bool> inFight, Func<bool> busy)
        {
            _settings = settings;
            _fighter = fighter;
            _inFight = inFight;
            _busy = busy;
        }

        /// <summary>Draws the active Weapon Set: its models in the hands, its guard while the body stands.</summary>
        public bool Draw()
        {
            if (!Find() || !CanDraw())
            {
                return false;
            }

            var fighter = _fighter?.Invoke();
            if (fighter == null)
            {
                return false;
            }

            _motion = Mathf.RoundToInt(SliceConfront.Motion(fighter));
            _held = HeldWeapons.Of(_player);
            _held.Draw(_settings != null ? _settings.WeaponModels : null, fighter.Set);
            _drawnAt = Time.time;
            Drawn = true;
            return true;
        }

        /// <summary>The weapon out of sight and the guard left.</summary>
        public void Sheathe()
        {
            if (_held != null)
            {
                _held.Sheathe();
            }

            if (_animator != null && _animator.isActiveAndEnabled)
            {
                _animator.SetBool(FightingHash, false);
            }

            Drawn = false;
        }

        /// <summary>A click's blow: at the person in reach, which is an Assault, or at the air. False when no blow can be struck now.</summary>
        public bool Strike()
        {
            if (!Drawn || Striking || Time.time < _drawnAt + DrawSeconds || !Find() || !_player.enabled || !_player.IsGrounded ||
                _player.State != PlayerLocomotion.Move.Grounded)
            {
                return false;
            }

            if (Ranged)
            {
                return Aim != null && Aim.Fire();
            }

            var target = Ahead();
            if (target != null && Assault != null && Assault(target))
            {
                return true;
            }

            var motion = _motion;
            if (motion == FightBeats.Fists)
            {
                motion = _punches % 2 == 0 ? FightBeats.Fists : FightBeats.FistsLeft;
                _punches++;
            }

            _player.enabled = false;
            if (_animator.HasState(0, AttackState))
            {
                _animator.SetFloat(WeaponHash, motion);
                _animator.SetBool(FightingHash, true);
                _animator.CrossFadeInFixedTime(AttackState, 0.08f, 0);
            }

            _length = FightBeats.ContactOf(motion) + FightBeats.HoldOf(motion);
            _blow = 0f;
            return true;
        }

        private void Update()
        {
            if (!Find())
            {
                return;
            }

            if (_inFight != null && _inFight())
            {
                // The fight draws the weapons and sheathes them at its end.
                _blow = -1f;
                Drawn = false;
                return;
            }

            if (Striking)
            {
                _blow += Time.deltaTime;
                if (_blow >= _length)
                {
                    _blow = -1f;
                    _player.enabled = true;
                }

                return;
            }

            var input = GameInputService.Instance;
            if (Drawn && (MustSheathe() || (input != null && input.InteractPressed)))
            {
                Sheathe();
            }

            if (input == null)
            {
                return;
            }

            if (input.DrawWeaponPressed)
            {
                if (Drawn)
                {
                    Sheathe();
                }
                else
                {
                    Draw();
                }
            }

            if (!Drawn)
            {
                return;
            }

            if (_animator.isActiveAndEnabled)
            {
                _animator.SetFloat(WeaponHash, _motion);
                var aiming = Aim != null && (Aim.Held || Aim.Loading);
                _animator.SetBool(FightingHash, !aiming && _player.IsGrounded && _player.State == PlayerLocomotion.Move.Grounded && _player.PlanarSpeed < StandingSpeed && input.MoveVector.sqrMagnitude < 0.01f);
            }

            if (input.AttackPressed)
            {
                Strike();
            }
        }

        private bool Find()
        {
            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerLocomotion>();
                _animator = _player != null ? _player.GetComponentInChildren<Animator>() : null;
            }

            return _player != null && _animator != null;
        }

        private bool CanDraw() => _player.enabled && (_busy == null || !_busy()) && !MustSheathe();

        // A sprint, the water, a climb, a crouch, or work, talk and reading put the weapon away first.
        private bool MustSheathe() =>
            _player.IsSprinting || _player.InWater || _player.State == PlayerLocomotion.Move.Crouched || (_busy != null && _busy());

        /// <summary>The person straight ahead within reach: the nearest within <see cref="Reach"/> and <see cref="ReachAngle"/> of the facing.</summary>
        private Transform Ahead()
        {
            var from = _player.transform.position;
            var face = Vector3.ProjectOnPlane(_player.transform.forward, Vector3.up).normalized;
            Transform best = null;
            var nearest = float.MaxValue;
            foreach (var person in Candidates())
            {
                var way = Vector3.ProjectOnPlane(person.position - from, Vector3.up);
                var distance = way.magnitude;
                if (distance <= Reach && distance < nearest && (distance < 0.05f || Vector3.Angle(face, way) <= ReachAngle) && Mathf.Abs(person.position.y - from.y) < 1.2f)
                {
                    best = person;
                    nearest = distance;
                }
            }

            return best;
        }

        private IEnumerable<Transform> Candidates()
        {
            if (People == null)
            {
                yield break;
            }

            foreach (var person in People())
            {
                if (person != null && person.gameObject.activeInHierarchy && !person.IsChildOf(_player.transform) && !_player.transform.IsChildOf(person))
                {
                    yield return person;
                }
            }
        }
    }
}

using GameDev.Input;
using UnityEngine;

namespace GameDev.Locomotion
{
    /// <summary>
    /// Free third-person movement on a CharacterController (17 L.2): WASD relative to the camera, Shift to sprint, Space
    /// to jump. Moved from GameDev.Player.OverworldPlayerController in S0.3; the Overworld player and the slice player
    /// share it.
    /// <para>
    /// An explicit movement state machine (owner's play test of 2026-11-11: the jump played every other time, the landing
    /// held the walk back so the body glided in its pose, stealth stood upright, and the moves felt slow and sticky). The
    /// code owns the state, <see cref="Move"/>; the moment it changes, the Animator cross-fades to that state's clip, never
    /// waiting for a clip to end, so no pose of an earlier action lingers. The feel follows common practice for
    /// responsive third-person control: velocity reached by a steep acceleration rather than a slow blend, a fast turn, a
    /// ground probe that sticks the body to slopes and steps down instead of falling, coyote time and a jump buffer.
    /// </para>
    /// <para>
    /// The character faces the camera (owner, 2026-11-11; 16 §2) and moves in eight directions: walking, running and
    /// sprinting go any way, the body turned to where the camera looks, and standing still it turns on the spot once the
    /// camera has swung more than <see cref="idleTurnAngle"/> away. A sprint backward is a run, as the clips have none.
    /// In Stealth Mode the body turns to where it goes, as the sneak has only a forward clip.
    /// </para>
    /// <para>
    /// The Animator's states, by name, where the controller has them: <c>Locomotion</c> (a 2D blend on MoveX and MoveZ,
    /// the body's own velocity on the scale 0–3 of idle, walk, run and sprint; a controller with the old 1D blend reads
    /// Speed), <c>TurnLeft</c> and <c>TurnRight</c> (a turn on the spot), <c>Crouch</c> (blend on Speed 0–1, crouched
    /// idle and sneak, its pace on <c>Pace</c> against the sneak's own in <c>SneakPace</c>), <c>Fall</c> and
    /// <c>JumpLand</c>. A controller without one of them falls back to <c>Locomotion</c>.
    /// </para>
    /// Every number is an authoring default.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerLocomotion : MonoBehaviour
    {
        /// <summary>What the body is doing, which decides the clip it plays.</summary>
        public enum Move
        {
            Grounded,
            Crouched,
            Airborne,
            Landing,
        }

        [Header("Speeds (m/s) - match Animator Speed blend positions 0/1/2/3")]
        [SerializeField] private float walkSpeed = 2f;
        [SerializeField] private float runSpeed = 4.5f;
        [SerializeField] private float sprintSpeed = 7.5f;

        [Header("Feel")]
        [Tooltip("m/s² toward the wanted velocity on the ground: a run in about 0.16 s.")]
        [SerializeField] private float groundAcceleration = 28f;
        [Tooltip("m/s² to a stop or a reversal on the ground: from a run in about 0.12 s.")]
        [SerializeField] private float groundDeceleration = 38f;
        [Tooltip("m/s² of steering in the air.")]
        [SerializeField] private float airAcceleration = 9f;
        [Tooltip("Degrees a second the body turns toward where it goes.")]
        [SerializeField] private float turnSpeed = 900f;
        [Tooltip("The body faces the camera and moves in eight directions (owner, 2026-11-11); off, it turns to where it goes.")]
        [SerializeField] private bool faceCamera = true;
        [Tooltip("Degrees the camera swings from a body standing still before it turns on the spot.")]
        [SerializeField] private float idleTurnAngle = 60f;
        [Tooltip("Degrees a second a body standing still turns on the spot.")]
        [SerializeField] private float idleTurnSpeed = 240f;
        [Tooltip("Degrees from the camera's forward within which Shift sprints; further back it runs.")]
        [SerializeField] private float sprintArc = 100f;

        [Header("Jump / Gravity")]
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -18f;
        [Tooltip("s after leaving an edge in which a jump still counts.")]
        [SerializeField] private float coyoteTime = 0.12f;
        [Tooltip("s before landing in which a pressed jump is kept.")]
        [SerializeField] private float jumpBuffer = 0.15f;
        [Tooltip("m: how far below the feet the ground is sought, so slopes and steps down keep the body grounded.")]
        [SerializeField] private float groundSnap = 0.35f;
        [Tooltip("s a landing from a long fall holds when no key is pressed; any key leaves it at once.")]
        [SerializeField] private float landingHold = 0.2f;
        [Tooltip("s in the air before a landing plays at all.")]
        [SerializeField] private float landingAfter = 0.45f;

        private const float LocomotionFade = 0.12f;
        private const float CrouchFade = 0.15f;
        private const float AirFade = 0.08f;
        private const float LandFade = 0.05f;
        private const float BlendDamp = 0.06f;

        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int MoveXParam = Animator.StringToHash("MoveX");
        private static readonly int MoveZParam = Animator.StringToHash("MoveZ");
        private static readonly int TurnLeftState = Animator.StringToHash("TurnLeft");
        private static readonly int TurnRightState = Animator.StringToHash("TurnRight");
        private static readonly int GroundedParam = Animator.StringToHash("Grounded");
        private static readonly int PaceParam = Animator.StringToHash("Pace");
        private static readonly int SneakPaceParam = Animator.StringToHash("SneakPace");
        private static readonly int LocomotionState = Animator.StringToHash("Locomotion");
        private static readonly int CrouchState = Animator.StringToHash("Crouch");
        private static readonly int FallState = Animator.StringToHash("Fall");
        private static readonly int LandState = Animator.StringToHash("JumpLand");

        private CharacterController _controller;
        private Animator _animator;
        private Vector3 _velocity;
        private float _verticalVelocity;
        private float _sinceGround;
        private float _sinceJumpPressed = float.MaxValue;
        private float _airTime;
        private float _stateTime;
        private bool _grounded;
        private int _playing;
        private float _sneakPace;
        private bool _hasPace;
        private bool _hasMove;
        private int _turning;

        /// <summary>The movement state now.</summary>
        public Move State { get; private set; } = Move.Grounded;

        public bool IsGrounded => _grounded;

        /// <summary>The body's speed over the ground now, m/s.</summary>
        public float PlanarSpeed => new Vector3(_velocity.x, 0f, _velocity.z).magnitude;

        /// <summary>
        /// Stealth Mode (10.3 §1, S3b.1): the character moves at this share of walking speed and cannot sprint; zero
        /// when it walks upright. Above zero the body crouches and plays the Crouch state.
        /// </summary>
        public float CrouchShare { get; set; }

        /// <summary>
        /// While false the character cannot sprint, as when Stamina has run out and not yet come back to a tenth (07.2
        /// §3.2); true by default.
        /// </summary>
        public bool SprintAllowed { get; set; } = true;

        /// <summary>True while the character sprints: Shift held, input given, upright and allowed.</summary>
        public bool IsSprinting { get; private set; }

        /// <summary>Puts the player at a pose at once, as a door does; a CharacterController ignores a moved transform
        /// while it is enabled.</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            var controller = _controller != null ? _controller : GetComponent<CharacterController>();
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = true;
            _verticalVelocity = 0f;
            _velocity = Vector3.zero;
            _airTime = 0f;
            Enter(CrouchShare > 0f ? Move.Crouched : Move.Grounded);
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            // Unity's default of 0.001 m drops shorter steps, which at a high frame rate is every step.
            _controller.minMoveDistance = 0f;

            // The capsule rests its skin width over the ground: lift it by that much, so the root, and the feet the
            // model stands on it with, meet the ground (owner's play test of 2026-11-11: the feet hung over it).
            var centre = _controller.center;
            _controller.center = new Vector3(centre.x, (_controller.height / 2f) + _controller.skinWidth, centre.z);
            _animator = GetComponentInChildren<Animator>();
            if (_animator != null)
            {
                _animator.applyRootMotion = false;
                _hasPace = HasParameter(_animator, PaceParam);
                _hasMove = HasParameter(_animator, MoveXParam) && HasParameter(_animator, MoveZParam);
                _sneakPace = HasParameter(_animator, SneakPaceParam) ? _animator.GetFloat(SneakPaceParam) : 0f;
            }

            GameInputService.EnsureExists();
        }

        private void OnEnable()
        {
            // Whoever drove the Animator meanwhile (a fight, a conversation) left it in a state of its own.
            _playing = 0;
            Enter(State == Move.Airborne ? Move.Airborne : CrouchShare > 0f ? Move.Crouched : Move.Grounded, true);
        }

        private void OnDisable()
        {
            // Back to the plain locomotion standing still, from which a fight's guard and a talk are entered.
            _velocity = Vector3.zero;
            IsSprinting = false;
            _turning = 0;
            if (_animator != null && _animator.isActiveAndEnabled)
            {
                _animator.SetFloat(SpeedParam, 0f);
                if (_hasMove)
                {
                    _animator.SetFloat(MoveXParam, 0f);
                    _animator.SetFloat(MoveZParam, 0f);
                }

                Play(LocomotionState, LocomotionFade);
            }
        }

        private void Update()
        {
            var input = GameInputService.Instance;
            if (input == null)
            {
                return;
            }

            var dt = Time.deltaTime;
            _stateTime += dt;
            _sinceJumpPressed = input.JumpPressed ? 0f : _sinceJumpPressed + dt;

            var moveInput = input.MoveVector;
            var hasInput = moveInput.sqrMagnitude > 0.01f;
            var crouching = CrouchShare > 0f;
            var direction = CameraRelativeDirection(new Vector3(moveInput.x, 0f, moveInput.y));
            var look = CameraForward();
            var strafing = faceCamera && !crouching && look.sqrMagnitude > 0.5f;
            IsSprinting = hasInput && !crouching && SprintAllowed && input.SprintHeld &&
                          (!strafing || Vector3.Angle(look, direction) <= sprintArc);

            // The wanted speed: a crouch at its share of the walk, else a run, a sprint with Shift; a light stick walks.
            var wanted = !hasInput ? 0f
                : crouching ? walkSpeed * CrouchShare
                : IsSprinting ? sprintSpeed
                : Mathf.Lerp(walkSpeed, runSpeed, Mathf.InverseLerp(0.5f, 0.9f, moveInput.magnitude));

            // The jump: a press kept for a moment before landing, honoured a moment after leaving an edge.
            if (_sinceJumpPressed <= jumpBuffer && (_grounded || _sinceGround <= coyoteTime) && State != Move.Airborne)
            {
                _sinceJumpPressed = float.MaxValue;
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _grounded = false;
                _sinceGround = coyoteTime + 1f;
                Enter(Move.Airborne);
            }

            // Velocity over the ground, reached by a steep acceleration; a reversal or a stop brakes harder.
            var target = direction * wanted;
            var airborne = State == Move.Airborne;
            var rate = airborne ? airAcceleration
                : Vector3.Dot(target, _velocity) < 0f || target.sqrMagnitude < _velocity.sqrMagnitude ? groundDeceleration
                : groundAcceleration;
            _velocity = Vector3.MoveTowards(_velocity, target, rate * dt);

            _verticalVelocity += gravity * dt;
            if (_grounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f; // pressed to the ground, so CharacterController keeps reporting it
            }

            var flags = _controller.Move((_velocity + (Vector3.up * _verticalVelocity)) * dt);
            if ((flags & CollisionFlags.Sides) != 0)
            {
                // A wall takes the speed it stops, so the body does not press on and burst away when it clears.
                var actual = _controller.velocity;
                _velocity = Vector3.ClampMagnitude(new Vector3(actual.x, 0f, actual.z), _velocity.magnitude);
            }

            if ((flags & CollisionFlags.Above) != 0 && _verticalVelocity > 0f)
            {
                _verticalVelocity = 0f;
            }

            var wasGrounded = _grounded;
            _grounded = _controller.isGrounded || (_verticalVelocity <= 0f && Snap(wasGrounded));
            _sinceGround = _grounded ? 0f : _sinceGround + dt;

            Face(strafing, look, direction, hasInput, dt);
            Step(hasInput, crouching, dt);
            Drive(dt);
        }

        /// <summary>
        /// Turns the body: to the camera's forward while it moves (eight-way movement), on the spot once the camera has
        /// swung far from a body standing still; in Stealth Mode, or with <see cref="faceCamera"/> off, to where it goes.
        /// </summary>
        private void Face(bool strafing, Vector3 look, Vector3 direction, bool hasInput, float dt)
        {
            if (!strafing)
            {
                StopTurning();
                if (direction.sqrMagnitude > 0.0001f)
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction, Vector3.up), turnSpeed * dt);
                }

                return;
            }

            var want = Quaternion.LookRotation(look, Vector3.up);
            var off = Vector3.SignedAngle(Flat(transform.forward), look, Vector3.up);
            if (hasInput)
            {
                StopTurning();
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * dt);
                return;
            }

            if (State != Move.Grounded)
            {
                // A fall or a landing with no key pressed keeps the way the body faced, as a body set down at a door.
                StopTurning();
                return;
            }

            if (_turning == 0 && Mathf.Abs(off) > idleTurnAngle)
            {
                _turning = off > 0f ? 1 : -1;
                if (Has(TurnRightState) && Has(TurnLeftState))
                {
                    Play(_turning > 0 ? TurnRightState : TurnLeftState, LocomotionFade);
                }
            }

            if (_turning != 0)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, idleTurnSpeed * dt);
                if (Mathf.Abs(off) < 2f)
                {
                    StopTurning();
                }
            }
        }

        private void StopTurning()
        {
            if (_turning == 0)
            {
                return;
            }

            _turning = 0;
            if (State == Move.Grounded)
            {
                Play(LocomotionState, LocomotionFade);
            }
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
        }

        /// <summary>The state machine's transitions, decided by what the body does now, never by a clip's end.</summary>
        private void Step(bool hasInput, bool crouching, float dt)
        {
            switch (State)
            {
                case Move.Grounded:
                case Move.Crouched:
                    if (!_grounded && _sinceGround > coyoteTime)
                    {
                        Enter(Move.Airborne);
                    }
                    else if (crouching != (State == Move.Crouched))
                    {
                        Enter(crouching ? Move.Crouched : Move.Grounded);
                    }

                    break;

                case Move.Airborne:
                    _airTime += dt;
                    if (_grounded && _verticalVelocity <= 0f)
                    {
                        // A long fall with no key pressed lands in the knees; otherwise the walk takes over at once.
                        Enter(_airTime >= landingAfter && !hasInput && !crouching ? Move.Landing : crouching ? Move.Crouched : Move.Grounded);
                    }

                    break;

                case Move.Landing:
                    if (!_grounded && _sinceGround > coyoteTime)
                    {
                        Enter(Move.Airborne);
                    }
                    else if (hasInput || crouching || _stateTime >= landingHold)
                    {
                        Enter(crouching ? Move.Crouched : Move.Grounded);
                    }

                    break;
            }
        }

        private void Enter(Move next, bool force = false)
        {
            if (next == State && !force)
            {
                return;
            }

            State = next;
            _stateTime = 0f;
            _turning = 0;
            if (next == Move.Airborne)
            {
                _airTime = 0f;
            }

            switch (next)
            {
                case Move.Grounded:
                    Play(LocomotionState, LocomotionFade);
                    break;
                case Move.Crouched:
                    Play(Has(CrouchState) ? CrouchState : LocomotionState, CrouchFade);
                    break;
                case Move.Airborne:
                    Play(Has(FallState) ? FallState : LocomotionState, AirFade);
                    break;
                case Move.Landing:
                    Play(Has(LandState) ? LandState : LocomotionState, LandFade);
                    break;
            }
        }

        /// <summary>The blends follow the body's real speed, so the clips' steps keep pace with the ground.</summary>
        private void Drive(float dt)
        {
            if (_animator == null)
            {
                return;
            }

            var speed = PlanarSpeed;
            float blend;
            var pace = 1f;
            if (State == Move.Crouched)
            {
                var creep = Mathf.Max(0.01f, walkSpeed * Mathf.Max(CrouchShare, 0.01f));
                blend = Mathf.Clamp01(speed / creep);
                if (_sneakPace > 0.01f && speed > 0.1f)
                {
                    pace = Mathf.Clamp(speed / _sneakPace, 0.6f, 1.4f);
                }
            }
            else
            {
                blend = speed <= walkSpeed ? speed / walkSpeed
                    : speed <= runSpeed ? 1f + ((speed - walkSpeed) / (runSpeed - walkSpeed))
                    : 2f + Mathf.Clamp01((speed - runSpeed) / (sprintSpeed - runSpeed));
            }

            _animator.SetFloat(SpeedParam, blend, BlendDamp, dt);
            if (_hasMove)
            {
                // The body's own velocity, its length on the blend's scale: the eight-way clips by direction and pace.
                var local = transform.InverseTransformDirection(new Vector3(_velocity.x, 0f, _velocity.z));
                local.y = 0f;
                var way = local.sqrMagnitude > 1e-4f ? local.normalized * blend : Vector3.zero;
                _animator.SetFloat(MoveXParam, State == Move.Crouched ? 0f : way.x, BlendDamp, dt);
                _animator.SetFloat(MoveZParam, State == Move.Crouched ? blend : way.z, BlendDamp, dt);
            }

            _animator.SetBool(GroundedParam, _grounded);
            if (_hasPace)
            {
                _animator.SetFloat(PaceParam, pace);
            }
        }

        /// <summary>
        /// Seeks the ground under the feet within <see cref="groundSnap"/> and, if the body stood on ground a moment ago,
        /// sets it down there: a step down or a slope keeps it walking instead of falling for a frame.
        /// </summary>
        private bool Snap(bool wasGrounded)
        {
            var radius = _controller.radius * 0.9f;
            var bottom = transform.position + _controller.center - (Vector3.up * ((_controller.height / 2f) - _controller.radius));
            if (!Physics.SphereCast(bottom, radius, Vector3.down, out var hit, groundSnap + _controller.skinWidth, ~0, QueryTriggerInteraction.Ignore) ||
                hit.collider.transform.IsChildOf(transform) || Vector3.Angle(hit.normal, Vector3.up) > _controller.slopeLimit)
            {
                return false;
            }

            if (!wasGrounded)
            {
                return hit.distance <= _controller.skinWidth + 0.02f;
            }

            var drop = hit.distance - _controller.skinWidth;
            if (drop > 0.001f)
            {
                _controller.Move(Vector3.down * drop);
            }

            return true;
        }

        private void Play(int state, float fade)
        {
            if (_animator == null || !_animator.isActiveAndEnabled || _playing == state || !_animator.HasState(0, state))
            {
                return;
            }

            _animator.CrossFadeInFixedTime(state, fade, 0);
            _playing = state;
        }

        private bool Has(int state) => _animator != null && _animator.isActiveAndEnabled && _animator.HasState(0, state);

        private static bool HasParameter(Animator animator, int hash)
        {
            if (animator.runtimeAnimatorController == null)
            {
                return false;
            }

            foreach (var p in animator.parameters)
            {
                if (p.nameHash == hash)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The camera's forward over the ground, or zero with no camera.</summary>
        private static Vector3 CameraForward()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                return Vector3.zero;
            }

            var forward = cam.transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.zero;
        }

        private static Vector3 CameraRelativeDirection(Vector3 input)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                return input.normalized;
            }

            var forward = cam.transform.forward;
            forward.y = 0f;
            forward.Normalize();
            var right = cam.transform.right;
            right.y = 0f;
            right.Normalize();
            var dir = forward * input.z + right * input.x;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
        }
    }
}

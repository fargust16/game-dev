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
    /// The body always faces where it goes (owner, 2026-11-12; 16 §2): the keys pick a way relative to the camera, the body
    /// turns to it at <see cref="turnSpeed"/> and walks, runs or sprints face first, never backward or sideways. The camera
    /// only looks; standing still, the body keeps the way it last faced. This replaces the owner's earlier choice of
    /// 2026-11-11, a body facing the camera and stepping in eight directions.
    /// </para>
    /// <para>
    /// The Animator's states, by name, where the controller has them: <c>Locomotion</c> (a 2D blend on MoveX and MoveZ
    /// on the scale 0–3 of idle, walk, run and sprint, driven straight ahead only, MoveX staying 0; a controller with the
    /// old 1D blend reads Speed), <c>Crouch</c> (blend on Speed 0–1, crouched
    /// idle and sneak, its pace on <c>Pace</c> against the sneak's own in <c>SneakPace</c>), <c>Fall</c> and
    /// <c>JumpLand</c>, <c>Swim</c> (blend on Speed 0–1, the float and the stroke, played faster on Pace for the fast
    /// swim) and <c>ClimbOut</c>. A controller without one of them falls back to <c>Locomotion</c>.
    /// </para>
    /// <para>
    /// Swimming (10.1, AN5): where a <see cref="SwimWater"/> lies <see cref="wadeDepth"/> or more over the bed, the
    /// character swims, free as a run, the fast swim with Shift spending Stamina as the sprint, at half speed when
    /// overloaded (<see cref="SwimShare"/>), its body turned to where it goes. Space at a bank whose top stands no more
    /// than <see cref="climbOutHeight"/> over the water climbs out onto it; where the bed rises the character wades out.
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
            Swimming,
            Climbing,
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
        [Tooltip("Degrees a second the body turns toward where it goes: a turn round in a fifth of a second.")]
        [SerializeField] private float turnSpeed = 900f;

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

        [Header("Swimming (10.1, AN5)")]
        [Tooltip("m of water over the bed from which the character swims: chest-deep on a person 1.8 m tall (authoring default).")]
        [SerializeField] private float wadeDepth = 1.3f;
        [Tooltip("m the root floats over the water while swimming: the Swim clip holds the chest 0.12 m under the root and the head from 0.04 m under it to 0.08 m over (measured, SliceAnimationCheck).")]
        [SerializeField] private float floatHeight = 0.05f;
        [SerializeField] private float swimSpeed = 1.8f;
        [SerializeField] private float fastSwimSpeed = 3f;
        [Tooltip("m/s² toward the wanted velocity in the water.")]
        [SerializeField] private float swimAcceleration = 6f;
        [Tooltip("m over the water a bank's top may stand for Space to climb out onto it: the city's quays stand 1.9 m over the sea (measured: built ground 25.9 m, Sea Level 24 m).")]
        [SerializeField] private float climbOutHeight = 2.1f;
        [SerializeField] private float climbOutSeconds = 0.8f;

        private const float LocomotionFade = 0.12f;
        private const float CrouchFade = 0.15f;
        private const float AirFade = 0.08f;
        private const float LandFade = 0.05f;
        private const float BlendDamp = 0.06f;

        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int MoveXParam = Animator.StringToHash("MoveX");
        private static readonly int MoveZParam = Animator.StringToHash("MoveZ");
        private static readonly int GroundedParam = Animator.StringToHash("Grounded");
        private static readonly int PaceParam = Animator.StringToHash("Pace");
        private static readonly int SneakPaceParam = Animator.StringToHash("SneakPace");
        private static readonly int LocomotionState = Animator.StringToHash("Locomotion");
        private static readonly int CrouchState = Animator.StringToHash("Crouch");
        private static readonly int FallState = Animator.StringToHash("Fall");
        private static readonly int LandState = Animator.StringToHash("JumpLand");
        private static readonly int SwimState = Animator.StringToHash("Swim");
        private static readonly int ClimbState = Animator.StringToHash("ClimbOut");

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
        private float _climbTime;
        private Vector3 _climbFrom;
        private Vector3 _climbTo;

        /// <summary>The movement state now.</summary>
        public Move State { get; private set; } = Move.Grounded;

        public bool IsGrounded => _grounded;

        /// <summary>True while the character swims or climbs out of the water: no fight starts, and beasts give up (10.1).</summary>
        public bool InWater => State == Move.Swimming || State == Move.Climbing;

        /// <summary>The share of swimming speed: a half when the character is overloaded (10.1), set by whoever holds the bag.</summary>
        public float SwimShare { get; set; } = 1f;

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
            // A climb out of the water broken off ends on the bank.
            if (State == Move.Climbing && _controller != null)
            {
                transform.position = _climbTo;
                _controller.enabled = true;
                State = Move.Grounded;
            }

            // Back to the plain locomotion standing still, from which a fight's guard and a talk are entered.
            _velocity = Vector3.zero;
            IsSprinting = false;
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
            if (State == Move.Climbing)
            {
                Climb(dt);
                Drive(dt);
                return;
            }

            var water = SwimWater.LevelAt(transform.position, out var level);
            if (State == Move.Swimming)
            {
                if (!water)
                {
                    Enter(Move.Airborne);
                }
                else if (Swim(input, hasInput, direction, level, dt))
                {
                    return;
                }
                else
                {
                    // Wading out: the root set down on the rising bed, where the standing pose holds the chest as the stroke did.
                    WadeOut(level);
                }
            }

            IsSprinting = hasInput && !crouching && SprintAllowed && input.SprintHeld;

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

            Face(direction, dt);
            Step(hasInput, crouching, dt);
            if (water && Deep(level))
            {
                _verticalVelocity = 0f;
                IsSprinting = false;
                Enter(Move.Swimming);
            }

            Drive(dt);
        }

        /// <summary>The character has gone into water at least <see cref="wadeDepth"/> deep, walking or falling in.</summary>
        private bool Deep(float level) => transform.position.y <= level - wadeDepth + 0.05f && Bed(level) <= level - wadeDepth;

        /// <summary>The bed under the water at the character's place: the first thing under the surface, or far down.</summary>
        private float Bed(float level)
        {
            var from = new Vector3(transform.position.x, level - 0.01f, transform.position.z);
            return Physics.Raycast(from, Vector3.down, out var hit, 50f, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(transform)
                ? hit.point.y
                : level - 50f;
        }

        /// <summary>One frame in the water; false once the bed has risen to wading depth.</summary>
        private bool Swim(GameInputService input, bool hasInput, Vector3 direction, float level, float dt)
        {
            if (Bed(level) > level - wadeDepth + 0.15f)
            {
                return false;
            }

            if (input.JumpPressed && Bank(level, out var top))
            {
                _climbFrom = transform.position;
                _climbTo = top;
                _climbTime = 0f;
                _controller.enabled = false;
                IsSprinting = false;
                Enter(Move.Climbing);
                Drive(dt);
                return true;
            }

            // The fast swim spends Stamina as the sprint does (07.2 §3.2).
            IsSprinting = hasInput && SprintAllowed && input.SprintHeld;
            var wanted = !hasInput ? 0f : (IsSprinting ? fastSwimSpeed : swimSpeed) * Mathf.Clamp01(SwimShare);
            _velocity = Vector3.MoveTowards(_velocity, direction * wanted, swimAcceleration * dt);

            // The root eased to its float over the water, so a body that fell in comes up and one that waded in rises.
            _verticalVelocity = Mathf.Clamp((level + floatHeight - transform.position.y) * 6f, -4f, 4f);
            var flags = _controller.Move((_velocity + (Vector3.up * _verticalVelocity)) * dt);
            if ((flags & CollisionFlags.Sides) != 0)
            {
                var actual = _controller.velocity;
                _velocity = Vector3.ClampMagnitude(new Vector3(actual.x, 0f, actual.z), _velocity.magnitude);
            }

            _grounded = false;
            _sinceGround = 0f;
            Face(direction, dt);
            Drive(dt);
            return true;
        }

        private void WadeOut(float level)
        {
            var bed = Bed(level);
            var drop = transform.position.y - bed;
            if (drop > 0f && drop < 3f)
            {
                _controller.Move(Vector3.down * drop);
            }

            _verticalVelocity = -2f;
            _grounded = true;
            Enter(Move.Grounded);
        }

        /// <summary>The top of a bank ahead within <see cref="climbOutHeight"/> over the water, with room to stand on it.</summary>
        private bool Bank(float level, out Vector3 top)
        {
            top = default;
            var forward = Flat(transform.forward);
            var from = transform.position + (forward * (_controller.radius + 0.7f));
            from.y = level + climbOutHeight + 0.3f;
            if (!Physics.Raycast(from, Vector3.down, out var hit, climbOutHeight + 0.6f, ~0, QueryTriggerInteraction.Ignore) ||
                hit.collider.transform.IsChildOf(transform) || hit.point.y < level + 0.2f || hit.point.y > level + climbOutHeight ||
                Vector3.Angle(hit.normal, Vector3.up) > _controller.slopeLimit)
            {
                return false;
            }

            var low = hit.point + (Vector3.up * (_controller.radius + 0.05f));
            var high = hit.point + (Vector3.up * (_controller.height - _controller.radius));
            if (Physics.CheckCapsule(low, high, _controller.radius * 0.9f, ~0, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            top = hit.point;
            return true;
        }

        /// <summary>The climb out: up the bank's face, then over its edge onto the top.</summary>
        private void Climb(float dt)
        {
            _climbTime += dt;
            var t = Mathf.Clamp01(_climbTime / climbOutSeconds);
            var up = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.7f));
            var over = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.4f) / 0.6f));
            transform.position = new Vector3(Mathf.Lerp(_climbFrom.x, _climbTo.x, over), Mathf.Lerp(_climbFrom.y, _climbTo.y, up), Mathf.Lerp(_climbFrom.z, _climbTo.z, over));
            if (t < 1f)
            {
                return;
            }

            _controller.enabled = true;
            _velocity = Vector3.zero;
            _verticalVelocity = -2f;
            _grounded = true;
            Enter(Move.Grounded);
        }

        /// <summary>
        /// Turns the body to where it goes (owner, 2026-11-12); with no key held it keeps the way it faced, as a body set
        /// down at a door.
        /// </summary>
        private void Face(Vector3 direction, float dt)
        {
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction, Vector3.up), turnSpeed * dt);
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
                case Move.Swimming:
                    Play(Has(SwimState) ? SwimState : LocomotionState, CrouchFade);
                    break;
                case Move.Climbing:
                    Play(Has(ClimbState) ? ClimbState : LocomotionState, AirFade);
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
            if (InWater)
            {
                // The stroke by the swimming speed, played faster for the fast swim.
                blend = Mathf.Clamp01(speed / swimSpeed);
                pace = speed > swimSpeed ? Mathf.Clamp(speed / swimSpeed, 1f, 1.8f) : 1f;
            }
            else if (State == Move.Crouched)
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
                // Straight ahead only: the body faces where it goes, so it walks face first even while it turns round
                // (owner, 2026-11-12). The blend's side and back clips stay in the controller, unplayed.
                _animator.SetFloat(MoveXParam, 0f, BlendDamp, dt);
                _animator.SetFloat(MoveZParam, blend, BlendDamp, dt);
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

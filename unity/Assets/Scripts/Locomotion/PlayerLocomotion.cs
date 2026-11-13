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
    /// 2026-11-11, a body facing the camera and stepping in eight directions. The one exception is the Aimed Walk
    /// (<see cref="AimFacing"/>): a firearm aimed, the body faces the aim and walks any way.
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
        [Tooltip("s the landing's bent knees hold at least, the body braking to a stop meanwhile (owner, 2026-11-12: half a second, then 1.5–2 times shorter, then half again); with no key pressed the landing plays landingShare of its clip. In the slice the tuning page sets it.")]
        [SerializeField] private float landingHold = 0.15f;
        [Tooltip("The share of the landing clip, after its first frames, played with no key pressed: 0.6, then halved (owner, 2026-11-12), the run's fade taking the rest. In the slice the tuning page sets it.")]
        [Range(0f, 1f)]
        [SerializeField] private float landingShare = 0.3f;
        [Tooltip("s in the air before a fall off an edge lands in the knees; a jump always does.")]
        [SerializeField] private float landingAfter = 0.45f;

        [Header("Swimming (10.1, AN5)")]
        [Tooltip("m of water over the bed from which the character swims: chest-deep on a person 1.8 m tall (authoring default).")]
        [SerializeField] private float wadeDepth = 1.3f;
        [Tooltip("m the root floats over the water in the stroke, below it when negative: the Swim clip holds the chest 0.12 m under the root and the head from 0.04 m under it to 0.08 m over (measured, SliceAnimationCheck), so at -0.08 the chest is 0.20 m under the water and the head's bone from 0.12 m under to level with it, the face just out (owner, 2026-11-12, twice: the body floated too high).")]
        [SerializeField] private float floatHeight = -0.08f;
        [Tooltip("m the root floats over the water treading still: Swim_Idle holds the head 0.25–0.35 m over the root (measured), so at -0.28 the head's bone, at the top of the neck, is from 0.03 m under the water to 0.07 m over: neck-deep (owner, 2026-11-12). The float goes from this to floatHeight with the stroke's speed.")]
        [SerializeField] private float treadHeight = -0.28f;
        [SerializeField] private float swimSpeed = 1.8f;
        [SerializeField] private float fastSwimSpeed = 3f;
        [Tooltip("m/s² toward the wanted velocity in the water.")]
        [SerializeField] private float swimAcceleration = 6f;
        [Tooltip("m over the water a bank's top may stand for Space to climb out onto it: the city's quays stand 1.9 m over the sea (measured: built ground 25.9 m, Sea Level 24 m).")]
        [SerializeField] private float climbOutHeight = 2.1f;
        [SerializeField] private float climbOutSeconds = 0.8f;

        /// <summary>
        /// Authoring defaults: the water's pull toward the float, 1/s², and its drag, 1/s, so a body that falls or wades
        /// in goes under first and comes up to its float in about a second with a small bob (owner, 2026-11-12: the change
        /// to swimming was abrupt, with no dip).
        /// </summary>
        private const float BuoyancySpring = 12f;

        private const float BuoyancyDrag = 5f;

        /// <summary>Authoring default, m: how far under the root the treading body's soles hang.</summary>
        private const float TreadSoles = 1.3f;

        /// <summary>Authoring default, degrees: a turn sharper than this brakes and turns round; a gentler one carries the run round with the body.</summary>
        private const float ReverseAngle = 135f;

        /// <summary>
        /// s into the landing clip where it starts: its first frames still hold the arms up as the fall did, so they are
        /// left out (measured, tools.txt: the hands over the shoulders until 0.10 s, the knees bending from 0.07 s).
        /// </summary>
        private const float LandStart = 0.12f;

        /// <summary>s the landing's knees hold at least before a key runs on (the tuning page's <c>Jump</c>).</summary>
        public float LandingHold
        {
            get => landingHold;
            set => landingHold = Mathf.Max(0f, value);
        }

        /// <summary>The share of the landing clip played with no key pressed, from 0 to 1.</summary>
        public float LandingShare
        {
            get => landingShare;
            set => landingShare = Mathf.Clamp01(value);
        }

        /// <summary>s in the air before a fall off an edge lands in the knees.</summary>
        public float LandingAfter
        {
            get => landingAfter;
            set => landingAfter = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Authoring default, degrees: the least an upper arm or a forearm may hang from the body's down in a jump, so an
        /// arm held close at take-off is never pressed into the body.
        /// </summary>
        private const float LeastArmReach = 10f;

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
        private static readonly int JumpRiseState = Animator.StringToHash("JumpRise");
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
        private bool _jumped;
        private bool _jumpQueued;

        // Each arm's upper arm and forearm in pairs of a bone and the next, and how far each may hang from the body's down.
        private Transform[] _arms;
        private readonly float[] _armReach = new float[4];
        private bool _armsHeld;

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

        /// <summary>
        /// The Aimed Walk (owner, 2026-11-12, VS-ALARM (h); 10.2 §5e): while set, the body faces this way instead of where
        /// it goes and walks any way the keys choose, sideways and backward too, never running or sprinting, the blend
        /// playing its side and back walks. Null for the body that faces where it goes.
        /// </summary>
        public Vector3? AimFacing { get; set; }

        /// <summary>The walking speed, m/s: the blend's 1.</summary>
        public float WalkSpeed => walkSpeed;

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
            _jumped = false;
            _jumpQueued = false;
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
            Bind();
            GameInputService.EnsureExists();
        }

        /// <summary>
        /// Reads the body's Animator again once the body under the root has been changed (character creation, S5b.1):
        /// its parameters, its arms, and the state it plays from.
        /// </summary>
        public void Rebind()
        {
            Bind();
            if (_animator != null && isActiveAndEnabled)
            {
                _playing = 0;
                Enter(State == Move.Airborne ? Move.Airborne : CrouchShare > 0f ? Move.Crouched : Move.Grounded, true);
            }
        }

        private void Bind()
        {
            // An old body switched off on its way out is not found.
            _animator = GetComponentInChildren<Animator>();
            _arms = null;
            if (_animator != null)
            {
                _animator.applyRootMotion = false;
                _hasPace = HasParameter(_animator, PaceParam);
                _hasMove = HasParameter(_animator, MoveXParam) && HasParameter(_animator, MoveZParam);
                _sneakPace = HasParameter(_animator, SneakPaceParam) ? _animator.GetFloat(SneakPaceParam) : 0f;
                if (_animator.isHuman)
                {
                    Transform Bone(HumanBodyBones b) => _animator.GetBoneTransform(b);
                    _arms = new[]
                    {
                        Bone(HumanBodyBones.LeftUpperArm), Bone(HumanBodyBones.LeftLowerArm),
                        Bone(HumanBodyBones.LeftLowerArm), Bone(HumanBodyBones.LeftHand),
                        Bone(HumanBodyBones.RightUpperArm), Bone(HumanBodyBones.RightLowerArm),
                        Bone(HumanBodyBones.RightLowerArm), Bone(HumanBodyBones.RightHand),
                    };
                }
            }
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
                    _jumped = false;
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

            var aiming = AimFacing.HasValue;
            IsSprinting = hasInput && !crouching && !aiming && SprintAllowed && input.SprintHeld;

            // The wanted speed: a crouch at its share of the walk, else a run, a sprint with Shift; a light stick walks.
            // Aimed, the body walks and never runs (10.2 §5e).
            var wanted = !hasInput ? 0f
                : crouching ? walkSpeed * CrouchShare
                : aiming ? walkSpeed
                : IsSprinting ? sprintSpeed
                : Mathf.Lerp(walkSpeed, runSpeed, Mathf.InverseLerp(0.5f, 0.9f, moveInput.magnitude));

            // The jump: a press kept for a moment before landing, honoured a moment after leaving an edge. A press while
            // the landing's knees are bent waits for them (owner, 2026-11-12).
            var landing = State == Move.Landing && _stateTime < landingHold;
            _jumpQueued |= landing && input.JumpPressed;
            if ((_sinceJumpPressed <= jumpBuffer || _jumpQueued) && !landing && (_grounded || _sinceGround <= coyoteTime) && State != Move.Airborne)
            {
                _sinceJumpPressed = float.MaxValue;
                _jumpQueued = false;
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _grounded = false;
                _sinceGround = coyoteTime + 1f;
                _jumped = true;
                HoldArms();
                Enter(Move.Airborne);
            }

            // The body turns first, and on the ground the run goes the way it faces, so a turn carries the run round
            // with it and never slides it on the old way (owner, 2026-11-12). A turn sharper than ReverseAngle, and a
            // stop, brake on the old way as before; the landing brakes to a stop.
            // Aimed, the body faces the aim and the velocity goes where the keys point, sideways and backward too.
            Face(aiming ? Flat(AimFacing.Value) : direction, dt);
            var target = State == Move.Landing ? Vector3.zero : direction * wanted;
            var airborne = State == Move.Airborne;
            var speedNow = PlanarSpeed;
            if (!aiming && !airborne && State != Move.Landing && hasInput && (speedNow < 0.1f || Vector3.Angle(_velocity, direction) < ReverseAngle))
            {
                var along = Mathf.MoveTowards(speedNow, wanted, (wanted < speedNow ? groundDeceleration : groundAcceleration) * dt);
                _velocity = Flat(transform.forward) * along;
            }
            else
            {
                var rate = airborne ? airAcceleration
                    : Vector3.Dot(target, _velocity) < 0f || target.sqrMagnitude < _velocity.sqrMagnitude ? groundDeceleration
                    : groundAcceleration;
                _velocity = Vector3.MoveTowards(_velocity, target, rate * dt);
            }

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

            Step(hasInput, crouching, dt);
            if (water && Deep(level))
            {
                // The fall carried in, so the body goes under before it comes up (owner, 2026-11-12).
                _verticalVelocity = Mathf.Max(_verticalVelocity, -4f);
                IsSprinting = false;
                _jumpQueued = false;
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
            var bed = Bed(level);
            if (bed > level - wadeDepth + 0.15f)
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

            // The water pulls the root to its float and drags on it, so a body that fell or waded in goes under, comes
            // up and bobs once, never springing straight to the surface. Treading still it floats neck-deep, in the
            // stroke with the face just out; over a shallow bed the treading feet stand on it, their soles about
            // TreadSoles under the root (authoring default, read from the float's head 0.25–0.35 m over the root on a
            // body 1.8 m tall).
            var rest = Mathf.Lerp(treadHeight, floatHeight, Mathf.Clamp01(PlanarSpeed / swimSpeed));
            rest = Mathf.Max(rest, Mathf.Min(floatHeight, bed - level + TreadSoles));
            _verticalVelocity += (((level + rest - transform.position.y) * BuoyancySpring) - (_verticalVelocity * BuoyancyDrag)) * dt;
            _verticalVelocity = Mathf.Clamp(_verticalVelocity, -4f, 4f);
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
                        _jumped = false;
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
                        // A jump, and a long fall, land in the knees (owner, 2026-11-12); a step down walks on.
                        Enter((_jumped || _airTime >= landingAfter) && !crouching ? Move.Landing : crouching ? Move.Crouched : Move.Grounded);
                    }

                    break;

                case Move.Landing:
                    // The knees bend for landingHold at least while the body brakes; then a key, or a jump pressed
                    // meanwhile, runs on at once, and with none the landing plays to its end.
                    if (!_grounded && _sinceGround > coyoteTime)
                    {
                        _jumped = false;
                        Enter(Move.Airborne);
                    }
                    else if (crouching || (_stateTime >= landingHold && (hasInput || _jumpQueued)) || _stateTime >= LandingLength())
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
                    // A jump plays its take-off and holds its last pose to the ground, the arms never rising (owner,
                    // 2026-11-12); a fall off an edge plays the fall.
                    Play(_jumped && Has(JumpRiseState) ? JumpRiseState : Has(FallState) ? FallState : LocomotionState, AirFade);
                    break;
                case Move.Landing:
                    Play(Has(LandState) ? LandState : LocomotionState, LandFade, LandStart);
                    break;
                case Move.Swimming:
                    Play(Has(SwimState) ? SwimState : LocomotionState, CrouchFade);
                    break;
                case Move.Climbing:
                    Play(Has(ClimbState) ? ClimbState : LocomotionState, AirFade);
                    break;
            }
        }

        /// <summary>The landing clip's whole length, s, as the Animator plays it; landingHold where it has none.</summary>
        private float LandingLength()
        {
            if (_animator == null || !_animator.isActiveAndEnabled || _animator.IsInTransition(0))
            {
                return Mathf.Max(landingHold, 0.3f);
            }

            var info = _animator.GetCurrentAnimatorStateInfo(0);
            return info.shortNameHash == LandState && info.length > 0.01f ? Mathf.Max(landingHold, (info.length - LandStart) * landingShare) : landingHold;
        }

        /// <summary>
        /// The arms in a jump (owner, 2026-11-12): the take-off clip's last pose spread them, so through the air and the
        /// landing neither upper arm nor forearm hangs farther from the body's down than it did at take-off, nor closer
        /// than <see cref="LeastArmReach"/>. The reach is read from the pose the body left the ground in.
        /// </summary>
        private void HoldArms()
        {
            _armsHeld = _arms != null;
            for (var i = 0; _armsHeld && i < _arms.Length; i += 2)
            {
                _armReach[i / 2] = _arms[i] != null && _arms[i + 1] != null
                    ? Mathf.Max(LeastArmReach, Vector3.Angle(-transform.up, _arms[i + 1].position - _arms[i].position))
                    : 180f;
            }
        }

        private void LateUpdate()
        {
            if (!_armsHeld)
            {
                return;
            }

            if (!(State == Move.Airborne && _jumped) && State != Move.Landing)
            {
                _armsHeld = false;
                return;
            }

            var down = -transform.up;
            for (var i = 0; i < _arms.Length; i += 2)
            {
                var from = _arms[i];
                var to = _arms[i + 1];
                var reach = _armReach[i / 2];
                if (from == null || to == null)
                {
                    continue;
                }

                var along = to.position - from.position;
                var angle = Vector3.Angle(down, along);
                if (angle > reach + 0.5f && angle < 179f)
                {
                    var wanted = Vector3.Slerp(down, along.normalized, reach / angle);
                    from.rotation = Quaternion.FromToRotation(along, wanted) * from.rotation;
                }
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
            if (_hasMove && AimFacing.HasValue && !InWater && State != Move.Crouched)
            {
                // The Aimed Walk (10.2 §5e): the velocity in the body's own frame, the walk at 1, so the blend plays
                // its side and back walks.
                var local = transform.InverseTransformDirection(new Vector3(_velocity.x, 0f, _velocity.z)) / Mathf.Max(0.01f, walkSpeed);
                _animator.SetFloat(MoveXParam, local.x, BlendDamp, dt);
                _animator.SetFloat(MoveZParam, local.z, BlendDamp, dt);
            }
            else if (_hasMove)
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

        private void Play(int state, float fade, float start = 0f)
        {
            if (_animator == null || !_animator.isActiveAndEnabled || _playing == state || !_animator.HasState(0, state))
            {
                return;
            }

            _animator.CrossFadeInFixedTime(state, fade, 0, start);
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

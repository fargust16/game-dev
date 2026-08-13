using GameDev.Input;
using UnityEngine;

namespace GameDev.Player
{
    /// <summary>
    /// Free-roam CharacterController locomotion for the Overworld (WASD move, camera-relative,
    /// Shift to sprint, Space to jump). Drives an Animator (Speed float 0-3 = Idle/Walk/Run/Sprint,
    /// Grounded bool, Jump trigger) built by GameDev.EditorTools.OverworldPlayerMenu - see that
    /// file for the blend tree/jump state machine this expects.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public sealed class OverworldPlayerController : MonoBehaviour
    {
        [Header("Speeds (m/s) - match Animator Speed blend positions 0/1/2/3")]
        [SerializeField] private float walkSpeed = 2f;
        [SerializeField] private float runSpeed = 4.5f;
        [SerializeField] private float sprintSpeed = 7.5f;
        [SerializeField] private float speedDamping = 5f;

        [Header("Turning")]
        [SerializeField] private float rotateSpeed = 10f;

        [Header("Jump / Gravity")]
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -18f;
        [SerializeField] private float groundedStickVelocity = -2f;

        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int GroundedParam = Animator.StringToHash("Grounded");
        private static readonly int JumpParam = Animator.StringToHash("Jump");

        private CharacterController _controller;
        private Animator _animator;
        private float _animSpeed;
        private float _verticalVelocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _animator = GetComponent<Animator>();
            _animator.applyRootMotion = false;
            GameInputService.EnsureExists();
        }

        private void Update()
        {
            var input = GameInputService.Instance;
            if (input == null)
            {
                return;
            }

            var grounded = _controller.isGrounded;
            if (grounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = groundedStickVelocity;
            }

            if (grounded && input.JumpPressed)
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _animator.SetTrigger(JumpParam);
            }

            _verticalVelocity += gravity * Time.deltaTime;

            var moveInput = input.MoveVector;
            var hasInput = moveInput.sqrMagnitude > 0.01f;
            var sprinting = hasInput && input.SprintHeld;
            var targetAnimSpeed = hasInput ? (sprinting ? 3f : 2f) : 0f;
            _animSpeed = Mathf.MoveTowards(_animSpeed, targetAnimSpeed, speedDamping * Time.deltaTime);

            var moveDir = CameraRelativeDirection(new Vector3(moveInput.x, 0f, moveInput.y));
            var planarSpeed = SpeedForAnim(_animSpeed);

            var motion = moveDir * planarSpeed + Vector3.up * _verticalVelocity;
            _controller.Move(motion * Time.deltaTime);

            if (moveDir.sqrMagnitude > 0.0001f)
            {
                var targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
            }

            _animator.SetFloat(SpeedParam, _animSpeed);
            _animator.SetBool(GroundedParam, grounded);
        }

        /// <summary>Maps the 0..3 Idle/Walk/Run/Sprint blend value to an actual world-space speed.</summary>
        private float SpeedForAnim(float animSpeed)
        {
            if (animSpeed <= 1f)
            {
                return Mathf.Lerp(0f, walkSpeed, animSpeed);
            }

            if (animSpeed <= 2f)
            {
                return Mathf.Lerp(walkSpeed, runSpeed, animSpeed - 1f);
            }

            return Mathf.Lerp(runSpeed, sprintSpeed, animSpeed - 2f);
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

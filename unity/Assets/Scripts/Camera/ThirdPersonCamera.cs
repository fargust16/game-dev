using GameDev.Input;
using UnityEngine;

// The namespace is Cameras, not Camera: a GameDev.Camera namespace would hide UnityEngine.Camera in GameDev code.
namespace GameDev.Cameras
{
    /// <summary>
    /// Third-person orbit camera (17 L.3): the locked mouse orbits behind the character at a close, mostly level
    /// distance, and a spring arm pulls it in front of walls. Moved from GameDev.Player.OverworldThirdPersonCamera in
    /// S0.3 and freed from the old WorldClock and DecisionNodeSession; whoever opens a menu sets <see cref="LookEnabled"/>.
    /// </summary>
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);

        [Header("Orbit")]
        [SerializeField] private float distance = 4.5f;
        [SerializeField] private float minDistance = 1.2f;
        [SerializeField] private float maxDistance = 8f;
        [SerializeField] private float pitch = 18f;
        [SerializeField] private float minPitch = -25f;
        [SerializeField] private float maxPitch = 65f;
        [SerializeField] private float mouseSensitivity = 2.5f;
        [SerializeField] private float zoomSpeed = 4f;
        [SerializeField] private float positionSmoothTime = 0.06f;

        // Common third-person practice (fast in, slow out): the arm shortens almost at once so a wall never stands
        // between the camera and the character, and lengthens slowly so passing a corner or a door frame does not jerk
        // the view. Authoring defaults, 2026-11-05.
        [SerializeField] private float pullInTime = 0.05f;
        [SerializeField] private float returnTime = 0.45f;
        [SerializeField] private float returnDelay = 0.15f;

        [Header("Collision")]
        [SerializeField] private float collisionRadius = 0.25f;
        [SerializeField] private LayerMask collisionMask = ~0;

        private float _yaw;
        private Vector3 _velocity;
        private float _armLength;
        private float _armVelocity;
        private float _clearSince;
        private Vector3 _smoothedPivot;

        // 10.2 §5e: aimed, the look reaches further up and down than the orbit's, degrees.
        private const float AimMinPitch = -60f;
        private const float AimMaxPitch = 60f;
        private const float AimNearClip = 0.03f;

        private Camera _camera;
        private float _orbitFieldOfView = -1f;
        private float _orbitNearClip = -1f;

        /// <summary>False while a menu or dialogue owns the mouse: the camera keeps following but stops orbiting.</summary>
        public bool LookEnabled { get; set; } = true;

        /// <summary>The aimed view the camera blends into by its weight (10.2 §5e); null for the orbit alone.</summary>
        public IAimView Aim { get; set; }

        /// <summary>The look the mouse gives, degrees: the yaw round the up axis and the pitch down from level.</summary>
        public float Yaw => _yaw;

        public float Pitch => pitch;

        /// <summary>Points the look as the mouse would, degrees: the yaw round the up axis and the pitch down from level.</summary>
        public void Look(float yaw, float down)
        {
            _yaw = yaw;
            pitch = Mathf.Clamp(down, Mathf.Min(minPitch, AimMinPitch), Mathf.Max(maxPitch, AimMaxPitch));
        }

        public Transform Target => target;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                _yaw = target.eulerAngles.y;
            }
        }

        public void SetCollisionMask(LayerMask mask)
        {
            collisionMask = mask;
        }

        /// <summary>
        /// A new scene, or a kept city switched on again behind an interior's door (17 I.2), starts behind its player,
        /// who may have just been placed there by a door: the camera snaps on its first LateUpdate after it is enabled,
        /// which comes after the door's arrival in the same frame.
        /// </summary>
        private void OnEnable()
        {
            SetCursorLocked(true);
            _snapPending = true;
        }

        private bool _snapPending;

        private void SnapBehindTarget()
        {
            _yaw = target.eulerAngles.y;
            var pivot = target.position + targetOffset;
            _smoothedPivot = pivot;
            var offset = Quaternion.Euler(pitch, _yaw, 0f) * (Vector3.back * distance);
            _armLength = ResolveCollisionDistance(pivot, offset, distance);
            transform.position = pivot + offset.normalized * _armLength;
            _velocity = Vector3.zero;
            _armVelocity = 0f;
        }

        private void OnDisable()
        {
            SetCursorLocked(false);
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (_snapPending)
            {
                _snapPending = false;
                SnapBehindTarget();
            }

            var aimed = Aim != null ? Mathf.Clamp01(Aim.Weight) : 0f;
            var input = GameInputService.Instance;
            if (input != null && LookEnabled)
            {
                if (input.CancelPressed)
                {
                    SetCursorLocked(false);
                }

                // The Game View honours Cursor.lockState only once it has focus, so the lock asked for in
                // OnEnable is dropped until the player clicks; ask again on the first left click.
                if (Cursor.lockState != CursorLockMode.Locked && input.ClickMovePressed)
                {
                    SetCursorLocked(true);
                }

                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    var delta = input.MouseDelta * (aimed > 0f ? Mathf.Lerp(1f, Aim.MouseShare, aimed) : 1f);
                    _yaw += delta.x * mouseSensitivity * 0.02f;
                    pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity * 0.02f, Mathf.Lerp(minPitch, AimMinPitch, aimed), Mathf.Lerp(maxPitch, AimMaxPitch, aimed));
                }

                var scroll = input.CameraZoomDelta;
                if (Mathf.Abs(scroll) > 0.01f && aimed <= 0f)
                {
                    distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
                }
            }

            var pivot = target.position + targetOffset;
            var rotation = Quaternion.Euler(pitch, _yaw, 0f);
            var desiredOffset = rotation * (Vector3.back * distance);

            var allowed = ResolveCollisionDistance(pivot, desiredOffset, distance);
            if (allowed < _armLength - 0.01f)
            {
                _armLength = Mathf.SmoothDamp(_armLength, allowed, ref _armVelocity, pullInTime);
                _armLength = Mathf.Min(_armLength, allowed + 0.1f);
                _clearSince = Time.time;
            }
            else if (Time.time - _clearSince >= returnDelay)
            {
                _armLength = Mathf.SmoothDamp(_armLength, allowed, ref _armVelocity, returnTime);
            }

            // The pivot follows the character smoothly; the arm's length is smoothed apart from it, so a wall never
            // drags the whole view sideways.
            _smoothedPivot = Vector3.SmoothDamp(_smoothedPivot, pivot, ref _velocity, positionSmoothTime);
            var orbit = _smoothedPivot + desiredOffset.normalized * _armLength;
            transform.position = aimed > 0f ? Vector3.Lerp(orbit, Aim.Eye, Smooth(aimed)) : orbit;
            transform.rotation = rotation;
            Narrow(aimed);
            if (aimed > 0f)
            {
                Aim.Placed(_camera);
            }
        }

        /// <summary>The field of view and the near clip between the orbit's and the aimed view's (10.2 §5e).</summary>
        private void Narrow(float aimed)
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
                if (_camera == null)
                {
                    return;
                }
            }

            if (aimed <= 0f)
            {
                // Back in the orbit: its own view again, which another owner may change while nothing is aimed.
                if (_orbitFieldOfView >= 0f)
                {
                    _camera.fieldOfView = _orbitFieldOfView;
                    _camera.nearClipPlane = _orbitNearClip;
                    _orbitFieldOfView = -1f;
                }

                return;
            }

            if (_orbitFieldOfView < 0f)
            {
                _orbitFieldOfView = _camera.fieldOfView;
                _orbitNearClip = _camera.nearClipPlane;
            }

            var share = Smooth(aimed);
            _camera.fieldOfView = Mathf.Lerp(_orbitFieldOfView, Aim.FieldOfView, share);
            _camera.nearClipPlane = Mathf.Lerp(_orbitNearClip, AimNearClip, share);
        }

        private static float Smooth(float t) => t * t * (3f - (2f * t));

        /// <summary>Spring arm: pulls the camera in along the same ray when something sits between the pivot and the
        /// desired position. Hits on the target's own colliders are skipped, because the chest-height pivot sits inside
        /// the player's capsule and would otherwise collapse the camera to its minimum distance.</summary>
        private float ResolveCollisionDistance(Vector3 pivot, Vector3 desiredOffset, float desiredDistance)
        {
            if (desiredDistance <= 0.0001f)
            {
                return desiredDistance;
            }

            var direction = desiredOffset / desiredDistance;
            var hits = Physics.SphereCastAll(pivot, collisionRadius, direction, desiredDistance, collisionMask, QueryTriggerInteraction.Ignore);
            var closest = desiredDistance;
            foreach (var hit in hits)
            {
                if (hit.collider.transform == target || hit.collider.transform.IsChildOf(target))
                {
                    continue;
                }

                if (hit.distance < closest)
                {
                    closest = hit.distance;
                }
            }

            return Mathf.Max(closest, minDistance * 0.4f);
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}

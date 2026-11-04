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

        [Header("Collision")]
        [SerializeField] private float collisionRadius = 0.25f;
        [SerializeField] private LayerMask collisionMask = ~0;

        private float _yaw;
        private Vector3 _velocity;

        /// <summary>False while a menu or dialogue owns the mouse: the camera keeps following but stops orbiting.</summary>
        public bool LookEnabled { get; set; } = true;

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

        private void OnEnable()
        {
            SetCursorLocked(true);
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
                    var delta = input.MouseDelta;
                    _yaw += delta.x * mouseSensitivity * 0.02f;
                    pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity * 0.02f, minPitch, maxPitch);
                }

                var scroll = input.CameraZoomDelta;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
                }
            }

            var pivot = target.position + targetOffset;
            var rotation = Quaternion.Euler(pitch, _yaw, 0f);
            var desiredOffset = rotation * (Vector3.back * distance);

            var clampedDistance = ResolveCollisionDistance(pivot, desiredOffset, distance);
            var finalPosition = pivot + desiredOffset.normalized * clampedDistance;

            transform.position = Vector3.SmoothDamp(transform.position, finalPosition, ref _velocity, positionSmoothTime);
            transform.rotation = rotation;
        }

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

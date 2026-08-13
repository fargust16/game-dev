using GameDev.Decisions;
using GameDev.Input;
using GameDev.World;
using UnityEngine;

namespace GameDev.Player
{
    /// <summary>
    /// Genshin Impact-style third-person orbit camera for the free-roam Overworld player: mouse
    /// (cursor locked) orbits behind/around the character at a close, mostly-level distance instead
    /// of GameDev.Player.SimpleFollowCamera's BG3-tactical top-down angle - that camera stays the
    /// default for the Town/Grid lab (GridPlayerController), this one is Overworld-only, wired up by
    /// GameDev.EditorTools.OverworldPlayerMenu.
    /// </summary>
    public sealed class OverworldThirdPersonCamera : MonoBehaviour
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

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                _yaw = target.eulerAngles.y;
            }
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

            var paused = (WorldClock.Instance != null && WorldClock.Instance.IsPaused) || DecisionNodeSession.IsOpen;
            var input = GameInputService.Instance;

            if (input != null && !paused)
            {
                if (input.CancelPressed)
                {
                    SetCursorLocked(false);
                }

                // The Editor Game View (like most OS/browser windows) only honours
                // Cursor.lockState once the window actually has input focus, which a script can't
                // force before the player interacts - the OnEnable lock request above is silently
                // dropped until then, so without this the camera never gets a single mouse delta
                // and looks completely static. Re-request the lock on the first click instead of
                // assuming it already took; ClickMovePressed (left mouse) only drives GridMap
                // path-clicking elsewhere, so it's free to reuse here.
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

        /// <summary>Spring-arm style: pulls the camera in along the same ray if something (terrain,
        /// a building) sits between the pivot and the desired position, so it never clips through
        /// geometry. Hits on the target's own collider (CharacterController) are skipped - casting
        /// from a chest-height pivot that sits inside the player's own capsule would otherwise
        /// register a same-point hit and permanently collapse the camera to minDistance.</summary>
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

using UnityEngine;

namespace GameDev.Player
{
    /// <summary>
    /// BG3-style free tactical camera: pan / zoom / rotate around a focus point.
    /// Does not glue to the avatar; F recenters on the controlled character.
    /// Classic Fallout / modern CRPGs use the same pattern (map camera ≠ character body).
    /// </summary>
    public sealed class SimpleFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform focusTarget;
        [SerializeField] private Vector3 focusPoint;
        [SerializeField] private float yaw = 30f;
        [SerializeField] private float pitch = 52f;
        [SerializeField] private float distance = 18f;
        [SerializeField] private float lookHeight = 0.75f;

        [Header("Speeds")]
        [SerializeField] private float panSpeed = 14f;
        [SerializeField] private float fastPanMultiplier = 2.2f;
        [SerializeField] private float mouseRotateSensitivity = 0.18f;
        [SerializeField] private float zoomSpeed = 6f;
        [SerializeField] private float minDistance = 7f;
        [SerializeField] private float maxDistance = 36f;
        [SerializeField] private float minPitch = 28f;
        [SerializeField] private float maxPitch = 72f;
        [SerializeField] private float edgePanSize = 12f;
        [SerializeField] private bool enableEdgePan = true;

        private bool _draggingPan;
        private bool _draggingRotate;
        private Vector3 _lastMouse;

        /// <summary>Compat with older builder call — sets recenter target and snaps focus.</summary>
        public void SetTarget(Transform newTarget)
        {
            focusTarget = newTarget;
            if (focusTarget != null)
            {
                focusPoint = focusTarget.position;
            }

            ApplyCameraTransform();
        }

        public void SetFocusPoint(Vector3 point)
        {
            focusPoint = point;
            focusPoint.y = 0f;
        }

        private void LateUpdate()
        {
            HandleCameraInput();
            ApplyCameraTransform();
        }

        private void HandleCameraInput()
        {
            var dt = Time.deltaTime;
            var fast = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var speed = panSpeed * (fast ? fastPanMultiplier : 1f);

            // WASD pan (BG3) — character uses arrows / click instead.
            var pan = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) pan += FlatForward();
            if (Input.GetKey(KeyCode.S)) pan -= FlatForward();
            if (Input.GetKey(KeyCode.D)) pan += FlatRight();
            if (Input.GetKey(KeyCode.A)) pan -= FlatRight();

            if (enableEdgePan && Application.isFocused)
            {
                var m = Input.mousePosition;
                if (m.x <= edgePanSize) pan -= FlatRight();
                if (m.x >= Screen.width - edgePanSize) pan += FlatRight();
                if (m.y <= edgePanSize) pan -= FlatForward();
                if (m.y >= Screen.height - edgePanSize) pan += FlatForward();
            }

            if (pan.sqrMagnitude > 0.0001f)
            {
                focusPoint += pan.normalized * (speed * dt);
            }

            // Middle-mouse drag pans.
            if (Input.GetMouseButtonDown(2))
            {
                _draggingPan = true;
                _lastMouse = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(2))
            {
                _draggingPan = false;
            }

            if (_draggingPan && Input.GetMouseButton(2))
            {
                var delta = Input.mousePosition - _lastMouse;
                _lastMouse = Input.mousePosition;
                focusPoint -= FlatRight() * (delta.x * 0.04f);
                focusPoint -= FlatForward() * (delta.y * 0.04f);
            }

            // Rotate with RMB drag only — E is reserved for interact.
            if (Input.GetMouseButtonDown(1))
            {
                _draggingRotate = true;
                _lastMouse = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(1))
            {
                _draggingRotate = false;
            }

            if (_draggingRotate && Input.GetMouseButton(1))
            {
                var delta = Input.mousePosition - _lastMouse;
                _lastMouse = Input.mousePosition;
                yaw += delta.x * mouseRotateSensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * mouseRotateSensitivity, minPitch, maxPitch);
            }

            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
            }

            // Recenter on controlled character (BG3-like focus).
            if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Home))
            {
                if (focusTarget != null)
                {
                    focusPoint = focusTarget.position;
                    focusPoint.y = 0f;
                }
            }
        }

        private void ApplyCameraTransform()
        {
            var look = focusPoint + Vector3.up * lookHeight;
            var orbit = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = look + orbit * (Vector3.back * distance);
            transform.rotation = Quaternion.LookRotation((look - transform.position).normalized, Vector3.up);
        }

        private Vector3 FlatForward()
        {
            var f = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            f.y = 0f;
            return f.normalized;
        }

        private Vector3 FlatRight()
        {
            var r = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            r.y = 0f;
            return r.normalized;
        }
    }
}

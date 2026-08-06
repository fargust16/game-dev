using UnityEngine;

namespace GameDev.Player
{
    public enum CameraRigMode
    {
        Free = 0,
        Follow = 1
    }

    /// <summary>
    /// Free: BG3 tactical pan/zoom/orbit. Follow: smooth track of avatar (WASD control scheme).
    /// </summary>
    public sealed class SimpleFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform focusTarget;
        [SerializeField] private Vector3 focusPoint;
        [SerializeField] private float yaw = 30f;
        [SerializeField] private float pitch = 52f;
        [SerializeField] private float distance = 18f;
        [SerializeField] private float lookHeight = 0.75f;
        [SerializeField] private CameraRigMode rigMode = CameraRigMode.Free;

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
        [SerializeField] private float followSmoothTime = 0.18f;

        private bool _draggingPan;
        private bool _draggingRotate;
        private Vector3 _lastMouse;
        private Vector3 _followVelocity;

        public CameraRigMode RigMode => rigMode;

        public void SetTarget(Transform newTarget)
        {
            focusTarget = newTarget;
            if (focusTarget != null)
            {
                focusPoint = focusTarget.position;
                focusPoint.y = 0f;
            }

            ApplyCameraTransform();
        }

        public void SetRigMode(CameraRigMode mode)
        {
            rigMode = mode;
            _followVelocity = Vector3.zero;
            if (rigMode == CameraRigMode.Follow && focusTarget != null)
            {
                focusPoint = focusTarget.position;
                focusPoint.y = 0f;
            }
        }

        private void LateUpdate()
        {
            if (rigMode == CameraRigMode.Follow)
            {
                UpdateFollowFocus();
                HandleSharedLookInput(allowWasdPan: false, allowEdgePan: false);
            }
            else
            {
                HandleSharedLookInput(allowWasdPan: true, allowEdgePan: enableEdgePan);
            }

            ApplyCameraTransform();
        }

        private void UpdateFollowFocus()
        {
            if (focusTarget == null)
            {
                return;
            }

            var desired = focusTarget.position;
            desired.y = 0f;
            focusPoint = Vector3.SmoothDamp(focusPoint, desired, ref _followVelocity, followSmoothTime);
        }

        private void HandleSharedLookInput(bool allowWasdPan, bool allowEdgePan)
        {
            var dt = Time.deltaTime;
            var fast = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var speed = panSpeed * (fast ? fastPanMultiplier : 1f);

            var pan = Vector3.zero;
            if (allowWasdPan)
            {
                if (Input.GetKey(KeyCode.W)) pan += FlatForward();
                if (Input.GetKey(KeyCode.S)) pan -= FlatForward();
                if (Input.GetKey(KeyCode.D)) pan += FlatRight();
                if (Input.GetKey(KeyCode.A)) pan -= FlatRight();
            }

            if (allowEdgePan && Application.isFocused)
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

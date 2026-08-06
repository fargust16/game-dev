using UnityEngine;

namespace GameDev.Player
{
    /// <summary>
    /// Stable tactical follow camera. Uses SmoothDamp and a fixed look point
    /// so lateral movement does not jitter the view.
    /// </summary>
    public sealed class SimpleFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 14f, -10f);
        [SerializeField] private float positionSmoothTime = 0.12f;
        [SerializeField] private float lookHeight = 1f;

        private Vector3 _positionVelocity;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                transform.position = target.position + offset;
                LookAtTargetImmediate();
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var desired = target.position + offset;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desired,
                ref _positionVelocity,
                positionSmoothTime);

            var lookPoint = target.position + Vector3.up * lookHeight;
            var toLook = lookPoint - transform.position;
            if (toLook.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(toLook.normalized, Vector3.up);
            }
        }

        private void LookAtTargetImmediate()
        {
            var lookPoint = target.position + Vector3.up * lookHeight;
            transform.rotation = Quaternion.LookRotation((lookPoint - transform.position).normalized, Vector3.up);
        }
    }
}

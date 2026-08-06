using UnityEngine;

namespace GameDev.World
{
    /// <summary>
    /// Keeps a world label facing the main camera (S0 route readability).
    /// </summary>
    public sealed class BillboardLabel : MonoBehaviour
    {
        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, Vector3.up);
        }
    }
}

using UnityEngine;

namespace GameDev.Slice.Kit
{
    /// <summary>
    /// The player pushes a loose prop it walks into (kit standard §4): a level push of <see cref="Force"/> at the point
    /// of contact, so a chair slides away and a barrel barely moves, and none faster than <see cref="MaxSpeed"/>. A
    /// CharacterController moves no body by itself.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PushLooseProps : MonoBehaviour
    {
        public const float Force = 600f;   // N, authoring default (2026-11-06)
        public const float MaxSpeed = 1.5f; // m/s a push gives a prop at most, authoring default (2026-11-06)

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var body = hit.rigidbody;
            if (body == null || body.isKinematic || hit.moveDirection.y < -0.3f || body.GetComponent<LooseProp>() == null)
            {
                return;
            }

            var push = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
            if (push.sqrMagnitude < 1e-4f)
            {
                return;
            }

            // A push speeds a prop to MaxSpeed at most, so a chair is shoved, never shot away.
            var along = push.normalized;
            if (Vector3.Dot(body.linearVelocity, along) >= MaxSpeed)
            {
                return;
            }

            body.AddForceAtPosition(along * Force, hit.point, ForceMode.Force);
        }
    }
}

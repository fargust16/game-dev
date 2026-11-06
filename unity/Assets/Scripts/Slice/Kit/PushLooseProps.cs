using UnityEngine;

namespace GameDev.Slice.Kit
{
    /// <summary>
    /// The player pushes a loose prop it walks into (kit standard §4): a level push of a walker's strength,
    /// <see cref="Force"/>, at the point of contact, never giving a prop more speed than <see cref="MaxSpeed"/>. The push
    /// is an impulse of Force over the frame, cut to what brings the prop to MaxSpeed, so a bottle is nudged, never shot
    /// away; friction then decides: a chair slides, a heavy table or a full barrel stands (owner's fourth play test,
    /// 2026-11-06). A CharacterController moves no body by itself.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PushLooseProps : MonoBehaviour
    {
        public const float Force = 200f;    // N, a walker leaning on a thing; authoring default (2026-11-06)
        public const float MaxSpeed = 1.2f; // m/s a push gives a prop at most; authoring default (2026-11-06)

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

            var along = push.normalized;
            var room = MaxSpeed - Vector3.Dot(body.linearVelocity, along);
            if (room <= 0f)
            {
                return;
            }

            var impulse = Mathf.Min(Force * Time.deltaTime, body.mass * room);
            body.AddForceAtPosition(along * impulse, hit.point, ForceMode.Impulse);
        }
    }
}

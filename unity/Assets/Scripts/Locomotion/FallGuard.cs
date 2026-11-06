using UnityEngine;

namespace GameDev.Locomotion
{
    /// <summary>
    /// Returns a player who has fallen out of the scene (owner, 2026-11-06): below the lowest solid thing in the scene,
    /// by <see cref="Margin"/>, the player is put back where it last stood on the ground. A safety net, not a rule of
    /// the world: a hole in a scene is still a fault to fix where it is.
    /// </summary>
    [RequireComponent(typeof(PlayerLocomotion))]
    public sealed class FallGuard : MonoBehaviour
    {
        /// <summary>Authoring default, m: how far below the scene's lowest collider a fall counts as out of the scene.</summary>
        public const float Margin = 5f;

        private PlayerLocomotion _player;
        private Vector3 _lastGround;
        private Quaternion _lastRotation;
        private float _floor = float.NegativeInfinity;

        public float Floor => _floor;

        private void Start()
        {
            _player = GetComponent<PlayerLocomotion>();
            _lastGround = transform.position;
            _lastRotation = transform.rotation;
            Measure();
        }

        /// <summary>Reads the lowest solid collider of the loaded scenes, the player's own excepted.</summary>
        public void Measure()
        {
            var lowest = float.PositiveInfinity;
            foreach (var c in FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (c.isTrigger || !c.enabled || c.transform.IsChildOf(transform))
                {
                    continue;
                }

                lowest = Mathf.Min(lowest, c.bounds.min.y);
            }

            _floor = float.IsPositiveInfinity(lowest) ? float.NegativeInfinity : lowest - Margin;
        }

        private void LateUpdate()
        {
            if (_player.IsGrounded)
            {
                _lastGround = transform.position;
                _lastRotation = transform.rotation;
                return;
            }

            if (transform.position.y < _floor)
            {
                _player.Teleport(_lastGround + Vector3.up * 0.1f, _lastRotation);
            }
        }
    }
}

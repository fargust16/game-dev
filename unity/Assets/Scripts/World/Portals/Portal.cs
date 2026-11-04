using GameDev.Core.Scenes;
using GameDev.Input;
using GameDev.Locomotion;
using UnityEngine;

namespace GameDev.World.Portals
{
    /// <summary>
    /// A door that leads to another scene (17 I.1; kit standard §6). It sits on a door block whose 3 m wall runs along
    /// local +X with the opening over its middle cell. Within reach of the opening, Interact sends the player to the
    /// paired portal in the target scene; a portal named by an arriving request places the player in front of itself,
    /// on the side the player comes out, facing away from the door. Paired doors therefore return the player where
    /// they entered.
    /// </summary>
    public sealed class Portal : MonoBehaviour
    {
        /// <summary>Kit standard §3: the opening's middle, from the block's pivot.</summary>
        private static readonly Vector3 OpeningCentre = new Vector3(1.5f, 0f, 0f);

        [Tooltip("Stable id, unique in its scene, e.g. 'tavern.street'.")]
        [SerializeField] private string portalId;

        [SerializeField] private string targetScene;
        [SerializeField] private string targetPortalId;

        [Tooltip("The side of the wall, local +Z or -Z, where an arriving player stands.")]
        [SerializeField] private bool arriveOnPositiveZ;

        [Tooltip("Authoring default, m: horizontal reach from the opening's middle.")]
        [SerializeField] private float interactRadius = 1.5f;

        [Tooltip("Authoring default, m: how far from the wall an arriving player stands.")]
        [SerializeField] private float arrivalDistance = 1f;

        private PlayerLocomotion _player;

        public string Id => portalId;

        public TravelRequest Target => new TravelRequest(targetScene, targetPortalId);

        /// <summary>Where an arriving player stands, facing away from the door.</summary>
        public Pose ArrivalPose
        {
            get
            {
                var side = arriveOnPositiveZ ? 1f : -1f;
                var position = transform.TransformPoint(OpeningCentre + new Vector3(0f, 0f, side * arrivalDistance));
                var facing = transform.TransformDirection(new Vector3(0f, 0f, side));
                return new Pose(position, Quaternion.LookRotation(facing, Vector3.up));
            }
        }

        /// <summary>Editor setup for scenes built by code; a placed door is set in the Inspector.</summary>
        public void Configure(string id, string sceneName, string pairedPortalId, bool arrivalOnPositiveZ)
        {
            portalId = id;
            targetScene = sceneName;
            targetPortalId = pairedPortalId;
            arriveOnPositiveZ = arrivalOnPositiveZ;
        }

        /// <summary>True when the player stands within reach of the opening.</summary>
        public bool InReach(Vector3 position)
        {
            var offset = position - transform.TransformPoint(OpeningCentre);
            offset.y = 0f;
            return offset.sqrMagnitude <= interactRadius * interactRadius;
        }

        private void OnEnable()
        {
            SceneTransition.Arriving += OnArriving;
        }

        private void OnDisable()
        {
            SceneTransition.Arriving -= OnArriving;
        }

        private void Update()
        {
            var input = GameInputService.Instance;
            if (input == null || !input.InteractPressed || SceneTransition.IsTraveling)
            {
                return;
            }

            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerLocomotion>();
            }

            if (_player != null && InReach(_player.transform.position))
            {
                SceneTransition.Travel(Target);
            }
        }

        private void OnArriving(TravelRequest request)
        {
            if (request.ArrivalPortalId != portalId)
            {
                return;
            }

            var player = FindAnyObjectByType<PlayerLocomotion>();
            if (player == null)
            {
                Debug.LogError($"Portal '{portalId}' has no player to place.", this);
                return;
            }

            var pose = ArrivalPose;
            player.Teleport(pose.position, pose.rotation);
        }
    }
}

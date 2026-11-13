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

        [Tooltip("Passed to the target scene with the journey: a shared scene (the Dwelling) reads which place it is from it.")]
        [SerializeField] private string travelContext;

        [Tooltip("The side of the wall, local +Z or -Z, where an arriving player stands.")]
        [SerializeField] private bool arriveOnPositiveZ;

        [Tooltip("Authoring default, m: horizontal reach from the opening's middle.")]
        [SerializeField] private float interactRadius = 1.5f;

        [Tooltip("Authoring default, m: how far from the wall an arriving player stands.")]
        [SerializeField] private float arrivalDistance = 1f;

        private PlayerLocomotion _player;

        public string Id => portalId;

        /// <summary>
        /// A door sealed shut, such as a shop the Wardens sealed with wax and brass (11.1 POI A; S2.7): Interact no longer
        /// sends the player through, but a player already behind it still comes out here.
        /// </summary>
        public bool Sealed { get; set; }

        /// <summary>The prompt's key while sealed; null for the sealed door's own (S5c.4: a locked door says what opens it).</summary>
        public string SealedPrompt { get; set; }

        public TravelRequest Target => new TravelRequest(targetScene, targetPortalId, travelContext);

        /// <summary>
        /// Where an arriving player stands, facing away from the door: on the ground under that point, so a door at
        /// the top of steps does not drop the player from its sill (C1's Wardens' station, 0.44 m).
        /// </summary>
        public Pose ArrivalPose
        {
            get
            {
                var side = arriveOnPositiveZ ? 1f : -1f;
                var position = transform.TransformPoint(OpeningCentre + new Vector3(0f, 0f, side * arrivalDistance));
                var facing = transform.TransformDirection(new Vector3(0f, 0f, side));
                var notCharacters = ~LayerMask.GetMask("Character");
                if (Physics.Raycast(position + Vector3.up * GroundSearch, Vector3.down, out var hit, GroundSearch * 2f,
                        notCharacters, QueryTriggerInteraction.Ignore))
                {
                    position.y = hit.point.y;
                }

                return new Pose(position, Quaternion.LookRotation(facing, Vector3.up));
            }
        }

        /// <summary>Authoring default, m: how far above and below the sill the arrival looks for the ground.</summary>
        private const float GroundSearch = 0.75f;

        /// <summary>
        /// Setup for scenes built by code, and for a shared scene that sets its door when it learns where it is; a
        /// placed door is set in the Inspector.
        /// </summary>
        public void Configure(string id, string sceneName, string pairedPortalId, bool arrivalOnPositiveZ, string context = null)
        {
            portalId = id;
            targetScene = sceneName;
            targetPortalId = pairedPortalId;
            arriveOnPositiveZ = arrivalOnPositiveZ;
            travelContext = context ?? string.Empty;
        }

        /// <summary>
        /// True when the player stands within reach of the opening, on its level: a landing over a street door is not
        /// within reach of it (owner's play test VI-5, remark 2). The player's feet stand from the sill up to a little
        /// above it, as on a step before the door.
        /// </summary>
        public bool InReach(Vector3 position)
        {
            var sill = transform.position.y;
            if (position.y < sill - 1f || position.y > sill + 1.5f)
            {
                return false;
            }

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
            if (Sealed || input == null || !input.InteractPressed || SceneTransition.IsTraveling)
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

using GameDev.Core.Scenes;
using UnityEngine;

namespace GameDev.World.Interiors
{
    /// <summary>
    /// Marks a scene as the interior of one building (17 I.3). V0 holds only the building's id: free third-person
    /// rules stay as they are outside, and the interior's door is an ordinary portal. Later systems find the open
    /// interior here, to keep its mutable state by building id (17 W.7).
    /// </summary>
    public sealed class InteriorRuntime : MonoBehaviour
    {
        [Tooltip("The building's id, the same as its root object's name in the street scene.")]
        [SerializeField] private string buildingId;

        /// <summary>The interior open now, or null outdoors.</summary>
        public static InteriorRuntime Current { get; private set; }

        public string BuildingId => buildingId;

        /// <summary>Editor setup for scenes built by code.</summary>
        public void Configure(string id)
        {
            buildingId = id;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
        }

        private void OnEnable()
        {
            Current = this;

            // Leaving this scene unloads it, and the street it was entered from stays loaded behind it (17 I.2).
            SceneTransition.MarkInterior(gameObject.scene);
        }

        private void OnDisable()
        {
            if (Current == this)
            {
                Current = null;
            }
        }
    }
}

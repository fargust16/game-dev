using GameDev.Locomotion;
using GameDev.World.Poi;
using UnityEngine;

namespace GameDev.Slice
{
    /// <summary>
    /// A POI's scene where it happens (04 *Lazy activation*; VERTICAL_SLICE_IMPLEMENTATION_PLAN S2.5): its people stand
    /// hidden until the player enters the area while the POI's window is open, and then stay, posed, as scenes stand still
    /// in S2. Its conversation, a <see cref="PersonTalk"/> beside it, opens only while the scene is there and the POI open;
    /// one already open runs on to its end, so the outcome that settles the POI is still read. Once the POI has resolved,
    /// its people leave when the player is out of the area, so nobody sees them go.
    /// </summary>
    public sealed class PoiStage : MonoBehaviour
    {
        private static readonly int Idle = Animator.StringToHash("Idle");

        [SerializeField] private PoiDefinition poi;

        [Tooltip("The scene's people, inactive in the scene.")]
        [SerializeField] private GameObject cast;

        [Tooltip("Authoring default, m: the area whose entry brings the scene, measured flat from this point.")]
        [SerializeField] private float appearRadius = 60f;

        private PersonTalk _talk;
        private PlayerLocomotion _player;

        public PoiDefinition Poi => poi;

        public float AppearRadius => appearRadius;

        public bool IsShown => cast != null && cast.activeSelf;

        public PersonTalk Talk => _talk != null ? _talk : _talk = GetComponent<PersonTalk>();

        /// <summary>Setup for scenes built by code.</summary>
        public void Configure(PoiDefinition definition, GameObject people, float radius)
        {
            poi = definition;
            cast = people;
            appearRadius = radius;
        }

        /// <summary>Whether <paramref name="position"/> is inside the scene's area.</summary>
        public bool Within(Vector3 position)
        {
            var flat = position - transform.position;
            flat.y = 0f;
            return flat.sqrMagnitude <= appearRadius * appearRadius;
        }

        private void Awake()
        {
            if (cast != null)
            {
                cast.SetActive(false);
            }

            if (Talk != null)
            {
                Talk.Available = false;
            }
        }

        private void Update()
        {
            var world = SliceWorldDriver.Running;
            if (world == null || poi == null || cast == null)
            {
                return;
            }

            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerLocomotion>();
                if (_player == null)
                {
                    return;
                }
            }

            var open = world.Director.Phase(poi) == PoiPhase.Open;
            var near = Within(_player.transform.position);
            var talking = Talk != null && Talk.IsOpen;
            if (!IsShown && open && near)
            {
                Show();
            }
            else if (IsShown && !open && !near && !talking)
            {
                cast.SetActive(false);
            }

            if (Talk != null)
            {
                Talk.Available = IsShown && open;
            }
        }

        // Scenes stand still: anyone with an idle pose takes it, the townsfolk's default being their walk.
        private void Show()
        {
            cast.SetActive(true);
            foreach (var animator in cast.GetComponentsInChildren<Animator>())
            {
                if (animator.runtimeAnimatorController != null && animator.HasState(0, Idle))
                {
                    animator.Play(Idle, 0);
                }
            }
        }
    }
}

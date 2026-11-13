using GameDev.Core.Scenes;
using GameDev.Locomotion;
using GameDev.World.Interiors;
using GameDev.World.Portals;
using UnityEngine;

namespace GameDev.Slice.Dwellings
{
    /// <summary>
    /// The shared Dwelling scene (15, *The slice's Dwellings*): every dwelling's street door leads here with its
    /// <see cref="DwellingKey"/> as the journey's context, and this builds that home's rooms on arrival from the key and
    /// its own seed stream, sets the door to lead back, and stands the player inside it. Opened alone in the editor, it
    /// builds its preview key instead.
    /// </summary>
    public sealed class DwellingRuntime : MonoBehaviour
    {
        /// <summary>The id every dwelling's street door arrives at.</summary>
        public const string ArrivalPortalId = "dwelling.inside";

        [SerializeField] private DwellingKit kit;
        [SerializeField] private SliceSettings settings;
        [SerializeField] private InteriorRuntime interior;

        [Tooltip("The dwelling built when the scene is opened alone, as a DwellingKey's context.")]
        [SerializeField] private string previewContext;

        private Transform _built;

        /// <summary>The dwelling standing in the scene now, or null.</summary>
        public static DwellingRuntime Current { get; private set; }

        public DwellingKey Key { get; private set; }

        public DwellingPlan Plan { get; private set; }

        /// <summary>Editor setup for the scene built by code.</summary>
        public void Configure(DwellingKit dwellingKit, SliceSettings sliceSettings, InteriorRuntime interiorRuntime, string preview)
        {
            kit = dwellingKit;
            settings = sliceSettings;
            interior = interiorRuntime;
            previewContext = preview;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
        }

        private void OnEnable()
        {
            Current = this;
            SceneTransition.Arriving += OnArriving;
        }

        private void OnDisable()
        {
            SceneTransition.Arriving -= OnArriving;
            if (Current == this)
            {
                Current = null;
            }
        }

        private void Start()
        {
            if (_built == null && DwellingKey.TryParse(previewContext, out var key))
            {
                Build(key);
            }
        }

        private void OnArriving(TravelRequest request)
        {
            if (request.ArrivalPortalId != ArrivalPortalId)
            {
                return;
            }

            if (!DwellingKey.TryParse(request.Context, out var key))
            {
                Debug.LogError($"[Dwelling] the journey '{request}' carries no dwelling: '{request.Context}'.", this);
                return;
            }

            var door = Build(key);
            var player = FindAnyObjectByType<PlayerLocomotion>();
            if (door != null && player != null)
            {
                var pose = door.ArrivalPose;
                player.Teleport(pose.position, pose.rotation);
            }
        }

        /// <summary>Builds the dwelling of <paramref name="key"/>, replacing any built before, and returns its street door.</summary>
        public Portal Build(DwellingKey key)
        {
            if (_built != null)
            {
                Destroy(_built.gameObject);
            }

            Key = key;
            Plan = PlanFor(key, settings, kit);
            _built = new GameObject("Dwelling - " + key.StreetPortalId).transform;
            _built.SetParent(transform, false);
            var door = DwellingAssembler.Build(Plan, kit, _built, key, key.StreetPortalId + ".inside");
            if (interior != null)
            {
                // The dwelling's own id, the same through its street door and its back door.
                interior.Configure(key.DwellingId);
            }

            // The door's arrival looks for the floor under it, which the physics scene knows only once synced.
            Physics.SyncTransforms();

            // Veinmarket's den keeps its bench on its office desk, for one who heard of it (S5b.4; SLICE_TRIAL §4).
            GameDev.Slice.Alchemy.AlchemyBench.AtDwelling(key.DwellingId, _built);

            // Who is at home, and the household's Stash (S5c.5; 10.3 §6.1, 19 §6.3).
            var residents = DwellingResidents.Populate(key, Plan, _built, settings);
            DwellingStash.At(_built, key, residents, settings);
            return door;
        }

        /// <summary>The plan of a dwelling: the same key and slice seed always give the same rooms.</summary>
        public static DwellingPlan PlanFor(DwellingKey key, SliceSettings sliceSettings, IDwellingFootprints footprints)
        {
            var rng = sliceSettings.CreateSeed().Fork(SliceSeedStreams.Dwellings, key.DoorX, key.DoorZ);
            return DwellingPlanner.Plan(key, rng, footprints);
        }
    }
}

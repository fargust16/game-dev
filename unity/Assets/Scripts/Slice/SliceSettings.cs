using GameDev.Player.Progression;
using GameDev.Player.Standing;
using GameDev.World.Seed;
using UnityEngine;

namespace GameDev.Slice
{
    /// <summary>Immutable settings of the Vertical Slice run (VERTICAL_SLICE_IMPLEMENTATION_PLAN §1).</summary>
    [CreateAssetMenu(menuName = "GameDev/Slice/Slice Settings", fileName = "SliceSettings")]
    public sealed class SliceSettings : ScriptableObject
    {
        [Tooltip("The slice's one fixed world seed; every random choice is a named fork of it (SliceSeedStreams). Authoring default.")]
        [SerializeField] private int seed = 110201;

        [Tooltip("10.4 §2's numbers for every conversation's checks.")]
        [SerializeField] private SocialCheckRules socialChecks;

        [Tooltip("10.4 §4–5's numbers: Reputation, Danger, Attitude and lies.")]
        [SerializeField] private StandingRules standing;

        public int Seed => seed;

        public SocialCheckRules SocialChecks => socialChecks;

        public StandingRules Standing => standing;

        public IWorldSeed CreateSeed()
        {
            return new WorldSeed(seed);
        }
    }
}

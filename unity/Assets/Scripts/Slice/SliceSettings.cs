using GameDev.Confront.Rules;
using GameDev.Confront.Stealth;
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

        [Header("Confront (S3a.4)")]
        [Tooltip("10.2 §5b's numbers for every fight.")]
        [SerializeField] private FightRules fightRules;

        [Tooltip("The development kit until S5a's shops (owner, 2026-11-09): a sword in hand, the rifle with its Cartridge Pouch as the second Weapon Set, and leather.")]
        [SerializeField] private FightWeapon kitSword;
        [SerializeField] private FightWeapon kitRifle;
        [SerializeField] private FightOffHand kitPouch;
        [SerializeField] private FightArmour kitArmour;

        [Tooltip("The development fight's opponents: a model and the weapon each carries. Authoring defaults.")]
        [SerializeField] private GameObject opponentModel;
        [SerializeField] private FightWeapon opponentWeapon;

        [Tooltip("The Combat Grid's cell outlines.")]
        [SerializeField] private Material gridMaterial;

        [Header("Stealth (S3b.1)")]
        [Tooltip("10.3's numbers: Stealth Mode, the Vision Cone and the Notice Meter.")]
        [SerializeField] private StealthRules stealthRules;

        [Header("The player's turn (S3a.5)")]
        [Tooltip("Basic RPG Cursors (asset map §5), one for each FightCursor shape in its order: plain, walk, strike, a walk the AP do not reach, a blow they do not reach. SliceConfrontSetup imports them as cursors.")]
        [SerializeField] private Texture2D[] fightCursors;

        public int Seed => seed;

        public SocialCheckRules SocialChecks => socialChecks;

        public StandingRules Standing => standing;

        public FightRules FightRules => fightRules;

        public StealthRules StealthRules => stealthRules;

        public FightWeapon KitSword => kitSword;

        public FightWeapon KitRifle => kitRifle;

        public FightOffHand KitPouch => kitPouch;

        public FightArmour KitArmour => kitArmour;

        public GameObject OpponentModel => opponentModel;

        public FightWeapon OpponentWeapon => opponentWeapon;

        public Material GridMaterial => gridMaterial;

        public Texture2D[] FightCursors => fightCursors;

        public IWorldSeed CreateSeed()
        {
            return new WorldSeed(seed);
        }
    }
}

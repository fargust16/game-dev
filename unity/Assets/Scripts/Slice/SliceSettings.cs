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

        [Tooltip("07.1 §4–8's curve for action checks outside a fight, such as a close look at a Physical Trace (S4.1).")]
        [SerializeField] private ActionCheckRules actionChecks;

        [Header("Confront (S3a.4)")]
        [Tooltip("10.2 §5b's numbers for every fight.")]
        [SerializeField] private FightRules fightRules;

        [Tooltip("The development kit (10.2 §5d, owner 2026-11-12: firearms first): the pistol with its Cartridge Pouch, the rifle with another as the second Weapon Set, and leather. The rifle is every rifleman's too.")]
        [SerializeField] private FightWeapon kitRifle;
        [SerializeField] private FightOffHand kitPouch;
        [SerializeField] private FightArmour kitArmour;

        [Tooltip("Every item of the slice (S5a.1; 19 §4.1, §6.3).")]
        [SerializeField] private GameDev.Items.ItemCatalogue items;

        [Tooltip("A character with no weapon in a Weapon Set fights with this: the dagger's numbers at half damage (S5a.2 working reading).")]
        [SerializeField] private FightWeapon fists;

        [Tooltip("Each weapon's and shield's model as held (10.2 §5c), made by SliceWeapons.")]
        [SerializeField] private GameDev.Slice.Gear.WeaponModels weaponModels;

        [Tooltip("The shops on Bawling Row, their keepers, goods and lessons (S5a.3).")]
        [SerializeField] private GameDev.Slice.Shops.ShopCatalogue shops;

        [Tooltip("Gathering's nodes and Stamina (S5a.5; 19 §6.4, 07.2 §3.2).")]
        [SerializeField] private GameDev.Slice.Gathering.GatherRules gathering;

        [Tooltip("The development fight's opponents' model; what they carry is their group's (10.2 §5d). Authoring default.")]
        [SerializeField] private GameObject opponentModel;

        [Tooltip("10.2 §5d (owner, 2026-11-12: firearms first): the pistol, with the Cartridge Pouch, of every fighter who carries one, the development kit's too.")]
        [SerializeField] private FightWeapon peoplePistol;

        [Tooltip("10.2 §5f: the Wardens' baton, every Warden's second set beside its firearm; it subdues.")]
        [SerializeField] private FightWeapon wardensBaton;

        [Tooltip("05.1 *Abilities of the slice*: every ability of the slice's two Arcana, in a fixed order the player's Prepared set is kept by (CR4). SliceConfrontSetup sets them.")]
        [SerializeField] private FightAbility[] abilities = new FightAbility[0];

        [Tooltip("The Combat Grid's cell outlines.")]
        [SerializeField] private Material gridMaterial;

        [Tooltip("The outline of the enemy under the fight's cursor (owner's play test, 2026-11-12).")]
        [SerializeField] private Shader targetOutline;

        [Header("The church (PB8d)")]
        [Tooltip("10.2 §5e: the church's people who stand in the old chapel, each a prefab with its PersonRole. When the goddess notices two deaths within the hour, those initiated who carry a firearm come out of its door.")]
        [SerializeField] private GameObject[] churchPeople = new GameObject[0];

        [Header("Stealth (S3b.1)")]
        [Tooltip("10.3's numbers: Stealth Mode, the Vision Cone and the Notice Meter.")]
        [SerializeField] private StealthRules stealthRules;

        [Header("Beasts (S3b.2)")]
        [Tooltip("10.2's beasts: their values, territory, warnings, glow, and what they leave.")]
        [SerializeField] private GameDev.Slice.Beasts.BeastRules beastRules;

        [Header("The player's turn (S3a.5)")]
        [Tooltip("Basic RPG Cursors (asset map §5), one for each FightCursor shape in its order: plain, walk, strike, a walk the AP do not reach, a blow they do not reach. SliceConfrontSetup imports them as cursors.")]
        [SerializeField] private Texture2D[] fightCursors;

        [Tooltip("The fight hotbar's icons (Modern GDR pack, asset map), one for each FightAction after None in its order: end turn, wait, Defensive Stance, change weapons, First Aid, Stealth, Сбежать, Yield, Spare, Infusion. SliceConfrontSetup sets them.")]
        [SerializeField] private Sprite[] fightIcons;

        public int Seed => seed;

        public SocialCheckRules SocialChecks => socialChecks;

        public StandingRules Standing => standing;

        public ActionCheckRules ActionChecks => actionChecks;

        public FightRules FightRules => fightRules;

        public StealthRules StealthRules => stealthRules;

        public GameDev.Slice.Beasts.BeastRules BeastRules => beastRules;

        public FightWeapon KitRifle => kitRifle;

        public FightOffHand KitPouch => kitPouch;

        public FightArmour KitArmour => kitArmour;

        public GameDev.Items.ItemCatalogue Items => items;

        public FightWeapon Fists => fists;

        public GameDev.Slice.Gear.WeaponModels WeaponModels => weaponModels;

        public GameDev.Slice.Shops.ShopCatalogue Shops => shops;

        public GameDev.Slice.Gathering.GatherRules Gathering => gathering;

        public GameObject OpponentModel => opponentModel;

        public FightWeapon PeoplePistol => peoplePistol;

        public FightWeapon WardensBaton => wardensBaton;

        public FightAbility[] Abilities => abilities;

        public GameObject[] ChurchPeople => churchPeople;

        public Material GridMaterial => gridMaterial;

        public Shader TargetOutline => targetOutline;

        public Texture2D[] FightCursors => fightCursors;

        public Sprite[] FightIcons => fightIcons;

        public IWorldSeed CreateSeed()
        {
            return new WorldSeed(seed);
        }
    }
}

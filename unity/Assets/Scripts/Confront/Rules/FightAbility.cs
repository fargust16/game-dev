using GameDev.Player.Progression;
using UnityEngine;

namespace GameDev.Confront.Rules
{
    /// <summary>What an ability does; the turns carry each effect out (10.2 §5f, 05.1 *Abilities of the slice*).</summary>
    public enum AbilityEffect
    {
        /// <summary>An ability of exploration or dialogue: no effect in a fight; its own system carries it (S5b).</summary>
        None,

        /// <summary>Дрёма: a person who fails a Will Contest sleeps until its next turn is lost, until damage or a waking ally.</summary>
        Sleep,

        /// <summary>Стой!: a target who fails a Will Contest loses its next turn.</summary>
        Order,

        /// <summary>Укол разума: a ranged magical attack; a hit disorients the target for its next turn, a miss the caster.</summary>
        Sting,

        /// <summary>Завеса: unseen for some rounds; an attack ends it.</summary>
        Veil,

        /// <summary>Столп: each enemy beside who fails a Will Contest must strike only the caster in its next turn.</summary>
        Pillar,

        /// <summary>Сокрушение: a heavy blow of the melee weapon in hand, that cannot be parried or blocked.</summary>
        Crush,

        /// <summary>Не сдвинуть: paid from the AP left at the end of the turn, less physical damage until the next.</summary>
        Brace,

        /// <summary>Опора, passive: allies within its range gain physical defence.</summary>
        Support,

        /// <summary>Пелена: a cloud at a point in sight, a Partial Obstacle for some rounds (CR5).</summary>
        Screen,

        /// <summary>Двойник: an unmoving copy stays and the caster is unseen until it deals damage (CR5).</summary>
        Double,

        /// <summary>Рывок: an instant move to a free cell behind an enemy in sight, drawing no Reaction Attack (CR5).</summary>
        Dash,

        /// <summary>Гнёт: magical damage to everyone in an area with a clear line from its point, with no roll (CR5).</summary>
        Press,
    }

    /// <summary>
    /// One ability of an Arcana at a Limit (05.1 *Abilities of the slice*, 10.2 §5f, 07.2 §3.3): passive or active, its
    /// price as a share of maximum Will Power, its AP, range and numbers. Will Power is the only limit of its use, with
    /// no cooldown. Its name and text are the string table's <c>ability.{key}.name</c> and <c>ability.{key}.text</c>.
    /// The numbers are 05.1's authoring defaults.
    /// </summary>
    [CreateAssetMenu(menuName = "GameDev/Confront/Fight Ability", fileName = "Ability")]
    public sealed class FightAbility : ScriptableObject
    {
        [Tooltip("The string table's key: ability.{key}.name and ability.{key}.text.")]
        [SerializeField] private string key;

        [Tooltip("05.1: the Arcana whose Limit opens it.")]
        [SerializeField] private Arcana arcana;

        [Tooltip("05.1: the Limit whose Ability Pool holds it.")]
        [SerializeField, Range(0, 9)] private int limit = 9;

        [Tooltip("05.1: a passive ability always works and takes no Prepared slot (07.2 §4.5).")]
        [SerializeField] private bool passive;

        [Tooltip("What it does in a fight; None for an ability of exploration or dialogue.")]
        [SerializeField] private AbilityEffect effect;

        [Tooltip("05.1: its price, a share of maximum Will Power: ordinary 0.15, powerful 0.33.")]
        [SerializeField, Range(0f, 1f)] private float cost;

        [Tooltip("05.1: its AP; for Сокрушение the AP added to its weapon's.")]
        [SerializeField, Min(0)] private int ap;

        [Tooltip("05.1: paid from the AP left at the end of the turn, which it ends, never from a blow (Не сдвинуть).")]
        [SerializeField] private bool endOfTurn;

        [Tooltip("05.1, cells: how far its target or point may be; 1 for one beside.")]
        [SerializeField, Min(0)] private int range;

        [Tooltip("05.1: magical damage before the roll and the target's magical defence; no attribute adds to it (10.2 §5f).")]
        [SerializeField, Min(0f)] private float baseDamage;

        [Tooltip("10.2 §5f Will Contest: the chance it starts from, 0.5, or 0.4 for Дрёма; 0 for none.")]
        [SerializeField, Range(0f, 1f)] private float contest;

        [Tooltip("05.1: rounds it lasts, or turns it takes from its target.")]
        [SerializeField, Min(0)] private int rounds;

        [Tooltip("05.1, cells: the side of its square area; 0 for none.")]
        [SerializeField, Min(0)] private int area;

        [Tooltip("05.1 (Сокрушение): what its blow's damage is multiplied by.")]
        [SerializeField, Min(0f)] private float multiplier = 1f;

        [Tooltip("Its slot's icon on the fight's hotbar; SliceConfrontSetup sets it from the Modern GDR pack (asset map).")]
        [SerializeField] private Sprite icon;

        public string Key => key;
        public Arcana Arcana => arcana;
        public int Limit => limit;
        public bool Passive => passive;
        public AbilityEffect Effect => effect;
        public float Cost => cost;
        public int Ap => ap;
        public bool EndOfTurn => endOfTurn;
        public int Range => range;
        public float BaseDamage => baseDamage;
        public float Contest => contest;
        public int Rounds => rounds;
        public int Area => area;
        public float Multiplier => multiplier;
        public Sprite Icon => icon;

        /// <summary>Whether it can be used in a fight: an active ability with an effect there.</summary>
        public bool InFight => !passive && effect != AbilityEffect.None;

        /// <summary>A test's or a tool's ability, made in code.</summary>
        public static FightAbility Make(string key, Arcana arcana, int limit, AbilityEffect effect, float cost, int ap, int range = 0,
            float baseDamage = 0f, float contest = 0f, int rounds = 0, bool passive = false, bool endOfTurn = false, int area = 0, float multiplier = 1f)
        {
            var a = CreateInstance<FightAbility>();
            a.name = key;
            a.hideFlags = HideFlags.DontSave;
            a.key = key;
            a.arcana = arcana;
            a.limit = limit;
            a.effect = effect;
            a.cost = cost;
            a.ap = ap;
            a.range = range;
            a.baseDamage = baseDamage;
            a.contest = contest;
            a.rounds = rounds;
            a.passive = passive;
            a.endOfTurn = endOfTurn;
            a.area = area;
            a.multiplier = multiplier;
            return a;
        }
    }
}

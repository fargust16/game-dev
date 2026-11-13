using System;
using System.Collections.Generic;

namespace GameDev.Player.Progression
{
    /// <summary>The seven attributes of 07.1 §2.</summary>
    public enum CharacterAttribute
    {
        Strength,
        Endurance,
        Dexterity,
        Perception,
        Intelligence,
        Will,
        Luck,
    }

    /// <summary>
    /// The character's skills. The slice starts with the three of the Social Checks (10.4 §2) and the combat skills
    /// (07.1 §10, 10.2 §5f); the rest of 07.1's catalogue joins with the system that uses it. A new skill goes at the end,
    /// because dialogue assets store a skill by its number; a renamed one keeps its number, and a save's old name is read
    /// by <see cref="CharacterSheet.Restore"/>.
    /// </summary>
    public enum CharacterSkill
    {
        Persuasion,
        Deception,
        Intimidation,

        /// <summary>10.2 §5f: the dagger, sword, axe, mace, the Wardens' baton, fists and natural weapons; Melee Skill until the combat rework.</summary>
        LightMelee,

        /// <summary>10.2 §5f: the pistol and the rifle; Ranged Skill until the combat rework.</summary>
        LightFirearms,

        /// <summary>10.2 First Aid in the slice: a treatment's check takes `(Dexterity + First Aid) / 2` (S3a.7).</summary>
        FirstAid,

        /// <summary>10.3 §3, §9: the Notice Meter weighs `(Dexterity + Stealth + Darkness) / 2` against Perception (S3b.1).</summary>
        Stealth,

        /// <summary>19 §3: the count of a made piece's modifiers rolls at Item Creation, then Perception and Luck (S5a.4).</summary>
        ItemCreation,

        /// <summary>19 §6.4: herb and flax patches (S5a.5).</summary>
        Herbalism,

        /// <summary>19 §6.4: fellable trees (S5a.5).</summary>
        Woodcutting,

        /// <summary>19 §6.4: iron veins (S5a.5).</summary>
        Mining,

        /// <summary>19 §6.4: a fallen beast's hide, `(Dexterity + Skinning) / 2` against D 2, after the lesson (S5a.6).</summary>
        Skinning,

        /// <summary>10.3 §6: theft from a person and a lock, `(Dexterity + Theft) / 2` against Perception or the lock (S5a.8).</summary>
        Theft,

        /// <summary>10.2 §5f: the greatsword, greataxe and maul, aimed by Strength.</summary>
        HeavyMelee,

        /// <summary>10.2 §5f: heavy firearms, none yet, aimed by Strength.</summary>
        HeavyFirearms,

        /// <summary>10.2 §5f, 05.1: the abilities of the character's own Arcana, aimed with Intelligence.</summary>
        Arcana,

    }

    /// <summary>What a check reads of a character.</summary>
    public interface ICharacterValues
    {
        int Get(CharacterAttribute attribute);

        int Get(CharacterSkill skill);
    }

    /// <summary>
    /// One character's attributes and skills (07.1 §1–2; 17 P.3): every value from 1 to 100, all starting at 1
    /// (07 Character Life). The player's sheet is kept in the save's player.character section; NPCs use the same
    /// sheet. Each value grows by its own use (07.1 §3, S5b.2): <see cref="Train"/> gives a use's XP by
    /// <see cref="Growth"/>, each track carries the XP toward its next level, and only the player's sheet is trained.
    /// </summary>
    public sealed class CharacterSheet : ICharacterValues
    {
        public const int Lowest = 1;
        public const int Highest = 100;

        private readonly int[] _attributes = Start(Enum.GetValues(typeof(CharacterAttribute)).Length);
        private readonly int[] _skills = Start(Enum.GetValues(typeof(CharacterSkill)).Length);
        private readonly double[] _attributeXp = new double[Enum.GetValues(typeof(CharacterAttribute)).Length];
        private readonly double[] _skillXp = new double[Enum.GetValues(typeof(CharacterSkill)).Length];

        /// <summary>An attribute reached a new level by use.</summary>
        public event Action<CharacterAttribute, int> AttributeGrew;

        /// <summary>A skill reached a new level by use.</summary>
        public event Action<CharacterSkill, int> SkillGrew;

        public int Get(CharacterAttribute attribute) => _attributes[(int)attribute];

        public int Get(CharacterSkill skill) => _skills[(int)skill];

        public void Set(CharacterAttribute attribute, int value) => _attributes[(int)attribute] = Clamp(value);

        public void Set(CharacterSkill skill, int value) => _skills[(int)skill] = Clamp(value);

        /// <summary>The XP an attribute carries toward its next level.</summary>
        public double Xp(CharacterAttribute attribute) => _attributeXp[(int)attribute];

        public double Xp(CharacterSkill skill) => _skillXp[(int)skill];

        /// <summary>
        /// One use (07.1 §3; owner, 2026-11-13, S5-1): the skill takes its award by the task's stage and the outcome, each
        /// primary attribute of the use's formula a hundredth of what it would take, each secondary one half that, each
        /// counted on its own level. A use with no skill (null) trains its attributes alone.
        /// </summary>
        public void Train(CharacterSkill? skill, int taskStage, CheckOutcome outcome, IReadOnlyList<CharacterAttribute> primary,
            IReadOnlyList<CharacterAttribute> secondary = null)
        {
            if (skill.HasValue)
            {
                Gain(skill.Value, Growth.Award(Get(skill.Value), taskStage, outcome));
            }

            foreach (var attribute in primary ?? Array.Empty<CharacterAttribute>())
            {
                Gain(attribute, Growth.Award(Get(attribute), taskStage, outcome, Growth.AttributeShare));
            }

            foreach (var attribute in secondary ?? Array.Empty<CharacterAttribute>())
            {
                Gain(attribute, Growth.Award(Get(attribute), taskStage, outcome, Growth.SecondaryShare));
            }
        }

        /// <summary>XP onto an attribute, its levels gained while enough is carried; returns the levels gained.</summary>
        public int Gain(CharacterAttribute attribute, double xp)
        {
            var gained = Grow(ref _attributes[(int)attribute], ref _attributeXp[(int)attribute], xp);
            if (gained > 0)
            {
                AttributeGrew?.Invoke(attribute, Get(attribute));
            }

            return gained;
        }

        public int Gain(CharacterSkill skill, double xp)
        {
            var gained = Grow(ref _skills[(int)skill], ref _skillXp[(int)skill], xp);
            if (gained > 0)
            {
                SkillGrew?.Invoke(skill, Get(skill));
            }

            return gained;
        }

        /// <summary>Ordinary growth stops at 99; 100 asks special criteria (07.1 §1), so XP stops gathering there.</summary>
        private static int Grow(ref int level, ref double carried, double xp)
        {
            if (xp <= 0 || double.IsNaN(xp) || level >= Growth.Top)
            {
                return 0;
            }

            var gained = 0;
            carried += xp;
            while (level < Growth.Top && carried >= Growth.ToNext(level))
            {
                carried -= Growth.ToNext(level);
                level++;
                gained++;
            }

            if (level >= Growth.Top)
            {
                carried = 0;
            }

            return gained;
        }

        public CharacterSheetState Capture()
        {
            var state = new CharacterSheetState();
            foreach (CharacterAttribute attribute in Enum.GetValues(typeof(CharacterAttribute)))
            {
                state.attributes.Add(new CharacterValueEntry { name = attribute.ToString(), value = Get(attribute), xp = Xp(attribute) });
            }

            foreach (CharacterSkill skill in Enum.GetValues(typeof(CharacterSkill)))
            {
                state.skills.Add(new CharacterValueEntry { name = skill.ToString(), value = Get(skill), xp = Xp(skill) });
            }

            return state;
        }

        /// <summary>The skills' names before the combat rework (10.2 §5f), read from older saves.</summary>
        private static readonly Dictionary<string, CharacterSkill> Renamed = new Dictionary<string, CharacterSkill>
        {
            ["MeleeSkill"] = CharacterSkill.LightMelee,
            ["RangedSkill"] = CharacterSkill.LightFirearms,
        };

        /// <summary>
        /// Values are saved by name, so a value a save lacks starts at 1 and a name this build lacks is skipped; a skill
        /// renamed since is read by its old name.
        /// </summary>
        public void Restore(CharacterSheetState state)
        {
            Array.Fill(_attributes, Lowest);
            Array.Fill(_skills, Lowest);
            Array.Fill(_attributeXp, 0d);
            Array.Fill(_skillXp, 0d);
            foreach (var entry in state.attributes)
            {
                if (Enum.IsDefined(typeof(CharacterAttribute), entry.name))
                {
                    var attribute = (CharacterAttribute)Enum.Parse(typeof(CharacterAttribute), entry.name);
                    Set(attribute, entry.value);
                    _attributeXp[(int)attribute] = Math.Max(0d, entry.xp);
                }
            }

            foreach (var entry in state.skills)
            {
                var known = Enum.IsDefined(typeof(CharacterSkill), entry.name);
                if (known || (entry.name != null && Renamed.ContainsKey(entry.name)))
                {
                    var skill = known ? (CharacterSkill)Enum.Parse(typeof(CharacterSkill), entry.name) : Renamed[entry.name];
                    Set(skill, entry.value);
                    _skillXp[(int)skill] = Math.Max(0d, entry.xp);
                }
            }
        }

        private static int Clamp(int value) => Math.Clamp(value, Lowest, Highest);

        private static int[] Start(int count)
        {
            var values = new int[count];
            Array.Fill(values, Lowest);
            return values;
        }
    }

    [Serializable]
    public sealed class CharacterSheetState
    {
        public List<CharacterValueEntry> attributes = new List<CharacterValueEntry>();
        public List<CharacterValueEntry> skills = new List<CharacterValueEntry>();
    }

    [Serializable]
    public sealed class CharacterValueEntry
    {
        public string name;
        public int value;

        /// <summary>The XP carried toward the next level (S5b.2); 0 in an older save.</summary>
        public double xp;
    }
}

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
    /// The character's skills. The slice starts with the three of the Social Checks (10.4 §2) and the two weapon skills
    /// of a fight (10.2 §5b *Attack*); the rest of 07.1's catalogue joins with the system that uses it. A new skill goes
    /// at the end, because dialogue assets store a skill by its number.
    /// </summary>
    public enum CharacterSkill
    {
        Persuasion,
        Deception,
        Intimidation,
        MeleeSkill,
        RangedSkill,

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
    /// sheet. How the values grow (07.1 §3) comes with the systems that train them.
    /// </summary>
    public sealed class CharacterSheet : ICharacterValues
    {
        public const int Lowest = 1;
        public const int Highest = 100;

        private readonly int[] _attributes = Start(Enum.GetValues(typeof(CharacterAttribute)).Length);
        private readonly int[] _skills = Start(Enum.GetValues(typeof(CharacterSkill)).Length);

        public int Get(CharacterAttribute attribute) => _attributes[(int)attribute];

        public int Get(CharacterSkill skill) => _skills[(int)skill];

        public void Set(CharacterAttribute attribute, int value) => _attributes[(int)attribute] = Clamp(value);

        public void Set(CharacterSkill skill, int value) => _skills[(int)skill] = Clamp(value);

        public CharacterSheetState Capture()
        {
            var state = new CharacterSheetState();
            foreach (CharacterAttribute attribute in Enum.GetValues(typeof(CharacterAttribute)))
            {
                state.attributes.Add(new CharacterValueEntry { name = attribute.ToString(), value = Get(attribute) });
            }

            foreach (CharacterSkill skill in Enum.GetValues(typeof(CharacterSkill)))
            {
                state.skills.Add(new CharacterValueEntry { name = skill.ToString(), value = Get(skill) });
            }

            return state;
        }

        /// <summary>Values are saved by name, so a value a save lacks starts at 1 and a name this build lacks is skipped.</summary>
        public void Restore(CharacterSheetState state)
        {
            Array.Fill(_attributes, Lowest);
            Array.Fill(_skills, Lowest);
            foreach (var entry in state.attributes)
            {
                if (Enum.IsDefined(typeof(CharacterAttribute), entry.name))
                {
                    Set((CharacterAttribute)Enum.Parse(typeof(CharacterAttribute), entry.name), entry.value);
                }
            }

            foreach (var entry in state.skills)
            {
                if (Enum.IsDefined(typeof(CharacterSkill), entry.name))
                {
                    Set((CharacterSkill)Enum.Parse(typeof(CharacterSkill), entry.name), entry.value);
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
    }
}

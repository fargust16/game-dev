using System;
using GameDev.Player.Progression;
using GameDev.World.Seed;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Slice.People
{
    /// <summary>A resident's role (10.2 §5a *Roles*; SLICE_ASSET_MAP.md §1): its model, Limit, points and firearm.</summary>
    public enum ResidentRole
    {
        /// <summary>A gentleman, a lady or a working woman of the streets.</summary>
        Townsfolk,
        OldBeliever,
        ChurchMaid,
        OldPriest,
        Acolyte,
        FactoryWorker,
        Vagrant,
        Dockworker,
        OldDockworker,
        Moneylender,
        Innkeeper,
        Tailor,
        Warden,
        Cult,
    }

    /// <summary>What a person carries into a fight under 10.2 §5d: a firearm with a Cartridge Pouch, or nothing but its fists.</summary>
    public enum RoleFirearm
    {
        None,
        Pistol,
        Rifle,
    }

    /// <summary>The role a person's prefab gives it; one without this part is <see cref="ResidentRole.Townsfolk"/>.</summary>
    public sealed class PersonRole : MonoBehaviour
    {
        [SerializeField] private ResidentRole role;

        public ResidentRole Role
        {
            get => role;
            set => role = value;
        }

        /// <summary>The role of the person whose body this is, from the part on it or its parents.</summary>
        public static ResidentRole Of(Component body)
        {
            if (body == null)
            {
                return ResidentRole.Townsfolk;
            }

            var part = body.GetComponentInParent<PersonRole>();
            if (part != null)
            {
                return part.Role;
            }

            return PeopleAround.IsWarden(body.transform) ? ResidentRole.Warden : ResidentRole.Townsfolk;
        }
    }

    /// <summary>
    /// 10.2 §5a *Roles* (owner, 2026-11-12: no Limit, or a low one, 9 to 7; the rest authoring defaults): a role's Limit,
    /// the points it adds over 07.1's starting attributes, the two attributes they lean to, and its firearm. Where a role
    /// gives a choice, the seed makes it from the person's name, so the same person is the same in every run of a world.
    /// A Limit is of Жрица; its abilities wait for S5b.
    /// </summary>
    public static class ResidentRoles
    {
        private static readonly CharacterAttribute[] Attributes = (CharacterAttribute[])Enum.GetValues(typeof(CharacterAttribute));

        /// <summary>One row of the table: a Limit with another one in so many, the points, the leaning, and the firearm one in so many.</summary>
        public readonly struct Row
        {
            public readonly int? Limit;
            public readonly int? OtherLimit;
            public readonly int OtherOneIn;
            public readonly int LeastPoints;
            public readonly int MostPoints;
            public readonly CharacterAttribute? First;
            public readonly CharacterAttribute? Second;
            public readonly RoleFirearm Firearm;
            public readonly int FirearmOneIn;

            public Row(int? limit, int? otherLimit, int otherOneIn, int least, int most, CharacterAttribute? first, CharacterAttribute? second, RoleFirearm firearm, int firearmOneIn = 1)
            {
                Limit = limit;
                OtherLimit = otherLimit;
                OtherOneIn = otherOneIn;
                LeastPoints = least;
                MostPoints = most;
                First = first;
                Second = second;
                Firearm = firearm;
                FirearmOneIn = firearmOneIn;
            }
        }

        /// <summary>One person of a role, as the seed made it.</summary>
        public readonly struct Made
        {
            public readonly CharacterSheet Sheet;
            public readonly int? Limit;
            public readonly RoleFirearm Firearm;

            public Made(CharacterSheet sheet, int? limit, RoleFirearm firearm)
            {
                Sheet = sheet;
                Limit = limit;
                Firearm = firearm;
            }
        }

        public static Row RowOf(ResidentRole role) => role switch
        {
            ResidentRole.OldBeliever => new Row(null, null, 0, 3, 5, CharacterAttribute.Will, CharacterAttribute.Perception, RoleFirearm.None),
            ResidentRole.ChurchMaid => new Row(null, 9, 5, 5, 7, CharacterAttribute.Dexterity, CharacterAttribute.Will, RoleFirearm.None),
            ResidentRole.OldPriest => new Row(8, 7, 3, 7, 9, CharacterAttribute.Will, CharacterAttribute.Intelligence, RoleFirearm.Pistol),
            ResidentRole.Acolyte => new Row(9, null, 2, 6, 8, CharacterAttribute.Endurance, CharacterAttribute.Will, RoleFirearm.Pistol),
            ResidentRole.FactoryWorker => new Row(null, null, 0, 5, 7, CharacterAttribute.Strength, CharacterAttribute.Endurance, RoleFirearm.None),
            ResidentRole.Vagrant => new Row(null, null, 0, 6, 8, CharacterAttribute.Dexterity, CharacterAttribute.Luck, RoleFirearm.None),
            ResidentRole.Dockworker => new Row(null, null, 0, 8, 10, CharacterAttribute.Strength, CharacterAttribute.Endurance, RoleFirearm.Pistol, 3),
            ResidentRole.OldDockworker => new Row(null, null, 0, 6, 8, CharacterAttribute.Strength, CharacterAttribute.Endurance, RoleFirearm.None),
            ResidentRole.Moneylender => new Row(null, null, 0, 5, 7, CharacterAttribute.Intelligence, CharacterAttribute.Perception, RoleFirearm.Pistol),
            ResidentRole.Innkeeper => new Row(null, null, 0, 6, 8, CharacterAttribute.Endurance, CharacterAttribute.Strength, RoleFirearm.Rifle),
            ResidentRole.Tailor => new Row(null, null, 0, 5, 7, CharacterAttribute.Dexterity, CharacterAttribute.Intelligence, RoleFirearm.None),
            // 10.2 §5d: a Warden carries a pistol, or a rifle one in three; it keeps the numbers it fights with.
            ResidentRole.Warden => new Row(null, null, 0, 0, 0, null, null, RoleFirearm.Pistol),
            ResidentRole.Cult => new Row(null, null, 0, 0, 0, null, null, RoleFirearm.Pistol),
            _ => new Row(null, null, 0, 4, 7, null, null, RoleFirearm.None),
        };

        /// <summary>
        /// 10.2 §5e (owner, 2026-11-12, VS-ALARM (f)): the share of a role's people who go to look at an Alarm, authoring
        /// defaults. <paramref name="otherResident"/> is a resident of no listed role, a Master or a servant.
        /// </summary>
        public static double LookShare(ResidentRole role, bool otherResident = false) => role switch
        {
            ResidentRole.OldPriest or ResidentRole.Acolyte or ResidentRole.Dockworker => 1d / 2d,
            ResidentRole.Vagrant => 2d / 5d,
            ResidentRole.FactoryWorker or ResidentRole.OldDockworker or ResidentRole.Innkeeper => 1d / 4d,
            ResidentRole.OldBeliever => 1d / 5d,
            ResidentRole.Townsfolk => otherResident ? 1d / 4d : 1d / 6d,
            ResidentRole.ChurchMaid or ResidentRole.Moneylender or ResidentRole.Tailor => 1d / 10d,
            _ => 1d / 4d,
        };

        /// <summary>10.2 §5e: a person's Boldness, 0 to 1, drawn once by the seed from its name, the same in every run of a world.</summary>
        public static double Boldness(string name, IWorldSeed seed) =>
            (seed != null ? seed.Fork(SliceSeedStreams.Boldness, StableHash.Of(name ?? string.Empty)) : new Random(StableHash.Of(name ?? string.Empty) ^ 0x2545F491)).NextDouble();

        /// <summary>10.2 §5e: whether a person goes to look at an Alarm, its Boldness higher than 1 minus its role's share.</summary>
        public static bool GoesToLook(double boldness, double share) => boldness > 1d - share;

        /// <summary>The person named <paramref name="name"/> of <paramref name="role"/>, made by the world's seed.</summary>
        public static Made Make(ResidentRole role, string name, IWorldSeed seed)
        {
            var rolls = seed != null ? seed.Fork(SliceSeedStreams.Roles, StableHash.Of(name ?? string.Empty)) : new Random(StableHash.Of(name ?? string.Empty));
            return Make(role, rolls);
        }

        public static Made Make(ResidentRole role, Random rolls)
        {
            var row = RowOf(role);
            var limit = row.OtherOneIn > 0 && rolls.Next(row.OtherOneIn) == 0 ? row.OtherLimit : row.Limit;
            var sheet = new CharacterSheet();
            var points = row.LeastPoints + rolls.Next(row.MostPoints - row.LeastPoints + 1);
            if (row.First is CharacterAttribute first && row.Second is CharacterAttribute second)
            {
                // Two thirds of the points, rounded up, to the leaning attributes in turn; the rest to the others by the seed.
                var leaning = (points * 2 + 2) / 3;
                for (var i = 0; i < leaning; i++)
                {
                    Add(sheet, i % 2 == 0 ? first : second);
                }

                for (var i = leaning; i < points; i++)
                {
                    CharacterAttribute other;
                    do
                    {
                        other = Attributes[rolls.Next(Attributes.Length)];
                    }
                    while (other == first || other == second);
                    Add(sheet, other);
                }
            }
            else
            {
                for (var i = 0; i < points; i++)
                {
                    Add(sheet, Attributes[rolls.Next(Attributes.Length)]);
                }
            }

            var firearm = row.Firearm;
            if (role == ResidentRole.Warden)
            {
                firearm = rolls.Next(3) == 0 ? RoleFirearm.Rifle : RoleFirearm.Pistol;
            }
            else if (row.FirearmOneIn > 1 && rolls.Next(row.FirearmOneIn) != 0)
            {
                firearm = RoleFirearm.None;
            }

            return new Made(sheet, limit, firearm);
        }

        private static void Add(CharacterSheet sheet, CharacterAttribute attribute) => sheet.Set(attribute, sheet.Get(attribute) + 1);
    }
}

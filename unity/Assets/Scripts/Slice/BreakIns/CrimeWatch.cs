using System.Collections.Generic;
using GameDev.Core.Localization;
using GameDev.Locomotion;
using GameDev.Player.Standing;
using GameDev.Slice.Fights;
using GameDev.Slice.People;
using GameDev.Slice.Stealth;
using GameDev.UI;
using UnityEngine;

namespace GameDev.Slice.BreakIns
{
    /// <summary>
    /// Who sees a crime, and what follows (S5c.5; 10.3 §6.1–6.2). A person sees the character by its Vision Cone and a
    /// clear line, as Stealth Mode reads it; one asleep or frozen by an ability sees nothing, and a beast is no witness. A
    /// witness says its line, the character stands up from Stealth Mode, Reputation falls once a place a day, the
    /// character is Wanted until 08:00, and the nearest free Warden is fetched to the place.
    /// </summary>
    public static class CrimeWatch
    {
        /// <summary>Authoring default, s: how long a witness's line stays over its head.</summary>
        public const float LineSeconds = 4f;

        /// <summary>Every person who sees the character at <paramref name="feet"/> now, among <paramref name="among"/> or everyone.</summary>
        public static List<StealthObserver> Seeing(SliceWorldDriver world, Vector3 feet, IEnumerable<StealthObserver> among = null)
        {
            var seeing = new List<StealthObserver>();
            var hiding = world != null ? world.Hiding : null;
            if (hiding == null || Object.FindAnyObjectByType<PlayerLocomotion>() == null)
            {
                return seeing;
            }

            foreach (var o in among ?? StealthObserver.All)
            {
                if (o == null || o.Managed || !o.isActiveAndEnabled || HeldStill.Of(o.transform) != null)
                {
                    continue;
                }

                if (hiding.Sees(o, feet, hiding.Active))
                {
                    seeing.Add(o);
                }
            }

            // A Warden among them first: it stops the character itself.
            seeing.Sort((a, b) => PeopleAround.IsWarden(b.transform).CompareTo(PeopleAround.IsWarden(a.transform)));
            return seeing;
        }

        /// <summary>
        /// A crime at <paramref name="place"/> was noticed by <paramref name="witness"/>, the character last seen at
        /// <paramref name="at"/>: its line, Reputation, Wanted, and a Warden fetched. False when nothing could follow.
        /// </summary>
        public static bool Noticed(SliceWorldDriver world, StealthObserver witness, Vector3 at, string place, string lineKey, Deed deed = Deed.BreakInNoticed, string messageKey = "ui.breakin.noticed")
        {
            if (world == null || world.BreakIns == null || witness == null)
            {
                return false;
            }

            var warden = PeopleAround.IsWarden(witness.transform);
            if (world.Hiding != null)
            {
                world.Hiding.NoticedBy(witness, Loc.Get(lineKey));
            }
            else
            {
                witness.Say(Loc.Get(lineKey), LineSeconds);
            }

            world.BreakIns.Noticed(place, warden, deed);
            world.Fights?.ReportCrime(at, witness.transform, warden);
            if (messageKey != null)
            {
                Object.FindAnyObjectByType<HudView>()?.ShowMessage(Loc.Get(messageKey));
            }

            return true;
        }
    }
}

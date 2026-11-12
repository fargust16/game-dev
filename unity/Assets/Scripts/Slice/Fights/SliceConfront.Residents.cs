using System.Collections.Generic;
using GameDev.Confront.Rules;
using GameDev.Core.Localization;
using GameDev.Slice.People;
using GameDev.Slice.Stealth;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// 10.2 §5a, residents in a fight (owner, 2026-11-12; PB6). The one struck calls for help as the fight begins, its
    /// line over its head for 3 s, and every person within 30 cells of it hears and shows a line of its own. Every Warden
    /// among them comes from the next round, beside the two nearest beyond who come running (§5c). A resident who hears
    /// joins the lawful side only when its own Force Estimate (21 §8) gives that side, with it, twice the character's
    /// strength. A resident in the fight fights while its side's estimate holds at 1; below it, it walks away toward the
    /// nearest Warden, breaks away (§9), and runs on to that Warden (<see cref="HelpRun"/>).
    /// </summary>
    public sealed partial class SliceConfront
    {
        /// <summary>Authoring defaults, 10.2 §5a: how far a call is heard, in cells counted as moves are.</summary>
        public const int CallCells = 30;

        /// <summary>Authoring defaults, 10.2 §5a: the Force Estimate a resident fights at, and the one it joins at.</summary>
        public const double FightsBackAt = 1d;

        public const double JoinsAt = 2d;

        /// <summary>Authoring default: how long a call's line hangs over a head, s.</summary>
        public const float CallSeconds = 3f;

        /// <summary>Whether a place is within the call's 30 cells of the one struck.</summary>
        private bool Heard(Vector3 at) => Mathf.Max(Mathf.Abs(at.x - _assaultAt.x), Mathf.Abs(at.z - _assaultAt.z)) <= CallCells;

        /// <summary>The one struck calls, and every person within 30 cells of it shows a line of its own.</summary>
        private void CallForHelp(Transform victim)
        {
            Say(victim, "ui.fight.help.call");
            foreach (var body in PeopleAround.InSight())
            {
                if (body != victim && !_player.transform.IsChildOf(body) && Heard(body.position) && !Lying(body))
                {
                    Say(body, PeopleAround.IsWarden(body) ? "ui.fight.help.warden" : "ui.fight.help.heard");
                }
            }
        }

        private static void Say(Transform body, string key)
        {
            var eyes = body.GetComponent<StealthObserver>();
            if (eyes == null)
            {
                eyes = body.GetComponentInChildren<StealthObserver>();
            }

            if (eyes == null)
            {
                // A posted Warden has no eyes of its own: a holder for the line, which the city's watch skips, gone with it.
                eyes = body.gameObject.AddComponent<StealthObserver>();
                eyes.Managed = true;
                Destroy(eyes, CallSeconds);
            }

            eyes.Say(Loc.Get(key), CallSeconds);
        }

        /// <summary>The nearest Warden standing, where a resident runs for help; null when there is none.</summary>
        private static Vector3? HelpFor(Transform body)
        {
            foreach (var warden in PeopleAround.Wardens(body.position))
            {
                if (warden != body && !Lying(warden))
                {
                    return warden.position;
                }
            }

            return null;
        }

        /// <summary>
        /// The residents within the call who join the lawful side, nearest the place first: each when its own Force
        /// Estimate gives that side, with it and those who joined before it, twice the character's strength. Each stops
        /// where it stands and joins at the end of the first round if it stands on the Combat Grid, as everyone does.
        /// </summary>
        private int JoinTheCall(Fighter character, List<Fighter> lawful, Transform victim, ref int id)
        {
            var joined = 0;
            var theirs = new List<Fighter> { character };
            var people = PeopleAround.InSight();
            people.Sort((a, b) => (a.position - _assaultAt).sqrMagnitude.CompareTo((b.position - _assaultAt).sqrMagnitude));
            foreach (var body in people)
            {
                if (body == victim || _peopleInFight.Contains(body) || PeopleAround.IsWarden(body) || _player.transform.IsChildOf(body)
                    || !Heard(body.position) || Lying(body) || body.GetComponent<FightSearch>() != null || body.GetComponent<HelpRun>() != null)
                {
                    continue;
                }

                var fighter = Armed(body, Residents);
                lawful.Add(fighter);
                if (_settings.FightRules.ForceEstimate(lawful, theirs) < JoinsAt)
                {
                    lawful.RemoveAt(lawful.Count - 1);
                    continue;
                }

                var c = Person(body, Residents, id++, fighter);
                c.FightsAt = FightsBackAt;
                c.HelpAt = HelpFor(body);
                joined++;
            }

            return joined;
        }
    }
}

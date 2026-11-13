using System.Collections.Generic;
using GameDev.Confront.Rules;
using GameDev.Core.Localization;
using GameDev.Slice.People;
using GameDev.Slice.Stealth;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// 10.2 §5a, residents in a fight (owner, 2026-11-12; PB6, PB8a). The one struck calls for help as the fight begins,
    /// an Alarm (§5e, <see cref="SliceAlarm"/>): its line over its head for 3 s, and every person within 30 cells of it
    /// hears and shows a line of its own. Every free Warden among them comes running (§5e). A resident who hears joins the
    /// lawful side only when its own Force Estimate (21 §8) gives that side, with it, twice the character's strength. A
    /// resident in the fight fights while its side's estimate holds at 1; below it, it walks away toward the nearest post
    /// whose Wardens are at it, breaks away (§9), and runs on there (<see cref="HelpRun"/>) as the runner who fetches them.
    /// </summary>
    public sealed partial class SliceConfront
    {
        /// <summary>Authoring defaults, 10.2 §5a: the Force Estimate a resident fights at, and the one it joins at.</summary>
        public const double FightsBackAt = 1d;

        public const double JoinsAt = 2d;

        /// <summary>Authoring default: how long a call's line hangs over a head, s.</summary>
        public const float CallSeconds = 3f;

        /// <summary>Whether a place is within the call's 30 cells of the one struck (10.2 §5e).</summary>
        private bool Heard(Vector3 at) => SliceAlarm.Hears(_assaultAt, at);

        internal static void Say(Transform body, string key)
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

        /// <summary>
        /// The nearest post whose Warden is at it and free, where a resident runs for help (10.2 §5e); null when there is
        /// none, and the resident only walks away.
        /// </summary>
        private Vector3? HelpFor(Transform body)
        {
            foreach (var warden in PeopleAround.Wardens(body.position))
            {
                if (warden != body && AtPost(warden))
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

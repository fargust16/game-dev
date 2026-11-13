using GameDev.Slice.People;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// 10.2 §5e, the townsfolk's part of the Alarm Check (owner, 2026-11-12, VS-ALARM (f); PB8c). Every walker who hears
    /// an Alarm of any kind and is in no fight goes to look when its Boldness, drawn once by the seed from its name, is
    /// higher than 1 minus its role's share, and walks 30 cells away otherwise (<see cref="TownsfolkWalker.Heed"/>).
    /// Wardens answer by their own part, the cult through its Link, and the one who raised the Alarm, the character, a
    /// fallen body, a searcher, one inside a door and a person who keeps its place indoors with no route do not walk.
    /// </summary>
    public sealed partial class SliceConfront
    {
        // The townsfolk who went to look at an Alarm since the fight began.
        private readonly System.Collections.Generic.List<Transform> _lookers = new System.Collections.Generic.List<Transform>();

        /// <summary>The walkers the last Alarm sent to look and away.</summary>
        public int LastLooked { get; private set; }

        public int LastAway { get; private set; }

        /// <summary>10.2 §5e: whether this person goes to look at an Alarm, by its Boldness and its role's share.</summary>
        public bool GoesToLook(Transform body)
        {
            var otherResident = body.GetComponentInParent<PersonRole>() == null && body.GetComponentInParent<NamedPerson>() != null;
            return ResidentRoles.GoesToLook(ResidentRoles.Boldness(Who(body), _seed), ResidentRoles.LookShare(PersonRole.Of(body), otherResident));
        }

        private void TownsfolkHear(Alarm alarm)
        {
            LastLooked = 0;
            LastAway = 0;
            foreach (var body in PeopleAround.InSight())
            {
                if (body == alarm.Source || PeopleAround.IsWarden(body) || IsPlayer(body) || Lying(body) || _peopleInFight.Contains(body)
                    || !SliceAlarm.Hears(alarm.At, body.position) || PersonRole.Of(body) == ResidentRole.Cult
                    || body.GetComponent<FightSearch>() != null || (body.TryGetComponent<SliceFighter>(out var fighter) && fighter.Combatant != null)
                    || !body.TryGetComponent<TownsfolkWalker>(out var walker) || !walker.isActiveAndEnabled)
                {
                    continue;
                }

                var look = GoesToLook(body);
                if (walker.Heed(alarm.At, look))
                {
                    if (look)
                    {
                        LastLooked++;
                        if (!_lookers.Contains(body))
                        {
                            _lookers.Add(body); // one a whistling Warden may send running (10.2 §5e; S5c.7)
                        }
                    }
                    else
                    {
                        LastAway++;
                    }
                }
            }

            if (LastLooked + LastAway > 0)
            {
                Debug.Log($"[Confront] the {alarm.Kind.ToString().ToLowerInvariant()}: {LastLooked} townsfolk go to look, {LastAway} walk away.");
            }
        }
    }
}

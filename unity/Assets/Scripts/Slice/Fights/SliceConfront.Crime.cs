using GameDev.Slice.People;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// 10.3 §6.2 (S5c.5): a witness to a break-in or a theft fetches the nearest free Warden. One within an Alarm's hearing
    /// of the witness is called at once, and any other comes after the witness's run to it, reckoned at 4.5 m/s as the
    /// Wardens' run is; a Warden who saw the crime itself sets out at once. The Warden goes to where the character was
    /// last seen and looks round as one come late to a shot does, then goes back to its post. Seeing the character on its
    /// way or there, it stops it with an Arrest (<see cref="WardenAccusation"/>).
    /// </summary>
    public sealed partial class SliceConfront
    {
        /// <summary>
        /// Fetches a Warden to <paramref name="at"/> for <paramref name="witness"/>; false when none is free or a fight is
        /// on. One who <paramref name="cries"/> out holding on, as a robbed person who catches the thief, calls the nearest at once.
        /// </summary>
        public bool ReportCrime(Vector3 at, Transform witness, bool wardenSaw, bool cries = false)
        {
            if (InFight || witness == null)
            {
                return false;
            }

            var warden = wardenSaw && Free(witness) ? witness : NearestFreeWarden(witness.position);
            if (warden == null)
            {
                Debug.Log("[Confront] a crime noticed: no free Warden to fetch.");
                return false;
            }

            var called = warden == witness || cries || SliceAlarm.Hears(witness.position, warden.position);
            var ticks = (called ? 0 : TicksToRun(witness.position, warden.position)) + TicksToRun(warden.position, at);
            Fetch(warden, at, ticks);
            if (warden != witness)
            {
                Say(witness, called ? "ui.crime.call" : "ui.crime.runs");
            }

            Debug.Log($"[Confront] a crime noticed: a Warden {(called ? "called" : "fetched")}, there in {ticks} ticks.");
            return true;
        }

        /// <summary>
        /// A resident who noticed a break-in ran out of its door at <paramref name="door"/> <paramref name="since"/> ticks
        /// ago, while the street was not loaded (S5c.5): the nearest free Warden comes after the resident's run to it, less
        /// the ticks gone by. False when none is free or a fight is on.
        /// </summary>
        public bool ReportCrimeFrom(Vector3 door, long since)
        {
            var warden = InFight ? null : NearestFreeWarden(door);
            if (warden == null)
            {
                return false;
            }

            var run = SliceAlarm.Hears(door, warden.position) ? 0 : TicksToRun(door, warden.position);
            var ticks = Mathf.Max(0, (int)(run + TicksToRun(warden.position, door) - since));
            Fetch(warden, door, ticks);
            Debug.Log($"[Confront] a resident's report: a Warden at the door in {ticks} ticks.");
            return true;
        }

        private Transform NearestFreeWarden(Vector3 to)
        {
            foreach (var body in PeopleAround.Wardens(to))
            {
                if (Free(body) && !_late.Exists(k => k.Body == body))
                {
                    return body;
                }
            }

            return null;
        }

        private void Fetch(Transform warden, Vector3 at, int ticks)
        {
            _late.RemoveAll(k => k.Body == warden);
            _late.Add(new Coming
            {
                Body = warden,
                Arrives = Time.time + (ticks * SecondsPerTick),
                To = at,
                OnlyLooks = true,
                Post = warden.position,
                PostFacing = warden.rotation,
            });
        }

        /// <summary>A Warden who stops the character on its way is on its way no more.</summary>
        public void CallOff(Transform warden) => _late.RemoveAll(k => k.Body == warden);
    }
}

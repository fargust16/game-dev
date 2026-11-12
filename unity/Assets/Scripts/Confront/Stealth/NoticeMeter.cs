using System;
using UnityEngine;

namespace GameDev.Confront.Stealth
{
    /// <summary>
    /// One observer's Notice Meter (10.3 §3): it fills while the character is in the observer's cone and in sight, and
    /// the observer has noticed the character when it is full. Outside every cone a meter that never filled fades in
    /// 8 s, and the observer goes nowhere; a meter that filled fades in 16 s while the observer goes to the last place
    /// it saw the character. From the notice the observer is Alert, and it stays Alert for 10 world ticks after its
    /// meter is empty; seeing the character again holds that time full. Plain C#, so tests drive it without a scene.
    /// </summary>
    public sealed class NoticeMeter
    {
        private readonly StealthRules _rules;
        private int _alertLeft;

        public NoticeMeter(StealthRules rules) => _rules = rules ?? throw new ArgumentNullException(nameof(rules));

        /// <summary>0 to 1; full is noticed.</summary>
        public float Fill { get; private set; }

        /// <summary>The meter filled, and it has not emptied since.</summary>
        public bool Noticed { get; private set; }

        /// <summary>The observer noticed the character, lost sight of it, and is going to look where it last saw it.</summary>
        public bool Searching => Noticed && !Seeing;

        /// <summary>The character is in the observer's cone and sight this frame.</summary>
        public bool Seeing { get; private set; }

        public bool Alert { get; private set; }

        /// <summary>Where the observer last saw the character, valid once it has noticed.</summary>
        public Vector3 LastSeen { get; private set; }

        /// <summary>
        /// The character in sight for <paramref name="seconds"/> at the given time to fill; true on the frame the meter
        /// becomes full, the moment the observer notices.
        /// </summary>
        public bool See(double timeToFill, float seconds, Vector3 where)
        {
            Seeing = true;
            var was = Noticed;
            if (Noticed)
            {
                Fill = 1f; // a noticed character in sight stays noticed
            }
            else
            {
                Fill = Mathf.Min(1f, Fill + (float)(seconds / Math.Max(1e-3, timeToFill)));
            }

            if (Fill >= 1f)
            {
                Noticed = true;
                Alert = true;
                _alertLeft = _rules.AlertTicks;
                LastSeen = where;
            }

            return Noticed && !was;
        }

        /// <summary>
        /// The observer sees the character standing in plain view: a meter that noticed it stays full and follows it,
        /// and any other does not move, since a standing character hides nothing.
        /// </summary>
        public void Watch(Vector3 where)
        {
            if (Noticed)
            {
                Seeing = true;
                Fill = 1f;
                LastSeen = where;
            }
            else
            {
                Seeing = false;
            }
        }

        /// <summary>The character out of the cone or out of sight for <paramref name="seconds"/>.</summary>
        public void Unseen(float seconds)
        {
            Seeing = false;
            if (Fill <= 0f)
            {
                return;
            }

            Fill = Mathf.Max(0f, Fill - (seconds / (Noticed ? _rules.SearchFadeSeconds : _rules.FadeSeconds)));
            if (Fill <= 0f)
            {
                Noticed = false;
            }
        }

        /// <summary>One world tick: an Alert observer with an empty meter counts down to calm.</summary>
        public void Tick()
        {
            if (!Alert)
            {
                return;
            }

            if (Fill > 0f)
            {
                _alertLeft = _rules.AlertTicks;
                return;
            }

            if (--_alertLeft <= 0)
            {
                Alert = false;
            }
        }

        /// <summary>10.2 §9: Alert now, as a searcher is, its count to calm started again.</summary>
        public void Alarm()
        {
            Alert = true;
            _alertLeft = _rules.AlertTicks;
        }

        /// <summary>Forget everything: a new scene or a new run.</summary>

        public void Reset()
        {
            Fill = 0f;
            Noticed = false;
            Seeing = false;
            Alert = false;
            _alertLeft = 0;
        }
    }
}

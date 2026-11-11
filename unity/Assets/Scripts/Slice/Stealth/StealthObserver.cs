using System.Collections.Generic;
using GameDev.Confront.Stealth;
using GameDev.Slice.People;
using UnityEngine;

namespace GameDev.Slice.Stealth
{
    /// <summary>
    /// Someone who can notice the character (10.3 §2–3, S3b.1): a Vision Cone along the way it faces and a Notice Meter,
    /// whether or not the player can see either. <see cref="SliceStealth"/> fills and fades the meter; this body acts on
    /// it: an observer that noticed the character stops and faces it, and one that lost it goes to look where it last
    /// saw it when the way there is clear, or turns to look from where it stands, then returns to its round.
    /// </summary>
    public sealed class StealthObserver : MonoBehaviour
    {
        private static readonly List<StealthObserver> Registered = new List<StealthObserver>();
        private static readonly int IdleState = Animator.StringToHash("Idle");
        private static readonly int WalkState = Animator.StringToHash("Walk");

        private const float Pace = 1.2f;        // m/s, the townsfolk's walk
        private const float Arrive = 0.8f;      // m from the last place, where it stops to look
        private const float TurnRate = 240f;    // degrees a second toward what it watches
        private const float LookRate = 50f;     // degrees a second looking round at the last place
        private const float KneeHeight = 0.5f;  // m, the height of the way checked for a wall

        [Tooltip("10.3 §3: the observer's Perception (townsfolk 2 and Wardens 4 are 10.3's examples; authoring defaults).")]
        [SerializeField] private int perception = 2;

        [Tooltip("What blocks the way to the last place: structure, not people.")]
        [SerializeField] private LayerMask walls = ~0;

        private TownsfolkWalker _walker;
        private Animator _animator;
        private bool _paused;
        private bool _walking;

        public static IReadOnlyList<StealthObserver> All => Registered;

        public int Perception
        {
            get => perception;
            set => perception = value;
        }

        public NoticeMeter Meter { get; private set; }

        /// <summary>The line it says over its head when it notices the character, and until when it shows.</summary>
        public string Line { get; private set; }

        public float LineUntil { get; private set; }

        /// <summary>Where its marks hang over its head.</summary>
        public Vector3 MarkPosition => transform.position + (Vector3.up * 2.15f);

        public void Bind(StealthRules rules)
        {
            if (Meter == null && rules != null)
            {
                Meter = new NoticeMeter(rules);
            }
        }

        public void Say(string line, float seconds)
        {
            Line = line;
            LineUntil = Time.time + seconds;
        }

        /// <summary>One frame of what it does about the meter, after <see cref="SliceStealth"/> has filled or faded it.</summary>
        public void Behave(Vector3 character, float seconds)
        {
            if (Meter == null)
            {
                return;
            }

            if (Meter.Noticed && Meter.Seeing)
            {
                Pause();
                Walking(false);
                Turn(character - transform.position, TurnRate * seconds);
                return;
            }

            if (Meter.Searching)
            {
                Pause();
                Seek(Meter.LastSeen, seconds);
                return;
            }

            Resume();
        }

        private void Seek(Vector3 place, float seconds)
        {
            var at = transform.position;
            var to = place - at;
            to.y = 0f;
            var knee = Vector3.up * KneeHeight;
            if (to.magnitude <= Arrive || Physics.Linecast(at + knee, place + knee, walls, QueryTriggerInteraction.Ignore))
            {
                // At the last place, or with a wall in the way: it looks round from where it stands.
                Walking(false);
                if (to.magnitude > Arrive)
                {
                    Turn(to, TurnRate * seconds);
                }
                else
                {
                    transform.Rotate(0f, LookRate * seconds, 0f);
                }

                return;
            }

            Walking(true);
            var step = Mathf.Min(Pace * seconds, to.magnitude);
            var next = at + (to.normalized * step);
            if (Physics.Raycast(next + (Vector3.up * 1.5f), Vector3.down, out var hit, 4f, walls, QueryTriggerInteraction.Ignore))
            {
                next.y = hit.point.y;
            }

            transform.position = next;
            Turn(to, TurnRate * seconds);
        }

        private void Turn(Vector3 toward, float degrees)
        {
            toward.y = 0f;
            if (toward.sqrMagnitude > 1e-4f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toward, Vector3.up), degrees);
            }
        }

        private void Pause()
        {
            if (_walker != null && _walker.enabled)
            {
                _walker.enabled = false;
                _paused = true;
            }
        }

        private void Resume()
        {
            if (!_paused)
            {
                return;
            }

            _paused = false;
            if (_walker != null)
            {
                _walker.enabled = true;
                Walking(true);
            }
        }

        private void Walking(bool walking)
        {
            if (_walking == walking || _animator == null)
            {
                return;
            }

            _walking = walking;
            var state = walking ? WalkState : IdleState;
            if (_animator.HasState(0, state))
            {
                _animator.CrossFade(state, 0.2f);
            }
        }

        private void OnEnable()
        {
            Registered.Add(this);
            _walker = GetComponent<TownsfolkWalker>();
            _animator = GetComponentInChildren<Animator>();
            _walking = _walker != null;
        }

        private void OnDisable()
        {
            Registered.Remove(this);
            Resume();
        }
    }
}

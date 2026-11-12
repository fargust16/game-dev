using System;
using System.Collections.Generic;
using GameDev.Input;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.World.Timekeeping;
using UnityEngine;

namespace GameDev.Slice
{
    /// <summary>
    /// One stroke of work by its animation (07.1 §8a, the owner's second direction of 2026-11-11): the clip plays to its key
    /// frame, the axe meeting the tree or the hands done at the ground, and there it stops for the Reaction Check, the world
    /// paused but for theft; after the check the clip plays to its end. <see cref="Again"/> asks for the next stroke, as at a
    /// tree, and <see cref="LeadIn"/> names clips played whole first, as the anvil's blows before the last.
    /// </summary>
    public sealed class WorkStroke
    {
        /// <summary>The work clip's state in the player's controller, one of <see cref="PlayerWork"/>'s.</summary>
        public string Clip;

        /// <summary>Clips played whole before the stroke, as the blows before the last.</summary>
        public string[] LeadIn = Array.Empty<string>();

        /// <summary>The clip after the key frame, as the hammer's rebound; null plays the rest of <see cref="Clip"/>.</summary>
        public string Then;

        /// <summary>The check's bar, asked for at the key frame.</summary>
        public Func<ReactionBar> Bar;

        public string CaptionKey;

        /// <summary>Whether the world pauses for the check: always, but for theft (07.1 §8a).</summary>
        public bool PauseWorld = true;

        /// <summary>
        /// Whether a move key leaves the work be: true for work whose ticks have passed already, at the anvil, over a
        /// carcass or a wound, which plays to its check and its end.
        /// </summary>
        public bool Steady;

        /// <summary>The check's result, given the moment the marker stops.</summary>
        public Action<ReactionResult> Settle;

        /// <summary>Asked when the stroke has played out: true begins the next, as at a tree or a vein.</summary>
        public Func<bool> Again;

        /// <summary>Asked every frame: true calls the work off, the bar and all, as when the person robbed walks on.</summary>
        public Func<bool> CalledOff;

        /// <summary>The work over: false when it played out, true when it was called off or the player broke it off.</summary>
        public Action<bool> Ended;

        /// <summary>What the character turns to, and how far from its pivot it stands (m, 0 or less to stay where it is).</summary>
        public Transform Face;

        public float StandOff;
    }

    /// <summary>
    /// The player's work by its animation (07.1 §8a): plays a <see cref="WorkStroke"/> on the player's own controller, holds
    /// the character while it works, freezes the clip at its key frame while <see cref="SliceReaction"/>'s bar shows, and
    /// gives the character back when the work is over. A move key breaks the work off between checks. The driver puts one
    /// beside the Reaction Check.
    /// </summary>
    public sealed class PlayerWork : MonoBehaviour
    {
        /// <summary>
        /// Each work clip's key frame, s into it (measured by <c>SliceClipSurvey</c>, `clip_survey.txt`, and on the armed
        /// blacksmith for the anvil; ANIMATION_PIPELINE_PLAN.md §8, §10): the axe's blade at its farthest, the pick's fist at
        /// its lowest, the hammer on the anvil's face at the end of its blow, the hips beginning to rise from the ground, the
        /// crouched hand at its farthest into the pocket.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, float> Keys = new Dictionary<string, float>
        {
            ["Chop"] = 0.53f,
            ["Mine"] = 0.40f,
            ["HammerDown"] = 0.37f,
            ["Harvest"] = 1.27f,
            ["Kneel"] = 4.30f,
            ["Gather2"] = 0.60f,
        };

        /// <summary>Every work state the player's controller carries: those with a key frame, and the hammer's rebound.</summary>
        public static readonly string[] Clips = { "Chop", "Mine", "HammerDown", "HammerUp", "Harvest", "Kneel", "Gather2" };

        /// <summary>The fade into a work clip and back to walking, s.</summary>
        public const float Fade = 0.2f;

        /// <summary>Authoring default, s: how long the character takes to turn to the work and step to it.</summary>
        public const float ApproachSeconds = 0.3f;

        private static readonly int LocomotionState = Animator.StringToHash("Locomotion");

        private enum Phase
        {
            None,
            Approach,
            LeadIn,
            ToKey,
            Check,
            Rest,
        }

        private SliceReaction _reaction;
        private IWorldClock _clock;
        private PlayerLocomotion _player;
        private Animator _animator;
        private WorkStroke _stroke;
        private Phase _phase;
        private int _lead;
        private int _playing;
        private float _base;
        private float _phaseTime;
        private bool _held;
        private Vector3 _approachFrom;
        private Vector3 _approachTo;
        private Quaternion _turnFrom;
        private Quaternion _turnTo;

        public void Configure(SliceReaction reaction, IWorldClock clock)
        {
            _reaction = reaction;
            _clock = clock;
        }

        public bool Busy => _phase != Phase.None;

        /// <summary>The stroke at work, or null.</summary>
        public WorkStroke Stroke => _stroke;

        /// <summary>Whether the clip waits at its key frame for the check.</summary>
        public bool AtKey => _phase == Phase.Check;

        /// <summary>Begins <paramref name="stroke"/>; false when the character is busy or has no animator to work with.</summary>
        public bool Begin(WorkStroke stroke)
        {
            if (Busy || stroke == null || _reaction == null || _reaction.Running || !Keys.ContainsKey(stroke.Clip))
            {
                return false;
            }

            _player = FindAnyObjectByType<PlayerLocomotion>();
            _animator = _player != null ? _player.GetComponentInChildren<Animator>() : null;
            if (_player == null || _animator == null || !_animator.HasState(0, Animator.StringToHash(stroke.Clip)))
            {
                return false;
            }

            _stroke = stroke;
            _held = _player.enabled;
            _player.enabled = false;
            _animator.speed = 1f;
            _phaseTime = 0f;
            _approachFrom = _approachTo = _player.transform.position;
            _turnFrom = _turnTo = _player.transform.rotation;
            if (stroke.Face != null)
            {
                var toward = stroke.Face.position - _player.transform.position;
                toward.y = 0f;
                if (toward.sqrMagnitude > 1e-4f)
                {
                    _turnTo = Quaternion.LookRotation(toward.normalized, Vector3.up);
                    if (stroke.StandOff > 0f)
                    {
                        var stand = stroke.Face.position - (toward.normalized * stroke.StandOff);
                        _approachTo = new Vector3(stand.x, _approachFrom.y, stand.z);
                    }
                }
            }

            _phase = Phase.Approach;
            return true;
        }

        /// <summary>Breaks the work off: the bar goes with no result, and the character walks again.</summary>
        public void Stop() => End(true);

        /// <summary>
        /// How far a thing reaches out from its pivot over the ground, m: its colliders' bounds, its renderers' where it has
        /// none, the nearer side held to 1.5 m, so the character stands at its edge to work it.
        /// </summary>
        public static float Radius(Transform thing)
        {
            var bounds = new Bounds(thing.position, Vector3.zero);
            var any = false;
            foreach (var c in thing.GetComponentsInChildren<Collider>())
            {
                if (c.enabled && !c.isTrigger)
                {
                    bounds = any ? Encapsulate(bounds, c.bounds) : c.bounds;
                    any = true;
                }
            }

            if (!any)
            {
                foreach (var r in thing.GetComponentsInChildren<Renderer>())
                {
                    bounds = any ? Encapsulate(bounds, r.bounds) : r.bounds;
                    any = true;
                }
            }

            return any ? Mathf.Min(Mathf.Max(bounds.extents.x, bounds.extents.z), 1.5f) : 0.3f;
        }

        private static Bounds Encapsulate(Bounds a, Bounds b)
        {
            a.Encapsulate(b);
            return a;
        }

        private void Update()
        {
            if (_phase == Phase.None)
            {
                return;
            }

            if (_player == null || _animator == null || (_stroke.CalledOff != null && _stroke.CalledOff()))
            {
                End(true);
                return;
            }

            var input = GameInputService.Instance;
            if (_phase != Phase.Check && !_stroke.Steady && input != null && input.MoveVector.sqrMagnitude > 0.25f)
            {
                End(true); // a step is the player's own choice and ends the work
                return;
            }

            _phaseTime += Time.deltaTime;
            switch (_phase)
            {
                case Phase.Approach:
                    Approach();
                    break;
                case Phase.LeadIn:
                    if (Played(_stroke.LeadIn[_lead], 1f))
                    {
                        _lead++;
                        Next();
                    }

                    break;
                case Phase.ToKey:
                    // A clip that does not loop stops at its end, so a key at the end is read there.
                    var key = Keys[_stroke.Clip] / Length(_stroke.Clip);
                    if (Played(_stroke.Clip, _base + (Loops(_stroke.Clip) ? key : Mathf.Min(key, 1f))))
                    {
                        Check();
                    }

                    break;
                case Phase.Rest:
                    var rest = _stroke.Then ?? _stroke.Clip;
                    if (Played(rest, _stroke.Then != null ? 1f : _base + 1f))
                    {
                        if (_stroke.Again != null && _stroke.Again())
                        {
                            _base = _stroke.Then == null && Loops(_stroke.Clip) ? _base + 1f : 0f;
                            if (_base == 0f)
                            {
                                Play(_stroke.Clip, _stroke.Then != null ? 0.05f : Fade);
                            }

                            _phase = Phase.ToKey;
                        }
                        else
                        {
                            End(false);
                        }
                    }

                    break;
            }
        }

        /// <summary>Turns the character to the work and steps it to its place, through a CharacterController where it has one.</summary>
        private void Approach()
        {
            var share = Mathf.Clamp01(_phaseTime / ApproachSeconds);
            var t = _player.transform;
            t.rotation = Quaternion.Slerp(_turnFrom, _turnTo, share);
            var want = Vector3.Lerp(_approachFrom, _approachTo, share);
            var controller = _player.GetComponent<CharacterController>();
            if (controller != null && controller.enabled)
            {
                var step = want - t.position;
                step.y = 0f;
                controller.Move(step);
            }
            else
            {
                t.position = new Vector3(want.x, t.position.y, want.z);
            }

            if (share >= 1f)
            {
                _lead = 0;
                Next();
            }
        }

        /// <summary>The next lead-in clip, or the stroke itself.</summary>
        private void Next()
        {
            if (_lead < _stroke.LeadIn.Length)
            {
                Play(_stroke.LeadIn[_lead], _lead == 0 ? Fade : 0.05f);
                _phase = Phase.LeadIn;
                return;
            }

            _base = 0f;
            Play(_stroke.Clip, _stroke.LeadIn.Length > 0 ? 0.05f : Fade);
            _phase = Phase.ToKey;
        }

        /// <summary>The clip stops at its key frame and the bar shows, the world paused but for theft.</summary>
        private void Check()
        {
            _animator.speed = 0f;
            _phase = Phase.Check;
            var bar = _stroke.Bar != null ? _stroke.Bar() : default;
            var hold = _stroke.PauseWorld ? _clock?.Pause() : null;
            if (!_reaction.Begin(bar, _stroke.CaptionKey, OnChecked, hold))
            {
                End(true);
            }
        }

        private void OnChecked(ReactionResult result)
        {
            if (_phase != Phase.Check)
            {
                return;
            }

            _animator.speed = 1f;
            _stroke.Settle?.Invoke(result);
            if (_phase != Phase.Check)
            {
                return; // the settling ended the work, as a spent node does
            }

            if (_stroke.Then != null)
            {
                Play(_stroke.Then, 0.05f);
            }

            _phase = Phase.Rest;
        }

        /// <summary>
        /// Whether the state <paramref name="clip"/> has played to <paramref name="normalized"/> of its length, a loop's
        /// cycles counted on.
        /// </summary>
        private bool Played(string clip, float normalized)
        {
            var hash = Animator.StringToHash(clip);
            if (_animator.IsInTransition(0))
            {
                var next = _animator.GetNextAnimatorStateInfo(0);
                return next.shortNameHash == hash && next.normalizedTime >= normalized - 0.005f;
            }

            var info = _animator.GetCurrentAnimatorStateInfo(0);
            return info.shortNameHash == hash && info.normalizedTime >= normalized - 0.005f;
        }

        private float Length(string clip)
        {
            var hash = Animator.StringToHash(clip);
            var info = _animator.IsInTransition(0) ? _animator.GetNextAnimatorStateInfo(0) : _animator.GetCurrentAnimatorStateInfo(0);
            return info.shortNameHash == hash && info.length > 0.01f ? info.length : Keys[clip] + 0.01f;
        }

        private bool Loops(string clip)
        {
            var hash = Animator.StringToHash(clip);
            var info = _animator.GetCurrentAnimatorStateInfo(0);
            return info.shortNameHash == hash && info.loop;
        }

        private void Play(string clip, float fade)
        {
            _playing = Animator.StringToHash(clip);
            _animator.CrossFadeInFixedTime(_playing, fade, 0, 0f);
        }

        private void End(bool broken)
        {
            if (_phase == Phase.None)
            {
                return;
            }

            var stroke = _stroke;
            var atKey = _phase == Phase.Check;
            _phase = Phase.None;
            _stroke = null;
            if (atKey && _reaction != null)
            {
                _reaction.Cancel();
            }

            if (_animator != null)
            {
                _animator.speed = 1f;
                if (_animator.isActiveAndEnabled && _animator.HasState(0, LocomotionState))
                {
                    _animator.CrossFadeInFixedTime(LocomotionState, Fade, 0);
                }
            }

            if (_player != null && _held)
            {
                // A fight begun meanwhile holds the character itself and frees it when it ends.
                var fights = SliceWorldDriver.Running != null ? SliceWorldDriver.Running.Fights : null;
                _player.enabled = fights == null || !fights.InFight;
            }

            _held = false;
            stroke?.Ended?.Invoke(broken);
        }

        private void OnDisable() => End(true);
    }
}

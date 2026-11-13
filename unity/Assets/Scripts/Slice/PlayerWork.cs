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
        /// Each work clip's key frame, s into it (ANIMATION_PIPELINE_PLAN.md §8, §10). A blow stops just before it lands,
        /// at the right hand's fastest (owner, 2026-11-12: the check came after the axe had begun to swing back; measured
        /// by <c>SliceTools.Measure</c>, `tools.txt`): the axe's at 0.17 s, the hand falling through 1.38 m to strike at
        /// 0.18 s (0.13 s came too early, the owner's play test of the same day); the pick's at 0.35 s, through 1.3 m to strike the ground at 0.40 s; the hammer's at 0.35 s, through
        /// 1.25 m to meet the anvil at 0.38 s. The rest by <c>SliceClipSurvey</c>, `clip_survey.txt`: the hips beginning to
        /// rise from the ground, the crouched hand at its farthest into the pocket.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, float> Keys = new Dictionary<string, float>
        {
            ["Chop"] = 0.17f,
            ["Mine"] = 0.35f,
            ["HammerDown"] = 0.35f,
            ["Harvest"] = 1.27f,
            ["Kneel"] = 4.30f,
            ["Gather2"] = 0.60f,

            // Brewing (S5b.4): Q1 Interact, the left hand furthest out, 0.77 of 2.00 s (measured in AN3).
            ["Interact"] = 0.77f,
        };

        /// <summary>
        /// Where a looping work clip's first stroke starts, s into it, when its blow comes too early to be seen after the
        /// fade: the axe's swing begins its clip, so the first stroke starts at the raised axe (0.6 s, measured: the hand
        /// rising through 1.49 m) and its check comes in the clip's second cycle.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, float> Starts = new Dictionary<string, float>
        {
            ["Chop"] = 0.6f,
        };

        /// <summary>
        /// A work clip's key frame, s into it: the owner's tuning page's (<see cref="SliceTuning"/>, owner 2026-11-12: the
        /// check came too early at the axe's 0.13 s, and the owner sets it there), or <see cref="Keys"/>' default.
        /// </summary>
        public static float KeyOf(string clip)
        {
            var timing = SliceTuning.Current.Work(clip);
            return timing != null ? timing.CheckAtSeconds : Keys.TryGetValue(clip, out var key) ? key : 0f;
        }

        /// <summary>Where a work clip's first stroke starts, s into it: the tuning page's, or <see cref="Starts"/>' default.</summary>
        public static float StartOf(string clip)
        {
            var timing = SliceTuning.Current.Work(clip);
            return timing != null ? timing.FirstStrokeFromSeconds : Starts.TryGetValue(clip, out var start) ? start : 0f;
        }

        /// <summary>Each tool's grip as <c>SliceTools.Arm</c> set it in the hand, read the first time the tool is out.</summary>
        private readonly Dictionary<GameObject, (Vector3 Position, Quaternion Rotation)> _baked = new Dictionary<GameObject, (Vector3, Quaternion)>();

        /// <summary>Every work state the player's controller carries: those with a key frame, and the hammer's rebound.</summary>
        public static readonly string[] Clips = { "Chop", "Mine", "HammerDown", "HammerUp", "Harvest", "Kneel", "Gather2", "Interact" };

        /// <summary>The tools in the player's right hand, switched off but while the work they are for lasts (<c>SliceTools.Arm</c>).</summary>
        public const string AxeTool = "Tool_Axe";

        public const string PickaxeTool = "Tool_Pickaxe";

        public const string HammerTool = "Tool_Hammer";

        /// <summary>The tool each work clip is played with: the axe at a tree, the pick at a vein, the hammer at the anvil.</summary>
        public static readonly IReadOnlyDictionary<string, string> ToolOf = new Dictionary<string, string>
        {
            ["Chop"] = AxeTool,
            ["Mine"] = PickaxeTool,
            ["HammerDown"] = HammerTool,
        };

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
        private GameObject _tool;

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
            _tool = ToolOf.TryGetValue(stroke.Clip, out var tool) ? Tool(_player.transform, tool) : null;
            if (_tool != null)
            {
                _tool.SetActive(true);
                Grip();
            }

            return true;
        }

        /// <summary>The tool <paramref name="name"/> in the character's hand, switched on or off, or null.</summary>
        public static GameObject Tool(Transform character, string name)
        {
            foreach (var t in character.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                {
                    return t.gameObject;
                }
            }

            return null;
        }

        /// <summary>
        /// The tool in hand at the grip <c>SliceTools.Arm</c> set, with the tuning page's correction on it: shifted in the
        /// hand's axes, turned about its own (owner, 2026-11-12: the axe was held wrong, and the owner sets the grip).
        /// </summary>
        private void Grip()
        {
            var grip = _tool != null ? SliceTuning.Current.Grip(_tool.name) : null;
            if (grip == null)
            {
                return;
            }

            var t = _tool.transform;
            if (!_baked.TryGetValue(_tool, out var baked))
            {
                baked = (t.localPosition, t.localRotation);
                _baked[_tool] = baked;
            }

            t.localPosition = baked.Position + grip.ShiftInHandMetres;
            t.localRotation = baked.Rotation * Quaternion.Euler(grip.TurnDegrees);
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

            Grip(); // each frame, so the owner's turn of the grip shows while the work plays
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
                    var key = KeyOf(_stroke.Clip) / Length(_stroke.Clip);
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

            var start = StartOf(_stroke.Clip);
            _base = start > KeyOf(_stroke.Clip) ? 1f : 0f;
            Play(_stroke.Clip, _stroke.LeadIn.Length > 0 ? 0.05f : Fade, start);
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
            return info.shortNameHash == hash && info.length > 0.01f ? info.length : KeyOf(clip) + 0.01f;
        }

        private bool Loops(string clip)
        {
            var hash = Animator.StringToHash(clip);
            var info = _animator.GetCurrentAnimatorStateInfo(0);
            return info.shortNameHash == hash && info.loop;
        }

        private void Play(string clip, float fade, float start = 0f)
        {
            _playing = Animator.StringToHash(clip);
            _animator.CrossFadeInFixedTime(_playing, fade, 0, start);
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
            if (_tool != null)
            {
                _tool.SetActive(false);
                _tool = null;
            }
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

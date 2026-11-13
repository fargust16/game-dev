using GameDev.Confront.Turns;
using GameDev.Slice.Kit;
using UnityEngine;

namespace GameDev.Slice.Beasts
{
    /// <summary>Where a beast stands toward the character during the run (S3b.2).</summary>
    public enum BeastState
    {
        /// <summary>At rest in its territory.</summary>
        Resting,

        /// <summary>Gone from the run: a wolf whose pack broke, or one of a pack that shuns a glowing wolf.</summary>
        Gone,

        /// <summary>Fallen; its carcass lies where it fell.</summary>
        Dead,
    }

    /// <summary>
    /// One beast of the slice in the scene (10.2 *Beasts of the slice*, S3b.2): its kind, its pack (one territory) and
    /// the territory's centre, placed by <c>SliceBeastsSetup</c>. It plays its own clips from the beast controllers the
    /// setup builds (Idle, Walk, Run, Attack, Hit, Dead, Warn): its walk and run from how fast it moves, the rest when told.
    /// It glows when the seed makes it a glowing beast. <see cref="SliceBeasts"/> decides what it does.
    /// </summary>
    public sealed class SliceBeast : MonoBehaviour, GameDev.Core.Save.ISavedObject
    {
        private const float WalkAbove = 0.25f;  // m/s
        private const float RunAbove = 2.6f;    // m/s
        private const float Fade = 0.15f;

        private static readonly int Idle = Animator.StringToHash("Idle");
        private static readonly int Walk = Animator.StringToHash("Walk");
        private static readonly int Run = Animator.StringToHash("Run");
        private static readonly int DeadState = Animator.StringToHash("Dead");

        [SerializeField] private string id;
        [SerializeField] private BeastKind kind;
        [SerializeField] private int pack;
        [SerializeField] private Vector3 home;

        private Animator _animator;
        private Vector3 _last;
        private int _playing;
        private float _oneShotUntil;
        private bool _dead;
        private Vector3 _goal;
        private float _goalSpeed;
        private bool _going;
        private bool _vanish;
        private int _ground = ~0;

        /// <summary>A name that is the same in every run, such as <c>wolf.1.a</c>, for the world state's keys.</summary>
        public string Id => id;

        public BeastKind Kind => kind;

        /// <summary>Its territory's number: the wolves' packs first, then the boars.</summary>
        public int Pack => pack;

        /// <summary>The centre of its territory.</summary>
        public Vector3 Home => home;

        /// <summary>Where it rests in its territory: where it was placed.</summary>
        public Vector3 Spot { get; private set; }

        public bool Glowing { get; private set; }

        public BeastState State { get; set; }

        /// <summary>The HP an earlier fight left it, or NaN while it is whole.</summary>
        public double Hp { get; set; } = double.NaN;

        /// <summary>Its combatant in the fight under way, or null.</summary>
        public Combatant Combatant { get; set; }

        public bool Moving => _going;

        public void Configure(string beastId, BeastKind beastKind, int beastPack, Vector3 territory)
        {
            id = beastId;
            kind = beastKind;
            pack = beastPack;
            home = territory;
        }

        /// <summary>10.2: the glow shows from afar, a pale light on the beast and its fur lit from within.</summary>
        public void Glow(Color colour, float light, float range, float emission)
        {
            if (Glowing)
            {
                return;
            }

            Glowing = true;
            var lamp = new GameObject("Glow").AddComponent<Light>();
            lamp.transform.SetParent(transform, false);
            lamp.transform.localPosition = Vector3.up * 1.6f / Mathf.Max(0.01f, transform.lossyScale.y);
            lamp.type = LightType.Point;
            lamp.color = colour;
            lamp.intensity = light;
            lamp.range = range;
            lamp.shadows = LightShadows.None;
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                foreach (var m in r.materials)
                {
                    if (m.HasProperty("_EmissionColor"))
                    {
                        m.EnableKeyword("_EMISSION");
                        m.SetColor("_EmissionColor", colour * emission);
                        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    }
                }
            }
        }

        /// <summary>Plays one of its clips for a while (Attack, Hit, Warn), then goes back to its walk or rest.</summary>
        public void Act(string state, float seconds)
        {
            if (_dead || _animator == null)
            {
                return;
            }

            var hash = Animator.StringToHash(state);
            if (_animator.HasState(0, hash))
            {
                _animator.CrossFadeInFixedTime(hash, Fade, 0, 0f);
                _playing = hash;
                _oneShotUntil = Time.time + seconds;
            }
        }

        /// <summary>It falls, or lies already fallen (<paramref name="atOnce"/>), and stays down.</summary>
        public void Die(bool atOnce)
        {
            _dead = true;
            _going = false;
            State = BeastState.Dead;
            if (TryGetComponent<BoxCollider>(out var body))
            {
                body.enabled = false;
            }
            if (_animator != null && _animator.HasState(0, DeadState))
            {
                if (atOnce)
                {
                    _animator.Play(DeadState, 0, 1f);
                }
                else
                {
                    _animator.CrossFadeInFixedTime(DeadState, Fade);
                }

                _playing = DeadState;
            }
        }

        /// <summary>Goes to a place over the ground at a speed; vanishes there when told, for a beast leaving the run.</summary>
        public void Go(Vector3 to, float speed, bool vanish)
        {
            _goal = to;
            _goalSpeed = speed;
            _going = true;
            _vanish = vanish;
        }

        public void Stop() => _going = false;

        /// <summary>Turns toward a place at so many degrees.</summary>
        public void Face(Vector3 place, float degrees)
        {
            var toward = place - transform.position;
            toward.y = 0f;
            if (toward.sqrMagnitude > 1e-4f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toward, Vector3.up), degrees);
            }
        }

        [System.Serializable]
        private sealed class Saved
        {
            public Vector3 at;
            public float yaw;
            public bool wounded;
            public double hp;
            public bool going;
            public Vector3 goal;
            public float goalSpeed;
            public bool vanish;
        }

        /// <summary>A Suspend Save's id (17 X.3): its scene and its run-long name.</summary>
        public string SaveId => gameObject.scene.name + "/beast:" + id;

        /// <summary>
        /// Where it stands or lies and faces, the HP a fight left it and the walk it is on; whether it is dead or gone the
        /// world state keeps (17 X.3).
        /// </summary>
        public string CaptureState() => JsonUtility.ToJson(new Saved
        {
            at = transform.position,
            yaw = transform.eulerAngles.y,
            wounded = !double.IsNaN(Hp),
            hp = double.IsNaN(Hp) ? 0d : Hp,
            going = _going,
            goal = _goal,
            goalSpeed = _goalSpeed,
            vanish = _vanish,
        });

        public void ClearState()
        {
        }

        public void RestoreState(string payload)
        {
            var s = JsonUtility.FromJson<Saved>(payload);
            transform.SetPositionAndRotation(s.at, Quaternion.Euler(0f, s.yaw, 0f));
            _last = s.at;
            Hp = s.wounded ? s.hp : double.NaN;
            _going = s.going && !_dead;
            _goal = s.goal;
            _goalSpeed = s.goalSpeed;
            _vanish = s.vanish;
        }

        private void Awake()
        {
            Spot = transform.position;
            _last = transform.position;
            _animator = GetComponentInChildren<Animator>();
            if (_animator != null)
            {
                _animator.applyRootMotion = false;
            }

            _ground = ~LayerMask.GetMask(KitLayers.Character, KitLayers.NoGrid);
            Body();
        }

        /// <summary>
        /// 10.2 §5e: a shot out of a fight touches what it flies into, so a beast has a body to touch, as a person has: a
        /// box round its model as it stands, on the characters' layer, which neither the ground under a beast nor the
        /// Combat Grid reads. A fallen beast's body stops nothing.
        /// </summary>
        private void Body()
        {
            if (GetComponent<Collider>() != null)
            {
                return;
            }

            var turn = transform.rotation;
            transform.rotation = Quaternion.identity;
            var seen = default(Bounds);
            var any = false;
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer)
                {
                    continue;
                }

                if (any)
                {
                    seen.Encapsulate(r.bounds);
                }
                else
                {
                    seen = r.bounds;
                    any = true;
                }
            }

            if (any)
            {
                var scale = transform.lossyScale;
                var box = gameObject.AddComponent<BoxCollider>();
                box.center = transform.InverseTransformPoint(seen.center);
                box.size = new Vector3(seen.size.x / Mathf.Max(1e-4f, scale.x), seen.size.y / Mathf.Max(1e-4f, scale.y), seen.size.z / Mathf.Max(1e-4f, scale.z));
            }

            transform.rotation = turn;
        }

        private void Update()
        {
            // A paused world holds a beast mid-stride, as it holds the townsfolk.
            var clock = GameDev.World.Timekeeping.GameClockDriver.Running;
            if (!_going || _dead || (clock != null && clock.IsPaused))
            {
                return;
            }

            var at = transform.position;
            var to = _goal - at;
            to.y = 0f;
            var step = _goalSpeed * Time.deltaTime;
            if (to.magnitude <= Mathf.Max(step, 0.05f))
            {
                _going = false;
                if (_vanish)
                {
                    gameObject.SetActive(false);
                }

                return;
            }

            var next = at + (to.normalized * step);
            if (Physics.Raycast(next + (Vector3.up * 2f), Vector3.down, out var hit, 6f, _ground, QueryTriggerInteraction.Ignore))
            {
                next.y = hit.point.y;
            }

            transform.SetPositionAndRotation(next, Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to, Vector3.up), 360f * Time.deltaTime));
        }

        /// <summary>Its walk, run or rest from how fast it moved this frame, unless a clip it was told to play still runs.</summary>
        private void LateUpdate()
        {
            var moved = transform.position - _last;
            moved.y = 0f;
            _last = transform.position;
            if (_animator == null || _dead || Time.time < _oneShotUntil || Time.deltaTime <= 0f)
            {
                return;
            }

            var speed = moved.magnitude / Time.deltaTime;
            var want = speed > RunAbove ? Run : speed > WalkAbove ? Walk : Idle;
            if (want != _playing && _animator.HasState(0, want))
            {
                _animator.CrossFadeInFixedTime(want, Fade);
                _playing = want;
            }
        }
    }
}

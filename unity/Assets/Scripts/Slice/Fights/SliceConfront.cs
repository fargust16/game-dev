using System.Collections.Generic;
using GameDev.Confront;
using GameDev.Confront.Grid;
using GameDev.Confront.Rules;
using GameDev.Confront.Turns;
using GameDev.Core.Localization;
using GameDev.Input;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.Kit;
using GameDev.UI;
using GameDev.World.Seed;
using GameDev.World.Timekeeping;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// The slice's fights in the scene (S3a.4, S3a.5): the Encounter Lock and the turns on a Combat Grid round the player,
    /// the grid drawn with a short sound as it forms, free movement held until the fight ends, and fighters who come
    /// within the grid joining at the next round while those who fall out of it drop out. The player's turn is played
    /// by the cursor under the fight camera, with the order, HP and AP on the HUD; the turns resolve at once and the
    /// bodies show them beat by beat (<see cref="FightBeats"/>), the next turn waiting for the last beat. F7 starts a
    /// development fight.
    /// </summary>
    public sealed class SliceConfront : MonoBehaviour
    {
        public const int PlayerSide = 0;
        public const int HostileSide = 1;
        public const int PlayerId = 0;

        // Authoring defaults, S3a.4.
        private const int OpponentDistance = 4;   // cells from the player to the development opponents
        private const int Opponents = 2;
        private const float OpponentHeight = 1.8f; // m, a person (kit standard §3)

        // The fight clips' parameters (ANIMATION_PIPELINE_PLAN.md A2), on the controllers SliceMixamo builds.
        private static readonly int FightingHash = Animator.StringToHash("Fighting");
        private static readonly int WeaponHash = Animator.StringToHash("Weapon");
        private static readonly int DownHash = Animator.StringToHash("Down");

        // Basic RPG Cursors' points, pixels from the top left of their 64-pixel images (measured on the images): the
        // arrows' tips, and the middle of the four arrows and of the sword.
        private static readonly Vector2[] CursorPoints = { new Vector2(20f, 5f), new Vector2(32f, 32f), new Vector2(32f, 32f), new Vector2(20f, 5f), new Vector2(32f, 32f) };

        // Authoring default, S3a.5: the outlines of hidden houses, a pale line apart from the grid's.
        private static readonly Color OutlineColour = new Color(0.92f, 0.9f, 0.84f);

        private readonly Dictionary<Combatant, Transform> _bodies = new Dictionary<Combatant, Transform>();
        private readonly Dictionary<Combatant, string> _names = new Dictionary<Combatant, string>();
        private readonly Dictionary<Transform, Animator> _animated = new Dictionary<Transform, Animator>();
        private readonly List<GameObject> _developmentOpponents = new List<GameObject>();
        private readonly List<CombatCell> _ring = new List<CombatCell>();
        private readonly List<Vector3> _points = new List<Vector3>();
        private SliceSettings _settings;
        private IWorldSeed _seed;
        private CharacterSheet _character;
        private CombatGridReader _grid;
        private TurnEconomy _turns;
        private EncounterLock _lock;
        private CombatGridView _view;
        private FightBeats _beats;
        private FightCamera _camera;
        private FightCursor _cursor;
        private FightHud _hud;
        private Material _outline;
        private AudioSource _audio;
        private AudioClip _formed;
        private PlayerLocomotion _player;
        private Combatant _playerCombatant;
        private int _fights;
        private int _turnAp;
        private float _wait;
        private bool _ending;

        /// <summary>Seconds between turns played without the player's will, so a fight can be followed (authoring default).</summary>
        public float TurnPause { get; set; } = 0.35f;

        /// <summary>Development and tests: the player's character fights by the rule of the others, as before S3a.5.</summary>
        public bool AutoPlayer { get; set; }

        /// <summary>Speeds the bodies' beats up, for tests.</summary>
        public float BeatPace
        {
            get => _beats?.Pace ?? 1f;
            set
            {
                if (_beats != null)
                {
                    _beats.Pace = value;
                }
            }
        }

        /// <summary>Raised as a blow is seen to land, the target's hit or death played: the attacker, the target and what it did.</summary>
        public event System.Action<Combatant, Combatant, StrikeResult> BlowShown;

        public IEncounterLock Lock => _lock;

        public ITurnEconomy Turns => _turns;

        public ICombatGrid Grid => _grid;

        public bool GridShown => _view != null && _view.Shown;

        /// <summary>True from the fight's start until its last beat has played and free movement is back.</summary>
        public bool InFight => (_lock != null && _lock.InConfront) || _ending;

        /// <summary>True while the bodies still show turns already resolved.</summary>
        public bool Showing => _beats != null && _beats.Busy;

        /// <summary>The player's cursor in a fight.</summary>
        public FightCursor PlayerCursor => _cursor;

        public FightHud Hud => _hud;

        public FightCamera FightCamera => _camera;

        public FightPathView PathView { get; private set; }

        /// <summary>The player's combatant in the fight under way, or null.</summary>
        public Combatant PlayerCombatant => _playerCombatant;

        public void Configure(SliceSettings settings, IWorldSeed seed, IWorldClock clock, CharacterSheet character)
        {
            _settings = settings;
            _seed = seed;
            _character = character;
            _grid = new CombatGridReader(1f);
            _turns = new TurnEconomy(settings.FightRules, _grid);
            _lock = new EncounterLock(_turns, _grid, clock);
            _lock.ConfrontStarted += OnStarted;
            _lock.ConfrontEnded += OnEnded;
            _turns.RoundEnded += OnRoundEnded;
            _turns.Struck += OnStruck;
            _turns.Walked += OnWalked;
            _turns.TurnAdvanced += OnTurnAdvanced;
            _view = CombatGridView.Create(transform, settings.GridMaterial);
            _beats = new FightBeats(Animated, Place);
            _beats.Landed += OnLanded;
            _outline = new Material(settings.GridMaterial) { name = "Fight Outlines" };
            _outline.SetColor("_BaseColor", OutlineColour);
            _camera = FightCamera.Create(transform, _outline);
            PathView = FightPathView.Create(transform, settings.GridMaterial);
            _cursor = new FightCursor(_turns, _grid, settings.FightRules, PathView, settings.FightCursors, CursorPoints);
            _hud = new FightHud(Name, settings.SocialChecks);
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            _formed = Formed();
        }

        /// <summary>
        /// Development command (S3a.4): two opponents with daggers four cells from the player, and a fight. The player's
        /// character takes the development kit for it, a sword, leather, and the rifle as its second set.
        /// </summary>
        public bool StartDevelopmentFight()
        {
            _player = FindAnyObjectByType<PlayerLocomotion>();
            if (_lock == null || InFight || _player == null || _settings.OpponentModel == null)
            {
                return false;
            }

            var at = _player.transform.position;
            _grid.Follow(at);
            if (!_grid.TryCellAt(at, out var home) || !Places(home, at.y))
            {
                return false;
            }

            _fights++;
            _bodies.Clear();
            _names.Clear();
            var rules = _settings.FightRules;
            var player = new Combatant(PlayerId, PlayerSide,
                rules.Fighter(_character, null, new WeaponSet(_settings.KitSword), _settings.KitArmour, new WeaponSet(_settings.KitRifle, _settings.KitPouch)), at,
                takesIntents: !AutoPlayer);
            var combatants = new List<Combatant> { player };
            _bodies[player] = _player.transform;
            _names[player] = "ui.fight.you";
            _playerCombatant = player;
            for (var i = 0; i < Opponents; i++)
            {
                var body = Opponent(_grid.Center(_ring[i]), at, i + 1);
                var opponent = new Combatant(i + 1, HostileSide, rules.Fighter(new CharacterSheet(), null, new WeaponSet(_settings.OpponentWeapon)), body.position);
                body.GetComponent<SliceFighter>().Combatant = opponent;
                _bodies[opponent] = body;
                _names[opponent] = "ui.fight.cultist";
                combatants.Add(opponent);
            }

            return _lock.TryBegin(new EncounterRequest(at, combatants, _seed.Fork(SliceSeedStreams.Combat, _fights), "development command"));
        }

        private void Update()
        {
            if (_lock == null)
            {
                return;
            }

            if (_ending)
            {
                _beats.Tick(Time.deltaTime);
                if (!_beats.Busy)
                {
                    Finish();
                }

                return;
            }

            var input = GameInputService.Instance;
            if (!_lock.InConfront)
            {
                if (input != null && input.DevFightPressed && Debug.isDebugBuild)
                {
                    StartDevelopmentFight();
                }

                return;
            }

            _beats.Tick(Time.deltaTime);
            var settled = !_beats.Busy;
            var acting = _turns.Acting;
            if (settled && acting != null && !acting.TakesIntents)
            {
                _wait -= Time.deltaTime;
                if (_wait <= 0f)
                {
                    _wait = TurnPause;
                    _turns.PlayTurn();
                }
            }
            else if (settled && acting != null && input != null)
            {
                PlayerTurn(acting, input);
            }
            else if (!settled)
            {
                _cursor.Clear();
            }

            if (!_beats.Busy && _lock.InConfront)
            {
                Mirror();
            }

            _hud.Refresh(_turns, _playerCombatant, _cursor, _turnAp, input != null ? input.MousePosition : Vector2.zero, !_beats.Busy);
        }

        /// <summary>The player's turn by the cursor: a click walks or strikes, Space ends the turn, and so do AP spent.</summary>
        private void PlayerTurn(Combatant player, GameInputService input)
        {
            _cursor.Point(player, _camera.View != null ? _camera.View : Camera.main, input.MousePosition);
            if (input.ClickMovePressed && _cursor.Click(player))
            {
                _hud.MarkDirty();
                return;
            }

            if (input.EndTurnPressed || _turns.ApLeft <= 0)
            {
                _cursor.Clear();
                _turns.SubmitIntent(TurnIntent.EndTurn);
            }
        }

        /// <summary>Sets every body where its combatant stands; the grid follows the player, and who falls outside it drops out.</summary>
        private void Mirror()
        {
            foreach (var pair in _bodies)
            {
                var body = pair.Value;
                if (body == null || (body.position - pair.Key.Position).sqrMagnitude < 1e-4f)
                {
                    continue;
                }

                Place(body, pair.Key.Position, body.rotation);
            }

            var playerBody = _player != null ? _player.transform : null;
            foreach (var pair in _bodies)
            {
                // A fallen body with no death clip lies down; one with a clip plays it (FightBeats).
                if (pair.Key.Fighter.Down && pair.Value != null && pair.Value != playerBody && Animated(pair.Value) == null
                    && Vector3.Dot(pair.Value.up, Vector3.up) > 0.5f)
                {
                    pair.Value.rotation = Quaternion.LookRotation(Vector3.up, pair.Value.forward);
                }
            }

            if (_player != null && _turns.IsRunning && _grid.Follow(_player.transform.position))
            {
                _view.Show(_grid);
                foreach (var c in _bodies.Keys)
                {
                    if (c.State == CombatantState.Fighting && !_grid.InBounds(c.Cell.X, c.Cell.Z))
                    {
                        _turns.Leave(c);
                        break; // the set may change; the rest are seen on the next frame
                    }
                }
            }
        }

        /// <summary>Sets a body where it stands and how it faces; the player's through its controller.</summary>
        private void Place(Transform body, Vector3 position, Quaternion rotation)
        {
            if (_player != null && body == _player.transform)
            {
                _player.Teleport(position, rotation);
            }
            else
            {
                body.SetPositionAndRotation(position, rotation);
            }
        }

        private void OnStarted(EncounterRequest request)
        {
            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerLocomotion>();
            }

            if (_player != null)
            {
                _player.enabled = false;
                _camera.Begin(_player.transform);
            }

            _view.Show(_grid);
            _audio.PlayOneShot(_formed);
            _wait = TurnPause;
            _beats.Clear();
            _hud.Show(true);
            _cursor.Clear();
            foreach (var pair in _bodies)
            {
                Guard(pair.Key, pair.Value, true);
            }

            Debug.Log($"[Confront] fight {_fights} begins ({request.Reason}): {request.Combatants.Count} combatants, {_view.Cells} cells.");
        }

        private void OnTurnAdvanced(Combatant acting)
        {
            if (acting == _playerCombatant)
            {
                _turnAp = _turns.ApLeft;
            }

            _hud.MarkDirty();
        }

        private void OnRoundEnded(int round)
        {
            if (!_turns.IsRunning)
            {
                return;
            }

            foreach (var fighter in SliceFighter.All)
            {
                var c = fighter.Combatant;
                if (c != null && c.State != CombatantState.Fighting && !c.Fighter.Down && _grid.TryCellAt(fighter.transform.position, out _))
                {
                    c.Position = fighter.transform.position;
                    _bodies[c] = fighter.transform;
                    Guard(c, fighter.transform, true);
                    _turns.Join(c);
                }
            }
        }

        /// <summary>The fight is decided; its last beats play out before free movement returns (<see cref="Finish"/>).</summary>
        private void OnEnded(int side)
        {
            Debug.Log($"[Confront] fight {_fights} ends after {_turns.Round} rounds; side {side} stands.");
            _ending = true;
            _cursor.Release();
        }

        private void Finish()
        {
            _ending = false;
            _view.Hide();
            _camera.End();
            _cursor.Release();
            _hud.Show(false);
            if (_player != null)
            {
                _player.enabled = true;
            }

            foreach (var pair in _bodies)
            {
                Guard(pair.Key, pair.Value, false);
            }

            _animated.Clear();
            foreach (var opponent in _developmentOpponents)
            {
                if (opponent != null)
                {
                    Destroy(opponent);
                }
            }

            _developmentOpponents.Clear();
            _bodies.Clear();
            _names.Clear();
            _playerCombatant = null;
        }

        /// <summary>
        /// A body into its guard by its weapon, or out of it, where its controller has the fight clips; one that is down
        /// lies in its death. A body without them is noted as such, and Mirror lays it down when it falls.
        /// </summary>
        private void Guard(Combatant c, Transform body, bool fighting)
        {
            var animator = Animated(body);
            if (animator == null)
            {
                return;
            }

            animator.SetFloat(WeaponHash, Motion(c.Fighter.Weapon));
            animator.SetBool(DownHash, c.Fighter.Down);
            animator.SetBool(FightingHash, fighting);
        }

        /// <summary>The walk is shown cell by cell, from where the body stands.</summary>
        private void OnWalked(Combatant c, IReadOnlyList<CombatCell> cells)
        {
            if (!_bodies.TryGetValue(c, out var body) || body == null)
            {
                return;
            }

            _points.Clear();
            foreach (var cell in cells)
            {
                _points.Add(_grid.Center(cell));
            }

            _beats.Walk(body, _points);
        }

        /// <summary>The blow is shown as a beat: the attacker turns and strikes, the target is hit or falls at the impact.</summary>
        private void OnStruck(Combatant attacker, Combatant target, StrikeResult result)
        {
            if (!_bodies.TryGetValue(attacker, out var body) || body == null)
            {
                return;
            }

            _bodies.TryGetValue(target, out var targetBody);
            _beats.Strike(body, targetBody, Motion(attacker.Fighter.Weapon), result, attacker, target);
        }

        private void OnLanded(FightBeats.Beat beat)
        {
            var attacker = beat.AttackerKey as Combatant;
            var target = beat.TargetKey as Combatant;
            _hud.Log(attacker, target, beat.Result);
            BlowShown?.Invoke(attacker, target, beat.Result);
        }

        /// <summary>A combatant's name in the language shown: the player, the development opponents, or a named person.</summary>
        private string Name(Combatant c)
        {
            if (c == null)
            {
                return string.Empty;
            }

            if (_names.TryGetValue(c, out var key))
            {
                return Loc.Get(key);
            }

            var tag = _bodies.TryGetValue(c, out var body) && body != null ? body.GetComponentInParent<INameTag>() : null;
            return tag != null ? tag.DisplayName : Loc.Get("ui.fight.someone");
        }

        /// <summary>The body's Animator if its controller carries the fight clips' parameters, else null; looked up once a fight.</summary>
        private Animator Animated(Transform body)
        {
            if (body == null)
            {
                return null;
            }

            if (!_animated.TryGetValue(body, out var animator))
            {
                animator = body.GetComponentInChildren<Animator>();
                if (animator == null || animator.runtimeAnimatorController == null || !System.Array.Exists(animator.parameters, p => p.nameHash == DownHash))
                {
                    animator = null;
                }

                _animated[body] = animator;
            }

            return animator;
        }

        /// <summary>
        /// The weapon's clips, 1 to 5 on the controllers' Weapon blend: sword, axe or mace (with a shield's guard),
        /// greatsword, greataxe or maul, dagger, pistol, rifle.
        /// </summary>
        private static float Motion(FightWeapon weapon)
        {
            if (weapon == null)
            {
                return 1f;
            }

            if (weapon.Ranged)
            {
                return weapon.TwoHanded ? 5f : 4f;
            }

            return weapon.TwoHanded ? 2f : weapon.TakesShield ? 1f : 3f;
        }

        /// <summary>Two free cells the given distance from the player, as far apart as the ring gives, into <see cref="_ring"/>.</summary>
        private bool Places(CombatCell home, float y)
        {
            _ring.Clear();
            for (var dz = -OpponentDistance; dz <= OpponentDistance; dz++)
            {
                for (var dx = -OpponentDistance; dx <= OpponentDistance; dx++)
                {
                    var x = home.X + dx;
                    var z = home.Z + dz;
                    var level = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) == OpponentDistance ? _grid.LevelNear(x, z, y) : -1;
                    var cell = new CombatCell(x, z, Mathf.Max(0, level));
                    if (level >= 0 && _grid.IsReachable(cell) && _grid.OccupantAt(cell) < 0)
                    {
                        _ring.Add(cell);
                    }
                }
            }

            if (_ring.Count < Opponents)
            {
                return false;
            }

            var far = 1;
            for (var i = 2; i < _ring.Count; i++)
            {
                if (Apart(_ring[0], _ring[i]) > Apart(_ring[0], _ring[far]))
                {
                    far = i;
                }
            }

            (_ring[1], _ring[far]) = (_ring[far], _ring[1]);
            return true;
        }

        private static int Apart(CombatCell a, CombatCell b) => Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Z - b.Z);

        private Transform Opponent(Vector3 at, Vector3 player, int number)
        {
            var look = player - at;
            look.y = 0f;
            var go = Instantiate(_settings.OpponentModel, at, look.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(look) : Quaternion.identity);
            go.name = $"Development opponent {number}";
            var bounds = new Bounds(at, Vector3.zero);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                bounds.Encapsulate(r.bounds);
            }

            if (bounds.size.y > 0.1f)
            {
                go.transform.localScale *= OpponentHeight / bounds.size.y;
            }

            // A person is a layer of its own, which the grid never reads (17 C.2).
            var layer = LayerMask.NameToLayer(KitLayers.Character);
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = layer;
            }

            go.AddComponent<SliceFighter>();
            _developmentOpponents.Add(go);
            return go.transform;
        }

        private void OnDestroy()
        {
            if (_outline != null)
            {
                Destroy(_outline);
            }
        }

        /// <summary>The grid's short sound as it forms: a low struck chord, made here until the owner picks a sound (authoring default).</summary>
        private static AudioClip Formed()
        {
            const int rate = 44100;
            const float length = 0.3f;
            var samples = new float[(int)(rate * length)];
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)rate;
                var tone = Mathf.Sin(2f * Mathf.PI * 110f * t) + (0.5f * Mathf.Sin(2f * Mathf.PI * 165f * t)) + (0.25f * Mathf.Sin(2f * Mathf.PI * 220f * t));
                samples[i] = 0.3f * Mathf.Exp(-t * 14f) * tone;
            }

            var clip = AudioClip.Create("Confront formed", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

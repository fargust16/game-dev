using System.Collections.Generic;
using GameDev.Confront;
using GameDev.Confront.Grid;
using GameDev.Confront.Rules;
using GameDev.Confront.Turns;
using GameDev.Input;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.Kit;
using GameDev.World.Seed;
using GameDev.World.Timekeeping;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// The slice's fights in the scene (S3a.4): the Encounter Lock and the turns on a Combat Grid round the player, the
    /// grid drawn with a short sound as it forms, the bodies set where their combatants stand, free movement held
    /// until the fight ends, and fighters who come within the grid joining at the next round while those who fall out
    /// of it drop out. Until S3a.5 brings the cursor the player's character fights by the same rule as the others.
    /// F7 starts a development fight.
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

        private readonly Dictionary<Combatant, Transform> _bodies = new Dictionary<Combatant, Transform>();
        private readonly List<GameObject> _developmentOpponents = new List<GameObject>();
        private readonly List<CombatCell> _ring = new List<CombatCell>();
        private SliceSettings _settings;
        private IWorldSeed _seed;
        private CharacterSheet _character;
        private CombatGridReader _grid;
        private TurnEconomy _turns;
        private EncounterLock _lock;
        private CombatGridView _view;
        private AudioSource _audio;
        private AudioClip _formed;
        private PlayerLocomotion _player;
        private int _fights;
        private float _wait;

        /// <summary>Seconds between turns played without the player's will, so a fight can be followed (authoring default).</summary>
        public float TurnPause { get; set; } = 0.35f;

        public IEncounterLock Lock => _lock;

        public ITurnEconomy Turns => _turns;

        public ICombatGrid Grid => _grid;

        public bool GridShown => _view != null && _view.Shown;

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
            _view = CombatGridView.Create(transform, settings.GridMaterial);
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
            if (_lock == null || _lock.InConfront || _player == null || _settings.OpponentModel == null)
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
            var rules = _settings.FightRules;
            var player = new Combatant(PlayerId, PlayerSide,
                rules.Fighter(_character, null, new WeaponSet(_settings.KitSword), _settings.KitArmour, new WeaponSet(_settings.KitRifle, _settings.KitPouch)), at);
            var combatants = new List<Combatant> { player };
            _bodies[player] = _player.transform;
            for (var i = 0; i < Opponents; i++)
            {
                var body = Opponent(_grid.Center(_ring[i]), at, i + 1);
                var opponent = new Combatant(i + 1, HostileSide, rules.Fighter(new CharacterSheet(), null, new WeaponSet(_settings.OpponentWeapon)), body.position);
                body.GetComponent<SliceFighter>().Combatant = opponent;
                _bodies[opponent] = body;
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

            var input = GameInputService.Instance;
            if (!_lock.InConfront && input != null && input.DevFightPressed && Debug.isDebugBuild)
            {
                StartDevelopmentFight();
            }

            if (!_lock.InConfront)
            {
                return;
            }

            var acting = _turns.Acting;
            if (acting != null && !acting.TakesIntents)
            {
                _wait -= Time.deltaTime;
                if (_wait <= 0f)
                {
                    _wait = TurnPause;
                    _turns.PlayTurn();
                }
            }

            Mirror();
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

                if (_player != null && body == _player.transform)
                {
                    _player.Teleport(pair.Key.Position, body.rotation);
                }
                else
                {
                    body.position = pair.Key.Position;
                }
            }

            var playerBody = _player != null ? _player.transform : null;
            foreach (var pair in _bodies)
            {
                // A fallen body lies down until the death clips of S3a.5.
                if (pair.Key.Fighter.Down && pair.Value != null && pair.Value != playerBody && Vector3.Dot(pair.Value.up, Vector3.up) > 0.5f)
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

        private void OnStarted(EncounterRequest request)
        {
            if (_player != null)
            {
                _player.enabled = false;
            }

            _view.Show(_grid);
            _audio.PlayOneShot(_formed);
            _wait = TurnPause;
            Debug.Log($"[Confront] fight {_fights} begins ({request.Reason}): {request.Combatants.Count} combatants, {_view.Cells} cells.");
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
                    _turns.Join(c);
                }
            }
        }

        private void OnEnded(int side)
        {
            Debug.Log($"[Confront] fight {_fights} ends after {_turns.Round} rounds; side {side} stands.");
            _view.Hide();
            if (_player != null)
            {
                _player.enabled = true;
            }

            foreach (var opponent in _developmentOpponents)
            {
                if (opponent != null)
                {
                    Destroy(opponent);
                }
            }

            _developmentOpponents.Clear();
            _bodies.Clear();
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

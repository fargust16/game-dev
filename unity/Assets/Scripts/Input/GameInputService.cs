using UnityEngine;
using UnityEngine.InputSystem;

namespace GameDev.Input
{
    /// <summary>
    /// Modern Input System facade (Package Only). Replaces UnityEngine.Input.
    /// </summary>
    public sealed class GameInputService : MonoBehaviour
    {
        public static GameInputService Instance { get; private set; }

        private InputActionMap _player;
        private InputActionMap _camera;
        private InputActionMap _world;
        private InputActionMap _ui;

        private InputAction _move;
        private InputAction _clickMove;
        private InputAction _interact;
        private InputAction _toggleScheme;
        private InputAction _toggleTurnBased;
        private InputAction _legend;
        private InputAction _jump;
        private InputAction _notes;
        private InputAction _map;
        private InputAction _sprint;

        private InputAction _camPan;
        private InputAction _camOrbit;
        private InputAction _camZoom;
        private InputAction _camFocus;
        private InputAction _camFast;
        private InputAction _camTurn;
        private InputAction _endTurn;
        private InputAction _waitTurn;
        private InputAction _stance;
        private InputAction _changeSet;
        private InputAction _firstAid;

        private InputAction _pause;
        private InputAction _advanceHour;
        private InputAction _rewindHour;
        private InputAction _skipDayNight;
        private InputAction _devCharacterValues;
        private InputAction _devFight;

        private InputAction _cancel;
        private InputAction _language;
        private InputAction[] _chooseOptions;

        public static GameInputService EnsureExists()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var existing = FindAnyObjectByType<GameInputService>();
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject("GameInputService");
            return go.AddComponent<GameInputService>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BuildActions();
            EnableGameplayMaps();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            _player?.Disable();
            _camera?.Disable();
            _world?.Disable();
            _ui?.Disable();
            _player?.Dispose();
            _camera?.Dispose();
            _world?.Dispose();
            _ui?.Dispose();
        }

        public void EnableGameplayMaps()
        {
            _player?.Enable();
            _camera?.Enable();
            _world?.Enable();
            _ui?.Enable();
        }

        public Vector2 MoveVector => _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
        public bool ClickMovePressed => _clickMove != null && _clickMove.WasPressedThisFrame();
        public bool InteractPressed => _interact != null && _interact.WasPressedThisFrame();
        public bool ToggleSchemePressed => _toggleScheme != null && _toggleScheme.WasPressedThisFrame();
        public bool ToggleTurnBasedPressed => _toggleTurnBased != null && _toggleTurnBased.WasPressedThisFrame();
        public bool LegendPressed => _legend != null && _legend.WasPressedThisFrame();
        public bool JumpPressed => _jump != null && _jump.WasPressedThisFrame();
        public bool NotesPressed => _notes != null && _notes.WasPressedThisFrame();
        public bool MapPressed => _map != null && _map.WasPressedThisFrame();
        public bool SprintHeld => _sprint != null && _sprint.IsPressed();

        public Vector2 CameraPanVector => _camPan != null ? _camPan.ReadValue<Vector2>() : Vector2.zero;
        public bool CameraOrbitHeld => _camOrbit != null && _camOrbit.IsPressed();
        public bool CameraOrbitPressed => _camOrbit != null && _camOrbit.WasPressedThisFrame();
        public bool CameraOrbitReleased => _camOrbit != null && _camOrbit.WasReleasedThisFrame();
        public bool CameraPanHeld => Mouse.current != null && Mouse.current.middleButton.isPressed;
        public bool CameraPanPressed => Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame;
        public bool CameraPanReleased => Mouse.current != null && Mouse.current.middleButton.wasReleasedThisFrame;
        public float CameraZoomDelta
        {
            get
            {
                if (_camZoom == null)
                {
                    return 0f;
                }

                var v = _camZoom.ReadValue<float>();
                // Mouse wheel often reports ±120 per notch in the Input System.
                if (Mathf.Abs(v) > 10f)
                {
                    v /= 120f;
                }

                return v;
            }
        }
        public bool CameraFocusPressed => _camFocus != null && _camFocus.WasPressedThisFrame();
        public bool CameraFastHeld => _camFast != null && _camFast.IsPressed();

        /// <summary>The fight camera's turn (S3a.5, the grid lab's keys): −1 for Q, +1 for E.</summary>
        public float CameraTurn => _camTurn != null ? _camTurn.ReadValue<float>() : 0f;

        /// <summary>Ends the player's turn in a fight (S3a.5 authoring default: Space or Enter).</summary>
        public bool EndTurnPressed => _endTurn != null && _endTurn.WasPressedThisFrame();

        /// <summary>S3a.6 authoring defaults in a fight: R waits to the end of the round, G ends the turn in a Defensive Stance, X changes the Weapon Set.</summary>
        public bool WaitTurnPressed => _waitTurn != null && _waitTurn.WasPressedThisFrame();

        public bool StancePressed => _stance != null && _stance.WasPressedThisFrame();

        public bool ChangeSetPressed => _changeSet != null && _changeSet.WasPressedThisFrame();

        /// <summary>S3a.7 authoring default, B: First Aid on oneself, the most urgent condition first, in a fight and out of one.</summary>
        public bool FirstAidPressed => _firstAid != null && _firstAid.WasPressedThisFrame();

        public Vector2 MousePosition => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        public Vector2 MouseDelta => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;

        public bool PausePressed => _pause != null && _pause.WasPressedThisFrame();
        public bool AdvanceHourPressed => _advanceHour != null && _advanceHour.WasPressedThisFrame();
        public bool RewindHourPressed => _rewindHour != null && _rewindHour.WasPressedThisFrame();
        public bool SkipDayNightPressed => _skipDayNight != null && _skipDayNight.WasPressedThisFrame();

        /// <summary>Development command (S2 authoring default, F8): the character's values to the next test step.</summary>
        public bool DevCharacterValuesPressed => _devCharacterValues != null && _devCharacterValues.WasPressedThisFrame();

        /// <summary>Development command (S3a.4 authoring default, F7): a fight with two opponents beside the player.</summary>
        public bool DevFightPressed => _devFight != null && _devFight.WasPressedThisFrame();

        public bool CancelPressed => _cancel != null && _cancel.WasPressedThisFrame();

        /// <summary>The next language (17 X.7); authoring default F10 until a settings page exists.</summary>
        public bool LanguagePressed => _language != null && _language.WasPressedThisFrame();

        public bool OptionPressed(int index1Based)
        {
            if (_chooseOptions == null || index1Based < 1 || index1Based > _chooseOptions.Length)
            {
                return false;
            }

            var action = _chooseOptions[index1Based - 1];
            return action != null && action.WasPressedThisFrame();
        }

        private void BuildActions()
        {
            _player = new InputActionMap("Player");
            _move = _player.AddAction("Move", InputActionType.Value);
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            _clickMove = _player.AddAction("ClickMove", InputActionType.Button, "<Mouse>/leftButton");
            _interact = _player.AddAction("Interact", InputActionType.Button, "<Keyboard>/e");
            _toggleScheme = _player.AddAction("ToggleControlScheme", InputActionType.Button, "<Keyboard>/tab");
            _toggleTurnBased = _player.AddAction("ToggleTurnBased", InputActionType.Button, "<Keyboard>/t");
            _legend = _player.AddAction("Legend", InputActionType.Button, "<Keyboard>/h");
            _jump = _player.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
            _notes = _player.AddAction("Notes", InputActionType.Button, "<Keyboard>/j");
            _map = _player.AddAction("Map", InputActionType.Button, "<Keyboard>/m");
            _sprint = _player.AddAction("Sprint", InputActionType.Button);
            _sprint.AddBinding("<Keyboard>/leftShift");
            _sprint.AddBinding("<Keyboard>/rightShift");
            _endTurn = _player.AddAction("EndTurn", InputActionType.Button);
            _endTurn.AddBinding("<Keyboard>/space");
            _endTurn.AddBinding("<Keyboard>/enter");
            _endTurn.AddBinding("<Keyboard>/numpadEnter");
            _waitTurn = _player.AddAction("WaitTurn", InputActionType.Button, "<Keyboard>/r");
            _stance = _player.AddAction("DefensiveStance", InputActionType.Button, "<Keyboard>/g");
            _changeSet = _player.AddAction("ChangeWeaponSet", InputActionType.Button, "<Keyboard>/x");
            _firstAid = _player.AddAction("FirstAid", InputActionType.Button, "<Keyboard>/b");

            _camera = new InputActionMap("Camera");
            _camPan = _camera.AddAction("Pan", InputActionType.Value);
            _camPan.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _camOrbit = _camera.AddAction("Orbit", InputActionType.Button, "<Mouse>/rightButton");
            _camZoom = _camera.AddAction("Zoom", InputActionType.Value, "<Mouse>/scroll/y");
            _camFocus = _camera.AddAction("Focus", InputActionType.Button);
            _camFocus.AddBinding("<Keyboard>/f");
            _camFocus.AddBinding("<Keyboard>/home");
            _camFast = _camera.AddAction("Fast", InputActionType.Button);
            _camFast.AddBinding("<Keyboard>/leftShift");
            _camFast.AddBinding("<Keyboard>/rightShift");
            _camTurn = _camera.AddAction("Turn", InputActionType.Value);
            _camTurn.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/q")
                .With("Positive", "<Keyboard>/e");

            _world = new InputActionMap("World");
            _pause = _world.AddAction("Pause", InputActionType.Button, "<Keyboard>/p");
            _advanceHour = _world.AddAction("AdvanceHour", InputActionType.Button);
            _advanceHour.AddBinding("<Keyboard>/rightBracket");
            _advanceHour.AddBinding("<Keyboard>/equals");
            _rewindHour = _world.AddAction("RewindHour", InputActionType.Button);
            _rewindHour.AddBinding("<Keyboard>/leftBracket");
            _rewindHour.AddBinding("<Keyboard>/minus");
            _skipDayNight = _world.AddAction("SkipDayNight", InputActionType.Button, "<Keyboard>/n");
            _devCharacterValues = _world.AddAction("DevCharacterValues", InputActionType.Button, "<Keyboard>/f8");
            _devFight = _world.AddAction("DevFight", InputActionType.Button, "<Keyboard>/f7");

            _ui = new InputActionMap("UI");
            _cancel = _ui.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
            _language = _ui.AddAction("Language", InputActionType.Button, "<Keyboard>/f10");
            // A conversation numbers up to nine options (S2.4).
            _chooseOptions = new InputAction[9];
            for (var i = 0; i < _chooseOptions.Length; i++)
            {
                var action = _ui.AddAction($"Option{i + 1}", InputActionType.Button);
                action.AddBinding($"<Keyboard>/{i + 1}");
                action.AddBinding($"<Keyboard>/numpad{i + 1}");
                _chooseOptions[i] = action;
            }
        }
    }
}

#if UNITY_EDITOR
using System.Collections;
using GameDev.Cameras;
using GameDev.Locomotion;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>S0.3 acceptance: the player walks the movement test scene in third person.</summary>
    public sealed class MovementSmokeTests
    {
        private const string ScenePath = "Assets/Scenes/Slice/S0_Movement_Test.unity";

        // A device of the test's own, fed through the Input System's public event queue.
        private Keyboard _keyboard;
        private InputSettings _savedSettings;
        private InputSettings _testSettings;

        [SetUp]
        public void SetUp()
        {
            // A batch run has no focused Game View, and by default the Input System drops keyboard events
            // without that focus. The test lets every device through and restores the project's settings after.
            _savedSettings = InputSystem.settings;
            _testSettings = Object.Instantiate(_savedSettings);
            _testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _testSettings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = _testSettings;
            _keyboard = InputSystem.AddDevice<Keyboard>("SliceTestKeyboard");
        }

        [TearDown]
        public void TearDown()
        {
            InputSystem.RemoveDevice(_keyboard);

            // A Single scene load may unload the project's settings object when no asset holds it; then the
            // Input System's defaults go back instead.
            InputSystem.settings = _savedSettings != null
                ? _savedSettings
                : ScriptableObject.CreateInstance<InputSettings>();
            Object.Destroy(_testSettings);
        }

        private void Hold(params Key[] keys)
        {
            // Processed at the next frame's input update; the state holds until the next event.
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
        }

        private static string Describe(PlayerLocomotion player)
        {
            return $"position {player.transform.position}, grounded {player.IsGrounded}, enabled {player.isActiveAndEnabled}, " +
                   $"input service {GameDev.Input.GameInputService.Instance != null}, move {GameDev.Input.GameInputService.Instance?.MoveVector}, " +
                   $"timeScale {Time.timeScale}, frame {Time.frameCount}, players {Object.FindObjectsByType<PlayerLocomotion>(FindObjectsInactive.Include).Length}";
        }

        private static IEnumerator LoadScene()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));

            // Wait by time, not frames: a batch run draws thousands of frames a second, so frames carry almost no time.
            yield return new WaitForSeconds(0.5f);
        }

        [UnityTest]
        public IEnumerator ThePlayerStandsOnTheStreetAndTheCameraFollows()
        {
            yield return LoadScene();

            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            Assert.IsNotNull(player);
            Assert.IsTrue(player.IsGrounded, Describe(player));
            Assert.AreEqual(0f, player.transform.position.y, 0.1f);

            var camera = Object.FindAnyObjectByType<ThirdPersonCamera>();
            Assert.AreSame(player.transform, camera.Target);
            Assert.Less(Vector3.Distance(camera.transform.position, player.transform.position), 10f);
            Assert.IsNotNull(GameClockDriver.Running);
        }

        [UnityTest]
        public IEnumerator HoldingForwardWalksThePlayerNorth()
        {
            yield return LoadScene();
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var start = player.transform.position;

            Hold(Key.W);
            yield return new WaitForSeconds(0.5f);
            var midway = Describe(player);
            yield return new WaitForSeconds(1f);
            Hold();
            yield return null;

            var moved = player.transform.position - start;
            Assert.Greater(moved.z, 1.5f, $"the player should have walked north; midway: {midway}; end: {Describe(player)}");
            Assert.Less(Mathf.Abs(moved.x), 0.5f);
            Assert.IsTrue(player.IsGrounded);
            Assert.AreEqual(start.y, player.transform.position.y, 0.15f);
        }
    }
}
#endif

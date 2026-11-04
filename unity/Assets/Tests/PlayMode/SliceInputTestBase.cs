#if UNITY_EDITOR
using System.Collections;
using GameDev.Locomotion;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// PlayMode tests that press keys: a keyboard of the test's own, fed through the Input System's public event queue,
    /// and settings that let input through without a focused Game View.
    /// </summary>
    public abstract class SliceInputTestBase
    {
        private Keyboard _keyboard;
        private InputSettings _savedSettings;
        private InputSettings _testSettings;

        [SetUp]
        public void SetUpInput()
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
        public void TearDownInput()
        {
            InputSystem.RemoveDevice(_keyboard);

            // A Single scene load may unload the project's settings object when no asset holds it; then the
            // Input System's defaults go back instead.
            InputSystem.settings = _savedSettings != null
                ? _savedSettings
                : ScriptableObject.CreateInstance<InputSettings>();
            Object.Destroy(_testSettings);
        }

        /// <summary>Holds exactly these keys from the next input update until the next call.</summary>
        protected void Hold(params Key[] keys)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
        }

        /// <summary>Presses and releases one key, so it reads as pressed on exactly one frame.</summary>
        protected IEnumerator Tap(Key key)
        {
            Hold(key);
            yield return null;
            yield return null;
            Hold();
            yield return null;
        }

        protected static string Describe(PlayerLocomotion player)
        {
            return $"position {player.transform.position}, grounded {player.IsGrounded}, enabled {player.isActiveAndEnabled}, " +
                   $"input service {GameDev.Input.GameInputService.Instance != null}, move {GameDev.Input.GameInputService.Instance?.MoveVector}, " +
                   $"timeScale {Time.timeScale}, frame {Time.frameCount}, players {Object.FindObjectsByType<PlayerLocomotion>(FindObjectsInactive.Include).Length}";
        }

        protected static IEnumerator LoadScene(string path)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));

            // Wait by time, not frames: a batch run draws thousands of frames a second, so frames carry almost no time.
            yield return new WaitForSeconds(0.5f);
        }
    }
}
#endif

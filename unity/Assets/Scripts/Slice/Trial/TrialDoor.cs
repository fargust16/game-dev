using GameDev.Core.Localization;
using GameDev.Core.Scenes;
using GameDev.Input;
using GameDev.Locomotion;
using GameDev.UI;
using GameDev.World.Portals;
using UnityEngine;

namespace GameDev.Slice.Trial
{
    /// <summary>
    /// Thomas Wren's door from Draught Lane (S5c.4; SLICE_TRIAL §1.2): locked until it is opened once. With Lottie's key
    /// <c>E  Unlock with Thomas's key</c> opens it for good and goes through; without it the door says it is locked, and
    /// picking the lock comes with S5c.5. Added by the world's driver to the <c>missing-room.street</c> portal.
    /// </summary>
    [RequireComponent(typeof(Portal))]
    public sealed class TrialDoor : MonoBehaviour
    {
        private Portal _portal;
        private PlayerLocomotion _player;
        private HudView _hud;

        private void Awake() => _portal = GetComponent<Portal>();

        private void Update()
        {
            var trial = SliceWorldDriver.Running != null ? SliceWorldDriver.Running.Trial : null;
            if (_portal == null || trial == null)
            {
                return;
            }

            _portal.Sealed = !trial.DoorOpen;
            _portal.SealedPrompt = trial.HasThomasKey ? "ui.prompt.trial.unlock" : "ui.prompt.locked";
            var input = GameInputService.Instance;
            if (!_portal.Sealed || input == null || !input.InteractPressed || SceneTransition.IsTraveling)
            {
                return;
            }

            _player = _player != null ? _player : FindAnyObjectByType<PlayerLocomotion>();
            _hud = _hud != null ? _hud : FindAnyObjectByType<HudView>();
            if (_player == null || !_portal.InReach(_player.transform.position) || (_hud != null && _hud.AnyWindowOpen))
            {
                return;
            }

            if (!trial.HasThomasKey)
            {
                _hud?.ShowMessage(Loc.Get("ui.trial.door.locked"));
                return;
            }

            SliceWorldDriver.Running.State.Raise(SliceTrial.DoorOpenKey);
            _portal.Sealed = false;
            SceneTransition.Travel(_portal.Target);
        }
    }
}

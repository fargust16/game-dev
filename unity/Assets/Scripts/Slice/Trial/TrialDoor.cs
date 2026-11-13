using GameDev.Slice.BreakIns;
using GameDev.World.Portals;
using UnityEngine;

namespace GameDev.Slice.Trial
{
    /// <summary>
    /// Thomas Wren's door from Draught Lane (S5c.4; SLICE_TRIAL §1.2): a lock of a poor home, Lock 2, until it is opened
    /// once. With Lottie's key <c>E  Unlock with Thomas's key</c> opens it for good and goes through; without it the lock is
    /// picked as any dwelling's, a break-in by every rule of 10.3 §6.1–6.2 (S5c.5). Added by the world's driver to the
    /// <c>missing-room.street</c> portal.
    /// </summary>
    [RequireComponent(typeof(Portal))]
    public sealed class TrialDoor : MonoBehaviour
    {
        /// <summary>The place Thomas Wren's lock is kept under, and its Lock: a poor home's (10.3 §6.1).</summary>
        public const string Place = "missing-room";

        public const int Lock = 2;

        private void Awake()
        {
            var door = GetComponent<LockedDoor>();
            door = door != null ? door : gameObject.AddComponent<LockedDoor>();
            door.Configure(Place, Lock);
            door.OpenForGood = () => Trial != null && Trial.DoorOpen;
            door.HasKey = () => Trial != null && Trial.HasThomasKey;
            door.KeyPrompt = "ui.prompt.trial.unlock";
            door.UseKey = () => SliceWorldDriver.Running.State.Raise(SliceTrial.DoorOpenKey);
        }

        private static SliceTrial Trial => SliceWorldDriver.Running != null ? SliceWorldDriver.Running.Trial : null;
    }
}

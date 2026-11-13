using System.Linq;
using GameDev.Core.Localization;
using GameDev.Input;
using GameDev.Items;
using GameDev.Locomotion;
using GameDev.UI;
using GameDev.World.Portals;
using UnityEngine;

namespace GameDev.Slice.Mind
{
    /// <summary>
    /// The Soot &amp; Bell's let room (S5c.2; 11 *Sleep*, 19 §6.1): <c>E  Rent the room for the night</c> beside its bed opens the
    /// page of sleep at the room's price, paid as the sleep begins; a sleep there is safe. The driver puts it on the bed
    /// when the tavern's scene loads, so no scene is changed for it.
    /// </summary>
    public sealed class SleepBed : MonoBehaviour, IPromptSource
    {
        /// <summary>Authoring default, m: how far from the bed's edge the character may stand to lie down in it.</summary>
        public const float Reach = 1.2f;

        /// <summary>As every interaction's, m: the height within which the character counts as level with it.</summary>
        private const float SameLevel = 1.5f;

        private PlayerLocomotion _player;
        private HudView _hud;
        private Portal[] _doors;
        private Bounds _footprint;
        private bool _measured;

        /// <summary>Within <see cref="Reach"/> of the bed's edge and level with it.</summary>
        public bool InReach(Vector3 position)
        {
            if (!_measured)
            {
                Measure();
            }

            if (Mathf.Abs(position.y - _footprint.min.y) > SameLevel)
            {
                return false;
            }

            var dx = Mathf.Max(0f, Mathf.Abs(position.x - _footprint.center.x) - _footprint.extents.x);
            var dz = Mathf.Max(0f, Mathf.Abs(position.z - _footprint.center.z) - _footprint.extents.z);
            return (dx * dx) + (dz * dz) <= Reach * Reach;
        }

        private void Measure()
        {
            var any = false;
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (any)
                {
                    _footprint.Encapsulate(r.bounds);
                }
                else
                {
                    _footprint = r.bounds;
                    any = true;
                }
            }

            if (!any)
            {
                _footprint = new Bounds(transform.position, new Vector3(1f, 0.6f, 2f));
            }

            _measured = true;
        }

        public string PromptFor(Vector3 player)
        {
            var sleep = SliceWorldDriver.Running != null ? SliceWorldDriver.Running.Sleep : null;
            if (sleep == null || sleep.Sleeping || sleep.IsPageOpen || !InReach(player))
            {
                return null;
            }

            return Loc.Format("ui.prompt.sleep_room", Coins.Show(SliceSleep.RoomPrice));
        }

        private void Update()
        {
            var input = GameInputService.Instance;
            var sleep = SliceWorldDriver.Running != null ? SliceWorldDriver.Running.Sleep : null;
            _player = _player != null ? _player : FindAnyObjectByType<PlayerLocomotion>();
            if (input == null || sleep == null || _player == null || !input.InteractPressed || sleep.Sleeping || sleep.IsPageOpen
                || !InReach(_player.transform.position))
            {
                return;
            }

            // E belongs to the bed only while the HUD shows its prompt, so a door beside it keeps E.
            _hud = _hud != null ? _hud : FindAnyObjectByType<HudView>();
            if (_hud != null && _hud.PromptText != PromptFor(_player.transform.position))
            {
                return;
            }

            _doors ??= FindObjectsByType<Portal>(FindObjectsInactive.Exclude);
            if (!_doors.Any(d => d != null && d.isActiveAndEnabled && d.InReach(_player.transform.position)))
            {
                sleep.OpenPage(SleepPlace.Room);
            }
        }
    }
}

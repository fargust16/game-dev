using System.Linq;
using GameDev.Core.Localization;
using GameDev.Core.Scenes;
using GameDev.Input;
using GameDev.Items;
using GameDev.Locomotion;
using GameDev.Slice.Fights;
using GameDev.UI;
using GameDev.World.Portals;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDev.Slice.People
{
    /// <summary>
    /// The front desk of the Wardens' lock-up (10.2 §9; S5c.7), at Sergeant Coyle's post: while the character owes the
    /// fine or has weapons in the Wardens' store, E pays the fine from the bag and takes the weapons back. Put into the
    /// lock-up's scene by the world's driver when it loads.
    /// </summary>
    public sealed class WardensDesk : MonoBehaviour, IPromptSource
    {
        public const string RootName = "Wardens' Desk";

        /// <summary>The post the desk stands at: Sergeant Coyle's work in the lock-up (SLICE_PEOPLE §4).</summary>
        public const string Keeper = "coyle";

        /// <summary>Authoring defaults, m: the reach in the room and how far above or below still counts as the same floor.</summary>
        private const float Reach = 2f;
        private const float SameLevel = 1.5f;

        private PlayerLocomotion _player;
        private HudView _hud;
        private Portal[] _doors;

        /// <summary>Puts the desk at the keeper's post in the lock-up's scene; true when placed.</summary>
        public static bool Place(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene.name != SliceConfront.LockupScene
                || scene.GetRootGameObjects().Any(r => r.name == RootName))
            {
                return false;
            }

            var post = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PersonPost>(true))
                .FirstOrDefault(p => p.Person == Keeper);
            if (post == null)
            {
                return false;
            }

            var desk = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(desk, scene);
            desk.transform.position = post.transform.position;
            desk.AddComponent<WardensDesk>();
            FindAnyObjectByType<HudView>()?.FindPromptSources();
            return true;
        }

        public bool InReach(Vector3 position) =>
            Mathf.Abs(position.y - transform.position.y) <= SameLevel
            && new Vector2(position.x - transform.position.x, position.z - transform.position.z).sqrMagnitude <= Reach * Reach;

        public string PromptFor(Vector3 player)
        {
            var debts = SliceWorldDriver.Running != null ? SliceWorldDriver.Running.Debts : null;
            if (debts == null || !debts.Owing || !InReach(player))
            {
                return null;
            }

            return debts.Owed > 0 ? Loc.Format("ui.prompt.fine.pay", Coins.Show(debts.Owed)) : Loc.Get("ui.prompt.fine.weapons");
        }

        /// <summary>Pays and takes the weapons back, saying so on the screen; false when the bag holds too little.</summary>
        public bool Settle(YieldDebts debts)
        {
            var owed = debts.Owed;
            var paid = debts.Pay();
            _hud = _hud != null && _hud.isActiveAndEnabled ? _hud : FindAnyObjectByType<HudView>();
            if (!paid)
            {
                _hud?.ShowMessage(Loc.Format("ui.fine.short", Coins.Show(owed)));
                return false;
            }

            _hud?.ShowMessage(Loc.Get(debts.Held.Count > 0 ? "ui.fine.paid.full" : "ui.fine.paid"));
            return true;
        }

        private void Update()
        {
            var world = SliceWorldDriver.Running;
            var debts = world != null ? world.Debts : null;
            var input = GameInputService.Instance;
            if (debts == null || input == null || !input.InteractPressed || world.Clock.IsPaused || SceneTransition.IsTraveling)
            {
                return;
            }

            _player = _player != null ? _player : FindAnyObjectByType<PlayerLocomotion>();
            var at = _player != null ? _player.transform.position : transform.position + (Vector3.up * 100f);
            var prompt = PromptFor(at);
            if (prompt == null)
            {
                return;
            }

            // E belongs to the desk only while the HUD shows its prompt, no door in reach keeps it, and no page is open.
            _hud = _hud != null && _hud.isActiveAndEnabled ? _hud : FindAnyObjectByType<HudView>();
            _doors ??= FindObjectsByType<Portal>(FindObjectsInactive.Exclude);
            if (_hud == null || _hud.AnyWindowOpen || _hud.PromptText != prompt || _doors.Any(d => d != null && d.isActiveAndEnabled && d.InReach(at)))
            {
                return;
            }

            Settle(debts);
        }
    }
}

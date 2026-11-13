using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Core.Scenes;
using GameDev.Input;
using GameDev.Items;
using GameDev.Locomotion;
using GameDev.UI;
using GameDev.World.Portals;
using UnityEngine;

namespace GameDev.Slice.People
{
    /// <summary>
    /// 19 §6 (S5c.7): E beside a fallen person's body searches it once, as <see cref="SliceRecovery"/> rolls, and says
    /// on the screen what the character took. Out of a fight only, and only while the body lies where it fell: once a
    /// Warden has taken it up (10.2 §5c *The fallen*), what it carried is gone with it.
    /// </summary>
    public sealed class BodySearch : MonoBehaviour, IPromptSource
    {
        /// <summary>Authoring defaults, m: the reach to a body and how far above or below still counts as the same floor.</summary>
        public const float Reach = 1.5f;

        private const float SameLevel = 1.5f;
        private const float LookAgain = 1f; // s between looks for the fallen

        private readonly List<FallenBody> _bodies = new List<FallenBody>();
        private float _lookedAt = float.NegativeInfinity;
        private PlayerLocomotion _player;
        private HudView _hud;

        private static SliceRecovery Recovery => SliceWorldDriver.Running != null ? SliceWorldDriver.Running.Recovery : null;

        /// <summary>The body in reach of <paramref name="player"/> that can be searched, the nearest, or null.</summary>
        public Transform BodyFor(Vector3 player)
        {
            var recovery = Recovery;
            if (recovery == null)
            {
                return null;
            }

            if (Time.unscaledTime - _lookedAt > LookAgain)
            {
                _lookedAt = Time.unscaledTime;
                _bodies.Clear();
                _bodies.AddRange(FindObjectsByType<FallenBody>(FindObjectsInactive.Exclude));
            }

            Transform best = null;
            var bestDistance = Reach * Reach;
            foreach (var body in _bodies)
            {
                if (body == null || !body.isActiveAndEnabled || Mathf.Abs(body.transform.position.y - player.y) > SameLevel)
                {
                    continue;
                }

                var d = new Vector2(body.transform.position.x - player.x, body.transform.position.z - player.z).sqrMagnitude;
                if (d <= bestDistance && recovery.CanSearch(body.transform))
                {
                    best = body.transform;
                    bestDistance = d;
                }
            }

            return best;
        }

        public string PromptFor(Vector3 player)
        {
            var world = SliceWorldDriver.Running;
            if (world == null || (world.Fights != null && world.Fights.InFight))
            {
                return null;
            }

            return BodyFor(player) != null ? Loc.Get("ui.prompt.fallen.search") : null;
        }

        /// <summary>Searches <paramref name="body"/> and says what was taken; false when it could not be searched.</summary>
        public bool Search(Transform body)
        {
            var recovery = Recovery;
            if (recovery == null || !recovery.CanSearch(body))
            {
                return false;
            }

            var (taken, heavy) = recovery.Search(body);
            string line;
            if (taken.Count == 0 && heavy.Count == 0)
            {
                line = Loc.Get("ui.fallen.nothing");
            }
            else
            {
                line = taken.Count > 0 ? Loc.Format("ui.fallen.taken", string.Join(", ", taken.Select(p => Loc.Get(new ItemDefinition { id = p.item }.NameKey)))) : string.Empty;
                if (heavy.Count > 0)
                {
                    line = (line + " " + Loc.Format("ui.fallen.heavy", string.Join(", ", heavy.Select(id => Loc.Get(new ItemDefinition { id = id }.NameKey))))).Trim();
                }
            }

            _hud = _hud != null && _hud.isActiveAndEnabled ? _hud : FindAnyObjectByType<HudView>();
            _hud?.ShowMessage(line);
            return true;
        }

        private void Update()
        {
            var world = SliceWorldDriver.Running;
            var input = GameInputService.Instance;
            if (world == null || input == null || !input.InteractPressed || world.Clock.IsPaused || SceneTransition.IsTraveling)
            {
                return;
            }

            _player = _player != null ? _player : FindAnyObjectByType<PlayerLocomotion>();
            if (_player == null)
            {
                return;
            }

            var at = _player.transform.position;
            var prompt = PromptFor(at);
            if (prompt == null)
            {
                return;
            }

            // E belongs to the search only while the HUD shows its prompt, no door in reach keeps it, and no page is open.
            _hud = _hud != null && _hud.isActiveAndEnabled ? _hud : FindAnyObjectByType<HudView>();
            if (_hud == null || _hud.AnyWindowOpen || !_hud.IsPromptVisible || _hud.PromptText != prompt
                || FindObjectsByType<Portal>(FindObjectsInactive.Exclude).Any(d => d.isActiveAndEnabled && d.InReach(at)))
            {
                return;
            }

            Search(BodyFor(at));
        }
    }
}

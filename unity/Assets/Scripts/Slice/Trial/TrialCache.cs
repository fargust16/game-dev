using System.Linq;
using GameDev.Core.Localization;
using GameDev.Input;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.UI;
using GameDev.World.Portals;
using UnityEngine;

namespace GameDev.Slice.Trial
{
    /// <summary>
    /// The loose board under Thomas Wren's bed (S5c.4; SLICE_TRIAL §1.2–1.3): each time the character comes within 2 m it
    /// rolls one Perception check at stage 9, and Lottie's line about the boards finds it with no roll. Once noticed,
    /// <c>E  Lift the loose board</c> takes the cache: both Император formulas, the letter of introduction and Thomas's
    /// savings. Placed under the bed by <c>SliceTrialPlaces</c>.
    /// </summary>
    public sealed class TrialCache : MonoBehaviour, IPromptSource
    {
        /// <summary>Authoring default: a stage 9 Perception check is difficulty 2 on 07.1's scale, as a stage 9 piece's is.</summary>
        public const int NoticeDifficulty = 2;

        /// <summary>As every interaction's, m: the height within which the character counts as level with it.</summary>
        private const float SameLevel = 1.5f;

        private PlayerLocomotion _player;
        private HudView _hud;
        private Portal[] _doors;
        private bool _near;

        public bool InReach(Vector3 position) =>
            Mathf.Abs(position.y - transform.position.y) <= SameLevel
            && new Vector2(position.x - transform.position.x, position.z - transform.position.z).sqrMagnitude <= SliceTrial.NoticeReach * SliceTrial.NoticeReach;

        public string PromptFor(Vector3 player)
        {
            var trial = SliceWorldDriver.Running != null ? SliceWorldDriver.Running.Trial : null;
            return trial != null && trial.CacheNoticed && !trial.CacheFound && InReach(player) ? Loc.Get("ui.prompt.trial.board") : null;
        }

        private void Update()
        {
            var world = SliceWorldDriver.Running;
            var trial = world != null ? world.Trial : null;
            _player = _player != null ? _player : FindAnyObjectByType<PlayerLocomotion>();
            if (trial == null || _player == null)
            {
                return;
            }

            var at = _player.transform.position;
            var near = InReach(at);
            if (near && !_near && !trial.CacheFound && !trial.CacheNoticed)
            {
                Notice(world);
            }

            _near = near;
            var input = GameInputService.Instance;
            if (!near || input == null || !input.InteractPressed || !trial.CacheNoticed || trial.CacheFound)
            {
                return;
            }

            // E belongs to the board only while the HUD shows its prompt, and a door in reach keeps it.
            _hud = _hud != null ? _hud : FindAnyObjectByType<HudView>();
            _doors ??= FindObjectsByType<Portal>(FindObjectsInactive.Exclude);
            if ((_hud != null && _hud.PromptText != PromptFor(at)) || _doors.Any(d => d != null && d.isActiveAndEnabled && d.InReach(at)))
            {
                return;
            }

            if (trial.TakeCache())
            {
                _hud?.ShowMessage(Loc.Get("ui.trial.cache.taken"));
            }
        }

        /// <summary>One Perception check of 07.1 at stage 9, from the action checks' stream forked by the tick; it trains Perception.</summary>
        private void Notice(SliceWorldDriver world)
        {
            _hud = _hud != null ? _hud : FindAnyObjectByType<HudView>();
            if (world.State.IsSet(SliceTrial.BoardsArgument))
            {
                world.State.Raise(SliceTrial.NoticedKey);
                _hud?.ShowMessage(Loc.Get("ui.trial.cache.remembered"));
                return;
            }

            if (world.ActionChecks == null)
            {
                return;
            }

            var rolls = world.Seed.Fork(SliceSeedStreams.ActionChecks, GameDev.World.Seed.StableHash.Of(SliceTrial.NoticedKey), (int)world.Clock.Tick);
            var chance = TraceSpot.LookChance(world.ActionChecks, world.Character, NoticeDifficulty, world.Mind != null ? world.Mind.CheckPenalty : 0);
            var noticed = SliceTrial.Notices(chance, rolls);
            world.Character.Train(null, SliceTrial.NoticeStage, noticed ? CheckOutcome.Normal : CheckOutcome.Failure,
                new[] { CharacterAttribute.Perception }, new[] { CharacterAttribute.Intelligence });
            if (noticed)
            {
                world.State.Raise(SliceTrial.NoticedKey);
                _hud?.ShowMessage(Loc.Get("ui.trial.cache.noticed"));
            }
        }
    }
}

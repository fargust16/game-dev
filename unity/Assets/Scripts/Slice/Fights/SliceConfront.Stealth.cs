using System.Collections.Generic;
using GameDev.Confront.Turns;
using GameDev.Core.Localization;
using GameDev.Slice.Stealth;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// S3b.1, stealth in a fight (10.3 §7–8): the character enters the fight hidden from Stealth Mode, and strikes first
    /// if an opponent is in reach; C hides for 3 AP when no enemy cone covers it; the log tells who hid, who stayed
    /// unseen under a cone, and who was revealed. Free movement's Stealth Mode resumes after the fight if the character
    /// ended it hidden.
    /// </summary>
    public sealed partial class SliceConfront
    {
        /// <summary>Stealth in free movement, whose Stealth Mode and Darkness the fight reads.</summary>
        public SliceStealth Hiding { get; set; }

        private bool HideInFight(Combatant player)
        {
            if (_turns.SubmitIntent(TurnIntent.Hide))
            {
                return true;
            }

            _hud.Line(Loc.Get(player.Hidden ? "ui.fight.hidden" : "ui.fight.nohide"));
            _hud.MarkDirty();
            return false;
        }

        private void OnHiddenChanged(Combatant c)
        {
            _hud.Line(Loc.Format(c.Hidden ? "ui.fight.log.hidden" : "ui.fight.log.revealed", Name(c)));
            _hud.MarkDirty();
        }

        private void OnConeChecked(Combatant observer, Combatant hider, double chance, bool stays)
        {
            if (stays)
            {
                _hud.Line(Loc.Format("ui.fight.log.unseen", Name(hider), Name(observer)));
            }
        }

        private static Combatant Nearest(Combatant player, IReadOnlyList<Combatant> combatants)
        {
            Combatant nearest = null;
            var best = float.MaxValue;
            foreach (var c in combatants)
            {
                var d = (c.Position - player.Position).sqrMagnitude;
                if (c.Side != player.Side && d < best)
                {
                    best = d;
                    nearest = c;
                }
            }

            return nearest;
        }
    }
}

using GameDev.Confront.Rules;
using GameDev.Confront.Turns;
using GameDev.Player.Progression;
using GameDev.Slice.Beasts;
using UnityEngine;

namespace GameDev.Slice.Fights
{
    /// <summary>
    /// Every use grows (07.1 §3; owner, 2026-11-13, S5-1; S5b.2): each blow or shot the character makes at someone grows
    /// its weapon's skill and Profile Attribute, a miss half and a critical hit twice, the target's Limit being the task's
    /// stage; each ability it uses grows the Arcana Skill and Intelligence; each treatment First Aid and Dexterity. Only the
    /// character grows, never an NPC, and a blow at the air gives nothing.
    /// </summary>
    public sealed partial class SliceConfront
    {
        /// <summary>The player's blow, a Reaction Attack and a Counterattack too: its weapon by the hand it was struck with.</summary>
        private void TrainStrike(Combatant attacker, Combatant target, StrikeResult result)
        {
            if (attacker == null || attacker.Id != PlayerId || target == null)
            {
                return;
            }

            TrainStrike(attacker.Fighter, result.OffHand, target.Fighter.Limit, Glowing(target), result);
        }

        /// <summary>A blow or shot by <paramref name="attacker"/>, the character, at a target of <paramref name="limit"/>.</summary>
        private void TrainStrike(Fighter attacker, bool offHand, int? limit, bool glowing, StrikeResult result)
        {
            if (_character == null || attacker == null)
            {
                return;
            }

            var set = attacker.Set;
            var weapon = offHand && set.OffWeapon != null ? set.OffWeapon : set.Weapon;
            var skill = weapon != null ? weapon.Skill : CharacterSkill.LightMelee;
            var profile = weapon != null ? weapon.ProfileAttribute : CharacterAttribute.Dexterity;
            _character.Train(skill, Growth.StageOfTarget(limit, glowing), OutcomeOf(result.Hit, result), new[] { profile },
                _settings.FightRules.PerceptionAims ? Perception : null);
        }

        /// <summary>
        /// The player's ability, once a use: the Arcana Skill and Intelligence, the target's Limit its stage, or the
        /// character's own Limit for one with no target (07.1 §3).
        /// </summary>
        private void OnAbilityCast(Combatant user, Combatant target, FightAbility ability, bool worked, StrikeResult result)
        {
            if (_character == null || user == null || user.Id != PlayerId || ability == null)
            {
                return;
            }

            var aimed = target != null && target != user;
            var stage = aimed ? Growth.StageOfTarget(target.Fighter.Limit, Glowing(target)) : PlayerLimit ?? Growth.Stage(1);
            _character.Train(CharacterSkill.Arcana, stage, OutcomeOf(worked, result), new[] { CharacterAttribute.Intelligence },
                aimed && ability.Effect == AbilityEffect.Sting && _settings.FightRules.PerceptionAims ? Perception : null); // only a hit aims
        }

        /// <summary>A treatment in a fight by the character, checked when chosen (07.1 §8a).</summary>
        private void TrainTreatment(Combatant c, Treatment treatment, bool success)
        {
            if (c != null && c.Id == PlayerId && _settings != null)
            {
                SliceFirstAid.Train(_character, _settings.FightRules, treatment, success ? CheckOutcome.Normal : CheckOutcome.Failure);
            }
        }

        private static readonly CharacterAttribute[] Perception = { CharacterAttribute.Perception };

        private static CheckOutcome OutcomeOf(bool worked, StrikeResult result) =>
            !worked ? CheckOutcome.Failure : result.Critical ? CheckOutcome.Critical : CheckOutcome.Normal;

        /// <summary>A glowing beast is a stage 8 task, an ordinary one stage 9 (authoring default, 2026-11-13).</summary>
        private bool Glowing(Combatant c)
        {
            if (c == null || !c.Beast || !_bodies.TryGetValue(c, out var body) || body == null)
            {
                return false;
            }

            var beast = body.GetComponent<SliceBeast>();
            return beast != null && beast.Glowing;
        }
    }
}

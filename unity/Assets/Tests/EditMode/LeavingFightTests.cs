using System;
using System.Collections.Generic;
using GameDev.Confront.Grid;
using GameDev.Confront.Rules;
using GameDev.Confront.Stealth;
using GameDev.Confront.Turns;
using GameDev.Player.Progression;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S3a.9, leaving a fight (08 §7; 10.2 §9), on a floor of 24 × 24 one-metre cells: since the owner's direction of
    /// 2026-11-12 no walk leaves the fight, and Сбежать does, for 2 AP, 15 cells or more from the nearest enemy and in
    /// no enemy's cone, the player having none; a Yield to people costs 2 AP and ends the fight, and the
    /// cult refuses it; a person who is losing asks for mercy and is spared, by the player or by a fighter with no will
    /// of its own; a shooter's step back stays in the fight. The fighter whose turn is tested acts first by Perception.
    /// </summary>
    public sealed class LeavingFightTests
    {
        private const string Data = "Assets/Data/Confront/";

        private FightRules _rules;
        private StealthRules _sight;
        private FightWeapon _sword;
        private FightWeapon _rifle;
        private FightOffHand _pouch;
        private CombatGridReader _grid;
        private TurnEconomy _turns;
        private readonly List<Combatant> _departed = new List<Combatant>();
        private readonly List<Combatant> _asked = new List<Combatant>();
        private int _endedSide;

        [SetUp]
        public void BuildThePlace()
        {
            _rules = AssetDatabase.LoadAssetAtPath<FightRules>(Data + "FightRules.asset");
            _sword = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/Sword.asset");
            _rifle = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/Rifle.asset");
            _pouch = AssetDatabase.LoadAssetAtPath<FightOffHand>(Data + "Weapons/CartridgePouch.asset");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(10f, -0.5f, 10f);
            floor.transform.localScale = new Vector3(24f, 1f, 24f);
            _departed.Clear();
            _asked.Clear();
            _endedSide = -2;
            _sight = null;
        }

        private static CombatCell Cell(int x, int z) => new CombatCell(x, z, 0);

        private Fighter Fighter(int perception = 1, bool shooter = false)
        {
            var sheet = new CharacterSheet();
            foreach (CharacterAttribute a in Enum.GetValues(typeof(CharacterAttribute)))
            {
                sheet.Set(a, 8);
            }

            sheet.Set(CharacterAttribute.Perception, perception);
            sheet.Set(CharacterSkill.MeleeSkill, shooter ? 1 : 8);
            sheet.Set(CharacterSkill.RangedSkill, shooter ? 8 : 1);
            return shooter
                ? _rules.Fighter(sheet, 9, new WeaponSet(_rifle, _pouch), null, new WeaponSet(_sword))
                : _rules.Fighter(sheet, 9, new WeaponSet(_sword), null, default);
        }

        private static Combatant At(int id, int side, Fighter fighter, int x, int z, bool will = false, bool people = false) =>
            new Combatant(id, side, fighter, new Vector3(x + 0.5f, 0f, z + 0.5f), will) { AcceptsYield = people };

        private void Begin(params Combatant[] combatants)
        {
            Physics.SyncTransforms();
            _grid = new CombatGridReader(1f);
            Assert.IsTrue(_grid.Follow(new Vector3(10.5f, 0.05f, 10.5f)), "the grid forms");
            _turns = new TurnEconomy(_rules, _grid) { Stealth = _sight };
            _turns.Departed += c => _departed.Add(c);
            _turns.MercyAsked += c => _asked.Add(c);
            _turns.Ended += side => _endedSide = side;
            _turns.BeginEncounter(combatants, new Random(17));
            Assert.AreEqual(combatants[0], _turns.Acting, "the first named acts first");
        }

        [Test]
        public void BreakingAwayFifteenCellsFromTheNearestCostsTwoApAndEndsTheFight()
        {
            var me = At(0, 0, Fighter(100), 2, 10, will: true);
            var enemy = At(1, 1, Fighter(), 17, 10);
            Begin(me, enemy);
            Assert.AreEqual(15, _rules.BreakAwayCells, "the owner's direction of 2026-11-12");
            Assert.AreEqual(2, _rules.BreakAwayAp);
            var ap = _turns.ApLeft;
            var left = -1;
            _turns.Departed += c => left = _turns.ApLeft;
            Assert.IsTrue(_turns.CanBreakAway(me), "fifteen cells, nobody's cone");
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.BreakAway), "Сбежать");
            Assert.AreEqual(ap - 2, left, "2 AP, spent before it leaves");

            Assert.AreEqual(Departure.BrokeAway, me.Departure, "broken away");
            Assert.AreEqual(CombatantState.Out, me.State, "out of the fight");
            CollectionAssert.AreEqual(new[] { me }, _departed, "the departure is told");
            Assert.IsFalse(_turns.IsRunning, "nobody is left to fight the enemy");
            Assert.AreEqual(1, _endedSide, "the enemy's side stands");
        }

        [Test]
        public void AWalkNeverLeavesTheFightAndNearerThanFifteenCellsNobodyBreaksAway()
        {
            var me = At(0, 0, Fighter(100), 4, 10, will: true);
            var enemy = At(1, 1, Fighter(), 18, 10);
            Begin(me, enemy);
            Assert.IsFalse(_turns.CanBreakAway(me), "fourteen cells");
            var ap = _turns.ApLeft;
            Assert.IsFalse(_turns.SubmitIntent(TurnIntent.BreakAway), "refused");
            Assert.AreEqual(ap, _turns.ApLeft, "nothing is spent");
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.MoveTo(Cell(1, 10))), "three steps back, seventeen cells off");
            Assert.AreEqual(Departure.None, me.Departure, "still in: no walk leaves a fight");
            Assert.IsTrue(_turns.IsRunning && _turns.Acting == me, "the turn goes on");
            Assert.IsTrue(_turns.CanBreakAway(me), "now it may");
        }

        [Test]
        public void AnEnemysConeKeepsBreakingAwayClosed()
        {
            _sight = AssetDatabase.LoadAssetAtPath<StealthRules>(Data + "StealthRules.asset");
            var me = At(0, 0, Fighter(100), 2, 10, will: true);
            var enemy = At(1, 1, Fighter(), 17, 10);
            Begin(me, enemy);
            Assert.IsTrue(_turns.Seen(me), "fifteen cells straight ahead of it, at its cone's reach");
            Assert.IsFalse(_turns.CanBreakAway(me));
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.MoveTo(Cell(2, 4))), "aside");
            Assert.AreEqual(15, _turns.NearestEnemy(me), "fifteen cells as moves count them");
            Assert.IsFalse(_turns.Seen(me), "16.2 m away, beyond the cone's fifteen");
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.BreakAway));
            Assert.AreEqual(Departure.BrokeAway, me.Departure);
        }

        [Test]
        public void ThePlayerHasNoConeSoOnlyItsAlliesSeeAnNpcBreakAway()
        {
            _sight = AssetDatabase.LoadAssetAtPath<StealthRules>(Data + "StealthRules.asset");
            var npc = At(0, 1, Fighter(100), 2, 10, will: true);
            var player = At(1, 0, Fighter(), 17, 10);
            player.Watches = false;
            Begin(npc, player);
            Assert.IsTrue(_turns.CanBreakAway(npc), "the player faces it, but has no cone");
            player.Watches = true;
            Assert.IsFalse(_turns.CanBreakAway(npc), "an ally of the player with a cone would see it");
        }

        [Test]
        public void AWalkTowardAnEnemyFromAfarNeverBreaksAway()
        {
            var c = At(0, 0, Fighter(100), 0, 4);
            var enemy = At(1, 1, Fighter(), 20, 18);
            Begin(c, enemy);
            _turns.PlayTurn();
            Assert.AreEqual(Departure.None, c.Departure, "coming from twenty cells, it stays in");
            Assert.AreEqual(CombatantState.Fighting, c.State);
            Assert.IsTrue(_turns.IsRunning);
        }

        [Test]
        public void AYieldToPeopleCostsTwoApAndEndsTheFight()
        {
            var me = At(0, 0, Fighter(100), 6, 12, will: true);
            var warden = At(1, 1, Fighter(), 10, 10, people: true);
            Begin(me, warden);
            Assert.AreEqual(2, _rules.YieldAp, "10.2 §9");
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Yield), "the Wardens accept");
            Assert.AreEqual(Departure.Yielded, me.Departure);
            CollectionAssert.AreEqual(new[] { me }, _departed, "the Yield is told before the end");
            Assert.IsFalse(_turns.IsRunning, "the fight ends at once");
            Assert.AreEqual(1, _endedSide, "the side yielded to stands");
            Assert.IsFalse(warden.Fighter.Down, "nobody falls");
        }

        [Test]
        public void AYieldWithLessThanTwoApIsRefused()
        {
            var me = At(0, 0, Fighter(100), 6, 12, will: true);
            var warden = At(1, 1, Fighter(), 10, 10, people: true);
            Begin(me, warden);
            var steps = _turns.ApLeft - 1;
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.MoveTo(Cell(6 + steps, 12))), "a walk that leaves 1 AP");
            Assert.AreEqual(1, _turns.ApLeft);
            Assert.IsFalse(_turns.SubmitIntent(TurnIntent.Yield), "1 AP is not enough");
            Assert.IsTrue(_turns.IsRunning);
            Assert.AreEqual(Departure.None, me.Departure);
        }

        [Test]
        public void TheCultTakesNoSurrender()
        {
            var me = At(0, 0, Fighter(100), 6, 12, will: true);
            var cultist = At(1, 1, Fighter(), 10, 10);
            var warden = At(2, 1, Fighter(), 12, 10, people: true);
            Begin(me, cultist, warden);
            var ap = _turns.ApLeft;
            Assert.IsFalse(_turns.SubmitIntent(TurnIntent.Yield), "a cultist among the enemies refuses it");
            Assert.AreEqual(ap, _turns.ApLeft, "nothing is spent");
            Assert.IsTrue(_turns.IsRunning);
        }

        [Test]
        public void ALosingPersonAsksForMercyAndThePlayerSparesIt()
        {
            var warden = At(0, 1, Fighter(100), 10, 10, people: true);
            var me = At(1, 0, Fighter(50), 10, 12, will: true);
            var ally = At(2, 0, Fighter(), 12, 12);
            Begin(warden, me, ally);
            warden.Fighter.Hp = warden.Fighter.MaxHp * 0.2d;
            _turns.PlayTurn();
            Assert.IsTrue(warden.AsksMercy, "a fifth of its HP, one against two: it asks");
            CollectionAssert.AreEqual(new[] { warden }, _asked, "the plea is told");
            Assert.AreEqual(me, _turns.Acting, "its turn passes with nothing struck");
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Spare(warden.Id)), "the player spares it");
            Assert.AreEqual(Departure.Spared, warden.Departure);
            Assert.AreEqual(CombatantState.Out, warden.State, "it leaves");
            Assert.IsFalse(_turns.IsRunning, "nobody is left against the player");
            Assert.AreEqual(0, _endedSide);
        }

        [Test]
        public void APersonWhoIsNotOutnumberedFightsOn()
        {
            var warden = At(0, 1, Fighter(100), 10, 10, people: true);
            var second = At(1, 1, Fighter(), 4, 4, people: true);
            var me = At(2, 0, Fighter(50), 10, 12, will: true);
            Begin(warden, second, me);
            warden.Fighter.Hp = warden.Fighter.MaxHp * 0.2d;
            _turns.PlayTurn();
            Assert.IsFalse(warden.AsksMercy, "two against one: no plea");
            CollectionAssert.IsEmpty(_asked);
        }

        [Test]
        public void TheCultNeverAsksForMercy()
        {
            var cultist = At(0, 1, Fighter(100), 10, 10);
            var me = At(1, 0, Fighter(50), 10, 12, will: true);
            var ally = At(2, 0, Fighter(), 12, 12);
            Begin(cultist, me, ally);
            cultist.Fighter.Hp = cultist.Fighter.MaxHp * 0.2d;
            _turns.PlayTurn();
            Assert.IsFalse(cultist.AsksMercy, "the cult does not bargain");
            CollectionAssert.IsEmpty(_asked);
        }

        [Test]
        public void AFighterWithNoWillSparesThoseWhoAskOnceNobodyElseStands()
        {
            var warden = At(0, 1, Fighter(100), 10, 10, people: true);
            var a = At(1, 0, Fighter(50), 10, 12);
            var b = At(2, 0, Fighter(40), 12, 12);
            Begin(warden, a, b);
            warden.Fighter.Hp = warden.Fighter.MaxHp * 0.2d;
            _turns.PlayTurn();
            Assert.IsTrue(warden.AsksMercy);
            Assert.AreEqual(a, _turns.Acting);
            var hp = warden.Fighter.Hp;
            _turns.PlayTurn();
            Assert.AreEqual(Departure.Spared, warden.Departure, "spared, not struck");
            Assert.AreEqual(hp, warden.Fighter.Hp, "no blow fell on it");
            Assert.IsFalse(_turns.IsRunning, "the fight ends");
            Assert.AreEqual(0, _endedSide);
        }

        [Test]
        public void AShootersStepBackStaysInTheFight()
        {
            var shooter = At(0, 0, Fighter(100, shooter: true), 10, 10);
            var enemy = At(1, 1, Fighter(), 10, 18);
            Begin(shooter, enemy);
            Assert.Greater(_turns.ApLeft - _rifle.AttackAp, 1, "AP left after the shot for two steps or more");
            _turns.PlayTurn();
            Assert.AreEqual(Departure.None, shooter.Departure, "no step leaves the fight");
            Assert.AreEqual(CombatantState.Fighting, shooter.State);
            Assert.Greater(enemy.Cell.Z - shooter.Cell.Z, 8, "it stepped back from the enemy's eight cells");

        }
    }
}

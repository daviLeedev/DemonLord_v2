using System;
using DemonLord.Application.Combat;
using DemonLord.Domain.Combat;
using NUnit.Framework;

namespace DemonLord.Tests.EditMode
{
    public sealed class CombatTrainingBattleTests
    {
        [Test]
        public void NewBattle_PublishesSixDynamicLineRowsAndEnemyIntents()
        {
            CombatTrainingBattle battle = CreateBattle(99);

            Assert.That(battle.Phase, Is.EqualTo(CombatTrainingPhase.Planning));
            Assert.That(battle.Round, Is.EqualTo(1));
            Assert.That(battle.SharedSp, Is.EqualTo(CombatTrainingBattle.StartingSp));
            Assert.That(battle.TimelineEntryCount, Is.EqualTo(CombatTrainingBattle.TimelineCount));
            Assert.That(battle.GetTimelineEntry(0).Side, Is.EqualTo(CombatTimelineSide.Enemy));
            Assert.That(battle.GetTimelineEntry(0).EnemyId, Is.EqualTo(CombatTrainingEnemyId.TraineeSwordsman));
            Assert.That(battle.GetTimelineEntry(0).Initiative, Is.EqualTo(40));
            Assert.That(battle.GetTimelineEntry(1).EnemyId, Is.EqualTo(CombatTrainingEnemyId.TraineeShieldbearer));
            Assert.That(battle.GetTimelineEntry(2).EnemyId, Is.EqualTo(CombatTrainingEnemyId.ApprenticeMage));
            Assert.That(battle.GetTimelineEntry(3).HasAllyAction, Is.False);
            Assert.That(battle.GetEnemyIntent(CombatTrainingEnemyId.ApprenticeMage).TargetKind,
                Is.EqualTo(CombatIntentTargetKind.All));
        }

        [Test]
        public void SlimeSkillChoice_RebuildsInitiativeLineBetweenDifferentEnemyIntents()
        {
            CombatTrainingBattle battle = CreateBattle(99);
            QueuePlan(
                battle,
                CombatTrainingSkillId.SlipperyFloor,
                CombatTrainingSkillId.BoneSpear,
                CombatTrainingSkillId.CoverAmbush,
                CombatTrainingEnemyId.TraineeShieldbearer);

            int slipperyIndex = FindAllyTimelineIndex(battle, CombatTrainingUnitId.Slime001);
            CombatTimelineEntry slippery = battle.GetTimelineEntry(slipperyIndex);
            Assert.That(slippery.Initiative, Is.EqualTo(52));
            Assert.That(slipperyIndex, Is.EqualTo(0));

            Assert.That(
                battle.TrySetAction(
                    CombatTrainingSkillId.ElasticCharge,
                    CombatTrainingEnemyId.TraineeShieldbearer,
                    out _),
                Is.True);

            int chargeIndex = FindAllyTimelineIndex(battle, CombatTrainingUnitId.Slime001);
            CombatTimelineEntry charge = battle.GetTimelineEntry(chargeIndex);
            Assert.That(charge.Initiative, Is.EqualTo(28));
            Assert.That(chargeIndex, Is.GreaterThan(FindEnemyTimelineIndex(battle, CombatTrainingEnemyId.TraineeSwordsman)));
            Assert.That(chargeIndex, Is.LessThan(FindEnemyTimelineIndex(battle, CombatTrainingEnemyId.TraineeShieldbearer)));
            Assert.That(charge.TieBreakOrder, Is.EqualTo((int)CombatTrainingUnitId.Slime001));
        }

        [Test]
        public void DynamicLine_UsesPublicInitiativeThenStableTieBreakOrder()
        {
            CombatTrainingBattle battle = CreateBattle(99);
            QueuePlan(
                battle,
                CombatTrainingSkillId.SlipperyFloor,
                CombatTrainingSkillId.GateDelay,
                CombatTrainingSkillId.StoneVolley,
                CombatTrainingEnemyId.TraineeShieldbearer);

            for (int index = 1; index < battle.TimelineEntryCount; index++)
            {
                CombatTimelineEntry previous = battle.GetTimelineEntry(index - 1);
                CombatTimelineEntry current = battle.GetTimelineEntry(index);
                Assert.That(previous.Initiative, Is.GreaterThanOrEqualTo(current.Initiative));
                if (previous.Initiative == current.Initiative)
                {
                    Assert.That(previous.TieBreakOrder, Is.LessThan(current.TieBreakOrder));
                }
            }
        }

        [Test]
        public void SharedSp_UsesDynamicActionLineAndRejectsManualOrdering()
        {
            CombatTrainingBattle battle = CreateBattle(99);
            QueuePlan(
                battle,
                CombatTrainingSkillId.SlipperyFloor,
                CombatTrainingSkillId.BoneSpear,
                CombatTrainingSkillId.CoverAmbush,
                CombatTrainingEnemyId.TraineeShieldbearer);

            Assert.That(battle.IsPlanComplete, Is.True);
            Assert.That(battle.IsPlanAffordable, Is.True);
            Assert.That(battle.TryMoveAction(0, 2, out string error), Is.False);
            Assert.That(error, Is.EqualTo("combat_manual_order_removed"));

            int slimeSlot = FindAllyStorageSlot(battle, CombatTrainingUnitId.Slime001);
            int skeletonSlot = FindAllyStorageSlot(battle, CombatTrainingUnitId.SkeletonGuard);
            int goblinSlot = FindAllyStorageSlot(battle, CombatTrainingUnitId.GoblinCover);
            Assert.That(battle.GetActionPreview(slimeSlot).ProjectedSpAfter, Is.EqualTo(5));
            Assert.That(battle.GetActionPreview(skeletonSlot).ProjectedSpAfter, Is.EqualTo(3));
            Assert.That(battle.GetActionPreview(goblinSlot).ProjectedSpAfter, Is.EqualTo(0));
        }

        [Test]
        public void GuaranteedCondition_RaisesLaterBonusChanceAcrossEnemyEntry()
        {
            CombatTrainingBattle battle = CreateBattle(99);
            QueuePlan(
                battle,
                CombatTrainingSkillId.SlipperyFloor,
                CombatTrainingSkillId.BoneSpear,
                CombatTrainingSkillId.StoneVolley,
                CombatTrainingEnemyId.ApprenticeMage);

            int slimeSlot = FindAllyStorageSlot(battle, CombatTrainingUnitId.Slime001);
            int skeletonSlot = FindAllyStorageSlot(battle, CombatTrainingUnitId.SkeletonGuard);
            Assert.That(battle.GetActionPreview(slimeSlot).BonusChance, Is.EqualTo(50));
            Assert.That(battle.GetActionPreview(skeletonSlot).BonusChance, Is.EqualTo(70));
            Assert.That(
                FindAllyTimelineIndex(battle, CombatTrainingUnitId.Slime001),
                Is.LessThan(FindAllyTimelineIndex(battle, CombatTrainingUnitId.SkeletonGuard)));
            Assert.That(
                FindEnemyTimelineIndex(battle, CombatTrainingEnemyId.TraineeSwordsman),
                Is.GreaterThan(FindAllyTimelineIndex(battle, CombatTrainingUnitId.Slime001))
                    .And.LessThan(FindAllyTimelineIndex(battle, CombatTrainingUnitId.SkeletonGuard)));
        }

        [Test]
        public void ResolveNextTimelineAction_FollowsSortedAllyEnemyTikiTaka()
        {
            CombatTrainingBattle battle = CreateBattle(99, 99, 99);
            QueuePlan(
                battle,
                CombatTrainingSkillId.SlipperyFloor,
                CombatTrainingSkillId.BoneSpear,
                CombatTrainingSkillId.CoverAmbush,
                CombatTrainingEnemyId.TraineeShieldbearer);
            Assert.That(battle.TryBeginExecution(out _), Is.True);

            Assert.That(battle.TryResolveNextTimelineAction(out CombatTimelineResolution first, out _), Is.True);
            Assert.That(first.TimelineIndex, Is.EqualTo(0));
            Assert.That(first.Side, Is.EqualTo(CombatTimelineSide.Ally));
            Assert.That(first.AllyActorId, Is.EqualTo(CombatTrainingUnitId.Slime001));

            Assert.That(battle.TryResolveNextTimelineAction(out CombatTimelineResolution second, out _), Is.True);
            Assert.That(second.TimelineIndex, Is.EqualTo(1));
            Assert.That(second.Side, Is.EqualTo(CombatTimelineSide.Enemy));
            Assert.That(second.EnemyActorId, Is.EqualTo(CombatTrainingEnemyId.TraineeSwordsman));

            Assert.That(battle.TryResolveNextTimelineAction(out CombatTimelineResolution third, out _), Is.True);
            Assert.That(third.TimelineIndex, Is.EqualTo(2));
            Assert.That(third.Side, Is.EqualTo(CombatTimelineSide.Ally));
            Assert.That(third.AllyActorId, Is.EqualTo(CombatTrainingUnitId.SkeletonGuard));
        }

        [Test]
        public void BasePackage_AppliesBeforeExactlyOneBonusRoll()
        {
            CombatTrainingBattle battle = null;
            int rolls = 0;
            var random = new CallbackRandom(() =>
            {
                rolls++;
                Assert.That(battle.SharedSp, Is.EqualTo(5));
                Assert.That(battle.GetEnemyHp(CombatTrainingEnemyId.ApprenticeMage), Is.EqualTo(91));
                Assert.That(
                    (battle.GetEnemyConditions(CombatTrainingEnemyId.ApprenticeMage) & CombatCondition.Sticky) != 0,
                    Is.True);
                return 99;
            });
            battle = new CombatTrainingBattle(random);
            QueuePlan(
                battle,
                CombatTrainingSkillId.SlipperyFloor,
                CombatTrainingSkillId.GateDelay,
                CombatTrainingSkillId.StoneVolley,
                CombatTrainingEnemyId.ApprenticeMage);
            Assert.That(battle.TryBeginExecution(out _), Is.True);

            Assert.That(battle.TryResolveNextTimelineAction(out CombatTimelineResolution result, out _), Is.True);
            Assert.That(result.BonusTriggered, Is.False);
            Assert.That(result.BaseDamage, Is.EqualTo(14));
            Assert.That(rolls, Is.EqualTo(1));
        }

        [Test]
        public void DefeatedEnemy_DoesNotSilentlyRetargetItsPublishedDynamicIntent()
        {
            CombatTrainingBattle battle = CreateBattle(0, 0, 0);
            QueuePlan(
                battle,
                CombatTrainingSkillId.SlipperyFloor,
                CombatTrainingSkillId.GateDelay,
                CombatTrainingSkillId.CoverAmbush,
                CombatTrainingEnemyId.ApprenticeMage);
            Assert.That(battle.TryBeginExecution(out _), Is.True);

            Assert.That(battle.TryResolveNextTimelineAction(out CombatTimelineResolution floor, out _), Is.True);
            Assert.That(floor.AllyActorId, Is.EqualTo(CombatTrainingUnitId.Slime001));
            Assert.That(battle.TryResolveNextTimelineAction(out CombatTimelineResolution swordsman, out _), Is.True);
            Assert.That(swordsman.EnemyActorId, Is.EqualTo(CombatTrainingEnemyId.TraineeSwordsman));
            Assert.That(battle.TryResolveNextTimelineAction(out CombatTimelineResolution delay, out _), Is.True);
            Assert.That(delay.AllyActorId, Is.EqualTo(CombatTrainingUnitId.SkeletonGuard));
            Assert.That(battle.TryResolveNextTimelineAction(out CombatTimelineResolution shield, out _), Is.True);
            Assert.That(shield.EnemyActorId, Is.EqualTo(CombatTrainingEnemyId.TraineeShieldbearer));
            Assert.That(battle.TryResolveNextTimelineAction(out CombatTimelineResolution cover, out _), Is.True);
            Assert.That(cover.AllyActorId, Is.EqualTo(CombatTrainingUnitId.GoblinCover));
            Assert.That(battle.IsEnemyAlive(CombatTrainingEnemyId.ApprenticeMage), Is.False);
            Assert.That(battle.TryResolveNextTimelineAction(out CombatTimelineResolution mage, out _), Is.True);
            Assert.That(mage.SkipReason, Is.EqualTo(CombatTimelineSkipReason.EnemyDefeated));
        }

        [Test]
        public void CompletingDynamicLine_RecoversSpClearsPlanAndStartsNextRound()
        {
            CombatTrainingBattle battle = CreateBattle(99, 99, 99);
            QueuePlan(
                battle,
                CombatTrainingSkillId.SlipperyFloor,
                CombatTrainingSkillId.GateDelay,
                CombatTrainingSkillId.StoneVolley,
                CombatTrainingEnemyId.TraineeShieldbearer);
            Assert.That(battle.TryBeginExecution(out _), Is.True);

            for (int index = 0; index < CombatTrainingBattle.TimelineCount; index++)
            {
                Assert.That(battle.TryResolveNextTimelineAction(out _, out _), Is.True);
            }

            Assert.That(battle.Phase, Is.EqualTo(CombatTrainingPhase.Planning));
            Assert.That(battle.Round, Is.EqualTo(2));
            Assert.That(battle.PlannedActionCount, Is.EqualTo(0));
            Assert.That(battle.SharedSp, Is.EqualTo(7));
        }

        [Test]
        public void Session_ForwardsUnifiedDynamicTimelineResolution()
        {
            CombatTrainingSession session = new CombatTrainingSession(new SequenceRandom(99, 99, 99));
            QueuePlan(
                session.Battle,
                CombatTrainingSkillId.SlipperyFloor,
                CombatTrainingSkillId.BoneSpear,
                CombatTrainingSkillId.StoneVolley,
                CombatTrainingEnemyId.TraineeShieldbearer);
            Assert.That(session.TryBeginExecution(out _), Is.True);
            Assert.That(session.TryResolveNextTimelineAction(out CombatTimelineResolution result, out _), Is.True);
            Assert.That(result.Side, Is.EqualTo(CombatTimelineSide.Ally));
            Assert.That(result.AllyActorId, Is.EqualTo(CombatTrainingUnitId.Slime001));
        }

        private static CombatTrainingBattle CreateBattle(params int[] rolls)
        {
            return new CombatTrainingBattle(new SequenceRandom(rolls));
        }

        private static void QueuePlan(
            CombatTrainingBattle battle,
            CombatTrainingSkillId slimeSkill,
            CombatTrainingSkillId skeletonSkill,
            CombatTrainingSkillId goblinSkill,
            CombatTrainingEnemyId target)
        {
            Assert.That(battle.TrySetAction(slimeSkill, target, out _), Is.True);
            Assert.That(battle.TrySetAction(skeletonSkill, target, out _), Is.True);
            Assert.That(battle.TrySetAction(goblinSkill, target, out _), Is.True);
        }

        private static int FindAllyStorageSlot(CombatTrainingBattle battle, CombatTrainingUnitId unitId)
        {
            for (int slot = 0; slot < CombatTrainingBattle.AllySlotCount; slot++)
            {
                if (!battle.IsAllySlotOccupied(slot))
                {
                    continue;
                }

                CombatTrainingSkillDefinition skill = CombatTrainingBattle.GetSkillDefinition(battle.GetPlannedAction(slot).SkillId);
                if (skill.ActorId == unitId)
                {
                    return slot;
                }
            }

            Assert.Fail("Planned action not found for " + unitId + ".");
            return -1;
        }

        private static int FindAllyTimelineIndex(CombatTrainingBattle battle, CombatTrainingUnitId unitId)
        {
            for (int index = 0; index < battle.TimelineEntryCount; index++)
            {
                CombatTimelineEntry entry = battle.GetTimelineEntry(index);
                if (entry.Side != CombatTimelineSide.Ally || !entry.HasAllyAction)
                {
                    continue;
                }

                CombatTrainingSkillDefinition skill = CombatTrainingBattle.GetSkillDefinition(entry.AllyAction.SkillId);
                if (skill.ActorId == unitId)
                {
                    return index;
                }
            }

            Assert.Fail("Timeline ally action not found for " + unitId + ".");
            return -1;
        }

        private static int FindEnemyTimelineIndex(CombatTrainingBattle battle, CombatTrainingEnemyId enemyId)
        {
            for (int index = 0; index < battle.TimelineEntryCount; index++)
            {
                CombatTimelineEntry entry = battle.GetTimelineEntry(index);
                if (entry.Side == CombatTimelineSide.Enemy && entry.EnemyId == enemyId)
                {
                    return index;
                }
            }

            Assert.Fail("Timeline enemy action not found for " + enemyId + ".");
            return -1;
        }

        private sealed class SequenceRandom : ICombatRandomSource
        {
            private readonly int[] values;
            private int index;

            public SequenceRandom(params int[] values)
            {
                this.values = values != null && values.Length > 0 ? values : new[] { 99 };
            }

            public int NextPercent()
            {
                int value = values[Math.Min(index, values.Length - 1)];
                index++;
                return value;
            }
        }

        private sealed class CallbackRandom : ICombatRandomSource
        {
            private readonly Func<int> callback;

            public CallbackRandom(Func<int> callback)
            {
                this.callback = callback ?? throw new ArgumentNullException(nameof(callback));
            }

            public int NextPercent()
            {
                return callback();
            }
        }
    }
}

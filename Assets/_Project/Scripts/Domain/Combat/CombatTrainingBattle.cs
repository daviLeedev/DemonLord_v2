using System;
using System.Collections.Generic;

namespace DemonLord.Domain.Combat
{
    /// <summary>
    /// The liaison battle has one shared, initiative-sorted action line. Three selected ally
    /// actions and three published enemy intents occupy its six rows. There is no reaction phase.
    /// </summary>
    public enum CombatTrainingPhase
    {
        Planning = 0,
        Resolving = 1,
        Victory = 2,
        Defeat = 3,
    }

    [Flags]
    public enum CombatCondition
    {
        None = 0,
        Sticky = 1 << 0,
        Exposed = 1 << 1,
        Marked = 1 << 2,
        Suppressed = 1 << 3,
    }

    public enum CombatTrainingUnitId
    {
        Slime001 = 0,
        SkeletonGuard = 1,
        GoblinCover = 2,
    }

    public enum CombatTrainingEnemyId
    {
        TraineeSwordsman = 0,
        TraineeShieldbearer = 1,
        ApprenticeMage = 2,
    }

    public enum CombatTrainingSkillId
    {
        SlipperyFloor = 0,
        ElasticCharge = 1,
        GateDelay = 2,
        BoneSpear = 3,
        CoverAmbush = 4,
        StoneVolley = 5,
    }

    public enum CombatIntentTargetKind
    {
        Single = 0,
        All = 1,
    }

    public enum CombatTimelineSide
    {
        Ally = 0,
        Enemy = 1,
    }

    public enum CombatTimelineSkipReason
    {
        None = 0,
        EmptySlot = 1,
        ActorDefeated = 2,
        TargetUnavailable = 3,
        EnemyDefeated = 4,
        InsufficientSp = 5,
    }

    public interface ICombatRandomSource
    {
        /// <summary>Returns one integer from 0 through 99.</summary>
        int NextPercent();
    }

    public readonly struct CombatTrainingUnitDefinition
    {
        public CombatTrainingUnitDefinition(
            CombatTrainingUnitId id,
            string displayName,
            string battleLabel,
            int maximumHp,
            int baseSpeed = 0)
        {
            Id = id;
            DisplayName = displayName;
            BattleLabel = battleLabel;
            MaximumHp = maximumHp;
            BaseSpeed = baseSpeed != 0 ? baseSpeed : GetDefaultBaseSpeed(id);
        }

        public CombatTrainingUnitId Id { get; }
        public string DisplayName { get; }
        public string BattleLabel { get; }
        public int MaximumHp { get; }
        public int BaseSpeed { get; }

        private static int GetDefaultBaseSpeed(CombatTrainingUnitId id)
        {
            switch (id)
            {
                case CombatTrainingUnitId.Slime001:
                    return 34;
                case CombatTrainingUnitId.SkeletonGuard:
                    return 30;
                case CombatTrainingUnitId.GoblinCover:
                    return 27;
                default:
                    return 0;
            }
        }
    }

    public readonly struct CombatTrainingEnemyDefinition
    {
        public CombatTrainingEnemyDefinition(
            CombatTrainingEnemyId id,
            string displayName,
            string battleLabel,
            int maximumHp,
            int baseSpeed = 0)
        {
            Id = id;
            DisplayName = displayName;
            BattleLabel = battleLabel;
            MaximumHp = maximumHp;
            BaseSpeed = baseSpeed != 0 ? baseSpeed : GetDefaultBaseSpeed(id);
        }

        public CombatTrainingEnemyId Id { get; }
        public string DisplayName { get; }
        public string BattleLabel { get; }
        public int MaximumHp { get; }
        public int BaseSpeed { get; }

        private static int GetDefaultBaseSpeed(CombatTrainingEnemyId id)
        {
            switch (id)
            {
                case CombatTrainingEnemyId.TraineeSwordsman:
                    return 37;
                case CombatTrainingEnemyId.TraineeShieldbearer:
                    return 27;
                case CombatTrainingEnemyId.ApprenticeMage:
                    return 21;
                default:
                    return 0;
            }
        }
    }

    public readonly struct CombatTrainingSkillDefinition
    {
        public CombatTrainingSkillDefinition(
            CombatTrainingSkillId id,
            CombatTrainingUnitId actorId,
            string displayName,
            string description,
            int spCost,
            int spGain,
            int basePower,
            int baseBonusChance,
            int bonusPower,
            CombatCondition appliedCondition,
            int baseSuppression,
            int bonusSuppression,
            int initiativePriority = 0)
        {
            Id = id;
            ActorId = actorId;
            DisplayName = displayName;
            Description = description;
            SpCost = spCost;
            SpGain = spGain;
            BasePower = basePower;
            BaseBonusChance = baseBonusChance;
            BonusPower = bonusPower;
            AppliedCondition = appliedCondition;
            BaseSuppression = baseSuppression;
            BonusSuppression = bonusSuppression;
            InitiativePriority = initiativePriority != 0 ? initiativePriority : GetDefaultInitiativePriority(id);
        }

        public CombatTrainingSkillId Id { get; }
        public CombatTrainingUnitId ActorId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int SpCost { get; }
        public int SpGain { get; }
        public int BasePower { get; }
        public int BaseBonusChance { get; }
        public int BonusPower { get; }
        public CombatCondition AppliedCondition { get; }
        public int BaseSuppression { get; }
        public int BonusSuppression { get; }
        public int InitiativePriority { get; }

        private static int GetDefaultInitiativePriority(CombatTrainingSkillId id)
        {
            switch (id)
            {
                case CombatTrainingSkillId.SlipperyFloor:
                    return 18;
                case CombatTrainingSkillId.ElasticCharge:
                    return -6;
                case CombatTrainingSkillId.GateDelay:
                    return 5;
                case CombatTrainingSkillId.CoverAmbush:
                    return -2;
                case CombatTrainingSkillId.StoneVolley:
                    return 9;
                default:
                    return 0;
            }
        }
    }

    public readonly struct CombatTrainingEnemyIntent
    {
        public CombatTrainingEnemyIntent(
            CombatTrainingEnemyId enemyId,
            string displayName,
            string description,
            CombatIntentTargetKind targetKind,
            CombatTrainingUnitId targetUnitId,
            int baseDamage,
            int initiativePriority = 0)
        {
            EnemyId = enemyId;
            DisplayName = displayName;
            Description = description;
            TargetKind = targetKind;
            TargetUnitId = targetUnitId;
            BaseDamage = baseDamage;
            InitiativePriority = initiativePriority != 0 ? initiativePriority : GetDefaultInitiativePriority(enemyId);
        }

        public CombatTrainingEnemyId EnemyId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public CombatIntentTargetKind TargetKind { get; }
        public CombatTrainingUnitId TargetUnitId { get; }
        public int BaseDamage { get; }
        public int InitiativePriority { get; }

        private static int GetDefaultInitiativePriority(CombatTrainingEnemyId enemyId)
        {
            switch (enemyId)
            {
                case CombatTrainingEnemyId.TraineeSwordsman:
                    return 3;
                case CombatTrainingEnemyId.TraineeShieldbearer:
                case CombatTrainingEnemyId.ApprenticeMage:
                    return -1;
                default:
                    return 0;
            }
        }
    }

    public readonly struct CombatPlannedAction
    {
        public CombatPlannedAction(CombatTrainingSkillId skillId, CombatTrainingEnemyId targetEnemyId)
        {
            SkillId = skillId;
            TargetEnemyId = targetEnemyId;
        }

        public CombatTrainingSkillId SkillId { get; }
        public CombatTrainingEnemyId TargetEnemyId { get; }
    }

    public readonly struct CombatActionPreview
    {
        public CombatActionPreview(
            int allySlotIndex,
            int timelineIndex,
            CombatPlannedAction action,
            int projectedSpBefore,
            int projectedSpAfter,
            bool canPay,
            int minimumBonusChance,
            int maximumBonusChance,
            bool mayBeCancelled,
            int initiative = 0,
            int tieBreakOrder = 0)
        {
            AllySlotIndex = allySlotIndex;
            TimelineIndex = timelineIndex;
            Action = action;
            ProjectedSpBefore = projectedSpBefore;
            ProjectedSpAfter = projectedSpAfter;
            CanPay = canPay;
            MinimumBonusChance = minimumBonusChance;
            MaximumBonusChance = maximumBonusChance;
            MayBeCancelled = mayBeCancelled;
            Initiative = initiative;
            TieBreakOrder = tieBreakOrder;
        }

        public int AllySlotIndex { get; }
        public int TimelineIndex { get; }
        public CombatPlannedAction Action { get; }
        public int ProjectedSpBefore { get; }
        public int ProjectedSpAfter { get; }
        public bool CanPay { get; }
        public int MinimumBonusChance { get; }
        public int MaximumBonusChance { get; }
        public int BonusChance => MaximumBonusChance;
        public bool HasChanceRange => MinimumBonusChance != MaximumBonusChance;
        public bool MayBeCancelled { get; }
        public int Initiative { get; }
        public int TieBreakOrder { get; }
    }

    public readonly struct CombatTimelineEntry
    {
        public CombatTimelineEntry(
            int timelineIndex,
            CombatTimelineSide side,
            int allySlotIndex,
            bool hasAllyAction,
            CombatPlannedAction allyAction,
            CombatTrainingEnemyId enemyId,
            CombatTrainingEnemyIntent enemyIntent,
            bool wasResolved,
            CombatTimelineSkipReason skipReason,
            int initiative = 0,
            int tieBreakOrder = 0)
        {
            TimelineIndex = timelineIndex;
            Side = side;
            AllySlotIndex = allySlotIndex;
            HasAllyAction = hasAllyAction;
            AllyAction = allyAction;
            EnemyId = enemyId;
            EnemyIntent = enemyIntent;
            WasResolved = wasResolved;
            SkipReason = skipReason;
            Initiative = initiative;
            TieBreakOrder = tieBreakOrder;
        }

        public int TimelineIndex { get; }
        public CombatTimelineSide Side { get; }
        public int AllySlotIndex { get; }
        public bool HasAllyAction { get; }
        public CombatPlannedAction AllyAction { get; }
        public CombatTrainingEnemyId EnemyId { get; }
        public CombatTrainingEnemyIntent EnemyIntent { get; }
        public bool WasResolved { get; }
        public CombatTimelineSkipReason SkipReason { get; }
        public bool IsCancelled => SkipReason != CombatTimelineSkipReason.None;
        public int Initiative { get; }
        public int TieBreakOrder { get; }
    }

    /// <summary>
    /// A single resolved entry. The presentation layer receives this one union instead of separate
    /// ally and enemy phases, so playback follows the sorted action line exactly.
    /// </summary>
    public readonly struct CombatTimelineResolution
    {
        public CombatTimelineResolution(
            int timelineIndex,
            CombatTimelineSide side,
            CombatTimelineSkipReason skipReason,
            CombatTrainingUnitId allyActorId,
            CombatTrainingEnemyId enemyActorId,
            CombatTrainingEnemyId targetEnemyId,
            CombatTrainingUnitId targetUnitId,
            int spBefore,
            int spAfter,
            int bonusChance,
            int roll,
            int baseDamage,
            bool bonusTriggered,
            int bonusDamage,
            CombatCondition appliedCondition,
            int suppressionApplied,
            int slimeDamage,
            int skeletonDamage,
            int goblinDamage,
            bool targetDefeated)
        {
            TimelineIndex = timelineIndex;
            Side = side;
            SkipReason = skipReason;
            AllyActorId = allyActorId;
            EnemyActorId = enemyActorId;
            TargetEnemyId = targetEnemyId;
            TargetUnitId = targetUnitId;
            SpBefore = spBefore;
            SpAfter = spAfter;
            BonusChance = bonusChance;
            Roll = roll;
            BaseDamage = baseDamage;
            BonusTriggered = bonusTriggered;
            BonusDamage = bonusDamage;
            AppliedCondition = appliedCondition;
            SuppressionApplied = suppressionApplied;
            SlimeDamage = slimeDamage;
            SkeletonDamage = skeletonDamage;
            GoblinDamage = goblinDamage;
            TargetDefeated = targetDefeated;
        }

        public int TimelineIndex { get; }
        public CombatTimelineSide Side { get; }
        public CombatTimelineSkipReason SkipReason { get; }
        public bool Skipped => SkipReason != CombatTimelineSkipReason.None;
        public CombatTrainingUnitId AllyActorId { get; }
        public CombatTrainingEnemyId EnemyActorId { get; }
        public CombatTrainingEnemyId TargetEnemyId { get; }
        public CombatTrainingUnitId TargetUnitId { get; }
        public int SpBefore { get; }
        public int SpAfter { get; }
        public int BonusChance { get; }
        public int Roll { get; }
        public int BaseDamage { get; }
        public bool BonusTriggered { get; }
        public int BonusDamage { get; }
        public int TotalDamage => BaseDamage + BonusDamage;
        public CombatCondition AppliedCondition { get; }
        public int SuppressionApplied { get; }
        public int SlimeDamage { get; }
        public int SkeletonDamage { get; }
        public int GoblinDamage { get; }
        public int IncomingTotalDamage => SlimeDamage + SkeletonDamage + GoblinDamage;
        public bool TargetDefeated { get; }

        public int GetIncomingDamage(CombatTrainingUnitId unitId)
        {
            switch (unitId)
            {
                case CombatTrainingUnitId.Slime001:
                    return SlimeDamage;
                case CombatTrainingUnitId.SkeletonGuard:
                    return SkeletonDamage;
                case CombatTrainingUnitId.GoblinCover:
                    return GoblinDamage;
                default:
                    throw new ArgumentOutOfRangeException(nameof(unitId));
            }
        }
    }

    public sealed class CombatTrainingBattle
    {
        public const int UnitCount = 3;
        public const int EnemyCount = 3;
        public const int SkillCount = 6;
        public const int AllySlotCount = 3;
        public const int TimelineCount = 6;
        public const int StartingSp = 4;
        public const int MaximumSp = 8;
        public const int RoundSpRecovery = 2;

        private sealed class PlanningSimulationState
        {
            public PlanningSimulationState(
                int sharedSp,
                int[] allyHp,
                int[] enemyHp,
                CombatCondition[] conditions,
                int[] suppression)
            {
                SharedSp = sharedSp;
                AllyHp = allyHp;
                EnemyHp = enemyHp;
                Conditions = conditions;
                Suppression = suppression;
            }

            public int SharedSp { get; set; }
            public int[] AllyHp { get; }
            public int[] EnemyHp { get; }
            public CombatCondition[] Conditions { get; }
            public int[] Suppression { get; }

            public PlanningSimulationState Clone()
            {
                return new PlanningSimulationState(
                    SharedSp,
                    (int[])AllyHp.Clone(),
                    (int[])EnemyHp.Clone(),
                    (CombatCondition[])Conditions.Clone(),
                    (int[])Suppression.Clone());
            }
        }

        private readonly ICombatRandomSource randomSource;
        private readonly int[] allyHp = new int[UnitCount];
        private readonly int[] enemyHp = new int[EnemyCount];
        private readonly CombatCondition[] enemyConditions = new CombatCondition[EnemyCount];
        private readonly int[] enemySuppression = new int[EnemyCount];
        private readonly CombatPlannedAction[] allyActions = new CombatPlannedAction[AllySlotCount];
        private readonly bool[] allySlotOccupied = new bool[AllySlotCount];
        private readonly CombatTrainingEnemyIntent[] enemyIntents = new CombatTrainingEnemyIntent[EnemyCount];
        // The selected actions are stored in three editor slots, but these slots never determine
        // execution order. Every plan change rebuilds this public, initiative-sorted action line.
        private readonly CombatTimelineEntry[] actionLine = new CombatTimelineEntry[TimelineCount];
        private readonly bool[] timelineResolved = new bool[TimelineCount];
        private readonly CombatTimelineSkipReason[] timelineSkipReasons = new CombatTimelineSkipReason[TimelineCount];

        private int resolutionIndex;

        public CombatTrainingBattle(ICombatRandomSource randomSource)
        {
            this.randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
            Reset();
        }

        public CombatTrainingPhase Phase { get; private set; }
        public int Round { get; private set; }
        public int SharedSp { get; private set; }
        public int ResolutionIndex => resolutionIndex;
        public int TimelineEntryCount => TimelineCount;
        public int ActionTimelineCount => TimelineCount;
        public bool IsFinished => Phase == CombatTrainingPhase.Victory || Phase == CombatTrainingPhase.Defeat;
        public int RequiredActionCount => AliveAllyCount;

        public int PlannedActionCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < AllySlotCount; index++)
                {
                    if (allySlotOccupied[index])
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int AliveAllyCount => CountAlive(allyHp);
        public int AliveEnemyCount => CountAlive(enemyHp);

        public bool IsPlanComplete
        {
            get
            {
                if (Phase != CombatTrainingPhase.Planning || AliveAllyCount <= 0)
                {
                    return false;
                }

                for (int unitIndex = 0; unitIndex < UnitCount; unitIndex++)
                {
                    if (allyHp[unitIndex] > 0 && !IsUnitPlanned((CombatTrainingUnitId)unitIndex))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool IsPlanAffordable
        {
            get
            {
                if (Phase != CombatTrainingPhase.Planning)
                {
                    return false;
                }

                int projectedSp = SharedSp;
                for (int timelineIndex = 0; timelineIndex < TimelineCount; timelineIndex++)
                {
                    CombatTimelineEntry entry = actionLine[timelineIndex];
                    if (!entry.HasAllyAction)
                    {
                        continue;
                    }

                    CombatTrainingSkillDefinition skill = GetSkillDefinition(entry.AllyAction.SkillId);
                    if (projectedSp < skill.SpCost)
                    {
                        return false;
                    }

                    projectedSp = Math.Min(MaximumSp, projectedSp - skill.SpCost + skill.SpGain);
                }

                return true;
            }
        }

        public bool CanExecutePlan => IsPlanComplete && IsPlanAffordable;

        public void Reset()
        {
            Phase = CombatTrainingPhase.Planning;
            Round = 1;
            SharedSp = StartingSp;
            resolutionIndex = 0;
            ClearPlan();
            ClearRoundConditions();
            ClearTimelineResolution();

            for (int index = 0; index < UnitCount; index++)
            {
                allyHp[index] = GetUnitDefinition((CombatTrainingUnitId)index).MaximumHp;
            }

            for (int index = 0; index < EnemyCount; index++)
            {
                enemyHp[index] = GetEnemyDefinition((CombatTrainingEnemyId)index).MaximumHp;
            }

            BuildEnemyIntents();
            RebuildActionLine();
        }

        public bool TrySetAction(
            CombatTrainingSkillId skillId,
            CombatTrainingEnemyId targetEnemyId,
            out string errorCode)
        {
            errorCode = null;
            if (Phase != CombatTrainingPhase.Planning)
            {
                errorCode = "combat_plan_not_available";
                return false;
            }

            if (!TryGetSkillDefinition(skillId, out CombatTrainingSkillDefinition skill))
            {
                errorCode = "combat_skill_invalid";
                return false;
            }

            if (!IsUnitAlive(skill.ActorId))
            {
                errorCode = "combat_actor_defeated";
                return false;
            }

            if (!IsEnemyAlive(targetEnemyId))
            {
                errorCode = "combat_target_unavailable";
                return false;
            }

            int slot = FindActionSlotByActor(skill.ActorId);
            if (slot < 0)
            {
                slot = FindFirstEmptyAllySlot();
                if (slot < 0)
                {
                    errorCode = "combat_plan_full";
                    return false;
                }
            }

            allyActions[slot] = new CombatPlannedAction(skillId, targetEnemyId);
            allySlotOccupied[slot] = true;
            RebuildActionLine();
            return true;
        }

        public bool TryRemoveAction(int allySlotIndex, out string errorCode)
        {
            errorCode = null;
            if (Phase != CombatTrainingPhase.Planning
                || !IsValidAllySlot(allySlotIndex)
                || !allySlotOccupied[allySlotIndex])
            {
                errorCode = "combat_plan_remove_unavailable";
                return false;
            }

            allySlotOccupied[allySlotIndex] = false;
            allyActions[allySlotIndex] = default;
            RebuildActionLine();
            return true;
        }

        /// <summary>
        /// Manual queue ordering was intentionally removed. Skill and intent initiative determine
        /// the public action line, so this compatibility entry point never performs a hidden swap.
        /// </summary>
        public bool TryMoveAction(int fromAllySlotIndex, int toAllySlotIndex, out string errorCode)
        {
            errorCode = "combat_manual_order_removed";
            return false;
        }

        public bool IsAllySlotOccupied(int allySlotIndex)
        {
            return IsValidAllySlot(allySlotIndex) && allySlotOccupied[allySlotIndex];
        }

        public CombatPlannedAction GetPlannedAction(int allySlotIndex)
        {
            if (!IsValidAllySlot(allySlotIndex) || !allySlotOccupied[allySlotIndex])
            {
                throw new ArgumentOutOfRangeException(nameof(allySlotIndex));
            }

            return allyActions[allySlotIndex];
        }

        public CombatActionPreview GetActionPreview(int allySlotIndex)
        {
            if (!IsValidAllySlot(allySlotIndex) || !allySlotOccupied[allySlotIndex])
            {
                throw new ArgumentOutOfRangeException(nameof(allySlotIndex));
            }

            CombatPlannedAction action = allyActions[allySlotIndex];
            CombatTrainingSkillDefinition skill = GetSkillDefinition(action.SkillId);
            int timelineIndex = FindTimelineIndexForAllySlot(allySlotIndex);
            CombatTimelineEntry entry = actionLine[timelineIndex];
            GetProjectedSpForTimelineEntry(timelineIndex, out int spBefore, out int projectedSp, out bool canPay);

            List<PlanningSimulationState> states = SimulatePlanningStatesBefore(timelineIndex);
            int minimumChance = 100;
            int maximumChance = 0;
            bool foundExecutableState = false;
            bool mayBeCancelled = false;
            for (int index = 0; index < states.Count; index++)
            {
                PlanningSimulationState state = states[index];
                if (state.AllyHp[(int)skill.ActorId] <= 0
                    || state.EnemyHp[(int)action.TargetEnemyId] <= 0
                    || state.SharedSp < skill.SpCost)
                {
                    mayBeCancelled = true;
                    continue;
                }

                foundExecutableState = true;
                int chance = CalculateBonusChance(skill.Id, state.Conditions[(int)action.TargetEnemyId]);
                minimumChance = Math.Min(minimumChance, chance);
                maximumChance = Math.Max(maximumChance, chance);
            }

            if (!foundExecutableState)
            {
                minimumChance = 0;
                maximumChance = 0;
            }

            return new CombatActionPreview(
                allySlotIndex,
                timelineIndex,
                action,
                spBefore,
                projectedSp,
                canPay,
                minimumChance,
                maximumChance,
                mayBeCancelled,
                entry.Initiative,
                entry.TieBreakOrder);
        }

        public void GetProjectedEnemyDamageRange(
            CombatTrainingEnemyId enemyId,
            out int minimumDamage,
            out int maximumDamage,
            out bool mayBeCancelled)
        {
            ValidateEnemy(enemyId);
            CombatTrainingEnemyIntent intent = enemyIntents[(int)enemyId];
            int timelineIndex = FindTimelineIndexForEnemy(enemyId);
            List<PlanningSimulationState> states = Phase == CombatTrainingPhase.Planning
                ? SimulatePlanningStatesBefore(timelineIndex)
                : new List<PlanningSimulationState> { CaptureSimulationState() };

            minimumDamage = int.MaxValue;
            maximumDamage = int.MinValue;
            mayBeCancelled = false;
            for (int index = 0; index < states.Count; index++)
            {
                PlanningSimulationState state = states[index];
                bool canResolve = state.EnemyHp[(int)enemyId] > 0;
                if (canResolve && intent.TargetKind == CombatIntentTargetKind.Single)
                {
                    canResolve = state.AllyHp[(int)intent.TargetUnitId] > 0;
                }
                else if (canResolve && intent.TargetKind == CombatIntentTargetKind.All)
                {
                    canResolve = CountAlive(state.AllyHp) > 0;
                }

                int damage = canResolve
                    ? Math.Max(0, intent.BaseDamage - state.Suppression[(int)enemyId])
                    : 0;
                minimumDamage = Math.Min(minimumDamage, damage);
                maximumDamage = Math.Max(maximumDamage, damage);
                mayBeCancelled |= !canResolve;
            }

            if (minimumDamage == int.MaxValue)
            {
                minimumDamage = 0;
                maximumDamage = 0;
                mayBeCancelled = true;
            }
        }

        public bool TryBeginExecution(out string errorCode)
        {
            errorCode = null;
            if (Phase != CombatTrainingPhase.Planning)
            {
                errorCode = "combat_execution_not_available";
                return false;
            }

            if (!IsPlanComplete)
            {
                errorCode = "combat_plan_incomplete";
                return false;
            }

            if (!IsPlanAffordable)
            {
                errorCode = "combat_sp_sequence_invalid";
                return false;
            }

            resolutionIndex = 0;
            ClearTimelineResolution();
            Phase = CombatTrainingPhase.Resolving;
            return true;
        }

        public bool TryResolveNextTimelineAction(
            out CombatTimelineResolution resolution,
            out string errorCode)
        {
            resolution = default;
            errorCode = null;
            if (Phase != CombatTrainingPhase.Resolving
                || resolutionIndex < 0
                || resolutionIndex >= TimelineCount)
            {
                errorCode = "combat_timeline_resolution_not_available";
                return false;
            }

            int timelineIndex = resolutionIndex;
            CombatTimelineEntry entry = actionLine[timelineIndex];
            resolution = entry.Side == CombatTimelineSide.Ally
                ? entry.HasAllyAction
                    ? ResolveAllyTimelineSlot(timelineIndex, entry.AllySlotIndex)
                    : CreateSkippedAllyResolution(timelineIndex, CombatTimelineSkipReason.EmptySlot)
                : ResolveEnemyTimelineSlot(timelineIndex, entry.EnemyId);
            timelineResolved[timelineIndex] = true;
            timelineSkipReasons[timelineIndex] = resolution.SkipReason;
            CompleteTimelineStep();
            return true;
        }

        public CombatTimelineEntry GetTimelineEntry(int timelineIndex)
        {
            if (timelineIndex < 0 || timelineIndex >= TimelineCount)
            {
                throw new ArgumentOutOfRangeException(nameof(timelineIndex));
            }

            CombatTimelineEntry source = actionLine[timelineIndex];
            bool resolved = timelineResolved[timelineIndex];
            CombatTimelineSkipReason reason = resolved
                ? timelineSkipReasons[timelineIndex]
                : source.Side == CombatTimelineSide.Ally && source.HasAllyAction
                    ? GetCurrentAllySlotSkipReason(source.AllySlotIndex)
                    : source.Side == CombatTimelineSide.Enemy
                        ? GetCurrentEnemySkipReason(source.EnemyId)
                        : CombatTimelineSkipReason.EmptySlot;
            return new CombatTimelineEntry(
                timelineIndex,
                source.Side,
                source.AllySlotIndex,
                source.HasAllyAction,
                source.AllyAction,
                source.EnemyId,
                source.EnemyIntent,
                resolved,
                reason,
                source.Initiative,
                source.TieBreakOrder);
        }

        public CombatTimelineEntry GetActionTimelineEntry(int timelineIndex)
        {
            return GetTimelineEntry(timelineIndex);
        }

        public int GetAllyHp(CombatTrainingUnitId unitId)
        {
            ValidateUnit(unitId);
            return allyHp[(int)unitId];
        }

        public int GetEnemyHp(CombatTrainingEnemyId enemyId)
        {
            ValidateEnemy(enemyId);
            return enemyHp[(int)enemyId];
        }

        public CombatCondition GetEnemyConditions(CombatTrainingEnemyId enemyId)
        {
            ValidateEnemy(enemyId);
            return enemyConditions[(int)enemyId];
        }

        public int GetEnemySuppression(CombatTrainingEnemyId enemyId)
        {
            ValidateEnemy(enemyId);
            return enemySuppression[(int)enemyId];
        }

        public bool IsUnitAlive(CombatTrainingUnitId unitId)
        {
            return IsValidUnit(unitId) && allyHp[(int)unitId] > 0;
        }

        public bool IsEnemyAlive(CombatTrainingEnemyId enemyId)
        {
            return IsValidEnemy(enemyId) && enemyHp[(int)enemyId] > 0;
        }

        public bool IsUnitPlanned(CombatTrainingUnitId unitId)
        {
            if (!IsValidUnit(unitId))
            {
                return false;
            }

            return FindActionSlotByActor(unitId) >= 0;
        }

        public CombatTrainingEnemyIntent GetEnemyIntent(CombatTrainingEnemyId enemyId)
        {
            ValidateEnemy(enemyId);
            return enemyIntents[(int)enemyId];
        }

        public bool TryGetFirstAliveEnemy(out CombatTrainingEnemyId enemyId)
        {
            for (int index = 0; index < EnemyCount; index++)
            {
                if (enemyHp[index] > 0)
                {
                    enemyId = (CombatTrainingEnemyId)index;
                    return true;
                }
            }

            enemyId = default;
            return false;
        }

        public bool TryGetFirstAliveUnit(out CombatTrainingUnitId unitId)
        {
            for (int index = 0; index < UnitCount; index++)
            {
                if (allyHp[index] > 0)
                {
                    unitId = (CombatTrainingUnitId)index;
                    return true;
                }
            }

            unitId = default;
            return false;
        }

        public static int GetTimelineIndexForAllySlot(int allySlotIndex)
        {
            if (!IsValidAllySlot(allySlotIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(allySlotIndex));
            }

            return allySlotIndex * 2;
        }

        public static int GetAllySlotForTimelineIndex(int timelineIndex)
        {
            if (!IsAllyTimelineIndex(timelineIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(timelineIndex));
            }

            return timelineIndex / 2;
        }

        public static int GetTimelineIndexForEnemy(CombatTrainingEnemyId enemyId)
        {
            switch (enemyId)
            {
                case CombatTrainingEnemyId.TraineeSwordsman:
                    return 1;
                case CombatTrainingEnemyId.ApprenticeMage:
                    return 3;
                case CombatTrainingEnemyId.TraineeShieldbearer:
                    return 5;
                default:
                    throw new ArgumentOutOfRangeException(nameof(enemyId));
            }
        }

        public static CombatTrainingEnemyId GetEnemyIdForTimelineIndex(int timelineIndex)
        {
            switch (timelineIndex)
            {
                case 1:
                    return CombatTrainingEnemyId.TraineeSwordsman;
                case 3:
                    return CombatTrainingEnemyId.ApprenticeMage;
                case 5:
                    return CombatTrainingEnemyId.TraineeShieldbearer;
                default:
                    throw new ArgumentOutOfRangeException(nameof(timelineIndex));
            }
        }

        public static int CalculateBonusChance(CombatTrainingSkillId skillId, CombatCondition conditions)
        {
            CombatTrainingSkillDefinition skill = GetSkillDefinition(skillId);
            int chance = skill.BaseBonusChance;
            switch (skillId)
            {
                case CombatTrainingSkillId.SlipperyFloor:
                    chance += HasCondition(conditions, CombatCondition.Marked) ? 20 : 0;
                    break;
                case CombatTrainingSkillId.ElasticCharge:
                    chance += HasCondition(conditions, CombatCondition.Sticky) ? 30 : 0;
                    chance += HasCondition(conditions, CombatCondition.Marked) ? 20 : 0;
                    break;
                case CombatTrainingSkillId.GateDelay:
                    chance += HasCondition(conditions, CombatCondition.Sticky) ? 25 : 0;
                    break;
                case CombatTrainingSkillId.BoneSpear:
                    chance += HasCondition(conditions, CombatCondition.Sticky) ? 30 : 0;
                    break;
                case CombatTrainingSkillId.CoverAmbush:
                    chance += HasCondition(conditions, CombatCondition.Sticky) ? 25 : 0;
                    chance += HasCondition(conditions, CombatCondition.Exposed) ? 30 : 0;
                    chance += HasCondition(conditions, CombatCondition.Marked) ? 20 : 0;
                    break;
                case CombatTrainingSkillId.StoneVolley:
                    chance += HasCondition(conditions, CombatCondition.Exposed) ? 20 : 0;
                    break;
            }

            return Math.Min(95, Math.Max(5, chance));
        }

        /// <summary>
        /// Optional state component of initiative. These are ordinary debuffs on a published
        /// enemy intent, not a stagger, clash, timing, or reaction system.
        /// </summary>
        public static int CalculateInitiativeModifier(CombatCondition conditions)
        {
            int modifier = 0;
            if (HasCondition(conditions, CombatCondition.Sticky))
            {
                modifier -= 6;
            }

            if (HasCondition(conditions, CombatCondition.Suppressed))
            {
                modifier -= 3;
            }

            return modifier;
        }

        public static CombatTrainingUnitDefinition GetUnitDefinition(CombatTrainingUnitId unitId)
        {
            switch (unitId)
            {
                case CombatTrainingUnitId.Slime001:
                    return new CombatTrainingUnitDefinition(unitId, "슬라임 001호", "점착 선봉", 72);
                case CombatTrainingUnitId.SkeletonGuard:
                    return new CombatTrainingUnitDefinition(unitId, "해골 경비조", "성문 경계", 92);
                case CombatTrainingUnitId.GoblinCover:
                    return new CombatTrainingUnitDefinition(unitId, "고블린 엄폐조", "엄폐 사수", 66);
                default:
                    throw new ArgumentOutOfRangeException(nameof(unitId));
            }
        }

        public static CombatTrainingEnemyDefinition GetEnemyDefinition(CombatTrainingEnemyId enemyId)
        {
            switch (enemyId)
            {
                case CombatTrainingEnemyId.TraineeSwordsman:
                    return new CombatTrainingEnemyDefinition(enemyId, "견습 검사", "SWORD", 115);
                case CombatTrainingEnemyId.TraineeShieldbearer:
                    return new CombatTrainingEnemyDefinition(enemyId, "견습 방패병", "SHIELD", 130);
                case CombatTrainingEnemyId.ApprenticeMage:
                    return new CombatTrainingEnemyDefinition(enemyId, "견습 마법사", "MAGE", 105);
                default:
                    throw new ArgumentOutOfRangeException(nameof(enemyId));
            }
        }

        public static CombatTrainingSkillDefinition GetSkillDefinition(CombatTrainingSkillId skillId)
        {
            if (!TryGetSkillDefinition(skillId, out CombatTrainingSkillDefinition skill))
            {
                throw new ArgumentOutOfRangeException(nameof(skillId));
            }

            return skill;
        }

        public static bool TryGetSkillDefinition(CombatTrainingSkillId skillId, out CombatTrainingSkillDefinition skill)
        {
            switch (skillId)
            {
                case CombatTrainingSkillId.SlipperyFloor:
                    skill = new CombatTrainingSkillDefinition(
                        skillId, CombatTrainingUnitId.Slime001, "미끄러운 바닥",
                        "확정 피해와 점착. 사용 뒤 공유 SP를 회복한다.",
                        0, 1, 14, 50, 12, CombatCondition.Sticky, 0, 0);
                    return true;
                case CombatTrainingSkillId.ElasticCharge:
                    skill = new CombatTrainingSkillDefinition(
                        skillId, CombatTrainingUnitId.Slime001, "탄성 돌진",
                        "점착·표식 대상에게 보너스 확률이 크게 오른다.",
                        2, 0, 36, 35, 30, CombatCondition.None, 0, 0);
                    return true;
                case CombatTrainingSkillId.GateDelay:
                    skill = new CombatTrainingSkillDefinition(
                        skillId, CombatTrainingUnitId.SkeletonGuard, "성문 지연전",
                        "확정 피해와 억제. 해당 적의 이번 의도 피해를 낮춘다.",
                        1, 0, 10, 50, 8, CombatCondition.Suppressed, 8, 6);
                    return true;
                case CombatTrainingSkillId.BoneSpear:
                    skill = new CombatTrainingSkillDefinition(
                        skillId, CombatTrainingUnitId.SkeletonGuard, "뼈창 찌르기",
                        "확정 피해와 노출. 점착 대상에게 보너스 확률이 오른다.",
                        2, 0, 32, 40, 24, CombatCondition.Exposed, 0, 0);
                    return true;
                case CombatTrainingSkillId.CoverAmbush:
                    skill = new CombatTrainingSkillDefinition(
                        skillId, CombatTrainingUnitId.GoblinCover, "엄폐 기습",
                        "점착·노출·표식이 겹칠수록 보너스 확률이 크게 오른다.",
                        3, 0, 50, 25, 42, CombatCondition.None, 0, 0);
                    return true;
                case CombatTrainingSkillId.StoneVolley:
                    skill = new CombatTrainingSkillDefinition(
                        skillId, CombatTrainingUnitId.GoblinCover, "돌팔매 견제",
                        "확정 피해와 표식. 사용 뒤 공유 SP를 회복한다.",
                        0, 1, 16, 55, 10, CombatCondition.Marked, 0, 0);
                    return true;
                default:
                    skill = default;
                    return false;
            }
        }

        private CombatTimelineResolution ResolveAllyTimelineSlot(int timelineIndex, int allySlot)
        {
            if (!allySlotOccupied[allySlot])
            {
                return CreateSkippedAllyResolution(timelineIndex, CombatTimelineSkipReason.EmptySlot);
            }

            CombatPlannedAction action = allyActions[allySlot];
            CombatTrainingSkillDefinition skill = GetSkillDefinition(action.SkillId);
            CombatTrainingUnitId actorId = skill.ActorId;
            if (!IsUnitAlive(actorId))
            {
                return CreateSkippedAllyResolution(timelineIndex, CombatTimelineSkipReason.ActorDefeated, actorId, action.TargetEnemyId);
            }

            if (!IsEnemyAlive(action.TargetEnemyId))
            {
                return CreateSkippedAllyResolution(timelineIndex, CombatTimelineSkipReason.TargetUnavailable, actorId, action.TargetEnemyId);
            }

            if (SharedSp < skill.SpCost)
            {
                return CreateSkippedAllyResolution(timelineIndex, CombatTimelineSkipReason.InsufficientSp, actorId, action.TargetEnemyId);
            }

            int targetIndex = (int)action.TargetEnemyId;
            int bonusChance = CalculateBonusChance(skill.Id, enemyConditions[targetIndex]);
            int spBefore = SharedSp;
            SharedSp = Math.Min(MaximumSp, SharedSp - skill.SpCost + skill.SpGain);

            // The guaranteed package deliberately commits before requesting the one random bonus roll.
            int baseDamage = ApplyEnemyDamage(action.TargetEnemyId, skill.BasePower);
            if (skill.AppliedCondition != CombatCondition.None)
            {
                enemyConditions[targetIndex] |= skill.AppliedCondition;
            }

            int suppressionApplied = skill.BaseSuppression;
            if (skill.BaseSuppression > 0)
            {
                enemySuppression[targetIndex] = Math.Min(30, enemySuppression[targetIndex] + skill.BaseSuppression);
                enemyConditions[targetIndex] |= CombatCondition.Suppressed;
            }

            int roll = randomSource.NextPercent();
            if (roll < 0 || roll > 99)
            {
                throw new InvalidOperationException("Combat random sources must return an integer from 0 through 99.");
            }

            bool bonusTriggered = roll < bonusChance;
            int bonusDamage = bonusTriggered ? ApplyEnemyDamage(action.TargetEnemyId, skill.BonusPower) : 0;
            if (bonusTriggered && skill.BonusSuppression > 0)
            {
                enemySuppression[targetIndex] = Math.Min(30, enemySuppression[targetIndex] + skill.BonusSuppression);
                enemyConditions[targetIndex] |= CombatCondition.Suppressed;
                suppressionApplied += skill.BonusSuppression;
            }

            return new CombatTimelineResolution(
                timelineIndex,
                CombatTimelineSide.Ally,
                CombatTimelineSkipReason.None,
                actorId,
                CombatTrainingEnemyId.TraineeSwordsman,
                action.TargetEnemyId,
                CombatTrainingUnitId.Slime001,
                spBefore,
                SharedSp,
                bonusChance,
                roll,
                baseDamage,
                bonusTriggered,
                bonusDamage,
                skill.AppliedCondition,
                suppressionApplied,
                0,
                0,
                0,
                !IsEnemyAlive(action.TargetEnemyId));
        }

        private CombatTimelineResolution ResolveEnemyTimelineSlot(int timelineIndex, CombatTrainingEnemyId enemyId)
        {
            CombatTrainingEnemyIntent intent = enemyIntents[(int)enemyId];
            if (!IsEnemyAlive(enemyId))
            {
                return CreateSkippedEnemyResolution(timelineIndex, enemyId, intent, CombatTimelineSkipReason.EnemyDefeated);
            }

            if (intent.TargetKind == CombatIntentTargetKind.Single && !IsUnitAlive(intent.TargetUnitId))
            {
                return CreateSkippedEnemyResolution(timelineIndex, enemyId, intent, CombatTimelineSkipReason.TargetUnavailable);
            }

            if (intent.TargetKind == CombatIntentTargetKind.All && AliveAllyCount <= 0)
            {
                return CreateSkippedEnemyResolution(timelineIndex, enemyId, intent, CombatTimelineSkipReason.TargetUnavailable);
            }

            int suppression = enemySuppression[(int)enemyId];
            int damage = Math.Max(0, intent.BaseDamage - suppression);
            int slimeDamage = 0;
            int skeletonDamage = 0;
            int goblinDamage = 0;
            if (intent.TargetKind == CombatIntentTargetKind.All)
            {
                slimeDamage = ApplyAllyDamage(CombatTrainingUnitId.Slime001, damage);
                skeletonDamage = ApplyAllyDamage(CombatTrainingUnitId.SkeletonGuard, damage);
                goblinDamage = ApplyAllyDamage(CombatTrainingUnitId.GoblinCover, damage);
            }
            else
            {
                int applied = ApplyAllyDamage(intent.TargetUnitId, damage);
                switch (intent.TargetUnitId)
                {
                    case CombatTrainingUnitId.Slime001:
                        slimeDamage = applied;
                        break;
                    case CombatTrainingUnitId.SkeletonGuard:
                        skeletonDamage = applied;
                        break;
                    case CombatTrainingUnitId.GoblinCover:
                        goblinDamage = applied;
                        break;
                }
            }

            return new CombatTimelineResolution(
                timelineIndex,
                CombatTimelineSide.Enemy,
                CombatTimelineSkipReason.None,
                CombatTrainingUnitId.Slime001,
                enemyId,
                CombatTrainingEnemyId.TraineeSwordsman,
                intent.TargetUnitId,
                SharedSp,
                SharedSp,
                0,
                -1,
                0,
                false,
                0,
                CombatCondition.None,
                suppression,
                slimeDamage,
                skeletonDamage,
                goblinDamage,
                false);
        }

        private CombatTimelineResolution CreateSkippedAllyResolution(
            int timelineIndex,
            CombatTimelineSkipReason reason,
            CombatTrainingUnitId actorId = CombatTrainingUnitId.Slime001,
            CombatTrainingEnemyId targetId = CombatTrainingEnemyId.TraineeSwordsman)
        {
            return new CombatTimelineResolution(
                timelineIndex,
                CombatTimelineSide.Ally,
                reason,
                actorId,
                CombatTrainingEnemyId.TraineeSwordsman,
                targetId,
                CombatTrainingUnitId.Slime001,
                SharedSp,
                SharedSp,
                0,
                -1,
                0,
                false,
                0,
                CombatCondition.None,
                0,
                0,
                0,
                0,
                false);
        }

        private CombatTimelineResolution CreateSkippedEnemyResolution(
            int timelineIndex,
            CombatTrainingEnemyId enemyId,
            CombatTrainingEnemyIntent intent,
            CombatTimelineSkipReason reason)
        {
            return new CombatTimelineResolution(
                timelineIndex,
                CombatTimelineSide.Enemy,
                reason,
                CombatTrainingUnitId.Slime001,
                enemyId,
                CombatTrainingEnemyId.TraineeSwordsman,
                intent.TargetUnitId,
                SharedSp,
                SharedSp,
                0,
                -1,
                0,
                false,
                0,
                CombatCondition.None,
                0,
                0,
                0,
                0,
                false);
        }

        private void CompleteTimelineStep()
        {
            if (AliveEnemyCount <= 0)
            {
                Phase = CombatTrainingPhase.Victory;
                return;
            }

            if (AliveAllyCount <= 0)
            {
                Phase = CombatTrainingPhase.Defeat;
                return;
            }

            resolutionIndex++;
            if (resolutionIndex < TimelineCount)
            {
                return;
            }

            Round++;
            SharedSp = Math.Min(MaximumSp, SharedSp + RoundSpRecovery);
            resolutionIndex = 0;
            ClearPlan();
            ClearRoundConditions();
            ClearTimelineResolution();
            BuildEnemyIntents();
            RebuildActionLine();
            Phase = CombatTrainingPhase.Planning;
        }

        private List<PlanningSimulationState> SimulatePlanningStatesBefore(int exclusiveTimelineIndex)
        {
            List<PlanningSimulationState> states = new List<PlanningSimulationState> { CaptureSimulationState() };
            for (int timelineIndex = 0; timelineIndex < exclusiveTimelineIndex; timelineIndex++)
            {
                List<PlanningSimulationState> next = new List<PlanningSimulationState>();
                for (int stateIndex = 0; stateIndex < states.Count; stateIndex++)
                {
                    PlanningSimulationState state = states[stateIndex];
                    CombatTimelineEntry entry = actionLine[timelineIndex];
                    if (entry.Side == CombatTimelineSide.Ally)
                    {
                        if (entry.HasAllyAction)
                        {
                            SimulateAllySlot(state, entry.AllySlotIndex, next);
                        }
                        else
                        {
                            next.Add(state.Clone());
                        }
                    }
                    else
                    {
                        SimulateEnemySlot(state, entry.EnemyId, next);
                    }
                }

                states = next;
            }

            return states;
        }

        private void SimulateAllySlot(PlanningSimulationState state, int allySlot, List<PlanningSimulationState> output)
        {
            if (!allySlotOccupied[allySlot])
            {
                output.Add(state.Clone());
                return;
            }

            CombatPlannedAction action = allyActions[allySlot];
            CombatTrainingSkillDefinition skill = GetSkillDefinition(action.SkillId);
            if (state.AllyHp[(int)skill.ActorId] <= 0
                || state.EnemyHp[(int)action.TargetEnemyId] <= 0
                || state.SharedSp < skill.SpCost)
            {
                output.Add(state.Clone());
                return;
            }

            int targetIndex = (int)action.TargetEnemyId;
            int chance = CalculateBonusChance(skill.Id, state.Conditions[targetIndex]);
            SimulateAllyBranch(state, skill, action.TargetEnemyId, false, output);
            if (chance > 0)
            {
                SimulateAllyBranch(state, skill, action.TargetEnemyId, true, output);
            }
        }

        private static void SimulateAllyBranch(
            PlanningSimulationState source,
            CombatTrainingSkillDefinition skill,
            CombatTrainingEnemyId targetEnemyId,
            bool bonusTriggered,
            List<PlanningSimulationState> output)
        {
            PlanningSimulationState state = source.Clone();
            int targetIndex = (int)targetEnemyId;
            state.SharedSp = Math.Min(MaximumSp, state.SharedSp - skill.SpCost + skill.SpGain);
            ApplyDamage(state.EnemyHp, targetIndex, skill.BasePower);
            if (skill.AppliedCondition != CombatCondition.None)
            {
                state.Conditions[targetIndex] |= skill.AppliedCondition;
            }

            if (skill.BaseSuppression > 0)
            {
                state.Suppression[targetIndex] = Math.Min(30, state.Suppression[targetIndex] + skill.BaseSuppression);
                state.Conditions[targetIndex] |= CombatCondition.Suppressed;
            }

            if (bonusTriggered)
            {
                ApplyDamage(state.EnemyHp, targetIndex, skill.BonusPower);
                if (skill.BonusSuppression > 0)
                {
                    state.Suppression[targetIndex] = Math.Min(30, state.Suppression[targetIndex] + skill.BonusSuppression);
                    state.Conditions[targetIndex] |= CombatCondition.Suppressed;
                }
            }

            output.Add(state);
        }

        private void SimulateEnemySlot(PlanningSimulationState state, CombatTrainingEnemyId enemyId, List<PlanningSimulationState> output)
        {
            PlanningSimulationState result = state.Clone();
            CombatTrainingEnemyIntent intent = enemyIntents[(int)enemyId];
            if (result.EnemyHp[(int)enemyId] <= 0)
            {
                output.Add(result);
                return;
            }

            int damage = Math.Max(0, intent.BaseDamage - result.Suppression[(int)enemyId]);
            if (intent.TargetKind == CombatIntentTargetKind.Single)
            {
                if (result.AllyHp[(int)intent.TargetUnitId] > 0)
                {
                    ApplyDamage(result.AllyHp, (int)intent.TargetUnitId, damage);
                }
            }
            else
            {
                for (int ally = 0; ally < UnitCount; ally++)
                {
                    ApplyDamage(result.AllyHp, ally, damage);
                }
            }

            output.Add(result);
        }

        private PlanningSimulationState CaptureSimulationState()
        {
            return new PlanningSimulationState(
                SharedSp,
                (int[])allyHp.Clone(),
                (int[])enemyHp.Clone(),
                (CombatCondition[])enemyConditions.Clone(),
                (int[])enemySuppression.Clone());
        }

        /// <summary>
        /// Rebuilds the six visible entries after every planning mutation. The storage index of an
        /// ally action is deliberately not its execution index: selected skills and enemy intents
        /// are sorted together by base speed + priority + status modifier.
        /// </summary>
        private void RebuildActionLine()
        {
            var entries = new List<CombatTimelineEntry>(TimelineCount);
            for (int allySlot = 0; allySlot < AllySlotCount; allySlot++)
            {
                if (!allySlotOccupied[allySlot])
                {
                    continue;
                }

                CombatPlannedAction action = allyActions[allySlot];
                CombatTrainingSkillDefinition skill = GetSkillDefinition(action.SkillId);
                int initiative = GetUnitDefinition(skill.ActorId).BaseSpeed + skill.InitiativePriority;
                entries.Add(new CombatTimelineEntry(
                    -1,
                    CombatTimelineSide.Ally,
                    allySlot,
                    true,
                    action,
                    CombatTrainingEnemyId.TraineeSwordsman,
                    default,
                    false,
                    CombatTimelineSkipReason.None,
                    initiative,
                    (int)skill.ActorId));
            }

            for (int enemy = 0; enemy < EnemyCount; enemy++)
            {
                CombatTrainingEnemyId enemyId = (CombatTrainingEnemyId)enemy;
                CombatTrainingEnemyIntent intent = enemyIntents[enemy];
                int initiative = GetEnemyDefinition(enemyId).BaseSpeed
                    + intent.InitiativePriority
                    + CalculateInitiativeModifier(enemyConditions[enemy]);
                entries.Add(new CombatTimelineEntry(
                    -1,
                    CombatTimelineSide.Enemy,
                    -1,
                    false,
                    default,
                    enemyId,
                    intent,
                    false,
                    CombatTimelineSkipReason.None,
                    initiative,
                    100 + enemy));
            }

            // Keep a stable six-row UI even while the player has not selected every ally yet.
            // These empty entries never resolve because TryBeginExecution requires a complete plan.
            for (int empty = entries.Count; empty < TimelineCount; empty++)
            {
                entries.Add(new CombatTimelineEntry(
                    -1,
                    CombatTimelineSide.Ally,
                    -1,
                    false,
                    default,
                    CombatTrainingEnemyId.TraineeSwordsman,
                    default,
                    false,
                    CombatTimelineSkipReason.EmptySlot,
                    int.MinValue,
                    200 + empty));
            }

            entries.Sort(CompareActionLineEntries);
            for (int timelineIndex = 0; timelineIndex < TimelineCount; timelineIndex++)
            {
                CombatTimelineEntry entry = entries[timelineIndex];
                actionLine[timelineIndex] = new CombatTimelineEntry(
                    timelineIndex,
                    entry.Side,
                    entry.AllySlotIndex,
                    entry.HasAllyAction,
                    entry.AllyAction,
                    entry.EnemyId,
                    entry.EnemyIntent,
                    false,
                    entry.SkipReason,
                    entry.Initiative,
                    entry.TieBreakOrder);
            }
        }

        private static int CompareActionLineEntries(CombatTimelineEntry left, CombatTimelineEntry right)
        {
            int initiative = right.Initiative.CompareTo(left.Initiative);
            return initiative != 0
                ? initiative
                : left.TieBreakOrder.CompareTo(right.TieBreakOrder);
        }

        private int FindTimelineIndexForAllySlot(int allySlotIndex)
        {
            for (int timelineIndex = 0; timelineIndex < TimelineCount; timelineIndex++)
            {
                CombatTimelineEntry entry = actionLine[timelineIndex];
                if (entry.Side == CombatTimelineSide.Ally
                    && entry.HasAllyAction
                    && entry.AllySlotIndex == allySlotIndex)
                {
                    return timelineIndex;
                }
            }

            throw new InvalidOperationException("The ally action was missing from the dynamic action line.");
        }

        private int FindTimelineIndexForEnemy(CombatTrainingEnemyId enemyId)
        {
            for (int timelineIndex = 0; timelineIndex < TimelineCount; timelineIndex++)
            {
                CombatTimelineEntry entry = actionLine[timelineIndex];
                if (entry.Side == CombatTimelineSide.Enemy && entry.EnemyId == enemyId)
                {
                    return timelineIndex;
                }
            }

            throw new InvalidOperationException("The enemy intent was missing from the dynamic action line.");
        }

        private void GetProjectedSpForTimelineEntry(
            int targetTimelineIndex,
            out int spBefore,
            out int spAfter,
            out bool canPay)
        {
            int projectedSp = SharedSp;
            spBefore = SharedSp;
            spAfter = SharedSp;
            canPay = false;
            for (int timelineIndex = 0; timelineIndex <= targetTimelineIndex; timelineIndex++)
            {
                CombatTimelineEntry entry = actionLine[timelineIndex];
                if (!entry.HasAllyAction)
                {
                    continue;
                }

                CombatTrainingSkillDefinition skill = GetSkillDefinition(entry.AllyAction.SkillId);
                int before = projectedSp;
                bool payable = before >= skill.SpCost;
                projectedSp = Math.Min(MaximumSp, projectedSp - skill.SpCost + skill.SpGain);
                if (timelineIndex == targetTimelineIndex)
                {
                    spBefore = before;
                    spAfter = projectedSp;
                    canPay = payable;
                    return;
                }
            }
        }

        private void BuildEnemyIntents()
        {
            int roundBonus = Math.Max(0, Round - 1);
            CombatTrainingUnitId swordsmanTarget = SelectAliveIntentTarget(Round - 1, null);
            CombatTrainingUnitId shieldTarget = SelectAliveIntentTarget(
                Round,
                AliveAllyCount > 1 ? swordsmanTarget : (CombatTrainingUnitId?)null);

            enemyIntents[(int)CombatTrainingEnemyId.TraineeSwordsman] = new CombatTrainingEnemyIntent(
                CombatTrainingEnemyId.TraineeSwordsman,
                "중앙 돌진",
                "한 명을 노리는 빠른 베기",
                CombatIntentTargetKind.Single,
                swordsmanTarget,
                18 + roundBonus * 2,
                3);
            enemyIntents[(int)CombatTrainingEnemyId.TraineeShieldbearer] = new CombatTrainingEnemyIntent(
                CombatTrainingEnemyId.TraineeShieldbearer,
                "방패 압박",
                "한 명을 밀어붙이는 방패타",
                CombatIntentTargetKind.Single,
                shieldTarget,
                16 + roundBonus * 2,
                -1);
            enemyIntents[(int)CombatTrainingEnemyId.ApprenticeMage] = new CombatTrainingEnemyIntent(
                CombatTrainingEnemyId.ApprenticeMage,
                "화염 영창",
                "아군 전체를 공격하는 주문",
                CombatIntentTargetKind.All,
                swordsmanTarget,
                9 + roundBonus,
                -1);
        }

        private CombatTrainingUnitId SelectAliveIntentTarget(int startIndex, CombatTrainingUnitId? excluded)
        {
            int normalizedStart = ((startIndex % UnitCount) + UnitCount) % UnitCount;
            for (int offset = 0; offset < UnitCount; offset++)
            {
                CombatTrainingUnitId candidate = (CombatTrainingUnitId)((normalizedStart + offset) % UnitCount);
                if (allyHp[(int)candidate] > 0 && (!excluded.HasValue || candidate != excluded.Value))
                {
                    return candidate;
                }
            }

            if (excluded.HasValue)
            {
                return SelectAliveIntentTarget(normalizedStart, null);
            }

            return CombatTrainingUnitId.Slime001;
        }

        private CombatTimelineSkipReason GetCurrentAllySlotSkipReason(int allySlot)
        {
            if (!allySlotOccupied[allySlot])
            {
                return CombatTimelineSkipReason.EmptySlot;
            }

            CombatPlannedAction action = allyActions[allySlot];
            CombatTrainingSkillDefinition skill = GetSkillDefinition(action.SkillId);
            if (!IsUnitAlive(skill.ActorId))
            {
                return CombatTimelineSkipReason.ActorDefeated;
            }

            if (!IsEnemyAlive(action.TargetEnemyId))
            {
                return CombatTimelineSkipReason.TargetUnavailable;
            }

            return Phase == CombatTrainingPhase.Resolving && SharedSp < skill.SpCost
                ? CombatTimelineSkipReason.InsufficientSp
                : CombatTimelineSkipReason.None;
        }

        private CombatTimelineSkipReason GetCurrentEnemySkipReason(CombatTrainingEnemyId enemyId)
        {
            if (!IsEnemyAlive(enemyId))
            {
                return CombatTimelineSkipReason.EnemyDefeated;
            }

            CombatTrainingEnemyIntent intent = enemyIntents[(int)enemyId];
            if (intent.TargetKind == CombatIntentTargetKind.Single && !IsUnitAlive(intent.TargetUnitId))
            {
                return CombatTimelineSkipReason.TargetUnavailable;
            }

            return intent.TargetKind == CombatIntentTargetKind.All && AliveAllyCount <= 0
                ? CombatTimelineSkipReason.TargetUnavailable
                : CombatTimelineSkipReason.None;
        }

        private int FindActionSlotByActor(CombatTrainingUnitId actorId)
        {
            for (int slot = 0; slot < AllySlotCount; slot++)
            {
                if (allySlotOccupied[slot]
                    && GetSkillDefinition(allyActions[slot].SkillId).ActorId == actorId)
                {
                    return slot;
                }
            }

            return -1;
        }

        private int FindFirstEmptyAllySlot()
        {
            for (int slot = 0; slot < AllySlotCount; slot++)
            {
                if (!allySlotOccupied[slot])
                {
                    return slot;
                }
            }

            return -1;
        }

        private int ApplyEnemyDamage(CombatTrainingEnemyId enemyId, int damage)
        {
            return ApplyDamage(enemyHp, (int)enemyId, damage);
        }

        private int ApplyAllyDamage(CombatTrainingUnitId unitId, int damage)
        {
            return ApplyDamage(allyHp, (int)unitId, damage);
        }

        private static int ApplyDamage(int[] hp, int index, int damage)
        {
            if (hp[index] <= 0)
            {
                return 0;
            }

            int applied = Math.Min(hp[index], Math.Max(0, damage));
            hp[index] -= applied;
            return applied;
        }

        private void ClearPlan()
        {
            for (int index = 0; index < AllySlotCount; index++)
            {
                allyActions[index] = default;
                allySlotOccupied[index] = false;
            }
        }

        private void ClearRoundConditions()
        {
            for (int index = 0; index < EnemyCount; index++)
            {
                enemyConditions[index] = CombatCondition.None;
                enemySuppression[index] = 0;
            }
        }

        private void ClearTimelineResolution()
        {
            for (int index = 0; index < TimelineCount; index++)
            {
                timelineResolved[index] = false;
                timelineSkipReasons[index] = CombatTimelineSkipReason.None;
            }
        }

        private static int CountAlive(int[] hp)
        {
            int count = 0;
            for (int index = 0; index < hp.Length; index++)
            {
                if (hp[index] > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsAllyTimelineIndex(int timelineIndex)
        {
            return timelineIndex >= 0 && timelineIndex < TimelineCount && timelineIndex % 2 == 0;
        }

        private static bool IsValidAllySlot(int allySlotIndex)
        {
            return allySlotIndex >= 0 && allySlotIndex < AllySlotCount;
        }

        private static bool HasCondition(CombatCondition value, CombatCondition condition)
        {
            return (value & condition) == condition;
        }

        private static bool IsValidUnit(CombatTrainingUnitId unitId)
        {
            return unitId >= CombatTrainingUnitId.Slime001 && unitId <= CombatTrainingUnitId.GoblinCover;
        }

        private static bool IsValidEnemy(CombatTrainingEnemyId enemyId)
        {
            return enemyId >= CombatTrainingEnemyId.TraineeSwordsman
                && enemyId <= CombatTrainingEnemyId.ApprenticeMage;
        }

        private static void ValidateUnit(CombatTrainingUnitId unitId)
        {
            if (!IsValidUnit(unitId))
            {
                throw new ArgumentOutOfRangeException(nameof(unitId));
            }
        }

        private static void ValidateEnemy(CombatTrainingEnemyId enemyId)
        {
            if (!IsValidEnemy(enemyId))
            {
                throw new ArgumentOutOfRangeException(nameof(enemyId));
            }
        }
    }
}

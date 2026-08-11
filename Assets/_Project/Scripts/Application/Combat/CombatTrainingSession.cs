using System;
using DemonLord.Domain.Combat;

namespace DemonLord.Application.Combat
{
    public sealed class SystemCombatRandomSource : ICombatRandomSource
    {
        private readonly Random random;

        public SystemCombatRandomSource()
            : this(Environment.TickCount)
        {
        }

        public SystemCombatRandomSource(int seed)
        {
            random = new Random(seed);
        }

        public int NextPercent()
        {
            return random.Next(0, 100);
        }
    }

    /// <summary>
    /// Application boundary for one repeatable liaison training battle.
    /// The domain owns the dynamic initiative action line; this type only owns the injected random
    /// source and restart lifecycle.
    /// </summary>
    public sealed class CombatTrainingSession
    {
        private readonly ICombatRandomSource randomSource;

        public CombatTrainingSession()
            : this(new SystemCombatRandomSource())
        {
        }

        public CombatTrainingSession(ICombatRandomSource randomSource)
        {
            this.randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
            Battle = new CombatTrainingBattle(this.randomSource);
        }

        public CombatTrainingBattle Battle { get; private set; }

        public void Restart()
        {
            Battle = new CombatTrainingBattle(randomSource);
        }

        public bool TrySetAction(
            CombatTrainingSkillId skillId,
            CombatTrainingEnemyId targetEnemyId,
            out string errorCode)
        {
            return Battle.TrySetAction(skillId, targetEnemyId, out errorCode);
        }

        public bool TryRemoveAction(int allySlotIndex, out string errorCode)
        {
            return Battle.TryRemoveAction(allySlotIndex, out errorCode);
        }

        public bool TryMoveAction(int fromAllySlotIndex, int toAllySlotIndex, out string errorCode)
        {
            // Retained for callers during the UI migration. The domain returns
            // combat_manual_order_removed because initiative, not manual swapping, owns order.
            return Battle.TryMoveAction(fromAllySlotIndex, toAllySlotIndex, out errorCode);
        }

        public bool TryBeginExecution(out string errorCode)
        {
            return Battle.TryBeginExecution(out errorCode);
        }

        public bool TryResolveNextTimelineAction(
            out CombatTimelineResolution resolution,
            out string errorCode)
        {
            return Battle.TryResolveNextTimelineAction(out resolution, out errorCode);
        }
    }
}

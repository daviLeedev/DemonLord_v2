using System;
using System.Threading.Tasks;
using DemonLord.Domain;

namespace DemonLord.Application
{
    public sealed class BattleLaunchRequest
    {
        public BattleLaunchRequest(
            string battleId,
            string enemyGroupId,
            ExplorationLocation returnLocation)
        {
            if (!StableWorldId.IsValid(battleId)) throw new ArgumentException("Invalid battle ID.", nameof(battleId));
            if (!StableWorldId.IsValid(enemyGroupId)) throw new ArgumentException("Invalid enemy group ID.", nameof(enemyGroupId));
            BattleId = battleId;
            EnemyGroupId = enemyGroupId;
            ReturnLocation = returnLocation ?? throw new ArgumentNullException(nameof(returnLocation));
        }

        public string BattleId { get; }
        public string EnemyGroupId { get; }
        public ExplorationLocation ReturnLocation { get; }
    }

    public readonly struct BattleLaunchResult
    {
        private BattleLaunchResult(bool isSuccess, string errorCode)
        {
            IsSuccess = isSuccess;
            ErrorCode = errorCode ?? string.Empty;
        }

        public bool IsSuccess { get; }
        public string ErrorCode { get; }

        public static BattleLaunchResult Success() => new BattleLaunchResult(true, string.Empty);
        public static BattleLaunchResult Failure(string errorCode) => new BattleLaunchResult(false, errorCode);
    }

    public interface IBattleFlowService
    {
        Task<BattleLaunchResult> LaunchAsync(BattleLaunchRequest request);
    }
}

using System;
using System.Collections;
using System.Threading.Tasks;
using DemonLord.Application;
using DemonLord.Application.Combat;
using DemonLord.Domain.Combat;
using DemonLord.Presentation.Exploration;
using UnityEngine;

namespace DemonLord.Presentation.Combat
{
    [DisallowMultipleComponent]
    public sealed class CombatTrainingCoordinator : MonoBehaviour, IBattleFlowService
    {
        public const string LiaisonStableId = "combat-liaison-officer";

        private const float ActionWindupSeconds = 0.22f;
        private const float ActionResultSeconds = 0.68f;

        [SerializeField] private ExplorationInputReader inputReader;
        [SerializeField] private InGameUiCoordinator inGameUiCoordinator;
        [SerializeField] private CombatTrainingView view;

        private CombatTrainingView subscribedView;
        private CombatTrainingSession session;
        private IDisposable explorationGateToken;
        private IDisposable uiBlockToken;
        private Coroutine playbackRoutine;
        private Func<CombatTrainingSession> sessionFactory;
        private CombatTrainingUnitId selectedUnit;
        private CombatTrainingEnemyId selectedTarget;
        private bool trainingActive;
        private bool resolving;
        private float playbackDelayScale = 1f;

        public bool IsTrainingActive => trainingActive;
        public bool IsResolving => resolving;
        public CombatTrainingSession Session => session;
        public CombatTrainingView View => view;
        public CombatTrainingUnitId SelectedUnit => selectedUnit;
        public CombatTrainingEnemyId SelectedTarget => selectedTarget;

        private void OnEnable()
        {
            SubscribeDependencies();
        }

        private void OnDisable()
        {
            UnsubscribeDependencies();
            EndTraining();
        }

        private void OnDestroy()
        {
            UnsubscribeDependencies();
            EndTraining();
        }

        public void Configure(
            ExplorationInputReader configuredInputReader,
            InGameUiCoordinator configuredInGameUiCoordinator,
            CombatTrainingView configuredView)
        {
            UnsubscribeDependencies();
            inputReader = configuredInputReader;
            inGameUiCoordinator = configuredInGameUiCoordinator;
            view = configuredView;
            if (isActiveAndEnabled)
            {
                SubscribeDependencies();
            }
        }

        public void SetSessionFactoryForTests(Func<CombatTrainingSession> configuredFactory)
        {
            if (trainingActive)
            {
                throw new InvalidOperationException("The combat session factory cannot change during training.");
            }

            sessionFactory = configuredFactory;
        }

        public void SetPlaybackDelayScaleForTests(float scale)
        {
            playbackDelayScale = Mathf.Max(0f, scale);
        }

        public bool BeginTraining()
        {
            if (trainingActive
                || inputReader == null
                || inGameUiCoordinator == null
                || view == null
                || !view.isActiveAndEnabled
                || !inGameUiCoordinator.IsInitialized
                || inGameUiCoordinator.State != DemonLord.Application.InGameMenuState.Closed
                || inGameUiCoordinator.IsMapOpen)
            {
                return false;
            }

            try
            {
                uiBlockToken = inGameUiCoordinator.AcquireExternalInputBlock();
                explorationGateToken = inputReader.Gate.AcquireLock(ExplorationInputChannel.All);
                inputReader.ClearPendingDialogueInput();
                inputReader.ClearPendingMenuInput();
                session = sessionFactory != null ? sessionFactory() : new CombatTrainingSession();
                if (session == null)
                {
                    throw new InvalidOperationException("The combat session factory returned null.");
                }

                trainingActive = true;
                resolving = false;
                SelectFirstAliveUnit();
                SelectFirstAliveTarget();
                view.Show();
                view.ClearPlaybackFocus();
                Render("왼쪽 부대를 고르고, 오른쪽 기술을 선택해 행동선에 넣으세요. 적의 붉은 화살은 실제 다음 행동입니다.");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                EndTraining();
                return false;
            }
        }

        public Task<BattleLaunchResult> LaunchAsync(BattleLaunchRequest request)
        {
            if (request == null)
            {
                return Task.FromResult(BattleLaunchResult.Failure("battle_request_missing"));
            }

            return Task.FromResult(BeginTraining()
                ? BattleLaunchResult.Success()
                : BattleLaunchResult.Failure("combat_training_unavailable"));
        }

        public void ExitTraining()
        {
            EndTraining();
        }

        private void OnViewBecameUnavailable()
        {
            if (trainingActive)
            {
                EndTraining();
            }
        }

        private void OnAllyRosterRequested(int unitIndex)
        {
            if (!CanAcceptPlanningInput() || unitIndex < 0 || unitIndex >= CombatTrainingBattle.UnitCount)
            {
                return;
            }

            CombatTrainingUnitId candidate = (CombatTrainingUnitId)unitIndex;
            if (!session.Battle.IsUnitAlive(candidate))
            {
                return;
            }

            selectedUnit = candidate;
            Render();
        }

        private void OnEnemyTargetRequested(int enemyIndex)
        {
            if (!CanAcceptPlanningInput() || enemyIndex < 0 || enemyIndex >= CombatTrainingBattle.EnemyCount)
            {
                return;
            }

            CombatTrainingEnemyId candidate = (CombatTrainingEnemyId)enemyIndex;
            if (!session.Battle.IsEnemyAlive(candidate))
            {
                return;
            }

            selectedTarget = candidate;
            Render();
        }

        private void OnSkillRequested(CombatTrainingSkillId skillId)
        {
            if (!CanAcceptPlanningInput())
            {
                return;
            }

            if (!CombatTrainingBattle.TryGetSkillDefinition(skillId, out CombatTrainingSkillDefinition skill)
                || skill.ActorId != selectedUnit)
            {
                return;
            }

            EnsureSelectedTargetIsAlive();
            if (session.TrySetAction(skillId, selectedTarget, out string errorCode))
            {
                Render();
            }
            else
            {
                Render(PlanningError(errorCode));
            }
        }

        private void OnExecuteRequested()
        {
            if (!CanAcceptPlanningInput())
            {
                return;
            }

            if (!session.TryBeginExecution(out string errorCode))
            {
                Render(PlanningError(errorCode));
                return;
            }

            Render("행동선을 잠갔습니다. 이제 아군과 적이 같은 순서대로 맞물려 행동합니다.");
            playbackRoutine = StartCoroutine(ResolveRound());
        }

        private void OnRetryRequested()
        {
            if (!trainingActive || session == null || resolving)
            {
                return;
            }

            session.Restart();
            SelectFirstAliveUnit();
            SelectFirstAliveTarget();
            view.ClearPlaybackFocus();
            Render("모의전을 초기화했습니다. 새 행동선을 설계하세요.");
        }

        private IEnumerator ResolveRound()
        {
            resolving = true;
            CombatTrainingBattle battle = session.Battle;
            bool completedNormally = false;
            try
            {
                while (trainingActive && battle.Phase == CombatTrainingPhase.Resolving)
                {
                    CombatTimelineEntry entry = battle.GetTimelineEntry(battle.ResolutionIndex);
                    string headline = ActionLinePrefix(entry) + PlaybackHeadline(entry);
                    view.ShowPlaybackFocus(entry, headline);
                    yield return WaitUnscaled(ActionWindupSeconds);

                    if (!trainingActive || session == null)
                    {
                        yield break;
                    }

                    if (!session.TryResolveNextTimelineAction(out CombatTimelineResolution resolution, out string errorCode))
                    {
                        throw new InvalidOperationException("Combat timeline resolution rejected: " + errorCode);
                    }

                    EnsureSelectedUnitIsAlive();
                    EnsureSelectedTargetIsAlive();
                    string feedback = ActionLinePrefix(entry) + FormatTimelineResolution(entry, resolution);
                    Render(feedback);
                    view.ShowPlaybackFocus(entry, feedback);
                    yield return WaitUnscaled(ActionResultSeconds);
                }

                if (trainingActive && session != null)
                {
                    if (battle.Phase == CombatTrainingPhase.Planning)
                    {
                        EnsureSelectedUnitIsAlive();
                        EnsureSelectedTargetIsAlive();
                        Render("ROUND " + battle.Round + " · 공유 SP " + CombatTrainingBattle.RoundSpRecovery + " 회복. 다음 행동선을 설계하세요.");
                    }
                    else
                    {
                        Render();
                    }
                }

                completedNormally = true;
            }
            finally
            {
                resolving = false;
                playbackRoutine = null;
                if (!completedNormally && trainingActive)
                {
                    Debug.LogWarning("Combat playback stopped unexpectedly. Training locks were released.", this);
                    EndTraining();
                }
                else if (view != null && view.isActiveAndEnabled)
                {
                    try
                    {
                        view.ClearPlaybackFocus();
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception, this);
                        EndTraining();
                    }
                }
            }
        }

        private IEnumerator WaitUnscaled(float seconds)
        {
            float remaining = seconds * playbackDelayScale;
            while (trainingActive && remaining > 0f)
            {
                remaining -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private bool CanAcceptPlanningInput()
        {
            return trainingActive
                && !resolving
                && session != null
                && session.Battle.Phase == CombatTrainingPhase.Planning;
        }

        private void EnsureSelectedUnitIsAlive()
        {
            if (session == null || session.Battle.IsUnitAlive(selectedUnit))
            {
                return;
            }

            SelectFirstAliveUnit();
        }

        private void EnsureSelectedTargetIsAlive()
        {
            if (session == null || session.Battle.IsEnemyAlive(selectedTarget))
            {
                return;
            }

            SelectFirstAliveTarget();
        }

        private void SelectFirstAliveUnit()
        {
            if (session != null && session.Battle.TryGetFirstAliveUnit(out CombatTrainingUnitId unitId))
            {
                selectedUnit = unitId;
            }
            else
            {
                selectedUnit = CombatTrainingUnitId.Slime001;
            }
        }

        private void SelectFirstAliveTarget()
        {
            if (session != null && session.Battle.TryGetFirstAliveEnemy(out CombatTrainingEnemyId target))
            {
                selectedTarget = target;
            }
            else
            {
                selectedTarget = CombatTrainingEnemyId.TraineeSwordsman;
            }
        }

        private void Render(string feedback = null)
        {
            if (session != null && view != null)
            {
                view.Render(session.Battle, selectedUnit, selectedTarget, feedback);
            }
        }

        private void SubscribeDependencies()
        {
            if (view != null && subscribedView != view)
            {
                if (subscribedView != null)
                {
                    UnsubscribeView(subscribedView);
                }

                view.AllyRosterRequested += OnAllyRosterRequested;
                view.EnemyTargetRequested += OnEnemyTargetRequested;
                view.SkillRequested += OnSkillRequested;
                view.ExecuteRequested += OnExecuteRequested;
                view.RetryRequested += OnRetryRequested;
                view.ExitRequested += ExitTraining;
                view.BecameUnavailable += OnViewBecameUnavailable;
                subscribedView = view;
            }
        }

        private void UnsubscribeDependencies()
        {
            if (subscribedView != null)
            {
                UnsubscribeView(subscribedView);
                subscribedView = null;
            }
        }

        private void UnsubscribeView(CombatTrainingView target)
        {
            target.AllyRosterRequested -= OnAllyRosterRequested;
            target.EnemyTargetRequested -= OnEnemyTargetRequested;
            target.SkillRequested -= OnSkillRequested;
            target.ExecuteRequested -= OnExecuteRequested;
            target.RetryRequested -= OnRetryRequested;
            target.ExitRequested -= ExitTraining;
            target.BecameUnavailable -= OnViewBecameUnavailable;
        }

        private void EndTraining()
        {
            trainingActive = false;
            StopPlayback();
            session = null;
            if (view != null && view.isActiveAndEnabled)
            {
                try
                {
                    view.Hide();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }

            SafeDispose(ref explorationGateToken);
            SafeDispose(ref uiBlockToken);
            if (inputReader != null)
            {
                inputReader.ClearPendingDialogueInput();
                inputReader.ClearPendingMenuInput();
                inputReader.ConsumeMapPressed();
                inputReader.ConsumeMapFloorStep();
                inputReader.ConsumeMapZoomDelta();
            }
        }

        private void StopPlayback()
        {
            if (playbackRoutine != null)
            {
                StopCoroutine(playbackRoutine);
                playbackRoutine = null;
            }

            resolving = false;
            if (view != null && view.isActiveAndEnabled)
            {
                try
                {
                    view.ClearPlaybackFocus();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }
        }

        private static string ActionLinePrefix(CombatTimelineEntry entry)
        {
            return "ACTION " + (entry.TimelineIndex + 1)
                + "/" + CombatTrainingBattle.TimelineCount
                + " [SPD " + entry.Initiative + "] - ";
        }

        private static string PlaybackHeadline(CombatTimelineEntry entry)
        {
            if (entry.Side == CombatTimelineSide.Ally)
            {
                if (!entry.HasAllyAction)
                {
                    return "행동선 " + (entry.TimelineIndex + 1) + " · 빈 아군 슬롯";
                }

                CombatTrainingSkillDefinition skill = CombatTrainingBattle.GetSkillDefinition(entry.AllyAction.SkillId);
                return "행동선 " + (entry.TimelineIndex + 1) + " · " + skill.DisplayName;
            }

            CombatTrainingEnemyDefinition enemy = CombatTrainingBattle.GetEnemyDefinition(entry.EnemyId);
            return "행동선 " + (entry.TimelineIndex + 1) + " · " + enemy.DisplayName + " " + entry.EnemyIntent.DisplayName;
        }

        private static string FormatTimelineResolution(CombatTimelineEntry entry, CombatTimelineResolution resolution)
        {
            if (resolution.Skipped)
            {
                return "행동선 " + (resolution.TimelineIndex + 1) + " 취소 · " + SkipReasonLabel(resolution.SkipReason);
            }

            if (resolution.Side == CombatTimelineSide.Ally)
            {
                CombatTrainingSkillDefinition skill = CombatTrainingBattle.GetSkillDefinition(entry.AllyAction.SkillId);
                CombatTrainingEnemyDefinition enemy = CombatTrainingBattle.GetEnemyDefinition(resolution.TargetEnemyId);
                string bonus = resolution.BonusTriggered
                    ? "보너스 성공 +" + resolution.BonusDamage
                    : "보너스 실패";
                string text = skill.DisplayName + " → " + enemy.DisplayName
                    + " · 확정 " + resolution.BaseDamage + " · " + bonus
                    + " (" + resolution.BonusChance + "%) · SP " + resolution.SpBefore + "→" + resolution.SpAfter;
                if (resolution.TargetDefeated)
                {
                    text += " · 적 행동 취소";
                }

                return text;
            }

            CombatTrainingEnemyDefinition enemyActor = CombatTrainingBattle.GetEnemyDefinition(resolution.EnemyActorId);
            CombatTrainingEnemyIntent intent = entry.EnemyIntent;
            string target = intent.TargetKind == CombatIntentTargetKind.All
                ? "아군 전체"
                : CombatTrainingBattle.GetUnitDefinition(resolution.TargetUnitId).DisplayName;
            string result = enemyActor.DisplayName + " " + intent.DisplayName + " → " + target
                + " · 피해 " + resolution.IncomingTotalDamage;
            if (resolution.SuppressionApplied > 0)
            {
                result += " · 억제 -" + resolution.SuppressionApplied;
            }

            return result;
        }

        private static string SkipReasonLabel(CombatTimelineSkipReason reason)
        {
            switch (reason)
            {
                case CombatTimelineSkipReason.EmptySlot:
                    return "비어 있는 슬롯";
                case CombatTimelineSkipReason.ActorDefeated:
                    return "행동자가 먼저 쓰러짐";
                case CombatTimelineSkipReason.TargetUnavailable:
                    return "공개 대상이 먼저 쓰러짐";
                case CombatTimelineSkipReason.EnemyDefeated:
                    return "적이 먼저 쓰러짐";
                case CombatTimelineSkipReason.InsufficientSp:
                    return "공유 SP 부족";
                default:
                    return "행동 불가";
            }
        }

        private static string PlanningError(string errorCode)
        {
            switch (errorCode)
            {
                case "combat_plan_incomplete":
                    return "생존한 모든 부대에 기술 하나씩을 배정해야 합니다.";
                case "combat_sp_sequence_invalid":
                    return "행동선 중간에 공유 SP가 부족합니다. SP 회복 기술을 앞쪽으로 옮겨 보세요.";
                case "combat_target_unavailable":
                    return "이미 쓰러진 적은 표적으로 선택할 수 없습니다.";
                case "combat_actor_defeated":
                    return "쓰러진 부대는 행동을 배정할 수 없습니다.";
                case "combat_plan_move_unavailable":
                    return "배정된 아군 행동만 앞뒤 슬롯으로 옮길 수 있습니다.";
                default:
                    return "지금은 그 명령을 처리할 수 없습니다.";
            }
        }

        private static void SafeDispose(ref IDisposable handle)
        {
            IDisposable disposable = handle;
            handle = null;
            if (disposable == null)
            {
                return;
            }

            try
            {
                disposable.Dispose();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}

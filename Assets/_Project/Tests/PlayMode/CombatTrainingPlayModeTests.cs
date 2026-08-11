using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DemonLord.Application;
using DemonLord.Application.Combat;
using DemonLord.Bootstrap;
using DemonLord.Domain;
using DemonLord.Domain.Combat;
using DemonLord.Infrastructure;
using DemonLord.Presentation.Combat;
using DemonLord.Presentation.Exploration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DemonLord.Tests.PlayMode
{
    public sealed class CombatTrainingPlayModeTests
    {
        private const string GameShellSceneName = "90_GameShell";
        private Keyboard keyboard;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (keyboard != null && keyboard.added)
            {
                InputSystem.RemoveDevice(keyboard);
            }

            keyboard = null;
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator LiaisonBuildsSpeedSortedSharedActionLine_ThenRestoresExplorationInput()
        {
            yield return LoadGameShell();
            GameShellRoot root = FindCompositionRoot(SceneManager.GetActiveScene());
            Task<string> initialization = root.InitializeAsync(
                CreateSession(),
                new EntryDestination(
                    GameShellSceneName,
                    ExplorationAreaIds.WorldAdjustmentLabInterior,
                    ExplorationSpawnIds.ReceptionStart),
                CreateSaveProgress(),
                CreateSettingsService(),
                new TestSceneFlowService(),
                new TestApplicationQuitter());
            while (!initialization.IsCompleted)
            {
                yield return null;
            }

            Assert.That(initialization.IsFaulted, Is.False, initialization.Exception?.ToString());
            Assert.That(root.AreaTransitionCoordinator.CurrentAreaRoot.TryGetInteractable(
                CombatTrainingCoordinator.LiaisonStableId,
                out PrototypeInteractable liaison), Is.True);

            CombatTrainingCoordinator combat = root.CombatTrainingCoordinator;
            DialogueFocusController dialogue = combat.GetComponent<DialogueFocusController>();
            Assert.That(combat, Is.Not.Null);
            Assert.That(dialogue, Is.Not.Null);
            combat.SetSessionFactoryForTests(() => new CombatTrainingSession(new SequenceRandom(0)));
            combat.SetPlaybackDelayScaleForTests(0.02f);

            Assert.That(liaison.TryInteract(null), Is.True);
            dialogue.EndDialogue();
            Assert.That(combat.IsTrainingActive, Is.False, "Closing dialogue is cancellation, not approval.");

            Assert.That(liaison.TryInteract(null), Is.True);
            while (dialogue.IsDialogueActive)
            {
                dialogue.AdvanceDialogue();
            }

            BattlePreparationView preparation = root.GetComponentInChildren<BattlePreparationView>(true);
            Assert.That(preparation, Is.Not.Null);
            Assert.That(preparation.IsVisible, Is.True);
            Assert.That(root.BattleHandoffCoordinator.CurrentRequest, Is.Not.Null);
            Assert.That(combat.IsTrainingActive, Is.False, "Completing dialogue must stop at the dispatch confirmation.");

            Button dispatch = preparation.GetComponentsInChildren<Button>(true)
                .First(button => string.Equals(button.name, "DispatchButton", StringComparison.Ordinal));
            dispatch.onClick.Invoke();

            Assert.That(combat.IsTrainingActive, Is.True);
            Assert.That(combat.View.IsVisible, Is.True);
            Assert.That(preparation.IsVisible, Is.False);
            Assert.That(combat.Session.Battle.Phase, Is.EqualTo(CombatTrainingPhase.Planning));
            Assert.That(combat.SelectedUnit, Is.EqualTo(CombatTrainingUnitId.Slime001));
            Assert.That(combat.SelectedTarget, Is.EqualTo(CombatTrainingEnemyId.TraineeSwordsman));

            // The battlefield is a full-screen illustration; control groups are overlays rather
            // than a separate centered stage on a dark background.
            Image backdrop = FindChild(combat.View, "CombatBackdrop").GetComponent<Image>();
            Assert.That(backdrop, Is.Not.Null);
            Assert.That(backdrop.sprite, Is.Not.Null);
            Assert.That(FindChild(combat.View, "CombatBackdropShade").GetComponent<Image>(), Is.Not.Null);
            AssertResponsiveCombatOverlayLayout(combat.View);

            Assert.That(root.InputReader.Gate.IsBlocked(ExplorationInputChannel.Movement), Is.True);
            Assert.That(root.InputReader.Gate.IsBlocked(ExplorationInputChannel.Dash), Is.True);
            Assert.That(root.InputReader.Gate.IsBlocked(ExplorationInputChannel.Interaction), Is.True);
            Assert.That(root.InputReader.Gate.IsBlocked(ExplorationInputChannel.Camera), Is.True);
            Assert.That(root.InputReader.Gate.IsBlocked(ExplorationInputChannel.Dialogue), Is.True);
            Assert.That(root.InGameUiCoordinator.IsExternalInputBlocked, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            for (int index = 0; index < CombatTrainingBattle.UnitCount; index++)
            {
                Assert.That(FindChild(combat.View, "AllyRoster_" + index).GetComponent<Button>(), Is.Not.Null);
            }

            Assert.That(FindChild(combat.View, "SelectedUnitSkillSet").activeSelf, Is.True);
            Assert.That(FindChild(combat.View, "SkillOption_0").GetComponent<Button>(), Is.Not.Null);
            Assert.That(FindChild(combat.View, "SkillOption_1").GetComponent<Button>(), Is.Not.Null);
            AssertActiveVisual(combat.View, "EnemyIntentArc_0_0_0");
            AssertActiveVisual(combat.View, "EnemyIntentArc_2_2_0");
            AssertActiveVisual(combat.View, "TargetPreviewArc_0");
            for (int index = 0; index < combat.Session.Battle.TimelineEntryCount; index++)
            {
                Assert.That(FindChild(combat.View, "TimelineSlot_" + index), Is.Not.Null);
                Assert.That(FindSpeedChip(combat.View, index).text, Does.Contain("SPD"));
            }

            ClickButton(combat.View, "EnemyTarget_2");
            Assert.That(combat.SelectedTarget, Is.EqualTo(CombatTrainingEnemyId.ApprenticeMage));
            Assert.That(FindChild(combat.View, "TargetPreviewArc_0").activeInHierarchy, Is.True);

            ClickButton(combat.View, "AllyRoster_0");
            ClickButton(combat.View, "SkillOption_0");
            AssertActiveVisual(combat.View, "PlannedTargetArc_0_0");
            AssertActiveVisual(combat.View, "PlannedTargetArc_0_Head");
            ClickButton(combat.View, "AllyRoster_1");
            AssertActiveVisual(combat.View, "PlannedTargetArc_0_0");
            ClickButton(combat.View, "AllyRoster_0");
            CombatTimelineEntry slipperyFloor = FindTimelineEntryForAlly(
                combat.Session.Battle,
                CombatTrainingUnitId.Slime001);
            Assert.That(slipperyFloor.Initiative, Is.EqualTo(52));
            Assert.That(slipperyFloor.TimelineIndex, Is.EqualTo(0));

            ClickButton(combat.View, "SkillOption_1");
            CombatTimelineEntry elasticCharge = FindTimelineEntryForAlly(
                combat.Session.Battle,
                CombatTrainingUnitId.Slime001);
            Assert.That(elasticCharge.Initiative, Is.EqualTo(28));
            Assert.That(elasticCharge.TimelineIndex, Is.GreaterThan(slipperyFloor.TimelineIndex));
            Assert.That(combat.Session.Battle.GetTimelineEntry(0).Side, Is.EqualTo(CombatTimelineSide.Enemy));
            Assert.That(combat.Session.Battle.GetTimelineEntry(1).Side, Is.EqualTo(CombatTimelineSide.Ally));

            // Put the fast Slime skill back, then fill the adjacent two-skill sets for the small roster.
            ClickButton(combat.View, "SkillOption_0");
            PlanRound(
                combat.View,
                CombatTrainingEnemyId.ApprenticeMage,
                CombatTrainingEnemyId.TraineeShieldbearer,
                CombatTrainingEnemyId.TraineeSwordsman,
                goblinSkillOption: 0);

            CombatTrainingBattle battle = combat.Session.Battle;
            Assert.That(battle.PlannedActionCount, Is.EqualTo(CombatTrainingBattle.UnitCount));
            Assert.That(battle.IsPlanAffordable, Is.True);
            AssertTimelineIsSpeedSorted(battle);
            CombatTimelineEntry plannedSlime = FindTimelineEntryForAlly(battle, CombatTrainingUnitId.Slime001);
            Assert.That(plannedSlime.TimelineIndex, Is.EqualTo(0));
            Assert.That(FindSpeedChip(combat.View, plannedSlime.TimelineIndex).text,
                Does.Contain(plannedSlime.Initiative.ToString()));
            for (int allyIndex = 0; allyIndex < CombatTrainingBattle.UnitCount; allyIndex++)
            {
                AssertActiveVisual(combat.View, "PlannedTargetArc_" + allyIndex + "_0");
                AssertActiveVisual(combat.View, "PlannedTargetArc_" + allyIndex + "_Head");
            }
            Assert.That(FindButton(combat.View, "ExecutePlan").interactable, Is.True);

            ClickButton(combat.View, "ExecutePlan");
            Assert.That(combat.IsResolving, Is.True);
            Assert.That(combat.Session.Battle.Phase, Is.EqualTo(CombatTrainingPhase.Resolving));
            Assert.That(FindChild(combat.View, "SelectedUnitSkillSet").activeSelf, Is.False);
            yield return null;
            Assert.That(FindTextContaining(combat.View, "[SPD "), Is.Not.Null);

            keyboard = InputSystem.AddDevice<Keyboard>();
            SetKeyboard(Key.Escape, Key.M);
            yield return null;
            SetKeyboard();
            yield return null;
            Assert.That(root.InGameUiCoordinator.State, Is.EqualTo(InGameMenuState.Closed));
            Assert.That(root.InGameUiCoordinator.IsMapOpen, Is.False);

            yield return WaitForPlayback(combat);
            Assert.That(combat.Session.Battle.Phase, Is.EqualTo(CombatTrainingPhase.Planning));
            Assert.That(combat.Session.Battle.Round, Is.EqualTo(2));
            Assert.That(FindChild(combat.View, "SelectedUnitSkillSet").activeSelf, Is.True);

            // The low-cost volley preserves the shared SP for the final winning action line.
            PlanRound(
                combat.View,
                CombatTrainingEnemyId.ApprenticeMage,
                CombatTrainingEnemyId.TraineeShieldbearer,
                CombatTrainingEnemyId.TraineeSwordsman,
                goblinSkillOption: 1);
            ClickButton(combat.View, "ExecutePlan");
            yield return WaitForPlayback(combat);
            Assert.That(combat.Session.Battle.Phase, Is.EqualTo(CombatTrainingPhase.Planning));
            Assert.That(combat.Session.Battle.Round, Is.EqualTo(3));

            PlanRound(
                combat.View,
                CombatTrainingEnemyId.ApprenticeMage,
                CombatTrainingEnemyId.TraineeShieldbearer,
                CombatTrainingEnemyId.ApprenticeMage,
                goblinSkillOption: 0);
            ClickButton(combat.View, "ExecutePlan");
            yield return WaitForPlayback(combat);
            Assert.That(combat.Session.Battle.Phase, Is.EqualTo(CombatTrainingPhase.Victory));
            Assert.That(FindButton(combat.View, "Retry").interactable, Is.True);

            ClickButton(combat.View, "Retry");
            Assert.That(combat.Session.Battle.Phase, Is.EqualTo(CombatTrainingPhase.Planning));
            Assert.That(combat.Session.Battle.Round, Is.EqualTo(1));
            Assert.That(combat.Session.Battle.PlannedActionCount, Is.Zero);
            Assert.That(root.InputReader.Gate.IsBlocked(ExplorationInputChannel.All), Is.True);
            Assert.That(root.InGameUiCoordinator.IsExternalInputBlocked, Is.True);

            ClickButton(combat.View, "Exit");
            Assert.That(combat.IsTrainingActive, Is.False);
            Assert.That(combat.View.IsVisible, Is.False);
            Assert.That(root.InputReader.Gate.LockedChannels, Is.EqualTo(ExplorationInputChannel.None));
            Assert.That(root.InGameUiCoordinator.IsExternalInputBlocked, Is.False);
        }

        private static IEnumerator LoadGameShell()
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(GameShellSceneName, LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
        }

        private static IEnumerator WaitForPlayback(CombatTrainingCoordinator combat)
        {
            float timeoutAt = Time.realtimeSinceStartup + 5f;
            while (combat.IsResolving && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            Assert.That(combat.IsResolving, Is.False, "The automatic shared action line did not finish before timeout.");
        }

        private static GameShellRoot FindCompositionRoot(Scene scene)
        {
            GameShellRoot root = null;
            foreach (GameObject candidate in scene.GetRootGameObjects())
            {
                GameShellRoot found = candidate.GetComponent<GameShellRoot>();
                if (found == null)
                {
                    continue;
                }

                Assert.That(root, Is.Null, "The shell scene must contain exactly one GameShellRoot.");
                root = found;
            }

            Assert.That(root, Is.Not.Null);
            return root;
        }

        private static InMemoryPlayerSession CreateSession()
        {
            Assert.That(SaveSlotId.TryCreate(SaveSlotId.Slot01Value, out SaveSlotId slotId), Is.True);
            Assert.That(NewGameSettings.TryCreate(
                "CombatTest",
                DifficultyId.NormalValue,
                TutorialMode.CoreValue,
                out NewGameSettings settings,
                out string errorCode), Is.True, errorCode);
            InMemoryPlayerSession session = new InMemoryPlayerSession();
            DateTime now = DateTime.UtcNow;
            GameSave save = GameSave.CreateNew(slotId, settings, "combat-playmode-test", now);
            Assert.That(GameEntryPoint.TryCreate(
                GameEntryPoint.PrologueStartId,
                LabCheckpointId.ArchiveCatalogued,
                out GameEntryPoint progress,
                out string progressError), Is.True, progressError);
            session.SetCurrentSave(save.WithProgress(progress, now));
            return session;
        }

        private static SaveGameProgressUseCase CreateSaveProgress()
        {
            return new SaveGameProgressUseCase(
                new FileSaveRepository(
                    Path.Combine(UnityEngine.Application.temporaryCachePath, "DemonLordCombatTrainingPlayMode"),
                    new UnityJsonSaveSerializer(),
                    new NoSaveMigrationPipeline()),
                new SystemClock());
        }

        private static SettingsService CreateSettingsService()
        {
            SettingsService settings = new SettingsService(new TestSettingsRepository(), new TestSettingsApplier());
            settings.LoadAndApply();
            return settings;
        }

        private void SetKeyboard(params Key[] pressedKeys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(pressedKeys));
            InputSystem.Update();
        }

        private static void PlanRound(
            CombatTrainingView view,
            CombatTrainingEnemyId slimeTarget,
            CombatTrainingEnemyId skeletonTarget,
            CombatTrainingEnemyId goblinTarget,
            int goblinSkillOption)
        {
            PlanUnit(view, 0, slimeTarget, 0);
            PlanUnit(view, 1, skeletonTarget, 1);
            PlanUnit(view, 2, goblinTarget, goblinSkillOption);
        }

        private static void PlanUnit(
            CombatTrainingView view,
            int unitIndex,
            CombatTrainingEnemyId target,
            int skillOption)
        {
            ClickButton(view, "AllyRoster_" + unitIndex);
            ClickButton(view, "EnemyTarget_" + (int)target);
            ClickButton(view, "SkillOption_" + skillOption);
        }

        private static CombatTimelineEntry FindTimelineEntryForAlly(
            CombatTrainingBattle battle,
            CombatTrainingUnitId unitId)
        {
            for (int index = 0; index < battle.TimelineEntryCount; index++)
            {
                CombatTimelineEntry entry = battle.GetTimelineEntry(index);
                if (!entry.HasAllyAction)
                {
                    continue;
                }

                CombatTrainingSkillDefinition skill = CombatTrainingBattle.GetSkillDefinition(entry.AllyAction.SkillId);
                if (skill.ActorId == unitId)
                {
                    return entry;
                }
            }

            Assert.Fail("The planned ally was missing from the dynamic action line: " + unitId);
            return default;
        }

        private static void AssertTimelineIsSpeedSorted(CombatTrainingBattle battle)
        {
            CombatTimelineEntry previous = battle.GetTimelineEntry(0);
            for (int index = 1; index < battle.TimelineEntryCount; index++)
            {
                CombatTimelineEntry current = battle.GetTimelineEntry(index);
                Assert.That(previous.Initiative, Is.GreaterThanOrEqualTo(current.Initiative),
                    "Action line must be ordered by initiative descending.");
                if (previous.Initiative == current.Initiative)
                {
                    Assert.That(previous.TieBreakOrder, Is.LessThanOrEqualTo(current.TieBreakOrder),
                        "Equal initiative actions must keep the stable tie order.");
                }

                previous = current;
            }
        }

        private static void AssertResponsiveCombatOverlayLayout(CombatTrainingView view)
        {
            RectTransform header = FindChild(view, "Header").GetComponent<RectTransform>();
            AssertAnchors(header, new Vector2(0f, 1f), Vector2.one, "Header must stretch across the top edge.");

            RectTransform roster = FindChild(view, "AllyRoster").GetComponent<RectTransform>();
            Assert.That(roster.anchorMin.x, Is.EqualTo(0f), "Ally roster must stay left-anchored.");
            Assert.That(roster.anchorMax.x, Is.EqualTo(0f), "Ally roster must stay left-anchored.");

            RectTransform skillSet = FindChild(view, "SelectedUnitSkillSet").GetComponent<RectTransform>();
            Assert.That(skillSet.anchorMin.x, Is.EqualTo(0f), "Skill set must stay left-anchored.");
            Assert.That(skillSet.anchorMax.x, Is.EqualTo(0f), "Skill set must stay left-anchored.");

            RectTransform timeline = FindChild(view, "IntegratedActionTimeline").GetComponent<RectTransform>();
            AssertAnchors(timeline, Vector2.zero, new Vector2(1f, 0f), "Timeline must stretch across the bottom edge.");

            RectTransform battleStage = FindChild(view, "BattleStage").GetComponent<RectTransform>();
            AssertAnchors(battleStage, Vector2.zero, Vector2.one, "Battle stage must stretch across the viewport.");

            RectTransform title = FindChild(view, "Title").GetComponent<RectTransform>();
            Assert.That(title.pivot.x, Is.EqualTo(0f).Within(0.0001f), "Header title must use a left pivot.");

            RectTransform enemyFieldLabel = FindChild(view, "EnemyFieldLabel").GetComponent<RectTransform>();
            Assert.That(enemyFieldLabel.pivot.x, Is.EqualTo(1f).Within(0.0001f), "Enemy field label must use a right pivot.");
        }

        private static void AssertAnchors(RectTransform rect, Vector2 minimum, Vector2 maximum, string message)
        {
            Assert.That(rect, Is.Not.Null, message);
            Assert.That(rect.anchorMin, Is.EqualTo(minimum), message);
            Assert.That(rect.anchorMax, Is.EqualTo(maximum), message);
        }

        private static void AssertActiveVisual(CombatTrainingView view, string name)
        {
            GameObject visual = FindChild(view, name);
            Assert.That(visual.activeInHierarchy, Is.True, name + " must be visible.");
            Graphic graphic = visual.GetComponent<Graphic>();
            Assert.That(graphic, Is.Not.Null, name + " must be a UI graphic.");
            Assert.That(graphic.raycastTarget, Is.False, name + " must not block combat input.");
        }

        private static Text FindSpeedChip(CombatTrainingView view, int timelineIndex)
        {
            GameObject slot = FindChild(view, "TimelineSlot_" + timelineIndex);
            foreach (Text candidate in slot.GetComponentsInChildren<Text>(true))
            {
                if (string.Equals(candidate.name, "SpeedChip", StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            Assert.Fail("Timeline slot is missing its speed chip: " + timelineIndex);
            return null;
        }

        private static Text FindTextContaining(CombatTrainingView view, string text)
        {
            foreach (Text candidate in view.GetComponentsInChildren<Text>(true))
            {
                if (!string.IsNullOrEmpty(candidate.text)
                    && candidate.text.IndexOf(text, StringComparison.Ordinal) >= 0)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void ClickButton(CombatTrainingView view, string name)
        {
            Button button = FindButton(view, name);
            Assert.That(button.gameObject.activeInHierarchy, Is.True, name + " must be visible before it is clicked.");
            Assert.That(button.interactable, Is.True, name + " must be interactable before it is clicked.");
            button.onClick.Invoke();
        }

        private static Button FindButton(CombatTrainingView view, string name)
        {
            GameObject child = FindChild(view, name);
            Button button = child.GetComponent<Button>();
            Assert.That(button, Is.Not.Null, name + " must have a Button component.");
            return button;
        }

        private static GameObject FindChild(CombatTrainingView view, string name)
        {
            GameObject match = FindChildOrNull(view, name);
            Assert.That(match, Is.Not.Null, "Combat view child was not found: " + name);
            return match;
        }

        private static GameObject FindChildOrNull(CombatTrainingView view, string name)
        {
            Transform match = null;
            foreach (Transform candidate in view.GetComponentsInChildren<Transform>(true))
            {
                if (!string.Equals(candidate.name, name, StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.That(match, Is.Null, "Combat view child names used by tests must be unique: " + name);
                match = candidate;
            }

            return match != null ? match.gameObject : null;
        }

        private sealed class SequenceRandom : ICombatRandomSource
        {
            private readonly int[] values;
            private int index;

            public SequenceRandom(params int[] values)
            {
                this.values = values;
            }

            public int NextPercent()
            {
                int value = values[Math.Min(index, values.Length - 1)];
                index++;
                return value;
            }
        }

        private sealed class TestSettingsApplier : IGameSettingsRuntimeApplier
        {
            public void Apply(GameSettings settings)
            {
            }
        }

        private sealed class TestSettingsRepository : ISettingsRepository
        {
            public SettingsReadResult Load()
            {
                return SettingsReadResult.Success(GameSettings.Default, false);
            }

            public SettingsWriteResult Save(GameSettings settings)
            {
                return SettingsWriteResult.Success();
            }
        }

        private sealed class TestSceneFlowService : ISceneFlowService
        {
            public Task LoadFrontendAsync(FrontendEntryMode entryMode)
            {
                return Task.CompletedTask;
            }

            public Task LoadEntryAsync(EntryDestination destination)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class TestApplicationQuitter : IApplicationQuitter
        {
            public void Quit()
            {
            }
        }
    }
}

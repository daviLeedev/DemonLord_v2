using System;
using DemonLord.Application;
using DemonLord.Bootstrap;
using DemonLord.Domain;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    public sealed class InGameUiCoordinator : MonoBehaviour
    {
        private static readonly Vector2Int[] SupportedResolutions =
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440),
            new Vector2Int(3440, 1440),
        };

        [SerializeField] private ExplorationInputReader inputReader = null;
        [SerializeField] private DialogueFocusController dialogueController = null;
        [SerializeField] private PauseMenuView pauseMenuView = null;
        [SerializeField] private MapCoordinator mapCoordinator = null;
        [SerializeField] private AreaTransitionCoordinator areaTransitionCoordinator = null;

        private readonly InGameMenuStateMachine stateMachine = new InGameMenuStateMachine();
        private IPlayerSession playerSession;
        private SaveGameProgressUseCase saveProgress;
        private SettingsService settingsService;
        private ISceneFlowService sceneFlowService;
        private IApplicationQuitter applicationQuitter;
        private IDisposable pauseGateToken;
        private float previousTimeScale = 1f;
        private bool pauseApplied;
        private bool initialized;

        public InGameMenuState State => stateMachine.State;

        public bool IsInitialized => initialized;

        public bool IsMapOpen => mapCoordinator != null && mapCoordinator.IsMapOpen;

        public void Configure(
            ExplorationInputReader configuredInputReader,
            DialogueFocusController configuredDialogueController,
            PauseMenuView configuredPauseMenuView)
        {
            inputReader = configuredInputReader;
            dialogueController = configuredDialogueController;
            pauseMenuView = configuredPauseMenuView;
        }

        public void Configure(
            ExplorationInputReader configuredInputReader,
            DialogueFocusController configuredDialogueController,
            PauseMenuView configuredPauseMenuView,
            MapCoordinator configuredMapCoordinator,
            AreaTransitionCoordinator configuredAreaTransitionCoordinator)
        {
            Configure(configuredInputReader, configuredDialogueController, configuredPauseMenuView);
            mapCoordinator = configuredMapCoordinator;
            areaTransitionCoordinator = configuredAreaTransitionCoordinator;
        }

        public bool TryInitialize(
            IPlayerSession configuredPlayerSession,
            SaveGameProgressUseCase configuredSaveProgress,
            SettingsService configuredSettingsService,
            ISceneFlowService configuredSceneFlowService,
            IApplicationQuitter configuredApplicationQuitter,
            out string errorCode)
        {
            errorCode = null;
            if (initialized)
            {
                errorCode = "in_game_ui_already_initialized";
                return false;
            }

            if (inputReader == null || dialogueController == null || pauseMenuView == null)
            {
                errorCode = "in_game_ui_reference_missing";
                return false;
            }

            if (configuredPlayerSession == null
                || configuredSaveProgress == null
                || configuredSettingsService == null
                || configuredSceneFlowService == null
                || configuredApplicationQuitter == null)
            {
                errorCode = "in_game_ui_service_missing";
                return false;
            }

            playerSession = configuredPlayerSession;
            saveProgress = configuredSaveProgress;
            settingsService = configuredSettingsService;
            sceneFlowService = configuredSceneFlowService;
            applicationQuitter = configuredApplicationQuitter;
            SubscribeView();
            pauseMenuView.ApplySettings(settingsService.Persisted);
            pauseMenuView.Hide();
            if (mapCoordinator != null && !mapCoordinator.TryInitialize(out errorCode))
            {
                UnsubscribeView();
                return false;
            }

            inputReader.ClearPendingMenuInput();
            initialized = true;
            return true;
        }

        private void Update()
        {
            if (!initialized || inputReader == null)
            {
                return;
            }

            bool mapPressed = inputReader.ConsumeMapPressed();
            bool pausePressed = inputReader.ConsumePausePressed();
            bool backPressed = inputReader.ConsumeBackPressed();

            if (dialogueController != null && dialogueController.IsDialogueActive)
            {
                if (pausePressed || backPressed)
                {
                    dialogueController.EndDialogue();
                    inputReader.ClearPendingMenuInput();
                }

                return;
            }

            if (areaTransitionCoordinator != null && areaTransitionCoordinator.IsBusy)
            {
                inputReader.ConsumeMapFloorStep();
                inputReader.ConsumeMapZoomDelta();
                return;
            }

            if (IsMapOpen)
            {
                if (mapPressed || pausePressed || backPressed)
                {
                    CloseMap();
                    return;
                }

                mapCoordinator.CycleFloor(inputReader.ConsumeMapFloorStep());
                mapCoordinator.AdjustZoom(inputReader.ConsumeMapZoomDelta());
                return;
            }

            inputReader.ConsumeMapFloorStep();
            inputReader.ConsumeMapZoomDelta();
            if (mapPressed && stateMachine.State == InGameMenuState.Closed)
            {
                OpenMap();
                return;
            }

            if (pausePressed)
            {
                HandlePausePressed();
                return;
            }

            if (backPressed)
            {
                HandleBackPressed();
                return;
            }

            if (stateMachine.State == InGameMenuState.Closed || stateMachine.State == InGameMenuState.Busy)
            {
                return;
            }

            int navigationStep = inputReader.ConsumeMenuNavigationStep();
            if (navigationStep != 0)
            {
                pauseMenuView.MoveSelection(navigationStep);
            }

            if (inputReader.ConsumeMenuSubmitPressed())
            {
                pauseMenuView.SubmitFocused();
            }
        }

        private void OnDisable()
        {
            mapCoordinator?.CloseMap();
            CancelSettingsEditIfNeeded();
            RestoreExplorationState();
        }

        private void OnDestroy()
        {
            mapCoordinator?.CloseMap();
            UnsubscribeView();
            CancelSettingsEditIfNeeded();
            RestoreExplorationState();
        }

        private void HandlePausePressed()
        {
            if (dialogueController != null && dialogueController.IsDialogueActive)
            {
                dialogueController.EndDialogue();
                inputReader.ClearPendingMenuInput();
                return;
            }

            HandleMenuBack();
        }

        private void HandleBackPressed()
        {
            if (dialogueController != null && dialogueController.IsDialogueActive)
            {
                dialogueController.EndDialogue();
                inputReader.ClearPendingMenuInput();
                return;
            }

            if (stateMachine.State != InGameMenuState.Closed)
            {
                HandleMenuBack();
            }
        }

        private void HandleMenuBack()
        {
            if (stateMachine.State == InGameMenuState.Closed)
            {
                OpenRootMenu();
                return;
            }

            if (stateMachine.State == InGameMenuState.Busy)
            {
                return;
            }

            bool cancelSettings = stateMachine.State == InGameMenuState.Settings;
            InGameMenuBackResult result = stateMachine.TryBack();
            if (result == InGameMenuBackResult.Rejected)
            {
                return;
            }

            if (cancelSettings)
            {
                settingsService.CancelEdit();
                pauseMenuView.ApplySettings(settingsService.Persisted);
            }

            if (result == InGameMenuBackResult.CloseMenu)
            {
                pauseMenuView.Hide();
                RestoreExplorationState();
                inputReader.ClearPendingMenuInput();
                return;
            }

            ShowRoot();
        }

        private void OpenRootMenu()
        {
            if (!stateMachine.TryOpenRoot())
            {
                return;
            }

            ApplyPauseState();
            ShowRoot();
            inputReader.ClearPendingMenuInput();
        }

        private void OpenMap()
        {
            if (mapCoordinator == null || stateMachine.State != InGameMenuState.Closed)
            {
                return;
            }

            ApplyPauseState();
            if (!mapCoordinator.TryOpenMap())
            {
                RestoreExplorationState();
                return;
            }

            inputReader.ClearPendingMenuInput();
        }

        private void CloseMap()
        {
            if (mapCoordinator == null || !mapCoordinator.IsMapOpen)
            {
                return;
            }

            mapCoordinator.CloseMap();
            RestoreExplorationState();
            inputReader.ClearPendingMenuInput();
        }

        private void ShowRoot(string status = null)
        {
            pauseMenuView.ShowRoot(playerSession != null && playerSession.CurrentSave != null, status);
        }

        private void OnContinueRequested()
        {
            if (stateMachine.State != InGameMenuState.Root)
            {
                return;
            }

            HandleMenuBack();
        }

        private void OnSaveRequested()
        {
            if (stateMachine.State != InGameMenuState.Root || playerSession.CurrentSave == null || !stateMachine.TryBeginBusy())
            {
                return;
            }

            pauseMenuView.ShowBusy("기록을 저장하는 중입니다...");
            SaveWriteResult result;
            try
            {
                result = areaTransitionCoordinator != null && areaTransitionCoordinator.LocationState != null
                    ? saveProgress.Execute(
                        playerSession,
                        playerSession.CurrentSave.Progress.CheckpointId,
                        areaTransitionCoordinator.LocationState.Current)
                    : saveProgress.Execute(playerSession, playerSession.CurrentSave.Progress.CheckpointId);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                stateMachine.TryCompleteBusy(false);
                ShowRoot("기록을 저장하지 못했습니다. 다시 시도해 주세요.");
                return;
            }

            stateMachine.TryCompleteBusy(result.IsSuccess);
            if (result.IsSuccess)
            {
                pauseMenuView.PlaySaveComplete();
                ShowRoot("기록을 저장했습니다.");
            }
            else
            {
                Debug.LogWarning("Manual save failed: " + result.ErrorCode, this);
                ShowRoot("기록을 저장하지 못했습니다. 다시 시도해 주세요.");
            }
        }

        private void OnSettingsRequested()
        {
            if (!stateMachine.TryOpenSettings())
            {
                return;
            }

            GameSettings working = settingsService.BeginEdit();
            pauseMenuView.ApplySettings(working);
            pauseMenuView.SetSettingsPage(PauseSettingsPage.Audio, working);
            pauseMenuView.ShowSettings(settingsService.Working);
        }

        private void OnControlsRequested()
        {
            if (stateMachine.State == InGameMenuState.Controls)
            {
                HandleMenuBack();
                return;
            }

            if (stateMachine.TryOpenControls())
            {
                pauseMenuView.ShowControls();
            }
        }

        private void OnReturnToTitleRequested()
        {
            if (stateMachine.TryConfirmReturnToTitle())
            {
                pauseMenuView.ShowConfirmation(true);
            }
        }

        private void OnQuitRequested()
        {
            if (stateMachine.TryConfirmQuit())
            {
                pauseMenuView.ShowConfirmation(false);
            }
        }

        private void OnSettingsPageRequested(PauseSettingsPage page)
        {
            if (stateMachine.State == InGameMenuState.Settings)
            {
                pauseMenuView.SetSettingsStatus(string.Empty, Color.white);
                pauseMenuView.SetSettingsPage(page, settingsService.Working);
            }
        }

        private void OnSettingsChangeRequested(PauseSettingsChange change)
        {
            if (stateMachine.State != InGameMenuState.Settings)
            {
                return;
            }

            GameSettings current = settingsService.Working;
            settingsService.SetWorking(ApplySettingsChange(current, change));
            pauseMenuView.ApplySettings(settingsService.Working);
            pauseMenuView.SetSettingsStatus(string.Empty, Color.white);
            pauseMenuView.RenderSettings(settingsService.Working);
        }

        private void OnSettingsResetRequested()
        {
            if (stateMachine.State == InGameMenuState.Settings)
            {
                settingsService.ResetWorking();
                pauseMenuView.ApplySettings(settingsService.Working);
                pauseMenuView.SetSettingsStatus(string.Empty, Color.white);
                pauseMenuView.RenderSettings(settingsService.Working);
            }
        }

        private void OnSettingsApplyRequested()
        {
            if (stateMachine.State != InGameMenuState.Settings || !stateMachine.TryBeginBusy())
            {
                return;
            }

            SettingsWriteResult result;
            try
            {
                result = settingsService.SaveWorking();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                stateMachine.TryCompleteBusy(false);
                pauseMenuView.ShowSettings(settingsService.Working);
                pauseMenuView.SetSettingsStatus("\uD658\uACBD \uC124\uC815\uC744 \uC800\uC7A5\uD558\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4. \uB2E4\uC2DC \uC2DC\uB3C4\uD574 \uC8FC\uC138\uC694.", new Color(0.85f, 0.32f, 0.34f));
                return;
            }

            if (result.IsSuccess)
            {
                stateMachine.TryCompleteBusy(true);
                pauseMenuView.ApplySettings(settingsService.Persisted);
                ShowRoot("환경 설정을 적용했습니다.");
            }
            else
            {
                Debug.LogWarning("Settings save failed: " + result.ErrorCode, this);
                stateMachine.TryCompleteBusy(false);
                pauseMenuView.ShowSettings(settingsService.Working);
                pauseMenuView.SetSettingsStatus("\uD658\uACBD \uC124\uC815\uC744 \uC800\uC7A5\uD558\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4. \uB2E4\uC2DC \uC2DC\uB3C4\uD574 \uC8FC\uC138\uC694.", new Color(0.85f, 0.32f, 0.34f));
            }
        }

        private void OnSettingsCancelRequested()
        {
            if (stateMachine.State == InGameMenuState.Settings)
            {
                HandleMenuBack();
            }
        }

        private async void OnConfirmationAccepted()
        {
            if (stateMachine.State == InGameMenuState.ConfirmQuit)
            {
                if (stateMachine.TryBeginBusy())
                {
                    pauseMenuView.ShowBusy("게임을 종료하는 중입니다...");
                    applicationQuitter.Quit();
                }

                return;
            }

            if (stateMachine.State != InGameMenuState.ConfirmReturnToTitle || !stateMachine.TryBeginBusy())
            {
                return;
            }

            GameSave originalSave = playerSession.CurrentSave;
            pauseMenuView.ShowBusy("타이틀 화면으로 이동하는 중입니다...");
            RestoreExplorationState();
            try
            {
                playerSession.Clear();
                await sceneFlowService.LoadFrontendAsync(FrontendEntryMode.MainMenu);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                if (originalSave != null)
                {
                    playerSession.SetCurrentSave(originalSave);
                }

                stateMachine.TryCompleteBusy(false);
                stateMachine.ForceRoot();
                ApplyPauseState();
                ShowRoot("타이틀 화면으로 이동하지 못했습니다.");
            }
        }

        private void OnConfirmationCancelled()
        {
            if (stateMachine.State == InGameMenuState.ConfirmReturnToTitle || stateMachine.State == InGameMenuState.ConfirmQuit)
            {
                stateMachine.TryBack();
                ShowRoot();
            }
        }

        private void ApplyPauseState()
        {
            if (pauseApplied)
            {
                return;
            }

            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            pauseGateToken = inputReader.Gate.AcquireLock(
                ExplorationInputChannel.Movement |
                ExplorationInputChannel.Dash |
                ExplorationInputChannel.Interaction |
                ExplorationInputChannel.Camera |
                ExplorationInputChannel.Dialogue);
            pauseApplied = true;
        }

        private void RestoreExplorationState()
        {
            if (!pauseApplied)
            {
                return;
            }

            IDisposable token = pauseGateToken;
            pauseGateToken = null;
            token?.Dispose();
            Time.timeScale = previousTimeScale;
            pauseApplied = false;
        }

        private void CancelSettingsEditIfNeeded()
        {
            if (initialized && stateMachine.State == InGameMenuState.Settings && settingsService != null)
            {
                settingsService.CancelEdit();
                pauseMenuView?.ApplySettings(settingsService.Persisted);
            }
        }

        private void SubscribeView()
        {
            pauseMenuView.ContinueRequested += OnContinueRequested;
            pauseMenuView.SaveRequested += OnSaveRequested;
            pauseMenuView.SettingsRequested += OnSettingsRequested;
            pauseMenuView.ControlsRequested += OnControlsRequested;
            pauseMenuView.ReturnToTitleRequested += OnReturnToTitleRequested;
            pauseMenuView.QuitRequested += OnQuitRequested;
            pauseMenuView.SettingsApplyRequested += OnSettingsApplyRequested;
            pauseMenuView.SettingsCancelRequested += OnSettingsCancelRequested;
            pauseMenuView.SettingsResetRequested += OnSettingsResetRequested;
            pauseMenuView.SettingsPageRequested += OnSettingsPageRequested;
            pauseMenuView.SettingsChangeRequested += OnSettingsChangeRequested;
            pauseMenuView.ConfirmationAccepted += OnConfirmationAccepted;
            pauseMenuView.ConfirmationCancelled += OnConfirmationCancelled;
        }

        private void UnsubscribeView()
        {
            if (pauseMenuView == null)
            {
                return;
            }

            pauseMenuView.ContinueRequested -= OnContinueRequested;
            pauseMenuView.SaveRequested -= OnSaveRequested;
            pauseMenuView.SettingsRequested -= OnSettingsRequested;
            pauseMenuView.ControlsRequested -= OnControlsRequested;
            pauseMenuView.ReturnToTitleRequested -= OnReturnToTitleRequested;
            pauseMenuView.QuitRequested -= OnQuitRequested;
            pauseMenuView.SettingsApplyRequested -= OnSettingsApplyRequested;
            pauseMenuView.SettingsCancelRequested -= OnSettingsCancelRequested;
            pauseMenuView.SettingsResetRequested -= OnSettingsResetRequested;
            pauseMenuView.SettingsPageRequested -= OnSettingsPageRequested;
            pauseMenuView.SettingsChangeRequested -= OnSettingsChangeRequested;
            pauseMenuView.ConfirmationAccepted -= OnConfirmationAccepted;
            pauseMenuView.ConfirmationCancelled -= OnConfirmationCancelled;
        }

        private static GameSettings ApplySettingsChange(GameSettings current, PauseSettingsChange change)
        {
            const float VolumeStep = 0.05f;
            const float UiScaleStep = 0.05f;
            switch (change)
            {
                case PauseSettingsChange.MasterDown: return current.With(masterVolume: current.MasterVolume - VolumeStep);
                case PauseSettingsChange.MasterUp: return current.With(masterVolume: current.MasterVolume + VolumeStep);
                case PauseSettingsChange.BgmDown: return current.With(bgmVolume: current.BgmVolume - VolumeStep);
                case PauseSettingsChange.BgmUp: return current.With(bgmVolume: current.BgmVolume + VolumeStep);
                case PauseSettingsChange.SfxDown: return current.With(sfxVolume: current.SfxVolume - VolumeStep);
                case PauseSettingsChange.SfxUp: return current.With(sfxVolume: current.SfxVolume + VolumeStep);
                case PauseSettingsChange.DisplayModePrevious: return current.With(displayMode: CycleDisplayMode(current.DisplayMode, -1));
                case PauseSettingsChange.DisplayModeNext: return current.With(displayMode: CycleDisplayMode(current.DisplayMode, 1));
                case PauseSettingsChange.ResolutionPrevious: return WithResolution(current, -1);
                case PauseSettingsChange.ResolutionNext: return WithResolution(current, 1);
                case PauseSettingsChange.VSyncToggle: return current.With(vSyncEnabled: !current.VSyncEnabled);
                case PauseSettingsChange.QualityPrevious: return current.With(qualityPreset: CycleQuality(current.QualityPreset, -1));
                case PauseSettingsChange.QualityNext: return current.With(qualityPreset: CycleQuality(current.QualityPreset, 1));
                case PauseSettingsChange.UiScaleDown: return current.With(uiScale: current.UiScale - UiScaleStep);
                case PauseSettingsChange.UiScaleUp: return current.With(uiScale: current.UiScale + UiScaleStep);
                case PauseSettingsChange.ScreenShakeToggle: return current.With(reduceScreenShake: !current.ReduceScreenShake);
                case PauseSettingsChange.FlashesToggle: return current.With(reduceFlashes: !current.ReduceFlashes);
                default: return current.With(reduceTransitions: !current.ReduceTransitions);
            }
        }

        private static GameSettings WithResolution(GameSettings current, int direction)
        {
            int currentIndex = 0;
            for (int index = 0; index < SupportedResolutions.Length; index++)
            {
                if (SupportedResolutions[index].x == current.ResolutionWidth && SupportedResolutions[index].y == current.ResolutionHeight)
                {
                    currentIndex = index;
                    break;
                }
            }

            int next = (currentIndex + direction + SupportedResolutions.Length) % SupportedResolutions.Length;
            return current.With(resolutionWidth: SupportedResolutions[next].x, resolutionHeight: SupportedResolutions[next].y);
        }

        private static DisplayModeId CycleDisplayMode(DisplayModeId current, int direction)
        {
            int next = ((int)current + direction + 3) % 3;
            return (DisplayModeId)next;
        }

        private static QualityPresetId CycleQuality(QualityPresetId current, int direction)
        {
            int next = ((int)current + direction + 3) % 3;
            return (QualityPresetId)next;
        }
    }
}

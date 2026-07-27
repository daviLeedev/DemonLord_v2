using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DemonLord.Application;
using DemonLord.Bootstrap;
using DemonLord.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DemonLord.Presentation
{
    public sealed class FrontendView : MonoBehaviour
    {
        private enum UiButtonSound
        {
            Select,
            Cancel
        }

        private static readonly Color Paper = ColorFromHex("E7E2D8");
        private static readonly Color Iron = ColorFromHex("171A20");
        private static readonly Color Warning = ColorFromHex("B73B3B");
        private static readonly Color Focus = ColorFromHex("78BFFF");
        private static readonly Color MenuRow = ColorFromHex("101722");
        private static readonly Color ButtonText = ColorFromHex("FFF0D0");
        private static readonly Color OpeningPrimary = ColorFromHex("101720");
        private static readonly Color OpeningPrimaryLight = ColorFromHex("26384B");
        private static readonly Color OpeningCyan = ColorFromHex("66C7E8");
        private static readonly Color OpeningGold = ColorFromHex("B99A59");
        private static readonly Color OpeningBurgundy = ColorFromHex("2A1018");
        private const string DeveloperLogoPath = "UI/Boot/logo_developer_four_ravens";
        private const string TitleLogoPath = "UI/Title/title_logo_ko_transparent";
        private const string TitleBackgroundPath = "UI/Title/title_castle_ruins_background";
        private const string ContinueIconPath = "UI/Menu/continue_transparent";
        private const string NewGameIconPath = "UI/Menu/new_game_transparent";
        private const string LoadGameIconPath = "UI/Menu/load_game_transparent";
        private const string SettingsIconPath = "UI/Menu/settings_transparent";
        private const string ArchiveIconPath = "UI/Menu/archive_transparent";
        private const string ExitIconPath = "UI/Menu/exit_transparent";
        private const string MainMenuPanelPath = "UI/Menu/main_menu_panel_transparent";
        private const string StoryDifficultyIconPath = "UI/Difficulty/story_transparent";
        private const string NormalDifficultyIconPath = "UI/Difficulty/normal_transparent";
        private const string HardDifficultyIconPath = "UI/Difficulty/hard_transparent";
        private const string SaveSlotCardPath = "UI/Save/save_slot_card";
        private const string SaveThumbnailFramePath = "UI/Save/save_thumbnail_frame";
        private const string SaveBadgeEmptyPath = "UI/Save/badge_empty";
        private const string SaveBadgeValidPath = "UI/Save/badge_valid";
        private const string SaveBadgeRecoveredPath = "UI/Save/badge_recovered";
        private const string SaveBadgeCorruptPath = "UI/Save/badge_corrupt";
        private const string SaveBadgeIncompatiblePath = "UI/Save/badge_incompatible";
        private const string StandardButtonPath = "UI/Common/button_standard";
        private const string ModalFramePath = "UI/Common/modal_frame";
        private const string NoticeWindowFramePath = "UI/Common/notice_window_frame";
        private const string TitlePromptFramePath = "UI/Common/title_prompt_frame";
        private const string LoadingSealPath = "UI/Common/loading_seal";
        private const string LockedBadgePath = "UI/Common/badge_locked";
        private const string SettingsWindowFramePath = "UI/Settings/settings_window_frame";
        private const string SettingsTabPath = "UI/Settings/settings_tab";
        private const string SettingsValueSelectorPath = "UI/Settings/settings_value_selector";
        private const string SettingsStepperButtonPath = "UI/Settings/button_stepper";
        private const string SelectionCardWidePath = "UI/NewGame/selection_card_wide";
        private const string SelectionCardMediumPath = "UI/NewGame/selection_card_medium";
        private const string FrontendMusicPath = "Audio/Bgm/frontend_ambient";
        private const string FocusSoundPath = "Audio/Ui/ui_focus_01";
        private const string SelectSoundPath = "Audio/Ui/ui_select_01";
        private const string CancelSoundPath = "Audio/Ui/ui_cancel_01";
        private const string DisabledSoundPath = "Audio/Ui/ui_disabled_01";
        private const string ErrorSoundPath = "Audio/Ui/ui_error_01";
        private const string SaveCompleteSoundPath = "Audio/Ui/ui_save_complete_01";

        private FrontendCoordinator coordinator;
        private ISceneFlowService sceneFlowService;
        private SettingsService settingsService;
        private RectTransform contentRoot;
        private CanvasScaler canvasScaler;
        private GraphicRaycaster graphicRaycaster;
        private EventSystem eventSystem;
        private Image fadeOverlay;
        private string selectedDifficultyId = DifficultyId.NormalValue;
        private int tutorialSelection;
        private bool titleCanSkip;
        private int logoStage;
        private AudioSource uiSoundSource;
        private AudioClip focusSound;
        private AudioClip selectSound;
        private AudioClip cancelSound;
        private AudioClip disabledSound;
        private AudioClip errorSound;
        private AudioClip saveCompleteSound;
        private FrontendUiTheme theme;
        private AudioSource backgroundMusicSource;
        private GameSettings settingsDraft;
        private int settingsPage;
        private string settingsErrorMessage;
        private float uiTextScale = 1f;
        private bool isInputLocked;
        private bool isLoadingEntry;
        private GameObject lastFocusedButton;
        private CanvasGroup openingContentGroup;
        private BootAtmosphere openingAtmosphere;

        public void Initialize(
            FrontendCoordinator coordinator,
            ISceneFlowService sceneFlowService,
            SettingsService settingsService,
            FrontendEntryMode entryMode)
        {
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.sceneFlowService = sceneFlowService ?? throw new ArgumentNullException(nameof(sceneFlowService));
            this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            theme = new FrontendUiTheme();
            CreateCanvas();
            StartCoroutine(FadeOverlayTo(0f, GetTransitionDuration()));
            if (entryMode == FrontendEntryMode.MainMenu)
            {
                titleCanSkip = false;
                RenderMainMenu();
                SelectFirstInteractable();
                return;
            }

            StartCoroutine(PlayOpeningSequence());
        }

        private IEnumerator PlayOpeningSequence()
        {
            yield return PlayAnimatedLogoStage(0, 0.72f);

            logoStage = 1;
            SetOpeningContentVisual(0f, 0.965f);
            RenderLogoNotice();
            yield return AnimateOpeningContent(0f, 1f, 0.965f, 1f, 0.42f);
            yield return new WaitForSeconds(1.45f);
            openingAtmosphere?.TriggerNoticeCue();
            yield return new WaitForSeconds(0.6f);
            yield return AnimateOpeningContent(1f, 0f, 1f, 1.015f, 0.28f);

            ResetOpeningContentVisual();
            coordinator.CompleteLogoNotice();
            titleCanSkip = true;
            RenderTitleIntro();
            SelectFirstInteractable();
        }

        private IEnumerator PlayAnimatedLogoStage(int stage, float holdDuration)
        {
            logoStage = stage;
            SetOpeningContentVisual(0f, 0.92f);
            RenderLogoNotice();
            yield return AnimateOpeningContent(0f, 1f, 0.92f, 1f, 0.36f);
            yield return new WaitForSeconds(holdDuration);
            yield return AnimateOpeningContent(1f, 0f, 1f, 1.035f, 0.28f);
        }

        private void SetOpeningContentVisual(float alpha, float scale)
        {
            if (contentRoot == null)
            {
                return;
            }

            if (openingContentGroup == null)
            {
                openingContentGroup = contentRoot.GetComponent<CanvasGroup>();
                if (openingContentGroup == null)
                {
                    openingContentGroup = contentRoot.gameObject.AddComponent<CanvasGroup>();
                }
            }

            openingContentGroup.alpha = alpha;
            contentRoot.localScale = Vector3.one * scale;
        }

        private IEnumerator AnimateOpeningContent(float fromAlpha, float toAlpha, float fromScale, float toScale, float duration)
        {
            SetOpeningContentVisual(fromAlpha, fromScale);
            if (duration <= 0.001f)
            {
                SetOpeningContentVisual(toAlpha, toScale);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                SetOpeningContentVisual(Mathf.Lerp(fromAlpha, toAlpha, progress), Mathf.Lerp(fromScale, toScale, progress));
                yield return null;
            }

            SetOpeningContentVisual(toAlpha, toScale);
        }

        private void ResetOpeningContentVisual()
        {
            SetOpeningContentVisual(1f, 1f);
        }

        private void OnDestroy()
        {
            openingAtmosphere?.Dispose();
            openingAtmosphere = null;
        }

        private void Update()
        {
            openingAtmosphere?.Tick(Time.unscaledTime);

            if (titleCanSkip && coordinator != null && coordinator.Screen == FrontendScreen.TitleIntro && WasAnyInputPressed())
            {
                CompleteTitleIntro();
            }

            if (!isInputLocked && coordinator != null && WasCancelPressed())
            {
                Back();
            }
        }

        private void CreateCanvas()
        {
            GameObject cameraObject = new GameObject("FrontendBackgroundCamera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(transform, false);
            Camera backgroundCamera = cameraObject.GetComponent<Camera>();
            backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
            backgroundCamera.backgroundColor = Iron;
            backgroundCamera.cullingMask = 0;
            backgroundCamera.depth = -100f;

            GameObject canvasObject = new GameObject("FrontendCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasScaler = canvasObject.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasScaler.matchWidthOrHeight = 0.5f;
            graphicRaycaster = canvasObject.GetComponent<GraphicRaycaster>();

            GameObject eventSystemObject = new GameObject("FrontendEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.transform.SetParent(transform, false);
            eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            eventSystem = eventSystemObject.GetComponent<EventSystem>();

            GameObject root = new GameObject("Content", typeof(RectTransform));
            root.transform.SetParent(canvasObject.transform, false);
            contentRoot = root.GetComponent<RectTransform>();
            contentRoot.anchorMin = Vector2.zero;
            contentRoot.anchorMax = Vector2.one;
            contentRoot.offsetMin = Vector2.zero;
            contentRoot.offsetMax = Vector2.zero;
            CreateFadeOverlay(canvasObject.transform);
            CreateBackgroundMusic();
            CreateUiSoundPlayer();
            ApplyPresentationSettings(settingsService.Persisted);
        }

        private void CreateBackgroundMusic()
        {
            AudioClip music = Resources.Load<AudioClip>(FrontendMusicPath);
            if (music == null)
            {
                Debug.LogWarning("Frontend background music was not found in Resources.", this);
                return;
            }

            GameObject musicObject = new GameObject("FrontendBackgroundMusic", typeof(AudioSource));
            musicObject.transform.SetParent(transform, false);
            backgroundMusicSource = musicObject.GetComponent<AudioSource>();
            backgroundMusicSource.clip = music;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.playOnAwake = false;
            backgroundMusicSource.spatialBlend = 0f;
            backgroundMusicSource.volume = 0.32f;
            backgroundMusicSource.Play();
        }

        private void CreateFadeOverlay(Transform parent)
        {
            GameObject overlayObject = new GameObject("FadeOverlay", typeof(Image));
            overlayObject.transform.SetParent(parent, false);
            RectTransform rect = overlayObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            fadeOverlay = overlayObject.GetComponent<Image>();
            fadeOverlay.color = new Color(0f, 0f, 0f, 1f);
            fadeOverlay.raycastTarget = true;
        }

        private void CreateUiSoundPlayer()
        {
            GameObject soundObject = new GameObject("FrontendUiSounds", typeof(AudioSource));
            soundObject.transform.SetParent(transform, false);
            uiSoundSource = soundObject.GetComponent<AudioSource>();
            uiSoundSource.playOnAwake = false;
            uiSoundSource.spatialBlend = 0f;
            uiSoundSource.volume = 0.58f;
            focusSound = Resources.Load<AudioClip>(FocusSoundPath);
            selectSound = Resources.Load<AudioClip>(SelectSoundPath);
            cancelSound = Resources.Load<AudioClip>(CancelSoundPath);
            disabledSound = Resources.Load<AudioClip>(DisabledSoundPath);
            errorSound = Resources.Load<AudioClip>(ErrorSoundPath);
            saveCompleteSound = Resources.Load<AudioClip>(SaveCompleteSoundPath);
        }

        private void PlayUiSound(AudioClip sound)
        {
            if (uiSoundSource != null && sound != null)
            {
                uiSoundSource.PlayOneShot(sound);
            }
        }

        private void TransitionTo(System.Action stateChange)
        {
            if (isInputLocked || stateChange == null)
            {
                return;
            }

            SetInputLocked(true);
            StartCoroutine(TransitionRoutine(stateChange));
        }

        private IEnumerator TransitionRoutine(System.Action stateChange)
        {
            yield return FadeOverlayTo(1f, GetTransitionDuration());
            stateChange();
            Render();
            yield return FadeOverlayTo(0f, GetTransitionDuration());
            SetInputLocked(false);
            SelectFirstInteractable();
        }

        private IEnumerator FadeOverlayTo(float targetAlpha, float duration)
        {
            if (fadeOverlay == null)
            {
                yield break;
            }

            Color color = fadeOverlay.color;
            float startAlpha = color.a;
            if (duration <= 0.001f)
            {
                color.a = targetAlpha;
                fadeOverlay.color = color;
                fadeOverlay.raycastTarget = targetAlpha > 0.001f || isInputLocked;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                color.a = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
                fadeOverlay.color = color;
                yield return null;
            }

            color.a = targetAlpha;
            fadeOverlay.color = color;
            fadeOverlay.raycastTarget = targetAlpha > 0.001f || isInputLocked;
        }

        private void SetInputLocked(bool locked)
        {
            isInputLocked = locked;
            if (graphicRaycaster != null)
            {
                graphicRaycaster.enabled = !locked;
            }

            if (fadeOverlay != null)
            {
                fadeOverlay.raycastTarget = locked || fadeOverlay.color.a > 0.001f;
            }
        }

        private float GetTransitionDuration()
        {
            return settingsService != null && settingsService.Working.ReduceTransitions ? 0.01f : 0.22f;
        }

        private void Render()
        {
            ClearContent();
            switch (coordinator.Screen)
            {
                case FrontendScreen.LogoNotice:
                    RenderLogoNotice();
                    break;
                case FrontendScreen.TitleIntro:
                    RenderTitleIntro();
                    break;
                case FrontendScreen.MainMenu:
                    RenderMainMenu();
                    break;
                case FrontendScreen.SaveSlotsLoad:
                case FrontendScreen.SaveSlotsNew:
                    RenderSaveSlots();
                    break;
                case FrontendScreen.NewGameSetup:
                    RenderNewGameSetup();
                    break;
                case FrontendScreen.ConfirmOverwrite:
                    RenderOverwriteConfirm();
                    break;
                case FrontendScreen.Settings:
                    RenderSettings();
                    break;
                case FrontendScreen.ArchiveLocked:
                    RenderArchiveLocked();
                    break;
                case FrontendScreen.ConfirmExit:
                    RenderExitConfirm();
                    break;
                case FrontendScreen.ErrorDialog:
                    RenderError();
                    break;
                default:
                    RenderBusy();
                    break;
            }

            SelectFirstInteractable();
        }

        private void SelectFirstInteractable()
        {
            if (eventSystem == null || contentRoot == null || isInputLocked)
            {
                return;
            }

            Button[] buttons = contentRoot.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (!button.interactable || !button.gameObject.activeInHierarchy)
                {
                    continue;
                }

                lastFocusedButton = null;
                eventSystem.SetSelectedGameObject(button.gameObject);
                return;
            }

            eventSystem.SetSelectedGameObject(null);
        }

        private void RenderLogoNotice()
        {
            ClearContent();
            openingAtmosphere = new BootAtmosphere(contentRoot, logoStage == 1);
            if (logoStage == 0)
            {
                AddRawImage(DeveloperLogoPath, new Vector2(960f, 480f), new Vector2(440f, 440f), Color.white);
                openingAtmosphere.TriggerLogoPulse();
                return;
            }

            AddFramedPanel(NoticeWindowFramePath, new Vector2(960f, 565f), new Vector2(960f, 340f), new Color(0f, 0f, 0f, 0.48f));
            AddLabel("플레이 전 경고 사항", new Vector2(960f, 350f), 28, Paper, TextAnchor.MiddleCenter);
            AddLabel("본 게임에는 섬광, 화면 흔들림, 갑작스러운 음향 및\n빠른 카메라 이동이 포함되어 있습니다.\n\n불편함이 느껴질 경우 플레이를 중단하거나,\n환경 조정의 접근성 항목에서 효과 강도를 낮춰 주십시오.\n\n저장 아이콘이 표시되는 동안 게임을 종료하지 마십시오.", new Vector2(960f, 565f), 27, Paper, TextAnchor.MiddleCenter);
        }

        private void RenderTitleIntro()
        {
            ClearContent();
            AddFullscreenRawImage(TitleBackgroundPath, Color.white);
            AddFullscreenPanel(new Color(0f, 0f, 0f, 0.22f));
            AddRawImage(TitleLogoPath, new Vector2(960f, 545f), new Vector2(1180f, 664f), Color.white);
            AddButton("아무 버튼이나 누르십시오", new Vector2(960f, 880f), new Vector2(430f, 64f), CompleteTitleIntro, true, 18f, UiButtonSound.Select, true, TitlePromptFramePath);
        }

        private void RenderMainMenu()
        {
            coordinator.RefreshMainMenuSlots();
            bool canContinue = coordinator.HasContinueSlot;
            AddFullscreenRawImage(TitleBackgroundPath, Color.white);
            AddPanel(new Vector2(390f, 540f), new Vector2(780f, 1080f), new Color(Iron.r, Iron.g, Iron.b, 0.83f));
            AddRawImage(TitleLogoPath, new Vector2(390f, 175f), new Vector2(600f, 338f), Color.white);
            AddMenuButton("01  최근 기록 이어보기", "CONTINUE", ContinueIconPath, 405f, BeginContinue, canContinue);
            AddMenuButton("02  신규 파견 시작", "NEW GAME", NewGameIconPath, 525f, BeginNewGame);
            AddMenuButton("03  보존 기록 열람", "LOAD GAME", LoadGameIconPath, 645f, BeginLoadGame);
            AddMenuButton("04  환경 조정", "SETTINGS", SettingsIconPath, 765f, BeginSettings);
            AddMenuButton("05  기록 보관소", "ARCHIVE · LOCKED", ArchiveIconPath, 885f, BeginArchiveLocked);
            AddMenuButton("06  업무 종료", "EXIT", ExitIconPath, 1005f, BeginExitConfirmation);
        }

        private void RenderSaveSlots()
        {
            AddBackground(Iron);
            bool isLoad = coordinator.Screen == FrontendScreen.SaveSlotsLoad;
            AddLabel(isLoad ? "보존 기록 열람" : "신규 파견 기록 선택", new Vector2(960f, 130f), 38, Paper, TextAnchor.MiddleCenter);
            AddLabel(isLoad ? "열람할 보존 기록을 선택하십시오." : "신규 파견을 등록할 기록을 선택하십시오.", new Vector2(960f, 185f), 22, Focus, TextAnchor.MiddleCenter);
            float y = 350f;
            foreach (SaveSlotSummary slot in coordinator.Slots)
            {
                string body = BuildSlotText(slot, isLoad);
                bool interactable = !isLoad || slot.CanLoad;
                GameObject button = AddButton(body, new Vector2(960f, y), new Vector2(980f, 150f), () => SelectSlot(slot.SlotId), interactable, 178f, UiButtonSound.Select, true, SaveSlotCardPath, 78f);
                AddSlotPlaceholder(button.transform, slot);
                AddSlotBadge(button.transform, slot);
                y += 180f;
            }
            AddButton("뒤로", new Vector2(220f, 960f), new Vector2(220f, 56f), Back, true, 18f, UiButtonSound.Cancel);
        }

        private void RenderNewGameSetup()
        {
            AddBackground(Iron);
            AddLabel("신규 파견 등록", new Vector2(960f, 115f), 40, Paper, TextAnchor.MiddleCenter);
            AddLabel("난이도 선택", new Vector2(510f, 250f), 30, Paper, TextAnchor.MiddleCenter);
            AddDifficultyButton("기록 열람  STORY", DifficultyId.StoryValue, StoryDifficultyIconPath, 360f);
            AddDifficultyButton("정규 파견  NORMAL", DifficultyId.NormalValue, NormalDifficultyIconPath, 460f);
            AddDifficultyButton("특별 압류  HARD", DifficultyId.HardValue, HardDifficultyIconPath, 560f);
            AddLabel("튜토리얼", new Vector2(1370f, 250f), 30, Paper, TextAnchor.MiddleCenter);
            AddTutorialButton("상세 안내", 0, 360f);
            AddTutorialButton("핵심 안내", 1, 460f);
            AddTutorialButton("사용 안 함", 2, 560f);
            AddButton("파견 승인", new Vector2(1240f, 850f), new Vector2(300f, 64f), CreateNewGame, true);
            AddButton("뒤로", new Vector2(680f, 850f), new Vector2(300f, 64f), Back, true, 18f, UiButtonSound.Cancel);
        }

        private void RenderSettings()
        {
            AddFullscreenRawImage(TitleBackgroundPath, new Color(0.42f, 0.42f, 0.48f, 1f));
            AddFramedPanel(SettingsWindowFramePath, new Vector2(960f, 540f), new Vector2(1520f, 900f), new Color(Iron.r, Iron.g, Iron.b, 0.92f));
            AddLabel("환경 조정", new Vector2(960f, 125f), 42, theme.Paper, TextAnchor.MiddleCenter);
            AddLabel("변경사항은 적용을 눌러야 저장됩니다.", new Vector2(960f, 176f), 21, theme.Focus, TextAnchor.MiddleCenter);

            AddButton("음향", new Vector2(570f, 250f), new Vector2(230f, 58f), () => SetSettingsPage(0), settingsPage != 0, 18f, UiButtonSound.Select, true, SettingsTabPath);
            AddButton("화면", new Vector2(840f, 250f), new Vector2(230f, 58f), () => SetSettingsPage(1), settingsPage != 1, 18f, UiButtonSound.Select, true, SettingsTabPath);
            AddButton("접근성", new Vector2(1110f, 250f), new Vector2(230f, 58f), () => SetSettingsPage(2), settingsPage != 2, 18f, UiButtonSound.Select, true, SettingsTabPath);

            if (settingsPage == 0)
            {
                RenderAudioSettings();
            }
            else if (settingsPage == 1)
            {
                RenderDisplaySettings();
            }
            else
            {
                RenderAccessibilitySettings();
            }

            if (!string.IsNullOrWhiteSpace(settingsErrorMessage))
            {
                AddLabel(settingsErrorMessage, new Vector2(960f, 760f), 20, theme.Warning, TextAnchor.MiddleCenter);
            }

            AddButton("기본값", new Vector2(540f, 900f), new Vector2(220f, 62f), ResetSettings, true);
            AddButton("취소", new Vector2(820f, 900f), new Vector2(220f, 62f), CancelSettings, true, 18f, UiButtonSound.Cancel);
            AddButton("적용", new Vector2(1100f, 900f), new Vector2(220f, 62f), SaveSettings, true);
        }

        private void RenderAudioSettings()
        {
            AddLabel("음향", new Vector2(960f, 340f), 30, theme.Paper, TextAnchor.MiddleCenter);
            AddSettingsAdjustRow("전체 음량", FormatPercent(settingsDraft.MasterVolume), 440f,
                () => ChangeSettings(settingsDraft.With(masterVolume: settingsDraft.MasterVolume - 0.1f)),
                () => ChangeSettings(settingsDraft.With(masterVolume: settingsDraft.MasterVolume + 0.1f)));
            AddSettingsAdjustRow("배경 음악", FormatPercent(settingsDraft.BgmVolume), 555f,
                () => ChangeSettings(settingsDraft.With(bgmVolume: settingsDraft.BgmVolume - 0.1f)),
                () => ChangeSettings(settingsDraft.With(bgmVolume: settingsDraft.BgmVolume + 0.1f)));
            AddSettingsAdjustRow("효과음", FormatPercent(settingsDraft.SfxVolume), 670f,
                () => ChangeSettings(settingsDraft.With(sfxVolume: settingsDraft.SfxVolume - 0.1f)),
                () => ChangeSettings(settingsDraft.With(sfxVolume: settingsDraft.SfxVolume + 0.1f)));
        }

        private void RenderDisplaySettings()
        {
            AddLabel("화면", new Vector2(960f, 340f), 30, theme.Paper, TextAnchor.MiddleCenter);
            AddSettingsCycleRow("화면 모드", DisplayModeText(settingsDraft.DisplayMode), 420f, CycleDisplayMode);
            AddSettingsCycleRow("해상도", settingsDraft.ResolutionWidth + " × " + settingsDraft.ResolutionHeight, 535f, CycleResolution);
            AddSettingsCycleRow("수직 동기화", settingsDraft.VSyncEnabled ? "사용" : "사용 안 함", 650f,
                () => ChangeSettings(settingsDraft.With(vSyncEnabled: !settingsDraft.VSyncEnabled)));
            AddSettingsCycleRow("품질", QualityText(settingsDraft.QualityPreset), 735f, CycleQuality);
        }

        private void RenderAccessibilitySettings()
        {
            AddLabel("접근성", new Vector2(960f, 340f), 30, theme.Paper, TextAnchor.MiddleCenter);
            AddSettingsCycleRow("글자 크기", FormatPercent(settingsDraft.UiScale), 420f, CycleUiScale);
            AddSettingsCycleRow("화면 흔들림 감소", settingsDraft.ReduceScreenShake ? "사용" : "사용 안 함", 535f,
                () => ChangeSettings(settingsDraft.With(reduceScreenShake: !settingsDraft.ReduceScreenShake)));
            AddSettingsCycleRow("섬광 감소", settingsDraft.ReduceFlashes ? "사용" : "사용 안 함", 650f,
                () => ChangeSettings(settingsDraft.With(reduceFlashes: !settingsDraft.ReduceFlashes)));
            AddSettingsCycleRow("전환 애니메이션 감소", settingsDraft.ReduceTransitions ? "사용" : "사용 안 함", 735f,
                () => ChangeSettings(settingsDraft.With(reduceTransitions: !settingsDraft.ReduceTransitions)));
        }

        private void RenderArchiveLocked()
        {
            AddBackground(Iron);
            AddRawImage(LockedBadgePath, new Vector2(960f, 325f), new Vector2(48f, 48f), Color.white);
            AddLabel("기록 보관소", new Vector2(960f, 420f), 42, theme.Paper, TextAnchor.MiddleCenter);
            AddLabel("아직 열람 권한이 부여되지 않았습니다.\n추후 업데이트에서 개방됩니다.", new Vector2(960f, 535f), 27, theme.Focus, TextAnchor.MiddleCenter);
            AddButton("돌아가기", new Vector2(960f, 700f), new Vector2(280f, 62f), Back, true, 18f, UiButtonSound.Cancel);
        }

        private void RenderExitConfirm()
        {
            AddBackground(Iron);
            AddFramedPanel(ModalFramePath, new Vector2(960f, 520f), new Vector2(960f, 460f), new Color(0f, 0f, 0f, 0.6f));
            AddLabel("업무 종료", new Vector2(960f, 400f), 42, theme.Warning, TextAnchor.MiddleCenter);
            AddLabel("게임을 종료하시겠습니까?\n저장 중인 기록이 있다면 완료된 뒤 종료하십시오.", new Vector2(960f, 520f), 26, theme.Paper, TextAnchor.MiddleCenter);
            AddButton("종료", new Vector2(760f, 680f), new Vector2(240f, 62f), ConfirmExit, true);
            AddButton("취소", new Vector2(1160f, 680f), new Vector2(240f, 62f), CancelExitConfirmation, true, 18f, UiButtonSound.Cancel);
        }

        private void AddSettingsAdjustRow(string label, string value, float y, UnityEngine.Events.UnityAction decrease, UnityEngine.Events.UnityAction increase)
        {
            AddLabel(label, new Vector2(600f, y), 27, theme.Paper, TextAnchor.MiddleLeft, new Vector2(420f, 58f));
            AddLabel(value, new Vector2(960f, y), 27, theme.Focus, TextAnchor.MiddleCenter);
            AddButton("−", new Vector2(1190f, y), new Vector2(72f, 58f), decrease, true, 18f, UiButtonSound.Select, true, SettingsStepperButtonPath);
            AddButton("+", new Vector2(1280f, y), new Vector2(72f, 58f), increase, true, 18f, UiButtonSound.Select, true, SettingsStepperButtonPath);
        }

        private void AddSettingsCycleRow(string label, string value, float y, UnityEngine.Events.UnityAction action)
        {
            AddLabel(label, new Vector2(600f, y), 27, theme.Paper, TextAnchor.MiddleLeft, new Vector2(420f, 62f));
            AddButton(value, new Vector2(1040f, y), new Vector2(460f, 62f), action, true, 28f, UiButtonSound.Select, true, SettingsValueSelectorPath);
        }

        private void RenderOverwriteConfirm()
        {
            AddBackground(Iron);
            AddFramedPanel(ModalFramePath, new Vector2(960f, 520f), new Vector2(960f, 460f), new Color(0f, 0f, 0f, 0.55f));
            AddLabel("기존 기록 덮어쓰기", new Vector2(960f, 355f), 40, Warning, TextAnchor.MiddleCenter);
            AddLabel("이 슬롯에는 기존 보존 기록이 있습니다.\n\n신규 파견을 시작하면 해당 기록을 덮어쓰며,\n덮어쓴 기록은 복구할 수 없습니다.\n\n정말 신규 파견을 시작하시겠습니까?", new Vector2(960f, 535f), 27, Paper, TextAnchor.MiddleCenter);
            AddButton("기존 기록 덮어쓰기", new Vector2(700f, 760f), new Vector2(360f, 62f), () => ConfirmOverwrite(true), true);
            AddButton("취소", new Vector2(1220f, 760f), new Vector2(220f, 62f), () => ConfirmOverwrite(false), true, 18f, UiButtonSound.Cancel);
        }

        private void RenderError()
        {
            AddBackground(Iron);
            AddLabel("기록 처리 오류", new Vector2(960f, 420f), 42, Warning, TextAnchor.MiddleCenter);
            AddLabel("처리할 수 없는 기록입니다.\n오류 코드: " + coordinator.ErrorCode, new Vector2(960f, 530f), 27, Paper, TextAnchor.MiddleCenter);
            AddButton("돌아가기", new Vector2(960f, 680f), new Vector2(280f, 62f), Back, true, 18f, UiButtonSound.Cancel);
        }

        private void RenderBusy()
        {
            AddBackground(Iron);
            RawImage seal = AddRawImage(LoadingSealPath, new Vector2(960f, 440f), new Vector2(128f, 128f), Color.white);
            if (seal != null)
            {
                StartCoroutine(SpinWhileVisible(seal.rectTransform));
            }
            AddLabel("공식 기록을 처리하고 있습니다…", new Vector2(960f, 540f), 34, Paper, TextAnchor.MiddleCenter);
        }

        private void BeginContinue()
        {
            HandleCommand(coordinator.ContinueLatest());
        }

        private void BeginNewGame()
        {
            TransitionTo(() =>
            {
                coordinator.OpenStartMode();
                coordinator.OpenNewGameSlots();
            });
        }

        private void BeginLoadGame()
        {
            TransitionTo(() =>
            {
                coordinator.OpenStartMode();
                coordinator.OpenContinueSlots();
            });
        }

        private void BeginSettings()
        {
            TransitionTo(() =>
            {
                if (!coordinator.OpenSettings())
                {
                    return;
                }

                settingsDraft = settingsService.BeginEdit();
                settingsPage = 0;
                settingsErrorMessage = settingsService.LoadNoticeCode == null
                    ? null
                    : "설정 파일을 안전한 값으로 복구했습니다.";
                ApplyPresentationSettings(settingsDraft);
            });
        }

        private void BeginArchiveLocked()
        {
            TransitionTo(() => coordinator.OpenArchiveLocked());
        }

        private void BeginExitConfirmation()
        {
            TransitionTo(() => coordinator.OpenExitConfirmation());
        }

        private void SetSettingsPage(int page)
        {
            settingsPage = page;
            Render();
        }

        private void ChangeSettings(GameSettings updatedSettings)
        {
            settingsErrorMessage = null;
            settingsService.SetWorking(updatedSettings);
            settingsDraft = settingsService.Working;
            ApplyPresentationSettings(settingsDraft);
            Render();
        }

        private void ResetSettings()
        {
            settingsErrorMessage = null;
            settingsService.ResetWorking();
            settingsDraft = settingsService.Working;
            ApplyPresentationSettings(settingsDraft);
            Render();
        }

        private void SaveSettings()
        {
            SettingsWriteResult result = settingsService.SaveWorking();
            if (!result.IsSuccess)
            {
                settingsErrorMessage = "설정을 저장하지 못했습니다. 다시 시도하십시오.";
                PlayUiSound(errorSound);
                Debug.LogWarning("Settings save failed: " + result.ErrorCode, this);
                Render();
                return;
            }

            settingsErrorMessage = null;
            TransitionTo(() => coordinator.Back());
        }

        private void CancelSettings()
        {
            settingsService.CancelEdit();
            settingsDraft = settingsService.Persisted;
            ApplyPresentationSettings(settingsDraft);
            TransitionTo(() => coordinator.Back());
        }

        private void CycleDisplayMode()
        {
            DisplayModeId mode = settingsDraft.DisplayMode == DisplayModeId.FullScreenWindow
                ? DisplayModeId.Windowed
                : settingsDraft.DisplayMode == DisplayModeId.Windowed
                    ? DisplayModeId.ExclusiveFullScreen
                    : DisplayModeId.FullScreenWindow;
            ChangeSettings(settingsDraft.With(displayMode: mode));
        }

        private void CycleResolution()
        {
            if (settingsDraft.ResolutionWidth == 1280)
            {
                ChangeSettings(settingsDraft.With(resolutionWidth: 1920, resolutionHeight: 1080));
            }
            else if (settingsDraft.ResolutionWidth == 1920)
            {
                ChangeSettings(settingsDraft.With(resolutionWidth: 2560, resolutionHeight: 1440));
            }
            else if (settingsDraft.ResolutionWidth == 2560)
            {
                ChangeSettings(settingsDraft.With(resolutionWidth: 3440, resolutionHeight: 1440));
            }
            else
            {
                ChangeSettings(settingsDraft.With(resolutionWidth: 1280, resolutionHeight: 720));
            }
        }

        private void CycleQuality()
        {
            QualityPresetId quality = settingsDraft.QualityPreset == QualityPresetId.Low
                ? QualityPresetId.Medium
                : settingsDraft.QualityPreset == QualityPresetId.Medium
                    ? QualityPresetId.High
                    : QualityPresetId.Low;
            ChangeSettings(settingsDraft.With(qualityPreset: quality));
        }

        private void CycleUiScale()
        {
            float scale = settingsDraft.UiScale < 0.95f
                ? 1f
                : settingsDraft.UiScale < 1.05f ? 1.1f : 0.9f;
            ChangeSettings(settingsDraft.With(uiScale: scale));
        }

        private void ConfirmExit()
        {
            if (!coordinator.ConfirmExit(true))
            {
                return;
            }

            ExitApplication();
        }

        private void CancelExitConfirmation()
        {
            if (coordinator == null || !coordinator.ConfirmExit(false))
            {
                return;
            }

            Render();
            SelectFirstInteractable();
        }

        private void SelectSlot(SaveSlotId slotId)
        {
            HandleCommand(coordinator.SelectSlot(slotId));
        }

        private void ConfirmOverwrite(bool confirmed)
        {
            coordinator.ConfirmOverwrite(confirmed);
            Render();
        }

        private void CreateNewGame()
        {
            string tutorialMode = tutorialSelection == 0
                ? TutorialMode.DetailValue
                : tutorialSelection == 1 ? TutorialMode.CoreValue : TutorialMode.OffValue;
            if (!NewGameSettings.TryCreate("세무관", selectedDifficultyId, tutorialMode, out NewGameSettings settings, out string errorCode))
            {
                PlayUiSound(errorSound);
                return;
            }

            FrontendCommandResult result = coordinator.CreateSelectedNewGame(settings, UnityEngine.Application.version);
            if (result.HasEntryDestination)
            {
                PlayUiSound(saveCompleteSound);
            }

            HandleCommand(result);
        }

        private void CompleteTitleIntro()
        {
            TransitionTo(() =>
            {
                titleCanSkip = false;
                coordinator.CompleteTitleIntro();
            });
        }

        private void Back()
        {
            if (coordinator == null
                || coordinator.Screen == FrontendScreen.LogoNotice
                || coordinator.Screen == FrontendScreen.TitleIntro
                || coordinator.Screen == FrontendScreen.MainMenu
                || coordinator.Screen == FrontendScreen.Busy)
            {
                return;
            }

            if (coordinator.Screen == FrontendScreen.Settings)
            {
                CancelSettings();
                return;
            }

            if (coordinator.Screen == FrontendScreen.ConfirmExit)
            {
                CancelExitConfirmation();
                return;
            }

            TransitionTo(() => coordinator.Back());
        }

        private void HandleCommand(FrontendCommandResult result)
        {
            if (result.HasEntryDestination)
            {
                StartCoroutine(LoadEntry(result.Destination));
                return;
            }

            if (coordinator.Screen == FrontendScreen.ErrorDialog)
            {
                PlayUiSound(errorSound);
            }

            Render();
        }

        private IEnumerator LoadEntry(EntryDestination destination)
        {
            if (isLoadingEntry)
            {
                yield break;
            }

            isLoadingEntry = true;
            SetInputLocked(true);
            yield return FadeOverlayTo(1f, GetTransitionDuration());
            yield return FadeAudio(backgroundMusicSource, 0f, GetTransitionDuration());
            RenderBusy();
            Task task = sceneFlowService.LoadEntryAsync(destination);
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                Debug.LogException(task.Exception, this);
                coordinator.HandleSceneLoadFailure("scene_load_failed");
                Render();
                yield return FadeAudio(backgroundMusicSource, GetBgmVolume(), GetTransitionDuration());
                yield return FadeOverlayTo(0f, GetTransitionDuration());
                SetInputLocked(false);
                SelectFirstInteractable();
            }

            isLoadingEntry = false;
        }

        private IEnumerator FadeAudio(AudioSource source, float targetVolume, float duration)
        {
            if (source == null)
            {
                yield break;
            }

            float startVolume = source.volume;
            if (duration <= 0.001f)
            {
                source.volume = targetVolume;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            source.volume = targetVolume;
        }

        private static IEnumerator SpinWhileVisible(RectTransform target)
        {
            while (target != null)
            {
                target.Rotate(0f, 0f, -72f * Time.unscaledDeltaTime);
                yield return null;
            }
        }

        private void AddDifficultyButton(string label, string difficultyId, string iconResourcePath, float y)
        {
            string displayLabel = (selectedDifficultyId == difficultyId ? "▶ " : "   ") + label;
            GameObject button = AddButton(displayLabel, new Vector2(510f, y), new Vector2(500f, 84f), () => { selectedDifficultyId = difficultyId; Render(); }, true, 108f, UiButtonSound.Select, true, SelectionCardWidePath);
            AddButtonIcon(button.transform, iconResourcePath, new Vector2(78f, 78f), new Vector2(48f, 0f));
        }

        private void AddTutorialButton(string label, int value, float y)
        {
            string displayLabel = (tutorialSelection == value ? "▶ " : "   ") + label;
            AddButton(displayLabel, new Vector2(1370f, y), new Vector2(380f, 72f), () => { tutorialSelection = value; Render(); }, true, 18f, UiButtonSound.Select, true, SelectionCardMediumPath);
        }

        private void AddMenuButton(
            string koreanLabel,
            string englishLabel,
            string iconResourcePath,
            float y,
            UnityEngine.Events.UnityAction action,
            bool interactable = true)
        {
            GameObject button = AddButton(string.Empty, new Vector2(540f, y), new Vector2(620f, 118f), action, interactable, 132f, UiButtonSound.Select, false, null);
            button.GetComponent<Image>().color = Color.clear;
            Image panel = AddMenuPanel(button.transform);
            if (panel != null)
            {
                button.GetComponent<Button>().targetGraphic = panel;
                panel.color = interactable ? Color.white : new Color(0.45f, 0.45f, 0.45f, 0.72f);
            }

            AddButtonIcon(button.transform, iconResourcePath, new Vector2(78f, 78f), new Vector2(70f, 0f));
            AddMenuText(button.transform, koreanLabel, englishLabel, interactable);
        }

        private void AddMenuText(Transform parent, string koreanLabel, string englishLabel, bool interactable)
        {
            Color primary = interactable ? theme.MenuPrimary : theme.Disabled;
            Color secondary = interactable ? theme.MenuSecondary : theme.Disabled;
            AddMenuTextElement(parent, "Primary", koreanLabel, new Vector2(126f, 0f), new Vector2(300f, 92f), primary, FrontendTypography.MenuPrimary);
            AddMenuTextElement(parent, "Secondary", englishLabel, new Vector2(434f, 0f), new Vector2(154f, 78f), secondary, FrontendTypography.MenuSecondary);
        }

        private void AddMenuTextElement(
            Transform parent,
            string name,
            string value,
            Vector2 position,
            Vector2 size,
            Color color,
            FrontendTypography typography)
        {
            GameObject label = new GameObject(name, typeof(Text));
            label.transform.SetParent(parent, false);
            RectTransform rect = label.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text text = label.GetComponent<Text>();
            text.text = value;
            theme.Apply(text, typography, color, TextAnchor.MiddleLeft);
            text.fontSize = Mathf.RoundToInt(text.fontSize * uiTextScale);
            text.raycastTarget = false;
            theme.AddContrast(text, true);
        }

        private void ExitApplication()
        {
#if UNITY_EDITOR
            Debug.Log("Exit was requested. Application.Quit is skipped in the Unity Editor.", this);
#else
            UnityEngine.Application.Quit();
#endif
        }

        private void ApplyPresentationSettings(GameSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            uiTextScale = settings.UiScale;
            if (backgroundMusicSource != null)
            {
                backgroundMusicSource.volume = GetBgmVolume();
            }

            if (uiSoundSource != null)
            {
                uiSoundSource.volume = 0.58f * settings.MasterVolume * settings.SfxVolume;
            }

            if (canvasScaler != null)
            {
                canvasScaler.matchWidthOrHeight = Screen.width > Screen.height * 2f ? 1f : 0.5f;
            }
        }

        private static string FormatPercent(float value)
        {
            return Mathf.RoundToInt(value * 100f) + "%";
        }

        private float GetBgmVolume()
        {
            GameSettings settings = settingsService == null ? null : settingsService.Working;
            return settings == null ? 0.32f : 0.32f * settings.MasterVolume * settings.BgmVolume;
        }

        private static string DisplayModeText(DisplayModeId displayMode)
        {
            switch (displayMode)
            {
                case DisplayModeId.Windowed:
                    return "창 모드";
                case DisplayModeId.ExclusiveFullScreen:
                    return "전체 화면";
                default:
                    return "테두리 없는 전체 화면";
            }
        }

        private static string QualityText(QualityPresetId qualityPreset)
        {
            switch (qualityPreset)
            {
                case QualityPresetId.Low:
                    return "낮음";
                case QualityPresetId.Medium:
                    return "보통";
                default:
                    return "높음";
            }
        }

        private string BuildSlotText(SaveSlotSummary slot, bool isLoad)
        {
            if (slot.State == SaveSlotState.Empty)
            {
                return slot.SlotId.Value.ToUpperInvariant() + "\n빈 기록  |  이 슬롯에서 신규 파견을 시작할 수 있습니다.";
            }

            if (slot.State == SaveSlotState.Valid)
            {
                string recovered = slot.RecoveredFromBackup ? "  |  백업 복구됨" : string.Empty;
                return slot.SlotId.Value.ToUpperInvariant() + "  |  " + GetEntryDisplayName(slot.EntryId, slot.CheckpointId)
                    + "\n" + slot.ProfileName + "  |  " + DifficultyDisplayName(slot.DifficultyId)
                    + "  |  " + FormatPlayTime(slot.PlayTimeSeconds)
                    + "  |  " + slot.UpdatedAtUtc.Value.ToLocalTime().ToString("yyyy.MM.dd HH:mm") + recovered;
            }

            string stateLabel = slot.State == SaveSlotState.Corrupt ? "손상된 기록" : "호환되지 않는 기록";
            return slot.SlotId.Value.ToUpperInvariant() + "\n" + stateLabel + (isLoad ? "  |  열람 불가" : "  |  덮어쓰기 후 신규 파견 가능");
        }

        private void ClearContent()
        {
            if (contentRoot == null)
            {
                return;
            }

            openingAtmosphere?.Dispose();
            openingAtmosphere = null;

            for (int index = contentRoot.childCount - 1; index >= 0; index--)
            {
                Destroy(contentRoot.GetChild(index).gameObject);
            }
        }

        private void AddBackground(Color color)
        {
            GameObject background = new GameObject("Background", typeof(Image));
            background.transform.SetParent(contentRoot, false);
            RectTransform rect = background.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = color;
        }

        private void AddFullscreenPanel(Color color)
        {
            GameObject panel = new GameObject("FullscreenPanel", typeof(Image));
            panel.transform.SetParent(contentRoot, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = color;
            panel.GetComponent<Image>().raycastTarget = false;
        }

        private void AddPanel(Vector2 position, Vector2 size, Color color)
        {
            GameObject panel = new GameObject("Panel", typeof(Image));
            panel.transform.SetParent(contentRoot, false);
            ConfigureRect(panel.GetComponent<RectTransform>(), position, size);
            panel.GetComponent<Image>().color = color;
        }

        private void AddFramedPanel(string resourcePath, Vector2 position, Vector2 size, Color underlayColor)
        {
            AddPanel(position, size, underlayColor);
            AddRawImage(resourcePath, position, size, Color.white);
        }

        private RawImage AddRawImage(string resourcePath, Vector2 position, Vector2 size, Color color)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            GameObject imageObject = new GameObject("RawImage", typeof(RawImage));
            imageObject.transform.SetParent(contentRoot, false);
            ConfigureRect(imageObject.GetComponent<RectTransform>(), position, size);
            RawImage image = imageObject.GetComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private void AddFullscreenRawImage(string resourcePath, Color color)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                AddBackground(Iron);
                return;
            }

            GameObject imageObject = new GameObject("FullscreenRawImage", typeof(RawImage));
            imageObject.transform.SetParent(contentRoot, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            RawImage image = imageObject.GetComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            image.uvRect = CalculateCoverUv(texture.width, texture.height);
            image.raycastTarget = false;
        }

        private static Rect CalculateCoverUv(int textureWidth, int textureHeight)
        {
            if (textureWidth <= 0 || textureHeight <= 0 || Screen.width <= 0 || Screen.height <= 0)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            float textureAspect = textureWidth / (float)textureHeight;
            float viewportAspect = Screen.width / (float)Screen.height;
            if (viewportAspect > textureAspect)
            {
                float visibleHeight = textureAspect / viewportAspect;
                return new Rect(0f, (1f - visibleHeight) * 0.5f, 1f, visibleHeight);
            }

            float visibleWidth = viewportAspect / textureAspect;
            return new Rect((1f - visibleWidth) * 0.5f, 0f, visibleWidth, 1f);
        }

        private void AddLabel(string value, Vector2 position, int fontSize, Color color, TextAnchor alignment, Vector2? size = null)
        {
            GameObject label = new GameObject("Label", typeof(Text));
            label.transform.SetParent(contentRoot, false);
            ConfigureRect(label.GetComponent<RectTransform>(), position, size ?? new Vector2(1500f, 180f));
            Text text = label.GetComponent<Text>();
            text.text = value;
            FrontendTypography typography = fontSize >= 38
                ? FrontendTypography.Title
                : fontSize >= 28 ? FrontendTypography.Subtitle : FrontendTypography.Body;
            theme.Apply(text, typography, color, alignment);
            text.fontSize = Mathf.RoundToInt(fontSize * uiTextScale);
            theme.AddContrast(text, fontSize >= 24);
        }

        private GameObject AddButton(
            string value,
            Vector2 position,
            Vector2 size,
            UnityEngine.Events.UnityAction action,
            bool interactable,
            float labelLeftPadding = 18f,
            UiButtonSound clickSound = UiButtonSound.Select,
            bool addLabel = true,
            string frameResourcePath = StandardButtonPath,
            float labelRightPadding = 18f)
        {
            GameObject buttonObject = new GameObject("Button", typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(contentRoot, false);
            ConfigureRect(buttonObject.GetComponent<RectTransform>(), position, size);
            Image image = buttonObject.GetComponent<Image>();
            image.color = interactable ? new Color(MenuRow.r, MenuRow.g, MenuRow.b, 0.62f) : new Color(0.25f, 0.25f, 0.25f, 0.6f);
            Button button = buttonObject.GetComponent<Button>();
            RawImage frame = AddButtonFrame(buttonObject.transform, frameResourcePath);
            if (frame != null)
            {
                image.color = Color.clear;
                frame.color = interactable ? Color.white : new Color(0.48f, 0.48f, 0.48f, 0.72f);
                button.targetGraphic = frame;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.72f, 0.9f, 1f, 1f);
            colors.pressedColor = new Color(0.48f, 0.72f, 0.9f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.48f, 0.48f, 0.48f, 0.72f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.interactable = interactable;
            button.onClick.AddListener(() => PlayUiSound(clickSound == UiButtonSound.Cancel ? cancelSound : selectSound));
            button.onClick.AddListener(action);
            AddButtonEventSounds(buttonObject, interactable);
            if (addLabel)
            {
                AddButtonLabel(buttonObject.transform, value, interactable ? ButtonText : new Color(Paper.r, Paper.g, Paper.b, 0.52f), labelLeftPadding, labelRightPadding);
            }
            return buttonObject;
        }

        private static RawImage AddButtonFrame(Transform parent, string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return null;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            GameObject frameObject = new GameObject("Frame", typeof(RawImage));
            frameObject.transform.SetParent(parent, false);
            frameObject.transform.SetAsFirstSibling();
            RectTransform rect = frameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            RawImage frame = frameObject.GetComponent<RawImage>();
            frame.texture = texture;
            frame.raycastTarget = false;
            return frame;
        }

        private void AddButtonEventSounds(GameObject buttonObject, bool interactable)
        {
            EventTrigger eventTrigger = buttonObject.AddComponent<EventTrigger>();
            eventTrigger.triggers = new List<EventTrigger.Entry>();
            AddEventTriggerEntry(eventTrigger, EventTriggerType.PointerEnter, _ =>
            {
                if (interactable)
                {
                    PlayFocusSoundOnce(buttonObject);
                }
            });
            AddEventTriggerEntry(eventTrigger, EventTriggerType.Select, _ =>
            {
                if (interactable)
                {
                    PlayFocusSoundOnce(buttonObject);
                }
            });

            if (!interactable)
            {
                AddEventTriggerEntry(eventTrigger, EventTriggerType.PointerClick, _ => PlayUiSound(disabledSound));
            }
        }

        private void PlayFocusSoundOnce(GameObject buttonObject)
        {
            if (lastFocusedButton == buttonObject)
            {
                return;
            }

            lastFocusedButton = buttonObject;
            PlayUiSound(focusSound);
        }

        private static void AddEventTriggerEntry(
            EventTrigger eventTrigger,
            EventTriggerType eventType,
            UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = eventType,
                callback = new EventTrigger.TriggerEvent()
            };
            entry.callback.AddListener(callback);
            eventTrigger.triggers.Add(entry);
        }

        private void AddButtonLabel(Transform parent, string value, Color color, float leftPadding, float rightPadding)
        {
            GameObject label = new GameObject("Text", typeof(Text));
            label.transform.SetParent(parent, false);
            RectTransform rect = label.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(leftPadding, 6f);
            rect.offsetMax = new Vector2(-rightPadding, -6f);
            Text text = label.GetComponent<Text>();
            text.text = value;
            theme.Apply(text, FrontendTypography.MenuPrimary, color, TextAnchor.MiddleLeft);
            text.fontSize = Mathf.RoundToInt(text.fontSize * uiTextScale);
            text.raycastTarget = false;
            theme.AddContrast(text, true);
        }

        private static void AddButtonIcon(Transform parent, string resourcePath, Vector2 size, Vector2 position)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return;
            }

            GameObject iconObject = new GameObject("Icon", typeof(RawImage));
            iconObject.transform.SetParent(parent, false);
            RectTransform rect = iconObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            RawImage image = iconObject.GetComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
        }

        private void AddSlotPlaceholder(Transform parent, SaveSlotSummary slot)
        {
            GameObject panel = new GameObject("ThumbnailPlaceholder", typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(80f, 0f);
            rect.sizeDelta = new Vector2(126f, 110f);
            Image image = panel.GetComponent<Image>();
            image.color = slot.State == SaveSlotState.Valid
                ? new Color(0.12f, 0.2f, 0.29f, 0.88f)
                : new Color(0.12f, 0.12f, 0.15f, 0.8f);
            image.raycastTarget = false;
            AddButtonIcon(panel.transform, SaveThumbnailFramePath, new Vector2(126f, 110f), new Vector2(63f, 0f));

            GameObject label = new GameObject("PlaceholderLabel", typeof(Text));
            label.transform.SetParent(panel.transform, false);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            Text text = label.GetComponent<Text>();
            text.text = slot.State == SaveSlotState.Valid ? "기록\n이미지" : "기록";
            theme.Apply(text, FrontendTypography.Caption, theme.Disabled, TextAnchor.MiddleCenter);
            text.fontSize = Mathf.RoundToInt(text.fontSize * uiTextScale);
            text.raycastTarget = false;
        }

        private static void AddSlotBadge(Transform parent, SaveSlotSummary slot)
        {
            string badgePath;
            if (slot.State == SaveSlotState.Valid && slot.RecoveredFromBackup)
            {
                badgePath = SaveBadgeRecoveredPath;
            }
            else
            {
                switch (slot.State)
                {
                    case SaveSlotState.Empty:
                        badgePath = SaveBadgeEmptyPath;
                        break;
                    case SaveSlotState.Valid:
                        badgePath = SaveBadgeValidPath;
                        break;
                    case SaveSlotState.Corrupt:
                        badgePath = SaveBadgeCorruptPath;
                        break;
                    default:
                        badgePath = SaveBadgeIncompatiblePath;
                        break;
                }
            }

            AddButtonIcon(parent, badgePath, new Vector2(48f, 48f), new Vector2(938f, 0f));
        }

        private static Image AddMenuPanel(Transform parent)
        {
            Sprite sprite = Resources.Load<Sprite>(MainMenuPanelPath);
            if (sprite == null)
            {
                return null;
            }

            GameObject panelObject = new GameObject("Panel", typeof(Image));
            panelObject.transform.SetParent(parent, false);
            panelObject.transform.SetAsFirstSibling();
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image panel = panelObject.GetComponent<Image>();
            panel.sprite = sprite;
            panel.type = Image.Type.Simple;
            panel.preserveAspect = true;
            panel.raycastTarget = false;
            return panel;
        }

        private sealed class BootAtmosphere : IDisposable
        {
            private const float BreathSeconds = 10f;
            private const float LogoPulseSeconds = 0.4f;
            private const float NoticeCueSeconds = 0.45f;
            private readonly bool isWarningNotice;
            private readonly float createdAt;
            private readonly RawImage glow;
            private readonly RawImage burgundyUndertone;
            private readonly List<AtmosphereParticle> particles = new List<AtmosphereParticle>();
            private readonly List<ScanLine> scanLines = new List<ScanLine>();
            private readonly Texture2D radialTexture;
            private readonly Texture2D vignetteTexture;
            private readonly Texture2D bottomGradientTexture;
            private readonly Texture2D whiteTexture;
            private float logoPulseStartedAt = float.NegativeInfinity;
            private float noticeCueStartedAt = float.NegativeInfinity;
            private bool disposed;

            public BootAtmosphere(RectTransform parent, bool isWarningNotice)
            {
                this.isWarningNotice = isWarningNotice;
                createdAt = Time.unscaledTime;
                radialTexture = CreateRadialTexture(false);
                vignetteTexture = CreateRadialTexture(true);
                bottomGradientTexture = CreateBottomGradientTexture();
                whiteTexture = CreateWhiteTexture();

                AddSolidBackground(parent, OpeningPrimary);
                burgundyUndertone = AddFullscreenRawImage(parent, "OpeningBurgundyUndertone", bottomGradientTexture, new Color(OpeningBurgundy.r, OpeningBurgundy.g, OpeningBurgundy.b, isWarningNotice ? 0.28f : 0.08f));
                glow = AddFullscreenRawImage(parent, "OpeningCentralGlow", radialTexture, new Color(OpeningPrimaryLight.r, OpeningPrimaryLight.g, OpeningPrimaryLight.b, 0.12f));
                AddFullscreenRawImage(parent, "OpeningVignette", vignetteTexture, new Color(0f, 0f, 0f, 0.56f));
                CreateDust(parent);
                if (isWarningNotice)
                {
                    CreateScanLines(parent);
                }
            }

            public void TriggerLogoPulse()
            {
                logoPulseStartedAt = Time.unscaledTime;
            }

            public void TriggerNoticeCue()
            {
                if (isWarningNotice)
                {
                    noticeCueStartedAt = Time.unscaledTime;
                }
            }

            public void Tick(float now)
            {
                if (disposed)
                {
                    return;
                }

                float elapsed = Mathf.Max(0f, now - createdAt);
                float breath = 0.5f + 0.5f * Mathf.Sin((elapsed / BreathSeconds) * Mathf.PI * 2f - Mathf.PI * 0.5f);
                float logoPulse = Pulse(now, logoPulseStartedAt, LogoPulseSeconds);
                float noticeCue = Pulse(now, noticeCueStartedAt, NoticeCueSeconds);
                float glowAlpha = 0.10f + breath * 0.045f + logoPulse * 0.18f + noticeCue * 0.07f;
                glow.color = new Color(OpeningPrimaryLight.r, OpeningPrimaryLight.g, OpeningPrimaryLight.b, glowAlpha);
                glow.rectTransform.localScale = Vector3.one * (1.02f + breath * 0.035f + logoPulse * 0.08f + noticeCue * 0.025f);
                burgundyUndertone.color = new Color(
                    OpeningBurgundy.r,
                    OpeningBurgundy.g,
                    OpeningBurgundy.b,
                    (isWarningNotice ? 0.22f : 0.07f) + breath * 0.025f + noticeCue * 0.035f);

                foreach (AtmosphereParticle particle in particles)
                {
                    float height = Mathf.Repeat(particle.StartY + elapsed * particle.Speed, 1260f) - 630f;
                    float drift = Mathf.Sin(elapsed * particle.DriftSpeed + particle.Phase) * particle.DriftDistance;
                    particle.Rect.anchoredPosition = new Vector2(particle.StartX + drift, height);
                    float flicker = 0.72f + 0.28f * Mathf.Sin(elapsed * 0.36f + particle.Phase);
                    particle.Image.color = new Color(particle.Color.r, particle.Color.g, particle.Color.b, particle.Alpha * flicker);
                }

                foreach (ScanLine scanLine in scanLines)
                {
                    float offset = Mathf.Sin(elapsed * scanLine.Speed + scanLine.Phase) * 2.5f;
                    scanLine.Rect.anchoredPosition = new Vector2(offset, scanLine.Y);
                    float flicker = 0.65f + 0.35f * Mathf.Sin(elapsed * 0.22f + scanLine.Phase);
                    scanLine.Image.color = new Color(OpeningGold.r, OpeningGold.g, OpeningGold.b, scanLine.Alpha * flicker);
                }
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                DestroyTexture(radialTexture);
                DestroyTexture(vignetteTexture);
                DestroyTexture(bottomGradientTexture);
                DestroyTexture(whiteTexture);
                particles.Clear();
                scanLines.Clear();
            }

            private void CreateDust(RectTransform parent)
            {
                const int count = 22;
                for (int index = 0; index < count; index++)
                {
                    float seed = index * 17.173f;
                    RawImage image = AddRawImage(parent, "OpeningDust", whiteTexture, new Color(OpeningCyan.r, OpeningCyan.g, OpeningCyan.b, 0.03f));
                    RectTransform rect = image.rectTransform;
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    float size = 1.2f + Mathf.Repeat(seed * 1.71f, 1f) * 2.3f;
                    rect.sizeDelta = new Vector2(size, size);
                    float startX = Mathf.Lerp(-930f, 930f, Mathf.Repeat(seed * 0.619f, 1f));
                    float startY = Mathf.Lerp(-620f, 620f, Mathf.Repeat(seed * 0.287f, 1f));
                    Color particleColor = index % 6 == 0 ? OpeningGold : OpeningCyan;
                    particles.Add(new AtmosphereParticle(
                        rect,
                        image,
                        startX,
                        startY,
                        3.5f + Mathf.Repeat(seed * 0.43f, 1f) * 6f,
                        0.10f + Mathf.Repeat(seed * 0.91f, 1f) * 0.15f,
                        7f + Mathf.Repeat(seed * 0.37f, 1f) * 16f,
                        seed,
                        particleColor,
                        0.025f + Mathf.Repeat(seed * 0.73f, 1f) * 0.035f));
                }
            }

            private void CreateScanLines(RectTransform parent)
            {
                const int count = 16;
                for (int index = 0; index < count; index++)
                {
                    float seed = index * 23.791f;
                    RawImage image = AddRawImage(parent, "OpeningScanLine", whiteTexture, new Color(OpeningGold.r, OpeningGold.g, OpeningGold.b, 0.01f));
                    RectTransform rect = image.rectTransform;
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = new Vector2(1920f, 1f);
                    float y = Mathf.Lerp(-520f, 520f, Mathf.Repeat(seed * 0.243f, 1f));
                    scanLines.Add(new ScanLine(rect, image, y, 0.16f + Mathf.Repeat(seed * 0.61f, 1f) * 0.2f, seed, 0.012f));
                }
            }

            private static void AddSolidBackground(RectTransform parent, Color color)
            {
                GameObject background = new GameObject("OpeningBackground", typeof(Image));
                background.transform.SetParent(parent, false);
                RectTransform rect = background.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                Image image = background.GetComponent<Image>();
                image.color = color;
                image.raycastTarget = false;
            }

            private static RawImage AddFullscreenRawImage(RectTransform parent, string name, Texture texture, Color color)
            {
                RawImage image = AddRawImage(parent, name, texture, color);
                RectTransform rect = image.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                return image;
            }

            private static RawImage AddRawImage(RectTransform parent, string name, Texture texture, Color color)
            {
                GameObject imageObject = new GameObject(name, typeof(RawImage));
                imageObject.transform.SetParent(parent, false);
                RawImage image = imageObject.GetComponent<RawImage>();
                image.texture = texture;
                image.color = color;
                image.raycastTarget = false;
                return image;
            }

            private static Texture2D CreateRadialTexture(bool inverted)
            {
                const int size = 96;
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
                {
                    name = inverted ? "OpeningVignetteTexture" : "OpeningGlowTexture",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.DontSave,
                };
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float horizontal = (x / (float)(size - 1) - 0.5f) * 2f;
                        float vertical = (y / (float)(size - 1) - 0.5f) * 2f;
                        float distance = Mathf.Clamp01(Mathf.Sqrt(horizontal * horizontal + vertical * vertical));
                        float alpha = inverted
                            ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.38f, 1f, distance))
                            : Mathf.Pow(1f - distance, 2.15f);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }

                texture.Apply(false, true);
                return texture;
            }

            private static Texture2D CreateBottomGradientTexture()
            {
                const int width = 4;
                const int height = 96;
                Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
                {
                    name = "OpeningBottomGradientTexture",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.DontSave,
                };
                for (int y = 0; y < height; y++)
                {
                    float normalized = y / (float)(height - 1);
                    float alpha = Mathf.Pow(1f - normalized, 2.2f);
                    for (int x = 0; x < width; x++)
                    {
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }

                texture.Apply(false, true);
                return texture;
            }

            private static Texture2D CreateWhiteTexture()
            {
                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true)
                {
                    name = "OpeningParticleTexture",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.DontSave,
                };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply(false, true);
                return texture;
            }

            private static float Pulse(float now, float startedAt, float duration)
            {
                if (float.IsNegativeInfinity(startedAt) || duration <= 0f)
                {
                    return 0f;
                }

                float progress = (now - startedAt) / duration;
                return progress <= 0f || progress >= 1f ? 0f : Mathf.Sin(progress * Mathf.PI);
            }

            private static void DestroyTexture(Texture2D texture)
            {
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }

            private sealed class AtmosphereParticle
            {
                public AtmosphereParticle(RectTransform rect, RawImage image, float startX, float startY, float speed, float driftSpeed, float driftDistance, float phase, Color color, float alpha)
                {
                    Rect = rect;
                    Image = image;
                    StartX = startX;
                    StartY = startY;
                    Speed = speed;
                    DriftSpeed = driftSpeed;
                    DriftDistance = driftDistance;
                    Phase = phase;
                    Color = color;
                    Alpha = alpha;
                }

                public RectTransform Rect { get; }
                public RawImage Image { get; }
                public float StartX { get; }
                public float StartY { get; }
                public float Speed { get; }
                public float DriftSpeed { get; }
                public float DriftDistance { get; }
                public float Phase { get; }
                public Color Color { get; }
                public float Alpha { get; }
            }

            private sealed class ScanLine
            {
                public ScanLine(RectTransform rect, RawImage image, float y, float speed, float phase, float alpha)
                {
                    Rect = rect;
                    Image = image;
                    Y = y;
                    Speed = speed;
                    Phase = phase;
                    Alpha = alpha;
                }

                public RectTransform Rect { get; }
                public RawImage Image { get; }
                public float Y { get; }
                public float Speed { get; }
                public float Phase { get; }
                public float Alpha { get; }
            }
        }

        private static void ConfigureRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(position.x - 960f, 540f - position.y);
            rect.sizeDelta = size;
        }

        private static Color ColorFromHex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            return color;
        }

        private static string FormatPlayTime(long seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(Math.Max(0L, seconds));
            return time.TotalHours >= 1d
                ? ((int)time.TotalHours).ToString("00") + ":" + time.Minutes.ToString("00") + ":" + time.Seconds.ToString("00")
                : time.Minutes.ToString("00") + ":" + time.Seconds.ToString("00");
        }

        private static string GetEntryDisplayName(string entryId, string checkpointId)
        {
            if (entryId == GameEntryPoint.PrologueStartId && checkpointId == "start")
            {
                return "프롤로그 · 업무 개시";
            }

            return "미확인 기록 위치";
        }

        private static string DifficultyDisplayName(string difficultyId)
        {
            switch (difficultyId)
            {
                case DifficultyId.StoryValue:
                    return "기록 열람";
                case DifficultyId.HardValue:
                    return "특별 압류";
                default:
                    return "정규 파견";
            }
        }

        private static bool WasAnyInputPressed()
        {
            return (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                || (Mouse.current != null
                    && (Mouse.current.leftButton.wasPressedThisFrame
                        || Mouse.current.rightButton.wasPressedThisFrame
                        || Mouse.current.middleButton.wasPressedThisFrame))
                || (Gamepad.current != null
                    && (Gamepad.current.buttonSouth.wasPressedThisFrame
                        || Gamepad.current.buttonNorth.wasPressedThisFrame
                        || Gamepad.current.buttonEast.wasPressedThisFrame
                        || Gamepad.current.buttonWest.wasPressedThisFrame
                        || Gamepad.current.startButton.wasPressedThisFrame));
        }

        private static bool WasCancelPressed()
        {
            return (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                || (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
        }
    }
}

using System;
using DemonLord.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DemonLord.Presentation.Exploration
{
    public enum PauseSettingsChange
    {
        MasterDown,
        MasterUp,
        BgmDown,
        BgmUp,
        SfxDown,
        SfxUp,
        DisplayModePrevious,
        DisplayModeNext,
        ResolutionPrevious,
        ResolutionNext,
        VSyncToggle,
        QualityPrevious,
        QualityNext,
        UiScaleDown,
        UiScaleUp,
        ScreenShakeToggle,
        FlashesToggle,
        TransitionsToggle,
    }

    public enum PauseSettingsPage
    {
        Audio,
        Display,
        Accessibility,
    }

    [DisallowMultipleComponent]
    public sealed class PauseMenuView : MonoBehaviour
    {
        private const string PauseFramePath = "UI/Settings/settings_window_frame";
        private const string StandardButtonPath = "UI/Common/button_standard";

        private static readonly Vector2 CenterAnchor = new Vector2(0.5f, 0.5f);
        private static readonly Color Surface = ColorFromHex("171C22", 0.96f);
        private static readonly Color Gold = ColorFromHex("B99A59", 1f);
        private static readonly Color Cyan = ColorFromHex("5CAECC", 1f);
        private static readonly Color MainText = ColorFromHex("EEE6D5", 1f);
        private static readonly Color MutedText = ColorFromHex("9AA6AF", 1f);
        private static readonly Color Danger = ColorFromHex("7D1827", 1f);

        [SerializeField] private CanvasGroup overlayGroup = null;
        [SerializeField] private EventSystem eventSystem = null;
        [SerializeField] private AudioSource audioSource = null;
        [SerializeField] private AudioClip saveCompleteClip = null;
        [SerializeField] private RectTransform menuFrame = null;
        [SerializeField] private GameObject rootPanel = null;
        [SerializeField] private GameObject settingsPanel = null;
        [SerializeField] private GameObject controlsPanel = null;
        [SerializeField] private GameObject confirmationPanel = null;
        [SerializeField] private Text statusLabel = null;
        [SerializeField] private Text settingsStatusLabel = null;
        [SerializeField] private Text confirmationLabel = null;
        [SerializeField] private Text[] settingsValueLabels = Array.Empty<Text>();
        [SerializeField] private Button[] rootButtons = Array.Empty<Button>();
        [SerializeField] private Button[] settingsButtons = Array.Empty<Button>();
        [SerializeField] private Button[] controlsButtons = Array.Empty<Button>();
        [SerializeField] private Button[] confirmationButtons = Array.Empty<Button>();

        private Button[] activeButtons = Array.Empty<Button>();
        private int selectedIndex;
        private PauseSettingsPage settingsPage;
        private bool buttonListenersBound;

        public event Action ContinueRequested;
        public event Action SaveRequested;
        public event Action SettingsRequested;
        public event Action ControlsRequested;
        public event Action ReturnToTitleRequested;
        public event Action QuitRequested;
        public event Action SettingsApplyRequested;
        public event Action SettingsCancelRequested;
        public event Action SettingsResetRequested;
        public event Action<PauseSettingsPage> SettingsPageRequested;
        public event Action<PauseSettingsChange> SettingsChangeRequested;
        public event Action ConfirmationAccepted;
        public event Action ConfirmationCancelled;

        private void Awake()
        {
            // The scene builder creates the menu in Edit Mode. Unity does not serialize
            // lambda listeners that were added there, so every runtime instance must
            // establish its own button-to-view event bridge here.
            BindButtonListeners();
        }

        public void Configure(Font configuredFont, EventSystem configuredEventSystem, AudioSource configuredAudioSource, AudioClip configuredSaveCompleteClip)
        {
            eventSystem = configuredEventSystem;
            audioSource = configuredAudioSource;
            saveCompleteClip = configuredSaveCompleteClip;
            if (overlayGroup == null)
            {
                overlayGroup = GetComponent<CanvasGroup>();
                if (overlayGroup == null)
                {
                    overlayGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (rootPanel == null)
            {
                Build(configuredFont);
            }

            if (menuFrame == null && rootPanel != null)
            {
                menuFrame = rootPanel.transform.parent as RectTransform;
            }

            BindButtonListeners();
            SetVisible(false);
        }

        /// <summary>
        /// Applies only presentation settings that have a real owner in this view.
        /// The central frame scales without changing the fullscreen dim layer or its anchors.
        /// BGM is intentionally not touched here because this view does not own a BGM source.
        /// </summary>
        public void ApplySettings(GameSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            if (menuFrame == null && rootPanel != null)
            {
                menuFrame = rootPanel.transform.parent as RectTransform;
            }

            if (menuFrame != null)
            {
                menuFrame.localScale = Vector3.one * settings.UiScale;
            }

            if (audioSource != null)
            {
                audioSource.volume = Mathf.Clamp01(settings.MasterVolume * settings.SfxVolume);
            }
        }

        public void ShowRoot(bool canSave, string status = null)
        {
            SetVisible(true);
            rootPanel.SetActive(true);
            settingsPanel.SetActive(false);
            controlsPanel.SetActive(false);
            confirmationPanel.SetActive(false);
            foreach (Button button in rootButtons)
            {
                button.interactable = true;
            }

            rootButtons[1].interactable = canSave;
            SetStatus(status ?? string.Empty, status == null ? MutedText : MainText);
            SetActiveButtons(rootButtons);
        }

        public void ShowBusy(string message)
        {
            SetVisible(true);
            rootPanel.SetActive(true);
            settingsPanel.SetActive(false);
            controlsPanel.SetActive(false);
            confirmationPanel.SetActive(false);
            foreach (Button button in rootButtons)
            {
                button.interactable = false;
            }

            SetStatus(message, MutedText);
            activeButtons = Array.Empty<Button>();
        }

        public void ShowSettings(GameSettings settings)
        {
            SetVisible(true);
            rootPanel.SetActive(false);
            settingsPanel.SetActive(true);
            controlsPanel.SetActive(false);
            confirmationPanel.SetActive(false);
            SetStatus(string.Empty, MutedText);
            SetSettingsStatus(string.Empty, MutedText);
            RenderSettings(settings);
            SetActiveButtons(settingsButtons);
        }

        public void ShowControls()
        {
            SetVisible(true);
            rootPanel.SetActive(false);
            settingsPanel.SetActive(false);
            controlsPanel.SetActive(true);
            confirmationPanel.SetActive(false);
            SetStatus(string.Empty, MutedText);
            SetActiveButtons(controlsButtons);
        }

        public void ShowConfirmation(bool returnToTitle)
        {
            SetVisible(true);
            rootPanel.SetActive(false);
            settingsPanel.SetActive(false);
            controlsPanel.SetActive(false);
            confirmationPanel.SetActive(true);
            confirmationLabel.text = returnToTitle
                ? "타이틀 화면으로 돌아가시겠습니까?\n저장하지 않은 진행은 사라질 수 있습니다."
                : "게임을 종료하시겠습니까?";
            SetStatus(string.Empty, MutedText);
            SetActiveButtons(confirmationButtons);
        }

        public void SetStatus(string message, Color color)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message ?? string.Empty;
                statusLabel.color = color;
            }
        }

        public void SetSettingsStatus(string message, Color color)
        {
            if (settingsStatusLabel != null)
            {
                settingsStatusLabel.text = message ?? string.Empty;
                settingsStatusLabel.color = color;
            }
        }

        public void RenderSettings(GameSettings settings)
        {
            if (settings == null || settingsValueLabels == null || settingsValueLabels.Length < 4)
            {
                return;
            }

            switch (settingsPage)
            {
                case PauseSettingsPage.Audio:
                    settingsValueLabels[0].text = "주 음량  " + ToPercent(settings.MasterVolume);
                    settingsValueLabels[1].text = "배경 음악  " + ToPercent(settings.BgmVolume);
                    settingsValueLabels[2].text = "효과음  " + ToPercent(settings.SfxVolume);
                    settingsValueLabels[3].text = "현재 값은 즉시 적용됩니다.";
                    break;
                case PauseSettingsPage.Display:
                    settingsValueLabels[0].text = "화면 모드  " + DisplayModeLabel(settings.DisplayMode);
                    settingsValueLabels[1].text = "해상도  " + settings.ResolutionWidth + " × " + settings.ResolutionHeight;
                    settingsValueLabels[2].text = "수직 동기화  " + OnOff(settings.VSyncEnabled);
                    settingsValueLabels[3].text = "그래픽 품질  " + QualityLabel(settings.QualityPreset);
                    break;
                default:
                    settingsValueLabels[0].text = "UI 크기  " + ToPercent(settings.UiScale);
                    settingsValueLabels[1].text = "화면 흔들림 감소  " + OnOff(settings.ReduceScreenShake);
                    settingsValueLabels[2].text = "섬광 감소  " + OnOff(settings.ReduceFlashes);
                    settingsValueLabels[3].text = "전환 애니메이션 감소  " + OnOff(settings.ReduceTransitions);
                    break;
            }
        }

        public void SetSettingsPage(PauseSettingsPage page, GameSettings settings)
        {
            settingsPage = page;
            RenderSettings(settings);
        }

        public void MoveSelection(int delta)
        {
            if (activeButtons == null || activeButtons.Length == 0 || delta == 0)
            {
                return;
            }

            int direction = delta > 0 ? -1 : 1;
            int initial = selectedIndex;
            do
            {
                selectedIndex = (selectedIndex + direction + activeButtons.Length) % activeButtons.Length;
                if (activeButtons[selectedIndex] != null && activeButtons[selectedIndex].interactable)
                {
                    Select(activeButtons[selectedIndex]);
                    return;
                }
            }
            while (selectedIndex != initial);
        }

        public void SubmitFocused()
        {
            if (activeButtons == null || selectedIndex < 0 || selectedIndex >= activeButtons.Length)
            {
                return;
            }

            Button button = activeButtons[selectedIndex];
            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
            }
        }

        public void FocusFirst()
        {
            selectedIndex = 0;
            if (activeButtons != null)
            {
                for (int i = 0; i < activeButtons.Length; i++)
                {
                    if (activeButtons[i] != null && activeButtons[i].interactable)
                    {
                        selectedIndex = i;
                        Select(activeButtons[i]);
                        return;
                    }
                }
            }
        }

        public void PlaySaveComplete()
        {
            if (audioSource != null && saveCompleteClip != null)
            {
                audioSource.PlayOneShot(saveCompleteClip);
            }
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void Build(Font font)
        {
            CreateImage("Dim", transform, Color.black, true, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            GameObject frame = CreateImage("MenuFrame", transform, Surface, true, CenterAnchor, CenterAnchor, Vector2.zero, new Vector2(920f, 740f), CenterAnchor).gameObject;
            menuFrame = frame.GetComponent<RectTransform>();
            AddDecorativeFrame(frame.transform, PauseFramePath);

            rootPanel = CreatePanel("Root", frame.transform);
            CreateLabel("Title", rootPanel.transform, font, "메뉴", 36, MainText, TextAnchor.MiddleCenter, new Vector2(0f, 302f), new Vector2(680f, 58f));
            string[] rootLabels = { "계속하기", "저장하기", "환경 설정", "조작 안내", "타이틀로 돌아가기", "게임 종료" };
            rootButtons = new Button[rootLabels.Length];
            for (int i = 0; i < rootLabels.Length; i++)
            {
                rootButtons[i] = CreateButton("RootButton_" + i, rootPanel.transform, font, rootLabels[i], new Vector2(0f, 220f - i * 78f), new Vector2(560f, 60f), i >= 4 ? Danger : Surface);
            }

            statusLabel = CreateLabel("Status", rootPanel.transform, font, string.Empty, 19, MutedText, TextAnchor.MiddleCenter, new Vector2(0f, -290f), new Vector2(500f, 36f));

            settingsPanel = CreatePanel("Settings", frame.transform);
            CreateLabel("Title", settingsPanel.transform, font, "환경 설정", 34, MainText, TextAnchor.MiddleCenter, new Vector2(0f, 302f), new Vector2(680f, 52f));
            Button audioTab = CreateButton("AudioTab", settingsPanel.transform, font, "음향", new Vector2(-180f, 248f), new Vector2(150f, 46f), Surface);
            Button displayTab = CreateButton("DisplayTab", settingsPanel.transform, font, "화면", new Vector2(0f, 248f), new Vector2(150f, 46f), Surface);
            Button accessibilityTab = CreateButton("AccessibilityTab", settingsPanel.transform, font, "접근성", new Vector2(180f, 248f), new Vector2(150f, 46f), Surface);
            settingsValueLabels = new Text[4];
            for (int i = 0; i < settingsValueLabels.Length; i++)
            {
                settingsValueLabels[i] = CreateLabel("SettingValue_" + i, settingsPanel.transform, font, string.Empty, 22, MainText, TextAnchor.MiddleCenter, new Vector2(0f, 145f - i * 78f), new Vector2(390f, 44f));
            }

            settingsStatusLabel = CreateLabel("SettingsStatus", settingsPanel.transform, font, string.Empty, 17, MutedText, TextAnchor.MiddleCenter, new Vector2(0f, -205f), new Vector2(500f, 34f));

            Button[] rowAdjustmentButtons = new Button[8];
            for (int row = 0; row < 4; row++)
            {
                float y = 145f - row * 78f;
                rowAdjustmentButtons[row * 2] = CreateButton("SettingsPrevious_" + row, settingsPanel.transform, font, "−", new Vector2(-245f, y), new Vector2(54f, 46f), Surface);
                rowAdjustmentButtons[row * 2 + 1] = CreateButton("SettingsNext_" + row, settingsPanel.transform, font, "+", new Vector2(245f, y), new Vector2(54f, 46f), Surface);
            }
            Button reset = CreateButton("SettingsReset", settingsPanel.transform, font, "기본값", new Vector2(-170f, -282f), new Vector2(145f, 52f), Surface);
            Button cancel = CreateButton("SettingsCancel", settingsPanel.transform, font, "취소", new Vector2(0f, -282f), new Vector2(145f, 52f), Surface);
            Button apply = CreateButton("SettingsApply", settingsPanel.transform, font, "적용", new Vector2(170f, -282f), new Vector2(145f, 52f), Cyan);
            settingsButtons = new[]
            {
                audioTab, displayTab, accessibilityTab,
                rowAdjustmentButtons[0], rowAdjustmentButtons[1], rowAdjustmentButtons[2], rowAdjustmentButtons[3],
                rowAdjustmentButtons[4], rowAdjustmentButtons[5], rowAdjustmentButtons[6], rowAdjustmentButtons[7],
                reset, cancel, apply,
            };

            controlsPanel = CreatePanel("Controls", frame.transform);
            CreateLabel("Title", controlsPanel.transform, font, "조작 안내", 34, MainText, TextAnchor.MiddleCenter, new Vector2(0f, 302f), new Vector2(680f, 52f));
            Text controlText = CreateLabel(
                "Controls", controlsPanel.transform, font,
                "WASD  이동\nLeft Shift  달리기\nSpace  대시\nF / Enter  상호작용 · 대화 진행\nQ / E  카메라 회전\nMouse Wheel  확대 · 축소\nEscape  메뉴 · 뒤로\nGamepad  Left Stick 이동 · South 확인 · East 뒤로 · Start 메뉴",
                20, MainText, TextAnchor.UpperLeft, new Vector2(0f, 120f), new Vector2(500f, 360f));
            controlText.lineSpacing = 1.2f;
            Button controlsBack = CreateButton("ControlsBack", controlsPanel.transform, font, "돌아가기", new Vector2(0f, -282f), new Vector2(190f, 52f), Surface);
            controlsButtons = new[] { controlsBack };

            confirmationPanel = CreatePanel("Confirmation", frame.transform);
            CreateLabel("Title", confirmationPanel.transform, font, "확인", 34, MainText, TextAnchor.MiddleCenter, new Vector2(0f, 250f), new Vector2(680f, 52f));
            confirmationLabel = CreateLabel("Message", confirmationPanel.transform, font, string.Empty, 24, MainText, TextAnchor.MiddleCenter, new Vector2(0f, 70f), new Vector2(480f, 180f));
            Button confirm = CreateButton("Confirm", confirmationPanel.transform, font, "확인", new Vector2(-105f, -210f), new Vector2(180f, 58f), Danger);
            Button confirmationCancel = CreateButton("Cancel", confirmationPanel.transform, font, "취소", new Vector2(105f, -210f), new Vector2(180f, 58f), Surface);
            confirmationButtons = new[] { confirm, confirmationCancel };

            rootPanel.SetActive(false);
            settingsPanel.SetActive(false);
            controlsPanel.SetActive(false);
            confirmationPanel.SetActive(false);
        }

        private void BindButtonListeners()
        {
            if (buttonListenersBound)
            {
                return;
            }

            if (rootButtons == null || rootButtons.Length < 6
                || settingsButtons == null || settingsButtons.Length < 14
                || controlsButtons == null || controlsButtons.Length < 1
                || confirmationButtons == null || confirmationButtons.Length < 2)
            {
                return;
            }

            buttonListenersBound = true;
            for (int index = 0; index < rootButtons.Length; index++)
            {
                int capturedIndex = index;
                Bind(rootButtons[index], () => InvokeRoot(capturedIndex));
            }

            Bind(settingsButtons[0], () => RequestSettingsPage(PauseSettingsPage.Audio));
            Bind(settingsButtons[1], () => RequestSettingsPage(PauseSettingsPage.Display));
            Bind(settingsButtons[2], () => RequestSettingsPage(PauseSettingsPage.Accessibility));
            for (int row = 0; row < 4; row++)
            {
                int capturedRow = row;
                Bind(settingsButtons[3 + row * 2], () => SettingsChangeRequested?.Invoke(ChangeForSettingsRow(capturedRow, false)));
                Bind(settingsButtons[4 + row * 2], () => SettingsChangeRequested?.Invoke(ChangeForSettingsRow(capturedRow, true)));
            }

            Bind(settingsButtons[11], () => SettingsResetRequested?.Invoke());
            Bind(settingsButtons[12], () => SettingsCancelRequested?.Invoke());
            Bind(settingsButtons[13], () => SettingsApplyRequested?.Invoke());
            Bind(controlsButtons[0], () => ControlsRequested?.Invoke());
            Bind(confirmationButtons[0], () => ConfirmationAccepted?.Invoke());
            Bind(confirmationButtons[1], () => ConfirmationCancelled?.Invoke());
        }

        private static void Bind(Button button, Action callback)
        {
            if (button != null && callback != null)
            {
                button.onClick.AddListener(() => callback());
            }
        }

        private void InvokeRoot(int index)
        {
            switch (index)
            {
                case 0: ContinueRequested?.Invoke(); break;
                case 1: SaveRequested?.Invoke(); break;
                case 2: SettingsRequested?.Invoke(); break;
                case 3: ControlsRequested?.Invoke(); break;
                case 4: ReturnToTitleRequested?.Invoke(); break;
                case 5: QuitRequested?.Invoke(); break;
            }
        }

        private void RequestSettingsPage(PauseSettingsPage page)
        {
            settingsPage = page;
            SettingsPageRequested?.Invoke(page);
        }

        private PauseSettingsChange ChangeForSettingsRow(int row, bool increment)
        {
            switch (settingsPage)
            {
                case PauseSettingsPage.Audio:
                    if (row == 0)
                    {
                        return increment ? PauseSettingsChange.MasterUp : PauseSettingsChange.MasterDown;
                    }

                    if (row == 1)
                    {
                        return increment ? PauseSettingsChange.BgmUp : PauseSettingsChange.BgmDown;
                    }

                    return increment ? PauseSettingsChange.SfxUp : PauseSettingsChange.SfxDown;
                case PauseSettingsPage.Display:
                    if (row == 0)
                    {
                        return increment ? PauseSettingsChange.DisplayModeNext : PauseSettingsChange.DisplayModePrevious;
                    }

                    if (row == 1)
                    {
                        return increment ? PauseSettingsChange.ResolutionNext : PauseSettingsChange.ResolutionPrevious;
                    }

                    if (row == 2)
                    {
                        return PauseSettingsChange.VSyncToggle;
                    }

                    return increment ? PauseSettingsChange.QualityNext : PauseSettingsChange.QualityPrevious;
                default:
                    if (row == 0)
                    {
                        return increment ? PauseSettingsChange.UiScaleUp : PauseSettingsChange.UiScaleDown;
                    }

                    if (row == 1)
                    {
                        return PauseSettingsChange.ScreenShakeToggle;
                    }

                    if (row == 2)
                    {
                        return PauseSettingsChange.FlashesToggle;
                    }

                    return PauseSettingsChange.TransitionsToggle;
            }
        }

        private void SetActiveButtons(Button[] buttons)
        {
            activeButtons = buttons ?? Array.Empty<Button>();
            FocusFirst();
        }

        private void SetVisible(bool visible)
        {
            if (overlayGroup == null)
            {
                return;
            }

            overlayGroup.alpha = visible ? 1f : 0f;
            overlayGroup.interactable = visible;
            overlayGroup.blocksRaycasts = visible;
        }

        private void Select(Button button)
        {
            if (eventSystem != null && button != null)
            {
                eventSystem.SetSelectedGameObject(button.gameObject);
            }
        }

        private static GameObject CreatePanel(string name, Transform parent)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            Stretch(panel.GetComponent<RectTransform>());
            return panel;
        }

        private static Image CreateImage(string name, Transform parent, Color color, bool raycast, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot)
        {
            GameObject node = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            node.transform.SetParent(parent, false);
            Image image = node.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            SetRect(node.GetComponent<RectTransform>(), anchorMin, anchorMax, position, size, pivot);
            return image;
        }

        private static Text CreateLabel(string name, Transform parent, Font font, string content, int fontSize, Color color, TextAnchor alignment, Vector2 position, Vector2 size)
        {
            GameObject node = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            node.transform.SetParent(parent, false);
            Text label = node.GetComponent<Text>();
            label.font = font;
            label.text = content;
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            Shadow shadow = node.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(1f, -1f);
            SetRect(node.GetComponent<RectTransform>(), CenterAnchor, CenterAnchor, position, size, CenterAnchor);
            return label;
        }

        private static Button CreateButton(string name, Transform parent, Font font, string label, Vector2 position, Vector2 size, Color baseColor)
        {
            Image image = CreateImage(name, parent, baseColor, true, CenterAnchor, CenterAnchor, position, size, CenterAnchor);
            Button button = image.gameObject.AddComponent<Button>();
            RawImage decoration = AddDecorativeFrame(image.transform, StandardButtonPath);
            if (decoration != null)
            {
                image.color = Color.clear;
                decoration.color = baseColor == Surface ? Color.white : baseColor;
                button.targetGraphic = decoration;
            }
            else
            {
                Outline outline = image.gameObject.AddComponent<Outline>();
                outline.effectColor = Gold;
                outline.effectDistance = Vector2.one;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Cyan;
            colors.selectedColor = Cyan;
            colors.pressedColor = Gold;
            colors.disabledColor = new Color(MutedText.r, MutedText.g, MutedText.b, 0.45f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            CreateLabel("Label", image.transform, font, label, 23, MainText, TextAnchor.MiddleCenter, Vector2.zero, size - new Vector2(14f, 4f));
            return button;
        }

        private static RawImage AddDecorativeFrame(Transform parent, string resourcePath)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            GameObject node = new GameObject("FrameArt", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            node.transform.SetParent(parent, false);
            node.transform.SetAsFirstSibling();
            RawImage image = node.GetComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = false;
            Stretch(node.GetComponent<RectTransform>());
            return image;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.pivot = pivot;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static string ToPercent(float value)
        {
            return Mathf.RoundToInt(value * 100f) + "%";
        }

        private static string OnOff(bool value)
        {
            return value ? "사용" : "사용 안 함";
        }

        private static string DisplayModeLabel(DisplayModeId mode)
        {
            switch (mode)
            {
                case DisplayModeId.Windowed: return "창 모드";
                case DisplayModeId.ExclusiveFullScreen: return "전체 화면";
                default: return "테두리 없는 전체 화면";
            }
        }

        private static string QualityLabel(QualityPresetId quality)
        {
            switch (quality)
            {
                case QualityPresetId.Low: return "낮음";
                case QualityPresetId.Medium: return "보통";
                default: return "높음";
            }
        }

        private static Color ColorFromHex(string hex, float alpha)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            color.a = alpha;
            return color;
        }
    }
}

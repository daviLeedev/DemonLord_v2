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
        private static readonly Color Paper = ColorFromHex("E7E2D8");
        private static readonly Color Iron = ColorFromHex("171A20");
        private static readonly Color Warning = ColorFromHex("B73B3B");
        private static readonly Color Focus = ColorFromHex("78BFFF");
        private static readonly Color Crimson = ColorFromHex("762431");

        private FrontendCoordinator coordinator;
        private ISceneFlowService sceneFlowService;
        private RectTransform contentRoot;
        private string selectedDifficultyId = DifficultyId.NormalValue;
        private int tutorialSelection;
        private bool titleCanSkip;

        public void Initialize(FrontendCoordinator coordinator, ISceneFlowService sceneFlowService)
        {
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.sceneFlowService = sceneFlowService ?? throw new ArgumentNullException(nameof(sceneFlowService));
            CreateCanvas();
            StartCoroutine(PlayOpeningSequence());
        }

        private IEnumerator PlayOpeningSequence()
        {
            RenderLogoNotice();
            yield return new WaitForSeconds(2.5f);
            coordinator.CompleteLogoNotice();
            titleCanSkip = true;
            RenderTitleIntro();
        }

        private void Update()
        {
            if (titleCanSkip && coordinator != null && coordinator.Screen == FrontendScreen.TitleIntro && WasAnyInputPressed())
            {
                CompleteTitleIntro();
            }
        }

        private void CreateCanvas()
        {
            GameObject cameraObject = new GameObject("FrontendBackgroundCamera", typeof(Camera));
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
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject eventSystemObject = new GameObject("FrontendEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.transform.SetParent(transform, false);
            eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            GameObject root = new GameObject("Content", typeof(RectTransform));
            root.transform.SetParent(canvasObject.transform, false);
            contentRoot = root.GetComponent<RectTransform>();
            contentRoot.anchorMin = Vector2.zero;
            contentRoot.anchorMax = Vector2.one;
            contentRoot.offsetMin = Vector2.zero;
            contentRoot.offsetMax = Vector2.zero;
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
                case FrontendScreen.ErrorDialog:
                    RenderError();
                    break;
                default:
                    RenderBusy();
                    break;
            }
        }

        private void RenderLogoNotice()
        {
            ClearContent();
            AddBackground(Iron);
            AddLabel("세계중앙국 산하 삼계조정국", new Vector2(960f, 230f), 42, Focus, TextAnchor.MiddleCenter);
            AddLabel("공식 기록 열람 승인", new Vector2(960f, 310f), 28, Paper, TextAnchor.MiddleCenter);
            AddPanel(new Vector2(960f, 580f), new Vector2(960f, 340f), new Color(0f, 0f, 0f, 0.48f));
            AddLabel("본 게임에는 섬광, 화면 흔들림, 갑작스러운 음향 및\n빠른 카메라 이동이 포함되어 있습니다.\n\n불편함이 느껴질 경우 플레이를 중단하거나,\n환경 조정의 접근성 항목에서 효과 강도를 낮춰 주십시오.\n\n저장 아이콘이 표시되는 동안 게임을 종료하지 마십시오.", new Vector2(960f, 580f), 27, Paper, TextAnchor.MiddleCenter);
        }

        private void RenderTitleIntro()
        {
            ClearContent();
            AddBackground(new Color(0.055f, 0.06f, 0.08f, 1f));
            AddPanel(new Vector2(960f, 500f), new Vector2(1100f, 560f), new Color(0f, 0f, 0f, 0.42f));
            AddLabel("AURA-RD-0417", new Vector2(960f, 300f), 25, Focus, TextAnchor.MiddleCenter);
            AddLabel("공식 기록 열람 승인", new Vector2(960f, 390f), 38, Paper, TextAnchor.MiddleCenter);
            AddButton("아무 버튼이나 누르십시오", new Vector2(960f, 760f), new Vector2(430f, 64f), CompleteTitleIntro, true);
        }

        private void RenderMainMenu()
        {
            AddBackground(Iron);
            AddLabel("공식 기록 열람", new Vector2(380f, 170f), 42, Paper, TextAnchor.MiddleLeft);
            AddLabel("마왕성 제107관리구역", new Vector2(380f, 220f), 23, Focus, TextAnchor.MiddleLeft);
            AddMenuButton("01  최근 기록 이어보기   CONTINUE", 360f, BeginContinue);
            AddMenuButton("02  신규 파견 시작       NEW GAME", 440f, BeginNewGame);
            AddMenuButton("03  보존 기록 열람       LOAD GAME", 520f, BeginLoadGame);
            AddMenuButton("04  환경 조정            SETTINGS", 600f, ShowUnavailable);
            AddMenuButton("05  기록 보관소          ARCHIVE", 680f, ShowUnavailable);
            AddMenuButton("06  업무 종료            EXIT", 760f, ExitApplication);
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
                AddButton(body, new Vector2(960f, y), new Vector2(980f, 150f), () => SelectSlot(slot.SlotId), interactable);
                y += 180f;
            }
            AddButton("뒤로", new Vector2(220f, 960f), new Vector2(220f, 56f), Back, true);
        }

        private void RenderNewGameSetup()
        {
            AddBackground(Iron);
            AddLabel("신규 파견 등록", new Vector2(960f, 115f), 40, Paper, TextAnchor.MiddleCenter);
            AddLabel("담당자: 신입 세무관   /   파견 지역: 마왕성 제107관리구역", new Vector2(960f, 165f), 22, Focus, TextAnchor.MiddleCenter);
            AddLabel("난이도 선택", new Vector2(510f, 285f), 30, Paper, TextAnchor.MiddleCenter);
            AddDifficultyButton("기록 열람  STORY", DifficultyId.StoryValue, 390f);
            AddDifficultyButton("정규 파견  NORMAL", DifficultyId.NormalValue, 490f);
            AddDifficultyButton("특별 압류  HARD", DifficultyId.HardValue, 590f);
            AddLabel("튜토리얼", new Vector2(1370f, 285f), 30, Paper, TextAnchor.MiddleCenter);
            AddTutorialButton("상세 안내", 0, 390f);
            AddTutorialButton("핵심 안내", 1, 490f);
            AddTutorialButton("사용 안 함", 2, 590f);
            AddButton("파견 승인", new Vector2(1240f, 850f), new Vector2(300f, 64f), CreateNewGame, true);
            AddButton("뒤로", new Vector2(680f, 850f), new Vector2(300f, 64f), Back, true);
        }

        private void RenderOverwriteConfirm()
        {
            AddBackground(Iron);
            AddPanel(new Vector2(960f, 520f), new Vector2(960f, 460f), new Color(0f, 0f, 0f, 0.55f));
            AddLabel("기존 기록 덮어쓰기", new Vector2(960f, 355f), 40, Warning, TextAnchor.MiddleCenter);
            AddLabel("이 슬롯에는 기존 보존 기록이 있습니다.\n\n신규 파견을 시작하면 해당 기록을 덮어쓰며,\n덮어쓴 기록은 복구할 수 없습니다.\n\n정말 신규 파견을 시작하시겠습니까?", new Vector2(960f, 535f), 27, Paper, TextAnchor.MiddleCenter);
            AddButton("기존 기록 덮어쓰기", new Vector2(700f, 760f), new Vector2(360f, 62f), () => ConfirmOverwrite(true), true);
            AddButton("취소", new Vector2(1220f, 760f), new Vector2(220f, 62f), () => ConfirmOverwrite(false), true);
        }

        private void RenderError()
        {
            AddBackground(Iron);
            AddLabel("기록 처리 오류", new Vector2(960f, 420f), 42, Warning, TextAnchor.MiddleCenter);
            AddLabel("처리할 수 없는 기록입니다.\n오류 코드: " + coordinator.ErrorCode, new Vector2(960f, 530f), 27, Paper, TextAnchor.MiddleCenter);
            AddButton("돌아가기", new Vector2(960f, 680f), new Vector2(280f, 62f), Back, true);
        }

        private void RenderBusy()
        {
            AddBackground(Iron);
            AddLabel("공식 기록을 처리하고 있습니다…", new Vector2(960f, 540f), 34, Paper, TextAnchor.MiddleCenter);
        }

        private void BeginContinue()
        {
            coordinator.OpenStartMode();
            coordinator.OpenContinueSlots();
            foreach (SaveSlotSummary summary in coordinator.Slots)
            {
                if (summary.CanLoad)
                {
                    HandleCommand(coordinator.SelectSlot(summary.SlotId));
                    return;
                }
            }
            Render();
        }

        private void BeginNewGame()
        {
            coordinator.OpenStartMode();
            HandleCommand(coordinator.OpenNewGameSlots());
        }

        private void BeginLoadGame()
        {
            coordinator.OpenStartMode();
            HandleCommand(coordinator.OpenContinueSlots());
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
            bool tutorialEnabled = tutorialSelection != 2;
            if (!NewGameSettings.TryCreate("세무관", selectedDifficultyId, tutorialEnabled, out NewGameSettings settings, out string errorCode))
            {
                return;
            }

            HandleCommand(coordinator.CreateSelectedNewGame(settings, UnityEngine.Application.version));
        }

        private void CompleteTitleIntro()
        {
            titleCanSkip = false;
            coordinator.CompleteTitleIntro();
            Render();
        }

        private void Back()
        {
            coordinator.Back();
            Render();
        }

        private void HandleCommand(FrontendCommandResult result)
        {
            if (result.HasEntryDestination)
            {
                StartCoroutine(LoadEntry(result.Destination));
                return;
            }

            Render();
        }

        private IEnumerator LoadEntry(EntryDestination destination)
        {
            RenderBusy();
            Task task = sceneFlowService.LoadEntryAsync(destination);
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                Debug.LogException(task.Exception, this);
            }
        }

        private void AddDifficultyButton(string label, string difficultyId, float y)
        {
            string displayLabel = (selectedDifficultyId == difficultyId ? "▶ " : "   ") + label;
            AddButton(displayLabel, new Vector2(510f, y), new Vector2(500f, 72f), () => { selectedDifficultyId = difficultyId; Render(); }, true);
        }

        private void AddTutorialButton(string label, int value, float y)
        {
            string displayLabel = (tutorialSelection == value ? "▶ " : "   ") + label;
            AddButton(displayLabel, new Vector2(1370f, y), new Vector2(380f, 72f), () => { tutorialSelection = value; Render(); }, true);
        }

        private void AddMenuButton(string label, float y, UnityEngine.Events.UnityAction action)
        {
            AddButton(label, new Vector2(540f, y), new Vector2(620f, 64f), action, true);
        }

        private void ShowUnavailable()
        {
            Debug.Log("This menu item is planned but not implemented yet.", this);
        }

        private void ExitApplication()
        {
            UnityEngine.Application.Quit();
        }

        private string BuildSlotText(SaveSlotSummary slot, bool isLoad)
        {
            if (slot.State == SaveSlotState.Empty)
            {
                return slot.SlotId.Value.ToUpperInvariant() + "\n빈 기록  |  이 슬롯에서 신규 파견을 시작할 수 있습니다.";
            }

            if (slot.State == SaveSlotState.Valid)
            {
                return slot.SlotId.Value.ToUpperInvariant() + "\n" + slot.ProfileName + "  |  " + slot.DifficultyId + "  |  " + slot.UpdatedAtUtc.Value.ToString("yyyy.MM.dd HH:mm");
            }

            return slot.SlotId.Value.ToUpperInvariant() + "\n" + (slot.State == SaveSlotState.Corrupt ? "손상된 기록" : "호환되지 않는 기록") + (isLoad ? "  |  열람 불가" : "");
        }

        private void ClearContent()
        {
            if (contentRoot == null)
            {
                return;
            }

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

        private void AddPanel(Vector2 position, Vector2 size, Color color)
        {
            GameObject panel = new GameObject("Panel", typeof(Image));
            panel.transform.SetParent(contentRoot, false);
            ConfigureRect(panel.GetComponent<RectTransform>(), position, size);
            panel.GetComponent<Image>().color = color;
        }

        private void AddLabel(string value, Vector2 position, int fontSize, Color color, TextAnchor alignment)
        {
            GameObject label = new GameObject("Label", typeof(Text));
            label.transform.SetParent(contentRoot, false);
            ConfigureRect(label.GetComponent<RectTransform>(), position, new Vector2(1500f, 180f));
            Text text = label.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void AddButton(string value, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, bool interactable)
        {
            GameObject buttonObject = new GameObject("Button", typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(contentRoot, false);
            ConfigureRect(buttonObject.GetComponent<RectTransform>(), position, size);
            Image image = buttonObject.GetComponent<Image>();
            image.color = interactable ? new Color(Crimson.r, Crimson.g, Crimson.b, 0.68f) : new Color(0.25f, 0.25f, 0.25f, 0.6f);
            Button button = buttonObject.GetComponent<Button>();
            button.interactable = interactable;
            button.onClick.AddListener(action);
            AddButtonLabel(buttonObject.transform, value, interactable ? Paper : new Color(Paper.r, Paper.g, Paper.b, 0.4f));
        }

        private static void AddButtonLabel(Transform parent, string value, Color color)
        {
            GameObject label = new GameObject("Text", typeof(Text));
            label.transform.SetParent(parent, false);
            RectTransform rect = label.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(18f, 6f);
            rect.offsetMax = new Vector2(-18f, -6f);
            Text text = label.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.color = color;
            text.alignment = TextAnchor.MiddleLeft;
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
    }
}

using System;
using DemonLord.Application;
using UnityEngine;
using UnityEngine.UI;

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    public sealed class BattlePreparationView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text detailLabel;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Button dispatchButton;
        [SerializeField] private Button closeButton;

        public event Action DispatchRequested;
        public event Action CloseRequested;

        public bool IsVisible => rootGroup != null && rootGroup.alpha > 0.5f;

        public void Configure(
            CanvasGroup configuredRootGroup,
            Text configuredTitleLabel,
            Text configuredDetailLabel,
            Text configuredStatusLabel,
            Button configuredDispatchButton,
            Button configuredCloseButton)
        {
            rootGroup = configuredRootGroup;
            titleLabel = configuredTitleLabel;
            detailLabel = configuredDetailLabel;
            statusLabel = configuredStatusLabel;
            dispatchButton = configuredDispatchButton;
            closeButton = configuredCloseButton;
            Hide();
        }

        private void Awake()
        {
            dispatchButton?.onClick.AddListener(NotifyDispatch);
            closeButton?.onClick.AddListener(NotifyClose);
            Hide();
        }

        private void OnDestroy()
        {
            dispatchButton?.onClick.RemoveListener(NotifyDispatch);
            closeButton?.onClick.RemoveListener(NotifyClose);
        }

        public void Show(BattleLaunchRequest request)
        {
            if (request == null || rootGroup == null) return;
            ApplyConfirmationLayout();
            if (titleLabel != null) titleLabel.text = "모의전투를 시작하시겠습니까?";
            SetButtonLabel(dispatchButton, "시작");
            SetButtonLabel(closeButton, "그만두기");
            if (detailLabel != null)
            {
                detailLabel.text = "전투 대응 집행관이 준비한 모의전투를 진행합니다."
                    + "\n전투 화면에서 아군의 기술과 행동 순서를 선택할 수 있습니다.";
            }

            SetStatus("시작하면 전투 화면으로 이동합니다.");
            rootGroup.alpha = 1f;
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;
        }

        public void SetStatus(string message)
        {
            if (statusLabel != null) statusLabel.text = message ?? string.Empty;
        }

        public void Hide()
        {
            if (rootGroup == null) return;
            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }

        private void NotifyDispatch() => DispatchRequested?.Invoke();
        private void NotifyClose() => CloseRequested?.Invoke();

        private static void SetButtonLabel(Button button, string text)
        {
            if (button == null) return;

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null) label.text = text;
        }

        private void ApplyConfirmationLayout()
        {
            SetCardElementRect(titleLabel?.rectTransform, new Vector2(390f, 402f), new Vector2(680f, 54f));
            SetCardElementRect(detailLabel?.rectTransform, new Vector2(390f, 286f), new Vector2(650f, 142f));
            SetCardElementRect(statusLabel?.rectTransform, new Vector2(390f, 132f), new Vector2(660f, 42f));
            SetCardElementRect(dispatchButton?.GetComponent<RectTransform>(), new Vector2(205f, 60f), new Vector2(270f, 68f));
            SetCardElementRect(closeButton?.GetComponent<RectTransform>(), new Vector2(575f, 60f), new Vector2(270f, 68f));
        }

        private static void SetCardElementRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null) return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}

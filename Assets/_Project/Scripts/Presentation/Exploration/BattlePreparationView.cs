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
            if (titleLabel != null) titleLabel.text = "현장 출동 준비";
            if (detailLabel != null)
            {
                detailLabel.text = "조정 대상: " + request.EnemyGroupId
                    + "\n전투 코드: " + request.BattleId
                    + "\n복귀 위치: " + request.ReturnLocation.AreaId.Value + " / " + request.ReturnLocation.SpawnId.Value;
            }

            SetStatus("전투 시스템 연결 준비가 완료되었습니다.");
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
    }
}

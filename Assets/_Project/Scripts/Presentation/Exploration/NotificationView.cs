using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0649 // Serialized Unity references are assigned by the scene builder/inspector.

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    public sealed class NotificationView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text messageLabel;
        [SerializeField, Min(0.1f)] private float displaySeconds = 1.8f;
        [SerializeField, Min(0.01f)] private float fadeSeconds = 0.2f;

        private float remainingSeconds;

        public bool IsVisible => remainingSeconds > 0f;

        private void Awake()
        {
            HideImmediate();
        }

        private void Update()
        {
            if (remainingSeconds <= 0f)
            {
                return;
            }

            remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.unscaledDeltaTime);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = remainingSeconds >= fadeSeconds
                    ? 1f
                    : Mathf.Clamp01(remainingSeconds / fadeSeconds);
            }
        }

        private void OnDisable()
        {
            HideImmediate();
        }

        public void Show(string message)
        {
            if (messageLabel != null)
            {
                messageLabel.text = message ?? string.Empty;
            }

            remainingSeconds = displaySeconds;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void HideImmediate()
        {
            remainingSeconds = 0f;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }
    }
}

#pragma warning restore CS0649

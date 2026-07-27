using UnityEngine;
using UnityEngine.UI;

namespace DemonLord.Presentation.Exploration
{
    public sealed class InteractionPromptView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text targetLabel = null;
        [SerializeField] private Text promptLabel = null;

        public bool IsVisible { get; private set; }

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            Hide();
        }

        private void OnDisable()
        {
            Hide();
        }

        public void Show(IExplorationInteractable interactable)
        {
            if (interactable == null)
            {
                Hide();
                return;
            }

            if (targetLabel != null)
            {
                targetLabel.text = interactable.DisplayName;
            }

            if (promptLabel != null)
            {
                promptLabel.text = "F " + interactable.ActionLabel;
            }

            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            IsVisible = visible;
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}

using System.Threading.Tasks;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    public sealed class ScreenFadeView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;

        public void Configure(CanvasGroup configuredCanvasGroup)
        {
            canvasGroup = configuredCanvasGroup;
            SetImmediate(0f);
        }

        public void SetImmediate(float alpha)
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = Mathf.Clamp01(alpha);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = canvasGroup.alpha > 0.001f;
        }

        public async Task FadeToAsync(float targetAlpha, float seconds)
        {
            if (canvasGroup == null) return;
            float start = canvasGroup.alpha;
            float duration = Mathf.Max(0.001f, seconds);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetImmediate(Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(elapsed / duration)));
                await Task.Yield();
            }

            SetImmediate(targetAlpha);
        }
    }
}

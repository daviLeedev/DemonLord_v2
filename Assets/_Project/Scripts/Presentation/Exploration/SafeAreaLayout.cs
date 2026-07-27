using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    public sealed class SafeAreaLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform safeAreaRoot = null;
        private Rect lastSafeArea;

        public void Configure(RectTransform configuredSafeAreaRoot)
        {
            safeAreaRoot = configuredSafeAreaRoot;
            ApplyIfChanged(true);
        }

        private void OnEnable()
        {
            ApplyIfChanged(true);
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyIfChanged(false);
        }

        private void Update()
        {
            ApplyIfChanged(false);
        }

        private void ApplyIfChanged(bool force)
        {
            if (safeAreaRoot == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            if (!force && safeArea == lastSafeArea)
            {
                return;
            }

            lastSafeArea = safeArea;
            safeAreaRoot.anchorMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
            safeAreaRoot.anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
        }
    }
}

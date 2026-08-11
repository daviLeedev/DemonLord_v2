using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    public sealed class WorldInteractionIndicator : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Renderer indicatorRenderer;
        [SerializeField] private PrototypeInteractable interactable;
        [SerializeField] private LabDoorController door;
        [SerializeField] private Vector3 restLocalPosition;
        [SerializeField] private Color normalColor = new Color(0.40f, 0.78f, 0.91f, 1f);
        [SerializeField] private Color objectiveColor = new Color(0.95f, 0.72f, 0.24f, 1f);
        [SerializeField] private Color lockedColor = new Color(0.76f, 0.20f, 0.25f, 1f);
        [SerializeField, Min(0f)] private float bobDistance = 0.08f;
        [SerializeField, Min(0f)] private float bobSpeed = 2.2f;

        private MaterialPropertyBlock propertyBlock;
        private bool objectiveTarget;

        public bool IsObjectiveTarget => objectiveTarget;

        public void Configure(
            Transform configuredVisualRoot,
            Renderer configuredRenderer,
            PrototypeInteractable configuredInteractable,
            LabDoorController configuredDoor,
            Vector3 configuredRestLocalPosition)
        {
            visualRoot = configuredVisualRoot;
            indicatorRenderer = configuredRenderer;
            interactable = configuredInteractable;
            door = configuredDoor;
            restLocalPosition = configuredRestLocalPosition;
            ApplyVisualState();
        }

        public void SetObjectiveTarget(bool value)
        {
            if (objectiveTarget == value) return;
            objectiveTarget = value;
            ApplyVisualState();
        }

        private void Update()
        {
            if (visualRoot != null)
            {
                float offset = Mathf.Sin(Time.unscaledTime * bobSpeed) * bobDistance;
                visualRoot.localPosition = restLocalPosition + Vector3.up * offset;
                visualRoot.Rotate(Vector3.up, 45f * Time.unscaledDeltaTime, Space.Self);
            }

            ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            if (indicatorRenderer == null) return;
            propertyBlock ??= new MaterialPropertyBlock();
            bool available = (interactable != null && interactable.CanInteract)
                || (door != null && door.CanInteract);
            indicatorRenderer.enabled = available;
            if (!available) return;

            Color color = objectiveTarget
                ? objectiveColor
                : door != null && door.IsLocked
                    ? lockedColor
                    : normalColor;
            indicatorRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", color);
            propertyBlock.SetColor("_Color", color);
            indicatorRenderer.SetPropertyBlock(propertyBlock);
            float scale = objectiveTarget ? 1.22f : 1f;
            if (visualRoot != null) visualRoot.localScale = Vector3.one * scale;
        }
    }
}

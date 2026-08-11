using DemonLord.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    public sealed class InGameHudView : MonoBehaviour
    {
        [SerializeField] private Text areaLabel = null;
        [SerializeField] private Text roomLabel = null;
        [SerializeField] private Text currencyValueLabel = null;
        [SerializeField] private Text timeValueLabel = null;
        [SerializeField] private CanvasGroup objectiveGroup = null;
        [SerializeField] private Text objectiveStateLabel = null;
        [SerializeField] private Text objectiveLabel = null;

        private IInGameHudStateSource stateSource;
        private LabProgressController objectiveSource;
        private float objectivePulseRemaining;

        public void Configure(
            Text configuredAreaLabel,
            Text configuredRoomLabel,
            Text configuredCurrencyValueLabel,
            Text configuredTimeValueLabel,
            CanvasGroup configuredObjectiveGroup = null,
            Text configuredObjectiveStateLabel = null,
            Text configuredObjectiveLabel = null)
        {
            areaLabel = configuredAreaLabel;
            roomLabel = configuredRoomLabel;
            currencyValueLabel = configuredCurrencyValueLabel;
            timeValueLabel = configuredTimeValueLabel;
            objectiveGroup = configuredObjectiveGroup;
            objectiveStateLabel = configuredObjectiveStateLabel;
            objectiveLabel = configuredObjectiveLabel;
        }

        public void BindObjectiveSource(LabProgressController configuredSource)
        {
            DisconnectObjective();
            objectiveSource = configuredSource;
            if (objectiveSource == null) return;
            objectiveSource.ObjectiveChanged += RenderObjective;
            RenderObjective(objectiveSource.CurrentObjective);
        }

        private void Update()
        {
            if (objectiveGroup == null || objectivePulseRemaining <= 0f) return;
            objectivePulseRemaining = Mathf.Max(0f, objectivePulseRemaining - Time.unscaledDeltaTime);
            float pulse = 1f + Mathf.Sin((0.65f - objectivePulseRemaining) * 18f) * 0.025f;
            objectiveGroup.transform.localScale = Vector3.one * pulse;
            if (objectivePulseRemaining <= 0f) objectiveGroup.transform.localScale = Vector3.one;
        }

        public void Initialize(IInGameHudStateSource configuredStateSource)
        {
            Disconnect();
            stateSource = configuredStateSource;
            if (stateSource == null)
            {
                return;
            }

            stateSource.Changed += Render;
            Render(stateSource.Current);
        }

        private void OnDisable()
        {
            Disconnect();
            DisconnectObjective();
        }

        private void OnDestroy()
        {
            Disconnect();
            DisconnectObjective();
        }

        private void Render(InGameHudState state)
        {
            if (areaLabel != null)
            {
                areaLabel.text = state.AreaName;
            }

            if (roomLabel != null)
            {
                roomLabel.text = state.RoomName;
            }

            if (currencyValueLabel != null)
            {
                currencyValueLabel.text = state.HasCurrency ? state.Currency.ToString("N0") : "—";
            }

            if (timeValueLabel != null)
            {
                timeValueLabel.text = state.HasGameTime
                    ? (state.Day > 0
                        ? string.Format("D{0}  {1:00}:{2:00}", state.Day, state.Hour, state.Minute)
                        : string.Format("{0:00}:{1:00}", state.Hour, state.Minute))
                    : "--:--";
            }
        }

        private void Disconnect()
        {
            if (stateSource != null)
            {
                stateSource.Changed -= Render;
                stateSource = null;
            }
        }

        private void RenderObjective(LabObjectiveState state)
        {
            if (objectiveStateLabel != null)
            {
                objectiveStateLabel.text = state.IsComplete ? "✓" : "◆";
                objectiveStateLabel.color = state.IsComplete
                    ? new Color(0.42f, 0.86f, 0.58f, 1f)
                    : new Color(0.95f, 0.72f, 0.24f, 1f);
            }

            if (objectiveLabel != null) objectiveLabel.text = state.Title;
            if (objectiveGroup != null)
            {
                objectiveGroup.alpha = 1f;
                objectiveGroup.transform.localScale = Vector3.one;
            }

            objectivePulseRemaining = 0.65f;
        }

        private void DisconnectObjective()
        {
            if (objectiveSource != null)
            {
                objectiveSource.ObjectiveChanged -= RenderObjective;
                objectiveSource = null;
            }
        }
    }
}

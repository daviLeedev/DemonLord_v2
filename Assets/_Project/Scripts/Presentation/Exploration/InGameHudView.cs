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

        private IInGameHudStateSource stateSource;

        public void Configure(Text configuredAreaLabel, Text configuredRoomLabel, Text configuredCurrencyValueLabel, Text configuredTimeValueLabel)
        {
            areaLabel = configuredAreaLabel;
            roomLabel = configuredRoomLabel;
            currencyValueLabel = configuredCurrencyValueLabel;
            timeValueLabel = configuredTimeValueLabel;
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
        }

        private void OnDestroy()
        {
            Disconnect();
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
    }
}

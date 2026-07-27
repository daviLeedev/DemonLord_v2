using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    public interface IExplorationInteractable
    {
        string StableId { get; }

        string DisplayName { get; }

        string ActionLabel { get; }

        Transform FocusPoint { get; }

        Transform RootTransform { get; }

        bool CanInteract { get; }

        void SetSelected(bool selected);

        bool TryInteract(InteractionSensor sensor);
    }
}

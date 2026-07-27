using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace DemonLord.Presentation.Exploration
{
    /// <summary>
    /// Keeps the editor authoring assembly independent from the Input System UI implementation
    /// while providing pointer support for the in-game pause menu at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InputSystemUiBootstrap : MonoBehaviour
    {
        [SerializeField] private EventSystem eventSystem = null;

        public void Configure(EventSystem configuredEventSystem)
        {
            eventSystem = configuredEventSystem;
        }

        private void Awake()
        {
            if (eventSystem == null)
            {
                eventSystem = GetComponent<EventSystem>();
            }

            if (eventSystem == null)
            {
                return;
            }

            InputSystemUIInputModule module = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (module == null)
            {
                module = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            module.AssignDefaultActions();
        }
    }
}

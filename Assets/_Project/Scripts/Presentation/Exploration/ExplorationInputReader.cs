using UnityEngine;
using UnityEngine.InputSystem;

namespace DemonLord.Presentation.Exploration
{
    public sealed class ExplorationInputReader : MonoBehaviour
    {
        private const int MaximumQueuedCameraSteps = 32;

        private InputActionMap actionMap;
        private InputAction moveAction;
        private InputAction sprintAction;
        private InputAction dashAction;
        private InputAction interactAction;
        private InputAction cameraRotateLeftAction;
        private InputAction cameraRotateRightAction;
        private InputAction cameraZoomAction;
        private InputAction confirmAction;
        private InputAction pauseAction;
        private InputAction backAction;
        private InputAction menuNavigateAction;
        private InputAction menuSubmitAction;
        private InputAction mapAction;
        private InputAction mapFloorPreviousAction;
        private InputAction mapFloorNextAction;
        private InputAction mapZoomAction;

        private bool subscribed;
        private bool dashPressed;
        private bool interactPressed;
        private bool confirmPressed;
        private bool pausePressed;
        private bool backPressed;
        private bool menuSubmitPressed;
        private bool mapPressed;
        private bool mapFloorPreviousPressed;
        private bool mapFloorNextPressed;
        private float mapZoomDelta;
        private Vector2 menuNavigation;
        private int cameraRotationSteps;
        private float zoomDelta;

        public ExplorationInputGate Gate { get; } = new ExplorationInputGate();

        public Vector2 Move
        {
            get
            {
                if (!IsMapEnabled || Gate.IsBlocked(ExplorationInputChannel.Movement))
                {
                    return Vector2.zero;
                }

                return Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
            }
        }

        public bool SprintHeld => IsMapEnabled
            && Gate.IsAllowed(ExplorationInputChannel.Movement)
            && sprintAction.IsPressed();

        public bool IsMapEnabled => actionMap != null && actionMap.enabled;

        public bool ConsumeDashPressed()
        {
            return Consume(ref dashPressed, ExplorationInputChannel.Dash);
        }

        public bool ConsumeInteractPressed()
        {
            return Consume(ref interactPressed, ExplorationInputChannel.Interaction);
        }

        public bool ConsumeConfirmPressed()
        {
            return Consume(ref confirmPressed, ExplorationInputChannel.Dialogue);
        }

        /// <summary>
        /// Gamepad B/East only. Keyboard Escape is intentionally owned by the top-level
        /// in-game UI coordinator through <see cref="ConsumePausePressed"/>.
        /// </summary>
        public bool ConsumeBackPressed()
        {
            bool value = backPressed;
            backPressed = false;
            return value;
        }

        public bool ConsumePausePressed()
        {
            bool value = pausePressed;
            pausePressed = false;
            return value;
        }

        public bool ConsumeMenuSubmitPressed()
        {
            bool value = menuSubmitPressed;
            menuSubmitPressed = false;
            return value;
        }

        public bool ConsumeMapPressed()
        {
            bool value = mapPressed;
            mapPressed = false;
            return value;
        }

        public int ConsumeMapFloorStep()
        {
            int value = (mapFloorNextPressed ? 1 : 0) - (mapFloorPreviousPressed ? 1 : 0);
            mapFloorPreviousPressed = false;
            mapFloorNextPressed = false;
            return value;
        }

        public float ConsumeMapZoomDelta()
        {
            float value = mapZoomDelta;
            mapZoomDelta = 0f;
            return value;
        }

        public int ConsumeMenuNavigationStep()
        {
            float y = menuNavigation.y;
            menuNavigation = Vector2.zero;
            mapPressed = false;
            mapFloorPreviousPressed = false;
            mapFloorNextPressed = false;
            mapZoomDelta = 0f;
            if (y > 0.5f)
            {
                return 1;
            }

            return y < -0.5f ? -1 : 0;
        }

        /// <summary>
        /// Drops only one-shot dialogue inputs. This is used when a conversation opens so an
        /// the F that opened the conversation cannot affect its first visible frame. Movement,
        /// dash, interaction, camera rotation and zoom are
        /// deliberately preserved under their own consume policies.
        /// </summary>
        public void ClearPendingDialogueInput()
        {
            confirmPressed = false;
        }

        /// <summary>
        /// One-shot menu input is separate from exploration gates and must be explicitly cleared
        /// whenever the pause UI opens, closes or handles an Escape priority action.
        /// </summary>
        public void ClearPendingMenuInput()
        {
            pausePressed = false;
            backPressed = false;
            menuSubmitPressed = false;
            menuNavigation = Vector2.zero;
        }

        public int ConsumeCameraRotationSteps()
        {
            int value = cameraRotationSteps;
            cameraRotationSteps = 0;
            return Gate.IsAllowed(ExplorationInputChannel.Camera) ? value : 0;
        }

        public float ConsumeZoomDelta()
        {
            float value = zoomDelta;
            zoomDelta = 0f;
            return Gate.IsAllowed(ExplorationInputChannel.Camera) ? value : 0f;
        }

        private void Awake()
        {
            CreateActions();
        }

        private void OnEnable()
        {
            CreateActions();
            Subscribe();
            actionMap.Enable();
        }

        private void OnDisable()
        {
            if (actionMap != null)
            {
                actionMap.Disable();
            }

            Unsubscribe();
            ClearPendingInput();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (actionMap != null)
            {
                actionMap.Dispose();
                actionMap = null;
            }
        }

        private void CreateActions()
        {
            if (actionMap != null)
            {
                return;
            }

            actionMap = new InputActionMap("Exploration");

            moveAction = actionMap.AddAction(
                "Move",
                InputActionType.Value,
                expectedControlLayout: "Vector2");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            sprintAction = AddButtonAction("Sprint", "<Keyboard>/leftShift", false);
            dashAction = AddButtonAction("Dash", "<Keyboard>/space", true);
            interactAction = AddButtonAction("Interact", "<Keyboard>/f", true);
            cameraRotateLeftAction = AddButtonAction("CameraRotateLeft", "<Keyboard>/q", true);
            cameraRotateRightAction = AddButtonAction("CameraRotateRight", "<Keyboard>/e", true);

            cameraZoomAction = actionMap.AddAction(
                "CameraZoom",
                InputActionType.Value,
                expectedControlLayout: "Axis");
            cameraZoomAction.AddBinding("<Mouse>/scroll/y");

            confirmAction = AddButtonAction("Confirm", "<Keyboard>/enter", true);
            confirmAction.AddBinding("<Keyboard>/numpadEnter");
            confirmAction.AddBinding("<Keyboard>/f");
            pauseAction = AddButtonAction("Pause", "<Keyboard>/escape", true);
            pauseAction.AddBinding("<Gamepad>/start");
            backAction = AddButtonAction("Back", "<Gamepad>/buttonEast", true);
            menuSubmitAction = AddButtonAction("MenuSubmit", "<Keyboard>/enter", true);
            menuSubmitAction.AddBinding("<Keyboard>/space");
            menuSubmitAction.AddBinding("<Gamepad>/buttonSouth");
            menuNavigateAction = actionMap.AddAction(
                "MenuNavigate",
                InputActionType.Value,
                expectedControlLayout: "Vector2");
            menuNavigateAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            menuNavigateAction.AddBinding("<Gamepad>/dpad");
            menuNavigateAction.AddBinding("<Gamepad>/leftStick");

            mapAction = AddButtonAction("Map", "<Keyboard>/m", true);
            mapAction.AddBinding("<Gamepad>/select");
            mapFloorPreviousAction = AddButtonAction("MapFloorPrevious", "<Keyboard>/q", true);
            mapFloorPreviousAction.AddBinding("<Gamepad>/leftShoulder");
            mapFloorNextAction = AddButtonAction("MapFloorNext", "<Keyboard>/e", true);
            mapFloorNextAction.AddBinding("<Gamepad>/rightShoulder");
            mapZoomAction = actionMap.AddAction(
                "MapZoom",
                InputActionType.Value,
                expectedControlLayout: "Axis");
            mapZoomAction.AddBinding("<Mouse>/scroll/y");
        }

        private InputAction AddButtonAction(string name, string bindingPath, bool pressOnly)
        {
            InputAction action = actionMap.AddAction(
                name,
                InputActionType.Button,
                interactions: pressOnly ? "Press" : null,
                expectedControlLayout: "Button");
            action.AddBinding(bindingPath);
            return action;
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            dashAction.performed += OnDashPerformed;
            interactAction.performed += OnInteractPerformed;
            cameraRotateLeftAction.performed += OnCameraRotateLeftPerformed;
            cameraRotateRightAction.performed += OnCameraRotateRightPerformed;
            cameraZoomAction.performed += OnCameraZoomPerformed;
            confirmAction.performed += OnConfirmPerformed;
            pauseAction.performed += OnPausePerformed;
            backAction.performed += OnBackPerformed;
            menuSubmitAction.performed += OnMenuSubmitPerformed;
            menuNavigateAction.performed += OnMenuNavigatePerformed;
            mapAction.performed += OnMapPerformed;
            mapFloorPreviousAction.performed += OnMapFloorPreviousPerformed;
            mapFloorNextAction.performed += OnMapFloorNextPerformed;
            mapZoomAction.performed += OnMapZoomPerformed;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            dashAction.performed -= OnDashPerformed;
            interactAction.performed -= OnInteractPerformed;
            cameraRotateLeftAction.performed -= OnCameraRotateLeftPerformed;
            cameraRotateRightAction.performed -= OnCameraRotateRightPerformed;
            cameraZoomAction.performed -= OnCameraZoomPerformed;
            confirmAction.performed -= OnConfirmPerformed;
            pauseAction.performed -= OnPausePerformed;
            backAction.performed -= OnBackPerformed;
            menuSubmitAction.performed -= OnMenuSubmitPerformed;
            menuNavigateAction.performed -= OnMenuNavigatePerformed;
            mapAction.performed -= OnMapPerformed;
            mapFloorPreviousAction.performed -= OnMapFloorPreviousPerformed;
            mapFloorNextAction.performed -= OnMapFloorNextPerformed;
            mapZoomAction.performed -= OnMapZoomPerformed;
            subscribed = false;
        }

        private void OnDashPerformed(InputAction.CallbackContext context)
        {
            if (Gate.IsAllowed(ExplorationInputChannel.Dash))
            {
                dashPressed = true;
            }
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            if (Gate.IsAllowed(ExplorationInputChannel.Interaction))
            {
                interactPressed = true;
            }
        }

        private void OnCameraRotateLeftPerformed(InputAction.CallbackContext context)
        {
            QueueCameraRotation(-1);
        }

        private void OnCameraRotateRightPerformed(InputAction.CallbackContext context)
        {
            QueueCameraRotation(1);
        }

        private void OnCameraZoomPerformed(InputAction.CallbackContext context)
        {
            if (Gate.IsAllowed(ExplorationInputChannel.Camera))
            {
                zoomDelta += context.ReadValue<float>();
            }
        }

        private void OnConfirmPerformed(InputAction.CallbackContext context)
        {
            if (Gate.IsAllowed(ExplorationInputChannel.Dialogue))
            {
                confirmPressed = true;
            }
        }

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            pausePressed = true;
        }

        private void OnBackPerformed(InputAction.CallbackContext context)
        {
            backPressed = true;
        }

        private void OnMenuSubmitPerformed(InputAction.CallbackContext context)
        {
            menuSubmitPressed = true;
        }

        private void OnMenuNavigatePerformed(InputAction.CallbackContext context)
        {
            menuNavigation = context.ReadValue<Vector2>();
        }

        private void OnMapPerformed(InputAction.CallbackContext context)
        {
            mapPressed = true;
        }

        private void OnMapFloorPreviousPerformed(InputAction.CallbackContext context)
        {
            mapFloorPreviousPressed = true;
        }

        private void OnMapFloorNextPerformed(InputAction.CallbackContext context)
        {
            mapFloorNextPressed = true;
        }

        private void OnMapZoomPerformed(InputAction.CallbackContext context)
        {
            mapZoomDelta += context.ReadValue<float>();
        }

        private void QueueCameraRotation(int step)
        {
            if (Gate.IsBlocked(ExplorationInputChannel.Camera))
            {
                return;
            }

            cameraRotationSteps = Mathf.Clamp(
                cameraRotationSteps + step,
                -MaximumQueuedCameraSteps,
                MaximumQueuedCameraSteps);
        }

        private bool Consume(ref bool pending, ExplorationInputChannel channel)
        {
            bool value = pending;
            pending = false;
            return value && Gate.IsAllowed(channel);
        }

        private void ClearPendingInput()
        {
            dashPressed = false;
            interactPressed = false;
            ClearPendingDialogueInput();
            ClearPendingMenuInput();
            cameraRotationSteps = 0;
            zoomDelta = 0f;
        }
    }
}

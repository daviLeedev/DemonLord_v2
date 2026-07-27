using System;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private ExplorationInputReader inputReader;
        [SerializeField] private QuarterViewCameraRig cameraRig;
        [SerializeField] private PlayerFacing playerFacing;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float walkSpeed = 3f;
        [SerializeField, Min(0f)] private float sprintSpeed = 5.5f;
        [SerializeField] private float gravity = -25f;
        [SerializeField, Min(0f)] private float groundedStickSpeed = 2f;

        [Header("Dash")]
        [SerializeField, Min(0f)] private float dashDistance = 3f;
        [SerializeField, Min(0.01f)] private float dashDuration = 0.18f;
        [SerializeField, Min(0f)] private float dashCooldown = 0.65f;

        [Header("Character Controller")]
        [SerializeField, Range(0f, 90f)] private float slopeLimit = 45f;
        [SerializeField, Min(0f)] private float stepOffset = 0.3f;

        [Header("Ground Safety")]
        [SerializeField] private bool enforceMinimumWorldY;
        [SerializeField] private float minimumWorldY;

        private readonly DashRuntimeState dashState = new DashRuntimeState();
        private float verticalVelocity;

        public Vector3 CurrentHorizontalVelocity { get; private set; }

        public bool IsDashing => dashState.IsActive;

        public CollisionFlags LastCollisionFlags { get; private set; }

        public CharacterController CharacterController => characterController;

        private void Awake()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            ApplyControllerTuning();
        }

        private void OnDisable()
        {
            dashState.Reset();
            CurrentHorizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }

        private void OnValidate()
        {
            gravity = -Mathf.Abs(gravity);
            sprintSpeed = Mathf.Max(sprintSpeed, walkSpeed);
            if (characterController != null)
            {
                ApplyControllerTuning();
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime, Time.time);
        }

        public void Initialize(
            ExplorationInputReader explorationInputReader,
            QuarterViewCameraRig quarterViewCameraRig,
            PlayerFacing facing)
        {
            inputReader = explorationInputReader != null
                ? explorationInputReader
                : throw new ArgumentNullException(nameof(explorationInputReader));
            cameraRig = quarterViewCameraRig != null
                ? quarterViewCameraRig
                : throw new ArgumentNullException(nameof(quarterViewCameraRig));
            playerFacing = facing != null
                ? facing
                : throw new ArgumentNullException(nameof(facing));

            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            if (characterController == null)
            {
                throw new InvalidOperationException("PlayerMotor requires a CharacterController.");
            }

            ApplyControllerTuning();
            playerFacing.RefreshVisual();
        }

        public void ConfigureGroundSafety(float minimumY)
        {
            enforceMinimumWorldY = true;
            minimumWorldY = minimumY;
            EnforceMinimumWorldY();
        }

        public void Tick(float deltaTime, float currentTime)
        {
            if (deltaTime <= 0f
                || characterController == null
                || !characterController.enabled
                || inputReader == null
                || cameraRig == null
                || playerFacing == null)
            {
                return;
            }

            ExplorationInputGate gate = inputReader.Gate;
            bool movementAllowed = gate.IsAllowed(ExplorationInputChannel.Movement);
            bool dashAllowed = gate.IsAllowed(ExplorationInputChannel.Dash);
            bool dashPressed = inputReader.ConsumeDashPressed();

            if ((!movementAllowed || !dashAllowed) && dashState.IsActive)
            {
                dashState.Cancel();
            }

            Vector3 horizontalDisplacement;
            if (dashState.IsActive)
            {
                horizontalDisplacement = dashState.Tick(deltaTime);
                CurrentHorizontalVelocity = horizontalDisplacement / deltaTime;
            }
            else
            {
                Transform movementBasis = cameraRig.MovementBasis;
                Vector3 movementDirection = movementAllowed
                    ? ExplorationMath.CameraRelativeMove(inputReader.Move, movementBasis)
                    : Vector3.zero;

                if (movementDirection.sqrMagnitude > ExplorationMath.DirectionEpsilon * ExplorationMath.DirectionEpsilon)
                {
                    playerFacing.FaceMovementDirection(movementDirection);
                }

                if (movementAllowed && dashAllowed && dashPressed)
                {
                    dashState.TryStart(
                        movementDirection,
                        playerFacing.CurrentWorldDirection,
                        currentTime,
                        dashDistance,
                        dashDuration,
                        dashCooldown);
                }

                if (dashState.IsActive)
                {
                    horizontalDisplacement = dashState.Tick(deltaTime);
                    CurrentHorizontalVelocity = horizontalDisplacement / deltaTime;
                }
                else
                {
                    float speed = inputReader.SprintHeld ? sprintSpeed : walkSpeed;
                    CurrentHorizontalVelocity = movementDirection * speed;
                    horizontalDisplacement = CurrentHorizontalVelocity * deltaTime;
                }
            }

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -groundedStickSpeed;
            }
            else
            {
                verticalVelocity += gravity * deltaTime;
            }

            Vector3 displacement = horizontalDisplacement + Vector3.up * (verticalVelocity * deltaTime);
            LastCollisionFlags = characterController.Move(displacement);
            EnforceMinimumWorldY();
        }

        public void ResetMotion()
        {
            dashState.Reset();
            verticalVelocity = 0f;
            CurrentHorizontalVelocity = Vector3.zero;
            LastCollisionFlags = CollisionFlags.None;
        }

        private void ApplyControllerTuning()
        {
            characterController.slopeLimit = slopeLimit;
            characterController.stepOffset = Mathf.Min(stepOffset, characterController.height);
            characterController.minMoveDistance = 0f;
        }

        private void EnforceMinimumWorldY()
        {
            if (!enforceMinimumWorldY || characterController == null || transform.position.y >= minimumWorldY)
            {
                return;
            }

            bool controllerWasEnabled = characterController.enabled;
            characterController.enabled = false;
            Vector3 correctedPosition = transform.position;
            correctedPosition.y = minimumWorldY;
            transform.position = correctedPosition;
            characterController.enabled = controllerWasEnabled;
            verticalVelocity = -groundedStickSpeed;
        }
    }
}

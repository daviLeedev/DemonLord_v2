using System;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    #pragma warning disable CS0649 // Serialized Unity references are assigned by the scene builder/inspector.

    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DirectionalSpritePresenter : MonoBehaviour
    {
        [SerializeField] private PlayerFacing playerFacing;
        [SerializeField] private PlayerMotor playerMotor;
        [SerializeField] private ExplorationInputReader inputReader;
        [SerializeField] private QuarterViewCameraRig cameraRig;
        [SerializeField] private DirectionalAnimationSet animationSet;
        [SerializeField, Min(0f)] private float movementThreshold = 0.02f;

        private SpriteRenderer spriteRenderer;
        private DirectionalAnimationState currentState;
        private FacingDirection8 currentDirection;
        private float stateElapsedSeconds;

        public DirectionalAnimationState CurrentState => currentState;

        public FacingDirection8 CurrentCameraRelativeDirection => currentDirection;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            currentState = DirectionalAnimationState.Idle;
            currentDirection = FacingDirection8.South;
        }

        private void Update()
        {
            Refresh(Time.deltaTime);
        }

        private void LateUpdate()
        {
            FaceCamera();
        }

        public void Configure(
            PlayerFacing configuredFacing,
            PlayerMotor configuredMotor,
            ExplorationInputReader configuredInputReader,
            QuarterViewCameraRig configuredCameraRig,
            DirectionalAnimationSet configuredAnimationSet)
        {
            playerFacing = configuredFacing ?? throw new ArgumentNullException(nameof(configuredFacing));
            playerMotor = configuredMotor ?? throw new ArgumentNullException(nameof(configuredMotor));
            inputReader = configuredInputReader ?? throw new ArgumentNullException(nameof(configuredInputReader));
            cameraRig = configuredCameraRig ?? throw new ArgumentNullException(nameof(configuredCameraRig));
            animationSet = configuredAnimationSet ?? throw new ArgumentNullException(nameof(configuredAnimationSet));
            spriteRenderer ??= GetComponent<SpriteRenderer>();
            stateElapsedSeconds = 0f;
            Refresh(0f);
            FaceCamera();
        }

        public void Refresh(float deltaTime)
        {
            if (spriteRenderer == null || playerFacing == null || animationSet == null)
            {
                return;
            }

            DirectionalAnimationState nextState = ResolveState();
            float cameraYaw = cameraRig != null ? cameraRig.CurrentYawDegrees : 0f;
            FacingDirection8 nextDirection = DirectionalSpriteMath.ToCameraRelative(playerFacing.CurrentDirection, cameraYaw);
            if (nextState != currentState || nextDirection != currentDirection)
            {
                currentState = nextState;
                currentDirection = nextDirection;
                stateElapsedSeconds = 0f;
            }
            else
            {
                stateElapsedSeconds += Mathf.Max(0f, deltaTime);
            }

            Sprite sprite = animationSet.GetSprite(currentState, currentDirection, stateElapsedSeconds);
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }

        private DirectionalAnimationState ResolveState()
        {
            if (playerMotor != null && playerMotor.IsDashing)
            {
                return DirectionalAnimationState.Dash;
            }

            Vector3 velocity = playerMotor != null ? playerMotor.CurrentHorizontalVelocity : Vector3.zero;
            velocity.y = 0f;
            if (velocity.sqrMagnitude <= movementThreshold * movementThreshold)
            {
                return DirectionalAnimationState.Idle;
            }

            return inputReader != null && inputReader.SprintHeld
                ? DirectionalAnimationState.Run
                : DirectionalAnimationState.Walk;
        }

        private void FaceCamera()
        {
            Camera camera = cameraRig != null ? cameraRig.GameCamera : null;
            if (camera == null)
            {
                return;
            }

            Vector3 lookDirection = camera.transform.position - transform.position;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > ExplorationMath.DirectionEpsilon * ExplorationMath.DirectionEpsilon)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            }
        }
    }

    public static class DirectionalSpriteMath
    {
        public static FacingDirection8 ToCameraRelative(FacingDirection8 worldDirection, float cameraYawDegrees)
        {
            float relativeYaw = Mathf.Repeat(ExplorationMath.FacingYaw(worldDirection) - cameraYawDegrees, 360f);
            int index = Mathf.FloorToInt((relativeYaw + 22.5f) / 45f) % 8;
            return (FacingDirection8)index;
        }
    }

    #pragma warning restore CS0649
}

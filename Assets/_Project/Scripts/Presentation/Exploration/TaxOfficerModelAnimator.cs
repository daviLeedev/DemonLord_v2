using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    /// <summary>
    /// Drives the imported tax-officer model while the CharacterController remains
    /// the sole owner of world movement. This must live in a file with the same
    /// name as the MonoBehaviour so Unity can serialize it into generated prefabs.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class TaxOfficerModelAnimator : MonoBehaviour
    {
        #pragma warning disable CS0649 // Serialized Unity references are assigned by the scene builder/inspector.
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMotor playerMotor;
        [SerializeField] private ExplorationInputReader inputReader;
        #pragma warning restore CS0649
        [SerializeField, Min(0f)] private float movementThreshold = 0.02f;
        [SerializeField] private string idleState = "Idle";
        [SerializeField] private string walkState = "Walk";
        [SerializeField] private string runState = "Run";
        [SerializeField] private string dashState = "Dash";
        [SerializeField, Min(0f)] private float transitionDuration = 0.08f;

        private int activeStateHash;
        private bool initialized;
        private Vector3 authoredLocalPosition;
        private Quaternion authoredLocalRotation;

        public void Configure(
            Animator configuredAnimator,
            PlayerMotor configuredPlayerMotor,
            ExplorationInputReader configuredInputReader)
        {
            animator = configuredAnimator != null
                ? configuredAnimator
                : throw new System.ArgumentNullException(nameof(configuredAnimator));
            playerMotor = configuredPlayerMotor != null
                ? configuredPlayerMotor
                : throw new System.ArgumentNullException(nameof(configuredPlayerMotor));
            inputReader = configuredInputReader != null
                ? configuredInputReader
                : throw new System.ArgumentNullException(nameof(configuredInputReader));
            animator.applyRootMotion = false;
            initialized = false;
        }

        private void Awake()
        {
            animator ??= GetComponent<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
            }

            authoredLocalPosition = transform.localPosition;
            authoredLocalRotation = transform.localRotation;
        }

        private void Update()
        {
            Refresh();
        }

        private void LateUpdate()
        {
            // Third-party clips can contain translated roots. Only the player
            // CharacterController may move through the world, never this visual.
            transform.localPosition = authoredLocalPosition;
            transform.localRotation = authoredLocalRotation;
        }

        private void Refresh()
        {
            if (animator == null || animator.runtimeAnimatorController == null || playerMotor == null)
            {
                return;
            }

            int nextHash = ResolveStateHash(ResolveState());
            if (nextHash == 0)
            {
                return;
            }

            if (initialized && nextHash == activeStateHash)
            {
                return;
            }

            animator.CrossFade(nextHash, transitionDuration, 0, 0f);
            activeStateHash = nextHash;
            initialized = true;
        }

        private int ResolveStateHash(string stateName)
        {
            int fullPathHash = Animator.StringToHash("Base Layer." + stateName);
            if (animator.HasState(0, fullPathHash))
            {
                return fullPathHash;
            }

            int shortNameHash = Animator.StringToHash(stateName);
            return animator.HasState(0, shortNameHash) ? shortNameHash : 0;
        }

        private string ResolveState()
        {
            if (playerMotor.IsDashing)
            {
                return dashState;
            }

            Vector3 velocity = playerMotor.CurrentHorizontalVelocity;
            velocity.y = 0f;
            if (velocity.sqrMagnitude <= movementThreshold * movementThreshold)
            {
                return idleState;
            }

            return inputReader != null && inputReader.SprintHeld ? runState : walkState;
        }
    }
}

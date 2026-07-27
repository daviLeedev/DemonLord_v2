using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    public sealed class DashRuntimeState
    {
        private Vector3 direction;
        private float speed;
        private float remainingDistance;
        private float remainingDuration;
        private float nextAvailableTime;

        public bool IsActive { get; private set; }

        public Vector3 Direction => direction;

        public float RemainingDistance => remainingDistance;

        public float NextAvailableTime => nextAvailableTime;

        public bool CanStart(float currentTime)
        {
            return !IsActive && currentTime + ExplorationMath.DirectionEpsilon >= nextAvailableTime;
        }

        public bool TryStart(
            Vector3 currentMovement,
            Vector3 lastFacing,
            float currentTime,
            float dashDistance,
            float dashDuration,
            float dashCooldown)
        {
            if (!CanStart(currentTime)
                || dashDistance <= 0f
                || dashDuration <= 0f
                || dashCooldown < 0f)
            {
                return false;
            }

            Vector3 selectedDirection = ExplorationMath.SelectDashDirection(currentMovement, lastFacing);
            if (selectedDirection.sqrMagnitude <= ExplorationMath.DirectionEpsilon * ExplorationMath.DirectionEpsilon)
            {
                return false;
            }

            direction = selectedDirection;
            speed = dashDistance / dashDuration;
            remainingDistance = dashDistance;
            remainingDuration = dashDuration;
            nextAvailableTime = currentTime + dashCooldown;
            IsActive = true;
            return true;
        }

        public Vector3 Tick(float deltaTime)
        {
            if (!IsActive || deltaTime <= 0f)
            {
                return Vector3.zero;
            }

            float usableTime = Mathf.Min(deltaTime, remainingDuration);
            float distance = Mathf.Min(remainingDistance, speed * usableTime);
            remainingDistance = Mathf.Max(0f, remainingDistance - distance);
            remainingDuration = Mathf.Max(0f, remainingDuration - usableTime);

            if (remainingDistance <= ExplorationMath.DirectionEpsilon
                || remainingDuration <= ExplorationMath.DirectionEpsilon)
            {
                IsActive = false;
                remainingDistance = 0f;
                remainingDuration = 0f;
            }

            return direction * distance;
        }

        public bool IsCoolingDown(float currentTime)
        {
            return currentTime + ExplorationMath.DirectionEpsilon < nextAvailableTime;
        }

        public void Cancel()
        {
            IsActive = false;
            remainingDistance = 0f;
            remainingDuration = 0f;
        }

        public void Reset()
        {
            direction = Vector3.zero;
            speed = 0f;
            remainingDistance = 0f;
            remainingDuration = 0f;
            nextAvailableTime = 0f;
            IsActive = false;
        }
    }
}

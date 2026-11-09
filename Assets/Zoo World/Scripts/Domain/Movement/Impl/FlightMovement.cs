using System;

namespace Zoo.Domain
{
    public sealed class FlightMovement : IMovementBehavior
    {
        private const float MaxVerticalSpeed = 3;
        private readonly LinearMovement _linear;

        public float Height { get; }

        public FlightMovement(float speed, float height)
        {
            Guard.Positive(height, nameof(height));
            _linear = new LinearMovement(speed);
            Height = height;
        }

        public MovementPlan Plan(float remainingDistance) => _linear.Plan(remainingDistance);

        public static float VerticalSpeed(float altitudeError, float delta, float verticalGravity)
        {
            var correction = Math.Clamp(altitudeError / delta, -MaxVerticalSpeed, MaxVerticalSpeed);
            return correction - verticalGravity * delta;
        }
    }
}

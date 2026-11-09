using System;

namespace Zoo.Domain
{
    public sealed class LinearMovement : IMovementBehavior
    {
        public float Speed { get; }

        public LinearMovement(float speed)
        {
            Guard.Positive(speed, nameof(speed));
            Speed = speed;
        }

        public MovementPlan Plan(float remainingDistance) => new MovementPlan(Speed, Math.Max(0.02f, remainingDistance / Speed), 0, 0);
    }
}

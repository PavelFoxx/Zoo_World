using System;

namespace Zoo.Domain
{
    public sealed class JumpMovement : IMovementBehavior
    {
        public float Distance { get; }

        public float Duration { get; }

        public float Rest { get; }

        public float Height { get; }

        public JumpMovement(float distance, float duration, float rest, float height)
        {
            Guard.Positive(distance, nameof(distance));
            Guard.Positive(duration, nameof(duration));
            Guard.NonNegative(rest, nameof(rest));
            Guard.NonNegative(height, nameof(height));
            Distance = distance;
            Duration = duration;
            Rest = rest;
            Height = height;
        }

        public MovementPlan Plan(float remainingDistance) => new MovementPlan(Distance / Duration, Duration, Rest, Height);
    }
}

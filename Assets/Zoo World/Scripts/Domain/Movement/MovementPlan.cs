using System;

namespace Zoo.Domain
{
    public readonly struct MovementPlan
    {
        public readonly float Speed;
        public readonly float Duration;
        public readonly float Rest;
        public readonly float Height;

        public MovementPlan(float speed, float duration, float rest, float height)
        {
            Speed = speed;
            Duration = duration;
            Rest = rest;
            Height = height;
        }
    }
}

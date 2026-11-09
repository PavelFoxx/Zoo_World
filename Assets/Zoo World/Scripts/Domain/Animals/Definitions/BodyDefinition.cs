using System;

namespace Zoo.Domain
{
    public sealed class BodyDefinition
    {
        public float Radius { get; }

        public float Mass { get; }

        public float GravityScale { get; }

        public BodyDefinition(float radius, float mass, float gravityScale = 1)
        {
            Guard.Positive(radius, nameof(radius));
            Guard.Positive(mass, nameof(mass));
            Guard.NonNegative(gravityScale, nameof(gravityScale));
            Radius = radius;
            Mass = mass;
            GravityScale = gravityScale;
        }
    }
}

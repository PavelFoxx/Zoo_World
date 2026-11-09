using System;
using System.Numerics;
using Zoo.Domain;

namespace Zoo.Application
{
    public sealed class BounceInteraction : IInteraction
    {
        private readonly IPhysicsWorld _physics;

        public BounceInteraction(IPhysicsWorld physics)
        {
            _physics = physics ?? throw new ArgumentNullException(nameof(physics));
        }

        public InteractionType Type => InteractionType.Bounce;
        public bool SuppressesPhysicalResponse => false;

        public bool CanExecute(in CollisionEvent collision) =>
            FindRule(collision.First, collision.Second) != null || FindRule(collision.Second, collision.First) != null;

        public void Execute(in CollisionEvent collision)
        {
            var first = FindRule(collision.First, collision.Second);
            var second = FindRule(collision.Second, collision.First);
            var strength = Math.Max(first?.Impulse ?? 0, second?.Impulse ?? 0);
            var direction = collision.Second.Pose.CurrentValue.Position - collision.First.Pose.CurrentValue.Position;
            direction.Y = 0;
            if (direction.LengthSquared() > 0.00001f)
            {
                direction = Vector3.Normalize(direction);
            }
            else
            {
                direction = Vector3.UnitX;
            }

            var impulse = direction * strength;
            _physics.Impulse(collision.First.Id, -impulse);
            _physics.Impulse(collision.Second.Id, impulse);
        }

        private static BounceRule FindRule(Entity actor, Entity target) =>
            (actor as Animal)?.Definition.Interactions.FindRule(target) as BounceRule;
    }
}

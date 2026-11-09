using System;

namespace Zoo.Domain
{
    public sealed class AnimalDefinition
    {
        public string TypeId { get; }

        public FoodRole Role { get; }

        public BodyDefinition Body { get; }

        public IMovementBehavior Movement { get; }

        public InteractionProfile Interactions { get; }

        public BehaviorDefinition Behavior { get; }

        public string NavigationProfile { get; }

        public AnimalDefinition(string typeId, FoodRole role, BodyDefinition body,
            IMovementBehavior movement, InteractionProfile interactions,
            BehaviorDefinition behavior, string navigationProfile = "ground")
        {
            if (string.IsNullOrWhiteSpace(typeId))
            {
                throw new ArgumentException("TypeId is required.");
            }

            TypeId = typeId;
            Role = role;
            Body = body ?? throw new ArgumentNullException(nameof(body));
            Movement = movement ?? throw new ArgumentNullException(nameof(movement));
            Interactions = interactions ?? throw new ArgumentNullException(nameof(interactions));
            Behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
            NavigationProfile = navigationProfile;
        }
    }
}

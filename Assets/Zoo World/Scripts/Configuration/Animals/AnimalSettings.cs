using System;
using Zoo.Domain;

namespace Zoo.Configuration
{
    [Serializable]
    public sealed class AnimalSettings
    {
        public string typeId;
        public string role;
        public float radius;
        public float mass;
        public float gravityScale = 1;
        public string behavior;
        public float retrySeconds;
        public float stuckSeconds;
        public string navigationProfile;
        public MovementSettings movement;
        public InteractionSettings interactions;

        public AnimalDefinition Create()
        {
            SettingsValidation.Identifier(typeId, "TypeId");
            SettingsValidation.Identifier(navigationProfile, "Navigation profile");
            SettingsValidation.Require(behavior == "wanderAndReturn", "Unsupported behavior: " + behavior);
            SettingsValidation.Positive(radius, "animal.radius");
            SettingsValidation.Positive(mass, "animal.mass");
            SettingsValidation.NonNegative(gravityScale, "animal.gravityScale");
            SettingsValidation.Positive(retrySeconds, "animal.retrySeconds");
            SettingsValidation.Positive(stuckSeconds, "animal.stuckSeconds");
            SettingsValidation.Require(movement != null && interactions != null, "Animal movement and interactions are required.");
            return new AnimalDefinition(typeId, SettingsValidation.Role(role), new BodyDefinition(radius, mass, gravityScale),
                movement.Create(), interactions.Create(), new BehaviorDefinition(retrySeconds, stuckSeconds), navigationProfile);
        }
    }
}

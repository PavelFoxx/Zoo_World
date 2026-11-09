using System.Collections.Generic;

namespace Zoo.Domain
{
    public sealed class InteractionProfile
    {
        private readonly Dictionary<FoodRole, IInteractionRule> _roles;

        public InteractionProfile(IDictionary<FoodRole, IInteractionRule> roles = null)
        {
            _roles = roles == null ? new Dictionary<FoodRole, IInteractionRule>() : new Dictionary<FoodRole, IInteractionRule>(roles);
        }

        public IInteractionRule FindRule(Entity target)
        {
            if (target is Animal animal && _roles.TryGetValue(animal.Definition.Role, out var role))
            {
                return role;
            }

            return null;
        }
    }
}

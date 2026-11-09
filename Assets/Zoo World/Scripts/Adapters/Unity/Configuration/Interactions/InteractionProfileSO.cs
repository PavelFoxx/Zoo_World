using System;
using System.Collections.Generic;
using UnityEngine;
using Zoo.Domain;

namespace Zoo.Unity
{
    [CreateAssetMenu(menuName = "Zoo/Interaction profile")]
    public sealed class InteractionProfileSO : ScriptableObject
    {
        [Serializable]
        public sealed class RoleRule
        {
            public FoodRole Role;
            public InteractionRuleSO Rule;
        }

        public RoleRule[] Roles = Array.Empty<RoleRule>();

        public InteractionProfile Create()
        {
            var roles = new Dictionary<FoodRole, IInteractionRule>();
            foreach (var entry in Roles)
            {
                if (entry?.Rule == null)
                {
                    throw new InvalidOperationException(name + ": invalid role rule.");
                }

                roles.Add(entry.Role, entry.Rule.Create());
            }

            return new InteractionProfile(roles);
        }
    }
}

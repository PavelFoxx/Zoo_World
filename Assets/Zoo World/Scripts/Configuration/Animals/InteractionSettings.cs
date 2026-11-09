using System;
using System.Collections.Generic;
using Zoo.Domain;

namespace Zoo.Configuration
{
    [Serializable]
    public sealed class InteractionSettings
    {
        [Serializable]
        public sealed class Entry
        {
            public string target;
            public InteractionRuleSettings rule;
        }

        public Entry[] byRole;

        public InteractionProfile Create()
        {
            SettingsValidation.Require(byRole != null, "Interaction role rules are required (an empty array is allowed).");
            var roles = new Dictionary<FoodRole, IInteractionRule>();
            foreach (var entry in byRole)
            {
                ValidateEntry(entry);
                roles.Add(SettingsValidation.Role(entry.target), entry.rule.Create());
            }

            return new InteractionProfile(roles);
        }

        private static void ValidateEntry(Entry entry)
        {
            SettingsValidation.Require(entry != null && entry.rule != null, "Null interaction entry or rule.");
            SettingsValidation.Identifier(entry.target, "Interaction role");
        }
    }
}

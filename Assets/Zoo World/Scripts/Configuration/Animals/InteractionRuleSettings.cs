using System;
using Zoo.Domain;

namespace Zoo.Configuration
{
    [Serializable]
    public sealed class InteractionRuleSettings
    {
        public string kind;
        public float impulse;

        public IInteractionRule Create()
        {
            switch (kind)
            {
                case "eat":
                    return new EatRule();
                case "bounce":
                    SettingsValidation.NonNegative(impulse, "rule.impulse");
                    return new BounceRule(impulse);
                default:
                    throw new ArgumentException("Unsupported interaction kind: " + kind);
            }
        }
    }
}

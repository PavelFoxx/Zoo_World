using System;
using System.Linq;
using Zoo.Configuration;
using Zoo.Unity;

namespace Zoo.Editor
{
    internal static class AnimalSettingsExporter
    {
        public static AnimalSettings Capture(AnimalDefinitionSO source)
        {
            var definition = source.Create();
            return new AnimalSettings
            {
                typeId = definition.TypeId,
                role = definition.Role.ToString().ToLowerInvariant(),
                radius = definition.Body.Radius,
                mass = definition.Body.Mass,
                gravityScale = definition.Body.GravityScale,
                behavior = "wanderAndReturn",
                retrySeconds = definition.Behavior.RetrySeconds,
                stuckSeconds = definition.Behavior.StuckSeconds,
                navigationProfile = definition.NavigationProfile,
                movement = Movement(source.Movement),
                interactions = new InteractionSettings
                {
                    byRole = source.Interactions.Roles.Select(entry => new InteractionSettings.Entry
                    {
                        target = entry.Role.ToString().ToLowerInvariant(),
                        rule = Rule(entry.Rule)
                    }).ToArray()
                }
            };
        }

        private static MovementSettings Movement(MovementConfigSO source)
        {
            switch (source)
            {
                case LinearMovementConfigSO linear:
                    return new MovementSettings
                    {
                        kind = "linear",
                        speed = linear.Speed
                    };
                case FlightMovementConfigSO flight:
                    return new MovementSettings
                    {
                        kind = "flight",
                        speed = flight.Speed,
                        height = flight.Height
                    };
                case JumpMovementConfigSO jump:
                    return new MovementSettings
                    {
                        kind = "jump",
                        distance = jump.Distance,
                        duration = jump.Duration,
                        rest = jump.Rest,
                        height = jump.Height
                    };
                default:
                    throw new NotSupportedException("Add JSON export support for movement: " + source.GetType().Name);
            }
        }

        private static InteractionRuleSettings Rule(InteractionRuleSO source)
        {
            switch (source)
            {
                case EatRuleSO _:
                    return new InteractionRuleSettings
                    {
                        kind = "eat"
                    };
                case BounceRuleSO bounce:
                    return new InteractionRuleSettings
                    {
                        kind = "bounce",
                        impulse = bounce.Impulse
                    };
                default:
                    throw new NotSupportedException("Add JSON export support for rule: " + source.GetType().Name);
            }
        }
    }
}

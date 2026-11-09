using System;
using Zoo.Domain;

namespace Zoo.Configuration
{
    [Serializable]
    public sealed class MovementSettings
    {
        public string kind;
        public float speed;
        public float distance;
        public float duration;
        public float rest;
        public float height;

        public IMovementBehavior Create()
        {
            switch (kind)
            {
                case "linear":
                    SettingsValidation.Positive(speed, "movement.speed");
                    return new LinearMovement(speed);
                case "jump":
                    SettingsValidation.Positive(distance, "movement.distance");
                    SettingsValidation.Positive(duration, "movement.duration");
                    SettingsValidation.NonNegative(rest, "movement.rest");
                    SettingsValidation.NonNegative(height, "movement.height");
                    return new JumpMovement(distance, duration, rest, height);
                case "flight":
                    SettingsValidation.Positive(speed, "movement.speed");
                    SettingsValidation.Positive(height, "movement.height");
                    return new FlightMovement(speed, height);
                default:
                    throw new ArgumentException("Unsupported movement kind: " + kind);
            }
        }
    }
}

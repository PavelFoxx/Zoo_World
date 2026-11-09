using System;
using Zoo.Domain;

namespace Zoo.Configuration
{
    internal static class SettingsValidation
    {
        public static void Require(bool valid, string message)
        {
            if (!valid)
            {
                throw new ArgumentException(message);
            }
        }

        public static void Identifier(string value, string name) => Require(!string.IsNullOrWhiteSpace(value), name + " is required.");

        public static void Finite(float value, string name) => Require(!float.IsNaN(value) && !float.IsInfinity(value), name + " must be finite.");

        public static void Positive(float value, string name)
        {
            Finite(value, name);
            Require(value > 0, name + " must be positive.");
        }

        public static void NonNegative(float value, string name)
        {
            Finite(value, name);
            Require(value >= 0, name + " must be non-negative.");
        }

        public static FoodRole Role(string value)
        {
            switch (value)
            {
                case "prey":
                    return FoodRole.Prey;
                case "predator":
                    return FoodRole.Predator;
                default:
                    throw new ArgumentException("Unsupported food role: " + value);
            }
        }
    }
}

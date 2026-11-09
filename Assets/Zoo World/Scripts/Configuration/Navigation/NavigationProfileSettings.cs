using System;

namespace Zoo.Configuration
{
    [Serializable]
    public sealed class NavigationProfileSettings
    {
        public string id;
        public float radius;
        public float height;
        public float maxClimb;
        public float maxSlope;
        public string meshBase64;

        public void Validate()
        {
            SettingsValidation.Identifier(id, "Navigation profile id");
            SettingsValidation.Positive(radius, "navigation.radius");
            SettingsValidation.Positive(height, "navigation.height");
            SettingsValidation.NonNegative(maxClimb, "navigation.maxClimb");
            SettingsValidation.NonNegative(maxSlope, "navigation.maxSlope");
            SettingsValidation.Require(maxSlope < 90, "Navigation slope must be below 90 degrees.");
            SettingsValidation.Require(!string.IsNullOrEmpty(meshBase64), "Navigation mesh is missing.");
            SettingsValidation.Require(Convert.FromBase64String(meshBase64).Length > 0, "Navigation mesh is empty.");
        }
    }
}

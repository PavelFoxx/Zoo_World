using System;
using System.Collections.Generic;

namespace Zoo.Configuration
{
    [Serializable]
    public sealed class NavigationSettings
    {
        public string format;
        public string geometryHash;
        public float cellSize;
        public NavigationProfileSettings[] profiles;

        public void Validate()
        {
            SettingsValidation.Require(format == "dotrecast-2026.3.1", "Unsupported navigation format: " + format);
            SettingsValidation.Identifier(geometryHash, "Navigation geometry hash");
            SettingsValidation.Positive(cellSize, "navigation.cellSize");
            SettingsValidation.Require(profiles != null && profiles.Length > 0, "Navigation profiles are missing.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var profile in profiles)
            {
                SettingsValidation.Require(profile != null, "Null navigation profile.");
                profile.Validate();
                SettingsValidation.Require(ids.Add(profile.id), "Duplicate navigation profile: " + profile.id);
            }
        }
    }
}

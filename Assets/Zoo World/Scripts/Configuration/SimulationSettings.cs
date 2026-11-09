using System;
using System.Collections.Generic;
using System.Linq;
using Zoo.Application;
using Zoo.Domain;

namespace Zoo.Configuration
{
    [Serializable]
    public sealed class SimulationSettings
    {
        public const int CurrentSchemaVersion = 2;

        public int schemaVersion;
        public int seed;
        public bool randomizeSeed;
        public float fixedDelta;
        public WorldSettings world;
        public AnimalSettings[] animals;
        public NavigationSettings navigation;

        public AnimalDefinition[] CreateDefinitions()
        {
            SettingsValidation.Require(schemaVersion == CurrentSchemaVersion, "Unsupported configuration schema version: " + schemaVersion);
            SettingsValidation.Require(fixedDelta == GameSession.FixedDelta, "Configuration timestep does not match this simulation version.");
            SettingsValidation.Require(world != null && navigation != null, "World and navigation are required.");
            world.Validate();
            navigation.Validate();
            SettingsValidation.Require(animals != null && animals.Length > 0, "Animal catalog is missing or empty.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var definitions = new AnimalDefinition[animals.Length];
            for (var i = 0; i < animals.Length; i++)
            {
                var animal = animals[i];
                SettingsValidation.Require(animal != null, "Null animal definition.");
                definitions[i] = animal.Create();
                SettingsValidation.Require(ids.Add(animal.typeId), "Duplicate TypeId: " + animal.typeId);
                var profile = navigation.profiles.SingleOrDefault(p => p.id == animal.navigationProfile);
                SettingsValidation.Require(profile != null, "Unknown navigation profile: " + animal.navigationProfile);
                SettingsValidation.Require(profile.radius >= animal.radius, "Navigation clearance is smaller than animal radius: " + animal.typeId);
                var diameterWithMargin = 2 * (animal.radius + 0.8f);
                SettingsValidation.Require(world.boundsMax.x - world.boundsMin.x > diameterWithMargin && world.boundsMax.z - world.boundsMin.z > diameterWithMargin, "World is too small to spawn: " + animal.typeId);
            }

            return definitions;
        }
    }
}

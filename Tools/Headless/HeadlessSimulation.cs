using System;
using System.Security.Cryptography;
using Zoo.Application;
using Zoo.Configuration;
using Zoo.Headless.Physics;
using Zoo.Navigation;

namespace Zoo.Headless
{
    public static class HeadlessSimulation
    {
        public static int ResolveSeed(SimulationSettings settings, int? seedOverride)
        {
            if (seedOverride.HasValue)
            {
                return seedOverride.Value;
            }

            return settings.randomizeSeed ? BitConverter.ToInt32(RandomNumberGenerator.GetBytes(sizeof(int))) : settings.seed;
        }

        public static GameSession Create(SimulationSettings settings, int seed)
        {
            var definitions = settings.CreateDefinitions();
            var navigation = new DotRecastNavigationService();
            BepuPhysicsWorld physics = null;
            try
            {
                foreach (var profile in settings.navigation.profiles)
                {
                    navigation.Load(profile.id, Convert.FromBase64String(profile.meshBase64));
                }

                physics = new BepuPhysicsWorld(settings.world);
                return new GameSession(definitions, physics, navigation, settings.world.CreateBounds(), seed);
            }
            catch
            {
                physics?.Dispose();
                navigation.Dispose();
                throw;
            }
        }
    }
}

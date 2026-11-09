using System;
using Zenject;
using Zoo.Application;
using Zoo.Navigation;
using Zoo.Physics.Unity;
using Zoo.Unity;

namespace Zoo.Composition
{
    // Creation and ownership transfer of the native simulation adapters are atomic.
    public sealed class GameSessionFactory : IFactory<GameSession>
    {
        private readonly ZooCatalog _catalog;
        private readonly GameSceneBindings _scene;
        private readonly IViewPool _pool;
        private readonly int _seed;

        public GameSessionFactory(ZooCatalog catalog, GameSceneBindings scene, IViewPool pool, int seed)
        {
            _catalog = catalog;
            _scene = scene;
            _pool = pool;
            _seed = seed;
        }

        public GameSession Create()
        {
            if (_scene.NavigationData == null || _scene.NavigationData.Profiles == null)
            {
                throw new InvalidOperationException("Bake navigation before playing.");
            }

            var bounds = _scene.ReadBounds();
            var definitions = _catalog.CreateDefinitions();
            var navigation = new DotRecastNavigationService();
            UnityPhysicsWorld physics = null;
            try
            {
                foreach (var profile in _scene.NavigationData.Profiles)
                {
                    navigation.Load(profile.ProfileId, profile.Mesh);
                }

                physics = new UnityPhysicsWorld(_pool, _scene.PhysicsRoot, _scene.PhysicsBodyPrefab);
                return new GameSession(definitions, physics, navigation, bounds, _seed);
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

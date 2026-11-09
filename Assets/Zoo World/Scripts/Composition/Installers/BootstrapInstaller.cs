using UnityEngine;
using Zenject;
using Zoo.Application;
using Zoo.Unity;

namespace Zoo.Composition
{
    public sealed class BootstrapInstaller : MonoInstaller
    {
        public ZooCatalog Catalog;
        public int Seed = 1701;
        public bool RandomizeSeed;

        public override void InstallBindings()
        {
            Container.BindInstance(Catalog);
            var seed = RandomizeSeed ? Random.Range(int.MinValue, int.MaxValue) : Seed;
            Container.BindInstance(seed).WhenInjectedInto<GameSessionFactory>();
            Container.BindInterfacesAndSelfTo<UnitySceneLoadingService>().AsSingle();
            Container.BindInterfacesAndSelfTo<UnityGameSessionHost>().AsSingle();
            // ZooBootstrap owns the asynchronous flow until native scene unloading finishes.
            Container.Bind<ApplicationFlow>().FromMethod(c => new ApplicationFlow(
                c.Container.Resolve<ISceneLoadingService>(), c.Container.Resolve<IGameSessionHost>())).AsSingle();
        }
    }
}

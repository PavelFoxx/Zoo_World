using UnityEngine;
using Zenject;
using Zoo.Application;
using Zoo.Unity;

namespace Zoo.Composition
{
    public sealed class GameSceneInstaller : MonoInstaller
    {
        [SerializeField] private GameSceneBindings bindings;
        public GameSceneBindings Bindings => bindings;

        public override void InstallBindings()
        {
            Container.BindInstance(bindings);
            Container.BindInstance(bindings.Hud);
            Container.Bind<Transform>().FromInstance(bindings.AnimalsRoot).WhenInjectedInto<EntityViewCoordinator>();
            // GameSceneSession owns these disposables, including partial preparation failures.
            Container.Bind<IViewPool>().To<NightPoolViewPool>().AsSingle();
            Container.Bind<GameSession>().FromFactory<GameSessionFactory>().AsSingle();
            Container.Bind<ZooViewModel>().FromMethod(c => new ZooViewModel(c.Container.Resolve<GameSession>().World)).AsSingle();
            Container.Bind<EntityViewCoordinator>().AsSingle();
            Container.BindInterfacesAndSelfTo<GameSceneSession>().AsSingle();
        }
    }
}

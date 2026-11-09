using UnityEngine;
using Zenject;
using Zoo.Unity;

namespace Zoo.Composition
{
    public sealed class AnimalViewInstaller : MonoInstaller
    {
        [SerializeField] private AnimalView view;

        public override void InstallBindings()
        {
            Container.BindInstance(view);
            Container.BindInterfacesAndSelfTo<AnimalViewPresenter>().AsSingle();
        }
    }
}

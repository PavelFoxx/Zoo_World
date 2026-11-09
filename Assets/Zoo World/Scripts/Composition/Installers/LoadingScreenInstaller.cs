using Zenject;
using Zoo.Application;

namespace Zoo.Composition
{
    public sealed class LoadingScreenInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<LoadingScreenViewModel>().FromMethod(c =>
            {
                var flow = c.Container.Resolve<ApplicationFlow>();
                return new LoadingScreenViewModel(flow.Status, flow.Restart);
            }).AsSingle();
        }
    }
}

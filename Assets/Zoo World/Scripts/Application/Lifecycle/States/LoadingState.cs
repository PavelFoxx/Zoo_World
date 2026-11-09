using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniState;

namespace Zoo.Application
{
    public sealed partial class ApplicationFlow
    {
        [StateBehaviour(ProhibitReturnToState = true)]
        private sealed class LoadingState : StateBase
        {
            private readonly ISceneLoadingService _loader;
            private readonly string _gameScene;
            private readonly Action<ApplicationStatus> _publishStatus;

            public LoadingState(ISceneLoadingService loader, string gameScene, Action<ApplicationStatus> publishStatus)
            {
                _loader = loader;
                _gameScene = gameScene;
                _publishStatus = publishStatus;
            }

            public override async UniTask<StateTransitionInfo> Execute(CancellationToken token)
            {
                _publishStatus(new ApplicationStatus(ApplicationStage.Loading));
                var progress = Progress.Create<float>(value => _publishStatus(new ApplicationStatus(ApplicationStage.Loading, value)));
                await _loader.Load(_gameScene, token, progress);
                return Transition.GoTo<PreparingState>();
            }
        }
    }
}

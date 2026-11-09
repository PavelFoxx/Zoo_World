using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniState;

namespace Zoo.Application
{
    public sealed partial class ApplicationFlow
    {
        [StateBehaviour(ProhibitReturnToState = true)]
        private sealed class PreparingState : StateBase
        {
            private readonly IGameSessionHost _host;
            private readonly Action<ApplicationStatus> _publishStatus;

            public PreparingState(IGameSessionHost host, Action<ApplicationStatus> publishStatus)
            {
                _host = host;
                _publishStatus = publishStatus;
            }

            public override async UniTask<StateTransitionInfo> Execute(CancellationToken token)
            {
                _publishStatus(new ApplicationStatus(ApplicationStage.Preparing));
                await _host.Prepare(token);
                return Transition.GoTo<PlayingState>();
            }
        }
    }
}

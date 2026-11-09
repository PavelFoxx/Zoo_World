using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniState;

namespace Zoo.Application
{
    public sealed partial class ApplicationFlow
    {
        [StateBehaviour(ProhibitReturnToState = true)]
        private sealed class PlayingState : StateBase
        {
            private readonly IGameSessionHost _host;
            private readonly Func<ApplicationStatus, CancellationToken, UniTask> _waitForRestart;

            public PlayingState(IGameSessionHost host, Func<ApplicationStatus, CancellationToken, UniTask> waitForRestart)
            {
                _host = host;
                _waitForRestart = waitForRestart;
            }

            public override async UniTask<StateTransitionInfo> Execute(CancellationToken token)
            {
                _host.Start();
                await _waitForRestart(new ApplicationStatus(ApplicationStage.Playing), token);
                return Transition.GoToExit();
            }
        }
    }
}

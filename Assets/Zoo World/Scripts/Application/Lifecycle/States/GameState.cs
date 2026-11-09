using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniState;

namespace Zoo.Application
{
    public sealed partial class ApplicationFlow
    {
        [StateBehaviour(ProhibitReturnToState = true)]
        private sealed class GameState : StateBase
        {
            private readonly ITypeResolver _resolver;
            private readonly IGameSessionHost _host;
            private readonly ISceneLoadingService _loader;
            private readonly Func<ApplicationStatus, CancellationToken, UniTask> _waitForRestart;

            public GameState(
                ITypeResolver resolver,
                IGameSessionHost host,
                ISceneLoadingService loader,
                Func<ApplicationStatus, CancellationToken, UniTask> waitForRestart)
            {
                _resolver = resolver;
                _host = host;
                _loader = loader;
                _waitForRestart = waitForRestart;
            }

            public override async UniTask<StateTransitionInfo> Execute(CancellationToken token)
            {
                Exception failure = null;
                try
                {
                    var child = new StateMachine();
                    child.SetResolver(_resolver);
                    await child.Execute<LoadingState>(token);
                }
                catch (Exception error) when (!(error is OperationCanceledException && token.IsCancellationRequested))
                {
                    failure = error;
                }
                finally
                {
                    try
                    {
                        _host.Stop();
                    }
                    finally
                    {
                        await _loader.Unload();
                    }
                }

                token.ThrowIfCancellationRequested();
                if (failure != null)
                {
                    await _waitForRestart(new ApplicationStatus(ApplicationStage.Failed, error: failure.Message), token);
                }

                return Transition.GoTo<GameState>();
            }
        }
    }
}

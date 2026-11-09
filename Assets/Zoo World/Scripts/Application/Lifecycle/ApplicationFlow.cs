using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UniState;

namespace Zoo.Application
{
    public sealed partial class ApplicationFlow : ITypeResolver, IDisposable
    {
        private readonly ISceneLoadingService _loader;
        private readonly IGameSessionHost _host;
        private readonly string _gameScene;

        private readonly ReactiveProperty<ApplicationStatus> _status = new ReactiveProperty<ApplicationStatus>();
        private UniTaskCompletionSource _restart;
        private bool _running;
        public ReadOnlyReactiveProperty<ApplicationStatus> Status => _status;

        public ApplicationFlow(ISceneLoadingService loader, IGameSessionHost host, string gameScene = "Game")
        {
            _loader = loader;
            _host = host;
            _gameScene = gameScene;
        }

        public void Restart() => _restart?.TrySetResult();

        public async UniTask Run(CancellationToken token)
        {
            if (_running)
            {
                throw new InvalidOperationException("Application flow is already running.");
            }

            _running = true;
            try
            {
                var machine = new StateMachine();
                machine.SetResolver(this);
                await machine.Execute<GameState>(token);
            }
            finally
            {
                _running = false;
            }
        }

        public object Resolve(Type type)
        {
            if (type == typeof(GameState))
            {
                return new GameState(this, _host, _loader, WaitForRestart);
            }

            if (type == typeof(LoadingState))
            {
                return new LoadingState(_loader, _gameScene, PublishStatus);
            }

            if (type == typeof(PreparingState))
            {
                return new PreparingState(_host, PublishStatus);
            }

            if (type == typeof(PlayingState))
            {
                return new PlayingState(_host, WaitForRestart);
            }

            throw new InvalidOperationException("Unknown application state: " + type);
        }

        private void PublishStatus(ApplicationStatus status) => _status.Value = status;

        private async UniTask WaitForRestart(ApplicationStatus status, CancellationToken token)
        {
            var restart = new UniTaskCompletionSource();
            _restart = restart;
            using var registration = token.Register(() => restart.TrySetCanceled(token));
            try
            {
                PublishStatus(status);
                await restart.Task;
            }
            finally
            {
                _restart = null;
            }
        }

        public void Dispose() => _status.Dispose();
    }
}

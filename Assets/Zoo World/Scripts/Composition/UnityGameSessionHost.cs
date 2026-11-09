using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Zenject;
using Zoo.Application;
using Zoo.Unity;

namespace Zoo.Composition
{
    // Composition boundary between Bootstrap and the loaded Game scope.
    public sealed class UnityGameSessionHost : IGameSessionHost, IDisposable
    {
        private readonly UnitySceneLoadingService _loader;
        private readonly SceneContextRegistry _scenes;
        private GameSceneSession _game;

        public GameSession Session => _game?.Session;
        public bool IsPlaying => _game?.IsPlaying == true;

        public UnityGameSessionHost(UnitySceneLoadingService loader, SceneContextRegistry scenes)
        {
            _loader = loader;
            _scenes = scenes;
        }

        public async UniTask Prepare(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (_game != null)
            {
                throw new InvalidOperationException("Stop the current session before preparing another.");
            }

            var context = _scenes.TryGetSceneContextForScene(_loader.LoadedScene);
            if (context == null || !context.HasResolved)
            {
                throw new InvalidOperationException("Game requires an initialized SceneContext with GameSceneInstaller.");
            }

            try
            {
                _game = context.Container.Resolve<GameSceneSession>();
                await _game.Prepare(token);
            }
            catch
            {
                Stop();
                throw;
            }
        }

        public void Start() => _game.Start();
        public void Advance(float delta) => _game?.Advance(delta);

        public void Stop()
        {
            var game = _game;
            _game = null;
            game?.Dispose();
        }

        public void Dispose() => Stop();
    }
}

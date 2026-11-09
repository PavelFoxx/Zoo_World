using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Zenject;
using Zoo.Application;
using Zoo.Unity;

namespace Zoo.Composition
{
    public sealed class GameSceneSession : IDisposable
    {
        private readonly LazyInject<GameSession> _session;
        private readonly LazyInject<ZooViewModel> _model;
        private readonly LazyInject<EntityViewCoordinator> _views;
        private readonly IViewPool _pool;
        private readonly ZooHudView _hud;
        private ZooViewModel _activeModel;
        private EntityViewCoordinator _activeViews;
        private bool _disposed;

        public GameSession Session { get; private set; }
        public bool IsPlaying { get; private set; }

        public GameSceneSession(LazyInject<GameSession> session, LazyInject<ZooViewModel> model,
            LazyInject<EntityViewCoordinator> views, IViewPool pool, ZooHudView hud)
        {
            _session = session;
            _model = model;
            _views = views;
            _pool = pool;
            _hud = hud;
        }

        public async UniTask Prepare(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (_disposed || Session != null)
            {
                throw new InvalidOperationException("A Game scene scope can be prepared only once. Reload Game to retry.");
            }

            try
            {
                Session = _session.Value;
                _activeModel = _model.Value;
                _activeViews = _views.Value;
                _hud.Bind(_activeModel);
                await UniTask.Yield();
                token.ThrowIfCancellationRequested();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Start()
        {
            if (_disposed || Session == null)
            {
                throw new InvalidOperationException("Prepare the Game scene before starting it.");
            }

            Session.Start();
            IsPlaying = true;
        }

        public void Advance(float delta)
        {
            if (IsPlaying)
            {
                Session.Advance(delta);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            IsPlaying = false;
            Session?.Dispose();
            Session = null;
            _activeViews?.Dispose();
            if (_hud != null)
            {
                _hud.Unbind();
            }
            _activeModel?.Dispose();
            _pool.Dispose();
        }
    }
}

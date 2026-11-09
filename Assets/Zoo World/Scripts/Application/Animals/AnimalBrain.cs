using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Zoo.Application
{
    public abstract class AnimalBrain : IDisposable
    {
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private CancellationTokenSource _activityLifetime;
        private bool _running;
        private bool _disposed;

        public async UniTask Run()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (_running)
            {
                throw new InvalidOperationException("Animal brain is already running.");
            }

            _running = true;
            var token = _lifetime.Token;
            try
            {
                await Execute(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            finally
            {
                _running = false;
            }
        }

        internal abstract void CheckBounds();
        protected abstract UniTask Execute(CancellationToken token);
        protected abstract void DisposeResources();

        protected void InterruptActivity() => _activityLifetime?.Cancel();

        protected async UniTask RunActivity(Func<CancellationToken, UniTask> execute, CancellationToken token)
        {
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
            _activityLifetime = lifetime;
            try
            {
                lifetime.Token.ThrowIfCancellationRequested();
                await execute(lifetime.Token);
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested && !token.IsCancellationRequested)
            {
            }
            finally
            {
                _activityLifetime = null;
            }

            token.ThrowIfCancellationRequested();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                _lifetime.Cancel();
            }
            finally
            {
                try
                {
                    DisposeResources();
                }
                finally
                {
                    _lifetime.Dispose();
                }
            }
        }
    }
}

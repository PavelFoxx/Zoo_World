using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;
using Zoo.Application;

namespace Zoo.Composition
{
    public sealed class ZooBootstrap : MonoBehaviour
    {
        private CancellationTokenSource _lifetime;
        private UnityGameSessionHost _host;
        private ApplicationFlow _flow;
        public UnityGameSessionHost Host => _host;
        public string LastError { get; private set; }

        [Inject]
        public void Construct(UnityGameSessionHost host, ApplicationFlow flow)
        {
            _host = host;
            _flow = flow;
        }

        private void Start()
        {
            UnityEngine.Application.runInBackground = true;
            _lifetime = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken, UnityEngine.Application.exitCancellationToken);
            Run(_lifetime.Token).Forget(Debug.LogException);
        }

        private async UniTask Run(CancellationToken token)
        {
            try
            {
                await _flow.Run(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                LastError = exception.ToString();
                Debug.LogException(exception);
            }
            finally
            {
                _flow.Dispose();
            }
        }

        private void Update()
        {
            try
            {
                _host?.Advance(Time.deltaTime);
            }
            catch (Exception exception)
            {
                LastError = exception.ToString();
                Debug.LogException(exception);
                _host.Stop();
                enabled = false;
            }
        }

        public void Restart() => _flow.Restart();

        private void OnDestroy()
        {
            _lifetime?.Cancel();
            _host?.Stop();
            _lifetime?.Dispose();
            // Run keeps the flow alive until any native scene operation has completed.
        }
    }
}

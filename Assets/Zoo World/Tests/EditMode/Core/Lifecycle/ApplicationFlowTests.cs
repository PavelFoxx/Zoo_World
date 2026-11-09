using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using R3;
using Zoo.Application;

namespace Zoo.Tests
{
    public sealed class ApplicationFlowTests
    {
        [Test]
        public void CanceledNativeLoadDoesNotPrepareGameAndStillUnloads()
        {
            var loader = new ControlledLoader();
            var host = new TestHost();
            using var flow = new ApplicationFlow(loader, host);
            using var lifetime = new CancellationTokenSource();
            var run = flow.Run(lifetime.Token).SuppressCancellationThrow();
            lifetime.Cancel();
            loader.CompleteNativeLoad();
            Assert.That(run.GetAwaiter().GetResult(), Is.True);
            Assert.That(host.Prepared, Is.Zero);
            Assert.That(host.Started, Is.Zero);
            Assert.That(loader.Loaded, Is.False);
        }

        [Test]
        public void LoadErrorCanRetryAndRestartDoesNotStartBeforePreparation()
        {
            var loader = new ControlledLoader
            {
                FailNext = true
            };
            var host = new TestHost();
            using var flow = new ApplicationFlow(loader, host);
            using var lifetime = new CancellationTokenSource();
            var run = flow.Run(lifetime.Token).SuppressCancellationThrow();
            Assert.That(flow.Status.CurrentValue.Error, Is.Not.Null);
            Assert.That(host.Stopped, Is.EqualTo(1));
            Assert.That(loader.Unloaded, Is.EqualTo(1));
            flow.Restart();
            Assert.That(host.Stopped, Is.EqualTo(1));
            Assert.That(loader.Unloaded, Is.EqualTo(1));
            Assert.That(host.Prepared, Is.Zero);
            loader.CompleteNativeLoad();
            Assert.That(host.Prepared, Is.EqualTo(1));
            Assert.That(host.Started, Is.EqualTo(1));
            Assert.That(flow.Status.CurrentValue.Stage, Is.EqualTo(ApplicationStage.Playing));
            flow.Restart();
            Assert.That(host.Playing, Is.False);
            loader.CompleteNativeLoad();
            Assert.That(host.Prepared, Is.EqualTo(2));
            lifetime.Cancel();
            Assert.That(run.GetAwaiter().GetResult(), Is.True);
            Assert.That(host.Playing, Is.False);
        }

        [Test]
        public void LoadingScreenObservesProgressAndMapsApplicationStages()
        {
            var loader = new ControlledLoader();
            var host = new TestHost();
            using var flow = new ApplicationFlow(loader, host);
            using var screen = new LoadingScreenViewModel(flow.Status, flow.Restart);
            using var lifetime = new CancellationTokenSource();
            var run = flow.Run(lifetime.Token).SuppressCancellationThrow();

            loader.ReportProgress(0.5f);
            Assert.That(screen.Status.CurrentValue.Stage, Is.EqualTo("Loading habitat"));
            Assert.That(screen.Status.CurrentValue.Progress, Is.EqualTo(0.425f).Within(0.0001f));
            Assert.That(screen.Status.CurrentValue.Visible, Is.True);

            loader.CompleteNativeLoad();
            Assert.That(screen.Status.CurrentValue.Visible, Is.False);
            Assert.That(screen.Status.CurrentValue.Progress, Is.EqualTo(1));

            screen.Retry();
            Assert.That(screen.Status.CurrentValue.Visible, Is.True);
            Assert.That(host.Playing, Is.False);
            lifetime.Cancel();
            loader.CompleteNativeLoad();
            Assert.That(run.GetAwaiter().GetResult(), Is.True);
        }

        [Test]
        public void ErrorObserverCanRetryImmediatelyAfterCleanup()
        {
            var loader = new ControlledLoader { FailNext = true };
            var host = new TestHost();
            using var flow = new ApplicationFlow(loader, host);
            using var lifetime = new CancellationTokenSource();
            using var subscription = flow.Status.Subscribe(status =>
            {
                if (status.Stage == ApplicationStage.Failed)
                {
                    Assert.That(loader.Unloaded, Is.EqualTo(1));
                    flow.Restart();
                }
            });

            var run = flow.Run(lifetime.Token).SuppressCancellationThrow();
            loader.CompleteNativeLoad();
            Assert.That(host.Playing, Is.True);
            lifetime.Cancel();
            Assert.That(run.GetAwaiter().GetResult(), Is.True);
        }

        private sealed class TestHost : IGameSessionHost
        {
            public int Prepared;
            public int Started;
            public int Stopped;
            public bool Playing;

            public UniTask Prepare(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                Prepared++;
                return UniTask.CompletedTask;
            }

            public void Start()
            {
                Assert.That(Prepared, Is.GreaterThan(Started));
                Started++;
                Playing = true;
            }

            public void Stop()
            {
                Stopped++;
                Playing = false;
            }
        }

        private sealed class ControlledLoader : ISceneLoadingService
        {
            private UniTaskCompletionSource _native;
            private IProgress<float> _progress;
            public bool Loaded;
            public bool FailNext;
            public int Unloaded;

            public async UniTask Load(string scene, CancellationToken token, IProgress<float> progress = null)
            {
                if (FailNext)
                {
                    FailNext = false;
                    throw new InvalidOperationException("Test load failure");
                }

                _progress = progress;
                _native = new UniTaskCompletionSource();
                await _native.Task;
                Loaded = true;
                token.ThrowIfCancellationRequested();
            }

            public void CompleteNativeLoad() => _native.TrySetResult();

            public void ReportProgress(float progress) => _progress?.Report(progress);

            public UniTask Unload()
            {
                Unloaded++;
                Loaded = false;
                return UniTask.CompletedTask;
            }
        }
    }
}

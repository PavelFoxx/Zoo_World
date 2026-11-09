using System;
using R3;

namespace Zoo.Application
{
    public sealed class LoadingScreenViewModel : IDisposable
    {
        private readonly Action _retry;
        public ReadOnlyReactiveProperty<LoadingStatus> Status { get; }

        public LoadingScreenViewModel(ReadOnlyReactiveProperty<ApplicationStatus> status, Action retry)
        {
            _retry = retry;
            Status = status.Select(Present).ToReadOnlyReactiveProperty();
        }

        public void Retry() => _retry();

        public void Dispose() => Status.Dispose();

        private static LoadingStatus Present(ApplicationStatus status)
        {
            return status.Stage switch
            {
                ApplicationStage.Starting => new LoadingStatus(true, "Starting Zoo World", 0),
                ApplicationStage.Loading => new LoadingStatus(true, "Loading habitat", status.SceneProgress * 0.85f),
                ApplicationStage.Preparing => new LoadingStatus(true, "Preparing physics and navigation", 0.9f),
                ApplicationStage.Playing => new LoadingStatus(false, "Ready", 1),
                ApplicationStage.Failed => new LoadingStatus(true, "Could not load the habitat", 0, status.Error),
                _ => throw new ArgumentOutOfRangeException(nameof(status))
            };
        }
    }
}

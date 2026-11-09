using System.Numerics;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Zoo.Application;

namespace Zoo.Tests
{
    internal sealed class DirectNavigation : INavigationService
    {
        public int DisposeCount;

        private readonly Subject<int> _changed = new Subject<int>();
        public Observable<int> Changed => _changed;

        public UniTask<Vector3[]> FindPath(Vector3 start, Vector3 end, string profile, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return UniTask.FromResult(new[] { start, end });
        }

        public bool TryProject(Vector3 point, string profile, out Vector3 projected)
        {
            projected = point;
            return true;
        }

        public void AdvanceQueries(int budget)
        {
        }

        public void Dispose()
        {
            DisposeCount++;
            _changed.Dispose();
        }
    }
}

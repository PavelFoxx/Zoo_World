using System;
using System.Numerics;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Zoo.Domain;

namespace Zoo.Application
{
    public interface INavigationService : IDisposable
    {
        Observable<int> Changed { get; }

        UniTask<Vector3[]> FindPath(Vector3 start, Vector3 goal, string profile, CancellationToken token);
        bool TryProject(Vector3 point, string profile, out Vector3 projected);
        void AdvanceQueries(int iterations);
    }
}

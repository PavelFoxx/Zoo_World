using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading;
using Cysharp.Threading.Tasks;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using DotRecast.Detour.Io;
using R3;
using Zoo.Application;

namespace Zoo.Navigation
{
    public sealed class DotRecastNavigationService : INavigationService
    {
        private readonly Dictionary<string, DtNavMesh> _meshes = new Dictionary<string, DtNavMesh>();

        private readonly Queue<Request> _requests = new Queue<Request>();
        private readonly Subject<int> _changed = new Subject<int>();

        private readonly DtQueryDefaultFilter _filter = new DtQueryDefaultFilter();
        private bool _disposed;
        public Observable<int> Changed => _changed;
        public int Version { get; private set; }

        public void Load(string profile, byte[] bytes)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DotRecastNavigationService));
            }

            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);
            _meshes[profile] = new DtMeshSetReader().Read(reader);
            while (_requests.Count > 0)
            {
                _requests.Dequeue().Cancel();
            }

            _changed.OnNext(++Version);
        }

        private static RcVec3f Rc(Vector3 v) => new RcVec3f(v.X, v.Y, v.Z);

        private static Vector3 V(RcVec3f v) => new Vector3(v.X, v.Y, v.Z);

        public bool TryProject(Vector3 point, string profile, out Vector3 projected)
        {
            projected = point;
            if (!_meshes.TryGetValue(profile, out var mesh))
            {
                return false;
            }

            var query = new DtNavMeshQuery(mesh);
            var status = query.FindNearestPoly(Rc(point), new RcVec3f(1, 2, 1), _filter, out var polygon, out var nearest, out _);
            if (status.Failed() || polygon == 0)
            {
                return false;
            }

            projected = V(nearest);
            return true;
        }

        public UniTask<Vector3[]> FindPath(Vector3 start, Vector3 goal, string profile, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DotRecastNavigationService));
            }

            if (!_meshes.TryGetValue(profile, out var mesh))
            {
                return UniTask.FromResult(Array.Empty<Vector3>());
            }

            var query = new DtNavMeshQuery(mesh);
            query.FindNearestPoly(Rc(start), new RcVec3f(2, 2, 2), _filter, out var a, out var first, out _);
            query.FindNearestPoly(Rc(goal), new RcVec3f(1, 2, 1), _filter, out var b, out var last, out _);
            if (a == 0 || b == 0)
            {
                return UniTask.FromResult(Array.Empty<Vector3>());
            }

            var status = query.InitSlicedFindPath(a, b, first, last, _filter, 0);
            if (status.Failed())
            {
                return UniTask.FromResult(Array.Empty<Vector3>());
            }

            var request = new Request(query, first, last, b, token);
            _requests.Enqueue(request);
            return request.Completion.Task;
        }

        public void AdvanceQueries(int iterations)
        {
            // Global budget, round-robin. Query state is private to each request.
            while (iterations > 0 && _requests.Count > 0)
            {
                var request = _requests.Dequeue();
                if (request.Done)
                {
                    continue;
                }

                var status = request.Query.UpdateSlicedFindPath(Math.Min(16, iterations), out var used);
                iterations -= Math.Max(1, used);
                if (status.InProgress())
                {
                    _requests.Enqueue(request);
                }
                else
                {
                    request.Finish(status);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            while (_requests.Count > 0)
            {
                _requests.Dequeue().Cancel();
            }

            _meshes.Clear();
            _changed.Dispose();
        }

        private sealed class Request
        {
            public readonly DtNavMeshQuery Query;

            public readonly UniTaskCompletionSource<Vector3[]> Completion = new UniTaskCompletionSource<Vector3[]>();
            private readonly RcVec3f _start;
            private readonly RcVec3f _end;
            private readonly long _endRef;
            private readonly CancellationToken _token;
            private CancellationTokenRegistration _registration;
            public bool Done { get; private set; }

            public Request(DtNavMeshQuery query, RcVec3f start, RcVec3f end, long endRef, CancellationToken token)
            {
                Query = query;
                _start = start;
                _end = end;
                _endRef = endRef;
                _token = token;
                _registration = token.Register(Cancel);
                if (Done)
                {
                    _registration.Dispose();
                }
            }

            public void Cancel()
            {
                if (Done)
                {
                    return;
                }

                Done = true;
                _registration.Dispose();
                Completion.TrySetCanceled(_token);
            }

            public void Finish(DtStatus status)
            {
                if (Done)
                {
                    return;
                }

                Done = true;
                _registration.Dispose();
                if (status.Failed())
                {
                    Completion.TrySetResult(Array.Empty<Vector3>());
                    return;
                }

                var corridor = new long[256];
                status = Query.FinalizeSlicedFindPath(corridor, out var count, corridor.Length);
                if (status.Failed() || count == 0 || corridor[count - 1] != _endRef)
                {
                    Completion.TrySetResult(Array.Empty<Vector3>());
                    return;
                }

                var corners = new DtStraightPath[256];
                status = Query.FindStraightPath(_start, _end, corridor, count, corners, out var n, corners.Length, 0);
                if (status.Failed())
                {
                    Completion.TrySetResult(Array.Empty<Vector3>());
                    return;
                }

                var points = new Vector3[n];
                for (var i = 0; i < n; i++)
                {
                    points[i] = V(corners[i].pos);
                }

                Completion.TrySetResult(points);
            }
        }
    }
}

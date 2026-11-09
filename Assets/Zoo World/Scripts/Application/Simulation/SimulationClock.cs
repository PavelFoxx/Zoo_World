using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Zoo.Application
{
    // Single-threaded, simulation-driven time. No PlayerLoop, timers or worker threads.
    public sealed class SimulationClock : IDisposable
    {
        private readonly List<Wait> _waits = new List<Wait>();

        private readonly List<Wait> _ready = new List<Wait>();
        private bool _disposed;
        public double Time { get; private set; }

        public int PendingCount => _waits.Count;

        public UniTask Delay(float seconds, CancellationToken token)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SimulationClock));
            }

            if (!(seconds > 0) || float.IsInfinity(seconds))
            {
                throw new ArgumentOutOfRangeException(nameof(seconds));
            }

            if (token.IsCancellationRequested)
            {
                return UniTask.FromCanceled(token);
            }

            var wait = new Wait(this, Time + seconds);
            var index = _waits.FindIndex(item => item.Due > wait.Due);
            if (index < 0)
            {
                _waits.Add(wait);
            }
            else
            {
                _waits.Insert(index, wait);
            }

            wait.Register(token);
            return wait.Task;
        }

        public void Advance(float seconds)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SimulationClock));
            }

            if (!(seconds > 0) || float.IsInfinity(seconds))
            {
                throw new ArgumentOutOfRangeException(nameof(seconds));
            }

            Time += seconds;
            while (_waits.Count > 0 && _waits[0].Due <= Time + 0.000001)
            {
                _ready.Add(_waits[0]);
                _waits.RemoveAt(0);
            }

            foreach (var wait in _ready)
            {
                wait.Complete(_disposed);
            }

            _ready.Clear();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            while (_waits.Count > 0)
            {
                _waits[_waits.Count - 1].Complete(true);
            }
        }

        private sealed class Wait
        {
            private readonly SimulationClock _owner;

            private readonly UniTaskCompletionSource _completion = new UniTaskCompletionSource();
            private CancellationTokenRegistration _registration;
            private CancellationToken _token;
            private bool _completed;
            public double Due { get; }

            public UniTask Task => _completion.Task;

            public Wait(SimulationClock owner, double due)
            {
                _owner = owner;
                Due = due;
            }

            public void Register(CancellationToken token)
            {
                _token = token;
                _registration = token.Register(() => Complete(true));
                if (_completed)
                {
                    _registration.Dispose();
                }
            }

            public void Complete(bool canceled)
            {
                if (_completed)
                {
                    return;
                }

                _completed = true;
                _owner._waits.Remove(this);
                _registration.Dispose();
                if (canceled)
                {
                    _completion.TrySetCanceled(_token);
                }
                else
                {
                    _completion.TrySetResult();
                }
            }
        }
    }
}

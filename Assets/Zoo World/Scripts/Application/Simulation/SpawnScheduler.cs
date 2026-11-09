using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Zoo.Application
{
    public sealed class SpawnScheduler
    {
        private readonly SimulationClock _clock;
        private readonly Random _random;
        private readonly Action _spawn;

        public SpawnScheduler(SimulationClock clock, Random random, Action spawn)
        {
            _clock = clock;
            _random = random;
            _spawn = spawn;
        }

        public async UniTask Run(CancellationToken token)
        {
            while (true)
            {
                await _clock.Delay(1 + (float)_random.NextDouble(), token);
                token.ThrowIfCancellationRequested();
                _spawn();
            }
        }
    }
}

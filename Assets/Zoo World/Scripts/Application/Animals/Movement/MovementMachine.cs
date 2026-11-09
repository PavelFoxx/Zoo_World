using System;
using System.Numerics;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UniState;
using Zoo.Domain;

namespace Zoo.Application
{
    internal sealed partial class MovementMachine : ITypeResolver, IDisposable
    {
        private readonly Animal _animal;
        private readonly IPhysicsWorld _physics;
        private readonly SimulationClock _clock;
        private readonly Vector3[] _path;
        private readonly string _label;

        private readonly Subject<Unit> _arrived = new Subject<Unit>();
        private IDisposable _poseSubscription;
        private int _corner = 1;
        private bool _traveling;
        private MovementPlan _plan;

        public MovementMachine(Animal animal, IPhysicsWorld physics, SimulationClock clock, Vector3[] path, string label)
        {
            _animal = animal;
            _physics = physics;
            _clock = clock;
            _path = path;
            _label = label;
        }

        private bool Arrived => _corner >= _path.Length;

        public async UniTask Run(CancellationToken token)
        {
            using var owner = CancellationTokenSource.CreateLinkedTokenSource(token);
            _poseSubscription = _animal.Pose.Subscribe(Follow);
            var machine = new StateMachine();
            machine.SetResolver(this);
            // A bounded operation, not a periodically polled stuck detector.
            var timeout = Timeout(owner);
            try
            {
                await machine.Execute<TravelState>(owner.Token);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
            }
            finally
            {
                owner.Cancel();
                await timeout;
                _physics.Stop(_animal.Id);
            }
        }

        private async UniTask Timeout(CancellationTokenSource owner)
        {
            if (!await _clock.Delay(_animal.Definition.Behavior.StuckSeconds, owner.Token).SuppressCancellationThrow())
            {
                owner.Cancel();
            }
        }

        private void Follow(EntityPose pose)
        {
            if (Arrived)
            {
                return;
            }

            while (!Arrived && Vector3.Distance(pose.Position, _path[_corner]) < 0.18f)
            {
                _corner++;
            }

            if (Arrived)
            {
                _physics.Stop(_animal.Id);
                _arrived.OnNext(Unit.Default);
                return;
            }

            if (_traveling)
            {
                SetDirection();
            }
        }

        private void SetDirection(bool start = false)
        {
            var direction = _path[_corner] - _animal.Pose.CurrentValue.Position;
            direction.Y = 0;
            if (direction.LengthSquared() < 0.000001f)
            {
                return;
            }

            if (start)
            {
                _physics.Move(_animal.Id, Vector3.Normalize(direction), _plan.Speed, _plan.Duration);
            }
            else
            {
                _physics.Steer(_animal.Id, Vector3.Normalize(direction));
            }
        }

        public object Resolve(Type type)
        {
            if (type == typeof(TravelState))
            {
                return new TravelState(this);
            }

            if (type == typeof(RestState))
            {
                return new RestState(this);
            }

            throw new InvalidOperationException(type.Name);
        }

        public void Dispose()
        {
            _traveling = false;
            _poseSubscription?.Dispose();
            _arrived.Dispose();
            _physics.Stop(_animal.Id);
        }
    }
}

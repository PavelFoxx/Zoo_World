using System;
using System.Numerics;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniState;
using Zoo.Domain;

namespace Zoo.Application
{
    public sealed partial class FlyingAnimalBrain : AnimalBrain, ITypeResolver
    {
        private readonly Animal _animal;
        private readonly IPhysicsWorld _physics;
        private readonly SimulationClock _clock;
        private readonly WorldBounds _bounds;
        private readonly Random _random;
        private readonly FlightMovement _flight;
        private bool _returnRequested;

        public FlyingAnimalBrain(Animal animal, IPhysicsWorld physics, SimulationClock clock,
            WorldBounds bounds, Random random, FlightMovement flight)
        {
            _animal = animal;
            _physics = physics;
            _clock = clock;
            _bounds = bounds;
            _random = random;
            _flight = flight;
        }

        internal override void CheckBounds()
        {
            if (!_returnRequested && !_bounds.Contains(_animal.Pose.CurrentValue.Position))
            {
                _returnRequested = true;
                InterruptActivity();
            }
        }

        protected override async UniTask Execute(CancellationToken token)
        {
            var machine = new StateMachine();
            machine.SetResolver(this);
            await machine.Execute<WanderState>(token);
        }

        public object Resolve(Type type)
        {
            if (type == typeof(WanderState))
            {
                return new WanderState(this);
            }

            if (type == typeof(ReturnState))
            {
                return new ReturnState(this);
            }

            throw new InvalidOperationException("Unknown flying animal state: " + type);
        }

        private Vector3[] FindPath(Vector3 target)
        {
            target.Y = _bounds.Min.Y + _flight.Height;
            return _physics.IsFree(target, _animal.Definition.Body.Radius)
                ? new[] { _animal.Pose.CurrentValue.Position, target }
                : Array.Empty<Vector3>();
        }

        private async UniTask FollowPath(Vector3[] path, string label, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (path.Length > 1)
            {
                using var movement = new MovementMachine(_animal, _physics, _clock, path, label);
                await movement.Run(token);
            }

            await _clock.Delay(_animal.Definition.Behavior.RetrySeconds, token);
        }

        protected override void DisposeResources() => _physics.Stop(_animal.Id);
    }
}

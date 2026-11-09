using System;
using System.Numerics;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UniState;
using Zoo.Domain;

namespace Zoo.Application
{
    public sealed partial class GroundAnimalBrain : AnimalBrain, ITypeResolver
    {
        private readonly Animal _animal;
        private readonly IPhysicsWorld _physics;
        private readonly INavigationService _navigation;
        private readonly SimulationClock _clock;
        private readonly WorldBounds _bounds;
        private readonly Random _random;
        private readonly IDisposable _navigationSubscription;
        private bool _returnRequested;

        public GroundAnimalBrain(Animal animal, IPhysicsWorld physics, INavigationService navigation,
            SimulationClock clock, WorldBounds bounds, Random random)
        {
            _animal = animal;
            _physics = physics;
            _navigation = navigation;
            _clock = clock;
            _bounds = bounds;
            _random = random;
            _navigationSubscription = navigation.Changed.Subscribe(_ => InterruptActivity());
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

            throw new InvalidOperationException("Unknown ground animal state: " + type);
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

        protected override void DisposeResources()
        {
            _navigationSubscription.Dispose();
            _physics.Stop(_animal.Id);
        }
    }
}

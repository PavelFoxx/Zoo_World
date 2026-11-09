using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using Zoo.Domain;

namespace Zoo.Application
{
    internal sealed class AnimalLifecycle : IDisposable
    {
        private readonly EntityWorld _world;
        private readonly IPhysicsWorld _physics;
        private readonly INavigationService _navigation;
        private readonly SimulationClock _clock;
        private readonly WorldBounds _bounds;
        private readonly Random _random;
        private readonly Action<Exception> _onFailure;

        private readonly Dictionary<int, AnimalBrain> _brains = new Dictionary<int, AnimalBrain>();
        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();

        public AnimalLifecycle(EntityWorld world, IPhysicsWorld physics, INavigationService navigation,
            SimulationClock clock, WorldBounds bounds, Random random, Action<Exception> onFailure)
        {
            _world = world;
            _physics = physics;
            _navigation = navigation;
            _clock = clock;
            _bounds = bounds;
            _random = random;
            _onFailure = onFailure;
            physics.Poses.Subscribe(AcceptPose).AddTo(_subscriptions);
            world.Created.Subscribe(Add).AddTo(_subscriptions);
            world.Removed.Subscribe(Remove).AddTo(_subscriptions);
        }

        public void CheckBounds()
        {
            foreach (var brain in _brains.Values)
            {
                brain.CheckBounds();
            }
        }

        private void AcceptPose(PhysicsPose pose)
        {
            if (_world.TryGet(pose.Id, out var entity))
            {
                entity.AcceptPhysicsPose(pose.Pose);
            }
        }

        private void Add(Entity entity)
        {
            if (!(entity is Animal animal))
            {
                return;
            }

            var flight = animal.Definition.Movement as FlightMovement;
            float? flightAltitude = flight == null ? null : _bounds.Min.Y + flight.Height;
            _physics.Add(animal, flightAltitude);
            AnimalBrain brain = flight == null
                ? new GroundAnimalBrain(animal, _physics, _navigation, _clock, _bounds, _random)
                : new FlyingAnimalBrain(animal, _physics, _clock, _bounds, _random, flight);
            _brains.Add(animal.Id, brain);
            brain.Run().Forget(_onFailure, false);
        }

        private void Remove(Entity entity)
        {
            if (_brains.TryGetValue(entity.Id, out var brain))
            {
                brain.Dispose();
                _brains.Remove(entity.Id);
            }

            _physics.Remove(entity.Id);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
            foreach (var pair in _brains)
            {
                pair.Value.Dispose();
                _physics.Remove(pair.Key);
            }

            _brains.Clear();
        }
    }
}

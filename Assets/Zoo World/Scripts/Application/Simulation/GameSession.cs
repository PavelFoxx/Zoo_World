using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Zoo.Domain;

namespace Zoo.Application
{
    public sealed class GameSession : IDisposable
    {
        public const float FixedDelta = 0.02f;
        private const int StepsPerBoundaryCheck = (int)(1f / FixedDelta);
        private const int NavigationIterationsPerStep = 64;
        private readonly IPhysicsWorld _physics;
        private readonly INavigationService _navigation;
        private readonly AnimalLifecycle _animals;
        private readonly SpawnScheduler _spawns;
        private readonly IDisposable _contactSubscription;

        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private readonly Queue<Contact> _contacts = new Queue<Contact>();
        private readonly Subject<CollisionEvent> _collisions = new Subject<CollisionEvent>();
        private readonly InteractionResolver _interactions;
        private double _accumulator;
        private int _stepsUntilBoundaryCheck = StepsPerBoundaryCheck;
        private bool _started;
        private bool _disposed;
        private bool _advancing;
        private Exception _failure;

        public EntityWorld World { get; } = new EntityWorld();
        public SimulationClock Clock { get; } = new SimulationClock();
        public WorldBounds Bounds { get; }
        public Observable<CollisionEvent> Collisions => _collisions;

        // Ownership of physics and navigation transfers to the session after construction succeeds.
        public GameSession(AnimalDefinition[] definitions, IPhysicsWorld physics, INavigationService navigation,
            WorldBounds bounds, int seed = 1701,
            Func<EntityWorld, IPhysicsWorld, Random, IEnumerable<IInteraction>> createInteractions = null)
        {
            var random = new Random(seed);
            var spawner = new AnimalSpawner(definitions, World, physics, navigation, bounds, random);
            _physics = physics;
            _navigation = navigation;
            Bounds = bounds;
            var interactions = createInteractions?.Invoke(World, physics, random) ?? new IInteraction[]
            {
                new EatingInteraction(World, random),
                new BounceInteraction(physics)
            };
            _interactions = new InteractionResolver(Collisions, interactions, HandleFailure);
            if (physics is IContactResponseControl contactResponse)
            {
                contactResponse.SetContactFilter(_interactions.ShouldSuppressPhysicalResponse);
            }

            _spawns = new SpawnScheduler(Clock, random, spawner.SpawnRandom);
            _animals = new AnimalLifecycle(World, physics, navigation, Clock, bounds, random, HandleFailure);
            _contactSubscription = physics.Contacts.Subscribe(contact => _contacts.Enqueue(contact));
        }

        public void Start()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(GameSession));
            }

            if (_started)
            {
                return;
            }

            _started = true;
            _spawns.Run(_lifetime.Token).Forget(HandleFailure, false);
        }

        private void HandleFailure(Exception error)
        {
            if (error is OperationCanceledException && _lifetime.IsCancellationRequested)
            {
                return;
            }

            _failure = error;
        }

        public void Advance(float elapsed)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(GameSession));
            }

            Guard.NonNegative(elapsed, nameof(elapsed));
            if (_advancing)
            {
                throw new InvalidOperationException("Reentrant simulation advance.");
            }

            _advancing = true;
            try
            {
                _accumulator += elapsed;
                while (_accumulator + 0.0000001 >= FixedDelta)
                {
                    _accumulator -= FixedDelta;
                    _navigation.AdvanceQueries(NavigationIterationsPerStep);
                    _physics.Step(FixedDelta);
                    // Actions may remove bodies, so publish collisions only after Step has returned.
                    while (_contacts.Count > 0)
                    {
                        var contact = _contacts.Dequeue();
                        if (World.TryGet(contact.A, out var first) && World.TryGet(contact.B, out var second))
                        {
                            _collisions.OnNext(new CollisionEvent(first, second));
                        }
                    }

                    Clock.Advance(FixedDelta);
                    if (--_stepsUntilBoundaryCheck == 0)
                    {
                        _stepsUntilBoundaryCheck = StepsPerBoundaryCheck;
                        _animals.CheckBounds();
                    }

                    if (_failure != null)
                    {
                        throw new InvalidOperationException("Simulation operation failed.", _failure);
                    }
                }
            }
            finally
            {
                _advancing = false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _lifetime.Cancel();
            _contactSubscription.Dispose();
            _interactions.Dispose();
            _collisions.Dispose();
            if (_physics is IContactResponseControl contactResponse)
            {
                contactResponse.SetContactFilter(null);
            }

            World.Dispose();
            _animals.Dispose();
            _contacts.Clear();
            Clock.Dispose();
            _lifetime.Dispose();
            _physics.Dispose();
            _navigation.Dispose();
        }
    }
}

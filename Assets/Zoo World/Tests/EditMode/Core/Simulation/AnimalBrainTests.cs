using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using R3;
using Zoo.Application;
using Zoo.Domain;

namespace Zoo.Tests
{
    public sealed class AnimalBrainTests
    {
        private static WorldBounds Bounds => new WorldBounds(new Vector3(-20, 0, -20), new Vector3(20, 0, 20));

        [Test]
        public void GroundNavigationChangeCancelsPendingRouteBeforeStartingReplacement()
        {
            var physics = new TestPhysics();
            var navigation = new ControlledNavigation();
            using var session = new GameSession(new[] { SimulationTests.Snake }, physics, navigation, Bounds);
            var animal = session.World.Spawn(SimulationTests.Snake, Vector3.Zero);
            var original = navigation.Queries[0];

            navigation.NotifyChanged();

            Assert.That(original.Token.IsCancellationRequested, Is.True);
            Assert.That(navigation.Queries.Count, Is.EqualTo(2));
            Assert.That(session.Clock.PendingCount, Is.Zero);
            navigation.Queries[1].Complete(new[] { Vector3.Zero, new Vector3(10, 0, 0) });
            session.Advance(0.5f);
            Assert.That(animal.Pose.CurrentValue.Position.X, Is.GreaterThan(0.9f));
        }

        [Test]
        public void RemovingAnimalRejectsLateResultFromCanceledNavigation()
        {
            var physics = new TestPhysics();
            var navigation = new ControlledNavigation { CompleteOnCancellation = false };
            using var session = new GameSession(new[] { SimulationTests.Snake }, physics, navigation, Bounds);
            var animal = session.World.Spawn(SimulationTests.Snake, Vector3.Zero);
            var query = navigation.Queries[0];

            session.World.Remove(animal.Id);
            Assert.That(query.Token.IsCancellationRequested, Is.True);
            query.Complete(new[] { Vector3.Zero, new Vector3(10, 0, 0) });

            Assert.That(physics.Bodies, Is.Empty);
            Assert.That(session.Clock.PendingCount, Is.Zero);
            Assert.That(navigation.Queries.Count, Is.EqualTo(1));
            Assert.DoesNotThrow(() => session.Advance(1));
        }

        [Test]
        public void FlyingAnimalKeepsItsFlightWhenGroundNavigationChanges()
        {
            var definition = new AnimalDefinition("flying-test-animal", FoodRole.Prey,
                new BodyDefinition(0.3f, 1), new FlightMovement(2, 3),
                new InteractionProfile(), new BehaviorDefinition());
            var physics = new TestPhysics();
            var navigation = new ControlledNavigation();
            using var session = new GameSession(new[] { definition }, physics, navigation, Bounds);
            var animal = session.World.Spawn(definition, new Vector3(0, 3, 0));
            var movementRestarts = 0;
            using var subscription = animal.MovementStarted.Subscribe(_ => movementRestarts++);

            navigation.NotifyChanged();
            session.Advance(0.1f);

            Assert.That(navigation.Queries, Is.Empty);
            Assert.That(movementRestarts, Is.Zero);
            Assert.That(Vector3.Distance(animal.Pose.CurrentValue.Position, new Vector3(0, 3, 0)),
                Is.GreaterThan(0.15f));
        }

        private sealed class ControlledNavigation : INavigationService
        {
            private readonly Subject<int> _changed = new Subject<int>();
            public readonly List<Query> Queries = new List<Query>();
            public bool CompleteOnCancellation = true;
            public Observable<int> Changed => _changed;

            public async UniTask<Vector3[]> FindPath(Vector3 start, Vector3 goal, string profile, CancellationToken token)
            {
                var query = new Query(token);
                Queries.Add(query);
                using var registration = CompleteOnCancellation
                    ? token.Register(() => query.Completion.TrySetCanceled(token))
                    : default;
                return await query.Completion.Task;
            }

            public void NotifyChanged() => _changed.OnNext(1);

            public bool TryProject(Vector3 point, string profile, out Vector3 projected)
            {
                projected = point;
                return true;
            }

            public void AdvanceQueries(int iterations)
            {
            }

            public void Dispose() => _changed.Dispose();
        }

        private sealed class Query
        {
            public readonly UniTaskCompletionSource<Vector3[]> Completion = new UniTaskCompletionSource<Vector3[]>();
            public CancellationToken Token { get; }

            public Query(CancellationToken token)
            {
                Token = token;
            }

            public void Complete(Vector3[] path) => Completion.TrySetResult(path);
        }
    }
}

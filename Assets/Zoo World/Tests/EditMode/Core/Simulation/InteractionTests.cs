using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using R3;
using Zoo.Application;
using Zoo.Domain;

namespace Zoo.Tests
{
    public sealed class InteractionTests
    {
        private static AnimalDefinition Frog => SimulationTests.Frog;
        private static AnimalDefinition Snake => SimulationTests.Snake;
        private static WorldBounds Bounds => new WorldBounds(new Vector3(-20, 0, -20), new Vector3(20, 0, 20));

        [Test]
        public void CollisionCarriesParticipantsAndEatingUpdatesCountersOnceAfterPhysicsStep()
        {
            var physics = new TestPhysics();
            using var session = new GameSession(new[] { Snake, Frog }, physics, new DirectNavigation(), Bounds);
            using var vm = new ZooViewModel(session.World);
            var snake = session.World.Spawn(Snake, Vector3.Zero);
            var frog = session.World.Spawn(Frog, Vector3.UnitX);
            var meals = 0;
            var removals = 0;
            var collisions = new List<CollisionEvent>();
            using var m = vm.Animals.Single(a => a.Id == snake.Id).Ate.Subscribe(_ => meals++);
            using var r = vm.Removed.Subscribe(_ => removals++);
            using var c = session.Collisions.Subscribe(collisions.Add);

            physics.PublishContact(new Contact(frog.Id, snake.Id));
            physics.PublishContact(new Contact(snake.Id, frog.Id));
            physics.PublishContact(new Contact(snake.Id, int.MaxValue));
            Assert.That(meals, Is.Zero);
            Assert.That(collisions, Is.Empty);
            session.Advance(GameSession.FixedDelta);

            Assert.That(collisions.Count, Is.EqualTo(1));
            Assert.That(collisions[0].First, Is.SameAs(snake));
            Assert.That(collisions[0].Second, Is.SameAs(frog));
            Assert.That(meals, Is.EqualTo(1));
            Assert.That(removals, Is.EqualTo(1));
            Assert.That(vm.DeadPrey.CurrentValue, Is.EqualTo(1));
            Assert.That(session.World.Entities.Count(), Is.EqualTo(1));
            Assert.That(physics.Impulses, Is.Empty);
        }

        [TestCase("Delicious!")]
        [TestCase("Добыча поймана")]
        [TestCase(null)]
        public void EatingMessageReachesViewModelThroughInteractionEvent(string message)
        {
            var physics = new TestPhysics();
            using var session = new GameSession(new[] { Snake, Frog }, physics, new DirectNavigation(), Bounds,
                createInteractions: (world, bodies, random) => new[] { new EatingInteraction(world, random, message) });
            using var vm = new ZooViewModel(session.World);
            var predator = session.World.Spawn(Snake, Vector3.Zero);
            var victim = session.World.Spawn(Frog, Vector3.UnitX);
            var meals = new List<AnimalMeal>();
            using var subscription = vm.Animals.Single(animal => animal.Id == predator.Id).Ate.Subscribe(meals.Add);

            physics.PublishContact(new Contact(predator.Id, victim.Id));
            physics.PublishContact(new Contact(predator.Id, victim.Id));
            session.Advance(GameSession.FixedDelta);

            Assert.That(meals.Count, Is.EqualTo(1));
            Assert.That(meals[0].Predator, Is.SameAs(predator));
            Assert.That(meals[0].Victim, Is.SameAs(victim));
            Assert.That(meals[0].Message, Is.EqualTo(message));
            Assert.That(vm.DeadPrey.CurrentValue, Is.EqualTo(1));
        }

        [TestCase(false, "snake", "snake")]
        [TestCase(true, "snake", "snake")]
        [TestCase(false, "renamed-predator", "renamed-predator")]
        [TestCase(true, "renamed-predator", "renamed-predator")]
        [TestCase(false, "wolf", "fox")]
        [TestCase(true, "wolf", "fox")]
        public void MutualEatingUsesConfiguredRulesAndResolvesOnlyOnce(bool reverseContact, string firstTypeId, string secondTypeId)
        {
            var template = Snake;
            var firstDefinition = new AnimalDefinition(firstTypeId, FoodRole.Predator, template.Body, template.Movement,
                new InteractionProfile(new Dictionary<FoodRole, IInteractionRule> { { FoodRole.Predator, new EatRule() } }), template.Behavior);
            var secondDefinition = new AnimalDefinition(secondTypeId, FoodRole.Predator, template.Body, template.Movement,
                new InteractionProfile(new Dictionary<FoodRole, IInteractionRule> { { FoodRole.Predator, new EatRule() } }), template.Behavior);
            var physics = new TestPhysics();
            using var session = new GameSession(new[] { firstDefinition }, physics, new DirectNavigation(), Bounds, 1701);
            var meals = 0;
            using var subscription = session.World.Eaten.Subscribe(_ => meals++);
            var firstWasEaten = new HashSet<bool>();
            for (var i = 0; i < 32; i++)
            {
                var first = session.World.Spawn(firstDefinition, Vector3.Zero);
                var second = session.World.Spawn(secondDefinition, Vector3.Zero);
                Assert.That(physics.ContactFilter(first, second), Is.True);

                var forward = new Contact(first.Id, second.Id);
                var reverse = new Contact(second.Id, first.Id);
                physics.PublishContact(reverseContact ? reverse : forward);
                physics.PublishContact(reverseContact ? forward : reverse);
                session.Advance(GameSession.FixedDelta);

                Assert.That(session.World.Entities.Count(), Is.EqualTo(1));
                Assert.That(meals, Is.EqualTo(i + 1));
                firstWasEaten.Add(!first.IsActive);
                session.World.Remove(session.World.Entities.Single().Id);
            }

            Assert.That(firstWasEaten, Is.EquivalentTo(new[] { true, false }));
        }

        [TestCase("frog")]
        [TestCase("renamed-prey")]
        public void RoleRuleAppliesRegardlessOfTargetTypeId(string targetTypeId)
        {
            using var world = new EntityWorld();
            using var physics = new TestPhysics();
            using var collisions = new Subject<CollisionEvent>();
            var profile = new InteractionProfile(
                new Dictionary<FoodRole, IInteractionRule> { { FoodRole.Prey, new BounceRule(4) } });
            var definition = new AnimalDefinition("snake", FoodRole.Predator, Snake.Body, Snake.Movement, profile, Snake.Behavior);
            var snake = world.Spawn(definition, Vector3.Zero);
            var targetDefinition = new AnimalDefinition(targetTypeId, FoodRole.Prey, Frog.Body, Frog.Movement,
                Frog.Interactions, Frog.Behavior);
            var frog = world.Spawn(targetDefinition, Vector3.UnitX);
            using var resolver = new InteractionResolver(collisions, new IInteraction[]
            {
                new EatingInteraction(world, new Random(0)), new BounceInteraction(physics)
            });

            Assert.That(resolver.ShouldSuppressPhysicalResponse(snake, frog), Is.False);
            collisions.OnNext(new CollisionEvent(frog, snake));

            Assert.That(frog.IsActive, Is.True);
            Assert.That(physics.Impulses, Is.EqualTo(new[] { -4 * Vector3.UnitX, 4 * Vector3.UnitX }));
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void BounceRequestsOnePairOfOpposingImpulses(bool strongerFirst, bool samePosition)
        {
            using var world = new EntityWorld();
            using var physics = new TestPhysics();
            using var collisions = new Subject<CollisionEvent>();
            var profile = new InteractionProfile(roles: new Dictionary<FoodRole, IInteractionRule>
            {
                { FoodRole.Prey, new BounceRule(3) }
            });
            var stronger = new AnimalDefinition("frog", FoodRole.Prey, Frog.Body, Frog.Movement, profile, Frog.Behavior);
            var first = world.Spawn(strongerFirst ? stronger : Frog, Vector3.Zero);
            var second = world.Spawn(strongerFirst ? Frog : stronger, samePosition ? Vector3.Zero : Vector3.UnitX);
            using var resolver = new InteractionResolver(collisions, new[] { new BounceInteraction(physics) });

            collisions.OnNext(new CollisionEvent(second, first));

            Assert.That(physics.Impulses, Is.EqualTo(new[] { -3 * Vector3.UnitX, 3 * Vector3.UnitX }));
        }

        [TestCase(true, true, InteractionType.Eating)]
        [TestCase(false, true, InteractionType.Bounce)]
        [TestCase(false, false, null)]
        public void EnumOrderSelectsOnlyFirstSuitableInteraction(bool canEat, bool canBounce, InteractionType? expected)
        {
            using var world = new EntityWorld();
            using var collisions = new Subject<CollisionEvent>();
            var executed = new List<InteractionType>();
            using var resolver = new InteractionResolver(collisions, new IInteraction[]
            {
                new RecordingInteraction(InteractionType.Bounce, canBounce, executed),
                new RecordingInteraction(InteractionType.Eating, canEat, executed)
            });
            var first = world.Spawn(Frog, Vector3.Zero);
            var second = world.Spawn(Frog, Vector3.UnitX);

            Assert.That(resolver.ShouldSuppressPhysicalResponse(first, second), Is.EqualTo(canEat));
            Assert.That(executed, Is.Empty, "A physics query must not execute interactions.");
            collisions.OnNext(new CollisionEvent(first, second));

            Assert.That(executed, Is.EqualTo(expected.HasValue ? new[] { expected.Value } : Array.Empty<InteractionType>()));
        }

        [Test]
        public void InvalidOrRemovedParticipantsAreIgnoredAndDisposalUnsubscribes()
        {
            using var world = new EntityWorld();
            using var collisions = new Subject<CollisionEvent>();
            var executed = new List<InteractionType>();
            using var resolver = new InteractionResolver(collisions,
                new[] { new RecordingInteraction(InteractionType.Bounce, true, executed) });
            var first = world.Spawn(Frog, Vector3.Zero);
            var removed = world.Spawn(Frog, Vector3.UnitX);
            var second = world.Spawn(Frog, -Vector3.UnitX);
            world.Remove(removed.Id);

            collisions.OnNext(default);
            collisions.OnNext(new CollisionEvent(first, first));
            collisions.OnNext(new CollisionEvent(first, removed));
            Assert.That(resolver.ShouldSuppressPhysicalResponse(first, removed), Is.False);
            resolver.Dispose();
            collisions.OnNext(new CollisionEvent(first, second));

            Assert.That(executed, Is.Empty);
        }

        [Test]
        public void DuplicateInteractionTypesAreRejected()
        {
            using var collisions = new Subject<CollisionEvent>();
            var executed = new List<InteractionType>();
            Assert.Throws<ArgumentException>(() => new InteractionResolver(collisions, new[]
            {
                new RecordingInteraction(InteractionType.Eating, true, executed),
                new RecordingInteraction(InteractionType.Eating, true, executed)
            }));
        }

        [Test]
        public void PhysicalResponseQueriesDoNotAffectRandomDuelOutcome()
        {
            using var world = new EntityWorld();
            using var collisions = new Subject<CollisionEvent>();
            const int seed = 14;
            var first = world.Spawn(Snake, Vector3.Zero);
            var second = world.Spawn(Snake, Vector3.Zero);
            using var resolver = new InteractionResolver(collisions, new[] { new EatingInteraction(world, new Random(seed)) });
            for (var i = 0; i < 20; i++)
            {
                Assert.That(resolver.ShouldSuppressPhysicalResponse(first, second), Is.True);
            }

            collisions.OnNext(new CollisionEvent(second, first));

            Assert.That(first.IsActive, Is.EqualTo(new Random(seed).Next(2) == 0));
            Assert.That(second.IsActive, Is.Not.EqualTo(first.IsActive));
        }

        [Test]
        public void SessionReportsInteractionFailure()
        {
            var physics = new TestPhysics();
            var error = new InvalidOperationException("Interaction failed.");
            using var session = new GameSession(new[] { Frog }, physics, new DirectNavigation(), Bounds,
                createInteractions: (world, bodyWorld, random) => new[] { new FailingInteraction(error) });
            var first = session.World.Spawn(Frog, Vector3.Zero);
            var second = session.World.Spawn(Frog, Vector3.UnitX);
            physics.PublishContact(new Contact(first.Id, second.Id));

            var failure = Assert.Throws<InvalidOperationException>(() => session.Advance(GameSession.FixedDelta));

            Assert.That(failure.InnerException, Is.SameAs(error));
        }

        private sealed class RecordingInteraction : IInteraction
        {
            private readonly bool _canExecute;
            private readonly List<InteractionType> _executed;
            public InteractionType Type { get; }
            public bool SuppressesPhysicalResponse => Type == InteractionType.Eating;

            public RecordingInteraction(InteractionType type, bool canExecute, List<InteractionType> executed)
            {
                Type = type;
                _canExecute = canExecute;
                _executed = executed;
            }

            public bool CanExecute(in CollisionEvent collision) => _canExecute;
            public void Execute(in CollisionEvent collision) => _executed.Add(Type);
        }

        private sealed class FailingInteraction : IInteraction
        {
            private readonly Exception _error;
            public InteractionType Type => InteractionType.Eating;
            public bool SuppressesPhysicalResponse => false;
            public FailingInteraction(Exception error) => _error = error;
            public bool CanExecute(in CollisionEvent collision) => true;
            public void Execute(in CollisionEvent collision) => throw _error;
        }
    }
}

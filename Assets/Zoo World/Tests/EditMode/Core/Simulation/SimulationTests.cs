using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using R3;
using Zoo.Application;
using Zoo.Domain;
using Zoo.Navigation;
using Zoo.Navigation.Baking;

namespace Zoo.Tests
{
    public sealed class SimulationTests
    {
        public static AnimalDefinition Frog => new AnimalDefinition(
            "frog", FoodRole.Prey,
            new BodyDefinition(0.45f, 1),
            new JumpMovement(2, 0.35f, 1.1f, 0.7f),
            new InteractionProfile(new Dictionary<FoodRole, IInteractionRule>
            {
                { FoodRole.Prey, new BounceRule() }
            }),
            new BehaviorDefinition());

        public static AnimalDefinition Snake => new AnimalDefinition(
            "snake", FoodRole.Predator,
            new BodyDefinition(0.5f, 2),
            new LinearMovement(2),
            new InteractionProfile(new Dictionary<FoodRole, IInteractionRule>
            {
                { FoodRole.Prey, new EatRule() },
                { FoodRole.Predator, new EatRule() }
            }),
            new BehaviorDefinition());

        private static WorldBounds Bounds => new WorldBounds(new Vector3(-20, 0, -20), new Vector3(20, 0, 20));

        [Test]
        public void PoseNotificationDoesNotNeedViewModelRefresh()
        {
            using var world = new EntityWorld();
            var animal = world.Spawn(Frog, Vector3.Zero);
            using var vm = new AnimalViewModel(animal);
            var notifications = 0;
            using var subscription = vm.Pose.Subscribe(_ => notifications++);
            animal.AcceptPhysicsPose(new EntityPose(Vector3.One, Quaternion.Identity));
            animal.AcceptPhysicsPose(new EntityPose(Vector3.One, Quaternion.Identity));
            Assert.That(notifications, Is.EqualTo(2));
            Assert.That(vm.Pose.CurrentValue.Position, Is.EqualTo(Vector3.One));
        }

        [Test]
        public void SharedMovementHasNoSharedRuntimeProgress()
        {
            var physics = new TestPhysics();
            using var session = new GameSession(new[] { Frog }, physics, new DirectNavigation(), Bounds, 42);
            var a = session.World.Spawn(Frog, Vector3.Zero);
            session.Advance(0.2f);
            var b = session.World.Spawn(Frog, new Vector3(5, 0, 5));
            session.Advance(0.18f);
            Assert.That(a.Behavior.CurrentValue, Does.Contain("Rest"));
            Assert.That(b.Behavior.CurrentValue, Does.Contain("Travel"));
        }

        [Test]
        public void RemovingNpcCancelsItsNestedStateWaitsAndMovement()
        {
            var physics = new TestPhysics();
            using var session = new GameSession(new[] { Frog }, physics, new DirectNavigation(), Bounds);
            var a = session.World.Spawn(Frog, Vector3.Zero);
            session.Advance(0.1f);
            session.World.Remove(a.Id);
            Assert.That(physics.Bodies, Is.Empty);
            Assert.That(session.Clock.PendingCount, Is.Zero);
            session.Advance(2);
        }

        [Test]
        public void SpawnSchedulerUsesOneToTwoSecondIntervals()
        {
            using var session = new GameSession(new[] { Frog }, new TestPhysics(), new DirectNavigation(), Bounds);
            var times = new List<double>
            {
                0
            };
            using var subscription = session.World.Created.Subscribe(_ => times.Add(session.Clock.Time));
            session.Start();
            for (var i = 0; i < 500; i++)
            {
                session.Advance(0.02f);
            }

            Assert.That(times.Count, Is.GreaterThan(5));
            for (var i = 1; i < times.Count; i++)
            {
                Assert.That(times[i] - times[i - 1], Is.InRange(0.98, 2.02));
            }
        }

        [Test]
        public void LeavingWorldBoundsInterruptsWanderAtTheNextSecond()
        {
            var physics = new TestPhysics();
            using var session = new GameSession(new[] { Snake }, physics, new DirectNavigation(), Bounds);
            var animal = session.World.Spawn(Snake, Vector3.Zero);
            session.Advance(1);
            physics.Bodies[animal.Id] = new Vector3(25, 0, 0);
            session.Advance(0.98f);
            Assert.That(animal.Pose.CurrentValue.Position.X, Is.GreaterThan(Bounds.Max.X));
            Assert.That(animal.Behavior.CurrentValue, Does.StartWith("Wander"));

            session.Advance(GameSession.FixedDelta);
            Assert.That(animal.Behavior.CurrentValue, Does.StartWith("Return"));

            session.Advance(5);
            Assert.That(Bounds.Contains(animal.Pose.CurrentValue.Position), Is.True);
            Assert.That(animal.Behavior.CurrentValue, Does.StartWith("Wander"));
        }

        [Test]
        public void SpawningWaitsForFreeSpaceAndContinuesAfterItBecomesAvailable()
        {
            var physics = new TestPhysics { SpaceAvailable = false };
            using var session = new GameSession(new[] { Frog }, physics, new DirectNavigation(), Bounds);
            session.Start();
            session.Advance(5);
            Assert.That(session.World.Entities, Is.Empty);

            physics.SpaceAvailable = true;
            session.Advance(2.1f);
            Assert.That(session.World.Entities.Any(), Is.True);
            Assert.That(physics.Bodies.Count, Is.EqualTo(session.World.Entities.Count()));
        }

        [Test]
        public void DisposingSessionCancelsAnimalsAndReleasesAdaptersOnce()
        {
            var physics = new TestPhysics();
            var navigation = new DirectNavigation();
            var session = new GameSession(new[] { Frog }, physics, navigation, Bounds);
            session.Start();
            session.World.Spawn(Frog, Vector3.Zero);
            session.Advance(0.1f);

            session.Dispose();
            session.Dispose();

            Assert.That(session.Clock.PendingCount, Is.Zero);
            Assert.That(session.World.Entities, Is.Empty);
            Assert.That(physics.Bodies, Is.Empty);
            Assert.That(physics.DisposeCount, Is.EqualTo(1));
            Assert.That(navigation.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void ClockCancellationDoesNotLeavePendingWaits()
        {
            using var clock = new SimulationClock();
            using var token = new CancellationTokenSource();
            var wait = clock.Delay(10, token.Token).SuppressCancellationThrow();
            token.Cancel();
            Assert.That(wait.GetAwaiter().GetResult(), Is.True);
            Assert.That(clock.PendingCount, Is.Zero);
        }

        [Test]
        public void FrogDefinitionHasFixedUnobstructedJumpDistance()
        {
            var plan = Frog.Movement.Plan(10);
            Assert.That(plan.Speed * plan.Duration, Is.EqualTo(2).Within(0.0001));
        }

        [Test]
        public void NewSpeciesUsesExistingBehaviorWithoutClass()
        {
            var definition = new AnimalDefinition("crab", FoodRole.Prey, new BodyDefinition(0.3f, 1), new LinearMovement(1), new InteractionProfile(), new BehaviorDefinition());
            using var session = new GameSession(new[] { definition }, new TestPhysics(), new DirectNavigation(), Bounds);
            var crab = session.World.Spawn(definition, Vector3.Zero);
            session.Advance(0.5f);
            Assert.That(crab.GetType(), Is.EqualTo(typeof(Animal)));
            Assert.That(crab.Pose.CurrentValue.Position.Length(), Is.GreaterThan(0.4f));
        }

        [Test]
        public void DotRecastFindsPathAroundBoxWithoutUnity()
        {
            var vertices = new List<float>();
            var triangles = new List<int>();
            Box(vertices, triangles, new Vector3(0, -0.2f, 0), new Vector3(20, 0.4f, 20));
            Box(vertices, triangles, new Vector3(0, 1, 0), new Vector3(3, 2, 7));
            var bytes = NavMeshBaker.Bake(vertices.ToArray(), triangles.ToArray(), 0.6f, 1.2f, 0.25f, 40);
            using var navigation = new DotRecastNavigationService();
            navigation.Load("ground", bytes);
            var path = navigation.FindPath(new Vector3(-5, 0, 0), new Vector3(5, 0, 0), "ground", CancellationToken.None);
            navigation.AdvanceQueries(10000);
            var points = path.GetAwaiter().GetResult();
            Assert.That(points.Length, Is.GreaterThan(2));
            Assert.That(points.Any(p => Math.Abs(p.Z) > 3.5f), Is.True);
            using var canceled = new CancellationTokenSource();
            var pending = navigation.FindPath(new Vector3(-5, 0, 0), new Vector3(5, 0, 0), "ground", canceled.Token).SuppressCancellationThrow();
            canceled.Cancel();
            navigation.AdvanceQueries(10000);
            Assert.That(pending.GetAwaiter().GetResult().IsCanceled, Is.True);
        }

        public static void Box(List<float> verts, List<int> tris, Vector3 center, Vector3 size)
        {
            var offset = verts.Count / 3;
            foreach (var p in new[]
            {
                new Vector3(-1, -1, -1),
                new Vector3(-1, -1, 1),
                new Vector3(1, -1, 1),
                new Vector3(1, -1, -1),
                new Vector3(-1, 1, -1),
                new Vector3(-1, 1, 1),
                new Vector3(1, 1, 1),
                new Vector3(1, 1, -1)
            })
            {
                var v = center + p * size / 2;
                verts.Add(v.X);
                verts.Add(v.Y);
                verts.Add(v.Z);
            }

            foreach (var i in new[]
            {
                4, 5, 6, 4, 6, 7,
                0, 2, 1, 0, 3, 2,
                0, 4, 7, 0, 7, 3,
                1, 2, 6, 1, 6, 5,
                0, 1, 5, 0, 5, 4,
                3, 7, 6, 3, 6, 2
            })
            {
                tris.Add(offset + i);
            }
        }
    }
}

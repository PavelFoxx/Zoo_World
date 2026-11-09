#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using R3;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zoo.Application;
using Zoo.Composition;
using Zoo.Domain;
using Zoo.Physics.Unity;
using Zoo.Unity;
using Zenject;
using V = System.Numerics.Vector3;
using Q = System.Numerics.Quaternion;
using Object = UnityEngine.Object;

namespace Zoo.Tests
{
    public sealed class PresentationTests
    {
        private GameObject _root;
        private NightPoolViewPool _pool;
        private UnityPhysicsWorld _physics;
        private AnimalView _view;
        private AnimalViewPresenter _presenter;
        private EntityWorld _world;
        private ZooViewModel _vm;

        private static AnimalDefinition Snake => new AnimalDefinition("snake", FoodRole.Predator, new BodyDefinition(0.5f, 2), new LinearMovement(2), new InteractionProfile(), new BehaviorDefinition());

        private static AnimalDefinition Frog => new AnimalDefinition("frog", FoodRole.Prey, new BodyDefinition(0.45f, 1), new JumpMovement(2, 0.35f, 1.1f, 0.7f), new InteractionProfile(), new BehaviorDefinition());

        [SetUp]
        public void Setup()
        {
            _root = new GameObject("Test objects");
            var container = new DiContainer();
            ZenjectManagersInstaller.Install(container);
            _pool = new NightPoolViewPool(container);
            _world = new EntityWorld();
            _vm = new ZooViewModel(_world);
        }

        [TearDown]
        public void Cleanup()
        {
            if (_view != null)
            {
                _view.Unbind();
                _pool.Return(_view.gameObject);
                _view = null;
            }

            _physics?.Dispose();
            _physics = null;
            _vm.Dispose();
            _world.Dispose();
            _pool.Dispose();
            Object.Destroy(_root);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ReusedViewDoesNotObserveOldEntity()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<AnimalView>("Assets/Zoo World/Prefabs/Animals/Snake.prefab");
            var a = _world.Spawn(Snake, V.Zero);
            var b = _world.Spawn(Snake, new V(5, 0, 5));
            _presenter = _pool.Rent<AnimalViewPresenter>(prefab.gameObject, _root.transform);
            _view = _presenter.View;
            var id = _view.GetInstanceID();
            var previousPresenter = _presenter;
            var previousContext = _view.GetComponent<GameObjectContext>().Container;
            _presenter.Bind(_vm.Animals.Single(x => x.Id == a.Id));
            _view.Unbind();
            _pool.Return(_view.gameObject);
            _presenter = _pool.Rent<AnimalViewPresenter>(prefab.gameObject, _root.transform);
            _view = _presenter.View;
            _presenter.Bind(_vm.Animals.Single(x => x.Id == b.Id));
            a.AcceptPhysicsPose(new EntityPose(V.One, Q.Identity));
            Assert.That(_view.GetInstanceID(), Is.EqualTo(id));
            Assert.That(_presenter, Is.SameAs(previousPresenter));
            Assert.That(_view.GetComponent<GameObjectContext>().Container, Is.SameAs(previousContext));
            Assert.That(_view.transform.position, Is.EqualTo(new Vector3(5, 0, 5)));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReusedViewAnimatesJumpAndFollowsFlightPose()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<AnimalView>("Assets/Zoo World/Prefabs/Animals/Frog.prefab");
            var jumping = _world.Spawn(Frog, V.Zero);
            _presenter = _pool.Rent<AnimalViewPresenter>(prefab.gameObject, _root.transform);
            _view = _presenter.View;
            var body = _view.transform.Find("Body");
            _presenter.Bind(_vm.Animals.Single(model => model.Id == jumping.Id));
            jumping.BeginMovement(new MovementPlan(2, 4, 0, 0.7f));
            var deadline = Time.realtimeSinceStartup + 1;
            while (body.localPosition.y <= 0 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(body.localPosition.y, Is.GreaterThan(0));

            var flight = new FlightMovement(3, 4);
            var definition = new AnimalDefinition("frog", FoodRole.Prey, Frog.Body, flight, Frog.Interactions, Frog.Behavior);
            var flying = _world.Spawn(definition, V.Zero);
            _pool.Return(_view.gameObject);
            _presenter = _pool.Rent<AnimalViewPresenter>(prefab.gameObject, _root.transform);
            _view = _presenter.View;
            Assert.That(_view.transform.Find("Body"), Is.SameAs(body));
            _presenter.Bind(_vm.Animals.Single(model => model.Id == flying.Id));
            flying.BeginMovement(flight.Plan(3));
            flying.AcceptPhysicsPose(new EntityPose(new V(0, flight.Height, 0), Q.Identity));
            yield return null;
            Assert.That(body.localPosition.y, Is.Zero);
            Assert.That(_view.transform.position.y, Is.EqualTo(flight.Height));
        }

        [UnityTest]
        public IEnumerator PooledMealAndTweenCannotLeakIntoNextActivation()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<AnimalView>("Assets/Zoo World/Prefabs/Animals/Snake.prefab");
            var a = _world.Spawn(Snake, V.Zero);
            _presenter = _pool.Rent<AnimalViewPresenter>(prefab.gameObject, _root.transform);
            _view = _presenter.View;
            _presenter.Bind(_vm.Animals.Single());
            _world.Eat(a, _world.Spawn(Frog, V.Zero), "First meal");
            Assert.That(_view.IsLabelVisible, Is.True);
            Assert.That(_view.GetComponentInChildren<UnityEngine.UI.Text>(true).text, Is.EqualTo("First meal"));
            yield return new WaitForSeconds(0.6f);
            _view.Unbind();
            _pool.Return(_view.gameObject);
            var b = _world.Spawn(Snake, V.One);
            _presenter = _pool.Rent<AnimalViewPresenter>(prefab.gameObject, _root.transform);
            _view = _presenter.View;
            _presenter.Bind(_vm.Animals.Single(x => x.Id == b.Id));
            Assert.That(_view.IsLabelVisible, Is.False);
            _world.Eat(b, _world.Spawn(Frog, V.One));
            Assert.That(_view.IsLabelVisible, Is.False);
            _world.Eat(b, _world.Spawn(Frog, V.One), "Another meal");
            Assert.That(_view.GetComponentInChildren<UnityEngine.UI.Text>(true).text, Is.EqualTo("Another meal"));
            yield return new WaitForSeconds(0.4f);
            Assert.That(_view.IsLabelVisible, Is.True);
            yield return new WaitForSeconds(0.6f);
            Assert.That(_view.IsLabelVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator FlightHoldsAltitudeAndReusedBodyRestoresGravity()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Zoo World/Prefabs/Physics/PhysicsBody.prefab");
            _physics = new UnityPhysicsWorld(_pool, _root.transform, prefab);
            var flight = new AnimalDefinition("frog", FoodRole.Prey, new BodyDefinition(0.45f, 1, 0.5f),
                new FlightMovement(3, 4), new InteractionProfile(), new BehaviorDefinition());
            var flying = _world.Spawn(flight, new V(0, 4, 0));
            _physics.Add(flying, flightAltitude: 4);
            var body = _root.GetComponentInChildren<Rigidbody>();
            _physics.Move(flying.Id, V.UnitX, 3, 0.3f);
            for (var step = 0; step < 30; step++)
            {
                _physics.Step(GameSession.FixedDelta);
            }

            Assert.That(body.position.y, Is.EqualTo(4 + flight.Body.Radius).Within(0.02f));
            Assert.That(body.position.x, Is.GreaterThan(0.5f));
            Assert.That(body.useGravity, Is.False);
            _physics.Remove(flying.Id);

            var walking = _world.Spawn(Snake, new V(0, 4, 0));
            _physics.Add(walking);
            var reused = _root.GetComponentInChildren<Rigidbody>();
            Assert.That(reused, Is.SameAs(body));
            Assert.That(reused.useGravity, Is.True);
            _physics.Step(GameSession.FixedDelta);
            Assert.That(reused.linearVelocity.y, Is.LessThan(0));
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnityPhysicsStopsAtBox()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.SetParent(_root.transform);
            ground.transform.position = new Vector3(0, -0.2f, 0);
            ground.transform.localScale = new Vector3(20, 0.4f, 20);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(_root.transform);
            wall.transform.position = new Vector3(3, 1, 0);
            wall.transform.localScale = new Vector3(1, 2, 8);
            _physics = new UnityPhysicsWorld(_pool, _root.transform, AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Zoo World/Prefabs/Physics/PhysicsBody.prefab"));
            var a = _world.Spawn(Snake, V.Zero);
            _physics.Add(a);
            using var poses = _physics.Poses.Subscribe(p => a.AcceptPhysicsPose(p.Pose));
            UnityEngine.Physics.SyncTransforms();
            _physics.Move(a.Id, V.UnitX, 2, 5);
            for (var i = 0; i < 150; i++)
            {
                _physics.Step(0.02f);
            }

            Assert.That(a.Pose.CurrentValue.Position.X, Is.InRange(1.8f, 2.05f));
            yield return null;
        }

        [Test]
        public void PoolRejectsDoubleReturn()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<AnimalView>("Assets/Zoo World/Prefabs/Animals/Snake.prefab");
            var go = _pool.Rent(prefab.gameObject, _root.transform);
            _pool.Return(go);
            Assert.Throws<InvalidOperationException>(() => _pool.Return(go));
        }

        [UnityTest]
        public IEnumerator UnityPhysicsCrossesAllEdgesOfThePlayArea()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<BoxCollider>();
            ground.transform.SetParent(_root.transform);
            ground.transform.position = new Vector3(3, -0.2f, 4);
            ground.transform.localScale = new Vector3(8, 0.4f, 6);
            ground.center = new Vector3(0.25f, 0, -0.5f);
            ground.isTrigger = true;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(_root.transform);
            floor.transform.position = new Vector3(5, -0.2f, 1);
            floor.transform.localScale = new Vector3(20, 0.4f, 20);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Zoo World/Prefabs/Physics/PhysicsBody.prefab");
            _physics = new UnityPhysicsWorld(_pool, _root.transform, prefab);
            using var poses = _physics.Poses.Subscribe(p =>
            {
                if (_world.TryGet(p.Id, out var animal))
                {
                    animal.AcceptPhysicsPose(p.Pose);
                }
            });
            foreach (var direction in new[]
            {
                V.UnitX,
                -V.UnitX,
                V.UnitZ,
                -V.UnitZ
            }

            )
            {
                var animal = _world.Spawn(Snake, new V(5, 0, 1));
                _physics.Add(animal);
                UnityEngine.Physics.SyncTransforms();
                _physics.Move(animal.Id, direction, 8, 1);
                for (var i = 0; i < 40; i++)
                {
                    _physics.Step(0.02f);
                }

                var distance = V.Dot(animal.Pose.CurrentValue.Position - new V(5, 0, 1), direction);
                Assert.That(distance, Is.GreaterThan(direction.X != 0 ? 4 : 3), direction.ToString());
                Assert.That(animal.Pose.CurrentValue.Position.Y, Is.EqualTo(0).Within(0.05f));
                _physics.Remove(animal.Id);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator UnobstructedJumpKeepsDistanceAndPreyContactSeparates()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.SetParent(_root.transform);
            ground.transform.position = new Vector3(0, -0.2f, 0);
            ground.transform.localScale = new Vector3(20, 0.4f, 20);
            _physics = new UnityPhysicsWorld(_pool, _root.transform, AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Zoo World/Prefabs/Physics/PhysicsBody.prefab"));
            var a = _world.Spawn(Frog, V.Zero);
            _physics.Add(a);
            using var poses = _physics.Poses.Subscribe(p =>
            {
                if (_world.TryGet(p.Id, out var entity))
                {
                    entity.AcceptPhysicsPose(p.Pose);
                }
            });
            UnityEngine.Physics.SyncTransforms();
            _physics.Move(a.Id, V.UnitX, 2 / 0.35f, 0.35f);
            for (var i = 0; i < 20; i++)
            {
                _physics.Step(0.02f);
            }

            Assert.That(a.Pose.CurrentValue.Position.X, Is.EqualTo(2).Within(0.04));
            var b = _world.Spawn(Frog, new V(2.4f, 0, 0));
            _physics.Add(b);
            var contacts = 0;
            using var events = _physics.Contacts.Subscribe(c =>
            {
                contacts++;
                _physics.Impulse(c.A, -V.UnitX);
                _physics.Impulse(c.B, V.UnitX);
            });
            UnityEngine.Physics.SyncTransforms();
            for (var i = 0; i < 10; i++)
            {
                _physics.Step(0.02f);
            }

            Assert.That(contacts, Is.EqualTo(1), "Positions: " + a.Pose.CurrentValue.Position + " / " + b.Pose.CurrentValue.Position);
            Assert.That(V.Distance(a.Pose.CurrentValue.Position, b.Pose.CurrentValue.Position), Is.GreaterThan(0.9f));
            yield return null;
        }

        [TestCase(2f)]
        [TestCase(100f)]
        public void EatingContactDoesNotDeflectPredator(float speed)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.SetParent(_root.transform);
            ground.transform.position = new Vector3(0, -0.2f, 0);
            ground.transform.localScale = new Vector3(20, 0.4f, 20);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Zoo World/Prefabs/Physics/PhysicsBody.prefab");
            _physics = new UnityPhysicsWorld(_pool, _root.transform, prefab);
            var snakeDefinition = AssetDatabase.LoadAssetAtPath<AnimalDefinitionSO>("Assets/Zoo World/Settings/Animals/Snake/SnakeDefinition.asset");
            var snake = _world.Spawn(snakeDefinition.Create(), V.Zero);
            var frog = _world.Spawn(Frog, new V(1.25f, 0, 0.25f));
            _physics.Add(snake);
            _physics.Add(frog);
            using var collisions = new Subject<CollisionEvent>();
            using var resolver = new InteractionResolver(collisions, new IInteraction[]
            {
                new EatingInteraction(_world, new System.Random(0)),
                new BounceInteraction(_physics)
            });
            _physics.SetContactFilter(resolver.ShouldSuppressPhysicalResponse);
            using var poses = _physics.Poses.Subscribe(p =>
            {
                if (_world.TryGet(p.Id, out var entity))
                {
                    entity.AcceptPhysicsPose(p.Pose);
                }
            });
            using var contacts = _physics.Contacts.Subscribe(contact =>
            {
                if (_world.TryGet(contact.A, out var first) && _world.TryGet(contact.B, out var second))
                {
                    collisions.OnNext(new CollisionEvent(first, second));
                }
            });
            using var removals = _world.Removed.Subscribe(entity => _physics.Remove(entity.Id));
            var meals = 0;
            using var mealsSubscription = _world.Eaten.Subscribe(_ => meals++);
            UnityEngine.Physics.SyncTransforms();
            _physics.Move(snake.Id, V.UnitX, speed, 1f);
            _physics.Move(frog.Id, -V.UnitX, 2f / 0.35f, 1f);
            for (var step = 0; step < 12 && frog.IsActive; step++)
            {
                var before = snake.Pose.CurrentValue.Position;
                _physics.Step(GameSession.FixedDelta);
                var pose = snake.Pose.CurrentValue;
                // The motor can change speed by at most 20 units per second each step.
                var expectedSpeed = Math.Min(speed, 20f * (step + 1));
                Assert.That(pose.Position.X - before.X, Is.EqualTo(expectedSpeed * GameSession.FixedDelta).Within(0.005f));
                Assert.That(pose.Position.Z, Is.EqualTo(0).Within(0.005f));
                Assert.That(V.Dot(V.Transform(V.UnitZ, pose.Rotation), V.UnitX), Is.GreaterThan(0.999f));
            }

            Assert.That(meals, Is.EqualTo(1));
            Assert.That(frog.IsActive, Is.False);
            _physics.Step(GameSession.FixedDelta);
            Assert.That(meals, Is.EqualTo(1));
        }

        [Test]
        public void PoolExpansionNeverRecyclesLiveLease()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<AnimalView>("Assets/Zoo World/Prefabs/Animals/Snake.prefab");
            var leases = new System.Collections.Generic.List<GameObject>();
            for (var i = 0; i < 40; i++)
            {
                leases.Add(_pool.Rent(prefab.gameObject, _root.transform));
            }

            Assert.That(leases.Select(x => x.GetInstanceID()).Distinct().Count(), Is.EqualTo(40));
            Assert.That(leases.Select(x => x.GetComponent<GameObjectContext>().Container).Distinct().Count(), Is.EqualTo(40));
            Assert.That(leases.Select(x => x.GetComponent<GameObjectContext>().Container.Resolve<AnimalViewPresenter>()).Distinct().Count(), Is.EqualTo(40));
            foreach (var lease in leases)
            {
                _pool.Return(lease);
            }
        }
    }
}
#endif

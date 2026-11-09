using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using NUnit.Framework;
using R3;
using Zoo.Application;
using Zoo.Configuration;
using Zoo.Domain;
using Zoo.Headless;
using Zoo.Headless.Physics;

namespace Zoo.Tests
{
    public sealed class ExportedSimulationTests
    {
        private static string Json => File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "zoo-world.json"));

        [TestCase("version")]
        [TestCase("movement")]
        [TestCase("behavior")]
        [TestCase("rule")]
        [TestCase("unknownRole")]
        [TestCase("duplicateRole")]
        [TestCase("missingRoles")]
        [TestCase("profile")]
        [TestCase("duplicateType")]
        [TestCase("emptyGeometry")]
        [TestCase("invalidMass")]
        public void InvalidConfigurationFailsBeforeStarting(string error)
        {
            var root = JsonNode.Parse(Json);
            switch (error)
            {
                case "version":
                    root["schemaVersion"] = 99;
                    break;
                case "movement":
                    root["animals"][0]["movement"]["kind"] = "teleport";
                    break;
                case "behavior":
                    root["animals"][0]["behavior"] = "unknown";
                    break;
                case "rule":
                    root["animals"][0]["interactions"]["byRole"][0]["rule"]["kind"] = "unknown";
                    break;
                case "unknownRole":
                    root["animals"][0]["interactions"]["byRole"][0]["target"] = "unknown";
                    break;
                case "duplicateRole":
                    var rules = root["animals"][0]["interactions"]["byRole"].AsArray();
                    rules.Add(rules[0].DeepClone());
                    break;
                case "missingRoles":
                    root["animals"][0]["interactions"].AsObject().Remove("byRole");
                    break;
                case "profile":
                    root["animals"][0]["navigationProfile"] = "missing";
                    break;
                case "duplicateType":
                    root["animals"][1]["typeId"] = root["animals"][0]["typeId"].GetValue<string>();
                    break;
                case "emptyGeometry":
                    root["world"]["boxes"] = new JsonArray();
                    break;
                case "invalidMass":
                    root["animals"][0]["mass"] = 0;
                    break;
            }

            Assert.Throws<ArgumentException>(() => ConfigurationJson.Parse(root.ToJsonString()));
        }

        [Test]
        public void AmbiguousAndUnknownJsonFieldsFail()
        {
            Assert.Throws<ArgumentException>(() => ConfigurationJson.Parse("{\"schemaVersion\":1,\"schemaVersion\":2}"));
            var root = JsonNode.Parse(Json);
            root["animals"][0]["speeed"] = 3;
            Assert.Throws<JsonException>(() => ConfigurationJson.Parse(root.ToJsonString()));
        }

        [Test]
        public void ImportedRoleRuleAndMovementParametersAreUsed()
        {
            var settings = ConfigurationJson.Parse(Json);
            var frog = settings.animals.Single(animal => animal.typeId == "frog");
            frog.movement.distance = 3;
            frog.movement.duration = 0.5f;
            frog.interactions.byRole = new[]
            {
                new InteractionSettings.Entry
                {
                    target = "prey",
                    rule = new InteractionRuleSettings
                    {
                        kind = "eat"
                    }
                }
            };
            var roundTrip = ConfigurationJson.Parse(JsonSerializer.Serialize(settings, new JsonSerializerOptions { IncludeFields = true }));
            var definition = roundTrip.CreateDefinitions().Single(animal => animal.TypeId == "frog");
            using var world = new EntityWorld();
            var targetDefinition = new AnimalDefinition("renamed-prey", FoodRole.Prey, definition.Body,
                definition.Movement, new InteractionProfile(), definition.Behavior);
            var target = world.Spawn(targetDefinition, Vector3.One);
            Assert.That(definition.Interactions.FindRule(target), Is.TypeOf<EatRule>());
            Assert.That(definition.Movement.Plan(10).Speed, Is.EqualTo(6));
        }

        [Test]
        public void EmptyRoleRulesAreAllowed()
        {
            var root = JsonNode.Parse(Json);
            root["animals"][0]["interactions"]["byRole"] = new JsonArray();
            var definition = ConfigurationJson.Parse(root.ToJsonString()).CreateDefinitions()[0];
            using var world = new EntityWorld();
            var target = world.Spawn(definition, Vector3.Zero);
            Assert.That(definition.Interactions.FindRule(target), Is.Null);
        }

        [Test]
        public void LegacySchemaAndTypeRulesAreRejected()
        {
            var root = JsonNode.Parse(Json);
            root["schemaVersion"] = 1;
            Assert.Throws<ArgumentException>(() => ConfigurationJson.Parse(root.ToJsonString()));

            root["schemaVersion"] = SimulationSettings.CurrentSchemaVersion;
            root["animals"][0]["interactions"]["byType"] = new JsonArray();
            Assert.Throws<JsonException>(() => ConfigurationJson.Parse(root.ToJsonString()));
        }

        [Test]
        public void BepuHandlesRotatedBoxesAndReportsContactOnlyOnEntry()
        {
            var settings = ConfigurationJson.Parse(Json);
            var wallRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.3f);
            settings.world.boxes = new[]
            {
                new BoxSettings
                {
                    id = "ground",
                    center = new VectorSettings(0, -0.2f, 0),
                    size = new VectorSettings(40, 0.4f, 40),
                    rotation = new RotationSettings
                    {
                        w = 1
                    }
                },
                new BoxSettings
                {
                    id = "wall",
                    center = new VectorSettings(3, 1, 0),
                    size = new VectorSettings(1, 2, 8),
                    rotation = new RotationSettings
                    {
                        x = wallRotation.X,
                        y = wallRotation.Y,
                        z = wallRotation.Z,
                        w = wallRotation.W
                    }
                }
            };
            using var physics = new BepuPhysicsWorld(settings.world);
            Assert.That(physics.IsFree(new Vector3(3, 0, 0), 0.5f), Is.False);
            Assert.That(physics.IsFree(Vector3.Zero, 0.5f), Is.True);
            using var world = new EntityWorld();
            var definition = settings.CreateDefinitions()[0];
            var animal = world.Spawn(definition, Vector3.Zero);
            physics.Add(animal);
            using var poses = physics.Poses.Subscribe(pose =>
            {
                if (world.TryGet(pose.Id, out var entity))
                {
                    entity.AcceptPhysicsPose(pose.Pose);
                }
            });
            physics.Move(animal.Id, Vector3.UnitX, 2, 5);
            for (var step = 0; step < 150; step++)
            {
                physics.Step(0.02f);
            }

            Assert.That(animal.Pose.CurrentValue.Position.X, Is.LessThan(2.5f));
            Assert.That(animal.Pose.CurrentValue.Position.X, Is.GreaterThan(1));
            physics.Remove(animal.Id);
            var first = world.Spawn(definition, new Vector3(-5, 0, 0));
            var second = world.Spawn(definition, new Vector3(-5, 0, definition.Body.Radius * 1.8f));
            physics.Add(first);
            physics.Add(second);
            var contacts = 0;
            using var contactEvents = physics.Contacts.Subscribe(_ => contacts++);
            for (var step = 0; step < 20; step++)
            {
                physics.Step(0.02f);
            }

            Assert.That(contacts, Is.EqualTo(1));
            Assert.That(Vector3.Distance(first.Pose.CurrentValue.Position, second.Pose.CurrentValue.Position), Is.GreaterThan(definition.Body.Radius * 1.9f));
        }

        [Test]
        public void ImportedFlightSpawnsAboveGroundAndHoldsAltitudeUnderGravity()
        {
            var root = JsonNode.Parse(Json);
            var animalSettings = root["animals"][0];
            animalSettings["movement"] = new JsonObject { ["kind"] = "flight", ["speed"] = 3, ["height"] = 4 };
            animalSettings["gravityScale"] = 2;
            root["animals"] = new JsonArray(JsonNode.Parse(animalSettings.ToJsonString()));
            var settings = ConfigurationJson.Parse(root.ToJsonString());
            using var session = HeadlessSimulation.Create(settings, 1701);
            var spawnPosition = Vector3.Zero;
            using var created = session.World.Created.Subscribe(entity => spawnPosition = entity.Pose.CurrentValue.Position);
            session.Start();
            session.Advance(2);
            var animal = session.World.Entities.OfType<Animal>().Single();
            Assert.That(spawnPosition.Y, Is.EqualTo(4));
            Assert.That(animal.Definition.Body.GravityScale, Is.EqualTo(2));

            var start = spawnPosition;
            session.Advance(1);
            var position = animal.Pose.CurrentValue.Position;
            Assert.That(position.Y, Is.EqualTo(4).Within(0.02f));
            Assert.That(Vector3.Distance(position, start), Is.GreaterThan(0.5f));
        }

        [Test]
        public void BepuAppliesEachAnimalsGravityScaleDuringFreeFall()
        {
            var settings = ConfigurationJson.Parse(Json);
            using var physics = new BepuPhysicsWorld(settings.world);
            using var world = new EntityWorld();
            const float startHeight = 8;
            Animal Spawn(float gravityScale, float x)
            {
                var definition = new AnimalDefinition("frog", FoodRole.Prey, new BodyDefinition(0.45f, 1, gravityScale),
                    new LinearMovement(2), new InteractionProfile(), new BehaviorDefinition());
                var animal = world.Spawn(definition, new Vector3(x, startHeight, 0));
                physics.Add(animal);
                return animal;
            }

            var weightless = Spawn(0, -4);
            var normal = Spawn(1, 0);
            var heavy = Spawn(2, 4);
            using var poses = physics.Poses.Subscribe(pose =>
            {
                if (world.TryGet(pose.Id, out var animal))
                {
                    animal.AcceptPhysicsPose(pose.Pose);
                }
            });

            for (var step = 0; step < 20; step++)
            {
                physics.Step(GameSession.FixedDelta);
            }

            var normalDrop = startHeight - normal.Pose.CurrentValue.Position.Y;
            Assert.That(weightless.Pose.CurrentValue.Position.Y, Is.EqualTo(startHeight).Within(0.001f));
            Assert.That(normalDrop, Is.GreaterThan(0.5f));
            Assert.That(startHeight - heavy.Pose.CurrentValue.Position.Y, Is.EqualTo(2 * normalDrop).Within(0.01f));
        }

        [Test]
        public void AnimalOutsideExportedPlayAreaCanReturnOnTheBakedFloor()
        {
            var settings = ConfigurationJson.Parse(Json);
            using var session = HeadlessSimulation.Create(settings, 1701);
            var snake = settings.CreateDefinitions().Single(animal => animal.TypeId == "snake");
            var start = new Vector3(
                (session.Bounds.Min.X + session.Bounds.Max.X) * 0.5f,
                session.Bounds.Min.Y,
                session.Bounds.Max.Z + 7);
            var animal = session.World.Spawn(snake, start);

            session.Advance(0.98f);
            Assert.That(session.Bounds.Contains(animal.Pose.CurrentValue.Position), Is.False);
            Assert.That(animal.Pose.CurrentValue.Position.Y, Is.EqualTo(start.Y).Within(0.05f));
            Assert.That(animal.Behavior.CurrentValue, Does.StartWith("Wander"));

            session.Advance(GameSession.FixedDelta);
            Assert.That(animal.Behavior.CurrentValue, Does.StartWith("Return"));
            for (var step = 0; step < 400 && !session.Bounds.Contains(animal.Pose.CurrentValue.Position); step++)
            {
                session.Advance(GameSession.FixedDelta);
            }

            Assert.That(session.Bounds.Contains(animal.Pose.CurrentValue.Position), Is.True);
        }

        [Test]
        public void ExportRunsRealNavigationPhysicsAndGameRulesReproducibly()
        {
            var settings = ConfigurationJson.Parse(Json);
            string Run()
            {
                using var session = HeadlessSimulation.Create(settings, 1701);
                using var viewModel = new ZooViewModel(session.World);
                var initial = new Dictionary<int, Vector3>();
                var spawned = 0;
                var crossedBoundary = false;
                using var created = session.World.Created.Subscribe(entity =>
                {
                    spawned++;
                    initial.Add(entity.Id, entity.Pose.CurrentValue.Position);
                });
                session.Start();
                for (var step = 0; step < 3000; step++)
                {
                    session.Advance(GameSession.FixedDelta);
                    crossedBoundary |= session.World.Entities.Any(entity => !session.Bounds.Contains(entity.Pose.CurrentValue.Position));
                }

                var entities = session.World.Entities.ToArray();
                Assert.That(spawned, Is.GreaterThan(25));
                Assert.That(viewModel.DeadPrey.CurrentValue + viewModel.DeadPredators.CurrentValue, Is.GreaterThan(0));
                Assert.That(entities.Any(entity => Vector3.Distance(initial[entity.Id], entity.Pose.CurrentValue.Position) > 0.5f), Is.True);
                Assert.That(crossedBoundary, Is.True);
                return string.Join("|", entities.Select(entity => entity.Id + ":" + entity.Pose.CurrentValue.Position));
            }

            Assert.That(Run(), Is.EqualTo(Run()));
        }
    }
}

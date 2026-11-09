using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using Zoo.Composition;
using Zoo.Configuration;
using Zoo.Domain;
using Zoo.Editor;
using Zoo.Unity;

namespace Zoo.Tests
{
    public sealed class SimulationExportTests
    {
        [Test]
        public void JsonSnapshotPreservesAuthoredDefinitionsNavigationAndWorld()
        {
            WithScenes((bootstrap, scene) =>
            {
                var colliderCount = scene.Ground.GetComponents<Collider>().Length;
                var source = bootstrap.Catalog.CreateDefinitions();
                var snapshot = SimulationSettingsExporter.Capture(bootstrap, scene);
                var json = JsonUtility.ToJson(snapshot);
                Assert.That(json, Does.Not.Contain("\"byType\""));
                var restored = JsonUtility.FromJson<SimulationSettings>(json);
                var definitions = restored.CreateDefinitions();
                Assert.That(restored.schemaVersion, Is.EqualTo(SimulationSettings.CurrentSchemaVersion));
                Assert.That(restored.seed, Is.EqualTo(bootstrap.Seed));
                Assert.That(restored.randomizeSeed, Is.EqualTo(bootstrap.RandomizeSeed));
                Assert.That(restored.world.CreateBounds().Min, Is.EqualTo(scene.ReadBounds().Min));
                Assert.That(restored.world.CreateBounds().Max, Is.EqualTo(scene.ReadBounds().Max));
                Assert.That(restored.world.boxes.Any(box => box.id.StartsWith("boundary-")), Is.False);
                Assert.That(scene.Ground.GetComponents<Collider>().Length, Is.EqualTo(colliderCount));
                foreach (var profile in restored.navigation.profiles)
                {
                    Assert.That(Convert.FromBase64String(profile.meshBase64), Is.EqualTo(scene.NavigationData.Profiles.Single(p => p.ProfileId == profile.id).Mesh));
                }

                using var world = new EntityWorld();
                for (var i = 0; i < source.Length; i++)
                {
                    var original = source[i];
                    var imported = definitions[i];
                    Assert.That(imported.TypeId, Is.EqualTo(original.TypeId));
                    Assert.That(imported.Role, Is.EqualTo(original.Role));
                    Assert.That(imported.Body.Radius, Is.EqualTo(original.Body.Radius));
                    Assert.That(imported.Body.Mass, Is.EqualTo(original.Body.Mass));
                    Assert.That(imported.Body.GravityScale, Is.EqualTo(original.Body.GravityScale));
                    Assert.That(imported.Behavior.RetrySeconds, Is.EqualTo(original.Behavior.RetrySeconds));
                    Assert.That(imported.Behavior.StuckSeconds, Is.EqualTo(original.Behavior.StuckSeconds));
                    Assert.That(imported.Movement.Plan(7), Is.EqualTo(original.Movement.Plan(7)));
                    Assert.That(imported.NavigationProfile, Is.EqualTo(original.NavigationProfile));
                    var actor = world.Spawn(imported, System.Numerics.Vector3.Zero);
                    foreach (var targetDefinition in source)
                    {
                        var target = world.Spawn(targetDefinition, System.Numerics.Vector3.One);
                        var sourceRule = original.Interactions.FindRule(target);
                        var importedRule = imported.Interactions.FindRule(target);
                        Assert.That(importedRule?.GetType(), Is.EqualTo(sourceRule?.GetType()));
                        if (sourceRule is BounceRule bounce)
                        {
                            Assert.That(((BounceRule)importedRule).Impulse, Is.EqualTo(bounce.Impulse));
                        }
                    }
                }
            });
        }

        [Test]
        public void ExportPreservesFlightHeightAndBodyGravity()
        {
            WithScenes((bootstrap, scene) =>
            {
                var original = bootstrap.Catalog;
                var catalog = UnityEngine.Object.Instantiate(original);
                var animal = UnityEngine.Object.Instantiate(original.Animals[0]);
                var body = UnityEngine.Object.Instantiate(animal.Body);
                var flight = ScriptableObject.CreateInstance<FlightMovementConfigSO>();
                try
                {
                    flight.Height = 5;
                    body.GravityScale = 0;
                    animal.Movement = flight;
                    animal.Body = body;
                    catalog.Animals = new[] { animal };
                    bootstrap.Catalog = catalog;
                    var snapshot = SimulationSettingsExporter.Capture(bootstrap, scene);
                    var restored = JsonUtility.FromJson<SimulationSettings>(JsonUtility.ToJson(snapshot));
                    var definition = restored.CreateDefinitions().Single();
                    Assert.That(((FlightMovement)definition.Movement).Height, Is.EqualTo(5));
                    Assert.That(definition.Body.GravityScale, Is.Zero);
                }
                finally
                {
                    bootstrap.Catalog = original;
                    UnityEngine.Object.DestroyImmediate(flight);
                    UnityEngine.Object.DestroyImmediate(body);
                    UnityEngine.Object.DestroyImmediate(animal);
                    UnityEngine.Object.DestroyImmediate(catalog);
                }
            });
        }

        [Test]
        public void StaleNavigationIsRejectedBeforeExport()
        {
            WithScenes((bootstrap, scene) =>
            {
                var original = scene.NavigationData;
                var stale = UnityEngine.Object.Instantiate(original);
                try
                {
                    stale.GeometryHash = "stale";
                    scene.NavigationData = stale;
                    Assert.That(() => SimulationSettingsExporter.Capture(bootstrap, scene), Throws.InvalidOperationException.With.Message.Contains("out of date"));
                }
                finally
                {
                    scene.NavigationData = original;
                    UnityEngine.Object.DestroyImmediate(stale);
                }
            });
        }

        private static void WithScenes(Action<BootstrapInstaller, GameSceneBindings> test)
        {
            var bootstrapScene = EditorSceneManager.OpenPreviewScene("Assets/Zoo World/Scenes/Bootstrap.unity");
            try
            {
                var bootstrap = bootstrapScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BootstrapInstaller>(true)).Single();
                var gameScene = EditorSceneManager.OpenPreviewScene("Assets/Zoo World/Scenes/Game.unity");
                try
                {
                    var scene = gameScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<GameSceneBindings>(true)).Single();
                    test(bootstrap, scene);
                }
                finally
                {
                    EditorSceneManager.ClosePreviewScene(gameScene);
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(bootstrapScene);
            }
        }
    }
}

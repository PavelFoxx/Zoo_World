using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Zoo.Unity;
using Zoo.Composition;
using Zenject;

namespace Zoo.Tests
{
    public sealed class SceneConfigurationTests
    {
        [Test]
        public void BootstrapIsFirstAndGameContainsAuthoredBoxesAndUgui()
        {
            Assert.That(EditorBuildSettings.scenes[0].path, Does.EndWith("Bootstrap.unity"));
            Assert.That(EditorBuildSettings.scenes.Any(s => s.enabled && s.path.EndsWith("Game.unity")), Is.True);
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Zoo World/Scenes/Game.unity");
            try
            {
                var bindings = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameSceneBindings>()).Single();
                var context = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SceneContext>()).Single();
                var installer = context.Installers.OfType<GameSceneInstaller>().Single();
                Assert.That(installer.Bindings, Is.SameAs(bindings));
                Assert.That(bindings.LevelColliders.Length, Is.GreaterThan(2));
                Assert.That(bindings.Hud.GetComponent<Canvas>(), Is.Not.Null);
                Assert.That(bindings.NavigationData.Profiles.All(p => p.Mesh.Length > 0), Is.True);
                Assert.That(scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ZooBootstrap>()), Is.Empty);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void CatalogUsesIndependentConfigurationAssets()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ZooCatalog>("Assets/Zoo World/Settings/Catalogs/Animals.asset");
            Assert.That(catalog.CreateDefinitions().Length, Is.EqualTo(2));
            foreach (var animal in catalog.Animals)
            {
                Assert.That(AssetDatabase.Contains(animal.Movement), Is.True);
                Assert.That(AssetDatabase.Contains(animal.Interactions), Is.True);
                var context = animal.Prefab.GetComponent<GameObjectContext>();
                Assert.That(context, Is.Not.Null);
                Assert.That(context.Installers.OfType<AnimalViewInstaller>().Count(), Is.EqualTo(1));
            }
        }

        [Test]
        public void BootstrapAndLoadingScreenHaveSeparateScopes()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Zoo World/Scenes/Bootstrap.unity");
            try
            {
                var roots = scene.GetRootGameObjects();
                var context = roots.SelectMany(r => r.GetComponentsInChildren<SceneContext>()).Single();
                Assert.That(context.Installers.OfType<BootstrapInstaller>().Count(), Is.EqualTo(1));
                var screen = roots.SelectMany(r => r.GetComponentsInChildren<LoadingScreenView>()).Single();
                var screenContext = screen.GetComponent<GameObjectContext>();
                Assert.That(screenContext, Is.Not.Null);
                Assert.That(screenContext.Installers.OfType<LoadingScreenInstaller>().Count(), Is.EqualTo(1));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}

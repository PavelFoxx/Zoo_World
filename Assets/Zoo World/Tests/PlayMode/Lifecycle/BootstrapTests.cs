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
    public sealed class BootstrapTests
    {
        [UnityTest]
        public IEnumerator CanceledPreparationReleasesResourcesBeforeRetry()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var previousMode = UnityEngine.Physics.simulationMode;
                var bootstrapScene = SceneManager.CreateScene("Bootstrap cancellation test");
                var root = new GameObject("Test SceneContext");
                root.SetActive(false);
                SceneManager.MoveGameObjectToScene(root, bootstrapScene);
                var installer = root.AddComponent<BootstrapInstaller>();
                installer.Catalog = AssetDatabase.LoadAssetAtPath<ZooCatalog>("Assets/Zoo World/Settings/Catalogs/Animals.asset");
                var context = root.AddComponent<SceneContext>();
                context.Installers = new[] { installer };
                root.SetActive(true);
                var loader = context.Container.Resolve<UnitySceneLoadingService>();
                var host = context.Container.Resolve<UnityGameSessionHost>();
                try
                {
                    await loader.Load("Game", CancellationToken.None);
                    using var lifetime = new CancellationTokenSource();
                    var preparation = host.Prepare(lifetime.Token).SuppressCancellationThrow();
                    Assert.That(host.Session, Is.Not.Null);
                    lifetime.Cancel();

                    Assert.That(await preparation, Is.True);
                    Assert.That(host.Session, Is.Null);
                    Assert.That(host.IsPlaying, Is.False);
                    Assert.That(UnityEngine.Physics.simulationMode, Is.EqualTo(previousMode));

                    await loader.Unload();
                    await loader.Load("Game", CancellationToken.None);
                    await host.Prepare(CancellationToken.None);
                    host.Start();
                    host.Advance(2.1f);
                    Assert.That(host.Session.World.Entities.Any(), Is.True);
                }
                finally
                {
                    host.Stop();
                    await loader.Unload();
                    await SceneManager.UnloadSceneAsync(bootstrapScene);
                }

                Assert.That(UnityEngine.Physics.simulationMode, Is.EqualTo(previousMode));
                LogAssert.NoUnexpectedReceived();
            });
        }

        [UnityTest]
        public IEnumerator BootstrapLoadsPreparedGameAndCanRestart()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            var deadline = Time.realtimeSinceStartup + 15;
            ZooBootstrap bootstrap = null;
            while (Time.realtimeSinceStartup < deadline)
            {
                bootstrap = Object.FindFirstObjectByType<ZooBootstrap>();
                if (bootstrap != null && bootstrap.Host?.IsPlaying == true)
                {
                    break;
                }

                yield return null;
            }

            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.Host.IsPlaying, Is.True, bootstrap.LastError);
            Assert.That(SceneManager.GetSceneByName("Bootstrap").isLoaded, Is.True);
            Assert.That(SceneManager.GetSceneByName("Game").isLoaded, Is.True);
            var registry = ProjectContext.Instance.Container.Resolve<SceneContextRegistry>();
            var bootstrapContext = registry.GetSceneContextForScene("Bootstrap");
            var gameContext = registry.GetSceneContextForScene("Game");
            Assert.That(gameContext.Container.ParentContainers, Does.Contain(bootstrapContext.Container));
            var bindings = gameContext.Container.Resolve<GameSceneBindings>();
            Assert.That(bindings.LevelColliders.Length, Is.GreaterThan(2));
            Assert.That(Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length, Is.GreaterThanOrEqualTo(2));
            var ground = bindings.Ground.bounds;
            var session = bootstrap.Host.Session;
            Assert.That(session.Bounds.Min, Is.EqualTo(new V(ground.min.x, ground.max.y, ground.min.z)));
            Assert.That(session.Bounds.Max, Is.EqualTo(new V(ground.max.x, ground.max.y, ground.max.z)));
            yield return new WaitForSeconds(4.5f);
            Assert.That(bootstrap.Host.Session.World.Entities.Any(), Is.True);
            var previous = bootstrap.Host.Session;
            bootstrap.Restart();
            deadline = Time.realtimeSinceStartup + 15;
            while (Time.realtimeSinceStartup < deadline && (!bootstrap.Host.IsPlaying || ReferenceEquals(previous, bootstrap.Host.Session)))
            {
                yield return null;
            }

            Assert.That(bootstrap.Host.IsPlaying, Is.True, bootstrap.LastError);
            Assert.That(bootstrap.Host.Session, Is.Not.SameAs(previous));
            Assert.That(previous.World.Entities, Is.Empty);
            Assert.That(registry.GetSceneContextForScene("Game"), Is.Not.SameAs(gameContext));
            Assert.That(Object.FindObjectsByType<GameSceneBindings>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Object.Destroy(bootstrap.gameObject);
            yield return null;
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif

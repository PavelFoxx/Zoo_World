using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using Zoo.Application;

namespace Zoo.Unity
{
    public sealed class UnitySceneLoadingService : ISceneLoadingService
    {
        private readonly ZenjectSceneLoader _loader;
        private Scene _scene;
        private bool _busy;
        public Scene LoadedScene => _scene;

        public UnitySceneLoadingService(ZenjectSceneLoader loader)
        {
            _loader = loader;
        }

        public async UniTask Load(string scene, CancellationToken token, IProgress<float> progress = null)
        {
            if (_busy)
            {
                throw new InvalidOperationException("A scene transition is already running.");
            }

            token.ThrowIfCancellationRequested();
            if (_scene.IsValid() && _scene.isLoaded)
            {
                throw new InvalidOperationException("Unload the current scene before loading another.");
            }

            // Validate before Zenject sets its one-shot parent-container handoff.
            if (!UnityEngine.Application.CanStreamedLevelBeLoaded(scene))
            {
                throw new InvalidOperationException("Scene is not in the build: " + scene);
            }

            _busy = true;
            try
            {
                progress?.Report(0);
                await UniTask.Yield();
                token.ThrowIfCancellationRequested();
                var operation = _loader.LoadSceneAsync(scene, LoadSceneMode.Additive,
                    containerMode: LoadSceneRelationship.Child);
                if (operation == null)
                {
                    throw new InvalidOperationException("Scene is not in the build: " + scene);
                }

                // Native loading cannot be canceled. Finish it, then clean up on canceled ownership.
                while (!operation.isDone)
                {
                    progress?.Report(Mathf.Clamp01(operation.progress / 0.9f));
                    await UniTask.Yield();
                }

                _scene = SceneManager.GetSceneByName(scene);
                // SceneContextRegistry is populated by SceneKernel.Start, after scene Awake.
                await UniTask.NextFrame();
                token.ThrowIfCancellationRequested();
                SceneManager.SetActiveScene(_scene);
                progress?.Report(1);
            }
            finally
            {
                _busy = false;
            }
        }

        public async UniTask Unload()
        {
            if (!_scene.IsValid() || !_scene.isLoaded)
            {
                return;
            }

            if (UnityEngine.Application.exitCancellationToken.IsCancellationRequested)
            {
                return;
            }

            var scene = _scene;
            _scene = default;
            await SceneManager.UnloadSceneAsync(scene);
        }
    }
}

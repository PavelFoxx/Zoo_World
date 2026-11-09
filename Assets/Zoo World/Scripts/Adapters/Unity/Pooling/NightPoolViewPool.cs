using System;
using System.Collections.Generic;
using NTC.Pool;
using UnityEngine;
using Zenject;

namespace Zoo.Unity
{
    public sealed class NightPoolViewPool : IViewPool
    {
        private readonly DiContainer _container;
        private readonly HashSet<GameObject> _injected = new HashSet<GameObject>();
        private readonly HashSet<GameObject> _leased = new HashSet<GameObject>();

        private readonly HashSet<GameObject> _prefabs = new HashSet<GameObject>();
        private bool _disposed;
        private Exception _injectionFailure;

        public NightPoolViewPool(DiContainer container)
        {
            _container = container;
        }

        public T Rent<T>(GameObject prefab, Transform parent) where T : class
        {
            var instance = Rent(prefab, parent);
            try
            {
                if (!instance.TryGetComponent<GameObjectContext>(out var context))
                {
                    throw new InvalidOperationException("A scoped view prefab requires GameObjectContext: " + prefab.name);
                }

                return context.Container.Resolve<T>();
            }
            catch
            {
                Return(instance);
                throw;
            }
        }

        public GameObject Rent(GameObject prefab, Transform parent)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(NightPoolViewPool));
            }

            if (_injectionFailure != null)
            {
                throw new InvalidOperationException("The pool scope failed to initialize. Reload Game before renting again.", _injectionFailure);
            }

            if (NightPool.TryGetPoolByPrefab(prefab, out var pool) && pool.SpawnedClonesCount >= pool.Capacity)
            {
                pool.SetCapacity(Math.Max(32, pool.Capacity * 2));
            }

            var instance = NightPool.Spawn(prefab, parent);
            _prefabs.Add(prefab);
            if (!_leased.Add(instance))
            {
                throw new InvalidOperationException("Pool recycled a live object.");
            }

            try
            {
                // A clone keeps its object scope across leases. Bind(model) starts each activation.
                // Pooled components must not consume injected services in Awake/OnEnable:
                // NightPool activates clones before returning them to this adapter.
                if (_injected.Add(instance))
                {
                    _container.InjectGameObject(instance);
                }
            }
            catch (Exception error)
            {
                // A partially initialized GameObjectContext cannot be injected a second time.
                _injectionFailure = error;
                Return(instance);
                throw;
            }

            return instance;
        }

        public void Return(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (!_leased.Remove(instance))
            {
                throw new InvalidOperationException("Object is not leased by this pool.");
            }

            if (!UnityEngine.Application.exitCancellationToken.IsCancellationRequested)
            {
                NightPool.Despawn(instance);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (var instance in new List<GameObject>(_leased))
            {
                Return(instance);
            }

            if (!UnityEngine.Application.exitCancellationToken.IsCancellationRequested)
            {
                foreach (var prefab in _prefabs)
                {
                    if (prefab != null && NightPool.TryGetPoolByPrefab(prefab, out var pool))
                    {
                        pool.DestroyPool();
                    }
                }
            }

            _prefabs.Clear();
            _injected.Clear();
        }
    }
}

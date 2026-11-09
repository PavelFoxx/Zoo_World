using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using Zoo.Application;

namespace Zoo.Unity
{
    public sealed class EntityViewCoordinator : IDisposable
    {
        private readonly IViewPool _pool;
        private readonly Transform _root;

        private readonly Dictionary<string, AnimalView> _prefabs = new Dictionary<string, AnimalView>();
        private readonly Dictionary<int, AnimalViewPresenter> _views = new Dictionary<int, AnimalViewPresenter>();

        private readonly CompositeDisposable _bindings = new CompositeDisposable();
        public EntityViewCoordinator(ZooViewModel model, ZooCatalog catalog, IViewPool pool, Transform root)
        {
            _pool = pool;
            _root = root;
            foreach (var definition in catalog.Animals)
            {
                _prefabs.Add(definition.Type.TypeId, definition.Prefab);
            }

            model.Added.Subscribe(Add).AddTo(_bindings);
            model.Removed.Subscribe(Remove).AddTo(_bindings);
            foreach (var animal in model.Animals)
            {
                Add(animal);
            }
        }

        private void Add(AnimalViewModel model)
        {
            var presenter = _pool.Rent<AnimalViewPresenter>(_prefabs[model.TypeId].gameObject, _root);
            try
            {
                presenter.Bind(model);
                _views.Add(model.Id, presenter);
            }
            catch
            {
                presenter.Unbind();
                _pool.Return(presenter.View.gameObject);
                throw;
            }
        }

        private void Remove(int id)
        {
            if (!_views.TryGetValue(id, out var view))
            {
                return;
            }

            _views.Remove(id);
            if (view.View == null)
            {
                return;
            }

            view.Unbind();
            _pool.Return(view.View.gameObject);
        }

        public void Dispose()
        {
            _bindings.Dispose();
            foreach (var id in new List<int>(_views.Keys))
            {
                Remove(id);
            }
        }
    }
}

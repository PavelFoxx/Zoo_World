using System;
using System.Collections.Generic;
using System.Numerics;
using R3;

namespace Zoo.Domain
{
    public sealed class EntityWorld : IDisposable
    {
        private readonly Dictionary<int, Entity> _entities = new Dictionary<int, Entity>();

        private readonly Subject<Entity> _created = new Subject<Entity>();
        private readonly Subject<Entity> _removed = new Subject<Entity>();

        private readonly Subject<AnimalMeal> _eaten = new Subject<AnimalMeal>();
        private int _nextId;
        public IEnumerable<Entity> Entities => _entities.Values;
        public Observable<Entity> Created => _created;
        public Observable<Entity> Removed => _removed;
        public Observable<AnimalMeal> Eaten => _eaten;

        public Animal Spawn(AnimalDefinition definition, Vector3 position)
        {
            var id = ++_nextId;
            Animal animal = new Animal(id, definition, position);
            _entities.Add(id, animal);
            _created.OnNext(animal);
            return animal;
        }

        public bool TryGet(int id, out Entity entity) => _entities.TryGetValue(id, out entity);

        public bool Eat(Animal predator, Entity victim, string message = null)
        {
            if (!predator.IsActive || !victim.IsActive || predator.Id == victim.Id)
            {
                return false;
            }

            victim.Deactivate();
            _eaten.OnNext(new AnimalMeal(predator, victim, message));
            Remove(victim.Id);
            return true;
        }

        public void Remove(int id)
        {
            if (!_entities.TryGetValue(id, out var entity))
            {
                return;
            }

            entity.Deactivate();
            _entities.Remove(id);
            _removed.OnNext(entity);
            entity.Dispose();
        }

        public void Dispose()
        {
            foreach (var id in new List<int>(_entities.Keys))
            {
                Remove(id);
            }

            _created.Dispose();
            _removed.Dispose();
            _eaten.Dispose();
        }
    }
}

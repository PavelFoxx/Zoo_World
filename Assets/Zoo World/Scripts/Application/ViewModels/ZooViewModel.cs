using System;
using System.Collections.Generic;
using R3;
using Zoo.Domain;

namespace Zoo.Application
{
    public sealed class ZooViewModel : IDisposable
    {
        private readonly Dictionary<int, AnimalViewModel> _animals = new Dictionary<int, AnimalViewModel>();

        private readonly Subject<AnimalViewModel> _added = new Subject<AnimalViewModel>();
        private readonly Subject<int> _removed = new Subject<int>();

        private readonly ReactiveProperty<int> _prey = new ReactiveProperty<int>();
        private readonly ReactiveProperty<int> _predators = new ReactiveProperty<int>();

        private readonly ReactiveProperty<int> _population = new ReactiveProperty<int>();
        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();
        public IEnumerable<AnimalViewModel> Animals => _animals.Values;
        public Observable<AnimalViewModel> Added => _added;
        public Observable<int> Removed => _removed;
        public ReadOnlyReactiveProperty<int> DeadPrey => _prey;
        public ReadOnlyReactiveProperty<int> DeadPredators => _predators;
        public ReadOnlyReactiveProperty<int> Population => _population;

        public ZooViewModel(EntityWorld world)
        {
            world.Created.Subscribe(Add).AddTo(_subscriptions);
            world.Removed.Subscribe(Remove).AddTo(_subscriptions);
            world.Eaten.Subscribe(Meal).AddTo(_subscriptions);
            foreach (var entity in world.Entities)
            {
                Add(entity);
            }
        }

        private void Add(Entity entity)
        {
            if (!(entity is Animal animal))
            {
                return;
            }

            var vm = new AnimalViewModel(animal);
            _animals.Add(vm.Id, vm);
            _population.Value = _animals.Count;
            _added.OnNext(vm);
        }

        private void Remove(Entity entity)
        {
            if (!_animals.TryGetValue(entity.Id, out var vm))
            {
                return;
            }

            _animals.Remove(entity.Id);
            _removed.OnNext(entity.Id);
            vm.Dispose();
            _population.Value = _animals.Count;
        }

        private void Meal(AnimalMeal meal)
        {
            if (meal.Victim is Animal victim)
            {
                if (victim.Definition.Role == FoodRole.Prey)
                {
                    _prey.Value++;
                }
                else
                {
                    _predators.Value++;
                }
            }

            if (_animals.TryGetValue(meal.Predator.Id, out var predator))
            {
                predator.NotifyMeal(meal);
            }
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
            foreach (var vm in _animals.Values)
            {
                vm.Dispose();
            }

            _animals.Clear();
            _added.Dispose();
            _removed.Dispose();
            _prey.Dispose();
            _predators.Dispose();
            _population.Dispose();
        }
    }
}

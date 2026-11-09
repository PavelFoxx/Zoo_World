using System;
using R3;
using Zoo.Domain;

namespace Zoo.Application
{
    public sealed class AnimalViewModel : IDisposable
    {
        private readonly Subject<AnimalMeal> _ate = new Subject<AnimalMeal>();
        public Animal Model { get; }

        public int Id => Model.Id;
        public string TypeId => Model.TypeId;
        public ReadOnlyReactiveProperty<EntityPose> Pose => Model.Pose;
        public Observable<MovementPlan> MovementStarted => Model.MovementStarted;
        public Observable<AnimalMeal> Ate => _ate;

        public AnimalViewModel(Animal animal)
        {
            Model = animal;
        }

        internal void NotifyMeal(AnimalMeal meal) => _ate.OnNext(meal);

        public void Dispose() => _ate.Dispose();
    }
}

using System.Numerics;
using R3;

namespace Zoo.Domain
{
    public class Animal : Entity
    {
        private readonly ReactiveProperty<string> _behavior = new ReactiveProperty<string>("Created");

        private readonly Subject<MovementPlan> _movementStarted = new Subject<MovementPlan>();
        public AnimalDefinition Definition { get; }

        public ReadOnlyReactiveProperty<string> Behavior => _behavior;
        public Observable<MovementPlan> MovementStarted => _movementStarted;

        public Animal(int id, AnimalDefinition definition, Vector3 position) : base(id, definition.TypeId, EntityKind.Animal, position)
        {
            Definition = definition;
        }

        public void SetBehavior(string behavior)
        {
            if (IsActive)
            {
                _behavior.Value = behavior;
            }
        }

        public void BeginMovement(MovementPlan plan)
        {
            if (IsActive)
            {
                _movementStarted.OnNext(plan);
            }
        }

        public override void Dispose()
        {
            _behavior.Dispose();
            _movementStarted.Dispose();
            base.Dispose();
        }
    }
}

using System;
using Zoo.Domain;

namespace Zoo.Application
{
    public sealed class EatingInteraction : IInteraction
    {
        private readonly EntityWorld _world;
        private readonly Random _random;
        private readonly string _message;

        public EatingInteraction(EntityWorld world, Random random, string message = "Tasty!")
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _message = message;
        }

        public InteractionType Type => InteractionType.Eating;
        public bool SuppressesPhysicalResponse => true;

        public bool CanExecute(in CollisionEvent collision) =>
            CanEat(collision.First, collision.Second) || CanEat(collision.Second, collision.First);

        public void Execute(in CollisionEvent collision)
        {
            var firstWins = CanEat(collision.First, collision.Second);
            if (firstWins && CanEat(collision.Second, collision.First))
            {
                firstWins = _random.Next(2) == 0;
            }

            var actor = (Animal)(firstWins ? collision.First : collision.Second);
            var target = firstWins ? collision.Second : collision.First;
            _world.Eat(actor, target, _message);
        }

        private static bool CanEat(Entity actor, Entity target) =>
            actor is Animal animal && animal.Definition.Interactions.FindRule(target) is EatRule;
    }
}

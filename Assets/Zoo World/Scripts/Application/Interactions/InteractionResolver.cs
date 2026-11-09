using System;
using System.Collections.Generic;
using System.Linq;
using R3;
using Zoo.Domain;

namespace Zoo.Application
{
    public sealed class InteractionResolver : IDisposable
    {
        private readonly IInteraction[] _interactions;
        private readonly IDisposable _subscription;

        public InteractionResolver(Observable<CollisionEvent> collisions, IEnumerable<IInteraction> interactions,
            Action<Exception> onError = null)
        {
            if (collisions == null)
            {
                throw new ArgumentNullException(nameof(collisions));
            }

            if (interactions == null)
            {
                throw new ArgumentNullException(nameof(interactions));
            }

            _interactions = interactions.OrderBy(interaction => interaction.Type).ToArray();
            if (_interactions.Select(interaction => interaction.Type).Distinct().Count() != _interactions.Length)
            {
                throw new ArgumentException("Register only one interaction per InteractionType.", nameof(interactions));
            }

            _subscription = collisions.Subscribe(Resolve,
                onError ?? ObservableSystem.GetUnhandledExceptionHandler(), _ => { });
        }

        public bool ShouldSuppressPhysicalResponse(Animal first, Animal second)
        {
            return IsValid(first, second) &&
                   FindInteraction(new CollisionEvent(first, second))?.SuppressesPhysicalResponse == true;
        }

        private void Resolve(CollisionEvent collision)
        {
            if (!IsValid(collision.First, collision.Second))
            {
                return;
            }

            FindInteraction(collision)?.Execute(collision);
        }

        private IInteraction FindInteraction(in CollisionEvent collision)
        {
            foreach (var interaction in _interactions)
            {
                if (interaction.CanExecute(collision))
                {
                    return interaction;
                }
            }

            return null;
        }

        private static bool IsValid(Entity first, Entity second) =>
            first != null && second != null && first.IsActive && second.IsActive && first.Id != second.Id;

        public void Dispose() => _subscription.Dispose();
    }
}

using System;
using Zoo.Domain;

namespace Zoo.Application
{
    public readonly struct CollisionEvent
    {
        public Entity First { get; }
        public Entity Second { get; }

        public CollisionEvent(Entity first, Entity second)
        {
            if (first == null)
            {
                throw new ArgumentNullException(nameof(first));
            }

            if (second == null)
            {
                throw new ArgumentNullException(nameof(second));
            }

            if (first.Id > second.Id)
            {
                (first, second) = (second, first);
            }

            First = first;
            Second = second;
        }
    }
}

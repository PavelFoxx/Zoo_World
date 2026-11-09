using System;
using System.Collections.Generic;
using Zoo.Domain;

namespace Zoo.Application
{
    public sealed class AnimalSpawner
    {
        private const int MaximumAttempts = 32;
        private const float BoundaryMargin = 0.8f;
        private readonly AnimalDefinition[] _definitions;
        private readonly EntityWorld _world;
        private readonly IPhysicsWorld _physics;
        private readonly INavigationService _navigation;
        private readonly WorldBounds _bounds;
        private readonly Random _random;

        public AnimalSpawner(AnimalDefinition[] definitions, EntityWorld world, IPhysicsWorld physics, INavigationService navigation, WorldBounds bounds, Random random)
        {
            if (definitions == null || definitions.Length == 0)
            {
                throw new ArgumentException("Animal catalog is empty.", nameof(definitions));
            }

            var ids = new HashSet<string>();
            foreach (var definition in definitions)
            {
                if (definition == null || !ids.Add(definition.TypeId))
                {
                    throw new ArgumentException("Duplicate or null animal definition.", nameof(definitions));
                }
            }

            _definitions = (AnimalDefinition[])definitions.Clone();
            _world = world;
            _physics = physics;
            _navigation = navigation;
            _bounds = bounds;
            _random = random;
        }

        public void SpawnRandom()
        {
            var definition = _definitions[_random.Next(_definitions.Length)];
            for (var attempt = 0; attempt < MaximumAttempts; attempt++)
            {
                var point = _bounds.Sample(_random, definition.Body.Radius + BoundaryMargin);
                if (definition.Movement is FlightMovement flight)
                {
                    point.Y = _bounds.Min.Y + flight.Height;
                }
                else if (!_navigation.TryProject(point, definition.NavigationProfile, out point))
                {
                    continue;
                }

                if (_physics.IsFree(point, definition.Body.Radius))
                {
                    _world.Spawn(definition, point);
                    return;
                }
            }
        }
    }
}

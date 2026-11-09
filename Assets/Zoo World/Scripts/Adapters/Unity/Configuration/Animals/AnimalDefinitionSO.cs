using System;
using UnityEngine;
using Zoo.Domain;

namespace Zoo.Unity
{
    [CreateAssetMenu(menuName = "Zoo/Animal definition")]
    public sealed class AnimalDefinitionSO : ScriptableObject
    {
        public EntityTypeSO Type;
        public FoodRole Role;
        public BehaviorProfileSO Behavior;
        public MovementConfigSO Movement;
        public InteractionProfileSO Interactions;
        public PhysicsBodyConfigSO Body;
        public NavigationAgentConfigSO Navigation;
        public AnimalView Prefab;

        public AnimalDefinition Create()
        {
            if (Type == null || Behavior == null || Movement == null || Interactions == null || Body == null || Navigation == null || Prefab == null)
            {
                throw new InvalidOperationException(name + ": incomplete animal definition.");
            }

            if (Navigation.Radius < Body.Radius)
            {
                throw new InvalidOperationException(name + ": navigation clearance is smaller than physical radius.");
            }

            return new AnimalDefinition(Type.TypeId, Role, Body.Create(), Movement.Create(), Interactions.Create(), Behavior.Create(), Navigation.ProfileId);
        }
    }
}

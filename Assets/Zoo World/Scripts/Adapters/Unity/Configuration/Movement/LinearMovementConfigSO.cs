using UnityEngine;
using Zoo.Domain;

namespace Zoo.Unity
{
    [CreateAssetMenu(menuName = "Zoo/Movement/Linear")]
    public sealed class LinearMovementConfigSO : MovementConfigSO
    {
        [Min(0.01f)]
        public float Speed = 2;

        public override IMovementBehavior Create() => new LinearMovement(Speed);
    }
}

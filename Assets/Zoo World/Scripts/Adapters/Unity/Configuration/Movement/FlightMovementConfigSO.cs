using UnityEngine;
using Zoo.Domain;

namespace Zoo.Unity
{
    [CreateAssetMenu(menuName = "Zoo/Movement/Flight")]
    public sealed class FlightMovementConfigSO : MovementConfigSO
    {
        [Min(0.01f)]
        public float Speed = 3;
        [Min(0.01f)]
        public float Height = 4;

        public override IMovementBehavior Create() => new FlightMovement(Speed, Height);
    }
}

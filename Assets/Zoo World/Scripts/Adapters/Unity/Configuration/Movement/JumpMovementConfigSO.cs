using UnityEngine;
using Zoo.Domain;

namespace Zoo.Unity
{
    [CreateAssetMenu(menuName = "Zoo/Movement/Jump")]
    public sealed class JumpMovementConfigSO : MovementConfigSO
    {
        [Min(0.01f)]
        public float Distance = 2;
        [Min(0.01f)]
        public float Duration = 0.35f;
        [Min(0)]
        public float Rest = 1.1f;
        [Min(0)]
        public float Height = 0.7f;

        public override IMovementBehavior Create() => new JumpMovement(Distance, Duration, Rest, Height);
    }
}

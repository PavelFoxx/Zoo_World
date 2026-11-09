using UnityEngine;
using Zoo.Domain;

namespace Zoo.Unity
{
    [CreateAssetMenu(menuName = "Zoo/Physics body")]
    public sealed class PhysicsBodyConfigSO : ScriptableObject
    {
        [Min(0.01f)]
        public float Radius = 0.45f;
        [Min(0.01f)]
        public float Mass = 1;

        [Min(0)]
        public float GravityScale = 1;

        public BodyDefinition Create() => new BodyDefinition(Radius, Mass, GravityScale);
    }
}

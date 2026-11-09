using UnityEngine;

namespace Zoo.Unity
{
    [CreateAssetMenu(menuName = "Zoo/Navigation/Agent")]
    public sealed class NavigationAgentConfigSO : ScriptableObject
    {
        public string ProfileId = "ground";
        [Min(0.01f)]
        public float Radius = 0.6f;
        [Min(0.01f)]
        public float Height = 1.2f;
        [Min(0)]
        public float MaxClimb = 0.25f;
        [Range(0, 85)]
        public float MaxSlope = 40;
    }
}

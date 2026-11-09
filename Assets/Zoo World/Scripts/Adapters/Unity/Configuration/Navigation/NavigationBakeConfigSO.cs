using UnityEngine;

namespace Zoo.Unity
{
    [CreateAssetMenu(menuName = "Zoo/Navigation/Bake settings")]
    public sealed class NavigationBakeConfigSO : ScriptableObject
    {
        [Min(0.05f)]
        public float CellSize = 0.15f;
        public NavigationAgentConfigSO[] Profiles;
    }
}

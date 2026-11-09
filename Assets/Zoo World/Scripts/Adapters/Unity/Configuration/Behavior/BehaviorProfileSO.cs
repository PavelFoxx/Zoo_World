using UnityEngine;
using Zoo.Domain;

namespace Zoo.Unity
{
    [CreateAssetMenu(menuName = "Zoo/Behavior/Wander and return")]
    public sealed class BehaviorProfileSO : ScriptableObject
    {
        [Min(0.02f)]
        public float RetrySeconds = 0.4f;
        [Min(0.02f)]
        public float StuckSeconds = 8;

        public BehaviorDefinition Create() => new BehaviorDefinition(RetrySeconds, StuckSeconds);
    }
}

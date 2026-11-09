using UnityEngine;

namespace Zoo.Unity
{
    [CreateAssetMenu(menuName = "Zoo/Entity type")]
    public sealed class EntityTypeSO : ScriptableObject
    {
        public string TypeId;
    }
}

using UnityEngine;
using Zoo.Domain;

namespace Zoo.Unity
{
    public abstract class MovementConfigSO : ScriptableObject
    {
        public abstract IMovementBehavior Create();
    }
}

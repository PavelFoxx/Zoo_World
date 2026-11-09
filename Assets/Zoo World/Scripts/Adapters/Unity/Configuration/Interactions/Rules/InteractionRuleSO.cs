using UnityEngine;
using Zoo.Domain;

namespace Zoo.Unity
{
    public abstract class InteractionRuleSO : ScriptableObject
    {
        public abstract IInteractionRule Create();
    }
}

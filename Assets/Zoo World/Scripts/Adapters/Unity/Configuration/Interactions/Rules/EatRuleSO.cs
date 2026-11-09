using UnityEngine;
using Zoo.Domain;

namespace Zoo.Unity
{
    [CreateAssetMenu(menuName = "Zoo/Interaction/Eat")]
    public sealed class EatRuleSO : InteractionRuleSO
    {
        public override IInteractionRule Create() => new EatRule();
    }
}

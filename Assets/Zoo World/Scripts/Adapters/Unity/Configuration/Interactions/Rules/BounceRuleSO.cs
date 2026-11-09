using UnityEngine;
using Zoo.Domain;

namespace Zoo.Unity
{
    [CreateAssetMenu(menuName = "Zoo/Interaction/Bounce")]
    public sealed class BounceRuleSO : InteractionRuleSO
    {
        [Min(0)]
        public float Impulse = 1.5f;

        public override IInteractionRule Create() => new BounceRule(Impulse);
    }
}

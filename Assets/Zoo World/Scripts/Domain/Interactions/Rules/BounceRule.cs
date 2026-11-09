namespace Zoo.Domain
{
    public sealed class BounceRule : IInteractionRule
    {
        public float Impulse { get; }

        public BounceRule(float impulse = 1.5f)
        {
            Guard.NonNegative(impulse, nameof(impulse));
            Impulse = impulse;
        }
    }
}

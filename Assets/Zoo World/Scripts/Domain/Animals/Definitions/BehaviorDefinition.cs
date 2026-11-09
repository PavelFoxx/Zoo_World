namespace Zoo.Domain
{
    public sealed class BehaviorDefinition
    {
        public float RetrySeconds { get; }

        public float StuckSeconds { get; }

        public BehaviorDefinition(float retrySeconds = 0.4f, float stuckSeconds = 8)
        {
            Guard.Positive(retrySeconds, nameof(retrySeconds));
            Guard.Positive(stuckSeconds, nameof(stuckSeconds));
            RetrySeconds = retrySeconds;
            StuckSeconds = stuckSeconds;
        }
    }
}

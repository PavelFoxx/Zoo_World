namespace Zoo.Application
{
    public interface IInteraction
    {
        InteractionType Type { get; }
        bool SuppressesPhysicalResponse { get; }

        // PhysX also uses this query. It must not mutate state, consume randomness or allocate.
        bool CanExecute(in CollisionEvent collision);
        void Execute(in CollisionEvent collision);
    }
}

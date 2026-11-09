namespace Zoo.Domain
{
    public interface IMovementBehavior
    {
        MovementPlan Plan(float remainingDistance);
    }
}
